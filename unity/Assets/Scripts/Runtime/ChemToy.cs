using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// World-space hearts / scribbles so Camera.Render stills show chemistry.
    /// OnGUI edges were white rays across the dirt.
    /// </summary>
    public sealed class ChemToy : MonoBehaviour
    {
        readonly List<Sticker> _pool = new List<Sticker>();

        public static ChemToy Attach(Transform parent)
        {
            var go = new GameObject("ChemStickers");
            go.transform.SetParent(parent, false);
            return go.AddComponent<ChemToy>();
        }

        public void Show(IReadOnlyList<(Vector3 At, string Kind)> edges)
        {
            gameObject.SetActive(true);
            while (_pool.Count < edges.Count)
                _pool.Add(Build(_pool.Count));
            for (var i = 0; i < _pool.Count; i++)
            {
                if (i >= edges.Count)
                {
                    _pool[i].Root.SetActive(false);
                    continue;
                }
                var e = edges[i];
                var s = _pool[i];
                s.Root.SetActive(true);
                s.Root.transform.position = e.At;
                s.Root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                var heart = e.Kind == ChemistryToy.Heart;
                s.Heart.SetActive(heart);
                s.Scribble.SetActive(!heart);
            }
        }

        public void Hide()
        {
            if (gameObject != null) gameObject.SetActive(false);
        }

        Sticker Build(int i)
        {
            var root = new GameObject("Sticker" + i);
            root.transform.SetParent(transform, false);
            var heart = new GameObject("Heart");
            heart.transform.SetParent(root.transform, false);
            var red = Look.Toon(new Color(0.86f, 0.18f, 0.28f));
            var gold = Look.Toon(Colors.Gold);
            Look.Prim(PrimitiveType.Sphere, "LobeL", heart.transform, new Vector3(-0.22f, 0.16f, 0f), new Vector3(0.42f, 0.42f, 0.28f), red);
            Look.Prim(PrimitiveType.Sphere, "LobeR", heart.transform, new Vector3(0.22f, 0.16f, 0f), new Vector3(0.42f, 0.42f, 0.28f), red);
            var point = Look.Prim(PrimitiveType.Cube, "Point", heart.transform, new Vector3(0f, -0.14f, 0f), new Vector3(0.46f, 0.46f, 0.18f), red);
            point.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Look.Prim(PrimitiveType.Cube, "Pip", heart.transform, new Vector3(0f, 0.02f, 0.14f), new Vector3(0.16f, 0.16f, 0.08f), gold);

            var scribble = new GameObject("Scribble");
            scribble.transform.SetParent(root.transform, false);
            var a = Look.Prim(PrimitiveType.Cube, "A", scribble.transform, Vector3.zero, new Vector3(0.92f, 0.12f, 0.12f), red);
            a.transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
            var b = Look.Prim(PrimitiveType.Cube, "B", scribble.transform, Vector3.zero, new Vector3(0.92f, 0.12f, 0.12f), red);
            b.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
            return new Sticker { Root = root, Heart = heart, Scribble = scribble };
        }

        sealed class Sticker
        {
            public GameObject Root;
            public GameObject Heart;
            public GameObject Scribble;
        }
    }
}
