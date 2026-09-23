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

        public static void UseController() { _hover = false; _pointerMode = false; }

        public static Action Pointer(LineupScreens lineup, out LineupFocus focus, out int index)
        {
            focus = default;
            index = -1;
            var mouse = Mouse.current;
            if (mouse == null || !Controls.SeatUsesKeyboard(0)) { _hover = false; return Action.None; }
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
            Label(24, 24, 860, 22, team ? "01  /  CHOOSE YOUR TEAM" : "02  /  SET YOUR LINEUP", _small);
            Label(24, 54, 1000, 46, team ? "Build your nine" : "Batting order & field positions", _title);
            Label(24, 108, 880, 26, lineup.Help, _body);
            Label(936, 32, 320, 24, match == null ? "EXHIBITION" : match.Park.Name.ToUpperInvariant(), _small);
            Label(936, 60, 320, 26, "BOTH TEAMS  ·  " + lineup.HomeStars + " STARTING STARS", _small);
            var inspected = _pointerMode ? (_hover ? lineup.CharacterAt(_hoverFocus, _hoverIndex) : null) : lineup.Highlighted;
            if (team)
            {
                Label(24, 137, 880, 24, "HOME  /  " + lineup.HomeCaptain.Name.ToUpperInvariant(), _heading);
                DrawCells(lineup, LineupFocus.HomeRow, 9, inspected);
                DrawCells(lineup, LineupFocus.Pool, lineup.Pool.Count, inspected);
                Label(24, 613, 880, 24, "AWAY  /  " + lineup.AwayCaptain.Name.ToUpperInvariant(), _heading);
                DrawCells(lineup, LineupFocus.AwayRow, 9, inspected);
            }
            else
            {
                Side(lineup, true, inspected);
                Side(lineup, false, inspected);
            }
            PlayerCard(lineup, inspected);
            var keys = Controls.SeatUsesKeyboard(0);
            Button(LineupLayout.BackButton, team ? (keys ? "F  Remove player" : "West  Remove player") : lineup.AnyPick ? (keys ? "F  Cancel pick" : "West  Cancel pick") : (keys ? "F  Back" : "West  Back"), false);
            if (team)
            {
                Button(LineupLayout.FillButton, keys ? "Tab  Fill team" : "RB  Fill team", false);
                Label(390, 716, 605, 46, keys ? "WASD  Move   ·   Space  Add / continue\nEsc  How to play" : "Stick  Move   ·   South  Add / continue\nEsc  How to play", _body);
            }
            else Label(206, 720, 785, 42, keys ? "WASD  Move     Space / click  Pick & swap\nG  List / field     Esc  How to play" : "Stick  Move     South  Pick & swap\nEast  List / field     Esc  How to play", _body);
            Button(LineupLayout.ContinueButton, team ? "Continue  →" : (keys ? "Q  First pitch  →" : "North  First pitch  →"), team ? lineup.Ready : !lineup.AnyPick);
            GUI.matrix = old;
        }

        static void Side(LineupScreens lineup, bool home, Character inspected)
        {
            var x = home ? 24 : 472;
            var seat = home ? lineup.HomeSeat : lineup.AwaySeat;
            var captain = home ? lineup.HomeCaptain : lineup.AwayCaptain;
            Label(x, 151, 432, 27, (home ? "HOME" : "AWAY") + "  /  " + captain.Name.ToUpperInvariant()
                + "  ·  " + (seat == LineupSeat.Cpu ? "CPU" : seat == LineupSeat.Pad1 ? "P1" : "P2"), _heading);
            Label(x, 178, 142, 20, "BATTING ORDER", _small);
            Label(x + 156, 178, 275, 20, "FIELD POSITIONS", _small);
            GUI.DrawTexture(RectOf(home ? LineupLayout.HomeDiamondPanel : LineupLayout.AwayDiamondPanel), _field);
            DrawCells(lineup, home ? LineupFocus.HomeOrder : LineupFocus.AwayOrder, 9, inspected);
            DrawCells(lineup, home ? LineupFocus.HomeDiamond : LineupFocus.AwayDiamond, 9, inspected);
        }

        static void DrawCells(LineupScreens lineup, LineupFocus focus, int count, Character inspected)
        {
            for (var i = 0; i < count; i++)
            {
                var c = Cell(focus, i, count);
                var who = lineup.CharacterAt(focus, i);
                var on = _pointerMode ? _hover && focus == _hoverFocus && i == _hoverIndex : lineup.Lit(focus, i);
                var picked = lineup.Picked(focus, i);
                var buddy = lineup.Buddies(inspected, who);
                var r = RectOf(c);
                var order = focus is LineupFocus.HomeOrder or LineupFocus.AwayOrder;
                var field = focus is LineupFocus.HomeDiamond or LineupFocus.AwayDiamond;
                // Only interaction and a currently inspected relationship add color; portraits have no faction frame.
                if (order || who == null) Fill(r, new Color(1, 1, 1, .035f));
                if (buddy) Fill(r, new Color(.76f, .96f, .88f, .13f));
                if (on) Fill(r, new Color(1, 1, 1, .10f));
                if (on || picked) Border(r, picked ? Gold : new Color(1, 1, 1, .65f), picked ? 2 : 1);
                if (order)
                {
                    Label(r.x + 6, r.y, 22, r.height, (i + 1).ToString("00"), _mark);
                    Portrait(who, new Rect(r.x + 28, r.y + 3, 36, 43));
                    Label(r.x + 67, r.y + 3, r.width - 69, r.height - 6, who?.Name ?? "Empty", _name);
                }
                else
                {
                    var mark = field ? Diamond.Order[i] : LineupLayout.TeamMark(who);
                    Label(r.x, r.y - 3, r.width, 18, mark, _mark);
                    Portrait(who, new Rect(r.x + 6, r.y + 13, r.width - 12, r.height - 31));
                    Label(r.x - 7, r.yMax - 18, r.width + 14, 20, who?.Name ?? "+", _mark);
                }
            }
        }

        static void PlayerCard(LineupScreens lineup, Character who)
        {
            var r = RectOf(LineupLayout.CardPanel);
            Fill(r, new Color(.075f, .115f, .14f));
            Label(r.x + 20, r.y + 18, r.width - 40, 22, "PLAYER CARD", _small);
            if (who == null)
            {
                Label(r.x + 20, r.y + 75, r.width - 40, 100, "Hover over a player\nor move to them\nto see their card.", _body);
                return;
            }
            var card = lineup.CardFor(who).Value;
            Portrait(who, new Rect(r.x + 78, r.y + 48, 172, 150));
            Label(r.x + 20, r.y + 204, r.width - 40, 34, card.Name.ToUpperInvariant(), _heading);
            Label(r.x + 20, r.y + 239, r.width - 40, 23, HowToPlay.CardBatHand(card.Bats), _small);
            var values = new[] { card.Stats.Pitch, card.Stats.Bat, card.Stats.Field, card.Stats.Run };
            var labels = new[] { "PITCH", "BAT", "FIELD", "RUN" };
            for (var i = 0; i < 4; i++)
            {
                var y = r.y + 278 + i * 30;
                Label(r.x + 20, y, 62, 22, labels[i], _small);
                Fill(new Rect(r.x + 91, y + 6, 170, 8), new Color(1, 1, 1, .10f));
                Fill(new Rect(r.x + 91, y + 6, 170 * (float)CharacterCard.BarFill(values[i]), 8), new Color(.76f, .85f, .83f));
                Label(r.x + 277, y, 30, 22, values[i].ToString(), _small);
            }
            Label(r.x + 20, r.y + 410, r.width - 40, 30, card.StarPitch, _body);
            Label(r.x + 20, r.y + 442, r.width - 40, 30, card.StarSwing, _body);
            Label(r.x + 20, r.y + 477, r.width - 40, 44, card.FieldVerb, _small);
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
            var old = GUI.color; GUI.color = color; GUI.DrawTexture(r, _white); GUI.color = old;
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
        static GUIStyle Style(int size, Color color, FontStyle weight)
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = weight, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
            s.normal.textColor = color; return s;
        }

        // A schematic baseball field: a curved outfield, foul lines from home, dirt apron,
        // four bags and a mound. Coordinates share the field's board frame; no park art is invented.
        static Texture2D FieldTexture()
        {
            const int w = 560, h = 896;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[w * h];
            var bags = new[] { new Vector2(0, 0), new Vector2(.27f, .19f), new Vector2(0, .38f), new Vector2(-.27f, .19f) };
            for (var py = 0; py < h; py++)
            for (var px = 0; px < w; px++)
            {
                var x = (px + .5f) / w - .5f;
                var y = (py + .5f) / h;
                var z = y - .16f;
                var radius = Mathf.Sqrt(x * x * 1.65f * 1.65f + z * z);
                var fair = z >= Mathf.Abs(x) * .70f && radius < .83f;
                var col = Color.clear;
                if (fair) col = ((int)(z * 15) % 2 == 0) ? new Color(.15f, .30f, .25f) : new Color(.17f, .33f, .27f);
                var diamond = Mathf.Abs(x) / .27f + Mathf.Abs(z - .19f) / .19f;
                if (fair && diamond < 1.22f) col = new Color(.47f, .36f, .24f);
                if (diamond < .76f) col = new Color(.20f, .36f, .28f);
                if (fair && (Mathf.Abs(z - Mathf.Abs(x) * .70f) < .0035f || radius > .822f)) col = new Color(.73f, .79f, .65f, .85f);
                if (Mathf.Abs(diamond - 1) < .024f) col = new Color(.80f, .76f, .60f);
                if ((x * x + (z - .15f) * (z - .15f)) < .0007f) col = new Color(.54f, .42f, .28f);
                foreach (var bag in bags)
                    if (Mathf.Abs(x - bag.x) + Mathf.Abs(z - bag.y) < .017f) col = new Color(.95f, .93f, .80f);
                pixels[py * w + px] = col;
            }
            tex.SetPixels(pixels); tex.Apply(); return tex;
        }
        public static void Place(LineupScreens lineup, Transform parent, ChemToy chem, CardToy card) { chem?.Hide(); card?.Hide(); }
        public static void HideBoard() { _hover = false; _shown = null; }
    }
}
