namespace GrandSluggers.Sim;

/// <summary>
/// One body class (spec §8.1, CH-05, CH-11, CH-12): what a body's size and weight mean in play. Size is the reach it catches at
/// and the width of its bat's contact; weight is how fast it gets to speed and stops, and how much a hard ball knocks it back.
/// The motion style names the class's takes (<c>data/art/clips.json</c>). Every number is authored here, never read off the mesh.
/// Top speed is not a class number: it is the Run sub-stat's, on the one speed curve (§2).
/// </summary>
public sealed record BodyClassRow
{
    /// <summary>The class id a character names (<c>bodyClass</c>), lowercase kebab.</summary>
    public string Id { get; init; } = "";

    /// <summary>The class's motion style id in <c>data/art/clips.json</c> (CH-12).</summary>
    public string MotionStyle { get; init; } = "";

    /// <summary>The stand-up reach on a ball hit on the ground (a grounder or a bunt), in feet: the ring a take happens inside before the dirt pad.</summary>
    [Positive] public double GroundReachFt { get; init; }

    /// <summary>The stand-up reach on a ball hit in the air (a fly, a liner, a pop), in feet: the horizontal envelope of the ordinary catch.</summary>
    [Positive] public double FlyReachFt { get; init; }

    /// <summary>× the barrel's two half-extents along the bat (§5.2): the cursor the batter draws and the resolver judges. Its height never scales.</summary>
    [Positive] public double ContactWidthMul { get; init; }

    /// <summary>Seconds from rest to the body's rated speed on the response law (§8.1): light bodies ramp fast, heavy ones slow.</summary>
    [Positive] public double AccelSec { get; init; }

    /// <summary>Seconds from the rated speed to rest; a reversal is this brake and then the ramp.</summary>
    [Positive] public double BrakeSec { get; init; }

    /// <summary>× the recoil weight a hard take costs this body (§8.6): 1.0 takes the full knockback, a heavy body less.</summary>
    [Positive] public double KnockbackMul { get; init; }
}

/// <summary>
/// The body-class table (<c>data/rules/body-classes.json</c>, spec §8.1, §16): one row per class, room for
/// <see cref="Capacity"/>. Every character names a class (<c>bodyClass</c>); a role player that names none wears its captain's.
/// </summary>
public sealed record BodyClassLibrary
{
    /// <summary>The table's room (CH-12: about fifteen, so role players can have their own class). A row past it is a decision, not a data edit.</summary>
    public const int Capacity = 15;

    public IReadOnlyList<BodyClassRow> Classes { get; init; } = [];

    /// <summary>True when the table has a row for <paramref name="id"/>.</summary>
    public bool Has(string? id) => !string.IsNullOrEmpty(id) && Classes.Any(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    /// <summary>The row for a class id. An id with no row stops: a body playing a class nobody authored is a data error, never a fallback.</summary>
    public BodyClassRow Of(string id) =>
        Classes.FirstOrDefault(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException(
            $"'{id}' is not a body class with a row in {RulesTable.Directory}/body-classes.json; the classes are "
            + $"[{string.Join(", ", Classes.Select(c => c.Id))}]", nameof(id));

    static bool IsId(string? id) =>
        !string.IsNullOrEmpty(id) && System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-z0-9]+(-[a-z0-9]+)*$");

    internal void Validate(string source, List<string> errors)
    {
        if (Classes.Count == 0)
            errors.Add($"{source}: body-classes.classes must name at least one class");
        if (Classes.Count > Capacity)
            errors.Add($"{source}: body-classes.classes has {Classes.Count} rows; the table has room for {Capacity}");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Classes.Count; i++)
        {
            var row = Classes[i];
            var name = $"body-classes.classes[{i}]";
            if (!IsId(row.Id))
                errors.Add($"{source}: {name}.id must be a lowercase kebab id; got '{row.Id}'");
            else if (!seen.Add(row.Id))
                errors.Add($"{source}: {name}.id '{row.Id}' is named twice");
            if (!IsId(row.MotionStyle))
                errors.Add($"{source}: {name}.motionStyle must be a lowercase kebab style id; got '{row.MotionStyle}'");
            if (row.KnockbackMul > 1)
                errors.Add($"{source}: {name}.knockbackMul must be at most 1 (1 is the full knockback); got {row.KnockbackMul}");
        }
    }
}

/// <summary>
/// The body class a character plays (spec §8.1): the row its <see cref="Character.BodyClass"/> names. A character built by hand,
/// with no class, plays the unclassed body — the table's own <c>fielding.chase</c> ramp and <c>fielding.catch.standUpReachFt</c>,
/// the full knockback and the plain barrel — so a test fixture keeps the arithmetic it had before classes.
/// Every data character names a class; the validator refuses one that does not resolve.
/// </summary>
public static class BodyClasses
{
    /// <summary>The class row of <paramref name="who"/>, or the unclassed body for a character with none.</summary>
    public static BodyClassRow Of(Character who, RulesTable rules) =>
        string.IsNullOrEmpty(who.BodyClass) ? Unclassed(rules) : rules.BodyClasses.Of(who.BodyClass);

    /// <summary>The unclassed body: the table's shared ramp and reach, full knockback, the plain barrel.</summary>
    public static BodyClassRow Unclassed(RulesTable rules) => new()
    {
        Id = "",
        MotionStyle = "",
        GroundReachFt = rules.Fielding.Catch.StandUpReachFt,
        FlyReachFt = rules.Fielding.Catch.StandUpReachFt,
        ContactWidthMul = 1,
        AccelSec = rules.Fielding.Chase.AccelSec,
        BrakeSec = rules.Fielding.Chase.BrakeSec,
        KnockbackMul = 1
    };

    /// <summary>The body's ramp on the response law: seconds rest → rated speed, and rated speed → rest.</summary>
    public static (double AccelSec, double BrakeSec) Ramp(Character? who, RulesTable rules)
    {
        var row = who is null ? Unclassed(rules) : Of(who, rules);
        return (row.AccelSec, row.BrakeSec);
    }

    /// <summary>The body's stand-up reach (spec §8.3): the fly reach on a ball hit in the air, the ground reach on one hit on the ground.</summary>
    public static double ReachFt(Character who, bool air, RulesTable rules)
    {
        var row = Of(who, rules);
        return air ? row.FlyReachFt : row.GroundReachFt;
    }
}
