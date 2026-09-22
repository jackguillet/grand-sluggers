using System.Reflection;

namespace GrandSluggers.Sim;

/// <summary>
/// The rule numbers of play (docs/gameplay-spec.md §16). One table per file under
/// <c>data/rules/</c>, loaded like <see cref="FeelTable"/>: the JSON is the source of
/// truth, the C# initializers are the load fallback for a field the file does not name,
/// and <see cref="ContentDataValidator"/> refuses a file with an unknown field or a value
/// outside its declared range. Presentation numbers stay in <c>data/feel/</c>.
/// </summary>
public sealed class RulesTable
{
    public const string Directory = "rules";

    public static readonly IReadOnlyList<string> Files =
        ["match", "pitching", "batting", "flight", "grounds", "walls", "infield", "boundary", "fielders", "fielding", "hazards", "running", "stars", "cpu"];

    public MatchRules Match { get; init; } = new();

    public PitchingRules Pitching { get; init; } = new();
    public BattingRules Batting { get; init; } = new();
    public FlightRules Flight { get; init; } = new();

    /// <summary>The closed ground library (FD-05): one row per ground a zone may name.</summary>
    public GroundLibrary Grounds { get; init; } = new();

    /// <summary>The closed wall-material library (FD-06): one row per material a span may name.</summary>
    public WallMaterialLibrary Walls { get; init; } = new();

    public InfieldRules Infield { get; init; } = new();
    public BoundaryRules Boundary { get; init; } = new();
    public FielderRules Fielders { get; init; } = new();
    public FieldingRules Fielding { get; init; } = new();

    /// <summary>The closed hazard type library (§14, FD-09): one authored row per type.</summary>
    public HazardRules Hazards { get; init; } = new();

    public RunningRules Running { get; init; } = new();
    public StarRules Stars { get; init; } = new();
    public CpuRules Cpu { get; init; } = new();

    /// <summary>The code-side numbers. Equal to the shipped JSON; a load fallback, not a second table.</summary>
    public static RulesTable Defaults => new();

    /// <summary>
    /// The same tables played at another difficulty rung (§16 <c>cpu.json</c>): every section is shared,
    /// only <see cref="Cpu"/>'s active rung changes. The table itself is returned when the rung is already this one.
    /// </summary>
    public RulesTable AtLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level) || CpuRules.Same(level, Cpu.Level)) return this;
        return new RulesTable
        {
            Match = Match, Pitching = Pitching, Batting = Batting, Flight = Flight,
            Grounds = Grounds, Walls = Walls,
            Infield = Infield, Boundary = Boundary, Fielders = Fielders, Fielding = Fielding,
            Hazards = Hazards, Running = Running, Stars = Stars,
            Cpu = Cpu.AtLevel(level)
        };
    }

    /// <summary>
    /// The same tables played in one park (§0.3 D21, FD-03): every section is shared, and only
    /// <see cref="Flight"/> carries what the park's <see cref="ParkEnvironment"/> names. The table itself
    /// is returned when the park names no environment, so Harbor's resolved table <em>is</em> the global
    /// table — same reference, same numbers on either root (SF-01). Derived once, beside
    /// <see cref="AtLevel"/>, in <see cref="Match"/>: a reader is handed the match's table and never
    /// resolves a park itself.
    /// </summary>
    public RulesTable AtPark(Park park)
    {
        var env = park.Environment;
        if (env is null || !env.Names) return this;
        return new RulesTable
        {
            Match = Match, Pitching = Pitching, Batting = Batting, Flight = Flight.WithEnvironment(env),
            // The ground and wall libraries are global: a park chooses which row each of its zones
            // names (its `zones` block, read through GroundZones), never what a row says (FD-05).
            Grounds = Grounds, Walls = Walls,
            Infield = Infield, Boundary = Boundary, Fielders = Fielders, Fielding = Fielding,
            Hazards = Hazards, Running = Running, Stars = Stars,
            Cpu = Cpu
        };
    }

    public static RulesTable Load(DataRoot dataRoot)
    {
        var errors = new List<string>();
        var table = Load(dataRoot, errors);
        if (errors.Count > 0)
            throw new InvalidDataException("Invalid rules tables:" + Environment.NewLine
                + string.Join(Environment.NewLine, errors.Select(e => "  - " + e)));
        return table;
    }

    /// <summary>Load every table, collecting errors instead of throwing. Missing fields fall back to code.</summary>
    public static RulesTable Load(DataRoot dataRoot, List<string> errors)
    {
        var json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        var table = new RulesTable
        {
            Match = Read<MatchRules>(dataRoot, "match", json, errors),
            Pitching = Read<PitchingRules>(dataRoot, "pitching", json, errors),
            Batting = Read<BattingRules>(dataRoot, "batting", json, errors),
            Flight = Read<FlightRules>(dataRoot, "flight", json, errors),
            Grounds = Read<GroundLibrary>(dataRoot, "grounds", json, errors),
            Walls = Read<WallMaterialLibrary>(dataRoot, "walls", json, errors),
            Infield = Read<InfieldRules>(dataRoot, "infield", json, errors),
            Boundary = Read<BoundaryRules>(dataRoot, "boundary", json, errors),
            Fielders = Read<FielderRules>(dataRoot, "fielders", json, errors),
            Fielding = Read<FieldingRules>(dataRoot, "fielding", json, errors),
            Hazards = Read<HazardRules>(dataRoot, "hazards", json, errors),
            Running = Read<RunningRules>(dataRoot, "running", json, errors),
            Stars = Read<StarRules>(dataRoot, "stars", json, errors),
            Cpu = Read<CpuRules>(dataRoot, "cpu", json, errors)
        };
        RulesValidation.Validate(table, dataRoot, errors);
        return table;
    }

    /// <summary>Where one rules table is read from — the trial overlay's copy when it carries one.</summary>
    public static string PathFor(DataRoot dataRoot, string name) => dataRoot.Resolve(Directory, name + ".json");

    public static IReadOnlyList<string> Validate(DataRoot dataRoot)
    {
        var errors = new List<string>();
        Load(dataRoot, errors);
        return errors.OrderBy(e => e, StringComparer.Ordinal).ToList();
    }

    static T Read<T>(DataRoot root, string name, JsonSerializerOptions json, List<string> errors) where T : class, new()
    {
        var path = PathFor(root, name);
        if (!File.Exists(path))
        {
            errors.Add($"{path}: required rules table is missing");
            return new T();
        }
        try
        {
            var text = File.ReadAllText(path);
            using (var doc = JsonDocument.Parse(text, new JsonDocumentOptions
                   {
                       CommentHandling = JsonCommentHandling.Skip,
                       AllowTrailingCommas = true
                   }))
                RulesValidation.UnknownFields(doc.RootElement, typeof(T), name, path, errors);
            return JsonSerializer.Deserialize<T>(text, json) ?? new T();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            errors.Add($"{path}: cannot read rules table: {ex.Message}");
            return new T();
        }
    }
}

/// <summary>
/// The process-wide table for callers that hold no <see cref="ContentCatalog"/>: the
/// data root found from the binary, else the code defaults. <see cref="Match"/> and the
/// Unity client pass <see cref="ContentCatalog.Rules"/> explicitly; every helper that reads
/// a number takes a <c>rules</c> argument and only falls back here when it is omitted.
/// </summary>
public static class Rules
{
    static RulesTable? _default;

    public static RulesTable Default => _default ??= LoadDefault();

    public static RulesTable Or(RulesTable? rules) => rules ?? Default;

    static RulesTable LoadDefault()
    {
        // Outside the catch on purpose: a data root or trial overlay the run named and cannot have
        // is a stop, not a fallback (#711, #716).
        var root = ContentCatalog.TryFindDataRoot();
        return root is null ? RulesTable.Defaults : ForProcess(root);
    }

    /// <summary>
    /// The process-wide table for a root, or the code defaults when there is nothing readable to
    /// take. A root the run <see cref="DataRoot.Named"/> gets no such fallback: tables it cannot
    /// read stop the run, because the alternative is the control's numbers playing under the
    /// trial's name — and <see cref="Diamond"/> reads this table, so the geometry would be the
    /// control's while the operator believed they were measuring a trial.
    /// </summary>
    public static RulesTable ForProcess(DataRoot root)
    {
        try
        {
            var errors = new List<string>();
            var table = RulesTable.Load(root, errors);
            if (errors.Count == 0) return table;
            if (root.Named)
                throw new InvalidDataException(
                    "Invalid rules tables — " + root.Provenance + Environment.NewLine
                    + string.Join(Environment.NewLine, errors.Select(e => "  - " + e)));
            return RulesTable.Defaults;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (root.Named) throw;
            return RulesTable.Defaults;
        }
    }
}

/// <summary>Value must be strictly greater than zero.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PositiveAttribute : Attribute;

/// <summary>Value is a probability in [0, 1].</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ChanceAttribute : Attribute;

/// <summary>Value may be negative (an offset). Everything else must be at least zero.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SignedAttribute : Attribute;

/// <summary>
/// A nested table the data may leave out: null is "nothing authors this", not a broken table
/// (#818, spec §4.3). Every other object property must be present, so a null there is still an
/// error. The absence has to come from the file having no such key: a JSON <c>null</c> is refused
/// by <see cref="RulesValidation.UnknownFields"/>, which walks the element and reports a row that
/// is not an object.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class OptionalAttribute : Attribute;

public static class RulesValidation
{
    /// <summary>
    /// Every error names the file it came from, resolved through the trial overlay: a number a
    /// trial supplied is reported against the trial's copy, never against the shipped one.
    /// </summary>
    public static void Validate(RulesTable table, DataRoot root, List<string> errors)
    {
        Walk(table.Match, RulesTable.PathFor(root, "match"), "match", errors);
        Walk(table.Pitching, RulesTable.PathFor(root, "pitching"), "pitching", errors);
        Walk(table.Batting, RulesTable.PathFor(root, "batting"), "batting", errors);
        Walk(table.Flight, RulesTable.PathFor(root, "flight"), "flight", errors);
        Walk(table.Grounds, RulesTable.PathFor(root, "grounds"), "grounds", errors);
        Walk(table.Walls, RulesTable.PathFor(root, "walls"), "walls", errors);
        Walk(table.Infield, RulesTable.PathFor(root, "infield"), "infield", errors);
        Walk(table.Boundary, RulesTable.PathFor(root, "boundary"), "boundary", errors);
        Walk(table.Fielders, RulesTable.PathFor(root, "fielders"), "fielders", errors);
        Walk(table.Fielding, RulesTable.PathFor(root, "fielding"), "fielding", errors);
        Walk(table.Hazards, RulesTable.PathFor(root, "hazards"), "hazards", errors);
        Walk(table.Running, RulesTable.PathFor(root, "running"), "running", errors);
        Walk(table.Stars, RulesTable.PathFor(root, "stars"), "stars", errors);
        Walk(table.Cpu, RulesTable.PathFor(root, "cpu"), "cpu", errors);
        table.Cpu.Validate(RulesTable.PathFor(root, "cpu"), errors);
        table.Flight.Validate(RulesTable.PathFor(root, "flight"), errors);
        table.Grounds.Validate(RulesTable.PathFor(root, "grounds"), errors);
        table.Infield.Validate(RulesTable.PathFor(root, "infield"), errors);
        table.Boundary.Validate(RulesTable.PathFor(root, "boundary"), errors);
        table.Fielders.Validate(RulesTable.PathFor(root, "fielders"), errors);
        table.Running.Validate(RulesTable.PathFor(root, "running"), errors);
        table.Fielding.Validate(RulesTable.PathFor(root, "fielding"), errors);
        table.Hazards.Validate(RulesTable.PathFor(root, "hazards"), errors);
        table.Batting.Validate(RulesTable.PathFor(root, "batting"), errors);
        table.Pitching.Cpu.Validate(RulesTable.PathFor(root, "pitching"), errors);
        table.Pitching.Families.Validate(RulesTable.PathFor(root, "pitching"), errors);
    }

    /// <summary>Every numeric leaf is finite and inside its attribute range.</summary>
    static void Walk(object node, string source, string path, List<string> errors)
    {
        foreach (var p in node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.GetIndexParameters().Length > 0) continue;
            var name = path + "." + Camel(p.Name);
            var value = p.GetValue(node);
            if (value is null)
            {
                // A row the data does not author has nothing to range-check (#818). Everything else
                // that is null is a table that failed to build.
                if (p.GetCustomAttribute<OptionalAttribute>() is not null) continue;
                errors.Add($"{source}: {name} must be an object; got null");
                continue;
            }
            if (p.PropertyType == typeof(double) || p.PropertyType == typeof(int))
            {
                var d = Convert.ToDouble(value);
                if (double.IsNaN(d) || double.IsInfinity(d))
                    errors.Add($"{source}: {name} must be finite; got {d}");
                else if (p.GetCustomAttribute<ChanceAttribute>() is not null && (d < 0 || d > 1))
                    errors.Add($"{source}: {name} must be between 0 and 1; got {d}");
                else if (p.GetCustomAttribute<PositiveAttribute>() is not null && d <= 0)
                    errors.Add($"{source}: {name} must be greater than 0; got {d}");
                else if (p.GetCustomAttribute<SignedAttribute>() is null && d < 0)
                    errors.Add($"{source}: {name} must be finite and at least 0; got {d}");
                continue;
            }
            if (p.PropertyType == typeof(string) || p.PropertyType.IsPrimitive) continue;
            if (p.PropertyType.IsClass && p.PropertyType.Namespace == typeof(RulesTable).Namespace
                && !typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType))
                Walk(value, source, name, errors);
        }
    }

    /// <summary>A field the table does not declare is a typo, not a silent fallback.</summary>
    public static void UnknownFields(JsonElement element, Type type, string path, string source, List<string> errors)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{source}: {path} must be an object; got {element.ValueKind}");
            return;
        }
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
        foreach (var field in element.EnumerateObject())
        {
            if (!props.TryGetValue(field.Name, out var p))
            {
                errors.Add($"{source}: {path}.{field.Name} is not a rule this table owns");
                continue;
            }
            if (p.PropertyType.IsClass && p.PropertyType != typeof(string)
                && p.PropertyType.Namespace == typeof(RulesTable).Namespace
                && !typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType))
                UnknownFields(field.Value, p.PropertyType, path + "." + field.Name, source, errors);
        }
    }

    static string Camel(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);

    internal static void Order(string source, string field, double low, double high, List<string> errors)
    {
        if (low > high)
            errors.Add($"{source}: {field} must not exceed its upper bound; got {low} > {high}");
    }
}

// ---------------------------------------------------------------------------------------
// match.json — §1 (innings, extras, mercy)
// ---------------------------------------------------------------------------------------

public sealed class MatchRules
{
    /// <summary>A tie after the last scheduled inning plays on, at most this many extra innings; a tie at the cap is a tie (D8).</summary>
    public int ExtraInningsCap { get; init; } = 3;
    public MercyRules Mercy { get; init; } = new();
}

/// <summary>Mercy (§1): on by default; a lead of <see cref="Runs"/> at the end of an inning from <see cref="FromInning"/> ends the game; off below <see cref="MinScheduledInnings"/> innings.</summary>
public sealed class MercyRules
{
    public int Runs { get; init; } = 10;
    public int FromInning { get; init; } = 3;
    public int MinScheduledInnings { get; init; } = 6;
}

// ---------------------------------------------------------------------------------------
// infield.json — the diamond every park shares
// ---------------------------------------------------------------------------------------

/// <summary>
/// Bases, rubber and the ground that dresses them, in feet, read by <see cref="Diamond"/> and
/// <see cref="ParkDiamond"/>. Every park plays on the same infield — that is the design, not an
/// accident — so these are one global set loaded once and never changed at runtime. What varies
/// per park is the outfield, the foul area and the environment, and none of that belongs here.
///
/// The corners are authored, not derived from <see cref="BaselineFt"/>. A 90-ft baseline
/// rotated 45° is 63.6396…, and the diamond has always played at a rounded 63.64; deriving it
/// would move the bases by four thousandths of a foot and silently change every route.
///
/// <para>
/// <b>Why the dress is here and not in <c>data/feel/</c> (#729).</b> The grass diamond and the
/// back arc are drawn, not played — nothing in the sim reads them to judge a ball. But they are
/// measured from the bags, so when the bags move they have to move with them or the picture stops
/// describing the field. Path width, the bag pads, the home pad, the mound table, the warning
/// track and the foul apron are bodies and equipment, not geometry, and they stay in feet as
/// <c>const</c> on <see cref="ParkDiamond"/>.
/// </para>
/// </summary>
public sealed class InfieldRules
{
    /// <summary>Bag to bag. The unit a runner's progress is measured in.</summary>
    [Positive] public double BaselineFt { get; init; } = 90;

    /// <summary>Home to the rubber, along the center line.</summary>
    [Positive] public double MoundFt { get; init; } = 60.5;

    /// <summary>First and third, off the center line and out from home by the same amount each.</summary>
    [Positive] public double CornerFt { get; init; } = 63.64;

    /// <summary>Second, straight out from home.</summary>
    [Positive] public double SecondFt { get; init; } = 127.28;

    /// <summary>
    /// Half-diagonal of the drawn grass diamond around second and the mound (#729). The bags sit
    /// outside it, on the dirt, which is the whole reason it is smaller than <see cref="CornerFt"/>.
    /// </summary>
    [Positive] public double InnerHalfFt { get; init; } = 50;

    /// <summary>
    /// Outer arc of the 1B–2B–3B dirt, measured from the rubber (#729). Farther out than the home
    /// legs, so the back of the diamond reads as a curve rather than a matching frame.
    /// </summary>
    [Positive] public double BackArcFt { get; init; } = 92;

    /// <summary>
    /// The rubber sits between home and second, second is past the corners, and the drawn grass
    /// stays inside both the bag it points at and the arc behind it.
    ///
    /// <para>
    /// The bag-pad clearance is deliberately not a rule here. The grass vertex must also clear
    /// <c>ParkDiamond.BagPadR</c>, but that margin is 1.64 ft shipped and 0.13 ft at C80 — too thin
    /// to refuse a table over, because a park override or a trial that rounds differently would fail
    /// validation on a dress that draws correctly. The hairline is pinned in the tests instead.
    /// </para>
    /// </summary>
    public void Validate(string source, List<string> errors)
    {
        RulesValidation.Order(source, "infield.moundFt", MoundFt, SecondFt, errors);
        RulesValidation.Order(source, "infield.cornerFt", CornerFt, SecondFt, errors);
        RulesValidation.Order(source, "infield.innerHalfFt", InnerHalfFt, CornerFt, errors);
        RulesValidation.Order(source, "infield.innerHalfFt", InnerHalfFt, BackArcFt, errors);
    }
}

// ---------------------------------------------------------------------------------------
// boundary.json — the field's edge every park shares (§6.1, #826)
// ---------------------------------------------------------------------------------------

/// <summary>
/// Where the field ends, in feet, read through <see cref="ParkBoundary"/> by the flight's clip
/// polygon (<see cref="FieldBounds"/>), the drawn kit (<see cref="HarborWall"/>) and the park
/// validator's fence floor. These were <c>HarborWall</c> literals until #826 (F2-a): a class named
/// after one park decided the foul wrap of all six, and the sim could not be handed a different
/// edge without editing code (FD-07, FR-05).
///
/// <para>
/// One global set, like <see cref="InfieldRules"/>: every park shares this edge today, so a single
/// table loaded once is the honest model. It is a table rather than five <c>const</c>s so that a
/// park may name its own foul area later (FD-07 C, F2-d) and so the polyline fence (FD-06, F2-c)
/// has somewhere to land — the value type that carries a resolved edge is
/// <see cref="ParkBoundary"/>, and it is what the cache keys on.
/// </para>
///
/// <para>
/// <b>Every number here is the number that shipped.</b> #730 / #732 own
/// <see cref="FoulOffsetFt"/> and <see cref="FlareStartFt"/> until they close; F2-a moved them, it
/// did not choose them. <c>trials/c80</c> deliberately carries no copy of this file: whether the
/// compact profile scales the offset or the flare is #732's question, and an overlay that answered
/// it here would answer it by accident.
/// </para>
/// </summary>
public sealed class BoundaryRules
{
    /// <summary>
    /// The hip rail's offset from the foul line, into foul territory, measured perpendicular to the
    /// line. The dugout rail <b>is</b> this line. 0 at the pole, where the rail meets the fence.
    /// </summary>
    [Positive] public double FoulOffsetFt { get; init; } = 36;

    /// <summary>
    /// How far along the line the rail runs parallel before it flares out to the pole. A pole
    /// closer than this leaves the rail parallel the whole way (a smoothstep over nothing).
    /// </summary>
    [Positive] public double FlareStartFt { get; init; } = 95;

    /// <summary>
    /// The rail's top, pole to pole. A park's <c>fenceHeightFt</c> must stand over it (D15), which
    /// is the floor <see cref="ContentDataValidator"/> refuses a park under.
    /// </summary>
    [Positive] public double RailHeightFt { get; init; } = 4.2;

    /// <summary>
    /// The backstop's wrap behind the plate, as a Z. Negative: behind home is −Z, and a backstop in
    /// front of the plate is a field with no room to catch a pitch.
    /// </summary>
    [Signed] public double BackstopZFt { get; init; } = -36;

    /// <summary>
    /// Clearance from the dugout's back wall to the edge the stands may start at
    /// (<see cref="HarborWall.DugoutClearX"/>). Drawn, not played, and here for the same reason the
    /// grass diamond is in <c>infield.json</c>: it is measured off the rail, so it travels with it.
    /// </summary>
    [Positive] public double DugoutPadFt { get; init; } = 18;

    /// <summary>The rule the attributes cannot say: the backstop is behind the plate, not in front of it.</summary>
    public void Validate(string source, List<string> errors)
    {
        if (BackstopZFt >= 0)
            errors.Add($"{source}: boundary.backstopZFt must be behind the plate (less than 0); got {BackstopZFt}");
    }
}

// ---------------------------------------------------------------------------------------
// pitching.json — §4, §4.7, §4.8
// ---------------------------------------------------------------------------------------

public sealed class PitchingRules
{
    public PitchSpeedRules Speed { get; init; } = new();
    public PitchReleaseRules Release { get; init; } = new();
    public PitchFlightRules Flight { get; init; } = new();
    public PitchFamilyTable Families { get; init; } = new();
    public StarPitchShapeRules StarShapes { get; init; } = new();
    public StaminaRules Stamina { get; init; } = new();
    public CpuPitcherRules Cpu { get; init; } = new();
}

/// <summary>The one coefficient every family shares: the Pitch stat's mph (<see cref="AtBatResolver.PitchSpeedMph(PitchCommand, int, RulesTable)"/>). Base mph and charge mph are per family.</summary>
public sealed class PitchSpeedRules
{
    public double MphPerPitchStat { get; init; } = 0.9;
}

/// <summary>The charge release (spec §4.1): inside the first <see cref="NiceBandSec"/> of MAX is a Nice! release, +<see cref="NiceMul"/> mph.</summary>
public sealed class PitchReleaseRules
{
    [Positive] public double NiceBandSec { get; init; } = 0.25;
    [Positive] public double NiceMul { get; init; } = 1.05;
}

/// <summary>Release point, air time, and the break (<see cref="PitchFlight"/>, spec §4.1 – §4.3).</summary>
public sealed class PitchFlightRules
{
    [Signed] public double ReleaseHandX { get; init; } = 1.55;
    [Positive] public double ReleaseHandY { get; init; } = 6.2;
    public double ReleaseTowardPlate { get; init; } = 2.6;
    [Positive] public double ArcadeScale { get; init; } = 2.05;
    [Positive] public double AirMinSec { get; init; } = 0.78;
    [Positive] public double AirMaxSec { get; init; } = 1.28;
    [Positive] public double MinMph { get; init; } = 40;
    /// <summary>Feet the crossing moves at full break: half the zone width (spec §4.2).</summary>
    [Positive] public double BreakMaxFt { get; init; } = 0.46;
    /// <summary>The mid-flight bend the eye sees; it is gone by the plate.</summary>
    public double BreakEarly { get; init; } = 0.35;
    /// <summary>The drift grows over [breakLateFrom, breakLateFrom + breakLateSpan] of the flight.</summary>
    [Chance] public double BreakLateFrom { get; init; } = 0.55;
    [Positive] public double BreakLateSpan { get; init; } = 0.45;
    /// <summary>A charged pitch or a changeup takes this much of the break (reference: "essentially straight").</summary>
    [Chance] public double BreakDampedMul { get; init; } = 0.10;
    /// <summary>How fast a held stick brings the bend to full, per second, at Pitch 5 …</summary>
    [Positive] public double BreakRatePerSec { get; init; } = 2.4;
    /// <summary>… and per Pitch-stat point above 5.</summary>
    public double BreakRatePerPitchStat { get; init; } = 0.12;
}

/// <summary>
/// The authored rows of the shared pitch library (spec §4.3, PH-02-R2, #810). One row per family
/// the game can throw; <see cref="PitchFlight.Shape"/> evaluates every one of them with the same
/// arithmetic, so a new family is a row and not a new <c>if</c>.
///
/// The rows are <b>named properties</b>, never a dictionary or a list. A collection would load, and
/// would then slip past every guard this file has: the reflective range checks in
/// <see cref="RulesValidation.Walk"/>, the unknown-field refusal in
/// <see cref="RulesValidation.UnknownFields"/>, and the JSON = code parity tests all walk declared
/// properties. A row hiding in a collection would be a table nothing validated.
///
/// Three of the five library ids (<see cref="PitchFamily.Curveball"/>,
/// <see cref="PitchFamily.Slider"/>, <see cref="PitchFamily.Sinker"/>) are <b>known but unauthored</b>:
/// their numbers and their natural sweep are P1-d, a numeric trial Jack accepts. Asking for one is a
/// loud stop that names it, never a silent fastball.
///
/// <para>
/// <b>Authored is a fact about the data, not about the code</b> (#818). The three are nullable rows
/// that the shipped <c>pitching.json</c> simply has no key for, and the trial overlay
/// <c>trials/pitch5</c> is a copy of that file with the three keys present. So the same build stops
/// on <c>Of("slider")</c> under the shipped root and flies it under the trial, and nothing anywhere
/// hard-wires which ids have numbers. <see cref="Fastball"/> and <see cref="Changeup"/> are not
/// nullable: a table without them is a game that cannot throw a pitch.
/// </para>
/// </summary>
public sealed class PitchFamilyTable
{
    /// <summary>Speed pressure, straight, with a mild mid-flight hump. Every pitcher throws it (PH-15-R1).</summary>
    public PitchFamilyRules Fastball { get; init; } = new()
    {
        Mph = 86, ChargeMph = 8,
        Hump = 0.35, HangUntil = 1, HangRate = 1, DumpRate = 1, DropFt = 0,
        SweepFt = 0, SweepFrom = 1,
        BreakDamped = false, StaminaCost = 0, OffSpeed = false
    };

    /// <summary>0.80× the meat; hangs at or above the fastball, then dumps to a lower aim (#668).</summary>
    public PitchFamilyRules Changeup { get; init; } = new()
    {
        Mph = 68.8, ChargeMph = 3,
        Hump = 0, HangUntil = 0.62, HangRate = 0.22, DumpRate = 2.4, DropFt = 0.9,
        SweepFt = 0, SweepFrom = 1,
        BreakDamped = true, StaminaCost = 3, OffSpeed = true
    };

    /// <summary>Pronounced arc and drop (PH-02-R2). Unauthored on the shipped root; <c>trials/pitch5</c> proposes it (#818).</summary>
    [Optional] public PitchFamilyRules? Curveball { get; init; }

    /// <summary>Sideways movement that challenges coverage (PH-02-R2). Unauthored on the shipped root (#818).</summary>
    [Optional] public PitchFamilyRules? Slider { get; init; }

    /// <summary>A faster dipping alternative to the curveball (PH-02-R2). Unauthored on the shipped root (#818).</summary>
    [Optional] public PitchFamilyRules? Sinker { get; init; }

    /// <summary>
    /// This table's row for a library id, or null when the data does not author one. Not public:
    /// callers ask <see cref="IsAuthored"/> or take <see cref="Of"/>'s named stop, so an unauthored
    /// family can never be read as a missing-but-harmless nothing.
    /// </summary>
    PitchFamilyRules? Named(string family) => family switch
    {
        PitchFamily.Fastball => Fastball,
        PitchFamily.Changeup => Changeup,
        PitchFamily.Curveball => Curveball,
        PitchFamily.Slider => Slider,
        PitchFamily.Sinker => Sinker,
        _ => null
    };

    /// <summary>True for a library id <b>this table</b> has numbers for (#818).</summary>
    public bool IsAuthored(string? family) => family is not null && Named(family) is not null;

    IReadOnlyList<string>? _authored;

    /// <summary>
    /// The authored ids in library order (<see cref="PitchFamily.All"/>). Built once per table and
    /// then handed out: <see cref="PitchSelection"/> asks for it on the SET tick, so it must not
    /// allocate per call. A table is never mutated after it loads, so the answer cannot go stale.
    /// </summary>
    public IReadOnlyList<string> Authored =>
        _authored ??= PitchFamily.All.Where(IsAuthored).ToList();

    /// <summary>
    /// The row for a family id. A library id with no row and an id that is not in the library are
    /// different mistakes and get different messages; neither one flies.
    /// </summary>
    public PitchFamilyRules Of(string? family)
    {
        if (family is not null && Named(family) is { } row) return row;
        if (PitchFamily.IsKnown(family ?? ""))
            throw new InvalidOperationException(
                $"pitch family '{family}' is in the library but has no authored row in pitching.json families; "
                + "its numbers are a trial (trials/pitch5, PH-20-R1). Authored: " + string.Join(", ", Authored));
        throw new ArgumentException(
            $"'{family}' is not a pitch family. The library is [{string.Join(", ", PitchFamily.All)}] (PitchFamily); "
            + "break is a stick verb and charge is a modifier, not a type.", nameof(family));
    }

    /// <summary>
    /// The rules across a row's fields the attributes cannot say, checked on every <b>authored</b>
    /// row, shipped or trial:
    /// <list type="bullet">
    /// <item>The hang must reach the aim before the plate. A row whose hang and dump never sum to 1
    /// is a pitch that never arrives, and the clamp in <see cref="PitchFlight.Shape"/> would hide it
    /// as a snap at u=1 (#668).</item>
    /// <item>A row that sweeps must say when the sweep starts (#818). <c>sweepFrom</c> 1 is "never
    /// begins", so a row with feet of sweep and no start is a number that does nothing — the kind of
    /// dead rule a table should refuse rather than carry.</item>
    /// </list>
    /// </summary>
    internal void Validate(string source, List<string> errors)
    {
        foreach (var name in Authored)
        {
            var row = Of(name);
            var atPlate = row.HangUntil * row.HangRate + (1 - row.HangUntil) * row.DumpRate;
            if (atPlate < 1)
                errors.Add($"{source}: pitching.families.{name} must finish its drop in flight; "
                           + $"hangUntil × hangRate + (1 − hangUntil) × dumpRate = {atPlate} < 1");
            if (row.SweepFt != 0 && row.SweepFrom >= 1)
                errors.Add($"{source}: pitching.families.{name} sweeps {row.SweepFt} ft but its sweepFrom is "
                           + $"{row.SweepFrom}, which never begins; give the sweep a start below 1 or set sweepFt to 0");
        }
    }
}

/// <summary>
/// One family's numbers (spec §4.3). Every field is something that used to be an <c>if</c> on the
/// retired <c>Changeup</c> bool, under a name that says what it does rather than which pitch it was
/// written for. The defaults are the fastball — the family every pitcher throws — so a row that
/// omits a field reads as straight and ordinary rather than as a broken changeup.
/// </summary>
public sealed class PitchFamilyRules
{
    /// <summary>Base mph, before the Pitch stat, the charge, Nice! and the tired arm.</summary>
    [Positive] public double Mph { get; init; } = 86;
    /// <summary>mph a full charge adds to <em>this</em> family (spec §4.1).</summary>
    public double ChargeMph { get; init; } = 8;
    /// <summary>Feet the ball rides above the straight line at mid-flight; 0 is a shape with no hump.</summary>
    public double Hump { get; init; } = 0.35;
    /// <summary>Where the hang ends and the dump begins, as a share of the flight. 1 is a shape that never dumps.</summary>
    [Chance] public double HangUntil { get; init; } = 1;
    /// <summary>How fast Y interpolates toward the aim before <see cref="HangUntil"/>. Well below 1 keeps the ball up; 1 is a straight line.</summary>
    public double HangRate { get; init; } = 1;
    /// <summary>How fast Y interpolates after <see cref="HangUntil"/>. With <see cref="HangUntil"/> 1 there is no after, and it only has to keep the seam continuous.</summary>
    public double DumpRate { get; init; } = 1;
    /// <summary>Feet below a fastball's height this family crosses (up to one zone-half).</summary>
    public double DropFt { get; init; } = 0;
    /// <summary>
    /// The family's <b>natural sweep</b> (spec §4.2, #818): feet the crossing ends off the straight
    /// line from the rubber, positive toward the pitcher's <b>glove side</b> and mirrored by the
    /// throwing hand. It is the shape's own movement, so the stick does not scale it and a charge
    /// does not damp it (PH-05-R1, PH-15-R6); it adds to the player's steering, whose own cap
    /// <c>flight.breakMaxFt</c> is unchanged. 0 is a family that flies straight.
    /// </summary>
    [Signed] public double SweepFt { get; init; }
    /// <summary>
    /// Where in the flight the sweep starts to show, as a share of it. From there it grows as the
    /// square of the remaining flight, so it is nothing early and most of itself at the end — late
    /// enough to fool, early enough to read (§4.2). 1 is "never begins", which is why it is the
    /// default and what a family with no sweep authors.
    /// </summary>
    [Chance] public double SweepFrom { get; init; } = 1;
    /// <summary>The stick's break takes <c>flight.breakDampedMul</c> for this family even uncharged (spec §4.1).</summary>
    public bool BreakDamped { get; init; }
    /// <summary>Stamina on top of <c>stamina.pitchCost</c> (spec §4.7).</summary>
    public int StaminaCost { get; init; }
    /// <summary>The batter can be fooled slow by it: the sour-slap pop band (§5.2) and the CPU batter's late error (§5.9).</summary>
    public bool OffSpeed { get; init; }
}

public sealed class StarPitchShapeRules
{
    public double HeatballWobbleHz { get; init; } = 18;
    public double HeatballWobbleFt { get; init; } = 0.4;
    public double PrismballWobbleHz { get; init; } = 24;
    public double PrismballWobbleFt { get; init; } = 1.8;
    public double CharmballWobbleHz { get; init; } = 9;
    public double CharmballWobbleFt { get; init; } = 0.7;
    [Chance] public double PhonyballSwitchAt { get; init; } = 0.55;
    [Signed] public double PhonyballEarlyX { get; init; } = -0.5;
    [Signed] public double PhonyballLateX { get; init; } = 2.4;
    public double CaskballRise { get; init; } = 0.55;
}

/// <summary>
/// Per-pitcher stamina (spec §4.7): pool = poolBase + Pitch × poolPerPitch; costs per verb; a
/// family's own extra is its row's <see cref="PitchFamilyRules.StaminaCost"/>, and a star's cost is
/// its <c>staminaCost</c> in star-skills.json. Below tiredBelow = TIRED (−mph, −break, a crossing
/// wobble); below 0 = exhausted (worse). The CPU swaps at TIRED with a lead.
/// </summary>
public sealed class StaminaRules
{
    public int PoolBase { get; init; } = 60;
    public int PoolPerPitch { get; init; } = 6;
    public int PitchCost { get; init; } = 4;
    public int ChargeCost { get; init; } = 3;
    public int BreakCost { get; init; } = 1;
    public int HomerCost { get; init; } = 6;
    public int RunCost { get; init; } = 2;
    public int TiredBelow { get; init; } = 25;
    public double TiredMph { get; init; } = 6;
    [Chance] public double TiredBreakMul { get; init; } = 0.6;
    public double TiredWobbleFt { get; init; } = 0.25;
    public double ExhaustedMph { get; init; } = 10;
    public double ExhaustedWobbleFt { get; init; } = 0.45;
    public int CpuSwapLead { get; init; } = 3;
}

/// <summary>
/// The CPU pitcher (spec §4.8): a decision table, evaluated once per SET from the count, the
/// outs, the runners, and its stamina. Each row names a location and a pitch mix; scatter is
/// σ = (11 − Pitch) × scatterFtPerPitchStat around the target, never a dead-center default.
/// </summary>
public sealed class CpuPitcherRules
{
    /// <summary>
    /// <b>The switch (PH-18-R1, #823).</b> <c>false</c> — the shipped root — is today's CPU: it
    /// solves an endpoint with a height (<c>AimX</c> / <c>AimY</c>) and treats charge, changeup and
    /// break as four exclusive verbs. <c>true</c> — <c>trials/pitch5</c> — is
    /// <see cref="Match.CpuPitchByInputs"/>: the pitch is built from the inputs a human has and
    /// nothing else (rubber for location, presses for family, charge and steer as modifiers, a bend
    /// no bigger than a held stick reaches), and its height is whatever the family gives (PH-03).
    ///
    /// <para>
    /// Off, not a never-sentinel, because this is a code path and not a number: the #722 convention
    /// picks a per-rung sentinel when a rule has a value that could shadow "never", and a bool has
    /// no such value. Off is byte-identical to the shipped CPU — same draws, same order, same
    /// stream (S-114).
    /// </para>
    ///
    /// <para>
    /// Jack judges the on side in sitting 1, with P1-d's shapes and P1-f's verb. Whether it ever
    /// flips on the shipped root is his, not an agent's.
    /// </para>
    /// </summary>
    public bool HumanInputs { get; init; } = false;
    /// <summary>0-0, 1-0, 1-1 and every count no other row claims.</summary>
    public CpuPitchRow Even { get; init; } = new() { Location = "edge", Normal = 45, Charge = 20, Changeup = 15, Break = 20, StarChance = 0.05,
        Families = new CpuFamilyWeights { Fastball = 85, Changeup = 15, Curveball = 0, Slider = 0, Sinker = 0 },
        ChargeChance = 0.20, SteerChance = 0.20 };
    /// <summary>Ahead 0-2, 1-2: waste, then edge.</summary>
    public CpuPitchRow Ahead { get; init; } = new() { Location = "waste", Normal = 20, Charge = 15, Changeup = 35, Break = 30, StarChance = 0.15,
        Families = new CpuFamilyWeights { Fastball = 65, Changeup = 35, Curveball = 0, Slider = 0, Sinker = 0 },
        ChargeChance = 0.15, SteerChance = 0.30 };
    /// <summary>Behind 2-0, 3-0, 3-1: middle-in, safe.</summary>
    public CpuPitchRow Behind { get; init; } = new() { Location = "middleIn", Normal = 60, Charge = 30, Changeup = 5, Break = 5, StarChance = 0,
        Families = new CpuFamilyWeights { Fastball = 95, Changeup = 5, Curveball = 0, Slider = 0, Sinker = 0 },
        ChargeChance = 0.30, SteerChance = 0.05 };
    /// <summary>A runner on with two outs: middle, fast; never a pitch-out.</summary>
    public CpuPitchRow RunnerTwoOuts { get; init; } = new() { Location = "middle", Normal = 50, Charge = 40, Changeup = 0, Break = 10, StarChance = 0,
        Families = new CpuFamilyWeights { Fastball = 100, Changeup = 0, Curveball = 0, Slider = 0, Sinker = 0 },
        ChargeChance = 0.40, SteerChance = 0.10 };
    public CpuPitchLocations Locations { get; init; } = new();
    /// <summary>
    /// Scatter in feet per Pitch-stat point below 11 (spec §4.8). Off the switch it is aim scatter in
    /// both axes around the endpoint; on it, it is noise on the CPU's own rubber intent in X, because
    /// under the human-input model there is no vertical input to miss in.
    /// </summary>
    public double ScatterFtPerPitchStat { get; init; } = 0.10;
    [Positive] public double TiredScatterMul { get; init; } = 1.6;
    /// <summary>A charged CPU pitch releases inside the Nice! band this often.</summary>
    [Chance] public double NiceChance { get; init; } = 0.3;
    [Chance] public double TapMin { get; init; } = 0.1;
    [Chance] public double TapSpan { get; init; } = 0.35;
    /// <summary>
    /// The CPU walks the rubber before this share of pitches (a real verb: the batter may mistrack,
    /// §5.9). <b>Shipped-path only</b>: with <see cref="HumanInputs"/> on, the rubber <i>is</i> the
    /// location, so it is solved on every pitch and there is no separate walk to roll for.
    /// </summary>
    [Chance] public double RubberWalkChance { get; init; } = 0.35;
    /// <inheritdoc cref="RubberWalkChance"/>
    [Chance] public double RubberWalkMax { get; init; } = 0.4;

    internal void Validate(string source, List<string> errors)
    {
        foreach (var (name, row) in new[] { ("even", Even), ("ahead", Ahead), ("behind", Behind), ("runnerTwoOuts", RunnerTwoOuts) })
        {
            if (row.Location is not ("edge" or "waste" or "middleIn" or "middle"))
                errors.Add($"{source}: pitching.cpu.{name}.location must be one of [edge, waste, middleIn, middle]; got '{row.Location}'");
            if (row.Normal + row.Charge + row.Changeup + row.Break <= 0)
                errors.Add($"{source}: pitching.cpu.{name} pitch mix must have a positive total");
            // A row that weights nothing has no family to throw. The run-time filter can still empty
            // a row for one pitcher (the row weights only families this arm does not own, or the
            // table does not author) and that falls back to the fastball every pitcher throws
            // (PH-15-R1, Match.CpuPitchByInputs) — but a row that weights nothing for *anybody* is a
            // broken table, and it is caught here rather than read as "always the fastball".
            if (row.Families.Total() <= 0)
                errors.Add($"{source}: pitching.cpu.{name}.families must weight at least one family: a row with no "
                    + "family to throw is not a pitch (spec §4.8, pitching.cpu.humanInputs)");
        }
    }
}

/// <summary>
/// One row of the CPU pitcher's table: where, and how it throws there.
///
/// <para>
/// Two mixes live here, one per side of <see cref="CpuPitcherRules.HumanInputs"/>.
/// <see cref="Normal"/> / <see cref="Charge"/> / <see cref="Changeup"/> / <see cref="Break"/> are
/// the shipped model's four <b>exclusive verbs</b>. <see cref="Families"/> plus
/// <see cref="ChargeChance"/> and <see cref="SteerChance"/> are the human-input model: a family from
/// the library, then charge and steer as <b>independent modifiers</b> on it, the way a hand has them
/// (PH-02-R1). The shipped file authors both, and the second is the port of the first written down —
/// fastball takes every verb that was not the changeup, and the charge and break shares become the
/// two chances — so the switch has a stated starting point rather than a new invention.
/// </para>
/// </summary>
public sealed class CpuPitchRow
{
    public string Location { get; init; } = "edge";
    /// <summary>Shipped model: a tap with no stick. Inert while <see cref="CpuPitcherRules.HumanInputs"/> is on.</summary>
    public double Normal { get; init; } = 45;
    /// <inheritdoc cref="Normal"/>
    public double Charge { get; init; } = 20;
    /// <inheritdoc cref="Normal"/>
    public double Changeup { get; init; } = 15;
    /// <inheritdoc cref="Normal"/>
    public double Break { get; init; } = 20;
    [Chance] public double StarChance { get; init; } = 0.05;
    /// <summary>
    /// Human-input model: the weight of each family in this count, filtered at run time to the slots
    /// this pitcher can actually select (<c>PitchSelection.IsSelectable</c>) and renormalised. Inert
    /// while <see cref="CpuPitcherRules.HumanInputs"/> is off.
    /// </summary>
    public CpuFamilyWeights Families { get; init; } = new();
    /// <summary>Human-input model: this share of pitches is charged to MAX, independent of the family.</summary>
    [Chance] public double ChargeChance { get; init; } = 0.20;
    /// <summary>Human-input model: this share holds the stick one way from release, independent of the family and the charge.</summary>
    [Chance] public double SteerChance { get; init; } = 0.20;
}

/// <summary>
/// The weight of each family in one count row (spec §4.8, PH-15, PH-18-R1). Named properties, not a
/// dictionary, for the same reason the family library is (#810): every weight is range-checked,
/// compared against the JSON, and refused when it is a key this table does not own.
///
/// <para>A weight is a share, not a probability: the row's weights are filtered to what this pitcher
/// can select and then renormalised, so a row may weight a family no pitcher on the roster owns
/// without being wrong — it simply never comes up.</para>
/// </summary>
public sealed class CpuFamilyWeights
{
    public double Fastball { get; init; } = 85;
    public double Changeup { get; init; } = 15;
    public double Curveball { get; init; }
    public double Slider { get; init; }
    public double Sinker { get; init; }

    /// <summary>The weight this row gives a family id; an id the library does not have weighs nothing.</summary>
    public double Of(string family) => family switch
    {
        PitchFamily.Fastball => Fastball,
        PitchFamily.Changeup => Changeup,
        PitchFamily.Curveball => Curveball,
        PitchFamily.Slider => Slider,
        PitchFamily.Sinker => Sinker,
        _ => 0
    };

    /// <summary>Every weight in the row, before any pitcher filters it.</summary>
    public double Total() => Fastball + Changeup + Curveball + Slider + Sinker;
}

/// <summary>Where the named locations sit, in feet from the zone's edges and center.</summary>
public sealed class CpuPitchLocations
{
    /// <summary>An edge target sits this far inside the frame.</summary>
    public double EdgeInsetFt { get; init; } = 0.2;
    /// <summary>The corner picked is the away side (by batter hand) this often.</summary>
    [Chance] public double EdgeAwayChance { get; init; } = 0.7;
    /// <summary>A waste pitch sits this far outside the frame.</summary>
    public double WasteOutFt { get; init; } = 0.3;
    /// <summary>Middle-in sits this far toward the batter from center.</summary>
    public double MiddleInFt { get; init; } = 0.35;
    /// <summary>
    /// A middle target varies its height by ± this. <b>Shipped-path only</b>: with
    /// <see cref="CpuPitcherRules.HumanInputs"/> on there is no vertical input at all, so height is
    /// the family's and nothing spreads it (PH-03).
    /// </summary>
    public double MiddleYSpreadFt { get; init; } = 0.5;
}

// ---------------------------------------------------------------------------------------
// batting.json — §5, §5.9
// ---------------------------------------------------------------------------------------

public sealed class BattingRules
{
    /// <summary>
    /// Whether the stick at contact shapes an <b>ordinary</b> swing (PH-12 option C, spec §5.3, §5.4).
    /// <c>false</c> on the shipped root — stick L/R adds <see cref="SprayRules.StickDeg"/> to the
    /// direction and stick U/D takes <see cref="LaunchRules.StickDeg"/> off the launch, bit for bit
    /// what shipped. <c>true</c> in <c>trials/pitch5</c>: a swing that is neither a bunt nor a Star
    /// Swing ignores both aims, so timing, contact position, pitch height and the swing decide the
    /// flight, and the CPU batter stops drawing the two aims it no longer needs (PH-18). Bunts keep
    /// the stick until P4-b; Star Swings keep it until Phase 6. The box walk and the SET recenter
    /// read the same stick and are untouched either way (PH-09).
    ///
    /// Sitting 2 judges the trial; until then the shipped game is the game it was.
    /// </summary>
    public bool GeometryOnly { get; init; }

    public ContactWindowRules Window { get; init; } = new();
    public SwingChargeRules Charge { get; init; } = new();
    public QualityRules Quality { get; init; } = new();
    public ExitRules Exit { get; init; } = new();
    public LaunchRules Launch { get; init; } = new();
    public BuntRules Bunt { get; init; } = new();
    public SprayRules Spray { get; init; } = new();
    public FoulRules Foul { get; init; } = new();
    public CursorRules Cursor { get; init; } = new();
    public HbpRules Hbp { get; init; } = new();
    public StarSwingRules Star { get; init; } = new();
    public BuddiesOnBaseRules BuddiesOnBase { get; init; } = new();
    public PitchFactorRules PitchFactor { get; init; } = new();
    public OffenseItemRules Items { get; init; } = new();
    public CpuBatterRules Cpu { get; init; } = new();

    internal void Validate(string source, List<string> errors)
    {
        RulesValidation.Order(source, "batting.launch.minDeg", Launch.MinDeg, Launch.MaxDeg, errors);
        RulesValidation.Order(source, "batting.window.floorFrames", Window.FloorFrames, Window.ChargeFrames, errors);
        RulesValidation.Order(source, "batting.window.chargeFrames", Window.ChargeFrames, Window.SlapFrames, errors);
        // The shared window is floored by the same number as the split one, so a table that authors
        // a window under its own floor is refused whether the switch is on or off (PH-10-R1).
        RulesValidation.Order(source, "batting.window.floorFrames", Window.FloorFrames, Window.Frames, errors);
        RulesValidation.Order(source, "batting.cursor.perfectFraction", Cursor.PerfectFraction, 1, errors);
    }
}

/// <summary>
/// The timing window (spec §5.3, D4, D13): slap 9 frames, charge 7, ± (contact − 5) × framesPerContact,
/// × skill, park and the human rung's multipliers, floored. It is centered on the ball's plate time
/// minus <see cref="LeadSec"/>. Inside the window timing decides direction only; the outermost
/// (1 − squareFraction) of each half demotes the cursor zone by one tier, never two.
///
/// <see cref="Shared"/> picks which of two windows the formula is: off is the split one described
/// above, on is <see cref="Frames"/> for everyone (PH-10-R1).
/// </summary>
public sealed class ContactWindowRules
{
    /// <summary>
    /// One window for every hitter, every swing and every human rung (PH-10-R1, PH-11-R1, PH-15-R7,
    /// PH-17). <c>false</c> on the shipped root — the split window above is what plays, bit for bit.
    /// <c>true</c> in <c>trials/pitch5</c>: <see cref="Frames"/> is the window, and
    /// <see cref="SlapFrames"/>, <see cref="ChargeFrames"/>, <see cref="FramesPerContact"/> and the
    /// rung's <c>humanWindowMul</c> do not enter it. The Star Pitch multiplier, the park's night
    /// multiplier and <see cref="FloorFrames"/> apply either way, in the same order.
    ///
    /// Sitting 2 judges the trial; until then the shipped game is the game it was.
    /// </summary>
    public bool Shared { get; init; }

    /// <summary>
    /// The one shared window in frames at 60 Hz, total width (PH-10-R1's trial start value, 9).
    /// Read only when <see cref="Shared"/> is on; the shipped file authors it so the start value is
    /// written down once, beside the numbers it replaces, rather than living in a trial alone.
    /// </summary>
    [Positive] public double Frames { get; init; } = 9.0;

    [Positive] public double SlapFrames { get; init; } = 9.0;
    [Positive] public double ChargeFrames { get; init; } = 7.0;
    public double FramesPerContact { get; init; } = 0.4;
    [Positive] public double FloorFrames { get; init; } = 5.0;
    [Chance] public double SquareFraction { get; init; } = 0.9;
    /// <summary>
    /// The square press is this long before the ball reaches the plate (D13, #612 / #670): a
    /// human's eye times the ball meeting the bat, so the press leads the plate; the take is
    /// warped so its Contact mark meets the ball. 0.18 s, not the old press + 0.30 plane.
    /// </summary>
    [Positive] public double LeadSec { get; init; } = 0.18;
}

/// <summary>Charge adds loft; its power is the charge column of <see cref="QualityRules"/> (spec §5.5).</summary>
public sealed class SwingChargeRules
{
    public double LoftDeg { get; init; } = 2.5;
}

/// <summary>
/// Exit velocity by cursor zone (spec §5.2 table): the slap column at no charge, the charge column
/// at MAX, interpolated by the effective charge. Perfect charge is the ×1.25 of §5.5. Energy is
/// what the ball carries into a glove (<see cref="InPlay.Energy"/>).
/// </summary>
public sealed class QualityRules
{
    public ZoneExitRules Slap { get; init; } = new() { Perfect = 1.00, Nice = 0.95, Sour = 0.75 };
    public ZoneExitRules Charge { get; init; } = new() { Perfect = 1.25, Nice = 1.12, Sour = 0.95 };
    [Positive] public double PerfectEnergyMul { get; init; } = 1.25;
    [Positive] public double NiceEnergyMul { get; init; } = 1.0;
    [Positive] public double SourEnergyMul { get; init; } = 0.55;
}

public sealed class ZoneExitRules
{
    [Positive] public double Perfect { get; init; } = 1.0;
    [Positive] public double Nice { get; init; } = 1.0;
    [Positive] public double Sour { get; init; } = 1.0;

    public double For(ContactQuality quality) => quality switch
    {
        ContactQuality.Perfect => Perfect,
        ContactQuality.Nice => Nice,
        _ => Sour
    };
}

public sealed class ExitRules
{
    [Positive] public double BaseMph { get; init; } = 61;
    public double MphPerPower { get; init; } = 3.7;
    [Positive] public double TiredPitcherMul { get; init; } = 1.05;
}

/// <summary>
/// Launch (spec §5.4): base by power and charge, plus the pitch height (a low crossing launches
/// lower), plus the stick (up = over the top = grounder). Sour contact is forced to the topper
/// band (early) or the pop band (late, or a slap on a changeup / charged pitch).
/// </summary>
public sealed class LaunchRules
{
    public double LoftBaseDeg { get; init; } = 16;
    public double LoftPerPower { get; init; } = 1.0;
    /// <summary>Degrees of launch per foot the crossing sits above the zone center.</summary>
    public double PerFtOfHeight { get; init; } = 6;
    /// <summary>
    /// Stick U/D at contact. An ordinary swing reads it only while <see cref="BattingRules.GeometryOnly"/>
    /// is off; a Star Swing reads it either way (Phase 6). A bunt's launch is its own band and never read it.
    /// </summary>
    public double StickDeg { get; init; } = 12;
    public double NoiseDeg { get; init; } = 14;
    public double TopperMinDeg { get; init; } = 3;
    public double TopperSpanDeg { get; init; } = 9;
    public double PopMinDeg { get; init; } = 44;
    public double PopSpanDeg { get; init; } = 8;
    public double MinDeg { get; init; } = 3;
    public double MaxDeg { get; init; } = 52;
}

/// <summary>Bunt (spec §5.8): judged on the bat plane through the cursor; a high crossing or a sour bunt pops.</summary>
public sealed class BuntRules
{
    [Positive] public double ExitMul { get; init; } = 0.42;
    public double LaunchMinDeg { get; init; } = 3;
    public double LaunchSpanDeg { get; init; } = 9;
    public double SpraySpanDeg { get; init; } = 28;
    /// <summary>A crossing this far above the zone center is bunted into a pop.</summary>
    public double PopAboveCenterFt { get; init; } = 0.8;
}

/// <summary>
/// Direction (spec §5.3): early pulls, late pushes, linear across the window to ±timingDeg;
/// stick L/R shifts the range by ±stickDeg; the zone adds its spread.
/// </summary>
public sealed class SprayRules
{
    public double PerfectSpreadDeg { get; init; } = 8;
    public double NiceSpreadDeg { get; init; } = 18;
    public double SourSpreadDeg { get; init; } = 52;
    /// <summary>
    /// Stick L/R at contact. An ordinary swing reads it only while <see cref="BattingRules.GeometryOnly"/>
    /// is off; a bunt (until P4-b) and a Star Swing (until Phase 6) read it either way.
    /// </summary>
    public double StickDeg { get; init; } = 12;
    public double TimingDeg { get; init; } = 55;
    public double OutOfZoneSpanDeg { get; init; } = 18;
}

/// <summary>Sour contact pulled past the chalk (the chalk itself is <see cref="AtBatResolver.FoulLineDeg"/>, diamond geometry).</summary>
public sealed class FoulRules
{
    public double CheapPullMinDeg { get; init; } = 20;
    [Chance] public double CheapPullChance { get; init; } = 0.4;
    public double CheapPullPastDeg { get; init; } = 6;
    public double CheapPullSpanDeg { get; init; } = 14;
}

/// <summary>
/// The cursor (spec §5.2, D4): the bat drawn on the plate plane in world feet, centered where the
/// batter's box walk puts it, tall as the zone. Along the barrel the nice half-axis is
/// <see cref="NiceTipFt"/> toward the tip and <see cref="NiceHandleFt"/> toward the hands; the
/// perfect heart is <see cref="PerfectFraction"/> of that; the sour rim reaches
/// <see cref="RimFraction"/> beyond it. Bat (contact) scales the barrel; a charge narrows it;
/// buddies on base widen a slap (<see cref="BuddiesOnBaseRules"/>).
/// </summary>
public sealed class CursorRules
{
    [Positive] public double NiceTipFt { get; init; } = 1.05;
    [Positive] public double NiceHandleFt { get; init; } = 0.75;
    [Chance] public double PerfectFraction { get; init; } = 0.42;
    [Positive] public double RimFraction { get; init; } = 0.33;
    public double ScalePerContact { get; init; } = 0.04;
    [Positive] public double ChargeMul { get; init; } = 0.8;
}

/// <summary>Hit by pitch (spec §4.6): the body circle at the plate plane, in world feet.</summary>
public sealed class HbpRules
{
    [Positive] public double BodyRadiusFt { get; init; } = 0.45;
}

/// <summary>Star-swing rules that are not the skill's own numbers (those are star-skills.json, spec §13).</summary>
public sealed class StarSwingRules
{
    [Chance] public double PhonyballWhiff { get; init; } = 0.4;
    public double PrismballSpraySpanDeg { get; init; } = 22;
}

/// <summary>Good-chemistry runners on base (spec §5.2, §5.5): power on a charged swing, width on a slap.</summary>
public sealed class BuddiesOnBaseRules
{
    [Positive] public double OneMul { get; init; } = 1.10;
    [Positive] public double TwoMul { get; init; } = 1.25;
    [Positive] public double ThreeMul { get; init; } = 1.50;
    [Positive] public double WidenOne { get; init; } = 1.05;
    [Positive] public double WidenTwo { get; init; } = 1.10;
    [Positive] public double WidenThree { get; init; } = 1.20;
}

/// <summary>
/// The pitch's say in the exit (spec §5.5): a charged pitch met sour, a charged pitch met by a
/// perfect charge, and a high-Pitch arm dampening non-perfect contact per stat point above 5.
/// </summary>
public sealed class PitchFactorRules
{
    [Positive] public double ChargedVsSour { get; init; } = 0.6;
    [Positive] public double ChargedVsPerfectCharge { get; init; } = 1.1;
    [Chance] public double NiceDampPerPitch { get; init; } = 0.02;
    [Chance] public double SourDampPerPitch { get; init; } = 0.05;
}

/// <summary>
/// Items after contact (§12): each is a field effect with a duration, never a kind conversion.
/// A peel slips the fielder who steps on it, a rocket dazes the body it hits, a POW hops every
/// ball on the dirt once. They add time; the geometry then decides the play.
/// </summary>
public sealed class OffenseItemRules
{
    /// <summary>Flight of the item from the dugout to its aim, play seconds; it lands by geometry when the clock gets there (§12).</summary>
    [Positive] public double FlySec { get; init; } = 0.9;
    /// <summary>A peel is a spot on the grass: a body inside this radius of it slips (§12).</summary>
    [Positive] public double PeelRadiusFt { get; init; } = 5;
    /// <summary>How long the peel stays on the grass after it lands.</summary>
    [Positive] public double PeelSec { get; init; } = 6;
    /// <summary>The CPU offense throws its offered item when the batter's slack at first (§9.9 margin) is under this: the item is thrown when it would matter, never on a roll.</summary>
    [Signed] public double CpuThrowMarginSec { get; init; } = 0.3;
    /// <summary>A fielder on a peel cannot take the ball for this long.</summary>
    [Positive] public double SlipSec { get; init; } = 0.8;
    /// <summary>A dazed fielder cannot take the ball for this long.</summary>
    [Positive] public double DazeSec { get; init; } = 0.8;
    /// <summary>A POW keeps every ball on the dirt hopping, unscoopable, for this long.</summary>
    [Positive] public double PowHopSec { get; init; } = 0.8;
}

/// <summary>
/// The CPU batter (spec §5.9): a table evaluated when the ball reaches the plate plane, from the
/// final trajectory. Zone class by the crossing (middle third / edge / near / far), the swing by
/// count, the box by tracking (perfect, or the last pitch's crossing plus a fixed offset; worse
/// after the pitcher moved on the rubber), timing σ by Bat and the difficulty rung.
/// </summary>
public sealed class CpuBatterRules
{
    /// <summary>The middle third of the frame, as a fraction of its half-width and half-height.</summary>
    [Chance] public double MiddleFraction { get; init; } = 0.34;
    /// <summary>Outside the frame by at most this is "near" (chaseable); further is a take.</summary>
    public double NearFt { get; init; } = 0.4;
    [Chance] public double EdgeSwingChance { get; init; } = 0.65;
    /// <summary>Chase % = chaseBase − Bat, with fewer than two strikes …</summary>
    public double ChaseBase { get; init; } = 18;
    /// <summary>… and with two strikes.</summary>
    public double ChaseTwoStrikesBase { get; init; } = 30;
    /// <summary>Charge on 2-0 / 3-0 / 3-1 from this Bat …</summary>
    public int ChargeBatMin { get; init; } = 6;
    /// <summary>… and with a runner in scoring position and fewer than two outs from this Bat.</summary>
    public int RispChargeBatMin { get; init; } = 7;
    [Chance] public double SacBuntChance { get; init; } = 0.35;
    public int SacBuntBatMax { get; init; } = 5;
    public int SacBuntTrailMax { get; init; } = 2;
    public double SacBuntErrorSigma { get; init; } = 2.2;
    public double SacBuntSpraySigma { get; init; } = 10;
    public double SacBuntLaunchAim { get; init; } = 0.35;
    /// <summary>The headless CPU batter has been squared this long at the plate time (§7.3); the client's clock replaces it when there is one.</summary>
    [Positive] public double SacBuntSquareSec { get; init; } = 1.8;
    /// <summary>A captain with a star, a runner on or two strikes.</summary>
    [Chance] public double StarChance { get; init; } = 0.2;
    /// <summary>Timing σ = (11 − Bat) × this, frames.</summary>
    public double ErrorFramesPerBatStat { get; init; } = 0.62;
    /// <summary>Fooled by a changeup (late) or a charged pitch (early) when not tracked: this many frames …</summary>
    public double FooledMinFrames { get; init; } = 4;
    /// <summary>… plus up to this many more.</summary>
    public double FooledSpanFrames { get; init; } = 5;
    /// <summary>After release the batter re-reads the ball and centers the cursor on it this often.</summary>
    [Chance] public double TrackPerfectChance { get; init; } = 0.55;
    /// <summary>Otherwise the box stays at the guess (the last pitch's crossing) plus a fixed offset of this …</summary>
    public double MistrackMinFt { get; init; } = 0.11;
    /// <summary>… plus up to this.</summary>
    public double MistrackSpanFt { get; init; } = 0.30;
    /// <summary>The re-read fails this often when the pitcher moved on the rubber since the last pitch …</summary>
    [Chance] public double MistrackMovedChance { get; init; } = 0.7;
    /// <summary>… and this often when they did not (both × the rung's mistrackMul).</summary>
    [Chance] public double MistrackChance { get; init; } = 0.3;
    public double SpraySigmaDeg { get; init; } = 12;
    public double LaunchAimSigma { get; init; } = 0.45;
    public CpuArchetypeRules Archetype { get; init; } = new();
    /// <summary>
    /// The CPU batter commits this long before the square press (plate − batting.window.leadSec),
    /// from the trajectory as it stands then (spec §3, §5.9). Its earliest error is −decideLeadSec × 60 frames.
    /// </summary>
    [Positive] public double DecideLeadSec { get; init; } = 0.12;
}

/// <summary>Charge vs slap by archetype (spec §5.9), derived from the Bat / Run split.</summary>
public sealed class CpuArchetypeRules
{
    [Chance] public double Balanced { get; init; } = 0.5;
    [Chance] public double Power { get; init; } = 0.8;
    [Chance] public double Speed { get; init; } = 0.3;
    [Chance] public double Technique { get; init; } = 0.1;
    /// <summary>Bat − Run at least this = power; Run − Bat at least this = speed.</summary>
    public int SplitStat { get; init; } = 2;
    /// <summary>Both Bat and Run at least this = technique.</summary>
    public int TechniqueMin { get; init; } = 7;
}

// ---------------------------------------------------------------------------------------
// flight.json — §6
// ---------------------------------------------------------------------------------------

public sealed class FlightRules
{
    [Positive] public double Gravity { get; init; } = 32.174;
    public double Drag { get; init; } = 0.0019;
    /// <summary>
    /// Arcade hang: sample times are stretched by this so gloves can get under a fly. Carry
    /// does not change. The stretched sample clock <em>is</em> the play clock fielders and
    /// runners run on (<see cref="LivePlaySystem.ElapsedSeconds"/>) — one clock (§0.3, §6.1).
    /// </summary>
    [Positive] public double TimeScale { get; init; } = 1.65;
    /// <summary>
    /// The liner's stretch (§7.6): a rope has the short hang so a dive is possible and a ball
    /// past the glove falls in. Read off the launch class at the crack (<see cref="BattedBallClasses.ByLaunch"/>).
    /// </summary>
    [Positive] public double LinerTimeScale { get; init; } = 1.0;
    /// <summary>The stretch for a ball on the dirt (topper, grounder, chopper, bunt): the scoop is a race (§7.1).</summary>
    [Positive] public double DirtTimeScale { get; init; } = 1.65;
    [Positive] public double PlateHeightFt { get; init; } = 2.5;

    /// <summary>The stretch this contact's flight runs on, by its launch class (§6.1, §6.2).</summary>
    public double TimeScaleFor(double launchDeg, double exitMph, RulesTable table)
    {
        var shape = BattedBallClasses.ByLaunch(launchDeg, exitMph, table);
        if (shape == BattedBallClass.Liner) return LinerTimeScale;
        return shape.OnTheDirt() ? DirtTimeScale : TimeScale;
    }
    /// <summary>How much of the flag reading the ball feels at field level (drag is taken relative to the wind).</summary>
    public double WindMul { get; init; } = 0.35;
    [Positive] public int SampleHz { get; init; } = 120;
    [Positive] public double MaxSeconds { get; init; } = 12;
    public BounceRules Bounce { get; init; } = new();
    public SkidRules Skid { get; init; } = new();
    public RollRules Roll { get; init; } = new();
    public WallRules Wall { get; init; } = new();
    public LandingRules Landing { get; init; } = new();
    public BattedBallClassRules Classes { get; init; } = new();
    public DeadBallRules DeadBall { get; init; } = new();

    /// <summary>
    /// This table with one park's air (§0.3, FD-03): <c>dragMul</c> multiplies the root's drag and
    /// <c>windMul</c> replaces the global exposure. Every other number — gravity, the three stretches, the
    /// plate, the sample clock — is the global one, and the ground, wall, landing, class and dead-ball
    /// blocks are shared by reference: a park does not own them (they are F3-b / F3-c's zone and span rows).
    /// Built once per match by <see cref="RulesTable.AtPark"/>, never per flight.
    /// </summary>
    internal FlightRules WithEnvironment(ParkEnvironment env) => new()
    {
        Gravity = Gravity,
        Drag = Drag * (env.DragMul ?? 1.0),
        TimeScale = TimeScale,
        LinerTimeScale = LinerTimeScale,
        DirtTimeScale = DirtTimeScale,
        PlateHeightFt = PlateHeightFt,
        WindMul = env.WindMul ?? WindMul,
        SampleHz = SampleHz,
        MaxSeconds = MaxSeconds,
        Bounce = Bounce,
        Skid = Skid,
        Roll = Roll,
        Wall = Wall,
        Landing = Landing,
        Classes = Classes,
        DeadBall = DeadBall
    };

    internal void Validate(string source, List<string> errors)
    {
        RulesValidation.Order(source, "flight.skid.launchMinDeg", Skid.LaunchMinDeg, Skid.LaunchMaxDeg, errors);
        RulesValidation.Order(source, "flight.classes.topperMaxLaunchDeg", Classes.TopperMaxLaunchDeg, Classes.GrounderMaxLaunchDeg, errors);
        RulesValidation.Order(source, "flight.classes.grounderMaxLaunchDeg", Classes.GrounderMaxLaunchDeg, Classes.ChopperMaxLaunchDeg, errors);
        RulesValidation.Order(source, "flight.classes.chopperMaxLaunchDeg", Classes.ChopperMaxLaunchDeg, Classes.LinerMaxLaunchDeg, errors);
    }
}

public sealed class BounceRules
{
    [Chance] public double Restitution { get; init; } = 0.48;
    [Chance] public double Horizontal { get; init; } = 0.82;
    public double MinVy { get; init; } = 3.6;
}

/// <summary>Liners skid instead of hopping.</summary>
public sealed class SkidRules
{
    public double LaunchMinDeg { get; init; } = 14;
    public double LaunchMaxDeg { get; init; } = 22;
    public double MinVy { get; init; } = 2.2;
    [Chance] public double Restitution { get; init; } = 0.28;
    [Chance] public double Horizontal { get; init; } = 0.93;
}

public sealed class RollRules
{
    [Positive] public double Friction { get; init; } = 22;
    public double RestSpeed { get; init; } = 1.4;
}

/// <summary>The outfield fence below fence height: the carom (§7.9). Normal speed × restitution, along the wall × tangential.</summary>
public sealed class WallRules
{
    [Chance] public double Restitution { get; init; } = 0.48;
    [Chance] public double Tangential { get; init; } = 0.82;
}

public sealed class LandingRules
{
    /// <summary>Samples before this play time are still leaving the bat: the ground does not exist yet (one time base for every landing guard).</summary>
    public double FirstGrassMinSec { get; init; } = 0.08;
}

/// <summary>The one batted-ball class table (§6.2): launch and exit at contact, refined by the flight.</summary>
public sealed class BattedBallClassRules
{
    /// <summary>Below this launch: topper (a weak roller in front of the plate).</summary>
    public double TopperMaxLaunchDeg { get; init; } = 3;
    /// <summary>Topper … this: grounder (an infield hop).</summary>
    public double GrounderMaxLaunchDeg { get; init; } = 10;
    /// <summary>Topper … this with a high first bounce inside the plate: chopper.</summary>
    public double ChopperMaxLaunchDeg { get; init; } = 14;
    public double ChopperFirstBounceFt { get; init; } = 30;
    public double ChopperBounceHeightFt { get; init; } = 3;
    public double ChopperMinExitMph { get; init; } = 70;
    /// <summary>Grounder … this with real exit: liner (a rope).</summary>
    public double LinerMaxLaunchDeg { get; init; } = 22;
    public double LinerMinExitMph { get; init; } = 74;
    /// <summary>Dirt / grass lip past the rubber. A fly landing inside it is a pop; infielders own the hop inside it.</summary>
    [Positive] public double InfieldLipFt { get; init; } = 155;
}

/// <summary>Hit type by carry when nobody catches it. P3 replaces these with runner geometry.</summary>
public sealed class DeadBallRules
{
    public double MinSec { get; init; } = 2.4;
    public double AfterHangSec { get; init; } = 0.35;
    /// <summary>A flight nobody plays holds this long past the ball's rest (or exit) before the result.</summary>
    public double RestHoldSec { get; init; } = 0.2;
}

// ---------------------------------------------------------------------------------------
// grounds.json — the closed ground library (§6.1, §16; FD-05, F3-b)
// ---------------------------------------------------------------------------------------

/// <summary>
/// What the ball does on one kind of ground: one named row per id in <see cref="Ground"/>, the way
/// <see cref="PitchFamilyTable"/> has one named row per pitch family (D20). Named properties, not a
/// <c>Dictionary</c> — a dictionary bypasses the reflective range walk, the unknown-key refusal and
/// the JSON = code parity test, and a row that slipped into one would load and be checked by nothing
/// (FR-02).
///
/// <para>
/// <b>Every row here is today's global number.</b> F3-b (#846) built the library at parity: `grass`,
/// `dirt`, `ice` and `ash` all carry exactly what <c>flight.json</c>'s <c>roll</c>, <c>bounce</c> and
/// <c>skid</c> blocks carry, so the ground under the ball cannot change a play. The flight and the
/// three loose-ball models still read <see cref="FlightRules"/>'s own copy; F3-c moves those reads to
/// the zone under the ball, and only then does a row that differs mean anything. Until it does,
/// <c>GroundLibraryTests</c> asserts every row equals the flight block field for field, so the two
/// copies cannot drift apart while both exist. A ground value that is <em>not</em> today's arrives
/// with Crystal (F9-a), as a trial, after Jack has accepted it.
/// </para>
/// </summary>
public sealed class GroundLibrary
{
    /// <summary>The outfield of every shipped park. The rows are equal today, so this is also the ball's only roll.</summary>
    public GroundRules Grass { get; init; } = new();

    /// <summary>The infield and the warning track of every park, whatever its <c>surface</c> says (<see cref="GroundZones"/>).</summary>
    public GroundRules Dirt { get; init; } = new();

    /// <summary>Crystal Rink's surface. Its numbers are grass's until F9-a measures a trial.</summary>
    public GroundRules Ice { get; init; } = new();

    /// <summary>Ember Keep's surface. Its numbers are grass's until F9-a measures a trial.</summary>
    public GroundRules Ash { get; init; } = new();

    /// <summary>This table's row for a library id, or null for an id the library does not have.</summary>
    GroundRules? Named(string id) => id switch
    {
        Ground.Grass => Grass,
        Ground.Dirt => Dirt,
        Ground.Ice => Ice,
        Ground.Ash => Ash,
        _ => null
    };

    /// <summary>True for an id <b>this table</b> has a row for (<c>SF-03</c>).</summary>
    public bool Has(string? id) => id is not null && Named(id) is not null;

    /// <summary>The ids this table has rows for, in library order.</summary>
    public IReadOnlyList<string> Ids => Ground.All;

    /// <summary>
    /// The row for a ground id (<c>SF-03</c>). There is no silent fallback: a park whose zone names an
    /// id with no row is a park playing a ground nobody authored, and it stops rather than quietly
    /// rolling on grass. The message names the id and the file, because the two things a reader needs
    /// are what was asked for and where the rows live.
    /// </summary>
    public GroundRules Of(string? id)
    {
        if (id is not null && Named(id) is { } row) return row;
        throw new ArgumentException(
            $"'{id}' is not a ground with a row in {RulesTable.Directory}/grounds.json; "
            + $"the library is [{string.Join(", ", Ground.All)}] (Ground). A new ground is a row in that "
            + "file and an id in the library, never a fallback (FR-02, SF-03).", nameof(id));
    }

    /// <summary>
    /// The rule across a row's fields the attributes cannot say, checked on every row and on both
    /// roots: the skid band has to be a band. It is the same order <see cref="FlightRules.Validate"/>
    /// holds for <c>flight.skid</c>, held here per row so a trial that authors one ground cannot write
    /// a band that never opens.
    /// </summary>
    internal void Validate(string source, List<string> errors)
    {
        foreach (var id in Ids)
        {
            var row = Of(id);
            RulesValidation.Order(source, $"grounds.{id}.skid.launchMinDeg", row.Skid.LaunchMinDeg, row.Skid.LaunchMaxDeg, errors);
        }
    }
}

/// <summary>
/// One ground's numbers. The three blocks are the <em>same types</em> <see cref="FlightRules"/>
/// carries — <see cref="RollRules"/>, <see cref="BounceRules"/>, <see cref="SkidRules"/> — rather than
/// near-copies under new names, for two reasons. The parity this child ships is then literally field
/// for field, with nothing to translate; and F3-c's move is a change of <em>which</em> object the
/// flight reads, not a rewrite of how it reads one.
///
/// <para>
/// <c>skid.launchMinDeg</c> / <c>launchMaxDeg</c> are a property of the <em>ball</em> (the launch band
/// a rope skids in), not of the ground, and they ride along here because the child's contract is the
/// whole skid block at parity. Which fields of a zone's row the flight actually reads is F3-c's to
/// decide; a row whose band differed from the flight's would be a behavior change, so today none does.
/// </para>
/// </summary>
public sealed class GroundRules
{
    /// <summary>The ball rolling on this ground: friction and the speed it comes to rest at.</summary>
    public RollRules Roll { get; init; } = new();

    /// <summary>The hop off this ground.</summary>
    public BounceRules Bounce { get; init; } = new();

    /// <summary>The rope's skid across this ground, band included.</summary>
    public SkidRules Skid { get; init; } = new();
}

// ---------------------------------------------------------------------------------------
// walls.json — the closed wall-material library (§6.1, §16; FD-06, F3-b)
// ---------------------------------------------------------------------------------------

/// <summary>
/// What the ball does off one kind of wall: one named row per id in <see cref="WallMaterial"/>, the
/// same shape as <see cref="GroundLibrary"/> and for the same reasons (FR-02). One row today —
/// <c>padded</c>, carrying exactly <c>flight.wall</c> — because every span of every park is the one
/// padded wall the ball caroms off now.
///
/// <para>
/// <b>A material, not a span.</b> Which span of which fence is made of what, and whether it can be
/// climbed or robbed over, is the polyline fence's business (FD-06, F2-c): a trait like
/// <c>climbable</c> belongs to a stretch of wall, not to the stuff it is made of, and putting one here
/// would make the library answer a question it cannot see. The flight still reads
/// <see cref="FlightRules.Wall"/>; F3-c moves that read to the span's row.
/// </para>
/// </summary>
public sealed class WallMaterialLibrary
{
    /// <summary>The padded outfield wall every park has today. Exactly <c>flight.wall</c>.</summary>
    public WallRules Padded { get; init; } = new();

    WallRules? Named(string id) => id switch
    {
        WallMaterial.Padded => Padded,
        _ => null
    };

    /// <summary>True for an id <b>this table</b> has a row for (<c>SF-03</c>).</summary>
    public bool Has(string? id) => id is not null && Named(id) is not null;

    /// <summary>The ids this table has rows for, in library order.</summary>
    public IReadOnlyList<string> Ids => WallMaterial.All;

    /// <summary>
    /// The row for a wall material (<c>SF-03</c>). A span that names a material with no row stops and
    /// names the id and the file, rather than caroming off whatever the first row happens to be.
    /// </summary>
    public WallRules Of(string? id)
    {
        if (id is not null && Named(id) is { } row) return row;
        throw new ArgumentException(
            $"'{id}' is not a wall material with a row in {RulesTable.Directory}/walls.json; "
            + $"the library is [{string.Join(", ", WallMaterial.All)}] (WallMaterial). A new material is a "
            + "row in that file and an id in the library, never a fallback (FR-02, SF-03).", nameof(id));
    }
}

// ---------------------------------------------------------------------------------------
// fielders.json — §8.1
// ---------------------------------------------------------------------------------------

/// <summary>
/// Where the seven gloves stand with nobody on, in feet from home, read by
/// <see cref="Diamond.Positions"/> (#725). Home is the origin, +Z runs toward second and centre,
/// +X toward first, so the left side of the field is negative X.
///
/// <para>
/// Only seven. The pitcher stands on <c>infield.moundFt</c> and the catcher on
/// <see cref="HomeSet.CatcherZ"/>; naming either here would be a second source of truth for a
/// spot that is already decided elsewhere.
/// </para>
///
/// <para>
/// <b>One global set, outfield included (#730, decision 3).</b> Every park shares these — that is
/// what ships today, and per-park depth is new behaviour that stays with #713. A trial that wants
/// a smaller field writes the whole file. The infield four scale with the basepath; the outfield
/// three are placed by keeping each body's bearing and its fraction of the fence <em>at that
/// bearing</em>, measured through the circular fence arc (<see cref="AtBatResolver.FenceAt"/>).
/// That is not one scale factor applied to these numbers, and it is not the park fence scale:
/// LF and RF keep 0.7203 of a 379.16-ft fence at ∓23.75°, centre keeps 0.7625 of 400, and the
/// two fractions are different. See <c>docs/research-game-feel-730.md</c>.
/// </para>
/// </summary>
public sealed class FielderRules
{
    public FielderSpotRules First { get; init; } = new() { XFt = 78, ZFt = 72 };
    public FielderSpotRules Second { get; init; } = new() { XFt = 42, ZFt = 118 };
    public FielderSpotRules Third { get; init; } = new() { XFt = -78, ZFt = 72 };
    public FielderSpotRules Short { get; init; } = new() { XFt = -42, ZFt = 118 };
    public FielderSpotRules Left { get; init; } = new() { XFt = -110, ZFt = 250 };
    public FielderSpotRules Center { get; init; } = new() { XFt = 0, ZFt = 305 };
    public FielderSpotRules Right { get; init; } = new() { XFt = 110, ZFt = 250 };

    /// <summary>
    /// The start for <paramref name="pos"/>, spelled the way the rest of the sim spells a position.
    /// "P" and "C" are not this table's to answer — <see cref="Diamond.Positions"/> holds those.
    /// </summary>
    public (double X, double Z) Spot(string pos) => pos switch
    {
        "1B" => (First.XFt, First.ZFt),
        "2B" => (Second.XFt, Second.ZFt),
        "3B" => (Third.XFt, Third.ZFt),
        "SS" => (Short.XFt, Short.ZFt),
        "LF" => (Left.XFt, Left.ZFt),
        "CF" => (Center.XFt, Center.ZFt),
        "RF" => (Right.XFt, Right.ZFt),
        _ => throw new ArgumentOutOfRangeException(nameof(pos), pos, "fielders.json names 1B, 2B, 3B, SS, LF, CF and RF")
    };

    /// <summary>
    /// Left field is on the left, and right field is on the right. The sides are a frame, not a
    /// preference — the lineup toy measures its mini-diamond against the right fielder's x, so a
    /// table that put both corners on one side would draw a field folded in half rather than fail
    /// anywhere a reader would look.
    ///
    /// <para>
    /// Each side is named on its own rather than only ordered against the other, because ordering
    /// alone admits the one table that actually breaks: <c>left.xFt == right.xFt == 0</c> satisfies
    /// <c>left &lt;= right</c>, <c>[Signed]</c> permits zero, and <c>ChemistryToy.MiniSpot</c> then
    /// divides by it. Before #725 that divisor was the constant 110; it is a table value now, so the
    /// table is where it gets checked.
    /// </para>
    /// </summary>
    public void Validate(string source, List<string> errors)
    {
        RulesValidation.Order(source, "fielders.left.xFt", Left.XFt, Right.XFt, errors);
        if (!(Left.XFt < 0))
            errors.Add($"{source}: fielders.left.xFt must be left of the centre line; got {Left.XFt}");
        if (!(Right.XFt > 0))
            errors.Add($"{source}: fielders.right.xFt must be right of the centre line; got {Right.XFt}");
    }
}

/// <summary>One body's start. <see cref="XFt"/> is signed; <see cref="ZFt"/> is in front of home.</summary>
public sealed class FielderSpotRules
{
    /// <summary>Off the centre line. Negative toward third and left.</summary>
    [Signed] public double XFt { get; init; }

    /// <summary>Out from home, along the centre line.</summary>
    [Positive] public double ZFt { get; init; }
}

// ---------------------------------------------------------------------------------------
// fielding.json — §8
// ---------------------------------------------------------------------------------------

public sealed class FieldingRules
{
    public ChaseRules Chase { get; init; } = new();
    public ReactionRules Reaction { get; init; } = new();
    public CoverRules Cover { get; init; } = new();
    public FieldStickRules Stick { get; init; } = new();
    public FieldDashRules Dash { get; init; } = new();
    public CatchRules Catch { get; init; } = new();
    public DropRules Drops { get; init; } = new();
    public WallPlantRules WallPlant { get; init; } = new();
    public FieldAbilityRules Abilities { get; init; } = new();
    public ThrowRules Throw { get; init; } = new();
    public OverthrowRules Overthrow { get; init; } = new();
    public CatcherRules Catcher { get; init; } = new();
    public ThrowChemistryRules Chem { get; init; } = new();
    public BobbleRules Bobble { get; init; } = new();
    public KnockbackRules Knockback { get; init; } = new();
    public RecoilRules Recoil { get; init; } = new();
    public HandlingRules Handling { get; init; } = new();
    public ParkHazardRules Park { get; init; } = new();
    public BuntDefenseRules Bunt { get; init; } = new();

    internal void Validate(string source, List<string> errors)
    {
        RulesValidation.Order(source, "fielding.catcher.cpuReleaseMinSec", Catcher.CpuReleaseMinSec, Catcher.CpuReleaseMaxSec, errors);
        RulesValidation.Order(source, "fielding.chem.slantLateralMinFt", Chem.SlantLateralMinFt, Chem.SlantLateralMaxFt, errors);
        RulesValidation.Order(source, "fielding.throw.minFtPerSec", Throw.MinFtPerSec, Throw.BaseFtPerSec, errors);
        RulesValidation.Order(source, "fielding.stick.leaveMag", Stick.LeaveMag, Stick.EnterMag, errors);
        RulesValidation.Order(source, "fielding.stick.leaveMag", Stick.LeaveMag, 0.99, errors);
        RulesValidation.Order(source, "fielding.recoil.onsetFtPerSec", Recoil.OnsetFtPerSec, Recoil.FullFtPerSec, errors);
        if (Recoil.Active && Recoil.FullFtPerSec <= Recoil.OnsetFtPerSec)
            errors.Add($"{source}: fielding.recoil.fullFtPerSec must exceed onsetFtPerSec while the recoil is on; got {Recoil.FullFtPerSec} <= {Recoil.OnsetFtPerSec}");
        RulesValidation.Order(source, "fielding.recoil.airOnsetFtPerSec", Recoil.AirOnsetFtPerSec, Recoil.AirFullFtPerSec, errors);
        RulesValidation.Order(source, "fielding.handling.hopMinApexFt", Handling.HopMinApexFt, Handling.HopFullApexFt, errors);
        RulesValidation.Order(source, "fielding.handling.bobbleSettleFt", Handling.BobbleSettleFt, Handling.BobbleReboundCapFt, errors);
        RulesValidation.Order(source, "fielding.handling.deflectRetainMin", Handling.DeflectRetainMin, Handling.DeflectRetainMax, errors);
        if (Recoil.AirActive && Recoil.AirFullFtPerSec <= Recoil.AirOnsetFtPerSec)
            errors.Add($"{source}: fielding.recoil.airFullFtPerSec must exceed airOnsetFtPerSec while the airborne recoil is on; got {Recoil.AirFullFtPerSec} <= {Recoil.AirOnsetFtPerSec}");
    }
}

/// <summary>
/// The human seat's pursuit stick (#718: F693-02-pursuit-analog-response, -neutral-boundary, -calibration-policy,
/// -arming, -calibration-samples). At <c>enterMag</c> 0 the stick is the one the game shipped with — a Manhattan gate
/// at <c>feel.fieldAssistStick</c>, the full stick vector as the asked velocity, no calibration, no arming. Above 0 it
/// is the calibrated radial stick: manual pursuit from <c>enterMag</c>, back to assistance at <c>leaveMag</c>, the owner
/// kept between the two, and the asked speed the linear remap of the magnitude from <c>leaveMag</c> to 1 (half the
/// usable range asks half the speed). A seat arms with a valid profile plus one neutral observation (≤ <c>leaveMag</c>)
/// on defensive-role entry, device recovery or recalibration, and stays armed through the half. The calibration window
/// is <c>calibrationSec</c> of released-stick samples on an input clock with a mean centre offset ≤ <c>centerOffsetMax</c>
/// and every sample ≤ <c>sampleSpreadMax</c> from that mean, inclusive; only a complete valid window is adopted.
/// </summary>
public sealed class FieldStickRules
{
    [Chance] public double EnterMag { get; init; } = 0;
    [Chance] public double LeaveMag { get; init; } = 0;
    [Positive] public double CalibrationSec { get; init; } = 0.50;
    [Chance] public double CenterOffsetMax { get; init; } = 0.10;
    [Chance] public double SampleSpreadMax { get; init; } = 0.02;
    /// <summary>The calibrated radial stick is on above <c>enterMag</c> 0; at 0 every read is the shipped Manhattan gate.</summary>
    public bool Radial => EnterMag > 0;
}

/// <summary>
/// Reaction lockout after contact before a body moves, by position (§8.2, reference frames → seconds:
/// P 25, C 40, IF 15–18, OF 50). A CPU-driven body waits it × the rung's <c>cpu.reactionMul</c>; the
/// human glove waits the reference at every rung; a ball in the air caps it at its hang
/// (<see cref="FieldingResolver.ReactionLockouts"/>). The CPU fielder's delay before a throw is
/// <c>throwBaseSec − Field × throwPerFieldSec</c> (§8.8), × the difficulty's reaction multiplier.
/// </summary>
public sealed class ReactionRules
{
    [Positive] public double PitcherSec { get; init; } = 0.42;
    [Positive] public double CatcherSec { get; init; } = 0.67;
    [Positive] public double FirstSec { get; init; } = 0.27;
    [Positive] public double SecondSec { get; init; } = 0.25;
    [Positive] public double ThirdSec { get; init; } = 0.30;
    [Positive] public double ShortSec { get; init; } = 0.28;
    [Positive] public double OutfieldSec { get; init; } = 0.83;
    [Positive] public double ThrowBaseSec { get; init; } = 0.35;
    public double ThrowPerFieldSec { get; init; } = 0.02;
    [Positive] public double ThrowMinSec { get; init; } = 0.08;

    /// <summary>Seconds after contact before the body at <paramref name="pos"/> may move.</summary>
    public double LockoutSec(string pos) => pos switch
    {
        "P" => PitcherSec,
        "C" => CatcherSec,
        "1B" => FirstSec,
        "2B" => SecondSec,
        "3B" => ThirdSec,
        "SS" => ShortSec,
        _ => OutfieldSec
    };
}

/// <summary>
/// Cover, cutoff and backup bodies (§8.7): on the shipped table a flat speed (D11) after a start delay, gated by the
/// body's reaction lockout; on the c80 copy the body's own pursuit speed from contact with no read
/// (F693-02-coverage-budget, #718) — covering is a known assignment to a fixed spot, not the recognition of where a
/// ball went. A throw is caught inside the cover radius of its target.
/// </summary>
public sealed class CoverRules
{
    /// <summary>The flat cover speed (D11); <see cref="ChaseSpeedWeight"/> blends the body's own speed over it.</summary>
    [Positive] public double FtPerSec { get; init; } = 28;
    /// <summary>Cover starts walking this long after contact. 0 is at contact.</summary>
    public double StartSec { get; init; } = 0.23;
    public double StopFt { get; init; } = 1.2;
    /// <summary>A throw landing farther than this from the receiver is not caught: it skips past, live (§8.5).</summary>
    [Positive] public double RadiusFt { get; init; } = 6;
    /// <summary>The backup body stands this far behind a throw's target, on its line.</summary>
    [Positive] public double BackupFt { get; init; } = 60;
    /// <summary>How much of the body's reaction lockout (§8.2) gates its cover walk: 1 is the shipped rule, 0 the c80 rule (#718).</summary>
    [Chance] public double LockoutMul { get; init; } = 1;
    /// <summary>How much of the body's own pursuit speed (<c>fielding.chase</c>) a cover, cutoff or backup walk uses in place of <see cref="FtPerSec"/>: 0 is the flat speed exactly, 1 the body's speed (#718).</summary>
    [Chance] public double ChaseSpeedWeight { get; init; } = 0;
}

/// <summary>
/// The bunt defense (§7.3, <c>fielding.bunt</c>): who crashes and how far when the batter squares, who covers
/// first and second behind the crash, who charges the triangle after contact, and the throw rule's numbers
/// (a bunt too hard for the sac is played at the lead force; the squeeze runner is thrown for only from
/// inside the plate distance). Positions are the diamond's keys; <see cref="BuntDefense"/> reads them.
/// </summary>
public sealed class BuntDefenseRules
{
    /// <summary>The crash bodies run this far toward the plate from their spots (25 in the reference).</summary>
    [Positive] public double CrashFt { get; init; } = 25;
    /// <summary>Who crashes at the square.</summary>
    public string[] Crash { get; init; } = ["1B", "3B"];
    /// <summary>Who covers first behind the crash.</summary>
    public string CoverFirst { get; init; } = "2B";
    /// <summary>Who covers second behind the crash.</summary>
    public string CoverSecond { get; init; } = "SS";
    /// <summary>Who converges on a bunt after contact (the triangle); the glove among them is the pursuit planner's.</summary>
    public string[] Charge { get; init; } = ["P", "C", "1B", "3B"];
    /// <summary>A charging body that is not the glove stops this far from the ball.</summary>
    [Positive] public double ChargeStopFt { get; init; } = 6;
    /// <summary>On a squeeze the glove throws home only from inside this distance to the plate.</summary>
    [Positive] public double SqueezeHomeFt { get; init; } = 30;
    /// <summary>A bunt leaving the bat at or above this is too hard for the sac: the lead force is played if makeable.</summary>
    [Positive] public double HardExitMph { get; init; } = 36;
}

/// <summary>A throw that misses its cover, or drops at an uncovered bag, rolls on from where it landed.</summary>
public sealed class OverthrowRules
{
    /// <summary>A sailed throw keeps this much of its flight speed past the target.</summary>
    [Chance] public double CarryMul { get; init; } = 0.35;
    [Positive] public double DecelFtPerSec2 { get; init; } = 18;
    [Positive] public double MaxRollFt { get; init; } = 45;
}

public sealed class ChaseRules
{
    [Positive] public double BaseFtPerSec { get; init; } = 21;
    public double FtPerSecPerRun { get; init; } = 1.9;
    [Positive] public double FrozenMul { get; init; } = 0.45;
    [Positive] public double MinFtPerSec { get; init; } = 8;
    public double StepStopFt { get; init; } = 0.35;
    /// <summary>A route counts as reachable when the glove lands within this of the meet point.</summary>
    public double ReachSlackFt { get; init; } = 0.35;
    /// <summary>After Select / R swaps the glove, another press is ignored and the CPU chase waits for this long (§8.9).</summary>
    public double SwapLockSec { get; init; } = 0.7;
    /// <summary>After a hand-off the body the ring left keeps its velocity for this long, then stops (§8.9): the swap does not jerk.</summary>
    public double HandoffCoastSec { get; init; } = 0.2;
    /// <summary>The nearest body to a loose ball chases it; a throw's receiver steps to a ball inside this of them.</summary>
    [Positive] public double LooseScoopFt { get; init; } = 3.5;
    /// <summary>
    /// An outfielder chasing a ball hit in the air runs at the one glove speed × this (§8.1, §8.2). The S-29 lever since the
    /// outfield read went back to the reference (#609): the read is when a body starts, this is how much ground it covers.
    /// </summary>
    [Positive] public double OutfieldAirMul { get; init; } = 0.6;
    /// <summary>
    /// An infielder (P, C, 1B, 2B, 3B, SS) under a ball on the stretched clock (a fly or a pop, §6.1) runs at the one glove speed × this
    /// (§8.1). The other half of the S-29 lever (#636): once the hand-off honours the infielder's route in the air (D17), this is how far
    /// the infield reaches back under a short fly past the lip. A liner runs on its own clock and an infielder runs the one speed at it;
    /// balls on the dirt run the one speed the §10.4 double-play rows were tuned on.
    /// </summary>
    [Positive] public double InfieldAirMul { get; init; } = 0.45;
    /// <summary>
    /// The response law (#718, F693-02-carry-movement-response): seconds from rest to the body's rated speed, a linear ramp.
    /// 0 is the instant step the game shipped with; the c80 copy carries 0.20. The pursuit planner charges half of it to a route.
    /// </summary>
    public double AccelSec { get; init; } = 0;
    /// <summary>Seconds from the rated speed to rest, a constant deceleration; a reversal is this brake and then the ramp. 0 is the instant stop; the c80 copy carries 0.10.</summary>
    public double BrakeSec { get; init; } = 0;
}

public sealed class CatchRules
{
    [Positive] public double RadiusBaseFt { get; init; } = 10;
    public double RadiusPerField { get; init; } = 0.6;
    /// <summary>
    /// The authored stand-up reach every body without its own <see cref="Character.ReachFt"/> gets (F693-02-catch-reach-envelope,
    /// #719): roughly what the visible glove covers from a planted stance, independent of ratings. 0 keeps the legacy
    /// <c>radiusBaseFt + Field × radiusPerField</c> the game shipped with; the c80 copy carries 6.0. A character's authored
    /// <c>reachFt</c> wins over both, and the ability bonuses add to whichever applies.
    /// </summary>
    public double StandUpReachFt { get; init; } = 0;
    public double ClamberRadiusFt { get; init; } = 6;
    public double WindowPadFt { get; init; } = 4;
    public double DiveReachFt { get; init; } = 8;
    public double JumpReachFt { get; init; } = 8;
    public double DiveMaxBallY { get; init; } = 7.5;
    public double NeedsJumpReachFt { get; init; } = 22;
    /// <summary>Rob heights (§8.4): a leap at the wall takes a ball clearing the fence by at most this.</summary>
    public double JumpRobFt { get; init; } = 4;
    public double SuperJumpRobFt { get; init; } = 18;
    public double ClamberRobFt { get; init; } = 28;
    public double BuddyJumpRobFt { get; init; } = 18;
    public double TouchScoopY { get; init; } = 3.2;
    /// <summary>
    /// Still in the air for a catch (§7.6): a route that meets the ball above this before the first
    /// bounce is a catch; at or below it the hop is a scoop. Chase targeting uses the same floor.
    /// </summary>
    [Positive] public double InAirMinY { get; init; } = 0.75;
    public double JumpBallY { get; init; } = 2.2;
    public double WallBallY { get; init; } = 4.5;
    public double WindowBeforeSec { get; init; } = 0.48;
    public double WindowAfterSec { get; init; } = 0.14;
    public double WallSitSec { get; init; } = 1.15;
    public double SuperJumpWindowSec { get; init; } = 0.16;
    public double GrowWindowSec { get; init; } = 0.08;
    public double ClamberWindowSec { get; init; } = 0.12;
    public double BuddyPlantFt { get; init; } = 26;
    public double BuddyJumpHoldSec { get; init; } = 0.18;
    public double HeldBallY { get; init; } = 2.2;
    public double BuddyHeldBallY { get; init; } = 6.4;
    public double BuddyLeapBallY { get; init; } = 2.2;
    public double HoverLeadSec { get; init; } = 0.4;
    public double HoverMinSec { get; init; } = 0.25;
    /// <summary>West arms the leap for this long (§8.4); longer when the play is at the wall.</summary>
    public double JumpArmSec { get; init; } = 0.55;
    public double WallJumpArmSec { get; init; } = 0.7;
    /// <summary>East arms the dive reach for this long.</summary>
    public double DiveArmSec { get; init; } = 0.5;
    /// <summary>
    /// The dive without a decision (#719, F693-02-dive-jump-scoop-reach, -cpu-dive-intent): 1 is the game as shipped — the
    /// dead-stick assistance and the CPU dive at the rim on their own, for free; 0 is the c80 rule — a dive is a press, or
    /// the CPU's deliberate commitment on the live ball, never free.
    /// </summary>
    [Chance] public double AutoDive { get; init; } = 1;
    /// <summary>
    /// What a dive costs (F693-02-dive-recovery-cost): the diver neither moves nor throws for this long after the
    /// commitment, at Field 1, caught or missed alike, human and CPU alike — and never revoking a catch already made. The
    /// later end wins against the bobble's fumble. 0 shipped: today's dive is free; the c80 copy carries 0.60.
    /// </summary>
    public double DiveRecoverySec { get; init; } = 0;
    /// <summary>The cost shortens by this fraction of itself per Field point above 1 (2.5 % in the c80 copy: 0.60 at Field 1 to 0.465 at 10).</summary>
    [Chance] public double DiveRecoveryFieldCut { get; init; } = 0;
    /// <summary>
    /// The normal jump's airtime (#719, F693-02-normal-jump-*): 0 is the jump the game shipped with — West arms a window of
    /// <c>jumpArmSec</c> and the body never leaves the ground; above 0 a fresh eligible West press is a takeoff with no added
    /// startup, the body is airborne this long with a root rise of <c>jumpRiseFt</c> (<c>h = 4 H u (1 − u)</c>), the same for
    /// every character, one profile per press, and a jumping catch throws only after it has landed. The c80 copy carries 0.60.
    /// </summary>
    public double JumpAirSec { get; init; } = 0;
    /// <summary>The root rise at the apex of the normal jump, in feet: 2.0 in both roots, read only above <c>jumpAirSec</c> 0.</summary>
    [Positive] public double JumpRiseFt { get; init; } = 2.0;
    /// <summary>A fresh grounded West press blocked by the read or a recovery is remembered this long, inclusive, and takes off at the first eligible instant (F693-02-normal-jump-input-buffer). 0 shipped: no buffer; the c80 copy carries 0.10.</summary>
    public double JumpBufferSec { get; init; } = 0;
    /// <summary>Airborne, the body answers the stick at this fraction of its ground rates (F693-02-normal-jump-air-response-trial): 0.10 in both roots, read only above <c>jumpAirSec</c> 0.</summary>
    [Chance] public double JumpAirResponseMul { get; init; } = 0.10;
    /// <summary>The physical normal jump is on above <c>jumpAirSec</c> 0; at 0 the jump is the arm window the game shipped with.</summary>
    public bool JumpArc => JumpAirSec > 0;
}

/// <summary>Field dash, buddy toss, kick, and the dive lunge (§8.1, §8.4, §8.7).</summary>
public sealed class FieldDashRules
{
    [Positive] public double ChaseMul { get; init; } = 1.35;
    public double BuddyTossFt { get; init; } = 28;
    public double KickFt { get; init; } = 22;
    public double DiveLungeFt { get; init; } = 10;
    public double ItemSmashFt { get; init; } = 24;
}

public sealed class DropRules
{
    [Chance] public double Heatball { get; init; } = 0.35;
    [Chance] public double PhonySwing { get; init; } = 0.35;
    [Chance] public double Frozen { get; init; } = 0.4;
}

public sealed class WallPlantRules
{
    public double InsideFenceFt { get; init; } = 8;
    [Chance] public double MinFraction { get; init; } = 0.45;
    public double FallbackInsideFt { get; init; } = 10;
    public double MinFt { get; init; } = 8;
}

public sealed class FieldAbilityRules
{
    public double BigCatchBonusFt { get; init; } = 6;
    /// <summary>Lick Catch / Grow reach further on the tag too (§10.3): added to running.bags.tagReachFt.</summary>
    public double TagReachBonusFt { get; init; } = 2;
    public double SuperJumpCatchBonusFt { get; init; } = 3;
    public double SuperJumpFlyRangeFt { get; init; } = 22;
    public double DiveGroundRangeFt { get; init; } = 16;
    [Positive] public double LaserMul { get; init; } = 1.45;
    [Positive] public double SnapThrowMul { get; init; } = 1.22;
    /// <summary>
    /// Snap Throw's release after a clean received teammate throw (F693-03-snap-throw, #723). On the shipped table it equals
    /// the ordinary release, so it is inert beside the ×1.22 flight; the c80 copy keeps 0.22 against an ordinary 0.30 and
    /// sets the flight boost to 1.0. A pickup, a bobble, a sail or a hand-off clears the eligibility.
    /// </summary>
    [Positive] public double SnapReleaseSec { get; init; } = 0.22;
    /// <summary>How far Laser is confined to a throw home with a live runner on third or the third–home segment (F693-03-laser-throw, #723): 0 is the universal boost the game shipped with, 1 the c80 rule — a cutoff feed never carries it.</summary>
    [Chance] public double LaserHomeOnly { get; init; } = 0;
    /// <summary>
    /// Ball Dash's carry (F693-02-ball-dash-carrier, #718): a holder with the ball securely in the glove moves at this multiple
    /// of its ordinary pursuit speed, automatically — no press, no timer, no cooldown — and the CPU's carry forecast reads the
    /// same number. Only the cap moves: the response rates stay the body's own (F693-02-carry-movement-response). No shipped
    /// body holds the ability, so the shipped table never reads it; the c80 roster gives it to dart, pip and jester.
    /// </summary>
    [Positive] public double BallDashMul { get; init; } = 1.20;
}

/// <summary>
/// The one throw model (§8.5, F693-03-long-throw-numbers):
/// <c>throwSec = releaseSec + [dist / (baseFtPerSec × arm) + longThrowLossSec × (max(0, dist − range) / 80)²] / (chem × ability)</c>
/// with <c>arm = armBase + Arm × armPerField</c> and <c>range = comfortableRangeFt + rangePerArmFt × (Arm − 5)</c>.
/// It flies the ball and judges the bag, for every arm on the field, the catcher's gun included, and it
/// is the arithmetic both CPU estimates read. With <c>longThrowLossSec</c> 0 it is the flat clock the game
/// shipped with, byte for byte; the c80 copy carries the accepted curve (#722). Lateral error
/// σ = (11 − Arm) × lateralSigmaPerFieldDeficitFt.
/// </summary>
public sealed class ThrowRules
{
    public double ReleaseSec { get; init; } = 0.22;
    [Positive] public double BaseFtPerSec { get; init; } = 100;
    [Positive] public double MinFtPerSec { get; init; } = 32;
    [Positive] public double ArmBase { get; init; } = 0.85;
    public double ArmPerField { get; init; } = 0.03;
    /// <summary>The neutral arm's comfortable range (F693-03-long-throws): inside it a throw is the flat clock; past it the flight loses pace, smoothly.</summary>
    [Positive] public double ComfortableRangeFt { get; init; } = 160;
    /// <summary>The range shifts this much per Arm point either side of the neutral arm (<see cref="InPlay.NeutralArm"/>).</summary>
    public double RangePerArmFt { get; init; } = 5;
    /// <summary>Seconds added to the flight per (feet past the range / 80)², before chemistry and ability divide it. 0 is the flat clock; the accepted trial value is 0.60.</summary>
    public double LongThrowLossSec { get; init; } = 0;
    public double LateralSigmaPerFieldDeficitFt { get; init; } = 0.35;
    /// <summary>A throw to an uncovered bag hangs as a lob this long for the cover; then it drops at the bag, live.</summary>
    [Positive] public double LobMaxSec { get; init; } = 1.5;
    /// <summary>
    /// The forced-relay ceiling (§8.7, #722): a throw longer than this always goes through the cutoff on the line, whatever
    /// the clock says, and the relay continues with the cutoff's arm. Inside it the CPU relays only when the relay arrives
    /// first by more than the rung's <c>cpu.*.relayBiasSec</c>. Shipped 200 beside a bias no relay can save is the rule the
    /// game shipped with; the c80 copy sets the ceiling to 9999 so time alone decides.
    /// </summary>
    [Positive] public double OnTheFlyFt { get; init; } = 200;
    /// <summary>A fielder holding the ball this close to a force bag steps on it instead of throwing (§10.4, S-41).</summary>
    [Positive] public double UnassistedFt { get; init; } = 8;
    public double HandHeightFt { get; init; } = 3.2;
    public double BagHeightFt { get; init; } = 1.2;
    /// <summary>A human's cutoff throws the armed onward leg for them (§8.7): 1 is the rule the game shipped with; 0 is F693-03-relay-ownership (#723), where each relay leg needs its own command.</summary>
    [Chance] public double RelayAutoContinue { get; init; } = 1;
    /// <summary>An early onward-throw press is remembered this long of active play and fires when the receiver has the ball (F693-03-input-buffer, #723). 0 is no queue; the c80 copy carries 0.25.</summary>
    public double RelayBufferSec { get; init; } = 0;
}

/// <summary>The CPU catcher's release on a steal (§11.3): <c>base − Field × perField ± noise / 2</c>, clamped, × the difficulty's reaction. The gun itself is the one throw model (<see cref="ThrowRules"/>); the out is the tag at the bag (§10.3).</summary>
public sealed class CatcherRules
{
    /// <summary>Where the catcher holds the ball at the crossing (§11.3): this far behind the plate, so the gun is a throw from the plate, not from the SET crouch spot.</summary>
    [Positive] public double BehindPlateFt { get; init; } = 3;
    public double CpuReleaseBaseSec { get; init; } = 0.42;
    public double CpuReleasePerField { get; init; } = 0.014;
    public double CpuReleaseNoiseSec { get; init; } = 0.20;
    public double CpuReleaseMinSec { get; init; } = 0.10;
    public double CpuReleaseMaxSec { get; init; } = 0.58;
}

/// <summary>
/// Chemistry on a throw (§8.5): good is faster; bad has a chance of a slanted throw — slower and
/// off the cover by a lateral miss the receiver cannot reach — and flies at <see cref="BadSpeedMul"/>
/// otherwise. The roll is on the input; the outcome is still the ball missing the cover. The shipped
/// table keeps the slant and a bad speed of 1.0; the c80 copy is F693-03-negative-chemistry — 0.90 and
/// no slant, slow rather than random (#722).
/// </summary>
public sealed class ThrowChemistryRules
{
    [Positive] public double GoodSpeedMul { get; init; } = 1.30;
    /// <summary>A bad pair's unslanted throw flies at this multiple of the arm's speed; 1.0 is today's ordinary throw.</summary>
    [Positive] public double BadSpeedMul { get; init; } = 1.0;
    [Chance] public double SlantChance { get; init; } = 0.20;
    [Positive] public double SlantSpeedMul { get; init; } = 0.70;
    [Positive] public double SlantLateralMinFt { get; init; } = 10;
    [Positive] public double SlantLateralMaxFt { get; init; } = 14;
}

public sealed class BobbleRules
{
    public double MinEnergy { get; init; } = 78;
    public double HandsPerGloveReduction { get; init; } = 3;
    [Positive] public double EnergySpan { get; init; } = 110;
    public double ChancePerHands { get; init; } = 0.08;
    [Chance] public double MaxChance { get; init; } = 0.5;
    public double FumbleSec { get; init; } = 0.58;
    public double ScatterFt { get; init; } = 6.5;
    public double ScatterFtPerSec { get; init; } = 12;
    public double ScatterDropFtPerSec { get; init; } = 16;
    public double ScatterBallY { get; init; } = 3.1;
    public double RecoilFtPerSec { get; init; } = 14;
}

/// <summary>
/// The shipped recoil: a stop read off the contact's energy and the Hands deficit, the whole tick held for it. Read only
/// while <see cref="RecoilRules.OnsetFtPerSec"/> is 0; above it the ordinary impact recoil (<see cref="RecoilRules"/>) replaces it.
/// </summary>
public sealed class KnockbackRules
{
    public double MinEnergy { get; init; } = 72;
    public double SecPerFieldDeficit { get; init; } = 0.045;
    [Positive] public double EnergySpan { get; init; } = 90;
    public double MaxSec { get; init; } = 0.55;
    public double MinSec { get; init; } = 0.02;
}

/// <summary>
/// What the ball costs the hands that take it (#720: F693-02-ground-pickup-recoil-basis, -ground-pickup-recoil-cap,
/// -recoil-field-shaping, -recoil-field-factors, -recoil-severity-curve, -ordinary-recoil-actions, -ordinary-recoil-displacement,
/// -ordinary-recoil-distance-cap, -ordinary-recoil-motion-profile). At <c>onsetFtPerSec</c> 0 the recoil is the one the game
/// shipped with (<see cref="KnockbackRules"/>). Above 0 it is read off the ball's actual incoming speed the frame before the
/// take, deterministic: severity <c>S = clamp((v − onset) / (full − onset), 0, 1)</c>, the weight <c>w = S × (1 −
/// handsCutPerPoint × (Hands − 1))</c>, the recovery <c>capSec × w</c>, an impact kick of <c>kickFtPerSec × w</c> along the
/// ball's horizontal travel slowing linearly to rest over the recovery (<c>w²</c> feet at the accepted 10 ft/s and 0.20 s).
/// A routine arrival at or below the onset costs nothing. The body's steering and throw start wait; possession and the contact
/// at the bag do not. The c80 copy carries the measured anchors; the shipped table carries 0 / 0.
/// </summary>
public sealed class RecoilRules
{
    public double OnsetFtPerSec { get; init; } = 0;
    public double FullFtPerSec { get; init; } = 0;
    /// <summary>
    /// The airborne pair (F693-02-grounded-air-catch-recoil, #720 slice 2): a hard batted ball caught in the air by a body on its
    /// feet — not diving, not jumping, not a buddy leap — costs the same response off these anchors. 0 = no airborne recoil (the
    /// shipped table); the c80 copy carries the measured 80 / 115, a distinct pair because the ground pair would charge every liner.
    /// </summary>
    public double AirOnsetFtPerSec { get; init; } = 0;
    public double AirFullFtPerSec { get; init; } = 0;
    [Positive] public double CapSec { get; init; } = 0.20;
    [Chance] public double HandsCutPerPoint { get; init; } = 0.05;
    public double KickFtPerSec { get; init; } = 10;
    /// <summary>The speed-read recoil is on; the shipped knockback is not read.</summary>
    public bool Active => OnsetFtPerSec > 0;
    /// <summary>A hard catch in the air by a grounded body recoils.</summary>
    public bool AirActive => AirOnsetFtPerSec > 0;
}

/// <summary>
/// Where an ordinary play is allowed to go wrong (#721: F693-02-handling-error-opportunities, -ordinary-handling-error-chance,
/// -ordinary-handling-error-cap, -ordinary-handling-chance-curve, -awkward-hop-difficulty-source, -ordinary-bobble-outcome,
/// -bobble-stun, -bobble-stun-duration, -bobble-recovery-reliability, -bobble-direction-spread, -uniform-error-direction,
/// -local-bobble-*). At <c>awkwardHop</c> 0 the bobble is the one the game shipped with (<see cref="BobbleRules"/>: a roll off
/// the contact's energy on every grounder take, a whole-tick fumble). Above 0 a legal routine pickup never rolls: the one
/// difficulty is the awkward in-between hop at the take — the ball rising off a real bounce, mid-way up a hop tall enough to
/// matter — and its chance is <c>chanceCap × D × (1 − handsCut × H)</c> with D the normalized difficulty and H the normalized
/// Hands. A failed take is a local bobble: the ball spills down from the contact with a fifth of its horizontal speed (six ft/s
/// at most) inside ±<c>bobbleSpreadDeg</c> of its travel, rebounds at <c>bobbleRestitution</c> under a <c>bobbleReboundCapFt</c>
/// ceiling, settles below <c>bobbleSettleFt</c>, keeps <c>bobbleGroundRetain</c> of its roll per impact and slows at
/// <c>bobbleDecelFtPerSec2</c>; the fumbler is stunned <c>stunSec</c> — no steering, no take — while the ball and every other
/// body stay live, and the recovery never rolls again. Or (F693-02-expanded-ordinary-error-outcomes, -error-outcome-selection,
/// -continuing-error-*) the ball gets past: how squarely the ring met the ball — the obstruction, 1 at the body, 0 at the edge of
/// the take — and the ball's speed decide, never a second roll. Below <c>deflectObstruction</c> and at or above
/// <c>deflectMinFtPerSec</c> coming in, the ball carries on with <c>deflectRetainMax</c> → <c>deflectRetainMin</c> of its horizontal
/// and signed vertical speed inside ±<c>deflectSpreadDeg</c> of its travel on the shared batted-ball ground physics; the same stun,
/// the same reliable recovery, another body's if it reaches it first.
/// </summary>
public sealed class HandlingRules
{
    [Chance] public double AwkwardHop { get; init; } = 0;
    [Chance] public double ChanceCap { get; init; } = 0.10;
    [Chance] public double HandsCut { get; init; } = 0.80;
    /// <summary>A hop whose apex is below this is a micro-bounce and never awkward; the difficulty grows to full at <see cref="HopFullApexFt"/>.</summary>
    public double HopMinApexFt { get; init; } = 0.5;
    public double HopFullApexFt { get; init; } = 1.5;
    /// <summary>The phase band: D is 1 at the middle of the rise (φ = 0.5) and falls linearly to 0 this far either side — the clean short hop below, the clean long hop above.</summary>
    [Positive] public double HopPhaseHalfWidth { get; init; } = 0.35;
    [Positive] public double StunSec { get; init; } = 0.40;
    public double BobbleSpreadDeg { get; init; } = 30;
    [Chance] public double BobbleRetain { get; init; } = 0.20;
    public double BobbleCapFtPerSec { get; init; } = 6;
    [Chance] public double BobbleRestitution { get; init; } = 0.35;
    public double BobbleReboundCapFt { get; init; } = 0.50;
    public double BobbleSettleFt { get; init; } = 0.25;
    [Chance] public double BobbleGroundRetain { get; init; } = 0.90;
    public double BobbleDecelFtPerSec2 { get; init; } = 6;
    public double DeflectSpreadDeg { get; init; } = 15;
    [Chance] public double DeflectRetainMax { get; init; } = 0.80;
    [Chance] public double DeflectRetainMin { get; init; } = 0.50;
    /// <summary>The obstruction at and above which a failed take is a knockdown (the local bobble); below it the ball glances on.</summary>
    [Chance] public double DeflectObstruction { get; init; } = 0.5;
    /// <summary>A glancing touch sends the ball on only when it came in at least this fast — the hot ball of the recoil's onset; a slower one drops at the feet.</summary>
    public double DeflectMinFtPerSec { get; init; } = 55;
    /// <summary>The routine pickup never rolls; the awkward hop is the one difficulty.</summary>
    public bool Active => AwkwardHop > 0;
}

public sealed class ParkHazardRules
{
    /// <summary>
    /// The one number here that is not a park hazard's: a shell or cask star swing flags a grounder
    /// warped with no can in the park at all (<c>Fielding.cs</c>). The hazard types' own numbers
    /// moved to <c>hazards.json</c> with the pattern library (#847).
    /// </summary>
    [Chance] public double ShellWarpChance { get; init; } = 0.6;
}

// ---------------------------------------------------------------------------------------
// hazards.json — the closed hazard type library (§14, §16; FD-09, FR-02, FR-08)
// ---------------------------------------------------------------------------------------

/// <summary>
/// One authored row per hazard type (#847). Each row names the <see cref="HazardPattern"/> the sim
/// runs for that type and carries the numbers that are the type's own. <see cref="ParkHazards"/>
/// dispatches on the row's pattern, never on a type string, so a type that fits a pattern is a row
/// here and nothing in the sim (FR-08).
///
/// <para>
/// The shape is the pitch-family library's (D20, <see cref="PitchFamilyTable"/>): named properties
/// rather than a dictionary, so the reflective validator and the JSON = code parity test both reach
/// every row; <see cref="Of"/> gives an id in the library with no row and an id outside the library
/// different messages, and neither one flies.
/// </para>
///
/// <para>
/// <b>Every number here is the number that shipped.</b> #847 moved <c>fielding.park.emberNightFireMul</c>
/// (1.6) under <c>fireBreath</c> and <c>fielding.park.pipeReachPadFt</c> (8; 5.6 on <c>trials/c80</c>)
/// under <c>warpPipe</c> and <c>barrel</c>; it chose neither, and #730 / #732 still own them. What a
/// status volume costs a body stays <c>fielding.chase.frozenMul</c> — a star swing sets the same slow
/// and the specials are outside this phase — and the billboard's payout stays <c>stars.gains.billboard</c>.
/// </para>
/// </summary>
public sealed class HazardRules
{
    /// <summary>Crystal's freezers: a landing in the disc slows the chase.</summary>
    public HazardTypeRules FreezeVolume { get; init; } = new() { Pattern = HazardPattern.StatusVolume };

    /// <summary>Ember's lava, the freezer's twin.</summary>
    public HazardTypeRules LavaPit { get; init; } = new() { Pattern = HazardPattern.StatusVolume };

    /// <summary>Ember's breath: the one volume night widens.</summary>
    public HazardTypeRules FireBreath { get; init; } = new()
    {
        Pattern = HazardPattern.StatusVolume,
        NightRadiusMul = 1.6
    };

    /// <summary>Funfair's warp cans: a grounder that enters one comes out of another.</summary>
    public HazardTypeRules WarpPipe { get; init; } = new()
    {
        Pattern = HazardPattern.BallRedirect,
        ReachPadFt = 8
    };

    /// <summary>Canopy's barrel cannons, the warp can's twin.</summary>
    public HazardTypeRules Barrel { get; init; } = new()
    {
        Pattern = HazardPattern.BallRedirect,
        ReachPadFt = 8
    };

    /// <summary>Rooftop's star signs: a landing on one pays the batting team.</summary>
    public HazardTypeRules Billboard { get; init; } = new() { Pattern = HazardPattern.RewardTarget };

    /// <summary>Canopy's climbable wall: a Clamber fielder's reach and rob.</summary>
    public HazardTypeRules ClimbWall { get; init; } = new() { Pattern = HazardPattern.WallTrait };

    /// <summary>Funfair's mouths: at night a fly that lands in one is an out with no glove.</summary>
    public HazardTypeRules Chomper { get; init; } = new()
    {
        Pattern = HazardPattern.CatchStealer,
        NightOnly = true
    };

    /// <summary>Ember's captain statue. Drawn, never played.</summary>
    public HazardTypeRules Statue { get; init; } = new() { Pattern = HazardPattern.Decoration };

    /// <summary>Funfair's boxcar. Drawn, never played; the timed mover is F4-f's.</summary>
    public HazardTypeRules Train { get; init; } = new() { Pattern = HazardPattern.Decoration };

    /// <summary>Rooftop's air-conditioning units. Drawn, never played; the solid body is F4-g's.</summary>
    public HazardTypeRules AcUnit { get; init; } = new() { Pattern = HazardPattern.Decoration };

    /// <summary>Canopy's trees. Drawn, never played; the solid body is F4-g's.</summary>
    public HazardTypeRules Tree { get; init; } = new() { Pattern = HazardPattern.Decoration };

    /// <summary>
    /// This table's row for a library id, or null when the data does not author one. Not public:
    /// callers ask <see cref="IsAuthored"/> or take <see cref="Of"/>'s named stop, so an unauthored
    /// type can never be read as a missing-but-harmless nothing (FR-02).
    /// </summary>
    HazardTypeRules? Named(string? type) => type switch
    {
        HazardType.FreezeVolume => FreezeVolume,
        HazardType.LavaPit => LavaPit,
        HazardType.FireBreath => FireBreath,
        HazardType.WarpPipe => WarpPipe,
        HazardType.Barrel => Barrel,
        HazardType.Billboard => Billboard,
        HazardType.ClimbWall => ClimbWall,
        HazardType.Chomper => Chomper,
        HazardType.Statue => Statue,
        HazardType.Train => Train,
        HazardType.AcUnit => AcUnit,
        HazardType.Tree => Tree,
        _ => null
    };

    /// <summary>True for a library id <b>this table</b> has a row for.</summary>
    public bool IsAuthored(string? type) => Named(type) is not null;

    IReadOnlyList<string>? _authored;

    /// <summary>
    /// The authored ids in library order (<see cref="HazardType.All"/>). Built once per table and
    /// then handed out: the park validator asks for it on every load. A table is never mutated after
    /// it loads, so the answer cannot go stale.
    /// </summary>
    public IReadOnlyList<string> Authored => _authored ??= HazardType.All.Where(IsAuthored).ToList();

    /// <summary>
    /// The row for a hazard type. A library id with no row and an id that is not in the library are
    /// different mistakes and get different messages; neither one flies (SF-03).
    /// </summary>
    public HazardTypeRules Of(string? type)
    {
        if (Named(type) is { } row) return row;
        if (HazardType.IsKnown(type))
            throw new InvalidOperationException(
                $"hazard type '{type}' is in the library but has no authored row in hazards.json; "
                + "every type a park may name carries a row (FD-09, FR-02). Authored: " + string.Join(", ", Authored));
        throw new ArgumentException(
            $"'{type}' is not a hazard type. The library is [{string.Join(", ", HazardType.All)}] (HazardType).",
            nameof(type));
    }

    /// <summary>
    /// The rules across a row's fields the attributes cannot say, checked on every library id:
    /// <list type="bullet">
    /// <item>Every type in the library has a row. A type a park may name with nothing behind it would
    /// load and then throw at the first ball that reached it (FR-02).</item>
    /// <item>The pattern is one the sim implements. An unknown pattern is a type that silently does
    /// nothing, which is exactly what <see cref="HazardPattern.Decoration"/> exists to say out loud.</item>
    /// <item>A number that its pattern never reads is a dead rule, the way a sweep with no start is
    /// (#818): only a <c>statusVolume</c> widens at night, and only a <c>ballRedirect</c> has a reach pad.</item>
    /// </list>
    /// </summary>
    internal void Validate(string source, List<string> errors)
    {
        foreach (var type in HazardType.All)
        {
            var key = "hazards." + HazardType.Key(type);
            if (Named(type) is not { } row)
            {
                errors.Add($"{source}: {key} is in the library but has no authored row; "
                           + $"every hazard type carries a row (FD-09). Authored: {string.Join(", ", Authored)}");
                continue;
            }
            if (!HazardPattern.IsKnown(row.Pattern))
            {
                errors.Add($"{source}: {key}.pattern must be one of [{string.Join(", ", HazardPattern.All)}]; "
                           + $"got '{row.Pattern}'");
                continue;
            }
            if (row.NightRadiusMul != 1 && row.Pattern != HazardPattern.StatusVolume)
                errors.Add($"{source}: {key}.nightRadiusMul is {row.NightRadiusMul}, but only a "
                           + $"{HazardPattern.StatusVolume} widens at night; give the row that pattern or set it to 1");
            if (row.ReachPadFt != 0 && row.Pattern != HazardPattern.BallRedirect)
                errors.Add($"{source}: {key}.reachPadFt is {row.ReachPadFt}, but only a "
                           + $"{HazardPattern.BallRedirect} has a reach pad; give the row that pattern or set it to 0");
        }
    }
}

/// <summary>
/// One hazard type's row (§14, §16). The defaults are an inert drawn prop — the honest answer for a
/// type whose pattern nobody has built — so a row that omits a field reads as scenery rather than as
/// a broken volume.
/// </summary>
public sealed class HazardTypeRules
{
    /// <summary>Which of <see cref="HazardPattern"/> the sim runs for this type.</summary>
    public string Pattern { get; init; } = HazardPattern.Decoration;

    /// <summary>
    /// A <c>statusVolume</c>'s disc at night, as a multiple of the radius the park authored. 1 is a
    /// disc night does not widen, which is every volume but Ember's breath.
    /// </summary>
    [Positive] public double NightRadiusMul { get; init; } = 1;

    /// <summary>
    /// How far outside its own radius a <c>ballRedirect</c> still takes a grounder. The pad is the
    /// larger part of the capture disc (#732), not a rounding allowance.
    /// </summary>
    public double ReachPadFt { get; init; }

    /// <summary>True for a type that acts only at night. A day game plays as if it were not there.</summary>
    public bool NightOnly { get; init; }
}

// ---------------------------------------------------------------------------------------
// running.json — §9, §10.2, §10.3, §10.6, §11.2, §11.6
// ---------------------------------------------------------------------------------------

public sealed class RunningRules
{
    /// <summary>The one speed formula for every runner and every segment (spec §9.1).</summary>
    public BagSecRules BagSec { get; init; } = new();
    public BagRules Bags { get; init; } = new();
    public ClosePlayRules Close { get; init; } = new();
    public StealRules Steal { get; init; } = new();
    public RunStickRules Stick { get; init; } = new();
    public DashRules Dash { get; init; } = new();
    public RundownRules Rundown { get; init; } = new();
    public CpuRunnerRules Cpu { get; init; } = new();

    internal void Validate(string source, List<string> errors)
    {
        RulesValidation.Order(source, "running.bagSec.minSec", BagSec.MinSec, BagSec.MaxSec, errors);
        RulesValidation.Order(source, "running.bags.tagSafeRadiusFt", Bags.TagSafeRadiusFt, Bags.OccupyRadiusFt, errors);
        RulesValidation.Order(source, "running.bags.slideReachCutFt", Bags.SlideReachCutFt, Bags.TagReachFt, errors);
        RulesValidation.Order(source, "running.cpu.stealBaseRun6", Cpu.StealBaseRun6, Cpu.StealBaseRun8, errors);
        RulesValidation.Order(source, "running.cpu.stealBaseRun8", Cpu.StealBaseRun8, Cpu.StealBaseRun10, errors);
        // A slide narrows the tag window but never closes it: the safe radius stays inside the slid reach (§10.3).
        RulesValidation.Order(source, "running.bags.tagSafeRadiusFt", Bags.TagSafeRadiusFt, Bags.TagReachFt - Bags.SlideReachCutFt, errors);
    }
}

/// <summary>
/// Seconds per 90 ft bag-to-bag: <c>baseSec − Run × secPerRun</c>, clamped (spec §9.1). Every runner,
/// including the batter-runner, runs every segment on it; the batter starts <see cref="BatterStartSec"/>
/// after contact from the box. Dash ×(1 + dashMul) at full mash (§9.4).
/// </summary>
public sealed class BagSecRules
{
    [Positive] public double BaseSec { get; init; } = 3.55;
    public double SecPerRun { get; init; } = 0.12;
    [Positive] public double MinSec { get; init; } = 2.45;
    [Positive] public double MaxSec { get; init; } = 3.65;
    [Chance] public double DashMul { get; init; } = 0.12;
    /// <summary>The batter-runner leaves the box this long after contact (reference: 31 frames).</summary>
    public double BatterStartSec { get; init; } = 0.5;
    /// <summary>A trailing runner stops this far behind the runner ahead; runners never pass (§9.1).</summary>
    [Positive] public double NoPassFt { get; init; } = 27;
}

public sealed class BagRules
{
    [Positive] public double OccupyRadiusFt { get; init; } = 6;
    /// <summary>A glove with the ball inside this of a runner's body is the tag (§10.3); Lick / Grow add fielding.abilities.tagReachBonusFt.</summary>
    [Positive] public double TagReachFt { get; init; } = 4;
    /// <summary>A runner inside this of a bag is touching it: safe from the tag. Inside the slid reach so a slide never closes the window.</summary>
    [Positive] public double TagSafeRadiusFt { get; init; } = 1.5;
    [Positive] public double TimeOnBagSec { get; init; } = 1.0;
    /// <summary>Inside this of the end of a segment the runner is placed on the bag.</summary>
    public double SnapFt { get; init; } = 0.5;
    /// <summary>A runner stopping at the next bag slides over its last feet when a tag is threatened (§9.4).</summary>
    [Positive] public double SlideFt { get; init; } = 12;
    /// <summary>A tag is threatened when a glove with the ball is inside this of the bag, or a throw is armed there.</summary>
    [Positive] public double SlideThreatFt { get; init; } = 20;
    /// <summary>A slide shrinks the tag reach by this much; it does not change the arrival (§9.4).</summary>
    public double SlideReachCutFt { get; init; } = 2;
    /// <summary>The batter-runner runs through first by this much and comes straight back, safe from the tag on the way (§9.4).</summary>
    [Positive] public double OverrunFt { get; init; } = 12;
}

public sealed class ClosePlayRules
{
    /// <summary>
    /// The close play is geometric (§9.6, D5): the ball at a tag bag ahead of the runner by no more than
    /// this runs the mash; the runner in ahead of the ball by no more than this pops the small SAFE.
    /// Outside it the geometry decides silently.
    /// </summary>
    [Positive] public double MarginSec { get; init; } = 0.25;
    public double IconDelaySec { get; init; } = 0.22;
    public double CpuReactionBaseSec { get; init; } = 0.20;
    public double CpuReactionPerStat { get; init; } = 0.032;
}

/// <summary>
/// The steal jump (§11.2, D2): an armed runner breaks at release and runs at <see cref="AirSpeedMul"/> of
/// their speed while the pitch is in the air; armed inside the first <see cref="PerfectWindowSec"/> of the
/// windup is a perfect steal that breaks <see cref="PerfectEarlySec"/> before release. There is no time
/// credit: the body is placed by the clock and the tag decides (§10.3).
/// </summary>
public sealed class StealRules
{
    /// <summary>Speed fraction of an armed runner between the break and the ball reaching the plate.</summary>
    [Chance] public double AirSpeedMul { get; init; } = 0.6667;
    /// <summary>A perfect steal breaks this long before release.</summary>
    [Positive] public double PerfectEarlySec { get; init; } = 0.4;
    /// <summary>Arming inside this many seconds of the windup is the perfect steal.</summary>
    [Positive] public double PerfectWindowSec { get; init; } = 0.25;
    /// <summary>The CPU catcher throws on the trailing runner of a double steal only when that margin is better by this (§11.3).</summary>
    [Positive] public double CpuTrailPreferSec { get; init; } = 0.3;
    /// <summary>With a runner on third watching a steal of second, the free middle infielder cuts this far in front of the bag on the throw line (S-66).</summary>
    [Positive] public double CutInFrontFt { get; init; } = 25;
}

/// <summary>
/// The rundown (§9.7): a runner off the bags with a glove holding the ball inside <see cref="RangeFt"/>.
/// The CPU glove runs at them and throws ahead at the last makeable moment (the §8.8 margin, no fixed
/// distance); a throw that races nobody to its bag, with every moving body at least
/// <see cref="LazyLobFraction"/> of the way to a bag, is a lazy lob at <see cref="LazyLobSpeedMul"/> of the arm.
/// </summary>
public sealed class RundownRules
{
    [Positive] public double RangeFt { get; init; } = 20;
    [Chance] public double LazyLobFraction { get; init; } = 0.8;
    [Positive] public double LazyLobSpeedMul { get; init; } = 0.5;
}

/// <summary>Mash South to dash (§9.4): each press adds this, to the cap.</summary>
public sealed class DashRules
{
    [Chance] public double PerPress { get; init; } = 0.28;
    [Chance] public double MaxDash { get; init; } = 1.0;
}

public sealed class RunStickRules
{
    /// <summary>Squared stick magnitude below which the diamond stick names no bag.</summary>
    [Chance] public double DiamondDeadMag2 { get; init; } = 0.55;
}

/// <summary>
/// The CPU baserunner (spec §9.9): decisions at contact, at every fielder touch, at every throw
/// release and at every bag, from <c>margin(next) = throwArrival(next) − runnerArrival(next)</c>.
/// The steal roll is still the SET roll as shipped; §11.6 moves it to the runner AI (P6).
/// </summary>
public sealed class CpuRunnerRules
{
    /// <summary>The defense's reaction before a throw in the runner's estimate.</summary>
    public double ReactionSec { get; init; } = 0.35;
    /// <summary>Unforced on a grounder to the infield: go if margin(next) is at least this and the ball is not in front.</summary>
    public double GroundGoMarginSec { get; init; } = 0.4;
    /// <summary>Runner on third, grounder, fewer than two outs: go if the fielder is this far from home (infield back) …</summary>
    public double InfieldBackFt { get; init; } = 110;
    /// <summary>… or margin(home) is at least this.</summary>
    public double ThirdHomeMarginSec { get; init; } = 0.3;
    /// <summary>Hit to the outfield: go if margin(next) is at least outfieldGoSec − Run × outfieldGoPerRunSec.</summary>
    public double OutfieldGoSec { get; init; } = 0.5;
    public double OutfieldGoPerRunSec { get; init; } = 0.03;
    /// <summary>With two outs the outfield threshold drops by this (go more).</summary>
    public double TwoOutsGoBonusSec { get; init; } = 0.3;
    /// <summary>The batter-runner rounds first for second if margin(2B) is at least roundFirstSec − Run × roundFirstPerRunSec.</summary>
    public double RoundFirstSec { get; init; } = 0.6;
    public double RoundFirstPerRunSec { get; init; } = 0.03;
    /// <summary>Trailing by at least this in the last inning: every threshold drops by desperateSec.</summary>
    public int DesperateTrailRuns { get; init; } = 3;
    public double DesperateSec { get; init; } = 0.2;
    /// <summary>A runner already this far along a segment keeps going rather than turning back.</summary>
    [Chance] public double CommitFraction { get; init; } = 0.4;
    /// <summary>Runner on third tags on a caught fly this deep with fewer than two outs. The shipped rule; the c80 copy sets it to never and races instead (#732).</summary>
    public double TagThirdMinCarryFt { get; init; } = 200;
    /// <summary>Runner on second tags for third on a caught fly to right this deep. The shipped rule; the c80 copy sets it to never and races instead (#732).</summary>
    public double TagSecondMinCarryFt { get; init; } = 250;
    /// <summary>
    /// The race from third (#732, decision 5 of #730): at the catch, the runner goes home when <c>margin(home)</c> — the
    /// defense's estimated arrival minus the runner's — clears this plus the rung's <c>runnerMarginSec</c>. 99 is a margin
    /// no play reaches, so the shipped table decides by the carry gate alone; the c80 copy authors the number.
    /// </summary>
    [Signed] public double TagUpHomeMarginSec { get; init; } = 99;
    /// <summary>The race from second to third at the catch, on the same terms as <see cref="TagUpHomeMarginSec"/>.</summary>
    [Signed] public double TagUpThirdMarginSec { get; init; } = 99;
    /// <summary>The CPU steal table (§11.6): base chance by Run, 0 at or below <see cref="StealMinRun"/>, linear between the anchors.</summary>
    public int StealMinRun { get; init; } = 4;
    [Chance] public double StealBaseRun6 { get; init; } = 0.06;
    [Chance] public double StealBaseRun8 { get; init; } = 0.16;
    [Chance] public double StealBaseRun10 { get; init; } = 0.25;
    /// <summary>× with two outs.</summary>
    [Positive] public double StealTwoOutsMul { get; init; } = 1.5;
    /// <summary>× with the captain slugger at the plate.</summary>
    [Positive] public double StealCaptainUpMul { get; init; } = 0.5;
    /// <summary>No steal when trailing by at least this many runs.</summary>
    public int StealTrailingRuns { get; init; } = 5;
    /// <summary>The CPU pitcher's pickoff chance (cpu.*.pickoffChance) is multiplied by this when it sees a steal pip armed in SET (§4.5, D3).</summary>
    [Positive] public double PickoffSeenArmMul { get; init; } = 3;
    /// <summary>On first-and-third the CPU pickoff goes to first this often, else the lead runner (§4.8).</summary>
    [Chance] public double PickoffFirstOnCornersChance { get; init; } = 0.66;
}

// ---------------------------------------------------------------------------------------
// stars.json — §12, §13
// ---------------------------------------------------------------------------------------

public sealed class StarRules
{
    [Positive] public double MeterMax { get; init; } = 5;
    public StarGainRules Gains { get; init; } = new();
    public StarCostRules Costs { get; init; } = new();
    public StartingStarRules Starting { get; init; } = new();
    public MvpRules Mvp { get; init; } = new();
}

public sealed class StarGainRules
{
    public double Strikeout { get; init; } = 0.8;
    public double HomeRun { get; init; } = 1.0;
    public double ExtraBaseHit { get; init; } = 0.8;
    public double Single { get; init; } = 0.4;
    /// <summary>Fly or ground out resolved by the resolver.</summary>
    public double Out { get; init; } = 0.35;
    /// <summary>An out made live: a force, a tag, a caught runner.</summary>
    public double LiveOut { get; init; } = 0.4;
    public double StolenBase { get; init; } = 0.35;
    public double Billboard { get; init; } = 1.0;
    /// <summary>Two or more outs on one live ball (§10.4, §12).</summary>
    public double DoublePlay { get; init; } = 1.0;
    /// <summary>A leap that takes a ball clearing the fence (§8.4, §12).</summary>
    public double RobbedHomer { get; init; } = 1.0;
}

/// <summary>
/// The MVP point table (§12, <c>stars.json</c> <c>mvp</c>). A walk-off hit names its hitter ahead of the
/// points; otherwise the most points on either roster. Read by <see cref="Match.Mvp"/>, never literals.
/// </summary>
public sealed class MvpRules
{
    public int HomeRun { get; init; } = 10;
    /// <summary>The arm on the mound for the winner when it took the lead for the last time.</summary>
    public int WinningPitcher { get; init; } = 5;
    /// <summary>The run batted in that put the offense ahead, on top of the RBI.</summary>
    public int GoAheadRbi { get; init; } = 5;
    /// <summary>A robbed homer, a buddy jump, or a climb at the wall.</summary>
    public int RobbedHomer { get; init; } = 5;
    public int Strikeout { get; init; } = 3;
    public int Rbi { get; init; } = 3;
    public int Hit { get; init; } = 1;
    public int Walk { get; init; } = 1;
    public int HitByPitch { get; init; } = 1;
    public int StolenBase { get; init; } = 1;
    /// <summary>The seat that won a close play at the bag (§9.6): the runner safe, or the glove that tagged.</summary>
    public int ClosePlayWon { get; init; } = 2;
    /// <summary>A thrown item landed and the batter reached (§12).</summary>
    public int ItemMattered { get; init; } = 2;
    /// <summary>A putout made live: the glove that forced, tagged, or caught the runner (also the catcher on a caught stealing).</summary>
    public int PutOut { get; init; } = 1;
    /// <summary>The MVP line reads "took over the diamond" at or above this many points.</summary>
    public int TookOverAt { get; init; } = 8;
    /// <summary>"kept the line moving" at or above this; under it "did the little things".</summary>
    public int KeptMovingAt { get; init; } = 4;
}

public sealed class StarCostRules
{
    public int Own { get; init; } = 1;
    public int GuestCaptain { get; init; } = 2;
}

/// <summary>Roster chemistry with the captain → starting meter (<see cref="ChemistryTable.StartingStars(Team)"/>).</summary>
public sealed class StartingStarRules
{
    public double GoodScore { get; init; } = 100;
    public double NeutralScore { get; init; } = 50;
    public double BadScore { get; init; } = 10;
    public double FiveAt { get; init; } = 70;
    public double FourAt { get; init; } = 55;
    public double ThreeAt { get; init; } = 35;
    public double TwoAt { get; init; } = 15;
}

// ---------------------------------------------------------------------------------------
// cpu.json — difficulty multipliers over the tables above (§4.8, §5.9, §8.8, §11.6)
// ---------------------------------------------------------------------------------------

public sealed class CpuRules
{
    /// <summary>The ladder rung in play. Normal is ×1 everywhere so the tables above read as written.</summary>
    public string Level { get; init; } = "normal";
    public CpuLevelRules Easy { get; init; } = new()
    {
        HumanWindowMul = 1.3, TimingSigmaMul = 1.3, ReactionMul = 1.4, MistrackMul = 1.3, MakeableMarginSec = 0.30, PerfectStealChance = 0, PickoffChance = 0.03, RunnerMarginSec = 0.15
    };
    public CpuLevelRules Normal { get; init; } = new();
    public CpuLevelRules Hard { get; init; } = new()
    {
        HumanWindowMul = 0.9, TimingSigmaMul = 0.8, ReactionMul = 0.8, MistrackMul = 0.8, MakeableMarginSec = 0.05, PerfectStealChance = 0.4, PickoffChance = 0.10, RunnerMarginSec = -0.15
    };

    public CpuLevelRules Active => Level.ToLowerInvariant() switch
    {
        "easy" => Easy,
        "hard" => Hard,
        _ => Normal
    };

    /// <summary>The ladder, in order: the title cycles through it next to the innings.</summary>
    public static readonly IReadOnlyList<string> Levels = ["easy", "normal", "hard"];

    public static bool IsLevel(string? level) => level is not null && Levels.Contains(level.ToLowerInvariant());

    public static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    /// <summary>The rung after <paramref name="level"/>, wrapping (easy → normal → hard → easy).</summary>
    public static string Next(string level)
    {
        var i = Levels.ToList().FindIndex(l => Same(l, level));
        return Levels[(Math.Max(0, i) + 1) % Levels.Count];
    }

    /// <summary>The same three rungs with another one active (a match's difficulty, §16).</summary>
    public CpuRules AtLevel(string level) =>
        new() { Level = IsLevel(level) ? level.ToLowerInvariant() : Level, Easy = Easy, Normal = Normal, Hard = Hard };

    internal void Validate(string source, List<string> errors)
    {
        if (Level.ToLowerInvariant() is not ("easy" or "normal" or "hard"))
            errors.Add($"{source}: cpu.level must be one of [easy, hard, normal]; got '{Level}'");
    }
}

public sealed class CpuLevelRules
{
    /// <summary>
    /// Multiplies a human batter's timing window before the floor (spec §5.3, #612): the ladder
    /// is also a swing-timing ladder for the player. The CPU batter's window is never scaled.
    /// </summary>
    [Positive] public double HumanWindowMul { get; init; } = 1.0;
    /// <summary>Multiplies the CPU batter's timing-error σ (§5.9).</summary>
    [Positive] public double TimingSigmaMul { get; init; } = 1.0;
    /// <summary>Multiplies CPU reaction and release delays (§8.8, §9.6, §11.3).</summary>
    [Positive] public double ReactionMul { get; init; } = 1.0;
    /// <summary>Multiplies the CPU batter's mistrack chances (§5.9).</summary>
    [Positive] public double MistrackMul { get; init; } = 1.0;
    /// <summary>Margin a CPU fielder needs to call a play makeable (§8.8). Read by P4.</summary>
    public double MakeableMarginSec { get; init; } = 0.15;
    /// <summary>Perfect-steal chance (§11.6): the CPU runner arms inside the window instead of in SET.</summary>
    [Chance] public double PerfectStealChance { get; init; } = 0.2;
    /// <summary>Pickoff attempt chance per SET with a runner on (§4.5, §4.8); × running.cpu.pickoffSeenArmMul when a pip is armed.</summary>
    [Chance] public double PickoffChance { get; init; } = 0.06;
    /// <summary>Added to every CPU baserunner threshold (§9.9): easy hesitates, hard goes.</summary>
    [Signed] public double RunnerMarginSec { get; init; } = 0;
    /// <summary>
    /// The CPU fielder throws through the cutoff only when the relay beats the direct throw by more than this (§8.7, #722) —
    /// the total-time read, graded by rung. 99, a value no relay can save, leaves <see cref="ThrowRules.OnTheFlyFt"/> as the
    /// only reason to relay, which is the rule the game shipped with.
    /// </summary>
    public double RelayBiasSec { get; init; } = 99;
    /// <summary>How much of the thrower's real arm and ability the CPU runner reads (§9.9): 0 assumes the neutral arm, as the shipped runner does; 1 reads the arm the ball will fly on.</summary>
    [Chance] public double RunnerReadsArm { get; init; } = 0;
    /// <summary>How much of the fielder's relay the CPU runner reads: 0 assumes a direct throw; 1 reads the leg the fielder will actually take.</summary>
    [Chance] public double RunnerReadsRelay { get; init; } = 0;
    /// <summary>How much of the pair chemistry the CPU forecasts, fielder and runner alike (F693-03-good-chemistry): 0 ignores it, 1 is the deterministic pair factor, never a sampled roll.</summary>
    [Chance] public double ReadsChemistry { get; init; } = 0;
}
