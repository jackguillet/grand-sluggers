using System.Reflection;

namespace GrandSluggers.Sim;

/// <summary>
/// The rule numbers of play (docs/gameplay-spec.md §16). One table per file under
/// <c>data/rules/</c>. The JSON is the only source of a rule number: the tables hold no C#
/// defaults, the loader refuses a file with a missing field, an unknown field or a value outside
/// its declared range, and a changed table is derived from a loaded one with <c>with</c>.
/// Presentation numbers stay in <c>data/feel/</c>.
/// </summary>
public sealed record RulesTable
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

    /// <summary>
    /// The same tables played at another difficulty rung (§16 <c>cpu.json</c>): every section is shared,
    /// only <see cref="Cpu"/>'s active rung changes. The table itself is returned when the rung is already this one.
    /// </summary>
    public RulesTable AtLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level) || CpuRules.Same(level, Cpu.Level)) return this;
        return this with { Cpu = Cpu.AtLevel(level) };
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
        // The ground and wall libraries are global: a park chooses which row each of its zones
        // names (its `zones` block, read through GroundZones), never what a row says (FD-05).
        return this with { Flight = Flight.WithEnvironment(env) };
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

    /// <summary>Load every table, collecting errors instead of throwing. A missing field is an error: there is no code fallback.</summary>
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
        // A missing field reads as zero. The range and cross-field checks of that file would judge a
        // number the data never wrote, so they wait until the file is whole.
        var incomplete = errors.Select(RulesValidation.MissingSource).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var checks = new List<string>();
        RulesValidation.Validate(table, dataRoot, checks);
        errors.AddRange(checks.Where(e => !incomplete.Any(f => e.StartsWith(f + ": ", StringComparison.Ordinal))));
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
            {
                RulesValidation.UnknownFields(doc.RootElement, typeof(T), name, path, errors);
                RulesValidation.MissingFields(doc.RootElement, typeof(T), name, path, errors);
            }
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
/// The process-wide table: the data root found from the binary or named by
/// <see cref="ContentCatalog.DataRootVariable"/>. Only entry points read it — the CLI, the Unity
/// bootstrap, tools and tests — and hand it on. The sim takes the table it plays as an argument
/// everywhere; nothing in the play loop reaches for this one (<c>RulesPlumbingTests</c>). There is
/// no code table to fall back to, so a process that finds no readable root stops.
/// </summary>
public static class Rules
{
    static RulesTable? _default;

    public static RulesTable Default => _default ??= LoadDefault();

    static RulesTable LoadDefault()
    {
        var root = ContentCatalog.TryFindDataRoot()
            ?? throw new DirectoryNotFoundException(
                "No data root for the rules tables: none above " + AppContext.BaseDirectory
                + " and " + ContentCatalog.DataRootVariable + " is unset");
        return ForProcess(root);
    }

    /// <summary>
    /// The process-wide table for a root. Tables it cannot read stop the run, named root or not:
    /// there is no code table to play instead, and <see cref="Diamond"/> reads this table, so a
    /// silent substitute would put the bags somewhere the data never said.
    /// </summary>
    public static RulesTable ForProcess(DataRoot root)
    {
        var errors = new List<string>();
        var table = RulesTable.Load(root, errors);
        if (errors.Count == 0) return table;
        throw new InvalidDataException(
            "Invalid rules tables — " + root.Provenance + Environment.NewLine
            + string.Join(Environment.NewLine, errors.Select(e => "  - " + e)));
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
///
/// <para>
/// The same holds for a number a row may leave out (a <c>double?</c> leaf, F4-b: a
/// <c>statusVolume</c> row's <c>slowSec</c>, which no other pattern reads). Absent is null;
/// present, it is range-checked like every other leaf, so <see cref="PositiveAttribute"/> still
/// holds; and a JSON <c>null</c> for it is refused rather than read as absent.
/// </para>
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
        table.Stars.Validate(RulesTable.PathFor(root, "stars"), errors);
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
            var leaf = p.PropertyType == typeof(double) || p.PropertyType == typeof(int) || p.PropertyType == typeof(double?);
            if (value is null)
            {
                // A row the data does not author has nothing to range-check (#818), and neither has a
                // number a row may leave out (F4-b). Everything else that is null is a table that
                // failed to build.
                if (p.GetCustomAttribute<OptionalAttribute>() is not null) continue;
                errors.Add(leaf ? $"{source}: {name} must be a number; got null" : $"{source}: {name} must be an object; got null");
                continue;
            }
            if (leaf)
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
            // A number a row may leave out is left out by having no key (OptionalAttribute): a JSON null
            // would read as absent, which is a second way to say one thing.
            if (p.PropertyType == typeof(double?) && field.Value.ValueKind == JsonValueKind.Null)
            {
                errors.Add($"{source}: {path}.{field.Name} must be a number; leave the key out rather than writing null");
                continue;
            }
            if (p.PropertyType.IsClass && p.PropertyType != typeof(string)
                && p.PropertyType.Namespace == typeof(RulesTable).Namespace
                && !typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType))
                UnknownFields(field.Value, p.PropertyType, path + "." + field.Name, source, errors);
        }
    }

    /// <summary>
    /// The other half of <see cref="UnknownFields"/>: every rule the table declares is authored in the
    /// file. The JSON is the only source of a rule number; the code holds no second default to fall
    /// back on, so a missing key is an error, not a silent zero. An <see cref="OptionalAttribute"/>
    /// property is authored by the data or by nobody.
    /// </summary>
    public static void MissingFields(JsonElement element, Type type, string path, string source, List<string> errors)
    {
        if (element.ValueKind != JsonValueKind.Object) return; // UnknownFields reports the shape
        var keys = new HashSet<string>(element.EnumerateObject().Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.GetIndexParameters().Length > 0 || p.GetGetMethod() is null || p.GetSetMethod(true) is null) continue;
            if (p.Name == "EqualityContract") continue;
            if (!keys.Contains(p.Name))
            {
                if (p.GetCustomAttribute<OptionalAttribute>() is null)
                    errors.Add($"{source}: {path}.{Camel(p.Name)}" + MissingSuffix);
                continue;
            }
            if (p.PropertyType.IsClass && p.PropertyType != typeof(string)
                && p.PropertyType.Namespace == typeof(RulesTable).Namespace
                && !typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType))
                MissingFields(element.EnumerateObject().First(f => string.Equals(f.Name, p.Name, StringComparison.OrdinalIgnoreCase)).Value,
                    p.PropertyType, path + "." + Camel(p.Name), source, errors);
        }
    }

    const string MissingSuffix = " is missing; every rule is authored in the table";

    /// <summary>The file a <see cref="MissingFields"/> error names, or null for any other error.</summary>
    internal static string? MissingSource(string error)
    {
        if (!error.EndsWith(MissingSuffix, StringComparison.Ordinal)) return null;
        var head = error.Substring(0, error.Length - MissingSuffix.Length);
        var colon = head.LastIndexOf(": ", StringComparison.Ordinal);
        return colon < 0 ? null : head.Substring(0, colon);
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

public sealed record MatchRules
{
    /// <summary>A tie after the last scheduled inning plays on, at most this many extra innings; a tie at the cap is a tie (D8).</summary>
    public int ExtraInningsCap { get; init; }
    public MercyRules Mercy { get; init; } = new();
}

/// <summary>Mercy (§1): on by default; a lead of <see cref="Runs"/> at the end of an inning from <see cref="FromInning"/> ends the game; off below <see cref="MinScheduledInnings"/> innings.</summary>
public sealed record MercyRules
{
    public int Runs { get; init; }
    public int FromInning { get; init; }
    public int MinScheduledInnings { get; init; }
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
/// The corners are authored, not derived from <see cref="BaselineFt"/>. An 80-ft baseline
/// rotated 45° is 56.5685…, and the diamond plays at a rounded 56.57 (the 90-ft diamond's rounded
/// 63.64 × 80/90); deriving it would move the bases by thousandths of a foot and silently change
/// every route.
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
public sealed record InfieldRules
{
    /// <summary>Bag to bag. The unit a runner's progress is measured in.</summary>
    [Positive] public double BaselineFt { get; init; }

    /// <summary>Home to the rubber, along the center line.</summary>
    [Positive] public double MoundFt { get; init; }

    /// <summary>First and third, off the center line and out from home by the same amount each.</summary>
    [Positive] public double CornerFt { get; init; }

    /// <summary>Second, straight out from home.</summary>
    [Positive] public double SecondFt { get; init; }

    /// <summary>
    /// Half-diagonal of the drawn grass diamond around second and the mound (#729). The bags sit
    /// outside it, on the dirt, which is the whole reason it is smaller than <see cref="CornerFt"/>.
    /// </summary>
    [Positive] public double InnerHalfFt { get; init; }

    /// <summary>
    /// Outer arc of the 1B–2B–3B dirt, measured from the rubber (#729). Farther out than the home
    /// legs, so the back of the diamond reads as a curve rather than a matching frame.
    /// </summary>
    [Positive] public double BackArcFt { get; init; }

    /// <summary>
    /// The rubber sits between home and second, second is past the corners, and the drawn grass
    /// stays inside both the bag it points at and the arc behind it.
    ///
    /// <para>
    /// The bag-pad clearance is deliberately not a rule here. The grass vertex must also clear
    /// <c>ParkDiamond.BagPadR</c>, but that margin is 0.13 ft on the 80-ft diamond (1.64 ft at 90) — too thin
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
/// did not choose them. They were not rescaled when the 80-ft diamond shipped: whether the
/// compact profile scales the offset or the flare is #732's question, and an overlay that answered
/// it here would answer it by accident.
/// </para>
/// </summary>
public sealed record BoundaryRules
{
    /// <summary>
    /// The hip rail's offset from the foul line, into foul territory, measured perpendicular to the
    /// line. The dugout rail <b>is</b> this line. 0 at the pole, where the rail meets the fence.
    /// </summary>
    [Positive] public double FoulOffsetFt { get; init; }

    /// <summary>
    /// How far along the line the rail runs parallel before it flares out to the pole. A pole
    /// closer than this leaves the rail parallel the whole way (a smoothstep over nothing).
    /// </summary>
    [Positive] public double FlareStartFt { get; init; }

    /// <summary>
    /// The rail's top, pole to pole. A park's <c>fenceHeightFt</c> must stand over it (D15), which
    /// is the floor <see cref="ContentDataValidator"/> refuses a park under.
    /// </summary>
    [Positive] public double RailHeightFt { get; init; }

    /// <summary>
    /// The backstop's wrap behind the plate, as a Z. Negative: behind home is −Z, and a backstop in
    /// front of the plate is a field with no room to catch a pitch.
    /// </summary>
    [Signed] public double BackstopZFt { get; init; }

    /// <summary>
    /// Clearance from the dugout's back wall to the edge the stands may start at
    /// (<see cref="HarborWall.DugoutClearX"/>). Drawn, not played, and here for the same reason the
    /// grass diamond is in <c>infield.json</c>: it is measured off the rail, so it travels with it.
    /// </summary>
    [Positive] public double DugoutPadFt { get; init; }

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

public sealed record PitchingRules
{
    public PitchSpeedRules Speed { get; init; } = new();
    public PitchReleaseRules Release { get; init; } = new();
    public PitchFlightRules Flight { get; init; } = new();
    public PitchFamilyTable Families { get; init; } = new();
    public StarPitchShapeRules StarShapes { get; init; } = new();
    public StaminaRules Stamina { get; init; } = new();
    public CpuPitcherRules Cpu { get; init; } = new();
}

/// <summary>The one coefficient every family shares: mph per point of the arm's Velocity (PH-15-R6; the key keeps its historical name) (<see cref="AtBatResolver.PitchSpeedMph(PitchCommand, int, RulesTable)"/>). Base mph and charge mph are per family.</summary>
public sealed record PitchSpeedRules
{
    public double MphPerPitchStat { get; init; }
}

/// <summary>The charge release (spec §4.1): inside the first <see cref="NiceBandSec"/> of MAX is a Nice! release, +<see cref="NiceMul"/> mph.</summary>
public sealed record PitchReleaseRules
{
    [Positive] public double NiceBandSec { get; init; }
    [Positive] public double NiceMul { get; init; }
}

/// <summary>Release point, air time, and the break (<see cref="PitchFlight"/>, spec §4.1 – §4.3).</summary>
public sealed record PitchFlightRules
{
    [Signed] public double ReleaseHandX { get; init; }
    [Positive] public double ReleaseHandY { get; init; }
    public double ReleaseTowardPlate { get; init; }
    [Positive] public double ArcadeScale { get; init; }
    [Positive] public double AirMinSec { get; init; }
    [Positive] public double AirMaxSec { get; init; }
    [Positive] public double MinMph { get; init; }
    /// <summary>Feet the crossing moves at full break: half the zone width (spec §4.2).</summary>
    [Positive] public double BreakMaxFt { get; init; }
    /// <summary>The mid-flight bend the eye sees; it is gone by the plate.</summary>
    public double BreakEarly { get; init; }
    /// <summary>The drift grows over [breakLateFrom, breakLateFrom + breakLateSpan] of the flight.</summary>
    [Chance] public double BreakLateFrom { get; init; }
    [Positive] public double BreakLateSpan { get; init; }
    /// <summary>A charged pitch or a changeup takes this much of the break (reference: "essentially straight").</summary>
    [Chance] public double BreakDampedMul { get; init; }
    /// <summary>How fast a held stick brings the bend to full, per second, at Control 5 …</summary>
    [Positive] public double BreakRatePerSec { get; init; }
    /// <summary>… and per Control point above 5 (PH-15-R6; the key keeps its historical name).</summary>
    public double BreakRatePerPitchStat { get; init; }
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
/// <see cref="PitchFamily.Slider"/>, <see cref="PitchFamily.Sinker"/>) are <b>optional rows</b>:
/// their numbers and their natural sweep are P1-d's, which Jack accepted in the <c>trials/pitch5</c>
/// window on September 22, 2026 ("trial was good.") and #860 moved into the shipped
/// <c>pitching.json</c>. A table that does not author one still stops loudly on it, naming it,
/// never a silent fastball.
///
/// <para>
/// <b>Authored is a fact about the data, not about the code</b> (#818). The three are nullable rows
/// with no code default: the shipped <c>pitching.json</c> authors all three, and a table without a
/// key (a fixture, a trial that drops one) stops on <c>Of("slider")</c> and never selects it.
/// Nothing anywhere hard-wires which ids have numbers. <see cref="Fastball"/> and <see cref="Changeup"/> are not
/// nullable: a table without them is a game that cannot throw a pitch.
/// </para>
/// </summary>
public sealed record PitchFamilyTable
{
    /// <summary>Speed pressure, straight, with a mild mid-flight hump. Every pitcher throws it (PH-15-R1).</summary>
    public PitchFamilyRules Fastball { get; init; } = new();

    /// <summary>0.80× the meat; hangs at or above the fastball, then dumps to a lower aim (#668).</summary>
    public PitchFamilyRules Changeup { get; init; } = new();

    /// <summary>Pronounced arc and drop (PH-02-R2). Authored in the shipped data since #860; no code default (#818).</summary>
    [Optional] public PitchFamilyRules? Curveball { get; init; }

    /// <summary>Sideways movement that challenges coverage (PH-02-R2). Authored in the shipped data since #860; no code default (#818).</summary>
    [Optional] public PitchFamilyRules? Slider { get; init; }

    /// <summary>A faster dipping alternative to the curveball (PH-02-R2). Authored in the shipped data since #860; no code default (#818).</summary>
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

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<PitchFamilyTable, IReadOnlyList<string>> AuthoredCache = new();

    /// <summary>
    /// The authored ids in library order (<see cref="PitchFamily.All"/>). Built once per table and
    /// then handed out: <see cref="PitchSelection"/> asks for it on the SET tick, so it must not
    /// allocate per call. Cached by instance, not in a field, so a <c>with</c> copy that changes a
    /// row gets its own answer instead of the original's.
    /// </summary>
    public IReadOnlyList<string> Authored =>
        AuthoredCache.GetValue(this, static t => PitchFamily.All.Where(t.IsAuthored).ToList());

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
                + "this table does not author it (the shipped data does since #860). Authored: " + string.Join(", ", Authored));
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
public sealed record PitchFamilyRules
{
    /// <summary>Base mph, before the arm's Velocity, the charge, Nice! and the tired arm.</summary>
    [Positive] public double Mph { get; init; }
    /// <summary>mph a full charge adds to <em>this</em> family (spec §4.1).</summary>
    public double ChargeMph { get; init; }
    /// <summary>Feet the ball rides above the straight line at mid-flight; 0 is a shape with no hump.</summary>
    public double Hump { get; init; }
    /// <summary>Where the hang ends and the dump begins, as a share of the flight. 1 is a shape that never dumps.</summary>
    [Chance] public double HangUntil { get; init; }
    /// <summary>How fast Y interpolates toward the aim before <see cref="HangUntil"/>. Well below 1 keeps the ball up; 1 is a straight line.</summary>
    public double HangRate { get; init; }
    /// <summary>How fast Y interpolates after <see cref="HangUntil"/>. With <see cref="HangUntil"/> 1 there is no after, and it only has to keep the seam continuous.</summary>
    public double DumpRate { get; init; }
    /// <summary>Feet below a fastball's height this family crosses (up to one zone-half).</summary>
    public double DropFt { get; init; }
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
    [Chance] public double SweepFrom { get; init; }
    /// <summary>The stick's break takes <c>flight.breakDampedMul</c> for this family even uncharged (spec §4.1).</summary>
    public bool BreakDamped { get; init; }
    /// <summary>Stamina on top of <c>stamina.pitchCost</c> (spec §4.7).</summary>
    public int StaminaCost { get; init; }
    /// <summary>The batter can be fooled slow by it: the sour-slap pop band (§5.2) and the CPU batter's late error (§5.9).</summary>
    public bool OffSpeed { get; init; }
}

public sealed record StarPitchShapeRules
{
    public double HeatballWobbleHz { get; init; }
    public double HeatballWobbleFt { get; init; }
    public double PrismballWobbleHz { get; init; }
    public double PrismballWobbleFt { get; init; }
    public double CharmballWobbleHz { get; init; }
    public double CharmballWobbleFt { get; init; }
    [Chance] public double PhonyballSwitchAt { get; init; }
    [Signed] public double PhonyballEarlyX { get; init; }
    [Signed] public double PhonyballLateX { get; init; }
    public double CaskballRise { get; init; }
}

/// <summary>
/// Per-pitcher stamina (spec §4.7): pool = poolBase + Endurance × poolPerPitch (PH-15-R6); costs per verb; a
/// family's own extra is its row's <see cref="PitchFamilyRules.StaminaCost"/>, and a star's cost is
/// its <c>staminaCost</c> in star-skills.json. Only a pitch costs the arm: a hit, a homer or a run allowed
/// costs nothing (PH-08-R3).
///
/// <para>
/// Fatigue is gradual (PH-08, PH-08-R1): the fade runs from 0 at a pool of <see cref="FadeFrom"/> to 1 at an
/// empty pool. The arm loses fade × <see cref="ExhaustedMph"/> and its break is scaled from 1 down to
/// <see cref="TiredBreakMul"/>, so both keys name the arm at empty. Fatigue is never a random miss. Below
/// <see cref="TiredBelow"/> the arm is TIRED: the label on the card and the CPU's swap trigger, not a step.
/// </para>
/// </summary>
public sealed record StaminaRules
{
    public int PoolBase { get; init; }
    public int PoolPerPitch { get; init; }
    public int PitchCost { get; init; }
    public int ChargeCost { get; init; }
    public int BreakCost { get; init; }
    public int TiredBelow { get; init; }
    [Chance] public double TiredBreakMul { get; init; }
    public double ExhaustedMph { get; init; }
    public int CpuSwapLead { get; init; }
    [Positive] public int FadeFrom { get; init; }

    /// <summary>How far into the fade a pool is: 0 at <see cref="FadeFrom"/> or more, 1 at an empty pool or below.</summary>
    public double Fade(int stamina) => Math.Clamp((FadeFrom - stamina) / (double)FadeFrom, 0, 1);

    /// <summary>The mph a pool of <paramref name="stamina"/> costs the pitch (spec §4.7).</summary>
    public double MphLost(int stamina) => Fade(stamina) * ExhaustedMph;

    /// <summary>The scale on the stick's break a pool of <paramref name="stamina"/> leaves: the steering room (spec §4.7).</summary>
    public double BreakMul(int stamina) => 1 - Fade(stamina) * (1 - TiredBreakMul);
}

/// <summary>
/// The CPU pitcher (spec §4.8): a decision table, evaluated once per SET from the count, the
/// outs, the runners, and its stamina. Each row names a horizontal location and a family mix, and
/// <see cref="Match.CpuPitch"/> builds the pitch from the inputs a human has and nothing else
/// (PH-18-R1): the rubber for location, presses for the family, charge and steer as modifiers, a
/// bend no bigger than a held stick reaches. Scatter is σ = (11 − Control) × scatterFtPerPitchStat on
/// the rubber intent, never a dead-center default.
/// </summary>
public sealed record CpuPitcherRules
{
    /// <summary>0-0, 1-0, 1-1 and every count no other row claims.</summary>
    public CpuPitchRow Even { get; init; } = new();
    /// <summary>Ahead 0-2, 1-2: waste, then edge.</summary>
    public CpuPitchRow Ahead { get; init; } = new();
    /// <summary>Behind 2-0, 3-0, 3-1: middle-in, safe.</summary>
    public CpuPitchRow Behind { get; init; } = new();
    /// <summary>A runner on with two outs: middle, fast; never a pitch-out.</summary>
    public CpuPitchRow RunnerTwoOuts { get; init; } = new();
    public CpuPitchLocations Locations { get; init; } = new();
    /// <summary>
    /// Scatter in feet per Control point below 11 (PH-15-R6) (spec §4.8): noise on the CPU's own rubber
    /// intent in X, because a hand has no vertical input to miss in.
    /// </summary>
    public double ScatterFtPerPitchStat { get; init; }
    [Positive] public double TiredScatterMul { get; init; }
    /// <summary>A charged CPU pitch releases inside the Nice! band this often.</summary>
    [Chance] public double NiceChance { get; init; }
    [Chance] public double TapMin { get; init; }
    [Chance] public double TapSpan { get; init; }

    internal void Validate(string source, List<string> errors)
    {
        foreach (var (name, row) in new[] { ("even", Even), ("ahead", Ahead), ("behind", Behind), ("runnerTwoOuts", RunnerTwoOuts) })
        {
            if (row.Location is not ("edge" or "waste" or "middleIn" or "middle"))
                errors.Add($"{source}: pitching.cpu.{name}.location must be one of [edge, waste, middleIn, middle]; got '{row.Location}'");
            // A row that weights nothing has no family to throw. The run-time filter can still empty
            // a row for one pitcher (the row weights only families this arm does not own, or the
            // table does not author) and that falls back to the fastball every pitcher throws
            // (PH-15-R1, Match.CpuPitchByInputs) — but a row that weights nothing for *anybody* is a
            // broken table, and it is caught here rather than read as "always the fastball".
            if (row.Families.Total() <= 0)
                errors.Add($"{source}: pitching.cpu.{name}.families must weight at least one family: a row with no "
                    + "family to throw is not a pitch (spec §4.8, PH-18-R1)");
        }
    }
}

/// <summary>
/// One row of the CPU pitcher's table: where, and how it throws there. <see cref="Families"/> plus
/// <see cref="ChargeChance"/> and <see cref="SteerChance"/> are a family from the library, then
/// charge and steer as <b>independent modifiers</b> on it, the way a hand has them (PH-02-R1).
/// </summary>
public sealed record CpuPitchRow
{
    public string Location { get; init; } = "";
    [Chance] public double StarChance { get; init; }
    /// <summary>
    /// The weight of each family in this count, filtered at run time to the slots this pitcher can
    /// actually select (<c>PitchSelection.IsSelectable</c>) and renormalised.
    /// </summary>
    public CpuFamilyWeights Families { get; init; } = new();
    /// <summary>This share of pitches is charged to MAX, independent of the family.</summary>
    [Chance] public double ChargeChance { get; init; }
    /// <summary>This share holds the stick one way from release, independent of the family and the charge.</summary>
    [Chance] public double SteerChance { get; init; }
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
public sealed record CpuFamilyWeights
{
    public double Fastball { get; init; }
    public double Changeup { get; init; }
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

/// <summary>
/// Where the named locations sit, in feet from the zone's edges and center. Horizontal only: a hand
/// has no vertical input, so a pitch's height is its family's (PH-03).
/// </summary>
public sealed record CpuPitchLocations
{
    /// <summary>An edge target sits this far inside the frame.</summary>
    public double EdgeInsetFt { get; init; }
    /// <summary>The corner picked is the away side (by batter hand) this often.</summary>
    [Chance] public double EdgeAwayChance { get; init; }
    /// <summary>A waste pitch sits this far outside the frame.</summary>
    public double WasteOutFt { get; init; }
    /// <summary>Middle-in sits this far toward the batter from center.</summary>
    public double MiddleInFt { get; init; }
}

// ---------------------------------------------------------------------------------------
// batting.json — §5, §5.9
// ---------------------------------------------------------------------------------------

public sealed record BattingRules
{
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
    public PitchFactorRules PitchFactor { get; init; } = new();
    public OffenseItemRules Items { get; init; } = new();
    public CpuBatterRules Cpu { get; init; } = new();

    internal void Validate(string source, List<string> errors)
    {
        RulesValidation.Order(source, "batting.launch.minDeg", Launch.MinDeg, Launch.MaxDeg, errors);
        RulesValidation.Order(source, "batting.launch.maxDeg", Launch.MaxDeg, 180, errors);
        // A table that authors the window under its own floor is refused (PH-10-R1).
        RulesValidation.Order(source, "batting.window.floorFrames", Window.FloorFrames, Window.Frames, errors);
        RulesValidation.Order(source, "batting.cursor.perfectFraction", Cursor.PerfectFraction, 1, errors);
    }
}

/// <summary>
/// The timing window (spec §5.3, D4, D13): one window for every hitter, both swings and every human
/// rung (PH-10-R1, PH-11-R1, PH-15-R7, PH-17), × the star pitch's and the park's multipliers,
/// floored. It is centered on the ball's plate time minus <see cref="LeadSec"/>. Inside the window
/// timing decides direction only; the outermost (1 − squareFraction) of each half demotes the cursor
/// zone by one tier, never two.
/// </summary>
public sealed record ContactWindowRules
{
    /// <summary>
    /// The one window in frames at 60 Hz, total width (PH-10-R1: 9, the value Jack played and
    /// accepted in September 2026).
    /// </summary>
    [Positive] public double Frames { get; init; }

    [Positive] public double FloorFrames { get; init; }
    [Chance] public double SquareFraction { get; init; }
    /// <summary>
    /// The square press is this long before the ball reaches the plate (D13, #612 / #670): a
    /// human's eye times the ball meeting the bat, so the press leads the plate; the take is
    /// warped so its Contact mark meets the ball. 0.18 s, not the old press + 0.30 plane.
    /// </summary>
    [Positive] public double LeadSec { get; init; }
}

/// <summary>Charge adds loft; its power is the charge column of <see cref="QualityRules"/> (spec §5.5).</summary>
public sealed record SwingChargeRules
{
    public double LoftDeg { get; init; }
}

/// <summary>
/// Exit velocity by cursor zone (spec §5.2 table): the slap column at no charge, the charge column
/// at MAX, interpolated by the effective charge. Perfect charge is the ×1.25 of §5.5. Energy is
/// what the ball carries into a glove (<see cref="InPlay.Energy"/>).
/// </summary>
public sealed record QualityRules
{
    public ZoneExitRules Slap { get; init; } = new();
    public ZoneExitRules Charge { get; init; } = new();
    [Positive] public double PerfectEnergyMul { get; init; }
    [Positive] public double NiceEnergyMul { get; init; }
    [Positive] public double SourEnergyMul { get; init; }
}

public sealed record ZoneExitRules
{
    [Positive] public double Perfect { get; init; }
    [Positive] public double Nice { get; init; }
    [Positive] public double Sour { get; init; }

    public double For(ContactQuality quality) => quality switch
    {
        ContactQuality.Perfect => Perfect,
        ContactQuality.Nice => Nice,
        _ => Sour
    };
}

public sealed record ExitRules
{
    [Positive] public double BaseMph { get; init; }
    public double MphPerPower { get; init; }
    [Positive] public double TiredPitcherMul { get; init; }
}

/// <summary>
/// Launch (spec §5.4): base by power and charge, plus the pitch height (a low crossing launches
/// lower), plus the stick on a Star Swing only (up = over the top = grounder). Sour contact is forced to the topper
/// band (early) or the pop band (late, or a slap on a changeup / charged pitch).
/// </summary>
public sealed record LaunchRules
{
    public double LoftBaseDeg { get; init; }
    public double LoftPerPower { get; init; }
    /// <summary>Degrees of launch per foot the crossing sits above the zone center.</summary>
    public double PerFtOfHeight { get; init; }
    /// <summary>
    /// Stick U/D at contact. Only a Star Swing reads it (until Phase 6); an ordinary swing's launch is
    /// geometry (PH-12), and a bunt's launch is its own band.
    /// </summary>
    public double StickDeg { get; init; }
    public double NoiseDeg { get; init; }
    public double TopperMinDeg { get; init; }
    public double TopperSpanDeg { get; init; }
    public double PopMinDeg { get; init; }
    public double PopSpanDeg { get; init; }
    /// <summary>
    /// Under the ball (spec §5.4): degrees of launch per foot the crossing sits above the barrel's nice top
    /// (<see cref="SweetSpot.HalfHeightFt"/> over the cursor center), on top of <see cref="PerFtOfHeight"/>.
    /// Only the upper sour rim is there; enough of it lifts the launch past 90°, and the ball goes back over
    /// the catcher (<see cref="AtBatResolver.PastVertical"/>).
    /// </summary>
    public double UnderBallDegPerFt { get; init; }
    [Signed] public double MinDeg { get; init; }
    /// <summary>Past 90° the ball leaves up and back over the plate (§5.4); under 180°, so it never leaves down.</summary>
    public double MaxDeg { get; init; }
}

/// <summary>
/// Bunt (spec §5.8): the held bat meets the ball on the plane through the cursor, with no timed press; a high
/// crossing pops. The ball off the bat is the bunt's own <see cref="Response"/> by contact quality (PH-14-R1).
/// </summary>
public sealed record BuntRules
{
    public double LaunchMinDeg { get; init; }
    public double LaunchSpanDeg { get; init; }
    /// <summary>A crossing this far above the zone center is bunted into a pop.</summary>
    public double PopAboveCenterFt { get; init; }
    /// <summary>The held side's lean on the outgoing bunt, degrees of spray toward that side (PH-14-R2, <see cref="BuntHold.LeanDeg"/>).</summary>
    public double SideDeg { get; init; }
    public BuntResponseRules Response { get; init; } = new();
}

/// <summary>
/// The bunt's own response to contact quality (spec §5.8, PH-14-R1; <c>batting.bunt.response</c>). The exit is the
/// bunt's own by quality (a square bunt is the dead one, never the ordinary swing's harder ball), the spray spread
/// is the bunt's own by quality, and a sour bunt pops only when the ball crossed above the bat's center; below it
/// the ball is chopped down with the sour pace.
/// </summary>
public sealed record BuntResponseRules
{
    /// <summary>Exit speed off the held bat by quality, mph. No Power, charge, star or pitch term.</summary>
    public ZoneExitRules ExitMph { get; init; } = new();
    /// <summary>Total spray spread by quality, degrees (± half), around the side's lean.</summary>
    public ZoneExitRules SpreadDeg { get; init; } = new();
}

/// <summary>
/// Direction (spec §5.3): early pulls, late pushes, linear across the window to ±timingDeg;
/// on a Star Swing, stick L/R shifts the range by ±stickDeg; a bunt leans toward its held side; the zone adds its spread.
/// </summary>
public sealed record SprayRules
{
    public double PerfectSpreadDeg { get; init; }
    public double NiceSpreadDeg { get; init; }
    public double SourSpreadDeg { get; init; }
    /// <summary>
    /// Stick L/R at contact. Only a Star Swing (until Phase 6) reads it; a bunt reads its held side and an
    /// ordinary swing's direction is timing's (PH-12).
    /// </summary>
    public double StickDeg { get; init; }
    public double TimingDeg { get; init; }
    public double OutOfZoneSpanDeg { get; init; }
}

/// <summary>Sour contact pulled past the chalk (the chalk itself is <see cref="AtBatResolver.FoulLineDeg"/>, diamond geometry).</summary>
public sealed record FoulRules
{
    public double CheapPullMinDeg { get; init; }
    [Chance] public double CheapPullChance { get; init; }
    public double CheapPullPastDeg { get; init; }
    public double CheapPullSpanDeg { get; init; }
}

/// <summary>
/// The cursor (spec §5.2, D4): the bat drawn on the plate plane in world feet, centered where the
/// batter's box walk puts it, tall as the zone. Along the barrel the nice half-axis is
/// <see cref="NiceTipFt"/> toward the tip and <see cref="NiceHandleFt"/> toward the hands; the
/// perfect heart is <see cref="PerfectFraction"/> of that; the sour rim reaches
/// <see cref="RimFraction"/> beyond it. Bat (contact) scales the barrel; a charge narrows it.
/// Runners on base never change it: there is no plate-level chemistry (PH-16-R14, #891).
/// </summary>
public sealed record CursorRules
{
    [Positive] public double NiceTipFt { get; init; }
    [Positive] public double NiceHandleFt { get; init; }
    [Chance] public double PerfectFraction { get; init; }
    [Positive] public double RimFraction { get; init; }
    public double ScalePerContact { get; init; }
    [Positive] public double ChargeMul { get; init; }
}

/// <summary>Hit by pitch (spec §4.6): the body circle at the plate plane, in world feet.</summary>
public sealed record HbpRules
{
    [Positive] public double BodyRadiusFt { get; init; }
}

/// <summary>Star-swing rules that are not the skill's own numbers (those are star-skills.json, spec §13).</summary>
public sealed record StarSwingRules
{
    public double PrismballSpraySpanDeg { get; init; }
}

/// <summary>
/// The pitch's say in the exit (spec §5.5): a charged pitch met sour, a charged pitch met by a
/// perfect charge, and a high-Movement arm (PH-15-R6) dampening non-perfect contact per point above 5.
/// </summary>
public sealed record PitchFactorRules
{
    [Positive] public double ChargedVsSour { get; init; }
    [Positive] public double ChargedVsPerfectCharge { get; init; }
    [Chance] public double NiceDampPerPitch { get; init; }
    [Chance] public double SourDampPerPitch { get; init; }
}

/// <summary>
/// Items after contact (§12): each is a field effect with a duration, never a kind conversion.
/// A peel slips the fielder who steps on it, a rocket dazes the body it hits, a POW hops every
/// ball on the dirt once. They add time; the geometry then decides the play.
/// </summary>
public sealed record OffenseItemRules
{
    /// <summary>Flight of the item from the dugout to its aim, play seconds; it lands by geometry when the clock gets there (§12).</summary>
    [Positive] public double FlySec { get; init; }
    /// <summary>A peel is a spot on the grass: a body inside this radius of it slips (§12).</summary>
    [Positive] public double PeelRadiusFt { get; init; }
    /// <summary>How long the peel stays on the grass after it lands.</summary>
    [Positive] public double PeelSec { get; init; }
    /// <summary>The CPU offense throws its offered item when the batter's slack at first (§9.9 margin) is under this: the item is thrown when it would matter, never on a roll.</summary>
    [Signed] public double CpuThrowMarginSec { get; init; }
    /// <summary>A fielder on a peel cannot take the ball for this long.</summary>
    [Positive] public double SlipSec { get; init; }
    /// <summary>A dazed fielder cannot take the ball for this long.</summary>
    [Positive] public double DazeSec { get; init; }
    /// <summary>A POW keeps every ball on the dirt hopping, unscoopable, for this long.</summary>
    [Positive] public double PowHopSec { get; init; }
}

/// <summary>
/// The CPU batter (spec §5.9): a table evaluated from one read of the crossing — the flight as it stands at
/// the commit instant (<see cref="Match.CpuReadPitch"/>), never the future the pitcher has yet to steer. Zone class by the crossing (middle third / edge / near / far), the swing by
/// count, the box by tracking (perfect, or the last pitch's crossing plus a fixed offset; worse
/// after the pitcher moved on the rubber), timing σ by Bat and the difficulty rung.
/// </summary>
public sealed record CpuBatterRules
{
    /// <summary>The middle third of the frame, as a fraction of its half-width and half-height.</summary>
    [Chance] public double MiddleFraction { get; init; }
    /// <summary>Outside the frame by at most this is "near" (chaseable); further is a take.</summary>
    public double NearFt { get; init; }
    [Chance] public double EdgeSwingChance { get; init; }
    /// <summary>Chase % = chaseBase − Bat, with fewer than two strikes …</summary>
    public double ChaseBase { get; init; }
    /// <summary>… and with two strikes.</summary>
    public double ChaseTwoStrikesBase { get; init; }
    /// <summary>Charge on 2-0 / 3-0 / 3-1 from this Bat …</summary>
    public int ChargeBatMin { get; init; }
    /// <summary>… and with a runner in scoring position and fewer than two outs from this Bat.</summary>
    public int RispChargeBatMin { get; init; }
    [Chance] public double SacBuntChance { get; init; }
    public int SacBuntBatMax { get; init; }
    public int SacBuntTrailMax { get; init; }
    /// <summary>
    /// The side the squared CPU holds (§5.9, PH-14-R2): first-base side at this chance, else third, drawn once at SET
    /// with the square. Even, so the side is a tell to read and not a habit to cheat.
    /// </summary>
    [Chance] public double SacBuntFirstSideChance { get; init; }
    /// <summary>The headless CPU batter has been squared this long at the plate time (§7.3); the client's clock replaces it when there is one.</summary>
    [Positive] public double SacBuntSquareSec { get; init; }
    /// <summary>A captain with a star, a runner on or two strikes.</summary>
    [Chance] public double StarChance { get; init; }
    /// <summary>Timing σ = (11 − Bat) × this, frames.</summary>
    public double ErrorFramesPerBatStat { get; init; }
    /// <summary>Fooled by a changeup (late) or a charged pitch (early) when not tracked: this many frames …</summary>
    public double FooledMinFrames { get; init; }
    /// <summary>… plus up to this many more.</summary>
    public double FooledSpanFrames { get; init; }
    /// <summary>After release the batter re-reads the ball and centers the cursor on it this often.</summary>
    [Chance] public double TrackPerfectChance { get; init; }
    /// <summary>Otherwise the box stays at the guess (the last pitch's crossing) plus a fixed offset of this …</summary>
    public double MistrackMinFt { get; init; }
    /// <summary>… plus up to this.</summary>
    public double MistrackSpanFt { get; init; }
    /// <summary>The re-read fails this often when the pitcher moved on the rubber since the last pitch …</summary>
    [Chance] public double MistrackMovedChance { get; init; }
    /// <summary>… and this often when they did not (both × the rung's mistrackMul).</summary>
    [Chance] public double MistrackChance { get; init; }
    /// <summary>The CPU's two stick aims, drawn on a Star Swing only: an ordinary swing reads no stick (§5.9, PH-12).</summary>
    public double SpraySigmaDeg { get; init; }
    /// <inheritdoc cref="SpraySigmaDeg"/>
    public double LaunchAimSigma { get; init; }
    public CpuArchetypeRules Archetype { get; init; } = new();
    /// <summary>
    /// The CPU batter commits this long before the square press (plate − batting.window.leadSec),
    /// from the trajectory as it stands then (spec §3, §5.9). Its earliest error is −decideLeadSec × 60 frames.
    /// </summary>
    [Positive] public double DecideLeadSec { get; init; }
}

/// <summary>Charge vs slap by archetype (spec §5.9), derived from the Bat / Run split.</summary>
public sealed record CpuArchetypeRules
{
    [Chance] public double Balanced { get; init; }
    [Chance] public double Power { get; init; }
    [Chance] public double Speed { get; init; }
    [Chance] public double Technique { get; init; }
    /// <summary>Bat − Run at least this = power; Run − Bat at least this = speed.</summary>
    public int SplitStat { get; init; }
    /// <summary>Both Bat and Run at least this = technique.</summary>
    public int TechniqueMin { get; init; }
}

// ---------------------------------------------------------------------------------------
// flight.json — §6
// ---------------------------------------------------------------------------------------

public sealed record FlightRules
{
    [Positive] public double Gravity { get; init; }
    public double Drag { get; init; }
    /// <summary>
    /// Arcade hang: sample times are stretched by this so gloves can get under a fly. Carry
    /// does not change. The stretched sample clock <em>is</em> the play clock fielders and
    /// runners run on (<see cref="LivePlaySystem.ElapsedSeconds"/>) — one clock (§0.3, §6.1).
    /// </summary>
    [Positive] public double TimeScale { get; init; }
    [Positive] public double PlateHeightFt { get; init; }

    /// <summary>All contacts share the same clock; launch and exit never select a speed mode.</summary>
    public double TimeScaleFor(double launchDeg, double exitMph, RulesTable table) => TimeScale;
    /// <summary>How much of the flag reading the ball feels at field level (drag is taken relative to the wind).</summary>
    public double WindMul { get; init; }
    [Positive] public int SampleHz { get; init; }
    [Positive] public double MaxSeconds { get; init; }
    // The roll, the bounce, the skid and the wall carom are not here (F3-c, FD-05 / FD-03): the ball reads them off the
    // ground row of the zone it is on (grounds.json, through GroundZones.RowAt) and off the wall material of the segment
    // it meets (walls.json, through WallMaterial.OfSegment). One copy of each number, where a park can reach it.
    public BattedBallClassRules Classes { get; init; } = new();
    public DeadBallRules DeadBall { get; init; } = new();

    /// <summary>
    /// This table with one park's air (§0.3, FD-03): <c>dragMul</c> multiplies the root's drag and
    /// <c>windMul</c> replaces the global exposure. Every other number — gravity, the shared stretch, the
    /// plate, the sample clock — is the global one, and the landing, class and dead-ball blocks are shared
    /// by reference. The ground and the wall are not on this table at all: a park reaches them by naming
    /// which ground row each of its zones stands on (<see cref="GroundZones"/>), never by a copy here
    /// (F3-b, F3-c). Built once per match by <see cref="RulesTable.AtPark"/>, never per flight.
    /// </summary>
    internal FlightRules WithEnvironment(ParkEnvironment env) => this with
    {
        Drag = Drag * (env.DragMul ?? 1.0),
        WindMul = env.WindMul ?? WindMul
    };

    internal void Validate(string source, List<string> errors)
    {
        RulesValidation.Order(source, "flight.classes.topperMaxLaunchDeg", Classes.TopperMaxLaunchDeg, Classes.GrounderMaxLaunchDeg, errors);
        RulesValidation.Order(source, "flight.classes.grounderMaxLaunchDeg", Classes.GrounderMaxLaunchDeg, Classes.ChopperMaxLaunchDeg, errors);
        RulesValidation.Order(source, "flight.classes.chopperMaxLaunchDeg", Classes.ChopperMaxLaunchDeg, Classes.LinerMaxLaunchDeg, errors);
    }
}

/// <summary>The one batted-ball class table (§6.2): launch and exit at contact, refined by the flight.</summary>
public sealed record BattedBallClassRules
{
    /// <summary>Below this launch: topper (a weak roller in front of the plate).</summary>
    public double TopperMaxLaunchDeg { get; init; }
    /// <summary>Topper … this: grounder (an infield hop).</summary>
    public double GrounderMaxLaunchDeg { get; init; }
    /// <summary>Topper … this with a high first bounce inside the plate: chopper.</summary>
    public double ChopperMaxLaunchDeg { get; init; }
    public double ChopperFirstBounceFt { get; init; }
    public double ChopperBounceHeightFt { get; init; }
    public double ChopperMinExitMph { get; init; }
    /// <summary>Grounder … this with real exit: liner (a rope).</summary>
    public double LinerMaxLaunchDeg { get; init; }
    public double LinerMinExitMph { get; init; }
    /// <summary>Dirt / grass lip past the rubber. A fly landing inside it is a pop; infielders own the hop inside it.</summary>
    [Positive] public double InfieldLipFt { get; init; }
}

/// <summary>Hit type by carry when nobody catches it. P3 replaces these with runner geometry.</summary>
public sealed record DeadBallRules
{
    public double MinSec { get; init; }
    public double AfterHangSec { get; init; }
    /// <summary>A flight nobody plays holds this long past the ball's rest (or exit) before the result.</summary>
    public double RestHoldSec { get; init; }
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
/// <b>The ball reads the row under it (F3-c, #856).</b> The batted ball's roll and every one of its
/// bounces, the overthrow's roll and the local bobble each take their ground numbers from the row of
/// the zone the ball is in (<see cref="GroundZones.RowAt"/>): there is no other copy.
/// <c>flight.json</c>'s <c>roll</c>, <c>bounce</c> and <c>skid</c> and <c>fielding.json</c>'s
/// <c>overthrow.decelFtPerSec2</c> and three <c>handling.bobble*</c> keys are gone, on both roots.
/// </para>
///
/// <para>
/// <b>Every row here is today's number.</b> F3-b (#846) seeded the four rows with the flight's blocks
/// and F3-c moved the two loose-ball models' ground numbers in at their shipped values, so `grass`,
/// `dirt`, `ice` and `ash` are equal and the ground under the ball cannot change a play.
/// <c>GroundLibraryTests</c> pins every row to those numbers, written in the test. A ground value that
/// is <em>not</em> today's arrives with Crystal (F9-a), as a trial, after Jack has accepted it.
/// </para>
/// </summary>
public sealed record GroundLibrary
{
    /// <summary>The outfield of every shipped park. The rows are equal today, so this is also the ball's only roll.</summary>
    public GroundRules Grass { get; init; } = new();

    /// <summary>The infield and the warning track of every park, whatever its <c>surface</c> says (<see cref="GroundZones"/>).</summary>
    public GroundRules Dirt { get; init; } = new();

    /// <summary>
    /// Crystal Rink's surface (F9-a): the ball runs and skids farther and a body is slower to start, stop and turn, never
    /// faster and never off its heading (FD-04 B). Proposed numbers, not tuned; Jack judges them in play (FD-13-R2).
    /// </summary>
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
    /// roots: the skid band has to be a band. <c>flight.skid</c> held the same order until F3-c retired
    /// it; held here per row, a trial that authors one ground cannot write a band that never opens.
    /// </summary>
    internal void Validate(string source, List<string> errors)
    {
        foreach (var id in Ids)
        {
            var row = Of(id);
            RulesValidation.Order(source, $"grounds.{id}.skid.impactMinDeg", row.Skid.ImpactMinDeg, row.Skid.ImpactMaxDeg, errors);
        }
    }
}

/// <summary>
/// One ground's numbers: everything the ground does to a ball on it, for the three ground models that
/// read it (FD-05) — the batted ball (<see cref="BallFlight"/>: <see cref="Roll"/>, <see cref="Bounce"/>,
/// <see cref="Skid"/>), the overthrow (<see cref="Overthrow"/>) and the local bobble (<see cref="Bobble"/>).
/// What belongs to the <em>ball</em> or the <em>hands</em> rather than the ground stays where it was: a
/// sailed throw's carry and its roll cap are the throw's (<c>fielding.overthrow</c>), and the fumble's
/// spill, stun, rebound ceiling and settle are the fumble's (<c>fielding.handling</c>).
///
/// <para>
/// <b>The skid band is judged against the row under each bounce.</b> The incoming impact angle is the ball's; the
/// band between skid and hop (<c>skid.impactMinDeg</c> / <c>impactMaxDeg</c>) is the ground's.
/// Each impact blends the two responses from its current incoming velocity.
/// </para>
///
/// <para>
/// <c>body</c> is the one block that is not the ball's: what the ground does to a
/// fielder's start, brake and cut-back and to a runner's slide and overrun (<see cref="GroundBodyRules"/>,
/// F3-d). A body reads the row of the zone it stands in, through <see cref="GroundZones.RowAt"/>.
/// </para>
/// </summary>
public sealed record GroundRules
{
    /// <summary>The ball rolling on this ground: friction and the speed it comes to rest at.</summary>
    public RollRules Roll { get; init; } = new();

    /// <summary>The hop off this ground.</summary>
    public BounceRules Bounce { get; init; } = new();

    /// <summary>The rope's skid across this ground, band included.</summary>
    public SkidRules Skid { get; init; } = new();

    /// <summary>A sailed throw (or the shipped fumble's scatter) rolling loose on this ground.</summary>
    public GroundOverthrowRules Overthrow { get; init; } = new();

    /// <summary>The local bobble's ball (#721) rebounding and rolling on this ground.</summary>
    public GroundBobbleRules Bobble { get; init; } = new();

    /// <summary>What this ground does to a body standing on it (FD-04 B, F3-d): five multipliers, every one 1.0 today.</summary>
    public GroundBodyRules Body { get; init; } = new();
}

/// <summary>The batted ball's hop off a ground: vertical speed × restitution, horizontal × horizontal; below <c>minVy</c> it rolls.</summary>
public sealed record BounceRules
{
    [Chance] public double Restitution { get; init; }
    [Chance] public double Horizontal { get; init; }
    public double MinVy { get; init; }
}

/// <summary>Shallow impacts skid; interpolate toward BounceRules across this incoming-impact angle band.</summary>
public sealed record SkidRules
{
    public double ImpactMinDeg { get; init; }
    public double ImpactMaxDeg { get; init; }
    public double MinVy { get; init; }
    [Chance] public double Restitution { get; init; }
    [Chance] public double Horizontal { get; init; }
}

/// <summary>The batted ball rolling on a ground: friction (ft/s²) and the speed it comes to rest below.</summary>
public sealed record RollRules
{
    [Positive] public double Friction { get; init; }
    public double RestSpeed { get; init; }
}

/// <summary>
/// A loose thrown ball rolling to a stop on a ground (<c>fielding.overthrow.decelFtPerSec2</c> until F3-c). The throw's own
/// numbers — how much speed it carries past the target and how far it may roll — stay in <see cref="OverthrowRules"/>.
/// </summary>
public sealed record GroundOverthrowRules
{
    [Positive] public double DecelFtPerSec2 { get; init; }
}

/// <summary>
/// The local bobble's ball on a ground (#721, F693-02-local-bobble-restitution, -ground-horizontal, -rolling-deceleration;
/// <c>fielding.handling.bobbleRestitution</c> / <c>bobbleGroundRetain</c> / <c>bobbleDecelFtPerSec2</c> until F3-c): the share of
/// its downward speed it rebounds with, the share of its roll it keeps at each impact, and how fast it slows rolling. The spill,
/// the rebound ceiling and the settle are the fumble's and stay in <see cref="HandlingRules"/>.
/// </summary>
public sealed record GroundBobbleRules
{
    [Chance] public double Restitution { get; init; }
    [Chance] public double GroundRetain { get; init; }
    public double DecelFtPerSec2 { get; init; }
}

/// <summary>
/// What one ground does to a body on it (spec §8, §9, §16; FD-04 B, FD-05, F3-d #857). Each multiplier scales a
/// <em>time</em> or a <em>length</em> the body already has, never a speed, so a number above 1 always reads "more
/// slippery" and 1 is the table itself:
/// <list type="bullet">
/// <item><c>startMul</c> × <c>fielding.chase.accelSec</c>: rest to the rated speed (and the planner's half-ramp).</item>
/// <item><c>brakeMul</c> × <c>fielding.chase.brakeSec</c>: the rated speed to rest.</item>
/// <item><c>cutMul</c> × the time the across-heading correction takes, which is the ramp time: the cut-back.</item>
/// <item><c>slideMul</c> × <c>running.bags.slideFt</c> into a bag on this ground.</item>
/// <item><c>overrunMul</c> × <c>running.bags.overrunFt</c> past a bag on this ground.</item>
/// </list>
///
/// <para>
/// <b>The body always goes where the stick points.</b> Nothing here touches the rated speed, the asked velocity, the
/// stick or the heading the body settles on (FD-04: no skating, no loss of control): a slick ground is slower to
/// answer, not a different answer. The first three act through the §8 response law and exist only where it is on
/// (<c>chase.accelSec</c> or <c>brakeSec</c> above 0, as shipped; implementation map finding 8); a
/// multiplier cannot switch the law on, because the switch reads the table and not a product. The slide and the
/// overrun are not behind the law and act on both roots.
/// </para>
///
/// <para>
/// <b>Every row carries 1.0.</b> <c>x × 1.0</c> is exact in IEEE doubles, and each reader multiplies its time or
/// length once, so the shipped and the trial paths are bit-identical to the code before the block existed. A value
/// that is not 1.0 arrives with Crystal (F9-a) as a trial Jack has accepted. Full traction (FD-04 C: drift, slow
/// steering, a runner who overruns any bag) is held; were it accepted it would be more named fields in this block,
/// not a second block.
/// </para>
/// </summary>
public sealed record GroundBodyRules
{
    /// <summary>× <c>fielding.chase.accelSec</c>: the time from rest to the rated speed on this ground.</summary>
    [Positive] public double StartMul { get; init; }

    /// <summary>× <c>fielding.chase.brakeSec</c>: the time from the rated speed to rest on this ground.</summary>
    [Positive] public double BrakeMul { get; init; }

    /// <summary>× the ramp time the component across the body's heading is corrected over: the cut-back.</summary>
    [Positive] public double CutMul { get; init; }

    /// <summary>× <c>running.bags.slideFt</c>: how far out a runner goes down into a bag on this ground.</summary>
    [Positive] public double SlideMul { get; init; }

    /// <summary>× <c>running.bags.overrunFt</c>: how far a runner carries past a bag on this ground.</summary>
    [Positive] public double OverrunMul { get; init; }
}

// ---------------------------------------------------------------------------------------
// walls.json — the closed wall-material library (§6.1, §16; FD-06, F3-b)
// ---------------------------------------------------------------------------------------

/// <summary>
/// What the ball does off one kind of wall: one named row per id in <see cref="WallMaterial"/>, the
/// same shape as <see cref="GroundLibrary"/> and for the same reasons (FR-02). One row today —
/// <c>padded</c>, carrying exactly what <c>flight.wall</c> carried — because every span of every park
/// is the one padded wall the ball caroms off now.
///
/// <para>
/// <b>A material, not a span.</b> Which span of which fence is made of what, and whether it can be
/// climbed or robbed over, is the polyline fence's business (FD-06, F2-c): a trait like
/// <c>climbable</c> belongs to a stretch of wall, not to the stuff it is made of, and putting one here
/// would make the library answer a question it cannot see. Since F3-c the carom reads the row of the
/// segment it meets (<see cref="WallMaterial.OfSegment"/>, which answers <c>padded</c> for every
/// segment until F2-c gives a span its own) and <c>flight.wall</c> is gone.
/// </para>
/// </summary>
public sealed record WallMaterialLibrary
{
    /// <summary>The padded outfield wall every park has today, at what <c>flight.wall</c> carried.</summary>
    public WallRules Padded { get; init; } = new();

    /// <summary>Crystal Rink's glass boards (F9-a): a livelier carom than the pad. Proposed, not tuned.</summary>
    public WallRules Glass { get; init; } = new();

    WallRules? Named(string id) => id switch
    {
        WallMaterial.Padded => Padded,
        WallMaterial.Glass => Glass,
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

/// <summary>The fence below fence height: the carom (§7.9). Normal speed × restitution, along the wall × tangential.</summary>
public sealed record WallRules
{
    [Chance] public double Restitution { get; init; }
    [Chance] public double Tangential { get; init; }
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
/// <summary>The fence the outfield starts were authored against (F2-d): the three posts, feet from home.</summary>
public sealed record AuthoredFenceRules
{
    [Positive] public double LeftFt { get; init; }
    [Positive] public double CenterFt { get; init; }
    [Positive] public double RightFt { get; init; }
}

public sealed record FielderRules
{
    public FielderSpotRules First { get; init; } = new();
    public FielderSpotRules Second { get; init; } = new();
    public FielderSpotRules Third { get; init; } = new();
    public FielderSpotRules Short { get; init; } = new();
    public FielderSpotRules Left { get; init; } = new();
    public FielderSpotRules Center { get; init; } = new();
    public FielderSpotRules Right { get; init; } = new();

    /// <summary>
    /// The fence the three outfield starts are authored against (FD-07, F2-d; the default park's posts, 232 / 280 / 232): a
    /// park that names no start stands each outfielder at his bearing and his fraction of this fence, on its own fence.
    /// </summary>
    public AuthoredFenceRules AuthoredFence { get; init; } = new();

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
public sealed record FielderSpotRules
{
    /// <summary>Off the centre line. Negative toward third and left.</summary>
    [Signed] public double XFt { get; init; }

    /// <summary>Out from home, along the centre line.</summary>
    [Positive] public double ZFt { get; init; }
}

// ---------------------------------------------------------------------------------------
// fielding.json — §8
// ---------------------------------------------------------------------------------------

public sealed record FieldingRules
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
        if (Chase.WallClearanceFt >= Chase.LooseScoopFt)
            errors.Add($"{source}: fielding.chase.wallClearanceFt must be smaller than looseScoopFt so a wall ball remains reachable");
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
public sealed record FieldStickRules
{
    [Chance] public double EnterMag { get; init; }
    [Chance] public double LeaveMag { get; init; }
    [Positive] public double CalibrationSec { get; init; }
    [Chance] public double CenterOffsetMax { get; init; }
    [Chance] public double SampleSpreadMax { get; init; }
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
public sealed record ReactionRules
{
    [Positive] public double PitcherSec { get; init; }
    /// <summary>Post-delivery recovery after full-swing contact; no difficulty or hang-time shortening. Bunts use the ordinary read.</summary>
    [Positive] public double PitcherRecoverySec { get; init; }
    [Positive] public double CatcherSec { get; init; }
    [Positive] public double FirstSec { get; init; }
    [Positive] public double SecondSec { get; init; }
    [Positive] public double ThirdSec { get; init; }
    [Positive] public double ShortSec { get; init; }
    [Positive] public double OutfieldSec { get; init; }
    [Positive] public double ThrowBaseSec { get; init; }
    public double ThrowPerFieldSec { get; init; }
    [Positive] public double ThrowMinSec { get; init; }

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
/// Cover, cutoff and backup bodies (§8.7): the body's own pursuit speed from contact with no read (at lockoutMul 1 and
/// chaseSpeedWeight 0, the old flat speed (D11) after a start delay, gated by the body's reaction lockout)
/// (F693-02-coverage-budget, #718) — covering is a known assignment to a fixed spot, not the recognition of where a
/// ball went. A throw is caught inside the cover radius of its target.
/// </summary>
public sealed record CoverRules
{
    /// <summary>The flat cover speed (D11); <see cref="ChaseSpeedWeight"/> blends the body's own speed over it.</summary>
    [Positive] public double FtPerSec { get; init; }
    /// <summary>Cover starts walking this long after contact. 0 is at contact.</summary>
    public double StartSec { get; init; }
    public double StopFt { get; init; }
    /// <summary>A throw landing farther than this from the receiver is not caught: it skips past, live (§8.5).</summary>
    [Positive] public double RadiusFt { get; init; }
    /// <summary>The backup body stands this far behind a throw's target, on its line.</summary>
    [Positive] public double BackupFt { get; init; }
    /// <summary>How much of the body's reaction lockout (§8.2) gates its cover walk: 0 is the rule (#718), 1 the old gate.</summary>
    [Chance] public double LockoutMul { get; init; }
    /// <summary>How much of the body's own pursuit speed (<c>fielding.chase</c>) a cover, cutoff or backup walk uses in place of <see cref="FtPerSec"/>: 0 is the flat speed exactly, 1 the body's speed (#718).</summary>
    [Chance] public double ChaseSpeedWeight { get; init; }
}

/// <summary>
/// The bunt defense (§7.3, <c>fielding.bunt</c>): who crashes and how far when the batter squares, who covers
/// first and second behind the crash, who charges the triangle after contact, and the throw rule's numbers
/// (a bunt too hard for the sac is played at the lead force; the squeeze runner is thrown for only from
/// inside the plate distance). Positions are the diamond's keys; <see cref="BuntDefense"/> reads them.
/// </summary>
public sealed record BuntDefenseRules
{
    /// <summary>The crash bodies run this far toward the plate from their spots (25 in the reference).</summary>
    [Positive] public double CrashFt { get; init; }
    /// <summary>Who crashes at the square.</summary>
    public string[] Crash { get; init; } = [];
    /// <summary>Who covers first behind the crash.</summary>
    public string CoverFirst { get; init; } = "";
    /// <summary>Who covers second behind the crash.</summary>
    public string CoverSecond { get; init; } = "";
    /// <summary>Who converges on a bunt after contact (the triangle); the glove among them is the pursuit planner's.</summary>
    public string[] Charge { get; init; } = [];
    /// <summary>A charging body that is not the glove stops this far from the ball.</summary>
    [Positive] public double ChargeStopFt { get; init; }
    /// <summary>On a squeeze the glove throws home only from inside this distance to the plate.</summary>
    [Positive] public double SqueezeHomeFt { get; init; }
    /// <summary>A bunt leaving the bat at or above this is too hard for the sac: the lead force is played if makeable.</summary>
    [Positive] public double HardExitMph { get; init; }
}

/// <summary>
/// A throw that misses its cover, or drops at an uncovered bag, rolls on from where it landed. These are the throw's numbers;
/// how fast the loose ball slows is the ground's, read off the row under it (<see cref="GroundOverthrowRules"/>, F3-c).
/// </summary>
public sealed record OverthrowRules
{
    /// <summary>A sailed throw keeps this much of its flight speed past the target.</summary>
    [Chance] public double CarryMul { get; init; }
    [Positive] public double MaxRollFt { get; init; }
}

public sealed record ChaseRules
{
    [Positive] public double BaseFtPerSec { get; init; }
    public double FtPerSecPerRun { get; init; }
    [Positive] public double FrozenMul { get; init; }
    [Positive] public double MinFtPerSec { get; init; }
    public double StepStopFt { get; init; }
    /// <summary>A route counts as reachable when the glove lands within this of the meet point.</summary>
    public double ReachSlackFt { get; init; }
    /// <summary>After Select / R swaps the glove, another press is ignored and the CPU chase waits for this long (§8.9).</summary>
    public double SwapLockSec { get; init; }
    /// <summary>After a hand-off the body the ring left keeps its velocity for this long, then stops (§8.9): the swap does not jerk.</summary>
    public double HandoffCoastSec { get; init; }
    /// <summary>The nearest body to a loose ball chases it; a throw's receiver steps to a ball inside this of them.</summary>
    [Positive] public double LooseScoopFt { get; init; }
    /// <summary>Body clearance inside the fence, small enough to recover a ball with loose scoop reach.</summary>
    [Positive] public double WallClearanceFt { get; init; }
    /// <summary>
    /// An outfielder chasing a ball hit in the air runs at the one glove speed × this (§8.1, §8.2). The S-29 lever since the
    /// outfield read went back to the reference (#609): the read is when a body starts, this is how much ground it covers.
    /// </summary>
    [Positive] public double OutfieldAirMul { get; init; }
    /// <summary>
    /// An infielder (P, C, 1B, 2B, 3B, SS) under a ball on the stretched clock (a fly or a pop, §6.1) runs at the one glove speed × this
    /// (§8.1). The other half of the S-29 lever (#636): once the hand-off honours the infielder's route in the air (D17), this is how far
    /// the infield reaches back under a short fly past the lip. A liner runs on its own clock and an infielder runs the one speed at it;
    /// balls on the dirt run the one speed the §10.4 double-play rows were tuned on.
    /// </summary>
    [Positive] public double InfieldAirMul { get; init; }
    /// <summary>
    /// The response law (#718, F693-02-carry-movement-response): seconds from rest to the body's rated speed, a linear ramp.
    /// 0.20 s; 0 is the old instant step. The pursuit planner charges half of it to a route.
    /// </summary>
    public double AccelSec { get; init; }
    /// <summary>Seconds from the rated speed to rest, a constant deceleration; a reversal is this brake and then the ramp. 0.10 s; 0 is the old instant stop.</summary>
    public double BrakeSec { get; init; }
    /// <summary>
    /// The route keeps this far off a status volume's disc when it goes around one (§14, FD-14, F4-g): a bent path on the rim
    /// is not clipped by the body's own ramp. Not a tuned number; a park with no volume never reads it.
    /// </summary>
    public double VolumeClearFt { get; init; }
}

public sealed record CatchRules
{
    [Positive] public double RadiusBaseFt { get; init; }
    public double RadiusPerField { get; init; }
    /// <summary>
    /// The authored stand-up reach every body without its own <see cref="Character.ReachFt"/> gets (F693-02-catch-reach-envelope,
    /// #719): roughly what the visible glove covers from a planted stance, independent of ratings. 0 keeps the legacy
    /// <c>radiusBaseFt + Field × radiusPerField</c>; the game plays 4.0. A character's authored
    /// <c>reachFt</c> wins over both, and the ability bonuses add to whichever applies.
    /// </summary>
    public double StandUpReachFt { get; init; }
    public double ClamberRadiusFt { get; init; }
    public double WindowPadFt { get; init; }
    public double DiveReachFt { get; init; }
    public double JumpReachFt { get; init; }
    public double DiveMaxBallY { get; init; }
    public double NeedsJumpReachFt { get; init; }
    /// <summary>Rob heights (§8.4): a leap at the wall takes a ball clearing the fence by at most this.</summary>
    public double JumpRobFt { get; init; }
    public double SuperJumpRobFt { get; init; }
    public double ClamberRobFt { get; init; }
    public double BuddyJumpRobFt { get; init; }
    public double TouchScoopY { get; init; }
    /// <summary>Highest ball center a planted glove can take; a live jump adds its actual root rise.</summary>
    [Positive] public double StandingHeightFt { get; init; }
    /// <summary>
    /// Off the bat (§7.11): no glove takes a batted ball until it has been this far (3-D) from where it left the bat,
    /// or has come down to the ground. A ball straight off the bat into the catcher's mitt is a foul tip, not a catch;
    /// a pop behind the plate is caught coming down.
    /// </summary>
    [Positive] public double OffTheBatFt { get; init; }
    /// <summary>
    /// Still in the air for a catch (§7.6): a route that meets the ball above this before the first
    /// bounce is a catch; at or below it the hop is a scoop. Chase targeting uses the same floor.
    /// </summary>
    [Positive] public double InAirMinY { get; init; }
    public double JumpBallY { get; init; }
    public double WallBallY { get; init; }
    public double WindowBeforeSec { get; init; }
    public double WindowAfterSec { get; init; }
    public double WallSitSec { get; init; }
    public double SuperJumpWindowSec { get; init; }
    public double GrowWindowSec { get; init; }
    public double ClamberWindowSec { get; init; }
    /// <summary>Each partner must be this close to both the wall plant and the live ball in XZ.</summary>
    public double BuddyPlantFt { get; init; }
    public double BuddyJumpHoldSec { get; init; }
    public double HeldBallY { get; init; }
    public double BuddyHeldBallY { get; init; }
    public double BuddyLeapBallY { get; init; }
    public double HoverLeadSec { get; init; }
    public double HoverMinSec { get; init; }
    /// <summary>West arms the leap for this long (§8.4); longer when the play is at the wall.</summary>
    public double JumpArmSec { get; init; }
    public double WallJumpArmSec { get; init; }
    /// <summary>East arms the dive reach for this long.</summary>
    public double DiveArmSec { get; init; }
    /// <summary>
    /// What a dive costs (F693-02-dive-recovery-cost): the diver neither moves nor throws for this long after the
    /// commitment, at Field 1, caught or missed alike, player initiated — and never revoking a catch already made. The
    /// later end wins against the bobble's fumble. 0.60 s; 0 is a free dive.
    /// </summary>
    public double DiveRecoverySec { get; init; }
    /// <summary>The cost shortens by this fraction of itself per Field point above 1 (2.5 %: 0.60 at Field 1 to 0.465 at 10).</summary>
    [Chance] public double DiveRecoveryFieldCut { get; init; }
    /// <summary>
    /// The normal jump's airtime (#719, F693-02-normal-jump-*): 0 is the old jump — West arms a window of
    /// <c>jumpArmSec</c> and the body never leaves the ground; above 0 a fresh eligible West press is a takeoff with no added
    /// startup, the body is airborne this long with a root rise of <c>jumpRiseFt</c> (<c>h = 4 H u (1 − u)</c>), the same for
    /// every character, one profile per press, and a jumping catch throws only after it has landed. 0.60 s.
    /// </summary>
    public double JumpAirSec { get; init; }
    /// <summary>The root rise at the apex of the normal jump, in feet: 2.0 in both roots, read only above <c>jumpAirSec</c> 0.</summary>
    [Positive] public double JumpRiseFt { get; init; }
    /// <summary>A fresh grounded West press blocked by the read or a recovery is remembered this long, inclusive, and takes off at the first eligible instant (F693-02-normal-jump-input-buffer). 0.10 s; 0 is no buffer.</summary>
    public double JumpBufferSec { get; init; }
    /// <summary>Airborne, the body answers the stick at this fraction of its ground rates (F693-02-normal-jump-air-response-trial): 0.10 in both roots, read only above <c>jumpAirSec</c> 0.</summary>
    [Chance] public double JumpAirResponseMul { get; init; }
    /// <summary>The physical normal jump is on above <c>jumpAirSec</c> 0; at 0 the jump is the old arm window.</summary>
    public bool JumpArc => JumpAirSec > 0;
}

/// <summary>Field dash, buddy toss, kick, and the dive lunge (§8.1, §8.4, §8.7).</summary>
public sealed record FieldDashRules
{
    [Positive] public double ChaseMul { get; init; }
    public double BuddyTossFt { get; init; }
    public double KickFt { get; init; }
    public double DiveLungeFt { get; init; }
    public double ItemSmashFt { get; init; }
}

public sealed record DropRules
{
    [Chance] public double Heatball { get; init; }
    [Chance] public double PhonySwing { get; init; }
    /// <summary>The heart swing's frozen glove (a special, §13). A park's status volume no longer rolls it (F4-b, #896; FD-08-R1).</summary>
    [Chance] public double Frozen { get; init; }
}

public sealed record WallPlantRules
{
    public double InsideFenceFt { get; init; }
    [Chance] public double MinFraction { get; init; }
    public double FallbackInsideFt { get; init; }
    public double MinFt { get; init; }
}

public sealed record FieldAbilityRules
{
    public double BigCatchBonusFt { get; init; }
    /// <summary>Lick Catch / Grow reach further on the tag too (§10.3): added to running.bags.tagReachFt.</summary>
    public double TagReachBonusFt { get; init; }
    public double SuperJumpCatchBonusFt { get; init; }
    public double SuperJumpFlyRangeFt { get; init; }
    public double DiveGroundRangeFt { get; init; }
    [Positive] public double LaserMul { get; init; }
    [Positive] public double SnapThrowMul { get; init; }
    /// <summary>
    /// Snap Throw's release after a clean received teammate throw (F693-03-snap-throw, #723): 0.22 against an ordinary 0.30,
    /// with the flight boost at 1.0. A pickup, a bobble, a sail or a hand-off clears the eligibility.
    /// </summary>
    [Positive] public double SnapReleaseSec { get; init; }
    /// <summary>How far Laser is confined to a throw home with a live runner on third or the third–home segment (F693-03-laser-throw, #723): 1 is the rule — a cutoff feed never carries it; 0 is the old universal boost.</summary>
    [Chance] public double LaserHomeOnly { get; init; }
    /// <summary>
    /// Ball Dash's carry (F693-02-ball-dash-carrier, #718): a holder with the ball securely in the glove moves at this multiple
    /// of its ordinary pursuit speed, automatically — no press, no timer, no cooldown — and the CPU's carry forecast reads the
    /// same number. Only the cap moves: the response rates stay the body's own (F693-02-carry-movement-response). The role
    /// players dart, pip and jester hold it.
    /// </summary>
    [Positive] public double BallDashMul { get; init; }
}

/// <summary>
/// The one throw model (§8.5, F693-03-long-throw-numbers):
/// <c>throwSec = releaseSec + [dist / (baseFtPerSec × arm) + longThrowLossSec × (max(0, dist − range) / 80)²] / (chem × ability)</c>
/// with <c>arm = armBase + Arm × armPerField</c> and <c>range = comfortableRangeFt + rangePerArmFt × (Arm − 5)</c>.
/// It flies the ball and judges the bag, for every arm on the field, the catcher's gun included, and it
/// is the arithmetic both CPU estimates read. With <c>longThrowLossSec</c> 0 it is the old flat clock,
/// byte for byte; the game plays the accepted curve (#722). Lateral error
/// σ = (11 − Arm) × lateralSigmaPerFieldDeficitFt.
/// </summary>
public sealed record ThrowRules
{
    public double ReleaseSec { get; init; }
    [Positive] public double BaseFtPerSec { get; init; }
    [Positive] public double MinFtPerSec { get; init; }
    [Positive] public double ArmBase { get; init; }
    public double ArmPerField { get; init; }
    /// <summary>The neutral arm's comfortable range (F693-03-long-throws): inside it a throw is the flat clock; past it the flight loses pace, smoothly.</summary>
    [Positive] public double ComfortableRangeFt { get; init; }
    /// <summary>The range shifts this much per Arm point either side of the neutral arm (<see cref="InPlay.NeutralArm"/>).</summary>
    public double RangePerArmFt { get; init; }
    /// <summary>Seconds added to the flight per (feet past the range / 80)², before chemistry and ability divide it. 0 is the flat clock; the accepted trial value is 0.60.</summary>
    public double LongThrowLossSec { get; init; }
    public double LateralSigmaPerFieldDeficitFt { get; init; }
    /// <summary>A throw to an uncovered bag hangs as a lob this long for the cover; then it drops at the bag, live.</summary>
    [Positive] public double LobMaxSec { get; init; }
    /// <summary>
    /// The forced-relay ceiling (§8.7, #722): a throw longer than this always goes through the cutoff on the line, whatever
    /// the clock says, and the relay continues with the cutoff's arm. Inside it the CPU relays only when the relay arrives
    /// first by more than the rung's <c>cpu.*.relayBiasSec</c>. 9999 is never, so time alone decides;
    /// 200 beside a bias no relay can save was the old rule.
    /// </summary>
    [Positive] public double OnTheFlyFt { get; init; }
    /// <summary>A fielder holding the ball this close to a force bag steps on it instead of throwing (§10.4, S-41).</summary>
    [Positive] public double UnassistedFt { get; init; }
    public double HandHeightFt { get; init; }
    public double BagHeightFt { get; init; }
    /// <summary>A human's cutoff throws the armed onward leg for them (§8.7): 0 is F693-03-relay-ownership (#723), where each relay leg needs its own command; 1 is the old automatic leg.</summary>
    [Chance] public double RelayAutoContinue { get; init; }
    /// <summary>An early onward-throw press is remembered this long of active play and fires when the receiver has the ball (F693-03-input-buffer, #723). 0.25 s; 0 is no queue.</summary>
    public double RelayBufferSec { get; init; }
}

/// <summary>The CPU catcher's release on a steal (§11.3): <c>base − Field × perField ± noise / 2</c>, clamped, × the difficulty's reaction. The gun itself is the one throw model (<see cref="ThrowRules"/>); the out is the tag at the bag (§10.3).</summary>
public sealed record CatcherRules
{
    /// <summary>Where the catcher holds the ball at the crossing (§11.3): this far behind the plate, so the gun is a throw from the plate, not from the SET crouch spot.</summary>
    [Positive] public double BehindPlateFt { get; init; }
    public double CpuReleaseBaseSec { get; init; }
    public double CpuReleasePerField { get; init; }
    public double CpuReleaseNoiseSec { get; init; }
    public double CpuReleaseMinSec { get; init; }
    public double CpuReleaseMaxSec { get; init; }
}

/// <summary>
/// Chemistry on a throw (§8.5): good is faster; bad has a chance of a slanted throw — slower and
/// off the cover by a lateral miss the receiver cannot reach — and flies at <see cref="BadSpeedMul"/>
/// otherwise. The roll is on the input; the outcome is still the ball missing the cover. The game
/// plays F693-03-negative-chemistry — 0.90 and no slant, slow rather than random (#722); a slant chance above 0 and a bad
/// speed of 1.0 is the old random throw.
/// </summary>
public sealed record ThrowChemistryRules
{
    [Positive] public double GoodSpeedMul { get; init; }
    /// <summary>A bad pair's unslanted throw flies at this multiple of the arm's speed; 1.0 is today's ordinary throw.</summary>
    [Positive] public double BadSpeedMul { get; init; }
    [Chance] public double SlantChance { get; init; }
    [Positive] public double SlantSpeedMul { get; init; }
    [Positive] public double SlantLateralMinFt { get; init; }
    [Positive] public double SlantLateralMaxFt { get; init; }
}

public sealed record BobbleRules
{
    public double MinEnergy { get; init; }
    public double HandsPerGloveReduction { get; init; }
    [Positive] public double EnergySpan { get; init; }
    public double ChancePerHands { get; init; }
    [Chance] public double MaxChance { get; init; }
    public double FumbleSec { get; init; }
    public double ScatterFt { get; init; }
    public double ScatterFtPerSec { get; init; }
    public double ScatterDropFtPerSec { get; init; }
    public double ScatterBallY { get; init; }
    public double RecoilFtPerSec { get; init; }
}

/// <summary>
/// The old recoil: a stop read off the contact's energy and the Hands deficit, the whole tick held for it. Read only
/// while <see cref="RecoilRules.OnsetFtPerSec"/> is 0; above it the ordinary impact recoil (<see cref="RecoilRules"/>) replaces it.
/// </summary>
public sealed record KnockbackRules
{
    public double MinEnergy { get; init; }
    public double SecPerFieldDeficit { get; init; }
    [Positive] public double EnergySpan { get; init; }
    public double MaxSec { get; init; }
    public double MinSec { get; init; }
}

/// <summary>
/// What the ball costs the hands that take it (#720: F693-02-ground-pickup-recoil-basis, -ground-pickup-recoil-cap,
/// -recoil-field-shaping, -recoil-field-factors, -recoil-severity-curve, -ordinary-recoil-actions, -ordinary-recoil-displacement,
/// -ordinary-recoil-distance-cap, -ordinary-recoil-motion-profile). At <c>onsetFtPerSec</c> 0 the recoil is the old one
/// (<see cref="KnockbackRules"/>). Above 0 it is read off the ball's actual incoming speed the frame before the
/// take, deterministic: severity <c>S = clamp((v − onset) / (full − onset), 0, 1)</c>, the weight <c>w = S × (1 −
/// handsCutPerPoint × (Hands − 1))</c>, the recovery <c>capSec × w</c>, an impact kick of <c>kickFtPerSec × w</c> along the
/// ball's horizontal travel slowing linearly to rest over the recovery (<c>w²</c> feet at the accepted 10 ft/s and 0.20 s).
/// A routine arrival at or below the onset costs nothing. The body's steering and throw start wait; possession and the contact
/// at the bag do not. The game plays the measured anchors, 55 / 75.
/// </summary>
public sealed record RecoilRules
{
    public double OnsetFtPerSec { get; init; }
    public double FullFtPerSec { get; init; }
    /// <summary>
    /// The airborne pair (F693-02-grounded-air-catch-recoil, #720 slice 2): a hard batted ball caught in the air by a body on its
    /// feet — not diving, not jumping, not a buddy leap — costs the same response off these anchors. 0 = no airborne recoil; the
    /// game plays the measured 80 / 115, a distinct pair because the ground pair would charge every liner.
    /// </summary>
    public double AirOnsetFtPerSec { get; init; }
    public double AirFullFtPerSec { get; init; }
    [Positive] public double CapSec { get; init; }
    [Chance] public double HandsCutPerPoint { get; init; }
    public double KickFtPerSec { get; init; }
    /// <summary>The speed-read recoil is on; the old knockback is not read.</summary>
    public bool Active => OnsetFtPerSec > 0;
    /// <summary>A hard catch in the air by a grounded body recoils.</summary>
    public bool AirActive => AirOnsetFtPerSec > 0;
}

/// <summary>
/// Where an ordinary play is allowed to go wrong (#721: F693-02-handling-error-opportunities, -ordinary-handling-error-chance,
/// -ordinary-handling-error-cap, -ordinary-handling-chance-curve, -awkward-hop-difficulty-source, -ordinary-bobble-outcome,
/// -bobble-stun, -bobble-stun-duration, -bobble-recovery-reliability, -bobble-direction-spread, -uniform-error-direction,
/// -local-bobble-*). At <c>awkwardHop</c> 0 the bobble is the old one (<see cref="BobbleRules"/>: a roll off
/// the contact's energy on every grounder take, a whole-tick fumble). Above 0 a legal routine pickup never rolls: the one
/// difficulty is the awkward in-between hop at the take — the ball rising off a real bounce, mid-way up a hop tall enough to
/// matter — and its chance is <c>chanceCap × D × (1 − handsCut × H)</c> with D the normalized difficulty and H the normalized
/// Hands. A failed take is a local bobble: the ball spills down from the contact with a fifth of its horizontal speed (six ft/s
/// at most) inside ±<c>bobbleSpreadDeg</c> of its travel, rebounds under a <c>bobbleReboundCapFt</c> ceiling, settles below
/// <c>bobbleSettleFt</c>, and takes its restitution, the roll it keeps per impact and its rolling deceleration from the ground row
/// it is on (<see cref="GroundBobbleRules"/>, F3-c); the fumbler is stunned <c>stunSec</c> — no steering, no take — while the ball and every other
/// body stay live, and the recovery never rolls again. Or (F693-02-expanded-ordinary-error-outcomes, -error-outcome-selection,
/// -continuing-error-*) the ball gets past: how squarely the ring met the ball — the obstruction, 1 at the body, 0 at the edge of
/// the take — and the ball's speed decide, never a second roll. Below <c>deflectObstruction</c> and at or above
/// <c>deflectMinFtPerSec</c> coming in, the ball carries on with <c>deflectRetainMax</c> → <c>deflectRetainMin</c> of its horizontal
/// and signed vertical speed inside ±<c>deflectSpreadDeg</c> of its travel on the shared batted-ball ground physics; the same stun,
/// the same reliable recovery, another body's if it reaches it first.
/// </summary>
public sealed record HandlingRules
{
    [Chance] public double AwkwardHop { get; init; }
    [Chance] public double ChanceCap { get; init; }
    [Chance] public double HandsCut { get; init; }
    /// <summary>A hop whose apex is below this is a micro-bounce and never awkward; the difficulty grows to full at <see cref="HopFullApexFt"/>.</summary>
    public double HopMinApexFt { get; init; }
    public double HopFullApexFt { get; init; }
    /// <summary>The phase band: D is 1 at the middle of the rise (φ = 0.5) and falls linearly to 0 this far either side — the clean short hop below, the clean long hop above.</summary>
    [Positive] public double HopPhaseHalfWidth { get; init; }
    [Positive] public double StunSec { get; init; }
    public double BobbleSpreadDeg { get; init; }
    [Chance] public double BobbleRetain { get; init; }
    public double BobbleCapFtPerSec { get; init; }
    public double BobbleReboundCapFt { get; init; }
    public double BobbleSettleFt { get; init; }
    public double DeflectSpreadDeg { get; init; }
    [Chance] public double DeflectRetainMax { get; init; }
    [Chance] public double DeflectRetainMin { get; init; }
    /// <summary>The obstruction at and above which a failed take is a knockdown (the local bobble); below it the ball glances on.</summary>
    [Chance] public double DeflectObstruction { get; init; }
    /// <summary>A glancing touch sends the ball on only when it came in at least this fast — the hot ball of the recoil's onset; a slower one drops at the feet.</summary>
    public double DeflectMinFtPerSec { get; init; }
    /// <summary>The routine pickup never rolls; the awkward hop is the one difficulty.</summary>
    public bool Active => AwkwardHop > 0;
}

public sealed record ParkHazardRules
{
    /// <summary>
    /// The one number here that is not a park hazard's: a shell or cask star swing flags a grounder
    /// warped with no can in the park at all (<c>Fielding.cs</c>). The hazard types' own numbers
    /// moved to <c>hazards.json</c> with the pattern library (#847).
    /// </summary>
    [Chance] public double ShellWarpChance { get; init; }
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
/// <b>Every number here is the number that shipped, but one.</b> #847 moved <c>fielding.park.emberNightFireMul</c>
/// (1.6) under <c>fireBreath</c> and <c>fielding.park.pipeReachPadFt</c> (8, now 5.6 on the fence scale)
/// under <c>warpPipe</c> and <c>barrel</c>; it chose neither, and #730 / #732 still own them. What a
/// status volume costs a body stays <c>fielding.chase.frozenMul</c> — a star swing sets the same slow
/// and the specials are outside this phase — and the billboard's payout stays <c>stars.gains.billboard</c>.
/// The one new number is each status volume's <c>slowSec</c> (3.0, F4-b, #896): how long a touch slows
/// the body that made it. It is Jack's number (FD-08-R2, "slows for 3 seconds", September 22, 2026) on
/// both roots, and it is not trial-accepted until he has played it.
/// </para>
/// </summary>
public sealed record HazardRules
{
    /// <summary>Crystal's freezers: a body that touches the disc runs slowed for <see cref="HazardTypeRules.SlowSec"/>.</summary>
    public HazardTypeRules FreezeVolume { get; init; } = new();

    /// <summary>Ember's lava, the freezer's twin.</summary>
    public HazardTypeRules LavaPit { get; init; } = new();

    /// <summary>Ember's breath: the one volume night widens.</summary>
    public HazardTypeRules FireBreath { get; init; } = new();

    /// <summary>Funfair's warp cans: a ball that enters one comes out of another, live (F4-c).</summary>
    public HazardTypeRules WarpPipe { get; init; } = new();

    /// <summary>Canopy's barrel cannons, the warp can's twin, a little hotter out of the exit.</summary>
    public HazardTypeRules Barrel { get; init; } = new();

    /// <summary>Rooftop's star signs: a ball whose live path passes under the top inside the disc pays the batting team (F4-c).</summary>
    public HazardTypeRules Billboard { get; init; } = new();

    /// <summary>Canopy's climbable wall: a Clamber fielder's reach and rob.</summary>
    public HazardTypeRules ClimbWall { get; init; } = new();

    /// <summary>
    /// Funfair's mouths (FD-09-R2, F4-c): a ball redirect. A ball that comes into one up to <c>mouthFt</c> is spat out
    /// of another; nobody is out by a mouth. Night-only because Funfair authors them in its night block (FD-11, F4-d).
    /// </summary>
    public HazardTypeRules Chomper { get; init; } = new();

    /// <summary>Ember's captain statue: a solid body (F4-f).</summary>
    public HazardTypeRules Statue { get; init; } = new();

    /// <summary>Funfair's train: a solid body that runs along the fence on the play clock (F4-f).</summary>
    public HazardTypeRules Train { get; init; } = new();

    /// <summary>Rooftop's air-conditioning unit: a solid body (F4-f).</summary>
    public HazardTypeRules AcUnit { get; init; } = new();

    /// <summary>Canopy's trees: solid bodies (F4-f).</summary>
    public HazardTypeRules Tree { get; init; } = new();

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

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<HazardRules, IReadOnlyList<string>> AuthoredCache = new();

    /// <summary>
    /// The authored ids in library order (<see cref="HazardType.All"/>). Built once per table and
    /// then handed out: the park validator asks for it on every load. Cached by instance, not in a
    /// field, so a <c>with</c> copy that changes a row gets its own answer.
    /// </summary>
    public IReadOnlyList<string> Authored => AuthoredCache.GetValue(this, static t => HazardType.All.Where(t.IsAuthored).ToList());

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
    /// <item>A <c>statusVolume</c> says how long a touch slows a body (<c>slowSec</c>, FD-08-R2), and no other
    /// pattern may: a volume with no time would slow nobody or everybody forever, and a time on a row that
    /// never slows anyone is the same dead rule.</item>
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
            // How long a touch slows a body (FD-08-R2, F4-b): every status volume says it, and nothing else may,
            // because no other pattern reads it. Its range ([Positive]) is the reflective walk's.
            if (row.Pattern == HazardPattern.StatusVolume && row.SlowSec is null)
                errors.Add($"{source}: {key} is a {HazardPattern.StatusVolume} and must author slowSec, how long a "
                           + "touch slows the body that made it (FD-08-R2)");
            if (row.SlowSec is { } slow && row.Pattern != HazardPattern.StatusVolume)
                errors.Add($"{source}: {key}.slowSec is {slow}, but only a {HazardPattern.StatusVolume} slows a body; "
                           + "give the row that pattern or leave slowSec out");
            // The live redirect (F4-c): every redirect says how high its mouth takes a ball and how the ball leaves the exit,
            // and nothing else may. The reward target says how high its sign reaches.
            var redirect = row.Pattern == HazardPattern.BallRedirect;
            foreach (var (name, value) in new[] { ("mouthFt", row.MouthFt), ("exitSpeedMul", row.ExitSpeedMul), ("exitVyFtPerSec", row.ExitVyFtPerSec) })
            {
                if (redirect && value is null)
                    errors.Add($"{source}: {key} is a {HazardPattern.BallRedirect} and must author {name} (F4-c)");
                if (!redirect && value is { } v)
                    errors.Add($"{source}: {key}.{name} is {v}, but only a {HazardPattern.BallRedirect} reads it; "
                               + $"give the row that pattern or leave {name} out");
            }
            if (row.MouthFloorFt is { } floor && (!redirect || row.MouthFt is not { } mouthTop || floor >= mouthTop))
                errors.Add($"{source}: {key}.mouthFloorFt is {floor}, but it is read only on a {HazardPattern.BallRedirect}, "
                           + "below its mouthFt");
            // A solid body and a timed mover (F4-f): both say how tall they stand and how they give the ball back; a mover
            // also says its clock and its run. Nothing else reads these.
            var solid = row.Pattern is HazardPattern.SolidBody or HazardPattern.TimedMover;
            var mover = row.Pattern == HazardPattern.TimedMover;
            foreach (var (name, value, needed) in new[] { ("heightFt", row.HeightFt, solid), ("restitution", row.Restitution, solid),
                         ("periodSec", row.PeriodSec, mover), ("travelFt", row.TravelFt, mover) })
            {
                if (needed && value is null)
                    errors.Add($"{source}: {key} is a {row.Pattern} and must author {name} (F4-f)");
                if (!needed && value is { } v)
                    errors.Add($"{source}: {key}.{name} is {v}, but a {row.Pattern} does not read it; leave {name} out");
            }
            if (row.Restitution is > 1)
                errors.Add($"{source}: {key}.restitution must be between 0 and 1; got {row.Restitution}");
            if (row.Pattern == HazardPattern.RewardTarget && row.TopFt is null)
                errors.Add($"{source}: {key} is a {HazardPattern.RewardTarget} and must author topFt, the top of the sign (F4-c)");
            if (row.TopFt is { } top && row.Pattern != HazardPattern.RewardTarget)
                errors.Add($"{source}: {key}.topFt is {top}, but only a {HazardPattern.RewardTarget} reads it; "
                           + "give the row that pattern or leave topFt out");
        }
    }
}

/// <summary>
/// One hazard type's row (§14, §16). The defaults are an inert drawn prop — the honest answer for a
/// type whose pattern nobody has built — so a row that omits a field reads as scenery rather than as
/// a broken volume.
/// </summary>
public sealed record HazardTypeRules
{
    /// <summary>Which of <see cref="HazardPattern"/> the sim runs for this type.</summary>
    public string Pattern { get; init; } = "";

    /// <summary>
    /// A <c>statusVolume</c>'s disc at night, as a multiple of the radius the park authored. 1 is a
    /// disc night does not widen, which is every volume but Ember's breath.
    /// </summary>
    [Positive] public double NightRadiusMul { get; init; }

    /// <summary>
    /// How far outside its own radius a <c>ballRedirect</c> still takes a grounder. The pad is the
    /// larger part of the capture disc (#732), not a rounding allowance.
    /// </summary>
    public double ReachPadFt { get; init; }

    /// <summary>
    /// A <c>statusVolume</c>'s touch (FR-07, FD-08-R2; F4-b, #896): a body that enters the disc runs at
    /// <c>fielding.chase.frozenMul</c> for this many seconds from the touch; staying inside keeps it slowed,
    /// and leaving and entering again starts the time again. Every status volume authors it and no other
    /// pattern may, so it is absent (null) on every row but the three volumes. 3.0 on both roots is Jack's
    /// number (FD-08-R2), not yet played.
    /// </summary>
    [Optional, Positive] public double? SlowSec { get; init; }

    /// <summary>A <c>ballRedirect</c>'s mouth (F4-c): a ball inside the disc at or below this height enters it.</summary>
    [Optional, Positive] public double? MouthFt { get; init; }

    /// <summary>
    /// A <c>ballRedirect</c>'s mouth floor (F4-c): a ball under this height passes beneath it. Absent is the ground (a can or a
    /// barrel takes a ball rolling in); a chomper's head stands up off the grass, so it takes a fly on its way down, not a grounder.
    /// </summary>
    [Optional, Positive] public double? MouthFloorFt { get; init; }

    /// <summary>A <c>ballRedirect</c>'s exit (F4-c): the share of the ball's horizontal speed it leaves with, on the same heading.</summary>
    [Optional, Positive] public double? ExitSpeedMul { get; init; }

    /// <summary>A <c>ballRedirect</c>'s exit (F4-c): the vertical speed, ft/s up, the ball leaves with.</summary>
    [Optional, Positive] public double? ExitVyFtPerSec { get; init; }

    /// <summary>A <c>rewardTarget</c>'s sign (F4-c): a ball inside the disc at or below this height hits it.</summary>
    [Optional, Positive] public double? TopFt { get; init; }

    /// <summary>A <c>solidBody</c> or <c>timedMover</c> (F4-f): how tall it stands; a ball over it passes.</summary>
    [Optional, Positive] public double? HeightFt { get; init; }

    /// <summary>A <c>solidBody</c> or <c>timedMover</c> (F4-f): the share of the ball's speed into the body it keeps off it.</summary>
    [Optional, Positive] public double? Restitution { get; init; }

    /// <summary>A <c>timedMover</c> (F4-f): seconds for one run out and back along the fence.</summary>
    [Optional, Positive] public double? PeriodSec { get; init; }

    /// <summary>A <c>timedMover</c> (F4-f): how far to either side of its authored spot it runs, along the fence.</summary>
    [Optional, Positive] public double? TravelFt { get; init; }
}

// ---------------------------------------------------------------------------------------
// running.json — §9, §10.2, §10.3, §10.6, §11.2, §11.6
// ---------------------------------------------------------------------------------------

public sealed record RunningRules
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
public sealed record BagSecRules
{
    [Positive] public double BaseSec { get; init; }
    public double SecPerRun { get; init; }
    [Positive] public double MinSec { get; init; }
    [Positive] public double MaxSec { get; init; }
    [Chance] public double DashMul { get; init; }
    /// <summary>The batter-runner leaves the box this long after contact (reference: 31 frames).</summary>
    public double BatterStartSec { get; init; }
    /// <summary>A trailing runner stops this far behind the runner ahead; runners never pass (§9.1).</summary>
    [Positive] public double NoPassFt { get; init; }
}

public sealed record BagRules
{
    [Positive] public double OccupyRadiusFt { get; init; }
    /// <summary>A glove with the ball inside this of a runner's body is the tag (§10.3); Lick / Grow add fielding.abilities.tagReachBonusFt.</summary>
    [Positive] public double TagReachFt { get; init; }
    /// <summary>A runner inside this of a bag is touching it: safe from the tag. Inside the slid reach so a slide never closes the window.</summary>
    [Positive] public double TagSafeRadiusFt { get; init; }
    [Positive] public double TimeOnBagSec { get; init; }
    /// <summary>Inside this of the end of a segment the runner is placed on the bag.</summary>
    public double SnapFt { get; init; }
    /// <summary>A runner stopping at the next bag slides over its last feet when a tag is threatened (§9.4).</summary>
    [Positive] public double SlideFt { get; init; }
    /// <summary>A tag is threatened when a glove with the ball is inside this of the bag, or a throw is armed there.</summary>
    [Positive] public double SlideThreatFt { get; init; }
    /// <summary>A slide shrinks the tag reach by this much; it does not change the arrival (§9.4).</summary>
    public double SlideReachCutFt { get; init; }
    /// <summary>The batter-runner runs through first by this much and comes straight back, safe from the tag on the way (§9.4).</summary>
    [Positive] public double OverrunFt { get; init; }
}

public sealed record ClosePlayRules
{
    /// <summary>
    /// The close play is geometric (§9.6, D5): the ball at a tag bag ahead of the runner by no more than
    /// this runs the mash; the runner in ahead of the ball by no more than this pops the small SAFE.
    /// Outside it the geometry decides silently.
    /// </summary>
    [Positive] public double MarginSec { get; init; }
    public double IconDelaySec { get; init; }
    public double CpuReactionBaseSec { get; init; }
    public double CpuReactionPerStat { get; init; }
}

/// <summary>
/// The steal jump (§11.2, D2): an armed runner breaks at release and runs at <see cref="AirSpeedMul"/> of
/// their speed while the pitch is in the air; armed inside the first <see cref="PerfectWindowSec"/> of the
/// windup is a perfect steal that breaks <see cref="PerfectEarlySec"/> before release. There is no time
/// credit: the body is placed by the clock and the tag decides (§10.3).
/// </summary>
public sealed record StealRules
{
    /// <summary>Speed fraction of an armed runner between the break and the ball reaching the plate.</summary>
    [Chance] public double AirSpeedMul { get; init; }
    /// <summary>A perfect steal breaks this long before release.</summary>
    [Positive] public double PerfectEarlySec { get; init; }
    /// <summary>Arming inside this many seconds of the windup is the perfect steal.</summary>
    [Positive] public double PerfectWindowSec { get; init; }
    /// <summary>The CPU catcher throws on the trailing runner of a double steal only when that margin is better by this (§11.3).</summary>
    [Positive] public double CpuTrailPreferSec { get; init; }
    /// <summary>With a runner on third watching a steal of second, the free middle infielder cuts this far in front of the bag on the throw line (S-66).</summary>
    [Positive] public double CutInFrontFt { get; init; }
}

/// <summary>
/// The rundown (§9.7): a runner off the bags with a glove holding the ball inside <see cref="RangeFt"/>.
/// The CPU glove runs at them and throws ahead at the last makeable moment (the §8.8 margin, no fixed
/// distance); a throw that races nobody to its bag, with every moving body at least
/// <see cref="LazyLobFraction"/> of the way to a bag, is a lazy lob at <see cref="LazyLobSpeedMul"/> of the arm.
/// </summary>
public sealed record RundownRules
{
    [Positive] public double RangeFt { get; init; }
    [Chance] public double LazyLobFraction { get; init; }
    [Positive] public double LazyLobSpeedMul { get; init; }
}

/// <summary>Mash South to dash (§9.4): each press adds this, to the cap.</summary>
public sealed record DashRules
{
    [Chance] public double PerPress { get; init; }
    [Chance] public double MaxDash { get; init; }
}

public sealed record RunStickRules
{
    /// <summary>Squared stick magnitude below which the diamond stick names no bag.</summary>
    [Chance] public double DiamondDeadMag2 { get; init; }
}

/// <summary>
/// The CPU baserunner (spec §9.9): decisions at contact, at every fielder touch, at every throw
/// release and at every bag, from <c>margin(next) = throwArrival(next) − runnerArrival(next)</c>.
/// The steal roll is still the SET roll as shipped; §11.6 moves it to the runner AI (P6).
/// </summary>
public sealed record CpuRunnerRules
{
    /// <summary>The defense's reaction before a throw in the runner's estimate.</summary>
    public double ReactionSec { get; init; }
    /// <summary>Unforced on a grounder to the infield: go if margin(next) is at least this and the ball is not in front.</summary>
    public double GroundGoMarginSec { get; init; }
    /// <summary>Runner on third, grounder, fewer than two outs: go if the infield is back (the fielder meets it at or behind his own depth) or margin(home) is at least this.</summary>
    public double ThirdHomeMarginSec { get; init; }
    /// <summary>Hit to the outfield: go if margin(next) is at least outfieldGoSec − Run × outfieldGoPerRunSec.</summary>
    public double OutfieldGoSec { get; init; }
    public double OutfieldGoPerRunSec { get; init; }
    /// <summary>With two outs the outfield threshold drops by this (go more).</summary>
    public double TwoOutsGoBonusSec { get; init; }
    /// <summary>The batter-runner rounds first for second if margin(2B) is at least roundFirstSec − Run × roundFirstPerRunSec.</summary>
    public double RoundFirstSec { get; init; }
    public double RoundFirstPerRunSec { get; init; }
    /// <summary>Trailing by at least this in the last inning: every threshold drops by desperateSec.</summary>
    public int DesperateTrailRuns { get; init; }
    public double DesperateSec { get; init; }
    /// <summary>A runner already this far along a segment keeps going rather than turning back.</summary>
    [Chance] public double CommitFraction { get; init; }
    /// <summary>Runner on third tags on a caught fly this deep with fewer than two outs. 9999 is never: the runner races instead (#732).</summary>
    public double TagThirdMinCarryFt { get; init; }
    /// <summary>Runner on second tags for third on a caught fly to right this deep. 9999 is never: the runner races instead (#732).</summary>
    public double TagSecondMinCarryFt { get; init; }
    /// <summary>
    /// The race from third (#732, decision 5 of #730): at the catch, the runner goes home when <c>margin(home)</c> — the
    /// defense's estimated arrival minus the runner's — clears this plus the rung's <c>runnerMarginSec</c>. 99 is a margin
    /// no play reaches, so a table at 99 decides by the carry gate alone.
    /// </summary>
    [Signed] public double TagUpHomeMarginSec { get; init; }
    /// <summary>The race from second to third at the catch, on the same terms as <see cref="TagUpHomeMarginSec"/>.</summary>
    [Signed] public double TagUpThirdMarginSec { get; init; }
    /// <summary>The CPU steal table (§11.6): base chance by Run, 0 at or below <see cref="StealMinRun"/>, linear between the anchors.</summary>
    public int StealMinRun { get; init; }
    [Chance] public double StealBaseRun6 { get; init; }
    [Chance] public double StealBaseRun8 { get; init; }
    [Chance] public double StealBaseRun10 { get; init; }
    /// <summary>× with two outs.</summary>
    [Positive] public double StealTwoOutsMul { get; init; }
    /// <summary>× with the captain slugger at the plate.</summary>
    [Positive] public double StealCaptainUpMul { get; init; }
    /// <summary>No steal when trailing by at least this many runs.</summary>
    public int StealTrailingRuns { get; init; }
    /// <summary>The CPU pitcher's pickoff chance (cpu.*.pickoffChance) is multiplied by this when it sees a steal pip armed in SET (§4.5, D3).</summary>
    [Positive] public double PickoffSeenArmMul { get; init; }
    /// <summary>On first-and-third the CPU pickoff goes to first this often, else the lead runner (§4.8).</summary>
    [Chance] public double PickoffFirstOnCornersChance { get; init; }
}

// ---------------------------------------------------------------------------------------
// stars.json — §12, §13
// ---------------------------------------------------------------------------------------

public sealed record StarRules
{
    [Positive] public double MeterMax { get; init; }
    public StarGainRules Gains { get; init; } = new();
    /// <summary>The price of each cost tier (§12, PH-16-R7): every Star Pitch and Star Swing names one in star-skills.json.</summary>
    public StarTierRules Tiers { get; init; } = new();
    public StarCostRules Costs { get; init; } = new();
    /// <summary>
    /// The Stars each team's one pool holds at the first pitch (§12, PH-16-R6, PH-16-R16): the same for both teams,
    /// whatever they drafted. Chemistry does not set it. It must buy the cheapest tier and fit the meter.
    /// </summary>
    public int StartingReserve { get; init; }
    public MvpRules Mvp { get; init; } = new();

    internal void Validate(string source, List<string> errors)
    {
        Tiers.Validate(MeterMax, source, errors);
        if (StartingReserve > MeterMax)
            errors.Add($"{source}: stars.startingReserve must fit the meter (meterMax {MeterMax}); got {StartingReserve}");
        // A usable reserve (PH-16-R6): at least the cheapest special is affordable from the first plate appearance.
        if (StartingReserve < Tiers.Low)
            errors.Add($"{source}: stars.startingReserve must buy the cheapest tier (low {Tiers.Low}); got {StartingReserve}");
    }
}

/// <summary>
/// What each team's pool earns (§12, PH-16-R5). <see cref="PlateAppearance"/> is the base: both teams, once, when a
/// plate appearance completes. Every other row is a bonus to the side whose play it was, stacking on the base.
/// </summary>
public sealed record StarGainRules
{
    /// <summary>Both teams, once per completed plate appearance (Match.NextBatter). A half that ends mid-appearance earns none.</summary>
    public double PlateAppearance { get; init; }
    public double Strikeout { get; init; }
    public double HomeRun { get; init; }
    public double ExtraBaseHit { get; init; }
    public double Single { get; init; }
    /// <summary>Fly or ground out resolved by the resolver.</summary>
    public double Out { get; init; }
    /// <summary>An out made live: a force, a tag, a caught runner.</summary>
    public double LiveOut { get; init; }
    public double StolenBase { get; init; }
    public double Billboard { get; init; }
    /// <summary>Two or more outs on one live ball (§10.4, §12).</summary>
    public double DoublePlay { get; init; }
    /// <summary>A leap that takes a ball clearing the fence (§8.4, §12).</summary>
    public double RobbedHomer { get; init; }
}

/// <summary>
/// The MVP point table (§12, <c>stars.json</c> <c>mvp</c>). A walk-off hit names its hitter ahead of the
/// points; otherwise the most points on either roster. Read by <see cref="Match.Mvp"/>, never literals.
/// </summary>
public sealed record MvpRules
{
    public int HomeRun { get; init; }
    /// <summary>The arm on the mound for the winner when it took the lead for the last time.</summary>
    public int WinningPitcher { get; init; }
    /// <summary>The run batted in that put the offense ahead, on top of the RBI.</summary>
    public int GoAheadRbi { get; init; }
    /// <summary>A robbed homer, a buddy jump, or a climb at the wall.</summary>
    public int RobbedHomer { get; init; }
    public int Strikeout { get; init; }
    public int Rbi { get; init; }
    public int Hit { get; init; }
    public int Walk { get; init; }
    public int HitByPitch { get; init; }
    public int StolenBase { get; init; }
    /// <summary>The seat that won a close play at the bag (§9.6): the runner safe, or the glove that tagged.</summary>
    public int ClosePlayWon { get; init; }
    /// <summary>A thrown item landed and the batter reached (§12).</summary>
    public int ItemMattered { get; init; }
    /// <summary>A putout made live: the glove that forced, tagged, or caught the runner (also the catcher on a caught stealing).</summary>
    public int PutOut { get; init; }
    /// <summary>The MVP line reads "took over the diamond" at or above this many points.</summary>
    public int TookOverAt { get; init; }
    /// <summary>"kept the line moving" at or above this; under it "did the little things".</summary>
    public int KeptMovingAt { get; init; }
}

/// <summary>
/// The Star cost tiers (§12, PH-16-R7, PH-16-R8): a small, fixed set of named prices. Each ability in
/// <c>data/abilities/star-skills.json</c> names its tier; <see cref="Top"/> is the highest, and only a captain may
/// carry a top-tier ability (the content validator refuses anything else).
/// </summary>
public sealed record StarTierRules
{
    public const string LowId = "low";
    public const string MidId = "mid";
    public const string TopId = "top";

    /// <summary>Every tier id, cheapest first.</summary>
    public static readonly IReadOnlyList<string> Ids = [LowId, MidId, TopId];

    public static bool IsTier(string? id) => id is not null && Ids.Contains(id, StringComparer.Ordinal);

    public int Low { get; init; }
    public int Mid { get; init; }
    public int Top { get; init; }

    /// <summary>The price of <paramref name="tier"/>. An unknown tier never loads (the content validator), so the fallback is only for a character with no ability.</summary>
    public int Of(string? tier) => tier switch
    {
        TopId => Top,
        MidId => Mid,
        _ => Low
    };

    internal void Validate(double meterMax, string source, List<string> errors)
    {
        foreach (var (name, cost) in new[] { (LowId, Low), (MidId, Mid), (TopId, Top) })
        {
            // A free special is not a resource (PH-16 acceptance), and one the meter cannot hold is never usable.
            if (cost < 1)
                errors.Add($"{source}: stars.tiers.{name} must cost at least 1; got {cost}");
            else if (cost > meterMax)
                errors.Add($"{source}: stars.tiers.{name} must fit the meter (meterMax {meterMax}); got {cost}");
        }
        // The top tier is the captains' (PH-16-R8), so it has to be the highest price.
        if (Low > Mid || Mid > Top)
            errors.Add($"{source}: stars.tiers must not get cheaper up the ladder; got low {Low}, mid {Mid}, top {Top}");
    }
}

public sealed record StarCostRules
{
    /// <summary>
    /// Added to the ability's tier price when a captain throws or swings for a team he does not captain: a guest
    /// captain from the draft, or a captain swapped onto the mound (§4.7).
    /// </summary>
    public int GuestCaptainSurcharge { get; init; }
}

// ---------------------------------------------------------------------------------------
// cpu.json — difficulty multipliers over the tables above (§4.8, §5.9, §8.8, §11.6)
// ---------------------------------------------------------------------------------------

public sealed record CpuRules
{
    /// <summary>The ladder rung in play. Normal is ×1 everywhere so the tables above read as written.</summary>
    public string Level { get; init; } = "";
    public CpuLevelRules Easy { get; init; } = new();
    public CpuLevelRules Normal { get; init; } = new();
    public CpuLevelRules Hard { get; init; } = new();

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
        this with { Level = IsLevel(level) ? level.ToLowerInvariant() : Level };

    internal void Validate(string source, List<string> errors)
    {
        if (Level.ToLowerInvariant() is not ("easy" or "normal" or "hard"))
            errors.Add($"{source}: cpu.level must be one of [easy, hard, normal]; got '{Level}'");
    }
}

public sealed record CpuLevelRules
{
    /// <summary>Multiplies the CPU batter's timing-error σ (§5.9).</summary>
    [Positive] public double TimingSigmaMul { get; init; }
    /// <summary>Multiplies CPU reaction and release delays (§8.8, §9.6, §11.3).</summary>
    [Positive] public double ReactionMul { get; init; }
    /// <summary>Multiplies the CPU batter's mistrack chances (§5.9).</summary>
    [Positive] public double MistrackMul { get; init; }
    /// <summary>Margin a CPU fielder needs to call a play makeable (§8.8). Read by P4.</summary>
    public double MakeableMarginSec { get; init; }
    /// <summary>Perfect-steal chance (§11.6): the CPU runner arms inside the window instead of in SET.</summary>
    [Chance] public double PerfectStealChance { get; init; }
    /// <summary>Pickoff attempt chance per SET with a runner on (§4.5, §4.8); × running.cpu.pickoffSeenArmMul when a pip is armed.</summary>
    [Chance] public double PickoffChance { get; init; }
    /// <summary>Added to every CPU baserunner threshold (§9.9): easy hesitates, hard goes.</summary>
    [Signed] public double RunnerMarginSec { get; init; }
    /// <summary>
    /// The CPU fielder throws through the cutoff only when the relay beats the direct throw by more than this (§8.7, #722) —
    /// the total-time read, graded by rung. 99, a value no relay can save, leaves <see cref="ThrowRules.OnTheFlyFt"/> as the
    /// only reason to relay.
    /// </summary>
    public double RelayBiasSec { get; init; }
    /// <summary>How much of the thrower's real arm and ability the CPU runner reads (§9.9): 0 assumes the neutral arm; 1 reads the arm the ball will fly on.</summary>
    [Chance] public double RunnerReadsArm { get; init; }
    /// <summary>How much of the fielder's relay the CPU runner reads: 0 assumes a direct throw; 1 reads the leg the fielder will actually take.</summary>
    [Chance] public double RunnerReadsRelay { get; init; }
    /// <summary>How much of the pair chemistry the CPU forecasts, fielder and runner alike (F693-03-good-chemistry): 0 ignores it, 1 is the deterministic pair factor, never a sampled roll.</summary>
    [Chance] public double ReadsChemistry { get; init; }
}
