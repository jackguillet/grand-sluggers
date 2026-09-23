using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class TutorialPlateTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    static readonly string[] Added = ["T-P04", "T-P05", "T-P06", "T-B02", "T-B04", "T-B07", "T-B08", "T-B10", "T-B11"];
    const double Tick = 1 / 60.0;
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
        // The held bunt (§5.8, PH-14-R5): the client sends the held side at the plate, no timed press.
        "T-B07" => run.Swing(SwingCommand.HeldBunt(BuntSide.Third, 0, squareSec: 1), source),
        "T-B08" => run.Swing(new(false, 0, 0, false), source),
        "T-B10" => CancelThenTake(run, source),
        "T-B11" => run.Swing(SwingCommand.HeldBunt(BuntSide.First, 0, squareSec: 1), source),
        _ => throw new InvalidOperationException()
    };

    /// <summary>
    /// T-B10's player input (PH-13-R1): South loads in SET and holds into the flight, East / G cancels the armed load,
    /// the cancelled hold comes up (which commits nothing), and the pitch reaches the plate with no swing.
    /// </summary>
    static bool CancelThenTake(TutorialSession run, LivePlayCommandSource source)
    {
        var plate = run.Plate(new(new(true, true, false), Tick, Commits: false), source)
            && run.Plate(new(new(false, true, false), Tick, Commits: true), source)
            && run.Plate(new(new(false, true, false, Cancel: true, CancelHeld: true), Tick, Commits: true), source)
            && run.Plate(new(new(false, false, true), Tick, Commits: true), source);
        _ = plate;
        return run.Swing(new(false, 0, 0, false), source);
    }

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
    public void CancelASwingNeedsTheExplicitCancelOfARealLoadAndATakenBall()
    {
        // T-B10 (PH-13-R1). The session steps the player's own plate buttons: the pass rests on East / G discarding
        // an armed load, never on a flag the client sets. The released hold after the cancel commits nothing.
        var run = Start("T-B10"); Assert.False(AtBatResolver.PitchInZone(run.CpuPitch, run.Match.Pitcher.Stats.Pitch));
        Assert.True(CancelThenTake(run, LivePlayCommandSource.Human));
        Assert.True(run.CancelledLoad); Assert.False(run.PlateState.SwingCommitted);
        Assert.Equal("cancelled-take", run.Feedback!.Code); Assert.Equal(PlayKind.TakeBall, run.LastPlay!.Kind);

        // A take with nothing loaded is T-B08, not this lesson.
        run.Retry(); Assert.False(run.CancelledLoad);
        run.Swing(new(false, 0, 0, false)); Assert.Equal("load-then-cancel", run.Feedback!.Code);

        // East with nothing armed discards nothing: still no cancelled load.
        run.Retry();
        run.Plate(new(new(false, false, false, Cancel: true, CancelHeld: true), Tick, Commits: true));
        Assert.True(run.PlateState.CancelSpent); Assert.False(run.CancelledLoad);
        run.Swing(new(false, 0, 0, false)); Assert.Equal("load-then-cancel", run.Feedback!.Code);

        // A swing that went is a swing, whatever came before.
        run.Retry(); run.Swing(new(true, 0, 0, false)); Assert.Equal("cancel-swung", run.Feedback!.Code);

        // A bunt press converts the load (PH-13-R1) but it is not the explicit cancel, and the bunt is not a take.
        run.Retry();
        run.Plate(new(new(true, true, false), Tick, Commits: false));
        run.Plate(new(new(false, true, false, ThirdPressed: true, ThirdHeld: true), Tick, Commits: true));
        Assert.False(run.CancelledLoad);
        run.Swing(SwingCommand.HeldBunt(BuntSide.Third, 0)); Assert.Equal("cancel-swung", run.Feedback!.Code);
        Assert.Equal(1, run.Successes); // only the first attempt earned one
    }

    [Fact]
    public void BuntTowardFirstNeedsTheFirstBaseSideAndABallThatWentThere()
    {
        // T-B11 (§5.8, PH-14-R2 … R5): the named side is the first-base trigger (RT / L), and the fair bunt must
        // have gone toward first. The runner on first is real.
        var run = Start("T-B11"); Assert.NotNull(run.Match.First);
        Perform(run);
        Assert.True(run.Feedback!.Success, run.Feedback.Detail);
        Assert.Equal("fair-bunt-first", run.Feedback.Code); Assert.True(run.LastHit!.SprayDeg > 0);
        run.Retry(); run.Swing(SwingCommand.HeldBunt(BuntSide.Third, 0, squareSec: 1));
        Assert.Equal("use-first-side", run.Feedback!.Code);
        run.Retry(); run.Swing(new(true, 0, 0, false)); Assert.Equal("use-bunt", run.Feedback!.Code);
        // Released every trigger before the plate: the bat came back and the pitch was taken.
        run.Retry(); run.Swing(new(false, 0, 0, false)); Assert.Equal("use-bunt", run.Feedback!.Code);
        run.Retry(); run.Swing(SwingCommand.HeldBunt(BuntSide.First, boxOffsetX: 1));
        Assert.Equal("bunt-miss", run.Feedback!.Code);
        Assert.Equal(1, run.Successes);
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
