using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed class BallView : MonoBehaviour
    {
        Transform _root;
        GameObject _ball;
        TrailRenderer _trail;
        Light _glow;
        bool _heat;

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
            _trail.time = 0.38f;
            _trail.startWidth = 0.55f;
            _trail.endWidth = 0.04f;
            _trail.material = new Material(Shader.Find("Sprites/Default") ?? Look.LitShader);
            _trail.startColor = Color.white;
            _trail.endColor = new Color(1, 1, 1, 0);
            _trail.minVertexDistance = 0.15f;

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(_root, false);
            _glow = glowGo.AddComponent<Light>();
            _glow.type = LightType.Point;
            _glow.range = 18f;
            _glow.intensity = 0f;
            _glow.color = Colors.EmberFire;
        }

        public void Place(Vector3 p, bool heat, string pitchType)
        {
            if (_root == null) return;
            _root.position = p;
            _root.gameObject.SetActive(true);
            if (heat != _heat)
            {
                _heat = heat;
                Look.Paint(_ball, Look.Lit(heat ? Colors.EmberFire : Colors.Ball, smooth: heat ? 0.2f : 0.65f));
                _ball.transform.localScale = Vector3.one * (heat ? 1.7f : 1.35f);
            }
            var col = heat ? Colors.EmberFire
                : pitchType == "curve" ? new Color(0.45f, 0.75f, 1f)
                : pitchType == "slider" ? new Color(0.85f, 0.55f, 1f)
                : pitchType == "changeup" ? new Color(1f, 0.92f, 0.45f)
                : Color.white;
            _trail.startColor = col;
            _trail.endColor = new Color(col.r, col.g, col.b, 0);
            _glow.intensity = heat ? 3.4f : 0f;
        }

        public void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
        }
    }
}
