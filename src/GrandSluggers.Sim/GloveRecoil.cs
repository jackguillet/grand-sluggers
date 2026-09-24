namespace GrandSluggers.Sim;

/// <summary>
/// The glove's impact recoil (#720: F693-02-ground-pickup-recoil-basis, -ground-pickup-recoil-cap, -recoil-field-shaping,
/// -recoil-field-factors, -recoil-severity-curve, -ordinary-recoil-actions, -ordinary-recoil-displacement,
/// -ordinary-recoil-distance-cap, -ordinary-recoil-motion-profile). One live play owns one: it samples the ball's speed at the
/// take, charges the take once, and runs the recovery down. The world goes on through it; only the body's own steering and
/// throw start wait, and <see cref="LivePlaySystem"/> reads <see cref="Active"/> for that.
/// </summary>
public sealed class GloveRecoil
{
    (double X, double Z) _kick;

    /// <summary>The recovery left, seconds on the play clock; 0 when none. Above 0 exactly while <see cref="Active"/>.</summary>
    public double T { get; private set; }

    /// <summary>A recovery is running: steering and the throw start wait for it; possession does not.</summary>
    public bool Active { get; private set; }

    /// <summary>The full length the recovery started from (<c>recoil.capSec × w</c>); the kick's decay reads it. Kept to the play's end for the outcome.</summary>
    public double Duration { get; private set; }

    /// <summary>The ball's speed the frame before the glove took the batted ball, ft/s on the play clock; 0 until the take. Sampled on both tables.</summary>
    public double IncomingFtPerSec { get; private set; }

    /// <summary>This play's take has been charged (or ruled free): a play charges its take once.</summary>
    public bool Charged { get; private set; }

    public void Reset()
    {
        T = 0;
        Active = false;
        Duration = 0;
        IncomingFtPerSec = 0;
        _kick = (0, 0);
        Charged = false;
    }

    /// <summary>The one input the recoil reads: the ball's velocity the frame before possession attaches it to the glove.</summary>
    public void SampleIncoming((double X, double Y, double Z) ballVel) =>
        IncomingFtPerSec = Math.Sqrt(ballVel.X * ballVel.X + ballVel.Y * ballVel.Y + ballVel.Z * ballVel.Z);

    /// <summary>The take is spent, whether or not it costs anything.</summary>
    public void MarkCharged() => Charged = true;

    /// <summary>
    /// What this take costs the hands, read off the incoming speed and the body's Hands: the same ball to the same hands costs
    /// the same every time, and a routine arrival costs nothing at all. The recovery is <c>capSec × w</c>; the kick is
    /// <c>kickFtPerSec × w</c> along the ball's horizontal travel, slowing linearly to rest over the recovery (<c>w²</c> feet); a
    /// purely vertical arrival supplies no kick. Returns whether a recovery started.
    /// </summary>
    public bool Arm(Character who, bool airborne, (double X, double Y, double Z) ballVel, RulesTable rules)
    {
        var w = FieldingResolver.RecoilWeight(who, IncomingFtPerSec, rules, airborne);
        if (w <= 0) return false;
        Duration = rules.Fielding.Recoil.CapSec * w;
        T = Duration;
        Active = true;
        var h = Math.Sqrt(ballVel.X * ballVel.X + ballVel.Z * ballVel.Z);
        var kick = FieldingResolver.RecoilKickFtPerSec(w, rules);
        _kick = h > 1e-9 ? (ballVel.X / h * kick, ballVel.Z / h * kick) : (0, 0);
        return true;
    }

    /// <summary>
    /// One frame: the clock runs down, and the kick's exact integral over the frame — <c>K (1 − t / T)</c> from t to t + dt — is
    /// the displacement the caller moves the body (and the ball in its glove) by, on top of whatever the idle brake left of the
    /// body's own locomotion. Null when the frame moves nothing. At readiness the recovery ends.
    /// </summary>
    public (double X, double Z)? Step(double dt)
    {
        (double X, double Z)? move = null;
        var t0 = Math.Clamp(Duration - T, 0, Duration);
        T -= dt;
        var t1 = Math.Clamp(Duration - T, 0, Duration);
        if (Duration > 0 && t1 > t0)
        {
            var s = (t1 - t0) - (t1 * t1 - t0 * t0) / (2 * Duration);
            move = (_kick.X * s, _kick.Z * s);
        }
        if (T <= 1e-9)
        {
            T = 0;
            Active = false;
            _kick = (0, 0);
        }
        return move;
    }
}
