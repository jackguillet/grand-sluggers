namespace GrandSluggers.Sim;

/// <summary>
/// SET dirt tells as product state. Charge ring, zone locator, and ball trail
/// are queryable without F2. Analog pull threshold matches the pad (0.15).
/// </summary>
public static class SetTells
{
    public const double ChargePull = 0.15;

    public static bool RingOn(double charge01) => charge01 >= ChargePull;

    /// <summary>
    /// Major radius in feet. Small values sat under the mesh as a gold pancake.
    /// Pull already clears the toy; max grows around the box.
    /// </summary>
    public static double RingScale(double charge01)
    {
        var u = Math.Clamp(charge01, 0, 1);
        return RingOn(u) ? 3.6 + u * 2.0 : 0;
    }

    /// <summary>Bottom of the tube sits on packed dirt, not on the toy's waist.</summary>
    public const double RingHeightFt = 0.06;

    /// <summary>Torus tube radius. 0.045 vanished from the plate 3/4.</summary>
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
