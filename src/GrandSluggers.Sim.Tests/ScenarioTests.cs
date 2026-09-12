using System.Text.RegularExpressions;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Spec Appendix B rows owned by P0 (#562): B.7 determinism and seats. Each row is named by
/// its spec id. Later epics add their rows next to these on the same <see cref="Scenario"/> rail.
/// </summary>
public sealed class ScenarioTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    // ---------------------------------------------------------------------------------
    // S-90  Same seed + same recorded commands → identical PlayEvent stream, CPU seat and human seat
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S90_GrounderRelayReplaysIdenticallyForCpuAndHumanSeats()
    {
        var cpu = RunGrounderRelay(LivePlayCommandSource.Cpu);
        var human = RunGrounderRelay(LivePlayCommandSource.Human);

        Assert.Equal(cpu.Stream, human.Stream);
        Assert.Equal(PlayKind.GroundOut, cpu.Play.Kind);
        Assert.Equal(2, cpu.Play.OutsOnPlay);
        Assert.Collection(cpu.Play.Outcome!.OutsMade,
            force => Assert.Equal((OutType.Force, 2, 1), (force.Type, force.Bag, force.FromBag)),
            first => Assert.Equal((OutType.ThrowOutAtFirst, 1, 0), (first.Type, first.Bag, first.FromBag)));
    }

    [Fact]
    public void S90_FlyWithLiveTagReplaysIdenticallyForCpuAndHumanSeats()
    {
        var cpu = RunFlyWithBodyTag(LivePlayCommandSource.Cpu);
        var human = RunFlyWithBodyTag(LivePlayCommandSource.Human);

        Assert.Equal(cpu.Stream, human.Stream);
        Assert.Equal(PlayKind.FlyOut, cpu.Play.Kind);
        Assert.Contains(cpu.Play.Outcome!.OutsMade, o => o.Type == OutType.Tag && o.FromBag == 1);
        Assert.Contains(cpu.Play.Outcome!.OutsMade, o => o.Type == OutType.Catch && o.FromBag == 0);
    }

    [Fact]
    public void S90_StealThrowResolvesIdenticallyForCpuAndHumanSeats()
    {
        var cpu = RunStealThrow(seed: 3);
        var human = RunStealThrow(seed: 3);

        Assert.Equal(cpu, human);
        Assert.Contains("caught stealing", cpu, StringComparison.Ordinal);
    }

    [Fact]
    public void S90_PauseAndResumeInsideTheScriptDoNotChangeTheStream()
    {
        var straight = RunGrounderRelay(LivePlayCommandSource.Human, pauseMidway: false);
        var paused = RunGrounderRelay(LivePlayCommandSource.Human, pauseMidway: true);

        Assert.Equal(straight.Stream, paused.Stream);
    }

    // ---------------------------------------------------------------------------------
    // S-91  Every scenario runs without Unity scene objects
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S91_TheSimAndTheHarnessReferenceNoUnityAssembly()
    {
        var sim = typeof(Match).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? "");
        Assert.DoesNotContain(sim, name => name.StartsWith("Unity", StringComparison.Ordinal));
        var tests = typeof(Scenario).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? "");
        Assert.DoesNotContain(tests, name => name.StartsWith("Unity", StringComparison.Ordinal));
    }

    [Fact]
    public void S91_AFullCpuHalfInningRunsHeadlessThroughLivePlayCommands()
    {
        // The CPU seat plays every ball through the same command boundary the human seat uses.
        var scenario = new Scenario(_content, seed: 5);
        var match = scenario.Match;
        var plays = 0;
        while (match.Top && !match.Over && plays++ < 60)
        {
            if (!match.BeginAtBat(scenario.CpuPitch(), Scenario.Swing, out var hit, out _))
                continue;
            var preview = match.PreviewHit(hit);
            var field = match.ResolveFielding(hit, preview);
            match.LivePlay.Apply(LivePlayCommand.Begin(field.Kind, LivePlayCommandSource.Cpu));
            match.LivePlay.Apply(LivePlayCommand.Advance(
                4, field.Kind, true, false, true, 0, LivePlayCommandSource.Cpu));
            var done = match.LivePlay.Apply(LivePlayCommand.Complete(
                Scenario.Paint, Scenario.Swing, hit, field, LivePlayCommandSource.Cpu));
            Assert.NotNull(done.CompletedPlay);
            Assert.NotNull(done.CompletedPlay!.Outcome);
        }
        Assert.False(match.Top, "three outs were made through commands alone");
        Assert.All(match.Log, ev => Assert.NotNull(ev.Outcome));
    }

    // ---------------------------------------------------------------------------------
    // S-92  No PlayEvent is produced by a System.Random outside the sim's seeded _rng
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S92_TheOnlyRandomInTheSimIsTheMatchSeed()
    {
        var simDir = Path.Combine(RepoRoot(), "src", "GrandSluggers.Sim");
        var random = new Regex(@"new\s+(System\.)?Random\s*\(|Random\.Shared");
        var offenders = new List<string>();
        foreach (var file in Directory.GetFiles(simDir, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                continue;
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
                if (random.IsMatch(lines[i]))
                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
        }
        var allowed = Assert.Single(offenders);
        Assert.StartsWith("Match.cs:", allowed);
        Assert.Contains("_rng = new Random(seed)", allowed);
    }

    [Fact]
    public void S92_SameSeedSameCommandsSameStreamAcrossWholeGames()
    {
        var a = new Scenario(_content, seed: 11);
        var b = new Scenario(_content, seed: 11);
        a.Match.AutoPlayGame();
        b.Match.AutoPlayGame();

        Assert.True(a.Match.Over);
        Assert.Equal(a.Stream(), b.Stream());
        Assert.NotEqual(a.Stream(), new Scenario(_content, seed: 12).AlsoAutoPlayed().Stream());
    }

    // ---------------------------------------------------------------------------------
    // Typed outcome: every result carries its facts, and the caption is narrated from them
    // ---------------------------------------------------------------------------------

    [Fact]
    public void StrikeoutCarriesTheBatterOutAtThePlate()
    {
        var scenario = new Scenario(_content, seed: 1);
        var match = scenario.Match;
        PlayEvent? last = null;
        for (var i = 0; i < 3; i++)
            last = match.Play(Scenario.Paint, Scenario.Take);
        Assert.Equal(PlayKind.Strikeout, last!.Kind);
        var only = Assert.Single(last.Outcome!.OutsMade);
        Assert.Equal((OutType.Strikeout, 0, 0), (only.Type, only.Bag, only.FromBag));
        Assert.Equal(last.Batter.Id, only.Runner.Id);
        Assert.Equal(0, last.Outcome.BatterToBag);
        Assert.False(last.Outcome.FieldersChoice);
    }

    [Fact]
    public void WalkCarriesEveryForcedAdvanceAndTheBatterAtFirst()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(1, 1);
        var match = scenario.Match;
        var runner = match.First!;
        var wide = new PitchCommand("fastball", 0, false, AimX: 3, AimY: 3);
        PlayEvent? last = null;
        for (var i = 0; i < 4; i++)
            last = match.Play(wide, Scenario.Take);
        Assert.Equal(PlayKind.Walk, last!.Kind);
        Assert.Equal(1, last.Outcome!.BatterToBag);
        Assert.Contains(last.Outcome.Moves, m => m.Runner.Id == runner.Id && m.FromBag == 1 && m.ToBag == 2);
        Assert.Contains(last.Outcome.Moves, m => m.Runner.Id == last.Batter.Id && m.FromBag == 0 && m.ToBag == 1);
        Assert.Empty(last.Outcome.OutsMade);
    }

    [Fact]
    public void ForceWithTheBatterSafeIsAFieldersChoice()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(1, 1);
        var match = scenario.Match;
        var runner = match.First!;
        var hit = scenario.Contact();
        var field = Grounder(match);
        var play = scenario.Replay(LivePlayCommandSource.Human,
        [
            LivePlayCommand.Begin(PlayKind.GroundOut),
            LivePlayCommand.Advance(1.5, PlayKind.GroundOut, true, false, true, 0),
            LivePlayCommand.ThrowArrived(2, runnerBeats: false, field.Fielder),
            LivePlayCommand.ThrowArrived(1, runnerBeats: true, field.Fielder),
            LivePlayCommand.Complete(Scenario.Paint, Scenario.Swing, hit, field)
        ]).CompletedPlay!;

        Assert.Equal(PlayKind.GroundOut, play.Kind);
        var force = Assert.Single(play.Outcome!.OutsMade);
        Assert.Equal((OutType.Force, 2, 1, runner.Id), (force.Type, force.Bag, force.FromBag, force.Runner.Id));
        Assert.Equal(1, play.Outcome.BatterToBag);
        Assert.True(play.Outcome.FieldersChoice);
        // The caption is narrated from the typed moment, last. (Its wording — the relay's
        // "in at first" repeats today — is P8's FIELDER'S CHOICE stamp, not a rule.)
        Assert.StartsWith(InPlay.Narrate(InPlay.ThrowVerdict.BatterSafeAfterForce, 1, field.Fielder!.Name, play.Batter.Name), play.Caption);
    }

    [Fact]
    public void LiveBodyTagOfTheBatterIsATagFromBagZero()
    {
        var scenario = new Scenario(_content, seed: 1);
        var match = scenario.Match;
        var hit = scenario.Contact();
        var field = Grounder(match);
        match.LivePlay.Apply(LivePlayCommand.Begin(PlayKind.GroundOut));
        var advanced = match.LivePlay.Apply(LivePlayCommand.Advance(1, PlayKind.GroundOut, true, false, true, 0));
        _ = advanced;
        var at = match.BatterRunner!.Position;
        var tag = match.LivePlay.Apply(LivePlayCommand.Contact(
            PlayKind.GroundOut, true, false, true, at.X, at.Z, 0, field.Fielder));
        Assert.Equal(0, tag.TaggedFromBag);
        Assert.Equal(InPlay.ThrowVerdict.TagRunner, match.LivePlay.LastMoment!.Verdict);
        Assert.Equal($"{field.Fielder!.Name} tags {match.Batter.Name}.", match.LivePlay.Caption);

        var play = match.LivePlay.Apply(LivePlayCommand.Complete(Scenario.Paint, Scenario.Swing, hit, field)).CompletedPlay!;
        var only = Assert.Single(play.Outcome!.OutsMade);
        Assert.Equal((OutType.Tag, 0, 0), (only.Type, only.Bag, only.FromBag));
        Assert.Equal(0, play.Outcome.BatterToBag);
    }

    [Fact]
    public void CaughtStealingIsATagAndAStolenBaseIsAMove()
    {
        var caught = RunStealThrowEvent(seed: 3, throwBag: 2);
        Assert.Equal(PlayKind.CaughtStealing, caught.Kind);
        var tag = Assert.Single(caught.Outcome!.OutsMade);
        Assert.Equal((OutType.Tag, 2, 1), (tag.Type, tag.Bag, tag.FromBag));
        Assert.Equal(RunnerPlayResult.CaughtStealing, caught.Outcome.RunnerResult);

        var stole = RunStealThrowEvent(seed: 3, throwBag: 3);
        Assert.Equal(PlayKind.StolenBase, stole.Kind);
        Assert.Empty(stole.Outcome!.OutsMade);
        var move = Assert.Single(stole.Outcome.Moves);
        Assert.Equal((1, 2), (move.FromBag, move.ToBag));
        Assert.Equal(RunnerPlayResult.StolenBase, stole.Outcome.RunnerResult);
    }

    [Fact]
    public void GrandSlamCarriesFourRunnersHome()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(1, 1).Runner(2, 2).Runner(3, 3);
        var match = scenario.Match;
        scenario.Contact();
        var hit = FlightFixtures.OverTheFence(match.Park, 20, 0);
        var field = new FieldingResult(PlayKind.HomeRun, null, null, 4, 0, 420, false, false);
        var play = match.FinishAtBat(Scenario.Paint, Scenario.Swing, hit, field);

        Assert.Equal(4, play.RunsScored);
        Assert.Equal(4, play.Outcome!.BatterToBag);
        Assert.Equal(4, play.Outcome.Moves.Count);
        Assert.All(play.Outcome.Moves, m => Assert.Equal(4, m.ToBag));
        Assert.Empty(play.Outcome.OutsMade);
    }

    [Fact]
    public void ABobbledHitIsAnErrorNotACaption()
    {
        var scenario = new Scenario(_content, seed: 1);
        var match = scenario.Match;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 45, 8, -12);
        var field = new FieldingResult(
            PlayKind.Single, match.Pitcher, match.Batter, 1.5, 48, 72, false, false,
            new ThrowResult(Chemistry.Good, 1.7, false), Bobble: true);
        var play = match.FinishAtBat(Scenario.Paint, Scenario.Swing, hit, field);

        Assert.Equal(PlayKind.Single, play.Kind);
        Assert.True(play.Outcome!.Error);
        Assert.Equal(1, play.Outcome.BatterToBag);
    }

    [Fact]
    public void ThrowStepCaptionIsNarratedFromItsVerdict()
    {
        var step = InPlay.ThrowToBag(2, firstOccupied: true, alreadyForced: false, runnerBeats: false, outs: 0, "Rio", "Vale");
        Assert.Equal(InPlay.ThrowVerdict.ForceOut, step.Verdict);
        Assert.Equal(InPlay.Narrate(step.Verdict, step.Bag, "Rio", "Vale"), step.Caption);
        Assert.Equal(OutType.Force, step.OutType);

        var two = InPlay.ThrowToBag(1, firstOccupied: true, alreadyForced: true, runnerBeats: false, outs: 1, "Rio", "Vale");
        Assert.Equal(InPlay.ThrowVerdict.TurnedTwo, two.Verdict);
        Assert.Equal("Rio turns two.", two.Caption);

        var safe = InPlay.ThrowToBag(1, firstOccupied: true, alreadyForced: true, runnerBeats: true, outs: 1, "Rio", "Vale");
        Assert.Equal(InPlay.ThrowVerdict.BatterSafeAfterForce, safe.Verdict);
        Assert.True(InPlay.NarratesBatterAtFirst(safe.Verdict));
    }

    // ---------------------------------------------------------------------------------

    (string Stream, PlayEvent Play) RunGrounderRelay(LivePlayCommandSource seat, bool pauseMidway = false)
    {
        var scenario = new Scenario(_content, seed: 1).Runner(1, 1);
        var match = scenario.Match;
        var hit = scenario.Contact();
        var field = Grounder(match);
        var script = new List<LivePlayCommand>
        {
            LivePlayCommand.Begin(PlayKind.GroundOut),
            LivePlayCommand.Advance(1, PlayKind.GroundOut, false, false, false, 0)
        };
        if (pauseMidway)
        {
            script.Add(LivePlayCommand.Pause());
            script.Add(LivePlayCommand.Advance(9, PlayKind.GroundOut, true, false, true, 0));
            script.Add(LivePlayCommand.Resume());
        }
        script.Add(LivePlayCommand.Advance(0.5, PlayKind.GroundOut, true, false, true, 0));
        script.Add(LivePlayCommand.ThrowArrived(2, runnerBeats: false, field.Fielder));
        script.Add(LivePlayCommand.ThrowArrived(1, runnerBeats: false, field.Fielder));
        script.Add(LivePlayCommand.Complete(Scenario.Paint, Scenario.Swing, hit, field));
        var play = scenario.Replay(seat, script).CompletedPlay!;
        return (scenario.Stream(), play);
    }

    (string Stream, PlayEvent Play) RunFlyWithBodyTag(LivePlayCommandSource seat)
    {
        var scenario = new Scenario(_content, seed: 1).Runner(1, 1);
        var match = scenario.Match;
        var hit = scenario.Contact();
        var field = new FieldingResult(PlayKind.FlyOut, match.Pitcher, null, 1.2, 40, 150, false, false);
        var script = new List<LivePlayCommand>
        {
            LivePlayCommand.Begin(PlayKind.FlyOut),
            LivePlayCommand.Advance(1.3, PlayKind.FlyOut, true, false, true, 0),
            LivePlayCommand.TagRunner(1, field.Fielder),
            LivePlayCommand.Complete(Scenario.Paint, Scenario.Swing, hit, field)
        };
        var play = scenario.Replay(seat, script).CompletedPlay!;
        return (scenario.Stream(), play);
    }

    string RunStealThrow(int seed) => Scenario.Fingerprint(RunStealThrowEvent(seed, throwBag: 2));

    PlayEvent RunStealThrowEvent(int seed, int throwBag)
    {
        var scenario = new Scenario(_content, seed).Runner(1, 1);
        var match = scenario.Match;
        Assert.True(match.SelectRunner(1));
        Assert.True(match.StartSteal());
        Assert.False(match.BeginAtBat(Scenario.Paint, Scenario.Take, out _, out var pitch));
        Assert.True(match.StealThrowPending);
        var laser = new ThrowResult(Chemistry.Good, 1.6, false);
        return match.ResolveStealThrow(pitch!, throwBag, releaseSec: 0.1, laser);
    }

    static FieldingResult Grounder(Match match) => new(
        PlayKind.GroundOut, match.Pitcher, match.Batter, 1.5, 48, 72, false, false,
        new ThrowResult(Chemistry.Good, 1.7, false));

    static string RepoRoot() => Path.GetFullPath(Path.Combine(ContentCatalog.Load().Root, ".."));
}

static class ScenarioExtensions
{
    public static Scenario AlsoAutoPlayed(this Scenario scenario)
    {
        scenario.Match.AutoPlayGame();
        return scenario;
    }

    public static PitchCommand CpuPitch(this Scenario scenario) => scenario.Match.PreparePitch(scenario.Match.CpuPitch());
}
