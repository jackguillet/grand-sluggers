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
            ShotPlate = ShotAnchor("ShotPlate", new Vector3(2.6f, 2.9f, -13.2f), new Vector3(0.2f, 3.2f, 54f), 42f);
            ShotMound = ShotAnchor("ShotMound", new Vector3(12.5f, 7.4f, 86.0f), new Vector3(0.35f, 2.9f, 8.0f), 40f);
            ShotDiamond = ShotAnchor("ShotDiamond", new Vector3(8f, 26f, -18f), new Vector3(0f, 6f, 90f), 50f);
            ShotThrow = ShotAnchor("ShotThrow", new Vector3(0f, 7.5f, -18f), new Vector3(0f, 1.4f, 0f), 42f);
            WarningTrack = Folder("WarningTrack");
            Backstop = Folder("Backstop");
            Dugouts = Folder("Dugouts");
            WallDress = Folder("WallDress");
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
            var bag = Look.Lit(Colors.Chalk, smooth: 0.08f);
            Mesh(Bag1, PrimitiveType.Cube, bag);
            Mesh(Bag2, PrimitiveType.Cube, bag);
            Mesh(Bag3, PrimitiveType.Cube, bag);
            DressPlace();
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
            var net = Look.Unlit(new Color(0.82f, 0.84f, 0.86f));
            var post = Look.Lit(new Color(0.55f, 0.55f, 0.5f), smooth: 0.15f);
            Cube(Backstop, "Net", new Vector3(0, 9f, -22f), new Vector3(42, 18, 0.6f), net);
            Cube(Backstop, "BackL", new Vector3(-22, 8f, -12f), new Vector3(0.6f, 16, 18f), net);
            Cube(Backstop, "BackR", new Vector3(22, 8f, -12f), new Vector3(0.6f, 16, 18f), net);
            Cylinder(Backstop, "PostL", new Vector3(-21, 0, -22), 0.45f, 18f, post);
            Cylinder(Backstop, "PostR", new Vector3(21, 0, -22), 0.45f, 18f, post);
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

        void DressWall()
        {
            var pad = Look.Toon(new Color(0.18f, 0.38f, 0.28f));
            var ads = new[]
            {
                Look.Toon(Colors.Spark),
                Look.Toon(Colors.Gold),
                Look.Toon(new Color(0.18f, 0.42f, 0.72f)),
                Look.Toon(new Color(0.94f, 0.94f, 0.9f))
            };
            for (var i = -18; i <= 18; i++)
            {
                var spray = i / 18f * 48f;
                var fence = (float)AtBatResolver.FenceAt(_park, spray);
                var rad = spray * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Sin(rad) * fence, 5.2f, Mathf.Cos(rad) * fence);
                Cube(WallDress, "Pad" + i, p + new Vector3(0f, -2.8f, 0f), new Vector3(14f, 4.4f, 2.4f), pad);
                Cube(WallDress, "Ad" + i, p + new Vector3(0f, 1.1f, -0.4f), new Vector3(12f, 3.4f, 0.55f), ads[Mathf.Abs(i) % ads.Length]);
            }
        }

        void DressScoreboard()
        {
            var z = (float)_park.CenterFenceFt + 18f;
            var house = Look.Toon(new Color(0.12f, 0.14f, 0.18f));
            var face = Look.Unlit(new Color(0.16f, 0.52f, 0.3f));
            Cube(Scoreboard, "ScoreHouse", new Vector3(0f, 22f, z), new Vector3(48f, 28f, 8f), house);
            Cube(Scoreboard, "ScoreFace", new Vector3(0f, 22f, z - 4.2f), new Vector3(42f, 18f, 0.6f), face);
            Cube(Scoreboard, "ScoreSpark", new Vector3(0f, 34f, z - 4.4f), new Vector3(16f, 3.2f, 0.5f), Look.Toon(Colors.Spark));
            Cube(Scoreboard, "ScoreBar", new Vector3(0f, 12f, z - 4f), new Vector3(36f, 1.2f, 0.5f), Look.Toon(Colors.Gold));
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
                CrowdCard(Bleachers, "CrowdH" + row, new Vector3(0, y + 1.6f, z - 1.4f), new Vector3(90 - row * 2, 2.8f, 0.4f));
            }
            for (var row = 0; row < 5; row++)
            {
                var y = 3.0f + row * 2.1f;
                Cube(Bleachers, "LStep" + row, new Vector3(-102 - row * 2.4f, y, 40), new Vector3(3.2f, 2.0f, 88), conc);
                CrowdCard(Bleachers, "CrowdL" + row, new Vector3(-104 - row * 2.4f, y + 1.5f, 40), new Vector3(0.4f, 2.6f, 80));
                Cube(Bleachers, "RStep" + row, new Vector3(102 + row * 2.4f, y, 40), new Vector3(3.2f, 2.0f, 88), conc);
                CrowdCard(Bleachers, "CrowdR" + row, new Vector3(104 + row * 2.4f, y + 1.5f, 40), new Vector3(0.4f, 2.6f, 80));
            }
            Cube(Bleachers, "RailHome", new Vector3(0, 2.0f, -36), new Vector3(70, 1.2f, 1.2f), rail);
            Fans(new Vector3(-40, 5.2f, -46), new Vector3(8, 0, 0), 10);
            Fans(new Vector3(-90, 6.4f, 20), new Vector3(0, 0, 8), 8);
            Fans(new Vector3(90, 6.4f, 20), new Vector3(0, 0, 8), 8);
        }

        void Fans(Vector3 origin, Vector3 step, int n)
        {
            var jersey = new[] { Colors.Spark, Colors.Royal, Color.white, Colors.Gold };
            for (var i = 0; i < n; i++)
            {
                var p = origin + step * i;
                var body = Look.Toon(jersey[i % jersey.Length]);
                var flesh = Look.Toon(new Color(1f, 0.8f, 0.68f));
                Cube(Bleachers, "Fan" + origin.x + i, p + new Vector3(0, 1.1f, 0), new Vector3(1.1f, 2.0f, 0.9f), body);
                Cube(Bleachers, "FanHead" + origin.x + i, p + new Vector3(0, 2.35f, 0), new Vector3(0.85f, 0.85f, 0.85f), flesh);
            }
        }

        void CrowdCard(Transform parent, string name, Vector3 pos, Vector3 scale)
        {
            Cube(parent, name, pos, scale, Look.Lit(Color.white, Look.Crowd, 1f, 0.05f));
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
            if (_night)
            {
                Glow(Fireworks, "FloodL", new Vector3(-90, 28, 40), new Color(1f, 0.92f, 0.75f), 1.8f, 140f);
                Glow(Fireworks, "FloodR", new Vector3(90, 28, 40), new Color(1f, 0.92f, 0.75f), 1.8f, 140f);
                Glow(Fireworks, "FloodH", new Vector3(0, 22, -40), new Color(1f, 0.90f, 0.70f), 1.4f, 110f);
                Glow(Fireworks, "FloodC", new Vector3(0, 32, 240), new Color(0.85f, 0.9f, 1f), 1.2f, 180f);
            }
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
