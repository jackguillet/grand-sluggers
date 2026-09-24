namespace GrandSluggers.Sim;

/// <summary>
/// How a fielding body shows what the ball cost it (<c>feel.fieldTells</c>, the Unity pass of #719–#721). The sim decides every
/// debt; these two numbers only say how long the get-up reads and how hard the brace squashes.
/// </summary>
public sealed record FieldTellsFeel
{
    /// <summary>The last seconds of a dive's recovery draw the get-up (the crouch take); the rest of it holds the laid-out dive.</summary>
    public double DiveGetUpSec { get; init; }

    /// <summary>The impact recoil's brace (#720): the body wrapper's squash at a full-severity take, eased out over the recovery.</summary>
    public double BraceSquash { get; init; }

    public void Validate()
    {
        if (!double.IsFinite(DiveGetUpSec) || !double.IsFinite(BraceSquash)
            || DiveGetUpSec < 0 || BraceSquash < 0 || BraceSquash >= 0.5)
            throw new InvalidDataException("feel.fieldTells needs diveGetUpSec >= 0 and braceSquash in [0, 0.5).");
    }
}

/// <summary>
/// What a fielding body owes after the ball met it, as the client draws it (the Unity pass of #719, #720, #721). The live ball
/// binds each debt to a body — the fumble's stun (<see cref="LivePlaySystem.StunPos"/>), the dive's recovery
/// (<see cref="LivePlaySystem.DivingPos"/>), the impact brace and the jump's airtime (the glove) — and the ring can leave that
/// body while it pays. So the director asks per body, not per ring. Nothing here holds a pose (docs/character-motion.md): it
/// picks which take a debt plays, the root rise the sim computed, and the squash on the presentation wrapper.
/// </summary>
public static class FielderTells
{
    /// <summary>
    /// One frame of what the live ball owes, mirrored for the actor director; <see cref="None"/> between plays. It carries its
    /// own clocks so the client can <see cref="Aged">age</see> it past the play's completing frame, which resets the live
    /// field: a diving catch that ends the play still gets up, a jumping one still lands.
    /// </summary>
    public readonly record struct Owed(
        string GlovePos,
        string StunPos,
        double StunSec,
        double FumbleSec,
        string DivingPos,
        double DiveRecoverySec,
        bool Bracing,
        double BraceSec,
        double BraceDurSec,
        bool Airborne,
        double AirSec,
        double AirTotalSec,
        double RiseTopFt)
    {
        public static Owed None { get; } = new("", "", 0, 0, "", 0, false, 0, 0, false, 0, 0, 0);

        /// <summary>This frame's debts on <paramref name="live"/>, with the jump's arc from <paramref name="rules"/> (the live ball's own table).</summary>
        public static Owed Of(LivePlaySystem live, RulesTable rules) => new(
            live.GlovePos,
            live.StunPos,
            live.StunT,
            live.Bobbling ? live.RecoilT : 0,
            live.DivingPos,
            live.DiveRecoveryT,
            live.ImpactRecoil && live.RecoilT > 0,
            live.RecoilT,
            live.RecoilDur,
            live.Airborne,
            live.JumpAirT,
            rules.Fielding.Catch.JumpAirSec,
            rules.Fielding.Catch.JumpRiseFt);

        /// <summary>The same debts <paramref name="dt"/> seconds on, with no live ball to extend them: every clock runs out and the jumper lands.</summary>
        public Owed Aged(double dt)
        {
            if (dt <= 0) return this;
            var stun = Math.Max(0, StunSec - dt);
            var dive = Math.Max(0, DiveRecoverySec - dt);
            var brace = Math.Max(0, BraceSec - dt);
            var air = AirSec + dt;
            var airborne = Airborne && air < AirTotalSec - 1e-9;
            return this with
            {
                StunSec = stun,
                StunPos = stun > 0 ? StunPos : "",
                FumbleSec = Math.Max(0, FumbleSec - dt),
                DiveRecoverySec = dive,
                DivingPos = dive > 0 ? DivingPos : "",
                BraceSec = brace,
                Bracing = Bracing && brace > 0,
                Airborne = airborne,
                AirSec = airborne ? air : 0
            };
        }
    }

    /// <summary>
    /// The fumbler (#721): the ordinary bobble's stun on the body that fumbled, wherever the ring went, or the shipped
    /// whole-tick fumble on the glove.
    /// </summary>
    public static bool Stunned(Owed o, string pos) =>
        (o.StunSec > 0 && Same(pos, o.StunPos)) || (o.FumbleSec > 0 && Same(pos, o.GlovePos));

    /// <summary>The diver paying its recovery (#719), caught or missed, wherever the ring went.</summary>
    public static bool Recovering(Owed o, string pos) => o.DiveRecoverySec > 0 && Same(pos, o.DivingPos);

    /// <summary>The glove in the air on a normal jump (#719): never on the shipped table.</summary>
    public static bool InTheAir(Owed o, string pos) => o.Airborne && Same(pos, o.GlovePos);

    /// <summary>The glove bracing on a hard ball (#720): the ordinary impact recoil only; the shipped knockback is not a brace.</summary>
    public static bool Braced(Owed o, string pos) => o.Bracing && Same(pos, o.GlovePos);

    /// <summary>
    /// The take a body's debt plays, or null when it owes nothing and the director's own choice stands. The fumbler throws
    /// its arms out (<see cref="Motion.Verb.Spin"/>) — never the batter's miss, which carries the bat; the diver stays laid out
    /// and gets up in the last <see cref="FieldTellsFeel.DiveGetUpSec"/>; the jumper reaches up while the sim lifts the root
    /// (the jump take bakes its own lift, <see cref="Motion.JumpPeak"/> rig units, which the normal jump's two feet replace).
    /// </summary>
    public static Motion.Verb? Verb(Owed o, string pos, FieldTellsFeel feel)
    {
        if (Stunned(o, pos)) return Motion.Verb.Spin;
        if (Recovering(o, pos)) return o.DiveRecoverySec > feel.DiveGetUpSec + 1e-9 ? Motion.Verb.Dive : Motion.Verb.Crouch;
        if (InTheAir(o, pos)) return Motion.Verb.Catch;
        return null;
    }

    /// <summary>
    /// The body's root rise, feet: on the glove in the air, the accepted arc <c>4 H u (1 − u)</c> over the airtime
    /// (F693-02-normal-jump-arc-trial) — what the sim's <see cref="LivePlaySystem.JumpHeightFt"/> reads frame for frame, and
    /// what carries the landing past the completing frame; 0 on the ground.
    /// </summary>
    public static double RiseFt(Owed o, string pos)
    {
        if (!InTheAir(o, pos) || o.AirTotalSec <= 0) return 0;
        var u = Math.Clamp(o.AirSec / o.AirTotalSec, 0, 1);
        return 4 * o.RiseTopFt * u * (1 - u);
    }

    /// <summary>
    /// The brace on the wrapper (#720), as a scale (1, 1, 1 = none): squashed by <see cref="FieldTellsFeel.BraceSquash"/> × the
    /// take's severity at the take and eased back over the recovery. Severity is the recovery's length against
    /// <c>recoil.capSec</c> — the same weight the sim charged, so a harder ball reads harder and a routine one not at all.
    /// </summary>
    public static (double X, double Y, double Z) Brace(Owed o, string pos, double capSec, FieldTellsFeel feel)
    {
        if (!Braced(o, pos) || o.BraceDurSec <= 0 || capSec <= 0) return (1, 1, 1);
        var weight = Math.Clamp(o.BraceDurSec / capSec, 0, 1);
        var left = Math.Clamp(o.BraceSec / o.BraceDurSec, 0, 1);
        var s = feel.BraceSquash * weight * left;
        return (1 + s * 0.5, 1 - s, 1 + s * 0.5);
    }

    static bool Same(string pos, string owner) =>
        !string.IsNullOrEmpty(owner) && string.Equals(pos, owner, StringComparison.OrdinalIgnoreCase);
}
