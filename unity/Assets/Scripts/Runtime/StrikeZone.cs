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
            _oval = cursor.AddComponent<LineRenderer>();
            _oval.name = "SweetSpotOval";
            _oval.useWorldSpace = false;
            _oval.loop = true;
            _oval.positionCount = Segments;
            _oval.startWidth = _oval.endWidth = 0.075f;
            _oval.sharedMaterial = gold;
            _oval.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _oval.receiveShadows = false;
            _rules = rules;
            Outline(Hand.R, 1f);
            var centerMarker = Look.Prim(PrimitiveType.Sphere, "SweetSpot", cursor.transform,
                Vector3.zero, Vector3.one * 0.13f, gold);
            Object.Destroy(centerMarker.GetComponent<Collider>());
            _target = cursor.transform;

            // The pitcher's aim tell: the crossing of the pitch as it stands (spec §4.4, #577).
            var tell = new GameObject("AimTell");
            tell.transform.SetParent(_root, false);
            var ice = Look.Unlit(new Color(0.55f, 0.85f, 1f, 0.95f));
            var ring = tell.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 24;
            ring.startWidth = ring.endWidth = 0.06f;
            ring.sharedMaterial = ice;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            for (var i = 0; i < ring.positionCount; i++)
            {
                var a = i * Mathf.PI * 2f / ring.positionCount;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * AimTellRadius, Mathf.Sin(a) * AimTellRadius, 0));
            }
            var dot = Look.Prim(PrimitiveType.Sphere, "AimDot", tell.transform, Vector3.zero, Vector3.one * 0.09f, ice);
            Object.Destroy(dot.GetComponent<Collider>());
            _aim = tell.transform;
            _aim.gameObject.SetActive(false);
            _root.gameObject.SetActive(false);
        }

        const float AimTellRadius = 0.2f;
        Transform _aim;

        /// <summary>Place the aim tell at a world crossing on the plate plane, or hide it.</summary>
        public void AimTell(bool on, float x, float y)
        {
            if (_aim == null) return;
            _aim.gameObject.SetActive(on);
            if (!on) return;
            _aim.localPosition = new Vector3(x, y, (float)StrikeZoneGeometry.PlateZ);
        }

        const int Segments = 40;
        LineRenderer _oval;
        RulesTable _rules;
        Hand _drawnHand = Hand.R;
        float _drawnScale = -1f;

        /// <summary>The drawn oval is the hitbox the sim judges: tip side long, handle side short (spec §5.2).</summary>
        void Outline(Hand bats, float barrelScale)
        {
            if (_oval == null) return;
            if (bats == _drawnHand && Mathf.Approximately(barrelScale, _drawnScale)) return;
            _drawnHand = bats;
            _drawnScale = barrelScale;
            var pts = SweetSpot.Outline(bats, barrelScale, Segments, _rules);
            for (var i = 0; i < pts.Count; i++)
                _oval.SetPosition(i, new Vector3((float)pts[i].X, (float)pts[i].Y, 0));
        }

        /// <summary>
        /// Follow the batter, never the pitch: the box walk in X, the zone center in Y.
        /// <paramref name="barrelScale"/> is the swing's bat scale (contact, charge, buddies).
        /// </summary>
        public void Hide() => Show(false, 0, Hand.R);

        public void Show(bool on, float boxOffsetX, Hand bats, float barrelScale = 1f)
        {
            if (_root == null) return;
            _root.gameObject.SetActive(on);
            if (!on || _target == null) return;
            Outline(bats, barrelScale);
            var (x, y) = SweetSpot.WorldCenter(boxOffsetX);
            _target.localPosition = new Vector3((float)x, (float)y, (float)StrikeZoneGeometry.PlateZ);
        }
    }
}
