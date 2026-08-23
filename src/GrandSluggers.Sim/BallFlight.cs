namespace GrandSluggers.Sim;

/// <summary>Toy ballistic with linear drag. Tuned so a 95 mph / 28° carry is ~380 ft in still air.</summary>
public static class BallFlight
{
    public const double Gravity = 32.174;
    public const double Drag = 0.0019;
    public const double PlateHeightFt = 2.5;

    public static double CarryFeet(double exitMph, double launchDeg, double windMph)
    {
        var samples = Trajectory(exitMph, launchDeg, windMph);
        return samples.Count == 0 ? 0 : samples[^1].Dist;
    }

    public static IReadOnlyList<Sample> Trajectory(double exitMph, double launchDeg, double windMph)
    {
        var v = exitMph * 1.4667;
        var a = launchDeg * Math.PI / 180.0;
        var vx = v * Math.Cos(a) + windMph * 1.4667 * 0.35;
        var vy = v * Math.Sin(a);
        var x = 0.0;
        var y = PlateHeightFt;
        var dt = 1.0 / 120.0;
        var list = new List<Sample>(256) { new(0, 0, y) };
        for (var i = 0; i < 120 * 8; i++)
        {
            var speed = Math.Sqrt(vx * vx + vy * vy);
            vx -= Drag * speed * vx * dt;
            vy -= (Gravity + Drag * speed * vy) * dt;
            x += vx * dt;
            y += vy * dt;
            var t = (i + 1) * dt;
            list.Add(new Sample(t, x, Math.Max(0, y)));
            if (i > 8 && y <= 0)
                break;
        }
        return list;
    }

    public static (double X, double Z) GroundPoint(double carryFt, double sprayDeg)
    {
        var a = sprayDeg * Math.PI / 180.0;
        return (carryFt * Math.Sin(a), carryFt * Math.Cos(a));
    }

    public static double HangTime(IReadOnlyList<Sample> samples) =>
        samples.Count == 0 ? 0 : samples[^1].T;

    public static (double X, double Y, double Z) PointAt(IReadOnlyList<Sample> samples, double sprayDeg, double t)
    {
        if (samples.Count == 0) return (0, PlateHeightFt, 0);
        if (t <= 0) return (0, samples[0].Height, 0);
        var last = samples[^1];
        if (t >= last.T) return Spread(last.Dist, last.Height, sprayDeg);
        for (var i = 1; i < samples.Count; i++)
        {
            if (samples[i].T >= t)
            {
                var a = samples[i - 1];
                var b = samples[i];
                var u = (t - a.T) / Math.Max(1e-6, b.T - a.T);
                var dist = a.Dist + (b.Dist - a.Dist) * u;
                var h = a.Height + (b.Height - a.Height) * u;
                return Spread(dist, h, sprayDeg);
            }
        }
        return Spread(last.Dist, last.Height, sprayDeg);
    }

    static (double X, double Y, double Z) Spread(double dist, double height, double sprayDeg)
    {
        var a = sprayDeg * Math.PI / 180.0;
        return (dist * Math.Sin(a), height, dist * Math.Cos(a));
    }
}
