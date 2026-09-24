using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// F3-c (#856; FD-05, FD-03, FD-06; FR-01, FR-02, FR-06): the ball and the loose-ball models read the ground zone under
/// them (spec §6.1, §7.9, §16; Appendix B.9 <c>SF-10</c> zone half, <c>SF-11</c>, <c>SF-12</c>, <c>SF-14</c>).
///
/// <para>
/// <b>Two kinds of row live here.</b> The parity rows prove the move changed nothing: with the shipped rows — every ground
/// equal, every number today's — every path and every loose-ball tick is the pre-move one <em>bit for bit</em>, held against
/// the pre-move code kept verbatim below as the expectation (<see cref="Old"/>, <see cref="OldLoose"/>). The <c>SF</c> rows
/// prove the reads are real: each runs on a fixture root built in the test whose rows are <b>unequal</b> (the shipped rows
/// stay equal, so on them a wrong read and a right one look the same), and shows the ball following the row of the zone it
/// is in.
/// </para>
/// </summary>
public sealed class GroundReadTests
{
    static readonly ContentCatalog Game = ContentCatalog.Load(new DataRoot(Shipped.Content.Root.Shipped));

    /// <summary>The loose-ball clock the live play ticks on (<c>LivePlaySystem</c> at 60 Hz).</summary>
    const double Frame = 1.0 / 60.0;

    // ---------------------------------------------------------------------------------
    // Parity — with equal rows, every path is the pre-move path bit for bit
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Every park, a grid of grounders, choppers, liners, flies and fouls: the clipped path is, sample for
    /// sample and bit for bit, the path the pre-move integrator flies (<see cref="Old"/>: the flight exactly as it read
    /// <c>flight.roll</c> / <c>bounce</c> / <c>skid</c> / <c>wall</c>, those four blocks written in as the shipped numbers).
    /// The grid crosses every zone boundary and meets the fence and the foul wrap, so a read that moved the wrong way would
    /// show here even with equal rows only if it changed the arithmetic — which is the thing this row forbids.
    /// </summary>
    [Fact]
    public void EqualRowsFlyThePreMovePathBitForBitInEveryPark()
    {
        var paths = 0;
        var events = new HashSet<SampleEvent>();
        var catalog = Game;
        // Every park still at today's numbers (F9-a: Crystal names its own rows and is held by them).
        foreach (var park in TodaysParks.Of(catalog))
            foreach (var exit in new[] { 42.0, 70, 96, 118 })
                foreach (var launch in new[] { -9.0, 0, 4, 11, 15, 19, 24, 33, 52 })
                    foreach (var spray in new[] { -52.0, -31, -12, 0, 17, 38, 46 })
                    {
                        var expected = Old.Trajectory(exit, launch, spray, park, catalog.Rules, Old.Today);
                        var actual = BallFlight.Trajectory(exit, launch, spray, park, catalog.Rules);
                        SamePath($"{park.Id} {exit}/{launch}/{spray}", expected, actual);
                        foreach (var s in actual) events.Add(s.Event);
                        paths++;
                    }
        Assert.Equal(5 * 4 * 9 * 7, paths); // the five parks at today's numbers (Crystal names its own rows, F9-a)
        // The grid is not all flies into the seats: it hops, rolls, caroms off the fence and off the foul wrap, and leaves.
        Assert.Superset(new HashSet<SampleEvent> { SampleEvent.Ground, SampleEvent.Wall, SampleEvent.FoulWall, SampleEvent.Fence, SampleEvent.Stands }, events);
    }

    /// <summary>The open field (no park, no zones) and the continued ball (#721) are the pre-move paths too.</summary>
    [Fact]
    public void EqualRowsFlyThePreMoveOpenFieldAndContinuedPaths()
    {
        var catalog = Game;
        foreach (var exit in new[] { 42.0, 70, 96, 118 })
            foreach (var launch in new[] { -9.0, 0, 4, 11, 15, 19, 24, 33, 52 })
                foreach (var wind in new[] { 0.0, 9 })
                    SamePath($"open {exit}/{launch}/{wind}", Old.Trajectory(exit, launch, wind, catalog.Rules, Old.Today),
                        BallFlight.Trajectory(exit, launch, wind, catalog.Rules));

        // Every park still at today's numbers (F9-a: Crystal names its own rows and is held by them).
        foreach (var park in TodaysParks.Of(catalog))
            foreach (var (exit, launch, spray) in new[] { (96.0, 12.0, 0.0), (80.0, 4.0, -20.0), (105.0, 18.0, 30.0), (70.0, 30.0, 10.0) })
            {
                var path = BallFlight.Trajectory(exit, launch, spray, park, catalog.Rules);
                foreach (var after in new[] { 0.10, 0.45 })
                {
                    var t0 = BallFlight.HangTime(path, catalog.Rules) + after;
                    var (x, y, z) = BallFlight.PointAt(path, t0, catalog.Rules);
                    foreach (var (vx, vy, vz) in new[] { (0.0, 0.0, 30.0), (-12.0, 8.0, 40.0), (20.0, -3.0, -15.0) })
                        SamePath($"{park.Id} continue {exit}/{launch}/{spray}+{after}",
                            Old.Continue(path, t0, x, y, z, vx, vy, vz, launch, exit, park, catalog.Rules, Old.Today),
                            BallFlight.Continue(path, t0, x, y, z, vx, vy, vz, launch, exit, park, catalog.Rules));
                }
            }
    }

    /// <summary>
    /// The two loose-ball models, tick for tick and bit for bit against the pre-move <c>TickLooseBall</c> and
    /// <c>TickLocalBobble</c> (<see cref="OldLoose"/>, their ground numbers written in as the shipped 18 ft/s² and
    /// 0.35 / 0.90 / 6): a grid of places — the dirt, the grass, the track, the apron, against the fence so the edge stops
    /// the ball — speeds, headings and heights, run to rest.
    /// </summary>
    [Fact]
    public void EqualRowsRollTheLooseBallsThePreMoveWayBitForBit()
    {
        var ticks = 0;
        var catalog = Game;
        var rules = catalog.Rules;
        // Every park still at today's numbers (F9-a: Crystal names its own rows and is held by them).
        foreach (var park in TodaysParks.Of(catalog))
        {
            var zones = GroundZones.Of(park, rules);
            var fence = AtBatResolver.FenceAt(park, 0);
            foreach (var (x0, z0) in new[] { (0.0, 60.0), (30.0, rules.Flight.Classes.InfieldLipFt - 1), (-40.0, 220.0), (0.0, fence - 4), (70.0, 20.0), (0.0, -20.0) })
                foreach (var (vx0, vz0) in new[] { (0.0, 35.0), (-22.0, 9.0), (4.0, -5.0), (0.5, 0.2) })
                {
                    // The overthrow, from a roll to rest.
                    var (x, z, vx, vz) = (x0, z0, vx0, vz0);
                    var (ox, oz, ovx, ovz) = (x0, z0, vx0, vz0);
                    for (var i = 0; i < 600 && Math.Sqrt(vx * vx + vz * vz) > 0; i++, ticks++)
                    {
                        var step = BallFlight.OverthrowTick(zones, rules.Grounds, x, z, vx, vz, Frame);
                        var old = OldLoose.Overthrow(park, ox, oz, ovx, ovz, Frame);
                        Assert.True(Bits(old.X) == Bits(step.X) && Bits(old.Z) == Bits(step.Z) && Bits(old.VX) == Bits(step.VX)
                                    && Bits(old.VZ) == Bits(step.VZ) && Bits(old.Next) == Bits(step.Speed) && step.Y == 0 && !step.Air,
                            $"{park.Id} overthrow from ({x0}, {z0}) at ({vx0}, {vz0}), tick {i}: {old} vs {step}");
                        (x, z, vx, vz) = (step.X, step.Z, step.VX, step.VZ);
                        (ox, oz, ovx, ovz) = (old.X, old.Z, old.VX, old.VZ);
                    }

                    // The local bobble, from a spill in the air to rest.
                    foreach (var (y0, vy0) in new[] { (3.1, 0.0), (0.4, -2.0), (0.0, 0.0) })
                    {
                        var n = new OldLoose.Ball(x0, y0, z0, vx0 * 0.2, vy0, vz0 * 0.2, y0 > 1e-9);
                        var o = n;
                        for (var i = 0; i < 600 && (n.Air || n.VX != 0 || n.VZ != 0); i++, ticks++)
                        {
                            var step = BallFlight.LocalBobbleTick(zones, rules.Grounds, rules.Fielding.Handling, rules.Flight.Gravity,
                                n.X, n.Y, n.Z, n.VX, n.VY, n.VZ, n.Air, Frame);
                            o = OldLoose.Bobble(park, rules.Fielding.Handling, rules.Flight.Gravity, o, Frame);
                            Assert.True(Bits(o.X) == Bits(step.X) && Bits(o.Y) == Bits(step.Y) && Bits(o.Z) == Bits(step.Z)
                                        && Bits(o.VX) == Bits(step.VX) && Bits(o.VY) == Bits(step.VY) && Bits(o.VZ) == Bits(step.VZ)
                                        && o.Air == step.Air,
                                $"{park.Id} bobble from ({x0}, {y0}, {z0}), tick {i}: {o} vs {step}");
                            n = new OldLoose.Ball(step.X, step.Y, step.Z, step.VX, step.VY, step.VZ, step.Air);
                        }
                    }
                }
        }
        Assert.True(ticks > 10_000, $"only {ticks} ticks compared");
    }

    // ---------------------------------------------------------------------------------
    // SF-11 — the same grounder on two grounds
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-11</c>. The same grounder at Harbor and at a Harbor whose outfield names <c>ice</c>, on a fixture root
    /// whose <c>ice</c> row rolls at half of grass's friction and whose <c>dirt</c> row at more than it: the two paths are one
    /// path until the ball crosses the lip, the iced one rolls farther, and — read off the path's own samples, the speed of
    /// one step against the next — the speed a step loses is the friction of the zone of the sample it starts from, so it
    /// changes exactly at the first sample past the lip, where <see cref="GroundZones.ZoneAt"/> changes.
    /// </summary>
    [Fact]
    public void SF11_TheSameGrounderRollsFartherOnTheSlickerOutfieldAndChangesItsLossAtTheLip()
    {
        using var fixture = new UnequalGrounds();
        var catalog = fixture.Catalog;
        var rules = catalog.Rules;
        var plain = catalog.Parks[ParkId.Harbor];
        var iced = plain with { Zones = new ParkZones(Outfield: Ground.Ice) };
        var dt = 1.0 / rules.Flight.SampleHz;

        var (exit, launch, spray) = LipGrounder(plain, rules);
        var a = BallFlight.Trajectory(exit, launch, spray, plain, rules);
        var b = BallFlight.Trajectory(exit, launch, spray, iced, rules);
        Assert.True(b[^1].Dist > a[^1].Dist + 10, $"{catalog.Root.Provenance}: the ice roll ends at {b[^1].Dist:0.0} ft, the grass at {a[^1].Dist:0.0}");

        foreach (var (park, path, outfield) in new[] { (plain, a, rules.Grounds.Grass), (iced, b, rules.Grounds.Ice) })
        {
            var zones = GroundZones.Of(park, rules);
            var roll = RollStart(path);
            var lip = -1;
            for (var i = roll; i < path.Count; i++)
                if (zones.ZoneAt(path[i].X, path[i].Z) != GroundZone.InfieldDirt) { lip = i; break; }
            Assert.True(lip > roll + 2 && lip < path.Count - 3, $"{park.Id}: the roll ({roll}..{path.Count}) must cross the lip; crossed at {lip}");
            Assert.Equal(GroundZone.Outfield, zones.ZoneAt(path[lip].X, path[lip].Z));

            // The step that starts from sample i - 1 loses that sample's zone's friction × dt of speed: the lip's dirt, the
            // outfield's own row, the track's dirt again if it gets there. A step off the fence is a carom, not a roll.
            var checkedSteps = 0;
            for (var i = roll + 2; i < path.Count - 1; i++)
            {
                if (path[i].Event != SampleEvent.Ground || path[i - 1].Event != SampleEvent.Ground || path[i - 2].Event != SampleEvent.Ground) continue;
                var loss = Speed(path, i - 1, dt) - Speed(path, i, dt);
                var row = zones.ZoneAt(path[i - 1].X, path[i - 1].Z) switch
                {
                    GroundZone.InfieldDirt or GroundZone.WarningTrack => rules.Grounds.Dirt,
                    GroundZone.Outfield => outfield,
                    var other => throw new InvalidOperationException($"the grounder went {other}")
                };
                Assert.True(Math.Abs(loss - row.Roll.Friction * dt) < 1e-9,
                    $"{park.Id}: step {i} lost {loss:0.000000} ft/s, its zone's row says {row.Roll.Friction * dt:0.000000}");
                checkedSteps++;
            }
            Assert.True(checkedSteps > 20, $"{park.Id}: only {checkedSteps} rolling steps");
            // And the first loss that is the outfield's is the step leaving the first sample past the lip.
            Assert.True(Math.Abs(Speed(path, lip - 1, dt) - Speed(path, lip, dt) - rules.Grounds.Dirt.Roll.Friction * dt) < 1e-9);
            Assert.True(Math.Abs(Speed(path, lip, dt) - Speed(path, lip + 1, dt) - outfield.Roll.Friction * dt) < 1e-9);
        }

        // One grounder: until the first sample past the lip the two parks' paths are the same bits.
        var shared = Math.Min(a.Count, b.Count);
        var cross = Enumerable.Range(0, shared).First(i => GroundZones.Of(plain, rules).ZoneAt(a[i].X, a[i].Z) != GroundZone.InfieldDirt);
        SamePath("before the lip", a.Take(cross + 1).ToList(), b.Take(cross + 1).ToList());
    }

    // ---------------------------------------------------------------------------------
    // SF-10 (the zone half) — the same fly lands on two outfield rows
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-10</c>'s zone half. The same fly lands in the outfield of Harbor (grass) and of a Harbor whose outfield
    /// names <c>ice</c>, on a fixture root whose <c>ice</c> row hops lower and shorter. The carry to the first landing is the
    /// same bits in both — the ground does not exist until the ball meets it — and from that bounce on each path is exactly
    /// the pre-move integrator run on <em>that</em> row's numbers: the hop follows the row it lands on. (The air half of
    /// <c>SF-10</c>, the same fly at two drags, is F3-a's <c>ParkEnvironmentTests</c>.)
    /// </summary>
    [Fact]
    public void SF10_TheSameFlyCarriesTheSameAndHopsOffTheRowItLandsOn()
    {
        using var fixture = new UnequalGrounds();
        var catalog = fixture.Catalog;
        var rules = catalog.Rules;
        var plain = catalog.Parks[ParkId.Harbor];
        var iced = plain with { Zones = new ParkZones(Outfield: Ground.Ice) };
        // The softest fly at 34° that lands past the lip: it hops and rolls out on the outfield, short of the track.
        const double launch = 34, spray = 8;
        var exit = Enumerable.Range(0, 40).Select(i => 50.0 + 2 * i).First(e =>
            GroundZones.Of(plain, rules).ZoneAt(BallFlight.LandingPoint(BallFlight.Trajectory(e, launch, spray, plain, rules)).X,
                BallFlight.LandingPoint(BallFlight.Trajectory(e, launch, spray, plain, rules)).Z) == GroundZone.Outfield);

        var onGrass = BallFlight.Trajectory(exit, launch, spray, plain, rules);
        var onIce = BallFlight.Trajectory(exit, launch, spray, iced, rules);
        var land = BallFlight.LandingIndex(onGrass);
        Assert.Equal(land, BallFlight.LandingIndex(onIce));
        Assert.Equal(SampleEvent.Ground, onGrass[land].Event);
        Assert.Equal(BallFlight.FirstLandingDist(onGrass, rules: Rules.Default), BallFlight.FirstLandingDist(onIce, rules: Rules.Default));
        SamePath("to the first landing", onGrass.Take(land + 1).ToList(), onIce.Take(land + 1).ToList());

        // Every ground contact of both paths is in the outfield, so each whole path is one row's.
        foreach (var (park, path) in new[] { (plain, onGrass), (iced, onIce) })
            for (var i = land; i < path.Count; i++)
                if (path[i].Event != SampleEvent.None)
                    Assert.Equal(GroundZone.Outfield, GroundZones.Of(park, rules).ZoneAt(path[i].X, path[i].Z));

        SamePath("the grass hop", Old.Trajectory(exit, launch, spray, plain, rules, Old.Of(rules.Grounds.Grass, rules.Walls.Padded)), onGrass);
        SamePath("the ice hop", Old.Trajectory(exit, launch, spray, iced, rules, Old.Of(rules.Grounds.Ice, rules.Walls.Padded)), onIce);
        Assert.True(ApexAfter(onIce, land) < ApexAfter(onGrass, land) - 0.5,
            $"the ice row's hop rises {ApexAfter(onIce, land):0.00} ft, the grass row's {ApexAfter(onGrass, land):0.00}");
    }

    // ---------------------------------------------------------------------------------
    // SF-12 — the same carom off the padded row at two values
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-12</c>. The same ball rolling into the centre-field fence at an angle, on the shipped table and on a
    /// fixture whose <c>padded</c> row is 0.30 / 0.60 instead of 0.48 / 0.82: read off the samples on either side of the wall,
    /// the speed into the wall comes back at that table's restitution and the speed along it keeps that table's tangential.
    /// Every segment is <c>padded</c> today (<see cref="WallMaterial.OfSegment"/>); a span's own material is F2-c's half.
    /// </summary>
    [Fact]
    public void SF12_TheSameCaromFollowsThePaddedRowOfEachTable()
    {
        using var fixture = new UnequalGrounds(walls: json => json["padded"] = new JsonObject { ["restitution"] = 0.30, ["tangential"] = 0.60 });
        var (control, softer) = (Game, fixture.Catalog);
        foreach (var catalog in new[] { control, softer })
        {
            var rules = catalog.Rules;
            var park = catalog.Parks[ParkId.Harbor];
            var fence = AtBatResolver.FenceAt(park, 0);
            var dt = 1.0 / rules.Flight.SampleHz;
            // Rolling, 6 ft short of the fence, heading out and to the right.
            var path = BallFlight.Continue([], 0, -3, 0, fence - 6, 14, 0, 40, 0, 90, park, rules);
            var hit = Enumerable.Range(1, path.Count - 1).First(i => path[i].Event == SampleEvent.Wall);
            Assert.True(hit >= 2 && hit + 1 < path.Count);

            // Into the wall: the step's velocity after its own friction.
            var zones = GroundZones.Of(park, rules);
            var (inX, inZ) = Velocity(path, hit - 1, dt);
            var inSpeed = Math.Sqrt(inX * inX + inZ * inZ);
            var k = (inSpeed - zones.RowAt(path[hit - 1].X, path[hit - 1].Z, rules.Grounds).Roll.Friction * dt) / inSpeed;
            (inX, inZ) = (inX * k, inZ * k);
            var crossing = FieldBounds.Of(park).Cross(path[hit - 1].X, path[hit - 1].Z, path[hit - 1].X + inX * dt, path[hit - 1].Z + inZ * dt);
            Assert.NotNull(crossing);
            var n = crossing.Value.Segment;
            Assert.Equal(WallMaterial.Padded, WallMaterial.OfSegment(n));

            // Out of the wall: the next step's velocity with its friction put back.
            var (outX, outZ) = Velocity(path, hit + 1, dt);
            var outSpeed = Math.Sqrt(outX * outX + outZ * outZ);
            var back = (outSpeed + zones.RowAt(path[hit].X, path[hit].Z, rules.Grounds).Roll.Friction * dt) / outSpeed;
            (outX, outZ) = (outX * back, outZ * back);

            var inNormal = inX * n.Nx + inZ * n.Nz;
            var outNormal = outX * n.Nx + outZ * n.Nz;
            var (inTx, inTz) = (inX - inNormal * n.Nx, inZ - inNormal * n.Nz);
            var (outTx, outTz) = (outX - outNormal * n.Nx, outZ - outNormal * n.Nz);
            var row = rules.Walls.Of(WallMaterial.Padded);
            Assert.True(inNormal > 5, $"the ball must meet the wall going out; normal speed {inNormal:0.00}");
            Assert.True(Math.Abs(outNormal + row.Restitution * inNormal) < 1e-6,
                $"{catalog.Root.Provenance}: normal {inNormal:0.0000} came back {outNormal:0.0000}, the row says × {row.Restitution}");
            Assert.True(Math.Abs(outTx - row.Tangential * inTx) < 1e-6 && Math.Abs(outTz - row.Tangential * inTz) < 1e-6,
                $"{catalog.Root.Provenance}: along the wall ({inTx:0.0000}, {inTz:0.0000}) became ({outTx:0.0000}, {outTz:0.0000}), the row says × {row.Tangential}");
        }
        Assert.NotEqual(control.Rules.Walls.Padded.Restitution, softer.Rules.Walls.Padded.Restitution);
    }

    // ---------------------------------------------------------------------------------
    // SF-14 — the three loose-ball ground models read the same zone row
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-14</c>. On a fixture root whose <c>dirt</c> row differs from <c>grass</c> in every loose-ball number,
    /// the batted ball's roll, the overthrow and the local bobble are dropped at one point three feet inside the lip, then at
    /// one three feet past it. At each point each model slows, rebounds and keeps its roll by the row of that point's zone —
    /// dirt inside, grass past — and moving the point across the lip moves every one of them to the other row.
    /// </summary>
    [Fact]
    public void SF14_TheBattedRollTheOverthrowAndTheBobbleReadTheRowOfTheSamePoint()
    {
        using var fixture = new UnequalGrounds();
        var catalog = fixture.Catalog;
        var rules = catalog.Rules;
        var park = catalog.Parks[ParkId.Harbor];
        var zones = GroundZones.Of(park, rules);
        var lip = rules.Flight.Classes.InfieldLipFt;
        var h = rules.Fielding.Handling;
        var g = rules.Flight.Gravity;
        var dt = 1.0 / rules.Flight.SampleHz;

        foreach (var (z, zone, row) in new[] { (lip - 3, GroundZone.InfieldDirt, rules.Grounds.Dirt), (lip + 3, GroundZone.Outfield, rules.Grounds.Grass) })
        {
            Assert.Equal(zone, zones.ZoneAt(0, z));
            Assert.Same(row, zones.RowAt(0, z, rules.Grounds));

            // The batted ball, dropped rolling at the point: its first step loses the row's friction.
            var batted = BallFlight.Continue([], 0, 0, 0, z, 6, 0, 0, 0, 80, park, rules);
            var scale = rules.Flight.TimeScale;
            Assert.True(Math.Abs(6 - Speed(batted, 1, dt * scale) - row.Roll.Friction * dt / scale) < 1e-9, $"batted roll at {z:0.00}: {Speed(batted, 1, dt * scale)}");

            // The overthrow: the row's deceleration.
            var thrown = BallFlight.OverthrowTick(zones, rules.Grounds, 0, z, 6, 0, Frame);
            Assert.Equal(6 - row.Overthrow.DecelFtPerSec2 * Frame, thrown.Speed, 12);

            // The local bobble landing: the row's restitution and the roll it keeps; rolling: the row's deceleration.
            var landing = BallFlight.LocalBobbleTick(zones, rules.Grounds, h, g, 0, 0.05, z, 4, -14.5, 0, true, Frame);
            var down = 14.5 + g * Frame;
            Assert.True(landing.Air, "the rebound clears the settle and stays under the ceiling at both rows");
            Assert.Equal(down * row.Bobble.Restitution, landing.VY, 12);
            Assert.Equal(4 * row.Bobble.GroundRetain, landing.VX, 12);
            var rolling = BallFlight.LocalBobbleTick(zones, rules.Grounds, h, g, 0, 0, z, 4, 0, 0, false, Frame);
            Assert.Equal(4 - row.Bobble.DecelFtPerSec2 * Frame, rolling.Speed, 12);
        }

        // And the two rows really are two: each model differs across the lip.
        Assert.NotEqual(rules.Grounds.Dirt.Roll.Friction, rules.Grounds.Grass.Roll.Friction);
        Assert.NotEqual(rules.Grounds.Dirt.Overthrow.DecelFtPerSec2, rules.Grounds.Grass.Overthrow.DecelFtPerSec2);
        Assert.NotEqual(rules.Grounds.Dirt.Bobble.Restitution, rules.Grounds.Grass.Bobble.Restitution);
        Assert.NotEqual(rules.Grounds.Dirt.Bobble.GroundRetain, rules.Grounds.Grass.Bobble.GroundRetain);
        Assert.NotEqual(rules.Grounds.Dirt.Bobble.DecelFtPerSec2, rules.Grounds.Grass.Bobble.DecelFtPerSec2);
    }

    // ---------------------------------------------------------------------------------
    // The open field, the table handed over, and no silent fallback
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The open field has no park and so no zones; it names its ground, <see cref="BallFlight.OpenFieldGround"/>, which is
    /// <c>grass</c>. On a fixture whose grass rolls slicker, the open-field grounder rolls farther than on the shipped table;
    /// on one where only dirt differs, it is the shipped path to the bit.
    /// </summary>
    [Fact]
    public void TheOpenFieldStandsOnGrassAndOnlyOnGrass()
    {
        Assert.Equal(Ground.Grass, BallFlight.OpenFieldGround);
        using var slickGrass = new UnequalGrounds(grounds: json => json["grass"]!["roll"]!["friction"] = 11);
        using var slowDirt = new UnequalGrounds();
        var (control, grass, dirt) = (Game, slickGrass.Catalog, slowDirt.Catalog);
        var shipped = BallFlight.Trajectory(90, 3, 0, control.Rules);
        Assert.True(BallFlight.Trajectory(90, 3, 0, grass.Rules)[^1].Dist > shipped[^1].Dist + 10);
        SamePath("the open field ignores dirt", shipped, BallFlight.Trajectory(90, 3, 0, dirt.Rules));
    }

    /// <summary>
    /// FR-01 / FR-02 at run time. A zone that names a ground with no row — possible only for a park built past the loader,
    /// which refuses it (<c>SF-03</c>) — stops the ball the moment it needs that row, in the flight and in both loose-ball
    /// models; it never rolls on grass by default. And the rows come from the table handed over, not the process default:
    /// the fixture's are not <see cref="Rules.Default"/>'s, and the path follows the fixture's.
    /// </summary>
    [Fact]
    public void AGroundWithNoRowStopsTheBallAndTheRowsAreTheHandedTables()
    {
        var rules = Game.Rules;
        var mud = Game.Parks[ParkId.Harbor] with { Zones = new ParkZones(Outfield: "mud") };
        var zones = GroundZones.Of(mud, rules);
        var past = rules.Flight.Classes.InfieldLipFt + 20;

        var (exit, launch, spray) = LipGrounder(Game.Parks[ParkId.Harbor], rules);
        var thrown = Assert.Throws<ArgumentException>(() => BallFlight.Trajectory(exit, launch, spray, mud, rules));
        Assert.Contains("'mud' is not a ground with a row in rules/grounds.json", thrown.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => BallFlight.OverthrowTick(zones, rules.Grounds, 0, past, 6, 0, Frame));
        Assert.Throws<ArgumentException>(() => BallFlight.LocalBobbleTick(zones, rules.Grounds, rules.Fielding.Handling, rules.Flight.Gravity, 0, 0, past, 4, 0, 0, false, Frame));
        // Inside the lip the ball is on dirt, which has a row: nothing there asks for mud.
        _ = BallFlight.OverthrowTick(zones, rules.Grounds, 0, 60, 6, 0, Frame);

        using var fixture = new UnequalGrounds();
        var park = fixture.Catalog.Parks[ParkId.Harbor];
        Assert.NotEqual(Rules.Default.Grounds.Dirt.Roll.Friction, fixture.Catalog.Rules.Grounds.Dirt.Roll.Friction);
        (exit, launch, spray) = LipGrounder(park, fixture.Catalog.Rules);
        Assert.NotEqual(BallFlight.Trajectory(exit, launch, spray, park, Rules.Default)[^1].Dist,
            BallFlight.Trajectory(exit, launch, spray, park, fixture.Catalog.Rules)[^1].Dist);
    }

    // ---------------------------------------------------------------------------------
    // Reading a path
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The softest grounder up the middle (−2°, 6° of spray) that settles into its roll on the infield dirt and rolls at
    /// least 20 ft past the lip on this table — chosen by the table rather than written in, because a fixture's dirt may roll stickier than the shipped one.
    /// </summary>
    static (double Exit, double Launch, double Spray) LipGrounder(Park park, RulesTable rules)
    {
        const double launch = -2, spray = 6;
        var zones = GroundZones.Of(park, rules);
        foreach (var exit in Enumerable.Range(0, 40).Select(i => 70.0 + 2 * i))
        {
            var path = BallFlight.Trajectory(exit, launch, spray, park, rules);
            var start = path[RollStart(path)];
            if (zones.ZoneAt(start.X, start.Z) == GroundZone.InfieldDirt && path[^1].Dist > rules.Flight.Classes.InfieldLipFt + 20)
                return (exit, launch, spray);
        }
        throw new InvalidOperationException("no grounder up the middle rolls across the lip on this table");
    }

    /// <summary>The first sample of the path's final roll: the last airborne sample's successor, after which every sample is on the ground.</summary>
    static int RollStart(IReadOnlyList<Sample> path)
    {
        var last = path.Count - 1;
        while (last > 0 && path[last - 1].Height <= 0 && path[last - 1].Event is SampleEvent.Ground) last--;
        return last;
    }

    /// <summary>The ball's horizontal speed over the step that ends at sample <paramref name="i"/>, in ft/s of the integrator's own step.</summary>
    static double Speed(IReadOnlyList<Sample> path, int i, double dt)
    {
        var (vx, vz) = Velocity(path, i, dt);
        return Math.Sqrt(vx * vx + vz * vz);
    }

    static (double X, double Z) Velocity(IReadOnlyList<Sample> path, int i, double dt) =>
        ((path[i].X - path[i - 1].X) / dt, (path[i].Z - path[i - 1].Z) / dt);

    static double ApexAfter(IReadOnlyList<Sample> path, int land)
    {
        var apex = 0.0;
        for (var i = land + 1; i < path.Count && path[i].Event != SampleEvent.Ground; i++) apex = Math.Max(apex, path[i].Height);
        return apex;
    }

    /// <summary>Two paths, sample for sample, compared by the bits of every double — not by a tolerance.</summary>
    static void SamePath(string what, IReadOnlyList<Sample> expected, IReadOnlyList<Sample> actual)
    {
        Assert.True(expected.Count == actual.Count, $"{what}: {expected.Count} samples expected, {actual.Count} flown");
        for (var i = 0; i < expected.Count; i++)
        {
            var e = expected[i];
            var a = actual[i];
            Assert.True(Bits(e.T) == Bits(a.T) && Bits(e.Dist) == Bits(a.Dist) && Bits(e.Height) == Bits(a.Height)
                        && Bits(e.X) == Bits(a.X) && Bits(e.Z) == Bits(a.Z) && e.Event == a.Event,
                $"{what}: sample {i} expected {e}, flown {a}");
        }
    }

    static long Bits(double value) => BitConverter.DoubleToInt64Bits(value);

    // ---------------------------------------------------------------------------------
    // The fixture root — unequal rows, built here
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// A throwaway copy of the data root whose ground (and wall) rows are unequal. By default <c>dirt</c> differs from every other row in every number a loose ball reads — rolls
    /// stickier (30), stops an overthrow faster (24), and takes a bobble back lower, keeps less of it and slows it harder
    /// (0.30 / 0.70 / 9) — and <c>ice</c> rolls at half of grass's friction (11) and hops lower and shorter (0.30 / 0.60).
    /// Grass keeps the shipped numbers. The shipped rows themselves stay equal: nothing here is a proposed value.
    /// </summary>
    sealed class UnequalGrounds : IDisposable
    {
        /// <summary>
        /// A fixture root. With no argument the grounds are <see cref="DirtAndIce"/>; with only <paramref name="walls"/> the
        /// grounds are the shipped (equal) rows and only the wall row differs.
        /// </summary>
        public UnequalGrounds(Action<JsonObject>? grounds = null, Action<JsonObject>? walls = null)
        {
            Root = Path.Combine(Path.GetTempPath(), "grand-sluggers-ground-reads-" + Guid.NewGuid().ToString("N"));
            CopyTree(GroundReadTests.Game.Root.Shipped, Root);
            if ((grounds ?? (walls is null ? DirtAndIce : null)) is { } change) Change("grounds.json", change);
            if (walls is not null) Change("walls.json", walls);
            Catalog = ContentCatalog.Load(new DataRoot(Root));
        }

        static void DirtAndIce(JsonObject json)
        {
            json["dirt"]!["roll"]!["friction"] = 30;
            json["dirt"]!["overthrow"]!["decelFtPerSec2"] = 24;
            json["dirt"]!["bobble"] = new JsonObject { ["restitution"] = 0.30, ["groundRetain"] = 0.70, ["decelFtPerSec2"] = 9 };
            json["ice"]!["roll"]!["friction"] = 11;
            json["ice"]!["bounce"] = new JsonObject { ["restitution"] = 0.30, ["horizontal"] = 0.60, ["minVy"] = 3.6 };
        }

        public string Root { get; }
        public ContentCatalog Catalog { get; }

        void Change(string file, Action<JsonObject> change)
        {
            var path = Path.Combine(Root, RulesTable.Directory, file);
            var json = JsonNode.Parse(File.ReadAllText(path), null, new System.Text.Json.JsonDocumentOptions
            {
                CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            })!.AsObject();
            change(json);
            File.WriteAllText(path, json.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);

        static void CopyTree(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
                File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)));
        }
    }

    // ---------------------------------------------------------------------------------
    // The pre-move code, kept as the expectation
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The flight as it was before F3-c moved its reads, verbatim, with the four blocks it read from <c>flight.json</c> passed
    /// in as one set of numbers for the whole path (<see cref="Numbers"/>). <see cref="Today"/> is the shipped set — what
    /// <c>flight.roll</c> / <c>bounce</c> / <c>skid</c> / <c>wall</c> carried the day they moved into every
    /// ground row and the <c>padded</c> wall row (FD-05). With equal rows the moved flight must be this, to the last bit; with
    /// one row under a whole path, it must be this run on that row.
    /// </summary>
    static class Old
    {
        const double MphToFtPerSec = BallFlight.MphToFtPerSec;

        public sealed record Numbers(
            double RollFriction, double RollRestSpeed,
            double BounceRestitution, double BounceHorizontal, double BounceMinVy,
            double SkidLaunchMinDeg, double SkidLaunchMaxDeg, double SkidMinVy, double SkidRestitution, double SkidHorizontal,
            double WallRestitution, double WallTangential);

        public static readonly Numbers Today = new(22, 1.4, 0.48, 0.82, 3.6, 14, 22, 2.2, 0.28, 0.93, 0.48, 0.82);

        public static Numbers Of(GroundRules g, WallRules w) => new(
            g.Roll.Friction, g.Roll.RestSpeed, g.Bounce.Restitution, g.Bounce.Horizontal, g.Bounce.MinVy,
            g.Skid.ImpactMinDeg, g.Skid.ImpactMaxDeg, g.Skid.MinVy, g.Skid.Restitution, g.Skid.Horizontal, w.Restitution, w.Tangential);

        public static IReadOnlyList<Sample> Trajectory(double exitMph, double launchDeg, double windMph, RulesTable rules, Numbers g) =>
            Integrate(exitMph, launchDeg, 0, windMph, (0, 1), null, rules, g);

        public static IReadOnlyList<Sample> Trajectory(double exitMph, double launchDeg, double sprayDeg, Park park, RulesTable rules, Numbers g) =>
            Integrate(exitMph, launchDeg, sprayDeg, park.WindMph, park.WindDirection, FieldBounds.Of(park), rules, g);

        static IReadOnlyList<Sample> Integrate(
            double exitMph, double launchDeg, double sprayDeg, double windMph, (double X, double Z) windDir,
            FieldBounds.Boundary? walls, RulesTable rules, Numbers g)
        {
            var f = rules.Flight;
            var v = exitMph * MphToFtPerSec;
            var a = launchDeg * Math.PI / 180.0;
            var s = sprayDeg * Math.PI / 180.0;
            var vh = v * Math.Cos(a);
            var vx = vh * Math.Sin(s);
            var vz = vh * Math.Cos(s);
            var vy = v * Math.Sin(a);
            var wx = windMph * MphToFtPerSec * f.WindMul * windDir.X;
            var wz = windMph * MphToFtPerSec * f.WindMul * windDir.Z;
            var skid = launchDeg >= g.SkidLaunchMinDeg && launchDeg < g.SkidLaunchMaxDeg;
            var scale = f.TimeScaleFor(launchDeg, exitMph, rules);
            var list = new List<Sample>(512) { new(0, 0, f.PlateHeightFt, 0, 0) };
            Run(list, f, g, walls, 0.0, 0.0, f.PlateHeightFt, 0.0, vx, vy, vz, wx, wz, skid, scale, rolling: false, grounded: false);
            return list;
        }

        public static IReadOnlyList<Sample> Continue(IReadOnlyList<Sample> path, double fromT, double x, double y, double z,
            double vx, double vy, double vz, double launchDeg, double exitMph, Park park, RulesTable r, Numbers g)
        {
            var f = r.Flight;
            var wx = park.WindMph * MphToFtPerSec * f.WindMul * park.WindDirection.X;
            var wz = park.WindMph * MphToFtPerSec * f.WindMul * park.WindDirection.Z;
            var skid = launchDeg >= g.SkidLaunchMinDeg && launchDeg < g.SkidLaunchMaxDeg;
            var scale = f.TimeScaleFor(launchDeg, exitMph, r);
            var list = new List<Sample>(512);
            foreach (var s in path)
                if (s.T < fromT - 1e-9) list.Add(s);
            var y0 = Math.Max(0, y);
            list.Add(new Sample(fromT, Math.Sqrt(x * x + z * z), y0, x, z));
            var rolling = y0 <= 1e-9 && Math.Abs(vy) < 1e-9;
            Run(list, f, g, FieldBounds.Of(park), fromT, x, y0, z, vx * scale, vy * scale, vz * scale, wx, wz, skid, scale, rolling, grounded: true);
            return list;
        }

        static void Run(List<Sample> list, FlightRules f, Numbers g, FieldBounds.Boundary? walls, double t0, double x, double y, double z,
            double vx, double vy, double vz, double wx, double wz, bool skid, double scale, bool rolling, bool grounded)
        {
            var dt = 1.0 / f.SampleHz;
            var gone = false;
            var steps = (int)(f.SampleHz * f.MaxSeconds);
            for (var i = 0; i < steps; i++)
            {
                var t = t0 + (i + 1) * dt * scale;
                if (rolling)
                {
                    var speed = Math.Sqrt(vx * vx + vz * vz);
                    var decel = g.RollFriction * dt;
                    if (speed <= decel || speed - decel < g.RollRestSpeed)
                    {
                        vx = vz = 0;
                        list.Add(new Sample(t, Math.Sqrt(x * x + z * z), 0, x, z, SampleEvent.Ground));
                        break;
                    }
                    var k = (speed - decel) / speed;
                    vx *= k;
                    vz *= k;
                    var rx = x + vx * dt;
                    var rz = z + vz * dt;
                    var ev = SampleEvent.Ground;
                    if (walls is not null && walls.Cross(x, z, rx, rz) is { } hit)
                    {
                        ev = hit.Segment.Kind == FieldBounds.WallKind.FairFence ? SampleEvent.Wall : SampleEvent.FoulWall;
                        (rx, rz) = Carom(hit, ref vx, ref vz, g);
                    }
                    x = rx;
                    z = rz;
                    list.Add(new Sample(t, Math.Sqrt(x * x + z * z), 0, x, z, ev));
                    continue;
                }

                var rvx = vx - wx;
                var rvz = vz - wz;
                var rs = Math.Sqrt(rvx * rvx + vy * vy + rvz * rvz);
                vx -= f.Drag * rs * rvx * dt;
                vz -= f.Drag * rs * rvz * dt;
                vy -= (f.Gravity + f.Drag * rs * vy) * dt;
                var nx = x + vx * dt;
                var nz = z + vz * dt;
                var ny = y + vy * dt;
                var evt = SampleEvent.None;

                if (!gone && walls is not null && walls.Cross(x, z, nx, nz) is { } cross)
                {
                    var h = y + (ny - y) * cross.U;
                    var fair = cross.Segment.Kind == FieldBounds.WallKind.FairFence;
                    if (h > cross.Segment.HeightFt)
                    {
                        evt = fair ? SampleEvent.Fence : SampleEvent.Stands;
                        gone = true;
                    }
                    else
                    {
                        evt = fair ? SampleEvent.Wall : SampleEvent.FoulWall;
                        (nx, nz) = Carom(cross, ref vx, ref vz, g);
                        ny = h;
                    }
                }

                if (ny <= 0)
                {
                    ny = 0;
                    if (gone)
                    {
                        x = nx;
                        z = nz;
                        list.Add(new Sample(t, Math.Sqrt(x * x + z * z), 0, x, z, evt));
                        break;
                    }
                    grounded = true;
                    if (evt == SampleEvent.None) evt = SampleEvent.Ground;
                    if (vy < 0)
                    {
                        var angle = Math.Atan2(-vy, Math.Sqrt(vx * vx + vz * vz)) * 180 / Math.PI;
                        var blend = Math.Clamp((angle - g.SkidLaunchMinDeg) / (g.SkidLaunchMaxDeg - g.SkidLaunchMinDeg), 0, 1);
                        var minVy = g.SkidMinVy + blend * (g.BounceMinVy - g.SkidMinVy);
                        var rest = g.SkidRestitution + blend * (g.BounceRestitution - g.SkidRestitution);
                        var horiz = g.SkidHorizontal + blend * (g.BounceHorizontal - g.SkidHorizontal);
                        if (-vy < minVy)
                        {
                            vy = 0;
                            rolling = true;
                        }
                        else
                        {
                            vy = -vy * rest;
                            vx *= horiz;
                            vz *= horiz;
                        }
                    }
                }
                x = nx;
                y = ny;
                z = nz;
                list.Add(new Sample(t, Math.Sqrt(x * x + z * z), Math.Max(0, y), x, z, evt));
            }
        }

        static (double X, double Z) Carom(FieldBounds.Crossing hit, ref double vx, ref double vz, Numbers g)
        {
            var n = hit.Segment;
            var vn = vx * n.Nx + vz * n.Nz;
            var tx = vx - vn * n.Nx;
            var tz = vz - vn * n.Nz;
            var outward = Math.Max(0, vn);
            vx = tx * g.WallTangential - outward * g.WallRestitution * n.Nx;
            vz = tz * g.WallTangential - outward * g.WallRestitution * n.Nz;
            const double inside = 0.15;
            return (hit.X - n.Nx * inside, hit.Z - n.Nz * inside);
        }
    }

    /// <summary>
    /// <c>LivePlaySystem.TickLooseBall</c> and <c>TickLocalBobble</c> as they were before F3-c, verbatim but for the state
    /// they wrote, with the ground numbers they read from <c>fielding.json</c> written in as the shipped ones:
    /// <c>overthrow.decelFtPerSec2</c> 18 and <c>handling.bobbleRestitution</c> / <c>bobbleGroundRetain</c> /
    /// <c>bobbleDecelFtPerSec2</c> 0.35 / 0.90 / 6.
    /// </summary>
    static class OldLoose
    {
        const double OverthrowDecelFtPerSec2 = 18;
        const double BobbleRestitution = 0.35, BobbleGroundRetain = 0.90, BobbleDecelFtPerSec2 = 6;

        public readonly record struct Ball(double X, double Y, double Z, double VX, double VY, double VZ, bool Air);

        public static (double X, double Z, double VX, double VZ, double Next) Overthrow(Park park, double x, double z, double vx, double vz, double dt)
        {
            var speed = Math.Sqrt(vx * vx + vz * vz);
            var decel = OverthrowDecelFtPerSec2 * dt;
            var next = Math.Max(0, speed - decel);
            var nx = x + vx / speed * (speed + next) * 0.5 * dt;
            var nz = z + vz / speed * (speed + next) * 0.5 * dt;
            var inside = FieldBounds.Clamp(park, nx, nz);
            if (Math.Abs(inside.X - nx) > 1e-6 || Math.Abs(inside.Z - nz) > 1e-6) next = 0;
            return (inside.X, inside.Z, speed > 0 ? vx / speed * next : 0, speed > 0 ? vz / speed * next : 0, next);
        }

        public static Ball Bobble(Park park, HandlingRules h, double g, Ball b, double dt)
        {
            var (ballX, ballY, ballZ, looseVX, looseVY, looseVZ, looseAir) = b;
            var nx = ballX + looseVX * dt;
            var nz = ballZ + looseVZ * dt;
            if (looseAir)
            {
                looseVY -= g * dt;
                var ny = ballY + looseVY * dt;
                if (ny <= 0)
                {
                    ny = 0;
                    var up = -looseVY * BobbleRestitution;
                    up = Math.Min(up, Math.Sqrt(2 * g * Math.Max(0, h.BobbleReboundCapFt)));
                    if (up * up / (2 * g) <= h.BobbleSettleFt) up = 0;
                    looseVX *= BobbleGroundRetain;
                    looseVZ *= BobbleGroundRetain;
                    looseVY = up;
                    looseAir = up > 0;
                }
                ballY = ny;
            }
            else
            {
                var speed = Math.Sqrt(looseVX * looseVX + looseVZ * looseVZ);
                var next = Math.Max(0, speed - BobbleDecelFtPerSec2 * dt);
                nx = ballX + (speed > 0 ? looseVX / speed * (speed + next) * 0.5 * dt : 0);
                nz = ballZ + (speed > 0 ? looseVZ / speed * (speed + next) * 0.5 * dt : 0);
                looseVX = speed > 0 ? looseVX / speed * next : 0;
                looseVZ = speed > 0 ? looseVZ / speed * next : 0;
                ballY = 0;
            }
            var inside = FieldBounds.Clamp(park, nx, nz);
            if (Math.Abs(inside.X - nx) > 1e-6 || Math.Abs(inside.Z - nz) > 1e-6) looseVX = looseVZ = 0;
            return new Ball(inside.X, ballY, inside.Z, looseVX, looseVY, looseVZ, looseAir);
        }
    }
}
