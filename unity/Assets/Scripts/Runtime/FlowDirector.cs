using System;
using GrandSluggers.Sim;
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

    public sealed partial class MatchDirector
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
                if (_t > 2.05f || Controls.SouthDown)
                {
                    _replaying = false;
                    _t = 0;
                    _cam.Play("replay");
                }
                return;
            }
            if (Controls.SouthDown && _t > 0.2f) ConfirmGameOver();
        }

        int _titleFocus, _fieldFocus, _captainFocus;
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
            _spec.Build(transform); _items.Build(transform); _stars?.Build(transform);
            OpenField();
        }

        void OpenControlsBook()
        {
            _pausePad = Controls.Pad1;
            _match.SetPaused(true);
            _pauseHowTo = _pauseFromHowTo = true;
            _pausePage = 0; _t = 0;
            BookScheme.Open(); Controls.CatchPlay();
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
            _captainFocus = 0;
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
            var p1 = Controls.Pad1;
            var p2 = Controls.Pad2;
            var pad2Sits = _versusWanted && p2.Present;
            var dt = Time.unscaledDeltaTime;
            var dy = _selectY.Tick(p1.MenuAxisY, p1.MenuTapY, dt);
            var dx = _selectX.Tick(p1.MenuAxisX, p1.MenuTapX, dt);
            if (dy != 0) _captainFocus = (_captainFocus + (dy > 0 ? 4 : 1)) % 5;
            if (dx != 0)
            {
                if (_captainFocus == 0) ApplyPick(ExhibitionPick.CycleYours(CurrentPick(), dx));
                if (_captainFocus == 1 && !pad2Sits) ApplyPick(ExhibitionPick.CycleTheirs(CurrentPick(), dx));
                if (_captainFocus == 2) WantVersus(!_versusWanted);
                if (_captainFocus == 3) ApplyPick(ExhibitionPick.ToggleSeat(CurrentPick()));
            }
            if (pad2Sits)
            {
                var d2 = _selectX2.Tick(p2.MenuAxisX, p2.MenuTapX, dt);
                if (d2 != 0) ApplyPick(ExhibitionPick.CycleTheirs(CurrentPick(), d2));
            }
            if (p1.SouthDown && _captainFocus is 2 or 3 && _t > .15f)
            {
                if (_captainFocus == 2) WantVersus(!_versusWanted);
                else ApplyPick(ExhibitionPick.ToggleSeat(CurrentPick()));
                return;
            }
            LookAtYourCaptain();
            var navigation = SetupSheet.Pointer(false, out _);
            if ((Controls.EastDown || navigation == SetupSheet.Action.Back) && _t > 0.15f)
            {
                OpenField();
                return;
            }
            if (Controls.PointerDown && _t > 0.15f)
            {
                var mouse = Controls.GuiMouse;
                if (CarnivalFront.HitSeatMode(mouse.x, mouse.y, Screen.width, Screen.height) is { } versus)
                {
                    WantVersus(versus);
                    return;
                }
            }
            if ((navigation == SetupSheet.Action.Next || (Controls.SouthDown && _captainFocus == 4 && !Controls.PointerDown)) && _t > 0.15f)
            {
                BindMatchSeats();
                GuidedSeatsBound();
                if (_guided?.Phase != TutorialPhase.Feedback) OpenLineup();
            }
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

        void TickField()
        {
            var dy = _selectY.Tick(Controls.MenuY, Controls.MenuTapY, Time.unscaledDeltaTime);
            var dx = _selectX.Tick(Controls.MenuX, Controls.MenuTapX, Time.unscaledDeltaTime);
            if (dy != 0) _fieldFocus = (_fieldFocus + (dy > 0 ? 3 : 1)) % 4;
            if (_t <= .15f) return;
            if (Controls.EastDown) { OpenTitle(); return; }
            if (dx != 0 || Controls.SouthDown)
            {
                if (_fieldFocus == 0) ApplyPick(ExhibitionPick.CyclePark(_content, CurrentPick(), dx == 0 ? 1 : dx));
                if (_fieldFocus == 1) Night = !Night;
                if (_fieldFocus == 2) Hazards = !Hazards;
                if (_fieldFocus == 3 && Controls.SouthDown) { OpenSelect(); return; }
                RebuildTitlePark();
            }
            _cam.Play("field");
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

        void OpenTitle()
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

        void LookAtYourCaptain()
        {
            var yours = CurrentPick().Yours;
            var ids = PresetTeams.CaptainIds;
            var i = 0;
            for (; i < ids.Length; i++)
                if (ids[i] == yours) break;
            if (i >= ids.Length) i = 0;
            var look = CarnivalFront.SelectLook(i, ids.Length);
            _cam.PlayLook("select", new Vector3(look.X, look.Y, look.Z));
        }

        void BeginTraining()
        {
            _mode = PlayMode.Training;
            ParkId = Training.ParkId;
            HomeCaptain = "rio";
            AwayCaptain = "ashlord";
            if (_coach == null) _coach = gameObject.AddComponent<TrainingDirector>();
            _coach.Begin(_content, PracticePick);
            _match = _coach.MakeMatch(_content, Seed);
            _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
            _spec.Build(transform);
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
            if (_coach != null && _coach.Session != null && _coach.Session.Finished)
            {
                PlayerPrefs.SetInt(TrainedKey, 1);
                PlayerPrefs.Save();
                _hideHelp = true;
            }
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
            if (_campaign != null && !_campaign.AllBeaten)
            {
                _match = _campaign.MakeMatch(_content, Innings, Seed);
                _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
                _spec.Build(transform);
                _items.Build(transform);
                _stars?.Build(transform);
                _clip = null;
                _hlPath = null;
                OpenLineup();
                return;
            }
            _match = NewMatch();
            _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
            _spec.Build(transform);
            _items.Build(transform);
            _stars?.Build(transform);
            ReleaseMatchSeats();
            _phase = Phase.Title;
            _t = 0;
            _replaying = false;
            _audio?.CrowdBed(false);
            _cam.Play("replay");
        }

        void OpenLineup()
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

        static bool Key(KeyCode k) => UnityEngine.Input.GetKeyDown(k);

        void TickLineup()
        {
            if (_lineup == null)
            {
                if (Controls.SouthDown || _t > 10f) BeginSet();
                return;
            }

            SyncLineupSeats();
            if (_lineup.Step == LineupStep.MatchSettings) { TickMatchSettings(); return; }
            var action = TeamSheet.Pointer(_lineup, out var focus, out var index);
            if (action == TeamSheet.Action.Player && _lineup.FocusCell(LineupSeat.Pad1, focus, index))
            {
                if (_lineup.Step == LineupStep.DefenseSetup) PickLineup(LineupSeat.Pad1);
                else if (focus == LineupFocus.Pool) DropLineup(LineupSeat.Pad1);
            }
            else if (action == TeamSheet.Action.Continue)
            {
                if (_lineup.Step == LineupStep.TeamSetup) _lineup.ConfirmTeam();
                else ReadyLineup(LineupSeat.Pad1);
            }
            else if (action == TeamSheet.Action.Back)
            {
                if (_lineup.Step == LineupStep.TeamSetup) OpenSelect();
                else _lineup.West(LineupSeat.Pad1);
            }
            else if (action == TeamSheet.Action.Fill) _lineup.RandomFill(LineupSeat.Pad1);
            if (_phase != Phase.Lineup || _lineup.Step == LineupStep.MatchSettings) return;
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
            if (!_lineup.PickOrSwap(seat) || seat != LineupSeat.Pad1 || !GuidedAttempt("T-G01")) return;
            GuidedObserve(focus is LineupFocus.HomeOrder or LineupFocus.AwayOrder
                ? GuidedAction.BattingOrderChanged : GuidedAction.GlovePositionChanged);
        }

        void DropLineup(LineupSeat seat)
        {
            var pool = _lineup.Pool;
            var who = pool.Count == 0 ? null : pool[Mathf.Clamp(_lineup.PoolOf(seat), 0, pool.Count - 1)];
            var dropped = _lineup.South(seat);
            GuidedLineupDrop(seat, who, dropped && _lineup.Step == LineupStep.TeamSetup);
        }

        void TickLineupPad(Controls.Pad pad, LineupSeat seat, ref MenuNav.Gate armedX, ref MenuNav.Gate armedY)
        {
            TickLineupStick(pad, seat, ref armedX, ref armedY);
            if (pad.PageNext && _lineup.Step == LineupStep.TeamSetup)
            {
                TeamSheet.UseController(seat);
                _lineup.RandomFill(seat);
            }
            if (pad.WestDown && _lineup.Step == LineupStep.TeamSetup)
            {
                TeamSheet.UseController(seat);
                _lineup.Remove(seat);
            }
            if (pad.EastDown)
            {
                TeamSheet.UseController(seat);
                if (_lineup.Step == LineupStep.TeamSetup) { if (seat == LineupSeat.Pad1) OpenSelect(); }
                else _lineup.West(seat); // cancel pick, withdraw ready, then back; never change panels
                return;
            }
            if ((pad.PagePrevious || pad.PageNext) && _lineup.Step == LineupStep.DefenseSetup)
            {
                TeamSheet.UseController(seat);
                _lineup.ToggleArea(seat);
            }
            if (pad.NorthDown && _lineup.CanPlay)
            {
                ReadyLineup(seat);
                return;
            }
            // A pointer click is handled by its hit target above, never also as a global confirm.
            if (pad.SouthDown && !(seat == LineupSeat.Pad1 && Controls.PointerDown))
            {
                TeamSheet.UseController(seat);
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
            TeamSheet.UseController(seat);
            _lineup.Stick(seat, dx, dy);
        }

        void ReadyLineup(LineupSeat seat)
        {
            _lineup.ToggleReady(seat);
            if (!_lineup.BothReady) return;
            if (_lineup.Step == LineupStep.DefenseSetup)
            {
                _lineup.OpenSettings();
                _lineupX.Catch(Controls.Pad1.MenuAxisX);
                _lineupY.Catch(Controls.Pad1.MenuAxisY);
                TeamSheet.HideBoard();
            }
            else ConfirmDraft();
        }

        void TickMatchSettings()
        {
            var pad = Controls.Pad1;
            var action = SetupSheet.Pointer(true, out var row);
            var dy = _lineupY.Tick(pad.MenuAxisY, pad.MenuTapY, Time.unscaledDeltaTime);
            var dx = _lineupX.Tick(pad.MenuAxisX, pad.MenuTapX, Time.unscaledDeltaTime);
            if (dy != 0) _settings.Move(dy > 0 ? -1 : 1);
            if (action == SetupSheet.Action.Change) _settings.Select(row);
            if (dx != 0 || action == SetupSheet.Action.Change || (pad.SouthDown && !Controls.PointerDown))
            {
                if (_settings.Change(LineupSeat.Pad1, dx == 0 ? 1 : dx)) _lineup.ResetReady();
                Innings = _settings.Innings;
                Difficulty = _settings.Difficulty;
            }
            if (pad.EastDown || action == SetupSheet.Action.Back) { _lineup.West(LineupSeat.Pad1); return; }
            if (pad.NorthDown || action == SetupSheet.Action.Next) ReadyLineup(LineupSeat.Pad1);
            if (_phase != Phase.Lineup || _lineup.Step != LineupStep.MatchSettings) return;
            if (_lineup.HomeSeat == LineupSeat.Pad2 || _lineup.AwaySeat == LineupSeat.Pad2)
            {
                if (Controls.Pad2.EastDown) _lineup.West(LineupSeat.Pad2);
                else if (Controls.Pad2.NorthDown) ReadyLineup(LineupSeat.Pad2);
            }
        }

        void ConfirmDraft()
        {
            if (_lineup != null)
            {
                if (_lineup.Step == LineupStep.TeamSetup)
                {
                    _lineup.RandomFill();
                    _lineup.ConfirmTeam();
                }
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
            TeamSheet.HideBoard();
            BeginSet();
        }

        void RestoreGear(BatItem homeBat, GloveItem homeGlove, BatItem awayBat, GloveItem awayGlove)
        {
            for (var i = 0; i < 12 && _match.HomeBat.Id != homeBat.Id; i++) _match.CycleBat(true);
            for (var i = 0; i < 12 && _match.HomeGlove.Id != homeGlove.Id; i++) _match.CycleGlove(true);
            for (var i = 0; i < 12 && _match.AwayBat.Id != awayBat.Id; i++) _match.CycleBat(false);
            for (var i = 0; i < 12 && _match.AwayGlove.Id != awayGlove.Id; i++) _match.CycleGlove(false);
        }

    }
}
