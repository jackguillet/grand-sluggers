using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The closed hazard pattern library (§14, §16; FD-09, FR-02, FR-08; F4-a, #847). A hazard type is a
/// data row in <c>data/rules/hazards.json</c> that names its pattern and carries the numbers that are
/// its own; <see cref="ParkHazards"/> dispatches on the pattern, never on a type string, and
/// <see cref="ContentDataValidator"/> takes its set of legal types from that table rather than from a
/// list of its own.
///
/// <para>
/// <b>The oracle.</b> <see cref="OldDispatch"/> below is the pre-#847 code's shape: dispatch by type
/// string, the shipped numbers typed out (the redirect pad, Ember's night widening), and the
/// <c>park.Id != "funfair-park"</c> chomper discs as literals. Every dispatch test compares the pattern
/// dispatch against it over a grid of points, so "the table plays what the numbers say" is a
/// measurement and not a claim. It is dead weight the day F4-b changes what a hazard does.
/// </para>
/// </summary>
public sealed class HazardLibraryTests
{
    static readonly ContentCatalog Content = Shipped.Content;
    static readonly RulesTable Table = Content.Rules;

    static readonly JsonDocumentOptions JsonComments = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    static IEnumerable<Park> Parks => Content.ParkPickOrder.Select(id => Content.Parks[id]);

    /// <summary>
    /// The park a match plays at <paramref name="park"/> by day or at night, hazards on — the one resolution
    /// (<see cref="PlayedPark.Of"/>, FD-11, F4-d). The oracle below takes the catalog park and the clock, as
    /// the pre-#847 code did; the live dispatch takes the played park, as <see cref="Match"/> hands it over.
    /// </summary>
    static Park Played(Park park, bool night) => PlayedPark.Of(park, night, hazards: true, Table.Hazards);

    // ---------------------------------------------------------------------------------
    // The table
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The library and the table are one list. Every id has a property named for it, every property
    /// is an id, and the key is derived from the id rather than typed twice — so a type cannot be
    /// spelled one way in code and another in <c>data/</c>.
    /// </summary>
    [Fact]
    public void EveryLibraryIdHasOneAuthoredRowAndNoRowIsAnythingElse()
    {
        Assert.Equal(12, HazardType.All.Count);
        Assert.Equal(HazardType.All, Table.Hazards.Authored);

        var properties = typeof(HazardRules).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(HazardTypeRules))
            .Select(p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..])
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(HazardType.All.Select(HazardType.Key).OrderBy(n => n, StringComparer.Ordinal), properties);

        var json = JsonNode.Parse(File.ReadAllText(RulesTable.PathFor(Content.Root, "hazards")), null, JsonComments)!.AsObject();
        Assert.Equal(
            HazardType.All.Select(HazardType.Key).OrderBy(n => n, StringComparer.Ordinal),
            json.Select(kv => kv.Key).OrderBy(n => n, StringComparer.Ordinal));
    }

    /// <summary>The twelve rows, by name. This is the table the rest of the file measures against.</summary>
    [Fact]
    public void EveryTypeNamesThePatternItPlaysAndTheNumbersThatAreItsOwn()
    {
        var root = Content.Root;
        var hazards = RulesTable.Load(root).Hazards;
        Assert.Equal(HazardPattern.StatusVolume, hazards.Of(HazardType.FreezeVolume).Pattern);
        Assert.Equal(HazardPattern.StatusVolume, hazards.Of(HazardType.LavaPit).Pattern);
        Assert.Equal(HazardPattern.StatusVolume, hazards.Of(HazardType.FireBreath).Pattern);
        Assert.Equal(HazardPattern.BallRedirect, hazards.Of(HazardType.WarpPipe).Pattern);
        Assert.Equal(HazardPattern.BallRedirect, hazards.Of(HazardType.Barrel).Pattern);
        Assert.Equal(HazardPattern.RewardTarget, hazards.Of(HazardType.Billboard).Pattern);
        Assert.Equal(HazardPattern.WallTrait, hazards.Of(HazardType.ClimbWall).Pattern);
        Assert.Equal(HazardPattern.BallRedirect, hazards.Of(HazardType.Chomper).Pattern); // FD-09-R2, F4-c
        // F4-f: the four types that were scenery act now — three solid bodies and the mover.
        foreach (var body in new[] { HazardType.Statue, HazardType.AcUnit, HazardType.Tree })
            Assert.Equal(HazardPattern.SolidBody, hazards.Of(body).Pattern);
        Assert.Equal(HazardPattern.TimedMover, hazards.Of(HazardType.Train).Pattern);

        // The numbers that are the rows' own.
        Assert.Equal(1.6, hazards.Of(HazardType.FireBreath).NightRadiusMul);
        Assert.Equal(5.6, hazards.Of(HazardType.WarpPipe).ReachPadFt);
        Assert.Equal(5.6, hazards.Of(HazardType.Barrel).ReachPadFt);
        // The chomper row no longer says "night only" (F4-d, FD-11): Funfair's night block does.

        // And the numbers that are not: the slow's size and the sign's payout keep their homes,
        // because a star swing sets the same slow and the specials are outside this phase.
        var rules = RulesTable.Load(root);
        Assert.Equal(0.45, rules.Fielding.Chase.FrozenMul);
        Assert.Equal(0.4, rules.Fielding.Drops.Frozen);
        Assert.Equal(1.0, rules.Stars.Gains.Billboard);
        Assert.Equal(0.6, rules.Fielding.Park.ShellWarpChance);

        // Only a status volume widens at night, only a redirect has a pad, and only a status volume slows a body for a
        // time (F4-b, FD-08-R2: 3.0 s).
        foreach (var type in HazardType.All)
        {
            var row = hazards.Of(type);
            Assert.True(row.NightRadiusMul == 1 || row.Pattern == HazardPattern.StatusVolume, type);
            Assert.True(row.ReachPadFt == 0 || row.Pattern == HazardPattern.BallRedirect, type);
            Assert.Equal(row.Pattern == HazardPattern.StatusVolume ? (double?)3.0 : null, row.SlowSec);
        }
    }

    // ---------------------------------------------------------------------------------
    // SF-03 — an unknown id, an unauthored id, and an unknown pattern all stop the load
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// SF-03 (the hazard half): a hazard type with no authored row stops the load and names the id.
    /// On the shipped root and through an overlay, because an overlay writes whole files and could
    /// carry a park the shipped table does not agree with.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SF03_AParkHazardTypeOutsideTheLibraryStopsTheLoadAndNamesIt(bool overlay)
    {
        using var fixture = new HazardFixture(overlay);
        fixture.Park("funfair-park", json => json["hazards"]![0]!["type"] = "sprinkler");

        var errors = ContentDataValidator.Validate(fixture.Root());
        Assert.Contains(errors, e => e.Contains("hazard[0] type must be one of", StringComparison.Ordinal)
            && e.Contains("got 'sprinkler'", StringComparison.Ordinal));
        // The list it offers is the table's authored rows, not a set this validator keeps.
        Assert.Contains(errors, e => e.Contains(
            "[" + string.Join(", ", HazardType.All.OrderBy(t => t, StringComparer.Ordinal)) + "]", StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root()));
    }

    /// <summary>
    /// SF-03: a type that <i>is</i> in the library but has no row is a different mistake and gets a
    /// different message — the D20 refusal, applied to hazards. The rules table reports the hole and
    /// every park that names the type reports it again, so the failure names both ends.
    /// </summary>
    [Fact]
    public void SF03_ALibraryTypeWithNoAuthoredRowStopsTheLoadAndNamesIt()
    {
        using var fixture = new HazardFixture(overlay: false);
        fixture.Hazards(json => json["warpPipe"] = null);

        var errors = RulesTable.Validate(fixture.Root());
        Assert.Contains(errors, e => e.Contains(
            "hazards.warpPipe is in the library but has no authored row", StringComparison.Ordinal));

        var content = ContentDataValidator.Validate(fixture.Root());
        Assert.Contains(content, e => e.Contains("park 'funfair-park' hazard[0] type 'warp_pipe' is in the library "
            + "but has no authored row in hazards.json", StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root()));

        // In code the same hole is a named stop, never a hazard that quietly does nothing.
        var table = Rules.Default.Hazards with { WarpPipe = null! };
        var ex = Assert.Throws<InvalidOperationException>(() => table.Of(HazardType.WarpPipe));
        Assert.Contains("is in the library but has no authored row", ex.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => table.Of("sprinkler"));
    }

    /// <summary>
    /// SF-03 through an overlay: the overlay's own rule gets there first. An overlay writes whole files
    /// (#716), so a copy of <c>hazards.json</c> that dropped a row is refused by name before the
    /// table is ever built — which is the stricter of the two nets, not a hole in this one.
    /// </summary>
    [Fact]
    public void SF03_AnOverlayCopyMissingARowIsRefusedByTheWholeFileRule()
    {
        using var fixture = new HazardFixture(overlay: true);
        fixture.Hazards(json => json.Remove("warpPipe"));

        var ex = Assert.Throws<InvalidDataException>(() => fixture.Root());
        Assert.Contains("carries part of rules/hazards.json (no warpPipe)", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// SF-03: a row whose pattern is not one the sim implements stops the load. A type that silently
    /// did nothing is exactly what <c>decoration</c> exists to say out loud, so it may not happen by
    /// typo.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SF03_ARowWithAnUnknownPatternStopsTheLoadAndNamesIt(bool overlay)
    {
        using var fixture = new HazardFixture(overlay);
        fixture.Hazards(json => json["tree"]!["pattern"] = "catchStealer");

        var errors = RulesTable.Validate(fixture.Root());
        Assert.Contains(errors, e => e.Contains("hazards.tree.pattern must be one of", StringComparison.Ordinal)
            && e.Contains("got 'catchStealer'", StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root()));
    }

    /// <summary>A number its pattern never reads is a dead rule, the way a sweep with no start is (#818).</summary>
    [Fact]
    public void ANumberThePatternNeverReadsIsRefused()
    {
        using var fixture = new HazardFixture(overlay: false);
        fixture.Hazards(json =>
        {
            json["tree"]!["nightRadiusMul"] = 1.4;
            json["statue"]!["reachPadFt"] = 3;
        });

        var errors = RulesTable.Validate(fixture.Root());
        Assert.Contains(errors, e => e.Contains(
            "hazards.tree.nightRadiusMul is 1.4, but only a statusVolume widens at night", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains(
            "hazards.statue.reachPadFt is 3, but only a ballRedirect has a reach pad", StringComparison.Ordinal));
    }

    /// <summary>
    /// The train is a mover since F4-f, so it carries a disc like everything that acts; a hazard with no disc is refused.
    /// </summary>
    [Fact]
    public void EveryActingHazardMustHaveADisc()
    {
        var train = Content.Parks["funfair-park"].Hazards.Single(h => h.Type == HazardType.Train);
        Assert.Equal(6, train.Radius);
        Assert.Equal(HazardPattern.TimedMover, Table.Hazards.Of(HazardType.Train).Pattern);
        Assert.Empty(ContentDataValidator.Validate(Content.Root));

        using var fixture = new HazardFixture(overlay: false);
        fixture.Park("funfair-park", json => json["hazards"]![0]!["radius"] = 0);
        Assert.Contains(
            ContentDataValidator.Validate(fixture.Root()),
            e => e.Contains("hazard[0] radius must be greater than 0", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------
    // The dispatch is the oracle's dispatch
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Every status volume in every park, against the oracle, by day and by night, at eight radii
    /// around each disc's rim. The night ring is the one that matters: the oracle widens a
    /// <c>fire_breath</c> and nothing else, and the table widens a row whose
    /// <c>nightRadiusMul</c> is not 1 — the same three discs, the same 1.6.
    /// </summary>
    [Fact]
    public void TheStatusVolumeDispatchEqualsTheOracle()
    {
        foreach (var park in Parks)
            foreach (var (x, z) in Probes(park))
                foreach (var night in new[] { false, true })
                    Assert.Equal(
                        OldDispatch.InSlow(park, x, z, night),
                        ParkHazards.InSlow(park, x, z, Table, night));
    }

    static BallHazards Live(Park park, bool night = false)
    {
        var b = new BallHazards();
        b.Begin(park, night, Table);
        return b;
    }

    /// <summary>
    /// Every redirect's mouth, against the oracle (F4-c): a ball on the ground is in a mouth exactly where the old landing
    /// test caught a grounder — the same disc, radius plus the reach pad — so the redirect moved from the landing to the live
    /// ball without moving a disc. Where it goes is now the live ball's (<c>BallRedirectTests</c>).
    /// </summary>
    [Fact]
    public void TheBallRedirectMouthIsTheOraclesDisc()
    {
        foreach (var park in Parks)
            foreach (var (x, z) in Probes(park))
            {
                var mouth = Live(park).Entered(x, 0, z);
                Assert.Equal(OldDispatch.WarpIfPipe(park, x, z, new Random(847)).Warped, mouth is not null && mouth.Type != HazardType.Chomper);
            }
    }

    /// <summary>Every sign, against the oracle: a ball at the ground is under a sign exactly where the old landing test paid.</summary>
    [Fact]
    public void TheRewardTargetDiscIsTheOraclesDisc()
    {
        foreach (var park in Parks)
            foreach (var (x, z) in Probes(park))
                Assert.Equal(OldDispatch.HitStarSign(park, x, z), Live(park).Reward(x, 0, z) is not null);
    }

    /// <summary>
    /// The wall trait, against the oracle, for a Clamber fielder and for one without the
    /// ability, in every park. It is still a park-wide flag and the row's disc is still never read.
    /// </summary>
    [Fact]
    public void TheWallTraitDispatchEqualsTheOracle()
    {
        foreach (var park in Parks)
            foreach (var id in new[] { "konga", "rio", "ashlord" })
            {
                var fielder = Content.Must(id);
                Assert.Equal(OldDispatch.CanClamber(park, fielder), ParkHazards.CanClamber(park, fielder, Table));
            }
    }

    /// <summary>
    /// The chompers' mouths, against the oracle's discs (FD-09-R2, F4-c): a fly on its way down (6 ft up) is in a chomper
    /// exactly where the old catch stealer ate one — the three literal discs, by night only, at Funfair only. The mouth is a
    /// redirect now, not an out.
    /// </summary>
    [Fact]
    public void TheChomperMouthIsTheOraclesDisc()
    {
        foreach (var park in Parks)
            foreach (var (x, z) in Probes(Played(park, night: true)))
                foreach (var night in new[] { false, true })
                    Assert.Equal(
                        OldDispatch.ChompFly(park, night, x, z, grounder: false),
                        Live(Played(park, night), night).Entered(x, 6, z) is { Type: HazardType.Chomper });
    }

    /// <summary>
    /// The chomper rows are the oracle's literal discs, typed out here rather than read from the sim,
    /// because the sim has no copy of them. They are Funfair's night block (FD-11): the park a night
    /// match plays holds them after the day's four instances, in file order, and the park a day match
    /// plays has none.
    /// </summary>
    [Fact]
    public void TheChomperRowsAreTheOracleLiterals()
    {
        var funfair = Content.Parks["funfair-park"];
        var rows = Played(funfair, night: true).Hazards
            .Where(h => h.Type == HazardType.Chomper)
            .ToList();
        Assert.Equal(
            [(-50.0, 144.0, 11.2, "L"), (0.0, 160.0, 12.6, "C"), (55.0, 139.0, 11.2, "R")],
            rows.Select(h => (h.X, h.Z, h.Radius, h.Tag)).ToList());
        Assert.Equal(
            OldDispatch.FunfairChompers.Select(h => (h.X, h.Z, h.Radius, h.Tag)),
            rows.Select(h => (h.X, h.Z, h.Radius, h.Tag)));
        Assert.Equal(funfair.Night!.Hazards, rows);
        Assert.DoesNotContain(funfair.Hazards, h => h.Type == HazardType.Chomper);
        Assert.DoesNotContain(Played(funfair, night: false).Hazards, h => h.Type == HazardType.Chomper);

        // And nowhere else: the mouths are one park's rows, not a rule about a park.
        foreach (var park in Parks.Where(p => p.Id != "funfair-park"))
            Assert.DoesNotContain(Played(park, night: true).Hazards, h => h.Type == HazardType.Chomper);
    }

    // ---------------------------------------------------------------------------------
    // Decorations
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// No library type is scenery since F4-f: the four that were (statue, AC unit, tree, train) are solid bodies and a mover,
    /// every one in some park. The decoration pattern stays for the next piece of scenery. A park whose only hazards are bodies
    /// lends no Clamber reach, redirects nothing and pays nothing.
    /// </summary>
    [Fact]
    public void TheFourFormerDecorationsAreBodies()
    {
        Assert.DoesNotContain(HazardType.All, t => Table.Hazards.Of(t).Pattern == HazardPattern.Decoration);
        var bodies = Parks.SelectMany(p => p.Hazards).Where(h => Table.Hazards.Of(h.Type).Pattern is HazardPattern.SolidBody or HazardPattern.TimedMover)
            .Select(h => h.Type).Distinct().OrderBy(t => t, StringComparer.Ordinal).ToList();
        Assert.Equal([HazardType.AcUnit, HazardType.Statue, HazardType.Train, HazardType.Tree], bodies);

        var konga = Content.Must("konga");
        var scenery = new Park(
            "scenery", "Scenery", "none", "grass", 330, 400, 330, 0,
            [new Hazard(HazardType.Tree, 40, 200, 6, null), new Hazard(HazardType.Statue, -40, 200, 6, null)],
            WindDeg: 0, FenceHeightFt: 12);
        Assert.False(ParkHazards.CanClamber(scenery, konga, Table));
        Assert.Empty(Live(scenery).Mouths);
        Assert.Null(Live(scenery).Reward(40, 0, 200));
        Assert.Equal(2, SolidBodies.Of(scenery, Table).Count);
    }

    // ---------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------

    /// <summary>Points around every hazard in the park, plus the plate and a deep centre mark.</summary>
    static IEnumerable<(double X, double Z)> Probes(Park park)
    {
        yield return (0, 0);
        yield return (0, 380);
        foreach (var h in park.Hazards)
            foreach (var p in Ring(h.X, h.Z, h.Radius))
                yield return p;
    }

    /// <summary>The centre, then just inside and just outside the rim on four bearings, then either side of the redirect pad.</summary>
    static IEnumerable<(double X, double Z)> Ring(double x, double z, double radius)
    {
        yield return (x, z);
        var pad = OldDispatch.PipeReachPadFt;
        foreach (var d in new[] { radius - 0.1, radius + 0.1, radius + pad - 0.1, radius + pad + 0.1 })
        {
            if (d <= 0) continue;
            yield return (x + d, z);
            yield return (x - d, z);
            yield return (x, z + d);
            yield return (x, z - d);
        }
    }

    /// <summary>
    /// <b>ParkHazards in its pre-#847 shape</b>: dispatch by type string, with the shipped numbers
    /// typed out rather than read from the table, so a row that drifted from them fails by name.
    /// </summary>
    static class OldDispatch
    {
        const double EmberNightFireMul = 1.6;
        public const double PipeReachPadFt = 5.6;

        public static readonly Hazard[] FunfairChompers =
        [
            new("chomper", -50, 144, 11.2, "L"),
            new("chomper", 0, 160, 12.6, "C"),
            new("chomper", 55, 139, 11.2, "R")
        ];

        public static bool InSlow(Park park, double x, double z, bool night)
        {
            foreach (var h in park.Hazards)
            {
                if (h.Type is not ("freeze_volume" or "lava_pit" or "fire_breath")) continue;
                var r = h.Radius;
                if (night && h.Type == "fire_breath") r *= EmberNightFireMul;
                if (Diamond.Dist(h.X, h.Z, x, z) <= r) return true;
            }
            return false;
        }

        public static bool ChompFly(Park park, bool night, double x, double z, bool grounder)
        {
            if (!night || grounder || park.Id != "funfair-park") return false;
            foreach (var h in FunfairChompers)
                if (Diamond.Dist(h.X, h.Z, x, z) <= h.Radius) return true;
            return false;
        }

        public static (double X, double Z, bool Warped) WarpIfPipe(Park park, double x, double z, Random rng)
        {
            var pipes = park.Hazards.Where(h => h.Type is "warp_pipe" or "barrel").ToList();
            if (pipes.Count < 2) return (x, z, false);
            Hazard? hit = null;
            foreach (var p in pipes)
            {
                if (Diamond.Dist(p.X, p.Z, x, z) <= p.Radius + PipeReachPadFt)
                {
                    hit = p;
                    break;
                }
            }
            if (hit is null) return (x, z, false);
            var exits = pipes.Where(p => !ReferenceEquals(p, hit)).ToList();
            var dest = exits[rng.Next(exits.Count)];
            return (dest.X, dest.Z, true);
        }

        public static bool HitStarSign(Park park, double x, double z)
        {
            foreach (var h in park.Hazards)
            {
                if (h.Type != "billboard") continue;
                if (Diamond.Dist(h.X, h.Z, x, z) <= h.Radius) return true;
            }
            return false;
        }

        public static bool CanClamber(Park park, Character fielder) =>
            fielder.FieldAbility.Equals("clamber", StringComparison.OrdinalIgnoreCase) &&
            park.Hazards.Any(h => h.Type == "climb_wall");
    }

    /// <summary>
    /// A whole copy of the shipped data root that a test may break, optionally with an overlay laid over
    /// it. The overlay carries whole-file copies of the hazard library and of any park a test changes
    /// (#716: an overlay may only carry files the shipped root has, whole), so a change lands in the
    /// overlay's copy, which is the one that is read.
    /// </summary>
    sealed class HazardFixture : IDisposable
    {
        readonly string _shipped;
        readonly string? _overlay;

        public HazardFixture(bool overlay)
        {
            var source = Shipped.Content.Root.Shipped;
            var temp = Path.Combine(Path.GetTempPath(), "grand-sluggers-hazards-" + Guid.NewGuid().ToString("N"));
            _shipped = Path.Combine(temp, "data");
            Copy(source, _shipped);
            if (overlay)
            {
                _overlay = Path.Combine(temp, "overlay");
                CopyFile(Path.Combine("rules", "hazards.json"));
            }
        }

        public DataRoot Root() => _overlay is null ? new DataRoot(_shipped) : new DataRoot(_shipped, _overlay);

        /// <summary>Change the hazard library. On an overlay fixture the overlay's copy is the one that is read.</summary>
        public void Hazards(Action<JsonObject> change) =>
            Edit(Path.Combine(_overlay ?? _shipped, "rules", "hazards.json"), change);

        /// <summary>Change a park. On an overlay fixture the park is copied into the overlay whole, and that copy is changed.</summary>
        public void Park(string id, Action<JsonObject> change)
        {
            var relative = Path.Combine("parks", id + ".json");
            if (_overlay is not null) CopyFile(relative);
            Edit(Path.Combine(_overlay ?? _shipped, relative), change);
        }

        void CopyFile(string relative)
        {
            var destination = Path.Combine(_overlay!, relative);
            if (File.Exists(destination)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(Path.Combine(_shipped, relative), destination);
        }

        static void Edit(string path, Action<JsonObject> change)
        {
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
            var temp = Path.GetFullPath(Path.Combine(_shipped, ".."));
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }
}
