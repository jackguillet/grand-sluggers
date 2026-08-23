using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed class ParkView : MonoBehaviour
    {
        Transform _root;
        GameObject _ball;
        TrailRenderer _trail;
        readonly System.Collections.Generic.Dictionary<string, Transform> _people = new();

        public Transform Ball => _ball.transform;

        public void Build(Park park)
        {
            if (_root != null) Destroy(_root.gameObject);
            _people.Clear();
            _root = new GameObject("Park").transform;
            _root.SetParent(transform, false);

            var ice = park.Surface == "ice";
            var ash = park.Surface == "ash";
            var jungle = park.Id == "canopy-yard";
            var water = ash ? new Color(0.35f, 0.11f, 0.07f) : ice ? Colors.Ice : Colors.Water;
            var grass = ash ? new Color(0.28f, 0.19f, 0.16f)
                : jungle ? new Color(0.14f, 0.43f, 0.2f)
                : ice ? Colors.Ice
                : Colors.Grass;
            Quad("Water", new Vector3(0, -1.2f, 220), new Vector3(900, 1, 900), water);
            Quad("Grass", new Vector3(0, -0.15f, 180), new Vector3(560, 0.4f, 560), grass);
            Quad("Dirt", new Vector3(0, 0.05f, 70), new Vector3(160, 0.2f, 160), Colors.Dirt);
            Cylinder("Mound", new Vector3(0, 0.4f, 60.5f), 9f, 1.1f, Colors.Dirt);
            Bag(Diamond.First.X, Diamond.First.Z);
            Bag(Diamond.Second.X, Diamond.Second.Z);
            Bag(Diamond.Third.X, Diamond.Third.Z);
            Bag(0, -0.4);

            for (var i = -16; i <= 16; i++)
            {
                var spray = i / 16f * 48f;
                var fence = (float)AtBatResolver.FenceAt(park, spray);
                var a = spray * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Sin(a) * fence, 8f, Mathf.Cos(a) * fence);
                Cube($"Fence{i}", p, new Vector3(10, 16, 3.2f), ice ? Colors.Chalk : Colors.Fence);
            }

            foreach (var h in park.Hazards)
            {
                var p = new Vector3((float)h.X, 0, (float)h.Z);
                switch (h.Type)
                {
                    case "freeze_volume":
                        Cylinder($"Freeze{h.X}", new Vector3(p.x, 2.2f, p.z), (float)h.Radius, 4.5f, new Color(0.65f, 0.9f, 1f));
                        break;
                    case "warp_pipe":
                        Cylinder($"Pipe{h.X}", new Vector3(p.x, 3f, p.z), (float)h.Radius, 6f, new Color(0.15f, 0.65f, 0.28f));
                        break;
                    case "billboard":
                        Cube($"Sign{h.X}", new Vector3(p.x, 18, p.z), new Vector3(24, 16, 2), Colors.Gold);
                        break;
                    case "ac_unit":
                        Cube($"Ac{h.X}", new Vector3(p.x, 2, p.z), new Vector3(8, 4, 8), new Color(0.55f, 0.55f, 0.58f));
                        break;
                    case "barrel":
                        Cylinder($"Barrel{h.X}", new Vector3(p.x, 2.6f, p.z), (float)h.Radius, 5.2f, new Color(0.46f, 0.28f, 0.11f));
                        break;
                    case "lava_pit":
                        Cylinder($"Lava{h.X}", new Vector3(p.x, 0.4f, p.z), (float)h.Radius, 0.8f, Colors.EmberFire);
                        break;
                    case "fire_breath":
                        Cylinder($"Fire{h.X}", new Vector3(p.x, 4f, p.z), (float)h.Radius, 8f, new Color(1f, 0.35f, 0.1f));
                        break;
                    case "statue":
                        Cube($"Statue{h.X}", new Vector3(p.x, 10, p.z), new Vector3(10, 20, 10), Colors.Ember);
                        break;
                    case "tree":
                        Cylinder($"Tree{h.X}", new Vector3(p.x, 6f, p.z), 1.6f, 12f, new Color(0.36f, 0.21f, 0.11f));
                        break;
                }
            }

            _ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _ball.name = "Ball";
            _ball.transform.SetParent(_root, false);
            _ball.transform.localScale = Vector3.one * 1.1f;
            _ball.GetComponent<Renderer>().material.color = Colors.Ball;
            Destroy(_ball.GetComponent<Collider>());
            _trail = _ball.AddComponent<TrailRenderer>();
            _trail.time = 0.45f;
            _trail.startWidth = 0.4f;
            _trail.endWidth = 0.05f;
            _trail.material = new Material(Shader.Find("Sprites/Default"));
            _trail.startColor = Color.white;
            _trail.endColor = new Color(1, 1, 1, 0);
        }

        public Transform Person(string id, string faction)
        {
            if (_people.TryGetValue(id, out var t)) return t;
            var go = new GameObject(id);
            go.transform.SetParent(_root, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0, 2.6f, 0);
            body.transform.localScale = new Vector3(2.2f, 2.4f, 2.2f);
            body.GetComponent<Renderer>().material.color = Colors.Body(faction);
            Destroy(body.GetComponent<Collider>());
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(0, 5.2f, 0);
            head.transform.localScale = Vector3.one * 2.1f;
            head.GetComponent<Renderer>().material.color = Colors.SkinTone(faction);
            Destroy(head.GetComponent<Collider>());
            var hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.transform.SetParent(go.transform, false);
            hat.transform.localPosition = new Vector3(0, 6.2f, 0);
            hat.transform.localScale = new Vector3(1.8f, 0.25f, 1.8f);
            hat.GetComponent<Renderer>().material.color = Colors.Accent(faction);
            Destroy(hat.GetComponent<Collider>());
            _people[id] = go.transform;
            return go.transform;
        }

        public void PlaceBall(Vector3 p, bool heat)
        {
            _ball.transform.position = p;
            _ball.GetComponent<Renderer>().material.color = heat ? Colors.EmberFire : Colors.Ball;
            _trail.startColor = heat ? Colors.EmberFire : Color.white;
        }

        public void HideBall(bool hide) => _ball.SetActive(!hide);

        void Bag(double x, double z) =>
            Cube("Bag", new Vector3((float)x, 0.25f, (float)z), new Vector3(2.2f, 0.4f, 2.2f), Colors.Chalk);

        void Quad(string name, Vector3 pos, Vector3 scale, Color color) =>
            Cube(name, pos, scale, color);

        void Cube(string name, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material.color = color;
            Destroy(go.GetComponent<Collider>());
        }

        void Cylinder(string name, Vector3 pos, float radius, float height, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(radius * 2, height * 0.5f, radius * 2);
            go.GetComponent<Renderer>().material.color = color;
            Destroy(go.GetComponent<Collider>());
        }
    }
}
