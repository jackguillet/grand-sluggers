using Xunit;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;

namespace GrandSluggers.Sim.Tests;

public class SchemeTests
{
    [Fact]
    public void EveryProductVerbHasControllerOnlyBindings()
    {
        foreach (var v in Scheme.Product)
        {
            Assert.False(string.IsNullOrWhiteSpace(v.Pad), v.Id);
            Assert.False(HowToPlay.NamesKeyboard(v.Pad), v.Id);
        }
        Assert.Equal("RT hold / release", Scheme.Pad("charge"));
        Assert.Equal("LT hold at RT release", Scheme.Pad("star"));
        Assert.Equal("LB", Scheme.Pad("steal"));
        Assert.Equal("North", Scheme.Pad("jump"));
        Assert.Equal("West hold", Scheme.Pad("bunt-third"));
        Assert.Equal("North hold", Scheme.Pad("bunt-first"));
        Assert.Equal("Automatic by position", Scheme.Pad("catch"));
        Assert.Equal("Start", Scheme.Pad("call-time"));
        Assert.Equal("View / Select", Scheme.Pad("how-to"));
    }

    [Fact]
    public void F1F2F3StayDebug()
    {
        Assert.True(Scheme.IsDebug("F1"));
        Assert.True(Scheme.IsDebug("F2"));
        Assert.True(Scheme.IsDebug("F3"));
        Assert.False(Scheme.IsDebug("Space / Enter"));
        Assert.False(Scheme.IsDebug("Z"));
        Assert.DoesNotContain(Scheme.Product, v => v.Pad.Contains("F1") || v.Pad.Contains("F2") || v.Pad.Contains("F3"));
    }

    [Fact]
    public void PauseMenuAndHowToPlayAreTheInGameCouchMap()
    {
        Assert.Equal(6, PauseMenu.Items.Count);
        Assert.Equal(PauseMenu.Item.Resume, PauseMenu.At(0));
        Assert.Equal(PauseMenu.Item.Restart, PauseMenu.At(1));
        Assert.Equal(PauseMenu.Item.HowToPlay, PauseMenu.At(2));
        Assert.Equal(PauseMenu.Item.ArrangeDefense, PauseMenu.At(3));
        Assert.Equal("How to play", PauseMenu.Label(PauseMenu.Item.HowToPlay));
        Assert.Equal(PauseMenu.Item.Quit, PauseMenu.At(PauseMenu.Wrap(0, -1)));
        Assert.Equal(PauseMenu.Item.Restart, PauseMenu.At(PauseMenu.Wrap(0, 1)));
        Assert.True(HowToPlay.Pages.Count >= 4);
        Assert.Equal("contents", HowToPlay.Pages[0].Id);
        var book = HowToPlay.BookPanel(1280, 800);
        Assert.True(book.W >= 1100, $"book too narrow w={book.W}");
        Assert.True(book.H >= 700, $"book too short h={book.H}");
        foreach (var page in HowToPlay.Pages)
            Assert.InRange(page.Lines.Count, 1, HowToPlay.KidLineMax);
        Assert.Contains(HowToPlay.Must("contents").Lines, l => l.Contains("instruction booklet") || l.Contains("Call time"));
        var contents = HowToPlay.Must("contents");
        var introBand = ContentsToc.LineBand(1280, 800);
        Assert.True(contents.Lines.Count * HowToPlay.KidLineH <= introBand.H,
            "contents copy must fit the readable intro band");
        Assert.True(HowToPlay.KidLineH >= 48);
        Assert.True(HowToPlay.BookLinePt >= 32);
        var text = HowToPlay.TextRect(1280, 800);
        Assert.True(text.W > 1000, "type uses the page, not a strip beside a photo");
        Assert.Contains(HowToPlay.Must("getting-started").Lines, l => l.Contains("Exhibition"));
        Assert.Contains(HowToPlay.Must("getting-started").Lines, l => l.Contains("Training"));
        Assert.Contains(HowToPlay.Must("getting-started").Lines, l => l.Contains("View"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("landing ring") && l.Contains("circle"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("circle") && l.Contains("red"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("YOU"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("TIRED"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("ITEM"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("BALL") && l.Contains("STRIKE") && l.Contains("WALK") && l.Contains("stamp"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("on the field") && l.Contains("next pitch"));
        Assert.True(HowToPlay.Mentions("South"));
        Assert.False(HowToPlay.Mentions("Space"));
        Assert.True(HowToPlay.Mentions("MAX"));
        Assert.True(HowToPlay.Mentions("oval"));
        Assert.Contains(HowToPlay.Must("the-box").Lines, l => l.Contains("sitting") && l.Contains("stick"));
        // D12: the box recenters after every pitch, and the book says so.
        Assert.Contains(HowToPlay.Must("the-box").Lines, l => l.Contains("resets each pitch"));
        // #876: the cycle walks all three of a pitcher's pitches (fastball, second, third), so the
        // pitching card names the slots rather than the old two-pitch "FB, CH changeup".
        Assert.Contains(HowToPlay.Must("pitch-swing").Lines, l => l.Contains("Cycle pitch") && l.Contains("charge"));
        Assert.DoesNotContain(HowToPlay.Must("pitch-swing").Lines, l => l.Contains("CH changeup"));
        Assert.True(HowToPlay.Mentions("call time"));
        Assert.True(HowToPlay.Mentions("outfielder"));
        Assert.Contains(HowToPlay.Must("pitch-swing").Lines,
            line => line.Contains("charge", StringComparison.OrdinalIgnoreCase) && line.Contains("MAX"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("Shadow tracks ball"));
        Assert.True(HowToPlay.Mentions("does not follow"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("time and hazards"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("portraits") && l.Contains("South"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("confirmed", StringComparison.OrdinalIgnoreCase) && l.Contains("reserved"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("HOME") && l.Contains("AWAY"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("Choose captains"));
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("1 vs CPU") && l.Contains("2 controllers"));
        Assert.Contains(HowToPlay.Must("two-pads").Lines, l => l.Contains("2 controllers"));
        Assert.Contains(HowToPlay.Must("lineup").Lines, l => l.Contains("card") && l.Contains("highlights buddies"));
        // PH-16-R16: both teams start on the one reserve; the draft does not move it.
        Assert.Contains(HowToPlay.Must("lineup").Lines, l => l.Contains("same stars"));
        Assert.Contains(HowToPlay.Must("lineup").Lines, l => l.Contains("Team Setup"));
        Assert.Contains(HowToPlay.Must("lineup").Lines, l => l.Contains("Two diamonds") || l.Contains("two diamonds"));
        Assert.Contains(HowToPlay.Must("controls").Lines, l => l.Contains("RT"));
        Assert.DoesNotContain(HowToPlay.Must("controls").Lines, l => l.Contains("Space"));
        Assert.False(HowToPlay.Mentions("mouse"));
        Assert.False(HowToPlay.Mentions("keyboard"));
        Assert.False(HowToPlay.Mentions("Esc"));
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
        Assert.True(HowToPlay.Mentions("foul"));
        Assert.True(HowToPlay.Mentions("foul"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("you are the glove"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("turn two"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("throw both"));
        // PH-02-R5 (#825) supersedes the #563 ban: the pre-charge family cycle IS the mound's verb now,
        // so the book must teach it. "cycle fastball" stays retired (it was a
        // post-release trajectory cycle, a different thing).
        Assert.True(HowToPlay.Mentions("Cycle pitch"));
        Assert.False(HowToPlay.Mentions("cycle fastball"));
        Assert.DoesNotContain(HowToPlay.Pages.SelectMany(p => p.Lines), l => l.Contains("F1") && l.Contains("timing", StringComparison.OrdinalIgnoreCase) && !l.Contains("debug"));
        Assert.Contains(HowToPlay.Must("pause-practice").Lines, l => l.Contains("Title → Tutorials"));
        var running = HowToPlay.Must("running").Lines;
        Assert.Contains(running, l => l.Contains("on a bag") && l.Contains("second"));
        Assert.Contains(running, l => l.Contains("nobody left") || l.Contains("out with nobody"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("Pickup") || l.Contains("does not end"));
        Assert.Contains(running, l => l.Contains("Right stick") && l.Contains("1B"));
        Assert.Contains(running, l => l.Contains("Selection follows that runner"));
        Assert.Contains(running, l => l.Contains("LB sends the selection"));
        Assert.Contains(running, l => l.Contains("before contact"));
        Assert.Contains(running, l => l.Contains("RB returns it immediately"));
        Assert.DoesNotContain(running, l => l.Contains("perfect steal"));
        Assert.Contains(HowToPlay.Must("steal-race").Lines, l => l.Contains("catcher") && l.Contains("buffer a throw"));
        Assert.Contains(running, l => l.Contains("CAUGHT STEALING"));
        Assert.DoesNotContain(running, l => l.Contains("No steal home"));
        Assert.Contains(running, l => l.Contains("D-pad Up halts"));
        Assert.DoesNotContain(running, l => l.Contains("steal the lead runner"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("landing") && l.Contains("fly"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("hangs"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("North") && l.Contains("jumps"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("dives sideways") && l.Contains("never automatic"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("wall"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("gun to first") || l.Contains("throw is yours") || l.Contains("guess a force"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("catch") && l.Contains("throw"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("45") && l.Contains("fly"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("CF") && l.Contains("top"));
        // Spec §15, D14: the camera follows the ball; only a close play cuts to the bag. The couch map says so.
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("close play") && l.Contains("bag"));
        Assert.DoesNotContain(HowToPlay.Must("fielding").Lines, l => l.Contains("throw sits on its bag"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("Arrange defense") && l.Contains("pulses"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("runs with the ball"));
        var two = HowToPlay.Must("two-pads").Lines;
        Assert.Contains(two, l => l.Contains("first controller") && l.Contains("player 1"));
        Assert.Contains(two, l => l.Contains("P1") && l.Contains("HOME") && l.Contains("AWAY"));
        Assert.Contains(two, l => l.Contains("second controller"));
        Assert.Contains(two, l => l.Contains("Each player uses a controller"));
        Assert.Contains(two, l => l.Contains("drops") && l.Contains("play stops"));
        Assert.Contains(two, l => l.Contains("unseated controller") && l.Contains("South"));
        Assert.Contains(two, l => l.Contains("plate"));
        Assert.Contains(two, l => l.Contains("CPU never"));
        Assert.Contains(two, l => l.Contains("fielding controller") || l.Contains("Fielding controller"));
        Assert.True(HowToPlay.Mentions("controller 2") || HowToPlay.Mentions("Controller 2"));
        foreach (var page in HowToPlay.Pages)
        foreach (var line in page.Lines)
            Assert.False(HowToPlay.NamesKeyboard(line), page.Id + ": " + line);
        Assert.Contains(HowToPlay.Must("pitch-swing").Lines, l => l.Contains("RT"));

        var padPitch = HowToPlay.Must("pitch-swing").Lines;
        Assert.Contains(padPitch, l => l.Contains("Cycle pitch: West before charge")
            && l.Contains("hold West / North") && l.Contains("East cancels")
            && l.Contains("LT at RT release"));
        var cycle = RoleTables.Pad.First(b => b.Id == "pitching").Rows.First(r => r.Verb == "Cycle pitch");
        Assert.Contains("West", cycle.Press);
        Assert.Contains("locks", cycle.Press);
        Assert.Contains("Fastball", cycle.Press);
        // PH-06-R1: the SET ring names the rubber, never the crossing.
        {
            var box = HowToPlay.Must("the-box").Lines;
            Assert.Contains(box, l => l.Contains("pale ring") && l.Contains("rubber")
                && l.Contains("not where the pitch will cross"));
            Assert.DoesNotContain(box, l => l.Contains("ring") && l.Contains("where it will cross"));
        }

        var allCouchCopy = HowToPlay.Pages.SelectMany(page => page.Lines)
            .Concat(GettingStarted.Modes.Select(mode => mode.Line))
            .ToArray();
        Assert.DoesNotContain(allCouchCopy, line => line.Contains("Gamepad 0") || line.Contains("Gamepad 1"));
        Assert.DoesNotContain(allCouchCopy, line => line.Contains("Unplug = CPU"));
    }

    [Fact]
    public void TheProjectRunsTheInputSystemAlone()
    {
        // Gamepad only (#1047): the player reads pads through the Input System, and the old Input Manager is off, so no
        // code path can read a key or the mouse through it. 0 is the old manager, 1 the Input System, 2 both.
        var settings = File.ReadAllText(Path.Combine(Shipped.Content.Root.Shipped, "..", "unity", "ProjectSettings", "ProjectSettings.asset"));
        Assert.Contains("  activeInputHandler: 1\n", settings.Replace("\r\n", "\n"), StringComparison.Ordinal);
    }
}
