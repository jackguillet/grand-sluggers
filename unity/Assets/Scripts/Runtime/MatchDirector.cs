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

        enum Phase { Title, Lineup, Set, Flight, InPlay, Result, GameOver }

        ContentCatalog _content;
        Match _match;
        ParkView _view;
        Camera _cam;
        Phase _phase = Phase.Title;
        readonly string[] _pitches = { "fastball", "changeup", "curve" };
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
        Sample[] _path;
        Vector3 _ball;
        double _fx, _fz;
        bool _caught, _buddy;
        string _banner, _sub;

        bool HumanPitches => _match.Top;
        bool HumanBats => !_match.Top;

        void Start()
        {
            var data = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "data"));
            _content = ContentCatalog.Load(data);
            _match = NewMatch();
            _view = gameObject.AddComponent<ParkView>();
            _view.Build(_match.Park);
            _cam = Camera.main;
            if (_cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                _cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Colors.Sky;
            _cam.fieldOfView = 48f;
            if (FindAnyObjectByType<Light>() == null)
            {
                var sun = new GameObject("Sun");
                var light = sun.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                sun.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
            }
            PlaceCam(new Vector3(0, 95, -40), new Vector3(0, 0, 140));
        }

        void Update()
        {
            if (_match == null) return;
            var dt = Time.deltaTime;
            _t += dt;
            switch (_phase)
            {
                case Phase.Title:
                    TickTitle();
                    break;
                case Phase.Lineup:
                    if (Key(KeyCode.B)) _match.CycleBat(true);
                    if (Key(KeyCode.G)) _match.CycleGlove(true);
                    if (Key(KeyCode.N)) _match.CycleBat(false);
                    if (Key(KeyCode.M)) _match.CycleGlove(false);
                    if (Confirm() || _t > 8f) BeginSet();
                    break;
                case Phase.Set:
                    TickSet(dt);
                    break;
                case Phase.Flight:
                    TickFlight(dt);
                    break;
                case Phase.InPlay:
                    TickInPlay(dt);
                    break;
                case Phase.Result:
                    if (_t > (_last?.Kind == PlayKind.HomeRun ? 2.2f : 1.3f))
                    {
                        if (_match.Over)
                        {
                            _campaign?.Resolve(_match);
                            _phase = Phase.GameOver;
                            _t = 0;
                        }
                        else BeginSet();
                    }
                    break;
                case Phase.GameOver:
                    if (Confirm()) ConfirmGameOver();
                    break;
            }
            DrawActors();
        }

        void OnGUI()
        {
            if (_match == null) return;
            var s = 18;
            GUI.skin.label.fontSize = s;
            if (_phase == Phase.Title)
            {
                var awayId = _challenge
                    ? (_campaign != null ? _campaign.NextOpponentId(_content) : Challenge.Start(_content, HomeCaptain).NextOpponentId(_content))
                    : AwayCaptain;
                var home = _content.Must(HomeCaptain);
                var away = _content.Must(awayId);
                var parkName = _content.Parks.TryGetValue(ParkId, out var pk) ? pk.Name : ParkId;
                GUI.Label(new Rect(60, 80, 900, 40), "GRAND SLUGGERS");
                GUI.Label(new Rect(60, 120, 900, 30), _challenge ? "CHALLENGE" : "EXHIBITION");
                GUI.Label(new Rect(60, 160, 900, 30), $"YOU  {home.Name}     VS  {away.Name}");
                GUI.Label(new Rect(60, 190, 900, 30), parkName);
                GUI.Label(new Rect(60, 230, 1100, 30), "A/D captain   W/S opponent   C park   H challenge   SPACE play");
                return;
            }
            if (_phase == Phase.Lineup)
            {
                GUI.Label(new Rect(40, 40, 1100, 30), $"TEAM SHEET  {_match.Park.Name}  stars home {_match.HomeStars:0.#}  away {_match.AwayStars:0.#}");
                GUI.Label(new Rect(40, 64, 1100, 24), $"{_match.Home.Name} {_match.HomeBat.Name} / {_match.HomeGlove.Name}  [B][G]     {_match.Away.Name} {_match.AwayBat.Name} / {_match.AwayGlove.Name}  [N][M]");
                var y = 80;
                foreach (var c in _match.HomeOrder)
                {
                    GUI.Label(new Rect(40, y, 500, 24), $"{(c.Captain ? "C" : "+")} {c.Name}  B{c.Stats.Bat} P{c.Stats.Pitch} F{c.Stats.Field} R{c.Stats.Run}");
                    y += 22;
                }
                return;
            }
            if (_phase == Phase.GameOver)
            {
                var mvp = _match.Mvp();
                GUI.Label(new Rect(60, 80, 800, 30), "FINAL");
                GUI.Label(new Rect(60, 120, 800, 30), $"{_match.Away.Name} {_match.AwayScore}   {_match.Home.Name} {_match.HomeScore}");
                GUI.Label(new Rect(60, 170, 800, 30), $"MVP  {mvp.Who.Name}  ({mvp.Points})");
                if (_campaign != null && _campaign.LastRecruit != null)
                    GUI.Label(new Rect(60, 200, 800, 30), $"{_campaign.LastRecruit.Name} joins the roster");
                GUI.Label(new Rect(60, 240, 800, 30), "SPACE  continue");
                return;
            }
            GUI.Label(new Rect(20, 16, 700, 24), $"{(_match.Top ? "TOP" : "BOT")} {_match.Inning}   {_match.Away.Captain.Name} {_match.AwayScore}  {_match.Home.Captain.Name} {_match.HomeScore}");
            GUI.Label(new Rect(20, 42, 500, 24), $"B {_match.Balls}  S {_match.Strikes}  O {_match.Outs}   arm {_match.PitcherStamina}");
            GUI.Label(new Rect(20, 68, 700, 24), $"P {_match.Pitcher.Name}   AB {_match.Batter.Name}   {_pitches[_pitchIndex]}{(_star ? " *" : "")}");
            if (!string.IsNullOrEmpty(_banner))
                GUI.Label(new Rect(Screen.width / 2 - 160, Screen.height / 2 - 20, 400, 40), _banner);
            if (!string.IsNullOrEmpty(_sub))
                GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 + 16, 440, 24), _sub);
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
                _view.Build(_match.Park);
                _phase = Phase.Lineup;
                _t = 0;
            }
        }

        void ConfirmGameOver()
        {
            Seed++;
            if (_campaign != null && !_campaign.AllBeaten)
            {
                _match = _campaign.MakeMatch(_content, Innings, Seed);
                _view.Build(_match.Park);
                _phase = Phase.Lineup;
                _t = 0;
                return;
            }
            _match = NewMatch();
            _view.Build(_match.Park);
            _phase = Phase.Title;
            _t = 0;
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
            PlaceCam(_match.Top ? new Vector3(-6, 10, 82) : new Vector3(7, 9, -16),
                _match.Top ? new Vector3(0, 4, 8) : new Vector3(0, 4, 50));
            _view.HideBall(false);
        }

        void TickSet(float dt)
        {
            _pip += dt * 1.35f;
            if (Key(KeyCode.Tab)) _pitchIndex = (_pitchIndex + 1) % _pitches.Length;
            if (Key(KeyCode.R)) _match.SwapPitcher();
            if (Key(KeyCode.Q) && (HumanPitches ? _match.CanStarPitch : _match.CanStarSwing)) _star = !_star;
            if (HumanPitches)
            {
                _charge = Input.GetKey(KeyCode.LeftShift) ? Mathf.Min(1, _charge + dt / 0.55f) : Mathf.Max(0, _charge - dt * 1.4f);
                if (Confirm()) Launch(PlayerPitch());
                return;
            }
            if (_t > 0.55f) Launch(_match.CpuPitch());
        }

        PitchCommand PlayerPitch()
        {
            var x = _pip % 2f;
            var bounce = x < 1f ? x : 2f - x;
            return new PitchCommand(_pitches[_pitchIndex], _charge, (bounce - 0.5f) * 18f, _star && _match.CanStarPitch);
        }

        void Launch(PitchCommand pitch)
        {
            _pitch = pitch;
            var mph = AtBatResolver.PitchSpeedMph(pitch, _match.Pitcher.Stats.Pitch);
            _pitchDur = Mathf.Clamp((float)(Diamond.Mound / (mph * 1.4667)), 0.32f, 0.85f);
            _flight = 0;
            _swung = false;
            _charge = 0;
            _phase = Phase.Flight;
            _t = 0;
            _ball = new Vector3(0, 5.4f, 60.5f);
        }

        void TickFlight(float dt)
        {
            _flight += dt;
            var u = Mathf.Clamp01(_flight / _pitchDur);
            var breakX = _pitch.Type == "curve" ? Mathf.Sin(u * Mathf.PI) * 2.8f : 0f;
            var y = 5.4f + (2.4f - 5.4f) * u * u + (_pitch.Type == "changeup" ? -1.2f * u * u : 0f);
            var z = 60.5f * (1 - u);
            if (_pitch.Star) breakX += Mathf.Sin(u * 18f) * 0.4f;
            _ball = new Vector3(breakX, y, z);
            if (HumanBats)
            {
                if (Key(KeyCode.Q) && _match.CanStarSwing) _star = !_star;
                if (Input.GetKey(KeyCode.LeftShift)) _charge = Mathf.Min(1, _charge + dt / 0.45f);
                if (Confirm() && !_swung)
                {
                    _swung = true;
                    var spray = 0f;
                    if (Input.GetKey(KeyCode.A)) spray -= 18;
                    if (Input.GetKey(KeyCode.D)) spray += 18;
                    _swing = new SwingCommand(true, _charge, (_flight - _pitchDur) * 60f, _star && _match.CanStarSwing, spray);
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
            _last = _match.Play(_pitch, _swing);
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
        }

        void TickInPlay(float dt)
        {
            _hitT += dt;
            if (_path == null || _path.Length == 0) { BeginResult(); return; }
            var spray = _pending?.SprayDeg ?? _last.AtBat.SprayDeg;
            var p = BallFlight.PointAt(_path, spray, _hitT);
            _ball = new Vector3((float)p.X, (float)Mathf.Max(0.6f, (float)p.Y), (float)p.Z);
            PlaceCam(_ball + new Vector3(18, 16, -22), _ball + new Vector3(0, 2, 8));

            if (_playerFielding && _preview != null && _pending != null)
            {
                var speed = (18 + _preview.Fielder.Stats.Run * 1.8) * (_preview.Frozen ? 0.4 : 1);
                var mx = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
                var mz = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
                _fx += mx * speed * dt;
                _fz += mz * speed * dt;
                if (Key(KeyCode.F) && _preview.Buddy != null && _preview.HomeRunLikely) _buddy = true;
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
            var bag = Input.GetKey(KeyCode.Alpha1) ? 1 : Input.GetKey(KeyCode.Alpha2) ? 2 : Input.GetKey(KeyCode.Alpha3) ? 3 : Input.GetKey(KeyCode.H) ? 4 : 0;
            if ((_caught || _buddy) && bag > 0)
            {
                var key = bag switch { 1 => "1B", 2 => "2B", 3 => "3B", _ => "C" };
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
            _last = _match.FinishAtBat(_pitch, _swing, hit, result);
            Banner();
            _playerFielding = false;
            _pending = null;
            BeginResult();
        }

        void DrawActors()
        {
            var defense = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            foreach (var kv in defense)
            {
                var who = kv.Value;
                var pos = Diamond.Positions[kv.Key];
                double x = pos.X, z = pos.Z;
                if (_playerFielding && _preview != null && who.Id == _preview.Fielder.Id)
                {
                    x = _fx;
                    z = _fz;
                }
                else if (_phase == Phase.InPlay && !_playerFielding && _last?.Fielder != null && who.Id == _last.Fielder.Id)
                {
                    var u = Mathf.Clamp01(_hitT / Mathf.Max(0.2f, (float)_last.HangTimeSec));
                    x = pos.X + (_last.LandingX - pos.X) * u;
                    z = pos.Z + (_last.LandingZ - pos.Z) * u;
                }
                _view.Person(who.Id, who.Faction).position = new Vector3((float)x, 0, (float)z);
            }
            var batter = _match.Batter;
            _view.Person("batter-" + batter.Id, batter.Faction).position = new Vector3(1.6f, 0, 0.8f);
            if (_match.First != null) _view.Person("r1", _match.First.Faction).position = new Vector3((float)Diamond.First.X, 0, (float)Diamond.First.Z);
            if (_match.Second != null) _view.Person("r2", _match.Second.Faction).position = new Vector3((float)Diamond.Second.X, 0, (float)Diamond.Second.Z);
            if (_match.Third != null) _view.Person("r3", _match.Third.Faction).position = new Vector3((float)Diamond.Third.X, 0, (float)Diamond.Third.Z);
            var heat = _pitch?.Star == true || _last?.Heatball == true;
            if (_phase is Phase.Flight or Phase.InPlay or Phase.Set)
            {
                _view.HideBall(false);
                _view.PlaceBall(_ball, heat);
            }
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
            PlaceCam(new Vector3(0, 95, -40), new Vector3(0, 0, 140));
        }

        void PlaceCam(Vector3 pos, Vector3 target)
        {
            _cam.transform.position = pos;
            _cam.transform.LookAt(target);
        }

        static bool Confirm() => Key(KeyCode.Space) || Key(KeyCode.Return);
        static bool Key(KeyCode k) => Input.GetKeyDown(k);
    }
}
