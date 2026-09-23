namespace GrandSluggers.Sim;

/// <summary>
/// The side a bunt is held toward (spec §5.8, PH-14-R2, PH-14-R5). It is the batter's intent, not a landing
/// point: it leans the outgoing ball (<see cref="BuntHold.LeanDeg"/>) and the contact decides the rest.
/// <see cref="None"/> is not squared. The same two values name the two bunt triggers: <see cref="Third"/> is
/// LT / J, <see cref="First"/> is RT / L (the bindings are the client's).
/// </summary>
public enum BuntSide
{
    None = 0,
    /// <summary>Toward third base: negative spray, the left side of the field.</summary>
    Third = 1,
    /// <summary>Toward first base: positive spray, the right side of the field.</summary>
    First = 2
}

/// <summary>
/// The two bunt triggers between ticks. <paramref name="Side"/> is the held intent: the latest press among the
/// triggers that are down, <see cref="BuntSide.None"/> when none is. <paramref name="Fixed"/> is set by contact
/// (<see cref="BuntHold.Contact"/>): the ball has left and no trigger moves the side again this pitch.
/// <paramref name="ThirdSpent"/> / <paramref name="FirstSpent"/> are the PH-14-R6 guard: a trigger that was
/// held for a bunt at contact counts as nothing, for any verb, until it comes up and is pressed again.
/// <c>default</c> is at rest: not squared, nothing spent.
/// </summary>
public readonly record struct BuntHoldState(
    BuntSide Side,
    bool Fixed = false,
    bool ThirdSpent = false,
    bool FirstSpent = false)
{
    /// <summary>
    /// The next pitch's state: contact no longer fixes a side. A spent trigger stays spent until it comes up,
    /// and a trigger still down (and not spent) still squares: the bunt is held, not latched.
    /// </summary>
    public BuntHoldState NextPitch() => this with { Fixed = false, Side = Fixed ? BuntSide.None : Side };
}

/// <summary>
/// One tick of <see cref="BuntHold.Advance"/>. <paramref name="Squared"/> is the bat held out on this tick
/// toward <see cref="BuntHoldState.Side"/> (accepting, a trigger down, no contact yet). <paramref name="Pressed"/>
/// is an accepted press on this tick that squared the bat or moved it to the other side: the edge that
/// replaces an uncommitted swing load (PH-13-R1). <paramref name="Withdrew"/> is the tick every trigger came
/// up before contact: the bat comes back and nothing is latched.
/// </summary>
public readonly record struct BuntHoldStep(
    BuntHoldState Next,
    bool Squared,
    bool Pressed,
    bool Withdrew)
{
    /// <summary>The side the defense sees on this tick: the held side while squared, else none.</summary>
    public BuntSide Showing => Squared ? Next.Side : BuntSide.None;
}

/// <summary>
/// The held directional bunt (spec §5.8; PH-14-R2 … R6), pure and beside <see cref="ChargeButton"/>: no clock,
/// no draw, no <c>Rules.Default</c>. Both seats and the CPU batter use it; the client passes its triggers and the
/// headless CPU passes its decided side (<see cref="SwingCommand.HeldBunt"/>).
///
/// <para>One tick applies, in this order:</para>
/// <list type="number">
/// <item><b>Spent.</b> A trigger that was held for a bunt at contact stays spent while it is down. It clears
/// when it comes up, or on a press edge (it came up and went down again between ticks). A spent trigger counts
/// as no verb: it neither squares nor changes the side (PH-14-R6).</item>
/// <item><b>Fixed.</b> After contact the side is the ball's fact; no trigger moves it (PH-14-R3).</item>
/// <item><b>Side.</b> Among the triggers that are down and not spent, the latest press wins. With both down,
/// the one pressed on this tick wins; with both pressed on one tick, or both down and neither pressed, the side
/// already held stays, and from no side it is the third-base side. With one down, it is that one's side:
/// releasing the active side while the other is held moves the bat to the other side. With none down, the bat
/// is withdrawn. A tap (down and up inside one tick) holds nothing.</item>
/// <item><b>Accepting.</b> The side is tracked on every tick, but the bat is out only while the plate accepts a
/// bunt (<paramref name="accepting"/>): in SET and in the pitch's flight, never after a committed swing.</item>
/// </list>
/// </summary>
public static class BuntHold
{
    public static BuntHoldStep Advance(
        BuntHoldState state,
        bool thirdPressed,
        bool thirdHeld,
        bool firstPressed,
        bool firstHeld,
        bool accepting = true)
    {
        // (1) Spent: only coming up (or a fresh press edge) frees a trigger held at contact.
        var thirdSpent = state.ThirdSpent && thirdHeld && !thirdPressed;
        var firstSpent = state.FirstSpent && firstHeld && !firstPressed;

        // (2) Fixed: contact decided the ball; the triggers only clear their spent flags.
        if (state.Fixed)
            return new BuntHoldStep(state with { ThirdSpent = thirdSpent, FirstSpent = firstSpent }, false, false, false);

        // (3) Side: latest press among the triggers that are down and free.
        var thirdDown = thirdHeld && !thirdSpent;
        var firstDown = firstHeld && !firstSpent;
        var thirdNew = thirdPressed && thirdDown;
        var firstNew = firstPressed && firstDown;
        BuntSide side;
        if (thirdDown && firstDown)
            side = thirdNew && !firstNew ? BuntSide.Third
                : firstNew && !thirdNew ? BuntSide.First
                : state.Side != BuntSide.None ? state.Side
                : BuntSide.Third;
        else
            side = thirdDown ? BuntSide.Third : firstDown ? BuntSide.First : BuntSide.None;

        // (4) Accepting: the bat is out only while the plate takes a bunt.
        var squared = accepting && side != BuntSide.None;
        var pressed = squared && (side == BuntSide.Third ? thirdNew : firstNew);
        var withdrew = accepting && side == BuntSide.None && state.Side != BuntSide.None;
        return new BuntHoldStep(new BuntHoldState(side, false, thirdSpent, firstSpent), squared, pressed, withdrew);
    }

    /// <summary>
    /// The held bat met the ball (PH-14-R3, PH-14-R6): the side is fixed for the ball that left, and every
    /// trigger down at that instant is spent until it comes up. The client calls it on the contact tick with the
    /// triggers as they stand.
    /// </summary>
    public static BuntHoldState Contact(BuntHoldState state, bool thirdHeld, bool firstHeld) =>
        new(state.Side, true, state.ThirdSpent || thirdHeld, state.FirstSpent || firstHeld);

    /// <summary>
    /// Whether <paramref name="trigger"/> may be read as any verb on this tick (PH-14-R6): false while it is still
    /// down from a bunt that made contact. Any other reader of LT / RT / J / L asks this first.
    /// </summary>
    public static bool IsFree(BuntHoldState state, BuntSide trigger) => trigger switch
    {
        BuntSide.Third => !state.ThirdSpent,
        BuntSide.First => !state.FirstSpent,
        _ => true
    };

    /// <summary>
    /// The side's lean on the outgoing bunt in degrees of spray (<c>batting.bunt.sideDeg</c>): toward first is
    /// positive, toward third negative, none is straight. A bias, never a landing point (PH-14-R2).
    /// </summary>
    public static double LeanDeg(BuntSide side, RulesTable? rules = null) => side switch
    {
        BuntSide.Third => -Rules.Or(rules).Batting.Bunt.SideDeg,
        BuntSide.First => Rules.Or(rules).Batting.Bunt.SideDeg,
        _ => 0
    };
}

/// <summary>
/// The plate's buttons on one tick: the swing button, the two bunt triggers and the cancel
/// (<see cref="PlateButtons.Advance"/>).
/// </summary>
public readonly record struct PlateInput(
    bool SouthPressed,
    bool SouthHeld,
    bool SouthReleased,
    bool ThirdPressed = false,
    bool ThirdHeld = false,
    bool FirstPressed = false,
    bool FirstHeld = false,
    bool Cancel = false);

/// <summary>
/// The plate between ticks: the swing button, the bunt triggers, and whether a swing already committed on this
/// pitch (<paramref name="SwingCommitted"/>), after which the swing follows through and no bunt squares.
/// </summary>
public readonly record struct PlateButtonsState(
    ChargeButtonState Swing,
    BuntHoldState Bunt,
    bool SwingCommitted = false)
{
    /// <summary>
    /// The next pitch: no swing is committed and contact no longer fixes a side. The swing button's
    /// must-release and the spent triggers carry: a hold still has to come up.
    /// </summary>
    public PlateButtonsState NextPitch() => new(Swing with { Armed = false, Fill01 = 0, SecondsPastFull = 0 }, Bunt.NextPitch());
}

/// <summary>
/// One tick of <see cref="PlateButtons.Advance"/>. <paramref name="Converted"/> is the tick a bunt press replaced
/// an armed swing load (PH-13-R1): the load and its charge are gone and the bat squares.
/// </summary>
public readonly record struct PlateButtonsStep(
    PlateButtonsState Next,
    ChargeButtonStep Swing,
    BuntHoldStep Bunt,
    bool Converted);

/// <summary>
/// The swing and the held bunt on one tick (spec §5.1, §5.8; PH-13-R1, PH-14-R4). The bunt runs first, then the
/// swing button, and the square is the swing's cancel (<see cref="ChargeButton.Advance"/>):
/// <list type="number">
/// <item><b>Committed.</b> A swing that committed on an earlier tick of this pitch follows through: no trigger
/// squares until <see cref="PlateButtonsState.NextPitch"/>.</item>
/// <item><b>Bunt.</b> <see cref="BuntHold.Advance"/>.</item>
/// <item><b>Swing.</b> While the bat is squared the swing button is cancelled: an armed load (or a press on this
/// tick) is discarded with its charge, even when South comes up on the same tick (the square beats the release,
/// as cancel does), and a button still down must come up before a fresh press loads a new swing from zero. The
/// explicit cancel (East / G) is the same cancel. To swing from a square, release every trigger, then press
/// South.</item>
/// </list>
/// Pure: the caller owns the state, the clock and the charge seconds.
/// </summary>
public static class PlateButtons
{
    public static PlateButtonsStep Advance(
        PlateButtonsState state,
        PlateInput input,
        double deltaSeconds,
        double secondsToFull,
        bool accepting = true,
        bool commits = true)
    {
        var bunt = BuntHold.Advance(state.Bunt, input.ThirdPressed, input.ThirdHeld, input.FirstPressed,
            input.FirstHeld, accepting && !state.SwingCommitted);
        var swing = ChargeButton.Advance(state.Swing, input.SouthPressed, input.SouthHeld, input.SouthReleased,
            deltaSeconds, secondsToFull, accepting, commits, cancel: input.Cancel || bunt.Squared);
        var converted = bunt.Pressed && swing.Cancelled && state.Swing.Armed;
        return new PlateButtonsStep(
            new PlateButtonsState(swing.Next, bunt.Next, state.SwingCommitted || swing.Committed),
            swing, bunt, converted);
    }

    /// <summary>
    /// What the plate offers when the ball reaches it (spec §5.8): the held bunt if the bat is out, else
    /// <see langword="null"/> (a committed swing is already the caller's; otherwise the pitch is taken).
    /// </summary>
    public static SwingCommand? HeldBuntAtPlate(PlateButtonsStep tick, double boxOffsetX, double squareSec = 0,
        bool human = true) =>
        tick.Bunt.Squared ? SwingCommand.HeldBunt(tick.Bunt.Next.Side, boxOffsetX, squareSec, human) : null;
}
