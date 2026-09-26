namespace GrandSluggers.Sim;

/// <summary>
/// A star swing's first-hop kick (spec §13, <see cref="StarSwingSkill.FirstHopKickDeg"/>): a fair ball off the swing turns at
/// its first ground contact, away from the fielder chasing it, springs (<see cref="StarSwingSkill.FirstHopBounceMul"/>) or stands
/// still for a while and runs on slower (<see cref="StarSwingSkill.FirstHopStallSec"/>), and runs on on the shared ground physics. The path is
/// continued the way a park redirect continues it, the ball is re-read and every chaser re-plans; the gloves, the throws
/// and the fair / foul line still decide the play. Nothing is rolled: the turn and its side come from the ball and the bodies.
/// </summary>
public sealed partial class LivePlaySystem
{
    /// <summary>When this play's ball first meets the ground, while its swing still owes a kick; null once kicked or when none is owed.</summary>
    double? _kickAt;
    StarSwingSkill? _kickSwing;

    /// <summary>Arms the kick for the ball just put in play: a fair ball (not a bunt) whose path first meets the ground, off a swing whose row names one. A ball caught before its hop never kicks.</summary>
    void BeginFirstHopKick()
    {
        _kickAt = null;
        _kickSwing = null;
        if (Hit is null || Ball is null || Path is null || !Hit.InPlay || Hit.Foul || Ball.Foul) return;
        if (Ball.Shape == BattedBallClass.Bunt) return;
        if (StarSkillTable.Or(_match.Content?.StarSkills).Swing(Hit.StarSwingUsed) is not { ShapesFirstHop: true } swing) return;
        var i = BallFlight.LandingIndex(Path);
        if (i < 0 || Path[i].Event != SampleEvent.Ground) return;
        _kickAt = Path[i].T;
        _kickSwing = swing;
    }

    /// <summary>Once, on the first frame at or past the first hop, while the ball follows its path.</summary>
    void ReadFirstHopKick()
    {
        if (_kickAt is not { } at || ElapsedSeconds < at || Path is null || Hit is null || Ball is null || Preview is null
            || _kickSwing is not { } swing)
            return;
        _kickAt = null;
        var t = ElapsedSeconds;
        const double step = 1.0 / 60;
        var now = BallFlight.PointAt(Path, t, R);
        var next = BallFlight.PointAt(Path, t + step, R);
        var (vx, vy, vz) = ((next.X - now.X) / step, (next.Y - now.Y) / step, (next.Z - now.Z) / step);
        if (vx * vx + vz * vz < 1e-6) return;
        var away = Chaser(now.X, now.Z);
        var turn = swing.FirstHopKickDeg > 0 ? FirstHopTurnDeg(vx, vz, now.X, now.Z, away.X, away.Z, swing.FirstHopKickDeg) : 0;
        // The hop's spring (§13): the ball leaves the ground this many times as fast upward, its horizontal pace its own.
        if (swing.FirstHopBounceMul != 1 && vy > 0) vy *= swing.FirstHopBounceMul;
        var r = turn * Math.PI / 180;
        var (kx, kz) = (vx * Math.Cos(r) - vz * Math.Sin(r), vx * Math.Sin(r) + vz * Math.Cos(r));
        // The stall (§13): the ball stands on the ground at its hop for the row's seconds — a glove that reaches it may
        // take it there — then runs on from the same spot at the row's share of its speed, on the shared ground physics.
        // The two-second rule holds: a hop that comes late stands only for what is left of the spectacle.
        var stall = swing.FirstHopStallSec > 0 ? Math.Min(swing.FirstHopStallSec, StarSkills.SpectacleSeconds(swing.Id) - t) : 0;
        if (stall > 0)
        {
            now = (now.X, 0, now.Z);
            var mul = swing.FirstHopStallSpeedMul;
            Path = BallFlight.Continue(Stall(Path, t, now.X, now.Z, stall), t + stall,
                now.X, 0, now.Z, kx * mul, vy * mul, kz * mul, Hit.LaunchDeg, Hit.ExitVeloMph, Park, R);
        }
        else
            Path = BallFlight.Continue(Path, t, now.X, now.Y, now.Z, kx, vy, kz, Hit.LaunchDeg, Hit.ExitVeloMph, Park, R);
        Ball = BattedBall.Reread(Path, Hit.ExitVeloMph, Hit.LaunchDeg, false, Park, R);
        (BallX, BallY, BallZ) = now;
        _ballPrev = null;
        Preview = Preview with { LandingX = Ball.LandingX, LandingZ = Ball.LandingZ };
        CoverBallX = Ball.LandingX;
        RecordFact(new FirstHopKicked(swing.Id, t, now.X, now.Z, turn, away.Pos, swing.FirstHopBounceMul, Math.Max(0, stall)));
        Sub = $"{swing.Name}!";
    }

    /// <summary>
    /// The path up to <paramref name="t"/>, then the ball at rest on the ground at (<paramref name="x"/>, <paramref name="z"/>)
    /// sampled every frame for <paramref name="sec"/> seconds: the stretch a planner and a glove read as a ball standing still.
    /// </summary>
    static List<Sample> Stall(IReadOnlyList<Sample> path, double t, double x, double z, double sec)
    {
        var list = new List<Sample>(path.Count + 64);
        foreach (var s in path)
            if (s.T < t - 1e-9) list.Add(s);
        var dist = Math.Sqrt(x * x + z * z);
        const double step = 1.0 / 60;
        for (var at = t; at < t + sec - 1e-9; at += step)
            list.Add(new Sample(at, dist, 0, x, z));
        return list;
    }

    /// <summary>The body the play sent after the ball (the preview's glove), where it stands now; the nearest body when none is named.</summary>
    (string Pos, double X, double Z) Chaser(double x, double z)
    {
        if (Preview is { } p && _fielders.TryGetValue(p.Position, out var glove)) return (p.Position, glove.X, glove.Z);
        var best = (Pos: "", X: x, Z: z);
        var bestD = double.MaxValue;
        foreach (var (pos, at) in _fielders)
        {
            var d = Diamond.Dist(at.X, at.Z, x, z);
            if (d < bestD) (best, bestD) = ((pos, at.X, at.Z), d);
        }
        return best;
    }

    /// <summary>
    /// The signed turn of a first-hop kick: <paramref name="kickDeg"/> away from the body at (<paramref name="fx"/>,
    /// <paramref name="fz"/>). A positive turn is counter-clockwise in the (x, z) plane. A body dead ahead or behind is
    /// taken as on the left, so the same ball always kicks the same way.
    /// </summary>
    public static double FirstHopTurnDeg(double vx, double vz, double x, double z, double fx, double fz, double kickDeg)
    {
        var cross = vx * (fz - z) - vz * (fx - x);
        return cross >= 0 ? -kickDeg : kickDeg;
    }
}
