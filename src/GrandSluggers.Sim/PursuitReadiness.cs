namespace GrandSluggers.Sim;

/// <summary>
/// The couch side of the pursuit stick (the Unity pass of #718: F693-02-pursuit-calibration-policy, -calibration-samples, -arming).
/// The sim keeps each bound device's <see cref="PursuitStick"/> — its profile, its arming, its gates. The client decides when a
/// released-stick window is sampled and what the seat is told; this is that decision, kept out of Unity so a test can hammer it.
///
/// <list type="bullet">
/// <item>A seat a controller sits in starts the match with no profile and samples a window outside live baseball, with the
/// instruction on screen, until one is adopted. A seat with no bound gamepad keeps the identity profile a fresh stick starts on
/// and never waits.</item>
/// <item>A different device taking the seat is a replacement (no profile carried over); the same one coming back is a recovery
/// (profile kept, one neutral read owed again).</item>
/// <item>Call time's entry runs an explicit recalibration for every seated controller; backing out keeps the prior profile.</item>
/// <item>Nothing is sampled while the ball is live. A window that live play, a lost device or a stall interrupts starts over:
/// its samples must span the whole <c>calibrationSec</c> on the input clock, never be stitched across a gap.</item>
/// </list>
///
/// On the shipped table (<see cref="FieldStickRules.Radial"/> false) the stick reads no calibration, and this does nothing.
/// </summary>
public sealed class PursuitReadiness
{
    /// <summary>Seat 0 is player 1's pad, seat 1 player 2's: the index the sim keys a stick by (<see cref="LivePadInput.Device"/>).</summary>
    public const int SeatCount = 2;

    /// <summary>A sample this long after the last one is a stall: the window starts over rather than span the gap.</summary>
    public const double StallSec = 0.10;

    /// <summary>One seat's device this frame, as the client binds it. X / Y are the pursuit coordinate the sim reads.</summary>
    public readonly record struct SeatDevice(bool Human, int? DeviceId, bool Present, double X, double Y)
    {
        public static SeatDevice Empty => new(false, null, false, 0, 0);

        /// <summary>A controller's stick: the device a match-start window is owed for.</summary>
        public bool Analog => Human && DeviceId.HasValue;
    }

    /// <summary>What a seat is told.</summary>
    public enum Tell
    {
        /// <summary>Ready, or not a human's seat.</summary>
        None,
        /// <summary>No profile yet, or Call time asked for one: let go of the stick.</summary>
        LetGo,
        /// <summary>Call time's recalibration was adopted for this seat.</summary>
        Reset
    }

    sealed class Seat
    {
        public bool Seen;
        public int? DeviceId;
        public bool Present;
        public bool Requested;
        public bool Reset;
        public double WindowStart = -1;
        public double LastSample = -1;
    }

    readonly Seat[] _seats = [new(), new()];
    LivePlaySystem? _live;

    /// <summary>
    /// One frame: bind each human seat's device to its stick, then sample a window for a seat that has no profile or that Call
    /// time asked to recalibrate — only when <paramref name="outsidePlay"/>: no live ball, and the seat told to let go (the client
    /// passes SET and the result beat, or Call time's card) — on the input clock <paramref name="clockSec"/>.
    /// </summary>
    public void Tick(LivePlaySystem live, FieldStickRules rules, bool outsidePlay, double clockSec, IReadOnlyList<SeatDevice> devices)
    {
        if (!rules.Radial) return;
        if (!ReferenceEquals(live, _live))
        {
            // A new match is a new set of sticks: every seat binds afresh.
            _live = live;
            for (var i = 0; i < SeatCount; i++) _seats[i] = new Seat();
        }
        for (var i = 0; i < SeatCount; i++)
        {
            var d = i < devices.Count ? devices[i] : SeatDevice.Empty;
            if (!d.Human) continue;
            var seat = _seats[i];
            var stick = live.FieldStick(i);
            Bind(seat, d, stick);
            var owed = seat.Requested || !stick.Calibration.Valid;
            if (!owed || !outsidePlay || !d.Present)
            {
                Restart(seat, stick);
                continue;
            }
            if (seat.LastSample >= 0 && clockSec - seat.LastSample > StallSec + 1e-9) Restart(seat, stick);
            if (seat.WindowStart < 0) seat.WindowStart = clockSec;
            stick.Calibration.Sample(d.X, d.Y, clockSec);
            seat.LastSample = clockSec;
            if (!stick.Calibration.Complete(rules)) continue;
            // The window is spent either way; a refusal keeps the prior profile and the next window starts at once.
            var adopted = stick.Recalibrate(rules);
            seat.WindowStart = seat.LastSample = -1;
            if (adopted && seat.Requested)
            {
                seat.Requested = false;
                seat.Reset = true;
            }
        }
    }

    /// <summary>
    /// Call time's entry: every seated, connected controller recalibrates. False (and nothing starts) when no seat has a
    /// controller to calibrate — the entry is not offered then — or when these sticks have not been bound by a <see cref="Tick"/>.
    /// </summary>
    public bool Request(LivePlaySystem live, FieldStickRules rules, IReadOnlyList<SeatDevice> devices)
    {
        if (!Offered(rules, devices) || !ReferenceEquals(live, _live)) return false;
        for (var i = 0; i < SeatCount; i++)
        {
            var d = i < devices.Count ? devices[i] : SeatDevice.Empty;
            var seat = _seats[i];
            seat.Reset = false;
            seat.Requested = d.Analog && d.Present;
            Restart(seat, live.FieldStick(i));
        }
        return true;
    }

    /// <summary>Leave the recalibration: a seat still sampling keeps its prior profile, its partial window discarded.</summary>
    public void Close()
    {
        for (var i = 0; i < SeatCount; i++)
        {
            var seat = _seats[i];
            seat.Requested = false;
            seat.Reset = false;
            if (_live is not null) Restart(seat, _live.FieldStick(i));
        }
    }

    /// <summary>Call time offers the entry on the calibrated stick when a seated controller is connected.</summary>
    public static bool Offered(FieldStickRules rules, IReadOnlyList<SeatDevice> devices)
    {
        if (!rules.Radial) return false;
        for (var i = 0; i < Math.Min(SeatCount, devices.Count); i++)
            if (devices[i].Analog && devices[i].Present) return true;
        return false;
    }

    /// <summary>A recalibration is running: some seat is still owed its window.</summary>
    public bool Recalibrating
    {
        get
        {
            foreach (var s in _seats)
                if (s.Requested) return true;
            return false;
        }
    }

    /// <summary>Call time's recalibration finished: it was asked for and every seat asked has adopted a window.</summary>
    public bool Recalibrated
    {
        get
        {
            var any = false;
            foreach (var s in _seats)
            {
                if (s.Requested) return false;
                any |= s.Reset;
            }
            return any;
        }
    }

    /// <summary>What seat <paramref name="seat"/> is told this frame on <paramref name="live"/>'s sticks.</summary>
    public Tell TellFor(int seat, LivePlaySystem live, FieldStickRules rules)
    {
        if (!rules.Radial || seat < 0 || seat >= SeatCount || !ReferenceEquals(live, _live)) return Tell.None;
        var s = _seats[seat];
        if (!s.Seen) return Tell.None;
        if (s.Requested) return Tell.LetGo;
        if (s.Reset) return Tell.Reset;
        return live.FieldStick(seat).Calibration.Valid ? Tell.None : Tell.LetGo;
    }

    /// <summary>How far through its current window the seat is, 0 to 1, on the input clock.</summary>
    public double Progress(int seat, FieldStickRules rules, double clockSec)
    {
        if (seat < 0 || seat >= SeatCount) return 0;
        var s = _seats[seat];
        if (s.WindowStart < 0 || rules.CalibrationSec <= 0) return 0;
        return Math.Clamp((clockSec - s.WindowStart) / rules.CalibrationSec, 0, 1);
    }

    static void Bind(Seat seat, SeatDevice d, PursuitStick stick)
    {
        if (!seat.Seen)
        {
            seat.Seen = true;
            // A controller seated for this match has no profile yet.
            if (d.Analog) stick.DeviceReplaced();
        }
        else if (d.DeviceId != seat.DeviceId)
            stick.DeviceReplaced();   // a different device took the seat: nothing is carried over
        else if (d.Present && !seat.Present)
            stick.DeviceRecovered();  // the same device came back: the profile stands, one neutral read is owed
        seat.DeviceId = d.DeviceId;
        seat.Present = d.Present;
    }

    static void Restart(Seat seat, PursuitStick stick)
    {
        if (seat.WindowStart < 0 && seat.LastSample < 0) return;
        stick.Calibration.Begin();
        seat.WindowStart = seat.LastSample = -1;
    }
}
