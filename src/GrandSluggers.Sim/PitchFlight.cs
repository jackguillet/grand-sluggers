namespace GrandSluggers.Sim;

/// <summary>
/// The pitch as a readable object (spec §4.2, §4.3): a <b>family</b> from the shared library —
/// today the fastball, which flies true with a mild hump, and the changeup, which hangs then dumps
/// below the fastball's height — plus the stick's break after release and the rubber walk. u=0 at
/// the release, u=1 at the plate. One function,
/// <see cref="Point(PitchCommand, double, string?, ValueTuple{double, double, double}?, RulesTable?)"/>,
/// gives the ball at any u; the aim tell, the ball, the umpire, and the CPU batter all read
/// its u=1 sample (<see cref="Crossing"/>). Every shape number is a row in pitching.json
/// <c>families</c> (<see cref="PitchFamilyTable"/>, #810), and one <see cref="Shape"/> evaluates
/// every row — a new family is a row, not a new branch.
/// </summary>
public static class PitchFlight
{
    // Plate frame geometry: the normalized aim square and where it sits. Presentation, the
    // strike frame, and the umpire all share it, so it stays one number like Diamond — which
    // now reads it from data, so this forwards rather than baking a copy at compile time.
    public static double MoundZ => Diamond.Mound;
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
    /// Super Sluggers air time, not MLB 90 (pitching.flight.arcadeScale, airMin/MaxSec). A changeup is 0.80× the meat and ~0.25 s longer.
    /// </summary>
    public static double AirSeconds(double mph, RulesTable? rules = null)
    {
        var f = Rules.Or(rules).Pitching.Flight;
        var real = Diamond.Mound / (Math.Max(f.MinMph, mph) * 1.4667);
        return Math.Clamp(real * f.ArcadeScale, f.AirMinSec, f.AirMaxSec);
    }

    /// <summary>
    /// The ball at u for a family. <paramref name="type"/> is a <see cref="PitchFamily"/> id and is
    /// resolved through <see cref="PitchFamilyTable.Of"/>, so an unauthored or unknown id stops here
    /// instead of flying as a fastball. <paramref name="breakX"/> is the stick (−1..1); a charged
    /// pitch, or one whose family is <see cref="PitchFamilyRules.BreakDamped"/>, takes
    /// <c>breakDampedMul</c> of it (spec §4.1). The rubber walk moves the crossing by the same world
    /// distance as the body, once (spec §4.2).
    /// </summary>
    public static (double X, double Y, double Z) Point(
        string type, double u, double aimX = 0, double aimY = 0,
        double breakX = 0, double rubberX = 0,
        (double X, double Y, double Z)? from = null, RulesTable? rules = null, bool charged = false)
    {
        var r = Rules.Or(rules);
        var f = r.Pitching.Flight;
        var row = r.Pitching.Families.Of(type);
        u = Math.Clamp(u, 0, 1);
        var (tx, ty) = PlateTarget(aimX, aimY);
        tx += rubberX * HomeSet.PitcherWalk;
        ty -= row.DropFt;
        var rel = from ?? Release(rubberX, r);
        var z = rel.Z * (1 - u);
        var (x, y, zz) = Shape(u, tx, ty, z, rel, row);
        x += BreakShiftFt(u, breakX, charged || row.BreakDamped, f);
        return (x, y, zz);
    }

    /// <summary>The same delivered ball is used by rendering, contact and the umpire.</summary>
    public static (double X, double Y, double Z) Point(PitchCommand pitch, double u,
        string? starPitchId = null, (double X, double Y, double Z)? from = null, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        u = Math.Clamp(u, 0, 1);
        var p = Point(pitch.Type, u, pitch.AimX, pitch.AimY, pitch.BreakX * pitch.BreakMul,
            pitch.RubberX, from, r, ChargeFeel.IsCharge(pitch.Charge01));
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

    /// <summary>
    /// The one shape function every family is evaluated by (spec §4.3, #810). X always travels
    /// straight to the aim; Y travels by <c>hang</c>, which interpolates at
    /// <see cref="PitchFamilyRules.HangRate"/> until <see cref="PitchFamilyRules.HangUntil"/> and at
    /// <see cref="PitchFamilyRules.DumpRate"/> after it, and always arrives at u=1; on top of that
    /// rides the mid-flight <see cref="PitchFamilyRules.Hump"/>.
    ///
    /// <b>It reproduces both shipped shapes bit for bit, and the arithmetic is written that way on
    /// purpose</b> (<c>PitchFamilyGoldenTests</c> is the falsifier, not this comment):
    /// <list type="bullet">
    /// <item>The fastball row is <c>hangUntil 1, hangRate 1</c>, so below u=1 the hang branch gives
    /// <c>u * 1.0</c>, which is exactly <c>u</c>; the clamp returns a value already inside [0, 1]
    /// unchanged; and at u=1 the dump branch gives <c>1 * 1.0 + 0.0 * dumpRate</c> = 1.0, which the
    /// <c>u >= 1</c> line then reasserts. <c>dumpRate</c> is 1 so the two rates meet at the seam
    /// rather than sitting in the table as an arbitrary unused number.</item>
    /// <item>The changeup row is <c>hump 0</c>, and <c>0 * 4 * u * (1 - u)</c> is <c>+0.0</c> for
    /// every u in [0, 1], and <c>y + 0.0</c> is exactly <c>y</c> for the heights a pitch has.</item>
    /// <item><c>ty - dropFt</c> with <c>dropFt 0</c> is exact for every ty, including a signed zero
    /// (subtracting +0.0 never changes a value the way adding it can).</item>
    /// <item>The operation order is the order the two retired functions used, because floating-point
    /// addition does not associate: <c>rel + (t − rel) × hang</c>, then <c>+ ((hump × 4) × u) × (1 − u)</c>.</item>
    /// </list>
    /// </summary>
    public static (double X, double Y, double Z) Shape(double u, double tx, double ty, double z,
        (double X, double Y, double Z) rel, PitchFamilyRules row)
    {
        var hang = u < row.HangUntil
            ? u * row.HangRate
            : row.HangUntil * row.HangRate + (u - row.HangUntil) * row.DumpRate;
        hang = Math.Clamp(hang, 0, 1);
        if (u >= 1) hang = 1;
        var x = rel.X + (tx - rel.X) * u;
        var y = rel.Y + (ty - rel.Y) * hang + row.Hump * 4 * u * (1 - u);
        return (x, y, z);
    }
}
