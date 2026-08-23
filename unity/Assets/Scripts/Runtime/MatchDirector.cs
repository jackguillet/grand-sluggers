using System;
using System.Collections.Generic;
using System.IO;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed class MatchDirector : MonoBehaviour
    {
        public int Seed = 7;
        public int Innings = 3;
        public string ParkId = "harbor-diamond";
        public string HomeCaptain = "rio";
        public string AwayCaptain = "ashlord";
        public bool Night;
        static readonly string[] Parks = { "harbor-diamond", "crystal-rink", "funfair-park", "rooftop-city", "canopy-yard", "ember-keep" };
        float _cHold;
        bool _cNight;
        TeamBuilder _homeDraft;
        TeamBuilder _awayDraft;
        int _lineupSlot;
        int _poolIndex;
        bool _focusPool;
        bool _lineupTouched;
        float _lineupStick;

        enum PlayMode { Exhibition, Challenge, Training }
        PlayMode _mode;
        Challenge _campaign;
        TrainingDirector _coach;
        ContentCatalog _content;
        Match _match;
        ParkView _park;
        CameraRig _rig;
        SpecialFx _spec;
        ItemView _items;
        LandingRing _ring;
        StrikeZone _zone;
        AudioBus _audio;
        StarMeter _stars;
        const string TrainedKey = "gs.trained";
        bool _hideHelp;
        bool _gloved;
        HighlightClip _clip;
        Vector3 _hlAt;
        Sample[] _hlPath;
        float _hlSpray;
        bool _replaying;
        readonly Dictionary<string, HeroActor> _heroes = new Dictionary<string, HeroActor>();
        readonly HashSet<string> _used = new HashSet<string>();

        enum Phase { Title, Lineup, Set, Flight, InPlay, Result, GameOver }
        Phase _phase = Phase.Title;
        readonly string[] _pitches = { "fastball", "changeup", "curve", "slider" };
        int _itemPick;
        Character _itemTarget;
        bool _itemThrown;
        bool _itemFlying;
        float _itemFly;
        string _itemId = "";
        int _pitchIndex;
        bool _star;
        float _charge;
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
        float _hitT;
        float _freeze;
        float _smash;
        bool _showTiming;
        float _aimX, _aimY;
        Sample[] _path;
        Vector3 _ball;
        double _fx, _fz;
        bool _caught, _buddy;
        int _throwBag;
        readonly Dictionary<string, (double X, double Z)> _gloveAt = new Dictionary<string, (double X, double Z)>();
        string _glovePos = "P";
        string _buddyPos = "";
        bool _buddyWindow;
        float _diveT, _jumpT, _swapLock;
        bool _throwing;
        float _throwT, _throwDur;
        int[] _relayBags;
        int _relayI;
        string _coverPos = "";
        float _recoilT;
        bool _bobbling;
        bool _recoilArmed;
        bool _playerBobble;
        FieldingResult _cpuField;
        ThrowResult _armedThrow;
        Character _armedCut;
        Vector3 _throwFrom, _throwTo;
        string _banner, _sub;
        bool _gun;
        float _gunT, _gunDur;
        Vector3 _gunFrom, _gunTo;
        Character _gunRunner;
        int _gunFromBag, _gunToBag;
        bool _gunSafe, _gunPickoff;
        double _gunLead;

        bool TrainingOn => _coach != null && _coach.Session != null;
        bool HumanPitches => TrainingOn ? _coach.PlayerPitches : _match != null && _match.Top;
        bool HumanBats => TrainingOn ? _coach.PlayerBats : _match != null && !_match.Top;
        bool PlayerFields => TrainingOn ? _coach.PlayerFields : HumanPitches;
        bool ItemOffered =>
            HumanBats && _pending != null && _pending.ChemistryItemOffered && !_itemThrown
            && _phase == Phase.InPlay && !_throwing;

        void Start()
        {
            var data = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "data"));
            _content = ContentCatalog.Load(data);
            _coach = gameObject.AddComponent<TrainingDirector>();
            _match = NewMatch();
            _park = gameObject.AddComponent<ParkView>();
            _park.Build(_match.Park, _match.Night);
            _spec = gameObject.AddComponent<SpecialFx>();
            _spec.Build(transform);
            _items = gameObject.AddComponent<ItemView>();
            _items.Build(transform);
            _zone = gameObject.AddComponent<StrikeZone>();
            _zone.Build(transform);
            _ring = gameObject.AddComponent<LandingRing>();
            _ring.Build(transform);
            _audio = gameObject.AddComponent<AudioBus>();
            _audio.Build();
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
            _rig = gameObject.AddComponent<CameraRig>();
            _rig.Bind(cam);
            _rig.Cut(new Vector3(8, 18, -28), new Vector3(0, 4, 70), 46f);
        }

        void Update()
        {
            if (_match == null) return;
            var dt = Time.deltaTime;
            if (Controls.TimingAid) _showTiming = !_showTiming;
            if (_freeze > 0)
            {
                _freeze -= Time.unscaledDeltaTime;
                dt *= 0.12f;
            }
            _t += dt;
            TickGun(dt);
            switch (_phase)
            {
                case Phase.Title: TickTitle(); break;
                case Phase.Lineup:
                    TickLineup();
                    break;
                case Phase.Set: TickSet(dt); break;
                case Phase.Flight: TickFlight(dt); break;
                case Phase.InPlay: TickInPlay(dt); break;
                case Phase.Result:
                    if (_gun) break;
                    if (_t > (_last?.Kind == PlayKind.HomeRun ? 2.4f : 1.35f))
                    {
                        if (TrainingOn && _coach.Session.Finished)
                        {
                            EndTraining();
                            break;
                        }
                        if (_match.Over)
                        {
                            if (TrainingOn)
                            {
                                Seed++;
                                _match = _coach.MakeMatch(_content, Seed);
                                BeginSet();
                                break;
                            }
                            _campaign?.Resolve(_match);
                            BeginGameOver();
                        }
                        else BeginSet();
                    }
                    break;
                case Phase.GameOver:
                    if (_replaying)
                    {
                        TickReplay(dt);
                        if (_t > 2.05f || Controls.SouthDown)
                        {
                            _replaying = false;
                            _t = 0;
                            _rig.Aim(new Vector3(8, 18, -28), new Vector3(0, 4, 70), 46f);
                        }
                        break;
                    }
                    if (Controls.SouthDown && _t > 0.2f) ConfirmGameOver();
                    break;
            }
            DrawActors();
            _park?.Tick(_ball, dt);
            _coach?.Tick(_rig != null ? _rig.Cam : Camera.main);
            _stars?.Set(_match.HomeStars, _match.AwayStars);
            _audio?.Tick(dt);
            _rig.Tick(dt);
        }

        void OnGUI()
        {
            if (_match == null) return;
            if (_phase == Phase.Lineup && _homeDraft != null)
            {
                var taken = new List<string>();
                if (_awayDraft != null)
                    for (var i = 0; i < _awayDraft.Order.Count; i++)
                        taken.Add(_awayDraft.Order[i].Id);
                TeamSheet.Draw(_match, _homeDraft, _homeDraft.Pool(taken), _lineupSlot, _poolIndex, _focusPool);
                return;
            }
            var ui = _phase switch
            {
                Phase.Title => PhaseUi.Title,
                Phase.Lineup => PhaseUi.Lineup,
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
            var parkName = _mode == PlayMode.Training
                ? _content.Parks[Training.ParkId].Name
                : (_content.Parks.TryGetValue(ParkId, out var pk) ? pk.Name : ParkId);
            var banner = _banner;
            var sub = _sub;
            if (TrainingOn && _phase != Phase.Result)
            {
                banner = _coach.Session.Caption;
                sub = _coach.Session.Verb;
            }
            HudView.Draw(_match, ui, parkName, home.Name, away.Name, _mode == PlayMode.Challenge, _pitches, _pitchIndex,
                _star, _match.StealOn, ItemHud(), _charge, timing,
                _showTiming && _phase is Phase.Set or Phase.Flight && !TrainingOn, banner, sub, Look.Portrait(HomeCaptain),
                _mode == PlayMode.Training, TrainingOn ? _coach.Session.Progress : null,
                _phase == Phase.Title ? Night : _match.Night,
                HideHelp(), HighlightCaption(), _replaying && _phase == Phase.GameOver);
        }

        Match NewMatch()
        {
            if (_mode == PlayMode.Training)
            {
                _campaign = null;
                ParkId = Training.ParkId;
                if (_coach == null) _coach = gameObject.AddComponent<TrainingDirector>();
                _coach.Begin(_content);
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
                    if (_mode != PlayMode.Challenge) ParkId = PresetTeams.HomeParkId(HomeCaptain);
                }
                if (Key(KeyCode.D) || Key(KeyCode.RightArrow))
                {
                    HomeCaptain = PresetTeams.NextCaptain(HomeCaptain);
                    if (_mode != PlayMode.Challenge) ParkId = PresetTeams.HomeParkId(HomeCaptain);
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
                Night = !Night;
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
                    }
                    _cHold = 0f;
                    _cNight = false;
                }
            }
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
                OpenLineup();
            }
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
            _rig.Aim(new Vector3(8, 18, -28), new Vector3(0, 4, 70), 46f);
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
            _rig.Aim(new Vector3(8, 18, -28), new Vector3(0, 4, 70), 46f);
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
            _rig.Aim(new Vector3(0, 38, -48), new Vector3(0, 2, 90), 48f);
        }

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
            if (Controls.Steal)
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

        void BeginSet()
        {
            if (TrainingOn && (_match == null || _match.Over))
            {
                Seed++;
                _match = _coach.MakeMatch(_content, Seed);
            }
            _phase = Phase.Set;
            _t = 0;
            _charge = 0;
            _swung = false;
            _swing = null;
            _pitch = null;
            _last = null;
            _pending = null;
            _preview = null;
            _playerFielding = false;
            _cpuField = null;
            _throwing = false;
            _relayBags = null;
            _relayI = 0;
            _coverPos = "";
            _recoilT = 0;
            _bobbling = false;
            _recoilArmed = false;
            _playerBobble = false;
            _diveT = _jumpT = _swapLock = 0;
            _gloveAt.Clear();
            _star = false;
            _itemThrown = false;
            _itemFlying = false;
            _itemFly = 0;
            _itemId = "";
            _itemTarget = null;
            _itemPick = 0;
            _items?.Hide();
            _banner = _sub = "";
            _gun = false;
            _gunRunner = null;
            _ball = new Vector3(0, 5.4f, 60.5f);
            _park.Ball.Place(_ball, "", "fastball", false);
            _aimX = _aimY = 0;
            _smash = 0;
            _gloved = false;
            _audio?.CrowdBed(true);
            if (PlayerFields) _rig.Aim(new Vector3(0, 38, -48), new Vector3(0, 2, 90), 48f);
            else if (HumanPitches) _rig.Aim(new Vector3(-5.4f, 7.0f, 80f), new Vector3(0.3f, 3.4f, 4f), 40f);
            else _rig.Aim(new Vector3(5.8f, 6.2f, -14f), new Vector3(0f, 3.1f, 46f), 38f);
            _zone.Show(true, 0, 0);
        }

        void TickSet(float dt)
        {
            _pip += dt * 1.35f;
            if (Controls.CyclePitch) _pitchIndex = (_pitchIndex + 1) % _pitches.Length;
            if (Controls.SwapPitcher) _match.SwapPitcher();
            if (Controls.NorthDown && (HumanPitches ? _match.CanStarPitch : _match.CanStarSwing)) _star = !_star;
            TickBaserunning(dt);
            if (HumanPitches)
            {
                _aimX = Mathf.Clamp(Controls.StickX, -1, 1);
                _aimY = Mathf.Clamp(Controls.StickY, -1, 1);
                _zone.Show(true, _aimX, _aimY);
                _charge = Controls.Charge ? Mathf.Min(1, _charge + dt / 0.55f) : Mathf.Max(0, _charge - dt * 1.4f);
                if (Controls.SouthDown) Launch(PlayerPitch());
                return;
            }
            if (_t > 0.55f) Launch(_match.CpuPitch());
        }

        PitchCommand PlayerPitch()
        {
            return new PitchCommand(_pitches[_pitchIndex], _charge, 0, _star && _match.CanStarPitch, _aimX, _aimY);
        }

        void Launch(PitchCommand pitch)
        {
            _pitch = pitch;
            var mph = AtBatResolver.PitchSpeedMph(pitch, _match.Pitcher);
            _pitchDur = Mathf.Clamp((float)(Diamond.Mound / (mph * 1.4667)), 0.32f, 0.85f);
            _flight = 0;
            _swung = false;
            _charge = 0;
            _phase = Phase.Flight;
            _t = 0;
            _ball = new Vector3(0, 5.4f, 60.5f);
            _aimX = (float)pitch.AimX;
            _aimY = (float)pitch.AimY;
            _zone.Show(true, _aimX, _aimY);
            _rig.Punch(pitch.Star ? 8f : 4f);
            _spec.ResetDecoy();
            _hideHelp = true;
            if (pitch.Star) _audio?.CaptainVo(_match.Pitcher.Id);
        }

        void TickFlight(float dt)
        {
            _flight += dt;
            var u = Mathf.Clamp01(_flight / _pitchDur);
            var p = PitchFlight.Point(_pitch.Type, u, _pitch.AimX, _pitch.AimY);
            var x = (float)p.X;
            var y = (float)p.Y;
            var z = (float)p.Z;
            if (_pitch.Star)
            {
                var id = _match.Pitcher.StarPitch;
                if (id == "heatball") x += Mathf.Sin(u * 18f) * 0.4f;
                else if (id == "prismball") x += Mathf.Sin(u * 24f) * 1.8f;
                else if (id == "charmball") x += Mathf.Sin(u * 9f) * 0.7f;
                else if (id == "phonyball") x += u > 0.55f ? 2.4f : -0.5f;
                else if (id == "caskball") y += 0.55f * u;
            }
            _ball = new Vector3(x, y, z);
            TickBaserunning(dt);
            if (HumanBats)
            {
                if (Controls.NorthDown && _match.CanStarSwing) _star = !_star;
                if (Controls.Charge) _charge = Mathf.Min(1, _charge + dt / 0.45f);
                if (Controls.SouthDown && !_swung)
                {
                    _swung = true;
                    _swing = new SwingCommand(true, _charge, (_flight - _pitchDur) * 60f, _star && _match.CanStarSwing, Controls.StickX * 18f, Controls.WestHeld, Controls.StickY);
                }
            }
            if (u < 1) return;
            _swing ??= HumanBats
                ? new SwingCommand(false, _charge, 12, false)
                : _match.CpuSwing(_pitch, AtBatResolver.PitchInZone(_pitch, _match.Pitcher.Stats.Pitch));
            Resolve();
        }

        void Resolve()
        {
            var stealRunner = _match.LeadRunner;
            var stealBag = _match.LeadBag;
            var stealLead = _match.Lead01;
            if (!_match.BeginAtBat(_pitch, _swing, out var hit, out var finished))
            {
                _last = finished;
                NoteTrainingPitch();
                NoteTrainingSwing();
                Banner();
                if (finished != null && stealRunner != null &&
                    (finished.Kind == PlayKind.StolenBase || finished.Kind == PlayKind.CaughtStealing))
                {
                    StartStealGun(stealRunner, stealBag, stealLead, finished);
                    return;
                }
                if (finished != null && finished.Kind == PlayKind.Foul && hit.ExitVeloMph > 1)
                    StartFly(hit);
                else
                    BeginResult();
                return;
            }
            NoteTrainingPitch();
            if (HumanBats) _coach?.OnSwing(_swing, hit);
            _pending = hit;
            _preview = _match.PreviewHit(hit);
            _cpuField = null;
            _playerFielding = PlayerFields;
            _itemThrown = false;
            _itemFlying = false;
            _itemFly = 0;
            _itemId = "";
            _itemPick = 0;
            _itemTarget = _preview != null ? _preview.Fielder : null;
            if (!_playerFielding)
            {
                _cpuField = _match.ResolveFielding(hit, _preview);
                if (!HumanBats)
                    _cpuField = _match.ApplyOffenseItem(hit, _cpuField, null);
            }
            InitGloves();
            _caught = _buddy = false;
            _throwBag = 0;
            _throwing = false;
            _relayBags = null;
            _relayI = 0;
            _coverPos = "";
            _recoilT = 0;
            _bobbling = false;
            _recoilArmed = false;
            _playerBobble = false;
            _armedThrow = null;
            _armedCut = null;
            _park.Ball.Release();
            _diveT = _jumpT = 0;
            _buddyWindow = false;
            _buddyPos = "";
            StartFly(hit);
        }

        void InitGloves()
        {
            _gloveAt.Clear();
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            foreach (var kv in map)
                _gloveAt[kv.Key] = Diamond.Positions[kv.Key];
            _swapLock = 0;
            if (_preview == null)
            {
                _glovePos = "P";
                _fx = Diamond.Rubber.X;
                _fz = Diamond.Rubber.Z;
                return;
            }
            (Character who, string pos) pick;
            if (_playerFielding && !FieldingResolver.BuddyJumpOffered(_preview))
                pick = FieldingResolver.NearestGlove(map, _preview.LandingX, _preview.LandingZ, _gloveAt);
            else
                pick = ( _preview.Fielder, _preview.Position );
            _glovePos = pick.pos;
            var at = _gloveAt[_glovePos];
            _fx = at.X;
            _fz = at.Z;
        }

        void StartFly(AtBatResult hit)
        {
            var list = BallFlight.Trajectory(hit.ExitVeloMph, hit.LaunchDeg, _match.Park.WindMph);
            _path = new Sample[list.Count];
            for (var i = 0; i < list.Count; i++) _path[i] = list[i];
            _hitT = 0;
            _phase = Phase.InPlay;
            _t = 0;
            if (hit.HomeRun && _match.Night)
                _park.BurstFireworks(_ball);
            _gloved = false;
            if (hit.Quality != ContactQuality.Miss) _audio?.Bat(hit.Quality);
            if (hit.StarSwingUsed != null) _audio?.CaptainVo(_match.Batter.Id);
            if (hit.Quality == ContactQuality.Perfect || hit.StarSwingUsed != null)
            {
                _freeze = 0.32f;
                _smash = 0.18f;
                _rig.Smash(_ball);
            }
            else if (hit.Quality == ContactQuality.Solid)
            {
                _freeze = 0.08f;
                _rig.Punch(8f);
            }
            AimDiamond(hit);
        }

        void AimDiamond(AtBatResult hit)
        {
            var grounder = hit.LaunchDeg < 14;
            var look = _ball.sqrMagnitude > 1 ? _ball : new Vector3((float)(_preview?.LandingX ?? 0), 3f, (float)(_preview?.LandingZ ?? 80));
            if (grounder && hit.SprayDeg < -8)
                _rig.Aim(new Vector3(-32f, 14f, 36f), look, 46f);
            else if (grounder)
                _rig.Aim(new Vector3(38f, 14f, 22f), look, 46f);
            else
                _rig.Aim(new Vector3(8f, 26f, -18f), look + new Vector3(0, 3, 10), 50f);
        }

        void TickInPlay(float dt)
        {
            _hitT += dt;
            if (_path == null || _path.Length == 0) { BeginResult(); return; }
            var spray = _pending != null ? _pending.SprayDeg : _last.AtBat.SprayDeg;
            if (_bobbling)
            {
                var away = new Vector3((float)_fx, 0f, (float)_fz);
                var dir = _ball - away;
                if (dir.sqrMagnitude < 0.4f) dir = Vector3.forward;
                dir.y = 0;
                _ball += dir.normalized * (12f * dt);
                _ball.y = Mathf.Max(0f, _ball.y - 16f * dt);
            }
            else if ((_caught || _buddy) && !_throwing)
                _ball = new Vector3((float)_fx, _buddy ? 6.4f : 2.2f, (float)_fz);
            else if (!_throwing)
            {
                var p = BallFlight.PointAt(_path, spray, _hitT);
                _ball = new Vector3((float)p.X, (float)p.Y, (float)p.Z);
            }
            if (_smash > 0) _smash -= dt;
            else if (_throwing)
                _rig.Aim(_throwTo + new Vector3(14f, 11f, -18f), _throwTo, 48f);
            else if (BuddySet && _hitT > 0.7f)
            {
                var plant = WallPlant(_preview);
                _rig.Aim(
                    new Vector3((float)plant.X + 24f, 15f, (float)plant.Z - 34f),
                    new Vector3((float)plant.X, 5.5f, (float)plant.Z),
                    42f);
            }
            else if (_pending != null)
                AimDiamond(_pending);
            else
                _rig.Aim(_ball + new Vector3(14, 11, -20), _ball + new Vector3(0, 2, 6), 50f);

            if (_ring != null && _preview != null && !_preview.Grounder && !_caught && !_buddy)
            {
                var hang = BallFlight.HangTime(_path);
                var red = _hitT >= hang - 0.48f && _hitT <= hang + 0.14f;
                _ring.Show(_preview.LandingX, _preview.LandingZ, (float)_preview.CatchRadius, red);
            }
            else
                _ring?.Hide();

            if (_diveT > 0) _diveT -= dt;
            if (_jumpT > 0) _jumpT -= dt;
            if (_swapLock > 0) _swapLock -= dt;

            TickBuddyPartner(dt);
            TickItem(dt);

            if (_recoilT > 0 && !_throwing)
            {
                if (!_bobbling && _caught)
                {
                    var away = new Vector3((float)_fx - _ball.x, 0f, (float)_fz - _ball.z);
                    if (away.sqrMagnitude > 0.2f)
                    {
                        away.Normalize();
                        _fx += away.x * 14f * dt;
                        _fz += away.z * 14f * dt;
                        _gloveAt[_glovePos] = (_fx, _fz);
                    }
                }
                _recoilT -= dt;
                if (_recoilT <= 0 && _bobbling)
                {
                    CommitInPlay();
                    return;
                }
                if (_recoilT > 0) return;
            }

            if (_throwing)
            {
                _throwT += dt;
                var u = Mathf.Clamp01(_throwT / Mathf.Max(0.05f, _throwDur));
                var arc = _armedThrow != null && _armedThrow.Relation == Chemistry.Good ? 5.2f
                    : _armedThrow != null && _armedThrow.Relation == Chemistry.Bad ? 1.6f : 3.2f;
                _ball = Vector3.Lerp(_throwFrom, _throwTo, u);
                _ball.y += Mathf.Sin(u * Mathf.PI) * arc;
                if (_throwT >= _throwDur && !_itemFlying)
                {
                    if (AdvanceRelay()) return;
                    CommitInPlay();
                }
                return;
            }

            if (_playerFielding && _preview != null && _pending != null)
            {
                TickPlayerField(dt);
                return;
            }

            if (_preview != null && _cpuField != null && _pending != null)
            {
                TickCpuField(dt);
                return;
            }

            var rest = BallFlight.RestTime(_path);
            var done = _hitT >= rest + 0.2f;
            if (_last?.Kind == PlayKind.HomeRun && _hitT > 2.4f) done = true;
            if (done && !_itemFlying) BeginResult();
        }

        void TickPlayerField(float dt)
        {
            var pre = _preview;
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            var hang = BallFlight.HangTime(_path);
            var rest = BallFlight.RestTime(_path);
            var chasing = !_caught && !_buddy && (pre.Grounder ? _hitT < rest : _hitT < hang);
            var buddyOn = FieldingResolver.BuddyJumpOffered(pre);
            var plant = buddyOn ? WallPlant(pre) : (X: pre.LandingX, Z: pre.LandingZ);
            _buddyWindow = buddyOn && _hitT >= hang - 0.48f && _hitT <= hang + 0.12f;

            if (Controls.SwapPitcher && !buddyOn)
            {
                CycleGlove(map);
                _swapLock = 0.7f;
            }

            var stick = Mathf.Abs(Controls.StickX) + Mathf.Abs(Controls.StickY);
            if (chasing && _swapLock <= 0 && stick < 0.35f)
            {
                if (buddyOn)
                {
                    var speed = (18 + pre.Fielder.Stats.Run * 1.8) * (pre.Frozen ? 0.4 : 1);
                    var dx = plant.X - _fx;
                    var dz = plant.Z - _fz;
                    var dist = Math.Sqrt(dx * dx + dz * dz);
                    if (dist > 8)
                    {
                        var step = Math.Min(dist, speed * dt);
                        _fx += dx / dist * step;
                        _fz += dz / dist * step;
                    }
                    _gloveAt[_glovePos] = (_fx, _fz);
                }
                else AutoGlove(map);
            }

            if (chasing && map.TryGetValue(_glovePos, out var glove) && stick >= 0.35f)
            {
                var speed = (18 + glove.Stats.Run * 1.8) * (pre.Frozen ? 0.4 : 1);
                _fx += Controls.StickX * speed * dt;
                _fz += Controls.StickY * speed * dt;
                _gloveAt[_glovePos] = (_fx, _fz);
            }

            if (Controls.WestDown)
            {
                _jumpT = buddyOn ? 0.7f : 0.55f;
                if (buddyOn)
                {
                    var near = Diamond.Dist(_fx, _fz, plant.X, plant.Z) < 26;
                    if (_buddyWindow && near && _ball.y > 4.5f)
                    {
                        _buddy = true;
                        CatchGlove();
                        _fx = plant.X;
                        _fz = plant.Z;
                        _gloveAt[_glovePos] = (_fx, _fz);
                    }
                }
            }
            if (Controls.EastDown) _diveT = 0.5f;

            var window = CatchWindow(map);
            var d = Diamond.Dist(_fx, _fz, _ball.x, _ball.z);
            if (Controls.SouthDown && d < window) { CatchGlove(); ArmRecoil(); }
            if (_diveT > 0 && d < window && _ball.y < 7.5f) { CatchGlove(); ArmRecoil(); }
            if (!buddyOn && _jumpT > 0 && d < window && _ball.y > 2.2f) { CatchGlove(); ArmRecoil(); }

            ReadThrowBag(!chasing || _caught || _buddy);

            if (_buddy)
                _ball = new Vector3((float)_fx, 6.4f + (_jumpT > 0 ? 2.2f : 0f), (float)_fz);

            if (buddyOn && !_buddy && _hitT < hang + 0.18f) return;
            if (pre.Grounder)
            {
                if (!_caught && _hitT < rest) return;
            }
            else if (_hitT < hang) return;
            if ((_caught || _buddy) && _throwBag == 0 && _hitT < hang + 0.85f)
                return;
            BeginPlayerThrowOrCommit(map);
        }

        void TickCpuField(float dt)
        {
            var hang = BallFlight.HangTime(_path);
            var rest = BallFlight.RestTime(_path);
            var grounder = _preview.Grounder;
            var spray = _pending != null ? _pending.SprayDeg : 0;
            if (grounder && _path != null && !_caught)
            {
                var live = BallFlight.PointAt(_path, spray, _hitT);
                var speed = (21 + _preview.Fielder.Stats.Run * 1.9) * (_preview.Frozen ? 0.45 : 1);
                var dx = live.X - _fx;
                var dz = live.Z - _fz;
                var dist = Math.Sqrt(dx * dx + dz * dz);
                if (dist > 0.35)
                {
                    var step = Math.Min(dist, speed * dt);
                    _fx += dx / dist * step;
                    _fz += dz / dist * step;
                }
            }
            else if (!_caught)
            {
                var u = Mathf.Clamp01(_hitT / Mathf.Max(0.2f, (float)hang));
                var start = Diamond.Positions[_preview.Position];
                _fx = start.X + (_preview.LandingX - start.X) * u;
                _fz = start.Z + (_preview.LandingZ - start.Z) * u;
            }
            _glovePos = _preview.Position;
            _gloveAt[_glovePos] = (_fx, _fz);
            var outPlay = _cpuField.Kind is PlayKind.FlyOut or PlayKind.GroundOut;
            var reached = outPlay || _cpuField.Bobble;
            if (reached && (grounder ? _hitT >= hang && _ball.y < 3.2f : _hitT >= hang - 0.18f))
            {
                CatchGlove();
                ArmRecoil();
            }
            if (!grounder && _hitT < hang) return;
            if (grounder && !_caught && _hitT < rest) return;
            if (_cpuField.Bobble && _caught)
                return;
            if (outPlay && grounder)
            {
                if (StartGroundRelays()) return;
            }
            else if (outPlay && _cpuField.Throw != null)
            {
                _armedThrow = _cpuField.Throw;
                _armedCut = _cpuField.Cutoff;
                BeginThrow(_cpuField.Throw, _cpuField.Cutoff, 0);
                return;
            }
            if (_cpuField.Kind == PlayKind.HomeRun && _hitT < 2.4f) return;
            if (!grounder && _hitT < hang + 0.35f) return;
            if (_itemFlying) return;
            CommitInPlay();
        }

        void AutoGlove(Dictionary<string, Character> map)
        {
            var pick = FieldingResolver.NearestGlove(map, _ball.x, _ball.z, _gloveAt);
            if (pick.Pos == _glovePos) return;
            _gloveAt[_glovePos] = (_fx, _fz);
            _glovePos = pick.Pos;
            var at = _gloveAt[_glovePos];
            _fx = at.X;
            _fz = at.Z;
        }

        void CycleGlove(Dictionary<string, Character> map)
        {
            var order = Diamond.Order;
            var i = 0;
            for (; i < order.Length; i++)
                if (order[i] == _glovePos) break;
            var next = order[(i + 1) % order.Length];
            if (!map.ContainsKey(next)) next = "P";
            _gloveAt[_glovePos] = (_fx, _fz);
            _glovePos = next;
            var at = _gloveAt[_glovePos];
            _fx = at.X;
            _fz = at.Z;
        }

        double CatchWindow(Dictionary<string, Character> map)
        {
            var who = map.TryGetValue(_glovePos, out var c) ? c : _preview.Fielder;
            var radius = 10 + who.Stats.Field * 0.6 + FieldAbilities.CatchBonus(who);
            return FieldingResolver.CatchWindowFt(radius, _diveT > 0, _jumpT > 0);
        }

        bool BuddySet => _preview != null && FieldingResolver.BuddyJumpOffered(_preview);

        static (double X, double Z) WallPlant(FieldingPreview pre)
        {
            var x = pre.LandingX;
            var z = pre.LandingZ;
            var dist = Math.Sqrt(x * x + z * z);
            if (dist < 1) return (x, z);
            var pull = 10.0 / dist;
            return (x * (1 - pull), z * (1 - pull));
        }

        static string PosOf(Dictionary<string, Character> map, Character who)
        {
            foreach (var kv in map)
                if (kv.Value.Id == who.Id) return kv.Key;
            return "";
        }

        void TickBuddyPartner(float dt)
        {
            _ = dt;
            if (_preview == null || _path == null || !FieldingResolver.BuddyJumpOffered(_preview))
            {
                _buddyWindow = false;
                return;
            }
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            _buddyPos = PosOf(map, _preview.Buddy);
            if (string.IsNullOrEmpty(_buddyPos)) return;
            var hang = BallFlight.HangTime(_path);
            var plant = WallPlant(_preview);
            var u = Mathf.Clamp01(_hitT / Mathf.Max(0.25f, (float)hang - 0.4f));
            var start = Diamond.Positions[_buddyPos];
            _gloveAt[_buddyPos] = (start.X + (plant.X - start.X) * u, start.Z + (plant.Z - start.Z) * u);
            if (!_playerFielding)
                _buddyWindow = _hitT >= hang - 0.48f && _hitT <= hang + 0.12f;
        }

        void ReadThrowBag(bool stickOk)
        {
            if (Controls.ThrowBag > 0) _throwBag = Controls.ThrowBag;
            else if (stickOk && Controls.StickBag > 0) _throwBag = Controls.StickBag;
        }

        void BeginPlayerThrowOrCommit(Dictionary<string, Character> map)
        {
            if (!(_caught || _buddy) || _throwBag <= 0)
            {
                CommitInPlay();
                return;
            }
            var key = _throwBag == 1 ? "1B" : _throwBag == 2 ? "2B" : _throwBag == 3 ? "3B" : "C";
            map.TryGetValue(key, out var cut);
            var from = map.TryGetValue(_glovePos, out var glove) ? glove : _preview.Fielder;
            ThrowResult thr = null;
            if (cut != null) thr = _match.ThrowBetween(from, cut);
            _armedThrow = thr;
            _armedCut = cut;
            if (thr != null)
            {
                if (_preview != null && _preview.Grounder && _throwBag == 2 && _match.First != null && _pending != null)
                {
                    var beats = InPlay.BatterBeatsThrow(_match.Batter, _pending, BuildPlayerResult());
                    _relayBags = beats ? new[] { 2 } : new[] { 2, 1 };
                }
                else
                    _relayBags = new[] { _throwBag };
                _relayI = 0;
                BeginThrow(thr, cut, _throwBag);
            }
            else CommitInPlay();
        }

        void BeginThrow(ThrowResult thr, Character cut, int bag)
        {
            _park.Ball.Release();
            _throwing = true;
            _throwT = 0;
            _throwBag = bag;
            _coverPos = CoverKey(bag);
            var to = cut != null && _heroes.TryGetValue(cut.Id, out var ch) && ch != null
                ? ch.transform.position
                : BagWorld(bag);
            _throwFrom = new Vector3((float)_fx, 3.2f, (float)_fz);
            _throwTo = to + Vector3.up * 1.2f;
            _spec.ArmThrow(_throwFrom, to, thr);
            _throwDur = Mathf.Max(0.28f, _spec.ThrowSeconds);
            _audio?.ThrowPop();
        }

        static string CoverKey(int bag) =>
            bag == 1 ? "1B" : bag == 2 ? "2B" : bag == 3 ? "3B" : bag == 4 ? "C" : "";

        bool StartGroundRelays()
        {
            if (_relayBags != null) return false;
            if (_pending == null || _cpuField == null) return false;
            var beats = InPlay.BatterBeatsThrow(_match.Batter, _pending, _cpuField);
            _relayBags = InPlay.GroundThrowBags(_match.First != null, beats);
            _relayI = 0;
            if (_relayBags.Length == 0) return false;
            return FireRelay();
        }

        bool FireRelay()
        {
            if (_relayBags == null || _relayI >= _relayBags.Length) return false;
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            var bag = _relayBags[_relayI];
            var key = CoverKey(bag);
            map.TryGetValue(key, out var cut);
            var from = map.TryGetValue(_glovePos, out var glove) ? glove : _preview.Fielder;
            ThrowResult thr = _cpuField != null ? _cpuField.Throw : null;
            if (thr == null && from != null && cut != null)
                thr = _match.ThrowBetween(from, cut);
            if (thr == null && _cpuField != null)
                thr = _cpuField.Throw;
            if (cut == null && thr == null) return false;
            _armedThrow = thr;
            _armedCut = cut;
            BeginThrow(thr ?? new ThrowResult(Chemistry.Neutral, 1.0, false), cut, bag);
            return true;
        }

        bool AdvanceRelay()
        {
            if (_relayBags == null || _relayI + 1 >= _relayBags.Length) return false;
            var bag = _relayBags[_relayI];
            var dest = BagWorld(bag);
            _fx = dest.x;
            _fz = dest.z;
            _glovePos = CoverKey(bag);
            if (!string.IsNullOrEmpty(_glovePos))
                _gloveAt[_glovePos] = (_fx, _fz);
            _throwing = false;
            CatchGlove();
            _relayI++;
            return FireRelay();
        }

        static Vector3 BagWorld(int bag)
        {
            var p = bag == 1 ? Diamond.First : bag == 2 ? Diamond.Second : bag == 3 ? Diamond.Third : Diamond.Home;
            return new Vector3((float)p.X, 1.2f, (float)p.Z);
        }

        void CommitInPlay()
        {
            if (_playerFielding && _pending != null && _preview != null)
            {
                var result = BuildPlayerResult();
                // Player already resolved catch/throw. CPU bananas must play visibly
                // during InPlay (TickItem) — never a silent 40% roll after the glove.
                _last = _match.FinishAtBat(_pitch, _swing, _pending, result);
                _coach?.OnField(result, _match);
            }
            else if (_cpuField != null && _pending != null)
            {
                _last = _match.FinishAtBat(_pitch, _swing, _pending, _cpuField);
                _coach?.OnField(_cpuField, _match);
            }
            Banner();
            if (_last != null && _last.Kind is PlayKind.HomeRun or PlayKind.Triple or PlayKind.Double)
                _audio?.Swell();
            _playerFielding = false;
            _pending = null;
            _cpuField = null;
            _throwing = false;
            _relayBags = null;
            _coverPos = "";
            _bobbling = false;
            _recoilT = 0;
            _park.Ball.Release();
            BeginResult();
        }

        FieldingResult BuildPlayerResult()
        {
            var pre = _preview;
            var hit = _pending;
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            var from = map.TryGetValue(_glovePos, out var glove) ? glove : pre.Fielder;
            Character cut = _armedCut;
            ThrowResult thr = _armedThrow;
            var bag = _throwBag > 0 ? _throwBag : Controls.ThrowBag;
            if (thr == null && (_caught || _buddy) && bag > 0)
            {
                var key = bag == 1 ? "1B" : bag == 2 ? "2B" : bag == 3 ? "3B" : "C";
                map.TryGetValue(key, out cut);
                if (cut != null) thr = _match.ThrowBetween(from, cut);
            }
            if (_buddy || _caught)
            {
                if (_bobbling || _playerBobble)
                    return new FieldingResult(PlayKind.Single, from, cut, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace, thr, pre.Buddy, Bobble: true);
                var kind = pre.Grounder ? PlayKind.GroundOut : PlayKind.FlyOut;
                var knock = pre.Grounder && hit != null ? InPlay.KnockbackSec(InPlay.Energy(hit), from) : 0;
                return new FieldingResult(kind, from, cut, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace, thr, pre.Buddy, KnockbackSec: knock);
            }
            if (pre.HomeRunLikely)
                return new FieldingResult(PlayKind.HomeRun, from, null, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace, Buddy: pre.Buddy);
            var extra = hit.CarryFt >= 250 ? PlayKind.Double : PlayKind.Single;
            return new FieldingResult(extra, from, null, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace, Buddy: pre.Buddy);
        }

        void DrawActors()
        {
            _used.Clear();
            var defense = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            var litId = "";
            if (_phase == Phase.InPlay && defense.TryGetValue(_glovePos, out var litWho))
                litId = litWho.Id;
            var itemLit = ItemOffered && _itemTarget != null ? _itemTarget.Id : "";
            foreach (var kv in defense)
            {
                var who = kv.Value;
                var pos = Diamond.Positions[kv.Key];
                double x = pos.X, z = pos.Z;
                if (_gloveAt.TryGetValue(kv.Key, out var live))
                {
                    x = live.X;
                    z = live.Z;
                }
                var pose = HeroActor.Pose.Idle;
                var buddyPartner = _phase == Phase.InPlay && BuddySet && _preview.Buddy != null && who.Id == _preview.Buddy.Id;
                var highlighted = _phase == Phase.InPlay && (who.Id == itemLit || (who.Id == litId && !HumanBats) || (buddyPartner && !_buddy));
                if (highlighted && !buddyPartner)
                {
                    x = _fx;
                    z = _fz;
                    if (_throwing) pose = HeroActor.Pose.Throw;
                    else if (_bobbling) pose = HeroActor.Pose.Miss;
                    else if (_recoilT > 0) pose = HeroActor.Pose.Dive;
                    else if (_jumpT > 0) pose = who.FieldAbility == "clamber" ? HeroActor.Pose.Clamber : HeroActor.Pose.Jump;
                    else if (_caught && _preview != null && _preview.Grounder) pose = HeroActor.Pose.Crouch;
                    else if (_caught || _buddy) pose = HeroActor.Pose.Catch;
                    else if (_diveT > 0) pose = HeroActor.Pose.Dive;
                    else if (_preview != null) pose = FieldPose(who, _preview, false);
                    else pose = HeroActor.Pose.Field;
                }
                else if (buddyPartner)
                {
                    var atWall = Diamond.Dist(x, z, WallPlant(_preview).X, WallPlant(_preview).Z) < 18;
                    if (_throwing) pose = HeroActor.Pose.Field;
                    else if (atWall) pose = HeroActor.Pose.Crouch;
                    else pose = HeroActor.Pose.Field;
                }
                else if (_phase == Phase.InPlay && _preview != null && who.Id == _preview.Fielder.Id)
                {
                    if (_buddy && _jumpT > 0)
                        pose = who.FieldAbility == "clamber" ? HeroActor.Pose.Clamber : HeroActor.Pose.Jump;
                    else
                        pose = FieldPose(who, _preview, _caught || _buddy);
                }
                if (kv.Key == "P" && _phase is Phase.Set or Phase.Flight)
                    pose = _phase == Phase.Flight ? HeroActor.Pose.ThrowPitch : HeroActor.Pose.ChargePitch;
                if (_throwing && !string.IsNullOrEmpty(_coverPos) && kv.Key == _coverPos)
                    pose = HeroActor.Pose.Catch;
                if (_gun && kv.Key == "C" && !_gunPickoff) pose = HeroActor.Pose.Throw;
                if (_gun && kv.Key == "P" && _gunPickoff) pose = HeroActor.Pose.Throw;
                var hero = Hero(who);
                hero.SetGrow(who.FieldAbility == "grow" && highlighted);
                hero.SetHighlight(highlighted);
                if (_pending != null && _pending.StarSwingUsed == "heart-swing" && highlighted)
                    pose = HeroActor.Pose.Charm;
                var pType = _pitch != null ? _pitch.Type : _pitches[_pitchIndex];
                hero.SetPose(pose, kv.Key == "P" ? _charge : 0, kv.Key == "P" ? pType : null);
                hero.SetGear(_match.OffenseBat, _match.DefenseGlove);
                hero.SetHeld(false, true);
                var look = kv.Key == "P" && _phase != Phase.InPlay
                    ? new Vector3(0, 0, -1)
                    : _phase == Phase.InPlay
                        ? new Vector3(_ball.x - (float)x, 0, _ball.z - (float)z)
                        : new Vector3((float)-x, 0, (float)-z + 8f);
                if (_throwing && highlighted)
                    look = _throwTo - new Vector3((float)x, 0, (float)z);
                if (_gun && ((kv.Key == "C" && !_gunPickoff) || (kv.Key == "P" && _gunPickoff)))
                    look = _gunTo - new Vector3((float)x, 0, (float)z);
                hero.Place(new Vector3((float)x, 0, (float)z), look);
                hero.Tick(Time.deltaTime);
            }

            var batter = _match.Batter;
            var bHero = Hero(batter);
            var racing = _phase == Phase.InPlay && _pending != null;
            var stillSwing = racing && _hitT < 0.40f && _swing != null && _swing.Swing && !_swing.Bunt;
            var bPose = racing ? (stillSwing ? HeroActor.Pose.Swing : HeroActor.Pose.Run) : BatterPose();
            bHero.SetPose(bPose, HumanBats ? _charge : 0);
            bHero.SetGear(_match.OffenseBat, _match.DefenseGlove);
            var batting = bPose is HeroActor.Pose.ChargeSwing or HeroActor.Pose.Swing
                or HeroActor.Pose.CheckSwing or HeroActor.Pose.Bunt or HeroActor.Pose.Miss;
            bHero.SetHeld(batting, false);
            bHero.SetHighlight(false);
            if (racing)
            {
                var tFirst = (float)InPlay.HomeToFirstSec(batter);
                var u = Mathf.Clamp01(_hitT / Mathf.Max(0.4f, tFirst));
                var hx = 1.6f + (float)(Diamond.First.X - 1.6) * u;
                var hz = 0.8f + (float)(Diamond.First.Z - 0.8) * u;
                bHero.Place(new Vector3(hx, 0, hz), new Vector3((float)Diamond.First.X, 0, (float)Diamond.First.Z));
            }
            else
                bHero.Place(new Vector3(1.6f, 0, 0.8f), new Vector3(0, 0, 1));
            bHero.Tick(Time.deltaTime);

            PlaceRunner(_match.First, Diamond.First, 1);
            PlaceRunner(_match.Second, Diamond.Second, 2);
            PlaceRunner(_match.Third, Diamond.Third, 3);
            PlaceStealRunner();

            foreach (var kv in _heroes)
                if (!_used.Contains(kv.Key) && kv.Value != null)
                    kv.Value.gameObject.SetActive(false);

            var starPitch = _pitch != null && _pitch.Star ? _match.Pitcher.StarPitch : _spec.ActivePitch;
            var starSwing = _pending != null ? _pending.StarSwingUsed
                : _last != null ? _last.AtBat.StarSwingUsed : null;
            var ptype = _pitch != null ? _pitch.Type : "fastball";
            var heat = _last != null && _last.Heatball;
            if ((_caught || _buddy) && !_throwing)
                HoldBallInGlove();
            if (_replaying || _phase is Phase.Flight or Phase.InPlay or Phase.Set || _spec.Active)
                _park.Ball.Place(_ball, starPitch, ptype, heat);
            else
                _park.Ball.Hide();

            _zone.Show(_phase is Phase.Set or Phase.Flight, _aimX, _aimY);

            Character fielder = null;
            if (_phase == Phase.InPlay && defense.TryGetValue(_glovePos, out var gloveNow))
                fielder = gloveNow;
            else if (_preview != null) fielder = _preview.Fielder;
            else if (_last != null) fielder = _last.Fielder;
            var from = Vector3.zero;
            if (fielder != null && _heroes.TryGetValue(fielder.Id, out var fh) && fh != null)
                from = fh.transform.position;
            var lick = fielder != null && fielder.FieldAbility == "lick-catch" && _phase == Phase.InPlay;
            var laser = fielder != null && fielder.FieldAbility == "laser" && (_caught || (_last != null && _last.Throw != null));
            var burn = starSwing == "furnace" || starSwing == "heat-swing";
            var frags = starSwing == "cask-swing" || starSwing == "shell-swing";
            _spec.Tick(Time.deltaTime, _ball, _phase == Phase.Flight, _phase == Phase.InPlay,
                _pitch != null && _pitch.Star, starPitch, starSwing ?? "", from, _ball, lick, laser, burn, frags);
            var flash = _phase == Phase.InPlay && BuddySet && !_buddy && !_throwing;
            var flashAt = Vector3.zero;
            if (flash && !string.IsNullOrEmpty(_buddyPos) && _gloveAt.TryGetValue(_buddyPos, out var planted))
                flashAt = new Vector3((float)planted.X, 0f, (float)planted.Z);
            _spec.BuddyTell(flash, flashAt, _buddyWindow);
            var itemTargetPos = ItemTargetWorld();
            var showThrow = _itemFlying || (_itemThrown && _phase == Phase.InPlay);
            var flyU = !_itemFlying && _itemThrown ? 1f
                : _itemFlying ? Mathf.Clamp01(_itemFly / ItemView.FlySeconds) : 0f;
            _items?.Present(Time.deltaTime, ItemOffered, _itemPick, itemTargetPos, showThrow, _itemId, flyU);
        }

        HeroActor.Pose BatterPose()
        {
            if (_phase == Phase.Result && _last != null)
            {
                if (_last.Kind == PlayKind.SwingMiss) return HeroActor.Pose.Miss;
                if (_last.Kind == PlayKind.HomeRun) return HeroActor.Pose.Cheer;
                if (_swing != null && _swing.Bunt) return HeroActor.Pose.Bunt;
                if (_swing != null && _swing.Swing) return HeroActor.Pose.Swing;
                return HeroActor.Pose.Idle;
            }
            if (_phase == Phase.GameOver)
                return _match.HomeScore >= _match.AwayScore ? HeroActor.Pose.Cheer : HeroActor.Pose.Idle;
            if (_phase == Phase.Flight && (_swung || (_swing != null && _swing.Swing)))
            {
                if (_swing != null && _swing.Bunt) return HeroActor.Pose.Bunt;
                if (_charge < 0.2f && _flight > _pitchDur * 0.88f) return HeroActor.Pose.CheckSwing;
                return HeroActor.Pose.Swing;
            }
            if (_phase is Phase.Set or Phase.Flight) return HeroActor.Pose.ChargeSwing;
            return HeroActor.Pose.Idle;
        }

        static HeroActor.Pose FieldPose(Character who, FieldingPreview pre, bool caught)
        {
            if (caught) return pre.Grounder ? HeroActor.Pose.Crouch : HeroActor.Pose.Catch;
            var a = who.FieldAbility;
            if (a == "dive" && pre.Grounder) return HeroActor.Pose.Dive;
            if (a == "burrow" && pre.Grounder) return HeroActor.Pose.Dive;
            if (a == "super-jump" && pre.HomeRunLikely) return HeroActor.Pose.Jump;
            if (a == "clamber" && pre.HomeRunLikely) return HeroActor.Pose.Clamber;
            if (a == "spin-check") return HeroActor.Pose.Spin;
            return HeroActor.Pose.Field;
        }

        void PlaceRunner(Character who, (double X, double Z) bag, int bagNum)
        {
            if (who == null) return;
            if (_gun && _gunRunner != null && who.Id == _gunRunner.Id) return;
            var state = _match.RunnerAt(bagNum);
            var spot = Diamond.LeadSpot(bagNum, state != null ? state.Lead01 : 0);
            var next = Diamond.Bag(bagNum >= 3 ? 4 : bagNum + 1);
            var h = Hero(who);
            var pose = HeroActor.Pose.Idle;
            var racing = _phase == Phase.InPlay && _pending != null && (_preview == null || _preview.Grounder || _pending.LaunchDeg < 18);
            if (racing)
            {
                var u = Mathf.Clamp01(_hitT / 3.1f);
                spot = (spot.X + (next.X - spot.X) * u, spot.Z + (next.Z - spot.Z) * u);
                pose = u > 0.82f ? HeroActor.Pose.Slide : HeroActor.Pose.Run;
            }
            else if (state != null && state.Sliding) pose = HeroActor.Pose.Slide;
            else if (state != null && state.StealAttempt) pose = HeroActor.Pose.Run;
            else if (state != null && state.Lead01 > 0.08) pose = HeroActor.Pose.StealLead;
            h.SetPose(pose);
            h.SetGear(_match.OffenseBat, _match.DefenseGlove);
            h.SetHeld(false, false);
            var lead = _match.LeadRunner;
            h.SetHighlight(HumanBats && lead != null && who.Id == lead.Id && _phase is Phase.Set or Phase.Flight);
            h.Place(new Vector3((float)spot.X, 0, (float)spot.Z),
                new Vector3((float)(next.X - bag.X), 0, (float)(next.Z - bag.Z)));
            h.Tick(Time.deltaTime);
        }

        void PlaceStealRunner()
        {
            if (!_gun || _gunRunner == null) return;
            var u = Mathf.Clamp01(_gunT / Mathf.Max(0.05f, _gunDur));
            double x, z;
            if (_gunPickoff)
            {
                var from = Diamond.LeadSpot(_gunFromBag, _gunLead > 0.15 ? _gunLead : 1);
                var to = Diamond.Bag(_gunFromBag);
                x = from.X + (to.X - from.X) * u;
                z = from.Z + (to.Z - from.Z) * u;
            }
            else
            {
                if (!_gunSafe) u *= 0.7f;
                var from = Diamond.Bag(_gunFromBag);
                var to = Diamond.Bag(_gunToBag);
                var t = 0.2 + 0.8 * u;
                x = from.X + (to.X - from.X) * t;
                z = from.Z + (to.Z - from.Z) * t;
            }
            var h = Hero(_gunRunner);
            var pose = u > 0.55f ? HeroActor.Pose.Slide : HeroActor.Pose.Run;
            if (!_gunSafe && u > 0.5f) pose = HeroActor.Pose.Dive;
            h.SetPose(pose);
            h.SetGear(_match.OffenseBat, _match.DefenseGlove);
            h.SetHeld(false, false);
            h.SetHighlight(true);
            var dest = Diamond.Bag(_gunToBag);
            h.Place(new Vector3((float)x, 0, (float)z), new Vector3((float)dest.X - (float)x, 0, (float)dest.Z - (float)z));
            h.Tick(Time.deltaTime);
        }

        void TickBaserunning(float dt)
        {
            if (_match == null || _match.LeadBag == 0) return;
            if (HumanBats && _phase is Phase.Set or Phase.Flight)
            {
                var bag = _match.LeadBag;
                var next = bag == 3 ? 4 : bag + 1;
                var prev = bag == 1 ? 4 : bag - 1;
                var stick = Controls.StickBag;
                if (stick == next) _match.TakeLead(dt * 1.7f);
                else if (stick == bag || stick == prev) _match.ReturnToBag(dt * 2.0f);
                if (Controls.Steal) _match.ToggleSteal();
                var near = _match.Lead01 <= 0.24 || (_match.StealAttempt && _match.Lead01 >= 0.7);
                if (near && (Controls.WestDown || Controls.SouthDown))
                    _match.Slide();
            }
            if (_phase == Phase.Flight && _match.StealOn)
                _match.TakeLead(dt * 2.4f);
            else if (_match.Returning)
                _match.ReturnToBag(dt * 2.2f);
        }

        void TickGun(float dt)
        {
            if (!_gun) return;
            _gunT += dt;
            var u = Mathf.Clamp01(_gunT / Mathf.Max(0.05f, _gunDur));
            _ball = Vector3.Lerp(_gunFrom, _gunTo, u);
            _ball.y += Mathf.Sin(u * Mathf.PI) * 3.4f;
            if (_gunT >= _gunDur) _gun = false;
        }

        void StartStealGun(Character runner, int fromBag, double lead, PlayEvent ev)
        {
            _gun = true;
            _gunT = 0;
            _gunRunner = runner;
            _gunFromBag = fromBag;
            _gunLead = lead;
            _gunSafe = ev.Kind == PlayKind.StolenBase;
            _gunPickoff = ev.Caption != null && ev.Caption.IndexOf("picked off", System.StringComparison.OrdinalIgnoreCase) >= 0;
            _gunToBag = _gunPickoff ? fromBag : fromBag == 1 ? 2 : fromBag == 2 ? 3 : 4;
            var origin = _gunPickoff ? Diamond.Rubber : Diamond.Positions["C"];
            var dest = Diamond.Bag(_gunToBag);
            _gunFrom = new Vector3((float)origin.X, 3.4f, (float)origin.Z);
            _gunTo = new Vector3((float)dest.X, 1.2f, (float)dest.Z);
            var thr = ev.Throw;
            if (thr == null)
                thr = _match.ThrowBetween(_match.Pitcher, runner);
            _spec.ArmThrow(_gunFrom, _gunTo, thr);
            _gunDur = Mathf.Max(0.5f, _spec.ThrowSeconds);
            _audio?.ThrowPop();
            ConsiderHighlight();
            _phase = Phase.Result;
            _t = 0;
        }

        HeroActor Hero(Character who)
        {
            _used.Add(who.Id);
            if (!_heroes.TryGetValue(who.Id, out var h) || h == null)
            {
                var go = new GameObject("Hero-" + who.Id);
                h = go.AddComponent<HeroActor>();
                _heroes[who.Id] = h;
            }
            h.gameObject.SetActive(true);
            h.Bind(who);
            return h;
        }

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
                _last.Caption != null && _last.Caption.IndexOf("BUDDY", System.StringComparison.OrdinalIgnoreCase) >= 0)
                _banner = "BUDDY JUMP";
            else
                _banner = _last != null ? _last.Kind.ToString().ToUpperInvariant() : (_coach != null && _coach.Session != null ? _coach.Session.Caption : "");
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
            _zone.Show(false, 0, 0);
            _ring?.Hide();
            _rig.Aim(new Vector3(0, 36, -46), new Vector3(0, 2, 110), 48f);
        }

        void TickItem(float dt)
        {
            if (_itemFlying)
            {
                _itemFly += dt;
                if (_itemFly >= ItemView.FlySeconds) _itemFlying = false;
            }
            if (!ItemOffered) return;
            if (Controls.CyclePitch && !Controls.Item)
                _itemPick = (_itemPick + 1) % ErrorItems.All.Length;
            AimItem();
            if (!TrainingOn)
                _sub = ErrorItems.All[_itemPick].ToUpperInvariant() + "  ·  stick aim  ·  E throw";
            if (!Controls.ItemConfirm || _itemTarget == null) return;
            var id = ErrorItems.All[_itemPick];
            if (_cpuField != null)
            {
                _cpuField = _match.ThrowItem(_cpuField, id, _itemTarget);
                if (_cpuField.Kind is not (PlayKind.FlyOut or PlayKind.GroundOut))
                    _caught = false;
            }
            _itemThrown = true;
            _itemFlying = true;
            _itemFly = 0;
            _itemId = id;
            _audio?.Item(id);
            if (!TrainingOn) _sub = "";
        }

        void AimItem()
        {
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            var play = _cpuField != null && _cpuField.Fielder != null ? _cpuField.Fielder
                : _preview != null ? _preview.Fielder : null;
            var stick = Mathf.Abs(Controls.StickX) + Mathf.Abs(Controls.StickY);
            if (stick < 0.28f)
            {
                _itemTarget = play;
                return;
            }
            var x = Controls.StickX * 160;
            var z = 30 + (Controls.StickY * 0.5f + 0.5f) * 300;
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

        void CatchGlove()
        {
            if (!_caught && !_gloved) _audio?.Glove();
            _caught = true;
            _gloved = true;
            HoldBallInGlove();
        }

        void ArmRecoil()
        {
            if (_recoilArmed || _preview == null || !_preview.Grounder || _buddy) return;
            _recoilArmed = true;
            var bobble = false;
            var knock = 0.0;
            if (_cpuField != null)
            {
                bobble = _cpuField.Bobble;
                knock = _cpuField.KnockbackSec;
            }
            else if (_pending != null)
            {
                var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
                var who = map.TryGetValue(_glovePos, out var g) ? g : _preview.Fielder;
                var energy = InPlay.Energy(_pending);
                var rng = new System.Random(Seed + _match.Inning * 17 + _match.Outs * 5 + (int)(_hitT * 40));
                bobble = InPlay.Bobbles(energy, who, rng, _match.DefenseGlove);
                knock = InPlay.KnockbackSec(energy, who);
                _playerBobble = bobble;
            }
            if (bobble)
            {
                _bobbling = true;
                _recoilT = 0.58f;
                _park.Ball.Release();
                var dir = new Vector3((float)_fx, 0f, (float)_fz);
                var away = _ball - dir;
                away.y = 0;
                if (away.sqrMagnitude < 0.4f) away = Vector3.forward;
                _ball = new Vector3((float)_fx, 3.1f, (float)_fz) + away.normalized * 6.5f;
            }
            else if (knock > 0.02)
                _recoilT = (float)knock;
        }

        void HoldBallInGlove()
        {
            if (_throwing) return;
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
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
            if (_last.Kind != pick.Play.Kind || _last.Caption != pick.Play.Caption) return;
            _clip = pick;
            _hlAt = _ball;
            var fly = _last.Kind is PlayKind.HomeRun or PlayKind.Triple or PlayKind.Double
                or PlayKind.Single or PlayKind.FlyOut or PlayKind.GroundOut or PlayKind.Foul;
            _hlPath = fly ? _path : null;
            _hlSpray = _pending != null ? (float)_pending.SprayDeg : (float)_last.AtBat.SprayDeg;
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
                _rig.Aim(new Vector3(8, 18, -28), new Vector3(0, 4, 70), 46f);
        }

        void TickReplay(float dt)
        {
            _ = dt;
            if (_hlPath != null && _hlPath.Length > 0)
            {
                var hang = (float)BallFlight.HangTime(_hlPath);
                var t = Mathf.Clamp(_t, 0f, Mathf.Max(0.4f, hang));
                var p = BallFlight.PointAt(_hlPath, _hlSpray, t);
                _ball = new Vector3((float)p.X, (float)p.Y, (float)p.Z);
                if (_clip != null && _clip.Beat is HighlightBeat.BuddyJump or HighlightBeat.RobbedHomer)
                    _rig.Smash(_hlAt.sqrMagnitude > 0.4f ? _hlAt : _ball);
                else if (_clip != null && _clip.Beat == HighlightBeat.StarK)
                    _rig.Smash(new Vector3(0.4f, 3.2f, 2f));
                else
                    _rig.Smash(_ball);
                return;
            }
            _rig.Smash(_hlAt.sqrMagnitude > 0.4f ? _hlAt : new Vector3(0.4f, 3.2f, 2f));
        }

        static bool Key(KeyCode k) => Input.GetKeyDown(k);
    }
}
