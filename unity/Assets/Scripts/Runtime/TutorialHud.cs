using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public static partial class HudView
    {
        public static void Tutorials(bool menu, TutorialLesson[] lessons, int pick,
            TutorialSession run, TutorialProgress progress, string profile,
            string[] categories, int category, int page, int pages)
        {
            Ensure();
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _bookBack);
            Rect Region(int n) { var r = menu ? HowToPlay.TutorialBrowseRegion(Screen.width, Screen.height, n) : HowToPlay.TutorialRegion(Screen.width, Screen.height, n); return new Rect(r.X, r.Y, r.W, r.H); }
            var header = Region(0); var left = Region(1); var right = Region(2); var footer = Region(3);
            if (menu)
            {
                GUI.Label(new Rect(header.x, header.y, header.width * .30f, header.height), HowToPlay.TutorialMenuTitle, _bookTitle);
                GUI.Label(new Rect(header.x + header.width * .30f, header.y, header.width * .52f, header.height),
                    HowToPlay.TutorialBrowseHint, _bookTab);
                for (var i = 0; i < categories.Length; i++)
                {
                    var b = HowToPlay.TutorialTab(Screen.width, Screen.height, i, categories.Length);
                    var tab = new Rect(b.X, b.Y, b.W, b.H);
                    GUI.DrawTexture(tab, i == category ? _ink : _bookCard);
                    GUI.Label(tab, HowToPlay.TutorialCategoryTitle(categories[i]), i == category ? _bookTabSelected : _bookTab);
                }
                if (pages > 1)
                {
                    foreach (var direction in new[] { -1, 1 })
                    {
                        var b = HowToPlay.TutorialPageButton(Screen.width, Screen.height, direction);
                        var button = new Rect(b.X, b.Y, b.W, b.H);
                        GUI.DrawTexture(button, _ink);
                        GUI.Label(button, direction < 0 ? "‹" : "›", _bookTabSelected);
                    }
                    GUI.Label(new Rect(header.xMax - 128, header.y, 72, 54), $"{page + 1}/{pages}", _bookTab);
                }
                for (var i = 0; i < Mathf.Max(1, lessons.Length); i++)
                {
                    var bounds = HowToPlay.TutorialRow(Screen.width, Screen.height, i, Mathf.Max(1, lessons.Length));
                    var row = new Rect(bounds.X, bounds.Y, bounds.W, bounds.H);
                    var title = i == lessons.Length ? HowToPlay.TutorialFree : HowToPlay.TutorialTitle(lessons[i].Id);
                    if (i < lessons.Length)
                        title = (progress.Has(lessons[i], profile) ? "✓ " : "") + title
                            + " · " + HowToPlay.TutorialCount(progress.Count(lessons[i], profile));
                    GUI.DrawTexture(row, i == pick ? _ink : _bookCard);
                    GUI.Label(row, title, i == pick ? _bookTabSelected : _bookTab);
                }
                var id = pick < lessons.Length ? lessons[pick].Id : null;
                TutorialText(right, id == null ? HowToPlay.TutorialFree : HowToPlay.TutorialTitle(id),
                    id == null ? HowToPlay.TutorialFreeGoal : HowToPlay.TutorialGoal(id),
                    id == null ? "" : HowToPlay.TutorialControls(id, profile));
                TutorialButton(0, 2, HowToPlay.TutorialButton(-2));
                TutorialButton(1, 2, HowToPlay.TutorialButton(-4));
            }
            else
            {
                var feedback = run.Phase == TutorialPhase.Feedback;
                GUI.Label(header, feedback ? HowToPlay.TutorialResultTitle(run.Feedback.Success, run.Successes)
                    : HowToPlay.TutorialAttemptTitle(run.Lesson.Id, run.Successes), _bookTitle);
                TutorialText(left, HowToPlay.TutorialTitle(run.Lesson.Id), HowToPlay.TutorialGoal(run.Lesson.Id),
                    HowToPlay.TutorialControls(run.Lesson.Id, profile));
                TutorialText(right, feedback ? "" : HowToPlay.TutorialSetupHeader, feedback
                    ? HowToPlay.TutorialFeedbackText(run.Feedback.Code) : HowToPlay.TutorialSetup(run.Lesson.Id, profile), feedback ? "" : HowToPlay.TutorialRule);
                var n = feedback ? 3 : 2;
                TutorialButton(0, n, HowToPlay.TutorialButton(feedback ? run.Feedback.Success && !run.Passed ? -7 : -6 : -2));
                if (feedback) TutorialButton(1, n, HowToPlay.TutorialButton(-5));
                TutorialButton(n - 1, n, HowToPlay.TutorialButton(-3));
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
