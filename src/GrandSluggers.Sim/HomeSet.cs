namespace GrandSluggers.Sim;

/// <summary>
/// Home plate as a readable SET: catcher behind the camera, pentagon and
/// two boxes with dirt between them, batter in the third-base box so a
/// behind-home look puts them left of the pitcher.
/// </summary>
public static class HomeSet
{
    /// <summary>Behind the batting SET camera. z=−4 sat in the look cone.</summary>
    public const double CatcherZ = -15;

    /// <summary>Toy-fat MLB pentagon. Point at origin (catcher, −Z). Front toward the mound.</summary>
    public const double PlateW = 2.0;
    public const double PlateDepth = 2.0;
    public const double PlateY = 0.44;
    public const double PlateZ = 1.0;
    public const double PlatePointZ = 0;
    public const double PlateFrontZ = 2.0;
    public const double PlateShoulderZ = 1.0;
    public const double PlatePointW = 0.15;

    /// <summary>MLB 4×6 boxes, 6 in from the plate. Chalk on the dirt.</summary>
    public const double BoxW = 4.0;
    public const double BoxD = 6.0;
    public const double BoxGap = 0.5;
    public const double BoxX = PlateW / 2 + BoxGap + BoxW / 2;
    public const double BoxZ = PlateDepth / 2;
    public const double BoxY = 0.44;

    public const double CatcherBoxW = 8.0;
    public const double CatcherBoxD = 3.5;
    public const double CatcherBoxFrontZ = -3.58;
    public const double CatcherBoxZ = CatcherBoxFrontZ - CatcherBoxD / 2;

    /// <summary>Third-base box. From behind home the batter sits left of the look.</summary>
    public const double BatterX = -BoxX;
    public const double BatterZ = 3.0;
    public const double BatterWalk = 2.4;
    public const double BatterChestY = 3.2;

    /// <summary>Behind home, slight first-base so the RH batter is left, looking at the mound.</summary>
    public const double CamX = 1.2;
    public const double CamY = 6.0;
    public const double CamZ = -9.5;
    public const double LookX = 0.2;
    public const double LookY = 1.4;
    public const double LookZ = 16;
    public const double Fov = 50;

    public static bool CameraIsBehindHome(double x, double z) =>
        Math.Abs(x) < 4 && z < -7 && z > -18;

    public static bool CatcherIsBehindCamera(double camZ) =>
        CatcherZ < camZ - 2;

    public static bool BoxesClearThePlate()
    {
        var gap = BoxX - BoxW / 2 - PlateW / 2;
        return gap >= 0.45 && gap <= 0.7;
    }

    public static bool PlatePointFacesTheCatcher() =>
        PlatePointZ <= 0.05 && PlateFrontZ > PlateW * 0.5;
}
