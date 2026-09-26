namespace GrandSluggers.Sim;

/// <summary>
/// A sidekick species (WD-27, <c>data/world/species.json</c>): an animal hybrid or a made-up creature themed to its park
/// (the neighborhood's are human kids). Each captain's faction has three, one of each <see cref="Build"/>; a sidekick
/// names its species, wears its body and carries its field ability (AB-12: the sidekick's row names none). The sidekick's
/// own row still names its specials.
/// </summary>
public sealed record Species(
    string Id, string Name, string Faction, string Blend, string Build, string Look,
    string StarPitch, string StarSwing, string FieldAbility, Silhouette.Spec Proportions);

/// <summary>The three sidekick builds (WD-27): power, speed and glove.</summary>
public static class SpeciesBuilds
{
    public const string Bruiser = "bruiser";
    public const string Scamp = "scamp";
    public const string Glove = "glove";
    public static readonly IReadOnlyList<string> All = [Bruiser, Scamp, Glove];
    /// <summary>How many sidekicks each captain brings (WD-27).</summary>
    public const int SidekicksPerCaptain = 8;
}

internal sealed class SpeciesFile
{
    public Dictionary<string, ProportionsDto?>? Builds { get; set; }
    public List<SpeciesDto?>? Species { get; set; }
}

internal sealed class SpeciesDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Faction { get; set; } = "";
    public string Blend { get; set; } = "";
    public string Build { get; set; } = "";
    public string Look { get; set; } = "";
    public string StarPitch { get; set; } = "";
    public string StarSwing { get; set; } = "";
    public string FieldAbility { get; set; } = "";
    /// <summary>The species' own body, when it is not its build's (the human kids).</summary>
    public ProportionsDto? Proportions { get; set; }
}
