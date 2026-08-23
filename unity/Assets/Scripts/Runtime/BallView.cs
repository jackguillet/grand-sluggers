using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed class BallView : MonoBehaviour
    {
        Transform _home;
        Transform _root;
        GameObject _ball;
        TrailRenderer _trail;
        Light _glow;
        Transform _shadow;
        Transform _puff;
        readonly Transform[] _bits = new Transform[6];
        string _look = "";
        Transform _held;
        float _lastY = 4f;
        bool _hadY;
        float _puffT = -1f;
        Vector3 _lastPlace;
        bool _hadPlace;
        const float Sit = 0.55f;

        public bool Held => _held != null;

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
            _ball.transform.localScale = Vector3.one * 1.35f;
            Destroy(_ball.GetComponent<Collider>());
            Look.Paint(_ball, Look.Lit(Colors.Ball, smooth: 0.55f));
            var stitch = Look.Unlit(new Color(0.82f, 0.16f, 0.18f));
            var ringA = Look.Prim(PrimitiveType.Cylinder, "SeamA", _ball.transform,
                Vector3.zero, new Vector3(1.02f, 0.035f, 1.02f), stitch);
            ringA.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var ringB = Look.Prim(PrimitiveType.Cylinder, "SeamB", _ball.transform,
                Vector3.zero, new Vector3(1.02f, 0.035f, 1.02f), stitch);
            ringB.transform.localRotation = Quaternion.Euler(90f, 0f, 90f);

            _trail = _ball.AddComponent<TrailRenderer>();
            _trail.time = 0.42f;
            _trail.startWidth = 0.55f;
            _trail.endWidth = 0.04f;
            _trail.material = new Material(Shader.Find("Sprites/Default") ?? Look.LitShader);
            _trail.startColor = Color.white;
            _trail.endColor = new Color(1, 1, 1, 0);
            _trail.minVertexDistance = 0.12f;

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(_root, false);
            _glow = glowGo.AddComponent<Light>();
            _glow.type = LightType.Point;
            _glow.range = 18f;
            _glow.intensity = 0f;
            _glow.color = Colors.EmberFire;

            var dirt = Look.Unlit(new Color(0.12f, 0.1f, 0.08f, 0.55f));
            _shadow = Look.Prim(PrimitiveType.Cylinder, "BallShadow", parent,
                new Vector3(0, 0.05f, 0), new Vector3(1.8f, 0.04f, 1.8f), dirt).transform;
            _shadow.gameObject.SetActive(false);

            _puff = new GameObject("HopPuff").transform;
            _puff.SetParent(parent, false);
            var dust = Look.Unlit(new Color(0.48f, 0.34f, 0.18f, 0.85f));
            Look.Prim(PrimitiveType.Cylinder, "Cloud", _puff, Vector3.zero, new Vector3(1.4f, 0.1f, 1.4f), dust);
            for (var i = 0; i < _bits.Length; i++)
            {
                _bits[i] = Look.Prim(PrimitiveType.Cube, "Bit" + i, _puff, Vector3.zero,
                    new Vector3(0.38f, 0.22f, 0.3f), Look.Lit(new Color(0.42f, 0.28f, 0.12f), smooth: 0.08f)).transform;
            }
            _puff.gameObject.SetActive(false);
        }

        public void Place(Vector3 p, string starPitch, string pitchType, bool inPlayHeat)
        {
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
                _root.localPosition = new Vector3(0f, 0.12f, 0.18f);
                _root.localRotation = Quaternion.identity;
                if (_shadow != null) _shadow.gameObject.SetActive(false);
                _hadPlace = false;
            }

            TickPuff();

            var key = (starPitch ?? "") + "|" + pitchType + "|" + inPlayHeat;
            if (key != _look || starPitch == "prismball")
            {
                _look = key;
                ApplyLook(starPitch, pitchType, inPlayHeat);
            }
        }

        public void Hold(Transform glove)
        {
            if (_root == null || glove == null) return;
            _held = glove;
            _root.SetParent(glove, false);
            _root.localPosition = new Vector3(0f, 0.12f, 0.18f);
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
            var scale = 1.35f;
            var glow = 0f;
            var glowCol = Colors.EmberFire;
            var matCol = Colors.Ball;
            var smooth = 0.65f;

            if (star == "heatball" || heat)
            {
                matCol = Colors.EmberFire;
                col = Colors.EmberFire;
                scale = 1.75f;
                glow = 3.6f;
                smooth = 0.15f;
            }
            else if (star == "charmball")
            {
                matCol = new Color(1f, 0.45f, 0.7f);
                col = matCol;
                scale = 1.5f;
                glow = 2.2f;
                glowCol = matCol;
                smooth = 0.4f;
            }
            else if (star == "prismball")
            {
                matCol = new Color(0.55f, 1f, 0.75f);
                col = Color.HSVToRGB((Time.time * 0.4f) % 1f, 0.7f, 1f);
                scale = 1.45f;
                glow = 1.8f;
                glowCol = col;
            }
            else if (star == "skullball")
            {
                matCol = new Color(0.12f, 0.08f, 0.14f);
                col = new Color(0.7f, 0.2f, 0.85f);
                scale = 1.9f;
                glow = 2.4f;
                glowCol = col;
                smooth = 0.1f;
            }
            else if (star == "caskball")
            {
                matCol = new Color(0.42f, 0.24f, 0.1f);
                col = matCol;
                scale = 2.1f;
                glow = 0.6f;
                glowCol = matCol;
                smooth = 0.08f;
            }
            else if (star == "phonyball")
            {
                matCol = new Color(0.95f, 0.95f, 0.7f);
                col = matCol;
                scale = 1.4f;
            }
            else
            {
                col = type == "curve" ? new Color(0.45f, 0.75f, 1f)
                    : type == "slider" ? new Color(0.85f, 0.55f, 1f)
                    : type == "changeup" ? new Color(1f, 0.92f, 0.45f)
                    : Color.white;
            }

            Look.Paint(_ball, Look.Lit(matCol, smooth: smooth));
            _ball.transform.localScale = Vector3.one * scale;
            _ball.transform.localPosition = new Vector3(0f, Sit, 0f);
            _trail.startColor = col;
            _trail.endColor = new Color(col.r, col.g, col.b, 0);
            _trail.time = star == "prismball" ? 0.7f : 0.42f;
            _glow.color = glowCol;
            _glow.intensity = glow;
        }

        public void Hide()
        {
            Release();
            if (_root != null) _root.gameObject.SetActive(false);
            if (_shadow != null) _shadow.gameObject.SetActive(false);
            if (_puff != null) _puff.gameObject.SetActive(false);
            _look = "";
            _hadY = false;
            _hadPlace = false;
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
                    _ball.transform.Rotate(axis.normalized, speed * 6.5f * Time.deltaTime, Space.World);
            }
            _lastPlace = p;
            _hadPlace = true;
        }

        void StampShadow(Vector3 p)
        {
            if (_shadow == null) return;
            var h = Mathf.Max(0f, p.y);
            var s = Mathf.Lerp(2.4f, 0.65f, Mathf.Clamp01(h / 38f));
            _shadow.position = new Vector3(p.x, 0.05f, p.z);
            _shadow.localScale = new Vector3(s, 0.05f, s);
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
            _puff.gameObject.SetActive(true);
            for (var i = 0; i < _bits.Length; i++)
            {
                if (_bits[i] == null) continue;
                var a = i * (Mathf.PI * 2f / _bits.Length);
                _bits[i].localPosition = new Vector3(Mathf.Cos(a) * 0.4f, 0.12f, Mathf.Sin(a) * 0.4f);
            }
        }

        void TickPuff()
        {
            if (_puffT < 0f || _puff == null) return;
            _puffT += Time.deltaTime;
            var u = Mathf.Clamp01(_puffT / 0.38f);
            if (u >= 1f)
            {
                _puff.gameObject.SetActive(false);
                _puffT = -1f;
                return;
            }
            _puff.localScale = new Vector3(1.1f + 4.2f * u, 0.12f + 0.2f * (1f - u), 1.1f + 4.2f * u);
            for (var i = 0; i < _bits.Length; i++)
            {
                if (_bits[i] == null) continue;
                var a = i * (Mathf.PI * 2f / _bits.Length);
                var r = 0.4f + 2.4f * u;
                _bits[i].localPosition = new Vector3(Mathf.Cos(a) * r, 0.12f + 1.4f * u * (1f - u), Mathf.Sin(a) * r);
            }
        }
    }
}
