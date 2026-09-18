namespace GrandSluggers.Sim;

/// <summary>
/// One frame's read of the human seat's pursuit stick (#718): who owns the body this frame — the seat (manual) or the
/// assistance — and the velocity the seat asks for as a fraction of the glove speed along the stick. On the shipped table
/// the want is the raw stick vector; on the calibrated radial stick it is the unit direction × the remapped magnitude.
/// </summary>
public readonly record struct StickRead(bool Manual, double WantX, double WantY)
{
    public static StickRead Assist => new(false, 0, 0);
}

/// <summary>
/// A bound device's released-stick centre (F693-02-pursuit-calibration-policy, -calibration-samples, #718). Samples are the
/// documented normalized device coordinate before any centre subtraction, on a non-live input clock — never paused sim time.
/// A window is <c>fielding.stick.calibrationSec</c> long; it is adopted only when complete, its mean radial offset is at most
/// <c>centerOffsetMax</c> and every sample is at most <c>sampleSpreadMax</c> from that mean, inclusive. Anything else keeps
/// the prior profile. A fresh device starts on the identity profile — centre (0, 0), what a digital stick or a perfectly
/// centred one reports — so a keyboard seat never waits on a window; a client that binds an analog device calls
/// <see cref="Invalidate"/> and samples a window before the seat can arm. Nothing is copied to a replacement device.
/// </summary>
public sealed class StickCalibration
{
    readonly List<(double T, double X, double Y)> _window = [];

    public bool Valid { get; private set; } = true;
    public (double X, double Y) Center { get; private set; }
    public bool Sampling => _window.Count > 0;
    public int Adopted { get; private set; }
    public int Refused { get; private set; }

    /// <summary>A replaced or newly bound device has no profile until a window is adopted.</summary>
    public void Invalidate()
    {
        Valid = false;
        Center = (0, 0);
        _window.Clear();
    }

    /// <summary>Start a window over; a recalibration is explicit, never a silent centre learned during play.</summary>
    public void Begin() => _window.Clear();

    /// <summary>One released-stick sample at <paramref name="clockSec"/> on the input clock.</summary>
    public void Sample(double x, double y, double clockSec) => _window.Add((clockSec, x, y));

    /// <summary>The window spans the calibration time.</summary>
    public bool Complete(FieldStickRules r) =>
        _window.Count >= 2 && _window[^1].T - _window[0].T >= r.CalibrationSec - 1e-9;

    /// <summary>
    /// Adopt the window when it is complete and valid; a complete window is spent either way, an incomplete one keeps
    /// filling. Returns whether a profile was adopted; a refusal keeps the prior profile, valid or not.
    /// </summary>
    public bool TryAdopt(FieldStickRules r)
    {
        if (!Complete(r)) return false;
        var mx = _window.Average(s => s.X);
        var my = _window.Average(s => s.Y);
        var offset = Math.Sqrt(mx * mx + my * my);
        var spread = _window.Max(s => Math.Sqrt((s.X - mx) * (s.X - mx) + (s.Y - my) * (s.Y - my)));
        _window.Clear();
        if (offset > r.CenterOffsetMax + 1e-12 || spread > r.SampleSpreadMax + 1e-12)
        {
            Refused++;
            return false;
        }
        Center = (mx, my);
        Valid = true;
        Adopted++;
        return true;
    }

    /// <summary>The raw stick with this device's centre subtracted.</summary>
    public (double X, double Y) Apply(double rawX, double rawY) => (rawX - Center.X, rawY - Center.Y);
}

/// <summary>
/// The human seat's pursuit stick, per bound device (#718: F693-02-pursuit-neutral-boundary, -analog-response, -arming).
/// The seat arms with a valid profile plus one neutral observation (magnitude ≤ <c>leaveMag</c>) after defensive-role
/// entry, device recovery or a recalibration, and stays armed through the half. Armed, manual pursuit begins at
/// <c>enterMag</c> and returns to assistance at <c>leaveMag</c>, the owner kept between the two; the asked speed is the
/// linear remap of the magnitude from <c>leaveMag</c> to 1, capped at 1, along the calibrated direction. Unarmed, every
/// read is assistance — and the client shows that the seat is not ready.
/// </summary>
public sealed class PursuitStick
{
    public StickCalibration Calibration { get; } = new();

    /// <summary>A valid profile and one neutral observation since the last arming event.</summary>
    public bool Armed { get; private set; }

    /// <summary>The owner this frame: the seat steers (true) or the assistance runs the body (false).</summary>
    public bool Manual { get; private set; }

    /// <summary>The last read's calibrated radial magnitude.</summary>
    public double Magnitude { get; private set; }

    /// <summary>Defensive-role entry: the seat owes one neutral observation before it steers.</summary>
    public void EnterDefense()
    {
        Armed = false;
        Manual = false;
    }

    /// <summary>The same device reconnected: the profile stands, the neutral observation is owed again.</summary>
    public void DeviceRecovered() => EnterDefense();

    /// <summary>A different device bound: no profile is copied over, and the neutral observation is owed.</summary>
    public void DeviceReplaced()
    {
        Calibration.Invalidate();
        EnterDefense();
    }

    /// <summary>Adopt the sampled window if it is complete and valid; a success owes the neutral observation again.</summary>
    public bool Recalibrate(FieldStickRules r)
    {
        if (!Calibration.TryAdopt(r)) return false;
        EnterDefense();
        return true;
    }

    /// <summary>One read a frame of the raw stick in the documented device coordinate.</summary>
    public StickRead Read(double rawX, double rawY, FieldStickRules r)
    {
        var (x, y) = Calibration.Apply(rawX, rawY);
        var m = Math.Sqrt(x * x + y * y);
        Magnitude = m;
        if (!Armed)
        {
            if (Calibration.Valid && m <= r.LeaveMag) Armed = true;
            Manual = false;
            return StickRead.Assist;
        }
        if (Manual)
        {
            if (m <= r.LeaveMag) Manual = false;
        }
        else if (m >= r.EnterMag)
        {
            Manual = true;
        }
        if (!Manual) return StickRead.Assist;
        var span = Math.Max(1e-9, 1 - r.LeaveMag);
        var t = Math.Clamp((m - r.LeaveMag) / span, 0, 1);
        return new StickRead(true, x / m * t, y / m * t);
    }
}
