namespace GrandSluggers.Sim;

/// <summary>
/// A glove runs to where it can meet the ball, rather than following the ball's
/// current position. Flies (and a liner this body can take in the air) use the
/// legal catch plant; hops, rolls, and a liner that will bounce use the first
/// future trajectory sample the glove can reach at its rated speed (D16, #667).
/// </summary>
public static class FieldingPursuit
{
    public readonly record struct Route(
        double X,
        double Z,
        double MeetTimeSec,
        double TravelFt,
        double SpeedFtPerSec,
        double AvailableSec,
        bool Reachable,
        bool AirCatch,
        /// <summary>Seconds the body loses getting to speed (#718): half the ramp, charged once to the route. 0 on the shipped table.</summary>
        double RampSec = 0)
    {
        public double TravelTimeSec => TravelFt / Math.Max(0.01, SpeedFtPerSec) + RampSec;
        public double MissFt => Math.Max(0, TravelFt - SpeedFtPerSec * Math.Max(0, AvailableSec - RampSec));
    }

    public readonly record struct Choice(Character Fielder, string Position, Route Route);

    /// <param name="readySec">Play seconds when this body may start moving (the reaction lockout, §8.2); the route's travel time starts there.</param>
    public static Route Plan(
        FieldingPreview preview,
        Park park,
        IReadOnlyList<Sample> path,
        double nowSec,
        double fromX,
        double fromZ,
        double speedFtPerSec,
        RulesTable? rules = null,
        double readySec = 0)
    {
        var r = Rules.Or(rules);
        var hang = BallFlight.HangTime(path, r);
        var live = BallFlight.PointAt(path, nowSec, r);
        var startSec = Math.Max(nowSec, readySec);
        var ramp = RampSec(park, fromX, fromZ, r);
        if (FieldingResolver.InAir(preview, live.Y, nowSec, hang, r))
        {
            var plant = FlyCatch.ChaseTarget(preview, park, r);
            var air = Fixed(plant.X, plant.Z, hang, startSec, fromX, fromZ, speedFtPerSec, airCatch: true, ramp, r);
            // A fly always runs the plant. A liner this body can take in the air is the same catch.
            // A liner still up that this body cannot catch: first reachable point on the roll, not the bounce (#667).
            if (preview.Class.IsFlyShape() || CanTakeInAir(air, preview, r))
                return air;
        }
        return Rolling(path, park, nowSec, startSec, fromX, fromZ, speedFtPerSec, ramp, r);
    }

    /// <summary>
    /// The seconds a route loses getting to speed (#718): half the ramp, charged once. The ramp is <c>chase.accelSec</c> on the
    /// ground the body starts on — × that zone's <c>body.startMul</c> (FD-04 B, F3-d), the row the body's own step reads there —
    /// so the plan and the body agree about the ramp. 0 on the shipped table, whatever the ground.
    /// </summary>
    static double RampSec(Park park, double fromX, double fromZ, RulesTable rules) =>
        rules.Fielding.Chase.AccelSec * GroundZones.Of(park, rules).RowAt(fromX, fromZ, rules.Grounds).Body.StartMul / 2;

    /// <summary>Inside the catch window of the plant at hang: the body holds it in the air, so the plant is the route.</summary>
    static bool CanTakeInAir(Route air, FieldingPreview preview, RulesTable rules)
    {
        if (air.Reachable) return true;
        var window = FieldingResolver.CatchWindowFt(preview.CatchRadius, false, false, rules);
        return air.MissFt < window;
    }

    /// <param name="readyAt">Per position, the play seconds each body may start moving (the reaction lockout, §8.2).</param>
    public static Choice Choose(
        IReadOnlyDictionary<string, Character> assigned,
        IReadOnlyList<string> positions,
        FieldingPreview preview,
        Park park,
        IReadOnlyList<Sample> path,
        IReadOnlyDictionary<string, (double X, double Z)>? at = null,
        double nowSec = 0,
        RulesTable? rules = null,
        IReadOnlyDictionary<string, double>? readyAt = null)
    {
        Choice? best = null;
        foreach (var position in positions)
        {
            if (!assigned.TryGetValue(position, out var fielder)) continue;
            var start = at != null && at.TryGetValue(position, out var live)
                ? live
                : Diamond.Positions[position];
            var speed = FieldingResolver.ChaseSpeedFt(fielder, position, preview, rules);
            var ready = readyAt != null && readyAt.TryGetValue(position, out var r0) ? r0 : 0;
            var route = Plan(preview, park, path, nowSec, start.X, start.Z, speed, rules, ready);
            var candidate = new Choice(fielder, position, route);
            if (best is null || Better(candidate.Route, best.Value.Route))
                best = candidate;
        }
        if (best is not null) return best.Value;
        throw new InvalidOperationException("pursuit pool has no assigned fielder");
    }

    static Route Rolling(
        IReadOnlyList<Sample> path,
        Park park,
        double nowSec,
        double startSec,
        double fromX,
        double fromZ,
        double speedFtPerSec,
        double rampSec,
        RulesTable rules)
    {
        Route? lastLegal = null;
        var scoopY = rules.Fielding.Catch.TouchScoopY;
        for (var i = 0; i < path.Count; i++)
        {
            var sample = path[i];
            if (sample.T + 1e-6 < nowSec) continue;
            if (sample.Height >= scoopY) continue;
            // Gone over a wall: nothing past this sample is a pickup.
            if (sample.Event is SampleEvent.Fence or SampleEvent.Stands) break;
            if (!FieldBounds.Inside(park, sample.X, sample.Z)) continue;
            var route = Fixed(sample.X, sample.Z, sample.T, startSec, fromX, fromZ, speedFtPerSec, airCatch: false, rampSec, rules);
            lastLegal = route;
            if (route.Reachable) return route;
        }

        if (lastLegal is not null) return lastLegal.Value;
        var live = BallFlight.PointAt(path, nowSec, rules);
        var legal = FieldBounds.Clamp(park, live.X, live.Z);
        return Fixed(legal.X, legal.Z, nowSec, startSec, fromX, fromZ, speedFtPerSec, airCatch: false, rampSec, rules);
    }

    static Route Fixed(
        double x,
        double z,
        double meetSec,
        double nowSec,
        double fromX,
        double fromZ,
        double speedFtPerSec,
        bool airCatch,
        double ramp,
        RulesTable rules)
    {
        var travel = Diamond.Dist(fromX, fromZ, x, z);
        var available = Math.Max(0, meetSec - nowSec);
        var speed = Math.Max(0, speedFtPerSec);
        // The response law (#718): a body from rest reaches its speed over the ramp, which costs it half that ramp of travel (RampSec).
        return new Route(x, z, meetSec, travel, speed, available,
            travel <= speed * Math.Max(0, available - ramp) + rules.Fielding.Chase.ReachSlackFt, airCatch, ramp);
    }

    /// <summary>
    /// The one route ordering (§8.2, D16): a reachable route beats an unreachable one, then the earlier meet, then the shorter run;
    /// unreachable routes rank by the smaller miss. <see cref="Choose"/> picks by it and the hand-off guard (§8.9) compares by it.
    /// </summary>
    public static bool Better(Route candidate, Route current)
    {
        if (candidate.Reachable != current.Reachable) return candidate.Reachable;
        if (candidate.Reachable && Math.Abs(candidate.MeetTimeSec - current.MeetTimeSec) > 1e-6)
            return candidate.MeetTimeSec < current.MeetTimeSec;
        if (candidate.Reachable && Math.Abs(candidate.TravelTimeSec - current.TravelTimeSec) > 1e-6)
            return candidate.TravelTimeSec < current.TravelTimeSec;
        if (Math.Abs(candidate.MissFt - current.MissFt) > 1e-6)
            return candidate.MissFt < current.MissFt;
        return candidate.TravelFt < current.TravelFt;
    }
}
