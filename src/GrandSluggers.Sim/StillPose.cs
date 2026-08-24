namespace GrandSluggers.Sim;

/// <summary>
/// Staged still-gate poses. Scoop lives on the dirt in the first-base hole,
/// not on the rubber — the 2026-08-24 PNG was Ashlord on the mound.
/// </summary>
public static class StillPose
{
    public const double ScoopX = 26;
    public const double ScoopZ = 26;
    public const double ScoopBallY = 0.55;
    /// <summary>Mid-pick. Later t stands them up (12:22 PNG).</summary>
    public const double ScoopPoseT = 0.20;
    public const double RunnerX = 42;
    public const double RunnerZ = 42;
    public const string ScoopGlove = "2B";
    public const double CamX = 10;
    public const double CamY = 4.8;
    public const double CamZ = 12;

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
        x < 24 && z < 28;

    public static bool PlateIsThirdBaseThreeQuarter(double x, double z) =>
        x < -6 && z < -3 && z > -12;
}
