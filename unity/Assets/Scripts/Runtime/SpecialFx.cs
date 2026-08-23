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
        Transform _crack;
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

            _decoy = Ballish(Group("phonyball"), "Decoy", new Color(0.95f, 0.95f, 0.72f, 0.7f), 1.35f);
            _swingDecoy = Ballish(Group("phony-swing"), "Decoy", new Color(0.95f, 0.95f, 0.72f, 0.7f), 1.35f);
            _barrel = Barrel(Group("caskball"));
            _skull = Skull(Group("skullball"));

            var pink = Look.Unlit(new Color(1f, 0.32f, 0.58f));
            FillHearts(_hearts, Group("charmball"), pink);
            FillHearts(_swingHearts, Group("heart-swing"), pink);

            var wood = Look.Lit(new Color(0.4f, 0.22f, 0.1f), smooth: 0.1f);
            FillFrags(_bits, Group("cask-swing"), wood);
            FillFrags(_shellBits, Group("shell-swing"), wood);

            var fire = Look.Unlit(Colors.EmberFire);
            FillEmbers(_embers, Group("heatball"), fire);
            FillEmbers(_swingEmbers, Group("heat-swing"), fire);
            FillEmbers(_furnaceEmbers, Group("furnace"), fire);

            for (var i = 0; i < _prism.Length; i++)
                _prism[i] = Ballish(Group("prismball"), "Prism" + i, Color.HSVToRGB(i / 3f, 0.7f, 1f), 1.1f);

            _laser = Line(_root, "Laser", Colors.EmberFire, 0.35f);
            _tongue = Line(_root, "Tongue", new Color(1f, 0.4f, 0.55f), 0.28f);
            _throw = Line(Group("throw-trail-good"), "Trail", Colors.Gold, 0.42f);
            _throwBad = Line(Group("throw-trail-bad"), "Trail", new Color(0.4f, 0.3f, 0.16f), 0.26f);
            if (_throw != null) _throw.positionCount = 12;
            if (_throwBad != null) _throwBad.positionCount = 12;

            _heatBurn = Look.Prim(PrimitiveType.Cylinder, "Burn", Group("heat-swing"), Vector3.zero, new Vector3(22f, 0.1f, 22f),
                Look.Lit(Colors.EmberFire, smooth: 0.35f)).transform;
            _burn = Look.Prim(PrimitiveType.Cylinder, "Burn", Group("furnace"), Vector3.zero, new Vector3(22f, 0.1f, 22f),
                Look.Lit(Colors.EmberFire, smooth: 0.35f)).transform;
            _crack = Look.Prim(PrimitiveType.Cube, "Crack", Group("furnace"), Vector3.zero, new Vector3(14f, 0.12f, 1.1f),
                Look.Lit(new Color(0.12f, 0.05f, 0.04f), smooth: 0.05f)).transform;
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
            Embers(_embers, pitchOn && IdIs(_pitchId, "heatball"), ball);
            Embers(_swingEmbers, swingOn && IdIs(_swingId, "heat-swing"), ball);
            Embers(_furnaceEmbers, swingOn && IdIs(_swingId, "furnace"), ball);
            Hearts(_hearts, pitchOn && IdIs(_pitchId, "charmball"), ball);
            Hearts(_swingHearts, swingOn && IdIs(_swingId, "heart-swing"), ball);
            Fragments(_bits, showFrags || swingOn && IdIs(_swingId, "cask-swing"), ball);
            Fragments(_shellBits, swingOn && IdIs(_swingId, "shell-swing"), ball);
            Burn(_heatBurn, showBurn || swingOn && IdIs(_swingId, "heat-swing"), ball);
            Burn(_burn, showBurn || swingOn && IdIs(_swingId, "furnace"), ball);
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
            if (rel == Chemistry.Good) return Color.Lerp(Colors.Gold, new Color(0.62f, 0.28f, 1f), 0.5f);
            if (rel == Chemistry.Bad) return new Color(0.4f, 0.3f, 0.16f);
            return Colors.Chalk;
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
            decoy.localScale = Vector3.one * (1.2f + 0.08f * Mathf.Sin(_t * 10f));
        }

        void PlaceBarrel(bool on, Vector3 p)
        {
            if (_barrel == null) return;
            _barrel.gameObject.SetActive(on);
            if (!on) return;
            _barrel.position = p;
            _barrel.rotation = Quaternion.Euler(_t * 220f, 40f, _t * 90f);
        }

        void PlaceSkull(bool on, Vector3 p)
        {
            if (_skull == null) return;
            _skull.gameObject.SetActive(on);
            if (!on) return;
            _skull.position = p;
            _skull.rotation = Quaternion.Euler(0, _t * 80f, 8f * Mathf.Sin(_t * 4f));
            _skull.localScale = Vector3.one * 1.15f;
        }

        void Prism(bool on, Vector3 around)
        {
            for (var i = 0; i < _prism.Length; i++)
            {
                if (_prism[i] == null) continue;
                _prism[i].gameObject.SetActive(on);
                if (!on) continue;
                var a = _t * 4f + i * 2.1f;
                _prism[i].position = around + new Vector3(Mathf.Cos(a) * 1.6f, Mathf.Sin(_t * 5f + i) * 0.7f, Mathf.Sin(a) * 1.6f);
                var col = Color.HSVToRGB((_t * 0.35f + i / 3f) % 1f, 0.75f, 1f);
                Look.Paint(_prism[i].gameObject, Look.Unlit(col));
            }
        }

        void Embers(Transform[] bits, bool on, Vector3 around)
        {
            if (bits == null) return;
            for (var i = 0; i < bits.Length; i++)
            {
                if (bits[i] == null) continue;
                bits[i].gameObject.SetActive(on);
                if (!on) continue;
                var a = i * 0.7f + _t * 3f;
                var r = 0.8f + (i % 4) * 0.45f;
                var y = (i * 0.35f + _t * 2.4f) % 3.2f;
                bits[i].position = around + new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                bits[i].localScale = Vector3.one * (0.28f + 0.18f * Mathf.Sin(_t * 8f + i));
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
                var a = _t * 2.2f + i * Mathf.PI * 2f / bits.Length;
                bits[i].position = around + new Vector3(Mathf.Cos(a) * 2.6f, 1.4f + Mathf.Sin(_t * 3f + i) * 0.55f, Mathf.Sin(a) * 2.6f);
                bits[i].rotation = Quaternion.Euler(-20, a * Mathf.Rad2Deg, 0);
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
            var wood = Look.Lit(new Color(0.45f, 0.26f, 0.1f), smooth: 0.12f);
            var hoop = Look.Lit(new Color(0.22f, 0.16f, 0.1f), smooth: 0.05f);
            var go = Look.Prim(PrimitiveType.Cylinder, "BarrelBall", parent, Vector3.zero, new Vector3(1.55f, 2.05f, 1.55f), wood);
            Look.Prim(PrimitiveType.Cylinder, "HoopA", go.transform, new Vector3(0, 0.55f, 0), new Vector3(1.15f, 0.08f, 1.15f), hoop);
            Look.Prim(PrimitiveType.Cylinder, "HoopB", go.transform, new Vector3(0, -0.55f, 0), new Vector3(1.15f, 0.08f, 1.15f), hoop);
            go.SetActive(false);
            return go.transform;
        }

        Transform Skull(Transform parent)
        {
            var bone = Look.Lit(new Color(0.78f, 0.74f, 0.68f), smooth: 0.18f);
            var voidMat = Look.Unlit(new Color(0.08f, 0.02f, 0.1f));
            var go = Look.Prim(PrimitiveType.Sphere, "SkullBall", parent, Vector3.zero, Vector3.one * 1.85f, bone);
            Look.Prim(PrimitiveType.Sphere, "EyeL", go.transform, new Vector3(-0.28f, 0.12f, 0.38f), Vector3.one * 0.32f, voidMat);
            Look.Prim(PrimitiveType.Sphere, "EyeR", go.transform, new Vector3(0.28f, 0.12f, 0.38f), Vector3.one * 0.32f, voidMat);
            Look.Prim(PrimitiveType.Cube, "Jaw", go.transform, new Vector3(0, -0.28f, 0.32f), new Vector3(0.7f, 0.18f, 0.35f), bone);
            go.SetActive(false);
            return go.transform;
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
            for (var i = 0; i < dst.Length; i++)
                dst[i] = Look.Prim(PrimitiveType.Cube, "Frag", parent, Vector3.zero, new Vector3(0.9f, 0.32f, 0.5f), wood).transform;
        }

        void FillEmbers(Transform[] dst, Transform parent, Material fire)
        {
            for (var i = 0; i < dst.Length; i++)
                dst[i] = Look.Prim(PrimitiveType.Sphere, "Ember", parent, Vector3.zero, Vector3.one * 0.42f, fire).transform;
        }
    }
}
