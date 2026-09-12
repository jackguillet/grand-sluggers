using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public class PitchJudgmentTests
{
    readonly ContentCatalog content = ContentCatalog.Load();
    static readonly SwingCommand Take = new(false, 0, 0, false);

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void TakenLiveCurveOutsideCannotBeCalledStrikeThree(double curve)
    {
        var match = Match.Slice(content, innings: 3, seed: 1);
        var heart = new PitchCommand("fastball", 0, 0, false);
        Assert.Equal(PlayKind.TakeStrike, match.Play(heart, Take).Kind);
        Assert.Equal(PlayKind.TakeStrike, match.Play(heart, Take).Kind);
        var delivered = match.PreparePitch(heart with { BreakX = curve });
        var crossing = PitchFlight.Point(delivered, 1);
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
        var heart = new PitchCommand("fastball", 0, 0, false);
        match.Play(heart, Take); match.Play(heart, Take);
        var result = match.Play(heart with { BreakX = 1 }, new SwingCommand(true, 0, 99, false));
        Assert.Equal(PlayKind.Strikeout, result.Kind);
        Assert.False(result.AtBat.InZone);
        Assert.Equal(1, match.Outs);
    }

    [Fact]
    public void PitchTypeCurveRubberAndStarAreJudgedAtTheRenderedCrossing()
    {
        foreach (var type in new[] { "fastball", "changeup", "curve", "slider" })
        foreach (var star in new[] { "", "heatball", "prismball", "charmball", "phonyball", "caskball" })
        foreach (var offset in new[] { -1.0, 0, 1.0 })
        foreach (var charge in new[] { 0.0, 1.0 })
        {
            var pitch = new PitchCommand(type, charge, 0, star != "", 0.2, -0.2, offset, false, offset);
            var world = PitchFlight.Point(pitch, 1, star);
            var aim = PitchFlight.ContactAim(pitch, star);
            Assert.Equal(world.X, aim.X * PitchFlight.PlateScaleX, 10);
            Assert.Equal(world.Y, PitchFlight.PlateY + aim.Y * PitchFlight.PlateScaleY, 10);
            var inside = Math.Abs(world.X) <= 0.92 && world.Y >= 1.45 && world.Y <= 3.65;
            Assert.Equal(inside, AtBatResolver.PitchInZone(pitch, 1, star));
            Assert.Equal(inside, AtBatResolver.PitchInZone(pitch, 10, star));
        }
    }

    [Fact]
    public void VisibleZoneEdgesDoNotShrinkWithChargeOrStar()
    {
        foreach (var x in new[] { -0.92, 0.92 })
        foreach (var y in new[] { 1.45, 3.65 })
        {
            Assert.True(StrikeZoneGeometry.Contains(x, y));
            Assert.False(StrikeZoneGeometry.Contains(x * 1.001, y));
        }
        Assert.False(StrikeZoneGeometry.Contains(0, 1.449));
        Assert.False(StrikeZoneGeometry.Contains(0, 3.651));
        Assert.False(StrikeZoneGeometry.Contains(double.NaN, 2));
    }

    [Theory]
    [InlineData(Hand.R, -1)]
    [InlineData(Hand.L, 1)]
    public void HitByPitchBodyUsesTheHandedAuthoredBoxAndWorldWalk(Hand bats, double side)
    {
        var body = AtBatResolver.BatterBodyPlateX(0, bats);
        Assert.Equal(side * HomeSet.BoxX / PitchFlight.PlateScaleX, body, 10);
        Assert.True(AtBatResolver.HitsBatter(0, body, 0, bats));
        Assert.False(AtBatResolver.HitsBatter(0, -body, 0, bats));

        const double offset = 0.5;
        var walked = AtBatResolver.BatterBodyPlateX(offset, bats);
        Assert.Equal(body + offset * HomeSet.BatterWalk / PitchFlight.PlateScaleX, walked, 10);
        Assert.True(AtBatResolver.HitsBatter(offset, walked, 0, bats));
    }

    [Fact]
    public void NearZoneOutsideTakeStaysBallAndTrueBodyTakeIsHitByPitch()
    {
        var nearWorldX = StrikeZoneGeometry.HalfWidth + 0.05;
        var nearAimX = nearWorldX / PitchFlight.PlateScaleX;
        var near = PitchFlight.AimForCrossing(
            new PitchCommand("fastball", 0, 0, false), nearAimX, 0);
        Assert.False(StrikeZoneGeometry.Contains(near));
        Assert.False(AtBatResolver.HitsBatter(0, nearAimX, 0, Hand.R));
        var ballMatch = Match.Slice(content, innings: 3, seed: 1);
        var ball = ballMatch.Play(near, Take);
        Assert.Equal(PlayKind.TakeBall, ball.Kind);

        var bodyMatch = Match.Slice(content, innings: 3, seed: 1);
        var bodyAimX = AtBatResolver.BatterBodyPlateX(0, bodyMatch.Batter.Bats);
        var bodyPitch = PitchFlight.AimForCrossing(
            new PitchCommand("fastball", 0, 0, false), bodyAimX, 0);
        var plunk = bodyMatch.Play(bodyPitch, Take);
        Assert.Equal(PlayKind.HitByPitch, plunk.Kind);
        Assert.False(plunk.AtBat.InZone);
    }

    [Fact]
    public void PreparedDeliveryKeepsItsRubberPositionThroughJudgment()
    {
        var match = Match.Slice(content, innings: 3, seed: 1);
        match.WalkPitcher(0.6);
        var ready = match.PreparePitch(new PitchCommand("fastball", 0, 0, false));
        Assert.Equal(0.6, ready.RubberX);
        Assert.Same(ready, match.PreparePitch(ready));
        match.WalkPitcher(-1.2);
        var result = match.Play(ready, Take);
        Assert.Equal(ready, result.Pitch);
        Assert.Equal(StrikeZoneGeometry.Contains(ready), result.AtBat.InZone);
    }
    [Fact]
    public void FatigueDriftIsSampledOnceBeforeVisibleFlight()
    {
        var match = Match.Slice(content, innings: 99, seed: 17);
        // Repeated taken outside pitches spend stamina without retiring the side.
        var outside = new PitchCommand("fastball", 0, 0, false, AimX: 1.5);
        for (var i = 0; i < 150 && !match.PitcherTired; i++) match.Play(outside, Take);
        Assert.True(match.PitcherTired);
        var raw = new PitchCommand("fastball", 0, 0, false);
        var ready = match.PreparePitch(raw);
        Assert.True(ready.DeliveryPrepared);
        Assert.True(ready.AimX != raw.AimX || ready.AimY != raw.AimY);
        var crossing = PitchFlight.Point(ready, 1);
        for (var i = 0; i < 5; i++) Assert.Same(ready, match.PreparePitch(ready));
        var result = match.Play(ready, Take);
        Assert.Equal(ready, result.Pitch);
        Assert.Equal(crossing, PitchFlight.Point(result.Pitch, 1));
        Assert.Equal(StrikeZoneGeometry.Contains(crossing.X, crossing.Y), result.AtBat.InZone);
    }

    [Fact]
    public void IntendedCrossingSurvivesEveryDeliveryShape()
    {
        foreach (var type in new[] { "fastball", "changeup", "curve", "slider" })
        foreach (var star in new[] { "", "heatball", "prismball", "charmball", "phonyball", "caskball" })
        foreach (var offset in new[] { -1.0, 0, 1.0 })
        {
            var raw = new PitchCommand(type, 1, 0, star != "", BreakX: offset, RubberX: offset);
            var aimed = PitchFlight.AimForCrossing(raw, 0.2, -0.1, star);
            var contact = PitchFlight.ContactAim(aimed, star);
            Assert.Equal(0.2, contact.X, 10);
            Assert.Equal(-0.1, contact.Y, 10);
            Assert.Equal(raw.BreakX, aimed.BreakX);
            Assert.Equal(raw.RubberX, aimed.RubberX);
            Assert.Equal(raw.Type, aimed.Type);
            Assert.False(aimed.DeliveryPrepared);
        }
    }

    [Fact]
    public void CpuBreakingBallsCanReachTheIntendedZone()
    {
        var match = Match.Slice(content, seed: 535);
        var seen = new Dictionary<string, int>();
        var strikes = new Dictionary<string, int>();
        for (var i = 0; i < 5000; i++)
        {
            var pitch = match.CpuPitch();
            var type = pitch.Changeup ? "changeup" : pitch.Type;
            seen[type] = seen.GetValueOrDefault(type) + 1;
            if (AtBatResolver.PitchInZone(pitch, match.Pitcher.Stats.Pitch, match.Pitcher.StarPitch))
                strikes[type] = strikes.GetValueOrDefault(type) + 1;
        }
        foreach (var type in new[] { "fastball", "changeup", "curve", "slider" })
        {
            Assert.True(seen[type] > 100);
            Assert.InRange((double)strikes.GetValueOrDefault(type) / seen[type], 0.65, 1.0);
        }
    }

}
