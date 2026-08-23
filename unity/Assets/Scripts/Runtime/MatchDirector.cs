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

        bool _challenge;
        Challenge _campaign;
        ContentCatalog _content;
        Match _match;
        ParkView _park;
        CameraRig _rig;
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
        Sample[] _path;
        Vector3 _ball;
        double _fx, _fz;
        bool _caught, _buddy;
        string _banner, _sub;

        bool HumanPitches => _match != null && _match.Top;
        bool HumanBats => _match != null && !_match.Top;

        void Start()
        {
            var data = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "data"));
            _content = ContentCatalog.Load(data);
            _match = NewMatch();
            _park = gameObject.AddComponent<ParkView>();
            _park.Build(_match.Park);
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            if (FindAnyObjectByType<Light>() == null)
            {
                var sun = new GameObject("Sun");
                var light = sun.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                light.color = new Color(1f, 0.95f, 0.86f);
                light.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(48f, 28f, 0f);
            }
            _rig = gameObject.AddComponent<CameraRig>();
            _rig.Bind(cam);
            Look.SetupLighting(cam, Colors.Sky);
            _rig.Cut(new Vector3(8, 18, -28), new Vector3(0, 4, 70), 46f);
        }

        void Update()
        {
            if (_match == null) return;
            var dt = Time.deltaTime;
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
                    if (Confirm() || _t > 10f) BeginSet();
                    break;
                case Phase.Set: TickSet(dt); break;
                case Phase.Flight: TickFlight(dt); break;
                case Phase.InPlay: TickInPlay(dt); break;
                case Phase.Result:
                    if (_t > (_last?.Kind == PlayKind.HomeRun ? 2.4f : 1.35f))
                    {
                        if (_match.Over)
                        {
                            _campaign?.Resolve(_match);
                            _phase = Phase.GameOver;
                            _t = 0;
                            _rig.Aim(new Vector3(8, 18, -28), new Vector3(0, 4, 70), 46f);
                        }
                        else BeginSet();
                    }
                    break;
                case Phase.GameOver:
                    if (Confirm()) ConfirmGameOver();
                    break;
            }
            DrawActors();
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
            var awayId = _challenge
                ? (_campaign != null ? _campaign.NextOpponentId(_content) : Challenge.Start(_content, HomeCaptain).NextOpponentId(_content))
                : AwayCaptain;
            var away = _content.Must(awayId);
            var parkName = _content.Parks.TryGetValue(ParkId, out var pk) ? pk.Name : ParkId;
            var rio = HomeCaptain == "rio" ? Look.Rio : null;
            HudView.Draw(_match, ui, parkName, home.Name, away.Name, _challenge, _pitches, _pitchIndex,
                _star, _match.StealOn, _itemArmed, _charge, timing,
                _phase is Phase.Set or Phase.Flight, _banner, _sub, rio);
        }

        Match NewMatch()
        {
            if (_challenge)
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
            if (Key(KeyCode.H)) _challenge = !_challenge;
            if (Key(KeyCode.A) || Key(KeyCode.LeftArrow))
            {
                HomeCaptain = PresetTeams.PrevCaptain(HomeCaptain);
                if (!_challenge) ParkId = PresetTeams.HomeParkId(HomeCaptain);
            }
            if (Key(KeyCode.D) || Key(KeyCode.RightArrow))
            {
                HomeCaptain = PresetTeams.NextCaptain(HomeCaptain);
                if (!_challenge) ParkId = PresetTeams.HomeParkId(HomeCaptain);
            }
            if (!_challenge)
            {
                if (Key(KeyCode.W) || Key(KeyCode.UpArrow)) AwayCaptain = PresetTeams.PrevCaptain(AwayCaptain);
                if (Key(KeyCode.S) || Key(KeyCode.DownArrow)) AwayCaptain = PresetTeams.NextCaptain(AwayCaptain);
                if (HomeCaptain.Equals(AwayCaptain, System.StringComparison.OrdinalIgnoreCase))
                    AwayCaptain = PresetTeams.NextCaptain(HomeCaptain);
            }
            if (Key(KeyCode.C))
            {
                var i = System.Array.IndexOf(Parks, ParkId);
                ParkId = Parks[(i < 0 ? 0 : i + 1) % Parks.Length];
            }
            if (Confirm())
            {
                _match = NewMatch();
                _park.Build(_match.Park);
                _phase = Phase.Lineup;
                _t = 0;
                _rig.Aim(new Vector3(0, 38, -48), new Vector3(0, 2, 90), 48f);
            }
        }

        void ConfirmGameOver()
        {
            Seed++;
            if (_campaign != null && !_campaign.AllBeaten)
            {
                _match = _campaign.MakeMatch(_content, Innings, Seed);
                _park.Build(_match.Park);
                _phase = Phase.Lineup;
                _t = 0;
                return;
            }
            _match = NewMatch();
            _park.Build(_match.Park);
            _phase = Phase.Title;
            _t = 0;
            _rig.Aim(new Vector3(8, 18, -28), new Vector3(0, 4, 70), 46f);
        }

        void BeginSet()
        {
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
            _star = false;
            _banner = _sub = "";
            _ball = new Vector3(0, 5.4f, 60.5f);
            _park.Ball.Place(_ball, false, "fastball");
            if (_match.Top) _rig.Aim(new Vector3(-5f, 7.2f, 78f), new Vector3(0.4f, 3.6f, 6f), 42f);
            else _rig.Aim(new Vector3(5.5f, 6.4f, -13f), new Vector3(0f, 3.2f, 48f), 40f);
        }

        void TickSet(float dt)
        {
            _pip += dt * 1.35f;
            if (Key(KeyCode.Tab) || Pad(5)) _pitchIndex = (_pitchIndex + 1) % _pitches.Length;
            if (Key(KeyCode.R)) _match.SwapPitcher();
            if ((Key(KeyCode.Q) || Pad(3)) && (HumanPitches ? _match.CanStarPitch : _match.CanStarSwing)) _star = !_star;
            if (HumanBats && (Key(KeyCode.X) || Pad(4))) _match.ToggleSteal();
            if (HumanBats && (Key(KeyCode.E) || Pad(5)) && _match.Chemistry.ChemistryItemOffered(_match.Batter, _match.OnDeck))
                _itemArmed = !_itemArmed;
            if (HumanPitches)
            {
                _charge = Charging() ? Mathf.Min(1, _charge + dt / 0.55f) : Mathf.Max(0, _charge - dt * 1.4f);
                if (Confirm()) Launch(PlayerPitch());
                return;
            }
            if (_t > 0.55f) Launch(_match.CpuPitch());
        }

        PitchCommand PlayerPitch()
        {
            var bounce = Bounce(_pip);
            var stick = StickX();
            return new PitchCommand(_pitches[_pitchIndex], _charge, (bounce - 0.5f) * 18f + stick * 4f, _star && _match.CanStarPitch);
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
            _rig.Punch(pitch.Star ? 8f : 4f);
        }

        void TickFlight(float dt)
        {
            _flight += dt;
            var u = Mathf.Clamp01(_flight / _pitchDur);
            var breakX = _pitch.Type == "curve" ? Mathf.Sin(u * Mathf.PI) * 2.8f
                : _pitch.Type == "slider" ? Mathf.Sin(u * Mathf.PI) * 1.6f : 0f;
            var y = 5.4f + (2.4f - 5.4f) * u * u + (_pitch.Type == "changeup" ? -1.2f * u * u : 0f);
            var z = 60.5f * (1 - u);
            if (_pitch.Star)
            {
                var id = _match.Pitcher.StarPitch;
                if (id == "heatball") breakX += Mathf.Sin(u * 18f) * 0.4f;
                else if (id == "prismball") breakX += Mathf.Sin(u * 24f) * 1.8f;
                else if (id == "charmball") breakX += Mathf.Sin(u * 9f) * 0.7f;
                else if (id == "phonyball") breakX += u > 0.55f ? 2.4f : -0.5f;
                else if (id == "caskball") y += 0.55f * u;
            }
            _ball = new Vector3(breakX, y, z);
            if (HumanBats)
            {
                if ((Key(KeyCode.Q) || Pad(3)) && _match.CanStarSwing) _star = !_star;
                if (Charging()) _charge = Mathf.Min(1, _charge + dt / 0.45f);
                if (Confirm() && !_swung)
                {
                    _swung = true;
                    _swing = new SwingCommand(true, _charge, (_flight - _pitchDur) * 60f, _star && _match.CanStarSwing, StickX() * 18f);
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
            if (HumanPitches)
            {
                if (!_match.BeginAtBat(_pitch, _swing, out var hit, out var finished))
                {
                    _last = finished;
                    Banner();
                    BeginResult();
                    return;
                }
                _pending = hit;
                _preview = _match.PreviewHit(hit);
                var start = Diamond.Positions[_preview.Position];
                _fx = start.X;
                _fz = start.Z;
                _caught = _buddy = false;
                _playerFielding = true;
                StartFly(hit);
                return;
            }
            var item = HumanBats ? (_itemArmed ? "banana" : "") : null;
            _itemArmed = false;
            _last = _match.Play(_pitch, _swing, item);
            Banner();
            var fly = _last.Kind is PlayKind.Single or PlayKind.Double or PlayKind.Triple
                or PlayKind.HomeRun or PlayKind.FlyOut or PlayKind.GroundOut or PlayKind.Foul;
            if (fly && _last.AtBat.ExitVeloMph > 1) StartFly(_last.AtBat);
            else BeginResult();
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
                _freeze = 0.18f;
                _rig.Punch(14f);
            }
        }

        void TickInPlay(float dt)
        {
            _hitT += dt;
            if (_path == null || _path.Length == 0) { BeginResult(); return; }
            var spray = _pending != null ? _pending.SprayDeg : _last.AtBat.SprayDeg;
            var p = BallFlight.PointAt(_path, spray, _hitT);
            _ball = new Vector3((float)p.X, (float)Mathf.Max(0.6f, (float)p.Y), (float)p.Z);
            _rig.Aim(_ball + new Vector3(14, 11, -20), _ball + new Vector3(0, 2, 6), 50f);

            if (_playerFielding && _preview != null && _pending != null)
            {
                var speed = (18 + _preview.Fielder.Stats.Run * 1.8) * (_preview.Frozen ? 0.4 : 1);
                _fx += StickX() * speed * dt;
                _fz += StickY() * speed * dt;
                if ((Key(KeyCode.F) || Pad(1)) && _preview.Buddy != null && _preview.HomeRunLikely) _buddy = true;
                if (Confirm() && Diamond.Dist(_fx, _fz, _ball.x, _ball.z) < _preview.CatchRadius + 4) _caught = true;
                var hang = BallFlight.HangTime(_path);
                if (_hitT >= hang)
                {
                    if (Diamond.Dist(_fx, _fz, _preview.LandingX, _preview.LandingZ) < _preview.CatchRadius + 6)
                        _caught = true;
                    FinishField();
                }
                return;
            }

            var done = _hitT >= BallFlight.HangTime(_path) + 0.35f;
            if (_last?.Kind == PlayKind.HomeRun && _hitT > 2.4f) done = true;
            if (done) BeginResult();
        }

        void FinishField()
        {
            var pre = _preview;
            var hit = _pending;
            Character cut = null;
            ThrowResult thr = null;
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            var bag = Input.GetKey(KeyCode.Alpha1) || PadDir(1) ? 1
                : Input.GetKey(KeyCode.Alpha2) || PadDir(2) ? 2
                : Input.GetKey(KeyCode.Alpha3) || PadDir(3) ? 3
                : Input.GetKey(KeyCode.H) || PadDir(4) ? 4 : 0;
            if ((_caught || _buddy) && bag > 0)
            {
                var key = bag == 1 ? "1B" : bag == 2 ? "2B" : bag == 3 ? "3B" : "C";
                map.TryGetValue(key, out cut);
                if (cut != null) thr = _match.ThrowBetween(pre.Fielder, cut);
            }
            FieldingResult result;
            if (_buddy || _caught)
            {
                var kind = pre.Grounder ? PlayKind.GroundOut : PlayKind.FlyOut;
                result = new FieldingResult(kind, pre.Fielder, cut, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace, thr, pre.Buddy);
            }
            else if (pre.HomeRunLikely)
                result = new FieldingResult(PlayKind.HomeRun, pre.Fielder, null, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace, Buddy: pre.Buddy);
            else
                result = new FieldingResult(hit.CarryFt >= 250 ? PlayKind.Double : PlayKind.Single, pre.Fielder, null, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace, Buddy: pre.Buddy);
            result = _match.ApplyOffenseItem(hit, result, null);
            _last = _match.FinishAtBat(_pitch, _swing, hit, result);
            Banner();
            _playerFielding = false;
            _pending = null;
            BeginResult();
        }

        void DrawActors()
        {
            _used.Clear();
            var defense = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            foreach (var kv in defense)
            {
                var who = kv.Value;
                var pos = Diamond.Positions[kv.Key];
                double x = pos.X, z = pos.Z;
                var pose = HeroActor.Pose.Idle;
                if (_playerFielding && _preview != null && who.Id == _preview.Fielder.Id)
                {
                    x = _fx;
                    z = _fz;
                    pose = _caught ? HeroActor.Pose.Catch : HeroActor.Pose.Field;
                }
                else if (_phase == Phase.InPlay && !_playerFielding && _last?.Fielder != null && who.Id == _last.Fielder.Id)
                {
                    var u = Mathf.Clamp01(_hitT / Mathf.Max(0.2f, (float)_last.HangTimeSec));
                    x = pos.X + (_last.LandingX - pos.X) * u;
                    z = pos.Z + (_last.LandingZ - pos.Z) * u;
                    pose = HeroActor.Pose.Field;
                }
                if (kv.Key == "P" && _phase is Phase.Set or Phase.Flight)
                    pose = _phase == Phase.Flight ? HeroActor.Pose.Throw : HeroActor.Pose.ChargePitch;
                var hero = Hero(who);
                hero.SetPose(pose, kv.Key == "P" ? _charge : 0);
                var look = kv.Key == "P" ? new Vector3(0, 0, -1) : new Vector3((float)-x, 0, (float)-z + 8f);
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
            bHero.Place(new Vector3(1.6f, 0, 0.8f), new Vector3(0, 0, 1));
            bHero.Tick(Time.deltaTime);

            PlaceRunner(_match.First, Diamond.First);
            PlaceRunner(_match.Second, Diamond.Second);
            PlaceRunner(_match.Third, Diamond.Third);

            foreach (var kv in _heroes)
                if (!_used.Contains(kv.Key) && kv.Value != null)
                    kv.Value.gameObject.SetActive(false);

            var heat = (_pitch != null && _pitch.Star && _match.Pitcher.StarPitch == "heatball")
                       || (_last != null && _last.Heatball);
            var ptype = _pitch != null ? _pitch.Type : "fastball";
            if (_phase is Phase.Flight or Phase.InPlay or Phase.Set)
                _park.Ball.Place(_ball, heat, ptype);
            else
                _park.Ball.Hide();
        }

        void PlaceRunner(Character who, (double X, double Z) bag)
        {
            if (who == null) return;
            var h = Hero(who);
            h.SetPose(HeroActor.Pose.Idle);
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

        void Banner()
        {
            _banner = _last.Kind.ToString().ToUpperInvariant();
            _sub = _last.Caption;
        }

        void BeginResult()
        {
            _phase = Phase.Result;
            _t = 0;
            _rig.Aim(new Vector3(0, 36, -46), new Vector3(0, 2, 110), 48f);
        }

        static float Bounce(float t)
        {
            var x = t % 2f;
            return x < 1f ? x : 2f - x;
        }

        static bool Confirm() => Key(KeyCode.Space) || Key(KeyCode.Return) || Pad(0);
        static bool Charging() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.JoystickButton4) || Input.GetKey(KeyCode.JoystickButton6) || Input.GetAxis("Fire3") > 0.45f;
        static bool Key(KeyCode k) => Input.GetKeyDown(k);
        static bool Pad(int button) => Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + button));
        static bool PadDir(int bag) =>
            bag == 1 && Input.GetKey(KeyCode.JoystickButton15) ||
            bag == 2 && Input.GetKey(KeyCode.JoystickButton13) ||
            bag == 3 && Input.GetKey(KeyCode.JoystickButton16) ||
            bag == 4 && Input.GetKey(KeyCode.JoystickButton14);
        static float StickX()
        {
            var v = Input.GetAxisRaw("Horizontal");
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v += 1;
            return Mathf.Clamp(v, -1, 1);
        }
        static float StickY()
        {
            var v = Input.GetAxisRaw("Vertical");
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1;
            return Mathf.Clamp(v, -1, 1);
        }
    }
}
