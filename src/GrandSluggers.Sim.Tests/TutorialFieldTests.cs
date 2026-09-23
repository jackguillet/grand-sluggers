using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class TutorialFieldTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    const double Frame = 1.0 / 60;

    TutorialSession Start(string id, TutorialProgress? progress = null, bool demonstration = false)
    {
        var run = new TutorialSession(_content, TutorialCatalog.Load(_content), id, progress);
        run.Begin(demonstration);
        return run;
    }

    static int Bag(string id) => id switch
    {
        "T-F03" => 1, "T-F03-2" => 2, "T-F03-3" => 3, "T-F03-H" => 4, _ => 0
    };

    static void Drive(TutorialSession run, bool wrong = false, bool noHuman = false, bool noAction = false, bool neutralJump = false)
    {
        var jumped = false;
        for (var i = 0; i < 1900 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            var pad = LivePadInput.Dead;
            if (!noAction)
            {
                if (run.Lesson.Id == "T-F02" && live.ElapsedSeconds >= .4 && !live.HoldsBall)
                {
                    var dx = live.BallX - live.GloveX;
                    var dz = live.BallZ - live.GloveZ;
                    var len = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
                    pad = new(StickX: dx / len, StickY: dz / len, SouthDown: true);
                }
                else if (Bag(run.Lesson.Id) is var bag and > 0 && live.HoldsBall && !live.Throwing)
                    pad = new(KeysBag: wrong ? bag == 1 ? 2 : 1 : bag, SouthDown: true);
                else if (run.Lesson.Id is "T-F04" or "T-F06" && live.Preview is { } preview)
                {
                    var hang = preview.HangTimeSec;
                    if (live.ElapsedSeconds >= .4 && !live.HoldsBall)
                    {
                        var target = FlyCatch.ChaseTarget(preview, run.Match.Park, run.Match.Rules);
                        var dx = target.X - live.GloveX;
                        var dz = target.Z - live.GloveZ;
                        var len = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
                        var mag = len > 2 ? 1 : .21;
                        pad = new(StickX: dx / len * mag, StickY: dz / len * mag,
                            SouthDown: run.Lesson.Id == "T-F06" && wrong && live.ElapsedSeconds >= hang - .6,
                            WestDown: (run.Lesson.Id == "T-F06" && !wrong || run.Lesson.Id == "T-F04" && wrong) && !jumped && live.ElapsedSeconds >= hang - .6);
                        if (neutralJump && pad.WestDown) pad = pad with { StickX = 0, StickY = 0 };
                        if (pad.WestDown) jumped = true;
                    }
                }
            }
            run.Tick(Frame, pad, noHuman ? LivePlayCommandSource.Cpu : LivePlayCommandSource.Human);
        }
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void HumanActionAndRealLiveResultEarnThreeSeparateAttempts(string id)
    {
        var run = Start(id);
        for (var n = 1; n <= 3; n++)
        {
            Drive(run);
            Assert.True(run.Feedback?.Success == true, $"{id}: {run.Feedback}; at {run.Elapsed:0.00}; play {run.LastPlay?.Kind}; throws {string.Join(',', run.HumanThrows)}");
            Assert.Equal(n, run.Successes);
            Assert.Equal(n == 3, run.Passed);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            Assert.Equal(run.HumanThrows, replay.HumanThrows);
            run.Retry();
            Assert.Empty(run.HumanThrows);
            Assert.Equal(0, run.Match.LivePlay.ElapsedSeconds);
        }
    }

    public static TheoryData<string> Ids => new() { "T-F02", "T-F03", "T-F03-2", "T-F03-3", "T-F03-H", "T-F04", "T-F06" };

    [Theory]
    [MemberData(nameof(Ids))]
    public void CpuOrDeadInputCannotEarnCredit(string id)
    {
        var run = Start(id);
        Drive(run, noAction: true);
        Assert.False(run.Feedback?.Success ?? false);
        Assert.Equal(0, run.Successes);
        run.Retry();
        Drive(run, noHuman: true);
        Assert.False(run.Feedback?.Success ?? false);
        Assert.Equal(0, run.Successes);
    }

    [Theory]
    [InlineData("T-F03")]
    [InlineData("T-F03-2")]
    [InlineData("T-F03-3")]
    [InlineData("T-F03-H")]
    public void WrongNamedBagNeverCountsEvenIfItReceivesTheBall(string id)
    {
        var run = Start(id);
        Drive(run, wrong: true);
        Assert.Equal("wrong-bag", run.Feedback?.Code);
        Assert.False(run.Feedback!.Success);
        Assert.Equal(0, run.Successes);
    }

    [Theory]
    [InlineData("T-F04")]
    [InlineData("T-F06")]
    public void WrongAerialButtonCannotPass(string id)
    {
        var run = Start(id);
        Drive(run, wrong: true);
        Assert.False(run.Feedback?.Success ?? false);
        Assert.Equal(0, run.Successes);
    }

    [Fact]
    public void NeutralStickWestStillOwnsTheCompletedJumpCatch()
    {
        var run = Start("T-F06");
        Drive(run, neutralJump: true);
        Assert.Contains(run.Inputs, input => input.Field is { WestDown: true, StickX: 0, StickY: 0 });
        Assert.Equal(DefensiveFeat.Jump, run.LastPlay?.Outcome?.DefensiveFeat);
        Assert.Equal("jumping-out", run.Feedback?.Code);
        Assert.True(run.Feedback!.Success);
        var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
        Assert.Equal(run.Feedback, replay.Feedback);
    }

    [Fact]
    public void ArmingWithoutReleaseCannotEarnAThrow()
    {
        var run = Start("T-F03-2");
        for (var i = 0; i < 1900 && run.Phase == TutorialPhase.Attempt; i++)
            run.Tick(Frame, run.Match.LivePlay.HoldsBall ? new(KeysBag: 2) : LivePadInput.Dead);
        Assert.False(run.Feedback!.Success);
        Assert.Empty(run.HumanThrows);
    }

    [Theory]
    [InlineData("T-F01")]
    [InlineData("T-F02")]
    public void OneSteeringFrameThenAssistedPursuitCannotEarnManualPickup(string id)
    {
        var run = Start(id);
        var steered = false;
        for (var i = 0; i < 1900 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            var pad = LivePadInput.Dead;
            if (!steered && live.ElapsedSeconds >= .4)
            {
                var dx = live.BallX - live.GloveX;
                var dz = live.BallZ - live.GloveZ;
                var len = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
                pad = new(StickX: dx / len, StickY: dz / len, SouthDown: true);
                steered = true;
            }
            run.Tick(Frame, pad);
        }
        Assert.True(steered);
        Assert.Equal("assisted-pickup", run.Feedback?.Code);
        Assert.False(run.Feedback!.Success);
    }

    [Fact]
    public void HumanReleaseCanBeReceivedOnLaterSystemFrames()
    {
        var run = Start("T-F03-2");
        for (var i = 0; i < 600 && run.Phase == TutorialPhase.Attempt && run.HumanThrows.Count == 0; i++)
            run.Tick(Frame, run.Match.LivePlay.HoldsBall ? new(KeysBag: 2, SouthDown: true) : LivePadInput.Dead);
        Assert.Equal(new[] { 2 }, run.HumanThrows);
        Assert.Equal(TutorialPhase.Attempt, run.Phase); // release itself is not receipt
        for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++)
            run.Tick(Frame, LivePadInput.Dead, LivePlayCommandSource.Cpu);
        Assert.Equal("throw-second", run.Feedback?.Code);
        Assert.True(run.Feedback!.Success);
    }

    [Fact]
    public void BufferedRecoveryPressKeepsHumanOwnershipWhenLaterTicksAreCpu()
    {
        var catalog = TutorialCatalog.Load(_content);
        var setup = catalog.Setups.Single(s => s.Id == "T-F03-2");
        // This regression specifically needs a retained hot pickup, independent of the ordinary lesson's pace.
        setup.Balls["shipped"] = new TutorialBall(0, 145, -3, -18);
        var run = new TutorialSession(_content, catalog, "T-F03-2");
        run.Begin();
        for (var i = 0; i < 600 && !run.Match.LivePlay.HoldsBall && run.Phase == TutorialPhase.Attempt; i++)
            run.Tick(Frame);
        Assert.True(run.Match.LivePlay.HoldsBall);
        var recovery = run.Match.LivePlay.RecoilT;
        run.Tick(Frame, new(KeysBag: 2, SouthDown: true));
        Assert.True(recovery > 0);
        Assert.Contains(LiveEvent.ThrowQueued, run.Match.LivePlay.Events);
        Assert.Equal(0, run.Match.LivePlay.QueuedThrowBag); // recovery buffer, not an onward relay
        Assert.Empty(run.HumanThrows); // no release yet
        for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++)
            run.Tick(Frame, LivePadInput.Dead, LivePlayCommandSource.Cpu);
        Assert.Equal(new[] { 2 }, run.HumanThrows);
        Assert.Equal("throw-second", run.Feedback?.Code);
        Assert.True(run.Feedback!.Success);
        var replay = TutorialSession.Replay(_content, catalog, run.Recording());
        Assert.Equal(run.Feedback, replay.Feedback);
        Assert.Equal(run.HumanThrows, replay.HumanThrows);
    }

    [Fact]
    public void DemonstrationAndFailedAttemptDoNotErasePriorPractice()
    {
        var progress = new TutorialProgress();
        var first = Start("T-F03", progress); Drive(first);
        Assert.Equal(1, first.Successes);
        var demo = Start("T-F03", progress, demonstration: true); Drive(demo);
        Assert.False(demo.Feedback?.Success ?? false);
        Assert.Equal(1, demo.Successes);
        var wrong = Start("T-F03", progress); Drive(wrong, wrong: true);
        Assert.False(wrong.Feedback!.Success);
        Assert.Equal(1, wrong.Successes);
    }
}
