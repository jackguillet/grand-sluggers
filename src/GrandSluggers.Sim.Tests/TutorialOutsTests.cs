using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

[Trait("Rows", "compact")]
public sealed class TutorialOutsTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    const double Frame = 1.0 / 60;

    TutorialSession Start(string id)
    {
        var run = new TutorialSession(_content, TutorialCatalog.Load(_content), id);
        run.Begin();
        return run;
    }

    static void Drive(TutorialSession run, int bag, LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        for (var i = 0; i < 1900 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            var pad = live.HoldsBall && !live.Throwing && run.HumanThrows.Count == 0
                ? new LivePadInput(KeysBag: bag, SouthDown: true) : LivePadInput.Dead;
            run.Tick(Frame, pad, source);
        }
    }

    [Fact]
    public void ChoiceAtSecondRequiresOwnThrowAndCorrectRunnerOutThreeTimes()
    {
        var run = Start("T-D03");
        for (var n = 1; n <= 3; n++)
        {
            Drive(run, 2);
            Assert.True(run.Feedback?.Success == true, $"{run.Feedback}; {run.LastPlay?.Kind}; throws {string.Join(',',run.HumanThrows)}");
            Assert.Equal(n, run.Successes);
            Assert.Equal(n == 3, run.Passed);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            run.Retry();
        }
    }

    [Fact]
    public void FirstBagAndCpuThrowCannotEarnChoiceAtSecond()
    {
        var run = Start("T-D03");
        Drive(run, 1);
        Assert.False(run.Feedback?.Success ?? false);
        run.Retry();
        Drive(run, 2, LivePlayCommandSource.Cpu);
        Assert.False(run.Feedback?.Success ?? false);
    }

    [Fact]
    public void HumanReturnThrowDoublesOffEarlyRunnerThreeTimes()
    {
        var run = Start("T-D04");
        for (var n = 1; n <= 3; n++)
        {
            Drive(run, 2);
            Assert.True(run.Feedback?.Success == true, $"{run.Feedback}; {run.LastPlay?.Kind}; throws {string.Join(',',run.HumanThrows)}");
            Assert.Equal(n, run.Successes);
            Assert.Equal(n == 3, run.Passed);
            Assert.Equal(run.Feedback, TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording()).Feedback);
            run.Retry();
        }
    }

    [Fact]
    public void WrongBagOrCpuReturnThrowCannotDoubleOffForHuman()
    {
        var run = Start("T-D04");
        Drive(run, 3);
        Assert.False(run.Feedback?.Success ?? false);
        run.Retry();
        Drive(run, 2, LivePlayCommandSource.Cpu);
        Assert.False(run.Feedback?.Success ?? false);
    }

    [Fact]
    public void BasesLoadedHumanThrowForcesOriginalRunnerAtHomeThreeTimes()
    {
        var run = Start("T-D01");
        for (var n = 1; n <= 3; n++)
        {
            Drive(run, 4);
            Assert.True(run.Feedback?.Success == true, $"{run.Feedback}; {run.LastPlay?.Kind}; outs {string.Join(',',run.LastPlay?.Outcome?.OutsMade.Select(o => $"{o.Runner.Id}:{o.Type}:{o.Bag}") ?? [])}; throws {string.Join(',',run.HumanThrows)}");
            Assert.Equal(n, run.Successes);
            Assert.Equal(n == 3, run.Passed);
            Assert.Equal(run.Feedback, TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording()).Feedback);
            run.Retry();
        }
    }

    [Fact]
    public void WrongBagAndCpuThrowCannotEarnHomeForce()
    {
        var run = Start("T-D01");
        Drive(run, 1);
        Assert.False(run.Feedback?.Success ?? false);
        run.Retry();
        Drive(run, 4, LivePlayCommandSource.Cpu);
        Assert.False(run.Feedback?.Success ?? false);
    }
}
