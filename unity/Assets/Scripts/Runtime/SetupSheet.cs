using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>The setup screens draw on the shared setup cells; pads navigate them.</summary>
    public static class SetupSheet
    {
        static GUIStyle _title, _label, _body, _small;
        static readonly Color Ink = new Color(.045f, .075f, .095f);
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
        public static void ConnectController()
        {
            var old = Begin();
            Fill(new Rect(270, 290, 740, 180), Ink);
            Text(300, 315, 680, 48, "CONNECT A CONTROLLER", _title);
            Text(300, 378, 680, 54, "Plug in or pair a controller to play. Two controllers play together.", _label);
            GUI.matrix = old;
        }
        public static void TitleMenu(int focus)
        {
            var old = Begin();
            FocusRows(40, 235, 410, focus, new[] { "Exhibition", "Tutorials", "Controls", "Quit" });
            GUI.matrix = old;
        }
        public static void FieldFocus(int focus, string park, bool night, bool hazards, bool versus, bool home)
        {
            var old = Begin();
            FocusRows(864, 270, 392, focus, new[] { "Stadium: " + park, "Time: " + (night ? "Night" : "Day"),
                "Hazards: " + (hazards ? "On" : "Off"), "Players: " + (versus ? "2 controllers" : "1 vs CPU"),
                "P1 side: " + (home ? "Home" : "Away"), "Choose captains" });
            Text(40, 688, 1130, 65, "Up/down choose • Left/right change • South confirm • East back", _label);
            GUI.matrix = old;
        }
        public static void LiveOrders(string runners, int target)
        {
            var old = Begin();
            if (runners != null)
            {
                var width = Mathf.Min(320, _small.CalcSize(new GUIContent(runners)).x + 24);
                Fill(new Rect(24, 696, width, 30), Ink);
                Text(36, 696, width - 24, 30, runners, _small);
            }
            if (target >= 0)
            {
                Fill(new Rect(700, 680, 556, 52), Ink);
                Text(712, 684, 532, 44, target == 0 ? "Right stick: choose base • RT throw" : "THROW " + (target == 4 ? "HOME" : target + "B") + " • RT", _label);
            }
            GUI.matrix = old;
        }
        static void FocusRows(float x, float y, float width, int focus, string[] rows)
        {
            for (var i = 0; i < rows.Length; i++)
            {
                var r = new Rect(x, y + i * 54, width, 48);
                Fill(r, i == focus ? new Color(.16f, .32f, .34f) : Ink);
                Text(x + 16, r.y + 2, width - 32, 44, (i == focus ? "> " : "") + rows[i], _label);
            }
        }
        public static void FieldControls(bool night, bool hazards)
        {
            var old = Begin();
            Text(40, 8, 650, 22, CarnivalFront.SetupStadiumStep, _small);
            Button(ExhibitionSetupLayout.Night, CarnivalFront.SetupNightLabel(night), night);
            Button(ExhibitionSetupLayout.Hazards, CarnivalFront.SetupHazardsLabel(hazards), hazards);
            Button(ExhibitionSetupLayout.PreviousPark, CarnivalFront.SetupPreviousStadium, false);
            Button(ExhibitionSetupLayout.NextPark, CarnivalFront.SetupNextStadium, false);
            Button(ExhibitionSetupLayout.Back, CarnivalFront.SetupBackTitle, false);
            Button(ExhibitionSetupLayout.Next, CarnivalFront.SetupPickCaptains, true);
            Text(250, 718, 710, 48, CarnivalFront.SetupStadiumHelp, _body);
            GUI.matrix = old;
        }
        public static void Settings(ExhibitionSettings settings, LineupScreens lineup, Match match)
        {
            var old = Begin();
            Fill(new Rect(0, 0, 1280, 800), Ink);
            Text(40, 28, 900, 24, CarnivalFront.SetupSettingsStep, _small);
            Text(40, 62, 1100, 48, CarnivalFront.SetupSettingsTitle, _title);
            Text(40, 119, 1100, 24, CarnivalFront.SetupRulesOwner, _body);
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
            Text(850, 226, 370, 25, match.Night ? CarnivalFront.SetupNight : CarnivalFront.SetupDay, _small);
            Text(850, 270, 370, 32, CarnivalFront.SetupAwayName(lineup.AwayCaptain.Name), _body);
            Text(850, 300, 370, 32, lineup.HomeCaptain.Name, _label);
            ReadySeat(850, 380, LineupSeat.Pad1, lineup);
            if (lineup.HomeSeat == LineupSeat.Pad2 || lineup.AwaySeat == LineupSeat.Pad2)
                ReadySeat(850, 455, LineupSeat.Pad2, lineup);
            else Text(850, 455, 370, 32, CarnivalFront.SetupCpuReady, _body);
            Text(850, 563, 370, 48, CarnivalFront.SetupRulesChanged, _small);
            Button(ExhibitionSetupLayout.Back, lineup.IsReady(LineupSeat.Pad1) ? CarnivalFront.SetupEditSettings : CarnivalFront.SetupBackPositions, false);
            Button(ExhibitionSetupLayout.Next, lineup.IsReady(LineupSeat.Pad1) ? CarnivalFront.SetupWaiting : CarnivalFront.SetupPlayBall, true);
            Text(242, 716, 734, 50, CarnivalFront.SetupRulesHelp, _body);
            GUI.matrix = old;
        }
        static void ReadySeat(float x, float y, LineupSeat seat, LineupScreens lineup)
        {
            var ready = lineup.IsReady(seat);
            Text(x, y, 370, 30, CarnivalFront.SetupReadySeat(seat, ready), _label);
            Text(x, y + 31, 370, 24, CarnivalFront.SetupReadyHelp, _small);
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
