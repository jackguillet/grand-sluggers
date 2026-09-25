using System;
using System.Collections.Generic;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>Title, select, lineup. Play does not know draft.</summary>
    public sealed class FlowDirector
    {
        readonly MatchDirector _play;
        public FlowDirector(MatchDirector play) { _play = play; }
        public void Tick() { _play.TickFlow(); }
    }

    public sealed partial class MatchDirector : IStillHost, ISeatHost, IAtBatHost, IInPlayHost, IActorHost, IItemHost, IDefenseSwapHost, IRunnerPlayHost
    {
        internal void TickFlow()
        {
            switch (_phase)
            {
                case Phase.Title: TickTitle(); break;
                case Phase.Select: TickSelect(); break;
                case Phase.Field: TickField(); break;
                case Phase.Lineup: TickLineup(); break;
                case Phase.Result: TickResult(); break;
                case Phase.GameOver: TickGameOver(); break;
            }
        }

        void TickResult()
        {
            if (TutorialOn && _coach.Tutorial.Phase != TutorialPhase.Attempt) return;
            var hold = _last != null
                ? (float)PlayStamp.HoldSeconds(_last.Kind, _feel)
                : (float)_feel.AfterOutSeconds;
            if (_t <= hold) return;
            if (TrainingOn && _coach.Session.Finished)
            {
                EndTraining();
                return;
            }
            if (_match.Over)
            {
                if (TrainingOn)
                {
                    Seed++;
                    _match = _coach.MakeMatch(_content, Seed);
                    BeginSet();
                    return;
                }
                _campaign?.Resolve(_match);
                BeginGameOver();
            }
            else BeginSet();
        }

        void TickGameOver()
        {
            if (_replaying)
            {
                TickReplay(Time.deltaTime);
                if (_t > (float)_feel.ReplaySec || Controls.SouthDown)
                {
                    _replaying = false;
                    _t = 0;
                    _cam.Play("replay");
                }
                return;
            }
            if (Controls.SouthDown && _t > 0.2f) ConfirmGameOver();
        }

        internal int _titleFocus, _fieldFocus;
        internal CaptainSelection _captains;
        void TickTitle()
        {
            var dy = _selectY.Tick(Controls.MenuY, Controls.MenuTapY, Time.unscaledDeltaTime);
            if (dy != 0) _titleFocus = (_titleFocus + (dy > 0 ? 3 : 1)) % 4;
            _cam.Cut("title");
            if (!Controls.SouthDown || _t <= .15f) return;
            if (_titleFocus == 1) { OpenTutorials(); return; }
            if (_titleFocus == 2) { OpenControlsBook(); return; }
            if (_titleFocus == 3) { Application.Quit(); return; }
            _mode = PlayMode.Exhibition;
            _match = NewMatch();
            _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
            _spec.Build(transform, DiamondGeometry.Of(_match.Rules)); _items.Build(transform); _stars?.Build(transform);
            OpenField();
        }

        internal void OpenControlsBook()
        {
            _pausePad = Controls.Pad1;
            _match.SetPaused(true);
            _pauseHowTo = _pauseFromHowTo = true;
            _pausePage = 0; _t = 0;
            Controls.CatchPlay();
        }

        void RebuildTitlePark()
        {
            if (_park == null || _content == null) return;
            if (!_content.Parks.TryGetValue(ParkId, out var park)) return;
            // The park as this exhibition will play it: tonight's instances, and none with hazards off (FD-10).
            var hazards = Hazards || _mode != PlayMode.Exhibition;
            _park.Build(PlayedPark.Of(park, Night, hazards, _content.Rules.Hazards), Night, _content.Rules, _content.Feel);
            if (_phase == Phase.Title)
                _cam?.Cut("title");
        }

        void OpenSelect()
        {
            ReleaseMatchSeats();
            _match = NewMatch();
            _phase = Phase.Select;
            _captains = new CaptainSelection(_content, CurrentPick(), _versusWanted);
            _t = 0;
            _selectX.Catch(Controls.Pad1.MenuAxisX);
            _selectY.Catch(Controls.Pad1.MenuAxisY);
            _selectX2.Catch(Controls.Pad2.MenuAxisX);
            _clip = null;
            _hlPath = null;
            _replaying = false;
            _cam.Cut("select");
        }

        void TickSelect()
        {
            if (_captains.Versus && !Controls.Pad2.Present)
                Controls.TryRecoverMatchSeat(LineupSeat.Pad2);
            var p1 = Controls.Pad1;
            var p2 = Controls.Pad2;
            var dt = Time.unscaledDeltaTime;
            var dx = _selectX.Tick(p1.MenuAxisX, p1.MenuTapX, dt);
            var dx2 = _selectX2.Tick(p2.MenuAxisX, p2.MenuTapX, dt);
            _captains.Move(_captains.ActiveOne, dx);
            if (_captains.Versus && p2.Present) _captains.Move(1, dx2);
            if (_t <= .15f) return;
            if (p1.EastDown)
            {
                if (_captains.Back(0)) OpenField();
                return;
            }
            if (_captains.Versus && p2.EastDown) _captains.Back(1);
            if (p1.SouthDown) _captains.Confirm(_captains.ActiveOne);
            if (_captains.Versus && p2.Present && p2.SouthDown) _captains.Confirm(1);
            if (!_captains.Complete || (_captains.Versus && !p2.Present)) return;
            ApplyPick(_captains.ApplyTo(CurrentPick()));
            BindMatchSeats();
            GuidedSeatsBound();
            if (_guided?.Phase != TutorialPhase.Feedback) OpenLineup();
        }

        void WantVersus(bool versus)
        {
            if (_versusWanted == versus) return;
            _versusWanted = versus;
            if (versus)
                _selectX2.Catch(Controls.Pad2.MenuAxisX);
        }

        void OpenField()
        {
            ReleaseMatchSeats();
            _phase = Phase.Field;
            _fieldFocus = 0;
            _selectY.Catch(Controls.MenuY);
            _t = 0;
            _selectX.Catch(Controls.MenuX);
            _clip = null;
            _hlPath = null;
            _replaying = false;
            _match = NewMatch();
            RebuildTitlePark();
            _cam.Play("field");
        }

        /// <summary>The continent map on the stadium row (WD-17 A).</summary>
        readonly MapPicker _map = new MapPicker();

        void TickField()
        {
            var dy = _selectY.Tick(Controls.MenuY, Controls.MenuTapY, Time.unscaledDeltaTime);
            var dx = _selectX.Tick(Controls.MenuX, Controls.MenuTapX, Time.unscaledDeltaTime);
            if (_map.IsOpen) { TickMap(dx, dy); return; }
            if (dy != 0) _fieldFocus = (_fieldFocus + (dy > 0 ? 5 : 1)) % 6;
            if (_t <= .15f) return;
            if (Controls.EastDown) { OpenTitle(); return; }
            // The stadium row opens the map: a change on it (left, right or South) is a pick on the map.
            if (_fieldFocus == 0 && (dx != 0 || Controls.SouthDown)) { _map.Open(ParkId); _t = 0; return; }
            if (dx != 0 || Controls.SouthDown)
            {
                if (_fieldFocus == 1) Night = !Night;
                if (_fieldFocus is 0 or 1) GuidedObserve("T-G07", GuidedAction.StadiumChosen);
                if (_fieldFocus == 2) Hazards = !Hazards;
                if (_fieldFocus == 3) WantVersus(!_versusWanted);
                if (_fieldFocus == 4) ApplyPick(ExhibitionPick.ToggleSeat(CurrentPick()));
                if (_fieldFocus == 5 && Controls.SouthDown) { OpenSelect(); return; }
                RebuildTitlePark();
            }
            _cam.Play("field");
        }

        /// <summary>The map is up: the stick moves the cursor and the postcard follows; South plays the park, East keeps yours.</summary>
        void TickMap(int dx, int dy)
        {
            if (_map.Move(_content.World, dx, dy) is { } park) { ApplyPick(CurrentPick() with { Park = park }); RebuildTitlePark(); }
            if (_t > .15f && Controls.SouthDown)
            {
                ApplyPick(CurrentPick() with { Park = _map.Confirm() });
                GuidedObserve("T-G07", GuidedAction.StadiumChosen);
                RebuildTitlePark();
            }
            else if (_t > .15f && Controls.EastDown)
            {
                ApplyPick(CurrentPick() with { Park = _map.Cancel() });
                RebuildTitlePark();
            }
            _cam.Play("field");
        }

        /// <summary>The stadium screen: the map while it is up, else the postcard's HUD and the setup rows.</summary>
        void DrawField()
        {
            if (_map.IsOpen) { SetupSheet.Map(_content, _map.Park, Night, Hazards); return; }
            HudView.Field(ParkId, ParkDisplayName(ParkId), Night, Hazards, FieldHazardsLine(), FieldCardLines());
            SetupSheet.FieldFocus(_fieldFocus, ParkDisplayName(ParkId), Night, Hazards, _versusWanted, Pad1Home);
        }

        ExhibitionPick CurrentPick() => new(HomeCaptain, AwayCaptain, ParkId, Pad1Home);

        void ApplyPick(ExhibitionPick pick)
        {
            HomeCaptain = pick.Home;
            AwayCaptain = pick.Away;
            ParkId = pick.Park;
            Pad1Home = pick.Pad1Home;
            _match = NewMatch();
        }

        internal void OpenTitle()
        {
            if (_guided != null)
            {
                OpenTutorials();
                return;
            }
            ReleaseMatchSeats();
            _phase = Phase.Title;
            _t = 0;
            _clip = null;
            _hlPath = null;
            _replaying = false;
            RebuildTitlePark();
            _cam.Cut("title");
        }

        void BeginTraining()
        {
            _mode = PlayMode.Training;
            ParkId = Training.ParkId;
            HomeCaptain = ExhibitionPick.Default.Home;
            AwayCaptain = ExhibitionPick.Default.Away;
            if (_coach == null) _coach = gameObject.AddComponent<TrainingDirector>();
            _coach.Begin(_content, PracticePick);
            _match = _coach.MakeMatch(_content, Seed);
            _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
            _spec.Build(transform, DiamondGeometry.Of(_match.Rules));
            _items.Build(transform);
            _stars?.Build(transform);
            _clip = null;
            _hlPath = null;
            _banner = _coach.Session.Caption;
            _sub = _coach.Session.Verb;
            BeginSet();
        }

        void EndTraining()
        {
            _coach?.Stop();
            ReleaseMatchSeats();
            _mode = PlayMode.Training;
            Seed++;
            _phase = Phase.Title;
            _t = 0;
            _banner = _sub = "";
            _replaying = false;
            _audio?.CrowdBed(false);
            _cam.Play("replay");
        }

        void ConfirmGameOver()
        {
            Seed++;
            if (_campaign != null && !_campaign.AllBeaten(_content))
            {
                _match = _campaign.MakeMatch(_content, Innings, Seed);
                _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
                _spec.Build(transform, DiamondGeometry.Of(_match.Rules));
                _items.Build(transform);
                _stars?.Build(transform);
                _clip = null;
                _hlPath = null;
                OpenLineup();
                return;
            }
            _match = NewMatch();
            _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
            _spec.Build(transform, DiamondGeometry.Of(_match.Rules));
            _items.Build(transform);
            _stars?.Build(transform);
            ReleaseMatchSeats();
            _phase = Phase.Title;
            _t = 0;
            _replaying = false;
            _audio?.CrowdBed(false);
            _cam.Play("replay");
        }

        internal void OpenLineup()
        {
            if (_mode == PlayMode.Exhibition)
            {
                var seats = LiveSeats;
                if (_lineup == null || _lineup.HomeCaptain.Id != HomeCaptain || _lineup.AwayCaptain.Id != AwayCaptain)
                    _lineup = LineupScreens.Open(_content, HomeCaptain, AwayCaptain, seats.Home, seats.Away);
                else
                {
                    _lineup.Sit(seats.Home, seats.Away);
                    if (_lineup.Step == LineupStep.MatchSettings) _lineup.BackToDefense();
                    if (_lineup.Step == LineupStep.DefenseSetup) _lineup.BackToTeam();
                }
                _lineupX.Catch(Controls.Pad1.MenuAxisX);
                _lineupX2.Catch(Controls.Pad2.MenuAxisX);
                _lineupY.Catch(Controls.Pad1.MenuAxisY);
                _lineupY2.Catch(Controls.Pad2.MenuAxisY);
            }
            else
                _lineup = null;
            _phase = Phase.Lineup;
            _t = 0;
            _clip = null;
            _hlPath = null;
            _replaying = false;
            _cam.Play("lineup");
        }

        internal void TickLineup()
        {
            if (_lineup == null)
            {
                if (Controls.SouthDown || _t > (float)_feel.LineupAutoStartSec) BeginSet();
                return;
            }

            SyncLineupSeats();
            if (_lineup.Step == LineupStep.MatchSettings) { TickMatchSettings(); return; }
            TickLineupPad(Controls.Pad1, LineupSeat.Pad1, ref _lineupX, ref _lineupY);
            if (_phase != Phase.Lineup || _lineup.Step == LineupStep.MatchSettings) return;
            if (_lineup.HomeSeat == LineupSeat.Pad2 || _lineup.AwaySeat == LineupSeat.Pad2)
                TickLineupPad(Controls.Pad2, LineupSeat.Pad2, ref _lineupX2, ref _lineupY2);
        }

        void SyncLineupSeats()
        {
            if (_lineup == null) return;
            var seats = LiveSeats;
            if (_lineup.HomeSeat != seats.Home || _lineup.AwaySeat != seats.Away)
                _lineup.Sit(seats.Home, seats.Away);
        }

        void PickLineup(LineupSeat seat)
        {
            var focus = _lineup.FocusOf(seat);
            if (!_lineup.PickOrSwap(seat) || seat != LineupSeat.Pad1) return;
            GuidedObserve("T-G01", focus is LineupFocus.HomeOrder or LineupFocus.AwayOrder
                ? GuidedAction.BattingOrderChanged : GuidedAction.GlovePositionChanged);
        }

        void DropLineup(LineupSeat seat)
        {
            var pool = _lineup.Pool;
            var who = pool.Count == 0 ? null : pool[Mathf.Clamp(_lineup.PoolOf(seat), 0, pool.Count - 1)];
            var dropped = _lineup.South(seat);
            if (_lessons.Guided.LineupDrop(seat, who, dropped && _lineup.Step == LineupStep.TeamSetup)) GuidedFeedbackOpened();
        }

        void TickLineupPad(Controls.Pad pad, LineupSeat seat, ref MenuNav.Gate armedX, ref MenuNav.Gate armedY)
        {
            TickLineupStick(pad, seat, ref armedX, ref armedY);
            if (pad.PageNext && _lineup.Step == LineupStep.TeamSetup)
                _lineup.RandomFill(seat);
            if (pad.WestDown && _lineup.Step == LineupStep.TeamSetup
                && _lineup.FocusOf(seat) != LineupFocus.Pool)
                _lineup.Remove(seat);
            if (pad.EastDown)
            {
                if (_lineup.Step == LineupStep.TeamSetup) { if (seat == LineupSeat.Pad1) OpenSelect(); }
                else _lineup.West(seat); // cancel pick, withdraw ready, then back; never change panels
                return;
            }
            if ((pad.PagePrevious || pad.PageNext) && _lineup.Step == LineupStep.DefenseSetup)
                _lineup.ToggleArea(seat);
            if (pad.NorthDown && _lineup.CanPlay)
            {
                ReadyLineup(seat);
                return;
            }
            if (pad.SouthDown)
            {
                if (_lineup.Step == LineupStep.TeamSetup) DropLineup(seat);
                else PickLineup(seat);
            }
        }

        void TickLineupStick(Controls.Pad pad, LineupSeat seat, ref MenuNav.Gate armedX, ref MenuNav.Gate armedY)
        {
            var dt = Time.unscaledDeltaTime;
            var dx = armedX.Tick(pad.MenuAxisX, pad.MenuTapX, dt);
            var dy = armedY.Tick(pad.MenuAxisY, pad.MenuTapY, dt);
            if (dx == 0 && dy == 0) return;
            if (dx != 0 && Mathf.Abs(pad.MenuAxisX) >= Mathf.Abs(pad.MenuAxisY)) dy = 0;
            else if (dy != 0) dx = 0;
            _lineup.Stick(seat, dx, dy);
        }

        void ReadyLineup(LineupSeat seat)
        {
            if (_lineup.ToggleReady(seat)) GuidedReadyChanged(seat);
            if (!_lineup.BothReady) return;
            if (_lineup.Step == LineupStep.DefenseSetup)
            {
                _lineup.OpenSettings();
                _lineupX.Catch(Controls.Pad1.MenuAxisX);
                _lineupY.Catch(Controls.Pad1.MenuAxisY);
            }
            else ConfirmDraft();
        }

        void TickMatchSettings()
        {
            var pad = Controls.Pad1;
            var dy = _lineupY.Tick(pad.MenuAxisY, pad.MenuTapY, Time.unscaledDeltaTime);
            var dx = _lineupX.Tick(pad.MenuAxisX, pad.MenuTapX, Time.unscaledDeltaTime);
            if (dy != 0) _settings.Move(dy > 0 ? -1 : 1);
            if (dx != 0 || pad.SouthDown)
            {
                var direction = dx == 0 ? 1 : dx;
                var refusal = _settings.Refusal(LineupSeat.Pad1, direction);
                var wasReady = HumanReady(LineupSeat.Pad1) || HumanReady(LineupSeat.Pad2);
                var changed = _settings.Change(LineupSeat.Pad1, direction);
                if (changed) _lineup.ResetReady();
                _lessons.Guided.RuleEdit(_settings.Selected, LineupSeat.Pad1, refusal, changed && wasReady);
                Innings = _settings.Innings;
                Difficulty = _settings.Difficulty;
            }
            if (pad.EastDown)
            {
                _lineup.West(LineupSeat.Pad1);
                GuidedReadyChanged(LineupSeat.Pad1);
                return;
            }
            if (pad.NorthDown) ReadyLineup(LineupSeat.Pad1);
            if (_phase != Phase.Lineup || _lineup.Step != LineupStep.MatchSettings) return;
            if (_lineup.HomeSeat == LineupSeat.Pad2 || _lineup.AwaySeat == LineupSeat.Pad2)
            {
                if (Controls.Pad2.EastDown) { _lineup.West(LineupSeat.Pad2); GuidedReadyChanged(LineupSeat.Pad2); }
                else if (Controls.Pad2.NorthDown) ReadyLineup(LineupSeat.Pad2);
            }
        }

        bool HumanReady(LineupSeat seat) =>
            (_lineup.HomeSeat == seat || _lineup.AwaySeat == seat) && seat != LineupSeat.Cpu && _lineup.IsReady(seat);

        void ConfirmDraft()
        {
            if (_lineup != null)
            {
                if (_lineup.Step == LineupStep.TeamSetup)
                {
                    _lineup.RandomFill();
                    _lineup.ConfirmTeam();
                }
                GuidedSettingsStart();
                if (_lineup.Home != null)
                {
                    var homeBat = _match.HomeBat;
                    var homeGlove = _match.HomeGlove;
                    var awayBat = _match.AwayBat;
                    var awayGlove = _match.AwayGlove;
                    var away = _lineup.Away != null
                        ? _lineup.Away.ToTeam()
                        : PresetTeams.ForCaptain(_content, AwayCaptain);
                    _match = Match.Exhibition(_content, _lineup.Home.ToTeam(), away, Innings, Seed, ParkId, Night, Difficulty, Hazards, mercy: _settings.Mercy, stars: _settings.Stars);
                    RestoreGear(homeBat, homeGlove, awayBat, awayGlove);
                }
            }
            BeginSet();
        }

        void RestoreGear(BatItem homeBat, GloveItem homeGlove, BatItem awayBat, GloveItem awayGlove)
        {
            for (var i = 0; i < 12 && _match.HomeBat.Id != homeBat.Id; i++) _match.CycleBat(true);
            for (var i = 0; i < 12 && _match.HomeGlove.Id != homeGlove.Id; i++) _match.CycleGlove(true);
            for (var i = 0; i < 12 && _match.AwayBat.Id != awayBat.Id; i++) _match.CycleBat(false);
            for (var i = 0; i < 12 && _match.AwayGlove.Id != awayGlove.Id; i++) _match.CycleGlove(false);
        }

        // The Tutorials section's flow. The menu, progress and card state are TutorialDirector's; building a lesson's match
        // and scene, and routing the card's pad, are the flow's.
        readonly TutorialDirector _lessons = new TutorialDirector();
        GuidedTutorialSession _guided => _lessons.Guided.Session;
        bool TutorialOn => _coach != null && _coach.Tutorial != null;
        bool TutorialFeedbackReady => TutorialOn && _coach.Tutorial.Phase == TutorialPhase.Feedback
            && (_coach.Tutorial.IsFieldLesson || !_coach.PlayerBats || _phase == Phase.Result || _coach.Tutorial.Feedback.Code == "timeout");
        bool TutorialModal => _lessons.MenuOpen || TutorialOn && (_coach.Tutorial.Phase == TutorialPhase.Brief
                || TutorialFeedbackReady && _coach.Tutorial.Passed)
            || _guided != null && (_guided.Phase == TutorialPhase.Brief
                || _guided.Phase == TutorialPhase.Feedback && _guided.Passed);
        /// <summary>The guided hint names the live refusal, else the reason the last attempt failed.</summary>
        TutorialFeedback GuidedNotice => _guided?.Notice ?? (_lessons.PreviousFeedback is { Success: false } ? _lessons.PreviousFeedback : null);

        void OpenTutorials()
        {
            var selected = _guided?.Lesson.Id ?? (TutorialOn ? _coach.Tutorial.Lesson.Id : null);
            _lessons.Guided.Exit();
            ReturnGuidedSettings();
            _coach?.Stop();
            ReleaseMatchSeats();
            _lessons.OpenMenu(_content, selected);
            _phase = Phase.Title; _cam.Play("title");
        }

        void PrepareTutorial(string id)
        {
            var lesson = _lessons.Prepare(id);
            ReturnGuidedSettings();
            if (GuidedTutorialDirector.IsGuided(id)) { PrepareGuidedTutorial(lesson); return; }
            _lessons.Guided.Exit();
            ReleaseMatchSeats();
            _mode = PlayMode.Training; ParkId = Training.ParkId;
            if (_coach == null) _coach = gameObject.AddComponent<TrainingDirector>();
            _coach.BeginTutorial(_content, _lessons.Catalog, id, _lessons.Progress);
            _match = _coach.Tutorial.Match;
            HomeCaptain = _match.Home.Captain.Id; AwayCaptain = _match.Away.Captain.Id;
            _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
            _spec.Build(transform, DiamondGeometry.Of(_match.Rules)); _items.Build(transform); _stars?.Build(transform);
            _clip = null; _hlPath = null;
            _lessons.LessonReady();
            BeginSet();
        }

        void BeginTutorialAttempt()
        {
            var run = _coach.Tutorial;
            run.Begin(); _lessons.AttemptBegun();
            Controls.CatchPlay();
            if (run.IsStealLesson && _match.LivePlay.Active) { StartRunnerPlay(null); return; }
            if (!run.IsFieldLesson) return;
            _pending = run.LastHit; _preview = _match.LivePlay.Preview;
            _pitch = _match.LivePlay.Pitch; _swing = _match.LivePlay.Swing;
            _cpuField = null;
            _park.Ball.Release();
            StartFly(_pending, alreadyLive: true);
        }

        bool TickTutorialUi(float dt)
        {
            if (!_lessons.MenuOpen && !TutorialOn && _guided == null) return false;
            if (_guided != null && _guided.Phase == TutorialPhase.Attempt) return false;
            if (_guided != null && _guided.Phase == TutorialPhase.Feedback) _lessons.Save(_guided.Lesson, _guided.Successes);
            _lessons.Age(dt);
            if (TutorialOn && _coach.Tutorial.Feedback?.Success == true) _lessons.Save(_coach.Tutorial.Lesson, _coach.Tutorial.Successes);
            // Save the completed attempt before replacing its session. Never consume its input
            // again in the fresh setup: returning true skips the rest of this frame's play tick.
            if (_guided != null && HowToPlay.TutorialRepeatsImmediately(_guided.Phase, _guided.Successes))
            {
                var feedback = _guided.Feedback;
                PrepareTutorial(_guided.Lesson.Id);
                _lessons.PreviousFeedback = feedback;
                BeginGuidedAttempt();
                return true;
            }
            if (TutorialFeedbackReady && HowToPlay.TutorialRepeatsImmediately(_coach.Tutorial.Phase, _coach.Tutorial.Successes))
            {
                var feedback = _coach.Tutorial.Feedback;
                PrepareTutorial(_coach.Tutorial.Lesson.Id);
                _lessons.PreviousFeedback = feedback;
                BeginTutorialAttempt();
                return true;
            }
            if (!_lessons.Modal(TutorialModal, out var settled))
            {
                if (!_coach.Tutorial.IsFieldLesson && !_match.LivePlay.Active && _coach.Tutorial.Phase == TutorialPhase.Attempt)
                {
                    var left = (double)dt;
                    var runnerInput = _coach.Tutorial.IsStealLesson ? RunInput() with { SouthDown = false, WestDown = false } : LivePadInput.Dead;
                    while (left > 0 && !_match.LivePlay.Active) { var step = Math.Min(left, .05); _coach.Tutorial.Tick(step, runnerInput); left -= step; }
                    if (_coach.Tutorial.IsStealLesson && _match.LivePlay.Active)
                    { StartRunnerPlay(null); return true; }
                }
                return false;
            }
            if (!settled) return true;
            var confirm = Controls.SouthDown;
            if (_lessons.MenuOpen)
            {
                switch (_lessons.TickMenu(dt, confirm, Controls.EastDown))
                {
                    case TutorialDirector.MenuStep.Choose: ChooseTutorialMenu(); break;
                    case TutorialDirector.MenuStep.Leave: _mode = PlayMode.Exhibition; _t = 0; break;
                }
                return true;
            }
            if (_guided != null && _guided.Phase == TutorialPhase.Brief)
            {
                if (confirm) BeginGuidedAttempt();
                else if (Controls.EastDown) OpenTutorials();
                return true;
            }
            if (_guided != null && _guided.Phase == TutorialPhase.Feedback)
            {
                if (confirm) PrepareTutorial(_guided.Lesson.Id);
                else if (Controls.WestDown) PrepareTutorial(_lessons.NextAfter(_guided.Lesson.Id));
                else if (Controls.EastDown) OpenTutorials();
                return true;
            }
            if (_coach.Tutorial.Phase == TutorialPhase.Brief)
            {
                if (confirm) BeginTutorialAttempt();
                else if (Controls.EastDown) OpenTutorials();
                return true;
            }
            if (TutorialFeedbackReady)
            {
                if (confirm) PrepareTutorial(_coach.Tutorial.Lesson.Id);
                else if (Controls.WestDown) PrepareTutorial(_lessons.NextAfter(_coach.Tutorial.Lesson.Id));
                else if (Controls.EastDown) OpenTutorials();
                return true;
            }
            return false;
        }

        void ChooseTutorialMenu()
        {
            if (_lessons.Chosen == null)
            {
                _lessons.CloseMenu(); PracticePick = PracticeLesson.Free; BeginTraining();
            }
            else PrepareTutorial(_lessons.Chosen.Id);
        }

        bool DrawTutorialUi()
        {
            if (!TutorialModal) return false;
            if (_guided != null) HudView.GuidedTutorial(_guided);
            else _lessons.Draw(TutorialOn ? _coach.Tutorial : null);
            return true;
        }

        // The guided lessons' flow: the state is GuidedTutorialDirector's; building the match and the scene is the flow's.
        bool GuidedAttempt(string id) => _lessons.Guided.Attempt(id);

        void PrepareGuidedTutorial(TutorialLesson lesson)
        {
            var pick = _lessons.Guided.Start(lesson, _lessons.Profile, _lessons.Progress,
                new GuidedTutorialDirector.Pick(HomeCaptain, AwayCaptain, ParkId, Night, Pad1Home, Seed));
            _coach?.Stop();
            ReleaseMatchSeats();
            _mode = PlayMode.Exhibition;
            _versusWanted = false;
            (HomeCaptain, AwayCaptain, ParkId, Night, Pad1Home, Seed) = (pick.Home, pick.Away, pick.Park, pick.Night, pick.Pad1Home, pick.Seed);
            _lessons.LessonReady();
            _match = NewMatch();
            _phase = Phase.Title;
            _cam.Play("title");
        }

        void BeginGuidedAttempt()
        {
            if (!_lessons.Guided.CanBegin(Controls.PadCount)) return;
            _lineup = null; // Every lesson attempt needs a fresh roster, unlike Back during setup.
            if (_guided.Lesson.Id == "T-G07")
            {
                _settings = _lessons.Guided.LendSettings(_settings, Innings, Difficulty);
                Innings = _settings.Innings; Difficulty = _settings.Difficulty;
            }
            _guided.Begin();
            _lessons.AttemptBegun();
            Controls.CatchPlay();
            if (!_lessons.Guided.StartsOnSet) { OpenField(); return; }
            _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
            _spec.Build(transform, DiamondGeometry.Of(_match.Rules)); _items.Build(transform); _stars?.Build(transform);
            _clip = null; _hlPath = null;
            BeginSet();
        }

        void ReturnGuidedSettings()
        {
            if (_lessons.Guided.ReturnSettings(out var own, out var innings, out var difficulty))
                (_settings, Innings, Difficulty) = (own, innings, difficulty);
        }

        /// <summary>A guided observation opened the lesson's feedback: the card starts fresh and the match runs again.</summary>
        void GuidedFeedbackOpened()
        {
            _lessons.FeedbackOpened();
            _match.SetPaused(false);
        }

        void GuidedObserve(string lesson, GuidedAction action)
        {
            if (_lessons.Guided.Observe(lesson, action)) GuidedFeedbackOpened();
        }

        void GuidedReadyChanged(LineupSeat seat)
        {
            var onSettings = _lineup != null && _lineup.Step == LineupStep.MatchSettings;
            _lessons.Guided.ReadyChanged(seat, onSettings, onSettings && _lineup.IsReady(seat));
        }

        void GuidedSettingsStart()
        {
            var onSettings = _lineup != null && _lineup.Step == LineupStep.MatchSettings;
            var humans = new List<LineupSeat>();
            if (onSettings && _lineup.HomeSeat != LineupSeat.Cpu) humans.Add(_lineup.HomeSeat);
            if (onSettings && _lineup.AwaySeat != LineupSeat.Cpu) humans.Add(_lineup.AwaySeat);
            if (_lessons.Guided.SettingsStart(onSettings, humans)) _lessons.FeedbackOpened();
        }

        /// <summary>Call time's Reset stick card closed (<see cref="PursuitSeatDirector"/>); a recalibrated close is T-G06-C's action.</summary>
        void StickResetClosed(bool recalibrated)
        {
            _t = 0;
            if (recalibrated) GuidedObserve("T-G06-C", GuidedAction.StickRecalibrated);
        }

        void GuidedSeatRecovered(LineupSeat seat)
        {
            if (_lessons.Guided.SeatRecovered(seat)) GuidedFeedbackOpened();
        }

        void GuidedSeatsBound()
        {
            if (_lessons.Guided.SeatsBound(_matchSeats.Bound, LiveSeats.BothHuman, Controls.SeatDeviceId(0), Controls.SeatDeviceId(1)))
                GuidedFeedbackOpened();
        }

        // The still gate's host (#1042): StillStaging owns the staging; the flow owns the match it stages and the switches.
        StillStaging _stills;
        internal StillStaging Stills => _stills ??= new StillStaging(Scene, Play, this);
        /// <summary>A HUD-off still is being captured: the play HUD draws nothing.</summary>
        bool CaptureMuteHud => _stills != null && _stills.MuteHud;
        string IStillHost.HomeCaptain => HomeCaptain;
        void IStillHost.Pick(string home, string away) { HomeCaptain = home; AwayCaptain = away; }
        void IStillHost.UsePark(string parkId, bool night) { ParkId = parkId; Night = night; }
        void IStillHost.ResetForStill(bool muteHud, bool feelDebug)
        {
            _mode = PlayMode.Exhibition;
            _forceMuteHud = muteHud; _feelDebug = feelDebug; _showTiming = false;
            _freezeCam = true; _gateHold = false; _turntable = false;
            _caught = false; _smash = 0; _juice.Clear();
        }
        void IStillHost.HoldStill(bool turntable)
        {
            _gateHold = true; _freezeCam = true;
            if (turntable) _turntable = true;
        }
        Match IStillHost.NewMatch() => NewMatch();
        void IStillHost.BeginSet() => BeginSet();
        bool IStillHost.HasLineup => _lineup != null;
        void IStillHost.OpenLineup() => OpenLineup();
        void IStillHost.HoldPitchInHand() => HoldPitchInHand();
        void IStillHost.CaptureReleaseFromHand() => CaptureReleaseFromHand();
        Vector3 IStillHost.Ball { get => _ball; set => _ball = value; }

        TrainingDirector ISeatHost.Coach => _coach;
        bool ISeatHost.Exhibition => _mode == PlayMode.Exhibition;
        bool ISeatHost.Pad1Home => Pad1Home;
        bool ISeatHost.VersusWanted => _versusWanted;

        // ---- The at-bat and the directors it calls on (#1042) ----
        // The at-bat (#1042): AtBatDirector owns SET → contact and its state; these names forward to it.
        AtBatDirector _atBat;
        internal AtBatDirector AtBat => _atBat ??= new AtBatDirector(Scene, Play, Live, Pads, this);
        internal ChargeButtonState _pitchButton { get => AtBat.PitchButton; set => AtBat.PitchButton = value; }

        /// <summary>
        /// The batting seat's plate buttons (spec §5.1, §5.8): the swing button, the two bunt triggers and the East / G
        /// cancel, stepped by the sim (<see cref="PlateButtons.Advance"/>) every frame in the spec's order. It also holds
        /// the leak guards (PH-13-R1, PH-14-R6): a trigger held for a bunt at contact and a cancel press the plate took
        /// are spent until they come up. It belongs to one pad (<see cref="_plateSeat"/>); a new batting pad starts at rest.
        /// </summary>
        internal PlateButtonsState _plate { get => Play.Plate; set => Play.Plate = value; }


        internal StarRequests StarAsks => AtBat.StarAsks;

        /// <summary>The at-bat's verbs, for the flow and the editor gates that drive them.</summary>
        internal void TickAtBat(float dt) => AtBat.TickAtBat(dt);
        internal void BeginSet() => AtBat.BeginSet();
        internal void TickSet(float dt) => AtBat.TickSet(dt);
        internal void Launch(PitchCommand pitch) => AtBat.Launch(pitch);
        internal void TickFlight(float dt) => AtBat.TickFlight(dt);
        internal void StartFly(AtBatResult hit, bool alreadyLive = false) => AtBat.StartFly(hit, alreadyLive);
        internal string ShownPitchType => AtBat.ShownPitchType;
        void HoldPitchInHand() => AtBat.HoldPitchInHand();
        void CaptureReleaseFromHand() => AtBat.CaptureReleaseFromHand();

        TrainingDirector IAtBatHost.Coach => _coach;
        bool IAtBatHost.TutorialOn => TutorialOn;
        void IAtBatHost.BindSeats() => BindMatchSeats();
        Match IAtBatHost.NextTrainingMatch() { Seed++; return _coach.MakeMatch(_content, Seed); }
        string IAtBatHost.BannerText { set => _banner = value; }
        string IAtBatHost.Sub { set => _sub = value; }
        float IAtBatHost.Smash { set => _smash = value; }
        void IAtBatHost.Banner() => Banner();
        void IAtBatHost.BeginResult() => BeginResult();
        InPlayDirector IAtBatHost.InPlay => _inPlay;
        JuiceDirector IAtBatHost.Juice => _juice;
        StealDirector IAtBatHost.Steal => Steal;
        DefenseSwapWindow IAtBatHost.Swap => Swap;
        ItemToss IAtBatHost.Toss => Toss;

        /// <summary>Whether <paramref name="pad"/>'s <paramref name="trigger"/> may mean any verb on this tick (PH-14-R6).</summary>
        internal bool TriggerFree(Controls.Pad pad, BuntSide trigger) => Pads.TriggerFree(pad, trigger);

        /// <summary>Whether <paramref name="pad"/>'s East / G may mean a dive, a dash or a skip on this tick (PH-13-R1).</summary>
        internal bool CancelFree(Controls.Pad pad) => Pads.CancelFree(pad);

        /// <summary>The pickoff (§4.5, D3): a runner on the bag is the beat; a runner who broke is the live runner play.</summary>
        StealDirector _steal;
        internal StealDirector Steal => _steal ??= new StealDirector(gameObject, Play, this);

        /// <summary>
        /// Select opens the defense window. South picks two positions; Select is the pitcher shortcut.
        /// East cancels a pending pick or closes. All baseball input waits for the window.
        /// </summary>
        /// <summary>Call time's Arrange defense: open SET's swap window.</summary>
        internal bool OpenDefenseSetup() => Swap.TryOpen();

        // The live play (#1042): InPlayDirector owns the play; the pads, the seats, the items and the result beat are the flow's.
        LiveSeats LiveSeatsNow() => Pads.LiveNow();
        internal LivePadInput FieldInput() => Pads.FieldInput();
        LivePadInput RunInput() => Pads.RunInput();

        /// <summary>A frame of the live play, for the editor gates that drive it.</summary>
        internal void TickLive(float dt) => _inPlay.Tick(dt);
        internal PlayKind LiveKind() => _inPlay.LiveKind();
        void StartRunnerPlay(PlayEvent pitch) => _inPlay.StartRunnerPlay(pitch);
        void SyncFromLive() => _inPlay.SyncFromLive();
        void AimLive() => _inPlay.AimLive();
        Character PlayFielder() => _inPlay.PlayFielder();
        bool BuddySet => _inPlay.BuddySet;
        (double X, double Z) WallPlant(FieldingPreview pre) => _inPlay.WallPlant(pre);

        LivePadInput IInPlayHost.FieldInput() => FieldInput();
        LivePadInput IInPlayHost.RunInput() => RunInput();
        void IInPlayHost.ClearThrowTarget() => FieldPad.ClearThrowTarget();
        LiveSeats IInPlayHost.LiveSeatsNow() => LiveSeatsNow();
        JuiceDirector IInPlayHost.Juice => _juice;
        TutorialSession IInPlayHost.FieldLesson => TutorialOn ? _coach.Tutorial : null;
        void IInPlayHost.OnFieldResult(FieldingResult result) => _coach?.OnField(result, _match);
        void IInPlayHost.TickItem(float dt) => Toss.Tick(dt);
        bool IInPlayHost.ItemFlying => Toss.Flying;
        void IInPlayHost.ItemSmashed() => Toss.Smashed();
        void IInPlayHost.Banner() => Banner();
        void IInPlayHost.BeginResult() => BeginResult();
        Vector3 IInPlayHost.SmashLook() => AtBat.SmashLook();
        float IInPlayHost.Smash { get => _smash; set => _smash = value; }
        string IInPlayHost.Sub { set => _sub = value; }
        void IInPlayHost.RestartClock() => _t = 0;

        // The bodies (#1042): ActorDirector draws them; the swing clocks it and the at-bat share live in PlayState.
        internal float _committedSwingT { get => Play.CommittedSwingT; set => Play.CommittedSwingT = value; }
        /// <summary>Seconds from the press to the committed take's Contact mark (D13); NaN until a swing commits.</summary>
        float _swingContactSec { get => Play.SwingContactSec; set => Play.SwingContactSec = value; }
        /// <summary>A frame of the bodies, for the editor gates that draw them.</summary>
        internal void DrawActors(float dt) => _actors.Draw(dt);

        bool IActorHost.TutorialModal => TutorialModal;
        bool IActorHost.Turntable => _turntable;
        bool IActorHost.Replaying => _replaying;
        bool IActorHost.HumanBats => HumanBats;
        bool IActorHost.HumanPitches => HumanPitches;
        bool IActorHost.HumanOwnsThrow => HumanOwnsThrow;
        bool IActorHost.SquaredNow => AtBat.SquaredNow;
        bool IActorHost.PlateSwingArmed => _plate.Swing.Armed;
        float IActorHost.PitchCharge => AtBat.PitchCharge;
        string IActorHost.ShownPitchType => AtBat.ShownPitchType;
        ItemToss IActorHost.Toss => Toss;
        float IActorHost.SwingContactSec(SwingCommand swing) => AtBat.SwingContactSec(swing);
        void IActorHost.ShowCursor() => AtBat.ShowCursor();
        void IActorHost.HoldBallInGlove() => HoldBallInGlove();
        void IActorHost.OnRun() { if (TrainingOn) _coach.OnRun(_match); }
        StealDirector IActorHost.Steal => Steal;
        JuiceDirector IActorHost.Juice => _juice;
        LineupScreens IActorHost.Lineup => _lineup;
        ExhibitionPick IActorHost.CurrentPick() => CurrentPick();
        Vector2 IActorHost.FieldStick => new Vector2(FieldPad.StickX, FieldPad.StickY);

        // The on-deck item (#1042): ItemToss owns the pick, the target and the throw; the flow owns the subtitle.
        ItemToss _toss;
        internal ItemToss Toss => _toss ??= new ItemToss(Scene, Play, Live, Pads, this);
        TrainingDirector IItemHost.Coach => _coach;
        string IItemHost.Sub { set => _sub = value; }

        // SET's Arrange defense window (#1042): DefenseSwapWindow owns the pick and the swaps; the flow resets the pitch selection.
        DefenseSwapWindow _swap;
        internal DefenseSwapWindow Swap => _swap ??= new DefenseSwapWindow(Play, Pads, this);
        TrainingDirector IDefenseSwapHost.Coach => _coach;
        void IDefenseSwapHost.PitcherChanged() => AtBat.PitchSelect = PitchSelectionState.Reset;

        // The pre-contact runner play (#1042): StealDirector owns the pickoff and the pre-contact clock; the flow owns the result beat.
        TutorialSession IRunnerPlayHost.Lesson => TutorialOn ? _coach.Tutorial : null;
        LiveSeats IRunnerPlayHost.LiveSeatsNow() => LiveSeatsNow();
        void IRunnerPlayHost.StartRunnerPlay() => StartRunnerPlay(null);
        void IRunnerPlayHost.EndWith(PlayEvent play) { _last = play; Banner(); BeginResult(); }
        string IRunnerPlayHost.Banner { set => _banner = value; }
    }
}
