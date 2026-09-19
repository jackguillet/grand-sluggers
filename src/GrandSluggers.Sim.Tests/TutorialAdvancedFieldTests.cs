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

    static void Drive(TutorialSession run, bool act, LivePlayCommandSource source = LivePlayCommandSource.Human, bool wrongBag = false)
    {
        var jumped = false;
        var wallSeen = false;
        for (var i = 0; i < 1900 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            wallSeen |= live.Events.Contains(LiveEvent.WallCarom);
            var pad = LivePadInput.Dead;
            if (act && run.Lesson.Id == "T-F11" && live.HoldsBall && !live.Throwing)
                pad = new(KeysBag: wrongBag ? 2 : 3, SouthDown: true);
            else if (act && run.Lesson.Id == "T-F11" && !wallSeen && live.ElapsedSeconds > .5)
                pad = new(StickX: -1, StickY: 0);
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
}
