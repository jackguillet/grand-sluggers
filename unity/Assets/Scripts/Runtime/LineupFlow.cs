using System.Collections.Generic;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The lineup screens' flow (its own class since #1042): team setup, defense setup and match settings for an
    /// exhibition, each seat on its own pad. The screens' state and rules are the sim's (<see cref="LineupScreens"/>,
    /// <see cref="ExhibitionSettings"/>); this class turns the pads into their verbs, tells the guided lessons what the
    /// player did, and builds the match the draft confirms from <see cref="FlowChoices"/>. The lessons and what comes
    /// before and after the screens are the flow's (<see cref="ILineupHost"/>).
    /// </summary>
    internal sealed class LineupFlow
    {
        readonly PlayState _play;
        readonly MatchScene _scene;
        readonly FlowChoices _choices;
        readonly SeatPads _pads;
        readonly ILineupHost _host;
        MenuNav.Gate _x, _x2, _y, _y2;

        public LineupFlow(PlayState play, MatchScene scene, FlowChoices choices, SeatPads pads, ILineupHost host)
        {
            _play = play; _scene = scene; _choices = choices; _pads = pads; _host = host;
        }

        /// <summary>The exhibition's screens, or null outside an exhibition (the lineup then auto-starts).</summary>
        public LineupScreens Screens { get; set; }

        GuidedTutorialDirector Guided => _host.Lessons.Guided;

        /// <summary>
        /// An exhibition's screens for these captains, seated as the pads sit: kept (back on team setup) when the captains
        /// are the same, opened fresh when they change.
        /// </summary>
        public void Open(string homeCaptain, string awayCaptain)
        {
            var seats = _pads.Live;
            if (Screens == null || Screens.HomeCaptain.Id != homeCaptain || Screens.AwayCaptain.Id != awayCaptain)
                Screens = LineupScreens.Open(_scene.Content, homeCaptain, awayCaptain, seats.Home, seats.Away);
            else
            {
                Screens.Sit(seats.Home, seats.Away);
                if (Screens.Step == LineupStep.MatchSettings) Screens.BackToDefense();
                if (Screens.Step == LineupStep.DefenseSetup) Screens.BackToTeam();
            }
            _x.Catch(Controls.Pad1.MenuAxisX);
            _x2.Catch(Controls.Pad2.MenuAxisX);
            _y.Catch(Controls.Pad1.MenuAxisY);
            _y2.Catch(Controls.Pad2.MenuAxisY);
        }

        public void Tick()
        {
            if (Screens == null)
            {
                if (Controls.SouthDown || _play.T > (float)_scene.Feel.LineupAutoStartSec) _host.BeginSet();
                return;
            }

            SyncSeats();
            if (Screens.Step == LineupStep.MatchSettings) { TickMatchSettings(); return; }
            TickPad(Controls.Pad1, LineupSeat.Pad1, ref _x, ref _y);
            if (_play.Phase != MatchDirector.Phase.Lineup || Screens.Step == LineupStep.MatchSettings) return;
            if (Screens.HomeSeat == LineupSeat.Pad2 || Screens.AwaySeat == LineupSeat.Pad2)
                TickPad(Controls.Pad2, LineupSeat.Pad2, ref _x2, ref _y2);
        }

        void SyncSeats()
        {
            if (Screens == null) return;
            var seats = _pads.Live;
            if (Screens.HomeSeat != seats.Home || Screens.AwaySeat != seats.Away)
                Screens.Sit(seats.Home, seats.Away);
        }

        void Pick(LineupSeat seat)
        {
            var focus = Screens.FocusOf(seat);
            if (!Screens.PickOrSwap(seat) || seat != LineupSeat.Pad1) return;
            _host.GuidedObserve("T-G01", focus is LineupFocus.HomeOrder or LineupFocus.AwayOrder
                ? GuidedAction.BattingOrderChanged : GuidedAction.GlovePositionChanged);
        }

        void Drop(LineupSeat seat)
        {
            var pool = Screens.Pool;
            var who = pool.Count == 0 ? null : pool[Mathf.Clamp(Screens.PoolOf(seat), 0, pool.Count - 1)];
            var dropped = Screens.South(seat);
            if (Guided.LineupDrop(seat, who, dropped && Screens.Step == LineupStep.TeamSetup)) _host.GuidedFeedbackOpened();
        }

        void TickPad(Controls.Pad pad, LineupSeat seat, ref MenuNav.Gate armedX, ref MenuNav.Gate armedY)
        {
            TickStick(pad, seat, ref armedX, ref armedY);
            if (pad.PageNext && Screens.Step == LineupStep.TeamSetup)
                Screens.RandomFill(seat);
            if (pad.WestDown && Screens.Step == LineupStep.TeamSetup
                && Screens.FocusOf(seat) != LineupFocus.Pool)
                Screens.Remove(seat);
            if (pad.EastDown)
            {
                if (Screens.Step == LineupStep.TeamSetup) { if (seat == LineupSeat.Pad1) _host.OpenSelect(); }
                else Screens.West(seat); // cancel pick, withdraw ready, then back; never change panels
                return;
            }
            if ((pad.PagePrevious || pad.PageNext) && Screens.Step == LineupStep.DefenseSetup)
                Screens.ToggleArea(seat);
            if (pad.NorthDown && Screens.CanPlay)
            {
                Ready(seat);
                return;
            }
            if (pad.SouthDown)
            {
                if (Screens.Step == LineupStep.TeamSetup) Drop(seat);
                else Pick(seat);
            }
        }

        void TickStick(Controls.Pad pad, LineupSeat seat, ref MenuNav.Gate armedX, ref MenuNav.Gate armedY)
        {
            var dt = Time.unscaledDeltaTime;
            var dx = armedX.Tick(pad.MenuAxisX, pad.MenuTapX, dt);
            var dy = armedY.Tick(pad.MenuAxisY, pad.MenuTapY, dt);
            if (dx == 0 && dy == 0) return;
            if (dx != 0 && Mathf.Abs(pad.MenuAxisX) >= Mathf.Abs(pad.MenuAxisY)) dy = 0;
            else if (dy != 0) dx = 0;
            Screens.Stick(seat, dx, dy);
        }

        void Ready(LineupSeat seat)
        {
            if (Screens.ToggleReady(seat)) ReadyChanged(seat);
            if (!Screens.BothReady) return;
            if (Screens.Step == LineupStep.DefenseSetup)
            {
                Screens.OpenSettings();
                _x.Catch(Controls.Pad1.MenuAxisX);
                _y.Catch(Controls.Pad1.MenuAxisY);
            }
            else ConfirmDraft();
        }

        void TickMatchSettings()
        {
            var pad = Controls.Pad1;
            var settings = _choices.Settings;
            var dy = _y.Tick(pad.MenuAxisY, pad.MenuTapY, Time.unscaledDeltaTime);
            var dx = _x.Tick(pad.MenuAxisX, pad.MenuTapX, Time.unscaledDeltaTime);
            if (dy != 0) settings.Move(dy > 0 ? -1 : 1);
            if (dx != 0 || pad.SouthDown)
            {
                var direction = dx == 0 ? 1 : dx;
                var refusal = settings.Refusal(LineupSeat.Pad1, direction);
                var wasReady = HumanReady(LineupSeat.Pad1) || HumanReady(LineupSeat.Pad2);
                var changed = settings.Change(LineupSeat.Pad1, direction);
                if (changed) Screens.ResetReady();
                Guided.RuleEdit(settings.Selected, LineupSeat.Pad1, refusal, changed && wasReady);
                _choices.FollowSettings();
            }
            if (pad.EastDown)
            {
                Screens.West(LineupSeat.Pad1);
                ReadyChanged(LineupSeat.Pad1);
                return;
            }
            if (pad.NorthDown) Ready(LineupSeat.Pad1);
            if (_play.Phase != MatchDirector.Phase.Lineup || Screens.Step != LineupStep.MatchSettings) return;
            if (Screens.HomeSeat == LineupSeat.Pad2 || Screens.AwaySeat == LineupSeat.Pad2)
            {
                if (Controls.Pad2.EastDown) { Screens.West(LineupSeat.Pad2); ReadyChanged(LineupSeat.Pad2); }
                else if (Controls.Pad2.NorthDown) Ready(LineupSeat.Pad2);
            }
        }

        bool HumanReady(LineupSeat seat) =>
            (Screens.HomeSeat == seat || Screens.AwaySeat == seat) && seat != LineupSeat.Cpu && Screens.IsReady(seat);

        void ReadyChanged(LineupSeat seat)
        {
            var onSettings = Screens != null && Screens.Step == LineupStep.MatchSettings;
            Guided.ReadyChanged(seat, onSettings, onSettings && Screens.IsReady(seat));
        }

        void SettingsStart()
        {
            var onSettings = Screens != null && Screens.Step == LineupStep.MatchSettings;
            var humans = new List<LineupSeat>();
            if (onSettings && Screens.HomeSeat != LineupSeat.Cpu) humans.Add(Screens.HomeSeat);
            if (onSettings && Screens.AwaySeat != LineupSeat.Cpu) humans.Add(Screens.AwaySeat);
            if (Guided.SettingsStart(onSettings, humans)) _host.Lessons.FeedbackOpened();
        }

        void ConfirmDraft()
        {
            if (Screens != null)
            {
                if (Screens.Step == LineupStep.TeamSetup)
                {
                    Screens.RandomFill();
                    Screens.ConfirmTeam();
                }
                SettingsStart();
                if (Screens.Home != null)
                {
                    var match = _play.Match;
                    var homeBat = match.HomeBat;
                    var homeGlove = match.HomeGlove;
                    var awayBat = match.AwayBat;
                    var awayGlove = match.AwayGlove;
                    var away = Screens.Away != null
                        ? Screens.Away.ToTeam()
                        : PresetTeams.ForCaptain(_scene.Content, _choices.AwayCaptain);
                    var c = _choices;
                    _play.Match = Match.Exhibition(_scene.Content, Screens.Home.ToTeam(), away, c.Innings, c.Seed, c.ParkId, c.Night,
                        c.Difficulty, c.Hazards, mercy: c.Settings.Mercy, stars: c.Settings.Stars);
                    RestoreGear(homeBat, homeGlove, awayBat, awayGlove);
                }
            }
            _host.BeginSet();
        }

        void RestoreGear(BatItem homeBat, GloveItem homeGlove, BatItem awayBat, GloveItem awayGlove)
        {
            var match = _play.Match;
            for (var i = 0; i < 12 && match.HomeBat.Id != homeBat.Id; i++) match.CycleBat(true);
            for (var i = 0; i < 12 && match.HomeGlove.Id != homeGlove.Id; i++) match.CycleGlove(true);
            for (var i = 0; i < 12 && match.AwayBat.Id != awayBat.Id; i++) match.CycleBat(false);
            for (var i = 0; i < 12 && match.AwayGlove.Id != awayGlove.Id; i++) match.CycleGlove(false);
        }
    }

    /// <summary>What <see cref="LineupFlow"/> asks of the flow: the lessons and the screens on either side.</summary>
    internal interface ILineupHost
    {
        TutorialDirector Lessons { get; }
        void GuidedObserve(string lesson, GuidedAction action);
        /// <summary>A guided observation opened the lesson's feedback: the card starts fresh and the match runs again.</summary>
        void GuidedFeedbackOpened();
        void OpenSelect();
        void BeginSet();
    }
}
