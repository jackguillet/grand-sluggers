using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Circle on the grass at the landing. Yellow while it is coming, red in
    /// the catch window. Tube from <see cref="LandingMark"/> — not a pancake.
    /// </summary>
    public sealed class LandingRing : MonoBehaviour
    {
        Transform _root;
        Transform _ring;
        Material _gold;
        Material _hot;

        public void Build(Transform parent)
        {
            if (_root != null) Destroy(_root.gameObject);
            _gold = Look.Unlit(new Color(1f, 0.86f, 0.12f));
            _hot = Look.Unlit(new Color(0.95f, 0.18f, 0.16f));
            _root = new GameObject("LandingRing").transform;
            _root.SetParent(parent, false);
            var max = (float)Mathf.Max((float)LandingMark.MinRadiusFt, 0.01f);
            var minor = (float)(LandingMark.ThickFt / max);
            _ring = Look.Torus("Circle", _root, 1f, minor, _gold, seg: 40, sides: 8).transform;
            Hide();
        }

        public void Show(double x, double z, float radius, bool catchWindow)
        {
            if (_root == null) return;
            _root.gameObject.SetActive(true);
            _root.position = new Vector3((float)x, (float)LandingMark.WorldY, (float)z);
            var r = Mathf.Max((float)LandingMark.MinRadiusFt, radius);
            _root.localScale = Vector3.one * r;
            if (_ring != null)
                Look.Paint(_ring.gameObject, catchWindow ? _hot : _gold);
        }

        public void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
        }
    }
}
