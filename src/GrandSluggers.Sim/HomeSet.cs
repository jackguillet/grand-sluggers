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

    public const double Inch = 1.0 / 12.0;

    /// <summary>
    /// OBR 2.02 pentagon. Point at origin (catcher, −Z). 17″ front toward the mound.
    /// 8½″ sides then 12″ sides on the foul lines.
    /// </summary>
    public const double PlateW = 17 * Inch;
    public const double PlateDepth = 17 * Inch;
    public const double PlateY = 0.44;
    public const double PlateCenterZ = 8.5 * Inch;
    public const double PlateZ = PlateCenterZ;
    public const double PlatePointZ = 0;
    public const double PlateFrontZ = 17 * Inch;
    public const double PlateShoulderZ = 8.5 * Inch;
    public const double PlatePointW = 0.02;

    /// <summary>
    /// OBR Diagram 2 / NCAA: 4′ × 6′, 6″ from the plate. Front is 4′ in front of
    /// a line through the plate center; rear is 2′ behind that line.
    /// </summary>
    public const double BoxW = 4.0;
    public const double BoxD = 6.0;
    public const double BoxGap = 6 * Inch;
    public const double BoxFrontFromCenter = 4.0;
    public const double BoxRearFromCenter = 2.0;
    public const double BoxX = PlateW / 2 + BoxGap + BoxW / 2;
    public const double BoxFrontZ = PlateCenterZ + BoxFrontFromCenter;
    public const double BoxRearZ = PlateCenterZ - BoxRearFromCenter;
    public const double BoxZ = (BoxFrontZ + BoxRearZ) / 2.0;
    public const double BoxY = 0.44;

    /// <summary>OBR Diagram 2: 8′ wide, 43″ deep, flush with the batter’s-box rear.</summary>
    public const double CatcherBoxW = 8.0;
    public const double CatcherBoxD = 43 * Inch;
    public const double CatcherBoxFrontZ = BoxRearZ;
    public const double CatcherBoxZ = CatcherBoxFrontZ - CatcherBoxD / 2.0;

    /// <summary>Official chalk is 2–4″. 4″ so the line reads at couch.</summary>
    public const double ChalkW = 4 * Inch;

    /// <summary>
    /// Long foul chalk starts past the front of the batter’s box. The plate’s
    /// 12″ edges already begin the line; drawing from the point slices the box.
    /// </summary>
    public static double FoulLineStartZ => BoxFrontZ + ChalkW;
    public static double FoulLineStartAlong => FoulLineStartZ * Math.Sqrt(2);

    public static bool FoulLineClearsTheBattersBox() =>
        FoulLineStartZ >= BoxFrontZ;

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

    public static double BoxInnerX => BoxX - BoxW / 2;

    public static bool BoxesClearThePlate()
    {
        var gap = BoxInnerX - PlateW / 2;
        return Math.Abs(gap - BoxGap) < 0.001;
    }

    public static bool PlatePointFacesTheCatcher() =>
        PlatePointZ <= 0.05 && PlateFrontZ > PlateW * 0.5;

    public static bool IsOfficialLayout() =>
        Math.Abs(PlateW - 17 * Inch) < 1e-9
        && Math.Abs(BoxW - 4) < 1e-9
        && Math.Abs(BoxD - 6) < 1e-9
        && Math.Abs(BoxGap - 6 * Inch) < 1e-9
        && Math.Abs(BoxFrontZ - (PlateCenterZ + 4)) < 1e-9
        && Math.Abs(BoxRearZ - (PlateCenterZ - 2)) < 1e-9
        && Math.Abs(CatcherBoxW - 8) < 1e-9
        && Math.Abs(CatcherBoxD - 43 * Inch) < 1e-9
        && ChalkW >= 2 * Inch && ChalkW <= 4 * Inch
        && FoulLineClearsTheBattersBox();
}
