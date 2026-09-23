namespace GrandSluggers.Sim;

/// <summary>
/// Where a park's fielders start (§8, FD-07 C, F2-d; <c>SF-09</c>): the infield, the pitcher and the catcher are the global
/// set (<see cref="Diamond.Positions"/>) in every park. Each outfielder is the park's named start
/// (<see cref="Park.OutfieldStarts"/>), or by default the global start's bearing from home at its fraction of the fence it
/// was authored against (<c>fielders.authoredFence</c>), on this park's fence at that bearing. A park whose fence at that
/// bearing is the authored one keeps the global start to the bit.
/// </summary>
public static class OutfieldStarts
{
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Park, IReadOnlyDictionary<string, (double X, double Z)>> Cache = new();

    public static IReadOnlyDictionary<string, (double X, double Z)> Of(Park park, RulesTable? rules = null) =>
        Cache.GetValue(park, p => Build(p, Rules.Or(rules)));

    static IReadOnlyDictionary<string, (double X, double Z)> Build(Park park, RulesTable rules)
    {
        var starts = new Dictionary<string, (double X, double Z)>(Diamond.Positions, StringComparer.Ordinal);
        var a = rules.Fielders.AuthoredFence;
        var authored = park with
        {
            LeftFenceFt = (int)Math.Round(a.LeftFt), CenterFenceFt = (int)Math.Round(a.CenterFt), RightFenceFt = (int)Math.Round(a.RightFt),
            Fence = null
        };
        foreach (var (pos, named) in new[] { ("LF", park.OutfieldStarts?.LF), ("CF", park.OutfieldStarts?.CF), ("RF", park.OutfieldStarts?.RF) })
        {
            if (named is not null)
            {
                starts[pos] = (named.X, named.Z);
                continue;
            }
            var (x, z) = starts[pos];
            var bearing = Math.Atan2(x, z) * 180 / Math.PI;
            var from = AtBatResolver.FenceAt(authored, bearing);
            var to = AtBatResolver.FenceAt(park, bearing);
            if (from == to) continue;
            var k = to / from;
            starts[pos] = (x * k, z * k);
        }
        return starts;
    }
}
