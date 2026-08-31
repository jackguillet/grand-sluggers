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
            var hold = _last?.Kind == PlayKind.HomeRun ? 2.4f : (float)_feel.AfterOutSeconds;
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
            _cam.Play("title");
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
            _cam?.Play("title");
        }

        void OpenSelect()
        {
            _phase = Phase.Select;
            _t = 0;
            _selectStick = 0;
            _selectStick2 = 0;
            _clip = null;
            _hlPath = null;
            _replaying = false;
            _cam.Cut("select");
        }

        void TickSelect()
        {
            var p1 = Controls.Pad1;
            var p2 = Controls.Pad2;
            if (_selectStick > 0) _selectStick -= Time.deltaTime;
            else
            {
                var x = p1.StickX;
                var y = p1.StickY;
                if (Mathf.Abs(x) >= 0.45f && Mathf.Abs(x) >= Mathf.Abs(y))
                {
                    ApplyPick(ExhibitionPick.CycleHome(CurrentPick(), x > 0 ? 1 : -1));
                    _selectStick = 0.22f;
                }
                else if (!p2.Present && Mathf.Abs(y) >= 0.45f)
                {
                    ApplyPick(ExhibitionPick.CycleAway(CurrentPick(), y > 0 ? -1 : 1));
                    _selectStick = 0.22f;
                }
            }
            if (p2.Present)
            {
                if (_selectStick2 > 0) _selectStick2 -= Time.deltaTime;
                else if (Mathf.Abs(p2.StickX) >= 0.45f)
                {
                    ApplyPick(ExhibitionPick.CycleAway(CurrentPick(), p2.StickX > 0 ? 1 : -1));
                    _selectStick2 = 0.22f;
                }
            }
            LookAtHomeCaptain();
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
            _selectStick = 0;
            _clip = null;
            _hlPath = null;
            _replaying = false;
            _match = NewMatch();
            RebuildTitlePark();
            _cam.Play("field");
        }

        void TickField()
        {
            if (_selectStick > 0) _selectStick -= Time.deltaTime;
            else
            {
                var x = Controls.StickX;
                if (Mathf.Abs(x) >= 0.45f)
                {
                    ApplyPick(ExhibitionPick.CyclePark(CurrentPick(), x > 0 ? 1 : -1));
                    RebuildTitlePark();
                    _selectStick = 0.22f;
                }
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

        ExhibitionPick CurrentPick() => new(HomeCaptain, AwayCaptain, ParkId);

        void ApplyPick(ExhibitionPick pick)
        {
            HomeCaptain = pick.Home;
            AwayCaptain = pick.Away;
            ParkId = pick.Park;
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
            _cam.Play("title");
        }

        void LookAtHomeCaptain()
        {
            var ids = PresetTeams.CaptainIds;
            var i = 0;
            for (; i < ids.Length; i++)
                if (ids[i] == HomeCaptain) break;
            if (i >= ids.Length) i = 0;
            var spot = CarnivalFront.CaptainSpot(i, ids.Length, select: true, home: true);
            _cam.PlayLook("select", new Vector3(spot.X, 4.4f, spot.Z));
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
                _lineupStick = 0;
                _lineupStick2 = 0;
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
            TickLineupPad(Controls.Pad1, LineupSeat.Pad1, ref _lineupStick);
            if (_lineup.AwaySeat == LineupSeat.Pad2)
                TickLineupPad(Controls.Pad2, LineupSeat.Pad2, ref _lineupStick2);
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

        void TickLineupPad(Controls.Pad pad, LineupSeat seat, ref float stickT)
        {
            TickLineupStick(pad, seat, ref stickT);
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

        void TickLineupStick(Controls.Pad pad, LineupSeat seat, ref float stickT)
        {
            var x = pad.StickX;
            var y = pad.StickY;
            if (Mathf.Abs(x) < 0.4f && Mathf.Abs(y) < 0.4f)
            {
                stickT = 0;
                return;
            }
            if (stickT > 0)
            {
                stickT -= Time.deltaTime;
                return;
            }
            _lineupTouched = true;
            stickT = 0.2f;
            var dx = Mathf.Abs(x) >= Mathf.Abs(y) ? (x > 0 ? 1 : -1) : 0;
            var dy = dx == 0 ? (y > 0 ? 1 : -1) : 0;
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
