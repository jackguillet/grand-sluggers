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
        return RingOn(u) ? 1.7 + u * 1.5 : 0;
    }

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
