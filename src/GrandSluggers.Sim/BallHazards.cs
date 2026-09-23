namespace GrandSluggers.Sim;

/// <summary>
/// One <see cref="HazardPattern.BallRedirect"/> instance of the park a play is on, as that play reads it (F4-c): the instance's
/// index in <see cref="Park.Hazards"/>, its type, its centre, the disc the ball enters (<see cref="HazardActors.PlayDiscFt"/>,
/// the ring the view draws) and the type's row.
/// </summary>
public sealed record RedirectMouth(int Hazard, string Type, double X, double Z, double DiscFt, HazardTypeRules Row)
{
    /// <summary>A ball at (<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>) is in the mouth: inside the disc, between its floor and its top.</summary>
    public bool Takes(double x, double y, double z) =>
        y <= (Row.MouthFt ?? 0) && y >= (Row.MouthFloorFt ?? double.NegativeInfinity) && Diamond.Dist(X, Z, x, z) <= DiscFt;
}

/// <summary>
/// The ball went into a redirect and came out of another (F4-c; FR-07, FD-08-R1): the typed live fact the play trace records and
/// presentation reads (F8-b), never a caption. <see cref="Exit"/> was drawn from the match's seeded stream: the draw decides
/// where the ball comes out, never a result.
/// </summary>
public sealed record BallRedirected(int Hazard, string Type, int Exit, double T, double EntryX, double EntryZ, double ExitX, double ExitZ);

/// <summary>The ball hit a reward target on its live path (F4-c): the batting team is paid <c>stars.gains.billboard</c> on a hit or a fly out.</summary>
public sealed record RewardHit(int Hazard, string Type, double T);

/// <summary>
/// The ball's hazards, live (§14; F4-c, FR-07, FD-08-R1, FD-09-R2). Once a frame, while the ball follows its path, the live ball
/// asks whether it is in a redirect's mouth (<see cref="Entered"/>) or under a reward target's top (<see cref="Reward"/>). Nothing
/// is decided from where the ball lands and nothing is foreseen: the fielding preview plans the path as hit, and every chaser
/// re-plans from the path the frame after it changes.
///
/// <para>
/// <b>The redirect.</b> A ball inside a mouth's disc at or below its <c>mouthFt</c> enters it and comes out of another
/// instance of the same type — a mouth is never its own exit, so a type with one instance at the park redirects nothing — with
/// <c>exitSpeedMul</c> of its horizontal speed on the same heading and <c>exitVyFtPerSec</c> up. The exit is drawn from the
/// match's seeded stream. The mouth it came out of does not take it again until the ball is clear of that disc.
/// </para>
/// </summary>
public sealed class BallHazards
{
    IReadOnlyList<RedirectMouth> _mouths = [];
    IReadOnlyList<(int Hazard, string Type, double X, double Z, double DiscFt, double TopFt)> _rewards = [];
    int? _lock;

    /// <summary>The mouths this play reads: every redirect instance of a type the park has at least two of.</summary>
    public IReadOnlyList<RedirectMouth> Mouths => _mouths;

    /// <summary>A new play on this park (the played park, so night instances and the hazards switch are already applied).</summary>
    public void Begin(Park park, bool night, RulesTable rules)
    {
        var mouths = new List<RedirectMouth>();
        var rewards = new List<(int, string, double, double, double, double)>();
        for (var i = 0; i < park.Hazards.Count; i++)
        {
            var h = park.Hazards[i];
            var row = rules.Hazards.Of(h.Type);
            if (row.Pattern == HazardPattern.BallRedirect)
                mouths.Add(new RedirectMouth(i, h.Type, h.X, h.Z, HazardActors.PlayDiscFt(h.Radius, row, night), row));
            else if (row.Pattern == HazardPattern.RewardTarget)
                rewards.Add((i, h.Type, h.X, h.Z, HazardActors.PlayDiscFt(h.Radius, row, night), row.TopFt ?? 0));
        }
        _mouths = mouths.Where(m => mouths.Count(o => o.Type == m.Type) >= 2).ToList();
        _rewards = rewards;
        _lock = null;
    }

    /// <summary>Forget the play (no hazard reads).</summary>
    public void Clear()
    {
        _mouths = [];
        _rewards = [];
        _lock = null;
    }

    /// <summary>The mouth the ball at this point enters, or null. The mouth it last came out of is skipped until the ball has left it.</summary>
    public RedirectMouth? Entered(double x, double y, double z)
    {
        if (_lock is { } locked)
        {
            // The mouth the ball came out of lets it go only once it is clear of the disc on the ground — not when the lift
            // carries it over the mouth's top, or it would fall straight back in.
            var exit = _mouths.First(m => m.Hazard == locked);
            if (Diamond.Dist(exit.X, exit.Z, x, z) <= exit.DiscFt) return null;
            _lock = null;
        }
        foreach (var m in _mouths)
            if (m.Takes(x, y, z)) return m;
        return null;
    }

    /// <summary>The exits a mouth can send the ball to: every other instance of its type, in park order.</summary>
    public IReadOnlyList<RedirectMouth> ExitsFor(RedirectMouth mouth) =>
        _mouths.Where(m => m.Type == mouth.Type && m.Hazard != mouth.Hazard).ToList();

    /// <summary>The ball came out of <paramref name="exit"/>: that mouth does not take it again until it has left the disc.</summary>
    public void Exited(RedirectMouth exit) => _lock = exit.Hazard;

    /// <summary>
    /// Where the ball comes out and how (F4-c): at the exit's centre, on the ground, with <c>exitSpeedMul</c> of the entry's
    /// horizontal speed on the entry's heading (straight out from home if it came in with none) and <c>exitVyFtPerSec</c> up.
    /// </summary>
    public static (double X, double Y, double Z, double Vx, double Vy, double Vz) Launch(RedirectMouth exit, double vx, double vz)
    {
        var speed = Math.Sqrt(vx * vx + vz * vz);
        var (dx, dz) = speed > 1e-6 ? (vx / speed, vz / speed) : Out(exit.X, exit.Z);
        var s = speed * (exit.Row.ExitSpeedMul ?? 1);
        return (exit.X, 0, exit.Z, dx * s, exit.Row.ExitVyFtPerSec ?? 0, dz * s);
    }

    static (double, double) Out(double x, double z)
    {
        var d = Math.Sqrt(x * x + z * z);
        return d > 1e-6 ? (x / d, z / d) : (0, 1);
    }

    /// <summary>The reward target the ball at this point hits, or null: inside a sign's disc, at or below its top.</summary>
    public (int Hazard, string Type)? Reward(double x, double y, double z)
    {
        foreach (var r in _rewards)
            if (y <= r.TopFt && Diamond.Dist(r.X, r.Z, x, z) <= r.DiscFt) return (r.Hazard, r.Type);
        return null;
    }
}
