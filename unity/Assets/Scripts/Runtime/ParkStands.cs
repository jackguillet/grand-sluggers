using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The park-neutral bowl of bleachers (<see cref="ParkKitSlots.KitBowl"/>): the treads <see cref="KitBowl"/> lays out
    /// from the park's own wall loop, drawn as a few meshes in the colors of the palette's stands block. Solid risers down
    /// to the ground and closed at each end, a seat bench per section in the seat colors taken in turn, aisles between
    /// sections, seated toy fans in the crowd colors in most seats, a front rail, a back wall with a trim, and an optional
    /// roof over the back rows of the horseshoe.
    /// Nothing here picks a position or a color: the geometry is the sim's and the paint is data.
    /// </summary>
    public sealed class ParkStands
    {
        const float SeatDepth = 1.1f;
        const float SeatHeight = 0.85f;
        const float SeatInset = 0.35f;
        const float FanBodyW = 1.5f;
        const float FanBodyD = 1.0f;
        const float FanBodyH = 2.0f;
        const float FanHead = 1.05f;
        const float FrontRailFt = 3.2f;
        const float BackWallFt = 5.5f;
        const float TrimFt = 0.8f;
        const float RoofFromRow = 3f;
        const float PostEveryFt = 36f;

        readonly Transform _root;

        public ParkStands(Transform parent)
        {
            _root = new GameObject("KitBowl").transform;
            _root.SetParent(parent, false);
        }

        public void Build(Park park, ParkStandsLook look, RulesTable rules)
        {
            var steps = new Quads();
            var rail = new Quads();
            var roof = new Quads();
            var shirts = new Quads[look.Crowd.Count];
            for (var i = 0; i < shirts.Length; i++) shirts[i] = new Quads();
            var heads = new[] { new Quads(), new Quads() };
            var pieceNo = 0;
            var seats = new Quads[look.Seats.Count];
            for (var i = 0; i < seats.Length; i++) seats[i] = new Quads();
            var posts = new List<(Vector3 Base, float Height)>();

            foreach (var piece in KitBowl.Of(park, rules))
            {
                pieceNo++;
                var treads = piece.Treads;
                Ends(piece, steps);
                for (var r = 0; r < treads.Count; r++)
                {
                    var t = treads[r];
                    var y = (float)t.Y;
                    var below = r == 0 ? 0f : (float)treads[r - 1].Y;
                    for (var j = 0; j + 1 < t.Front.Count; j++)
                    {
                        var a = V(t.Front[j]);
                        var b = V(t.Front[j + 1]);
                        var oa = V(t.Out[j]);
                        var ob = V(t.Out[j + 1]);
                        var run = HarborStands.RowRun;
                        // The riser from the tread below up to this one, and this tread's top.
                        steps.Wall(a, b, below, y);
                        steps.Flat(a, b, a + oa * run, b + ob * run, y);
                        if (r == 0) rail.Wall(a - oa * 0.2f, b - ob * 0.2f, 0f, y + FrontRailFt - 1f);
                        Sections(t, j, y, seats, shirts, heads, look.Fill, pieceNo);
                        if (r == treads.Count - 1)
                        {
                            var ba = a + oa * run;
                            var bb = b + ob * run;
                            steps.Wall(ba, bb, 0f, y + BackWallFt, facingIn: true);
                            steps.Wall(ba, bb, 0f, y + BackWallFt, facingIn: false);
                            rail.Wall(ba - oa * 0.05f, bb - ob * 0.05f, y + BackWallFt - TrimFt, y + BackWallFt + 0.05f);
                            if (look.Roof != null && piece.Kind == KitBowl.Horseshoe)
                            {
                                var ra = a - oa * ((treads.Count - RoofFromRow) * run);
                                var rb = b - ob * ((treads.Count - RoofFromRow) * run);
                                var ry = y + HarborStands.RoofLift;
                                roof.Flat(ra, rb, ba, bb, ry);
                                roof.Flat(ra, rb, ba, bb, ry - HarborStands.RoofThick, down: true);
                                roof.Wall(ra, rb, ry - HarborStands.RoofThick, ry);
                            }
                        }
                    }
                    if (look.Roof != null && piece.Kind == KitBowl.Horseshoe && r == treads.Count - 1)
                        for (var j = 0; j < t.Front.Count; j++)
                            if (j == 0 || j == t.Front.Count - 1 || Crosses(t.S, j, PostEveryFt))
                                posts.Add((V(t.Front[j]) + V(t.Out[j]) * (HarborStands.RowRun - 0.6f), y + HarborStands.RoofLift));
                }
            }

            steps.Mesh("Steps", _root, Look.Lit(look.Steps));
            rail.Mesh("Rail", _root, Look.Lit(look.Rail));
            for (var i = 0; i < seats.Length; i++)
                seats[i].Mesh("Seats" + i, _root, Look.Lit(Look.Of(look.Seats[i]), smooth: 0.3f), shadows: false);
            for (var i = 0; i < shirts.Length; i++)
                shirts[i].Mesh("Fans" + i, _root, Look.Toon(Look.Of(look.Crowd[i])), shadows: false);
            heads[0].Mesh("FanHeads", _root, Look.Toon(new Color(1f, 0.80f, 0.68f)), shadows: false);
            heads[1].Mesh("FanHeadsDark", _root, Look.Toon(new Color(0.46f, 0.30f, 0.20f)), shadows: false);
            if (look.Roof != null)
            {
                var roofMat = Look.Lit(look.Roof);
                roof.Mesh("Roof", _root, roofMat);
                var postMat = Look.Lit(look.Rail);
                for (var i = 0; i < posts.Count; i++)
                {
                    var (p, h) = posts[i];
                    Look.Prim(PrimitiveType.Cylinder, "Post" + i, _root, new Vector3(p.x, h * 0.5f, p.z), new Vector3(0.9f, h * 0.5f, 0.9f), postMat);
                }
            }
        }

        /// <summary>
        /// Close each end of a piece: the stepped profile of every tread, from the ground up, and the back wall's end, facing
        /// both ways so the stand reads solid from either side.
        /// </summary>
        static void Ends(KitBowl.Piece piece, Quads steps)
        {
            var treads = piece.Treads;
            var last = treads[^1];
            foreach (var j in new[] { 0, last.Front.Count - 1 })
            {
                foreach (var t in treads)
                {
                    var a = V(t.Front[j]);
                    var b = a + V(t.Out[j]) * HarborStands.RowRun;
                    var top = (float)t.Y + (t == last ? BackWallFt : 0f);
                    steps.Wall(a, b, 0f, top, facingIn: true);
                    steps.Wall(a, b, 0f, top, facingIn: false);
                }
            }
        }

        /// <summary>This tread's seats and fans between front points j and j+1, split at the aisles.</summary>
        static void Sections(KitBowl.Tread t, int j, float y, Quads[] seats, Quads[] shirts, Quads[] heads, double fill, int pieceNo)
        {
            var s0 = t.S[j];
            var s1 = t.S[j + 1];
            if (s1 - s0 < 1e-3) return;
            var period = KitBowl.SectionFt + KitBowl.AisleFt;
            // Walk the segment in pieces that do not cross a section or aisle boundary.
            var s = s0;
            while (s < s1 - 1e-6)
            {
                var k = System.Math.Floor(s / period);
                var local = s - k * period;
                var edge = local < KitBowl.SectionFt ? k * period + KitBowl.SectionFt : (k + 1) * period;
                var e = System.Math.Min(edge, s1);
                var section = KitBowl.SectionAt((s + e) * 0.5);
                if (section >= 0)
                {
                    var seat = seats[((section % seats.Length) + seats.Length) % seats.Length];
                    Edge(t, j, s, e, SeatInset, out var fa, out var fb);
                    Edge(t, j, s, e, SeatInset + SeatDepth, out var ba, out var bb);
                    seat.Wall(fa, fb, y, y + SeatHeight);
                    seat.Flat(fa, fb, ba, bb, y + SeatHeight);
                    Edge(t, j, s, e, SeatInset + SeatDepth + 0.2f, out var ka, out var kb);
                    seat.Wall(ka, kb, y, y + SeatHeight + 1.3f);
                    // One fan per seat whose centre falls in this piece of the section, in most seats.
                    var seatFt = HarborStands.SeatFt;
                    for (var n = System.Math.Ceiling(s / seatFt - 0.5); (n + 0.5) * seatFt < e; n++)
                    {
                        var at = (n + 0.5) * seatFt;
                        if (at < s) continue;
                        var id = pieceNo * 100003 + t.Row * 7919 + (int)n;
                        if (Hash01(id) >= fill) continue;
                        var u = (float)((at - s0) / (s1 - s0));
                        var p = Vector3.Lerp(V(t.Front[j]), V(t.Front[j + 1]), u);
                        var o = Vector3.Lerp(V(t.Out[j]), V(t.Out[j + 1]), u).normalized;
                        var along = (V(t.Front[j + 1]) - V(t.Front[j])).normalized;
                        var c = p + o * (SeatInset + SeatDepth * 0.6f) + along * ((Hash01(id + 3) - 0.5f) * 0.5f);
                        var lean = Hash01(id + 5);
                        var shirt = shirts[(int)(Hash01(id + 1) * shirts.Length) % shirts.Length];
                        shirt.Box(c + Vector3.up * (y + SeatHeight), along, o, FanBodyW, FanBodyD, FanBodyH);
                        var head = heads[Hash01(id + 2) < 0.25f ? 1 : 0];
                        head.Box(c + Vector3.up * (y + SeatHeight + FanBodyH + 0.05f) + o * ((lean - 0.5f) * 0.3f), along, o, FanHead, FanHead, FanHead);
                        // A few fans have an arm up.
                        if (lean > 0.86f)
                            shirt.Box(c + along * (FanBodyW * 0.62f) + Vector3.up * (y + SeatHeight + FanBodyH * 0.7f), along, o, 0.45f, 0.45f, 1.6f);
                    }
                }
                s = e;
            }
        }

        /// <summary>The ground points <paramref name="inset"/> behind this tread's front edge at parameters s and e of segment j.</summary>
        static void Edge(KitBowl.Tread t, int j, double s, double e, float inset, out Vector3 a, out Vector3 b)
        {
            var s0 = t.S[j];
            var s1 = t.S[j + 1];
            var u0 = (float)((s - s0) / (s1 - s0));
            var u1 = (float)((e - s0) / (s1 - s0));
            var oa = Vector3.Lerp(V(t.Out[j]), V(t.Out[j + 1]), u0).normalized;
            var ob = Vector3.Lerp(V(t.Out[j]), V(t.Out[j + 1]), u1).normalized;
            a = Vector3.Lerp(V(t.Front[j]), V(t.Front[j + 1]), u0) + oa * inset;
            b = Vector3.Lerp(V(t.Front[j]), V(t.Front[j + 1]), u1) + ob * inset;
        }

        static bool Crosses(IReadOnlyList<double> s, int j, double every) =>
            j > 0 && System.Math.Floor(s[j] / every) != System.Math.Floor(s[j - 1] / every);

        static Vector3 V((double X, double Z) p) => new Vector3((float)p.X, 0f, (float)p.Z);

        static float Hash01(int n)
        {
            unchecked
            {
                var h = (uint)n * 2654435761u;
                h ^= h >> 13;
                h *= 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFF) / 65535f;
            }
        }

        /// <summary>
        /// Flat-shaded quads gathered into one mesh per material. A wall faces the field (against the tread's outward
        /// direction) unless told otherwise; its u runs along the edge so a texture tiles along the row.
        /// </summary>
        sealed class Quads
        {
            readonly List<Vector3> _v = new List<Vector3>();
            readonly List<Vector2> _uv = new List<Vector2>();
            readonly List<int> _t = new List<int>();

            /// <summary>An upright quad over the ground edge a→b, from y0 to y1.</summary>
            public void Wall(Vector3 a, Vector3 b, float y0, float y1, bool facingIn = true, float uv0 = 0f, float uv1 = 1f)
            {
                if (y1 - y0 < 1e-3f) return;
                var p0 = new Vector3(a.x, y0, a.z);
                var p1 = new Vector3(b.x, y0, b.z);
                var p2 = new Vector3(b.x, y1, b.z);
                var p3 = new Vector3(a.x, y1, a.z);
                Add(p0, p1, p2, p3, new Vector2(uv0, 0), new Vector2(uv1, 0), new Vector2(uv1, 1), new Vector2(uv0, 1), facingIn);
            }

            /// <summary>A level quad at height y: front edge a→b, back edge ba→bb, facing up (or down, under a roof).</summary>
            public void Flat(Vector3 a, Vector3 b, Vector3 ba, Vector3 bb, float y, bool down = false)
            {
                var p0 = new Vector3(a.x, y, a.z);
                var p1 = new Vector3(b.x, y, b.z);
                var p2 = new Vector3(bb.x, y, bb.z);
                var p3 = new Vector3(ba.x, y, ba.z);
                var n = Vector3.Cross(p1 - p0, p3 - p0);
                Add(p0, p1, p2, p3, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), (n.y < 0) != down);
            }

            /// <summary>
            /// A box standing on <paramref name="baseCenter"/>: <paramref name="w"/> along <paramref name="along"/>,
            /// <paramref name="d"/> along <paramref name="outward"/>, <paramref name="h"/> tall; four sides and a top.
            /// </summary>
            public void Box(Vector3 baseCenter, Vector3 along, Vector3 outward, float w, float d, float h)
            {
                var x = along * (w * 0.5f);
                var z = outward * (d * 0.5f);
                var y0 = baseCenter.y;
                var y1 = y0 + h;
                var c = new Vector3(baseCenter.x, 0f, baseCenter.z);
                var fl = c - x - z;
                var fr = c + x - z;
                var bl = c - x + z;
                var br = c + x + z;
                Wall(fl, fr, y0, y1, facingIn: true);
                Wall(bl, br, y0, y1, facingIn: false);
                Wall(fr, br, y0, y1, facingIn: true);
                Wall(fl, bl, y0, y1, facingIn: false);
                Flat(fl, fr, bl, br, y1);
            }

            void Add(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector2 u0, Vector2 u1, Vector2 u2, Vector2 u3, bool flip)
            {
                var i = _v.Count;
                _v.Add(p0); _v.Add(p1); _v.Add(p2); _v.Add(p3);
                _uv.Add(u0); _uv.Add(u1); _uv.Add(u2); _uv.Add(u3);
                if (flip) { _t.Add(i); _t.Add(i + 2); _t.Add(i + 1); _t.Add(i); _t.Add(i + 3); _t.Add(i + 2); }
                else { _t.Add(i); _t.Add(i + 1); _t.Add(i + 2); _t.Add(i); _t.Add(i + 2); _t.Add(i + 3); }
            }

            public void Mesh(string name, Transform parent, Material mat, bool shadows = true)
            {
                if (_v.Count == 0) return;
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(_v);
                mesh.SetUVs(0, _uv);
                mesh.SetTriangles(_t, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                mr.receiveShadows = true;
            }
        }
    }
}
