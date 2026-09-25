using GrandSluggers.Sim;
using Xunit;
using Xunit.Abstractions;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Crystal Rink's differences (F9-a; FD-18, FD-02, FD-03, FD-04 B, FD-06, FD-13-R2). The park declares its intent first —
/// an ice outfield where grounders run and skid farther and fielders are slower to start, stop and turn; glass boards
/// with a livelier carom; cold air that carries a little less — and each row below is the same ball (or body) in two
/// states, Harbor's and Crystal's, on the shipped numbers. The rows hold the direction, not a band: the numbers are a
/// proposal Jack judges in play (Q10, what counts as noticeable, stays open until tuning). Each probe writes its two
/// numbers to the test output, which is the probe table in the PR.
/// </summary>
public sealed class CrystalRinkTests(ITestOutputHelper output)
{
    static readonly ContentCatalog Game = Shipped.Content;
    static Park Harbor => Game.MustPark(ParkId.Harbor);
    static Park Crystal => Game.MustPark(ParkId.Crystal);

    /// <summary>
    /// The intent is data: the ice outfield and apron are the <c>ice</c> row, the fence is glass from pole to pole on the
    /// park's own three posts, and the air is its own. The infield and the track stay dirt.
    /// </summary>
    [Fact]
    public void F9A_CrystalNamesIceGlassAndItsAir()
    {
        var zones = GroundZones.Of(Crystal, Game.Rules);
        Assert.Equal((Ground.Ice, Ground.Ice, Ground.Dirt, Ground.Dirt), (zones.Outfield, zones.FoulApron, zones.InfieldDirt, zones.WarningTrack));
        Assert.NotNull(Crystal.Fence);
        Assert.All(Crystal.Fence!.Points.SkipLast(1), p => Assert.Equal(WallMaterial.Glass, p.Material));
        Assert.All(Crystal.Fence.Points, p => Assert.Equal((1.0, 8.0), (p.FenceFrac, p.HeightFt)));
        // On the posts at every point, and within a chord's sag (under a foot) between them: the same field, glass boards.
        var arc = Crystal with { Fence = null };
        for (var b = -45.0; b <= 45.0; b += 0.5)
        {
            var d = AtBatResolver.FenceAt(arc, b) - AtBatResolver.FenceAt(Crystal, b);
            Assert.InRange(d, -1e-9, 1.0);
        }
        var fence = FieldBounds.Of(Crystal, Rules.Default).Segments.Where(s => s.Kind == FieldBounds.WallKind.FairFence).ToList();
        Assert.NotEmpty(fence);
        Assert.All(fence, s => Assert.Equal(WallMaterial.Glass, WallMaterial.OfSegment(s)));
        Assert.Equal(1.06, Crystal.Environment!.DragMul);
    }

    /// <summary><c>SF-11</c> on the shipped rows: the same grounder off the bat rolls farther on Crystal's ice than on grass.</summary>
    [Fact]
    public void SF11_TheSameGrounderRunsFartherOnTheIce()
    {
        var rules = Game.Rules;
        var iced = Harbor with { Zones = new ParkZones(Outfield: Ground.Ice) };
        foreach (var exit in new[] { 70.0, 80, 95 })
        {
            var grass = BallFlight.Trajectory(exit, -2, 6, Harbor, rules)[^1].Dist;
            var ice = BallFlight.Trajectory(exit, -2, 6, iced, rules)[^1].Dist;
            output.WriteLine($"grounder {exit} mph at -2°: stops at {grass:0.0} ft on grass, {ice:0.0} ft on ice");
            Assert.True(ice > grass + 5, $"{exit}: ice {ice:0.0} vs grass {grass:0.0}");
        }
    }

    /// <summary>
    /// <c>SF-12</c>, the two-row half (map finding 44): the same roll into the wall caroms off Crystal's glass faster than off
    /// Harbor's pad — the first second material, so two spans now carom off two different rows.
    /// </summary>
    [Fact]
    public void SF12_TheSameRollCaromsLivelierOffTheGlass()
    {
        var rules = Game.Rules;
        var glassed = Harbor with
        {
            Fence = new ParkFence(Enumerable.Range(0, 13).Select(k =>
                new FencePoint(-45 + 7.5 * k, 1.0, Harbor.FenceHeightFt, k < 12 ? WallMaterial.Glass : null)).ToList())
        };
        double Back(Park park)
        {
            var fence = AtBatResolver.FenceAt(park, 0);
            var path = BallFlight.Continue([], 0, -3, 0, fence - 6, 14, 0, 40, 0, 90, park, rules);
            Assert.Contains(path, p => p.Event == SampleEvent.Wall);
            // How far back off the wall the ball ends up.
            return fence - Math.Sqrt(path[^1].X * path[^1].X + path[^1].Z * path[^1].Z);
        }
        var pad = Back(Harbor);
        var glass = Back(glassed);
        output.WriteLine($"roll into the wall at 40 ft/s: comes back {pad:0.0} ft off the pad, {glass:0.0} ft off the glass");
        Assert.True(glass > pad + 1, $"glass {glass:0.0} vs pad {pad:0.0}");
    }

    /// <summary><c>SF-10</c> on the shipped air: the same fly carries a little less in Crystal's cold air than in Harbor's.</summary>
    [Fact]
    public void SF10_TheSameFlyCarriesALittleLessInTheColdAir()
    {
        var cold = Game.Rules.AtPark(Crystal);
        var open = Harbor with { LeftFenceFt = 999, CenterFenceFt = 999, RightFenceFt = 999 };
        foreach (var (exit, launch) in new[] { (95.0, 28.0), (105.0, 32.0) })
        {
            var at = BallFlight.LandingPoint(BallFlight.Trajectory(exit, launch, 0, open, Game.Rules));
            var cr = BallFlight.LandingPoint(BallFlight.Trajectory(exit, launch, 0, open, cold));
            var harbor = Math.Sqrt(at.X * at.X + at.Z * at.Z);
            var crystal = Math.Sqrt(cr.X * cr.X + cr.Z * cr.Z);
            output.WriteLine($"fly {exit} mph at {launch}°: lands {harbor:0.0} ft in Harbor's air, {crystal:0.0} ft in Crystal's");
            Assert.InRange(harbor - crystal, 0.5, 15);
        }
    }

    /// <summary>
    /// <c>SF-13</c> on the shipped row, as numbers: the ice multiplies the time a body takes to start, stop and turn and the
    /// length it slides and overruns, never its speed. With the response law's 0.20 s ramp and 0.10 s brake, that is the
    /// probe table's body row; <c>BodyGroundTests</c> holds the behavior on a fixture with larger multipliers than these.
    /// </summary>
    [Fact]
    public void SF13_TheIceMakesABodySlowerToStartStopAndTurnNeverSlower()
    {
        var chase = Game.Rules.Fielding.Chase;
        var grass = Game.Rules.Grounds.Of(Ground.Grass).Body;
        var ice = Game.Rules.Grounds.Of(Ground.Ice).Body;
        output.WriteLine($"rest to speed {chase.AccelSec * grass.StartMul:0.00} s on grass, {chase.AccelSec * ice.StartMul:0.00} s on ice; "
            + $"stop {chase.BrakeSec * grass.BrakeMul:0.00} / {chase.BrakeSec * ice.BrakeMul:0.00} s; "
            + $"turn {chase.AccelSec * grass.CutMul:0.00} / {chase.AccelSec * ice.CutMul:0.00} s; "
            + $"slide x{ice.SlideMul}, overrun x{ice.OverrunMul}");
        Assert.True(chase.AccelSec > 0 && chase.BrakeSec > 0, "the response law is on, so the ice reaches the body");
        Assert.True(ice.StartMul > grass.StartMul && ice.BrakeMul > grass.BrakeMul && ice.CutMul > grass.CutMul);
        Assert.True(ice.SlideMul > 1 && ice.OverrunMul > 1);
    }
}
