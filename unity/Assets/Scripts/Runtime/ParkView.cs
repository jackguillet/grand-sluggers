using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed class ParkView : MonoBehaviour
    {
        Transform _root;
        BallView _ball;

        public BallView Ball => _ball;

        public void Build(Park park)
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = new GameObject("Park").transform;
            _root.SetParent(transform, false);

            var ice = park.Surface == "ice";
            var ash = park.Surface == "ash";
            var jungle = park.Id == "canopy-yard";
            var harbor = park.Id == "harbor-diamond";

            var sky = ice ? Colors.Ice : ash ? new Color(0.22f, 0.1f, 0.12f) : Colors.Sky;
            if (Camera.main != null)
            {
                if (harbor) Look.RigAfternoon(Camera.main);
                else Look.SetupLighting(Camera.main, sky);
            }

            var grassCol = ice ? Colors.Ice : ash ? new Color(0.28f, 0.19f, 0.16f) : jungle ? new Color(0.14f, 0.43f, 0.2f) : Colors.Grass;
            var waterCol = ash ? new Color(0.35f, 0.11f, 0.07f) : ice ? Colors.Ice : Colors.Water;
            var grassMat = Look.Lit(grassCol, ice || ash ? null : Look.Grass, ice || ash ? 1f : 18f, 0.08f);
            var dirtMat = Look.Lit(Colors.Dirt, Look.Dirt, 8f, 0.12f);
            var waterMat = Look.Lit(waterCol, smooth: 0.85f);

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
            }
            else
            {
                Stands(ice, ash);
                if (jungle) Jungle();
                else if (ash) Keep();
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
            var wall = Look.Lit(ash ? Colors.EmberFire : new Color(0.22f, 0.48f, 0.28f), smooth: 0.18f);
            var cap = Look.Lit(Colors.Gold, smooth: 0.4f);
            var pole = Look.Lit(Colors.Gold, smooth: 0.45f);
            for (var i = -18; i <= 18; i++)
            {
                var spray = i / 18f * 48f;
                var fence = (float)AtBatResolver.FenceAt(park, spray);
                var rad = spray * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Sin(rad) * fence, 5.2f, Mathf.Cos(rad) * fence);
                Cube("Fence" + i, p, new Vector3(14, 10.4f, 1.8f), wall);
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

        void Jungle()
        {
            var bark = Look.Lit(new Color(0.36f, 0.21f, 0.11f), smooth: 0.08f);
            var leaf = Look.Lit(new Color(0.12f, 0.4f, 0.18f), smooth: 0.1f);
            for (var i = -4; i <= 4; i++)
            {
                Cylinder("Trunk" + i, new Vector3(i * 40, 0, 430), 3.2f, 24f, bark);
                Cylinder("Canopy" + i, new Vector3(i * 40, 26, 430), 14f, 8f, leaf);
            }
        }

        void Keep()
        {
            var stone = Look.Lit(new Color(0.16f, 0.1f, 0.12f), smooth: 0.08f);
            Cube("Keep", new Vector3(0, 30, 475), new Vector3(100, 60, 40), stone);
            Cube("TowerL", new Vector3(-52, 42, 475), new Vector3(18, 84, 18), stone);
            Cube("TowerR", new Vector3(52, 42, 475), new Vector3(18, 84, 18), stone);
        }

        void Hazards(Park park)
        {
            foreach (var h in park.Hazards)
            {
                var p = new Vector3((float)h.X, 0, (float)h.Z);
                switch (h.Type)
                {
                    case "freeze_volume":
                        Cylinder("Freeze", new Vector3(p.x, 2.2f, p.z), (float)h.Radius, 4.5f, Look.Lit(new Color(0.65f, 0.9f, 1f), smooth: 0.7f));
                        break;
                    case "warp_pipe":
                        Cylinder("Pipe", new Vector3(p.x, 3f, p.z), (float)h.Radius, 6f, Look.Lit(new Color(0.15f, 0.65f, 0.28f), smooth: 0.2f));
                        break;
                    case "billboard":
                        Cube("Sign", new Vector3(p.x, 18, p.z), new Vector3(24, 16, 2), Look.Lit(Colors.Gold, smooth: 0.35f));
                        break;
                    case "ac_unit":
                        Cube("Ac", new Vector3(p.x, 2, p.z), new Vector3(8, 4, 8), Look.Lit(new Color(0.55f, 0.55f, 0.58f), smooth: 0.3f));
                        break;
                    case "barrel":
                        Cylinder("Barrel", new Vector3(p.x, 2.6f, p.z), (float)h.Radius, 5.2f, Look.Lit(new Color(0.46f, 0.28f, 0.11f), smooth: 0.12f));
                        break;
                    case "lava_pit":
                        Cylinder("Lava", new Vector3(p.x, 0.4f, p.z), (float)h.Radius, 0.8f, Look.Lit(Colors.EmberFire, smooth: 0.5f));
                        break;
                    case "fire_breath":
                        Cylinder("Fire", new Vector3(p.x, 4f, p.z), (float)h.Radius, 8f, Look.Lit(new Color(1f, 0.35f, 0.1f), smooth: 0.4f));
                        break;
                    case "statue":
                        Cube("Statue", new Vector3(p.x, 10, p.z), new Vector3(10, 20, 10), Look.Lit(Colors.Ember, smooth: 0.1f));
                        break;
                    case "tree":
                        Cylinder("Tree", new Vector3(p.x, 6f, p.z), 1.6f, 12f, Look.Lit(new Color(0.36f, 0.21f, 0.11f), smooth: 0.08f));
                        break;
                }
            }
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
