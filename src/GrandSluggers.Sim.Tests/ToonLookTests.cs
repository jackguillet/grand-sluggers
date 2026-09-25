using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The character toon (CF-7, CH-14): the bands and the rim live in <c>data/art/toon.json</c>, the body slot names the
/// shader that reads them, and the shader is a URP shader with the depth, depth-normals and shadow passes whose absence
/// made the old ToonFill lose the body to the grass. The source rows are the falsifier for the Unity half.
/// </summary>
public sealed class ToonLookTests
{
    static readonly ContentCatalog Catalog = Shipped.Content;

    const string Good = """
        {"fill": {"whiten": 0.1, "gain": [1.2, 1.1, 1.0]},
         "light": {"wrap": 0.5, "bandAt": 0.5, "bandSoft": 0.05, "shade": [0.6, 0.5, 0.7], "shadow": 1, "sunFull": 1, "sunTint": 0.2, "ambient": 0.2},
         "rim": {"color": [1, 1, 1], "at": 0.6, "soft": 0.05, "strength": 0.5, "up": 0.3}}
        """;

    [Fact]
    public void CF7_TheShippedToonLoadsAndTheBodySlotNamesToonRim()
    {
        var toon = Catalog.Art.Toon;
        Assert.NotNull(toon);
        Assert.Empty(ToonLook.Validate(Catalog.Art.Materials));
        Assert.InRange(toon.Light.BandAt, 0.05, 0.95);
        Assert.True(toon.Rim.Strength > 0, "a toon with no rim is the old fill");
    }

    [Fact]
    public void CF7_TheFillIsWhitenedThenGainedAndCapped()
    {
        var fill = ToonLook.Parse(JsonNode.Parse(Good), "t").Fill;
        var (r, g, b) = fill.Of(0.5, 0.5, 1.0);
        Assert.Equal(0.55 * 1.2, r, 9);
        Assert.Equal(0.55 * 1.1, g, 9);
        Assert.Equal(1.0, b, 9);
        Assert.Equal(1.0, fill.Of(0.95, 0, 0).R, 9);
    }

    [Theory]
    [InlineData("\"bandAt\": 0.5", "\"bandAt\": 1.5", "t.light.bandAt is 1.5; it is 0 to 1")]
    [InlineData("\"soft\": 0.05", "\"soft\": 0.8", "t.rim.soft is 0.8; a step width is 0 to 0.5")]
    [InlineData("\"sunFull\": 1", "\"sunFull\": 0", "t.light.sunFull must be greater than 0")]
    [InlineData("\"gain\": [1.2, 1.1, 1.0]", "\"gain\": [1.2, 3, 1.0]", "t.fill.gain[1] is 3; a gain is 0.5 to 2")]
    [InlineData("\"up\": 0.3", "\"up\": 0.3, \"power\": 4", "t.rim.power is not a key here")]
    [InlineData("\"ambient\": 0.2", "\"ambient\": \"lots\"", "t.light.ambient must be a number")]
    public void CF7_TheReaderRefusesABadKnobByName(string from, string to, string message)
    {
        var ex = Assert.Throws<InvalidDataException>(() => ToonLook.Parse(JsonNode.Parse(Good.Replace(from, to)), "t"));
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void CF7_TheValidatorNamesAMissingOrWrongBodySlot()
    {
        Assert.Contains("materials.json has no toon-body slot; the character toon (toon.json) draws in it",
            ToonLook.Validate([new NamedSlot("dirt", "Assets/Art/Materials/dirt", "ToonFill")]));
        Assert.Contains("materials.json toon-body names shader ToonFill; toon.json is drawn by ToonRim",
            ToonLook.Validate([new NamedSlot("toon-body", "Assets/Art/Materials/toon-body", "ToonFill")]));
    }

    /// <summary>
    /// The depth bug's rail: a URP pass for colour, and the DepthOnly, DepthNormals and ShadowCaster passes, all reading
    /// one UnityPerMaterial buffer (SRP Batcher). ToonFill had one SRPDefaultUnlit pass and none of the others.
    /// </summary>
    [Fact]
    public void CF7_ToonRimIsAUrpShaderWithDepthAndShadowPasses()
    {
        var shader = Unity("Assets", "Resources", "Shaders", "ToonRim.shader");
        foreach (var mode in new[] { "UniversalForwardOnly", "DepthOnly", "DepthNormals", "ShadowCaster" })
            Assert.Contains("\"LightMode\" = \"" + mode + "\"", shader);
        Assert.DoesNotContain("\"LightMode\" = \"SRPDefaultUnlit\"", shader);
        Assert.DoesNotMatch(new Regex("^\\s*CGPROGRAM", RegexOptions.Multiline), shader);
        Assert.Contains("CBUFFER_START(UnityPerMaterial)", shader);
        Assert.Matches(new Regex("\"Queue\"\\s*=\\s*\"Geometry\""), shader);
    }

    /// <summary>Every knob of toon.json reaches the material, and every material property the shader declares is set from data.</summary>
    [Fact]
    public void CF7_LookBodySetsEveryShaderPropertyFromTheTable()
    {
        var shader = Unity("Assets", "Resources", "Shaders", "ToonRim.shader");
        var look = Unity("Assets", "Scripts", "Runtime", "Look.cs");
        var body = look.Substring(look.IndexOf("public static Material Body(", StringComparison.Ordinal));
        body = body.Substring(0, body.IndexOf("public static Material Unlit(", StringComparison.Ordinal));
        var props = Regex.Matches(shader, "^\\s*(_[A-Za-z]+)\\s*\\(\"", RegexOptions.Multiline).Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(14, props.Count);
        foreach (var p in props)
            Assert.Contains("\"" + p + "\"", body);
        foreach (var knob in new[] { "Wrap", "BandAt", "BandSoft", "Shade", "Shadow", "SunFull", "SunTint", "Ambient", "At", "Soft", "Strength", "Up" })
            Assert.Matches(new Regex("\\b(l|rim)\\." + knob + "\\b"), body);
        Assert.Contains("toon.Fill.Of(", body);
    }

    static string Unity(params string[] parts)
    {
        var repo = Directory.GetParent(Catalog.Root.Shipped)!.FullName;
        return File.ReadAllText(Path.Combine([repo, "unity", .. parts]));
    }
}
