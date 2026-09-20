using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

[Trait("Rows", "compact")]
public sealed class TutorialAdvancedFieldTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    const double Frame = 1.0 / 60;

    TutorialSession Start(string id, bool demo = false)
    {
        var run = new TutorialSession(_content, TutorialCatalog.Load(_content), id);
        run.Begin(demo);
        return run;
    }

    static void Drive(TutorialSession run, bool act, LivePlayCommandSource source = LivePlayCommandSource.Human,
        bool wrongBag = false, bool skipOnward = false)
    {
        var jumped = false;
        var wallSeen = false;
        var relayStage = 0;
        var laserArmed = false;
        for (var i = 0; i < 1900 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            wallSeen |= live.Events.Contains(LiveEvent.WallCarom);
            var pad = LivePadInput.Dead;
            if (act && run.Lesson.Id == "T-F11" && live.HoldsBall && !live.Throwing)
                pad = new(KeysBag: wrongBag ? 2 : 3, SouthDown: true);
            else if (act && run.Lesson.Id == "T-F11" && !wallSeen && live.ElapsedSeconds > .5)
                pad = new(StickX: -1, StickY: 0);
            else if (act && run.Lesson.Id == "T-F09" && wallSeen && !live.HoldsBall)
            {
                var dx = live.BallX - live.GloveX;
                var dz = live.BallZ - live.GloveZ;
                var length = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
                pad = new(StickX: dx / length, StickY: dz / length);
            }
            else if (act && run.Lesson.Id == "T-F09" && !wallSeen && live.ElapsedSeconds > .5)
                pad = new(StickX: -1, StickY: 0);
            else if (act && run.Lesson.Id == "T-F13" && live.HoldsBall && !live.Throwing)
                pad = new(StickY: 1);
            else if (act && run.Lesson.Id == "T-F10" && live.HoldsBall && !live.Throwing)
                pad = new(KeysBag: wrongBag ? 2 : 1, SouthDown: true);
            else if (act && run.Lesson.Id == "T-F15" && live.HoldsBall && live.GlovePos == "CF" && !live.Throwing)
            {
                pad = laserArmed ? new(SouthDown: true) : new(KeysBag: 4);
                laserArmed = true;
            }
            else if (act && run.Lesson.Id is "T-F07" or "T-F14" or "T-F08" or "T-F08-R" or "T-F08-C" or "T-G02")
            {
                if (relayStage == 0 && live.HoldsBall && live.GlovePos == "CF")
                {
                    pad = new(KeysBag: run.Lesson.Id == "T-F08-R" ? 3 : 4);
                    relayStage = 1;
                }
                else if (relayStage == 1)
                {
                    pad = new(Cutoff: true);
                    relayStage = 2;
                }
                else if (!skipOnward && relayStage == 2 && live.Throwing && live.ThrowBag == 0
                    && run.Match.Rules.Fielding.Throw.RelayAutoContinue == 0
                    && live.ThrowDur - live.ThrowT <= .15)
                {
                    pad = new(SouthDown: true);
                    relayStage = 3;
                }
                else if (relayStage == 3 && live.ThrowQueued && run.Lesson.Id == "T-F08-R")
                {
                    pad = new(KeysBag: 4);
                    relayStage = 4;
                }
                else if (relayStage == 3 && live.ThrowQueued && run.Lesson.Id == "T-F08-C")
                {
                    pad = new(Cancel: true);
                    relayStage = 4;
                }
            }
            else if (act && run.Lesson.Id == "T-F12" && live.Preview is { } fly)
            {
                var plant = FlyCatch.WallPlant(fly, run.Match.Park, run.Match.Rules);
                var dx = plant.X - live.GloveX;
                var dz = plant.Z - live.GloveZ;
                var len = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
                var west = !jumped && live.BuddyWindow;
                pad = new(StickX: dx / len, StickY: dz / len, WestDown: west);
                jumped |= west;
            }
            run.Tick(Frame, pad, source);
        }
    }

    [Theory]
    [InlineData("T-F11")]
    [InlineData("T-F12")]
    public void LessonRequiresHumanActionAndARealResult(string id)
    {
        var run = Start(id);
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            Drive(run, act: true);
            Assert.True(run.Feedback?.Success == true, $"{id}: {run.Feedback}; at {run.Elapsed:0.00}; play {run.LastPlay?.Kind}; feat {run.LastPlay?.Outcome?.DefensiveFeat}");
            Assert.Equal(attempt, run.Successes);
            Assert.Equal(attempt == 3, run.Passed);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            run.Retry();
            Assert.Empty(run.HumanThrows);
        }
    }

    [Theory]
    [InlineData("T-F11")]
    [InlineData("T-F12")]
    public void DeadInputAndCpuCommandsCannotEarnCredit(string id)
    {
        var dead = Start(id);
        Drive(dead, act: false);
        Assert.False(dead.Feedback?.Success ?? false);
        var cpu = Start(id);
        Drive(cpu, act: true, source: LivePlayCommandSource.Cpu);
        Assert.False(cpu.Feedback?.Success ?? false);
        var demo = Start(id, demo: true);
        Drive(demo, act: true);
        Assert.False(demo.Feedback?.Success ?? false);
    }

    [Fact]
    public void AThrowToTheWrongBagDoesNotCompleteTheCaromLesson()
    {
        var run = Start("T-F11");
        Drive(run, act: true, wrongBag: true);
        Assert.Equal("wrong-carom-bag", run.Feedback?.Code);
        Assert.Equal(0, run.Successes);
    }

    [Fact]
    public void RetryNeedsAFreshWallCaromEvent()
    {
        if (TestRoot.Compact) return; // On C80 the unattended contact also reaches the wall.
        var run = Start("T-F11");
        Drive(run, act: true);
        Assert.Equal("carom-returned", run.Feedback?.Code);
        Assert.Equal(1, run.Successes);
        run.Retry();
        Drive(run, act: false);
        Assert.Equal("no-carom", run.Feedback?.Code);
        Assert.Equal(1, run.Successes);
    }

    [Fact]
    public void BallDashIsEarnedOnlyByHumanCarryOnTheProfileWithAnEligibleHolder()
    {
        if (!TestRoot.Compact)
        {
            Assert.DoesNotContain(_content.Characters.Values, FieldAbilities.HasBallDash);
            return;
        }
        var run = Start("T-F13");
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            Drive(run, act: true);
            Assert.Equal("ball-dash-carried", run.Feedback?.Code);
            Assert.True(run.Feedback!.Success);
            Assert.Equal(attempt, run.Successes);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            run.Retry();
        }
        var cpu = Start("T-F13");
        Drive(cpu, act: true, source: LivePlayCommandSource.Cpu);
        Assert.Equal(0, cpu.Successes);
        var dead = Start("T-F13");
        Drive(dead, act: false);
        Assert.Equal(0, dead.Successes);
    }

    [Theory]
    [InlineData("T-F07")]
    [InlineData("T-F14")]
    public void CutoffReceiverAndOnwardThrowUseTheRealRelay(string id)
    {
        var run = Start(id);
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            Drive(run, act: true);
            Assert.True(run.Feedback?.Success == true, $"{id}: {run.Feedback}; play {run.LastPlay?.Kind}; throws {string.Join(',', run.HumanThrows)}");
            Assert.Equal(attempt, run.Successes);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            run.Retry();
        }
        var dead = Start(id);
        Drive(dead, act: false);
        Assert.False(dead.Feedback?.Success ?? false);
        var cpu = Start(id);
        Drive(cpu, act: true, source: LivePlayCommandSource.Cpu);
        Assert.False(cpu.Feedback?.Success ?? false);
        if (TestRoot.Compact)
        {
            var noSecond = Start(id);
            Drive(noSecond, act: true, skipOnward: true);
            Assert.Equal("relay-not-completed", noSecond.Feedback?.Code);
        }
    }

    [Fact]
    public void LaserHolderMustReceiveAHumanHomeThrowCommandAndTheRealBoost()
    {
        var run = Start("T-F15");
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            Drive(run, act: true);
            Assert.True(run.Feedback?.Success == true, $"{run.Feedback}; play {run.LastPlay?.Kind}");
            Assert.Equal("laser-home", run.Feedback?.Code);
            Assert.Equal(attempt, run.Successes);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            run.Retry();
        }
        var dead = Start("T-F15");
        Drive(dead, act: false);
        Assert.Equal(0, dead.Successes);
        var cpu = Start("T-F15");
        Drive(cpu, act: true, source: LivePlayCommandSource.Cpu);
        Assert.Equal(0, cpu.Successes);
    }

    [Fact]
    public void PressingThrowAfterAnExistingFlightDoesNotClaimTheLaserRelease()
    {
        var run = Start("T-F15");
        var pressedDuringFlight = false;
        var armThird = false;
        var sentThird = false;
        for (var i = 0; i < 1900 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            var late = sentThird && !pressedDuringFlight && live.Throwing;
            var pad = LivePadInput.Dead;
            if (!armThird && live.HoldsBall && live.GlovePos == "CF")
            {
                pad = new LivePadInput(KeysBag: 3);
                armThird = true;
            }
            else if (armThird && !sentThird && live.HoldsBall && live.GlovePos == "CF")
            {
                pad = new LivePadInput(SouthDown: true);
                sentThird = true;
            }
            else if (late) pad = new LivePadInput(SouthDown: true);
            run.Tick(Frame, pad);
            pressedDuringFlight |= late;
        }
        Assert.True(pressedDuringFlight);
        Assert.Equal(0, run.Successes);
    }

    [Theory]
    [InlineData("T-F08", "relay-buffered")]
    [InlineData("T-F08-R", "relay-retargeted")]
    [InlineData("T-F08-C", "relay-cancelled")]
    public void BufferedRelayQueueEditsNeedHumanEvidence(string id, string code)
    {
        if (!TestRoot.Compact) return;
        var run = Start(id);
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            Drive(run, act: true);
            Assert.Equal(code, run.Feedback?.Code);
            Assert.Equal(attempt, run.Successes);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            run.Retry();
        }
        var dead = Start(id);
        Drive(dead, act: false);
        Assert.Equal(0, dead.Successes);
        var cpu = Start(id);
        Drive(cpu, act: true, source: LivePlayCommandSource.Cpu);
        Assert.Equal(0, cpu.Successes);
        var noQueue = Start(id);
        Drive(noQueue, act: true, skipOnward: true);
        Assert.Equal(0, noQueue.Successes);
    }

    [Fact]
    public void GoodChemistryIsEarnedFromTheHumanCutoffFeedAndRealThrowSpeed()
    {
        var run = Start("T-G02");
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            Drive(run, act: true);
            Assert.Equal("chemistry-throw", run.Feedback?.Code);
            Assert.Equal(attempt, run.Successes);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            run.Retry();
        }
        var dead = Start("T-G02");
        Drive(dead, act: false);
        Assert.Equal(0, dead.Successes);
        var cpu = Start("T-G02");
        Drive(cpu, act: true, source: LivePlayCommandSource.Cpu);
        Assert.Equal(0, cpu.Successes);
    }

    [Fact]
    public void LooseWallBallRequiresHumanChaseAndActualScoop()
    {
        var run = Start("T-F09");
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            Drive(run, act: true);
            Assert.True(run.Feedback?.Code == "loose-recovered", run.Feedback?.ToString());
            Assert.Equal(attempt, run.Successes);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            run.Retry();
        }
        var dead = Start("T-F09");
        Drive(dead, act: false);
        Assert.Equal(0, dead.Successes);
        var cpu = Start("T-F09");
        Drive(cpu, act: true, source: LivePlayCommandSource.Cpu);
        Assert.Equal(0, cpu.Successes);
    }

    [Fact]
    public void ThrowToUncoveredFirstWaitsForActualCoverReceiver()
    {
        var run = Start("T-F10");
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            Drive(run, act: true);
            Assert.Equal("cover-arrived", run.Feedback?.Code);
            Assert.Equal(attempt, run.Successes);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            run.Retry();
        }
        var dead = Start("T-F10");
        Drive(dead, act: false);
        Assert.Equal(0, dead.Successes);
        var cpu = Start("T-F10");
        Drive(cpu, act: true, source: LivePlayCommandSource.Cpu);
        Assert.Equal(0, cpu.Successes);
        var wrong = Start("T-F10");
        Drive(wrong, act: true, wrongBag: true);
        Assert.Equal(0, wrong.Successes);
    }
}
