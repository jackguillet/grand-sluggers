using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class BaserunningTests
{
    [Fact]
    public void DiamondMatchesThrows()
    {
        Assert.Equal(InPlay.DiamondBag(1, 0), Baserunning.DiamondBag(1, 0));
        Assert.Equal(1, Baserunning.DiamondBag(1, 0));
        Assert.Equal(2, Baserunning.DiamondBag(0, 1));
        Assert.Equal(3, Baserunning.DiamondBag(-1, 0));
        Assert.Equal(4, Baserunning.DiamondBag(0, -1));
        Assert.Equal(0, Baserunning.DiamondBag(0, 0));
    }

    [Fact]
    public void StickTowardTheNextBagArmsTheStealAndBackReturns()
    {
        // Spec §9.2 / §11.1: no lead stick. Toward the next bag = steal, this bag or behind = return.
        Assert.Equal(RunStick.Steal, Baserunning.StickVerb(2, 1));
        Assert.Equal(RunStick.Steal, Baserunning.StickVerb(3, 2));
        Assert.Equal(RunStick.Steal, Baserunning.StickVerb(4, 3));
        Assert.Equal(RunStick.Return, Baserunning.StickVerb(1, 1));
        Assert.Equal(RunStick.Return, Baserunning.StickVerb(4, 1));
        Assert.Equal(RunStick.Return, Baserunning.StickVerb(2, 3));
        Assert.Equal(RunStick.None, Baserunning.StickVerb(3, 1));
        Assert.Equal(RunStick.None, Baserunning.StickVerb(0, 1));
        Assert.Equal(RunStick.None, Baserunning.StickVerb(2, 0));
    }

    [Fact]
    public void StealTargetIsTheNextBagHomeIncluded()
    {
        // D10: the steal of home is legal, armed from third.
        Assert.Equal(2, Baserunning.StealTarget(1));
        Assert.Equal(3, Baserunning.StealTarget(2));
        Assert.Equal(4, Baserunning.StealTarget(3));
        Assert.Equal(0, Baserunning.StealTarget(4));
        Assert.Equal(0, Baserunning.StealTarget(0));
        Assert.Equal(4, Baserunning.NextBag(3));
        Assert.True(Baserunning.CanSteal(1, true, false, false));
        Assert.False(Baserunning.CanSteal(1, true, true, false));
        Assert.True(Baserunning.CanSteal(2, true, true, false));
        Assert.True(Baserunning.CanSteal(3, false, false, true));
        // A steal into a body is offered only when that body is armed too (the double steal, §11.1).
        Assert.True(Baserunning.CanSteal(1, true, true, false, nextRunnerArmed: true));
        Assert.False(Baserunning.CanSelect(4, true, true, true));
        Assert.False(Baserunning.CanSelect(1, false, true, true));
    }

    [Fact]
    public void MiniDiamondPipsAreTheThrowTellPips()
    {
        // D1: there are no leads, so a pip sits on its bag; the map is the throw tell's.
        Assert.Equal(FieldAssist.BagPip(1), Baserunning.DiamondPip(1));
        Assert.Equal(FieldAssist.BagPip(2), Baserunning.DiamondPip(2));
        Assert.Equal((1.0, 0.5), Baserunning.DiamondPip(1));
        Assert.Equal((0.5, 1.0), Baserunning.DiamondPip(2));
        Assert.Equal((0.0, 0.5), Baserunning.DiamondPip(3));
        Assert.Equal((0.5, 0.0), Baserunning.DiamondPip(4));
    }

    [Fact]
    public void DiamondPipInterpolatesTheTwoBagPipsAlongTheSegment()
    {
        // #606: a runner between bags is drawn between their pips; the plate (bag 0) is home's pip.
        Assert.Equal(Baserunning.DiamondPip(1), Baserunning.DiamondPip(1, 2, 0));
        Assert.Equal(Baserunning.DiamondPip(2), Baserunning.DiamondPip(1, 2, 1));
        Assert.Equal((0.75, 0.75), Baserunning.DiamondPip(1, 2, 0.5));
        Assert.Equal((0.25, 0.75), Baserunning.DiamondPip(2, 3, 0.5));
        Assert.Equal((0.25, 0.25), Baserunning.DiamondPip(3, 4, 0.5));
        Assert.Equal(Baserunning.DiamondPip(4), Baserunning.DiamondPip(0, 1, 0));
        Assert.Equal((0.75, 0.25), Baserunning.DiamondPip(0, 1, 0.5));
        // Past first on the run-through keeps going along home → first, beyond the bag.
        var through = Baserunning.DiamondPip(0, 1, 1.2);
        Assert.True(through.U > 1 && through.V > 0.5, $"{through}");
        // A returning runner is the same segment with the fraction falling: 0.25 of the way is nearer the from bag.
        var back = Baserunning.DiamondPip(1, 2, 0.25);
        Assert.True(Math.Abs(back.U - 1) < Math.Abs(back.U - 0.5));
    }

    [Fact]
    public void SyncKeepsAPickedRunnerUntilTheyLeave()
    {
        Assert.Equal(2, Baserunning.SyncSelected(1, picked: false, true, true, false, leadBag: 2));
        Assert.Equal(1, Baserunning.SyncSelected(1, picked: true, true, true, false, leadBag: 2));
        Assert.Equal(2, Baserunning.SyncSelected(1, picked: true, false, true, false, leadBag: 2));
        Assert.Equal(0, Baserunning.SyncSelected(2, picked: true, false, false, false, leadBag: 0));
    }
}
