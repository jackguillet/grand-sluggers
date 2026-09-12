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
/// One release-edge swing, captured before a presentation phase can change.
/// SET and Flight both resolve this same immutable intent exactly once.
/// </summary>
public readonly record struct SwingInputIntent(
    bool Committed,
    double Fill01,
    double SecondsPastFull,
    double SprayAimDeg,
    bool Bunt,
    double LaunchAim,
    double BoxOffsetX)
{
    public static SwingInputIntent Capture(
        ChargeButtonStep button,
        double stickX,
        double stickY,
        bool bunt,
        double boxOffsetX) =>
        button.Committed
            ? new SwingInputIntent(
                true,
                button.CommitFill01,
                button.CommitSecondsPastFull,
                AtBatResolver.SprayAimDeg(stickX),
                bunt,
                stickY,
                boxOffsetX)
            : default;

    public SwingCommand Resolve(double releaseAt, double plateAt, double effectiveCharge, bool star) =>
        new(
            true,
            effectiveCharge,
            AtBatMotion.SwingErrorFrames(releaseAt, plateAt, Bunt),
            star,
            SprayAimDeg,
            Bunt,
            LaunchAim,
            BoxOffsetX);
}

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
    /// <summary>Oval half sizes in plate-aim units (batting.oval).</summary>
    public static double HalfWidth(RulesTable? rules = null) => Rules.Or(rules).Batting.Oval.HalfWidth;
    public static double HalfHeight(RulesTable? rules = null) => Rules.Or(rules).Batting.Oval.HalfHeight;

    public static double WorldHalfWidth(RulesTable? rules = null) => HalfWidth(rules) * PitchFlight.PlateScaleX;
    public static double WorldHalfHeight(RulesTable? rules = null) => HalfHeight(rules) * PitchFlight.PlateScaleY;

    public static (double X, double Y) WorldCenter(double boxOffsetX) =>
        PitchFlight.PlateTarget(boxOffsetX, 0);

    public static double Overlap(double boxOffsetX, double pitchAimX, double pitchAimY, RulesTable? rules = null)
    {
        var oval = Rules.Or(rules).Batting.Oval;
        var dx = (pitchAimX - boxOffsetX) / oval.HalfWidth;
        var dy = pitchAimY / oval.HalfHeight;
        var d2 = dx * dx + dy * dy;
        if (d2 <= 1) return 1;
        if (d2 <= oval.EdgeD2) return oval.EdgeOverlap;
        return 0;
    }

    public static bool CenterEatsHeart(RulesTable? rules = null) => Overlap(0, 0, 0, rules) >= 1;

    public static bool WalkedOffMissesHeart(double walk = 0.85, RulesTable? rules = null) =>
        Overlap(walk, 0, 0, rules) <= 0;
}

/// <summary>Fielding dash and buddy-toss before the glove (fielding.dash).</summary>
public static class FieldDash
{
    public static double ChaseMul(RulesTable? rules = null) => Rules.Or(rules).Fielding.Dash.ChaseMul;

    public static bool BuddyTossOffered(Chemistry rel, double distFt, RulesTable? rules = null) =>
        rel == Chemistry.Good && distFt < Rules.Or(rules).Fielding.Dash.BuddyTossFt;

    public static FieldingResult ApplyBuddyToss(FieldingResult field, Character partner, ThrowResult thr) =>
        field with { Fielder = partner, Throw = thr };

    public static bool KickOffered(double distFt, RulesTable? rules = null) =>
        distFt < Rules.Or(rules).Fielding.Dash.KickFt;

    /// <summary>Dive carries the body toward the ball. Not a teleport.</summary>
    public static (double X, double Z) Lunge(double x, double z, double tx, double tz, double? ft = null, RulesTable? rules = null)
    {
        var reach = ft ?? Rules.Or(rules).Fielding.Dash.DiveLungeFt;
        var dx = tx - x;
        var dz = tz - z;
        var d = Math.Sqrt(dx * dx + dz * dz);
        if (d < 0.01) return (x, z);
        var u = Math.Min(1, reach / d);
        return (x + dx * u, z + dz * u);
    }

    public static bool DestroysItem(bool attack, bool itemFlying, double distFt, RulesTable? rules = null) =>
        attack && itemFlying && distFt < Rules.Or(rules).Fielding.Dash.ItemSmashFt;
}

/// <summary>The button starts the swing; the contact mark is what meets the pitch.
/// Timing errors remain in the resolver's 60 Hz frames, independent of render rate.</summary>
public static class AtBatMotion
{
    /// <summary>Sentinel before a committed swing has entered presentation.</summary>
    public const double SwingNotStarted = -1;

    /// <summary>
    /// Blend a held load into its committed take: finished halfway to the
    /// event so release and contact sample the take exactly, with no
    /// frame-dependent smoothing.
    /// </summary>
    public static double LoadBlend01(double poseTime, double eventAt)
    {
        var u = Math.Clamp(poseTime / (eventAt * 0.5), 0, 1);
        return u * u * (3 - 2 * u);
    }

    /// <summary>
    /// Continue forward from the held load sample and meet the take's own
    /// clock by halfway to the event. Monotonic for every load offset and
    /// exact at <paramref name="eventAt"/>. One rule for pitch and swing.
    /// </summary>
    public static double LoadedClipTime(double poseTime, double loadAt, double eventAt) =>
        poseTime + loadAt * (1 - LoadBlend01(poseTime, eventAt));

    public static double SwingClipTime(double poseTime, double charge01) =>
        LoadedClipTime(poseTime, SwingPresentation.LoadSampleAt(charge01), Motion.SwingContact);

    public static double PitchClipTime(double poseTime, double charge01) =>
        LoadedClipTime(poseTime, Motion.PitchLoadSampleAt(charge01), Motion.PitchRelease);

    public static double SwingErrorFrames(double pressAt, double plateAt, bool bunt = false) =>
        (pressAt + (bunt ? 0 : Motion.SwingContact) - plateAt) * 60;

    public static double SwingStart(double plateAt, double errorFrames, bool bunt = false) =>
        plateAt + errorFrames / 60 - (bunt ? 0 : Motion.SwingContact);

    /// <summary>
    /// Advance one committed action clock. The flight-derived target keeps the
    /// authored contact mark tied to pitch timing; after the pitch resolves,
    /// frame time carries the same action through its follow-through. Landing
    /// exactly on SwingDur presents the final key once before the clock retires.
    /// </summary>
    public static double AdvanceCommittedSwing(
        double current, double flightTime, double swingStart, double dt)
    {
        var target = Math.Max(0, flightTime - swingStart);
        if (current < 0)
            return Math.Min(target, Motion.SwingDur);
        if (current >= Motion.SwingDur)
            return current + Math.Max(0, dt);
        return Math.Min(Motion.SwingDur,
            Math.Max(target, current + Math.Max(0, dt)));
    }

    public static bool PresentsCommittedSwing(double actionTime) =>
        actionTime >= 0 && actionTime <= Motion.SwingDur;

    public static double CommittedSwingSample(double actionTime) =>
        Math.Clamp(actionTime, 0, Motion.SwingDur);
}
