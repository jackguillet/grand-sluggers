using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>The hazards' tells (FD-15, FR-15; F8-b): each typed hazard event has its word and its anchor; nothing else does.</summary>
public sealed class HazardTellTests
{
    [Fact]
    public void F8B_EveryHazardEventHasItsTellAndNoOtherEventDoes()
    {
        Assert.Equal(new LiveStamp(PlayStamp.Slowed, StampAnchor.Glove), PlayStamp.HazardTell(LiveEvent.BodySlowed));
        Assert.Equal("WARP!", PlayStamp.HazardTell(LiveEvent.BallRedirected, HazardType.WarpPipe)!.Word);
        Assert.Equal("BLAST!", PlayStamp.HazardTell(LiveEvent.BallRedirected, HazardType.Barrel)!.Word);
        Assert.Equal("CHOMP!", PlayStamp.HazardTell(LiveEvent.BallRedirected, HazardType.Chomper)!.Word);
        Assert.Equal(new LiveStamp("STAR!", StampAnchor.Dirt), PlayStamp.HazardTell(LiveEvent.RewardHit));
        Assert.Equal(new LiveStamp("BONK!", StampAnchor.Dirt), PlayStamp.HazardTell(LiveEvent.BodyCarom));
        Assert.Equal(new LiveStamp("SURF'S UP!", StampAnchor.Dirt), PlayStamp.HazardTell(LiveEvent.BallCarried));
        foreach (var other in Enum.GetValues<LiveEvent>().Except([LiveEvent.BodySlowed, LiveEvent.BallRedirected, LiveEvent.RewardHit, LiveEvent.BodyCarom,
                     LiveEvent.BallCarried]))
            Assert.Null(PlayStamp.HazardTell(other));
    }
}
