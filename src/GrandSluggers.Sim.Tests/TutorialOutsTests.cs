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
    public void CatchAndTwoHumanReturnThrowsMakeThreeRealOuts()
    {
        var run = Start("T-D08");
        for (var n = 1; n <= 3; n++)
        {
            for (var i = 0; i < 2400 && run.Phase == TutorialPhase.Attempt; i++)
            {
                var live = run.Match.LivePlay;
                var bag = run.HumanThrows.Count switch { 0 => 2, 1 => 1, _ => 0 };
                var pad = bag > 0 && live.HoldsBall && !live.Throwing
                    ? new LivePadInput(KeysBag: bag, SouthDown: true) : LivePadInput.Dead;
                run.Tick(Frame, pad);
            }
            Assert.True(run.Feedback?.Success == true, $"{run.Feedback}; {run.LastPlay?.Kind}; outs {string.Join(',', run.LastPlay?.Outcome?.OutsMade.Select(o => $"{o.Runner.Id}:{o.Type}:{o.Bag}") ?? [])}; throws {string.Join(',', run.HumanThrows)}");
            Assert.Equal(3, run.LastPlay?.Outcome?.OutsMade.Count);
            Assert.Equal(n, run.Successes);
            Assert.Equal(n == 3, run.Passed);
            Assert.Equal(run.Feedback, TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording()).Feedback);
            run.Retry();
        }
    }

    [Fact]
    public void OneReturnThrowOrCpuThrowsCannotEarnTriplePlay()
    {
        var run = Start("T-D08");
        for (var i = 0; i < 2400 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            var pad = run.HumanThrows.Count == 0 && live.HoldsBall && !live.Throwing
                ? new LivePadInput(KeysBag: 2, SouthDown: true) : LivePadInput.Dead;
            run.Tick(Frame, pad);
        }
        Assert.False(run.Feedback?.Success ?? false);
        run.Retry();
        for (var i = 0; i < 2400 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            var bag = live.HoldsBall && !live.Throwing ? (live.FirstThrowBag == 0 ? 2 : 1) : 0;
            run.Tick(Frame, bag > 0 ? new LivePadInput(KeysBag: bag, SouthDown: true) : LivePadInput.Dead,
                LivePlayCommandSource.Cpu);
        }
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

    [Theory]
    [InlineData(1.0 / 30)]
    [InlineData(1.0 / 60)]
    [InlineData(1.0 / 120)]
    public void HumanFielderWinsCloseTagAtThirdThreeTimes(double frame)
    {
        var run = Start("T-D09");
        for (var n = 1; n <= 3; n++)
        {
            var iconAt = -1.0;
            for (var i = 0; i < 70 / frame && run.Phase == TutorialPhase.Attempt; i++)
            {
                var live = run.Match.LivePlay;
                if (iconAt < 0 && live.CloseIcon) iconAt = run.Elapsed;
                var pad = iconAt >= 0 && run.Elapsed >= iconAt + 2.0 / 60
                    && run.Elapsed < iconAt + 2.0 / 60 + frame ? new LivePadInput(SouthDown: true)
                    : live.HoldsBall && !live.Throwing && run.HumanThrows.Count == 0
                        && run.Match.Runners.Any(r => r.FromBag == 2 && r.Feet > (TestRoot.Compact ? 35 : 45))
                        ? new LivePadInput(KeysBag: 3, SouthDown: true) : LivePadInput.Dead;
                run.Tick(frame, pad);
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

    [Fact]
    public void ThirdForceOutChangesHalfWithoutARecordedRunThreeTimes()
    {
        var run = Start("T-D07");
        for (var n = 1; n <= 3; n++)
        {
            Drive(run, 4);
            Assert.True(run.Feedback?.Success == true, $"{run.Feedback}; {run.LastPlay?.Kind}; outs {string.Join(',',run.LastPlay?.Outcome?.OutsMade.Select(o => $"{o.Runner.Id}:{o.Type}:{o.Bag}") ?? [])}");
            Assert.Equal(0, run.LastPlay?.RunsScored);
            Assert.Equal(n, run.Successes);
            Assert.Equal(n == 3, run.Passed);
            Assert.Equal(run.Feedback, TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording()).Feedback);
            run.Retry();
        }
    }

    [Fact]
    public void CrossedRunIsCanceledByLaterHumanThirdForce()
    {
        var run = Start("T-D07-T");
        for (var n = 1; n <= 3; n++)
        {
            var crossed = false;
            for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++)
            {
                var live = run.Match.LivePlay;
                crossed |= run.Match.Runners.Any(r => r.FromBag == 3 && r.Phase == RunnerPhase.Scored);
                var scorerNearHome = run.Match.Runners.Any(r => r.FromBag == 3
                    && r.Phase == RunnerPhase.Advancing && r.Feet / r.SegmentFt >= 0.82);
                var pad = scorerNearHome && live.HoldsBall && !live.Throwing && run.HumanThrows.Count == 0
                    ? new LivePadInput(KeysBag: 2, SouthDown: true) : LivePadInput.Dead;
                run.Tick(Frame, pad);
            }
            Assert.True(run.Feedback?.Success == true,
                $"{run.Feedback}; crossed {crossed}; play {run.LastPlay?.Kind}; throws {string.Join(',',run.HumanThrows)}; outs {string.Join(',',run.LastPlay?.Outcome?.OutsMade.Select(o => $"{o.Runner.Id}:{o.Type}:{o.Bag}") ?? [])}; runners {string.Join(',',run.Match.Runners.Select(r => $"{r.Who.Id}:{r.Bag}:{r.Feet:0.0}:{r.Phase}"))}");
            Assert.Equal(0, run.LastPlay?.RunsScored);
            Assert.Equal(n, run.Successes);
            Assert.Equal(n == 3, run.Passed);
            Assert.Equal(run.Feedback, TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording()).Feedback);
            run.Retry();
        }
    }

    [Fact]
    public void EarlyOrCpuForceDoesNotEarnCrossedRunLesson()
    {
        var run = Start("T-D07-T");
        Drive(run, 2);
        Assert.False(run.Feedback?.Success ?? false);
        run.Retry();
        for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            var scorerNearHome = run.Match.Runners.Any(r => r.FromBag == 3
                && r.Phase == RunnerPhase.Advancing && r.Feet / r.SegmentFt >= 0.82);
            var pad = scorerNearHome && live.HoldsBall && !live.Throwing
                ? new LivePadInput(KeysBag: 2, SouthDown: true) : LivePadInput.Dead;
            run.Tick(Frame, pad, LivePlayCommandSource.Cpu);
        }
        Assert.False(run.Feedback?.Success ?? false);
    }

    [Fact]
    public void CrossedRunCountsBeforeLaterHumanNonforceThirdTag()
    {
        var run = Start("T-D07-C");
        for (var n = 1; n <= 3; n++)
        {
            var crossed = false;
            var iconSeen = false;
            for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++)
            {
                var live = run.Match.LivePlay;
                crossed |= run.Match.Runners.Any(r => r.FromBag == 3 && r.Phase == RunnerPhase.Scored);
                iconSeen |= live.CloseIcon;
                var scorerNearHome = run.Match.Runners.Any(r => r.FromBag == 3
                    && r.Phase == RunnerPhase.Advancing && r.Feet / r.SegmentFt >= 0.82);
                var pad = live.CloseIcon ? new LivePadInput(SouthDown: true)
                    : scorerNearHome && live.HoldsBall && !live.Throwing && run.HumanThrows.Count == 0
                        ? new LivePadInput(KeysBag: 3, SouthDown: true) : LivePadInput.Dead;
                run.Tick(Frame, pad);
            }
            Assert.True(run.Feedback?.Success == true,
                $"{run.Feedback}; crossed {crossed}; icon {iconSeen}; play {run.LastPlay?.Kind}; throws {string.Join(',',run.HumanThrows)}; outs {string.Join(',',run.LastPlay?.Outcome?.OutsMade.Select(o => $"{o.Runner.Id}:{o.Type}:{o.Bag}") ?? [])}; runners {string.Join(',',run.Match.Runners.Select(r => $"{r.Who.Id}:{r.Bag}:{r.Feet:0.0}:{r.Phase}"))}");
            Assert.Equal(1, run.LastPlay?.RunsScored);
            Assert.Equal(n, run.Successes);
            Assert.Equal(n == 3, run.Passed);
            Assert.Equal(run.Feedback, TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording()).Feedback);
            run.Retry();
        }
    }

    [Fact]
    public void WrongBagOrCpuTagCannotEarnCrossedRunCount()
    {
        var run = Start("T-D07-C");
        for (var attempt = 0; attempt < 2; attempt++)
        {
            for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++)
            {
                var live = run.Match.LivePlay;
                var scorerNearHome = run.Match.Runners.Any(r => r.FromBag == 3
                    && r.Phase == RunnerPhase.Advancing && r.Feet / r.SegmentFt >= 0.82);
                var pad = live.CloseIcon ? new LivePadInput(SouthDown: true)
                    : scorerNearHome && live.HoldsBall && !live.Throwing && live.FirstThrowBag == 0
                        ? new LivePadInput(KeysBag: attempt == 0 ? 2 : 3, SouthDown: true)
                        : LivePadInput.Dead;
                run.Tick(Frame, pad, attempt == 0 ? LivePlayCommandSource.Human : LivePlayCommandSource.Cpu);
            }
            Assert.False(run.Feedback?.Success ?? false);
            run.Retry();
        }
    }

    [Fact]
    public void WrongBagAndCpuThirdOutDoNotEarnScoringLesson()
    {
        var run = Start("T-D07");
        Drive(run, 1);
        Assert.False(run.Feedback?.Success ?? false);
        run.Retry();
        Drive(run, 4, LivePlayCommandSource.Cpu);
        Assert.False(run.Feedback?.Success ?? false);
    }
}
