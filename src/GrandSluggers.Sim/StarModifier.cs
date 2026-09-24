namespace GrandSluggers.Sim;

/// <summary>
/// One seat's held special modifier between ticks (spec §12, PH-16-R10, R11, R17). <paramref name="Spent"/> is the
/// leak guard: a modifier held at an accepted release asked for that release's special, and it counts as nothing —
/// not the next release's special, not all-advance, not the cutoff, not half of the halt — until it comes up.
/// <c>default</c> is at rest: nothing spent.
/// </summary>
public readonly record struct StarModifierState(bool Spent = false);

/// <summary>
/// One accepted release as the modifier read it. <paramref name="Request"/> is the special asked for: the
/// modifier was down, and free, on the release tick. <paramref name="Next"/> is the state after it.
/// </summary>
public readonly record struct StarModifierRelease(bool Request, StarModifierState Next);

/// <summary>
/// The held special modifier (spec §12, PH-16-R10 … R12, R17), pure: no clock, no draw, no rules table. The
/// binding is the client's (the star button held on either seat's own pad).
///
/// <para>
/// <b>Read at the release.</b> The pitch or the swing is ordinary or special on the tick its release is accepted
/// (<see cref="Release"/>): pressing or letting go of the modifier while the button charges changes the intent; after the release nothing does. The ordinary pitch family still locks at
/// the charge (§3). The client sends the request; <see cref="Match"/> settles it (an unaffordable request is the
/// ordinary action, spends nothing, and is recorded as a <see cref="StarRequest"/> not afforded).
/// </para>
///
/// <para>
/// <b>No leak.</b> The same button means other verbs once the ball is live (LB is all-advance and the cutoff),
/// so a hold that asked for a special is spent until it comes up (<see cref="Tick"/>), the way a bunt trigger held
/// at contact is (PH-14-R6). A spent hold asks for no special at the next release either: let go and hold it again.
/// Every other reader of the modifier's button asks <see cref="IsFree"/> first.
/// </para>
/// </summary>
public static class StarModifier
{
    /// <summary>
    /// One tick before any reader: a modifier that is up is free again. Held, the state is unchanged (a spent hold
    /// stays spent; a free hold stays free until a release takes it).
    /// </summary>
    public static StarModifierState Tick(StarModifierState state, bool held) => held ? state : default;

    /// <summary>
    /// An accepted release on this tick. The request is the modifier held and free; a request spends the hold. A
    /// release with the modifier up, or with a spent hold, is the ordinary action and leaves the state as it was.
    /// </summary>
    public static StarModifierRelease Release(StarModifierState state, bool held)
    {
        var request = held && !state.Spent;
        return new(request, request ? new StarModifierState(true) : state);
    }

    /// <summary>Whether the modifier's button may mean any other verb on this tick.</summary>
    public static bool IsFree(StarModifierState state) => !state.Spent;
}
