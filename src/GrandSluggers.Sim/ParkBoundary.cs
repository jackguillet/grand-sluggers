namespace GrandSluggers.Sim;

/// <summary>
/// One field's edge, resolved: the foul rail's offset and flare, the rail's top, the backstop and
/// the dugout pad, in feet (spec §6.1, §16; #826, F2-a). The numbers come from
/// <c>data/rules/boundary.json</c> (<see cref="BoundaryRules"/>); this is the shape the geometry is
/// handed, and it names the rail math both the flight and the drawn kit run.
///
/// <para>
/// It is a value type with named members rather than a bag of statics for two reasons (FD-06,
/// FD-07). A park may name its own foul area later (F2-d), so the edge has to be something a caller
/// can hold two of and compare — <see cref="FieldBounds"/> keys its polygon cache on this value, so
/// a second edge cannot be served a first edge's polygon. And the polyline fence (F2-c) needs a
/// place to live that is not a static class; a member added here arrives at every reader at once.
/// </para>
///
/// <para>
/// <b>Naming.</b> The issue calls this type <c>Boundary</c>. <c>FieldBounds.Boundary</c> already
/// names the built polygon and <c>BallFlight.cs</c> — which this child must not touch — spells that
/// type out, so a second <c>Boundary</c> would shadow it inside <c>FieldBounds</c> and could not be
/// written there without full qualification. <c>ParkBoundary</c> is the same thing under a name that
/// reads where F2-d will use it: the boundary this park plays on.
/// </para>
/// </summary>
public readonly record struct ParkBoundary
{
    /// <summary>cos 45°: the foul line's projection. The 45° line is structural (§16), not a rule number.</summary>
    const double Inv = 0.7071067811865476;

    /// <inheritdoc cref="BoundaryRules.FoulOffsetFt"/>
    public double FoulOffsetFt { get; init; }

    /// <inheritdoc cref="BoundaryRules.FlareStartFt"/>
    public double FlareStartFt { get; init; }

    /// <inheritdoc cref="BoundaryRules.RailHeightFt"/>
    public double RailHeightFt { get; init; }

    /// <inheritdoc cref="BoundaryRules.BackstopZFt"/>
    public double BackstopZFt { get; init; }

    /// <inheritdoc cref="BoundaryRules.DugoutPadFt"/>
    public double DugoutPadFt { get; init; }

    /// <summary>
    /// The height the rail's geometry actually stands at, which is <see cref="RailHeightFt"/>
    /// narrowed through <c>float</c>. The shipped number was <c>HarborWall.HipHeight</c>, a
    /// <c>float</c> const, so every foul segment of the clip polygon and every drawn rail box has
    /// stood at <c>(double)4.2f</c> = 4.19999980926514, not at 4.2. F2-a moved the number's home and
    /// changed nothing about it; widening this is a change to the boundary, and it belongs to
    /// whoever owns the value (#732), with the parity evidence that goes with a changed polygon.
    /// </summary>
    public double RailTopFt => (float)RailHeightFt;

    /// <summary>The edge as one table names it.</summary>
    public static ParkBoundary From(BoundaryRules rules) => new()
    {
        FoulOffsetFt = rules.FoulOffsetFt,
        FlareStartFt = rules.FlareStartFt,
        RailHeightFt = rules.RailHeightFt,
        BackstopZFt = rules.BackstopZFt,
        DugoutPadFt = rules.DugoutPadFt
    };

    /// <summary>
    /// The process-wide edge, from the one default table — the same fallback <see cref="Diamond"/>
    /// reads, and for the same reason: every park shares this edge today, nothing may change it
    /// mid-run, and tests execute in parallel. To play a different edge, point the process at
    /// another data root or trial overlay and diff the two runs.
    /// </summary>
    public static ParkBoundary Default => From(Rules.Default.Boundary);

    /// <summary>The process-wide edge a park dresses: <see cref="For(Park, RulesTable)"/> on <see cref="Rules.Default"/>.</summary>
    public static ParkBoundary For(Park park) => For(park, Rules.Default);

    /// <summary>
    /// The boundary a park plays on <paramref name="rules"/> (FD-07 C, F2-d): the table's edge with the park's own
    /// <see cref="Park.Foul"/> values over it. A park that names none is the table's edge itself, value for value.
    /// </summary>
    public static ParkBoundary For(Park park, RulesTable rules)
    {
        var edge = From(rules.Boundary);
        return park.Foul is not { } foul ? edge : edge with
        {
            FoulOffsetFt = foul.OffsetFt ?? edge.FoulOffsetFt,
            FlareStartFt = foul.FlareStartFt ?? edge.FlareStartFt,
            RailHeightFt = foul.RailHeightFt ?? edge.RailHeightFt,
        };
    }

    /// <summary>
    /// A point on the foul rail: <paramref name="alongFt"/> out from home along the line (0 at the
    /// plate, <paramref name="poleFt"/> at the pole), offset into foul so the rail stays off the
    /// dirt and outside the dugout. The offset is full through the infield and smoothsteps to 0 at
    /// the pole from <see cref="FlareStartFt"/> out, so the rail meets the fence where the fence
    /// meets the line. <paramref name="sign"/> is +1 for the first-base side.
    /// </summary>
    public (double X, double Z) RailPoint(int sign, double alongFt, double poleFt)
    {
        var s = Math.Max(0, alongFt);
        var flareStart = FlareStartFt;
        // A pole inside the flare start leaves the rail parallel the whole way.
        var u = s <= flareStart || poleFt <= flareStart
            ? 1
            : 1 - Math.Clamp((s - flareStart) / (poleFt - flareStart), 0, 1);
        u = u * u * (3 - 2 * u);
        var off = FoulOffsetFt * u;
        return (sign * (Inv * s + Inv * off), Inv * s - Inv * off);
    }
}
