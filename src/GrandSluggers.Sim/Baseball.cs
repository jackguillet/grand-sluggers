namespace GrandSluggers.Sim;

/// <summary>
/// Drawn baseball in game feet (sim units). Real ball is ~0.25.
/// DiameterFt is the posed still (held, mound, glove).
/// FlightDiameterFt is pitch-toward-plate at the mound so a pitch reads at 60 ft.
/// InPlayDiameterFt is contact, hops, and throws — a glove, not a torso.
/// </summary>
public static class Baseball
{
    public const double DiameterFt = 0.62;
    public const double FlightDiameterFt = 1.42;
    public const double InPlayDiameterFt = 1.12;

    /// <summary>Unlit gold shell so a pitch reads at 60 ft without a 2 ft mesh.</summary>
    public const double HaloMul = 1.55;
    public const double FlightGlow = 1.6;

    public static double InFlightScale(bool inFlight) =>
        inFlight ? FlightDiameterFt : DiameterFt;

    /// <summary>
    /// Pitch far from the plate eases up so it reads. In-play never uses that far-scale.
    /// </summary>
    public static double ApparentScale(bool inFlight, double z, bool inPlay = false)
    {
        if (!inFlight) return DiameterFt;
        if (inPlay) return InPlayDiameterFt;
        var far = Math.Clamp((z - 8) / 40, 0, 1);
        return DiameterFt + (FlightDiameterFt - DiameterFt) * far;
    }
}
