using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public static partial class HudView
    {
        public static void GuidedTutorial(GuidedTutorialSession run)
        {
            Ensure();
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _bookBack);
            Rect Region(int n) { var r = HowToPlay.TutorialRegion(Screen.width, Screen.height, n); return new Rect(r.X, r.Y, r.W, r.H); }
            var feedback = run.Phase == TutorialPhase.Feedback;
            GUI.Label(Region(0), feedback ? HowToPlay.TutorialResultTitle(true, run.Successes)
                : HowToPlay.TutorialAttemptTitle(run.Lesson.Id, run.Successes), _bookTitle);
            TutorialText(Region(1), HowToPlay.TutorialTitle(run.Lesson.Id), HowToPlay.TutorialGoal(run.Lesson.Id),
                HowToPlay.TutorialControls(run.Lesson.Id, BookScheme.Current));
            TutorialText(Region(2), feedback ? "GOOD WORK" : "THE SETUP",
                feedback ? HowToPlay.TutorialFeedbackText("guided-complete") : HowToPlay.TutorialSetup(run.Lesson.Id),
                feedback ? "" : HowToPlay.TutorialRule);
            var count = feedback ? 3 : 2;
            TutorialButton(0, count, HowToPlay.TutorialButton(feedback && !run.Passed ? -7 : feedback ? -6 : -2, BookScheme.Current));
            if (feedback) TutorialButton(1, count, HowToPlay.TutorialButton(-5, BookScheme.Current));
            TutorialButton(count - 1, count, HowToPlay.TutorialButton(-3, BookScheme.Current));
        }

        public static void GuidedHint(GuidedTutorialSession run)
        {
            if (run?.Phase != TutorialPhase.Attempt) return;
            Ensure();
            var text = HowToPlay.TutorialTitle(run.Lesson.Id) + " · " + HowToPlay.TutorialCount(run.Successes)
                + "    " + HowToPlay.GuidedNext(run.Missing);
            GUI.DrawTexture(new Rect(24, Screen.height - 100, Screen.width - 48, 40), _ink);
            GUI.Label(new Rect(32, Screen.height - 96, Screen.width - 64, 34), text, _bookTabSelected);
        }
    }
}
