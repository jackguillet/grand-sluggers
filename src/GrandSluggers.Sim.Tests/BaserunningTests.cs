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
    public void StealTargetNeverHome()
    {
        Assert.Equal(2, Baserunning.StealTarget(1));
        Assert.Equal(3, Baserunning.StealTarget(2));
        Assert.Equal(0, Baserunning.StealTarget(3));
        Assert.Equal(0, Baserunning.StealTarget(4));
        Assert.Equal(0, Baserunning.StealTarget(0));
        Assert.Equal(4, Baserunning.NextBag(3));
        Assert.True(Baserunning.CanSteal(1, true, false, false));
        Assert.False(Baserunning.CanSteal(1, true, true, false));
        Assert.True(Baserunning.CanSteal(2, true, true, false));
        Assert.False(Baserunning.CanSteal(3, false, false, true));
        Assert.False(Baserunning.CanSelect(4, true, true, true));
        Assert.False(Baserunning.CanSelect(1, false, true, true));
    }

    [Fact]
    public void MiniLeadWalksOffTheBag()
    {
        Assert.Equal(FieldAssist.BagPip(1), Baserunning.DiamondPip(1));
        Assert.Equal(FieldAssist.BagPip(2), Baserunning.DiamondPip(2));
        var glued = Baserunning.MiniLead(1, 0);
        var walked = Baserunning.MiniLead(1, 1);
        Assert.Equal(Baserunning.DiamondPip(1).U, glued.U, 3);
        Assert.Equal(Baserunning.DiamondPip(1).V, glued.V, 3);
        Assert.True(walked.U < glued.U);
        Assert.True(walked.V > glued.V);
        var second = Baserunning.MiniLead(2, 1);
        Assert.True(second.U < Baserunning.DiamondPip(2).U);
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
