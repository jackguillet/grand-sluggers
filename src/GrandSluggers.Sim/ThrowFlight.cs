namespace GrandSluggers.Sim;

/// <summary>
/// One throw's clock (§8.5): the command-to-release preparation, then the ball's flight on a straight line from the hand to the
/// landing, on the same seconds the bag is judged on. It owns the clock, the release and the line; <see cref="LivePlaySystem"/>
/// decides who throws where, hands the ring on at the release, and judges the landing.
/// </summary>
public sealed class ThrowFlight
{
    /// <summary>Seconds since the command.</summary>
    public double T { get; private set; }

    /// <summary>Command to landing: the preparation plus the flight.</summary>
    public double Duration { get; private set; }

    /// <summary>Command-to-release preparation, part of <see cref="Duration"/>, never part of ball flight.</summary>
    public double ReleaseSec { get; private set; }

    /// <summary>The ball has left the hand.</summary>
    public bool Released { get; private set; }

    /// <summary>The hand the ball leaves.</summary>
    public (double X, double Y, double Z) From { get; private set; }

    /// <summary>Where the ball lands (the bag, the cutoff's glove, or its lateral miss).</summary>
    public (double X, double Y, double Z) To { get; private set; }

    /// <summary>The flight's progress from the release to the landing, 0 to 1.</summary>
    public double Flight01 => Math.Clamp((T - ReleaseSec) / Math.Max(1e-9, Duration - ReleaseSec), 0, 1);

    /// <summary>The ball on the line at <see cref="Flight01"/>.</summary>
    public (double X, double Y, double Z) Ball
    {
        get
        {
            var u = Flight01;
            return (From.X + (To.X - From.X) * u, From.Y + (To.Y - From.Y) * u, From.Z + (To.Z - From.Z) * u);
        }
    }

    /// <summary>The ball has reached its landing.</summary>
    public bool Landed => T >= Duration;

    /// <summary>Seconds until the landing, 0 once there.</summary>
    public double Remaining => Math.Max(0, Duration - T);

    /// <summary>The preparation is spent and the ball has not left the hand yet.</summary>
    public bool ReleaseDue => !Released && T >= ReleaseSec;

    public void Reset()
    {
        T = 0;
        Duration = 0;
        ReleaseSec = 0;
        Released = false;
    }

    /// <summary>A new throw: the clock restarts from the command. The caller releases at once when <see cref="ReleaseDue"/>.</summary>
    public void Begin((double X, double Y, double Z) from, (double X, double Y, double Z) to, double duration, double releaseSec)
    {
        T = 0;
        From = from;
        To = to;
        Duration = duration;
        ReleaseSec = releaseSec;
        Released = false;
    }

    /// <summary>The ball leaves the hand.</summary>
    public void Release() => Released = true;

    /// <summary>One frame of the clock. True on the frame the ball reaches its landing.</summary>
    public bool Advance(double dt)
    {
        var before = T;
        T += dt;
        return before < Duration && T >= Duration;
    }
}
