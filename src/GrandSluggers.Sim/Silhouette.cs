namespace GrandSluggers.Sim;

/// <summary>
/// Locked body types. Role players reuse the faction captain. See docs/silhouette-bible.md. The body rows are data:
/// each captain's <c>proportions</c> in <c>data/characters</c> (#1032).
/// Scale factors are Unity root multipliers used by HeroActor.Build.
/// </summary>
public static class Silhouette
{
    public readonly record struct Spec(float Height, float Width, float Head, float Arms, float Torso);

    /// <summary>SMS toys sit bigger than an honest diamond. Applied on the shared chain root.</summary>
    public const float ToyScale = 1.18f;
    public const float GloveScale = 1.42f;
    public const float BatScale = 1.28f;

    /// <summary>The captain whose body this character wears: data (<see cref="Character.BodyType"/>), resolved at load.</summary>
    public static string BodyType(Character who) => who.BodyType;

    /// <summary>Lineup face. Role players reuse the faction captain — no unique JPGs.</summary>
    public static string PortraitId(Character who) => who.BodyType;

    public static Spec Proportions(Character who) => who.Proportions;

    /// <summary>A body type's proportions: the captain of that id's authored row (<c>data/characters</c>).</summary>
    public static Spec Proportions(ContentCatalog content, string bodyType) => content.Must(bodyType).Proportions;

    /// <summary>
    /// Shared-rig axes keep stature on Y while X/Z blend girth with height.
    /// Swing reach and Unity presentation consume this same scale relationship.
    /// </summary>
    public static Vec3 SharedRootScale(Spec spec) => new(
        (spec.Height * 0.45 + spec.Width * 0.55) * ToyScale,
        spec.Height * ToyScale,
        (spec.Height * 0.55 + spec.Width * 0.45) * ToyScale);

    /// <summary>Toy read: face must be at least as big as the body is tall.</summary>
    public static float HeadToHeight(Spec spec) => spec.Head / Math.Max(0.01f, spec.Height);
}
