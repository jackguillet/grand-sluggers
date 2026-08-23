namespace GrandSluggers.Sim;

/// <summary>One defensive verb per character — the Sluggers "who you are on defense."</summary>
public static class FieldAbilities
{
    public static double CatchBonus(Character c) => c.FieldAbility switch
    {
        "lick-catch" or "grow" => 6,
        "super-jump" => 3,
        _ => 0
    };

    public static double FlyRangeBonus(Character c) =>
        c.FieldAbility == "super-jump" ? 22 : 0;

    public static double GroundRangeBonus(Character c) => c.FieldAbility switch
    {
        "dive" or "burrow" => 16,
        _ => 0
    };

    public static double ThrowMul(Character c) => c.FieldAbility switch
    {
        "laser" => 1.45,
        "snap-throw" => 1.22,
        _ => 1.0
    };

    public static bool IgnoresParkSlow(Character c) =>
        c.FieldAbility.Equals("burrow", StringComparison.OrdinalIgnoreCase);

    public static bool AirRob(Park park, Character fielder, AtBatResult hit)
    {
        if (!fielder.FieldAbility.Equals("super-jump", StringComparison.OrdinalIgnoreCase))
            return false;
        var fence = AtBatResolver.FenceAt(park, hit.SprayDeg);
        return hit.CarryFt <= fence + 18;
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

    public static ThrowResult ApplyThrow(Character from, ThrowResult throwRes) =>
        throwRes with { SpeedMul = throwRes.SpeedMul * ThrowMul(from) };
}

public static class ErrorItems
{
    public static readonly string[] All = ["banana", "rocket", "pow"];

    public static string Pick(Random rng) => All[rng.Next(All.Length)];

    public static FieldingResult Apply(FieldingResult field, string item, Random rng)
    {
        var outPlay = field.Kind is PlayKind.FlyOut or PlayKind.GroundOut;
        var turns = item switch
        {
            "banana" => outPlay,
            "rocket" => outPlay && rng.NextDouble() < 0.55,
            "pow" => field.Kind == PlayKind.GroundOut,
            _ => false
        };
        return field with
        {
            Kind = turns ? PlayKind.Single : field.Kind,
            Item = item
        };
    }
}

public static class StarSkills
{
    public static double PitchSpeedMul(string id) => id switch
    {
        "heatball" => 1.15,
        "skullball" => 1.20,
        "charmball" => 0.90,
        "caskball" => 0.85,
        "fastball" => 1.25,
        "changeup" => 0.70,
        "breaker" => 0.85,
        "prismball" => 1.0,
        "phonyball" => 1.0,
        _ => 1.12
    };

    public static double BatterWindowMul(string? starPitch) => starPitch switch
    {
        "charmball" => 0.75,
        "skullball" => 0.80,
        _ => 1.0
    };

    public static double SwingExitMul(string? starSwing) => starSwing switch
    {
        "furnace" => 1.25,
        "cask-swing" => 1.20,
        "heat-swing" => 1.15,
        "shell-swing" or "phony-swing" => 1.10,
        "heart-swing" => 1.05,
        "ground" or "fly" => 1.20,
        "line" => 1.25,
        _ => 1.06
    };
}
