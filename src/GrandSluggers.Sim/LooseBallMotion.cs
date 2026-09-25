namespace GrandSluggers.Sim;

/// <summary>
/// The ball in nobody's glove and off its batted path (§8.6): a fumble, an overthrow, a drop at an uncovered bag, a local bobble.
/// It owns the ball's velocity, whether it answers to the bobble's response (<see cref="Local"/>: in the air, rebounding, then
/// rolling) or rolls as an overthrow, and when it came to rest. <see cref="LivePlaySystem"/> owns where the ball is, and says
/// who takes it.
/// </summary>
public sealed class LooseBallMotion
{
    double _vx;
    double _vy;
    double _vz;
    bool _air;
    double _restAt = -1;

    /// <summary>The ball is loose.</summary>
    public bool Active { get; private set; }

    /// <summary>This loose ball answers to the local bobble's response, not the overthrow roll.</summary>
    public bool Local { get; private set; }

    public void Reset()
    {
        _vy = 0;
        _air = false;
        Local = false;
        Active = false;
        _vx = _vz = 0;
        _restAt = -1;
    }

    /// <summary>
    /// The ball is loose on the ground at (vx, vz): an overthrow, a drop at the bag, a ball knocked from a foiled glove. A ball
    /// at rest has been at rest since <paramref name="elapsed"/>. The bobble's own state is left as it is.
    /// </summary>
    public void Roll(double vx, double vz, double elapsed)
    {
        Active = true;
        _vx = vx;
        _vz = vz;
        _restAt = vx == 0 && vz == 0 ? elapsed : -1;
    }

    /// <summary>The local bobble: the ball leaves the glove at (vx, vz), falling from the contact when it was above the ground.</summary>
    public void Bobble(double vx, double vz, bool inTheAir)
    {
        Active = true;
        Local = true;
        _air = inTheAir;
        _vx = vx;
        _vz = vz;
        _vy = 0;
        _restAt = -1;
    }

    /// <summary>A glove has the ball.</summary>
    public void Held()
    {
        Active = false;
        _vx = _vz = 0;
        _restAt = -1;
    }

    /// <summary>The loose ball has been at rest for <paramref name="holdSec"/> by <paramref name="elapsed"/>.</summary>
    public bool RestedFor(double elapsed, double holdSec) => _restAt >= 0 && elapsed >= _restAt + holdSec;

    /// <summary>
    /// One frame of the loose ball from (x, y, z): the local bobble's fall, rebound and roll (<see cref="BallFlight.LocalBobbleTick"/>),
    /// or the overthrow's roll to a stop (<see cref="BallFlight.OverthrowTick"/>), each on the ground row of the zone the ball is in.
    /// The new position, or null when a ball at rest did not move.
    /// </summary>
    public (double X, double Y, double Z)? Tick(double x, double y, double z, double dt, double elapsed, GroundZones zones, RulesTable rules)
    {
        if (Local)
        {
            var bob = BallFlight.LocalBobbleTick(zones, rules.Grounds, rules.Fielding.Handling, rules.Flight.Gravity,
                x, y, z, _vx, _vy, _vz, _air, dt);
            (_vx, _vy, _vz, _air) = (bob.VX, bob.VY, bob.VZ, bob.Air);
            if (!_air && _vx == 0 && _vz == 0)
            {
                if (_restAt < 0) _restAt = elapsed;
            }
            else _restAt = -1;
            return (bob.X, bob.Y, bob.Z);
        }
        var speed = Math.Sqrt(_vx * _vx + _vz * _vz);
        if (speed <= 0)
        {
            if (_restAt < 0) _restAt = elapsed;
            return null;
        }
        var step = BallFlight.OverthrowTick(zones, rules.Grounds, x, z, _vx, _vz, dt);
        _vx = step.VX;
        _vz = step.VZ;
        if (step.Speed <= 0) _restAt = elapsed;
        return (step.X, 0, step.Z);
    }
}
