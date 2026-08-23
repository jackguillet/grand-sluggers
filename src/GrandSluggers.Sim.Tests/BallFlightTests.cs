using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class BallFlightTests
{
    [Fact]
    public void LowLaunchHopsAfterFirstGrass()
    {
        var samples = BallFlight.Trajectory(78, 8, 0);
        var hang = BallFlight.HangTime(samples);
        Assert.True(hang > 0.12, $"hang {hang}");
        Assert.True(samples[^1].T > hang + 0.25, $"rest {samples[^1].T} hang {hang}");
        var hopped = samples.Any(s => s.T > hang + 0.03 && s.Height > 0.18);
        Assert.True(hopped, "low liner should bounce after first grass");
        Assert.True(BallFlight.RestTime(samples) > hang);
        Assert.True(BallFlight.FirstLandingDist(samples) < samples[^1].Dist);
    }

    [Fact]
    public void FlyHangTimeIsFirstGrassNotRest()
    {
        var samples = BallFlight.Trajectory(95, 28, 0);
        var hang = BallFlight.HangTime(samples);
        Assert.InRange(hang, 3.0, 6.5);
        Assert.True(BallFlight.RestTime(samples) >= hang);
        var p = BallFlight.PointAt(samples, 0, 0.5);
        Assert.True(p.Y > 2);
        Assert.True(p.Z > 0);
    }

    [Fact]
    public void CarryIsFirstLandingNotTheRoll()
    {
        var carry = BallFlight.CarryFeet(95, 28, 0);
        var samples = BallFlight.Trajectory(95, 28, 0);
        Assert.Equal(BallFlight.FirstLandingDist(samples), carry);
        Assert.InRange(carry, 300, 450);
        Assert.True(samples[^1].Dist + 0.01 >= carry);
    }

    [Fact]
    public void PointAtAfterHangCanBeOffTheDirtThenDown()
    {
        var samples = BallFlight.Trajectory(70, 10, 0);
        var hang = BallFlight.HangTime(samples);
        var atGrass = BallFlight.PointAt(samples, 0, hang);
        Assert.True(atGrass.Y < 0.6, $"first grass y {atGrass.Y}");
        var later = BallFlight.PointAt(samples, 0, hang + 0.08);
        // hop: height after grass is not stuck at zero for a hopper
        var peak = 0.0;
        for (var t = hang; t < hang + 0.4 && t < BallFlight.RestTime(samples); t += 0.02)
            peak = Math.Max(peak, BallFlight.PointAt(samples, 0, t).Y);
        Assert.True(peak > 0.15, $"hop peak {peak}");
        _ = later;
    }
}
