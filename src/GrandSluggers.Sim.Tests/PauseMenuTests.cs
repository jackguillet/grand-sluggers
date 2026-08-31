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

    [Fact]
    public void ClickHitsTheHighlightedRow()
    {
        const float sw = 1280f;
        const float sh = 720f;
        var howTo = PauseMenu.HitItem(0, 0, sw, sh);
        Assert.Equal(-1, howTo);
        for (var i = 0; i < PauseMenu.Items.Count; i++)
        {
            var r = PauseMenu.ItemRect(i, sw, sh);
            Assert.Equal(i, PauseMenu.HitItem(r.X + 8, r.Y + 8, sw, sh));
        }
        Assert.True(PauseMenu.Contains(sw * 0.5f, sh * 0.5f, sw, sh));
    }
}
