using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public static class HudView
    {
        static GUIStyle _title, _h1, _body, _gold, _tiny, _score, _team;
        static Texture2D _panel, _ink, _starOn, _starOff, _dotOn, _dotOff, _outOn, _outOff, _bar, _white;
        static Texture2D _spark, _royal, _carnival, _goldrush, _canopy, _ember;

        public static void Draw(
            Match match, PhaseUi phase, string parkName, string homeCap, string awayCap,
            bool challenge, string[] pitches, int pitchIndex, bool star, bool steal, string item,
            float charge, float timing, bool showTiming, string banner, string sub, Texture2D portrait,
            bool training = false, string drillProgress = null, bool night = false,
            bool hideHelp = false, string highlight = null, bool replaying = false,
            bool mutePlay = false)
        {
            Ensure();
            if (phase == PhaseUi.Title)
            {
                Title(parkName, homeCap, awayCap, challenge, portrait, training, night, hideHelp);
                return;
            }
            if (phase == PhaseUi.Select)
            {
                Select(homeCap, awayCap, null);
                return;
            }
            if (phase == PhaseUi.Lineup)
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
            Play(match, pitches, pitchIndex, star, steal, item, charge, timing, showTiming, banner, sub);
        }

        static void Title(string park, string home, string away, bool challenge, Texture2D portrait, bool training, bool night, bool hideHelp)
        {
            var w = Screen.width;
            GUI.DrawTexture(new Rect(36, 28, 620, 92), _panel);
            GUI.Label(new Rect(52, 36, 600, 54), "GRAND SLUGGERS", _title);
            var exhibition = !training && !challenge;
            GUI.DrawTexture(new Rect(36, 136, 420, exhibition ? 92 : 64), _panel);
            GUI.Label(new Rect(52, 144, 400, 36), exhibition ? "EXHIBITION" : (training ? "TRAINING" : "CHALLENGE"), _h1);
            GUI.Label(new Rect(52, 184, 390, 28),
                exhibition ? "South  ·  play ball" : (training ? "South  ·  Harbor drills" : "South  ·  next match"),
                _gold);
            if (exhibition)
            {
                GUI.Label(new Rect(52, 244, 640, 24), home + "  vs  " + away, _body);
                GUI.Label(new Rect(52, 270, 640, 22), park + "  ·  " + (night ? "NIGHT" : "DAY"), night ? _gold : _tiny);
            }
            else if (training)
                GUI.Label(new Rect(52, 216, 640, 22), "Harbor Diamond  ·  four drills", _tiny);
            if (portrait != null)
                GUI.DrawTexture(new Rect(w - 300, 36, 260, 260), portrait, ScaleMode.ScaleToFit);
            if (hideHelp) return;
            GUI.Label(new Rect(52, Screen.height - 56, w - 80, 22),
                "South Exhibition   West training   Start mode   stick later picks captains", _tiny);
        }

        public static void Select(string homeId, string awayId, ContentCatalog content)
        {
            Ensure();
            var w = Screen.width;
            GUI.DrawTexture(new Rect(36, 24, 560, 78), _panel);
            GUI.Label(new Rect(52, 32, 540, 36), "PICK YOUR CAPTAIN", _h1);
            GUI.Label(new Rect(52, 68, 520, 22), "stick  home / away    South  lineup", _gold);
            var ids = PresetTeams.CaptainIds;
            var card = Mathf.Min(150f, (w - 80) / ids.Length - 8);
            var x0 = (w - ids.Length * (card + 8) + 8) * 0.5f;
            for (var i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                var r = new Rect(x0 + i * (card + 8), Screen.height - card - 36, card, card);
                GUI.DrawTexture(r, _panel);
                if (id == homeId)
                    GUI.DrawTexture(new Rect(r.x, r.y, r.width, 6), _starOn != null ? _starOn : _panel);
                if (Look.HasPortrait(id))
                    GUI.DrawTexture(new Rect(r.x + 8, r.y + 10, r.width - 16, r.height - 36), Look.Portrait(id), ScaleMode.ScaleToFit);
                var label = id == homeId ? "HOME" : id == awayId ? "AWAY" : "";
                if (content != null && content.Characters.TryGetValue(id, out var who))
                    GUI.Label(new Rect(r.x, r.yMax - 24, r.width, 22), who.Name, _tiny);
                else
                    GUI.Label(new Rect(r.x, r.yMax - 24, r.width, 22), string.IsNullOrEmpty(label) ? id : label, _tiny);
            }
            GUI.Label(new Rect(36, 110, 700, 24),
                (content != null && content.Characters.TryGetValue(homeId, out var h) ? h.Name : homeId)
                + "  vs  "
                + (content != null && content.Characters.TryGetValue(awayId, out var a) ? a.Name : awayId),
                _gold);
        }

        public static void BagTell(int bag)
        {
            Ensure();
            var labels = new[] { "1B", "2B", "3B", "HOME" };
            var w = 88f;
            var x0 = Screen.width * 0.5f - (w * 4 + 24) * 0.5f;
            var y = Screen.height - 78f;
            GUI.Label(new Rect(x0, y - 26, 400, 22), "throw  ·  stick / 1 2 3 4", _tiny);
            for (var i = 0; i < 4; i++)
            {
                var r = new Rect(x0 + i * (w + 8), y, w, 48);
                GUI.DrawTexture(r, _panel);
                var lit = bag == i + 1;
                GUI.Label(r, labels[i], lit ? _gold : _body);
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
            GUI.Label(new Rect(40, 36, 1000, 32), "TEAM SHEET  ·  " + match.Park.Name + (match.Night ? "  NIGHT" : ""), _h1);
            GUI.Label(new Rect(40, 72, 1000, 22),
                match.Home.Name + "  " + match.HomeBat.Name + " / " + match.HomeGlove.Name + "   [B][G]", _body);
            var y = 110;
            foreach (var c in match.HomeOrder)
            {
                var mark = c.Captain ? "C" : "+";
                GUI.Label(new Rect(48, y, 700, 22),
                    mark + "  " + c.Name + "   B" + c.Stats.Bat + "  P" + c.Stats.Pitch + "  F" + c.Stats.Field + "  R" + c.Stats.Run,
                    _body);
                y += 24;
            }
            GUI.Label(new Rect(40, y + 16, 800, 22), "SPACE  first pitch", _gold);
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
            float charge, float timing, bool showTiming, string banner, string sub)
        {
            Scorebug(match);
            Matchup(match, pitches, pi, star, steal, item, charge, timing, showTiming);

            if (!string.IsNullOrEmpty(banner))
            {
                GUI.DrawTexture(new Rect(18, 148, 456, 64), _panel);
                GUI.Label(new Rect(30, 152, 432, 28), banner, _h1);
                if (!string.IsNullOrEmpty(sub))
                    GUI.Label(new Rect(30, 180, 432, 24), sub, _gold);
            }
            else if (!string.IsNullOrEmpty(sub))
                GUI.Label(new Rect(24, 146, 456, 22), sub, _tiny);
        }

        static void Scorebug(Match match)
        {
            var bug = BroadcastHud.From(match);
            const float x = 18f;
            const float y = 14f;
            GUI.DrawTexture(new Rect(x, y, 456, 148), _panel);
            GUI.DrawTexture(new Rect(x, y, 6, 148), _ink);
            var half = bug.Over ? "FINAL" : (bug.Top ? "TOP " : "BOT ") + bug.Inning;
            GUI.Label(new Rect(x + 16, y + 6, 180, 20), half, _gold);

            Row(x + 16, y + 30, match.Away, bug.AwayScore, match.AwayStars, AwayStripe(match));
            Row(x + 16, y + 62, match.Home, bug.HomeScore, match.HomeStars, HomeStripe(match));

            Count(x + 16, y + 96, bug.Balls, 4, _dotOn, _dotOff);
            GUI.Label(new Rect(x + 92, y + 96, 18, 18), "B", _tiny);
            Count(x + 118, y + 96, bug.Strikes, 3, _dotOn, _dotOff);
            GUI.Label(new Rect(x + 176, y + 96, 18, 18), "S", _tiny);
            Count(x + 200, y + 96, bug.Outs, 3, _outOn, _outOff);
            GUI.Label(new Rect(x + 258, y + 96, 18, 18), "O", _tiny);

            MiniDiamond(x + 290, y + 96, bug);
            GUI.Label(new Rect(x + 16, y + 118, 420, 22), "NEXT  " + bug.Next, _tiny);
        }

        static void MiniDiamond(float x, float y, BroadcastHud.Scorebug bug)
        {
            BagPip(x + 28, y + 14, bug.RunnerSecond);
            BagPip(x + 46, y + 4, bug.RunnerFirst);
            BagPip(x + 10, y + 4, bug.RunnerThird);
        }

        static void BagPip(float x, float y, bool on)
        {
            GUI.DrawTexture(new Rect(x, y, 14, 14), on ? _outOn : _outOff);
        }

        static void Row(float x, float y, Team team, int runs, double stars, Texture2D stripe)
        {
            GUI.DrawTexture(new Rect(x, y + 4, 8, 22), stripe);
            GUI.Label(new Rect(x + 16, y, 210, 28), Short(team), _team);
            GUI.Label(new Rect(x + 220, y - 2, 70, 32), runs.ToString(), _score);
            Stars(x + 292, y + 6, stars);
        }

        static void Matchup(Match match, string[] pitches, int pi, bool star, bool steal, string item,
            float charge, float timing, bool showTiming)
        {
            var x = Screen.width - 428f;
            const float y = 14f;
            GUI.DrawTexture(new Rect(x, y, 410, 118), _panel);
            var bug = BroadcastHud.From(match);
            GUI.Label(new Rect(x + 16, y + 8, 280, 22), "P   " + bug.Pitcher, _body);
            Bar(x + 16, y + 32, 220, match.PitcherStamina / 100f);
            GUI.Label(new Rect(x + 244, y + 26, 150, 20), "ARM  " + match.PitcherStamina, _tiny);
            GUI.Label(new Rect(x + 16, y + 48, 380, 22), "AB  " + bug.Batter, _body);
            var extra = (star ? "STAR  " : "") + (steal ? "STEAL  " : "") + (string.IsNullOrEmpty(item) ? "" : item);
            GUI.Label(new Rect(x + 16, y + 74, 380, 22), pitches[pi].ToUpperInvariant() + (extra.Length > 0 ? "   " + extra.Trim() : ""), extra.Length > 0 ? _gold : _tiny);

            if (!showTiming) return;
            GUI.DrawTexture(new Rect(x + 16, y + 98, 220, 8), _dotOff);
            GUI.DrawTexture(new Rect(x + 16 + 96, y + 96, 28, 12), _starOn);
            var pip = x + 16 + Mathf.Clamp01(timing) * 220f;
            GUI.DrawTexture(new Rect(pip - 2, y + 94, 4, 16), _white);
            GUI.DrawTexture(new Rect(x + 250, y + 100, Mathf.Clamp01(charge) * 140f, 6), _starOn);
        }

        static void Stars(float x, float y, double n)
        {
            for (var i = 0; i < 5; i++)
                GUI.DrawTexture(new Rect(x + i * 22, y, 18, 18), n > i ? _starOn : _starOff);
        }

        static void Count(float x, float y, int n, int max, Texture2D on, Texture2D off)
        {
            for (var i = 0; i < max; i++)
                GUI.DrawTexture(new Rect(x + i * 16, y, 14, 14), i < n ? on : off);
        }

        static void Bar(float x, float y, float w, float u)
        {
            GUI.DrawTexture(new Rect(x, y, w, 8), _dotOff);
            GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(u), 8), _bar);
        }

        static string Short(Team t)
        {
            var n = t.Captain.Name;
            var sp = n.LastIndexOf(' ');
            return (sp >= 0 ? n.Substring(sp + 1) : n).ToUpperInvariant();
        }

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

    public enum PhaseUi { Title, Select, Lineup, Set, Flight, InPlay, Result, GameOver }
}
