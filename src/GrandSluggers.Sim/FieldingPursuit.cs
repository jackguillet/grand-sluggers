namespace GrandSluggers.Sim;

/// <summary>
/// A glove runs to where it can meet the ball, rather than following the ball's
/// current position. Air balls use their legal catch plant; hops and rolls use
/// the first future trajectory sample the glove can reach at its rated speed.
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
        bool AirCatch)
    {
        public double TravelTimeSec => TravelFt / Math.Max(0.01, SpeedFtPerSec);
        public double MissFt => Math.Max(0, TravelFt - SpeedFtPerSec * AvailableSec);
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
        if (FieldingResolver.InAir(preview, live.Y, nowSec, hang))
        {
            var plant = FlyCatch.ChaseTarget(preview, park, r);
            return Fixed(plant.X, plant.Z, hang, startSec, fromX, fromZ, speedFtPerSec, airCatch: true, r);
        }
        return Rolling(path, park, nowSec, startSec, fromX, fromZ, speedFtPerSec, r);
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
            var route = Fixed(sample.X, sample.Z, sample.T, startSec, fromX, fromZ, speedFtPerSec, airCatch: false, rules);
            lastLegal = route;
            if (route.Reachable) return route;
        }

        if (lastLegal is not null) return lastLegal.Value;
        var live = BallFlight.PointAt(path, nowSec, rules);
        var legal = FieldBounds.Clamp(park, live.X, live.Z);
        return Fixed(legal.X, legal.Z, nowSec, startSec, fromX, fromZ, speedFtPerSec, airCatch: false, rules);
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
        RulesTable rules)
    {
        var travel = Diamond.Dist(fromX, fromZ, x, z);
        var available = Math.Max(0, meetSec - nowSec);
        var speed = Math.Max(0, speedFtPerSec);
        return new Route(x, z, meetSec, travel, speed, available,
            travel <= speed * available + rules.Fielding.Chase.ReachSlackFt, airCatch);
    }

    static bool Better(Route candidate, Route current)
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
