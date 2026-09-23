namespace GrandSluggers.Sim;

public sealed record RaceCameraFeel
{
    public double Margin { get; init; } = .10;
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

    /// <summary>Fit the whole race by dollying the authored shot, preserving angle, FOV and toy scale at every aspect ratio.</summary>
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
        var center = new Vec3(0, shot.Target.Y,
            (points.Min(p => p.Z) + points.Max(p => p.Z)) / 2);
        // Both eye and target stay on the home–centerfield axis: no lateral tracking or yaw.
        var dx = 0.0;
        var dy = shot.Target.Y - shot.Pos.Y;
        var dz = shot.Target.Z - shot.Pos.Z;
        var distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        var fx = dx / distance; var fy = dy / distance; var fz = dz / distance;
        var horizontal = Math.Sqrt(fx * fx + fz * fz);
        var rx = fz / horizontal; var rz = -fx / horizontal;
        var ux = fy * rz; var uy = fz * rx - fx * rz; var uz = -fy * rx;
        var tanV = Math.Tan(shot.Fov * Math.PI / 360) * (1 - 2 * feel.Margin);
        var tanH = tanV * Math.Max(.1, aspect);
        foreach (var p in points)
        foreach (var reserve in new[] { -feel.MaxTravelFt, feel.MaxTravelFt })
        {
            // Reserve the full travel envelope before moving, so following the ball cannot crop a bag.
            var x = p.X - center.X; var y = p.Y - center.Y; var z = p.Z - center.Z - reserve;
            var forward = x * fx + y * fy + z * fz;
            distance = Math.Max(distance, Math.Abs(x * rx + z * rz) / tanH - forward);
            distance = Math.Max(distance, Math.Abs(x * ux + y * uy + z * uz) / tanV - forward);
        }
        center = center with { Z = center.Z + Math.Clamp(travelZ, -feel.MaxTravelFt, feel.MaxTravelFt) };
        return new Framing(shot.Id, new Vec3(center.X - fx * distance, center.Y - fy * distance, center.Z - fz * distance),
            center, shot.Fov, shot.Blend);
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
