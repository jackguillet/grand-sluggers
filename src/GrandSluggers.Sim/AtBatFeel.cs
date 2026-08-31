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

    public static bool KickOffered(double distFt) => distFt < KickFt;

    public static bool DestroysItem(bool attack, bool itemFlying, double distFt) =>
        attack && itemFlying && distFt < 24;
}
