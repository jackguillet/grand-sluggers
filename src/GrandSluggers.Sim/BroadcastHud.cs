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

    public static Scorebug From(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
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
