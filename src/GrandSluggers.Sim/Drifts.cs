namespace GrandSluggers.Sim;

/// <summary>
/// One drifting disc as a play reads it (§14, the <c>drift</c> pattern): where it wanders from, how far and how fast, the band of
/// heights it reaches and its push. <see cref="PhaseA"/> and <see cref="PhaseB"/> are the play's seeded draws.
/// </summary>
public sealed record DriftDisc(int Hazard, string Type, double X, double Z, double RadiusFt, double FloorFt, double TopFt,
    double PeriodSec, double TravelFt, double PushFt, double PushSec, double PhaseA, double PhaseB)
{
    /// <summary>The second axis runs this much slower than the first, so the path loops rather than retraces a line.</summary>
    public const double SecondAxisMul = 1.37;

    /// <summary>
    /// Where the disc stands at play second <paramref name="t"/>: up to <see cref="TravelFt"/> to either side of its spot across
    /// the field (x) and 0.6 of it in depth (z), on two unrelated clocks from the play's phases. A function of the clock alone.
    /// </summary>
    public (double X, double Z) At(double t) =>
        (X + TravelFt * Math.Sin(2 * Math.PI * (t / PeriodSec + PhaseA)),
         Z + 0.6 * TravelFt * Math.Sin(2 * Math.PI * (t / (PeriodSec * SecondAxisMul) + PhaseB)));

    /// <summary>A ball at (x, y, z) is inside the disc at <paramref name="t"/>, between its floor and its top.</summary>
    public bool Takes(double t, double x, double y, double z)
    {
        if (y < FloorFt || y > TopFt) return false;
        var (dx, dz) = At(t);
        return Diamond.Dist(x, z, dx, dz) <= RadiusFt;
    }
}

/// <summary>The park's drifting discs and the side a push goes.</summary>
public static class Drifts
{
    /// <summary>
    /// A played park's drift instances with their rows and this play's phases (two draws each, from <paramref name="draw"/>).
    /// Empty — and nothing drawn — at a park without one.
    /// </summary>
    public static IReadOnlyList<DriftDisc> Of(Park park, RulesTable rules, Func<double> draw)
    {
        var discs = new List<DriftDisc>();
        for (var i = 0; i < park.Hazards.Count; i++)
        {
            var h = park.Hazards[i];
            var row = rules.Hazards.Of(h.Type);
            if (row.Pattern != HazardPattern.Drift) continue;
            discs.Add(new DriftDisc(i, h.Type, h.X, h.Z, h.Radius, row.FloorFt ?? 0, row.TopFt ?? 0, row.PeriodSec ?? 1,
                row.TravelFt ?? 0, row.PushFt ?? 0, row.PushSec ?? 1, draw(), draw()));
        }
        return discs;
    }

    /// <summary>
    /// The unit push for a ball heading (<paramref name="vx"/>, <paramref name="vz"/>) at (x, z) through a disc centred at
    /// (cx, cz): square to its heading, away from the disc's middle. A ball through the dead centre goes to its left.
    /// </summary>
    public static (double Dx, double Dz) PushDir(double vx, double vz, double x, double z, double cx, double cz)
    {
        var len = Math.Sqrt(vx * vx + vz * vz);
        if (len < 1e-9) return (0, 0);
        var (lx, lz) = (-vz / len, vx / len);   // the heading's left, counter-clockwise from above
        var side = (x - cx) * lx + (z - cz) * lz;
        return side >= 0 ? (lx, lz) : (-lx, -lz);
    }
}

/// <summary>A drifting disc pushed the live ball this play (§14): the disc, when the push started, and its direction and feet so far.</summary>
public sealed record BallPushed(int Hazard, string Type, double T, double X, double Z, double Dx, double Dz, double PushedFt);
