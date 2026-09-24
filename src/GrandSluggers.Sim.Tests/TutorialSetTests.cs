using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class TutorialSetTests
{
    readonly ContentCatalog _content = Shipped.Content;
    const double Frame = 1.0 / 60;

    [Fact]
    public void ChargeThenPickoffIsATypedBalkFailureAndReplayKeepsTheCharge()
    {
        var run = Start("T-P08");
        Assert.False(run.BeginPitchCharge(LivePlayCommandSource.Cpu));
        Assert.True(run.BeginPitchCharge());
        Assert.True(run.Pickoff(1));
        Assert.False(run.Feedback!.Success);
        Assert.Equal("balk-after-charge", run.Feedback.Code);
        Assert.Equal(PlayKind.Balk, run.LastPlay!.Kind);
        Assert.Empty(run.LastPlay.Outcome!.OutsMade);
        Assert.Equal(0, run.Successes);
        var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
        Assert.Equal(run.Feedback, replay.Feedback);
        Assert.Equal(PlayKind.Balk, replay.LastPlay!.Kind);
        run.Retry();
        DrivePickoff(run, 1);
        Assert.True(run.Feedback!.Success);
    }

    TutorialSession Start(string id)
    {
        var run = new TutorialSession(_content, TutorialCatalog.Load(_content), id);
        run.Begin();
        return run;
    }

    static void DrivePickoff(TutorialSession run, int bag, LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        Assert.True(run.Pickoff(bag, source));
        for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++) run.Tick(Frame);
    }

    [Fact]
    public void TiredStarterCanBeReplacedByHumanWithFreshNamedFielder()
    {
        var run = Start("T-P07");
        for (var n = 1; n <= 3; n++)
        {
            Assert.True(run.Match.PitcherTired);
            var old = run.Match.Pitcher.Id;
            var next = run.Match.DefenseRoster.First(c => c.Id != old && c.Stats.Pitch >= 6);
            Assert.True(run.SwapPitcher(next.Id));
            Assert.True(run.Feedback?.Success == true, run.Feedback?.Detail);
            Assert.Equal(next.Id, run.Match.Pitcher.Id);
            Assert.Equal(n, run.Successes);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            run.Retry();
        }
    }

    [Fact]
    public void HumanPickoffChecksExposedRunnerWithRealFirstBaseReception()
    {
        var run = Start("T-P08");
        for (var n = 1; n <= 3; n++)
        {
            DrivePickoff(run, 1);
            Assert.True(run.Feedback?.Success == true, $"{run.Feedback}; {run.LastPlay?.Kind}");
            Assert.Equal(n, run.Successes);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            run.Retry();
        }
    }

    [Fact]
    public void CpuSetCommandsAndWrongPickoffBagDoNotEarnCredit()
    {
        var swap = Start("T-P07");
        Assert.False(swap.SwapPitcher(swap.Match.DefenseRoster.First(c => c.Id != swap.Match.Pitcher.Id).Id,
            LivePlayCommandSource.Cpu));
        Assert.Equal(TutorialPhase.Attempt, swap.Phase);
        var pickoff = Start("T-P08");
        DrivePickoff(pickoff, 2);
        Assert.False(pickoff.Feedback?.Success ?? false);
        pickoff.Retry();
        Assert.False(pickoff.Pickoff(1, LivePlayCommandSource.Cpu));
        Assert.Equal(TutorialPhase.Attempt, pickoff.Phase);
    }
}
