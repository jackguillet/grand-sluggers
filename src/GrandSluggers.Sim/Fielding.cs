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
        Random rng,
        bool night = false)
    {
        var landing = BallFlight.GroundPoint(hit.CarryFt, hit.SprayDeg);
        var samples = BallFlight.Trajectory(hit.ExitVeloMph, hit.LaunchDeg, park.WindMph);
        var hang = BallFlight.HangTime(samples);
        var grounder = IsGrounder(hit);
        var line = IsLine(hit);
        var hrLikely = HomeRunLikely(hit, park);
        var (fielder, pos) = Nearest(defense, pitcher, landing.X, landing.Z, outfield: !grounder && !line);
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
        var buddy = Buddy(defense, pitcher, fielder, pos, landing.X, landing.Z);
        var freeze = (ParkHazards.InSlow(park, landing.X, landing.Z, night) && !FieldAbilities.IgnoresParkSlow(fielder))
                     || hit.StarSwingUsed == "heart-swing";
        if (grounder && hit.StarSwingUsed is "shell-swing" or "cask-swing" && rng.NextDouble() < 0.6)
            warped = true;
        var radius = 10 + fielder.Stats.Field * 0.6 + FieldAbilities.CatchBonus(fielder);
        if (ParkHazards.CanClamber(park, fielder))
            radius += 6;
        var heat = hit.StarPitchUsed is "heatball" or "caskball";
        var furnace = hit.StarSwingUsed is "furnace" or "heat-swing";
        var chomped = ParkHazards.ChompFly(park, night, landing.X, landing.Z, grounder || line);
        return new FieldingPreview(
            fielder, pos, buddy, hang, landing.X, landing.Z, grounder, hrLikely,
            heat, furnace, freeze, radius, warped, Chomped: chomped, Line: line);
    }

    public FieldingResult Resolve(
        AtBatResult hit,
        Park park,
        IReadOnlyList<Character> defense,
        Character pitcher,
        Random rng,
        GloveItem? glove = null,
        FieldingPreview? pre = null,
        bool night = false)
    {
        var shown = pre ?? Preview(hit, park, defense, pitcher, rng, night);
        if (shown.HomeRunLikely && hit.HomeRun)
        {
            if (ParkHazards.CanClamberRob(park, shown.Fielder, hit) || FieldAbilities.AirRob(park, shown.Fielder, hit))
                return new FieldingResult(PlayKind.FlyOut, shown.Fielder, null, shown.HangTimeSec, shown.LandingX, shown.LandingZ, false, shown.Furnace, Buddy: shown.Buddy);
            return new FieldingResult(PlayKind.HomeRun, null, null, shown.HangTimeSec, shown.LandingX, shown.LandingZ, false, shown.Furnace);
        }
        if (shown.Chomped)
            return new FieldingResult(PlayKind.FlyOut, shown.Fielder, null, shown.HangTimeSec, shown.LandingX, shown.LandingZ, shown.Heatball, shown.Furnace, Buddy: shown.Buddy, Chomped: true);

        var fielder = shown.Fielder;
        var pos = shown.Position;
        var landingX = shown.LandingX;
        var landingZ = shown.LandingZ;
        var hang = shown.HangTimeSec;
        var grounder = shown.Grounder;
        var line = shown.Line;
        var furnace = shown.Furnace;
        var heatball = shown.Heatball;
        var range = 24 + fielder.Stats.Field * 2.8 + fielder.Stats.Run * 1.8
                    + FieldAbilities.FlyRangeBonus(fielder) + FieldAbilities.GroundRangeBonus(fielder);
        if (ParkHazards.CanClamber(park, fielder))
            range += 18;
        var speed = 21 + fielder.Stats.Run * 1.9; // ft/s
        if (shown.Frozen) speed *= 0.45;
        var start = Diamond.Positions[pos];
        var toBall = Diamond.Dist(start.X, start.Z, landingX, landingZ);
        var arrive = toBall / Math.Max(8, speed);

        if (line)
        {
            var window = CatchWindowFt(shown.CatchRadius, false, false);
            var reached = arrive <= hang && toBall < window * 3.6 && !shown.Frozen;
            if (reached)
            {
                var drop = (heatball && rng.NextDouble() < 0.35)
                           || (hit.StarSwingUsed == "phony-swing" && rng.NextDouble() < 0.35);
                if (!drop)
                    return new FieldingResult(PlayKind.FlyOut, fielder, null, hang, landingX, landingZ, heatball, furnace, Buddy: shown.Buddy);
            }
            var skipKind = hit.CarryFt >= 180 ? PlayKind.Double : PlayKind.Single;
            skipKind = FieldAbilities.SpinCheck(fielder, skipKind);
            return new FieldingResult(skipKind, fielder, null, hang, landingX, landingZ, heatball, furnace, Buddy: shown.Buddy, Warped: shown.Warped);
        }

        if (!grounder)
        {
            var catchWindow = hang - 0.25;
            var reached = arrive <= catchWindow && toBall < range * 3.2;
            if (reached)
            {
                var drop = (heatball && rng.NextDouble() < 0.35)
                           || (shown.Frozen && rng.NextDouble() < 0.4)
                           || (hit.StarSwingUsed == "phony-swing" && rng.NextDouble() < 0.35);
                if (!drop)
                    return new FieldingResult(PlayKind.FlyOut, fielder, null, hang, landingX, landingZ, heatball, furnace, Buddy: shown.Buddy);
            }

            var kind = hit.CarryFt >= 330 ? PlayKind.Triple
                : hit.CarryFt >= 250 ? PlayKind.Double
                : PlayKind.Single;
            kind = FieldAbilities.SpinCheck(fielder, kind);
            return new FieldingResult(kind, fielder, null, hang, landingX, landingZ, heatball, furnace, Buddy: shown.Buddy, Warped: shown.Warped);
        }

        var gloveScore = fielder.Stats.Field + rng.NextDouble() * 4 + (glove?.ErrorReduction ?? 0) * 4;
        var beat = hit.Quality == ContactQuality.Perfect ? 2.5 : 0;
        var outPlay = gloveScore + 3 > 7 + beat && toBall < range * 2.5 && !shown.Frozen && !shown.Warped;
        if (outPlay)
        {
            var cut = Cutoff(defense, pitcher, fielder);
            var throwRes = cut is null ? null : FieldAbilities.ApplyThrow(fielder, _chem.FieldingThrow(fielder, cut, rng));
            var energy = InPlay.Energy(hit);
            var bobble = InPlay.Bobbles(energy, fielder, rng, glove);
            var knock = InPlay.KnockbackSec(energy, fielder);
            var error = throwRes is { Error: true } || bobble;
            return new FieldingResult(
                error ? PlayKind.Single : PlayKind.GroundOut,
                fielder, cut, hang, landingX, landingZ, heatball, furnace, throwRes, shown.Buddy,
                Bobble: bobble, KnockbackSec: error ? 0 : knock);
        }

        var extra = hit.CarryFt > 90 && hit.Quality == ContactQuality.Perfect;
        var groundKind = FieldAbilities.SpinCheck(fielder, extra ? PlayKind.Double : PlayKind.Single);
        return new FieldingResult(
            groundKind,
            fielder, null, hang, landingX, landingZ, heatball, furnace, Buddy: shown.Buddy, Warped: shown.Warped);
    }

    public (Character Fielder, string Pos) NearestPublic(
        IReadOnlyList<Character> defense, Character pitcher, double x, double z, bool outfield) =>
        Nearest(defense, pitcher, x, z, outfield);

    /// <summary>Closest glove to (x, z) among all nine. Pass live spots when fielders have moved.</summary>
    public static (Character Fielder, string Pos) NearestGlove(
        IReadOnlyList<Character> defense, Character pitcher, double x, double z) =>
        NearestGlove(Assign(defense, pitcher), x, z);

    public static (Character Fielder, string Pos) NearestGlove(
        IReadOnlyDictionary<string, Character> assigned,
        double x,
        double z,
        IReadOnlyDictionary<string, (double X, double Z)>? at = null)
    {
        Character? best = null;
        var bestPos = "P";
        var bestD = double.MaxValue;
        foreach (var kv in assigned)
        {
            var p = at != null && at.TryGetValue(kv.Key, out var live)
                ? live
                : Diamond.Positions[kv.Key];
            var d = Diamond.Dist(p.X, p.Z, x, z);
            if (d < bestD)
            {
                bestD = d;
                best = kv.Value;
                bestPos = kv.Key;
            }
        }
        return (best ?? assigned.Values.First(), bestPos);
    }

    /// <summary>Catch radius plus dive/jump window. Body verbs buy you the extra feet.</summary>
    public static double CatchWindowFt(double catchRadius, bool dive, bool jump)
    {
        var w = catchRadius + 4;
        if (dive) w += 8;
        if (jump) w += 8;
        return w;
    }

    public static bool IsOutfield(string pos) => pos is "LF" or "CF" or "RF";

    /// <summary>
    /// Dirt / grass lip ~95 ft past the rubber, same split baseball games use:
    /// infielders own the hop on the dirt; outfielders own the grass.
    /// </summary>
    public const double InfieldLipFt = 155;

    public static bool OutfieldGrass(double x, double z) =>
        Diamond.Dist(0, 0, x, z) >= InfieldLipFt;

    public static bool OutfieldShouldCharge(double ballX, double ballZ, double landingX, double landingZ) =>
        OutfieldGrass(ballX, ballZ) || OutfieldGrass(landingX, landingZ);

    /// <summary>Charge the landing until the ball is on the grass, then chase the live hop.</summary>
    public static (double X, double Z) OutfieldChaseTarget(
        double ballX, double ballZ, double landingX, double landingZ) =>
        OutfieldGrass(ballX, ballZ) ? (ballX, ballZ) : (landingX, landingZ);

    /// <summary>
    /// Live glove: IF while the ball is on the dirt, nearest OF once it reaches the grass.
    /// One-way handoff — the infielder who first ran it does not keep the play in the outfield.
    /// </summary>
    public static (Character Fielder, string Pos) PlayGlove(
        IReadOnlyDictionary<string, Character> assigned,
        double ballX,
        double ballZ,
        IReadOnlyDictionary<string, (double X, double Z)>? at = null) =>
        OutfieldGrass(ballX, ballZ)
            ? NearestIn(assigned, OutfieldCorners, ballX, ballZ, at)
            : NearestIn(assigned, InfieldPool, ballX, ballZ, at);

    public static (Character Fielder, string Pos) NearestOutfielder(
        IReadOnlyDictionary<string, Character> assigned,
        double x,
        double z,
        IReadOnlyDictionary<string, (double X, double Z)>? at = null) =>
        NearestIn(assigned, OutfieldCorners, x, z, at);

    public static bool HandoffToOutfield(string currentPos, string playPos) =>
        !IsOutfield(currentPos) && IsOutfield(playPos);

    public static double ChaseSpeedFt(Character fielder, bool frozen) =>
        (21 + fielder.Stats.Run * 1.9) * (frozen ? 0.45 : 1);

    public static (double X, double Z) StepToward(
        double x, double z, double tx, double tz, double speed, double dt)
    {
        var dx = tx - x;
        var dz = tz - z;
        var dist = Math.Sqrt(dx * dx + dz * dz);
        if (dist <= 0.35) return (x, z);
        var step = Math.Min(dist, speed * dt);
        return (x + dx / dist * step, z + dz / dist * step);
    }

    public static bool IsGrounder(AtBatResult hit) => hit.LaunchDeg < 14;

    /// <summary>Low rocket: 14–22° with real exit. Not a hopper, not a fly with a ring.</summary>
    public static bool IsLine(AtBatResult hit) =>
        hit.LaunchDeg is >= 14 and < 22 && hit.ExitVeloMph >= 78 && !IsGrounder(hit);

    public static bool HomeRunLikely(AtBatResult hit, Park park)
    {
        if (hit.HomeRun) return true;
        var fence = AtBatResolver.FenceAt(park, hit.SprayDeg);
        return hit.CarryFt >= fence - 15 && hit.LaunchDeg is > 16 and < 40;
    }

    /// <summary>Timed wall leap. Two good-chem outfielders under a would-be homer, not a flag on any fly.</summary>
    public static bool BuddyJumpOffered(FieldingPreview pre) =>
        pre.Buddy is not null && pre.HomeRunLikely && !pre.Grounder && !pre.Line && IsOutfield(pre.Position);

    static readonly string[] InfieldPool = ["P", "C", "1B", "2B", "3B", "SS"];
    static readonly string[] OutfieldPool = ["LF", "CF", "RF", "SS", "2B"];
    static readonly string[] OutfieldCorners = ["LF", "CF", "RF"];

    static (Character Fielder, string Pos) Nearest(
        IReadOnlyList<Character> defense,
        Character pitcher,
        double x,
        double z,
        bool outfield)
    {
        var keyed = Assign(defense, pitcher);
        return NearestIn(keyed, outfield ? OutfieldPool : InfieldPool, x, z, at: null);
    }

    static (Character Fielder, string Pos) NearestIn(
        IReadOnlyDictionary<string, Character> keyed,
        IReadOnlyList<string> pool,
        double x,
        double z,
        IReadOnlyDictionary<string, (double X, double Z)>? at)
    {
        Character? best = null;
        var bestPos = pool[0];
        var bestD = double.MaxValue;
        foreach (var pos in pool)
        {
            if (!keyed.TryGetValue(pos, out var c)) continue;
            var p = at != null && at.TryGetValue(pos, out var live)
                ? live
                : Diamond.Positions[pos];
            var d = Diamond.Dist(p.X, p.Z, x, z);
            if (d < bestD)
            {
                bestD = d;
                best = c;
                bestPos = pos;
            }
        }
        return (best ?? keyed.Values.First(), bestPos);
    }

    Character? Buddy(
        IReadOnlyList<Character> defense,
        Character pitcher,
        Character fielder,
        string fielderPos,
        double x,
        double z)
    {
        if (!IsOutfield(fielderPos)) return null;
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
    bool Warped = false,
    string? Item = null,
    bool Chomped = false,
    bool Bobble = false,
    double KnockbackSec = 0);

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
    bool Warped = false,
    bool Chomped = false,
    bool Line = false);

public static class ParkHazards
{
    public const double CrystalNightWindowMul = 0.85;
    public const double EmberNightFireMul = 1.6;

    public static double ContactWindowMul(Park park, bool night) =>
        night && park.Id == "crystal-rink" ? CrystalNightWindowMul : 1.0;

    public static bool InFreeze(Park park, double x, double z, bool night = false) =>
        InSlow(park, x, z, night);

    public static bool InSlow(Park park, double x, double z, bool night = false)
    {
        foreach (var h in park.Hazards)
        {
            if (h.Type is not ("freeze_volume" or "lava_pit" or "fire_breath")) continue;
            var r = h.Radius;
            if (night && h.Type == "fire_breath")
                r *= EmberNightFireMul;
            if (Diamond.Dist(h.X, h.Z, x, z) <= r) return true;
        }
        return false;
    }

    public static readonly Hazard[] FunfairChompers =
    [
        new("chomper", -72, 205, 16, "L"),
        new("chomper", 0, 228, 18, "C"),
        new("chomper", 78, 198, 16, "R")
    ];

    public static bool ChompFly(Park park, bool night, double x, double z, bool grounder = false)
    {
        if (!night || grounder || park.Id != "funfair-park") return false;
        foreach (var h in FunfairChompers)
            if (Diamond.Dist(h.X, h.Z, x, z) <= h.Radius) return true;
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
