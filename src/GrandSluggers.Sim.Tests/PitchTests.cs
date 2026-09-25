using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class PitchTests
{
    [Fact]
    public void CenterAimIsInTheZone()
    {
        Assert.True(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, false), 7, rules: Rules.Default));
        Assert.True(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, false, 0.1, 0.1), 7, rules: Rules.Default));
    }

    [Fact]
    public void InsideAimIsABall()
    {
        Assert.False(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, false, 0.95, 0), 7, rules: Rules.Default));
    }

    [Fact]
    public void DirtAimIsABall()
    {
        Assert.False(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, false, 0, -0.9), 7, rules: Rules.Default));
    }

    [Fact]
    public void HighAimIsABall()
    {
        Assert.False(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, false, 0, 1.3), 7, rules: Rules.Default));
    }

    [Fact]
    public void InsideTakeCanPlunkTheBatter()
    {
        var y = StrikeZoneGeometry.Reference.CenterY;
        var body = AtBatResolver.BatterBodyX(0, Hand.R);
        var walked = AtBatResolver.BatterBodyX(0.5, Hand.R);
        Assert.False(AtBatResolver.HitsBatter(0, 0, y, Rules.Default, StrikeZoneGeometry.Reference));
        Assert.False(AtBatResolver.HitsBatter(0, 1.7, y, Rules.Default, StrikeZoneGeometry.Reference));
        Assert.False(AtBatResolver.HitsBatter(0, 0, y - 1.2, Rules.Default, StrikeZoneGeometry.Reference));
        Assert.True(AtBatResolver.HitsBatter(0, body, y, Rules.Default, StrikeZoneGeometry.Reference));
        Assert.True(AtBatResolver.HitsBatter(0.5, walked, y + 0.1, Rules.Default, StrikeZoneGeometry.Reference));
        Assert.False(AtBatResolver.HitsBatter(0, body, y + 0.5, Rules.Default, StrikeZoneGeometry.Reference), "the body circle is 0.45 ft (spec §4.6)");
    }

    [Fact]
    public void AirSecondsIsSluggersPaceNotMlbNinety()
    {
        var fam = Rules.Default.Pitching.Families;
        var meat = PitchFlight.AirSeconds(fam.Fastball.Mph, rules: Rules.Default);
        var gas = PitchFlight.AirSeconds(fam.Fastball.Mph + fam.Fastball.ChargeMph, rules: Rules.Default);
        var change = PitchFlight.AirSeconds(fam.Changeup.Mph, rules: Rules.Default);
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
        var start = PitchFlight.Point("fastball", 0, from: hand, rules: Rules.Default);
        Assert.Equal(hand.Item1, start.X, 3);
        Assert.Equal(hand.Item2, start.Y, 3);
        Assert.Equal(hand.Item3, start.Z, 3);
        var plate = PitchFlight.Point("fastball", 1, from: hand, rules: Rules.Default);
        Assert.Equal(StrikeZoneGeometry.PlateZ, plate.Z, 9); // the flight ends on the zone's plane, the plate's front edge (§4.4)
    }

    [Fact]
    public void FastballFliesTrue()
    {
        var rel = PitchFlight.Release(rules: Rules.Default);
        var mid = PitchFlight.Point("fastball", 0.5, rules: Rules.Default);
        var plate = PitchFlight.Point("fastball", 1, rules: Rules.Default);
        Assert.True(rel.X > 1.0, $"release is the hand, not the torso x={rel.X}");
        Assert.True(rel.Z < Diamond.Mound, $"release toward the plate z={rel.Z}");
        Assert.InRange(plate.X, -0.2, 0.2);
        Assert.True(mid.Y > plate.Y, $"fastball should drop, mid {mid.Y} plate {plate.Y}");
        // The C80 copy's mound is at 8/9 of the distance: halfway is 25.6 ft, inside the same band at 8/9.
        var (lo, hi) = (23.0, 30.0);
        Assert.InRange(mid.Z, lo, hi);
        Assert.Equal(StrikeZoneGeometry.PlateZ, plate.Z, 9); // the flight ends on the zone's plane, the plate's front edge (§4.4)
        Assert.True(Math.Abs(mid.X - rel.X) > 0.3, "hand offset fades toward the plate");
    }

    [Fact]
    public void ChangeupHangsThenDumps()
    {
        var hangU = Rules.Default.Pitching.Families.Changeup.HangUntil;
        var hang = PitchFlight.Point(PitchFamily.Changeup, hangU, rules: Rules.Default).Y;
        var fb = PitchFlight.Point("fastball", hangU, rules: Rules.Default).Y;
        var plate = PitchFlight.Point(PitchFamily.Changeup, 1, rules: Rules.Default).Y;
        Assert.True(hang >= fb, $"changeup should hang, hang {hang} vs fb {fb}");
        Assert.True(hang - plate > 1.0, $"then dump, hang {hang} plate {plate}");
    }

    [Fact]
    public void BreakIsAStickVerbNotAFamilyAndAnUnauthoredFamilyDoesNotFly()
    {
        // Spec §4.3 (#810): the type is a family id from the closed library, and the old rule —
        // "an unknown type flies as a fastball" — is retired. Break stays a stick verb: it is not
        // in the library at all, so the retired trajectory string "curve" is simply not a family.
        Assert.DoesNotContain("curve", PitchFamily.All);
        Assert.DoesNotContain("break", PitchFamily.All);

        var unknown = Assert.Throws<ArgumentException>(() => PitchFlight.Point("curve", 1, rules: Rules.Default));
        Assert.Contains("curve", unknown.Message, StringComparison.Ordinal);
        Assert.Contains("not a pitch family", unknown.Message, StringComparison.Ordinal);

        // "curveball" is different: it is in the library, and a table with no row for it must say so
        // by name rather than quietly flying it as a fastball. The shipped data authors all three, so
        // the unauthored case is a copy of it with the three optional rows cleared.
        var bare = RuleCopies.TwoFamilies();
        foreach (var unauthored in new[] { PitchFamily.Curveball, PitchFamily.Slider, PitchFamily.Sinker })
        {
            var fell = Assert.Throws<InvalidOperationException>(() => PitchFlight.Point(unauthored, 1, rules: bare));
            Assert.Contains(unauthored, fell.Message, StringComparison.Ordinal);
            Assert.Contains("no authored row", fell.Message, StringComparison.Ordinal);
            Assert.Throws<InvalidOperationException>(() =>
                AtBatResolver.PitchSpeedMph(new PitchCommand(unauthored, 0, false), 5, bare));

            // … and on the shipped root the same family flies (#860).
            Assert.True(Rules.Default.Pitching.Families.IsAuthored(unauthored));
            Assert.True(AtBatResolver.PitchSpeedMph(new PitchCommand(unauthored, 0, false), 5, rules: Rules.Default) > 0);
        }

        // Training offers the loaded table's authored rows (#888): the two-family copy offers two, the
        // shipped data the whole library.
        Assert.Equal(["fastball", "changeup"], Training.PitchesOf(bare));
        Assert.Equal(bare.Pitching.Families.Authored, Training.PitchesOf(bare));
        Assert.Equal(PitchFamily.All, Rules.Default.Pitching.Families.Authored);
    }

    [Fact]
    public void BreakGrowsLateAndTheEyeSeesABendMidFlight()
    {
        var f = Rules.Default.Pitching.Flight;
        var straightMid = PitchFlight.Point("fastball", 0.5, rules: Rules.Default).X;
        var bentMid = PitchFlight.Point("fastball", 0.5, breakX: 1, rules: Rules.Default).X;
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
        var inOff = PitchFlight.Point("fastball", 1, Rules.Default, 0.8, -0.4);
        var heart = PitchFlight.Point("fastball", 1, Rules.Default, 0, 0);
        Assert.True(inOff.X > heart.X + 1.0);
        Assert.True(inOff.Y < heart.Y - 0.3);
    }

    [Fact]
    public void FullBreakMovesTheCrossingHalfAZoneAndChargeOrChangeupTakeATenth()
    {
        var f = Rules.Default.Pitching.Flight;
        var heart = PitchFlight.Point("fastball", 1, Rules.Default, 0, 0);
        var broke = PitchFlight.Point("fastball", 1, Rules.Default, 0, 0, breakX: 1);
        Assert.Equal(f.BreakMaxFt, broke.X - heart.X, 6);
        Assert.Equal(StrikeZoneGeometry.HalfWidth / 2, f.BreakMaxFt, 2);
        Assert.Equal(-f.BreakMaxFt, PitchFlight.Point("fastball", 1, Rules.Default, 0, 0, breakX: -1).X - heart.X, 6);
        var charged = PitchFlight.Point("fastball", 1, Rules.Default, 0, 0, breakX: 1, charged: true);
        var change = PitchFlight.Point(PitchFamily.Changeup, 1, Rules.Default, 0, 0, breakX: 1);
        Assert.Equal(f.BreakMaxFt * f.BreakDampedMul, charged.X - heart.X, 6);
        Assert.Equal(f.BreakMaxFt * f.BreakDampedMul, change.X - PitchFlight.Point(PitchFamily.Changeup, 1, Rules.Default, 0, 0).X, 6);
        // Full stick over the top of the range stays at the cap.
        Assert.Equal(broke.X, PitchFlight.Point("fastball", 1, Rules.Default, 0, 0, breakX: 3).X, 6);
    }

    [Fact]
    public void EveryShapeCrossesAtItsAimAndHeightIsAPitchProperty()
    {
        // Spec §4.2: a normal / charged pitch crosses mid-zone; a changeup crosses lower by a pitch number.
        var fb = PitchFlight.Point("fastball", 1, rules: Rules.Default);
        Assert.Equal(StrikeZoneGeometry.Reference.CenterY, fb.Y, 6);
        Assert.Equal(0, fb.X, 6);
        var charged = PitchFlight.Point("fastball", 1, charged: true, rules: Rules.Default);
        Assert.Equal(fb.Y, charged.Y, 6);
        var change = PitchFlight.Point(PitchFamily.Changeup, 1, rules: Rules.Default);
        Assert.Equal(StrikeZoneGeometry.Reference.CenterY - Rules.Default.Pitching.Families.Changeup.DropFt, change.Y, 6);
        Assert.True(change.Y > StrikeZoneGeometry.Reference.Bottom, "the changeup dumps inside the zone by default");
        Assert.True(change.Y < StrikeZoneGeometry.Reference.CenterY - StrikeZoneGeometry.Reference.Height / 4);
    }

    [Fact]
    public void NiceReleaseAddsFivePercent()
    {
        var plain = new PitchCommand("fastball", 1, false);
        var nice = plain with { Nice = true };
        Assert.Equal(Rules.Default.Pitching.Release.NiceMul, AtBatResolver.PitchSpeedMph(nice, 7, rules: Rules.Default) / AtBatResolver.PitchSpeedMph(plain, 7, rules: Rules.Default), 8);
        var band = Rules.Default.Pitching.Release.NiceBandSec;
        Assert.True(ChargeFeel.NiceRelease(1, band - 0.01, 0.5, rules: Rules.Default));
        Assert.False(ChargeFeel.NiceRelease(1, band + 0.01, 0.5, rules: Rules.Default));
        Assert.False(ChargeFeel.NiceRelease(0.9, 0, 0.5, rules: Rules.Default));
    }

    [Fact]
    public void ChangeupCommandHangsThenDumps()
    {
        // Was the Changeup modifier on a fastball command (#810 retired it): the family id is the
        // one way to ask for the shape, and it is still the same shape.
        var hangU = Rules.Default.Pitching.Families.Changeup.HangUntil;
        var hang = PitchFlight.Point(PitchFamily.Changeup, hangU, rules: Rules.Default).Y;
        var fb = PitchFlight.Point(PitchFamily.Fastball, hangU, rules: Rules.Default).Y;
        var plate = PitchFlight.Point(PitchFamily.Changeup, 1, rules: Rules.Default).Y;
        Assert.True(hang >= fb, $"changeup hang {hang} vs fb {fb}");
        Assert.True(hang - plate > 1.0, $"changeup dump hang {hang} plate {plate}");
    }

    [Fact]
    public void ChangeupNamesItselfWithoutTheCard()
    {
        // #668 / spec §4.1–§4.3: HUD-off the changeup is slower, hangs, then dumps below the
        // fastball. CHANGE on the card is not the tell. A fade (hangRate near 1, hang below the
        // fastball mid-flight) would pass S-06's plate Y and still read as a slow fastball.
        var r = Rules.Default.Pitching;
        var sh = r.Families.Changeup;
        var fb = new PitchCommand(PitchFamily.Fastball, 0, false);
        var ch = fb with { Type = PitchFamily.Changeup };

        Assert.Equal(0.80, sh.Mph / r.Families.Fastball.Mph, 2);
        var fbAir = PitchFlight.AirSeconds(AtBatResolver.PitchSpeedMph(fb, 5, rules: Rules.Default), rules: Rules.Default);
        var chAir = PitchFlight.AirSeconds(AtBatResolver.PitchSpeedMph(ch, 5, rules: Rules.Default), rules: Rules.Default);
        Assert.True(chAir > fbAir + 0.18, $"changeup {chAir:0.000}s vs fastball {fbAir:0.000}s");

        var fbPlate = PitchFlight.Point(fb, 1, rules: Rules.Default);
        var chPlate = PitchFlight.Point(ch, 1, rules: Rules.Default);
        Assert.Equal(sh.DropFt, fbPlate.Y - chPlate.Y, 6);
        Assert.True(chPlate.Y > StrikeZoneGeometry.Reference.Bottom);

        var hangU = sh.HangUntil;
        var chRel = PitchFlight.Point(ch, 0, rules: Rules.Default).Y;
        var chHang = PitchFlight.Point(ch, hangU, rules: Rules.Default).Y;
        var fbHang = PitchFlight.Point(fb, hangU, rules: Rules.Default).Y;
        Assert.True(chHang >= fbHang, $"hang {chHang:0.00} vs fastball {fbHang:0.00} at u={hangU}");
        var hangDrop = chRel - chHang;
        var dumpDrop = chHang - chPlate.Y;
        Assert.True(dumpDrop > hangDrop * 2,
            $"dump {dumpDrop:0.00} vs hang-drop {hangDrop:0.00} — a fade drops evenly");
        Assert.True(sh.DumpRate > sh.HangRate * 4,
            $"dumpRate {sh.DumpRate} vs hangRate {sh.HangRate}");
        var hangT = hangU * sh.HangRate + (1 - hangU) * sh.DumpRate;
        Assert.True(hangT >= 1, $"dump must reach the aim in flight, hang(1)={hangT}");
        var almost = PitchFlight.Point(ch, 0.99, rules: Rules.Default).Y;
        Assert.True(Math.Abs(almost - chPlate.Y) < 0.15,
            $"dump finishes before the plate, not a snap: u=0.99 Y={almost:0.00} plate={chPlate.Y:0.00}");
    }

    [Fact]
    public void RubberWalkMovesTheCrossingTheSameWorldDistanceAsTheBodyOnce()
    {
        // Spec §4.2, #577: release hand and crossing both move HomeSet.PitcherWalk per unit, for every seat.
        var heart = PitchFlight.Point("fastball", 1, Rules.Default, 0, 0, rubberX: 0);
        var walked = PitchFlight.Point("fastball", 1, Rules.Default, 0, 0, rubberX: 1);
        Assert.Equal(HomeSet.PitcherWalk, walked.X - heart.X, 6);
        Assert.Equal(HomeSet.PitcherWalk, PitchFlight.Release(Rules.Default,1).X - PitchFlight.Release(Rules.Default,0).X, 6);
        var command = new PitchCommand("fastball", 0, false, RubberX: -0.5);
        Assert.Equal(-0.5 * HomeSet.PitcherWalk, PitchFlight.Crossing(command, rules: Rules.Default).X, 6);
    }

    [Fact]
    public void ChangeupIsSlowerThanAMaxFastball()
    {
        var maxFb = AtBatResolver.PitchSpeedMph(new PitchCommand(PitchFamily.Fastball, 1, false), 7, rules: Rules.Default);
        var typed = AtBatResolver.PitchSpeedMph(new PitchCommand(PitchFamily.Changeup, 1, false), 7, rules: Rules.Default);
        Assert.True(typed < maxFb, $"changeup {typed} vs MAX fastball {maxFb}");
        // A full charge is worth less on the changeup than on the fastball: each family carries its
        // own chargeMph (#810), so the gap widens with the charge rather than shifting by a constant.
        var restFb = AtBatResolver.PitchSpeedMph(new PitchCommand(PitchFamily.Fastball, 0, false), 7, rules: Rules.Default);
        var restCh = AtBatResolver.PitchSpeedMph(new PitchCommand(PitchFamily.Changeup, 0, false), 7, rules: Rules.Default);
        Assert.True(maxFb - typed > restFb - restCh, $"charged gap {maxFb - typed} vs rest gap {restFb - restCh}");
    }
}
