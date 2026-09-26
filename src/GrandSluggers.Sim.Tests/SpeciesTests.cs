using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The sidekicks (WD-27): eight per captain, from three species of the captain's park, one of each build. A sidekick
/// wears its species' body; only Rio's are human.
/// </summary>
public sealed class SpeciesTests
{
    static ContentCatalog Content => Shipped.Content;

    [Fact]
    public void EveryCaptainBringsEightSidekicksFromThreeSpeciesOneOfEachBuild()
    {
        foreach (var id in Content.CaptainIds)
        {
            var cap = Content.Must(id);
            var mine = Content.Species.Values.Where(s => s.Faction == cap.Faction).ToList();
            Assert.Equal(SpeciesBuilds.All.OrderBy(b => b), mine.Select(s => s.Build).OrderBy(b => b));
            var sidekicks = Content.Characters.Values.Where(c => !c.Captain && c.Faction == cap.Faction).ToList();
            Assert.Equal(SpeciesBuilds.SidekicksPerCaptain, sidekicks.Count);
            Assert.All(sidekicks, c => Assert.Contains(c.Species, mine.Select(s => s.Id)));
        }
        Assert.Equal(10 * SpeciesBuilds.SidekicksPerCaptain, Content.Characters.Values.Count(c => !c.Captain));
    }

    [Fact]
    public void ASidekickWearsItsSpeciesBodyAndTwoOfOneSpeciesStandAlike()
    {
        foreach (var c in Content.Characters.Values.Where(c => !c.Captain))
            Assert.Equal(Content.Species[c.Species].Proportions, c.Proportions);
        var bruiser = Content.Species["walrams"].Proportions;
        var scamp = Content.Species["frostlings"].Proportions;
        // A bruiser stands taller and wider than a scamp.
        Assert.True(bruiser.Height > scamp.Height && bruiser.Width > scamp.Width);
        // Only the neighborhood's are human, and they wear their own bodies, not a build's.
        Assert.All(Content.Species.Values, s => Assert.Equal(s.Faction == "spark", s.Blend == "Human"));
    }

    [Fact]
    public void ASidekickWithNoSpeciesOrAnotherFactionsIsRefusedByName()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeArray("characters/role-players.json", rows =>
        {
            rows[0]!["species"] = "walrams";
            rows[1]!.AsObject().Remove("species");
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("sidekick 'nico' species 'walrams' belongs to faction 'royal', not 'spark'", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("sidekick 'pip' species 'null' is not a row", StringComparison.Ordinal));
    }

    [Fact]
    public void AFactionMissingABuildOrShortOfEightIsRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("world/species.json", json =>
        {
            var rows = json["species"]!.AsArray();
            var glint = rows.First(r => r!["id"]!.GetValue<string>() == "glintfoxes")!;
            glint["build"] = "scamp";
        });
        fixture.ChangeArray("characters/role-players.json", rows => rows.RemoveAt(rows.Count - 1));
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("faction 'royal' needs three species, one of each build", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("faction 'peak' needs 8 sidekicks; got 7", StringComparison.Ordinal));
    }
}
