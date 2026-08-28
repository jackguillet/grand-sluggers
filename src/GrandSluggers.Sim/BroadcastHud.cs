namespace GrandSluggers.Sim;

/// <summary>
/// Scorebug is the product. Mute it while a special or smash owns the picture.
/// Title, select, lineup, and final still draw.
/// </summary>
public static class BroadcastHud
{
    public static bool MutePlay(bool spectacleActive, double smashSeconds, double freezeSeconds = 0)
        => spectacleActive || smashSeconds > 0 || freezeSeconds > 0;

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
        PlayKind.TakeStrike => "TAKE STRIKE",
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
            match.Pitcher.Name,
            match.Batter.Name,
            match.OnDeck?.Name ?? "",
            (int)Math.Floor(match.OffenseStars),
            (int)Math.Floor(match.DefenseStars),
            match.Away.Name,
            match.Home.Name);
    }
}
