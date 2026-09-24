using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// What night does (§0.3, §14; FD-11 B, FD-11-R2, F4-d #895). Night keeps the stadium lights, so it
/// changes only the view outside the stadium and the hazards: Crystal's contact window is dropped on both
/// roots and night at the rink is the day's at-bat; Funfair's chompers are its night block, played through
/// the one resolution (<see cref="PlayedPark.Of"/>); Ember's breath still reaches farther at night, because
/// that is its hazard type's own night number. Re-authored to FD-11-R2 from the three night rules that
/// hung off <c>Match.Night</c> in three shapes; the night block's own rows are <see cref="NightBlockTests"/>.
/// </summary>
public class NightTests
{
    readonly ContentCatalog _content = Shipped.Content;

    [Fact]
    public void SliceAndExhibitionDefaultToDay()
    {
        var slice = Match.Slice(_content, seed: 1);
        Assert.False(slice.Night);
        var show = Match.Exhibition(_content, "vale", "brondo", seed: 7);
        Assert.False(show.Night);
        Assert.Equal("crystal-rink", show.Park.Id);
    }

    [Fact]
    public void NightIsStoredOnTheMatch()
    {
        var harbor = Match.Slice(_content, seed: 7, night: true);
        Assert.True(harbor.Night);
        Assert.Equal("harbor-diamond", harbor.Park.Id);
        var crystal = Match.Exhibition(_content, "vale", "brondo", seed: 7, parkId: "crystal-rink", night: true);
        Assert.True(crystal.Night);
        Assert.Equal("crystal-rink", crystal.Park.Id);
    }

    [Fact]
    public void HarborNightPlayMatchesDayAtTheSameSeed()
    {
        var day = Match.Slice(_content, innings: 3, seed: 7);
        var night = Match.Slice(_content, innings: 3, seed: 7, night: true);
        Assert.False(day.Night);
        Assert.True(night.Night);
        day.AutoPlayGame();
        night.AutoPlayGame();
        Assert.Equal(day.AwayScore, night.AwayScore);
        Assert.Equal(day.HomeScore, night.HomeScore);
        Assert.Equal(day.Log.Select(e => e.Kind).ToList(), night.Log.Select(e => e.Kind).ToList());
    }

    /// <summary>
    /// Re-authored to FD-11-R2 (Jack, September 22, 2026: "for night time, we will still have stadium
    /// lights"; F4-d, #895). This row held Crystal's night window: × 0.85 on the at-bat, so a timing error
    /// between the two windows' edges was a hit by day and a miss at night. The window is dropped on both
    /// roots, so the same swing at the same error is the same contact by day and at night, and the rink's
    /// window is Harbor's. The fixture keeps the old edge: an error the × 0.85 night window would have missed.
    /// </summary>
    [Fact]
    public void CrystalNightPlaysTheDayWindow()
    {
        var park = _content.Parks["crystal-rink"];
        var rio = _content.Must("rio");
        var dayWindow = AtBatResolver.ContactWindowFrames(null, park, false, rules: Rules.Default);
        var nightWindow = AtBatResolver.ContactWindowFrames(null, park, true, rules: Rules.Default);
        Assert.Equal(dayWindow, nightWindow);
        Assert.Equal(AtBatResolver.ContactWindowFrames(null, _content.Parks["harbor-diamond"], true, rules: Rules.Default), nightWindow);

        // Between the day window's edge and the edge the dropped × 0.85 would have drawn.
        var removedNightWindow = dayWindow * 0.85;
        var input = new AtBatInput(
            _content.Must("ashlord"), rio, _content.Must("nico"), [],
            false, false, (dayWindow + removedNightWindow) / 4, false, false,
            _content.Bats["harbor-lumber"], 80, PitchInZone: true);
        var resolver = new AtBatResolver(_content.Chemistry, rules: Rules.Default);
        var day = resolver.Resolve(input, park, new Random(1));
        var night = resolver.Resolve(input, park, new Random(1), night: true);
        Assert.NotEqual(ContactQuality.Miss, day.Quality);
        Assert.Equal(day, night);
        Assert.True(night.InPlay);
    }

    /// <summary>
    /// Funfair's mouths take a fly on its way down at night and spit it out of another (FD-09-R2, F4-c). They are park
    /// data since #847 and Funfair's night block since F4-d (FD-11): the park a night match plays holds them after the day's
    /// instances, the park a day match plays does not, and nothing reads the clock. The centre one is at (0, 160) r 12.60.
    /// A mouth is a redirect, not an out: the preview plans the fly as hit and nobody is out by a mouth.
    /// </summary>
    [Fact]
    public void FunfairNightChompersTakeFliesOnlyAtNight()
    {
        var catalog = _content.Parks["funfair-park"];
        var byDay = Match.Exhibition(_content, "vale", "brondo", seed: 7, parkId: "funfair-park").Park;
        var atNight = Match.Exhibition(_content, "vale", "brondo", seed: 7, parkId: "funfair-park", night: true).Park;
        var mouthZ = 160.0;
        Assert.DoesNotContain(catalog.Hazards, h => h.Type == HazardType.Chomper);
        Assert.DoesNotContain(byDay.Hazards, h => h.Type == HazardType.Chomper);
        Assert.Equal(mouthZ, atNight.Hazards.Single(h => h.Type == HazardType.Chomper && h.Tag == "C").Z);
        bool Mouth(Park park, double y) { var b = new BallHazards(); b.Begin(park, false, _content.Rules); return b.Entered(0, y, mouthZ) is { Type: HazardType.Chomper }; }
        Assert.False(Mouth(byDay, 6));
        Assert.True(Mouth(atNight, 6));
        Assert.False(Mouth(atNight, 20), "above the mouth");
        Assert.False(Mouth(Match.Exhibition(_content, "vale", "brondo", seed: 7, parkId: "harbor-diamond", night: true).Park, 6));

        // The same fly is the same preview by day and at night: nothing is decided from where it lands.
        var hit = FlightFixtures.Landing(catalog, mouthZ, 22, 0);
        var spark = PresetTeams.SparkAllStars(_content);
        var fielding = new FieldingResolver(_content.Chemistry, rules: Rules.Default);
        var day = fielding.Resolve(hit, byDay, spark.Roster, spark.Captain, new Random(1));
        var night = fielding.Resolve(hit, atNight, spark.Roster, spark.Captain, new Random(1), night: true);
        Assert.Equal(day.Kind, night.Kind);
        Assert.NotEqual(PlayKind.FlyOut, night.Kind);
    }

    /// <summary>
    /// Ember's breath reaches farther at night: its hazard type's own night number
    /// (<c>fireBreath.nightRadiusMul</c> 1.6), which FD-11-R2 keeps — night changes the hazards. Unchanged by
    /// F4-d: the breath is a day instance, so it stands by day and at night, and only its disc widens.
    /// </summary>
    [Fact]
    public void EmberNightFireBreathReachesFarther()
    {
        var park = _content.Parks["ember-keep"];
        // The C80 copy carries the statue's breath at the field's scale (#732): (0, 175) with a 11.2 ft disc, so the same two
        // points are 175 ft and 189 ft out (20 ft past the mouth at 0.70).
        var (mouthZ, pastZ) = (175.0, 189.0);
        Assert.True(ParkHazards.InSlow(park, 0, mouthZ, rules: Rules.Default));
        Assert.False(ParkHazards.InSlow(park, 0, pastZ, rules: Rules.Default));
        Assert.False(ParkHazards.InSlow(park, 0, pastZ, night: false, rules: Rules.Default));
        Assert.True(ParkHazards.InSlow(park, 0, pastZ, night: true, rules: Rules.Default));
        Assert.False(ParkHazards.InSlow(_content.Parks["harbor-diamond"], 0, pastZ, night: true, rules: Rules.Default));

        var lava = park.Hazards.First(h => h.Type == "lava_pit");
        Assert.False(ParkHazards.InSlow(park, lava.X, lava.Z + lava.Radius + 4, rules: Rules.Default));
        Assert.False(ParkHazards.InSlow(park, lava.X, lava.Z + lava.Radius + 4, night: true, rules: Rules.Default));
    }

    [Fact]
    public void NightGamesFinishOnTheRuleParks()
    {
        foreach (var id in new[] { "harbor-diamond", "crystal-rink", "funfair-park", "ember-keep" })
        {
            var match = Match.Exhibition(_content, "vale", "brondo", innings: 3, seed: 7, parkId: id, night: true);
            Assert.True(match.Night, id);
            match.AutoPlayGame();
            Assert.True(match.Over, id);
            Assert.True(match.Log.Count > 8, id);
        }
    }
}
