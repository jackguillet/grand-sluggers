namespace GrandSluggers.Sim;

/// <summary>
/// Drawn baseball in game feet (sim units). Real ball is ~0.25.
/// DiameterFt is the posed still (mound must not become a beach ball).
/// FlightDiameterFt is the in-flight scale so a pitch reads at 60 ft.
/// </summary>
public static class Baseball
{
    public const double DiameterFt = 0.62;
    public const double FlightDiameterFt = 2.05;

    public static double InFlightScale(bool inFlight) =>
        inFlight ? FlightDiameterFt : DiameterFt;

    /// <summary>Far from the plate the toy is big. It eases to DiameterFt in the box.</summary>
    public static double ApparentScale(bool inFlight, double z)
    {
        if (!inFlight) return DiameterFt;
        var far = Math.Clamp((z - 8) / 40, 0, 1);
        return DiameterFt + (FlightDiameterFt - DiameterFt) * far;
    }
}
