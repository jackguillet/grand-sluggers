using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class PlayStampTests
{
    [Fact]
    public void StampNamesTheDeadPlayNotTheSimEnum()
    {
        Assert.Equal("OUT", PlayStamp.Label(PlayKind.FlyOut, 1, 0));
        Assert.Equal("OUT", PlayStamp.Label(PlayKind.GroundOut, 1, 0));
        Assert.Equal("OUT", PlayStamp.Label(PlayKind.Strikeout, 1, 0));
        Assert.Equal("DOUBLE PLAY", PlayStamp.Label(PlayKind.GroundOut, 2, 0));
        Assert.Equal("TRIPLE PLAY", PlayStamp.Label(PlayKind.GroundOut, 3, 0));
        Assert.Equal("SINGLE", PlayStamp.Label(PlayKind.Single, 0, 1));
        Assert.Equal("DOUBLE", PlayStamp.Label(PlayKind.Double, 0, 1));
        Assert.Equal("TRIPLE", PlayStamp.Label(PlayKind.Triple, 0, 1));
        Assert.Equal("HOME RUN", PlayStamp.Label(PlayKind.HomeRun, 0, 1));
        Assert.Equal("GRAND SLAM", PlayStamp.Label(PlayKind.HomeRun, 0, 4));
        Assert.False(PlayStamp.Shows(PlayKind.TakeBall));
        Assert.False(PlayStamp.Shows(PlayKind.Foul));
        Assert.False(PlayStamp.Shows(PlayKind.Walk));
        Assert.True(PlayStamp.Shows(PlayKind.FlyOut));
        Assert.True(PlayStamp.Shows(PlayKind.HomeRun));
    }

    [Fact]
    public void OutsRecordedSurvivesTheInningFlip()
    {
        var content = ContentCatalog.Load();
        var match = Match.Exhibition(content, "rio", "ashlord", 3, seed: 1);
        Assert.Equal(2, PlayStamp.OutsRecorded(1, match.Inning - 1, match.Top, match));
        Assert.Equal(1, PlayStamp.OutsRecorded(2, match.Inning, !match.Top, match));
        Assert.Equal(0, PlayStamp.OutsRecorded(0, match.Inning, match.Top, match));
    }

    [Fact]
    public void HoldIsABeatNotASkip()
    {
        var feel = ContentCatalog.Load().Feel;
        Assert.InRange(PlayStamp.HoldSeconds(PlayKind.FlyOut, feel), 1.0, 2.0);
        Assert.True(PlayStamp.HoldSeconds(PlayKind.HomeRun, feel) >
            PlayStamp.HoldSeconds(PlayKind.Single, feel));
    }
}
