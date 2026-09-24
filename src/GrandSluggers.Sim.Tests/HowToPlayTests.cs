using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The book follows the rules (spec §15, #570): the verbs P3–P6 retired are gone from every spread,
/// and the verbs they landed sit on the spread a stranger would open for them.
/// </summary>
public class HowToPlayTests
{
    [Theory]
    [InlineData("Center the stick, then tap South.", false)]
    [InlineData("Tap South or Enter.", true)]
    [InlineData("Tap South/Space.", true)]
    [InlineData("Hold the left click.", true)]
    [InlineData("Spacebar", false)]
    public void KeyboardCopyChecksWholeKeyNames(string copy, bool names) =>
        Assert.Equal(names, HowToPlay.NamesKeyboard(copy));

    static IEnumerable<string> EveryLine =>
        HowToPlay.Pages.SelectMany(p => p.Lines.Append(p.Title))
            .Concat(RoleTables.Pad.SelectMany(b => b.Rows).SelectMany(r => new[] { r.Verb, r.Press }))
            .Concat(BagDiagrams.Callouts.SelectMany(c => new[] { c.Title, c.Press, c.Line }))
            .Concat(BagDiagrams.Running.SelectMany(d => new[] { d.Title, d.Press }))
            .Concat(HudCallouts.OnScreenPage.SelectMany(s => s.Marks).Select(m => m.Label))
            .Concat(ControlDiagram.PadCallouts.SelectMany(c => new[] { c.Hardware, c.Offense, c.Defense, c.Always }))
            .Concat(GettingStarted.Path.Select(p => p.Caption))
            .Concat(GettingStarted.Modes.Select(m => m.Line))
            .Concat(HowToComic.OnPitchSwingPage.SelectMany(c => new[] { c.Caption, c.Motion.Charge, c.Motion.Commit }));

    [Fact]
    public void TheBookIsGamepadOnly()
    {
        // The game is gamepad only (pad 1, pad 2): no spread names a keyboard key or a mouse button.
        foreach (var line in EveryLine)
            Assert.False(HowToPlay.NamesKeyboard(line), line);
    }

    [Fact]
    public void ScreenPageExplainsTheLiveHandoffAndImmediateReturn()
    {
        var lines = HowToPlay.Must("screen-live").Lines;
        Assert.Contains(lines, line => line.Contains("Live: runners and outs"));
        Assert.Contains(lines, line => line.Contains("Effects never hide runners or outs"));
        Assert.Contains(lines, line => line.Contains("plate HUD returns immediately"));
    }

    [Fact]
    public void RetiredVerbsAreGoneFromEverySpread()
    {
        // D1 / P3: no leads, no lead stick, no lead pips; D3: no random pickoff.
        // #563 also banned "cycle pitch" — a post-release trajectory cycle. PH-02-R5 (#825) reuses the
        // words for a different verb, the pre-charge family cycle, which the book must now teach:
        // the ban is superseded here and the presence of the new row is asserted below.
        foreach (var retired in new[] { "take a lead", "lead off", "lead-off", "Lead01", "lead pip", "walking lead", "cycle fastball", "random pickoff" })
            Assert.DoesNotContain(EveryLine, l => l.Contains(retired, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(RoleTables.Pad.SelectMany(b => b.Rows),
            r => r.Verb.Contains("Lead", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(HowToPlay.Must("running").Lines, l => l.Contains("there is no lead"));
        // PH-02-R5: the verb that replaced the West changeup is on the pitching spread, and the
        // retired modifier is gone from every spread.
        Assert.Contains(EveryLine, l => l.Contains("Cycle pitch"));
        Assert.DoesNotContain(RoleTables.Pad.SelectMany(b => b.Rows),
            r => r.Verb.Equals("Changeup", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RunningSpreadHasPerRunnerVerbsAndImmediateStealWithHome()
    {
        var running = RoleTables.Pad.First(b => b.Id == "running");
        Row(running, "Select runner");
        Row(running, "Send");
        var halt = Row(running, "Halt");
        Assert.Contains("D-pad Up", halt.Press);
        var steal = Row(running, "Steal");
        Assert.Contains("LB", steal.Press);
        Assert.DoesNotContain("L3", steal.Press);
        Assert.Contains("home", steal.Press);
        Assert.Contains("NOW", steal.Press);
        var close = Row(running, "Close play");
        Assert.Contains("South", close.Press);
        Assert.Contains("icon", close.Press);
        Row(running, "Rundown");
        Row(running, "Dash");
        Assert.DoesNotContain(running.Rows, r => r.Verb == "Tag");
        // The running page itself: the steal of home, the perfect steal, the per-runner send and return, tag-and-go.
        var page = HowToPlay.Must("running").Lines;
        Assert.Contains(page, l => l.Contains("D-pad Down"));
        Assert.Contains(page, l => l.Contains("before contact"));
        Assert.DoesNotContain(page, l => l.Contains("perfect steal"));
        Assert.Contains(page, l => l.Contains("RB returns"));
        Assert.Contains(page, l => l.Contains("tag and go"));
        Assert.Contains(page, l => l.Contains("CAUGHT STEALING") && l.Contains("STOLEN BASE"));
    }

    [Fact]
    public void FieldingSpreadHasThrowCutoffRelayTagAndRundown()
    {
        var fielding = RoleTables.Pad.First(b => b.Id == "fielding");
        var throwRow = Row(fielding, "Throw");
        Assert.Contains("Right stick", throwRow.Press);
        var cutoff = Row(fielding, "Cutoff / relay");
        Assert.Contains("RB", cutoff.Press);
        Assert.Contains("RT sends", cutoff.Press);
        var tag = Row(fielding, "Tag");
        Assert.Contains("touch runner", tag.Press);
        var rundown = Row(fielding, "Rundown");
        Assert.Contains("throw ahead", rundown.Press);
        Row(fielding, "Jump");
        Row(fielding, "Dive");
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("close play") && l.Contains("bag"));
        Assert.Contains(HowToPlay.Must("fielding").Lines, l => l.Contains("liner") && l.Contains("fly"));
    }

    [Fact]
    public void PitchingSpreadHasTheCycleBreakTheVisibleSwapAndThePickoffRule()
    {
        var pitching = RoleTables.Pad.First(b => b.Id == "pitching");
        // PH-02-R5 (#825): the pre-charge family cycle replaces the West changeup row.
        var cycle = Row(pitching, "Cycle pitch");
        Assert.Contains("West", cycle.Press);
        Assert.Contains("charge", cycle.Press);
        Row(pitching, "Break");
        var swap = Row(pitching, "Swap pitcher");
        Assert.Contains("Arrange defense", swap.Press);
        var pickoff = Row(pitching, "Pickoff");
        Assert.Contains("right stick + RT", pickoff.Press);
        Assert.Contains("BALK", pickoff.Press);
        Assert.Contains(HowToPlay.Must("the-box").Lines, l => l.Contains("pickoff") && l.Contains("on the bag is safe"));
    }

    [Fact]
    public void ClosePlayCalloutIsThirdAndHomeInsideTheMargin()
    {
        Assert.Contains("3rd or home only", BagDiagrams.ClosePlay.Line);
        Assert.Contains("bang-bang", BagDiagrams.ClosePlay.Line);
        Assert.Contains(HowToPlay.Must("running").Lines, l => l.Contains("Close play") && l.Contains("pictures below"));
        Assert.Contains("FIRST SOUTH", BagDiagrams.ClosePlay.Press);
        Assert.Contains(HowToPlay.Must("running").Lines, l => l.Contains("SAFE pops small"));
    }

    [Fact]
    public void InPlayHudMapNamesTheYouHandOffAndTheErrorTell()
    {
        var you = HudCallouts.InPlay.Marks.First(m => m.Id == "you");
        Assert.Contains("receiver", you.Label);
        var error = HudCallouts.InPlay.Marks.First(m => m.Id == "error");
        Assert.Contains(PlayStamp.Error, error.Label);
        Assert.Equal(BroadcastHud.StampDirt, error.Anchor);
        Assert.Equal(BroadcastHud.YouTell, you.Anchor);
    }

    [Fact]
    public void EveryRoleTablePageIsABookPageWithAChapterCaptain()
    {
        foreach (var id in RoleTables.PageIds)
        {
            Assert.Equal(id, HowToPlay.Must(id).Id);
            Assert.True(BookChapter.Captains.ContainsKey(id), id);
        }
        Assert.True(BookChapter.EveryPageHasARosterCaptain());
        foreach (var block in RoleTables.Pad)
        foreach (var row in block.Rows)
            Assert.False(HowToPlay.NamesKeyboard(row.Press), block.Id + " " + row.Verb);
    }

    static RoleTables.Row Row(RoleTables.Block block, string verb)
    {
        var row = block.Rows.FirstOrDefault(r => r.Verb.Equals(verb, StringComparison.OrdinalIgnoreCase));
        Assert.True(row != null, block.Id + " has no row " + verb);
        return row!;
    }
}
