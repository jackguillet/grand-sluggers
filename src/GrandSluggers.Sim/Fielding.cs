namespace GrandSluggers.Sim;

public sealed class FieldingResolver
{
    readonly ChemistryTable _chem;

    public FieldingResolver(ChemistryTable chem) => _chem = chem;

    public FieldingResult Resolve(
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
        var furnace = hit.StarSwingUsed == "furnace";
        var heatball = hit.StarPitchUsed == "heatball";

        if (hit.HomeRun)
        {
            return new FieldingResult(PlayKind.HomeRun, null, null, hang, landing.X, landing.Z, false, furnace);
        }

        var (fielder, pos) = Nearest(defense, pitcher, landing.X, landing.Z, outfield: !grounder);
        var range = 24 + fielder.Stats.Field * 2.8 + fielder.Stats.Run * 1.8;
        var speed = 21 + fielder.Stats.Run * 1.9; // ft/s
        var start = Diamond.Positions[pos];
        var toBall = Diamond.Dist(start.X, start.Z, landing.X, landing.Z);
        var arrive = toBall / Math.Max(8, speed);

        if (!grounder)
        {
            var catchWindow = hang - 0.25;
            var reached = arrive <= catchWindow && toBall < range * 3.2;
            if (reached)
            {
                var drop = heatball && rng.NextDouble() < 0.35;
                if (!drop)
                    return new FieldingResult(PlayKind.FlyOut, fielder, null, hang, landing.X, landing.Z, heatball, furnace);
            }

            var kind = hit.CarryFt >= 330 ? PlayKind.Triple
                : hit.CarryFt >= 250 ? PlayKind.Double
                : PlayKind.Single;
            return new FieldingResult(kind, fielder, null, hang, landing.X, landing.Z, heatball, furnace);
        }

        var glove = fielder.Stats.Field + rng.NextDouble() * 4;
        var beat = hit.Quality == ContactQuality.Perfect ? 2.5 : 0;
        var outPlay = glove + 3 > 7 + beat && toBall < range * 2.5;
        if (outPlay)
        {
            var cut = Cutoff(defense, pitcher, fielder);
            var throwRes = cut is null ? null : _chem.FieldingThrow(fielder, cut, rng);
            var error = throwRes is { Error: true };
            return new FieldingResult(
                error ? PlayKind.Single : PlayKind.GroundOut,
                fielder, cut, hang, landing.X, landing.Z, heatball, furnace, throwRes);
        }

        var extra = hit.CarryFt > 90 && hit.Quality == ContactQuality.Perfect;
        return new FieldingResult(
            extra ? PlayKind.Double : PlayKind.Single,
            fielder, null, hang, landing.X, landing.Z, heatball, furnace);
    }

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
    ThrowResult? Throw = null);
