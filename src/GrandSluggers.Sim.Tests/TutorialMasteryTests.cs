using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class TutorialMasteryTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Theory]
    [InlineData("T-P01")][InlineData("T-P03")][InlineData("T-B01")]
    [InlineData("T-F01")][InlineData("T-F05")][InlineData("T-D02")]
    public void EveryLessonNeedsThreeDistinctSuccessfulAttempts(string id)
    {
        var run = new TutorialSession(_content, TutorialCatalog.Load(_content), id);
        run.Begin();
        for (var repetition = 1; repetition <= 4; repetition++)
        {
            if (id is "T-P01" or "T-P03") run.Pitch(new(id == "T-P03" ? PitchFamily.Changeup : PitchFamily.Fastball, 0, false));
            else if (id == "T-B01") run.Swing(new(true, 0, 0, false));
            else TutorialSessionTests.Drive(run);
            Assert.True(run.Feedback!.Success, run.Feedback.Detail);
            Assert.Equal(Math.Min(3, repetition), run.Successes);
            Assert.Equal(repetition >= 3, run.Passed);
            Assert.Equal(repetition >= 3 ? 1 : 0, run.Progress.Completed.Count);
            // A held action, repeated result read, or late tick cannot be another successful attempt.
            Assert.False(run.Pitch(new("fastball", 0, false)));
            Assert.False(run.Swing(new(true, 0, 0, false)));
            run.Tick(.05);
            Assert.Equal(Math.Min(3, repetition), run.Successes);
            var match = run.Match;
            run.Retry();
            Assert.NotSame(match, run.Match);
            Assert.Equal(Math.Min(3, repetition), run.Successes);
        }
    }

    [Fact]
    public void FailurePauseCpuAndDemonstrationPreserveButNeverIncreaseEarnedSuccesses()
    {
        var c = TutorialCatalog.Load(_content); var progress = new TutorialProgress();
        var run = new TutorialSession(_content, c, "T-P03", progress); run.Begin();
        run.Pitch(new(PitchFamily.Changeup, 0, false));
        run.Retry(); run.Pitch(new("fastball", 0, false));
        Assert.False(run.Feedback!.Success); Assert.Equal(1, run.Successes);
        run.Retry(); run.Pause(true);
        Assert.False(run.Pitch(new(PitchFamily.Changeup, 0, false)));
        run.Tick(.1); Assert.Equal(0, run.Elapsed); Assert.Equal(1, run.Successes);
        run.Pause(false);
        Assert.False(run.Pitch(new(PitchFamily.Changeup, 0, false), LivePlayCommandSource.Cpu));
        Assert.Equal(1, run.Successes);
        run.Exit();
        var demo = new TutorialSession(_content, c, "T-P03", progress); demo.Begin(demonstration: true);
        demo.Pitch(new(PitchFamily.Changeup, 0, false), LivePlayCommandSource.Cpu);
        Assert.Equal(1, demo.Successes); Assert.False(demo.Passed);
        var resume = new TutorialSession(_content, c, "T-P03", progress); resume.Begin();
        resume.Pitch(new(PitchFamily.Changeup, 0, false)); Assert.Equal(2, resume.Successes);
        resume.Retry(); resume.Pitch(new(PitchFamily.Changeup, 0, false)); Assert.True(resume.Passed);
    }

    [Fact]
    public void SavedPartialProgressResumesWithoutConfusingLessonsProfilesOrOldSingleSuccessRevisions()
    {
        var c = TutorialCatalog.Load(_content); var run = new TutorialSession(_content, c, "T-P01"); run.Begin();
        run.Pitch(new("fastball", 0, false));
        var saved = System.Text.Json.JsonSerializer.Serialize(run.Progress.Saved);
        var progress = new TutorialProgress();
        progress.RestorePractice(System.Text.Json.JsonSerializer.Deserialize<TutorialPracticeProgress[]>(saved)!, c);
        progress.RestorePractice([
            new("T-P01", 1, c.Profile, 3), new("T-P01", run.Lesson.Revision, "invalid", 3),
            new("T-P01", run.Lesson.Revision, c.Profile, 99), new("T-P01", run.Lesson.Revision, c.Profile, -1),
            new(c.Lessons.First(l => l.Status == "planned").Id, 1, c.Profile, 3), new("missing", 2, c.Profile, 3)
        ], c);
        Assert.Equal(1, progress.Count(run.Lesson, c.Profile)); Assert.Empty(progress.Completed);
        Assert.Equal(0, progress.Count(c.Lesson("T-P03"), c.Profile));
        Assert.Equal(0, progress.Count(run.Lesson, c.Profile == "shipped" ? "some-trial" : "shipped"));
        var resume = new TutorialSession(_content, c, "T-P01", progress); resume.Begin();
        resume.Pitch(new("fastball", 0, false)); Assert.Equal(2, resume.Successes); Assert.False(resume.Passed);
        resume.Retry(); resume.Pitch(new("fastball", 0, false)); Assert.True(resume.Passed);
        progress.RestorePractice(System.Text.Json.JsonSerializer.Deserialize<TutorialPracticeProgress[]>(saved)!, c);
        Assert.True(resume.Passed); // a stale save cannot erase newer progress
    }
}
