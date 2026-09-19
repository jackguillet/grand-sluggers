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
        TutorialLesson[] _tutorialChoices = Array.Empty<TutorialLesson>();
        bool _tutorialMenu;
        int _tutorialPick;
        float _tutorialUiAge;
        int _tutorialClick = -1;
        MenuNav.Gate _tutorialY;
        bool _tutorialSaved;
        bool _tutorialWasModal;
        bool TutorialOn => _coach != null && _coach.Tutorial != null;
        bool TutorialFeedbackReady => TutorialOn && _coach.Tutorial.Phase == TutorialPhase.Feedback
            && (_coach.Tutorial.IsFieldLesson || !_coach.PlayerBats || _phase == Phase.Result || _coach.Tutorial.Feedback.Code == "timeout");
        bool TutorialModal => _tutorialMenu || TutorialOn && (_coach.Tutorial.Phase == TutorialPhase.Brief || TutorialFeedbackReady);

        string TutorialSaveKey(TutorialLesson lesson) => "tutorial.v1." + _tutorials.Profile + "." + lesson.Id + "." + lesson.Revision;

        void OpenTutorials()
        {
            _coach?.Stop();
            ReleaseMatchSeats();
            _tutorials ??= TutorialCatalog.Load(_content);
            _tutorialChoices = _tutorials.Lessons.Where(l => l.Status == "implemented" && l.Profiles.Contains(_tutorials.Profile)).ToArray();
            _tutorialProgress.Restore(_tutorialChoices.Where(l => PlayerPrefs.GetInt(TutorialSaveKey(l), 0) == 1)
                .Select(l => new TutorialCompletion(l.Id, l.Revision, _tutorials.Profile)), _tutorials);
            _tutorialMenu = true; _tutorialUiAge = 0; _tutorialClick = -1;
            _tutorialY.Catch(Controls.MenuY);
            _phase = Phase.Title; _cam.Play("title");
        }

        void PrepareTutorial(string id)
        {
            ReleaseMatchSeats();
            _mode = PlayMode.Training; ParkId = Training.ParkId;
            if (_coach == null) _coach = gameObject.AddComponent<TrainingDirector>();
            _coach.BeginTutorial(_content, _tutorials, id, _tutorialProgress);
            _match = _coach.Tutorial.Match;
            HomeCaptain = _match.Home.Captain.Id; AwayCaptain = _match.Away.Captain.Id;
            _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
            _spec.Build(transform); _items.Build(transform); _stars?.Build(transform);
            _clip = null; _hlPath = null;
            _tutorialMenu = false; _tutorialUiAge = 0; _tutorialClick = -1;
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
            if (!_tutorialMenu && !TutorialOn) return false;
            _tutorialUiAge += dt;
            if (!TutorialModal)
            {
                _tutorialWasModal = false;
                if (!_coach.Tutorial.IsFieldLesson && _coach.Tutorial.Phase == TutorialPhase.Attempt)
                {
                    var left = (double)dt;
                    while (left > 0) { var step = Math.Min(left, .05); _coach.Tutorial.Tick(step); left -= step; }
                }
                return false;
            }
            if (!_tutorialWasModal) { _tutorialUiAge = 0; _tutorialWasModal = true; }
            if (_tutorialUiAge < .2f) return true;
            var click = _tutorialClick; _tutorialClick = -1;
            if (_tutorialMenu)
            {
                var count = _tutorialChoices.Length + 1; // preserve the existing free-practice escape hatch
                var step = _tutorialY.Tick(Controls.MenuY, Controls.MenuTapY, dt);
                if (step != 0) _tutorialPick = (_tutorialPick - step % count + count) % count;
                if (click >= 0) { _tutorialPick = click; ChooseTutorialMenu(); }
                else if (Controls.SouthDown) ChooseTutorialMenu();
                else if (Controls.EastDown || click == -4) { _tutorialMenu = false; _mode = PlayMode.Exhibition; _t = 0; }
                return true;
            }
            if (_coach.Tutorial.Phase == TutorialPhase.Brief)
            {
                if (Controls.SouthDown || click == -2) BeginTutorialAttempt();
                else if (Controls.EastDown || click == -3) OpenTutorials();
                return true;
            }
            if (TutorialFeedbackReady)
            {
                if (_coach.Tutorial.Feedback.Success && !_tutorialSaved)
                {
                    PlayerPrefs.SetInt(TutorialSaveKey(_coach.Tutorial.Lesson), 1);
                    PlayerPrefs.Save(); _tutorialSaved = true;
                }
                if (Controls.SouthDown || click == -2) PrepareTutorial(_coach.Tutorial.Lesson.Id);
                else if (Controls.WestDown || click == -5)
                {
                    var next = (Array.FindIndex(_tutorialChoices, l => l.Id == _coach.Tutorial.Lesson.Id) + 1) % _tutorialChoices.Length;
                    _tutorialPick = next; PrepareTutorial(_tutorialChoices[next].Id);
                }
                else if (Controls.EastDown || click == -3) OpenTutorials();
                return true;
            }
            return false;
        }

        void ChooseTutorialMenu()
        {
            if (_tutorialPick == _tutorialChoices.Length)
            {
                _tutorialMenu = false; PracticePick = PracticeLesson.Free; BeginTraining();
            }
            else PrepareTutorial(_tutorialChoices[_tutorialPick].Id);
        }

        bool DrawTutorialUi()
        {
            if (!TutorialModal) return false;
            var click = HudView.Tutorials(_tutorialMenu, _tutorialChoices, _tutorialPick,
                TutorialOn ? _coach.Tutorial : null, _tutorialProgress, _tutorials.Profile);
            if (click != -1) _tutorialClick = click;
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
            var pad = FieldInput();
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
