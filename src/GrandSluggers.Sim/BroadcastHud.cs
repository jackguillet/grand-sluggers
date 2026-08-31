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
    }

    public sealed record PlayLayout(
        HudRect Score,
        HudRect Count,
        HudRect MiniDiamond,
        HudRect BatterCard,
        HudRect PitcherCard,
        HudRect Banner);

    /// <summary>
    /// SMS information architecture: score top-right, S/B/O + diamond under it,
    /// batter card bottom-left, pitcher card bottom-right. Seat count must not move them.
    /// </summary>
    public static PlayLayout Layout(int seats = 1)
    {
        _ = seats;
        return Standard;
    }

    public static readonly PlayLayout Standard = new(
        Score: new(0.70, 0.018, 0.28, 0.14),
        Count: new(0.78, 0.165, 0.20, 0.08),
        MiniDiamond: new(0.70, 0.165, 0.08, 0.08),
        BatterCard: new(0.012, 0.78, 0.26, 0.20),
        PitcherCard: new(0.728, 0.78, 0.26, 0.20),
        Banner: new(0.28, 0.018, 0.40, 0.10));

    /// <summary>Couch scorebug. Every field is readable without F2.</summary>
    public sealed record Scorebug(
        int Inning,
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

    public static string ItemPointer(bool offered, string? targetName) =>
        offered && !string.IsNullOrWhiteSpace(targetName)
            ? "ITEM  →  " + targetName
            : "";
}
