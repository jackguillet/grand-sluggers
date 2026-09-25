namespace GrandSluggers.Sim;

/// <summary>
/// The closed field-ability registry (spec §8.9): every id a character's <c>fieldAbility</c> may name. The content load
/// refuses any other, and every rule compares against these, exactly.
/// </summary>
public static class FieldAbilityId
{
    public const string BallDash = "ball-dash";
    public const string Burrow = "burrow";
    public const string Clamber = "clamber";
    public const string Dive = "dive";
    public const string Grow = "grow";
    public const string Laser = "laser";
    public const string LickCatch = "lick-catch";
    public const string LongToss = "long-toss";
    public const string SandScoop = "sand-scoop";
    public const string SnapThrow = "snap-throw";
    public const string SpinCheck = "spin-check";
    public const string SuperJump = "super-jump";
    public const string Withdraw = "withdraw";

    public static readonly IReadOnlyList<string> All =
        [BallDash, Burrow, Clamber, Dive, Grow, Laser, LickCatch, LongToss, SandScoop, SnapThrow, SpinCheck, SuperJump, Withdraw];
}

/// <summary>One defensive verb per character — the Sluggers "who you are on defense."</summary>
public static class FieldAbilities
{
    public static double CatchBonus(Character c, RulesTable rules)
    {
        var a = rules.Fielding.Abilities;
        return c.FieldAbility switch
        {
            FieldAbilityId.LickCatch or FieldAbilityId.Grow or FieldAbilityId.Withdraw => a.BigCatchBonusFt,
            FieldAbilityId.SuperJump => a.SuperJumpCatchBonusFt,
            _ => 0
        };
    }

    /// <summary>Lick Catch / Grow reach further on the tag (§10.3): fielding.abilities.tagReachBonusFt.</summary>
    public static double TagReachBonus(Character? c, RulesTable rules) => c?.FieldAbility switch
    {
        FieldAbilityId.LickCatch or FieldAbilityId.Grow => rules.Fielding.Abilities.TagReachBonusFt,
        _ => 0
    };

    public static double FlyRangeBonus(Character c, RulesTable rules) =>
        c.FieldAbility == FieldAbilityId.SuperJump ? rules.Fielding.Abilities.SuperJumpFlyRangeFt : 0;

    /// <summary>
    /// The extra reach on a ball hit on the ground (§8.4): Dive / Burrow on every grounder, Sand Scoop only while the ball is
    /// at or below <c>sandScoopMaxFt</c> (<paramref name="ballY"/>, the ball's height now).
    /// </summary>
    public static double GroundRangeBonus(Character c, RulesTable rules, double ballY = 0) => c.FieldAbility switch
    {
        FieldAbilityId.Dive or FieldAbilityId.Burrow => rules.Fielding.Abilities.DiveGroundRangeFt,
        FieldAbilityId.SandScoop when ballY <= rules.Fielding.Abilities.SandScoopMaxFt => rules.Fielding.Abilities.SandScoopFt,
        _ => 0
    };

    /// <summary>
    /// Sand Scoop's sure hands (§8.4): a ball the glove scoops at or below <c>sandScoopMaxFt</c> never bobbles, whatever the hop.
    /// </summary>
    public static bool SureScoop(Character c, RulesTable rules, double ballY) =>
        c.FieldAbility == FieldAbilityId.SandScoop && ballY <= rules.Fielding.Abilities.SandScoopMaxFt;

    public static double ThrowMul(Character c, RulesTable rules)
    {
        var a = rules.Fielding.Abilities;
        return c.FieldAbility switch
        {
            FieldAbilityId.Laser => a.LaserMul,
            FieldAbilityId.SnapThrow => a.SnapThrowMul,
            _ => 1.0
        };
    }

    /// <summary>Ball Dash (F693-02-ball-dash-carrier, #718): the one ability that is about the body's own feet with the ball in its glove.</summary>
    public static bool HasBallDash(Character c) =>
        c.FieldAbility == FieldAbilityId.BallDash;

    /// <summary>What a body carries the ball at, as a multiple of its pursuit speed: <c>fielding.abilities.ballDashMul</c> for a Ball Dash holder, 1 for everyone else.</summary>
    public static double CarryMul(Character c, RulesTable rules) =>
        HasBallDash(c) ? rules.Fielding.Abilities.BallDashMul : 1.0;

    public static bool IgnoresParkSlow(Character c) =>
        c.FieldAbility == FieldAbilityId.Burrow;

    /// <summary>Super Jump robs a ball clearing the fence by at most fielding.catch.superJumpRobFt (§8.4).</summary>
    public static bool AirRob(Park park, Character fielder, AtBatResult hit, RulesTable rules)
    {
        if (fielder.FieldAbility != FieldAbilityId.SuperJump)
            return false;
        var ball = BattedBall.Of(hit, park, rules);
        return ball.HomeRun && ball.FenceClearFt <= rules.Fielding.Catch.SuperJumpRobFt;
    }

    public static PlayKind SpinCheck(Character fielder, PlayKind kind)
    {
        if (fielder.FieldAbility != FieldAbilityId.SpinCheck)
            return kind;
        return kind switch
        {
            PlayKind.Triple => PlayKind.Double,
            PlayKind.Double => PlayKind.Single,
            _ => kind
        };
    }

    /// <summary>Long Toss (§8.5): the feet its holder's comfortable range reaches past the arm's own before a long throw loses pace; 0 for every other thrower.</summary>
    public static double RangeBonusFt(Character c, RulesTable rules) =>
        c.FieldAbility == FieldAbilityId.LongToss ? rules.Fielding.Abilities.LongTossRangeFt : 0;

    /// <summary>The thrower's arm and ability on a chemistry throw: one speed multiplier the one throw clock reads (§8.5), the arm rating its range is measured from, and Long Toss's reach past it.</summary>
    public static ThrowResult ApplyThrow(Character from, ThrowResult throwRes, RulesTable rules) =>
        throwRes with
        {
            SpeedMul = throwRes.SpeedMul * ThrowMul(from, rules) * InPlay.ArmMul(from, rules), Arm = from.Stats.Arm,
            RangeBonusFt = RangeBonusFt(from, rules)
        };
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
