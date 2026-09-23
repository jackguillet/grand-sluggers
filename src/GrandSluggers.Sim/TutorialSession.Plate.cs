namespace GrandSluggers.Sim;

/// <summary>
/// One plate tick a player's pad sent to a plate lesson: the buttons (<see cref="PlateInput"/>), the tick's
/// seconds and whether a release commits (false in SET, spec §3).
/// </summary>
public sealed record TutorialPlateTick(PlateInput Input, double Seconds, bool Commits);

/// <summary>
/// Plate lessons read the player's own plate buttons through the Exhibition step (<see cref="PlateButtons.Advance"/>),
/// so a verdict about a cancel (T-B10) rests on the press the player made, never on a flag a caller sets.
/// </summary>
public sealed partial class TutorialSession
{
    PlateButtonsState _plate;
    bool _plateCancelledLoad;

    /// <summary>The plate as this lesson's own step saw it (evidence; the client keeps its own copy for the body).</summary>
    public PlateButtonsState PlateState => _plate;

    /// <summary>An explicit cancel (East / G) discarded an armed swing load on this attempt (PH-13-R1).</summary>
    public bool CancelledLoad => _plateCancelledLoad;

    void ResetPlateEvidence()
    {
        _plate = default;
        _plateCancelledLoad = false;
    }

    /// <summary>
    /// One plate tick of the teaching seat, before the ball reaches the plate. Recorded for replay; it moves no
    /// clock and decides nothing by itself: the pitch is still judged by <see cref="Swing"/>.
    /// </summary>
    public bool Plate(TutorialPlateTick tick, LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        if (tick is null || !Accepts(source) || _setup.Policy is not ("cpu-strike" or "cpu-ball")) return false;
        if (!double.IsFinite(tick.Seconds) || tick.Seconds < 0 || tick.Seconds > 1)
            throw new ArgumentOutOfRangeException(nameof(tick));
        _inputs.Add(new(Elapsed, source, Plate: tick));
        var before = _plate;
        var step = PlateButtons.Advance(before, tick.Input, tick.Seconds, _content.Feel.SwingChargeSeconds,
            accepting: true, commits: tick.Commits);
        _plate = step.Next;
        // The explicit cancel, not a bunt's conversion: East / G pressed while a load was armed.
        if (tick.Input.Cancel && step.Swing.Cancelled && before.Swing.Armed && !step.Bunt.Squared
            && source == LivePlayCommandSource.Human && !Demonstration)
            _plateCancelledLoad = true;
        return true;
    }
}
