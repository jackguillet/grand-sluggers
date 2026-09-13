using System;
using System.Linq;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public static class HudView
    {
        static GUIStyle _title, _h1, _body, _gold, _tiny, _stat, _score, _team, _bookTitle, _bookLine,
            _bookHead, _bookHeader, _bookHeaderNumber, _bookNumber, _bookTab, _bookTabSelected, _bookBadge,
            _bookChip, _bookFooter, _bookLineCompact, _stamp;
        static Texture2D _panel, _ink, _starOn, _starOff, _dotOn, _dotOff, _outOn, _outOff, _bar, _white, _bookBack, _bookCard;
        static Texture2D _spark, _royal, _carnival, _goldrush, _canopy, _ember;

        public static void Draw(
            Match match, PhaseUi phase, string parkName, string homeCap, string awayCap,
            bool challenge, string pitcherExtra, bool star, bool steal, string item,
            float charge, float timing, bool showTiming, string banner, string sub, Texture2D portrait,
            bool training = false, string drillProgress = null, bool night = false,
            bool hideHelp = false, string highlight = null, bool replaying = false,
            bool mutePlay = false, int seats = 1,
            bool humanPitches = true, bool humanBats = false,
            bool starPitch = false, bool starSwing = false, bool pad1Home = true,
            bool bunt = false, string titleSetup = null)
        {
            Ensure();
            if (phase == PhaseUi.Title)
            {
                Title(challenge, portrait, training, night, hideHelp, titleSetup);
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
            Play(match, pitcherExtra, star, steal, item, charge, timing, showTiming, banner, sub, seats,
                humanPitches, humanBats, starPitch, starSwing, bunt);
        }

        static void Title(bool challenge, Texture2D portrait, bool training, bool night, bool hideHelp, string setup = null)
        {
            var w = Screen.width;
            Sticker(CarnivalFront.SkyGag(night), w - 168, 36, 140, 32, night ? _gold : _h1);
            var exhibition = !training && !challenge;
            if (exhibition)
            {
                Sticker(CarnivalFront.PlayBall, 44, 88, 640, 28, _gold);
                // Innings and the difficulty rung, next to each other (P7): the two numbers the title owns.
                if (!string.IsNullOrEmpty(setup))
                    GUI.Label(new Rect(44, 124, 640, 22), setup, _gold);
            }
            else
                Sticker(training ? "TRAINING" : "CHALLENGE", 44, 88, 420, 32, _h1);
            if (training)
                GUI.Label(new Rect(44, 124, 640, 22), "Harbor  ·  stick lesson  ·  South start  ·  East skip to field", _tiny);
            else if (challenge)
                GUI.Label(new Rect(44, 124, 640, 22), "South / Space  ·  next match", _gold);
            _ = portrait;
            if (hideHelp) return;
            GUI.Label(new Rect(44, Screen.height - 48, w - 80, 22),
                $"South pick captain    West / F training    Esc how to play    Start / H mode    Tab innings    X / LB difficulty    F6 input: {Controls.Player1InputLabel}", _tiny);
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
            const string bookLabel = "HOW TO PLAY";
            var bookLabelWidth = MeasureWidth(_gold, bookLabel);
            GUI.Label(new Rect(book.X + 88, book.Y + 10, bookLabelWidth, 28), bookLabel, _gold);
            var pageLabel = (page + 1) + " / " + n;
            var header = BookletLayout.Header(
                Screen.width, Screen.height, MeasureWidth(_bookHeader, pageLabel));
            GUI.Label(ToRect(header.Title), p.Title.ToUpperInvariant(), _bookHeader);
            GUI.Label(ToRect(header.Page), pageLabel, _bookHeaderNumber);
            DrawSchemeToggle(scheme);
            DrawSchemeBadges(p, scheme);
            if (p.Id == "contents")
                DrawContentsToc(p);
            else if (p.Id == "getting-started")
                DrawGettingStartedPath(scheme);
            else if (p.Id == "getting-started-modes")
                DrawGettingStartedModes(scheme, p);
            else if (p.Id.StartsWith("controls", StringComparison.Ordinal))
                DrawHardware(scheme, p.Id);
            else if (p.Id.StartsWith("roles", StringComparison.Ordinal))
                DrawRoleTable(scheme, p.Id);
            else if (p.Id == "pitch-swing")
                DrawHowToComics(scheme, p);
            else if (p.Id == "running")
                DrawBagDiagrams(scheme, p);
            else if (p.Id == "screen")
                DrawHudCallouts(p);
            else if (p.Id == "chemistry")
                DrawChemBook(p);
            else if (p.Id == "abilities")
                DrawAbilityCard(p);
            else if (p.Id == "abilities-types")
                DrawAbilityTypes();
            else
            {
                DrawBookLines(p, HowToPlay.TextRect(Screen.width, Screen.height));
            }
            var footer = BookScheme.Footer(scheme);
            var footerH = MeasureHeight(_bookFooter, footer, book.W - 56f);
            GUI.Label(new Rect(book.X + 28, book.Y + book.H - footerH - 6f, book.W - 56, footerH),
                footer, _bookFooter);
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
            var labels = new System.Collections.Generic.List<string>();
            var seat = BookScheme.SeatBadge(scheme);
            if (!string.IsNullOrEmpty(seat)) labels.Add(seat);
            var pageBadge = BookScheme.PageBadge(page.Id, scheme);
            if (!string.IsNullOrEmpty(pageBadge)) labels.Add(pageBadge);
            var widths = labels.Select(label => MeasureWidth(_bookBadge, label)).ToArray();
            var boxes = BookletLayout.Badges(
                Screen.width, Screen.height, MeasureWidth(_gold, "HOW TO PLAY"), widths);
            for (var i = 0; i < labels.Count; i++)
            {
                var r = ToRect(boxes[i]);
                if (MeasureHeight(_bookBadge, labels[i], r.width - 16f) > r.height)
                    ReportBookOverflow("header/badge/" + labels[i]);
                GUI.DrawTexture(r, _ink);
                GUI.Label(new Rect(r.x + 8, r.y, r.width - 16, r.height), labels[i], _bookBadge);
            }
        }

        static void DrawTab(InputScheme kind, InputScheme current)
        {
            var t = BookScheme.Tab(kind, Screen.width, Screen.height);
            var r = new Rect(t.X, t.Y, t.W, t.H);
            var on = kind == current;
            GUI.DrawTexture(r, on ? _ink : _bookCard);
            GUI.Label(new Rect(r.x + 8, r.y + 4, r.width - 16, r.height - 8),
                BookScheme.Label(kind), on ? _bookTabSelected : _bookTab);
        }

        static void DrawHardware(InputScheme scheme, string pageId)
        {
            var b = ControlDiagram.Board(Screen.width, Screen.height);
            var prev = GUI.color;
            GUI.color = new Color(0.22f, 0.78f, 0.38f, 1f);
            GUI.DrawTexture(new Rect(b.X, b.Y, 22, 22), _dotOn);
            GUI.color = prev;
            GUI.Label(new Rect(b.X + 30, b.Y - 4, 180, 32), BookScheme.OffenseLabel, _h1);
            GUI.color = new Color(0.92f, 0.28f, 0.22f, 1f);
            GUI.DrawTexture(new Rect(b.X + 220, b.Y, 22, 22), _dotOn);
            GUI.color = prev;
            GUI.Label(new Rect(b.X + 250, b.Y - 4, 180, 32), BookScheme.DefenseLabel, _h1);

            var calls = ControlDiagram.PageCallouts(scheme, pageId);
            var stackBand = new BookletLayout.Box(b.X, b.Y + 40f, b.W, b.H - 40f);
            var actionStyle = _bookLine;
            var heights = calls.Select(call => 44f + HardwareActionsHeight(call, actionStyle, b.W - 24f)).ToArray();
            var cells = BookletLayout.MeasuredStack(stackBand, heights);
            if (!BookletLayout.Fits(cells, stackBand))
            {
                actionStyle = _bookLineCompact;
                heights = calls.Select(call => 44f + HardwareActionsHeight(call, actionStyle, b.W - 24f)).ToArray();
                cells = BookletLayout.MeasuredStack(stackBand, heights);
            }
            if (!BookletLayout.Fits(cells, stackBand))
                ReportBookOverflow(pageId + "/hardware-stack");
            for (var i = 0; i < calls.Count; i++)
            {
                var c = calls[i];
                var cell = cells[i];
                var r = new Rect(cell.X, cell.Y + 4, cell.W, cell.H - 8);
                GUI.DrawTexture(r, _bookCard);
                GUI.Label(new Rect(r.x + 12, r.y + 3, r.width - 24, 30), c.Hardware, _h1);
                DrawHardwareActions(c, new Rect(r.x + 12, r.y + 32, r.width - 24, r.height - 36), actionStyle);
            }
        }

        static System.Collections.Generic.List<(string Text, Color Color)> HardwareActionLines(
            ControlDiagram.Callout callout)
        {
            var lines = new System.Collections.Generic.List<(string Text, Color Color)>();
            if (callout.Always.Length > 0) lines.Add((callout.Always, Color.white));
            if (callout.Offense.Length > 0) lines.Add((callout.Offense, new Color(0.45f, 0.95f, 0.55f, 1f)));
            if (callout.Defense.Length > 0) lines.Add((callout.Defense, new Color(1f, 0.55f, 0.45f, 1f)));
            return lines;
        }

        static float HardwareActionsHeight(ControlDiagram.Callout callout, GUIStyle style, float width)
        {
            var lines = HardwareActionLines(callout);
            return lines.Sum(line => MeasureHeight(style, line.Text, width)) +
                Mathf.Max(0, lines.Count - 1) * BookletLayout.BlockGap;
        }

        static void DrawHardwareActions(ControlDiagram.Callout callout, Rect band, GUIStyle style)
        {
            var lines = HardwareActionLines(callout);
            var texts = lines.Select(line => line.Text).ToArray();
            var box = new BookletLayout.Box(band.x, band.y, band.width, band.height);
            var blocks = BookletLayout.Flow(texts, box,
                (text, width) => MeasureHeight(style, text, width), 0f, 0f);
            if (!BookletLayout.Fits(blocks, box))
                ReportBookOverflow("controls/" + callout.Id);
            for (var i = 0; i < blocks.Count; i++)
            {
                var old = GUI.color;
                GUI.color = lines[i].Color;
                GUI.Label(ToRect(blocks[i].Box), blocks[i].Text, style);
                GUI.color = old;
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
                GUI.Label(new Rect(r.x + 10, r.y + 7, r.width - 20, 40), diagram.Title.ToUpperInvariant(), _bookHeader);

                var press = BagDiagrams.Press(diagram, scheme);
                var pressH = Mathf.Max(36f, MeasureHeight(_bookChip, press, r.width - 32) + 8f);
                GUI.DrawTexture(new Rect(r.x + 10, r.y + 46, r.width - 20, pressH), _ink);
                GUI.Label(new Rect(r.x + 16, r.y + 50, r.width - 32, pressH - 8), press, _bookChip);

                var caption = BagDiagramCaption(diagram.Kind);
                var captionH = MeasureHeight(_h1, caption, r.width - 20);
                var diagramTop = r.y + 50f + pressH;
                var diagramBottom = r.y + r.height - captionH - 12f;
                var size = Mathf.Min(r.width * 0.68f, Mathf.Max(0f, diagramBottom - diagramTop));
                var x = r.x + (r.width - size) * 0.5f;
                var y = diagramTop;
                DrawBagDiamond(x, y, size, diagram);
                GUI.Label(new Rect(r.x + 10, r.y + r.height - captionH - 8, r.width - 20, captionH), caption, _h1);
            }

            for (var i = 0; i < BagDiagrams.Callouts.Count; i++)
            {
                var call = BagDiagrams.Callouts[i];
                var cell = BagDiagrams.CalloutCard(i, Screen.width, Screen.height);
                var box = new Rect(cell.X, cell.Y, cell.W, cell.H);
                GUI.DrawTexture(box, _bookCard);
                GUI.Label(new Rect(box.x + 10, box.y + 6, box.width - 20, 42), call.Title.ToUpperInvariant(), _bookHeader);
                GUI.Label(new Rect(box.x + 14, box.y + 44, box.width - 28, 32),
                    BagDiagrams.CalloutPress(call, scheme), _gold);
                DrawFittingBookText(new Rect(box.x + 10, box.y + 80, box.width - 20, box.height - 88),
                    call.Line, "running/" + call.Title);
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

        static void DrawGettingStartedPath(InputScheme scheme)
        {
            for (var i = 0; i < GettingStarted.Path.Count; i++)
            {
                var step = GettingStarted.Path[i];
                var cell = GettingStarted.StepCell(i, Screen.width, Screen.height);
                var r = new Rect(cell.X, cell.Y, cell.W, cell.H);
                GUI.DrawTexture(r, _bookCard);
                var title = step.Title.ToUpperInvariant();
                var columns = BookletLayout.NumberedRow(cell, MeasureWidth(_bookHeader, title));
                GUI.Label(ToRect(columns.Number), (i + 1).ToString(), _bookHeader);
                GUI.Label(ToRect(columns.Title), title, _bookHeader);
                var caption = GettingStarted.Caption(step, scheme);
                DrawFittingBookText(ToRect(columns.Body),
                    caption, "getting-started/" + step.Id);
            }
        }

        static void DrawGettingStartedModes(InputScheme scheme, HowToPlay.Page page)
        {
            var table = GettingStarted.ModeTable(Screen.width, Screen.height);
            var tableBox = new BookletLayout.Box(table.X, table.Y, table.W, table.H);
            var bodyStyle = _bookLine;
            var heights = ModeHeights(scheme, table, bodyStyle);
            var rows = BookletLayout.MeasuredStack(tableBox, heights);
            if (!BookletLayout.Fits(rows, tableBox))
            {
                bodyStyle = _bookLineCompact;
                heights = ModeHeights(scheme, table, bodyStyle);
                rows = BookletLayout.MeasuredStack(tableBox, heights);
            }
            if (!BookletLayout.Fits(rows, tableBox))
                ReportBookOverflow("getting-started-modes/table");
            for (var i = 0; i < GettingStarted.Modes.Count; i++)
            {
                var mode = GettingStarted.Modes[i];
                var row = rows[i];
                var columns = BookletLayout.LabeledRow(
                    (row.X, row.Y, row.W, row.H), MeasureWidth(_h1, mode.Title));
                var head = ToRect(columns.Label);
                var prev = GUI.color;
                GUI.color = new Color(0.22f, 0.62f, 0.32f, 1f);
                GUI.DrawTexture(head, _white);
                GUI.color = prev;
                GUI.Label(new Rect(head.x + 8, head.y + 4, head.width - 12, head.height - 8), mode.Title, _h1);
                var line = GettingStarted.Line(mode, scheme);
                DrawFittingBookText(ToRect(columns.Body),
                    line, "getting-started-modes/" + mode.Id);
            }
            DrawBookLines(page, GettingStarted.LineBand(Screen.width, Screen.height));
        }

        static float[] ModeHeights(InputScheme scheme,
            (float X, float Y, float W, float H) table, GUIStyle bodyStyle)
        {
            return GettingStarted.Modes.Select(mode =>
            {
                var columns = BookletLayout.LabeledRow(table, MeasureWidth(_h1, mode.Title));
                var labelH = MeasureHeight(_h1, mode.Title, columns.Label.W - 12f) + 12f;
                var bodyH = MeasureHeight(bodyStyle, GettingStarted.Line(mode, scheme), columns.Body.W) + 8f;
                return Mathf.Max(labelH, bodyH);
            }).ToArray();
        }

        static void DrawContentsToc(HowToPlay.Page page)
        {
            var card = ContentsToc.Card(Screen.width, Screen.height);
            GUI.DrawTexture(new Rect(card.X, card.Y, card.W, card.H), _bookCard);
            var widestNumber = 0f;
            foreach (var chapter in ContentsToc.Chapters)
                widestNumber = Mathf.Max(widestNumber, MeasureWidth(_bookHead, chapter.Number.ToString()));
            for (var i = 0; i < ContentsToc.Chapters.Count; i++)
            {
                var chapter = ContentsToc.Chapters[i];
                var row = ContentsToc.Row(i, Screen.width, Screen.height);
                var columns = BookletLayout.TocColumns(row, widestNumber);
                GUI.Label(ToRect(columns.Title), chapter.Title, _bookLine);
                GUI.Label(ToRect(columns.Number), chapter.Number.ToString(), _bookNumber);
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
                GUI.Label(new Rect(r.x + 16, r.y + 12, r.width - 56, 44), pair.Title.ToUpperInvariant(), _bookHeader);
                ChemPip(r.x + r.width - 40, r.y + 20, pair.Chem);
                DrawFittingBookText(new Rect(r.x + 16, r.y + 60, r.width - 32, r.height - 76),
                    pair.Caption, "chemistry/" + pair.Id);
            }
            DrawBookLines(page, ChemBook.LineBand(Screen.width, Screen.height));
        }

        static void DrawAbilityCard(HowToPlay.Page page)
        {
            var stillCell = ChemBook.AbilityStill(Screen.width, Screen.height);
            var still = new Rect(stillCell.X, stillCell.Y, stillCell.W, stillCell.H);
            GUI.DrawTexture(still, _bookCard);
            GUI.Label(new Rect(still.x + 20, still.y + 20, still.width - 40, 44), "THE CARD", _bookHeader);
            for (var i = 0; i < ChemBook.CardStats.Count; i++)
            {
                var w = (still.width - 40) / ChemBook.CardStats.Count;
                Sticker(ChemBook.CardStats[i], still.x + 20 + i * w, still.y + 92, w - 10, 68, _bookLine);
            }
            DrawBookLines(page, ChemBook.LineBand(Screen.width, Screen.height));
        }

        static void DrawAbilityTypes()
        {
            for (var i = 0; i < ChemBook.Types.Count; i++)
            {
                var row = ChemBook.Types[i];
                var cell = ChemBook.TypeCell(i, Screen.width, Screen.height);
                var r = new Rect(cell.X, cell.Y, cell.W, cell.H);
                GUI.DrawTexture(r, _bookCard);
                var text = row.Title.ToUpperInvariant() + "  ·  " + row.Line;
                DrawFittingBookText(new Rect(r.x + 12, r.y + 6, r.width - 24, r.height - 12),
                    text, "abilities-types/" + row.Id);
            }
        }

        static void DrawBookLines(HowToPlay.Page page, (float X, float Y, float W, float H) band)
        {
            var lines = page.Shown(BookScheme.Current);
            var box = new BookletLayout.Box(band.X, band.Y, band.W, band.H);
            var style = _bookLine;
            var blocks = BookletLayout.BestFlow(lines, box,
                (text, width) => MeasureHeight(style, text, width));
            if (!BookletLayout.Fits(blocks, box))
            {
                style = _bookLineCompact;
                blocks = BookletLayout.BestFlow(lines, box,
                    (text, width) => MeasureHeight(style, text, width));
            }
            if (!BookletLayout.Fits(blocks, box))
            {
                ReportBookOverflow(page.Id);
            }
            foreach (var block in blocks)
                Sticker(block.Text, block.Box.X, block.Box.Y, block.Box.W, block.Box.H, style);
        }

        static float MeasureWidth(GUIStyle style, string text) =>
            Mathf.Ceil(style.CalcSize(new GUIContent(text)).x);

        static float MeasureHeight(GUIStyle style, string text, float width) =>
            Mathf.Ceil(style.CalcHeight(new GUIContent(text), width));

        static void DrawFittingBookText(Rect rect, string text, string context)
        {
            var style = MeasureHeight(_bookLine, text, rect.width) <= rect.height
                ? _bookLine
                : _bookLineCompact;
            if (MeasureHeight(style, text, rect.width) > rect.height)
                ReportBookOverflow(context);
            GUI.Label(rect, text, style);
        }

        static void ReportBookOverflow(string context)
        {
            var warning = context + " / " + BookScheme.Current + " / " + Screen.width + "x" + Screen.height;
            if (_bookOverflowWarnings.Add(warning))
                Debug.LogError("Booklet copy does not fit its band: " + warning);
        }

        static Rect ToRect(BookletLayout.Box box) => new(box.X, box.Y, box.W, box.H);

        static void DrawHudCallouts(HowToPlay.Page page)
        {
            var spreads = HudCallouts.OnScreenPage;
            for (var i = 0; i < spreads.Count; i++)
            {
                var spread = spreads[i];
                var row = HudCallouts.Row(i, Screen.width, Screen.height);
                var r = new Rect(row.X, row.Y, row.W, row.H);
                GUI.DrawTexture(r, _bookCard);
                GUI.Label(new Rect(r.x + 12, r.y + 6, r.width - 24, 38), spread.Title.ToUpperInvariant(), _bookHeader);
                for (var m = 0; m < spread.Marks.Count; m++)
                {
                    var mark = spread.Marks[m];
                    var cell = HudCallouts.MarkCell(i, m, Screen.width, Screen.height);
                    Sticker("•  " + mark.Label, cell.X, cell.Y, cell.W, cell.H, _h1);
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
                GUI.Label(new Rect(r.x + 16, r.y + 8, r.width - 32, 40), strip.Title.ToUpperInvariant(), _bookHeader);
                var motion = HowToComic.MotionOf(strip, scheme);
                var chipH = Mathf.Max(56f,
                    Mathf.Max(MeasureHeight(_bookChip, motion.Charge, (r.width - 64f) * 0.5f - 16f),
                        MeasureHeight(_bookChip, motion.Commit, (r.width - 64f) * 0.5f - 16f)) + 10f);
                var arrowW = 32f;
                var chipW = (r.width - 32f - arrowW) * 0.5f;
                var y = r.y + 46;
                DrawMotionChip(new Rect(r.x + 16, y, chipW, chipH), motion.Charge);
                GUI.Label(new Rect(r.x + 16 + chipW, y, arrowW, chipH), "→", _bookHeader);
                DrawMotionChip(new Rect(r.x + r.width - 16 - chipW, y, chipW, chipH), motion.Commit);
                GUI.Label(new Rect(r.x + 16, y + chipH + 6, r.width - 32, r.height - chipH - 58),
                    HowToComic.Caption(strip, scheme), _h1);
            }
            DrawBookLines(page, HowToComic.LineBand(Screen.width, Screen.height));
        }

        static void DrawMotionChip(Rect r, string label)
        {
            GUI.DrawTexture(r, _ink);
            GUI.Label(new Rect(r.x + 8, r.y + 5, r.width - 16, r.height - 10), label, _bookChip);
        }

        static void DrawRoleTable(InputScheme scheme, string pageId)
        {
            var block = RoleTables.OnPage(scheme, pageId);
            var board = ControlDiagram.Board(Screen.width, Screen.height);
            var head = new Rect(board.X, board.Y, board.W, 44);
            var prev = GUI.color;
            GUI.color = new Color(0.22f, 0.62f, 0.32f, 1f);
            GUI.DrawTexture(head, _white);
            GUI.color = prev;
            GUI.Label(new Rect(head.x + 12, head.y + 3, head.width - 20, 38), block.Title, _bookHeader);
            for (var i = 0; i < block.Rows.Count; i++)
            {
                var row = block.Rows[i];
                var cell = RoleTables.RowCard(i, block.Rows.Count, Screen.width, Screen.height);
                var r = new Rect(cell.X, cell.Y, cell.W, cell.H);
                GUI.DrawTexture(r, _bookCard);
                var line = row.Verb.ToUpperInvariant() + "  ·  " + row.Press;
                DrawFittingBookText(new Rect(r.x + 12, r.y + 6, r.width - 24, r.height - 12),
                    line, "roles/" + block.Id + "/" + row.Verb);
            }
        }

        static readonly System.Collections.Generic.Dictionary<string, Texture2D> _bookPics = new();
        static readonly System.Collections.Generic.HashSet<string> _bookOverflowWarnings = new();

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

        static void Play(Match match, string pitcherExtra, bool star, bool steal, string item,
            float charge, float timing, bool showTiming, string banner, string sub, int seats,
            bool humanPitches, bool humanBats, bool starPitch, bool starSwing, bool bunt)
        {
            var lay = BroadcastHud.Layout(seats);
            Scorebug(match, lay);
            Cards(match, pitcherExtra, star, steal, item, charge, timing, showTiming, lay,
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
            BagPip(x, y, size, 1, bug.RunnerFirst, bug.SelectedBag);
            BagPip(x, y, size, 2, bug.RunnerSecond, bug.SelectedBag);
            BagPip(x, y, size, 3, bug.RunnerThird, bug.SelectedBag);
        }

        /// <summary>Occupied bags only (D1): there are no leads to show.</summary>
        static void BagPip(float x, float y, float size, int bag, bool on, int selected)
        {
            var uv = Baserunning.DiamondPip(bag);
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

        static void Cards(Match match, string pitcherExtra, bool star, bool steal, string item,
            float charge, float timing, bool showTiming, BroadcastHud.PlayLayout lay,
            bool humanPitches, bool humanBats, bool starPitch, bool starSwing, bool bunt)
        {
            var bug = BroadcastHud.From(match);
            var bStar = starSwing || (star && humanBats);
            SeatCard(Px(lay.BatterCard), "AB", bug.Batter, humanBats,
                "NEXT  " + bug.Next,
                BroadcastHud.BatterExtra(bStar, steal, match.CanSteal, bunt, item),
                Look.Portrait(match.Batter));
            SeatCard(Px(lay.PitcherCard), "P", bug.Pitcher, humanPitches,
                BroadcastHud.ArmLine(match.PitcherStamina, match.Rules),
                (pitcherExtra ?? "")
                    + (BroadcastHud.PoorArm(match.PitcherStamina, match.Rules) ? "  SWEAT" : ""),
                Look.Portrait(match.Pitcher));
            Bar(Px(lay.PitcherCard).x + 16, Px(lay.PitcherCard).y + Px(lay.PitcherCard).height - 22,
                Px(lay.PitcherCard).width - 32, Mathf.Clamp01(match.PitcherStamina / (float)match.PitcherStaminaMax));
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
            _bookHead.wordWrap = false;
            _bookHeader = Sty(HowToPlay.BookHeaderPt, new Color(1f, 0.92f, 0.28f), FontStyle.Bold);
            _bookHeader.wordWrap = false;
            _bookHeader.clipping = TextClipping.Clip;
            _bookTitle = Sty(48, new Color(1f, 0.92f, 0.28f), FontStyle.Bold);
            _bookLine = Sty(HowToPlay.BookLinePt, Color.white, FontStyle.Bold);
            _bookLine.wordWrap = true;
            _bookLine.clipping = TextClipping.Overflow;
            _bookLineCompact = Sty(HowToPlay.BookLineMinPt, Color.white, FontStyle.Bold);
            _bookLineCompact.wordWrap = true;
            _bookLineCompact.clipping = TextClipping.Overflow;
            _bookNumber = new GUIStyle(_bookHead);
            _bookNumber.wordWrap = false;
            _bookNumber.clipping = TextClipping.Clip;
            _bookNumber.alignment = TextAnchor.MiddleRight;
            _bookHeaderNumber = new GUIStyle(_bookHeader);
            _bookHeaderNumber.alignment = TextAnchor.MiddleRight;
            _bookTab = Sty(HowToPlay.BookTabPt, Color.white, FontStyle.Bold);
            _bookTab.wordWrap = false;
            _bookTab.clipping = TextClipping.Clip;
            _bookTab.alignment = TextAnchor.MiddleCenter;
            _bookTabSelected = Sty(HowToPlay.BookTabPt, new Color(0.08f, 0.07f, 0.04f), FontStyle.Bold);
            _bookTabSelected.wordWrap = false;
            _bookTabSelected.clipping = TextClipping.Clip;
            _bookTabSelected.alignment = TextAnchor.MiddleCenter;
            _bookBadge = Sty(HowToPlay.BookBadgePt, new Color(0.08f, 0.07f, 0.04f), FontStyle.Bold);
            _bookBadge.wordWrap = false;
            _bookBadge.clipping = TextClipping.Clip;
            _bookBadge.alignment = TextAnchor.MiddleCenter;
            _bookBadge.padding = new RectOffset(0, 0, 0, 0);
            _bookChip = new GUIStyle(_bookTabSelected);
            _bookChip.wordWrap = true;
            _bookFooter = Sty(HowToPlay.BookFooterPt, Color.white, FontStyle.Bold);
            _bookFooter.wordWrap = false;
            _bookFooter.clipping = TextClipping.Clip;
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
