using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// World-space title sticker so Camera.Render stills show GRAND SLUGGERS.
    /// OnGUI is skipped by the still-gate.
    /// </summary>
    public sealed class LogoToy : MonoBehaviour
    {
        TextMesh _copy;

        public static LogoToy Attach(Transform parent)
        {
            var go = new GameObject("TitleSticker");
            go.transform.SetParent(parent, false);
            return go.AddComponent<LogoToy>();
        }

        public void Show(string copy, Vector3 at, Vector3 face)
        {
            if (_copy == null) Build();
            transform.position = at;
            var fwd = face.sqrMagnitude > 0.01f ? face.normalized : Vector3.back;
            transform.rotation = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(8f, 0f, 0f);
            gameObject.SetActive(true);
            if (_copy != null) _copy.text = copy;
        }

        public void Hide()
        {
            if (gameObject != null) gameObject.SetActive(false);
        }

        void Build()
        {
            var gold = Look.Toon(Colors.Gold);
            var ink = Look.Toon(new Color(0.12f, 0.08f, 0.04f));
            Look.Prim(PrimitiveType.Cube, "Board", transform, Vector3.zero, new Vector3(7.6f, 1.72f, 0.16f), gold);
            Look.Prim(PrimitiveType.Cube, "Ink", transform, new Vector3(0f, 0f, 0.06f), new Vector3(7.2f, 1.32f, 0.08f), ink);
            Look.Prim(PrimitiveType.Cube, "Star", transform, new Vector3(-3.45f, 0.05f, 0.12f), new Vector3(0.48f, 0.48f, 0.12f), gold);
            _copy = Label(transform, "Copy", 0.10f, 64, Colors.Gold, new Vector3(0.28f, 0.02f, 0.14f));
        }

        static TextMesh Label(Transform parent, string name, float size, int font, Color color, Vector3 local)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var mesh = go.AddComponent<TextMesh>();
            mesh.fontSize = font;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            mesh.fontStyle = FontStyle.Bold;
            return mesh;
        }
    }
}
