using Xunit;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;

namespace GrandSluggers.Sim.Tests;

public class PauseMenuTests
{
    [Fact]
    public void StartOpensCallTimeAfterTheReadyBeat()
    {
        Assert.False(PauseMenu.Open(paused: false, allowed: true, start: true, t: 0f));
        Assert.False(PauseMenu.Open(paused: false, allowed: true, start: true, t: 0.19f));
        Assert.True(PauseMenu.Open(paused: false, allowed: true, start: true, t: 0.21f));
        Assert.False(PauseMenu.Open(paused: true, allowed: true, start: true, t: 1f));
        Assert.False(PauseMenu.Open(paused: false, allowed: false, start: true, t: 1f));
        Assert.False(PauseMenu.Open(paused: false, allowed: true, start: false, t: 1f));
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
    public void RowsStackInsideThePanelWithoutOverlap()
    {
        const float sw = 1280f;
        const float sh = 720f;
        var p = PauseMenu.Panel(sw, sh);
        for (var i = 0; i < PauseMenu.Items.Count; i++)
        {
            var r = PauseMenu.ItemRect(i, sw, sh);
            Assert.True(r.X >= p.X && r.X + r.W <= p.X + p.W, $"row {i} leaves the panel");
            Assert.True(r.Y >= p.Y && r.Y + r.H <= p.Y + p.H, $"row {i} leaves the panel");
            if (i > 0) Assert.True(r.Y >= PauseMenu.ItemRect(i - 1, sw, sh).Y + PauseMenu.ItemRect(i - 1, sw, sh).H, $"row {i} overlaps");
        }
    }

    [Fact]
    public void ViewOpensHowToOnTheFrontOfHouse()
    {
        Assert.False(PauseMenu.OpenHowTo(paused: false, allowed: true, howTo: true, t: 0f));
        Assert.True(PauseMenu.OpenHowTo(paused: false, allowed: true, howTo: true, t: 0.21f));
        Assert.False(PauseMenu.OpenHowTo(paused: false, allowed: false, howTo: true, t: 1f));
        Assert.False(PauseMenu.OpenHowTo(paused: true, allowed: true, howTo: true, t: 1f));
        Assert.False(PauseMenu.OpenHowTo(paused: false, allowed: true, howTo: false, t: 1f));
        Assert.True(PauseMenu.Open(paused: false, allowed: true, start: true, t: 1f));
        Assert.False(PauseMenu.Open(paused: false, allowed: true, start: false, t: 1f));
    }

    [Theory]
    [InlineData(1280, 800)]
    [InlineData(1920, 1080)]
    public void CallTimeFooterKeepsEscResumeInFrame(int screenW, int screenH)
    {
        var p = PauseMenu.Panel(screenW, screenH);
        var f = PauseMenu.FooterRect(screenW, screenH);
        Assert.True(p.X >= 8 && p.X + p.W <= screenW - 8);
        Assert.True(p.Y >= 8 && p.Y + p.H <= screenH - 8);
        Assert.True(f.X >= 8 && f.X + f.W <= screenW - 8);
        Assert.True(f.Y >= p.Y && f.Y + f.H <= p.Y + p.H + 0.01f);
        Assert.True(f.W >= 400);
        Assert.Equal(2, PauseMenu.FooterLines.Count);
        Assert.Contains("East / Start resume", PauseMenu.FooterLines[1]);
        Assert.DoesNotContain("Esc /", PauseMenu.FooterLines[0]);
    }
}
