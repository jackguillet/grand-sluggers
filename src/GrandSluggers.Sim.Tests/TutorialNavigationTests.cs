using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class TutorialNavigationTests
{
    [Theory]
    [InlineData(1280, 800)]
    [InlineData(1920, 1080)]
    public void ClickingAnyLessonSelectsThatRowAndBlankSpaceDoesNothing(int w, int h)
    {
        for (var i = 0; i < 7; i++)
        {
            var r = HowToPlay.TutorialRow(w, h, i, 7);
            Assert.Equal(i, HowToPlay.TutorialHit(r.X + r.W / 2, r.Y + r.H / 2, w, h, true, 7, false));
        }
        Assert.Equal(-1, HowToPlay.TutorialHit(0, 0, w, h, true, 7, false));
    }

    [Theory]
    [InlineData(true, false, 0, -2)]  // selected lesson
    [InlineData(true, false, 1, -4)]  // title, never selected lesson
    [InlineData(false, false, 0, -2)] // begin
    [InlineData(false, false, 1, -3)] // lessons, never begin
    [InlineData(false, true, 0, -2)]  // retry
    [InlineData(false, true, 1, -5)]  // next, never retry
    [InlineData(false, true, 2, -3)]  // lessons, never retry
    public void FooterRoutesToItsNamedAction(bool menu, bool feedback, int button, int action)
    {
        var r = HowToPlay.TutorialAction(1280, 800, button, !menu && feedback ? 3 : 2);
        Assert.Equal(action, HowToPlay.TutorialHit(r.X + r.W / 2, r.Y + r.H / 2, 1280, 800, menu, 7, feedback));
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
    public void CategoryTabsPagesAndLessonRowsHaveSeparateHitTargets(int w, int h)
    {
        for (var i = 0; i < 8; i++)
        {
            var tab = HowToPlay.TutorialTab(w, h, i, 8);
            var x = tab.X + tab.W / 2; var y = tab.Y + tab.H / 2;
            Assert.Equal(i, HowToPlay.TutorialTabHit(x, y, w, h, 8));
            Assert.Equal(-1, HowToPlay.TutorialHit(x, y, w, h, true, 6, false));
        }
        foreach (var direction in new[] { -1, 1 })
        {
            var button = HowToPlay.TutorialPageButton(w, h, direction);
            Assert.Equal(direction, HowToPlay.TutorialPageHit(button.X + 2, button.Y + 2, w, h));
            Assert.Equal(-1, HowToPlay.TutorialTabHit(button.X + 2, button.Y + 2, w, h, 8));
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
    [InlineData("T-P04")]
    [InlineData("T-P05")]
    [InlineData("T-P06")]
    [InlineData("T-B02")]
    [InlineData("T-B04")]
    [InlineData("T-B07")]
    [InlineData("T-B08")]
    public void NewPlateLessonsExplainSetupGoalAndBothSchemes(string id)
    {
        Assert.NotEqual(id, HowToPlay.TutorialTitle(id));
        Assert.NotEmpty(HowToPlay.TutorialSetup(id));
        Assert.NotEmpty(HowToPlay.TutorialGoal(id));
        Assert.NotEmpty(HowToPlay.TutorialControls(id, InputScheme.Pad));
        Assert.NotEmpty(HowToPlay.TutorialControls(id, InputScheme.Keys));
    }

}
