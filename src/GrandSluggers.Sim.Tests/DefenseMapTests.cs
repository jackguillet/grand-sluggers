using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// <see cref="Match.DefenseMap"/> (#1049): the defense by position, built once and rebuilt only when it can change, so the
/// HUD and the actor tick that ask every frame allocate nothing — and never a stale map.
/// </summary>
public sealed class DefenseMapTests
{
    static void SameAsAssign(Match match)
    {
        var fresh = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        Assert.Equal(fresh.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value.Id)),
            match.DefenseMap.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value.Id)));
    }

    [Fact]
    public void RepeatedReadsShareOneMap()
    {
        var match = Match.Slice(Shipped.Content, seed: 3);
        Assert.Same(match.DefenseMap, match.DefenseMap);
        SameAsAssign(match);
    }

    [Fact]
    public void APitcherSwapAPositionSwapAndTheHalfEachRebuildIt()
    {
        var match = Match.Slice(Shipped.Content, seed: 3);
        var before = match.DefenseMap;
        Assert.True(match.SwapDefensePositions("LF", "RF"));
        Assert.NotSame(before, match.DefenseMap);
        SameAsAssign(match);

        var afterSwap = match.DefenseMap;
        Assert.True(match.SwapPitcher());
        Assert.NotSame(afterSwap, match.DefenseMap);
        SameAsAssign(match);

        var top = match.DefenseMap;
        match.SkipToHomeHalf();
        Assert.NotSame(top, match.DefenseMap);
        SameAsAssign(match);
    }

    [Fact]
    public void ThroughAWholeGameTheMapIsTheAssignmentOfTheMoment()
    {
        var match = Match.Slice(Shipped.Content, seed: 9);
        for (var i = 0; i < 400 && !match.Over; i++)
        {
            match.AutoPlay();
            if (!match.Over) SameAsAssign(match);
        }
    }
}
