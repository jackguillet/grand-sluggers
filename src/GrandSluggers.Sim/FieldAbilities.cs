namespace GrandSluggers.Sim;

/// <summary>
/// The closed field-ability pool (spec §8.9, AB-12): every id a character's <c>fieldAbility</c> may name, for captains and
/// sidekicks alike. The content load refuses any other, and every rule compares against these, exactly.
/// </summary>
public static class FieldAbilityId
{
    public const string Laser = "laser";
    public const string LickCatch = "lick-catch";
    public const string RelayPivot = "relay-pivot";
    public const string SnapThrow = "snap-throw";
    public const string WallSpring = "wall-spring";

    public static readonly IReadOnlyList<string> All = [Laser, LickCatch, RelayPivot, SnapThrow, WallSpring];
}

/// <summary>
/// One defensive verb per character (§8.4, §8.5, AB-12): Snap Throw, Lick Catch, Laser, Relay Pivot or Wall Spring. Every
/// number is <c>fielding.abilities</c>; a play is decided by the ball, the glove and the wall, never by a roll.
/// </summary>
public static class FieldAbilities
{
    static bool Has(Character? c, string id) => c is not null && c.FieldAbility == id;

    /// <summary>
    /// Whether <paramref name="c"/> may carry Lick Catch (§8.4): a tongue body, which is a character of a faction the table
    /// names in <c>fielding.abilities.lickCatchFactions</c> (Zig's, Reed's). The validator refuses the ability on anyone else.
    /// </summary>
    public static bool TongueBody(string faction, RulesTable rules) =>
        rules.Fielding.Abilities.LickCatchFactions.Any(f => string.Equals(f, faction, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The ball is on the tongue (§8.4, Lick Catch): at most <c>lickReachFt</c> ahead of the body along
    /// (<paramref name="faceX"/>, <paramref name="faceZ"/>), within <c>lickWidthFt</c> of that line, and no higher than a
    /// standing glove reaches (<c>catch.standingHeightFt</c>). A ball behind the body, or beside it past the width, is not.
    /// </summary>
    public static bool TongueReaches(double bodyX, double bodyZ, double faceX, double faceZ,
        double ballX, double ballY, double ballZ, RulesTable rules)
    {
        var a = rules.Fielding.Abilities;
        var len = Math.Sqrt(faceX * faceX + faceZ * faceZ);
        if (len < 1e-9 || ballY > rules.Fielding.Catch.StandingHeightFt) return false;
        var ux = faceX / len;
        var uz = faceZ / len;
        var dx = ballX - bodyX;
        var dz = ballZ - bodyZ;
        var along = dx * ux + dz * uz;
        var across = Math.Abs(dx * uz - dz * ux);
        return along >= 0 && along <= a.LickReachFt && across <= a.LickWidthFt;
    }

    /// <summary>
    /// Which way the tongue snaps (§8.4): the body's own run when it moves faster than a walk
    /// (<see cref="CartoonJuice.WalkFtPerSec"/>), else toward the ball — the heading <see cref="BodyFacing"/> gives the body.
    /// </summary>
    public static (double X, double Z) Facing(double vx, double vz, double bodyX, double bodyZ, double ballX, double ballZ) =>
        Math.Sqrt(vx * vx + vz * vz) > CartoonJuice.WalkFtPerSec ? (vx, vz) : (ballX - bodyX, ballZ - bodyZ);

    /// <summary>
    /// Wall Spring (§8.4): the feet a holder's leap reaches past the ordinary one while the body stands within
    /// <c>wallSpringFromFt</c> of the outfield fence — <c>wallSpringReachFt</c> there, 0 anywhere else, and 0 for every other body.
    /// </summary>
    public static double WallSpringFt(Character? c, Park? park, double x, double z, RulesTable rules)
    {
        if (!Has(c, FieldAbilityId.WallSpring) || park is null) return 0;
        var a = rules.Fielding.Abilities;
        var fence = AtBatResolver.FenceAt(park, FieldBounds.SprayDeg(x, z));
        return fence - Math.Sqrt(x * x + z * z) <= a.WallSpringFromFt ? a.WallSpringReachFt : 0;
    }

    /// <summary>Wall Spring over any wall (§8.4): a leap at a wall is always at one, so the holder's rob reaches <c>wallSpringReachFt</c> higher.</summary>
    public static double WallSpringRobFt(Character? c, RulesTable rules) =>
        Has(c, FieldAbilityId.WallSpring) ? rules.Fielding.Abilities.WallSpringReachFt : 0;

    public static double ThrowMul(Character c, RulesTable rules) => c.FieldAbility switch
    {
        FieldAbilityId.Laser => rules.Fielding.Abilities.LaserMul,
        FieldAbilityId.SnapThrow => rules.Fielding.Abilities.SnapThrowMul,
        _ => 1.0
    };

    /// <summary>
    /// The release of a throw by <paramref name="who"/> (§8.5): Relay Pivot's <c>relayPivotReleaseSec</c> when the body is the
    /// cutoff throwing on a relay it caught clean (<paramref name="relayLeg"/>), Snap Throw's <c>snapReleaseSec</c> after any
    /// clean received throw, else null — the ordinary <c>throw.releaseSec</c>.
    /// </summary>
    public static double? ReleaseSec(Character who, bool receivedClean, bool relayLeg, RulesTable rules)
    {
        if (!receivedClean) return null;
        var a = rules.Fielding.Abilities;
        if (relayLeg && Has(who, FieldAbilityId.RelayPivot)) return a.RelayPivotReleaseSec;
        return Has(who, FieldAbilityId.SnapThrow) ? a.SnapReleaseSec : null;
    }

    /// <summary>The thrower's arm and ability on a chemistry throw: one speed multiplier the one throw clock reads (§8.5), and the arm rating its range is measured from.</summary>
    public static ThrowResult ApplyThrow(Character from, ThrowResult throwRes, RulesTable rules) =>
        throwRes with { SpeedMul = throwRes.SpeedMul * ThrowMul(from, rules) * InPlay.ArmMul(from, rules), Arm = from.Stats.Arm };
}

public static class ErrorItems
{
    public static readonly string[] All = ["banana", "rocket", "pow"];

    public static string Pick(Random rng) => All[rng.Next(All.Length)];

    public static bool Known(string? item)
    {
        if (string.IsNullOrWhiteSpace(item)) return false;
        var id = item.Trim().ToLowerInvariant();
        foreach (var a in All)
            if (a == id) return true;
        return false;
    }

    /// <summary>
    /// The offense throws an item at a body (§12). The result records the throw; the live ball lands
    /// it by geometry after <c>batting.items.flySec</c>: a banana is a peel on the grass where the body
    /// stood, a rocket dazes the body only if it is still there, a POW hops every ball on the dirt.
    /// No roll decides it, and it never converts an out into a caption. <see cref="FieldingResult.ItemHit"/>
    /// says the item is live at <see cref="FieldingResult.ItemTarget"/>.
    /// </summary>
    public static FieldingResult Apply(FieldingResult field, string item, Character? target)
    {
        if (!Known(item)) return field;
        var id = item.Trim().ToLowerInvariant();
        var who = target ?? field.Fielder;
        var live = id == "pow" || who is not null;
        return field with { Item = id, ItemHit = live, ItemTarget = who };
    }

    /// <summary>Seconds the item keeps its glove off the ball (batting.items).</summary>
    public static double EffectSec(string? item, RulesTable rules)
    {
        var items = rules.Batting.Items;
        return item?.Trim().ToLowerInvariant() switch
        {
            "banana" => items.SlipSec,
            "rocket" => items.DazeSec,
            "pow" => items.PowHopSec,
            _ => 0
        };
    }

    /// <summary>A POW hops every ball on the dirt: no glove scoops while it lasts. A peel or a rocket is one body.</summary>
    public static bool AffectsEveryGlove(string? item) => item?.Trim().ToLowerInvariant() == "pow";

    /// <summary>A peel lies on the grass where it landed; anybody who steps on it slips (§12).</summary>
    public static bool IsPeel(string? item) => item?.Trim().ToLowerInvariant() == "banana";

    /// <summary>
    /// The CPU offense's item, when it throws one (§12): a POW with a runner on and the ball on the dirt
    /// (every glove hops, the runners run), a peel at the glove going for a ball on the dirt, a rocket at
    /// the body under a ball in the air. A table, not a roll.
    /// </summary>
    public static string CpuPick(BattedBallClass shape, bool runnersOn) =>
        shape.OnTheDirt() ? (runnersOn ? "pow" : "banana") : "rocket";

    /// <summary>A body inside the peel's radius is on the peel.</summary>
    public static bool OnPeel(double peelX, double peelZ, double bodyX, double bodyZ, RulesTable rules) =>
        Diamond.Dist(peelX, peelZ, bodyX, bodyZ) <= rules.Batting.Items.PeelRadiusFt;

    /// <summary>Attack smashed the flying item: it never lands.</summary>
    public static FieldingResult Smash(FieldingResult field, bool grounder)
    {
        _ = grounder;
        if (string.IsNullOrEmpty(field.Item)) return field;
        return field with { Item = null, ItemHit = false, ItemTarget = null };
    }
}
