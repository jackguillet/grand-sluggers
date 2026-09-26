using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>One board owns both team cards, the shared portrait rail and its controller help.</summary>
    public static class CaptainSheet
    {
        static GUIStyle _title, _name, _body, _small, _tile, _badge, _tileBadge, _barLabel, _barValue;
        static readonly Color Ink = FrontBoardStyle.Ink;
        static readonly Color Panel = FrontBoardStyle.Panel;
        static readonly Color One = FrontBoardStyle.Gold;
        static readonly Color Two = FrontBoardStyle.Blue;
        public static void Draw(CaptainSelection selection, ContentCatalog content, bool pad2)
        {
            if (selection == null || content == null) return;
            Ensure();
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 800f, 1));
            Fill(new Rect(0, 0, 1280, 800), Ink);
            Text(new Rect(24, 20, 780, 45), CarnivalFront.CaptainTitle, _title);
            Text(new Rect(910, 28, 346, 28), CarnivalFront.SetupCaptainsStep, _small);
            for (var panel = 0; panel < 2; panel++)
                Card(selection, panel, content.Must(selection.Id(panel)), pad2, content.StarSkills);
            Text(new Rect(24, 510, 1232, 30), CarnivalFront.CaptainPrompt(selection, pad2), _body);
            for (var i = 0; i < selection.Count; i++)
            {
                var id = content.CaptainIds[i];
                var who = content.Must(id);
                var r = RectOf(CarnivalFront.CaptainTile(i, selection.Count));
                Fill(r, Panel);
                var p1 = selection.Id(0) == id;
                var p2 = selection.Id(1) == id;
                Portrait(who, new Rect(r.x + 6, r.y + 6, r.width - 12, 104));
                Text(new Rect(r.x + 4, r.y + 110, r.width - 8, 28), who.TileName, _tile);
                if (p1) Border(r, One, 3);
                if (p2) Border(new Rect(r.x + (p1 ? 5 : 0), r.y + (p1 ? 5 : 0), r.width - (p1 ? 10 : 0), r.height - (p1 ? 10 : 0)), Two, 3);
                if (p1 || p2)
                {
                    Fill(new Rect(r.x, r.y + 140, r.width, 25), p1 && !p2 ? One : Two);
                    Text(new Rect(r.x + 3, r.y + 139, r.width - 6, 26),
                        CarnivalFront.CaptainTileMark(selection, p1, p2), _tileBadge);
                }
            }
            Text(new Rect(24, 746, 1232, 34), CarnivalFront.CaptainControls, _body);
            GUI.matrix = old;
        }
        static void Card(CaptainSelection s, int panel, Character who, bool pad2, StarSkillTable skills)
        {
            var r = RectOf(CarnivalFront.CaptainPanel(panel));
            var accent = panel == 0 ? One : Two;
            var active = s.Versus ? !s.Ready(panel) : s.ActiveOne == panel;
            Fill(r, Panel);
            Border(r, active ? accent : new Color(.23f, .31f, .36f), active ? 3 : 1);
            Fill(new Rect(r.x, r.y, r.width, 44), accent);
            Text(new Rect(r.x + 18, r.y + 4, 280, 36), CarnivalFront.CaptainSeat(s, panel), _badge);
            Text(new Rect(r.x + 324, r.y + 4, 266, 36), CarnivalFront.CaptainStatus(s, panel, pad2), _tileBadge);
            Text(new Rect(r.x + 18, r.y + 58, r.width - 36, 39), who.Name.ToUpperInvariant(), _name);
            Portrait(who, new Rect(r.x + 18, r.y + 108, 274, 274));
            var card = CharacterCard.Of(who, skills: skills);
            Bars(r, card.Stats, accent);
            var verbs = r.y + CarnivalFront.CaptainCardVerbsTop;
            const float step = CarnivalFront.CaptainCardVerbPitch;
            Text(new Rect(r.x + 314, verbs, 276, 26), card.StarPitch, _body);
            Text(new Rect(r.x + 314, verbs + step, 276, 26), card.StarSwing, _body);
            Text(new Rect(r.x + 314, verbs + 2 * step, 276, 24), card.FieldVerb, _small);
            Text(new Rect(r.x + 314, verbs + 3 * step, 276, 24), HowToPlay.CardBatHand(card.Bats), _small);
        }
        // The four derived bars (StatBars), laid out by CarnivalFront.CaptainCardBars. Either seat draws the same rows.
        static void Bars(Rect r, Stats stats, Color accent)
        {
            var bars = CarnivalFront.CaptainCardBars;
            for (var i = 0; i < StatBars.Count; i++)
            {
                var y = r.y + bars.RowY(i);
                var barY = r.y + bars.BarY(i);
                Text(new Rect(r.x + bars.LabelX, y, bars.LabelW, bars.Pitch), StatBars.Labels[i], _barLabel);
                Fill(new Rect(r.x + bars.BarX, barY, bars.BarW, bars.BarH), new Color(.15f, .22f, .27f));
                Fill(new Rect(r.x + bars.BarX, barY, bars.BarW * (float)StatBars.Fill(stats, i), bars.BarH), accent);
                Text(new Rect(r.x + bars.ValueX, y, bars.ValueW, bars.Pitch), StatBars.ValueText(stats, i), _barValue);
            }
        }
        static Rect RectOf(CaptainPanelRect r) => new Rect(r.X, r.Y, r.W, r.H);
        static void Portrait(Character who, Rect r)
        {
            var texture = Look.Portrait(who);
            if (texture != null) GUI.DrawTexture(r, texture, ScaleMode.ScaleToFit);
        }
        static void Fill(Rect r, Color c)
        {
            var previous = GUI.color;
            GUI.color = QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;
            GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = previous;
        }
        static void Border(Rect r, Color c, float n)
        {
            Fill(new Rect(r.x, r.y, r.width, n), c); Fill(new Rect(r.x, r.yMax - n, r.width, n), c);
            Fill(new Rect(r.x, r.y, n, r.height), c); Fill(new Rect(r.xMax - n, r.y, n, r.height), c);
        }
        static void Text(Rect r, string value, GUIStyle style) => GUI.Label(Fit(r, value, style), value, style);
        // A row is at least as tall as its rendered text, grown about its own center, so no captain,
        // hand or seat copy is clipped by the nominal row height.
        static Rect Fit(Rect r, string value, GUIStyle style)
        {
            var h = Mathf.Ceil(style.CalcHeight(new GUIContent(value), r.width));
            return h <= r.height ? r : new Rect(r.x, r.center.y - h * .5f, r.width, h);
        }
        static void Ensure()
        {
            if (_title != null) return;
            _title = Style(34); _name = Style(30); _body = Style(21); _small = Style(17);
            _tile = Style(16); _tile.alignment = TextAnchor.MiddleCenter;
            _barLabel = Style(CarnivalFront.CaptainCardBars.LabelFont);
            _barValue = Style(CarnivalFront.CaptainCardBars.ValueFont); _barValue.alignment = TextAnchor.MiddleRight;
            _badge = Style(21); _badge.normal.textColor = Ink;
            _tileBadge = Style(16); _tileBadge.normal.textColor = Ink;
            _tileBadge.alignment = TextAnchor.MiddleCenter;
        }
        static GUIStyle Style(int size)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft, wordWrap = false };
            style.normal.textColor = Color.white; return style;
        }
    }
}
