using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public static class HudView
    {
        static GUIStyle _title, _h1, _body, _gold, _tiny, _stat, _score, _team, _bookTitle, _bookLine, _bookHead, _stamp;
        static Texture2D _panel, _ink, _starOn, _starOff, _dotOn, _dotOff, _outOn, _outOff, _bar, _white, _bookBack, _bookCard;
        static Texture2D _spark, _royal, _carnival, _goldrush, _canopy, _ember;

        public static void Draw(
            Match match, PhaseUi phase, string parkName, string homeCap, string awayCap,
            bool challenge, string[] pitches, int pitchIndex, bool star, bool steal, string item,
            float charge, float timing, bool showTiming, string banner, string sub, Texture2D portrait,
            bool training = false, string drillProgress = null, bool night = false,
            bool hideHelp = false, string highlight = null, bool replaying = false,
            bool mutePlay = false, int seats = 1,
            bool humanPitches = true, bool humanBats = false,
            bool starPitch = false, bool starSwing = false, bool pad1Home = true,
            bool bunt = false)
        {
            Ensure();
            if (phase == PhaseUi.Title)
            {
                Title(challenge, portrait, training, night, hideHelp);
                return;
            }
            if (phase == PhaseUi.Select)
            {
                Select(homeCap, awayCap, pad1Home, null, false, false);
                return;
            }
            if (phase == PhaseUi.Field)
            {
                Field(match != null ? match.Park.Id : "", parkName, night);
                return;
            }
            if (phase == PhaseUi.Lineup || phase == PhaseUi.TeamSetup || phase == PhaseUi.DefenseSetup)
            {
                Lineup(match);
                return;
            }
            if (phase == PhaseUi.GameOver)
            {
                if (replaying) Replay(match, highlight);
                else Final(match, highlight);
                return;
            }
            if (training)
            {
                TrainingPlay(banner, sub, drillProgress);
                return;
            }
            if (mutePlay) return;
            Play(match, pitches, pitchIndex, star, steal, item, charge, timing, showTiming, banner, sub, seats,
                humanPitches, humanBats, starPitch, starSwing, bunt);
        }

        static void Title(bool challenge, Texture2D portrait, bool training, bool night, bool hideHelp)
        {
            var w = Screen.width;
            Sticker(CarnivalFront.SkyGag(night), w - 168, 36, 140, 32, night ? _gold : _h1);
            var exhibition = !training && !challenge;
            if (exhibition)
                Sticker(CarnivalFront.PlayBall, 44, 88, 640, 28, _gold);
            else
                Sticker(training ? "TRAINING" : "CHALLENGE", 44, 88, 420, 32, _h1);
            if (training)
                GUI.Label(new Rect(44, 124, 640, 22), "Harbor  ·  stick lesson  ·  South start  ·  East skip to field", _tiny);
            else if (challenge)
                GUI.Label(new Rect(44, 124, 640, 22), "South / Space  ·  next match", _gold);
            _ = portrait;
            if (hideHelp) return;
            GUI.Label(new Rect(44, Screen.height - 48, w - 80, 22),
                $"South pick captain    West / F training    Esc how to play    Start / H mode    Tab innings    F6 input: {Controls.Player1InputLabel}", _tiny);
        }

        public static void Select(string homeId, string awayId, bool pad1Home, ContentCatalog content,
            bool versus = false, bool pad2 = false)
        {
            Ensure();
            DrawSeatModeTabs(versus);
            var yours = pad1Home ? homeId : awayId;
            var theirs = pad1Home ? awayId : homeId;
            if (content != null && content.Characters.TryGetValue(yours, out var youWho))
                Card(CharacterCard.Of(youWho), 36, 28);
            var vs = "vs  ";
            if (content != null && content.Characters.TryGetValue(theirs, out var themWho))
                vs += themWho.Name;
            Sticker(CarnivalFront.SeatMark(pad1Home) + "  " + vs, 36, 268, 480, 24, _gold);
            GUI.Label(new Rect(36, 300, 520, 22), CarnivalFront.SeatModeHint(versus, pad2, pad1Home), _tiny);
            GUI.Label(new Rect(44, Screen.height - 48, Screen.width - 80, 22),
                CarnivalFront.SelectHelp, _tiny);
        }

        static void DrawSeatModeTabs(bool versus)
        {
            DrawSeatModeTab(false, versus);
            DrawSeatModeTab(true, versus);
        }

        static void DrawSeatModeTab(bool two, bool versus)
        {
            var t = CarnivalFront.SeatModeTab(two, Screen.width, Screen.height);
            var r = new Rect(t.X, t.Y, t.W, t.H);
            var on = two == versus;
            GUI.DrawTexture(r, on ? _ink : _panel);
            GUI.Label(new Rect(r.x + 8, r.y + 6, r.width - 12, r.height - 8),
                CarnivalFront.SeatModeLabel(two), on ? _h1 : _body);
        }

        public static void Card(CharacterCard card, float x, float y)
        {
            Ensure();
            const float w = 312f;
            const float h = 232f;
            GUI.DrawTexture(new Rect(x, y, w, h), _panel);
            GUI.Label(new Rect(x + 18, y + 8, w - 56, 28), card.Name.ToUpperInvariant(), _h1);
            ChemPip(x + w - 34, y + 14, card.VsCaptain);
            StatRow(x + 14, y + 42, "PIT", card.Stats.Pitch);
            StatRow(x + 14, y + 64, "BAT", card.Stats.Bat);
            StatRow(x + 14, y + 86, "FLD", card.Stats.Field);
            StatRow(x + 14, y + 108, "RUN", card.Stats.Run);
            GUI.Label(new Rect(x + 14, y + 136, w - 28, 24), card.StarPitch, _body);
            GUI.Label(new Rect(x + 14, y + 160, w - 28, 24), card.StarSwing, _body);
            GUI.Label(new Rect(x + 14, y + 186, w - 28, 24), card.FieldVerb, _tiny);
        }

        static void StatRow(float x, float y, string label, int n)
        {
            GUI.Label(new Rect(x, y, 56, 20), label, _stat);
            n = Mathf.Clamp(n, 0, 10);
            for (var i = 0; i < 10; i++)
                GUI.DrawTexture(new Rect(x + 60 + i * 22, y + 3, 16, 14), i < n ? _bar : _dotOff);
        }

        static void ChemPip(float x, float y, Chemistry chem)
        {
            var prev = GUI.color;
            if (chem == Chemistry.Good) GUI.color = new Color(1f, 0.82f, 0.2f, 1f);
            else if (chem == Chemistry.Bad) GUI.color = new Color(0.92f, 0.28f, 0.22f, 1f);
            else GUI.color = new Color(1f, 1f, 1f, 0.28f);
            GUI.DrawTexture(new Rect(x, y, 18, 18), _dotOn != null ? _dotOn : _white);
            GUI.color = prev;
        }

        public static void Field(string parkId, string parkName, bool night)
        {
            Ensure();
            Sticker(parkName, 40, 28, 720, 48, _title);
            Sticker(CarnivalFront.SkyGag(night), 40, 78, 200, 28, night ? _gold : _h1);
            GUI.Label(new Rect(44, 112, 720, 26), CarnivalFront.Gimmick(parkId, night), _gold);
            if (!CarnivalFront.HarborIsTheProduct(parkId))
                GUI.Label(new Rect(44, 140, 720, 22), "Harbor is the slice.", _tiny);
            GUI.Label(new Rect(44, Screen.height - 48, Screen.width - 80, 22),
                "stick L/R the field    South lineup    West captains    N night    Esc how to play", _tiny);
        }

        static void Sticker(string text, float x, float y, float w, float h, GUIStyle style)
        {
            var old = GUI.color;
            GUI.color = new Color(0.06f, 0.04f, 0.08f, 0.88f);
            GUI.Label(new Rect(x + 3, y + 3, w, h), text, style);
            GUI.color = old;
            GUI.Label(new Rect(x, y, w, h), text, style);
        }

        /// <summary>End-of-play cartoon stamp. Only after the play is dead. Counts are smaller and quicker.</summary>
        public static void PlayStamp(string text, float t, float size = 1f, float popSeconds = 0.16f)
        {
            Ensure();
            if (string.IsNullOrEmpty(text)) return;
            var popDur = popSeconds > 0.01f ? popSeconds : 0.16f;
            var pop = Mathf.Clamp01(t / popDur);
            var peak = size < 1f ? 1.06f : 1.12f;
            var scale = Mathf.Lerp(0.35f, peak, pop);
            if (pop >= 1f)
                scale = size < 1f ? 1f : 1.06f + 0.04f * Mathf.Sin(t * 5.5f);
            scale *= size;
            var w = Screen.width;
            var h = Screen.height;
            var cx = w * 0.5f;
            var cy = h * 0.40f;
            var rw = w * (size < 1f ? 0.70f : 0.92f);
            var rh = h * (size < 1f ? 0.20f : 0.28f);
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(size < 1f ? -5f : -7f, new Vector2(cx, cy));
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), new Vector2(cx, cy));
            Sticker(text, cx - rw * 0.5f, cy - rh * 0.5f, rw, rh, _stamp);
            GUI.matrix = matrix;
        }

        public static void DeviceRecovery(LineupSeat missingSeat)
        {
            Ensure();
            var old = GUI.color;
            GUI.color = new Color(0.025f, 0.018f, 0.04f, 1f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _white);
            GUI.color = old;
            var w = Mathf.Min(760f, Screen.width - 72f);
            var h = missingSeat == LineupSeat.Pad1 ? 260f : 224f;
            var x = (Screen.width - w) * 0.5f;
            var y = (Screen.height - h) * 0.5f;
            GUI.DrawTexture(new Rect(x, y, w, h), _panel);
            GUI.Label(new Rect(x + 28, y + 22, w - 56, 42), SeatRecoveryCopy.Title(missingSeat), _h1);
            var lines = SeatRecoveryCopy.Lines(missingSeat);
            for (var i = 0; i < lines.Length; i++)
                GUI.Label(new Rect(x + 28, y + 82 + i * 36, w - 56, 32), lines[i], i == 0 ? _gold : _body);
        }

        public static void Pause(int item, bool howTo, int page)
        {
            Ensure();
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), howTo ? _bookBack : _panel);
            if (howTo)
            {
                Book(page);
                return;
            }
            var panel = PauseMenu.Panel(Screen.width, Screen.height);
            GUI.DrawTexture(new Rect(panel.X, panel.Y, panel.W, panel.H), _panel);
            GUI.Label(new Rect(panel.X + 24, panel.Y + 16, panel.W - 48, 32), "CALL TIME", _h1);
            for (var i = 0; i < PauseMenu.Items.Count; i++)
            {
                var label = PauseMenu.Label(PauseMenu.Items[i]);
                var ir = PauseMenu.ItemRect(i, Screen.width, Screen.height);
                var r = new Rect(ir.X, ir.Y, ir.W, ir.H);
                if (i == item)
                    GUI.DrawTexture(r, _ink);
                GUI.Label(r, label, i == item ? _h1 : _body);
            }
            var foot = PauseMenu.FooterRect(Screen.width, Screen.height);
            var lineH = foot.H / Mathf.Max(1, PauseMenu.FooterLines.Count);
            for (var i = 0; i < PauseMenu.FooterLines.Count; i++)
                GUI.Label(new Rect(foot.X, foot.Y + i * lineH, foot.W, lineH), PauseMenu.FooterLines[i], _tiny);
        }

        static void Book(int page)
        {
            var n = HowToPlay.Pages.Count;
            var p = HowToPlay.Pages[(page % n + n) % n];
            var scheme = BookScheme.Current;
            var book = HowToPlay.BookPanel(Screen.width, Screen.height);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _bookBack);
            GUI.DrawTexture(new Rect(book.X, book.Y, book.W, book.H), _bookBack);
            DrawChapterMascot(p.Id);
            GUI.Label(new Rect(book.X + 88, book.Y + 10, 280, 28), "HOW TO PLAY", _gold);
            GUI.Label(new Rect(book.X + 88, book.Y + 38, book.W - 440, 44),
                p.Title.ToUpperInvariant() + "   " + (page + 1) + " / " + n, _bookHead);
            DrawSchemeToggle(scheme);
            DrawSchemeBadges(p, scheme);
            if (p.Id == "contents")
                DrawContentsToc(p);
            else if (p.Id == "getting-started")
                DrawGettingStarted(scheme, p);
            else if (p.Id == "controls")
                DrawHardware(scheme);
            else if (p.Id == "roles")
                DrawRoleTables(scheme);
            else if (p.Id == "pitch-swing")
                DrawHowToComics(scheme, p);
            else if (p.Id == "running")
                DrawBagDiagrams(scheme, p);
            else if (p.Id == "screen")
                DrawHudCallouts(p);
            else if (p.Id == "chemistry")
                DrawChemBook(p);
            else if (p.Id == "abilities")
                DrawAbilityBook(scheme, p);
            else
            {
                var text = HowToPlay.TextRect(Screen.width, Screen.height);
                var lines = p.Shown(scheme);
                var lineH = HowToPlay.KidLineH;
                for (var i = 0; i < lines.Count; i++)
                    Sticker(lines[i], text.X, text.Y + i * lineH, text.W, lineH + 10f, _bookLine);
            }
            GUI.Label(new Rect(book.X + 28, book.Y + book.H - 44, book.W - 56, 40),
                BookScheme.Footer(scheme), _bookLine);
        }

        static void DrawSchemeToggle(InputScheme scheme)
        {
            DrawTab(InputScheme.Pad, scheme);
            DrawTab(InputScheme.Keys, scheme);
        }

        static void DrawChapterMascot(string pageId)
        {
            var book = HowToPlay.BookPanel(Screen.width, Screen.height);
            var r = new Rect(book.X + 16, book.Y + 10, 56, 56);
            var tex = Look.Portrait(BookChapter.Captain(pageId));
            GUI.DrawTexture(r, _ink);
            if (tex != null)
                GUI.DrawTexture(new Rect(r.x + 4, r.y + 4, r.width - 8, r.height - 8), tex, ScaleMode.ScaleAndCrop);
        }

        static void DrawSchemeBadges(HowToPlay.Page page, InputScheme scheme)
        {
            var book = HowToPlay.BookPanel(Screen.width, Screen.height);
            var x = book.X + 220;
            var y = book.Y + 12;
            void Pill(string label)
            {
                var w = Mathf.Max(88f, 16f + label.Length * 7.2f);
                var r = new Rect(x, y, w, 22);
                GUI.DrawTexture(r, _ink);
                GUI.Label(new Rect(r.x + 8, r.y + 2, r.width - 12, r.height - 2), label, _gold);
                x += w + 8;
            }
            var seat = BookScheme.SeatBadge(scheme);
            if (!string.IsNullOrEmpty(seat)) Pill(seat);
            var pageBadge = BookScheme.PageBadge(page.Id, scheme);
            if (!string.IsNullOrEmpty(pageBadge)) Pill(pageBadge);
        }

        static void DrawTab(InputScheme kind, InputScheme current)
        {
            var t = BookScheme.Tab(kind, Screen.width, Screen.height);
            var r = new Rect(t.X, t.Y, t.W, t.H);
            var on = kind == current;
            GUI.DrawTexture(r, on ? _ink : _bookCard);
            GUI.Label(new Rect(r.x + 8, r.y + 6, r.width - 12, r.height - 8), BookScheme.Label(kind), on ? _h1 : _bookLine);
        }

        static void DrawHardware(InputScheme scheme)
        {
            var b = ControlDiagram.Board(Screen.width, Screen.height);
            var prev = GUI.color;
            GUI.color = new Color(0.22f, 0.78f, 0.38f, 1f);
            GUI.DrawTexture(new Rect(b.X, b.Y, 22, 22), _dotOn);
            GUI.color = prev;
            GUI.Label(new Rect(b.X + 30, b.Y - 4, 180, 32), BookScheme.OffenseLabel, _bookLine);
            GUI.color = new Color(0.92f, 0.28f, 0.22f, 1f);
            GUI.DrawTexture(new Rect(b.X + 220, b.Y, 22, 22), _dotOn);
            GUI.color = prev;
            GUI.Label(new Rect(b.X + 250, b.Y - 4, 180, 32), BookScheme.DefenseLabel, _bookLine);

            var calls = ControlDiagram.Callouts(scheme);
            for (var i = 0; i < calls.Count; i++)
            {
                var c = calls[i];
                var cell = ControlDiagram.CalloutCell(i, scheme, Screen.width, Screen.height);
                var r = new Rect(cell.X, cell.Y + 4, cell.W, cell.H - 8);
                GUI.DrawTexture(r, _bookCard);
                Sticker(c.Hardware, r.x + 12, r.y + 6, r.width - 24, 34, _bookHead);
                var y = r.y + 42;
                if (c.Always.Length > 0)
                {
                    Sticker(c.Always, r.x + 12, y, r.width - 24, 30, _bookLine);
                    y += 30;
                }
                if (c.Offense.Length > 0)
                {
                    prev = GUI.color;
                    GUI.color = new Color(0.45f, 0.95f, 0.55f, 1f);
                    GUI.Label(new Rect(r.x + 12, y, r.width - 24, 28), c.Offense, _bookLine);
                    GUI.color = prev;
                    y += 28;
                }
                if (c.Defense.Length > 0)
                {
                    prev = GUI.color;
                    GUI.color = new Color(1f, 0.55f, 0.45f, 1f);
                    GUI.Label(new Rect(r.x + 12, y, r.width - 24, 28), c.Defense, _bookLine);
                    GUI.color = prev;
                }
            }
        }

        static void DrawBagDiagrams(InputScheme scheme, HowToPlay.Page page)
        {
            _ = page;
            for (var i = 0; i < BagDiagrams.Running.Count; i++)
            {
                var diagram = BagDiagrams.Running[i];
                var cell = BagDiagrams.Card(i, Screen.width, Screen.height);
                var r = new Rect(cell.X, cell.Y, cell.W, cell.H);
                GUI.DrawTexture(r, _bookCard);
                GUI.Label(new Rect(r.x + 10, r.y + 8, r.width - 20, 36), diagram.Title.ToUpperInvariant(), _bookHead);

                var press = BagDiagrams.Press(diagram, scheme);
                GUI.DrawTexture(new Rect(r.x + 10, r.y + 46, r.width - 20, 36), _ink);
                GUI.Label(new Rect(r.x + 16, r.y + 48, r.width - 32, 32), press, _bookLine);

                var size = Mathf.Min(r.width * 0.76f, r.height * 0.50f);
                var x = r.x + (r.width - size) * 0.5f;
                var y = r.y + 76f;
                DrawBagDiamond(x, y, size, diagram);
                GUI.Label(new Rect(r.x + 10, r.y + r.height - 44, r.width - 20, 36),
                    BagDiagramCaption(diagram.Kind), _bookLine);
            }

            for (var i = 0; i < BagDiagrams.Callouts.Count; i++)
            {
                var call = BagDiagrams.Callouts[i];
                var cell = BagDiagrams.CalloutCard(i, Screen.width, Screen.height);
                var box = new Rect(cell.X, cell.Y, cell.W, cell.H);
                GUI.DrawTexture(box, _bookCard);
                GUI.Label(new Rect(box.x + 10, box.y + 6, box.width - 20, 36), call.Title.ToUpperInvariant(), _bookHead);
                GUI.Label(new Rect(box.x + 14, box.y + 44, box.width - 28, 32),
                    BagDiagrams.CalloutPress(call, scheme), _gold);
                GUI.Label(new Rect(box.x + 10, box.y + 80, box.width - 20, box.height - 88), call.Line, _bookLine);
            }
        }

        static string BagDiagramCaption(BagDiagrams.Kind kind) => kind switch
        {
            BagDiagrams.Kind.BagMap => "PICK A RUNNER · ARM A THROW",
            BagDiagrams.Kind.Advance => "EVERY RUNNER GOES FOR THE NEXT BAG",
            BagDiagrams.Kind.Return => "EVERY RUNNER COMES BACK ONE BAG",
            _ => ""
        };

        static void DrawBagDiamond(float x, float y, float size, BagDiagrams.Diagram diagram)
        {
            for (var bag = 1; bag <= 4; bag++)
            {
                var point = BagDiagramPoint(x, y, size, bag);
                DrawBagBase(point, BagDiagrams.BagName(bag));
            }

            if (diagram.Kind == BagDiagrams.Kind.BagMap)
            {
                DrawBagDirection(BagDiagramPoint(x, y, size, 1), 1, new Vector2(12, -10));
                DrawBagDirection(BagDiagramPoint(x, y, size, 2), 2, new Vector2(-24, -34));
                DrawBagDirection(BagDiagramPoint(x, y, size, 3), 3, new Vector2(-54, -10));
                DrawBagDirection(BagDiagramPoint(x, y, size, 4), 4, new Vector2(-30, 14));
                return;
            }

            var color = diagram.Kind == BagDiagrams.Kind.Advance
                ? new Color(0.30f, 0.92f, 0.48f, 1f)
                : new Color(1f, 0.62f, 0.22f, 1f);
            foreach (var route in diagram.Routes)
                DrawBagRoute(BagDiagramPoint(x, y, size, route.FromBag), BagDiagramPoint(x, y, size, route.ToBag), color);
        }

        static Vector2 BagDiagramPoint(float x, float y, float size, int bag)
        {
            var uv = BagDiagrams.Pip(bag);
            return new Vector2(x + (float)(uv.U * size), y + size - (float)(uv.V * size));
        }

        static void DrawBagBase(Vector2 point, string label)
        {
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, point);
            var prev = GUI.color;
            GUI.color = new Color(0.95f, 0.91f, 0.72f, 1f);
            GUI.DrawTexture(new Rect(point.x - 10, point.y - 10, 20, 20), _white);
            GUI.color = prev;
            GUI.matrix = matrix;
            GUI.Label(new Rect(point.x - 20, point.y - 8, 40, 16), label, _tiny);
        }

        static void DrawBagDirection(Vector2 point, int bag, Vector2 offset)
        {
            var mark = bag switch
            {
                1 => "→ 1B",
                2 => "↑ 2B",
                3 => "3B ←",
                4 => "↓ HOME",
                _ => ""
            };
            GUI.Label(new Rect(point.x + offset.x, point.y + offset.y, 62, 20), mark, _gold);
        }

        static void DrawBagRoute(Vector2 from, Vector2 to, Color color)
        {
            var prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(from.x - 8, from.y - 8, 16, 16), _dotOn);
            GUI.color = prev;

            var mid = (from + to) * 0.5f;
            var dx = to.x - from.x;
            var dy = to.y - from.y;
            var arrow = dx > 12f
                ? (dy > 12f ? "↘" : dy < -12f ? "↗" : "→")
                : dx < -12f
                    ? (dy > 12f ? "↙" : dy < -12f ? "↖" : "←")
                    : dy > 0f ? "↓" : "↑";
            GUI.color = color;
            GUI.Label(new Rect(mid.x - 14, mid.y - 14, 28, 28), arrow, _h1);
            GUI.color = prev;
        }

        static void DrawGettingStarted(InputScheme scheme, HowToPlay.Page page)
        {
            for (var i = 0; i < GettingStarted.Path.Count; i++)
            {
                var step = GettingStarted.Path[i];
                var cell = GettingStarted.StepCell(i, Screen.width, Screen.height);
                var r = new Rect(cell.X, cell.Y, cell.W, cell.H);
                GUI.DrawTexture(r, _bookCard);
                GUI.Label(new Rect(r.x + 8, r.y + 8, 36, 36), (i + 1).ToString(), _bookHead);
                GUI.Label(new Rect(r.x + 44, r.y + 8, r.width - 52, 32), step.Title.ToUpperInvariant(), _bookHead);
                GUI.Label(new Rect(r.x + 12, r.y + 48, r.width - 24, r.height - 56),
                    GettingStarted.Caption(step, scheme), _bookLine);
            }
            for (var i = 0; i < GettingStarted.Modes.Count; i++)
            {
                var mode = GettingStarted.Modes[i];
                var row = GettingStarted.ModeRow(i, Screen.width, Screen.height);
                var head = new Rect(row.X, row.Y + 2, 168, row.H - 4);
                var prev = GUI.color;
                GUI.color = new Color(0.22f, 0.62f, 0.32f, 1f);
                GUI.DrawTexture(head, _white);
                GUI.color = prev;
                GUI.Label(new Rect(head.x + 8, head.y + 4, head.width - 12, head.height - 8), mode.Title, _h1);
                GUI.Label(new Rect(row.X + 180, row.Y + 4, row.W - 188, row.H - 8),
                    GettingStarted.Line(mode, scheme), _bookLine);
            }
            DrawBookLines(page, GettingStarted.LineBand(Screen.width, Screen.height));
        }

        static void DrawContentsToc(HowToPlay.Page page)
        {
            var card = ContentsToc.Card(Screen.width, Screen.height);
            GUI.DrawTexture(new Rect(card.X, card.Y, card.W, card.H), _bookCard);
            for (var i = 0; i < ContentsToc.Chapters.Count; i++)
            {
                var chapter = ContentsToc.Chapters[i];
                var row = ContentsToc.Row(i, Screen.width, Screen.height);
                GUI.Label(new Rect(row.X, row.Y, row.W - 56, row.H), chapter.Title, _bookLine);
                GUI.Label(new Rect(row.X + row.W - 56, row.Y, 52, row.H), chapter.Number.ToString(), _bookHead);
            }
            DrawBookLines(page, ContentsToc.LineBand(Screen.width, Screen.height));
        }

        static void DrawChemBook(HowToPlay.Page page)
        {
            for (var i = 0; i < ChemBook.ChemistryPairs.Count; i++)
            {
                var pair = ChemBook.ChemistryPairs[i];
                var cell = ChemBook.ChemCell(i, Screen.width, Screen.height);
                var r = new Rect(cell.X, cell.Y, cell.W, cell.H);
                GUI.DrawTexture(r, _bookCard);
                GUI.Label(new Rect(r.x + 16, r.y + 16, r.width - 56, 36), pair.Title.ToUpperInvariant(), _bookHead);
                ChemPip(r.x + r.width - 40, r.y + 20, pair.Chem);
                GUI.Label(new Rect(r.x + 16, r.y + 60, r.width - 32, r.height - 76), pair.Caption, _bookLine);
            }
            DrawBookLines(page, ChemBook.LineBand(Screen.width, Screen.height));
        }

        static void DrawAbilityBook(InputScheme scheme, HowToPlay.Page page)
        {
            var stillCell = ChemBook.AbilityStill(Screen.width, Screen.height);
            var tableCell = ChemBook.TypeTable(Screen.width, Screen.height);
            var still = new Rect(stillCell.X, stillCell.Y, stillCell.W, stillCell.H);
            GUI.DrawTexture(still, _bookCard);
            GUI.Label(new Rect(still.x + 12, still.y + 10, still.width - 24, 36), "THE CARD", _bookHead);
            for (var i = 0; i < ChemBook.CardStats.Count; i++)
            {
                var y = still.y + 52 + i * 40;
                Sticker(ChemBook.CardStats[i], still.x + 16, y, still.width - 32, 36, _bookLine);
            }
            var table = new Rect(tableCell.X, tableCell.Y, tableCell.W, tableCell.H);
            GUI.DrawTexture(table, _bookCard);
            GUI.Label(new Rect(table.x + 10, table.y + 8, table.width - 20, 36), "SPECIAL ABILITY TYPES", _bookHead);
            var rowH = (table.height - 44) / ChemBook.Types.Count;
            for (var i = 0; i < ChemBook.Types.Count; i++)
            {
                var row = ChemBook.Types[i];
                var y = table.y + 40 + i * rowH;
                var head = new Rect(table.x + 10, y, table.width - 20, 24);
                var prev = GUI.color;
                GUI.color = new Color(0.22f, 0.62f, 0.32f, 1f);
                GUI.DrawTexture(head, _white);
                GUI.color = prev;
                GUI.Label(new Rect(head.x + 8, head.y + 2, head.width - 16, 28), row.Title, _bookHead);
                GUI.Label(new Rect(table.x + 18, y + 34, table.width - 36, rowH - 40), row.Line, _bookLine);
            }
            DrawBookLines(page, ChemBook.LineBand(Screen.width, Screen.height));
            _ = scheme;
        }

        static void DrawBookLines(HowToPlay.Page page, (float X, float Y, float W, float H) band)
        {
            var lines = page.Shown(BookScheme.Current);
            var lineH = HowToPlay.KidLineH;
            for (var i = 0; i < lines.Count; i++)
                Sticker(lines[i], band.X, band.Y + i * lineH, band.W, lineH, _bookLine);
        }

        static void DrawHudCallouts(HowToPlay.Page page)
        {
            var spreads = HudCallouts.OnScreenPage;
            for (var i = 0; i < spreads.Count; i++)
            {
                var spread = spreads[i];
                var row = HudCallouts.Row(i, Screen.width, Screen.height);
                var r = new Rect(row.X, row.Y, row.W, row.H);
                GUI.DrawTexture(r, _bookCard);
                GUI.Label(new Rect(r.x + 12, r.y + 8, 280, 32), spread.Title.ToUpperInvariant(), _bookHead);
                for (var m = 0; m < spread.Marks.Count; m++)
                {
                    var mark = spread.Marks[m];
                    Sticker("•  " + mark.Label, r.x + 16, r.y + 48 + m * 36, r.width - 32, 34, _bookLine);
                }
            }
            DrawBookLines(page, HudCallouts.LineBand(Screen.width, Screen.height));
        }

        static void DrawHowToComics(InputScheme scheme, HowToPlay.Page page)
        {
            var strips = HowToComic.OnPitchSwingPage;
            for (var i = 0; i < strips.Count; i++)
            {
                var strip = strips[i];
                var row = HowToComic.Row(i, Screen.width, Screen.height);
                var r = new Rect(row.X, row.Y, row.W, row.H);
                GUI.DrawTexture(r, _bookCard);
                GUI.Label(new Rect(r.x + 16, r.y + 10, r.width - 32, 40), strip.Title.ToUpperInvariant(), _bookHead);
                var motion = HowToComic.MotionOf(strip, scheme);
                var chipH = 56f;
                var chipW = r.width * 0.38f;
                var y = r.y + 58;
                DrawMotionChip(new Rect(r.x + 16, y, chipW, chipH), motion.Charge);
                GUI.Label(new Rect(r.x + 16 + chipW, y, 48, chipH), "→", _bookHead);
                DrawMotionChip(new Rect(r.x + r.width - 16 - chipW, y, chipW, chipH), motion.Commit);
                GUI.Label(new Rect(r.x + 16, y + chipH + 12, r.width - 32, r.height - chipH - 80),
                    HowToComic.Caption(strip, scheme), _bookLine);
            }
            DrawBookLines(page, HowToComic.LineBand(Screen.width, Screen.height));
        }

        static void DrawMotionChip(Rect r, string label)
        {
            GUI.DrawTexture(r, _ink);
            GUI.Label(new Rect(r.x + 10, r.y + 8, r.width - 20, r.height - 12), label, _bookHead);
        }

        static void DrawRoleTables(InputScheme scheme)
        {
            var blocks = RoleTables.Of(scheme);
            for (var i = 0; i < blocks.Count; i++)
            {
                var cell = RoleTables.Cell(i, Screen.width, Screen.height);
                var r = new Rect(cell.X, cell.Y, cell.W, cell.H);
                GUI.DrawTexture(r, _bookCard);
                var head = new Rect(r.x, r.y, r.width, 44);
                var prev = GUI.color;
                GUI.color = new Color(0.22f, 0.62f, 0.32f, 1f);
                GUI.DrawTexture(head, _white);
                GUI.color = prev;
                GUI.Label(new Rect(head.x + 12, head.y + 4, head.width - 20, 36), blocks[i].Title, _bookHead);
                var rows = blocks[i].Rows;
                var rowH = Mathf.Max(40f, (r.height - 52) / Mathf.Max(1, rows.Count));
                for (var n = 0; n < rows.Count; n++)
                {
                    var y = r.y + 48 + n * rowH;
                    prev = GUI.color;
                    GUI.color = new Color(1f, 0.82f, 0.25f, 1f);
                    GUI.Label(new Rect(r.x + 12, y, r.width * 0.40f, rowH), rows[n].Verb, _bookLine);
                    GUI.color = prev;
                    GUI.Label(new Rect(r.x + r.width * 0.42f, y, r.width * 0.56f, rowH), rows[n].Press, _bookLine);
                }
            }
        }

        static readonly System.Collections.Generic.Dictionary<string, Texture2D> _bookPics = new();

        static Texture2D BookPic(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_bookPics.TryGetValue(id, out var cached) && cached != null) return cached;
            var tex = Resources.Load<Texture2D>("Art/Booklet/" + id);
            if (tex != null) _bookPics[id] = tex;
            return tex;
        }

        public static void ControlDisplay(string pos, string name, bool jump = false, bool dive = false)
        {
            var label = BroadcastHud.ControlDisplay(true, pos, name, jump, dive);
            if (string.IsNullOrEmpty(label)) return;
            Ensure();
            var r = Px(BroadcastHud.YouTell);
            GUI.DrawTexture(r, _panel);
            GUI.Label(new Rect(r.x + 12, r.y + 4, r.width - 16, r.height - 6), label, _gold);
        }

        public static void SwitchTell(string current, string hint, string hintName, bool hasBall)
        {
            var label = BroadcastHud.SwitchTell(current, hint, hintName, hasBall);
            if (string.IsNullOrEmpty(label)) return;
            Ensure();
            GUI.DrawTexture(new Rect(36, Screen.height - 160, 280, 36), _panel);
            GUI.Label(new Rect(48, Screen.height - 156, 256, 28), label, _gold);
        }

        public static void ItemPointer(string targetName)
        {
            var label = BroadcastHud.ItemPointer(true, targetName);
            if (string.IsNullOrEmpty(label)) return;
            Ensure();
            var r = Px(BroadcastHud.ItemTell);
            GUI.DrawTexture(r, _panel);
            GUI.Label(new Rect(r.x + 12, r.y + 4, r.width - 16, r.height - 6), label, _gold);
        }

        public static void ClosePlay(int bag, bool icon)
        {
            Ensure();
            var w = 520f;
            var h = 92f;
            var x = Screen.width * 0.5f - w * 0.5f;
            var y = Screen.height * 0.38f;
            GUI.DrawTexture(new Rect(x, y, w, h), _panel);
            var bagName = bag == 4 ? "HOME" : "3B";
            GUI.Label(new Rect(x + 20, y + 14, w - 40, 32), "CLOSE PLAY  ·  " + bagName, _h1);
            GUI.Label(new Rect(x + 20, y + 50, w - 40, 28),
                icon ? "PRESS SOUTH  ·  Space / left click  ·  first wins" : "Get ready…", _gold);
        }

        public static void BagTell(int bag)
        {
            Ensure();
            const float size = 88f;
            var x = Screen.width * 0.5f - size * 0.5f;
            var y = Screen.height - 168f;
            GUI.Label(new Rect(x - 40, y - 22, size + 80, 20), "throw", _tiny);
            for (var i = 1; i <= 4; i++)
            {
                var uv = FieldAssist.BagPip(i);
                var px = x + (float)(uv.U * size);
                var py = y + size - (float)(uv.V * size);
                var r = new Rect(px - 10, py - 10, 20, 20);
                GUI.DrawTexture(r, i == bag ? _outOn : _outOff);
            }
        }

        static void TrainingPlay(string banner, string sub, string progress)
        {
            var w = 560;
            GUI.DrawTexture(new Rect(Screen.width / 2 - w / 2, 24, w, 86), _panel);
            GUI.Label(new Rect(Screen.width / 2 - w / 2 + 16, 32, w - 32, 40), banner ?? "", _h1);
            if (!string.IsNullOrEmpty(progress))
                GUI.Label(new Rect(Screen.width / 2 - w / 2 + 16, 72, w - 32, 24), progress, _tiny);
            if (!string.IsNullOrEmpty(sub))
                GUI.Label(new Rect(48, Screen.height - 52, Screen.width - 96, 28), sub, _gold);
        }

        static void Lineup(Match match)
        {
            _ = match;
        }

        static void Replay(Match match, string highlight)
        {
            GUI.DrawTexture(new Rect(40, 36, 520, 92), _panel);
            GUI.Label(new Rect(56, 44, 500, 24), "HIGHLIGHT", _gold);
            GUI.Label(new Rect(56, 70, 490, 40), string.IsNullOrEmpty(highlight) ? "The play of the game." : highlight, _h1);
            GUI.Label(new Rect(56, 140, 400, 22), Short(match.Away) + " " + match.AwayScore + "   " + Short(match.Home) + " " + match.HomeScore, _body);
        }

        static void Final(Match match, string highlight)
        {
            var mvp = match.Mvp();
            GUI.DrawTexture(new Rect(48, 48, 640, 320), _panel);
            GUI.Label(new Rect(68, 62, 400, 28), "FINAL", _gold);
            GUI.Label(new Rect(68, 100, 600, 40), match.Away.Name + "  " + match.AwayScore, _h1);
            GUI.Label(new Rect(68, 148, 600, 40), match.Home.Name + "  " + match.HomeScore, _h1);
            if (!string.IsNullOrEmpty(highlight))
            {
                GUI.Label(new Rect(68, 200, 600, 22), "HIGHLIGHT", _tiny);
                GUI.Label(new Rect(68, 222, 600, 24), highlight, _gold);
            }
            GUI.Label(new Rect(68, 258, 600, 28), "MVP  " + mvp.Who.Name, _gold);
            GUI.Label(new Rect(68, 290, 600, 22), mvp.Why, _body);
            GUI.Label(new Rect(68, 330, 600, 22), "SPACE  continue", _tiny);
        }

        static void Play(Match match, string[] pitches, int pi, bool star, bool steal, string item,
            float charge, float timing, bool showTiming, string banner, string sub, int seats,
            bool humanPitches, bool humanBats, bool starPitch, bool starSwing, bool bunt)
        {
            var lay = BroadcastHud.Layout(seats);
            Scorebug(match, lay);
            Cards(match, pitches, pi, star, steal, item, charge, timing, showTiming, lay,
                humanPitches, humanBats, starPitch, starSwing, bunt);

            if (!string.IsNullOrEmpty(banner))
            {
                var r = Px(lay.Banner);
                GUI.DrawTexture(r, _panel);
                GUI.Label(new Rect(r.x + 12, r.y + 6, r.width - 24, 28), banner, _h1);
                if (!string.IsNullOrEmpty(sub))
                    GUI.Label(new Rect(r.x + 12, r.y + 34, r.width - 24, 22), sub, _gold);
            }
            else if (!string.IsNullOrEmpty(sub))
            {
                var r = Px(lay.Banner);
                GUI.Label(new Rect(r.x + 12, r.y + 8, r.width - 24, 22), sub, _tiny);
            }
        }

        static Rect Px(BroadcastHud.HudRect r)
        {
            var p = r.Pixel(Screen.width, Screen.height);
            return new Rect((float)p.X, (float)p.Y, (float)p.W, (float)p.H);
        }

        static void Scorebug(Match match, BroadcastHud.PlayLayout lay)
        {
            var bug = BroadcastHud.From(match);
            var r = Px(lay.Score);
            GUI.DrawTexture(r, _panel);
            GUI.DrawTexture(new Rect(r.x, r.y, 6, r.height), _ink);
            var half = bug.Over ? "FINAL" : (bug.Top ? "TOP" : "BOT");
            GUI.Label(Px(BroadcastHud.InningMark(lay.Score)), half, _gold);
            var innings = Mathf.Max(1, bug.Innings);
            for (var i = 1; i <= innings; i++)
            {
                var box = Px(BroadcastHud.InningBox(lay.Score, i, innings));
                var prev = GUI.color;
                GUI.color = i == bug.Inning
                    ? new Color(0.12f, 0.10f, 0.06f, 1f)
                    : new Color(1f, 1f, 1f, 0.14f);
                GUI.DrawTexture(box, _white);
                GUI.color = prev;
                GUI.Label(box, i.ToString(), i == bug.Inning ? _gold : _tiny);
            }
            Row(lay.Score, 0, match.Away, bug.AwayScore, match.AwayStars, AwayStripe(match));
            Row(lay.Score, 1, match.Home, bug.HomeScore, match.HomeStars, HomeStripe(match));

            var c = Px(lay.Count);
            CountLine(c, 0, bug.Balls, 4, "B", _dotOn, _dotOff);
            CountLine(c, 1, bug.Strikes, 3, "S", _dotOn, _dotOff);
            CountLine(c, 2, bug.Outs, 3, "O", _outOn, _outOff);
            MiniDiamond(Px(lay.MiniDiamond), bug);
        }

        static void MiniDiamond(Rect r, BroadcastHud.Scorebug bug)
        {
            var size = Mathf.Min(r.width, r.height) * 0.72f;
            var x = r.x + (r.width - size) * 0.5f;
            var y = r.y + (r.height - size) * 0.15f;
            BagPip(x, y, size, 1, bug.RunnerFirst, bug.LeadFirst, bug.SelectedBag);
            BagPip(x, y, size, 2, bug.RunnerSecond, bug.LeadSecond, bug.SelectedBag);
            BagPip(x, y, size, 3, bug.RunnerThird, bug.LeadThird, bug.SelectedBag);
        }

        static void BagPip(float x, float y, float size, int bag, bool on, double lead, int selected)
        {
            var uv = Baserunning.MiniLead(bag, on ? lead : 0);
            var px = x + (float)(uv.U * size);
            var py = y + size - (float)(uv.V * size);
            var pip = on && bag == selected ? 16f : 14f;
            var tex = !on ? _outOff : bag == selected ? _ink : _outOn;
            GUI.DrawTexture(new Rect(px - pip * 0.5f, py - pip * 0.5f, pip, pip), tex);
        }

        static void Row(BroadcastHud.HudRect score, int row, Team team, int runs, double stars, Texture2D stripe)
        {
            var stripeR = Px(BroadcastHud.StripeCol(score, row));
            var nameR = Px(BroadcastHud.NameCol(score, row));
            var runR = Px(BroadcastHud.RunsCol(score, row));
            var starR = Px(BroadcastHud.StarsCol(score, row));
            GUI.DrawTexture(stripeR, stripe);
            GUI.Label(nameR, BroadcastHud.BugName(team.Captain.Name), _team);
            GUI.Label(runR, BroadcastHud.RunsLabel(runs), _score);
            Stars(starR.x, starR.y, stars);
        }

        static void Cards(Match match, string[] pitches, int pi, bool star, bool steal, string item,
            float charge, float timing, bool showTiming, BroadcastHud.PlayLayout lay,
            bool humanPitches, bool humanBats, bool starPitch, bool starSwing, bool bunt)
        {
            var bug = BroadcastHud.From(match);
            var pStar = starPitch || (star && humanPitches);
            var bStar = starSwing || (star && humanBats);
            SeatCard(Px(lay.BatterCard), "AB", bug.Batter, humanBats,
                "NEXT  " + bug.Next,
                BroadcastHud.BatterExtra(bStar, steal, match.CanSteal, bunt, item),
                Look.Portrait(match.Batter));
            SeatCard(Px(lay.PitcherCard), "P", bug.Pitcher, humanPitches,
                BroadcastHud.ArmLine(match.PitcherStamina),
                (pitches != null && pi >= 0 && pi < pitches.Length ? pitches[pi].ToUpperInvariant() : "")
                    + (pStar ? "  STAR" : "")
                    + (BroadcastHud.PoorArm(match.PitcherStamina) ? "  SWEAT" : ""),
                Look.Portrait(match.Pitcher));
            Bar(Px(lay.PitcherCard).x + 16, Px(lay.PitcherCard).y + Px(lay.PitcherCard).height - 22,
                Px(lay.PitcherCard).width - 32, match.PitcherStamina / 100f);
            if (!showTiming) return;
            var box = humanPitches ? Px(lay.PitcherCard) : Px(lay.BatterCard);
            GUI.DrawTexture(new Rect(box.x + 16, box.y + box.height - 12, 160, 6), _dotOff);
            var pip = box.x + 16 + Mathf.Clamp01(timing) * 160f;
            GUI.DrawTexture(new Rect(pip - 2, box.y + box.height - 16, 4, 14), _white);
            GUI.DrawTexture(new Rect(box.x + 186, box.y + box.height - 12, Mathf.Clamp01(charge) * 80f, 6), _starOn);
        }

        static void SeatCard(Rect r, string role, string name, bool you, string line2, string extra, Texture2D face)
        {
            if (you)
            {
                var pad = Mathf.Min(3f, r.height * 0.02f);
                var edge = new Rect(r.x - pad, r.y - pad, r.width + pad * 2, r.height + pad * 2);
                GUI.DrawTexture(edge, _ink);
            }
            GUI.DrawTexture(r, _panel);
            var x = r.x + r.width * 0.05f;
            var faceSize = Mathf.Min(44f, r.height * 0.28f);
            if (face != null)
            {
                GUI.DrawTexture(new Rect(x, r.y + r.height * 0.08f, faceSize, faceSize), face, ScaleMode.ScaleToFit);
                x += faceSize + r.width * 0.04f;
            }
            var textW = r.x + r.width - x - r.width * 0.04f;
            var nameH = r.height * 0.22f;
            GUI.Label(new Rect(x, r.y + r.height * 0.07f, textW, nameH), role + "  " + name, you ? _h1 : _body);
            GUI.Label(new Rect(x, r.y + r.height * 0.32f, textW, r.height * 0.18f), line2, _tiny);
            if (!string.IsNullOrWhiteSpace(extra))
                GUI.Label(new Rect(x, r.y + r.height * 0.50f, textW, r.height * 0.18f), extra.Trim(), _gold);
        }

        static void Stars(float x, float y, double n)
        {
            for (var i = 0; i < 5; i++)
                GUI.DrawTexture(new Rect(x + i * 16, y, 14, 14), n > i ? _starOn : _starOff);
        }

        static void CountLine(Rect r, int row, int n, int max, string tag, Texture2D on, Texture2D off)
        {
            var h = r.height / 3f;
            var y = r.y + row * h + h * 0.12f;
            var pip = Mathf.Min(14f, h * 0.72f);
            var gap = pip * 0.18f;
            var tagW = Mathf.Min(18f, r.width * 0.16f);
            var x0 = r.x + 4f;
            for (var i = 0; i < max; i++)
                GUI.DrawTexture(new Rect(x0 + i * (pip + gap), y, pip, pip), i < n ? on : off);
            GUI.Label(new Rect(r.x + r.width - tagW - 2f, y - 2f, tagW, h), tag, _tiny);
        }

        static void Bar(float x, float y, float w, float u)
        {
            GUI.DrawTexture(new Rect(x, y, w, 8), _dotOff);
            GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(u), 8), _bar);
        }

        static string Short(Team t) => BroadcastHud.BugName(t.Captain.Name);

        static Texture2D HomeStripe(Match match) => Stripe(match.Home.Captain.Faction);
        static Texture2D AwayStripe(Match match) => Stripe(match.Away.Captain.Faction);

        static Texture2D Stripe(string faction)
        {
            switch (faction)
            {
                case "spark": return _spark;
                case "royal": return _royal;
                case "carnival": return _carnival;
                case "goldrush": return _goldrush;
                case "canopy": return _canopy;
                case "ember": return _ember;
                default: return _ink;
            }
        }

        static void Ensure()
        {
            if (_title != null) return;
            _title = Sty(42, new Color(1f, 0.85f, 0.2f), FontStyle.Bold);
            _h1 = Sty(26, Color.white, FontStyle.Bold);
            _h1.clipping = TextClipping.Overflow;
            _h1.padding = new RectOffset(4, 4, 0, 0);
            _stamp = Sty(92, new Color(1f, 0.82f, 0.18f), FontStyle.Bold);
            _stamp.alignment = TextAnchor.MiddleCenter;
            _stamp.clipping = TextClipping.Overflow;
            _stamp.wordWrap = false;
            _bookHead = Sty(44, new Color(1f, 0.92f, 0.28f), FontStyle.Bold);
            _bookHead.clipping = TextClipping.Overflow;
            _bookTitle = Sty(48, new Color(1f, 0.92f, 0.28f), FontStyle.Bold);
            _bookLine = Sty(HowToPlay.BookLinePt, Color.white, FontStyle.Bold);
            _bookLine.wordWrap = true;
            _bookLine.clipping = TextClipping.Overflow;
            _body = Sty(18, new Color(0.95f, 0.96f, 0.97f), FontStyle.Normal);
            _gold = Sty(20, new Color(1f, 0.82f, 0.25f), FontStyle.Bold);
            _tiny = Sty(15, new Color(0.85f, 0.88f, 0.9f), FontStyle.Normal);
            _stat = Sty(15, new Color(1f, 0.82f, 0.25f), FontStyle.Bold);
            _stat.clipping = TextClipping.Overflow;
            _score = Sty(28, Color.white, FontStyle.Bold);
            _team = Sty(22, Color.white, FontStyle.Bold);
            _panel = Tex(new Color(0.05f, 0.06f, 0.09f, 0.86f));
            _bookBack = Tex(new Color(0.06f, 0.07f, 0.11f, 1f));
            _bookCard = Tex(new Color(0.14f, 0.16f, 0.22f, 1f));
            _ink = Tex(new Color(1f, 0.82f, 0.2f, 1f));
            _white = Tex(Color.white);
            _bar = Tex(new Color(0.35f, 0.82f, 0.45f, 1f));
            _spark = Tex(Colors.Spark);
            _royal = Tex(Colors.Royal);
            _carnival = Tex(Colors.Carnival);
            _goldrush = Tex(Colors.Goldrush);
            _canopy = Tex(Colors.Canopy);
            _ember = Tex(Colors.EmberFire);
            _starOn = StarTex(new Color(1f, 0.82f, 0.18f, 1f));
            _starOff = StarTex(new Color(1f, 1f, 1f, 0.28f));
            _dotOn = CircleTex(new Color(1f, 0.92f, 0.55f, 1f));
            _dotOff = CircleTex(new Color(1f, 1f, 1f, 0.22f));
            _outOn = DiamondTex(new Color(1f, 0.45f, 0.28f, 1f));
            _outOff = DiamondTex(new Color(1f, 1f, 1f, 0.22f));
        }

        static GUIStyle Sty(int size, Color c, FontStyle fs)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = size;
            s.fontStyle = fs;
            s.normal.textColor = c;
            s.hover.textColor = c;
            s.clipping = TextClipping.Clip;
            return s;
        }

        static Texture2D Tex(Color c)
        {
            var t = new Texture2D(2, 2);
            t.SetPixels(new[] { c, c, c, c });
            t.Apply();
            return t;
        }

        static Texture2D StarTex(Color c)
        {
            const int n = 32;
            var t = new Texture2D(n, n) { filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var u = (x + 0.5f) / n * 2f - 1f;
                var v = (y + 0.5f) / n * 2f - 1f;
                px[y * n + x] = InStar(u, v) ? c : new Color(0, 0, 0, 0);
            }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        static bool InStar(float x, float y)
        {
            const int k = 5;
            var inside = false;
            float px = 0f, py = 0.95f;
            for (var i = 1; i <= k * 2; i++)
            {
                var r = i % 2 == 0 ? 0.95f : 0.38f;
                var a = -Mathf.PI / 2f + i * Mathf.PI / k;
                var cx = Mathf.Cos(a) * r;
                var cy = Mathf.Sin(a) * r;
                if ((cy > y) != (py > y))
                {
                    var den = py - cy;
                    if (Mathf.Abs(den) < 0.0001f) den = 0.0001f;
                    if (x < (px - cx) * (y - cy) / den + cx)
                        inside = !inside;
                }
                px = cx;
                py = cy;
            }
            return inside;
        }

        static Texture2D CircleTex(Color c)
        {
            const int n = 24;
            var t = new Texture2D(n, n) { filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            const float r = 0.82f;
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var u = (x + 0.5f) / n * 2f - 1f;
                var v = (y + 0.5f) / n * 2f - 1f;
                px[y * n + x] = u * u + v * v <= r * r ? c : new Color(0, 0, 0, 0);
            }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        static Texture2D DiamondTex(Color c)
        {
            const int n = 24;
            var t = new Texture2D(n, n) { filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var u = (x + 0.5f) / n * 2f - 1f;
                var v = (y + 0.5f) / n * 2f - 1f;
                px[y * n + x] = Mathf.Abs(u) + Mathf.Abs(v) <= 0.9f ? c : new Color(0, 0, 0, 0);
            }
            t.SetPixels(px);
            t.Apply();
            return t;
        }
    }

    public enum PhaseUi { Title, Select, Field, Lineup, TeamSetup, DefenseSetup, Set, Flight, InPlay, Result, GameOver }
}
