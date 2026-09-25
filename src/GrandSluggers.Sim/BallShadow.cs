namespace GrandSluggers.Sim;

/// <summary>Ground cue for the ball's current position, independent of the landing ring and camera.</summary>
public sealed record BallShadowFeel
{
    [Positive] public double NearDiameterFt { get; init; }
    [Positive] public double FarDiameterFt { get; init; }
    [Positive] public double HeightRangeFt { get; init; }
    [Positive] public double SurfaceLiftFt { get; init; }
    [Positive, Chance] public double Opacity { get; init; }

    public void Validate()
    {
        if (!double.IsFinite(NearDiameterFt) || !double.IsFinite(FarDiameterFt)
            || !double.IsFinite(HeightRangeFt) || !double.IsFinite(SurfaceLiftFt)
            || !double.IsFinite(Opacity) || FarDiameterFt <= 0 || NearDiameterFt < FarDiameterFt
            || HeightRangeFt <= 0 || SurfaceLiftFt <= 0 || Opacity <= 0 || Opacity > 1)
            throw new InvalidDataException("Ball shadow needs positive diameters, near >= far, a positive height/lift and opacity in (0, 1].");
    }
}

public static class BallShadow
{
    public static double Diameter(double heightFt, BallShadowFeel feel) =>
        feel.NearDiameterFt + (feel.FarDiameterFt - feel.NearDiameterFt)
        * Math.Clamp(heightFt / feel.HeightRangeFt, 0, 1);

    /// <summary>
    /// Clear the field's highest flat skin and follow the mound. The small gap above grass
    /// also prevents the disk from slicing into dirt when it straddles the apron edge.
    /// </summary>
    public static Vec3 Project(double x, double z, BallShadowFeel feel, DiamondGeometry diamond) =>
        new(x, Math.Max(Math.Max(ParkDiamond.GrassTop, ParkDiamond.PathTop),
            ParkDiamond.StandY(x, z, diamond)) + feel.SurfaceLiftFt, z);
}
