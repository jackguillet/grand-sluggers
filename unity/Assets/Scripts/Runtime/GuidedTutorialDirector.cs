using System;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed partial class MatchDirector
    {
        static bool GuidedLesson(string id) => id is "T-G01" or "T-G05" or "T-G06" or "T-G06-R" or "T-G06-C" or "T-G07";
        bool GuidedAttempt(string id) => _guided?.Lesson.Id == id && _guided.Phase == TutorialPhase.Attempt;
        string _guidedHomeCaptain, _guidedAwayCaptain, _guidedPark;
        bool _guidedNight, _guidedPad1Home;
        int _guidedSeed;
        ExhibitionSettings _exhibitionSettings;
        int _exhibitionInnings;
        string _exhibitionDifficulty;

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
            if (needsPad && Controls.PadCount == 0)
                return;
            _lineup = null; // Every lesson attempt needs a fresh roster, unlike Back during setup.
            if (_guided.Lesson.Id == "T-G07") LendGuidedSettings();
            _guided.Begin();
            _tutorialUiAge = 0;
            Controls.CatchPlay();
            if (!onSet) { OpenField(); return; }
            _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
            _spec.Build(transform); _items.Build(transform); _stars?.Build(transform);
            _clip = null; _hlPath = null;
            BeginSet();
        }

        /// <summary>T-G07 edits a fresh default rule set; the player's own Exhibition rules come back when the lesson ends.</summary>
        void LendGuidedSettings()
        {
            if (_exhibitionSettings == null)
            {
                _exhibitionSettings = _settings; _exhibitionInnings = Innings; _exhibitionDifficulty = Difficulty;
            }
            _settings = new ExhibitionSettings();
            Innings = _settings.Innings; Difficulty = _settings.Difficulty;
        }

        void ReturnGuidedSettings()
        {
            if (_exhibitionSettings == null) return;
            _settings = _exhibitionSettings; Innings = _exhibitionInnings; Difficulty = _exhibitionDifficulty;
            _exhibitionSettings = null;
        }

        void GuidedStadiumChosen()
        {
            if (GuidedAttempt("T-G07")) GuidedObserve(GuidedAction.StadiumChosen);
        }

        /// <summary>Player 1's settings edit as the rule owner typed it, and whether it cleared a human ready.</summary>
        void GuidedRuleEdit(int row, LineupSeat seat, string refusal, bool clearedReady)
        {
            if (GuidedAttempt("T-G07")) _guided.ObserveRuleEdit(row, seat, refusal, clearedReady);
        }

        void GuidedReadyChanged(LineupSeat seat)
        {
            if (GuidedAttempt("T-G07") && _lineup != null && _lineup.Step == LineupStep.MatchSettings)
                _guided.ObserveReady(seat, _lineup.IsReady(seat));
        }

        void GuidedSettingsStart()
        {
            if (!GuidedAttempt("T-G07") || _lineup == null || _lineup.Step != LineupStep.MatchSettings) return;
            var humans = new System.Collections.Generic.List<LineupSeat>();
            if (_lineup.HomeSeat != LineupSeat.Cpu) humans.Add(_lineup.HomeSeat);
            if (_lineup.AwaySeat != LineupSeat.Cpu) humans.Add(_lineup.AwaySeat);
            _guided.ObserveSettingsStart(humans);
            _tutorialUiAge = 0; _tutorialSaved = false;
        }

        /// <summary>Call time's Reset stick card closed (<see cref="PursuitSeatDirector"/>); a recalibrated close is T-G06-C's action.</summary>
        void StickResetClosed(bool recalibrated)
        {
            _t = 0;
            if (recalibrated && GuidedAttempt("T-G06-C")) GuidedObserve(GuidedAction.StickRecalibrated);
        }

        void GuidedObserve(GuidedAction action)
        {
            if (_guided == null || !_guided.Observe(action) || _guided.Phase != TutorialPhase.Feedback) return;
            _tutorialUiAge = 0; _tutorialSaved = false;
            _match.SetPaused(false);
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
                _match.SetPaused(false);
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
