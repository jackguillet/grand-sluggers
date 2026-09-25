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
    /// OBR-shaped pentagon widened to the displayed arcade strike zone. Point at
    /// origin (catcher, −Z); depth follows width so the rear edges stay on the foul rays.
    /// </summary>
    public const double AuthoredPlateW = 17 * Inch;
    public const double PlateW = StrikeZoneGeometry.HalfWidth * 2;
    public const double PlateDepth = PlateW;
    public const double PlateMeshScale = PlateW / AuthoredPlateW;
    public const double PlateY = 0.44;
    public const double PlateCenterZ = PlateDepth * 0.5;
    public const double PlateZ = PlateCenterZ;
    public const double PlatePointZ = 0;
    public const double PlateFrontZ = PlateDepth;
    public const double PlateShoulderZ = PlateDepth * 0.5;
    public const double PlatePointW = 0.02;

    /// <summary>Clockwise in X/Z; the authored mesh and missing-art fallback share this footprint.</summary>
    public static (double X, double Z)[] PlateOutline() =>
    [
        (-PlateW * 0.5, PlateFrontZ), (PlateW * 0.5, PlateFrontZ),
        (PlateW * 0.5, PlateShoulderZ), (0, PlatePointZ), (-PlateW * 0.5, PlateShoulderZ)
    ];

    /// <summary>
    /// Six feet deep, narrowed symmetrically from the authored four-foot box as
    /// the plate widens. Preserve the batter centers and six-inch plate gap.
    /// Front is four feet ahead of the plate center; rear is two feet behind it.
    /// </summary>
    public const double BoxW = 4.0 - (PlateW - AuthoredPlateW);
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

    /// <summary>
    /// The box walk's rate at full stick: box offset per second (spec §3, §5.2, PH-09). The client's
    /// rate since the box walk shipped, named here unchanged so the rule can be read by the sim.
    /// </summary>
    public const float BoxWalkPerSec = 1.6f;

    /// <summary>
    /// One frame of box walk for a horizontal stick (spec §3, PH-09): stick × dt × <see cref="BoxWalkPerSec"/>,
    /// computed in single precision as the client always did. It takes no charge: holding a load
    /// before the commit leaves the walk's speed unchanged (PH-09-R1, S-137).
    /// </summary>
    public static double BoxWalkStep(float stickX, float dt) => stickX * dt * BoxWalkPerSec;

    /// <summary>Rubber units a held stick walks the pitcher per second in SET (spec §4.2): the same pace as the box walk.</summary>
    public const float RubberWalkPerSec = 1.6f;

    /// <summary>
    /// One frame of rubber walk for a horizontal stick (spec §4.2): stick × dt × <see cref="RubberWalkPerSec"/>. The human
    /// pitcher's walk and the CPU body's walk to its solved rubber use it, so the two move at one pace.
    /// </summary>
    public static double RubberWalkStep(float stickX, float dt) => stickX * dt * RubberWalkPerSec;
    /// <summary>Feet the pitcher's body, release hand, and crossing move per unit of rubber walk (spec §4.2): once, the same for both seats.</summary>
    public const double PitcherWalk = 2.4;
    public const double BatterChestY = 3.2;

    /// <summary>
    /// A right-handed hitter stands in the third-base box (negative world X);
    /// a left-handed hitter mirrors into the first-base box. The player's box
    /// offset remains a world-X value for either hand.
    /// </summary>
    public static double BatterXFor(Hand hand) => hand == Hand.L ? BoxX : BatterX;

    public static double BatterBodyX(Hand hand, double worldOffsetX = 0) =>
        BatterXFor(hand) + worldOffsetX * BatterWalk;

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

    public static bool FitsStrikeZoneLayout() =>
        Math.Abs(PlateW - StrikeZoneGeometry.HalfWidth * 2) < 1e-9
        && Math.Abs(BoxW - (4 - (PlateW - AuthoredPlateW))) < 1e-9
        && Math.Abs(BoxD - 6) < 1e-9
        && Math.Abs(BoxGap - 6 * Inch) < 1e-9
        && Math.Abs(BoxFrontZ - (PlateCenterZ + 4)) < 1e-9
        && Math.Abs(BoxRearZ - (PlateCenterZ - 2)) < 1e-9
        && Math.Abs(CatcherBoxW - 8) < 1e-9
        && Math.Abs(CatcherBoxD - 43 * Inch) < 1e-9
        && ChalkW >= 2 * Inch && ChalkW <= 4 * Inch
        && FoulLineClearsTheBattersBox();
}
