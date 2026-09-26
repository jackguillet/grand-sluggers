namespace GrandSluggers.Sim;

/// <summary>
/// In-play body scale. Every body plays at rest scale: no field ability grows the toy (AB-12), and YOU, the switch hint
/// and a glove with the ball stay rest scale — do not scale the toy to sell the glove. Cameras look at the chest.
/// </summary>
public static class BodyScale
{
    public const double Rest = 1;

    /// <summary>Highlight and hint never enter the product: the body is rest scale.</summary>
    public static double Of(bool highlighted = false, bool hint = false)
    {
        _ = highlighted;
        _ = hint;
        return Rest;
    }
}
