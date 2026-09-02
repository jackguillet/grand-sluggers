using GrandSluggers.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrandSluggers.UnityClient
{
    public sealed class BallView : MonoBehaviour
    {
        Transform _home;
        Transform _root;
        GameObject _ball;
        GameObject _halo;
        TrailRenderer _trail;
        Light _glow;
        Transform _shadow;
        Transform _puff;
        Transform _cloud;
        readonly Transform[] _bits = new Transform[5];
        string _look = "";
        Transform _held;
        float _lastY = 4f;
        bool _hadY;
        float _puffT = -1f;
        Vector3 _lastPlace;
        bool _hadPlace;
        static readonly float Diameter = (float)Baseball.DiameterFt;
        static readonly float Sit = Diameter * 0.5f;
        bool _inFlight;
        bool _inPlay;

        public bool Held => _held != null;

        public void EmitTrail(bool on)
        {
            if (_trail == null) return;
            _trail.emitting = on && _held == null;
            if (!on) _trail.Clear();
        }

        public void SetTrailColor(Color c)
        {
            if (_trail == null) return;
            _trail.startColor = new Color(c.r, c.g, c.b, 0.85f);
            _trail.endColor = new Color(c.r, c.g, c.b, 0f);
        }

        public void ContactPuff(Vector3 p) => BurstPuff(p);

        public void Build(Transform parent)
        {
            if (_root != null) Destroy(_root.gameObject);
            if (_shadow != null) Destroy(_shadow.gameObject);
            if (_puff != null) Destroy(_puff.gameObject);
            _home = parent;
            _held = null;
            _hadY = false;
            _puffT = -1f;

            _root = new GameObject("Ball").transform;
            _root.SetParent(parent, false);
            _ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _ball.name = "Mesh";
            _ball.transform.SetParent(_root, false);
            _ball.transform.localPosition = new Vector3(0f, Sit, 0f);
            _ball.transform.localScale = Vector3.one * Diameter;
            Destroy(_ball.GetComponent<Collider>());
            Look.Paint(_ball, Look.Lit(new Color(0.96f, 0.93f, 0.86f), smooth: 0.45f));
            Stitch(_ball.transform);

            _halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _halo.name = "Halo";
            _halo.transform.SetParent(_root, false);
            _halo.transform.localPosition = new Vector3(0f, Sit, 0f);
            Destroy(_halo.GetComponent<Collider>());
            Look.Paint(_halo, Look.Unlit(new Color(1f, 0.88f, 0.28f, 0.72f)));
            var haloR = _halo.GetComponent<Renderer>();
            if (haloR != null)
            {
                haloR.shadowCastingMode = ShadowCastingMode.Off;
                haloR.receiveShadows = false;
            }
            _halo.SetActive(false);

            var trailGo = new GameObject("Trail");
            trailGo.transform.SetParent(_root, false);
            trailGo.transform.localPosition = new Vector3(0f, Sit, 0f);
            _trail = trailGo.AddComponent<TrailRenderer>();
            _trail.time = (float)SetTells.TrailSeconds;
            _trail.startWidth = (float)SetTells.TrailStartFt(Diameter);
            _trail.endWidth = (float)SetTells.TrailEndFt(Diameter);
            _trail.widthMultiplier = 1f;
            _trail.alignment = LineAlignment.View;
            _trail.numCapVertices = 4;
            _trail.numCornerVertices = 4;
            _trail.minVertexDistance = 0.08f;
            _trail.shadowCastingMode = ShadowCastingMode.Off;
            _trail.receiveShadows = false;
            _trail.material = MotionArc();
            _trail.startColor = new Color(1f, 0.86f, 0.28f, 0.95f);
            _trail.endColor = new Color(1f, 0.78f, 0.18f, 0f);

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(_root, false);
            _glow = glowGo.AddComponent<Light>();
            _glow.type = LightType.Point;
            _glow.range = 18f;
            _glow.intensity = 0f;
            _glow.color = Colors.EmberFire;

            var dirt = Look.Unlit(new Color(0.12f, 0.1f, 0.08f, 0.55f));
            _shadow = Look.Prim(PrimitiveType.Cylinder, "BallShadow", parent,
                new Vector3(0, 0.04f, 0), new Vector3(Diameter * 1.15f, 0.04f, Diameter * 1.15f), dirt).transform;
            _shadow.gameObject.SetActive(false);

            _puff = new GameObject("HopPuff").transform;
            _puff.SetParent(parent, false);
            var dust = Look.Unlit(new Color(0.40f, 0.28f, 0.16f, 0.75f));
            _cloud = Look.Prim(PrimitiveType.Cylinder, "Cloud", _puff, Vector3.zero,
                new Vector3(0.55f, 0.06f, 0.55f), dust).transform;
            for (var i = 0; i < _bits.Length; i++)
            {
                _bits[i] = Look.Prim(PrimitiveType.Sphere, "Bit" + i, _puff, Vector3.zero,
                    Vector3.one * 0.14f, dust).transform;
            }
            _puff.gameObject.SetActive(false);
        }

        public void Place(Vector3 p, string starPitch, string pitchType, bool inPlayHeat, bool inFlight = false, bool inPlay = false)
        {
            _inFlight = inFlight;
            _inPlay = inPlay;
            if (_root == null) return;
            _root.gameObject.SetActive(true);

            if (_held == null)
            {
                _root.position = p;
                StampShadow(p);
                MaybeHopPuff(p);
                Spin(p);
            }
            else
            {
                _root.localPosition = new Vector3(0f, 0.1f, 0.52f);
                _root.localRotation = Quaternion.identity;
                if (_shadow != null) _shadow.gameObject.SetActive(false);
                _hadPlace = false;
            }

            TickPuff();

            var key = (starPitch ?? "") + "|" + pitchType + "|" + inPlayHeat + "|" + _inFlight + "|" + _inPlay;
            if (key != _look || starPitch == "prismball")
            {
                _look = key;
                ApplyLook(starPitch, pitchType, inPlayHeat);
            }
            StampScale(starPitch, inPlayHeat);
        }

        public void Hold(Transform glove)
        {
            if (_root == null || glove == null) return;
            _held = glove;
            _root.SetParent(glove, false);
            _root.localPosition = new Vector3(0f, 0.1f, 0.52f);
            _root.localRotation = Quaternion.identity;
            _root.localScale = Vector3.one;
            if (_trail != null)
            {
                _trail.Clear();
                _trail.emitting = false;
            }
            if (_shadow != null) _shadow.gameObject.SetActive(false);
        }

        public void Release()
        {
            if (_root == null || _held == null) return;
            var world = _root.position;
            _root.SetParent(_home, true);
            _root.position = world;
            _root.localScale = Vector3.one;
            _held = null;
            if (_trail != null)
            {
                _trail.Clear();
                _trail.emitting = true;
            }
        }

        void ApplyLook(string star, string type, bool heat)
        {
            Color col;
            var glow = 0f;
            var glowCol = Colors.EmberFire;
            var matCol = new Color(0.96f, 0.93f, 0.86f);
            var smooth = 0.45f;

            if (star == "heatball" || heat)
            {
                matCol = Colors.EmberFire;
                col = Colors.EmberFire;
                glow = 3.6f;
                smooth = 0.15f;
            }
            else if (star == "charmball")
            {
                matCol = new Color(1f, 0.42f, 0.68f);
                col = matCol;
                glow = 2.8f;
                glowCol = matCol;
                smooth = 0.35f;
            }
            else if (star == "prismball")
            {
                matCol = new Color(0.55f, 1f, 0.75f);
                col = Color.HSVToRGB((Time.time * 0.4f) % 1f, 0.7f, 1f);
                glow = 1.8f;
                glowCol = col;
            }
            else if (star == "skullball")
            {
                matCol = new Color(0.12f, 0.08f, 0.14f);
                col = new Color(0.7f, 0.2f, 0.85f);
                glow = 2.4f;
                glowCol = col;
                smooth = 0.1f;
            }
            else if (star == "caskball")
            {
                matCol = new Color(0.42f, 0.24f, 0.1f);
                col = matCol;
                glow = 0.6f;
                glowCol = matCol;
                smooth = 0.08f;
            }
            else if (star == "phonyball")
            {
                matCol = new Color(0.96f, 0.93f, 0.86f);
                col = Color.white;
            }
            else
            {
                col = type == "curve" ? new Color(0.45f, 0.75f, 1f)
                    : type == "slider" ? new Color(0.85f, 0.55f, 1f)
                    : type == "changeup" ? new Color(1f, 0.92f, 0.45f)
                    : new Color(1f, 0.86f, 0.28f);
                glow = (float)Baseball.FlightGlow;
                glowCol = col;
            }

            Look.Paint(_ball, Look.Lit(matCol, smooth: smooth));
            SetTrailColor(col);
            _trail.time = star == "prismball" ? 0.7f : (float)SetTells.TrailSeconds;
            _glow.color = glowCol;
            _glow.intensity = _inFlight ? glow : 0f;
        }

        void StampScale(string star, bool heat)
        {
            if (_ball == null) return;
            float scale;
            if (star == "heatball" || heat)
                scale = Diameter * 1.15f;
            else if (star == "caskball")
                scale = Diameter * 1.3f;
            else if (star == "skullball")
                scale = Diameter * 1.22f;
            else if (!_inFlight && star == "charmball")
                scale = Diameter * 1.08f;
            else
                scale = (float)Baseball.ApparentScale(_inFlight, _root.position.z, _inPlay);
            _ball.transform.localScale = Vector3.one * scale;
            _ball.transform.localPosition = new Vector3(0f, Sit, 0f);
            if (_halo != null)
            {
                var on = _inFlight && _held == null;
                _halo.SetActive(on);
                if (on)
                {
                    _halo.transform.localScale = Vector3.one * scale * (float)Baseball.HaloMul;
                    _halo.transform.localPosition = new Vector3(0f, Sit, 0f);
                }
            }
            if (_trail != null)
            {
                _trail.startWidth = (float)SetTells.TrailStartFt(scale);
                _trail.endWidth = (float)SetTells.TrailEndFt(scale);
            }
        }

        public void Hide()
        {
            Release();
            if (_root != null) _root.gameObject.SetActive(false);
            if (_shadow != null) _shadow.gameObject.SetActive(false);
            if (_puff != null) _puff.gameObject.SetActive(false);
            _puffT = -1f;
            _look = "";
            _hadY = false;
            _hadPlace = false;
        }

        static Material MotionArc()
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Look.LitShader;
            var m = new Material(sh);
            var gold = new Color(1f, 0.86f, 0.28f, 1f);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", gold);
            if (m.HasProperty("_Color")) m.SetColor("_Color", gold);
            m.color = gold;
            m.renderQueue = 3000;
            return m;
        }

        static void Stitch(Transform ball)
        {
            var thread = Look.Unlit(new Color(0.72f, 0.1f, 0.12f));
            var ringA = Look.Prim(PrimitiveType.Cylinder, "SeamA", ball,
                Vector3.zero, new Vector3(1.04f, 0.028f, 1.04f), thread);
            ringA.transform.localRotation = Quaternion.Euler(90f, 0f, 18f);
            var ringB = Look.Prim(PrimitiveType.Cylinder, "SeamB", ball,
                Vector3.zero, new Vector3(1.04f, 0.028f, 1.04f), thread);
            ringB.transform.localRotation = Quaternion.Euler(90f, 0f, 72f);
            for (var i = 0; i < 14; i++)
            {
                var t = i / 14f * Mathf.PI * 2f;
                var a = new Vector3(Mathf.Sin(t) * 0.51f, Mathf.Cos(t) * 0.16f, Mathf.Cos(t) * 0.48f);
                Look.Prim(PrimitiveType.Cube, "StA" + i, ball, a, new Vector3(0.07f, 0.05f, 0.035f), thread);
                var b = new Vector3(Mathf.Cos(t) * 0.48f, Mathf.Sin(t) * 0.16f, Mathf.Sin(t) * 0.51f);
                Look.Prim(PrimitiveType.Cube, "StB" + i, ball, b, new Vector3(0.07f, 0.05f, 0.035f), thread);
            }
        }

        void Spin(Vector3 p)
        {
            if (_ball == null) return;
            if (_hadPlace)
            {
                var d = p - _lastPlace;
                var speed = d.magnitude / Mathf.Max(1e-4f, Time.deltaTime);
                var axis = Vector3.Cross(Vector3.up, d);
                if (axis.sqrMagnitude > 1e-6f && speed > 0.4f)
                    _ball.transform.Rotate(axis.normalized, speed * 9.5f * Time.deltaTime, Space.World);
            }
            _lastPlace = p;
            _hadPlace = true;
        }

        void StampShadow(Vector3 p)
        {
            if (_shadow == null) return;
            var h = Mathf.Max(0f, p.y);
            var d = (float)Baseball.ApparentScale(_inFlight, p.z, _inPlay);
            var s = Mathf.Lerp(d * 1.2f, d * 0.5f, Mathf.Clamp01(h / 38f));
            _shadow.position = new Vector3(p.x, 0.04f, p.z);
            _shadow.localScale = new Vector3(s, 0.04f, s);
            _shadow.gameObject.SetActive(true);
        }

        void MaybeHopPuff(Vector3 p)
        {
            if (_hadY && _lastY > 0.45f && p.y < 0.22f)
                BurstPuff(p);
            _lastY = p.y;
            _hadY = true;
        }

        void BurstPuff(Vector3 p)
        {
            if (_puff == null) return;
            _puffT = 0f;
            _puff.position = new Vector3(p.x, 0.08f, p.z);
            _puff.localScale = Vector3.one;
            _puff.gameObject.SetActive(true);
            if (_cloud != null)
                _cloud.localScale = new Vector3(0.55f, 0.06f, 0.55f);
            for (var i = 0; i < _bits.Length; i++)
            {
                if (_bits[i] == null) continue;
                var a = i * (Mathf.PI * 2f / _bits.Length);
                _bits[i].localPosition = new Vector3(Mathf.Cos(a) * 0.12f, 0.06f, Mathf.Sin(a) * 0.12f);
                _bits[i].localScale = Vector3.one * 0.14f;
            }
        }

        void TickPuff()
        {
            if (_puffT < 0f || _puff == null) return;
            _puffT += Time.deltaTime;
            var u = Mathf.Clamp01(_puffT / 0.18f);
            if (u >= 1f)
            {
                _puff.gameObject.SetActive(false);
                _puffT = -1f;
                return;
            }
            var fade = 1f - u;
            if (_cloud != null)
                _cloud.localScale = new Vector3(0.55f + 0.7f * u, 0.05f, 0.55f + 0.7f * u);
            for (var i = 0; i < _bits.Length; i++)
            {
                if (_bits[i] == null) continue;
                var a = i * (Mathf.PI * 2f / _bits.Length);
                var r = 0.12f + 0.45f * u;
                _bits[i].localPosition = new Vector3(Mathf.Cos(a) * r, 0.06f + 0.22f * u * fade, Mathf.Sin(a) * r);
                var s = 0.14f * fade;
                _bits[i].localScale = Vector3.one * s;
            }
        }
    }
}
