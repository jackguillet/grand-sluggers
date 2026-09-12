namespace GrandSluggers.Sim;

public sealed class FieldingResolver
{
    public static readonly IReadOnlyList<string> InfieldPursuitPositions = ["P", "C", "1B", "2B", "3B", "SS"];
    public static readonly IReadOnlyList<string> AirPursuitPositions = ["LF", "CF", "RF", "SS", "2B"];
    /// <summary>A pop: the corners and the catcher join the air pool in their sector; the P never takes one if anyone else can (§7.7, §8.2).</summary>
    public static readonly IReadOnlyList<string> PopPursuitPositions = ["LF", "CF", "RF", "SS", "2B", "1B", "3B", "C"];
    public static readonly IReadOnlyList<string> OutfieldPursuitPositions = ["LF", "CF", "RF"];
    /// <summary>A foul flight near the lines (§7.11).</summary>
    public static readonly IReadOnlyList<string> FoulPursuitPositions = ["C", "1B", "3B", "LF", "RF"];

    /// <summary>The pursuit pool for a class (§8.2): dirt and ropes to the infield, pops to the air pool plus the corners, flies to the air pool.</summary>
    public static IReadOnlyList<string> PursuitPool(BattedBallClass shape, bool foul)
    {
        if (foul) return FoulPursuitPositions;
        if (shape.OnTheDirt() || shape == BattedBallClass.Liner) return InfieldPursuitPositions;
        return shape == BattedBallClass.Pop ? PopPursuitPositions : AirPursuitPositions;
    }

    readonly ChemistryTable _chem;
    readonly RulesTable _rules;

    public FieldingResolver(ChemistryTable chem, RulesTable? rules = null)
    {
        _chem = chem;
        _rules = Rules.Or(rules);
    }

    public FieldingPreview Preview(
        AtBatResult hit,
        Park park,
        IReadOnlyList<Character> defense,
        Character pitcher,
        Random rng,
        bool night = false,
        IReadOnlyDictionary<string, Character>? gloves = null)
    {
        // One flight for the preview, the ring, and the homer call (§5.6): the clipped path in this park.
        var ball = BattedBall.Of(hit, park, _rules);
        var samples = ball.Samples;
        var hang = ball.HangT;
        var landing = (X: ball.LandingX, Z: ball.LandingZ);
        var shape = ball.Shape;
        var grounder = shape.OnTheDirt();
        var line = shape == BattedBallClass.Liner;
        var assigned = Assign(defense, pitcher, gloves);
        var seed = new FieldingPreview(
            pitcher, "P", null, hang, landing.X, landing.Z, shape, false, false, false, 10, Foul: ball.Foul, Ball: ball);
        var pursuit = FieldingPursuit.Choose(
            assigned,
            PursuitPool(shape, ball.Foul),
            seed,
            park,
            samples,
            readyAt: ReactionLockouts(_rules));
        var fielder = pursuit.Fielder;
        var pos = pursuit.Position;
        var warped = false;
        if (grounder)
        {
            var w = ParkHazards.WarpIfPipe(park, landing.X, landing.Z, rng, _rules);
            if (w.Warped)
            {
                landing = (w.X, w.Z);
                warped = true;
            }
        }
        var buddyPlant = FlyCatch.ChaseTarget(seed with { Fielder = fielder, Position = pos }, park, _rules);
        var buddy = Buddy(assigned, fielder, pos, buddyPlant.X, buddyPlant.Z);
        var freeze = (ParkHazards.InSlow(park, landing.X, landing.Z, night, _rules) && !FieldAbilities.IgnoresParkSlow(fielder))
                     || hit.StarSwingUsed == "heart-swing";
        if (grounder && hit.StarSwingUsed is "shell-swing" or "cask-swing" && rng.NextDouble() < _rules.Fielding.Park.ShellWarpChance)
            warped = true;
        var radius = CatchRadiusFt(fielder, park, _rules);
        var heat = hit.StarPitchUsed is "heatball" or "caskball";
        var furnace = hit.StarSwingUsed is "furnace" or "heat-swing";
        var chomped = ParkHazards.ChompFly(park, night, landing.X, landing.Z, grounder || line);
        return new FieldingPreview(
            fielder, pos, buddy, hang, landing.X, landing.Z, shape,
            heat, furnace, freeze, radius, warped, Chomped: chomped, Foul: ball.Foul, Ball: ball);
    }

    /// <summary>
    /// What the batted ball decides on its own, before any glove (§7): a homer at the crossing, a
    /// foul on the untouched path, a chomped fly. Everything else is <see cref="PlayKind.InPlay"/>:
    /// the live ball's gloves, throws and runner bodies decide it, never a roll (§8.3, §8.8, A.4 #37).
    /// </summary>
    public FieldingResult Resolve(
        AtBatResult hit,
        Park park,
        IReadOnlyList<Character> defense,
        Character pitcher,
        Random rng,
        GloveItem? glove = null,
        FieldingPreview? pre = null,
        bool night = false,
        IReadOnlyDictionary<string, Character>? gloves = null)
    {
        _ = glove;
        var shown = pre ?? Preview(hit, park, defense, pitcher, rng, night, gloves);
        var ball = shown.Ball ?? BattedBall.Of(hit, park, _rules);
        var kind = shown.Chomped ? PlayKind.FlyOut
            : shown.HomeRunLikely ? PlayKind.HomeRun
            : shown.Foul ? PlayKind.Foul
            : PlayKind.InPlay;
        return new FieldingResult(
            kind, shown.Fielder, null, shown.HangTimeSec, shown.LandingX, shown.LandingZ, shown.Heatball, shown.Furnace,
            Buddy: shown.Buddy, Warped: shown.Warped, Chomped: shown.Chomped, GroundRule: ball.GroundRule);
    }

    /// <summary>The resolved catch verb, kept as a fact so copy can change without changing behavior.</summary>
    public static DefensiveFeat CatchFeat(FieldingPreview shown, AtBatResult hit, Park park)
    {
        _ = hit;
        if (BuddyJumpOffered(shown))
            return DefensiveFeat.BuddyJump;
        if (!shown.HomeRunLikely)
            return DefensiveFeat.None;
        if (ParkHazards.CanClamber(park, shown.Fielder))
            return DefensiveFeat.Clamber;
        if (shown.Fielder.FieldAbility.Equals("super-jump", StringComparison.OrdinalIgnoreCase))
            return DefensiveFeat.SuperJump;
        return DefensiveFeat.None;
    }

    /// <summary>The live glove verb the player actually completed on this catch.</summary>
    public static DefensiveFeat PlayerCatchFeat(FieldingPreview shown, Park park, bool buddyJump, bool jumped)
    {
        if (buddyJump)
            return DefensiveFeat.BuddyJump;
        if (!jumped || !shown.HomeRunLikely)
            return DefensiveFeat.None;
        if (ParkHazards.CanClamber(park, shown.Fielder))
            return DefensiveFeat.Clamber;
        if (shown.Fielder.FieldAbility.Equals("super-jump", StringComparison.OrdinalIgnoreCase))
            return DefensiveFeat.SuperJump;
        return DefensiveFeat.None;
    }

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

    /// <summary>Catch radius plus dive/jump window (fielding.catch). Body verbs buy you the extra feet.</summary>
    public static double CatchWindowFt(double catchRadius, bool dive, bool jump, RulesTable? rules = null)
    {
        var c = Rules.Or(rules).Fielding.Catch;
        var w = catchRadius + c.WindowPadFt;
        if (dive) w += c.DiveReachFt;
        if (jump) w += c.JumpReachFt;
        return w;
    }

    /// <summary>Base catch radius for a glove (fielding.catch.radius*, abilities, clamber parks).</summary>
    public static double CatchRadiusFt(Character fielder, Park? park, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        var radius = r.Fielding.Catch.RadiusBaseFt + fielder.Stats.Field * r.Fielding.Catch.RadiusPerField
                     + FieldAbilities.CatchBonus(fielder, r);
        if (park != null && ParkHazards.CanClamber(park, fielder))
            radius += r.Fielding.Catch.ClamberRadiusFt;
        return radius;
    }

    public static bool IsOutfield(string pos) => pos is "LF" or "CF" or "RF";

    /// <summary>
    /// Dirt / grass lip ~95 ft past the rubber (flight.classes.infieldLipFt), same split baseball
    /// games use: infielders own the hop on the dirt; outfielders own the grass.
    /// </summary>
    public static bool OutfieldGrass(double x, double z, RulesTable? rules = null) =>
        Diamond.Dist(0, 0, x, z) >= Rules.Or(rules).Flight.Classes.InfieldLipFt;

    public static bool OutfieldShouldCharge(double ballX, double ballZ, double landingX, double landingZ, RulesTable? rules = null) =>
        OutfieldGrass(ballX, ballZ, rules) || OutfieldGrass(landingX, landingZ, rules);

    /// <summary>
    /// Still up: fly or liner, hang not due, height above a hop.
    /// A hopper is never in the air for chase — they charge the live ball.
    /// </summary>
    public static bool InAir(FieldingPreview pre, double ballY, double hitT, double? hangSec = null)
    {
        if (pre.Grounder) return false;
        var hang = hangSec ?? pre.HangTimeSec;
        return hitT < hang && ballY > 0.75;
    }

    /// <summary>
    /// Where the glove runs. Air → landing / wall plant. Dirt hop → live ball.
    /// Chasing live XZ while the ball is still up is the home-first path
    /// (run to the plate, then watch it fly over).
    /// </summary>
    public static (double X, double Z) GloveChaseTarget(
        FieldingPreview pre,
        Park? park,
        double ballX,
        double ballZ,
        double ballY,
        double hitT,
        double? hangSec = null,
        RulesTable? rules = null) =>
        InAir(pre, ballY, hitT, hangSec)
            ? FlyCatch.ChaseTarget(pre, park, rules)
            : (ballX, ballZ);

    /// <summary>
    /// Charge the landing while the ball is in the air or still on the dirt.
    /// Live hop only once it is on the grass.
    /// </summary>
    public static (double X, double Z) OutfieldChaseTarget(
        double ballX, double ballZ, double landingX, double landingZ, bool inAir = false, RulesTable? rules = null) =>
        inAir || !OutfieldGrass(ballX, ballZ, rules) ? (landingX, landingZ) : (ballX, ballZ);

    /// <summary>
    /// Live glove: IF while the ball is on the dirt, nearest OF once it reaches the grass.
    /// One-way handoff — the infielder who first ran it does not keep the play in the outfield.
    /// </summary>
    public static (Character Fielder, string Pos) PlayGlove(
        IReadOnlyDictionary<string, Character> assigned,
        double ballX,
        double ballZ,
        IReadOnlyDictionary<string, (double X, double Z)>? at = null,
        RulesTable? rules = null) =>
        OutfieldGrass(ballX, ballZ, rules)
            ? NearestIn(assigned, OutfieldPursuitPositions, ballX, ballZ, at)
            : NearestIn(assigned, InfieldPursuitPositions, ballX, ballZ, at);

    public static (Character Fielder, string Pos) NearestOutfielder(
        IReadOnlyDictionary<string, Character> assigned,
        double x,
        double z,
        IReadOnlyDictionary<string, (double X, double Z)>? at = null) =>
        NearestIn(assigned, OutfieldPursuitPositions, x, z, at);

    public static bool HandoffToOutfield(string currentPos, string playPos) =>
        !IsOutfield(currentPos) && IsOutfield(playPos);

    /// <summary>The one glove speed (§8.1, fielding.chase): human stick and CPU chase share it; dash (East held) multiplies it.</summary>
    public static double ChaseSpeedFt(Character fielder, bool frozen, RulesTable? rules = null, bool dash = false)
    {
        var c = Rules.Or(rules).Fielding.Chase;
        return (c.BaseFtPerSec + fielder.Stats.Run * c.FtPerSecPerRun) * (frozen ? c.FrozenMul : 1)
               * (dash ? FieldDash.ChaseMul(rules) : 1);
    }

    /// <summary>The reaction lockout per position (§8.2, fielding.reaction): play seconds before each body may move.</summary>
    public static Dictionary<string, double> ReactionLockouts(RulesTable? rules = null)
    {
        var re = Rules.Or(rules).Fielding.Reaction;
        var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var pos in Diamond.Order) map[pos] = re.LockoutSec(pos);
        return map;
    }

    public static (double X, double Z) StepToward(
        double x, double z, double tx, double tz, double speed, double dt, Park? park = null, RulesTable? rules = null)
    {
        var dx = tx - x;
        var dz = tz - z;
        var dist = Math.Sqrt(dx * dx + dz * dz);
        if (dist <= Rules.Or(rules).Fielding.Chase.StepStopFt) return park == null ? (x, z) : FieldBounds.Clamp(park, x, z);
        var step = Math.Min(dist, speed * dt);
        var next = (X: x + dx / dist * step, Z: z + dz / dist * step);
        return park == null ? next : FieldBounds.Clamp(park, next.X, next.Z);
    }

    /// <summary>Timed wall leap. Two good-chem outfielders under a would-be homer, not a flag on any fly.</summary>
    public static bool BuddyJumpOffered(FieldingPreview pre) =>
        pre.Buddy is not null && pre.HomeRunLikely && !pre.Grounder && !pre.Line && IsOutfield(pre.Position);

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
        IReadOnlyDictionary<string, Character> keyed,
        Character fielder,
        string fielderPos,
        double x,
        double z)
    {
        if (!IsOutfield(fielderPos)) return null;
        Character? best = null;
        var bestD = double.MaxValue;
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

    /// <summary>The defensive alignment: the team's glove diamond (§8.1) with whoever is on the mound now.</summary>
    public static Dictionary<string, Character> Assign(Team team, Character pitcher) =>
        Assign(team.Roster, pitcher, team.Gloves);

    /// <summary>
    /// Positions from the lineup's glove diamond, not roster order (§8.1). When the pitcher on the
    /// mound is not the diamond's P (a swap, §4.7), the old pitcher takes the vacated glove. Without
    /// a diamond the roster order stands in for it, P first.
    /// </summary>
    public static Dictionary<string, Character> Assign(
        IReadOnlyList<Character> defense, Character pitcher, IReadOnlyDictionary<string, Character>? gloves)
    {
        if (gloves is not null && gloves.TryGetValue("P", out var diamondP) && Diamond.Order.All(gloves.ContainsKey))
        {
            var map = new Dictionary<string, Character>(StringComparer.OrdinalIgnoreCase);
            foreach (var pos in Diamond.Order) map[pos] = gloves[pos];
            if (!diamondP.Id.Equals(pitcher.Id, StringComparison.OrdinalIgnoreCase))
            {
                var vacated = Diamond.Order.FirstOrDefault(pos => gloves[pos].Id.Equals(pitcher.Id, StringComparison.OrdinalIgnoreCase));
                if (vacated is not null)
                {
                    map[vacated] = diamondP;
                    map["P"] = pitcher;
                    return map;
                }
            }
            else
                return map;
        }
        return Assign(defense, pitcher);
    }

    public static Dictionary<string, Character> Assign(IReadOnlyList<Character> defense, Character pitcher)
    {
        var map = new Dictionary<string, Character>(StringComparer.OrdinalIgnoreCase) { ["P"] = pitcher };
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

/// <summary>
/// The fielding facts of one batted ball. Before the live ball runs, <see cref="Kind"/> is only
/// what the flight decides alone (homer, foul, chomped) or <see cref="PlayKind.InPlay"/>; after
/// Time it carries what the gloves did — who took the ball, the last throw, a bobble, a sailed
/// throw — and Complete names the play from the bodies (§10.6).
/// </summary>
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
    double KnockbackSec = 0,
    DefensiveFeat Feat = DefensiveFeat.None,
    bool GroundRule = false,
    /// <summary>A glove took the ball this play.</summary>
    bool Caught = false,
    /// <summary>A throw missed its cover and skipped past, live (§8.5, §8.6): the ERROR.</summary>
    bool ThrowSailed = false,
    /// <summary>The thrown item landed on its body (§12); <see cref="ItemTarget"/> is who.</summary>
    bool ItemHit = false,
    Character? ItemTarget = null);

/// <summary>
/// What the defense is looking at from the crack: the glove on it, the landing mark, and the
/// ball's class (§6.2) — the one table the pools, the ring, the catch window, and the cameras read.
/// </summary>
public sealed record FieldingPreview(
    Character Fielder,
    string Position,
    Character? Buddy,
    double HangTimeSec,
    double LandingX,
    double LandingZ,
    BattedBallClass Class,
    bool Heatball,
    bool Furnace,
    bool Frozen,
    double CatchRadius,
    bool Warped = false,
    bool Chomped = false,
    bool Foul = false,
    BattedBall? Ball = null)
{
    /// <summary>On the dirt: the glove scoops it, no ring, no window.</summary>
    public bool Grounder => Class.OnTheDirt();

    /// <summary>A rope: the short window on the infield.</summary>
    public bool Line => Class == BattedBallClass.Liner;

    /// <summary>The flight clears the fence: only a leap at the wall takes it (§8.4).</summary>
    public bool HomeRunLikely => Class == BattedBallClass.Homer;
}

public static class ParkHazards
{
    public static double ContactWindowMul(Park park, bool night, RulesTable? rules = null) =>
        night && park.Id == "crystal-rink" ? Rules.Or(rules).Fielding.Park.CrystalNightWindowMul : 1.0;

    public static bool InFreeze(Park park, double x, double z, bool night = false, RulesTable? rules = null) =>
        InSlow(park, x, z, night, rules);

    public static bool InSlow(Park park, double x, double z, bool night = false, RulesTable? rules = null)
    {
        foreach (var h in park.Hazards)
        {
            if (h.Type is not ("freeze_volume" or "lava_pit" or "fire_breath")) continue;
            var r = h.Radius;
            if (night && h.Type == "fire_breath")
                r *= Rules.Or(rules).Fielding.Park.EmberNightFireMul;
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

    public static (double X, double Z, bool Warped) WarpIfPipe(Park park, double x, double z, Random rng, RulesTable? rules = null)
    {
        var pipes = park.Hazards.Where(h => h.Type is "warp_pipe" or "barrel").ToList();
        if (pipes.Count < 2) return (x, z, false);
        var pad = Rules.Or(rules).Fielding.Park.PipeReachPadFt;
        Hazard? hit = null;
        foreach (var p in pipes)
        {
            if (Diamond.Dist(p.X, p.Z, x, z) <= p.Radius + pad)
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

    /// <summary>Clamber robs a ball clearing the fence by at most fielding.catch.clamberRobFt (§8.4).</summary>
    public static bool CanClamberRob(Park park, Character fielder, AtBatResult hit, RulesTable? rules = null)
    {
        if (!CanClamber(park, fielder)) return false;
        var ball = BattedBall.Of(hit, park, rules);
        return ball.HomeRun && ball.FenceClearFt <= Rules.Or(rules).Fielding.Catch.ClamberRobFt;
    }
}
