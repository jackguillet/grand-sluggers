using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed class StrikeZone : MonoBehaviour
    {
        Transform _root;
        Transform _target;

        public void Build(Transform parent)
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = new GameObject("StrikeZone").transform;
            _root.SetParent(parent, false);

            var frame = Look.Unlit(new Color(0.95f, 0.96f, 0.9f, 1f));
            // Zone sits just in front of the plate, ~17" wide × ~28" tall.
            Look.Prim(PrimitiveType.Cube, "Left", _root, new Vector3(-0.92f, 2.55f, 1.1f), new Vector3(0.06f, 2.2f, 0.06f), frame);
            Look.Prim(PrimitiveType.Cube, "Right", _root, new Vector3(0.92f, 2.55f, 1.1f), new Vector3(0.06f, 2.2f, 0.06f), frame);
            Look.Prim(PrimitiveType.Cube, "Top", _root, new Vector3(0, 3.65f, 1.1f), new Vector3(1.9f, 0.05f, 0.05f), frame);
            Look.Prim(PrimitiveType.Cube, "Bot", _root, new Vector3(0, 1.45f, 1.1f), new Vector3(1.9f, 0.05f, 0.05f), frame);

            var pip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pip.name = "Aim";
            pip.transform.SetParent(_root, false);
            pip.transform.localScale = Vector3.one * 0.38f;
            Object.Destroy(pip.GetComponent<Collider>());
            Look.Paint(pip, Look.Unlit(new Color(1f, 0.82f, 0.15f, 0.9f)));
            _target = pip.transform;
            _root.gameObject.SetActive(false);
        }

        public void Show(bool on, float aimX, float aimY)
        {
            if (_root == null) return;
            _root.gameObject.SetActive(on);
            if (!on || _target == null) return;
            var (x, y) = GrandSluggers.Sim.PitchFlight.PlateTarget(aimX, aimY);
            _target.localPosition = new Vector3((float)x, (float)y, 1.15f);
        }
    }
}
