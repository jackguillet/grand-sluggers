using GrandSluggers.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// A park's rough backdrop (<see cref="ParkKitSlots.BlockoutBackdrop"/>; data/art/backdrops.json, WD-03 A): the Blender
    /// blockout the row bakes (<c>tools/blender/backdrop_blockout.py</c>), loaded from its Resources copy and painted shape by
    /// shape in the row's colours; until the bake is placed, the same rows stood as greybox primitives. Nothing here picks a
    /// position, a shape or a colour: the rows do, for every park the same way.
    /// </summary>
    public sealed class ParkBackdropView
    {
        readonly Transform _root;

        public ParkBackdropView(Transform parent)
        {
            _root = new GameObject("Backdrop").transform;
            _root.SetParent(parent, false);
        }

        public void Build(ParkBackdrop row)
        {
            if (row == null) return;
            var baked = row.Placed ? Resources.Load<GameObject>(row.Resources) : null;
            if (baked != null)
            {
                var go = Object.Instantiate(baked, _root);
                go.name = "Blockout";
                for (var i = 0; i < row.Shapes.Count; i++)
                {
                    var part = go.transform.Find("Shape" + i);
                    if (part != null) Paint(part.gameObject, row.Shapes[i].Color);
                }
                return;
            }
            for (var i = 0; i < row.Shapes.Count; i++) Stand(i, row.Shapes[i]);
        }

        /// <summary>One greybox shape: at its bearing and distance, base on the ground, turned to face home and tipped back by its pitch.</summary>
        void Stand(int index, BackdropShape s)
        {
            var type = s.Kind switch
            {
                "sphere" => PrimitiveType.Sphere,
                "cylinder" or "cone" => PrimitiveType.Cylinder,
                _ => PrimitiveType.Cube,
            };
            var (x, z) = s.At;
            float w = (float)s.SizeFt[0], h = (float)s.SizeFt[1], d = (float)s.SizeFt[2];
            var pitch = (float)s.PitchDeg;
            var up = Mathf.Abs(h * Mathf.Cos(pitch * Mathf.Deg2Rad)) + Mathf.Abs(d * Mathf.Sin(pitch * Mathf.Deg2Rad));
            // A Unity cylinder is two units tall; a cone stands as a cylinder in the greybox (the bake has the point).
            var scale = type == PrimitiveType.Cylinder ? new Vector3(w, h * 0.5f, d) : new Vector3(w, h, d);
            var go = Look.Prim(type, "Shape" + index, _root, new Vector3((float)x, up * 0.5f, (float)z), scale, Look.Lit(Hex(s.Color), smooth: 0.1f));
            go.transform.localRotation = Quaternion.Euler(0f, (float)s.BearingDeg, 0f) * Quaternion.Euler(pitch, 0f, 0f);
            Quiet(go);
        }

        static void Paint(GameObject go, string hex)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = Look.Lit(Hex(hex), smooth: 0.1f);
            Quiet(go);
        }

        static void Quiet(GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        internal static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
    }

    /// <summary>
    /// A park's night rig (<see cref="ParkKitSlots.NightRig"/>; data/art/night-rigs.json, WD-10 A): towers, torches, neon and
    /// lanterns as greybox shapes at the bearings their rows list. Their glow is an unlit colour and never a light, so the play
    /// light is the same night rig for every park (FD-11-R2). Built at night only; the rows pick every place and colour.
    /// </summary>
    public sealed class NightRigView
    {
        readonly Transform _root;

        public NightRigView(Transform parent)
        {
            _root = new GameObject("NightRig").transform;
            _root.SetParent(parent, false);
        }

        public void Build(NightRig rig)
        {
            if (rig == null) return;
            foreach (var piece in rig.Pieces)
            {
                var body = Look.Lit(ParkBackdropView.Hex(piece.Color), smooth: 0.2f);
                var glow = Look.Unlit(ParkBackdropView.Hex(piece.Glow));
                foreach (var bearing in piece.BearingsDeg)
                    Stand(piece, bearing, body, glow);
            }
        }

        void Stand(NightRigPiece p, double bearingDeg, Material body, Material glow)
        {
            var rad = bearingDeg * Mathf.Deg2Rad;
            var root = new GameObject(p.Kind).transform;
            root.SetParent(_root, false);
            root.localPosition = new Vector3((float)(p.DistanceFt * System.Math.Sin(rad)), 0f, (float)(p.DistanceFt * System.Math.Cos(rad)));
            root.localRotation = Quaternion.Euler(0f, (float)bearingDeg, 0f);
            float h = (float)p.HeightFt, s = (float)p.SizeFt;
            switch (p.Kind)
            {
                case "tower":
                    Look.Prim(PrimitiveType.Cylinder, "Mast", root, new Vector3(0, h * 0.5f, 0), new Vector3(s * 0.35f, h * 0.5f, s * 0.35f), body);
                    Look.Prim(PrimitiveType.Cube, "Bank", root, new Vector3(0, h, 0), new Vector3(s * 2.2f, s * 1.2f, s * 0.5f), glow);
                    break;
                case "torch":
                    Look.Prim(PrimitiveType.Cylinder, "Post", root, new Vector3(0, h * 0.5f, 0), new Vector3(s * 0.3f, h * 0.5f, s * 0.3f), body);
                    Look.Prim(PrimitiveType.Sphere, "Flame", root, new Vector3(0, h + s * 0.4f, 0), new Vector3(s, s * 1.4f, s), glow);
                    break;
                case "neon":
                    Look.Prim(PrimitiveType.Cube, "Frame", root, new Vector3(0, h * 0.5f, 0.3f), new Vector3(s * 1.4f, h, s * 0.3f), body);
                    Look.Prim(PrimitiveType.Cube, "Tube", root, new Vector3(0, h * 0.5f, 0f), new Vector3(s * 0.5f, h * 0.9f, s * 0.4f), glow);
                    break;
                default:
                    Look.Prim(PrimitiveType.Cylinder, "Post", root, new Vector3(0, h * 0.5f, 0), new Vector3(s * 0.2f, h * 0.5f, s * 0.2f), body);
                    Look.Prim(PrimitiveType.Sphere, "Lamp", root, new Vector3(0, h + s * 0.5f, 0), Vector3.one * s, glow);
                    break;
            }
        }
    }
}
