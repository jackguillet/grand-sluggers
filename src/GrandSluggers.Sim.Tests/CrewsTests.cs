using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Crews carry chemistry across teams (WD-28): a shared crew is good whatever the faction; a crew and its rival crew are
/// bad across factions; faction-mates stay good; an authored story pair beats all of it.
/// </summary>
public sealed class CrewsTests
{
    static ContentCatalog Content => Shipped.Content;
    static Chemistry Between(string a, string b) => Content.Chemistry.Between(a, b);

    [Fact]
    public void ASharedCrewIsGoodChemistryAcrossFactions()
    {
        // Pip (Spark) and Tilt (Carnival) are both Cool Kids.
        Assert.NotEqual(Content.Must("pip").Faction, Content.Must("tilt").Faction);
        Assert.Contains("cool-kids", Content.Must("tilt").Crews);
        Assert.Equal(Chemistry.Good, Between("pip", "tilt"));
    }

    [Fact]
    public void ARivalCrewIsBadAcrossFactionsButFactionMatesStayGood()
    {
        // Night Crew and Early Birds are rivals: Soot (Ember, Night Crew) and Gull (Spark, Early Birds).
        Assert.Equal(Chemistry.Bad, Between("soot", "gull"));
        // Two faction-mates in rival crews are still good: the team comes first.
        var ember = Content.Characters.Values.Where(c => c.Faction == "ember").ToList();
        Assert.All(ember, a => Assert.All(ember.Where(b => b.Id != a.Id), b => Assert.Equal(Chemistry.Good, Between(a.Id, b.Id))));
    }

    [Fact]
    public void TheStoryPairBeatsEverything()
    {
        Assert.Equal(Chemistry.Bad, Between("rio", "ashlord"));
    }

    [Fact]
    public void EveryCharacterNamesAtMostTwoKnownCrewsAndABadCrewIsRefused()
    {
        Assert.All(Content.Characters.Values, c => Assert.InRange(c.Crews.Count, 0, 2));
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/zig.json", json => json["crews"] = new System.Text.Json.Nodes.JsonArray("showboats", "rockers", "brainiacs"));
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("character 'zig' names 3 crews; at most 2", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("character 'zig' crew 'rockers' is not a row", StringComparison.Ordinal));
    }
}
