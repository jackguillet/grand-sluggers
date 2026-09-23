using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>Pointer and controller views share the same setup cells.</summary>
    public static class SetupSheet
    {
        public enum Action { None, Change, Next, Back, PreviousPark, NextPark, Night, Hazards }
        static GUIStyle _title, _label, _body, _small;
        static readonly Color Ink = new Color(.045f, .075f, .095f);
        public static Action Pointer(bool settings, out int row)
        {
            row = -1;
            if (!Controls.PointerDown) return Action.None;
            var p = Controls.GuiMouse;
            var x = p.x / Screen.width; var y = 1 - p.y / Screen.height;
            if (Hit(ExhibitionSetupLayout.Back, x, y)) return Action.Back;
            if (Hit(ExhibitionSetupLayout.Next, x, y)) return Action.Next;
            if (settings)
            {
                for (var i = 0; i < ExhibitionSettings.RowCount; i++)
                    if (Hit(ExhibitionSetupLayout.SettingsRow(i), x, y)) { row = i; return Action.Change; }
            }
            else
            {
                if (Hit(ExhibitionSetupLayout.PreviousPark, x, y)) return Action.PreviousPark;
                if (Hit(ExhibitionSetupLayout.NextPark, x, y)) return Action.NextPark;
                if (Hit(ExhibitionSetupLayout.Night, x, y)) return Action.Night;
                if (Hit(ExhibitionSetupLayout.Hazards, x, y)) return Action.Hazards;
            }
            return Action.None;
        }
        static bool Hit(LineupCell c, float x, float y) => ExhibitionSetupLayout.Contains(c, x, y);
        static Matrix4x4 Begin()
        {
            if (_title == null)
            {
                _title = Style(34, Color.white); _label = Style(22, Color.white);
                _body = Style(16, new Color(.82f, .88f, .88f));
                _small = Style(13, new Color(.62f, .73f, .76f));
            }
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 800f, 1));
            return old;
        }
        public static void FieldControls(bool night, bool hazards)
        {
            var old = Begin();
            var keys = Controls.SeatUsesKeyboard(0);
            Text(40, 8, 650, 22, "01 / STADIUM", _small);
            Button(ExhibitionSetupLayout.Night, "Night mode   " + (night ? "ON" : "OFF") + (keys ? "    N" : "    R3"), night);
            Button(ExhibitionSetupLayout.Hazards, "Hazards   " + (hazards ? "ON" : "OFF") + (keys ? "    R" : "    Select"), hazards);
            Button(ExhibitionSetupLayout.PreviousPark, "← Previous stadium", false);
            Button(ExhibitionSetupLayout.NextPark, "Next stadium →", false);
            Button(ExhibitionSetupLayout.Back, "Back to title", false);
            Button(ExhibitionSetupLayout.Next, "Pick captains →", true);
            Text(250, 718, 710, 48, keys ? "A/D  Stadium    Space  Captains    F  Back" : "Stick  Stadium    South  Captains    West  Back", _body);
            GUI.matrix = old;
        }
        public static void Settings(ExhibitionSettings settings, LineupScreens lineup, Match match)
        {
            var old = Begin();
            Fill(new Rect(0, 0, 1280, 800), Ink);
            Text(40, 28, 900, 24, "05 / MATCH SETTINGS", _small);
            Text(40, 62, 1100, 48, "Set the rules. Play ball.", _title);
            Text(40, 119, 1100, 24, "Player 1 adjusts settings. Both players ready up to start.", _body);
            for (var i = 0; i < ExhibitionSettings.RowCount; i++)
            {
                var r = RectOf(ExhibitionSetupLayout.SettingsRow(i));
                Fill(r, i == settings.Selected ? new Color(.13f, .23f, .25f) : new Color(.075f, .115f, .14f));
                Text(r.x + 18, r.y + 9, 455, 30, ExhibitionSettings.Labels[i], _label);
                Text(r.x + 18, r.y + 45, r.width - 36, 27, settings.Description(i, match.Rules), _small);
                Text(r.x + 552, r.y + 9, 185, 32, settings.Value(i), _label);
            }
            Fill(new Rect(830, 166, 410, 468), new Color(.075f, .115f, .14f));
            Text(850, 187, 370, 30, match.Park.Name, _label);
            Text(850, 226, 370, 25, match.Night ? "NIGHT GAME" : "DAY GAME", _small);
            Text(850, 270, 370, 32, lineup.AwayCaptain.Name + " at", _body);
            Text(850, 300, 370, 32, lineup.HomeCaptain.Name, _label);
            ReadySeat(850, 380, LineupSeat.Pad1, lineup);
            if (lineup.HomeSeat == LineupSeat.Pad2 || lineup.AwaySeat == LineupSeat.Pad2)
                ReadySeat(850, 455, LineupSeat.Pad2, lineup);
            else Text(850, 455, 370, 32, "CPU · READY", _body);
            Text(850, 563, 370, 48, "Changing a setting clears both ready states.", _small);
            var keys = Controls.SeatUsesKeyboard(0);
            Button(ExhibitionSetupLayout.Back, lineup.IsReady(LineupSeat.Pad1) ? "Edit settings" : "Positions / order", false);
            Button(ExhibitionSetupLayout.Next, lineup.IsReady(LineupSeat.Pad1) ? "P1 ready · waiting" : (keys ? "Q  Ready / play ball" : "North  Ready / play ball"), true);
            Text(242, 716, 734, 50, keys ? "W/S  Select     A/D or Space  Change\nF  Back     Esc  How to play" : "Stick  Select / change     South  Change\nWest  Back     Esc  How to play", _body);
            GUI.matrix = old;
        }
        static void ReadySeat(float x, float y, LineupSeat seat, LineupScreens lineup)
        {
            var ready = lineup.IsReady(seat);
            Text(x, y, 370, 30, (seat == LineupSeat.Pad1 ? "P1" : "P2") + (ready ? " · READY" : " · NOT READY"), _label);
            var keys = Controls.SeatUsesKeyboard(seat == LineupSeat.Pad1 ? 0 : 1);
            Text(x, y + 31, 370, 24, keys ? "Q  Ready    F  Edit" : "North  Ready    West  Edit", _small);
        }
        static void Button(LineupCell c, string text, bool primary)
        {
            var r = RectOf(c); Fill(r, primary ? new Color(.20f, .32f, .30f) : new Color(.10f, .16f, .19f));
            Text(r.x + 14, r.y, r.width - 28, r.height, text, _body);
        }
        static Rect RectOf(LineupCell c) => new Rect((float)c.X * 1280, (float)(1 - c.Y - c.H) * 800, (float)c.W * 1280, (float)c.H * 800);
        static void Text(float x, float y, float w, float h, string value, GUIStyle style) => GUI.Label(new Rect(x, y, w, h), value, style);
        static void Fill(Rect r, Color color) { var old = GUI.color; GUI.color = QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        static GUIStyle Style(int size, Color color)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            style.normal.textColor = color; return style;
        }
    }
}
