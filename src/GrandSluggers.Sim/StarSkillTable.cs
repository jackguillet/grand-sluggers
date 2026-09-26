namespace GrandSluggers.Sim;

/// <summary>A captain's star pitch (data/abilities/star-skills.json, spec §13): the numbers the sim reads at runtime.</summary>
public sealed record StarPitchSkill(
    string Id,
    string Name,
    string Kind,
    double SpeedMul,
    int StaminaCost,
    bool LateBreak,
    bool Decoy,
    string? OnCatch,
    /// <summary>A faint second ball drawn beside the real one early in the flight, or null (§13).</summary>
    PitchTwin? Twin = null,
    /// <summary>A path that floats high early and drops onto the unchanged crossing late, or null (§13).</summary>
    PitchFloat? Float = null,
    /// <summary>A pace that hangs the ball over one stretch of its path and leaps it to the plate on time, or null (§13).</summary>
    PitchLeap? Leap = null,
    /// <summary>A late rise that lifts the ball over the last stretch of its flight to a crossing above the aimed one, or null (§13).</summary>
    PitchRise? Rise = null,
    /// <summary>One full vertical loop mid-flight, then the ordinary crossing on the ordinary time, or null (§13).</summary>
    PitchLoop? Loop = null,
    /// <summary>A path that swings in on one pendulum arc from a pivot above the ball onto the unchanged crossing, or null (§13).</summary>
    PitchPendulum? Pendulum = null);

/// <summary>
/// A star pitch's loop (spec §13): from <see cref="At"/> of the flight the ball runs one full vertical loop,
/// <see cref="DiameterFt"/> across, over <see cref="Span"/> of the flight, standing still along its path while it loops;
/// then it runs on along the rest of its path to arrive at the ordinary instant. The loop sits on the path like a coaster
/// loop on its track: it leaves forward along the flight, climbs, turns over the top and comes back down to where it
/// started. The path, the crossing and the arrival time are the ordinary pitch's; only where the ball is, and when, changes.
/// </summary>
public sealed record PitchLoop(double At, double Span, double DiameterFt)
{
    /// <summary>The widest loop a row may name: a loop over the plate's width, not over the backstop.</summary>
    public const double MaxDiameterFt = 6;

    /// <summary>The time fraction the loop is done by: the ball runs its last stretch on its own path.</summary>
    public double Exit => At + Span;

    /// <summary>How far along its path the ball is at time fraction <paramref name="u"/>: the identity, held through the loop, then a catch-up; 1 at 1.</summary>
    public double Progress(double u)
    {
        u = Math.Clamp(u, 0, 1);
        if (u <= At) return u;
        if (u <= Exit) return At;
        if (u >= 1) return 1;
        return At + (1 - At) * (u - Exit) / (1 - Exit);
    }

    /// <summary>
    /// The loop's offset from the path at time fraction <paramref name="u"/>: along the flight (toward the plate) and up, in feet.
    /// A circle of the loop's diameter standing on the path, entered and left at its foot; (0, 0) outside the loop.
    /// </summary>
    public (double Forward, double Up) Offset(double u)
    {
        if (u <= At || u >= Exit) return (0, 0);
        var turn = 2 * Math.PI * (u - At) / Span;
        var r = DiameterFt / 2;
        return (r * Math.Sin(turn), r * (1 - Math.Cos(turn)));
    }
}

/// <summary>
/// A star pitch's late rise (spec §13): nothing until <see cref="From"/> of the flight, then the ball climbs on a quadratic ease
/// to <see cref="RiseFt"/> above its ordinary path exactly at the plate. Unlike a float, the crossing moves: the umpire, the
/// bat and the CPU judge the risen ball, and the timing window is judged at that real crossing. The rise is always up.
/// </summary>
public sealed record PitchRise(double RiseFt, double From)
{
    /// <summary>The largest rise a row may name, in reference-zone feet: a jump out of the heart of the zone, not over the batter.</summary>
    public const double MaxRiseFt = 2;

    /// <summary>The height over the ordinary path at <paramref name="u"/>: 0 up to <see cref="From"/>, then (share of the stretch)² × <see cref="RiseFt"/>; the full rise at the plate.</summary>
    public double Lift(double u)
    {
        u = Math.Clamp(u, 0, 1);
        if (u <= From) return 0;
        var t = (u - From) / (1 - From);
        return RiseFt * t * t;
    }
}

/// <summary>
/// A star pitch's pendulum (spec §13): the ball hangs on a vine <see cref="LengthFt"/> long from a pivot that rides
/// <see cref="LengthFt"/> above the ordinary ball. The vine swings out from straight down to <see cref="SwingDeg"/> by
/// <see cref="WidestAt"/> of the flight, then swings back in on one pendulum arc to straight down at the plate. Every point
/// of the flight is on the circle about the pivot, so the ball is always exactly <see cref="LengthFt"/> from it; at the plate
/// the vine hangs straight and the ball is on the ordinary crossing at the ordinary instant, so the umpire, the bat and the
/// CPU read the ordinary pitch.
/// </summary>
public sealed record PitchPendulum(double LengthFt, double SwingDeg, double WidestAt)
{
    /// <summary>The longest vine a row may name: a swing into the zone, not across the infield.</summary>
    public const double MaxLengthFt = 12;

    /// <summary>The widest swing a row may name, from straight down: the ball never swings above the pivot.</summary>
    public const double MaxSwingDeg = 80;

    /// <summary>The vine's angle from straight down at <paramref name="u"/>, in degrees: out along a quarter sine, in along a pendulum's quarter swing, exactly 0 at the plate.</summary>
    public double AngleDeg(double u)
    {
        u = Math.Clamp(u, 0, 1);
        if (u >= 1) return 0;
        if (u <= WidestAt) return SwingDeg * Math.Sin(Math.PI / 2 * u / WidestAt);
        return SwingDeg * Math.Cos(Math.PI / 2 * (u - WidestAt) / (1 - WidestAt));
    }

    /// <summary>
    /// The ball's offset from the ordinary path at <paramref name="u"/>, in world feet: sideways toward <paramref name="side"/>
    /// (+1 or −1 in X) and up, both exactly 0 at the plate.
    /// </summary>
    public (double X, double Y) Offset(double u, double side)
    {
        var a = AngleDeg(u) * Math.PI / 180;
        if (a == 0) return (0, 0);
        return (side * LengthFt * Math.Sin(a), LengthFt * (1 - Math.Cos(a)));
    }
}

/// <summary>
/// A star pitch's leap (spec §13): from <see cref="At"/> of the flight the ball crawls at <see cref="HoldPace"/> of its pace for
/// <see cref="Hold"/> of the flight, then leaps over the rest of its path to arrive at the ordinary instant. The path, the
/// crossing and the arrival time are the ordinary pitch's; only where the ball is along its path, and when, changes.
/// </summary>
public sealed record PitchLeap(double At, double Hold, double HoldPace)
{
    /// <summary>How far along its path the ball is at time fraction <paramref name="u"/>: the identity, a crawl, then a catch-up; 1 at 1.</summary>
    public double Progress(double u)
    {
        u = Math.Clamp(u, 0, 1);
        if (u <= At) return u;
        var held = At + HoldPace * Hold;
        if (u <= At + Hold) return At + HoldPace * (u - At);
        if (u >= 1) return 1;
        return held + (1 - held) * (u - At - Hold) / (1 - At - Hold);
    }
}

/// <summary>
/// A star pitch's float (spec §13): the ball rises up to <see cref="RiseFt"/> above its ordinary path, highest at
/// <see cref="DropFrom"/> of the flight, then drops back onto the ordinary path by the plate. The crossing — what the
/// umpire, the bat and the CPU judge — is the ordinary one; only the look of the flight bends.
/// </summary>
public sealed record PitchFloat(double RiseFt, double DropFrom)
{
    /// <summary>The height over the ordinary path at <paramref name="u"/>: up along a quarter sine, down along a parabola, exactly 0 at the plate.</summary>
    public double Lift(double u)
    {
        u = Math.Clamp(u, 0, 1);
        if (u >= 1) return 0;
        if (u <= DropFrom) return RiseFt * Math.Sin(Math.PI / 2 * u / DropFrom);
        var d = (u - DropFrom) / (1 - DropFrom);
        return RiseFt * (1 - d * d);
    }
}

/// <summary>
/// A star pitch's twin (spec §13): a faint second ball <see cref="OffsetFt"/> to the far side of the zone from the real
/// crossing, flying beside the real ball at full strength until <see cref="FadeFrom"/> of the flight and gone by
/// <see cref="FadeTo"/>. It is drawn only: the umpire, the bat and the CPU read the one real ball.
/// </summary>
public sealed record PitchTwin(double OffsetFt, double FadeFrom, double FadeTo)
{
    /// <summary>The twin is gone by half the flight at the latest: the hitter judges one ball in the second half.</summary>
    public const double GoneBy = 0.5;

    /// <summary>How strongly the twin shows at <paramref name="u"/> of the flight: 1, fading linearly to 0.</summary>
    public double Alpha(double u) =>
        u <= FadeFrom ? 1 : u >= FadeTo ? 0 : 1 - (u - FadeFrom) / (FadeTo - FadeFrom);
}

/// <summary>A captain's star swing (data/abilities/star-skills.json, spec §13).</summary>
public sealed record StarSwingSkill(
    string Id,
    string Name,
    string Kind,
    double ExitVeloMul,
    double? LaunchDeg,
    string? Terrain,
    double FielderPauseSec,
    bool InfieldChaos,
    bool Decoy,
    /// <summary>
    /// A fair ball off this swing turns this many degrees at its first hop, away from the fielder chasing it (§13); 0 is none.
    /// </summary>
    double FirstHopKickDeg = 0,
    /// <summary>How strongly the park's wind acts on this swing's ball (§13, <see cref="AtBatResult.WindMul"/>); 1 is the ordinary ball.</summary>
    double WindMul = 1,
    /// <summary>A fair ball off this swing leaves its first hop this many times as fast upward (§13); 1 is the ordinary hop.</summary>
    double FirstHopBounceMul = 1,
    /// <summary>A fair ball off this swing stands still at its first hop for this many seconds (§13); 0 is none.</summary>
    double FirstHopStallSec = 0,
    /// <summary>After a first-hop stall the ball runs on at this share of its speed (§13); 1 is its own.</summary>
    double FirstHopStallSpeedMul = 1,
    /// <summary>
    /// This swing's Perfect ring is this many times the ordinary one (§13, PH-16-R2: a swing's own contact area); 1 is the
    /// ordinary ring. The nice oval, the sour rim and the timing window are unchanged, so a miss is still a miss.
    /// </summary>
    double PerfectRingMul = 1,
    /// <summary>A ball off this swing that jags sideways twice in the air and lands where the straight ball would, or null (§13).</summary>
    BallJag? Jag = null)
{
    /// <summary>The longest stall a row may name: the ball spins, then baseball resumes inside the two-second rule.</summary>
    public const double MaxStallSec = 1.2;

    /// <summary>The highest launch a stalling swing may name: the stall is a grounder's, so its first hop comes early.</summary>
    public const double MaxStallLaunchDeg = 4;

    /// <summary>The largest Perfect ring a row may name: twice the ordinary heart, and never past the drawn oval (<see cref="SweetSpot.Zone"/>).</summary>
    public const double MaxPerfectRingMul = 2;

    /// <summary>The highest first-hop bounce a row may name: a chopper, not a moon shot.</summary>
    public const double MaxBounceMul = 3;

    /// <summary>The swing changes its ball's first hop.</summary>
    public bool ShapesFirstHop => FirstHopKickDeg > 0 || FirstHopBounceMul != 1 || FirstHopStallSec > 0;

    /// <summary>The largest wind factor a row may name: the wind may carry a star ball twice as far, never more.</summary>
    public const double MaxWindMul = 2;

    /// <summary>The largest kick a row may name: a hop, not a U-turn.</summary>
    public const double MaxKickDeg = 45;
}

/// <summary>
/// A star swing's jagged flight (spec §13): the batted ball jags sideways <see cref="OffsetFt"/> at <see cref="FirstAt"/> of its
/// jag window, runs on beside its line, and jags back onto the line at <see cref="SecondAt"/>; each jag takes
/// <see cref="Span"/> of the window. The window is the ball's air time to its first landing (the ground, the wall, the fence or
/// the stands) and never longer than <see cref="WithinSec"/>, so the ball is back on its line before it lands: the landing, the
/// fair / foul verdict and the fence are the straight ball's. The jagged ball is the real ball — the gloves, the CPU's read and
/// the drawn ball all take its position from the one path.
/// </summary>
public sealed record BallJag(double OffsetFt, double FirstAt, double SecondAt, double Span)
{
    /// <summary>The largest jag a row may name.</summary>
    public const double MaxOffsetFt = 3;

    /// <summary>Every bend ends within two seconds of contact (§13).</summary>
    public const double WithinSec = 2;

    /// <summary>How far the ball is off its line at <paramref name="s"/> of the jag window: out, beside, back, 0 outside it.</summary>
    public double OffFt(double s)
    {
        if (s <= FirstAt || s >= SecondAt + Span) return 0;
        if (s < FirstAt + Span) return OffsetFt * (s - FirstAt) / Span;
        if (s <= SecondAt) return OffsetFt;
        return OffsetFt * (1 - (s - SecondAt) / Span);
    }

    /// <summary>
    /// The jag window for a path: the time of its first landing mark, capped at <see cref="WithinSec"/>; 0 when the path
    /// has none.
    /// </summary>
    public static double WindowSec(IReadOnlyList<Sample> samples)
    {
        var i = BallFlight.LandingIndex(samples);
        var land = i < 0 ? (samples.Count > 0 ? samples[^1].T : 0) : samples[i].T;
        return Math.Min(land, WithinSec);
    }

    /// <summary>
    /// The side the ball jags to, as a unit vector in (X, Z): square to its line from contact to its landing mark, toward the
    /// middle of the field, so the jag never carries a fair ball over a foul line. A ball straight up the middle jags to −X.
    /// </summary>
    public static (double X, double Z) Side(IReadOnlyList<Sample> samples)
    {
        if (samples.Count == 0) return (0, 0);
        var i = BallFlight.LandingIndex(samples);
        var end = i < 0 ? samples[^1] : samples[i];
        var (dx, dz) = (end.X - samples[0].X, end.Z - samples[0].Z);
        var len = Math.Sqrt(dx * dx + dz * dz);
        if (len < 1e-9) return (0, 0);
        var (nx, nz) = (-dz / len, dx / len);
        return nx * dx <= 0 ? (nx, nz) : (-nx, -nz);
    }

    /// <summary>The path with the jags laid on it: every sample before the window's end moved sideways, every one after it untouched.</summary>
    public IReadOnlyList<Sample> Apply(IReadOnlyList<Sample> samples)
    {
        var w = WindowSec(samples);
        if (w <= 0) return samples;
        var (sx, sz) = Side(samples);
        if (sx == 0 && sz == 0) return samples;
        var list = new List<Sample>(samples.Count);
        foreach (var s in samples)
        {
            var off = s.T < w ? OffFt(s.T / w) : 0;
            if (off == 0) { list.Add(s); continue; }
            var (x, z) = (s.X + sx * off, s.Z + sz * off);
            list.Add(s with { X = x, Z = z, Dist = Math.Sqrt(x * x + z * z) });
        }
        return list;
    }
}

/// <summary>
/// The star skills as loaded from JSON. The JSON is the only copy (spec §13): no C# switch may
/// re-type a multiplier. Callers with a <see cref="ContentCatalog"/> use its table; a caller
/// without one falls back to the table found from the data root, like <see cref="Rules"/>.
/// </summary>
public sealed class StarSkillTable
{
    public const string Path = "abilities/star-skills.json";

    public StarSkillTable(IReadOnlyDictionary<string, StarPitchSkill> pitches, IReadOnlyDictionary<string, StarSwingSkill> swings)
    {
        Pitches = pitches;
        Swings = swings;
    }

    public IReadOnlyDictionary<string, StarPitchSkill> Pitches { get; }
    public IReadOnlyDictionary<string, StarSwingSkill> Swings { get; }

    public StarPitchSkill? Pitch(string? id) =>
        !string.IsNullOrEmpty(id) && Pitches.TryGetValue(id, out var p) ? p : null;

    public StarSwingSkill? Swing(string? id) =>
        !string.IsNullOrEmpty(id) && Swings.TryGetValue(id, out var s) ? s : null;

    /// <summary>An empty table: every lookup is the identity. Only for callers that own no data at all.</summary>
    public static StarSkillTable Empty { get; } = new(
        new Dictionary<string, StarPitchSkill>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, StarSwingSkill>(StringComparer.OrdinalIgnoreCase));

    static StarSkillTable? _default;

    /// <summary>The table found from the data root, else empty.</summary>
    public static StarSkillTable Default => _default ??= LoadDefault();

    public static StarSkillTable Or(StarSkillTable? table) => table ?? Default;

    static StarSkillTable LoadDefault()
    {
        // Outside the catch on purpose: see RulesTable.LoadDefault. A named root or overlay that
        // cannot be honoured stops the run rather than quietly handing back the control.
        var root = ContentCatalog.TryFindDataRoot();
        if (root is null) return Empty;
        try
        {
            return ContentCatalog.Load(root).StarSkills;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return Empty;
        }
    }
}

/// <summary>Star-skill lookups (spec §13). Every number comes from the table; an unknown id bends nothing.</summary>
public static class StarSkills
{
    public static double PitchSpeedMul(string? id, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Pitch(id)?.SpeedMul ?? 1.0;

    public static int StaminaCost(string? starPitch, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Pitch(starPitch)?.StaminaCost ?? 0;

    public static double SwingExitMul(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.ExitVeloMul ?? 1.0;

    /// <summary>The kind a sidekick's special names: the generic pool (§13). A captain's specials name any other kind.</summary>
    public const string GenericKind = "generic";

    /// <summary>How strongly the park's wind acts on a star swing's ball (§13); 1 for a swing whose row names none.</summary>
    public static double SwingWindMul(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.WindMul ?? 1.0;

    /// <summary>How much larger a star swing's Perfect ring is (§13); 1 for a swing whose row names none.</summary>
    public static double SwingPerfectRingMul(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.PerfectRingMul ?? 1.0;

    /// <summary>The jagged flight a star swing's ball flies (§13); null for a swing whose row names none.</summary>
    public static BallJag? SwingJag(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.Jag;

    /// <summary>A role player's star swing names its launch; a captain's keeps the swing's own.</summary>
    public static double? SwingLaunchDeg(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.LaunchDeg;

    /// <summary>
    /// What <paramref name="who"/>'s Star Pitch costs their team (§12): the carrier's price (a captain's or a
    /// sidekick's), plus <c>costs.guestCaptainSurcharge</c> when a captain acts for a team <paramref name="teamCaptain"/> captains.
    /// </summary>
    public static int PitchCost(Character who, Character teamCaptain, RulesTable rules, StarSkillTable? table = null) =>
        Price(who, teamCaptain, rules);

    /// <summary>What <paramref name="who"/>'s Star Swing costs his team: the same rule as <see cref="PitchCost"/>.</summary>
    public static int SwingCost(Character who, Character teamCaptain, RulesTable rules, StarSkillTable? table = null) =>
        Price(who, teamCaptain, rules);

    static int Price(Character who, Character teamCaptain, RulesTable rules) =>
        rules.Stars.Prices.Of(who)
        + (who.Captain && !who.Id.Equals(teamCaptain.Id, StringComparison.OrdinalIgnoreCase)
            ? rules.Stars.Costs.GuestCaptainSurcharge
            : 0);

    /// <summary>
    /// How long a special owns the ball or the field before baseball resumes.
    /// Readable clip, not a full-screen blind.
    /// </summary>
    public static double SpectacleSeconds(string? id) =>
        string.IsNullOrEmpty(id) ? 0 : 2.0;
}

/// <summary>The float's bound (§13): a rise, not a lob over the backstop.</summary>
public static class PitchFloatLimits
{
    public const double MaxRiseFt = 4;
}
