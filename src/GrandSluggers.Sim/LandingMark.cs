namespace GrandSluggers.Sim;

/// <summary>
/// Circle on the grass where the ball will land. Yellow while it is coming,
/// red in the catch window. A tube on the dirt — not a filled pancake.
/// Same job as <see cref="SetTells"/> for the charge ring.
/// </summary>
public static class LandingMark
{
    /// <summary>
    /// Torus construction scale (tube thickness). Not the catch ring: the ring is
    /// <see cref="RadiusFt"/>, the stand-up catch radius (#669).
    /// </summary>
    public const double MinRadiusFt = 16;

    /// <summary>Torus tube. Thin discs z-fight the grass.</summary>
    public const double ThickFt = 0.72;

    /// <summary>Grass / dirt top the tube sits on.</summary>
    public const double DirtY = 0.18;

    public static double WorldY => DirtY + ThickFt;

    /// <summary>
    /// Air (fly or liner), not yet caught. Hopper has no circle — they chase the hop.
    /// </summary>
    public static bool On(
        FieldingPreview pre,
        double ballY,
        double hitT,
        bool caught,
        bool buddy,
        double? hangSec = null) =>
        !caught && !buddy && FieldingResolver.InAir(pre, ballY, hitT, hangSec);

    public static (double X, double Z) At(FieldingPreview pre, Park? park = null) =>
        FlyCatch.ChaseTarget(pre, park);

    /// <summary>The yellow ring is the stand-up catch (§8.3, #669), not a 16 ft floor.</summary>
    public static double RadiusFt(FieldingPreview pre) => pre.CatchRadius;

    public static bool Hot(double hitT, double hangSec, Character? fielder = null, Park? park = null) =>
        FlyCatch.JumpWindow(hitT, hangSec, fielder, park);
}
