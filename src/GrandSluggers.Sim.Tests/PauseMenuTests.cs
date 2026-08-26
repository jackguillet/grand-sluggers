using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class PauseMenuTests
{
    [Fact]
    public void StartOpensCallTimeAfterTheReadyBeat()
    {
        Assert.False(PauseMenu.Open(paused: false, inAtBat: true, start: true, t: 0f));
        Assert.False(PauseMenu.Open(paused: false, inAtBat: true, start: true, t: 0.19f));
        Assert.True(PauseMenu.Open(paused: false, inAtBat: true, start: true, t: 0.21f));
        Assert.False(PauseMenu.Open(paused: true, inAtBat: true, start: true, t: 1f));
        Assert.False(PauseMenu.Open(paused: false, inAtBat: false, start: true, t: 1f));
        Assert.False(PauseMenu.Open(paused: false, inAtBat: true, start: false, t: 1f));
    }

    [Fact]
    public void SameStartPressDoesNotDismiss()
    {
        Assert.False(PauseMenu.Dismiss(startOrBack: true, t: 0f));
        Assert.False(PauseMenu.Dismiss(startOrBack: true, t: PauseMenu.Debounce));
        Assert.True(PauseMenu.Dismiss(startOrBack: true, t: PauseMenu.Debounce + 0.01f));
        Assert.False(PauseMenu.Dismiss(startOrBack: false, t: 1f));
    }
}
