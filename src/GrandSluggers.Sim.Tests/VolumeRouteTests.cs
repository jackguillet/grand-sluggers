using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The route cost of a status volume (§14; FD-14, <c>SF-26</c>; F4-g). A body walking to a goal goes around a volume when
/// going around costs less time than the slow it would pay going through, and takes the slow when it does not. The CPU and
/// the assistance read the same discs, <c>slowSec</c> and <c>frozenMul</c> the touch test does; the route draws nothing.
///
/// <para>
/// The live rows add one volume to Harbor as a <see cref="Park"/> record, the way <see cref="StatusVolumeTests"/> does, so
/// the volume sits where a body will certainly run.
///
/// </para>
/// </summary>
public sealed class VolumeRouteTests
{
    static readonly ContentCatalog Catalog = Shipped.Content;
    const double Frame = 1.0 / 60.0;
    const double SlowSec = 3.0;
    const double Mul = 0.45;
    const double Clear = 2.0;
    const double Speed = 25.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    static StatusVolume Disc(double x, double z, double r) => new(0, HazardType.FreezeVolume, x, z, r, SlowSec);

    // ---------------------------------------------------------------------------------
    // The cost, on its own
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-26</c>: the goal comes back exactly (the same doubles) when no volume is in the way: no volume, a volume off the
    /// line, a volume behind the body, a goal inside the kept-off disc (the ball lies in it), and a body already inside it.
    /// </summary>
    [Fact]
    public void SF26_TheRouteIsTheGoalWhenNoVolumeIsInTheWay()
    {
        var at = (X: 0.0, Z: 0.0);
        var goal = (X: 3.1, Z: 97.3);
        Assert.Equal(goal, VolumeRoute.Waypoint(at, goal, [], Speed, Mul, Clear));
        Assert.Equal(goal, VolumeRoute.Waypoint(at, goal, [Disc(40, 50, 8)], Speed, Mul, Clear));
        Assert.Equal(goal, VolumeRoute.Waypoint(at, goal, [Disc(0, -30, 8)], Speed, Mul, Clear));
        Assert.Equal(goal, VolumeRoute.Waypoint(at, goal, [Disc(3, 88, 8)], Speed, Mul, Clear));
        Assert.Equal(goal, VolumeRoute.Waypoint(at, goal, [Disc(0, 5, 8)], Speed, Mul, Clear));
        Assert.Null(VolumeRoute.Plan(at, goal, [Disc(40, 50, 8)], Speed, Mul, Clear).Volume);
    }

    /// <summary>
    /// <c>SF-26</c>: the two costs, worked by hand. A body 11 ft from the centre of an 8 ft volume (10 ft kept off) going to a
    /// point 11 ft past it. Through: in 1 ft, 20 ft inside, 21 ft to the goal, all of it slowed (3 s at 0.45 × 25 ft/s is 33.75
    /// ft): 21 × (1 / 0.45 − 1) / 25 s. Around: two 4.58 ft tangents and the arc between them, 32.0 ft against 22, so 0.40 s.
    /// At 0.45 it goes around, on the side the goal leans to; at 0.9 the slow is cheap and it goes through.
    /// </summary>
    [Fact]
    public void SF26_AroundWhenAroundIsCheaperThroughWhenNot()
    {
        var disc = Disc(0, 0, 8);
        var at = (X: 0.0, Z: -11.0);
        var plan = VolumeRoute.Plan(at, (0.5, 11), [disc], Speed, Mul, Clear);
        Assert.Same(disc, plan.Volume);
        Assert.Equal(21.0 * (1 / Mul - 1) / Speed, plan.ThroughSec, 1);
        var tangent = Math.Sqrt(11 * 11 - 10 * 10);
        var theta = Math.Atan2(11 * 0.5, -11 * 11.0); // the angle at the centre between the body and the goal
        var arc = theta - 2 * Math.Acos(10.0 / 11);
        var around = 2 * tangent + 10 * arc;
        Assert.Equal((around - Math.Sqrt(0.25 + 22 * 22)) / Speed, plan.AroundSec, 3);
        Assert.True(plan.Around);
        Assert.True(plan.Waypoint.X > 0, "the goal leans east, so the route goes east");
        Assert.True(VolumeRoute.Plan(at, (-0.5, 11), [disc], Speed, Mul, Clear).Waypoint.X < 0, "and west for a goal leaning west");
        // The heading is the tangent: the waypoint's line from the body never comes inside the kept-off disc.
        var (wx, wz) = plan.Waypoint;
        var len = Math.Sqrt((wx - at.X) * (wx - at.X) + (wz - at.Z) * (wz - at.Z));
        var closest = Math.Abs((wx - at.X) * (disc.Z - at.Z) - (wz - at.Z) * (disc.X - at.X)) / len;
        Assert.Equal(10.0, closest, 6);

        var cheap = VolumeRoute.Plan(at, (0.5, 11), [disc], Speed, 0.9, Clear);
        Assert.False(cheap.Around);
        Assert.True(cheap.ThroughSec < cheap.AroundSec);
        Assert.Equal((0.5, 11.0), VolumeRoute.Waypoint(at, (0.5, 11), [disc], Speed, 0.9, Clear));
    }

    /// <summary>
    /// <c>SF-26</c>: a body on the rim of the kept-off disc heads along it (the tangent angle is a right angle), so walking
    /// the route frame by frame at 25 ft/s keeps it outside the volume all the way to the goal.
    /// </summary>
    [Fact]
    public void SF26_WalkingTheRouteNeverEntersTheVolume()
    {
        var disc = Disc(0, 50, 8);
        var at = (X: 0.3, Z: 0.0);
        var goal = (X: 0.0, Z: 100.0);
        for (var i = 0; i < 60 * 10; i++)
        {
            var w = VolumeRoute.Waypoint(at, goal, [disc], Speed, Mul, Clear);
            at = FieldingResolver.StepToward(at.X, at.Z, w.X, w.Z, Speed, Frame, rules: Rules.Default);
            Assert.False(disc.Contains(at.X, at.Z), $"inside at frame {i}: ({at.X:0.00}, {at.Z:0.00})");
            if (Diamond.Dist(at.X, at.Z, goal.X, goal.Z) < 0.5) break;
        }
        Assert.True(Diamond.Dist(at.X, at.Z, goal.X, goal.Z) < 0.5, "it gets there");
    }

    // ---------------------------------------------------------------------------------
    // Live: the CPU seat and the human seat's assistance
    // ---------------------------------------------------------------------------------

    sealed record Played(FieldingPreview Preview, PlayEvent? Done, IReadOnlyList<BodySlowed> Touches, List<(double X, double Z)> Chaser);

    static Played Play(Match match, AtBatResult hit, string chaser, LiveSeats? seats = null)
    {
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        var human = seats is not null;
        var source = human ? LivePlayCommandSource.Human : LivePlayCommandSource.Cpu;
        var field = human ? null : match.ResolveFielding(hit, preview);
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field,
            seats ?? LiveSeats.CpuOnly, 0, source)).Snapshot.Active);
        var path = new List<(double, double)>();
        PlayEvent? done = null;
        for (var i = 0; i < 60 * 20 && done is null; i++)
        {
            done = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, source)).CompletedPlay;
            if (done is not null) break;
            path.Add(chaser == live.GlovePos ? (live.GloveX, live.GloveZ) : live.Fielders[chaser]);
        }
        return new Played(preview, done, live.SlowsThisPlay.ToArray(), path);
    }

    /// <summary>
    /// <c>SF-26</c>, the CPU seat and the human seat (FD-14: the same outcome from the same positions): the glove that goes
    /// out for a shallow fly to centre has a freeze volume two fifths of the way along his run. Both seats' glove goes
    /// around it — never inside the disc, no touch, nobody slowed — and the play ends the way it ends at plain Harbor. The
    /// human seat's stick is neutral, so the assistance walks the glove, through the same route.
    /// </summary>
    [Fact]
    public void SF26_TheCpuAndTheAssistanceGoAroundAVolumeInTheRunAndMakeThePlayTheyMakeWithoutIt()
    {
        var harbor = Catalog.MustPark(ParkIds.Harbor);
        var carry = Diamond.Positions["CF"].Z - 75;
        Match New(Park park) => new(Catalog, PresetTeams.EmberCourt(Catalog), PresetTeams.SparkAllStars(Catalog), park, seed: 1);
        var hit = FlightFixtures.Landing(harbor, carry, 40, 0);
        var plainPre = New(harbor).PreviewHit(hit);
        var chaser = plainPre.Position;
        var from = Diamond.Positions[chaser];
        var disc = (X: from.X + (plainPre.LandingX - from.X) * 0.4, Z: from.Z + (plainPre.LandingZ - from.Z) * 0.4);
        var park = harbor with { Hazards = [.. harbor.Hazards, new Hazard(HazardType.FreezeVolume, disc.X, disc.Z, 8, null)] };
        var volume = ParkHazards.StatusVolumes(park, rules: Rules.Default).Single();

        foreach (var seats in new LiveSeats?[] { null, HumanGlove })
        {
            var plain = Play(New(harbor), hit, chaser, seats);
            var p = Play(New(park), hit, chaser, seats);
            var seat = seats is null ? "CPU" : "human";
            Assert.Equal(chaser, p.Preview.Position);
            Assert.Empty(p.Touches);
            Assert.All(p.Chaser, at => Assert.False(volume.Contains(at.X, at.Z), $"{seat}: inside at ({at.X:0.0}, {at.Z:0.0})"));
            // He did bend: at plain Harbor his path crosses the disc.
            Assert.Contains(plain.Chaser, at => volume.Contains(at.X, at.Z));
            Assert.NotNull(p.Done);
            Assert.NotNull(plain.Done);
            Assert.Equal((plain.Done!.Kind, plain.Done.Fielder?.Id), (p.Done!.Kind, p.Done.Fielder?.Id));
        }
    }
}
