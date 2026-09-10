namespace GrandSluggers.Sim;

/// <summary>
/// Shared charge language for pitch and swing: fill to MAX, hold, then decay.
/// </summary>
public static class ChargeFeel
{
    public const double SlapBelow = 0.2;
    public const double ChargeAt = 0.55;

    public static double Effective01(double fill01, double secondsPastFull, double maxHold, double decayPerSec)
    {
        fill01 = Math.Clamp(fill01, 0, 1);
        if (fill01 < 1) return fill01;
        if (secondsPastFull <= maxHold) return 1;
        return Math.Clamp(1 - (secondsPastFull - maxHold) * decayPerSec, SlapBelow, 1);
    }

    public static bool AtMax(double fill01, double secondsPastFull, double maxHold) =>
        fill01 >= 1 && secondsPastFull <= maxHold;

    public static bool IsSlap(double effective01) => effective01 < SlapBelow;

    public static bool IsCharge(double effective01) => effective01 >= ChargeAt;

    public static string NiceCopy(bool pitching, double fill01, double secondsPastFull, double maxHold) =>
        AtMax(fill01, secondsPastFull, maxHold)
            ? (pitching ? "Nice!" : "Nice Hit!")
            : "";
}

/// <summary>
/// Sweet-spot oval at the plate, smaller than the zone. Center follows the batter.
/// </summary>
public static class SweetSpot
{
    public const double HalfWidth = 0.32;
    public const double HalfHeight = 0.28;

    public static double Overlap(double boxOffsetX, double pitchAimX, double pitchAimY)
    {
        var dx = (pitchAimX - boxOffsetX) / HalfWidth;
        var dy = pitchAimY / HalfHeight;
        var d2 = dx * dx + dy * dy;
        if (d2 <= 1) return 1;
        if (d2 <= 2.25) return 0.35;
        return 0;
    }

    public static bool CenterEatsHeart() => Overlap(0, 0, 0) >= 1;

    public static bool WalkedOffMissesHeart(double walk = 0.85) =>
        Overlap(walk, 0, 0) <= 0;
}

/// <summary>Fielding dash and buddy-toss before the glove.</summary>
public static class FieldDash
{
    public const double ChaseMul = 1.35;

    public static bool BuddyTossOffered(Chemistry rel, double distFt) =>
        rel == Chemistry.Good && distFt < 28;

    public static FieldingResult ApplyBuddyToss(FieldingResult field, Character partner, ThrowResult thr) =>
        field with { Fielder = partner, Throw = thr };

    public const double KickFt = 22;
    public const double DiveLungeFt = 10;

    public static bool KickOffered(double distFt) => distFt < KickFt;

    /// <summary>Dive carries the body toward the ball. Not a teleport.</summary>
    public static (double X, double Z) Lunge(double x, double z, double tx, double tz, double ft = DiveLungeFt)
    {
        var dx = tx - x;
        var dz = tz - z;
        var d = Math.Sqrt(dx * dx + dz * dz);
        if (d < 0.01) return (x, z);
        var u = Math.Min(1, ft / d);
        return (x + dx * u, z + dz * u);
    }

    public static bool DestroysItem(bool attack, bool itemFlying, double distFt) =>
        attack && itemFlying && distFt < 24;
}

/// <summary>The button starts the swing; the contact mark is what meets the pitch.
/// Timing errors remain in the resolver's 60 Hz frames, independent of render rate.</summary>
public static class AtBatMotion
{
    // Finish blending the held load halfway to the event. Release/contact itself
    // must sample the clip exactly, without frame-dependent recursive smoothing.
    public static MoveBones.Sample FromLoad(MoveBones.Sample load, MoveBones.Sample motion,
        double poseTime, double eventAt)
    {
        if (poseTime <= 0) return load;
        if (poseTime >= eventAt * 0.5) return motion;
        var u = Math.Clamp(poseTime / (eventAt * 0.5), 0, 1);
        return MoveBones.Mix(load, motion, u * u * (3 - 2 * u));
    }

    public static double SwingErrorFrames(double pressAt, double plateAt, bool bunt = false) =>
        (pressAt + (bunt ? 0 : MoveBones.SwingContact) - plateAt) * 60;

    public static double SwingStart(double plateAt, double errorFrames, bool bunt = false) =>
        plateAt + errorFrames / 60 - (bunt ? 0 : MoveBones.SwingContact);
}
