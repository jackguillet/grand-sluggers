namespace GrandSluggers.Sim;

/// <summary>
/// One batter's strike zone at the plate crossing, in world feet (spec §4.4): from the mid-thigh landmark to the chest
/// landmark of the body this batter wears, at the rest rig, clamped by <c>pitching.zone</c>. The width is the plate's
/// and is the same for every batter (<see cref="StrikeZoneGeometry.HalfWidth"/>); the height scales with the body.
/// The pose never moves it: it is read from the rest landmarks and the captain's data, never from a take.
/// </summary>
public readonly record struct BatterZone(double Bottom, double Top)
{
    [System.Text.Json.Serialization.JsonIgnore] public double HalfWidth => StrikeZoneGeometry.HalfWidth;
    [System.Text.Json.Serialization.JsonIgnore] public double CenterY => (Bottom + Top) / 2;
    [System.Text.Json.Serialization.JsonIgnore] public double Height => Top - Bottom;
    [System.Text.Json.Serialization.JsonIgnore] public double HalfHeight => Height / 2;

    /// <summary>
    /// This zone's height over <see cref="StrikeZoneGeometry.Reference"/>'s: how much the vertical frame stretches.
    /// The family table's heights (<c>dropFt</c>, the vertical aim) are authored in the reference frame, so a pitch
    /// keeps the same place in every batter's zone. Exactly 1 for the reference zone itself.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore] public double VerticalScale => Height / StrikeZoneGeometry.Reference.Height;

    public bool Contains(double x, double y) =>
        double.IsFinite(x) && double.IsFinite(y)
        && Math.Abs(x) <= HalfWidth && y >= Bottom && y <= Top;
}

/// <summary>World-space bounds of the arcade zone at the plate crossing: fixed width, per-batter height (spec §4.4).</summary>
public static class StrikeZoneGeometry
{
    /// <summary>Half the plate: the zone's width is the same for every batter.</summary>
    public const double HalfWidth = 0.92;

    /// <summary>
    /// The zone's plane (spec §4.4): the plate's front edge, the pitcher's side (<see cref="HomeSet.PlateFrontZ"/>).
    /// The flight ends here (<see cref="PitchFlight.Point"/> at u = 1), the umpire judges the crossing here, and
    /// the frame, the aim tell and the cursor are drawn here: the drawn zone is the judged zone.
    /// </summary>
    public const double PlateZ = HomeSet.PlateFrontZ;

    /// <summary>
    /// The frame the pitch family table is authored in: 1.45 to 3.65 ft. A pitch's heights (<c>dropFt</c>, the vertical
    /// aim, the star rise) are feet in this frame, scaled by <see cref="BatterZone.VerticalScale"/> onto a batter.
    /// It is also the named fallback for a pitch no batter faces (a probe, a tool, a unit test). No live path reads it
    /// as a zone: the match stamps every delivery with its batter's zone (<see cref="Match.PreparePitch"/>).
    /// </summary>
    public static readonly BatterZone Reference = new(1.45, 3.65);

    [ThreadStatic] static int _fallbackReads;

    /// <summary>
    /// How many times this thread judged or flew a pitch that carried no batter zone and fell back to
    /// <see cref="Reference"/>. A test resets it, plays a live path, and asserts it stayed zero.
    /// </summary>
    public static int FallbackReads { get => _fallbackReads; set => _fallbackReads = value; }

    /// <summary>The zone a pitch flies in: its stamped batter's, or <see cref="Reference"/> (counted) when it carries none.</summary>
    public static BatterZone Of(PitchCommand pitch)
    {
        if (pitch.Zone is { } zone) return zone;
        _fallbackReads++;
        return Reference;
    }

    /// <summary>
    /// This batter's zone: mid-thigh to chest from <see cref="Silhouette.ZoneLandmarks"/>, so a later build channel
    /// flows through with no code here, then the <c>pitching.zone</c> safety net.
    /// </summary>
    public static BatterZone For(Character batter, RulesTable rules)
    {
        var (thighMid, chest) = Silhouette.ZoneLandmarks(batter);
        return Clamp(thighMid, chest, rules.Pitching.Zone);
    }

    /// <summary>
    /// The safety net (<c>pitching.zone</c>): the bottom and the top each into their band, then the height into its
    /// band about the zone's center. It is not a design lever: every shipped captain sits inside it untouched.
    /// </summary>
    public static BatterZone Clamp(double thighMidFt, double chestFt, PitchZoneRules z)
    {
        var bottom = Math.Clamp(thighMidFt, z.BottomMinFt, z.BottomMaxFt);
        var top = Math.Clamp(chestFt, z.TopMinFt, z.TopMaxFt);
        var height = top - bottom;
        var clamped = Math.Clamp(height, z.HeightMinFt, z.HeightMaxFt);
        if (clamped == height) return new BatterZone(bottom, top);
        var center = (bottom + top) / 2;
        return new BatterZone(center - clamped / 2, center + clamped / 2);
    }

    /// <summary>
    /// The pitch's plate crossing is inside its batter's frame. <paramref name="rules"/> is the table the family
    /// flies on — the match's own when a match asks (#855); absent, the process-wide table.
    /// </summary>
    public static bool Contains(PitchCommand pitch, RulesTable rules, string? starPitchId = null)
    {
        var p = PitchFlight.Point(pitch, 1, rules, starPitchId);
        return Of(pitch).Contains(p.X, p.Y);
    }
}
