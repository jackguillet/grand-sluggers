using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class BallFlightTests
{
    [Fact]
    public void LowLaunchHopsAfterFirstGrass()
    {
        var samples = BallFlight.Trajectory(78, 8, 0, rules: Rules.Default);
        var hang = BallFlight.HangTime(samples, rules: Rules.Default);
        Assert.True(hang > 0.12, $"hang {hang}");
        Assert.True(samples[^1].T > hang + 0.25, $"rest {samples[^1].T} hang {hang}");
        var hopped = samples.Any(s => s.T > hang + 0.03 && s.Height > 0.18);
        Assert.True(hopped, "low liner should bounce after first grass");
        Assert.True(BallFlight.RestTime(samples) > hang);
        Assert.True(BallFlight.FirstLandingDist(samples, rules: Rules.Default) < samples[^1].Dist);
    }

    [Fact]
    public void FlyHangTimeIsFirstGrassNotRest()
    {
        var samples = BallFlight.Trajectory(95, 28, 0, rules: Rules.Default);
        var hang = BallFlight.HangTime(samples, rules: Rules.Default);
        Assert.InRange(hang, 3.0 * BallFlight.TimeScale(rules: Rules.Default), 6.5 * BallFlight.TimeScale(rules: Rules.Default));
        Assert.True(BallFlight.RestTime(samples) >= hang);
        var p = BallFlight.PointAt(samples, 0.5, rules: Rules.Default);
        Assert.True(p.Y > 2);
        Assert.True(p.Z > 0);
    }

    [Fact]
    public void ArcadeTimeStretchesHangWithoutCuttingCarry()
    {
        Assert.True(BallFlight.TimeScale(rules: Rules.Default) >= 1.5, $"arcade hang {BallFlight.TimeScale(rules: Rules.Default)}");
        var samples = BallFlight.Trajectory(95, 28, 0, rules: Rules.Default);
        var hang = BallFlight.HangTime(samples, rules: Rules.Default);
        var carry = BallFlight.CarryFeet(95, 28, 0, rules: Rules.Default);
        Assert.True(hang > 4.5, $"fly should hang for gloves, hang {hang}");
        // The C80 copy's drag is 0.0040: the same fly carries 233 ft, inside the same band at 0.70.
        var (lo, hi) = (210.0, 315.0);
        Assert.InRange(carry, lo, hi);
    }

    [Fact]
    public void CarryIsFirstLandingNotTheRoll()
    {
        var carry = BallFlight.CarryFeet(95, 28, 0, rules: Rules.Default);
        var samples = BallFlight.Trajectory(95, 28, 0, rules: Rules.Default);
        Assert.Equal(BallFlight.FirstLandingDist(samples, rules: Rules.Default), carry);
        var (lo, hi) = (210.0, 315.0);
        Assert.InRange(carry, lo, hi);
        Assert.True(samples[^1].Dist + 0.01 >= carry);
    }

    [Fact]
    public void LineHangIsShorterThanAComparableFly()
    {
        var line = BallFlight.Trajectory(95, 16, 0, rules: Rules.Default);
        var fly = BallFlight.Trajectory(95, 28, 0, rules: Rules.Default);
        var lineHang = BallFlight.HangTime(line, rules: Rules.Default);
        var flyHang = BallFlight.HangTime(fly, rules: Rules.Default);
        Assert.True(lineHang < flyHang, $"line hang {lineHang} vs fly {flyHang}");
        Assert.True(lineHang < 2.6 * BallFlight.TimeScale(rules: Rules.Default), $"line should get on you, hang {lineHang}");
        Assert.True(BallFlight.RestTime(line) > lineHang + 0.15, "line skips after first grass");
        Assert.True(line[^1].Dist > BallFlight.FirstLandingDist(line, rules: Rules.Default) + 8, "skip carries past first grass");
    }

    [Fact]
    public void PointAtAfterHangCanBeOffTheDirtThenDown()
    {
        var samples = BallFlight.Trajectory(70, 10, 0, rules: Rules.Default);
        var hang = BallFlight.HangTime(samples, rules: Rules.Default);
        var atGrass = BallFlight.PointAt(samples, hang, rules: Rules.Default);
        Assert.True(atGrass.Y < 0.6, $"first grass y {atGrass.Y}");
        var later = BallFlight.PointAt(samples, hang + 0.08, rules: Rules.Default);
        // hop: height after grass is not stuck at zero for a hopper
        var peak = 0.0;
        for (var t = hang; t < hang + 0.4 && t < BallFlight.RestTime(samples); t += 0.02)
            peak = Math.Max(peak, BallFlight.PointAt(samples, t, rules: Rules.Default).Y);
        Assert.True(peak > 0.15, $"hop peak {peak}");
        _ = later;
    }
}
