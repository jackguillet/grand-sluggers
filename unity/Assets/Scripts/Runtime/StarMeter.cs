using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>Five physical star pips per dugout. Filled gold, empty pewter.</summary>
    public sealed class StarMeter : MonoBehaviour
    {
        Transform _root;
        readonly Transform[] _home = new Transform[5];
        readonly Transform[] _away = new Transform[5];
        Material _on;
        Material _off;
        Mesh _mesh;

        public void Build(Transform parent)
        {
            if (_root != null) Destroy(_root.gameObject);
            _on = Look.Unlit(Colors.Gold);
            _off = Look.Unlit(new Color(0.22f, 0.22f, 0.24f, 0.85f));
            _mesh = StarMesh();
            _root = new GameObject("StarMeter").transform;
            _root.SetParent(parent, false);
            for (var i = 0; i < 5; i++)
            {
                _home[i] = Pip("HomeStar" + i, new Vector3(34.2f + i * 3.15f, 3.62f, 16.45f));
                _away[i] = Pip("AwayStar" + i, new Vector3(-34.2f - i * 3.15f, 3.62f, 16.45f));
            }
        }

        public void Set(double home, double away)
        {
            Paint(_home, home);
            Paint(_away, away);
        }

        void Paint(Transform[] row, double n)
        {
            for (var i = 0; i < row.Length; i++)
            {
                if (row[i] == null) continue;
                var lit = n > i;
                var r = row[i].GetComponent<MeshRenderer>();
                if (r != null) r.sharedMaterial = lit ? _on : _off;
                var fill = lit ? (n >= i + 1 ? 1f : (float)(n - i)) : 0.55f;
                var s = 0.62f + 0.18f * fill;
                row[i].localScale = new Vector3(s, s, 0.7f);
            }
        }

        Transform Pip(string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = _mesh;
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = _off;
            return go.transform;
        }

        static Mesh StarMesh()
        {
            var verts = new Vector3[11];
            var tris = new int[30];
            verts[0] = new Vector3(0f, 0f, 0.08f);
            for (var i = 0; i < 10; i++)
            {
                var r = i % 2 == 0 ? 0.42f : 0.16f;
                var a = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0.08f);
            }
            var t = 0;
            for (var i = 1; i <= 10; i++)
            {
                tris[t++] = 0;
                tris[t++] = i;
                tris[t++] = i == 10 ? 1 : i + 1;
            }
            var back = new Vector3[11];
            for (var i = 0; i < 11; i++)
                back[i] = new Vector3(verts[i].x, verts[i].y, -0.08f);
            var all = new Vector3[22];
            verts.CopyTo(all, 0);
            back.CopyTo(all, 11);
            var triAll = new int[60];
            tris.CopyTo(triAll, 0);
            t = 30;
            for (var i = 1; i <= 10; i++)
            {
                triAll[t++] = 11;
                triAll[t++] = 11 + (i == 10 ? 1 : i + 1);
                triAll[t++] = 11 + i;
            }
            var mesh = new Mesh { name = "StarPip" };
            mesh.vertices = all;
            mesh.triangles = triAll;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
