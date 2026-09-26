namespace GrandSluggers.Sim;

/// <summary>
/// The tells a special shows (spec §13, AB-13, AB-C15): which procedural stand-in draws each catalog VFX slot, and the
/// instants a tell beats on, read from the special's own row and the live ball. Presentation only: nothing here decides a
/// play. A slot in <c>data/art/vfx.json</c> names its tell by <c>tell</c> (one of <see cref="Builders"/>) and its sound by
/// <c>cue</c> (an <c>data/art/audio.json</c> slot); an Art session fills the slot's folder and the stand-in steps aside.
/// </summary>
public static class SpecialTells
{
    /// <summary>
    /// The procedural stand-ins the client can draw. A new tell is a name here plus its builder in the client
    /// (<c>SpecialFx</c>), never a switch on a special's id.
    /// </summary>
    public static readonly IReadOnlyList<string> Builders =
    [
        "spark-tail",     // gold sparks trail the ball; the rise lights them (Skyrocket)
        "spark-ring",     // a ring of sparks at the Perfect ring on the contact oval (Sparkler)
        "aurora-ribbon",  // a ribbon traces the path the ball swayed along (Aurora Ribbon)
        "follow-spot",    // a light cone on the one fielder the pause holds (Follow Spot)
        "coaster-track",  // a coaster track laid along the loop (Loop-the-Loop)
        "spinning-top",   // the stalled ball spins in place (Spinning Top)
        "card-flip",      // a card flips over the ball at the switch (Phonyball)
        "card-decoy",     // a card-back ball flies beside the real one until the apex (Double Deal)
        "vine",           // a vine from the pendulum's pivot to the ball (Vine Swing)
        "bolt-trail",     // a lightning bolt along the jagged flight (Lightning Liner)
        "anvil",          // a glowing ball that clangs and turns to cold iron at the turn (Anvil)
        "molten",         // the ball glows and steams while it is molten (Hot Iron)
        "wave-ring",      // waves wash out across the undertow ring at home (Undertow)
        "tall-oval",      // the contact oval stretches up and down, capped like a staff (Driftwood Reach)
        "shimmer",        // heat shimmer where the ball vanished and where it comes back (Mirage)
        "dust-swirl",     // dust swirls over the bowl (Dust Bowl)
        "cable-line",     // a cable from the station to the plate (Cable Car)
        "snow-flurry",    // snow bursts at the fly's apex (Summit Gust)
        "splash-ring",    // a splash ring on the dirt at each skip (Skipping Stone)
        "lily-pad",       // a lily pad under the ball as it hops a glove (Lily Hop)
    ];

    public static bool IsBuilder(string? name) =>
        !string.IsNullOrEmpty(name) && Builders.Contains(name, StringComparer.Ordinal);

    /// <summary>
    /// The instants, as time fractions of the flight, at which a star pitch's tell beats (a sound, a flash): where the rise
    /// starts, the loop enters, the phony switch comes, the anvil turns, the cable car stops, the ball vanishes and comes
    /// back, and each skip meets the dirt. Read from the row and the rules, never typed again; empty for a pitch with none.
    /// </summary>
    public static IReadOnlyList<double> PitchBeats(StarPitchSkill? row, RulesTable rules)
    {
        var beats = new List<double>();
        if (row is null) return beats;
        if (row.Rise is { } rise) beats.Add(rise.From);
        if (row.Loop is { } loop) beats.Add(loop.At);
        if (row.Decoy) beats.Add(rules.Pitching.StarShapes.PhonyballSwitchAt);
        if (row.Drop is { } drop) beats.Add(drop.From);
        if (row.Hitch is { } hitch) beats.Add(hitch.At);
        if (row.Vanish is { } vanish)
        {
            beats.Add(vanish.From);
            beats.Add(vanish.To);
        }
        if (row.Skips is { } skips)
        {
            beats.Add(skips.FirstAt);
            beats.Add(skips.SecondAt);
        }
        beats.Sort();
        return beats;
    }

    /// <summary>A beat at <paramref name="at"/> fires on the one frame the clock passes it: from before it to on or past it.</summary>
    public static bool Crossed(double before, double now, double at) => before < at && now >= at;

    /// <summary>
    /// The play second a batted ball tops out: the highest sample before its first landing mark (the whole path when it has
    /// none). A decoy flies until then; a gust bursts there. 0 for an empty path.
    /// </summary>
    public static double ApexT(IReadOnlyList<Sample>? path)
    {
        if (path is null || path.Count == 0) return 0;
        var land = BallFlight.LandingIndex(path);
        var end = land < 0 ? path.Count - 1 : land;
        var best = 0;
        for (var i = 1; i <= end; i++)
            if (path[i].Height > path[best].Height)
                best = i;
        return path[best].T;
    }

    /// <summary>
    /// The play second a jagged ball is back on its line for good (<see cref="BallJag"/>): the end of its second jag, as a
    /// share of the jag window. The bolt ends there and the thunder follows. 0 when the path has no window.
    /// </summary>
    public static double JagDoneT(IReadOnlyList<Sample>? path, BallJag jag)
    {
        if (path is null || path.Count == 0) return 0;
        return BallJag.WindowSec(path) * Math.Min(1, jag.SecondAt + jag.Span);
    }
}
