using System;
using System.Collections.Generic;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The guided lessons that teach the front of house and the couch (T-G01 lineup, T-G05 two pads, T-G06 Call time and
    /// its stick and device cards, T-G07 match settings; a real director since #1042). It owns the guided session, the
    /// player's own Exhibition pick and rules while a lesson borrows the match, and every observation a lesson credits.
    /// Building the match and the scene stays with the flow; an observation reports whether it opened the lesson's
    /// feedback, and the flow resets its tutorial card on that.
    /// </summary>
    public sealed class GuidedTutorialDirector
    {
        /// <summary>The pick a guided lesson plays: the player's own Exhibition, remembered on the lesson's first start.</summary>
        public readonly record struct Pick(string Home, string Away, string Park, bool Night, bool Pad1Home, int Seed);

        Pick _playerPick;
        ExhibitionSettings _lentFrom;
        int _lentInnings;
        string _lentDifficulty;

        /// <summary>The running guided lesson, or null.</summary>
        public GuidedTutorialSession Session { get; private set; }

        public static bool IsGuided(string id) => id is "T-G01" or "T-G05" or "T-G06" or "T-G06-R" or "T-G06-C" or "T-G07";

        /// <summary>Lesson <paramref name="id"/> is running and its attempt is open.</summary>
        public bool Attempt(string id) => Session?.Lesson.Id == id && Session.Phase == TutorialPhase.Attempt;

        /// <summary>
        /// Start (or restart) <paramref name="lesson"/>. The player's pick is remembered on the lesson's first start and
        /// played on every retry; the T-G06 lessons play at the training park. Returns the pick the match must use.
        /// </summary>
        public Pick Start(TutorialLesson lesson, string profile, TutorialProgress progress, Pick current)
        {
            if (Session == null || Session.Lesson.Id != lesson.Id) _playerPick = current;
            Session = new GuidedTutorialSession(lesson, profile, progress);
            return lesson.Id.StartsWith("T-G06", StringComparison.Ordinal) ? _playerPick with { Park = Training.ParkId } : _playerPick;
        }

        /// <summary>The lesson ends (the menu, another lesson, the title).</summary>
        public void Exit()
        {
            Session?.Exit();
            Session = null;
        }

        /// <summary>Whether the attempt this lesson needs can begin: the pad lessons wait for a controller.</summary>
        public bool CanBegin(int padCount) => !(Session.Lesson.Id is "T-G06-R" or "T-G06-C" && padCount == 0);

        /// <summary>The T-G06 lessons start at SET; the others open the field.</summary>
        public bool StartsOnSet => Session.Lesson.Id.StartsWith("T-G06", StringComparison.Ordinal);

        /// <summary>T-G07 edits a fresh default rule set: keep the player's own rules to give back when the lesson ends.</summary>
        public ExhibitionSettings LendSettings(ExhibitionSettings own, int innings, string difficulty)
        {
            if (_lentFrom == null) (_lentFrom, _lentInnings, _lentDifficulty) = (own, innings, difficulty);
            return new ExhibitionSettings();
        }

        /// <summary>The player's own rules back, or false when none were lent.</summary>
        public bool ReturnSettings(out ExhibitionSettings own, out int innings, out string difficulty)
        {
            (own, innings, difficulty) = (_lentFrom, _lentInnings, _lentDifficulty);
            if (_lentFrom == null) return false;
            _lentFrom = null;
            return true;
        }

        /// <summary>Credit <paramref name="action"/> to lesson <paramref name="lesson"/>'s open attempt. True when it opened the feedback.</summary>
        public bool Observe(string lesson, GuidedAction action) => Attempt(lesson) && Observe(action);

        /// <summary>Credit <paramref name="action"/> to the running lesson. True when it opened the feedback.</summary>
        public bool Observe(GuidedAction action) =>
            Session != null && Session.Observe(action) && Session.Phase == TutorialPhase.Feedback;

        /// <summary>T-G07: player 1's settings edit as the rule owner typed it, and whether it cleared a human ready.</summary>
        public void RuleEdit(int row, LineupSeat seat, string refusal, bool clearedReady)
        {
            if (Attempt("T-G07")) Session.ObserveRuleEdit(row, seat, refusal, clearedReady);
        }

        /// <summary>T-G07: a seat's ready on the match-settings step.</summary>
        public void ReadyChanged(LineupSeat seat, bool onSettings, bool ready)
        {
            if (Attempt("T-G07") && onSettings) Session.ObserveReady(seat, ready);
        }

        /// <summary>T-G07: the match starts from the settings step with these human seats. True when it was observed.</summary>
        public bool SettingsStart(bool onSettings, IReadOnlyList<LineupSeat> humans)
        {
            if (!Attempt("T-G07") || !onSettings) return false;
            Session.ObserveSettingsStart(humans);
            return true;
        }

        /// <summary>T-G06-R: a seated controller went away.</summary>
        public void SeatLost(LineupSeat seat)
        {
            if (Attempt("T-G06-R")) Session.ObserveSeatLost(seat);
        }

        /// <summary>T-G06-R: the lost controller came back. True when that passed the lesson.</summary>
        public bool SeatRecovered(LineupSeat seat) => Attempt("T-G06-R") && Session.ObserveSeatRecovered(seat);

        /// <summary>T-G01: player 1 dropped a left-handed hitter into the roster. True when it opened the feedback.</summary>
        public bool LineupDrop(LineupSeat seat, Character before, bool accepted) =>
            seat == LineupSeat.Pad1 && accepted && before != null && before.Bats == Hand.L
            && Observe("T-G01", GuidedAction.LeftHandedRosterDrop);

        /// <summary>T-G05: both seats are human and bound to two different physical controllers. True when it opened the feedback.</summary>
        public bool SeatsBound(bool bound, bool bothHuman, int? padOne, int? padTwo) =>
            bound && bothHuman && padOne.HasValue && padTwo.HasValue && padOne.Value != padTwo.Value
            && Observe("T-G05", GuidedAction.TwoPhysicalSeatsBound);
    }
}
