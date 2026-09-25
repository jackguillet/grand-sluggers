using System.Text.RegularExpressions;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Hazards drawn by pattern at their true size, and the park dress by slot (FD-16, FD-16-R1, FD-09, FR-13, FR-04; F6-d):
/// every hazard type names a toy in <c>data/art/hazard-actors.json</c>, every acting pattern a ring color; the ring is the
/// disc the sim reads, reach pad included; and the Unity view picks the dress and the toys by data, never by a park id or a
/// hazard type string. The source rows are the falsifier for the Unity half, since a Unity component cannot run here.
/// </summary>
public sealed class HazardActorsTests
{
    static readonly ContentCatalog Catalog = Shipped.Content;

    /// <summary>Every type of the library has a toy and every acting pattern a ring; the shipped rows validate.</summary>
    [Fact]
    public void FD16_EveryHazardTypeHasAToyAndEveryActingPatternARing()
    {
        var actors = Catalog.Art.Actors;
        Assert.Empty(actors.Validate());
        foreach (var type in HazardType.All)
            Assert.Contains(actors.Toy(type)!, HazardActors.Builders);
        Assert.Equal(HazardPattern.Hazards.Order(), actors.Rings.Keys.Order());
        Assert.Equal(HazardActors.WarpCan, actors.Toy(HazardType.WarpPipe));
        Assert.Equal(HazardActors.MidwayTrain, actors.Toy(HazardType.Train));
    }

    /// <summary>The validator names a type with no toy, a toy that is not one, a ring for a pattern that does not act and one missing.</summary>
    [Fact]
    public void FD16_TheValidatorRefusesAMissingToyAnUnknownToyAndAWrongRing()
    {
        var bad = HazardActors.Parse(System.Text.Json.Nodes.JsonNode.Parse("""
            {"toys": {"freezeVolume": "snowman"}, "rings": {"decoration": "#FFFFFF"}}
            """), "t");
        var errors = bad.Validate();
        Assert.Contains("hazard actor freezeVolume names snowman, which is not a toy", errors);
        Assert.Contains("hazard actor missing for lavaPit", errors);
        Assert.Contains("hazard ring decoration is not a pattern that acts", errors);
        Assert.Contains("hazard ring missing for pattern statusVolume", errors);
    }

    /// <summary>
    /// The ring is the disc the sim reads: a warp pipe's radius plus its reach pad (the pad nobody saw before), the
    /// fire breath's radius × 1.6 at night, a freeze volume's radius by day and night.
    /// </summary>
    [Fact]
    public void FD16_TheRingIsTheDiscTheSimReads()
    {
        var hazards = Catalog.Rules.Hazards;
        var warp = hazards.Of(HazardType.WarpPipe);
        Assert.Equal(6 + warp.ReachPadFt, HazardActors.PlayDiscFt(6, warp, night: false));
        Assert.True(warp.ReachPadFt > 0, "the redirect has a pad the ring must include");
        var breath = hazards.Of(HazardType.FireBreath);
        Assert.Equal(ParkHazards.NightDiscFt(10, breath), HazardActors.PlayDiscFt(10, breath, night: true));
        Assert.Equal(10 * breath.NightRadiusMul, HazardActors.PlayDiscFt(10, breath, night: true), 9);
        Assert.Equal(10.0, HazardActors.PlayDiscFt(10, breath, night: false));
        var freeze = hazards.Of(HazardType.FreezeVolume);
        Assert.Equal(7.0, HazardActors.PlayDiscFt(7, freeze, night: true));
    }

    static string View()
    {
        var repo = Directory.GetParent(Catalog.Root.Shipped)!.FullName;
        return File.ReadAllText(Path.Combine(repo, "unity", "Assets", "Scripts", "Runtime", "ParkView.cs"));
    }

    /// <summary>
    /// FR-04: a park the Harbor kit does not draw is the greybox, and each hazard's toy is picked by its row — the five per-park methods and
    /// the type-string switch are gone, no park id or hazard type string is compared, and Rooftop's two AC units that were
    /// never in its data are gone with them.
    /// </summary>
    [Fact]
    public void FR04_TheDressAndTheToysArePickedByDataNotByAParkIdOrATypeString()
    {
        var view = View();
        foreach (var retired in new[] { "void CrystalGarden(", "void FunfairGrounds(", "void RooftopDeck(", "void CanopyGrounds(", "void EmberCourtyard(", "void FunfairNightHook(", "void Stands(bool",
                     // FD-16-R2 (#1045): the named dress builders went too; a non-Harbor park is the greybox.
                     "void DressBy(", "void FerrisWheel(", "void KeepCastle(", "void RoyalPalace(", "void RooftopSkyline(", "void CircusTents(" })
            Assert.DoesNotContain(retired, view);
        Assert.DoesNotContain("park.Id ==", view);
        Assert.DoesNotContain("new Hazard(", view);
        Assert.DoesNotMatch(new Regex(@"case\s+""", RegexOptions.None), view);
        Assert.DoesNotContain("HazardType.", view);
        Assert.Contains("if (!placed) Dress(park, kitRow);", view);
        Assert.Contains("Ring(h, (float)HazardActors.PlayDiscFt(h.Radius, row, _night), Look.Of(ring));", view);
        foreach (var toy in HazardActors.Builders)
        {
            var constant = typeof(HazardActors).GetFields().Single(f => f.IsLiteral && (string)f.GetRawConstantValue()! == toy).Name;
            Assert.Contains("case HazardActors." + constant + ":", view);
        }
        // Every non-Harbor dress builder the catalog allows is named by the view, so a slot the validator passes is one it draws.
        foreach (var field in typeof(ParkKitSlots).GetFields().Where(f => f.IsLiteral))
        {
            var value = (string)field.GetRawConstantValue()!;
            var isDress = new[] { ParkKitSlots.Stands, ParkKitSlots.Backdrop, ParkKitSlots.Night, ParkKitSlots.Props }
                .Any(slot => ParkKitSlots.Builders[slot].Contains(value));
            if (isDress && !value.StartsWith("harbor-", StringComparison.Ordinal))
                Assert.Contains("ParkKitSlots." + field.Name, view);
        }
    }
}
