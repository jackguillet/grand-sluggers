namespace GrandSluggers.Sim;

/// <summary>
/// One place on the continent (<c>data/world/regions.json</c>): its id, the name a player reads, and where it sits on a
/// unit map — <see cref="X"/> 0 is the west coast and 1 the east, <see cref="Y"/> 0 the north and 1 the south.
/// <see cref="Island"/> marks the region off the coast. <see cref="Apart"/> marks the one region that is not on the continent
/// at all (WD-25, WD-26: the neighborhood field, in another world): the map draws it in its own inset at its place and draws
/// a <see cref="Portal"/> between it and the continent, on the same unit map.
/// </summary>
public sealed record Region(string Id, string Name, double X, double Y, bool Island, bool Apart = false, MapPoint? Portal = null);

/// <summary>A point on the world's unit map (x east, y south, both 0 to 1).</summary>
public readonly record struct MapPoint(double X, double Y);

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

    /// <summary>The region apart from the continent (WD-26), reached through the portal, or null when the world has none.</summary>
    public Region? ApartRegion => Regions.FirstOrDefault(r => r.Apart);

    /// <summary>
    /// The continent region the portal opens onto: the one on the Grand Reach nearest the portal. The map draws the portal's
    /// sparks from the apart region to it. Null when the world has no apart region.
    /// </summary>
    public Region? PortalShore()
    {
        if (ApartRegion is not { Portal: { } portal }) return null;
        return Regions.Where(r => !r.Apart)
            .OrderBy(r => (r.X - portal.X) * (r.X - portal.X) + (r.Y - portal.Y) * (r.Y - portal.Y))
            .ThenBy(r => r.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    readonly IReadOnlyDictionary<string, Region> _byId;
    readonly IReadOnlyDictionary<string, string> _regionOfPark;

    /// <summary>The region a park stands in. Every park names one; the validator refuses a park that does not.</summary>
    public Region RegionOf(string parkId) =>
        _regionOfPark.TryGetValue(parkId, out var id) ? _byId[id]
            : throw new KeyNotFoundException($"No park '{parkId}' in the world map");

    /// <summary>
    /// The park the map cursor moves to from <paramref name="fromPark"/> when the stick points (<paramref name="dx"/>,
    /// <paramref name="dy"/>) in map terms (x east, y south): the nearest other park inside a 60° cone either side of the
    /// stick, distance weighted by how far off the stick it lies. No park in the cone keeps the cursor where it is.
    /// </summary>
    public string Step(string fromPark, double dx, double dy)
    {
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-9) return fromPark;
        var (ux, uy) = (dx / len, dy / len);
        var from = RegionOf(fromPark);
        string? best = null;
        var bestScore = double.MaxValue;
        foreach (var (park, regionId) in _regionOfPark)
        {
            if (park.Equals(fromPark, StringComparison.OrdinalIgnoreCase)) continue;
            var r = _byId[regionId];
            var (vx, vy) = (r.X - from.X, r.Y - from.Y);
            var d = Math.Sqrt(vx * vx + vy * vy);
            if (d < 1e-9) continue;
            var cos = (vx * ux + vy * uy) / d;
            if (cos < 0.5) continue; // outside the 60° cone
            var score = d * (2 - cos);
            if (score < bestScore || (score == bestScore && string.CompareOrdinal(park, best) < 0))
            {
                bestScore = score;
                best = park;
            }
        }
        return best ?? fromPark;
    }

    /// <summary>The eight stick directions the map cursor reads (x east, y south).</summary>
    public static readonly IReadOnlyList<(int Dx, int Dy)> StickDirections =
        [(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)];

    /// <summary>
    /// The parks the stick cannot reach from <paramref name="fromPark"/> by any walk of <see cref="Step"/>s, in id order.
    /// Empty on a good map: the apart region is reached across the gap like any pin (the validator refuses a map that
    /// strands a park).
    /// </summary>
    public IReadOnlyList<string> Unreached(string fromPark)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { fromPark };
        var frontier = new Queue<string>([fromPark]);
        while (frontier.Count > 0)
        {
            var at = frontier.Dequeue();
            foreach (var (dx, dy) in StickDirections)
            {
                var next = Step(at, dx, dy);
                if (seen.Add(next)) frontier.Enqueue(next);
            }
        }
        return _regionOfPark.Keys.Where(p => !seen.Contains(p)).OrderBy(p => p, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The map read from the world file and the parks' regions. The caller has validated both: every region placed, every
    /// park's region a row.
    /// </summary>
    internal static WorldMap From(WorldDto world, IReadOnlyDictionary<string, string> regionOfPark) => new(
        world.Continent,
        world.Regions!.Select(r => new Region(r!.Id, r.Name, r.X!.Value, r.Y!.Value, r.Island!.Value, r.Apart ?? false,
            r.PortalX is { } px && r.PortalY is { } py ? new MapPoint(px, py) : null)).ToList(),
        regionOfPark);

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
    /// <summary>Optional: true on the one region apart from the continent (WD-26). Absent is false.</summary>
    public bool? Apart { get; set; }
    /// <summary>The apart region's portal on the unit map; only the apart region carries one.</summary>
    public double? PortalX { get; set; }
    public double? PortalY { get; set; }
}
