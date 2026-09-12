using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Spec §0.4: dead stick means the CPU plays that seat by the same rules, and the seat a pad owns
/// is a function of (half, home/away, seated controllers). #579: the batting human never owns a
/// glove, so a CPU defense throws without any input from the offense pad (S-93). #209 / #83: the
/// human on defense still owns the throw, and a dead stick never guns to first for them (S-33).
/// </summary>
public sealed class SeatOwnershipTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    const double Frame = 1.0 / 60.0;

    public static IEnumerable<object[]> Halves()
    {
        // seats, top half?, human fields?, human runs?
        yield return [Seats.One, true, true, false];        // 1P home pitches the top
        yield return [Seats.One, false, false, true];       // 1P home bats the bottom: whole defense is CPU
        yield return [Seats.AwayOne, true, false, true];    // 1P away bats the top
        yield return [Seats.AwayOne, false, true, false];   // 1P away pitches the bottom
        yield return [Seats.Versus, true, true, true];      // 1v1: a controller on both sides every half
        yield return [Seats.Versus, false, true, true];
        yield return [Seats.AwayVersus, true, true, true];
        yield return [Seats.AwayVersus, false, true, true];
    }

    [Theory]
    [MemberData(nameof(Halves))]
    public void S93_SeatOwnershipIsAFunctionOfHalfSideAndPads(Seats seats, bool top, bool humanFields, bool humanRuns)
    {
        var live = LiveSeats.For(seats, top);
        Assert.Equal(humanFields, live.HumanFields);
        Assert.Equal(humanFields, live.HumanOwnsThrow);
        Assert.Equal(humanRuns, live.HumanRuns);
        Assert.False(live.PlayerMustField, "only a training drill puts the player on the glove");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void S93_PadsFromSeatsThatDoNotOwnTheVerbAreDead(bool top)
    {
        var stick = new LivePadInput(StickX: 1, SouthDown: true, Swap: true);
        var batting = LiveSeats.For(Seats.One, top: false);
        var pitching = LiveSeats.For(Seats.One, top: true);
        Assert.Same(LivePadInput.Dead, batting.OwnedFieldPad(stick));
        Assert.Same(stick, batting.OwnedRunPad(stick));
        Assert.Same(stick, pitching.OwnedFieldPad(stick));
        Assert.Same(LivePadInput.Dead, pitching.OwnedRunPad(stick));
        var versus = LiveSeats.For(Seats.Versus, top);
        Assert.Same(stick, versus.OwnedFieldPad(stick));
        Assert.Same(stick, versus.OwnedRunPad(stick));
    }

    [Theory]
    [InlineData(true, false)]   // 1P HOME: bottom half, Jack bats, nobody presses a field verb
    [InlineData(false, false)]  // 1P AWAY: top half, same
    [InlineData(true, true)]    // and even if the offense stick leaks in as the field pad (the old 1P plumbing)
    [InlineData(false, true)]
    public void S93_CpuDefenseThrowsOutTheBatterWithoutTheBattingHumansPad(bool pad1Home, bool offenseStickLeaksToFieldPad)
    {
        var seats = pad1Home ? Seats.One : Seats.AwayOne;
        var scenario = new Scenario(_content, seed: 1);
        var match = scenario.Match;
        if (pad1Home) match.SkipToHomeHalf();
        Assert.Equal(!pad1Home, match.Top);
        Assert.True(seats.HumanBats(match.Top), "the human is on offense this half");
        var live = LiveSeats.For(seats, match.Top);
        Assert.False(live.HumanFields);

        var hit = HopperToShort(scenario, match);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        // The throw the CPU makes once it has the ball. The resolver's roll for whether the
        // grounder is an out at all is spec A.4 #38 (P4); this row is about who throws, not that.
        var field = SyntheticGroundOut(match, preview);
        Assert.False(InPlay.BatterBeatsThrow(match.Batter, hit, field, 0, match.Rules), "the play at first is makeable");

        var offense = new LivePadInput(StickX: 0.9, StickY: 0.3, Swap: true);
        var fieldPad = offenseStickLeaksToFieldPad ? offense : LivePadInput.Dead;
        var play = RunToComplete(match, hit, preview, field, live, fieldPad, offense, out var caughtAt, out var threwAt, out var everHuman);

        Assert.False(everHuman, "the batting human never owned a glove");
        Assert.Equal(PlayKind.GroundOut, play.Kind);
        var only = Assert.Single(play.Outcome!.OutsMade);
        Assert.Equal((OutType.ThrowOutAtFirst, 1, 0), (only.Type, only.Bag, only.FromBag));
        Assert.True(caughtAt >= 0, "the CPU glove scooped");
        Assert.True(threwAt >= 0, "the CPU glove threw");
        // The CPU glove throws as soon as the scoop's knockback settles: within the throw time, not on a press.
        Assert.InRange(threwAt - caughtAt, 0, match.Rules.Fielding.Knockback.MaxSec + Frame * 2);
    }

    [Theory]
    [InlineData(true)]   // 1P HOME pitches the top
    [InlineData(false)]  // 1P AWAY pitches the bottom
    public void S33_HumanDefenseDeadStickScoopsButNeverThrowsForThem(bool pad1Home)
    {
        var seats = pad1Home ? Seats.One : Seats.AwayOne;
        var scenario = new Scenario(_content, seed: 1);
        var match = scenario.Match;
        if (!pad1Home) match.SkipToHomeHalf();
        Assert.True(seats.HumanPitches(match.Top), "the human is on defense this half");
        var live = LiveSeats.For(seats, match.Top);
        Assert.True(live.HumanFields);

        var hit = HopperToShort(scenario, match);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        var field = SyntheticGroundOut(match, preview);

        var play = RunToComplete(match, hit, preview, field, live, LivePadInput.Dead, LivePadInput.Dead, out var caughtAt, out var threwAt, out _);

        Assert.True(caughtAt >= 0, "dead stick still chases and scoops like CPU");
        Assert.True(threwAt < 0, "they do not throw for you");
        Assert.Empty(play.Outcome!.OutsMade);
        Assert.NotNull(match.First);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void S93_InVersusTheDefenseControllerOwnsTheGloveEveryHalf(bool top)
    {
        var scenario = new Scenario(_content, seed: 1);
        var match = scenario.Match;
        if (!top) match.SkipToHomeHalf();
        var live = LiveSeats.For(Seats.Versus, match.Top);
        var hit = HopperToShort(scenario, match);
        var preview = match.PreviewHit(hit);
        match.LivePlay.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, live));
        var stick = new LivePadInput(StickX: 1, StickY: 0);
        for (var i = 0; i < 10; i++)
            match.LivePlay.Apply(LivePlayCommand.Tick(Frame, stick, LivePadInput.Dead, false, LivePlayCommandSource.Human));
        Assert.True(match.LivePlay.PlayerFielding, "the other controller's stick takes the glove");
    }

    // ---------------------------------------------------------------------------------

    static PlayEvent RunToComplete(
        Match match, AtBatResult hit, FieldingPreview preview, FieldingResult field, LiveSeats seats,
        LivePadInput fieldPad, LivePadInput runPad, out double caughtAt, out double threwAt, out bool everHuman)
    {
        var live = match.LivePlay;
        var begun = live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field, seats, 0, live.Source));
        Assert.True(begun.Snapshot.Active);
        caughtAt = threwAt = -1;
        everHuman = false;
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 20 && play is null; i++)
        {
            var result = live.Apply(LivePlayCommand.Tick(Frame, fieldPad, runPad, false, live.Source));
            if (caughtAt < 0 && live.Caught) caughtAt = live.ElapsedSeconds;
            if (threwAt < 0 && live.Throwing) threwAt = live.ElapsedSeconds;
            everHuman |= live.PlayerFielding;
            play = result.CompletedPlay;
        }
        Assert.NotNull(play);
        return play!;
    }

    /// <summary>A routine hopper with the throw that beats the batter, as in <see cref="LiveBallScenarioTests"/>.</summary>
    static FieldingResult SyntheticGroundOut(Match match, FieldingPreview preview)
    {
        var map = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
        map.TryGetValue("2B", out var cut);
        return new FieldingResult(PlayKind.GroundOut, preview.Fielder, cut, preview.HangTimeSec, preview.LandingX, preview.LandingZ, false, false,
            new ThrowResult(Chemistry.Good, 1.35, false));
    }

    /// <summary>
    /// A hopper the shortstop fields with a makeable play at first, for whichever roster is on
    /// defense this half (the two preset defenses align differently, so the shape differs).
    /// </summary>
    static AtBatResult HopperToShort(Scenario scenario, Match match) => match.Top
        ? Shape(scenario.Contact(), match, exit: 84, launch: 8, spray: -12)
        : Shape(scenario.Contact(), match, exit: 88, launch: 6, spray: -18);

    static AtBatResult Shape(AtBatResult hit, Match match, double exit, double launch, double spray)
    {
        var carry = BallFlight.CarryFeet(exit, launch, match.Park.WindMph, match.Rules);
        return hit with { ExitVeloMph = exit, LaunchDeg = launch, SprayDeg = spray, CarryFt = carry, HomeRun = false, Foul = false, InPlay = true };
    }
}
