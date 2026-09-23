namespace GrandSluggers.Sim;

/// <summary>
/// One <see cref="HazardPattern.StatusVolume"/> of the park a play is on, as that play reads it (§14; FR-07, FD-08-R2;
/// F4-b, #896): the park's instance <see cref="Hazard"/> (its index in <see cref="Park.Hazards"/>), its type, its centre,
/// the disc it covers — the instance's radius × the type row's <c>nightRadiusMul</c> at night — and how long a touch
/// slows the body that made it (the row's <c>slowSec</c>).
/// </summary>
public sealed record StatusVolume(int Hazard, string Type, double X, double Z, double RadiusFt, double SlowSec)
{
    /// <summary>A body standing at (<paramref name="x"/>, <paramref name="z"/>) is touching the volume. A body on the rim touches it.</summary>
    public bool Contains(double x, double z) => Diamond.Dist(X, Z, x, z) <= RadiusFt;
}

/// <summary>
/// A body touched a status volume and is slowed (FR-07, FD-08-R2; F4-b, #896): the typed live fact the play trace records
/// (<see cref="PlayTraceMarkKind.BodySlowed"/>) and presentation reads (F8-b), never a caption. <see cref="Pos"/> is the
/// fielding position of the body, or <see cref="FieldBody.Runner"/> for a runner; <see cref="Who"/> is the character.
/// <see cref="Hazard"/> is the instance's index in the match's <see cref="Park.Hazards"/>. <see cref="T"/> is the play
/// second of the touch, and the body runs slowed at least until <see cref="UntilT"/> — longer while it stays inside.
/// </summary>
public sealed record BodySlowed(string Pos, Character Who, int Hazard, string Type, double T, double UntilT)
{
    public bool IsRunner => Pos == FieldBody.Runner;
}

/// <summary>
/// The status volume, live and per body (§14; FR-07, FD-08-R1, FD-08-R2; F4-b, #896). A volume acts on the body that
/// touches it, when it touches it, for a stated time — never on a play from where the ball lands, and never on anyone
/// else.
///
/// <para>
/// <b>The touch.</b> Once a frame, before anybody moves, the live ball reads every fielder and every live runner where the
/// last frame left it (<see cref="Read"/>). A body inside a volume it was not inside at the last read has touched it:
/// that is a <see cref="BodySlowed"/>, and the body's slow runs to the touch plus the row's <c>slowSec</c> (3.0 s, Jack's
/// number). A later touch starts the time again from itself; it never shortens a time already running. A body that
/// starts a play inside a volume touches it at the first read.
/// </para>
///
/// <para>
/// <b>The slow.</b> A body is slowed for the frame when it stands inside a volume, or when the frame starts before its
/// time runs out: so staying inside keeps it slowed, and leaving lets the time run out. Slowed, every step the body takes
/// is at <c>fielding.chase.frozenMul</c> (0.45) of the speed it was asked for — a fielder's chase, carry, cover, cutoff or
/// backup walk, a runner's run along his path — and nothing else about it changes: not its reach, its read, its throw
/// or its catch (SF-22). The time is counted in frames of the one play clock, like every other clock of the live ball: a
/// frame that starts inside the time is slowed through its end.
/// </para>
///
/// <para>
/// <b>Who is never slowed.</b> A body with <see cref="FieldAbilities.IgnoresParkSlow"/> (Burrow) touches nothing: no
/// event, no slow, whether it is fielding or running. Nobody plans for a slow: the fielding preview, the CPU's route and
/// every estimate of arrival read the full speed (the CPU's cost of a volume is F4-g's).
/// </para>
/// </summary>
public sealed class BodySlows
{
    const double Eps = 1e-9;

    sealed class State
    {
        public readonly HashSet<int> Inside = [];
        public double Until = double.NegativeInfinity;
        public bool Slowed;
    }

    IReadOnlyList<StatusVolume> _volumes = [];
    /// <summary>The volumes the body being read stands in: one set reused for every read, so a frame allocates nothing.</summary>
    readonly HashSet<int> _inside = [];
    readonly Dictionary<string, State> _fielders = new(StringComparer.OrdinalIgnoreCase);
    // A Runner is a body, compared by reference (the class does not override equality; netstandard 2.1 has no ReferenceEqualityComparer).
    readonly Dictionary<Runner, State> _runners = new();

    /// <summary>The volumes this play reads. Empty at a park with none, and with hazards off.</summary>
    public IReadOnlyList<StatusVolume> Volumes => _volumes;

    /// <summary>A new play on these volumes: every body starts outside every volume, unslowed.</summary>
    public void Begin(IReadOnlyList<StatusVolume> volumes)
    {
        _volumes = volumes;
        Clear();
    }

    /// <summary>Forget every body (the play is over).</summary>
    public void Clear()
    {
        _fielders.Clear();
        _runners.Clear();
    }

    /// <summary>
    /// Read one fielder where it stands at play second <paramref name="t"/>: the volumes it touched since the last read,
    /// in park order, and whether it runs slowed this frame. An immune body touches nothing.
    /// </summary>
    public IReadOnlyList<(StatusVolume Volume, double UntilT)> Read(string pos, double x, double z, double t, bool immune) =>
        _volumes.Count == 0 ? [] : Read(Of(_fielders, pos), x, z, t, immune);

    /// <summary>Read one runner where he stands at play second <paramref name="t"/>, the same way as a fielder.</summary>
    public IReadOnlyList<(StatusVolume Volume, double UntilT)> Read(Runner runner, double x, double z, double t, bool immune) =>
        _volumes.Count == 0 ? [] : Read(Of(_runners, runner), x, z, t, immune);

    /// <summary>The fielder at <paramref name="pos"/> runs slowed this frame.</summary>
    public bool Slowed(string pos) => _fielders.TryGetValue(pos, out var s) && s.Slowed;

    /// <summary>This runner runs slowed this frame.</summary>
    public bool Slowed(Runner runner) => _runners.TryGetValue(runner, out var s) && s.Slowed;

    /// <summary>What a step of a body is multiplied by: <c>fielding.chase.frozenMul</c> slowed, else exactly 1.</summary>
    public static double Mul(bool slowed, RulesTable? rules = null) => slowed ? Rules.Or(rules).Fielding.Chase.FrozenMul : 1.0;

    static State Of<TKey>(Dictionary<TKey, State> bodies, TKey key) where TKey : notnull
    {
        if (!bodies.TryGetValue(key, out var s)) bodies[key] = s = new State();
        return s;
    }

    IReadOnlyList<(StatusVolume Volume, double UntilT)> Read(State s, double x, double z, double t, bool immune)
    {
        if (immune)
        {
            s.Inside.Clear();
            s.Slowed = false;
            return [];
        }
        List<(StatusVolume, double)>? touched = null;
        _inside.Clear();
        foreach (var v in _volumes)
        {
            if (!v.Contains(x, z)) continue;
            _inside.Add(v.Hazard);
            if (s.Inside.Contains(v.Hazard)) continue;
            s.Until = Math.Max(s.Until, t + v.SlowSec);
            (touched ??= []).Add((v, s.Until));
        }
        s.Inside.Clear();
        s.Inside.UnionWith(_inside);
        s.Slowed = s.Inside.Count > 0 || t < s.Until - Eps;
        return touched is null ? [] : touched;
    }
}
