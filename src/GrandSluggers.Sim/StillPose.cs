namespace GrandSluggers.Sim;

/// <summary>
/// Staged still-gate poses. Scoop lives on the dirt in the first-base hole,
/// not on the rubber — the 2026-08-24 PNG was Ashlord on the mound.
/// </summary>
public static class StillPose
{
    public const double ScoopX = 26;
    public const double ScoopZ = 26;
    public const double ScoopBallY = 0.40;
    /// <summary>Authored contact. Camera must be a side 3/4 — down the path hides the glove.</summary>
    public const double ScoopPoseT = 0.22;
    public const double RunnerX = 34;
    public const double RunnerZ = 34;
    public const string ScoopGlove = "2B";
    public const double CamX = 8;
    public const double CamY = 3.2;
    public const double CamZ = 16;
    public const double ScoopLookX = 30;
    public const double ScoopLookY = 0.55;
    public const double ScoopLookZ = 30;

    public const double PlateCamX = -12.5;
    public const double PlateCamY = 5.5;
    public const double PlateCamZ = -5.6;
    public const double PlateLookX = 2.55;
    public const double PlateLookY = 1.05;
    public const double PlateLookZ = 14;
    public const double PlateFov = 52;

    public static bool ScoopIsNotTheMound(double x, double z) =>
        Diamond.Dist(x, z, 0, Diamond.Mound) > 20;

    public static bool CameraClearsTheDugout(double x, double z) =>
        x < 26 || z > 24 || z < 4;

    public static bool CameraIsSideThreeQuarter(double camX, double camZ, double scoopX, double scoopZ) =>
        Math.Abs(camZ - scoopZ) > 8 && Math.Abs(camX - scoopX) > 8;

    /// <summary>Runner is toward first and in front of the camera, not a sliver behind the lens.</summary>
    public static bool RunnerLeavesInFrame(double camX, double camZ, double scoopX, double scoopZ, double runX, double runZ) =>
        runX > scoopX && runZ > scoopZ
        && runX < Diamond.First.X && runZ < Diamond.First.Z
        && (runX - camX) + (runZ - camZ) > 12;

    public static bool PlateIsThirdBaseThreeQuarter(double x, double z) =>
        x < -6 && z < -3 && z > -12;

    /// <summary>
    /// Catcher is at (0, -4). A camera further behind home puts him in the
    /// look cone; the bound mesh then owns the right foreground.
    /// </summary>
    public static bool PlateCatcherClearsTheLens(double camX, double camZ, double lookX, double lookZ)
    {
        var (cx, cz) = Diamond.Positions["C"];
        var ldx = lookX - camX;
        var ldz = lookZ - camZ;
        var cdx = cx - camX;
        var cdz = cz - camZ;
        var lookLen = Math.Sqrt(ldx * ldx + ldz * ldz);
        var cLen = Math.Sqrt(cdx * cdx + cdz * cdz);
        if (lookLen < 1 || cLen < 1) return false;
        var cos = (ldx * cdx + ldz * cdz) / (lookLen * cLen);
        var deg = Math.Acos(Math.Clamp(cos, -1, 1)) * 180 / Math.PI;
        return deg > 40;
    }
}
