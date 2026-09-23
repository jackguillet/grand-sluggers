using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Each park's light, sky and greybox colors are data (FD-16, FR-04; F6-c): the <c>light</c> and <c>sky</c> slots of its kit
/// row name rows of <c>data/art/looks.json</c>, and its <c>palette</c> colors the greybox. The rows carry the numbers of the
/// eleven <c>Look.Rig*</c> methods they replace and the palettes the colors of the old id chains, so every park looks as it
/// did; the rows pinned here are copied from those methods.
/// </summary>
public sealed class ParkLooksTests
{
    static readonly ContentCatalog Catalog = ContentCatalog.Load();
    static ParkLooks Looks => Catalog.Art.Looks;

    static ParkKitSlot Kit(string id)
    {
        Assert.True(Catalog.Art.TryPark(id, out var kit), id);
        return kit;
    }

    static void Rgb(double r, double g, double b, LookColor c)
    {
        Assert.Null(c.Hex);
        Assert.Equal((r, g, b), (c.R, c.G, c.B));
    }

    /// <summary>Every park names one light and one sky from the table and carries a palette; the old rigs map one to one.</summary>
    [Fact]
    public void FD16_EveryParkNamesALightASkyAndAPalette()
    {
        var expected = new Dictionary<string, string>
        {
            ["harbor-diamond"] = "harbor", ["crystal-rink"] = "ice-garden", ["funfair-park"] = "carnival",
            ["rooftop-city"] = "neon", ["canopy-yard"] = "canopy", ["ember-keep"] = "courtyard",
        };
        Assert.Equal(expected.Keys.OrderBy(k => k), Catalog.Parks.Keys.OrderBy(k => k));
        foreach (var (park, look) in expected)
        {
            var kit = Kit(park);
            Assert.Equal(look, kit.Filler(ParkKitSlots.Light));
            Assert.Equal(look, kit.Filler(ParkKitSlots.Sky));
            Assert.NotNull(kit.Palette);
            Assert.Empty(ParkKitSlots.Validate(kit, Looks));
        }
        Assert.Equal(expected.Values.Order(), Looks.Skies.Keys.Order());
        Assert.Equal(expected.Values.Order(), Looks.Lights.Keys.Order());
    }

    /// <summary>
    /// The rows are the rigs' numbers: Harbor's afternoon and night, Crystal's night blackout, and Rooftop and Canopy, which
    /// had one rig day and night, so their night is null and plays the day.
    /// </summary>
    [Fact]
    public void FD16_TheRowsCarryTheRetiredRigsNumbers()
    {
        var harbor = Looks.Skies["harbor"].At(false);
        Rgb(0.52, 0.70, 0.88, harbor.Color);
        Rgb(0.78, 0.80, 0.72, harbor.Fog.Color);
        Assert.Equal((380.0, 820.0), (harbor.Fog.Start, harbor.Fog.End));
        var afternoon = Looks.Lights["harbor"].At(false);
        Rgb(0.58, 0.74, 0.90, afternoon.AmbientSky);
        Rgb(1.0, 0.91, 0.72, afternoon.Sun.Color);
        Assert.Equal((1.55, 38.0, 42.0, 0.0, true), (afternoon.Sun.Intensity, afternoon.Sun.EulerX, afternoon.Sun.EulerY, afternoon.Sun.EulerZ, afternoon.Sun.Shadows));
        Assert.Equal((0.32, 58.0, -78.0, false), (afternoon.Fill.Intensity, afternoon.Fill.EulerX, afternoon.Fill.EulerY, afternoon.Fill.Shadows));
        Assert.Equal(0.42, Looks.Lights["harbor"].At(true).Sun.Intensity);

        var blackout = Looks.Lights["ice-garden"].At(true);
        Assert.Equal((0.06, false), (blackout.Sun.Intensity, blackout.Sun.Shadows));
        Assert.Equal((70.0, 380.0), (Looks.Skies["ice-garden"].At(true).Fog.Start, Looks.Skies["ice-garden"].At(true).Fog.End));

        foreach (var id in new[] { "neon", "canopy" })
        {
            Assert.Null(Looks.Skies[id].Night);
            Assert.Null(Looks.Lights[id].Night);
            Assert.Same(Looks.Lights[id].Day, Looks.Lights[id].At(true));
        }
        Assert.Equal(1.55, Looks.Lights["courtyard"].At(true).Sun.Intensity);
    }

    /// <summary>
    /// The palettes are the old chains' colors: a named color stays its hex (read the way <c>Colors.Hex</c> reads it), a
    /// literal stays its numbers; ice, ash and Rooftop draw untextured grass; only Funfair alternates its wall panels.
    /// </summary>
    [Fact]
    public void FD16_ThePalettesAreTheOldChainsColors()
    {
        var harbor = Kit("harbor-diamond").Palette!;
        Assert.Equal((0x3EA84E, "grass", 18.0, 0.08), (harbor.Grass.Color.Hex, harbor.Grass.Texture, harbor.Grass.Tile, harbor.Grass.Smooth));
        Assert.Equal((0x2E7CB0, 0.85), (harbor.Water.Color.Hex, harbor.Water.Smooth));
        Assert.Equal((0xC49A60, "dirt", 8.0, 0.12), (harbor.Dirt.Color.Hex, harbor.Dirt.Texture, harbor.Dirt.Tile, harbor.Dirt.Smooth));

        var crystal = Kit("crystal-rink").Palette!;
        Assert.Equal((0xBED8F0, (string?)null, 0.72), (crystal.Grass.Color.Hex, crystal.Grass.Texture, crystal.Grass.Smooth));
        Rgb(0.74, 0.84, 0.90, crystal.Dirt.Color);
        Assert.Equal((6.0, 0.35), (crystal.Dirt.Tile, crystal.Dirt.Smooth));
        Assert.Equal((0.92, 0.62, 0.75), (crystal.Water.Smooth, crystal.Wall.Smooth, crystal.Cap.Smooth));
        Assert.Equal(0xE878A8, crystal.Pole.Color.Hex);

        Assert.Equal(0xFF7A20, Kit("ember-keep").Palette!.Wall.Color.Hex);
        Assert.Null(Kit("ember-keep").Palette!.Grass.Texture);
        Rgb(0.32, 0.32, 0.34, Kit("rooftop-city").Palette!.Grass.Color);
        Assert.Equal(0.18, Kit("rooftop-city").Palette!.Grass.Smooth);
        Assert.Null(Kit("rooftop-city").Palette!.Grass.Texture);
        Assert.Equal(0xE8BC28, Kit("rooftop-city").Palette!.Pole.Color.Hex);
        Assert.Equal("grass", Kit("canopy-yard").Palette!.Grass.Texture);
        foreach (var park in Catalog.Parks.Keys)
            Assert.Equal(park == "funfair-park", Kit(park).Palette!.WallAlt is not null);
        Rgb(0.96, 0.90, 0.72, Kit("funfair-park").Palette!.WallAlt!.Color);
    }

    /// <summary>The reader is strict: an unknown key, a missing key, a bad color, a texture that is not one, and a tile with no texture stop the load.</summary>
    [Theory]
    [InlineData("""{"color":"#12345","smooth":0.1}""", "is #12345; a color is")]
    [InlineData("""{"color":[0.1,0.2],"smooth":0.1}""", ".color must be 3 numbers")]
    [InlineData("""{"color":[0.1,0.2,1.5],"smooth":0.1}""", "has 1.5; each channel is 0 to 1")]
    [InlineData("""{"color":"#123456"}""", ".smooth is missing")]
    [InlineData("""{"color":"#123456","smooth":0.1,"shine":1}""", ".shine is not a key here")]
    [InlineData("""{"color":"#123456","smooth":0.1,"texture":"ice","tile":2}""", "is ice; a texture is grass or dirt")]
    [InlineData("""{"color":"#123456","smooth":0.1,"tile":2}""", "names a tile with no texture")]
    public void FD16_TheReaderRefusesABadSurface(string grass, string expected)
    {
        var surface = """{"color":"#123456","smooth":0.1}""";
        var palette = JsonNode.Parse($$"""{"grass":{{grass}},"water":{{surface}},"dirt":{{surface}},"wall":{{surface}},"wallAlt":null,"cap":{{surface}},"pole":{{surface}}}""");
        var e = Assert.Throws<InvalidDataException>(() => ParkLooks.ParsePalette(palette, "p"));
        Assert.Contains(expected, e.Message, StringComparison.Ordinal);
    }

    /// <summary>A light or sky slot must name a row of the table, and a kit row must carry a palette.</summary>
    [Fact]
    public void FD16_TheValidatorRefusesAnUnknownLookAnEmptyLookAndNoPalette()
    {
        var kit = Kit("crystal-rink");
        var slots = new Dictionary<string, string?>(kit.Slots!, StringComparer.Ordinal) { [ParkKitSlots.Light] = "moon", [ParkKitSlots.Sky] = null };
        var errors = ParkKitSlots.Validate(kit with { Slots = slots, Palette = null }, Looks);
        Assert.Contains("park kit crystal-rink slot light names moon, which is not a light in looks.json", errors);
        Assert.Contains("park kit crystal-rink must name a sky from looks.json", errors);
        Assert.Contains("park kit crystal-rink names no palette", errors);
    }

    /// <summary>FR-04: no park id chooses a light, a sky or a greybox color any more; the rigs are gone from the code.</summary>
    [Fact]
    public void FR04_NoParkIdChoosesALookInTheCode()
    {
        var repo = Directory.GetParent(Catalog.Root.Shipped)!.FullName;
        var runtime = Path.Combine(repo, "unity", "Assets", "Scripts", "Runtime");
        var look = File.ReadAllText(Path.Combine(runtime, "Look.cs"));
        var view = File.ReadAllText(Path.Combine(runtime, "ParkView.cs"));
        Assert.DoesNotContain("public static void Rig", look);
        Assert.DoesNotContain("Look.Rig", view);
        Assert.Contains("Look.Apply(Camera.main, skyLook, lightLook, night)", view);
        // The dress methods keep their own colors until F6-d; the ground, water and field-kit colors are the palette's.
        foreach (var chain in new[] { "var grassCol", "var waterCol", "var skipGrass", "FieldSkin(park," })
            Assert.DoesNotContain(chain, view);
        Assert.Contains("FieldSkin(palette, dirtMat)", view);
    }
}
