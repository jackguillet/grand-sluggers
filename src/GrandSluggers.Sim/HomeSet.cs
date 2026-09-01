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

    public const double PlateW = 2.4;
    public const double PlateD = 1.8;
    public const double PlateY = 0.22;
    public const double PlateZ = 0.2;
    public const double PlatePointZ = 1.25;
    public const double PlatePointW = 1.6;

    public const double BoxW = 4.8;
    public const double BoxD = 7.0;
    public const double BoxX = 5.0;
    public const double BoxZ = 3.4;
    public const double BoxY = 0.20;

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

    public static bool BoxesClearThePlate() =>
        BoxX - BoxW / 2 > PlateW / 2 + 0.8;
}
