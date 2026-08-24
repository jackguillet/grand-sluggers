using System;
using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Exhibition lineup as a chemistry toy: diamond tokens, sticker graph, jumping stars.
    /// </summary>
    public static class TeamSheet
    {
        static GUIStyle _h1, _body, _gold, _tiny, _center, _centerTiny;
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
            MiniDiamond(home, slot, 36, 118, 280, 280);
            Graph(home, slot, w * 0.48f, h * 0.42f, Mathf.Min(w * 0.18f, 170f));
            PoolColumn(home, pool, poolIndex, focusPool, w - 320, 88);

            if (home.Order.Count > 0)
            {
                var who = home.Order[Mathf.Clamp(slot, 0, home.Order.Count - 1)];
                var vs = who.Captain ? Chemistry.Good : home.Chem(who);
                BigToy(who, home.PosOf(who.Id), w * 0.5f - 70, h * 0.42f - 90);
                HudView.Card(CharacterCard.Of(who, vs), 36, h - 248);
            }

            Dugout(home, slot, !focusPool, 36, h - 328, w - 380);
            GUI.Label(new Rect(36, h - 40, w - 80, 24),
                "stick slot / pool    West swap    RB glove P/C/IF/OF    LB/East order    South play",
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
                var py = y + h - (float)(uv.V * h) - 22;
                var lit = g == group;
                var r = new Rect(px - 22, py - 14, 44, 28);
                GUI.DrawTexture(r, lit ? _pipOn : _panel);
                GUI.Label(r, g, lit ? _center : _centerTiny);
            }
            foreach (var pos in Diamond.Order)
            {
                if (!home.Gloves.TryGetValue(pos, out var who)) continue;
                var uv = ChemistryToy.MiniSpot(pos);
                var px = x + (float)((uv.U * 0.42 + 0.5) * w);
                var py = y + h - (float)(uv.V * 0.82 * h) - 18;
                var i = SlotOf(home, who.Id);
                var mark = who.Captain ? "C" : (i + 1).ToString();
                var r = new Rect(px - 16, py - 16, 32, 32);
                GUI.DrawTexture(r, i == slot ? _pipOn : CardTex(who));
                GUI.Label(r, mark, _centerTiny);
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

        static void BigToy(Character who, string pos, float x, float y)
        {
            var r = new Rect(x, y, 140, 176);
            GUI.DrawTexture(r, CardTex(who));
            if (Look.HasPortrait(who.Id))
            {
                var tex = Look.Portrait(who.Id);
                if (tex != null)
                    GUI.DrawTexture(new Rect(r.x + 8, r.y + 8, r.width - 16, r.height - 36), tex, ScaleMode.ScaleToFit);
            }
            GUI.Label(new Rect(r.x, r.yMax - 28, r.width, 24),
                (string.IsNullOrEmpty(pos) ? "" : pos + "  ") + Short(who.Name), _centerTiny);
        }

        static void PoolColumn(TeamBuilder home, IReadOnlyList<Character> pool, int poolIndex, bool lit, float x, float y)
        {
            GUI.Label(new Rect(x, y, 300, 22), "AVAILABLE", _gold);
            y += 26;
            var shown = Mathf.Min(pool.Count, 10);
            var start = 0;
            if (pool.Count > 10)
            {
                start = Mathf.Clamp(poolIndex - 4, 0, pool.Count - 10);
                shown = 10;
            }
            for (var n = 0; n < shown; n++)
            {
                var i = start + n;
                var c = pool[i];
                var r = new Rect(x, y, 292, 34);
                var selected = lit && i == poolIndex;
                GUI.DrawTexture(r, selected ? _pipOn : CardTex(c));
                StickerEdge(x + 10, y + 8, home.Chem(c));
                GUI.Label(new Rect(x + 36, y + 6, 250, 24), c.Name, selected ? _center : _body);
                y += 38;
            }
        }

        static void Graph(TeamBuilder home, int slot, float cx, float cy, float radius)
        {
            var cap = home.Captain;
            var capMid = new Vector2(cx, cy);
            var others = new List<Character>();
            foreach (var c in home.Order)
                if (!c.Id.Equals(cap.Id, System.StringComparison.OrdinalIgnoreCase))
                    others.Add(c);
            for (var i = 0; i < others.Count; i++)
            {
                var ang = i / (float)others.Count * Mathf.PI * 2f - Mathf.PI * 0.5f;
                var px = cx + Mathf.Cos(ang) * radius;
                var py = cy + Mathf.Sin(ang) * radius;
                var selected = SlotOf(home, others[i].Id) == slot;
                var chem = home.Chem(others[i]);
                StickerSpoke(capMid, new Vector2(px, py), chem, selected);
                var size = selected ? 96f : 56f;
                DrawPerson(new Rect(px - size * 0.5f, py - size * 0.55f, size, size * 1.15f),
                    others[i], selected, Look.HasPortrait(others[i].Id), home.PosOf(others[i].Id));
            }
        }

        static void StickerSpoke(Vector2 from, Vector2 to, Chemistry chem, bool selected)
        {
            var kind = ChemistryToy.Sticker(chem);
            if (kind == ChemistryToy.None) return;
            if (kind == ChemistryToy.Heart)
            {
                Edge(from, to, Gold, selected ? 10f : 6f);
                var mid = (from + to) * 0.5f;
                GUI.DrawTexture(new Rect(mid.x - 9, mid.y - 9, 18, 18), _pipOn);
            }
            else
            {
                var n = (to - from).normalized;
                var perp = new Vector2(-n.y, n.x) * 5f;
                Edge(from + perp, to + perp, Red, selected ? 5f : 3f);
                Edge(from - perp, to - perp, Red, selected ? 5f : 3f);
                Edge(from, to, Red, selected ? 4f : 2.5f);
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
                GUI.color = Red;
                GUI.DrawTexture(new Rect(x, y + 4, 16, 8), _white);
                GUI.color = prev;
            }
            else
                GUI.DrawTexture(new Rect(x, y, 16, 16), _pipOff);
        }

        static void DrawPerson(Rect r, Character c, bool selected, bool portrait, string pos)
        {
            GUI.DrawTexture(r, CardTex(c));
            if (selected)
            {
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, 4), _pipOn);
                GUI.DrawTexture(new Rect(r.x, r.yMax - 4, r.width, 4), _pipOn);
                GUI.DrawTexture(new Rect(r.x, r.y, 4, r.height), _pipOn);
                GUI.DrawTexture(new Rect(r.xMax - 4, r.y, 4, r.height), _pipOn);
            }
            if (portrait)
            {
                var tex = Look.Portrait(c.Id);
                if (tex != null)
                    GUI.DrawTexture(new Rect(r.x + 6, r.y + 6, r.width - 12, r.height - 28), tex, ScaleMode.ScaleToFit);
            }
            GUI.Label(new Rect(r.x, r.yMax - 24, r.width, 22),
                (string.IsNullOrEmpty(pos) ? "" : pos + "  ") + Short(c.Name), _centerTiny);
        }

        static int SlotOf(TeamBuilder home, string id)
        {
            for (var i = 0; i < home.Order.Count; i++)
                if (home.Order[i].Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        static void Edge(Vector2 from, Vector2 to, Color color, float thickness)
        {
            var dir = to - from;
            var len = dir.magnitude;
            if (len < 1f) return;
            var ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var old = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang, from);
            var prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(from.x, from.y - thickness * 0.5f, len, thickness), _white);
            GUI.color = prev;
            GUI.matrix = old;
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
        static readonly Color Red = new Color(0.86f, 0.18f, 0.22f, 1f);

        static void Ensure()
        {
            if (_h1 != null) return;
            _h1 = Sty(28, Color.white, FontStyle.Bold);
            _body = Sty(18, new Color(0.95f, 0.96f, 0.97f), FontStyle.Normal);
            _gold = Sty(16, Gold, FontStyle.Bold);
            _tiny = Sty(14, new Color(0.85f, 0.88f, 0.9f), FontStyle.Normal);
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
