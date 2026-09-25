using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The kit slots (FD-16 B, FR-13; F6-b): every park names every slot in <c>data/art/parks.json</c>, by a builder the
/// presentation owns or null for the greybox; the validator refuses anything else; Harbor fills exactly the pieces its kit
/// draws, so it looks as it did; and the kit is chosen by the lawn slot, never by a park id.
/// </summary>
public sealed class ParkKitSlotsTests
{
    static readonly ContentCatalog Catalog = Shipped.Content;

    static ParkKitSlot Kit(string id)
    {
        Assert.True(Catalog.Art.TryPark(id, out var kit), id);
        return kit;
    }

    /// <summary>Every catalog park has a kit row naming all ten slots, and the shipped rows validate.</summary>
    [Fact]
    public void FD16_EveryParkNamesEverySlotAndTheShippedRowsValidate()
    {
        foreach (var park in Catalog.Parks.Keys)
        {
            var kit = Kit(park);
            Assert.NotNull(kit.Slots);
            Assert.Equal(ParkKitSlots.All.OrderBy(s => s), kit.Slots!.Keys.OrderBy(s => s));
            Assert.Empty(ParkKitSlots.Validate(kit));
        }
        Assert.DoesNotContain(Catalog.Art.Validate(Catalog), e => e.StartsWith("park kit", StringComparison.Ordinal));
        Assert.Equal(ParkKitSlots.All.OrderBy(s => s), ParkKitSlots.Builders.Keys.OrderBy(s => s));
    }

    /// <summary>
    /// Harbor fills the seven pieces <c>HarborKit</c> draws today, and every park names its light and sky (F6-c). The five
    /// other parks are the plain greybox (FR-13, #1045): every dress slot empty, <c>toy-actors</c> for their hazards.
    /// </summary>
    [Fact]
    public void FD16_HarborFillsItsKitsSevenPiecesAndTheOtherParksAreTheGreybox()
    {
        var harbor = Kit(ParkId.Harbor);
        Assert.Equal(ParkKitSlots.HarborLawn, harbor.Filler(ParkKitSlots.Lawn));
        Assert.Equal(ParkKitSlots.HarborDugouts, harbor.Filler(ParkKitSlots.Dugouts));
        Assert.Equal(ParkKitSlots.HarborWall, harbor.Filler(ParkKitSlots.Wall));
        Assert.Equal(ParkKitSlots.HarborScoreboard, harbor.Filler(ParkKitSlots.Scoreboard));
        Assert.Equal(ParkKitSlots.HarborStands, harbor.Filler(ParkKitSlots.Stands));
        Assert.Equal(ParkKitSlots.HarborTown, harbor.Filler(ParkKitSlots.Backdrop));
        Assert.Equal(ParkKitSlots.HarborFireworks, harbor.Filler(ParkKitSlots.Night));
        Assert.Equal([ParkKitSlots.Props, ParkKitSlots.HazardActors], harbor.Empty);
        // Every other park is the plain greybox (FR-13, #1045): no dress slot names a builder; light, sky and palette are data.
        string[] greybox = [ParkKitSlots.Lawn, ParkKitSlots.Dugouts, ParkKitSlots.Wall, ParkKitSlots.Scoreboard,
            ParkKitSlots.Stands, ParkKitSlots.Backdrop, ParkKitSlots.Night, ParkKitSlots.Props];
        foreach (var park in Catalog.Parks.Keys.Where(p => p != ParkId.Harbor))
        {
            Assert.Equal(greybox, Kit(park).Empty);
            Assert.Equal(ParkKitSlots.ToyActors, Kit(park).Filler(ParkKitSlots.HazardActors));
            Assert.NotNull(Kit(park).Filler(ParkKitSlots.Light));
            Assert.NotNull(Kit(park).Filler(ParkKitSlots.Sky));
        }
        // Only Harbor's kit pieces are builders: no other park can name hand-built dress again.
        Assert.All(new[] { ParkKitSlots.Stands, ParkKitSlots.Backdrop, ParkKitSlots.Night },
            slot => Assert.Equal(ParkKitSlots.Builders[slot], ParkKitSlots.Builders[slot].Where(b => b.StartsWith("harbor-", StringComparison.Ordinal))));
        Assert.Empty(ParkKitSlots.Builders[ParkKitSlots.Props]);
    }

    /// <summary>The validator names each fault: a slot left out, a slot that is not one, a builder that is not the slot's, and a Harbor piece off the Harbor lawn.</summary>
    [Fact]
    public void FD16_TheValidatorRefusesAMissingSlotAnUnknownSlotAWrongBuilderAndAHarborPieceOffTheLawn()
    {
        var harbor = Kit(ParkId.Harbor);
        ParkKitSlot With(Action<Dictionary<string, string?>> change)
        {
            var slots = new Dictionary<string, string?>(harbor.Slots!, StringComparer.Ordinal);
            change(slots);
            return harbor with { Slots = slots };
        }
        Assert.Contains("park kit harbor-diamond must name slot sky (null leaves it empty)",
            ParkKitSlots.Validate(With(s => s.Remove(ParkKitSlots.Sky))));
        Assert.Contains("park kit harbor-diamond slot skybox is not a kit slot",
            ParkKitSlots.Validate(With(s => s["skybox"] = null)));
        Assert.Contains("park kit harbor-diamond slot wall names harbor-town, which is not a wall builder",
            ParkKitSlots.Validate(With(s => s[ParkKitSlots.Wall] = ParkKitSlots.HarborTown)));
        Assert.Contains("park kit harbor-diamond slot backdrop names harbor-town, a Harbor piece, but its lawn is not harbor-lawn",
            ParkKitSlots.Validate(With(s => s[ParkKitSlots.Lawn] = null)));
        Assert.Contains("park kit harbor-diamond names no slots", ParkKitSlots.Validate(harbor with { Slots = null }));
    }

    /// <summary>
    /// FR-04: the Unity side picks the Harbor kit and each piece of its dress by the slots, not by the park id. The source
    /// is the falsifier, since a Unity component cannot run here.
    /// </summary>
    [Fact]
    public void FR04_TheHarborKitIsChosenByItsSlotsNotByAParkId()
    {
        var repo = Directory.GetParent(Catalog.Root.Shipped)!.FullName;
        var kit = File.ReadAllText(Path.Combine(repo, "unity", "Assets", "Scripts", "Runtime", "HarborKit.cs"));
        var view = File.ReadAllText(Path.Combine(repo, "unity", "Assets", "Scripts", "Runtime", "ParkView.cs"));
        Assert.DoesNotContain("HarborPostcard.Owns", kit);
        Assert.Contains("OwnsDiamond = _slots.Fills(ParkKitSlots.Lawn, ParkKitSlots.HarborLawn)", kit);
        foreach (var (slot, builder, dress) in new[]
                 {
                     ("Dugouts", "HarborDugouts", "DressDugouts"), ("Wall", "HarborWall", "DressWall"),
                     ("Scoreboard", "HarborScoreboard", "DressScoreboard"), ("Stands", "HarborStands", "DressBleachers"),
                     ("Backdrop", "HarborTown", "DressTown"), ("Night", "HarborFireworks", "DressNight"),
                 })
            Assert.Contains($"if (Fills(ParkKitSlots.{slot}, ParkKitSlots.{builder})) {dress}();", kit);
        Assert.Contains("ArtBinder.ParkKit(park.Id).Fills(ParkKitSlots.Lawn, ParkKitSlots.HarborLawn)", view);
        Assert.DoesNotContain("kit == null && harbor", view);
    }
}
