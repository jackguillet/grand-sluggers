using System.Text.RegularExpressions;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Rule numbers live in <c>data/rules</c>, and field-ability ids live in one registry (<see cref="FieldAbilityId"/>) that
/// the content validator and every rule share.
/// </summary>
public sealed class RuleLiteralsTests
{
    static string SimDir => Path.Combine(Shipped.Content.Root.Shipped, "..", "src", "GrandSluggers.Sim");

    [Fact]
    public void EveryShippedAbilityIsInTheRegistryAndTheRegistryIsTheValidatorsList()
    {
        Assert.Equal(FieldAbilityId.All.OrderBy(x => x), ContentDataValidator.TutorialFieldAbilities.OrderBy(x => x));
        foreach (var who in Shipped.Content.Characters.Values.Where(c => c.FieldAbility.Length > 0))
            Assert.Contains(who.FieldAbility, FieldAbilityId.All);
    }

    [Fact]
    public void NoAbilityIdIsSpelledOutsideTheRegistry()
    {
        // Motion clip ids ("dive") and Scheme control ids share spellings with abilities; they are not ability reads.
        var ids = string.Join("|", FieldAbilityId.All.Select(Regex.Escape));
        var literal = new Regex($"\"({ids})\"");
        var exempt = new[] { "FieldAbilities.cs", "Motion.cs", "Scheme.cs" };
        var offenders = Directory.GetFiles(SimDir, "*.cs")
            .Where(f => !exempt.Contains(Path.GetFileName(f)))
            .SelectMany(f => File.ReadAllLines(f).Select((line, i) => (f, line, i)))
            .Where(x => literal.IsMatch(x.line) && !x.line.TrimStart().StartsWith("//", StringComparison.Ordinal))
            .Select(x => $"{Path.GetFileName(x.f)}:{x.i + 1}: {x.line.Trim()}")
            .ToList();
        Assert.Empty(offenders);
    }

    [Fact]
    public void TheCutoffLaneAndStealAnchorsAreRefusedWhenMalformed()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("rules/fielding.json", json =>
        {
            var t = json["throw"]!.AsObject();
            t["cutoffLaneMin"] = 0.9;
            t["cutoffLaneMax"] = 0.1;
            t["cutoffPositions"] = new System.Text.Json.Nodes.JsonArray("SS", "XX");
        });
        fixture.ChangeObject("rules/running.json", json =>
            json["cpu"]!["stealAnchors"] = System.Text.Json.Nodes.JsonNode.Parse("""[{"run": 8, "chance": 0.2}, {"run": 6, "chance": 0.1}]"""));
        var errors = RulesTable.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("fielding.throw.cutoffLaneMin"));
        Assert.Contains(errors, e => e.Contains("names 'XX'"));
        Assert.Contains(errors, e => e.Contains("must rise in run"));
        Assert.Contains(errors, e => e.Contains("chances must rise"));
    }

    [Fact]
    public void TheStealTableIsLinearBetweenItsAnchorsAndHoldsPastTheLast()
    {
        var cpu = Shipped.Content.Rules.Running.Cpu with
        {
            StealMinRun = 2,
            StealAnchors = [new StealAnchor(4, 0.2), new StealAnchor(8, 0.6)]
        };
        Assert.Equal(0, RunnerAi.StealBase(2, cpu));
        Assert.Equal(0.1, RunnerAi.StealBase(3, cpu), 12);
        Assert.Equal(0.4, RunnerAi.StealBase(6, cpu), 12);
        Assert.Equal(0.6, RunnerAi.StealBase(10, cpu), 12);
    }
}
