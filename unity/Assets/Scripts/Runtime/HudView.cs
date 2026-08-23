using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public static class HudView
    {
        static GUIStyle _title, _h1, _body, _gold, _tiny;
        static Texture2D _panel, _pipOn, _pipOff;

        public static void Draw(
            Match match, PhaseUi phase, string parkName, string homeCap, string awayCap,
            bool challenge, string[] pitches, int pitchIndex, bool star, bool steal, bool item,
            float charge, float timing, bool showTiming, string banner, string sub, Texture2D portrait)
        {
            Ensure();
            if (phase == PhaseUi.Title)
            {
                Title(parkName, homeCap, awayCap, challenge, portrait);
                return;
            }
            if (phase == PhaseUi.Lineup)
            {
                Lineup(match);
                return;
            }
            if (phase == PhaseUi.GameOver)
            {
                Final(match);
                return;
            }
            Play(match, pitches, pitchIndex, star, steal, item, charge, timing, showTiming, banner, sub);
        }

        static void Title(string park, string home, string away, bool challenge, Texture2D portrait)
        {
            GUI.DrawTexture(new Rect(40, 40, 520, 70), _panel);
            GUI.Label(new Rect(56, 48, 500, 54), "GRAND SLUGGERS", _title);
            GUI.Label(new Rect(56, 120, 700, 28), challenge ? "CHALLENGE" : "EXHIBITION  ·  Harbor first", _gold);
            GUI.Label(new Rect(56, 160, 800, 28), home + "  vs  " + away, _h1);
            GUI.Label(new Rect(56, 196, 800, 24), park, _body);
            if (portrait != null)
                GUI.DrawTexture(new Rect(Screen.width - 360, 40, 320, 320), portrait, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(56, 250, 900, 24), "A/D captain   W/S opponent   C park   SPACE play", _tiny);
            GUI.Label(new Rect(56, 278, 900, 24), "South pitch/swing/catch   LT charge   Y star   LB steal   RB banana", _tiny);
        }

        static void Lineup(Match match)
        {
            GUI.Label(new Rect(40, 36, 1000, 32), "TEAM SHEET  ·  " + match.Park.Name, _h1);
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

        static void Final(Match match)
        {
            var mvp = match.Mvp();
            GUI.Label(new Rect(60, 80, 800, 40), "FINAL", _gold);
            GUI.Label(new Rect(60, 130, 900, 40), match.Away.Name + "  " + match.AwayScore, _h1);
            GUI.Label(new Rect(60, 178, 900, 40), match.Home.Name + "  " + match.HomeScore, _h1);
            GUI.Label(new Rect(60, 240, 900, 32), "MVP  " + mvp.Who.Name + "  (" + mvp.Points + ")", _gold);
            GUI.Label(new Rect(60, 280, 900, 22), mvp.Why, _body);
            GUI.Label(new Rect(60, 330, 900, 22), "SPACE  continue", _tiny);
        }

        static void Play(Match match, string[] pitches, int pi, bool star, bool steal, bool item,
            float charge, float timing, bool showTiming, string banner, string sub)
        {
            GUI.DrawTexture(new Rect(18, 14, 420, 118), _panel);
            var half = match.Over ? "FINAL" : (match.Top ? "TOP " : "BOT ") + match.Inning;
            GUI.Label(new Rect(30, 20, 400, 22), half, _gold);
            GUI.Label(new Rect(30, 44, 400, 26),
                Short(match.Away) + " " + match.AwayScore + "     " + Short(match.Home) + " " + match.HomeScore, _h1);
            GUI.Label(new Rect(30, 74, 400, 20),
                "B " + match.Balls + "   S " + match.Strikes + "   O " + match.Outs + "    arm " + match.PitcherStamina, _tiny);
            Stars(30, 98, match.AwayStars);
            Stars(210, 98, match.HomeStars);

            GUI.DrawTexture(new Rect(Screen.width - 430, 14, 410, 88), _panel);
            GUI.Label(new Rect(Screen.width - 416, 22, 390, 22), "P   " + match.Pitcher.Name, _body);
            GUI.Label(new Rect(Screen.width - 416, 46, 390, 22), "AB  " + match.Batter.Name, _body);
            var extra = (star ? "  STAR" : "") + (steal ? "  STEAL" : "") + (item ? "  ITEM" : "");
            GUI.Label(new Rect(Screen.width - 416, 70, 390, 22), pitches[pi] + extra, star ? _gold : _tiny);

            if (showTiming)
            {
                var x = Screen.width / 2 - 210;
                var y = Screen.height - 70;
                GUI.DrawTexture(new Rect(x, y, 420, 44), _panel);
                GUI.DrawTexture(new Rect(x + 16 + 175, y + 12, 70, 20), _pipOn);
                var pip = x + 16 + (int)(Mathf.Clamp01(timing) * 388);
                GUI.DrawTexture(new Rect(pip - 3, y + 8, 6, 28), _pipOff);
                GUI.DrawTexture(new Rect(x + 16, y + 34, (int)(388 * Mathf.Clamp01(charge)), 6), _pipOn);
            }

            if (!string.IsNullOrEmpty(banner))
            {
                var w = 480;
                GUI.DrawTexture(new Rect(Screen.width / 2 - w / 2, Screen.height / 2 - 52, w, 88), _panel);
                GUI.Label(new Rect(Screen.width / 2 - w / 2 + 16, Screen.height / 2 - 44, w - 32, 40), banner, _h1);
                if (!string.IsNullOrEmpty(sub))
                    GUI.Label(new Rect(Screen.width / 2 - w / 2 + 16, Screen.height / 2, w - 32, 24), sub, _gold);
            }
        }

        static void Stars(int x, int y, double n)
        {
            for (var i = 0; i < 5; i++)
                GUI.DrawTexture(new Rect(x + i * 18, y, 14, 14), n > i ? _pipOn : _pipOff);
        }

        static string Short(Team t)
        {
            var n = t.Captain.Name;
            var sp = n.LastIndexOf(' ');
            return (sp >= 0 ? n.Substring(sp + 1) : n).ToUpperInvariant();
        }

        static void Ensure()
        {
            if (_title != null) return;
            _title = Sty(42, new Color(1f, 0.85f, 0.2f), FontStyle.Bold);
            _h1 = Sty(26, Color.white, FontStyle.Bold);
            _body = Sty(18, new Color(0.95f, 0.96f, 0.97f), FontStyle.Normal);
            _gold = Sty(20, new Color(1f, 0.82f, 0.25f), FontStyle.Bold);
            _tiny = Sty(15, new Color(0.85f, 0.88f, 0.9f), FontStyle.Normal);
            _panel = Tex(new Color(0.07f, 0.08f, 0.1f, 0.72f));
            _pipOn = Tex(new Color(1f, 0.8f, 0.15f, 1f));
            _pipOff = Tex(new Color(1f, 1f, 1f, 0.85f));
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

    public enum PhaseUi { Title, Lineup, Set, Flight, InPlay, Result, GameOver }
}
