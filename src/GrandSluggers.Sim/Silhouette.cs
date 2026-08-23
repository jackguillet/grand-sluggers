namespace GrandSluggers.Sim;

/// <summary>
/// Locked body types. Role players reuse the faction captain. See docs/silhouette-bible.md.
/// Scale factors are Unity root multipliers used by HeroActor.Build.
/// </summary>
public static class Silhouette
{
    public readonly record struct Spec(float Height, float Width, float Head, float Arms, float Torso);

    public static readonly string[] Captains = ["rio", "vale", "zig", "brondo", "konga", "ashlord"];

    public static string BodyType(Character who)
    {
        if (who.Captain) return who.Id.ToLowerInvariant();
        return who.Faction.ToLowerInvariant() switch
        {
            "royal" => "vale",
            "carnival" => "zig",
            "goldrush" => "brondo",
            "canopy" => "konga",
            "ember" => "ashlord",
            _ => "rio"
        };
    }

    public static Spec Proportions(Character who) => Proportions(BodyType(who));

    public static Spec Proportions(string bodyType) => bodyType.ToLowerInvariant() switch
    {
        "vale" => new(1.22f, 0.68f, 0.82f, 0.88f, 0.72f),
        "zig" => new(0.64f, 1.26f, 1.55f, 0.92f, 0.70f),
        "brondo" => new(1.00f, 1.55f, 0.80f, 1.18f, 1.42f),
        "konga" => new(1.32f, 1.40f, 1.10f, 1.55f, 1.18f),
        "ashlord" => new(1.26f, 1.16f, 1.12f, 1.10f, 1.30f),
        _ => new(0.88f, 0.90f, 1.22f, 0.95f, 0.88f)
    };
}
