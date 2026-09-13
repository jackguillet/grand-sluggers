using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The book follows the rules (spec §15, #570): the verbs P3–P6 retired are gone from every spread,
/// and the verbs they landed sit on the spread a stranger would open for them.
/// </summary>
public class HowToPlayTests
{
    static IEnumerable<string> EveryLine =>
        HowToPlay.Pages.SelectMany(p => p.Lines.Concat(p.KeyLines ?? []).Append(p.Title))
            .Concat(RoleTables.Pad.Concat(RoleTables.Keys).SelectMany(b => b.Rows).SelectMany(r => new[] { r.Verb, r.Press }))
            .Concat(BagDiagrams.Callouts.SelectMany(c => new[] { c.Title, c.PadPress, c.KeysPress, c.Line }))
            .Concat(BagDiagrams.Running.SelectMany(d => new[] { d.Title, d.PadPress, d.KeysPress }))
            .Concat(HudCallouts.OnScreenPage.SelectMany(s => s.Marks).Select(m => m.Label));

    [Fact]
    public void RetiredVerbsAreGoneFromEverySpread()
    {
        // D1 / P3: no leads, no lead stick, no lead pips; #563: no pitch-type cycle; D3: no random pickoff.
        foreach (var retired in new[] { "take a lead", "lead off", "lead-off", "Lead01", "lead pip", "walking lead", "cycle pitch", "cycle fastball", "random pickoff" })
            Assert.DoesNotContain(EveryLine, l => l.Contains(retired, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(RoleTables.Pad.Concat(RoleTables.Keys).SelectMany(b => b.Rows),
            r => r.Verb.Contains("Lead", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(HowToPlay.Must("running").Lines, l => l.Contains("there is no lead"));
    }

    [Theory]
    [InlineData(InputScheme.Pad)]
    [InlineData(InputScheme.Keys)]
    public void RunningSpreadHasPerRunnerVerbsAndTheStealArmWithHome(InputScheme scheme)
    {
        var running = RoleTables.Of(scheme).First(b => b.Id == "running");
        Row(running, "Select runner");
        Row(running, "Send");
        var halt = Row(running, "Halt");
        Assert.Contains("that runner only", halt.Press);
        var steal = Row(running, "Steal");
        Assert.Contains(scheme == InputScheme.Pad ? "L3" : "Z", steal.Press);
        Assert.Contains(scheme == InputScheme.Pad ? "Stick to the bag" : "WASD to the bag", steal.Press);
        Assert.Contains("Home", steal.Press);
        Assert.Contains("SET", steal.Press);
        var close = Row(running, "Close play");
        Assert.Contains("third", close.Press);
        Assert.Contains("home", close.Press);
        Row(running, "Rundown");
        Row(running, "Dash");
        Assert.DoesNotContain(running.Rows, r => r.Verb == "Tag");
        // The running page itself: the steal of home, the perfect steal, the per-runner send and return, tag-and-go.
        var page = HowToPlay.Must("running").Shown(scheme);
        Assert.Contains(page, l => l.Contains("home included"));
        Assert.Contains(page, l => l.Contains("perfect steal"));
        Assert.Contains(page, l => l.Contains("sends the selected runner") && l.Contains("returns them"));
        Assert.Contains(page, l => l.Contains("tag and go"));
        Assert.Contains(page, l => l.Contains("CAUGHT STEALING") && l.Contains("STOLEN BASE"));
    }

    [Theory]
    [InlineData(InputScheme.Pad)]
    [InlineData(InputScheme.Keys)]
    public void FieldingSpreadHasThrowCutoffRelayTagAndRundown(InputScheme scheme)
    {
        var fielding = RoleTables.Of(scheme).First(b => b.Id == "fielding");
        var throwRow = Row(fielding, "Throw");
        Assert.Contains("glove at that bag", throwRow.Press);
        var cutoff = Row(fielding, "Cutoff / relay");
        Assert.Contains(scheme == InputScheme.Pad ? "LB" : "X", cutoff.Press);
        Assert.Contains("sends it on", cutoff.Press);
        var tag = Row(fielding, "Tag");
        Assert.Contains("off a bag", tag.Press);
        var rundown = Row(fielding, "Rundown");
        Assert.Contains("covered bag", rundown.Press);
        Row(fielding, "Jump");
        Row(fielding, "Dive");
        Assert.Contains(HowToPlay.Must("fielding").Shown(scheme), l => l.Contains("close play") && l.Contains("bag"));
    }

    [Theory]
    [InlineData(InputScheme.Pad)]
    [InlineData(InputScheme.Keys)]
    public void PitchingSpreadHasChangeupBreakTheVisibleSwapAndThePickoffRule(InputScheme scheme)
    {
        var pitching = RoleTables.Of(scheme).First(b => b.Id == "pitching");
        Row(pitching, "Changeup");
        Row(pitching, "Break");
        var swap = Row(pitching, "Swap pitcher");
        Assert.Contains("any fielder", swap.Press);
        var pickoff = Row(pitching, "Pickoff");
        Assert.Contains("On the bag is safe", pickoff.Press);
        Assert.Contains("SET", pickoff.Press);
        Assert.Contains(HowToPlay.Must("the-box").Shown(scheme), l => l.Contains("pickoff") && l.Contains("on the bag is safe"));
    }

    [Fact]
    public void ClosePlayCalloutIsThirdAndHomeInsideTheMargin()
    {
        Assert.Contains("3rd or home only", BagDiagrams.ClosePlay.Line);
        Assert.Contains("bang-bang", BagDiagrams.ClosePlay.Line);
        Assert.Contains(HowToPlay.Must("running").Lines, l => l.Contains("Close play") && l.Contains("pictures below"));
        Assert.Contains("FIRST SOUTH", BagDiagrams.ClosePlay.PadPress);
        Assert.Contains("SPACE", BagDiagrams.ClosePlay.KeysPress);
        Assert.Contains(HowToPlay.Must("running").Lines, l => l.Contains("SAFE pops small"));
    }

    [Fact]
    public void InPlayHudMapNamesTheYouHandOffAndTheErrorTell()
    {
        var you = HudCallouts.InPlay.Marks.First(m => m.Id == "you");
        Assert.Contains("receiver", you.Label);
        var error = HudCallouts.InPlay.Marks.First(m => m.Id == "error");
        Assert.Contains(PlayStamp.Error, error.Label);
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
        foreach (var scheme in new[] { InputScheme.Pad, InputScheme.Keys })
        foreach (var block in RoleTables.Of(scheme))
        foreach (var row in block.Rows)
            Assert.False(HowToPlay.MixesHardware(row.Press), block.Id + " " + row.Verb);
    }

    static RoleTables.Row Row(RoleTables.Block block, string verb)
    {
        var row = block.Rows.FirstOrDefault(r => r.Verb.Equals(verb, StringComparison.OrdinalIgnoreCase));
        Assert.True(row != null, block.Id + " has no row " + verb);
        return row!;
    }
}
