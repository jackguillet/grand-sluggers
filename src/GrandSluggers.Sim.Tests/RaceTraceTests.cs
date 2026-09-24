using GrandSluggers.Sim;
using Xunit;
namespace GrandSluggers.Sim.Tests;

public sealed class RaceTraceTests
{
    readonly ContentCatalog _content = Shipped.Content;
    const double Frame = 1.0 / 60;

    [Theory]
    [InlineData("cinder", "S31", PlayKind.GroundOut)]
    [InlineData("dart", "S32", PlayKind.Single)]
    public void RoutineRaceRetainsPossessionThrowAndRunnerEvidence(string batter, string id, PlayKind expected)
    {
        var match = Defense(batter);
        var hit = FlightFixtures.Hit(match.Park, 85, -12, -18);
        var trace = Run(match, hit);
        Assert.Equal(expected, trace.Completed!.Kind);
        Assert.Equal(2, trace.SchemaVersion);
        Assert.Equal(batter, trace.Context!.Batter.Id);
        Assert.Equal(ParkIds.Crystal, trace.Context.Park.Id);
        Assert.Equal(64, trace.Context.Identity.Sha256.Length);
        var possession = trace.Marks!.First(m => m.Kind == PlayTraceMarkKind.Possession);
        Assert.True(possession.T > 0);
        if (expected == PlayKind.GroundOut)
        {
            var release = Assert.Single(trace.Marks!, m => m.Kind == PlayTraceMarkKind.ThrowRelease);
            var reception = Assert.Single(trace.Marks!, m => m.Kind == PlayTraceMarkKind.Reception);
            var retired = Assert.Single(trace.Marks!, m => m.Kind == PlayTraceMarkKind.Out);
            Assert.True(possession.T < release.T && release.T < reception.T);
            Assert.True(possession.Geometry!.HoldsUnthrownBall);
            var target = Assert.Single(trace.Marks!, m => m.Kind == PlayTraceMarkKind.ThrowTargetReached);
            Assert.Equal(release.Flight!.ToX, target.Geometry!.BallX, 9);
            Assert.Equal(release.Flight.ToZ, target.Geometry.BallZ, 9);
            Assert.InRange(reception.T - release.T - release.Flight!.DurationSec, -1e-8, Frame + 1e-8);
            Assert.Equal(OutType.ThrowOutAtFirst, retired.OutType);
            Assert.NotNull(retired.Runner);
            Assert.True(retired.PredictedRunnerAt > reception.T);
            Assert.DoesNotContain(trace.Marks!, m => m.Kind == PlayTraceMarkKind.RunnerArrival && m.Runner?.Id == batter);
            Assert.False(retired.Runner!.Live);
            Assert.Equal(RunnerPhase.Out, retired.Runner.Phase);
            // The first sample after release already moves: releaseSec is part of continuous flight, not a stationary hold.
            var moving = trace.Ticks.First(t => t.T > release.T);
            Assert.True(Diamond.Dist(moving.Ball.X, moving.Ball.Z, release.Flight.FromX, release.Flight.FromZ) > 0);
        }
        else
        {
            Assert.DoesNotContain(trace.Marks!, m => m.Kind == PlayTraceMarkKind.ThrowRelease);
            var arrival = trace.Marks!.First(m => m.Kind == PlayTraceMarkKind.RunnerArrival && m.Bag == 1);
            Assert.InRange(arrival.T - arrival.LowerT, 0, Frame + 1e-8);
        }
        Write(id, trace);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HumanDeadStickHasPossessionButNoInventedRelease(bool versus)
    {
        var match = Defense("cinder");
        var seats = new LiveSeats(versus, true, true, versus);
        var trace = Run(match, FlightFixtures.Hit(match.Park, 85, -12, -18), seats);
        Assert.Contains(trace.Marks!, m => m.Kind == PlayTraceMarkKind.Possession);
        Assert.DoesNotContain(trace.Marks!, m => m.Kind == PlayTraceMarkKind.ThrowRelease);
        Assert.Equal(PlayKind.Single, trace.Completed!.Kind);
        Write(versus ? "S33-versus" : "S33-one", trace);
    }

    [Fact]
    public void SerializedCommandsReplayTheSameGeometryAfterTheSameFixtureSetup()
    {
        var first = Defense("cinder");
        var trace = Run(first, FlightFixtures.Hit(first.Park, 85, -12, -18));
        var parsed = PlayTrace.Parse(trace.ToJson());
        var replay = Defense("cinder");
        replay.LivePlay.Recording = true;
        PlayEvent? done = null;
        foreach (var command in parsed.Commands!) done = replay.LivePlay.Apply(command.Input).CompletedPlay ?? done;
        var repeated = replay.LivePlay.TakeTrace(done);
        Assert.Equal(trace.ToJson(), repeated.ToJson());
        var changed = Match.Exhibition(_content, "rio", "ashlord", difficulty: "hard");
        var normal = Match.Exhibition(_content, "rio", "ashlord", difficulty: "normal");
        Assert.NotEqual(PlayTraceIdentity.Capture(normal).Sha256, PlayTraceIdentity.Capture(changed).Sha256);
        Assert.Equal(1, PlayTrace.Parse("{\"ticks\":[]}").SchemaVersion);
        Assert.Throws<System.Text.Json.JsonException>(() => PlayTrace.Parse("{\"schemaVersion\":999,\"ticks\":[]}"));
    }

    [Fact]
    public void FlightReadAndPursuitOverlapWithoutInventingControllerLatency()
    {
        var match = Defense("cinder", ParkIds.Harbor);
        var hit = FlightFixtures.Hit(match.Park, 100, 30, -18);
        var trace = Run(match, hit);
        var first = trace.Ticks[0].Fielders!.First(f => f.Selected);
        Assert.True(first.ReadEligibleAt > 0);
        Assert.Contains(trace.Ticks, t => t.T > 0 && t.T < first.ReadEligibleAt && t.Ball.Y > 0);
        var displaced = trace.Ticks.First(t => t.Fielders!.Any(f => f.Pos == first.Pos && (Math.Abs(f.ObservedVx ?? 0) + Math.Abs(f.ObservedVz ?? 0)) > .01));
        Assert.True(displaced.T + 1e-9 >= first.ReadEligibleAt);
        Assert.True(displaced.Ball.Y > 0);
        Assert.All(trace.Ticks, t => Assert.Equal(9, t.Fielders!.Count));
        Write("harbor-fly", trace);
    }

    [Fact]
    public void FixedInputsAndTacticalFixturesRemainDistinct()
    {
        var match = Defense("cinder", ParkIds.Harbor);
        var fixedHit = FlightFixtures.Hit(match.Park, 84, 8, -12);
        var tactical = FlightFixtures.Hit(match.Park, 85, -12, -18);
        Assert.Equal(84, fixedHit.ExitVeloMph);
        Assert.NotEqual(fixedHit.ExitVeloMph, tactical.ExitVeloMph);
        Write("harbor-fixed-grounder", Run(match, fixedHit));
        Write("harbor-tactical-grounder", Run(Defense("cinder", ParkIds.Harbor), tactical));
    }

    [Fact]
    public void EveryRelayLegSurvivesAndCommandSourceDoesNotChangeBaseball()
    {
        PlayTrace Make(LivePlayCommandSource source)
        {
            var match = Defense("cinder");
            Assert.True(match.StationRunner(1, match.AwayOrder[2]));
            var hit = FlightFixtures.Hit(match.Park, 85, -12, -18);
            return Run(match, hit, new LiveSeats(false, true, true, false), source,
                pad: live => live.HoldsBall && !live.Throwing
                    ? new LivePadInput(KeysBag: live.Throws == 0 ? 2 : 1, SouthDown: true) : LivePadInput.Dead);
        }
        var cpu = Make(LivePlayCommandSource.Cpu);
        var human = Make(LivePlayCommandSource.Human);
        Assert.Equal(cpu.Ticks, human.Ticks, TickComparer.Instance);
        Assert.Equal(cpu.Marks, human.Marks);
        var throws = cpu.Marks!.Where(m => m.Kind == PlayTraceMarkKind.ThrowRelease).ToArray();
        Assert.True(throws.Length >= 2);
        Assert.Equal(Enumerable.Range(1, throws.Length), throws.Select(m => m.Leg!.Value));
        foreach (var leg in throws)
            Assert.Single(cpu.Marks!, m => m.Kind == PlayTraceMarkKind.Reception && m.Leg == leg.Leg);
        Write("relay", cpu);
    }

    [Fact]
    public void InputReadGateDisplacementAndReversalRemainDistinctAcrossPause()
    {
        var match = Defense("cinder");
        var live = match.LivePlay;
        var hit = FlightFixtures.Hit(match.Park, 100, 30, -18);
        live.Recording = true;
        live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, match.PreviewHit(hit), null,
            new LiveSeats(false, true, true, false)));
        for (var i = 0; i < 6; i++) live.Apply(LivePlayCommand.Tick(Frame));
        for (var i = 0; i < 60; i++) live.Apply(LivePlayCommand.Tick(Frame, new LivePadInput(StickX: 1)));
        var x = live.GloveX;
        var t = live.ElapsedSeconds;
        live.Apply(LivePlayCommand.Pause());
        live.Apply(LivePlayCommand.Tick(.2, new LivePadInput(StickX: -1)));
        Assert.Equal(t, live.ElapsedSeconds);
        Assert.Equal(x, live.GloveX);
        live.Apply(LivePlayCommand.Resume());
        for (var i = 0; i < 30; i++) live.Apply(LivePlayCommand.Tick(Frame, new LivePadInput(StickX: -1)));
        var trace = live.TakeTrace();
        var first = trace.Ticks[0].Fielders!.First(f => f.Selected);
        var motion = trace.Ticks.Where(tick => tick.T < t).SelectMany(tick => tick.Fielders!.Where(f => f.Pos == first.Pos));
        Assert.Contains(motion, f => !f.ReadEligible && f.ObservedVx == 0);
        Assert.Contains(motion, f => f.ReadEligible && f.ObservedVx > 0);
        Assert.Contains(trace.Ticks.Where(tick => tick.T > t).SelectMany(tick => tick.Fielders!), f => f.Selected && f.ObservedVx < 0);
        Assert.Contains(trace.Commands!, c => c.Input.Kind == LivePlayCommandKind.Pause);
        Assert.Null(trace.Completed); // a measured movement prefix, not a fabricated finished play
        Write("movement-prefix", trace);
    }

    [Theory]
    [InlineData(2.7, false)]
    [InlineData(100, true)]
    public void UncoveredTargetArrivalIsNotReceptionAndADroppedLobStaysLive(double coverStart, bool drops)
    {
        // Fault fixture only: delay coverage in an isolated copy. The production rule table is untouched.
        var root = Path.Combine(Path.GetTempPath(), "gs702-cover-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var file in Directory.GetFiles(_content.Root.Shipped, "*", SearchOption.AllDirectories))
            {
                var dest = Path.Combine(root, Path.GetRelativePath(_content.Root.Shipped, file));
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(file, dest);
            }
            var path = Path.Combine(root, "rules/fielding.json");
            var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path), documentOptions: new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true })!;
            json["cover"]!["startSec"] = coverStart;
            File.WriteAllText(path, json.ToJsonString());
            var content = ContentCatalog.Load(root);
            var match = Defense("cinder", content: content);
            var hit = FlightFixtures.Hit(match.Park, 85, -12, -18, rules: match.Rules);
            var trace = Run(match, hit, new LiveSeats(false, true, true, false), pad: live =>
                live.HoldsBall && !live.Throwing ? new LivePadInput(KeysBag: 1, SouthDown: true) : LivePadInput.Dead);
            var wait = trace.Marks!.First(m => m.Kind == PlayTraceMarkKind.UncoveredWait);
            var target = trace.Marks!.First(m => m.Kind == PlayTraceMarkKind.ThrowTargetReached);
            Assert.Equal(target.Leg, wait.Leg);
            Assert.False(wait.Geometry!.ReceiverInReach);
            if (drops)
            {
                Assert.DoesNotContain(trace.Marks!, m => m.Kind == PlayTraceMarkKind.Reception && m.Leg == wait.Leg);
                var loose = trace.Marks!.First(m => m.Kind == PlayTraceMarkKind.LooseBall);
                Assert.True(loose.T > wait.T);
            }
            else
            {
                var reception = trace.Marks!.First(m => m.Kind == PlayTraceMarkKind.Reception);
                Assert.True(reception.T > target.T);
                Assert.True(reception.Geometry!.ReceiverInReach);
            }
            Write(drops ? "fault-uncovered-drop" : "fault-uncovered-receive", trace);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    Match Defense(string leadoff, string? park = null, ContentCatalog? content = null)
    {
        content ??= _content;
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var rest = new[] { "jester", "soot", "grit", "nugget", "boom", "marlow", "gull", "pip" }.Where(id => id != leadoff).Take(7).ToArray();
        var away = content.Team("Offense", "zig", new[] { leadoff }.Concat(rest).ToArray());
        var match = Match.Exhibition(content, home, away, innings: 3, seed: 1, parkId: park);
        Assert.True(match.BeginAtBat(Scenario.Paint, Scenario.Swing, out _, out _));
        return match;
    }

    static PlayTrace Run(Match match, AtBatResult hit, LiveSeats? seats = null,
        LivePlayCommandSource source = LivePlayCommandSource.Cpu, FieldingResult? field = null,
        Func<LivePlaySystem, LivePadInput>? pad = null)
    {
        var live = match.LivePlay;
        live.Recording = true;
        live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, match.PreviewHit(hit), field, seats ?? LiveSeats.CpuOnly, source: source));
        PlayEvent? done = null;
        for (var i = 0; i < 60 * 30 && done is null; i++)
            done = live.Apply(LivePlayCommand.Tick(Frame, pad?.Invoke(live), source: source)).CompletedPlay;
        Assert.NotNull(done);
        return live.TakeTrace(done);
    }

    static void Write(string id, PlayTrace trace)
    {
        var folder = Environment.GetEnvironmentVariable("GS_RACE_TRACE_OUTPUT");
        if (string.IsNullOrEmpty(folder)) return;
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, id + ".json"), trace.ToJson());
    }

    sealed class TickComparer : IEqualityComparer<PlayTraceTick>
    {
        public static readonly TickComparer Instance = new();
        public bool Equals(PlayTraceTick? x, PlayTraceTick? y) => System.Text.Json.JsonSerializer.Serialize(x, PlayTrace.Json) == System.Text.Json.JsonSerializer.Serialize(y, PlayTrace.Json);
        public int GetHashCode(PlayTraceTick obj) => obj.I;
    }
}
