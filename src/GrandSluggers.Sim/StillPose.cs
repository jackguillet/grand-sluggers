namespace GrandSluggers.Sim;

/// <summary>
/// Staged still-gate poses. Scoop lives on the dirt in the first-base hole,
/// not on the rubber — the 2026-08-24 PNG was Ashlord on the mound.
/// </summary>
public static class StillPose
{
    public const double ScoopX = 24;
    public const double ScoopZ = 36;
    public const double ScoopBallY = 0.85;
    /// <summary>Mid-pick. Later t stands them up (12:22 PNG).</summary>
    public const double ScoopPoseT = 0.20;
    public const double RunnerX = 30;
    public const double RunnerZ = 30;
    public const string ScoopGlove = "2B";
    public const double CamX = 6;
    public const double CamY = 5.8;
    public const double CamZ = 20;

    public const double PlateCamX = -11.4;
    public const double PlateCamY = 5.5;
    public const double PlateCamZ = -7.0;
    public const double PlateLookX = 2.55;
    public const double PlateLookY = 1.25;
    public const double PlateLookZ = 34;
    public const double PlateFov = 50;

    public static bool ScoopIsNotTheMound(double x, double z) =>
        Diamond.Dist(x, z, 0, Diamond.Mound) > 20;

    public static bool CameraClearsTheDugout(double x, double z) =>
        x < 24 && z < 28;

    public static bool PlateIsThirdBaseThreeQuarter(double x, double z) =>
        x < -6 && z < -3 && z > -12;
}
