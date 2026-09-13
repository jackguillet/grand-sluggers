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

    /// <summary>A release inside the first <c>pitching.release.niceBandSec</c> of MAX is Nice! (spec §4.1).</summary>
    public static bool NiceRelease(double fill01, double secondsPastFull, double maxHold, RulesTable? rules = null) =>
        AtMax(fill01, secondsPastFull, maxHold) && secondsPastFull <= Rules.Or(rules).Pitching.Release.NiceBandSec;

    public static bool IsSlap(double effective01) => effective01 < SlapBelow;

    public static bool IsCharge(double effective01) => effective01 >= ChargeAt;

    /// <summary>
    /// The release tell (spec §4.1, §5.1; #578): MAX inside the band says the charge landed and
    /// nothing else. "Nice!" is the booklet's word for the pitch release; the swing shows MAX.
    /// Words about the contact come only from the typed zone (<see cref="PlayStamp.ContactTell"/>).
    /// </summary>
    public static string NiceCopy(bool pitching, double fill01, double secondsPastFull, double maxHold) =>
        AtMax(fill01, secondsPastFull, maxHold)
            ? (pitching ? "Nice!" : "MAX")
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

    public SwingCommand Resolve(double releaseAt, double plateAt, double effectiveCharge, bool star,
        RulesTable? rules = null) =>
        new(
            true,
            effectiveCharge,
            AtBatMotion.SwingErrorFrames(releaseAt, plateAt, Bunt, rules),
            star,
            SprayAimDeg,
            Bunt,
            LaunchAim,
            BoxOffsetX,
            Human: true);
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
        bool accepting = true,
        bool commits = true)
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

        // A release that cannot commit (a press during SET, spec §3) disarms and is not a swing.
        if (armed && released)
            return commits ? new ChargeButtonStep(default, true, fill, past) : default;

        var nextState = armed
            ? new ChargeButtonState(true, fill, past)
            : default;
        return new ChargeButtonStep(nextState, false, 0, 0);
    }
}

/// <summary>
/// The cursor (spec §5.2, D4): the bat drawn on the plate plane in world feet. It follows the
/// batter (box walk moves it the same distance as the body, <see cref="HomeSet.BatterWalk"/>),
/// never the pitch; it is as tall as the zone so any strike is hittable; along the barrel it is
/// asymmetric (tip side long, handle side short). Five zones: sour / nice / perfect / nice / sour.
/// The drawn oval is the nice boundary; the perfect heart and the sour rim are fractions of it.
/// </summary>
public static class SweetSpot
{
    /// <summary>World X direction of the bat tip: away from the body (batting.cursor).</summary>
    public static double TipSign(Hand bats) => bats == Hand.L ? -1 : 1;

    /// <summary>Center of the cursor in world feet: the box walk in X, the zone center in Y.</summary>
    public static (double X, double Y) WorldCenter(double boxOffsetX) =>
        (boxOffsetX * HomeSet.BatterWalk, StrikeZoneGeometry.CenterY);

    /// <summary>Half the zone height: the nice half-axis up and down. Never scaled, so every strike stays hittable.</summary>
    public static double HalfHeightFt => StrikeZoneGeometry.Height / 2;

    /// <summary>Bat (contact) scales the barrel around 5.</summary>
    public static double ContactScale(int contact, RulesTable? rules = null) =>
        Math.Max(0.5, 1 + (Math.Clamp(contact, 1, 10) - 5) * Rules.Or(rules).Batting.Cursor.ScalePerContact);

    /// <summary>Good-chemistry runners widen a slap's zones (batting.buddiesOnBase.widen*).</summary>
    public static double BuddyWiden(int buddies, RulesTable? rules = null)
    {
        var b = Rules.Or(rules).Batting.BuddiesOnBase;
        return buddies switch
        {
            >= 3 => b.WidenThree,
            2 => b.WidenTwo,
            1 => b.WidenOne,
            _ => 1.0
        };
    }

    /// <summary>
    /// The barrel scale for one swing: contact × (charge narrows | buddies widen a slap).
    /// The Charge Bat is a MAX charge with the narrowing off (spec §5.5).
    /// </summary>
    public static double BarrelScale(int contact, bool charged, bool chargeBat, int buddies, RulesTable? rules = null)
    {
        var c = Rules.Or(rules).Batting.Cursor;
        var scale = ContactScale(contact, rules);
        if (charged) return chargeBat ? scale : scale * c.ChargeMul;
        return scale * BuddyWiden(buddies, rules);
    }

    /// <summary>Nice half-axis along the barrel on the side of <paramref name="dx"/> (world feet from the center).</summary>
    public static double NiceHalfWidthFt(Hand bats, double dx, double barrelScale, RulesTable? rules = null)
    {
        var c = Rules.Or(rules).Batting.Cursor;
        var towardTip = dx * TipSign(bats) >= 0;
        return (towardTip ? c.NiceTipFt : c.NiceHandleFt) * barrelScale;
    }

    /// <summary>
    /// Normalized distance of a crossing from the cursor center: 1 on the drawn (nice) boundary.
    /// </summary>
    public static double Distance(double boxOffsetX, Hand bats, double crossingX, double crossingY,
        double barrelScale = 1, RulesTable? rules = null)
    {
        var (cx, cy) = WorldCenter(boxOffsetX);
        var dx = crossingX - cx;
        var dy = crossingY - cy;
        var nx = NiceHalfWidthFt(bats, dx, barrelScale, rules);
        var ny = HalfHeightFt;
        return Math.Sqrt(dx * dx / (nx * nx) + dy * dy / (ny * ny));
    }

    /// <summary>
    /// Where the crossing meets the bat. Cursor decides quality (D4): the perfect heart and the
    /// nice oval are the drawn ellipse (batting.cursor.perfectFraction); the sour rim is the
    /// barrel's rectangle <see cref="CursorRules.RimFraction"/> beyond it, so the corners of the
    /// zone are on the bat with the box centered.
    /// </summary>
    public static ContactQuality Zone(double boxOffsetX, Hand bats, double crossingX, double crossingY,
        double barrelScale = 1, RulesTable? rules = null)
    {
        var c = Rules.Or(rules).Batting.Cursor;
        var (cx, cy) = WorldCenter(boxOffsetX);
        var dx = crossingX - cx;
        var dy = crossingY - cy;
        if (!double.IsFinite(dx) || !double.IsFinite(dy)) return ContactQuality.Miss;
        var nx = NiceHalfWidthFt(bats, dx, barrelScale, rules);
        var ny = HalfHeightFt;
        var d = Math.Sqrt(dx * dx / (nx * nx) + dy * dy / (ny * ny));
        if (d <= c.PerfectFraction) return ContactQuality.Perfect;
        if (d <= 1) return ContactQuality.Nice;
        if (Math.Abs(dx) <= nx * (1 + c.RimFraction) && Math.Abs(dy) <= ny * (1 + c.RimFraction))
            return ContactQuality.Sour;
        return ContactQuality.Miss;
    }

    /// <summary>
    /// The drawn oval: the nice boundary, local to the cursor center, in world feet. The client
    /// draws exactly the hitbox the sim judges; the tip half is longer than the handle half.
    /// </summary>
    public static IReadOnlyList<(double X, double Y)> Outline(Hand bats, double barrelScale = 1,
        int segments = 40, RulesTable? rules = null)
    {
        var pts = new List<(double X, double Y)>(segments);
        for (var i = 0; i < segments; i++)
        {
            var a = i * Math.PI * 2 / segments;
            var ux = Math.Cos(a);
            var nx = NiceHalfWidthFt(bats, ux, barrelScale, rules);
            pts.Add((ux * nx, Math.Sin(a) * HalfHeightFt));
        }
        return pts;
    }

    /// <summary>Every strike is hittable with the box centered: no zone corner is off the bat (spec §5.2).</summary>
    public static bool CoversTheZone(Hand bats, double barrelScale = 1, RulesTable? rules = null)
    {
        foreach (var x in new[] { -StrikeZoneGeometry.HalfWidth, 0, StrikeZoneGeometry.HalfWidth })
        foreach (var y in new[] { StrikeZoneGeometry.Bottom, StrikeZoneGeometry.CenterY, StrikeZoneGeometry.Top })
            if (Zone(0, bats, x, y, barrelScale, rules) == ContactQuality.Miss)
                return false;
        return true;
    }
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

/// <summary>The button starts the swing; the ball at the plate is what the press is judged against
/// (D13, #612). Inside the window the take is warped so its Contact mark meets the ball; outside it
/// plays at its own length and misses. Timing errors remain in the resolver's 60 Hz frames,
/// independent of render rate.</summary>
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

    /// <summary>
    /// The swing take's clip time <paramref name="poseTime"/> seconds after the press (D13, #612).
    /// <paramref name="contactSec"/> is when the take's Contact mark lands after the press
    /// (<see cref="SwingContactSec"/>): the ball's plate time inside the window, the take's own
    /// <see cref="Motion.SwingContact"/> outside it. Load → contact is warped onto that span; the
    /// follow-through and finish play at the take's own speed. Monotonic for every span, so the
    /// authored keys keep their order and the take never plays backward.
    /// </summary>
    public static double SwingClipTime(double poseTime, double charge01, double contactSec = Motion.SwingContact)
    {
        if (poseTime >= contactSec)
            return Motion.SwingContact + (poseTime - Math.Max(0, contactSec));
        var u = contactSec <= 0 ? 0 : Math.Max(0, poseTime) / contactSec;
        return LoadedClipTime(u * Motion.SwingContact, SwingPresentation.LoadSampleAt(charge01), Motion.SwingContact);
    }

    /// <summary>
    /// Seconds from the press to the take's Contact mark (D13): inside the window the ball's plate
    /// time (never before the press), outside it the take's own mark, so the bat misses honestly.
    /// </summary>
    public static double SwingContactSec(double errorFrames, double windowFrames, RulesTable? rules = null) =>
        AtBatResolver.InWindow(errorFrames, windowFrames)
            ? Math.Max(0, Rules.Or(rules).Batting.Window.LeadSec - errorFrames / 60)
            : Motion.SwingContact;

    /// <summary>Real seconds the committed take lasts after the press: the warped span to contact plus the authored follow-through.</summary>
    public static double SwingTakeSeconds(double contactSec) =>
        Math.Max(0, contactSec) + (Motion.SwingDur - Motion.SwingContact);

    public static double PitchClipTime(double poseTime, double charge01) =>
        LoadedClipTime(poseTime, Motion.PitchLoadSampleAt(charge01), Motion.PitchRelease);

    /// <summary>
    /// The press against the square press, the ball's plate time less batting.window.leadSec (D13),
    /// in 60 Hz frames: negative is early. A bunt is already on the plane, so it has no lead (§5.8).
    /// </summary>
    public static double SwingErrorFrames(double pressAt, double plateAt, bool bunt = false, RulesTable? rules = null) =>
        (pressAt - SquarePressAt(plateAt, bunt, rules)) * 60;

    /// <summary>The press that is square: the ball's plate time less the authored lead (none for a bunt).</summary>
    public static double SquarePressAt(double plateAt, bool bunt = false, RulesTable? rules = null) =>
        plateAt - (bunt ? 0 : Rules.Or(rules).Batting.Window.LeadSec);

    /// <summary>
    /// When the CPU batter commits (spec §3, §5.9): the square press less batting.cpu.decideLeadSec,
    /// from the trajectory as it stands then. A human who has already pressed is in the same
    /// position: the judgment still reads the final crossing.
    /// </summary>
    public static double CpuDecisionTime(double plateAt, RulesTable? rules = null) =>
        SquarePressAt(plateAt, rules: rules) - Rules.Or(rules).Batting.Cpu.DecideLeadSec;

    /// <summary>A CPU swing cannot start before the decision: the judged error is clamped to what the bat can show.</summary>
    public static SwingCommand CommitCpuSwing(SwingCommand swing, double plateAt, RulesTable? rules = null)
    {
        if (!swing.Swing) return swing;
        var earliest = SwingErrorFrames(CpuDecisionTime(plateAt, rules), plateAt, swing.Bunt, rules);
        return swing.TimingErrorFrames < earliest ? swing with { TimingErrorFrames = earliest } : swing;
    }

    /// <summary>The press, recovered from its error: the square press plus the error.</summary>
    public static double SwingStart(double plateAt, double errorFrames, bool bunt = false, RulesTable? rules = null) =>
        SquarePressAt(plateAt, bunt, rules) + errorFrames / 60;

    /// <summary>
    /// Advance one committed action clock, in real seconds after the press. The
    /// flight-derived target keeps the warped contact mark tied to pitch timing;
    /// after the pitch resolves, frame time carries the same action through its
    /// follow-through. Landing exactly on <paramref name="takeSec"/>
    /// (<see cref="SwingTakeSeconds"/>) presents the final key once before the clock retires.
    /// </summary>
    public static double AdvanceCommittedSwing(
        double current, double flightTime, double swingStart, double dt, double takeSec = Motion.SwingDur)
    {
        var target = Math.Max(0, flightTime - swingStart);
        if (current < 0)
            return Math.Min(target, takeSec);
        if (current >= takeSec)
            return current + Math.Max(0, dt);
        return Math.Min(takeSec,
            Math.Max(target, current + Math.Max(0, dt)));
    }

    public static bool PresentsCommittedSwing(double actionTime, double takeSec = Motion.SwingDur) =>
        actionTime >= 0 && actionTime <= takeSec;

    public static double CommittedSwingSample(double actionTime, double takeSec = Motion.SwingDur) =>
        Math.Clamp(actionTime, 0, takeSec);
}
