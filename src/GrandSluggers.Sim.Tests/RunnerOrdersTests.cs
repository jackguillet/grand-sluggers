using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class RunnerOrdersTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    Match Game()
    {
        var m = Match.Slice(_content, seed: 7);
        Assert.True(m.StationRunner(1, m.Offense.BattingOrder[2]));
        Assert.True(m.StationRunner(3, m.Offense.BattingOrder[3]));
        return m;
    }

    [Fact]
    public void SelectionMovesNobodyAndHeldOrdersDoNotTransferToAnotherRunner()
    {
        var m = Game();
        var orders = m.ControllerRunners;
        orders.Apply(m, new(SelectBag: 1));
        Assert.All(m.Runners, r => Assert.False(r.Moving));
        orders.Apply(m, new(Advance: true));
        Assert.True(m.RunnerAt(1)!.Moving);
        orders.Apply(m, new(SelectBag: 3, Advance: true));
        Assert.False(m.RunnerAt(3)!.Moving);
        orders.Apply(m, new());
        orders.Apply(m, new(Advance: true));
        Assert.True(m.RunnerAt(3)!.Moving);
    }

    [Fact]
    public void ReturnReversesImmediatelyAndHaltRequiresFreshPress()
    {
        var m = Game();
        var o = m.ControllerRunners;
        o.Apply(m, new(SelectBag: 1, Advance: true));
        m.PitchSetup.Advance(.2);
        o.Apply(m, new(Return: true));
        m.PitchSetup.Advance(.01);
        Assert.Equal(RunnerPhase.Returning, m.RunnerAt(1)!.Phase);
        o.Apply(m, new(Return: true, Halt: true));
        Assert.True(m.RunnerAt(1)!.Held);
        o.Apply(m, new(Return: true));
        Assert.True(m.RunnerAt(1)!.Held);
        o.Apply(m, new());
        o.Apply(m, new(Return: true));
        Assert.False(m.RunnerAt(1)!.Held);
    }

    [Theory]
    [InlineData(2)] [InlineData(4)]
    public void EmptySelectionDoesNotSendAll(int bag)
    {
        var m = Game();
        m.ControllerRunners.Apply(m, new(SelectBag: bag, Advance: true));
        Assert.False(m.ControllerRunners.AllSelected);
        Assert.Null(m.ControllerRunners.Selected(m));
        Assert.All(m.Runners, r => Assert.False(r.Moving));
    }

    [Fact]
    public void RetiredSelectionStaysEmptyUntilDeliberatelySelectingAll()
    {
        var m = Game();
        m.ControllerRunners.Apply(m, new(SelectBag: 1));
        m.RunnerAt(1)!.Retire();
        m.ControllerRunners.Apply(m, new(Advance: true));
        Assert.False(m.RunnerAt(3)!.Moving);
        m.ControllerRunners.Apply(m, new(SelectAll: true));
        m.ControllerRunners.Apply(m, new(Advance: true));
        Assert.True(m.RunnerAt(3)!.Moving);
    }

    [Fact]
    public void BothShouldersHaltAndReleasingOneDoesNotResume()
    {
        var m = Game();
        m.ControllerRunners.Apply(m, new(Advance: true));
        m.ControllerRunners.Apply(m, new(Advance: true, Return: true));
        m.ControllerRunners.Apply(m, new(Advance: true));
        Assert.All(m.Runners, r => Assert.True(r.Held));
    }
}
