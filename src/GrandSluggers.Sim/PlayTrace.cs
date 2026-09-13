namespace GrandSluggers.Sim;

/// <summary>
/// Frozen per-tick geometry of one play (agent-rails D7 / G4). Observation only: the sim
/// still owns the verdict. A cheap loop greps ball / runner / glove / bag without Unity.
/// </summary>
public sealed record PlayTrace(IReadOnlyList<PlayTraceTick> Ticks, PlayTraceEvent? Completed = null)
{
    public static PlayTrace Empty { get; } = new([], null);

    public static JsonSerializerOptions Json { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public string ToJson(bool indented = false) =>
        JsonSerializer.Serialize(this, indented ? IndentedJson : Json);

    public static PlayTrace Parse(string json) =>
        JsonSerializer.Deserialize<PlayTrace>(json, Json) ?? Empty;

    internal static readonly JsonSerializerOptions IndentedJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
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
    IReadOnlyList<PlayTrace> Plays)
{
    public string ToJson(bool indented = false) =>
        JsonSerializer.Serialize(this, indented ? PlayTrace.IndentedJson : PlayTrace.Json);

    public static PlayTraceLog Parse(string json) =>
        JsonSerializer.Deserialize<PlayTraceLog>(json, PlayTrace.Json)
        ?? new PlayTraceLog(0, "", "", "", []);
}

/// <summary>One live-ball frame. Bags are the diamond; the play is typed facts, never a caption to branch on.</summary>
public sealed record PlayTraceTick(
    int I,
    double T,
    PlayTraceBall Ball,
    PlayTraceGlove Glove,
    IReadOnlyList<PlayTraceRunner> Runners,
    IReadOnlyList<PlayTraceBag> Bags,
    PlayTracePlay Play)
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
        new PlayTraceBall(live.BallX, live.BallY, live.BallZ, live.HasBall, live.Throwing, live.CatchMade || live.Caught, live.Fly,
            live.Throwing ? live.ThrowBag : null),
        new PlayTraceGlove(live.GlovePos, live.GloveX, live.GloveZ, live.HasBall),
        live.Runners.Where(r => r.Live).Select(PlayTraceRunner.Of).ToArray(),
        DiamondBags,
        PlayTracePlay.Capture(live, completed));
}

public sealed record PlayTraceBall(
    double X,
    double Y,
    double Z,
    bool Held,
    bool Throwing,
    bool Caught,
    FlyState Fly,
    int? ThrowBag = null);

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
    bool Armed)
{
    public static PlayTraceRunner Of(Runner r)
    {
        var (x, z) = r.Position;
        return new(r.Who.Id, r.FromBag, r.Bag, r.DestBag, x, z, r.Feet, r.Phase, r.Broke, r.Forced, r.StealArmed);
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

    public int Count => _ticks.Count;

    public void Clear()
    {
        _ticks.Clear();
        _completed = null;
    }

    public void Record(LivePlaySystem live, PlayEvent? completed = null)
    {
        _ticks.Add(PlayTraceTick.Capture(live, _ticks.Count, completed));
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

    public PlayTrace Freeze() => new(_ticks.ToArray(), _completed);
}
