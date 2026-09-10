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

    [Fact]
    public void HitByPitchUsesTheBattersHand()
    {
        Assert.True(AtBatResolver.HitsBatter(0, -0.75, 0, Hand.R));
        Assert.False(AtBatResolver.HitsBatter(0, -0.75, 0, Hand.L));
        Assert.True(AtBatResolver.HitsBatter(0, 0.75, 0, Hand.L));
        Assert.False(AtBatResolver.HitsBatter(0, 0.75, 0, Hand.R));
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
}
