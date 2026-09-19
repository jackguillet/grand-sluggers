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

}
