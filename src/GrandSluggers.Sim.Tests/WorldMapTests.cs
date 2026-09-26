using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The continent the parks stand on (WD-05, WD-19, WD-20; data/world/regions.json): the Grand Reach, one land and one
/// island. Every park names one region, no two parks share one, every region sits on the unit map, and the park players
/// used to call Crystal Rink reads Aurora Rink while its id stays.
/// </summary>
public sealed class WorldMapTests
{
    static readonly ContentCatalog Catalog = Shipped.Content;

    [Fact]
    public void TheContinentIsTheGrandReachWithNineRegionsOneIslandAndTheNeighborhoodApart()
    {
        var world = Catalog.World;
        Assert.Equal("The Grand Reach", world.Continent);
        Assert.Equal(10, world.Regions.Count);
        Assert.Equal("tropical-island", Assert.Single(world.Regions, r => r.Island).Id);
        // WD-25, WD-26: the neighborhood field is in its own world, reached through a portal; no other region has one.
        var apart = Assert.Single(world.Regions, r => r.Apart);
        Assert.Same(apart, world.ApartRegion);
        Assert.Equal("neighborhood", apart.Id);
        Assert.False(apart.Island);
        Assert.NotNull(apart.Portal);
        Assert.All(world.Regions.Where(r => !r.Apart), r => Assert.Null(r.Portal));
        Assert.DoesNotContain(world.Regions, r => r.Id == "south-coast");
        Assert.All(world.Regions, r =>
        {
            Assert.InRange(r.X, 0, 1);
            Assert.InRange(r.Y, 0, 1);
            Assert.False(string.IsNullOrWhiteSpace(r.Name));
        });
    }

    /// <summary>Every park stands in its own region, where the plan's map puts it.</summary>
    [Fact]
    public void EveryParkStandsInItsOwnRegion()
    {
        var expected = new Dictionary<string, string>
        {
            [ParkId.Harbor] = "neighborhood",
            [ParkId.Crystal] = "frozen-north",
            [ParkId.Funfair] = "central-plains",
            [ParkId.Rooftop] = "eastern-capital",
            [ParkId.Canopy] = "rainforest",
            [ParkId.Ember] = "volcano",
            [ParkId.Stillwater] = "river-delta",
            [ParkId.Coconut] = "tropical-island",
            [ParkId.Sunscorch] = "desert-canyon",
            [ParkId.Summit] = "high-peaks",
        };
        Assert.Equal(expected.Keys.Order(), Catalog.Parks.Keys.Order());
        foreach (var park in Catalog.Parks.Keys)
            Assert.Equal(expected[park], Catalog.World.RegionOf(park).Id);
        // Ten parks, ten regions: every region has its park.
        Assert.All(Catalog.World.Regions, r => Assert.NotNull(Catalog.World.ParkIn(r.Id)));
        Assert.Equal(ParkId.Harbor, Catalog.World.ParkIn("neighborhood"));
        Assert.Equal(ParkId.Coconut, Catalog.World.ParkIn("tropical-island"));
    }

    /// <summary>The map's relationships: the cold park is the northernmost, the island the southernmost.</summary>
    [Fact]
    public void TheColdParkIsNorthAndTheIslandIsSouth()
    {
        var regions = Catalog.World.Regions;
        Assert.Equal("frozen-north", regions.MinBy(r => r.Y)!.Id);
        Assert.Equal("tropical-island", regions.MaxBy(r => r.Y)!.Id);
        Assert.True(Catalog.World.RegionOf(ParkId.Rooftop).X > Catalog.World.RegionOf(ParkId.Canopy).X, "the capital is east of the rainforest");
    }

    [Fact]
    public void AuroraRinkIsTheNamePlayersReadAndTheIdStays()
    {
        var park = Catalog.MustPark(ParkId.Crystal);
        Assert.Equal("crystal-rink", park.Id);
        Assert.Equal("Aurora Rink", park.Name);
        foreach (var lesson in new[] { "T-H01", "T-H02" })
        {
            var copy = GrandSluggers.Sim.Front.HowToPlay.TutorialGoal(lesson) + "\n" + GrandSluggers.Sim.Front.HowToPlay.TutorialSetup(lesson);
            Assert.DoesNotContain("Crystal", copy, StringComparison.Ordinal);
        }
        Assert.Contains("Aurora Rink", GrandSluggers.Sim.Front.HowToPlay.TutorialSetup("T-H01"), StringComparison.Ordinal);
    }

    [Fact]
    public void AParkWithNoRegionOrAnUnknownRegionIsRefusedByName()
    {
        using var missing = new ContentFixture();
        missing.ChangeObject("parks/crystal-rink.json", json => json.Remove("region"));
        Assert.Contains(ContentDataValidator.Validate(missing.Root), e =>
            e.StartsWith(missing.Path("parks/crystal-rink.json"), StringComparison.Ordinal)
            && e.Contains("park 'crystal-rink' region must name its place on the map", StringComparison.Ordinal));

        using var unknown = new ContentFixture();
        unknown.ChangeObject("parks/crystal-rink.json", json => json["region"] = "the-moon");
        Assert.Contains(ContentDataValidator.Validate(unknown.Root), e =>
            e.Contains("park 'crystal-rink' region 'the-moon' is not a region in", StringComparison.Ordinal));
    }

    [Fact]
    public void TwoParksInOneRegionAreRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("parks/crystal-rink.json", json => json["region"] = "neighborhood");
        var thrown = Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root));
        Assert.Contains("park region 'neighborhood' is claimed by more than one park", thrown.Message, StringComparison.Ordinal);
        Assert.Contains(fixture.Path("parks/harbor-diamond.json"), thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>The neighborhood field keeps its id and its dimensions; only its name and its place on the map moved.</summary>
    [Fact]
    public void TheNeighborhoodParkIsTheHarborIdApartFromTheContinent()
    {
        var park = Catalog.MustPark(ParkId.Harbor);
        Assert.Equal("harbor-diamond", park.Id);
        Assert.Equal("Neighborhood Park", park.Name);
        Assert.True(Catalog.World.RegionOf(ParkId.Harbor).Apart);
        // The portal opens onto the continent, never onto the neighborhood itself.
        var shore = Catalog.World.PortalShore();
        Assert.NotNull(shore);
        Assert.False(shore!.Apart);
    }

    [Fact]
    public void ASecondApartRegionAPortalOnTheContinentOrAMissingPortalIsRefused()
    {
        using var two = new ContentFixture();
        two.ChangeObject("world/regions.json", json =>
        {
            json["regions"]![0]!["apart"] = true;
            json["regions"]![0]!["portalX"] = 0.1;
            json["regions"]![0]!["portalY"] = 0.1;
        });
        Assert.Contains(ContentDataValidator.Validate(two.Root), e =>
            e.Contains("only one region stands apart from the continent", StringComparison.Ordinal));

        using var stray = new ContentFixture();
        stray.ChangeObject("world/regions.json", json => json["regions"]![0]!["portalX"] = 0.2);
        Assert.Contains(ContentDataValidator.Validate(stray.Root), e =>
            e.Contains("'frozen-north' carries a portal but is not apart", StringComparison.Ordinal));

        using var none = new ContentFixture();
        none.ChangeObject("world/regions.json", json =>
        {
            var apart = json["regions"]!.AsArray().Single(r => (string?)r!["id"] == "neighborhood")!.AsObject();
            apart.Remove("portalX");
        });
        Assert.Contains(ContentDataValidator.Validate(none.Root), e =>
            e.Contains("'neighborhood' is apart from the continent and must place its portal", StringComparison.Ordinal));
    }

    /// <summary>A park the stick cannot reach is refused by name, so the map never strands the neighborhood across the gap.</summary>
    [Fact]
    public void AParkTheStickCannotReachIsRefused()
    {
        using var fixture = new ContentFixture();
        // Stack the neighborhood exactly on the frozen north: a zero-length step is never taken, so no stick finds it.
        fixture.ChangeObject("world/regions.json", json =>
        {
            var apart = json["regions"]!.AsArray().Single(r => (string?)r!["id"] == "neighborhood")!.AsObject();
            apart["x"] = 0.50;
            apart["y"] = 0.08;
        });
        Assert.Contains(ContentDataValidator.Validate(fixture.Root), e =>
            e.Contains("the map cursor cannot reach", StringComparison.Ordinal) && e.Contains(ParkId.Harbor, StringComparison.Ordinal));
    }

    [Fact]
    public void TheWorldFileIsReadStrictly()
    {
        using var offMap = new ContentFixture();
        offMap.ChangeObject("world/regions.json", json => json["regions"]![0]!["x"] = 1.5);
        Assert.Contains(ContentDataValidator.Validate(offMap.Root), e =>
            e.Contains("'frozen-north' x must be 0 to 1 on the unit map; got 1.5", StringComparison.Ordinal));

        using var twice = new ContentFixture();
        twice.ChangeObject("world/regions.json", json => json["regions"]![1]!["id"] = "frozen-north");
        Assert.Contains(ContentDataValidator.Validate(twice.Root), e =>
            e.Contains("id 'frozen-north' is declared twice", StringComparison.Ordinal));

        using var extra = new ContentFixture();
        extra.ChangeObject("world/regions.json", json => json["regions"]![0]!["climate"] = "cold");
        Assert.Contains(ContentDataValidator.Validate(extra.Root), e =>
            e.Contains("climate", StringComparison.Ordinal) && e.Contains("is not a key this file declares", StringComparison.Ordinal));
    }
}
