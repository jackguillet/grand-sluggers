using GrandSluggers.Sim;
using GrandSluggers.Sim.Tooling;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The conventions data/ keeps by validator, not by habit: a number names its unit (DataNaming), a one-row file is
/// named for its id (ContentDataValidator), and the tests' park ids are the catalog's.
/// </summary>
public sealed class DataConventionTests
{
    static string DataFolder => Shipped.Content.Root.Shipped;

    [Fact]
    public void EveryShippedNumberNamesItsUnit() =>
        Assert.Empty(DataNaming.Validate(DataFolder));

    [Fact]
    public void EveryGrandfatheredKeyIsStillInTheData() =>
        Assert.Empty(DataNaming.StaleGrandfathered(DataFolder));

    [Theory]
    [InlineData("chargeSeconds")]
    [InlineData("depthFeet")]
    [InlineData("reachInches")]
    [InlineData("tiltDegrees")]
    [InlineData("delayMs")]
    [InlineData("smashFreeze")]
    [InlineData("cameraBlend")]
    [InlineData("restSpeed")]
    [InlineData("speed")]
    [InlineData("hold")]
    [InlineData("radius")]
    [InlineData("throwDistance")]
    [InlineData("bodyHeight")]
    public void AUnitSpelledLongOrMissingIsRefused(string key) =>
        Assert.NotNull(DataNaming.Problem(key));

    [Theory]
    [InlineData("chargeSec")]
    [InlineData("depthFt")]
    [InlineData("tiltDeg")]
    [InlineData("exitVyFtPerSec")]
    [InlineData("topSpeedMph")]
    [InlineData("restSpeedFtPerSec")]
    [InlineData("holdUntilSec")]
    [InlineData("contactMod")]
    [InlineData("chance")]
    [InlineData("radiusFt")]
    [InlineData("heightMul")]
    [InlineData("perFtOfHeight")]
    public void AKeyThatNamesItsUnitPasses(string key) =>
        Assert.Null(DataNaming.Problem(key));

    [Fact]
    public void AFolderRowCoversEveryFileInItsFolderAndNoOther()
    {
        using var fixture = new ContentFixture();
        // A new park's hazard discs carry the same grandfathered radius; a radius anywhere else is new, and refused.
        fixture.Copy("parks/crystal-rink.json", "parks/new-park.json");
        fixture.ChangeObject("rules/fielding.json", json => json["radius"] = 4);
        var error = Assert.Single(DataNaming.Validate(fixture.Root));
        Assert.Contains("rules/fielding.json: 'radius' is a length with no unit", error);
    }

    [Fact]
    public void ANewKeyWithoutItsUnitFailsInItsFile()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("rules/flight.json", json => json["hangSeconds"] = 1.5);
        var error = Assert.Single(DataNaming.Validate(fixture.Root));
        Assert.Equal("rules/flight.json: 'hangSeconds' spells its unit long; use Sec, Ft, Deg, FtPerSec or Mph", error);
    }

    [Fact]
    public void AOneRowFileIsNamedForItsId()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("bats/fen-cane.json", json => json["id"] = "fen-stick");
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.EndsWith("bat id 'fen-stick' must match its file name 'fen-cane'", StringComparison.Ordinal));
    }

    [Fact]
    public void TheTestsParkIdsAreTheCatalogs() =>
        Assert.Equal(Shipped.Content.Parks.Keys.Order(StringComparer.Ordinal), ParkId.All.Order(StringComparer.Ordinal));
}
