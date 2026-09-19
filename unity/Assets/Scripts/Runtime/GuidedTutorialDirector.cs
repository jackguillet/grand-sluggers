using System;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed partial class MatchDirector
    {
        static bool GuidedLesson(string id) => id is "T-G01" or "T-G05" or "T-G06" or "T-G06-R" or "T-G06-C";
        bool GuidedAttempt(string id) => _guided?.Lesson.Id == id && _guided.Phase == TutorialPhase.Attempt;
        string _guidedHomeCaptain, _guidedAwayCaptain, _guidedPark;
        bool _guidedNight, _guidedPad1Home;
        int _guidedSeed;

        void PrepareGuidedTutorial(TutorialLesson lesson)
        {
            if (_guided == null || _guided.Lesson.Id != lesson.Id)
            {
                _guidedHomeCaptain = HomeCaptain; _guidedAwayCaptain = AwayCaptain;
                _guidedPark = ParkId; _guidedNight = Night; _guidedPad1Home = Pad1Home; _guidedSeed = Seed;
            }
            _coach?.Stop();
            ReleaseMatchSeats();
            _mode = PlayMode.Exhibition;
            _versusWanted = false;
            HomeCaptain = _guidedHomeCaptain; AwayCaptain = _guidedAwayCaptain;
            ParkId = lesson.Id.StartsWith("T-G06", StringComparison.Ordinal) ? Training.ParkId : _guidedPark;
            Night = _guidedNight; Pad1Home = _guidedPad1Home; Seed = _guidedSeed;
            _guided = new GuidedTutorialSession(lesson, _tutorials.Profile, _tutorialProgress);
            _tutorialSaved = false; _tutorialMenu = false; _tutorialUiAge = 0; _tutorialWasModal = true;
            _match = NewMatch();
            _phase = Phase.Title;
            _cam.Play("title");
        }

        void BeginGuidedAttempt()
        {
            var onSet = _guided.Lesson.Id.StartsWith("T-G06", StringComparison.Ordinal);
            var needsPad = _guided.Lesson.Id is "T-G06-R" or "T-G06-C";
            if (needsPad && (Controls.PadCount == 0 || Controls.Player1InputMode == Controls.P1InputMode.KeyboardMouse))
                return;
            _guided.Begin();
            _tutorialUiAge = 0;
            Controls.CatchPlay();
            if (!onSet) { OpenSelect(); return; }
            _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
            _spec.Build(transform); _items.Build(transform); _stars?.Build(transform);
            _clip = null; _hlPath = null;
            BeginSet();
        }

        void GuidedObserve(GuidedAction action)
        {
            if (_guided == null || !_guided.Observe(action) || _guided.Phase != TutorialPhase.Feedback) return;
            _tutorialUiAge = 0; _tutorialSaved = false;
            _match.SetPaused(false);
            _phase = Phase.Title;
            _cam.Play("title");
        }

        void GuidedSeatLost(LineupSeat seat)
        {
            if (GuidedAttempt("T-G06-R")) _guided.ObserveSeatLost(seat);
        }

        void GuidedSeatRecovered(LineupSeat seat)
        {
            if (GuidedAttempt("T-G06-R") && _guided.ObserveSeatRecovered(seat))
            {
                _tutorialUiAge = 0; _tutorialSaved = false;
                _match.SetPaused(false); _phase = Phase.Title; _cam.Play("title");
            }
        }

        void GuidedLineupDrop(LineupSeat seat, Character before, bool accepted)
        {
            if (GuidedAttempt("T-G01") && seat == LineupSeat.Pad1 && accepted && before != null && before.Bats == Hand.L)
                GuidedObserve(GuidedAction.LeftHandedRosterDrop);
        }

        void GuidedSeatsBound()
        {
            if (!GuidedAttempt("T-G05") || !_matchSeats.Bound || !LiveSeats.BothHuman) return;
            var one = Controls.SeatDeviceId(0);
            var two = Controls.SeatDeviceId(1);
            if (one.HasValue && two.HasValue && one.Value != two.Value)
                GuidedObserve(GuidedAction.TwoPhysicalSeatsBound);
        }
    }
}
