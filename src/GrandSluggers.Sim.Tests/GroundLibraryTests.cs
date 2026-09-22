using System.Reflection;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// F3-b (#846, FD-05 / FD-03 / FR-02): the ground and wall-material libraries, and the zone map that
/// says which row a point on the field is standing on (spec §6.1, §16).
///
/// <para>
/// <b>This child changes no play, and these rows are how that is true rather than hoped.</b> The two
/// libraries are new tables, but every row in them is today's <c>flight.json</c> number; the flight
/// and the three loose-ball models still read <c>flight.roll</c> / <c>bounce</c> / <c>skid</c> /
/// <c>wall</c>, and F3-c moves those reads. While both copies exist the parity rows hold them equal
/// field for field, so nothing can drift between now and that move — which is the whole reason the
/// duplication is allowed to exist at all (FR-06).
/// </para>
///
/// <para>
/// Every row runs against the shipped root and against <c>trials/c80</c>, both loaded by hand, so the
/// class is independent of <c>GRAND_SLUGGERS_TRIAL</c>. It is tagged <c>Rows=compact</c> all the same:
/// CI plays it a second time under the overlay, and a row that quietly started depending on the
/// process root would fail there.
/// </para>
/// </summary>
[Trait("Rows", "compact")]
public sealed class GroundLibraryTests
{
    /// <summary>The shipped root, named explicitly so the process's own overlay cannot stand in for it.</summary>
    static readonly ContentCatalog Shipped = ContentCatalog.Load(new DataRoot(ContentCatalog.Load().Root.Shipped));

    static string TrialDir =>
        Path.GetFullPath(Path.Combine(Shipped.Root.Shipped, "..", "trials", "c80"));

    static readonly ContentCatalog Trial = ContentCatalog.Load(new DataRoot(Shipped.Root.Shipped, TrialDir));

    static IEnumerable<ContentCatalog> BothRoots => [Shipped, Trial];

    // ---------------------------------------------------------------------------------
    // Parity — the libraries are today's flight blocks, row for row
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Every ground row equals the flight's own block field for field, on both roots. This is the
    /// guardrail that lets two copies of the same numbers exist until F3-c retires one: a tune written
    /// into <c>flight.json</c> and not into <c>grounds.json</c> (or the other way) fails here, by name,
    /// instead of shipping as a silent split between what the ball does and what the library says it
    /// does.
    /// </summary>
    [Fact]
    public void EveryGroundRowIsTodaysFlightBlockFieldForField()
    {
        foreach (var catalog in BothRoots)
        {
            var flight = catalog.Rules.Flight;
            var grounds = catalog.Rules.Grounds;
            Assert.Equal(["grass", "dirt", "ice", "ash"], grounds.Ids);
            foreach (var id in grounds.Ids)
            {
                var row = grounds.Of(id);
                SameNumbers($"grounds.{id}.roll", flight.Roll, row.Roll);
                SameNumbers($"grounds.{id}.bounce", flight.Bounce, row.Bounce);
                SameNumbers($"grounds.{id}.skid", flight.Skid, row.Skid);
            }
        }
    }

    /// <summary>
    /// And the rows against each other: every ground is the same ground today. An unequal row is a
    /// behavior change, and it arrives with Crystal (F9-a) as a trial Jack has accepted — never as a
    /// side effect of building the library.
    /// </summary>
    [Fact]
    public void EveryGroundRowIsEqualToEveryOtherToday()
    {
        foreach (var catalog in BothRoots)
        {
            var grounds = catalog.Rules.Grounds;
            var grass = grounds.Of(Ground.Grass);
            foreach (var id in grounds.Ids)
            {
                SameNumbers($"grounds.{id}.roll", grass.Roll, grounds.Of(id).Roll);
                SameNumbers($"grounds.{id}.bounce", grass.Bounce, grounds.Of(id).Bounce);
                SameNumbers($"grounds.{id}.skid", grass.Skid, grounds.Of(id).Skid);
            }
        }
    }

    /// <summary>The one wall material is today's <c>flight.wall</c>, on both roots.</summary>
    [Fact]
    public void TheWallRowIsTodaysFlightWall()
    {
        foreach (var catalog in BothRoots)
        {
            Assert.Equal(["padded"], catalog.Rules.Walls.Ids);
            SameNumbers("walls.padded", catalog.Rules.Flight.Wall, catalog.Rules.Walls.Of(WallMaterial.Padded));
        }
    }

    /// <summary>
    /// The flight still reads its own blocks. Stated as a row rather than left to the diff, because
    /// "F3-c moves the read" is the only reason the duplication above is honest: if a reader had
    /// already moved, the parity rows would be pinning a copy nobody uses.
    /// </summary>
    [Fact]
    public void TheFlightStillReadsItsOwnGroundBlocksUntilF3c()
    {
        var source = Path.Combine(Shipped.Root.Shipped, "..", "src", "GrandSluggers.Sim", "BallFlight.cs");
        var text = File.ReadAllText(Path.GetFullPath(source));
        // `f` is the flight table the loop was handed; these are the four blocks F3-c will move.
        Assert.Contains("f.Roll.Friction", text, StringComparison.Ordinal);
        Assert.Contains("f.Bounce.MinVy", text, StringComparison.Ordinal);
        Assert.Contains("f.Skid.MinVy", text, StringComparison.Ordinal);
        Assert.Contains("f.Wall", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Grounds", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GroundZones", text, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------
    // The tables — JSON is the source of truth, code is the fallback
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Both files exist, are named by <see cref="RulesTable.Files"/> (a table the code does not name is
    /// never loaded and never validated), and equal the code fallback field for field.
    /// </summary>
    [Fact]
    public void BothTablesAreLoadedAndEqualTheCodeFallback()
    {
        Assert.Contains("grounds", RulesTable.Files);
        Assert.Contains("walls", RulesTable.Files);
        foreach (var name in new[] { "grounds", "walls" })
            Assert.True(File.Exists(Path.Combine(Shipped.Root.Shipped, RulesTable.Directory, name + ".json")), name);

        var loaded = RulesTable.Load(Shipped.Root);
        var defaults = RulesTable.Defaults;
        foreach (var id in defaults.Grounds.Ids)
        {
            SameNumbers($"grounds.{id}.roll", defaults.Grounds.Of(id).Roll, loaded.Grounds.Of(id).Roll);
            SameNumbers($"grounds.{id}.bounce", defaults.Grounds.Of(id).Bounce, loaded.Grounds.Of(id).Bounce);
            SameNumbers($"grounds.{id}.skid", defaults.Grounds.Of(id).Skid, loaded.Grounds.Of(id).Skid);
        }
        foreach (var id in defaults.Walls.Ids)
            SameNumbers($"walls.{id}", defaults.Walls.Of(id), loaded.Walls.Of(id));
    }

    /// <summary>
    /// Every rule the two types declare is written in the file. A number that exists only as a C#
    /// initializer is a literal hiding from the table (§16).
    /// </summary>
    [Theory]
    [InlineData("grounds", new[] { "grass", "dirt", "ice", "ash" })]
    [InlineData("walls", new[] { "padded" })]
    public void TheFileNamesEveryRowAndEveryRuleInIt(string file, string[] rows)
    {
        var json = JsonNode.Parse(
            File.ReadAllText(Path.Combine(Shipped.Root.Shipped, RulesTable.Directory, file + ".json")),
            null,
            new System.Text.Json.JsonDocumentOptions
            {
                CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            })!.AsObject();

        Assert.Equal(rows, json.Select(p => p.Key));
        var section = typeof(RulesTable).GetProperty(char.ToUpperInvariant(file[0]) + file[1..])!.PropertyType;
        foreach (var row in rows)
        {
            var declared = section.GetProperty(row, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)!;
            MissingKeys(json[row]!.AsObject(), declared.PropertyType, $"{file}.{row}");
        }
    }

    /// <summary>
    /// A range the attributes own is refused by name and against the file it came from, the way every
    /// other rule is: the rows are named properties, so the reflective walk reaches inside one. A
    /// <c>Dictionary</c> of grounds would have loaded and been checked by nothing (FR-02).
    /// </summary>
    [Theory]
    [InlineData("grounds.json", "ice", "roll", "friction", 0, "grounds.ice.roll.friction must be greater than 0")]
    [InlineData("grounds.json", "grass", "bounce", "restitution", 1.4, "grounds.grass.bounce.restitution must be between 0 and 1")]
    [InlineData("grounds.json", "dirt", "skid", "horizontal", -0.2, "grounds.dirt.skid.horizontal must be between 0 and 1")]
    [InlineData("grounds.json", "ash", "roll", "restSpeed", -1, "grounds.ash.roll.restSpeed must be finite and at least 0")]
    public void AGroundRowIsRangeCheckedLikeEveryOtherRule(
        string file, string row, string block, string field, double value, string expected)
    {
        using var fixture = new RulesFileFixture();
        fixture.Change(file, json => json[row]![block]![field] = value);

        var errors = RulesTable.Validate(new DataRoot(fixture.Root));
        Assert.Contains(errors, e => e.Contains(expected, StringComparison.Ordinal)
                                     && e.Contains(fixture.Path(file), StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("walls.json", "padded", "restitution", 2.5, "walls.padded.restitution must be between 0 and 1")]
    [InlineData("walls.json", "padded", "tangential", -0.1, "walls.padded.tangential must be between 0 and 1")]
    public void AWallRowIsRangeCheckedLikeEveryOtherRule(
        string file, string row, string field, double value, string expected)
    {
        using var fixture = new RulesFileFixture();
        fixture.Change(file, json => json[row]![field] = value);

        var errors = RulesTable.Validate(new DataRoot(fixture.Root));
        Assert.Contains(errors, e => e.Contains(expected, StringComparison.Ordinal)
                                     && e.Contains(fixture.Path(file), StringComparison.Ordinal));
    }

    [Fact]
    public void AnUnknownKeyInsideARowIsAnError()
    {
        using var fixture = new RulesFileFixture();
        fixture.Change("grounds.json", json => json["ice"]!["roll"]!["slipperiness"] = 3);

        Assert.Contains(RulesTable.Validate(new DataRoot(fixture.Root)), e =>
            e.Contains("grounds.ice.roll.slipperiness is not a rule this table owns", StringComparison.Ordinal));
    }

    /// <summary>The cross-field rule the attributes cannot say, per row: the skid band has to be a band.</summary>
    [Fact]
    public void AGroundWhoseSkidBandNeverOpensIsRefused()
    {
        using var fixture = new RulesFileFixture();
        fixture.Change("grounds.json", json => json["dirt"]!["skid"]!["launchMinDeg"] = 30);

        Assert.Contains(RulesTable.Validate(new DataRoot(fixture.Root)), e =>
            e.Contains("grounds.dirt.skid.launchMinDeg must not exceed its upper bound", StringComparison.Ordinal));
    }

    /// <summary>
    /// The two sections survive both derivations. <see cref="RulesTable.AtLevel"/> and
    /// <see cref="RulesTable.AtPark"/> copy section by section by hand, and a section left out of a copy
    /// silently reverts to its code defaults with every test still green (implementation map finding
    /// 14). A park chooses which row its zones name; it never owns what a row says.
    /// </summary>
    [Fact]
    public void BothDerivedTablesShareTheLibrariesByReference()
    {
        var global = Shipped.Rules;
        var air = new ParkEnvironment(DragMul: 1.5);
        var park = Shipped.Parks["harbor-diamond"] with { Environment = air };

        foreach (var derived in new[] { global.AtLevel("hard"), global.AtPark(park), global.AtLevel("easy").AtPark(park) })
        {
            Assert.NotSame(global, derived);
            Assert.Same(global.Grounds, derived.Grounds);
            Assert.Same(global.Walls, derived.Walls);
        }
    }

    // ---------------------------------------------------------------------------------
    // SF-03 — an id with no row stops the load and names the id and the file
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-03</c>, the shipped root: a <c>surface</c> the library has no row for stops the load. The
    /// message names the bad id, the park file it was written in, and the library file the rows live
    /// in — the reader either misspelled it or has not authored it, and both files are where they
    /// would look.
    /// </summary>
    [Fact]
    public void SF03_ASurfaceWithNoGroundRowStopsTheLoadAndNamesTheIdAndTheFile()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("parks/harbor-diamond.json", json => json["surface"] = "mud");

        var error = Assert.Single(ContentDataValidator.Validate(new DataRoot(fixture.Root)), e =>
            e.Contains("park 'harbor-diamond' surface", StringComparison.Ordinal));
        Assert.StartsWith(fixture.Path("parks/harbor-diamond.json"), error, StringComparison.Ordinal);
        Assert.Contains("must be a ground with a row in", error, StringComparison.Ordinal);
        Assert.Contains(Path.Combine(fixture.Root, "rules", "grounds.json"), error, StringComparison.Ordinal);
        Assert.Contains("[grass, dirt, ice, ash]", error, StringComparison.Ordinal);
        Assert.Contains("got 'mud'", error, StringComparison.Ordinal);

        var thrown = Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(new DataRoot(fixture.Root)));
        Assert.Contains(error, thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>SF-03</c>, the trial root. The same refusal, reported against the <em>trial's</em> park file,
    /// because that is the copy the id was written in — and against the shipped library, because
    /// <c>trials/c80</c> carries no <c>grounds.json</c> and the rows it is checked against are the
    /// shipped ones (§16, per-file resolution).
    /// </summary>
    [Fact]
    public void SF03_ASurfaceWithNoGroundRowIsRefusedOnTheTrialRootToo()
    {
        using var shipped = new ContentFixture();
        using var trial = new TrialCopy(shipped.Root);
        trial.ChangeObject("parks/crystal-rink.json", json => json["surface"] = "slush");

        var error = Assert.Single(ContentDataValidator.Validate(trial.Root), e =>
            e.Contains("park 'crystal-rink' surface", StringComparison.Ordinal));
        Assert.StartsWith(trial.Path("parks/crystal-rink.json"), error, StringComparison.Ordinal);
        Assert.Contains(Path.Combine(shipped.Root, "rules", "grounds.json"), error, StringComparison.Ordinal);
        Assert.Contains("got 'slush'", error, StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(trial.Root));
    }

    /// <summary><c>SF-03</c>: a zone that names a ground with no row is the same stop, on both roots, per zone.</summary>
    [Theory]
    [InlineData("infieldDirt")]
    [InlineData("outfield")]
    [InlineData("warningTrack")]
    [InlineData("foulApron")]
    public void SF03_AZoneIdWithNoGroundRowStopsTheLoadOnBothRoots(string zone)
    {
        using var shipped = new ContentFixture();
        shipped.ChangeObject("parks/harbor-diamond.json",
            json => json["zones"] = new JsonObject { [zone] = "astroturf" });
        var onShipped = Assert.Single(ContentDataValidator.Validate(new DataRoot(shipped.Root)), e =>
            e.Contains($"park 'harbor-diamond' zones.{zone}", StringComparison.Ordinal));
        Assert.StartsWith(shipped.Path("parks/harbor-diamond.json"), onShipped, StringComparison.Ordinal);
        Assert.Contains("got 'astroturf'", onShipped, StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(new DataRoot(shipped.Root)));

        using var clean = new ContentFixture();
        using var trial = new TrialCopy(clean.Root);
        trial.ChangeObject("parks/ember-keep.json",
            json => json["zones"] = new JsonObject { [zone] = "astroturf" });
        var onTrial = Assert.Single(ContentDataValidator.Validate(trial.Root), e =>
            e.Contains($"park 'ember-keep' zones.{zone}", StringComparison.Ordinal));
        Assert.StartsWith(trial.Path("parks/ember-keep.json"), onTrial, StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(trial.Root));
    }

    /// <summary>
    /// <c>SF-03</c>, the wall side. No span names a material yet — that is the polyline fence's (F2-c) —
    /// so the refusal is proved where it lives: asking the library for a material it has no row for is
    /// a stop that names the id and the file, not a carom off whatever row happens to be first.
    /// </summary>
    [Fact]
    public void SF03_AWallMaterialWithNoRowIsAStopThatNamesTheIdAndTheFile()
    {
        foreach (var catalog in BothRoots)
        {
            var thrown = Assert.Throws<ArgumentException>(() => catalog.Rules.Walls.Of("brick"));
            Assert.Contains("'brick' is not a wall material with a row in rules/walls.json", thrown.Message, StringComparison.Ordinal);
            Assert.Contains("[padded]", thrown.Message, StringComparison.Ordinal);
            Assert.False(catalog.Rules.Walls.Has("brick"));
            Assert.False(catalog.Rules.Walls.Has(null));
        }
    }

    /// <summary>And the same for a ground asked for in code rather than written in a park file.</summary>
    [Fact]
    public void SF03_AGroundWithNoRowIsAStopThatNamesTheIdAndTheFile()
    {
        foreach (var catalog in BothRoots)
        {
            var thrown = Assert.Throws<ArgumentException>(() => catalog.Rules.Grounds.Of("mud"));
            Assert.Contains("'mud' is not a ground with a row in rules/grounds.json", thrown.Message, StringComparison.Ordinal);
            Assert.Contains("[grass, dirt, ice, ash]", thrown.Message, StringComparison.Ordinal);
            Assert.False(catalog.Rules.Grounds.Has("mud"));
            Assert.False(catalog.Rules.Grounds.Has(null));
            // Case is part of the id: the library spells them lowercase and so does a park file.
            Assert.False(catalog.Rules.Grounds.Has("Grass"));
        }
    }

    /// <summary>The block is inside the strict park schema (#820): a zone the file does not declare is named, not dropped.</summary>
    [Fact]
    public void SF03_AnUnknownKeyInsideTheZonesBlockIsRefusedByName()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("parks/harbor-diamond.json",
            json => json["zones"] = new JsonObject { ["infieldGrass"] = "grass" });

        Assert.Contains(ContentDataValidator.Validate(new DataRoot(fixture.Root)), e =>
            e.Contains("zones.infieldGrass is not a key this file declares", StringComparison.Ordinal)
            && e.Contains("[foulApron, infieldDirt, outfield, warningTrack]", StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(new DataRoot(fixture.Root)));
    }

    // ---------------------------------------------------------------------------------
    // The zone map — derived from surface, overridden by the park, read from the geometry
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// No park names a <c>zones</c> block, on either root, and every park's resolved map is the one
    /// derived from its <c>surface</c>: the outfield and the apron are the surface, the infield and the
    /// track are dirt. That derivation is chosen so <c>surface</c> keeps exactly the meaning it has
    /// today — so this child gives the field a ground map without moving any park's ground.
    /// </summary>
    [Fact]
    public void TheDefaultZoneMapOfEveryParkIsItsSurfacePlusDirt()
    {
        foreach (var catalog in BothRoots)
        {
            Assert.Equal(6, catalog.Parks.Count);
            foreach (var park in catalog.Parks.Values)
            {
                Assert.Null(park.Zones);
                var zones = GroundZones.Of(park, catalog.Rules);
                Assert.Equal(park.Surface, zones.Outfield);
                Assert.Equal(park.Surface, zones.FoulApron);
                Assert.Equal(Ground.Dirt, zones.InfieldDirt);
                Assert.Equal(Ground.Dirt, zones.WarningTrack);
                foreach (var (_, ground) in zones.All())
                    Assert.True(catalog.Rules.Grounds.Has(ground), $"{park.Id}: {ground}");
            }
        }
    }

    /// <summary>
    /// The four surfaces the six parks actually name are all four library ids, so every row in the file
    /// is a row some park is standing on — not four rows of which two are decoration.
    /// </summary>
    [Fact]
    public void EveryGroundInTheLibraryIsSomeParksSurface()
    {
        foreach (var catalog in BothRoots)
            Assert.Equal(
                catalog.Rules.Grounds.Ids.OrderBy(x => x, StringComparer.Ordinal),
                catalog.Parks.Values.Select(p => p.Surface).Distinct().OrderBy(x => x, StringComparer.Ordinal));
    }

    /// <summary>
    /// The lip, the track and the apron, named point by point at Harbor and at the compact diamond. The
    /// two roots have different fences (330 / 400 / 330 against 232 / 280 / 232) and different lips (155
    /// against 137.78), so a row that hard-coded either would fail on the other.
    /// </summary>
    [Fact]
    public void ZoneAtNamesTheDirtTheGrassTheTrackAndTheApron()
    {
        foreach (var catalog in BothRoots)
        {
            var park = catalog.Parks["harbor-diamond"];
            var zones = GroundZones.Of(park, catalog.Rules);
            var lip = catalog.Rules.Flight.Classes.InfieldLipFt;
            var fence = AtBatResolver.FenceAt(park, 0);

            // Straight out to centre: dirt, grass, track.
            Assert.Equal(GroundZone.InfieldDirt, zones.ZoneAt(0, lip * 0.5));
            Assert.Equal(GroundZone.Outfield, zones.ZoneAt(0, (lip + fence - ParkDiamond.TrackWidth) * 0.5));
            Assert.Equal(GroundZone.WarningTrack, zones.ZoneAt(0, fence - 1));

            // The lip is the same edge, and the same side of it, FieldingResolver splits dirt from grass at.
            Assert.Equal(GroundZone.InfieldDirt, zones.ZoneAt(0, lip - 0.001));
            Assert.Equal(GroundZone.Outfield, zones.ZoneAt(0, lip));
            Assert.False(FieldingResolver.OutfieldGrass(0, lip - 0.001, catalog.Rules));
            Assert.True(FieldingResolver.OutfieldGrass(0, lip, catalog.Rules));

            // The track's inner edge is where ParkDiamond draws it.
            var inner = ParkDiamond.TrackInner(park, 0);
            var innerR = Diamond.Dist(0, 0, inner.X, inner.Z);
            Assert.Equal(GroundZone.Outfield, zones.ZoneAt(0, innerR - 0.001));
            Assert.Equal(GroundZone.WarningTrack, zones.ZoneAt(0, innerR));

            // Outside the chalk, and behind the plate, is the apron at any distance.
            Assert.Equal(GroundZone.FoulApron, zones.ZoneAt(60, 40));
            Assert.Equal(GroundZone.FoulApron, zones.ZoneAt(-300, 100));
            Assert.Equal(GroundZone.FoulApron, zones.ZoneAt(0, -10));
            // The chalk itself is fair (FieldBounds.IsFair), so the line at short range is infield dirt.
            Assert.Equal(GroundZone.InfieldDirt, zones.ZoneAt(50, 50));
        }
    }

    /// <summary>
    /// The boundaries are read, not chosen (#730 / #732). Swept over both roots, all six parks and a
    /// grid of bearings and radii, <see cref="GroundZones.ZoneAt"/> agrees with the geometry that
    /// already existed — the chalk (<see cref="FieldBounds.IsFair"/>), the lip
    /// (<see cref="FieldingResolver.OutfieldGrass"/>) and the track's inner edge
    /// (<see cref="ParkDiamond.TrackInner"/>) — computed here independently. F3-b moved none of them,
    /// and this row is what would fail if a later child moved one by accident.
    /// </summary>
    [Fact]
    public void ZoneAtIsTheGeometryTheFieldAlreadyHad()
    {
        foreach (var catalog in BothRoots)
        {
            foreach (var park in catalog.Parks.Values)
            {
                var zones = GroundZones.Of(park, catalog.Rules);
                for (var sprayDeg = -80.0; sprayDeg <= 80.0; sprayDeg += 2.5)
                {
                    var rad = sprayDeg * Math.PI / 180.0;
                    for (var r = 5.0; r <= 420.0; r += 5)
                    {
                        var x = r * Math.Sin(rad);
                        var z = r * Math.Cos(rad);
                        var inner = ParkDiamond.TrackInner(park, sprayDeg);
                        var innerR = Diamond.Dist(0, 0, inner.X, inner.Z);
                        // The two sides reach each edge by different arithmetic, so the last ulp is
                        // noise rather than a rule. A grid point that lands on an edge (the compact
                        // Harbor track's inner radius is exactly 265) is skipped here and asserted
                        // exactly, from both sides, in ZoneAtNamesTheDirtTheGrassTheTrackAndTheApron.
                        var lip = catalog.Rules.Flight.Classes.InfieldLipFt;
                        if (Math.Abs(r - innerR) < 1e-6 || Math.Abs(r - lip) < 1e-6) continue;
                        var expected =
                            !FieldBounds.IsFair(x, z) ? GroundZone.FoulApron
                            : !FieldingResolver.OutfieldGrass(x, z, catalog.Rules) ? GroundZone.InfieldDirt
                            : r >= innerR ? GroundZone.WarningTrack
                            : GroundZone.Outfield;
                        Assert.Equal(expected, zones.ZoneAt(x, z));
                    }
                }
            }
        }
    }

    /// <summary>
    /// A park may override one zone and leave the rest derived, and <c>GroundAt</c> answers the id the
    /// ball will be handed. Proved on a park built in this file, because no shipped or trial park names
    /// a block — the lever exists and nothing pulls it (FR-06).
    /// </summary>
    [Fact]
    public void AParkMayOverrideOneZoneAndLeaveTheRestDerived()
    {
        var harbor = Shipped.Parks["harbor-diamond"];
        Assert.Equal(Ground.Grass, harbor.Surface);

        var iced = harbor with { Zones = new ParkZones(InfieldDirt: Ground.Ice) };
        var zones = GroundZones.Of(iced, Shipped.Rules);
        Assert.Equal(Ground.Ice, zones.InfieldDirt);
        Assert.Equal(Ground.Grass, zones.Outfield);
        Assert.Equal(Ground.Dirt, zones.WarningTrack);
        Assert.Equal(Ground.Grass, zones.FoulApron);

        Assert.Equal(Ground.Ice, zones.GroundAt(0, 60));
        Assert.Equal(Ground.Grass, zones.GroundAt(0, 250));
        Assert.Equal(Ground.Grass, zones.GroundAt(60, 40));

        // An empty block is not an override: every zone is the derived one.
        var empty = GroundZones.Of(harbor with { Zones = new ParkZones() }, Shipped.Rules);
        Assert.False(new ParkZones().Names);
        Assert.True(new ParkZones(Outfield: Ground.Ash).Names);
        foreach (var (zone, ground) in empty.All())
            Assert.Equal(GroundZones.Of(harbor, Shipped.Rules).IdOf(zone), ground);
    }

    /// <summary>And the way through: a park file may name the block, it loads, and the resolved map reads it.</summary>
    [Fact]
    public void AParkFileMayNameItsZonesAndTheMapReadsThem()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("parks/harbor-diamond.json", json => json["zones"] = new JsonObject
        {
            ["infieldDirt"] = "ash",
            ["warningTrack"] = "ice"
        });

        Assert.Empty(ContentDataValidator.Validate(new DataRoot(fixture.Root)));
        var content = ContentCatalog.Load(new DataRoot(fixture.Root));
        var park = content.Parks["harbor-diamond"];
        Assert.Equal("ash", park.Zones!.InfieldDirt);
        Assert.Null(park.Zones.Outfield);

        var zones = GroundZones.Of(park, content.Rules);
        Assert.Equal(Ground.Ash, zones.InfieldDirt);
        Assert.Equal(Ground.Ice, zones.WarningTrack);
        Assert.Equal(Ground.Grass, zones.Outfield);
        Assert.Equal(Ground.Grass, zones.FoulApron);
    }

    /// <summary>
    /// The no-table overload takes the process-wide table and nothing else — the same fallback
    /// <see cref="ParkBoundary.Default"/> and <see cref="FieldBounds.Of(Park)"/> take, for the callers
    /// that hold no catalog. It is pinned here rather than left to a comment, because a reader who
    /// used it from inside a match would be reading the process's lip instead of the match's, and that
    /// is exactly the class of bug the F3-a audit was opened for (map finding 3). A match hands its
    /// own table over; `Of(park)` is for the ones that cannot.
    /// </summary>
    [Fact]
    public void TheNoTableOverloadTakesTheProcessWideTableAndNothingElse()
    {
        var park = Shipped.Parks["harbor-diamond"] with { Zones = new ParkZones(WarningTrack: Ground.Ash) };
        Assert.Equal(GroundZones.Of(park, Rules.Default), GroundZones.Of(park));
        Assert.Equal(Rules.Default.Flight.Classes.InfieldLipFt, GroundZones.Of(park).InfieldLipFt);
        // The zones themselves come from the park either way; only the lip is the table's.
        Assert.Equal(Ground.Ash, GroundZones.Of(park).WarningTrack);
    }

    /// <summary>
    /// The map is resolved beside the table, not hung on it. <see cref="RulesTable.AtPark"/> hands back
    /// the global table <em>itself</em> for a park that names no air (<c>SF-01</c>), and a per-park zone
    /// map on that table would have made every park's table a copy and broken the one thing F3-a
    /// proved. So the library stays global and the map is a value resolved from the park, the way
    /// <see cref="ParkBoundary"/> resolves the edge.
    /// </summary>
    [Fact]
    public void TheZoneMapDoesNotMakeAParksResolvedTableACopy()
    {
        foreach (var catalog in BothRoots)
        {
            foreach (var park in catalog.Parks.Values)
                Assert.Same(catalog.Rules, catalog.Rules.AtPark(park));

            // A park that names zones and no air is still the global table: zones are not on it.
            var zoned = catalog.Parks["harbor-diamond"] with { Zones = new ParkZones(Outfield: Ground.Ice) };
            Assert.Same(catalog.Rules, catalog.Rules.AtPark(zoned));
            Assert.Equal(Ground.Ice, GroundZones.Of(zoned, catalog.Rules).Outfield);
        }
    }

    /// <summary>
    /// A park that names no zones writes nothing into <see cref="PlayTraceIdentity"/>, so every stored
    /// identity SHA taken at a shipped park is the string it was. (The two new rule <em>sections</em>
    /// are in the identity like every other section, so the sealed evidence moves by hash; that is the
    /// member being honest, not hidden.) A park that does name one is a different identity.
    /// </summary>
    [Fact]
    public void TheParksZonesAreInTheTraceIdentityAndTodaysParksWriteNone()
    {
        var (home, away) = PresetTeams.Pair(Shipped, "rio", "ashlord");
        foreach (var park in Shipped.Parks.Values)
        {
            var match = new Match(Shipped, away, home, park, innings: 3, seed: 7);
            // The quoted key, not the bare word: `batting.spray.outOfZoneSpanDeg` contains "zones".
            Assert.DoesNotContain("\"zones\"", PlayTraceIdentity.Capture(match).InputsJson, StringComparison.Ordinal);
        }

        var plain = Shipped.Parks["harbor-diamond"];
        var iced = plain with { Zones = new ParkZones(Outfield: Ground.Ice) };
        var identity = PlayTraceIdentity.Capture(new Match(Shipped, away, home, iced, innings: 3, seed: 7));
        Assert.Contains("\"zones\":{\"outfield\":\"ice\"", identity.InputsJson, StringComparison.Ordinal);
        Assert.NotEqual(
            PlayTraceIdentity.Capture(new Match(Shipped, away, home, plain, innings: 3, seed: 7)).Sha256,
            identity.Sha256);
    }

    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Two blocks of the same type, compared field for field rather than by <c>Equals</c>: these are
    /// classes, not records, so the equality a reader would reach for is reference equality and would
    /// pass nothing worth passing.
    /// </summary>
    static void SameNumbers(string what, object expected, object actual)
    {
        Assert.Equal(expected.GetType(), actual.GetType());
        var properties = expected.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .ToArray();
        Assert.NotEmpty(properties);
        foreach (var p in properties)
            Assert.True(Equals(p.GetValue(expected), p.GetValue(actual)),
                $"{what}.{p.Name}: {p.GetValue(expected)} vs {p.GetValue(actual)}");
    }

    /// <summary>Every field the type declares is a key in the file (§16: no rule hides in a C# initializer).</summary>
    static void MissingKeys(JsonObject json, Type type, string path)
    {
        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.GetIndexParameters().Length > 0 || !p.CanWrite) continue;
            var key = char.ToLowerInvariant(p.Name[0]) + p.Name[1..];
            Assert.True(json.ContainsKey(key), $"{path}.{key} is declared in code but not written in the file");
            if (p.PropertyType.IsClass && p.PropertyType != typeof(string)
                && p.PropertyType.Namespace == typeof(RulesTable).Namespace)
                MissingKeys(json[key]!.AsObject(), p.PropertyType, $"{path}.{key}");
        }
    }

    /// <summary>A throwaway copy of the shipped root whose rule tables a row may break on purpose.</summary>
    sealed class RulesFileFixture : IDisposable
    {
        public RulesFileFixture()
        {
            Root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "grand-sluggers-grounds-" + Guid.NewGuid().ToString("N"));
            CopyTree(ContentCatalog.Load().Root.Shipped, Root);
        }

        public string Root { get; }

        public string Path(string file) => System.IO.Path.Combine(Root, RulesTable.Directory, file);

        public void Change(string file, Action<JsonObject> change)
        {
            var path = Path(file);
            var json = JsonNode.Parse(File.ReadAllText(path), null, new System.Text.Json.JsonDocumentOptions
            {
                CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            })!.AsObject();
            change(json);
            File.WriteAllText(path, json.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }

    /// <summary>
    /// A throwaway copy of <c>trials/c80</c> laid over a throwaway shipped root, so an <c>SF-03</c> row
    /// can break the trial's copy of a park and read the refusal back against the trial's own path.
    /// </summary>
    sealed class TrialCopy : IDisposable
    {
        readonly string _shipped;

        public TrialCopy(string shipped)
        {
            _shipped = shipped;
            Dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "grand-sluggers-trial-" + Guid.NewGuid().ToString("N"));
            CopyTree(TrialDir, Dir);
        }

        public string Dir { get; }

        public DataRoot Root => new(_shipped, Dir);

        public string Path(string relative) =>
            System.IO.Path.Combine(Dir, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));

        public void ChangeObject(string relative, Action<JsonObject> change)
        {
            var path = Path(relative);
            var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            change(json);
            File.WriteAllText(path, json.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }

        public void Dispose() => Directory.Delete(Dir, recursive: true);
    }

    static void CopyTree(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)));
    }
}
