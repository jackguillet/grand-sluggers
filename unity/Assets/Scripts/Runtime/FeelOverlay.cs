using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>Debug feel HUD. Off by default. Does not replace the scorebug.</summary>
    public static class FeelOverlay
    {
        static GUIStyle _label;
        static Texture2D _panel;

        public static void Draw(
            string shot, string verb, float charge, float hang, float rest,
            int bag, float slow, bool freezeCam, string currentEvent = "")
        {
            Ensure();
            var w = 420f;
            var x = 16f;
            var y = Screen.height - 156f;
            GUI.DrawTexture(new Rect(x, y, w, 140), _panel);
            GUI.Label(new Rect(x + 12, y + 8, w - 24, 22),
                "SHOT  " + (string.IsNullOrEmpty(shot) ? "-" : shot.ToUpperInvariant())
                + "    VERB  " + (string.IsNullOrEmpty(verb) ? "-" : verb.ToUpperInvariant()),
                _label);
            GUI.Label(new Rect(x + 12, y + 34, w - 24, 22),
                "CHARGE  " + charge.ToString("0.00")
                + "    HANG  " + hang.ToString("0.00")
                + "    REST  " + rest.ToString("0.00"),
                _label);
            GUI.Label(new Rect(x + 12, y + 60, w - 24, 22),
                "BAG  " + (bag > 0 ? bag.ToString() : "-")
                + "    TIME  " + slow.ToString("0.00")
                + (freezeCam ? "    CAM FREEZE" : ""),
                _label);
            GUI.Label(new Rect(x + 12, y + 86, w - 24, 22),
                "EVENT  " + (string.IsNullOrEmpty(currentEvent) ? "-" : currentEvent.ToUpperInvariant()),
                _label);
            GUI.Label(new Rect(x + 12, y + 110, w - 24, 22),
                "F2 overlay   [ slow   ] cam freeze",
                _label);
        }

        static void Ensure()
        {
            if (_label != null) return;
            _panel = new Texture2D(1, 1);
            _panel.SetPixel(0, 0, new Color(0.04f, 0.05f, 0.07f, 0.78f));
            _panel.Apply();
            _label = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _label.normal.textColor = new Color(0.95f, 0.88f, 0.45f);
        }
    }
}
