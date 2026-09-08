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
            if (_gun) return;
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

        void TickTitle()
        {
            if (Controls.Start)
            {
                _mode = _mode == PlayMode.Exhibition ? PlayMode.Challenge
                    : _mode == PlayMode.Challenge ? PlayMode.Training
                    : PlayMode.Exhibition;
                if (_mode == PlayMode.Training) ParkId = Training.ParkId;
            }
            if (Controls.CyclePitch && _mode == PlayMode.Exhibition)
                Innings = Innings == 3 ? 6 : Innings == 6 ? 9 : 3;
            if (_mode == PlayMode.Training)
            {
                if (Key(KeyCode.A) || Key(KeyCode.LeftArrow) || Key(KeyCode.W) || Key(KeyCode.UpArrow))
                    PracticePick = Training.Shift(PracticePick, -1);
                if (Key(KeyCode.D) || Key(KeyCode.RightArrow) || Key(KeyCode.S) || Key(KeyCode.DownArrow))
                    PracticePick = Training.Shift(PracticePick, 1);
                if (Controls.Skip)
                    PracticePick = PracticeLesson.Fielding;
            }
            if (Controls.WestDown || (_mode == PlayMode.Training && Controls.SouthDown && _t > 0.15f))
            {
                BeginTraining();
                return;
            }
            if (_mode != PlayMode.Training && Controls.NightToggle)
            {
                Night = !Night;
                RebuildTitlePark();
            }
            _cam.Cut("title");
            if (Controls.SouthDown)
            {
                if (_mode == PlayMode.Training)
                {
                    BeginTraining();
                    return;
                }
                _match = NewMatch();
                _park.Build(_match.Park, _match.Night);
                _spec.Build(transform);
                _items.Build(transform);
                _stars?.Build(transform);
                if (_mode == PlayMode.Challenge)
                    OpenLineup();
                else
                    OpenSelect();
            }
        }

        void RebuildTitlePark()
        {
            if (_park == null || _content == null) return;
            if (!_content.Parks.TryGetValue(ParkId, out var park)) return;
            _park.Build(park, Night);
            if (_phase == Phase.Title)
                _cam?.Cut("title");
        }

        void OpenSelect()
        {
            _phase = Phase.Select;
            _t = 0;
            _selectArmedX = MenuNav.Arm(Controls.Pad1.MenuAxisX);
            _selectArmedY = MenuNav.Arm(Controls.Pad1.MenuAxisY);
            _selectArmedX2 = MenuNav.Arm(Controls.Pad2.MenuAxisX);
            _clip = null;
            _hlPath = null;
            _replaying = false;
            _cam.Cut("select");
        }

        void TickSelect()
        {
            var p1 = Controls.Pad1;
            var p2 = Controls.Pad2;
            if (p1.NorthDown && _t > 0.15f)
                ApplyPick(ExhibitionPick.ToggleSeat(CurrentPick()));
            var dx = MenuNav.Step(p1.MenuAxisX, p1.MenuTapX, ref _selectArmedX);
            var dy = MenuNav.Step(p1.MenuAxisY, p1.MenuTapY, ref _selectArmedY);
            if (dx != 0)
                ApplyPick(ExhibitionPick.CycleYours(CurrentPick(), dx));
            else if (dy != 0 && !p2.Present)
                ApplyPick(ExhibitionPick.CycleTheirs(CurrentPick(), dy > 0 ? -1 : 1));
            if (p2.Present)
            {
                var d2 = MenuNav.Step(p2.MenuAxisX, p2.MenuTapX, ref _selectArmedX2);
                if (d2 != 0)
                    ApplyPick(ExhibitionPick.CycleTheirs(CurrentPick(), d2));
            }
            LookAtYourCaptain();
            if (Controls.WestDown && _t > 0.15f)
            {
                OpenTitle();
                return;
            }
            if (Controls.SouthDown && _t > 0.15f)
                OpenField();
        }

        void OpenField()
        {
            _phase = Phase.Field;
            _t = 0;
            _selectArmedX = MenuNav.Arm(Controls.MenuX);
            _clip = null;
            _hlPath = null;
            _replaying = false;
            _match = NewMatch();
            RebuildTitlePark();
            _cam.Play("field");
        }

        void TickField()
        {
            var dx = MenuNav.Step(Controls.MenuX, Controls.MenuTapX, ref _selectArmedX);
            if (dx != 0)
            {
                ApplyPick(ExhibitionPick.CyclePark(CurrentPick(), dx));
                RebuildTitlePark();
            }
            if (Controls.NightToggle)
            {
                Night = !Night;
                RebuildTitlePark();
            }
            _cam.Play("field");
            if (Controls.WestDown && _t > 0.15f)
            {
                OpenSelect();
                return;
            }
            if (Controls.SouthDown && _t > 0.15f)
                OpenLineup();
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
            _park.Build(_match.Park, _match.Night);
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
                _park.Build(_match.Park, _match.Night);
                _spec.Build(transform);
                _items.Build(transform);
                _stars?.Build(transform);
                _clip = null;
                _hlPath = null;
                OpenLineup();
                return;
            }
            _match = NewMatch();
            _park.Build(_match.Park, _match.Night);
            _spec.Build(transform);
            _items.Build(transform);
            _stars?.Build(transform);
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
                _lineup = LineupScreens.Open(_content, HomeCaptain, AwayCaptain, seats.Home, seats.Away);
                _lineupTouched = false;
                _lineupStick = MenuNav.Arm(Controls.Pad1.MenuAxisX);
                _lineupStick2 = MenuNav.Arm(Controls.Pad2.MenuAxisX);
                _lineupArmedY = MenuNav.Arm(Controls.Pad1.MenuAxisY);
                _lineupArmedY2 = MenuNav.Arm(Controls.Pad2.MenuAxisY);
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
            if (Key(KeyCode.B)) _match.CycleBat(true);
            if (Key(KeyCode.G)) _match.CycleGlove(true);
            if (Key(KeyCode.N)) _match.CycleBat(false);
            if (Key(KeyCode.M)) _match.CycleGlove(false);
            if (_lineup == null)
            {
                if (Controls.SouthDown || _t > 10f) BeginSet();
                return;
            }

            SyncLineupSeats();
            TickLineupPad(Controls.Pad1, LineupSeat.Pad1, ref _lineupStick, ref _lineupArmedY);
            if (_lineup.AwaySeat == LineupSeat.Pad2)
                TickLineupPad(Controls.Pad2, LineupSeat.Pad2, ref _lineupStick2, ref _lineupArmedY2);
            else if (_t > 10f && !_lineupTouched)
                ConfirmDraft();
        }

        void SyncLineupSeats()
        {
            if (_lineup == null) return;
            var seats = LiveSeats;
            if (_lineup.HomeSeat != seats.Home || _lineup.AwaySeat != seats.Away)
                _lineup.Sit(seats.Home, seats.Away);
        }

        void TickLineupPad(Controls.Pad pad, LineupSeat seat, ref float armedX, ref float armedY)
        {
            TickLineupStick(pad, seat, ref armedX, ref armedY);
            if (pad.WestDown)
            {
                _lineupTouched = true;
                _lineup.West(seat);
            }
            if (pad.CyclePitch)
            {
                _lineupTouched = true;
                if (_lineup.Step == LineupStep.TeamSetup) _lineup.RandomFill(seat);
                else _lineup.CycleGlove(seat);
            }
            if (pad.AllAdvanceDown)
            {
                _lineupTouched = true;
                _lineup.StepBatting(seat, -1);
            }
            if (pad.EastDown)
            {
                _lineupTouched = true;
                _lineup.StepBatting(seat, 1);
            }
            if (pad.SouthDown)
            {
                _lineupTouched = true;
                if (_lineup.Step == LineupStep.TeamSetup) _lineup.South(seat);
                else ConfirmDraft();
            }
        }

        void TickLineupStick(Controls.Pad pad, LineupSeat seat, ref float armedX, ref float armedY)
        {
            var dx = MenuNav.Step(pad.MenuAxisX, pad.MenuTapX, ref armedX);
            var dy = MenuNav.Step(pad.MenuAxisY, pad.MenuTapY, ref armedY);
            if (dx == 0 && dy == 0) return;
            if (dx != 0 && Mathf.Abs(pad.MenuAxisX) >= Mathf.Abs(pad.MenuAxisY))
                dy = 0;
            else if (dy != 0)
                dx = 0;
            if (dx == 0 && dy == 0) return;
            _lineupTouched = true;
            _lineup.Stick(seat, dx, dy);
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
                    _match = Match.Exhibition(_content, _lineup.Home.ToTeam(), away, Innings, Seed, ParkId, Night);
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
