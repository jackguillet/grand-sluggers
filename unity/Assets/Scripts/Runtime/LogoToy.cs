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
            var yaw = face.z <= 0f ? 180f : 0f;
            transform.rotation = Quaternion.Euler(0f, yaw, 8f);
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
            Look.Prim(PrimitiveType.Cube, "Board", transform, Vector3.zero, new Vector3(11.6f, 2.05f, 0.16f), gold);
            Look.Prim(PrimitiveType.Cube, "Ink", transform, new Vector3(0f, 0f, 0.06f), new Vector3(11.1f, 1.62f, 0.08f), ink);
            Look.Prim(PrimitiveType.Cube, "Star", transform, new Vector3(-5.35f, 0.05f, 0.12f), new Vector3(0.55f, 0.55f, 0.12f), gold);
            _copy = Label(transform, "Copy", 0.14f, 64, Colors.Gold, new Vector3(0.35f, 0.02f, 0.14f));
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
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            return mesh;
        }
    }
}
