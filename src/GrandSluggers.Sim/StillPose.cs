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

    /// <summary>Behind home looking at the mound. Numbers live on <see cref="HomeSet"/>.</summary>
    public const double PlateCamX = HomeSet.CamX;
    public const double PlateCamY = HomeSet.CamY;
    public const double PlateCamZ = HomeSet.CamZ;
    public const double PlateLookX = HomeSet.LookX;
    public const double PlateLookY = HomeSet.LookY;
    public const double PlateLookZ = HomeSet.LookZ;
    public const double PlateFov = HomeSet.Fov;

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

    /// <summary>
    /// Character turntable. 3/4 on the face/chest, full toy in frame.
    /// Look-at is a world point (the camera), not a direction.
    /// XZ is the authored 3/4 at Rio height; <see cref="CharFraming"/> looks at
    /// the toy's chest and pulls back by extra height from
    /// <see cref="Silhouette.Proportions"/>. A constant Y cut Ashlord and Konga.
    /// </summary>
    public const double CharX = 0;
    public const double CharZ = 20;
    public const double CharCamX = 8;
    public const double CharCamZ = 9;
    /// <summary>Camera sits this above the chest so the look is not the brim.</summary>
    public const double CharCamLift = 0.9;
    public const double CharFov = 34;
    public const double CharPoseT = Motion.SwingContact;

    /// <summary>Torso joint origin in data/art/rig.json. World chest is this × SharedRootScale.Y.</summary>
    public const double CharUnscaledChestY = 2.95;
    /// <summary>HEAD.z — same landmark as <see cref="SwingPresentation.HeadCenterAtRest"/>.</summary>
    public static readonly double CharUnscaledHeadCenterY = SwingPresentation.HeadCenterAtRest.Y;
    /// <summary>Same radius as <see cref="SwingPresentation.HeadRadius"/>.</summary>
    public const double CharUnscaledHeadRadius = SwingPresentation.HeadRadius;

    public static double CharChestY(string bodyType)
    {
        var spec = Silhouette.Proportions(bodyType);
        return CharUnscaledChestY * Silhouette.SharedRootScale(spec).Y;
    }

    public static double CharHeadTopY(string bodyType)
    {
        var spec = Silhouette.Proportions(bodyType);
        return (CharUnscaledHeadCenterY + CharUnscaledHeadRadius) * Silhouette.SharedRootScale(spec).Y;
    }

    /// <summary>
    /// 3/4 on this captain. Look is the chest from <see cref="Silhouette.Proportions"/>;
    /// the camera pulls back by extra height above the Rio-sized template so a
    /// taller cut (Ashlord, Konga) keeps its head in the frustum.
    /// </summary>
    public static CameraShot CharFraming(string bodyType)
    {
        var chestY = CharChestY(bodyType);
        var height = CharHeadTopY(bodyType);
        var dx = CharCamX - CharX;
        var dz = CharCamZ - CharZ;
        var template = Math.Sqrt(dx * dx + dz * dz);
        var dist = template + Math.Max(0, height - CharHeadTopY("rio"));
        CameraShot AtDistance(double distance)
        {
            var u = distance / template;
            var pos = new Vec3(CharX + dx * u, chestY + CharCamLift, CharZ + dz * u);
            return new CameraShot("select", "chest", pos, new Vec3(CharX, chestY, CharZ), CharFov, 0);
        }
        bool Fits(double distance)
        {
            var shot = AtDistance(distance);
            return PlayCamera.InFrame(PlayCamera.Project(shot, new Vec3(CharX, 0, CharZ)), 0.04)
                && PlayCamera.InFrame(PlayCamera.Project(shot, new Vec3(CharX, height, CharZ)), 0.04);
        }
        // The taller rig raises the chest. Fit feet as well as head while
        // preserving the same chest target and 3/4 direction for every captain.
        if (!Fits(dist))
        {
            var near = dist;
            var far = dist * 2;
            while (!Fits(far)) far *= 2;
            for (var i = 0; i < 32; i++)
            {
                var mid = (near + far) / 2;
                if (Fits(mid)) far = mid;
                else near = mid;
            }
            dist = far;
        }
        return AtDistance(dist);
    }

    public static bool CharCameraLooksAtChest(double lookY, double chestY) =>
        Math.Abs(lookY - chestY) < 0.05;

    public static bool CharCameraIsNotBrim(double lookY, double camY) => lookY < camY;

    public static bool CharCameraIsThreeQuarter(double camX, double camZ, double charZ) =>
        Math.Abs(camX) >= 6 && camZ < charZ && charZ - camZ >= 8;

    public static bool CharHeadTopInFrame(string bodyType, double margin = 0.04)
    {
        var shot = CharFraming(bodyType);
        var top = new Vec3(CharX, CharHeadTopY(bodyType), CharZ);
        return PlayCamera.InFrame(PlayCamera.Project(shot, top), margin);
    }

    /// <summary>Throwing hand must be on the rubber. Home-plate from was a beach ball in the lens.</summary>
    public static bool PitchReleaseIsOnTheMound(double z) =>
        z > Diamond.Mound - 16 && z < Diamond.Mound + 8;

    public static bool PitchBallIsOffTheHand(double ballZ) =>
        ballZ > 40 && ballZ < Diamond.Mound;

    public static bool ScoopIsNotTheMound(double x, double z) =>
        Diamond.Dist(x, z, 0, Diamond.Mound) > 20;

    public static bool CameraClearsTheDugout(double x, double z) =>
        HarborDugout.CameraClears(x, z);

    public static bool CameraIsSideThreeQuarter(double camX, double camZ, double scoopX, double scoopZ) =>
        Math.Abs(camZ - scoopZ) > 8 && Math.Abs(camX - scoopX) > 8;

    /// <summary>Runner is toward first and in front of the camera, not a sliver behind the lens.</summary>
    public static bool RunnerLeavesInFrame(double camX, double camZ, double scoopX, double scoopZ, double runX, double runZ) =>
        runX > scoopX && runZ > scoopZ
        && runX < Diamond.First.X && runZ < Diamond.First.Z
        && (runX - camX) + (runZ - camZ) > 12;

    public static bool PlateIsBehindHome(double x, double z) =>
        HomeSet.CameraIsBehindHome(x, z);

    public static bool MoundIsPitcherOverShoulder(double x, double z) =>
        x > 4 && z > Diamond.Mound + 8 && z < Diamond.Mound + 16;

    /// <summary>
    /// Catcher crouches behind the batting SET (<see cref="HomeSet.CatcherZ"/>).
    /// A catcher at z=−4 sat in the look cone and owned the foreground.
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
