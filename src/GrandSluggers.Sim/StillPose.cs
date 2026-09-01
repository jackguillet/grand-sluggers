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

    /// <summary>
    /// First-base 3/4 behind home. Batter sits left of the look; the pitcher
    /// is in the diamond, not a dirt close-up of the box.
    /// </summary>
    public const double PlateCamX = 8.0;
    public const double PlateCamY = 5.0;
    public const double PlateCamZ = -5.6;
    public const double PlateLookX = 0.4;
    public const double PlateLookY = 1.8;
    public const double PlateLookZ = 16;
    public const double PlateFov = 54;

    public const double PitchCamX = -12.0;
    public const double PitchCamY = 5.0;
    public const double PitchCamZ = -4.8;
    public const double PitchLookX = 0.7;
    public const double PitchLookY = 5.1;
    public const double PitchLookZ = 57.0;
    public const double PitchFov = 34;

    /// <summary>
    /// First-base 3/4 behind the rubber. Pitcher sits right of the look;
    /// rubber in the bottom; the box at home is the look, not CF or brim.
    /// </summary>
    public const double MoundCamX = 5.0;
    public const double MoundCamY = 5.4;
    public const double MoundCamZ = 72.0;
    public const double MoundLookX = 0.4;
    public const double MoundLookY = 1.2;
    public const double MoundLookZ = 6.0;
    public const double MoundFov = 46;
    /// <summary>Just off the hand, still on the pitcher, coming at the box.</summary>
    public const double PitchBallU = 0.12;

    /// <summary>Throwing hand must be on the rubber. Home-plate from was a beach ball in the lens.</summary>
    public static bool PitchReleaseIsOnTheMound(double z) =>
        z > Diamond.Mound - 16 && z < Diamond.Mound + 8;

    public static bool PitchBallIsOffTheHand(double ballZ) =>
        ballZ > 40 && ballZ < Diamond.Mound;

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

    public static bool PlateIsBatterOverShoulder(double x, double z) =>
        x > 6 && z < -3 && z > -12;

    public static bool MoundIsPitcherOverShoulder(double x, double z) =>
        x > 4 && z > Diamond.Mound + 8 && z < Diamond.Mound + 16;

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

    /// <summary>From the box, third-base 3/4, looking at the pitcher — not the dirt.</summary>
    public static bool PitchLooksAtTheThrow(double camX, double camZ, double lookY, double lookZ) =>
        camX < -6 && camZ < 0 && camZ > -12 && lookZ > 45 && lookY > 3.5;
}
