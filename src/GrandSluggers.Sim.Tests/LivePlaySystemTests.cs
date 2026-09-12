using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class LivePlaySystemTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    static readonly PitchCommand Paint = new("fastball", 0, 0, false);
    static readonly SwingCommand Swing = new(true, 0, 0, false);

    [Fact]
    public void CpuAndHumanReplayTheSameGrounderCommands()
    {
        var cpu = ReplayDoublePlay(LivePlayCommandSource.Cpu);
        var human = ReplayDoublePlay(LivePlayCommandSource.Human);

        Assert.Equal(cpu, human);
        Assert.Equal((2, false, false, PlayKind.GroundOut), cpu);
    }

    [Theory]
    [InlineData(LivePlayCommandSource.Cpu)]
    [InlineData(LivePlayCommandSource.Human)]
    public void PausedGrounderDoesNotAdvanceOrAcceptAThrow(LivePlayCommandSource source)
    {
        var (match, hit, field) = GrounderOnFirst();
        match.LivePlay.Apply(LivePlayCommand.Begin(PlayKind.GroundOut, source));
        match.LivePlay.Apply(LivePlayCommand.Advance(
            0.75, PlayKind.GroundOut, false, false, false, 0, source));
        match.LivePlay.Apply(LivePlayCommand.Pause(source));

        var frozen = match.LivePlay.Apply(LivePlayCommand.Advance(
            4, PlayKind.GroundOut, true, false, true, 0, source));
        var ignored = match.LivePlay.Apply(LivePlayCommand.ThrowArrived(2, false, field.Fielder, source));

        Assert.True(match.Paused);
        Assert.Equal(0.75, frozen.Snapshot.ElapsedSeconds, 6);
        Assert.Null(ignored.Throw);
        Assert.Equal(0, match.Outs);
        Assert.NotNull(match.First);

        match.LivePlay.Apply(LivePlayCommand.Resume(source));
        Assert.False(match.Paused);
        var landed = match.LivePlay.Apply(LivePlayCommand.ThrowArrived(2, false, field.Fielder, source));
        Assert.True(landed.Throw?.Out);
        Assert.Equal(1, match.Outs);

        var complete = match.LivePlay.Apply(LivePlayCommand.Complete(Paint, Swing, hit, field, source));
        Assert.NotNull(complete.CompletedPlay);
    }

    [Theory]
    [InlineData(LivePlayCommandSource.Cpu)]
    [InlineData(LivePlayCommandSource.Human)]
    public void MatchPausedBeforeContactCannotBeBypassedByBegin(LivePlayCommandSource source)
    {
        var (match, _, field) = GrounderOnFirst();
        match.SetPaused(true);

        var begun = match.LivePlay.Apply(LivePlayCommand.Begin(PlayKind.GroundOut, source));
        var advance = match.LivePlay.Apply(LivePlayCommand.Advance(
            1, PlayKind.GroundOut, true, false, true, 0, source));
        var landed = match.LivePlay.Apply(LivePlayCommand.ThrowArrived(2, false, field.Fielder, source));

        Assert.True(begun.Snapshot.Paused);
        Assert.Equal(0, advance.Snapshot.ElapsedSeconds);
        Assert.Null(landed.Throw);
        Assert.Equal(0, match.Outs);
    }

    [Theory]
    [InlineData(LivePlayCommandSource.Cpu)]
    [InlineData(LivePlayCommandSource.Human)]
    public void BatterTagRemovesTheLoadedForceChainForLaterCommands(LivePlayCommandSource source)
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        Assert.True(match.StationRunner(1, match.AwayOrder[1]));
        Assert.True(match.StationRunner(2, match.AwayOrder[2]));
        Assert.True(match.StationRunner(3, match.AwayOrder[3]));
        Assert.True(match.BeginAtBat(Paint, Swing, out _, out _));
        match.LivePlay.Apply(LivePlayCommand.Begin(PlayKind.GroundOut, source));
        var advanced = match.LivePlay.Apply(LivePlayCommand.Advance(
            1, PlayKind.GroundOut, true, false, true, 0, source));
        var feet = InPlay.RunFeet(advanced.Snapshot.ElapsedSeconds, match.Batter);
        var batter = InPlay.AlongBases(feet, 1, HomeSet.BatterX, HomeSet.BatterZ);

        var tag = match.LivePlay.Apply(LivePlayCommand.Contact(
            PlayKind.GroundOut, true, false, true, batter.X, batter.Z, 0, match.Pitcher, source));

        Assert.Equal(0, tag.TaggedFromBag);
        Assert.True(tag.Snapshot.BatterOut);
        Assert.All(new[] { 1, 2, 3, 4 }, bag => Assert.False(tag.Snapshot.Forces.At(bag)));

        var later = match.LivePlay.Apply(LivePlayCommand.ThrowArrived(2, false, match.Pitcher, source));
        Assert.True(later.Throw?.Out);
        Assert.False(later.Throw?.Force);
    }

    [Theory]
    [InlineData(LivePlayCommandSource.Cpu)]
    [InlineData(LivePlayCommandSource.Human)]
    public void FlyCommandCompletesTheThirdOutAndChangesHalfInning(LivePlayCommandSource source)
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        FinishFlyOut(match);
        FinishFlyOut(match);
        Assert.Equal((1, true, 2), (match.Inning, match.Top, match.Outs));
        Assert.True(match.BeginAtBat(Paint, Swing, out var hit, out _));
        var field = new FieldingResult(
            PlayKind.FlyOut, match.Pitcher, null, 1, 0, 200, false, false);

        match.LivePlay.Apply(LivePlayCommand.Begin(PlayKind.FlyOut, source));
        var caught = match.LivePlay.Apply(LivePlayCommand.Advance(
            1.2, PlayKind.FlyOut, true, false, true, 0, source));
        Assert.True(caught.Snapshot.IsTime);
        var completed = match.LivePlay.Apply(LivePlayCommand.Complete(Paint, Swing, hit, field, source));

        Assert.NotNull(completed.CompletedPlay);
        Assert.Equal(PlayKind.FlyOut, completed.CompletedPlay!.Kind);
        Assert.Equal((1, false, 0), (match.Inning, match.Top, match.Outs));
    }

    [Theory]
    [InlineData(LivePlayCommandSource.Cpu)]
    [InlineData(LivePlayCommandSource.Human)]
    public void TimeWaitsForTheLiveRunnerToHoldTheBagForOneSecond(LivePlayCommandSource source)
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        Assert.True(match.BeginAtBat(Paint, Swing, out _, out _));
        match.LivePlay.Apply(LivePlayCommand.Begin(PlayKind.Single, source));

        LivePlaySnapshot snapshot;
        do
        {
            snapshot = match.LivePlay.Apply(LivePlayCommand.Advance(
                0.05, PlayKind.Single, true, false, true, 0, source)).Snapshot;
        } while (!snapshot.Batter.OnBag);

        Assert.False(snapshot.IsTime);
        var almostSettled = Rules.Default.Running.Bags.TimeOnBagSec - snapshot.Batter.Sec - 0.01;
        snapshot = match.LivePlay.Apply(LivePlayCommand.Advance(
            almostSettled, PlayKind.Single, true, false, true, 0, source)).Snapshot;
        Assert.False(snapshot.IsTime);

        snapshot = match.LivePlay.Apply(LivePlayCommand.Advance(
            0.02, PlayKind.Single, true, false, true, 0, source)).Snapshot;
        Assert.True(snapshot.IsTime, snapshot.ToString());
    }

    [Fact]
    public void CompletedCommandCannotFinishASecondAtBat()
    {
        var (match, hit, field) = GrounderOnFirst();
        match.LivePlay.Apply(LivePlayCommand.Begin(PlayKind.GroundOut));
        var command = LivePlayCommand.Complete(Paint, Swing, hit, field);

        var first = match.LivePlay.Apply(command);
        var logCount = match.Log.Count;
        var batter = match.Batter.Id;
        var stale = match.LivePlay.Apply(command);
        var staleThrow = match.LivePlay.Apply(LivePlayCommand.ThrowArrived(1, false, match.Pitcher));

        Assert.NotNull(first.CompletedPlay);
        Assert.Null(stale.CompletedPlay);
        Assert.Null(staleThrow.Throw);
        Assert.Equal(logCount, match.Log.Count);
        Assert.Equal(batter, match.Batter.Id);
    }

    [Theory]
    [InlineData(LivePlayCommandSource.Cpu, false)]
    [InlineData(LivePlayCommandSource.Human, false)]
    [InlineData(LivePlayCommandSource.Cpu, true)]
    [InlineData(LivePlayCommandSource.Human, true)]
    public void UncaughtHomerCompletesWithoutPossessionAndScoresExactlyOnce(
        LivePlayCommandSource source, bool loaded)
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        if (loaded)
            for (var bag = 1; bag <= 3; bag++)
                Assert.True(match.StationRunner(bag, match.AwayOrder[bag]));
        Assert.True(match.BeginAtBat(Paint, Swing, out var hit, out _));
        hit = hit with { HomeRun = true, CarryFt = 420, LaunchDeg = 35 };
        var field = new FieldingResult(PlayKind.HomeRun, null, null, 4, 0, 420, false, false);
        var batter = match.Batter.Id;
        match.LivePlay.Apply(LivePlayCommand.Begin(field.Kind, source));
        match.LivePlay.Apply(LivePlayCommand.Advance(4.2, field.Kind, false, false, false, 0, source));
        Assert.False(InPlay.DeadBallResultReady(field.Kind, match.LivePlay.ElapsedSeconds,
            field.HangTimeSec, false, false, false)); // Wall-catch window is still open.
        match.SetPaused(true);
        match.LivePlay.Apply(LivePlayCommand.Advance(10, field.Kind, false, false, false, 0, source));
        Assert.Equal(4.2, match.LivePlay.ElapsedSeconds);
        match.SetPaused(false);
        var atEnd = match.LivePlay.Apply(LivePlayCommand.Advance(
            0.2, field.Kind, false, false, false, 0, source)).Snapshot;
        Assert.False(atEnd.IsTime); // Ordinary live-ball Time correctly requires possession.
        Assert.True(InPlay.DeadBallResultReady(field.Kind, atEnd.ElapsedSeconds,
            field.HangTimeSec, false, false, false));
        var command = LivePlayCommand.Complete(Paint, Swing, hit, field, source);
        var result = match.LivePlay.Apply(command).CompletedPlay;
        Assert.Equal(PlayKind.HomeRun, result!.Kind);
        Assert.Equal(loaded ? 4 : 1, match.AwayScore);
        Assert.NotEqual(batter, match.Batter.Id);
        Assert.Null(match.First);
        Assert.Null(match.Second);
        Assert.Null(match.Third);
        Assert.Null(match.LivePlay.Apply(command).CompletedPlay);
        Assert.Equal(loaded ? 4 : 1, match.AwayScore);
        match.LivePlay.Apply(LivePlayCommand.Begin(PlayKind.Single, source));
        Assert.Equal(0, match.LivePlay.ElapsedSeconds);
    }

    [Theory]
    [InlineData(PlayKind.HomeRun, true, false, false)]
    [InlineData(PlayKind.HomeRun, false, true, false)]
    [InlineData(PlayKind.HomeRun, false, false, true)]
    [InlineData(PlayKind.FlyOut, false, false, false)]
    [InlineData(PlayKind.Single, false, false, false)]
    [InlineData(PlayKind.Foul, true, false, false)]
    public void DeadBallCompletionCannotResolveACatchThrowEffectOrOrdinaryLiveBall(
        PlayKind kind, bool caught, bool throwing, bool effectInFlight)
    {
        Assert.False(InPlay.DeadBallResultReady(kind, 100, 4, caught, throwing, effectInFlight));
    }

    [Fact]
    public void AFoulNobodyCaughtIsADeadBallResultOnceItsCallIsIn()
    {
        // §7.11: dead at the verdict plus the spectacle beat, never before it.
        Assert.True(InPlay.HasDeadBallResult(PlayKind.Foul));
        Assert.True(InPlay.DeadBallResultReady(PlayKind.Foul, 100, 4, false, false, false));
        Assert.False(InPlay.DeadBallResultReady(PlayKind.Foul, 4.1, 4, false, false, false));
    }

    (Match Match, AtBatResult Hit, FieldingResult Field) GrounderOnFirst()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        Assert.True(match.StationRunner(1, match.AwayOrder[1]));
        Assert.True(match.BeginAtBat(Paint, Swing, out var hit, out _));
        var field = new FieldingResult(
            PlayKind.GroundOut, match.Pitcher, match.Batter, 1.5, 48, 72, false, false,
            new ThrowResult(Chemistry.Good, 1.7, false));
        return (match, hit, field);
    }

    (int Outs, bool FirstOccupied, bool SecondOccupied, PlayKind Kind) ReplayDoublePlay(
        LivePlayCommandSource source)
    {
        var (match, hit, field) = GrounderOnFirst();
        var commands = new[]
        {
            LivePlayCommand.Begin(PlayKind.GroundOut, source),
            LivePlayCommand.Advance(1, PlayKind.GroundOut, false, false, false, 0, source),
            LivePlayCommand.Pause(source),
            LivePlayCommand.Advance(2, PlayKind.GroundOut, true, false, true, 0, source),
            LivePlayCommand.Resume(source),
            LivePlayCommand.ThrowArrived(2, false, field.Fielder, source),
            LivePlayCommand.ThrowArrived(1, false, field.Fielder, source),
            LivePlayCommand.Complete(Paint, Swing, hit, field, source)
        };
        LivePlayCommandResult? last = null;
        foreach (var command in commands) last = match.LivePlay.Apply(command);
        Assert.NotNull(last?.CompletedPlay);
        return (match.Outs, match.First is not null, match.Second is not null, last!.CompletedPlay!.Kind);
    }

    static void FinishFlyOut(Match match)
    {
        Assert.True(match.BeginAtBat(Paint, Swing, out var hit, out _));
        var field = new FieldingResult(
            PlayKind.FlyOut, match.Pitcher, null, 1, 0, 200, false, false);
        match.FinishAtBat(Paint, Swing, hit, field);
    }
}
