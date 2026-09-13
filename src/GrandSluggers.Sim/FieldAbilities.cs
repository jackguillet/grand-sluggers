namespace GrandSluggers.Sim;

/// <summary>One defensive verb per character — the Sluggers "who you are on defense."</summary>
public static class FieldAbilities
{
    public static double CatchBonus(Character c, RulesTable? rules = null)
    {
        var a = Rules.Or(rules).Fielding.Abilities;
        return c.FieldAbility switch
        {
            "lick-catch" or "grow" or "withdraw" => a.BigCatchBonusFt,
            "super-jump" => a.SuperJumpCatchBonusFt,
            _ => 0
        };
    }

    /// <summary>Lick Catch / Grow reach further on the tag (§10.3): fielding.abilities.tagReachBonusFt.</summary>
    public static double TagReachBonus(Character? c, RulesTable? rules = null) => c?.FieldAbility switch
    {
        "lick-catch" or "grow" => Rules.Or(rules).Fielding.Abilities.TagReachBonusFt,
        _ => 0
    };

    public static double FlyRangeBonus(Character c, RulesTable? rules = null) =>
        c.FieldAbility == "super-jump" ? Rules.Or(rules).Fielding.Abilities.SuperJumpFlyRangeFt : 0;

    public static double GroundRangeBonus(Character c, RulesTable? rules = null) => c.FieldAbility switch
    {
        "dive" or "burrow" => Rules.Or(rules).Fielding.Abilities.DiveGroundRangeFt,
        _ => 0
    };

    public static double ThrowMul(Character c, RulesTable? rules = null)
    {
        var a = Rules.Or(rules).Fielding.Abilities;
        return c.FieldAbility switch
        {
            "laser" => a.LaserMul,
            "snap-throw" => a.SnapThrowMul,
            _ => 1.0
        };
    }

    public static bool IgnoresParkSlow(Character c) =>
        c.FieldAbility.Equals("burrow", StringComparison.OrdinalIgnoreCase);

    /// <summary>Super Jump robs a ball clearing the fence by at most fielding.catch.superJumpRobFt (§8.4).</summary>
    public static bool AirRob(Park park, Character fielder, AtBatResult hit, RulesTable? rules = null)
    {
        if (!fielder.FieldAbility.Equals("super-jump", StringComparison.OrdinalIgnoreCase))
            return false;
        var ball = BattedBall.Of(hit, park, rules);
        return ball.HomeRun && ball.FenceClearFt <= Rules.Or(rules).Fielding.Catch.SuperJumpRobFt;
    }

    public static PlayKind SpinCheck(Character fielder, PlayKind kind)
    {
        if (!fielder.FieldAbility.Equals("spin-check", StringComparison.OrdinalIgnoreCase))
            return kind;
        return kind switch
        {
            PlayKind.Triple => PlayKind.Double,
            PlayKind.Double => PlayKind.Single,
            _ => kind
        };
    }

    /// <summary>The thrower's arm and ability on a chemistry throw: one speed multiplier the one throw clock reads (§8.5).</summary>
    public static ThrowResult ApplyThrow(Character from, ThrowResult throwRes, RulesTable? rules = null) =>
        throwRes with { SpeedMul = throwRes.SpeedMul * ThrowMul(from, rules) * InPlay.ArmMul(from, rules) };
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
    public static double EffectSec(string? item, RulesTable? rules = null)
    {
        var items = Rules.Or(rules).Batting.Items;
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
    public static bool OnPeel(double peelX, double peelZ, double bodyX, double bodyZ, RulesTable? rules = null) =>
        Diamond.Dist(peelX, peelZ, bodyX, bodyZ) <= Rules.Or(rules).Batting.Items.PeelRadiusFt;

    /// <summary>Attack smashed the flying item: it never lands.</summary>
    public static FieldingResult Smash(FieldingResult field, bool grounder)
    {
        _ = grounder;
        if (string.IsNullOrEmpty(field.Item)) return field;
        return field with { Item = null, ItemHit = false, ItemTarget = null };
    }
}
