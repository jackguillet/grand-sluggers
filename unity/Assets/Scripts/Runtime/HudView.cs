using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public static class HudView
    {
        static GUIStyle _title, _h1, _body, _gold, _tiny, _stat, _score, _team;
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
            bool starPitch = false, bool starSwing = false)
        {
            Ensure();
            if (phase == PhaseUi.Title)
            {
                Title(challenge, portrait, training, night, hideHelp);
                return;
            }
            if (phase == PhaseUi.Select)
            {
                Select(homeCap, awayCap, null);
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

        public static void Select(string homeId, string awayId, ContentCatalog content)
        {
            Ensure();
            if (content != null && content.Characters.TryGetValue(homeId, out var homeWho))
                Card(CharacterCard.Of(homeWho), 36, 28);
            if (content != null && content.Characters.TryGetValue(awayId, out var awayWho))
                Sticker("vs  " + awayWho.Name, 36, 268, 400, 24, _gold);
            GUI.Label(new Rect(44, Screen.height - 48, Screen.width - 80, 22),
                "pad 1 L/R home    pad 2 L/R or U/D away    South the field    West title    Esc how to play", _tiny);
        }

        public static void Card(CharacterCard card, float x, float y)
        {
            Ensure();
            const float w = 312f;
            const float h = 232f;
            GUI.DrawTexture(new Rect(x, y, w, h), _panel);
            GUI.Label(new Rect(x + 14, y + 8, w - 50, 28), card.Name.ToUpperInvariant(), _h1);
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
                var p = HowToPlay.Pages[(page % HowToPlay.Pages.Count + HowToPlay.Pages.Count) % HowToPlay.Pages.Count];
                var book = HowToPlay.BookPanel(Screen.width, Screen.height, p.Lines.Count);
                var x = book.X;
                var y = book.Y;
                var w = book.W;
                var h = book.H;
                GUI.DrawTexture(new Rect(x, y, w, h), _panel);
                GUI.Label(new Rect(x + 24, y + 12, w - 48, 22), "HOW TO PLAY", _gold);
                GUI.Label(new Rect(x + 24, y + 32, w - 48, 28),
                    p.Title.ToUpperInvariant() + "    " + (page + 1) + " / " + HowToPlay.Pages.Count, _h1);
                for (var i = 0; i < p.Lines.Count; i++)
                    GUI.Label(new Rect(x + 28, y + 64 + i * 24, w - 56, 22), p.Lines[i], _body);
                GUI.Label(new Rect(x + 28, y + h - 32, w - 56, 20),
                    "South / left click next    wheel    East / right click / Esc back", _tiny);
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

        public static void ControlDisplay(string pos, string name)
        {
            var label = BroadcastHud.ControlDisplay(true, pos, name);
            if (string.IsNullOrEmpty(label)) return;
            Ensure();
            GUI.DrawTexture(new Rect(36, Screen.height - 120, 280, 36), _panel);
            GUI.Label(new Rect(48, Screen.height - 116, 256, 28), label, _gold);
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
                Look.HasPortrait(match.Batter.Id) ? Look.Portrait(match.Batter.Id) : null);
            SeatCard(Px(lay.PitcherCard), "P", bug.Pitcher, humanPitches,
                BroadcastHud.ArmLine(match.PitcherStamina),
                (pitches != null && pi >= 0 && pi < pitches.Length ? pitches[pi].ToUpperInvariant() : "")
                    + (pStar ? "  STAR" : "")
                    + (BroadcastHud.PoorArm(match.PitcherStamina) ? "  SWEAT" : ""),
                Look.HasPortrait(match.Pitcher.Id) ? Look.Portrait(match.Pitcher.Id) : null);
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
