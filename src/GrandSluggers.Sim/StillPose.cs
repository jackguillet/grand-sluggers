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

    /// <summary>Beside the batter, looking at the mound. Batter fills the right foreground.</summary>
    public const double PlateCamX = 0.4;
    public const double PlateCamY = 3.6;
    public const double PlateCamZ = -2.8;
    public const double PlateLookX = 0.8;
    public const double PlateLookY = 4.4;
    public const double PlateLookZ = 52;
    public const double PlateFov = 54;

    /// <summary>3/4 over the pitcher, looking at the box. Home is the picture, not CF dirt.</summary>
    public const double MoundCamX = -3.8;
    public const double MoundCamY = 7.6;
    public const double MoundCamZ = 76.0;
    public const double MoundLookX = 0.4;
    public const double MoundLookY = 2.2;
    public const double MoundLookZ = 2.5;
    public const double MoundFov = 46;

    public const double PitchCamX = 0.5;
    public const double PitchCamY = 3.4;
    public const double PitchCamZ = -2.6;
    public const double PitchLookX = 0.4;
    public const double PitchLookY = 5.2;
    public const double PitchLookZ = 57.0;
    public const double PitchFov = 36;
    /// <summary>Just off the hand, still on the pitcher, coming at the box.</summary>
    public const double PitchBallU = 0.12;

    public const double BatterX = 2.55;
    public const double BatterY = 2.4;
    public const double BatterZ = 2.4;

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

    /// <summary>Beside the RH box, slightly behind home. Not a far 3B park shot and not catcher-spine.</summary>
    public static bool PlateIsBesideTheBatter(double x, double z) =>
        x > -4 && x < 2 && z < 0 && z > -8;

    public static bool PlateIsThirdBaseThreeQuarter(double x, double z) =>
        PlateIsBesideTheBatter(x, z);

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

    /// <summary>From the box, beside the batter, looking at the pitcher — not the dirt.</summary>
    public static bool PitchLooksAtTheThrow(double camX, double camZ, double lookY, double lookZ) =>
        camX > -4 && camX < 3 && camZ < 0 && camZ > -8 && lookZ > 45 && lookY > 3.5;

    /// <summary>SET batting looks at the mound so the pitcher and the ball share the frame.</summary>
    public static bool PlateLooksAtTheMound(double lookY, double lookZ) =>
        lookZ > 40 && lookY > 3.0;

    /// <summary>SET pitching is behind the rubber looking at home, not at CF dirt.</summary>
    public static bool MoundLooksAtTheBox(double camZ, double lookY, double lookZ) =>
        camZ > Diamond.Mound && lookZ < 12 && lookY > 1.5;

    public static double LookDeg(double posX, double posY, double posZ, double lookX, double lookY, double lookZ, double px, double py, double pz)
    {
        var lx = lookX - posX;
        var ly = lookY - posY;
        var lz = lookZ - posZ;
        var dx = px - posX;
        var dy = py - posY;
        var dz = pz - posZ;
        var ln = Math.Sqrt(lx * lx + ly * ly + lz * lz);
        var dn = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (ln < 1e-6 || dn < 1e-6) return 180;
        var dot = Math.Clamp((lx * dx + ly * dy + lz * dz) / (ln * dn), -1, 1);
        return Math.Acos(dot) * 180 / Math.PI;
    }

    public static bool InFov(double posX, double posY, double posZ, double lookX, double lookY, double lookZ, double px, double py, double pz, double fov) =>
        LookDeg(posX, posY, posZ, lookX, lookY, lookZ, px, py, pz) < fov * 0.5;
}
