using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class BroadcastHudTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void PlayHudMutesDuringSpectacleSmashAndFreeze()
    {
        Assert.False(BroadcastHud.MutePlay(false, 0, 0));
        Assert.True(BroadcastHud.MutePlay(true, 0, 0));
        Assert.True(BroadcastHud.MutePlay(false, 0.55, 0));
        Assert.True(BroadcastHud.MutePlay(false, 0, 0.12));
        Assert.False(BroadcastHud.MutePlay(false, 0, 0));
        foreach (var c in _content.Characters.Values.Where(c => c.Captain))
            Assert.True(BroadcastHud.MutePlay(StarSkills.SpectacleSeconds(c.StarPitch) > 0, 0, 0));
    }

    [Fact]
    public void ScorebugCoversInningScoreCountRunnersMatchupStarsAndNext()
    {
        var match = Match.Exhibition(_content, "vale", "brondo", seed: 7);
        var bug = BroadcastHud.From(match);
        Assert.Equal(1, bug.Inning);
        Assert.True(bug.Top);
        Assert.False(bug.Over);
        Assert.Equal(0, bug.AwayScore);
        Assert.Equal(0, bug.HomeScore);
        Assert.Equal(0, bug.Outs);
        Assert.Equal(0, bug.Balls);
        Assert.Equal(0, bug.Strikes);
        Assert.False(bug.RunnerFirst);
        Assert.False(bug.RunnerSecond);
        Assert.False(bug.RunnerThird);
        Assert.Equal(match.Pitcher.Name, bug.Pitcher);
        Assert.Equal(match.Batter.Name, bug.Batter);
        Assert.False(string.IsNullOrWhiteSpace(bug.Next));
        Assert.NotEqual(bug.Batter, bug.Next);
        Assert.Equal(match.Away.Name, bug.AwayName);
        Assert.Equal(match.Home.Name, bug.HomeName);
        Assert.InRange(bug.OffenseStars, 0, 5);
        Assert.InRange(bug.DefenseStars, 0, 5);
        Assert.False(BroadcastHud.MutePlay(false, 0, 0));
        Assert.Throws<ArgumentNullException>(() => BroadcastHud.From(null!));

    }

    [Fact]
    public void HeadlineTakeStrikeIsStrikeNotTakeStrikeGlued()
    {
        Assert.Equal("STRIKE", BroadcastHud.Headline(PlayKind.TakeStrike));
        Assert.Equal("BALL", BroadcastHud.Headline(PlayKind.TakeBall));
        Assert.Equal("GROUNDOUT", BroadcastHud.Headline(PlayKind.GroundOut));
        Assert.DoesNotContain("TAKESTRIKE", BroadcastHud.Headline(PlayKind.TakeStrike));
    }
}
