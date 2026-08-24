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
        "vale" => new(1.22f, 0.72f, 1.22f, 0.92f, 0.78f),
        "zig" => new(0.64f, 1.26f, 1.55f, 0.92f, 0.70f),
        "brondo" => new(1.00f, 1.55f, 1.12f, 1.22f, 1.42f),
        "konga" => new(1.32f, 1.40f, 1.35f, 1.55f, 1.18f),
        "ashlord" => new(1.26f, 1.16f, 1.30f, 1.14f, 1.30f),
        _ => new(0.88f, 0.98f, 1.45f, 1.05f, 0.92f)
    };

    /// <summary>Toy read: face must be at least as big as the body is tall.</summary>
    public static float HeadToHeight(Spec spec) => spec.Head / Math.Max(0.01f, spec.Height);
}
