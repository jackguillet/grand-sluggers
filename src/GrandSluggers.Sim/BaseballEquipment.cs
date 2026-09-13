namespace GrandSluggers.Sim;

/// <summary>Equipment handedness is anatomy, independent of batting hand or seat.</summary>
public static class BaseballEquipment
{
    public static readonly Vec3 GlovePocket = new(0, .26, -.14);

    public static Hand GloveHand(Hand throws) => throws == Hand.L ? Hand.R : Hand.L;

    /// <summary>Right-worn gloves are reflected in Blender, never by a negative runtime scale.</summary>
    public static string GloveMesh(Hand throws, bool gold = false) =>
        (gold ? "glove-gold" : "glove-brown") + (GloveHand(throws) == Hand.R ? "-R" : "");
}
