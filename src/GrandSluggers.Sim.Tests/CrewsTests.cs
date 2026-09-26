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

    /// <summary>
    /// One function says why (WD-28): <see cref="ChemistryTable.Reason(string, string)"/> is the verdict every reader gets,
    /// and it names the rule and the crews that decide each pair.
    /// </summary>
    [Fact]
    public void TheReasonIsTheVerdictAndNamesTheRuleAndTheCrews()
    {
        var ids = Content.Characters.Keys.ToList();
        foreach (var a in ids)
        foreach (var b in ids)
        {
            var reason = Content.Chemistry.Reason(a, b);
            Assert.Equal(Between(a, b), reason.Chemistry);
            Assert.Equal(reason.Why == ChemistryWhy.None, reason.Chemistry == Chemistry.Neutral);
        }
        Assert.Equal(new ChemistryReason(Chemistry.Good, ChemistryWhy.SharedCrew, "cool-kids"), Content.Chemistry.Reason("pip", "tilt"));
        Assert.Equal(new ChemistryReason(Chemistry.Bad, ChemistryWhy.RivalCrews, "night-crew", "early-birds"), Content.Chemistry.Reason("soot", "gull"));
        Assert.Equal(new ChemistryReason(Chemistry.Bad, ChemistryWhy.StoryPair), Content.Chemistry.Reason("rio", "ashlord"));
        // Faction-mates with no shared crew are good because of the team.
        var mates = ids.SelectMany(a => ids.Select(b => (a, b)))
            .First(p => p.a != p.b && Content.Must(p.a).Faction == Content.Must(p.b).Faction
                && !Content.Must(p.a).Crews.Intersect(Content.Must(p.b).Crews).Any()
                && Content.Chemistry.Reason(p.a, p.b).Why != ChemistryWhy.StoryPair);
        Assert.Equal(ChemistryWhy.FactionMates, Content.Chemistry.Reason(mates.a, mates.b).Why);
        Assert.Equal(ChemistryWhy.None, Content.Chemistry.Reason("rio", "rio").Why);
        Assert.Equal("Cool Kids", Content.CrewName("cool-kids"));
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
