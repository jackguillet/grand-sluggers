using System;
using System.Collections.Generic;
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
            if (Controls.WestDown)
            {
                BeginTraining();
                return;
            }
            if (_mode != PlayMode.Training)
            {
                if (Key(KeyCode.A) || Key(KeyCode.LeftArrow))
                {
                    HomeCaptain = PresetTeams.PrevCaptain(HomeCaptain);
                    if (_mode != PlayMode.Challenge)
                    {
                        ParkId = PresetTeams.HomeParkId(HomeCaptain);
                        RebuildTitlePark();
                    }
                }
                if (Key(KeyCode.D) || Key(KeyCode.RightArrow))
                {
                    HomeCaptain = PresetTeams.NextCaptain(HomeCaptain);
                    if (_mode != PlayMode.Challenge)
                    {
                        ParkId = PresetTeams.HomeParkId(HomeCaptain);
                        RebuildTitlePark();
                    }
                }
            }
            if (_mode == PlayMode.Exhibition)
            {
                if (Key(KeyCode.W) || Key(KeyCode.UpArrow)) AwayCaptain = PresetTeams.PrevCaptain(AwayCaptain);
                if (Key(KeyCode.S) || Key(KeyCode.DownArrow)) AwayCaptain = PresetTeams.NextCaptain(AwayCaptain);
                if (HomeCaptain.Equals(AwayCaptain, System.StringComparison.OrdinalIgnoreCase))
                    AwayCaptain = PresetTeams.NextCaptain(HomeCaptain);
            }
            if (_mode != PlayMode.Training && Controls.NightToggle)
            {
                Night = !Night;
                RebuildTitlePark();
            }
            if (_mode == PlayMode.Exhibition)
            {
                if (Controls.ParkHeld)
                {
                    _cHold += Time.deltaTime;
                    if (_cHold > 0.4f && !_cNight)
                    {
                        Night = !Night;
                        _cNight = true;
                    }
                }
                else
                {
                    if (_cHold > 0f && _cHold < 0.4f && !_cNight)
                    {
                        var i = System.Array.IndexOf(Parks, ParkId);
                        ParkId = Parks[(i < 0 ? 0 : i + 1) % Parks.Length];
                        RebuildTitlePark();
                    }
                    _cHold = 0f;
                    _cNight = false;
                }
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
            _clip = null;
            _hlPath = null;
            _replaying = false;
            _cam.Cut("select");
        }

        void TickSelect()
        {
            if (_selectStick > 0) _selectStick -= Time.deltaTime;
            else
            {
                var x = Controls.StickX;
                var y = Controls.StickY;
                if (Mathf.Abs(x) >= 0.45f && Mathf.Abs(x) >= Mathf.Abs(y))
                {
                    HomeCaptain = x > 0 ? PresetTeams.NextCaptain(HomeCaptain) : PresetTeams.PrevCaptain(HomeCaptain);
                    _match = NewMatch();
                    _selectStick = 0.22f;
                }
                else if (Mathf.Abs(y) >= 0.45f)
                {
                    AwayCaptain = y > 0 ? PresetTeams.PrevCaptain(AwayCaptain) : PresetTeams.NextCaptain(AwayCaptain);
                    if (HomeCaptain.Equals(AwayCaptain, System.StringComparison.OrdinalIgnoreCase))
                        AwayCaptain = PresetTeams.NextCaptain(HomeCaptain);
                    _match = NewMatch();
                    _selectStick = 0.22f;
                }
            }
            if (HomeCaptain.Equals(AwayCaptain, System.StringComparison.OrdinalIgnoreCase))
                AwayCaptain = PresetTeams.NextCaptain(HomeCaptain);
            LookAtHomeCaptain();
            if (Controls.SouthDown && _t > 0.15f)
                OpenLineup();
        }

        void LookAtHomeCaptain()
        {
            var ids = PresetTeams.CaptainIds;
            var i = 0;
            for (; i < ids.Length; i++)
                if (ids[i] == HomeCaptain) break;
            if (i >= ids.Length) i = 0;
            var x = (i - (ids.Length - 1) * 0.5f) * 7.6f;
            _cam.PlayLook("select", new Vector3(x, 3.2f, 12f));
        }

        void BeginTraining()
        {
            _mode = PlayMode.Training;
            ParkId = Training.ParkId;
            HomeCaptain = "rio";
            AwayCaptain = "ashlord";
            if (_coach == null) _coach = gameObject.AddComponent<TrainingDirector>();
            _coach.Begin(_content);
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
                _homeDraft = TeamBuilder.Draft(_content, HomeCaptain);
                var taken = new List<string>();
                for (var i = 0; i < _homeDraft.Order.Count; i++)
                    taken.Add(_homeDraft.Order[i].Id);
                _awayDraft = TeamBuilder.Draft(_content, AwayCaptain, taken);
                _lineupSlot = 0;
                _poolIndex = 0;
                _focusPool = true;
                _lineupTouched = false;
                _lineupStick = 0;
            }
            else
            {
                _homeDraft = null;
                _awayDraft = null;
            }
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
            if (_homeDraft == null)
            {
                if (Controls.SouthDown || _t > 10f) BeginSet();
                return;
            }

            TickLineupStick();
            if (Controls.WestDown)
            {
                _lineupTouched = true;
                TryDraftSwap();
            }
            if (Controls.CyclePitch)
            {
                _lineupTouched = true;
                var who = _homeDraft.Order[_lineupSlot];
                _homeDraft.CycleGlove(who.Id);
            }
            if (Controls.AllAdvanceDown)
            {
                _lineupTouched = true;
                _homeDraft.SwapOrder(_lineupSlot, _lineupSlot - 1);
                if (_lineupSlot > 0) _lineupSlot--;
            }
            if (Controls.EastDown)
            {
                _lineupTouched = true;
                _homeDraft.SwapOrder(_lineupSlot, _lineupSlot + 1);
                if (_lineupSlot < _homeDraft.Order.Count - 1) _lineupSlot++;
            }
            if (Controls.SouthDown || (_t > 10f && !_lineupTouched))
                ConfirmDraft();
        }

        void TickLineupStick()
        {
            var x = Controls.StickX;
            var y = Controls.StickY;
            if (Mathf.Abs(x) < 0.4f && Mathf.Abs(y) < 0.4f)
            {
                _lineupStick = 0;
                return;
            }
            if (_lineupStick > 0)
            {
                _lineupStick -= Time.deltaTime;
                return;
            }
            _lineupTouched = true;
            _lineupStick = 0.2f;
            if (Mathf.Abs(x) >= Mathf.Abs(y))
            {
                _focusPool = x > 0;
                return;
            }
            if (_focusPool)
            {
                var taken = new List<string>();
                if (_awayDraft != null)
                    for (var i = 0; i < _awayDraft.Order.Count; i++)
                        taken.Add(_awayDraft.Order[i].Id);
                var pool = _homeDraft.Pool(taken);
                if (pool.Count == 0) return;
                _poolIndex = (_poolIndex + (y < 0 ? 1 : -1) + pool.Count) % pool.Count;
            }
            else
                _lineupSlot = (_lineupSlot + (y < 0 ? 1 : -1) + _homeDraft.Order.Count) % _homeDraft.Order.Count;
        }

        void TryDraftSwap()
        {
            if (_homeDraft == null) return;
            var taken = new List<string>();
            if (_awayDraft != null)
                for (var i = 0; i < _awayDraft.Order.Count; i++)
                    taken.Add(_awayDraft.Order[i].Id);
            var pool = _homeDraft.Pool(taken);
            if (pool.Count == 0) return;
            _poolIndex = Mathf.Clamp(_poolIndex, 0, pool.Count - 1);
            _lineupSlot = Mathf.Clamp(_lineupSlot, 0, _homeDraft.Order.Count - 1);
            var outgoing = _homeDraft.Order[_lineupSlot];
            var incoming = pool[_poolIndex];
            if (!_homeDraft.Replace(outgoing.Id, incoming.Id)) return;
            var nextTaken = new List<string>();
            for (var i = 0; i < _homeDraft.Order.Count; i++)
                nextTaken.Add(_homeDraft.Order[i].Id);
            _awayDraft = TeamBuilder.Draft(_content, AwayCaptain, nextTaken);
            var next = _homeDraft.Pool(nextTaken);
            _poolIndex = next.Count == 0 ? 0 : Mathf.Clamp(_poolIndex, 0, next.Count - 1);
        }

        void ConfirmDraft()
        {
            if (_homeDraft != null)
            {
                var homeBat = _match.HomeBat;
                var homeGlove = _match.HomeGlove;
                var awayBat = _match.AwayBat;
                var awayGlove = _match.AwayGlove;
                var away = _awayDraft != null
                    ? _awayDraft.ToTeam()
                    : PresetTeams.ForCaptain(_content, AwayCaptain);
                _match = Match.Exhibition(_content, _homeDraft.ToTeam(), away, Innings, Seed, ParkId, Night);
                RestoreGear(homeBat, homeGlove, awayBat, awayGlove);
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

    }
}
