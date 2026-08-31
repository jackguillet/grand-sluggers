using System;
using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Exhibition lineup HUD: stars, glove tokens, card, dugout.
    /// World toys + hearts are the picture. No AVAILABLE list, no white rays.
    /// </summary>
    public static class TeamSheet
    {
        static GUIStyle _h1, _body, _gold, _center, _centerTiny;
        static Texture2D _panel, _pipOn, _pipOff, _white;
        static readonly Dictionary<string, Texture2D> _faction = new Dictionary<string, Texture2D>();

        public static void Draw(
            Match match, TeamBuilder home, IReadOnlyList<Character> pool,
            int slot, int poolIndex, bool focusPool)
        {
            Ensure();
            var w = Screen.width;
            var h = Screen.height;
            var t = Time.unscaledTime;
            Sticker(home.Name.ToUpperInvariant() + "  ·  " + match.Park.Name, 36, 18, 640, 36, _h1);
            JumpingStars(36, 58, home.StartingStars, t);
            MiniDiamond(home, slot, 36, 118, 168, 168);

            if (home.Order.Count > 0)
            {
                var who = home.Order[Mathf.Clamp(slot, 0, home.Order.Count - 1)];
                var vs = who.Captain ? Chemistry.Good : home.Chem(who);
                HudView.Card(CharacterCard.Of(who, vs), 36, h - 248);
            }

            Dugout(home, slot, !focusPool, 36, h - 328, w - 380);
            if (focusPool)
                Bench(home, pool, poolIndex, w - 280, 88);
            GUI.Label(new Rect(36, h - 40, w - 80, 24),
                "stick slot / bench    West swap    RB glove P/C/IF/OF    LB/East order    South play",
                _gold);
        }

        static void MiniDiamond(TeamBuilder home, int slot, float x, float y, float w, float h)
        {
            var selected = home.Order.Count > 0
                ? home.Order[Mathf.Clamp(slot, 0, home.Order.Count - 1)]
                : null;
            var group = selected != null ? TeamBuilder.GloveGroup(home.PosOf(selected.Id) ?? "") : "";
            foreach (var g in TeamBuilder.GloveGroups)
            {
                var uv = ChemistryToy.GroupTokenSpot(g);
                var px = x + (float)((uv.U * 0.5 + 0.5) * w);
                var py = y + h - (float)(uv.V * h) - 18;
                var lit = g == group;
                var r = new Rect(px - 18, py - 12, 36, 24);
                GUI.DrawTexture(r, lit ? _pipOn : _panel);
                GUI.Label(r, g, lit ? _center : _centerTiny);
            }
        }

        static void Dugout(TeamBuilder home, int slot, bool lit, float x, float y, float w)
        {
            var n = home.Order.Count;
            if (n == 0) return;
            var cell = Mathf.Min(48f, w / n);
            for (var i = 0; i < n; i++)
            {
                var r = new Rect(x + i * cell, y, cell - 4, 36);
                var on = lit && i == slot;
                GUI.DrawTexture(r, on ? _pipOn : _panel);
                var mark = home.Order[i].Captain ? "C" : (i + 1).ToString();
                GUI.Label(r, mark, on ? _center : _centerTiny);
            }
        }

        static void JumpingStars(float x, float y, int filled, float t)
        {
            for (var i = 0; i < 5; i++)
            {
                var s = (float)ChemistryToy.StarScale(i, filled, t);
                var size = 28f * s;
                var px = x + i * 36f + (28f - size) * 0.5f;
                var py = y + (28f - size) * 0.5f;
                GUI.DrawTexture(new Rect(px, py, size, size),
                    ChemistryToy.StarFilled(i, filled) ? _pipOn : _pipOff);
            }
        }

        static void Bench(TeamBuilder home, IReadOnlyList<Character> pool, int poolIndex, float x, float y)
        {
            var shown = Mathf.Min(pool.Count, 6);
            var start = 0;
            if (pool.Count > 6)
            {
                start = Mathf.Clamp(poolIndex - 2, 0, pool.Count - 6);
                shown = 6;
            }
            for (var n = 0; n < shown; n++)
            {
                var i = start + n;
                var c = pool[i];
                var r = new Rect(x, y, 240, 44);
                var selected = i == poolIndex;
                GUI.DrawTexture(r, selected ? _pipOn : CardTex(c));
                StickerEdge(x + 8, y + 14, home.Chem(c));
                if (Look.HasPortrait(c.Id))
                {
                    var tex = Look.Portrait(c.Id);
                    if (tex != null)
                        GUI.DrawTexture(new Rect(x + 32, y + 4, 36, 36), tex, ScaleMode.ScaleToFit);
                }
                GUI.Label(new Rect(x + 74, y + 10, 160, 24), Short(c.Name), selected ? _center : _body);
                y += 48;
            }
        }

        static void StickerEdge(float x, float y, Chemistry chem)
        {
            var kind = ChemistryToy.Sticker(chem);
            if (kind == ChemistryToy.Heart)
                GUI.DrawTexture(new Rect(x, y, 16, 16), _pipOn);
            else if (kind == ChemistryToy.Scribble)
            {
                var prev = GUI.color;
                GUI.color = new Color(0.86f, 0.18f, 0.22f, 1f);
                GUI.DrawTexture(new Rect(x, y + 4, 16, 8), _white);
                GUI.color = prev;
            }
            else
                GUI.DrawTexture(new Rect(x, y, 16, 16), _pipOff);
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

        static string Short(string n)
        {
            var sp = n.LastIndexOf(' ');
            return sp >= 0 ? n.Substring(sp + 1) : n;
        }

        static void Sticker(string text, float x, float y, float w, float h, GUIStyle style)
        {
            var old = GUI.color;
            GUI.color = new Color(0.06f, 0.04f, 0.08f, 0.88f);
            GUI.Label(new Rect(x + 3, y + 3, w, h), text, style);
            GUI.color = old;
            GUI.Label(new Rect(x, y, w, h), text, style);
        }

        static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);

        static void Ensure()
        {
            if (_h1 != null) return;
            _h1 = Sty(28, Color.white, FontStyle.Bold);
            _body = Sty(18, new Color(0.95f, 0.96f, 0.97f), FontStyle.Normal);
            _gold = Sty(16, Gold, FontStyle.Bold);
            _center = Sty(16, new Color(0.08f, 0.08f, 0.1f), FontStyle.Bold);
            _center.alignment = TextAnchor.MiddleCenter;
            _centerTiny = Sty(13, Color.white, FontStyle.Bold);
            _centerTiny.alignment = TextAnchor.MiddleCenter;
            _panel = Tex(new Color(0.07f, 0.08f, 0.1f, 0.78f));
            _pipOn = Tex(Gold);
            _pipOff = Tex(new Color(1f, 1f, 1f, 0.28f));
            _white = Tex(Color.white);
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
