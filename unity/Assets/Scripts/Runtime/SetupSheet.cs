using System.Linq;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
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
                _pin = Style(14, Color.white); _pin.alignment = TextAnchor.MiddleCenter;
            }
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 800f, 1));
            return old;
        }
        public static void ConnectController()
        {
            var old = Begin();
            Fill(new Rect(270, 290, 740, 180), Ink);
            Text(300, 315, 680, 48, CarnivalFront.ConnectTitle, _title);
            Text(300, 378, 680, 54, CarnivalFront.ConnectBody, _label);
            GUI.matrix = old;
        }
        public static void TitleMenu(int focus)
        {
            var old = Begin();
            FocusRows(40, 235, 410, focus, CarnivalFront.TitleMenu.ToArray());
            GUI.matrix = old;
        }
        public static void FieldFocus(int focus, string park, bool night, bool hazards, bool versus, bool home)
        {
            var old = Begin();
            FocusRows(864, 270, 392, focus, CarnivalFront.StadiumRows(park, night, hazards, versus, home));
            Text(40, 688, 1130, 65, CarnivalFront.StadiumHelp, _label);
            GUI.matrix = old;
        }
        static Texture2D _land, _disc;
        static int _landKey;

        /// <summary>
        /// The continent map (WD-17 A, WD-18 C): the painted map when its slot is placed, else a greybox generated from the
        /// regions — land around the mainland's regions, the island on its own — with a pin per park in its home team's color,
        /// the cursor's pin ringed in gold, and the park under the cursor on the card beside it.
        /// </summary>
        public static void Map(ContentCatalog content, string cursor, bool night, bool hazards)
        {
            var old = Begin();
            Fill(new Rect(0, 0, 1280, 800), Ink);
            var panel = Box(CarnivalFront.MapPanel);
            GUI.DrawTexture(panel, MapTexture(content.World, content.Art.Map), ScaleMode.StretchToFill);
            Text(panel.x + 16, panel.y + 8, 500, 36, CarnivalFront.MapTitle, _label);
            foreach (var region in content.World.Regions)
            {
                var park = content.World.ParkIn(region.Id);
                if (park == null) continue;
                var (x, y) = CarnivalFront.MapPin(region);
                var here = park == cursor;
                var size = here ? CarnivalFront.MapCursorSize - 8 : CarnivalFront.MapPinSize;
                if (here) Disc(x, y, CarnivalFront.MapCursorSize, Colors.Gold);
                Disc(x, y, size, Colors.Body(content.MustPark(park).Faction));
                var label = CarnivalFront.MapLabel(region);
                Fill(new Rect(label.X, label.Y, label.W, label.H), new Color(.045f, .075f, .095f, here ? .9f : .6f));
                GUI.Label(new Rect(label.X, label.Y, label.W, label.H), content.MustPark(park).Name, _pin);
            }
            var card = Box(CarnivalFront.MapCardPanel);
            Fill(card, new Color(.075f, .115f, .14f));
            var lines = CarnivalFront.MapCard(content, cursor, night, hazards);
            var y0 = card.y + 18;
            for (var i = 0; i < lines.Count; i++)
            {
                var style = i == 0 ? _title : i < 3 ? _label : _body;
                var h = i == 0 ? 44 : i < 3 ? 32 : 48;
                Text(card.x + 20, y0, card.width - 40, h, lines[i], style);
                y0 += h + 6;
            }
            Text(40, 688, 1200, 40, CarnivalFront.MapHelp, _label);
            GUI.matrix = old;
        }

        static GUIStyle _pin;

        static void Disc(float x, float y, float size, Color color)
        {
            if (_disc == null)
            {
                const int n = 64;
                _disc = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color[n * n];
                for (var j = 0; j < n; j++)
                for (var i = 0; i < n; i++)
                {
                    var d = Vector2.Distance(new Vector2(i + .5f, j + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    px[j * n + i] = new Color(1, 1, 1, Mathf.Clamp01((1f - d) * 12f));
                }
                _disc.SetPixels(px); _disc.Apply();
            }
            var oldColor = GUI.color;
            GUI.color = QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
            GUI.DrawTexture(new Rect(x - size / 2, y - size / 2, size, size), _disc);
            GUI.color = oldColor;
        }

        /// <summary>The placed map art, or the greybox continent made once from the regions.</summary>
        static Texture2D MapTexture(WorldMap world, MapArtSlot art)
        {
            if (art.Placed && Resources.Load<Texture2D>(art.Slot.Substring("Resources/".Length)) is { } painted) return painted;
            var key = world.Regions.Count;
            if (_land != null && _landKey == key) return _land;
            const int w = 328, h = 240;
            var sea = new Color(.16f, .38f, .56f);
            var shallow = new Color(.28f, .56f, .70f);
            var sand = new Color(.86f, .78f, .56f);
            var grass = new Color(.38f, .62f, .34f);
            _land = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[w * h];
            var inset = CarnivalFront.MapInset / CarnivalFront.MapPanel.W;
            var insetY = CarnivalFront.MapInset / CarnivalFront.MapPanel.H;
            for (var j = 0; j < h; j++)
            for (var i = 0; i < w; i++)
            {
                var u = (i + .5f) / w;
                var v = 1f - (j + .5f) / h; // texture rows run bottom-up; the map's y runs north to south
                float main = 0, isle = 0;
                foreach (var r in world.Regions)
                {
                    var rx = inset + (float)r.X * (1 - 2 * inset);
                    var ry = insetY + (float)r.Y * (1 - 2 * insetY);
                    var dx = (u - rx) * 1.37f; // the panel is wider than tall: keep the blobs round
                    var dy = v - ry;
                    var f = Mathf.Exp(-(dx * dx + dy * dy) / (r.Island ? 0.004f : 0.018f));
                    if (r.Island) isle += f; else main += f;
                }
                var land = Mathf.Max(main, isle * 1.2f);
                px[j * w + i] = land > .55f ? grass : land > .42f ? sand : land > .28f ? shallow : sea;
            }
            _land.SetPixels(px); _land.Apply();
            _landKey = key;
            return _land;
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
                Text(712, 684, 532, 44, BroadcastHud.SetupThrow(target), _label);
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
        static Rect Box(CaptainPanelRect r) => new Rect(r.X, r.Y, r.W, r.H);
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
