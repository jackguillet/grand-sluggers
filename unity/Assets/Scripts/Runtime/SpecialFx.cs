using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// HUD-off specials: change the ball or the field for ~2 seconds, then baseball resumes.
    /// Groups are catalog VFX ids. Missing Unity prefabs keep the procedural stand-in.
    /// No full-screen paint, no input invert, no invisible ball.
    /// </summary>
    public sealed class SpecialFx : MonoBehaviour
    {
        static readonly string[] EventIds =
        {
            "heatball", "charmball", "prismball", "phonyball", "caskball", "skullball",
            "heat-swing", "heart-swing", "shell-swing", "phony-swing", "cask-swing", "furnace",
            "buddy-flash", "throw-trail-good", "throw-trail-bad"
        };

        Transform _root;
        readonly Dictionary<string, Transform> _groups = new Dictionary<string, Transform>(System.StringComparer.OrdinalIgnoreCase);
        Transform _decoy;
        Transform _swingDecoy;
        Transform _barrel;
        Transform _skull;
        readonly Transform[] _hearts = new Transform[8];
        readonly Transform[] _swingHearts = new Transform[8];
        readonly Transform[] _bits = new Transform[10];
        readonly Transform[] _shellBits = new Transform[10];
        readonly Transform[] _embers = new Transform[12];
        readonly Transform[] _swingEmbers = new Transform[12];
        readonly Transform[] _furnaceEmbers = new Transform[12];
        readonly Transform[] _prism = new Transform[3];
        LineRenderer _laser;
        LineRenderer _tongue;
        LineRenderer _throw;
        LineRenderer _throwBad;
        Transform _burn;
        Transform _heatBurn;
        Transform _heatCore;
        Transform _heatShell;
        Transform _charmCore;
        Transform _charmShell;
        Transform _heartCore;
        Transform _heartShell;
        Transform _prismCore;
        Transform _prismShell;
        Transform _crack;
        readonly Transform[] _sparkles = new Transform[6];
        readonly Transform[] _furnaceRim = new Transform[8];
        Transform _buddyFlash;
        float _t;
        float _pitchLinger;
        float _swingLinger;
        float _throwLinger;
        float _throwDur;
        float _throwArc;
        float _throwWobble;
        bool _throwGood;
        string _pitchId = "";
        string _swingId = "";
        Vector3 _decoyPos;
        Vector3 _lastBall;
        Vector3 _throwFrom;
        Vector3 _throwTo;

        public void Build(Transform parent)
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = new GameObject("SpecialFx").transform;
            _root.SetParent(parent, false);
            _groups.Clear();
            foreach (var id in EventIds)
                _groups[id] = Group(id);

            _decoy = Ballish(Group("phonyball"), "Decoy", new Color(0.98f, 0.94f, 0.55f, 0.92f), 1.55f);
            FakeFace(_decoy);
            _swingDecoy = Ballish(Group("phony-swing"), "Decoy", new Color(0.98f, 0.94f, 0.55f, 0.92f), 1.55f);
            FakeFace(_swingDecoy);
            _barrel = Barrel(Group("caskball"));
            _skull = Skull(Group("skullball"));

            var pink = Look.Unlit(new Color(1f, 0.32f, 0.58f));
            FillHearts(_hearts, Group("charmball"), pink);
            FillHearts(_swingHearts, Group("heart-swing"), pink);
            FillSparkles(_sparkles, Group("charmball"));

            var wood = Look.Lit(new Color(0.4f, 0.22f, 0.1f), smooth: 0.1f);
            FillFrags(_bits, Group("cask-swing"), wood);
            FillShell(_shellBits, Group("shell-swing"));

            var fire = Look.Unlit(Colors.EmberFire);
            FillEmbers(_embers, Group("heatball"), fire);
            FillEmbers(_swingEmbers, Group("heat-swing"), fire);
            FillEmbers(_furnaceEmbers, Group("furnace"), fire);
            _heatCore = Look.Prim(PrimitiveType.Sphere, "Core", Group("heatball"), Vector3.zero,
                Vector3.one * 0.9f, Look.Unlit(Colors.EmberFire)).transform;
            _heatShell = Look.Prim(PrimitiveType.Sphere, "Shell", Group("heatball"), Vector3.zero,
                Vector3.one * 1.35f, Look.Unlit(new Color(1f, 0.45f, 0.12f, 0.55f))).transform;
            _charmCore = Look.Prim(PrimitiveType.Sphere, "Core", Group("charmball"), Vector3.zero,
                Vector3.one * 0.88f, Look.Unlit(new Color(1f, 0.38f, 0.62f))).transform;
            _charmShell = Look.Prim(PrimitiveType.Sphere, "Shell", Group("charmball"), Vector3.zero,
                Vector3.one * 1.32f, Look.Unlit(new Color(1f, 0.55f, 0.78f, 0.5f))).transform;
            _heartCore = Look.Prim(PrimitiveType.Sphere, "Core", Group("heart-swing"), Vector3.zero,
                Vector3.one * 0.88f, Look.Unlit(new Color(1f, 0.38f, 0.62f))).transform;
            _heartShell = Look.Prim(PrimitiveType.Sphere, "Shell", Group("heart-swing"), Vector3.zero,
                Vector3.one * 1.32f, Look.Unlit(new Color(1f, 0.55f, 0.78f, 0.5f))).transform;
            _prismCore = Look.Prim(PrimitiveType.Sphere, "Core", Group("prismball"), Vector3.zero,
                Vector3.one * 0.86f, Look.Unlit(new Color(0.92f, 0.98f, 1f))).transform;
            _prismShell = Look.Prim(PrimitiveType.Sphere, "Shell", Group("prismball"), Vector3.zero,
                Vector3.one * 1.28f, Look.Unlit(new Color(0.55f, 1f, 0.85f, 0.45f))).transform;

            for (var i = 0; i < _prism.Length; i++)
                _prism[i] = Ballish(Group("prismball"), "Prism" + i, Color.HSVToRGB(i / 3f, 0.75f, 1f), 1.25f);

            _laser = Line(_root, "Laser", Colors.EmberFire, 0.35f);
            _tongue = Line(_root, "Tongue", new Color(1f, 0.4f, 0.55f), 0.28f);
            _throw = Line(Group("throw-trail-good"), "Trail", Colors.Gold, 0.42f);
            _throwBad = Line(Group("throw-trail-bad"), "Trail", new Color(0.4f, 0.3f, 0.16f), 0.26f);
            if (_throw != null) _throw.positionCount = 12;
            if (_throwBad != null) _throwBad.positionCount = 12;

            _heatBurn = Look.Prim(PrimitiveType.Cylinder, "Burn", Group("heat-swing"), Vector3.zero, new Vector3(8.2f, 0.1f, 8.2f),
                Look.Lit(Colors.EmberFire, smooth: 0.35f)).transform;
            _burn = Look.Prim(PrimitiveType.Cylinder, "Burn", Group("furnace"), Vector3.zero, new Vector3(7.4f, 0.12f, 7.4f),
                Look.Lit(Colors.EmberFire, smooth: 0.35f)).transform;
            _crack = Look.Prim(PrimitiveType.Cube, "Crack", Group("furnace"), Vector3.zero, new Vector3(8.5f, 0.14f, 0.9f),
                Look.Lit(new Color(0.12f, 0.05f, 0.04f), smooth: 0.05f)).transform;
            var rim = Look.Toon(new Color(0.22f, 0.08f, 0.04f));
            for (var i = 0; i < _furnaceRim.Length; i++)
                _furnaceRim[i] = Look.Prim(PrimitiveType.Cube, "Rim" + i, Group("furnace"), Vector3.zero,
                    new Vector3(1.6f, 0.28f, 0.55f), rim).transform;
            _buddyFlash = Look.Prim(PrimitiveType.Cylinder, "Flash", Group("buddy-flash"), Vector3.zero, new Vector3(3.6f, 0.12f, 3.6f),
                Look.Unlit(Colors.Gold)).transform;
            HideAll();
        }

        public void Tick(
            float dt,
            Vector3 ball,
            bool flight,
            bool inPlay,
            bool starPitch,
            string starPitchId,
            string starSwingId,
            Vector3 tongueFrom,
            Vector3 laserTo,
            bool showTongue,
            bool showLaser,
            bool showBurn,
            bool showFrags)
        {
            if (_root == null) return;
            _t += dt;
            _lastBall = ball;

            if (flight && starPitch && Known(starPitchId))
            {
                _pitchId = starPitchId;
                _pitchLinger = (float)StarSkills.SpectacleSeconds(starPitchId);
            }
            if (Known(starSwingId))
            {
                _swingId = starSwingId;
                _swingLinger = (float)StarSkills.SpectacleSeconds(starSwingId);
            }

            _pitchLinger = Mathf.Max(0, _pitchLinger - dt);
            _swingLinger = Mathf.Max(0, _swingLinger - dt);

            var pitchOn = _pitchLinger > 0 && Known(_pitchId);
            var swingOn = _swingLinger > 0 && Known(_swingId);

            Show("heatball", pitchOn && IdIs(_pitchId, "heatball"));
            Show("charmball", pitchOn && IdIs(_pitchId, "charmball"));
            Show("prismball", pitchOn && IdIs(_pitchId, "prismball"));
            Show("phonyball", pitchOn && IdIs(_pitchId, "phonyball"));
            Show("caskball", pitchOn && IdIs(_pitchId, "caskball"));
            Show("skullball", pitchOn && IdIs(_pitchId, "skullball"));
            Show("heat-swing", swingOn && IdIs(_swingId, "heat-swing"));
            Show("heart-swing", swingOn && IdIs(_swingId, "heart-swing"));
            Show("shell-swing", swingOn && IdIs(_swingId, "shell-swing"));
            Show("phony-swing", swingOn && IdIs(_swingId, "phony-swing"));
            Show("cask-swing", showFrags || swingOn && IdIs(_swingId, "cask-swing"));
            Show("furnace", swingOn && IdIs(_swingId, "furnace"));

            PlaceDecoy(_decoy, pitchOn && IdIs(_pitchId, "phonyball"), ball, dt);
            PlaceDecoy(_swingDecoy, swingOn && IdIs(_swingId, "phony-swing"), ball, dt);
            PlaceBarrel(pitchOn && IdIs(_pitchId, "caskball"), ball);
            PlaceSkull(pitchOn && IdIs(_pitchId, "skullball"), ball);
            Prism(pitchOn && IdIs(_pitchId, "prismball"), ball);
            Embers(_embers, pitchOn && IdIs(_pitchId, "heatball"), ball, true);
            HeatCore(pitchOn && IdIs(_pitchId, "heatball"), ball);
            CharmCore(pitchOn && IdIs(_pitchId, "charmball"), ball);
            HeartCore(swingOn && IdIs(_swingId, "heart-swing"), ball);
            Sparkles(_sparkles, pitchOn && IdIs(_pitchId, "charmball"), ball);
            Embers(_swingEmbers, swingOn && IdIs(_swingId, "heat-swing"), ball, false);
            Embers(_furnaceEmbers, swingOn && IdIs(_swingId, "furnace"), ball, false);
            Hearts(_hearts, pitchOn && IdIs(_pitchId, "charmball"), ball);
            Hearts(_swingHearts, swingOn && IdIs(_swingId, "heart-swing"), ball);
            Fragments(_bits, showFrags || swingOn && IdIs(_swingId, "cask-swing"), ball);
            Fragments(_shellBits, swingOn && IdIs(_swingId, "shell-swing"), ball);
            Burn(_heatBurn, showBurn || swingOn && IdIs(_swingId, "heat-swing"), ball);
            Burn(_burn, showBurn || swingOn && IdIs(_swingId, "furnace"), ball);
            FurnaceRim(swingOn && IdIs(_swingId, "furnace"), ball);
            Crack(swingOn && IdIs(_swingId, "furnace"), ball);
            Beam(_tongue, showTongue, tongueFrom, ball);
            Beam(_laser, showLaser, tongueFrom, laserTo == Vector3.zero ? ball : laserTo);
            DrawThrow(dt);
        }

        public void ArmThrow(Vector3 from, Vector3 to, ThrowResult thr)
        {
            _throwFrom = from + Vector3.up * 2.4f;
            _throwTo = to + Vector3.up * 1.2f;
            if (thr.Relation == Chemistry.Bad || thr.Error)
                _throwTo += new Vector3((float)thr.LateralFt, 0, (float)(-Mathf.Abs((float)thr.LateralFt) * 0.3f));
            _throwDur = Mathf.Clamp(0.82f / Mathf.Max(0.45f, (float)thr.SpeedMul), 0.28f, 1.4f);
            _throwLinger = _throwDur;
            _throwArc = thr.Relation == Chemistry.Good ? 5.2f : thr.Relation == Chemistry.Bad ? 1.6f : 3.2f;
            _throwWobble = thr.Relation == Chemistry.Bad || thr.Error ? 3.4f : 0f;
            _throwGood = thr.Relation == Chemistry.Good;
            var lr = _throwGood ? _throw : _throwBad;
            if (lr == null) lr = _throw;
            if (lr == null) return;
            var c = ThrowColor(thr.Relation);
            lr.startColor = c;
            lr.endColor = new Color(c.r, c.g, c.b, 0.18f);
            lr.startWidth = thr.Relation == Chemistry.Good ? 0.55f : 0.26f;
            lr.endWidth = thr.Relation == Chemistry.Good ? 0.18f : 0.1f;
        }

        public static Color ThrowColor(Chemistry rel)
        {
            var rgb = CartoonJuice.ThrowRgb(rel);
            return new Color((float)rgb.R, (float)rgb.G, (float)rgb.B, 1f);
        }

        public float ThrowSeconds => _throwDur;

        public Vector3 DecoyBall => _decoyPos;
        public bool Active => _pitchLinger > 0 || _swingLinger > 0;
        public string ActivePitch => _pitchLinger > 0 ? _pitchId : "";
        public string CurrentEvent =>
            _swingLinger > 0 && !string.IsNullOrEmpty(_swingId) ? _swingId
            : _pitchLinger > 0 && !string.IsNullOrEmpty(_pitchId) ? _pitchId
            : "";

        void PlaceDecoy(Transform decoy, bool on, Vector3 real, float dt)
        {
            if (decoy == null) return;
            decoy.gameObject.SetActive(on);
            if (!on) return;
            if (_decoyPos.sqrMagnitude < 0.01f) _decoyPos = new Vector3(0, 5.4f, 60.5f);
            _decoyPos = Vector3.Lerp(_decoyPos, new Vector3(-1.6f, 2.6f, 4f), 1f - Mathf.Exp(-1.8f * dt));
            decoy.position = _decoyPos;
            decoy.localScale = Vector3.one * (1.35f + 0.12f * Mathf.Sin(_t * 9f));
            decoy.rotation = Quaternion.Euler(0f, _t * 40f, 6f * Mathf.Sin(_t * 5f));
        }

        void PlaceBarrel(bool on, Vector3 p)
        {
            if (_barrel == null) return;
            _barrel.gameObject.SetActive(on);
            if (!on) return;
            _barrel.position = p;
            _barrel.rotation = Quaternion.Euler(_t * 180f, 28f, _t * 70f);
            var u = 1.05f + 0.08f * Mathf.Abs(Mathf.Sin(_t * 7f));
            _barrel.localScale = Vector3.one * u;
        }

        void PlaceSkull(bool on, Vector3 p)
        {
            if (_skull == null) return;
            _skull.gameObject.SetActive(on);
            if (!on) return;
            _skull.position = p;
            _skull.rotation = Quaternion.Euler(8f * Mathf.Sin(_t * 3f), _t * 70f, 6f * Mathf.Sin(_t * 5f));
            var u = 1.08f + 0.12f * Mathf.Abs(Mathf.Sin(_t * 8f));
            _skull.localScale = Vector3.one * u;
        }

        void Prism(bool on, Vector3 around)
        {
            if (_prismCore != null)
            {
                _prismCore.gameObject.SetActive(on);
                if (on)
                {
                    _prismCore.position = around;
                    _prismCore.localScale = Vector3.one * (0.78f + 0.18f * Mathf.Abs(Mathf.Sin(_t * 14f)));
                }
            }
            if (_prismShell != null)
            {
                _prismShell.gameObject.SetActive(on);
                if (on)
                {
                    _prismShell.position = around;
                    var hue = (_t * 0.45f) % 1f;
                    Look.Paint(_prismShell.gameObject, Look.Unlit(Color.HSVToRGB(hue, 0.55f, 1f) * new Color(1f, 1f, 1f, 0.45f)));
                    _prismShell.localScale = Vector3.one * (1.18f + 0.28f * Mathf.Abs(Mathf.Sin(_t * 8f)));
                }
            }
            for (var i = 0; i < _prism.Length; i++)
            {
                if (_prism[i] == null) continue;
                _prism[i].gameObject.SetActive(on);
                if (!on) continue;
                var a = _t * 3.4f + i * 2.094f;
                var r = 2.15f + 0.45f * Mathf.Sin(_t * 2.2f + i);
                _prism[i].position = around + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(_t * 5f + i) * 0.85f, Mathf.Sin(a) * r);
                var col = Color.HSVToRGB((_t * 0.4f + i / 3f) % 1f, 0.8f, 1f);
                Look.Paint(_prism[i].gameObject, Look.Unlit(col));
                _prism[i].localScale = Vector3.one * (1.05f + 0.2f * Mathf.Abs(Mathf.Sin(_t * 9f + i)));
            }
        }

        void Embers(Transform[] bits, bool on, Vector3 around, bool tight)
        {
            if (bits == null) return;
            for (var i = 0; i < bits.Length; i++)
            {
                if (bits[i] == null) continue;
                bits[i].gameObject.SetActive(on);
                if (!on) continue;
                var a = i * 0.7f + _t * (tight ? 5.5f : 3f);
                var r = tight ? 0.26f + (i % 4) * 0.14f : 0.8f + (i % 4) * 0.45f;
                var y = tight ? (i * 0.18f + _t * 4.2f) % 1.15f : (i * 0.35f + _t * 2.4f) % 3.2f;
                bits[i].position = around + new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                var s = tight ? 0.24f + 0.16f * Mathf.Abs(Mathf.Sin(_t * 11f + i))
                    : 0.28f + 0.18f * Mathf.Sin(_t * 8f + i);
                bits[i].localScale = Vector3.one * s;
            }
        }

        void HeatCore(bool on, Vector3 p)
        {
            if (_heatCore != null)
            {
                _heatCore.gameObject.SetActive(on);
                if (on)
                {
                    _heatCore.position = p;
                    var u = 0.72f + 0.28f * Mathf.Abs(Mathf.Sin(_t * 16f));
                    _heatCore.localScale = Vector3.one * u;
                }
            }
            if (_heatShell != null)
            {
                _heatShell.gameObject.SetActive(on);
                if (on)
                {
                    _heatShell.position = p;
                    var u = 1.15f + 0.35f * Mathf.Abs(Mathf.Sin(_t * 9f + 0.6f));
                    _heatShell.localScale = Vector3.one * u;
                }
            }
        }

        void CharmCore(bool on, Vector3 p) => PulseCore(_charmCore, _charmShell, on, p, 12f, 7f);

        void HeartCore(bool on, Vector3 p) => PulseCore(_heartCore, _heartShell, on, p, 12f, 7f);

        void PulseCore(Transform core, Transform shell, bool on, Vector3 p, float coreHz, float shellHz)
        {
            if (core != null)
            {
                core.gameObject.SetActive(on);
                if (on)
                {
                    core.position = p;
                    core.localScale = Vector3.one * (0.7f + 0.22f * Mathf.Abs(Mathf.Sin(_t * coreHz)));
                }
            }
            if (shell != null)
            {
                shell.gameObject.SetActive(on);
                if (on)
                {
                    shell.position = p;
                    shell.localScale = Vector3.one * (1.12f + 0.28f * Mathf.Abs(Mathf.Sin(_t * shellHz + 0.4f)));
                }
            }
        }

        void Sparkles(Transform[] bits, bool on, Vector3 around)
        {
            if (bits == null) return;
            for (var i = 0; i < bits.Length; i++)
            {
                if (bits[i] == null) continue;
                bits[i].gameObject.SetActive(on);
                if (!on) continue;
                var a = _t * 6.5f + i * 1.2f;
                var r = 0.7f + (i % 3) * 0.25f;
                bits[i].position = around + new Vector3(Mathf.Cos(a) * r, 0.35f + Mathf.Abs(Mathf.Sin(_t * 8f + i)) * 0.7f, Mathf.Sin(a) * r);
                bits[i].localScale = Vector3.one * (0.12f + 0.1f * Mathf.Abs(Mathf.Sin(_t * 14f + i)));
            }
        }

        void FurnaceRim(bool on, Vector3 p)
        {
            for (var i = 0; i < _furnaceRim.Length; i++)
            {
                if (_furnaceRim[i] == null) continue;
                _furnaceRim[i].gameObject.SetActive(on);
                if (!on) continue;
                var a = i / (float)_furnaceRim.Length * Mathf.PI * 2f;
                _furnaceRim[i].position = new Vector3(p.x + Mathf.Cos(a) * 3.6f, 0.22f, p.z + Mathf.Sin(a) * 3.6f);
                _furnaceRim[i].rotation = Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f);
            }
        }

        void Hearts(Transform[] bits, bool on, Vector3 around)
        {
            if (bits == null) return;
            for (var i = 0; i < bits.Length; i++)
            {
                if (bits[i] == null) continue;
                bits[i].gameObject.SetActive(on);
                if (!on) continue;
                var a = _t * 3.4f + i * Mathf.PI * 2f / bits.Length;
                var r = 0.55f + (i % 4) * 0.18f;
                bits[i].position = around + new Vector3(Mathf.Cos(a) * r, 0.12f + Mathf.Sin(_t * 5f + i) * 0.28f, Mathf.Sin(a) * r);
                bits[i].rotation = Quaternion.Euler(-18f, a * Mathf.Rad2Deg, 12f * Mathf.Sin(_t * 4f + i));
                bits[i].localScale = Vector3.one * (0.52f + 0.12f * Mathf.Abs(Mathf.Sin(_t * 7f + i)));
            }
        }

        void Fragments(Transform[] bits, bool on, Vector3 around)
        {
            if (bits == null) return;
            for (var i = 0; i < bits.Length; i++)
            {
                if (bits[i] == null) continue;
                bits[i].gameObject.SetActive(on);
                if (!on) continue;
                var a = i * Mathf.PI * 2f / bits.Length;
                var r = 2.2f + (_t * 6f + i) % 3.5f;
                bits[i].position = around + new Vector3(Mathf.Cos(a) * r, 0.5f + (i % 3) * 0.4f, Mathf.Sin(a) * r);
                bits[i].rotation = Quaternion.Euler(_t * 90f, a * Mathf.Rad2Deg, 20);
            }
        }

        void Burn(Transform burn, bool on, Vector3 p)
        {
            if (burn == null) return;
            burn.gameObject.SetActive(on);
            if (on) burn.position = new Vector3(p.x, 0.18f, p.z);
        }

        void Crack(bool on, Vector3 p)
        {
            if (_crack == null) return;
            _crack.gameObject.SetActive(on);
            if (!on) return;
            _crack.position = new Vector3(p.x, 0.22f, p.z);
            _crack.rotation = Quaternion.Euler(0, 28f, 0);
        }

        void Beam(LineRenderer lr, bool on, Vector3 from, Vector3 to)
        {
            if (lr == null) return;
            lr.enabled = on;
            if (!on) return;
            lr.SetPosition(0, from + Vector3.up * 2.4f);
            lr.SetPosition(1, to);
        }

        public void ResetDecoy()
        {
            _decoyPos = Vector3.zero;
        }

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

        void HideAll()
        {
            foreach (var kv in _groups)
                if (kv.Value != null) kv.Value.gameObject.SetActive(false);
            if (_laser != null) _laser.enabled = false;
            if (_tongue != null) _tongue.enabled = false;
            if (_throw != null) _throw.enabled = false;
            if (_throwBad != null) _throwBad.enabled = false;
        }

        Transform Heart(Transform parent, string name, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Look.Prim(PrimitiveType.Sphere, "L", go.transform, new Vector3(-0.18f, 0.12f, 0), Vector3.one * 0.42f, mat);
            Look.Prim(PrimitiveType.Sphere, "R", go.transform, new Vector3(0.18f, 0.12f, 0), Vector3.one * 0.42f, mat);
            var tip = Look.Prim(PrimitiveType.Cube, "Tip", go.transform, new Vector3(0, -0.18f, 0), new Vector3(0.42f, 0.42f, 0.28f), mat);
            tip.transform.localRotation = Quaternion.Euler(0, 0, 45);
            go.SetActive(false);
            return go.transform;
        }

        Transform Barrel(Transform parent)
        {
            var wood = Look.Lit(new Color(0.48f, 0.28f, 0.12f), smooth: 0.12f);
            var stave = Look.Toon(new Color(0.38f, 0.2f, 0.08f));
            var hoop = Look.Lit(new Color(0.18f, 0.12f, 0.08f), smooth: 0.05f);
            var go = Look.Prim(PrimitiveType.Cylinder, "BarrelBall", parent, Vector3.zero, new Vector3(1.45f, 1.85f, 1.45f), wood);
            Look.Prim(PrimitiveType.Cylinder, "HoopA", go.transform, new Vector3(0, 0.62f, 0), new Vector3(1.18f, 0.07f, 1.18f), hoop);
            Look.Prim(PrimitiveType.Cylinder, "HoopM", go.transform, Vector3.zero, new Vector3(1.22f, 0.07f, 1.22f), hoop);
            Look.Prim(PrimitiveType.Cylinder, "HoopB", go.transform, new Vector3(0, -0.62f, 0), new Vector3(1.18f, 0.07f, 1.18f), hoop);
            Look.Prim(PrimitiveType.Cube, "Bung", go.transform, new Vector3(0.72f, 0.08f, 0f), new Vector3(0.18f, 0.18f, 0.18f), hoop);
            for (var i = 0; i < 6; i++)
            {
                var a = i / 6f * Mathf.PI * 2f;
                var staveGo = Look.Prim(PrimitiveType.Cube, "Stave" + i, go.transform,
                    new Vector3(Mathf.Cos(a) * 0.62f, 0f, Mathf.Sin(a) * 0.62f),
                    new Vector3(0.16f, 1.7f, 0.12f), stave);
                staveGo.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }
            go.SetActive(false);
            return go.transform;
        }

        Transform Skull(Transform parent)
        {
            var bone = Look.Lit(new Color(0.82f, 0.78f, 0.7f), smooth: 0.16f);
            var voidMat = Look.Unlit(new Color(0.06f, 0.01f, 0.08f));
            var glow = Look.Unlit(new Color(0.72f, 0.18f, 0.88f));
            var go = Look.Prim(PrimitiveType.Sphere, "SkullBall", parent, Vector3.zero, Vector3.one * 1.7f, bone);
            Look.Prim(PrimitiveType.Sphere, "EyeL", go.transform, new Vector3(-0.26f, 0.14f, 0.4f), Vector3.one * 0.34f, voidMat);
            Look.Prim(PrimitiveType.Sphere, "EyeR", go.transform, new Vector3(0.26f, 0.14f, 0.4f), Vector3.one * 0.34f, voidMat);
            Look.Prim(PrimitiveType.Sphere, "GlowL", go.transform, new Vector3(-0.26f, 0.14f, 0.42f), Vector3.one * 0.16f, glow);
            Look.Prim(PrimitiveType.Sphere, "GlowR", go.transform, new Vector3(0.26f, 0.14f, 0.42f), Vector3.one * 0.16f, glow);
            Look.Prim(PrimitiveType.Cube, "Nose", go.transform, new Vector3(0f, -0.02f, 0.42f), new Vector3(0.16f, 0.22f, 0.14f), voidMat);
            Look.Prim(PrimitiveType.Cube, "Jaw", go.transform, new Vector3(0, -0.32f, 0.3f), new Vector3(0.72f, 0.2f, 0.38f), bone);
            Look.Prim(PrimitiveType.Cube, "Teeth", go.transform, new Vector3(0f, -0.24f, 0.42f), new Vector3(0.5f, 0.1f, 0.12f), bone);
            go.SetActive(false);
            return go.transform;
        }

        static void FakeFace(Transform ball)
        {
            if (ball == null) return;
            var voidMat = Look.Unlit(new Color(0.12f, 0.08f, 0.1f));
            var grin = Look.Unlit(new Color(0.62f, 0.12f, 0.18f));
            Look.Prim(PrimitiveType.Sphere, "EyeL", ball, new Vector3(-0.22f, 0.1f, 0.48f), Vector3.one * 0.22f, voidMat);
            Look.Prim(PrimitiveType.Sphere, "EyeR", ball, new Vector3(0.22f, 0.1f, 0.48f), Vector3.one * 0.22f, voidMat);
            Look.Prim(PrimitiveType.Cube, "Grin", ball, new Vector3(0f, -0.16f, 0.5f), new Vector3(0.5f, 0.08f, 0.1f), grin);
        }

        Transform Ballish(Transform parent, string name, Color c, float scale)
        {
            var go = Look.Prim(PrimitiveType.Sphere, name, parent, Vector3.zero, Vector3.one * scale, Look.Unlit(c));
            go.SetActive(false);
            return go.transform;
        }

        void DrawThrow(float dt)
        {
            _throwLinger = Mathf.Max(0, _throwLinger - dt);
            var on = _throwLinger > 0;
            Show("throw-trail-good", on && _throwGood);
            Show("throw-trail-bad", on && !_throwGood);
            var lr = _throwGood ? _throw : _throwBad;
            if (lr == null) lr = _throw;
            if (_throw != null) _throw.enabled = on && _throwGood;
            if (_throwBad != null) _throwBad.enabled = on && !_throwGood;
            if (lr == null || !on) return;
            var n = lr.positionCount;
            if (n < 2) n = 12;
            lr.positionCount = n;
            var head = 1f - Mathf.Clamp01(_throwLinger / Mathf.Max(0.05f, _throwDur));
            if (_throwGood)
                lr.startColor = Color.Lerp(Colors.Gold, new Color(0.62f, 0.28f, 1f), 0.5f + 0.5f * Mathf.Sin(_t * 10f));
            for (var i = 0; i < n; i++)
            {
                var t = n == 1 ? 1f : i / (float)(n - 1);
                t = Mathf.Min(t, head);
                var p = Vector3.Lerp(_throwFrom, _throwTo, t);
                p.y += Mathf.Sin(t * Mathf.PI) * _throwArc;
                if (_throwWobble > 0)
                    p += new Vector3(Mathf.Sin(t * 7f) * _throwWobble * t, 0, Mathf.Cos(t * 5f) * _throwWobble * 0.4f * t);
                lr.SetPosition(i, p);
            }
        }

        LineRenderer Line(Transform parent, string name, Color c, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = width;
            lr.endWidth = width * 0.4f;
            lr.material = new Material(Shader.Find("Sprites/Default") ?? Look.LitShader);
            lr.startColor = c;
            lr.endColor = new Color(c.r, c.g, c.b, 0.2f);
            lr.enabled = false;
            return lr;
        }

        Transform Group(string id)
        {
            if (_groups.TryGetValue(id, out var existing) && existing != null) return existing;
            var go = new GameObject(id);
            go.transform.SetParent(_root, false);
            var prefab = ArtBinder.LoadVfx(id);
            if (prefab != null)
            {
                var inst = Object.Instantiate(prefab, go.transform);
                inst.name = id;
            }
            _groups[id] = go.transform;
            return go.transform;
        }

        void Show(string id, bool on)
        {
            if (_groups.TryGetValue(id, out var tf) && tf != null)
                tf.gameObject.SetActive(on);
        }

        static bool Known(string id) => ArtBinder.HasVfx(id);

        static bool IdIs(string id, string want) =>
            !string.IsNullOrEmpty(id) && id.Equals(want, System.StringComparison.OrdinalIgnoreCase);

        void FillHearts(Transform[] dst, Transform parent, Material pink)
        {
            for (var i = 0; i < dst.Length; i++)
                dst[i] = Heart(parent, "Heart" + i, pink);
        }

        void FillFrags(Transform[] dst, Transform parent, Material wood)
        {
            var hoop = Look.Lit(new Color(0.18f, 0.12f, 0.08f), smooth: 0.05f);
            for (var i = 0; i < dst.Length; i++)
            {
                var stave = i % 3 != 0;
                dst[i] = Look.Prim(PrimitiveType.Cube, stave ? "Stave" : "Hoop", parent, Vector3.zero,
                    stave ? new Vector3(1.15f, 0.28f, 0.38f) : new Vector3(0.9f, 0.16f, 0.9f),
                    stave ? wood : hoop).transform;
            }
        }

        void FillShell(Transform[] dst, Transform parent)
        {
            var green = Look.Toon(new Color(0.22f, 0.62f, 0.28f));
            var cream = Look.Toon(new Color(0.92f, 0.88f, 0.7f));
            for (var i = 0; i < dst.Length; i++)
                dst[i] = Look.Prim(PrimitiveType.Cube, "Plate", parent, Vector3.zero,
                    new Vector3(1.15f, 0.16f, 0.72f), i % 2 == 0 ? green : cream).transform;
        }

        void FillSparkles(Transform[] dst, Transform parent)
        {
            var spark = Look.Unlit(new Color(1f, 0.85f, 0.95f));
            for (var i = 0; i < dst.Length; i++)
                dst[i] = Look.Prim(PrimitiveType.Sphere, "Sparkle", parent, Vector3.zero, Vector3.one * 0.18f, spark).transform;
        }

        void FillEmbers(Transform[] dst, Transform parent, Material fire)
        {
            for (var i = 0; i < dst.Length; i++)
                dst[i] = Look.Prim(PrimitiveType.Sphere, "Ember", parent, Vector3.zero, Vector3.one * 0.42f, fire).transform;
        }
    }
}
