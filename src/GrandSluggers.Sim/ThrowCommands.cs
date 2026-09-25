namespace GrandSluggers.Sim;

/// <summary>
/// The human's remembered throw presses (#723: F693-03-relay-ownership, -input-buffer, -throw-cancel; F693-02-ordinary-recoil-actions).
/// Three presses wait here for <c>fielding.throw.relayBufferSec</c>: a press aimed at a bag before the glove has the ball (the
/// approach), a press while the throw flies to the cutoff (the relay), and a press while the body recovers (the recovery). A fresh
/// cancel clears them. <see cref="LivePlaySystem"/> says when a press is allowed, and fires the one it takes.
/// </summary>
public sealed class ThrowCommands
{
    bool _cancelWasDown;
    LivePadInput? _approach;
    double _approachAge;
    LivePadInput? _recovery;
    double _relayAge;

    /// <summary>A press waits for the cutter's catch: the onward throw fires at the reception.</summary>
    public bool RelayQueued { get; private set; }

    /// <summary>Any remembered press (the HUD's queue tell).</summary>
    public bool Queued => RelayQueued || _approach is not null || _recovery is not null;

    public void Reset()
    {
        _recovery = null;
        _approach = null;
        _approachAge = 0;
        RelayQueued = false;
        _relayAge = 0;
        _cancelWasDown = false;
    }

    /// <summary>
    /// One frame of the queues. The approach press ages and retargets with the bag keys; a fresh cancel clears every press; an
    /// explicit South with a bag, while <paramref name="mayAim"/> (no ball in hand, no throw, no runner play), is remembered as
    /// the approach. The relay queue lives only while the human owns the throw on a table with a buffer; while
    /// <paramref name="relayInFlight"/> (a throw to the cutoff), a South that is not a cancel queues the onward throw.
    /// </summary>
    public void Tick(LivePadInput field, double dt, ThrowRules rules, bool mayAim, bool humanOwnsThrow, bool relayInFlight,
        List<LiveEvent> events)
    {
        var cancel = field.Cancel && !_cancelWasDown;
        _cancelWasDown = field.Cancel;
        if (_approach is not null)
        {
            _approachAge += dt;
            if (cancel || _approachAge > rules.RelayBufferSec)
            {
                _approach = null;
                events.Add(LiveEvent.ThrowQueueCleared);
            }
            else if (field.KeysBag > 0) _approach = _approach with { KeysBag = field.KeysBag };
        }
        if (cancel) _recovery = null;
        if (field.ExplicitTarget && mayAim && field.SouthDown && field.KeysBag > 0 && !cancel && rules.RelayBufferSec > 0)
        {
            _approach = field;
            _approachAge = 0;
            events.Add(LiveEvent.ThrowQueued);
        }
        if (rules.RelayBufferSec <= 0 || !humanOwnsThrow)
        {
            RelayQueued = false;
            return;
        }
        if (RelayQueued)
        {
            _relayAge += dt;
            if (cancel || _relayAge > rules.RelayBufferSec)
            {
                RelayQueued = false;
                events.Add(LiveEvent.ThrowQueueCleared);
            }
        }
        if (!relayInFlight) return;
        if (field.SouthDown && !cancel)
        {
            RelayQueued = true;
            _relayAge = 0;
            events.Add(LiveEvent.ThrowQueued);
        }
    }

    /// <summary>The cutter caught the ball: a queued relay fires now. True when one was queued.</summary>
    public bool TakeRelay()
    {
        if (!RelayQueued) return false;
        RelayQueued = false;
        return true;
    }

    /// <summary>The remembered approach press, merged with this frame's bag key, taken once; null when none.</summary>
    public LivePadInput? TakeApproach(LivePadInput pad)
    {
        if (_approach is not { } approach) return null;
        _approach = null;
        return approach with { KeysBag = pad.KeysBag > 0 ? pad.KeysBag : approach.KeysBag };
    }

    /// <summary>A press inside the recovery's last buffer is remembered, to fire at readiness.</summary>
    public void RememberRecovery(LivePadInput pad) => _recovery = pad;

    /// <summary>The remembered recovery press, taken once; null when none.</summary>
    public LivePadInput? TakeRecovery()
    {
        var remembered = _recovery;
        _recovery = null;
        return remembered;
    }

    /// <summary>A recovery press is dropped (a cancel, a new dive).</summary>
    public void DropRecovery() => _recovery = null;
}
