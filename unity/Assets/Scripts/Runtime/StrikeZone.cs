using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed class StrikeZone : MonoBehaviour
    {
        Transform _root;
        Transform _target;

        public void Build(Transform parent, RulesTable rules)
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

            var cursor = new GameObject("BatterCursor");
            cursor.transform.SetParent(_root, false);
            var gold = Look.Unlit(new Color(1f, 0.82f, 0.15f, 0.92f));
            var oval = cursor.AddComponent<LineRenderer>();
            oval.name = "SweetSpotOval";
            oval.useWorldSpace = false;
            oval.loop = true;
            oval.positionCount = 40;
            oval.startWidth = oval.endWidth = 0.075f;
            oval.sharedMaterial = gold;
            oval.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            oval.receiveShadows = false;
            for (var i = 0; i < oval.positionCount; i++)
            {
                var a = i * Mathf.PI * 2f / oval.positionCount;
                oval.SetPosition(i, new Vector3(
                    Mathf.Cos(a) * (float)SweetSpot.WorldHalfWidth(rules),
                    Mathf.Sin(a) * (float)SweetSpot.WorldHalfHeight(rules),
                    0));
            }
            var centerMarker = Look.Prim(PrimitiveType.Sphere, "SweetSpot", cursor.transform,
                Vector3.zero, Vector3.one * 0.13f, gold);
            Object.Destroy(centerMarker.GetComponent<Collider>());
            _target = cursor.transform;
            _root.gameObject.SetActive(false);
        }

        public void Show(bool on, float boxOffsetX, float unusedY)
        {
            if (_root == null) return;
            _root.gameObject.SetActive(on);
            if (!on || _target == null) return;
            _ = unusedY;
            var (x, y) = SweetSpot.WorldCenter(boxOffsetX);
            _target.localPosition = new Vector3((float)x, (float)y, (float)StrikeZoneGeometry.PlateZ);
        }
    }
}
