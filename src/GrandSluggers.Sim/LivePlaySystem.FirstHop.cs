namespace GrandSluggers.Sim;

/// <summary>
/// A star swing's first-hop kick (spec §13, <see cref="StarSwingSkill.FirstHopKickDeg"/>): a fair ball off the swing turns at
/// its first ground contact, away from the fielder chasing it, and runs on on the shared ground physics. The path is
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
        if (StarSkillTable.Or(_match.Content?.StarSkills).Swing(Hit.StarSwingUsed) is not { FirstHopKickDeg: > 0 } swing) return;
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
        var turn = FirstHopTurnDeg(vx, vz, now.X, now.Z, away.X, away.Z, swing.FirstHopKickDeg);
        var r = turn * Math.PI / 180;
        var (kx, kz) = (vx * Math.Cos(r) - vz * Math.Sin(r), vx * Math.Sin(r) + vz * Math.Cos(r));
        Path = BallFlight.Continue(Path, t, now.X, now.Y, now.Z, kx, vy, kz, Hit.LaunchDeg, Hit.ExitVeloMph, Park, R);
        Ball = BattedBall.Reread(Path, Hit.ExitVeloMph, Hit.LaunchDeg, false, Park, R);
        (BallX, BallY, BallZ) = now;
        _ballPrev = null;
        Preview = Preview with { LandingX = Ball.LandingX, LandingZ = Ball.LandingZ };
        CoverBallX = Ball.LandingX;
        RecordFact(new FirstHopKicked(swing.Id, t, now.X, now.Z, turn, away.Pos));
        Sub = $"{swing.Name}!";
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
