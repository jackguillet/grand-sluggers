using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class StickPlayTests
{
    [Fact]
    public void CatchOffCenterIsDeadUntilTheStickPassesThroughRest()
    {
        var pad = new StickPlay.Pad();
        pad.Catch(0.70f, 0);
        Assert.False(pad.SeenCenter);
        Assert.Equal(0, pad.LiveX(0.70f));
        pad.Tick(0.90f, 0, 0.02f);
        Assert.Equal(0, pad.LiveX(0.90f));
        pad.Tick(0.90f, 0, 1f);
        Assert.Equal(0, pad.LiveX(0.90f));
        pad.Tick(0.10f, 0, 0.02f);
        Assert.True(pad.SeenCenter);
        Assert.Equal(0, pad.LiveX(0.10f));
        pad.Tick(0.90f, 0, 0.02f);
        Assert.True(pad.LiveX(0.90f) > 0.4f);
    }

    [Fact]
    public void CatchAtRestThenAThrowWalks()
    {
        var pad = new StickPlay.Pad();
        pad.Catch(0.05f, 0);
        Assert.True(pad.SeenCenter);
        Assert.Equal(0, pad.LiveX(0.05f));
        pad.Tick(-0.95f, 0, 0.02f);
        Assert.True(pad.LiveX(-0.95f) < -0.5f);
    }

    [Fact]
    public void HeldThrowDoesNotGetEatenByRecenter()
    {
        var pad = new StickPlay.Pad();
        pad.Catch(0, 0);
        for (var i = 0; i < 40; i++)
            pad.Tick(-0.95f, 0, 0.02f);
        Assert.Equal(0, pad.RestX);
        Assert.True(pad.LiveX(-0.95f) < -0.5f);
    }

    [Fact]
    public void SittingDriftNearCenterBecomesRest()
    {
        var pad = new StickPlay.Pad();
        pad.Catch(0, 0);
        for (var i = 0; i < 20; i++)
            pad.Tick(-0.38f, 0, 0.02f);
        Assert.True(Math.Abs(pad.RestX + 0.38f) < 0.001f);
        Assert.Equal(0, pad.LiveX(-0.38f));
        Assert.True(pad.LiveX(-0.95f) < -0.4f);
    }
}
