namespace GrandSluggers.Sim;

/// <summary>
/// The closed hazard library (§0.3 D21, §14; FD-09, FR-02): every id a park may name in a
/// <c>hazards[]</c> row, spelled the way <c>data/parks/*.json</c> spells it. The shape is
/// <see cref="PitchFamily"/>'s (D20): this list is the library, and <c>data/rules/hazards.json</c>
/// (<see cref="HazardRules"/>) carries one authored row per id. An id outside this list and an id
/// inside it with no authored row are different mistakes, and neither one loads (SF-03).
///
/// <para>
/// The ids are the data's spelling, not C#'s, because a park file is where they are written and a
/// second spelling would be a second source of truth. <see cref="Key"/> derives the table key from
/// the id, so the library and the table cannot drift apart by a rename.
/// </para>
/// </summary>
public static class HazardType
{
    /// <summary>Crystal's freezers.</summary>
    public const string FreezeVolume = "freeze_volume";

    /// <summary>Ember's lava.</summary>
    public const string LavaPit = "lava_pit";

    /// <summary>Ember's breath, the one volume night widens.</summary>
    public const string FireBreath = "fire_breath";

    /// <summary>Funfair's warp cans.</summary>
    public const string WarpPipe = "warp_pipe";

    /// <summary>Canopy's barrel cannons.</summary>
    public const string Barrel = "barrel";

    /// <summary>Rooftop's star signs.</summary>
    public const string Billboard = "billboard";

    /// <summary>Canopy's climbable wall.</summary>
    public const string ClimbWall = "climb_wall";

    /// <summary>Funfair's night mouths (park data since #847; a code literal before it).</summary>
    public const string Chomper = "chomper";

    /// <summary>Ember's captain statue.</summary>
    public const string Statue = "statue";

    /// <summary>Funfair's boxcar.</summary>
    public const string Train = "train";

    /// <summary>Rooftop's air-conditioning units.</summary>
    public const string AcUnit = "ac_unit";

    /// <summary>Canopy's trees.</summary>
    public const string Tree = "tree";

    /// <summary>Every type, in library order: the acting patterns first, then the decorations.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        FreezeVolume, LavaPit, FireBreath, WarpPipe, Barrel, Billboard, ClimbWall, Chomper,
        Statue, Train, AcUnit, Tree
    ];

    static readonly HashSet<string> KnownIds = new(All, StringComparer.Ordinal);

    /// <summary>True for one of the twelve library ids, spelled the way the library spells it.</summary>
    public static bool IsKnown(string? type) => type is not null && KnownIds.Contains(type);

    /// <summary>
    /// The key a type's row is authored under in <c>hazards.json</c>: the id in the camelCase every
    /// rule table spells a key in. Derived rather than listed, so a type cannot carry one name in the
    /// library and another in the table.
    /// </summary>
    public static string Key(string type)
    {
        var parts = type.Split('_');
        var key = parts[0];
        for (var i = 1; i < parts.Length; i++)
            key += parts[i].Length == 0 ? "" : char.ToUpperInvariant(parts[i][0]) + parts[i][1..];
        return key;
    }
}

/// <summary>
/// What a hazard type <em>does</em> (FD-09, FR-08): the closed set of patterns the sim implements.
/// A hazard type is a data row that picks one of these; a ninth park needs rows, not code.
///
/// <para>
/// The set is what the sim does <b>today</b>, at parity. The matrix in
/// <c>docs/decisions/plan-fields.md</c> also names a solid body and a timed mover; neither has any sim code,
/// so the four types that would take them (<c>statue</c>, <c>train</c>, <c>ac_unit</c>,
/// <c>tree</c>) carry <see cref="Decoration"/> — an honest row that says the type changes no play —
/// until the child that builds their pattern arrives (map §5 Q6, Jack, September 22, 2026).
/// </para>
/// </summary>
public static class HazardPattern
{
    /// <summary>A disc that slows the chase (<c>freeze_volume</c>, <c>lava_pit</c>, <c>fire_breath</c>).</summary>
    public const string StatusVolume = "statusVolume";

    /// <summary>A mouth a ball enters and leaves out of another of its type (<c>warp_pipe</c>, <c>barrel</c>, <c>chomper</c>; F4-c).</summary>
    public const string BallRedirect = "ballRedirect";

    /// <summary>A target that pays the batting team when the ball hits it (<c>billboard</c>).</summary>
    public const string RewardTarget = "rewardTarget";

    /// <summary>A body the ball caroms off and a fielder cannot stand in (<c>statue</c>, <c>ac_unit</c>, <c>tree</c>; F4-f).</summary>
    public const string SolidBody = "solidBody";

    /// <summary>A solid body that moves along the fence on the play clock (<c>train</c>; F4-f).</summary>
    public const string TimedMover = "timedMover";

    /// <summary>A property of the wall a fielder works at (<c>climb_wall</c>).</summary>
    public const string WallTrait = "wallTrait";

    /// <summary>Drawn, never played: the type has no effect on a play at all.</summary>
    public const string Decoration = "decoration";

    /// <summary>Every pattern, in the order §14 lists them.</summary>
    public static IReadOnlyList<string> All { get; } =
        [StatusVolume, BallRedirect, RewardTarget, SolidBody, TimedMover, WallTrait, Decoration];

    static readonly HashSet<string> KnownIds = new(All, StringComparer.Ordinal);

    /// <summary>True for one of the seven patterns the sim implements (the catch stealer retired into the redirect, FD-09-R2).</summary>
    public static bool IsKnown(string? pattern) => pattern is not null && KnownIds.Contains(pattern);

    /// <summary>
    /// The patterns that count as a hazard, which are the ones the match's hazards switch removes
    /// (FD-10, §14, SF-24): a status volume, a ball redirect, a reward target, a solid body and a timed mover —
    /// the five that act on a play. <see cref="WallTrait"/> is not one, because a climbable span is a
    /// property of the wall (FD-06) and the park keeps its walls with hazards off. <see cref="Decoration"/>
    /// is not one, because it does nothing in play and the kit still draws it.
    ///
    /// <para>
    /// This is the one place the choice is written: a property of the pattern set, so a type is in or
    /// out by the pattern its row names, never by a per-park list or a test on a type string. It is
    /// contract work FD-10 left open, written by F4-h (#858) and confirmed by Jack (4. a, September 22,
    /// 2026).
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> Hazards { get; } = [StatusVolume, BallRedirect, RewardTarget, SolidBody, TimedMover];

    static readonly HashSet<string> HazardIds = new(Hazards, StringComparer.Ordinal);

    /// <summary>True for a pattern that counts as a hazard (<see cref="Hazards"/>): the switch removes an instance of it.</summary>
    public static bool IsHazard(string? pattern) => pattern is not null && HazardIds.Contains(pattern);

    /// <summary>
    /// The park a match plays with hazards off (FD-10, SF-24): the same park with every instance whose
    /// type's pattern <see cref="IsHazard">is a hazard</see> removed, and nothing else. Its size, fence,
    /// walls, air, wind, ground zones, foul territory and depth are the same values, and the
    /// <see cref="WallTrait"/> and <see cref="Decoration"/> instances stay, in their authored order.
    ///
    /// <para>
    /// It reads each instance's row from <paramref name="library"/> — the same table
    /// <see cref="ParkHazards"/> dispatches on — and does not dispatch: nothing here decides what a
    /// hazard does. A park that has no hazard instance comes back as itself, so a hazards-off match at
    /// Harbor plays the catalog's own park object. A match reaches it through <see cref="PlayedPark.Of"/>,
    /// after the night block is resolved, so the night's instances go with the day's (FD-10-R1).
    /// </para>
    /// </summary>
    public static Park HazardsOff(Park park, HazardRules library)
    {
        var kept = park.Hazards.Where(h => !IsHazard(library.Of(h.Type).Pattern)).ToArray();
        return kept.Length == park.Hazards.Count ? park : park with { Hazards = kept };
    }
}

/// <summary>
/// The one resolution of a park into the park a match plays (§0.3, §14; FD-11 B, FD-11-R2, FD-10,
/// FD-10-R1; F4-d). A match resolves its park once, and every reader — the at-bat, the fielding preview,
/// the live ball, the CPU, the trace and the presentation — reads the <see cref="Park.Hazards"/> of the
/// park this returns. Nothing else reads a <see cref="ParkNight"/>.
///
/// <para>
/// The order is fixed: the day's instances; then, at night, the night block's, after them, in the order
/// the file lists them; then, with hazards off, the switch (<see cref="HazardPattern.HazardsOff"/>). The
/// played park carries no night block — its night is already in its hazard list, or it is day — so
/// resolving it again, as <c>ParkView</c> does for the title park and the match's alike, is itself.
/// </para>
///
/// <para>
/// <b>This is the one place night reaches a park.</b> The other night read in the sim is a hazard type's
/// own night number (<c>fireBreath.nightRadiusMul</c>, <see cref="ParkHazards.NightDiscFt"/>), which
/// FD-11-R2 keeps: it widens an instance at night wherever the instance is authored. Night changes no
/// rule of the at-bat, the flight, the ground or the bodies (the contact window's night multiplier is
/// gone on both roots). A park with no night block comes back as itself, day or night, so every park but
/// Funfair plays the catalog's own object exactly as before.
/// </para>
/// </summary>
public static class PlayedPark
{
    /// <summary>
    /// The park a match plays at <paramref name="park"/>, by day or at night, with hazards on or off,
    /// reading each instance's pattern from <paramref name="library"/> when hazards are off.
    /// </summary>
    public static Park Of(Park park, bool night, bool hazards, HazardRules library)
    {
        var tonight = park.Night is not { } block
            ? park
            : park with { Hazards = night ? [.. park.Hazards, .. block.Hazards] : park.Hazards, Night = null };
        return hazards ? tonight : HazardPattern.HazardsOff(tonight, library);
    }
}
