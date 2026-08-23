using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Harbor diamond as placed Hierarchy objects. Runtime dresses meshes;
    /// it does not emit plate/chalk/mound/cameras from ParkView every Play.
    /// </summary>
    public sealed class HarborKit : MonoBehaviour
    {
        public static HarborKit Instance { get; private set; }

        public Transform DirtPad;
        public Transform HomeDirt;
        public Transform DirtDiamond;
        public Transform HomePlate;
        public Transform HomePoint;
        public Transform BoxL;
        public Transform BoxR;
        public Transform FoulL;
        public Transform FoulR;
        public Transform Mound;
        public Transform Rubber;
        public Transform ShotPlate;
        public Transform ShotMound;
        public Transform ShotDiamond;
        public Transform ShotThrow;

        bool _dressed;
        public bool OwnsDiamond { get; private set; }

        void Awake()
        {
            Instance = this;
            EnsureAnchors();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Bind(Park park)
        {
            OwnsDiamond = park != null && park.Id == "harbor-diamond";
            gameObject.SetActive(OwnsDiamond);
            if (OwnsDiamond) Dress();
        }

        public bool TryShot(string id, out Vector3 pos, out Vector3 look, out float fov)
        {
            pos = default;
            look = default;
            fov = 0;
            var tf = ShotNamed(id);
            if (tf == null) return false;
            pos = tf.position;
            var aim = tf.Find("Look");
            look = aim != null ? aim.position : pos + tf.forward * 40f;
            fov = tf.localScale.x > 1f ? tf.localScale.x : 0f;
            return true;
        }

        Transform ShotNamed(string id)
        {
            if (string.Equals(id, "plate", System.StringComparison.OrdinalIgnoreCase)) return ShotPlate;
            if (string.Equals(id, "mound", System.StringComparison.OrdinalIgnoreCase)) return ShotMound;
            if (string.Equals(id, "diamond", System.StringComparison.OrdinalIgnoreCase)) return ShotDiamond;
            if (string.Equals(id, "throw", System.StringComparison.OrdinalIgnoreCase)) return ShotThrow;
            if (string.IsNullOrEmpty(id)) return null;
            return transform.Find("Shot" + char.ToUpperInvariant(id[0]) + id.Substring(1));
        }

        public void EnsureAnchors()
        {
            DirtPad = Anchor("DirtPad", new Vector3(0f, 0.04f, 64f), new Vector3(150f, 0.18f, 150f), Quaternion.identity);
            HomeDirt = Anchor("HomeDirt", new Vector3(0f, 0.16f, 0f), new Vector3(36f, 0.08f, 36f), Quaternion.identity);
            DirtDiamond = Anchor("DirtDiamond", new Vector3(0f, 0.12f, 63.64f), new Vector3(132f, 0.24f, 132f), Quaternion.Euler(0f, 45f, 0f));
            HomePlate = Anchor("HomePlate", new Vector3(0f, 0.24f, 0.4f), new Vector3(2.4f, 0.28f, 1.55f), Quaternion.identity);
            HomePoint = Anchor("HomePoint", new Vector3(0f, 0.24f, -0.52f), new Vector3(1.7f, 0.28f, 1.7f), Quaternion.Euler(0f, 45f, 0f));
            BoxL = Anchor("BoxL", new Vector3(-2.85f, 0.21f, 3.1f), new Vector3(2.2f, 0.07f, 5.6f), Quaternion.identity);
            BoxR = Anchor("BoxR", new Vector3(2.85f, 0.21f, 3.1f), new Vector3(2.2f, 0.07f, 5.6f), Quaternion.identity);
            FoulL = Anchor("FoulL", new Vector3(112f, 0.14f, 112f), new Vector3(0.95f, 0.08f, 200f), Quaternion.Euler(0f, 45f, 0f));
            FoulR = Anchor("FoulR", new Vector3(-112f, 0.14f, 112f), new Vector3(0.95f, 0.08f, 200f), Quaternion.Euler(0f, -45f, 0f));
            Mound = Anchor("Mound", new Vector3(0f, 1.025f, 60.5f), new Vector3(20f, 0.575f, 20f), Quaternion.identity);
            Rubber = Anchor("Rubber", new Vector3(0f, 1.08f, 60.5f), new Vector3(1.9f, 0.08f, 0.45f), Quaternion.identity);
            ShotPlate = ShotAnchor("ShotPlate", new Vector3(-2.4f, 3.4f, -10.5f), new Vector3(0.6f, 4.6f, 56f), 46f);
            ShotMound = ShotAnchor("ShotMound", new Vector3(3.2f, 4.8f, 71f), new Vector3(0.2f, 3.6f, 2.5f), 40f);
            ShotDiamond = ShotAnchor("ShotDiamond", new Vector3(8f, 26f, -18f), new Vector3(0f, 6f, 90f), 50f);
            ShotThrow = ShotAnchor("ShotThrow", new Vector3(0f, 7.5f, -18f), new Vector3(0f, 1.4f, 0f), 42f);
        }

        public void Dress()
        {
            if (_dressed) return;
            EnsureAnchors();
            var chalk = Look.Lit(Colors.Chalk, smooth: 0.05f);
            var dirt = Look.Lit(new Color(0.62f, 0.42f, 0.26f), Look.Dirt, 3f, 0.1f);
            var infield = Look.Lit(Colors.Dirt, Look.Dirt, 10f, 0.1f);
            var boxDirt = Look.Lit(Colors.Dirt, Look.Dirt, 10f, 0.1f);
            Mesh(DirtPad, PrimitiveType.Cube, infield);
            Mesh(HomeDirt, PrimitiveType.Cylinder, infield);
            Mesh(DirtDiamond, PrimitiveType.Cube, infield);
            Mesh(HomePlate, PrimitiveType.Cube, chalk);
            Mesh(HomePoint, PrimitiveType.Cube, chalk);
            Mesh(BoxL, PrimitiveType.Cube, chalk);
            Mesh(BoxR, PrimitiveType.Cube, chalk);
            Look.Prim(PrimitiveType.Cube, "BoxLIn", BoxL, Vector3.zero, new Vector3(0.70f, 0.72f, 0.88f), boxDirt);
            Look.Prim(PrimitiveType.Cube, "BoxRIn", BoxR, Vector3.zero, new Vector3(0.70f, 0.72f, 0.88f), boxDirt);
            Mesh(FoulL, PrimitiveType.Cube, chalk);
            Mesh(FoulR, PrimitiveType.Cube, chalk);
            Mesh(Mound, PrimitiveType.Cylinder, dirt);
            Mesh(Rubber, PrimitiveType.Cube, chalk);
            _dressed = true;
        }

        Transform Anchor(string name, Vector3 pos, Vector3 scale, Quaternion rot)
        {
            var tf = transform.Find(name);
            if (tf != null) return tf;
            var go = new GameObject(name);
            tf = go.transform;
            tf.SetParent(transform, false);
            tf.position = pos;
            tf.localScale = scale;
            tf.rotation = rot;
            return tf;
        }

        Transform ShotAnchor(string name, Vector3 pos, Vector3 look, float fov)
        {
            var tf = transform.Find(name);
            if (tf == null)
            {
                var go = new GameObject(name);
                tf = go.transform;
                tf.SetParent(transform, false);
                tf.position = pos;
                tf.localScale = new Vector3(fov, 1f, 1f);
                tf.LookAt(look);
            }
            var aim = tf.Find("Look");
            if (aim == null)
            {
                var child = new GameObject("Look");
                aim = child.transform;
                aim.SetParent(tf, false);
                aim.position = look;
            }
            return tf;
        }

        static void Mesh(Transform anchor, PrimitiveType type, Material mat)
        {
            if (anchor == null) return;
            if (anchor.childCount > 0 && anchor.GetComponentInChildren<MeshRenderer>() != null) return;
            Look.Prim(type, "Mesh", anchor, Vector3.zero, Vector3.one, mat);
        }
    }
}
