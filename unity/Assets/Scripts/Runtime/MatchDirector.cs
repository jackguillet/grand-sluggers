using System;
using System.Collections.Generic;
using System.IO;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed partial class MatchDirector : MonoBehaviour
    {
        public int Seed = 7;
        public int Innings = 3;
        public PracticeLesson PracticePick = PracticeLesson.Pitching;
        int _pauseItem;
        bool _pauseHowTo;
        bool _pauseFromHowTo;
        int _pausePage;
        float _pauseStick;
        MenuNav.Gate _menuX;
        MenuNav.Gate _pauseY;
        bool _wheelSpin;
        MenuNav.Gate _selectX;
        MenuNav.Gate _selectY;
        MenuNav.Gate _selectX2;
        public string ParkId = "harbor-diamond";
        public string HomeCaptain = "rio";
        public string AwayCaptain = "ashlord";
        public bool Night;
        [System.NonSerialized] public bool Pad1Home = true;
        bool _versusWanted;
        readonly MatchSeatLifecycle _matchSeats = new MatchSeatLifecycle();
        readonly DeviceSeatRecovery _deviceRecovery = new DeviceSeatRecovery();
        LineupScreens _lineup;
        bool _lineupTouched;
        MenuNav.Gate _lineupX;
        MenuNav.Gate _lineupX2;
        MenuNav.Gate _lineupY;
        MenuNav.Gate _lineupY2;
        enum PlayMode { Exhibition, Challenge, Training }
        PlayMode _mode;
        Challenge _campaign;
        TrainingDirector _coach;
        ContentCatalog _content;
        Match _match;
        ParkView _park;
        CameraRig _rig;
        CameraDirector _cam;
        FeelTable _feel;
        FlowDirector _flow;
        AtBatDirector _atBat;
        InPlayDirector _inPlay;
        ActorDirector _actors;
        SpecialFx _spec;
        ItemView _items;
        LandingRing _ring;
        StrikeZone _zone;
        AudioBus _audio;
        StarMeter _stars;
        const string TrainedKey = "gs.trained";
        bool _hideHelp;
        HighlightClip _clip;
        Vector3 _hlAt;
        Sample[] _hlPath;
        bool _replaying;
        bool _turntable;
        readonly Dictionary<string, HeroActor> _heroes = new Dictionary<string, HeroActor>();
        readonly HashSet<string> _used = new HashSet<string>();

        enum Phase { Title, Select, Field, Lineup, Set, Flight, InPlay, StealThrow, Result, GameOver }
        Phase _phase = Phase.Title;
        /// <summary>The SET swap pick while open (spec §4.7, #582); null otherwise.</summary>
        PitcherSwapPick _swapPick;
        float _swapArmed, _swapHold;
        int _itemPick;
        Character _itemTarget;
        bool _itemThrown;
        bool _itemFlying;
        float _itemFly;
        string _itemId = "";
        bool _starPitch;
        bool _starSwing;
        bool _bunt;
        float _charge;
        float _chargePast;
        float _pitchCharge;
        float _pitchPast;
        float _breakX;
        float _dash01;
        float _t;
        float _pip;
        PitchCommand _pitch;
        SwingCommand _swing;
        PlayEvent _last;
        AtBatResult _pending;
        FieldingPreview _preview;
        bool _playerFielding;
        bool _swung;
        float _flight;
        float _pitchDur = 0.5f;
        bool _pitchAir;
        Vector3 _relFrom;
        float LiveTime => _match != null ? (float)_match.LivePlay.ElapsedSeconds : 0f;
        float _freeze;
        float _smash;
        bool _showTiming;
        bool _feelDebug;
        bool _forceMuteHud;
        bool _gateHold;
        CardToy _card;
        LogoToy _logo;
        ChemToy _chem;
        float _feelSlow = 1f;
        bool _freezeCam;
        float _aimX, _aimY;
        Sample[] _path;
        Vector3 _ball;
        double _fx, _fz;
        bool _caught, _buddy;
        int _throwBag;
        readonly Dictionary<string, (double X, double Z)> _gloveAt = new Dictionary<string, (double X, double Z)>();
        string _glovePos = "P";
        string _switchPos = "";
        string _throwFromPos = "";
        string _buddyPos = "";
        bool _buddyWindow;
        float _diveT, _jumpT, _swapLock;
        bool _catchDive, _catchJump;
        bool _throwing;
        float _throwT, _throwDur;
        bool _closePlay;
        string _bagStamp = "";
        float _bagStampT;
        bool _closeIcon;
        int _closeBag;
        string _coverPos = "";
        float _recoilT;
        bool _bobbling;
        FieldingResult _cpuField;
        ThrowResult _armedThrow;
        Vector3 _throwFrom, _throwTo;
        string _banner, _sub;

        bool TrainingOn => _coach != null && _coach.Session != null;
        Seats SelectedSeats =>
            TrainingOn || _mode != PlayMode.Exhibition
                ? Seats.One
                : Seats.FromPads(Controls.PadCount, Pad1Home, versus: _versusWanted);
        Seats LiveSeats => _matchSeats.Current(SelectedSeats);
        bool HumanPitches => TrainingOn
            ? _coach.PlayerPitches
            : _match != null && LiveSeats.HumanPitches(_match.Top);
        bool HumanBats => TrainingOn
            ? (_coach.PlayerBats || _coach.PlayerRuns)
            : _match != null && LiveSeats.HumanBats(_match.Top);
        bool PlayerMustField => TrainingOn && _coach.PlayerFields;
        bool PlayerFields => _playerFielding || PlayerMustField;
        /// <summary>A human sits the defense this half (spec §0.4). Mirrors <see cref="GrandSluggers.Sim.LiveSeats.HumanFields"/>.</summary>
        bool HumanFields => LiveSeatsNow().HumanFields;
        bool HumanOwnsThrow => LiveSeatsNow().HumanOwnsThrow;
        Controls.Pad PitchPad => HumanPitches && _match != null
            ? Controls.Of(LiveSeats.Pitching(_match.Top))
            : Controls.Pad1;
        Controls.Pad BatPad => HumanBats && _match != null
            ? Controls.Of(LiveSeats.Batting(_match.Top))
            : Controls.None;
        // The glove pad is the controller seated on defense this half; the runner pad is the one
        // on offense. A seat the CPU holds is a dead pad, so the batting human's stick never
        // takes a glove and their South never gates a CPU throw (#579, #209).
        Controls.Pad FieldPad => !HumanFields ? Controls.None
            : TrainingOn ? Controls.Pad1
            : Controls.Of(LiveSeats.Fielding(_match.Top));
        Controls.Pad RunPad => !HumanBats ? Controls.None
            : TrainingOn ? Controls.Pad1
            : Controls.Of(LiveSeats.Running(_match.Top));
        bool ItemOffered =>
            HumanBats && _pending != null && _pending.ChemistryItemOffered && !_itemThrown
            && _phase == Phase.InPlay && !_throwing;

        void Start()
        {
            Controls.Initialize();
            var data = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "data"));
            _content = ContentCatalog.Load(data);
            ArtBinder.Bind(_content.Art);
            _coach = gameObject.AddComponent<TrainingDirector>();
            _match = NewMatch();
            _park = gameObject.AddComponent<ParkView>();
            _park.Build(_match.Park, _match.Night, _content.Rules);
            _spec = gameObject.AddComponent<SpecialFx>();
            _spec.Build(transform);
            _items = gameObject.AddComponent<ItemView>();
            _items.Build(transform);
            _zone = gameObject.AddComponent<StrikeZone>();
            _zone.Build(transform, _content.Rules);
            _ring = gameObject.AddComponent<LandingRing>();
            _ring.Build(transform);
            _audio = gameObject.AddComponent<AudioBus>();
            _audio.Build(data);
            _stars = gameObject.AddComponent<StarMeter>();
            _stars.Build(transform);
            _hideHelp = PlayerPrefs.GetInt(TrainedKey, 0) == 1;
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            _feel = _content.Feel;
            _rig = gameObject.AddComponent<CameraRig>();
            _rig.Bind(cam);
            _cam = gameObject.AddComponent<CameraDirector>();
            _cam.Bind(_rig, _content.Shots, _feel);
            _cam.Cut("title");
            _flow = new FlowDirector(this);
            _atBat = new AtBatDirector(this);
            _inPlay = new InPlayDirector(this);
            _actors = new ActorDirector(this);
            StillCapture.Attach(this);
        }

        void Update()
        {
            Controls.Tick(Time.unscaledDeltaTime);
            if (_match == null) return;
            if (TickDeviceRecovery())
            {
                _actors.Draw(0f);
                return;
            }
            var dt = Time.deltaTime;
            if (Controls.TimingAid) _showTiming = !_showTiming;
            if (Controls.FeelDebug) _feelDebug = !_feelDebug;
            if (Controls.HudMute) _forceMuteHud = !_forceMuteHud;
            if (_feelDebug && Controls.SlowMo)
                _feelSlow = _feelSlow > 0.9f ? 0.35f : _feelSlow > 0.2f ? 0.12f : 1f;
            if (_feelDebug && Controls.FreezeCam) _freezeCam = !_freezeCam;
            if (_feelDebug && _feelSlow < 0.99f) dt *= _feelSlow;
            if (_freeze > 0)
            {
                _freeze -= Time.unscaledDeltaTime;
                dt *= 0.12f;
            }
            _t += dt;
            if (!string.IsNullOrEmpty(_bagStamp))
            {
                _bagStampT += dt;
                if (_bagStampT > (float)PlayStamp.SafeHoldSeconds(_feel))
                    _bagStamp = "";
            }
            Controls.NoteInput();
            var playPause = _phase is Phase.Set or Phase.Flight or Phase.InPlay or Phase.StealThrow or Phase.Result;
            var front = _phase is Phase.Title or Phase.Select or Phase.Field or Phase.Lineup;
            var openedHowTo = PauseMenu.OpenHowTo(_match.Paused, playPause || front, Controls.HowTo, _t);
            var openedPause = PauseMenu.Open(_match.Paused, playPause || _phase is Phase.Select or Phase.Field or Phase.Lineup,
                Controls.CallTime, _t);
            if (openedHowTo || openedPause)
            {
                _match.SetPaused(true);
                _pauseItem = openedHowTo ? 2 : 0;
                _pauseHowTo = openedHowTo;
                _pauseFromHowTo = openedHowTo;
                _pausePage = 0;
                _pauseStick = 0;
                _menuX.Catch(Controls.MenuX);
                _pauseY.Catch(Controls.MenuY);
                _wheelSpin = true;
                _t = 0;
                if (openedHowTo) BookScheme.Open();
            }
            if (_match.Paused)
            {
                if (!openedPause) TickPause();
                _actors.Draw(0f);
                return;
            }
            if (!_gateHold)
            {
                _flow.Tick();
                _atBat.Tick(dt);
                _inPlay.Tick(dt);
            }
            _actors.Draw(dt);
            _park?.Tick(_ball, dt);
            _coach?.Tick(_rig != null ? _rig.Cam : Camera.main);
            _stars?.Set(_match.HomeStars, _match.AwayStars);
            if (HarborKit.Instance != null && HarborKit.Instance.OwnsDiamond)
                HarborKit.Instance.SetScore(_match.AwayScore, _match.HomeScore, _match.Inning);
            _audio?.Tick(dt);
            if (!_freezeCam) _rig.Tick(dt);
        }

        void OnGUI()
        {
            if (_match == null) return;
            if (_deviceRecovery.Active)
            {
                HudView.DeviceRecovery(_deviceRecovery.MissingSeat);
                return;
            }
            if (_phase == Phase.Select)
                HudView.Select(HomeCaptain, AwayCaptain, Pad1Home, _content,
                    _versusWanted, Controls.Pad2.Present);
            else if (_phase == Phase.Field)
                HudView.Field(ParkId, ParkDisplayName(ParkId), Night);
            else if (_phase == Phase.Lineup && _lineup != null)
                TeamSheet.Draw(_match, _lineup);
            if (_match.Paused && _phase is Phase.Select or Phase.Field or Phase.Lineup)
            {
                HudView.Pause(_pauseItem, _pauseHowTo, _pausePage);
                return;
            }
            if (_phase == Phase.Select || _phase == Phase.Field || (_phase == Phase.Lineup && _lineup != null))
                return;
            var ui = _phase switch
            {
                Phase.Title => PhaseUi.Title,
                Phase.Select => PhaseUi.Select,
                Phase.Field => PhaseUi.Field,
                Phase.Lineup => _lineup != null && _lineup.Step == LineupStep.DefenseSetup
                    ? PhaseUi.DefenseSetup
                    : PhaseUi.TeamSetup,
                Phase.GameOver => PhaseUi.GameOver,
                _ => PhaseUi.Set
            };
            var timing = _phase == Phase.Set ? Bounce(_pip)
                : _phase == Phase.Flight ? Mathf.Clamp01(_flight / _pitchDur) : 0f;
            var home = _content.Must(HomeCaptain);
            var awayId = _mode == PlayMode.Challenge
                ? (_campaign != null ? _campaign.NextOpponentId(_content) : Challenge.Start(_content, HomeCaptain).NextOpponentId(_content))
                : AwayCaptain;
            var away = _content.Must(awayId);
            var parkName = ParkDisplayName(_mode == PlayMode.Training ? Training.ParkId : ParkId);
            var banner = _banner;
            var sub = _sub;
            if (TrainingOn && _phase != Phase.Result)
            {
                banner = _coach.Session.Caption;
                sub = _coach.Session.Verb;
            }
            var stamp = _phase == Phase.Result && _last != null && PlayStamp.Shows(_last.Kind)
                ? banner : "";
            if (!string.IsNullOrEmpty(stamp)) banner = "";
            var mutePlay = BroadcastHud.MutePlay(
                _spec != null && _spec.Active, _smash, _freeze)
                || _forceMuteHud || StillCapture.ForceMute;
            if (_match.Paused && _pauseHowTo)
            {
                HudView.Pause(_pauseItem, true, _pausePage);
                return;
            }
            HudView.Draw(_match, ui, parkName, home.Name, away.Name, _mode == PlayMode.Challenge, PitcherExtra(),
                _starPitch || _starSwing, _match.StealOn, ItemHud(), _charge, timing,
                _showTiming && _phase is Phase.Set or Phase.Flight && !TrainingOn, banner, sub, Look.Portrait(HomeCaptain),
                _mode == PlayMode.Training, TrainingOn ? _coach.Session.Progress : null,
                _phase == Phase.Title ? Night : _match.Night,
                HideHelp(), HighlightCaption(), _replaying && _phase == Phase.GameOver, mutePlay,
                LiveSeats.Count, HumanPitches, HumanBats, _starPitch, _starSwing, Pad1Home, _bunt);
            if (!mutePlay && !string.IsNullOrEmpty(_bagStamp))
                HudView.PlayStamp(_bagStamp, _bagStampT,
                    (float)PlayStamp.SafeScale, (float)PlayStamp.SafePopSeconds);
            else if (!string.IsNullOrEmpty(stamp) && !mutePlay)
                HudView.PlayStamp(stamp, _t,
                    (float)PlayStamp.Scale(_last.Kind),
                    (float)PlayStamp.PopSeconds(_last.Kind));
            if (_match.Paused)
            {
                HudView.Pause(_pauseItem, _pauseHowTo, _pausePage);
                return;
            }
            if (_closePlay)
                HudView.ClosePlay(_closeBag, _closeIcon);
            if (!mutePlay && _phase == Phase.InPlay && HumanOwnsThrow)
            {
                var who = PlayFielder();
                if (FieldAssist.ShowYou(true, _glovePos))
                    HudView.ControlDisplay(_glovePos, who != null ? who.Name : "", _jumpT > 0, _diveT > 0);
                if (!string.IsNullOrEmpty(_switchPos) && _switchPos != _glovePos && !(_caught || _buddy))
                {
                    var map = FieldingResolver.Assign(_match.DefenseRoster, _match.Pitcher, _match.Defense.Gloves);
                    map.TryGetValue(_switchPos, out var hint);
                    HudView.SwitchTell(_glovePos, _switchPos, hint != null ? hint.Name : "", false);
                }
            }
            if (!mutePlay && ItemOffered && _itemTarget != null)
                HudView.ItemPointer(_itemTarget.Name);
            if (!mutePlay && _phase == Phase.InPlay && (_caught || _buddy) && !_throwing)
                HudView.BagTell(_match.LivePlay.CommitBagFor(FieldInput()));
            if (!mutePlay && _phase == Phase.StealThrow && _caught && !_throwing && HumanOwnsThrow)
                HudView.BagTell(_match.LivePlay.CommitBagFor(FieldInput()));
            if (_feelDebug)
            {
                var verb = "";
                if (_match.Batter != null && _heroes.TryGetValue(_match.Batter.Id, out var batter) && batter != null)
                    verb = batter.Current.ToString();
                else if (_match.Pitcher != null && _heroes.TryGetValue(_match.Pitcher.Id, out var pitcher) && pitcher != null)
                    verb = pitcher.Current.ToString();
                var hang = _path != null && _path.Length > 0 ? (float)BallFlight.HangTime(_path) : 0f;
                var rest = _path != null && _path.Length > 0 ? (float)BallFlight.RestTime(_path) : 0f;
                FeelOverlay.Draw(
                    _cam != null ? _cam.Shot : "",
                    verb, _charge, hang, rest,
                    _throwBag > 0 ? _throwBag : FieldPad.StickBag,
                    _feelSlow, _freezeCam,
                    _spec != null ? _spec.CurrentEvent : "");
            }
        }

        void TickPause()
        {
            var dt = Time.unscaledDeltaTime;
            if (_pauseStick > 0) _pauseStick -= dt;
            var mouse = Controls.GuiMouse;
            if (_pauseHowTo)
            {
                var n = HowToPlay.Pages.Count;
                var page = HowToPlay.Pages[(_pausePage % n + n) % n];
                var axis = _menuX.Tick(Controls.MenuX, Controls.MenuTapX, dt);
                if (axis != 0)
                    _pausePage = (_pausePage + (axis > 0 ? 1 : n - 1)) % n;
                var wheel = MenuNav.WheelStep(Controls.ScrollY, ref _wheelSpin);
                if (wheel != 0)
                    _pausePage = (_pausePage + (wheel > 0 ? 1 : n - 1)) % n;
                if (Controls.PointerDown)
                {
                    var tab = BookScheme.HitToggle(mouse.x, mouse.y, Screen.width, Screen.height);
                    if (tab is { } kind)
                        BookScheme.Select(kind);
                    else
                    {
                        var nav = HowToPlay.HitNav(mouse.x, mouse.y, Screen.width, Screen.height, page.Lines.Count);
                        if (nav != 0)
                            _pausePage = (_pausePage + (nav < 0 ? n - 1 : 1)) % n;
                    }
                }
                else if (Controls.SouthDown)
                    _pausePage = (_pausePage + 1) % n;
                if (PauseMenu.Dismiss(Controls.EastDown || Controls.CallTime || Controls.MouseBack || Controls.HowTo, _t))
                {
                    _pauseHowTo = false;
                    BookScheme.Close();
                    _t = 0;
                    if (_pauseFromHowTo)
                    {
                        _pauseFromHowTo = false;
                        _match.SetPaused(false);
                    }
                }
                return;
            }
            var hit = PauseMenu.HitItem(mouse.x, mouse.y, Screen.width, Screen.height);
            if (hit >= 0) _pauseItem = hit;
            if (Controls.MenuDown)
            {
                _pauseItem = PauseMenu.Wrap(_pauseItem, 1);
                _pauseStick = 0.22f;
            }
            else if (Controls.MenuUp)
            {
                _pauseItem = PauseMenu.Wrap(_pauseItem, -1);
                _pauseStick = 0.22f;
            }
            else
            {
                var dy = _pauseY.Tick(Controls.MenuY, Controls.MenuTapY, dt);
                if (dy != 0)
                    _pauseItem = PauseMenu.Wrap(_pauseItem, dy > 0 ? -1 : 1);
            }
            if (Controls.SouthDown)
            {
                switch (PauseMenu.At(_pauseItem))
                {
                    case PauseMenu.Item.Resume:
                        _match.SetPaused(false);
                        break;
                    case PauseMenu.Item.Restart:
                        RestartFromPause();
                        break;
                    case PauseMenu.Item.HowToPlay:
                        _pauseHowTo = true;
                        _pausePage = 0;
                        _menuX.Catch(Controls.MenuX);
                        _wheelSpin = true;
                        BookScheme.Open();
                        break;
                    case PauseMenu.Item.Title:
                        PauseToTitle();
                        break;
                }
                return;
            }
            if (PauseMenu.Dismiss(Controls.EastDown || Controls.CallTime || Controls.MouseBack || Controls.HowTo, _t))
                _match.SetPaused(false);
        }

        void RestartFromPause()
        {
            Seed++;
            _match = NewMatch();
            _park.Build(_match.Park, _match.Night, _content.Rules);
            _spec.Build(transform);
            _items.Build(transform);
            _stars?.Build(transform);
            _clip = null;
            _hlPath = null;
            BeginSet();
            _match.SetPaused(false);
        }

        void PauseToTitle()
        {
            _match.SetPaused(false);
            if (TrainingOn) _coach.Stop();
            ReleaseMatchSeats();
            _phase = Phase.Title;
            _t = 0;
            _banner = _sub = "";
            _replaying = false;
            _audio?.CrowdBed(false);
            _cam.Play("title");
        }

        void BindMatchSeats()
        {
            if (_matchSeats.Bound) return;
            var seats = _matchSeats.Bind(SelectedSeats);
            Controls.BeginMatch(seats.BothHuman);
        }

        void ReleaseMatchSeats()
        {
            Controls.EndMatch();
            _matchSeats.Release();
            _deviceRecovery.Complete();
        }

        /// <summary>
        /// Runs before every play-phase director. A missing physical device therefore
        /// freezes SET, pitch flight, live balls, and throws at the same boundary.
        /// </summary>
        bool TickDeviceRecovery()
        {
            if (!_matchSeats.Bound) return false;
            var missing = Controls.MissingMatchSeat(_matchSeats.Seats);
            if (missing != LineupSeat.Cpu)
            {
                _deviceRecovery.WaitFor(missing, _match.Paused);
                _match.SetPaused(true);
                Controls.TryRecoverMatchSeat(missing);
                return true;
            }
            if (!_deviceRecovery.Active) return false;
            var resume = _deviceRecovery.ResumeWhenReady;
            _deviceRecovery.Complete();
            if (resume) _match.SetPaused(false);
            Controls.CatchPlay();
            return true;
        }

        Match NewMatch()
        {
            if (_mode == PlayMode.Training)
            {
                _campaign = null;
                ParkId = Training.ParkId;
                if (_coach == null) _coach = gameObject.AddComponent<TrainingDirector>();
                var lesson = _coach.Session != null ? _coach.Session.Lesson : PracticePick;
                _coach.Begin(_content, lesson);
                return _coach.MakeMatch(_content, Seed);
            }
            if (_mode == PlayMode.Challenge)
            {
                if (_campaign == null || !_campaign.CaptainId.Equals(HomeCaptain, System.StringComparison.OrdinalIgnoreCase))
                    _campaign = Challenge.Start(_content, HomeCaptain);
                return _campaign.MakeMatch(_content, Innings, Seed, night: Night);
            }
            _campaign = null;
            return Match.Exhibition(_content, HomeCaptain, AwayCaptain, Innings, Seed, ParkId, Night);
        }

        string ParkDisplayName(string parkId) =>
            _content != null && _content.Parks.TryGetValue(parkId, out var park) ? park.Name : parkId;

        void NoteTrainingPitch()
        {
            if (_coach == null || _pitch == null) return;
            _coach.OnPitch(_pitch, _match);
        }

        void NoteTrainingSwing()
        {
            if (_coach == null || _swing == null || _last == null) return;
            _coach.OnSwing(_swing, _last.AtBat);
        }

        void Banner()
        {
            if (TrainingOn && _phase != Phase.Result && _last == null)
            {
                _banner = _coach.Session.Caption;
                _sub = _coach.Session.Verb;
                return;
            }
            if (_last != null && _last.Kind == PlayKind.FlyOut &&
                _last.Outcome?.DefensiveFeat == DefensiveFeat.BuddyJump)
                _banner = "BUDDY JUMP";
            else if (_last != null && PlayStamp.Shows(_last.Kind))
            {
                _banner = PlayStamp.Label(_last.Kind, _last.Outcome?.Outs?.Count ?? _last.OutsOnPlay, _last.RunsScored,
                    _last.Swing.Bunt, _catchDive, _catchJump, _last.Outcome != null && _last.Outcome.Error,
                    _last.Outcome != null && _last.Outcome.RunnerResult == RunnerPlayResult.PickedOff);
            }
            else
                _banner = _last != null ? BroadcastHud.Headline(_last.Kind) : (_coach != null && _coach.Session != null ? _coach.Session.Caption : "");
            _sub = _last != null ? _last.Caption : (_coach != null && _coach.Session != null ? _coach.Session.Verb : "");
        }

        void BeginResult()
        {
            ConsiderHighlight();
            _phase = Phase.Result;
            _t = 0;
            _smash = 0;
            _itemFlying = false;
            _items?.Hide();
            _zone.Hide();
            _ring?.Hide();
            // Hits/outs stamp on the live field camera. Next pitch SET is BeginSet (#301).
            if (_last == null || !PlayStamp.HoldsLiveCamera(_last.Kind))
                _cam.Cut(AtBatShots.SetShot(HumanPitches, false, 0, 0, 0, TrainingOn, LiveSeats.Count));
        }

        void TickItem(float dt)
        {
            if (_itemFlying)
            {
                _itemFly += dt;
                if (_itemFly >= ItemView.FlySeconds) _itemFlying = false;
            }
            if (_itemFlying && FieldPad.Attack)
            {
                var dest = ItemTargetWorld();
                var dist = Diamond.Dist(_fx, _fz, dest.x, dest.z);
                if (FieldDash.DestroysItem(true, true, dist, _content.Rules))
                {
                    _match.LivePlay.Apply(LivePlayCommand.SmashItem(_match.LivePlay.Source));
                    _itemFlying = false;
                    _itemId = "";
                    _items?.Hide();
                    _sub = "Item smashed.";
                    return;
                }
            }
            if (!ItemOffered) return;
            var pad = RunPad;
            if (pad.CyclePitch && !pad.Item)
                _itemPick = (_itemPick + 1) % ErrorItems.All.Length;
            AimItem();
            if (!TrainingOn)
                _sub = ErrorItems.All[_itemPick].ToUpperInvariant() + "  ·  stick aim  ·  E throw";
            if (!pad.ItemConfirm || _itemTarget == null) return;
            var id = ErrorItems.All[_itemPick];
            _match.LivePlay.Apply(LivePlayCommand.ApplyItem(id, _itemTarget, _match.LivePlay.Source));
            _itemThrown = true;
            _itemFlying = true;
            _itemFly = 0;
            _itemId = id;
            _audio?.Item(id);
            if (!TrainingOn) _sub = "";
        }

        void AimItem()
        {
            var map = FieldingResolver.Assign(_match.DefenseRoster, _match.Pitcher, _match.Defense.Gloves);
            var play = _cpuField != null && _cpuField.Fielder != null ? _cpuField.Fielder
                : _preview != null ? _preview.Fielder : null;
            var stick = Mathf.Abs(RunPad.StickX) + Mathf.Abs(RunPad.StickY);
            if (stick < 0.28f)
            {
                _itemTarget = play;
                return;
            }
            var x = RunPad.StickX * 160;
            var z = 30 + (RunPad.StickY * 0.5f + 0.5f) * 300;
            var pick = FieldingResolver.NearestGlove(map, x, z, _gloveAt);
            _itemTarget = pick.Fielder;
        }

        Vector3 ItemTargetWorld()
        {
            if (_itemTarget != null && _heroes.TryGetValue(_itemTarget.Id, out var h) && h != null)
                return h.transform.position;
            if (_preview != null)
                return new Vector3((float)_preview.LandingX, 0, (float)_preview.LandingZ);
            return new Vector3(0, 0, 80);
        }

        string ItemHud()
        {
            if (ItemOffered) return ErrorItems.All[_itemPick].ToUpperInvariant();
            if (_itemFlying || _itemThrown) return _itemId.ToUpperInvariant();
            return "";
        }

        static float Bounce(float t)
        {
            var x = t % 2f;
            return x < 1f ? x : 2f - x;
        }

        bool HideHelp() => _hideHelp || PlayerPrefs.GetInt(TrainedKey, 0) == 1;

        string HighlightCaption() => _clip != null ? _clip.Play.Caption : "";

        void HoldBallInGlove()
        {
            if (_throwing) return;
            var map = FieldingResolver.Assign(_match.DefenseRoster, _match.Pitcher, _match.Defense.Gloves);
            if (!map.TryGetValue(_glovePos, out var who) || who == null) return;
            if (!_heroes.TryGetValue(who.Id, out var hero) || hero == null) return;
            var hand = hero.CatchHand;
            if (hand != null) _park.Ball.Hold(hand);
        }

        void HoldPitchInHand()
        {
            var hero = PitcherHero();
            var hand = hero != null ? hero.ThrowHand : null;
            if (hand != null) _park.Ball.Hold(hand);
        }

        void CaptureReleaseFromHand()
        {
            var hero = PitcherHero();
            var hand = hero != null ? hero.ThrowHand : null;
            if (hand != null)
                _relFrom = hand.TransformPoint(0f, 0.1f, 0.52f);
            else
            {
                var rel = PitchFlight.Release(_pitch != null ? _pitch.RubberX : 0);
                _relFrom = new Vector3((float)rel.X, (float)rel.Y, (float)rel.Z);
            }
        }

        HeroActor PitcherHero()
        {
            if (_match?.Pitcher == null) return null;
            _heroes.TryGetValue(_match.Pitcher.Id, out var hero);
            return hero;
        }

        void ConsiderHighlight()
        {
            if (_last == null || _match == null) return;
            var pick = Highlight.Pick(_match.Log);
            if (pick == null) return;
            if (_match.Log.Count == 0 || !ReferenceEquals(_match.Log[_match.Log.Count - 1], pick.Play)) return;
            _clip = pick;
            _hlAt = _ball;
            var fly = _last.Kind is PlayKind.HomeRun or PlayKind.Triple or PlayKind.Double
                or PlayKind.Single or PlayKind.FlyOut or PlayKind.GroundOut or PlayKind.Foul;
            _hlPath = fly ? _path : null;
        }

        void BeginGameOver()
        {
            ConsiderHighlight();
            _phase = Phase.GameOver;
            _t = 0;
            _replaying = _clip != null;
            if (_replaying)
            {
                TickReplay(0);
                _audio?.Swell();
            }
            else
                _cam.Play("replay");
        }

        void TickReplay(float dt)
        {
            _ = dt;
            if (_hlPath != null && _hlPath.Length > 0)
            {
                var hang = (float)BallFlight.HangTime(_hlPath);
                var t = Mathf.Clamp(_t, 0f, Mathf.Max(0.4f, hang));
                var p = BallFlight.PointAt(_hlPath, t);
                _ball = new Vector3((float)p.X, (float)p.Y, (float)p.Z);
                if (_clip != null && _clip.Beat is HighlightBeat.BuddyJump or HighlightBeat.RobbedHomer)
                    _cam.SmashAt(_hlAt.sqrMagnitude > 0.4f ? _hlAt : _ball);
                else if (_clip != null && _clip.Beat == HighlightBeat.StarK)
                    _cam.SmashAt(new Vector3(0.4f, 3.2f, 2f));
                else
                    _cam.SmashAt(_ball);
                return;
            }
            _cam.SmashAt(_hlAt.sqrMagnitude > 0.4f ? _hlAt : new Vector3(0.4f, 3.2f, 2f));
        }

        void OnDisable() => Controls.Silence();
    }
}
