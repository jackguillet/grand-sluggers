namespace GrandSluggers.Sim;

/// <summary>
/// Scorebug is the product. Mute it while a special or smash owns the picture.
/// Title, select, lineup, and final still draw.
/// Play HUD anchors are normalized 0–1, Y down (IMGUI). 1P and 2P share one layout (#325).
/// </summary>
public static class BroadcastHud
{
    public static bool MutePlay(bool spectacleActive, double smashSeconds, double freezeSeconds = 0)
        => spectacleActive || smashSeconds > 0 || freezeSeconds > 0;

    /// <summary>Normalized rect. X/Y is top-left. Pixel() scales to a screen.</summary>
    public readonly record struct HudRect(double X, double Y, double W, double H)
    {
        public (double X, double Y, double W, double H) Pixel(double screenW, double screenH) =>
            (X * screenW, Y * screenH, W * screenW, H * screenH);

        public double Right => X + W;
        public double Bottom => Y + H;
    }

    public sealed record PlayLayout(
        HudRect Score,
        HudRect Count,
        HudRect MiniDiamond,
        HudRect BatterCard,
        HudRect PitcherCard,
        HudRect Banner);

    /// <summary>
    /// SMS information architecture: score top-right with S/B/O + diamond on the panel,
    /// batter card bottom-left, pitcher card bottom-right. Seat count must not move them.
    /// </summary>
    public static PlayLayout Layout(int seats = 1)
    {
        _ = seats;
        return Standard;
    }

    /// <summary>
    /// One recipe for 1P and 1v1. Rects keep ≥8px margin at 1280×800 and 1920×1080.
    /// Count and diamond sit on the score panel, not in the sky.
    /// </summary>
    public static readonly PlayLayout Standard = new(
        Score: new(0.695, 0.015, 0.290, 0.248),
        Count: new(0.780, 0.178, 0.190, 0.072),
        MiniDiamond: new(0.705, 0.178, 0.070, 0.072),
        BatterCard: new(0.012, 0.735, 0.260, 0.225),
        PitcherCard: new(0.728, 0.735, 0.260, 0.225),
        Banner: new(0.28, 0.018, 0.40, 0.10));

    /// <summary>In-play YOU tell. Same recipe 1P and 1v1.</summary>
    public static readonly HudRect YouTell = new(0.028125, 0.850, 0.21875, 0.045);

    /// <summary>In-play ITEM pointer. Opposite corner from YOU.</summary>
    public static readonly HudRect ItemTell = new(0.750, 0.850, 0.221875, 0.045);

    public const double FrameMarginPx = 8;

    public static bool InFrame(HudRect r, double screenW, double screenH, double marginPx = FrameMarginPx)
    {
        var (x, y, w, h) = r.Pixel(screenW, screenH);
        return w > 0 && h > 0
            && x >= marginPx
            && y >= marginPx
            && x + w <= screenW - marginPx
            && y + h <= screenH - marginPx;
    }

    public static bool Contains(HudRect outer, HudRect inner) =>
        inner.X >= outer.X - 1e-9
        && inner.Y >= outer.Y - 1e-9
        && inner.Right <= outer.Right + 1e-9
        && inner.Bottom <= outer.Bottom + 1e-9;

    /// <summary>S/B/O belongs on the scorebug: inside the panel, or flush under it.</summary>
    public static bool OnScorebug(HudRect score, HudRect child)
    {
        if (Contains(score, child)) return true;
        var flush = Math.Abs(child.Y - score.Bottom) < 0.002;
        var xOverlap = child.X < score.Right && child.Right > score.X;
        return flush && xOverlap;
    }

    public static HudRect InningMark(HudRect score) =>
        new(score.X + score.W * 0.04, score.Y + score.H * 0.035, score.W * 0.22, score.H * 0.145);

    public static HudRect InningBox(HudRect score, int inning, int innings)
    {
        innings = Math.Max(1, innings);
        inning = Math.Clamp(inning, 1, innings);
        var stripX = score.X + score.W * 0.28;
        var stripY = score.Y + score.H * 0.035;
        var stripW = score.W * 0.68;
        var stripH = score.H * 0.145;
        var slot = stripW / innings;
        var gap = slot * 0.12;
        return new(stripX + (inning - 1) * slot + gap * 0.5, stripY, slot - gap, stripH);
    }

    public static HudRect NameCol(HudRect score, int row)
    {
        var y = score.Y + score.H * (0.20 + row * 0.20);
        return new(score.X + score.W * 0.07, y, score.W * 0.39, score.H * 0.18);
    }

    public static HudRect RunsCol(HudRect score, int row)
    {
        var y = score.Y + score.H * (0.18 + row * 0.20);
        return new(score.X + score.W * 0.52, y, score.W * 0.14, score.H * 0.20);
    }

    public static HudRect StarsCol(HudRect score, int row)
    {
        var y = score.Y + score.H * (0.22 + row * 0.20);
        return new(score.X + score.W * 0.68, y, score.W * 0.28, score.H * 0.14);
    }

    public static HudRect StripeCol(HudRect score, int row)
    {
        var y = score.Y + score.H * (0.22 + row * 0.20);
        return new(score.X + score.W * 0.025, y, score.W * 0.028, score.H * 0.12);
    }

    /// <summary>Captain last name on the bug. Never glued to the run total.</summary>
    public static string BugName(string captainName)
    {
        if (string.IsNullOrWhiteSpace(captainName)) return "";
        var sp = captainName.LastIndexOf(' ');
        return (sp >= 0 ? captainName[(sp + 1)..] : captainName).ToUpperInvariant();
    }

    public static string RunsLabel(int runs) => runs.ToString();

    /// <summary>Couch scorebug. Every field is readable without F2.</summary>
    public sealed record Scorebug(
        int Inning,
        int Innings,
        bool Top,
        bool Over,
        int AwayScore,
        int HomeScore,
        int Outs,
        int Balls,
        int Strikes,
        bool RunnerFirst,
        bool RunnerSecond,
        bool RunnerThird,
        double LeadFirst,
        double LeadSecond,
        double LeadThird,
        int SelectedBag,
        string Pitcher,
        string Batter,
        string Next,
        int OffenseStars,
        int DefenseStars,
        string AwayName,
        string HomeName);

    /// <summary>Banner headline. Kind.ToString() is TAKESTRIKE, not a scorebug.</summary>
    public static string Headline(PlayKind kind) => kind switch
    {
        PlayKind.StolenBase => "STOLEN BASE",
        PlayKind.CaughtStealing => "CAUGHT STEALING",
        PlayKind.HomeRun => "HOME RUN",
        PlayKind.Triple => "TRIPLE",
        PlayKind.Double => "DOUBLE",
        PlayKind.Single => "SINGLE",
        PlayKind.Walk => "WALK",
        PlayKind.Strikeout => "STRIKEOUT",
        PlayKind.FlyOut => "OUT",
        PlayKind.GroundOut => "GROUNDOUT",
        PlayKind.Foul => "FOUL",
        PlayKind.SwingMiss => "SWING AND A MISS",
        PlayKind.TakeStrike => "STRIKE",
        PlayKind.TakeBall => "BALL",
        _ => kind.ToString().ToUpperInvariant()
    };

    public static Scorebug From(Match match)
    {
        if (match == null) throw new ArgumentNullException(nameof(match));
        return new Scorebug(
            match.Inning,
            match.Innings,
            match.Top,
            match.Over,
            match.AwayScore,
            match.HomeScore,
            match.Outs,
            match.Balls,
            match.Strikes,
            match.First is not null,
            match.Second is not null,
            match.Third is not null,
            match.RunnerAt(1)?.Lead01 ?? 0,
            match.RunnerAt(2)?.Lead01 ?? 0,
            match.RunnerAt(3)?.Lead01 ?? 0,
            match.SelectedBag,
            match.Pitcher.Name,
            match.Batter.Name,
            match.OnDeck?.Name ?? "",
            (int)Math.Floor(match.OffenseStars),
            (int)Math.Floor(match.DefenseStars),
            match.Away.Name,
            match.Home.Name);
    }

    /// <summary>Booklet Game Rules spread. Copy a stranger can read without F2.</summary>
    public const int TiredArm = 25;

    public static bool PoorArm(int stamina) => stamina < TiredArm;

    public static string ArmLine(int stamina) =>
        PoorArm(stamina) ? $"ARM  {stamina}  ·  TIRED" : $"ARM  {stamina}";

    public static string ControlDisplay(bool hasGlove, string pos, string name)
    {
        if (!hasGlove || string.IsNullOrWhiteSpace(pos)) return "";
        return string.IsNullOrWhiteSpace(name)
            ? "YOU  " + pos
            : "YOU  " + pos + "  ·  " + name;
    }

    /// <summary>Smart switch tell. Empty when the hint is you, or you have the ball.</summary>
    public static string SwitchTell(string current, string hint, string? hintName, bool hasBall)
    {
        if (hasBall || string.IsNullOrWhiteSpace(hint) || hint == current) return "";
        return string.IsNullOrWhiteSpace(hintName)
            ? "R  →  " + hint
            : "R  →  " + hint + "  ·  " + hintName;
    }

    public static string ItemPointer(bool offered, string? targetName) =>
        offered && !string.IsNullOrWhiteSpace(targetName)
            ? "ITEM  →  " + targetName
            : "";
}
