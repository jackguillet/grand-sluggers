using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

[Trait("Rows", "compact")]
public class NightTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

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

    [Fact]
    public void CrystalNightShrinksTheContactWindow()
    {
        var park = _content.Parks["crystal-rink"];
        Assert.Equal(1.0, ParkHazards.ContactWindowMul(park, false));
        Assert.Equal(park.NightContactWindowMul, ParkHazards.ContactWindowMul(park, true));
        Assert.Equal(0.85, park.NightContactWindowMul);
        Assert.Equal(1.0, ParkHazards.ContactWindowMul(_content.Parks["harbor-diamond"], true));

        var rio = _content.Must("rio");
        var dayWindow = AtBatResolver.ContactWindowFrames(null, park, false);
        var nightWindow = AtBatResolver.ContactWindowFrames(null, park, true);
        Assert.True(nightWindow < dayWindow);
        var input = new AtBatInput(
            _content.Must("ashlord"), rio, _content.Must("nico"), [],
            false, false, (dayWindow + nightWindow) / 4, false, false,
            _content.Bats["harbor-lumber"], 80, PitchInZone: true);
        var resolver = new AtBatResolver(_content.Chemistry);
        var day = resolver.Resolve(input, park, new Random(1));
        var night = resolver.Resolve(input, park, new Random(1), night: true);
        Assert.NotEqual(ContactQuality.Miss, day.Quality);
        Assert.Equal(ContactQuality.Miss, night.Quality);
        Assert.True(day.InPlay);
        Assert.False(night.InPlay);
    }

    [Fact]
    public void FunfairNightChompersEatOutfieldFlies()
    {
        var park = _content.Parks["funfair-park"];
        // The mouths are park data since #847, so the C80 copy carries them at the field's scale:
        // the centre one is at (0, 228) r 18 shipped and (0, 160) r 12.60 on the trial.
        var mouthZ = TestRoot.Pick(228.0, 160.0);
        Assert.Equal(mouthZ, park.Hazards.Single(h => h.Type == HazardType.Chomper && h.Tag == "C").Z);
        Assert.False(ParkHazards.ChompFly(park, false, 0, mouthZ));
        Assert.True(ParkHazards.ChompFly(park, true, 0, mouthZ));
        Assert.False(ParkHazards.ChompFly(park, true, 0, 0));
        Assert.False(ParkHazards.ChompFly(park, true, 0, mouthZ, grounder: true));
        Assert.False(ParkHazards.ChompFly(_content.Parks["harbor-diamond"], true, 0, mouthZ));

        var hit = FlightFixtures.Landing(park, mouthZ, 22, 0);
        var spark = PresetTeams.SparkAllStars(_content);
        var fielding = new FieldingResolver(_content.Chemistry);
        var day = fielding.Resolve(hit, park, spark.Roster, spark.Captain, new Random(1));
        var night = fielding.Resolve(hit, park, spark.Roster, spark.Captain, new Random(1), night: true);
        Assert.False(day.Chomped);
        Assert.True(night.Chomped);
        Assert.Equal(PlayKind.FlyOut, night.Kind);
    }

    [Fact]
    public void EmberNightFireBreathReachesFarther()
    {
        var park = _content.Parks["ember-keep"];
        // The C80 copy carries the statue's breath at the field's scale (#732): (0, 175) with a 11.2 ft disc, so the same two
        // points are 175 ft and 189 ft out (20 ft past the mouth at 0.70).
        var (mouthZ, pastZ) = TestRoot.Pick((250.0, 270.0), (175.0, 189.0));
        Assert.True(ParkHazards.InSlow(park, 0, mouthZ));
        Assert.False(ParkHazards.InSlow(park, 0, pastZ));
        Assert.False(ParkHazards.InSlow(park, 0, pastZ, night: false));
        Assert.True(ParkHazards.InSlow(park, 0, pastZ, night: true));
        Assert.False(ParkHazards.InSlow(_content.Parks["harbor-diamond"], 0, pastZ, night: true));

        var lava = park.Hazards.First(h => h.Type == "lava_pit");
        Assert.False(ParkHazards.InSlow(park, lava.X, lava.Z + lava.Radius + 4));
        Assert.False(ParkHazards.InSlow(park, lava.X, lava.Z + lava.Radius + 4, night: true));
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
