using Xunit;
namespace GrandSluggers.Sim.Tests;

public sealed class ControllerInputTests
{
    [Fact]
    public void StarAndRunnerOrdersAreIndependentDuringASwing()
    {
        var p = new ControllerInput();
        p.Tick(ControllerButton.LB, 1, 1, 1, 0);
        Assert.True(p.IsHeld(ControllerLayout.Star));
        Assert.True(p.IsDown(ControllerLayout.Advance));
        Assert.True(p.IsDown(ControllerLayout.Ball));
        Assert.Equal(1, p.TargetFlick);
        p.Tick(ControllerButton.LB, 0, 1, 0, 0);
        Assert.True(p.IsUp(ControllerLayout.Ball));
        Assert.True(p.IsHeld(ControllerLayout.Star));
        Assert.True(p.IsHeld(ControllerLayout.Advance));
        Assert.False(p.IsDown(ControllerLayout.Advance));
    }
    [Fact]
    public void TriggerNoiseAndHoldingThroughPossessionCannotCreateAnotherThrow()
    {
        var p = new ControllerInput();
        p.Tick(0, .51, 0, 0, 0);
        Assert.True(p.IsDown(ControllerLayout.Ball));
        foreach (var value in new[] { .49, .44, .6, 1.0 })
        {
            p.Tick(0, value, 0, 0, 0);
            Assert.True(p.IsHeld(ControllerLayout.Ball));
            Assert.False(p.IsDown(ControllerLayout.Ball));
            Assert.False(p.IsUp(ControllerLayout.Ball));
        }
        p.Tick(0, .3, 0, 0, 0);
        Assert.True(p.IsUp(ControllerLayout.Ball));
    }
    [Fact]
    public void PauseRetryAndRecoveryRequireReleaseAndRightStickNeutral()
    {
        var p = new ControllerInput();
        p.Tick(ControllerButton.South | ControllerButton.North | ControllerButton.LB, 1, 1, 1, 0);
        p.Catch();
        p.Tick(ControllerButton.South | ControllerButton.North | ControllerButton.LB, 1, 1, 0, 1);
        Assert.Equal(ControllerButton.None, p.Held);
        Assert.Equal(0, p.TargetFlick); Assert.Equal(0, p.TargetHeld);
        p.Tick(0, 0, 0, 0, 0);
        Assert.Equal(ControllerButton.None, p.Up);
        p.Tick(ControllerButton.North, 1, 0, 0, 1);
        Assert.True(p.IsDown(ControllerLayout.Jump));
        Assert.True(p.IsDown(ControllerLayout.Ball));
        Assert.Equal(2, p.TargetFlick);
    }
    [Fact]
    public void TargetFlickLatchesAndMustRecenterBeforeChangingSelection()
    {
        var p = new ControllerInput();
        p.Tick(0, 0, 0, 1, .4);
        Assert.Equal(1, p.TargetFlick);
        p.Tick(0, 0, 0, -1, .4);
        Assert.Equal(0, p.TargetFlick); Assert.Equal(1, p.ThrowTarget);
        p.Tick(0, 0, 0, 0, 0);
        Assert.Equal(1, p.ThrowTarget);
        p.Tick(0, 0, 0, -1, .4);
        Assert.Equal(3, p.ThrowTarget);
        p.ClearTarget(); Assert.Equal(0, p.ThrowTarget);
    }
    [Fact]
    public void ControllersNeverShareEdgesOrTargets()
    {
        var p1 = new ControllerInput(); var p2 = new ControllerInput();
        p1.Tick(ControllerButton.North, 1, 1, 1, 0); p2.Tick(0, 0, 0, 0, 0);
        Assert.True(p1.IsDown(ControllerLayout.Jump));
        Assert.Equal(0, p2.TargetFlick); Assert.Equal(ControllerButton.None, p2.Down);
        Assert.NotEqual(ControllerLayout.Star, ControllerLayout.Advance);
        Assert.NotEqual(ControllerButton.South, ControllerLayout.Ball);
        Assert.Equal(ControllerButton.North, ControllerLayout.Jump);
    }
}
