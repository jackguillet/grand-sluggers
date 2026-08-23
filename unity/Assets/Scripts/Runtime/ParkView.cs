using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed class ParkView : MonoBehaviour
    {
        Transform _root;
        BallView _ball;
        bool _night;
        Light _followSpot;
        Transform _fireworks;
        readonly Firework[] _sparks = new Firework[28];

        struct Firework
        {
            public Transform Tf;
            public Vector3 Vel;
            public float Life;
        }

        public BallView Ball => _ball;
        public bool Night => _night;

        public void Build(Park park, bool night = false)
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = new GameObject("Park").transform;
            _root.SetParent(transform, false);
            _night = night;
            _followSpot = null;
            _fireworks = null;
            for (var i = 0; i < _sparks.Length; i++)
                _sparks[i] = default;

            var ice = park.Surface == "ice";
            var ash = park.Surface == "ash";
            var jungle = park.Id == "canopy-yard";
            var harbor = park.Id == "harbor-diamond";
            var crystal = park.Id == "crystal-rink";
            var funfair = park.Id == "funfair-park";
            var rooftop = park.Id == "rooftop-city";
            var ember = park.Id == "ember-keep";

            var sky = ice ? Colors.Ice : ash ? new Color(0.22f, 0.1f, 0.12f) : Colors.Sky;
            if (Camera.main != null)
            {
                if (harbor)
                {
                    if (night) Look.RigHarborNight(Camera.main);
                    else Look.RigAfternoon(Camera.main);
                }
                else if (crystal)
                {
                    if (night) Look.RigIceGardenNight(Camera.main);
                    else Look.RigIceGarden(Camera.main);
                }
                else if (funfair)
                {
                    if (night) Look.RigCarnivalNight(Camera.main);
                    else Look.RigCarnival(Camera.main);
                }
                else if (rooftop) Look.RigNeon(Camera.main);
                else if (jungle) Look.RigCanopy(Camera.main);
                else if (ember)
                {
                    if (night) Look.RigCourtyardNight(Camera.main);
                    else Look.RigCourtyard(Camera.main);
                }
                else Look.SetupLighting(Camera.main, sky);
            }

            var grassCol = ice ? Colors.Ice
                : ash ? new Color(0.28f, 0.19f, 0.16f)
                : jungle ? new Color(0.14f, 0.43f, 0.2f)
                : rooftop ? new Color(0.32f, 0.32f, 0.34f)
                : Colors.Grass;
            var waterCol = ash ? new Color(0.35f, 0.11f, 0.07f)
                : ice ? new Color(0.55f, 0.78f, 0.92f)
                : funfair ? new Color(0.46f, 0.32f, 0.22f)
                : rooftop ? new Color(0.08f, 0.08f, 0.12f)
                : jungle ? new Color(0.08f, 0.18f, 0.10f)
                : Colors.Water;
            var skipGrass = ice || ash || rooftop;
            var grassMat = Look.Lit(grassCol, skipGrass ? null : Look.Grass, skipGrass ? 1f : 18f, ice ? 0.72f : rooftop ? 0.18f : 0.08f);
            var dirtMat = crystal
                ? Look.Lit(new Color(0.74f, 0.84f, 0.90f), Look.Dirt, 6f, 0.35f)
                : rooftop
                    ? Look.Lit(new Color(0.42f, 0.40f, 0.38f), Look.Dirt, 8f, 0.15f)
                    : jungle
                        ? Look.Lit(new Color(0.42f, 0.32f, 0.18f), Look.Dirt, 8f, 0.1f)
                        : Look.Lit(Colors.Dirt, Look.Dirt, 8f, 0.12f);
            var waterMat = Look.Lit(waterCol, smooth: ice ? 0.92f : 0.85f);

            Quad("Water", new Vector3(0, -1.4f, 240), new Vector3(1100, 1, 1100), waterMat);
            Quad("Outfield", new Vector3(0, -0.12f, 190), new Vector3(620, 0.35f, 620), grassMat);
            Infield(dirtMat);
            Mound();
            FoulLines();
            Bags();
            Fence(park, ash);
            if (harbor)
            {
                WarningTrack(park);
                Backstop();
                Dugouts();
                HarborBleachers();
                HarborTown();
                HarborNightHook();
            }
            else if (crystal)
            {
                CrystalGarden(park);
            }
            else if (funfair)
            {
                FunfairGrounds(park);
            }
            else if (rooftop)
            {
                RooftopDeck(park);
            }
            else if (jungle)
            {
                CanopyGrounds(park);
            }
            else if (ember)
            {
                EmberCourtyard(park);
            }
            else
            {
                Stands(ice, ash);
            }
            Hazards(park);

            _ball = gameObject.GetComponent<BallView>();
            if (_ball == null) _ball = gameObject.AddComponent<BallView>();
            _ball.Build(_root);
        }

        void Infield(Material dirt)
        {
            Quad("DirtPad", new Vector3(0, 0.04f, 64), new Vector3(150, 0.18f, 150), dirt);
            Cylinder("HomeDirt", new Vector3(0, 0.08f, 0), 18f, 0.16f, dirt);
            Cylinder("BagDirt1", new Vector3((float)Diamond.First.X, 0.08f, (float)Diamond.First.Z), 11f, 0.16f, dirt);
            Cylinder("BagDirt2", new Vector3((float)Diamond.Second.X, 0.08f, (float)Diamond.Second.Z), 11f, 0.16f, dirt);
            Cylinder("BagDirt3", new Vector3((float)Diamond.Third.X, 0.08f, (float)Diamond.Third.Z), 11f, 0.16f, dirt);
        }

        void Mound()
        {
            var dirt = Look.Lit(new Color(0.62f, 0.42f, 0.26f), Look.Dirt, 3f, 0.1f);
            Cylinder("Mound", new Vector3(0, 0.45f, 60.5f), 10f, 1.15f, dirt);
            Quad("Rubber", new Vector3(0, 1.08f, 60.5f), new Vector3(1.9f, 0.08f, 0.45f), Look.Lit(Colors.Chalk, smooth: 0.05f));
        }

        void FoulLines()
        {
            var chalk = Look.Lit(Colors.Chalk, smooth: 0.05f);
            var a = Quaternion.Euler(0, 45, 0);
            var b = Quaternion.Euler(0, -45, 0);
            var l = Look.Prim(PrimitiveType.Cube, "FoulL", _root, new Vector3(90, 0.12f, 90), new Vector3(1.1f, 0.08f, 260), chalk);
            l.transform.rotation = a;
            var r = Look.Prim(PrimitiveType.Cube, "FoulR", _root, new Vector3(-90, 0.12f, 90), new Vector3(1.1f, 0.08f, 260), chalk);
            r.transform.rotation = b;
        }

        void Bags()
        {
            var bag = Look.Lit(Colors.Chalk, smooth: 0.08f);
            Cube("1B", new Vector3((float)Diamond.First.X, 0.28f, (float)Diamond.First.Z), new Vector3(2.4f, 0.4f, 2.4f), bag);
            Cube("2B", new Vector3((float)Diamond.Second.X, 0.28f, (float)Diamond.Second.Z), new Vector3(2.4f, 0.4f, 2.4f), bag);
            Cube("3B", new Vector3((float)Diamond.Third.X, 0.28f, (float)Diamond.Third.Z), new Vector3(2.4f, 0.4f, 2.4f), bag);
            Cube("Home", new Vector3(0, 0.22f, -0.5f), new Vector3(2.6f, 0.22f, 2.6f), bag);
        }

        void Fence(Park park, bool ash)
        {
            var crystal = park.Id == "crystal-rink";
            var funfair = park.Id == "funfair-park";
            var rooftop = park.Id == "rooftop-city";
            var jungle = park.Id == "canopy-yard";
            var wall = Look.Lit(
                ash ? Colors.EmberFire
                    : crystal ? new Color(0.52f, 0.76f, 0.88f)
                    : funfair ? new Color(0.86f, 0.18f, 0.28f)
                    : rooftop ? new Color(0.38f, 0.38f, 0.42f)
                    : jungle ? new Color(0.28f, 0.42f, 0.18f)
                    : new Color(0.22f, 0.48f, 0.28f),
                smooth: crystal ? 0.62f : rooftop ? 0.32f : 0.18f);
            var wallAlt = funfair ? Look.Lit(new Color(0.96f, 0.90f, 0.72f), smooth: 0.18f) : wall;
            var cap = Look.Lit(
                crystal ? new Color(0.88f, 0.94f, 1f)
                    : rooftop ? new Color(0.72f, 0.72f, 0.76f)
                    : Colors.Gold,
                smooth: crystal ? 0.75f : rooftop ? 0.28f : 0.4f);
            var pole = Look.Lit(crystal ? Colors.Royal : rooftop ? Colors.Goldrush : Colors.Gold, smooth: 0.45f);
            for (var i = -18; i <= 18; i++)
            {
                var spray = i / 18f * 48f;
                var fence = (float)AtBatResolver.FenceAt(park, spray);
                var rad = spray * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Sin(rad) * fence, 5.2f, Mathf.Cos(rad) * fence);
                Cube("Fence" + i, p, new Vector3(14, 10.4f, 1.8f), funfair && (i & 1) == 0 ? wallAlt : wall);
                Cube("Cap" + i, p + new Vector3(0, 5.4f, 0), new Vector3(14, 0.35f, 2.1f), cap);
            }
            var lf = (float)park.LeftFenceFt;
            var rf = (float)park.RightFenceFt;
            Cylinder("PoleL", new Vector3(Mathf.Sin(-0.78f) * lf, 0, Mathf.Cos(-0.78f) * lf), 0.7f, 52f, pole);
            Cylinder("PoleR", new Vector3(Mathf.Sin(0.78f) * rf, 0, Mathf.Cos(0.78f) * rf), 0.7f, 52f, pole);
            Cube("ScreenL", new Vector3(Mathf.Sin(-0.78f) * lf, 38f, Mathf.Cos(-0.78f) * lf), new Vector3(0.2f, 18f, 8f), Look.Unlit(new Color(0.9f, 0.9f, 0.7f)));
            Cube("ScreenR", new Vector3(Mathf.Sin(0.78f) * rf, 38f, Mathf.Cos(0.78f) * rf), new Vector3(0.2f, 18f, 8f), Look.Unlit(new Color(0.9f, 0.9f, 0.7f)));
        }

        void WarningTrack(Park park)
        {
            var dirt = Look.Lit(new Color(0.72f, 0.52f, 0.32f), Look.Dirt, 6f, 0.1f);
            for (var i = -18; i <= 18; i++)
            {
                var spray = i / 18f * 48f;
                var fence = (float)AtBatResolver.FenceAt(park, spray) - 12f;
                var rad = spray * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Sin(rad) * fence, 0.14f, Mathf.Cos(rad) * fence);
                Cube("Track" + i, p, new Vector3(16, 0.2f, 10f), dirt);
            }
        }

        void Backstop()
        {
            var net = Look.Unlit(new Color(0.82f, 0.84f, 0.86f));
            var post = Look.Lit(new Color(0.55f, 0.55f, 0.5f), smooth: 0.15f);
            Cube("Backstop", new Vector3(0, 9f, -22f), new Vector3(42, 18, 0.6f), net);
            Cube("BackL", new Vector3(-22, 8f, -12f), new Vector3(0.6f, 16, 18f), net);
            Cube("BackR", new Vector3(22, 8f, -12f), new Vector3(0.6f, 16, 18f), net);
            Cylinder("PostL", new Vector3(-21, 0, -22), 0.45f, 18f, post);
            Cylinder("PostR", new Vector3(21, 0, -22), 0.45f, 18f, post);
        }

        void Dugouts()
        {
            var roof = Look.Lit(new Color(0.18f, 0.32f, 0.22f), smooth: 0.12f);
            var pad = Look.Lit(new Color(0.55f, 0.42f, 0.28f), Look.Dirt, 2f, 0.1f);
            Cube("Dugout1B", new Vector3(42, 2.4f, 22), new Vector3(22, 1.2f, 10), roof);
            Cube("Dugout1BPad", new Vector3(42, 0.3f, 22), new Vector3(20, 0.4f, 8), pad);
            Cube("Dugout3B", new Vector3(-42, 2.4f, 22), new Vector3(22, 1.2f, 10), roof);
            Cube("Dugout3BPad", new Vector3(-42, 0.3f, 22), new Vector3(20, 0.4f, 8), pad);
        }

        void HarborBleachers()
        {
            var conc = Look.Lit(new Color(0.74f, 0.75f, 0.76f), smooth: 0.1f);
            var rail = Look.Lit(Colors.Gold, smooth: 0.4f);
            for (var row = 0; row < 6; row++)
            {
                var y = 3.2f + row * 2.15f;
                var z = -44f - row * 3.6f;
                Cube("HomeStep" + row, new Vector3(0, y, z), new Vector3(96 - row * 2, 2.0f, 3.4f), conc);
                CrowdCard("CrowdH" + row, new Vector3(0, y + 1.6f, z - 1.4f), new Vector3(90 - row * 2, 2.8f, 0.4f));
            }
            for (var row = 0; row < 5; row++)
            {
                var y = 3.0f + row * 2.1f;
                Cube("LStep" + row, new Vector3(-102 - row * 2.4f, y, 40), new Vector3(3.2f, 2.0f, 88), conc);
                CrowdCard("CrowdL" + row, new Vector3(-104 - row * 2.4f, y + 1.5f, 40), new Vector3(0.4f, 2.6f, 80));
                Cube("RStep" + row, new Vector3(102 + row * 2.4f, y, 40), new Vector3(3.2f, 2.0f, 88), conc);
                CrowdCard("CrowdR" + row, new Vector3(104 + row * 2.4f, y + 1.5f, 40), new Vector3(0.4f, 2.6f, 80));
            }
            Cube("RailHome", new Vector3(0, 2.0f, -36), new Vector3(70, 1.2f, 1.2f), rail);
        }

        void Stands(bool ice, bool ash)
        {
            var conc = Look.Lit(ice ? new Color(0.85f, 0.9f, 0.95f) : new Color(0.78f, 0.8f, 0.82f), smooth: 0.12f);
            var rail = Look.Lit(Colors.Gold, smooth: 0.4f);
            Cube("HomePlateStand", new Vector3(0, 16, -56), new Vector3(120, 28, 22), conc);
            Cube("LeftStand", new Vector3(-108, 14, 36), new Vector3(28, 24, 110), conc);
            Cube("RightStand", new Vector3(108, 14, 36), new Vector3(28, 24, 110), conc);
            Cube("Lip", new Vector3(0, 2.2f, -38), new Vector3(90, 3, 4), rail);
            CrowdCard("CrowdH", new Vector3(0, 18, -66), new Vector3(110, 18, 1));
            CrowdCard("CrowdL", new Vector3(-120, 16, 36), new Vector3(1, 16, 90));
            CrowdCard("CrowdR", new Vector3(120, 16, 36), new Vector3(1, 16, 90));
            if (ash)
            {
                Cube("KeepWall", new Vector3(0, 22, 455), new Vector3(140, 50, 30), Look.Lit(Colors.Ember, smooth: 0.08f));
            }
        }

        void CrowdCard(string name, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            Destroy(go.GetComponent<Collider>());
            var mat = Look.Lit(Color.white, Look.Crowd, 1f, 0.05f);
            Look.Paint(go, mat);
        }

        void HarborTown()
        {
            var white = Look.Lit(new Color(0.93f, 0.95f, 0.96f), smooth: 0.2f);
            var brick = Look.Lit(new Color(0.62f, 0.28f, 0.22f), smooth: 0.1f);
            var red = Look.Lit(Colors.SparkDark, smooth: 0.15f);
            Cube("Wharf", new Vector3(-70, 16, 490), new Vector3(36, 32, 24), white);
            Cube("SparkHall", new Vector3(-18, 18, 505), new Vector3(22, 36, 20), red);
            Cube("Loft", new Vector3(55, 22, 495), new Vector3(28, 44, 22), white);
            Cube("Pier", new Vector3(110, 6, 470), new Vector3(70, 5, 16), brick);
            Cylinder("Light", new Vector3(155, 0, 440), 2.2f, 34f, Look.Lit(new Color(0.35f, 0.3f, 0.22f), smooth: 0.1f));
            Cube("RoofSpark", new Vector3(-18, 38, 505), new Vector3(24, 4, 22), Look.Lit(Colors.Gold, smooth: 0.4f));
        }

        void HarborNightHook()
        {
            var go = new GameObject("Fireworks");
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector3(0, 12, 400);
            _fireworks = go.transform;
            if (_night)
            {
                Glow("FloodL", new Vector3(-90, 28, 40), new Color(1f, 0.92f, 0.75f), 1.8f, 140f);
                Glow("FloodR", new Vector3(90, 28, 40), new Color(1f, 0.92f, 0.75f), 1.8f, 140f);
                Glow("FloodH", new Vector3(0, 22, -40), new Color(1f, 0.90f, 0.70f), 1.4f, 110f);
                Glow("FloodC", new Vector3(0, 32, 240), new Color(0.85f, 0.9f, 1f), 1.2f, 180f);
            }
            go.SetActive(_night);
        }

        public void Tick(Vector3 ball, float dt)
        {
            if (_followSpot != null && _followSpot.gameObject.activeInHierarchy)
            {
                var t = _followSpot.transform;
                t.position = ball + new Vector3(0f, 26f, -8f);
                t.LookAt(ball);
            }
            TickFireworks(dt);
        }

        public void BurstFireworks(Vector3 at)
        {
            if (!_night || _fireworks == null) return;
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
                var go = Look.Prim(PrimitiveType.Sphere, "Spark" + i, _fireworks, origin, Vector3.one * 1.4f, Look.Unlit(col));
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

        void TickFireworks(float dt)
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

        void CrystalGarden(Park park)
        {
            var ice = Look.Lit(new Color(0.84f, 0.93f, 0.98f), smooth: 0.72f);
            var glass = Look.Lit(new Color(0.70f, 0.88f, 0.98f), smooth: 0.88f);
            var pink = Look.Lit(Colors.Royal, smooth: 0.32f);
            var gold = Look.Lit(Colors.Gold, smooth: 0.5f);
            var stone = Look.Lit(new Color(0.76f, 0.82f, 0.88f), smooth: 0.28f);

            CrystalBoards(park, glass, pink);
            Cube("IceBench1B", new Vector3(42, 1.1f, 22), new Vector3(20, 1.0f, 6), ice);
            Cube("IceBench3B", new Vector3(-42, 1.1f, 22), new Vector3(20, 1.0f, 6), ice);
            Cube("HomePavilion", new Vector3(0, 10, -62), new Vector3(80, 18, 16), ice);
            Cube("HomeRoof", new Vector3(0, 20.2f, -62), new Vector3(86, 2.2f, 20), pink);
            Cube("LeftPavilion", new Vector3(-118, 12, 40), new Vector3(16, 20, 90), ice);
            Cube("RightPavilion", new Vector3(118, 12, 40), new Vector3(16, 20, 90), ice);
            Cube("IceLip", new Vector3(0, 2.0f, -36), new Vector3(70, 1.4f, 1.4f), pink);
            CrowdCard("CrowdH", new Vector3(0, 12, -70), new Vector3(74, 12, 1));
            CrowdCard("CrowdL", new Vector3(-126, 14, 40), new Vector3(1, 14, 80));
            CrowdCard("CrowdR", new Vector3(126, 14, 40), new Vector3(1, 14, 80));
            FrozenFountain(ice, stone);
            RoyalPalace(ice, pink, gold);
            CrystalNightHook();
        }

        void CrystalBoards(Park park, Material glass, Material kick)
        {
            for (var i = -18; i <= 18; i++)
            {
                var spray = i / 18f * 48f;
                var fence = (float)AtBatResolver.FenceAt(park, spray) - 7f;
                var rad = spray * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Sin(rad) * fence, 2.5f, Mathf.Cos(rad) * fence);
                Cube("Board" + i, p, new Vector3(14, 4.6f, 0.55f), glass);
                Cube("Kick" + i, p + new Vector3(0, -1.7f, 0), new Vector3(14, 1.2f, 0.8f), kick);
            }
            Cube("GlassBack", new Vector3(0, 8f, -24f), new Vector3(38, 16, 0.45f), glass);
            Cube("GlassL", new Vector3(-20, 7f, -12f), new Vector3(0.45f, 14, 16f), glass);
            Cube("GlassR", new Vector3(20, 7f, -12f), new Vector3(0.45f, 14, 16f), glass);
        }

        void FrozenFountain(Material ice, Material stone)
        {
            Cube("FountainBase", new Vector3(0, 1.1f, -48), new Vector3(20, 2.2f, 20), stone);
            Cylinder("FountainBowl", new Vector3(0, 2.8f, -48), 8.5f, 1.2f, ice);
            Cylinder("Jet", new Vector3(0, 6.4f, -48), 1.2f, 7.2f, ice);
            for (var i = 0; i < 6; i++)
            {
                var a = i / 6f * Mathf.PI * 2f;
                Cylinder("Spray" + i, new Vector3(Mathf.Cos(a) * 5.2f, 4.6f, -48 + Mathf.Sin(a) * 5.2f), 0.55f, 4.8f, ice);
            }
            Cylinder("IceGlobe", new Vector3(0, 11.2f, -48), 1.6f, 1.6f, ice);
        }

        void RoyalPalace(Material ice, Material pink, Material gold)
        {
            Cube("Palace", new Vector3(0, 28, 502), new Vector3(88, 56, 34), ice);
            Cube("PalaceRoof", new Vector3(0, 58, 502), new Vector3(96, 8, 40), pink);
            Cylinder("SpireL", new Vector3(-46, 0, 502), 6.5f, 86f, ice);
            Cylinder("SpireR", new Vector3(46, 0, 502), 6.5f, 86f, ice);
            Cube("CrownL", new Vector3(-46, 88, 502), new Vector3(10, 8, 10), gold);
            Cube("CrownR", new Vector3(46, 88, 502), new Vector3(10, 8, 10), gold);
            Cube("WingL", new Vector3(-92, 18, 488), new Vector3(42, 36, 22), ice);
            Cube("WingR", new Vector3(92, 18, 488), new Vector3(42, 36, 22), ice);
            Cube("Gate", new Vector3(0, 14, 480), new Vector3(26, 28, 8), pink);
            Cube("GateCrown", new Vector3(0, 30, 480), new Vector3(18, 4, 6), gold);
        }

        void CrystalNightHook()
        {
            var go = new GameObject("FollowSpot");
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector3(0, 52, 36);
            go.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(0.92f, 0.96f, 1f);
            light.intensity = _night ? 7.2f : 0f;
            light.range = 280f;
            light.spotAngle = 26f;
            go.SetActive(_night);
            _followSpot = light;
        }

        void FunfairGrounds(Park park)
        {
            var red = Look.Lit(new Color(0.86f, 0.16f, 0.22f), smooth: 0.18f);
            var cream = Look.Lit(new Color(0.96f, 0.92f, 0.82f), smooth: 0.16f);
            var yellow = Look.Lit(Colors.Gold, smooth: 0.4f);
            var pink = Look.Lit(new Color(1f, 0.31f, 0.63f), smooth: 0.28f);
            var wood = Look.Lit(new Color(0.46f, 0.28f, 0.14f), smooth: 0.1f);
            var canvas = Look.Lit(new Color(0.94f, 0.78f, 0.48f), smooth: 0.12f);

            WarningTrack(park);
            FunfairBackstop(cream, red, wood);
            FunfairBenches(wood, canvas);
            Tent("HomeTent", new Vector3(0, 0, -62), 72, 22, 20, red, cream, wood);
            Tent("LeftTent", new Vector3(-118, 0, 38), 22, 84, 18, pink, cream, wood);
            Tent("RightTent", new Vector3(118, 0, 38), 22, 84, 18, yellow, red, wood);
            CrowdCard("CrowdH", new Vector3(0, 12, -72), new Vector3(64, 12, 1));
            CrowdCard("CrowdL", new Vector3(-128, 12, 38), new Vector3(1, 12, 72));
            CrowdCard("CrowdR", new Vector3(128, 12, 38), new Vector3(1, 12, 72));
            StripedPoles();
            FerrisWheel();
            FunfairBooths(wood, red, cream, yellow, pink);
            FunfairTrain(park, wood, red, cream, yellow);
            FunfairNightHook();
        }

        void FunfairBackstop(Material cream, Material red, Material wood)
        {
            Cube("BuntHome", new Vector3(0, 8f, -24f), new Vector3(38, 14, 0.5f), cream);
            Cube("BuntStripe", new Vector3(0, 8f, -23.6f), new Vector3(38, 2.2f, 0.2f), red);
            Cube("BuntL", new Vector3(-20, 7f, -12f), new Vector3(0.5f, 12, 16f), cream);
            Cube("BuntR", new Vector3(20, 7f, -12f), new Vector3(0.5f, 12, 16f), cream);
            Cylinder("PostL", new Vector3(-19, 0, -24), 0.5f, 16f, wood);
            Cylinder("PostR", new Vector3(19, 0, -24), 0.5f, 16f, wood);
        }

        void FunfairBenches(Material wood, Material canvas)
        {
            Cube("Bench1B", new Vector3(42, 1.0f, 22), new Vector3(20, 1.0f, 6), wood);
            Cube("Awning1B", new Vector3(42, 5.2f, 22), new Vector3(22, 0.5f, 8), canvas);
            Cube("Bench3B", new Vector3(-42, 1.0f, 22), new Vector3(20, 1.0f, 6), wood);
            Cube("Awning3B", new Vector3(-42, 5.2f, 22), new Vector3(22, 0.5f, 8), canvas);
        }

        void Tent(string name, Vector3 pos, float w, float d, float h, Material a, Material b, Material pole)
        {
            var root = new GameObject(name).transform;
            root.SetParent(_root, false);
            root.position = pos;
            Look.Prim(PrimitiveType.Cylinder, "Mast", root, new Vector3(0, h * 0.55f, 0), new Vector3(1.1f, h * 0.55f, 1.1f), pole);
            Look.Prim(PrimitiveType.Cube, "Wall", root, new Vector3(0, h * 0.38f, 0), new Vector3(w * 0.82f, h * 0.72f, d * 0.82f), a);
            Look.Prim(PrimitiveType.Cube, "Roof", root, new Vector3(0, h * 0.92f, 0), new Vector3(w, 1.6f, d), b);
            Look.Prim(PrimitiveType.Cube, "Peak", root, new Vector3(0, h * 1.12f, 0), new Vector3(w * 0.42f, 3.2f, d * 0.42f), a);
            Look.Prim(PrimitiveType.Cube, "Stripe", root, new Vector3(0, h * 0.92f, 0), new Vector3(w * 1.02f, 0.45f, d * 1.02f), a);
            Look.Prim(PrimitiveType.Cylinder, "Flagpole", root, new Vector3(0, h * 1.32f, 0), new Vector3(0.28f, 2.4f, 0.28f), pole);
            Look.Prim(PrimitiveType.Cube, "Pennant", root, new Vector3(1.8f, h * 1.42f, 0), new Vector3(3.6f, 1.2f, 0.18f), b);
        }

        void StripedPoles()
        {
            var red = Look.Lit(new Color(0.86f, 0.16f, 0.22f), smooth: 0.2f);
            var cream = Look.Lit(new Color(0.96f, 0.92f, 0.82f), smooth: 0.2f);
            var spots = new[]
            {
                new Vector3(-86, 0, 210), new Vector3(86, 0, 210),
                new Vector3(-70, 0, 320), new Vector3(70, 0, 320),
                new Vector3(-48, 0, 430), new Vector3(48, 0, 430),
                new Vector3(-140, 0, 80), new Vector3(140, 0, 80)
            };
            for (var p = 0; p < spots.Length; p++)
            {
                const int bands = 8;
                const float h = 4.2f;
                for (var i = 0; i < bands; i++)
                    Cylinder("Pole" + p + i, spots[p] + new Vector3(0, i * h, 0), 1.15f, h, i % 2 == 0 ? red : cream);
            }
        }

        void FerrisWheel()
        {
            var red = Look.Lit(new Color(0.86f, 0.16f, 0.22f), smooth: 0.22f);
            var yellow = Look.Lit(Colors.Gold, smooth: 0.42f);
            var cream = Look.Lit(new Color(0.96f, 0.92f, 0.82f), smooth: 0.18f);
            var pink = Look.Lit(new Color(1f, 0.31f, 0.63f), smooth: 0.28f);
            var steel = Look.Lit(new Color(0.55f, 0.52f, 0.50f), smooth: 0.3f);

            var root = new GameObject("FerrisWheel").transform;
            root.SetParent(_root, false);
            root.position = new Vector3(0, 0, 508);

            Look.Prim(PrimitiveType.Cube, "Base", root, new Vector3(0, 2.2f, 0), new Vector3(30, 4.4f, 16), steel);
            var legL = Look.Prim(PrimitiveType.Cube, "LegL", root, new Vector3(-11, 20, 0), new Vector3(3.4f, 38, 3.4f), steel);
            legL.transform.localRotation = Quaternion.Euler(0, 0, 16f);
            var legR = Look.Prim(PrimitiveType.Cube, "LegR", root, new Vector3(11, 20, 0), new Vector3(3.4f, 38, 3.4f), steel);
            legR.transform.localRotation = Quaternion.Euler(0, 0, -16f);

            var wheel = new GameObject("Rim").transform;
            wheel.SetParent(root, false);
            wheel.localPosition = new Vector3(0, 40, 0);
            Look.Prim(PrimitiveType.Cylinder, "Hub", wheel, Vector3.zero, new Vector3(7.2f, 2.4f, 7.2f), yellow);
            var axle = Look.Prim(PrimitiveType.Cylinder, "Axle", wheel, Vector3.zero, new Vector3(2.4f, 5.5f, 2.4f), steel);
            axle.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            for (var i = 0; i < 12; i++)
            {
                var a = i / 12f * 360f;
                var rad = a * Mathf.Deg2Rad;
                var x = Mathf.Cos(rad) * 22f;
                var y = Mathf.Sin(rad) * 22f;
                var spoke = Look.Prim(PrimitiveType.Cube, "Spoke" + i, wheel, new Vector3(x * 0.5f, y * 0.5f, 0), new Vector3(1.15f, 22.5f, 1.15f), i % 2 == 0 ? red : yellow);
                spoke.transform.localRotation = Quaternion.Euler(0, 0, a - 90f);
                Look.Prim(PrimitiveType.Cube, "Gondola" + i, wheel, new Vector3(x, y - 2.5f, 0), new Vector3(4.4f, 3.6f, 3.8f), i % 2 == 0 ? pink : cream);
            }
        }

        void FunfairBooths(Material wood, Material red, Material cream, Material yellow, Material pink)
        {
            var colors = new[] { red, cream, yellow, pink, red, cream, yellow };
            for (var i = -3; i <= 3; i++)
            {
                if (i == 0) continue;
                var x = i * 30f;
                var root = new GameObject("Booth" + i).transform;
                root.SetParent(_root, false);
                root.position = new Vector3(x, 0, 478);
                var cloth = colors[i + 3];
                Look.Prim(PrimitiveType.Cube, "Counter", root, new Vector3(0, 3.2f, 0), new Vector3(16, 6.4f, 10), wood);
                Look.Prim(PrimitiveType.Cube, "Awning", root, new Vector3(0, 7.4f, 2.2f), new Vector3(18, 0.7f, 14), cloth);
                Look.Prim(PrimitiveType.Cube, "Sign", root, new Vector3(0, 9.2f, 0), new Vector3(12, 2.4f, 0.6f), cream);
            }
        }

        void FunfairTrain(Park park, Material wood, Material red, Material cream, Material yellow)
        {
            var spray = 18f;
            var fence = (float)AtBatResolver.FenceAt(park, spray) - 12f;
            var rad = spray * Mathf.Deg2Rad;
            var p = new Vector3(Mathf.Sin(rad) * fence, 0, Mathf.Cos(rad) * fence);

            var root = new GameObject("TrackTrain").transform;
            root.SetParent(_root, false);
            root.position = p;
            root.rotation = Quaternion.Euler(0, spray, 0);

            Look.Prim(PrimitiveType.Cube, "Engine", root, new Vector3(0, 4.2f, 0), new Vector3(8.5f, 6.4f, 14), red);
            Look.Prim(PrimitiveType.Cube, "Cab", root, new Vector3(0, 8.4f, -4.2f), new Vector3(8.2f, 4.6f, 6.4f), cream);
            Look.Prim(PrimitiveType.Cylinder, "Stack", root, new Vector3(0, 10.2f, 3.6f), new Vector3(2.2f, 2.4f, 2.2f), wood);
            Look.Prim(PrimitiveType.Cube, "Boxcar", root, new Vector3(0, 4.0f, -16f), new Vector3(8.2f, 6.2f, 14), yellow);
            Look.Prim(PrimitiveType.Cube, "Stripe", root, new Vector3(0, 4.0f, -16f), new Vector3(8.4f, 1.4f, 14.2f), red);
            Look.Prim(PrimitiveType.Cube, "Plate", root, new Vector3(4.3f, 5.6f, -16f), new Vector3(0.2f, 2.4f, 3.2f), cream);
            Wheel("WheelFL", root, new Vector3(-3.4f, 1.3f, 4.2f), wood);
            Wheel("WheelFR", root, new Vector3(3.4f, 1.3f, 4.2f), wood);
            Wheel("WheelBL", root, new Vector3(-3.4f, 1.3f, -16f), wood);
            Wheel("WheelBR", root, new Vector3(3.4f, 1.3f, -16f), wood);
        }

        static void Wheel(string name, Transform parent, Vector3 localPos, Material mat)
        {
            var go = Look.Prim(PrimitiveType.Cylinder, name, parent, localPos, new Vector3(2.4f, 0.55f, 2.4f), mat);
            go.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        }

        void FunfairNightHook()
        {
            var go = new GameObject("Chompers");
            go.transform.SetParent(_root, false);
            go.transform.position = Vector3.zero;
            if (_night)
            {
                foreach (var h in ParkHazards.FunfairChompers)
                    ChomperMouth(go.transform, h);
            }
            go.SetActive(_night);
        }

        void ChomperMouth(Transform parent, Hazard h)
        {
            var stem = Look.Lit(new Color(0.16f, 0.48f, 0.18f), smooth: 0.12f);
            var lip = Look.Lit(new Color(0.82f, 0.12f, 0.16f), smooth: 0.18f);
            var hole = Look.Unlit(new Color(0.08f, 0.02f, 0.04f));
            var tooth = Look.Unlit(new Color(0.96f, 0.92f, 0.78f));
            var r = Mathf.Max(3.2f, (float)h.Radius * 0.38f);
            var root = new GameObject("Chomper-" + (h.Tag ?? "?")).transform;
            root.SetParent(parent, false);
            root.position = new Vector3((float)h.X, 0, (float)h.Z);
            Look.Prim(PrimitiveType.Cylinder, "Stem", root, new Vector3(0, 4.2f, 0), new Vector3(r * 0.7f, 4.2f, r * 0.7f), stem);
            Look.Prim(PrimitiveType.Sphere, "Head", root, new Vector3(0, 9.2f, 0.6f), Vector3.one * r * 2.1f, stem);
            Look.Prim(PrimitiveType.Cylinder, "Mouth", root, new Vector3(0, 9.0f, r * 0.85f), new Vector3(r * 1.55f, r * 0.22f, r * 1.15f), hole);
            Look.Prim(PrimitiveType.Cube, "Jaw", root, new Vector3(0, 8.2f, r * 0.9f), new Vector3(r * 1.7f, r * 0.35f, r * 0.7f), lip);
            Look.Prim(PrimitiveType.Cube, "ToothL", root, new Vector3(-r * 0.35f, 8.55f, r * 1.05f), new Vector3(r * 0.22f, r * 0.28f, r * 0.18f), tooth);
            Look.Prim(PrimitiveType.Cube, "ToothR", root, new Vector3(r * 0.35f, 8.55f, r * 1.05f), new Vector3(r * 0.22f, r * 0.28f, r * 0.18f), tooth);
            Glow("ChompGlow", new Vector3((float)h.X, 9f, (float)h.Z), new Color(0.7f, 0.12f, 0.18f), 1.1f, 28f);
        }

        void RooftopDeck(Park park)
        {
            var tar = Look.Lit(new Color(0.28f, 0.28f, 0.30f), smooth: 0.12f);
            var steel = Look.Lit(new Color(0.48f, 0.50f, 0.54f), smooth: 0.32f);
            var neon = Look.Unlit(new Color(0.22f, 0.82f, 1f));
            var gold = Look.Lit(Colors.Gold, smooth: 0.5f);
            var magenta = Look.Unlit(new Color(1f, 0.28f, 0.72f));

            WarningTrack(park);
            Cube("ChainBack", new Vector3(0, 8f, -24f), new Vector3(38, 16, 0.45f), steel);
            Cube("ChainL", new Vector3(-20, 7f, -12f), new Vector3(0.45f, 14, 16f), steel);
            Cube("ChainR", new Vector3(20, 7f, -12f), new Vector3(0.45f, 14, 16f), steel);
            Cube("Bench1B", new Vector3(42, 1.0f, 22), new Vector3(20, 1.0f, 6), tar);
            Cube("Awning1B", new Vector3(42, 5.4f, 22), new Vector3(22, 0.35f, 8), neon);
            Cube("Bench3B", new Vector3(-42, 1.0f, 22), new Vector3(20, 1.0f, 6), tar);
            Cube("Awning3B", new Vector3(-42, 5.4f, 22), new Vector3(22, 0.35f, 8), magenta);
            Cube("HomeRoofStand", new Vector3(0, 10, -62), new Vector3(76, 16, 14), tar);
            Cube("HomeNeon", new Vector3(0, 18.6f, -62), new Vector3(80, 0.5f, 16), gold);
            Cube("LeftRoof", new Vector3(-118, 12, 40), new Vector3(16, 18, 86), tar);
            Cube("RightRoof", new Vector3(118, 12, 40), new Vector3(16, 18, 86), tar);
            CrowdCard("CrowdH", new Vector3(0, 12, -70), new Vector3(70, 12, 1));
            CrowdCard("CrowdL", new Vector3(-126, 14, 40), new Vector3(1, 14, 76));
            CrowdCard("CrowdR", new Vector3(126, 14, 40), new Vector3(1, 14, 76));
            Cube("ParapetLip", new Vector3(0, 2.0f, -36), new Vector3(70, 1.2f, 1.2f), steel);
            AcUnit(new Hazard("ac_unit", -52, 118, 6, null));
            AcUnit(new Hazard("ac_unit", 72, 188, 6, null));
            RooftopSkyline(tar, steel, gold, neon, magenta);
            RooftopNightHook();
        }

        void RooftopSkyline(Material tar, Material steel, Material gold, Material neon, Material magenta)
        {
            var brick = Look.Lit(new Color(0.42f, 0.22f, 0.18f), smooth: 0.1f);
            var glass = Look.Lit(new Color(0.22f, 0.32f, 0.48f), smooth: 0.62f);
            Building("LoftGold", new Vector3(-70, 0, 498), 28, 22, 52, brick, gold);
            Building("TowerCyan", new Vector3(-18, 0, 512), 20, 18, 72, glass, neon);
            Building("TowerMag", new Vector3(48, 0, 505), 24, 20, 64, tar, magenta);
            Building("BlockR", new Vector3(110, 0, 488), 36, 18, 40, steel, gold);
            Building("BlockL", new Vector3(-130, 0, 470), 30, 16, 36, brick, neon);
            Cylinder("WaterTower", new Vector3(0, 0, 528), 6.5f, 18f, steel);
            Cube("TowerTank", new Vector3(0, 22, 528), new Vector3(14, 8, 14), steel);
            Cube("SkySign", new Vector3(0, 48, 512), new Vector3(22, 10, 1.4f), gold);
            Cube("SkyStar", new Vector3(0, 48, 511.2f), new Vector3(6.5f, 6.5f, 0.6f), Look.Unlit(Colors.Gold));
            Glow("NeonFill", new Vector3(0, 28, 420), new Color(0.35f, 0.7f, 1f), 1.6f, 220f);
        }

        void Building(string name, Vector3 pos, float w, float d, float h, Material body, Material accent)
        {
            var root = new GameObject(name).transform;
            root.SetParent(_root, false);
            root.position = pos;
            Look.Prim(PrimitiveType.Cube, "Body", root, new Vector3(0, h * 0.5f, 0), new Vector3(w, h, d), body);
            Look.Prim(PrimitiveType.Cube, "Crown", root, new Vector3(0, h + 1.4f, 0), new Vector3(w * 0.7f, 2.8f, d * 0.7f), accent);
            Look.Prim(PrimitiveType.Cube, "Band", root, new Vector3(0, h * 0.62f, d * 0.52f), new Vector3(w * 0.82f, 1.6f, 0.4f), accent);
        }

        void RooftopNightHook()
        {
            var go = new GameObject("NeonGlare");
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector3(0, 36, 240);
            if (_night)
                Glow("GlareFill", new Vector3(0, 28, 240), new Color(0.4f, 0.8f, 1f), 2.2f, 260f);
            go.SetActive(_night);
        }

        void CanopyGrounds(Park park)
        {
            var bark = Look.Lit(new Color(0.36f, 0.21f, 0.11f), smooth: 0.08f);
            var leaf = Look.Lit(new Color(0.12f, 0.4f, 0.18f), smooth: 0.1f);
            var vine = Look.Lit(new Color(0.22f, 0.48f, 0.18f), smooth: 0.12f);
            var wood = Look.Lit(new Color(0.46f, 0.28f, 0.14f), smooth: 0.1f);

            WarningTrack(park);
            Cube("VineBack", new Vector3(0, 8f, -24f), new Vector3(38, 16, 0.7f), vine);
            Cube("VineL", new Vector3(-20, 7f, -12f), new Vector3(0.7f, 14, 16f), vine);
            Cube("VineR", new Vector3(20, 7f, -12f), new Vector3(0.7f, 14, 16f), vine);
            Cube("LogBench1B", new Vector3(42, 1.0f, 22), new Vector3(20, 1.0f, 6), wood);
            Cube("LeafAwning1B", new Vector3(42, 5.4f, 22), new Vector3(22, 0.6f, 8), leaf);
            Cube("LogBench3B", new Vector3(-42, 1.0f, 22), new Vector3(20, 1.0f, 6), wood);
            Cube("LeafAwning3B", new Vector3(-42, 5.4f, 22), new Vector3(22, 0.6f, 8), leaf);
            Cube("HomeGrove", new Vector3(0, 10, -62), new Vector3(72, 16, 14), bark);
            Cube("HomeCanopy", new Vector3(0, 19.2f, -62), new Vector3(80, 4, 18), leaf);
            Cube("LeftGrove", new Vector3(-118, 12, 40), new Vector3(16, 18, 86), bark);
            Cube("RightGrove", new Vector3(118, 12, 40), new Vector3(16, 18, 86), bark);
            CrowdCard("CrowdH", new Vector3(0, 12, -70), new Vector3(64, 12, 1));
            CrowdCard("CrowdL", new Vector3(-126, 14, 40), new Vector3(1, 14, 76));
            CrowdCard("CrowdR", new Vector3(126, 14, 40), new Vector3(1, 14, 76));
            VineWall("ClimbL", new Vector3(-96, 0, 210), 18, 36, 14, bark, vine);
            VineWall("ClimbR", new Vector3(96, 0, 210), 18, 36, 14, bark, vine);
            for (var i = -3; i <= 3; i++)
                JungleTree(new Vector3(i * 36f, 0, 448), 10f + (i & 1) * 2f);
            CanopyNightHook();
        }

        void VineWall(string name, Vector3 pos, float w, float h, float d, Material bark, Material vine)
        {
            var root = new GameObject(name).transform;
            root.SetParent(_root, false);
            root.position = pos;
            Look.Prim(PrimitiveType.Cube, "Face", root, new Vector3(0, h * 0.5f, 0), new Vector3(w, h, d), bark);
            Look.Prim(PrimitiveType.Cube, "LedgeLow", root, new Vector3(0, 2.2f, d * 0.42f), new Vector3(w * 0.92f, 0.45f, 1.6f), bark);
            Look.Prim(PrimitiveType.Cube, "LedgeClamber", root, new Vector3(0, 4.2f, d * 0.42f), new Vector3(w * 0.92f, 0.45f, 1.8f), bark);
            Look.Prim(PrimitiveType.Cube, "LedgeHigh", root, new Vector3(0, 6.4f, d * 0.42f), new Vector3(w * 0.92f, 0.45f, 1.6f), bark);
            Look.Prim(PrimitiveType.Cube, "Vines", root, new Vector3(0, h * 0.55f, d * 0.52f), new Vector3(w * 0.7f, h * 0.9f, 0.35f), vine);
        }

        void JungleTree(Vector3 p, float radius)
        {
            var bark = Look.Lit(new Color(0.36f, 0.21f, 0.11f), smooth: 0.08f);
            var leaf = Look.Lit(new Color(0.12f, 0.4f, 0.18f), smooth: 0.1f);
            var moss = Look.Lit(new Color(0.22f, 0.48f, 0.18f), smooth: 0.1f);
            var root = new GameObject("Tree").transform;
            root.SetParent(_root, false);
            root.position = p;
            var h = Mathf.Clamp(radius * 1.6f, 10f, 22f);
            Look.Prim(PrimitiveType.Cylinder, "Trunk", root, new Vector3(0, h * 0.5f, 0), new Vector3(radius * 0.42f, h * 0.5f, radius * 0.42f), bark);
            Look.Prim(PrimitiveType.Sphere, "Canopy", root, new Vector3(0, h + radius * 0.35f, 0), Vector3.one * radius * 1.6f, leaf);
            Look.Prim(PrimitiveType.Sphere, "Canopy2", root, new Vector3(radius * 0.45f, h + radius * 0.1f, radius * 0.2f), Vector3.one * radius * 1.1f, moss);
            Look.Prim(PrimitiveType.Cube, "RootL", root, new Vector3(-radius * 0.4f, 0.4f, 0), new Vector3(radius * 0.7f, 0.7f, 0.7f), bark);
            Look.Prim(PrimitiveType.Cube, "RootR", root, new Vector3(radius * 0.4f, 0.4f, 0), new Vector3(radius * 0.7f, 0.7f, 0.7f), bark);
        }

        void ClimbWall(Hazard h)
        {
            var bark = Look.Lit(new Color(0.36f, 0.21f, 0.11f), smooth: 0.08f);
            var vine = Look.Lit(new Color(0.22f, 0.48f, 0.18f), smooth: 0.12f);
            var p = new Vector3((float)h.X, 0, (float)h.Z);
            var w = Mathf.Max(28f, (float)h.Radius * 0.7f);
            VineWall("ClimbWall", p, w, 14f, 6f, bark, vine);
        }

        void BarrelCannon(Hazard h)
        {
            var r = Mathf.Max(2.2f, (float)h.Radius);
            var tag = string.IsNullOrWhiteSpace(h.Tag) ? "?" : h.Tag;
            var wood = Look.Lit(new Color(0.46f, 0.28f, 0.11f), smooth: 0.12f);
            var band = Look.Lit(new Color(0.62f, 0.50f, 0.28f), smooth: 0.35f);
            var hole = Look.Unlit(new Color(0.06f, 0.04f, 0.03f));
            var pip = Look.Unlit(Color.white);

            var root = new GameObject("BarrelCannon-" + tag).transform;
            root.SetParent(_root, false);
            root.position = new Vector3((float)h.X, 0, (float)h.Z);
            root.rotation = Quaternion.Euler(-18f, 0f, 0f);

            Look.Prim(PrimitiveType.Cylinder, "Body", root, new Vector3(0, r * 0.9f, 0), new Vector3(r * 1.7f, r * 0.9f, r * 1.7f), wood);
            Look.Prim(PrimitiveType.Cylinder, "BandLow", root, new Vector3(0, r * 0.35f, 0), new Vector3(r * 1.82f, r * 0.12f, r * 1.82f), band);
            Look.Prim(PrimitiveType.Cylinder, "BandHigh", root, new Vector3(0, r * 1.45f, 0), new Vector3(r * 1.82f, r * 0.12f, r * 1.82f), band);
            Look.Prim(PrimitiveType.Cylinder, "Lip", root, new Vector3(0, r * 1.88f, 0), new Vector3(r * 2.0f, r * 0.16f, r * 2.0f), band);
            Look.Prim(PrimitiveType.Cylinder, "Mouth", root, new Vector3(0, r * 1.72f, 0), new Vector3(r * 1.25f, r * 0.28f, r * 1.25f), hole);
            Look.Prim(PrimitiveType.Cylinder, "Well", root, new Vector3(0, r * 1.2f, 0), new Vector3(r * 1.1f, r * 0.5f, r * 1.1f), hole);
            var n = tag == "A" ? 1 : tag == "B" ? 2 : 3;
            for (var i = 0; i < n; i++)
            {
                var x = (i - (n - 1) * 0.5f) * (r * 0.28f);
                Look.Prim(PrimitiveType.Cube, "Pip" + i, root, new Vector3(x, r * 0.9f, r * 0.92f), new Vector3(r * 0.16f, r * 0.16f, 0.12f), pip);
            }
        }

        void CanopyNightHook()
        {
            var go = new GameObject("Fireflies");
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector3(0, 8, 240);
            if (_night)
            {
                var gold = Look.Unlit(new Color(0.92f, 1f, 0.42f));
                for (var i = 0; i < 12; i++)
                {
                    var a = i / 12f * Mathf.PI * 2f;
                    Look.Prim(PrimitiveType.Sphere, "Fly" + i, go.transform,
                        new Vector3(Mathf.Cos(a) * 40f, 4f + (i % 4), Mathf.Sin(a) * 30f),
                        Vector3.one * 0.55f, gold);
                }
            }
            go.SetActive(_night);
        }

        void EmberCourtyard(Park park)
        {
            var stone = Look.Lit(new Color(0.22f, 0.14f, 0.16f), smooth: 0.1f);
            var iron = Look.Lit(new Color(0.28f, 0.22f, 0.22f), smooth: 0.28f);
            var fire = Look.Unlit(Colors.EmberFire);
            var gold = Look.Lit(Colors.Gold, smooth: 0.45f);

            WarningTrack(park);
            Cube("IronBack", new Vector3(0, 8f, -24f), new Vector3(38, 16, 0.55f), iron);
            Cube("IronL", new Vector3(-20, 7f, -12f), new Vector3(0.55f, 14, 16f), iron);
            Cube("IronR", new Vector3(20, 7f, -12f), new Vector3(0.55f, 14, 16f), iron);
            Cube("StoneBench1B", new Vector3(42, 1.0f, 22), new Vector3(20, 1.0f, 6), stone);
            Cube("StoneBench3B", new Vector3(-42, 1.0f, 22), new Vector3(20, 1.0f, 6), stone);
            Cube("HomeKeep", new Vector3(0, 10, -62), new Vector3(76, 18, 16), stone);
            Cube("HomeCrenel", new Vector3(0, 20.2f, -62), new Vector3(82, 2.4f, 18), iron);
            Cube("LeftBattlement", new Vector3(-118, 12, 40), new Vector3(16, 20, 90), stone);
            Cube("RightBattlement", new Vector3(118, 12, 40), new Vector3(16, 20, 90), stone);
            CrowdCard("CrowdH", new Vector3(0, 12, -70), new Vector3(68, 12, 1));
            CrowdCard("CrowdL", new Vector3(-126, 14, 40), new Vector3(1, 14, 80));
            CrowdCard("CrowdR", new Vector3(126, 14, 40), new Vector3(1, 14, 80));
            Cube("AshLip", new Vector3(0, 2.0f, -36), new Vector3(70, 1.4f, 1.4f), gold);
            KeepCastle(stone, iron, gold, fire);
            Brazier(new Vector3(-36, 0, -40), fire, stone);
            Brazier(new Vector3(36, 0, -40), fire, stone);
            Glow("CourtyardGlow", new Vector3(0, 10, 180), Colors.EmberFire, 1.8f, 260f);
            EmberNightHook();
        }

        void KeepCastle(Material stone, Material iron, Material gold, Material fire)
        {
            Cube("KeepHall", new Vector3(0, 28, 500), new Vector3(78, 56, 32), stone);
            Cube("KeepRoof", new Vector3(0, 58, 500), new Vector3(86, 6, 36), iron);
            for (var i = -3; i <= 3; i++)
                Cube("Merlon" + i, new Vector3(i * 11f, 63.5f, 500), new Vector3(6, 5, 8), stone);
            Tower("TowerL", new Vector3(-52, 0, 500), stone, iron, gold);
            Tower("TowerR", new Vector3(52, 0, 500), stone, iron, gold);
            Cube("Gate", new Vector3(0, 14, 480), new Vector3(22, 28, 8), iron);
            Cube("GateArch", new Vector3(0, 30, 480), new Vector3(26, 6, 8), gold);
            Cube("Portcullis", new Vector3(0, 10, 476), new Vector3(14, 18, 1.2f), fire);
            Cube("WingL", new Vector3(-96, 16, 488), new Vector3(36, 32, 18), stone);
            Cube("WingR", new Vector3(96, 16, 488), new Vector3(36, 32, 18), stone);
        }

        void Tower(string name, Vector3 pos, Material stone, Material iron, Material gold)
        {
            var root = new GameObject(name).transform;
            root.SetParent(_root, false);
            root.position = pos;
            Look.Prim(PrimitiveType.Cube, "Shaft", root, new Vector3(0, 42, 0), new Vector3(18, 84, 18), stone);
            Look.Prim(PrimitiveType.Cube, "Crown", root, new Vector3(0, 86, 0), new Vector3(22, 4, 22), iron);
            for (var i = 0; i < 4; i++)
            {
                var x = (i % 2 == 0 ? -7f : 7f);
                var z = i < 2 ? -7f : 7f;
                Look.Prim(PrimitiveType.Cube, "Tooth" + i, root, new Vector3(x, 90, z), new Vector3(5, 5, 5), stone);
            }
            Look.Prim(PrimitiveType.Cube, "Banner", root, new Vector3(0, 70, 9.2f), new Vector3(6, 10, 0.3f), gold);
        }

        void Brazier(Vector3 p, Material fire, Material stone)
        {
            var root = new GameObject("Brazier").transform;
            root.SetParent(_root, false);
            root.position = p;
            Look.Prim(PrimitiveType.Cylinder, "Bowl", root, new Vector3(0, 2.4f, 0), new Vector3(3.6f, 0.7f, 3.6f), stone);
            Look.Prim(PrimitiveType.Cylinder, "Stem", root, new Vector3(0, 1.2f, 0), new Vector3(1.1f, 1.2f, 1.1f), stone);
            Look.Prim(PrimitiveType.Sphere, "Flame", root, new Vector3(0, 3.6f, 0), Vector3.one * 1.8f, fire);
            Glow("BrazierGlow", p + new Vector3(0, 4.2f, 0), Colors.EmberFire, 1.2f, 40f);
        }

        void LavaPit(Hazard h)
        {
            var r = Mathf.Max(3f, (float)h.Radius);
            var stone = Look.Lit(new Color(0.22f, 0.14f, 0.16f), smooth: 0.1f);
            var lava = Look.Unlit(Colors.EmberFire);
            var glow = Look.Unlit(new Color(1f, 0.35f, 0.08f));
            var root = new GameObject("LavaPit").transform;
            root.SetParent(_root, false);
            root.position = new Vector3((float)h.X, 0, (float)h.Z);
            Look.Prim(PrimitiveType.Cylinder, "Rim", root, new Vector3(0, 0.55f, 0), new Vector3(r * 2.2f, 0.55f, r * 2.2f), stone);
            Look.Prim(PrimitiveType.Cylinder, "Well", root, new Vector3(0, 0.18f, 0), new Vector3(r * 1.7f, 0.22f, r * 1.7f), lava);
            Look.Prim(PrimitiveType.Cylinder, "Glow", root, new Vector3(0, 0.42f, 0), new Vector3(r * 1.45f, 0.08f, r * 1.45f), glow);
            Glow("LavaGlow", new Vector3((float)h.X, 1.4f, (float)h.Z), Colors.EmberFire, 1.4f, r * 6f);
        }

        void FireStatue(Vector3 p, float radius, bool breath)
        {
            var stone = Look.Lit(new Color(0.22f, 0.14f, 0.16f), smooth: 0.1f);
            var ember = Look.Lit(Colors.Ember, smooth: 0.12f);
            var fire = Look.Unlit(Colors.EmberFire);
            var gold = Look.Lit(Colors.Gold, smooth: 0.45f);
            var root = new GameObject(breath ? "FireBreath" : "KeepStatue").transform;
            root.SetParent(_root, false);
            root.position = p;

            var ped = Mathf.Clamp(radius * 0.38f, 2.2f, 4.2f);
            Look.Prim(PrimitiveType.Cylinder, "Pedestal", root, new Vector3(0, 1.0f, 0), new Vector3(ped * 2, 1.0f, ped * 2), stone);
            Look.Prim(PrimitiveType.Cube, "Plinth", root, new Vector3(0, 2.05f, 0), new Vector3(ped * 1.7f, 0.22f, ped * 1.7f), gold);
            Look.Prim(PrimitiveType.Capsule, "LegL", root, new Vector3(-0.55f, 3.4f, 0), new Vector3(0.65f, 1.25f, 0.65f), ember);
            Look.Prim(PrimitiveType.Capsule, "LegR", root, new Vector3(0.55f, 3.4f, 0), new Vector3(0.65f, 1.25f, 0.65f), ember);
            Look.Prim(PrimitiveType.Cube, "Torso", root, new Vector3(0, 5.3f, 0), new Vector3(2.1f, 2.2f, 1.2f), ember);
            Look.Prim(PrimitiveType.Sphere, "Head", root, new Vector3(0, 6.9f, 0.15f), Vector3.one * 1.35f, ember);
            Look.Prim(PrimitiveType.Cube, "HornL", root, new Vector3(-0.55f, 7.7f, 0), new Vector3(0.28f, 0.9f, 0.28f), gold);
            Look.Prim(PrimitiveType.Cube, "HornR", root, new Vector3(0.55f, 7.7f, 0), new Vector3(0.28f, 0.9f, 0.28f), gold);
            var armL = Look.Prim(PrimitiveType.Capsule, "ArmL", root, new Vector3(-1.5f, 5.6f, 0.2f), new Vector3(0.45f, 1.15f, 0.45f), ember);
            armL.transform.localRotation = Quaternion.Euler(0, 0, 28f);
            var armR = Look.Prim(PrimitiveType.Capsule, "ArmR", root, new Vector3(1.5f, 5.6f, 0.2f), new Vector3(0.45f, 1.15f, 0.45f), ember);
            armR.transform.localRotation = Quaternion.Euler(0, 0, -28f);
            if (breath)
            {
                var amp = _night ? (float)ParkHazards.EmberNightFireMul : 1f;
                var br = radius * amp;
                Look.Prim(PrimitiveType.Cylinder, "Breath", root, new Vector3(0, 6.6f, 2.8f), new Vector3(br * 0.55f, br * 0.55f, br * 0.55f), fire);
                var cone = Look.Prim(PrimitiveType.Cylinder, "Flame", root, new Vector3(0, 6.4f, 5.4f * amp), new Vector3(br * 1.1f, br * 0.7f, br * 1.1f), fire);
                cone.transform.localRotation = Quaternion.Euler(78f, 0f, 0f);
                Glow("BreathGlow", p + new Vector3(0, 6.4f, 4.2f * amp), Colors.EmberFire, _night ? 2.8f : 1.6f, br * 5f);
            }
            else
            {
                Look.Prim(PrimitiveType.Cube, "Sash", root, new Vector3(0.2f, 5.2f, 0.65f), new Vector3(0.8f, 0.2f, 0.1f), gold);
            }
        }

        void EmberNightHook()
        {
            var fire = Look.Unlit(Colors.EmberFire);
            var stone = Look.Lit(new Color(0.22f, 0.14f, 0.16f), smooth: 0.1f);
            var go = new GameObject("NightBraziers");
            go.transform.SetParent(_root, false);
            go.transform.position = Vector3.zero;
            if (_night)
            {
                Brazier(new Vector3(-88, 0, 120), fire, stone);
                Brazier(new Vector3(88, 0, 120), fire, stone);
                Brazier(new Vector3(-70, 0, 280), fire, stone);
                Brazier(new Vector3(70, 0, 280), fire, stone);
                Glow("NightFire", new Vector3(0, 14, 220), Colors.EmberFire, 3.4f, 380f);
            }
            go.SetActive(_night);
        }

        void StarBillboard(Hazard h)
        {
            var gold = Look.Lit(Colors.Gold, smooth: 0.42f);
            var frame = Look.Lit(new Color(0.18f, 0.16f, 0.2f), smooth: 0.2f);
            var star = Look.Unlit(new Color(1f, 0.92f, 0.35f));
            var neon = Look.Unlit(new Color(1f, 0.28f, 0.72f));
            var root = new GameObject("StarBillboard").transform;
            root.SetParent(_root, false);
            root.position = new Vector3((float)h.X, 0, (float)h.Z);
            Look.Prim(PrimitiveType.Cylinder, "Pole", root, new Vector3(0, 10f, 0), new Vector3(1.2f, 10f, 1.2f), frame);
            Look.Prim(PrimitiveType.Cube, "Frame", root, new Vector3(0, 20f, 0), new Vector3(26, 18, 2.4f), frame);
            Look.Prim(PrimitiveType.Cube, "Face", root, new Vector3(0, 20f, -0.4f), new Vector3(23, 15, 1.2f), gold);
            Look.Prim(PrimitiveType.Cube, "Star", root, new Vector3(0, 20.4f, -1.1f), new Vector3(7.2f, 7.2f, 0.5f), star);
            var diamond = Look.Prim(PrimitiveType.Cube, "StarTilt", root, new Vector3(0, 20.4f, -1.15f), new Vector3(7.2f, 7.2f, 0.4f), star);
            diamond.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            Look.Prim(PrimitiveType.Cube, "Neon", root, new Vector3(0, 28.4f, 0), new Vector3(26.4f, 0.4f, 2.6f), neon);
            Glow("SignGlow", new Vector3((float)h.X, 20f, (float)h.Z), Colors.Gold, 1.1f, 48f);
        }

        void AcUnit(Hazard h)
        {
            var steel = Look.Lit(new Color(0.55f, 0.55f, 0.58f), smooth: 0.3f);
            var dark = Look.Lit(new Color(0.28f, 0.28f, 0.30f), smooth: 0.18f);
            var fan = Look.Lit(new Color(0.22f, 0.24f, 0.28f), smooth: 0.4f);
            var root = new GameObject("AcUnit").transform;
            root.SetParent(_root, false);
            root.position = new Vector3((float)h.X, 0, (float)h.Z);
            Look.Prim(PrimitiveType.Cube, "Body", root, new Vector3(0, 2.2f, 0), new Vector3(8, 4.4f, 8), steel);
            Look.Prim(PrimitiveType.Cube, "Grille", root, new Vector3(0, 2.4f, 4.15f), new Vector3(6.2f, 2.8f, 0.2f), dark);
            Look.Prim(PrimitiveType.Cylinder, "Fan", root, new Vector3(0, 4.7f, 0), new Vector3(3.6f, 0.35f, 3.6f), fan);
            Look.Prim(PrimitiveType.Cube, "VentL", root, new Vector3(-4.15f, 2.4f, 0), new Vector3(0.2f, 2.4f, 5.2f), dark);
            Look.Prim(PrimitiveType.Cube, "VentR", root, new Vector3(4.15f, 2.4f, 0), new Vector3(0.2f, 2.4f, 5.2f), dark);
            Look.Prim(PrimitiveType.Cylinder, "Pipe", root, new Vector3(3.2f, 0.7f, -3.2f), new Vector3(0.7f, 0.7f, 0.7f), dark);
        }

        void FreezeStatue(Vector3 p, float radius, int pose)
        {
            var ice = Look.Lit(new Color(0.68f, 0.88f, 1f), smooth: 0.78f);
            var stone = Look.Lit(new Color(0.70f, 0.76f, 0.84f), smooth: 0.22f);
            var frost = Look.Lit(new Color(0.82f, 0.94f, 1f), smooth: 0.88f);
            var gold = Look.Lit(Colors.Gold, smooth: 0.5f);

            var root = new GameObject("FreezeStatue").transform;
            root.SetParent(_root, false);
            root.position = p;
            root.rotation = Quaternion.Euler(0, pose == 1 ? -35f : pose == 2 ? 25f : 8f, 0);

            var ped = Mathf.Clamp(radius * 0.38f, 2.0f, 3.4f);
            Look.Prim(PrimitiveType.Cylinder, "FrostRing", root, new Vector3(0, 0.06f, 0), new Vector3(radius * 2, 0.05f, radius * 2), frost);
            Look.Prim(PrimitiveType.Cylinder, "Pedestal", root, new Vector3(0, 0.85f, 0), new Vector3(ped * 2, 0.85f, ped * 2), stone);
            Look.Prim(PrimitiveType.Cube, "Plinth", root, new Vector3(0, 1.75f, 0), new Vector3(ped * 1.7f, 0.22f, ped * 1.7f), frost);
            Look.Prim(PrimitiveType.Capsule, "LegL", root, new Vector3(-0.45f, 3.0f, 0), new Vector3(0.55f, 1.15f, 0.55f), ice);
            Look.Prim(PrimitiveType.Capsule, "LegR", root, new Vector3(0.45f, 3.0f, 0), new Vector3(0.55f, 1.15f, 0.55f), ice);
            Look.Prim(PrimitiveType.Cube, "Torso", root, new Vector3(0, 4.7f, 0), new Vector3(1.7f, 1.9f, 1.05f), ice);
            Look.Prim(PrimitiveType.Sphere, "Head", root, new Vector3(0, 6.15f, 0), Vector3.one * 1.15f, ice);

            if (pose == 0)
            {
                Look.Prim(PrimitiveType.Capsule, "ArmL", root, new Vector3(-1.15f, 4.9f, 0.15f), new Vector3(0.4f, 0.95f, 0.4f), ice);
                Look.Prim(PrimitiveType.Capsule, "ArmR", root, new Vector3(1.2f, 5.2f, 0.35f), new Vector3(0.4f, 0.95f, 0.4f), ice);
                var bat = Look.Prim(PrimitiveType.Cylinder, "Bat", root, new Vector3(1.35f, 6.4f, 0.2f), new Vector3(0.22f, 1.5f, 0.22f), ice);
                bat.transform.localRotation = Quaternion.Euler(0, 0, 28f);
                Look.Prim(PrimitiveType.Cube, "Cap", root, new Vector3(0, 6.7f, 0.15f), new Vector3(1.1f, 0.22f, 1.1f), frost);
            }
            else if (pose == 1)
            {
                Look.Prim(PrimitiveType.Capsule, "ArmL", root, new Vector3(-1.1f, 4.6f, 0), new Vector3(0.4f, 0.9f, 0.4f), ice);
                var armR = Look.Prim(PrimitiveType.Capsule, "ArmR", root, new Vector3(0.55f, 6.0f, 0), new Vector3(0.4f, 1.1f, 0.4f), ice);
                armR.transform.localRotation = Quaternion.Euler(0, 0, -40f);
                Look.Prim(PrimitiveType.Sphere, "Ball", root, new Vector3(1.5f, 7.1f, 0), Vector3.one * 0.45f, frost);
            }
            else
            {
                Look.Prim(PrimitiveType.Capsule, "ArmL", root, new Vector3(-1.7f, 5.0f, 0), new Vector3(0.38f, 1.05f, 0.38f), ice);
                Look.Prim(PrimitiveType.Capsule, "ArmR", root, new Vector3(1.7f, 5.0f, 0), new Vector3(0.38f, 1.05f, 0.38f), ice);
                Look.Prim(PrimitiveType.Cylinder, "Crown", root, new Vector3(0, 6.75f, 0), new Vector3(0.9f, 0.18f, 0.9f), gold);
                Look.Prim(PrimitiveType.Cube, "Point", root, new Vector3(0, 7.1f, 0), new Vector3(0.18f, 0.45f, 0.18f), gold);
                Look.Prim(PrimitiveType.Cube, "Sash", root, new Vector3(0.15f, 4.7f, 0.55f), new Vector3(0.7f, 0.16f, 0.08f), frost);
            }
        }

        void WarpCan(Hazard h)
        {
            var r = Mathf.Max(2.4f, (float)h.Radius);
            var tag = string.IsNullOrWhiteSpace(h.Tag) ? "?" : h.Tag;
            var bodyCol = TagColor(tag);
            var body = Look.Lit(bodyCol, smooth: 0.22f);
            var lip = Look.Lit(Color.Lerp(bodyCol, Color.white, 0.28f), smooth: 0.3f);
            var hole = Look.Unlit(new Color(0.05f, 0.04f, 0.06f));
            var badge = Look.Unlit(Color.Lerp(bodyCol, Color.white, 0.18f));
            var pip = Look.Unlit(Color.white);

            var root = new GameObject("WarpCan-" + tag).transform;
            root.SetParent(_root, false);
            root.position = new Vector3((float)h.X, 0, (float)h.Z);
            root.rotation = Quaternion.Euler(-16f, 0f, 0f);

            Look.Prim(PrimitiveType.Cylinder, "Body", root, new Vector3(0, r * 0.82f, 0), new Vector3(r * 1.7f, r * 0.82f, r * 1.7f), body);
            Look.Prim(PrimitiveType.Cylinder, "Lip", root, new Vector3(0, r * 1.72f, 0), new Vector3(r * 2.2f, r * 0.2f, r * 2.2f), lip);
            Look.Prim(PrimitiveType.Cylinder, "Well", root, new Vector3(0, r * 1.28f, 0), new Vector3(r * 1.35f, r * 0.52f, r * 1.35f), hole);
            Look.Prim(PrimitiveType.Cylinder, "Mouth", root, new Vector3(0, r * 1.86f, 0), new Vector3(r * 1.5f, r * 0.1f, r * 1.5f), hole);
            Look.Prim(PrimitiveType.Cylinder, "Band", root, new Vector3(0, r * 0.55f, 0), new Vector3(r * 1.82f, r * 0.16f, r * 1.82f), badge);
            Look.Prim(PrimitiveType.Cube, "Plate", root, new Vector3(0, r * 1.05f, r * 0.92f), new Vector3(r * 0.95f, r * 0.7f, 0.18f), badge);
            var n = tag == "A" ? 1 : tag == "B" ? 2 : 3;
            for (var i = 0; i < n; i++)
            {
                var x = (i - (n - 1) * 0.5f) * (r * 0.28f);
                Look.Prim(PrimitiveType.Cube, "Pip" + i, root, new Vector3(x, r * 1.05f, r * 1.02f), new Vector3(r * 0.18f, r * 0.18f, 0.12f), pip);
            }
        }

        static Color TagColor(string tag) => tag switch
        {
            "A" => new Color(0.92f, 0.16f, 0.24f),
            "B" => new Color(1f, 0.78f, 0.12f),
            "C" => new Color(0.18f, 0.48f, 0.92f),
            _ => Colors.Carnival
        };

        void Hazards(Park park)
        {
            var freezePose = 0;
            foreach (var h in park.Hazards)
            {
                var p = new Vector3((float)h.X, 0, (float)h.Z);
                switch (h.Type)
                {
                    case "freeze_volume":
                        FreezeStatue(p, (float)h.Radius, freezePose++);
                        break;
                    case "warp_pipe":
                        WarpCan(h);
                        break;
                    case "billboard":
                        StarBillboard(h);
                        break;
                    case "ac_unit":
                        AcUnit(h);
                        break;
                    case "barrel":
                        BarrelCannon(h);
                        break;
                    case "lava_pit":
                        LavaPit(h);
                        break;
                    case "fire_breath":
                        FireStatue(p, (float)h.Radius, true);
                        break;
                    case "statue":
                        FireStatue(p, (float)h.Radius, false);
                        break;
                    case "tree":
                        JungleTree(p, (float)h.Radius);
                        break;
                    case "climb_wall":
                        ClimbWall(h);
                        break;
                }
            }
        }

        void Glow(string name, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        void Quad(string name, Vector3 pos, Vector3 scale, Material mat) =>
            Cube(name, pos, scale, mat);

        void Cube(string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            Destroy(go.GetComponent<Collider>());
            Look.Paint(go, mat);
        }

        void Cylinder(string name, Vector3 pos, float radius, float height, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.position = pos + new Vector3(0, height * 0.5f, 0);
            go.transform.localScale = new Vector3(radius * 2, height * 0.5f, radius * 2);
            Destroy(go.GetComponent<Collider>());
            Look.Paint(go, mat);
        }
    }
}
