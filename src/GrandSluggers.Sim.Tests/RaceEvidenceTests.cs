using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;
namespace GrandSluggers.Sim.Tests;

public sealed class RaceEvidenceTests
{
    static JsonObject Catalog() => JsonNode.Parse(File.ReadAllText(RaceEvidence.PathFor(ContentCatalog.Load().Root)))!.AsObject();
    [Fact]
    public void PendingMeasurementsStayNullAndTheScoringDecisionIsAccepted()
    {
        var root = ContentCatalog.Load().Root;
        Assert.Empty(RaceEvidence.Validate(root));
        var pending = Catalog()["budgets"]![1]!;
        Assert.Null(pending["lower"]);
        Assert.False(pending["activeDefault"]!.GetValue<bool>());
        Assert.Empty(RaceCohort.CalibrationSeeds.Intersect(RaceCohort.ValidationSeeds));
    }

    [Theory]
    [InlineData("sources", 0, "locator", "missing locator")]
    [InlineData("sources", 0, "uncertainty", "missing uncertainty")]
    [InlineData("budgets", 0, "unit", "missing unit")]
    [InlineData("budgets", 0, "lower", "accepted bounds required")]
    [InlineData("decisions", 0, "locator", "missing locator")]
    public void MissingEvidenceCannotPass(string array, int index, string key, string message)
    {
        var json = Catalog(); json[array]![index]!.AsObject().Remove(key);
        Assert.Contains(RaceEvidence.ValidateJson(json.ToJsonString()), e => e.Contains(message));
    }

    [Fact]
    public void ContradictoryAndUnknownRecordsCannotBecomeTargets()
    {
        var json = Catalog();
        json["budgets"]![0]!["lower"] = 6;
        json["budgets"]![1]!["activeDefault"] = true;
        json["budgets"]![1]!["source"] = "unknown";
        json["budgets"]![1]!["fixture"] = "unknown";
        json["budgets"]![1]!["decision"] = "unknown";
        json["sources"]!.AsArray().Add(json["sources"]![0]!.DeepClone());
        var errors = RaceEvidence.ValidateJson(json.ToJsonString());
        foreach (var text in new[] { "inverted bounds", "active default", "duplicate id", "unknown source", "unknown fixture", "unknown decision" })
            Assert.Contains(errors, e => e.Contains(text));
    }

    [Fact]
    public void AgentDataValidationRejectsAMissingLedger()
    {
        // The agent step (cli art) still refuses it; the game load does not read it (RuntimePackageTests).
        var missingRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Assert.Contains(AgentData.Validate(missingRoot), e => e.Contains("race-evidence.json"));
    }
}
