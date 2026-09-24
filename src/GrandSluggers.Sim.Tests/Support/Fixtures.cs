namespace GrandSluggers.Sim.Tests;

/// <summary>The checked-in expectations under <c>src/GrandSluggers.Sim.Tests/fixtures/</c>, which a <see cref="WriterFactAttribute"/> test regenerates.</summary>
static class Fixtures
{
    public static string PathOf(string name) => Path.Combine(
        Path.GetFullPath(Path.Combine(Shipped.Content.Root.Shipped, "..")),
        "src", "GrandSluggers.Sim.Tests", "fixtures", name);
}
