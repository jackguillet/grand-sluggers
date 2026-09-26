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
    PitchLoop? Loop = null);

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
    bool Fragments,
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
    double PerfectRingMul = 1)
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
