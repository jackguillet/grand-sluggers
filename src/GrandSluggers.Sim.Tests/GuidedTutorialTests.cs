using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class GuidedTutorialTests
{
    static TutorialLesson Lesson(string id) => new(id, 2, ["game.01"], id, "game", "implemented", 796,
        "Guided", ["shipped"], [], [], "", id switch
        {
            "T-G01" => "guided-lineup", "T-G05" => "guided-seats", "T-G06" => "guided-pause",
            "T-G06-R" => "guided-recovery", "T-G06-C" => "guided-calibration", "T-G07" => "guided-settings", _ => ""
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

    /// <summary>Drives the real settings model and reports each accepted or refused edit as the director does.</summary>
    static void Edit(GuidedTutorialSession run, ExhibitionSettings settings, LineupScreens lineup, int row, LineupSeat seat = LineupSeat.Pad1)
    {
        settings.Select(row);
        var refusal = settings.Refusal(seat);
        var cleared = lineup.IsReady(LineupSeat.Pad1) && lineup.HomeSeat != LineupSeat.Cpu
            || lineup.IsReady(LineupSeat.Pad2) && (lineup.HomeSeat == LineupSeat.Pad2 || lineup.AwaySeat == LineupSeat.Pad2);
        if (settings.Change(seat)) lineup.ResetReady(); else cleared = false;
        run.ObserveRuleEdit(row, seat, refusal, refusal == null && cleared);
    }

    static LineupScreens AtSettings(bool versus)
    {
        var lineup = LineupScreens.Open(Shipped.Content, "vale", "brondo", LineupSeat.Pad1, versus ? LineupSeat.Pad2 : LineupSeat.Cpu);
        lineup.RandomFill(LineupSeat.Pad1);
        if (versus) lineup.RandomFill(LineupSeat.Pad2);
        Assert.True(lineup.ConfirmTeam());
        lineup.ToggleReady(LineupSeat.Pad1);
        if (versus) lineup.ToggleReady(LineupSeat.Pad2);
        Assert.True(lineup.OpenSettings());
        return lineup;
    }

    static void Ready(GuidedTutorialSession run, LineupScreens lineup, LineupSeat seat)
    {
        Assert.True(lineup.ToggleReady(seat));
        run.ObserveReady(seat, lineup.IsReady(seat));
    }

    static LineupSeat[] Humans(LineupScreens lineup) =>
        new[] { lineup.HomeSeat, lineup.AwaySeat }.Where(s => s != LineupSeat.Cpu).ToArray();

    [Fact]
    public void SettingsNeedStadiumThenEveryRuleEditThenAnOwnReadyAndThreeAttempts()
    {
        var progress = new TutorialProgress();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var run = new GuidedTutorialSession(Lesson("T-G07"), "shipped", progress);
            var settings = new ExhibitionSettings();
            var lineup = AtSettings(versus: false);
            Assert.False(run.Observe(GuidedAction.StadiumChosen)); // Brief earns nothing.
            run.Begin();
            Edit(run, settings, lineup, 0);
            Assert.Contains(GuidedAction.StarsChanged, run.Missing); // A rule before the stadium does not count.
            Assert.True(run.Observe(GuidedAction.StadiumChosen));
            Assert.False(run.Observe(GuidedAction.StadiumChosen));
            Assert.False(run.Observe(GuidedAction.SettingsStarted)); // Only the typed start may complete it.
            // Any order: mercy, innings, then Stars.
            Edit(run, settings, lineup, 3); Edit(run, settings, lineup, 2); Edit(run, settings, lineup, 4); Edit(run, settings, lineup, 0);
            Assert.Equal(new[] { GuidedAction.SettingsStarted }, run.Missing);
            Assert.Equal((true, 6, false), (settings.Stars, settings.Innings, settings.Mercy)); // Stars went off, then on.
            Ready(run, lineup, LineupSeat.Pad1);
            Assert.True(lineup.BothReady); // The CPU seat is always ready; it is not evidence.
            Assert.Equal(attempt, run.Successes);
            Assert.True(run.ObserveSettingsStart(Humans(lineup)));
            Assert.Equal(TutorialPhase.Feedback, run.Phase);
            Assert.True(run.Feedback!.Success);
            Assert.Equal(attempt + 1, run.Successes);
        }
        Assert.True(new GuidedTutorialSession(Lesson("T-G07"), "shipped", progress).Passed);
    }

    [Fact]
    public void ItemsAndPlayerTwoEditsAreTypedRefusalsThatEarnNothing()
    {
        var run = new GuidedTutorialSession(Lesson("T-G07"), "shipped", new TutorialProgress());
        var settings = new ExhibitionSettings();
        var lineup = AtSettings(versus: true);
        run.Begin();
        run.Observe(GuidedAction.StadiumChosen);
        Edit(run, settings, lineup, 1);
        Assert.Equal("items-unavailable", run.Notice!.Code);
        Assert.False(run.Notice.Success);
        Assert.False(settings.ItemsAvailable);
        Edit(run, settings, lineup, 0, LineupSeat.Pad2);
        Assert.Equal("rules-player-one", run.Notice!.Code);
        Assert.True(settings.Stars);
        Assert.Equal(TutorialPhase.Attempt, run.Phase);
        Assert.Contains(GuidedAction.StarsChanged, run.Missing);
        Assert.False(string.IsNullOrWhiteSpace(HowToPlay.TutorialFeedbackText("items-unavailable")));
        Assert.False(string.IsNullOrWhiteSpace(HowToPlay.TutorialFeedbackText("rules-player-one")));
    }

    [Fact]
    public void RuleChangeAfterAReadyClearsItAndBothSeatsMustReadyAgainOnTheirOwn()
    {
        var progress = new TutorialProgress();
        var run = new GuidedTutorialSession(Lesson("T-G07"), "shipped", progress);
        var settings = new ExhibitionSettings();
        var lineup = AtSettings(versus: true);
        run.Begin();
        run.Observe(GuidedAction.StadiumChosen);
        Edit(run, settings, lineup, 0); Edit(run, settings, lineup, 2);
        Ready(run, lineup, LineupSeat.Pad2);
        Edit(run, settings, lineup, 3); // P2 was ready: the edit clears it.
        Assert.Equal("ready-reset", run.Notice!.Code);
        Assert.False(lineup.IsReady(LineupSeat.Pad2));
        Ready(run, lineup, LineupSeat.Pad1);
        Assert.False(lineup.BothReady);
        Ready(run, lineup, LineupSeat.Pad2);
        Assert.True(lineup.BothReady);
        Assert.True(run.ObserveSettingsStart(Humans(lineup)));
        Assert.Equal(1, run.Successes);

        // A seat readied before the last change and forced through still fails with its reason.
        run.Retry(); run.Begin();
        run.Observe(GuidedAction.StadiumChosen);
        Edit(run, settings, lineup, 0); Edit(run, settings, lineup, 2);
        run.ObserveReady(LineupSeat.Pad2, true);
        Edit(run, settings, lineup, 3);
        run.ObserveReady(LineupSeat.Pad1, true);
        Assert.True(run.ObserveSettingsStart(Humans(lineup)));
        Assert.Equal("ready-missing", run.Feedback!.Code);
        Assert.Equal(1, run.Successes);
    }

    [Fact]
    public void StartingBeforeTheRuleEditsFailsTypedAndRetryStartsFresh()
    {
        var run = new GuidedTutorialSession(Lesson("T-G07"), "shipped", new TutorialProgress());
        var settings = new ExhibitionSettings();
        var lineup = AtSettings(versus: false);
        run.Begin();
        run.Observe(GuidedAction.StadiumChosen);
        Edit(run, settings, lineup, 0);
        Ready(run, lineup, LineupSeat.Pad1);
        Assert.True(run.ObserveSettingsStart(Humans(lineup)));
        Assert.Equal(TutorialPhase.Feedback, run.Phase);
        Assert.Equal("settings-incomplete", run.Feedback!.Code);
        Assert.False(run.Feedback.Success);
        Assert.Equal(0, run.Successes);
        Assert.False(run.ObserveSettingsStart([])); // Not an attempt any more.
        run.Retry(); run.Begin();
        Assert.Equal(5, run.Missing.Count);
        Assert.Null(run.Notice);
        // No human seat is never a success, even with every rule edited.
        run.Observe(GuidedAction.StadiumChosen);
        foreach (var row in new[] { 0, 2, 3 }) Edit(run, settings, lineup, row);
        Assert.True(run.ObserveSettingsStart([LineupSeat.Cpu]));
        Assert.Equal("ready-missing", run.Feedback!.Code);
    }

    [Fact]
    public void RejectsWrongProfileAndObjective()
    {
        Assert.Throws<ArgumentException>(() => new GuidedTutorialSession(Lesson("T-G05"), "some-trial", new TutorialProgress()));
        Assert.Throws<ArgumentException>(() => new GuidedTutorialSession(Lesson("T-G05") with { Objective = "guided-pause" },
            "shipped", new TutorialProgress()));
    }
}
