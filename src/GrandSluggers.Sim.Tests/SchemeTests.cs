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
        Assert.Equal("Space hold", Scheme.Keys("charge"));
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
        Assert.Equal("Left click hold", Scheme.Mouse("charge"));
        Assert.Equal("Right-drag", Scheme.Mouse("aim-run"));
        Assert.Equal("South", Scheme.Pad("confirm"));
        Assert.Equal("South hold", Scheme.Pad("charge"));
        Assert.Equal("LB", Scheme.Pad("all-advance"));
        Assert.Equal("RB", Scheme.Pad("all-return"));
        Assert.Equal("Stick to the next bag / L3", Scheme.Pad("steal"));
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
        var book = HowToPlay.BookPanel(1280, 800);
        Assert.True(book.W >= 1100, $"book too narrow w={book.W}");
        Assert.True(book.H >= 700, $"book too short h={book.H}");
        foreach (var page in HowToPlay.Pages)
        {
            Assert.False(string.IsNullOrWhiteSpace(page.Picture), page.Id);
            Assert.InRange(page.Lines.Count, 1, HowToPlay.KidLineMax);
            if (page.KeyLines != null)
                Assert.InRange(page.KeyLines.Count, 1, HowToPlay.KidLineMax);
        }
        Assert.Contains(HowToPlay.Must("contents").Lines, l => l.Contains("instruction booklet") || l.Contains("Call time"));
        var contents = HowToPlay.Must("contents");
        var introBand = ContentsToc.LineBand(1280, 800);
        foreach (var scheme in new[] { InputScheme.Pad, InputScheme.Keys })
            Assert.True(contents.Shown(scheme).Count * HowToPlay.KidLineH <= introBand.H,
                $"{scheme} contents copy must fit the readable intro band");
        Assert.False(HowToPlay.ShowsSplash("fielding"));
        Assert.False(HowToPlay.ShowsSplash("stars"));
        Assert.False(HowToPlay.ShowsSplash("the-box"));
        Assert.True(HowToPlay.KidLineH >= 48);
        Assert.True(HowToPlay.BookLinePt >= 32);
        var text = HowToPlay.TextRect(1280, 800);
        Assert.True(text.W > 1000, "type uses the page, not a strip beside a photo");
        Assert.Contains(HowToPlay.Must("getting-started").Lines, l => l.Contains("Exhibition"));
        Assert.Contains(HowToPlay.Must("getting-started").Lines, l => l.Contains("Training"));
        Assert.Contains(HowToPlay.Must("getting-started").Lines, l => l.Contains("Esc"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("landing ring") && l.Contains("circle"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("circle") && l.Contains("red"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("YOU"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("TIRED"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("ITEM"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("BALL") && l.Contains("STRIKE") && l.Contains("WALK") && l.Contains("stamp"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("on the field") && l.Contains("next pitch"));
        Assert.True(HowToPlay.Mentions("South"));
        Assert.True(HowToPlay.Mentions("Space"));
        Assert.True(HowToPlay.Mentions("MAX"));
        Assert.True(HowToPlay.Mentions("oval"));
        Assert.Contains(HowToPlay.Must("the-box").Lines, l => l.Contains("sitting") && l.Contains("stick"));
        Assert.Contains(HowToPlay.Must("the-box").KeyLines!, l => l.Contains("SET") && l.Contains("do not walk"));
        Assert.True(HowToPlay.Mentions("changeup"));
        Assert.True(HowToPlay.Mentions("call time"));
        Assert.True(HowToPlay.Mentions("outfielder"));
        foreach (var scheme in new[] { InputScheme.Pad, InputScheme.Keys })
            Assert.Contains(HowToPlay.Must("pitch-swing").Shown(scheme),
                line => line.Contains("charge", StringComparison.OrdinalIgnoreCase) && line.Contains("MAX"));
        Assert.True(HowToPlay.Mentions("puffs dirt"));
        Assert.True(HowToPlay.Mentions("does not follow"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("sticker") && l.Contains("over the infield"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("postcard"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("toys"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("brim"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("dirt"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("HOME") && l.Contains("AWAY"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("North"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("1 PLAYER") && l.Contains("2 PLAYERS"));
        Assert.Contains(HowToPlay.Must("two-pads").Lines, l => l.Contains("2 PLAYERS"));
        Assert.Contains(HowToPlay.Must("lineup").Lines, l => l.Contains("Hearts"));
        Assert.Contains(HowToPlay.Must("lineup").Lines, l => l.Contains("Stars jump"));
        Assert.Contains(HowToPlay.Must("lineup").Lines, l => l.Contains("Team Setup"));
        Assert.Contains(HowToPlay.Must("lineup").Lines, l => l.Contains("Two diamonds") || l.Contains("two diamonds"));
        Assert.Contains(HowToPlay.Must("controls").Lines, l => l.Contains("South"));
        Assert.DoesNotContain(HowToPlay.Must("controls").Lines, l => l.Contains("Space"));
        Assert.Contains(HowToPlay.Must("controls").KeyLines!, l => l.Contains("Space") || l.Contains("left click"));
        Assert.DoesNotContain(HowToPlay.Must("controls").KeyLines!, l => l.Contains("South"));
        Assert.True(HowToPlay.Mentions("mouse") || HowToPlay.Mentions("Mouse"));
        Assert.True(HowToPlay.Mentions("Esc"));
        Assert.Contains(HowToPlay.Must("chemistry").Lines, l => l.Contains("Hearts"));
        Assert.Contains(HowToPlay.Must("stars").Lines, l => l.Contains("two seconds"));
        Assert.Contains(HowToPlay.Must("abilities").Lines, l => l.Contains("field verb"));
        Assert.Contains(HowToPlay.Must("items").Lines, l => l.Contains("banana"));
        Assert.Contains(HowToPlay.Must("running").Lines, l => l.Contains("Close play"));
        Assert.Contains(HowToPlay.Must("running").Lines, l => l.Contains("SAFE"));
        Assert.Contains(HowToPlay.Must("running").Lines, l => l.Contains("touch") && l.Contains("tag"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("tag") && l.Contains("throw"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("attack"));
        Assert.Contains(HowToPlay.Must("pitch-swing").Lines, l => l.Contains("charge", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(HowToPlay.Must("pitch-swing").Lines, l => l.Contains("gold streak"));
        Assert.Contains(HowToPlay.Must("pitch-swing").Lines, l => l.Contains("pitcher's shoulder") && l.Contains("behind home"));
        Assert.Contains(HowToPlay.Must("pitch-swing").Lines, l => l.Contains("plate") && l.Contains("SET"));
        Assert.Contains(HowToPlay.Must("pitch-swing").Lines, l => l.Contains("does not cut"));
        Assert.Contains(HowToPlay.Must("pitch-swing").Lines, l => l.Contains("foul") && l.Contains("Strike"));
        Assert.True(HowToPlay.Mentions("foul"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("you are the glove"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("turn two"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("throw both"));
        Assert.False(HowToPlay.Mentions("cycle pitch"));
        Assert.False(HowToPlay.Mentions("cycle fastball"));
        Assert.DoesNotContain(HowToPlay.Pages.SelectMany(p => p.Lines), l => l.Contains("F1") && l.Contains("timing", StringComparison.OrdinalIgnoreCase) && !l.Contains("debug"));
        Assert.Contains(HowToPlay.Must("pause-practice").Lines, l => l.Contains("F1") && l.Contains("debug"));
        var running = HowToPlay.Must("running").Lines;
        Assert.Contains(running, l => l.Contains("on a bag") && l.Contains("second"));
        Assert.Contains(running, l => l.Contains("nobody left") || l.Contains("out with nobody"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("Pickup") || l.Contains("does not end"));
        Assert.Contains(running, l => l.Contains("D-pad") && l.Contains("1B"));
        Assert.Contains(running, l => l.Contains("highlighted"));
        Assert.Contains(running, l => l.Contains("selected runner"));
        Assert.Contains(running, l => l.Contains("No steal home"));
        Assert.Contains(running, l => l.Contains("catcher") && l.Contains("guns"));
        Assert.Contains(running, l => l.Contains("CAUGHT STEALING"));
        Assert.Contains(running, l => l.Contains("Dead stick"));
        Assert.DoesNotContain(running, l => l.Contains("steal the lead runner"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("landing") && l.Contains("fly"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("hangs"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("West") && l.Contains("window"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("wall"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("gun to first") || l.Contains("throw is yours") || l.Contains("guess a force"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("catch") && l.Contains("throw"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("45") && l.Contains("fly"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("CF") && l.Contains("top"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("Home") && l.Contains("second"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("Select") && l.Contains("pulses"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("runs with the ball"));
        var two = HowToPlay.Must("two-pads").Lines;
        Assert.Contains(two, l => l.Contains("first controller") && l.Contains("player 1"));
        Assert.Contains(two, l => l.Contains("North") && l.Contains("HOME"));
        Assert.Contains(two, l => l.Contains("second controller"));
        Assert.Contains(two, l => l.Contains("Keyboard") && l.Contains("mouse") && l.Contains("player 1"));
        Assert.Contains(two, l => l.Contains("drops") && l.Contains("play stops"));
        Assert.Contains(two, l => l.Contains("unseated controller") && l.Contains("South"));
        Assert.Contains(HowToPlay.Must("two-pads").KeyLines!,
            l => l.Contains("Space") && l.Contains("keyboard + mouse"));
        Assert.Contains(two, l => l.Contains("plate"));
        Assert.Contains(two, l => l.Contains("CPU never"));
        Assert.Contains(two, l => l.Contains("fielding controller") || l.Contains("Fielding controller"));
        Assert.True(HowToPlay.Mentions("controller 2") || HowToPlay.Mentions("Controller 2"));
        foreach (var page in HowToPlay.Pages)
        {
            foreach (var line in page.Shown(InputScheme.Pad))
                Assert.False(HowToPlay.MixesHardware(line), page.Id + " pad: " + line);
            foreach (var line in page.Shown(InputScheme.Keys))
                Assert.False(HowToPlay.MixesHardware(line), page.Id + " keys: " + line);
        }
        Assert.True(HowToPlay.Must("pitch-swing").KeyLines is { Count: > 0 });
        Assert.True(HowToPlay.Must("fielding").KeyLines is { Count: > 0 });
        Assert.True(HowToPlay.Must("running").KeyLines is { Count: > 0 });
        Assert.Contains(HowToPlay.Must("pitch-swing").Shown(InputScheme.Pad), l => l.Contains("South"));
        Assert.DoesNotContain(HowToPlay.Must("pitch-swing").Shown(InputScheme.Pad), l => l.Contains("Space"));
        Assert.Contains(HowToPlay.Must("pitch-swing").Shown(InputScheme.Keys), l => l.Contains("Space"));
        Assert.DoesNotContain(HowToPlay.Must("pitch-swing").Shown(InputScheme.Keys), l => l.Contains("South"));

        var padPitch = HowToPlay.Must("pitch-swing").Shown(InputScheme.Pad);
        Assert.Contains(padPitch, l => l.Contains("hold West", StringComparison.OrdinalIgnoreCase)
            && l.Contains("South") && l.Contains("changeup", StringComparison.OrdinalIgnoreCase)
            && l.Contains("bunt", StringComparison.OrdinalIgnoreCase));
        var keyPitch = HowToPlay.Must("pitch-swing").Shown(InputScheme.Keys);
        Assert.Contains(keyPitch, l => l.Contains("hold V/Ctrl", StringComparison.OrdinalIgnoreCase)
            && l.Contains("Space/left click") && l.Contains("changeup", StringComparison.OrdinalIgnoreCase)
            && l.Contains("bunt", StringComparison.OrdinalIgnoreCase));

        var allCouchCopy = HowToPlay.Pages.SelectMany(page => page.Lines.Concat(page.KeyLines ?? []))
            .Concat(GettingStarted.Modes.SelectMany(mode => new[] { mode.PadLine, mode.KeysLine }))
            .ToArray();
        Assert.DoesNotContain(allCouchCopy, line => line.Contains("Gamepad 0") || line.Contains("Gamepad 1"));
        Assert.DoesNotContain(allCouchCopy, line => line.Contains("Unplug = CPU"));
        Assert.Contains(HowToPlay.Must("two-pads").KeyLines!, line => line.Contains("Player 1: Q"));
    }
}
