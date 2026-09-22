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
/// <c>docs/plan-fields.md</c> also names a solid body and a timed mover; neither has any sim code,
/// so the four types that would take them (<c>statue</c>, <c>train</c>, <c>ac_unit</c>,
/// <c>tree</c>) carry <see cref="Decoration"/> — an honest row that says the type changes no play —
/// until the child that builds their pattern arrives (map §5 Q6, Jack, September 22, 2026).
/// </para>
/// </summary>
public static class HazardPattern
{
    /// <summary>A disc that slows the chase (<c>freeze_volume</c>, <c>lava_pit</c>, <c>fire_breath</c>).</summary>
    public const string StatusVolume = "statusVolume";

    /// <summary>A mouth a ball on the ground enters and leaves somewhere else (<c>warp_pipe</c>, <c>barrel</c>).</summary>
    public const string BallRedirect = "ballRedirect";

    /// <summary>A target that pays the batting team when the ball hits it (<c>billboard</c>).</summary>
    public const string RewardTarget = "rewardTarget";

    /// <summary>A mouth that takes a fly out of the air before a glove can (<c>chomper</c>).</summary>
    public const string CatchStealer = "catchStealer";

    /// <summary>A property of the wall a fielder works at (<c>climb_wall</c>).</summary>
    public const string WallTrait = "wallTrait";

    /// <summary>Drawn, never played: the type has no effect on a play at all.</summary>
    public const string Decoration = "decoration";

    /// <summary>Every pattern, in the order §14 lists them.</summary>
    public static IReadOnlyList<string> All { get; } =
        [StatusVolume, BallRedirect, RewardTarget, CatchStealer, WallTrait, Decoration];

    static readonly HashSet<string> KnownIds = new(All, StringComparer.Ordinal);

    /// <summary>True for one of the six patterns the sim implements.</summary>
    public static bool IsKnown(string? pattern) => pattern is not null && KnownIds.Contains(pattern);
}
