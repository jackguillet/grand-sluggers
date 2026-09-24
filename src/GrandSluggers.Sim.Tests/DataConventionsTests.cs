using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The data's naming rules (spec §16): a number's unit is in its key, and a one-row file is named for its row.
/// </summary>
public sealed class DataConventionsTests
{
    static string ShippedRoot => Shipped.Content.Root.Shipped;

    [Fact]
    public void TheShippedDataNamesItsUnits() => Assert.Empty(DataNaming.Validate(ShippedRoot));

    [Theory]
    [InlineData("hangSeconds", "'Sec'")]
    [InlineData("throwMillis", "'Sec'")]
    [InlineData("reachFeet", "'Ft'")]
    [InlineData("spinDegrees", "'Deg'")]
    [InlineData("tagFreeze", "a time with no unit")]
    [InlineData("cameraHold", "a time with no unit")]
    [InlineData("swapDelay", "a time with no unit")]
    [InlineData("runnerSpeed", "a speed with no unit")]
    [InlineData("radius", "a length with no unit")]
    [InlineData("throwDistance", "a length with no unit")]
    public void AKeyThatHidesItsUnitIsRefused(string key, string message)
    {
        var errors = DataNaming.ValidateJson("rules/example.json", "{\"row\": {\"" + key + "\": 1.5}}");
        var error = Assert.Single(errors);
        Assert.Contains($"'row.{key}'", error);
        Assert.Contains(message, error);
    }

    [Theory]
    [InlineData("hangSec")]
    [InlineData("reachFt")]
    [InlineData("launchDeg")]
    [InlineData("exitMph")]
    [InlineData("kickFtPerSec")]
    [InlineData("bodyTurnDegPerSec")]
    [InlineData("perFtOfHeight")]
    [InlineData("speedMul")]
    [InlineData("secondSec")]
    [InlineData("floorFrames")]
    public void AKeyThatNamesItsUnitPasses(string key) =>
        Assert.Empty(DataNaming.ValidateJson("rules/example.json", $$"""{"{{key}}": 2}"""));

    [Fact]
    public void OnlyNumbersAreJudged() =>
        Assert.Empty(DataNaming.ValidateJson("rules/example.json", """{"speed": "fast", "hold": true, "radius": null}"""));

    [Fact]
    public void ANewKeyBesideAGrandfatheredOneIsStillRefused()
    {
        // feel/table.json keeps its smashFreeze; a second unitless freeze in the same file is new, and refused.
        Assert.Empty(DataNaming.ValidateJson("feel/table.json", """{"smashFreeze": 0.1}"""));
        Assert.Single(DataNaming.ValidateJson("feel/table.json", """{"catchFreeze": 0.1}"""));
        Assert.Single(DataNaming.ValidateJson("rules/fielding.json", """{"smashFreeze": 0.1}"""));
    }

    [Fact]
    public void ARowsPathIsJudgedInsideArrays()
    {
        var error = Assert.Single(DataNaming.ValidateJson("parks/example.json", """{"zones": [{"radius": 4}]}"""));
        Assert.Contains("'zones[].radius'", error);
        Assert.Empty(DataNaming.ValidateJson("parks/example.json", """{"hazards": [{"radius": 4}]}"""));
    }

    [Fact]
    public void AGrandfatheredRowThatNamesNothingIsRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("rules/flight.json", json =>
        {
            json["maxSec"] = json["maxSeconds"]!.DeepClone();
            json.Remove("maxSeconds");
        });
        Assert.Contains(DataNaming.Validate(fixture.Root), e => e.Contains("'rules/flight.json#maxSeconds' names no key any more"));
    }

    [Theory]
    [InlineData("parks", ParkIds.Crystal)]
    [InlineData("bats", "barrel-bat")]
    [InlineData("gloves", "web-back")]
    [InlineData("characters", "rio")]
    public void AOneRowFileIsNamedForItsRow(string folder, string id)
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject($"{folder}/{id}.json", json => json["id"] = id + "-copy");
        Assert.Contains(ContentDataValidator.Validate(fixture.Root),
            e => e.Contains($"{id}.json: id '{id}-copy' must equal the file name '{id}'"));
    }

    [Fact]
    public void EveryShippedOneRowFileIsNamedForItsRow()
    {
        foreach (var folder in new[] { "parks", "bats", "gloves", "characters" })
            foreach (var file in Directory.EnumerateFiles(Path.Combine(ShippedRoot, folder), "*.json"))
            {
                var json = JsonNode.Parse(File.ReadAllText(file), documentOptions: DataJson.Document);
                if (json is JsonObject row)
                    Assert.Equal(Path.GetFileNameWithoutExtension(file), (string?)row["id"]);
            }
    }
}
