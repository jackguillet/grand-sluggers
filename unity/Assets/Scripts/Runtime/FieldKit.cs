using System;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The one field kit (FD-16, FR-13; #859): the diamond every park plays on — the home pad, the
    /// plate and its chalk boxes, the mound and rubber, the bags, the infield dirt with its home and
    /// bag cut-outs, the foul lines, the warning track, the poles and screens, and the wall loop
    /// (outfield fence, foul rail and backstop). Every position is read from the geometry owner —
    /// <see cref="Diamond"/>, <see cref="ParkDiamond"/>, <see cref="HomeSet"/> and the loop
    /// <see cref="HarborWall"/> builds from <see cref="ParkBoundary"/> — so the drawn rail is the rail
    /// the ball meets at every park (D15 as amended by D21, FD-06).
    ///
    /// <para>
    /// A plain builder, not a component, so the <c>HarborDiamond</c> scene's serialized
    /// <see cref="HarborKit"/> is untouched: <see cref="HarborKit"/> calls it for Harbor under its
    /// placed anchors and hangs Harbor's own dress on it; <see cref="ParkView"/> calls it for every
    /// other park under the park root. Nothing here names a park (FR-04).
    /// </para>
    ///
    /// <para>
    /// A park hands the kit its dirt, track and wall colors (<see cref="Skin"/>); where they are
    /// chosen does not move here (F6-c moves the choice into data). The chalk, the bags, the plate
    /// and the mound are the kit's own and the same at every park.
    /// </para>
    /// </summary>
    public sealed class FieldKit
    {
        /// <summary>What a park hands the kit. Each field is a material the park already draws today.</summary>
        public sealed class Skin
        {
            /// <summary>The infield dirt: paths, bag pads, the back arc.</summary>
            public Material Dirt;
            /// <summary>The packed pad around the plate.</summary>
            public Material HomeDirt;
            /// <summary>The warning track inside the whole wall loop.</summary>
            public Material Track;
            /// <summary>The wall's face, one per span of the loop in turn (a single entry is one color).</summary>
            public Material[] WallFaces;
            public Material WallCap;
            public Material Pole;
            public Material Screen;
        }

        /// <summary>One drawn span of the wall loop, handed to a dress that hangs on the wall.</summary>
        public readonly struct WallSpan
        {
            public readonly int Index;
            public readonly Vector3 A;
            public readonly Vector3 B;
            public readonly Vector3 Mid;
            public readonly Vector3 Outward;
            public readonly float H0;
            public readonly float H1;
            public readonly float Thick;

            public WallSpan(int index, Vector3 a, Vector3 b, Vector3 mid, Vector3 outward, float h0, float h1, float thick)
            {
                Index = index;
                A = a;
                B = b;
                Mid = mid;
                Outward = outward;
                H0 = h0;
                H1 = h1;
                Thick = thick;
            }
        }

        // Anchor names. Harbor's scene places the first nine; the kit finds or makes each one under its root.
        public const string HomeDirtName = "HomeDirt";
        public const string HomePlateName = "HomePlate";
        public const string HomePointName = "HomePoint";
        public const string BoxLName = "BoxL";
        public const string BoxRName = "BoxR";
        public const string FoulLName = "FoulL";
        public const string FoulRName = "FoulR";
        public const string MoundName = "Mound";
        public const string RubberName = "Rubber";
        public const string CatcherBoxName = "CatcherBox";
        public const string InfieldDirtName = "InfieldDirt";
        public const string TrackName = "WarningTrack";
        public const string PolesName = "Poles";
        public const string WallName = "Wall";

        public static string BagName(int bag) => bag + "B";

        /// <summary>
        /// The kit's meshes (<c>bag</c>, <c>home-plate</c>, <c>mound</c>) are in the default park's kit
        /// FBX until F6-b gives the kit slots of its own. They are the shared kit, drawn at every park;
        /// the dugouts and the fans in the same file are Harbor's dress and only <see cref="HarborKit"/>
        /// drops them.
        /// </summary>
        public const string MeshSlot = ExhibitionPick.DefaultPark;

        // Presentation offsets carried over from HarborKit as they were, named rather than inlined.
        // They size the drawing around a point the geometry owner gives; none of them moves a point.
        /// <summary>The home pad's center sits this far behind the plate, so the catcher's box is on dirt.</summary>
        const float HomePadBackFt = 1.2f;
        /// <summary>The home pad is an oval: this times its radius deep (it is twice its radius wide).</summary>
        const float HomePadDepthMul = 2.2f;
        /// <summary>White playing face clears the dirt by only 1/8 inch to avoid z-fighting.</summary>
        public const float PlateFaceClearanceFt = 1f / 96f;
        /// <summary>White face height in the authored FBX; the slab is embedded, not perched on dirt.</summary>
        const float AuthoredPlateFaceY = 0.22f;
        /// <summary>The primitive plate and bag slabs (when the kit FBX is missing).</summary>
        const float PlateSlabFt = 0.12f;
        const float BagSlabFt = 0.28f;
        /// <summary>A foul line runs this far past its pole, under the wall.</summary>
        const float FoulPastPoleFt = 4f;
        /// <summary>The wall cap overhangs each face by this much and stands this tall on the wall's top.</summary>
        const float CapOverhangFt = 0.25f;
        const float CapHeightFt = 0.45f;
        /// <summary>A span whose two ends are both within this of the rail's top is rail, not fence.</summary>
        const float RailSlackFt = 0.5f;

        readonly Transform _root;
        Material _chalk;
        Material _kitWood, _kitRoof, _kitGold, _kitPad, _kitPost, _kitFlesh, _kitChalk, _kitNavy, _kitDirt, _kitHill;

        /// <summary>The table this kit draws: its bags, rubber and dirt, and the edge its wall and rail stand on (#1190).</summary>
        readonly RulesTable _rules;
        readonly DiamondGeometry _diamond;

        public FieldKit(Transform root, RulesTable rules)
        {
            _root = root;
            _rules = rules;
            _diamond = rules != null ? DiamondGeometry.Of(rules) : null;
        }

        public Transform Root => _root;

        /// <summary>The named child of the kit's root, made at the origin when the scene has none.</summary>
        public Transform Anchor(string name)
        {
            if (_root == null) return null;
            var tf = _root.Find(name);
            if (tf != null) return tf;
            var go = new GameObject(name);
            tf = go.transform;
            tf.SetParent(_root, false);
            return tf;
        }

        Material Chalk => _chalk != null ? _chalk : _chalk = Look.Unlit(Colors.Chalk);

        /// <summary>
        /// The whole kit, in the order Harbor draws it, for a park whose dress stands beside the kit
        /// rather than on it. Harbor calls the pieces one by one instead, so its dress keeps its place
        /// between them (the lawn after the track, the ads on each wall span, the dugouts in the rail).
        /// </summary>
        public void Build(Park park, Skin skin)
        {
            if (park == null || skin == null) return;
            HomePad(skin.HomeDirt);
            Plate();
            Boxes();
            Mound();
            Bags();
            Track(park, skin.Track);
            InfieldDirt(skin.Dirt);
            FoulLines(park);
            Poles(park, skin.Pole, skin.Screen);
            Wall(park, skin.WallFaces, skin.WallCap);
        }

        /// <summary>The packed dirt around the plate (<see cref="ParkDiamond.HomePackedR"/>).</summary>
        public void HomePad(Material packed)
        {
            var pad = Anchor(HomeDirtName);
            var homeR = ParkDiamond.HomePackedR;
            Place(pad, new Vector3((float)Diamond.Home.X, ParkDiamond.PathY, (float)Diamond.Home.Z - HomePadBackFt),
                new Vector3(homeR * 2f, ParkDiamond.PathThick * 0.5f, homeR * HomePadDepthMul), Quaternion.identity);
            Wipe(pad);
            Mesh(pad, PrimitiveType.Cylinder, packed);
        }

        /// <summary>The kit's <c>home-plate</c> mesh at home, or a chalk pentagon from <see cref="HomeSet"/>.</summary>
        public void Plate()
        {
            var plate = Anchor(HomePlateName);
            var point = Anchor(HomePointName);
            // Embed the slab so its playing face, rather than its underside,
            // sits just above the dirt. The bag slabs retain their own height.
            var home = new Vector3((float)Diamond.Home.X, ParkDiamond.PathTop, (float)Diamond.Home.Z);
            Wipe(plate);
            Wipe(point);
            Place(plate, home, Vector3.one, Quaternion.identity);
            if (point != null) point.gameObject.SetActive(false);
            var scale = (float)HomeSet.PlateMeshScale;
            // FBX exports Blender +Y toward Unity -Z. Rotate about the authored
            // rear point so the wide edge faces the mound (+Z), as HomeSet does.
            if (DropMesh("home-plate", plate, "Mesh", home, Quaternion.Euler(0f, 180f, 0f),
                new Vector3(scale, 1f, scale), paint: true) is Transform mesh)
            {
                mesh.position += Vector3.up * (PlateFaceClearanceFt - AuthoredPlateFaceY);
                return;
            }
            plate.position += Vector3.up * (PlateFaceClearanceFt - PlateSlabFt);
            PrimitivePlate(plate);
        }

        // Missing art keeps the same pentagon, not two overlapping squares
        // whose corners extend outside the plate width and into the box gap.
        void PrimitivePlate(Transform plate)
        {
            var outline = HomeSet.PlateOutline();
            var n = outline.Length;
            var vertices = new Vector3[n * 2];
            for (var i = 0; i < n; i++)
            {
                vertices[i] = new Vector3((float)outline[i].X, 0f, (float)outline[i].Z);
                vertices[i + n] = vertices[i] + Vector3.up * PlateSlabFt;
            }
            var triangles = new int[(n - 2) * 6 + n * 6];
            var t = 0;
            for (var i = 1; i < n - 1; i++)
            {
                triangles[t++] = n; triangles[t++] = n + i; triangles[t++] = n + i + 1;
                triangles[t++] = 0; triangles[t++] = i + 1; triangles[t++] = i;
            }
            for (var i = 0; i < n; i++)
                t = Quad(triangles, t, i, (i + 1) % n, (i + 1) % n + n, i + n);
            Look.Solid("PrimitivePlate", plate, vertices, triangles, Chalk);
        }

        /// <summary>
        /// HomeSet chalk: six-foot-deep batter's boxes clear the zone-width plate
        /// by six inches without moving the batter centers; catcher's box is
        /// 8′×43″ on the rear line. Lines, not filled pads. Interior is dirt.
        /// </summary>
        public void Boxes()
        {
            var boxL = Anchor(BoxLName);
            var boxR = Anchor(BoxRName);
            Place(boxL,
                new Vector3((float)-HomeSet.BoxX, (float)HomeSet.BoxY, (float)HomeSet.BoxZ),
                Vector3.one, Quaternion.identity);
            Place(boxR,
                new Vector3((float)HomeSet.BoxX, (float)HomeSet.BoxY, (float)HomeSet.BoxZ),
                Vector3.one, Quaternion.identity);
            Wipe(boxL);
            Wipe(boxR);
            ChalkRect(boxL, (float)HomeSet.BoxW, (float)HomeSet.BoxD, Chalk);
            ChalkRect(boxR, (float)HomeSet.BoxW, (float)HomeSet.BoxD, Chalk);

            var catcher = Anchor(CatcherBoxName);
            Place(catcher,
                new Vector3(0f, (float)HomeSet.BoxY, (float)HomeSet.CatcherBoxZ),
                Vector3.one, Quaternion.identity);
            Wipe(catcher);
            ChalkRect(catcher, (float)HomeSet.CatcherBoxW, (float)HomeSet.CatcherBoxD, Chalk);
        }

        static void ChalkRect(Transform parent, float w, float d, Material chalk)
        {
            if (parent == null) return;
            var t = (float)HomeSet.ChalkW;
            var h = (float)ParkDiamond.FoulThick;
            Look.Prim(PrimitiveType.Cube, "N", parent, new Vector3(0f, 0f, d * 0.5f), new Vector3(w + t, h, t), chalk);
            Look.Prim(PrimitiveType.Cube, "S", parent, new Vector3(0f, 0f, -d * 0.5f), new Vector3(w + t, h, t), chalk);
            Look.Prim(PrimitiveType.Cube, "E", parent, new Vector3(w * 0.5f, 0f, 0f), new Vector3(t, h, d), chalk);
            Look.Prim(PrimitiveType.Cube, "W", parent, new Vector3(-w * 0.5f, 0f, 0f), new Vector3(t, h, d), chalk);
        }

        /// <summary>The kit's <c>mound</c> mesh on <see cref="DiamondGeometry.Mound"/>, or one smooth hill; the rubber on its table.</summary>
        public void Mound()
        {
            var mound = Anchor(MoundName);
            var rubber = Anchor(RubberName);
            var z = (float)_diamond.Mound;
            Place(mound, new Vector3(0f, 0f, z), Vector3.one, Quaternion.identity);
            Wipe(mound);
            if (DropMesh("mound", mound, "Mesh", new Vector3(0f, 0f, z), Quaternion.identity, Vector3.one, paint: true) == null)
            {
                Look.Prim(PrimitiveType.Sphere, "Hill", mound, Vector3.zero,
                    new Vector3(ParkDiamond.MoundR * 2f, ParkDiamond.MoundH * 2f, ParkDiamond.MoundR * 2f), Hill);
            }
            Place(rubber, new Vector3(0f, ParkDiamond.RubberY, z),
                new Vector3(ParkDiamond.RubberW, ParkDiamond.RubberH, ParkDiamond.RubberD), Quaternion.identity);
            Wipe(rubber);
            Mesh(rubber, PrimitiveType.Cube, Chalk);
        }

        /// <summary>The kit's <c>bag</c> mesh at each <see cref="ParkDiamond.BagVisual"/>, square to the diamond.</summary>
        public void Bags()
        {
            for (var bag = 1; bag <= 3; bag++)
                Bag(Anchor(BagName(bag)), ParkDiamond.BagVisual(bag, _diamond));
        }

        void Bag(Transform anchor, (double X, double Z) at)
        {
            if (anchor == null) return;
            var pos = new Vector3((float)at.X, 0f, (float)at.Z);
            Place(anchor, pos, Vector3.one, Quaternion.identity);
            Wipe(anchor);
            if (DropMesh("bag", anchor, "Mesh", pos, Quaternion.identity, Vector3.one, paint: true) != null)
                return;
            var bagSize = ParkDiamond.BagSize;
            Place(anchor, new Vector3((float)at.X, ParkDiamond.BagY, (float)at.Z),
                new Vector3(bagSize, BagSlabFt, bagSize), Quaternion.Euler(0f, ParkDiamond.FoulYaw(1), 0f));
            Mesh(anchor, PrimitiveType.Cube, Fill(HarborKitPaint.PrimitiveBag));
        }

        /// <summary>
        /// The warning track: an annulus inside the whole wall loop, from
        /// <see cref="HarborWall.TrackInner"/> to <see cref="HarborWall.TrackOuter"/>.
        /// </summary>
        public void Track(Park park, Material dirt)
        {
            var folder = Anchor(TrackName);
            Wipe(folder);
            if (park == null) return;
            var n = HarborWall.Loop(park, _rules).Length;
            var y0 = ParkDiamond.TrackY - ParkDiamond.TrackThick * 0.5f;
            var y1 = ParkDiamond.TrackY + ParkDiamond.TrackThick * 0.5f;
            var verts = new Vector3[n * 4];
            var uvs = new Vector2[n * 4];
            for (var i = 0; i < n; i++)
            {
                var inn = HarborWall.TrackInner(park, i, _rules);
                var outt = HarborWall.TrackOuter(park, i, _rules);
                var inner = new Vector3((float)inn.X, 0f, (float)inn.Z);
                var outer = new Vector3((float)outt.X, 0f, (float)outt.Z);
                var u = i / (float)n;
                verts[i * 4 + 0] = inner + Vector3.up * y0;
                verts[i * 4 + 1] = inner + Vector3.up * y1;
                verts[i * 4 + 2] = outer + Vector3.up * y0;
                verts[i * 4 + 3] = outer + Vector3.up * y1;
                uvs[i * 4 + 0] = new Vector2(u * 8f, 0f);
                uvs[i * 4 + 1] = new Vector2(u * 8f, 0.08f);
                uvs[i * 4 + 2] = new Vector2(u * 8f, 1f);
                uvs[i * 4 + 3] = new Vector2(u * 8f, 0.92f);
            }
            var tris = new int[n * 24];
            var t = 0;
            for (var i = 0; i < n; i++)
            {
                var a = i * 4;
                var b = ((i + 1) % n) * 4;
                t = Quad(tris, t, a + 1, a + 3, b + 3, b + 1);
                t = Quad(tris, t, a + 0, a + 1, b + 1, b + 0);
                t = Quad(tris, t, a + 3, a + 2, b + 2, b + 3);
                t = Quad(tris, t, a + 2, a + 0, b + 0, b + 2);
            }
            var go = Look.Solid("Track", folder, verts, tris, dirt);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            mesh.uv = uvs;
        }

        /// <summary>
        /// One rounded-diamond dirt skin (offset of the grass Y). Paths and bag
        /// pads are the same shape — Minkowski offset, not slabs butted to circles.
        /// </summary>
        public void InfieldDirt(Material dirt)
        {
            if (_root == null) return;
            var old = _root.Find(InfieldDirtName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var outer = ParkDiamond.OuterVerts(_diamond);
            var nPts = outer.Length;
            if (nPts < 8) return;
            var yTop = ParkDiamond.PathTop;
            var yBot = ParkDiamond.PathBottom;
            var verts = new Vector3[nPts * 4];
            for (var i = 0; i < nPts; i++)
            {
                var o = outer[i];
                var inn = ParkDiamond.InnerOnRay(o.X, o.Z, _diamond);
                verts[i] = new Vector3((float)inn.X, yTop, (float)inn.Z);
                verts[nPts + i] = new Vector3((float)o.X, yTop, (float)o.Z);
                verts[nPts * 2 + i] = new Vector3((float)inn.X, yBot, (float)inn.Z);
                verts[nPts * 3 + i] = new Vector3((float)o.X, yBot, (float)o.Z);
            }
            var tris = new int[nPts * 24];
            var t = 0;
            for (var i = 0; i < nPts; i++)
            {
                var j = (i + 1) % nPts;
                // top, CCW from above
                tris[t++] = i; tris[t++] = j; tris[t++] = nPts + j;
                tris[t++] = i; tris[t++] = nPts + j; tris[t++] = nPts + i;
                // bottom, CW from above
                tris[t++] = nPts * 2 + i; tris[t++] = nPts * 3 + i; tris[t++] = nPts * 3 + j;
                tris[t++] = nPts * 2 + i; tris[t++] = nPts * 3 + j; tris[t++] = nPts * 2 + j;
                // outer wall
                tris[t++] = nPts + i; tris[t++] = nPts + j; tris[t++] = nPts * 3 + j;
                tris[t++] = nPts + i; tris[t++] = nPts * 3 + j; tris[t++] = nPts * 3 + i;
                // inner wall
                tris[t++] = i; tris[t++] = nPts * 2 + j; tris[t++] = j;
                tris[t++] = i; tris[t++] = nPts * 2 + i; tris[t++] = nPts * 2 + j;
            }
            Look.Solid(InfieldDirtName, _root, verts, tris, dirt);
        }

        /// <summary>
        /// Chalk from past the batter's box (<see cref="HomeSet.FoulLineStartAlong"/>) to just past each
        /// side's own pole, the fair edge on the foul line itself, x = ±z (<see cref="ParkDiamond.FoulLineCenter"/>).
        /// </summary>
        public void FoulLines(Park park)
        {
            if (park == null) return;
            FoulLine(Anchor(FoulLName), park, -1);
            FoulLine(Anchor(FoulRName), park, 1);
        }

        void FoulLine(Transform line, Park park, int sign)
        {
            var pole = ParkDiamond.FoulPole(park, sign);
            var end = (float)Diamond.Dist(0, 0, pole.X, pole.Z) + FoulPastPoleFt;
            var start = (float)HomeSet.FoulLineStartAlong;
            var len = end - start;
            if (len < 4f) return;
            var mid = start + len * 0.5f;
            var c = ParkDiamond.FoulLineCenter(sign, mid);
            Place(line, new Vector3(c.X, ParkDiamond.FoulY, c.Z),
                new Vector3(ParkDiamond.FoulWidth, ParkDiamond.FoulThick, len), Quaternion.Euler(0f, ParkDiamond.FoulYaw(sign), 0f));
            Wipe(line);
            Mesh(line, PrimitiveType.Cube, Chalk);
        }

        /// <summary>Each pole on its side's fence (<see cref="ParkDiamond.FoulPole"/>), its grate facing fair.</summary>
        public void Poles(Park park, Material pole, Material screen)
        {
            var folder = Anchor(PolesName);
            Wipe(folder);
            if (park == null) return;
            for (var sign = -1; sign <= 1; sign += 2)
            {
                var pz = ParkDiamond.FoulPole(park, sign);
                var pos = new Vector3((float)pz.X, 0f, (float)pz.Z);
                var name = sign < 0 ? "L" : "R";
                var fair = ParkDiamond.FairInward(sign);
                var fairV = new Vector3((float)fair.X, 0f, (float)fair.Z);
                // Face the grate toward fair: local Z is the thin axis, so the
                // big face looks at the diamond. Offset the panel onto the fair side.
                var rot = Quaternion.LookRotation(fairV, Vector3.up);
                Cylinder(folder, "Pole" + name, pos, ParkDiamond.PoleRadius, ParkDiamond.PoleHeight, pole);
                Sphere(folder, "Ball" + name, pos + Vector3.up * ParkDiamond.PoleHeight, ParkDiamond.PoleRadius * 1.25f, pole);
                Box(folder, "Screen" + name,
                    pos + Vector3.up * ParkDiamond.PoleScreenY
                        + fairV * (ParkDiamond.PoleScreenThick * 0.5f + ParkDiamond.PoleRadius),
                    new Vector3(ParkDiamond.PoleScreenW, ParkDiamond.PoleScreenH, ParkDiamond.PoleScreenThick), rot, screen);
            }
        }

        /// <summary>
        /// The wall loop, once around (<see cref="HarborWall.Loop"/>): each span a prism from the top of
        /// the flight's wall under it at one end to its top at the other (<see cref="HarborWall.SpanTops"/>)
        /// — the park's fence in the outfield (D15; a polyline span's two point heights, straight between
        /// them, F2-c), the rail's top along the foul wrap to each pole and behind the plate (FD-06-R2) —
        /// with a cap on top. Level wherever the two ends match, which is every span of a park with no
        /// polyline.
        ///
        /// <para>
        /// A span takes its own tops, not the tops of its two end vertices. The span that leaves a pole
        /// starts at a fence vertex and is rail, so reading its end vertices would stretch the step at the
        /// pole into a steep ramp over one sample; reading the span draws the fence to the pole and the
        /// rail from it, a vertical step where the pole and its screen stand and the ball's wall steps.
        /// </para>
        ///
        /// <para>
        /// <paramref name="opens"/> is a dress that stands in the rail and draws it there itself (Harbor's
        /// dugout fascia is the rail along its pit): a rail span whose middle it claims is left to it. A
        /// park with no such dress passes nothing and its rail runs unbroken, as the ball meets it.
        /// <paramref name="dress"/> is called for each drawn span after its prism and cap.
        /// </para>
        /// </summary>
        public void Wall(Park park, Material[] faces, Material cap,
            Func<double, double, bool> opens = null, Action<WallSpan> dress = null)
        {
            var folder = Anchor(WallName);
            Wipe(folder);
            if (park == null || faces == null || faces.Length == 0) return;
            var thick = HarborPostcard.WallThickFt;
            var half = thick * 0.5f;
            var n = HarborWall.Loop(park, _rules).Length;
            for (var i = 0; i < n; i++)
            {
                var p0 = HarborWall.LoopPoint(park, i, _rules);
                var p1 = HarborWall.LoopPoint(park, i + 1, _rules);
                var mid = new Vector3((float)((p0.X + p1.X) * 0.5), 0f, (float)((p0.Z + p1.Z) * 0.5));
                var (h0, h1) = HarborWall.SpanTops(park, i, _rules);
                // Skip the claimed span only. A vertex sits on each end of a claim, so the
                // span beside it butts the dress instead of leaving a 16-ft gap.
                var claimed = opens != null && opens((p0.X + p1.X) * 0.5, (p0.Z + p1.Z) * 0.5);
                if (h0 <= HarborWall.HipHeight(_rules) + RailSlackFt && h1 <= HarborWall.HipHeight(_rules) + RailSlackFt && claimed)
                    continue;
                var o = HarborWall.Outward(park, i, _rules);
                var outward = new Vector3((float)o.X, 0f, (float)o.Z);
                var a = new Vector3((float)p0.X, 0f, (float)p0.Z);
                var b = new Vector3((float)p1.X, 0f, (float)p1.Z);
                RampPrism(folder, "Wall" + i, a, b, h0, h1, half, outward, faces[i % faces.Length]);
                RampCap(folder, "Cap" + i, a, b, h0, h1, half + CapOverhangFt, outward, cap);
                dress?.Invoke(new WallSpan(i, a, b, mid, outward, h0, h1, thick));
            }
        }

        /// <summary>Wall segment whose top slopes from h0 to h1 — a ramp, not a stair.</summary>
        static void RampPrism(Transform parent, string name, Vector3 a, Vector3 b, float h0, float h1, float half, Vector3 outward, Material mat)
        {
            var in0 = a - outward * half;
            var out0 = a + outward * half;
            var in1 = b - outward * half;
            var out1 = b + outward * half;
            var verts = new[]
            {
                in0, out0, in0 + Vector3.up * h0, out0 + Vector3.up * h0,
                in1, out1, in1 + Vector3.up * h1, out1 + Vector3.up * h1
            };
            // 0 in-bot, 1 out-bot, 2 in-top, 3 out-top at A; 4–7 at B.
            var tris = new int[36];
            var t = 0;
            t = Quad(tris, t, 0, 2, 6, 4); // inner (field) face
            t = Quad(tris, t, 1, 5, 7, 3); // outer face
            t = Quad(tris, t, 2, 3, 7, 6); // top ramp
            t = Quad(tris, t, 0, 4, 5, 1); // bottom
            t = Quad(tris, t, 0, 1, 3, 2); // A end
            t = Quad(tris, t, 4, 6, 7, 5); // B end
            Look.Solid(name, parent, verts, tris, mat);
        }

        static void RampCap(Transform parent, string name, Vector3 a, Vector3 b, float h0, float h1, float half, Vector3 outward, Material mat)
        {
            var in0 = a - outward * half + Vector3.up * h0;
            var out0 = a + outward * half + Vector3.up * h0;
            var in1 = b - outward * half + Vector3.up * h1;
            var out1 = b + outward * half + Vector3.up * h1;
            var lift = Vector3.up * CapHeightFt;
            var verts = new[] { in0, out0, in0 + lift, out0 + lift, in1, out1, in1 + lift, out1 + lift };
            var tris = new int[36];
            var t = 0;
            t = Quad(tris, t, 0, 2, 6, 4);
            t = Quad(tris, t, 1, 5, 7, 3);
            t = Quad(tris, t, 2, 3, 7, 6);
            t = Quad(tris, t, 0, 4, 5, 1);
            t = Quad(tris, t, 0, 1, 3, 2);
            t = Quad(tris, t, 4, 6, 7, 5);
            Look.Solid(name, parent, verts, tris, mat);
        }

        // ---- The kit FBX: one mesh source and one paint table for every mesh in it ----

        /// <summary>
        /// A named mesh from the kit FBX (<see cref="MeshSlot"/>), placed under <paramref name="parent"/>.
        /// Null when the file or the mesh is missing, and the caller keeps its primitive.
        /// </summary>
        public Transform DropMesh(string meshName, Transform parent, string instanceName, Vector3 pos, Quaternion rot, Vector3 scale, bool paint)
        {
            var src = ArtBinder.LoadParkMesh(MeshSlot, meshName);
            if (src == null) return null;
            var go = UnityEngine.Object.Instantiate(src, parent);
            go.name = instanceName;
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
            foreach (var anim in go.GetComponentsInChildren<Animator>(true))
                anim.enabled = false;
            if (go.GetComponentInChildren<MeshRenderer>(true) == null)
            {
                UnityEngine.Object.Destroy(go);
                return null;
            }
            if (paint) PaintKit(go.transform, meshName);
            return go.transform;
        }

        /// <summary>
        /// One fill of the kit's paint. Unity drops Blender material names on import, so every kit mesh
        /// is painted from <see cref="HarborKitPaint"/> by its slot and whatever name survives (#683).
        /// </summary>
        public Material Fill(HarborKitPaint.Fill fill)
        {
            EnsureKitMats();
            return fill switch
            {
                HarborKitPaint.Fill.Roof => _kitRoof,
                HarborKitPaint.Fill.Gold => _kitGold,
                HarborKitPaint.Fill.Pad => _kitPad,
                HarborKitPaint.Fill.Post => _kitPost,
                HarborKitPaint.Fill.Flesh => _kitFlesh,
                HarborKitPaint.Fill.Chalk => _kitChalk,
                HarborKitPaint.Fill.Navy => _kitNavy,
                HarborKitPaint.Fill.Dirt => _kitDirt,
                _ => _kitWood,
            };
        }

        void EnsureKitMats()
        {
            if (_kitWood != null) return;
            _kitWood = Look.Toon(new Color(0.42f, 0.26f, 0.12f));
            _kitRoof = Look.Toon(new Color(0.14f, 0.32f, 0.20f));
            _kitGold = Look.Toon(Colors.Gold);
            _kitPad = Look.Toon(new Color(0.16f, 0.42f, 0.28f));
            _kitPost = Look.Toon(new Color(0.28f, 0.22f, 0.16f));
            _kitFlesh = Look.Toon(new Color(1f, 0.80f, 0.68f));
            _kitChalk = Look.Unlit(Colors.Chalk);
            _kitNavy = Look.Toon(new Color(0.06f, 0.18f, 0.42f));
            _kitDirt = Look.Lit(new Color(0.70f, 0.48f, 0.28f), Look.Dirt, 8f, 0.1f);
        }

        /// <summary>The primitive mound's dirt, when the kit FBX has no <c>mound</c>.</summary>
        Material Hill => _kitHill != null ? _kitHill : _kitHill = Look.Lit(new Color(0.66f, 0.44f, 0.26f), Look.Dirt, 3f, 0.1f);

        void PaintKit(Transform t, string slot)
        {
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                if (mats == null || mats.Length == 0)
                {
                    r.sharedMaterial = Fill(HarborKitPaint.For(slot, ""));
                    continue;
                }
                var next = new Material[mats.Length];
                for (var i = 0; i < mats.Length; i++)
                    next[i] = Fill(HarborKitPaint.For(slot, mats[i] != null ? mats[i].name : ""));
                r.sharedMaterials = next;
            }
        }

        // ---- Primitive helpers ----

        static void Mesh(Transform anchor, PrimitiveType type, Material mat)
        {
            if (anchor == null) return;
            if (anchor.childCount > 0 && anchor.GetComponentInChildren<MeshRenderer>() != null) return;
            Look.Prim(type, "Mesh", anchor, Vector3.zero, Vector3.one, mat);
        }

        static void Place(Transform tf, Vector3 pos, Vector3 scale, Quaternion rot)
        {
            if (tf == null) return;
            tf.position = pos;
            tf.localScale = scale;
            tf.rotation = rot;
        }

        static void Wipe(Transform tf)
        {
            if (tf == null) return;
            for (var i = tf.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(tf.GetChild(i).gameObject);
        }

        static int Quad(int[] tris, int t, int a, int b, int c, int d)
        {
            tris[t++] = a;
            tris[t++] = b;
            tris[t++] = c;
            tris[t++] = a;
            tris[t++] = c;
            tris[t++] = d;
            return t;
        }

        static void Box(Transform parent, string name, Vector3 pos, Vector3 scale, Quaternion rot, Material mat)
        {
            if (parent == null) return;
            var go = Look.Prim(PrimitiveType.Cube, name, parent, Vector3.zero, Vector3.one, mat);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
        }

        static void Sphere(Transform parent, string name, Vector3 pos, float radius, Material mat)
        {
            if (parent == null) return;
            var go = Look.Prim(PrimitiveType.Sphere, name, parent, Vector3.zero, Vector3.one, mat);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * (radius * 2f);
        }

        static void Cylinder(Transform parent, string name, Vector3 pos, float radius, float height, Material mat)
        {
            if (parent == null) return;
            var go = Look.Prim(PrimitiveType.Cylinder, name, parent, Vector3.zero, Vector3.one, mat);
            go.transform.position = pos + new Vector3(0, height * 0.5f, 0);
            go.transform.localScale = new Vector3(radius * 2, height * 0.5f, radius * 2);
        }
    }
}
