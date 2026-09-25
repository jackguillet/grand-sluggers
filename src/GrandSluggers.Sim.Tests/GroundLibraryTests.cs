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
/// <b>No play changed, and these rows are how that is true rather than hoped.</b> F3-b built the two
/// libraries with every row at today's <c>flight.json</c> number and held the two copies equal while
/// both existed. F3-c (#856) moved the reads — the batted ball, the overthrow and the local bobble read
/// the row of the zone under the ball (<c>GroundReadTests</c>) — and retired the flight's copy, so the
/// parity rows now pin every row to the shipped numbers written here (FD-05, FR-06). A row that is not
/// these numbers is a behavior change, and it arrives as a trial Jack has accepted.
/// </para>
/// </summary>
public sealed class GroundLibraryTests
{
    /// <summary>The data root, named explicitly so the process's own overlay cannot stand in for it.</summary>
    static readonly ContentCatalog Game = ContentCatalog.Load(new DataRoot(Shipped.Content.Root.Shipped));

    // ---------------------------------------------------------------------------------
    // Parity — every row is today's number, written here
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Every ground row is the shipped number, field for field (FD-05). Until F3-c this row
    /// held each ground equal to <c>flight.json</c>'s own <c>roll</c> / <c>bounce</c> / <c>skid</c>; F3-c
    /// retired that copy and moved the overthrow's deceleration and the local bobble's three ground numbers
    /// in, so the expectation is now the numbers themselves — the values those keys carried the day they
    /// moved. A tune to any of them fails here by name, and a tune is a trial Jack accepts (F9-a), never a
    /// side effect.
    /// </summary>
    [Fact]
    public void EveryGroundRowIsTodaysNumberFieldForField()
    {
        var catalog = Game;
        var grounds = catalog.Rules.Grounds;
        Assert.Equal(["grass", "dirt", "ice", "ash", "sand"], grounds.Ids);
        foreach (var id in grounds.Ids.Where(id => id is not Ground.Ice and not Ground.Sand))
            AssertTodaysGround($"grounds.{id}", grounds.Of(id));
    }

    /// <summary>
    /// Coconut Cove's beach: a roll dies sooner and a bounce is lower and softer than on grass, and a body runs as on grass.
    /// Its bobble is grass's, so the beach changes the ball, never the error rules. Written here so a tune fails by name.
    /// Proposed, not tuned.
    /// </summary>
    [Fact]
    public void TheSandRowIsTheBeachsNumbersFieldForField()
    {
        var sand = Game.Rules.Grounds.Of(Ground.Sand);
        var grass = Game.Rules.Grounds.Of(Ground.Grass);
        Assert.Equal((32.0, 2.0), (sand.Roll.Friction, sand.Roll.RestSpeed));
        Assert.Equal((0.34, 0.72, 3.6), (sand.Bounce.Restitution, sand.Bounce.Horizontal, sand.Bounce.MinVy));
        Assert.Equal((14.0, 22.0, 2.2, 0.2, 0.85), (sand.Skid.ImpactMinDeg, sand.Skid.ImpactMaxDeg, sand.Skid.MinVy, sand.Skid.Restitution, sand.Skid.Horizontal));
        Assert.Equal(26.0, sand.Overthrow.DecelFtPerSec2);
        Assert.Equal(grass.Bobble, sand.Bobble);
        Assert.Equal(grass.Body, sand.Body);
        Assert.True(sand.Roll.Friction > grass.Roll.Friction && sand.Bounce.Restitution < grass.Bounce.Restitution,
            "sand rolls shorter and bounces lower than grass");
    }

    /// <summary>
    /// F9-a: the ice row is Crystal's, the first row that is not today's number. It is written here so a tune fails by
    /// name. The ball runs and skids farther (less friction, a lower rest speed, a wider skid band, a flatter bounce, a
    /// longer overthrow and bobble) and the body is slower to start, stop and turn. Proposed, not tuned; Jack judges it
    /// in play (FD-13-R2).
    /// </summary>
    [Fact]
    public void F9A_TheIceRowIsCrystalsNumbersFieldForField()
    {
        var ice = Game.Rules.Grounds.Of(Ground.Ice);
        Assert.Equal((12.0, 0.8), (ice.Roll.Friction, ice.Roll.RestSpeed));
        Assert.Equal((0.40, 0.92, 3.6), (ice.Bounce.Restitution, ice.Bounce.Horizontal, ice.Bounce.MinVy));
        Assert.Equal((14.0, 28.0, 2.2, 0.22, 0.97), (ice.Skid.ImpactMinDeg, ice.Skid.ImpactMaxDeg, ice.Skid.MinVy, ice.Skid.Restitution, ice.Skid.Horizontal));
        Assert.Equal(10.0, ice.Overthrow.DecelFtPerSec2);
        Assert.Equal((0.35, 0.90, 3.5), (ice.Bobble.Restitution, ice.Bobble.GroundRetain, ice.Bobble.DecelFtPerSec2));
        Assert.Equal((1.3, 1.6, 1.4, 1.35, 1.5), (ice.Body.StartMul, ice.Body.BrakeMul, ice.Body.CutMul, ice.Body.SlideMul, ice.Body.OverrunMul));
        var grass = Game.Rules.Grounds.Of(Ground.Grass);
        Assert.True(ice.Roll.Friction < grass.Roll.Friction && ice.Skid.ImpactMaxDeg > grass.Skid.ImpactMaxDeg);
        Assert.True(ice.Body.StartMul > 1 && ice.Body.BrakeMul > 1 && ice.Body.CutMul > 1, "ice is slower to answer, never faster");
    }

    /// <summary>
    /// And the rows against each other: every ground is the same ground today. An unequal row is a
    /// behavior change, and it arrives with Crystal (F9-a) as a trial Jack has accepted — never as a
    /// side effect of building the library. The <c>body</c> block (F3-d, #857) is not the ball's, so
    /// <see cref="AssertTodaysGround"/> leaves it to <c>BodyGroundTests</c>, which pins it at 1.0; its
    /// rows equal each other here like every other block.
    /// </summary>
    [Fact]
    public void EveryGroundRowIsEqualToEveryOtherToday()
    {
        var catalog = Game;
        var grounds = catalog.Rules.Grounds;
        var grass = grounds.Of(Ground.Grass);
        // Ice (F9A_TheIceRowIsCrystalsNumbersFieldForField) and sand (TheSandRowIsTheBeachsNumbersFieldForField) are their
        // parks' own rows; every other row is still one ground.
        foreach (var id in grounds.Ids.Where(id => id is not Ground.Ice and not Ground.Sand))
        {
            SameNumbers($"grounds.{id}.roll", grass.Roll, grounds.Of(id).Roll);
            SameNumbers($"grounds.{id}.bounce", grass.Bounce, grounds.Of(id).Bounce);
            SameNumbers($"grounds.{id}.skid", grass.Skid, grounds.Of(id).Skid);
            SameNumbers($"grounds.{id}.overthrow", grass.Overthrow, grounds.Of(id).Overthrow);
            SameNumbers($"grounds.{id}.bobble", grass.Bobble, grounds.Of(id).Bobble);
            SameNumbers($"grounds.{id}.body", grass.Body, grounds.Of(id).Body);
        }
    }

    /// <summary>
    /// The one wall material is the shipped carom, written here (FD-05 / FD-06). Until F3-c
    /// this row held it equal to <c>flight.wall</c>; F3-c retired that copy, so the expectation is the
    /// numbers <c>flight.wall</c> carried the day it moved.
    /// </summary>
    [Fact]
    public void TheWallRowIsTodaysNumber()
    {
        var catalog = Game;
        Assert.Equal(["padded", "glass"], catalog.Rules.Walls.Ids);
        var padded = catalog.Rules.Walls.Of(WallMaterial.Padded);
        Assert.Equal((0.48, 0.82), (padded.Restitution, padded.Tangential));
        // F9-a: Crystal's glass boards, a livelier carom than the pad. Proposed, not tuned.
        var glass = catalog.Rules.Walls.Of(WallMaterial.Glass);
        Assert.Equal((0.62, 0.90), (glass.Restitution, glass.Tangential));
    }

    /// <summary>
    /// The copies are gone and the readers moved (F3-c, FD-05). Stated as a row rather than left to the
    /// diff, because one copy is the only reason the rows above pin anything a ball does: <c>FlightRules</c>
    /// declares no roll, bounce, skid or wall, <c>fielding.overthrow</c> no deceleration and
    /// <c>fielding.handling</c> none of the bobble's three ground numbers — so neither file can carry one
    /// (an unknown key is refused) — and the flight reads its ground through the zone map and its carom
    /// through the segment's material.
    /// </summary>
    [Fact]
    public void TheFlightAndTheLooseBallReadTheRowsAndNoCopyRemains()
    {
        static IEnumerable<string> Declared(Type type) =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name);

        Assert.Empty(Declared(typeof(FlightRules)).Intersect(["Roll", "Bounce", "Skid", "Wall"]));
        Assert.DoesNotContain("DecelFtPerSec2", Declared(typeof(OverthrowRules)));
        Assert.Empty(Declared(typeof(HandlingRules)).Intersect(["BobbleRestitution", "BobbleGroundRetain", "BobbleDecelFtPerSec2"]));

        var source = Path.Combine(Game.Root.Shipped, "..", "src", "GrandSluggers.Sim", "BallFlight.cs");
        var text = File.ReadAllText(Path.GetFullPath(source));
        Assert.Contains(".RowAt(", text, StringComparison.Ordinal);
        Assert.Contains("WallMaterial.OfSegment(", text, StringComparison.Ordinal);
        Assert.Contains(".Overthrow.DecelFtPerSec2", text, StringComparison.Ordinal);
        Assert.Contains(".Bobble;", text, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------
    // The tables — JSON is the source of truth, code is the fallback
    // ---------------------------------------------------------------------------------

    [Fact]
    public void BothTablesAreLoaded()
    {
        Assert.Contains("grounds", RulesTable.Files);
        Assert.Contains("walls", RulesTable.Files);
        foreach (var name in new[] { "grounds", "walls" })
            Assert.True(File.Exists(Path.Combine(Game.Root.Shipped, RulesTable.Directory, name + ".json")), name);
        Assert.Empty(RulesTable.Validate(Game.Root));
    }

    /// <summary>
    /// Every rule the two types declare is written in the file. A number that exists only as a C#
    /// initializer is a literal hiding from the table (§16).
    /// </summary>
    [Theory]
    [InlineData("grounds", new[] { "grass", "dirt", "ice", "ash", "sand" })]
    [InlineData("walls", new[] { "padded", "glass" })]
    public void TheFileNamesEveryRowAndEveryRuleInIt(string file, string[] rows)
    {
        var json = JsonNode.Parse(
            File.ReadAllText(Path.Combine(Game.Root.Shipped, RulesTable.Directory, file + ".json")),
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
        fixture.Change("grounds.json", json => json["dirt"]!["skid"]!["impactMinDeg"] = 30);

        Assert.Contains(RulesTable.Validate(new DataRoot(fixture.Root)), e =>
            e.Contains("grounds.dirt.skid.impactMinDeg must not exceed its upper bound", StringComparison.Ordinal));
    }

    /// <summary>
    /// The two sections survive both derivations. <see cref="RulesTable.AtLevel"/> and
    /// <see cref="RulesTable.AtPark"/> are <c>with</c> copies, and a section a copy lost would be an
    /// empty table (implementation map finding 14). A park chooses which row its zones name; it never
    /// owns what a row says.
    /// </summary>
    [Fact]
    public void BothDerivedTablesShareTheLibrariesByReference()
    {
        var global = Game.Rules;
        var air = new ParkEnvironment(DragMul: 1.5);
        var park = Game.Parks[ParkId.Harbor] with { Environment = air };

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
        Assert.Contains("[grass, dirt, ice, ash, sand]", error, StringComparison.Ordinal);
        Assert.Contains("got 'mud'", error, StringComparison.Ordinal);

        var thrown = Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(new DataRoot(fixture.Root)));
        Assert.Contains(error, thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>SF-03</c>, an overlay root. The same refusal, reported against the <em>overlay's</em> park file,
    /// because that is the copy the id was written in — and against the data root's library, because the
    /// overlay carries no <c>grounds.json</c> and the rows it is checked against are the data root's
    /// (§16, per-file resolution).
    /// </summary>
    [Fact]
    public void SF03_ASurfaceWithNoGroundRowIsRefusedOnAnOverlayToo()
    {
        using var shipped = new ContentFixture();
        using var overlay = new OverlayCopy(shipped.Root, "parks/crystal-rink.json");
        overlay.ChangeObject("parks/crystal-rink.json", json => json["surface"] = "slush");

        var error = Assert.Single(ContentDataValidator.Validate(overlay.Root), e =>
            e.Contains("park 'crystal-rink' surface", StringComparison.Ordinal));
        Assert.StartsWith(overlay.Path("parks/crystal-rink.json"), error, StringComparison.Ordinal);
        Assert.Contains(Path.Combine(shipped.Root, "rules", "grounds.json"), error, StringComparison.Ordinal);
        Assert.Contains("got 'slush'", error, StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(overlay.Root));
    }

    /// <summary><c>SF-03</c>: a zone that names a ground with no row is the same stop, per zone, on the data root and on an overlay.</summary>
    [Theory]
    [InlineData("infieldDirt")]
    [InlineData("outfield")]
    [InlineData("warningTrack")]
    [InlineData("foulApron")]
    public void SF03_AZoneIdWithNoGroundRowStopsTheLoadOnTheRootAndAnOverlay(string zone)
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
        using var overlay = new OverlayCopy(clean.Root, "parks/ember-keep.json");
        overlay.ChangeObject("parks/ember-keep.json",
            json => json["zones"] = new JsonObject { [zone] = "astroturf" });
        var onOverlay = Assert.Single(ContentDataValidator.Validate(overlay.Root), e =>
            e.Contains($"park 'ember-keep' zones.{zone}", StringComparison.Ordinal));
        Assert.StartsWith(overlay.Path("parks/ember-keep.json"), onOverlay, StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(overlay.Root));
    }

    /// <summary>
    /// <c>SF-03</c>, the wall side. No span names a material yet — that is the polyline fence's (F2-c) —
    /// so the refusal is proved where it lives: asking the library for a material it has no row for is
    /// a stop that names the id and the file, not a carom off whatever row happens to be first.
    /// </summary>
    [Fact]
    public void SF03_AWallMaterialWithNoRowIsAStopThatNamesTheIdAndTheFile()
    {
        var catalog = Game;
        var thrown = Assert.Throws<ArgumentException>(() => catalog.Rules.Walls.Of("brick"));
        Assert.Contains("'brick' is not a wall material with a row in rules/walls.json", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("[padded, glass]", thrown.Message, StringComparison.Ordinal);
        Assert.False(catalog.Rules.Walls.Has("brick"));
        Assert.False(catalog.Rules.Walls.Has(null));
    }

    /// <summary>And the same for a ground asked for in code rather than written in a park file.</summary>
    [Fact]
    public void SF03_AGroundWithNoRowIsAStopThatNamesTheIdAndTheFile()
    {
        var catalog = Game;
        var thrown = Assert.Throws<ArgumentException>(() => catalog.Rules.Grounds.Of("mud"));
        Assert.Contains("'mud' is not a ground with a row in rules/grounds.json", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("[grass, dirt, ice, ash, sand]", thrown.Message, StringComparison.Ordinal);
        Assert.False(catalog.Rules.Grounds.Has("mud"));
        Assert.False(catalog.Rules.Grounds.Has(null));
        // Case is part of the id: the library spells them lowercase and so does a park file.
        Assert.False(catalog.Rules.Grounds.Has("Grass"));
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
    /// No park names a <c>zones</c> block, and every park's resolved map is the one
    /// derived from its <c>surface</c>: the outfield and the apron are the surface, the infield and the
    /// track are dirt. That derivation is chosen so <c>surface</c> keeps exactly the meaning it has
    /// today — so this child gives the field a ground map without moving any park's ground.
    /// </summary>
    [Fact]
    public void TheDefaultZoneMapOfEveryParkIsItsSurfacePlusDirt()
    {
        var catalog = Game;
        Assert.Equal(ParkId.All.Count, catalog.Parks.Count);
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

    /// <summary>
    /// The four surfaces the six parks actually name are all four library ids, so every row in the file
    /// is a row some park is standing on — not four rows of which two are decoration.
    /// </summary>
    [Fact]
    public void EveryGroundInTheLibraryIsSomeParksSurface()
    {
        var catalog = Game;
        Assert.Equal(
            catalog.Rules.Grounds.Ids.OrderBy(x => x, StringComparer.Ordinal),
            catalog.Parks.Values.Select(p => p.Surface).Distinct().OrderBy(x => x, StringComparer.Ordinal));
    }

    /// <summary>
    /// The lip, the track and the apron, named point by point at Harbor. Every edge is read from the park and
    /// the table (fences 232 / 280 / 232, lip 137.78), never hard-coded.
    /// </summary>
    [Fact]
    public void ZoneAtNamesTheDirtTheGrassTheTrackAndTheApron()
    {
        var catalog = Game;
        var park = catalog.Parks[ParkId.Harbor];
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

    /// <summary>
    /// The boundaries are read, not chosen (#730 / #732). Swept over all six parks and a
    /// grid of bearings and radii, <see cref="GroundZones.ZoneAt"/> agrees with the geometry that
    /// already existed — the chalk (<see cref="FieldBounds.IsFair"/>), the lip
    /// (<see cref="FieldingResolver.OutfieldGrass"/>) and the track's inner edge
    /// (<see cref="ParkDiamond.TrackInner"/>) — computed here independently. F3-b moved none of them,
    /// and this row is what would fail if a later child moved one by accident.
    /// </summary>
    [Fact]
    public void ZoneAtIsTheGeometryTheFieldAlreadyHad()
    {
        var catalog = Game;
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
                    // noise rather than a rule. A grid point that lands on an edge (the
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

    /// <summary>
    /// A park may override one zone and leave the rest derived, and <c>GroundAt</c> answers the id the
    /// ball will be handed. Proved on a park built in this file, because no catalog park names
    /// a block — the lever exists and nothing pulls it (FR-06).
    /// </summary>
    [Fact]
    public void AParkMayOverrideOneZoneAndLeaveTheRestDerived()
    {
        var harbor = Game.Parks[ParkId.Harbor];
        Assert.Equal(Ground.Grass, harbor.Surface);

        var iced = harbor with { Zones = new ParkZones(InfieldDirt: Ground.Ice) };
        var zones = GroundZones.Of(iced, Game.Rules);
        Assert.Equal(Ground.Ice, zones.InfieldDirt);
        Assert.Equal(Ground.Grass, zones.Outfield);
        Assert.Equal(Ground.Dirt, zones.WarningTrack);
        Assert.Equal(Ground.Grass, zones.FoulApron);

        Assert.Equal(Ground.Ice, zones.GroundAt(0, 60));
        Assert.Equal(Ground.Grass, zones.GroundAt(0, 250));
        Assert.Equal(Ground.Grass, zones.GroundAt(60, 40));

        // An empty block is not an override: every zone is the derived one.
        var empty = GroundZones.Of(harbor with { Zones = new ParkZones() }, Game.Rules);
        Assert.False(new ParkZones().Names);
        Assert.True(new ParkZones(Outfield: Ground.Ash).Names);
        foreach (var (zone, ground) in empty.All())
            Assert.Equal(GroundZones.Of(harbor, Game.Rules).IdOf(zone), ground);
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
        var park = content.Parks[ParkId.Harbor];
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
    /// <see cref="ParkBoundary.Default"/> and <see cref="FieldBounds.Of(Park, Rules.Default)"/> take, for the callers
    /// that hold no catalog. It is pinned here rather than left to a comment, because a reader who
    /// used it from inside a match would be reading the process's lip instead of the match's, and that
    /// is exactly the class of bug the F3-a audit was opened for (map finding 3). A match hands its
    /// own table over; `Of(park)` is for the ones that cannot.
    /// </summary>
    [Fact]
    public void TheNoTableOverloadTakesTheProcessWideTableAndNothingElse()
    {
        var park = Game.Parks[ParkId.Harbor] with { Zones = new ParkZones(WarningTrack: Ground.Ash) };
        Assert.Equal(GroundZones.Of(park, Rules.Default), GroundZones.Of(park, rules: Rules.Default));
        Assert.Equal(Rules.Default.Flight.Classes.InfieldLipFt, GroundZones.Of(park, rules: Rules.Default).InfieldLipFt);
        // The zones themselves come from the park either way; only the lip is the table's.
        Assert.Equal(Ground.Ash, GroundZones.Of(park, rules: Rules.Default).WarningTrack);
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
        var catalog = Game;
        foreach (var park in catalog.Parks.Values.Where(p => p.Environment is null))
            Assert.Same(catalog.Rules, catalog.Rules.AtPark(park));

        // A park that names zones and no air is still the global table: zones are not on it.
        var zoned = catalog.Parks[ParkId.Harbor] with { Zones = new ParkZones(Outfield: Ground.Ice) };
        Assert.Same(catalog.Rules, catalog.Rules.AtPark(zoned));
        Assert.Equal(Ground.Ice, GroundZones.Of(zoned, catalog.Rules).Outfield);
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
        var (home, away) = PresetTeams.Pair(Game, "rio", "ashlord");
        foreach (var park in Game.Parks.Values)
        {
            var match = new Match(Game, away, home, park, innings: 3, seed: 7);
            // The quoted key, not the bare word: `batting.spray.outOfZoneSpanDeg` contains "zones".
            Assert.DoesNotContain("\"zones\"", PlayTraceIdentity.Capture(match).InputsJson, StringComparison.Ordinal);
        }

        var plain = Game.Parks[ParkId.Harbor];
        var iced = plain with { Zones = new ParkZones(Outfield: Ground.Ice) };
        var identity = PlayTraceIdentity.Capture(new Match(Game, away, home, iced, innings: 3, seed: 7));
        Assert.Contains("\"zones\":{\"outfield\":\"ice\"", identity.InputsJson, StringComparison.Ordinal);
        Assert.NotEqual(
            PlayTraceIdentity.Capture(new Match(Game, away, home, plain, innings: 3, seed: 7)).Sha256,
            identity.Sha256);
    }

    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The shipped ground, written out (FD-05): what <c>flight.json</c>'s <c>roll</c> / <c>bounce</c> /
    /// <c>skid</c> and <c>fielding.json</c>'s <c>overthrow.decelFtPerSec2</c> and <c>handling.bobbleRestitution</c>
    /// / <c>bobbleGroundRetain</c> / <c>bobbleDecelFtPerSec2</c> carried when F3-c moved them.
    /// </summary>
    static void AssertTodaysGround(string what, GroundRules row)
    {
        Assert.True((22.0, 1.4) == (row.Roll.Friction, row.Roll.RestSpeed), $"{what}.roll");
        Assert.True((0.48, 0.82, 3.6) == (row.Bounce.Restitution, row.Bounce.Horizontal, row.Bounce.MinVy), $"{what}.bounce");
        Assert.True((14.0, 22.0, 2.2, 0.28, 0.93)
                    == (row.Skid.ImpactMinDeg, row.Skid.ImpactMaxDeg, row.Skid.MinVy, row.Skid.Restitution, row.Skid.Horizontal), $"{what}.skid");
        Assert.True(18.0 == row.Overthrow.DecelFtPerSec2, $"{what}.overthrow");
        Assert.True((0.35, 0.90, 6.0) == (row.Bobble.Restitution, row.Bobble.GroundRetain, row.Bobble.DecelFtPerSec2), $"{what}.bobble");
    }

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

    /// <summary>Every field the type declares is a key in the file (§16: the JSON is the only source of a rule).</summary>
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
            CopyTree(Shipped.Content.Root.Shipped, Root);
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
    /// A throwaway overlay laid over a throwaway data root, carrying whole copies of the named files from that
    /// root, so an <c>SF-03</c> row can break the overlay's copy of a park and read the refusal back against
    /// the overlay's own path.
    /// </summary>
    sealed class OverlayCopy : IDisposable
    {
        readonly string _shipped;

        public OverlayCopy(string shipped, params string[] files)
        {
            _shipped = shipped;
            Dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "grand-sluggers-overlay-" + Guid.NewGuid().ToString("N"));
            foreach (var file in files)
            {
                var to = Path(file);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(to)!);
                File.Copy(System.IO.Path.Combine(shipped, file.Replace('/', System.IO.Path.DirectorySeparatorChar)), to);
            }
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
