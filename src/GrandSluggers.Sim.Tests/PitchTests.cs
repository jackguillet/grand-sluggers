using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class PitchTests
{
    [Fact]
    public void CenterAimIsInTheZone()
    {
        Assert.True(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, 0, false), 7));
        Assert.True(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, 0, false, 0.1, 0.1), 7));
    }

    [Fact]
    public void InsideAimIsABall()
    {
        Assert.False(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, 0, false, 0.95, 0), 7));
    }

    [Fact]
    public void DirtAimIsABall()
    {
        Assert.False(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, 0, false, 0, -0.9), 7));
    }

    [Fact]
    public void HighAimIsABall()
    {
        Assert.False(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, 0, false, 0, 0.9), 7));
    }

    [Fact]
    public void AirSecondsIsSluggersPaceNotMlbNinety()
    {
        var meat = PitchFlight.AirSeconds(86);
        var gas = PitchFlight.AirSeconds(100);
        var change = PitchFlight.AirSeconds(72);
        Assert.InRange(meat, 0.85, 1.15);
        Assert.True(gas < meat, $"charged FB {gas} vs meat {meat}");
        Assert.True(change > meat, $"changeup {change} vs meat {meat}");
        Assert.True(gas >= PitchFlight.AirMin);
        Assert.True(change <= PitchFlight.AirMax);
    }

    [Fact]
    public void PointFromAHandStartsAtThatHand()
    {
        var hand = (2.4, 4.8, 55.0);
        var start = PitchFlight.Point("fastball", 0, from: hand);
        Assert.Equal(hand.Item1, start.X, 3);
        Assert.Equal(hand.Item2, start.Y, 3);
        Assert.Equal(hand.Item3, start.Z, 3);
        var plate = PitchFlight.Point("fastball", 1, from: hand);
        Assert.InRange(plate.Z, -0.05, 0.05);
    }

    [Fact]
    public void FastballFliesTrue()
    {
        var rel = PitchFlight.Release();
        var mid = PitchFlight.Point("fastball", 0.5);
        var plate = PitchFlight.Point("fastball", 1);
        Assert.True(rel.X > 1.0, $"release is the hand, not the torso x={rel.X}");
        Assert.True(rel.Z < PitchFlight.MoundZ, $"release toward the plate z={rel.Z}");
        Assert.InRange(plate.X, -0.2, 0.2);
        Assert.True(mid.Y > plate.Y, $"fastball should drop, mid {mid.Y} plate {plate.Y}");
        Assert.InRange(mid.Z, 26, 34);
        Assert.InRange(plate.Z, -0.05, 0.05);
        Assert.True(Math.Abs(mid.X - rel.X) > 0.3, "hand offset fades toward the plate");
    }

    [Fact]
    public void ChangeupHangsThenDumps()
    {
        var hang = PitchFlight.Point("changeup", 0.5).Y;
        var fb = PitchFlight.Point("fastball", 0.5).Y;
        var plate = PitchFlight.Point("changeup", 1).Y;
        Assert.True(hang >= fb - 0.2, $"changeup should hang, hang {hang} vs fb {fb}");
        Assert.True(hang - plate > 1.0, $"then dump, hang {hang} plate {plate}");
    }

    [Fact]
    public void SliderBreaksLate()
    {
        var fb = PitchFlight.Point("fastball", 0.4).X;
        var early = PitchFlight.Point("slider", 0.4).X;
        var late = PitchFlight.Point("slider", 0.95).X;
        Assert.True(Math.Abs(early - fb) < 0.45, $"slider still true at 0.4, x={early} fb={fb}");
        Assert.True(late > early + 0.8, $"slider bites late, early {early} late {late}");
    }

    [Fact]
    public void CurveIsTwoPlane()
    {
        var mid = PitchFlight.Point("curve", 0.5);
        var fb = PitchFlight.Point("fastball", 0.5);
        Assert.True(Math.Abs(mid.X - fb.X) > 0.8, $"curve sweep, curveX {mid.X} fbX {fb.X}");
        Assert.True(mid.Y > fb.Y + 0.4, $"curve hump, curveY {mid.Y} fbY {fb.Y}");
    }

    [Fact]
    public void StickAimMovesThePlateTarget()
    {
        var inOff = PitchFlight.Point("fastball", 1, 0.8, -0.4);
        var heart = PitchFlight.Point("fastball", 1, 0, 0);
        Assert.True(inOff.X > heart.X + 1.0);
        Assert.True(inOff.Y < heart.Y - 0.3);
    }

    [Fact]
    public void LiveBreakMovesAFastballOffTheHeart()
    {
        var heart = PitchFlight.Point("fastball", 1, 0, 0);
        var broke = PitchFlight.Point("fastball", 1, 0, 0, breakX: 1);
        Assert.True(Math.Abs(broke.X - heart.X) > 1.0, $"break {broke.X} vs heart {heart.X}");
    }

    [Fact]
    public void ChangeupModifierHangsThenDumps()
    {
        var hang = PitchFlight.Point("fastball", 0.5, changeup: true).Y;
        var fb = PitchFlight.Point("fastball", 0.5).Y;
        var plate = PitchFlight.Point("fastball", 1, changeup: true).Y;
        Assert.True(hang >= fb - 0.2, $"changeup hang {hang} vs fb {fb}");
        Assert.True(hang - plate > 1.0, $"changeup dump hang {hang} plate {plate}");
    }

    [Fact]
    public void RubberWalkMovesPlateX()
    {
        var heart = PitchFlight.Point("fastball", 1, 0, 0, rubberX: 0);
        var walked = PitchFlight.Point("fastball", 1, 0, 0, rubberX: 1);
        Assert.True(walked.X > heart.X + 0.4, $"rubber {walked.X} vs heart {heart.X}");
    }

    [Fact]
    public void ChangeupFlagIsSlowerThanAMaxFastball()
    {
        var maxFb = AtBatResolver.PitchSpeedMph(new PitchCommand("fastball", 1, 0, false), 7);
        var change = AtBatResolver.PitchSpeedMph(new PitchCommand("fastball", 1, 0, false, Changeup: true), 7);
        var typed = AtBatResolver.PitchSpeedMph(new PitchCommand("changeup", 1, 0, false), 7);
        Assert.True(change < maxFb, $"changeup {change} vs MAX fastball {maxFb}");
        Assert.True(typed < maxFb, $"typed changeup {typed} vs MAX {maxFb}");
        Assert.InRange(change, typed - 0.5, typed + 0.5);
    }
}
