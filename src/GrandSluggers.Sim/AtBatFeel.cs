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

public readonly record struct ChargeButtonState(
    bool Armed,
    double Fill01,
    double SecondsPastFull);

public readonly record struct ChargeButtonStep(
    ChargeButtonState Next,
    bool Committed,
    double CommitFill01,
    double CommitSecondsPastFull);

/// <summary>
/// Super Sluggers' button load: press starts the windup, holding fills it,
/// and releasing commits either a quick normal action or the stored charge.
/// </summary>
public static class ChargeButton
{
    public static ChargeButtonStep Advance(
        ChargeButtonState state,
        bool pressed,
        bool held,
        bool released,
        double deltaSeconds,
        double secondsToFull,
        bool accepting = true)
    {
        if (!accepting)
            return default;

        var armed = state.Armed || pressed;
        var fill = Math.Clamp(state.Fill01, 0, 1);
        var past = Math.Max(0, state.SecondsPastFull);
        if (armed && held)
        {
            var next = Math.Min(1, fill + Math.Max(0, deltaSeconds) / Math.Max(0.01, secondsToFull));
            if (next >= 1 && fill >= 1) past += Math.Max(0, deltaSeconds);
            else if (next >= 1) past = 0;
            fill = next;
        }

        if (armed && released)
            return new ChargeButtonStep(default, true, fill, past);

        var nextState = armed
            ? new ChargeButtonState(true, fill, past)
            : default;
        return new ChargeButtonStep(nextState, false, 0, 0);
    }
}

/// <summary>
/// Sweet-spot oval at the plate, smaller than the zone. Center follows the batter.
/// </summary>
public static class SweetSpot
{
    public const double HalfWidth = 0.32;
    public const double HalfHeight = 0.28;

    public const double WorldHalfWidth = HalfWidth * PitchFlight.PlateScaleX;
    public const double WorldHalfHeight = HalfHeight * PitchFlight.PlateScaleY;

    public static (double X, double Y) WorldCenter(double boxOffsetX) =>
        PitchFlight.PlateTarget(boxOffsetX, 0);

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
        var u = LoadBlend01(poseTime, eventAt);
        if (u <= 0) return load;
        if (u >= 1) return motion;
        return MoveBones.Mix(load, motion, u);
    }

    public static double LoadBlend01(double poseTime, double eventAt)
    {
        var u = Math.Clamp(poseTime / (eventAt * 0.5), 0, 1);
        return u * u * (3 - 2 * u);
    }

    /// <summary>
    /// Continue forward from the held authored load and meet the canonical take
    /// by halfway to contact. This stays monotonic for the 0.075-second normal
    /// load offset and preserves the exact 0.30-second contact sample.
    /// </summary>
    public static double SwingClipTime(double poseTime, double charge01)
    {
        var loadAt = SwingPresentation.LoadSampleAt(charge01);
        return poseTime + loadAt * (1 - LoadBlend01(poseTime, MoveBones.SwingContact));
    }

    public static double SwingErrorFrames(double pressAt, double plateAt, bool bunt = false) =>
        (pressAt + (bunt ? 0 : MoveBones.SwingContact) - plateAt) * 60;

    public static double SwingStart(double plateAt, double errorFrames, bool bunt = false) =>
        plateAt + errorFrames / 60 - (bunt ? 0 : MoveBones.SwingContact);
}
