using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The sim's owned generator (xoshiro256** through SplitMix64) and the match's named streams. The pinned outputs come
/// from an independent implementation; a change to the generator changes every game and must fail here first.
/// </summary>
public sealed class SimRandomTests
{
    [Fact]
    public void TheGeneratorDrawsThePublishedSequence()
    {
        var rng = new SimRandom(0);
        Assert.Equal(0x99ec5f36cb75f2b4UL, rng.NextULong());
        Assert.Equal(0xbf6e1f784956452aUL, rng.NextULong());
        Assert.Equal(0x1a5f849d4933e6e0UL, rng.NextULong());
    }

    [Fact]
    public void ANamedStreamIsItsSeedAndName()
    {
        Assert.Equal(0.058090042864363145, SimRandom.Stream(7, "contact").NextDouble(), 15);
        Assert.Equal(SimRandom.Stream(7, "contact").NextULong(), SimRandom.Stream(7, "contact").NextULong());
        Assert.NotEqual(SimRandom.Stream(7, "contact").NextULong(), SimRandom.Stream(7, "handling").NextULong());
        Assert.NotEqual(SimRandom.Stream(7, "contact").NextULong(), SimRandom.Stream(8, "contact").NextULong());
    }

    [Fact]
    public void DrawsStayInTheirRanges()
    {
        var rng = new SimRandom(42);
        for (var i = 0; i < 10_000; i++)
        {
            var d = rng.NextDouble();
            Assert.InRange(d, 0.0, 0.9999999999999999);
            Assert.InRange(rng.Next(3), 0, 2);
            Assert.InRange(rng.Next(-5, 5), -5, 4);
            Assert.InRange(rng.Next(), 0, int.MaxValue - 1);
        }
        Assert.Equal(0, rng.Next(0));
        Assert.Equal(4, rng.Next(4, 4));
    }

    [Fact]
    public void EveryIndexOfASmallRangeComesUp()
    {
        var rng = new SimRandom(3);
        var seen = new int[7];
        for (var i = 0; i < 7_000; i++) seen[rng.Next(7)]++;
        Assert.All(seen, n => Assert.InRange(n, 850, 1150));
    }

    [Fact]
    public void ADrawOnOneStreamLeavesTheOthersWhereTheyWere()
    {
        // The point of named streams: one more CPU roll does not move contact, handling or hazard draws.
        var quiet = new MatchStreams(11);
        var busy = new MatchStreams(11);
        for (var i = 0; i < 50; i++) busy.PitchAi.NextDouble();
        Assert.Equal(quiet.Contact.NextULong(), busy.Contact.NextULong());
        Assert.Equal(quiet.Handling.NextULong(), busy.Handling.NextULong());
        Assert.Equal(quiet.Hazard.NextULong(), busy.Hazard.NextULong());
        Assert.Equal(quiet.BatAi.NextULong(), busy.BatAi.NextULong());
    }

    [Fact]
    public void ASeedStillReplaysAWholeGame()
    {
        var a = Match.Slice(Shipped.Content, seed: 21);
        var b = Match.Slice(Shipped.Content, seed: 21);
        a.AutoPlayGame();
        b.AutoPlayGame();
        Assert.Equal(a.Log.Select(e => e.Caption), b.Log.Select(e => e.Caption));
        Assert.Equal((a.AwayScore, a.HomeScore), (b.AwayScore, b.HomeScore));
    }
}
