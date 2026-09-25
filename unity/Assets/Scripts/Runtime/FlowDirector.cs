using System;
using System.Collections.Generic;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The flow between plays (a real director since #1042): which screen ticks, the result beat, game over and its
    /// replay, a practice session's start and end, and the step into the lineup. The screens are their own directors
    /// (<see cref="FrontMenus"/>, <see cref="LineupFlow"/>, <see cref="TutorialFlow"/>); the choices are
    /// <see cref="FlowChoices"/>; the coach, the scene build, the replay and the play's own directors are the host's
    /// (<see cref="IFlowHost"/>).
    /// </summary>
    internal sealed class FlowDirector
    {
        readonly MatchScene _scene;
        readonly PlayState _play;
        readonly FlowChoices _choices;
        readonly SeatPads _pads;
        readonly IFlowHost _host;

        public FlowDirector(MatchScene scene, PlayState play, FlowChoices choices, SeatPads pads, IFlowHost host)
        {
            _scene = scene; _play = play; _choices = choices; _pads = pads; _host = host;
        }

        public void Tick()
        {
            switch (_play.Phase)
            {
                case MatchDirector.Phase.Title: _host.Front.TickTitle(); break;
                case MatchDirector.Phase.Select: _host.Front.TickSelect(); break;
                case MatchDirector.Phase.Field: _host.Front.TickField(); break;
                case MatchDirector.Phase.Lineup: _host.Lineup.Tick(); break;
                case MatchDirector.Phase.Result: TickResult(); break;
                case MatchDirector.Phase.GameOver: TickGameOver(); break;
            }
        }

        void TickResult()
        {
            if (_host.TutorialOn && _host.Coach.Tutorial.Phase != TutorialPhase.Attempt) return;
            var hold = _play.Last != null
                ? (float)PlayStamp.HoldSeconds(_play.Last.Kind, _scene.Feel)
                : (float)_scene.Feel.AfterOutSeconds;
            if (_play.T <= hold) return;
            if (_pads.TrainingOn && _host.Coach.Session.Finished)
            {
                EndTraining();
                return;
            }
            if (_play.Match.Over)
            {
                if (_pads.TrainingOn)
                {
                    _choices.Seed++;
                    _play.Match = _host.Coach.MakeMatch(_scene.Content, _choices.Seed);
                    _host.BeginSet();
                    return;
                }
                _choices.Campaign?.Resolve(_play.Match);
                _host.BeginGameOver();
            }
            else _host.BeginSet();
        }

        void TickGameOver()
        {
            if (_host.Replaying)
            {
                _host.TickReplay(Time.deltaTime);
                if (_play.T > (float)_scene.Feel.ReplaySec || Controls.SouthDown)
                {
                    _host.Replaying = false;
                    _play.T = 0;
                    _scene.Cam.Play("replay");
                }
                return;
            }
            if (Controls.SouthDown && _play.T > 0.2f) ConfirmGameOver();
        }

        public void BeginTraining()
        {
            _choices.Mode = MatchDirector.PlayMode.Training;
            _choices.ParkId = Training.ParkId;
            _choices.HomeCaptain = ExhibitionPick.Default.Home;
            _choices.AwayCaptain = ExhibitionPick.Default.Away;
            _host.EnsureCoach();
            _host.Coach.Begin(_scene.Content, _choices.PracticePick);
            _play.Match = _host.Coach.MakeMatch(_scene.Content, _choices.Seed);
            _host.BuildMatchScene();
            _host.ForgetHighlight();
            _host.BannerText = _host.Coach.Session.Caption;
            _host.Sub = _host.Coach.Session.Verb;
            _host.BeginSet();
        }

        void EndTraining()
        {
            _host.Coach?.Stop();
            _host.ReleaseMatchSeats();
            _choices.Mode = MatchDirector.PlayMode.Training;
            _choices.Seed++;
            _play.Phase = MatchDirector.Phase.Title;
            _play.T = 0;
            _host.BannerText = _host.Sub = "";
            _host.Replaying = false;
            _scene.Audio?.CrowdBed(false);
            _scene.Cam.Play("replay");
        }

        void ConfirmGameOver()
        {
            _choices.Seed++;
            if (_choices.Campaign != null && !_choices.Campaign.AllBeaten(_scene.Content))
            {
                _play.Match = _choices.Campaign.MakeMatch(_scene.Content, _choices.Innings, _choices.Seed);
                _host.BuildMatchScene();
                _host.ForgetHighlight();
                OpenLineup();
                return;
            }
            _play.Match = _host.NewMatch();
            _host.BuildMatchScene();
            _host.ReleaseMatchSeats();
            _play.Phase = MatchDirector.Phase.Title;
            _play.T = 0;
            _host.Replaying = false;
            _scene.Audio?.CrowdBed(false);
            _scene.Cam.Play("replay");
        }

        public void OpenLineup()
        {
            if (_choices.Mode == MatchDirector.PlayMode.Exhibition) _host.Lineup.Open(_choices.HomeCaptain, _choices.AwayCaptain);
            else _host.Lineup.Screens = null;
            _play.Phase = MatchDirector.Phase.Lineup;
            _play.T = 0;
            _host.ForgetHighlight();
            _host.Replaying = false;
            _scene.Cam.Play("lineup");
        }
    }

    /// <summary>What <see cref="FlowDirector"/> asks of MatchDirector: the coach, the screens, the scene, the replay and SET.</summary>
    internal interface IFlowHost
    {
        TrainingDirector Coach { get; }
        /// <summary>Add the coach if the flow has none yet.</summary>
        void EnsureCoach();
        bool TutorialOn { get; }
        FrontMenus Front { get; }
        LineupFlow Lineup { get; }
        Match NewMatch();
        /// <summary>Build the match's park, specials, items and star meter.</summary>
        void BuildMatchScene();
        /// <summary>Forget the last game's highlight.</summary>
        void ForgetHighlight();
        bool Replaying { get; set; }
        void TickReplay(float dt);
        void BeginGameOver();
        void ReleaseMatchSeats();
        string BannerText { set; }
        string Sub { set; }
        void BeginSet();
    }
}
