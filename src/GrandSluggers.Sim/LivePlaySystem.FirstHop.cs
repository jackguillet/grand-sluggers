namespace GrandSluggers.Sim;

/// <summary>
/// A star swing's first hop (spec §13): a fair ball off the swing springs at its first ground contact
/// (<see cref="StarSwingSkill.FirstHopBounceMul"/>), or stands still for a while and runs on slower
/// (<see cref="StarSwingSkill.FirstHopStallSec"/>), on the shared ground physics; or it raises a bowl of loose dust there
/// (<see cref="SwingDustBowl"/>) that slows the fielders inside it and leaves the ball alone. A bent path is continued the way
/// a park redirect continues it, the ball is re-read and every chaser re-plans; the gloves, the throws and the fair / foul
/// line still decide the play. Nothing is rolled: every effect comes from the ball, the bodies and the row's numbers.
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
        if (swing.DustBowl is { } bowl) RaiseDustBowl(swing, bowl, t);
        if (!swing.BendsFirstHop) return;
        const double step = 1.0 / 60;
        var now = BallFlight.PointAt(Path, t, R);
        var next = BallFlight.PointAt(Path, t + step, R);
        var (kx, vy, kz) = ((next.X - now.X) / step, (next.Y - now.Y) / step, (next.Z - now.Z) / step);
        if (kx * kx + kz * kz < 1e-6) return;
        var away = Chaser(now.X, now.Z);
        // The hop's spring (§13): the ball leaves the ground this many times as fast upward, its horizontal pace its own.
        if (swing.FirstHopBounceMul != 1 && vy > 0) vy *= swing.FirstHopBounceMul;
        // The stall (§13): the ball stands on the ground at its hop for the row's seconds — a glove that reaches it may
        // take it there — then runs on from the same spot at the row's share of its speed, on the shared ground physics.
        // The two-second rule holds: a hop that comes late stands only for what is left of the spectacle.
        var stall = swing.FirstHopStallSec > 0 ? Math.Min(swing.FirstHopStallSec, StarSkills.SpectacleSeconds(swing.Id) - t) : 0;
        if (stall > 0)
        {
            now = (now.X, 0, now.Z);
            var mul = swing.FirstHopStallSpeedMul;
            Path = BallFlight.Continue(Stall(Path, t, now.X, now.Z, stall), t + stall,
                now.X, 0, now.Z, kx * mul, vy * mul, kz * mul, Hit.LaunchDeg, Hit.ExitVeloMph, Park, R, Hit.WindMul);
        }
        else
            Path = BallFlight.Continue(Path, t, now.X, now.Y, now.Z, kx, vy, kz, Hit.LaunchDeg, Hit.ExitVeloMph, Park, R, Hit.WindMul);
        Ball = BattedBall.Reread(Path, Hit.ExitVeloMph, Hit.LaunchDeg, false, Park, R);
        (BallX, BallY, BallZ) = now;
        _ballPrev = null;
        Preview = Preview with { LandingX = Ball.LandingX, LandingZ = Ball.LandingZ };
        CoverBallX = Ball.LandingX;
        RecordFact(new FirstHopKicked(swing.Id, t, now.X, now.Z, away.Pos, swing.FirstHopBounceMul, Math.Max(0, stall)));
        Sub = $"{swing.Name}!";
    }

    /// <summary>
    /// The Dust Bowl (§13, <see cref="SwingDustBowl"/>): a disc of the row's radius centred where the ball first met the ground,
    /// standing from that landing for the row's seconds, that slows every fielder inside it to the row's share of his step and
    /// touches no runner. It goes on the park's slow rail (<see cref="BodySlows"/>), read from the next frame; the ball's path
    /// is untouched. A fact records it for presentation and the trace.
    /// </summary>
    void RaiseDustBowl(StarSwingSkill swing, SwingDustBowl bowl, double t)
    {
        var i = BallFlight.LandingIndex(Path!);
        var (landT, x, z) = i >= 0 && Path![i].Event == SampleEvent.Ground && Path[i].T <= t
            ? (Path[i].T, Path[i].X, Path[i].Z)
            : (t, BallX, BallZ);
        var disc = bowl.Volume(x, z, landT);
        _bodySlows.Add(disc);
        RecordFact(new DustBowlRaised(swing.Id, landT, x, z, bowl.RadiusFt, disc.UntilT, Chaser(x, z).Pos));
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
}
