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
    public void FastballFliesTrue()
    {
        var mid = PitchFlight.Point("fastball", 0.5);
        var plate = PitchFlight.Point("fastball", 1);
        Assert.InRange(mid.X, -0.12, 0.12);
        Assert.InRange(plate.X, -0.12, 0.12);
        Assert.True(mid.Y > plate.Y, $"fastball should drop, mid {mid.Y} plate {plate.Y}");
        Assert.InRange(mid.Z, 28, 32);
        Assert.InRange(plate.Z, -0.05, 0.05);
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
        var early = PitchFlight.Point("slider", 0.4).X;
        var late = PitchFlight.Point("slider", 0.95).X;
        Assert.True(Math.Abs(early) < 0.45, $"slider still true at 0.4, x={early}");
        Assert.True(late > early + 1.2, $"slider bites late, early {early} late {late}");
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
}
