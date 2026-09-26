namespace GrandSluggers.Sim.Front;

/// <summary>
/// The continent map picker (WD-17 A, WD-18 C): the stadium row on the Exhibition setup screen opens a map of the continent,
/// drawn from the region data, with the neighborhood field in an inset of its own and a portal across (WD-26). The stick moves the cursor from pin to pin (<see cref="WorldMap.Step"/>), the card beside the
/// map reads the park under the cursor, South picks it and East keeps the park you had. Layout in the 1280 × 800 setup canvas.
/// </summary>
public static partial class CarnivalFront
{
    /// <summary>The map's panel: the left of the setup screen, clear of the step label and the help line.</summary>
    public static CaptainPanelRect MapPanel { get; } = new(24, 70, 820, 600);

    /// <summary>The unit map's margin inside the panel, so a pin and its label never sit on the panel's edge.</summary>
    public const float MapInset = 48;

    /// <summary>The card beside the map: the park under the cursor.</summary>
    public static CaptainPanelRect MapCardPanel { get; } = new(864, 70, 392, 600);

    /// <summary>A pin's size on the map, the cursor's (the pin grown and ringed), and its label's box under it.</summary>
    public const float MapPinSize = 22;
    public const float MapCursorSize = MapPinSize * 1.4f + 8;
    public const float MapLabelW = 150;
    public const float MapLabelH = 22;

    /// <summary>Where a region's pin stands in the canvas (its centre).</summary>
    public static (float X, float Y) MapPin(Region region) =>
        (MapPanel.X + MapInset + (float)region.X * (MapPanel.W - 2 * MapInset),
         MapPanel.Y + MapInset + (float)region.Y * (MapPanel.H - 2 * MapInset));

    /// <summary>A pin's label box, centred under the pin.</summary>
    public static CaptainPanelRect MapLabel(Region region)
    {
        var (x, y) = MapPin(region);
        return new(x - MapLabelW / 2, y + MapCursorSize / 2 + 2, MapLabelW, MapLabelH);
    }

    /// <summary>
    /// The inset that holds the region apart from the continent (WD-26): a small panel of its own in the open sea around its
    /// pin, its name in a band on top, its pin and label inside. The continent's greybox is never drawn under it.
    /// </summary>
    public const float MapApartW = 184;
    public const float MapApartTitleH = 20;
    public const float MapApartPad = 6;

    public static CaptainPanelRect MapApartPanel(Region region)
    {
        var (x, y) = MapPin(region);
        var label = MapLabel(region);
        var top = y - MapCursorSize / 2 - MapApartTitleH - MapApartPad;
        return new(x - MapApartW / 2, top, MapApartW, label.Y + label.H + MapApartPad - top);
    }

    /// <summary>The inset's name band: the region's name as a player reads it.</summary>
    public static CaptainPanelRect MapApartTitle(Region region)
    {
        var box = MapApartPanel(region);
        return new(box.X, box.Y, box.W, MapApartTitleH);
    }

    /// <summary>The portal ring's outer size and the hole inside it (a placeholder until the map art slot is placed).</summary>
    public const float MapPortalSize = 34;
    public const float MapPortalHole = 18;

    /// <summary>The portal's centre in the canvas: the apart region's portal on the unit map.</summary>
    public static (float X, float Y) MapPortal(Region apart)
    {
        var p = apart.Portal ?? new MapPoint(apart.X, apart.Y);
        return (MapPanel.X + MapInset + (float)p.X * (MapPanel.W - 2 * MapInset),
            MapPanel.Y + MapInset + (float)p.Y * (MapPanel.H - 2 * MapInset));
    }

    /// <summary>The spacing and size of the sparks that run from the inset through the portal to the continent.</summary>
    public const float MapSparkStep = 14;
    public const float MapSparkSize = 6;

    /// <summary>
    /// The portal's sparks: dots from the apart region's pin to the portal, and from the portal to the continent region it
    /// opens onto (<see cref="WorldMap.PortalShore"/>), every <see cref="MapSparkStep"/>. A spark never sits on the inset, the
    /// portal, a pin or a label, so the path reads as a way across and never hides a park. Empty when the world has no
    /// apart region.
    /// </summary>
    public static IReadOnlyList<(float X, float Y)> MapPortalSparks(WorldMap world)
    {
        var sparks = new List<(float X, float Y)>();
        if (world.ApartRegion is not { } apart || world.PortalShore() is not { } shore) return sparks;
        var portal = MapPortal(apart);
        var inset = MapApartPanel(apart);
        var r = MapSparkSize / 2;
        bool Clear((float X, float Y) p)
        {
            if (Overlaps(inset, p, r)) return false;
            if (Dist(p, portal) < MapPortalSize / 2 + MapSparkStep / 2) return false;
            foreach (var region in world.Regions)
            {
                if (Dist(p, MapPin(region)) < MapCursorSize / 2 + r + 2) return false;
                if (Overlaps(MapLabel(region), p, r)) return false;
            }
            return true;
        }
        foreach (var (from, to) in new[] { (MapPin(apart), portal), (portal, MapPin(shore)) })
        {
            var length = Dist(from, to);
            var n = (int)(length / MapSparkStep);
            for (var i = 1; i < n; i++)
            {
                var t = i / (float)n;
                var p = (from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t);
                if (Clear(p)) sparks.Add(p);
            }
        }
        return sparks;
    }

    static float Dist((float X, float Y) a, (float X, float Y) b) =>
        MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    static bool Overlaps(CaptainPanelRect box, (float X, float Y) p, float r) =>
        p.X + r > box.X && p.X - r < box.X + box.W && p.Y + r > box.Y && p.Y - r < box.Y + box.H;

    public const string MapTitle = "THE GRAND REACH";
    /// <summary>The card's region line for the park apart from the continent: how you get there.</summary>
    public const string MapThroughPortal = "through the portal";
    public const string MapHelp = "Stick  move between parks    South  play here    East  keep your park";

    /// <summary>
    /// The card for the park under the cursor: its name, its region, whose home it is, what the field changes (the field
    /// card, <see cref="FieldCard"/>), and whether it is day or night. Read from the park and the catalog, never by park id.
    /// </summary>
    public static IReadOnlyList<string> MapCard(ContentCatalog content, string parkId, bool night, bool hazards)
    {
        var park = content.MustPark(parkId);
        var region = content.World.RegionOf(parkId);
        var lines = new List<string> { park.Name, region.Apart ? region.Name + " · " + MapThroughPortal : region.Name };
        var captain = content.CaptainIds.Select(content.Must).FirstOrDefault(c =>
            c.Faction.Equals(park.Faction, StringComparison.OrdinalIgnoreCase));
        if (captain is not null) lines.Add("Home of " + captain.Name + " · " + PresetTeams.TeamName(captain));
        lines.AddRange(FieldCard(PlayedPark.Of(park, night, hazards, content.Rules.Hazards), content.Rules));
        lines.Add(night ? "Night game: lights on" : "Day game");
        return lines;
    }
}
