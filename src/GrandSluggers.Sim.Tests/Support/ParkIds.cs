namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The catalog's park ids, spelled once for every test (each equals its file name in <c>data/parks</c>, which the
/// content load checks). A renamed park fails to compile here instead of failing a string compare in 200 places.
/// </summary>
static class ParkIds
{
    public const string Harbor = "harbor-diamond";
    public const string Crystal = "crystal-rink";
    public const string Ember = "ember-keep";
    public const string Funfair = "funfair-park";
    public const string Canopy = "canopy-yard";
    public const string Rooftop = "rooftop-city";
}
