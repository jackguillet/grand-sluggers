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

    public static Route Plan(
        FieldingPreview preview,
        Park park,
        IReadOnlyList<Sample> path,
        double sprayDeg,
        double nowSec,
        double fromX,
        double fromZ,
        double speedFtPerSec)
    {
        var hang = BallFlight.HangTime(path);
        var live = BallFlight.PointAt(path, sprayDeg, nowSec);
        if (FieldingResolver.InAir(preview, live.Y, nowSec, hang))
        {
            var plant = FlyCatch.ChaseTarget(preview, park);
            return Fixed(plant.X, plant.Z, hang, nowSec, fromX, fromZ, speedFtPerSec, airCatch: true);
        }
        return Rolling(path, sprayDeg, park, nowSec, fromX, fromZ, speedFtPerSec);
    }

    public static Choice Choose(
        IReadOnlyDictionary<string, Character> assigned,
        IReadOnlyList<string> positions,
        FieldingPreview preview,
        Park park,
        IReadOnlyList<Sample> path,
        double sprayDeg,
        IReadOnlyDictionary<string, (double X, double Z)>? at = null,
        double nowSec = 0)
    {
        Choice? best = null;
        foreach (var position in positions)
        {
            if (!assigned.TryGetValue(position, out var fielder)) continue;
            var start = at != null && at.TryGetValue(position, out var live)
                ? live
                : Diamond.Positions[position];
            var speed = FieldingResolver.ChaseSpeedFt(fielder, preview.Frozen);
            var route = Plan(preview, park, path, sprayDeg, nowSec, start.X, start.Z, speed);
            var candidate = new Choice(fielder, position, route);
            if (best is null || Better(candidate.Route, best.Value.Route))
                best = candidate;
        }
        if (best is not null) return best.Value;
        throw new InvalidOperationException("pursuit pool has no assigned fielder");
    }

    static Route Rolling(
        IReadOnlyList<Sample> path,
        double sprayDeg,
        Park park,
        double nowSec,
        double fromX,
        double fromZ,
        double speedFtPerSec)
    {
        Route? lastLegal = null;
        for (var i = 0; i < path.Count; i++)
        {
            var sample = path[i];
            if (sample.T + 1e-6 < nowSec) continue;
            if (sample.Height >= FlyCatch.TouchScoopY) continue;
            var point = BallFlight.GroundPoint(sample.Dist, sprayDeg);
            if (!FieldBounds.Inside(park, point.X, point.Z)) break;
            var route = Fixed(point.X, point.Z, sample.T, nowSec, fromX, fromZ, speedFtPerSec, airCatch: false);
            lastLegal = route;
            if (route.Reachable) return route;
        }

        if (lastLegal is not null) return lastLegal.Value;
        var live = BallFlight.PointAt(path, sprayDeg, nowSec);
        var legal = FieldBounds.Clamp(park, live.X, live.Z);
        return Fixed(legal.X, legal.Z, nowSec, nowSec, fromX, fromZ, speedFtPerSec, airCatch: false);
    }

    static Route Fixed(
        double x,
        double z,
        double meetSec,
        double nowSec,
        double fromX,
        double fromZ,
        double speedFtPerSec,
        bool airCatch)
    {
        var travel = Diamond.Dist(fromX, fromZ, x, z);
        var available = Math.Max(0, meetSec - nowSec);
        var speed = Math.Max(0, speedFtPerSec);
        return new Route(x, z, meetSec, travel, speed, available,
            travel <= speed * available + 0.35, airCatch);
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
