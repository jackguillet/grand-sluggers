namespace GrandSluggers.Sim;

/// <summary>
/// Frozen per-tick geometry of one play (agent-rails D7 / G4). Observation only: the sim
/// still owns the verdict. A cheap loop greps ball / runner / glove / bag without Unity.
/// </summary>
public sealed record PlayTrace(IReadOnlyList<PlayTraceTick> Ticks, PlayTraceEvent? Completed = null,
    int SchemaVersion = 1, PlayTraceContext? Context = null,
    IReadOnlyList<PlayTraceCommand>? Commands = null, IReadOnlyList<PlayTraceMark>? Marks = null)
{
    public static PlayTrace Empty { get; } = new([], null);

    public static JsonSerializerOptions Json { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public string ToJson(bool indented = false) =>
        JsonSerializer.Serialize(this, indented ? IndentedJson : Json);

    public static PlayTrace Parse(string json)
    {
        var trace = JsonSerializer.Deserialize<PlayTrace>(json, Json) ?? throw new JsonException("Trace must be an object");
        CheckVersion(trace.SchemaVersion);
        return trace;
    }

    internal static void CheckVersion(int version)
    {
        if (version is not (1 or 2)) throw new JsonException($"Unsupported play trace schema {version}");
    }

    internal static readonly JsonSerializerOptions IndentedJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
}

/// <summary>Every play in a headless match, as <c>cli match --trace</c> writes it.</summary>
public sealed record PlayTraceLog(
    int Seed,
    string Home,
    string Away,
    string Park,
    IReadOnlyList<PlayTrace> Plays,
    int SchemaVersion = 1,
    PlayTraceIdentity? Identity = null,
    PlayTraceTrial? Trial = null)
{
    public string ToJson(bool indented = false) =>
        JsonSerializer.Serialize(this, indented ? PlayTrace.IndentedJson : PlayTrace.Json);

    public static PlayTraceLog Parse(string json)
    {
        var log = JsonSerializer.Deserialize<PlayTraceLog>(json, PlayTrace.Json) ?? throw new JsonException("Trace log must be an object");
        PlayTrace.CheckVersion(log.SchemaVersion);
        foreach (var play in log.Plays) PlayTrace.CheckVersion(play.SchemaVersion);
        return log;
    }
}

/// <summary>
/// The trial overlay a trace was produced under (#716). Absent from a control trace, so a control
/// run's bytes are what they always were, and present on a trial's, so a saved trace carries its
/// own provenance instead of relying on whoever ran it having kept the stderr line. The overlay is
/// named the way a run names it — <c>trials/example</c> — so a trial authored in the repository
/// attributes the same way on any machine.
/// </summary>
public sealed record PlayTraceTrial(string Overlay, IReadOnlyList<string> Files)
{
    public static PlayTraceTrial? For(DataRoot root) =>
        root.OverlayName is { } name ? new PlayTraceTrial(name, root.Overrides) : null;
}

/// <summary>One live-ball frame. Bags are the diamond; the play is typed facts, never a caption to branch on.</summary>
public sealed record PlayTraceTick(
    int I,
    double T,
    PlayTraceBall Ball,
    PlayTraceGlove Glove,
    IReadOnlyList<PlayTraceRunner> Runners,
    IReadOnlyList<PlayTraceBag> Bags,
    PlayTracePlay Play, IReadOnlyList<PlayTraceFielder>? Fielders = null,
    IReadOnlyList<PlayTraceCoverage>? Coverage = null, bool Paused = false)
{
    public static IReadOnlyList<PlayTraceBag> DiamondBags { get; } =
    [
        new(1, Diamond.First.X, Diamond.First.Z),
        new(2, Diamond.Second.X, Diamond.Second.Z),
        new(3, Diamond.Third.X, Diamond.Third.Z),
        new(4, Diamond.Home.X, Diamond.Home.Z)
    ];

    public static PlayTraceTick Capture(LivePlaySystem live, int i, PlayEvent? completed = null) => new(
        i,
        live.ElapsedSeconds,
        new PlayTraceBall(live.BallX, live.BallY, live.BallZ, live.TraceHoldsBall, live.Throwing, live.CatchMade || live.Caught, live.Fly,
            live.Throwing ? live.ThrowBag : null, live.LooseBall, live.Lobbing,
            live.Throwing ? live.ThrowT : null, live.Throwing ? live.ThrowDur : null),
        new PlayTraceGlove(live.GlovePos, live.GloveX, live.GloveZ, live.TraceHoldsBall),
        live.Runners.Select(r => PlayTraceRunner.Of(r, live.IsSlowed(r))).ToArray(),
        DiamondBags,
        PlayTracePlay.Capture(live, completed), live.TraceFielders(), live.TraceCoverage(), live.Paused);
}

public sealed record PlayTraceBall(
    double X,
    double Y,
    double Z,
    bool Held,
    bool Throwing,
    bool Caught,
    FlyState Fly,
    int? ThrowBag = null, bool Loose = false, bool WaitingForCover = false, double? ThrowElapsedSec = null, double? ThrowDurationSec = null);

public sealed record PlayTraceGlove(string Pos, double X, double Z, bool HasBall);

public sealed record PlayTraceRunner(
    string Id,
    int From,
    int Bag,
    int Dest,
    double X,
    double Z,
    double Feet,
    RunnerPhase Phase,
    bool Broke,
    bool Forced,
    bool Armed, bool Live = true, double? LastTouchAt = null, double Velocity = 0, bool Held = false,
    /// <summary>True on a frame a park's status volume slowed this runner (F4-b, #896); absent otherwise.</summary>
    bool? Slowed = null)
{
    public static PlayTraceRunner Of(Runner r) => Of(r, slowed: false);

    public static PlayTraceRunner Of(Runner r, bool slowed)
    {
        var (x, z) = r.Position;
        return new(r.Who.Id, r.FromBag, r.Bag, r.DestBag, x, z, r.Feet, r.Phase, r.Broke, r.Forced, r.StealArmed, r.Live, double.IsNaN(r.LastTouchAt) ? null : r.LastTouchAt, r.Velocity, r.Held,
            slowed ? true : null);
    }
}

public sealed record PlayTraceBag(int Bag, double X, double Z);

/// <summary>The live play as typed facts this frame. Caption is presentation; tests must not branch on it.</summary>
public sealed record PlayTracePlay(
    PlayKind Kind,
    string Caption,
    InPlay.ThrowVerdict? Verdict,
    int? VerdictBag,
    PlayTraceEvent? Completed)
{
    public static PlayTracePlay Capture(LivePlaySystem live, PlayEvent? completed)
    {
        var moment = live.LastMoment;
        return new(
            live.PlayKind,
            live.Caption,
            moment?.Verdict,
            moment is null ? null : moment.Bag,
            completed is null ? null : PlayTraceEvent.From(completed));
    }
}

public sealed record PlayTraceEvent(
    PlayKind Kind,
    string Caption,
    IReadOnlyList<PlayTraceOut> Outs,
    IReadOnlyList<PlayTraceMove> Moves,
    int BatterToBag,
    bool Error,
    bool FieldersChoice,
    RunnerPlayResult RunnerResult)
{
    public static PlayTraceEvent From(PlayEvent ev)
    {
        var o = ev.Outcome ?? PlayOutcome.Empty;
        return new(
            ev.Kind,
            ev.Caption,
            o.OutsMade.Select(x => new PlayTraceOut(x.Type, x.Bag, x.FromBag, x.Runner.Id, x.Fielder?.Id)).ToArray(),
            o.Moves.Select(m => new PlayTraceMove(m.Runner.Id, m.FromBag, m.ToBag)).ToArray(),
            o.BatterToBag,
            o.Error,
            o.FieldersChoice,
            o.RunnerResult);
    }
}

public sealed record PlayTraceOut(OutType Type, int Bag, int From, string Runner, string? Fielder);

public sealed record PlayTraceMove(string Runner, int From, int To);

/// <summary>
/// Opt-in buffer the live ball writes. Off, it allocates nothing per tick. The dump does not
/// feed back into baseball: no rule reads it, and Recording must not change a PlayEvent.
/// </summary>
public sealed class PlayTraceRecorder
{
    readonly List<PlayTraceTick> _ticks = [];
    PlayTraceEvent? _completed;
    PlayTraceContext? _context;
    readonly List<PlayTraceCommand> _commands = [];
    readonly List<PlayTraceMark> _marks = [];
    int _leg;
    double _commandStart;
    internal LivePlaySystem? Source { get; set; }
    public int Leg => _leg;
    public LivePlayCommand? CurrentCommand => _commands.Count == 0 ? null : _commands[^1].Input;
    public double CommandStart => _commandStart;

    public void Begin(PlayTraceContext context)
    {
        Clear();
        _context = context;
    }

    public void Command(LivePlayCommand command, double t)
    {
        _commandStart = t;
        _commands.Add(new(_commands.Count, t, command));
    }

    public void Mark(PlayTraceMarkKind kind, double t, string? fielder = null, int? bag = null,
        PlayTraceRunner? runner = null, OutType? outType = null, double? predictedRunnerAt = null,
        PlayTraceThrow? flight = null, InPlay.ThrowVerdict? verdict = null, PlayTraceHazard? hazard = null)
    {
        if (kind == PlayTraceMarkKind.ThrowRelease) _leg++;
        _marks.Add(new(_marks.Count, _commands.Count - 1, kind, t,
            kind is PlayTraceMarkKind.RunnerArrival ? _commandStart : t,
            _leg == 0 ? null : _leg, fielder, bag, runner, outType, predictedRunnerAt, flight, verdict, Source?.TraceMarkGeometry(), hazard));
    }

    public int Count => _ticks.Count;

    public void Clear()
    {
        _ticks.Clear();
        _completed = null;
        _context = null;
        _commands.Clear();
        _marks.Clear();
        _leg = 0;
        _commandStart = 0;
    }

    public void Record(LivePlaySystem live, PlayEvent? completed = null)
    {
        var tick = PlayTraceTick.Capture(live, _ticks.Count, completed);
        if (_ticks.Count > 0 && tick.T > _ticks[^1].T && tick.Fielders is not null)
        {
            var previous = _ticks[^1];
            var dt = tick.T - previous.T;
            tick = tick with { Fielders = tick.Fielders.Select(f =>
            {
                var old = previous.Fielders?.FirstOrDefault(p => p.Pos == f.Pos);
                return old is null ? f : f with { ObservedVx = (f.X - old.X) / dt, ObservedVz = (f.Z - old.Z) / dt };
            }).ToArray() };
        }
        _ticks.Add(tick);
        if (completed is not null) Complete(completed);
    }

    public void Complete(PlayEvent ev)
    {
        _completed = PlayTraceEvent.From(ev);
        if (_ticks.Count == 0) return;
        var last = _ticks[^1];
        if (last.Play.Completed is null)
            _ticks[^1] = last with { Play = last.Play with { Completed = _completed } };
    }

    public PlayTrace Freeze() => new(_ticks.ToArray(), _completed, 2, _context, _commands.ToArray(), _marks.ToArray());
}
