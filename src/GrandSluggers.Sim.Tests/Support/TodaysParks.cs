using System.Text.Json;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Which catalog parks still play at today's numbers (F9-a): no air of their own, no fence points, and every zone on a
/// ground row equal to grass's. A parity row that replays the pre-move code in "every park" means these; a park that
/// names a difference (Crystal since F9-a) is held by its own rows instead, so the parity is still proved for every other
/// park and the next park that names one drops out of it by its data, not by its id.
/// </summary>
static class TodaysParks
{
    public static bool AtTodaysNumbers(Park park, RulesTable rules)
    {
        if (park.Environment is not null || park.Fence is not null) return false;
        var zones = GroundZones.Of(park, rules);
        var grass = JsonSerializer.Serialize(rules.Grounds.Of(Ground.Grass));
        foreach (var id in new[] { zones.InfieldDirt, zones.Outfield, zones.WarningTrack, zones.FoulApron })
            if (JsonSerializer.Serialize(rules.Grounds.Of(id)) != grass) return false;
        return true;
    }

    /// <summary>The catalog's parks at today's numbers, in catalog order.</summary>
    public static IEnumerable<Park> Of(ContentCatalog catalog) =>
        catalog.Parks.Values.Where(p => AtTodaysNumbers(p, catalog.Rules));
}
