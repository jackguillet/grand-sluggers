namespace GrandSluggers.Sim;

public sealed record RaceCameraFeel
{
    public double Margin { get; init; } = .06;
    public double BodyHeightFt { get; init; } = 12;
    public double BodyRadiusFt { get; init; } = 3;
    public double ThrowFollow { get; init; } = .12;
    public double MaxTravelFt { get; init; } = 12;

    public void Validate()
    {
        if (!(Margin > 0 && Margin < .4 && BodyHeightFt > 0 && BodyRadiusFt > 0
            && ThrowFollow >= 0 && ThrowFollow <= 1 && MaxTravelFt >= 0))
            throw new InvalidDataException("Race camera needs a safe viewport margin and positive body bounds.");
    }
}

public static partial class PlayCamera
{
    public static Vec3 BagSubject(int bag)
    {
        var p = Diamond.Bag(Math.Clamp(bag, 1, 4));
        return new Vec3(p.X, 0, p.Z);
    }

    /// <summary>Both ends of each contested path, the actual bodies, ball, thrower and receiver. No chosen runner or seat wins the frame.</summary>
    public static IReadOnlyList<Vec3> RaceSubjects(Match match)
    {
        // Keep the catcher end of the race in view as the ball travels upfield.
        var points = new List<Vec3> { BagSubject(4) };
        var live = match.LivePlay;
        foreach (var runner in match.Runners)
        {
            if (runner.IsBatter || runner.Out || !live.RunnerPlay && !runner.Broke) continue;
            points.Add(BagSubject(runner.FromBag));
            points.Add(BagSubject(Math.Min(4, runner.Bag + 1)));
            points.Add(BagSubject(runner.DestBag));
            var p = runner.Position;
            points.Add(new Vec3(p.X, 0, p.Z));
        }
        if (live.RunnerPlay)
        {
            points.Add(new Vec3(live.BallX, live.BallY, live.BallZ));
            points.Add(new Vec3(live.GloveX, 0, live.GloveZ));
            if (live.Throwing)
            {
                points.Add(new Vec3(live.ThrowFrom.X, live.ThrowFrom.Y, live.ThrowFrom.Z));
                points.Add(new Vec3(live.ThrowTo.X, live.ThrowTo.Y, live.ThrowTo.Z));
            }
        }
        return points;
    }

    /// <summary>Keep the authored catcher-side eye position; fit the race with the lens instead of retreating behind the backstop.</summary>
    public static Framing RaceFraming(CameraShots shots, IReadOnlyList<Vec3> subjects, double aspect, RaceCameraFeel? feel = null, double travelZ = 0)
    {
        feel ??= new RaceCameraFeel();
        var shot = shots.Must(ThrowShot);
        if (subjects.Count == 0) return new Framing(shot.Id, shot.Pos, shot.Target, shot.Fov, shot.Blend);
        var points = new List<Vec3>();
        foreach (var p in subjects)
            foreach (var side in new[] { -feel.BodyRadiusFt, feel.BodyRadiusFt })
                foreach (var depth in new[] { -feel.BodyRadiusFt, feel.BodyRadiusFt })
                {
                    points.Add(new Vec3(p.X + side, p.Y, p.Z + depth));
                    points.Add(new Vec3(p.X + side, p.Y + feel.BodyHeightFt, p.Z + depth));
                }
        var travel = Math.Clamp(travelZ, -feel.MaxTravelFt, feel.MaxTravelFt);
        var eye = shot.Pos with { Z = shot.Pos.Z + travel };
        var look = shot.Target with { Z = shot.Target.Z + travel };
        // The table owns the physical opening position, inside the home board. Subject bounds
        // may widen the lens but must never pull this camera back through stadium geometry.
        var dy = look.Y - eye.Y;
        var dz = look.Z - eye.Z;
        var distance = Math.Sqrt(dy * dy + dz * dz);
        var fy = dy / distance; var fz = dz / distance;
        var tangent = Math.Tan(shot.Fov * Math.PI / 360);
        var safe = 1 - 2 * feel.Margin;
        foreach (var p in points)
        {
            var x = p.X - eye.X; var y = p.Y - eye.Y; var z = p.Z - eye.Z;
            var forward = Math.Max(1e-6, y * fy + z * fz);
            var up = y * fz - z * fy;
            tangent = Math.Max(tangent, Math.Abs(x) / (forward * safe * Math.Max(.1, aspect)));
            tangent = Math.Max(tangent, Math.Abs(up) / (forward * safe));
        }
        var fov = Math.Atan(tangent) * 360 / Math.PI;
        return new Framing(shot.Id, eye, look, fov, shot.Blend);
    }
}

/// <summary>A bounded upfield track driven by actual throw flight. Holding and transfer never move it.</summary>
public sealed class RaceCameraTravel
{
    double _previousBallZ;
    bool _wasInFlight;
    public double Feet { get; private set; }

    public void Reset(double ballZ)
    {
        _previousBallZ = ballZ;
        _wasInFlight = false;
        Feet = 0;
    }

    public double Step(double ballZ, bool inFlight, RaceCameraFeel feel)
    {
        if (inFlight || _wasInFlight)
            Feet = Math.Clamp(Feet + (ballZ - _previousBallZ) * feel.ThrowFollow, -feel.MaxTravelFt, feel.MaxTravelFt);
        _previousBallZ = ballZ;
        _wasInFlight = inFlight;
        return Feet;
    }
}
