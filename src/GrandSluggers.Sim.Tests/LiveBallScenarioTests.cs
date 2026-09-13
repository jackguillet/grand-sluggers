using System.Text.RegularExpressions;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The baseball that used to live in Unity's InPlayDirector — relay chain, close-play race,
/// CPU catch timing, the steal play, glove speed, bobble — now runs in <see cref="LivePlaySystem"/>
/// from one <c>Tick</c> per frame. These rows drive it with dead pads (the CPU seat) or a scripted
/// pad, with no scene object (S-91), and replay identically for either seat (S-90).
/// </summary>
public sealed class LiveBallScenarioTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    const double Frame = 1.0 / 60.0;

    [Theory]
    [InlineData(LivePlayCommandSource.Cpu)]
    [InlineData(LivePlayCommandSource.Human)]
    public void CpuGrounderRelayTurnsTwoThroughTicksAlone(LivePlayCommandSource seat)
    {
        var gloves = 0;
        var (scenario, play) = RunGrounder(seat, runnerOnFirst: true, live => gloves = Math.Max(gloves, live.Fielders.Count));
        Assert.Equal(PlayKind.GroundOut, play.Kind);
        var outs = play.Outcome!.OutsMade;
        Assert.NotEmpty(outs);
        Assert.Equal((OutType.Force, 2, 1), (outs[0].Type, outs[0].Bag, outs[0].FromBag));
        Assert.Equal(9, gloves);
        Assert.False(scenario.Match.LivePlay.Active, "the sim completed and reset the live ball itself");
    }

    [Fact]
    public void S90_RelayChainReplaysIdenticallyForCpuAndHumanSeats()
    {
        var cpu = RunGrounder(LivePlayCommandSource.Cpu, runnerOnFirst: true);
        var human = RunGrounder(LivePlayCommandSource.Human, runnerOnFirst: true);
        Assert.Equal(cpu.Scenario.Stream(), human.Scenario.Stream());
    }

    [Fact]
    public void CpuGloveCatchesARoutineFlyAtTheWindow()
    {
        var scenario = new Scenario(_content, seed: 2);
        var match = scenario.Match;
        var hit = Fly(scenario.Contact(), match);
        var preview = match.PreviewHit(hit);
        var field = new FieldingResult(PlayKind.FlyOut, preview.Fielder, null, preview.HangTimeSec, preview.LandingX, preview.LandingZ, false, false);
        var play = RunToComplete(match, hit, preview, field, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu, out var caughtAt);

        Assert.Equal(PlayKind.FlyOut, play.Kind);
        var only = Assert.Single(play.Outcome!.OutsMade);
        Assert.Equal((OutType.Catch, 0), (only.Type, only.FromBag));
        // Inside the catch window (fielding.catch.windowBefore/After plus the glove's ability bonus), not force-fed early.
        Assert.True(FlyCatch.JumpWindow(caughtAt, preview.HangTimeSec, preview.Fielder, match.Park, match.Rules)
                    || FlyCatch.JumpWindow(caughtAt - Frame, preview.HangTimeSec, preview.Fielder, match.Park, match.Rules),
            $"caught at {caughtAt:0.00} vs hang {preview.HangTimeSec:0.00}");
    }

    [Fact]
    public void ClosePlayRaceAtThirdResolvesAgainstTheCpuGlove()
    {
        // The offense sends the slowest runner from second at contact (LB) on a sharp grounder to the
        // first baseman; the CPU glove reads the tag at third as makeable and throws there before the
        // batter at first (§8.8 rules 3–4). The mash runs only when the ball is there ahead of the body
        // by no more than the margin (§9.6, D5); further out the geometry decides silently.
        var seedMatch = new Scenario(_content, seed: 5).Match;
        var slowest = Enumerable.Range(1, seedMatch.AwayOrder.Count - 1).OrderBy(i => seedMatch.AwayOrder[i].Stats.Run).First();
        var scenario = new Scenario(_content, seed: 5).Runner(2, slowest);
        var match = scenario.Match;
        var runner = match.Second!;
        // To the right side, away from the runner's path from second to third.
        var hit = Shape(scenario.Contact(), match, exit: 90, launch: 3, spray: 30);
        var preview = match.PreviewHit(hit);
        Assert.Equal("1B", preview.Position);
        var field = match.ResolveFielding(hit, preview);
        var seats = new LiveSeats(HumanBats: true, HumanPitches: false, PlayerMustField: false, Versus: false);
        var sawIcon = false;
        var sawThrowToThird = false;
        var frame = 0;
        var iconAt = -1;
        var arrivalBeforeLanding = double.NaN;
        var play = RunToComplete(match, hit, preview, field, seats, LivePlayCommandSource.Human, out _,
            observe: live =>
            {
                frame++;
                if (live.Events.Contains(LiveEvent.CloseIcon)) { sawIcon = true; iconAt = frame; }
                if (live.Throwing && live.ThrowBag == 3)
                {
                    sawThrowToThird = true;
                    var body = match.RunnerAt(2);
                    if (body is { Live: true }) arrivalBeforeLanding = RunnerSystem.ArrivalSec(body, 3, live.ElapsedSeconds, live.Dash01, match.Rules);
                }
            },
            // LB at contact sends the runner; the offense mashes a few frames after the icon (§9.6).
            runPad: i => i < 3 ? new LivePadInput(AllAdvance: true)
                : iconAt >= 0 && i == iconAt + 3 ? new LivePadInput(SouthDown: true)
                : LivePadInput.Dead);

        Assert.True(sawThrowToThird, "with first empty and the runner going the CPU throws to the tag bag (S-37)");
        var facts = play.Outcome!;
        var tagged = facts.OutsMade.Any(o => o.Type == OutType.Tag && o.Bag == 3 && o.Runner.Id == runner.Id);
        var advanced = facts.Moves.Any(m => m.Runner.Id == runner.Id && m.FromBag == 2 && m.ToBag == 3);
        Assert.True(tagged ^ advanced, "the race decided one thing: out at third, or safe at third");
        var within = arrivalBeforeLanding > 0 && arrivalBeforeLanding <= match.Rules.Running.Close.MarginSec + Frame;
        Assert.Equal(within, sawIcon);
        if (!within && arrivalBeforeLanding > 0) Assert.True(tagged, "the ball well ahead of the body is the tag, no contest");
    }

    [Theory]
    [InlineData(LivePlayCommandSource.Cpu)]
    [InlineData(LivePlayCommandSource.Human)]
    public void StealPlayRunsHeadlesslyWithTheCpuCatcher(LivePlayCommandSource seat)
    {
        var (scenario, play) = RunSteal(seat);
        Assert.True(play.Kind is PlayKind.CaughtStealing or PlayKind.StolenBase, play.Kind.ToString());
        if (play.Kind == PlayKind.CaughtStealing)
        {
            var tag = Assert.Single(play.Outcome!.OutsMade);
            Assert.Equal((OutType.Tag, 1), (tag.Type, tag.FromBag));
        }
        else
        {
            var move = Assert.Single(play.Outcome!.Moves);
            Assert.Equal((1, 2), (move.FromBag, move.ToBag));
        }
        Assert.False(scenario.Match.LivePlay.RunnerPlay);
        Assert.False(scenario.Match.LivePlay.Active);
    }

    [Fact]
    public void S90_StealPlayReplaysIdenticallyForCpuAndHumanSeats()
    {
        var cpu = RunSteal(LivePlayCommandSource.Cpu);
        var human = RunSteal(LivePlayCommandSource.Human);
        Assert.Equal(cpu.Scenario.Stream(), human.Scenario.Stream());
    }

    [Fact]
    public void AHumanStickRunsTheGloveAtTheOneChaseSpeedAfterTheLockout()
    {
        var scenario = new Scenario(_content, seed: 1);
        var match = scenario.Match;
        var hit = Fly(scenario.Contact(), match);
        var preview = match.PreviewHit(hit);
        var seats = new LiveSeats(HumanBats: false, HumanPitches: true, PlayerMustField: false, Versus: false);
        match.LivePlay.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, seats));
        var before = (match.LivePlay.GloveX, match.LivePlay.GloveZ);
        var pad = new LivePadInput(StickX: 1, StickY: 0);
        for (var i = 0; i < 30; i++)
            match.LivePlay.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false, LivePlayCommandSource.Human));

        Assert.True(match.LivePlay.PlayerFielding, "a live stick takes the glove from the CPU");
        // One glove speed for human and CPU (§8.1); the body moves once its reaction lockout is over (§8.2).
        var pos = match.LivePlay.GlovePos;
        var who = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher)[pos];
        var lockout = match.Rules.Fielding.Reaction.LockoutSec(pos);
        var moving = Math.Max(0, Frame * 30 - lockout);
        var expected = FieldingResolver.ChaseSpeedFt(who, pos, preview, match.Rules) * moving;
        var moved = match.LivePlay.GloveX - before.GloveX;
        Assert.InRange(moved, expected * 0.5, expected * 1.05);
    }

    [Fact]
    public void PausedTicksDoNotMoveTheLiveBall()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(1, 1);
        var match = scenario.Match;
        var hit = Grounder(scenario.Contact(), match, sprayDeg: -12);
        var preview = match.PreviewHit(hit);
        var field = SyntheticGroundOut(match, preview);
        match.LivePlay.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field, LiveSeats.CpuOnly));
        match.LivePlay.Apply(LivePlayCommand.Tick(0.3));
        var frozen = (match.LivePlay.ElapsedSeconds, match.LivePlay.BallX, match.LivePlay.GloveX);
        match.LivePlay.Apply(LivePlayCommand.Pause());
        for (var i = 0; i < 60; i++) match.LivePlay.Apply(LivePlayCommand.Tick(Frame));
        Assert.Equal(frozen, (match.LivePlay.ElapsedSeconds, match.LivePlay.BallX, match.LivePlay.GloveX));
        match.LivePlay.Apply(LivePlayCommand.Resume());
        match.LivePlay.Apply(LivePlayCommand.Tick(Frame));
        Assert.True(match.LivePlay.ElapsedSeconds > frozen.ElapsedSeconds);
    }

    [Fact]
    public void S92_NoRandomInTheUnityRuntimeDecidesAPlay()
    {
        // The client used to seed its own Random for the bobble and the CPU catcher's release.
        var runtime = Path.Combine(RepoRoot(), "unity", "Assets", "Scripts", "Runtime");
        var random = new Regex(@"new\s+(System\.)?Random\s*\(|Random\.Shared|UnityEngine\.Random|\bRandom\.(Range|value|insideUnit\w*|onUnit\w*)");
        var offenders = new List<string>();
        foreach (var file in Directory.GetFiles(runtime, "*.cs", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
                if (random.IsMatch(lines[i]))
                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
        }
        // Audio pitch jitter is presentation; it cannot reach a PlayEvent.
        Assert.All(offenders, o => Assert.StartsWith("AudioBus.cs:", o));
    }

    [Fact]
    public void OnePlayerBattingStealResolvesFromTicksAlone()
    {
        // The one-controller flow as the client drives it: no D-pad (selection syncs to the lead
        // runner), L3 toggles the steal, the runner breaks at release (D2), the take opens the
        // catcher's throw play, and the CPU catcher throws from ticks with a dead pad (§11.3).
        var scenario = new Scenario(_content, seed: 3).Runner(1, 1);
        var match = scenario.Match;
        Assert.True(match.ToggleSteal());
        Assert.True(match.StealOn);
        Assert.False(match.BeginAtBat(Scenario.Paint, Scenario.Take, out _, out var pitch));
        Assert.True(match.StealThrowPending);
        var seats = new LiveSeats(HumanBats: true, HumanPitches: false, PlayerMustField: false, Versus: false);
        match.LivePlay.Apply(LivePlayCommand.BeginSteal(pitch!, seats, LivePlayCommandSource.Human));
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 12 && play is null; i++)
            play = match.LivePlay.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Human)).CompletedPlay;
        Assert.NotNull(play);
        Assert.True(play!.Kind is PlayKind.StolenBase or PlayKind.CaughtStealing, play.Kind.ToString());
        Assert.False(match.LivePlay.RunnerPlay);
    }

    // ---------------------------------------------------------------------------------

    (Scenario Scenario, PlayEvent Play) RunGrounder(LivePlayCommandSource seat, bool runnerOnFirst, Action<LivePlaySystem>? observe = null)
    {
        var scenario = new Scenario(_content, seed: 1);
        if (runnerOnFirst) scenario.Runner(1, 1);
        var match = scenario.Match;
        var hit = Grounder(scenario.Contact(), match, sprayDeg: -12);
        var preview = match.PreviewHit(hit);
        var field = SyntheticGroundOut(match, preview);
        var play = RunToComplete(match, hit, preview, field, LiveSeats.CpuOnly, seat, out _, observe);
        return (scenario, play);
    }

    (Scenario Scenario, PlayEvent Play) RunSteal(LivePlayCommandSource seat)
    {
        var scenario = new Scenario(_content, seed: 3).Runner(1, 1);
        var match = scenario.Match;
        Assert.True(match.SelectRunner(1));
        Assert.True(match.StartSteal());
        Assert.False(match.BeginAtBat(Scenario.Paint, Scenario.Take, out _, out var pitch));
        Assert.True(match.StealThrowPending);
        match.LivePlay.Apply(LivePlayCommand.BeginSteal(pitch!, LiveSeats.CpuOnly, seat));
        Assert.True(match.LivePlay.RunnerPlay);
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 12 && play is null; i++)
            play = match.LivePlay.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, seat)).CompletedPlay;
        Assert.NotNull(play);
        return (scenario, play!);
    }

    static PlayEvent RunToComplete(
        Match match, AtBatResult hit, FieldingPreview preview, FieldingResult? field, LiveSeats seats,
        LivePlayCommandSource seat, out double caughtAt, Action<LivePlaySystem>? observe = null, Func<int, LivePadInput>? runPad = null)
    {
        var begun = match.LivePlay.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field, seats, 0, seat));
        Assert.True(begun.Snapshot.Active);
        caughtAt = -1;
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 20 && play is null; i++)
        {
            var result = match.LivePlay.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, runPad?.Invoke(i) ?? LivePadInput.Dead, false, seat));
            // A catch that ends the play (nobody left to run) commits on the same tick; the Glove cue still says when.
            if (caughtAt < 0 && (match.LivePlay.Caught || match.LivePlay.Events.Contains(LiveEvent.Glove)))
                caughtAt = match.LivePlay.ElapsedSeconds > 0 ? match.LivePlay.ElapsedSeconds : (i + 1) * Frame;
            observe?.Invoke(match.LivePlay);
            play = result.CompletedPlay;
        }
        Assert.NotNull(play);
        return play!;
    }

    static AtBatResult Grounder(AtBatResult hit, Match match, double sprayDeg) => Shape(hit, match, exit: 84, launch: 8, spray: sprayDeg);

    static AtBatResult Fly(AtBatResult hit, Match match) => Shape(hit, match, exit: 92, launch: 34, spray: 4);

    static AtBatResult Shape(AtBatResult hit, Match match, double exit, double launch, double spray)
    {
        var carry = BallFlight.CarryFeet(exit, launch, match.Park.WindMph, match.Rules);
        return hit with { ExitVeloMph = exit, LaunchDeg = launch, SprayDeg = spray, CarryFt = carry, HomeRun = false, Foul = false, InPlay = true };
    }

    static FieldingResult SyntheticGroundOut(Match match, FieldingPreview preview)
    {
        var map = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
        map.TryGetValue("2B", out var cut);
        return new FieldingResult(PlayKind.GroundOut, preview.Fielder, cut, preview.HangTimeSec, preview.LandingX, preview.LandingZ, false, false,
            new ThrowResult(Chemistry.Good, 1.35, false));
    }

    static string RepoRoot() => Path.GetFullPath(Path.Combine(ContentCatalog.Load().Root, ".."));
}
