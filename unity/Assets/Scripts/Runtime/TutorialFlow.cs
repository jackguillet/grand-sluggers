using System;
using System.Collections.Generic;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The Tutorials section's flow (its own class since #1042): the lesson menu, a practice lesson's brief, attempt and
    /// feedback, and the guided lessons that teach the front of house. The menu, progress and card state are
    /// <see cref="TutorialDirector"/>'s and the guided sessions <see cref="GuidedTutorialDirector"/>'s; this class builds a
    /// lesson's match and pick, routes the card's pad, and tells a guided lesson what the player did. The coach, the scene
    /// build, the seats and the screens a lesson hands off to are the flow's (<see cref="ITutorialFlowHost"/>).
    /// </summary>
    internal sealed class TutorialFlow
    {
        readonly MatchScene _scene;
        readonly PlayState _play;
        readonly LiveFieldState _live;
        readonly FlowChoices _choices;
        readonly SeatPads _pads;
        readonly ITutorialFlowHost _host;

        public TutorialFlow(MatchScene scene, PlayState play, LiveFieldState live, FlowChoices choices, SeatPads pads, ITutorialFlowHost host)
        {
            _scene = scene; _play = play; _live = live; _choices = choices; _pads = pads; _host = host;
        }

        public readonly TutorialDirector Lessons = new TutorialDirector();
        public GuidedTutorialSession Guided => Lessons.Guided.Session;
        public bool TutorialOn => _host.Coach != null && _host.Coach.Tutorial != null;
        bool TutorialFeedbackReady => TutorialOn && _host.Coach.Tutorial.Phase == TutorialPhase.Feedback
            && (_host.Coach.Tutorial.IsFieldLesson || !_host.Coach.PlayerBats || _play.Phase == MatchDirector.Phase.Result || _host.Coach.Tutorial.Feedback.Code == "timeout");
        public bool TutorialModal => Lessons.MenuOpen || TutorialOn && (_host.Coach.Tutorial.Phase == TutorialPhase.Brief
                || TutorialFeedbackReady && _host.Coach.Tutorial.Passed)
            || Guided != null && (Guided.Phase == TutorialPhase.Brief
                || Guided.Phase == TutorialPhase.Feedback && Guided.Passed);
        /// <summary>The guided hint names the live refusal, else the reason the last attempt failed.</summary>
        public TutorialFeedback GuidedNotice => Guided?.Notice ?? (Lessons.PreviousFeedback is { Success: false } ? Lessons.PreviousFeedback : null);

        public void OpenTutorials()
        {
            var selected = Guided?.Lesson.Id ?? (TutorialOn ? _host.Coach.Tutorial.Lesson.Id : null);
            Lessons.Guided.Exit();
            ReturnGuidedSettings();
            _host.Coach?.Stop();
            _host.ReleaseMatchSeats();
            Lessons.OpenMenu(_scene.Content, selected);
            _play.Phase = MatchDirector.Phase.Title; _scene.Cam.Play("title");
        }

        public void PrepareTutorial(string id)
        {
            var lesson = Lessons.Prepare(id);
            ReturnGuidedSettings();
            if (GuidedTutorialDirector.IsGuided(id)) { PrepareGuidedTutorial(lesson); return; }
            Lessons.Guided.Exit();
            _host.ReleaseMatchSeats();
            _choices.Mode = MatchDirector.PlayMode.Training; _choices.ParkId = Training.ParkId;
            _host.EnsureCoach();
            _host.Coach.BeginTutorial(_scene.Content, Lessons.Catalog, id, Lessons.Progress);
            _play.Match = _host.Coach.Tutorial.Match;
            _choices.HomeCaptain = _play.Match.Home.Captain.Id; _choices.AwayCaptain = _play.Match.Away.Captain.Id;
            _host.BuildScene();
            Lessons.LessonReady();
            _host.BeginSet();
        }

        void BeginTutorialAttempt()
        {
            var run = _host.Coach.Tutorial;
            run.Begin(); Lessons.AttemptBegun();
            Controls.CatchPlay();
            if (run.IsStealLesson && _play.Match.LivePlay.Active) { _host.StartRunnerPlay(); return; }
            if (!run.IsFieldLesson) return;
            _play.Pending = run.LastHit; _play.Preview = _play.Match.LivePlay.Preview;
            _play.Pitch = _play.Match.LivePlay.Pitch; _play.Swing = _play.Match.LivePlay.Swing;
            _live.CpuField = null;
            _scene.Park.Ball.Release();
            _host.StartFly(_play.Pending, alreadyLive: true);
        }

        public bool TickTutorialUi(float dt)
        {
            if (!Lessons.MenuOpen && !TutorialOn && Guided == null) return false;
            if (Guided != null && Guided.Phase == TutorialPhase.Attempt) return false;
            if (Guided != null && Guided.Phase == TutorialPhase.Feedback) Lessons.Save(Guided.Lesson, Guided.Successes);
            Lessons.Age(dt);
            if (TutorialOn && _host.Coach.Tutorial.Feedback?.Success == true) Lessons.Save(_host.Coach.Tutorial.Lesson, _host.Coach.Tutorial.Successes);
            // Save the completed attempt before replacing its session. Never consume its input
            // again in the fresh setup: returning true skips the rest of this frame's play tick.
            if (Guided != null && HowToPlay.TutorialRepeatsImmediately(Guided.Phase, Guided.Successes))
            {
                var feedback = Guided.Feedback;
                PrepareTutorial(Guided.Lesson.Id);
                Lessons.PreviousFeedback = feedback;
                BeginGuidedAttempt();
                return true;
            }
            if (TutorialFeedbackReady && HowToPlay.TutorialRepeatsImmediately(_host.Coach.Tutorial.Phase, _host.Coach.Tutorial.Successes))
            {
                var feedback = _host.Coach.Tutorial.Feedback;
                PrepareTutorial(_host.Coach.Tutorial.Lesson.Id);
                Lessons.PreviousFeedback = feedback;
                BeginTutorialAttempt();
                return true;
            }
            if (!Lessons.Modal(TutorialModal, out var settled))
            {
                if (!_host.Coach.Tutorial.IsFieldLesson && !_play.Match.LivePlay.Active && _host.Coach.Tutorial.Phase == TutorialPhase.Attempt)
                {
                    var left = (double)dt;
                    var runnerInput = _host.Coach.Tutorial.IsStealLesson ? _pads.RunInput() with { SouthDown = false, WestDown = false } : LivePadInput.Dead;
                    while (left > 0 && !_play.Match.LivePlay.Active) { var step = Math.Min(left, .05); _host.Coach.Tutorial.Tick(step, runnerInput); left -= step; }
                    if (_host.Coach.Tutorial.IsStealLesson && _play.Match.LivePlay.Active)
                    { _host.StartRunnerPlay(); return true; }
                }
                return false;
            }
            if (!settled) return true;
            var confirm = Controls.SouthDown;
            if (Lessons.MenuOpen)
            {
                switch (Lessons.TickMenu(dt, confirm, Controls.EastDown))
                {
                    case TutorialDirector.MenuStep.Choose: ChooseTutorialMenu(); break;
                    case TutorialDirector.MenuStep.Leave: _choices.Mode = MatchDirector.PlayMode.Exhibition; _play.T = 0; break;
                }
                return true;
            }
            if (Guided != null && Guided.Phase == TutorialPhase.Brief)
            {
                if (confirm) BeginGuidedAttempt();
                else if (Controls.EastDown) OpenTutorials();
                return true;
            }
            if (Guided != null && Guided.Phase == TutorialPhase.Feedback)
            {
                if (confirm) PrepareTutorial(Guided.Lesson.Id);
                else if (Controls.WestDown) PrepareTutorial(Lessons.NextAfter(Guided.Lesson.Id));
                else if (Controls.EastDown) OpenTutorials();
                return true;
            }
            if (_host.Coach.Tutorial.Phase == TutorialPhase.Brief)
            {
                if (confirm) BeginTutorialAttempt();
                else if (Controls.EastDown) OpenTutorials();
                return true;
            }
            if (TutorialFeedbackReady)
            {
                if (confirm) PrepareTutorial(_host.Coach.Tutorial.Lesson.Id);
                else if (Controls.WestDown) PrepareTutorial(Lessons.NextAfter(_host.Coach.Tutorial.Lesson.Id));
                else if (Controls.EastDown) OpenTutorials();
                return true;
            }
            return false;
        }

        void ChooseTutorialMenu()
        {
            if (Lessons.Chosen == null)
            {
                Lessons.CloseMenu(); _choices.PracticePick = PracticeLesson.Free; _host.BeginTraining();
            }
            else PrepareTutorial(Lessons.Chosen.Id);
        }

        public bool DrawTutorialUi()
        {
            if (!TutorialModal) return false;
            if (Guided != null) HudView.GuidedTutorial(Guided);
            else Lessons.Draw(TutorialOn ? _host.Coach.Tutorial : null);
            return true;
        }

        // The guided lessons' flow: the state is GuidedTutorialDirector's; building the match and the scene is the flow's.
        public bool GuidedAttempt(string id) => Lessons.Guided.Attempt(id);

        public void PrepareGuidedTutorial(TutorialLesson lesson)
        {
            var pick = Lessons.Guided.Start(lesson, Lessons.Profile, Lessons.Progress,
                new GuidedTutorialDirector.Pick(_choices.HomeCaptain, _choices.AwayCaptain, _choices.ParkId, _choices.Night, _choices.Pad1Home, _choices.Seed));
            _host.Coach?.Stop();
            _host.ReleaseMatchSeats();
            _choices.Mode = MatchDirector.PlayMode.Exhibition;
            _choices.VersusWanted = false;
            (_choices.HomeCaptain, _choices.AwayCaptain, _choices.ParkId, _choices.Night, _choices.Pad1Home, _choices.Seed) = (pick.Home, pick.Away, pick.Park, pick.Night, pick.Pad1Home, pick.Seed);
            Lessons.LessonReady();
            _play.Match = _host.NewMatch();
            _play.Phase = MatchDirector.Phase.Title;
            _scene.Cam.Play("title");
        }

        public void BeginGuidedAttempt()
        {
            if (!Lessons.Guided.CanBegin(Controls.PadCount)) return;
            _host.ClearLineup(); // Every lesson attempt needs a fresh roster, unlike Back during setup.
            if (Guided.Lesson.Id == "T-G07")
            {
                _choices.Settings = Lessons.Guided.LendSettings(_choices.Settings, _choices.Innings, _choices.Difficulty);
                _choices.FollowSettings();
            }
            Guided.Begin();
            Lessons.AttemptBegun();
            Controls.CatchPlay();
            if (!Lessons.Guided.StartsOnSet) { _host.OpenField(); return; }
            _host.BuildScene();
            _host.BeginSet();
        }

        void ReturnGuidedSettings()
        {
            if (Lessons.Guided.ReturnSettings(out var own, out var innings, out var difficulty))
                (_choices.Settings, _choices.Innings, _choices.Difficulty) = (own, innings, difficulty);
        }

        /// <summary>A guided observation opened the lesson's feedback: the card starts fresh and the match runs again.</summary>
        public void GuidedFeedbackOpened()
        {
            Lessons.FeedbackOpened();
            _play.Match.SetPaused(false);
        }

        public void GuidedObserve(string lesson, GuidedAction action)
        {
            if (Lessons.Guided.Observe(lesson, action)) GuidedFeedbackOpened();
        }

        /// <summary>Call time's Reset stick card closed (<see cref="PursuitSeatDirector"/>); a recalibrated close is T-G06-C's action.</summary>
        public void StickResetClosed(bool recalibrated)
        {
            _play.T = 0;
            if (recalibrated) GuidedObserve("T-G06-C", GuidedAction.StickRecalibrated);
        }

        public void GuidedSeatRecovered(LineupSeat seat)
        {
            if (Lessons.Guided.SeatRecovered(seat)) GuidedFeedbackOpened();
        }

        public void GuidedSeatsBound()
        {
            if (Lessons.Guided.SeatsBound(_host.SeatsBound, _pads.Live.BothHuman, Controls.SeatDeviceId(0), Controls.SeatDeviceId(1)))
                GuidedFeedbackOpened();
        }
    }

    /// <summary>What <see cref="TutorialFlow"/> asks of the flow: the coach, the scene, the seats and the screens a lesson hands off to.</summary>
    internal interface ITutorialFlowHost
    {
        TrainingDirector Coach { get; }
        /// <summary>Add the coach if the flow has none yet.</summary>
        void EnsureCoach();
        /// <summary>Build the match's park, specials, items and star meter, and forget the highlight.</summary>
        void BuildScene();
        Match NewMatch();
        void ReleaseMatchSeats();
        bool SeatsBound { get; }
        void ClearLineup();
        void BeginSet();
        void StartFly(AtBatResult hit, bool alreadyLive);
        /// <summary>A steal lesson's runner play went live before the pitch.</summary>
        void StartRunnerPlay();
        void OpenField();
        void BeginTraining();
    }
}
