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
        // Height ladder vs SMS research stills (Baby < Mario ≈ Wario < Peach < DK < Bowser).
        "vale" => new(1.24f, 0.70f, 1.24f, 0.88f, 0.74f),
        "zig" => new(0.56f, 1.18f, 1.62f, 0.82f, 0.68f),
        "brondo" => new(0.96f, 1.58f, 1.16f, 1.28f, 1.48f),
        "konga" => new(1.30f, 1.36f, 1.34f, 1.72f, 1.20f),
        "ashlord" => new(1.44f, 1.28f, 1.48f, 1.18f, 1.38f),
        _ => new(0.90f, 1.00f, 1.38f, 1.02f, 0.94f)
    };

    /// <summary>Toy read: face must be at least as big as the body is tall.</summary>
    public static float HeadToHeight(Spec spec) => spec.Head / Math.Max(0.01f, spec.Height);
}
