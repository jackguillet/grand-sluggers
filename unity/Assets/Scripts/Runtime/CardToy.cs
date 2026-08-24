using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// World-space captain card so Camera.Render stills show P/B/F/R.
    /// OnGUI is skipped by the still-gate.
    /// </summary>
    public sealed class CardToy : MonoBehaviour
    {
        Transform[] _bars;
        TextMesh _name;
        TextMesh _verbs;

        public static CardToy Attach(Transform parent)
        {
            var go = new GameObject("CaptainCard");
            go.transform.SetParent(parent, false);
            return go.AddComponent<CardToy>();
        }

        public void Show(CharacterCard card, Vector3 at, Vector3 face)
        {
            if (_bars == null) Build();
            transform.position = at;
            // Select/title cameras sit in -Z and look +Z. Face the camera, not the park.
            var yaw = face.z <= 0f ? 180f : 0f;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            gameObject.SetActive(true);
            if (_name != null) _name.text = card.Name.ToUpperInvariant();
            if (_verbs != null) _verbs.text = card.StarPitch + "   " + card.StarSwing;
            SetBar(0, card.Stats.Pitch);
            SetBar(1, card.Stats.Bat);
            SetBar(2, card.Stats.Field);
            SetBar(3, card.Stats.Run);
        }

        public void Hide()
        {
            if (gameObject != null) gameObject.SetActive(false);
        }

        void Build()
        {
            var ink = Look.Toon(new Color(0.07f, 0.05f, 0.1f));
            Look.Prim(PrimitiveType.Cube, "Panel", transform, Vector3.zero, new Vector3(3.6f, 2.5f, 0.12f), ink);
            _name = Label(transform, "Name", 0.09f, 48, Colors.Gold, new Vector3(0f, 0.92f, 0.08f));
            _verbs = Label(transform, "Verbs", 0.05f, 36, Color.white, new Vector3(0f, -0.92f, 0.08f));
            var labels = new[] { "P", "B", "F", "R" };
            _bars = new Transform[4];
            for (var i = 0; i < 4; i++)
            {
                var y = 0.48f - i * 0.32f;
                Label(transform, labels[i], 0.07f, 42, Colors.Gold, new Vector3(-1.45f, y, 0.08f));
                var row = Look.Prim(PrimitiveType.Cube, "Bar" + labels[i], transform,
                    new Vector3(-0.2f, y, 0.08f), new Vector3(2.4f, 0.16f, 0.08f), Look.Toon(Colors.Gold));
                _bars[i] = row.transform;
            }
        }

        void SetBar(int i, int n)
        {
            if (_bars == null || i < 0 || i >= _bars.Length || _bars[i] == null) return;
            var fill = (float)CharacterCard.BarFill(n);
            _bars[i].localScale = new Vector3(0.2f + 2.2f * fill, 0.16f, 0.08f);
            _bars[i].localPosition = new Vector3(-1.2f + (0.2f + 2.2f * fill) * 0.5f, _bars[i].localPosition.y, 0.08f);
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
