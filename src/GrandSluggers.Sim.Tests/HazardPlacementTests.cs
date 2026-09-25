using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Where a hazard may stand (§14, §0.3 FD-19, <c>SF-23</c>; F4-e, #862). A hazard may sit anywhere except the four running
/// lanes, the mound-to-plate lane, the three bag pads, the mound and the plate area, and the content validator refuses a park
/// file that puts one there. The ground is the diamond's own (<see cref="ParkDiamond.PathWidth"/>,
/// <see cref="ParkDiamond.BagPadR"/>, <see cref="ParkDiamond.MoundR"/>, <see cref="ParkDiamond.HomePackedR"/>) and the bags
/// and rubber are the root's own <c>infield.json</c>; the disc is the hazard's own radius.
///
/// <para>
/// <b>The five volumes (FD-19-R1, Jack, 2026-09-22).</b> Crystal's three freeze volumes and Ember's two shallow lava pits
/// were moved outward from home, along the ray through their own centres, until each cleared the diamond; they stand where
/// §14's table puts them.
/// </para>
/// </summary>
public sealed class HazardPlacementTests
{
    static readonly string Shipped = global::GrandSluggers.Sim.Tests.Shipped.Content.Root.Shipped;
    static readonly DataRoot Root = new(Shipped);
    static readonly ContentCatalog Game = ContentCatalog.Load(Root);

    static readonly JsonDocumentOptions JsonComments = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    static string Ft(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    // ---------------------------------------------------------------------------------
    // SF-23: the rule
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// SF-23 on the data as it stands: every hazard in every park clears every lane, pad, the mound and the plate area by its
    /// own radius, and the validator has nothing to say about the root.
    ///
    /// <para>
    /// Read through the one resolution (#895, FD-11, map finding 31): the list is the park a night match plays — the day
    /// block, then the night block, so Funfair's three mouths are measured and the count is 26 — and each instance is also
    /// measured at its night disc (<see cref="ParkHazards.NightDiscFt"/>). The one row with a night number, Ember's breath,
    /// clears by 32 ft at 1.6 × its radius.
    /// </para>
    /// </summary>
    [Fact]
    public void SF23_EveryHazardClearsTheLanesPadsMoundAndPlate()
    {
        Assert.Empty(ContentDataValidator.Validate(Root));
        var measured = 0;
        foreach (var id in Game.ParkPickOrder)
        {
            // Every instance the park stands, day or night: the day's (a type night clears included) and the night block's.
            var authored = Game.Parks[id];
            var all = authored.Hazards.Concat(authored.Night?.Hazards ?? []).ToList();
            for (var i = 0; i < all.Count; i++)
            {
                var h = all[i];
                foreach (var disc in new[] { h.Radius, ParkHazards.NightDiscFt(h.Radius, Game.Rules.Hazards.Of(h.Type)) })
                {
                    var crossings = HazardPlacement.Crossings(h.X, h.Z, disc, Game.Rules.Infield);
                    Assert.True(crossings.Count == 0,
                        $"{id} hazard[{i}] {h.Type} at ({h.X}, {h.Z}) r {disc} crosses "
                        + string.Join(", ", crossings.Select(c => $"{c.What} by {Ft(c.ByFt)} ft")));
                    Assert.True(HazardPlacement.ClearanceFt(h.X, h.Z, disc, Game.Rules.Infield) >= 0);
                }
                measured++;
            }
        }
        // Every hazard of every park was measured, not just the ones FD-19-R1 moved (the marsh's three lily pads too).
        Assert.Equal(33, measured);

        // Finding 31, measured: the breath at its night disc.
        var breath = Game.Parks[ParkId.Ember].Hazards.Single(h => h.Type == HazardType.FireBreath);
        var night = ParkHazards.NightDiscFt(breath.Radius, Game.Rules.Hazards.Of(HazardType.FireBreath));
        Assert.Equal(32, Math.Round(HazardPlacement.ClearanceFt(breath.X, breath.Z, night, Game.Rules.Infield)));
    }

    /// <summary>
    /// SF-23 by name: a volume on each lane, on each pad, on the mound and on the plate is refused, and the refusal names the
    /// park file it was written in, the hazard's index and type, what it crosses and by how many feet. The fixture is built on
    /// the root's own diamond.
    /// </summary>
    [Fact]
    public void SF23_AVolumeOnALanePadMoundOrPlateIsRefusedByName()
    {
        using var fixture = new PlacementFixture();
        var infield = Game.Rules.Infield;
        var c = infield.CornerFt;
        var s = infield.SecondFt;
        var m = infield.MoundFt;
        const double r = 2;
        var lane = r + ParkDiamond.PathWidth * 0.5;
        var pad = r + ParkDiamond.BagPadR;
        // Where each fixture volume stands, and the deepest thing it crosses. A pad, the mound and the plate also sit on the
        // lanes that end there; the deepest crossing is the pad itself.
        (double X, double Z, string What, double By)[] cases =
        [
            (c / 2, c / 2, "the home-first lane", lane),
            (c / 2, (c + s) / 2, "the first-second lane", lane),
            (-c / 2, (c + s) / 2, "the second-third lane", lane),
            (-c / 2, c / 2, "the third-home lane", lane),
            (0, m / 2, "the mound-to-plate lane", lane),
            (c, c, "first base's pad", pad),
            (0, s, "second base's pad", pad),
            (-c, c, "third base's pad", pad),
            (0, m, "the mound", r + ParkDiamond.MoundR),
            (0, 0, "the plate area", r + ParkDiamond.HomePackedR)
        ];
        fixture.Park(ParkId.Crystal, json =>
        {
            var hazards = new JsonArray();
            foreach (var (x, z, _, _) in cases)
                hazards.Add(new JsonObject { ["type"] = "freeze_volume", ["x"] = x, ["z"] = z, ["radius"] = r });
            json["hazards"] = hazards;
        });

        var errors = ContentDataValidator.Validate(fixture.Root());
        var file = fixture.ParkFile(ParkId.Crystal);
        var placement = errors.Where(e => e.Contains("SF-23", StringComparison.Ordinal)).ToList();
        Assert.Equal(cases.Length, placement.Count);
        string Refusal(int i) => Assert.Single(placement, e => e.Contains($"hazard[{i}] ", StringComparison.Ordinal));
        for (var i = 0; i < cases.Length; i++)
        {
            var (_, _, what, by) = cases[i];
            var refusal = Refusal(i);
            Assert.StartsWith(file + ": park 'crystal-rink' ", refusal, StringComparison.Ordinal);
            Assert.Contains($"hazard[{i}] freeze_volume at (", refusal, StringComparison.Ordinal);
            // Deepest first: the thing it is on is the first thing the refusal names.
            Assert.Contains($" crosses {what} by {Ft(by)} ft", refusal, StringComparison.Ordinal);
        }
        // The lanes that end at a pad, the mound or the plate are named too, each by its own depth.
        Assert.Contains($"crosses first base's pad by {Ft(pad)} ft, the home-first lane by {Ft(lane)} ft, "
            + $"the first-second lane by {Ft(lane)} ft;", Refusal(5), StringComparison.Ordinal);
        Assert.Contains($"crosses the mound by {Ft(r + ParkDiamond.MoundR)} ft, the mound-to-plate lane by {Ft(lane)} ft;",
            Refusal(8), StringComparison.Ordinal);
        Assert.Contains($"crosses the plate area by {Ft(r + ParkDiamond.HomePackedR)} ft, the home-first lane by {Ft(lane)} ft, "
            + $"the third-home lane by {Ft(lane)} ft, the mound-to-plate lane by {Ft(lane)} ft;", Refusal(9), StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root()));
    }

    /// <summary>
    /// SF-23 measures a root on its own diamond. Crystal's deep volume, (7, 131) r 7, clears second base at 113.14 ft by
    /// less than a foot, and on a 90-ft diamond — second at 127.28 — it would stand on second base's pad. The same disc gets
    /// two answers because the diamonds differ, not because the rule does.
    /// </summary>
    [Fact]
    public void SF23_EachRootIsMeasuredOnItsOwnDiamond()
    {
        var shipped = Game.Rules.Infield;
        Assert.Equal((56.57, 113.14, 53.78), (shipped.CornerFt, shipped.SecondFt, shipped.MoundFt));
        var ninety = shipped with { BaselineFt = 90, MoundFt = 60.5, CornerFt = 63.64, SecondFt = 127.28, InnerHalfFt = 50, BackArcFt = 92 };

        Assert.Empty(HazardPlacement.Crossings(7, 131, 7, shipped));
        Assert.Equal(Diamond.Dist(7, 131, 0, 113.14) - 7 - ParkDiamond.BagPadR, HazardPlacement.ClearanceFt(7, 131, 7, shipped), 9);
        Assert.Equal(0.18, Math.Round(HazardPlacement.ClearanceFt(7, 131, 7, shipped), 2));
        var onNinety = HazardPlacement.Crossings(7, 131, 7, ninety)[0];
        Assert.Equal("second base's pad", onNinety.What);
        Assert.Equal(7 + ParkDiamond.BagPadR - Diamond.Dist(7, 131, 0, 127.28), onNinety.ByFt, 9);
    }

    /// <summary>
    /// SF-23 is a rule about a root, not about a park file: an overlay that moves the bags to 70 ft and carries no park puts
    /// the shipped parks on the smaller diamond, and Canopy's shallow barrel then stands on the first-second lane and second
    /// base's pad. The refusal names the shipped park file, because that is the file the root read.
    /// </summary>
    [Fact]
    public void SF23_BagsMovedWithoutTheirHazardsAreRefused()
    {
        var overlay = Path.Combine(Path.GetTempPath(), "grand-sluggers-placement-bags-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(overlay, "rules"));
            var infield = JsonNode.Parse(File.ReadAllText(Path.Combine(Shipped, "rules", "infield.json")), null, JsonComments)!.AsObject();
            infield["baselineFt"] = 70;
            infield["cornerFt"] = 49.5;
            infield["secondFt"] = 98.99;
            File.WriteAllText(Path.Combine(overlay, "rules", "infield.json"), infield.ToJsonString());

            var placement = ContentDataValidator.Validate(new DataRoot(Shipped, overlay))
                .Where(e => e.Contains("SF-23", StringComparison.Ordinal)).ToList();
            var refusal = Assert.Single(placement);
            Assert.StartsWith(Path.Combine(Shipped, "parks", "canopy-yard.json") + ": park 'canopy-yard' hazard[2] barrel at (5, 91) radius 3.5 ",
                refusal, StringComparison.Ordinal);
            Assert.Contains("crosses the first-second lane by 6.38 ft, second base's pad by 6.07 ft;", refusal, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(overlay)) Directory.Delete(overlay, recursive: true);
        }
    }

    /// <summary>
    /// SF-23: the disc is the hazard's own radius. A ball redirect's <c>reachPadFt</c> is how near a ball must land for the
    /// can to take it (#732), not ground a body stands on, so a can that clears by its radius is legal even where its radius
    /// plus the pad would not be; a volume whose own radius is that same disc is refused, by exactly what the pad would have
    /// crossed.
    /// </summary>
    [Fact]
    public void SF23_TheDiscIsTheHazardsOwnRadius()
    {
        var content = Game;
        var infield = content.Rules.Infield;
        var reach = content.Rules.Hazards.Of(HazardType.WarpPipe).ReachPadFt;
        Assert.Equal(5.6, reach);

        // A can of radius 2 standing 1 ft clear of the first-second lane, on the infield side of its midpoint.
        const double r = 2;
        var half = ParkDiamond.PathWidth * 0.5;
        var off = half + r + 1;
        var (mx, mz) = (infield.CornerFt / 2, (infield.CornerFt + infield.SecondFt) / 2);
        var (x, z) = (mx - off / Math.Sqrt(2), mz - off / Math.Sqrt(2));
        Assert.Equal(1, HazardPlacement.ClearanceFt(x, z, r, infield), 9);
        var withPad = HazardPlacement.Crossings(x, z, r + reach, infield);
        Assert.Equal("the first-second lane", withPad[0].What);
        Assert.Equal(reach - 1, withPad[0].ByFt, 9);

        using var fixture = new PlacementFixture();
        fixture.Park(ParkId.Funfair, json =>
        {
            var hazards = json["hazards"]!.AsArray();
            hazards[0]!["x"] = x;
            hazards[0]!["z"] = z;
            hazards[0]!["radius"] = r;
            hazards.Add(new JsonObject { ["type"] = "freeze_volume", ["x"] = x, ["z"] = z, ["radius"] = r + reach });
        });
        var funfair = content.Parks[ParkId.Funfair].Hazards.Count;
        var placement = ContentDataValidator.Validate(fixture.Root())
            .Where(e => e.Contains("SF-23", StringComparison.Ordinal)).ToList();
        var refusal = Assert.Single(placement);
        Assert.Contains($"hazard[{funfair}] freeze_volume at (", refusal, StringComparison.Ordinal);
        Assert.Contains($" crosses the first-second lane by {Ft(reach - 1)} ft", refusal, StringComparison.Ordinal);
        Assert.DoesNotContain(placement, e => e.Contains("hazard[0] ", StringComparison.Ordinal));

        // The finding for Jack, not acted on: one ball redirect in the data is legal by its radius and would cross the
        // first-second lane if the pad were counted.
        (string Park, int Index, string What, double By)[] padFindings = [(ParkId.Canopy, 2, "the first-second lane", 1.98)];
        foreach (var (park, index, what, by) in padFindings)
        {
            var h = content.Parks[park].Hazards[index];
            Assert.Equal(HazardPattern.BallRedirect, content.Rules.Hazards.Of(h.Type).Pattern);
            Assert.Empty(HazardPlacement.Crossings(h.X, h.Z, h.Radius, infield));
            var deepest = HazardPlacement.Crossings(h.X, h.Z, h.Radius + reach, infield)[0];
            Assert.Equal((what, by), (deepest.What, Math.Round(deepest.ByFt, 2)));
        }
    }

    // ---------------------------------------------------------------------------------
    // FD-19-R1: the five volumes
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// FD-19-R1: the five moved volumes stand where §14's table puts them, each keeps its type and radius, each clears the
    /// diamond, and nothing else at either park is anywhere else.
    /// </summary>
    [Fact]
    public void FD19R1_TheFiveVolumesStandWhereTheTableSays()
    {
        (string Park, int Index, string Type, double X, double Z, double Radius)[] after =
        [
            (ParkId.Crystal, 0, "freeze_volume", 47, 83, 5.6),
            (ParkId.Crystal, 1, "freeze_volume", -44, 86, 5.6),
            (ParkId.Crystal, 2, "freeze_volume", 7, 131, 7),
            (ParkId.Ember, 0, "lava_pit", 44, 89, 7),
            (ParkId.Ember, 1, "lava_pit", -40, 92, 7)
        ];
        foreach (var (parkId, index, type, x, z, radius) in after)
        {
            var now = Game.Parks[parkId].Hazards[index];
            Assert.Equal((type, x, z, radius), (now.Type, now.X, now.Z, now.Radius));
            Assert.True(HazardPlacement.ClearanceFt(now.X, now.Z, now.Radius, Game.Rules.Infield) >= 0, $"{parkId} hazard[{index}]");
        }

        // Nothing else at either park: Crystal has no other hazard, and Ember's deep pit, breath and statue follow.
        Assert.Equal(3, Game.Parks[ParkId.Crystal].Hazards.Count);
        Assert.Equal(
            [("lava_pit", 8.0, 132.0, 8.4), ("fire_breath", 0.0, 175.0, 11.2), ("statue", 0.0, 217.0, 7.0)],
            Game.Parks[ParkId.Ember].Hazards.Skip(2).Select(h => (h.Type, h.X, h.Z, h.Radius)));
    }

    /// <summary>A copy of the data root that a test may break.</summary>
    sealed class PlacementFixture : IDisposable
    {
        readonly string _temp;
        readonly string _shipped;

        public PlacementFixture()
        {
            _temp = Path.Combine(Path.GetTempPath(), "grand-sluggers-placement-" + Guid.NewGuid().ToString("N"));
            _shipped = Path.Combine(_temp, "data");
            Copy(Shipped, _shipped);
        }

        public DataRoot Root() => new(_shipped);

        /// <summary>The park file a refusal names.</summary>
        public string ParkFile(string id) => Path.GetFullPath(Path.Combine(_shipped, "parks", id + ".json"));

        public void Park(string id, Action<JsonObject> change)
        {
            var path = ParkFile(id);
            var json = JsonNode.Parse(File.ReadAllText(path), null, JsonComments)!.AsObject();
            change(json);
            File.WriteAllText(path, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        static void Copy(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
                File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)));
        }

        public void Dispose()
        {
            if (Directory.Exists(_temp)) Directory.Delete(_temp, recursive: true);
        }
    }
}
