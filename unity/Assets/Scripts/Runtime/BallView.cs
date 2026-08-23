using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed class BallView : MonoBehaviour
    {
        Transform _root;
        GameObject _ball;
        TrailRenderer _trail;
        Light _glow;
        string _look = "";

        public void Build(Transform parent)
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = new GameObject("Ball").transform;
            _root.SetParent(parent, false);
            _ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _ball.name = "Mesh";
            _ball.transform.SetParent(_root, false);
            _ball.transform.localScale = Vector3.one * 1.35f;
            Destroy(_ball.GetComponent<Collider>());
            Look.Paint(_ball, Look.Lit(Colors.Ball, smooth: 0.65f));

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
        }

        public void Place(Vector3 p, string starPitch, string pitchType, bool inPlayHeat)
        {
            if (_root == null) return;
            _root.position = p;
            _root.gameObject.SetActive(true);

            var key = (starPitch ?? "") + "|" + pitchType + "|" + inPlayHeat;
            if (key != _look || starPitch == "prismball")
            {
                _look = key;
                ApplyLook(starPitch, pitchType, inPlayHeat);
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
            _trail.startColor = col;
            _trail.endColor = new Color(col.r, col.g, col.b, 0);
            _trail.time = star == "prismball" ? 0.7f : 0.42f;
            _glow.color = glowCol;
            _glow.intensity = glow;
        }

        public void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
            _look = "";
        }
    }
}
