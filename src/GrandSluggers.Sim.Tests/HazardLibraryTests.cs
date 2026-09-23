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
/// <b>The parity oracle.</b> <see cref="OldDispatch"/> below is the pre-#847 code, verbatim: the type
/// strings, the <c>fielding.park</c> numbers at their shipped values, and the
/// <c>park.Id != "funfair-park"</c> chomper literals. Every dispatch test compares the new pattern
/// dispatch against it over a grid of points, so "same outcomes as today" is a measurement and not a
/// claim. It is dead weight the day F4-b changes what a hazard does — and it is the only thing that
/// can prove F4-a did not.
/// </para>
/// </summary>
public sealed class HazardLibraryTests
{
    static readonly ContentCatalog Content = ContentCatalog.Load();
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
    /// The JSON is the source of truth and the C# initializers are its load fallback (§16). Checked
    /// here as well as in <c>RulesTests</c> because this table's leaves are mostly strings and bools,
    /// and a pattern that drifted between the file and the code would move every dispatch at once.
    /// </summary>
    [Fact]
    public void ShippedJsonEqualsTheCodeFallbackRowForRow()
    {
        var loaded = RulesTable.Load(Content.Root).Hazards;
        var defaults = RulesTable.Defaults.Hazards;
        foreach (var type in HazardType.All)
        {
            var json = loaded.Of(type);
            var code = defaults.Of(type);
            Assert.Equal(code.Pattern, json.Pattern);
            Assert.Equal(code.NightRadiusMul, json.NightRadiusMul);
            Assert.Equal(code.ReachPadFt, json.ReachPadFt);
        }
        // nightOnly left the rows with F4-d (FD-11): whether an instance exists only at night is where
        // the park authors it, its night block, so no row carries it and no row can drift from it.
        Assert.Null(typeof(HazardTypeRules).GetProperty("NightOnly"));
    }

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

        foreach (var root in Roots())
        {
            var json = JsonNode.Parse(File.ReadAllText(RulesTable.PathFor(root, "hazards")), null, JsonComments)!.AsObject();
            Assert.Equal(
                HazardType.All.Select(HazardType.Key).OrderBy(n => n, StringComparer.Ordinal),
                json.Select(kv => kv.Key).OrderBy(n => n, StringComparer.Ordinal));
        }
    }

    /// <summary>The twelve rows, by name, on both roots. This is the table the rest of the file measures against.</summary>
    [Fact]
    public void EveryTypeNamesThePatternItPlaysAndTheNumbersThatAreItsOwn()
    {
        foreach (var (root, pad) in new[] { (Content.Root, 8.0), (TrialRoot(), 5.6) })
        {
            var hazards = RulesTable.Load(root).Hazards;
            Assert.Equal(HazardPattern.StatusVolume, hazards.Of(HazardType.FreezeVolume).Pattern);
            Assert.Equal(HazardPattern.StatusVolume, hazards.Of(HazardType.LavaPit).Pattern);
            Assert.Equal(HazardPattern.StatusVolume, hazards.Of(HazardType.FireBreath).Pattern);
            Assert.Equal(HazardPattern.BallRedirect, hazards.Of(HazardType.WarpPipe).Pattern);
            Assert.Equal(HazardPattern.BallRedirect, hazards.Of(HazardType.Barrel).Pattern);
            Assert.Equal(HazardPattern.RewardTarget, hazards.Of(HazardType.Billboard).Pattern);
            Assert.Equal(HazardPattern.WallTrait, hazards.Of(HazardType.ClimbWall).Pattern);
            Assert.Equal(HazardPattern.CatchStealer, hazards.Of(HazardType.Chomper).Pattern);
            foreach (var inert in new[] { HazardType.Statue, HazardType.Train, HazardType.AcUnit, HazardType.Tree })
                Assert.Equal(HazardPattern.Decoration, hazards.Of(inert).Pattern);

            // The numbers that moved, at the values they moved from.
            Assert.Equal(1.6, hazards.Of(HazardType.FireBreath).NightRadiusMul);
            Assert.Equal(pad, hazards.Of(HazardType.WarpPipe).ReachPadFt);
            Assert.Equal(pad, hazards.Of(HazardType.Barrel).ReachPadFt);
            // The chomper row no longer says "night only" (F4-d, FD-11): Funfair's night block does.

            // And the numbers that did not: the slow's size and the sign's payout keep their homes,
            // because a star swing sets the same slow and the specials are outside this phase.
            var rules = RulesTable.Load(root);
            Assert.Equal(0.45, rules.Fielding.Chase.FrozenMul);
            Assert.Equal(0.4, rules.Fielding.Drops.Frozen);
            Assert.Equal(1.0, rules.Stars.Gains.Billboard);
            Assert.Equal(0.6, rules.Fielding.Park.ShellWarpChance);

            // Only a status volume widens at night, and only a redirect has a pad.
            foreach (var type in HazardType.All)
            {
                var row = hazards.Of(type);
                Assert.True(row.NightRadiusMul == 1 || row.Pattern == HazardPattern.StatusVolume, type);
                Assert.True(row.ReachPadFt == 0 || row.Pattern == HazardPattern.BallRedirect, type);
            }
        }
    }

    // ---------------------------------------------------------------------------------
    // SF-03 — an unknown id, an unauthored id, and an unknown pattern all stop the load
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// SF-03 (the hazard half): a hazard type with no authored row stops the load and names the id.
    /// Both roots, because a trial writes whole files and could drop a row the shipped table has.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SF03_AParkHazardTypeOutsideTheLibraryStopsTheLoadAndNamesIt(bool trial)
    {
        using var fixture = new HazardFixture(trial);
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
        using var fixture = new HazardFixture(trial: false);
        fixture.Hazards(json => json["warpPipe"] = null);

        var errors = RulesTable.Validate(fixture.Root());
        Assert.Contains(errors, e => e.Contains(
            "hazards.warpPipe is in the library but has no authored row", StringComparison.Ordinal));

        var content = ContentDataValidator.Validate(fixture.Root());
        Assert.Contains(content, e => e.Contains("park 'funfair-park' hazard[0] type 'warp_pipe' is in the library "
            + "but has no authored row in hazards.json", StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root()));

        // In code the same hole is a named stop, never a hazard that quietly does nothing.
        var table = new HazardRules { WarpPipe = null! };
        var ex = Assert.Throws<InvalidOperationException>(() => table.Of(HazardType.WarpPipe));
        Assert.Contains("is in the library but has no authored row", ex.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => table.Of("sprinkler"));
    }

    /// <summary>
    /// SF-03 on a trial root: the overlay's own rule gets there first. A trial writes whole files
    /// (#716), so a copy of <c>hazards.json</c> that dropped a row is refused by name before the
    /// table is ever built — which is the stricter of the two nets, not a hole in this one.
    /// </summary>
    [Fact]
    public void SF03_ATrialCopyMissingARowIsRefusedByTheWholeFileRule()
    {
        using var fixture = new HazardFixture(trial: true);
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
    public void SF03_ARowWithAnUnknownPatternStopsTheLoadAndNamesIt(bool trial)
    {
        using var fixture = new HazardFixture(trial);
        fixture.Hazards(json => json["tree"]!["pattern"] = "solidBody");

        var errors = RulesTable.Validate(fixture.Root());
        Assert.Contains(errors, e => e.Contains("hazards.tree.pattern must be one of", StringComparison.Ordinal)
            && e.Contains("got 'solidBody'", StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root()));
    }

    /// <summary>A number its pattern never reads is a dead rule, the way a sweep with no start is (#818).</summary>
    [Fact]
    public void ANumberThePatternNeverReadsIsRefused()
    {
        using var fixture = new HazardFixture(trial: false);
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
    /// The train's radius is no longer a special case by name. A decoration is drawn and never
    /// played, so it may carry no disc; anything that acts still needs one.
    /// </summary>
    [Fact]
    public void ADecorationMayHaveNoDiscAndEveryActingHazardMustHaveOne()
    {
        var train = Content.Parks["funfair-park"].Hazards.Single(h => h.Type == HazardType.Train);
        Assert.Equal(0, train.Radius);
        Assert.Equal(HazardPattern.Decoration, Table.Hazards.Of(HazardType.Train).Pattern);
        Assert.Empty(ContentDataValidator.Validate(Content.Root));

        using var fixture = new HazardFixture(trial: false);
        fixture.Park("funfair-park", json => json["hazards"]![0]!["radius"] = 0);
        Assert.Contains(
            ContentDataValidator.Validate(fixture.Root()),
            e => e.Contains("hazard[0] radius must be greater than 0", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------
    // The dispatch is the old dispatch
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Every status volume in every park, against the pre-#847 code, by day and by night, at eight
    /// radii around each disc's rim. The night ring is the one that matters: the old code widened a
    /// <c>fire_breath</c> and nothing else, and the new one widens a row whose
    /// <c>nightRadiusMul</c> is not 1 — the same three discs, the same 1.6.
    /// </summary>
    [Fact]
    public void TheStatusVolumeDispatchEqualsTheOldOne()
    {
        foreach (var park in Parks)
            foreach (var (x, z) in Probes(park))
                foreach (var night in new[] { false, true })
                    Assert.Equal(
                        OldDispatch.InSlow(park, x, z, night),
                        ParkHazards.InSlow(park, x, z, night, Table));
    }

    /// <summary>
    /// Every redirect, against the pre-#847 code, on the same seeded stream: a pipe that catches the
    /// ball must catch it at the same reach and send it to the same exit, because the exit is a draw
    /// and a draw that moved would reseed every game from there.
    /// </summary>
    [Fact]
    public void TheBallRedirectDispatchEqualsTheOldOneIncludingTheDraw()
    {
        foreach (var park in Parks)
            foreach (var (x, z) in Probes(park))
                Assert.Equal(
                    OldDispatch.WarpIfPipe(park, x, z, new Random(847)),
                    ParkHazards.WarpIfPipe(park, x, z, new Random(847), Table));
    }

    /// <summary>Every sign, against the pre-#847 code. The <c>tag</c> is still not read.</summary>
    [Fact]
    public void TheRewardTargetDispatchEqualsTheOldOne()
    {
        foreach (var park in Parks)
            foreach (var (x, z) in Probes(park))
                Assert.Equal(OldDispatch.HitStarSign(park, x, z), ParkHazards.HitStarSign(park, x, z, Table));
    }

    /// <summary>
    /// The wall trait, against the pre-#847 code, for a Clamber fielder and for one without the
    /// ability, in every park. It is still a park-wide flag and the row's disc is still never read.
    /// </summary>
    [Fact]
    public void TheWallTraitDispatchEqualsTheOldOne()
    {
        foreach (var park in Parks)
            foreach (var id in new[] { "konga", "rio", "ashlord" })
            {
                var fielder = Content.Must(id);
                Assert.Equal(OldDispatch.CanClamber(park, fielder), ParkHazards.CanClamber(park, fielder, Table));
            }
    }

    /// <summary>
    /// The catch stealer, against the pre-#847 code — the one place where "today's outcome" had to
    /// survive a park id turning into three data rows. The old code tested three literal discs when
    /// the park was Funfair and it was night; the new one tests the <c>chomper</c> instances of the park
    /// a match plays, which on the shipped root are those three literals at the same places.
    ///
    /// <para>
    /// Re-read through the night block since F4-d (FD-11): the mouths are Funfair's night-block
    /// instances, so the live dispatch is handed the played park (<see cref="Played"/>) — by day without
    /// them, at night with them — and no longer takes the clock at all. The oracle is unchanged, and the
    /// probes ring the night park's instances, so every mouth is still probed by day and by night.
    /// </para>
    /// </summary>
    [Fact]
    public void TheCatchStealerDispatchEqualsTheOldOneOnTheShippedRoot()
    {
        foreach (var park in Parks)
            foreach (var (x, z) in Probes(Played(park, night: true)))
                foreach (var night in new[] { false, true })
                    foreach (var grounder in new[] { false, true })
                        Assert.Equal(
                            OldDispatch.ChompFly(park, night, x, z, grounder),
                            ParkHazards.ChompFly(Played(park, night), x, z, grounder, Table));
    }

    /// <summary>
    /// The chomper rows reproduce the literal discs. The old array is written out here rather than
    /// read from the sim, because the sim no longer has it — that is the point of the child. Since F4-d
    /// they are Funfair's night block (FD-11): the park a night match plays holds them after the day's
    /// four instances, in file order, and the park a day match plays has none.
    /// </summary>
    [Fact]
    public void TheChomperRowsReproduceTheCodeLiteralOnTheShippedRoot()
    {
        var funfair = Content.Parks["funfair-park"];
        var rows = Played(funfair, night: true).Hazards
            .Where(h => h.Type == HazardType.Chomper)
            .ToList();
        Assert.Equal(
            [(-72.0, 205.0, 16.0, "L"), (0.0, 228.0, 18.0, "C"), (78.0, 198.0, 16.0, "R")],
            rows.Select(h => (h.X, h.Z, h.Radius, h.Tag)).ToList());
        Assert.Equal(funfair.Night!.Hazards, rows);
        Assert.DoesNotContain(funfair.Hazards, h => h.Type == HazardType.Chomper);
        Assert.DoesNotContain(Played(funfair, night: false).Hazards, h => h.Type == HazardType.Chomper);

        // And nowhere else: the mouths are one park's rows, not a rule about a park.
        foreach (var park in Parks.Where(p => p.Id != "funfair-park"))
            Assert.DoesNotContain(Played(park, night: true).Hazards, h => h.Type == HazardType.Chomper);
    }

    /// <summary>
    /// The trial's rows are the shipped ones through the accepted migration rule (F693-04): the
    /// fence scale beyond the lip, radii × 0.70, rounded the way <c>trials/c80</c> spells them.
    /// <c>CompactGeometryTests</c> holds the rule for every hazard; this holds it for the three that
    /// are new, so a chomper row edited on one root alone fails here by name.
    /// </summary>
    [Fact]
    public void TheTrialChomperRowsFollowTheAcceptedZoneRule()
    {
        var trial = ContentCatalog.Load(TrialRoot());
        // The night block on each root (FD-11, F4-d), read as a night match plays it.
        var was = Played(Content.Parks["funfair-park"], night: true).Hazards.Where(h => h.Type == HazardType.Chomper).ToList();
        var now = PlayedPark.Of(trial.Parks["funfair-park"], night: true, hazards: true, trial.Rules.Hazards).Hazards
            .Where(h => h.Type == HazardType.Chomper).ToList();
        Assert.Equal(3, now.Count);
        for (var i = 0; i < was.Count; i++)
        {
            // Every mouth stood on the grass, so every one takes the fence scale.
            Assert.True(FieldingResolver.OutfieldGrass(was[i].X, was[i].Z, Content.Rules), was[i].Tag);
            Assert.Equal(was[i].Tag, now[i].Tag);
            Assert.Equal(Math.Round(was[i].X * 0.70, MidpointRounding.AwayFromZero), now[i].X, 6);
            Assert.Equal(Math.Round(was[i].Z * 0.70, MidpointRounding.AwayFromZero), now[i].Z, 6);
            Assert.Equal(Math.Round(was[i].Radius * 0.70, 2), now[i].Radius, 6);
        }
        Assert.Equal(
            [(-50.0, 144.0, 11.2), (0.0, 160.0, 12.6), (55.0, 139.0, 11.2)],
            now.Select(h => (h.X, h.Z, h.Radius)).ToList());
    }

    // ---------------------------------------------------------------------------------
    // Decorations
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The four inert types do nothing, on purpose and by name (map §5 Q6, Jack, 2026-09-22). Each
    /// is probed at its own centre and around its rim, in the park that lists it, through every
    /// pattern the sim runs: a decoration slows nobody, redirects nothing, pays nothing, steals no
    /// catch and lends no reach.
    /// </summary>
    [Fact]
    public void TheFourDecorationsDoNothing()
    {
        var konga = Content.Must("konga");
        var seen = new List<string>();
        foreach (var park in Parks)
            foreach (var h in park.Hazards)
            {
                if (Table.Hazards.Of(h.Type).Pattern != HazardPattern.Decoration) continue;
                seen.Add(h.Type);
                foreach (var (x, z) in Ring(h.X, h.Z, h.Radius))
                    foreach (var night in new[] { false, true })
                    {
                        var where = $"{park.Id} {h.Type} at ({x}, {z}) night {night}";
                        var played = Played(park, night);
                        Assert.False(ParkHazards.InSlow(played, x, z, night, Table), where);
                        Assert.False(ParkHazards.ChompFly(played, x, z, rules: Table), where);
                        Assert.False(ParkHazards.HitStarSign(played, x, z, Table), where);
                    }
            }

        // Every inert type was actually reached, so a silent "nothing listed one" cannot pass this.
        Assert.Equal(
            [HazardType.AcUnit, HazardType.Statue, HazardType.Train, HazardType.Tree],
            seen.Distinct().OrderBy(t => t, StringComparer.Ordinal).ToList());

        // A park whose only hazards are decorations lends no Clamber reach and no redirect.
        var scenery = new Park(
            "scenery", "Scenery", "none", "grass", 330, 400, 330, 0,
            [new Hazard(HazardType.Tree, 40, 200, 6, null), new Hazard(HazardType.Statue, -40, 200, 6, null)],
            WindDeg: 0, FenceHeightFt: 12);
        Assert.False(ParkHazards.CanClamber(scenery, konga, Table));
        Assert.False(ParkHazards.WarpIfPipe(scenery, 40, 200, new Random(1), Table).Warped);
    }

    // ---------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------

    static DataRoot TrialRoot()
    {
        var shipped = Content.Root.Shipped;
        return new DataRoot(shipped, Path.GetFullPath(Path.Combine(shipped, "..", "trials", "c80")));
    }

    static IEnumerable<DataRoot> Roots() => [Content.Root, TrialRoot()];

    /// <summary>Points around every hazard in the park, plus the plate and a deep centre mark.</summary>
    static IEnumerable<(double X, double Z)> Probes(Park park)
    {
        yield return (0, 0);
        yield return (0, 380);
        foreach (var h in park.Hazards)
            foreach (var p in Ring(h.X, h.Z, h.Radius))
                yield return p;
    }

    /// <summary>The centre, then just inside and just outside the rim on four bearings, then well past the pad.</summary>
    static IEnumerable<(double X, double Z)> Ring(double x, double z, double radius)
    {
        yield return (x, z);
        foreach (var d in new[] { radius - 0.1, radius + 0.1, radius + 7.9, radius + 8.1 })
        {
            if (d <= 0) continue;
            yield return (x + d, z);
            yield return (x - d, z);
            yield return (x, z + d);
            yield return (x, z - d);
        }
    }

    /// <summary>
    /// <b>ParkHazards as it was before #847</b>, copied line for line. The numbers are the shipped
    /// <c>fielding.park</c> values it read; the trial's own pad is exercised through the live code in
    /// <c>CompactGeometryTests</c>, because this oracle exists to pin the shipped root's behaviour.
    /// </summary>
    static class OldDispatch
    {
        const double EmberNightFireMul = 1.6;
        const double PipeReachPadFt = 8;

        static readonly Hazard[] FunfairChompers =
        [
            new("chomper", -72, 205, 16, "L"),
            new("chomper", 0, 228, 18, "C"),
            new("chomper", 78, 198, 16, "R")
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

    /// <summary>A whole copy of a data root, optionally with the trial overlay laid over it, that a test may break.</summary>
    sealed class HazardFixture : IDisposable
    {
        readonly string _shipped;
        readonly string? _overlay;

        public HazardFixture(bool trial)
        {
            var source = ContentCatalog.Load().Root.Shipped;
            var repo = Path.GetFullPath(Path.Combine(source, ".."));
            var temp = Path.Combine(Path.GetTempPath(), "grand-sluggers-hazards-" + Guid.NewGuid().ToString("N"));
            _shipped = Path.Combine(temp, "data");
            Copy(source, _shipped);
            if (trial)
            {
                _overlay = Path.Combine(temp, "trials", "c80");
                Copy(Path.Combine(repo, "trials", "c80"), _overlay);
            }
        }

        public DataRoot Root() => _overlay is null ? new DataRoot(_shipped) : new DataRoot(_shipped, _overlay);

        /// <summary>Change the hazard library. On a trial fixture the overlay's copy is the one that is read.</summary>
        public void Hazards(Action<JsonObject> change) =>
            Edit(Path.Combine(_overlay ?? _shipped, "rules", "hazards.json"), change);

        /// <summary>Change a park. On a trial fixture the overlay's copy is the one that is read.</summary>
        public void Park(string id, Action<JsonObject> change) =>
            Edit(Path.Combine(_overlay ?? _shipped, "parks", id + ".json"), change);

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
