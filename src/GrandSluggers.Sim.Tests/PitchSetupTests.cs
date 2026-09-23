using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class PitchSetupTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    static readonly LiveSeats Both = new(true, true, true, true);
    const double Dt = 1.0 / 120;

    Match Game(params int[] bags)
    {
        var home = _content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = _content.Team("Offense", "zig", "cinder", "dart", "jester", "grit", "soot", "boom", "nugget", "rio");
        var match = Match.Exhibition(_content, home, away, 3, 1);
        foreach (var bag in bags) Assert.True(match.StationRunner(bag, away.BattingOrder[bag + 1]));
        return match;
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void DepartureAndReturnTraverseTheSameBodyBeforeAndDuringThePitch(int bag)
    {
        var m = Game(bag);
        var body = m.RunnerAt(bag)!;
        Assert.True(m.StartStealAt(bag));
        Assert.Equal(0, body.Feet); // no movement before time actually passes
        m.PitchSetup.Advance(.25);
        var outbound = body.Feet;
        Assert.True(outbound > 0);
        Assert.True(m.ReturnToBagAt(bag));
        Assert.Equal(outbound, body.Feet);
        m.PitchSetup.Advance(.1);
        Assert.InRange(body.Feet, 0.001, outbound - .001);
        Assert.Equal(RunnerPhase.Returning, body.Phase);
        Assert.True(m.PitchSetup.BeginCharge());
        Assert.True(m.PitchSetup.ReleaseBall());
        var returning = body.Feet;
        m.PitchSetup.Advance(.1);
        Assert.True(body.Feet < returning);
        Assert.Same(body, m.RunnerAt(bag));
    }

    [Fact]
    public void LateDepartureHasNoBackdatedBonusAndPauseStopsBothBodiesAndBufferAge()
    {
        var m = Game(1);
        m.PitchSetup.BeginCharge();
        m.PitchSetup.ReleaseBall();
        m.PitchSetup.Advance(.5);
        Assert.True(m.StartStealAt(1, windupSec: 10));
        var r = m.RunnerAt(1)!;
        Assert.Equal(0, r.Feet);
        m.PitchSetup.Advance(.1);
        Assert.Equal(RunnerSystem.SpeedFtPerSec(r.Who, 0, m.Rules) * m.Rules.Running.Steal.AirSpeedMul * .1, r.Feet, 8);
        var feet = r.Feet;
        var elapsed = m.PitchSetup.ElapsedSeconds;
        m.SetPaused(true);
        m.PitchSetup.Advance(5);
        Assert.Equal(feet, r.Feet);
        Assert.Equal(elapsed, m.PitchSetup.ElapsedSeconds);
    }

    [Fact]
    public void CatcherPossessionPreservesAReturningRunnersPositionAndDestination()
    {
        var m = Game(1);
        m.StartStealAt(1);
        m.PitchSetup.Advance(.4);
        m.ReturnToBagAt(1);
        m.PitchSetup.ReleaseBall();
        m.PitchSetup.Advance(.1);
        var r = m.RunnerAt(1)!;
        var feet = r.Feet;
        var dest = r.DestBag;
        var pitch = Receive(m);
        Assert.Equal(feet, r.Feet);
        Assert.Equal(dest, r.DestBag);
        Assert.Equal(RunnerPhase.Returning, r.Phase);
        Assert.Same(r, m.RunnerAt(1));
        Assert.NotNull(pitch);
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void PitcherMayThrowToAnyBagIncludingAnEmptyDestination(int bag)
    {
        var m = Game(1);
        m.StartStealAt(1);
        m.PitchSetup.Advance(.4);
        var feet = m.RunnerAt(1)!.Feet;
        Assert.True(m.BeginPickoff(bag, Both, out var dead, LivePlayCommandSource.Human));
        Assert.Null(dead);
        Assert.Equal(bag, m.LivePlay.ThrowBag);
        Assert.Equal("P", m.LivePlay.ThrowFromPos);
        Assert.Equal(feet, m.RunnerAt(1)!.Feet);
        Assert.DoesNotContain(m.LivePlay.Snapshot.Runners, r => r.Phase == RunnerPhase.Out);
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void ChargingCommitsImmediatelyAndABalkAwardsEveryRunnerWithoutCompletingTheAppearance(int target)
    {
        var m = Game(1, 2, 3);
        m.BeginAtBat(new PitchCommand("fastball", 0, false, AimX: 2), new(false, 0, 0, false), out _, out _);
        var batter = m.Batter.Id;
        var stars = (m.AwayStars, m.HomeStars);
        var count = (m.Balls, m.Strikes);
        var first = m.First!.Id;
        var second = m.Second!.Id;
        Assert.True(m.PitchSetup.BeginCharge());
        Assert.False(m.BeginPickoff(target, Both, out var balk, LivePlayCommandSource.Human));
        Assert.Equal(PlayKind.Balk, balk!.Kind);
        Assert.Equal(count, (m.Balls, m.Strikes));
        Assert.Equal(batter, m.Batter.Id);
        Assert.Equal(stars, (m.AwayStars, m.HomeStars));
        Assert.Equal(1, m.AwayScore);
        Assert.Null(m.First);
        Assert.Equal(first, m.Second!.Id);
        Assert.Equal(second, m.Third!.Id);
        Assert.Equal(3, balk.Outcome!.Moves.Count);
        Assert.Empty(balk.Outcome.OutsMade);
        Assert.False(m.LivePlay.Active);
        Assert.False(m.PitchSetup.Committed);
    }

    [Fact]
    public void IndefiniteChargeNeitherAutoReleasesNorPreventsARealCompletedSteal()
    {
        var m = Game(1);
        m.PitchSetup.BeginCharge();
        m.PitchSetup.Advance(60);
        Assert.Equal(PitchSetupPhase.Windup, m.PitchSetup.Phase);
        m.StartStealAt(1);
        var result = m.PitchSetup.Advance(4);
        Assert.Equal(PlayKind.StolenBase, result!.Kind);
        Assert.NotNull(m.Second);
        Assert.True(m.PitchSetup.Committed);
        Assert.True(m.PitchSetup.ReleaseBall());
    }

    [Fact]
    public void ResettingTheRubberCannotClearPitchCommitment()
    {
        var m = Game(1);
        m.PitchSetup.BeginCharge();
        m.ResetPitcher();
        Assert.True(m.PitchSetup.Committed);
        Assert.False(m.BeginPickoff(2, Both, out var balk));
        Assert.Equal(PlayKind.Balk, balk!.Kind);
    }

    [Fact]
    public void OnceThePitchIsAirborneAPickoffInputCannotRecallItOrAwardABalk()
    {
        var m = Game(1);
        m.PitchSetup.BeginCharge();
        m.PitchSetup.ReleaseBall();
        Assert.False(m.BeginPickoff(2, Both, out var result));
        Assert.Null(result);
        Assert.Equal(PitchSetupPhase.Flight, m.PitchSetup.Phase);
        Assert.NotNull(m.First);
    }

    [Fact]
    public void AHomeCrossingDuringPitchFlightIsScoredOnceWhenTheTakenPitchResolves()
    {
        var m = Game(3);
        m.StartStealAt(3);
        m.PitchSetup.ReleaseBall();
        m.PitchSetup.Advance(6);
        Assert.Equal(0, m.AwayScore);
        Receive(m);
        PlayEvent? play = null;
        for (var i = 0; i < 240 && play is null; i++)
            play = m.LivePlay.Apply(LivePlayCommand.Tick(Dt)).CompletedPlay;
        Assert.NotNull(play);
        Assert.Equal(1, m.AwayScore);
        Assert.Equal(1, play!.RunsScored);
        Assert.Single(play.Scorers);
        Assert.False(m.StealThrowPending);
    }

    [Fact]
    public void AWalkPreservesAHomeCrossingAndAwardsTheForcedRunnerOnlyOnce()
    {
        var m = Game(1, 3);
        var wide = new PitchCommand("fastball", 0, false, AimX: 2);
        var take = new SwingCommand(false, 0, 0, false);
        for (var i = 0; i < 3; i++) m.BeginAtBat(wide, take, out _, out _);
        m.StartStealAt(3);
        m.PitchSetup.ReleaseBall();
        m.PitchSetup.Advance(6);
        Assert.False(m.BeginAtBat(wide, take, out _, out var walk));
        Assert.Equal(PlayKind.Walk, walk!.Kind);
        Assert.Equal(1, walk.RunsScored);
        Assert.Equal(1, m.AwayScore);
        Assert.NotNull(m.First);
        Assert.NotNull(m.Second);
        Assert.Empty(walk.Outcome!.OutsMade);
    }

    [Fact]
    public void FoulReturnsAFlightScorerToTheOriginalBagWithoutAwardingARun()
    {
        var m = Game(3);
        var runner = m.Third!.Id;
        m.StartStealAt(3);
        m.PitchSetup.ReleaseBall();
        m.PitchSetup.Advance(6);
        m.PitchSetup.CatcherInput(new LivePadInput(KeysBag: 2, SouthDown: true));
        var pitch = new PitchCommand("fastball", 0, false);
        var early = -(AtBatResolver.ContactWindowFrames(null, m.Park, false, m.Rules, _content.StarSkills) / 2 - .05);
        var swing = new SwingCommand(true, 0, early, false);
        Assert.True(m.BeginAtBat(pitch, swing, out var hit, out _));
        Assert.True(hit.Foul);
        // A grounded foul tests restoration; a foul caught in the air is an out.
        hit = FlightFixtures.Hit(m.Park, 50, -20, 60);
        var preview = m.PreviewHit(hit);
        var foul = new FieldingResult(PlayKind.Foul, preview.Fielder, null, preview.HangTimeSec,
            preview.LandingX, preview.LandingZ, false, false);
        var result = m.FinishAtBat(pitch, swing, hit, foul);
        Assert.Equal(PlayKind.Foul, result.Kind);
        Assert.Equal(0, m.AwayScore);
        Assert.Equal(runner, m.Third!.Id);
        Assert.Equal(0, m.PitchSetup.CatcherBag);
    }

    [Fact]
    public void UnusedCatcherInputCannotFireOnTheNextPitch()
    {
        var m = Game(1);
        m.PitchSetup.CatcherInput(new LivePadInput(KeysBag: 3, SouthDown: true));
        m.BeginAtBat(new PitchCommand("fastball", 0, false), new(false, 0, 0, false), out _, out _);
        m.StartStealAt(1);
        m.PitchSetup.ReleaseBall();
        m.PitchSetup.Advance(.1);
        Receive(m);
        Assert.False(m.LivePlay.Throwing);
        Assert.Equal(0, m.PitchSetup.CatcherBag);
    }

    [Fact]
    public void ReturningRunnerCannotBeUsedAsPermissionToStealIntoTheirBag()
    {
        var m = Game(1, 2);
        m.StartStealAt(2);
        m.PitchSetup.Advance(.2);
        Assert.True(m.CanStealFrom(1));
        m.ReturnToBagAt(2);
        Assert.False(m.CanStealFrom(1));
    }

    [Theory]
    [InlineData(.1, true)] [InlineData(.3, false)]
    public void BufferedCatcherThrowWaitsForPossessionAndPhysicalTransfer(double age, bool accepted)
    {
        var m = Game(1);
        m.StartStealAt(1);
        m.PitchSetup.ReleaseBall();
        m.PitchSetup.CatcherInput(new LivePadInput(KeysBag: 2, SouthDown: true));
        m.PitchSetup.Advance(age);
        Assert.False(m.LivePlay.Throwing);
        Receive(m);
        var live = m.LivePlay;
        Assert.Equal(accepted, live.ThrowPreparing);
        if (!accepted) return;
        var start = (live.BallX, live.BallY, live.BallZ);
        Assert.Equal("C", live.GlovePos);
        for (var i = 0; i < 10; i++)
        {
            var tick = live.Apply(LivePlayCommand.Tick(Dt, LivePadInput.Dead, LivePadInput.Dead));
            Assert.Equal(start, (live.BallX, live.BallY, live.BallZ));
            Assert.DoesNotContain(LiveEvent.ThrowPop, live.Events);
        }
        while (live.ThrowPreparing) live.Apply(LivePlayCommand.Tick(Dt));
        Assert.True(live.ThrowInFlight);
        Assert.Equal("2B", live.GlovePos);
        Assert.True(live.ThrowT >= m.Rules.Fielding.Throw.ReleaseSec);
        Assert.Contains(LiveEvent.ThrowPop, live.Events);
    }

    PlayEvent Receive(Match m)
    {
        Assert.False(m.BeginAtBat(new PitchCommand("fastball", 0, false), new(false, 0, 0, false), out _, out var pitch));
        Assert.NotNull(pitch);
        m.LivePlay.Apply(LivePlayCommand.BeginSteal(pitch!, Both, LivePlayCommandSource.Human));
        return pitch!;
    }
}
