namespace GrandSluggers.Sim;

/// <summary>
/// The glove's normal jump (#719: F693-02-normal-jump-takeoff-ownership, -input-buffer, -input-profile, -arc): West's press,
/// its buffer, and the airborne clock with the root rise. One profile per press, the same for every body. <see cref="LivePlaySystem"/>
/// says when a takeoff is eligible, and starts the arm window and the event on a takeoff.
/// </summary>
public sealed class NormalJump
{
    bool _westHeld;
    double _age;
    string _pos = "";

    /// <summary>In the air on a normal jump.</summary>
    public bool Airborne { get; private set; }

    /// <summary>Seconds since takeoff while <see cref="Airborne"/>.</summary>
    public double AirT { get; private set; }

    /// <summary>The root rise now, <c>4 H u (1 − u)</c> over the airtime; 0 on the ground.</summary>
    public double HeightFt { get; private set; }

    /// <summary>This jump's peak rise <c>H</c>: <c>catch.jumpRiseFt</c> for every body but a Lily Leap (§8.4), set at takeoff.</summary>
    public double RiseFt { get; private set; }

    /// <summary>A blocked press is remembered, bound to the body it was made on.</summary>
    public bool Pending { get; private set; }

    public void Reset()
    {
        Airborne = false;
        AirT = 0;
        HeightFt = 0;
        RiseFt = 0;
        Pending = false;
        _age = 0;
        _pos = "";
        _westHeld = false;
    }

    /// <summary>
    /// One frame of West: a fresh grounded press takes off at once when <paramref name="eligible"/>; blocked by the read or a
    /// recovery it is remembered for <c>catch.jumpBufferSec</c>, bound to <paramref name="glovePos"/>, and takes off at the first
    /// eligible instant; a press in the air queues nothing, and holding through the landing repeats nothing. A throw or a dive
    /// already committed (<paramref name="committed"/>) prevents the buffer. True on the frame the body takes off.
    /// </summary>
    public bool Press(bool westDown, string glovePos, bool committed, Func<bool> eligible, double dt, CatchRules rules,
        double? riseFt = null)
    {
        var rise = riseFt ?? rules.JumpRiseFt;
        var fresh = westDown && !_westHeld;
        _westHeld = westDown;
        if (Pending && (_pos != glovePos || committed))
            Pending = false;
        if (fresh && !Airborne)
        {
            if (eligible()) return Takeoff(rise);
            if (!committed && rules.JumpBufferSec > 0)
            {
                Pending = true;
                _age = 0;
                _pos = glovePos;
            }
            return false;
        }
        if (!Pending) return false;
        _age += dt;
        if (eligible()) { Pending = false; return Takeoff(rise); }
        if (_age > rules.JumpBufferSec + 1e-9) Pending = false;
        return false;
    }

    /// <summary>The airborne clock and the root rise; the body lands when the airtime is spent.</summary>
    public void Tick(double dt, CatchRules rules)
    {
        if (!Airborne) return;
        AirT += dt;
        if (AirT >= rules.JumpAirSec - 1e-9)
        {
            Airborne = false;
            AirT = 0;
            HeightFt = 0;
            return;
        }
        var u = AirT / rules.JumpAirSec;
        HeightFt = 4 * RiseFt * u * (1 - u);
    }

    /// <summary>Takeoff, with no added startup: the airborne clock starts now.</summary>
    bool Takeoff(double riseFt)
    {
        Airborne = true;
        AirT = 0;
        HeightFt = 0;
        RiseFt = riseFt;
        return true;
    }

    /// <summary>The root rise <paramref name="airT"/> seconds into a jump of peak <paramref name="riseFt"/> over <paramref name="airSec"/>.</summary>
    public static double HeightAt(double airT, double airSec, double riseFt)
    {
        if (airT <= 0 || airT >= airSec) return 0;
        var u = airT / airSec;
        return 4 * riseFt * u * (1 - u);
    }
}
