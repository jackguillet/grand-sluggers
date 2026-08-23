namespace GrandSluggers.Sim;

/// <summary>
/// Shared movement bones: parented joint Euler (degrees) and jump lift vs time.
/// Unity applies these; xunit samples them. Same chain for every captain.
/// </summary>
public static class MoveBones
{
    public const double RunHz = 2.55;
    public const double JumpDur = 0.55;
    public const double JumpPeak = 4.2;
    public const double SwingDur = 0.50;
    public const double PitchDur = 0.50;

    public enum Verb
    {
        Idle, Walk, Run, Jump,
        ChargePitch, Pitch,
        ChargeSwing, Swing,
        Throw, Scoop, Slide
    }

    public enum ClipEvent { Contact, Release, FootPlant }

    public readonly record struct Clip(string Id, Verb Verb, ClipEvent[] Marks);

    /// <summary>One shared clip list. Captains are skins on this chain.</summary>
    public static readonly IReadOnlyList<Clip> ClipList =
    [
        new("idle", Verb.Idle, []),
        new("walk", Verb.Walk, [ClipEvent.FootPlant]),
        new("run", Verb.Run, [ClipEvent.FootPlant]),
        new("jump", Verb.Jump, [ClipEvent.FootPlant]),
        new("swing", Verb.Swing, [ClipEvent.Contact]),
        new("pitch", Verb.Pitch, [ClipEvent.Release]),
        new("scoop", Verb.Scoop, [ClipEvent.Contact]),
        new("slide", Verb.Slide, [ClipEvent.FootPlant]),
        new("throw", Verb.Throw, [ClipEvent.Release])
    ];

    public static IReadOnlyList<string> Clips { get; } =
        ClipList.Select(c => c.Id).ToArray();

    public readonly record struct Euler(double X, double Y, double Z);

    public readonly record struct Sample(
        Euler Torso,
        Euler Head,
        Euler LUpper, Euler LFore,
        Euler RUpper, Euler RFore,
        Euler LThigh, Euler LShin,
        Euler RThigh, Euler RShin,
        Euler Bat,
        double Lift);

    public static double RunPhase(double t)
    {
        var p = t * RunHz;
        p -= Math.Floor(p);
        if (p < 0) p += 1;
        return p;
    }

    public static double JumpLift(double poseT)
    {
        var u = Math.Clamp(poseT / JumpDur, 0, 1);
        return JumpPeak * Math.Sin(Math.PI * u);
    }

    public static Sample MirrorArms(Sample s) => s with
    {
        LUpper = Flip(s.RUpper), RUpper = Flip(s.LUpper),
        LFore = Flip(s.RFore), RFore = Flip(s.LFore)
    };

    public static Sample Evaluate(Verb verb, double t, double poseT, double charge = 0, string? pitchType = null)
    {
        charge = Math.Clamp(charge, 0, 1);
        return verb switch
        {
            Verb.Walk => Walk(t),
            Verb.Run => Run(t),
            Verb.Jump => Jump(poseT),
            Verb.ChargePitch => ChargePitch(charge, pitchType),
            Verb.Pitch => Pitch(poseT, pitchType),
            Verb.ChargeSwing => ChargeSwing(charge),
            Verb.Swing => Swing(poseT),
            Verb.Throw => Throw(poseT),
            Verb.Scoop => Scoop(poseT),
            Verb.Slide => Slide(poseT),
            _ => Idle()
        };
    }

    public static Sample Idle() =>
        Pose(
            torso: E(4, 0, 0),
            lUpper: E(12, 0, 16), rUpper: E(12, 0, -16),
            lFore: E(8, 0, 0), rFore: E(8, 0, 0),
            lThigh: E(6, 0, 0), rThigh: E(6, 0, 0),
            lShin: E(8, 0, 0), rShin: E(8, 0, 0));

    static Sample Walk(double t)
    {
        var u = RunPhase(t * 0.55 / RunHz);
        return Stride(u, 0.45);
    }

    static Sample Run(double t) => Stride(RunPhase(t), 1.0);

    /// <summary>
    /// One gait. Phase 0 = left plant (left thigh forward, right arm forward).
    /// Opposite arm vs plant leg. Shin flexes on the passing side.
    /// </summary>
    static Sample Stride(double phase, double amp)
    {
        var plantL = Wave(phase);
        var plantR = Wave(phase + 0.5);
        var lThigh = 8 + 50 * amp * plantL;
        var rThigh = 8 + 50 * amp * plantR;
        var lShin = 12 + 44 * amp * Math.Max(0, -plantL);
        var rShin = 12 + 44 * amp * Math.Max(0, -plantR);
        var lArm = -8 - 58 * amp * plantL;
        var rArm = -8 - 58 * amp * plantR;
        var lFore = 18 + 20 * amp * Math.Max(0, -plantL);
        var rFore = 18 + 20 * amp * Math.Max(0, -plantR);
        var yaw = 10 * amp * plantL;
        var lean = 8 + 8 * amp;
        return Pose(
            torso: E(lean, yaw, 0),
            lUpper: E(lArm, 6 * plantL, 8),
            rUpper: E(rArm, -6 * plantR, -8),
            lFore: E(lFore, 0, 0),
            rFore: E(rFore, 0, 0),
            lThigh: E(lThigh, 0, 0),
            rThigh: E(rThigh, 0, 0),
            lShin: E(lShin, 0, 0),
            rShin: E(rShin, 0, 0),
            lift: 0.12 * amp * Math.Abs(plantL));
    }

    static Sample Jump(double poseT)
    {
        var u = Math.Clamp(poseT / JumpDur, 0, 1);
        var take = Smooth(Math.Clamp(u / 0.28, 0, 1));
        var hang = Smooth(Math.Clamp((u - 0.22) / 0.28, 0, 1));
        var land = Smooth(Math.Clamp((u - 0.62) / 0.38, 0, 1));
        var coil = Lerp(55, 18, take);
        var air = Lerp(coil, 8, hang);
        var thigh = Lerp(air, 42, land);
        var shin = Lerp(Lerp(70, 20, take), Lerp(12, 50, land), hang * 0.2 + land);
        var arms = Lerp(-20, -85, take);
        arms = Lerp(arms, -40, land);
        return Pose(
            torso: E(Lerp(18, 4, hang) + 10 * land, 0, 0),
            lUpper: E(arms, 0, 12), rUpper: E(arms, 0, -12),
            lFore: E(20, 0, 0), rFore: E(20, 0, 0),
            lThigh: E(thigh, 6, 0), rThigh: E(thigh, -6, 0),
            lShin: E(shin, 0, 0), rShin: E(shin, 0, 0),
            lift: JumpLift(poseT));
    }

    static Sample ChargePitch(double charge, string? type)
    {
        var arm = Slot(-40 - 72 * charge, 16, -36, type);
        return Pose(
            torso: E(-10 - 16 * charge, 12, 0),
            lUpper: E(14, 0, 26), rUpper: arm,
            lFore: E(22, 0, 0), rFore: E(28 + 10 * charge, 0, 0),
            lThigh: E(10 + 28 * charge, 0, 0),
            rThigh: E(-8 * charge, 0, 0),
            lShin: E(18 + 20 * charge, 0, 0),
            rShin: E(10, 0, 0));
    }

    static Sample Pitch(double poseT, string? type)
    {
        var u = Math.Clamp(poseT / PitchDur, 0, 1);
        var wind = Smooth(Math.Clamp(u / 0.22, 0, 1));
        var stride = Smooth(Math.Clamp((u - 0.18) / 0.22, 0, 1));
        var rel = Smooth(Math.Clamp((u - 0.38) / 0.18, 0, 1));
        var fol = Smooth(Math.Clamp((u - 0.56) / 0.28, 0, 1));
        var back = Slot(-108, 18, -38, type);
        var slot = Slot(8, 6, -18, type);
        var outA = Slot(82, -12, -8, type);
        var wrap = Slot(112, -22, 10, type);
        Euler arm;
        if (u < 0.22) arm = Le(back, back, wind);
        else if (u < 0.40) arm = Le(back, slot, stride);
        else if (u < 0.58) arm = Le(slot, outA, rel);
        else arm = Le(outA, wrap, fol);
        var lThigh = Lerp(12, 48, stride);
        lThigh = Lerp(lThigh, 22, fol);
        var rThigh = Lerp(-6, 18, rel);
        var torsoY = Lerp(14, -22, rel);
        var torsoX = Lerp(-16, 14, rel);
        return Pose(
            torso: E(torsoX, torsoY, 0),
            lUpper: E(16 + 18 * stride, 0, 24),
            rUpper: arm,
            lFore: E(20, 0, 0),
            rFore: E(Lerp(32, 8, rel), 0, 0),
            lThigh: E(lThigh, 0, 0),
            rThigh: E(rThigh, 0, 0),
            lShin: E(Lerp(28, 12, stride), 0, 0),
            rShin: E(Lerp(14, 22, rel), 0, 0));
    }

    static Sample ChargeSwing(double charge) =>
        Pose(
            torso: E(2, -10 - 8 * charge, 0),
            lUpper: E(-12, 18, 28),
            rUpper: E(-38 - 48 * charge, -42, -52),
            lFore: E(16, 0, 0), rFore: E(22, 0, 0),
            lThigh: E(10, 0, 0), rThigh: E(-6, 8, 0),
            lShin: E(16, 0, 0), rShin: E(12, 0, 0),
            bat: E(78 + 30 * charge, 4, 8));

    static Sample Swing(double poseT)
    {
        var u = Math.Clamp(poseT / SwingDur, 0, 1);
        var load = Key(
            torso: E(2, -12, 0),
            lUpper: E(-12, 18, 28), rUpper: E(-82, -42, -52),
            lFore: E(14, 0, 0), rFore: E(18, 0, 0),
            lThigh: E(8, 0, 0), rThigh: E(-4, 6, 0),
            lShin: E(14, 0, 0), rShin: E(10, 0, 0),
            bat: E(100, 6, 8));
        var hips = Key(
            torso: E(8, 22, -4),
            lUpper: E(-4, 8, 22), rUpper: E(-48, -18, -28),
            lFore: E(20, 0, 0), rFore: E(24, 0, 0),
            lThigh: E(16, 0, 0), rThigh: E(-12, 14, 0),
            lShin: E(18, 0, 0), rShin: E(16, 0, 0),
            bat: E(48, 36, 10));
        var cut = Key(
            torso: E(12, 58, -8),
            lUpper: E(26, -46, 8), rUpper: E(24, 74, 26),
            lFore: E(28, 0, 0), rFore: E(12, 0, 0),
            lThigh: E(14, 0, 0), rThigh: E(-16, 18, 0),
            lShin: E(16, 0, 0), rShin: E(20, 0, 0),
            bat: E(-52, 112, 12));
        var wrap = Key(
            torso: E(8, 82, -12),
            lUpper: E(42, -72, -8), rUpper: E(10, 98, 38),
            lFore: E(18, 0, 0), rFore: E(8, 0, 0),
            lThigh: E(10, 0, 0), rThigh: E(-10, 16, 0),
            lShin: E(14, 0, 0), rShin: E(18, 0, 0),
            bat: E(-68, 158, 18),
            head: E(10, 20, 0));
        if (u < 0.22) return Mix(load, hips, Smooth(u / 0.22));
        if (u < 0.48) return Mix(hips, cut, Smooth((u - 0.22) / 0.26));
        return Mix(cut, wrap, Smooth(Math.Clamp((u - 0.48) / 0.36, 0, 1)));
    }

    static Sample Scoop(double poseT)
    {
        var drop = Smooth(Math.Clamp(poseT / 0.12, 0, 1));
        var pick = Smooth(Math.Clamp((poseT - 0.12) / 0.16, 0, 1));
        var up = Smooth(Math.Clamp((poseT - 0.28) / 0.16, 0, 1));
        var reach = Math.Max(drop, pick);
        var lUpper = Le(E(12, 0, 18), Le(E(62, 8, 10), E(28, 0, 16), up), reach);
        var rUpper = Le(E(12, 0, -18), Le(E(70, -12, -8), E(22, 0, -16), up), reach);
        return Pose(
            torso: E(Lerp(8, 42, drop) - 22 * up, 0, 0),
            lUpper: lUpper, rUpper: rUpper,
            lFore: E(18, 0, 0), rFore: E(22, 0, 0),
            lThigh: E(38 + 18 * drop - 12 * up, 8, 0),
            rThigh: E(28 + 22 * drop - 10 * up, -6, 0),
            lShin: E(28 + 16 * drop, 0, 0),
            rShin: E(22 + 18 * drop, 0, 0));
    }

    static Sample Slide(double poseT)
    {
        var tuck = Smooth(Math.Clamp(poseT / 0.18, 0, 1));
        var pop = Smooth(Math.Clamp((poseT - 0.18) / 0.22, 0, 1));
        var lThigh = Le(E(20, 0, 0), Le(E(88, 10, 12), E(42, 6, 6), pop), tuck);
        var rThigh = Le(E(12, 0, 0), Le(E(102, -8, -10), E(28, -4, -6), pop), tuck);
        return Pose(
            torso: E(Lerp(12, 62, tuck) - 28 * pop, 0, 0),
            lUpper: Le(E(10, 8, 18), E(-28, 12, 22), tuck),
            rUpper: Le(E(10, -8, -18), E(-48, -18, -12), tuck),
            lFore: E(14, 0, 0), rFore: E(16, 0, 0),
            lThigh: lThigh, rThigh: rThigh,
            lShin: E(18 + 40 * tuck - 20 * pop, 0, 0),
            rShin: E(14 + 48 * tuck - 24 * pop, 0, 0));
    }

    static Sample Throw(double poseT)
    {
        var u = Math.Clamp(poseT / 0.40, 0, 1);
        var dirt = Slot(52, 10, -14, null);
        var whip = Slot(78, -8, -16, null);
        var follow = Slot(112, -18, 8, null);
        Euler arm;
        if (u < 0.45) arm = Le(dirt, whip, Smooth(u / 0.45));
        else arm = Le(whip, follow, Smooth((u - 0.45) / 0.55));
        return Pose(
            torso: E(16 - 8 * u, -20 * u, 0),
            lUpper: E(30 - 6 * u, 0, 20),
            rUpper: arm,
            lFore: E(16, 0, 0), rFore: E(Lerp(28, 6, u), 0, 0),
            lThigh: E(26 - 10 * u, 0, 0), rThigh: E(10, 0, 0),
            lShin: E(22, 0, 0), rShin: E(14, 0, 0));
    }

    static Euler Slot(double x, double y, double z, string? type) => type switch
    {
        "curve" => E(x - 22, y, z - 10),
        "slider" => E(x + 24, y + 16, z + 8),
        _ => E(x, y, z)
    };

    static Sample Pose(
        Euler? torso = null, Euler? head = null,
        Euler? lUpper = null, Euler? lFore = null,
        Euler? rUpper = null, Euler? rFore = null,
        Euler? lThigh = null, Euler? lShin = null,
        Euler? rThigh = null, Euler? rShin = null,
        Euler? bat = null, double lift = 0) =>
        new(
            torso ?? default, head ?? default,
            lUpper ?? default, lFore ?? default,
            rUpper ?? default, rFore ?? default,
            lThigh ?? default, lShin ?? default,
            rThigh ?? default, rShin ?? default,
            bat ?? E(0, 0, 20), lift);

    static Sample Key(
        Euler torso, Euler lUpper, Euler rUpper, Euler lFore, Euler rFore,
        Euler lThigh, Euler rThigh, Euler lShin, Euler rShin, Euler bat, Euler? head = null) =>
        new(torso, head ?? default, lUpper, lFore, rUpper, rFore, lThigh, lShin, rThigh, rShin, bat, 0);

    static Sample Mix(Sample a, Sample b, double u) =>
        new(
            Le(a.Torso, b.Torso, u), Le(a.Head, b.Head, u),
            Le(a.LUpper, b.LUpper, u), Le(a.LFore, b.LFore, u),
            Le(a.RUpper, b.RUpper, u), Le(a.RFore, b.RFore, u),
            Le(a.LThigh, b.LThigh, u), Le(a.LShin, b.LShin, u),
            Le(a.RThigh, b.RThigh, u), Le(a.RShin, b.RShin, u),
            Le(a.Bat, b.Bat, u),
            Lerp(a.Lift, b.Lift, u));

    static Euler Flip(Euler e) => new(e.X, -e.Y, -e.Z);

    static Euler E(double x, double y, double z) => new(x, y, z);

    static Euler Le(Euler a, Euler b, double u) =>
        new(Lerp(a.X, b.X, u), Lerp(a.Y, b.Y, u), Lerp(a.Z, b.Z, u));

    static double Lerp(double a, double b, double u) => a + (b - a) * u;

    static double Smooth(double u)
    {
        u = Math.Clamp(u, 0, 1);
        return u * u * (3 - 2 * u);
    }

    /// <summary>+1 at phase 0 (left plant), −1 at 0.5. Flattened peaks, not a raw sine.</summary>
    static double Wave(double phase)
    {
        phase -= Math.Floor(phase);
        var s = Math.Cos(phase * Math.PI * 2);
        return Math.Sign(s) * Math.Pow(Math.Abs(s), 0.65);
    }
}
