using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The swing cancel (P4-a, PH-13, PH-13-R1, spec §5.1), Appendix B.1 rows S-150 … S-152. A cancel discards
/// an uncommitted load and its charge; the hold it cancelled cannot swing; a new swing needs a fresh press
/// and starts from zero. Every row drives <see cref="ChargeButton.Advance"/> frame by frame, the way the
/// client does, and reads the swing through <see cref="SwingInputIntent.Capture"/>.
/// </summary>
public sealed class ChargeCancelScenarioTests
{
    readonly ContentCatalog _content = Shipped.Content;
    const double Dt = 1.0 / 60;

    double ToFull => _content.Feel.SwingChargeSeconds;

    /// <summary>One frame of the batter's button, the client's order: edges first, then the step.</summary>
    ChargeButtonStep Frame(ref ChargeButtonState state, bool pressed = false, bool held = false, bool released = false,
        bool cancel = false)
    {
        var step = ChargeButton.Advance(state, pressed, held, released, Dt, ToFull, cancel: cancel);
        state = step.Next;
        return step;
    }

    // ---------------------------------------------------------------------------------
    // S-150  A cancel before the commit takes the pitch
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S150_ACancelledLoadNeverSwingsAndThePitchIsTaken()
    {
        var match = new Scenario(_content).Match;
        var state = default(ChargeButtonState);
        var committed = false;

        // Load to MAX and past it, cancel, hold the button through the plate, then let it go.
        Frame(ref state, pressed: true, held: true);
        for (var f = 0; f < 40; f++) Frame(ref state, held: true);
        Assert.True(state.Armed);
        Assert.Equal(1, state.Fill01);
        var cancel = Frame(ref state, held: true, cancel: true);
        Assert.True(cancel.Cancelled);
        Assert.Equal(new ChargeButtonState(false, 0, 0, MustRelease: true), state);
        for (var f = 0; f < 60; f++)
            committed |= SwingInputIntent.Capture(Frame(ref state, held: true), 0, 0, false, 0, rules: Rules.Default).Committed;
        committed |= SwingInputIntent.Capture(Frame(ref state, released: true), 0, 0, false, 0, rules: Rules.Default).Committed;
        Assert.False(committed, "the cancelled hold's release is not a swing");
        Assert.Equal(default, state);

        // No commit is a take: the umpire calls the pitch.
        var ball = match.Play(Scenario.PitchAt(2.5, StrikeZoneGeometry.Reference.CenterY), Scenario.Take);
        Assert.Equal(PlayKind.TakeBall, ball.Kind);
        var strike = match.Play(Scenario.PitchAt(0, StrikeZoneGeometry.Reference.CenterY), Scenario.Take);
        Assert.Equal(PlayKind.TakeStrike, strike.Kind);
        Assert.Equal((1, 1), (match.Balls, match.Strikes));
    }

    // ---------------------------------------------------------------------------------
    // S-151  A new swing needs a fresh press, from zero
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S151_AfterACancelOnlyAFreshPressSwingsAndItBanksNoCharge()
    {
        var state = default(ChargeButtonState);
        Frame(ref state, pressed: true, held: true);
        for (var f = 0; f < 20; f++) Frame(ref state, held: true);
        var loaded = state.Fill01;
        Assert.True(loaded > 0.5);
        Frame(ref state, held: true, cancel: true);

        // Still down: nothing fills and nothing arms.
        for (var f = 0; f < 10; f++)
        {
            var hold = Frame(ref state, held: true);
            Assert.False(hold.Committed);
            Assert.Equal((false, 0.0), (state.Armed, state.Fill01));
        }
        Assert.False(Frame(ref state, released: true).Committed);

        // The fresh press starts from zero: one frame of fill, a slap, not the load that was cancelled.
        Frame(ref state, pressed: true, held: true);
        Assert.True(state.Armed);
        Assert.Equal(Dt / ToFull, state.Fill01, 12);
        var swing = Frame(ref state, released: true);
        Assert.True(swing.Committed);
        Assert.Equal(Dt / ToFull, swing.CommitFill01, 12);
        Assert.True(ChargeFeel.IsSlap(swing.CommitFill01));
    }

    // ---------------------------------------------------------------------------------
    // S-152  The same-tick order: not accepting, cancel, must release, load
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S152_CancelBeatsTheReleaseOnItsTickAndCancelsNothingElse()
    {
        // Cancel and release on one tick: no swing, and the button is up, so nothing is owed.
        var state = default(ChargeButtonState);
        Frame(ref state, pressed: true, held: true);
        Frame(ref state, held: true);
        var both = Frame(ref state, released: true, cancel: true);
        Assert.True(both.Cancelled);
        Assert.False(both.Committed);
        Assert.Equal(default, state);

        // Cancel on the press's own tick: that press is discarded and its hold must come up.
        state = default;
        var onPress = Frame(ref state, pressed: true, held: true, cancel: true);
        Assert.True(onPress.Cancelled);
        Assert.True(state.MustRelease);
        Frame(ref state, held: true);
        Assert.Equal(0, state.Fill01);

        // A press, hold and release on the tick the old hold comes up count for nothing.
        var tap = Frame(ref state, pressed: true, held: true, released: true);
        Assert.False(tap.Committed);
        Assert.Equal(default, state);

        // A cancel with nothing loaded is nothing, and does not block the next press.
        state = default;
        var idle = Frame(ref state, cancel: true);
        Assert.False(idle.Cancelled);
        Assert.Equal(default, state);
        Frame(ref state, pressed: true, held: true);
        Assert.True(Frame(ref state, released: true).Committed);

        // A committed swing is gone: a cancel on the next tick has nothing to take back.
        state = default;
        Frame(ref state, pressed: true, held: true);
        var commit = Frame(ref state, released: true);
        Assert.True(commit.Committed);
        var late = Frame(ref state, cancel: true);
        Assert.False(late.Cancelled);

        // Not accepting is at rest, a cancelled hold included.
        state = new ChargeButtonState(false, 0, 0, MustRelease: true);
        var rest = ChargeButton.Advance(state, false, true, false, Dt, ToFull, accepting: false, cancel: true);
        Assert.Equal(default, rest);
    }
}
