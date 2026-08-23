namespace GrandSluggers.Sim;

public sealed class FieldingResolver
{
    readonly ChemistryTable _chem;

    public FieldingResolver(ChemistryTable chem) => _chem = chem;

    public FieldingPreview Preview(
        AtBatResult hit,
        Park park,
        IReadOnlyList<Character> defense,
        Character pitcher,
        Random rng)
    {
        var landing = BallFlight.GroundPoint(hit.CarryFt, hit.SprayDeg);
        var samples = BallFlight.Trajectory(hit.ExitVeloMph, hit.LaunchDeg, park.WindMph);
        var hang = BallFlight.HangTime(samples);
        var grounder = hit.LaunchDeg < 14;
        var fence = AtBatResolver.FenceAt(park, hit.SprayDeg);
        var hrLikely = hit.HomeRun || (hit.CarryFt >= fence - 15 && hit.LaunchDeg is > 16 and < 40);
        var (fielder, pos) = Nearest(defense, pitcher, landing.X, landing.Z, outfield: !grounder);
        var warped = false;
        if (grounder)
        {
            var w = ParkHazards.WarpIfPipe(park, landing.X, landing.Z, rng);
            if (w.Warped)
            {
                landing = (w.X, w.Z);
                warped = true;
            }
        }
        var buddy = Buddy(defense, pitcher, fielder, landing.X, landing.Z);
        var freeze = ParkHazards.InSlow(park, landing.X, landing.Z);
        var radius = 10 + fielder.Stats.Field * 0.6;
        if (ParkHazards.CanClamber(park, fielder))
            radius += 6;
        return new FieldingPreview(
            fielder, pos, buddy, hang, landing.X, landing.Z, grounder, hrLikely,
            hit.StarPitchUsed == "heatball", hit.StarSwingUsed == "furnace", freeze,
            radius, warped);
    }

    public FieldingResult Resolve(
        AtBatResult hit,
        Park park,
        IReadOnlyList<Character> defense,
        Character pitcher,
        Random rng,
        GloveItem? glove = null)
    {
        var pre = Preview(hit, park, defense, pitcher, rng);
        if (pre.HomeRunLikely && hit.HomeRun)
        {
            if (ParkHazards.CanClamberRob(park, pre.Fielder, hit))
                return new FieldingResult(PlayKind.FlyOut, pre.Fielder, null, pre.HangTimeSec, pre.LandingX, pre.LandingZ, false, pre.Furnace, Buddy: pre.Buddy);
            return new FieldingResult(PlayKind.HomeRun, null, null, pre.HangTimeSec, pre.LandingX, pre.LandingZ, false, pre.Furnace);
        }

        var fielder = pre.Fielder;
        var pos = pre.Position;
        var landingX = pre.LandingX;
        var landingZ = pre.LandingZ;
        var hang = pre.HangTimeSec;
        var grounder = pre.Grounder;
        var furnace = pre.Furnace;
        var heatball = pre.Heatball;
        var range = 24 + fielder.Stats.Field * 2.8 + fielder.Stats.Run * 1.8;
        if (ParkHazards.CanClamber(park, fielder))
            range += 18;
        var speed = 21 + fielder.Stats.Run * 1.9; // ft/s
        if (pre.Frozen) speed *= 0.45;
        var start = Diamond.Positions[pos];
        var toBall = Diamond.Dist(start.X, start.Z, landingX, landingZ);
        var arrive = toBall / Math.Max(8, speed);

        if (!grounder)
        {
            var catchWindow = hang - 0.25;
            var reached = arrive <= catchWindow && toBall < range * 3.2;
            if (reached)
            {
                var drop = (heatball && rng.NextDouble() < 0.35) || (pre.Frozen && rng.NextDouble() < 0.4);
                if (!drop)
                    return new FieldingResult(PlayKind.FlyOut, fielder, null, hang, landingX, landingZ, heatball, furnace, Buddy: pre.Buddy);
            }

            var kind = hit.CarryFt >= 330 ? PlayKind.Triple
                : hit.CarryFt >= 250 ? PlayKind.Double
                : PlayKind.Single;
            return new FieldingResult(kind, fielder, null, hang, landingX, landingZ, heatball, furnace, Buddy: pre.Buddy, Warped: pre.Warped);
        }

        var gloveScore = fielder.Stats.Field + rng.NextDouble() * 4 + (glove?.ErrorReduction ?? 0) * 4;
        var beat = hit.Quality == ContactQuality.Perfect ? 2.5 : 0;
        var outPlay = gloveScore + 3 > 7 + beat && toBall < range * 2.5 && !pre.Frozen && !pre.Warped;
        if (outPlay)
        {
            var cut = Cutoff(defense, pitcher, fielder);
            var throwRes = cut is null ? null : _chem.FieldingThrow(fielder, cut, rng);
            var error = throwRes is { Error: true };
            return new FieldingResult(
                error ? PlayKind.Single : PlayKind.GroundOut,
                fielder, cut, hang, landingX, landingZ, heatball, furnace, throwRes, pre.Buddy);
        }

        var extra = hit.CarryFt > 90 && hit.Quality == ContactQuality.Perfect;
        return new FieldingResult(
            extra ? PlayKind.Double : PlayKind.Single,
            fielder, null, hang, landingX, landingZ, heatball, furnace, Buddy: pre.Buddy, Warped: pre.Warped);
    }

    public (Character Fielder, string Pos) NearestPublic(
        IReadOnlyList<Character> defense, Character pitcher, double x, double z, bool outfield) =>
        Nearest(defense, pitcher, x, z, outfield);

    static (Character Fielder, string Pos) Nearest(
        IReadOnlyList<Character> defense,
        Character pitcher,
        double x,
        double z,
        bool outfield)
    {
        var keyed = Assign(defense, pitcher);
        string[] pool = outfield ? ["LF", "CF", "RF", "SS", "2B"] : ["P", "C", "1B", "2B", "3B", "SS"];
        Character? best = null;
        var bestPos = pool[0];
        var bestD = double.MaxValue;
        foreach (var pos in pool)
        {
            if (!keyed.TryGetValue(pos, out var c)) continue;
            var p = Diamond.Positions[pos];
            var d = Diamond.Dist(p.X, p.Z, x, z);
            if (d < bestD)
            {
                bestD = d;
                best = c;
                bestPos = pos;
            }
        }
        return (best ?? pitcher, bestPos);
    }

    Character? Buddy(IReadOnlyList<Character> defense, Character pitcher, Character fielder, double x, double z)
    {
        Character? best = null;
        var bestD = double.MaxValue;
        var keyed = Assign(defense, pitcher);
        foreach (var pos in new[] { "LF", "CF", "RF" })
        {
            if (!keyed.TryGetValue(pos, out var c) || c.Id == fielder.Id) continue;
            if (_chem.Between(fielder, c) != Chemistry.Good) continue;
            var p = Diamond.Positions[pos];
            var d = Diamond.Dist(p.X, p.Z, x, z);
            if (d < bestD) { bestD = d; best = c; }
        }
        return best;
    }

    static Character? Cutoff(IReadOnlyList<Character> defense, Character pitcher, Character from)
    {
        var keyed = Assign(defense, pitcher);
        if (keyed.TryGetValue("SS", out var ss) && ss.Id != from.Id) return ss;
        if (keyed.TryGetValue("2B", out var two) && two.Id != from.Id) return two;
        return defense.FirstOrDefault(c => c.Id != from.Id);
    }

    public static Dictionary<string, Character> Assign(IReadOnlyList<Character> defense, Character pitcher)
    {
        var map = new Dictionary<string, Character> { ["P"] = pitcher };
        var rest = defense.Where(c => c.Id != pitcher.Id).ToList();
        var i = 0;
        foreach (var pos in Diamond.Order)
        {
            if (pos == "P") continue;
            if (i >= rest.Count) break;
            map[pos] = rest[i++];
        }
        return map;
    }
}

public sealed record FieldingResult(
    PlayKind Kind,
    Character? Fielder,
    Character? Cutoff,
    double HangTimeSec,
    double LandingX,
    double LandingZ,
    bool Heatball,
    bool Furnace,
    ThrowResult? Throw = null,
    Character? Buddy = null,
    bool Warped = false);

public sealed record FieldingPreview(
    Character Fielder,
    string Position,
    Character? Buddy,
    double HangTimeSec,
    double LandingX,
    double LandingZ,
    bool Grounder,
    bool HomeRunLikely,
    bool Heatball,
    bool Furnace,
    bool Frozen,
    double CatchRadius,
    bool Warped = false);

public static class ParkHazards
{
    public static bool InFreeze(Park park, double x, double z) => InSlow(park, x, z);

    public static bool InSlow(Park park, double x, double z)
    {
        foreach (var h in park.Hazards)
        {
            if (h.Type is not ("freeze_volume" or "lava_pit" or "fire_breath")) continue;
            if (Diamond.Dist(h.X, h.Z, x, z) <= h.Radius) return true;
        }
        return false;
    }

    public static (double X, double Z, bool Warped) WarpIfPipe(Park park, double x, double z, Random rng)
    {
        var pipes = park.Hazards.Where(h => h.Type is "warp_pipe" or "barrel").ToList();
        if (pipes.Count < 2) return (x, z, false);
        Hazard? hit = null;
        foreach (var p in pipes)
        {
            if (Diamond.Dist(p.X, p.Z, x, z) <= p.Radius + 8)
            {
                hit = p;
                break;
            }
        }
        if (hit is null) return (x, z, false);
        var exits = pipes.Where(p => !ReferenceEquals(p, hit)).ToList();
        var dest = exits[rng.Next(exits.Count)];
        return (dest.X, dest.Z, true);
    }

    public static string WarpName(Park park) =>
        park.Hazards.Any(h => h.Type == "barrel") ? "barrel cannon" : "warp can";

    public static bool HitStarSign(Park park, double x, double z)
    {
        foreach (var h in park.Hazards)
        {
            if (h.Type != "billboard") continue;
            if (Diamond.Dist(h.X, h.Z, x, z) <= h.Radius) return true;
        }
        return false;
    }

    public static bool CanClamber(Park park, Character fielder) =>
        fielder.FieldAbility.Equals("clamber", StringComparison.OrdinalIgnoreCase) &&
        park.Hazards.Any(h => h.Type == "climb_wall");

    public static bool CanClamberRob(Park park, Character fielder, AtBatResult hit)
    {
        if (!CanClamber(park, fielder)) return false;
        var fence = AtBatResolver.FenceAt(park, hit.SprayDeg);
        return hit.CarryFt <= fence + 28;
    }
}
