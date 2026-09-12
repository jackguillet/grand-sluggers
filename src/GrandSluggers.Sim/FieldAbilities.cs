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

    public static ThrowResult ApplyThrow(Character from, ThrowResult throwRes, RulesTable? rules = null) =>
        throwRes with { SpeedMul = throwRes.SpeedMul * ThrowMul(from, rules) };
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

    public static FieldingResult Apply(FieldingResult field, string item, Random rng, RulesTable? rules = null) =>
        Apply(field, item, rng, null, rules);

    /// <summary>
    /// Banana slips the play fielder (peel on the grass). Rocket has to hit that body
    /// (batting.items.rocketDazeChance). POW is an infield hop — grounders only. Smoke/ghost/paint are not items.
    /// </summary>
    public static FieldingResult Apply(FieldingResult field, string item, Random rng, Character? target, RulesTable? rules = null)
    {
        if (!Known(item)) return field;
        var id = item.Trim().ToLowerInvariant();
        var outPlay = field.Kind is PlayKind.FlyOut or PlayKind.GroundOut;
        var onPlay = HitsPlay(field, id, target);
        var turns = id switch
        {
            "banana" => outPlay && onPlay,
            "rocket" => outPlay && onPlay && rng.NextDouble() < Rules.Or(rules).Batting.Items.RocketDazeChance,
            "pow" => field.Kind == PlayKind.GroundOut && onPlay,
            _ => false
        };
        return field with
        {
            Kind = turns ? PlayKind.Single : field.Kind,
            Item = id
        };
    }

    /// <summary>Attack smashed the flying item. An out that became a single goes back to an out.</summary>
    public static FieldingResult Smash(FieldingResult field, bool grounder)
    {
        if (string.IsNullOrEmpty(field.Item)) return field;
        var restored = field.Kind is PlayKind.Single or PlayKind.Double
            ? (grounder ? PlayKind.GroundOut : PlayKind.FlyOut)
            : field.Kind;
        return field with { Kind = restored, Item = null };
    }

    static bool HitsPlay(FieldingResult field, string item, Character? target)
    {
        if (target is null) return true;
        if (item == "pow") return true;
        return field.Fielder is not null
            && field.Fielder.Id.Equals(target.Id, StringComparison.OrdinalIgnoreCase);
    }
}
