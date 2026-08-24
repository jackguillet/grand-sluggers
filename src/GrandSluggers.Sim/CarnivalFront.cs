namespace GrandSluggers.Sim;

/// <summary>
/// Exhibition front of house: the park is the poster, captains are toys, the field is a postcard.
/// HUD draws this copy; tests lock it. Not a second UI toolkit.
/// </summary>
public static class CarnivalFront
{
    public const string Logo = "GRAND SLUGGERS";
    public const string PlayBall = "South / Space    play ball";
    public const float TitleRowZ = 28f;
    public const float SelectRowZ = 12f;
    public const float HomeStepSelectFt = 8f;
    public const float HomeStepTitleFt = 2.4f;
    public const float SelectSpacing = 7.6f;
    public const float TitleSpacing = 13.4f;

    public static (float X, float Z) CaptainSpot(int index, int count, bool select, bool home)
    {
        var spacing = select ? SelectSpacing : TitleSpacing;
        var x = (index - (count - 1) * 0.5f) * spacing;
        var z = select ? SelectRowZ : TitleRowZ;
        if (home) z -= select ? HomeStepSelectFt : HomeStepTitleFt;
        return (x, z);
    }

    public static string SkyGag(bool night) => night ? "NIGHT" : "DAY";

    public static bool HarborIsTheProduct(string parkId) =>
        parkId.Equals("harbor-diamond", StringComparison.OrdinalIgnoreCase);

    /// <summary>One line. Day vs night when the gimmick changes.</summary>
    public static string Gimmick(string parkId, bool night) => parkId.ToLowerInvariant() switch
    {
        "harbor-diamond" => night ? "Night fireworks. Still the real diamond." : "The real diamond.",
        "crystal-rink" => night ? "Ice. The lights go out." : "Ice. Don't fall down.",
        "funfair-park" => night ? "Chompers eat flies." : "Pipes swallow hoppers.",
        "rooftop-city" => "Billboards on a city roof.",
        "canopy-yard" => "Vines and barrels. Climb the wall.",
        "ember-keep" => night ? "Lava breathes farther." : "Lava in the grass.",
        _ => "A park."
    };
}
