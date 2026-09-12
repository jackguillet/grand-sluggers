using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>Spec §1 / B.6 rows S-80 … S-82: walk-off on every path, extra innings to the cap, mercy.</summary>
public sealed class InningsScenarioTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    static readonly PitchCommand Paint = new("fastball", 0, false);
    static readonly SwingCommand Take = new(false, 0, 0, false);
    static readonly PitchCommand Wide = new("fastball", 0, false, AimX: 1.5);

    [Fact]
    public void S80_HomeLeadingAfterTheTopOfTheLastInningSkipsTheBottom()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        EndHalf(match);                   // top 1
        Slam(match);                      // bottom 1: home 4
        EndHalf(match);                   // bottom 1 done
        EndHalf(match);                   // top 2
        EndHalf(match);                   // bottom 2
        Assert.Equal((3, true, 4), (match.Inning, match.Top, match.HomeScore));
        EndHalf(match);                   // top 3: three outs with home ahead
        Assert.True(match.Over);
        Assert.True(match.Top, "the bottom of the last inning is never played when home already leads");
        Assert.True(match.HomeScore > match.AwayScore);
    }

    [Fact]
    public void S80_WalkOffOnTheSwingPathAndOnTheTakePath()
    {
        // Swing path: tied in the bottom of the last inning, a homer ends it before three outs.
        var swing = Match.Slice(_content, innings: 3, seed: 1);
        for (var half = 0; half < 5; half++) EndHalf(swing);
        Assert.Equal((3, false, 0), (swing.Inning, swing.Top, swing.Outs));
        Assert.Equal(swing.AwayScore, swing.HomeScore);
        var stationed = Slam(swing);
        Assert.True(stationed, "the slam landed");
        Assert.True(swing.Over, "walk-off on the swing path");
        Assert.True(swing.HomeScore > swing.AwayScore);

        // Take path: bases loaded, ball four walks in the winning run.
        var take = Match.Slice(_content, innings: 3, seed: 1);
        for (var half = 0; half < 5; half++) EndHalf(take);
        Assert.Equal((3, false), (take.Inning, take.Top));
        for (var bag = 1; bag <= 3; bag++) Assert.True(take.StationRunner(bag, take.HomeOrder[bag + 3]));
        PlayEvent? ev = null;
        // Four wide pitches: ball four, or the body if the hitter stands on that side (a hit by pitch is the same path).
        for (var i = 0; i < 4 && !take.Over; i++) ev = take.Play(Wide, Take);
        Assert.True(ev!.Kind is PlayKind.Walk or PlayKind.HitByPitch, ev.Kind.ToString());
        Assert.True(take.Over, "walk-off on the take path");
        Assert.True(take.HomeScore > take.AwayScore);
    }

    [Fact]
    public void S81_ATieAfterTheLastInningPlaysExtraInningsToTheCapThenTies()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var cap = match.Rules.Match.ExtraInningsCap;
        for (var half = 0; half < 6; half++) EndHalf(match);
        Assert.False(match.Over, "a tie after the scheduled innings plays on");
        Assert.Equal((4, true), (match.Inning, match.Top));
        Assert.True(match.ExtraInnings);
        while (!match.Over) EndHalf(match);
        Assert.Equal(3 + cap, match.Inning);
        Assert.Equal(match.AwayScore, match.HomeScore);
        Assert.False(match.Top, "the cap's bottom half was played out");
    }

    [Fact]
    public void S81_ALeadAfterACompleteExtraInningEndsIt()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        for (var half = 0; half < 6; half++) EndHalf(match);
        Assert.Equal((4, true), (match.Inning, match.Top));
        Assert.True(Slam(match), "away scores in the top of the fourth");
        EndHalf(match);
        Assert.False(match.Over, "home still bats the bottom");
        EndHalf(match);
        Assert.True(match.Over);
        Assert.True(match.AwayScore > match.HomeScore);
    }

    [Theory]
    [InlineData(6, true)]     // a six-inning game with mercy on ends at the end of the third
    [InlineData(6, false)]    // off when the match says so: it plays on
    public void S82_ATenRunLeadAfterTheTrailingSideBatsInTheThirdIsMercy(int innings, bool mercy)
    {
        var park = _content.Parks["harbor-diamond"];
        var match = new Match(_content, PresetTeams.EmberCourt(_content), PresetTeams.SparkAllStars(_content), park, innings, seed: 1, mercy: mercy);
        var m = match.Rules.Match.Mercy;
        for (var half = 0; half < 4; half++) EndHalf(match);
        Assert.Equal((3, true), (match.Inning, match.Top));
        for (var slam = 0; slam < 3; slam++) Assert.True(Slam(match));
        Assert.True(match.AwayScore - match.HomeScore >= m.Runs);
        EndHalf(match);                   // top of the third: the leading side just batted, no mercy yet
        Assert.False(match.Over, "the trailing side has not batted in the third yet");
        Assert.False(match.Top);
        EndHalf(match);                   // bottom of the third: the trailing side batted
        Assert.Equal(mercy, match.Over);
        if (!mercy) Assert.Equal((4, true), (match.Inning, match.Top));
    }

    [Fact]
    public void S82_MercyIsOffForAThreeInningGame()
    {
        var park = _content.Parks["harbor-diamond"];
        var match = new Match(_content, PresetTeams.EmberCourt(_content), PresetTeams.SparkAllStars(_content), park, innings: 3, seed: 1, mercy: true);
        for (var half = 0; half < 4; half++) EndHalf(match);
        for (var slam = 0; slam < 3; slam++) Assert.True(Slam(match));
        Assert.False(match.MercyEnds(match.AwayScore - match.HomeScore), "three innings: the rule never reads");
        EndHalf(match);
        Assert.False(match.Over, "the bottom of the third is still played");
        EndHalf(match);
        Assert.True(match.Over, "and the game ends on innings, not on mercy");
    }

    // ---------------------------------------------------------------------------------

    /// <summary>Strikeouts until the half turns over (or the game ends).</summary>
    static void EndHalf(Match match)
    {
        var inning = match.Inning;
        var top = match.Top;
        var guard = 0;
        while (!match.Over && match.Inning == inning && match.Top == top && guard++ < 60)
            match.Play(Paint, Take);
    }

    /// <summary>Load the bases for the side at bat and hit a homer: four runs, by the dead-ball rule.</summary>
    static bool Slam(Match match)
    {
        var order = match.Top ? match.AwayOrder : match.HomeOrder;
        for (var bag = 1; bag <= 3; bag++)
            if (!match.StationRunner(bag, order[(bag + 4) % order.Count])) return false;
        if (!match.BeginAtBat(Paint, new SwingCommand(true, 0, 0, false), out _, out _)) return false;
        var hit = FlightFixtures.OverTheFence(match.Park, 20, 0);
        var field = new FieldingResult(PlayKind.HomeRun, null, null, 4, 0, 420, false, false);
        var ev = match.FinishAtBat(Paint, new SwingCommand(true, 0, 0, false), hit, field);
        return ev.Kind == PlayKind.HomeRun && ev.RunsScored == 4;
    }
}
