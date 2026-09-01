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
            Assert.False(string.IsNullOrWhiteSpace(v.Mouse), id);
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
        Assert.Equal("Esc", Scheme.Keys("how-to"));
        Assert.Equal("Esc", Scheme.Mouse("how-to"));
        Assert.Equal("Left click", Scheme.Mouse("confirm"));
        Assert.Equal("Right click hold", Scheme.Mouse("charge"));
        Assert.Equal("Right-drag", Scheme.Mouse("aim-run"));
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
        Assert.Equal("contents", HowToPlay.Pages[0].Id);
        Assert.Contains(HowToPlay.Must("contents").Lines, l => l.Contains("instruction booklet") || l.Contains("Call time"));
        Assert.Contains(HowToPlay.Must("getting-started").Lines, l => l.Contains("Exhibition"));
        Assert.Contains(HowToPlay.Must("getting-started").Lines, l => l.Contains("Training"));
        Assert.Contains(HowToPlay.Must("getting-started").Lines, l => l.Contains("Esc"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("landing ring"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("YOU"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("TIRED"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("ITEM"));
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
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("brim"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("dirt"));
        Assert.Contains(HowToPlay.Must("lineup").Lines, l => l.Contains("Hearts"));
        Assert.Contains(HowToPlay.Must("lineup").Lines, l => l.Contains("Stars jump"));
        Assert.Contains(HowToPlay.Must("lineup").Lines, l => l.Contains("Team Setup"));
        Assert.Contains(HowToPlay.Must("lineup").Lines, l => l.Contains("Two diamonds") || l.Contains("two diamonds"));
        Assert.Contains(HowToPlay.Must("controls").Lines, l => l.Contains("South") && l.Contains("Space"));
        Assert.Contains(HowToPlay.Must("controls").Lines, l => l.Contains("Left click"));
        Assert.True(HowToPlay.Mentions("mouse") || HowToPlay.Mentions("Mouse"));
        Assert.True(HowToPlay.Mentions("Esc"));
        Assert.Contains(HowToPlay.Must("chemistry").Lines, l => l.Contains("Hearts"));
        Assert.Contains(HowToPlay.Must("stars").Lines, l => l.Contains("two seconds"));
        Assert.Contains(HowToPlay.Must("abilities").Lines, l => l.Contains("field verb"));
        Assert.Contains(HowToPlay.Must("items").Lines, l => l.Contains("banana"));
        Assert.Contains(HowToPlay.Must("running").Lines, l => l.Contains("Close play"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("attack"));
        Assert.Contains(HowToPlay.Must("pitch-swing").Lines, l => l.Contains("charge", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(HowToPlay.Must("pitch-swing").Lines, l => l.Contains("mound") && l.Contains("SET"));
        Assert.Contains(HowToPlay.Must("pitch-swing").Lines, l => l.Contains("plate") && l.Contains("SET"));
        Assert.Contains(HowToPlay.Must("pitch-swing").Lines, l => l.Contains("cuts to pitch") || l.Contains("pitch:"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("you are the glove"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("turn two"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("throw both"));
        Assert.False(HowToPlay.Mentions("cycle pitch"));
        Assert.False(HowToPlay.Mentions("cycle fastball"));
        Assert.DoesNotContain(HowToPlay.Pages.SelectMany(p => p.Lines), l => l.Contains("F1") && l.Contains("timing", StringComparison.OrdinalIgnoreCase) && !l.Contains("debug"));
        Assert.Contains(HowToPlay.Must("pause-practice").Lines, l => l.Contains("F1") && l.Contains("debug"));
        var running = HowToPlay.Must("running").Lines;
        Assert.Contains(running, l => l.Contains("D-pad") && l.Contains("1B"));
        Assert.Contains(running, l => l.Contains("highlighted"));
        Assert.Contains(running, l => l.Contains("selected runner"));
        Assert.Contains(running, l => l.Contains("No steal home"));
        Assert.Contains(running, l => l.Contains("catcher") && l.Contains("guns"));
        Assert.Contains(running, l => l.Contains("CAUGHT STEALING"));
        Assert.Contains(running, l => l.Contains("Dead stick"));
        Assert.DoesNotContain(running, l => l.Contains("steal the lead runner"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("steal gun") || l.Contains("without a hop"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("West") && l.Contains("window"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("wall"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("3/4") && l.Contains("glove"));
        var two = HowToPlay.Must("two-pads").Lines;
        Assert.Contains(two, l => l.Contains("Gamepad 0") && l.Contains("home"));
        Assert.Contains(two, l => l.Contains("Gamepad 1") && l.Contains("away"));
        Assert.Contains(two, l => l.Contains("Keyboard") && l.Contains("mouse") && l.Contains("player 1"));
        Assert.Contains(two, l => l.Contains("Unplug"));
        Assert.Contains(two, l => l.Contains("plate"));
        Assert.Contains(two, l => l.Contains("CPU never"));
        Assert.Contains(two, l => l.Contains("fielding pad") || l.Contains("Fielding pad"));
        Assert.True(HowToPlay.Mentions("pad 2") || HowToPlay.Mentions("Pad 2"));
    }
}
