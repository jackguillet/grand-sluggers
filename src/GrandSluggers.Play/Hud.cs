using GrandSluggers.Sim;
using Raylib_cs;

namespace GrandSluggers.Play;

public static class Hud
{
    public static void Draw(
        Match match,
        string pitchType,
        bool starArmed,
        float charge,
        float timing01,
        bool showTiming,
        string banner,
        string sub,
        bool itemArmed = false)
    {
        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();

        DrawScorebug(match, 24, 18);
        DrawStars(match, 24, 108);
        DrawCount(match, 24, 168);
        DrawStamina(match, 24, 230);
        DrawMatchup(match, w - 420, 18);
        if (showTiming)
            DrawTiming(w / 2 - 220, h - 120, charge, timing01, starArmed, pitchType);
        else
            DrawHelp(w / 2 - 280, h - 70, match, pitchType, starArmed, itemArmed);

        if (!string.IsNullOrEmpty(banner))
        {
            var tw = Raylib.MeasureText(banner, 42);
            Raylib.DrawRectangle(w / 2 - tw / 2 - 24, h / 2 - 70, tw + 48, 84, Palette.Fade(Palette.HudInk, 180));
            Raylib.DrawText(banner, w / 2 - tw / 2, h / 2 - 48, 42, Palette.HudPaper);
            if (!string.IsNullOrEmpty(sub))
            {
                var sw = Raylib.MeasureText(sub, 20);
                Raylib.DrawText(sub, w / 2 - sw / 2, h / 2 + 28, 20, Palette.Gold);
            }
        }
    }

    public static void DrawTitle(
        int w, int h,
        ContentCatalog content,
        string homeId,
        string awayId,
        string parkId,
        bool two,
        bool challenge,
        Challenge? campaign)
    {
        Raylib.DrawText("GRAND SLUGGERS", 80, 72, 64, Palette.Spark);
        Raylib.DrawText("A 3-inning party baseball game. Chemistry is the draft.", 84, 148, 20, Palette.HudInk);

        var mode = challenge ? "CHALLENGE" : "EXHIBITION";
        Raylib.DrawText($"{mode}   H to switch", 84, 188, 26, challenge ? Palette.Gold : Palette.SparkDark);

        var home = content.Must(homeId);
        var away = content.Must(awayId);
        var park = content.Parks.TryGetValue(parkId, out var pk) ? pk.Name : parkId;
        Raylib.DrawText($"YOU   {home.Name}   {PresetTeams.TeamName(home)}", 84, 230, 24, Palette.Body(home.Faction));
        Raylib.DrawText($"VS    {away.Name}   {PresetTeams.TeamName(away)}", 84, 262, 24, Palette.Body(away.Faction));
        Raylib.DrawText($"Park  {park}", 84, 294, 22, Palette.HudInk);

        if (challenge && campaign is not null)
        {
            var owned = campaign.Owned.Count;
            var beaten = campaign.Beaten.Count;
            Raylib.DrawText($"Roster {owned}   captains beaten {beaten}/5", 84, 326, 20, Palette.Gold);
        }

        Raylib.DrawRectangle(80, 360, 680, 250, Palette.Fade(Palette.HudPaper, 210));
        Raylib.DrawText("A/D            your captain  (park stays)", 100, 376, 22, Palette.HudInk);
        Raylib.DrawText("W/S            opponent  (exhibition)", 100, 404, 22, Palette.HudInk);
        Raylib.DrawText("C              cycle park (not tied to captain)     T  2P", 100, 432, 22, Palette.HudInk);
        Raylib.DrawText("SPACE / A      pitch, swing, catch", 100, 460, 22, Palette.HudInk);
        Raylib.DrawText("SHIFT charge   WASD field   1 2 3 H throw", 100, 488, 22, Palette.HudInk);
        Raylib.DrawText("F buddy   R pitcher   Q star   X steal   E banana", 100, 516, 22, Palette.HudInk);
        Raylib.DrawText("P2: IJKL move, ENTER swing, P star, ; jump", 100, 544, 20, Palette.HudInk);
        var play = challenge ? "Press SPACE  —  recruit after a win" : "Press SPACE to play";
        Raylib.DrawText(play, 84, 630, 28, Palette.SparkDark);
        var who = two ? "2 PLAYER  (P1 home, P2 away)" : "1 PLAYER  (you bat last)";
        Raylib.DrawText(who, 84, 668, 20, Palette.HudInk);
    }

    public static void DrawLineup(Match match, int w)
    {
        Raylib.DrawText("TEAM SHEET", 64, 36, 36, Palette.HudInk);
        Raylib.DrawText($"{match.Park.Name}  ·  {match.Innings} innings", 66, 80, 20, Palette.HudInk);
        Raylib.DrawText($"{match.Home.Name}  bat {match.HomeBat.Name}  glove {match.HomeGlove.Name}   [B] bat  [G] glove", 66, 104, 18, Palette.Body(match.Home.Captain.Faction));
        Raylib.DrawText($"{match.Away.Name}  bat {match.AwayBat.Name}  glove {match.AwayGlove.Name}   [N] bat  [M] glove", 66, 126, 18, Palette.Body(match.Away.Captain.Faction));
        DrawRoster(match, match.Home, true, 64, 160);
        DrawRoster(match, match.Away, false, 640, 160);
        Raylib.DrawText("Good chemistry throws faster. A stacked group starts with more stars.", 64, 640, 20, Palette.HudInk);
        Raylib.DrawText("SPACE / A to throw out the first pitch", 64, 672, 22, Palette.SparkDark);
    }

    public static void DrawGameOver(Match match, int w, int h, Challenge? campaign)
    {
        var mvp = match.Mvp();
        Raylib.DrawRectangle(0, 0, w, h, Palette.Fade(Palette.Night, 120));
        Raylib.DrawText("FINAL", 80, 80, 28, Palette.Gold);
        Raylib.DrawText($"{match.Away.Name}  {match.AwayScore}", 80, 130, 42, Palette.Body(match.Away.Captain.Faction));
        Raylib.DrawText($"{match.Home.Name}  {match.HomeScore}", 80, 184, 42, Palette.Body(match.Home.Captain.Faction));
        Raylib.DrawText($"MVP  {mvp.Who.Name}", 80, 280, 36, Palette.Gold);
        Raylib.DrawText($"{mvp.Points} pts - {mvp.Why}", 84, 328, 22, Palette.HudPaper);
        if (campaign is not null)
        {
            if (campaign.LastWin && campaign.LastRecruit is { } who)
                Raylib.DrawText($"{who.Name} joins {match.Home.Name}!", 80, 380, 28, Palette.Gold);
            else if (campaign.LastWin && campaign.AllBeaten)
                Raylib.DrawText("Island tour done. Every captain beaten.", 80, 380, 24, Palette.Gold);
            else if (!campaign.LastWin)
                Raylib.DrawText("No recruit. Win it to add their role player.", 80, 380, 22, Palette.HudPaper);
            var next = campaign.AllBeaten ? "SPACE  title" : campaign.LastWin ? "SPACE  next rival" : "SPACE  rematch";
            Raylib.DrawText(next, 80, 430, 22, Palette.HudPaper);
        }
        else
            Raylib.DrawText("SPACE / A  play again     ESC  quit", 80, 420, 22, Palette.HudPaper);
    }

    static void DrawRoster(Match match, Team team, bool home, int x, int y)
    {
        var stars = home ? match.HomeStars : match.AwayStars;
        var color = Palette.Body(team.Captain.Faction);
        Raylib.DrawText($"{team.Name}   stars {stars:0.#}/5", x, y, 24, color);
        var yy = y + 40;
        foreach (var c in home ? match.HomeOrder : match.AwayOrder)
        {
            var rel = match.Chemistry.Between(team.Captain, c);
            var mark = c.Id == team.Captain.Id ? "C" : rel == Chemistry.Good ? "+" : rel == Chemistry.Bad ? "-" : " ";
            var line = $"{mark} {c.Name}   B {c.Stats.Bat}  P {c.Stats.Pitch}  F {c.Stats.Field}  R {c.Stats.Run}";
            var ink = rel == Chemistry.Good ? Palette.C(20, 110, 70)
                : rel == Chemistry.Bad ? Palette.Bad
                : Palette.HudInk;
            Raylib.DrawText(line, x, yy, 18, ink);
            yy += 26;
        }
    }

    static void DrawScorebug(Match match, int x, int y)
    {
        Raylib.DrawRectangle(x, y, 360, 78, Palette.Fade(Palette.HudInk, 200));
        var half = match.Over ? "FINAL" : $"{(match.Top ? "TOP" : "BOT")} {match.Inning}";
        Raylib.DrawText(half, x + 12, y + 8, 18, Palette.Gold);
        Raylib.DrawText($"{Short(match.Away)} {match.AwayScore}", x + 12, y + 32, 22, Palette.Body(match.Away.Captain.Faction));
        Raylib.DrawText($"{Short(match.Home)} {match.HomeScore}", x + 180, y + 32, 22, Palette.Body(match.Home.Captain.Faction));
    }

    static void DrawStars(Match match, int x, int y)
    {
        Raylib.DrawText("AWAY", x, y, 14, Palette.HudInk);
        Pips(x + 60, y, match.AwayStars);
        Raylib.DrawText("HOME", x, y + 24, 14, Palette.HudInk);
        Pips(x + 60, y + 24, match.HomeStars);
    }

    static void Pips(int x, int y, double stars)
    {
        for (var i = 0; i < 5; i++)
        {
            var filled = stars > i;
            Raylib.DrawRectangle(x + i * 18, y, 14, 14, filled ? Palette.Gold : Palette.C(200, 200, 204));
        }
    }

    static void DrawCount(Match match, int x, int y)
    {
        Raylib.DrawText($"B {match.Balls}   S {match.Strikes}   O {match.Outs}", x, y, 22, Palette.HudInk);
        var bags = $"{(match.First is null ? "-" : "1")} {(match.Second is null ? "-" : "2")} {(match.Third is null ? "-" : "3")}";
        Raylib.DrawText($"runners {bags}{(match.StealOn ? "  STEAL" : "")}", x, y + 28, 18,
            match.StealOn ? Palette.Gold : Palette.HudInk);
    }

    static void DrawStamina(Match match, int x, int y)
    {
        Raylib.DrawText($"P {match.Pitcher.Name}  arm {match.PitcherStamina}", x, y, 18, Palette.HudInk);
        Raylib.DrawRectangle(x, y + 22, 160, 10, Palette.C(200, 200, 204));
        Raylib.DrawRectangle(x, y + 22, (int)(160 * (match.PitcherStamina / 100.0)), 10,
            match.PitcherStamina < 25 ? Palette.Bad : Palette.Good);
        Raylib.DrawText(match.Park.Name, x, y + 38, 16, Palette.HudInk);
    }

    static void DrawMatchup(Match match, int x, int y)
    {
        Raylib.DrawRectangle(x, y, 400, 96, Palette.Fade(Palette.HudPaper, 220));
        Raylib.DrawText($"P  {match.Pitcher.Name}", x + 14, y + 12, 22, Palette.HudInk);
        Raylib.DrawText($"AB {match.Batter.Name}", x + 14, y + 42, 22, Palette.HudInk);
        var chem = match.Chemistry.Between(match.Batter, match.OnDeck!);
        var item = chem == Chemistry.Good ? "on-deck buddy  E banana" : $"on deck  {match.OnDeck?.Name}";
        Raylib.DrawText(item, x + 14, y + 70, 16, chem == Chemistry.Good ? Palette.C(20, 110, 70) : Palette.HudInk);
    }

    static void DrawTiming(int x, int y, float charge, float timing01, bool star, string pitch)
    {
        Raylib.DrawRectangle(x, y, 440, 70, Palette.Fade(Palette.HudInk, 200));
        Raylib.DrawRectangle(x + 16, y + 28, 408, 18, Palette.C(60, 60, 70));
        Raylib.DrawRectangle(x + 16 + 170, y + 28, 68, 18, Palette.Good);
        var pip = x + 16 + (int)(Math.Clamp(timing01, 0, 1) * 408);
        Raylib.DrawRectangle(pip - 3, y + 20, 6, 34, Palette.HudPaper);
        Raylib.DrawRectangle(x + 16, y + 52, (int)(408 * Math.Clamp(charge, 0, 1)), 8, Palette.Gold);
        var label = star ? $"STAR  {pitch}" : pitch;
        Raylib.DrawText(label, x + 16, y + 6, 16, star ? Palette.Gold : Palette.HudPaper);
    }

    static void DrawHelp(int x, int y, Match match, string pitch, bool star, bool itemArmed)
    {
        var fielding = match.Top;
        var steal = match.StealOn ? " STEAL ON" : match.CanSteal ? "  X steal" : "";
        var item = itemArmed ? "  BANANA ARMED" : "";
        var line = fielding
            ? $"PITCH  {pitch}{(star ? "  *" : "")}   SHIFT charge   TAB change   Q {match.Pitcher.StarPitch}"
            : $"SWING   SHIFT charge   Q {match.Batter.StarSwing}{(star ? " ARMED" : "")}   A/D spray{steal}{item}";
        Raylib.DrawText(line, x, y, 18, Palette.HudInk);
    }

    static string Short(Team team)
    {
        var n = team.Captain.Name;
        var sp = n.LastIndexOf(' ');
        return (sp >= 0 ? n[(sp + 1)..] : n).ToUpperInvariant();
    }
}
