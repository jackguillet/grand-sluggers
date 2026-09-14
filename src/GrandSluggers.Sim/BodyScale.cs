namespace GrandSluggers.Sim;

/// <summary>
/// In-play body scale. Grow is the field verb (a bigger catch). YOU, the
/// switch hint, and a glove with the ball stay rest scale — do not scale
/// the toy to sell the glove. Cameras look at the chest.
/// </summary>
public static class BodyScale
{
    public const double Rest = 1;
    public const double Grow = 1.45;
    public const string GrowAbility = "grow";

    public static bool IsGrow(string? fieldAbility) =>
        string.Equals(fieldAbility, GrowAbility, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Grow puffs the play glove while it is going for the ball. Holding it
    /// is rest. A YOU highlight or switch hint is not Grow.
    /// </summary>
    public static bool GrowOn(string? fieldAbility, bool playGlove, bool holdBall) =>
        IsGrow(fieldAbility) && playGlove && !holdBall;

    /// <summary>
    /// Highlight and hint never enter the product. Holding the ball cancels
    /// Grow so a pocketed ball is the same silhouette plus the ball.
    /// </summary>
    public static double Of(
        bool grow,
        bool holdBall = false,
        bool highlighted = false,
        bool hint = false)
    {
        _ = highlighted;
        _ = hint;
        return grow && !holdBall ? Grow : Rest;
    }

    public static double Live(
        string? fieldAbility,
        bool playGlove,
        bool holdBall,
        bool highlighted = false,
        bool hint = false) =>
        Of(GrowOn(fieldAbility, playGlove, holdBall), holdBall, highlighted, hint);
}
