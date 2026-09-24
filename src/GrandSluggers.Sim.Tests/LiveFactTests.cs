using System.Reflection;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The live ball records what bodies did as typed facts (<see cref="LiveFact"/>) that any reader consumes; it keeps no
/// field for one consumer. A lesson that needs a new receipt adds a fact, not a <c>Tutorial…</c> member on the core.
/// </summary>
public sealed class LiveFactTests
{
    [Fact]
    public void TheLiveSystemKeepsNoMemberForOneConsumer()
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        var named = typeof(LivePlaySystem).GetMembers(all)
            .Where(m => m.Name.Contains("Tutorial", StringComparison.Ordinal)).Select(m => m.Name).Distinct().ToList();
        Assert.Empty(named);
    }

    [Fact]
    public void EveryFactIsARecordWithItsGlove()
    {
        var facts = typeof(LiveFact).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(LiveFact))).ToList();
        Assert.Contains(typeof(ReachBonusTake), facts);
        Assert.Contains(typeof(AssistedRouteStep), facts);
        Assert.All(facts, t => Assert.NotNull(t.GetProperty("GloveId")));
    }
}
