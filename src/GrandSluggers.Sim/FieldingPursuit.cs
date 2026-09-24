namespace GrandSluggers.Sim;

/// <summary>
/// A glove runs to where it can meet the ball, rather than following the ball's
/// current position. Ordinary air routes meet a reachable sample within standing
/// catch height before the first bounce. Ground routes meet a scoopable sample
/// after that bounce; wall robs retain their legal plant (D16, #667).
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
    /// <param name="cutOff">The body has the outfield behind it (the infield, the pitcher, the catcher): a ball it cannot reach that is still
    /// coming is played where it crosses (<see cref="Crossing"/>). An outfielder is the last body; it chases the roll (#667).</param>
    public static Route Plan(
        FieldingPreview preview,
        Park park,
        IReadOnlyList<Sample> path,
        double nowSec,
        double fromX,
        double fromZ,
        double speedFtPerSec,
        RulesTable rules,
        double readySec = 0,
        bool cutOff = false)
    {
        var r = rules;
        var hang = BallFlight.HangTime(path, r);
        var startSec = Math.Max(nowSec, readySec);
        var ramp = RampSec(park, fromX, fromZ, r);
        if (nowSec < hang)
        {
            if (FlyCatch.NeedsJump(preview))
            {
                var plant = FlyCatch.ChaseTarget(preview, r, park);
                return Fixed(plant.X, plant.Z, hang, startSec, fromX, fromZ, speedFtPerSec, true, ramp, r);
            }
            foreach (var sample in path)
            {
                if (sample.T < nowSec || sample.T < startSec) continue;
                if (sample.T >= hang) break;
                if (sample.Height > r.Fielding.Catch.StandingHeightFt) continue;
                if (!FieldBounds.Inside(park, sample.X, sample.Z)) continue;
                var air = Fixed(sample.X, sample.Z, sample.T, startSec, fromX, fromZ, speedFtPerSec, true, ramp, r, preview.CatchRadius);
                if (air.Reachable) return air;
            }
        }
        var ground = Rolling(path, park, nowSec, startSec, fromX, fromZ, speedFtPerSec, ramp, r);
        if (ground.Reachable || !cutOff) return ground;
        return Crossing(path, park, hang, startSec, fromX, fromZ, speedFtPerSec, ramp, r, preview.CatchRadius) ?? ground;
    }

    /// <summary>
    /// A ball an infield body cannot reach, still coming toward it (§8.2, #580): the route is where the ball passes closest — a sample
    /// at catch height before the bounce or at scoop height after it — not where the ball comes to rest behind the body. Null
    /// once the ball has passed (its nearest playable point is its next one): then the body chases the smallest miss, and the
    /// outfield takes the ball on the grass (§8.9).
    /// </summary>
    static Route? Crossing(
        IReadOnlyList<Sample> path,
        Park park,
        double hang,
        double startSec,
        double fromX,
        double fromZ,
        double speedFtPerSec,
        double rampSec,
        RulesTable rules,
        double catchRadius)
    {
        Sample? first = null;
        Sample? nearest = null;
        var nearestFt = double.MaxValue;
        foreach (var sample in path)
        {
            if (sample.T < startSec) continue;
            if (sample.Event is SampleEvent.Fence or SampleEvent.Stands) break;
            var air = sample.T < hang;
            if (air ? sample.Height > rules.Fielding.Catch.StandingHeightFt : sample.Height >= rules.Fielding.Catch.TouchScoopY) continue;
            if (!FieldBounds.Inside(park, sample.X, sample.Z)) continue;
            first ??= sample;
            var d = Diamond.Dist(fromX, fromZ, sample.X, sample.Z);
            if (d < nearestFt)
            {
                nearestFt = d;
                nearest = sample;
            }
        }
        if (nearest is not { } at || first is not { } next || at.T <= next.T) return null;
        var inAir = at.T < hang;
        return Fixed(at.X, at.Z, at.T, startSec, fromX, fromZ, speedFtPerSec, inAir, rampSec, rules, inAir ? catchRadius : 0);
    }

    /// <summary>
    /// The seconds a route loses getting to speed (#718): half the ramp, charged once. The ramp is <c>chase.accelSec</c> on the
    /// ground the body starts on — × that zone's <c>body.startMul</c> (FD-04 B, F3-d), the row the body's own step reads there —
    /// so the plan and the body agree about the ramp. 0 on the shipped table, whatever the ground.
    /// </summary>
    static double RampSec(Park park, double fromX, double fromZ, RulesTable rules) =>
        rules.Fielding.Chase.AccelSec * GroundZones.Of(park, rules).RowAt(fromX, fromZ, rules.Grounds).Body.StartMul / 2;

    /// <param name="readyAt">Per position, the play seconds each body may start moving (the reaction lockout, §8.2).</param>
    public static Choice Choose(
        IReadOnlyDictionary<string, Character> assigned,
        IReadOnlyList<string> positions,
        FieldingPreview preview,
        Park park,
        IReadOnlyList<Sample> path,
        RulesTable rules,
        IReadOnlyDictionary<string, (double X, double Z)>? at = null,
        double nowSec = 0,
        IReadOnlyDictionary<string, double>? readyAt = null)
    {
        Choice? best = null;
        foreach (var position in positions)
        {
            if (!assigned.TryGetValue(position, out var fielder)) continue;
            var start = at != null && at.TryGetValue(position, out var live)
                ? live
                : OutfieldStarts.Of(park, rules)[position];
            var speed = FieldingResolver.ChaseSpeedFt(fielder, position, preview, rules);
            var ready = readyAt != null && readyAt.TryGetValue(position, out var r0) ? r0 : 0;
            var route = Plan(preview, park, path, nowSec, start.X, start.Z, speed, rules, ready, !FieldingResolver.IsOutfield(position));
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
        var firstTouch = BallFlight.HangTime(path, rules);
        for (var i = 0; i < path.Count; i++)
        {
            var sample = path[i];
            if (sample.T + 1e-6 < nowSec || sample.T < firstTouch) continue;
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
        var legal = FieldBounds.ClampFielder(park, live.X, live.Z, rules);
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
        RulesTable rules,
        double reachFt = 0)
    {
        var travel = Math.Max(0, Diamond.Dist(fromX, fromZ, x, z) - reachFt);
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
