namespace GrandSluggers.Sim;

/// <summary>
/// One place on the continent (<c>data/world/regions.json</c>): its id, the name a player reads, and where it sits on a
/// unit map — <see cref="X"/> 0 is the west coast and 1 the east, <see cref="Y"/> 0 the north and 1 the south.
/// <see cref="Island"/> marks the region off the coast.
/// </summary>
public sealed record Region(string Id, string Name, double X, double Y, bool Island);

/// <summary>
/// The continent the parks stand on (WD-05, WD-19): its name and its regions in map order. A park names its region
/// (<c>region</c> in its park file); no two parks share one, and a region with no park yet waits for its park file. It is
/// catalog data for the map and the menus: no rule of play reads it.
/// </summary>
public sealed class WorldMap
{
    public const string Directory = "world";
    public const string FileName = "regions.json";

    public WorldMap(string continent, IReadOnlyList<Region> regions, IReadOnlyDictionary<string, string> regionOfPark)
    {
        Continent = continent;
        Regions = regions;
        _byId = regions.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
        _regionOfPark = regionOfPark;
    }

    /// <summary>The continent's name as a player reads it.</summary>
    public string Continent { get; }

    /// <summary>The regions in the file's order.</summary>
    public IReadOnlyList<Region> Regions { get; }

    readonly IReadOnlyDictionary<string, Region> _byId;
    readonly IReadOnlyDictionary<string, string> _regionOfPark;

    /// <summary>The region a park stands in. Every park names one; the validator refuses a park that does not.</summary>
    public Region RegionOf(string parkId) =>
        _regionOfPark.TryGetValue(parkId, out var id) ? _byId[id]
            : throw new KeyNotFoundException($"No park '{parkId}' in the world map");

    /// <summary>The park that stands in a region, or null while the region waits for its park file.</summary>
    public string? ParkIn(string regionId) =>
        _regionOfPark.FirstOrDefault(kv => kv.Value.Equals(regionId, StringComparison.OrdinalIgnoreCase)).Key;
}

internal sealed class WorldDto
{
    public string Continent { get; set; } = "";
    public List<RegionDto?>? Regions { get; set; }
}

internal sealed class RegionDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public double? X { get; set; }
    public double? Y { get; set; }
    public bool? Island { get; set; }
}
