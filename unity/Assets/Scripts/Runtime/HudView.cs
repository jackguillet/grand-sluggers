using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public static class HudView
    {
        static GUIStyle _title, _h1, _body, _gold, _tiny, _stat, _score, _team, _bookTitle, _bookLine, _bookHead;
        static Texture2D _panel, _ink, _starOn, _starOff, _dotOn, _dotOff, _outOn, _outOff, _bar, _white;
        static Texture2D _spark, _royal, _carnival, _goldrush, _canopy, _ember;

        public static void Draw(
            Match match, PhaseUi phase, string parkName, string homeCap, string awayCap,
            bool challenge, string[] pitches, int pitchIndex, bool star, bool steal, string item,
            float charge, float timing, bool showTiming, string banner, string sub, Texture2D portrait,
            bool training = false, string drillProgress = null, bool night = false,
            bool hideHelp = false, string highlight = null, bool replaying = false,
            bool mutePlay = false, int seats = 1,
            bool humanPitches = true, bool humanBats = false,
            bool starPitch = false, bool starSwing = false, bool pad1Home = true)
        {
            Ensure();
            if (phase == PhaseUi.Title)
            {
                Title(challenge, portrait, training, night, hideHelp);
                return;
            }
            if (phase == PhaseUi.Select)
            {
                Select(homeCap, awayCap, pad1Home, null);
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
                humanPitches, humanBats, starPitch, starSwing);
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
                "South pick captain    West / F training    Esc how to play    Start / H mode    Tab innings", _tiny);
        }

        public static void Select(string homeId, string awayId, bool pad1Home, ContentCatalog content)
        {
            Ensure();
            var yours = pad1Home ? homeId : awayId;
            var theirs = pad1Home ? awayId : homeId;
            if (content != null && content.Characters.TryGetValue(yours, out var youWho))
                Card(CharacterCard.Of(youWho), 36, 28);
            var vs = "vs  ";
            if (content != null && content.Characters.TryGetValue(theirs, out var themWho))
                vs += themWho.Name;
            Sticker(CarnivalFront.SeatMark(pad1Home) + "  " + vs, 36, 268, 480, 24, _gold);
            GUI.Label(new Rect(36, 300, 520, 22), CarnivalFront.SeatHint(pad1Home), _tiny);
            GUI.Label(new Rect(44, Screen.height - 48, Screen.width - 80, 22),
                "L/R your team    U/D the other    North HOME/AWAY    South the field    West title    Esc how to play", _tiny);
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

        public static void Pause(int item, bool howTo, int page)
        {
            Ensure();
            var dim = _panel;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), dim);
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
            GUI.DrawTexture(new Rect(book.X, book.Y, book.W, book.H), _panel);
            GUI.Label(new Rect(book.X + 28, book.Y + 10, 280, 28), "HOW TO PLAY", _gold);
            GUI.Label(new Rect(book.X + 28, book.Y + 38, book.W - 380, 44),
                p.Title.ToUpperInvariant() + "   " + (page + 1) + " / " + n, _bookHead);
            DrawSchemeToggle(scheme);
            if (p.Id == "controls")
                DrawHardware(scheme);
            else if (p.Id == "roles")
                DrawRoleTables(scheme);
            else if (p.Id == "pitch-swing")
                DrawHowToComics(scheme, p);
            else if (p.Id == "running")
                DrawBagDiagrams(scheme, p);
            else
            {
                var pic = HowToPlay.PictureRect(Screen.width, Screen.height);
                var text = HowToPlay.TextRect(Screen.width, Screen.height);
                var tex = BookPic(p.Picture);
                if (tex != null)
                    GUI.DrawTexture(new Rect(pic.X, pic.Y, pic.W, pic.H), tex, ScaleMode.ScaleToFit);
                else
                    GUI.DrawTexture(new Rect(pic.X, pic.Y, pic.W, pic.H), _dotOff);
                var lines = p.Shown(scheme);
                var lineH = HowToPlay.KidLineH;
                for (var i = 0; i < lines.Count; i++)
                    GUI.Label(new Rect(text.X, text.Y + i * lineH, text.W, lineH + 8f), lines[i], _bookLine);
            }
            GUI.Label(new Rect(book.X + 28, book.Y + book.H - 36, book.W - 56, 28),
                BookScheme.Footer(scheme), _tiny);
        }

        static void DrawSchemeToggle(InputScheme scheme)
        {
            DrawTab(InputScheme.Pad, scheme);
            DrawTab(InputScheme.Keys, scheme);
        }

        static void DrawTab(InputScheme kind, InputScheme current)
        {
            var t = BookScheme.Tab(kind, Screen.width, Screen.height);
            var r = new Rect(t.X, t.Y, t.W, t.H);
            var on = kind == current;
            GUI.DrawTexture(r, on ? _ink : _dotOff);
            GUI.Label(new Rect(r.x + 8, r.y + 6, r.width - 12, r.height - 8), BookScheme.Label(kind), on ? _h1 : _tiny);
        }

        static void DrawHardware(InputScheme scheme)
        {
            var b = ControlDiagram.Board(Screen.width, Screen.height);
            var prev = GUI.color;
            GUI.color = new Color(0.22f, 0.78f, 0.38f, 1f);
            GUI.DrawTexture(new Rect(b.X, b.Y + 4, 16, 16), _dotOn);
            GUI.color = prev;
            GUI.Label(new Rect(b.X + 22, b.Y, 120, 22), BookScheme.OffenseLabel, _tiny);
            GUI.color = new Color(0.92f, 0.28f, 0.22f, 1f);
            GUI.DrawTexture(new Rect(b.X + 150, b.Y + 4, 16, 16), _dotOn);
            GUI.color = prev;
            GUI.Label(new Rect(b.X + 172, b.Y, 140, 22), BookScheme.DefenseLabel, _tiny);

            foreach (var part in ControlDiagram.Parts(scheme))
            {
                var r = Map(b, part.U, part.V, part.W, part.H);
                GUI.color = part.Id is "body" or "mouse" or "space" or "shift"
                    ? new Color(0.18f, 0.20f, 0.24f, 0.95f)
                    : new Color(0.82f, 0.84f, 0.88f, 1f);
                GUI.DrawTexture(r, _white);
                GUI.color = prev;
                if (part.Id is "south" or "east" or "west" or "north" or "wasd-w" or "wasd-a" or "wasd-s" or "wasd-d"
                    or "n1" or "n2" or "n3" or "n4" or "lt" or "stick" or "dpad" or "start" or "select")
                {
                    var tag = part.Id switch
                    {
                        "south" => "S",
                        "east" => "E",
                        "west" => "W",
                        "north" => "N",
                        "wasd-w" => "W",
                        "wasd-a" => "A",
                        "wasd-s" => "S",
                        "wasd-d" => "D",
                        "n1" => "1",
                        "n2" => "2",
                        "n3" => "3",
                        "n4" => "4",
                        "lt" => "LT",
                        "stick" => "",
                        "dpad" => "+",
                        "start" => "▶",
                        "select" => "≡",
                        _ => ""
                    };
                    if (tag.Length > 0)
                        GUI.Label(r, tag, _tiny);
                }
            }

            foreach (var c in ControlDiagram.Callouts(scheme))
            {
                var r = new Rect(b.X + c.U * b.W, b.Y + c.V * b.H, b.W * 0.27f, 58f);
                GUI.color = new Color(0.92f, 0.52f, 0.14f, 0.92f);
                GUI.DrawTexture(r, _white);
                GUI.color = prev;
                GUI.Label(new Rect(r.x + 8, r.y + 2, r.width - 12, 18), c.Hardware, _gold);
                var y = r.y + 20;
                if (c.Always.Length > 0)
                {
                    GUI.Label(new Rect(r.x + 8, y, r.width - 12, 16), c.Always, _tiny);
                    y += 16;
                }
                if (c.Offense.Length > 0)
                {
                    GUI.color = new Color(0.45f, 0.95f, 0.55f, 1f);
                    GUI.Label(new Rect(r.x + 8, y, r.width - 12, 16), c.Offense, _tiny);
                    GUI.color = prev;
                    y += 16;
                }
                if (c.Defense.Length > 0)
                {
                    GUI.color = new Color(1f, 0.45f, 0.38f, 1f);
                    GUI.Label(new Rect(r.x + 8, y, r.width - 12, 16), c.Defense, _tiny);
                    GUI.color = prev;
                }
            }
        }

        static Rect Map((float X, float Y, float W, float H) board, float u, float v, float w, float h) =>
            new(board.X + u * board.W, board.Y + v * board.H, w * board.W, h * board.H);

        static void DrawBagDiagrams(InputScheme scheme, HowToPlay.Page page)
        {
            for (var i = 0; i < BagDiagrams.Running.Count; i++)
            {
                var diagram = BagDiagrams.Running[i];
                var cell = BagDiagrams.Card(i, Screen.width, Screen.height);
                var r = new Rect(cell.X, cell.Y, cell.W, cell.H);
                GUI.DrawTexture(r, _dotOff);
                GUI.Label(new Rect(r.x + 10, r.y + 8, r.width - 20, 24), diagram.Title.ToUpperInvariant(), _gold);

                var press = BagDiagrams.Press(diagram, scheme);
                GUI.DrawTexture(new Rect(r.x + 10, r.y + 38, r.width - 20, 26), _ink);
                GUI.Label(new Rect(r.x + 16, r.y + 43, r.width - 32, 18), press, _tiny);

                var size = Mathf.Min(r.width * 0.76f, r.height * 0.50f);
                var x = r.x + (r.width - size) * 0.5f;
                var y = r.y + 76f;
                DrawBagDiamond(x, y, size, diagram);
                GUI.Label(new Rect(r.x + 10, r.y + r.height - 44, r.width - 20, 36),
                    BagDiagramCaption(diagram.Kind), _bookLine);
            }

            var band = BagDiagrams.LineBand(Screen.width, Screen.height);
            var lines = page.Shown(scheme);
            var lineH = HowToPlay.KidLineH * 0.72f;
            for (var i = 0; i < lines.Count; i++)
                GUI.Label(new Rect(band.X, band.Y + i * lineH, band.W, lineH), lines[i], _tiny);
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

        static void DrawHowToComics(InputScheme scheme, HowToPlay.Page page)
        {
            var strips = HowToComic.OnPitchSwingPage;
            for (var i = 0; i < strips.Count; i++)
            {
                var strip = strips[i];
                var row = HowToComic.Row(i, Screen.width, Screen.height);
                var r = new Rect(row.X, row.Y, row.W, row.H);
                GUI.DrawTexture(r, _dotOff);
                GUI.Label(new Rect(r.x + 10, r.y + 4, r.width - 20, 24), strip.Title.ToUpperInvariant(), _gold);
                var innerY = r.y + 30;
                var innerH = r.height - 58;
                var stillW = r.width * 0.28f;
                var gap = 10f;
                DrawComicStill(new Rect(r.x + 10, innerY, stillW, innerH), strip.First);
                DrawArrow(r.x + 12 + stillW, innerY + innerH * 0.45f);
                DrawComicStill(new Rect(r.x + 34 + stillW, innerY, stillW, innerH), strip.Second);
                var motionX = r.x + 48 + stillW * 2;
                var motionW = r.x + r.width - 10 - motionX;
                DrawComicMotion(new Rect(motionX, innerY, motionW, innerH), HowToComic.MotionOf(strip, scheme));
                GUI.Label(new Rect(r.x + 10, r.y + r.height - 26, r.width - 20, 22),
                    HowToComic.Caption(strip, scheme), _bookLine);
            }
            var band = HowToComic.LineBand(Screen.width, Screen.height);
            var lines = page.Shown(scheme);
            var lineH = HowToPlay.KidLineH * 0.72f;
            for (var i = 0; i < lines.Count; i++)
                GUI.Label(new Rect(band.X, band.Y + i * lineH, band.W, lineH), lines[i], _tiny);
        }

        static void DrawComicStill(Rect r, HowToComic.Panel panel)
        {
            var tex = BookPic(panel.Picture);
            GUI.DrawTexture(r, _panel);
            if (tex != null)
                GUI.DrawTexture(new Rect(r.x + 4, r.y + 4, r.width - 8, r.height - 28), tex, ScaleMode.ScaleToFit);
            else
            {
                var prev = GUI.color;
                GUI.color = new Color(0.45f, 0.32f, 0.18f, 1f);
                GUI.DrawTexture(new Rect(r.x + 8, r.y + r.height * 0.55f, r.width - 16, r.height * 0.28f), _white);
                GUI.color = new Color(1f, 0.82f, 0.2f, 1f);
                var ring = Mathf.Min(r.width, r.height) * 0.28f;
                GUI.DrawTexture(new Rect(r.x + r.width * 0.5f - ring * 0.5f, r.y + r.height * 0.28f, ring, ring), _dotOn);
                GUI.color = prev;
                GUI.Label(new Rect(r.x + 8, r.y + 8, r.width - 16, 22), panel.Shot.ToUpperInvariant(), _tiny);
            }
            GUI.Label(new Rect(r.x + 6, r.y + r.height - 24, r.width - 12, 20), panel.Label, _tiny);
        }

        static void DrawArrow(float x, float y)
        {
            GUI.Label(new Rect(x, y, 22, 22), "→", _gold);
        }

        static void DrawComicMotion(Rect r, HowToComic.Motion motion)
        {
            GUI.Label(new Rect(r.x, r.y + 4, r.width, 20), "MOTION", _tiny);
            var chipH = 36f;
            var chipW = r.width * 0.42f;
            var y = r.y + r.height * 0.35f;
            DrawMotionChip(new Rect(r.x, y, chipW, chipH), motion.Charge);
            DrawArrow(r.x + chipW + 2, y + 6);
            DrawMotionChip(new Rect(r.x + r.width - chipW, y, chipW, chipH), motion.Commit);
        }

        static void DrawMotionChip(Rect r, string label)
        {
            GUI.DrawTexture(r, _ink);
            GUI.Label(new Rect(r.x + 6, r.y + 8, r.width - 12, r.height - 10), label, _tiny);
        }

        static void DrawRoleTables(InputScheme scheme)
        {
            var blocks = RoleTables.Of(scheme);
            for (var i = 0; i < blocks.Count; i++)
            {
                var cell = RoleTables.Cell(i, Screen.width, Screen.height);
                var r = new Rect(cell.X, cell.Y, cell.W, cell.H);
                GUI.DrawTexture(r, _dotOff);
                var head = new Rect(r.x, r.y, r.width, 28);
                var prev = GUI.color;
                GUI.color = new Color(0.22f, 0.62f, 0.32f, 1f);
                GUI.DrawTexture(head, _white);
                GUI.color = prev;
                GUI.Label(new Rect(head.x + 10, head.y + 2, head.width - 16, 24), blocks[i].Title, _h1);
                var rows = blocks[i].Rows;
                var rowH = Mathf.Max(22f, (r.height - 36) / Mathf.Max(1, rows.Count));
                for (var n = 0; n < rows.Count; n++)
                {
                    var y = r.y + 32 + n * rowH;
                    GUI.Label(new Rect(r.x + 10, y, r.width * 0.42f, rowH), rows[n].Verb, _gold);
                    GUI.Label(new Rect(r.x + r.width * 0.44f, y, r.width * 0.54f, rowH), rows[n].Press, _tiny);
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

        public static void ControlDisplay(string pos, string name)
        {
            var label = BroadcastHud.ControlDisplay(true, pos, name);
            if (string.IsNullOrEmpty(label)) return;
            Ensure();
            GUI.DrawTexture(new Rect(36, Screen.height - 120, 280, 36), _panel);
            GUI.Label(new Rect(48, Screen.height - 116, 256, 28), label, _gold);
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
            GUI.DrawTexture(new Rect(Screen.width - 320, Screen.height - 120, 284, 36), _panel);
            GUI.Label(new Rect(Screen.width - 308, Screen.height - 116, 260, 28), label, _gold);
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
            bool humanPitches, bool humanBats, bool starPitch, bool starSwing)
        {
            var lay = BroadcastHud.Layout(seats);
            Scorebug(match, lay);
            Cards(match, pitches, pi, star, steal, item, charge, timing, showTiming, lay,
                humanPitches, humanBats, starPitch, starSwing);

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
            bool humanPitches, bool humanBats, bool starPitch, bool starSwing)
        {
            var bug = BroadcastHud.From(match);
            var pStar = starPitch || (star && humanPitches);
            var bStar = starSwing || (star && humanBats);
            SeatCard(Px(lay.BatterCard), "AB", bug.Batter, humanBats,
                "NEXT  " + bug.Next,
                (bStar ? "STAR  " : "") + (steal ? "STEAL  " : "") + (item ?? ""),
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
            _bookHead = Sty(36, Color.white, FontStyle.Bold);
            _bookHead.clipping = TextClipping.Overflow;
            _bookTitle = Sty(42, new Color(1f, 0.85f, 0.2f), FontStyle.Bold);
            _bookLine = Sty(24, new Color(0.95f, 0.96f, 0.97f), FontStyle.Normal);
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
