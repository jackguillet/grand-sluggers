namespace GrandSluggers.Sim;

/// <summary>
/// 3-D ballistic with drag taken relative to the park wind, then hops, a roll, and the park's
/// walls (spec §6.1). Tuned so a 95 mph / 28° first landing is ~380 ft in still air.
/// <para>
/// <b>The ground under the ball (FD-05, F3-c).</b> Every roll step reads <c>roll</c> from the row of
/// the zone the ball is in at the start of that step, and every bounce reads <c>bounce</c> — or
/// <c>skid</c>, blended by the incoming impact angle on that row — from the row of the zone at the
/// bounce point (<see cref="GroundZones.RowAt"/>, on the table the caller hands over). A carom reads
/// the row of the wall material the segment is made of (<see cref="WallMaterial.OfSegment"/>). The
/// two loose-ball models the live play runs, the overthrow and the local bobble, live here beside
/// the batted ball and read the same row (<see cref="OverthrowTick"/>, <see cref="LocalBobbleTick"/>).
/// Gravity, drag, the wind and the clock stay the flight table's (FD-03).
/// </para>
/// <para>
/// <b>One clock.</b> Sample times are stretched by <see cref="TimeScale"/> so gloves can get
/// under a fly; carry does not change. That stretched sample clock is <em>the</em> play clock:
/// <see cref="LivePlaySystem.ElapsedSeconds"/> advances on it, and every glove, runner, and throw
/// is judged against it. Pitches and throws are not stretched.
/// </para>
/// </summary>
public static class BallFlight
{
    /// <summary>Miles per hour to feet per second: the one conversion every flight and pitch uses (5280 / 3600, as the game has always rounded it).</summary>
    public const double MphToFtPerSec = 1.4667;

    /// <summary>Arcade hang (flight.timeScale). Distances stay; the clock is slower than the ballistic.</summary>
    public static double TimeScale(RulesTable rules) => rules.Flight.TimeScale;

    /// <summary>
    /// The ground the open field stands on (FD-05, F3-c). <see cref="Trajectory(double, double, double, RulesTable?)"/> has no
    /// park, so it has no zones: it names this one ground for its whole path — grass, the ground the carry tables were tuned
    /// over and what a park's outfield is unless it says otherwise. Every row is equal today, so the choice moves no path; it
    /// is written down so a grass row that differs later changes the open field on purpose, not by a default.
    /// </summary>
    public const string OpenFieldGround = Ground.Grass;

    /// <summary>Open-field carry with the wind straight out at <paramref name="windMph"/> — the carry the tables were tuned on. No fence.</summary>
    public static double CarryFeet(double exitMph, double launchDeg, double windMph, RulesTable rules) =>
        FirstLandingDist(Trajectory(exitMph, launchDeg, windMph, rules), rules);

    /// <summary>Open field: no walls, wind blowing out along the ball's line, on <see cref="OpenFieldGround"/>. For estimates and tests.</summary>
    public static IReadOnlyList<Sample> Trajectory(double exitMph, double launchDeg, double windMph, RulesTable rules) =>
        Integrate(exitMph, launchDeg, 0, windMph, (0, 1), null, null, rules);

    /// <summary>The clipped path in this park: 3-D, the park's directional wind, the fence and the foul walls, on the park's ground zones.</summary>
    /// <remarks><paramref name="windMul"/> scales the park's wind on this one ball (<see cref="AtBatResult.WindMul"/>); 1 is every ordinary ball.</remarks>
    public static IReadOnlyList<Sample> Trajectory(double exitMph, double launchDeg, double sprayDeg, Park park, RulesTable rules,
        double windMul = 1)
    {
        var r = rules;
        return Integrate(exitMph, launchDeg, sprayDeg, park.WindMph * windMul, park.WindDirection, FieldBounds.Of(park), GroundZones.Of(park, r), r);
    }

    static IReadOnlyList<Sample> Integrate(
        double exitMph, double launchDeg, double sprayDeg, double windMph, (double X, double Z) windDir,
        FieldBounds.Boundary? walls, GroundZones? zones, RulesTable rules)
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
        var scale = f.TimeScaleFor(launchDeg, exitMph, rules);
        var list = new List<Sample>(512) { new(0, 0, f.PlateHeightFt, 0, 0) };
        Run(list, rules, walls, zones, 0.0, 0.0, f.PlateHeightFt, 0.0, vx, vy, vz, wx, wz, scale, rolling: false);
        return list;
    }

    /// <summary>
    /// The batted ball from a state (#721, F693-02-continuing-error-ground-response): the path's samples before <paramref name="fromT"/>
    /// kept as they were, then the shared flight and ground physics — drag, the park's wind, gravity, the bounce, the roll, the walls —
    /// run on from (<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>) at (<paramref name="vx"/>, <paramref name="vy"/>,
    /// <paramref name="vz"/>) measured in play seconds, on the same time scale the hit had. A deflected ball is a batted ball still.
    /// </summary>
    public static IReadOnlyList<Sample> Continue(IReadOnlyList<Sample> path, double fromT, double x, double y, double z,
        double vx, double vy, double vz, double launchDeg, double exitMph, Park park, RulesTable rules, double windMul = 1)
    {
        var r = rules;
        var f = r.Flight;
        var wx = park.WindMph * windMul * MphToFtPerSec * f.WindMul * park.WindDirection.X;
        var wz = park.WindMph * windMul * MphToFtPerSec * f.WindMul * park.WindDirection.Z;
        var scale = f.TimeScaleFor(launchDeg, exitMph, r);
        var list = new List<Sample>(512);
        foreach (var s in path)
            if (s.T < fromT - 1e-9) list.Add(s);
        var y0 = Math.Max(0, y);
        list.Add(new Sample(fromT, Math.Sqrt(x * x + z * z), y0, x, z));
        var rolling = y0 <= 1e-9 && Math.Abs(vy) < 1e-9;
        Run(list, r, FieldBounds.Of(park), GroundZones.Of(park, r), fromT, x, y0, z, vx * scale, vy * scale, vz * scale, wx, wz, scale, rolling);
        return list;
    }

    /// <summary>
    /// One integration of the flight and ground physics, appending to <paramref name="list"/> from the given state until the ball
    /// rests or the clock runs out. <paramref name="zones"/> is the park's ground (null for the open field, which stands on
    /// <see cref="OpenFieldGround"/>); impact velocity determines each ground response.
    /// </summary>
    static void Run(List<Sample> list, RulesTable rules, FieldBounds.Boundary? walls, GroundZones? zones, double t0, double x, double y, double z,
        double vx, double vy, double vz, double wx, double wz, double scale, bool rolling)
    {
        var f = rules.Flight;
        var grounds = rules.Grounds;
        var open = zones is null ? grounds.Of(OpenFieldGround) : null;
        // The row of the zone under a point (FD-05): the open field's one ground, else the park's zone map.
        GroundRules Under(double px, double pz) => open ?? zones.GetValueOrDefault().RowAt(px, pz, grounds);
        var dt = 1.0 / f.SampleHz;
        var gone = false;
        var steps = (int)(f.SampleHz * f.MaxSeconds);
        for (var i = 0; i < steps; i++)
        {
            var t = t0 + (i + 1) * dt * scale;
            if (rolling)
            {
                var speed = Math.Sqrt(vx * vx + vz * vz);
                var roll = Under(x, z).Roll;
                var decel = roll.Friction * dt;
                if (speed <= decel || speed - decel < roll.RestSpeed)
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
                    (rx, rz) = Carom(hit, ref vx, ref vz, WallOf(rules.Walls, hit.Segment));
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
                // The top where the ball met the wall (FD-06, F2-c): a polyline span's top runs straight between
                // its two points; every level piece — the whole fence of a park with no points — is its HeightFt.
                if (h > cross.HeightFt)
                {
                    // Over the top: gone. Fair between the poles is the homer (or the ground-rule double
                    // after a bounce); a foul wall is the stands. The path continues for the camera only.
                    evt = fair ? SampleEvent.Fence : SampleEvent.Stands;
                    gone = true;
                }
                else
                {
                    evt = fair ? SampleEvent.Wall : SampleEvent.FoulWall;
                    (nx, nz) = Carom(cross, ref vx, ref vz, WallOf(rules.Walls, cross.Segment));
                    ny = h;
                }
            }

            if (ny <= 0)
            {
                ny = 0;
                if (gone)
                {
                    x = nx;
                    z = nz;
                    list.Add(new Sample(t, Math.Sqrt(x * x + z * z), 0, x, z, evt));
                    break;
                }
                if (evt == SampleEvent.None) evt = SampleEvent.Ground;
                if (vy < 0)
                {
                    // A shallow impact skids; a steep one hops. Blend continuously from the incoming
                    // velocity on this surface, including every later bounce and deflection.
                    var ground = Under(nx, nz);
                    var impact = Math.Atan2(-vy, Math.Sqrt(vx * vx + vz * vz)) * 180 / Math.PI;
                    var blend = Math.Clamp((impact - ground.Skid.ImpactMinDeg)
                        / (ground.Skid.ImpactMaxDeg - ground.Skid.ImpactMinDeg), 0, 1);
                    var minVy = ground.Skid.MinVy + blend * (ground.Bounce.MinVy - ground.Skid.MinVy);
                    var rest = ground.Skid.Restitution + blend * (ground.Bounce.Restitution - ground.Skid.Restitution);
                    var horiz = ground.Skid.Horizontal + blend * (ground.Bounce.Horizontal - ground.Skid.Horizontal);
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

    /// <summary>The row of the material this segment is made of (FD-06): <see cref="WallMaterial.OfSegment"/>, then the library's row.</summary>
    static WallRules WallOf(WallMaterialLibrary walls, FieldBounds.WallSegment segment) => walls.Of(WallMaterial.OfSegment(segment));

    /// <summary>Mirror the horizontal velocity off the wall's normal (the segment's wall row) and start the ball just inside.</summary>
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

    /// <summary>
    /// One tick of a loose ball rolling to a stop (FD-05): a throw that sailed past its cover or dropped at an uncovered bag,
    /// and the shipped fumble's scatter — <see cref="LivePlaySystem"/>'s loose ball. It slows at the <c>overthrow</c>
    /// deceleration of the row of the zone it is in at the start of the tick, moves the tick's mean speed along its heading,
    /// and stops dead against the park's edge. The ball must be moving; a ball at rest is the caller's to leave where it is.
    /// </summary>
    public static LooseBallStep OverthrowTick(GroundZones zones, GroundLibrary grounds, double x, double z, double vx, double vz, double dt)
    {
        var speed = Math.Sqrt(vx * vx + vz * vz);
        if (speed <= 0) throw new ArgumentException("a loose ball at rest does not roll; the caller leaves it where it is", nameof(vx));
        var decel = zones.RowAt(x, z, grounds).Overthrow.DecelFtPerSec2 * dt;
        var next = Math.Max(0, speed - decel);
        var nx = x + vx / speed * (speed + next) * 0.5 * dt;
        var nz = z + vz / speed * (speed + next) * 0.5 * dt;
        var inside = FieldBounds.Clamp(zones.Park, nx, nz);
        if (Math.Abs(inside.X - nx) > 1e-6 || Math.Abs(inside.Z - nz) > 1e-6) next = 0;
        return new LooseBallStep(inside.X, 0, inside.Z, vx / speed * next, 0, vz / speed * next, Air: false, Speed: next);
    }

    /// <summary>
    /// One tick of the local bobble's ball (#721, F693-02-local-bobble-*): it falls under gravity from the contact and
    /// rebounds at the ground row's <c>bobble.restitution</c> under the fumble's <c>bobbleReboundCapFt</c> ceiling, settles when
    /// the next rise would be <c>bobbleSettleFt</c> or less, keeps <c>bobble.groundRetain</c> of its roll at each impact and
    /// slows at <c>bobble.decelFtPerSec2</c> on the ground to rest. The ground numbers come from the row of the zone the ball
    /// is in at the start of the tick (FD-05); the spill's own numbers stay the fumble's (<see cref="HandlingRules"/>). The
    /// park's edge stops it.
    /// </summary>
    public static LooseBallStep LocalBobbleTick(GroundZones zones, GroundLibrary grounds, HandlingRules handling, double gravity,
        double x, double y, double z, double vx, double vy, double vz, bool air, double dt)
    {
        var ground = zones.RowAt(x, z, grounds).Bobble;
        var h = handling;
        var g = gravity;
        var nx = x + vx * dt;
        var nz = z + vz * dt;
        double ny;
        if (air)
        {
            vy -= g * dt;
            ny = y + vy * dt;
            if (ny <= 0)
            {
                ny = 0;
                var up = -vy * ground.Restitution;
                up = Math.Min(up, Math.Sqrt(2 * g * Math.Max(0, h.BobbleReboundCapFt)));
                if (up * up / (2 * g) <= h.BobbleSettleFt) up = 0;
                vx *= ground.GroundRetain;
                vz *= ground.GroundRetain;
                vy = up;
                air = up > 0;
            }
        }
        else
        {
            var speed = Math.Sqrt(vx * vx + vz * vz);
            var next = Math.Max(0, speed - ground.DecelFtPerSec2 * dt);
            nx = x + (speed > 0 ? vx / speed * (speed + next) * 0.5 * dt : 0);
            nz = z + (speed > 0 ? vz / speed * (speed + next) * 0.5 * dt : 0);
            vx = speed > 0 ? vx / speed * next : 0;
            vz = speed > 0 ? vz / speed * next : 0;
            ny = 0;
        }
        var inside = FieldBounds.Clamp(zones.Park, nx, nz);
        if (Math.Abs(inside.X - nx) > 1e-6 || Math.Abs(inside.Z - nz) > 1e-6) vx = vz = 0;
        return new LooseBallStep(inside.X, ny, inside.Z, vx, vy, vz, air, Math.Sqrt(vx * vx + vz * vz));
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
    public static double HangTime(IReadOnlyList<Sample> samples, RulesTable rules)
    {
        _ = rules;
        if (samples.Count == 0) return 0;
        var i = LandingIndex(samples);
        return i < 0 ? samples[^1].T : samples[i].T;
    }

    /// <summary>When the ball finishes hopping and rolling (or lands in the stands).</summary>
    public static double RestTime(IReadOnlyList<Sample> samples) =>
        samples.Count == 0 ? 0 : samples[^1].T;

    public static double FirstGrassTime(IReadOnlyList<Sample> samples, RulesTable rules) => HangTime(samples, rules);

    public static double FirstLandingDist(IReadOnlyList<Sample> samples, RulesTable rules)
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
    public static (double X, double Y, double Z) PointAt(IReadOnlyList<Sample> samples, double t, RulesTable rules)
    {
        if (samples.Count == 0) return (0, rules.Flight.PlateHeightFt, 0);
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

/// <summary>
/// A loose ball after one tick of a ground model (<see cref="BallFlight.OverthrowTick"/>, <see cref="BallFlight.LocalBobbleTick"/>):
/// where it is, how it is moving, whether it is still in the air, and its horizontal speed after the tick (0 is at rest).
/// </summary>
public readonly record struct LooseBallStep(double X, double Y, double Z, double VX, double VY, double VZ, bool Air, double Speed);
