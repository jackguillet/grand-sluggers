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
        // PH-02-R5 (#825): the CHANGE tell is gone. The card never names the selected family, so the
        // extras row carries only STAR and the swap pick, and the repertoire row below it is the
        // same for both seats.
        Assert.Equal("", BroadcastHud.PitcherExtra(false));
        Assert.Equal("STAR", BroadcastHud.PitcherExtra(true));
        Assert.Equal("Start → Arrange defense", BroadcastHud.PitcherExtra(false, null, canSwap: true));
        Assert.Equal("SWAP → SS Nugget", BroadcastHud.PitcherExtra(false, "SWAP → SS Nugget", canSwap: true));
        // The SET HUD map names the swap on the pitcher card (the mark cell is one measured line).
        Assert.Contains("SWAP", HudCallouts.Set.Marks.First(m => m.Id == "pitcher").Label);
    }

    [Fact]
    public void TheBookNamesTheCycleAndSwapOnThePitchingSpread()
    {
        // PH-02-R5 (#825): the Changeup row is replaced, not joined — MaxRows is 10 and the spread
        // is two couch-size pages of five. The changeup is now a family inside the cycle.
        {
            var pitching = RoleTables.Pad.First(b => b.Id == "pitching").Rows;
            Assert.Contains(pitching, r => r.Verb == "Cycle pitch");
            Assert.DoesNotContain(pitching, r => r.Verb == "Changeup");
            Assert.Contains(pitching, r => r.Verb == "Swap pitcher" && r.Press.Contains("Arrange defense"));
            Assert.Contains(pitching, r => r.Verb == "Break");
            Assert.DoesNotContain(pitching, r => r.Verb == "Curve");
            Assert.InRange(pitching.Count, RoleTables.MinRows, RoleTables.MaxRows);
        }
        var pad = RoleTables.Pad.First(b => b.Id == "pitching").Rows;
        Assert.Contains(pad, r => r.Verb == "Cycle pitch" && r.Press.Contains("West") && r.Press.Contains("Fastball"));
        Assert.Contains(pad, r => r.Verb == "Swap pitcher" && r.Press.Contains("Start"));
        Assert.Contains(pad, r => r.Verb == "Swap pitcher" && r.Press.StartsWith("Start"));
        Assert.Contains(HowToPlay.Must("the-box").Lines, l => l.Contains("any fielder"));
    }

    /// <summary>
    /// PH-02-R5 (#825): the card shows the pitcher's ordinary pitches in repertoire order, with no
    /// active mark — the row is a function of the arm and the table and of nothing the player is
    /// holding, so the shared screen cannot leak the selection.
    /// </summary>
    [Fact]
    public void ThePitcherCardListsTheRepertoireInOrderWithNoActiveMark()
    {
        var content = _content;
        var match = Match.Slice(content, seed: 1);
        var row = BroadcastHud.PitcherPitches(match);
        Assert.StartsWith("FB", row);
        Assert.DoesNotContain(row, "*[]<>".Contains);
        foreach (var family in match.Pitcher.Repertoire.Ordinary.Where(content.Rules.Pitching.Families.IsAuthored))
            Assert.Contains(BroadcastHud.ShortFamily(family), row);
        // #860 (Jack, September 22, 2026: "trial was good."): the shipped root authors all five
        // families, so the card names every ordinary pitch, in repertoire order.
        Assert.Equal("FB  ·  CH  ·  CU", BroadcastHud.PitcherPitches(
            new Repertoire(PitchFamily.Changeup, PitchFamily.Curveball), content.Rules.Pitching.Families));
        Assert.Equal("FB  ·  SL  ·  CU", BroadcastHud.PitcherPitches(
            new Repertoire(PitchFamily.Slider, PitchFamily.Curveball), content.Rules.Pitching.Families));
        // The off path: a table that authors only the two code-default rows skips the rest.
        var bare = RulesTable.Defaults.Pitching.Families;
        Assert.Equal("FB  ·  CH", BroadcastHud.PitcherPitches(
            new Repertoire(PitchFamily.Changeup, PitchFamily.Curveball), bare));
        Assert.Equal("FB", BroadcastHud.PitcherPitches(
            new Repertoire(PitchFamily.Slider, PitchFamily.Curveball), bare));
        Assert.Equal(["FB", "CH", "CU", "SL", "SI"],
            PitchFamily.All.Select(BroadcastHud.ShortFamily).ToArray());
    }
}
