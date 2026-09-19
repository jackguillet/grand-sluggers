using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public static partial class HudView
    {
        public static void Tutorials(bool menu, TutorialLesson[] lessons, int pick,
            TutorialSession run, TutorialProgress progress, string profile)
        {
            Ensure();
            var scheme = BookScheme.Current;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _bookBack);
            Rect Region(int n) { var r = HowToPlay.TutorialRegion(Screen.width, Screen.height, n); return new Rect(r.X, r.Y, r.W, r.H); }
            var header = Region(0); var left = Region(1); var right = Region(2); var footer = Region(3);
            if (menu)
            {
                GUI.Label(header, HowToPlay.TutorialMenuTitle, _bookTitle);
                for (var i = 0; i <= lessons.Length; i++)
                {
                    var bounds = HowToPlay.TutorialRow(Screen.width, Screen.height, i, lessons.Length + 1);
                    var row = new Rect(bounds.X, bounds.Y, bounds.W, bounds.H);
                    var title = i == lessons.Length ? HowToPlay.TutorialFree : HowToPlay.TutorialTitle(lessons[i].Id);
                    if (i < lessons.Length && progress.Has(lessons[i], profile)) title = "✓ " + title;
                    GUI.DrawTexture(row, i == pick ? _ink : _bookCard);
                    GUI.Label(row, title, i == pick ? _bookTabSelected : _bookTab);
                }
                var id = pick < lessons.Length ? lessons[pick].Id : null;
                TutorialText(right, id == null ? HowToPlay.TutorialFree : HowToPlay.TutorialTitle(id),
                    id == null ? HowToPlay.TutorialFreeGoal : HowToPlay.TutorialGoal(id),
                    id == null ? "" : HowToPlay.TutorialControls(id, scheme));
                TutorialButton(0, 2, HowToPlay.TutorialButton(-2, scheme));
                TutorialButton(1, 2, HowToPlay.TutorialButton(-4, scheme));
            }
            else
            {
                var feedback = run.Phase == TutorialPhase.Feedback;
                GUI.Label(header, feedback ? run.Feedback.Success ? HowToPlay.TutorialComplete : HowToPlay.TutorialRetry
                    : HowToPlay.TutorialTitle(run.Lesson.Id), _bookTitle);
                TutorialText(left, HowToPlay.TutorialTitle(run.Lesson.Id), HowToPlay.TutorialGoal(run.Lesson.Id),
                    HowToPlay.TutorialControls(run.Lesson.Id, scheme));
                TutorialText(right, feedback ? "" : "THE SETUP", feedback
                    ? HowToPlay.TutorialFeedbackText(run.Feedback.Code) : HowToPlay.TutorialSetup(run.Lesson.Id), "");
                var n = feedback ? 3 : 2;
                TutorialButton(0, n, HowToPlay.TutorialButton(feedback ? -6 : -2, scheme));
                if (feedback) TutorialButton(1, n, HowToPlay.TutorialButton(-5, scheme));
                TutorialButton(n - 1, n, HowToPlay.TutorialButton(-3, scheme));
            }
        }

        static void TutorialText(Rect r, string title, string goal, string controls)
        {
            GUI.DrawTexture(r, _bookCard);
            r = new Rect(r.x + 16, r.y + 12, r.width - 32, r.height - 24);
            var text = string.IsNullOrEmpty(title) ? goal : title + "\n\n" + goal;
            if (!string.IsNullOrEmpty(controls)) text += "\n\n" + controls;
            DrawFittingBookText(r, text, "tutorial");
        }

        static void TutorialButton(int index, int count, string text)
        {
            var b = HowToPlay.TutorialAction(Screen.width, Screen.height, index, count);
            var r = new Rect(b.X, b.Y, b.W, b.H);
            GUI.DrawTexture(r, _ink);
            GUI.Label(r, text, _bookTabSelected);
        }
    }
}
