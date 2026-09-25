namespace GrandSluggers.Sim;

/// <summary>
/// The response law (#718, F693-02-carry-movement-response): every fielder's velocity, answered toward what it wants at the ramp
/// and brake rates, and the record of which bodies were stepped this frame so that the rest can brake to a stop.
/// <see cref="LivePlaySystem"/> says what each body wants, its rated speed, the ground under it and whether it is in the air, and
/// moves the body on the velocity returned.
/// </summary>
public sealed class BodyResponse
{
    readonly Dictionary<string, (double X, double Z)> _vel = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> _stepped = new(StringComparer.OrdinalIgnoreCase);

    public void Reset()
    {
        _vel.Clear();
        _stepped.Clear();
    }

    /// <summary>
    /// One frame of a body's velocity toward <paramref name="want"/>: the component along its heading builds at the ramp rate and
    /// dies at the brake rate; the component across it builds at the ramp rate. Rest to <paramref name="top"/> takes
    /// <c>chase.accelSec</c> × the ground's <c>startMul</c>; <paramref name="top"/> to rest takes <c>chase.brakeSec</c> × its
    /// <c>brakeMul</c>; the cut-back takes <c>chase.accelSec</c> × its <c>cutMul</c>. <paramref name="rate"/> scales all three (the
    /// airborne body's fraction). The body is marked stepped this frame.
    /// </summary>
    public (double X, double Z) Respond(string pos, (double X, double Z) want, double top, double rate, GroundBodyRules ground,
        ChaseRules c, double dt)
    {
        var accel = top / (c.AccelSec * ground.StartMul) * rate;
        var brake = top / (c.BrakeSec * ground.BrakeMul) * rate;
        // The cut-back: the component across the heading is corrected at the ramp rate, over the ground's own time for it.
        var cut = top / (c.AccelSec * ground.CutMul) * rate;
        var v = _vel.TryGetValue(pos, out var cur) ? cur : (X: 0.0, Z: 0.0);
        var dvx = want.X - v.X;
        var dvz = want.Z - v.Z;
        var speed = Math.Sqrt(v.X * v.X + v.Z * v.Z);
        double nx, nz;
        if (speed < 1e-9)
        {
            var dv = Math.Sqrt(dvx * dvx + dvz * dvz);
            if (dv < 1e-12) (nx, nz) = want;
            else
            {
                var step = Math.Min(dv, accel * dt);
                (nx, nz) = (v.X + dvx / dv * step, v.Z + dvz / dv * step);
            }
        }
        else
        {
            var ux = v.X / speed;
            var uz = v.Z / speed;
            var along = dvx * ux + dvz * uz;
            var px = dvx - along * ux;
            var pz = dvz - along * uz;
            var across = Math.Sqrt(px * px + pz * pz);
            var alongStep = Math.Clamp(along, -brake * dt, accel * dt);
            var acrossStep = across > 1e-12 ? Math.Min(across, cut * dt) / across : 0;
            nx = v.X + alongStep * ux + px * acrossStep;
            nz = v.Z + alongStep * uz + pz * acrossStep;
        }
        _vel[pos] = (nx, nz);
        _stepped.Add(pos);
        return (nx, nz);
    }

    /// <summary>A body carried at <paramref name="v"/> this frame without a want (the hand-off coast): its velocity is that, and it was stepped.</summary>
    public void Carry(string pos, (double X, double Z) v)
    {
        _vel[pos] = v;
        _stepped.Add(pos);
    }

    /// <summary>The body was stepped this frame.</summary>
    public bool Stepped(string pos) => _stepped.Contains(pos);

    /// <summary>The body's velocity, when it has one.</summary>
    public bool TryVelocity(string pos, out (double X, double Z) v) => _vel.TryGetValue(pos, out v);

    /// <summary>
    /// The top of a frame: the bodies nobody stepped last frame and not <paramref name="exempt"/> (a coasting body), which brake
    /// this frame, in the order their velocities were first taken. The step record starts over.
    /// </summary>
    public List<string> Idle(Func<string, bool> exempt)
    {
        var idle = _vel.Keys.Where(pos => !_stepped.Contains(pos) && !exempt(pos)).ToList();
        _stepped.Clear();
        return idle;
    }

    /// <summary>A body at rest, or off the field, has no velocity to carry.</summary>
    public void Forget(string pos) => _vel.Remove(pos);

    /// <summary>The idle bodies' brakes are not this frame's steps: the step record starts over again after them.</summary>
    public void EndIdle() => _stepped.Clear();
}
