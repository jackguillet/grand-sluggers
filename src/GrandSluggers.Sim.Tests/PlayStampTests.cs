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
        Assert.Equal("BALL", PlayStamp.Label(PlayKind.TakeBall, 0, 0));
        Assert.Equal("STRIKE", PlayStamp.Label(PlayKind.TakeStrike, 0, 0));
        Assert.Equal("STRIKE", PlayStamp.Label(PlayKind.SwingMiss, 0, 0));
        Assert.Equal("FOUL", PlayStamp.Label(PlayKind.Foul, 0, 0));
        Assert.Equal("WALK", PlayStamp.Label(PlayKind.Walk, 0, 0));
        Assert.True(PlayStamp.Shows(PlayKind.TakeBall));
        Assert.True(PlayStamp.Shows(PlayKind.TakeStrike));
        Assert.True(PlayStamp.Shows(PlayKind.SwingMiss));
        Assert.True(PlayStamp.Shows(PlayKind.Foul));
        Assert.True(PlayStamp.Shows(PlayKind.Walk));
        Assert.True(PlayStamp.Shows(PlayKind.FlyOut));
        Assert.True(PlayStamp.Shows(PlayKind.HomeRun));
        Assert.True(PlayStamp.IsCount(PlayKind.Walk));
        Assert.False(PlayStamp.IsCount(PlayKind.Single));
        Assert.False(PlayStamp.IsCount(PlayKind.Strikeout));
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
        Assert.True(PlayStamp.HoldSeconds(PlayKind.TakeBall, feel) <
            PlayStamp.HoldSeconds(PlayKind.FlyOut, feel));
        Assert.True(PlayStamp.HoldSeconds(PlayKind.Walk, feel) <
            PlayStamp.HoldSeconds(PlayKind.Single, feel));
        Assert.InRange(PlayStamp.HoldSeconds(PlayKind.Foul, feel), 0.4, 0.9);
        Assert.True(PlayStamp.Scale(PlayKind.Strikeout) > PlayStamp.Scale(PlayKind.TakeStrike));
        Assert.True(PlayStamp.PopSeconds(PlayKind.TakeBall) < PlayStamp.PopSeconds(PlayKind.FlyOut));
        Assert.InRange(PlayStamp.Scale(PlayKind.Walk), 0.6, 0.85);
        Assert.Equal("SAFE", PlayStamp.Safe);
        Assert.True(PlayStamp.SafeScale < PlayStamp.Scale(PlayKind.Single));
        Assert.True(PlayStamp.SafeHoldSeconds(feel) < PlayStamp.HoldSeconds(PlayKind.Single, feel));
    }

    [Fact]
    public void HitsAndOutsStampOnTheFieldNotTheNextPitch()
    {
        Assert.True(PlayStamp.HoldsLiveCamera(PlayKind.Single));
        Assert.True(PlayStamp.HoldsLiveCamera(PlayKind.Double));
        Assert.True(PlayStamp.HoldsLiveCamera(PlayKind.HomeRun));
        Assert.True(PlayStamp.HoldsLiveCamera(PlayKind.GroundOut));
        Assert.True(PlayStamp.HoldsLiveCamera(PlayKind.FlyOut));
        Assert.True(PlayStamp.HoldsLiveCamera(PlayKind.Strikeout));
        Assert.False(PlayStamp.HoldsLiveCamera(PlayKind.TakeBall));
        Assert.False(PlayStamp.HoldsLiveCamera(PlayKind.TakeStrike));
        Assert.False(PlayStamp.HoldsLiveCamera(PlayKind.Foul));
        Assert.False(PlayStamp.HoldsLiveCamera(PlayKind.Walk));
    }
}
