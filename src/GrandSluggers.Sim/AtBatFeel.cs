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
/// One Flight release-edge swing, captured before presentation can change.
/// SET may build charge state, but its release does not create an intent (spec §3).
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
/// Which ordinary pitch the mound has selected, and whether the charge has locked it
/// (spec §3, PH-02-R3/R4/R5). <see cref="Slot"/> indexes the pitcher's <see cref="Repertoire"/>:
/// 0 is the fastball every pitcher throws, 1 the second pitch, 2 the third.
///
/// <c>default</c> is slot 0 (fastball), unlocked — the state every SET starts in.
///
/// <b>A lock exists only while the charge button is armed</b>: <see cref="PitchSelection.Advance"/>
/// reads a <see cref="Locked"/> state whose button is not armed as stale and clears it, so a caller
/// that skips the tick the charge went away cannot strand the selection.
/// </summary>
public readonly record struct PitchSelectionState(int Slot, bool Locked)
{
    /// <summary>
    /// Fastball, unlocked (PH-02-R5). The selection returns here at <b>every</b> SET entry — the
    /// first pitch of an at-bat, the SET after a pitch, after a dead pickoff, after a foul — and
    /// after a pitcher swap, because nothing on the shared screen marks the active family and the
    /// player counts presses from a known start. <see cref="PitchSelection.Advance"/> also resets
    /// itself on the commit tick, so a caller that forgets is still on the fastball next SET.
    /// </summary>
    public static PitchSelectionState Reset => default;
}

/// <summary>
/// One tick of the selection (<see cref="PitchSelection.Advance"/>).
/// </summary>
/// <param name="Next">The state to carry into the next tick; <see cref="PitchSelectionState.Reset"/> on the commit tick.</param>
/// <param name="Family">
/// The family id <see cref="Next"/> names, already resolved against the repertoire so a caller never
/// indexes it — except on the commit tick, where it is the <b>locked</b> family of the delivery
/// being committed (<see cref="Next"/> has already reset to the fastball for the next SET).
/// </param>
/// <param name="LockedThisTick">The charge armed on this tick and took the family with it.</param>
/// <param name="Committed">The delivery left the hand on this tick; <see cref="Family"/> is what it throws.</param>
public readonly record struct PitchSelectionStep(
    PitchSelectionState Next,
    string Family,
    bool LockedThisTick,
    bool Committed);

/// <summary>
/// The pre-charge pitch selection (spec §3, §4.3; PH-02-R3/R4/R5), beside
/// <see cref="ChargeButton"/> because it is the same SET tick: one button cycles the pitcher's three
/// ordinary families, the charge locks the one that is showing, and the commit resets to the fastball.
///
/// Pure: no allocation, no RNG, no clock, no <c>Rules.Default</c>. The caller owns the state and the
/// table (<see cref="Match.SelectPitch"/> passes the current pitcher and the match's own rules).
///
/// The rules, in the order one tick applies them:
/// <list type="number">
/// <item><b>Cycle.</b> Unlocked, <c>selectable</c>, and a cycle press advances to the next
/// <i>selectable</i> slot, wrapping 2 → 0. One press is one advance, never two.</item>
/// <item><b>Lock.</b> The tick the charge button arms — including the same-tick press-and-release
/// that commits at once — takes the slot <i>as it stands after the cycle</i>. Cycle, then lock: a
/// press on the arming tick is never dropped.</item>
/// <item><b>Commit.</b> The committed family is the locked one; nothing is re-polled at release.
/// The state resets to fastball, unlocked.</item>
/// <item><b>Disarm without a commit.</b> The charge went away without a delivery (the swap pick
/// opening mid-hold makes the button non-accepting and discards the charge, §4.7): the lock
/// releases and the slot is kept. This child adds no pitcher cancel (PH-02-R3 leaves it
/// unselected); it only makes the edge that already exists deterministic.</item>
/// </list>
///
/// The lock is held by an <b>invariant</b>, not only by that edge: a lock exists only while the
/// charge button is armed. A caller that skips the tick the charge went away — an early return in
/// SET for the swap pick, a pause, a tutorial gate — would otherwise strand the selection locked
/// with a disarmed button, with no later tick able to see the releasing edge and every cycle press
/// ignored until someone reset it. A locked state whose button is not armed is read as unlocked,
/// so the next press cycles and the next charge locks afresh.
///
/// A slot is <b>selectable</b> when its family has an authored row in the active table
/// (<see cref="PitchFamilyTable.IsAuthored"/>, #810): an unauthored family is skipped, so zero, one
/// or two presses always land on a pitch that can fly. <c>selectable</c> the parameter is the
/// seat's gate — "in SET and the swap pick is closed", not "accepting": cycling before the
/// pitcher-ready beat is allowed, because a selection is not a delivery.
/// </summary>
public static class PitchSelection
{
    /// <summary>
    /// One SET tick of the selection, against the active family table.
    /// </summary>
    /// <param name="state">The selection as the previous tick left it.</param>
    /// <param name="cyclePressed">The cycle button went down on this tick (<c>Controls.CyclePitch</c>).</param>
    /// <param name="selectable">The seat may cycle: in SET with the swap pick closed (§4.7).</param>
    /// <param name="prevButton">The charge button <i>before</i> <paramref name="buttonStep"/> ran: the arm edge is read from it.</param>
    /// <param name="buttonStep">This tick's <see cref="ChargeButton.Advance"/> result.</param>
    /// <param name="repertoire">The pitcher on the mound right now (§2, PH-15-R1).</param>
    /// <param name="families">The match's own table, never <see cref="Rules.Default"/>.</param>
    public static PitchSelectionStep Advance(
        PitchSelectionState state,
        bool cyclePressed,
        bool selectable,
        ChargeButtonState prevButton,
        ChargeButtonStep buttonStep,
        Repertoire repertoire,
        PitchFamilyTable families) =>
        Advance(state, cyclePressed, selectable, prevButton, buttonStep, repertoire,
            (families ?? throw new ArgumentNullException(nameof(families))).Authored);

    /// <summary>
    /// The same tick against an explicit list of authored family ids, so a rules table that authors
    /// more of the library than the shipped one does (P1-d) cycles through all of it without this
    /// step knowing a number. <see cref="PitchFamilyTable.Authored"/> is the shipped list.
    /// </summary>
    public static PitchSelectionStep Advance(
        PitchSelectionState state,
        bool cyclePressed,
        bool selectable,
        ChargeButtonState prevButton,
        ChargeButtonStep buttonStep,
        Repertoire repertoire,
        IReadOnlyList<string> authored)
    {
        if (repertoire is null) throw new ArgumentNullException(nameof(repertoire));
        if (authored is null) throw new ArgumentNullException(nameof(authored));
        RequireFastball(repertoire, authored);

        var slot = Sound(state.Slot, repertoire, authored);

        // The invariant: a lock exists only while the charge button is armed. A locked state with a
        // disarmed button is a releasing edge the caller skipped (an early return in SET), not a
        // charge; reading it as unlocked keeps the selection from stranding until the next Reset.
        var locked = state.Locked && prevButton.Armed;

        // (a) Cycle, before the lock, so a press on the arming tick is never dropped.
        if (!locked && selectable && cyclePressed)
            slot = NextSelectable(slot, repertoire, authored);

        // (b) The arm edge: the charge takes the family as it stands now. A press-and-release on one
        // tick commits without ever reporting Armed, so the commit is an arm edge too. An arm edge
        // needs a disarmed button, and the invariant above means a disarmed button is never locked,
        // so an edge always takes a fresh lock — including the one after a skipped release.
        var armedNow = !prevButton.Armed && (buttonStep.Next.Armed || buttonStep.Committed);
        var lockedThisTick = armedNow;
        if (armedNow) locked = true;

        // (c) The delivery left: throw the locked family and start the next SET on the fastball.
        if (buttonStep.Committed)
            return new PitchSelectionStep(
                PitchSelectionState.Reset, repertoire[slot], lockedThisTick, true);

        // (d) The charge went away with no delivery: release the lock, keep the slot.
        if (prevButton.Armed && !buttonStep.Next.Armed)
            locked = false;

        return new PitchSelectionStep(
            new PitchSelectionState(slot, locked), repertoire[slot], lockedThisTick, false);
    }

    /// <summary>
    /// The family id a state names, resolved against this pitcher and this table. Same defensive
    /// reading as <see cref="Advance"/>: a slot this repertoire and table cannot throw reads as the
    /// fastball rather than as a pitch that cannot fly.
    /// </summary>
    public static string FamilyAt(PitchSelectionState state, Repertoire repertoire, PitchFamilyTable families) =>
        FamilyAt(state, repertoire, (families ?? throw new ArgumentNullException(nameof(families))).Authored);

    /// <inheritdoc cref="FamilyAt(PitchSelectionState, Repertoire, PitchFamilyTable)"/>
    public static string FamilyAt(PitchSelectionState state, Repertoire repertoire, IReadOnlyList<string> authored)
    {
        if (repertoire is null) throw new ArgumentNullException(nameof(repertoire));
        if (authored is null) throw new ArgumentNullException(nameof(authored));
        RequireFastball(repertoire, authored);
        return repertoire[Sound(state.Slot, repertoire, authored)];
    }

    /// <summary>True when this pitcher's slot has an authored row: the slot a press may land on.</summary>
    public static bool IsSelectable(int slot, Repertoire repertoire, IReadOnlyList<string> authored) =>
        slot >= 0 && slot < Repertoire.Slots && Contains(authored, repertoire[slot]);

    /// <summary>
    /// A slot that can be read. A stored slot outside 0..2, or one whose family the active table no
    /// longer authors — a pitcher swapped without a <see cref="PitchSelectionState.Reset"/>, a
    /// trial overlay that drops a row — is the fastball, which every pitcher throws (PH-15-R1).
    /// Silent because there is no honest alternative mid-tick: the slot is stale input, not a broken
    /// table, and the fastball is the reset the next SET would have applied anyway.
    /// </summary>
    static int Sound(int slot, Repertoire repertoire, IReadOnlyList<string> authored) =>
        IsSelectable(slot, repertoire, authored) ? slot : 0;

    /// <summary>
    /// One press: the next selectable slot, wrapping 2 → 0 (PH-02-R4/R5). Tries +1 then +2 and stops
    /// at the first one that can fly, so a pitcher whose second and third families are unauthored
    /// stays on the fastball instead of cycling through pitches that would stop the delivery.
    /// </summary>
    static int NextSelectable(int slot, Repertoire repertoire, IReadOnlyList<string> authored)
    {
        for (var step = 1; step < Repertoire.Slots; step++)
        {
            var candidate = (slot + step) % Repertoire.Slots;
            if (Contains(authored, repertoire[candidate])) return candidate;
        }
        return 0;
    }

    /// <summary>
    /// The fastball is the one family every pitcher throws (PH-15-R1) and the slot every reset lands
    /// on, so a table that does not author it has no selection at all. That is a broken table, not a
    /// press: it stops by name instead of resolving to something that cannot fly.
    /// </summary>
    static void RequireFastball(Repertoire repertoire, IReadOnlyList<string> authored)
    {
        if (Contains(authored, repertoire[0])) return;
        throw new InvalidOperationException(
            $"the pitch family table authors no '{PitchFamily.Fastball}' row, so slot 0 — the family every "
            + "pitcher throws and every SET resets to (PH-15-R1, PH-02-R5) — cannot be selected. Authored: "
            + string.Join(", ", authored));
    }

    /// <summary>Ordinal membership over the ≤5 library ids, by hand: the hot path allocates nothing.</summary>
    static bool Contains(IReadOnlyList<string> authored, string family)
    {
        for (var i = 0; i < authored.Count; i++)
            if (string.Equals(authored[i], family, StringComparison.Ordinal))
                return true;
        return false;
    }
}

/// <summary>
/// One swing's cursor oval in world feet on the plate plane (spec §5.2): what the client draws and
/// what the resolver judges (<see cref="SweetSpot.Oval"/>). The nice boundary is the ellipse with
/// <see cref="TipHalfFt"/> toward the bat tip, <see cref="HandleHalfFt"/> toward the handle and
/// <see cref="HalfHeightFt"/> up and down, around (<see cref="CenterX"/>, <see cref="CenterY"/>).
/// </summary>
public readonly record struct CursorOval(
    Hand Bats,
    double CenterX,
    double CenterY,
    double TipHalfFt,
    double HandleHalfFt,
    double HalfHeightFt,
    double BarrelScale);

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

    /// <summary>
    /// The barrel scale of one swing from what the plate knows (spec §5.2): the hitter's Contact
    /// plus the bat's <c>contactMod</c>, clamped 1–10 (PH-15-R7); the charge as it stands, where the
    /// Charge Bat is a MAX charge (§5.5); the good-chemistry runners on base. The resolver judges
    /// with this and <see cref="Oval"/> draws with it, so the two cannot drift (P2-d, #889).
    /// </summary>
    /// <param name="charge01">The effective charge (after overcharge decay), 0–1.</param>
    public static double SwingBarrel(Character batter, BatItem? bat, double charge01, int buddies,
        RulesTable? rules = null)
    {
        var contact = Math.Clamp(batter.Stats.Contact + (bat?.ContactMod ?? 0), 1, 10);
        var chargeBat = bat?.ChargeAlwaysFull == true;
        var charged = ChargeFeel.IsCharge(chargeBat ? 1.0 : Math.Clamp(charge01, 0, 1));
        return BarrelScale(contact, charged, chargeBat, buddies, rules);
    }

    /// <summary>
    /// The oval the client draws for one swing, which is the nice boundary the resolver judges
    /// (spec §5.2, S-133): the center from the box walk, the half-extents from
    /// <see cref="SwingBarrel"/>. A charge and Contact move the two barrel half-extents only; the
    /// height is the zone's and never scales (PH-11-R1, PH-15-R7, S-134).
    /// </summary>
    public static CursorOval Oval(Character batter, BatItem? bat, double charge01, int buddies,
        double boxOffsetX, RulesTable? rules = null)
    {
        var scale = SwingBarrel(batter, bat, charge01, buddies, rules);
        var bats = batter.Bats;
        var (x, y) = WorldCenter(boxOffsetX);
        var tip = TipSign(bats);
        return new CursorOval(bats, x, y,
            NiceHalfWidthFt(bats, tip, scale, rules),
            NiceHalfWidthFt(bats, -tip, scale, rules),
            HalfHeightFt, scale);
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

    /// <summary>
    /// The drawn outline of <paramref name="oval"/>, local to its center: the tip half-extent on the
    /// tip side, the handle half-extent on the other, the zone's half height up and down. The same
    /// points as <see cref="Outline(Hand, double, int, RulesTable?)"/> for the oval's own barrel.
    /// </summary>
    public static IReadOnlyList<(double X, double Y)> Outline(CursorOval oval, int segments = 40)
    {
        var pts = new List<(double X, double Y)>(segments);
        var tip = TipSign(oval.Bats);
        for (var i = 0; i < segments; i++)
        {
            var a = i * Math.PI * 2 / segments;
            var ux = Math.Cos(a);
            var nx = ux * tip >= 0 ? oval.TipHalfFt : oval.HandleHalfFt;
            pts.Add((ux * nx, Math.Sin(a) * oval.HalfHeightFt));
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
        return LoadedClipTime(u * Motion.SwingContact, SwingPresentation.CommittedLoadAt(charge01), Motion.SwingContact);
    }

    /// <summary>
    /// Seconds from the press to the take's Contact mark (D13): inside the window the ball's plate
    /// time (never before the press), outside it the take's own mark, so the bat misses honestly.
    /// </summary>
    public static double SwingContactSec(double errorFrames, double windowFrames, RulesTable? rules = null) =>
        AtBatResolver.InWindow(errorFrames, windowFrames)
            ? Math.Max(0, Rules.Or(rules).Batting.Window.LeadSec - errorFrames / 60)
            : Motion.SwingContact;

    /// <summary>
    /// Real seconds the committed take lasts after the press: the warped span to contact plus the
    /// authored follow-through and finish (#613), which play at the take's own speed.
    /// </summary>
    public static double SwingTakeSeconds(double contactSec) =>
        Math.Max(0, contactSec) + (Motion.SwingFinish - Motion.SwingContact);

    /// <summary>
    /// The held finish (#583, #613): the batter shows the committed swing, then keeps its last key.
    /// A dead ball (a whiff, a strikeout) holds the finish through the stamp until SET clears the
    /// swing; a ball in play holds it through the contact freeze until the batter-runner is
    /// <paramref name="stepFt"/> (<c>feel.swingFinishStepFt</c>) out of the box. No ready pose in between.
    /// </summary>
    public static bool PresentsSwing(double actionTime, double takeSec, bool contact, double runnerFromBoxFt, double stepFt) =>
        actionTime >= 0 && (actionTime <= takeSec || !contact || runnerFromBoxFt < stepFt);

    public static double PitchClipTime(double poseTime, double charge01) =>
        LoadedClipTime(poseTime, Motion.LoadAtFor(Motion.Verb.ThrowPitch, charge01), Motion.PitchRelease);

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
    /// follow-through and finish. Landing exactly on <paramref name="takeSec"/>
    /// (<see cref="SwingTakeSeconds"/>) presents the finish key; past it the finish is held
    /// (<see cref="PresentsSwing"/>).
    /// </summary>
    public static double AdvanceCommittedSwing(
        double current, double flightTime, double swingStart, double dt, double takeSec = Motion.SwingFinish)
    {
        var target = Math.Max(0, flightTime - swingStart);
        if (current < 0)
            return Math.Min(target, takeSec);
        if (current >= takeSec)
            return current + Math.Max(0, dt);
        return Math.Min(takeSec,
            Math.Max(target, current + Math.Max(0, dt)));
    }

    public static bool PresentsCommittedSwing(double actionTime, double takeSec = Motion.SwingFinish) =>
        actionTime >= 0 && actionTime <= takeSec;

    /// <summary>The committed take's sample: past the take it is the held finish.</summary>
    public static double CommittedSwingSample(double actionTime, double takeSec = Motion.SwingFinish) =>
        Math.Clamp(actionTime, 0, takeSec);
}
