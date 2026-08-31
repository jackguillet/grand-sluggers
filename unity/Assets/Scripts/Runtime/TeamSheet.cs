using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Exhibition lineup HUD: Team Setup then Offense/Defense Setup.
    /// Two bars, a pool grid, two diamonds. World tiles so Camera.Render stills see it.
    /// No AVAILABLE list. Portraits from Look.Portrait.
    /// </summary>
    public static class TeamSheet
    {
        static GUIStyle _h1, _gold, _center, _centerTiny, _tiny;
        static Texture2D _panel, _pipOn, _pipOff, _white, _slot;
        static readonly Dictionary<string, Texture2D> _faction = new Dictionary<string, Texture2D>();
        static Board _board;

        public static void Draw(Match match, LineupScreens lineup)
        {
            Ensure();
            var w = Screen.width;
            var h = Screen.height;
            var t = Time.unscaledTime;
            var team = lineup.Step == LineupStep.TeamSetup;
            Sticker(team ? "TEAM SETUP" : "OFFENSE / DEFENSE SETUP", 28, 10, 720, 32, _h1);
            Sticker(lineup.HomeCaptain.Name.ToUpperInvariant(), 28, 42, 360, 24, _gold);
            Sticker("vs  " + lineup.AwayCaptain.Name.ToUpperInvariant(), w - 360, 42, 332, 24, _gold);
            if (match != null)
                GUI.Label(new Rect(28, 66, 480, 20), match.Park.Name, _tiny);
            JumpingStars(ScreenRect(LineupLayout.HomeSlot(0)).x, 70, lineup.HomeStars, t);

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

            GUI.Label(new Rect(28, h - 36, w - 56, 24), lineup.Help, _gold);
        }

        public static void Place(LineupScreens lineup, Transform parent, ChemToy chem, CardToy card)
        {
            if (lineup == null)
            {
                HideBoard();
                chem?.Hide();
                card?.Hide();
                return;
            }
            if (_board == null) _board = Board.Attach(parent);
            _board.Show(lineup, chem, card);
        }

        public static void HideBoard()
        {
            _board?.Hide();
        }

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
                var drop = lineup.Step == LineupStep.TeamSetup && lineup.Lit(focus, i);
                var on = lineup.Lit(focus, i) || drop;
                Head(cell, who, on, numbered ? (i + 1).ToString() : (who != null && who.Captain ? "C" : ""),
                    lineup.ChemSticker(who));
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
                Head(cell, who, on, pos, lineup.ChemSticker(who));
            }
        }

        static void Head(LineupCell cell, Character who, bool on, string mark, string chem)
        {
            var r = ScreenRect(cell);
            GUI.DrawTexture(r, on ? _pipOn : (who != null ? CardTex(who) : _slot));
            if (who != null && Look.HasPortrait(who.Id))
            {
                var tex = Look.Portrait(who.Id);
                if (tex != null)
                {
                    var pad = Mathf.Min(6f, r.width * 0.12f);
                    GUI.DrawTexture(new Rect(r.x + pad, r.y + pad, r.width - pad * 2f, r.height - pad * 2f - 10f),
                        tex, ScaleMode.ScaleToFit);
                }
            }
            if (!string.IsNullOrEmpty(mark))
                GUI.Label(new Rect(r.x, r.yMax - 16, r.width, 16), mark, on ? _center : _centerTiny);
            if (chem == ChemistryToy.Heart)
                GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, 12, 12), _pipOn);
            else if (chem == ChemistryToy.Scribble)
            {
                var prev = GUI.color;
                GUI.color = new Color(0.86f, 0.18f, 0.22f, 1f);
                GUI.DrawTexture(new Rect(r.x + 2, r.y + 6, 14, 6), _white);
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
            _h1 = Sty(26, Color.white, FontStyle.Bold);
            _gold = Sty(16, Gold, FontStyle.Bold);
            _tiny = Sty(14, new Color(0.85f, 0.88f, 0.9f), FontStyle.Normal);
            _center = Sty(13, new Color(0.08f, 0.08f, 0.1f), FontStyle.Bold);
            _center.alignment = TextAnchor.MiddleCenter;
            _centerTiny = Sty(11, Color.white, FontStyle.Bold);
            _centerTiny.alignment = TextAnchor.MiddleCenter;
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

        sealed class Board
        {
            readonly Transform _root;
            readonly List<Tile> _tiles = new List<Tile>();
            int _used;

            Board(Transform root) { _root = root; }

            public static Board Attach(Transform parent)
            {
                var go = new GameObject("LineupBoard");
                go.transform.SetParent(parent, false);
                return new Board(go.transform);
            }

            public void Hide()
            {
                if (_root != null) _root.gameObject.SetActive(false);
            }

            public void Show(LineupScreens lineup, ChemToy chem, CardToy card)
            {
                _root.gameObject.SetActive(true);
                _used = 0;
                var cam = new Vector3((float)ChemistryToy.CamX, (float)ChemistryToy.CamY, (float)ChemistryToy.CamZ);
                var look = new Vector3((float)ChemistryToy.LookX, (float)ChemistryToy.LookY, (float)ChemistryToy.LookZ);
                var forward = (look - cam).normalized;
                var right = Vector3.Cross(Vector3.up, forward).normalized;
                var up = Vector3.Cross(forward, right).normalized;
                var center = cam + forward * 16f;
                const float bw = 22f, bh = 12.6f;

                Vector3 At(LineupCell c) =>
                    center + right * (float)((c.CX - 0.5) * bw) + up * (float)((c.CY - 0.5) * bh);

                Vector3 Size(LineupCell c) =>
                    new Vector3(Mathf.Max(0.55f, (float)c.W * bw), Mathf.Max(0.7f, (float)c.H * bh), 0.08f);

                void Put(LineupCell cell, Character who, bool on, string mark)
                {
                    var tile = Next();
                    var at = At(cell);
                    var sz = Size(cell);
                    if (on) sz *= 1.12f;
                    tile.Root.transform.position = at;
                    tile.Root.transform.rotation = Quaternion.LookRotation(-forward, up);
                    tile.Root.SetActive(true);
                    tile.Face.transform.localScale = sz;
                    var col = who != null ? Colors.Body(who.Faction) : new Color(0.12f, 0.12f, 0.14f);
                    if (on) col = Color.Lerp(col, Gold, 0.45f);
                    var tex = who != null && Look.HasPortrait(who.Id) ? Look.Portrait(who.Id) : null;
                    Paint(tile, col, tex);
                    if (tile.Mark != null)
                    {
                        tile.Mark.text = mark ?? "";
                        tile.Mark.gameObject.SetActive(!string.IsNullOrEmpty(mark));
                    }
                }

                if (lineup.Step == LineupStep.TeamSetup)
                {
                    for (var i = 0; i < LineupScreens.Size; i++)
                    {
                        var who = lineup.HomeSlots[i];
                        var on = lineup.Lit(LineupFocus.HomeRow, i);
                        Put(LineupLayout.HomeSlot(i), who, on, who != null && who.Captain ? "C" : "");
                    }
                    var pool = lineup.Pool;
                    for (var i = 0; i < pool.Count; i++)
                        Put(LineupLayout.PoolCell(i, pool.Count), pool[i],
                            lineup.Lit(LineupFocus.Pool, i), "");
                    for (var i = 0; i < LineupScreens.Size; i++)
                    {
                        var who = lineup.AwaySlots[i];
                        Put(LineupLayout.AwaySlot(i), who,
                            lineup.Lit(LineupFocus.AwayRow, i),
                            who != null && who.Captain ? "C" : "");
                    }
                }
                else
                {
                    for (var i = 0; i < LineupScreens.Size; i++)
                    {
                        var who = lineup.Home != null && i < lineup.Home.Order.Count ? lineup.Home.Order[i] : null;
                        Put(LineupLayout.HomeOrder(i), who, lineup.Lit(LineupFocus.HomeOrder, i), (i + 1).ToString());
                    }
                    foreach (var pos in Diamond.Order)
                    {
                        Character who = null;
                        if (lineup.Home != null && lineup.Home.Gloves.TryGetValue(pos, out var g)) who = g;
                        var on = lineup.Lit(LineupFocus.HomeDiamond, System.Array.IndexOf(Diamond.Order, pos));
                        Put(LineupLayout.DiamondHead(true, pos), who, on, pos);
                    }
                    foreach (var pos in Diamond.Order)
                    {
                        Character who = null;
                        if (lineup.Away != null && lineup.Away.Gloves.TryGetValue(pos, out var g)) who = g;
                        var on = lineup.Lit(LineupFocus.AwayDiamond, System.Array.IndexOf(Diamond.Order, pos));
                        Put(LineupLayout.DiamondHead(false, pos), who, on, pos);
                    }
                    for (var i = 0; i < LineupScreens.Size; i++)
                    {
                        var who = lineup.Away != null && i < lineup.Away.Order.Count ? lineup.Away.Order[i] : null;
                        Put(LineupLayout.AwayOrder(i), who, lineup.Lit(LineupFocus.AwayOrder, i), (i + 1).ToString());
                    }
                }

                for (var i = _used; i < _tiles.Count; i++)
                    _tiles[i].Root.SetActive(false);

                var pick = lineup.Highlighted;
                var sticker = lineup.ChemSticker(pick);
                if (chem != null && pick != null && sticker != ChemistryToy.None)
                {
                    var capCell = CaptainCell(lineup);
                    var pickCell = HighlightCell(lineup);
                    var a = At(capCell);
                    var b = At(pickCell);
                    var mid = (a + b) * 0.5f + up * 0.55f;
                    chem.Show(new List<(Vector3 At, string Kind)> { (mid, sticker) });
                }
                else
                    chem?.Hide();

                var shown = lineup.HighlightCard();
                if (card != null && shown.HasValue && pick != null)
                {
                    var at = At(HighlightCell(lineup)) + right * 3.2f + up * 0.2f;
                    card.Show(shown.Value, at, -forward);
                }
                else
                    card?.Hide();
            }

            Tile Next()
            {
                while (_tiles.Count <= _used)
                    _tiles.Add(Build(_tiles.Count));
                return _tiles[_used++];
            }

            static void Paint(Tile tile, Color col, Texture2D tex)
            {
                if (tile.Mat == null)
                {
                    tile.Mat = Look.Unlit(col, tex);
                    Look.Paint(tile.Face, tile.Mat);
                    return;
                }
                if (tile.Mat.HasProperty("_BaseColor")) tile.Mat.SetColor("_BaseColor", col);
                else tile.Mat.color = col;
                if (tex != null)
                {
                    if (tile.Mat.HasProperty("_BaseMap")) tile.Mat.SetTexture("_BaseMap", tex);
                    tile.Mat.mainTexture = tex;
                }
            }

            Tile Build(int i)
            {
                var root = new GameObject("Tile" + i);
                root.transform.SetParent(_root, false);
                var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
                face.name = "Face";
                face.transform.SetParent(root.transform, false);
                Object.Destroy(face.GetComponent<Collider>());
                var markGo = new GameObject("Mark");
                markGo.transform.SetParent(root.transform, false);
                markGo.transform.localPosition = new Vector3(0f, -0.42f, -0.06f);
                markGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                var mesh = markGo.AddComponent<TextMesh>();
                mesh.fontSize = 42;
                mesh.characterSize = 0.06f;
                mesh.anchor = TextAnchor.MiddleCenter;
                mesh.alignment = TextAlignment.Center;
                mesh.color = Gold;
                mesh.fontStyle = FontStyle.Bold;
                return new Tile { Root = root, Face = face, Mark = mesh };
            }

            static LineupCell CaptainCell(LineupScreens s)
            {
                var away = s.Focus is LineupFocus.AwayRow or LineupFocus.AwayOrder or LineupFocus.AwayDiamond;
                if (s.Step == LineupStep.TeamSetup)
                    return away ? LineupLayout.AwaySlot(0) : LineupLayout.HomeSlot(0);
                var draft = away ? s.Away : s.Home;
                var cap = away ? s.AwayCaptain : s.HomeCaptain;
                if (s.Focus is LineupFocus.HomeDiamond or LineupFocus.AwayDiamond)
                {
                    var pos = draft != null ? draft.PosOf(cap.Id) ?? "P" : "P";
                    return LineupLayout.DiamondHead(!away, pos);
                }
                var i = 0;
                if (draft != null)
                    for (; i < draft.Order.Count; i++)
                        if (draft.Order[i].Id == cap.Id) break;
                return away ? LineupLayout.AwayOrder(i) : LineupLayout.HomeOrder(i);
            }

            static LineupCell HighlightCell(LineupScreens s)
            {
                if (s.Step == LineupStep.TeamSetup)
                {
                    if (s.Focus == LineupFocus.Pool)
                        return LineupLayout.PoolCell(s.PoolIndex, Mathf.Max(1, s.Pool.Count));
                    if (s.Focus == LineupFocus.AwayRow)
                        return LineupLayout.AwaySlot(s.SlotIndex);
                    return LineupLayout.HomeSlot(s.SlotIndex);
                }
                var away = s.Focus is LineupFocus.AwayOrder or LineupFocus.AwayDiamond;
                if (s.Focus is LineupFocus.HomeDiamond or LineupFocus.AwayDiamond)
                    return LineupLayout.DiamondHead(!away, Diamond.Order[s.GloveIndex]);
                return away ? LineupLayout.AwayOrder(s.OrderIndex) : LineupLayout.HomeOrder(s.OrderIndex);
            }

            sealed class Tile
            {
                public GameObject Root;
                public GameObject Face;
                public TextMesh Mark;
                public Material Mat;
            }
        }
    }
}
