namespace GrandSluggers.Sim;

/// <summary>
/// The pitch as a readable object (spec §4.2, §4.3): two shapes — fastball flies true with a
/// mild hump, changeup hangs then dumps below the fastball's height — plus the stick's break
/// after release and the rubber walk. u=0 at the release, u=1 at the plate. One function,
/// <see cref="Point(PitchCommand, double, string?, ValueTuple{double, double, double}?, RulesTable?)"/>,
/// gives the ball at any u; the aim tell, the ball, the umpire, and the CPU batter all read
/// its u=1 sample (<see cref="Crossing"/>). Every shape number is pitching.json.
/// </summary>
public static class PitchFlight
{
    // Plate frame geometry: the normalized aim square and where it sits. Presentation, the
    // strike frame, and the umpire all share it, so it stays a constant like Diamond.
    public const double MoundZ = Diamond.Mound;
    /// <summary>The natural crossing height: a normal or charged pitch crosses mid-zone (spec §4.2).</summary>
    public const double PlateY = StrikeZoneGeometry.CenterY;
    public const double PlateScaleX = 1.85;
    public const double PlateScaleY = 1.35;

    public static (double X, double Y) PlateTarget(double aimX, double aimY) =>
        (aimX * PlateScaleX, PlateY + aimY * PlateScaleY);

    /// <summary>Throwing hand, not the torso (pitching.flight.releaseHand*). +X toward first from a RHP. The rubber walk moves it by <see cref="HomeSet.PitcherWalk"/> per unit.</summary>
    public static (double X, double Y, double Z) Release(double rubberX = 0, RulesTable? rules = null)
    {
        var f = Rules.Or(rules).Pitching.Flight;
        return (rubberX * HomeSet.PitcherWalk + f.ReleaseHandX, f.ReleaseHandY, MoundZ - f.ReleaseTowardPlate);
    }

    /// <summary>
    /// Super Sluggers air time, not MLB 90 (pitching.flight.arcadeScale, airMin/MaxSec). Charged FB still beats a changeup.
    /// </summary>
    public static double AirSeconds(double mph, RulesTable? rules = null)
    {
        var f = Rules.Or(rules).Pitching.Flight;
        var real = Diamond.Mound / (Math.Max(f.MinMph, mph) * 1.4667);
        return Math.Clamp(real * f.ArcadeScale, f.AirMinSec, f.AirMaxSec);
    }

    /// <summary>
    /// The ball at u for a shape. <paramref name="breakX"/> is the stick (−1..1); a charged
    /// pitch or a changeup takes <c>breakDampedMul</c> of it (spec §4.1). The rubber walk moves
    /// the crossing by the same world distance as the body, once (spec §4.2).
    /// </summary>
    public static (double X, double Y, double Z) Point(
        string type, double u, double aimX = 0, double aimY = 0,
        double breakX = 0, bool changeup = false, double rubberX = 0,
        (double X, double Y, double Z)? from = null, RulesTable? rules = null, bool charged = false)
    {
        var r = Rules.Or(rules);
        var f = r.Pitching.Flight;
        var sh = r.Pitching.Shapes;
        u = Math.Clamp(u, 0, 1);
        var liveChange = changeup || type == "changeup";
        var (tx, ty) = PlateTarget(aimX, aimY);
        tx += rubberX * HomeSet.PitcherWalk;
        if (liveChange) ty -= sh.ChangeupDropFt;
        var rel = from ?? Release(rubberX, r);
        var z = rel.Z * (1 - u);
        var (x, y, zz) = liveChange
            ? Changeup(u, tx, ty, z, rel, sh)
            : Fastball(u, tx, ty, z, rel, sh);
        x += BreakShiftFt(u, breakX, charged || liveChange, f);
        return (x, y, zz);
    }

    /// <summary>The same delivered ball is used by rendering, contact and the umpire.</summary>
    public static (double X, double Y, double Z) Point(PitchCommand pitch, double u,
        string? starPitchId = null, (double X, double Y, double Z)? from = null, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        u = Math.Clamp(u, 0, 1);
        var p = Point(pitch.Type, u, pitch.AimX, pitch.AimY, pitch.BreakX,
            pitch.Changeup, pitch.RubberX, from, r, ChargeFeel.IsCharge(pitch.Charge01));
        if (!pitch.Star) return p;
        var st = r.Pitching.StarShapes;
        return starPitchId switch
        {
            "heatball" => (p.X + Math.Sin(u * st.HeatballWobbleHz) * st.HeatballWobbleFt, p.Y, p.Z),
            "prismball" => (p.X + Math.Sin(u * st.PrismballWobbleHz) * st.PrismballWobbleFt, p.Y, p.Z),
            "charmball" => (p.X + Math.Sin(u * st.CharmballWobbleHz) * st.CharmballWobbleFt, p.Y, p.Z),
            "phonyball" => (p.X + (u > st.PhonyballSwitchAt ? st.PhonyballLateX : st.PhonyballEarlyX), p.Y, p.Z),
            "caskball" => (p.X, p.Y + st.CaskballRise * u, p.Z),
            _ => p
        };
    }

    /// <summary>
    /// The plate crossing in world feet: the u=1 sample of the shown flight. The aim tell draws
    /// it, the umpire judges it, the cursor meets it, the CPU batter reads it (spec §3, #577).
    /// </summary>
    public static (double X, double Y) Crossing(PitchCommand pitch, string? starPitchId = null, RulesTable? rules = null)
    {
        var p = Point(pitch, 1, starPitchId, rules: rules);
        return (p.X, p.Y);
    }

    /// <summary>
    /// Lateral shift from the stick at u: a bend the eye sees mid-flight (gone by the plate) plus
    /// a drift that grows late and reaches at most <c>breakMaxFt</c>, half the zone (spec §4.2).
    /// </summary>
    public static double BreakShiftFt(double u, double breakX, bool damped, PitchFlightRules f)
    {
        if (breakX == 0) return 0;
        var b = Math.Clamp(breakX, -1, 1) * (damped ? f.BreakDampedMul : 1);
        var early = Math.Sin(u * Math.PI) * b * f.BreakEarly;
        var late = u <= f.BreakLateFrom ? 0 : Math.Min(1, (u - f.BreakLateFrom) / f.BreakLateSpan);
        return early + late * late * b * f.BreakMaxFt;
    }

    /// <summary>
    /// Advance the stick's break one frame (spec §4.1): direction only, magnitude ignored; how
    /// fast the bend reaches full is the Pitch stat (pitching.flight.breakRate*). Clamped to ±1.
    /// </summary>
    public static double BreakStep(double breakX, double stickDir, double dt, int pitchStat, RulesTable? rules = null)
    {
        var f = Rules.Or(rules).Pitching.Flight;
        var dir = Math.Sign(stickDir);
        if (dir == 0) return breakX;
        var rate = f.BreakRatePerSec * Math.Max(0.25, 1 + (Math.Clamp(pitchStat, 1, 10) - 5) * f.BreakRatePerPitchStat);
        return Math.Clamp(breakX + dir * rate * Math.Max(0, dt), -1, 1);
    }

    /// <summary>
    /// Aim a delivery at an intended normalized plate crossing while preserving
    /// its shape, break, rubber position and star movement. Endpoint aim is
    /// affine, so compensating the observed offset gives an exact target.
    /// Call before PreparePitch adds execution error.
    /// </summary>
    public static PitchCommand AimForCrossing(PitchCommand pitch, double targetX, double targetY,
        string? starPitchId = null, RulesTable? rules = null)
    {
        var actual = ContactAim(pitch, starPitchId, rules);
        return pitch with { AimX = pitch.AimX + targetX - actual.X,
            AimY = pitch.AimY + targetY - actual.Y };
    }

    public static (double X, double Y) ContactAim(PitchCommand pitch, string? starPitchId = null, RulesTable? rules = null)
    {
        var p = Point(pitch, 1, starPitchId, rules: rules);
        return (p.X / PlateScaleX, (p.Y - PlateY) / PlateScaleY);
    }

    public static bool InFrontOfLook(double x, double y, double z, CameraShot shot)
    {
        var dx = x - shot.Pos.X;
        var dy = y - shot.Pos.Y;
        var dz = z - shot.Pos.Z;
        var lx = shot.Target.X - shot.Pos.X;
        var ly = shot.Target.Y - shot.Pos.Y;
        var lz = shot.Target.Z - shot.Pos.Z;
        return dx * lx + dy * ly + dz * lz > 0;
    }

    public static double ApparentDeg(double x, double y, double z, CameraShot shot, double diameter)
    {
        var dist = Math.Sqrt(
            (x - shot.Pos.X) * (x - shot.Pos.X) +
            (y - shot.Pos.Y) * (y - shot.Pos.Y) +
            (z - shot.Pos.Z) * (z - shot.Pos.Z));
        return Math.Atan(diameter / Math.Max(0.4, dist)) * (180 / Math.PI);
    }

    /// <summary>Straight to the aim with a hump the eye reads as a fastball; on target at the plate.</summary>
    static (double X, double Y, double Z) Fastball(double u, double tx, double ty, double z, (double X, double Y, double Z) rel, PitchShapeRules sh)
    {
        var x = rel.X + (tx - rel.X) * u;
        var y = rel.Y + (ty - rel.Y) * u + sh.FastballHump * 4 * u * (1 - u);
        return (x, y, z);
    }

    /// <summary>Hangs until <c>changeupHangUntil</c>, then dumps to its (lower) aim.</summary>
    static (double X, double Y, double Z) Changeup(double u, double tx, double ty, double z, (double X, double Y, double Z) rel, PitchShapeRules sh)
    {
        var hang = u < sh.ChangeupHangUntil
            ? u * sh.ChangeupHangRate
            : sh.ChangeupHangUntil * sh.ChangeupHangRate + (u - sh.ChangeupHangUntil) * sh.ChangeupDumpRate;
        hang = Math.Clamp(hang, 0, 1);
        if (u >= 1) hang = 1;
        var x = rel.X + (tx - rel.X) * u;
        var y = rel.Y + (ty - rel.Y) * hang;
        return (x, y, z);
    }
}
