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
            // Zone sits just in front of the plate, ~17" wide × ~28" tall.
            Look.Prim(PrimitiveType.Cube, "Left", _root, new Vector3(-0.92f, 2.55f, 1.1f), new Vector3(0.06f, 2.2f, 0.06f), frame);
            Look.Prim(PrimitiveType.Cube, "Right", _root, new Vector3(0.92f, 2.55f, 1.1f), new Vector3(0.06f, 2.2f, 0.06f), frame);
            Look.Prim(PrimitiveType.Cube, "Top", _root, new Vector3(0, 3.65f, 1.1f), new Vector3(1.9f, 0.05f, 0.05f), frame);
            Look.Prim(PrimitiveType.Cube, "Bot", _root, new Vector3(0, 1.45f, 1.1f), new Vector3(1.9f, 0.05f, 0.05f), frame);

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
                    Mathf.Cos(a) * (float)SweetSpot.WorldHalfWidth,
                    Mathf.Sin(a) * (float)SweetSpot.WorldHalfHeight,
                    0));
            }
            var center = Look.Prim(PrimitiveType.Sphere, "SweetSpot", cursor.transform,
                Vector3.zero, Vector3.one * 0.13f, gold);
            Object.Destroy(center.GetComponent<Collider>());
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
            _target.localPosition = new Vector3((float)x, (float)y, 1.15f);
        }
    }
}
