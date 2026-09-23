using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public class MatchOptionsTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StarsOffRejectsBothSpecialsAndAllGrantsOnEitherSide(bool bottom)
    {
        var asked = Match.Exhibition(_content, stars: false);
        var ordinary = Match.Exhibition(_content, stars: false);
        if (bottom) { asked.SkipToHomeHalf(); ordinary.SkipToHomeHalf(); }
        asked.GiveDefenseStars(5); asked.GiveOffenseStars(5);
        Assert.Equal(0, asked.HomeStars); Assert.Equal(0, asked.AwayStars);
        Assert.False(asked.CanStarPitch); Assert.False(asked.CanStarSwing);
        var pitch = Scenario.PitchAt(0, StrikeZoneGeometry.CenterY);
        var swing = Scenario.SwingAt(0);
        var a = asked.Play(pitch with { Star = true }, swing with { Star = true });
        var b = ordinary.Play(pitch, swing);
        Assert.False(a.Pitch.Star); Assert.False(a.Swing.Star);
        Assert.Equal(b.AtBat, a.AtBat);
        Assert.Equal(b.Kind, a.Kind);
        Assert.Equal(2, a.Outcome!.Stars.Count);
        Assert.All(a.Outcome.Stars, request => Assert.False(request.Afforded));
        Assert.Equal(0, asked.HomeStars); Assert.Equal(0, asked.AwayStars);
    }

    [Fact]
    public void CpuCannotEarnOrSpendStarsInAStarsOffGame()
    {
        var match = Match.Exhibition(_content, stars: false);
        for (var i = 0; i < 30 && !match.Over; i++)
        {
            var play = match.AutoPlay();
            Assert.False(play.Pitch.Star); Assert.False(play.Swing.Star);
            Assert.Equal(0, match.HomeStars); Assert.Equal(0, match.AwayStars);
        }
        Assert.False(PlayTraceLog.Parse(match.TraceLog().ToJson()).StarsEnabled);
    }

    [Fact]
    public void FactoriesCarryOptionsWithoutChangingTheDefaults()
    {
        var normal = Match.Exhibition(_content);
        Assert.True(normal.StarsEnabled); Assert.True(normal.Mercy);
        Assert.Equal(_content.Rules.Stars.StartingReserve, normal.HomeStars);
        Assert.Null(normal.TraceLog().StarsEnabled);
        var fromCaptains = Match.Exhibition(_content, innings: 6, mercy: false, stars: false);
        var fromTeams = Match.Exhibition(_content, normal.Home, normal.Away, innings: 9, mercy: false, stars: false);
        foreach (var match in new[] { fromCaptains, fromTeams })
        {
            Assert.False(match.Mercy); Assert.False(match.StarsEnabled);
            Assert.False(match.MercyEnds(100));
            Assert.Equal(0, match.HomeStars); Assert.Equal(0, match.AwayStars);
        }
    }
}
