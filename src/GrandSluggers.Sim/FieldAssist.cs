namespace GrandSluggers.Sim;

/// <summary>
/// Exhibition: CPU covers until the human takes the glove.
/// Training drills that teach scoop still start the player on the glove (#83).
/// </summary>
public static class FieldAssist
{
    public const double StickTake = 0.35;

    public static bool PlayerStartsOnGlove(bool trainingRequiresPlayer) => trainingRequiresPlayer;

    public static bool StickTakesGlove(double stickX, double stickY, double threshold, bool swapPressed)
        => swapPressed || Math.Abs(stickX) + Math.Abs(stickY) >= threshold;

    public static (double X, double Z) CoverSpot(string pos) => pos switch
    {
        "1B" => Diamond.First,
        "2B" => Diamond.Second,
        "3B" => Diamond.Third,
        "C" => Diamond.Home,
        _ => Diamond.Positions.TryGetValue(pos, out var at) ? at : Diamond.Rubber
    };
}
