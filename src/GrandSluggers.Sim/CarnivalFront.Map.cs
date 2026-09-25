namespace GrandSluggers.Sim.Front;

/// <summary>
/// The continent map picker (WD-17 A, WD-18 C): the stadium row on the Exhibition setup screen opens a map of the continent,
/// drawn from the region data. The stick moves the cursor from pin to pin (<see cref="WorldMap.Step"/>), the card beside the
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

    public const string MapTitle = "THE GRAND REACH";
    public const string MapHelp = "Stick  move between parks    South  play here    East  keep your park";

    /// <summary>
    /// The card for the park under the cursor: its name, its region, whose home it is, what the field changes (the field
    /// card, <see cref="FieldCard"/>), and whether it is day or night. Read from the park and the catalog, never by park id.
    /// </summary>
    public static IReadOnlyList<string> MapCard(ContentCatalog content, string parkId, bool night, bool hazards)
    {
        var park = content.MustPark(parkId);
        var lines = new List<string> { park.Name, content.World.RegionOf(parkId).Name };
        var captain = content.CaptainIds.Select(content.Must).FirstOrDefault(c =>
            c.Faction.Equals(park.Faction, StringComparison.OrdinalIgnoreCase));
        if (captain is not null) lines.Add("Home of " + captain.Name + " · " + PresetTeams.TeamName(captain));
        lines.AddRange(FieldCard(PlayedPark.Of(park, night, hazards, content.Rules.Hazards), content.Rules));
        lines.Add(night ? "Night game: lights on" : "Day game");
        return lines;
    }
}
