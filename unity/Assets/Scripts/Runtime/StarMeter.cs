using GrandSluggers.Sim;
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
            _on = Look.Toon(Colors.Gold);
            _off = Look.Toon(new Color(0.28f, 0.28f, 0.30f));
            _mesh = StarMesh();
            _root = new GameObject("StarMeter").transform;
            _root.SetParent(parent, false);
            var y = HarborKit.DugoutFasciaY + 0.22f;
            var faceHome = Quaternion.Euler(-8f, -135f, 0f);
            var faceAway = Quaternion.Euler(-8f, 135f, 0f);
            for (var i = 0; i < 5; i++)
            {
                var along = HarborDugout.Along0 - HarborDugout.HalfAlong + 1.7f
                    + i * HarborKit.DugoutStarSpacing;
                var home = HarborDugout.RailAt(1, along);
                var away = HarborDugout.RailAt(-1, along);
                _home[i] = Pip("HomeStar" + i, new Vector3(home.X - 0.15f, y, home.Z), faceHome);
                _away[i] = Pip("AwayStar" + i, new Vector3(away.X + 0.15f, y, away.Z), faceAway);
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
                var fill = lit ? (n >= i + 1 ? 1f : (float)(n - i)) : 0.7f;
                var s = 0.92f + 0.18f * fill;
                row[i].localScale = new Vector3(s, s, 1f);
            }
        }

        Transform Pip(string name, Vector3 pos, Quaternion rot)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.SetPositionAndRotation(pos, rot);
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
            verts[0] = new Vector3(0f, 0f, 0.16f);
            for (var i = 0; i < 10; i++)
            {
                var r = i % 2 == 0 ? 0.55f : 0.22f;
                var a = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0.16f);
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
                back[i] = new Vector3(verts[i].x, verts[i].y, -0.16f);
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
