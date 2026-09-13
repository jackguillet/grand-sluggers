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
        ["match", "pitching", "batting", "flight", "fielding", "running", "stars", "cpu"];

    public MatchRules Match { get; init; } = new();

    public PitchingRules Pitching { get; init; } = new();
    public BattingRules Batting { get; init; } = new();
    public FlightRules Flight { get; init; } = new();
    public FieldingRules Fielding { get; init; } = new();
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
            Fielding = Fielding, Running = Running, Stars = Stars, Cpu = Cpu.AtLevel(level)
        };
    }

    public static RulesTable Load(string dataRoot)
    {
        var errors = new List<string>();
        var table = Load(dataRoot, errors);
        if (errors.Count > 0)
            throw new InvalidDataException("Invalid rules tables:" + Environment.NewLine
                + string.Join(Environment.NewLine, errors.Select(e => "  - " + e)));
        return table;
    }

    /// <summary>Load every table, collecting errors instead of throwing. Missing fields fall back to code.</summary>
    public static RulesTable Load(string dataRoot, List<string> errors)
    {
        var json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        var dir = Path.Combine(dataRoot, Directory);
        var table = new RulesTable
        {
            Match = Read<MatchRules>(dir, "match", json, errors),
            Pitching = Read<PitchingRules>(dir, "pitching", json, errors),
            Batting = Read<BattingRules>(dir, "batting", json, errors),
            Flight = Read<FlightRules>(dir, "flight", json, errors),
            Fielding = Read<FieldingRules>(dir, "fielding", json, errors),
            Running = Read<RunningRules>(dir, "running", json, errors),
            Stars = Read<StarRules>(dir, "stars", json, errors),
            Cpu = Read<CpuRules>(dir, "cpu", json, errors)
        };
        RulesValidation.Validate(table, dir, errors);
        return table;
    }

    public static IReadOnlyList<string> Validate(string dataRoot)
    {
        var errors = new List<string>();
        Load(dataRoot, errors);
        return errors.OrderBy(e => e, StringComparer.Ordinal).ToList();
    }

    static T Read<T>(string dir, string name, JsonSerializerOptions json, List<string> errors) where T : class, new()
    {
        var path = Path.Combine(dir, name + ".json");
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
        try
        {
            var root = ContentCatalog.TryFindDataRoot();
            if (root is null) return RulesTable.Defaults;
            var errors = new List<string>();
            var table = RulesTable.Load(root, errors);
            return errors.Count == 0 ? table : RulesTable.Defaults;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
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

public static class RulesValidation
{
    public static void Validate(RulesTable table, string dir, List<string> errors)
    {
        Walk(table.Match, Path.Combine(dir, "match.json"), "match", errors);
        Walk(table.Pitching, Path.Combine(dir, "pitching.json"), "pitching", errors);
        Walk(table.Batting, Path.Combine(dir, "batting.json"), "batting", errors);
        Walk(table.Flight, Path.Combine(dir, "flight.json"), "flight", errors);
        Walk(table.Fielding, Path.Combine(dir, "fielding.json"), "fielding", errors);
        Walk(table.Running, Path.Combine(dir, "running.json"), "running", errors);
        Walk(table.Stars, Path.Combine(dir, "stars.json"), "stars", errors);
        Walk(table.Cpu, Path.Combine(dir, "cpu.json"), "cpu", errors);
        table.Cpu.Validate(Path.Combine(dir, "cpu.json"), errors);
        table.Flight.Validate(Path.Combine(dir, "flight.json"), errors);
        table.Running.Validate(Path.Combine(dir, "running.json"), errors);
        table.Fielding.Validate(Path.Combine(dir, "fielding.json"), errors);
        table.Batting.Validate(Path.Combine(dir, "batting.json"), errors);
        table.Pitching.Cpu.Validate(Path.Combine(dir, "pitching.json"), errors);
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
// pitching.json — §4, §4.7, §4.8
// ---------------------------------------------------------------------------------------

public sealed class PitchingRules
{
    public PitchSpeedRules Speed { get; init; } = new();
    public PitchReleaseRules Release { get; init; } = new();
    public PitchFlightRules Flight { get; init; } = new();
    public PitchShapeRules Shapes { get; init; } = new();
    public StarPitchShapeRules StarShapes { get; init; } = new();
    public StaminaRules Stamina { get; init; } = new();
    public CpuPitcherRules Cpu { get; init; } = new();
}

/// <summary>Base mph per shape and the Pitch-stat and charge coefficients (<see cref="AtBatResolver.PitchSpeedMph(PitchCommand, int, RulesTable)"/>).</summary>
public sealed class PitchSpeedRules
{
    [Positive] public double FastballMph { get; init; } = 86;
    [Positive] public double ChangeupMph { get; init; } = 72;
    public double MphPerPitchStat { get; init; } = 0.9;
    public double ChargeMph { get; init; } = 8;
    public double ChangeupChargeMph { get; init; } = 3;
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

/// <summary>The two shapes (spec §4.3): every shape crosses at its aim; the changeup's aim is <see cref="ChangeupDropFt"/> lower.</summary>
public sealed class PitchShapeRules
{
    /// <summary>The fastball rides above the straight line mid-flight and settles on its aim.</summary>
    public double FastballHump { get; init; } = 0.35;
    [Chance] public double ChangeupHangUntil { get; init; } = 0.62;
    public double ChangeupHangRate { get; init; } = 0.72;
    public double ChangeupDumpRate { get; init; } = 1.55;
    /// <summary>The changeup crosses this far below a fastball's height (up to one zone-half).</summary>
    public double ChangeupDropFt { get; init; } = 0.9;
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
/// star's cost is its <c>staminaCost</c> in star-skills.json. Below tiredBelow = TIRED (−mph,
/// −break, a crossing wobble); below 0 = exhausted (worse). The CPU swaps at TIRED with a lead.
/// </summary>
public sealed class StaminaRules
{
    public int PoolBase { get; init; } = 60;
    public int PoolPerPitch { get; init; } = 6;
    public int PitchCost { get; init; } = 4;
    public int ChargeCost { get; init; } = 3;
    public int ChangeupCost { get; init; } = 3;
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
    /// <summary>0-0, 1-0, 1-1 and every count no other row claims.</summary>
    public CpuPitchRow Even { get; init; } = new() { Location = "edge", Normal = 45, Charge = 20, Changeup = 15, Break = 20, StarChance = 0.05 };
    /// <summary>Ahead 0-2, 1-2: waste, then edge.</summary>
    public CpuPitchRow Ahead { get; init; } = new() { Location = "waste", Normal = 20, Charge = 15, Changeup = 35, Break = 30, StarChance = 0.15 };
    /// <summary>Behind 2-0, 3-0, 3-1: middle-in, safe.</summary>
    public CpuPitchRow Behind { get; init; } = new() { Location = "middleIn", Normal = 60, Charge = 30, Changeup = 5, Break = 5, StarChance = 0 };
    /// <summary>A runner on with two outs: middle, fast; never a pitch-out.</summary>
    public CpuPitchRow RunnerTwoOuts { get; init; } = new() { Location = "middle", Normal = 50, Charge = 40, Changeup = 0, Break = 10, StarChance = 0 };
    public CpuPitchLocations Locations { get; init; } = new();
    /// <summary>Aim scatter in feet per Pitch-stat point below 11 (spec §4.8).</summary>
    public double ScatterFtPerPitchStat { get; init; } = 0.10;
    [Positive] public double TiredScatterMul { get; init; } = 1.6;
    /// <summary>A charged CPU pitch releases inside the Nice! band this often.</summary>
    [Chance] public double NiceChance { get; init; } = 0.3;
    [Chance] public double TapMin { get; init; } = 0.1;
    [Chance] public double TapSpan { get; init; } = 0.35;
    /// <summary>The CPU walks the rubber before this share of pitches (a real verb: the batter may mistrack, §5.9).</summary>
    [Chance] public double RubberWalkChance { get; init; } = 0.35;
    [Chance] public double RubberWalkMax { get; init; } = 0.4;

    internal void Validate(string source, List<string> errors)
    {
        foreach (var (name, row) in new[] { ("even", Even), ("ahead", Ahead), ("behind", Behind), ("runnerTwoOuts", RunnerTwoOuts) })
        {
            if (row.Location is not ("edge" or "waste" or "middleIn" or "middle"))
                errors.Add($"{source}: pitching.cpu.{name}.location must be one of [edge, waste, middleIn, middle]; got '{row.Location}'");
            if (row.Normal + row.Charge + row.Changeup + row.Break <= 0)
                errors.Add($"{source}: pitching.cpu.{name} pitch mix must have a positive total");
        }
    }
}

/// <summary>One row of the CPU pitcher's table: where, and the mix of the four verbs (weights).</summary>
public sealed class CpuPitchRow
{
    public string Location { get; init; } = "edge";
    public double Normal { get; init; } = 45;
    public double Charge { get; init; } = 20;
    public double Changeup { get; init; } = 15;
    public double Break { get; init; } = 20;
    [Chance] public double StarChance { get; init; } = 0.05;
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
    /// <summary>A middle target varies its height by ± this.</summary>
    public double MiddleYSpreadFt { get; init; } = 0.5;
}

// ---------------------------------------------------------------------------------------
// batting.json — §5, §5.9
// ---------------------------------------------------------------------------------------

public sealed class BattingRules
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
    public BuddiesOnBaseRules BuddiesOnBase { get; init; } = new();
    public PitchFactorRules PitchFactor { get; init; } = new();
    public OffenseItemRules Items { get; init; } = new();
    public CpuBatterRules Cpu { get; init; } = new();

    internal void Validate(string source, List<string> errors)
    {
        RulesValidation.Order(source, "batting.launch.minDeg", Launch.MinDeg, Launch.MaxDeg, errors);
        RulesValidation.Order(source, "batting.window.floorFrames", Window.FloorFrames, Window.ChargeFrames, errors);
        RulesValidation.Order(source, "batting.window.chargeFrames", Window.ChargeFrames, Window.SlapFrames, errors);
        RulesValidation.Order(source, "batting.cursor.perfectFraction", Cursor.PerfectFraction, 1, errors);
    }
}

/// <summary>
/// The timing window (spec §5.3, D4, D13): slap 9 frames, charge 7, ± (contact − 5) × framesPerContact,
/// × skill, park and the human rung's multipliers, floored. It is centered on the ball's plate time
/// minus <see cref="LeadSec"/>. Inside the window timing decides direction only; the outermost
/// (1 − squareFraction) of each half demotes the cursor zone by one tier, never two.
/// </summary>
public sealed class ContactWindowRules
{
    [Positive] public double SlapFrames { get; init; } = 9.0;
    [Positive] public double ChargeFrames { get; init; } = 7.0;
    public double FramesPerContact { get; init; } = 0.4;
    [Positive] public double FloorFrames { get; init; } = 5.0;
    [Chance] public double SquareFraction { get; init; } = 0.9;
    /// <summary>
    /// The square press is this long before the ball reaches the plate (D13, #612): a human's eye
    /// times the ball at the plate, and the take is warped so its Contact mark meets the ball.
    /// </summary>
    [Positive] public double LeadSec { get; init; } = 0.10;
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
// fielding.json — §8
// ---------------------------------------------------------------------------------------

public sealed class FieldingRules
{
    public ChaseRules Chase { get; init; } = new();
    public ReactionRules Reaction { get; init; } = new();
    public CoverRules Cover { get; init; } = new();
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
    public ParkHazardRules Park { get; init; } = new();

    internal void Validate(string source, List<string> errors)
    {
        RulesValidation.Order(source, "fielding.catcher.cpuReleaseMinSec", Catcher.CpuReleaseMinSec, Catcher.CpuReleaseMaxSec, errors);
        RulesValidation.Order(source, "fielding.chem.slantLateralMinFt", Chem.SlantLateralMinFt, Chem.SlantLateralMaxFt, errors);
        RulesValidation.Order(source, "fielding.throw.minFtPerSec", Throw.MinFtPerSec, Throw.BaseFtPerSec, errors);
    }
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

/// <summary>Cover, cutoff and backup bodies (§8.7): a flat speed (D11) after a start delay; a throw is caught inside the cover radius of its target.</summary>
public sealed class CoverRules
{
    [Positive] public double FtPerSec { get; init; } = 28;
    /// <summary>Cover starts walking this long after contact.</summary>
    public double StartSec { get; init; } = 0.23;
    public double StopFt { get; init; } = 1.2;
    /// <summary>A throw landing farther than this from the receiver is not caught: it skips past, live (§8.5).</summary>
    [Positive] public double RadiusFt { get; init; } = 6;
    /// <summary>The backup body stands this far behind a throw's target, on its line.</summary>
    [Positive] public double BackupFt { get; init; } = 60;
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
    /// <summary>After Select / R swaps the glove, the stick does not re-take it for this long.</summary>
    public double SwapLockSec { get; init; } = 0.7;
    /// <summary>The nearest body to a loose ball chases it; a throw's receiver steps to a ball inside this of them.</summary>
    [Positive] public double LooseScoopFt { get; init; } = 3.5;
    /// <summary>
    /// An outfielder chasing a ball hit in the air runs at the one glove speed × this (§8.1, §8.2). The S-29 lever since the
    /// outfield read went back to the reference (#609): the read is when a body starts, this is how much ground it covers.
    /// </summary>
    [Positive] public double OutfieldAirMul { get; init; } = 0.6;
}

public sealed class CatchRules
{
    [Positive] public double RadiusBaseFt { get; init; } = 10;
    public double RadiusPerField { get; init; } = 0.6;
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
}

/// <summary>
/// The one throw model (§8.5): <c>throwSec = releaseSec + dist / (baseFtPerSec × arm × chem × ability)</c>
/// with <c>arm = armBase + Field × armPerField</c>. It flies the ball and judges the bag, for every
/// arm on the field, the catcher's gun included. Lateral error σ = (11 − Field) × lateralSigmaPerFieldDeficitFt.
/// </summary>
public sealed class ThrowRules
{
    public double ReleaseSec { get; init; } = 0.22;
    [Positive] public double BaseFtPerSec { get; init; } = 100;
    [Positive] public double MinFtPerSec { get; init; } = 32;
    [Positive] public double ArmBase { get; init; } = 0.85;
    public double ArmPerField { get; init; } = 0.03;
    public double LateralSigmaPerFieldDeficitFt { get; init; } = 0.35;
    /// <summary>A throw to an uncovered bag hangs as a lob this long for the cover; then it drops at the bag, live.</summary>
    [Positive] public double LobMaxSec { get; init; } = 1.5;
    /// <summary>A throw longer than this goes through the cutoff on the line (§8.7); the relay continues with the cutoff's arm.</summary>
    [Positive] public double OnTheFlyFt { get; init; } = 200;
    /// <summary>A fielder holding the ball this close to a force bag steps on it instead of throwing (§10.4, S-41).</summary>
    [Positive] public double UnassistedFt { get; init; } = 8;
    public double HandHeightFt { get; init; } = 3.2;
    public double BagHeightFt { get; init; } = 1.2;
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
/// off the cover by a lateral miss the receiver cannot reach — and is ordinary otherwise. The roll
/// is on the input; the outcome is still the ball missing the cover.
/// </summary>
public sealed class ThrowChemistryRules
{
    [Positive] public double GoodSpeedMul { get; init; } = 1.30;
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

public sealed class KnockbackRules
{
    public double MinEnergy { get; init; } = 72;
    public double SecPerFieldDeficit { get; init; } = 0.045;
    [Positive] public double EnergySpan { get; init; } = 90;
    public double MaxSec { get; init; } = 0.55;
    public double MinSec { get; init; } = 0.02;
}

public sealed class ParkHazardRules
{
    [Positive] public double EmberNightFireMul { get; init; } = 1.6;
    public double PipeReachPadFt { get; init; } = 8;
    [Chance] public double ShellWarpChance { get; init; } = 0.6;
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
        RulesValidation.Order(source, "running.rundown.throwWithinFt", Rundown.ThrowWithinFt, Rundown.RangeFt, errors);
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
/// CPU fielders throw once the runner is inside <see cref="ThrowWithinFt"/> of a covered bag and run at
/// them otherwise; when every live runner is at least <see cref="LazyLobFraction"/> of the way to a bag
/// the throw is a lazy lob at <see cref="LazyLobSpeedMul"/> of the arm.
/// </summary>
public sealed class RundownRules
{
    [Positive] public double RangeFt { get; init; } = 20;
    [Positive] public double ThrowWithinFt { get; init; } = 8;
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
    /// <summary>Runner on third tags on a caught fly this deep with fewer than two outs.</summary>
    public double TagThirdMinCarryFt { get; init; } = 200;
    /// <summary>Runner on second tags for third on a caught fly to right this deep.</summary>
    public double TagSecondMinCarryFt { get; init; } = 250;
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
}
