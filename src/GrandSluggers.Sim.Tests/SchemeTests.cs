using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class SchemeTests
{
    [Fact]
    public void EveryProductVerbHasPadAndKeyboard()
    {
        foreach (var id in new[]
        {
            "confirm", "charge", "star", "aim-run", "bags",
            "all-advance", "all-return", "steal", "changeup", "swap", "bunt",
            "call-time", "dash", "pickoff", "skip"
        })
        {
            var v = Scheme.Must(id);
            Assert.False(string.IsNullOrWhiteSpace(v.Pad), id);
            Assert.False(string.IsNullOrWhiteSpace(v.Keys), id);
            Assert.False(Scheme.IsDebug(v.Keys));
        }
        Assert.Equal("Space / Enter", Scheme.Keys("confirm"));
        Assert.Equal("Shift", Scheme.Keys("charge"));
        Assert.Equal("Q", Scheme.Keys("star"));
        Assert.Equal("WASD", Scheme.Keys("aim-run"));
        Assert.Equal("1 2 3 4", Scheme.Keys("bags"));
        Assert.Equal(",", Scheme.Keys("all-advance"));
        Assert.Equal(".", Scheme.Keys("all-return"));
        Assert.Equal("Z", Scheme.Keys("steal"));
        Assert.Equal("V", Scheme.Keys("changeup"));
        Assert.Equal("R", Scheme.Keys("swap"));
        Assert.Equal("V", Scheme.Keys("bunt"));
        Assert.Equal("H", Scheme.Keys("call-time"));
        Assert.Equal("South", Scheme.Pad("confirm"));
        Assert.Equal("LB", Scheme.Pad("all-advance"));
        Assert.Equal("RB", Scheme.Pad("all-return"));
        Assert.Equal("L3", Scheme.Pad("steal"));
    }

    [Fact]
    public void F1F2F3StayDebug()
    {
        Assert.True(Scheme.IsDebug("F1"));
        Assert.True(Scheme.IsDebug("F2"));
        Assert.True(Scheme.IsDebug("F3"));
        Assert.False(Scheme.IsDebug("Space / Enter"));
        Assert.False(Scheme.IsDebug("Z"));
        Assert.DoesNotContain(Scheme.Product, v => v.Keys.Contains("F1") || v.Keys.Contains("F2") || v.Keys.Contains("F3"));
    }

    [Fact]
    public void PauseMenuAndHowToPlayAreTheInGameCouchMap()
    {
        Assert.Equal(4, PauseMenu.Items.Count);
        Assert.Equal(PauseMenu.Item.Resume, PauseMenu.At(0));
        Assert.Equal(PauseMenu.Item.Restart, PauseMenu.At(1));
        Assert.Equal(PauseMenu.Item.HowToPlay, PauseMenu.At(2));
        Assert.Equal(PauseMenu.Item.Title, PauseMenu.At(3));
        Assert.Equal("How to play", PauseMenu.Label(PauseMenu.Item.HowToPlay));
        Assert.Equal(PauseMenu.Item.Title, PauseMenu.At(PauseMenu.Wrap(0, -1)));
        Assert.Equal(PauseMenu.Item.Restart, PauseMenu.At(PauseMenu.Wrap(0, 1)));
        Assert.True(HowToPlay.Pages.Count >= 4);
        Assert.True(HowToPlay.Mentions("South"));
        Assert.True(HowToPlay.Mentions("Space"));
        Assert.True(HowToPlay.Mentions("MAX"));
        Assert.True(HowToPlay.Mentions("oval"));
        Assert.True(HowToPlay.Mentions("changeup"));
        Assert.True(HowToPlay.Mentions("call time"));
        Assert.True(HowToPlay.Mentions("outfielder"));
        Assert.True(HowToPlay.Mentions("Charge ring"));
        Assert.True(HowToPlay.Mentions("puffs dirt"));
        Assert.True(HowToPlay.Mentions("does not follow"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("sticker"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("postcard"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("toys"));
        Assert.Contains(HowToPlay.Must("lineup").Lines, l => l.Contains("Hearts"));
        Assert.Contains(HowToPlay.Must("lineup").Lines, l => l.Contains("Stars jump"));
        Assert.Contains(HowToPlay.Must("controls").Lines, l => l.Contains("South") && l.Contains("Space"));
        Assert.Contains(HowToPlay.Must("pitch-swing").Lines, l => l.Contains("charge", StringComparison.OrdinalIgnoreCase));
        Assert.False(HowToPlay.Mentions("cycle pitch"));
        Assert.False(HowToPlay.Mentions("cycle fastball"));
        Assert.DoesNotContain(HowToPlay.Pages.SelectMany(p => p.Lines), l => l.Contains("F1") && l.Contains("timing", StringComparison.OrdinalIgnoreCase) && !l.Contains("debug"));
        Assert.Contains(HowToPlay.Must("pause-practice").Lines, l => l.Contains("F1") && l.Contains("debug"));
    }
}
