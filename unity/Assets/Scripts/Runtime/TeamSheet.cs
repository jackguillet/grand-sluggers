using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Exhibition lineup: draft the eight around the captain, assign gloves, chemistry as a graph.
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
            GUI.DrawTexture(new Rect(24, 18, 520, 86), _panel);
            GUI.Label(new Rect(40, 24, 500, 32), home.Name.ToUpperInvariant() + "  ·  " + match.Park.Name, _h1);
            GUI.Label(new Rect(40, 58, 280, 22), "STARTING STARS", _tiny);
            Stars(200, 58, home.StartingStars);
            GUI.Label(new Rect(320, 56, 200, 22), home.StartingStars + " / 5", _gold);

            OrderColumn(home, slot, !focusPool, 32, 118);
            Graph(home, slot, w / 2f, h * 0.48f, Mathf.Min(w * 0.22f, 210f));
            PoolColumn(home, pool, poolIndex, focusPool, w - 340, 118);

            if (home.Order.Count > 0)
            {
                var who = home.Order[Mathf.Clamp(slot, 0, home.Order.Count - 1)];
                var vs = who.Captain ? Chemistry.Good : home.Chem(who);
                HudView.Card(CharacterCard.Of(who, vs), 40, h - 252);
            }

            GUI.Label(new Rect(40, h - 42, w - 80, 24),
                "stick slot / pool   West swap   RB glove   LB / East order   South play   [B][G] gear",
                _gold);
        }

        static void OrderColumn(TeamBuilder home, int slot, bool lit, float x, float y)
        {
            GUI.Label(new Rect(x, y, 280, 22), "LINEUP", _gold);
            y += 26;
            for (var i = 0; i < home.Order.Count; i++)
            {
                var c = home.Order[i];
                var pos = home.PosOf(c.Id) ?? "";
                var group = string.IsNullOrEmpty(pos) ? "" : TeamBuilder.GloveGroup(pos);
                var r = new Rect(x, y, 300, 36);
                var selected = lit && i == slot;
                GUI.DrawTexture(r, selected ? _pipOn : CardTex(c));
                var ink = selected ? _center : _body;
                var mark = c.Captain ? "C" : (i + 1).ToString();
                GUI.Label(new Rect(x + 8, y + 6, 28, 24), mark, ink);
                GUI.Label(new Rect(x + 36, y + 6, 48, 24), pos + (group == pos ? "" : " " + group), selected ? _centerTiny : _tiny);
                GUI.Label(new Rect(x + 110, y + 6, 180, 24), c.Name, ink);
                y += 40;
            }
        }

        static void PoolColumn(TeamBuilder home, IReadOnlyList<Character> pool, int poolIndex, bool lit, float x, float y)
        {
            GUI.Label(new Rect(x, y, 300, 22), "AVAILABLE", _gold);
            y += 26;
            var shown = Mathf.Min(pool.Count, 12);
            var start = 0;
            if (pool.Count > 12)
            {
                start = Mathf.Clamp(poolIndex - 5, 0, pool.Count - 12);
                shown = 12;
            }
            for (var n = 0; n < shown; n++)
            {
                var i = start + n;
                var c = pool[i];
                var r = new Rect(x, y, 308, 36);
                var selected = lit && i == poolIndex;
                GUI.DrawTexture(r, selected ? _pipOn : CardTex(c));
                var chem = home.Chem(c);
                DrawChemPip(x + 8, y + 10, chem);
                GUI.Label(new Rect(x + 32, y + 6, 270, 24), c.Name, selected ? _center : _body);
                y += 40;
            }
        }

        static void Graph(TeamBuilder home, int slot, float cx, float cy, float radius)
        {
            var cap = home.Captain;
            var capR = new Rect(cx - 52, cy - 64, 104, 128);
            DrawPerson(capR, cap, true, true, home.PosOf(cap.Id));

            var others = new List<Character>();
            foreach (var c in home.Order)
                if (!c.Id.Equals(cap.Id, System.StringComparison.OrdinalIgnoreCase))
                    others.Add(c);

            var capMid = new Vector2(cx, cy);
            for (var i = 0; i < others.Count; i++)
            {
                var ang = i / (float)others.Count * Mathf.PI * 2f - Mathf.PI * 0.5f;
                var px = cx + Mathf.Cos(ang) * radius;
                var py = cy + Mathf.Sin(ang) * radius;
                var r = new Rect(px - 40, py - 48, 80, 96);
                var selected = SlotOf(home, others[i].Id) == slot;
                var chem = home.Chem(others[i]);
                if (chem != Chemistry.Neutral)
                    Edge(capMid, new Vector2(px, py), chem == Chemistry.Good ? Gold : Red, selected ? 6f : 3f);
                DrawPerson(r, others[i], selected, Look.HasPortrait(others[i].Id), home.PosOf(others[i].Id));
            }
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
                if (home.Order[i].Id.Equals(id, System.StringComparison.OrdinalIgnoreCase))
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

        static void DrawChemPip(float x, float y, Chemistry chem)
        {
            var tex = chem == Chemistry.Good ? _pipOn : chem == Chemistry.Bad ? _white : _pipOff;
            var prev = GUI.color;
            if (chem == Chemistry.Bad) GUI.color = Red;
            else if (chem == Chemistry.Neutral) GUI.color = new Color(1f, 1f, 1f, 0.35f);
            GUI.DrawTexture(new Rect(x, y, 16, 16), tex);
            GUI.color = prev;
        }

        static void Stars(float x, float y, int n)
        {
            for (var i = 0; i < 5; i++)
                GUI.DrawTexture(new Rect(x + i * 20, y, 16, 16), n > i ? _pipOn : _pipOff);
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

        static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
        static readonly Color Red = new Color(0.86f, 0.18f, 0.22f, 1f);

        static void Ensure()
        {
            if (_h1 != null) return;
            _h1 = Sty(26, Color.white, FontStyle.Bold);
            _body = Sty(18, new Color(0.95f, 0.96f, 0.97f), FontStyle.Normal);
            _gold = Sty(18, Gold, FontStyle.Bold);
            _tiny = Sty(14, new Color(0.85f, 0.88f, 0.9f), FontStyle.Normal);
            _center = Sty(16, new Color(0.08f, 0.08f, 0.1f), FontStyle.Bold);
            _center.alignment = TextAnchor.MiddleLeft;
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
