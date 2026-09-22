using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

[Trait("Rows", "compact")]
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
        var fam = Rules.Default.Pitching.Families;
        var meat = PitchFlight.AirSeconds(fam.Fastball.Mph);
        var gas = PitchFlight.AirSeconds(fam.Fastball.Mph + fam.Fastball.ChargeMph);
        var change = PitchFlight.AirSeconds(fam.Changeup.Mph);
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
        // The C80 copy's mound is at 8/9 of the distance: halfway is 25.6 ft, inside the same band at 8/9.
        var (lo, hi) = TestRoot.Pick((26.0, 34.0), (23.0, 30.0));
        Assert.InRange(mid.Z, lo, hi);
        Assert.InRange(plate.Z, -0.05, 0.05);
        Assert.True(Math.Abs(mid.X - rel.X) > 0.3, "hand offset fades toward the plate");
    }

    [Fact]
    public void ChangeupHangsThenDumps()
    {
        var hangU = Rules.Default.Pitching.Families.Changeup.HangUntil;
        var hang = PitchFlight.Point(PitchFamily.Changeup, hangU).Y;
        var fb = PitchFlight.Point("fastball", hangU).Y;
        var plate = PitchFlight.Point(PitchFamily.Changeup, 1).Y;
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

        var unknown = Assert.Throws<ArgumentException>(() => PitchFlight.Point("curve", 1));
        Assert.Contains("curve", unknown.Message, StringComparison.Ordinal);
        Assert.Contains("not a pitch family", unknown.Message, StringComparison.Ordinal);

        // "curveball" is different: it is in the library, and a table with no row for it must say so
        // by name rather than quietly flying it as a fastball. #860 (Jack, September 22, 2026:
        // "trial was good."): the shipped data authors all three now, so the unauthored case is the
        // code-default table, whose three optional rows are null — the stop is still reachable by data.
        var bare = RulesTable.Defaults;
        foreach (var unauthored in new[] { PitchFamily.Curveball, PitchFamily.Slider, PitchFamily.Sinker })
        {
            var fell = Assert.Throws<InvalidOperationException>(() => PitchFlight.Point(unauthored, 1, rules: bare));
            Assert.Contains(unauthored, fell.Message, StringComparison.Ordinal);
            Assert.Contains("no authored row", fell.Message, StringComparison.Ordinal);
            Assert.Throws<InvalidOperationException>(() =>
                AtBatResolver.PitchSpeedMph(new PitchCommand(unauthored, 0, false), 5, bare));

            // … and on the shipped root the same family flies (#860).
            Assert.True(Rules.Default.Pitching.Families.IsAuthored(unauthored));
            Assert.True(AtBatResolver.PitchSpeedMph(new PitchCommand(unauthored, 0, false), 5) > 0);
        }

        // Training.CorePitches reads the code defaults, which author two rows; the shipped data
        // authors the whole library since #860.
        Assert.Equal(["fastball", "changeup"], Training.CorePitches);
        Assert.Equal(bare.Pitching.Families.Authored, Training.CorePitches);
        Assert.Equal(PitchFamily.All, Rules.Default.Pitching.Families.Authored);
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
        var change = PitchFlight.Point(PitchFamily.Changeup, 1, 0, 0, breakX: 1);
        Assert.Equal(f.BreakMaxFt * f.BreakDampedMul, charged.X - heart.X, 6);
        Assert.Equal(f.BreakMaxFt * f.BreakDampedMul, change.X - PitchFlight.Point(PitchFamily.Changeup, 1, 0, 0).X, 6);
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
        var change = PitchFlight.Point(PitchFamily.Changeup, 1);
        Assert.Equal(StrikeZoneGeometry.CenterY - Rules.Default.Pitching.Families.Changeup.DropFt, change.Y, 6);
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
    public void ChangeupCommandHangsThenDumps()
    {
        // Was the Changeup modifier on a fastball command (#810 retired it): the family id is the
        // one way to ask for the shape, and it is still the same shape.
        var hangU = Rules.Default.Pitching.Families.Changeup.HangUntil;
        var hang = PitchFlight.Point(PitchFamily.Changeup, hangU).Y;
        var fb = PitchFlight.Point(PitchFamily.Fastball, hangU).Y;
        var plate = PitchFlight.Point(PitchFamily.Changeup, 1).Y;
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
        var fbAir = PitchFlight.AirSeconds(AtBatResolver.PitchSpeedMph(fb, 5));
        var chAir = PitchFlight.AirSeconds(AtBatResolver.PitchSpeedMph(ch, 5));
        Assert.True(chAir > fbAir + 0.18, $"changeup {chAir:0.000}s vs fastball {fbAir:0.000}s");

        var fbPlate = PitchFlight.Point(fb, 1);
        var chPlate = PitchFlight.Point(ch, 1);
        Assert.Equal(sh.DropFt, fbPlate.Y - chPlate.Y, 6);
        Assert.True(chPlate.Y > StrikeZoneGeometry.Bottom);

        var hangU = sh.HangUntil;
        var chRel = PitchFlight.Point(ch, 0).Y;
        var chHang = PitchFlight.Point(ch, hangU).Y;
        var fbHang = PitchFlight.Point(fb, hangU).Y;
        Assert.True(chHang >= fbHang, $"hang {chHang:0.00} vs fastball {fbHang:0.00} at u={hangU}");
        var hangDrop = chRel - chHang;
        var dumpDrop = chHang - chPlate.Y;
        Assert.True(dumpDrop > hangDrop * 2,
            $"dump {dumpDrop:0.00} vs hang-drop {hangDrop:0.00} — a fade drops evenly");
        Assert.True(sh.DumpRate > sh.HangRate * 4,
            $"dumpRate {sh.DumpRate} vs hangRate {sh.HangRate}");
        var hangT = hangU * sh.HangRate + (1 - hangU) * sh.DumpRate;
        Assert.True(hangT >= 1, $"dump must reach the aim in flight, hang(1)={hangT}");
        var almost = PitchFlight.Point(ch, 0.99).Y;
        Assert.True(Math.Abs(almost - chPlate.Y) < 0.15,
            $"dump finishes before the plate, not a snap: u=0.99 Y={almost:0.00} plate={chPlate.Y:0.00}");
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
    public void ChangeupIsSlowerThanAMaxFastball()
    {
        var maxFb = AtBatResolver.PitchSpeedMph(new PitchCommand(PitchFamily.Fastball, 1, false), 7);
        var typed = AtBatResolver.PitchSpeedMph(new PitchCommand(PitchFamily.Changeup, 1, false), 7);
        Assert.True(typed < maxFb, $"changeup {typed} vs MAX fastball {maxFb}");
        // A full charge is worth less on the changeup than on the fastball: each family carries its
        // own chargeMph (#810), so the gap widens with the charge rather than shifting by a constant.
        var restFb = AtBatResolver.PitchSpeedMph(new PitchCommand(PitchFamily.Fastball, 0, false), 7);
        var restCh = AtBatResolver.PitchSpeedMph(new PitchCommand(PitchFamily.Changeup, 0, false), 7);
        Assert.True(maxFb - typed > restFb - restCh, $"charged gap {maxFb - typed} vs rest gap {restFb - restCh}");
    }
}
