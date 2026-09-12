using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>The SET swap pick (spec §4.7, #582): any fielder, a visible pick, the card updating.</summary>
public class PitcherSwapTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void ThePickWalksEveryGloveInOrderAndStartsOnTheBestArm()
    {
        var match = Match.Slice(_content, seed: 1);
        Assert.True(PitcherSwapPick.CanOpen(match));
        var pick = new PitcherSwapPick(match);
        Assert.Equal(8, pick.Candidates.Count);
        Assert.Equal(Diamond.Order.Where(p => p != "P"), pick.Candidates.Select(c => c.Pos));
        Assert.DoesNotContain(pick.Candidates, c => c.Who.Id == match.Pitcher.Id);
        Assert.Equal(pick.Candidates.Max(c => c.Who.Stats.Pitch), pick.Current.Who.Stats.Pitch);
        Assert.StartsWith("SWAP → " + pick.Current.Pos + " " + pick.Current.Who.Name, pick.Tell);

        var start = pick.Index;
        var seen = new List<string>();
        for (var i = 0; i < pick.Candidates.Count; i++)
            seen.Add(pick.Step(1).Pos);
        Assert.Equal(start, pick.Index);
        Assert.Equal(pick.Candidates.Select(c => c.Pos).OrderBy(p => p), seen.OrderBy(p => p));
        pick.Step(-1);
        Assert.Equal((start - 1 + 8) % 8, pick.Index);
    }

    [Fact]
    public void ConfirmPutsThePickOnTheMoundAndTheOldPitcherOnTheVacatedGlove()
    {
        var match = Match.Slice(_content, seed: 1);
        var old = match.Pitcher;
        var pick = new PitcherSwapPick(match);
        pick.Step(3);
        var chosen = pick.Current;
        Assert.True(pick.Confirm(match));
        Assert.Equal(chosen.Who.Id, match.Pitcher.Id);
        var gloves = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher);
        Assert.Equal(old.Id, gloves[chosen.Pos].Id);
        Assert.False(PitcherSwapPick.CanOpen(match), "once per half-inning");
    }

    [Fact]
    public void ThePitcherCardNamesTheVerbsTheWayTheBatterCardDoes()
    {
        Assert.Equal("", BroadcastHud.PitcherExtra(false, false));
        Assert.Equal("CHANGE", BroadcastHud.PitcherExtra(false, true));
        Assert.Equal("STAR  CHANGE", BroadcastHud.PitcherExtra(true, true));
        Assert.Equal("Select SWAP", BroadcastHud.PitcherExtra(false, false, null, canSwap: true));
        Assert.Equal("SWAP → SS Nugget  ·  Select", BroadcastHud.PitcherExtra(false, false, "SWAP → SS Nugget", canSwap: true));
        // The SET HUD map names the swap on the pitcher card (the mark cell is one measured line).
        Assert.Contains("SWAP", HudCallouts.Set.Marks.First(m => m.Id == "pitcher").Label);
    }

    [Fact]
    public void TheBookNamesChangeupAndSwapOnThePitchingSpreadInBothSchemes()
    {
        foreach (var scheme in new[] { RoleTables.Pad, RoleTables.Keys })
        {
            var pitching = scheme.First(b => b.Id == "pitching").Rows;
            Assert.Contains(pitching, r => r.Verb == "Changeup");
            Assert.Contains(pitching, r => r.Verb == "Swap pitcher" && r.Press.Contains("any fielder"));
            Assert.Contains(pitching, r => r.Verb == "Break");
            Assert.DoesNotContain(pitching, r => r.Verb == "Curve");
        }
        var pad = RoleTables.Pad.First(b => b.Id == "pitching").Rows;
        Assert.Contains(pad, r => r.Verb == "Changeup" && r.Press.Contains("West"));
        Assert.Contains(pad, r => r.Verb == "Swap pitcher" && r.Press.Contains("Select"));
        var keys = RoleTables.Keys.First(b => b.Id == "pitching").Rows;
        Assert.Contains(keys, r => r.Verb == "Swap pitcher" && r.Press.StartsWith("R"));
        Assert.Contains(HowToPlay.Must("the-box").Lines, l => l.Contains("any fielder"));
    }
}
