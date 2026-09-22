using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Where a hazard may stand (§14, §0.3 FD-19, <c>SF-23</c>; F4-e, #862). A hazard may sit anywhere except the four running
/// lanes, the mound-to-plate lane, the three bag pads, the mound and the plate area, and the content validator refuses a park
/// file that puts one there, on both data roots. The ground is the diamond's own (<see cref="ParkDiamond.PathWidth"/>,
/// <see cref="ParkDiamond.BagPadR"/>, <see cref="ParkDiamond.MoundR"/>, <see cref="ParkDiamond.HomePackedR"/>) and the bags
/// and rubber are the root's own <c>infield.json</c>; the disc is the hazard's own radius.
///
/// <para>
/// <b>The eight volumes (FD-19-R1, Jack, 2026-09-22).</b> Four status volumes crossed a lane on the shipped root and four on
/// <c>trials/c80</c>, one of them second base's pad. They are five shipped rows — the shipped Rink's deep volume had to move
/// for its trial twin's sake — and each moved outward from home along the ray through its own centre, by the smallest distance
/// at which the shipped disc clears the shipped diamond and its migrated twin clears the trial's, rounded to the whole foot
/// that still clears. The trial rows are what the accepted migration rule makes of the moved shipped rows, so
/// <see cref="CompactGeometryTests.HazardsMigrateByTheZoneTheySitInAndTheirRadiiTakeTheFenceScale"/> holds unedited.
/// </para>
/// </summary>
public sealed class HazardPlacementTests
{
    static readonly string Shipped = ContentCatalog.Load().Root.Shipped;
    static readonly string Overlay = Path.GetFullPath(Path.Combine(Shipped, "..", "trials", "c80"));
    static readonly DataRoot ShippedRoot = new(Shipped);
    static readonly DataRoot TrialRoot = new(Shipped, Overlay);
    static readonly ContentCatalog ShippedContent = ContentCatalog.Load(ShippedRoot);
    static readonly ContentCatalog TrialContent = ContentCatalog.Load(TrialRoot);

    static readonly JsonDocumentOptions JsonComments = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    static IEnumerable<(string Name, DataRoot Root, ContentCatalog Content)> Roots() =>
        [("shipped", ShippedRoot, ShippedContent), ("trials/c80", TrialRoot, TrialContent)];

    static string Ft(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    // ---------------------------------------------------------------------------------
    // SF-23: the rule
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// SF-23 on the data as it stands: every hazard in every park, on both roots, clears every lane, pad, the mound and the
    /// plate area by its own radius, and the validator has nothing to say about either root.
    /// </summary>
    [Fact]
    public void SF23_EveryHazardClearsTheLanesPadsMoundAndPlate()
    {
        foreach (var (name, root, content) in Roots())
        {
            Assert.Empty(ContentDataValidator.Validate(root));
            var measured = 0;
            foreach (var id in content.ParkPickOrder)
            {
                var park = content.Parks[id];
                for (var i = 0; i < park.Hazards.Count; i++)
                {
                    var h = park.Hazards[i];
                    var crossings = HazardPlacement.Crossings(h.X, h.Z, h.Radius, content.Rules.Infield);
                    Assert.True(crossings.Count == 0,
                        $"{name} {id} hazard[{i}] {h.Type} at ({h.X}, {h.Z}) r {h.Radius} crosses "
                        + string.Join(", ", crossings.Select(c => $"{c.What} by {Ft(c.ByFt)} ft")));
                    Assert.True(HazardPlacement.ClearanceFt(h.X, h.Z, h.Radius, content.Rules.Infield) >= 0);
                    measured++;
                }
            }
            // Every hazard of all six parks was measured, not just the ones this child moved.
            Assert.Equal(26, measured);
        }
    }

    /// <summary>
    /// SF-23 by name: a volume on each lane, on each pad, on the mound and on the plate is refused, and the refusal names the
    /// park file it was written in, the hazard's index and type, what it crosses and by how many feet. The fixture is built on
    /// the root's own diamond, so on the trial the lanes are the 80-ft ones and the file is the overlay's.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SF23_AVolumeOnALanePadMoundOrPlateIsRefusedByName(bool trial)
    {
        using var fixture = new PlacementFixture(trial);
        var infield = (trial ? TrialContent : ShippedContent).Rules.Infield;
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
        fixture.Park("crystal-rink", json =>
        {
            var hazards = new JsonArray();
            foreach (var (x, z, _, _) in cases)
                hazards.Add(new JsonObject { ["type"] = "freeze_volume", ["x"] = x, ["z"] = z, ["radius"] = r });
            json["hazards"] = hazards;
        });

        var errors = ContentDataValidator.Validate(fixture.Root());
        var file = fixture.ParkFile("crystal-rink");
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
    /// SF-23 measures a root on its own diamond. The trial's deep Rink volume, (7, 131) r 7, clears the trial's second base
    /// at 113.14 ft by less than a foot and would stand on the shipped second base at 127.28; the old one, (7, 126), sat on
    /// the trial's pad. The same disc gets two answers because the diamonds differ, not because the rule does.
    /// </summary>
    [Fact]
    public void SF23_EachRootIsMeasuredOnItsOwnDiamond()
    {
        var shipped = ShippedContent.Rules.Infield;
        var trial = TrialContent.Rules.Infield;
        Assert.Equal((63.64, 127.28, 60.5), (shipped.CornerFt, shipped.SecondFt, shipped.MoundFt));
        Assert.Equal((56.57, 113.14, 53.78), (trial.CornerFt, trial.SecondFt, trial.MoundFt));

        Assert.Empty(HazardPlacement.Crossings(7, 131, 7, trial));
        Assert.Equal(Diamond.Dist(7, 131, 0, 113.14) - 7 - ParkDiamond.BagPadR, HazardPlacement.ClearanceFt(7, 131, 7, trial), 9);
        var onShipped = HazardPlacement.Crossings(7, 131, 7, shipped)[0];
        Assert.Equal("second base's pad", onShipped.What);
        Assert.Equal(7 + ParkDiamond.BagPadR - Diamond.Dist(7, 131, 0, 127.28), onShipped.ByFt, 9);

        var old = Assert.Single(HazardPlacement.Crossings(7, 126, 7, trial));
        Assert.Equal(("second base's pad", 4.36), (old.What, Math.Round(old.ByFt, 2)));
    }

    /// <summary>
    /// SF-23 is a rule about a root, not about a park file: an overlay that moves the bags to 80 ft and carries no park puts
    /// the shipped parks on the smaller diamond, and Canopy's shallow barrel then stands on the first-second lane and second
    /// base's pad. The refusal names the shipped park file, because that is the file the root read. <c>trials/c80</c> moves
    /// the bags and the hazards together, which is why it loads.
    /// </summary>
    [Fact]
    public void SF23_BagsMovedWithoutTheirHazardsAreRefused()
    {
        var overlay = Path.Combine(Path.GetTempPath(), "grand-sluggers-placement-bags-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(overlay, "rules"));
            var infield = JsonNode.Parse(File.ReadAllText(Path.Combine(Shipped, "rules", "infield.json")), null, JsonComments)!.AsObject();
            infield["baselineFt"] = 80;
            infield["cornerFt"] = 56.57;
            infield["secondFt"] = 113.14;
            File.WriteAllText(Path.Combine(overlay, "rules", "infield.json"), infield.ToJsonString());

            var placement = ContentDataValidator.Validate(new DataRoot(Shipped, overlay))
                .Where(e => e.Contains("SF-23", StringComparison.Ordinal)).ToList();
            var refusal = Assert.Single(placement);
            Assert.StartsWith(Path.Combine(Shipped, "parks", "canopy-yard.json") + ": park 'canopy-yard' hazard[2] barrel at (6, 102) radius 5 ",
                refusal, StringComparison.Ordinal);
            Assert.Contains("crosses the first-second lane by 6.37 ft, second base's pad by 4.35 ft;", refusal, StringComparison.Ordinal);
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
    /// crossed. Both roots, each with its own pad (8 shipped, 5.6 on the trial).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SF23_TheDiscIsTheHazardsOwnRadius(bool trial)
    {
        var content = trial ? TrialContent : ShippedContent;
        var infield = content.Rules.Infield;
        var reach = content.Rules.Hazards.Of(HazardType.WarpPipe).ReachPadFt;
        Assert.Equal(trial ? 5.6 : 8.0, reach);

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

        using var fixture = new PlacementFixture(trial);
        fixture.Park("funfair-park", json =>
        {
            var hazards = json["hazards"]!.AsArray();
            hazards[0]!["x"] = x;
            hazards[0]!["z"] = z;
            hazards[0]!["radius"] = r;
            hazards.Add(new JsonObject { ["type"] = "freeze_volume", ["x"] = x, ["z"] = z, ["radius"] = r + reach });
        });
        var funfair = content.Parks["funfair-park"].Hazards.Count;
        var placement = ContentDataValidator.Validate(fixture.Root())
            .Where(e => e.Contains("SF-23", StringComparison.Ordinal)).ToList();
        var refusal = Assert.Single(placement);
        Assert.Contains($"hazard[{funfair}] freeze_volume at (", refusal, StringComparison.Ordinal);
        Assert.Contains($" crosses the first-second lane by {Ft(reach - 1)} ft", refusal, StringComparison.Ordinal);
        Assert.DoesNotContain(placement, e => e.Contains("hazard[0] ", StringComparison.Ordinal));

        // The finding for Jack, not acted on: four ball redirects in the data are legal by their radius and would cross the
        // mound or the first-second lane if the pad were counted.
        (string Park, int Index, string What, double By)[] padFindings = trial
            ? [("canopy-yard", 2, "the first-second lane", 1.98)]
            : [("canopy-yard", 0, "the mound", 0.06), ("canopy-yard", 2, "the first-second lane", 4.37), ("funfair-park", 0, "the mound", 0.46)];
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
    // FD-19-R1: the eight volumes
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The shipped rows behind the eight offenders, where they stood before F4-e: Crystal's two shallow volumes and its deep
    /// one (whose trial twin sat on second base's pad), and Ember's two shallow lava pits.
    /// </summary>
    static readonly (string Park, int Index, string Type, double X, double Z, double Radius)[] Before =
    [
        ("crystal-rink", 0, "freeze_volume", 40, 70, 8),
        ("crystal-rink", 1, "freeze_volume", -45, 90, 8),
        ("crystal-rink", 2, "freeze_volume", 10, 180, 10),
        ("ember-keep", 0, "lava_pit", 38, 78, 10),
        ("ember-keep", 1, "lava_pit", -42, 96, 10)
    ];

    /// <summary>The accepted migration rule (F693-04), as <see cref="CompactGeometryTests"/> states it.</summary>
    static (double X, double Z, double Radius) Migrate(double x, double z, double radius)
    {
        var scale = FieldingResolver.OutfieldGrass(x, z, ShippedContent.Rules) ? 0.70 : 80.0 / 90.0;
        return (Math.Round(x * scale, MidpointRounding.AwayFromZero), Math.Round(z * scale, MidpointRounding.AwayFromZero),
            Math.Round(radius * 0.70, 2));
    }

    /// <summary>The shipped disc clears the shipped diamond and its migrated twin clears the trial's.</summary>
    static bool ClearsBoth(double x, double z, double radius)
    {
        var (tx, tz, tr) = Migrate(x, z, radius);
        return HazardPlacement.ClearanceFt(x, z, radius, ShippedContent.Rules.Infield) >= 0
            && HazardPlacement.ClearanceFt(tx, tz, tr, TrialContent.Rules.Infield) >= 0;
    }

    /// <summary>
    /// FD-19-R1: each moved volume keeps its type and radius, is farther from home, and stands on the ray from home through
    /// its old centre within the whole-foot rounding. The move is the smallest one along that ray at which both roots clear
    /// (found here to a thousandth of a foot), and the whole-foot place is the rounding of that point, per coordinate, that
    /// still clears and is nearest it. Its trial twin is the migration of the moved row. Nothing else at either park moved.
    /// </summary>
    [Fact]
    public void FD19R1_TheEightMovedOutwardAlongTheirOwnBearing()
    {
        foreach (var (parkId, index, type, x, z, radius) in Before)
        {
            var where = $"{parkId} hazard[{index}] from ({x}, {z})";
            var now = ShippedContent.Parks[parkId].Hazards[index];
            var twin = TrialContent.Parks[parkId].Hazards[index];
            Assert.Equal(type, now.Type);
            Assert.Equal(radius, now.Radius);

            // It had to move: the old disc, or its old twin, crossed.
            Assert.False(ClearsBoth(x, z, radius), where);

            // Outward, on its own bearing.
            var d = Math.Sqrt(x * x + z * z);
            var (ux, uz) = (x / d, z / d);
            Assert.True(Math.Sqrt(now.X * now.X + now.Z * now.Z) > d, where);
            Assert.True(Math.Abs(now.X * uz - now.Z * ux) < 1, $"{where}: off its bearing by more than the rounding");
            Assert.Equal(Math.Round(now.X), now.X);
            Assert.Equal(Math.Round(now.Z), now.Z);

            // The smallest move along the ray at which both roots clear.
            var step = 0.001;
            var k = 0;
            while (!ClearsBoth(x + k * step * ux, z + k * step * uz, radius)) k++;
            var t = k * step;
            Assert.True(t > 0 && t < 30, where);
            var (px, pz) = (x + t * ux, z + t * uz);

            // The whole-foot place: the nearest rounding of that point that still clears.
            var place = new[] { Math.Floor(px), Math.Ceiling(px) }
                .SelectMany(rx => new[] { Math.Floor(pz), Math.Ceiling(pz) }, (rx, rz) => (X: rx, Z: rz))
                .Distinct()
                .Where(p => ClearsBoth(p.X, p.Z, radius))
                .OrderBy(p => Diamond.Dist(p.X, p.Z, px, pz))
                .First();
            Assert.Equal(place, (now.X, now.Z));

            // Its twin is the migration of the moved row, and both discs clear their own diamonds.
            Assert.Equal(Migrate(now.X, now.Z, now.Radius), (twin.X, twin.Z, twin.Radius));
            Assert.True(HazardPlacement.ClearanceFt(now.X, now.Z, now.Radius, ShippedContent.Rules.Infield) >= 0, where);
            Assert.True(HazardPlacement.ClearanceFt(twin.X, twin.Z, twin.Radius, TrialContent.Rules.Infield) >= 0, where);
        }

        // The table in §14, as the data now reads.
        (string Park, int Index, double X, double Z, double TrialX, double TrialZ)[] after =
        [
            ("crystal-rink", 0, 53, 93, 47, 83),
            ("crystal-rink", 1, -49, 97, -44, 86),
            ("crystal-rink", 2, 10, 187, 7, 131),
            ("ember-keep", 0, 49, 100, 44, 89),
            ("ember-keep", 1, -45, 104, -40, 92)
        ];
        foreach (var (parkId, index, sx, sz, tx, tz) in after)
        {
            var now = ShippedContent.Parks[parkId].Hazards[index];
            var twin = TrialContent.Parks[parkId].Hazards[index];
            Assert.Equal((sx, sz, tx, tz), (now.X, now.Z, twin.X, twin.Z));
        }

        // Nothing else at either park moved: Crystal has no other hazard, and Ember's deep pit, breath and statue stand where
        // they stood, on both roots.
        Assert.Equal(3, ShippedContent.Parks["crystal-rink"].Hazards.Count);
        Assert.Equal(
            [("lava_pit", 12.0, 188.0, 12.0), ("fire_breath", 0.0, 250.0, 16.0), ("statue", 0.0, 310.0, 10.0)],
            ShippedContent.Parks["ember-keep"].Hazards.Skip(2).Select(h => (h.Type, h.X, h.Z, h.Radius)));
        Assert.Equal(
            [("lava_pit", 8.0, 132.0, 8.4), ("fire_breath", 0.0, 175.0, 11.2), ("statue", 0.0, 217.0, 7.0)],
            TrialContent.Parks["ember-keep"].Hazards.Skip(2).Select(h => (h.Type, h.X, h.Z, h.Radius)));
    }

    /// <summary>A copy of the data root, optionally with the trial overlay laid over it, that a test may break.</summary>
    sealed class PlacementFixture : IDisposable
    {
        readonly string _temp;
        readonly string _shipped;
        readonly string? _overlay;

        public PlacementFixture(bool trial)
        {
            _temp = Path.Combine(Path.GetTempPath(), "grand-sluggers-placement-" + Guid.NewGuid().ToString("N"));
            _shipped = Path.Combine(_temp, "data");
            Copy(Shipped, _shipped);
            if (trial)
            {
                _overlay = Path.Combine(_temp, "trials", "c80");
                Copy(Overlay, _overlay);
            }
        }

        public DataRoot Root() => _overlay is null ? new DataRoot(_shipped) : new DataRoot(_shipped, _overlay);

        /// <summary>The park file a refusal names: on a trial fixture, the overlay's copy.</summary>
        public string ParkFile(string id) => Path.GetFullPath(Path.Combine(_overlay ?? _shipped, "parks", id + ".json"));

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
