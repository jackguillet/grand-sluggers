using System;
using System.Linq;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>Presentation adapter for the shared lesson runner. No tutorial baseball rules live here.</summary>
    public sealed partial class MatchDirector
    {
        TutorialCatalog _tutorials;
        TutorialProgress _tutorialProgress = new TutorialProgress();
        TutorialLesson[] _tutorialAll = Array.Empty<TutorialLesson>();
        string[] _tutorialCategories = Array.Empty<string>();
        int _tutorialCategory;
        MenuNav.Gate _tutorialX;
        TutorialLesson[] _tutorialChoices = Array.Empty<TutorialLesson>();
        bool _tutorialMenu;
        int _tutorialPick;
        float _tutorialUiAge;
        MenuNav.Gate _tutorialY;
        bool _tutorialSaved;
        bool _tutorialWasModal;
        GuidedTutorialSession _guided;
        bool TutorialOn => _coach != null && _coach.Tutorial != null;
        bool TutorialFeedbackReady => TutorialOn && _coach.Tutorial.Phase == TutorialPhase.Feedback
            && (_coach.Tutorial.IsFieldLesson || !_coach.PlayerBats || _phase == Phase.Result || _coach.Tutorial.Feedback.Code == "timeout");
        bool TutorialModal => _tutorialMenu || TutorialOn && (_coach.Tutorial.Phase == TutorialPhase.Brief || TutorialFeedbackReady)
            || _guided != null && _guided.Phase is TutorialPhase.Brief or TutorialPhase.Feedback;

        string TutorialSaveKey(TutorialLesson lesson) => "tutorial.v2." + _tutorials.Profile + "." + lesson.Id + "." + lesson.Revision;

        void OpenTutorials()
        {
            var selected = _guided?.Lesson.Id ?? (TutorialOn ? _coach.Tutorial.Lesson.Id : null);
            _guided?.Exit(); _guided = null;
            _coach?.Stop();
            ReleaseMatchSeats();
            _tutorials ??= TutorialCatalog.Load(_content);
            _tutorialAll = _tutorials.Lessons.Where(l => l.Status == "implemented" && l.Profiles.Contains(_tutorials.Profile)).ToArray();
            _tutorialProgress.RestorePractice(_tutorialAll.Select(l => new TutorialPracticeProgress(
                l.Id, l.Revision, _tutorials.Profile, PlayerPrefs.GetInt(TutorialSaveKey(l), 0))), _tutorials);
            _tutorialCategories = HowToPlay.TutorialCategories(_tutorialAll);
            SelectTutorialCategory(_tutorialCategory, selected);
            _tutorialMenu = true; _tutorialUiAge = 0;
            _tutorialY.Catch(Controls.MenuY);
            _phase = Phase.Title; _cam.Play("title");
        }

        void SelectTutorialCategory(int category, string selected = null)
        {
            _tutorialCategory = (category % _tutorialCategories.Length + _tutorialCategories.Length) % _tutorialCategories.Length;
            _tutorialChoices = _tutorialAll.Where(l => l.Category == _tutorialCategories[_tutorialCategory]).ToArray();
            _tutorialPick = Math.Max(0, Array.FindIndex(_tutorialChoices, l => l.Id == selected));
            _tutorialX.Catch(Controls.MenuX); _tutorialY.Catch(Controls.MenuY);
        }

        void PrepareTutorial(string id)
        {
            var lesson = _tutorialAll.First(l => l.Id == id);
            SelectTutorialCategory(Array.IndexOf(_tutorialCategories, lesson.Category), id);
            if (GuidedLesson(id)) { PrepareGuidedTutorial(lesson); return; }
            _guided?.Exit(); _guided = null;
            ReleaseMatchSeats();
            _mode = PlayMode.Training; ParkId = Training.ParkId;
            if (_coach == null) _coach = gameObject.AddComponent<TrainingDirector>();
            _coach.BeginTutorial(_content, _tutorials, id, _tutorialProgress);
            _match = _coach.Tutorial.Match;
            HomeCaptain = _match.Home.Captain.Id; AwayCaptain = _match.Away.Captain.Id;
            _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
            _spec.Build(transform); _items.Build(transform); _stars?.Build(transform);
            _clip = null; _hlPath = null;
            _tutorialMenu = false; _tutorialUiAge = 0;
            _tutorialSaved = false; _tutorialWasModal = true;
            BeginSet();
        }

        void BeginTutorialAttempt()
        {
            var run = _coach.Tutorial;
            run.Begin(); _tutorialUiAge = 0;
            Controls.CatchPlay();
            if (!run.IsFieldLesson) return;
            _pending = run.LastHit; _preview = _match.LivePlay.Preview;
            _pitch = _match.LivePlay.Pitch; _swing = _match.LivePlay.Swing;
            _cpuField = null;
            _park.Ball.Release();
            StartFly(_pending, alreadyLive: true);
        }

        bool TickTutorialUi(float dt)
        {
            if (!_tutorialMenu && !TutorialOn && _guided == null) return false;
            if (_guided != null && _guided.Phase == TutorialPhase.Attempt) return false;
            if (_guided != null && _guided.Phase == TutorialPhase.Feedback && !_tutorialSaved)
            {
                PlayerPrefs.SetInt(TutorialSaveKey(_guided.Lesson), _guided.Successes);
                PlayerPrefs.Save(); _tutorialSaved = true;
            }
            _tutorialUiAge += dt;
            if (TutorialOn && _coach.Tutorial.Feedback?.Success == true && !_tutorialSaved)
            {
                PlayerPrefs.SetInt(TutorialSaveKey(_coach.Tutorial.Lesson), _coach.Tutorial.Successes);
                PlayerPrefs.Save(); _tutorialSaved = true;
            }
            if (!TutorialModal)
            {
                _tutorialWasModal = false;
                if (!_coach.Tutorial.IsFieldLesson && !_match.LivePlay.Active && _coach.Tutorial.Phase == TutorialPhase.Attempt)
                {
                    var left = (double)dt;
                    while (left > 0) { var step = Math.Min(left, .05); _coach.Tutorial.Tick(step); left -= step; }
                }
                return false;
            }
            if (!_tutorialWasModal) { _tutorialUiAge = 0; _tutorialWasModal = true; }
            if (_tutorialUiAge < .2f) return true;
            var mouse = Controls.GuiMouse;
            var pageStart = HowToPlay.TutorialPageStart(_tutorialPick);
            var rows = Math.Max(1, Math.Min(HowToPlay.TutorialPageSize, _tutorialChoices.Length - pageStart));
            var click = Controls.PointerDown ? HowToPlay.TutorialHit(mouse.x, mouse.y, Screen.width, Screen.height,
                _tutorialMenu, rows, TutorialFeedbackReady) : -1;
            // Use the same Input System pointer and hit rectangles as the book/Call time menus.
            var confirm = Controls.SouthDown && !Controls.PointerDown;
            if (_tutorialMenu)
            {
                var categoryStep = _tutorialX.Tick(Controls.MenuX, Controls.MenuTapX, dt);
                if (categoryStep != 0) { SelectTutorialCategory(_tutorialCategory + categoryStep); return true; }
                if (Controls.PointerDown)
                {
                    var tab = HowToPlay.TutorialTabHit(mouse.x, mouse.y, Screen.width, Screen.height, _tutorialCategories.Length);
                    if (tab >= 0) { SelectTutorialCategory(tab); return true; }
                    var pages = HowToPlay.TutorialPages(_tutorialChoices.Length);
                    var direction = HowToPlay.TutorialPageHit(mouse.x, mouse.y, Screen.width, Screen.height);
                    if (pages > 1 && direction != 0)
                    {
                        var page = (pageStart / HowToPlay.TutorialPageSize + direction + pages) % pages;
                        _tutorialPick = page * HowToPlay.TutorialPageSize; return true;
                    }
                }
                var count = Math.Max(1, _tutorialChoices.Length);
                var step = _tutorialY.Tick(Controls.MenuY, Controls.MenuTapY, dt);
                if (step != 0) _tutorialPick = (_tutorialPick - step % count + count) % count;
                if (click >= 0) { _tutorialPick = pageStart + click; ChooseTutorialMenu(); }
                else if (confirm || click == -2) ChooseTutorialMenu();
                else if (Controls.EastDown || click == -4) { _tutorialMenu = false; _mode = PlayMode.Exhibition; _t = 0; }
                return true;
            }
            if (_guided != null && _guided.Phase == TutorialPhase.Brief)
            {
                if (confirm || click == -2) BeginGuidedAttempt();
                else if (Controls.EastDown || click == -3) OpenTutorials();
                return true;
            }
            if (_guided != null && _guided.Phase == TutorialPhase.Feedback)
            {
                if (confirm || click == -2)
                {
                    var continuePractice = !_guided.Passed;
                    var id = _guided.Lesson.Id;
                    PrepareTutorial(id);
                    if (continuePractice) BeginGuidedAttempt();
                }
                else if (Controls.WestDown || click == -5)
                {
                    var next = (Array.FindIndex(_tutorialAll, l => l.Id == _guided.Lesson.Id) + 1) % _tutorialAll.Length;
                    PrepareTutorial(_tutorialAll[next].Id);
                }
                else if (Controls.EastDown || click == -3) OpenTutorials();
                return true;
            }
            if (_coach.Tutorial.Phase == TutorialPhase.Brief)
            {
                if (confirm || click == -2) BeginTutorialAttempt();
                else if (Controls.EastDown || click == -3) OpenTutorials();
                return true;
            }
            if (TutorialFeedbackReady)
            {
                if (confirm || click == -2)
                {
                    var continuePractice = _coach.Tutorial.Feedback.Success && !_coach.Tutorial.Passed;
                    PrepareTutorial(_coach.Tutorial.Lesson.Id);
                    if (continuePractice) BeginTutorialAttempt();
                }
                else if (Controls.WestDown || click == -5)
                {
                    var next = (Array.FindIndex(_tutorialAll, l => l.Id == _coach.Tutorial.Lesson.Id) + 1) % _tutorialAll.Length;
                    PrepareTutorial(_tutorialAll[next].Id);
                }
                else if (Controls.EastDown || click == -3) OpenTutorials();
                return true;
            }
            return false;
        }

        void ChooseTutorialMenu()
        {
            if (_tutorialChoices.Length == 0)
            {
                _tutorialMenu = false; PracticePick = PracticeLesson.Free; BeginTraining();
            }
            else PrepareTutorial(_tutorialChoices[_tutorialPick].Id);
        }

        bool DrawTutorialUi()
        {
            if (!TutorialModal) return false;
            if (_guided != null)
            {
                HudView.GuidedTutorial(_guided);
                return true;
            }
            var start = HowToPlay.TutorialPageStart(_tutorialPick);
            HudView.Tutorials(_tutorialMenu, _tutorialChoices.Skip(start).Take(HowToPlay.TutorialPageSize).ToArray(), _tutorialPick - start,
                TutorialOn ? _coach.Tutorial : null, _tutorialProgress, _tutorials.Profile,
                _tutorialCategories, _tutorialCategory, start / HowToPlay.TutorialPageSize, HowToPlay.TutorialPages(_tutorialChoices.Length));
            return true;
        }

        bool ResolveTutorialOrAtBat(out AtBatResult hit, out PlayEvent finished)
        {
            if (!TutorialOn) return _match.BeginAtBat(_pitch, _swing, out hit, out finished);
            var run = _coach.Tutorial;
            if (_coach.PlayerPitches) run.Pitch(_pitch);
            else run.Swing(_swing);
            hit = run.LastHit; finished = run.LastPlay;
            return hit != null && hit.InPlay && finished == null;
        }

        LivePlayCommandResult TickTutorialField(float dt)
        {
            var run = _coach.Tutorial;
            var pad = run.IsOffenseLesson ? RunInput() : FieldInput();
            // Preserve simulation time through a long rendering frame without repeating edge-triggered commands.
            var left = (double)dt;
            LivePlayCommandResult result = new LivePlayCommandResult(_match.LivePlay.Snapshot);
            while (left > 0 && run.Phase == TutorialPhase.Attempt)
            {
                var step = Math.Min(left, .05);
                run.Tick(step, pad);
                result = run.LastTickResult ?? result;
                left -= step;
                if (left > 0) PlayLiveCues(result);
                pad = pad with { SouthDown = false, WestDown = false, EastDown = false, Swap = false, Cancel = false, Cutoff = false };
            }
            return result;
        }
    }
}
