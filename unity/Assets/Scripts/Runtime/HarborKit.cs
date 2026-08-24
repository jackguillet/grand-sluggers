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
        public Transform WarningTrack;
        public Transform Backstop;
        public Transform Dugouts;
        public Transform WallDress;
        public Transform Grass;
        public Transform Scoreboard;
        public Transform Bleachers;
        public Transform Town;
        public Transform Fireworks;
        public Transform Bag1;
        public Transform Bag2;
        public Transform Bag3;

        bool _dressed;
        CameraShots _shots;
        Park _park;
        bool _night;
        Transform _awayTens, _awayOnes, _homeTens, _homeOnes, _innDigit;
        Transform _plateAwayTens, _plateAwayOnes, _plateHomeTens, _plateHomeOnes;
        Material _ledOn;
        Material _ledOff;
        readonly Firework[] _sparks = new Firework[28];
        public bool OwnsDiamond { get; private set; }

        struct Firework
        {
            public Transform Tf;
            public Vector3 Vel;
            public float Life;
        }

        void Awake()
        {
            Instance = this;
            EnsureAnchors();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Bind(Park park, bool night = false)
        {
            _park = park;
            _night = night;
            OwnsDiamond = park != null && park.Id == "harbor-diamond";
            gameObject.SetActive(OwnsDiamond);
            if (OwnsDiamond)
            {
                Dress();
                ApplyNight();
                ApplyShots();
            }
        }

        public void SyncShots(CameraShots shots)
        {
            _shots = shots;
            if (OwnsDiamond) ApplyShots();
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
            DirtPad = Anchor("DirtPad", new Vector3(0f, 0.04f, 64f), new Vector3(92f, 0.18f, 92f), Quaternion.identity);
            HomeDirt = Anchor("HomeDirt", new Vector3(0f, 0.16f, 0f), new Vector3(36f, 0.08f, 36f), Quaternion.identity);
            DirtDiamond = Anchor("DirtDiamond", new Vector3(0f, 0.12f, 63.64f), new Vector3(100f, 0.24f, 100f), Quaternion.Euler(0f, 45f, 0f));
            HomePlate = Anchor("HomePlate", new Vector3(0f, 0.22f, 0.15f), new Vector3(1.42f, 0.12f, 1.05f), Quaternion.identity);
            HomePoint = Anchor("HomePoint", new Vector3(0f, 0.22f, 0.88f), new Vector3(1.02f, 0.12f, 1.02f), Quaternion.Euler(0f, 45f, 0f));
            BoxL = Anchor("BoxL", new Vector3(-2.72f, 0.20f, 3.05f), new Vector3(4.0f, 0.12f, 6.1f), Quaternion.identity);
            BoxR = Anchor("BoxR", new Vector3(2.72f, 0.20f, 3.05f), new Vector3(4.0f, 0.12f, 6.1f), Quaternion.identity);
            FoulL = Anchor("FoulL", new Vector3(-63.64f, 0.18f, 63.64f), new Vector3(0.48f, 0.14f, 186f), Quaternion.Euler(0f, -45f, 0f));
            FoulR = Anchor("FoulR", new Vector3(63.64f, 0.18f, 63.64f), new Vector3(0.48f, 0.14f, 186f), Quaternion.Euler(0f, 45f, 0f));
            Mound = Anchor("Mound", new Vector3(0f, 0f, 60.5f), Vector3.one, Quaternion.identity);
            Rubber = Anchor("Rubber", new Vector3(0f, 1.02f, 60.5f), new Vector3(1.7f, 0.07f, 0.42f), Quaternion.identity);
            ShotPlate = ShotAnchor("ShotPlate", new Vector3(8.4f, 6.1f, -4.8f), new Vector3(2.6f, 2.6f, 48f), 54f);
            ShotMound = ShotAnchor("ShotMound", new Vector3(9.8f, 6.4f, 76.5f), new Vector3(0.4f, 2.2f, 1.0f), 42f);
            ShotDiamond = ShotAnchor("ShotDiamond", new Vector3(20f, 20f, 55f), new Vector3(0f, 14f, 220f), 48f);
            ShotThrow = ShotAnchor("ShotThrow", new Vector3(0f, 6.2f, -14f), new Vector3(0f, 1.4f, 0f), 40f);
            WarningTrack = Folder("WarningTrack");
            Backstop = Folder("Backstop");
            Dugouts = Folder("Dugouts");
            WallDress = Folder("WallDress");
            Grass = Folder("Grass");
            Scoreboard = Folder("Scoreboard");
            Bleachers = Folder("Bleachers");
            Town = Folder("Town");
            Fireworks = Folder("Fireworks");
            Bag1 = Anchor("1B", new Vector3((float)Diamond.First.X, 0.28f, (float)Diamond.First.Z), new Vector3(2.4f, 0.4f, 2.4f), Quaternion.identity);
            Bag2 = Anchor("2B", new Vector3((float)Diamond.Second.X, 0.28f, (float)Diamond.Second.Z), new Vector3(2.4f, 0.4f, 2.4f), Quaternion.identity);
            Bag3 = Anchor("3B", new Vector3((float)Diamond.Third.X, 0.28f, (float)Diamond.Third.Z), new Vector3(2.4f, 0.4f, 2.4f), Quaternion.identity);
        }

        public void Dress()
        {
            if (_dressed) return;
            EnsureAnchors();
            DressDiamond();
            var bag = Look.Unlit(Colors.Chalk);
            Mesh(Bag1, PrimitiveType.Cube, bag);
            Mesh(Bag2, PrimitiveType.Cube, bag);
            Mesh(Bag3, PrimitiveType.Cube, bag);
            DressPlace();
            _dressed = true;
        }

        /// <summary>
        /// SMS diamond language from the title still: dirt *paths* and pads,
        /// grass in the Y, mound as a hill, two white boxes + pentagon at home.
        /// </summary>
        void DressDiamond()
        {
            var chalk = Look.Unlit(Colors.Chalk);
            var packed = Look.Lit(new Color(0.78f, 0.56f, 0.34f), Look.Dirt, 5f, 0.12f);
            var path = Look.Lit(new Color(0.70f, 0.48f, 0.28f), Look.Dirt, 8f, 0.1f);
            var boxDirt = Look.Lit(new Color(0.62f, 0.42f, 0.24f), Look.Dirt, 4f, 0.08f);
            var hill = Look.Lit(new Color(0.66f, 0.44f, 0.26f), Look.Dirt, 3f, 0.1f);
            var cut = Look.Lit(Colors.Cut, Look.Grass, 12f, 0.08f);

            // Kill the 100-ft dirt slab that ate the infield grass.
            Place(DirtPad, new Vector3(0f, 0.04f, 2f), new Vector3(0.2f, 0.02f, 0.2f), Quaternion.identity);
            if (DirtPad != null) DirtPad.gameObject.SetActive(false);
            Place(DirtDiamond, new Vector3(0f, 0.05f, 63.64f), new Vector3(0.2f, 0.02f, 0.2f), Quaternion.Euler(0f, 45f, 0f));
            if (DirtDiamond != null) DirtDiamond.gameObject.SetActive(false);

            Place(HomeDirt, new Vector3(0f, 0.10f, 1.4f), new Vector3(24f, 0.12f, 24f), Quaternion.identity);
            Wipe(HomeDirt);
            Mesh(HomeDirt, PrimitiveType.Cylinder, packed);

            // Infield lawn under the dirt paths — the SMS "Y" of grass.
            Slab(Grass, "InfieldLawn", new Vector3(0f, 0.05f, 63.64f), new Vector3(108f, 0.08f, 108f), Quaternion.Euler(0f, 45f, 0f), cut);

            var home = Vector3.zero;
            var first = new Vector3((float)Diamond.First.X, 0f, (float)Diamond.First.Z);
            var second = new Vector3((float)Diamond.Second.X, 0f, (float)Diamond.Second.Z);
            var third = new Vector3((float)Diamond.Third.X, 0f, (float)Diamond.Third.Z);
            DirtPath("PathHome1", home, first, 11f, path);
            DirtPath("Path1to2", first, second, 11f, path);
            DirtPath("Path2to3", second, third, 11f, path);
            DirtPath("Path3toHome", third, home, 11f, path);

            // Pentagon: 17" back edge toward the catcher, point toward the pitcher (+Z).
            Place(HomePlate, new Vector3(0f, 0.22f, 0.15f), new Vector3(1.42f, 0.12f, 1.05f), Quaternion.identity);
            Place(HomePoint, new Vector3(0f, 0.22f, 0.88f), new Vector3(1.02f, 0.12f, 1.02f), Quaternion.Euler(0f, 45f, 0f));
            Wipe(HomePlate);
            Wipe(HomePoint);
            Mesh(HomePlate, PrimitiveType.Cube, chalk);
            Mesh(HomePoint, PrimitiveType.Cube, chalk);

            // 4×6 ft boxes, dirt fill, thick unlit chalk so they read from the mound.
            Place(BoxL, new Vector3(-2.72f, 0.20f, 3.05f), new Vector3(4.0f, 0.12f, 6.1f), Quaternion.identity);
            Place(BoxR, new Vector3(2.72f, 0.20f, 3.05f), new Vector3(4.0f, 0.12f, 6.1f), Quaternion.identity);
            Wipe(BoxL);
            Wipe(BoxR);
            Mesh(BoxL, PrimitiveType.Cube, chalk);
            Mesh(BoxR, PrimitiveType.Cube, chalk);
            Look.Prim(PrimitiveType.Cube, "BoxLIn", BoxL, Vector3.zero, new Vector3(0.78f, 0.70f, 0.88f), boxDirt);
            Look.Prim(PrimitiveType.Cube, "BoxRIn", BoxR, Vector3.zero, new Vector3(0.78f, 0.70f, 0.88f), boxDirt);

            Place(FoulL, new Vector3(-63.64f, 0.18f, 63.64f), new Vector3(0.48f, 0.14f, 186f), Quaternion.Euler(0f, -45f, 0f));
            Place(FoulR, new Vector3(63.64f, 0.18f, 63.64f), new Vector3(0.48f, 0.14f, 186f), Quaternion.Euler(0f, 45f, 0f));
            Wipe(FoulL);
            Wipe(FoulR);
            Mesh(FoulL, PrimitiveType.Cube, chalk);
            Mesh(FoulR, PrimitiveType.Cube, chalk);

            Place(Mound, new Vector3(0f, 0f, 60.5f), Vector3.one, Quaternion.identity);
            Wipe(Mound);
            Cylinder(Mound, "HillPad", new Vector3(0f, 0f, 60.5f), 9.2f, 0.22f, packed);
            Cylinder(Mound, "HillMid", new Vector3(0f, 0.18f, 60.5f), 6.4f, 0.38f, hill);
            Cylinder(Mound, "HillTop", new Vector3(0f, 0.48f, 60.5f), 4.1f, 0.42f, hill);
            Place(Rubber, new Vector3(0f, 1.02f, 60.5f), new Vector3(1.7f, 0.07f, 0.42f), Quaternion.identity);
            Wipe(Rubber);
            Mesh(Rubber, PrimitiveType.Cube, chalk);
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

        void ApplyShots()
        {
            if (_shots == null) return;
            PlaceShot(ShotPlate, "plate");
            PlaceShot(ShotMound, "mound");
            PlaceShot(ShotDiamond, "diamond");
            PlaceShot(ShotThrow, "throw");
        }

        void PlaceShot(Transform tf, string id)
        {
            if (tf == null || !_shots.TryGet(id, out var s)) return;
            var pos = new Vector3((float)s.Pos.X, (float)s.Pos.Y, (float)s.Pos.Z);
            var look = new Vector3((float)s.Target.X, (float)s.Target.Y, (float)s.Target.Z);
            tf.position = pos;
            tf.localScale = new Vector3((float)s.Fov, 1f, 1f);
            tf.LookAt(look);
            var aim = tf.Find("Look");
            if (aim != null) aim.position = look;
        }

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

        static void Slab(Transform parent, string name, Vector3 pos, Vector3 scale, Quaternion rot, Material mat)
        {
            if (parent == null) return;
            if (parent.Find(name) != null) return;
            var go = Look.Prim(PrimitiveType.Cube, name, parent, Vector3.zero, Vector3.one, mat);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
        }

        void DirtPath(string name, Vector3 a, Vector3 b, float width, Material dirt)
        {
            var d = b - a;
            d.y = 0f;
            if (d.sqrMagnitude < 1f) return;
            var mid = (a + b) * 0.5f;
            mid.y = 0.11f;
            Slab(transform, name, mid, new Vector3(width, 0.16f, d.magnitude + 6f), Quaternion.LookRotation(d.normalized, Vector3.up), dirt);
        }

        Transform Folder(string name)
        {
            var tf = transform.Find(name);
            if (tf != null) return tf;
            var go = new GameObject(name);
            tf = go.transform;
            tf.SetParent(transform, false);
            return tf;
        }

        void DressPlace()
        {
            if (_park == null) return;
            var dirt = Look.Lit(new Color(0.72f, 0.52f, 0.32f), Look.Dirt, 6f, 0.1f);
            var bagDirt = Look.Lit(Colors.Dirt, Look.Dirt, 10f, 0.1f);
            Cylinder(transform, "BagDirt1", new Vector3((float)Diamond.First.X, 0.08f, (float)Diamond.First.Z), 11f, 0.16f, bagDirt);
            Cylinder(transform, "BagDirt2", new Vector3((float)Diamond.Second.X, 0.08f, (float)Diamond.Second.Z), 11f, 0.16f, bagDirt);
            Cylinder(transform, "BagDirt3", new Vector3((float)Diamond.Third.X, 0.08f, (float)Diamond.Third.Z), 11f, 0.16f, bagDirt);
            DressTrack(dirt);
            DressGrass();
            DressBackstop();
            DressDugouts();
            DressWall();
            DressScoreboard();
            DressBleachers();
            DressTown();
            DressNight();
        }

        void DressTrack(Material dirt)
        {
            for (var i = -18; i <= 18; i++)
            {
                var spray = i / 18f * 48f;
                var fence = (float)AtBatResolver.FenceAt(_park, spray) - 12f;
                var rad = spray * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Sin(rad) * fence, 0.14f, Mathf.Cos(rad) * fence);
                Cube(WarningTrack, "Track" + i, p, new Vector3(16, 0.2f, 10f), dirt);
            }
        }

        void DressBackstop()
        {
            var steel = Look.Lit(new Color(0.22f, 0.24f, 0.26f), smooth: 0.28f);
            var pad = Look.Lit(new Color(0.14f, 0.15f, 0.16f), smooth: 0.08f);
            Cube(Backstop, "RailTop", new Vector3(0, 16.2f, -22f), new Vector3(42, 0.32f, 0.55f), steel);
            Cube(Backstop, "RailBot", new Vector3(0, 1.15f, -22f), new Vector3(42, 0.4f, 0.7f), pad);
            Cylinder(Backstop, "PostL", new Vector3(-21, 0, -22), 0.38f, 16.4f, steel);
            Cylinder(Backstop, "PostR", new Vector3(21, 0, -22), 0.38f, 16.4f, steel);
            for (var i = -5; i <= 5; i++)
                Cylinder(Backstop, "Pipe" + i, new Vector3(i * 3.6f, 0, -22), 0.07f, 15.6f, steel);
            for (var r = 0; r < 8; r++)
                Cube(Backstop, "Bar" + r, new Vector3(0, 2.5f + r * 1.65f, -22f), new Vector3(40.5f, 0.07f, 0.1f), steel);
            for (var i = 0; i < 5; i++)
            {
                Cylinder(Backstop, "WingL" + i, new Vector3(-22, 0, -20 + i * 3.2f), 0.07f, 14f, steel);
                Cylinder(Backstop, "WingR" + i, new Vector3(22, 0, -20 + i * 3.2f), 0.07f, 14f, steel);
            }
        }

        void DressDugouts()
        {
            var roof = Look.Lit(new Color(0.18f, 0.32f, 0.22f), smooth: 0.12f);
            var pad = Look.Lit(new Color(0.55f, 0.42f, 0.28f), Look.Dirt, 2f, 0.1f);
            var toonRoof = Look.Toon(new Color(0.16f, 0.28f, 0.2f));
            var rail = Look.Toon(Colors.Gold);
            var bench = Look.Lit(new Color(0.45f, 0.28f, 0.12f), smooth: 0.1f);
            Cube(Dugouts, "Dugout1B", new Vector3(42, 2.4f, 22), new Vector3(22, 1.2f, 10), roof);
            Cube(Dugouts, "Dugout1BPad", new Vector3(42, 0.3f, 22), new Vector3(20, 0.4f, 8), pad);
            Cube(Dugouts, "Dugout3B", new Vector3(-42, 2.4f, 22), new Vector3(22, 1.2f, 10), roof);
            Cube(Dugouts, "Dugout3BPad", new Vector3(-42, 0.3f, 22), new Vector3(20, 0.4f, 8), pad);
            Cube(Dugouts, "Dug1Roof", new Vector3(42f, 5.2f, 22f), new Vector3(24f, 0.5f, 12f), toonRoof);
            Cube(Dugouts, "Dug3Roof", new Vector3(-42f, 5.2f, 22f), new Vector3(24f, 0.5f, 12f), toonRoof);
            Cube(Dugouts, "Dug1Rail", new Vector3(42f, 3.4f, 16f), new Vector3(20f, 0.35f, 0.5f), rail);
            Cube(Dugouts, "Dug3Rail", new Vector3(-42f, 3.4f, 16f), new Vector3(20f, 0.35f, 0.5f), rail);
            Cube(Dugouts, "Dug1Bench", new Vector3(42f, 1.0f, 24f), new Vector3(18f, 0.7f, 2.2f), bench);
            Cube(Dugouts, "Dug3Bench", new Vector3(-42f, 1.0f, 24f), new Vector3(18f, 0.7f, 2.2f), bench);
        }

        void DressGrass()
        {
            var lawn = Look.Lit(Colors.Grass, Look.Grass, 16f, 0.08f);
            Cube(Grass, "OutfieldCarpet", new Vector3(0f, 0.15f, 270f), new Vector3(520f, 0.12f, 240f), lawn);
        }

        void DressWall()
        {
            var pad = Look.Toon(new Color(0.18f, 0.38f, 0.28f));
            var ivy = Look.Toon(new Color(0.14f, 0.48f, 0.22f));
            var foam = Look.Toon(new Color(0.96f, 0.82f, 0.22f));
            var ads = new[]
            {
                Look.Toon(Colors.Spark),
                Look.Toon(Colors.Gold),
                Look.Toon(new Color(0.18f, 0.42f, 0.72f)),
                Look.Toon(new Color(0.94f, 0.94f, 0.9f)),
                Look.Toon(new Color(0.12f, 0.16f, 0.28f)),
                Look.Toon(new Color(0.95f, 0.55f, 0.18f)),
                Look.Toon(new Color(0.22f, 0.55f, 0.38f)),
                Look.Toon(new Color(0.72f, 0.22f, 0.38f))
            };
            var mark = Look.Toon(Colors.Gold);
            var spark = Look.Toon(Colors.Spark);
            for (var i = -18; i <= 18; i++)
            {
                var spray = i / 18f * 48f;
                var fence = (float)AtBatResolver.FenceAt(_park, spray);
                var rad = spray * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Sin(rad) * fence, 5.2f, Mathf.Cos(rad) * fence);
                Cube(WallDress, "Pad" + i, p + new Vector3(0f, -2.8f, 0f), new Vector3(14f, 4.4f, 2.4f), pad);
                Cube(WallDress, "Foam" + i, p + new Vector3(0f, -0.35f, 0.2f), new Vector3(13.4f, 0.45f, 2.7f), foam);
                Cube(WallDress, "Ad" + i, p + new Vector3(0f, 1.1f, -0.4f), new Vector3(12f, 3.4f, 0.55f), ads[Mathf.Abs(i) % ads.Length]);
                if (i % 3 == 0)
                    Cube(WallDress, "Ivy" + i, p + new Vector3(0f, -3.4f, 0.6f), new Vector3(5.5f, 2.2f, 0.35f), ivy);
                if (i == 0)
                {
                    Cube(WallDress, "MarkSpark", p + new Vector3(0f, 1.3f, -0.85f), new Vector3(2.2f, 2.2f, 0.4f), spark);
                    Cube(WallDress, "MarkGold", p + new Vector3(0f, 1.3f, -1.1f), new Vector3(1.1f, 1.1f, 0.35f), mark);
                }
                if (i == -5 || i == 5)
                {
                    Cube(WallDress, "WordH" + i, p + new Vector3(-2.4f, 1.35f, -0.85f), new Vector3(0.55f, 2.2f, 0.3f), mark);
                    Cube(WallDress, "WordBar" + i, p + new Vector3(0f, 1.35f, -0.85f), new Vector3(3.6f, 0.45f, 0.3f), mark);
                    Cube(WallDress, "WordE" + i, p + new Vector3(2.4f, 1.35f, -0.85f), new Vector3(0.55f, 2.2f, 0.3f), mark);
                }
            }
        }

        void DressScoreboard()
        {
            var z = (float)_park.CenterFenceFt + 18f;
            var house = Look.Toon(new Color(0.12f, 0.14f, 0.18f));
            var face = Look.Unlit(new Color(0.10f, 0.22f, 0.14f));
            _ledOn = Look.Unlit(new Color(1f, 0.78f, 0.18f));
            _ledOff = Look.Unlit(new Color(0.08f, 0.12f, 0.09f));
            var led = _ledOn;
            var label = Look.Toon(new Color(0.92f, 0.94f, 0.9f));
            Cube(Scoreboard, "ScoreHouse", new Vector3(0f, 22f, z), new Vector3(48f, 28f, 8f), house);
            Cube(Scoreboard, "ScoreFace", new Vector3(0f, 22f, z - 4.2f), new Vector3(42f, 18f, 0.6f), face);
            Cube(Scoreboard, "ScoreSpark", new Vector3(0f, 34f, z - 4.4f), new Vector3(16f, 3.2f, 0.5f), Look.Toon(Colors.Spark));
            Cube(Scoreboard, "ScoreBar", new Vector3(0f, 12f, z - 4f), new Vector3(36f, 1.2f, 0.5f), Look.Toon(Colors.Gold));
            Cube(Scoreboard, "LblAway", new Vector3(-12.5f, 28.6f, z - 4.55f), new Vector3(6.4f, 1.1f, 0.28f), label);
            Cube(Scoreboard, "LblHome", new Vector3(12.5f, 28.6f, z - 4.55f), new Vector3(6.4f, 1.1f, 0.28f), Look.Toon(Colors.Spark));
            Cube(Scoreboard, "LblInn", new Vector3(0f, 16.4f, z - 4.55f), new Vector3(4.2f, 0.9f, 0.28f), Look.Toon(Colors.Gold));
            _awayTens = MakeDigit(Scoreboard, "AwayTens", new Vector3(-15.4f, 23.2f, z - 4.6f), led, 1f);
            _awayOnes = MakeDigit(Scoreboard, "AwayOnes", new Vector3(-10.2f, 23.2f, z - 4.6f), led, 1f);
            _homeTens = MakeDigit(Scoreboard, "HomeTens", new Vector3(9.6f, 23.2f, z - 4.6f), led, 1f);
            _homeOnes = MakeDigit(Scoreboard, "HomeOnes", new Vector3(14.8f, 23.2f, z - 4.6f), led, 1f);
            _innDigit = MakeDigit(Scoreboard, "InnDigit", new Vector3(0f, 19.4f, z - 4.6f), led, 0.85f);
            Cube(Scoreboard, "HomeHouse", new Vector3(0f, 20.4f, -38.8f), new Vector3(22f, 11f, 2.8f), house);
            Cube(Scoreboard, "HomeFace", new Vector3(0f, 20.4f, -37.3f), new Vector3(18.5f, 7.6f, 0.35f), face);
            Cube(Scoreboard, "HomeLblA", new Vector3(-5.6f, 23.0f, -37.05f), new Vector3(3.2f, 0.7f, 0.22f), label);
            Cube(Scoreboard, "HomeLblH", new Vector3(5.6f, 23.0f, -37.05f), new Vector3(3.2f, 0.7f, 0.22f), Look.Toon(Colors.Spark));
            _plateAwayTens = MakeDigit(Scoreboard, "PlateAwayTens", new Vector3(-6.8f, 20.2f, -37.0f), led, 0.55f);
            _plateAwayOnes = MakeDigit(Scoreboard, "PlateAwayOnes", new Vector3(-4.4f, 20.2f, -37.0f), led, 0.55f);
            _plateHomeTens = MakeDigit(Scoreboard, "PlateHomeTens", new Vector3(4.4f, 20.2f, -37.0f), led, 0.55f);
            _plateHomeOnes = MakeDigit(Scoreboard, "PlateHomeOnes", new Vector3(6.8f, 20.2f, -37.0f), led, 0.55f);
            SetScore(0, 0, 1);
        }

        public void SetScore(int away, int home, int inning)
        {
            if (_awayTens == null) return;
            away = Mathf.Clamp(away, 0, 99);
            home = Mathf.Clamp(home, 0, 99);
            inning = Mathf.Clamp(inning, 1, 9);
            PaintDigit(_awayTens, away / 10);
            PaintDigit(_awayOnes, away % 10);
            PaintDigit(_homeTens, home / 10);
            PaintDigit(_homeOnes, home % 10);
            PaintDigit(_innDigit, inning);
            PaintDigit(_plateAwayTens, away / 10);
            PaintDigit(_plateAwayOnes, away % 10);
            PaintDigit(_plateHomeTens, home / 10);
            PaintDigit(_plateHomeOnes, home % 10);
        }

        void DressBleachers()
        {
            var conc = Look.Lit(new Color(0.74f, 0.75f, 0.76f), smooth: 0.1f);
            var rail = Look.Lit(Colors.Gold, smooth: 0.4f);
            for (var row = 0; row < 6; row++)
            {
                var y = 3.2f + row * 2.15f;
                var z = -44f - row * 3.6f;
                Cube(Bleachers, "HomeStep" + row, new Vector3(0, y, z), new Vector3(96 - row * 2, 2.0f, 3.4f), conc);
                CrowdBank(Bleachers, "CrowdH" + row, new Vector3(0, y + 0.95f, z - 1.15f), new Vector3(84 - row * 2, 0, 0), new Vector3(0, 0.12f, -0.85f), 14, 2, row * 31);
            }
            for (var row = 0; row < 5; row++)
            {
                var y = 3.0f + row * 2.1f;
                Cube(Bleachers, "LStep" + row, new Vector3(-102 - row * 2.4f, y, 40), new Vector3(3.2f, 2.0f, 88), conc);
                CrowdBank(Bleachers, "CrowdL" + row, new Vector3(-104 - row * 2.4f, y + 0.9f, 40), new Vector3(0, 0, 72), new Vector3(-0.75f, 0.1f, 0), 10, 1, 200 + row * 17);
                Cube(Bleachers, "RStep" + row, new Vector3(102 + row * 2.4f, y, 40), new Vector3(3.2f, 2.0f, 88), conc);
                CrowdBank(Bleachers, "CrowdR" + row, new Vector3(104 + row * 2.4f, y + 0.9f, 40), new Vector3(0, 0, 72), new Vector3(0.75f, 0.1f, 0), 10, 1, 400 + row * 19);
            }
            Cube(Bleachers, "RailHome", new Vector3(0, 2.0f, -36), new Vector3(70, 1.2f, 1.2f), rail);
        }

        void CrowdBank(Transform parent, string name, Vector3 origin, Vector3 along, Vector3 across, int seats, int deep, int seed)
        {
            if (parent == null) return;
            var root = parent.Find(name);
            if (root == null)
            {
                var go = new GameObject(name);
                root = go.transform;
                root.SetParent(parent, false);
            }
            var jersey = new[]
            {
                Colors.Spark, Colors.Royal, Color.white, Colors.Gold,
                new Color(0.14f, 0.18f, 0.34f), new Color(0.82f, 0.28f, 0.18f)
            };
            var flesh = Look.Toon(new Color(1f, 0.80f, 0.68f));
            var dark = Look.Toon(new Color(0.36f, 0.24f, 0.16f));
            var n = 0;
            for (var d = 0; d < deep; d++)
            {
                for (var s = 0; s < seats; s++)
                {
                    var u = seats <= 1 ? 0f : s / (float)(seats - 1) - 0.5f;
                    var p = origin + along * u + across * d;
                    p.x += (Hash01(seed + n) - 0.5f) * 0.45f;
                    p.z += (Hash01(seed + n + 9) - 0.5f) * 0.4f;
                    var body = Look.Toon(jersey[n % jersey.Length]);
                    Cube(root, "Body" + n, p + new Vector3(0f, 1.0f, 0f), new Vector3(0.95f, 1.8f, 0.78f), body);
                    Cube(root, "Head" + n, p + new Vector3(0f, 2.08f, 0f), new Vector3(0.7f, 0.7f, 0.7f), n % 5 == 0 ? dark : flesh);
                    n++;
                }
            }
        }

        void DressTown()
        {
            var white = Look.Lit(new Color(0.93f, 0.95f, 0.96f), smooth: 0.2f);
            var brick = Look.Lit(new Color(0.62f, 0.28f, 0.22f), smooth: 0.1f);
            var red = Look.Lit(Colors.SparkDark, smooth: 0.15f);
            Cube(Town, "Wharf", new Vector3(-70, 16, 490), new Vector3(36, 32, 24), white);
            Cube(Town, "SparkHall", new Vector3(-18, 18, 505), new Vector3(22, 36, 20), red);
            Cube(Town, "Loft", new Vector3(55, 22, 495), new Vector3(28, 44, 22), white);
            Cube(Town, "Pier", new Vector3(110, 6, 470), new Vector3(70, 5, 16), brick);
            Cylinder(Town, "Light", new Vector3(155, 0, 440), 2.2f, 34f, Look.Lit(new Color(0.35f, 0.3f, 0.22f), smooth: 0.1f));
            Cube(Town, "RoofSpark", new Vector3(-18, 38, 505), new Vector3(24, 4, 22), Look.Lit(Colors.Gold, smooth: 0.4f));
        }

        void DressNight()
        {
            if (Fireworks != null) Fireworks.position = new Vector3(0, 12, 400);
            if (Fireworks != null && Fireworks.Find("FloodL") == null)
            {
                Glow(Fireworks, "FloodL", new Vector3(-90, 28, 40), new Color(1f, 0.92f, 0.75f), 1.8f, 140f);
                Glow(Fireworks, "FloodR", new Vector3(90, 28, 40), new Color(1f, 0.92f, 0.75f), 1.8f, 140f);
                Glow(Fireworks, "FloodH", new Vector3(0, 22, -40), new Color(1f, 0.90f, 0.70f), 1.4f, 110f);
                Glow(Fireworks, "FloodC", new Vector3(0, 32, 240), new Color(0.85f, 0.9f, 1f), 1.2f, 180f);
            }
            ApplyNight();
        }

        void ApplyNight()
        {
            if (Fireworks != null) Fireworks.gameObject.SetActive(_night);
        }

        public void Tick(Vector3 ball, float dt)
        {
            for (var i = 0; i < _sparks.Length; i++)
            {
                var s = _sparks[i];
                if (s.Tf == null || s.Life <= 0) continue;
                s.Life -= dt;
                s.Vel += new Vector3(0, -22f * dt, 0);
                s.Tf.position += s.Vel * dt;
                var u = Mathf.Clamp01(s.Life);
                s.Tf.localScale = Vector3.one * (0.4f + 1.2f * u);
                s.Tf.gameObject.SetActive(s.Life > 0);
                _sparks[i] = s;
            }
        }

        public void BurstFireworks(Vector3 at)
        {
            if (!_night || Fireworks == null) return;
            var cols = new[]
            {
                Colors.Spark, Colors.Gold, new Color(1f, 0.45f, 0.2f),
                new Color(0.45f, 0.75f, 1f), new Color(1f, 0.35f, 0.7f)
            };
            var origin = at.z < 80f ? new Vector3(0, 18, 390) : at + Vector3.up * 8f;
            for (var i = 0; i < _sparks.Length; i++)
            {
                if (_sparks[i].Tf != null) Destroy(_sparks[i].Tf.gameObject);
                var col = cols[i % cols.Length];
                var go = Look.Prim(PrimitiveType.Sphere, "Spark" + i, Fireworks, origin, Vector3.one * 1.4f, Look.Unlit(col));
                go.transform.position = origin;
                var a = i / (float)_sparks.Length * Mathf.PI * 2f;
                var lift = 18f + (i % 5) * 6f;
                _sparks[i] = new Firework
                {
                    Tf = go.transform,
                    Vel = new Vector3(Mathf.Cos(a) * 22f, lift, Mathf.Sin(a) * 16f),
                    Life = 1.6f + (i % 4) * 0.12f
                };
            }
        }

        static readonly int[] DigitMask = { 0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F };

        Transform MakeDigit(Transform parent, string name, Vector3 pos, Material on, float scale)
        {
            if (parent == null) return null;
            var root = parent.Find(name);
            if (root == null)
            {
                var go = new GameObject(name);
                root = go.transform;
                root.SetParent(parent, false);
            }
            root.position = pos;
            var w = 1.7f * scale;
            var h = 3.1f * scale;
            var t = 0.38f * scale;
            var dim = _ledOff != null ? _ledOff : on;
            Seg(root, "A", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, t, t), dim);
            Seg(root, "B", new Vector3(w * 0.5f, h * 0.25f, 0f), new Vector3(t, h * 0.42f, t), dim);
            Seg(root, "C", new Vector3(w * 0.5f, -h * 0.25f, 0f), new Vector3(t, h * 0.42f, t), dim);
            Seg(root, "D", new Vector3(0f, -h * 0.5f, 0f), new Vector3(w, t, t), dim);
            Seg(root, "E", new Vector3(-w * 0.5f, -h * 0.25f, 0f), new Vector3(t, h * 0.42f, t), dim);
            Seg(root, "F", new Vector3(-w * 0.5f, h * 0.25f, 0f), new Vector3(t, h * 0.42f, t), dim);
            Seg(root, "G", new Vector3(0f, 0f, 0f), new Vector3(w * 0.92f, t, t), dim);
            return root;
        }

        static void Seg(Transform parent, string name, Vector3 local, Vector3 scale, Material mat)
        {
            if (parent.Find(name) != null) return;
            var go = Look.Prim(PrimitiveType.Cube, name, parent, local, scale, mat);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
        }

        void PaintDigit(Transform root, int value)
        {
            if (root == null) return;
            value = Mathf.Clamp(value, 0, 9);
            var mask = DigitMask[value];
            var lit = _ledOn != null ? _ledOn : Look.Unlit(new Color(1f, 0.78f, 0.18f));
            var dim = _ledOff != null ? _ledOff : Look.Unlit(new Color(0.08f, 0.12f, 0.09f));
            PaintSeg(root, "A", (mask & 1) != 0, lit, dim);
            PaintSeg(root, "B", (mask & 2) != 0, lit, dim);
            PaintSeg(root, "C", (mask & 4) != 0, lit, dim);
            PaintSeg(root, "D", (mask & 8) != 0, lit, dim);
            PaintSeg(root, "E", (mask & 16) != 0, lit, dim);
            PaintSeg(root, "F", (mask & 32) != 0, lit, dim);
            PaintSeg(root, "G", (mask & 64) != 0, lit, dim);
        }

        static void PaintSeg(Transform root, string name, bool on, Material lit, Material dim)
        {
            var tf = root.Find(name);
            if (tf == null) return;
            var r = tf.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = on ? lit : dim;
        }

        static float Hash01(int i)
        {
            var n = (uint)(i * 16777619);
            n ^= n >> 13;
            n *= 1274126177u;
            return (n & 0xFFFF) / 65535f;
        }

        static void Cube(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            if (parent == null) return;
            var go = Look.Prim(PrimitiveType.Cube, name, parent, Vector3.zero, Vector3.one, mat);
            go.transform.position = pos;
            go.transform.localScale = scale;
        }

        static void Cylinder(Transform parent, string name, Vector3 pos, float radius, float height, Material mat)
        {
            if (parent == null) return;
            var go = Look.Prim(PrimitiveType.Cylinder, name, parent, Vector3.zero, Vector3.one, mat);
            go.transform.position = pos + new Vector3(0, height * 0.5f, 0);
            go.transform.localScale = new Vector3(radius * 2, height * 0.5f, radius * 2);
        }

        static void Glow(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            if (parent == null) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }
    }
}
