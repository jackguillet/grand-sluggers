using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class TutorialStealTests
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
        if (run.Lesson.Id is "T-R06" or "T-R07")
        {
            if (run.Lesson.Id == "T-R06")
                while (run.Elapsed < run.StealWindupStartsAt + .2 - 1e-9) run.Tick(Frame);
            Assert.True(run.ArmSteal(1, source));
        }
        for (var i = 0; i < 2700 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            var pad = run.Lesson.Id == "T-R07" && live.Throwing && live.ThrowBag == 2
                    && live.BallZ > Diamond.Rubber.Z && run.Match.Runners.Any(r => r.Bag == 3 && r.Phase == RunnerPhase.OnBag)
                ? new LivePadInput(KeysBag: 3, StickBag: 4)
                : run.Lesson.Id == "T-R08" && live.HoldsBall && !live.Throwing && run.HumanThrows.Count == 0
                ? new LivePadInput(KeysBag: 2, SouthDown: true)
                : (run.Lesson.Id is "T-R06" or "T-R07") && live.Active && i % 4 == 0
                    ? new LivePadInput(SouthDown: true) : LivePadInput.Dead;
            run.Tick(Frame, pad, source);
        }
    }

    [Theory]
    [InlineData("T-R06")]
    [InlineData("T-R07")]
    [InlineData("T-R08")]
    public void HumanStealAndCatcherCommandsMeetGeometricRunnerOutcomeThreeTimes(string id)
    {
        var run = Start(id);
        for (var n = 1; n <= 3; n++)
        {
            Drive(run);
            Assert.True(run.Feedback?.Success == true, $"{id}: {run.Feedback}; play {run.LastPlay?.Kind}; moves {string.Join(',',run.LastPlay?.Outcome?.Moves.Select(m => $"{m.FromBag}-{m.ToBag}") ?? [])}; throws {string.Join(',',run.HumanThrows)}");
            if (id == "T-R07")
            {
                Assert.Equal(2, run.LastPlay?.Outcome?.ThrowEndpoint?.DestinationBag);
                Assert.Equal(1, run.LastPlay?.RunsScored);
                Assert.Contains("Zig", run.LastPlay!.Scorers);
                Assert.Empty(run.HumanThrows); // the scripted opponent throw is never human evidence
            }
            Assert.Equal(n, run.Successes);
            Assert.Equal(n == 3, run.Passed);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            run.Retry();
        }
    }

    [Theory]
    [InlineData("T-R06")]
    [InlineData("T-R07")]
    [InlineData("T-R08")]
    public void CpuSourceCannotEarnStealLesson(string id)
    {
        var run = Start(id);
        if (id != "T-R08")
            Assert.False(run.ArmSteal(1, LivePlayCommandSource.Cpu));
        else
        {
            Drive(run, LivePlayCommandSource.Cpu);
            Assert.False(run.Feedback?.Success ?? false);
        }
    }
}
