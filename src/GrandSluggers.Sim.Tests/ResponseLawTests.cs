using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-2 slice 2 (#718, F693-02-carry-movement-response): the response law. A body builds from rest to its
/// rated speed over 0.20 s and brakes to rest over 0.10 s; a reversal is the brake and then the ramp. A
/// table at 0 / 0 steps instantly — the same code path, not a product by one.
/// </summary>
public sealed class ResponseLawTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;

    [Fact]
    [Trait("Kind", "Balance")]
    public void TheStepRampsAndBrakes()
    {
        Assert.Equal((0.20, 0.10), (Game.Rules.Fielding.Chase.AccelSec, Game.Rules.Fielding.Chase.BrakeSec));
    }

    /// <summary>
    /// The planner charges half the ramp to a route: its travel time carries it, and a body that could just reach a
    /// sample at speed cannot once it has to get to speed first. At a ramp of 0 the arithmetic is the old one exactly.
    /// </summary>
    [Fact]
    public void ARouteChargesHalfTheRampToItsTravelAndItsReach()
    {
        var flat = new FieldingPursuit.Route(0, 100, 3.0, 54, 18, 3.0, true, false);
        Assert.Equal(3.0, flat.TravelTimeSec, 9);
        Assert.Equal(0, flat.MissFt, 9);

        var ramped = new FieldingPursuit.Route(0, 100, 3.0, 54, 18, 3.0, true, false, RampSec: 0.10);
        Assert.Equal(3.10, ramped.TravelTimeSec, 9);
        Assert.Equal(1.8, ramped.MissFt, 9);   // 54 − 18 × (3.0 − 0.10)
    }

    /// <summary>
    /// 1B covering first on a grounder to short. His first frames are the build-up — a fifth of the rated speed
    /// after two frames, the rated speed by the end of the ramp — and he settles at the bag rather than stopping
    /// dead: inside the cover stop plus the brake's overshoot, and at rest.
    /// </summary>
    [Fact]
    public void ACoverBodyBuildsToSpeedOverTheRampAndSettlesAtTheBag()
    {
        var home = Game.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = Game.Team("Offense", "zig", "boom", "jester", "grit", "soot", "nugget", "pip", "gull", "marlow");
        var match = Match.Exhibition(Game, home, away, 3, 1, parkId: ParkIds.Harbor);
        var hit = FlightFixtures.Hit(match.Park, 85, -12, -18, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        var lace = Game.Must("lace");
        var rated = FieldingResolver.ChaseSpeedFt(lace, false, match.Rules);
        var first = Diamond.Bag(1);

        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        var track = new List<(double X, double Z)> { live.Fielders["1B"] };
        for (var i = 0; i < 90; i++)
        {
            live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu));
            track.Add(live.Fielders["1B"]);
        }
        double Speed(int frame) => Diamond.Dist(track[frame].X, track[frame].Z, track[frame + 1].X, track[frame + 1].Z) / Frame;

        // The build-up: a linear ramp over accelSec, so two frames in the body is at two twelfths of its rated speed.
        var ramp = match.Rules.Fielding.Chase.AccelSec;
        Assert.InRange(Speed(1), rated * (2 * Frame / ramp) * 0.7, rated * (2 * Frame / ramp) * 1.3);
        Assert.True(Speed(1) < Speed(4) && Speed(4) < Speed(8), "the speed builds frame over frame");
        var atSpeed = (int)Math.Ceiling(ramp / Frame) + 2;
        Assert.InRange(Speed(atSpeed), rated * 0.97, rated * 1.03);

        // The stop: within the cover stop radius plus the brake's overshoot, and at rest by the end.
        var stop = match.Rules.Fielding.Cover.StopFt;
        var brake = match.Rules.Fielding.Chase.BrakeSec;
        var overshoot = rated * brake / 2;   // v² / (2 a) with a = v / brakeSec
        var rest = track[^1];
        Assert.InRange(Diamond.Dist(rest.X, rest.Z, first.X, first.Z), 0, stop + overshoot + 0.05);
        Assert.InRange(Speed(track.Count - 2), 0, 0.5);
    }

    /// <summary>
    /// The body the ring leaves on a hand-off (§8.9): SS runs at a liner up the middle, the ring goes to CF, and SS keeps its
    /// velocity for exactly <c>chase.handoffCoastSec</c> and then brakes over <c>chase.brakeSec</c>. No frame is faster than the
    /// coast (the idle brake does not step a coasting body), the brake begins on the frame after the coast's last step (no
    /// standing frame between them), and at an uneven frame time the coast's last frame is part coast, part brake.
    /// </summary>
    [Theory]
    [InlineData(1.0 / 60.0)]
    [InlineData(0.021)]
    public void TheBodyTheRingLeavesCoastsThenBrakesWithNoFasterFrameAndNoStandingFrame(double dt)
    {
        // A uniform faster fixture clock recreates the one-frame handoff race; production keeps 1.65.
        using var fixture = new ContentFixture();
        var flightFile = fixture.Path("rules/flight.json");
        File.WriteAllText(flightFile, File.ReadAllText(flightFile).Replace("\"timeScale\": 1.65", "\"timeScale\": 1.0"));
        var content = ContentCatalog.Load(new DataRoot(fixture.Root));
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = content.Team("Offense", "zig", "boom", "jester", "grit", "soot", "nugget", "pip", "gull", "marlow");
        var match = Match.Exhibition(content, home, away, 3, 1, parkId: ParkIds.Harbor);
        var hit = FlightFixtures.Hit(match.Park, 90, 10, -8, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        var chase = match.Rules.Fielding.Chase;
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        Assert.Equal("SS", live.GlovePos);
        var track = new List<(string Glove, (double X, double Z) At)> { (live.GlovePos, live.Fielders["SS"]) };
        for (var i = 0; i < 200 && live.Active; i++)
        {
            live.Apply(LivePlayCommand.Tick(dt, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu));
            if (live.Active) track.Add((live.GlovePos, live.Fielders["SS"]));
        }
        double Speed(int frame) => Diamond.Dist(track[frame - 1].At.X, track[frame - 1].At.Z, track[frame].At.X, track[frame].At.Z) / dt;

        var h = track.FindIndex(f => f.Glove != "SS");
        Assert.True(h > 2 && track[h].Glove == "CF", "the ring left SS for CF");
        var v = Speed(h - 1);
        Assert.True(v > 10, $"SS was running when the ring left ({v:0.0} ft/s)");
        Assert.Equal(0, Speed(h), 6);   // the hand-off frame: nobody steps the body the ring left (§8.9, as shipped)

        var window = (int)Math.Ceiling((chase.HandoffCoastSec + chase.BrakeSec) / dt) + 8;
        var speeds = Enumerable.Range(h + 1, window).Select(Speed).ToList();
        Assert.All(speeds, s => Assert.True(s <= v * 1.001, $"a frame at {s:0.0} ft/s inside a {v:0.0} ft/s coast"));
        var coastFrames = (int)Math.Floor(chase.HandoffCoastSec / dt + 1e-9);
        Assert.All(speeds.Take(coastFrames), s => Assert.Equal(v, s, 3));
        for (var i = 1; i < speeds.Count; i++)
            Assert.True(speeds[i] <= speeds[i - 1] + 1e-6, $"the body sped up after the coast ({speeds[i - 1]:0.0} → {speeds[i]:0.0} ft/s)");
        var rest = speeds.FindIndex(s => s < 1e-6);
        Assert.InRange(rest, coastFrames + 1, window - 1);
        Assert.True(speeds[coastFrames] > 0 && speeds[coastFrames] < v, "the brake begins on the frame after the coast's last full step");

        // The whole slide: the coast, plus the brake's v² / (2 a) less at most the one frame a stepped brake gives up.
        var slid = Diamond.Dist(track[h].At.X, track[h].At.Z, track[h + window].At.X, track[h + window].At.Z);
        var ideal = v * chase.HandoffCoastSec + v * chase.BrakeSec / 2;
        Assert.InRange(slid, ideal - v * dt, ideal + 0.01);
    }

    /// <summary>At 0 / 0 the cover body is at its rated speed on its very first step: the law's code path is not taken.</summary>
    [Fact]
    [Trait("Kind", "Balance")]
    public void AtZeroZeroTheFirstStepIsAlreadyAtSpeed()
    {
        var instant = InstantStep();
        Assert.Equal((0.0, 0.0), (instant.Rules.Fielding.Chase.AccelSec, instant.Rules.Fielding.Chase.BrakeSec));
        var home = instant.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = instant.Team("Offense", "zig", "boom", "jester", "grit", "soot", "nugget", "pip", "gull", "marlow");
        var match = Match.Exhibition(instant, home, away, 3, 1, parkId: ParkIds.Harbor);
        var rated = FieldingResolver.CoverSpeedFt(instant.Must("lace"), match.Rules);
        var hit = FlightFixtures.Hit(match.Park, 85, -12, -18, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        var before = live.Fielders["1B"];
        var moved = false;
        for (var i = 0; i < 40 && !moved; i++)
        {
            live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu));
            var at = live.Fielders["1B"];
            if (Diamond.Dist(at.X, at.Z, before.X, before.Z) > 1e-6)
            {
                moved = true;
                Assert.InRange(Diamond.Dist(at.X, at.Z, before.X, before.Z) / Frame, rated * 0.99, rated * 1.01);
            }
            before = at;
        }
        Assert.True(moved);
    }

    /// <summary>The game's catalog with <c>chase.accelSec</c> and <c>chase.brakeSec</c> at 0, laid over the shipped root.</summary>
    static ContentCatalog InstantStep()
    {
        var dir = Path.Combine(Path.GetTempPath(), "grand-sluggers-response-" + Guid.NewGuid().ToString("N"));
        try
        {
            var text = File.ReadAllText(Path.Combine(Game.Root.Shipped, "rules", "fielding.json"));
            foreach (var (from, to) in new[] { ("\"accelSec\": 0.20", "\"accelSec\": 0"), ("\"brakeSec\": 0.10", "\"brakeSec\": 0") })
            {
                Assert.Contains(from, text);
                text = text.Replace(from, to);
            }
            Directory.CreateDirectory(Path.Combine(dir, "rules"));
            File.WriteAllText(Path.Combine(dir, "rules", "fielding.json"), text);
            return ContentCatalog.Load(new DataRoot(Game.Root.Shipped, dir));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
