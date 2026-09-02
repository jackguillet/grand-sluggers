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

    /// <summary>Home pad / batter's box top. Ring sits on this, not at Y=0 under the mesh.</summary>
    public const double BoxDirtY = 0.26;

    /// <summary>Mound hill / rubber top. Pitching SET uses the same ring language here.</summary>
    public const double RubberDirtY = 0.96;

    /// <summary>
    /// World Y of the torus center at the player's feet. Packed dirt, never chest
    /// or a child of hero lift/grow. Presentation must not parent this to the toy.
    /// </summary>
    public static double RingWorldY(double feetZ = 0, double heroY = 0, double lift = 0)
    {
        _ = heroY;
        _ = lift;
        var dirt = Math.Abs(feetZ - Diamond.Mound) < 12 ? RubberDirtY : BoxDirtY;
        return dirt + RingThickFt;
    }

    public static (double X, double Y, double Z) RingAt(
        double feetX, double feetZ, double heroY = 0, double lift = 0) =>
        (feetX, RingWorldY(feetZ, heroY, lift), feetZ);

    public static bool ZoneOn(bool setOrFlight) => setOrFlight;

    public static (double X, double Y) Locator(double aimX, double aimY) =>
        PitchFlight.PlateTarget(aimX, aimY);

    public static bool InZone(double aimX, double aimY)
    {
        var pitch = new PitchCommand("fastball", 0, 0, false, aimX, aimY);
        return AtBatResolver.PitchInZone(pitch, 6);
    }

    public static bool TrailOn(bool flight) => flight;

    /// <summary>Seconds of streak. 0.28 vanished on plate SET.</summary>
    public const double TrailSeconds = 0.62;

    /// <summary>Start width as a fraction of the live ball diameter.</summary>
    public const double TrailStartMul = 0.62;

    public const double TrailEndMul = 0.10;

    public static double TrailStartFt(double diameter) =>
        Math.Max(0.28, diameter * TrailStartMul);

    public static double TrailEndFt(double diameter) =>
        Math.Max(0.06, diameter * TrailEndMul);
}
