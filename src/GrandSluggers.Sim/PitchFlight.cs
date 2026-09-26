namespace GrandSluggers.Sim;

/// <summary>
/// The pitch as a readable object (spec §4.2, §4.3): a <b>family</b> from the shared library —
/// today the fastball, which flies true with a mild hump, and the changeup, which hangs then dumps
/// below the fastball's height — plus the family's own natural sweep (<see cref="SweepShiftFt"/>,
/// zero for both shipped rows), the stick's break after release and the rubber walk. u=0 at
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
    // strike frame, and the umpire all share it. The rubber is the table's (DiamondGeometry.Mound).
    public const double PlateScaleX = 1.85;
    /// <summary>Feet of crossing per unit of vertical aim in the reference frame; a batter's zone scales it (§4.4).</summary>
    public const double PlateScaleY = 1.35;

    /// <summary>
    /// Where an aim crosses in <paramref name="zone"/> (spec §4.2, §4.4): the aim is relative to the batter's zone.
    /// Aim 0 is the middle of this batter's zone; the vertical aim stretches with the zone
    /// (<see cref="BatterZone.VerticalScale"/>) and the horizontal one does not, because the width is fixed.
    /// </summary>
    public static (double X, double Y) PlateTarget(double aimX, double aimY, BatterZone zone) =>
        (aimX * PlateScaleX, zone.CenterY + aimY * PlateScaleY * zone.VerticalScale);

    /// <summary>Throwing hand, not the torso (pitching.flight.releaseHand*). +X toward first from a RHP. The rubber walk moves it by <see cref="HomeSet.PitcherWalk"/> per unit.</summary>
    public static (double X, double Y, double Z) Release(RulesTable rules, double rubberX = 0)
    {
        var f = rules.Pitching.Flight;
        return (rubberX * HomeSet.PitcherWalk + f.ReleaseHandX, f.ReleaseHandY, DiamondGeometry.Of(rules).Mound - f.ReleaseTowardPlate);
    }

    /// <summary>
    /// Super Sluggers air time, not MLB 90 (pitching.flight.arcadeScale, airMin/MaxSec). A changeup is 0.80× the meat and ~0.25 s longer.
    /// </summary>
    public static double AirSeconds(double mph, RulesTable rules)
    {
        var f = rules.Pitching.Flight;
        var real = DiamondGeometry.Of(rules).Mound / (Math.Max(f.MinMph, mph) * BallFlight.MphToFtPerSec);
        return Math.Clamp(real * f.ArcadeScale, f.AirMinSec, f.AirMaxSec);
    }

    /// <summary>
    /// The ball at u for a family. <paramref name="type"/> is a <see cref="PitchFamily"/> id and is
    /// resolved through <see cref="PitchFamilyTable.Of"/>, so an unauthored or unknown id stops here
    /// instead of flying as a fastball. <paramref name="breakX"/> is the stick (−1..1); a charged
    /// pitch, or one whose family is <see cref="PitchFamilyRules.BreakDamped"/>, takes
    /// <c>breakDampedMul</c> of it (spec §4.1). The rubber walk moves the crossing by the same world
    /// distance as the body, once (spec §4.2). <paramref name="zone"/> is the batter's (§4.4): the aim is its
    /// middle and the row's <c>dropFt</c> is a share of it, so every family keeps its place in every batter's zone.
    /// Absent, the flight is in <see cref="StrikeZoneGeometry.Reference"/> (a probe or a still with no batter).
    /// </summary>
    public static (double X, double Y, double Z) Point(
        string type, double u, RulesTable rules, double aimX = 0, double aimY = 0,
        double breakX = 0, double rubberX = 0,
        (double X, double Y, double Z)? from = null, bool charged = false,
        Hand throws = Hand.R, BatterZone? zone = null)
    {
        var r = rules;
        var f = r.Pitching.Flight;
        var row = r.Pitching.Families.Of(type);
        u = Math.Clamp(u, 0, 1);
        var z0 = zone ?? StrikeZoneGeometry.Reference;
        var (tx, ty) = PlateTarget(aimX, aimY, z0);
        tx += rubberX * HomeSet.PitcherWalk;
        ty -= row.DropFt * z0.VerticalScale;
        var rel = from ?? Release(r, rubberX);
        // Release to the zone's plane, the plate's front edge (§4.4): u = 1 is the crossing the umpire judges.
        var z = rel.Z * (1 - u) + StrikeZoneGeometry.PlateZ * u;
        var (x, y, zz) = Shape(u, tx, ty, z, rel, row);
        // Shape → sweep → stick → star. The sweep is the family's own movement and the stick's shift
        // is the player's, so they add rather than one scaling the other (PH-15-R6). The add is
        // skipped outright when there is no sweep: `x + 0.0` is x for every value a flight has
        // except a negative zero, where it clears the sign bit, and the #811 golden holds X bit for
        // bit. Nothing in the shipped table sweeps, so nothing in the shipped flight moves.
        var sweep = SweepShiftFt(u, row, throws);
        if (sweep != 0) x += sweep;
        x += BreakShiftFt(u, breakX, charged || row.BreakDamped, f);
        return (x, y, zz);
    }

    /// <summary>The same delivered ball is used by rendering, contact and the umpire.</summary>
    public static (double X, double Y, double Z) Point(PitchCommand pitch, double u,
        RulesTable rules,
        string? starPitchId = null, (double X, double Y, double Z)? from = null, StarSkillTable? skills = null)
    {
        var r = rules;
        u = Math.Clamp(u, 0, 1);
        var zone = StrikeZoneGeometry.Of(pitch);
        // A row's own pace (§13): the leap hangs the ball over one stretch of its path and makes up the time after it,
        // so the same path arrives at the same instant — the timing window and the crossing are the ordinary pitch's.
        var row = pitch.Star ? StarSkillTable.Or(skills).Pitch(starPitchId) : null;
        var time = u;
        if (row?.Leap is { } leap) u = leap.Progress(u);
        // The loop holds the ball at one point of its path while it loops, then runs the rest on the same clock.
        if (row?.Loop is { } loop) u = loop.Progress(u);
        var p = Point(pitch.Type, u, r, pitch.AimX, pitch.AimY, pitch.BreakX * pitch.BreakMul,
            pitch.RubberX, from, ChargeFeel.IsCharge(pitch.Charge01, r), pitch.Throws, zone);
        if (!pitch.Star) return p;
        // A row's own path shape (§13): the float rises early and lands on the crossing the pitch was always going to make.
        if (row?.Float is { } rise && rise.Lift(u) is var lift and not 0)
            p = (p.X, p.Y + lift * zone.VerticalScale, p.Z);
        // A late rise (§13): the ball climbs over the last stretch to a crossing above the aimed one. This moves the crossing,
        // so the umpire, the bat and the CPU all judge the risen ball; in reference-zone feet, like every vertical star shape.
        if (row?.Rise is { } late && late.Lift(u) is var climb and not 0)
            p = (p.X, p.Y + climb * zone.VerticalScale, p.Z);
        // The loop itself (§13): a circle standing on the path at the held point, in the vertical plane of the flight.
        if (row?.Loop is { } ring && ring.Offset(time) is var (forward, up) && (forward != 0 || up != 0))
        {
            var toward = Math.Sign(StrikeZoneGeometry.PlateZ - (from ?? Release(r, pitch.RubberX)).Z);
            p = (p.X, p.Y + up, p.Z + forward * toward);
        }
        // The sway (§13): side to side across the path, widest mid-flight, settled onto the path before the plate.
        if (row?.Sway is { } sway && sway.OffsetFt(u) is var side and not 0)
            p = (p.X + side, p.Y, p.Z);
        // The pendulum (§13): the ball swings on its vine about a pivot riding above the ordinary ball, and hangs straight at the plate.
        if (row?.Pendulum is { } vine && vine.Offset(time, PendulumSide(pitch, r, from)) is var swing && swing != (0, 0))
            p = (p.X + swing.X, p.Y + swing.Y, p.Z);
        var st = r.Pitching.StarShapes;
        return starPitchId switch
        {
            "phonyball" => (p.X + (u > st.PhonyballSwitchAt ? st.PhonyballLateX : st.PhonyballEarlyX), p.Y, p.Z),
            _ => p
        };
    }

    /// <summary>
    /// The side a pendulum pitch swings in from (spec §13): the half of the plate away from its crossing, so the vine carries
    /// the ball in across the zone. A crossing on the middle swings in from −X. Read off the ordinary crossing, which the
    /// pendulum never moves.
    /// </summary>
    static double PendulumSide(PitchCommand pitch, RulesTable rules, (double X, double Y, double Z)? from)
    {
        var cross = Point(pitch.Type, 1, rules, pitch.AimX, pitch.AimY, pitch.BreakX * pitch.BreakMul,
            pitch.RubberX, from, ChargeFeel.IsCharge(pitch.Charge01, rules), pitch.Throws, StrikeZoneGeometry.Of(pitch));
        return cross.X > 0 ? -1 : 1;
    }

    /// <summary>
    /// The pivot a pendulum pitch's vine hangs from at <paramref name="u"/> (spec §13): <see cref="PitchPendulum.LengthFt"/>
    /// straight above the ordinary ball, so the vine drawn from it to the ball is always that long and hangs straight over the
    /// crossing at the plate. Null for a pitch that is not starred or has no pendulum. The tell draws the vine; nothing
    /// else reads it.
    /// </summary>
    public static (double X, double Y, double Z)? Pivot(PitchCommand pitch, double u, RulesTable rules,
        string? starPitchId, StarSkillTable? skills = null, (double X, double Y, double Z)? from = null)
    {
        if (!pitch.Star || StarSkillTable.Or(skills).Pitch(starPitchId)?.Pendulum is not { } vine) return null;
        var ball = Point(pitch, u, rules, starPitchId, from, skills);
        var swing = vine.Offset(Math.Clamp(u, 0, 1), PendulumSide(pitch, rules, from));
        return (ball.X - swing.X, ball.Y - swing.Y + vine.LengthFt, ball.Z);
    }

    /// <summary>
    /// The plate crossing in world feet: the u=1 sample of the shown flight. The aim tell draws
    /// it, the umpire judges it, the cursor meets it, the CPU batter reads it (spec §3, #577).
    /// </summary>
    public static (double X, double Y) Crossing(PitchCommand pitch, RulesTable rules, string? starPitchId = null)
    {
        var p = Point(pitch, 1, rules, starPitchId);
        return (p.X, p.Y);
    }

    /// <summary>
    /// Where a star pitch's twin is drawn at <paramref name="u"/> and how strongly (spec §13), or null when the pitch has
    /// no twin or it has faded. The twin flies beside the real ball, <see cref="PitchTwin.OffsetFt"/> toward the far
    /// half of the zone from the real crossing, so the hitter sees two balls on either side and must pick the real
    /// one before it goes. Nothing reads it but the eye: the umpire, the bat and the CPU batter read the real ball's <c>Point</c>.
    /// </summary>
    public static (double X, double Y, double Z, double Alpha)? Twin(PitchCommand pitch, double u, RulesTable rules,
        string? starPitchId, StarSkillTable? skills = null, (double X, double Y, double Z)? from = null)
    {
        if (!pitch.Star || StarSkillTable.Or(skills).Pitch(starPitchId)?.Twin is not { } twin) return null;
        var alpha = twin.Alpha(Math.Clamp(u, 0, 1));
        if (alpha <= 0) return null;
        var real = Point(pitch, u, rules, starPitchId, from, skills);
        var side = Crossing(pitch, rules, starPitchId).X >= 0 ? -1 : 1;
        return (real.X + side * twin.OffsetFt, real.Y, real.Z, alpha);
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
    /// World X of a pitcher's <b>glove side</b>, and the one place the sign is decided (spec §4.2,
    /// #818).
    ///
    /// <para>
    /// Read off the diamond, not remembered: home is the origin and first base is
    /// <see cref="Diamond.First"/>, whose X is <c>infield.cornerFt</c> — a <c>[Positive]</c> rule, so
    /// first base is on the +X side of the center line in every data root there can be. A
    /// right-hander stands on the rubber facing home with first base on the glove hand's side, so a
    /// right-hander's glove side is +X and a left-hander's is −X. A sweep toward the glove side is a
    /// slider running away from a same-handed batter, which is what "glove side" is worth naming for.
    /// </para>
    ///
    /// <para>
    /// This is the flight's only opinion about the arm. <see cref="Release"/> is not mirrored — every
    /// pitcher's hand leaves from the same <c>releaseHandX</c> — and #818 leaves that alone
    /// deliberately; see the report in <c>docs/research/pitch-families-p1d.md</c>.
    /// </para>
    /// </summary>
    public static double GloveSideSign(Hand throws) =>
        throws == Hand.L ? -1 : 1; // +X is the first-base side on every table

    /// <summary>
    /// The family's natural sweep at u, in feet of world X (spec §4.2, #818): nothing until
    /// <see cref="PitchFamilyRules.SweepFrom"/>, then the square of the share of flight that is left,
    /// reaching the row's whole <see cref="PitchFamilyRules.SweepFt"/> exactly at the plate, on the
    /// side <see cref="GloveSideSign"/> gives this arm.
    ///
    /// <para>
    /// The curve is a quadratic ease and nothing else: multiply, subtract, divide. No
    /// <c>Math.Sin</c>, <c>Pow</c> or <c>Atan</c> stands between the table and the crossing, so the
    /// same row produces the same bits on macOS and on glibc (#811, #736) and evidence derived from
    /// it can be compared across platforms.
    /// </para>
    ///
    /// <para>
    /// It does not read the stick, the charge or the Pitch stat. The stick's shift
    /// (<see cref="BreakShiftFt"/>) is added beside it under its own unchanged cap, and a charge
    /// damps that shift and not this one (PH-05-R1: a charge costs steering correction, never
    /// characteristic movement).
    /// </para>
    /// </summary>
    public static double SweepShiftFt(double u, PitchFamilyRules row, Hand throws)
    {
        if (row is null) throw new ArgumentNullException(nameof(row));
        // A row that never begins to sweep, and a row with no sweep to begin, are both straight —
        // and the guard is also what keeps 1 out of the divisor below.
        if (row.SweepFt == 0 || row.SweepFrom >= 1) return 0;
        var t = u <= row.SweepFrom ? 0 : (u - row.SweepFrom) / (1 - row.SweepFrom);
        return t * t * row.SweepFt * GloveSideSign(throws);
    }

    /// <summary>
    /// Advance the stick's break one frame (spec §4.1): direction only, magnitude ignored; how
    /// fast the bend reaches full is the arm's <b>Control</b> (<see cref="Stats.Control"/>, PH-15-R6; the
    /// rules keep the historical name pitching.flight.breakRate*PerPitchStat). Clamped to ±1.
    /// </summary>
    public static double BreakStep(double breakX, double stickDir, double dt, int pitchStat, RulesTable rules)
    {
        var f = rules.Pitching.Flight;
        var dir = Math.Sign(stickDir);
        if (dir == 0) return breakX;
        var rate = BreakRatePerSec(pitchStat, f);
        return Math.Clamp(breakX + dir * rate * Math.Max(0, dt), -1, 1);
    }

    /// <summary>
    /// How fast this arm bends a held stick, in stick-units per second (spec §4.1). One expression,
    /// so <see cref="BreakStep"/> (a frame of the hold) and <see cref="BreakReach"/> (the whole hold)
    /// cannot drift apart: the operation order is <see cref="BreakStep"/>'s own, unchanged, and the
    /// #811 golden is the falsifier.
    /// </summary>
    static double BreakRatePerSec(int pitchStat, PitchFlightRules f) =>
        f.BreakRatePerSec * Math.Max(0.25, 1 + (Math.Clamp(pitchStat, 1, 10) - 5) * f.BreakRatePerPitchStat);

    /// <summary>
    /// The whole bend a stick <b>held one way from release</b> reaches by the plate: the rate above
    /// over the flight's own air time, clamped at 1 the way <see cref="BreakStep"/>'s own clamp is
    /// (spec §4.1, §4.8; PH-04, PH-18-R1).
    ///
    /// <para>
    /// This exists because two callers must agree on one number. A hand holding the stick
    /// accumulates it a frame at a time; the CPU pitcher has no frames to hold, so it takes the same total in one
    /// step (<see cref="CpuPitcher.PitchByInputs"/>) rather than an instant ±1 no arm could reach.
    /// A short flight, a low Control, or both, and the hold simply does not get there.
    /// </para>
    ///
    /// <para>
    /// Pure and additive: it reads the same <c>pitching.flight</c> rows, decides nothing about a
    /// pitch, and nothing that flew before this function existed flies differently because of it.
    /// </para>
    /// </summary>
    /// <param name="pitchStat">The arm's Control (<see cref="Stats.Control"/>), clamped to 1..10 as <see cref="BreakStep"/> clamps it.</param>
    /// <param name="airSec">Seconds of flight the stick is held for (<see cref="AirSeconds"/>).</param>
    public static double BreakReach(int pitchStat, double airSec, RulesTable rules) =>
        Math.Min(1, BreakRatePerSec(pitchStat, rules.Pitching.Flight) * Math.Max(0, airSec));

    /// <summary>
    /// Aim a delivery at an intended normalized plate crossing while preserving
    /// its shape, break, rubber position and star movement. Endpoint aim is
    /// affine, so compensating the observed offset gives an exact target.
    /// Call before PreparePitch adds execution error.
    /// </summary>
    public static PitchCommand AimForCrossing(PitchCommand pitch, double targetX, double targetY,
        RulesTable rules,
        string? starPitchId = null)
    {
        var actual = ContactAim(pitch, rules, starPitchId);
        return pitch with { AimX = pitch.AimX + targetX - actual.X,
            AimY = pitch.AimY + targetY - actual.Y };
    }

    public static (double X, double Y) ContactAim(PitchCommand pitch, RulesTable rules, string? starPitchId = null)
    {
        var p = Point(pitch, 1, rules, starPitchId);
        var zone = StrikeZoneGeometry.Of(pitch);
        return (p.X / PlateScaleX, (p.Y - zone.CenterY) / (PlateScaleY * zone.VerticalScale));
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
