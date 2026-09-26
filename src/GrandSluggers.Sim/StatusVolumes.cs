namespace GrandSluggers.Sim;

/// <summary>
/// One <see cref="HazardPattern.StatusVolume"/> of the park a play is on, as that play reads it (§14; FR-07, FD-08-R2;
/// F4-b, #896): the park's instance <see cref="Hazard"/> (its index in <see cref="Park.Hazards"/>), its type, its centre,
/// the disc it covers — the instance's radius × the type row's <c>nightRadiusMul</c> at night — and how long a touch
/// slows the body that made it (the row's <c>slowSec</c>).
///
/// <para>
/// A star's volume (§13, Undertow: <see cref="PitchUndertow"/>) is the same disc with three fields a park's never sets: its own
/// <see cref="SlowMul"/> (a park's is <c>fielding.chase.frozenMul</c>), a play second <see cref="UntilT"/> after which it is gone,
/// and <see cref="BatterRunnerOnly"/>, a disc only the batter-runner touches. Its <see cref="Hazard"/> is <see cref="StarHazard"/>.
/// A Dust Bowl's (§13, <see cref="SwingDustBowl"/>) is raised mid-play, has its own negative <see cref="Hazard"/> and sets
/// <see cref="FieldersOnly"/>, a disc no runner touches.
/// </para>
/// </summary>
public sealed record StatusVolume(int Hazard, string Type, double X, double Z, double RadiusFt, double SlowSec,
    double? SlowMul = null, double UntilT = double.PositiveInfinity, bool BatterRunnerOnly = false, bool FieldersOnly = false)
{
    /// <summary>The <see cref="Hazard"/> of a volume a star made, not a park: it indexes no <see cref="Park.Hazards"/>.</summary>
    public const int StarHazard = -1;

    /// <summary>A body standing at (<paramref name="x"/>, <paramref name="z"/>) is touching the volume. A body on the rim touches it.</summary>
    public bool Contains(double x, double z) => Diamond.Dist(X, Z, x, z) <= RadiusFt;

    /// <summary>The volume is there at play second <paramref name="t"/>: a park's always, a star's until <see cref="UntilT"/>.</summary>
    public bool ActiveAt(double t) => t < UntilT;

    /// <summary>What a step inside this volume is multiplied by: its own <see cref="SlowMul"/>, else <c>fielding.chase.frozenMul</c>.</summary>
    public double MulOf(RulesTable rules) => SlowMul ?? rules.Fielding.Chase.FrozenMul;

    /// <summary>The volume is the park's own (a <see cref="Hazard"/> index into <see cref="Park.Hazards"/>), not a star's.</summary>
    public bool OfThePark => Hazard >= 0;
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
/// event, no slow, whether it is fielding or running. The fielding preview and every estimate of arrival read the full speed;
/// the step toward a goal may go around a volume (<see cref="VolumeRoute"/>).
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
        /// <summary>This frame's slow: whether a park's (<c>frozenMul</c>) disc or time holds the body, and the least star <see cref="StatusVolume.SlowMul"/>.</summary>
        public bool Frozen;
        public double OwnMul = 1;
        /// <summary>The same two for the time that runs after the body leaves (<see cref="Until"/>).</summary>
        public bool UntilFrozen;
        public double UntilOwnMul = 1;
    }

    IReadOnlyList<StatusVolume> _volumes = [];
    IReadOnlyList<StatusVolume> _fielderVolumes = [];
    IReadOnlyList<StatusVolume> _parkVolumes = [];
    /// <summary>The volumes the body being read stands in: one set reused for every read, so a frame allocates nothing.</summary>
    readonly HashSet<int> _inside = [];
    readonly Dictionary<string, State> _fielders = new(StringComparer.OrdinalIgnoreCase);
    // A Runner is a body, compared by reference (the class does not override equality; netstandard 2.1 has no ReferenceEqualityComparer).
    readonly Dictionary<Runner, State> _runners = new();

    /// <summary>The volumes this play reads. Empty at a park with none, and with hazards off.</summary>
    public IReadOnlyList<StatusVolume> Volumes => _volumes;

    /// <summary>The volumes a fielder can touch: every one but a batter-runner's own (<see cref="StatusVolume.BatterRunnerOnly"/>). The route reads these.</summary>
    public IReadOnlyList<StatusVolume> FielderVolumes => _fielderVolumes;

    /// <summary>
    /// The park's own volumes this play reads (<see cref="StatusVolume.OfThePark"/>): the discs a route goes around. A star's
    /// disc — the Undertow's ring, a Dust Bowl — is never routed around; going round it is the player's own verb.
    /// </summary>
    public IReadOnlyList<StatusVolume> ParkVolumes => _parkVolumes;

    /// <summary>A new play on these volumes: every body starts outside every volume, unslowed.</summary>
    public void Begin(IReadOnlyList<StatusVolume> volumes)
    {
        Set(volumes);
        Clear();
    }

    /// <summary>
    /// A star's disc raised mid-play (§13, <see cref="SwingDustBowl"/>): read from the next read on, every body outside it until
    /// it touches it. The park's volumes, and every body's state, are unchanged.
    /// </summary>
    public void Add(StatusVolume volume) => Set([.. _volumes, volume]);

    void Set(IReadOnlyList<StatusVolume> volumes)
    {
        _volumes = volumes;
        _fielderVolumes = volumes.Any(v => v.BatterRunnerOnly) ? volumes.Where(v => !v.BatterRunnerOnly).ToList() : volumes;
        _parkVolumes = volumes.All(v => v.OfThePark) ? volumes : volumes.Where(v => v.OfThePark).ToList();
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
        _volumes.Count == 0 ? [] : Read(Of(_fielders, pos), x, z, t, immune, runner: false, batterRunner: false);

    /// <summary>Read one runner where he stands at play second <paramref name="t"/>, the same way as a fielder; only the batter-runner touches a batter-runner's disc, and no runner a fielders-only disc.</summary>
    public IReadOnlyList<(StatusVolume Volume, double UntilT)> Read(Runner runner, double x, double z, double t, bool immune) =>
        _volumes.Count == 0 ? [] : Read(Of(_runners, runner), x, z, t, immune, runner: true, runner.IsBatter);

    /// <summary>The fielder at <paramref name="pos"/> runs slowed this frame.</summary>
    public bool Slowed(string pos) => _fielders.TryGetValue(pos, out var s) && s.Slowed;

    /// <summary>This runner runs slowed this frame.</summary>
    public bool Slowed(Runner runner) => _runners.TryGetValue(runner, out var s) && s.Slowed;

    /// <summary>What a step of a body is multiplied by: <c>fielding.chase.frozenMul</c> slowed, else exactly 1.</summary>
    public static double Mul(bool slowed, RulesTable rules) => slowed ? rules.Fielding.Chase.FrozenMul : 1.0;

    /// <summary>
    /// What this frame's step of the fielder at <paramref name="pos"/> is multiplied by: the strongest slow holding him
    /// (a park's <c>frozenMul</c> or a star disc's own <see cref="StatusVolume.SlowMul"/>), never two stacked; exactly 1 unslowed.
    /// </summary>
    public double Mul(string pos, RulesTable rules) => _fielders.TryGetValue(pos, out var s) ? Mul(s, rules) : 1.0;

    /// <summary>What this frame's step of this runner is multiplied by, the same way as a fielder's.</summary>
    public double Mul(Runner runner, RulesTable rules) => _runners.TryGetValue(runner, out var s) ? Mul(s, rules) : 1.0;

    static double Mul(State s, RulesTable rules) =>
        !s.Slowed ? 1.0 : s.Frozen ? Math.Min(rules.Fielding.Chase.FrozenMul, s.OwnMul) : s.OwnMul;

    static State Of<TKey>(Dictionary<TKey, State> bodies, TKey key) where TKey : notnull
    {
        if (!bodies.TryGetValue(key, out var s)) bodies[key] = s = new State();
        return s;
    }

    IReadOnlyList<(StatusVolume Volume, double UntilT)> Read(State s, double x, double z, double t, bool immune, bool runner, bool batterRunner)
    {
        if (immune)
        {
            s.Inside.Clear();
            s.Slowed = false;
            return [];
        }
        List<(StatusVolume, double)>? touched = null;
        _inside.Clear();
        // The time after leaving is over: what it held goes with it.
        if (t >= s.Until - Eps)
        {
            s.UntilFrozen = false;
            s.UntilOwnMul = 1;
        }
        var frozen = false;
        var ownMul = 1.0;
        foreach (var v in _volumes)
        {
            if (v.BatterRunnerOnly && !batterRunner) continue;
            if (v.FieldersOnly && runner) continue;
            if (!v.ActiveAt(t) || !v.Contains(x, z)) continue;
            _inside.Add(v.Hazard);
            if (v.SlowMul is { } own) ownMul = Math.Min(ownMul, own);
            else frozen = true;
            if (s.Inside.Contains(v.Hazard)) continue;
            s.Until = Math.Max(s.Until, t + v.SlowSec);
            if (v.SlowSec > 0)
            {
                if (v.SlowMul is { } after) s.UntilOwnMul = Math.Min(s.UntilOwnMul, after);
                else s.UntilFrozen = true;
            }
            (touched ??= []).Add((v, s.Until));
        }
        s.Inside.Clear();
        s.Inside.UnionWith(_inside);
        var timed = t < s.Until - Eps;
        s.Slowed = s.Inside.Count > 0 || timed;
        s.Frozen = frozen || (timed && s.UntilFrozen);
        s.OwnMul = timed ? Math.Min(ownMul, s.UntilOwnMul) : ownMul;
        return touched is null ? [] : touched;
    }
}

/// <summary>
/// The route cost of a status volume (§14; FD-14, SF-26; F4-g): a body walking to a goal goes around a volume when going
/// around costs less time than the slow it would pay going through, and goes through when it does not. The CPU and the
/// assistance read the same discs the touch test does (<see cref="BodySlows.Volumes"/>), the same <c>slowSec</c> and the
/// same <c>fielding.chase.frozenMul</c>; the stick is never steered.
///
/// <para>
/// <b>The disc the route keeps off.</b> The volume's disc plus <c>fielding.chase.volumeClearFt</c>, so a body that bends
/// its path on the rim is not clipped by its own ramp. A goal inside that disc (the ball lies in the volume), or a body
/// already inside the volume itself, goes straight: there is no way around a slow it must take or has taken.
/// </para>
///
/// <para>
/// <b>The two costs, in seconds at the body's own speed <c>v</c>.</b> Through: from the entry the body is slowed for the
/// row's time or for as long as it is inside, whichever is longer, but never past the goal; each slowed foot costs
/// <c>(1 / frozenMul − 1) / v</c> more than a free one. Around: the shortest path that keeps off the disc — tangent, arc,
/// tangent — less the straight line, over <c>v</c>. The side is the shorter arc. Only the first volume the straight line
/// meets is costed each frame; the next frame costs the next one.
/// </para>
///
/// <para>
/// <b>No foresight of a draw (FD-14).</b> The route reads the park's volumes, which are geometry, and nothing else: no
/// draw, no stream, nothing a hazard has not yet done. It is a function of where the body stands and where it is going,
/// so the same frame always steers the same way.
/// </para>
/// </summary>
public static class VolumeRoute
{
    /// <summary>
    /// Where a body at <paramref name="at"/> going to <paramref name="goal"/> at <paramref name="speed"/> ft/s should head this
    /// frame: <paramref name="goal"/> itself when the straight line costs no more (exactly the same doubles), else a point along
    /// the tangent that goes around the first volume in the way, as far out as the whole detour, so the step never brakes for it.
    /// </summary>
    public static (double X, double Z) Waypoint(
        (double X, double Z) at, (double X, double Z) goal, IReadOnlyList<StatusVolume> volumes,
        double speed, double frozenMul, double clearFt)
    {
        var plan = Plan(at, goal, volumes, speed, frozenMul, clearFt);
        return plan.Around ? plan.Waypoint : goal;
    }

    /// <summary>What <see cref="Waypoint"/> decided and why, for tests and the trace: the volume costed (or null), both costs in seconds, and the heading point.</summary>
    public readonly record struct Decision(StatusVolume? Volume, double ThroughSec, double AroundSec, bool Around, (double X, double Z) Waypoint);

    public static Decision Plan(
        (double X, double Z) at, (double X, double Z) goal, IReadOnlyList<StatusVolume> volumes,
        double speed, double frozenMul, double clearFt)
    {
        var none = new Decision(null, 0, 0, false, goal);
        if (volumes.Count == 0 || speed <= 0 || frozenMul <= 0 || frozenMul >= 1) return none;
        var dx = goal.X - at.X;
        var dz = goal.Z - at.Z;
        var dist = Math.Sqrt(dx * dx + dz * dz);
        if (dist < 1e-9) return none;
        var ux = dx / dist;
        var uz = dz / dist;

        // The first volume the straight line meets.
        StatusVolume? hit = null;
        double entry = double.PositiveInfinity, chord = 0, radius = 0;
        foreach (var v in volumes)
        {
            if (v.Contains(at.X, at.Z)) continue;
            var r = v.RadiusFt + Math.Max(0, clearFt);
            if (Diamond.Dist(v.X, v.Z, goal.X, goal.Z) <= r) continue;
            var along = (v.X - at.X) * ux + (v.Z - at.Z) * uz;
            if (along <= 0) continue;
            var cx = at.X + ux * along - v.X;
            var cz = at.Z + uz * along - v.Z;
            var h2 = cx * cx + cz * cz;
            if (h2 >= r * r) continue;
            var half = Math.Sqrt(r * r - h2);
            var e = Math.Max(0, along - half);
            if (e >= dist || e >= entry) continue;
            hit = v;
            entry = e;
            chord = Math.Min(along + half, dist) - e;
            radius = r;
        }
        if (hit is null) return none;

        // Through: the slowed feet, each dearer by (1 / m − 1) / v.
        var rest = dist - entry;
        var slowedFt = Math.Max(chord, Math.Min(rest, hit.SlowSec * speed * frozenMul));
        var throughSec = slowedFt * (1 / frozenMul - 1) / speed;

        // Around: tangent, arc, tangent on the shorter side, less the straight line.
        var ax = at.X - hit.X;
        var az = at.Z - hit.Z;
        var gx = goal.X - hit.X;
        var gz = goal.Z - hit.Z;
        var dA = Math.Max(radius, Math.Sqrt(ax * ax + az * az));
        var dG = Math.Sqrt(gx * gx + gz * gz);
        var cross = ax * gz - az * gx;
        var dot = ax * gx + az * gz;
        var theta = Math.Atan2(Math.Abs(cross), dot);
        var arc = Math.Max(0, theta - Math.Acos(radius / dA) - Math.Acos(radius / dG));
        var around = Math.Sqrt(dA * dA - radius * radius) + Math.Sqrt(dG * dG - radius * radius) + radius * arc;
        var aroundSec = Math.Max(0, around - dist) / speed;
        if (aroundSec >= throughSec) return new Decision(hit, throughSec, aroundSec, false, goal);

        // The heading: toward the centre, turned off it by the tangent angle, to the side the shorter arc goes round.
        var toC = Math.Sqrt(ax * ax + az * az);
        var (cxu, czu) = toC < 1e-9 ? (ux, uz) : (-ax / toC, -az / toC);
        var alpha = Math.Asin(Math.Min(1, radius / dA));
        var turn = (cross >= 0 ? -1 : 1) * alpha;
        var (s, c) = (Math.Sin(turn), Math.Cos(turn));
        var hx = cxu * c - czu * s;
        var hz = cxu * s + czu * c;
        return new Decision(hit, throughSec, aroundSec, true, (at.X + hx * around, at.Z + hz * around));
    }
}
