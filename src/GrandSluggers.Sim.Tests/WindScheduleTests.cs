using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// A park's wind schedule (§6.1; Summit Park's mountain gusts, #1155, WD-09 C). Before each inning's first pitch the wind turns
/// to a speed in the band and any bearing, drawn from the match's own wind stream; stronger at night; never inside an inning.
/// A park without a schedule draws nothing and keeps its fixed wind.
/// </summary>
public sealed class WindScheduleTests
{
    static readonly ContentCatalog Game = Shipped.Content;

    static Match New(string park, int seed, bool night = false) =>
        Match.Exhibition(Game, "hollis", "rio", innings: 9, seed: seed, parkId: park, night: night);

    /// <summary>Play CPU games to the end, reading the park's wind at every pitch: (inning, top, mph, deg).</summary>
    static List<(int Inning, bool Top, double Mph, double Deg)> Winds(Match match)
    {
        var seen = new List<(int, bool, double, double)>();
        for (var i = 0; i < 4000 && !match.Over; i++)
        {
            seen.Add((match.Inning, match.Top, match.Park.WindMph, match.Park.WindDeg));
            match.AutoPlay();
        }
        return seen;
    }

    [Fact]
    public void SummitNamesItsGustsAndItsThinAir()
    {
        var env = Game.MustPark(ParkId.Summit).Environment!;
        Assert.Equal(new WindSchedule(3, 14, 1.4), env.WindSchedule);
        Assert.Equal(0.9, env.DragMul);
        Assert.All(Game.Parks.Values.Where(p => p.Id != ParkId.Summit), p => Assert.Null(p.Environment?.WindSchedule));
    }

    [Fact]
    public void TheWindTurnsBeforeEachInningAndHoldsThroughIt()
    {
        var winds = Winds(New(ParkId.Summit, 7));
        var schedule = Game.MustPark(ParkId.Summit).Environment!.WindSchedule!;
        foreach (var inning in winds.GroupBy(w => w.Inning))
        {
            Assert.Single(inning.Select(w => (w.Mph, w.Deg)).Distinct());   // one wind, top and bottom, every pitch
            var (mph, deg) = (inning.First().Mph, inning.First().Deg);
            Assert.InRange(mph, schedule.MinMph, schedule.MaxMph);
            Assert.InRange(deg, -180, 180);
        }
        Assert.True(winds.Select(w => (w.Mph, w.Deg)).Distinct().Count() >= 5, "the wind turns from inning to inning");
    }

    [Fact]
    public void TheScheduleIsTheSameForTheSameSeedAndStrongerAtNight()
    {
        Assert.Equal(Winds(New(ParkId.Summit, 11)), Winds(New(ParkId.Summit, 11)));
        var first = New(ParkId.Summit, 11).Park;
        Assert.NotEqual((first.WindMph, first.WindDeg), (New(ParkId.Summit, 12).Park.WindMph, New(ParkId.Summit, 12).Park.WindDeg));
        // The same draws at night: the bearing is the day's and the speed is 1.4 times it.
        for (var seed = 1; seed <= 6; seed++)
        {
            var day = New(ParkId.Summit, seed).Park;
            var night = New(ParkId.Summit, seed, night: true).Park;
            Assert.Equal(day.WindDeg, night.WindDeg);
            Assert.InRange(night.WindMph, day.WindMph * 1.4 - 0.1, day.WindMph * 1.4 + 0.1);
        }
    }

    [Fact]
    public void AParkWithoutAScheduleKeepsItsFixedWindAndItsOwnParkObject()
    {
        var match = New(ParkId.Harbor, 7);
        var park = match.Park;
        var winds = Winds(match);
        Assert.All(winds, w => Assert.Equal((park.WindMph, park.WindDeg), (w.Mph, w.Deg)));
        Assert.Same(park, match.Park);
        Assert.Equal("", BroadcastHud.From(match).Wind);
    }

    [Theory]
    [InlineData(0, "out to center")]
    [InlineData(45, "out to right")]
    [InlineData(-50, "out to left")]
    [InlineData(90, "across to right")]
    [InlineData(-100, "across to left")]
    [InlineData(135, "in from left")]
    [InlineData(-140, "in from right")]
    [InlineData(180, "in from center")]
    public void TheWindReadsInTheFieldsOwnWords(double deg, string way)
    {
        var park = Game.MustPark(ParkId.Harbor) with { WindMph = 12, WindDeg = deg };
        Assert.Equal($"Wind 12 mph, {way}.", CarnivalFront.WindLine(park));
    }

    [Fact]
    public void TheCardNamesTheGustsAndTheBugShowsTheInningsWind()
    {
        var match = New(ParkId.Summit, 3);
        Assert.Contains(CarnivalFront.FieldCard(match.Park, match.Rules), l => l.StartsWith("Mountain gusts: the wind turns every inning, 3–14 mph", StringComparison.Ordinal)
            && l.EndsWith("stronger at night.", StringComparison.Ordinal));
        Assert.Equal(CarnivalFront.WindLine(match.Park).TrimEnd('.').ToUpperInvariant(), BroadcastHud.From(match).Wind);
    }

    [Fact]
    public void ABadScheduleIsRefusedByName()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("parks/summit-park.json", json =>
        {
            json["environment"]!["windSchedule"]!["minMph"] = 20;
            json["environment"]!["windSchedule"]!["maxMph"] = 10;
            json["environment"]!["windSchedule"]!["nightMul"] = 0.5;
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("environment.windSchedule needs 0 <= minMph <= maxMph", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("environment.windSchedule.nightMul must be between 1 and 2", StringComparison.Ordinal));
    }
}
