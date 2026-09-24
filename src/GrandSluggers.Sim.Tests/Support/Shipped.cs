using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>The shipped catalog, loaded once for the tests that only read it (the captain list, a body's proportions).</summary>
static class Shipped
{
    static readonly Lazy<ContentCatalog> _content = new(() => ContentCatalog.Load());

    public static ContentCatalog Content => _content.Value;

    /// <summary>The captains in select order (<c>data/teams/teams.json</c>).</summary>
    public static IReadOnlyList<string> CaptainIds => Content.CaptainIds;
}
