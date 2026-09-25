using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public class PitchJudgmentTests
{
    readonly ContentCatalog content = Shipped.Content;
    static readonly SwingCommand Take = new(false, 0, 0, false);

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void TakenLiveBreakOutsideCannotBeCalledStrikeThree(double curve)
    {
        var match = Match.Slice(content, innings: 3, seed: 1);
        var heart = new PitchCommand("fastball", 0, false);
        Assert.Equal(PlayKind.TakeStrike, match.Play(heart, Take).Kind);
        Assert.Equal(PlayKind.TakeStrike, match.Play(heart, Take).Kind);
        // On the edge, then the stick carries it out (the cap is half a zone, spec §4.2).
        var edge = Scenario.PitchAt(curve * (StrikeZoneGeometry.HalfWidth - 0.1), StrikeZoneGeometry.Reference.CenterY);
        var delivered = match.PreparePitch(edge with { BreakX = curve });
        var crossing = PitchFlight.Point(delivered, 1, rules: Rules.Default);
        Assert.True(Math.Abs(crossing.X) > StrikeZoneGeometry.HalfWidth);
        var result = match.Play(delivered, Take);
        Assert.False(result.AtBat.InZone);
        Assert.Equal(PlayKind.TakeBall, result.Kind);
        Assert.Equal(2, match.Strikes);
        Assert.Equal(0, match.Outs);
    }

    [Fact]
    public void MissedSwingOutsideIsStillStrikeThree()
    {
        var match = Match.Slice(content, innings: 3, seed: 1);
        var heart = new PitchCommand("fastball", 0, false);
        match.Play(heart, Take); match.Play(heart, Take);
        var edge = Scenario.PitchAt(StrikeZoneGeometry.HalfWidth - 0.1, StrikeZoneGeometry.Reference.CenterY);
        var result = match.Play(edge with { BreakX = 1 }, new SwingCommand(true, 0, 99, false));
        Assert.Equal(PlayKind.Strikeout, result.Kind);
        Assert.False(result.AtBat.InZone);
        Assert.Equal(1, match.Outs);
    }

    [Fact]
    public void PitchShapeBreakRubberAndStarAreJudgedAtTheRenderedCrossing()
    {
        foreach (var type in new[] { "fastball", "changeup" })
        foreach (var star in new[] { "", "heatball", "prismball", "charmball", "phonyball", "caskball" })
        foreach (var offset in new[] { -1.0, 0, 1.0 })
        foreach (var charge in new[] { 0.0, 1.0 })
        {
            var pitch = new PitchCommand(type, charge, star != "", 0.2, -0.2, offset, offset * 0.3);
            var world = PitchFlight.Point(pitch, 1, Rules.Default, star);
            var aim = PitchFlight.ContactAim(pitch, Rules.Default, star);
            Assert.Equal(world.X, aim.X * PitchFlight.PlateScaleX, 10);
            Assert.Equal(world.Y, StrikeZoneGeometry.Reference.CenterY + aim.Y * PitchFlight.PlateScaleY, 10);
            var inside = Math.Abs(world.X) <= 0.92 && world.Y >= 1.45 && world.Y <= 3.65;
            Assert.Equal(inside, AtBatResolver.PitchInZone(pitch, 1, Rules.Default, star));
            Assert.Equal(inside, AtBatResolver.PitchInZone(pitch, 10, Rules.Default, star));
        }
    }

    [Fact]
    public void VisibleZoneEdgesDoNotShrinkWithChargeOrStar()
    {
        foreach (var x in new[] { -0.92, 0.92 })
        foreach (var y in new[] { 1.45, 3.65 })
        {
            Assert.True(StrikeZoneGeometry.Reference.Contains(x, y));
            Assert.False(StrikeZoneGeometry.Reference.Contains(x * 1.001, y));
        }
        Assert.False(StrikeZoneGeometry.Reference.Contains(0, 1.449));
        Assert.False(StrikeZoneGeometry.Reference.Contains(0, 3.651));
        Assert.False(StrikeZoneGeometry.Reference.Contains(double.NaN, 2));
    }

    [Theory]
    [InlineData(Hand.R, -1)]
    [InlineData(Hand.L, 1)]
    public void HitByPitchBodyUsesTheHandedAuthoredBoxAndWorldWalk(Hand bats, double side)
    {
        var y = StrikeZoneGeometry.Reference.CenterY;
        var body = AtBatResolver.BatterBodyX(0, bats);
        Assert.Equal(side * HomeSet.BoxX, body, 10);
        Assert.True(AtBatResolver.HitsBatter(0, body, y, Rules.Default, StrikeZoneGeometry.Reference, bats));
        Assert.False(AtBatResolver.HitsBatter(0, -body, y, Rules.Default, StrikeZoneGeometry.Reference, bats));

        const double offset = 0.5;
        var walked = AtBatResolver.BatterBodyX(offset, bats);
        Assert.Equal(body + offset * HomeSet.BatterWalk, walked, 10);
        Assert.True(AtBatResolver.HitsBatter(offset, walked, y, Rules.Default, StrikeZoneGeometry.Reference, bats));
        // Body and cursor move the same world distance per box unit (spec §4.6).
        Assert.Equal(walked - body, SweetSpot.WorldCenter(offset, StrikeZoneGeometry.Reference).X - SweetSpot.WorldCenter(0, StrikeZoneGeometry.Reference).X, 10);
    }

    [Fact]
    public void NearZoneOutsideTakeStaysBallAndTrueBodyTakeIsHitByPitch()
    {
        var nearWorldX = StrikeZoneGeometry.HalfWidth + 0.05;
        var nearAimX = nearWorldX / PitchFlight.PlateScaleX;
        var near = PitchFlight.AimForCrossing(
            new PitchCommand("fastball", 0, false), nearAimX, 0, rules: Rules.Default);
        Assert.False(StrikeZoneGeometry.Contains(near, rules: Rules.Default));
        Assert.False(AtBatResolver.HitsBatter(0, nearWorldX, StrikeZoneGeometry.Reference.CenterY, Rules.Default, StrikeZoneGeometry.Reference, Hand.R));
        var ballMatch = Match.Slice(content, innings: 3, seed: 1);
        var ball = ballMatch.Play(near, Take);
        Assert.Equal(PlayKind.TakeBall, ball.Kind);

        var bodyMatch = Match.Slice(content, innings: 3, seed: 1);
        var bodyAimX = AtBatResolver.BatterBodyX(0, bodyMatch.Batter.Bats) / PitchFlight.PlateScaleX;
        var bodyPitch = PitchFlight.AimForCrossing(
            new PitchCommand("fastball", 0, false), bodyAimX, 0, rules: Rules.Default);
        var plunk = bodyMatch.Play(bodyPitch, Take);
        Assert.Equal(PlayKind.HitByPitch, plunk.Kind);
        Assert.False(plunk.AtBat.InZone);
    }

    [Fact]
    public void PreparedDeliveryKeepsItsRubberPositionThroughJudgment()
    {
        var match = Match.Slice(content, innings: 3, seed: 1);
        match.WalkPitcher(0.6);
        var ready = match.PreparePitch(new PitchCommand("fastball", 0, false));
        Assert.Equal(0.6, ready.RubberX);
        Assert.Same(ready, match.PreparePitch(ready));
        match.WalkPitcher(-1.2);
        var result = match.Play(ready, Take);
        Assert.Equal(ready, result.Pitch);
        Assert.Equal(StrikeZoneGeometry.Contains(ready, rules: Rules.Default), result.AtBat.InZone);
    }
    [Fact]
    public void TiredDeliveryIsPreparedOnceAndCrossesWhereItWasAimed()
    {
        var match = Match.Slice(content, innings: 99, seed: 17);
        // Repeated taken outside pitches spend stamina without retiring the side.
        var outside = new PitchCommand("fastball", 0, false, AimX: 1.5);
        for (var i = 0; i < 150 && !match.PitcherTired; i++) match.Play(outside, Take);
        Assert.True(match.PitcherTired);
        var raw = new PitchCommand("fastball", 0, false);
        var ready = match.PreparePitch(raw);
        Assert.True(ready.DeliveryPrepared);
        // No random miss (PH-08-R1): the aim is the aim.
        Assert.Equal((raw.AimX, raw.AimY), (ready.AimX, ready.AimY));
        Assert.Equal(PitchFlight.Point(raw with { DeliveryPrepared = true, RubberX = ready.RubberX, Throws = ready.Throws, Zone = match.BatterZone }, 1, rules: Rules.Default),
            PitchFlight.Point(ready, 1, rules: Rules.Default));
        var crossing = PitchFlight.Point(ready, 1, rules: Rules.Default);
        for (var i = 0; i < 5; i++) Assert.Same(ready, match.PreparePitch(ready));
        var result = match.Play(ready, Take);
        Assert.Equal(ready, result.Pitch);
        Assert.Equal(crossing, PitchFlight.Point(result.Pitch, 1, rules: Rules.Default));
        Assert.Equal(StrikeZoneGeometry.Reference.Contains(crossing.X, crossing.Y), result.AtBat.InZone);
    }

    [Fact]
    public void IntendedCrossingSurvivesEveryDeliveryShape()
    {
        foreach (var type in new[] { "fastball", "changeup" })
        foreach (var star in new[] { "", "heatball", "prismball", "charmball", "phonyball", "caskball" })
        foreach (var offset in new[] { -1.0, 0, 1.0 })
        {
            var raw = new PitchCommand(type, 1, star != "", BreakX: offset, RubberX: offset);
            var aimed = PitchFlight.AimForCrossing(raw, 0.2, -0.1, Rules.Default, star);
            var contact = PitchFlight.ContactAim(aimed, Rules.Default, star);
            Assert.Equal(0.2, contact.X, 10);
            Assert.Equal(-0.1, contact.Y, 10);
            Assert.Equal(raw.BreakX, aimed.BreakX);
            Assert.Equal(raw.RubberX, aimed.RubberX);
            Assert.Equal(raw.Type, aimed.Type);
            Assert.False(aimed.DeliveryPrepared);
        }
    }

    [Fact]
    public void CpuBreakingBallsAndChangeupsReachTheZoneLikeFastballs()
    {
        // The table names a crossing and compensates the shape, the break, and the rubber into the
        // aim (spec §4.8): at an even count every verb lands in the frame about as often.
        var match = Match.Slice(content, seed: 535);
        var seen = new Dictionary<string, int>();
        var strikes = new Dictionary<string, int>();
        for (var i = 0; i < 5000; i++)
        {
            var pitch = match.CpuPitcher.Pitch();
            var type = pitch.Type == PitchFamily.Changeup ? "changeup" : pitch.BreakX != 0 ? "break" : ChargeFeel.IsCharge(pitch.Charge01) ? "charge" : "fastball";
            seen[type] = seen.GetValueOrDefault(type) + 1;
            if (AtBatResolver.PitchInZone(pitch, match.Pitcher.Stats.Pitch, Rules.Default, match.Pitcher.StarPitch))
                strikes[type] = strikes.GetValueOrDefault(type) + 1;
        }
        var rates = new Dictionary<string, double>();
        foreach (var type in new[] { "fastball", "charge", "changeup", "break" })
        {
            Assert.True(seen[type] > 100, $"{type} seen {seen.GetValueOrDefault(type)}");
            rates[type] = (double)strikes.GetValueOrDefault(type) / seen[type];
            Assert.InRange(rates[type], 0.4, 0.95);
        }
        Assert.True(Math.Abs(rates["break"] - rates["fastball"]) < 0.15, $"break {rates["break"]:0.00} vs fastball {rates["fastball"]:0.00}");
        Assert.True(Math.Abs(rates["changeup"] - rates["fastball"]) < 0.15, $"changeup {rates["changeup"]:0.00} vs fastball {rates["fastball"]:0.00}");
    }

    [Fact]
    public void TheAimTellIsTheCrossingTheUmpireJudges()
    {
        // #577: one function for the tell, the ball, and the umpire. Walking the rubber moves the
        // tell with the body; the stick moves it during flight; a charged pitch barely bends.
        var r = content.Rules;
        var still = new PitchCommand("fastball", 0, false);
        var walked = still with { RubberX = 0.5 };
        Assert.Equal(HomeSet.PitcherWalk * 0.5, SetTells.Locator(walked, StrikeZoneGeometry.Reference, Rules.Default).X - SetTells.Locator(still, StrikeZoneGeometry.Reference, Rules.Default).X, 6);
        var bent = walked with { BreakX = -1 };
        Assert.Equal(-r.Pitching.Flight.BreakMaxFt, SetTells.Locator(bent, StrikeZoneGeometry.Reference, Rules.Default).X - SetTells.Locator(walked, StrikeZoneGeometry.Reference, Rules.Default).X, 6);
        var charged = bent with { Charge01 = 1 };
        Assert.Equal(-r.Pitching.Flight.BreakMaxFt * r.Pitching.Flight.BreakDampedMul,
            SetTells.Locator(charged, StrikeZoneGeometry.Reference, Rules.Default).X - SetTells.Locator(walked, StrikeZoneGeometry.Reference, Rules.Default).X, 6);
        foreach (var pitch in new[] { still, walked, bent, charged })
        {
            var (x, y) = SetTells.Locator(pitch, StrikeZoneGeometry.Reference, Rules.Default);
            var p = PitchFlight.Point(pitch, 1, rules: Rules.Default);
            Assert.Equal((p.X, p.Y), (x, y));
            Assert.Equal(StrikeZoneGeometry.Reference.Contains(x, y), SetTells.InZone(pitch, StrikeZoneGeometry.Reference, Rules.Default));
            Assert.Equal(StrikeZoneGeometry.Reference.Contains(x, y), AtBatResolver.PitchInZone(pitch, 5, rules: Rules.Default));
        }
        Assert.True(SetTells.AimTellOn(true, true));
        Assert.False(SetTells.AimTellOn(false, true));
        Assert.False(SetTells.AimTellOn(true, false));
    }

}
