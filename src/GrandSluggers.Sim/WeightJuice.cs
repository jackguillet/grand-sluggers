namespace GrandSluggers.Sim;

/// <summary>
/// One body class's juice (CH-13, <c>feel.weightJuice.classes</c>): how long it winds up, how long the game holds on its own
/// contact or catch, and how its wrapper squashes and settles. Keyed by the class id in <c>data/rules/body-classes.json</c>,
/// never by the class's knockback number: knockback is a rule a balance pass may move, and the look must not move with it.
/// Every number is presentation: none of them reaches the sim (<see cref="HitStop"/>).
/// </summary>
public sealed record WeightJuiceRow
{
    /// <summary>The body class this row dresses (a row id in <c>data/rules/body-classes.json</c>).</summary>
    public string Class { get; init; } = "";

    /// <summary>Seconds before a release (the swing's contact mark, a throw's release) the wrapper starts to sink into its load.</summary>
    [Positive] public double AnticipationSec { get; init; }

    /// <summary>How deep the load sinks at the release: the wrapper's height lost, as a fraction (0 is no load squash).</summary>
    public double AnticipationSquash { get; init; }

    /// <summary>× the contact freeze (<c>solidFreeze</c>, <c>smashFreeze</c>, the sour freeze) on this body's own contact.</summary>
    [Positive] public double HitStopMul { get; init; }

    /// <summary>The hold, seconds, on this body's own catch (the glove's first touch of a batted ball); 0 is none.</summary>
    public double CatchStopSec { get; init; }

    /// <summary>Seconds the wrapper takes to come to rest after an impact (the contact, the catch).</summary>
    [Positive] public double SettleSec { get; init; }

    /// <summary>The squash at the impact, as a fraction of the wrapper's height; the settle decays it to rest (0 is none).</summary>
    public double SettleSquash { get; init; }

    /// <summary>
    /// Half-cycles over the settle: 1 sinks and rises (heavy, flat), 3 squashes, stretches and squashes again (light, a fast
    /// overshoot).
    /// </summary>
    public int SettleHalfCycles { get; init; }

    /// <summary>× every stretch half-cycle of the settle: above 1 a light body overshoots tall, below 1 a heavy one barely rises.</summary>
    public double SettleStretch { get; init; }
}

/// <summary>
/// The juice-by-weight table (<c>feel.weightJuice</c>, CH-13, CF-6): one row per body class, and the creep the drawn clocks keep
/// while a hit-stop holds the sim.
/// </summary>
public sealed record WeightJuiceFeel
{
    /// <summary>The class whose row is the juice every body had before classes (Rio's); an unclassed body wears it.</summary>
    public const string ReferenceClass = "harbor-kid";

    /// <summary>
    /// × frame time on the drawn clocks (poses, camera shake, VFX) while a hit-stop holds; the sim's clock does not move at all.
    /// </summary>
    [Positive, Chance] public double HitStopCreepMul { get; init; }

    public IReadOnlyList<WeightJuiceRow> Classes { get; init; } = [];

    /// <summary>The row for a class id, or null when the table has none.</summary>
    public WeightJuiceRow? Find(string? id) =>
        string.IsNullOrEmpty(id) ? null : Classes.FirstOrDefault(r => r.Class.Equals(id, StringComparison.OrdinalIgnoreCase));

    /// <summary>The row for a class id. A class with no row stops: a body with no juice is a data error, never a fallback.</summary>
    public WeightJuiceRow Of(string id) =>
        Find(id) ?? throw new ArgumentException(
            $"body class '{id}' has no row in feel.weightJuice.classes; the rows are [{string.Join(", ", Classes.Select(r => r.Class))}]",
            nameof(id));

    public void Validate()
    {
        var errors = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Classes.Count; i++)
        {
            var r = Classes[i];
            var name = $"feel.weightJuice.classes[{i}]";
            if (string.IsNullOrWhiteSpace(r.Class)) errors.Add($"{name}.class must name a body class");
            else if (!seen.Add(r.Class)) errors.Add($"{name}.class '{r.Class}' is named twice");
            if (r.AnticipationSquash is < 0 or >= 0.5) errors.Add($"{name}.anticipationSquash must be in [0, 0.5); got {r.AnticipationSquash}");
            if (r.SettleSquash is < 0 or >= 0.5) errors.Add($"{name}.settleSquash must be in [0, 0.5); got {r.SettleSquash}");
            if (r.SettleHalfCycles < 1) errors.Add($"{name}.settleHalfCycles must be at least 1; got {r.SettleHalfCycles}");
            if (r.SettleStretch is < 0 or > 3) errors.Add($"{name}.settleStretch must be in [0, 3]; got {r.SettleStretch}");
        }
        if (Find(ReferenceClass) is null)
            errors.Add($"feel.weightJuice.classes needs the '{ReferenceClass}' row: it is the juice an unclassed body wears");
        if (errors.Count > 0) throw new InvalidDataException(string.Join("; ", errors));
    }

    /// <summary>
    /// The cross-table check: every body class in <c>data/rules/body-classes.json</c> has a juice row, and every juice row
    /// dresses a class that exists. Empty when the two tables agree.
    /// </summary>
    public IReadOnlyList<string> Coverage(BodyClassLibrary classes)
    {
        var errors = new List<string>();
        foreach (var c in classes.Classes)
            if (Find(c.Id) is null)
                errors.Add($"body class '{c.Id}' has no row in feel.weightJuice.classes (data/feel/table.json)");
        foreach (var r in Classes)
            if (!classes.Has(r.Class))
                errors.Add($"feel.weightJuice.classes '{r.Class}' is not a body class in data/rules/body-classes.json");
        return errors;
    }
}

/// <summary>
/// Juice by weight (CH-13): anticipation, hit-stop, squash and settle read off the body's class row. Heavy bodies wind up
/// longer, hold longer on their own contact or catch, squash low and flat and settle slowly; light bodies wind up short and
/// overshoot tall and fast. The squash is scale on the presentation wrapper (docs/character-motion.md rule 6), never a bone.
/// </summary>
public static class WeightJuice
{
    /// <summary>The juice row of <paramref name="who"/>: its body class's row, or the reference row for a body with no class.</summary>
    public static WeightJuiceRow Of(Character? who, FeelTable feel) =>
        feel.WeightJuice.Of(who is null || string.IsNullOrEmpty(who.BodyClass) ? WeightJuiceFeel.ReferenceClass : who.BodyClass);

    /// <summary>The hold on a body's own contact: the quality's freeze (<c>solidFreeze</c>, <c>smashFreeze</c>) × its class.</summary>
    public static double HitStopSec(double qualityFreezeSec, WeightJuiceRow row) => Math.Max(0, qualityFreezeSec) * row.HitStopMul;

    /// <summary>The hold on a body's own catch.</summary>
    public static double CatchStopSec(WeightJuiceRow row) => row.CatchStopSec;

    /// <summary>
    /// The load before a release, as a wrapper scale: nothing until <see cref="WeightJuiceRow.AnticipationSec"/> before it,
    /// then an eased sink to <see cref="WeightJuiceRow.AnticipationSquash"/> at the release. At or after the release it is gone.
    /// </summary>
    public static Vec3 Anticipation(WeightJuiceRow row, double secToRelease)
    {
        if (!(secToRelease > 0) || secToRelease > row.AnticipationSec || row.AnticipationSquash <= 0) return One;
        var u = 1 - secToRelease / row.AnticipationSec;
        var s = row.AnticipationSquash * u * u * (3 - 2 * u);
        return Squash(s);
    }

    /// <summary>
    /// The settle after an impact, as a wrapper scale: <see cref="WeightJuiceRow.SettleSquash"/> at the impact, decaying to rest
    /// over <see cref="WeightJuiceRow.SettleSec"/> through <see cref="WeightJuiceRow.SettleHalfCycles"/> half-cycles; each
    /// stretch half-cycle × <see cref="WeightJuiceRow.SettleStretch"/>.
    /// </summary>
    public static Vec3 Settle(WeightJuiceRow row, double secSinceImpact)
    {
        if (!(secSinceImpact >= 0) || secSinceImpact >= row.SettleSec || row.SettleSquash <= 0) return One;
        var u = secSinceImpact / row.SettleSec;
        var d = row.SettleSquash * (1 - u) * (1 - u) * Math.Cos(Math.PI * row.SettleHalfCycles * u);
        return d >= 0 ? Squash(d) : Stretch(-d * row.SettleStretch);
    }

    /// <summary>
    /// This frame's weight juice on the wrapper: the load before a release and the settle after an impact, multiplied.
    /// NaN means no release coming, or no impact yet.
    /// </summary>
    public static Vec3 Wrapper(WeightJuiceRow row, double secToRelease, double secSinceImpact) =>
        Mul(Anticipation(row, secToRelease), Settle(row, secSinceImpact));

    public static Vec3 Mul(Vec3 a, Vec3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

    static readonly Vec3 One = new(1, 1, 1);

    // Down by s and out by half of it: the toy keeps roughly its volume.
    static Vec3 Squash(double s) => new(1 + s * 0.5, 1 - s, 1 + s * 0.5);

    static Vec3 Stretch(double s) => new(1 - s * 0.5, 1 + s, 1 - s * 0.5);
}

/// <summary>
/// The hit-stop clock (CH-13): a hold on the game that is presentation only. While it holds, the sim is not stepped at all —
/// whole frames, so the sim sees exactly the frame steps it would have seen with no hold, only later on the wall clock — and the
/// drawn clocks creep at <see cref="WeightJuiceFeel.HitStopCreepMul"/>. A longer or shorter hold never changes the sim's clock,
/// its trace, or a play's outcome.
/// </summary>
public sealed class HitStop
{
    double _left;

    /// <summary>True while a hold is running.</summary>
    public bool Holding => _left > 0;

    /// <summary>Seconds of hold left.</summary>
    public double LeftSec => _left;

    /// <summary>Start a hold; a hold already running keeps the longer of the two.</summary>
    public void Begin(double sec)
    {
        if (sec > _left) _left = sec;
    }

    public void Clear() => _left = 0;

    /// <summary>
    /// One frame of <paramref name="frameSec"/> wall seconds. While a hold is left the whole frame is held: the sim steps 0 and
    /// the drawn clocks step <paramref name="frameSec"/> × <paramref name="creepMul"/>. Otherwise both step the frame.
    /// </summary>
    public (double SimSec, double DrawSec, bool Held) Frame(double frameSec, double creepMul)
    {
        if (_left <= 0) return (frameSec, frameSec, false);
        _left = Math.Max(0, _left - Math.Max(0, frameSec));
        return (0, frameSec * creepMul, true);
    }

    /// <summary>
    /// Fold a held frame's pad into the presses already held: a fresh press on any held frame is still a press when the sim
    /// steps again (at the same sim moment, since the sim did not move), and the stick, the held buttons and the orders are the
    /// latest frame's. A throw press keeps the bag it was pressed toward. Null <paramref name="held"/> is nothing held yet.
    /// </summary>
    public static LivePadInput Latch(LivePadInput? held, LivePadInput now)
    {
        if (held is null) return now;
        var south = held.SouthDown && !now.SouthDown;
        return now with
        {
            SouthDown = held.SouthDown || now.SouthDown,
            WestDown = held.WestDown || now.WestDown,
            EastDown = held.EastDown || now.EastDown,
            Cutoff = held.Cutoff || now.Cutoff,
            Swap = held.Swap || now.Swap,
            Item = held.Item || now.Item,
            Attack = held.Attack || now.Attack,
            Cancel = held.Cancel || now.Cancel,
            KeysBag = south ? held.KeysBag : now.KeysBag,
            StickBag = south && now.StickBag == 0 ? held.StickBag : now.StickBag,
            ArrowBag = south && now.ArrowBag == 0 ? held.ArrowBag : now.ArrowBag,
            Orders = now.Orders ?? held.Orders
        };
    }
}

/// <summary>
/// When each body last took an impact (a contact, a catch), on the drawn clock: the settle reads it. Keyed by character id, so
/// a body's settle follows the body when the ring moves on.
/// </summary>
public sealed class ImpactClock
{
    readonly Dictionary<string, double> _since = new(StringComparer.OrdinalIgnoreCase);

    public void Impact(string id)
    {
        if (!string.IsNullOrEmpty(id)) _since[id] = 0;
    }

    public void Age(double dt)
    {
        if (dt <= 0 || _since.Count == 0) return;
        foreach (var id in _since.Keys.ToList()) _since[id] += dt;
    }

    /// <summary>Seconds since <paramref name="id"/>'s last impact, or NaN when it has none.</summary>
    public double Since(string id) => !string.IsNullOrEmpty(id) && _since.TryGetValue(id, out var t) ? t : double.NaN;

    public void Clear() => _since.Clear();
}
