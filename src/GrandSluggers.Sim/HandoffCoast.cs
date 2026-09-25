namespace GrandSluggers.Sim;

/// <summary>
/// The hand-off coast (§8.9, #718): the body the ring leaves keeps the glove's last velocity for <c>chase.handoffCoastSec</c>, then
/// brakes rather than stopping dead. It samples the glove's velocity every frame, owns the coasting body and its clock, and the
/// brake that follows the coast's last step. <see cref="LivePlaySystem"/> moves the body, and brakes it through the response law.
/// </summary>
public sealed class HandoffCoast
{
    (string Pos, double X, double Z) _gloveLast = ("", 0, 0);
    double _lastDt;
    (double X, double Z) _gloveVel;
    string _pos = "";
    (double X, double Z) _vel;
    double _t;
    string _brakePos = "";
    double _brakeLeft = -1;

    public void Reset()
    {
        _gloveLast = ("", 0, 0);
        _lastDt = 0;
        _gloveVel = (0, 0);
        _pos = "";
        _vel = (0, 0);
        _t = 0;
        _brakePos = "";
        _brakeLeft = -1;
    }

    /// <summary>The glove's own velocity over the last frame, sampled at the top of this one: what a body keeps when the ring leaves it.</summary>
    public void Sample(string glovePos, double gloveX, double gloveZ, double dt)
    {
        _gloveVel = _gloveLast.Pos == glovePos && _lastDt > 0
            ? ((gloveX - _gloveLast.X) / _lastDt, (gloveZ - _gloveLast.Z) / _lastDt)
            : (0, 0);
        _gloveLast = (glovePos, gloveX, gloveZ);
        _lastDt = dt;
    }

    /// <summary>The ring leaves the glove at <paramref name="pos"/>: a body that was moving coasts on for <paramref name="coastSec"/>.</summary>
    public void Leave(string pos, double coastSec)
    {
        if (_gloveVel.X == 0 && _gloveVel.Z == 0) return;
        _pos = pos;
        _vel = _gloveVel;
        _t = coastSec;
    }

    /// <summary>The body at <paramref name="pos"/> is coasting: the walks leave it alone.</summary>
    public bool Coasting(string pos) => _t > 0 && pos == _pos;

    /// <summary>
    /// One frame of the coast: the coasting body, its velocity, and the seconds of this frame it moves on it. Null when nothing
    /// coasts, or when the coasting body has the ring again or has left the field (the coast ends). On the coast's last step,
    /// what it left of the frame goes to the brake.
    /// </summary>
    public (string Pos, (double X, double Z) Vel, double Step)? Step(double dt, string glovePos, Func<string, bool> onField)
    {
        if (_t <= 0) return null;
        if (string.IsNullOrEmpty(_pos) || _pos == glovePos || !onField(_pos))
        {
            _t = 0;
            return null;
        }
        var step = Math.Min(dt, _t);
        _t -= dt;
        var coast = (_pos, _vel, step);
        if (_t <= 1e-9)
        {
            // The coast's last step: what it left of the frame is the brake's, and so is every frame after that no walk takes.
            _t = 0;
            _brakePos = _pos;
            _brakeLeft = dt - step;
        }
        return coast;
    }

    /// <summary>
    /// The brake after the coast, this frame: the body, the seconds it brakes, and whether this is the coast's last frame (whose
    /// step record is the coast's own mark). Null when no body brakes out of a coast.
    /// </summary>
    public (string Pos, double Left, bool Ending)? Brake(double dt)
    {
        if (_brakePos.Length == 0) return null;
        var ending = _brakeLeft >= 0;
        var left = ending ? _brakeLeft : dt;
        _brakeLeft = -1;
        return (_brakePos, left, ending);
    }

    /// <summary>The body is walked, has the ring, or has stopped: its brake out of the coast is over.</summary>
    public void EndBrake() => _brakePos = "";
}
