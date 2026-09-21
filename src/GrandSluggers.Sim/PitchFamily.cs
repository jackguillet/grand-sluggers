namespace GrandSluggers.Sim;

/// <summary>
/// The shared ordinary pitch library (spec §4.3, PH-02-R2): the one closed set of family ids a
/// character's <see cref="Repertoire"/> may be drawn from. Ids are lowercase and stable, like every
/// other content id.
///
/// <b>This is not the star pitch namespace.</b> <c>data/abilities/star-skills.json</c> already
/// spells two of its own skills <c>fastball</c> and <c>changeup</c>, and
/// <see cref="Character.StarPitch"/> may equal <c>"changeup"</c> while that same character's
/// ordinary repertoire carries no changeup at all. The two sets are never compared, resolved
/// against each other, or validated against each other: a star pitch is a skill row with its own
/// speed and stamina numbers and sits outside the three-pitch count (§13, PH-02-R1), while a family
/// is the shape an ordinary pitch is selected from before the charge (PH-02-R3/R4/R5). A star id
/// that happens to spell a family is a coincidence of vocabulary, not a reference.
///
/// #807 lays the data rail only. <see cref="Curveball"/>, <see cref="Slider"/> and
/// <see cref="Sinker"/> have no flight and cannot be selected yet; the family library table is
/// P1-b and the RB/Tab selection verb is P1-c.
/// </summary>
public static class PitchFamily
{
    /// <summary>Speed pressure, relatively straight. Every pitcher throws it (PH-15-R1).</summary>
    public const string Fastball = "fastball";

    /// <summary>Slower delivery that punishes early swings.</summary>
    public const string Changeup = "changeup";

    /// <summary>Pronounced arc and drop. No flight yet (#807).</summary>
    public const string Curveball = "curveball";

    /// <summary>Sideways movement that challenges coverage. No flight yet (#807).</summary>
    public const string Slider = "slider";

    /// <summary>A faster dipping alternative to the curveball. No flight yet (#807).</summary>
    public const string Sinker = "sinker";

    /// <summary>Every family, in library order (PH-02-R2). The fastball leads because everyone has it.</summary>
    public static IReadOnlyList<string> All { get; } = [Fastball, Changeup, Curveball, Slider, Sinker];

    /// <summary>
    /// The four a character's second and third pitch are chosen from (PH-15-R1). The fastball is
    /// implied by the repertoire, so it is never assignable and never authored in a data row.
    /// </summary>
    public static IReadOnlyList<string> Assignable { get; } = [Changeup, Curveball, Slider, Sinker];

    static readonly HashSet<string> KnownIds = new(All, StringComparer.Ordinal);
    static readonly HashSet<string> AssignableIds = new(Assignable, StringComparer.Ordinal);

    /// <summary>True for one of the five library ids, spelled the way the library spells it.</summary>
    public static bool IsKnown(string family) => KnownIds.Contains(family);

    /// <summary>True for a family a <see cref="Repertoire"/> may carry beside the fastball.</summary>
    public static bool IsAssignable(string family) => AssignableIds.Contains(family);
}
