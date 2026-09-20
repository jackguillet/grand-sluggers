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

    [Fact]
    public void HumanPickoffAndFollowUpThrowTagOriginalRunnerThreeTimes()
    {
        var run = Start("T-D05");
        for (var n = 1; n <= 3; n++)
        {
            Assert.True(run.Pickoff(1));
            for (var i = 0; i < 2700 && run.Phase == TutorialPhase.Attempt; i++)
            {
                var live = run.Match.LivePlay;
                var pad = live.HoldsBall && !live.Throwing && live.GlovePos == "1B" && run.HumanThrows.Count == 0
                    ? new LivePadInput(KeysBag: 2, SouthDown: true) : LivePadInput.Dead;
                run.Tick(Frame, pad);
            }
            Assert.True(run.Feedback?.Success == true, $"{run.Feedback}; {run.LastPlay?.Kind}; outs {string.Join(',',run.LastPlay?.Outcome?.OutsMade.Select(o => $"{o.Runner.Id}:{o.Type}:{o.Bag}") ?? [])}; throws {string.Join(',',run.HumanThrows)}");
            Assert.Equal(n, run.Successes);
            Assert.Equal(n == 3, run.Passed);
            Assert.Equal(run.Feedback, TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording()).Feedback);
            run.Retry();
        }
    }

    [Fact]
    public void WrongPickoffBagAndDeadFollowUpDoNotEarnRundownTag()
    {
        var run = Start("T-D05");
        Assert.False(run.Pickoff(1, LivePlayCommandSource.Cpu));
        Assert.True(run.Pickoff(2));
        Assert.False(run.Feedback?.Success ?? false);
        run.Retry();
        Assert.True(run.Pickoff(1));
        for (var i = 0; i < 2700 && run.Phase == TutorialPhase.Attempt; i++) run.Tick(Frame);
        Assert.False(run.Feedback?.Success ?? false);
    }

    [Fact]
    public void HumanRunnerWinsClosePlayAtThirdThreeTimes()
    {
        var run = Start("T-D06");
        for (var n = 1; n <= 3; n++)
        {
            var iconAt = -1;
            for (var i = 0; i < 2100 && run.Phase == TutorialPhase.Attempt; i++)
            {
                var live = run.Match.LivePlay;
                if (iconAt < 0 && live.CloseIcon) iconAt = i;
                var pad = iconAt >= 0 && i == iconAt + 2 ? new LivePadInput(SouthDown: true)
                    : i < 3 ? new LivePadInput(AllAdvance: true)
                    : i < 20 && i % 4 == 0 && !live.InClosePlay ? new LivePadInput(SouthDown: true)
                    : LivePadInput.Dead;
                run.Tick(Frame, pad);
            }
            Assert.True(run.Feedback?.Success == true, $"{run.Feedback}; icon {iconAt}; play {run.LastPlay?.Kind}; moves {string.Join(',',run.LastPlay?.Outcome?.Moves.Select(m => $"{m.Runner.Id}:{m.FromBag}-{m.ToBag}") ?? [])}");
            Assert.Equal(n, run.Successes);
            Assert.Equal(n == 3, run.Passed);
            Assert.Equal(run.Feedback, TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording()).Feedback);
            run.Retry();
        }
    }

    [Fact]
    public void DeadAndCpuPressesCannotWinRunnerCloseLesson()
    {
        var run = Start("T-D06");
        for (var i = 0; i < 2100 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var pad = i < 3 ? new LivePadInput(AllAdvance: true)
                : i < 20 && i % 4 == 0 ? new LivePadInput(SouthDown: true) : LivePadInput.Dead;
            run.Tick(Frame, pad);
        }
        Assert.False(run.Feedback?.Success ?? false);
        run.Retry();
        Assert.False(run.Demonstration);
        for (var i = 0; i < 2100 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            var pad = live.CloseIcon ? new LivePadInput(SouthDown: true)
                : i < 3 ? new LivePadInput(AllAdvance: true)
                : i < 20 && i % 4 == 0 ? new LivePadInput(SouthDown: true) : LivePadInput.Dead;
            run.Tick(Frame, pad, LivePlayCommandSource.Cpu);
        }
        Assert.False(run.Feedback?.Success ?? false);
    }

    [Fact]
    public void HumanFielderWinsCloseTagAtThirdThreeTimes()
    {
        var run = Start("T-D09");
        for (var n = 1; n <= 3; n++)
        {
            var iconAt = -1;
            for (var i = 0; i < 2100 && run.Phase == TutorialPhase.Attempt; i++)
            {
                var live = run.Match.LivePlay;
                if (iconAt < 0 && live.CloseIcon) iconAt = i;
                var pad = iconAt >= 0 && i == iconAt + 2 ? new LivePadInput(SouthDown: true)
                    : live.HoldsBall && !live.Throwing && run.HumanThrows.Count == 0
                        && run.Match.Runners.Any(r => r.FromBag == 2 && r.Feet > (TestRoot.Compact ? 35 : 45))
                        ? new LivePadInput(KeysBag: 3, SouthDown: true) : LivePadInput.Dead;
                run.Tick(Frame, pad);
            }
            Assert.True(run.Feedback?.Success == true, $"{run.Feedback}; icon {iconAt}; play {run.LastPlay?.Kind}; outs {string.Join(',',run.LastPlay?.Outcome?.OutsMade.Select(o => $"{o.Runner.Id}:{o.Type}:{o.Bag}") ?? [])}; throws {string.Join(',',run.HumanThrows)}");
            Assert.Equal(n, run.Successes);
            Assert.Equal(n == 3, run.Passed);
            Assert.Equal(run.Feedback, TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording()).Feedback);
            run.Retry();
        }
    }

    [Fact]
    public void ThrowWithoutHumanClosePressCannotEarnDefenderLesson()
    {
        var run = Start("T-D09");
        for (var i = 0; i < 2100 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            var pad = live.HoldsBall && !live.Throwing && run.HumanThrows.Count == 0
                && run.Match.Runners.Any(r => r.FromBag == 2 && r.Feet > (TestRoot.Compact ? 35 : 45))
                ? new LivePadInput(KeysBag: 3, SouthDown: true) : LivePadInput.Dead;
            run.Tick(Frame, pad);
        }
        Assert.False(run.Feedback?.Success ?? false);
        run.Retry();
        for (var i = 0; i < 2100 && run.Phase == TutorialPhase.Attempt; i++)
            run.Tick(Frame, new LivePadInput(KeysBag: 3, SouthDown: true), LivePlayCommandSource.Cpu);
        Assert.False(run.Feedback?.Success ?? false);
    }
}
