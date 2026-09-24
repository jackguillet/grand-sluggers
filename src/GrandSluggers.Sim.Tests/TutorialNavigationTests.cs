using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class TutorialNavigationTests
{
    [Fact]
    public void CoachingDoesNotHideTheOrdinaryGameplayReadouts()
    {
        var coach = BroadcastHud.TutorialCoach;
        foreach (var seats in new[] { 1, 2 })
        {
            var layout = BroadcastHud.Layout(seats);
            foreach (var readout in new[] { layout.Score, layout.BatterCard, layout.PitcherCard })
                Assert.True(coach.Right <= readout.X || readout.Right <= coach.X
                    || coach.Bottom <= readout.Y || readout.Bottom <= coach.Y);
        }
    }

    [Theory]
    [InlineData("T-F07")]
    [InlineData("T-F14")]
    public void RelayInstructionsNameTheSecondThrowCommand(string id)
    {
        // Each human relay leg needs its own command (fielding.throw.relayAutoContinue 0): the book never says the receiver
        // sends it on its own.
        Assert.Contains("catch", HowToPlay.TutorialControls(id, "shipped"));
        Assert.Contains("receiver", HowToPlay.TutorialSetup(id, "shipped"));
        Assert.DoesNotContain("automatically", HowToPlay.TutorialSetup(id, "shipped"));
        Assert.Contains("second throw", HowToPlay.TutorialSetup("T-F07", "shipped"));
    }

    [Fact]
    public void EveryRunnableLessonHasPadTeachingCopy()
    {
        var catalog = TutorialCatalog.Load(ContentCatalog.Load());
        foreach (var lesson in catalog.Lessons.Where(l => l.Status == "implemented" && l.Profiles.Contains(catalog.Profile)))
        {
            Assert.NotEqual(lesson.Id, HowToPlay.TutorialTitle(lesson.Id));
            Assert.False(string.IsNullOrWhiteSpace(HowToPlay.TutorialGoal(lesson.Id)), lesson.Id + " needs a goal");
            Assert.False(string.IsNullOrWhiteSpace(HowToPlay.TutorialSetup(lesson.Id, catalog.Profile)), lesson.Id + " needs a setup");
            var controls = HowToPlay.TutorialControls(lesson.Id, catalog.Profile);
            Assert.False(string.IsNullOrWhiteSpace(controls), lesson.Id + " needs pad controls");
            // The game is gamepad only: no lesson copy names a keyboard or mouse.
            foreach (var copy in new[] { controls, HowToPlay.TutorialGoal(lesson.Id), HowToPlay.TutorialSetup(lesson.Id, catalog.Profile) })
                Assert.False(HowToPlay.NamesKeyboard(copy), lesson.Id + ": " + copy);
        }
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void FeedbackCallsTheLessonCompleteOnlyAfterThreeSuccesses(int successes, bool passed)
    {
        var title = HowToPlay.TutorialResultTitle(true, successes);
        Assert.Equal(passed, title.Contains(HowToPlay.TutorialComplete));
        Assert.Contains($"{successes}/3", title);
        Assert.Contains($"{successes}/3", HowToPlay.TutorialAttemptTitle("T-P01", successes));
        Assert.DoesNotContain(HowToPlay.TutorialComplete, HowToPlay.TutorialResultTitle(false, successes));
    }

    [Theory]
    [InlineData(1280, 800)]
    [InlineData(1920, 1080)]
    public void CategoryTabsPagesAndLessonRowsDoNotOverlap(int w, int h)
    {
        static bool Overlap((float X, float Y, float W, float H) a, (float X, float Y, float W, float H) b) =>
            a.X < b.X + b.W && b.X < a.X + a.W && a.Y < b.Y + b.H && b.Y < a.Y + a.H;
        for (var i = 0; i < 8; i++)
        {
            var tab = HowToPlay.TutorialTab(w, h, i, 8);
            for (var r = 0; r < 6; r++)
                Assert.False(Overlap(tab, HowToPlay.TutorialRow(w, h, r, 6)), $"tab {i} overlaps row {r}");
            foreach (var direction in new[] { -1, 1 })
                Assert.False(Overlap(tab, HowToPlay.TutorialPageButton(w, h, direction)), $"tab {i} overlaps page {direction}");
        }
        var last = HowToPlay.TutorialRow(w, h, 5, 6);
        Assert.True(last.Y + last.H < HowToPlay.TutorialAction(w, h, 0, 2).Y);
        Assert.True(last.H >= 72);
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(6, 1, 0)]
    [InlineData(7, 2, 6)]
    [InlineData(21, 4, 18)]
    [InlineData(26, 5, 24)]
    public void PagesKeepFutureCategoryGrowthWithinSixRows(int count, int pages, int lastStart)
    {
        Assert.Equal(pages, HowToPlay.TutorialPages(count));
        Assert.Equal(lastStart, HowToPlay.TutorialPageStart(Math.Max(0, count - 1)));
    }

    [Theory]
    [InlineData("T-P02")]
    [InlineData("T-B03")]
    [InlineData("T-B03-L")]
    [InlineData("T-B05")]
    [InlineData("T-P04")]
    [InlineData("T-P05")]
    [InlineData("T-P06")]
    [InlineData("T-B02")]
    [InlineData("T-B04")]
    [InlineData("T-B07")]
    [InlineData("T-B08")]
    [InlineData("T-B10")]
    [InlineData("T-B11")]
    public void NewPlateLessonsExplainSetupGoalAndPadControls(string id)
    {
        Assert.NotEqual(id, HowToPlay.TutorialTitle(id));
        Assert.NotEmpty(HowToPlay.TutorialSetup(id));
        Assert.NotEmpty(HowToPlay.TutorialGoal(id));
        Assert.NotEmpty(HowToPlay.TutorialControls(id));
    }

    [Theory]
    [InlineData("T-F02")][InlineData("T-F03")][InlineData("T-F03-2")][InlineData("T-F03-3")]
    [InlineData("T-F03-H")][InlineData("T-F04")][InlineData("T-F06")]
    public void FieldLessonsExplainTheGoalSetupAndPadControls(string id)
    {
        Assert.NotEqual(id, HowToPlay.TutorialTitle(id));
        Assert.NotEmpty(HowToPlay.TutorialGoal(id));
        Assert.NotEmpty(HowToPlay.TutorialSetup(id));
        Assert.NotEmpty(HowToPlay.TutorialControls(id));
    }

    [Theory]
    [InlineData("T-F03", "first", "Right")]
    [InlineData("T-F03-2", "second", "Up")]
    [InlineData("T-F03-3", "third", "Left")]
    [InlineData("T-F03-H", "home", "Down")]
    public void NamedThrowLessonsTeachTheCorrespondingBagInput(string id, string bag, string direction)
    {
        Assert.Contains(bag, HowToPlay.TutorialGoal(id).ToLowerInvariant());
        Assert.Contains("Right stick " + direction, HowToPlay.TutorialControls(id));
    }
}
