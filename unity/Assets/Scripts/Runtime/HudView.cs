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
            bool hideHelp = false, string highlight = null, bool replaying = false)
        {
            Ensure();
            if (phase == PhaseUi.Title)
            {
                Title(parkName, homeCap, awayCap, challenge, portrait, training, night, hideHelp);
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
            Play(match, pitches, pitchIndex, star, steal, item, charge, timing, showTiming, banner, sub);
        }

        static void Title(string park, string home, string away, bool challenge, Texture2D portrait, bool training, bool night, bool hideHelp)
        {
            GUI.DrawTexture(new Rect(36, 32, 560, 78), _panel);
            GUI.Label(new Rect(52, 40, 540, 54), "GRAND SLUGGERS", _title);
            var mode = training ? "TRAINING  ·  Harbor Diamond" : challenge ? "CHALLENGE" : "EXHIBITION  ·  Harbor first";
            GUI.Label(new Rect(52, 122, 700, 28), mode, _gold);
            var when = night ? "NIGHT" : "DAY";
            if (training)
            {
                GUI.Label(new Rect(52, 160, 800, 28), "Four drills. No gimmicks.", _h1);
                GUI.Label(new Rect(52, 196, 800, 24), park + "  ·  DAY", _body);
            }
            else
            {
                GUI.Label(new Rect(52, 160, 800, 28), home + "  vs  " + away, _h1);
                GUI.Label(new Rect(52, 196, 800, 24), park + "  ·  " + when, night ? _gold : _body);
            }
            if (portrait != null)
                GUI.DrawTexture(new Rect(Screen.width - 360, 40, 320, 320), portrait, ScaleMode.ScaleToFit);
            if (hideHelp) return;
            GUI.Label(new Rect(52, 250, 900, 24), "South play   LS captain / opponent / park   N night   hold C night   Start mode   West training", _tiny);
            if (!training)
                GUI.Label(new Rect(52, 278, 920, 24), "South pitch/swing/catch  LT charge  Y star  stick lead  LB steal  RB cycle  West bunt/jump/slide  East dive  E item", _gold);
            else
                GUI.Label(new Rect(52, 278, 920, 24), "West starts the drills. Prompts sit on the diamond.", _gold);
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
            const float x = 18f;
            const float y = 14f;
            GUI.DrawTexture(new Rect(x, y, 456, 126), _panel);
            GUI.DrawTexture(new Rect(x, y, 6, 126), _ink);
            var half = match.Over ? "FINAL" : (match.Top ? "TOP " : "BOT ") + match.Inning;
            GUI.Label(new Rect(x + 16, y + 6, 180, 20), half, _gold);

            Row(x + 16, y + 30, match.Away, match.AwayScore, match.AwayStars, AwayStripe(match));
            Row(x + 16, y + 62, match.Home, match.HomeScore, match.HomeStars, HomeStripe(match));

            Count(x + 16, y + 96, match.Balls, 4, _dotOn, _dotOff);
            GUI.Label(new Rect(x + 92, y + 96, 18, 18), "B", _tiny);
            Count(x + 118, y + 96, match.Strikes, 3, _dotOn, _dotOff);
            GUI.Label(new Rect(x + 176, y + 96, 18, 18), "S", _tiny);
            Count(x + 200, y + 96, match.Outs, 3, _outOn, _outOff);
            GUI.Label(new Rect(x + 258, y + 96, 18, 18), "O", _tiny);
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
            GUI.Label(new Rect(x + 16, y + 8, 280, 22), "P   " + match.Pitcher.Name, _body);
            Bar(x + 16, y + 32, 220, match.PitcherStamina / 100f);
            GUI.Label(new Rect(x + 244, y + 26, 150, 20), "ARM  " + match.PitcherStamina, _tiny);
            GUI.Label(new Rect(x + 16, y + 48, 380, 22), "AB  " + match.Batter.Name, _body);
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

    public enum PhaseUi { Title, Lineup, Set, Flight, InPlay, Result, GameOver }
}
