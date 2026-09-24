using System.Reflection;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// <c>data/feel/table.json</c> is read like the rules tables: the JSON is the only source of a presentation number. A key
/// the table does not declare, a key it declares and the file leaves out, and a value outside its range are each refused
/// by name; nothing is quietly replaced by a number in code.
/// </summary>
public sealed class FeelTableTests
{
    static InvalidDataException Refused(Action<System.Text.Json.Nodes.JsonObject> change)
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("feel/table.json", change);
        return Assert.Throws<InvalidDataException>(() => FeelTable.Load(new DataRoot(fixture.Root)));
    }

    [Fact]
    public void TheShippedTableLoadsWhole()
    {
        var feel = Shipped.Content.Feel;
        Assert.Equal(0.35, feel.FieldAssistStick);
        Assert.Equal(0.42, feel.ContactCutSeconds);
        Assert.Equal(0.06, feel.RaceCamera.Margin);
    }

    [Fact]
    public void AMissingKeyIsRefusedNotFilledIn()
    {
        var error = Refused(json => json.Remove("pitcherReadySeconds"));
        Assert.Contains("feel.pitcherReadySeconds is missing", error.Message, StringComparison.Ordinal);
        var nested = Refused(json => json["ballShadow"]!.AsObject().Remove("opacity"));
        Assert.Contains("feel.ballShadow.opacity is missing", nested.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownKeyIsATypo()
    {
        var error = Refused(json => json["contactCutSecs"] = 0.4);
        Assert.Contains("feel.contactCutSecs is not a rule this table owns", error.Message, StringComparison.Ordinal);
    }

    /// <summary>A zero used to be swapped for the code's number without a word; now it is the error it always was.</summary>
    [Theory]
    [InlineData("fieldAssistStick", "feel.fieldAssistStick must be greater than 0")]
    [InlineData("afterOutSeconds", "feel.afterOutSeconds must be greater than 0")]
    [InlineData("runnerShareStepFt", "feel.runnerShareStepFt must be greater than 0")]
    public void AZeroIsRefusedNotReplaced(string key, string expected)
    {
        var error = Refused(json => json[key] = 0);
        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ACrossFieldRuleStillNamesTheFile()
    {
        var error = Refused(json => json["ballShadow"]!["nearDiameterFt"] = 2);
        Assert.Contains("table.json", error.Message, StringComparison.Ordinal);
        Assert.Contains("near >= far", error.Message, StringComparison.Ordinal);
    }

    /// <summary>The rail under the rows above: a feel table built with <c>new</c> is all zeros, so no number hides in code.</summary>
    [Fact]
    public void TheFeelTablesCarryNoCodeDefaults()
    {
        var defaults = new List<string>();
        foreach (var type in new[] { typeof(FeelTable), typeof(BallShadowFeel), typeof(FieldTellsFeel), typeof(RaceCameraFeel) })
        {
            var blank = Activator.CreateInstance(type, nonPublic: true)!;
            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (p.PropertyType == typeof(double) && (double)p.GetValue(blank)! != 0)
                    defaults.Add($"{type.Name}.{p.Name} = {p.GetValue(blank)}");
        }
        Assert.True(defaults.Count == 0, string.Join("\n", defaults));
    }
}
