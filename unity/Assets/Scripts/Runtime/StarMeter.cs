using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Five physical star pips per dugout. Filled gold, empty pewter.
    ///
    /// <para>
    /// The pips stand on the foul rail of the park being played — the one the <see cref="ParkView"/>
    /// beside this meter last built — where the dugout is: the rail's top
    /// (<see cref="HarborWall.HipHeight"/>) at points of the rail the ball meets
    /// (<see cref="HarborWall.FoulWall"/>, which is <see cref="ParkBoundary.RailPoint"/>), along the
    /// dugout span the wall loop pins on each foul wrap at every park
    /// (<see cref="HarborDugout.AlongHome"/>). Nothing here reads Harbor's kit (#859); at Harbor that
    /// rail is the dugout fascia, so the pips land where they always have.
    /// </para>
    /// </summary>
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
            var view = GetComponent<ParkView>();
            var park = view != null ? view.Park : null;
            var rules = view != null && view.Table != null ? view.Table : Rules.Default;
            if (_root != null) Destroy(_root.gameObject);
            _on = Look.Toon(Colors.Gold);
            _off = Look.Toon(new Color(0.28f, 0.28f, 0.30f));
            _mesh = StarMesh();
            _root = new GameObject("StarMeter").transform;
            _root.SetParent(parent, false);
            var y = HarborWall.HipHeight(rules) + 0.22f;
            var faceHome = Quaternion.Euler(-8f, -135f, 0f);
            var faceAway = Quaternion.Euler(-8f, 135f, 0f);
            for (var i = 0; i < 5; i++)
            {
                var along = HarborDugout.AlongHome + 1.7f + i * HarborDugout.StarSpacing;
                var home = Rail(park, 1, along, rules);
                var away = Rail(park, -1, along, rules);
                _home[i] = Pip("HomeStar" + i, new Vector3(home.x - 0.15f, y, home.z), faceHome);
                _away[i] = Pip("AwayStar" + i, new Vector3(away.x + 0.15f, y, away.z), faceAway);
            }
        }

        /// <summary>
        /// The park's foul rail at this distance along the line, on this side (+1 first base). With no
        /// park built yet the rail runs parallel to the line, which is where every park's rail is this
        /// close to home (the flare starts at <see cref="ParkBoundary.FlareStartFt"/>).
        /// </summary>
        static Vector3 Rail(Park park, int sign, float along, RulesTable rules)
        {
            var pole = park != null ? AtBatResolver.FenceAt(park, sign * AtBatResolver.FoulLineDeg) : 0;
            var p = HarborWall.FoulWall(sign, along, pole, rules);
            return new Vector3((float)p.X, 0f, (float)p.Z);
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
