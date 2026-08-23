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
        static readonly string[] Parks = { "harbor-diamond", "crystal-rink", "funfair-park", "rooftop-city", "canopy-yard", "ember-keep" };

        enum PlayMode { Exhibition, Challenge, Training }
        PlayMode _mode;
        Challenge _campaign;
        TrainingDirector _coach;
        ContentCatalog _content;
        Match _match;
        ParkView _park;
        CameraRig _rig;
        SpecialFx _spec;
        StrikeZone _zone;
        readonly Dictionary<string, HeroActor> _heroes = new Dictionary<string, HeroActor>();
        readonly HashSet<string> _used = new HashSet<string>();

        enum Phase { Title, Lineup, Set, Flight, InPlay, Result, GameOver }
        Phase _phase = Phase.Title;
        readonly string[] _pitches = { "fastball", "changeup", "curve", "slider" };
        bool _itemArmed;
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
        float _diveT, _jumpT, _swapLock;
        bool _throwing;
        float _throwT, _throwDur;
        FieldingResult _cpuField;
        ThrowResult _armedThrow;
        Character _armedCut;
        Vector3 _throwFrom, _throwTo;
        string _banner, _sub;

        bool TrainingOn => _coach != null && _coach.Session != null;
        bool HumanPitches => TrainingOn ? _coach.PlayerPitches : _match != null && _match.Top;
        bool HumanBats => TrainingOn ? _coach.PlayerBats : _match != null && !_match.Top;
        bool PlayerFields => TrainingOn ? _coach.PlayerFields : HumanPitches;

        void Start()
        {
            var data = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "data"));
            _content = ContentCatalog.Load(data);
            _coach = gameObject.AddComponent<TrainingDirector>();
            _match = NewMatch();
            _park = gameObject.AddComponent<ParkView>();
            _park.Build(_match.Park);
            _spec = gameObject.AddComponent<SpecialFx>();
            _spec.Build(transform);
            _zone = gameObject.AddComponent<StrikeZone>();
            _zone.Build(transform);
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
            switch (_phase)
            {
                case Phase.Title: TickTitle(); break;
                case Phase.Lineup:
                    if (Key(KeyCode.B)) _match.CycleBat(true);
                    if (Key(KeyCode.G)) _match.CycleGlove(true);
                    if (Key(KeyCode.N)) _match.CycleBat(false);
                    if (Key(KeyCode.M)) _match.CycleGlove(false);
                    if (Controls.SouthDown || _t > 10f) BeginSet();
                    break;
                case Phase.Set: TickSet(dt); break;
                case Phase.Flight: TickFlight(dt); break;
                case Phase.InPlay: TickInPlay(dt); break;
                case Phase.Result:
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
                            _phase = Phase.GameOver;
                            _t = 0;
                            _rig.Aim(new Vector3(8, 18, -28), new Vector3(0, 4, 70), 46f);
                        }
                        else BeginSet();
                    }
                    break;
                case Phase.GameOver:
                    if (Controls.SouthDown) ConfirmGameOver();
                    break;
            }
            DrawActors();
            _coach?.Tick(_rig != null ? _rig.Cam : Camera.main);
            _rig.Tick(dt);
        }

        void OnGUI()
        {
            if (_match == null) return;
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
                _star, _match.StealOn, _itemArmed, _charge, timing,
                _showTiming && _phase is Phase.Set or Phase.Flight && !TrainingOn, banner, sub, Look.Portrait(HomeCaptain),
                _mode == PlayMode.Training, TrainingOn ? _coach.Session.Progress : null);
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
                return _campaign.MakeMatch(_content, Innings, Seed);
            }
            _campaign = null;
            return Match.Exhibition(_content, HomeCaptain, AwayCaptain, Innings, Seed, ParkId);
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
            if (Key(KeyCode.C) && _mode == PlayMode.Exhibition)
            {
                var i = System.Array.IndexOf(Parks, ParkId);
                ParkId = Parks[(i < 0 ? 0 : i + 1) % Parks.Length];
            }
            if (Controls.SouthDown)
            {
                if (_mode == PlayMode.Training)
                {
                    BeginTraining();
                    return;
                }
                _match = NewMatch();
                _park.Build(_match.Park);
                _spec.Build(transform);
                _phase = Phase.Lineup;
                _t = 0;
                _rig.Aim(new Vector3(0, 38, -48), new Vector3(0, 2, 90), 48f);
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
            _park.Build(_match.Park);
            _spec.Build(transform);
            _banner = _coach.Session.Caption;
            _sub = _coach.Session.Verb;
            BeginSet();
        }

        void EndTraining()
        {
            _coach?.Stop();
            _mode = PlayMode.Training;
            Seed++;
            _phase = Phase.Title;
            _t = 0;
            _banner = _sub = "";
            _rig.Aim(new Vector3(8, 18, -28), new Vector3(0, 4, 70), 46f);
        }

        void ConfirmGameOver()
        {
            Seed++;
            if (_campaign != null && !_campaign.AllBeaten)
            {
                _match = _campaign.MakeMatch(_content, Innings, Seed);
                _park.Build(_match.Park);
                _spec.Build(transform);
                _phase = Phase.Lineup;
                _t = 0;
                return;
            }
            _match = NewMatch();
            _park.Build(_match.Park);
            _spec.Build(transform);
            _phase = Phase.Title;
            _t = 0;
            _rig.Aim(new Vector3(8, 18, -28), new Vector3(0, 4, 70), 46f);
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
            _diveT = _jumpT = _swapLock = 0;
            _gloveAt.Clear();
            _star = false;
            _banner = _sub = "";
            _ball = new Vector3(0, 5.4f, 60.5f);
            _park.Ball.Place(_ball, "", "fastball", false);
            _aimX = _aimY = 0;
            _smash = 0;
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
            if (HumanBats && Controls.Steal) _match.ToggleSteal();
            if (HumanBats && Controls.Item && _match.Chemistry.ChemistryItemOffered(_match.Batter, _match.OnDeck))
                _itemArmed = !_itemArmed;
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
            if (HumanBats)
            {
                if (Controls.NorthDown && _match.CanStarSwing) _star = !_star;
                if (Controls.Charge) _charge = Mathf.Min(1, _charge + dt / 0.45f);
                if (Controls.SouthDown && !_swung)
                {
                    _swung = true;
                    _swing = new SwingCommand(true, _charge, (_flight - _pitchDur) * 60f, _star && _match.CanStarSwing, Controls.StickX * 18f, Controls.WestHeld);
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
            if (!_match.BeginAtBat(_pitch, _swing, out var hit, out var finished))
            {
                _last = finished;
                NoteTrainingPitch();
                NoteTrainingSwing();
                Banner();
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
            _itemArmed = HumanBats && _itemArmed;
            if (!_playerFielding)
            {
                var item = HumanBats ? (_itemArmed ? "banana" : "") : null;
                _cpuField = _match.ResolveFielding(hit, _preview);
                _cpuField = _match.ApplyOffenseItem(hit, _cpuField, item);
            }
            _itemArmed = false;
            InitGloves();
            _caught = _buddy = false;
            _throwBag = 0;
            _throwing = false;
            _armedThrow = null;
            _armedCut = null;
            _diveT = _jumpT = 0;
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
            if (_playerFielding)
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
            if (hit.Quality == ContactQuality.Perfect || hit.StarSwingUsed != null)
            {
                _freeze = 0.32f;
                _smash = 0.55f;
                _rig.Smash(_ball);
            }
            else if (hit.Quality == ContactQuality.Solid)
            {
                _freeze = 0.12f;
                _rig.Punch(10f);
            }
        }

        void TickInPlay(float dt)
        {
            _hitT += dt;
            if (_path == null || _path.Length == 0) { BeginResult(); return; }
            var spray = _pending != null ? _pending.SprayDeg : _last.AtBat.SprayDeg;
            var p = BallFlight.PointAt(_path, spray, _hitT);
            _ball = new Vector3((float)p.X, (float)Mathf.Max(0.6f, (float)p.Y), (float)p.Z);
            if (_smash > 0) _smash -= dt;
            else _rig.Aim(_ball + new Vector3(14, 11, -20), _ball + new Vector3(0, 2, 6), 50f);

            if (_diveT > 0) _diveT -= dt;
            if (_jumpT > 0) _jumpT -= dt;
            if (_swapLock > 0) _swapLock -= dt;

            if (_throwing)
            {
                _throwT += dt;
                var u = Mathf.Clamp01(_throwT / Mathf.Max(0.05f, _throwDur));
                var arc = _armedThrow != null && _armedThrow.Relation == Chemistry.Good ? 5.2f
                    : _armedThrow != null && _armedThrow.Relation == Chemistry.Bad ? 1.6f : 3.2f;
                _ball = Vector3.Lerp(_throwFrom, _throwTo, u);
                _ball.y += Mathf.Sin(u * Mathf.PI) * arc;
                if (_throwT >= _throwDur) CommitInPlay();
                return;
            }

            if (_playerFielding && _preview != null && _pending != null)
            {
                TickPlayerField(dt);
                return;
            }

            if (_preview != null && _cpuField != null && _pending != null)
            {
                TickCpuField();
                return;
            }

            var done = _hitT >= BallFlight.HangTime(_path) + 0.35f;
            if (_last?.Kind == PlayKind.HomeRun && _hitT > 2.4f) done = true;
            if (done) BeginResult();
        }

        void TickPlayerField(float dt)
        {
            var pre = _preview;
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            var hang = BallFlight.HangTime(_path);
            var chasing = _hitT < hang;

            if (Controls.SwapPitcher)
            {
                CycleGlove(map);
                _swapLock = 0.7f;
            }

            var stick = Mathf.Abs(Controls.StickX) + Mathf.Abs(Controls.StickY);
            if (chasing && _swapLock <= 0 && stick < 0.35f)
                AutoGlove(map);

            if (chasing && map.TryGetValue(_glovePos, out var glove))
            {
                var speed = (18 + glove.Stats.Run * 1.8) * (pre.Frozen ? 0.4 : 1);
                _fx += Controls.StickX * speed * dt;
                _fz += Controls.StickY * speed * dt;
                _gloveAt[_glovePos] = (_fx, _fz);
            }

            if (Controls.WestDown)
            {
                _jumpT = 0.55f;
                if (pre.Buddy != null && pre.HomeRunLikely) _buddy = true;
            }
            if (Controls.EastDown) _diveT = 0.5f;

            var window = CatchWindow(map);
            var d = Diamond.Dist(_fx, _fz, _ball.x, _ball.z);
            if (Controls.SouthDown && d < window) _caught = true;
            if (_diveT > 0 && d < window && _ball.y < 7.5f) _caught = true;
            if (_jumpT > 0 && d < window && _ball.y > 2.2f) _caught = true;

            ReadThrowBag(!chasing || _caught || _buddy);

            if (_hitT < hang) return;
            if (Diamond.Dist(_fx, _fz, pre.LandingX, pre.LandingZ) < window + 2)
                _caught = true;
            if ((_caught || _buddy) && _throwBag == 0 && _hitT < hang + 0.85f)
                return;
            BeginPlayerThrowOrCommit(map);
        }

        void TickCpuField()
        {
            var hang = BallFlight.HangTime(_path);
            var u = Mathf.Clamp01(_hitT / Mathf.Max(0.2f, hang));
            var start = Diamond.Positions[_preview.Position];
            _fx = start.X + (_preview.LandingX - start.X) * u;
            _fz = start.Z + (_preview.LandingZ - start.Z) * u;
            _glovePos = _preview.Position;
            _gloveAt[_glovePos] = (_fx, _fz);
            var outPlay = _cpuField.Kind is PlayKind.FlyOut or PlayKind.GroundOut;
            if (outPlay && _hitT >= hang - 0.18f) _caught = true;
            if (_hitT < hang) return;
            if (outPlay && _cpuField.Throw != null)
            {
                _armedThrow = _cpuField.Throw;
                _armedCut = _cpuField.Cutoff;
                BeginThrow(_cpuField.Throw, _cpuField.Cutoff, 0);
                return;
            }
            if (_cpuField.Kind == PlayKind.HomeRun && _hitT < 2.4f) return;
            if (_hitT < hang + 0.35f) return;
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
            if (thr != null) BeginThrow(thr, cut, _throwBag);
            else CommitInPlay();
        }

        void BeginThrow(ThrowResult thr, Character cut, int bag)
        {
            _throwing = true;
            _throwT = 0;
            var to = cut != null && _heroes.TryGetValue(cut.Id, out var ch) && ch != null
                ? ch.transform.position
                : BagWorld(bag);
            _throwFrom = new Vector3((float)_fx, 3.2f, (float)_fz);
            _throwTo = to + Vector3.up * 1.2f;
            _spec.ArmThrow(_throwFrom, to, thr);
            _throwDur = Mathf.Max(0.28f, _spec.ThrowSeconds);
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
                result = _match.ApplyOffenseItem(_pending, result, null);
                _last = _match.FinishAtBat(_pitch, _swing, _pending, result);
                _coach?.OnField(result, _match);
            }
            else if (_cpuField != null && _pending != null)
            {
                _last = _match.FinishAtBat(_pitch, _swing, _pending, _cpuField);
                _coach?.OnField(_cpuField, _match);
            }
            Banner();
            _playerFielding = false;
            _pending = null;
            _cpuField = null;
            _throwing = false;
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
                var kind = pre.Grounder ? PlayKind.GroundOut : PlayKind.FlyOut;
                return new FieldingResult(kind, from, cut, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace, thr, pre.Buddy);
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
                var highlighted = _phase == Phase.InPlay && who.Id == litId;
                if (highlighted)
                {
                    x = _fx;
                    z = _fz;
                    if (_throwing) pose = HeroActor.Pose.Throw;
                    else if (_caught || _buddy) pose = HeroActor.Pose.Catch;
                    else if (_diveT > 0) pose = HeroActor.Pose.Dive;
                    else if (_jumpT > 0) pose = HeroActor.Pose.Jump;
                    else if (_preview != null) pose = FieldPose(who, _preview, false);
                    else pose = HeroActor.Pose.Field;
                }
                else if (_phase == Phase.InPlay && _preview != null && who.Id == _preview.Fielder.Id)
                    pose = FieldPose(who, _preview, _caught);
                if (kv.Key == "P" && _phase is Phase.Set or Phase.Flight)
                    pose = _phase == Phase.Flight ? HeroActor.Pose.Throw : HeroActor.Pose.ChargePitch;
                var hero = Hero(who);
                hero.SetGrow(who.FieldAbility == "grow" && highlighted);
                hero.SetHighlight(highlighted);
                if (_pending != null && _pending.StarSwingUsed == "heart-swing" && highlighted)
                    pose = HeroActor.Pose.Charm;
                var pType = _pitch != null ? _pitch.Type : _pitches[_pitchIndex];
                hero.SetPose(pose, kv.Key == "P" ? _charge : 0, kv.Key == "P" ? pType : null);
                var look = kv.Key == "P" && _phase != Phase.InPlay
                    ? new Vector3(0, 0, -1)
                    : _phase == Phase.InPlay
                        ? new Vector3(_ball.x - (float)x, 0, _ball.z - (float)z)
                        : new Vector3((float)-x, 0, (float)-z + 8f);
                if (_throwing && highlighted)
                    look = _throwTo - new Vector3((float)x, 0, (float)z);
                hero.Place(new Vector3((float)x, 0, (float)z), look);
                hero.Tick(Time.deltaTime);
            }

            var batter = _match.Batter;
            var bHero = Hero(batter);
            var bPose = _phase == Phase.Flight && (_swung || (_swing != null && _swing.Swing))
                ? HeroActor.Pose.Swing
                : _phase is Phase.Set or Phase.Flight
                    ? HeroActor.Pose.ChargeSwing
                    : HeroActor.Pose.Idle;
            bHero.SetPose(bPose, HumanBats ? _charge : 0);
            bHero.SetHighlight(false);
            bHero.Place(new Vector3(1.6f, 0, 0.8f), new Vector3(0, 0, 1));
            bHero.Tick(Time.deltaTime);

            PlaceRunner(_match.First, Diamond.First);
            PlaceRunner(_match.Second, Diamond.Second);
            PlaceRunner(_match.Third, Diamond.Third);

            foreach (var kv in _heroes)
                if (!_used.Contains(kv.Key) && kv.Value != null)
                    kv.Value.gameObject.SetActive(false);

            var starPitch = _pitch != null && _pitch.Star ? _match.Pitcher.StarPitch : _spec.ActivePitch;
            var starSwing = _pending != null ? _pending.StarSwingUsed
                : _last != null ? _last.AtBat.StarSwingUsed : null;
            var ptype = _pitch != null ? _pitch.Type : "fastball";
            var heat = _last != null && _last.Heatball;
            if (_phase is Phase.Flight or Phase.InPlay or Phase.Set || _spec.Active)
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
        }

        static HeroActor.Pose FieldPose(Character who, FieldingPreview pre, bool caught)
        {
            if (caught) return HeroActor.Pose.Catch;
            var a = who.FieldAbility;
            if (a == "dive" && pre.Grounder) return HeroActor.Pose.Dive;
            if (a == "burrow" && pre.Grounder) return HeroActor.Pose.Dive;
            if (a == "super-jump" && pre.HomeRunLikely) return HeroActor.Pose.Jump;
            if (a == "clamber" && pre.HomeRunLikely) return HeroActor.Pose.Clamber;
            if (a == "spin-check") return HeroActor.Pose.Spin;
            return HeroActor.Pose.Field;
        }

        void PlaceRunner(Character who, (double X, double Z) bag)
        {
            if (who == null) return;
            var h = Hero(who);
            h.SetPose(HeroActor.Pose.Idle);
            h.SetHighlight(false);
            h.Place(new Vector3((float)bag.X, 0, (float)bag.Z), new Vector3(0, 0, -1));
            h.Tick(Time.deltaTime);
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
            _banner = _last != null ? _last.Kind.ToString().ToUpperInvariant() : (_coach != null && _coach.Session != null ? _coach.Session.Caption : "");
            _sub = _last != null ? _last.Caption : (_coach != null && _coach.Session != null ? _coach.Session.Verb : "");
        }

        void BeginResult()
        {
            _phase = Phase.Result;
            _t = 0;
            _smash = 0;
            _zone.Show(false, 0, 0);
            _rig.Aim(new Vector3(0, 36, -46), new Vector3(0, 2, 110), 48f);
        }

        static float Bounce(float t)
        {
            var x = t % 2f;
            return x < 1f ? x : 2f - x;
        }

        static bool Key(KeyCode k) => Input.GetKeyDown(k);
    }
}
