using System.Reflection;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The park's air (§0.3 D21, FD-03; §6.1, §16): a match plays on one resolved table,
/// <c>content.Rules.AtLevel(difficulty).AtPark(park)</c>, and a park is Harbor plus the differences it
/// names. Today <b>no park names any</b>, on either root, so every one of them resolves to the global
/// table itself — that is <c>SF-01</c>, and it is why this PR changes no behavior.
///
/// <para>
/// The lever is proved on a park built in this file and never shipped (<c>SF-10</c>): thicker air is a
/// shorter carry off the same launch, and a park the wind does not reach flies the still-air path. The
/// last rows are the audit the map asked for (§5, finding 3): a reader that quietly fell back to
/// <see cref="Rules.Default"/> would fly Harbor's air in another park with no test failing, so the flight
/// is run with a table whose drag is not the process's and asked which one it used.
/// </para>
///
/// <para>
/// Tagged <c>Rows=compact</c>: every row here holds on the shipped root and on <c>trials/c80</c>, and
/// CI plays them a second time on the copy. The two-root rows do not wait for that run — they load the
/// overlay by hand as well, the way <see cref="CompactGeometryTests"/> does.
/// </para>
/// </summary>
[Trait("Rows", "compact")]
public sealed class ParkEnvironmentTests
{
    static readonly ContentCatalog Catalog = ContentCatalog.Load();
    static readonly DataRoot TrialRoot =
        new(Catalog.Root.Shipped, Path.GetFullPath(Path.Combine(Catalog.Root.Shipped, "..", "trials", "c80")));
    static readonly ContentCatalog Trial = ContentCatalog.Load(TrialRoot);

    /// <summary>A park that exists only here. Harbor's posts, so the only thing that can move a flight is the air.</summary>
    static Park Air(ParkEnvironment? environment, double windMph = 0, double windDeg = 0) =>
        new("fixture-air", "Fixture Air", "harbor", "grass", 330, 400, 330, windMph,
            Array.Empty<Hazard>(), windDeg, 12, 1.0, environment);

    // ---------------------------------------------------------------------------------
    // SF-01 — Harbor is the default
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-01</c>, both roots. Every shipped and trial park resolves to the global table <em>itself</em> —
    /// the same reference, so there is nothing to compare field by field and nothing that can drift.
    /// </summary>
    [Fact]
    public void SF01_EveryParksResolvedTableIsTheGlobalTableOnBothRoots()
    {
        foreach (var catalog in new[] { Catalog, Trial })
        {
            Assert.Equal(6, catalog.Parks.Count);
            foreach (var park in catalog.Parks.Values)
            {
                Assert.Null(park.Environment);
                Assert.Same(catalog.Rules, catalog.Rules.AtPark(park));
                Assert.Same(catalog.Rules, catalog.Rules.AtLevel(null).AtPark(park));
                // The rung is still the rung: a park resolves after it and takes the table it is given.
                var hard = catalog.Rules.AtLevel("hard");
                Assert.Same(hard, hard.AtPark(park));
            }
        }
    }

    /// <summary>The whole derivation, from the outside: the match Harbor plays is the catalog's table, not a copy of it.</summary>
    [Fact]
    public void SF01_TheMatchAtHarborPlaysTheCatalogsOwnTable()
    {
        var match = Match.Exhibition(Catalog, "rio", "ashlord", innings: 3, seed: 7);
        Assert.Equal("harbor-diamond", match.Park.Id);
        Assert.Same(Catalog.Rules, match.Rules);
        var hard = Match.Exhibition(Catalog, "rio", "ashlord", innings: 3, seed: 7, difficulty: "hard");
        Assert.Same(Catalog.Rules.Flight, hard.Rules.Flight);
        Assert.Equal("hard", hard.Difficulty);
    }

    // ---------------------------------------------------------------------------------
    // SF-10 — the lever, on a fixture park
    // ---------------------------------------------------------------------------------

    /// <summary><c>SF-10</c>: the same fly at two drags. Carry differs; launch, gravity and the stretch do not.</summary>
    [Fact]
    public void SF10_ThickerAirIsAShorterCarryOffTheSameLaunch()
    {
        var global = Catalog.Rules;
        var thick = global.AtPark(Air(new ParkEnvironment(DragMul: 2.0)));

        Assert.Equal(global.Flight.Drag * 2.0, thick.Flight.Drag, 12);
        Assert.Equal(global.Flight.Gravity, thick.Flight.Gravity);
        Assert.Equal(global.Flight.TimeScale, thick.Flight.TimeScale);
        Assert.Equal(global.Flight.LinerTimeScale, thick.Flight.LinerTimeScale);
        Assert.Equal(global.Flight.WindMul, thick.Flight.WindMul);

        var open = BallFlight.Trajectory(95, 28, 0, global);
        var heavy = BallFlight.Trajectory(95, 28, 0, thick);
        Assert.True(BallFlight.FirstLandingDist(heavy) < BallFlight.FirstLandingDist(open) - 10,
            $"thicker air should cost carry: {BallFlight.FirstLandingDist(open):0.0} -> {BallFlight.FirstLandingDist(heavy):0.0} ft");

        // The same swing left the bat: same plate, same instant, the same 28° off it.
        Assert.Equal(open[0].T, heavy[0].T);
        Assert.Equal(open[0].Height, heavy[0].Height);
        Assert.InRange(LaunchOf(open), 27.7, 28.0);
        Assert.Equal(LaunchOf(open), LaunchOf(heavy), 1);
    }

    /// <summary><c>SF-10</c>, the other field: a park the wind does not reach flies the still-air path exactly.</summary>
    [Fact]
    public void SF10_AParkAtWindMulZeroFliesTheStillAirPath()
    {
        var global = Catalog.Rules;
        var windy = Air(null, windMph: 12, windDeg: 0);
        var calm = Air(null);
        var sheltered = Air(new ParkEnvironment(WindMul: 0), windMph: 12, windDeg: 0);

        var blown = Landing(windy, global.AtPark(windy));
        var still = Landing(calm, global.AtPark(calm));
        var inside = Landing(sheltered, global.AtPark(sheltered));

        Assert.True(blown > still + 5, $"the fixture: 12 mph out has to move the ball ({still:0.0} -> {blown:0.0} ft)");
        Assert.Equal(still, inside, 12);
        Assert.Equal(0.0, global.AtPark(sheltered).Flight.WindMul);
        // The flag still reads what the park says; only what the ball feels of it changed.
        Assert.Equal(12.0, sheltered.WindMph);
    }

    /// <summary>A park may name one field and leave the other global: absent is the global number, never a zero.</summary>
    [Fact]
    public void OneNamedFieldLeavesTheOtherGlobal()
    {
        var global = Catalog.Rules;
        var dragOnly = global.AtPark(Air(new ParkEnvironment(DragMul: 1.5)));
        Assert.Equal(global.Flight.WindMul, dragOnly.Flight.WindMul);

        var windOnly = global.AtPark(Air(new ParkEnvironment(WindMul: 0.2)));
        Assert.Equal(global.Flight.Drag, windOnly.Flight.Drag);
        Assert.Equal(0.2, windOnly.Flight.WindMul);

        // An empty block is not an override: the park plays the global table itself.
        Assert.Same(global, global.AtPark(Air(new ParkEnvironment())));
    }

    /// <summary>
    /// The copy is a copy. Every number the park did not name is the global one and every block is the
    /// global block by reference — proved by marking each field of a flight table with a value of its own
    /// and reading them back through <see cref="RulesTable.AtPark"/>, so a field added tomorrow and
    /// forgotten in the copy fails here instead of quietly flying a default.
    /// </summary>
    [Fact]
    public void TheParkedFlightKeepsEveryNumberTheParkDidNotName()
    {
        var marked = new FlightRules();
        var properties = typeof(FlightRules).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var p in properties.Where(p => p.CanWrite))
        {
            if (p.PropertyType == typeof(double)) p.SetValue(marked, (double)p.GetValue(marked)! + 7.5);
            else if (p.PropertyType == typeof(int)) p.SetValue(marked, (int)p.GetValue(marked)! + 3);
        }

        var environment = new ParkEnvironment(DragMul: 2.0, WindMul: 0.25);
        var parked = new RulesTable { Flight = marked }.AtPark(Air(environment)).Flight;

        Assert.Equal(marked.Drag * 2.0, parked.Drag, 12);
        Assert.Equal(0.25, parked.WindMul);
        foreach (var p in properties)
        {
            if (p.Name is nameof(FlightRules.Drag) or nameof(FlightRules.WindMul)) continue;
            if (p.PropertyType.IsValueType)
                Assert.Equal(p.GetValue(marked), p.GetValue(parked));
            else
                Assert.Same(p.GetValue(marked), p.GetValue(parked));
        }
    }

    /// <summary>
    /// A park copies <em>one</em> section. Every other section of the resolved table is the global one by
    /// reference, walked by reflection so a section added tomorrow and forgotten in <see cref="RulesTable.AtPark"/>
    /// fails here rather than silently reverting to its code defaults in every park that names air. (It has
    /// already happened once: `boundary` arrived with F2-a while this branch was open.)
    /// </summary>
    [Fact]
    public void AParkCopiesTheFlightAndSharesEveryOtherSection()
    {
        var global = Catalog.Rules;
        var parked = global.AtPark(Air(new ParkEnvironment(DragMul: 1.5)));
        Assert.NotSame(global, parked);

        var sections = typeof(RulesTable).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.PropertyType.IsClass && p.PropertyType != typeof(string))
            .ToArray();
        Assert.Contains(sections, p => p.Name == nameof(RulesTable.Flight));
        Assert.True(sections.Length >= 11, $"a section went missing from the walk: {sections.Length}");
        foreach (var section in sections)
        {
            if (section.Name == nameof(RulesTable.Flight))
                Assert.NotSame(section.GetValue(global), section.GetValue(parked));
            else
                Assert.Same(section.GetValue(global), section.GetValue(parked));
        }
    }

    // ---------------------------------------------------------------------------------
    // The fallback audit (map §5, finding 3)
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The flight flies the table it is handed. Each entry is run twice — once with the process-wide
    /// default and once with a table whose drag is not the default's — and has to tell them apart. A
    /// reader that fell back to <see cref="Rules.Default"/> would return the same number twice.
    /// </summary>
    [Fact]
    public void TheFlightReadsTheTableItIsHandedAndNotTheProcessDefault()
    {
        var park = Air(new ParkEnvironment(DragMul: 2.0));
        var fixture = Rules.Default.AtPark(park);
        Assert.NotEqual(Rules.Default.Flight.Drag, fixture.Flight.Drag);

        // BallFlight.Trajectory, open field and clipped in a park.
        Assert.True(BallFlight.CarryFeet(95, 28, 0, fixture) < BallFlight.CarryFeet(95, 28, 0) - 10);
        Assert.True(Landing(park, fixture) < Landing(park, Rules.Default) - 10);

        // BattedBall.Of — the one flight the resolver, the preview and the ring share.
        var thrown = BattedBall.Of(95, 28, 0, park, fixture);
        var light = BattedBall.Of(95, 28, 0, park);
        Assert.True(thrown.LandingDist < light.LandingDist - 10,
            $"the batted ball has to use the handed table: {light.LandingDist:0.0} -> {thrown.LandingDist:0.0} ft");

        // BallFlight.Continue — a deflected ball is a batted ball still (#721).
        var path = BallFlight.Trajectory(95, 12, 0, park, Rules.Default);
        var t0 = BallFlight.HangTime(path, Rules.Default) + 0.10;
        var (x, y, z) = BallFlight.PointAt(path, t0, Rules.Default);
        var heavy = BallFlight.Continue(path, t0, x, y, z, 0, 40, 70, 12, 95, park, fixture);
        var floaty = BallFlight.Continue(path, t0, x, y, z, 0, 40, 70, 12, 95, park, Rules.Default);
        Assert.True(heavy[^1].Dist < floaty[^1].Dist - 1,
            $"the continued ball has to use the handed table: {floaty[^1].Dist:0.0} -> {heavy[^1].Dist:0.0} ft");
    }

    /// <summary>A match on a park that names air plays that air, and shares every other block with the catalog.</summary>
    [Fact]
    public void AMatchOnAParkThatNamesAirPlaysThatAir()
    {
        var park = Air(new ParkEnvironment(DragMul: 1.5, WindMul: 0.1));
        var (home, away) = PresetTeams.Pair(Catalog, "rio", "ashlord");
        var match = new Match(Catalog, away, home, park, innings: 3, seed: 7);

        Assert.Equal(Catalog.Rules.Flight.Drag * 1.5, match.Rules.Flight.Drag, 12);
        Assert.Equal(0.1, match.Rules.Flight.WindMul);
        Assert.Same(Catalog.Rules.Flight.Bounce, match.Rules.Flight.Bounce);
        Assert.Same(Catalog.Rules.Flight.Roll, match.Rules.Flight.Roll);
        Assert.Same(Catalog.Rules.Flight.Wall, match.Rules.Flight.Wall);
        Assert.Same(Catalog.Rules.Fielding, match.Rules.Fielding);
        Assert.Same(Catalog.Rules.Running, match.Rules.Running);
        Assert.Same(Catalog.Rules.Infield, match.Rules.Infield);
    }

    /// <summary>
    /// The park's air is part of what a trace says it ran on: the member is in
    /// <see cref="PlayTraceIdentity"/> like every other field of the park, so evidence taken in one park's
    /// air can never be read as another's.
    /// </summary>
    [Fact]
    public void TheParksAirIsInTheTraceIdentity()
    {
        var (home, away) = PresetTeams.Pair(Catalog, "rio", "ashlord");
        var plain = new Match(Catalog, away, home, Air(null), innings: 3, seed: 7);
        var thick = new Match(Catalog, away, home, Air(new ParkEnvironment(DragMul: 2.0)), innings: 3, seed: 7);
        Assert.NotEqual(PlayTraceIdentity.Capture(plain).Sha256, PlayTraceIdentity.Capture(thick).Sha256);
        Assert.Contains("\"dragMul\":2", PlayTraceIdentity.Capture(thick).InputsJson);

        // And the parity side of the same fact: a park that names no air writes no air, so an identity
        // taken at Harbor is the string it has always been and no stored SHA moves (the trace options
        // omit a null). This row is why this PR regenerates no evidence dataset.
        foreach (var park in Catalog.Parks.Values)
        {
            var match = new Match(Catalog, away, home, park, innings: 3, seed: 7);
            Assert.DoesNotContain("environment", PlayTraceIdentity.Capture(match).InputsJson, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ---------------------------------------------------------------------------------
    // The schema
    // ---------------------------------------------------------------------------------

    /// <summary>A number the flight cannot fly is refused by name, before any catalog is built.</summary>
    [Theory]
    [InlineData(0.0, "environment.dragMul must be greater than 0 and at most 4; got 0")]
    [InlineData(-1.5, "environment.dragMul must be greater than 0 and at most 4; got -1.5")]
    [InlineData(9.0, "environment.dragMul must be greater than 0 and at most 4; got 9")]
    public void AnImpossibleDragMultiplierIsRefused(double dragMul, string expected)
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("parks/harbor-diamond.json",
            json => json["environment"] = new JsonObject { ["dragMul"] = dragMul });

        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains($"park 'harbor-diamond' {expected}", StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root));
    }

    /// <summary>Wind exposure is a fraction of the flag: outside [0, 1] there is no ball it describes.</summary>
    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.4)]
    public void AWindExposureOutsideTheFlagIsRefused(double windMul)
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("parks/harbor-diamond.json",
            json => json["environment"] = new JsonObject { ["windMul"] = windMul });

        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("park 'harbor-diamond' environment.windMul must be between 0 and 1", StringComparison.Ordinal));
    }

    /// <summary>
    /// And the way through: a park file that names air loads, carries it, and the match built on that park
    /// flies it. The block is read from the file, not from a fixture record this suite built by hand.
    /// </summary>
    [Fact]
    public void AParkFileMayNameItsAirAndTheMatchFliesIt()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("parks/harbor-diamond.json",
            json => json["environment"] = new JsonObject { ["dragMul"] = 1.25, ["windMul"] = 0.5 });

        Assert.Empty(ContentDataValidator.Validate(fixture.Root));
        var content = ContentCatalog.Load(fixture.Root);
        var park = content.Parks["harbor-diamond"];
        Assert.Equal(1.25, park.Environment!.DragMul);
        Assert.Equal(0.5, park.Environment.WindMul);

        var match = Match.Exhibition(content, "rio", "ashlord", innings: 3, seed: 7);
        Assert.Equal(content.Rules.Flight.Drag * 1.25, match.Rules.Flight.Drag, 12);
        Assert.Equal(0.5, match.Rules.Flight.WindMul);
        Assert.NotSame(content.Rules, match.Rules);
    }

    // ---------------------------------------------------------------------------------

    static double Landing(Park park, RulesTable rules) =>
        BallFlight.FirstLandingDist(BallFlight.Trajectory(95, 28, 0, park, rules));

    /// <summary>The angle the ball left the plate at, read off the first step of the path.</summary>
    static double LaunchOf(IReadOnlyList<Sample> samples)
    {
        var a = samples[0];
        var b = samples[1];
        return Math.Atan2(b.Height - a.Height, b.Dist - a.Dist) * 180.0 / Math.PI;
    }
}
