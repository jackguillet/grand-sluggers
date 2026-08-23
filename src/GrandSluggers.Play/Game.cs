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
    readonly string[] _parks = ["harbor-diamond", "crystal-rink"];

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
    string _parkId;
    bool _two;
    AtBatResult? _pendingHit;
    FieldingPreview? _preview;
    double _fx, _fz;
    bool _playerFielding;
    bool _caught;
    bool _buddyJump;
    bool _frozenSlow;

    public Game(bool demo, int seed, string parkId = "harbor-diamond", bool two = false)
    {
        _demo = demo;
        _seed = seed;
        _parkId = parkId;
        _two = two;
        _content = ContentCatalog.Load();
        _match = NewMatch(seed);
        _cam = WorldView.HighCamera();
    }

    Match NewMatch(int seed) => Match.Slice(_content, 3, seed, _parkId);

    bool HumanPitches => !_demo && (_two || _match.Top);
    bool HumanBats => !_demo && (_two || !_match.Top);
    bool HumanFields => HumanPitches;

    FrameInput PitchPad(FrameInput p1, FrameInput p2) =>
        _two && !_match.Top ? p2 : p1;

    FrameInput BatPad(FrameInput p1, FrameInput p2) =>
        _two && _match.Top ? p2 : p1;

    public void Run()
    {
        const int w = 1600, h = 900;
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint);
        Raylib.InitWindow(w, h, "Grand Sluggers");
        Raylib.SetTargetFPS(60);
        Raylib.SetExitKey(KeyboardKey.Null);

        if (_demo)
            BeginLineup();

        while (!Raylib.WindowShouldClose())
        {
            var dt = Raylib.GetFrameTime();
            if (_demo) dt *= 3.2f;
            var p1 = PlayerInput.ReadP1();
            var p2 = _two ? PlayerInput.ReadP2() : default;
            if (p1.Quit && !_demo) break;
            Tick(dt, p1, p2);
            Draw();
            if (_demo) DemoShots();
            if (_demo && _phase == Phase.GameOver && _phaseT > 2.2f) break;
        }

        Raylib.CloseWindow();
        if (_demo)
        {
            Console.WriteLine(_match.BoxLine());
            var mvp = _match.Mvp();
            Console.WriteLine($"MVP {mvp.Who.Name} ({mvp.Points}) - {mvp.Why}");
        }
    }

    public void Dispose() { }

    void Tick(float dt, FrameInput p1, FrameInput p2)
    {
        _phaseT += dt;
        switch (_phase)
        {
            case Phase.Title:
                if (p1.TogglePark)
                    _parkId = _parkId == "harbor-diamond" ? "crystal-rink" : "harbor-diamond";
                if (p1.ToggleTwoPlayer) _two = !_two;
                if (p1.ConfirmPressed || _demo && _phaseT > 0.6f)
                {
                    _match = NewMatch(_seed);
                    BeginLineup();
                }
                break;
            case Phase.Lineup:
                if (p1.ConfirmPressed || _demo && _phaseT > 1.4f) BeginSet();
                break;
            case Phase.Set:
                TickSet(dt, PitchPad(p1, p2), BatPad(p1, p2));
                break;
            case Phase.Flight:
                TickFlight(dt, PitchPad(p1, p2), BatPad(p1, p2));
                break;
            case Phase.InPlay:
                TickInPlay(dt, PitchPad(p1, p2));
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
                if (p1.ConfirmPressed)
                {
                    _match = NewMatch(_seed + 1);
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
        _pendingHit = null;
        _preview = null;
        _playerFielding = false;
        _caught = false;
        _buddyJump = false;
        _trail.Clear();
        _starArmed = false;
        _banner = "";
        _sub = "";
        _ball = new Vector3(0, 5.4f, 60.5f);
        _cam = _match.Top ? WorldView.PitchingCamera() : WorldView.BattingCamera();
    }

    void TickSet(float dt, FrameInput pitcher, FrameInput batter)
    {
        _pip += dt * 1.35f;
        if (pitcher.CyclePitch) _pitchIndex = (_pitchIndex + 1) % _pitches.Length;
        if (pitcher.Swap) _match.SwapPitcher();
        if (HumanPitches && pitcher.StarPressed && _match.CanStarPitch)
            _starArmed = !_starArmed;
        if (HumanBats && batter.StarPressed && _match.CanStarSwing)
            _starArmed = !_starArmed;

        if (HumanPitches)
        {
            if (pitcher.Charge) _charge = Math.Min(1, _charge + dt / 0.55f);
            else _charge = Math.Max(0, _charge - dt * 1.4f);
            if (pitcher.ConfirmPressed)
                LaunchPitch(PlayerPitch(pitcher));
            return;
        }

        if (_phaseT > (_demo ? 0.12f : 0.55f))
            LaunchPitch(_match.CpuPitch());
    }

    PitchCommand PlayerPitch(FrameInput input)
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

    void TickFlight(float dt, FrameInput pitcher, FrameInput batter)
    {
        _flightAge += dt;
        var u = Math.Clamp(_flightAge / _pitchDur, 0, 1);
        var breakX = _pitch!.Type == "curve" ? MathF.Sin(u * MathF.PI) * 2.8f : 0;
        var y = 5.4f + (2.4f - 5.4f) * u * u + (_pitch.Type == "changeup" ? -1.2f * u * u : 0);
        var z = 60.5f * (1 - u);
        if (_pitch.Star && _match.Pitcher.StarPitch == "heatball")
            breakX += MathF.Sin(u * 18) * 0.4f;
        _ball = new Vector3(breakX, y, z);
        _trail.Add(_ball);
        if (_trail.Count > 24) _trail.RemoveAt(0);

        if (HumanBats)
        {
            if (batter.StarPressed && _match.CanStarSwing) _starArmed = !_starArmed;
            if (batter.Charge) _charge = Math.Min(1, _charge + dt / 0.45f);
            if (batter.ConfirmPressed && !_playerSwung)
            {
                _playerSwung = true;
                var frames = (_flightAge - _pitchDur) * 60;
                _swing = new SwingCommand(true, _charge, frames, _starArmed && _match.CanStarSwing, batter.Spray * 18);
            }
        }

        if (u < 1) return;
        _swing ??= PlayerOrCpuSwing(batter);
        ResolvePitch();
    }

    SwingCommand PlayerOrCpuSwing(FrameInput batter)
    {
        if (_playerSwung && _swing is not null) return _swing;
        if (HumanBats)
            return new SwingCommand(false, _charge, 12, false);
        var inZone = AtBatResolver.PitchInZone(_pitch!, _match.Pitcher.Stats.Pitch);
        return _match.CpuSwing(_pitch!, inZone);
    }

    void ResolvePitch()
    {
        if (HumanFields)
        {
            if (!_match.BeginAtBat(_pitch!, _swing!, out var hit, out var finished))
            {
                _last = finished;
                _banner = Label(_last!);
                _sub = _last!.Caption;
                if (_last.Kind == PlayKind.Foul && _last.AtBat.ExitVeloMph > 1)
                    StartFly(hit: _last.AtBat, playerField: false);
                else
                    BeginResult();
                return;
            }
            _pendingHit = hit;
            _preview = _match.PreviewHit(hit);
            var start = Diamond.Positions[_preview.Position];
            _fx = start.X;
            _fz = start.Z;
            _frozenSlow = _preview.Frozen;
            _playerFielding = true;
            _caught = false;
            _buddyJump = false;
            StartFly(hit, playerField: true);
            return;
        }

        _last = _match.Play(_pitch!, _swing!);
        _banner = Label(_last);
        _sub = _last.Caption;
        var fly = _last.Kind is PlayKind.Single or PlayKind.Double or PlayKind.Triple
            or PlayKind.HomeRun or PlayKind.FlyOut or PlayKind.GroundOut or PlayKind.Foul;
        if (fly && _last.AtBat.ExitVeloMph > 1)
            StartFly(_last.AtBat, playerField: false);
        else
            BeginResult();
    }

    void StartFly(AtBatResult hit, bool playerField)
    {
        _hitPath = BallFlight.Trajectory(hit.ExitVeloMph, hit.LaunchDeg, _match.Park.WindMph);
        _hitT = 0;
        _phase = Phase.InPlay;
        _phaseT = 0;
        _playerFielding = playerField;
        _trail.Clear();
    }

    void TickInPlay(float dt, FrameInput field)
    {
        _hitT += dt;
        if (_hitPath.Count == 0)
        {
            BeginResult();
            return;
        }
        var spray = _pendingHit?.SprayDeg ?? _last!.AtBat.SprayDeg;
        var p = BallFlight.PointAt(_hitPath, spray, _hitT);
        _ball = new Vector3((float)p.X, (float)Math.Max(0.6, p.Y), (float)p.Z);
        _trail.Add(_ball);
        if (_trail.Count > 40) _trail.RemoveAt(0);
        _cam = WorldView.FollowCamera(_ball);

        if (_playerFielding && _preview is { } pre && _pendingHit is { } hit)
        {
            var speed = (18 + pre.Fielder.Stats.Run * 1.8) * (_frozenSlow ? 0.4 : 1);
            _fx += field.MoveX * speed * dt;
            _fz += field.MoveZ * speed * dt;
            if (field.Jump && pre.Buddy is not null && pre.HomeRunLikely)
                _buddyJump = true;
            if (field.ConfirmPressed)
            {
                var d = Diamond.Dist(_fx, _fz, _ball.X, _ball.Z);
                if (d < pre.CatchRadius + 4) _caught = true;
            }

            var hang = BallFlight.HangTime(_hitPath);
            if (_hitT >= hang && !_caught && !_buddyJump)
            {
                var d = Diamond.Dist(_fx, _fz, pre.LandingX, pre.LandingZ);
                if (d < pre.CatchRadius + 6) _caught = true;
            }

            if (_hitT >= hang + 0.15f)
            {
                Character? cut = null;
                ThrowResult? thr = null;
                if (field.ThrowBase is > 0 and < 5 && _caught)
                {
                    var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
                    cut = field.ThrowBase switch
                    {
                        1 => map.GetValueOrDefault("1B"),
                        2 => map.GetValueOrDefault("2B"),
                        3 => map.GetValueOrDefault("3B"),
                        _ => map.GetValueOrDefault("C")
                    };
                    if (cut is not null)
                        thr = _match.ThrowBetween(pre.Fielder, cut);
                }

                FieldingResult result;
                if (_buddyJump || _caught)
                {
                    var kind = pre.Grounder ? PlayKind.GroundOut : PlayKind.FlyOut;
                    result = new FieldingResult(kind, pre.Fielder, cut, pre.HangTimeSec, pre.LandingX, pre.LandingZ,
                        pre.Heatball, pre.Furnace, thr, pre.Buddy);
                }
                else if (pre.HomeRunLikely)
                {
                    result = new FieldingResult(PlayKind.HomeRun, pre.Fielder, null, pre.HangTimeSec, pre.LandingX, pre.LandingZ,
                        pre.Heatball, pre.Furnace, Buddy: pre.Buddy);
                }
                else
                {
                    var kind = hit.CarryFt >= 250 ? PlayKind.Double : PlayKind.Single;
                    result = new FieldingResult(kind, pre.Fielder, null, pre.HangTimeSec, pre.LandingX, pre.LandingZ,
                        pre.Heatball, pre.Furnace, Buddy: pre.Buddy);
                }

                _last = _match.FinishAtBat(_pitch!, _swing!, hit, result);
                _banner = Label(_last);
                _sub = _last.Caption;
                _playerFielding = false;
                _pendingHit = null;
                BeginResult();
            }
            return;
        }

        var done = _hitT >= BallFlight.HangTime(_hitPath) + 0.35f;
        if (_last?.Kind == PlayKind.HomeRun && _hitT > 2.4f) done = true;
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
        PlayKind.FlyOut when ev.Caption.Contains("BUDDY") => "BUDDY JUMP",
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
        Raylib.ClearBackground(_match.Park.Surface == "ice" ? Palette.C(168, 198, 222) : Palette.Sky);
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
                var park = _parkId == "crystal-rink" ? "Crystal Rink" : "Harbor Diamond";
                var mode = _two ? "2 PLAYER  (P1 Spark, P2 Ember)" : "1 PLAYER  (you are Spark)";
                Raylib.DrawText($"Park: {park}   Mode: {mode}", 84, 580, 22, Palette.SparkDark);
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
                if (_playerFielding && _preview?.Buddy is not null && _preview.HomeRunLikely)
                    Raylib.DrawText("F  BUDDY JUMP", w / 2 - 90, h - 40, 22, Palette.Gold);
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
            var controlled = _playerFielding && _preview is { } pre && who.Id == pre.Fielder.Id;
            if (controlled)
            {
                x = _fx;
                z = _fz;
                WorldView.DrawRing(x, z, 4.5f, Palette.Gold);
            }
            else if (_phase == Phase.InPlay && !_playerFielding && _last?.Fielder is { } f && f.Id == who.Id)
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
        WorldView.DrawPerson(1.6, 0.8, batter.Faction == "spark", true, false, batAngle, _starArmed && HumanBats);

        if (_match.First is { } r1) WorldView.DrawPerson(Diamond.First.X, Diamond.First.Z, r1.Faction == "spark", false, false, 0, false);
        if (_match.Second is { } r2) WorldView.DrawPerson(Diamond.Second.X, Diamond.Second.Z, r2.Faction == "spark", false, false, 0, false);
        if (_match.Third is { } r3) WorldView.DrawPerson(Diamond.Third.X, Diamond.Third.Z, r3.Faction == "spark", false, false, 0, false);

        if (_phase is Phase.Flight or Phase.InPlay or Phase.Set)
        {
            var heat = _pitch?.Star == true && _match.Pitcher.StarPitch == "heatball" || _last?.Heatball == true;
            var furnace = _last?.Furnace == true || _preview?.Furnace == true;
            WorldView.DrawBall(_ball, heat, furnace, _trail);
        }
    }

    void DemoShots()
    {
        var dir = Path.GetFullPath(Path.Combine(_content.Root, "..", "docs", "images"));
        Directory.CreateDirectory(dir);
        if (_phase == Phase.Lineup && !_shotLineup && _phaseT > 0.3f)
        {
            Grab(_parkId == "crystal-rink" ? "crystal-rink.png" : "lineup.png", dir);
            _shotLineup = true;
        }
        if (_phase == Phase.GameOver && !_shotFinal && _phaseT > 0.4f)
        {
            Grab("final.png", dir);
            _shotFinal = true;
        }
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
