using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Exhibition lineup HUD: Team Setup then Offense/Defense Setup.
    /// OnGUI is the sheet. World quads sat in the dirt and duplicated the HUD.
    /// Faces from Look.Portrait — role players reuse the faction captain.
    /// </summary>
    public static class TeamSheet
    {
        static GUIStyle _h1, _gold, _center, _centerTiny, _tiny, _name;
        static Texture2D _panel, _pipOn, _pipOff, _white, _slot;
        static readonly Dictionary<string, Texture2D> _faction = new Dictionary<string, Texture2D>();

        public static void Draw(Match match, LineupScreens lineup)
        {
            Ensure();
            var w = Screen.width;
            var h = Screen.height;
            var t = Time.unscaledTime;
            var team = lineup.Step == LineupStep.TeamSetup;
            Sticker(team ? "TEAM SETUP" : "OFFENSE / DEFENSE SETUP", ScreenRect(LineupLayout.Title), _h1);
            Sticker(lineup.HomeCaptain.Name.ToUpperInvariant(), ScreenRect(LineupLayout.HomeCaption), _gold);
            Sticker("vs  " + lineup.AwayCaptain.Name.ToUpperInvariant(), ScreenRect(LineupLayout.AwayCaption), _gold);
            if (match != null)
                GUI.Label(ScreenRect(LineupLayout.ParkLine), match.Park.Name, _tiny);
            var cap = ScreenRect(LineupLayout.HomeCaption);
            JumpingStars(cap.xMax + 8f, cap.y, lineup.HomeStars, t);

            if (team)
            {
                DrawBar(lineup, true, false, LineupFocus.HomeRow);
                DrawPool(lineup);
                DrawBar(lineup, false, false, LineupFocus.AwayRow);
            }
            else
            {
                DrawBar(lineup, true, true, LineupFocus.HomeOrder);
                DrawDiamonds(lineup);
                DrawBar(lineup, false, true, LineupFocus.AwayOrder);
            }

            var card = lineup.HighlightCard();
            if (card.HasValue)
                HudView.Card(card.Value, w - 340, h * 0.5f - 116);

            GUI.Label(ScreenRect(LineupLayout.Help), lineup.Help, _gold);
        }

        public static void Place(LineupScreens lineup, Transform parent, ChemToy chem, CardToy card)
        {
            _ = lineup;
            _ = parent;
            HideBoard();
            chem?.Hide();
            card?.Hide();
        }

        public static void HideBoard() { }

        static void DrawBar(LineupScreens lineup, bool home, bool numbered, LineupFocus focus)
        {
            for (var i = 0; i < LineupScreens.Size; i++)
            {
                var cell = home
                    ? (numbered ? LineupLayout.HomeOrder(i) : LineupLayout.HomeSlot(i))
                    : (numbered ? LineupLayout.AwayOrder(i) : LineupLayout.AwaySlot(i));
                var who = numbered
                    ? OrderAt(home ? lineup.Home : lineup.Away, i)
                    : (home ? lineup.HomeSlots : lineup.AwaySlots)[i];
                var on = lineup.Lit(focus, i);
                var mark = numbered ? LineupLayout.OrderMark(i) : LineupLayout.TeamMark(who);
                Head(cell, who, on, mark, lineup.ChemSticker(who));
            }
        }

        static Character OrderAt(TeamBuilder draft, int i)
        {
            if (draft == null || i < 0 || i >= draft.Order.Count) return null;
            return draft.Order[i];
        }

        static void DrawPool(LineupScreens lineup)
        {
            var pool = lineup.Pool;
            for (var i = 0; i < pool.Count; i++)
            {
                var on = lineup.Lit(LineupFocus.Pool, i);
                Head(LineupLayout.PoolCell(i, pool.Count), pool[i], on, "", lineup.ChemSticker(pool[i]));
            }
        }

        static void DrawDiamonds(LineupScreens lineup)
        {
            DiamondSide(lineup, true);
            DiamondSide(lineup, false);
        }

        static void DiamondSide(LineupScreens lineup, bool home)
        {
            var draft = home ? lineup.Home : lineup.Away;
            var focus = home ? LineupFocus.HomeDiamond : LineupFocus.AwayDiamond;
            var panel = home ? LineupLayout.HomeDiamondPanel : LineupLayout.AwayDiamondPanel;
            GUI.DrawTexture(ScreenRect(panel), _panel);
            foreach (var pos in Diamond.Order)
            {
                var cell = LineupLayout.DiamondHead(home, pos);
                Character who = null;
                if (draft != null && draft.Gloves.TryGetValue(pos, out var glove))
                    who = glove;
                var on = lineup.Lit(focus, System.Array.IndexOf(Diamond.Order, pos));
                Head(cell, who, on, LineupLayout.GloveMark(pos), lineup.ChemSticker(who));
            }
        }

        static void Head(LineupCell cell, Character who, bool on, string mark, string chem)
        {
            var r = ScreenRect(cell);
            GUI.DrawTexture(r, on ? _pipOn : (who != null ? CardTex(who) : _slot));
            if (who != null)
            {
                var tex = Look.Portrait(who);
                if (tex != null)
                    GUI.DrawTexture(ScreenRect(LineupLayout.FaceRect(cell)), tex, ScaleMode.ScaleToFit);
                GUI.Label(ScreenRect(LineupLayout.NameRect(cell)), who.Name.ToUpperInvariant(), _name);
            }
            if (!string.IsNullOrEmpty(mark))
                GUI.Label(new Rect(r.x + 4, r.y + 2, r.width - 8, 16), mark, on ? _center : _centerTiny);
            if (chem == ChemistryToy.Heart)
                GUI.DrawTexture(new Rect(r.xMax - 16, r.y + 2, 12, 12), _pipOn);
            else if (chem == ChemistryToy.Scribble)
            {
                var prev = GUI.color;
                GUI.color = new Color(0.86f, 0.18f, 0.22f, 1f);
                GUI.DrawTexture(new Rect(r.xMax - 18, r.y + 6, 14, 6), _white);
                GUI.color = prev;
            }
        }

        static void JumpingStars(float x, float y, int filled, float t)
        {
            for (var i = 0; i < 5; i++)
            {
                var s = (float)ChemistryToy.StarScale(i, filled, t);
                var size = 22f * s;
                var px = x + i * 26f + (22f - size) * 0.5f;
                var py = y + (22f - size) * 0.5f;
                GUI.DrawTexture(new Rect(px, py, size, size),
                    ChemistryToy.StarFilled(i, filled) ? _pipOn : _pipOff);
            }
        }

        static Rect ScreenRect(LineupCell c)
        {
            var w = Screen.width;
            var h = Screen.height;
            return new Rect((float)c.X * w, (float)(1.0 - c.Y - c.H) * h, (float)c.W * w, (float)c.H * h);
        }

        static Texture2D CardTex(Character c)
        {
            if (_faction.TryGetValue(c.Faction, out var t) && t != null) return t;
            var col = Colors.Body(c.Faction);
            col.a = 0.92f;
            t = Tex(Color.Lerp(col, new Color(0.08f, 0.08f, 0.1f), 0.28f));
            _faction[c.Faction] = t;
            return t;
        }

        static void Sticker(string text, Rect r, GUIStyle style)
        {
            var old = GUI.color;
            GUI.color = new Color(0.06f, 0.04f, 0.08f, 0.88f);
            GUI.Label(new Rect(r.x + 3, r.y + 3, r.width, r.height), text, style);
            GUI.color = old;
            GUI.Label(r, text, style);
        }

        static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);

        static void Ensure()
        {
            if (_h1 != null) return;
            _h1 = Sty(26, Color.white, FontStyle.Bold);
            _h1.clipping = TextClipping.Overflow;
            _gold = Sty(16, Gold, FontStyle.Bold);
            _gold.clipping = TextClipping.Overflow;
            _tiny = Sty(14, new Color(0.85f, 0.88f, 0.9f), FontStyle.Normal);
            _name = Sty(11, Color.white, FontStyle.Bold);
            _name.alignment = TextAnchor.MiddleCenter;
            _name.clipping = TextClipping.Overflow;
            _name.padding = new RectOffset(6, 6, 0, 0);
            _center = Sty(13, new Color(0.08f, 0.08f, 0.1f), FontStyle.Bold);
            _center.alignment = TextAnchor.MiddleCenter;
            _center.clipping = TextClipping.Overflow;
            _centerTiny = Sty(11, Color.white, FontStyle.Bold);
            _centerTiny.alignment = TextAnchor.MiddleCenter;
            _centerTiny.clipping = TextClipping.Overflow;
            _panel = Tex(new Color(0.07f, 0.08f, 0.1f, 0.62f));
            _pipOn = Tex(Gold);
            _pipOff = Tex(new Color(1f, 1f, 1f, 0.28f));
            _white = Tex(Color.white);
            _slot = Tex(new Color(0.10f, 0.11f, 0.14f, 0.82f));
        }

        static GUIStyle Sty(int size, Color c, FontStyle fs)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = size;
            s.fontStyle = fs;
            s.normal.textColor = c;
            s.hover.textColor = c;
            return s;
        }

        static Texture2D Tex(Color c)
        {
            var t = new Texture2D(2, 2);
            t.SetPixels(new[] { c, c, c, c });
            t.Apply();
            return t;
        }
    }
}
