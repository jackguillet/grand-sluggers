using System;
using System.Linq;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The Tutorials section at the couch (a real director since #1042): the lesson catalog and saved progress, the menu
    /// (category, pick, its gates), the lesson card's clock and saved flag, the reason the last attempt failed, and the
    /// guided lessons. No tutorial baseball rules live here — the sim's lesson runner owns them — and building a lesson's
    /// match and scene stays with the flow, which reads and drives this state through the methods below.
    /// </summary>
    public sealed class TutorialDirector
    {
        TutorialLesson[] _all = Array.Empty<TutorialLesson>();
        string[] _categories = Array.Empty<string>();
        TutorialLesson[] _choices = Array.Empty<TutorialLesson>();
        int _category, _pick;
        MenuNav.Gate _x, _y;

        /// <summary>The guided lessons (front of house and the couch).</summary>
        public readonly GuidedTutorialDirector Guided = new GuidedTutorialDirector();

        public TutorialCatalog Catalog { get; private set; }
        public TutorialProgress Progress { get; } = new TutorialProgress();
        public string Profile => Catalog.Profile;

        /// <summary>The lessons menu is showing.</summary>
        public bool MenuOpen { get; private set; }
        /// <summary>Unscaled seconds the current lesson card has shown; a card ignores input for its first 0.2 s.</summary>
        public float CardAge { get; private set; }
        /// <summary>The finished attempt's progress is saved.</summary>
        public bool Saved { get; private set; }
        /// <summary>Why the previous attempt failed, shown on the retry's hint; null after a pass or a fresh start.</summary>
        public TutorialFeedback PreviousFeedback { get; set; }
        bool _wasModal;

        string SaveKey(TutorialLesson lesson) => "tutorial.v2." + Catalog.Profile + "." + lesson.Id + "." + lesson.Revision;

        /// <summary>Open the menu on <paramref name="selected"/>'s category: load the catalog once and restore saved progress.</summary>
        public void OpenMenu(ContentCatalog content, string selected)
        {
            Catalog ??= TutorialCatalog.Load(content);
            _all = Catalog.Lessons.Where(l => l.Status == "implemented" && l.Profiles.Contains(Catalog.Profile)).ToArray();
            Progress.RestorePractice(_all.Select(l => new TutorialPracticeProgress(
                l.Id, l.Revision, Catalog.Profile, PlayerPrefs.GetInt(SaveKey(l), 0))), Catalog);
            _categories = HowToPlay.TutorialCategories(_all);
            SelectCategory(_category, selected);
            MenuOpen = true; CardAge = 0;
            _y.Catch(Controls.MenuY);
        }

        public void CloseMenu() => MenuOpen = false;

        void SelectCategory(int category, string selected = null)
        {
            _category = (category % _categories.Length + _categories.Length) % _categories.Length;
            _choices = _all.Where(l => l.Category == _categories[_category]).ToArray();
            _pick = Math.Max(0, Array.FindIndex(_choices, l => l.Id == selected));
            _x.Catch(Controls.MenuX); _y.Catch(Controls.MenuY);
        }

        /// <summary>Lesson <paramref name="id"/> is being set up: the menu closes on its category and its card starts fresh.</summary>
        public TutorialLesson Prepare(string id)
        {
            PreviousFeedback = null;
            var lesson = _all.First(l => l.Id == id);
            SelectCategory(Array.IndexOf(_categories, lesson.Category), id);
            return lesson;
        }

        /// <summary>A lesson's match is built: the card starts fresh and counts as already shown.</summary>
        public void LessonReady()
        {
            MenuOpen = false; CardAge = 0;
            Saved = false; _wasModal = true;
        }

        /// <summary>An attempt began: its card clock restarts.</summary>
        public void AttemptBegun() => CardAge = 0;

        /// <summary>A lesson's feedback opened: its card starts fresh and its result is not saved yet.</summary>
        public void FeedbackOpened()
        {
            CardAge = 0; Saved = false;
        }

        /// <summary>Save a finished attempt's successes once.</summary>
        public void Save(TutorialLesson lesson, int successes)
        {
            if (Saved) return;
            PlayerPrefs.SetInt(SaveKey(lesson), successes);
            PlayerPrefs.Save(); Saved = true;
        }

        public void Age(float dt) => CardAge += dt;

        /// <summary>
        /// The card's input gate: false while no card is modal (the lesson plays); on a card that just appeared the clock
        /// restarts, and a card takes input only after 0.2 s. <paramref name="settled"/> is whether it takes input now.
        /// </summary>
        public bool Modal(bool modal, out bool settled)
        {
            settled = false;
            if (!modal) { _wasModal = false; return false; }
            if (!_wasModal) { CardAge = 0; _wasModal = true; }
            settled = CardAge >= .2f;
            return true;
        }

        public enum MenuStep { Stay, Choose, Leave }

        /// <summary>The menu's pad: stick left/right changes category, up/down the lesson; South chooses; East leaves.</summary>
        public MenuStep TickMenu(float dt, bool confirm, bool back)
        {
            var categoryStep = _x.Tick(Controls.MenuX, Controls.MenuTapX, dt);
            if (categoryStep != 0) { SelectCategory(_category + categoryStep); return MenuStep.Stay; }
            var count = Math.Max(1, _choices.Length);
            var step = _y.Tick(Controls.MenuY, Controls.MenuTapY, dt);
            if (step != 0) _pick = (_pick - step % count + count) % count;
            if (confirm) return MenuStep.Choose;
            if (!back) return MenuStep.Stay;
            MenuOpen = false;
            return MenuStep.Leave;
        }

        /// <summary>The picked lesson, or null when the category is empty (the menu then opens free practice).</summary>
        public TutorialLesson Chosen => _choices.Length == 0 ? null : _choices[_pick];

        /// <summary>The next lesson after <paramref name="id"/> in the whole list, wrapping.</summary>
        public string NextAfter(string id) => _all[(Array.FindIndex(_all, l => l.Id == id) + 1) % _all.Length].Id;

        /// <summary>The menu and a regular lesson's card; <paramref name="running"/> is the lesson in play, or null.</summary>
        public void Draw(TutorialSession running)
        {
            var start = HowToPlay.TutorialPageStart(_pick);
            HudView.Tutorials(MenuOpen, _choices.Skip(start).Take(HowToPlay.TutorialPageSize).ToArray(), _pick - start,
                running, Progress, Catalog.Profile,
                _categories, _category, start / HowToPlay.TutorialPageSize, HowToPlay.TutorialPages(_choices.Length));
        }
    }
}
