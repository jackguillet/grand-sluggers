using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class BroadcastHudTests
{
    [Fact]
    public void PlayHudMutesDuringSpectacleSmashAndFreeze()
    {
        Assert.False(BroadcastHud.MutePlay(false, 0, 0));
        Assert.True(BroadcastHud.MutePlay(true, 0, 0));
        Assert.True(BroadcastHud.MutePlay(false, 0.55, 0));
        Assert.True(BroadcastHud.MutePlay(false, 0, 0.12));
        Assert.False(BroadcastHud.MutePlay(false, 0, 0));
    }
}
