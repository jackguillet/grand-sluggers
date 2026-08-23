using System.Numerics;
using GrandSluggers.Sim;
using Raylib_cs;

namespace GrandSluggers.Play;

public sealed class Game : IDisposable
{
    enum Phase { Title, Lineup, Set, Flight, InPlay, Result, GameOver }

    readonly bool _demo;
    readonly int _seed;
    readonly ContentCatalog _content;
    readonly string[] _pitches = ["fastball", "changeup", "curve"];

    Match _match;
    Phase _phase = Phase.Title;
    int _pitchIndex;
    bool _starArmed;
    float _charge;
    float _phaseT;
    float _resultT;
    PitchCommand? _pitch;
    SwingCommand? _swing;
    PlayEvent? _last;
    Vector3 _ball;
    readonly List<Vector3> _trail = [];
    IReadOnlyList<Sample> _hitPath = [];
    float _hitT;
    bool _playerSwung;
    float _flightAge;
    float _pitchDur = 0.5f;
    float _pip;
    bool _shotLineup;
    bool _shotFinal;
    string _banner = "";
    string _sub = "";
    Camera3D _cam;

    public Game(bool demo, int seed)
    {
        _demo = demo;
        _seed = seed;
        _content = ContentCatalog.Load();
        _match = Match.Slice(_content, 3, seed);
        _cam = WorldView.HighCamera();
    }

    public void Run()
    {
        const int w = 1600, h = 900;
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint);
        if (_demo)
            Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint);
        Raylib.InitWindow(w, h, "Grand Sluggers — Harbor Diamond");
        Raylib.SetTargetFPS(_demo ? 60 : 60);
        Raylib.SetExitKey(KeyboardKey.Null);

        if (_demo)
            BeginLineup();

        while (!Raylib.WindowShouldClose())
        {
            var dt = Raylib.GetFrameTime();
            if (_demo) dt *= 3.2f;
            var input = PlayerInput.Read();
            if (input.Quit && !_demo) break;
            Tick(dt, input);
            Draw();
            if (_demo) DemoShots();
            if (_demo && _phase == Phase.GameOver && _phaseT > 2.2f) break;
        }

        Raylib.CloseWindow();
        if (_demo)
        {
            Console.WriteLine(_match.BoxLine());
            var mvp = _match.Mvp();
            Console.WriteLine($"MVP {mvp.Who.Name} ({mvp.Points}) — {mvp.Why}");
        }
    }

    public void Dispose() { }

    void Tick(float dt, FrameInput input)
    {
        _phaseT += dt;
        switch (_phase)
        {
            case Phase.Title:
                if (input.ConfirmPressed || _demo && _phaseT > 0.6f) BeginLineup();
                break;
            case Phase.Lineup:
                if (input.ConfirmPressed || _demo && _phaseT > 1.4f) BeginSet();
                break;
            case Phase.Set:
                TickSet(dt, input);
                break;
            case Phase.Flight:
                TickFlight(dt, input);
                break;
            case Phase.InPlay:
                TickInPlay(dt);
                break;
            case Phase.Result:
                _resultT += dt;
                if (_resultT > (_last?.Kind is PlayKind.HomeRun ? 2.2f : 1.35f))
                {
                    if (_match.Over) BeginGameOver();
                    else BeginSet();
                }
                break;
            case Phase.GameOver:
                if (input.ConfirmPressed)
                {
                    _match = Match.Slice(_content, 3, _seed + 1);
                    _phase = Phase.Title;
                    _phaseT = 0;
                }
                break;
        }
    }

    void BeginLineup()
    {
        _phase = Phase.Lineup;
        _phaseT = 0;
        _cam = WorldView.HighCamera();
    }

    void BeginSet()
    {
        _phase = Phase.Set;
        _phaseT = 0;
        _charge = 0;
        _playerSwung = false;
        _swing = null;
        _pitch = null;
        _last = null;
        _trail.Clear();
        _starArmed = false;
        _banner = "";
        _sub = "";
        _ball = new Vector3(0, 5.4f, 60.5f);
        _cam = _match.Top ? WorldView.PitchingCamera() : WorldView.BattingCamera();
        if (_demo || !_match.Top)
        {
            // CPU pitches after a beat (player is batting, or demo)
        }
    }

    void TickSet(float dt, FrameInput input)
    {
        var playerPitches = _match.Top && !_demo;
        _pip += dt * 1.35f;
        if (input.CyclePitch) _pitchIndex = (_pitchIndex + 1) % _pitches.Length;
        if (input.StarPressed && (playerPitches ? _match.CanStarPitch : _match.CanStarSwing))
            _starArmed = !_starArmed;

        if (playerPitches)
        {
            if (input.Charge) _charge = Math.Min(1, _charge + dt / 0.55f);
            else _charge = Math.Max(0, _charge - dt * 1.4f);
            if (input.ConfirmPressed)
                LaunchPitch(PlayerPitch());
            return;
        }

        if (_phaseT > (_demo ? 0.12f : 0.55f))
            LaunchPitch(_match.CpuPitch());
    }

    PitchCommand PlayerPitch()
    {
        var timing = (Bounce(_pip) - 0.5f) * 18f;
        var star = _starArmed && _match.CanStarPitch;
        return new PitchCommand(_pitches[_pitchIndex], _charge, timing, star);
    }

    static float Bounce(float t)
    {
        var x = t % 2f;
        return x < 1f ? x : 2f - x;
    }

    void LaunchPitch(PitchCommand pitch)
    {
        _pitch = pitch;
        var mph = AtBatResolver.PitchSpeedMph(pitch, _match.Pitcher.Stats.Pitch);
        _pitchDur = (float)(Diamond.Mound / (mph * 1.4667));
        _pitchDur = Math.Clamp(_pitchDur, 0.32f, 0.85f);
        _flightAge = 0;
        _playerSwung = false;
        _charge = 0;
        _phase = Phase.Flight;
        _phaseT = 0;
        _ball = new Vector3(0, 5.4f, 60.5f);
        _cam = _match.Top ? WorldView.PitchingCamera() : WorldView.BattingCamera();
    }

    void TickFlight(float dt, FrameInput input)
    {
        _flightAge += dt;
        var u = Math.Clamp(_flightAge / _pitchDur, 0, 1);
        var breakX = _pitch!.Type == "curve" ? MathF.Sin(u * MathF.PI) * (_pitch.Type == "curve" ? 2.8f : 0) : 0;
        if (_pitch.Type == "changeup") breakX = 0;
        var y = 5.4f + (2.4f - 5.4f) * u * u + (_pitch.Type == "changeup" ? -1.2f * u * u : 0);
        var z = 60.5f * (1 - u);
        if (_pitch.Star && _match.Pitcher.StarPitch == "heatball")
            breakX += MathF.Sin(u * 18) * 0.4f;
        _ball = new Vector3(breakX, y, z);
        _trail.Add(_ball);
        if (_trail.Count > 24) _trail.RemoveAt(0);

        var playerBats = !_match.Top && !_demo;
        if (playerBats)
        {
            if (input.StarPressed && _match.CanStarSwing) _starArmed = !_starArmed;
            if (input.Charge) _charge = Math.Min(1, _charge + dt / 0.45f);
            if (input.ConfirmPressed && !_playerSwung)
            {
                _playerSwung = true;
                var frames = ((_flightAge - _pitchDur) * 60);
                _swing = new SwingCommand(true, _charge, frames, _starArmed && _match.CanStarSwing, input.Spray * 18);
            }
        }

        if (u < 1) return;

        _swing ??= PlayerOrCpuSwing();
        ResolvePitch();
    }

    SwingCommand PlayerOrCpuSwing()
    {
        if (_playerSwung && _swing is not null) return _swing;
        if (!_match.Top && !_demo)
            return new SwingCommand(false, _charge, 12, false);
        var inZone = AtBatResolver.PitchInZone(_pitch!, _match.Pitcher.Stats.Pitch);
        return _match.CpuSwing(_pitch!, inZone);
    }

    void ResolvePitch()
    {
        _last = _match.Play(_pitch!, _swing!);
        _banner = Label(_last);
        _sub = _last.Caption;
        var fly = _last.Kind is PlayKind.Single or PlayKind.Double or PlayKind.Triple
            or PlayKind.HomeRun or PlayKind.FlyOut or PlayKind.GroundOut or PlayKind.Foul;
        if (fly && _last.AtBat.ExitVeloMph > 1)
        {
            _hitPath = BallFlight.Trajectory(_last.AtBat.ExitVeloMph, _last.AtBat.LaunchDeg, _match.Park.WindMph);
            _hitT = 0;
            _phase = Phase.InPlay;
            _phaseT = 0;
            _trail.Clear();
            return;
        }
        BeginResult();
    }

    void TickInPlay(float dt)
    {
        _hitT += dt;
        if (_hitPath.Count == 0)
        {
            BeginResult();
            return;
        }
        var p = BallFlight.PointAt(_hitPath, _last!.AtBat.SprayDeg, _hitT);
        _ball = new Vector3((float)p.X, (float)Math.Max(0.6, p.Y), (float)p.Z);
        _trail.Add(_ball);
        if (_trail.Count > 40) _trail.RemoveAt(0);
        _cam = WorldView.FollowCamera(_ball);
        var done = _hitT >= BallFlight.HangTime(_hitPath) + 0.35f;
        if (_last.Kind == PlayKind.HomeRun && _hitT > 2.4f) done = true;
        if (done) BeginResult();
    }

    void BeginResult()
    {
        _phase = Phase.Result;
        _resultT = 0;
        _phaseT = 0;
        _cam = WorldView.HighCamera();
    }

    void BeginGameOver()
    {
        _phase = Phase.GameOver;
        _phaseT = 0;
        _cam = WorldView.HighCamera();
    }

    static string Label(PlayEvent ev) => ev.Kind switch
    {
        PlayKind.HomeRun => "HOME RUN",
        PlayKind.Triple => "TRIPLE",
        PlayKind.Double => "DOUBLE",
        PlayKind.Single => "SINGLE",
        PlayKind.Walk => "WALK",
        PlayKind.Strikeout => "STRIKEOUT",
        PlayKind.FlyOut => "OUT",
        PlayKind.GroundOut => "OUT",
        PlayKind.Foul => "FOUL",
        PlayKind.SwingMiss => "SWING AND A MISS",
        PlayKind.TakeStrike => "STRIKE",
        PlayKind.TakeBall => "BALL",
        _ => ev.Kind.ToString().ToUpperInvariant()
    };

    void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Palette.Sky);
        Raylib.BeginMode3D(_cam);
        WorldView.DrawPark(_match.Park, _last?.Furnace == true && _phase is Phase.InPlay or Phase.Result);
        DrawActors();
        Raylib.EndMode3D();

        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        switch (_phase)
        {
            case Phase.Title:
                Hud.DrawTitle(w, h);
                break;
            case Phase.Lineup:
                Hud.DrawLineup(_match, w);
                break;
            case Phase.GameOver:
                Hud.DrawGameOver(_match, w, h);
                break;
            default:
                var timing = _phase == Phase.Set ? Bounce(_pip)
                    : _phase == Phase.Flight ? 0.15f + 0.35f * Math.Clamp(_flightAge / _pitchDur, 0, 1.5f)
                    : 0;
                Hud.Draw(_match, _pitches[_pitchIndex], _starArmed, _charge, timing,
                    _phase is Phase.Flight or Phase.Set, _banner, _sub);
                break;
        }
        Raylib.EndDrawing();
    }

    void DrawActors()
    {
        var sparkField = _match.Top;
        var defense = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
        foreach (var (pos, who) in defense)
        {
            var p = Diamond.Positions[pos];
            var x = p.X;
            var z = p.Z;
            if (_phase == Phase.InPlay && _last?.Fielder is { } f && f.Id == who.Id)
            {
                var u = Math.Clamp(_hitT / Math.Max(0.2, _last.HangTimeSec), 0, 1);
                x = p.X + (_last.LandingX - p.X) * u;
                z = p.Z + (_last.LandingZ - p.Z) * u;
            }
            var pitching = pos == "P" && _phase is Phase.Set or Phase.Flight;
            WorldView.DrawPerson(x, z, sparkField, false, pitching, 0, who.Captain && _pitch?.Star == true);
        }

        var batter = _match.Batter;
        var batAngle = -40f;
        if (_phase == Phase.Flight && (_playerSwung || _swing?.Swing == true) && _flightAge > _pitchDur - 0.08f)
            batAngle = 50f;
        if (_phase == Phase.InPlay) batAngle = 80f;
        WorldView.DrawPerson(1.6, 0.8, batter.Faction == "spark", true, false, batAngle, _starArmed && !_match.Top);

        if (_match.First is { } r1) WorldView.DrawPerson(Diamond.First.X, Diamond.First.Z, r1.Faction == "spark", false, false, 0, false);
        if (_match.Second is { } r2) WorldView.DrawPerson(Diamond.Second.X, Diamond.Second.Z, r2.Faction == "spark", false, false, 0, false);
        if (_match.Third is { } r3) WorldView.DrawPerson(Diamond.Third.X, Diamond.Third.Z, r3.Faction == "spark", false, false, 0, false);

        if (_phase is Phase.Flight or Phase.InPlay or Phase.Set)
        {
            var heat = _pitch?.Star == true && _match.Pitcher.StarPitch == "heatball" || _last?.Heatball == true;
            var furnace = _last?.Furnace == true;
            WorldView.DrawBall(_ball, heat, furnace, _trail);
        }
    }

    void DemoShots()
    {
        var dir = Path.GetFullPath(Path.Combine(_content.Root, "..", "docs", "images"));
        Directory.CreateDirectory(dir);
        if (_phase == Phase.Lineup && !_shotLineup && _phaseT > 0.3f)
        {
            Grab("lineup.png", dir);
            _shotLineup = true;
        }
        if (_phase == Phase.GameOver && !_shotFinal && _phaseT > 0.4f)
        {
            Grab("final.png", dir);
            _shotFinal = true;
        }
        if (_phase == Phase.InPlay && _last?.Kind == PlayKind.HomeRun && _hitT > 1.0f && _hitT < 1.08f)
            Grab("harbor-diamond.png", dir);
    }

    static void Grab(string name, string dir)
    {
        Raylib.TakeScreenshot(name);
        var src = Path.Combine(Environment.CurrentDirectory, name);
        var dest = Path.Combine(dir, name);
        if (File.Exists(src))
            File.Copy(src, dest, true);
    }
}
