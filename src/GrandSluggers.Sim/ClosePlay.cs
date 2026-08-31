namespace GrandSluggers.Sim;

/// <summary>
/// SMS close play: when a throw and a runner arrive together at third or home,
/// the camera sits on the bag and the first South wins. Offense safe, defense out.
/// </summary>
public static class ClosePlay
{
    public const float IconDelay = 0.22f;

    public static bool Offered(int throwBag, bool secondOccupied, bool thirdOccupied)
    {
        if (throwBag is not (3 or 4)) return false;
        return InPlay.TagBag(secondOccupied, thirdOccupied) == throwBag;
    }

    public static bool IsCloseBag(int bag) => bag is 3 or 4;

    /// <summary>Seconds after the icon until a CPU side mashes. Better Field (defense) or Run (offense) is faster.</summary>
    public static double CpuReactionSec(int stat)
    {
        var n = Math.Clamp(stat, 1, 10);
        return 0.20 + (10 - n) * 0.032;
    }

    /// <summary>First press after the icon wins. A missing press is never. Tie goes to the runner.</summary>
    public static bool OffenseSafe(double offenseAt, double defenseAt) =>
        offenseAt <= defenseAt;

    public static string Caption(int bag, bool safe) =>
        safe
            ? (bag == 4 ? "SAFE at home!" : "SAFE at third!")
            : (bag == 4 ? "OUT at home!" : "OUT at third!");
}
