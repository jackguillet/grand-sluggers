namespace GrandSluggers.Sim;

/// <summary>
/// 3-D ballistic with drag taken relative to the park wind, then hops, a roll, and the park's
/// walls (spec §6.1). Tuned so a 95 mph / 28° first landing is ~380 ft in still air.
/// <para>
/// <b>One clock.</b> Sample times are stretched by <see cref="TimeScale"/> so gloves can get
/// under a fly; carry does not change. That stretched sample clock is <em>the</em> play clock:
/// <see cref="LivePlaySystem.ElapsedSeconds"/> advances on it, and every glove, runner, and throw
/// is judged against it. Pitches and throws are not stretched.
/// </para>
/// </summary>
public static class BallFlight
{
    const double MphToFtPerSec = 1.4667;

    /// <summary>Arcade hang (flight.timeScale). Distances stay; the clock is slower than the ballistic.</summary>
    public static double TimeScale(RulesTable? rules = null) => Rules.Or(rules).Flight.TimeScale;

    /// <summary>Open-field carry with the wind straight out at <paramref name="windMph"/> — the carry the tables were tuned on. No fence.</summary>
    public static double CarryFeet(double exitMph, double launchDeg, double windMph, RulesTable? rules = null) =>
        FirstLandingDist(Trajectory(exitMph, launchDeg, windMph, rules), rules);

    /// <summary>Open field: no walls, wind blowing out along the ball's line. For estimates and tests.</summary>
    public static IReadOnlyList<Sample> Trajectory(double exitMph, double launchDeg, double windMph, RulesTable? rules = null) =>
        Integrate(exitMph, launchDeg, 0, windMph, (0, 1), null, Rules.Or(rules));

    /// <summary>The clipped path in this park: 3-D, the park's directional wind, the fence and the foul walls.</summary>
    public static IReadOnlyList<Sample> Trajectory(double exitMph, double launchDeg, double sprayDeg, Park park, RulesTable? rules = null) =>
        Integrate(exitMph, launchDeg, sprayDeg, park.WindMph, park.WindDirection, FieldBounds.Of(park), Rules.Or(rules));

    static IReadOnlyList<Sample> Integrate(
        double exitMph, double launchDeg, double sprayDeg, double windMph, (double X, double Z) windDir,
        FieldBounds.Boundary? walls, RulesTable rules)
    {
        var f = rules.Flight;
        var v = exitMph * MphToFtPerSec;
        var a = launchDeg * Math.PI / 180.0;
        var s = sprayDeg * Math.PI / 180.0;
        var vh = v * Math.Cos(a);
        var vx = vh * Math.Sin(s);
        var vz = vh * Math.Cos(s);
        var vy = v * Math.Sin(a);
        var wx = windMph * MphToFtPerSec * f.WindMul * windDir.X;
        var wz = windMph * MphToFtPerSec * f.WindMul * windDir.Z;
        var skid = launchDeg >= f.Skid.LaunchMinDeg && launchDeg < f.Skid.LaunchMaxDeg;
        var scale = f.TimeScaleFor(launchDeg, exitMph, rules);
        var list = new List<Sample>(512) { new(0, 0, f.PlateHeightFt, 0, 0) };
        Run(list, f, walls, 0.0, 0.0, f.PlateHeightFt, 0.0, vx, vy, vz, wx, wz, skid, scale, rolling: false, grounded: false);
        return list;
    }

    /// <summary>
    /// The batted ball from a state (#721, F693-02-continuing-error-ground-response): the path's samples before <paramref name="fromT"/>
    /// kept as they were, then the shared flight and ground physics — drag, the park's wind, gravity, the bounce, the roll, the walls —
    /// run on from (<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>) at (<paramref name="vx"/>, <paramref name="vy"/>,
    /// <paramref name="vz"/>) on the same clock and the same time scale the hit had. A deflected ball is a batted ball still.
    /// </summary>
    public static IReadOnlyList<Sample> Continue(IReadOnlyList<Sample> path, double fromT, double x, double y, double z,
        double vx, double vy, double vz, double launchDeg, double exitMph, Park park, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        var f = r.Flight;
        var wx = park.WindMph * MphToFtPerSec * f.WindMul * park.WindDirection.X;
        var wz = park.WindMph * MphToFtPerSec * f.WindMul * park.WindDirection.Z;
        var skid = launchDeg >= f.Skid.LaunchMinDeg && launchDeg < f.Skid.LaunchMaxDeg;
        var scale = f.TimeScaleFor(launchDeg, exitMph, r);
        var list = new List<Sample>(512);
        foreach (var s in path)
            if (s.T < fromT - 1e-9) list.Add(s);
        var y0 = Math.Max(0, y);
        list.Add(new Sample(fromT, Math.Sqrt(x * x + z * z), y0, x, z));
        var rolling = y0 <= 1e-9 && Math.Abs(vy) < 1e-9;
        Run(list, f, FieldBounds.Of(park), fromT, x, y0, z, vx, vy, vz, wx, wz, skid, scale, rolling, grounded: true);
        return list;
    }

    /// <summary>One integration of the flight and ground physics, appending to <paramref name="list"/> from the given state until the ball rests or the clock runs out.</summary>
    static void Run(List<Sample> list, FlightRules f, FieldBounds.Boundary? walls, double t0, double x, double y, double z,
        double vx, double vy, double vz, double wx, double wz, bool skid, double scale, bool rolling, bool grounded)
    {
        var dt = 1.0 / f.SampleHz;
        var gone = false;
        var steps = (int)(f.SampleHz * f.MaxSeconds);
        for (var i = 0; i < steps; i++)
        {
            var t = t0 + (i + 1) * dt * scale;
            if (rolling)
            {
                var speed = Math.Sqrt(vx * vx + vz * vz);
                var decel = f.Roll.Friction * dt;
                if (speed <= decel || speed - decel < f.Roll.RestSpeed)
                {
                    vx = vz = 0;
                    list.Add(new Sample(t, Math.Sqrt(x * x + z * z), 0, x, z, SampleEvent.Ground));
                    break;
                }
                var k = (speed - decel) / speed;
                vx *= k;
                vz *= k;
                var rx = x + vx * dt;
                var rz = z + vz * dt;
                var ev = SampleEvent.Ground;
                if (walls is not null && walls.Cross(x, z, rx, rz) is { } hit)
                {
                    ev = hit.Segment.Kind == FieldBounds.WallKind.FairFence ? SampleEvent.Wall : SampleEvent.FoulWall;
                    (rx, rz) = Carom(hit, ref vx, ref vz, f.Wall);
                }
                x = rx;
                z = rz;
                list.Add(new Sample(t, Math.Sqrt(x * x + z * z), 0, x, z, ev));
                continue;
            }

            var rvx = vx - wx;
            var rvz = vz - wz;
            var rs = Math.Sqrt(rvx * rvx + vy * vy + rvz * rvz);
            vx -= f.Drag * rs * rvx * dt;
            vz -= f.Drag * rs * rvz * dt;
            vy -= (f.Gravity + f.Drag * rs * vy) * dt;
            var nx = x + vx * dt;
            var nz = z + vz * dt;
            var ny = y + vy * dt;
            var evt = SampleEvent.None;

            if (!gone && walls is not null && walls.Cross(x, z, nx, nz) is { } cross)
            {
                var h = y + (ny - y) * cross.U;
                var fair = cross.Segment.Kind == FieldBounds.WallKind.FairFence;
                if (h > cross.Segment.HeightFt)
                {
                    // Over the top: gone. Fair between the poles is the homer (or the ground-rule double
                    // after a bounce); a foul wall is the stands. The path continues for the camera only.
                    evt = fair ? SampleEvent.Fence : SampleEvent.Stands;
                    gone = true;
                }
                else
                {
                    evt = fair ? SampleEvent.Wall : SampleEvent.FoulWall;
                    (nx, nz) = Carom(cross, ref vx, ref vz, f.Wall);
                    ny = h;
                }
            }

            if (ny <= 0 && (grounded || t > f.Landing.FirstGrassMinSec))
            {
                ny = 0;
                if (gone)
                {
                    x = nx;
                    z = nz;
                    list.Add(new Sample(t, Math.Sqrt(x * x + z * z), 0, x, z, evt));
                    break;
                }
                grounded = true;
                if (evt == SampleEvent.None) evt = SampleEvent.Ground;
                if (vy < 0)
                {
                    var minVy = skid ? f.Skid.MinVy : f.Bounce.MinVy;
                    var rest = skid ? f.Skid.Restitution : f.Bounce.Restitution;
                    var horiz = skid ? f.Skid.Horizontal : f.Bounce.Horizontal;
                    if (-vy < minVy)
                    {
                        vy = 0;
                        rolling = true;
                    }
                    else
                    {
                        vy = -vy * rest;
                        vx *= horiz;
                        vz *= horiz;
                    }
                }
            }
            x = nx;
            y = ny;
            z = nz;
            list.Add(new Sample(t, Math.Sqrt(x * x + z * z), Math.Max(0, y), x, z, evt));
        }
    }

    /// <summary>Mirror the horizontal velocity off the wall's normal (flight.wall) and start the ball just inside.</summary>
    static (double X, double Z) Carom(FieldBounds.Crossing hit, ref double vx, ref double vz, WallRules wall)
    {
        var n = hit.Segment;
        var vn = vx * n.Nx + vz * n.Nz;
        var tx = vx - vn * n.Nx;
        var tz = vz - vn * n.Nz;
        var outward = Math.Max(0, vn);
        vx = tx * wall.Tangential - outward * wall.Restitution * n.Nx;
        vz = tz * wall.Tangential - outward * wall.Restitution * n.Nz;
        const double inside = 0.15;
        return (hit.X - n.Nx * inside, hit.Z - n.Nz * inside);
    }

    public static (double X, double Z) GroundPoint(double carryFt, double sprayDeg)
    {
        var a = sprayDeg * Math.PI / 180.0;
        return (carryFt * Math.Sin(a), carryFt * Math.Cos(a));
    }

    /// <summary>Index of the landing mark: the first ground contact, wall, fence crossing, or exit of the clipped path. −1 if none.</summary>
    public static int LandingIndex(IReadOnlyList<Sample> samples)
    {
        for (var i = 1; i < samples.Count; i++)
            if (samples[i].Event != SampleEvent.None)
                return i;
        return -1;
    }

    /// <summary>Time of the landing mark (first grass, or the wall / fence if the ball meets it first) — not the end of the play.</summary>
    public static double HangTime(IReadOnlyList<Sample> samples, RulesTable? rules = null)
    {
        _ = rules;
        if (samples.Count == 0) return 0;
        var i = LandingIndex(samples);
        return i < 0 ? samples[^1].T : samples[i].T;
    }

    /// <summary>When the ball finishes hopping and rolling (or lands in the stands).</summary>
    public static double RestTime(IReadOnlyList<Sample> samples) =>
        samples.Count == 0 ? 0 : samples[^1].T;

    public static double FirstGrassTime(IReadOnlyList<Sample> samples, RulesTable? rules = null) => HangTime(samples, rules);

    public static double FirstLandingDist(IReadOnlyList<Sample> samples, RulesTable? rules = null)
    {
        _ = rules;
        if (samples.Count == 0) return 0;
        var i = LandingIndex(samples);
        return i < 0 ? samples[^1].Dist : samples[i].Dist;
    }

    /// <summary>The landing mark's field position.</summary>
    public static (double X, double Z) LandingPoint(IReadOnlyList<Sample> samples)
    {
        if (samples.Count == 0) return (0, 0);
        var i = LandingIndex(samples);
        var s = i < 0 ? samples[^1] : samples[i];
        return (s.X, s.Z);
    }

    /// <summary>Where the ball is at play time <paramref name="t"/> (the same clock the samples carry).</summary>
    public static (double X, double Y, double Z) PointAt(IReadOnlyList<Sample> samples, double t, RulesTable? rules = null)
    {
        if (samples.Count == 0) return (0, Rules.Or(rules).Flight.PlateHeightFt, 0);
        if (t <= 0) return (samples[0].X, samples[0].Height, samples[0].Z);
        var last = samples[^1];
        if (t >= last.T) return (last.X, last.Height, last.Z);
        var lo = 1;
        var hi = samples.Count - 1;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (samples[mid].T >= t) hi = mid;
            else lo = mid + 1;
        }
        var a = samples[lo - 1];
        var b = samples[lo];
        var u = (t - a.T) / Math.Max(1e-6, b.T - a.T);
        return (a.X + (b.X - a.X) * u, a.Height + (b.Height - a.Height) * u, a.Z + (b.Z - a.Z) * u);
    }
}
