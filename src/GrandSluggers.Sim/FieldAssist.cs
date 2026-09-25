namespace GrandSluggers.Sim;

/// <summary>
/// Exhibition: CPU covers until the human takes the glove.
/// Training drills that teach scoop still start the player on the glove (#83).
/// </summary>
public static class FieldAssist
{
    public static bool PlayerStartsOnGlove(bool trainingRequiresPlayer) => trainingRequiresPlayer;

    /// <summary>
    /// Human is on defense: they throw. CPU may still run and catch on a dead stick.
    /// They do not gun to first for you.
    /// </summary>
    public static bool HumanOwnsThrow(bool humanDefense) => humanDefense;

    public static bool StickTakesGlove(double stickX, double stickY, double threshold, bool swapPressed)
        => swapPressed || Math.Abs(stickX) + Math.Abs(stickY) >= threshold;

    public static bool StickDead(double stickX, double stickY, double threshold) =>
        Math.Abs(stickX) + Math.Abs(stickY) < threshold;

    /// <summary>
    /// Dead stick, no ball: that glove still chases like CPU. Stick steers.
    /// They do not throw for you.
    /// </summary>
    public static bool CpuChases(bool hasBall, bool throwing, bool stickDead) =>
        !hasBall && !throwing && stickDead;

    /// <summary>YOU stays on the play glove the whole in-play, dead stick included.</summary>
    public static bool ShowYou(bool humanDefense, string pos) =>
        humanDefense && !string.IsNullOrWhiteSpace(pos);

    public static (double X, double Z) CoverSpot(string pos, RulesTable rules)
    {
        var diamond = DiamondGeometry.Of(rules);
        return pos switch
        {
            "1B" => diamond.First,
            "2B" => diamond.Second,
            "3B" => diamond.Third,
            "C" => Diamond.Home,
            _ => diamond.Positions.TryGetValue(pos, out var at) ? at : diamond.Rubber
        };
    }

    /// <summary>
    /// Bag number → the position that covers it when nobody else is on the ball: 1=1B, 2=2B,
    /// 3=3B, 4=home/C. The live map (<see cref="InPlay.CoverMap"/>) reassigns a bag whose owner
    /// is the glove (§8.7).
    /// </summary>
    public static string CoverKey(int bag) => bag switch
    {
        1 => "1B",
        2 => "2B",
        3 => "3B",
        4 => "C",
        _ => ""
    };

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
        double stickY,
        double stickTake,
        RulesTable rules) =>
        SwapGlove(current, at, aimX, aimZ, stickX, stickY, stickTake, rules);

    /// <summary>
    /// Select / R: stick points at who you want; dead stick takes the next-nearest to the ball.
    /// Not Diamond.Order. <paramref name="stickTake"/> is data/feel fieldAssistStick.
    /// </summary>
    public static string SwapGlove(
        string current,
        IReadOnlyDictionary<string, (double X, double Z)> at,
        double ballX,
        double ballZ,
        double stickX,
        double stickY,
        double stickTake,
        RulesTable rules)
    {
        if (at == null || at.Count == 0) return current;
        var mag = Math.Abs(stickX) + Math.Abs(stickY);
        if (mag >= stickTake)
            return NearestInDirection(current, at, stickX, stickY, rules);
        return NextNearestToBall(current, at, ballX, ballZ);
    }

    public static string NearestInDirection(
        string current,
        IReadOnlyDictionary<string, (double X, double Z)> at,
        double stickX,
        double stickY,
        RulesTable rules)
    {
        var from = At(current, at, rules);
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

    static (double X, double Z) At(string pos, IReadOnlyDictionary<string, (double X, double Z)> at, RulesTable rules)
    {
        if (at != null && at.TryGetValue(pos, out var live)) return live;
        return DiamondGeometry.Of(rules).Positions.TryGetValue(pos, out var p) ? p : Diamond.Home;
    }
}
