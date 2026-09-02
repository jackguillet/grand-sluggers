namespace GrandSluggers.Sim;

/// <summary>
/// Exhibition: CPU covers until the human takes the glove.
/// Training drills that teach scoop still start the player on the glove (#83).
/// </summary>
public static class FieldAssist
{
    public const double StickTake = 0.35;

    public static bool PlayerStartsOnGlove(bool trainingRequiresPlayer) => trainingRequiresPlayer;

    /// <summary>
    /// Human is on defense: they throw. CPU may still run and catch on a dead stick.
    /// They do not gun to first for you.
    /// </summary>
    public static bool HumanOwnsThrow(bool humanDefense) => humanDefense;

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

    /// <summary>Bag number → cover position. 1=1B, 2=2B, 3=3B, 4=home/C.</summary>
    public static string CoverKey(int bag) => bag switch
    {
        1 => "1B",
        2 => "2B",
        3 => "3B",
        4 => "C",
        _ => ""
    };

    /// <summary>
    /// The moment the ball leaves the hand you are the cover at that bag (#329).
    /// Cutoff / no bag leaves you where you were.
    /// </summary>
    public static string AfterThrowPos(string currentPos, int bag)
    {
        var cover = CoverKey(bag);
        return string.IsNullOrEmpty(cover) ? currentPos : cover;
    }

    /// <summary>Mini-diamond UV. Same named-bag map as running leads.</summary>
    public static (double U, double V) BagPip(int bag) => Baserunning.DiamondPip(bag);

    /// <summary>
    /// Who Select / R would take. Stick points at them; dead stick is the next-nearest
    /// to the landing (fly) or the ball (dirt). Not while you hold the ball.
    /// </summary>
    public static string SwitchHint(
        string current,
        IReadOnlyDictionary<string, (double X, double Z)> at,
        double aimX,
        double aimZ,
        double stickX,
        double stickY) =>
        SwapGlove(current, at, aimX, aimZ, stickX, stickY);

    /// <summary>
    /// Select / R: stick points at who you want; dead stick takes the next-nearest to the ball.
    /// Not Diamond.Order.
    /// </summary>
    public static string SwapGlove(
        string current,
        IReadOnlyDictionary<string, (double X, double Z)> at,
        double ballX,
        double ballZ,
        double stickX,
        double stickY)
    {
        if (at == null || at.Count == 0) return current;
        var mag = Math.Abs(stickX) + Math.Abs(stickY);
        if (mag >= StickTake)
            return NearestInDirection(current, at, stickX, stickY);
        return NextNearestToBall(current, at, ballX, ballZ);
    }

    public static string NearestInDirection(
        string current,
        IReadOnlyDictionary<string, (double X, double Z)> at,
        double stickX,
        double stickY)
    {
        var from = At(current, at);
        var mag = Math.Sqrt(stickX * stickX + stickY * stickY);
        if (mag < 0.01) return current;
        var nx = stickX / mag;
        var nz = stickY / mag;
        string best = current;
        var bestDot = 0.15;
        foreach (var kv in at)
        {
            if (kv.Key == current) continue;
            var dx = kv.Value.X - from.X;
            var dz = kv.Value.Z - from.Z;
            var len = Math.Sqrt(dx * dx + dz * dz);
            if (len < 1) continue;
            var dot = (dx / len) * nx + (dz / len) * nz;
            if (dot > bestDot)
            {
                bestDot = dot;
                best = kv.Key;
            }
        }
        return best;
    }

    public static string NextNearestToBall(
        string current,
        IReadOnlyDictionary<string, (double X, double Z)> at,
        double ballX,
        double ballZ)
    {
        string best = current;
        var bestD = double.MaxValue;
        foreach (var kv in at)
        {
            if (kv.Key == current) continue;
            var d = Diamond.Dist(kv.Value.X, kv.Value.Z, ballX, ballZ);
            if (d < bestD)
            {
                bestD = d;
                best = kv.Key;
            }
        }
        if (best != current) return best;
        var i = Array.IndexOf(Diamond.Order, current);
        if (i < 0) return Diamond.Order[0];
        return Diamond.Order[(i + 1) % Diamond.Order.Length];
    }

    static (double X, double Z) At(string pos, IReadOnlyDictionary<string, (double X, double Z)> at)
    {
        if (at != null && at.TryGetValue(pos, out var live)) return live;
        return Diamond.Positions.TryGetValue(pos, out var p) ? p : Diamond.Home;
    }
}
