using System;
using System.Collections.Generic;
using System.IO;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed partial class MatchDirector : MonoBehaviour
    {
        // What the menus chose lives in one object the menu directors are handed (FlowChoices, #1042); these names forward to it.
        internal readonly FlowChoices Choices = new FlowChoices();
        public int Seed { get => Choices.Seed; set => Choices.Seed = value; }
        public int Innings { get => Choices.Innings; set => Choices.Innings = value; }
        public string Difficulty { get => Choices.Difficulty; set => Choices.Difficulty = value; }
        public PracticeLesson PracticePick { get => Choices.PracticePick; set => Choices.PracticePick = value; }
        int _pauseItem;
        Controls.Pad _pausePad = Controls.Pad1;
        bool _pauseHowTo;
        bool _pauseFromHowTo;
        internal int _pausePage;
        float _pauseStick;
        MenuNav.Gate _menuX;
        MenuNav.Gate _pauseY;
        public string ParkId { get => Choices.ParkId; set => Choices.ParkId = value; }
        public string HomeCaptain { get => Choices.HomeCaptain; set => Choices.HomeCaptain = value; }
        public string AwayCaptain { get => Choices.AwayCaptain; set => Choices.AwayCaptain = value; }
        public bool Night { get => Choices.Night; set => Choices.Night = value; }
        public bool Hazards { get => Choices.Hazards; set => Choices.Hazards = value; }
        public bool Pad1Home { get => Choices.Pad1Home; set => Choices.Pad1Home = value; }
        internal bool _versusWanted { get => Choices.VersusWanted; set => Choices.VersusWanted = value; }
        internal readonly MatchSeatLifecycle _matchSeats = new MatchSeatLifecycle();
        readonly PursuitSeatDirector _seatStick = new PursuitSeatDirector();
        readonly DeviceSeatRecovery _deviceRecovery = new DeviceSeatRecovery();
        ExhibitionSettings _settings { get => Choices.Settings; set => Choices.Settings = value; }
        internal enum PlayMode { Exhibition, Challenge, Training }
        PlayMode _mode { get => Choices.Mode; set => Choices.Mode = value; }
        Challenge _campaign { get => Choices.Campaign; set => Choices.Campaign = value; }
        internal TrainingDirector _coach;
        // The scene and the play in flight live in two objects every director is handed (#1042); these names forward to them.
        internal readonly MatchScene Scene = new MatchScene();
        internal readonly PlayState Play = new PlayState();
        internal ContentCatalog _content { get => Scene.Content; set => Scene.Content = value; }
        internal Match _match { get => Play.Match; set => Play.Match = value; }

        /// <summary>The table this match plays on (<see cref="PlayState.Rules"/>).</summary>
        RulesTable MatchRules => Play.Rules(_content);
        internal ParkView _park { get => Scene.Park; set => Scene.Park = value; }
        internal CameraRig _rig { get => Scene.Rig; set => Scene.Rig = value; }
        CameraDirector _cam { get => Scene.Cam; set => Scene.Cam = value; }
        internal FeelTable _feel { get => Scene.Feel; set => Scene.Feel = value; }
        InPlayDirector _inPlay;
        ActorDirector _actors;
        SpecialFx _spec { get => Scene.Fx; set => Scene.Fx = value; }
        ItemView _items { get => Scene.Items; set => Scene.Items = value; }
        LandingRing _ring { get => Scene.Ring; set => Scene.Ring = value; }
        internal StrikeZone _zone { get => Scene.Zone; set => Scene.Zone = value; }
        AudioBus _audio { get => Scene.Audio; set => Scene.Audio = value; }
        StarMeter _stars { get => Scene.Stars; set => Scene.Stars = value; }
        HighlightClip _clip;
        Vector3 _hlAt;
        Sample[] _hlPath;
        bool _replaying;
        bool _turntable;
        internal Dictionary<string, HeroActor> _heroes => Scene.Heroes;

        internal enum Phase { Title, Select, Field, Lineup, Set, Flight, InPlay, StealThrow, Result, GameOver }
        internal Phase _phase { get => Play.Phase; set => Play.Phase = value; }
        internal BuntSide _buntSide { get => AtBat.BuntSide; set => AtBat.BuntSide = value; }
        /// <summary>The square clock (§7.3): up while the batter is squared (a bunt trigger held, or the CPU batter's square read at SET), back down when released — the bunt tell the defense reads.</summary>
        internal float _squareSec { get => Play.SquareSec; set => Play.SquareSec = value; }
        /// <summary>The bodies are off their spots on the square (crashing in, or walking back after a release).</summary>
        bool Squared => _squareSec > 0f;
        BuntSide ShowingSide => AtBat.ShowingSide;
        bool SquaredNow => AtBat.SquaredNow;
        internal float _charge { get => Play.Charge; set => Play.Charge = value; }
        internal float _breakX { get => Play.BreakX; set => Play.BreakX = value; }
        float _dash01 { get => Live.Dash01; set => Live.Dash01 = value; }
        internal float _t { get => Play.T; set => Play.T = value; }
        float _pip => Play.Pip;
        internal PitchCommand _pitch { get => Play.Pitch; set => Play.Pitch = value; }
        internal SwingCommand _swing { get => Play.Swing; set => Play.Swing = value; }
        internal PlayEvent _last { get => Play.Last; set => Play.Last = value; }
        internal AtBatResult _pending { get => Play.Pending; set => Play.Pending = value; }
        internal FieldingPreview _preview { get => Play.Preview; set => Play.Preview = value; }
        internal bool _playerFielding { get => Live.PlayerFielding; set => Live.PlayerFielding = value; }
        internal bool _swung { get => Play.Swung; set => Play.Swung = value; }
        internal float _flight { get => Play.Flight; set => Play.Flight = value; }
        internal float _pitchDur { get => Play.PitchDur; set => Play.PitchDur = value; }
        internal bool _pitchAir { get => Play.PitchAir; set => Play.PitchAir = value; }
        internal Vector3 _relFrom { get => Play.ReleaseFrom; set => Play.ReleaseFrom = value; }
        float LiveTime => _match != null ? (float)_match.LivePlay.ElapsedSeconds : 0f;
        internal readonly JuiceDirector _juice = new JuiceDirector();
        float _smash;
        bool _showTiming;
        bool _feelDebug;
        bool _forceMuteHud;
        internal bool _gateHold;
        CardToy _card { get => Scene.Card; set => Scene.Card = value; }
        LogoToy _logo { get => Scene.Logo; set => Scene.Logo = value; }
        ChemToy _chem { get => Scene.Chem; set => Scene.Chem = value; }
        float _feelSlow = 1f;
        bool _freezeCam;
        internal Sample[] _path { get => Play.Path; set => Play.Path = value; }
        internal Vector3 _ball { get => Play.Ball; set => Play.Ball = value; }
        // The live play as the client mirrors it (LiveFieldState); these names forward to it (#1042).
        internal readonly LiveFieldState Live = new LiveFieldState();
        internal double _fx { get => Live.GloveX; set => Live.GloveX = value; } internal double _fz { get => Live.GloveZ; set => Live.GloveZ = value; }
        internal bool _caught { get => Live.Caught; set => Live.Caught = value; } internal bool _buddy { get => Live.Buddy; set => Live.Buddy = value; }
        int _throwBag { get => Live.ThrowBag; set => Live.ThrowBag = value; }
        internal Dictionary<string, (double X, double Z)> _gloveAt => Live.GloveAt;
        IReadOnlyList<FieldBody> _resultBodies { get => Live.ResultBodies; set => Live.ResultBodies = value; }
        internal string _glovePos { get => Live.GlovePos; set => Live.GlovePos = value; }
        string _switchPos { get => Live.SwitchPos; set => Live.SwitchPos = value; }
        string _throwFromPos { get => Live.ThrowFromPos; set => Live.ThrowFromPos = value; }
        string _buddyPos { get => Live.BuddyPos; set => Live.BuddyPos = value; }
        bool _buddyWindow { get => Live.BuddyWindow; set => Live.BuddyWindow = value; }
        float _diveT { get => Live.DiveT; set => Live.DiveT = value; } float _jumpT { get => Live.JumpT; set => Live.JumpT = value; } float _swapLock { get => Live.SwapLock; set => Live.SwapLock = value; }
        internal bool _throwing { get => Live.Throwing; set => Live.Throwing = value; }
        float _throwT { get => Live.ThrowT; set => Live.ThrowT = value; } float _throwDur { get => Live.ThrowDur; set => Live.ThrowDur = value; }
        internal bool _closePlay { get => Live.ClosePlay; set => Live.ClosePlay = value; }
        string _bagStamp { get => Live.BagStamp; set => Live.BagStamp = value; }
        float _bagStampT { get => Live.BagStampT; set => Live.BagStampT = value; }
        float _bagStampHold { get => Live.BagStampHold; set => Live.BagStampHold = value; }
        StampAnchor _bagStampAnchor { get => Live.BagStampAnchor; set => Live.BagStampAnchor = value; }
        bool _closeIcon { get => Live.CloseIcon; set => Live.CloseIcon = value; }
        int _closeBag { get => Live.CloseBag; set => Live.CloseBag = value; }
        string _coverPos { get => Live.CoverPos; set => Live.CoverPos = value; }
        internal float _recoilT { get => Live.RecoilT; set => Live.RecoilT = value; }
        bool _bobbling { get => Live.Bobbling; set => Live.Bobbling = value; }
        internal FieldingResult _cpuField { get => Live.CpuField; set => Live.CpuField = value; }
        ThrowResult _armedThrow { get => Live.ArmedThrow; set => Live.ArmedThrow = value; }
        Vector3 _throwFrom { get => Live.ThrowFrom; set => Live.ThrowFrom = value; } Vector3 _throwTo { get => Live.ThrowTo; set => Live.ThrowTo = value; }
        string _banner, _sub;

        bool TrainingOn => _coach != null && _coach.Session != null;
        // Who sits which seat, and which pad speaks for it (SeatPads, #1042); these names forward to it.
        SeatPads _seatPads;
        SeatPads Pads => _seatPads ??= new SeatPads(Play, _matchSeats, this);
        Seats LiveSeats => Pads.Live;
        internal bool HumanPitches => Pads.HumanPitches;
        bool HumanBats => Pads.HumanBats;
        bool PlayerMustField => Pads.PlayerMustField;
        bool PlayerFields => _playerFielding || PlayerMustField;
        bool HumanFields => Pads.HumanFields;
        bool HumanOwnsThrow => Pads.HumanOwnsThrow;
        Controls.Pad PitchPad => Pads.PitchPad;
        Controls.Pad BatPad => Pads.BatPad;
        Controls.Pad FieldPad => Pads.FieldPad;
        Controls.Pad RunPad => Pads.RunPad;
        void Start()
        {
            Controls.Initialize();
            var data = DataProfile.ShippedRoot;
            // The shipped root, or a trial named in GRAND_SLUGGERS_TRIAL laid over it (#715): the window plays what cli match plays.
            _content = ContentCatalog.Load(DataProfile.Root);
            Rules.RequireDefaultRoot(_content.Root);
            Debug.Log("GS data " + DataProfile.Root.Provenance);
            ArtBinder.Bind(_content.Art);
            _coach = gameObject.AddComponent<TrainingDirector>();
            _match = NewMatch();
            _park = gameObject.AddComponent<ParkView>();
            _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
            _spec = gameObject.AddComponent<SpecialFx>();
            _spec.Build(transform, DiamondGeometry.Of(MatchRules));
            _items = gameObject.AddComponent<ItemView>();
            _items.Build(transform);
            _zone = gameObject.AddComponent<StrikeZone>();
            _zone.Build(transform);
            _ring = gameObject.AddComponent<LandingRing>();
            _ring.Build(transform);
            _audio = gameObject.AddComponent<AudioBus>();
            _audio.Build(data);
            _stars = gameObject.AddComponent<StarMeter>();
            _stars.Build(transform);
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
            _cam.Bind(_rig, _content.Shots, _feel, _park.Kit);
            _cam.Cut("title");
            _inPlay = new InPlayDirector(Scene, Play, Live, this, transform);
            _actors = new ActorDirector(Scene, Play, Live, _inPlay, this, transform);
        }

        void Update()
        {
            Controls.Tick(Time.unscaledDeltaTime, _content.Rules);
            StarAsks.Tick();
            if (_match == null) return;
            // The pursuit stick's seats (#718) bind every frame, recovery and Call time included, on the input clock.
            _seatStick.Tick(_match, _matchSeats.Bound, _phase is Phase.Set or Phase.Result, LiveSeats, TrainingOn);
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
            var held = _juice.Frame(Time.unscaledDeltaTime, _feel, ref dt); // the hit-stop (CH-13): the sim does not step
            _t += dt;
            if (!string.IsNullOrEmpty(_bagStamp))
            {
                _bagStampT += dt;
                var hold = _bagStampHold > 0 ? _bagStampHold : (float)PlayStamp.SafeHoldSeconds(_feel);
                if (_bagStampT > hold)
                    _bagStamp = "";
            }
            var playPause = _phase is Phase.Set or Phase.Flight or Phase.InPlay or Phase.StealThrow or Phase.Result;
            var front = _phase is Phase.Title or Phase.Select or Phase.Field or Phase.Lineup;
            var openedHowTo = PauseMenu.OpenHowTo(_match.Paused, playPause || front, Controls.HowTo, _t);
            var openedPause = PauseMenu.Open(_match.Paused, playPause || front,
                Controls.CallTime, _t);
            if (openedHowTo || openedPause)
            {
                _pausePad = Controls.Pad2.Start || Controls.Pad2.View ? Controls.Pad2 : Controls.Pad1;
                _match.SetPaused(true);
                if (openedPause) GuidedObserve("T-G06", GuidedAction.CallTimeOpened);
                _pauseItem = openedHowTo ? 2 : 0;
                _pauseHowTo = openedHowTo;
                _pauseFromHowTo = openedHowTo;
                _pausePage = 0;
                _pauseStick = 0;
                _menuX.Catch(_pausePad.MenuAxisX);
                _pauseY.Catch(_pausePad.MenuAxisY);
                _t = 0;
                Controls.CatchPlay();
            }
            if (_match.Paused)
            {
                if (!openedPause && !openedHowTo) TickPause();
                _actors.Draw(0f);
                return;
            }
            if (TickTutorialUi(Time.unscaledDeltaTime))
            {
                _actors.Draw(0f);
                if (!_freezeCam) _rig.Tick(dt);
                return;
            }
            if (held && !_gateHold && _match.LivePlay.Active) _juice.Latch(FieldInput(), RunInput());
            else if (!_gateHold && !held)
            {
                Flow.Tick();
                TickAtBat(dt);
                _inPlay.Tick(dt);
            }
            _actors.Draw(dt);
            _park?.Tick(_ball, dt);
            _park?.SetPlayClock(_match != null && _match.LivePlay.Active ? _match.LivePlay.ElapsedSeconds : 0, _match?.LivePlay);
            // East / G that the plate took as the swing cancel is not also the Training skip (PH-13-R1).
            _coach?.Tick(_rig != null ? _rig.Cam : Camera.main, CancelFree(Controls.Pad1));
            _stars?.Set(_match.HomeStars, _match.AwayStars);
            if (_park != null && _park.Kit != null && _park.Kit.OwnsDiamond)
                _park.Kit.SetScore(_match.AwayScore, _match.HomeScore, _match.Inning);
            _audio?.Tick(dt);
            if (!_freezeCam) _rig.Tick(dt);
        }

        void OnGUI()
        {
            if (_match == null) return;
            if (!Controls.Pad1.Present && !Controls.SeatDeviceId(0).HasValue)
            { SetupSheet.ConnectController(); return; }
            if (_deviceRecovery.Active)
            {
                HudView.DeviceRecovery(_deviceRecovery.MissingSeat);
                return;
            }
            if (!_match.Paused && DrawTutorialUi()) return;
            if (_phase == Phase.Select)
                CaptainSheet.Draw(_captains, _content, Controls.Pad2.Present);
            else if (_phase == Phase.Field)
                Front.DrawField();
            else if (_phase == Phase.Lineup && _lineup != null)
            {
                if (_lineup.Step == LineupStep.MatchSettings) SetupSheet.Settings(_settings, _lineup, _match);
                else TeamSheet.Draw(_match, _lineup);
            }
            if (_match.Paused && _phase is Phase.Select or Phase.Field or Phase.Lineup)
            {
                if (_seatStick.ResetOpen) _seatStick.DrawReset();
                else HudView.Pause(_pauseItem, _pauseHowTo, _pausePage, _seatStick.OffersReset, DataProfile.Label);
                return;
            }
            if (_phase == Phase.Select || _phase == Phase.Field || (_phase == Phase.Lineup && _lineup != null))
            {
                HudView.GuidedHint(_guided, GuidedNotice);
                return;
            }
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
                banner = TutorialOn ? HowToPlay.TutorialAttemptTitle(_coach.Tutorial.Lesson.Id, _coach.Tutorial.Successes) : _coach.Session.Caption;
                sub = TutorialOn ? HowToPlay.TutorialAttemptHint(_coach.Tutorial.Lesson.Id, _lessons.Profile, _lessons.PreviousFeedback) : _coach.Session.Verb;
            }
            var stamp = _phase == Phase.Result && _last != null && PlayStamp.ShowsAtTime(_last)
                ? banner : "";
            if (!string.IsNullOrEmpty(stamp)) banner = "";
            var mutePlay = _forceMuteHud || CaptureMuteHud;
            if (_match.Paused && _pauseHowTo)
            {
                HudView.Pause(_pauseItem, true, _pausePage);
                return;
            }
            if (_match.Paused && _seatStick.ResetOpen)
            {
                _seatStick.DrawReset();
                return;
            }
            HudView.Draw(_match, ui, parkName, home.Name, away.Name, _mode == PlayMode.Challenge, AtBat.PitcherExtra(),
                StarAsks.PitchShown || StarAsks.SwingShown, _match.StealOn, Toss.Hud(), _charge, timing,
                _showTiming && _phase is Phase.Set or Phase.Flight && !TrainingOn, banner, sub, Look.Portrait(HomeCaptain),
                _mode == PlayMode.Training, TutorialOn ? HowToPlay.TutorialGoal(_coach.Tutorial.Lesson.Id) : TrainingOn ? _coach.Session.Progress : null,
                _phase == Phase.Title ? Night : _match.Night,
                HighlightCaption(), _replaying && _phase == Phase.GameOver, mutePlay,
                LiveSeats.Count, HumanPitches, HumanBats, StarAsks.PitchShown, StarAsks.SwingShown, Pad1Home, ShowingSide,
                CarnivalFront.ExhibitionTitle,
                StarAsks.Unavailable, Time.unscaledTime - StarAsks.UnavailableAt,
                inPlay: _phase is Phase.InPlay or Phase.StealThrow);
            if (_phase == Phase.Title && !_match.Paused) SetupSheet.TitleMenu(Front.TitleFocus);
            if (!_match.Paused && _phase is Phase.Set or Phase.Flight or Phase.InPlay or Phase.StealThrow)
                SetupSheet.LiveOrders(HumanBats ? RunnerOrderLabel() : null,
                    HumanOwnsThrow && _phase is Phase.InPlay or Phase.StealThrow ? FieldPad.ThrowBag : -1);
            if (!mutePlay && !string.IsNullOrEmpty(_bagStamp))
                HudView.PlayStamp(_bagStamp, _bagStampT,
                    (float)PlayStamp.SafeScale, (float)PlayStamp.SafePopSeconds,
                    BroadcastHud.Stamp(_bagStampAnchor));
            else if (!string.IsNullOrEmpty(stamp) && !mutePlay)
                HudView.PlayStamp(stamp, _t,
                    (float)PlayStamp.Scale(_last.Kind),
                    (float)PlayStamp.PopSeconds(_last.Kind),
                    BroadcastHud.Stamp(StampAnchor.Dirt));
            if (_match.Paused)
            {
                HudView.Pause(_pauseItem, _pauseHowTo, _pausePage, _seatStick.OffersReset, DataProfile.Label);
                return;
            }
            if (_phase == Phase.Set && Swap.Open)
            {
                TeamSheet.DrawPitcherPick(_match, Swap.Pick, PitchPad.Index);
                return;
            }
            if (!mutePlay) _seatStick.DrawTells();
            if (_closePlay)
                HudView.ClosePlay(_closeBag, _closeIcon);
            if (!mutePlay && _phase == Phase.InPlay && HumanOwnsThrow)
            {
                var who = PlayFielder();
                if (FieldAssist.ShowYou(true, _glovePos))
                    HudView.ControlDisplay(_glovePos, who != null ? who.Name : "", _jumpT > 0, _diveT > 0);
                if (!string.IsNullOrEmpty(_switchPos) && _switchPos != _glovePos && !(_caught || _buddy))
                {
                    var map = _match.DefenseMap;
                    map.TryGetValue(_switchPos, out var hint);
                    HudView.SwitchTell(_glovePos, _switchPos, hint != null ? hint.Name : "", false);
                }
            }
            if (!mutePlay && _phase is Phase.InPlay or Phase.StealThrow)
                PursuitSeatDirector.DrawUnready(_match, HumanFields);
            if (!mutePlay && Toss.Offered && Toss.Target != null)
                HudView.ItemPointer(Toss.Target.Name);
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
                var hang = _path != null && _path.Length > 0 ? (float)BallFlight.HangTime(_path, MatchRules) : 0f;
                var rest = _path != null && _path.Length > 0 ? (float)BallFlight.RestTime(_path) : 0f;
                FeelOverlay.Draw(
                    _cam != null ? _cam.Shot : "",
                    verb, _charge, hang, rest,
                    _throwBag > 0 ? _throwBag : FieldPad.StickBag,
                    _feelSlow, _freezeCam,
                    _spec != null ? _spec.CurrentEvent : "");
            }
            HudView.GuidedHint(_guided, GuidedNotice);
        }

        string RunnerOrderLabel()
        {
            var orders = _match.ControllerRunners;
            var label = orders.Label(_match);
            var runner = orders.Selected(_match);
            if (runner == null) return label;
            return BroadcastHud.RunnerOrder(label, runner.Held, runner.DestBag);
        }

        void TickPause()
        {
            var dt = Time.unscaledDeltaTime;
            if (_pauseStick > 0) _pauseStick -= dt;
            if (_seatStick.ResetOpen)
            {
                if (_seatStick.TickReset(PauseMenu.Dismiss(_pausePad.EastDown || Controls.CallTime || Controls.HowTo, _t)) is bool recalibrated)
                    StickResetClosed(recalibrated);
                return;
            }
            if (_pauseHowTo)
            {
                var n = HowToPlay.Pages.Count;
                var shoulder = Controls.Pad1.PageNext || Controls.Pad2.PageNext ? 1 : Controls.Pad1.PagePrevious || Controls.Pad2.PagePrevious ? -1 : 0;
                if (shoulder != 0) _pausePage = (_pausePage + shoulder + n) % n;
                var axis = _menuX.Tick(_pausePad.MenuAxisX, _pausePad.MenuTapX, dt);
                if (axis != 0)
                    _pausePage = (_pausePage + (axis > 0 ? 1 : n - 1)) % n;
                if (_pausePad.SouthDown)
                    _pausePage = (_pausePage + 1) % n;
                if (PauseMenu.Dismiss(_pausePad.EastDown || Controls.CallTime || Controls.HowTo, _t))
                {
                    Controls.CatchPlay();
                    _pauseHowTo = false;
                    _t = 0;
                    if (_pauseFromHowTo)
                    {
                        _pauseFromHowTo = false;
                        _match.SetPaused(false);
                    }
                }
                return;
            }
            var stick = _seatStick.OffersReset;
            if (_pausePad.MenuDown)
            {
                _pauseItem = PauseMenu.Wrap(_pauseItem, 1, stick);
                _pauseStick = 0.22f;
            }
            else if (_pausePad.MenuUp)
            {
                _pauseItem = PauseMenu.Wrap(_pauseItem, -1, stick);
                _pauseStick = 0.22f;
            }
            else
            {
                var dy = _pauseY.Tick(_pausePad.MenuAxisY, _pausePad.MenuTapY, dt);
                if (dy != 0)
                    _pauseItem = PauseMenu.Wrap(_pauseItem, dy > 0 ? -1 : 1, stick);
            }
            if (_pausePad.SouthDown)
            {
                switch (PauseMenu.At(_pauseItem, stick))
                {
                    case PauseMenu.Item.Resume:
                        _match.SetPaused(false);
                        break;
                    case PauseMenu.Item.Restart:
                        if (_phase is Phase.Title or Phase.Select or Phase.Field or Phase.Lineup) PauseToTitle();
                        else RestartFromPause();
                        break;
                    case PauseMenu.Item.HowToPlay:
                        _pauseHowTo = true;
                        GuidedObserve("T-G06", GuidedAction.BookOpened);
                        _pausePage = 0;
                        _menuX.Catch(_pausePad.MenuAxisX);
                        break;
                    case PauseMenu.Item.ResetStick:
                        if (_seatStick.OpenReset()) _t = 0;
                        break;
                    case PauseMenu.Item.ArrangeDefense:
                        OpenDefenseSetup();
                        break;
                    case PauseMenu.Item.Quit:
                        Application.Quit();
                        break;
                    case PauseMenu.Item.Title:
                        PauseToTitle();
                        break;
                }
                Controls.CatchPlay();
                return;
            }
            if (PauseMenu.Dismiss(_pausePad.EastDown || Controls.CallTime || Controls.HowTo, _t))
            { _match.SetPaused(false); Controls.CatchPlay(); }
        }

        void RestartFromPause()
        {
            if (TutorialOn) { PrepareTutorial(_coach.Tutorial.Lesson.Id); return; }
            if (_guided != null && _guided.Phase == TutorialPhase.Attempt && _guided.Lesson.Id != "T-G06")
            {
                var lesson = _guided.Lesson;
                PrepareGuidedTutorial(lesson);
                BeginGuidedAttempt();
                return;
            }
            if (!GuidedAttempt("T-G06")) Seed++;
            _match = NewMatch();
            _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
            _spec.Build(transform, DiamondGeometry.Of(MatchRules));
            _items.Build(transform);
            _stars?.Build(transform);
            _clip = null;
            _hlPath = null;
            BeginSet();
            _match.SetPaused(false);
            GuidedObserve("T-G06", GuidedAction.MatchRestarted);
        }

        void PauseToTitle()
        {
            _match.SetPaused(false);
            if (_guided != null)
            {
                OpenTutorials();
                return;
            }
            _lessons.CloseMenu();
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
            var seats = _matchSeats.Bind(Pads.Selected);
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
            var missing = Controls.MissingMatchSeat(_matchSeats.Bound ? _matchSeats.Seats
                : Seats.One); // Before binding, the captain board owns P2 waiting and allows P1 to go back.
            if (missing != LineupSeat.Cpu)
            {
                _deviceRecovery.WaitFor(missing, _match.Paused);
                _match.SetPaused(true);
                _lessons.Guided.SeatLost(missing);
                Controls.TryRecoverMatchSeat(missing);
                return true;
            }
            if (!_deviceRecovery.Active) return false;
            var resume = _deviceRecovery.ResumeWhenReady;
            var recoveredSeat = _deviceRecovery.MissingSeat;
            _deviceRecovery.Complete();
            if (resume) _match.SetPaused(false);
            Controls.CatchPlay();
            GuidedSeatRecovered(recoveredSeat);
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
            return Match.Exhibition(_content, HomeCaptain, AwayCaptain, Innings, Seed, ParkId, Night, Difficulty, Hazards,
                mercy: _guided != null || _settings.Mercy, stars: _guided != null || _settings.Stars);
        }

        string ParkDisplayName(string parkId) => Front.ParkName(parkId);

        void Banner()
        {
            if (TrainingOn && _phase != Phase.Result && _last == null)
            {
                _banner = _coach.Session.Caption;
                _sub = _coach.Session.Verb;
                return;
            }
            // The stamp is the typed outcome's (§15): live outs and scores already named themselves
            // at the glove or bag. Counts, hits, and homers still ride the Time card. Nothing here
            // reads a mirrored flag or a caption.
            if (_last != null && PlayStamp.ShowsAtTime(_last))
                _banner = PlayStamp.Label(_last);
            else if (_last != null && PlayStamp.Shows(_last.Kind))
                _banner = "";
            else
                _banner = _last != null ? PlayNarrator.Headline(_last.Kind) : (_coach != null && _coach.Session != null ? _coach.Session.Caption : "");
            _sub = _last != null ? _last.Caption : (_coach != null && _coach.Session != null ? _coach.Session.Verb : "");
        }

        void BeginResult()
        {
            ConsiderHighlight();
            _phase = Phase.Result;
            _t = 0;
            _smash = 0;
            Toss.End();
            _zone.Hide();
            _ring?.Hide();
            // Hits/outs stamp on the live field camera. Next pitch SET is BeginSet (#301).
            if (_last == null || !PlayStamp.HoldsLiveCamera(_last.Kind))
                _cam.Cut(AtBat.SetCam.Rest());
        }

        static float Bounce(float t)
        {
            var x = t % 2f;
            return x < 1f ? x : 2f - x;
        }

        string HighlightCaption() => _clip != null ? _clip.Play.Caption : "";

        void HoldBallInGlove()
        {
            if (_throwing) return;
            var map = _match.DefenseMap;
            if (!map.TryGetValue(_glovePos, out var who) || who == null) return;
            if (!_heroes.TryGetValue(who.Id, out var hero) || hero == null) return;
            var hand = hero.CatchHand;
            if (hand != null) _park.Ball.Hold(hand);
        }

        void ConsiderHighlight()
        {
            if (_last == null || _match == null) return;
            var pick = Highlight.Pick(_match.Log);
            if (pick == null) return;
            if (_match.Log.Count == 0 || !ReferenceEquals(_match.Log[_match.Log.Count - 1], pick.Play)) return;
            _clip = pick;
            _hlAt = _ball;
            _hlPath = pick.UsesFlightPath ? _path : null;
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
                var hang = (float)BallFlight.HangTime(_hlPath, MatchRules);
                var t = Mathf.Clamp(_t, 0f, Mathf.Max(0.4f, hang));
                var p = BallFlight.PointAt(_hlPath, t, MatchRules);
                _ball = new Vector3((float)p.X, (float)p.Y, (float)p.Z);
                _cam.SmashAt(_clip?.Aim switch
                {
                    HighlightAim.Moment => _hlAt.sqrMagnitude > 0.4f ? _hlAt : _ball,
                    HighlightAim.Batter => _cam.SmashFallback,
                    _ => _ball
                });
                return;
            }
            _cam.SmashAt(_hlAt.sqrMagnitude > 0.4f ? _hlAt : _cam.SmashFallback);
        }

        void OnDisable() => Controls.Silence();
    }
}
