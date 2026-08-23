using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// On-deck chemistry items as objects: banana on the grass, rocket at a body, POW as an infield hop.
    /// No smoke / ghost / paint blinds.
    /// </summary>
    public sealed class ItemView : MonoBehaviour
    {
        public const float FlySeconds = 0.42f;

        Transform _root;
        Transform _banana;
        Transform _rocket;
        Transform _pow;
        float _t;

        public void Build(Transform parent)
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = new GameObject("ChemItems").transform;
            _root.SetParent(parent, false);
            _banana = Banana();
            _rocket = Rocket();
            _pow = Pow();
            Hide();
        }

        public void Present(
            float dt,
            bool offered,
            int pick,
            Vector3 target,
            bool flying,
            string thrown,
            float u)
        {
            if (_root == null) return;
            _t += dt;
            if (!offered && !flying)
            {
                Hide();
                return;
            }

            if (flying && !string.IsNullOrEmpty(thrown))
            {
                if (u >= 1f && thrown == "rocket")
                {
                    Hide();
                    return;
                }
                var from = new Vector3(1.6f, 4.2f, 0.8f);
                var to = Dest(thrown, target);
                PlaceThrown(thrown, from, to, Mathf.Clamp01(u));
                return;
            }

            ShowPicker(pick, target);
        }

        public static Vector3 Dest(string item, Vector3 target)
        {
            if (item == "rocket") return target + Vector3.up * 3.1f;
            if (item == "pow") return TowardHome(target, 8f) + Vector3.up * 0.45f;
            return new Vector3(target.x, 0.28f, target.z);
        }

        static Vector3 TowardHome(Vector3 target, float ft)
        {
            var d = new Vector3(-target.x, 0, -target.z);
            if (d.sqrMagnitude < 1f) d = new Vector3(0, 0, -1);
            return target + d.normalized * ft;
        }

        void ShowPicker(int pick, Vector3 target)
        {
            var feet = Dest("banana", target);
            var body = Dest("rocket", target);
            var hop = Dest("pow", target);
            Place(_banana, true, feet, pick == 0, Quaternion.Euler(18, _t * 40f, 12));
            Place(_rocket, true, body, pick == 1, Quaternion.Euler(80f, _t * 120f, 0));
            Place(_pow, true, hop, pick == 2, Quaternion.Euler(0, _t * 90f, 8f * Mathf.Sin(_t * 6f)));
        }

        void PlaceThrown(string id, Vector3 from, Vector3 to, float u)
        {
            Transform mesh = id == "rocket" ? _rocket : id == "pow" ? _pow : _banana;
            if (_banana != null) _banana.gameObject.SetActive(id == "banana");
            if (_rocket != null) _rocket.gameObject.SetActive(id == "rocket");
            if (_pow != null) _pow.gameObject.SetActive(id == "pow");
            var p = Vector3.Lerp(from, to, u);
            if (id == "banana")
                p.y += Mathf.Sin(u * Mathf.PI) * 3.4f;
            else if (id == "rocket")
                p.y += Mathf.Sin(u * Mathf.PI) * 1.1f;
            else
                p.y += Mathf.Abs(Mathf.Sin(u * Mathf.PI * 2f)) * 4.2f;
            var rot = id == "rocket"
                ? Quaternion.LookRotation((to - from).sqrMagnitude < 0.01f ? Vector3.up : (to - from))
                : Quaternion.Euler(_t * 180f, 40f, _t * 90f);
            Place(mesh, true, p, true, rot);
            if (id == "pow" && mesh != null)
                mesh.localScale = Vector3.one * (0.7f + 1.4f * u);
        }

        void Place(Transform t, bool on, Vector3 pos, bool selected, Quaternion rot)
        {
            if (t == null) return;
            t.gameObject.SetActive(on);
            if (!on) return;
            t.position = pos;
            t.rotation = rot;
            var pulse = selected ? 1.15f + 0.12f * Mathf.Sin(_t * 8f) : 0.72f;
            t.localScale = Vector3.one * pulse;
        }

        void HidePicker()
        {
            if (_banana != null) _banana.gameObject.SetActive(false);
            if (_rocket != null) _rocket.gameObject.SetActive(false);
            if (_pow != null) _pow.gameObject.SetActive(false);
        }

        public void Hide()
        {
            HidePicker();
        }

        Transform Banana()
        {
            var peel = Look.Lit(new Color(1f, 0.86f, 0.12f), smooth: 0.38f);
            var stem = Look.Lit(new Color(0.28f, 0.16f, 0.06f), smooth: 0.08f);
            var go = new GameObject("Banana");
            go.transform.SetParent(_root, false);
            Look.Prim(PrimitiveType.Sphere, "Body", go.transform, Vector3.zero, new Vector3(1.55f, 0.48f, 0.52f), peel);
            Look.Prim(PrimitiveType.Sphere, "Tip", go.transform, new Vector3(0.58f, 0.14f, 0), new Vector3(0.72f, 0.38f, 0.4f), peel);
            Look.Prim(PrimitiveType.Cylinder, "Stem", go.transform, new Vector3(-0.7f, 0.2f, 0), new Vector3(0.14f, 0.24f, 0.14f), stem);
            go.SetActive(false);
            return go.transform;
        }

        Transform Rocket()
        {
            var red = Look.Lit(new Color(0.9f, 0.16f, 0.12f), smooth: 0.22f);
            var nose = Look.Lit(new Color(0.95f, 0.94f, 0.88f), smooth: 0.4f);
            var fin = Look.Lit(new Color(0.18f, 0.2f, 0.28f), smooth: 0.12f);
            var go = new GameObject("Rocket");
            go.transform.SetParent(_root, false);
            Look.Prim(PrimitiveType.Cylinder, "Body", go.transform, Vector3.zero, new Vector3(0.42f, 0.85f, 0.42f), red);
            Look.Prim(PrimitiveType.Sphere, "Nose", go.transform, new Vector3(0, 0.92f, 0), Vector3.one * 0.48f, nose);
            Look.Prim(PrimitiveType.Cube, "FinL", go.transform, new Vector3(-0.32f, -0.52f, 0), new Vector3(0.12f, 0.42f, 0.32f), fin);
            Look.Prim(PrimitiveType.Cube, "FinR", go.transform, new Vector3(0.32f, -0.52f, 0), new Vector3(0.12f, 0.42f, 0.32f), fin);
            Look.Prim(PrimitiveType.Cube, "FinB", go.transform, new Vector3(0, -0.52f, -0.32f), new Vector3(0.32f, 0.42f, 0.12f), fin);
            go.SetActive(false);
            return go.transform;
        }

        Transform Pow()
        {
            var burst = Look.Unlit(new Color(1f, 0.84f, 0.12f));
            var go = new GameObject("POW");
            go.transform.SetParent(_root, false);
            Look.Prim(PrimitiveType.Sphere, "Core", go.transform, Vector3.zero, Vector3.one * 1.05f, burst);
            for (var i = 0; i < 6; i++)
            {
                var a = i * 60f * Mathf.Deg2Rad;
                Look.Prim(PrimitiveType.Cube, "Spike" + i, go.transform,
                    new Vector3(Mathf.Cos(a) * 0.72f, 0, Mathf.Sin(a) * 0.72f),
                    new Vector3(0.38f, 0.2f, 0.55f), burst);
            }
            go.SetActive(false);
            return go.transform;
        }
    }
}
