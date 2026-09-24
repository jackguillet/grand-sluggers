using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class TutorialSessionTests
{
    readonly ContentCatalog _content = Shipped.Content;
    const double Frame = 1.0 / 60;
    TutorialSession Start(string id)
    {
        var run = new TutorialSession(_content, TutorialCatalog.Load(_content), id);
        run.Begin(); return run;
    }

    [Theory]
    [InlineData("T-P01", false)]
    [InlineData("T-P03", true)]
    public void PitchUsesTheRealCrossingAndRetriesAFreshMatch(string id, bool changeup)
    {
        var run = Start(id);
        var original = run.Match;
        Assert.True(run.Pitch(new(changeup ? PitchFamily.Changeup : PitchFamily.Fastball, 0, false, AimX: 4)));
        Assert.False(run.Feedback!.Success);
        run.Retry();
        Assert.NotSame(original, run.Match);
        Assert.Equal(0, run.Match.Balls); Assert.Equal(0, run.Match.Strikes);
        Assert.Empty(run.Inputs);
        Assert.True(run.Pitch(new(changeup ? PitchFamily.Changeup : PitchFamily.Fastball, 0, false)));
        Assert.True(run.Feedback!.Success, run.Feedback.Detail);
        Assert.Equal(1, run.Successes);
        Assert.False(run.Passed);
    }

    [Fact]
    public void FastballStrikeCannotPassChangeupAndCpuCannotPassPitch()
    {
        var run = Start("T-P03");
        Assert.False(run.Pitch(new("changeup", 0, false), LivePlayCommandSource.Cpu));
        Assert.Equal(TutorialPhase.Attempt, run.Phase);
        run.Pitch(new("fastball", 0, false));
        Assert.Equal("use-changeup", run.Feedback!.Code);
        Assert.False(run.Feedback.Success);
    }

    /// <summary>
    /// The family the real mound cycle hands the delivery after <paramref name="presses"/> presses
    /// from the SET's fastball start, against the lesson's own match: its pitcher on the mound and
    /// its rules table (#876). A lesson that names a family its pitcher cannot reach is unwinnable
    /// in the window even when a hand-built command would pass the headless verdict.
    /// </summary>
    static string Cycled(TutorialSession run, int presses)
    {
        var state = PitchSelectionState.Reset;
        var button = default(ChargeButtonState);
        for (var i = 0; i < presses; i++)
            state = run.Match.SelectPitch(state, cyclePressed: true, selectable: true, button, default).Next;
        var press = ChargeButton.Advance(button, pressed: true, held: true, released: true,
            deltaSeconds: Frame, secondsToFull: 0.55);
        var thrown = run.Match.SelectPitch(state, cyclePressed: false, selectable: true, button, press);
        Assert.True(thrown.Committed);
        return thrown.Family;
    }

    [Fact]
    public void ChangeupLessonsPitcherOwnsTheChangeupOnePressFromTheFastball()
    {
        // T-P03 names hex (fastball, changeup, curveball): the home captain vale throws curveball /
        // slider and owns no changeup, so one press on vale's mound selected the curveball (#876).
        var run = Start("T-P03");
        Assert.Equal("hex", run.Match.Pitcher.Id);
        Assert.Equal(PitchFamily.Changeup, run.Match.Pitcher.Repertoire.Second);
        Assert.Equal(PitchFamily.Fastball, Cycled(run, 0));
        var family = Cycled(run, 1);
        Assert.Equal(PitchFamily.Changeup, family);
        Assert.True(run.Pitch(new(family, 0, false)));
        Assert.True(run.Feedback!.Success, run.Feedback.Detail);
    }

    [Fact]
    public void ThirdPitchLessonAwardsTheThirdSlotAfterTwoCyclePressesAndNothingElse()
    {
        var run = Start("T-P10");
        var repertoire = run.Match.Pitcher.Repertoire;
        Assert.Equal("vale", run.Match.Pitcher.Id);
        // The fastball and the second pitch are strikes the lesson refuses.
        foreach (var presses in new[] { 0, 1, 3 })
        {
            var wrong = Cycled(run, presses);
            Assert.NotEqual(repertoire[2], wrong);
            Assert.True(run.Pitch(new(wrong, 0, false)));
            Assert.False(run.Feedback!.Success);
            Assert.Equal("use-third-pitch", run.Feedback.Code);
            run.Retry();
        }
        // A star pitch is not an ordinary slot.
        Assert.True(run.Pitch(new(repertoire[2], 0, true)));
        Assert.False(run.Feedback!.Success);
        run.Retry();
        // A miss with the right pitch still fails on the zone.
        var third = Cycled(run, 2);
        Assert.Equal(repertoire.Third, third);
        Assert.True(run.Pitch(new(third, 0, false, AimX: 4)));
        Assert.Equal("outside-zone", run.Feedback!.Code);
        run.Retry();
        Assert.False(run.Pitch(new(third, 0, false), LivePlayCommandSource.Cpu));
        Assert.True(run.Pitch(new(third, 0, false)));
        Assert.True(run.Feedback!.Success, run.Feedback.Detail);
        Assert.Equal(1, run.Successes);
    }

    [Theory]
    [InlineData("T-P03", 1)]
    [InlineData("T-P10", 2)]
    public void TheCycleLessonsPassThreeTimesThroughTheRealCycle(string id, int presses)
    {
        var run = Start(id);
        for (var i = 0; i < 3; i++)
        {
            if (i > 0) run.Retry();
            Assert.True(run.Pitch(new(Cycled(run, presses), 0, false)));
            Assert.True(run.Feedback!.Success, run.Feedback.Detail);
        }
        Assert.True(run.Passed);
    }

    [Fact]
    public void SlapRequiresFairUnchargedHumanContact()
    {
        var run = Start("T-B01");
        Assert.False(run.Swing(new(true, 0, 0, false), LivePlayCommandSource.Cpu));
        run.Swing(new(true, 1, 0, false));
        Assert.False(run.Feedback!.Success);
        run.Retry(); run.Swing(new(true, 0, 50, false));
        Assert.False(run.Feedback!.Success);
        run.Retry(); run.Swing(new(true, 0, 0, false, Bunt: true));
        Assert.False(run.Feedback!.Success);
        run.Retry(); run.Swing(new(true, 0, 0, false));
        Assert.True(run.Feedback!.Success, run.Feedback.Detail);
        Assert.True(run.LastHit!.InPlay); Assert.False(run.LastHit.Foul);
    }

    [Theory]
    [InlineData("T-F01")]
    [InlineData("T-F05")]
    [InlineData("T-D02")]
    public void AuthoredOpportunityCanBeEarnedThroughOrdinaryHumanCommands(string id)
    {
        var run = Start(id);
        Drive(run);
        Assert.True(run.Feedback?.Success == true, $"{id}: {run.Feedback} at {run.Elapsed}; throws {string.Join(',',run.HumanThrows)}; play {run.LastPlay?.Kind}");
        if (id == "T-D02")
        {
            Assert.Equal(new[] { 2, 1 }, run.HumanThrows);
            Assert.Equal(2, run.LastPlay!.Outcome!.OutsMade.Count);
        }
        if (id == "T-F05") Assert.Equal(DefensiveFeat.Dive, run.LastPlay?.Outcome?.DefensiveFeat);
    }

    [Theory]
    [InlineData("T-F01", false)] [InlineData("T-F05", false)] [InlineData("T-D02", false)]
    [InlineData("T-F01", true)] [InlineData("T-F05", true)] [InlineData("T-D02", true)]
    public void DeadInputOrCpuCommandsNeverEarnHumanCompletion(string id, bool cpu)
    {
        var run = Start(id);
        Drive(run, cpu ? LivePlayCommandSource.Cpu : LivePlayCommandSource.Human, dead: !cpu);
        Assert.False(run.Feedback?.Success ?? false);
        Assert.Empty(run.Progress.Completed);
    }

    [Fact]
    public void WrongBagCannotPassDoublePlay()
    {
        var run = Start("T-D02");
        for (var i = 0; i < 1900 && run.Phase == TutorialPhase.Attempt; i++)
            run.Tick(Frame, run.Match.LivePlay.HoldsBall ? new(SouthDown: true, KeysBag: 3) : LivePadInput.Dead);
        Assert.False(run.Feedback!.Success);
    }

    [Fact]
    public void RetryAfterOneThrowRestoresRunnersTimersInputsAndSeed()
    {
        var run = Start("T-D02"); var first = run.Match.First!.Id;
        for (var i = 0; i < 600 && run.HumanThrows.Count == 0; i++)
            run.Tick(Frame, run.Match.LivePlay.HoldsBall ? new(SouthDown: true, KeysBag: 2) : LivePadInput.Dead);
        Assert.Single(run.HumanThrows);
        var old = run.Match;
        run.Retry();
        Assert.NotSame(old, run.Match);
        Assert.Equal(first, run.Match.First!.Id); Assert.Equal(0, run.Match.Outs);
        Assert.Empty(run.HumanThrows); Assert.Empty(run.Inputs);
        Assert.Equal(0, run.Match.LivePlay.ElapsedSeconds); Assert.Equal(0, run.Match.LivePlay.QueuedThrowBag);
        Drive(run); Assert.True(run.Feedback!.Success, run.Feedback.Detail);
    }

    [Fact]
    public void PauseFreezesTheOpportunityAndTimeoutNeverPasses()
    {
        var run = Start("T-P01"); run.Pause(true);
        for (var i = 0; i < 2000; i++) run.Tick(Frame);
        Assert.Equal(0, run.Elapsed); Assert.False(run.Pitch(new("fastball",0,false)));
        run.Pause(false);
        for (var i = 0; i < 2000; i++) run.Tick(Frame);
        Assert.Equal("timeout",run.Feedback!.Code); Assert.False(run.Feedback.Success);
        run.Exit(); Assert.Throws<InvalidOperationException>(() => run.Retry());
    }

    [Fact]
    public void DemoAndOldRevisionDoNotCountAsLearning()
    {
        var catalog = TutorialCatalog.Load(_content);
        var run = new TutorialSession(_content,catalog,"T-P01"); run.Begin(demonstration:true);
        run.Pitch(new("fastball",0,false),LivePlayCommandSource.Cpu);
        Assert.False(run.Feedback!.Success); Assert.Empty(run.Progress.Completed);
        var demoReplay=TutorialSession.Replay(_content,catalog,run.Recording());
        Assert.False(demoReplay.Feedback!.Success);Assert.Empty(demoReplay.Progress.Completed);
        run.Progress.Restore([new("T-P01",999,catalog.Profile)],catalog);
        Assert.Empty(run.Progress.Completed);
        run.Retry();run.Pitch(new("fastball",0,false));Assert.Equal(1, run.Successes);Assert.Empty(run.Progress.Completed);
    }

    [Theory]
    [InlineData("T-P01")][InlineData("T-P03")][InlineData("T-B01")]
    [InlineData("T-F01")][InlineData("T-F05")][InlineData("T-D02")]
    public void LiteralInputsReplayWithTheSameEvidence(string id)
    {
        var run=Start(id);
        if(id=="T-P01" || id=="T-P03")run.Pitch(new(id == "T-P03" ? PitchFamily.Changeup : PitchFamily.Fastball, 0, false));
        else if(id=="T-B01")run.Swing(new(true,0,0,false));
        else Drive(run);
        var recording=run.Recording();
        var replay=TutorialSession.Replay(_content,TutorialCatalog.Load(_content),recording);
        Assert.True(replay.Feedback!.Success);
        Assert.Equal(run.Feedback,replay.Feedback);
        Assert.Equal(run.HumanThrows,replay.HumanThrows);
        Assert.Equal(run.Elapsed,replay.Elapsed);
        Assert.Throws<InvalidDataException>(()=>TutorialSession.Replay(_content,TutorialCatalog.Load(_content),recording with { Revision=999 }));
    }

    internal static void Drive(TutorialSession run, LivePlayCommandSource source = LivePlayCommandSource.Human, bool dead = false)
    {
        var dove = false;
        for (var i = 0; i < 1900 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            var pad = LivePadInput.Dead;
            if (run.Lesson.Id == "T-F01" && live.ElapsedSeconds > .4)
            {
                var dx=live.BallX-live.GloveX; var dz=live.BallZ-live.GloveZ;
                var length=Math.Max(1e-6,Math.Sqrt(dx*dx+dz*dz));
                pad=new(StickX:dx/length,StickY:dz/length,SouthDown:true);
            }
            else if (run.Lesson.Id == "T-F05" && live.ElapsedSeconds >= live.Preview!.HangTimeSec-.6)
            {
                var d = Diamond.Dist(live.GloveX, live.GloveZ, live.BallX, live.BallZ);
                var dive = !dove && live.ElapsedSeconds >= live.Preview.HangTimeSec - .35 && d < 14;
                pad = new(EastDown: dive); if (dive) dove = true;
            }
            else if (run.Lesson.Id == "T-D02" && live.HoldsBall && !live.Throwing)
                pad=new(KeysBag:run.HumanThrows.Count==0 ? 2 : 1,SouthDown:true);
            run.Tick(Frame,dead ? LivePadInput.Dead : pad,source);
        }
    }
}
