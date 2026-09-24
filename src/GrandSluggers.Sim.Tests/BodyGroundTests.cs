using System.Security.Cryptography;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// F3-d (#857; FD-04 B, FD-05, FR-02 / FR-06 / FR-16; spec §8, §9, §16, <c>SF-13</c>): the ground acts on a body in a small
/// way. Every ground row carries a <c>body</c> block — <c>startMul</c>, <c>brakeMul</c>, <c>cutMul</c> through the §8
/// response law, <c>slideMul</c> and <c>overrunMul</c> on the runner — each scaling a time or a length, all 1.0 in the data.
///
/// <para>
/// <b>The fixture rows here are not shipped numbers.</b> The data root runs the response law (<c>accelSec</c> 0.2 /
/// <c>brakeSec</c> 0.1) with every row at 1.0. <see cref="Roots"/> copies it and gives the <c>ice</c> row fixture
/// multipliers; the parks
/// are Harbor as authored ("plain": dirt and grass, both 1.0) and Harbor with every zone named ice ("iced"). The same body,
/// the same stick, the same ball on the two parks is the <c>SF-13</c> comparison. Every root plays on the one diamond
/// <see cref="Diamond"/> reads in this process.
/// </para>
///
/// <para>
/// <b>Parity was written first.</b> The three <c>AtOne…</c> rows were committed against the code before the block existed,
/// with the values that code produced; the change had to keep them. The values were re-recorded when the compact profile
/// became the data (the diamond and the chase moved under them); the relation — a 1.0 zone map is the unzoned path — is the
/// one F3-d proved. Each value is reached by <c>+ − × ÷</c> and <c>sqrt</c>
/// alone — the stick glove's want is the raw stick, the route runs on a synthetic roller, the runner on the running table —
/// so the golden pins no libm (protocol <c>stored-double-pins-the-platform</c>).
/// </para>
/// </summary>
public sealed class BodyGroundTests : IClassFixture<BodyGroundTests.Roots>
{
    const double Frame = 1.0 / 60.0;
    readonly Roots _roots;

    public BodyGroundTests(Roots roots) => _roots = roots;

    static readonly ContentCatalog Game = ContentCatalog.Load(new DataRoot(Shipped.Content.Root.Shipped));

    /// <summary>The fixture ice row. Distinct values, so a reader that took the wrong multiplier fails by name.</summary>
    static readonly (double Start, double Brake, double Cut, double Slide, double Overrun) Slick = (1.5, 2.0, 2.5, 1.5, 2.0);

    /// <summary>
    /// The stick glove's script: one neutral frame, which arms the seat's radial stick (a seat steers only after it has seen
    /// the stick at rest once), then +X from rest (through the reaction lockout), a 90° cut to −Z, then a reversal to +Z.
    /// </summary>
    static readonly (int Frames, double X, double Y)[] Script = [(1, 0, 0), (99, 1, 0), (60, 0, -1), (45, 0, 1)];

    const int CutFrom = 100;
    const int ReverseFrom = 160;

    static Park Plain(ContentCatalog catalog) => catalog.Parks[ParkId.Harbor];

    /// <summary>
    /// Every zone on a shipped row still at 1.0 (ash; ice is Crystal's own since F9-a): the park the <c>AtOne</c> rows walk,
    /// so a row at 1.0 still plays the pre-change bits wherever it is laid.
    /// </summary>
    static Park AtOneGround(ContentCatalog catalog) => Plain(catalog) with
    {
        Zones = new ParkZones(InfieldDirt: Ground.Ash, Outfield: Ground.Ash, WarningTrack: Ground.Ash, FoulApron: Ground.Ash)
    };

    static Park Iced(ContentCatalog catalog) => Plain(catalog) with
    {
        Zones = new ParkZones(InfieldDirt: Ground.Ice, Outfield: Ground.Ice, WarningTrack: Ground.Ice, FoulApron: Ground.Ice)
    };

    // ---------------------------------------------------------------------------------
    // The table
    // ---------------------------------------------------------------------------------

    /// <summary>Every row names the block, every multiplier is 1.0, and the law those rows scale is on in the data.</summary>
    [Fact]
    public void EveryGroundRowCarriesTheBodyBlockAtOne()
    {
        // Ice is Crystal's own row since F9-a (GroundLibraryTests.F9A_TheIceRowIsCrystalsNumbersFieldForField).
        foreach (var id in Game.Rules.Grounds.Ids.Where(id => id != Ground.Ice))
        {
            var body = Game.Rules.Grounds.Of(id).Body;
            Assert.Equal((1.0, 1.0, 1.0, 1.0, 1.0), (body.StartMul, body.BrakeMul, body.CutMul, body.SlideMul, body.OverrunMul));
        }
        Assert.Equal((0.2, 0.1), (Game.Rules.Fielding.Chase.AccelSec, Game.Rules.Fielding.Chase.BrakeSec));
    }

    /// <summary>The multipliers are range-checked like every rule: 0 or less is refused by name, against the file.</summary>
    [Theory]
    [InlineData("grass", "startMul", 0)]
    [InlineData("dirt", "brakeMul", -1)]
    [InlineData("ice", "cutMul", 0)]
    [InlineData("ash", "slideMul", 0)]
    [InlineData("ice", "overrunMul", -0.5)]
    public void AMultiplierAtOrBelowZeroIsRefusedByName(string row, string field, double value)
    {
        var root = _roots.Fresh(grounds => grounds[row]!["body"]![field] = value);
        Assert.Contains(RulesTable.Validate(new DataRoot(root)), e =>
            e.Contains($"grounds.{row}.body.{field} must be greater than 0", StringComparison.Ordinal)
            && e.Contains(Path.Combine(root, RulesTable.Directory, "grounds.json"), StringComparison.Ordinal));
    }

    /// <summary>A key the block does not declare is a typo, not a silent default (the strict schema reaches inside a row).</summary>
    [Fact]
    public void AnUnknownKeyInTheBodyBlockIsRefused()
    {
        var root = _roots.Fresh(grounds => grounds["ice"]!["body"]!["driftMul"] = 1.0);
        Assert.Contains(RulesTable.Validate(new DataRoot(root)), e =>
            e.Contains("grounds.ice.body.driftMul is not a rule this table owns", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------
    // SF-13 — the same body on two grounds
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-13</c>: the same centre fielder, asked the same velocity from rest by the same stick, on the plain park and on the
    /// iced one. The start takes <c>accelSec × startMul</c>, the stop (the brake half of the reversal) <c>brakeSec ×
    /// brakeMul</c>, and the 90° cut longer on the slick row; the top speed and the heading the body settles on after the cut
    /// and after the reversal are the same on both — the body goes where the stick points, it only answers slower.
    /// </summary>
    [Fact]
    public void SF13_StartBrakeAndCutBackDifferOnTwoGrounds()
    {
        var catalog = _roots.Slick;
        var chase = catalog.Rules.Fielding.Chase;
        var plain = Velocities(StickRun(catalog, Plain(catalog), Script, out var rated));
        var iced = Velocities(StickRun(catalog, Iced(catalog), Script, out var ratedOnIce));
        Assert.Equal(rated, ratedOnIce);

        var top = Speed(plain[CutFrom]);
        Assert.True(top > 5, $"the body is at speed before the cut ({top:0.00} ft/s)");
        Assert.Equal(top, Speed(iced[CutFrom]), 9);
        // No frame is faster than the asked speed, on either ground (the ground never adds speed).
        Assert.All(plain.Concat(iced), v => Assert.True(Speed(v) <= top * (1 + 1e-9), $"{Speed(v)} over {top}"));
        // The rates are measured against the rated speed (§8, #718); an outfielder under a fly is asked a fraction of it.
        double Takes(double sec, double mul) => sec * mul * top / rated;

        // The start: rest to the asked speed.
        var startPlain = FramesToReach(plain, 0, v => v.X >= top * (1 - 1e-9));
        var startIced = FramesToReach(iced, 0, v => v.X >= top * (1 - 1e-9));
        Assert.InRange(startPlain * Frame, Takes(chase.AccelSec, 1), Takes(chase.AccelSec, 1) + Frame);
        Assert.InRange(startIced * Frame, Takes(chase.AccelSec, Slick.Start), Takes(chase.AccelSec, Slick.Start) + Frame);
        Assert.True(startIced > startPlain);

        // The cut-back: +X at speed, the stick to −Z. The same heading and speed at the end on both; later on the slick row.
        var cutPlain = FramesToReach(plain, CutFrom, v => Near(v, (0, -top)));
        var cutIced = FramesToReach(iced, CutFrom, v => Near(v, (0, -top)));
        Assert.True(cutIced > cutPlain, $"the cut on ice ({cutIced} frames) is slower than on dirt and grass ({cutPlain})");
        Assert.True(Near(plain[ReverseFrom], (0, -top)) && Near(iced[ReverseFrom], (0, -top)), "both finished the cut on −Z at speed");

        // The stop: the reversal's brake half, −Z at speed until the component along the old heading has died.
        var stopPlain = FramesToReach(plain, ReverseFrom, v => v.Z >= 0);
        var stopIced = FramesToReach(iced, ReverseFrom, v => v.Z >= 0);
        Assert.InRange(stopPlain * Frame, Takes(chase.BrakeSec, 1), Takes(chase.BrakeSec, 1) + Frame);
        Assert.InRange(stopIced * Frame, Takes(chase.BrakeSec, Slick.Brake), Takes(chase.BrakeSec, Slick.Brake) + Frame);
        Assert.True(stopIced > stopPlain);

        // And the body ends where the stick points, at the same speed, on both grounds.
        Assert.True(Near(plain[^1], (0, top)) && Near(iced[^1], (0, top)), "both run +Z at the asked speed at the end");
    }

    /// <summary>
    /// Each multiplier moves its own part of the law and nothing else. On the fixture ash row only <c>cutMul</c> is off 1.0: the
    /// start and the stop are the plain park's frame for frame, the 90° cut alone is slower, and the body still ends each leg on
    /// the stick's heading at the asked speed.
    /// </summary>
    [Fact]
    public void SF13_TheCutBackIsItsOwnTime()
    {
        var catalog = _roots.Slick;
        var ashen = Plain(catalog) with
        {
            Zones = new ParkZones(InfieldDirt: Ground.Ash, Outfield: Ground.Ash, WarningTrack: Ground.Ash, FoulApron: Ground.Ash)
        };
        var plain = Velocities(StickRun(catalog, Plain(catalog), Script));
        var cutOnly = Velocities(StickRun(catalog, ashen, Script));
        var top = Speed(plain[CutFrom]);
        Assert.Equal(plain.Take(CutFrom + 1), cutOnly.Take(CutFrom + 1));
        Assert.True(FramesToReach(cutOnly, CutFrom, v => Near(v, (0, -top))) > FramesToReach(plain, CutFrom, v => Near(v, (0, -top))));
        Assert.True(Near(cutOnly[ReverseFrom], (0, -top)));
        // The reversal is a brake along the heading and then the ramp: nothing across it, so the cut-back does not enter.
        Assert.Equal(FramesToReach(plain, ReverseFrom, v => v.Z >= 0), FramesToReach(cutOnly, ReverseFrom, v => v.Z >= 0));
        Assert.Equal(FramesToReach(plain, ReverseFrom, v => Near(v, (0, top))), FramesToReach(cutOnly, ReverseFrom, v => Near(v, (0, top))));
    }

    /// <summary>
    /// The planner charges the ramp of the ground the body starts on (FD-04 B): half of <c>accelSec × startMul</c>, so the plan
    /// and the body agree. The same roller and the same shortstop: on the iced infield the route pays the longer ramp.
    /// </summary>
    [Fact]
    public void SF13_ThePlannerChargesTheRampOfTheGroundTheBodyStartsOn()
    {
        var rules = _roots.Slick.Rules;
        var plain = PlanRoute(rules, Plain(_roots.Slick));
        var iced = PlanRoute(rules, Iced(_roots.Slick));
        Assert.Equal(rules.Fielding.Chase.AccelSec / 2, plain.RampSec);
        Assert.Equal(rules.Fielding.Chase.AccelSec * Slick.Start / 2, iced.RampSec);
        Assert.True(plain.Reachable && iced.Reachable);
        Assert.True(iced.MeetTimeSec >= plain.MeetTimeSec);
        Assert.True(iced.TravelTimeSec > plain.TravelTimeSec);

        // The start zone is what is read: a park whose only ice is the outfield leaves the infield body's ramp alone.
        var outfieldIce = Plain(_roots.Slick) with { Zones = new ParkZones(Outfield: Ground.Ice) };
        Assert.Equal(plain, PlanRoute(rules, outfieldIce));
    }

    /// <summary>
    /// <c>SF-13</c>: S-31's ball (118 ft, 4°, −18° to the shortstop, cinder at Run 5) on the slick row is still the routine
    /// out at first. The ground did act — the shortstop's path differs frame by frame — and the out stands, because the effect
    /// is small (FD-04 B: the same routine grounder is a routine out in every park).
    /// </summary>
    [Fact]
    public void SF13_TheRoutineGrounderIsStillAnOut()
    {
        var catalog = _roots.Slick;
        var (plainPlay, plainTrack) = RoutineGrounder(catalog, Plain(catalog));
        var (icedPlay, icedTrack) = RoutineGrounder(catalog, Iced(catalog));
        foreach (var play in new[] { plainPlay, icedPlay })
        {
            Assert.Equal(PlayKind.GroundOut, play.Kind);
            var only = Assert.Single(play.Outcome!.OutsMade);
            Assert.Equal((OutType.ThrowOutAtFirst, 1, 0), (only.Type, only.Bag, only.FromBag));
        }
        Assert.False(plainTrack.SequenceEqual(icedTrack), "the slick row changed the shortstop's steps");
    }

    /// <summary>
    /// The slide and the overrun are lengths at a bag, read from the row of the zone the bag stands in (§9.4). They are not
    /// behind the response law: into every bag the slide starts <c>slideFt × slideMul</c> out,
    /// past first the batter-runner carries <c>overrunFt × overrunMul</c>. A park whose only ice is the outfield leaves the
    /// bags on dirt, so it is the bag's zone that is read, not the park's surface.
    /// </summary>
    [Fact]
    public void SF13_TheSlideAndTheOverrunFollowTheRowOfTheBagsZone()
    {
        var catalog = _roots.Slick;
        var rules = catalog.Rules;
        var bags = rules.Running.Bags;
        var plain = GroundZones.Of(Plain(catalog), rules);
        var iced = GroundZones.Of(Iced(catalog), rules);
        var outfieldIce = GroundZones.Of(Plain(catalog) with { Zones = new ParkZones(Outfield: Ground.Ice) }, rules);
        for (var bag = 1; bag <= 4; bag++)
        {
            Assert.Equal(bags.SlideFt, RunnerSystem.SlideFt(bag, plain, rules));
            Assert.Equal(bags.SlideFt * Slick.Slide, RunnerSystem.SlideFt(bag, iced, rules));
            Assert.Equal(bags.SlideFt, RunnerSystem.SlideFt(bag, outfieldIce, rules));
            Assert.Equal(bags.SlideFt, RunnerSystem.SlideFt(bag, null, rules));
        }
        Assert.Equal(bags.OverrunFt, RunnerSystem.OverrunFt(1, plain, rules));
        Assert.Equal(bags.OverrunFt * Slick.Overrun, RunnerSystem.OverrunFt(1, iced, rules));
        Assert.Equal(bags.OverrunFt, RunnerSystem.OverrunFt(1, outfieldIce, rules));

        foreach (var zones in new[] { plain, iced })
        {
            // Through first: out to the bag's overrun length exactly, then straight back.
            var batter = Runner.BatterRunner(catalog.Must("rio"), HomeSet.BatterX, HomeSet.BatterZ);
            var t = 0.0;
            var farthest = 0.0;
            for (var i = 0; i < 400; i++)
            {
                RunnerSystem.Tick([batter], Frame, Context(t += Frame, _ => false, zones), rules);
                farthest = Math.Max(farthest, batter.OverrunFt);
            }
            Assert.Equal(RunnerSystem.OverrunFt(1, zones, rules), farthest);
            Assert.True(batter.IsOn(1) && !batter.Overrunning);

            // Into second with a tag threat: the slide starts at the first frame inside the bag's slide length.
            var runner = new Runner(catalog.Must("vale"), 1);
            runner.BeginPlay(forced: false, tagAndGo: false);
            runner.Send(2);
            var slide = RunnerSystem.SlideFt(2, zones, rules);
            var before = runner.SegmentFt;
            var began = -1.0;
            t = 0;
            for (var i = 0; i < 400 && began < 0; i++)
            {
                RunnerSystem.Tick([runner], Frame, Context(t += Frame, bag => bag == 2, zones), rules);
                if (runner.Phase == RunnerPhase.Sliding) began = runner.SegmentFt - runner.Feet;
                else before = runner.SegmentFt - runner.Feet;
            }
            Assert.InRange(began, 0, slide);
            Assert.True(before > slide, $"the frame before the slide was outside it ({before:0.00} ft against {slide})");
        }
    }

    /// <summary>
    /// The player's slide (§9.4: West or South near the bag forces it) reads the same length as the automatic one. The batter-runner
    /// on an infield single, the press held only while first is between the plain slide length and the slick one: on the iced
    /// park the press is inside the bag's slide and he goes down into the bag; on the plain park it is outside it, so nothing
    /// is forced and he runs through.
    /// </summary>
    [Fact]
    public void SF13_ThePlayersSlideReadsTheBagsZoneToo()
    {
        var catalog = _roots.Slick;
        var bags = catalog.Rules.Running.Bags;
        foreach (var (park, slid) in new[] { (Plain(catalog), false), (Iced(catalog), true) })
        {
            var match = Defense(catalog, park, leadoff: "dart");
            var hit = FlightFixtures.Hit(match.Park, 85, -12, -18, rules: match.Rules);
            var preview = match.PreviewHit(hit);
            var seats = new LiveSeats(HumanBats: true, HumanPitches: false, PlayerMustField: false, Versus: false);
            var live = match.LivePlay;
            Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, seats, 0, LivePlayCommandSource.Human)).Snapshot.Active);
            var batter = Assert.Single(match.Runners, r => r.IsBatter);
            var pressed = false;
            var wentDown = false;
            for (var i = 0; i < 60 * 20 && live.Active && batter.Live; i++)
            {
                var feet = batter.Bag == 0 ? batter.FeetTo(1) : double.PositiveInfinity;
                var press = feet > bags.SlideFt && feet <= bags.SlideFt * Slick.Slide;
                pressed |= press;
                live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, press ? new LivePadInput(WestDown: true) : LivePadInput.Dead, false, LivePlayCommandSource.Human));
                wentDown |= batter.Phase == RunnerPhase.Sliding;
                if (batter.Bag >= 1) break;
            }
            Assert.True(pressed, "the press band was crossed");
            Assert.Equal(slid, wentDown);
            Assert.Equal(!slid, batter.Overrunning);
        }
    }

    // ---------------------------------------------------------------------------------
    // Parity — every multiplier at 1.0 is the code before the block existed, bit for bit
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The stick glove's path, frame by frame, on the data with every row at 1.0. The
    /// relation is F3-d's: a zone map that names every zone a 1.0 row is the unzoned path.
    /// </summary>
    [Fact]
    public void AtOneTheStickGlovesStepIsThePreChangeBits()
    {
        Assert.Equal("048730fc3eb307df268818a40fa5102d45907d447632957fcdee82518c4c3fad",
            Hash(StickRun(Game, Plain(Game), Script).SelectMany(p => new[] { p.X, p.Z })));
        // A zone map that names every zone a 1.0 row is the same path: the read is there, the product is exact.
        Assert.Equal("048730fc3eb307df268818a40fa5102d45907d447632957fcdee82518c4c3fad",
            Hash(StickRun(Game, AtOneGround(Game), Script).SelectMany(p => new[] { p.X, p.Z })));
    }

    /// <summary>The planner's route on a synthetic roller: the pre-F3-d values on the data.</summary>
    [Fact]
    public void AtOneThePlannersRouteIsThePreChangeRoute()
    {
        var ramped = new FieldingPursuit.Route(-26.081249999999997, 120.85624999999999, 1.3, 16.172964033379905, 18, 1.05, true, false, 0.1);
        Assert.Equal(ramped, PlanRoute(Game.Rules, Plain(Game)));
        Assert.Equal(ramped, PlanRoute(Game.Rules, AtOneGround(Game)));
    }

    /// <summary>
    /// The runner through first and into second under a tag threat on the 80-ft diamond: one path, with no park named, with
    /// Harbor's zones and with every zone iced at 1.0 (every bag on a 1.0 row).
    /// </summary>
    [Fact]
    public void AtOneTheRunnersPathIsThePreChangePath()
    {
        const string expected = "f8fe94f919d63c22eba1332523b2f52b6b907b46f155741d78b08beb4d21692f";
        var catalog = Game;
        Assert.Equal(expected, Hash(RunnerPath(catalog.Rules, null)));
        Assert.Equal(expected, Hash(RunnerPath(catalog.Rules, GroundZones.Of(Plain(catalog), catalog.Rules))));
        Assert.Equal(expected, Hash(RunnerPath(catalog.Rules, GroundZones.Of(AtOneGround(catalog), catalog.Rules))));
    }

    // ---------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------

    static RunnerTickContext Context(double elapsed, Func<int, bool> tagThreat, GroundZones? zones) =>
        new(elapsed, 0, FlyState.None, 0, _ => false, tagThreat, zones);

    /// <summary>A human centre fielder on a high fly to left-centre, steered by the script; the glove's position every frame.</summary>
    static List<(double X, double Z)> StickRun(ContentCatalog catalog, Park park, (int Frames, double X, double Y)[] script) =>
        StickRun(catalog, park, script, out _);

    /// <param name="rated">The glove's rated speed, the one the response rates are measured against (§8.1, #718).</param>
    static List<(double X, double Z)> StickRun(ContentCatalog catalog, Park park, (int Frames, double X, double Y)[] script, out double rated)
    {
        var home = catalog.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = catalog.Team("Offense", "zig", "boom", "jester", "grit", "soot", "nugget", "pip", "gull", "marlow");
        var match = new Match(catalog, away, home, park, innings: 3, seed: 1);
        var hit = FlightFixtures.Landing(match.Park, 300, 34, -8, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("CF", preview.Position);
        rated = FieldingResolver.ChaseSpeedFt(preview.Fielder, false, match.Rules);
        var seats = new LiveSeats(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, seats, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        var track = new List<(double X, double Z)> { live.Fielders["CF"] };
        foreach (var (frames, x, y) in script)
            for (var i = 0; i < frames; i++)
            {
                live.Apply(LivePlayCommand.Tick(Frame, new LivePadInput(StickX: x, StickY: y), LivePadInput.Dead, false, LivePlayCommandSource.Human));
                Assert.True(live.Active);
                Assert.Equal("CF", live.GlovePos);
                track.Add(live.Fielders["CF"]);
            }
        return track;
    }

    /// <summary>The frame velocities of a track, <c>v[i]</c> being the step into frame <c>i</c> (so <c>v[0]</c> is rest).</summary>
    static List<(double X, double Z)> Velocities(List<(double X, double Z)> track)
    {
        var v = new List<(double X, double Z)> { (0, 0) };
        for (var i = 1; i < track.Count; i++)
            v.Add(((track[i].X - track[i - 1].X) / Frame, (track[i].Z - track[i - 1].Z) / Frame));
        return v;
    }

    static double Speed((double X, double Z) v) => Math.Sqrt(v.X * v.X + v.Z * v.Z);

    static bool Near((double X, double Z) v, (double X, double Z) want) =>
        Math.Abs(v.X - want.X) <= 1e-6 * Speed(want) && Math.Abs(v.Z - want.Z) <= 1e-6 * Speed(want);

    /// <summary>Frames from the first moving frame at or after <paramref name="from"/> until <paramref name="reached"/> holds.</summary>
    static int FramesToReach(List<(double X, double Z)> v, int from, Func<(double X, double Z), bool> reached)
    {
        var first = from;
        if (from == 0)
            while (first < v.Count && Speed(v[first]) < 1e-9) first++;
        else first = from + 1;
        for (var i = first; i < v.Count; i++)
            if (reached(v[i])) return i - first + 1;
        throw new Xunit.Sdk.XunitException($"never reached from frame {from}");
    }

    /// <summary>S-31's defense and batter (FieldingScenarioTests.Defense) at a named park.</summary>
    static Match Defense(ContentCatalog catalog, Park park, string leadoff)
    {
        var home = catalog.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var rest = new[] { "jester", "soot", "grit", "nugget", "boom", "marlow", "gull", "pip" }.Where(id => id != leadoff).Take(7).ToArray();
        var away = catalog.Team("Offense", "zig", leadoff, rest[0], rest[1], rest[2], rest[3], rest[4], rest[5], rest[6]);
        var match = new Match(catalog, away, home, park, innings: 3, seed: 1);
        Assert.Equal(leadoff, match.Batter.Id);
        Assert.True(match.BeginAtBat(Scenario.Paint, Scenario.Swing, out _, out _), "the scripted swing must put the ball in play");
        return match;
    }

    /// <summary>S-31 played CPU against CPU: the completed play and the shortstop's (or every body's) position each frame.</summary>
    static (PlayEvent Play, List<(double X, double Z)> Track) RoutineGrounder(ContentCatalog catalog, Park park, bool everyBody = false)
    {
        var match = Defense(catalog, park, leadoff: "cinder");
        var hit = FlightFixtures.Hit(match.Park, 85, -12, -18, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, match.ResolveFielding(hit, preview), LiveSeats.CpuOnly)).Snapshot.Active);
        var track = new List<(double X, double Z)>();
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 40 && play is null; i++)
        {
            play = live.Apply(LivePlayCommand.Tick(Frame)).CompletedPlay;
            if (everyBody) track.AddRange(Diamond.Order.Where(live.Fielders.ContainsKey).Select(p => live.Fielders[p]));
            else if (live.Fielders.TryGetValue("SS", out var ss)) track.Add(ss);
        }
        Assert.NotNull(play);
        return (play!, track);
    }

    static FieldingPursuit.Route PlanRoute(RulesTable rules, Park park)
    {
        var path = new List<Sample>();
        for (var i = 0; i <= 80; i++)
        {
            var t = i * 0.05;
            var d = 60 * t - 5 * t * t;
            path.Add(new Sample(t, d, 0, -0.375 * d, 60 + 0.875 * d, Event: i == 1 ? SampleEvent.Ground : SampleEvent.None));
        }
        var who = Game.Must("ashlord");
        var preview = FlightFixtures.Preview(who, "SS", BattedBallClass.Grounder, 0, path[^1].X, path[^1].Z);
        return FieldingPursuit.Plan(preview, park, path, 0.25, -42, 118, 18, rules, readySec: 0.25);
    }

    static List<double> RunnerPath(RulesTable rules, GroundZones? zones)
    {
        var values = new List<double>();
        var batter = Runner.BatterRunner(Game.Must("rio"), HomeSet.BatterX, HomeSet.BatterZ);
        var t = 0.0;
        for (var i = 0; i < 400; i++)
        {
            RunnerSystem.Tick([batter], Frame, Context(t += Frame, _ => false, zones), rules);
            values.Add(batter.Feet);
            values.Add(batter.OverrunFt);
            values.Add((double)batter.Phase);
        }
        var runner = new Runner(Game.Must("vale"), 1);
        runner.BeginPlay(forced: false, tagAndGo: false);
        runner.Send(2);
        t = 0;
        var slid = false;
        for (var i = 0; i < 400; i++)
        {
            RunnerSystem.Tick([runner], Frame, Context(t += Frame, bag => bag == 2, zones), rules);
            slid |= runner.Phase == RunnerPhase.Sliding;
            values.Add(runner.Feet);
            values.Add((double)runner.Phase);
            values.Add(runner.Bag);
        }
        Assert.True(slid, "the path holds a slide");
        return values;
    }

    static string Hash(IEnumerable<double> values) =>
        Convert.ToHexString(SHA256.HashData(values.SelectMany(v => BitConverter.GetBytes(BitConverter.DoubleToInt64Bits(v))).ToArray())).ToLowerInvariant();

    /// <summary>
    /// A throwaway copy of the data root, deleted with the class: the data's chase with the fixture ice row. Nothing under
    /// <c>data/</c> is written.
    /// </summary>
    public sealed class Roots : IDisposable
    {
        readonly List<string> _dirs = [];

        public Roots()
        {
            Slick = ContentCatalog.Load(new DataRoot(Copy(IceRow, _ => { })));
        }

        public ContentCatalog Slick { get; }

        /// <summary>A fresh copy of the data root with one change to <c>grounds.json</c>, for a refusal row.</summary>
        public string Fresh(Action<JsonObject> grounds) => Copy(grounds, _ => { });

        /// <summary>The fixture rows: ice carries all five, ash only the cut-back (so the cut is proved to be its own time).</summary>
        static void IceRow(JsonObject grounds)
        {
            // Isolate body response: both parks must send the same ball past the bodies.
            foreach (var key in new[] { "roll", "bounce", "skid", "overthrow", "bobble" })
                grounds["ice"]![key] = grounds["grass"]![key]!.DeepClone();
            grounds["ice"]!["body"] = new JsonObject
            {
                ["startMul"] = BodyGroundTests.Slick.Start,
                ["brakeMul"] = BodyGroundTests.Slick.Brake,
                ["cutMul"] = BodyGroundTests.Slick.Cut,
                ["slideMul"] = BodyGroundTests.Slick.Slide,
                ["overrunMul"] = BodyGroundTests.Slick.Overrun
            };
            grounds["ash"]!["body"]!["cutMul"] = BodyGroundTests.Slick.Cut;
        }

        string Copy(Action<JsonObject> grounds, Action<JsonObject> fielding)
        {
            var root = Path.Combine(Path.GetTempPath(), "grand-sluggers-body-ground-" + Guid.NewGuid().ToString("N"));
            _dirs.Add(root);
            CopyTree(Shipped.Content.Root.Shipped, root);
            Change(Path.Combine(root, RulesTable.Directory, "grounds.json"), grounds);
            Change(Path.Combine(root, RulesTable.Directory, "fielding.json"), fielding);
            return root;
        }

        static void Change(string path, Action<JsonObject> change)
        {
            var json = JsonNode.Parse(File.ReadAllText(path), null, new System.Text.Json.JsonDocumentOptions
            {
                CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            })!.AsObject();
            change(json);
            File.WriteAllText(path, json.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }

        static void CopyTree(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
                File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)));
        }

        public void Dispose()
        {
            foreach (var dir in _dirs)
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
