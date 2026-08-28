namespace GrandSluggers.Sim;

/// <summary>
/// SET dirt tells as product state. Charge ring, zone locator, and ball trail
/// are queryable without F2. Analog pull threshold matches the pad (0.15).
/// </summary>
public static class SetTells
{
    public const double ChargePull = 0.15;

    public static bool RingOn(double charge01) => charge01 >= ChargePull;

    public static double RingScale(double charge01)
    {
        var u = Math.Clamp(charge01, 0, 1);
        // Annulus on packed dirt. 2.1–3.9 ft sat under the mesh as a gold pancake (#221).
        return RingOn(u) ? 5.4 + u * 2.4 : 0;
    }

    /// <summary>Ring sits on packed dirt, not on the toy's waist.</summary>
    public const double RingHeightFt = 0.06;

    /// <summary>Cylinder Y scale. 0.045 vanished from the plate 3/4.</summary>
    public const double RingThickFt = 0.16;

    public static bool ZoneOn(bool setOrFlight) => setOrFlight;

    public static (double X, double Y) Locator(double aimX, double aimY) =>
        PitchFlight.PlateTarget(aimX, aimY);

    public static bool InZone(double aimX, double aimY)
    {
        var pitch = new PitchCommand("fastball", 0, 0, false, aimX, aimY);
        return AtBatResolver.PitchInZone(pitch, 6);
    }

    public static bool TrailOn(bool flight) => flight;
}
