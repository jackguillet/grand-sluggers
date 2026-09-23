using System;
using GrandSluggers.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GrandSluggers.UnityClient
{
    /// <summary>The lineup board uses one layout for rendering, pointer targets and pad navigation.</summary>
    public static class TeamSheet
    {
        public enum Action { None, Player, Continue, Back, Fill }
        static GUIStyle _title, _heading, _body, _small, _name, _mark;
        static Texture2D _white, _field;
        static LineupScreens _shown;
        static Vector2 _mouse;
        static bool _hover, _pointerMode;
        static LineupFocus _hoverFocus;
        static int _hoverIndex;
        static readonly Color Ink = new Color(.045f, .075f, .095f);
        static readonly Color Muted = new Color(.56f, .66f, .69f);
        static readonly Color Gold = new Color(1f, .83f, .40f);

        public static void UseController(LineupSeat seat) { if (seat == LineupSeat.Pad1) { _hover = false; _pointerMode = false; } }

        public static Action Pointer(LineupScreens lineup, out LineupFocus focus, out int index)
        {
            focus = default;
            index = -1;
            var mouse = Mouse.current;
            if (mouse == null) { _hover = false; return Action.None; }
            var p = mouse.position.ReadValue();
            if (_shown != lineup) { _shown = lineup; _mouse = p; _hover = false; }
            var moved = (p - _mouse).sqrMagnitude > .01f;
            _mouse = p;
            var clicked = Controls.PointerDown;
            if (moved || clicked) _pointerMode = true;
            if (!moved && !clicked && !_hover) return Action.None;
            var x = p.x / Screen.width;
            var y = p.y / Screen.height;
            _hover = false;
            foreach (LineupFocus target in Enum.GetValues(typeof(LineupFocus)))
            {
                var teamTarget = target is LineupFocus.Pool or LineupFocus.HomeRow or LineupFocus.AwayRow;
                if (teamTarget != (lineup.Step == LineupStep.TeamSetup)) continue;
                var count = target == LineupFocus.Pool ? lineup.Pool.Count : LineupScreens.Size;
                for (var i = 0; i < count; i++)
                {
                    if (!Contains(Cell(target, i, count), x, y)) continue;
                    _hover = true; _hoverFocus = focus = target; _hoverIndex = index = i;
                    return clicked ? Action.Player : Action.None;
                }
            }
            if (!clicked) return Action.None;
            if (Contains(LineupLayout.ContinueButton, x, y)) return Action.Continue;
            if (Contains(LineupLayout.BackButton, x, y)) return Action.Back;
            if (lineup.Step == LineupStep.TeamSetup && Contains(LineupLayout.FillButton, x, y)) return Action.Fill;
            return Action.None;
        }

        static bool Contains(LineupCell c, float x, float y) => x >= c.X && x <= c.X + c.W && y >= c.Y && y <= c.Y + c.H;
        static LineupCell Cell(LineupFocus f, int i, int count) => f switch
        {
            LineupFocus.HomeRow => LineupLayout.HomeSlot(i), LineupFocus.AwayRow => LineupLayout.AwaySlot(i),
            LineupFocus.Pool => LineupLayout.PoolCell(i, count),
            LineupFocus.HomeOrder => LineupLayout.HomeOrder(i), LineupFocus.AwayOrder => LineupLayout.AwayOrder(i),
            _ => LineupLayout.DiamondHead(f == LineupFocus.HomeDiamond, Diamond.Order[i])
        };

        public static void Draw(Match match, LineupScreens lineup)
        {
            Ensure();
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 800f, 1));
            Fill(new Rect(0, 0, 1280, 800), Ink);
            var team = lineup.Step == LineupStep.TeamSetup;
            Label(24, 24, 860, 22, team ? "03  /  CHOOSE YOUR TEAM" : "04  /  SET YOUR LINEUP", _small);
            Label(24, 54, 1000, 46, team ? "Build your nine" : "Batting order & field positions", _title);
            Label(24, 108, 880, 26, lineup.Help, _body);
            Label(936, 32, 320, 24, match == null ? "EXHIBITION" : match.Park.Name.ToUpperInvariant(), _small);
            Label(936, 60, 320, 26, "MATCH SETTINGS FOLLOW LINEUP", _small);
            var p1 = Inspection(lineup, LineupSeat.Pad1);
            var p2 = Inspection(lineup, LineupSeat.Pad2);
            if (team)
            {
                TeamLabel(lineup, true, 137);
                DrawCells(lineup, LineupFocus.HomeRow, 9, p1, p2);
                DrawCells(lineup, LineupFocus.Pool, lineup.Pool.Count, p1, p2);
                TeamLabel(lineup, false, 598);
                DrawCells(lineup, LineupFocus.AwayRow, 9, p1, p2);
            }
            else
            {
                TeamLabel(lineup, true, 137);
                TeamLabel(lineup, false, 598);
                Side(lineup, true, p1, p2);
                Side(lineup, false, p1, p2);
            }
            PlayerCard(lineup, true, lineup.HomeSeat == LineupSeat.Pad2 ? p2 : lineup.HomeSeat == LineupSeat.Pad1 ? p1 : lineup.HomeCaptain);
            PlayerCard(lineup, false, lineup.AwaySeat == LineupSeat.Pad2 ? p2 : lineup.AwaySeat == LineupSeat.Pad1 ? p1 : lineup.AwayCaptain);
            var keys = Controls.SeatUsesKeyboard(0);
            Button(LineupLayout.BackButton, team ? "Back to captains" : lineup.IsReady(LineupSeat.Pad1) ? (keys ? "F  Edit lineup" : "West  Edit lineup") : lineup.HasPick(LineupSeat.Pad1) ? (keys ? "F  Cancel pick" : "West  Cancel pick") : (keys ? "F  Back" : "West  Back"), false);
            if (team)
            {
                Button(LineupLayout.FillButton, keys ? "Tab  Fill team" : "RB  Fill team", false);
                Label(390, 716, 605, 46, keys ? "WASD  Move · Space  Add / continue\nF  Remove · G  Captains · Esc  How to play" : "Stick  Move · South  Add / continue\nWest  Remove · East  Captains · Esc  How to play", _body);
            }
            else Label(206, 720, 785, 42, keys ? "WASD  Move     Space / click  Pick & swap\nG  Order / field     Esc  How to play" : "Stick  Move     South  Pick & swap\nEast  Order / field     Esc  How to play", _body);
            Button(LineupLayout.ContinueButton, team ? "Continue  →" : lineup.IsReady(LineupSeat.Pad1) ? "P1 Ready · waiting for P2" : (keys ? "Q  Next: settings  →" : "North  Next: settings  →"), team ? lineup.Ready : !lineup.HasPick(LineupSeat.Pad1));
            GUI.matrix = old;
        }

        static string SeatName(LineupSeat seat) => seat == LineupSeat.Cpu ? "CPU" : seat == LineupSeat.Pad1 ? "P1" : "P2";
        static Character Inspection(LineupScreens lineup, LineupSeat seat) => seat == LineupSeat.Pad1 && _pointerMode
            ? (_hover ? lineup.CharacterAt(_hoverFocus, _hoverIndex) : null) : lineup.InspectedBy(seat);
        static void TeamLabel(LineupScreens lineup, bool home, float y)
        {
            var seat = home ? lineup.HomeSeat : lineup.AwaySeat;
            var captain = home ? lineup.HomeCaptain : lineup.AwayCaptain;
            var ready = lineup.Step == LineupStep.DefenseSetup && lineup.IsReady(seat);
            Label(24, y, 880, 24, (home ? "HOME" : "AWAY") + "  /  " + captain.Name.ToUpperInvariant()
                + "  ·  " + SeatName(seat) + (ready ? "  ·  READY" : ""), _heading);
        }
        static void Side(LineupScreens lineup, bool home, Character p1, Character p2)
        {
            var panel = RectOf(home ? LineupLayout.HomeDiamondPanel : LineupLayout.AwayDiamondPanel);
            var seat = home ? lineup.HomeSeat : lineup.AwaySeat;
            Label(panel.x, 250, panel.width, 23, (home ? "HOME FIELD" : "AWAY FIELD") + "  ·  " + SeatName(seat), _small);
            GUI.DrawTexture(panel, _field);
            DrawCells(lineup, home ? LineupFocus.HomeOrder : LineupFocus.AwayOrder, 9, p1, p2);
            DrawCells(lineup, home ? LineupFocus.HomeDiamond : LineupFocus.AwayDiamond, 9, p1, p2);
        }

        static void DrawCells(LineupScreens lineup, LineupFocus focus, int count, Character p1, Character p2)
        {
            for (var i = 0; i < count; i++)
            {
                var c = Cell(focus, i, count);
                var who = lineup.CharacterAt(focus, i);
                var one = _pointerMode ? _hover && focus == _hoverFocus && i == _hoverIndex
                    : lineup.FocusOf(LineupSeat.Pad1) == focus && lineup.IndexOf(LineupSeat.Pad1) == i;
                var two = (lineup.HomeSeat == LineupSeat.Pad2 || lineup.AwaySeat == LineupSeat.Pad2)
                    && lineup.FocusOf(LineupSeat.Pad2) == focus && lineup.IndexOf(LineupSeat.Pad2) == i;
                var on = one || two;
                var picked = lineup.Picked(focus, i);
                var home = focus is LineupFocus.HomeRow or LineupFocus.HomeOrder or LineupFocus.HomeDiamond;
                var seat = home ? lineup.HomeSeat : lineup.AwaySeat;
                var buddy = focus == LineupFocus.Pool
                    ? lineup.Buddies(p1, who) || lineup.Buddies(p2, who)
                    : lineup.Buddies(seat == LineupSeat.Pad1 ? p1 : seat == LineupSeat.Pad2 ? p2 : null, who);
                if (_pointerMode && _hover && focus != LineupFocus.Pool)
                {
                    var hoverHome = _hoverFocus is LineupFocus.HomeRow or LineupFocus.HomeOrder or LineupFocus.HomeDiamond;
                    if (_hoverFocus != LineupFocus.Pool && hoverHome == home) buddy |= lineup.Buddies(p1, who);
                }
                var r = RectOf(c);
                var order = focus is LineupFocus.HomeOrder or LineupFocus.AwayOrder;
                var field = focus is LineupFocus.HomeDiamond or LineupFocus.AwayDiamond;
                // Only interaction and a currently inspected relationship add color; portraits have no faction frame.
                if (order || who == null) Fill(r, new Color(1, 1, 1, .035f));
                if (buddy) Fill(r, new Color(.76f, .96f, .88f, .13f));
                if (on) Fill(r, new Color(1, 1, 1, .10f));
                if (on || picked) Border(r, picked ? Gold : new Color(1, 1, 1, .65f), picked ? 2 : 1);
                var mark = order ? (i + 1).ToString("00") : field ? Diamond.Order[i] : LineupLayout.TeamMark(who);
                Label(r.x, r.y - 3, r.width, 18, mark, _mark);
                Portrait(who, new Rect(r.x + 6, r.y + 13, r.width - 12, r.height - 31));
                Label(r.x - 7, r.yMax - 18, r.width + 14, 20, who?.Name ?? "+", _mark);
                if (one || two)
                {
                    var badge = new Rect(r.xMax - 25, r.y + 14, 25, two && one ? 28 : 15);
                    Fill(badge, Ink);
                    Label(badge.x, badge.y, badge.width, badge.height, one && two ? "P1\nP2" : one ? "P1" : "P2", _mark);
                }
            }
        }

        static void PlayerCard(LineupScreens lineup, bool home, Character who)
        {
            var r = RectOf(LineupLayout.CardPanel(home));
            var seat = home ? lineup.HomeSeat : lineup.AwaySeat;
            Fill(r, new Color(.075f, .115f, .14f));
            var ready = lineup.Step == LineupStep.DefenseSetup && lineup.IsReady(seat);
            Label(r.x + 16, r.y + 10, r.width - 32, 22, SeatName(seat) + "  /  " + (ready ? "READY" : "PLAYER CARD"), _small);
            if (who == null)
            {
                Label(r.x + 16, r.y + 60, r.width - 32, 70, "Hover over a player or move to them\nto see their card and chemistry.", _body);
                return;
            }
            var card = lineup.CardFor(who).Value;
            Portrait(who, new Rect(r.x + 12, r.y + 39, 100, 96));
            Label(r.x + 125, r.y + 38, r.width - 137, 26, card.Name.ToUpperInvariant(), _heading);
            Label(r.x + 125, r.y + 64, r.width - 137, 22, HowToPlay.CardBatHand(card.Bats), _small);
            var values = new[] { card.Stats.Pitch, card.Stats.Bat, card.Stats.Field, card.Stats.Run };
            var labels = new[] { "PITCH", "BAT", "FIELD", "RUN" };
            for (var i = 0; i < 4; i++)
            {
                var y = r.y + 91 + i * 21;
                Label(r.x + 125, y, 45, 20, labels[i], _small);
                Fill(new Rect(r.x + 175, y + 6, 100, 7), new Color(1, 1, 1, .10f));
                Fill(new Rect(r.x + 175, y + 6, 100 * (float)CharacterCard.BarFill(values[i]), 7), new Color(.76f, .85f, .83f));
                Label(r.x + 289, y, 26, 20, values[i].ToString(), _small);
            }
            Label(r.x + 16, r.y + 184, r.width - 32, 24, card.StarPitch, _body);
            Label(r.x + 16, r.y + 207, r.width - 32, 23, card.StarSwing + " · " + card.FieldVerb, _small);
            if (seat != LineupSeat.Cpu && lineup.Step == LineupStep.DefenseSetup)
            {
                var keys = Controls.SeatUsesKeyboard(seat == LineupSeat.Pad1 ? 0 : 1);
                Label(r.x + 16, r.y + 240, r.width - 32, 20, ready
                    ? (keys ? "Waiting for other player · F to edit" : "Waiting for other player · West to edit")
                    : (keys ? "Q  Continue to match settings" : "North  Continue to match settings"), _small);
            }
        }

        static void Portrait(Character who, Rect r)
        {
            if (who == null) return;
            var tex = Look.Portrait(who);
            if (tex != null) GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
        }
        static void Button(LineupCell cell, string text, bool primary)
        {
            var r = RectOf(cell);
            Fill(r, primary ? new Color(.20f, .32f, .30f) : new Color(1, 1, 1, .055f));
            Border(r, primary ? new Color(.60f, .79f, .69f) : new Color(1, 1, 1, .14f), 1);
            Label(r.x + 12, r.y, r.width - 24, r.height, text, _body);
        }
        static Rect RectOf(LineupCell c) => new Rect((float)c.X * 1280, (float)(1 - c.Y - c.H) * 800, (float)c.W * 1280, (float)c.H * 800);
        static void Label(float x, float y, float w, float h, string text, GUIStyle style) => GUI.Label(new Rect(x, y, w, h), text, style);
        static void Fill(Rect r, Color color)
        {
            var old = GUI.color; GUI.color = DisplayColor(color); GUI.DrawTexture(r, _white); GUI.color = old;
        }
        static void Border(Rect r, Color c, float n)
        {
            Fill(new Rect(r.x, r.y, r.width, n), c); Fill(new Rect(r.x, r.yMax - n, r.width, n), c);
            Fill(new Rect(r.x, r.y, n, r.height), c); Fill(new Rect(r.xMax - n, r.y, n, r.height), c);
        }
        static void Ensure()
        {
            if (_white != null) return;
            _white = Texture2D.whiteTexture;
            _title = Style(32, Color.white, FontStyle.Bold);
            _heading = Style(19, Color.white, FontStyle.Bold);
            _body = Style(15, new Color(.82f, .88f, .88f), FontStyle.Normal);
            _small = Style(12, Muted, FontStyle.Bold);
            _name = Style(13, Color.white, FontStyle.Bold); _name.wordWrap = true;
            _mark = Style(12, Color.white, FontStyle.Bold); _mark.alignment = TextAnchor.MiddleCenter;
            _field = FieldTexture();
        }
        // IMGUI writes directly to the linear player target; palette values are authored in sRGB.
        static Color DisplayColor(Color color) => QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
        static GUIStyle Style(int size, Color color, FontStyle weight)
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = weight, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
            s.normal.textColor = color; return s;
        }

        // A schematic baseball field: a curved outfield, foul lines from home, dirt apron,
        // four bags and a mound. Coordinates share the field's board frame; no park art is invented.
        static Texture2D FieldTexture()
        {
            const int w = 840, h = 600;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[w * h];
            var bags = new[] { new Vector2(0, 0), new Vector2(.27f, .27f), new Vector2(0, .54f), new Vector2(-.27f, .27f) };
            for (var py = 0; py < h; py++)
            for (var px = 0; px < w; px++)
            {
                var x = (px + .5f) / w - .5f;
                var y = (py + .5f) / h;
                var z = y - (1f - (float)LineupLayout.FieldHomeY);
                var radius = Mathf.Sqrt(x * x * (float)(LineupLayout.FieldXScale * LineupLayout.FieldXScale) + z * z);
                var fair = LineupLayout.InFairField(x + .5f, 1 - y);
                var col = Color.clear;
                if (fair) col = ((int)(z * 15) % 2 == 0) ? new Color(.15f, .30f, .25f) : new Color(.17f, .33f, .27f);
                var diamond = Mathf.Abs(x) / .27f + Mathf.Abs(z - .27f) / .27f;
                if (fair && diamond < 1.22f) col = new Color(.47f, .36f, .24f);
                if (diamond < .76f) col = new Color(.20f, .36f, .28f);
                if (fair && (Mathf.Abs(z - Mathf.Abs(x) * 1.0f) < .0035f || radius > LineupLayout.FieldRadius - .008)) col = new Color(.73f, .79f, .65f, .85f);
                if (Mathf.Abs(diamond - 1) < .024f) col = new Color(.80f, .76f, .60f);
                if ((x * x + (z - .23f) * (z - .23f)) < .0007f) col = new Color(.54f, .42f, .28f);
                foreach (var bag in bags)
                    if (Mathf.Abs(x - bag.x) + Mathf.Abs(z - bag.y) < .017f) col = new Color(.95f, .93f, .80f);
                pixels[py * w + px] = DisplayColor(col);
            }
            tex.SetPixels(pixels); tex.Apply(); return tex;
        }
        public static void Place(LineupScreens lineup, Transform parent, ChemToy chem, CardToy card) { chem?.Hide(); card?.Hide(); }
        public static void HideBoard() { _hover = false; _shown = null; }
    }
}
