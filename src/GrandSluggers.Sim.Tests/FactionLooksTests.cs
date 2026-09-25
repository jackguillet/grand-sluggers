using GrandSluggers.Sim;
using System.Text.Json.Nodes;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The faction colors (data/art/factions.json): every faction a character plays has its jersey, accent and skin, the seven
/// factions that were colored in code keep their colors, the three new ones have their own, and the reader is strict.
/// </summary>
public sealed class FactionLooksTests
{
    static readonly ContentCatalog Catalog = Shipped.Content;
    static FactionLooks Factions => Catalog.Art.Factions;

    [Fact]
    public void EveryPlayedFactionHasItsColorsAndNoRowIsStale()
    {
        var played = Catalog.Characters.Values.Select(c => c.Faction).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(f => f, StringComparer.Ordinal);
        Assert.Equal(played, Factions.Rows.Keys.OrderBy(f => f, StringComparer.Ordinal));
        Assert.DoesNotContain(Catalog.Art.Validate(Catalog), e => e.Contains("faction colors", StringComparison.Ordinal));
        Assert.Equal(10, Factions.Rows.Count);
    }

    /// <summary>The colors the code switches held, exactly, so no existing team changes color.</summary>
    [Fact]
    public void TheSevenFactionsKeepTheColorsTheCodeHeld()
    {
        Assert.Equal(0xDC302A, Factions.Of("spark")!.Body.Hex);
        Assert.Equal(0xFFCC40, Factions.Of("spark")!.Accent.Hex);
        Assert.Equal(0xE878A8, Factions.Of("royal")!.Body.Hex);
        Assert.Equal((0.7, 0.9, 1.0), (Factions.Of("royal")!.Accent.R, Factions.Of("royal")!.Accent.G, Factions.Of("royal")!.Accent.B));
        Assert.Equal(0x28AA5A, Factions.Of("carnival")!.Body.Hex);
        Assert.Equal(0xE8BC28, Factions.Of("goldrush")!.Body.Hex);
        Assert.Equal(0x784E2A, Factions.Of("canopy")!.Body.Hex);
        Assert.Equal(0x2C2034, Factions.Of("ember")!.Body.Hex);
        Assert.Equal(0x5B8F62, Factions.Of("fen")!.Body.Hex);
        Assert.Equal(0x7FB57A, Factions.Of("fen")!.Skin.Hex);
        Assert.Equal((0.35, 0.3, 0.36), (Factions.Of("ember")!.Skin.R, Factions.Of("ember")!.Skin.G, Factions.Of("ember")!.Skin.B));
    }

    /// <summary>The three new factions read as their own teams: no two factions share a jersey.</summary>
    [Fact]
    public void NoTwoFactionsShareAJersey()
    {
        var jerseys = Factions.Rows.Values.Select(f => (f.Body.R, f.Body.G, f.Body.B)).ToList();
        Assert.Equal(jerseys.Count, jerseys.Distinct().Count());
        Assert.NotNull(Factions.Of("dune"));
        Assert.NotNull(Factions.Of("peak"));
        Assert.NotNull(Factions.Of("marsh"));
    }

    /// <summary>The colors are read, never switched on: the three places that held a per-faction list spell no faction id.</summary>
    [Fact]
    public void NoColorCodeSpellsAFactionId()
    {
        var repo = Directory.GetParent(Catalog.Root.Shipped)!.FullName;
        foreach (var file in new[]
                 {
                     Path.Combine(repo, "unity", "Assets", "Scripts", "Runtime", "Colors.cs"),
                     Path.Combine(repo, "unity", "Assets", "Scripts", "Runtime", "HudView.cs"),
                     Path.Combine(repo, "src", "GrandSluggers.Play", "Palette.cs"),
                 })
        {
            var text = File.ReadAllText(file);
            foreach (var faction in Factions.Rows.Keys)
                Assert.DoesNotContain("\"" + faction + "\"", text, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("""{"factions":[{"id":"x","body":"#123456","accent":"#123456"}]}""", ".skin is missing")]
    [InlineData("""{"factions":[{"id":"x","body":"#123456","accent":"#123456","skin":"#123456","cap":"#000000"}]}""", ".cap is not a key here")]
    [InlineData("""{"factions":[{"id":"x","body":"#12345","accent":"#123456","skin":"#123456"}]}""", "a color is")]
    [InlineData("""{"factions":[{"id":"x","body":"#123456","accent":"#123456","skin":"#123456"},{"id":"x","body":"#123456","accent":"#123456","skin":"#123456"}]}""", "repeats faction x")]
    public void TheReaderIsStrict(string json, string expected)
    {
        var e = Assert.Throws<InvalidDataException>(() => FactionLooks.Parse(JsonNode.Parse(json), "factions.json"));
        Assert.Contains(expected, e.Message, StringComparison.Ordinal);
    }
}
