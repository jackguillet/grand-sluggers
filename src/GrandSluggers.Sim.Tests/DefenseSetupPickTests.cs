using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public class DefenseSetupPickTests
{
    readonly ContentCatalog _content = Shipped.Content;
    static int At(string pos) => Array.IndexOf(Diamond.Order, pos);
    static Dictionary<string, Character> Map(Match m) => FieldingResolver.Assign(m.DefenseRoster, m.Pitcher, m.Defense.Gloves);

    [Fact]
    public void InspectAndFirstPickAndCancelNeverMutateTheDefense()
    {
        var match = Match.Slice(_content, seed: 1); var before = Map(match);
        var pick = new DefenseSetupPick(match);
        Assert.Equal(9, pick.Candidates.Count);
        for (var i = 0; i < 9; i++)
        {
            pick.Inspect(i);
            Assert.Equal(Diamond.Order[i], pick.Current.Pos);
            Assert.False(pick.PickOrSwap(match));
            Assert.Equal(Diamond.Order[i], pick.PickedPosition);
            Assert.Equal(before, Map(match));
            pick.CancelPick();
        }
        Assert.True(match.CanSwapPitcher);
    }

    [Fact]
    public void TwoPicksTradeFieldersAndRefreshCardsWithoutSpendingThePitcherChange()
    {
        var match = Match.Slice(_content, seed: 1); var before = Map(match);
        var pick = new DefenseSetupPick(match);
        pick.Inspect(At("C")); pick.PickOrSwap(match);
        pick.Inspect(At("RF")); Assert.True(pick.PickOrSwap(match));
        Assert.Null(pick.PickedPosition);
        Assert.Equal(before["C"], pick.Current.Who);
        Assert.Equal(before["RF"], Map(match)["C"]);
        Assert.True(match.CanSwapPitcher);
        Assert.Equal(Map(match)["RF"], new DefenseSetupPick(match).Candidates[At("RF")].Who);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void BothMoundGesturesUseTheOneAllowanceAndOtherGlovesRemainEditable(bool quick)
    {
        var match = Match.Slice(_content, seed: 1);
        var pick = new DefenseSetupPick(match); var next = Map(match)["SS"];
        if (!quick) { pick.Inspect(At("P")); pick.PickOrSwap(match); }
        pick.Inspect(At("SS"));
        Assert.True(quick ? pick.QuickPitcher(match) : pick.PickOrSwap(match));
        Assert.Equal(next, match.Pitcher);
        Assert.False(pick.QuickPitcher(match));
        pick.Inspect(At("C")); pick.PickOrSwap(match);
        pick.Inspect(At("RF")); Assert.True(pick.PickOrSwap(match));
    }

    [Fact]
    public void SpatialControlsReachEveryGloveFromEveryStartingPosition()
    {
        for (var start = 0; start < 9; start++)
        {
            var seen = new HashSet<int> { start }; var queue = new Queue<int>(); queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var at = queue.Dequeue();
                foreach (var dir in new[] { (1,0), (-1,0), (0,1), (0,-1) })
                {
                    var next = LineupLayout.NeighborPosition(at, dir.Item1, dir.Item2);
                    if (next >= 0 && seen.Add(next)) queue.Enqueue(next);
                }
            }
            Assert.Equal(9, seen.Count);
        }
    }

    [Fact]
    public void ARoutedTradeGoesThroughTheCallerAndAMoundTradeToo()
    {
        var match = Match.Slice(_content, seed: 1); var before = Map(match);
        var routed = new List<(string, string)>();
        bool Route(string a, string b) { routed.Add((a, b)); return match.SwapDefensePositions(a, b); }
        var pick = new DefenseSetupPick(match);
        pick.Inspect(At("SS")); pick.PickOrSwap(match, null, Route);
        pick.Inspect(At("CF")); Assert.True(pick.PickOrSwap(match, null, Route));
        Assert.Equal(before["SS"], Map(match)["CF"]);
        // Without a pitcher route, the quick swap to the mound is a routed position trade as well.
        pick.Inspect(At("LF")); Assert.True(pick.QuickPitcher(match, null, Route));
        Assert.Equal([("SS", "CF"), ("P", "LF")], routed);
        Assert.Equal(before["LF"], match.Pitcher);
        // A refused route leaves the window's cards as they were.
        var cards = pick.Candidates;
        pick.Inspect(At("C")); pick.PickOrSwap(match, null, (_, _) => false);
        pick.Inspect(At("RF")); Assert.False(pick.PickOrSwap(match, null, (_, _) => false));
        Assert.Same(cards, pick.Candidates);
    }
}
