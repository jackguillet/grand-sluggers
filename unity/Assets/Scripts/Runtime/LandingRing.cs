using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>SMS fly locator: yellow while it's coming, red in the catch window.</summary>
    public sealed class LandingRing : MonoBehaviour
    {
        Transform _root;
        Transform _ring;

        public void Build(Transform parent)
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = new GameObject("LandingRing").transform;
            _root.SetParent(parent, false);
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Ring";
            go.transform.SetParent(_root, false);
            go.transform.localScale = new Vector3(14f, 0.06f, 14f);
            Destroy(go.GetComponent<Collider>());
            _ring = go.transform;
            Hide();
        }

        public void Show(double x, double z, float radius, bool catchWindow)
        {
            if (_root == null) return;
            _root.gameObject.SetActive(true);
            _root.position = new Vector3((float)x, 0.16f, (float)z);
            var d = Mathf.Max(8f, radius * 1.6f);
            _ring.localScale = new Vector3(d, 0.07f, d);
            var col = catchWindow ? new Color(0.95f, 0.18f, 0.16f, 1f) : new Color(1f, 0.86f, 0.12f, 1f);
            Look.Paint(_ring.gameObject, Look.Unlit(col));
        }

        public void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
        }
    }
}
