using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The greyboxes' night rigs and rough backdrops (#1156; WD-03 A, WD-10 A, FD-11-R2, FD-16): every park but Harbor fills its
/// night and backdrop slots from rows in data, the rows stand out of play, the backdrops are baked by the one Blender script
/// into their slots, the new parks carry looks of their own, and no presentation code names a park or lights the field.
/// </summary>
public sealed class ParkDressTests
{
    static readonly ContentCatalog Catalog = Shipped.Content;
    static string Repo => Path.GetFullPath(Path.Combine(Catalog.Root.Shipped, ".."));

    [Fact]
    public void EveryParkButHarborFillsItsNightAndBackdropFromARow()
    {
        foreach (var park in Catalog.Parks.Keys)
        {
            Assert.True(Catalog.Art.TryPark(park, out var kit), park);
            if (park == ParkId.Harbor)
            {
                Assert.True(kit.Fills(ParkKitSlots.Backdrop, ParkKitSlots.HarborTown));
                Assert.True(kit.Fills(ParkKitSlots.Night, ParkKitSlots.HarborFireworks));
                continue;
            }
            Assert.True(kit.Fills(ParkKitSlots.Backdrop, ParkKitSlots.BlockoutBackdrop), park);
            Assert.True(kit.Fills(ParkKitSlots.Night, ParkKitSlots.NightRig), park);
            Assert.Single(Catalog.Art.Backdrops, b => b.Park == park);
            Assert.Single(Catalog.Art.NightRigs, r => r.Park == park);
        }
        Assert.Empty(ParkDress.Validate(Catalog, Catalog.Art.Parks, Catalog.Art.NightRigs, Catalog.Art.Backdrops,
            slot => File.Exists(Path.Combine(Repo, "unity", slot))));
    }

    [Fact]
    public void EveryBackdropIsBakedIntoItsSlotAndItsPlayerCopyByTheOneScript()
    {
        foreach (var row in Catalog.Art.Backdrops)
        {
            Assert.True(row.Placed, row.Park);
            var slot = Path.Combine(Repo, "unity", row.Slot);
            var player = Path.Combine(Repo, "unity", "Assets", "Resources", row.Resources + ".fbx");
            Assert.True(File.Exists(slot) && File.Exists(slot + ".meta"), slot);
            Assert.True(File.Exists(player) && File.Exists(player + ".meta"), player);
        }
        var script = File.ReadAllText(Path.Combine(Repo, "tools", "blender", "backdrop_blockout.py"));
        Assert.Contains("data/art/backdrops.json", script, StringComparison.Ordinal);
    }

    [Fact]
    public void AShapeOrARigPieceInPlayIsRefused()
    {
        var mesa = Catalog.MustPark(ParkId.Sunscorch);
        var kits = Catalog.Art.Parks;
        var inPlay = new ParkBackdrop(mesa.Id, "Assets/Art/Parks/x/backdrop.fbx", "Art/Parks/x/backdrop", false,
            [new BackdropShape("box", 0, 200, [40, 40, 40], "#FFFFFF")]);
        var lamp = new NightRig(mesa.Id, [new NightRigPiece("lantern", [0], 150, 12, 3, "#FFFFFF", "#FFFF00")]);
        var badKind = new ParkBackdrop(mesa.Id, "Assets/Art/Parks/x/backdrop.fbx", "Art/Parks/x/backdrop", false,
            [new BackdropShape("pyramid", 0, 800, [40, 40, 40], "red")]);
        var errors = ParkDress.Validate(Catalog, kits, [lamp], [inPlay, badKind], _ => false);
        Assert.Contains(errors, e => e.Contains("backdrop sunscorch-mesa box at 0° stands", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("night rig sunscorch-mesa lantern at 0° stands", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("shape kind 'pyramid'", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("color must be #RRGGBB", StringComparison.Ordinal));
        // A park that names a builder with no row is refused by name.
        Assert.Contains(ParkDress.Validate(Catalog, kits, [], [], _ => true),
            e => e.Contains("names night-rig but data/art/night-rigs.json has no row", StringComparison.Ordinal));
    }

    [Fact]
    public void TheFourNewParksLookLikeThemselves()
    {
        var mine = new Dictionary<string, string>
        {
            [ParkId.Stillwater] = "marsh", [ParkId.Coconut] = "island", [ParkId.Sunscorch] = "mesa", [ParkId.Summit] = "summit",
        };
        foreach (var (park, look) in mine)
        {
            Assert.True(Catalog.Art.TryPark(park, out var kit));
            Assert.Equal(look, kit.Filler(ParkKitSlots.Light));
            Assert.Equal(look, kit.Filler(ParkKitSlots.Sky));
            Assert.True(Catalog.Art.Looks.Lights.ContainsKey(look) && Catalog.Art.Looks.Skies.ContainsKey(look), look);
        }
        // No other park borrows one of them.
        foreach (var park in Catalog.Parks.Keys.Where(p => !mine.ContainsKey(p)))
        {
            Assert.True(Catalog.Art.TryPark(park, out var kit));
            Assert.DoesNotContain(kit.Filler(ParkKitSlots.Light), mine.Values);
        }
    }

    [Fact]
    public void TheDressCodeNamesNoParkAndLightsNothing()
    {
        var view = File.ReadAllText(Path.Combine(Repo, "unity", "Assets", "Scripts", "Runtime", "ParkDressView.cs"));
        foreach (var id in ParkId.All) Assert.DoesNotContain("\"" + id + "\"", view, StringComparison.Ordinal);
        // The glow is an unlit colour: the play light stays the one night rig for every park (FD-11-R2).
        Assert.DoesNotContain("AddComponent<Light>", view, StringComparison.Ordinal);
        Assert.DoesNotContain("new Light", view, StringComparison.Ordinal);
        var parkView = File.ReadAllText(Path.Combine(Repo, "unity", "Assets", "Scripts", "Runtime", "ParkView.cs"));
        Assert.Contains("ParkKitSlots.BlockoutBackdrop", parkView, StringComparison.Ordinal);
        Assert.Contains("ParkKitSlots.NightRig", parkView, StringComparison.Ordinal);
    }
}
