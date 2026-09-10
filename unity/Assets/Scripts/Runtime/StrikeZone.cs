using GrandSluggers.Sim;
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
            // The frame and umpire use the same world-space plate-crossing bounds.
            var half = (float)StrikeZoneGeometry.HalfWidth;
            var bottom = (float)StrikeZoneGeometry.Bottom;
            var top = (float)StrikeZoneGeometry.Top;
            var center = (float)StrikeZoneGeometry.CenterY;
            var height = (float)StrikeZoneGeometry.Height;
            var z = (float)StrikeZoneGeometry.PlateZ;
            Look.Prim(PrimitiveType.Cube, "Left", _root, new Vector3(-half, center, z), new Vector3(0.06f, height, 0.06f), frame);
            Look.Prim(PrimitiveType.Cube, "Right", _root, new Vector3(half, center, z), new Vector3(0.06f, height, 0.06f), frame);
            Look.Prim(PrimitiveType.Cube, "Top", _root, new Vector3(0, top, z), new Vector3(half * 2, 0.05f, 0.05f), frame);
            Look.Prim(PrimitiveType.Cube, "Bot", _root, new Vector3(0, bottom, z), new Vector3(half * 2, 0.05f, 0.05f), frame);

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
            var (x, y) = SetTells.Locator(aimX, aimY);
            _target.localPosition = new Vector3((float)x, (float)y, (float)StrikeZoneGeometry.PlateZ);
        }
    }
}
