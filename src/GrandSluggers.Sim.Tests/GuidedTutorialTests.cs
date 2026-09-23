using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class GuidedTutorialTests
{
    static TutorialLesson Lesson(string id) => new(id, 2, ["game.01"], id, "game", "implemented", 796,
        "Guided", ["shipped"], [], [], "", id switch
        {
            "T-G01" => "guided-lineup", "T-G05" => "guided-seats", "T-G06" => "guided-pause",
            "T-G06-R" => "guided-recovery", "T-G06-C" => "guided-calibration", _ => ""
        }, [], ["GuidedTutorialTests"]);

    [Fact]
    public void LineupNeedsEveryAcceptedActionAndThreeSeparateAttempts()
    {
        var progress = new TutorialProgress();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var run = new GuidedTutorialSession(Lesson("T-G01"), "shipped", progress);
            Assert.False(run.Observe(GuidedAction.LeftHandedRosterDrop)); // Brief cannot earn credit.
            run.Begin();
            Assert.False(run.Observe(GuidedAction.BattingOrderChanged)); // Order matters.
            Assert.True(run.Observe(GuidedAction.LeftHandedRosterDrop));
            Assert.False(run.Observe(GuidedAction.LeftHandedRosterDrop));
            Assert.True(run.Observe(GuidedAction.BattingOrderChanged));
            Assert.Equal(TutorialPhase.Attempt, run.Phase);
            Assert.Equal(attempt, run.Successes);
            Assert.True(run.Observe(GuidedAction.GlovePositionChanged));
            Assert.Equal(TutorialPhase.Feedback, run.Phase);
            Assert.False(run.Observe(GuidedAction.GlovePositionChanged));
            Assert.Equal(attempt + 1, run.Successes);
        }
        Assert.True(new GuidedTutorialSession(Lesson("T-G01"), "shipped", progress).Passed);
    }

    [Fact]
    public void RecoveryCannotBeCreditedByAControlsPageVisit()
    {
        var lesson = Lesson("T-G06-R");
        var run = new GuidedTutorialSession(lesson, "shipped", new TutorialProgress());
        run.Begin();
        Assert.False(run.Observe(GuidedAction.TwoPhysicalSeatsBound));
        Assert.False(run.Observe(GuidedAction.SeatRecovered));
        Assert.True(run.ObserveSeatLost(LineupSeat.Pad2));
        Assert.False(run.ObserveSeatRecovered(LineupSeat.Pad1));
        Assert.Equal(TutorialPhase.Attempt, run.Phase);
        Assert.Equal(0, run.Successes);
        Assert.Contains(GuidedAction.SeatRecovered, run.Missing);
        Assert.True(run.ObserveSeatRecovered(LineupSeat.Pad2));
        Assert.Equal(1, run.Successes);
    }

    [Fact]
    public void PauseLessonRequiresBookAfterCallTimeAndThenRestart()
    {
        var run = new GuidedTutorialSession(Lesson("T-G06"), "shipped", new TutorialProgress());
        run.Begin();
        Assert.False(run.Observe(GuidedAction.BookOpened));
        Assert.False(run.Observe(GuidedAction.MatchRestarted));
        Assert.True(run.Observe(GuidedAction.CallTimeOpened));
        Assert.True(run.Observe(GuidedAction.BookOpened));
        Assert.True(run.Observe(GuidedAction.MatchRestarted));
        Assert.Equal(1, run.Successes);
    }

    [Fact]
    public void RejectsWrongProfileAndObjective()
    {
        Assert.Throws<ArgumentException>(() => new GuidedTutorialSession(Lesson("T-G05"), "some-trial", new TutorialProgress()));
        Assert.Throws<ArgumentException>(() => new GuidedTutorialSession(Lesson("T-G05") with { Objective = "guided-pause" },
            "shipped", new TutorialProgress()));
    }
}
