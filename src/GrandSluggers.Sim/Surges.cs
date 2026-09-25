namespace GrandSluggers.Sim;

/// <summary>
/// One surge band as a play reads it (§14, the <c>surge</c> pattern): the instance's disc (the night multiple at night), the
/// height under which a ball counts as rolling, the wave's clock and its carry.
/// </summary>
public sealed record SurgeBand(int Hazard, string Type, double X, double Z, double RadiusFt, double HeightFt,
    double PeriodSec, double SurgeSec, double CarryFt)
{
    /// <summary>
    /// The wave is in at play second <paramref name="t"/>: the first <see cref="SurgeSec"/> of every <see cref="PeriodSec"/>,
    /// counted from the play's seeded <paramref name="phaseSec"/>.
    /// </summary>
    public bool In(double t, double phaseSec)
    {
        var at = (t + phaseSec) % PeriodSec;
        if (at < 0) at += PeriodSec;
        return at < SurgeSec;
    }

    /// <summary>Seconds from <paramref name="t"/> until the wave next comes in; 0 while it is in.</summary>
    public double UntilIn(double t, double phaseSec)
    {
        if (In(t, phaseSec)) return 0;
        var at = (t + phaseSec) % PeriodSec;
        if (at < 0) at += PeriodSec;
        return PeriodSec - at;
    }

    /// <summary>A ball at (x, z), no higher than <see cref="HeightFt"/>, is inside the band.</summary>
    public bool Holds(double x, double y, double z) =>
        y <= HeightFt + 1e-9 && Diamond.Dist(x, z, X, Z) <= RadiusFt;

    /// <summary>
    /// The feet the band carries a ball in <paramref name="dt"/> seconds of wave: <see cref="CarryFt"/> spread over
    /// <see cref="SurgeSec"/>, so a ball that sits in a whole wave drifts the whole carry.
    /// </summary>
    public double StepFt(double dt) => CarryFt * dt / SurgeSec;
}

/// <summary>The park's surge bands and the one geometric question they ask: which way, and how far, to the nearer foul line.</summary>
public static class Surges
{
    /// <summary>A played park's surge instances with their rows (the night multiple at night). Empty for every park without one.</summary>
    public static IReadOnlyList<SurgeBand> Of(Park park, RulesTable rules, bool night)
    {
        var bands = new List<SurgeBand>();
        for (var i = 0; i < park.Hazards.Count; i++)
        {
            var h = park.Hazards[i];
            var row = rules.Hazards.Of(h.Type);
            if (row.Pattern != HazardPattern.Surge) continue;
            var radius = night ? ParkHazards.NightDiscFt(h.Radius, row) : h.Radius;
            bands.Add(new SurgeBand(i, h.Type, h.X, h.Z, radius, row.HeightFt ?? 0, row.PeriodSec ?? 1, row.SurgeSec ?? 0,
                row.CarryFt ?? 0));
        }
        return bands;
    }

    /// <summary>
    /// The unit direction from (x, z) to the nearer foul line and the distance to it, in the (x, z) plane: the left line for a
    /// ball on the third-base side (x &lt; 0), the right line otherwise. The lines run from home at ±45°.
    /// </summary>
    public static (double Dx, double Dz, double DistFt) TowardLine(double x, double z)
    {
        var s = Math.Sqrt(0.5);
        var (ux, uz) = x < 0 ? (-s, s) : (s, s);
        var along = x * ux + z * uz;
        var (px, pz) = (along * ux - x, along * uz - z);
        var d = Math.Sqrt(px * px + pz * pz);
        return d < 1e-9 ? (0, 0, 0) : (px / d, pz / d, d);
    }
}

/// <summary>A surge band carried the live ball this play (§14): the band, when the carry started, and how far it went in all.</summary>
public sealed record BallCarried(int Hazard, string Type, double T, double X, double Z, double CarriedFt);
