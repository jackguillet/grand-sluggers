namespace GrandSluggers.Sim;

/// <summary>
/// Drawn baseball in game feet (sim units). Real ball is ~0.25.
/// DiameterFt is the posed still (mound must not become a beach ball).
/// FlightDiameterFt is the in-flight scale so a pitch reads at 60 ft.
/// </summary>
public static class Baseball
{
    public const double DiameterFt = 0.62;
    public const double FlightDiameterFt = 1.15;

    public static double InFlightScale(bool inFlight) =>
        inFlight ? FlightDiameterFt : DiameterFt;
}
