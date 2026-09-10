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
        var almostSettled = InPlay.TimeOnBagSec - snapshot.Batter.Sec - 0.01;
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
