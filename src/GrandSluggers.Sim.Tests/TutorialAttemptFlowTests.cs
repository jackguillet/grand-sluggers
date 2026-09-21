using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class TutorialAttemptFlowTests
{
    [Fact]
    public void SuccessAndFailureBothRepeatUntilThreeEarnedSuccesses()
    {
        var content = ContentCatalog.Load();
        var catalog = TutorialCatalog.Load(content);
        var progress = new TutorialProgress();
        var run = new TutorialSession(content, catalog, "T-P03", progress);
        run.Begin();
        var successes = 0;
        foreach (var changeup in new[] { false, true, false, true, false, true })
        {
            Assert.True(run.Pitch(new("fastball", 0, false, Changeup: changeup)));
            Assert.Equal(changeup, run.Feedback!.Success);
            if (changeup) successes++;
            Assert.Equal(successes, run.Successes);
            var repeat = HowToPlay.TutorialRepeatsImmediately(run.Phase, run.Successes);
            Assert.Equal(successes < 3, repeat);
            if (!repeat) continue;

            // The presentation replaces the session, keeping only saved learning progress.
            var prior = run;
            run = new TutorialSession(content, catalog, prior.Lesson.Id, progress);
            run.Begin();
            Assert.NotSame(prior.Match, run.Match);
            Assert.Equal(TutorialPhase.Attempt, run.Phase);
            Assert.Equal(successes, run.Successes);
            Assert.Equal(0, run.Match.Balls);
            Assert.Equal(0, run.Match.Strikes);
            Assert.Empty(run.Inputs);
            Assert.Null(run.Feedback);
            Assert.Null(run.LastPlay);
        }
        Assert.True(run.Passed);
        Assert.Equal(TutorialPhase.Feedback, run.Phase);
    }

    [Fact]
    public void GuidedAttemptsRepeatWithFreshActionEvidenceUntilThreeSuccesses()
    {
        var catalog = TutorialCatalog.Load(ContentCatalog.Load());
        var lesson = catalog.Lessons.Single(l => l.Id == "T-G06");
        var progress = new TutorialProgress();
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var run = new GuidedTutorialSession(lesson, catalog.Profile, progress);
            run.Begin();
            Assert.False(run.Observe(GuidedAction.MatchRestarted));
            Assert.True(run.Observe(GuidedAction.CallTimeOpened));
            Assert.True(run.Observe(GuidedAction.BookOpened));
            Assert.False(HowToPlay.TutorialRepeatsImmediately(run.Phase, run.Successes));
            Assert.True(run.Observe(GuidedAction.MatchRestarted));
            Assert.Equal(attempt, run.Successes);
            Assert.Equal(attempt < 3, HowToPlay.TutorialRepeatsImmediately(run.Phase, run.Successes));
            Assert.False(run.Observe(GuidedAction.MatchRestarted));
        }
    }

    [Theory]
    [InlineData(TutorialPhase.Brief)]
    [InlineData(TutorialPhase.Attempt)]
    [InlineData(TutorialPhase.Exited)]
    public void OnlyFinishedAttemptsCanRestart(TutorialPhase phase)
    {
        for (var successes = 0; successes <= 3; successes++)
            Assert.False(HowToPlay.TutorialRepeatsImmediately(phase, successes));
    }

    [Theory]
    [InlineData(InputScheme.Keys)]
    [InlineData(InputScheme.Pad)]
    public void CorrectionAndControlsRemainAvailableDuringTheNextAttempt(InputScheme scheme)
    {
        var failure = new TutorialFeedback(false, "use-changeup", "");
        var text = HowToPlay.TutorialAttemptHint("T-P03", scheme, "shipped", failure);
        Assert.Contains(HowToPlay.TutorialFeedbackText(failure.Code), text);
        Assert.Contains(HowToPlay.TutorialControls("T-P03", scheme), text);
        Assert.Equal(HowToPlay.TutorialControls("T-P03", scheme),
            HowToPlay.TutorialAttemptHint("T-P03", scheme, "shipped", failure with { Success = true }));
    }

    [Fact]
    public void HeldButtonAndReleaseFromPreviousAttemptCannotCommitANewPitch()
    {
        var held = ChargeButton.Advance(default, false, true, false, .1, .5);
        var released = ChargeButton.Advance(held.Next, false, false, true, .1, .5);
        Assert.False(held.Committed);
        Assert.False(released.Committed);
    }
}
