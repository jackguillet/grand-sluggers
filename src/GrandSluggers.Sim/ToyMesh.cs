namespace GrandSluggers.Sim;

/// <summary>
/// Rest size of authored extras toys. extras.fbx <c>baseball</c> is 1 Unity-unit
/// diameter — the same as PrimitiveType.Sphere at scale 1. BallView then
/// multiplies by <see cref="Baseball.ApparentScale"/>. Glove extras are rest-1;
/// the glove root takes <see cref="Silhouette.GloveScale"/>.
/// </summary>
public static class ToyMesh
{
    public const float BaseballRestDiameter = 1f;

    public static float BallViewScale(bool inFlight, double z, bool inPlay = false) =>
        (float)(Baseball.ApparentScale(inFlight, z, inPlay) / BaseballRestDiameter);

    public static float GloveRootScale(bool gold) =>
        Silhouette.GloveScale * (gold ? 1.12f : 1f);
}
