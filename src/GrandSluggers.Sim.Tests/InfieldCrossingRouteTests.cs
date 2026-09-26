using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// An infielder plays the ball where it crosses him (§8.2, #580). When no pickup is reachable, the route is the
/// smallest miss, never the ball's resting place in the outfield: the body does not turn and run away from a ball
/// passing in front of it. The outfield takes the ball when it reaches the grass (§8.9).
/// </summary>
public sealed class InfieldCrossingRouteTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    static readonly string[] Infield = { "P", "1B", "2B", "SS", "3B" };
    /// <summary>A step or two of settling around the crossing point, and the glove's reach.</summary>
    const double SlackFt = 12;

    /// <summary>The four rows the #580 sweep pinned on main: each chaser ran 90–160 ft past where the ball crossed him.</summary>
    [Theory]
    [InlineData(80, 11, 0)]
    [InlineData(70, 8, 30)]
    [InlineData(105, -7, 0)]
    [InlineData(110, -7, -30)]
    public void TheChaserNeverRunsPastWhereTheBallCrossesHim(double exit, double launch, double spray) =>
        AssertNoDeepRun(exit, launch, spray);

    [Fact]
    public void NoInfieldChaseAcrossTheInfieldGridRunsPastTheCrossing()
    {
        var failures = new List<string>();
        foreach (var exit in new[] { 70d, 85d, 100d, 115d, 125d })
        foreach (var launch in new[] { -10d, -4d, 2d, 8d, 14d, 20d })
        foreach (var spray in new[] { -40d, -20d, 0d, 20d, 40d })
            if (DeepRun(exit, launch, spray) is { } why) failures.Add(why);
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    static void AssertNoDeepRun(double exit, double launch, double spray)
    {
        var why = DeepRun(exit, launch, spray);
        Assert.True(why is null, why);
    }

    /// <summary>
    /// Plays the ball with CPU seats. While the first infield glove keeps the ball, its body may not stand deeper than the
    /// deeper of its start and the ball's nearest playable point (a sample at catch height before the bounce, or on the ground
    /// after it) plus <see cref="SlackFt"/>. Null when it holds.
    /// </summary>
    static Team Nine(params string[] ids) =>
        new(PresetTeams.TeamName(Game.Must(ids[0])), Game.Must(ids[0]), ids.Select(Game.Must).ToList());

    static string? DeepRun(double exit, double launch, double spray)
    {
        // The nines the #580 sweep pinned, named, so the route rule is tested on the same bodies whatever the auto-fill picks.
        var match = Match.Exhibition(Game, Nine("rio", "nico", "pip", "gull", "marlow", "tad", "tumble", "cattail", "lace"),
            Nine("ashlord", "grit", "cinder", "soot", "sirocco", "scree", "frost", "adobe", "cairn"));
        var rules = match.Rules;
        var hit = FlightFixtures.Hit(match.Park, exit, launch, spray, rules: rules);
        if (hit.Foul || hit.HomeRun) return null;
        var pre = match.PreviewHit(hit);
        if (FieldingResolver.IsOutfield(pre.Position)) return null;
        var live = match.LivePlay;
        live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, pre, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu));
        var pos = live.GlovePos;
        if (Array.IndexOf(Infield, pos) < 0) return null;
        var start = live.Fielders.TryGetValue(pos, out var s) ? s : (live.GloveX, live.GloveZ);
        var path = live.Path!;
        var hang = BallFlight.HangTime(path, rules);
        var near = path
            .Where(p => !(p.T < hang && p.Height > rules.Fielding.Catch.StandingHeightFt))
            .MinBy(p => Diamond.Dist(start.Item1, start.Item2, p.X, p.Z));
        // A ball that gets past the glove where it cannot be played — before the glove may move (the pitcher inside his
        // delivery recovery, §8.2, §8.6) or over its head — is no route choice: the body chases a ball behind it until the
        // outfield takes it at the lip (§8.9). That chase is its own question (it showed once Rio's Grow ring left the pool,
        // AB-12), not the crossing this test pins.
        var ready = FieldingResolver.CpuReactionLockouts(rules, pre.Grounder ? null : hang)[pos];
        var reach = FieldingResolver.CatchRadiusFt(pre.Fielder, match.Park, rules, air: !pre.Grounder);
        var startDepth = FieldBounds.DistHome(start.Item1, start.Item2);
        if (path.FirstOrDefault(p => FieldBounds.DistHome(p.X, p.Z) > startDepth + reach) is { } by
            && (by.T < ready || by.Height > rules.Fielding.Catch.StandingHeightFt)) return null;
        var allowed = Math.Max(FieldBounds.DistHome(start.Item1, start.Item2), FieldBounds.DistHome(near.X, near.Z)) + SlackFt;
        for (var i = 0; i < 180 && live.GlovePos == pos; i++)
        {
            var deep = FieldBounds.DistHome(live.GloveX, live.GloveZ);
            if (deep > allowed)
                return $"{exit} mph {launch}° {spray}°: {pos} at {deep:0} ft from home by {live.ElapsedSeconds:0.00} s; " +
                       $"the ball's nearest playable point is {FieldBounds.DistHome(near.X, near.Z):0} ft (start {FieldBounds.DistHome(start.Item1, start.Item2):0} ft)";
            if (live.Apply(LivePlayCommand.Tick(1 / 60.0, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay is not null) break;
        }
        return null;
    }
}
