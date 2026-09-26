using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Zig's two (spec §13): Loop-the-Loop's vertical loop and Spinning Top's first-hop stall. The pitch keeps its crossing
/// and its arrival instant, so the timing window is the ordinary one; the stall is the ball standing on the ground at its
/// hop, where a glove may take it, then running on at half its speed on the shared ground physics. Nothing is rolled.
/// </summary>
public sealed class ZigAbilityTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;

    [Fact]
    public void ZigCarriesHisOwnTwoInTheirOwnFamilies()
    {
        var zig = Game.Must("zig");
        Assert.Equal("prismball", zig.StarPitch);
        Assert.Equal("shell-swing", zig.StarSwing);
        Assert.Equal("Loop-the-Loop", Game.StarSkills.Pitch("prismball")!.Name);
        Assert.Equal("Spinning Top", Game.StarSkills.Swing("shell-swing")!.Name);
        Assert.DoesNotContain(Game.Characters.Values, c => c.Id != "zig" && (c.StarPitch == "prismball" || c.StarSwing == "shell-swing"));
        // The late break and the ghosts are gone: the row is a loop and nothing else.
        var pitch = Game.StarSkills.Pitch("prismball")!;
        Assert.False(pitch.LateBreak);
        Assert.Null(pitch.Leap);
        Assert.Null(pitch.Hitch);
        Assert.Null(pitch.Twin);
        Assert.NotNull(pitch.Loop);
    }

    // ---------------------------------------------------------------------------------
    // Loop-the-Loop
    // ---------------------------------------------------------------------------------

    /// <summary>S-214: the crossing, the arrival instant and the timing window are the ordinary pitch's.</summary>
    [Theory]
    [InlineData("fastball", 0.0, 0.0)]
    [InlineData("changeup", 0.2, -0.3)]
    [InlineData("curveball", -0.4, 0.4)]
    public void S214_TheLoopCrossesWhereAndWhenThePlainPitchDoes(string type, double aimX, double aimY)
    {
        var rules = Game.Rules;
        var star = new PitchCommand(type, 0.2, true, aimX, aimY);
        var plain = star with { Star = false };
        Assert.Equal(PitchFlight.Crossing(plain, rules), PitchFlight.Crossing(star, rules, "prismball"));
        Assert.Equal(PitchFlight.Point(plain, 1, rules), PitchFlight.Point(star, 1, rules, "prismball", skills: Game.StarSkills));
        // The speed is the ordinary pitch's, so the plate time is too; the window is judged against that one clock.
        Assert.Equal(1.0, StarSkills.PitchSpeedMul("prismball", Game.StarSkills));
        Assert.Equal(AtBatResolver.ContactWindowFrames(null, Game.Parks[ParkId.Harbor], false, rules, Game.StarSkills),
            AtBatResolver.ContactWindowFrames("prismball", Game.Parks[ParkId.Harbor], false, rules, Game.StarSkills));
        // Before the loop the ball is the plain ball; after it the ball is back on the plain path, ahead of its old clock.
        var loop = Game.StarSkills.Pitch("prismball")!.Loop!;
        for (var u = 0.0; u <= 1.0001; u += 0.01)
        {
            var at = PitchFlight.Point(star, u, rules, "prismball", skills: Game.StarSkills);
            if (u <= loop.At) Assert.Equal(PitchFlight.Point(plain, u, rules), at);
            else if (u >= loop.Exit) Assert.Equal(PitchFlight.Point(plain, loop.Progress(u), rules), at);
        }
    }

    /// <summary>
    /// S-215: one full loop, 4 ft across, standing on the path at the same point of every flight — it leaves forward toward
    /// the plate, turns over 4 ft above its foot and comes back down to where it started; X never moves while it loops.
    /// </summary>
    [Theory]
    [InlineData("fastball", 0.0, 0.0, 0.0)]
    [InlineData("changeup", 0.3, -0.5, 0.6)]
    public void S215_TheLoopIsOneCircleStandingOnThePathAtTheSamePoint(string type, double aimX, double aimY, double rubberX)
    {
        var rules = Game.Rules;
        var loop = Game.StarSkills.Pitch("prismball")!.Loop!;
        Assert.Equal(4, loop.DiameterFt);
        var star = new PitchCommand(type, 0, true, aimX, aimY, RubberX: rubberX);
        var plain = star with { Star = false };
        var foot = PitchFlight.Point(plain, loop.At, rules);
        var toward = Math.Sign(StrikeZoneGeometry.PlateZ - PitchFlight.Release(rules, rubberX).Z);
        var top = double.MinValue;
        for (var i = 1; i < 100; i++)
        {
            var u = loop.At + loop.Span * i / 100.0;
            var p = PitchFlight.Point(star, u, rules, "prismball", skills: Game.StarSkills);
            Assert.Equal(foot.X, p.X, 9);
            // On the circle of radius 2 whose centre is 2 ft above the foot, in the flight's vertical plane.
            var dz = (p.Z - foot.Z) * toward;
            var dy = p.Y - (foot.Y + loop.DiameterFt / 2);
            Assert.Equal(loop.DiameterFt / 2, Math.Sqrt(dz * dz + dy * dy), 9);
            if (i == 5) Assert.True(dz > 0, "the loop leaves forward, toward the plate, like a coaster up its loop");
            top = Math.Max(top, p.Y - foot.Y);
        }
        Assert.Equal(loop.DiameterFt, top, 3);
        // The loop's foot is the plain pitch at the same share of every flight: always the same point.
        Assert.Equal(foot, PitchFlight.Point(star, loop.At, rules, "prismball", skills: Game.StarSkills));
        Assert.Equal(foot, PitchFlight.Point(star, loop.Exit, rules, "prismball", skills: Game.StarSkills));
    }

    [Fact]
    public void TheLoopIsOverWellInsideTwoSecondsAndNeverRunsBackwardsAlongThePath()
    {
        var rules = Game.Rules;
        var loop = Game.StarSkills.Pitch("prismball")!.Loop!;
        var slowest = PitchFlight.AirSeconds(rules.Pitching.Flight.MinMph, rules);
        Assert.True(loop.Exit * slowest < 2, $"the loop ends {loop.Exit * slowest:F2} s after the release");
        var last = 0.0;
        for (var u = 0.0; u <= 1.0001; u += 0.005)
        {
            var progress = loop.Progress(u);
            Assert.True(progress >= last - 1e-12);
            last = progress;
        }
        Assert.Equal(1, loop.Progress(1));
        Assert.Equal((0.0, 0.0), loop.Offset(loop.At));
        Assert.Equal((0.0, 0.0), loop.Offset(loop.Exit));
    }

    // ---------------------------------------------------------------------------------
    // Spinning Top
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// S-216: the grounder stands at its first hop for 0.8 s — the same point every frame of the stall — then runs on
    /// from there at half the speed it left the hop with; the plain swing's same ball never stops.
    /// </summary>
    [Theory]
    [InlineData(18)]
    [InlineData(28)]
    [InlineData(-30)]
    public void S216_TheSpinningTopStandsAtItsFirstHopThenRollsOnAtHalfSpeed(double spray)
    {
        var swing = Game.StarSkills.Swing("shell-swing")!;
        Assert.Equal(0.8, swing.FirstHopStallSec);
        Assert.Equal(0.5, swing.FirstHopStallSpeedMul);
        var top = Run(spray, "shell-swing");
        var plain = Run(spray, null);
        Assert.Empty(plain.Kicks);
        var hop = Assert.Single(top.Kicks);
        Assert.Equal("shell-swing", hop.SwingId);
        Assert.Equal(0, hop.TurnDeg);
        Assert.Equal(swing.FirstHopStallSec, hop.StallSec);
        // The path the play kept: at rest on the ground at the hop from the hop to the end of the stall.
        for (var t = hop.T; t < hop.T + hop.StallSec - 1e-6; t += 0.05)
        {
            var at = BallFlight.PointAt(top.Path!, t, Game.Rules);
            Assert.Equal(hop.X, at.X, 9);
            Assert.Equal(0, at.Y, 9);
            Assert.Equal(hop.Z, at.Z, 9);
        }
        // Then it runs on from the same spot, at half the speed the unstalled ball left the hop with.
        var after = Speed(top.Path!, hop.T + hop.StallSec);
        var before = Speed(plain.Path!, hop.T);
        Assert.True(after > 0, "the ball rolls on after the stall");
        Assert.Equal(swing.FirstHopStallSpeedMul, after / before, 1);
        Assert.True(BallFlight.PointAt(plain.Path!, hop.T + 0.4, Game.Rules) != (hop.X, 0.0, hop.Z), "the plain ball never stops");
        Assert.True(hop.T + hop.StallSec < 2, $"the stall ends {hop.T + hop.StallSec:F2} s after contact");
        Assert.NotNull(top.Play);
    }

    /// <summary>
    /// S-217: a comebacker the pitcher reaches while it spins: the glove takes the standing ball at the hop point inside
    /// the stall, and the play is decided by that glove, the throw and the bag, like any grounder.
    /// </summary>
    [Fact]
    public void S217_AGloveThatReachesTheSpinningBallTakesItWhereItStands()
    {
        var run = Run(-2, "shell-swing");
        var hop = Assert.Single(run.Kicks);
        Assert.NotNull(run.TakenAt);
        var (t, x, z) = run.TakenAt!.Value;
        Assert.InRange(t, hop.T, hop.T + hop.StallSec);
        Assert.Equal(hop.X, x, 6);
        Assert.Equal(hop.Z, z, 6);
        Assert.NotNull(run.Play);
    }

    /// <summary>The two-second rule: a ball whose first hop comes late stands only for what is left of two seconds, or not at all.</summary>
    [Theory]
    [InlineData(88, 4, 18, false)]
    [InlineData(100, 8, 0, true)]
    public void ALateHopStandsOnlyInsideTheTwoSeconds(double exit, double launch, double spray, bool pastTwo)
    {
        var run = Run(spray, "shell-swing", exit, launch);
        var hop = Assert.Single(run.Kicks);
        var two = StarSkills.SpectacleSeconds("shell-swing");
        Assert.True(hop.T + Game.StarSkills.Swing("shell-swing")!.FirstHopStallSec > two, $"the fixture hops late, at {hop.T:F2} s");
        Assert.Equal(pastTwo ? 0 : two - hop.T, hop.StallSec, 9);
    }

    static double Speed(IReadOnlyList<Sample> path, double t)
    {
        const double step = 1.0 / 60;
        var a = BallFlight.PointAt(path, t + 1e-6, Game.Rules);
        var b = BallFlight.PointAt(path, t + step, Game.Rules);
        return Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Z - a.Z) * (b.Z - a.Z)) / (step - 1e-6);
    }

    /// <summary>A hard grounder off Zig's bat with the named star swing (or none), on CPU gloves at Harbor.</summary>
    static (List<FirstHopKicked> Kicks, IReadOnlyList<Sample>? Path, (double T, double X, double Z)? TakenAt, PlayEvent? Play) Run(
        double spray, string? swing, double exit = 88, double launch = 0)
    {
        var home = Game.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "soot", "vine", "moss", "hex");
        var away = Game.Team("Offense", "zig", "boom", "cinder", "grit", "rio", "nugget", "nico", "gull", "marlow");
        var match = Match.Exhibition(Game, home, away, 3, 1, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Hit(match.Park, exit, launch, spray, rules: match.Rules) with { StarSwingUsed = swing };
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0,
            LivePlayCommandSource.Cpu)).Snapshot.Active);
        var kicks = new List<FirstHopKicked>();
        IReadOnlyList<Sample>? path = null;
        (double T, double X, double Z)? taken = null;
        (double T, double X, double Z) ball = (0, 0, 0);
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 12 && play is null; i++)
        {
            play = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
            if (play is not null) break;   // the completing frame resets the field
            if (live.HoldsBall && taken is null) taken = ball;
            ball = (live.ElapsedSeconds, live.BallX, live.BallZ);
            path = live.Path;
            kicks.AddRange(live.Facts.OfType<FirstHopKicked>());
        }
        return (kicks, path, taken, play);
    }

    // ---------------------------------------------------------------------------------
    // The rows are read strictly
    // ---------------------------------------------------------------------------------

    [Fact]
    public void AWildLoopOrAStallOnAPitchOrALoopOnASwingOrAStallOffAFlyIsRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["prismball"]!["loop"]!["diameterFt"] = 12;
            json["pitches"]!["fastball"]!["firstHopStallSec"] = 0.5;
            json["swings"]!["line"]!["loop"] = new JsonObject { ["at"] = 0.3, ["span"] = 0.2, ["diameterFt"] = 4 };
            json["swings"]!["shell-swing"]!["firstHopStallSec"] = 3;
            json["swings"]!["fly"]!["firstHopStallSec"] = 0.5;
            json["swings"]!["fly"]!["firstHopStallSpeedMul"] = 0.5;
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("star pitch 'prismball' loop needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'fastball' cannot carry a first-hop stall", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'line' cannot carry a loop", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'shell-swing' firstHopStallSec must be", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'fly' stalls on its first hop, so it must name a grounder's launchDeg", StringComparison.Ordinal));
    }
}
