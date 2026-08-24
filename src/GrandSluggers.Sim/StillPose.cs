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
    public const double RunnerX = 42;
    public const double RunnerZ = 42;
    public const string ScoopGlove = "2B";
    public const double CamX = 8;
    public const double CamY = 3.6;
    public const double CamZ = 38;

    public const double PlateCamX = -13.2;
    public const double PlateCamY = 5.2;
    public const double PlateCamZ = -9.2;
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

    public static bool PlateIsThirdBaseThreeQuarter(double x, double z) =>
        x < -6 && z < -3 && z > -12;
}
