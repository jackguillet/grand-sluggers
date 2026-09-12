using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class PitchTests
{
    [Fact]
    public void CenterAimIsInTheZone()
    {
        Assert.True(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, false), 7));
        Assert.True(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, false, 0.1, 0.1), 7));
    }

    [Fact]
    public void InsideAimIsABall()
    {
        Assert.False(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, false, 0.95, 0), 7));
    }

    [Fact]
    public void DirtAimIsABall()
    {
        Assert.False(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, false, 0, -0.9), 7));
    }

    [Fact]
    public void HighAimIsABall()
    {
        Assert.False(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, false, 0, 1.3), 7));
    }

    [Fact]
    public void InsideTakeCanPlunkTheBatter()
    {
        const double y = PitchFlight.PlateY;
        var body = AtBatResolver.BatterBodyX(0, Hand.R);
        var walked = AtBatResolver.BatterBodyX(0.5, Hand.R);
        Assert.False(AtBatResolver.HitsBatter(0, 0, y));
        Assert.False(AtBatResolver.HitsBatter(0, 1.7, y));
        Assert.False(AtBatResolver.HitsBatter(0, 0, y - 1.2));
        Assert.True(AtBatResolver.HitsBatter(0, body, y));
        Assert.True(AtBatResolver.HitsBatter(0.5, walked, y + 0.1));
        Assert.False(AtBatResolver.HitsBatter(0, body, y + 0.5), "the body circle is 0.45 ft (spec §4.6)");
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
        Assert.True(gas >= Rules.Default.Pitching.Flight.AirMinSec);
        Assert.True(change <= Rules.Default.Pitching.Flight.AirMaxSec);
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
    public void BreakIsAStickVerbNotAType()
    {
        // Spec §4.3: "curve" / "slider" are retired; an unknown type flies as a fastball.
        var fb = PitchFlight.Point("fastball", 1);
        Assert.Equal(fb, PitchFlight.Point("curve", 1));
        Assert.Equal(fb, PitchFlight.Point("slider", 1));
        Assert.Equal(["fastball", "changeup"], Training.CorePitches);
    }

    [Fact]
    public void BreakGrowsLateAndTheEyeSeesABendMidFlight()
    {
        var f = Rules.Default.Pitching.Flight;
        var straightMid = PitchFlight.Point("fastball", 0.5).X;
        var bentMid = PitchFlight.Point("fastball", 0.5, breakX: 1).X;
        Assert.True(bentMid > straightMid, "the stick bends the ball toward its side mid-flight");
        var atLateFrom = PitchFlight.BreakShiftFt(f.BreakLateFrom, 1, false, f);
        var atPlate = PitchFlight.BreakShiftFt(1, 1, false, f);
        Assert.True(atPlate > atLateFrom, $"drift grows late: {atLateFrom} → {atPlate}");
        Assert.Equal(f.BreakMaxFt, atPlate, 8);
    }

    [Fact]
    public void BreakStepIsDirectionOnlyAtAPitchStatRate()
    {
        var r = Rules.Default;
        var f = r.Pitching.Flight;
        Assert.Equal(PitchFlight.BreakStep(0, 1, 0.1, 5, r), PitchFlight.BreakStep(0, 0.2, 0.1, 5, r), 8);
        Assert.Equal(f.BreakRatePerSec * 0.1, PitchFlight.BreakStep(0, 1, 0.1, 5, r), 8);
        Assert.True(PitchFlight.BreakStep(0, 1, 0.1, 10, r) > PitchFlight.BreakStep(0, 1, 0.1, 2, r), "a better arm bends faster");
        Assert.Equal(0, PitchFlight.BreakStep(0, 0, 0.1, 5, r));
        Assert.Equal(-1, PitchFlight.BreakStep(-0.9, -1, 1, 5, r));
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
    public void FullBreakMovesTheCrossingHalfAZoneAndChargeOrChangeupTakeATenth()
    {
        var f = Rules.Default.Pitching.Flight;
        var heart = PitchFlight.Point("fastball", 1, 0, 0);
        var broke = PitchFlight.Point("fastball", 1, 0, 0, breakX: 1);
        Assert.Equal(f.BreakMaxFt, broke.X - heart.X, 6);
        Assert.Equal(StrikeZoneGeometry.HalfWidth / 2, f.BreakMaxFt, 2);
        Assert.Equal(-f.BreakMaxFt, PitchFlight.Point("fastball", 1, 0, 0, breakX: -1).X - heart.X, 6);
        var charged = PitchFlight.Point("fastball", 1, 0, 0, breakX: 1, charged: true);
        var change = PitchFlight.Point("fastball", 1, 0, 0, breakX: 1, changeup: true);
        Assert.Equal(f.BreakMaxFt * f.BreakDampedMul, charged.X - heart.X, 6);
        Assert.Equal(f.BreakMaxFt * f.BreakDampedMul, change.X - PitchFlight.Point("fastball", 1, 0, 0, changeup: true).X, 6);
        // Full stick over the top of the range stays at the cap.
        Assert.Equal(broke.X, PitchFlight.Point("fastball", 1, 0, 0, breakX: 3).X, 6);
    }

    [Fact]
    public void EveryShapeCrossesAtItsAimAndHeightIsAPitchProperty()
    {
        // Spec §4.2: a normal / charged pitch crosses mid-zone; a changeup crosses lower by a pitch number.
        var fb = PitchFlight.Point("fastball", 1);
        Assert.Equal(StrikeZoneGeometry.CenterY, fb.Y, 6);
        Assert.Equal(0, fb.X, 6);
        var charged = PitchFlight.Point("fastball", 1, charged: true);
        Assert.Equal(fb.Y, charged.Y, 6);
        var change = PitchFlight.Point("fastball", 1, changeup: true);
        Assert.Equal(StrikeZoneGeometry.CenterY - Rules.Default.Pitching.Shapes.ChangeupDropFt, change.Y, 6);
        Assert.True(change.Y > StrikeZoneGeometry.Bottom, "the changeup dumps inside the zone by default");
        Assert.True(change.Y < StrikeZoneGeometry.CenterY - StrikeZoneGeometry.Height / 4);
    }

    [Fact]
    public void NiceReleaseAddsFivePercent()
    {
        var plain = new PitchCommand("fastball", 1, false);
        var nice = plain with { Nice = true };
        Assert.Equal(Rules.Default.Pitching.Release.NiceMul, AtBatResolver.PitchSpeedMph(nice, 7) / AtBatResolver.PitchSpeedMph(plain, 7), 8);
        var band = Rules.Default.Pitching.Release.NiceBandSec;
        Assert.True(ChargeFeel.NiceRelease(1, band - 0.01, 0.5));
        Assert.False(ChargeFeel.NiceRelease(1, band + 0.01, 0.5));
        Assert.False(ChargeFeel.NiceRelease(0.9, 0, 0.5));
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
    public void RubberWalkMovesTheCrossingTheSameWorldDistanceAsTheBodyOnce()
    {
        // Spec §4.2, #577: release hand and crossing both move HomeSet.PitcherWalk per unit, for every seat.
        var heart = PitchFlight.Point("fastball", 1, 0, 0, rubberX: 0);
        var walked = PitchFlight.Point("fastball", 1, 0, 0, rubberX: 1);
        Assert.Equal(HomeSet.PitcherWalk, walked.X - heart.X, 6);
        Assert.Equal(HomeSet.PitcherWalk, PitchFlight.Release(1).X - PitchFlight.Release(0).X, 6);
        var command = new PitchCommand("fastball", 0, false, RubberX: -0.5);
        Assert.Equal(-0.5 * HomeSet.PitcherWalk, PitchFlight.Crossing(command).X, 6);
    }

    [Fact]
    public void ChangeupFlagIsSlowerThanAMaxFastball()
    {
        var maxFb = AtBatResolver.PitchSpeedMph(new PitchCommand("fastball", 1, false), 7);
        var change = AtBatResolver.PitchSpeedMph(new PitchCommand("fastball", 1, false, Changeup: true), 7);
        var typed = AtBatResolver.PitchSpeedMph(new PitchCommand("changeup", 1, false), 7);
        Assert.True(change < maxFb, $"changeup {change} vs MAX fastball {maxFb}");
        Assert.True(typed < maxFb, $"typed changeup {typed} vs MAX {maxFb}");
        Assert.InRange(change, typed - 0.5, typed + 0.5);
    }
}
