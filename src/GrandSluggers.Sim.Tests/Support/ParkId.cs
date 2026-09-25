namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The catalog's park ids (<c>data/parks/&lt;id&gt;.json</c>). Tests name a park through these, so a renamed park
/// breaks here and in <see cref="All"/>'s test, not in a few hundred strings.
/// </summary>
static class ParkId
{
    public const string Harbor = "harbor-diamond";
    public const string Crystal = "crystal-rink";
    public const string Canopy = "canopy-yard";
    public const string Ember = "ember-keep";
    public const string Funfair = "funfair-park";
    public const string Rooftop = "rooftop-city";
    public const string Stillwater = "stillwater-marsh";
    public const string Coconut = "coconut-cove";
    public const string Sunscorch = "sunscorch-mesa";
    public const string Summit = "summit-park";

    public static readonly IReadOnlyList<string> All = [Harbor, Crystal, Canopy, Ember, Funfair, Rooftop, Stillwater, Coconut, Sunscorch, Summit];
}
