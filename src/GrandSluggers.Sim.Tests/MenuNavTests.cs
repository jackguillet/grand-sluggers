using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class MenuNavTests
{
    [Fact]
    public void HeldAxisFiresOnceUntilItRests()
    {
        var armed = 0f;
        Assert.Equal(1, MenuNav.AxisStep(0.9f, ref armed));
        Assert.Equal(0, MenuNav.AxisStep(0.9f, ref armed));
        Assert.Equal(0, MenuNav.AxisStep(0.6f, ref armed));
        Assert.Equal(-1, MenuNav.AxisStep(-0.9f, ref armed));
        Assert.Equal(0, MenuNav.AxisStep(-0.9f, ref armed));
        Assert.Equal(0, MenuNav.AxisStep(0f, ref armed));
        Assert.Equal(1, MenuNav.AxisStep(0.9f, ref armed));
    }

    [Fact]
    public void FlickAgainAfterPartialRelease()
    {
        var armed = 0f;
        Assert.Equal(1, MenuNav.AxisStep(0.9f, ref armed));
        Assert.Equal(0, MenuNav.AxisStep(0.4f, ref armed));
        Assert.Equal(0, MenuNav.AxisStep(0.2f, ref armed));
        Assert.Equal(1, MenuNav.AxisStep(0.9f, ref armed));
    }

    [Fact]
    public void OpenWhileHeldDoesNotStep()
    {
        var armed = MenuNav.Arm(0.8f);
        Assert.Equal(0, MenuNav.AxisStep(0.8f, ref armed));
        Assert.Equal(0, MenuNav.AxisStep(0f, ref armed));
        Assert.Equal(1, MenuNav.AxisStep(0.8f, ref armed));
    }

    [Fact]
    public void TapStillWorksWhileAnalogIsLatched()
    {
        var armed = MenuNav.Arm(0.9f);
        Assert.Equal(0, MenuNav.Step(0.9f, 0, ref armed));
        Assert.Equal(-1, MenuNav.Step(0.9f, -1, ref armed));
        Assert.Equal(1, MenuNav.Step(0.9f, 0, ref armed));
    }

    [Fact]
    public void DriftBelowThresholdIsDead()
    {
        var armed = 0f;
        Assert.Equal(0, MenuNav.AxisStep(0.3f, ref armed));
        Assert.Equal(0, MenuNav.AxisStep(-0.3f, ref armed));
    }

    [Fact]
    public void HeldAxisDoesNotRepeat()
    {
        var armed = 0f;
        Assert.Equal(1, MenuNav.AxisStep(0.95f, ref armed));
        Assert.Equal(0, MenuNav.AxisStep(0.95f, ref armed));
        Assert.Equal(0, MenuNav.Step(0.95f, 0, ref armed));
        Assert.Equal(0, MenuNav.AxisStep(0.95f, ref armed));
        Assert.Equal(0, MenuNav.AxisStep(0.2f, ref armed));
        Assert.Equal(1, MenuNav.AxisStep(0.95f, ref armed));
    }

    [Fact]
    public void InertialScrollIsOnePageThenRest()
    {
        var spinning = false;
        Assert.Equal(1, MenuNav.WheelStep(-2f, ref spinning));
        Assert.Equal(0, MenuNav.WheelStep(-1.2f, ref spinning));
        Assert.Equal(0, MenuNav.WheelStep(-0.4f, ref spinning));
        Assert.Equal(0, MenuNav.WheelStep(0f, ref spinning));
        Assert.Equal(-1, MenuNav.WheelStep(2f, ref spinning));
        spinning = true;
        Assert.Equal(0, MenuNav.WheelStep(-3f, ref spinning));
    }
}
