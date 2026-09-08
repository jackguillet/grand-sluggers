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
    public void HardHoldRepeatsAfterABeat()
    {
        var armed = 0f;
        var hold = 0f;
        Assert.Equal(1, MenuNav.AxisStep(0.95f, 0.016f, ref armed, ref hold));
        Assert.Equal(0, MenuNav.AxisStep(0.95f, 0.016f, ref armed, ref hold));
        Assert.Equal(0, MenuNav.AxisStep(0.95f, 0.30f, ref armed, ref hold));
        Assert.Equal(1, MenuNav.AxisStep(0.95f, 0.20f, ref armed, ref hold));
        Assert.Equal(0, MenuNav.AxisStep(0.55f, 0.20f, ref armed, ref hold));
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

public class StickPlayTests
{
    [Fact]
    public void SittingDriftBecomesRestAndLiveIsZero()
    {
        float restX = 0, restY = 0, still = 0;
        var lastX = -0.35f;
        var lastY = 0f;
        for (var i = 0; i < 20; i++)
        {
            var next = StickPlay.Recenter(-0.35f, 0, lastX, lastY, restX, restY, still, 0.02f);
            restX = next.RestX;
            restY = next.RestY;
            still = next.Still;
            lastX = -0.35f;
            lastY = 0;
        }
        Assert.True(Math.Abs(restX + 0.35f) < 0.001f);
        Assert.Equal(0, StickPlay.Live(-0.35f, restX));
        Assert.True(StickPlay.Live(-0.95f, restX) < -0.4f);
    }

    [Fact]
    public void HeldThrowDoesNotGetEatenByRecenter()
    {
        float restX = 0, restY = 0, still = 0;
        for (var i = 0; i < 40; i++)
        {
            var next = StickPlay.Recenter(-0.95f, 0, -0.95f, 0, restX, restY, still, 0.02f);
            restX = next.RestX;
            restY = next.RestY;
            still = next.Still;
        }
        Assert.Equal(0, restX);
        Assert.True(StickPlay.Live(-0.95f, restX) < -0.8f);
    }
}
