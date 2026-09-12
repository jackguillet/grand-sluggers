using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class StealThrowTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void DefaultBagIsTheStealTargetNeverHome()
    {
        Assert.Equal(2, StealThrow.DefaultBag(2));
        Assert.Equal(3, StealThrow.DefaultBag(3));
        Assert.Equal(0, StealThrow.DefaultBag(4));
        Assert.Equal(0, StealThrow.DefaultBag(1));
        Assert.Equal(2, StealThrow.CommitBag(0, 2));
        Assert.Equal(3, StealThrow.CommitBag(0, 3));
        Assert.Equal(2, StealThrow.CommitBag(2, 2));
        Assert.Equal(3, StealThrow.CommitBag(3, 2));
        Assert.Equal(1, StealThrow.CommitBag(1, 2));
        Assert.Equal(4, StealThrow.CommitBag(4, 2));
        Assert.Equal(0, StealThrow.CommitBag(0, 4));
        Assert.Equal("2B", StealThrow.CoverPos(2));
        Assert.Equal("3B", StealThrow.CoverPos(3));
        Assert.Equal("", StealThrow.CoverPos(4));
        Assert.Equal("2B", StealThrow.AfterThrowPos("C", 2));
        Assert.Equal("2B", FieldAssist.AfterThrowPos("C", 2));
        Assert.Equal(FieldAssist.CoverKey(2), StealThrow.CoverPos(2));
        Assert.Equal(PlayCamera.Shot(PlayCamera.Beat.StealThrow), PlayCamera.Shot(PlayCamera.Beat.Throw));
        Assert.Equal(2, InPlay.DiamondBag(0, 1));
        Assert.Equal(2, Baserunning.DiamondBag(0, 1));
    }

    [Fact]
    public void TheStealRaceStartsFromTheBag()
    {
        // D1: no lead credit. The runner's time to the bag is the bag run less the jump on the pitch.
        var dart = _content.Must("dart");
        var slow = _content.Must("konga");
        Assert.True(StealThrow.RunnerRemainSec(dart) < RunnerSystem.BagSec(dart));
        Assert.True(StealThrow.RunnerRemainSec(dart) < StealThrow.RunnerRemainSec(slow));
        Assert.True(StealThrow.GunSec(2, null) > 0.4);
        Assert.True(StealThrow.GunDistFt(2) > 100);
    }

    [Fact]
    public void PlayerThrowToSecondCanOutOrLoseIndependentlyOfTheOldRoll()
    {
        var dart = _content.Must("dart");
        var laser = new ThrowResult(Chemistry.Good, 1.35, false);
        var mud = new ThrowResult(Chemistry.Bad, 0.7, true);
        Assert.True(StealThrow.PlayerOut(2, 2, 0.05, laser, dart), "early gun beats the runner");
        Assert.False(StealThrow.PlayerOut(2, 2, 1.35, laser, dart), "late gun loses the same steal");
        Assert.False(StealThrow.PlayerOut(1, 2, 0.05, laser, dart), "wrong bag is safe");
        Assert.False(StealThrow.PlayerOut(4, 2, 0.05, laser, dart), "home is not a steal gun");
        Assert.False(StealThrow.PlayerOut(2, 2, 1.0, mud, dart), "a late error throw is a steal");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void PickoffIsDecidedByTheMeasuredThrowAndReturnRace(int occupiedBag)
    {
        var dart = _content.Must("dart");
        var laser = new ThrowResult(Chemistry.Good, 1.35, false);
        var flight = StealThrow.CatcherThrowSec(occupiedBag, laser);
        var returnTime = StealThrow.RunnerReturnSec(dart);
        var lastWinningRelease = returnTime - flight;

        Assert.True(StealThrow.PickoffOut(occupiedBag, lastWinningRelease - 0.001, laser, dart));
        Assert.False(StealThrow.PickoffOut(occupiedBag, lastWinningRelease + 0.001, laser, dart));
    }

    [Fact]
    public void PickoffWindowFollowsRunnerAndThrowRelationships()
    {
        var fast = _content.Must("dart");
        var slow = _content.Must("konga");
        var laser = new ThrowResult(Chemistry.Good, 1.35, false);
        var mud = new ThrowResult(Chemistry.Bad, 0.7, true);

        Assert.True(StealThrow.RunnerReturnSec(slow) > StealThrow.RunnerReturnSec(fast));
        Assert.True(StealThrow.CatcherThrowSec(1, laser) < StealThrow.CatcherThrowSec(1, mud));
        Assert.False(StealThrow.PickoffOut(3, 0, laser, slow), "catcher pickoff targets are first or second");
    }

    [Fact]
    public void CpuGunCatchesASlowRunnerMoreThanAFastOne()
    {
        var dart = _content.Must("dart");
        var slow = _content.Must("konga");
        var catcher = _content.Must("vale");
        var thr = new ThrowResult(Chemistry.Neutral, 1.0, false);
        var fastSafe = 0;
        var slowSafe = 0;
        for (var seed = 1; seed <= 48; seed++)
        {
            if (!StealThrow.CpuOut(dart, catcher, 2, thr, new Random(seed))) fastSafe++;
            if (!StealThrow.CpuOut(slow, catcher, 2, thr, new Random(seed))) slowSafe++;
        }
        Assert.True(fastSafe >= slowSafe, $"fast runner steals {fastSafe} vs slow {slowSafe}");
    }

    [Fact]
    public void BeginAtBatDoesNotResolveAStealBeforeTheThrow()
    {
        var match = Match.Slice(_content, seed: 1);
        WalkOn(match);
        Assert.True(match.StartSteal());
        var take = new SwingCommand(false, 0, 0, false);
        var wild = new PitchCommand("fastball", 0, false);
        Assert.False(match.BeginAtBat(wild, take, out _, out var finished));
        Assert.NotNull(finished);
        Assert.True(match.StealThrowPending);
        Assert.NotNull(match.First);
        Assert.True(finished!.Kind is PlayKind.TakeBall or PlayKind.TakeStrike or PlayKind.SwingMiss,
            finished.Kind.ToString());
        Assert.NotEqual(PlayKind.StolenBase, finished.Kind);
        Assert.NotEqual(PlayKind.CaughtStealing, finished.Kind);
        Assert.True(match.StealOn);
    }

    [Fact]
    public void PlayerCatcherThrowToSecondOutsOrLosesOnTheSameSteal()
    {
        var early = ArmedTake(seed: 3);
        var late = ArmedTake(seed: 3);
        var laser = new ThrowResult(Chemistry.Good, 1.4, false);
        var outPlay = early.Match.ResolveStealThrow(early.Ev, 2, 0.04, laser);
        var safe = late.Match.ResolveStealThrow(late.Ev, 2, 1.4, laser);
        Assert.Equal(PlayKind.CaughtStealing, outPlay.Kind);
        Assert.Equal(PlayKind.StolenBase, safe.Kind);
        Assert.Equal(RunnerPlayResult.CaughtStealing, outPlay.Outcome?.RunnerResult);
        Assert.Equal(RunnerPlayResult.StolenBase, safe.Outcome?.RunnerResult);
        Assert.Equal(new ThrowEndpoint(ThrowOrigin.Catcher, 2), outPlay.Outcome?.ThrowEndpoint);
        Assert.Equal(outPlay.Outcome?.ThrowEndpoint,
            (outPlay with { Caption = "El corredor fue retirado." }).Outcome?.ThrowEndpoint);
        Assert.Null(early.Match.First);
        Assert.NotNull(late.Match.Second);
        Assert.False(early.Match.StealThrowPending);
        Assert.False(late.Match.StealThrowPending);
    }

    [Fact]
    public void LiveWrongBagPreservesTheActualThrowEndpoint()
    {
        var armed = ArmedTake(seed: 3);
        var laser = new ThrowResult(Chemistry.Good, 1.4, false);
        var ev = armed.Match.ResolveStealThrow(armed.Ev, 1, 1.4, laser);

        Assert.Equal(new ThrowEndpoint(ThrowOrigin.Catcher, 1), ev.Outcome?.ThrowEndpoint);
        Assert.Equal(1, ev.Outcome?.RunnerFromBag);
        Assert.Equal(ev.Outcome?.ThrowEndpoint,
            (ev with { Caption = "送球先はコピーに依存しない。" }).Outcome?.ThrowEndpoint);
    }

    [Fact]
    public void CpuGunPublishesCatcherAndTargetBag()
    {
        var armed = ArmedTake(seed: 3);
        var ev = armed.Match.GunSteal(armed.Ev);

        Assert.Equal(new ThrowEndpoint(ThrowOrigin.Catcher, 2), ev.Outcome?.ThrowEndpoint);
        Assert.Equal(1, ev.Outcome?.RunnerFromBag);
        Assert.Equal(2, ev.Outcome?.RunnerToBag);
    }

    [Fact]
    public void NamedPickoffPublishesPitcherAndOriginalBag()
    {
        var match = Match.Slice(_content, seed: 3);
        WalkOn(match);
        match.StartSteal();
        var ev = match.Pickoff(1);

        Assert.NotNull(ev);
        Assert.Equal(new ThrowEndpoint(ThrowOrigin.PitcherRubber, 1), ev!.Outcome?.ThrowEndpoint);
        Assert.Equal(1, ev.Outcome?.RunnerFromBag);
        Assert.Equal(ev.Outcome?.ThrowEndpoint,
            (ev with { Caption = "Texto reemplazado." }).Outcome?.ThrowEndpoint);
    }

    (Match Match, PlayEvent Ev) ArmedTake(int seed)
    {
        var match = Match.Slice(_content, seed: seed);
        WalkOn(match);
        match.StartSteal();
        var take = new SwingCommand(false, 0, 0, false);
        var wild = new PitchCommand("fastball", 0, false);
        Assert.False(match.BeginAtBat(wild, take, out _, out var finished));
        Assert.NotNull(finished);
        Assert.True(match.StealThrowPending);
        return (match, finished!);
    }

    static void WalkOn(Match match)
    {
        var wild = new PitchCommand("fastball", 0, false, AimX: 1.5);
        var take = new SwingCommand(false, 0, 0, false);
        while (match.First is null && !match.Over)
            match.Play(wild, take);
        Assert.NotNull(match.First);
    }
}
