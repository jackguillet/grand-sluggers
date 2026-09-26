using System;
using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The specials' tells (spec §13, AB-C15), HUD-off. Each special's VFX slot (<c>data/art/vfx.json</c>) names its tell — one
    /// of <see cref="SpecialTells.Builders"/> — and this draws that tell's procedural stand-in until an Art session drops a
    /// prefab into the slot's folder. Every tell is placed and timed from the sim alone: the pitch's own
    /// <see cref="PitchFlight.Point"/> and row, the live ball's path, facts and status volumes. Nothing here decides a play,
    /// and no tell names a special: the builder is picked by the slot's <c>tell</c>. No full-screen paint, no input invert, and
    /// no line to a glove or a throw's end: a tell's line traces the ball's own flight, a ring or a cable, never a beam.
    /// </summary>
    public sealed class SpecialFx : MonoBehaviour
    {
        /// <summary>What one frame of the match shows a tell: the pitch in the air, the swing at the plate, the live ball.</summary>
        public struct Frame
        {
            public float Dt;
            public RulesTable Rules;
            public StarSkillTable Skills;
            public AudioBus Audio;
            public BallView Ball;
            /// <summary>A star pitch in the air: its command, its id, the flight share and its air time, and where it left the hand.</summary>
            public PitchCommand Pitch;
            public string StarPitch;
            public float PitchU;
            public float PitchDur;
            public Vector3 ReleaseFrom;
            /// <summary>The star swing the batter holds (the release would ask for it), and the oval the plate draws for it.</summary>
            public string ArmedSwing;
            public CursorOval Oval;
            public bool OvalShown;
            /// <summary>The live ball: the system, where the drawn ball is, and where every glove stands.</summary>
            public LivePlaySystem Live;
            public bool InPlay;
            public Vector3 BallAt;
            public IReadOnlyDictionary<string, (double X, double Z)> Gloves;
        }

        delegate void TellTick(bool on);
        delegate void TellBeat(double u);

        Transform _root;
        readonly Dictionary<string, Transform> _groups = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, TellTick> _builders = new Dictionary<string, TellTick>(StringComparer.Ordinal);
        readonly Dictionary<string, Transform> _parts = new Dictionary<string, Transform>(StringComparer.Ordinal);
        /// <summary>The tells that burst at a pitch beat, by builder: a card flip, a clang, a splash, the shimmer coming back.</summary>
        readonly Dictionary<string, TellBeat> _beats = new Dictionary<string, TellBeat>(StringComparer.Ordinal);
        readonly HashSet<string> _on = new HashSet<string>(StringComparer.Ordinal);
        Frame _f;
        float _t;
        float _prevU = -1f;
        double _prevElapsed = -1;
        int _factsSeen;
        string _pitchTell = "";
        string _pitchId = "";
        string _event = "";
        float _hissAt = -1f;
        Transform _buddyFlash;
        Material _lineMat;

        /// <summary>The diamond the tells stand on (the match table's).</summary>
        DiamondGeometry _diamond;

        public void Build(Transform parent, DiamondGeometry diamond)
        {
            _diamond = diamond;
            if (_root != null) Destroy(_root.gameObject);
            _root = new GameObject("SpecialFx").transform;
            _root.SetParent(parent, false);
            _groups.Clear();
            _parts.Clear();
            _lineMat = LineMaterial();
            _builders.Clear();
            _builders["spark-tail"] = SparkTail;
            _builders["spark-ring"] = SparkRing;
            _builders["aurora-ribbon"] = AuroraRibbon;
            _builders["follow-spot"] = FollowSpot;
            _builders["coaster-track"] = CoasterTrack;
            _builders["spinning-top"] = SpinningTop;
            _builders["card-flip"] = CardFlip;
            _builders["card-decoy"] = CardDecoy;
            _builders["vine"] = Vine;
            _builders["bolt-trail"] = BoltTrail;
            _builders["anvil"] = Anvil;
            _builders["molten"] = Molten;
            _builders["wave-ring"] = WaveRing;
            _builders["tall-oval"] = TallOval;
            _builders["shimmer"] = Shimmer;
            _builders["dust-swirl"] = DustSwirl;
            _builders["cable-line"] = CableLine;
            _builders["snow-flurry"] = SnowFlurry;
            _builders["splash-ring"] = SplashRing;
            _builders["lily-pad"] = LilyPad;
            _beats.Clear();
            _beats["card-flip"] = _ => _flipT = 0f;
            _beats["anvil"] = _ => _clangT = 0f;
            _beats["splash-ring"] = SplashAt;
            _beats["shimmer"] = ShimmerBack;
            _buddyFlash = Look.Prim(PrimitiveType.Cylinder, "Flash", Group("buddy-flash"), Vector3.zero, new Vector3(3.6f, 0.12f, 3.6f),
                Look.Unlit(Colors.Gold)).transform;
            HideAll();
        }

        /// <summary>
        /// One frame: the pitch's tell while a star pitch flies (its beats sound its cue), the plate's tell while a star swing is
        /// held, and the live ball's tells — the swing that put it in play, and the pitch it was hit off — while it is live.
        /// </summary>
        public void Tick(Frame f)
        {
            if (_root == null) return;
            _f = f;
            _t += f.Dt;
            var on = _on;
            on.Clear();
            _event = "";

            // The pitch in the air.
            var flying = f.Pitch != null && f.Pitch.Star && !string.IsNullOrEmpty(f.StarPitch) && f.PitchDur > 0;
            if (flying)
            {
                if (!string.Equals(_pitchId, f.StarPitch, StringComparison.OrdinalIgnoreCase) || f.PitchU < _prevU)
                    NewPitch();
                _pitchId = f.StarPitch;
                _pitchTell = ArtBinder.TellOf(f.StarPitch);
                var row = f.Skills?.Pitch(f.StarPitch);
                foreach (var beat in SpecialTells.PitchBeats(row, f.Rules))
                {
                    if (!SpecialTells.Crossed(_prevU, f.PitchU, beat)) continue;
                    Cue(f.StarPitch);
                    Beat(beat);
                }
                _prevU = f.PitchU;
                Mark(on, _pitchTell, f.StarPitch);
            }
            else if (_pitchTell.Length > 0 && Lingers(_pitchTell))
                Mark(on, _pitchTell, _pitchId);

            // The swing held at the plate.
            if (f.OvalShown && !string.IsNullOrEmpty(f.ArmedSwing))
                Mark(on, ArtBinder.TellOf(f.ArmedSwing), f.ArmedSwing);

            // The live ball.
            var live = f.Live;
            var playing = f.InPlay && live != null && live.Hit != null;
            if (playing)
            {
                if (live.FactsThisPlay.Count < _factsSeen || live.ElapsedSeconds < _prevElapsed) NewPlay();
                var hit = live.Hit;
                if (!string.IsNullOrEmpty(hit.StarSwingUsed)) Mark(on, ArtBinder.TellOf(hit.StarSwingUsed), hit.StarSwingUsed);
                if (!string.IsNullOrEmpty(hit.StarPitchUsed)) Mark(on, ArtBinder.TellOf(hit.StarPitchUsed), hit.StarPitchUsed);
            }
            else NewPlay();

            foreach (var kv in _builders)
                kv.Value(on.Contains(kv.Key));
            foreach (var kv in _groups)
                if (kv.Value != null && kv.Key != "buddy-flash") kv.Value.gameObject.SetActive(on.Contains(ArtBinder.TellOf(kv.Key)));

            if (playing)
            {
                _factsSeen = live.FactsThisPlay.Count;
                _prevElapsed = live.ElapsedSeconds;
            }
        }

        void Mark(HashSet<string> on, string tell, string id)
        {
            if (string.IsNullOrEmpty(tell) || !_builders.ContainsKey(tell)) return;
            on.Add(tell);
            Group(id);
            if (_event.Length == 0) _event = id;
        }

        /// <summary>A pitch tell that keeps drawing after the flight: a splash ring still spreading on the dirt.</summary>
        bool Lingers(string tell) =>
            _beats.ContainsKey(tell) && (_splashT[0] < SplashSec || _splashT[1] < SplashSec);

        /// <summary>A new pitch: every trail and beat of the last one is forgotten.</summary>
        public void NewPitch()
        {
            _prevU = -1f;
            _flipT = 99f;
            _clangT = 99f;
            _shimmerT = 99f;
            _splashT[0] = _splashT[1] = 99f;
            _pitchTell = "";
            _pitchId = "";
        }

        void NewPlay()
        {
            _factsSeen = 0;
            _prevElapsed = -1;
            _flurryT = 99f;
            _thundered = false;
            _croaked = false;
            _hissAt = -1f;
        }

        /// <summary>Hide every tell (a staged still, a scene change).</summary>
        public void Clear()
        {
            NewPitch();
            NewPlay();
            HideAll();
            foreach (var kv in _parts) if (kv.Value != null) kv.Value.gameObject.SetActive(false);
            _f.Ball?.Tint(null);
        }

        public static Color ThrowColor(Chemistry rel)
        {
            var rgb = ThrowTrail.BallRgb(rel);
            return new Color((float)rgb.R, (float)rgb.G, (float)rgb.B, 1f);
        }

        /// <summary>A tell is drawing: the ball stays shown while it does.</summary>
        public bool Active => _event.Length > 0;

        /// <summary>The special whose tell is drawing now, for the feel overlay.</summary>
        public string CurrentEvent => _event;

        public void BuddyTell(bool on, Vector3 at, bool window)
        {
            Show("buddy-flash", on);
            if (_buddyFlash == null) return;
            _buddyFlash.gameObject.SetActive(on);
            if (!on) return;
            var pulse = window
                ? 4.4f + 1.8f * Mathf.Abs(Mathf.Sin(_t * 14f))
                : 3.2f + 0.55f * Mathf.Abs(Mathf.Sin(_t * 5f));
            _buddyFlash.position = at + Vector3.up * 0.14f;
            _buddyFlash.localScale = new Vector3(pulse, 0.14f, pulse);
            var c = window ? Colors.Gold : Color.Lerp(Colors.Gold, Color.white, 0.28f);
            Look.Paint(_buddyFlash.gameObject, Look.Unlit(c));
        }

        // ---------------------------------------------------------------- pitch tells

        const int TailSparks = 14;

        /// <summary>Skyrocket: gold sparks stream off the ball; from the rise they burn brighter and wider.</summary>
        void SparkTail(bool on)
        {
            var parts = Many("spark-tail", TailSparks, i => Look.Prim(PrimitiveType.Sphere, "Spark" + i, null, Vector3.zero,
                Vector3.one * 0.2f, Look.Unlit(Color.Lerp(Colors.Gold, Color.white, (i % 3) * 0.25f))));
            if (!Visible(parts, on)) return;
            var rise = _f.Skills?.Pitch(_f.StarPitch)?.Rise;
            var lit = rise != null && _f.PitchU >= rise.From;
            for (var i = 0; i < parts.Length; i++)
            {
                var s = Mathf.Max(0f, _f.PitchU - i * 0.012f);
                var jitter = new Vector3(Hash(i, 1), Hash(i, 2), Hash(i, 3)) * (0.06f * i) * (lit ? 1.6f : 1f);
                parts[i].position = PitchAt(s) + jitter;
                var flicker = 0.75f + 0.25f * Mathf.Abs(Mathf.Sin(_t * 23f + i));
                parts[i].localScale = Vector3.one * (0.26f - i * 0.013f) * flicker * (lit ? 1.5f : 1f);
            }
        }

        /// <summary>Aurora Ribbon: a ribbon traces the path the ball has swayed along, bright at the ball and fading behind.</summary>
        void AuroraRibbon(bool on)
        {
            var a = Line("aurora-ribbon", "RibbonA", new Color(0.35f, 1f, 0.8f, 0.9f), new Color(0.6f, 0.4f, 1f, 0f), 0.5f, 0.08f);
            var b = Line("aurora-ribbon", "RibbonB", new Color(1f, 0.55f, 0.85f, 0.8f), new Color(0.45f, 0.85f, 1f, 0f), 0.22f, 0.04f);
            if (!Visible(a, on) | !Visible(b, on)) return;
            const int n = 40;
            a.positionCount = n;
            b.positionCount = n;
            for (var i = 0; i < n; i++)
            {
                var s = _f.PitchU * (1f - i / (float)(n - 1));
                var p = PitchAt(s);
                a.SetPosition(i, p);
                b.SetPosition(i, p + Vector3.up * (0.28f + 0.08f * Mathf.Sin(_t * 9f + i * 0.4f)));
            }
        }

        /// <summary>Loop-the-Loop: two red rails and their ties laid under the ball along the loop, a coaster's track.</summary>
        void CoasterTrack(bool on)
        {
            var left = Line("coaster-track", "RailL", new Color(0.95f, 0.2f, 0.25f, 1f), new Color(0.95f, 0.2f, 0.25f, 0.4f), 0.1f, 0.1f);
            var right = Line("coaster-track", "RailR", new Color(0.95f, 0.2f, 0.25f, 1f), new Color(0.95f, 0.2f, 0.25f, 0.4f), 0.1f, 0.1f);
            var ties = Many("coaster-track", 18, i => Look.Prim(PrimitiveType.Cube, "Tie" + i, null, Vector3.zero,
                new Vector3(0.62f, 0.05f, 0.12f), Look.Unlit(new Color(0.98f, 0.93f, 0.85f))));
            var loop = _f.Skills?.Pitch(_f.StarPitch)?.Loop;
            on &= loop != null;
            Visible(left, on);
            Visible(right, on);
            if (!Visible(ties, on)) return;
            // The track runs from a little before the loop to the ball, never past the loop's exit.
            var from = Mathf.Max(0f, (float)loop.At - 0.06f);
            var to = Mathf.Clamp(_f.PitchU, from, (float)loop.Exit + 0.06f);
            const int n = 48;
            left.positionCount = right.positionCount = n;
            for (var i = 0; i < n; i++)
            {
                var p = PitchAt(Mathf.Lerp(from, to, i / (float)(n - 1)));
                left.SetPosition(i, p + Vector3.left * 0.28f);
                right.SetPosition(i, p + Vector3.right * 0.28f);
            }
            for (var i = 0; i < ties.Length; i++)
            {
                var s = Mathf.Lerp(from, to, (i + 0.5f) / ties.Length);
                var p = PitchAt(s);
                var ahead = PitchAt(Mathf.Min(1f, s + 0.004f)) - p;
                ties[i].position = p;
                ties[i].rotation = ahead.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(ahead.normalized, Vector3.right) * Quaternion.Euler(0, 0, 90) : Quaternion.identity;
            }
        }

        float _flipT = 99f;
        const float FlipSec = 0.35f;

        /// <summary>Phonyball: at the switch a playing card flips over the ball, face to back.</summary>
        void CardFlip(bool on)
        {
            var card = Part("card-flip", "Card", () =>
            {
                var go = new GameObject("Card");
                Look.Prim(PrimitiveType.Cube, "Face", go.transform, new Vector3(0, 0, -0.03f), new Vector3(1.1f, 1.5f, 0.04f),
                    Look.Unlit(new Color(0.98f, 0.96f, 0.9f)));
                Look.Prim(PrimitiveType.Cube, "Back", go.transform, new Vector3(0, 0, 0.03f), new Vector3(1.1f, 1.5f, 0.04f),
                    Look.Unlit(new Color(0.78f, 0.12f, 0.2f)));
                Look.Prim(PrimitiveType.Cube, "Pip", go.transform, new Vector3(0, 0, -0.06f), new Vector3(0.36f, 0.36f, 0.02f),
                    Look.Unlit(new Color(0.78f, 0.12f, 0.2f))).transform.localRotation = Quaternion.Euler(0, 0, 45);
                return go;
            });
            _flipT += _f.Dt;
            on &= _flipT < FlipSec;
            card.gameObject.SetActive(on);
            if (!on) return;
            var k = _flipT / FlipSec;
            card.position = _f.BallAt + Vector3.up * 1.1f;
            card.rotation = Quaternion.Euler(0, 180f * Mathf.SmoothStep(0, 1, k), 0);
            card.localScale = Vector3.one * (1f - 0.3f * k);
        }

        /// <summary>Vine Swing: the vine hangs from the pendulum's pivot to the ball, leaves along it.</summary>
        void Vine(bool on)
        {
            var vine = Line("vine", "Vine", new Color(0.25f, 0.62f, 0.2f, 1f), new Color(0.35f, 0.75f, 0.25f, 1f), 0.14f, 0.1f);
            var leaves = Many("vine", 4, i => Look.Prim(PrimitiveType.Sphere, "Leaf" + i, null, Vector3.zero,
                new Vector3(0.5f, 0.12f, 0.3f), Look.Unlit(new Color(0.3f, 0.78f, 0.3f))));
            var pivot = _f.Pitch == null ? null
                : PitchFlight.Pivot(_f.Pitch, _f.PitchU, _f.Rules, _f.StarPitch, _f.Skills, From());
            on &= pivot.HasValue;
            Visible(vine, on);
            if (!Visible(leaves, on)) return;
            var top = new Vector3((float)pivot.Value.X, (float)pivot.Value.Y, (float)pivot.Value.Z);
            var ball = PitchAt(_f.PitchU);
            vine.positionCount = 2;
            vine.SetPosition(0, top);
            vine.SetPosition(1, ball);
            for (var i = 0; i < leaves.Length; i++)
            {
                leaves[i].position = Vector3.Lerp(top, ball, (i + 1f) / (leaves.Length + 1f)) + Vector3.right * (i % 2 == 0 ? 0.18f : -0.18f);
                leaves[i].rotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 30f : -30f);
            }
        }

        float _clangT = 99f;

        /// <summary>Anvil: the ball glows hot, then at the turn it clangs (a ring of sparks) and goes cold iron to the plate.</summary>
        void Anvil(bool on)
        {
            var ring = Part("anvil", "Clang", () => Look.Torus("Clang", null, 0.6f, 0.07f, Look.Unlit(new Color(1f, 0.85f, 0.5f))));
            _clangT += _f.Dt;
            var drop = _f.Skills?.Pitch(_f.StarPitch)?.Drop;
            on &= drop != null;
            if (_f.Ball != null && _anvilTinted != on)
            {
                _anvilTinted = on;
                if (!on) _f.Ball.Tint(null);
            }
            if (on && _f.Ball != null)
            {
                var iron = drop.Turned(_f.PitchU);
                _f.Ball.Tint(iron ? new Color(0.3f, 0.32f, 0.36f) : Colors.EmberFire, iron ? 0f : 3.2f);
            }
            var flash = on && _clangT < 0.25f;
            ring.gameObject.SetActive(flash);
            if (!flash) return;
            ring.position = _f.BallAt;
            ring.rotation = Quaternion.Euler(90f, 0, 0);
            ring.localScale = Vector3.one * (1f + 5f * _clangT);
        }

        bool _anvilTinted;

        /// <summary>Mirage: heat shimmer where the ball vanished and, for a beat, where it comes back. The shadow is the ball's own.</summary>
        void Shimmer(bool on)
        {
            var blobs = Many("shimmer", 6, i => Look.Prim(PrimitiveType.Sphere, "Haze" + i, null, Vector3.zero,
                new Vector3(0.9f, 0.5f, 0.9f), Glass(new Color(1f, 0.93f, 0.78f, 0.28f))));
            var vanish = _f.Skills?.Pitch(_pitchId)?.Vanish;
            _shimmerT += _f.Dt;
            on &= vanish != null && _f.Pitch != null;
            var veiled = on && _f.PitchU >= vanish.From && _f.PitchU < vanish.To;
            var back = on && _shimmerT < ShimmerSec;
            if (!Visible(blobs, veiled || back)) return;
            var at = PitchAt(veiled ? (float)vanish.From : (float)vanish.To);
            for (var i = 0; i < blobs.Length; i++)
            {
                var a = _t * 3.1f + i * 1.05f;
                blobs[i].position = at + new Vector3(Mathf.Cos(a) * 0.35f, Mathf.Sin(_t * 5f + i) * 0.3f, Mathf.Sin(a) * 0.35f);
                var wob = 0.7f + 0.3f * Mathf.Sin(_t * 11f + i * 2f);
                blobs[i].localScale = new Vector3(0.9f * wob, 0.5f / wob, 0.9f * wob) * (back && !veiled ? 1f - _shimmerT / ShimmerSec : 1f);
            }
        }

        float _shimmerT = 99f;
        const float ShimmerSec = 0.3f;

        /// <summary>Cable Car: a cable runs from the station to the plate; the car stands at the station through the stop.</summary>
        void CableLine(bool on)
        {
            var cable = Line("cable-line", "Cable", new Color(0.2f, 0.2f, 0.22f, 1f), new Color(0.2f, 0.2f, 0.22f, 1f), 0.06f, 0.06f);
            var car = Part("cable-line", "Car", () =>
            {
                var go = new GameObject("Car");
                Look.Prim(PrimitiveType.Cube, "Cabin", go.transform, new Vector3(0, -0.55f, 0), new Vector3(0.9f, 0.6f, 0.6f),
                    Look.Unlit(new Color(0.85f, 0.16f, 0.14f)));
                Look.Prim(PrimitiveType.Cube, "Hanger", go.transform, new Vector3(0, -0.12f, 0), new Vector3(0.06f, 0.3f, 0.06f),
                    Look.Unlit(new Color(0.2f, 0.2f, 0.22f)));
                return go;
            });
            var hitch = _f.Skills?.Pitch(_f.StarPitch)?.Hitch;
            on &= hitch != null && _f.PitchU >= hitch.At - 0.15f;
            Visible(cable, on);
            car.gameObject.SetActive(on);
            if (!on) return;
            var station = PitchAt((float)hitch.At);
            const int n = 16;
            cable.positionCount = n;
            // The line the car runs down: the ball's own path from the station to the plate, a little above it.
            var crossing = PitchAt(1f);
            for (var i = 0; i < n; i++)
                cable.SetPosition(i, Vector3.Lerp(station, crossing, i / (float)(n - 1)) + Vector3.up * 0.35f);
            car.position = PitchAt(_f.PitchU) + Vector3.up * 0.35f;
        }

        readonly float[] _splashT = { 99f, 99f };
        readonly Vector3[] _splashAt = new Vector3[2];
        int _splashNext;
        const float SplashSec = 0.45f;

        /// <summary>Skipping Stone: a splash ring spreads on the dirt at each skip.</summary>
        void SplashRing(bool on)
        {
            for (var k = 0; k < 2; k++)
            {
                var ring = Part("splash-ring", "Splash" + k, () => Look.Torus("Splash", null, 1f, 0.06f, Glass(new Color(0.55f, 0.85f, 1f, 0.8f))));
                _splashT[k] += _f.Dt;
                var show = on && _splashT[k] < SplashSec;
                ring.gameObject.SetActive(show);
                if (!show) continue;
                var q = _splashT[k] / SplashSec;
                ring.position = new Vector3(_splashAt[k].x, 0.06f, _splashAt[k].z);
                ring.localScale = new Vector3(0.3f + 2f * q, 1f, 0.3f + 2f * q);
            }
        }

        /// <summary>A pitch beat (<see cref="SpecialTells.PitchBeats"/>): the tells that mark an instant start their burst here.</summary>
        void Beat(double u)
        {
            if (_beats.TryGetValue(_pitchTell, out var burst)) burst(u);
        }

        void SplashAt(double u)
        {
            _splashT[_splashNext] = 0f;
            _splashAt[_splashNext] = PitchAt((float)u);
            _splashNext = (_splashNext + 1) % 2;
        }

        void ShimmerBack(double u)
        {
            var vanish = _f.Skills?.Pitch(_f.StarPitch)?.Vanish;
            if (vanish != null && Math.Abs(u - vanish.To) < 1e-9) _shimmerT = 0f;
        }

        /// <summary>Undertow: waves wash out across the ring on home, out to its rim, while the ring is live.</summary>
        void WaveRing(bool on)
        {
            var rim = Line("wave-ring", "Rim", new Color(0.2f, 0.6f, 0.85f, 0.9f), new Color(0.2f, 0.6f, 0.85f, 0.9f), 0.12f, 0.12f);
            var waves = new LineRenderer[3];
            for (var k = 0; k < waves.Length; k++)
                waves[k] = Line("wave-ring", "Wave" + k, new Color(0.75f, 0.95f, 1f, 0.8f), new Color(0.75f, 0.95f, 1f, 0.8f), 0.1f, 0.1f);
            var ring = on ? Volume(PitchUndertow.Type) : null;
            on &= ring != null;
            Visible(rim, on);
            foreach (var w in waves) Visible(w, on);
            if (!on) return;
            var r = (float)ring.RadiusFt;
            var c = new Vector3((float)ring.X, 0.1f, (float)ring.Z);
            Circle(rim, c, r);
            var phase = (float)(_f.Live.ElapsedSeconds / 0.8);
            for (var k = 0; k < waves.Length; k++)
            {
                var q = (phase + k / (float)waves.Length) % 1f;
                Circle(waves[k], c, r * q);
                var col = new Color(0.75f, 0.95f, 1f, 0.9f * (1f - q));
                waves[k].startColor = waves[k].endColor = col;
            }
        }

        // ---------------------------------------------------------------- plate tells

        /// <summary>Sparkler: sparks ring the swing's Perfect ring on the contact oval while the star swing is held.</summary>
        void SparkRing(bool on)
        {
            var sparks = Many("spark-ring", 18, i => Look.Prim(PrimitiveType.Sphere, "Spark" + i, null, Vector3.zero,
                Vector3.one * 0.1f, Look.Unlit(Color.Lerp(Colors.Gold, Color.white, (i % 2) * 0.4f))));
            if (!Visible(sparks, on && _f.OvalShown)) return;
            var mul = StarSkills.SwingPerfectRingMul(_f.ArmedSwing, _f.Skills);
            var ring = (float)SweetSpot.PerfectFraction(_f.Rules, mul);
            var pts = SweetSpot.Outline(_f.Oval, sparks.Length);
            var center = OvalCenter();
            for (var i = 0; i < sparks.Length; i++)
            {
                var p = pts[(i + (int)(_t * 12f)) % pts.Count];
                sparks[i].position = center + new Vector3((float)p.X, (float)p.Y, 0) * ring;
                sparks[i].localScale = Vector3.one * (0.07f + 0.06f * Mathf.Abs(Mathf.Sin(_t * 17f + i * 1.3f)));
            }
        }

        /// <summary>Driftwood Reach: the oval the plate draws is the tall one; a driftwood staff runs top to bottom through it.</summary>
        void TallOval(bool on)
        {
            var staff = Line("tall-oval", "Staff", new Color(0.55f, 0.4f, 0.26f, 1f), new Color(0.72f, 0.58f, 0.4f, 1f), 0.09f, 0.09f);
            var caps = Many("tall-oval", 2, i => Look.Prim(PrimitiveType.Sphere, "Knot" + i, null, Vector3.zero,
                Vector3.one * 0.16f, Look.Unlit(new Color(0.5f, 0.36f, 0.22f))));
            on &= _f.OvalShown;
            Visible(staff, on);
            if (!Visible(caps, on)) return;
            var c = OvalCenter();
            var h = (float)_f.Oval.HalfHeightFt;
            staff.positionCount = 2;
            staff.SetPosition(0, c + Vector3.up * h);
            staff.SetPosition(1, c - Vector3.up * h);
            caps[0].position = c + Vector3.up * h;
            caps[1].position = c - Vector3.up * h;
        }

        // ---------------------------------------------------------------- live-ball tells

        /// <summary>Follow Spot: the rink's spot drops a cone of light on the one fielder the pause holds, until he may move.</summary>
        void FollowSpot(bool on)
        {
            var cone = Part("follow-spot", "Cone", () => Cone(3.2f, 26f, Glass(new Color(1f, 0.97f, 0.82f, 0.22f))));
            var pool = Part("follow-spot", "Pool", () => Look.Prim(PrimitiveType.Cylinder, "Pool", null, Vector3.zero,
                new Vector3(6.4f, 0.03f, 6.4f), Glass(new Color(1f, 0.97f, 0.8f, 0.45f))));
            FielderDazzled held = null;
            if (on && _f.Live != null)
                foreach (var fact in _f.Live.FactsThisPlay)
                    if (fact is FielderDazzled d && d.UntilT > _f.Live.ElapsedSeconds) held = d;
            var at = Vector3.zero;
            on &= held != null && _f.Gloves != null && GloveAt(held.GloveId, out at);
            cone.gameObject.SetActive(on);
            pool.gameObject.SetActive(on);
            if (!on) return;
            cone.position = at;
            pool.position = at + Vector3.up * 0.04f;
        }

        /// <summary>Spinning Top: the stalled grounder spins in place — a whirling top — and whistles as it starts.</summary>
        void SpinningTop(bool on)
        {
            var top = Part("spinning-top", "Top", () =>
            {
                var go = new GameObject("Top");
                Look.Prim(PrimitiveType.Cylinder, "Disc", go.transform, new Vector3(0, 0.25f, 0), new Vector3(1.1f, 0.05f, 1.1f),
                    Look.Unlit(new Color(0.35f, 0.85f, 0.4f)));
                Look.Prim(PrimitiveType.Cube, "StripeA", go.transform, new Vector3(0, 0.29f, 0), new Vector3(1.1f, 0.03f, 0.14f),
                    Look.Unlit(new Color(1f, 0.85f, 0.2f)));
                Look.Prim(PrimitiveType.Cube, "StripeB", go.transform, new Vector3(0, 0.29f, 0), new Vector3(0.14f, 0.03f, 1.1f),
                    Look.Unlit(new Color(0.95f, 0.3f, 0.6f)));
                Look.Prim(PrimitiveType.Cylinder, "Spindle", go.transform, new Vector3(0, 0.55f, 0), new Vector3(0.08f, 0.3f, 0.08f),
                    Look.Unlit(new Color(0.95f, 0.95f, 0.9f)));
                return go;
            });
            FirstHopKicked kick = null;
            if (on && _f.Live != null)
                foreach (var fact in _f.Live.FactsThisPlay)
                    if (fact is FirstHopKicked k && k.StallSec > 0) kick = k;
            if (kick != null && FactIsNew(kick)) Cue(kick.SwingId);
            var now = _f.Live?.ElapsedSeconds ?? 0;
            on &= kick != null && now >= kick.T && now < kick.T + kick.StallSec;
            top.gameObject.SetActive(on);
            if (!on) return;
            top.position = new Vector3(_f.BallAt.x, 0f, _f.BallAt.z);
            top.rotation = Quaternion.Euler(6f * Mathf.Sin(_t * 7f), _t * 1440f, 6f * Mathf.Cos(_t * 7f));
        }

        /// <summary>Double Deal: a card-back ball flies beside the real one, off its line, until the apex; then it is gone.</summary>
        void CardDecoy(bool on)
        {
            var decoy = Part("card-decoy", "Decoy", () =>
            {
                var go = Look.Prim(PrimitiveType.Sphere, "Decoy", null, Vector3.zero, Vector3.one * 0.9f,
                    Look.Unlit(new Color(0.78f, 0.12f, 0.2f)));
                Look.Prim(PrimitiveType.Cube, "DiamondA", go.transform, new Vector3(0, 0, 0.45f), new Vector3(0.45f, 0.45f, 0.08f),
                    Look.Unlit(new Color(0.98f, 0.96f, 0.9f))).transform.localRotation = Quaternion.Euler(0, 0, 45);
                Look.Prim(PrimitiveType.Cube, "DiamondB", go.transform, new Vector3(0, 0, -0.45f), new Vector3(0.45f, 0.45f, 0.08f),
                    Look.Unlit(new Color(0.98f, 0.96f, 0.9f))).transform.localRotation = Quaternion.Euler(0, 0, 45);
                return go;
            });
            var path = _f.Live?.Path;
            var now = _f.Live?.ElapsedSeconds ?? 0;
            on &= path != null && path.Count > 1 && now < SpecialTells.ApexT(path);
            decoy.gameObject.SetActive(on);
            if (!on) return;
            // Beside the real ball, square to its line toward the middle of the field: a second ball the eye can follow.
            var land = path[path.Count - 1];
            var line = new Vector3((float)(land.X - path[0].X), 0, (float)(land.Z - path[0].Z));
            var side = line.sqrMagnitude > 1e-4f ? Vector3.Cross(Vector3.up, line.normalized) : Vector3.right;
            if (side.x * line.x > 0) side = -side;
            decoy.position = _f.BallAt + side * 3f;
            decoy.rotation = Quaternion.Euler(_t * 400f, _t * 90f, 0);
        }

        /// <summary>Lightning Liner: a bolt crackles along the jagged flight until the ball is back on its line; then the thunder.</summary>
        void BoltTrail(bool on)
        {
            var bolt = Line("bolt-trail", "Bolt", new Color(1f, 1f, 0.75f, 1f), new Color(1f, 0.9f, 0.3f, 0.2f), 0.24f, 0.08f);
            var hit = _f.Live?.Hit;
            var path = _f.Live?.Path;
            on &= hit?.Jag != null && path != null && path.Count > 1;
            if (!on)
            {
                Visible(bolt, false);
                return;
            }
            var now = _f.Live.ElapsedSeconds;
            var done = SpecialTells.JagDoneT(path, hit.Jag);
            if (!_thundered && SpecialTells.Crossed(_prevElapsed, now, done))
            {
                _thundered = true;
                Cue(hit.StarSwingUsed);
            }
            if (!Visible(bolt, now <= done + 0.25)) return;
            const int n = 28;
            bolt.positionCount = n;
            var end = Math.Min(now, done);
            for (var i = 0; i < n; i++)
            {
                var t = end * i / (n - 1);
                var p = BallFlight.PointAt(path, t, _f.Rules);
                var zig = (i % 2 == 0 ? 1f : -1f) * 0.35f * (i == 0 || i == n - 1 ? 0f : 1f);
                bolt.SetPosition(i, new Vector3((float)p.X, (float)p.Y + zig, (float)p.Z));
            }
            bolt.widthMultiplier = 0.7f + 0.3f * Mathf.Abs(Mathf.Sin(_t * 31f));
        }

        bool _thundered;

        /// <summary>Hot Iron: the ball glows and steams while it is molten, hissing until it cools; a dropped ball puffs steam.</summary>
        void Molten(bool on)
        {
            var steam = Many("molten", 6, i => Look.Prim(PrimitiveType.Sphere, "Steam" + i, null, Vector3.zero,
                Vector3.one * 0.3f, Glass(new Color(1f, 1f, 1f, 0.45f))));
            var live = _f.Live;
            var left = on && live != null ? live.MoltenLeftSec : 0;
            var hot = left > 0;
            if (_f.Ball != null && hot != _moltenTinted)
            {
                _moltenTinted = hot;
                if (!hot) _f.Ball.Tint(null);
            }
            if (hot && _f.Ball != null)
            {
                var share = (float)Math.Min(1, left / 2.0);
                _f.Ball.Tint(Color.Lerp(new Color(0.55f, 0.5f, 0.48f), Colors.EmberFire, share), 1f + 3f * share);
                if (_t - _hissAt > 0.6f || _hissAt < 0f)
                {
                    _hissAt = _t;
                    Cue(live.Hit?.StarSwingUsed);
                }
            }
            HotBallDropped dropped = null;
            if (on && live != null)
                foreach (var fact in live.FactsThisPlay)
                    if (fact is HotBallDropped d && live.ElapsedSeconds - d.T < 0.6) dropped = d;
            if (!Visible(steam, hot || dropped != null)) return;
            var at = dropped != null ? new Vector3((float)dropped.X, 0.3f, (float)dropped.Z) : _f.BallAt;
            for (var i = 0; i < steam.Length; i++)
            {
                var rise = (i * 0.17f + _t * 1.8f) % 1f;
                var a = i * 1.1f + _t * 2f;
                steam[i].position = at + new Vector3(Mathf.Cos(a) * 0.3f, 0.2f + rise * 1.2f, Mathf.Sin(a) * 0.3f);
                steam[i].localScale = Vector3.one * (0.2f + 0.35f * rise);
            }
        }

        bool _moltenTinted;

        /// <summary>Dust Bowl: dust swirls over the bowl the grounder raised, to its rim, until it settles.</summary>
        void DustSwirl(bool on)
        {
            var disc = Part("dust-swirl", "Bowl", () => Look.Prim(PrimitiveType.Cylinder, "Bowl", null, Vector3.zero, Vector3.one,
                Glass(new Color(0.72f, 0.55f, 0.35f, 0.35f))));
            var motes = Many("dust-swirl", 14, i => Look.Prim(PrimitiveType.Sphere, "Mote" + i, null, Vector3.zero,
                Vector3.one * 0.5f, Glass(new Color(0.8f, 0.64f, 0.42f, 0.55f))));
            var bowl = on ? Volume(SwingDustBowl.Type) : null;
            on &= bowl != null;
            disc.gameObject.SetActive(on);
            if (!Visible(motes, on)) return;
            var r = (float)bowl.RadiusFt;
            var c = new Vector3((float)bowl.X, 0f, (float)bowl.Z);
            disc.position = c + Vector3.up * 0.05f;
            disc.localScale = new Vector3(r * 2f, 0.03f, r * 2f);
            for (var i = 0; i < motes.Length; i++)
            {
                var rr = r * (0.3f + 0.7f * ((i * 0.37f) % 1f));
                var a = _t * (2.4f - rr / r) + i * 0.9f;
                motes[i].position = c + new Vector3(Mathf.Cos(a) * rr, 0.3f + 0.9f * Mathf.Abs(Mathf.Sin(_t * 2f + i)), Mathf.Sin(a) * rr);
                motes[i].localScale = Vector3.one * (0.35f + 0.3f * Mathf.Abs(Mathf.Sin(_t * 3f + i)));
            }
        }

        float _flurryT = 99f;
        Vector3 _flurryAt;
        const float FlurrySec = 0.9f;

        /// <summary>Summit Gust: snow bursts out of the fly at its apex, where the gust takes it.</summary>
        void SnowFlurry(bool on)
        {
            var flakes = Many("snow-flurry", 16, i => Look.Prim(PrimitiveType.Sphere, "Flake" + i, null, Vector3.zero,
                Vector3.one * 0.3f, Look.Unlit(new Color(0.96f, 0.98f, 1f))));
            var path = _f.Live?.Path;
            if (on && path != null && path.Count > 1)
            {
                var apex = SpecialTells.ApexT(path);
                var p = BallFlight.PointAt(path, apex, _f.Rules);
                if (p.Y > 8 && SpecialTells.Crossed(_prevElapsed, _f.Live.ElapsedSeconds, apex))
                {
                    _flurryT = 0f;
                    _flurryAt = new Vector3((float)p.X, (float)p.Y, (float)p.Z);
                }
            }
            _flurryT += _f.Dt;
            if (!Visible(flakes, on && _flurryT < FlurrySec)) return;
            var q = _flurryT / FlurrySec;
            for (var i = 0; i < flakes.Length; i++)
            {
                var dir = new Vector3(Hash(i, 4), Hash(i, 5) * 0.6f, Hash(i, 6)).normalized;
                flakes[i].position = _flurryAt + dir * (7f * q) + Vector3.down * (2f * q * q);
                flakes[i].localScale = Vector3.one * (0.35f * (1f - q) + 0.05f);
            }
        }

        bool _croaked;

        /// <summary>Lily Hop: a lily pad flashes on the ground under the ball as it hops the glove, with a croak.</summary>
        void LilyPad(bool on)
        {
            var pad = Part("lily-pad", "Pad", () =>
            {
                var go = new GameObject("Pad");
                Look.Prim(PrimitiveType.Cylinder, "Leaf", go.transform, Vector3.zero, new Vector3(2.6f, 0.03f, 2.6f),
                    Look.Unlit(new Color(0.3f, 0.7f, 0.3f)));
                Look.Prim(PrimitiveType.Sphere, "Lotus", go.transform, new Vector3(0.5f, 0.12f, 0.3f), new Vector3(0.45f, 0.3f, 0.45f),
                    Look.Unlit(new Color(1f, 0.6f, 0.78f)));
                return go;
            });
            GloveHopped hop = null;
            if (on && _f.Live != null)
                foreach (var fact in _f.Live.FactsThisPlay)
                    if (fact is GloveHopped h) hop = h;
            var near = false;
            if (hop != null)
            {
                var d = new Vector2(_f.BallAt.x - (float)hop.X, _f.BallAt.z - (float)hop.Z).magnitude;
                near = d < 7f && _f.Live.ElapsedSeconds >= hop.T && _f.Live.ElapsedSeconds < hop.T + 2.0;
            }
            if (near && !_croaked)
            {
                _croaked = true;
                Cue(hop.SwingId);
            }
            pad.gameObject.SetActive(near);
            if (!near) return;
            pad.position = new Vector3(_f.BallAt.x, 0.05f, _f.BallAt.z);
            pad.localScale = Vector3.one * (0.9f + 0.1f * Mathf.Sin(_t * 12f));
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>The star pitch's own point at time fraction <paramref name="u"/> of its flight (<see cref="PitchFlight.Point"/>).</summary>
        Vector3 PitchAt(float u)
        {
            if (_f.Pitch == null || _f.Rules == null) return _f.BallAt;
            var p = PitchFlight.Point(_f.Pitch, Mathf.Clamp01(u), _f.Rules, _pitchId.Length > 0 ? _pitchId : _f.StarPitch, From(),
                _f.Skills, _f.PitchDur);
            return new Vector3((float)p.X, (float)p.Y, (float)p.Z);
        }

        (double X, double Y, double Z) From() => (_f.ReleaseFrom.x, _f.ReleaseFrom.y, _f.ReleaseFrom.z);

        Vector3 OvalCenter() => new Vector3((float)_f.Oval.CenterX, (float)_f.Oval.CenterY, (float)StrikeZoneGeometry.PlateZ);

        bool GloveAt(string pos, out Vector3 at)
        {
            at = Vector3.zero;
            if (string.IsNullOrEmpty(pos) || !_f.Gloves.TryGetValue(pos, out var g)) return false;
            at = new Vector3((float)g.X, 0f, (float)g.Z);
            return true;
        }

        /// <summary>The live status volume of this type (a star's ring or bowl), while it is live; null otherwise.</summary>
        StatusVolume Volume(string type)
        {
            var live = _f.Live;
            if (live == null) return null;
            foreach (var v in live.StatusVolumes)
                if (v.Type == type && v.ActiveAt(live.ElapsedSeconds)) return v;
            return null;
        }

        bool FactIsNew(LiveFact fact)
        {
            var facts = _f.Live.FactsThisPlay;
            for (var i = _factsSeen; i < facts.Count; i++)
                if (ReferenceEquals(facts[i], fact)) return true;
            return false;
        }

        /// <summary>Sound the tell cue a special's slot names (an audio slot; a generated stand-in until its wav is authored).</summary>
        void Cue(string specialId)
        {
            var cue = ArtBinder.CueOf(specialId);
            if (cue.Length > 0) _f.Audio?.Cue(cue);
        }

        void Circle(LineRenderer line, Vector3 c, float r)
        {
            const int n = 48;
            line.positionCount = n;
            line.loop = true;
            for (var i = 0; i < n; i++)
            {
                var a = i * Mathf.PI * 2f / n;
                line.SetPosition(i, c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
            }
        }

        /// <summary>The slot's group: its prefab when an Art session has dropped one, else an empty anchor.</summary>
        Transform Group(string id)
        {
            if (_groups.TryGetValue(id, out var existing) && existing != null) return existing;
            var go = new GameObject(id);
            go.transform.SetParent(_root, false);
            var prefab = ArtBinder.LoadVfx(id);
            if (prefab != null)
            {
                var inst = UnityEngine.Object.Instantiate(prefab, go.transform);
                inst.name = id;
            }
            go.SetActive(false);
            _groups[id] = go.transform;
            return go.transform;
        }

        /// <summary>One procedural part of a tell, built once under the tell's own holder.</summary>
        Transform Part(string tell, string name, Func<GameObject> build)
        {
            var key = tell + "/" + name;
            if (_parts.TryGetValue(key, out var t) && t != null) return t;
            var go = build();
            go.name = name;
            go.transform.SetParent(Holder(tell), true);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            go.SetActive(false);
            _parts[key] = go.transform;
            return go.transform;
        }

        Transform[] Many(string tell, int count, Func<int, GameObject> build)
        {
            var list = new Transform[count];
            for (var i = 0; i < count; i++)
            {
                var k = i;
                list[i] = Part(tell, "Bit" + i, () => build(k));
            }
            return list;
        }

        LineRenderer Line(string tell, string name, Color start, Color end, float startWidth, float endWidth)
        {
            var t = Part(tell, name, () =>
            {
                var go = new GameObject(name);
                var line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.sharedMaterial = _lineMat;
                line.startColor = start;
                line.endColor = end;
                line.startWidth = startWidth;
                line.endWidth = endWidth;
                line.numCapVertices = 3;
                line.numCornerVertices = 2;
                line.alignment = LineAlignment.View;
                line.positionCount = 0;
                return go;
            });
            return t.GetComponent<LineRenderer>();
        }

        Transform Holder(string tell)
        {
            var key = "@" + tell;
            if (_parts.TryGetValue(key, out var h) && h != null) return h;
            var go = new GameObject("Tell " + tell);
            go.transform.SetParent(_root, false);
            _parts[key] = go.transform;
            return go.transform;
        }

        static bool Visible(Transform[] parts, bool on)
        {
            foreach (var p in parts)
                if (p != null) p.gameObject.SetActive(on);
            return on;
        }

        static bool Visible(LineRenderer line, bool on)
        {
            if (line != null) line.gameObject.SetActive(on);
            return on;
        }

        /// <summary>A cone standing on its base (radius <paramref name="r"/> on the ground, apex <paramref name="h"/> above): a spotlight's beam.</summary>
        static GameObject Cone(float r, float h, Material mat)
        {
            const int n = 32;
            var verts = new Vector3[n + 1];
            var tris = new int[n * 6];
            verts[n] = new Vector3(0, h, 0);
            for (var i = 0; i < n; i++)
            {
                var a = i * Mathf.PI * 2f / n;
                verts[i] = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            }
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                // Both faces, so the beam reads from inside and outside.
                tris[i * 6] = i; tris[i * 6 + 1] = n; tris[i * 6 + 2] = j;
                tris[i * 6 + 3] = j; tris[i * 6 + 4] = n; tris[i * 6 + 5] = i;
            }
            return Look.Solid("Cone", null, verts, tris, mat);
        }

        /// <summary>A see-through unlit colour: alpha blended, no depth write, no shadow.</summary>
        static Material Glass(Color c)
        {
            var m = Look.Unlit(c);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
        }

        static Material LineMaterial()
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Look.LitShader;
            var m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            m.color = Color.white;
            m.renderQueue = 3000;
            return m;
        }

        static float Hash(int i, int salt)
        {
            var n = (uint)(i * 374761393 + salt * 668265263);
            n = (n ^ (n >> 13)) * 1274126177u;
            return ((n & 0xFFFF) / 32768f) - 1f;
        }

        void HideAll()
        {
            foreach (var kv in _groups)
                if (kv.Value != null) kv.Value.gameObject.SetActive(false);
        }

        void Show(string id, bool on)
        {
            var g = Group(id);
            if (g != null) g.gameObject.SetActive(on);
        }
    }
}
