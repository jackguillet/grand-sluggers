namespace GrandSluggers.Sim;

/// <summary>
/// Toy ballistic with linear drag, then hops and a roll.
/// Carry / hang are first grass — the play continues after that.
/// Tuned so a 95 mph / 28° first landing is ~380 ft in still air.
/// Time on the samples is stretched (<see cref="TimeScale"/>) so gloves can get there.
/// Carry does not change.
/// </summary>
public static class BallFlight
{
    /// <summary>Arcade hang (flight.timeScale). Distances stay; the clock is slower than the ballistic.</summary>
    public static double TimeScale(RulesTable? rules = null) => Rules.Or(rules).Flight.TimeScale;

    public static double CarryFeet(double exitMph, double launchDeg, double windMph, RulesTable? rules = null)
    {
        var samples = Trajectory(exitMph, launchDeg, windMph, rules);
        return FirstLandingDist(samples, rules);
    }

    public static IReadOnlyList<Sample> Trajectory(double exitMph, double launchDeg, double windMph, RulesTable? rules = null)
    {
        var f = Rules.Or(rules).Flight;
        var v = exitMph * 1.4667;
        var a = launchDeg * Math.PI / 180.0;
        var vx = v * Math.Cos(a) + windMph * 1.4667 * f.WindMul;
        var vy = v * Math.Sin(a);
        var x = 0.0;
        var y = f.PlateHeightFt;
        var dt = 1.0 / f.SampleHz;
        var rolling = false;
        var list = new List<Sample>(512) { new(0, 0, y) };
        var steps = (int)(f.SampleHz * f.MaxSeconds);
        for (var i = 0; i < steps; i++)
        {
            var speed = Math.Sqrt(vx * vx + vy * vy);
            if (rolling)
            {
                y = 0;
                vy = 0;
                var decel = f.Roll.Friction * dt;
                if (Math.Abs(vx) <= decel)
                {
                    vx = 0;
                    list.Add(new Sample((i + 1) * dt * f.TimeScale, x, 0));
                    break;
                }
                vx -= Math.Sign(vx) * decel;
                x += vx * dt;
                list.Add(new Sample((i + 1) * dt * f.TimeScale, x, 0));
                if (Math.Abs(vx) < f.Roll.RestSpeed)
                    break;
                continue;
            }

            vx -= f.Drag * speed * vx * dt;
            vy -= (f.Gravity + f.Drag * speed * vy) * dt;
            x += vx * dt;
            y += vy * dt;
            var t = (i + 1) * dt * f.TimeScale;
            if (i > 8 && y <= 0)
            {
                y = 0;
                if (vy < 0)
                {
                    var skip = launchDeg >= f.Skid.LaunchMinDeg && launchDeg < f.Skid.LaunchMaxDeg;
                    var minVy = skip ? f.Skid.MinVy : f.Bounce.MinVy;
                    var rest = skip ? f.Skid.Restitution : f.Bounce.Restitution;
                    var horiz = skip ? f.Skid.Horizontal : f.Bounce.Horizontal;
                    if (-vy < minVy)
                    {
                        vy = 0;
                        rolling = true;
                    }
                    else
                    {
                        vy = -vy * rest;
                        vx *= horiz;
                    }
                }
            }
            list.Add(new Sample(t, x, Math.Max(0, y)));
        }
        return list;
    }

    public static (double X, double Z) GroundPoint(double carryFt, double sprayDeg)
    {
        var a = sprayDeg * Math.PI / 180.0;
        return (carryFt * Math.Sin(a), carryFt * Math.Cos(a));
    }

    /// <summary>Time of first grass contact — not the end of the play.</summary>
    public static double HangTime(IReadOnlyList<Sample> samples, RulesTable? rules = null) => FirstGrassTime(samples, rules);

    /// <summary>When the ball finishes hopping and rolling.</summary>
    public static double RestTime(IReadOnlyList<Sample> samples) =>
        samples.Count == 0 ? 0 : samples[^1].T;

    public static double FirstGrassTime(IReadOnlyList<Sample> samples, RulesTable? rules = null)
    {
        var l = Rules.Or(rules).Flight.Landing;
        foreach (var s in samples)
            if (s.T > l.FirstGrassMinSec && s.Height <= l.GrassHeightFt)
                return s.T;
        return samples.Count == 0 ? 0 : samples[^1].T;
    }

    public static double FirstLandingDist(IReadOnlyList<Sample> samples, RulesTable? rules = null)
    {
        var l = Rules.Or(rules).Flight.Landing;
        foreach (var s in samples)
            if (s.T > l.FirstGrassMinSec && s.Height <= l.GrassHeightFt)
                return s.Dist;
        return samples.Count == 0 ? 0 : samples[^1].Dist;
    }

    public static (double X, double Y, double Z) PointAt(IReadOnlyList<Sample> samples, double sprayDeg, double t, RulesTable? rules = null)
    {
        if (samples.Count == 0) return (0, Rules.Or(rules).Flight.PlateHeightFt, 0);
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
