using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// A captain's identity is data (#1032): its team name, signature bat and body proportions in its character file, the
/// captain order and the preset nines in <c>data/teams/teams.json</c>, the sides' gloves in <c>match.json</c>. A role player
/// wears its faction's captain's body. Each rule below used to be a C# switch whose default quietly played rio.
/// </summary>
public sealed class CaptainDataTests
{
    static InvalidDataException Refused(Action<ContentFixture> change)
    {
        using var fixture = new ContentFixture();
        change(fixture);
        return Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(new DataRoot(fixture.Root)));
    }

    [Fact]
    public void TheCatalogCarriesEveryCaptainsIdentity()
    {
        var content = Shipped.Content;
        Assert.Equal(new[] { "rio", "vale", "zig", "brondo", "konga", "ashlord", "fenn" }, content.CaptainIds);
        Assert.Equal("Stillwater", PresetTeams.TeamName(content.Must("fenn")));
        Assert.Equal("fen-cane", GearMesh.SignatureBat(content, "fenn").Id);
        Assert.Equal("zig", content.Must("jester").BodyType);
        Assert.Equal(content.Must("zig").Proportions, content.Must("jester").Proportions);
        Assert.Equal("Ember Court", PresetTeams.EmberCourt(content).Name);
        Assert.Equal(9, PresetTeams.EmberCourt(content).Roster.Count);
        Assert.Equal("vale", PresetTeams.NextCaptain(content, "rio"));
        Assert.Equal("fenn", PresetTeams.PrevCaptain(content, "rio"));
    }

    [Theory]
    [InlineData("teamName", "captain 'vale' teamName is required")]
    [InlineData("signatureBat", "captain 'vale' signatureBat is required")]
    [InlineData("proportions", "captain 'vale' proportions are required")]
    public void ACaptainWithoutItsIdentityIsRefused(string key, string expected)
    {
        var error = Refused(f => f.ChangeObject("characters/vale.json", json => json.Remove(key)));
        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ASignatureBatTheCatalogDoesNotHaveIsRefused()
    {
        var error = Refused(f => f.ChangeObject("characters/zig.json", json => json["signatureBat"] = "pool-noodle"));
        Assert.Contains("signatureBat 'pool-noodle' is not a bat", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ARolePlayerWhoseFactionHasNoCaptainIsRefused()
    {
        var error = Refused(f => f.ChangeArray("characters/role-players.json", rows => rows[0]!["faction"] = "nowhere"));
        Assert.Contains("faction 'nowhere' has no captain to take its body from", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ARolePlayerCarriesNoCaptainIdentity()
    {
        var error = Refused(f => f.ChangeArray("characters/role-players.json", rows => rows[0]!["teamName"] = "Solo"));
        Assert.Contains("names teamName, signatureBat or proportions; those are a captain's", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheCaptainOrderNamesEveryCaptainOnce()
    {
        var left = Refused(f => f.ChangeObject("teams/teams.json", json => json["captains"]!.AsArray().RemoveAt(6)));
        Assert.Contains("captains leaves out 'fenn'", left.Message, StringComparison.Ordinal);
        var twice = Refused(f => f.ChangeObject("teams/teams.json", json => json["captains"]!.AsArray().Add("rio")));
        Assert.Contains("captains names 'rio' twice", twice.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void APresetNamesNineKnownCharacters()
    {
        var error = Refused(f => f.ChangeObject("teams/teams.json", json => json["presets"]![0]!["roster"]![8] = "ghost"));
        Assert.Contains("roster names 'ghost', which is not a character", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ASideGloveTheCatalogDoesNotHaveIsRefused()
    {
        var error = Refused(f => f.ChangeObject("rules/match.json", json => json["awayGlove"] = "oven-mitt"));
        Assert.Contains("match.awayGlove 'oven-mitt' is not a glove", error.Message, StringComparison.Ordinal);
    }
}
