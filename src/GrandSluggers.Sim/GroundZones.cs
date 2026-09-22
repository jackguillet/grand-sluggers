namespace GrandSluggers.Sim;

/// <summary>
/// The ground library's ids (spec §6.1, §16; FD-05), spelled the way a park file spells them. A
/// closed library like <see cref="PitchFamily"/>: an id here has a row in <c>data/rules/grounds.json</c>
/// (<see cref="GroundLibrary"/>), and an id that is not here is a typo, never a fallback (FR-02,
/// <c>SF-03</c>).
///
/// <para>
/// These four are exactly the ids the park validator has always known as a <c>surface</c>
/// (<c>ContentValidation.cs</c>, a hard-coded set until F3-b). Adding a fifth is a row in the file and
/// a constant here in the same change, so the library and the table can never disagree about what
/// exists.
/// </para>
/// </summary>
public static class Ground
{
    /// <summary>The outfield of most parks.</summary>
    public const string Grass = "grass";

    /// <summary>The infield and the warning track of every park, whatever the park's <c>surface</c> is.</summary>
    public const string Dirt = "dirt";

    /// <summary>Crystal Rink's surface.</summary>
    public const string Ice = "ice";

    /// <summary>Ember Keep's surface.</summary>
    public const string Ash = "ash";

    /// <summary>Every ground, in library order. Grass leads because it is what a park is unless it says otherwise.</summary>
    public static IReadOnlyList<string> All { get; } = [Grass, Dirt, Ice, Ash];

    static readonly HashSet<string> KnownIds = new(All, StringComparer.Ordinal);

    /// <summary>True for a library id, spelled the way the library spells it.</summary>
    public static bool IsKnown(string? id) => id is not null && KnownIds.Contains(id);
}

/// <summary>
/// The wall-material library's ids (spec §6.1, §16; FD-06). One today: every fence span of every park
/// is the padded wall the ball caroms off now. A span names one of these once the polyline fence lands
/// (F2-c); a material with no row in <c>data/rules/walls.json</c> is a stop (<c>SF-03</c>).
/// </summary>
public static class WallMaterial
{
    /// <summary>The padded outfield wall.</summary>
    public const string Padded = "padded";

    /// <summary>Every material, in library order.</summary>
    public static IReadOnlyList<string> All { get; } = [Padded];

    static readonly HashSet<string> KnownIds = new(All, StringComparer.Ordinal);

    /// <summary>True for a library id, spelled the way the library spells it.</summary>
    public static bool IsKnown(string? id) => id is not null && KnownIds.Contains(id);

    /// <summary>
    /// Which material one segment of the boundary is made of — the one place that question is answered
    /// (FD-06, F3-c). Every segment of every park, the fence between the poles and the foul wrap alike,
    /// is <see cref="Padded"/> until the polyline fence (F2-c) gives a span its own material; F2-c
    /// changes this function, and no caller spells a material. The carom takes the row for the answer
    /// out of <see cref="WallMaterialLibrary.Of"/>, which stops on a material with no row rather than
    /// caroming off the first one.
    /// </summary>
    public static string OfSegment(FieldBounds.WallSegment segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        return Padded;
    }
}

/// <summary>
/// The four zones a park's ground is made of (FD-05 B). They are the zones the shared diamond already
/// has — the dirt inside the lip, the grass beyond it, the track against the fence and the apron
/// outside the chalk — so no new shape language exists and nothing about the field moved to make them.
/// </summary>
public enum GroundZone
{
    /// <summary>Inside the infield lip: the dirt the infielders own the hop on.</summary>
    InfieldDirt,

    /// <summary>Fair, past the lip, short of the track.</summary>
    Outfield,

    /// <summary>Fair, within a track width of the fence.</summary>
    WarningTrack,

    /// <summary>Outside the chalk, the ground behind the plate included.</summary>
    FoulApron
}

/// <summary>
/// One park's ground, resolved: which ground each of its four zones names, and the geometry that says
/// which zone a point is in (spec §6.1, §16; FD-05, F3-b). This is the one function the ball and the
/// loose-ball models (F3-c) and the body multipliers (F3-d) call — ask <see cref="RowAt"/> for a point
/// and get the row of the zone it is in, out of <see cref="GroundLibrary"/>.
///
/// <para>
/// <b>Why this lives here and not on the resolved rules table.</b> <see cref="RulesTable.AtPark"/>
/// returns the global table <em>itself</em> when a park names no air, and <c>SF-01</c> is the
/// assertion that it does — a per-park zone map hung on that table would make every park's table a
/// copy and break the one thing F3-a proved. So the library (what a ground <em>is</em>) stays a global
/// rules section, and the map (which ground this park's zones <em>name</em>) is resolved from the park
/// beside it, the way <see cref="ParkBoundary"/> resolves the edge. A caller holds a park and a table
/// already; it now resolves one more value from them.
/// </para>
///
/// <para>
/// <b>The boundaries are read, not chosen (#730 / #732).</b> The lip is
/// <c>flight.classes.infieldLipFt</c>, the same radius <see cref="FieldingResolver.OutfieldGrass"/>
/// splits dirt from grass at and on the same side of the comparison. The track is
/// <see cref="ParkDiamond.TrackWidth"/> inside <see cref="AtBatResolver.FenceAt"/>, which is exactly
/// where <see cref="ParkDiamond.TrackInner"/> draws it. The chalk is
/// <see cref="FieldBounds.IsFair"/>, the ±45° wedge. F3-b moved none of them and added none; the track
/// width is read as the <c>float</c> const it is, and 15f widens to 15.0 exactly, so nothing shifts by
/// a rounding (implementation map finding 13).
/// </para>
/// </summary>
public readonly record struct GroundZones
{
    /// <summary>The park these zones belong to. The fence is read from it, per bearing.</summary>
    public Park Park { get; init; }

    /// <summary>The dirt / grass lip, from <c>flight.classes.infieldLipFt</c>.</summary>
    public double InfieldLipFt { get; init; }

    /// <summary>The warning track's width, inward from the fence (<see cref="ParkDiamond.TrackWidth"/>).</summary>
    public double TrackWidthFt { get; init; }

    /// <summary>The ground inside the lip. Defaults to <see cref="Ground.Dirt"/>: the infield is dirt in every park today.</summary>
    public string InfieldDirt { get; init; }

    /// <summary>The ground past the lip. Defaults to the park's <c>surface</c>, which is what that field has always meant.</summary>
    public string Outfield { get; init; }

    /// <summary>The ground against the fence. Defaults to <see cref="Ground.Dirt"/>: the track is dirt in every park today.</summary>
    public string WarningTrack { get; init; }

    /// <summary>The ground outside the chalk. Defaults to the park's <c>surface</c>, the same as the outfield.</summary>
    public string FoulApron { get; init; }

    /// <summary>
    /// The park's zone map, on the process-wide table. Present for the callers that hold no table
    /// (the same fallback <see cref="ParkBoundary.Default"/> takes); a match hands over its own.
    /// </summary>
    public static GroundZones Of(Park park) => Of(park, null);

    /// <summary>
    /// The park's zone map on one rules table. Each zone is what the park's optional <c>zones</c> block
    /// names, else the default derived from <c>surface</c> — <c>outfield</c> and <c>foulApron</c> are
    /// the surface, <c>infieldDirt</c> and <c>warningTrack</c> are dirt. Those defaults are chosen so
    /// that <c>surface</c> keeps exactly the meaning it has: the ground of the outfield. No shipped or
    /// trial park names a <c>zones</c> block, so every park today is its surface plus dirt.
    /// </summary>
    public static GroundZones Of(Park park, RulesTable? rules)
    {
        var zones = park.Zones;
        return new GroundZones
        {
            Park = park,
            InfieldLipFt = Rules.Or(rules).Flight.Classes.InfieldLipFt,
            TrackWidthFt = ParkDiamond.TrackWidth,
            InfieldDirt = zones?.InfieldDirt ?? Ground.Dirt,
            Outfield = zones?.Outfield ?? park.Surface,
            WarningTrack = zones?.WarningTrack ?? Ground.Dirt,
            FoulApron = zones?.FoulApron ?? park.Surface
        };
    }

    /// <summary>
    /// The zone a point on the field is in. The order is the order the field is built in, and it is
    /// what decides the two places zones could overlap:
    /// <list type="number">
    /// <item>Outside the chalk (or behind the plate) is the apron, whatever its distance — the foul
    /// ground beside the infield is apron, not infield dirt, because the chalk is what separates
    /// them.</item>
    /// <item>Inside the lip is infield dirt. A park whose fence came within a track width of the lip
    /// would be a field with no outfield, and its dirt still wins; that is a degenerate park, not a
    /// rule about tracks.</item>
    /// <item>Within a track width of the fence at this bearing is the track. Past the fence reads as
    /// the track too: it is the last ground before the wall, and a ball out there is off the field
    /// anyway.</item>
    /// <item>Everything else is the outfield.</item>
    /// </list>
    /// </summary>
    public GroundZone ZoneAt(double x, double z)
    {
        if (!FieldBounds.IsFair(x, z)) return GroundZone.FoulApron;
        var fromHome = Diamond.Dist(0, 0, x, z);
        if (fromHome < InfieldLipFt) return GroundZone.InfieldDirt;
        var fence = AtBatResolver.FenceAt(Park, FieldBounds.SprayDeg(x, z));
        return fromHome >= fence - TrackWidthFt ? GroundZone.WarningTrack : GroundZone.Outfield;
    }

    /// <summary>The ground one zone names.</summary>
    public string IdOf(GroundZone zone) => zone switch
    {
        GroundZone.InfieldDirt => InfieldDirt,
        GroundZone.Outfield => Outfield,
        GroundZone.WarningTrack => WarningTrack,
        GroundZone.FoulApron => FoulApron,
        _ => throw new ArgumentOutOfRangeException(nameof(zone), zone, "the zones are the four of GroundZone")
    };

    /// <summary>The ground under a point: <see cref="ZoneAt"/> and then <see cref="IdOf"/>, in one call.</summary>
    public string GroundAt(double x, double z) => IdOf(ZoneAt(x, z));

    /// <summary>
    /// The row of the ground under a point (FD-05): <see cref="GroundAt"/>, then that id's row in the match's library.
    /// The one lookup every reader of a zone's row goes through — the ball and the loose-ball models (F3-c), a body's
    /// start, brake and cut-back and a runner's slide and overrun (F3-d) — so no reader resolves a ground its own way.
    /// An id with no row is the library's stop (<c>SF-03</c>), never a fallback.
    /// </summary>
    public GroundRules RowAt(double x, double z, GroundLibrary grounds) => grounds.Of(GroundAt(x, z));

    /// <summary>Every zone with the ground it names, in zone order. For a validator, a report or a test.</summary>
    public IEnumerable<(GroundZone Zone, string Ground)> All()
    {
        yield return (GroundZone.InfieldDirt, InfieldDirt);
        yield return (GroundZone.Outfield, Outfield);
        yield return (GroundZone.WarningTrack, WarningTrack);
        yield return (GroundZone.FoulApron, FoulApron);
    }
}
