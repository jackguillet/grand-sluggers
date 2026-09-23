using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class TutorialPlateTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    static readonly string[] Added = ["T-P04", "T-P05", "T-P06", "T-B02", "T-B04", "T-B07", "T-B08"];
    public static IEnumerable<object[]> Lessons => Added.Select(id => new object[] { id });
    TutorialSession Start(string id)
    {
        var run = new TutorialSession(_content, TutorialCatalog.Load(_content), id); run.Begin(); return run;
    }
    static bool Perform(TutorialSession run, LivePlayCommandSource source = LivePlayCommandSource.Human) => run.Lesson.Id switch
    {
        "T-P04" => run.Pitch(new("fastball", 1, false), source),
        "T-P05" => run.Pitch(new("fastball", 0, false, BreakX: .8), source),
        "T-P06" => run.Pitch(new("fastball", 0, false, RubberX: .2), source),
        "T-B02" => run.Swing(new(true, 0, 0, false), source),
        "T-B04" => run.Swing(new(true, 1, 0, false), source),
        "T-B07" => run.Swing(new(true, 0, 0, false, Bunt: true, SquareSec: 1), source),
        "T-B08" => run.Swing(new(false, 0, 0, false), source),
        _ => throw new InvalidOperationException()
    };

    [Theory, MemberData(nameof(Lessons))]
    public void EachPlateLessonRequiresThreeRealResultsAndReplaysItsInputs(string id)
    {
        var run = Start(id);
        for (var i = 1; i <= 3; i++)
        {
            if (id == "T-B07") Assert.Equal(2, run.Match.Strikes);
            Assert.True(Perform(run));
            Assert.True(run.Feedback!.Success, id + ": " + run.Feedback.Detail);
            Assert.Equal(i, run.Successes); Assert.Equal(i == 3, run.Passed);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            Assert.Equal(run.LastHit, replay.LastHit);
            Assert.False(Perform(run)); Assert.Equal(i, run.Successes);
            run.Retry();
            Assert.Null(run.Feedback); Assert.Equal(i, run.Successes);
        }
    }

    [Theory, MemberData(nameof(Lessons))]
    public void CpuDemoAndClockAloneNeverCompleteTheLesson(string id)
    {
        var run = Start(id);
        Assert.False(Perform(run, LivePlayCommandSource.Cpu)); Assert.Equal(0, run.Successes);
        for (var i = 0; i < 601 && run.Phase == TutorialPhase.Attempt; i++) run.Tick(.05);
        Assert.Equal("timeout", run.Feedback!.Code); Assert.Equal(0, run.Successes);
        var demo = new TutorialSession(_content, TutorialCatalog.Load(_content), id); demo.Begin(demonstration: true);
        Perform(demo, LivePlayCommandSource.Cpu);
        Assert.Equal("demonstration", demo.Feedback!.Code); Assert.Equal(0, demo.Successes);
    }

    [Theory]
    [InlineData("T-P04", 0.75, 0, 0, "use-max-pitch")]
    [InlineData("T-P05", 0, 0, 0, "use-break")]
    [InlineData("T-P06", 0, 0, 0, "move-rubber")]
    public void AnOrdinaryStrikeWithoutTheRequiredActionFails(string id, double charge, double bend, double rubber, string code)
    {
        var run = Start(id); run.Pitch(new("fastball", charge, false, BreakX: bend, RubberX: rubber));
        Assert.Equal(PlayKind.TakeStrike, run.LastPlay!.Kind);
        Assert.Equal(code, run.Feedback!.Code); Assert.Equal(0, run.Successes);
    }

    [Theory]
    [InlineData("T-P04")][InlineData("T-P05")][InlineData("T-P06")]
    public void DoingTheActionOutsideTheZoneFails(string id)
    {
        var run = Start(id);
        run.Pitch(new("fastball", id == "T-P04" ? 1 : 0, false, AimY: 3, BreakX: .8, RubberX: .2));
        Assert.Equal("outside-zone", run.Feedback!.Code); Assert.Equal(0, run.Successes);
    }

    [Fact]
    public void ChargedBuntAndPoorContactCannotStandInForTheRequestedSwing()
    {
        var run = Start("T-B04"); run.Swing(new(true, .75, 0, false)); Assert.Equal("use-max-swing", run.Feedback!.Code);
        run.Retry(); run.Swing(new(true, 1, 50, false)); Assert.Equal("miss", run.Feedback!.Code);
        run = Start("T-B07"); run.Swing(new(true, 0, 0, false)); Assert.Equal("use-bunt", run.Feedback!.Code);
        // A held bunt has no press to time (§5.8): the poor bunt is the bat held off the ball, a strike.
        run.Retry(); run.Swing(SwingCommand.HeldBunt(BuntSide.First, boxOffsetX: 1));
        Assert.False(run.Feedback!.Success); Assert.Equal(PlayKind.Strikeout, run.LastPlay!.Kind);
        run.Retry(); Assert.Equal(2, run.Match.Strikes); Assert.Equal(0, run.Successes);
        run = Start("T-B02"); run.Swing(new(true, 0, 0, false, BoxOffsetX: .4));
        Assert.NotEqual(ContactQuality.Perfect, run.LastHit!.Quality); Assert.False(run.Feedback!.Success);
    }

    [Fact]
    public void TakingTheBallIsARealCalledBallAndChasingOrSquaringDoesNotEarnIt()
    {
        var run = Start("T-B08"); Assert.False(AtBatResolver.PitchInZone(run.CpuPitch, run.Match.Pitcher.Stats.Pitch));
        run.Swing(new(true, 0, 0, false)); Assert.Equal("chased-ball", run.Feedback!.Code);
        run.Retry(); run.Swing(new(false, 0, 0, false, Bunt: true)); Assert.False(run.Feedback!.Success);
        run.Retry(); Perform(run);
        Assert.Equal(PlayKind.TakeBall, run.LastPlay!.Kind); Assert.Equal(1, run.Match.Balls); Assert.Equal(1, run.Successes);
    }

    [Fact]
    public void AuthoringInvalidCpuPitchesCountsOrMovementTargetsFailsValidation()
    {
        var c = TutorialCatalog.Load(_content); var i = Array.FindIndex(c.Setups, s => s.Id == "T-B08");
        c.Setups[i] = c.Setups[i] with { Pitch = new("fastball", 0, false) };
        Assert.Contains(c.Validate(_content), e => e.Contains("strike/ball policy"));
        c.Setups[i] = c.Setups[i] with { Pitch = new("fastball", 0, false, AimY: double.NaN) };
        Assert.Contains(c.Validate(_content), e => e.Contains("invalid CPU pitch"));
        c = TutorialCatalog.Load(_content); i = Array.FindIndex(c.Setups, s => s.Id == "T-B07");
        c.Setups[i] = c.Setups[i] with { Strikes = 0 };
        Assert.Contains(c.Validate(_content), e => e.Contains("two-strike bunt risk"));
        c = TutorialCatalog.Load(_content); i = Array.FindIndex(c.Setups, s => s.Id == "T-P05");
        c.Setups[i] = c.Setups[i] with { MinMovement01 = 0 };
        Assert.Contains(c.Validate(_content), e => e.Contains("meaningful movement threshold"));
    }
}
