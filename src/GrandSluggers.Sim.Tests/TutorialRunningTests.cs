using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

[Trait("Rows", "compact")]
public sealed class TutorialRunningTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    const double Frame = 1.0 / 60;

    TutorialSession Start(string id)
    {
        var run = new TutorialSession(_content, TutorialCatalog.Load(_content), id);
        run.Begin();
        return run;
    }

    static void Drive(TutorialSession run, LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        var returningEarlyFly = false;
        for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var pad = LivePadInput.Dead;
            if (run.Lesson.Id == "T-R01")
            {
                var runner = run.Match.RunnerAt(2);
                if (runner is not null)
                {
                    pad = runner.Held ? new(StickBag: 2)
                        : runner.Phase == RunnerPhase.Returning ? LivePadInput.Dead
                        : runner.Feet > 8 ? new(Freeze: true, StickBag: 3)
                        : new(KeysBag: 2, StickBag: 3);
                }
            }
            else if (run.Lesson.Id == "T-R02")
            {
                var lead = run.Match.RunnerAt(2);
                pad = lead is null ? LivePadInput.Dead
                    : lead.Phase == RunnerPhase.Returning ? LivePadInput.Dead
                    : lead.Feet > 5 ? new(AllReturn: true)
                    : new(AllAdvance: true);
            }
            else if (run.Lesson.Id == "T-R03") pad = new(SouthDown: true);
            else if (run.Lesson.Id == "T-R09")
            {
                var runner = run.Match.Runners.FirstOrDefault(r => r.IsBatter);
                if (runner is { Bag: 1, DestBag: < 2 })
                    pad = new(KeysBag: 1, StickBag: 2);
                else if (runner is { Bag: 1, Feet: <= 10, DestBag: >= 2 } && !runner.Overrunning)
                    pad = new(SouthDown: true);
            }
            else if (run.Lesson.Id == "T-R05" && run.Match.LivePlay.Caught)
                pad = new(AllAdvance: true);
            else if (run.Lesson.Id == "T-R10")
            {
                var runner = run.Match.RunnerAt(3);
                if (runner is { Feet: > 12 }) returningEarlyFly = true;
                pad = runner is null || run.Match.LivePlay.Caught ? LivePadInput.Dead
                    : returningEarlyFly && runner.Feet > 0 ? new(AllReturn: true)
                    : returningEarlyFly ? LivePadInput.Dead
                    : new(KeysBag: 3, StickBag: 4);
            }
            else if (run.Lesson.Id == "T-R04")
            {
                var runner = run.Match.RunnerAt(0);
                pad = runner is not null && runner.FeetTo(runner.NextBag) <= run.Match.Rules.Running.Bags.SlideFt
                    ? new(WestDown: true) : LivePadInput.Dead;
            }
            run.Tick(Frame, pad, source);
        }
    }

    [Theory]
    [InlineData("T-R01")]
    [InlineData("T-R02")]
    [InlineData("T-R03")]
    [InlineData("T-R09")]
    [InlineData("T-R04")]
    [InlineData("T-R05")]
    [InlineData("T-R10")]
    public void HumanCommandsAndRunnerGeometryEarnThreeDistinctAttempts(string id)
    {
        var run = Start(id);
        for (var n = 1; n <= 3; n++)
        {
            Drive(run);
            Assert.True(run.Feedback?.Success == true, $"{id}: {run.Feedback} at {run.Elapsed:0.00}; play {run.LastPlay?.Kind}; presses {run.Inputs.Count(x => x.Field?.SouthDown == true)}; runners {string.Join(';',run.Match.Runners.Select(r => $"{r.FromBag}:{r.Bag}/{r.Feet:0.0}/{r.Phase}/{r.Held}"))}");
            Assert.Equal(n, run.Successes);
            Assert.Equal(n == 3, run.Passed);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.True(replay.Feedback == run.Feedback, $"{id}: replay {replay.Feedback}, phase {replay.Phase}, elapsed {replay.Elapsed:0.00}, inputs {replay.Inputs.Count}, runner {replay.Match.RunnerAt(2)?.Bag}/{replay.Match.RunnerAt(2)?.Feet:0.0}/{replay.Match.RunnerAt(2)?.Phase} versus {run.Feedback}, runner {run.Match.RunnerAt(2)?.Bag}/{run.Match.RunnerAt(2)?.Feet:0.0}/{run.Match.RunnerAt(2)?.Phase}");
            run.Retry();
        }
    }

    [Theory]
    [InlineData("T-R01")]
    [InlineData("T-R02")]
    [InlineData("T-R03")]
    [InlineData("T-R09")]
    [InlineData("T-R04")]
    [InlineData("T-R05")]
    [InlineData("T-R10")]
    public void CpuOrdersAndDeadInputCannotEarnCredit(string id)
    {
        var run = Start(id);
        Drive(run, LivePlayCommandSource.Cpu);
        Assert.False(run.Feedback?.Success ?? false);
        run.Retry();
        for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++) run.Tick(Frame);
        Assert.False(run.Feedback?.Success ?? false);
    }

    [Fact]
    public void EarlyOrUnsentDashDoesNotEarnCornerTurn()
    {
        var run = Start("T-R09");
        for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++)
            run.Tick(Frame, new LivePadInput(SouthDown: true));
        Assert.False(run.Feedback?.Success ?? false);
        run.Retry();
        for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var batter = run.Match.Runners.FirstOrDefault(r => r.IsBatter);
            var pad = batter is { Bag: 1, DestBag: < 2 } ? new LivePadInput(KeysBag: 1, StickBag: 2)
                : LivePadInput.Dead;
            run.Tick(Frame, pad);
        }
        Assert.False(run.Feedback?.Success ?? false);
    }

    [Fact]
    public void EarlyFlySendWithoutReturnDoesNotEarnSafeRetouch()
    {
        var run = Start("T-R10");
        for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++)
            run.Tick(Frame, new LivePadInput(KeysBag: 3, StickBag: 4));
        Assert.False(run.Feedback?.Success ?? false);
        run.Retry();
        for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++)
            run.Tick(Frame, new LivePadInput(AllReturn: true));
        Assert.False(run.Feedback?.Success ?? false);
    }
}
