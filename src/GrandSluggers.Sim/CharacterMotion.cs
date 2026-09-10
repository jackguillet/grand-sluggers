namespace GrandSluggers.Sim;

/// <summary>
/// Limb motion for a unique character package. Eulers are <b>local flexion</b>
/// on the bone (X = swing along the authored rest pose), then applied as
/// bind * Q(e). Shared MoveBones / clip FBX assume Rio's axes and must not
/// drive a unique rest pose. Authored clips on THIS armature still win.
/// </summary>
public static class CharacterMotion
{
    public static MoveBones.Sample Evaluate(MoveBones.Verb verb, double t, double poseT, double charge = 0)
    {
        charge = Math.Clamp(charge, 0, 1);
        return verb switch
        {
            MoveBones.Verb.Walk => Gait(t, 1.6, 22, 16, 18),
            MoveBones.Verb.Run => Gait(t, MoveBones.RunHz, 42, 28, 36),
            MoveBones.Verb.Jump => Jump(poseT),
            MoveBones.Verb.ChargePitch => ChargePitch(charge),
            MoveBones.Verb.Pitch => Pitch(poseT),
            MoveBones.Verb.ChargeSwing => ChargeSwing(charge),
            MoveBones.Verb.Swing => Swing(poseT),
            MoveBones.Verb.Throw => Throw(poseT),
            MoveBones.Verb.Scoop => Scoop(poseT),
            MoveBones.Verb.Slide => Slide(poseT),
            _ => Idle(t)
        };
    }

    static MoveBones.Euler E(double x, double y = 0, double z = 0) => new(x, y, z);

    static MoveBones.Sample Idle(double t)
    {
        var breathe = 3 * Math.Sin(t * 2.2);
        var look = 7 * Math.Sin(t * 1.35);
        return new(
            E(breathe), E(0, look),
            E(8), E(6), E(8), E(6),
            E(4), E(6), E(4), E(6),
            E(0, 0, 12), 0);
    }

    static MoveBones.Sample Gait(double t, double hz, double thigh, double shin, double arm)
    {
        var s = Math.Sin(t * hz * Math.PI * 2);
        return new(
            E(6), E(0, 4 * s),
            E(-arm * s, 0, 10), E(8),
            E(arm * s, 0, -10), E(8),
            E(thigh * s), E(Math.Max(0, shin * s)),
            E(-thigh * s), E(Math.Max(0, -shin * s)),
            E(0), 0);
    }

    static MoveBones.Sample Jump(double poseT)
    {
        var u = Math.Clamp(poseT / MoveBones.JumpDur, 0, 1);
        var lift = Math.Sin(Math.PI * u);
        return new(
            E(-8 * lift), E(0),
            E(-28 * lift), E(-12), E(-28 * lift), E(-12),
            E(18 * lift), E(-8), E(18 * lift), E(-8),
            E(0), MoveBones.JumpLift(poseT));
    }

    static MoveBones.Sample ChargeSwing(double charge)
    {
        var c = charge;
        return new(
            E(-6 * c, 8 * c), E(0, 6 * c),
            E(-12 - 18 * c, 18, 24), E(22 + 18 * c),
            E(-18 - 24 * c, -28, -32), E(32 + 20 * c),
            E(4), E(6), E(4), E(6),
            E(7.35, -7.27, -158.19), 0);
    }

    static MoveBones.Sample Swing(double poseT)
    {
        var beat = MoveBones.SwingAt(poseT);
        var (lux, lfx, rux, rfx) = beat switch
        {
            MoveBones.SwingBeat.Load => (-30.0, 40.0, -42.0, 52.0),
            MoveBones.SwingBeat.Contact => (38.0, 58.0, 34.0, 48.0),
            _ => (28.0, 34.0, 18.0, 30.0)
        };
        return new(
            E(-4, beat == MoveBones.SwingBeat.Contact ? 10 : 4), E(0, 4),
            E(lux, -18, 20), E(lfx),
            E(rux, 24, -22), E(rfx),
            E(6), E(8), E(6), E(8),
            beat switch
            {
                MoveBones.SwingBeat.Load => E(7.35, -7.27, -158.19),
                MoveBones.SwingBeat.Contact => E(71.06, 34.68, 32.35),
                _ => E(140.33, 12.53, 10.86)
            }, 0);
    }

    static MoveBones.Sample ChargePitch(double charge)
    {
        var c = charge;
        return new(
            E(-8 * c), E(0, -6 * c),
            E(10), E(8),
            E(-40 * c), E(-20 * c),
            E(4), E(6), E(4), E(6),
            E(0), 0);
    }

    static MoveBones.Sample Pitch(double poseT)
    {
        var beat = MoveBones.PitchAt(poseT);
        var (ux, fx) = beat switch
        {
            MoveBones.PitchBeat.Windup => (-42.0, -18.0),
            MoveBones.PitchBeat.Release => (38.0, 12.0),
            _ => (12.0, 6.0)
        };
        return new(
            E(beat == MoveBones.PitchBeat.Release ? 6 : -4), E(0),
            E(8), E(6),
            E(ux), E(fx),
            E(6), E(8), E(6), E(8),
            E(0), 0);
    }

    static MoveBones.Sample Throw(double poseT)
    {
        var u = Math.Clamp(poseT / 0.35, 0, 1);
        var ux = -30 + 70 * u;
        return new(
            E(4), E(0, 6),
            E(8), E(6),
            E(ux), E(10),
            E(4), E(6), E(4), E(6),
            E(0), 0);
    }

    static MoveBones.Sample Scoop(double poseT)
    {
        var u = Math.Clamp(poseT / 0.4, 0, 1);
        var down = 28 * Math.Sin(Math.PI * u);
        return new(
            E(12 * u), E(8),
            E(down), E(18), E(down), E(18),
            E(16), E(22), E(16), E(22),
            E(0), 0);
    }

    static MoveBones.Sample Slide(double poseT)
    {
        var u = Math.Clamp(poseT / 0.35, 0, 1);
        return new(
            E(18 * u), E(0),
            E(20), E(12), E(20), E(12),
            E(40 * u), E(20), E(-8), E(10),
            E(0), 0);
    }
}
