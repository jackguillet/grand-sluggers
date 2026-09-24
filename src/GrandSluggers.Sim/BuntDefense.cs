namespace GrandSluggers.Sim;

/// <summary>
/// The bunt defense (spec §7.3, #625). The batter squares at the West press and the defense reads it
/// before the pitch: the corners crash toward the plate, the middle infielders cover first and second
/// behind them, and after contact the pitcher, the catcher and the corners charge the triangle; the
/// earliest route among P / C / 1B / 3B fields it (§8.2). The square is a typed fact on the swing
/// (<see cref="SwingCommand.SquareSec"/>), so the client that saw the press and the headless game seed
/// the same bodies from one function. Every number is <c>fielding.bunt</c>. Nothing here decides the
/// play: the crash is a head start, and the pursuit planner and the throw table (§8.8) still read the
/// live bodies — a human who takes a crashing body keeps it (§8.9).
/// </summary>
public static class BuntDefense
{
    /// <summary>The batter has squared (§5.8): the defense read the tell before the pitch.</summary>
    public static bool Squared(SwingCommand? swing) => swing is { SquareSec: > 0 };

    /// <summary>
    /// The bunt alignment is on for this live ball: the batter squared, or the ball is a bunt (a late press nobody
    /// read still brings the middle in to cover, so the throw to first has a receiver, §7.3).
    /// </summary>
    public static bool AlignmentOn(SwingCommand? swing, BattedBallClass? shape) =>
        Squared(swing) || shape == BattedBallClass.Bunt;

    /// <summary>A crash body at the square (§7.3).</summary>
    public static bool Crashes(string pos, BuntDefenseRules b) => Has(b.Crash, pos);

    /// <summary>A body that converges on the ball after contact (the triangle).</summary>
    public static bool Charges(string pos, BuntDefenseRules b) => Has(b.Charge, pos);

    /// <summary>The bag <paramref name="pos"/> walks to at the square (first for the second baseman, second for the shortstop), or 0.</summary>
    public static int CoverBag(string pos, BuntDefenseRules b) =>
        Same(pos, b.CoverFirst) ? 1 : Same(pos, b.CoverSecond) ? 2 : 0;

    /// <summary>A bunt too hard for the sac (§7.3): the lead force is played if makeable (§8.8).</summary>
    public static bool TooHard(AtBatResult hit, BuntDefenseRules b) => hit.ExitVeloMph >= b.HardExitMph;

    /// <summary>
    /// Where every body of the alignment stands after the square has been held <paramref name="squareSec"/> play seconds
    /// (§7.3): a crash body has run toward the plate at its own chase speed (§8.1), up to <c>crashFt</c>; a cover body
    /// has walked toward its bag at the flat cover speed (§8.7), stopping at the cover stop; everyone else is at their
    /// spot (<paramref name="rest"/>, the diamond's positions by default). The presenter draws this during the pitch and
    /// the live ball seeds from it at contact — one function, one set of bodies.
    /// </summary>
    public static Dictionary<string, (double X, double Z)> Spots(
        IReadOnlyDictionary<string, Character> assigned,
        double squareSec,
        RulesTable rules,
        IReadOnlyDictionary<string, (double X, double Z)>? rest = null)
    {
        var r = rules;
        var b = r.Fielding.Bunt;
        var cover = r.Fielding.Cover;
        var held = Math.Max(0, squareSec);
        var spots = new Dictionary<string, (double X, double Z)>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in assigned)
        {
            var pos = kv.Key;
            var at = rest is not null && rest.TryGetValue(pos, out var live) ? live : Diamond.Positions[pos];
            if (held > 0 && Crashes(pos, b))
            {
                var speed = FieldingResolver.ChaseSpeedFt(kv.Value, false, r);
                at = Toward(at, Diamond.Home, Math.Min(b.CrashFt, speed * held));
            }
            else if (held > 0 && CoverBag(pos, b) is > 0 and var bag)
            {
                var goal = Diamond.Bag(bag);
                var dist = Diamond.Dist(at.X, at.Z, goal.X, goal.Z);
                at = Toward(at, goal, Math.Min(Math.Max(0, dist - cover.StopFt), FieldingResolver.CoverSpeedFt(kv.Value, r) * held));
            }
            spots[pos] = at;
        }
        return spots;
    }

    /// <summary>
    /// Who covers each bag on a bunt (§7.3, §8.7): the second baseman first and the shortstop second behind the crash,
    /// the corners their own bags when they are not the glove, the catcher home, the pitcher backfilling any bag whose
    /// cover is the glove. The glove covers nothing.
    /// </summary>
    public static Dictionary<int, string> CoverMap(string glovePos, BuntDefenseRules b)
    {
        string Pick(params string[] order)
        {
            foreach (var pos in order)
                if (!Same(pos, glovePos)) return pos;
            return "";
        }
        return new Dictionary<int, string>
        {
            [1] = Pick(b.CoverFirst, "1B", "P"),
            [2] = Pick(b.CoverSecond, "P"),
            [3] = Pick("3B", "P"),
            [4] = Pick("C", "P")
        };
    }

    static (double X, double Z) Toward((double X, double Z) from, (double X, double Z) to, double ft)
    {
        var dx = to.X - from.X;
        var dz = to.Z - from.Z;
        var dist = Math.Sqrt(dx * dx + dz * dz);
        if (dist < 1e-6 || ft <= 0) return from;
        var step = Math.Min(dist, ft);
        return (from.X + dx / dist * step, from.Z + dz / dist * step);
    }

    static bool Has(string[] list, string pos)
    {
        foreach (var p in list)
            if (Same(p, pos)) return true;
        return false;
    }

    static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
