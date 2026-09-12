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
        ["pitching", "batting", "flight", "fielding", "running", "stars", "cpu"];

    public PitchingRules Pitching { get; init; } = new();
    public BattingRules Batting { get; init; } = new();
    public FlightRules Flight { get; init; } = new();
    public FieldingRules Fielding { get; init; } = new();
    public RunningRules Running { get; init; } = new();
    public StarRules Stars { get; init; } = new();
    public CpuRules Cpu { get; init; } = new();

    /// <summary>The code-side numbers. Equal to the shipped JSON; a load fallback, not a second table.</summary>
    public static RulesTable Defaults => new();

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
// pitching.json — §4, §4.7, §4.8
// ---------------------------------------------------------------------------------------

public sealed class PitchingRules
{
    public PitchSpeedRules Speed { get; init; } = new();
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
    [Positive] public double SliderMph { get; init; } = 80;
    [Positive] public double CurveMph { get; init; } = 76;
    [Positive] public double ChangeupMph { get; init; } = 72;
    public double MphPerPitchStat { get; init; } = 0.9;
    public double ChargeMph { get; init; } = 8;
    public double ChangeupChargeMph { get; init; } = 3;
    [Positive] public double StarSpeedMul { get; init; } = 1.12;
}

/// <summary>Release point, air time, and break geometry (<see cref="PitchFlight"/>).</summary>
public sealed class PitchFlightRules
{
    [Signed] public double ReleaseHandX { get; init; } = 1.55;
    [Positive] public double ReleaseHandY { get; init; } = 6.2;
    public double ReleaseTowardPlate { get; init; } = 2.6;
    /// <summary>Feet the release hand moves per unit of rubber walk.</summary>
    public double RubberReleaseX { get; init; } = 2.2;
    /// <summary>Plate-aim units the crossing moves per unit of rubber walk.</summary>
    public double RubberCrossingX { get; init; } = 0.35;
    [Positive] public double ArcadeScale { get; init; } = 2.05;
    [Positive] public double AirMinSec { get; init; } = 0.78;
    [Positive] public double AirMaxSec { get; init; } = 1.28;
    [Positive] public double MinMph { get; init; } = 40;
    public double BreakEarly { get; init; } = 1.55;
    /// <summary>Late break ramps over [breakLateFrom, breakLateFrom + breakLateSpan] of the flight.</summary>
    [Chance] public double BreakLateFrom { get; init; } = 0.55;
    [Positive] public double BreakLateSpan { get; init; } = 0.45;
    public double BreakLate { get; init; } = 1.8;
}

public sealed class PitchShapeRules
{
    public double FastballDrop { get; init; } = 0.35;
    [Chance] public double ChangeupHangUntil { get; init; } = 0.62;
    public double ChangeupHangRate { get; init; } = 0.72;
    public double ChangeupDumpRate { get; init; } = 1.55;
    public double CurveSweep { get; init; } = 1.7;
    public double CurveHump { get; init; } = 1.35;
    [Chance] public double SliderBiteFrom { get; init; } = 0.55;
    [Positive] public double SliderBiteSpan { get; init; } = 0.45;
    public double SliderBite { get; init; } = 2.4;
    public double SliderDrop { get; init; } = 0.55;
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

/// <summary>Team stamina pool costs and the TIRED tell (<see cref="Match"/>).</summary>
public sealed class StaminaRules
{
    public int PitchCost { get; init; } = 6;
    public int ChargeCost { get; init; } = 4;
    public int StarCost { get; init; } = 12;
    public int TiredBelow { get; init; } = 25;
    public int SwapRestore { get; init; } = 35;
    public double TiredAimX { get; init; } = 0.22;
    public double TiredAimY { get; init; } = 0.18;
}

/// <summary>The CPU pitcher's rolls as shipped. §4.8 replaces them with a table (P1); the numbers live here until then.</summary>
public sealed class CpuPitcherRules
{
    [Chance] public double StarChanceCaptain { get; init; } = 0.14;
    [Chance] public double StarChance { get; init; } = 0.08;
    [Chance] public double ChangeupChance { get; init; } = 0.22;
    [Chance] public double SliderChance { get; init; } = 0.22;
    [Chance] public double CurveChance { get; init; } = 0.5;
    [Chance] public double ChargeChance { get; init; } = 0.3;
    [Chance] public double ChargeMin { get; init; } = 0.75;
    [Chance] public double ChargeSpan { get; init; } = 0.25;
    [Chance] public double TapMin { get; init; } = 0.1;
    [Chance] public double TapSpan { get; init; } = 0.35;
    public double ErrorFramesPerPitchStat { get; init; } = 0.42;
    public double TiredErrorMul { get; init; } = 1.6;
    public double ScatterPerPitchStat { get; init; } = 0.055;
    public double ScatterYMul { get; init; } = 0.85;
    public double TiredScatterMul { get; init; } = 1.6;
    public double SliderBreak { get; init; } = 0.85;
    public double CurveBreak { get; init; } = 0.7;
    public double RubberCrossingMul { get; init; } = 0.35;
    public CpuPickoffRules Pickoff { get; init; } = new();
}

/// <summary>Random pickoff on a walking lead. Retired by D1 / D3 (P6); the numbers live here until then.</summary>
public sealed class CpuPickoffRules
{
    [Chance] public double LeadMin { get; init; } = 0.2;
    public double RiskPerLead { get; init; } = 0.42;
    public double ReturningMul { get; init; } = 0.18;
    public double RiskPerStatDiff { get; init; } = 0.02;
    [Chance] public double MaxRisk { get; init; } = 0.72;
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
/// The timing window (spec §5.3, D4): slap 9 frames, charge 7, ± (contact − 5) × framesPerContact,
/// × skill and park multipliers, floored. Inside the window timing decides direction only; the
/// outermost (1 − squareFraction) of each half demotes the cursor zone by one tier, never two.
/// </summary>
public sealed class ContactWindowRules
{
    [Positive] public double SlapFrames { get; init; } = 9.0;
    [Positive] public double ChargeFrames { get; init; } = 7.0;
    public double FramesPerContact { get; init; } = 0.4;
    [Positive] public double FloorFrames { get; init; } = 5.0;
    [Chance] public double SquareFraction { get; init; } = 0.9;
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
    [Positive] public double BaseMph { get; init; } = 57;
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

public sealed class StarSwingRules
{
    [Chance] public double PhonyballWhiff { get; init; } = 0.4;
    public double PrismballSpraySpanDeg { get; init; } = 22;
    public double GroundLaunchDeg { get; init; } = 8;
    public double FlyLaunchDeg { get; init; } = 38;
    public double LineLaunchDeg { get; init; } = 18;
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

public sealed class OffenseItemRules
{
    [Chance] public double CpuThrowChance { get; init; } = 0.4;
    [Chance] public double RocketDazeChance { get; init; } = 0.55;
}

/// <summary>The CPU batter's rolls as shipped. §5.9 replaces them with a table (P1).</summary>
public sealed class CpuBatterRules
{
    [Chance] public double SacBuntChance { get; init; } = 0.12;
    [Chance] public double SacBuntCharge { get; init; } = 0.12;
    public double SacBuntErrorSigma { get; init; } = 2.2;
    public double SacBuntSpraySigma { get; init; } = 10;
    public double SacBuntLaunchAim { get; init; } = 0.35;
    [Chance] public double ChaseChance { get; init; } = 0.12;
    [Chance] public double StarChanceCaptain { get; init; } = 0.14;
    [Chance] public double StarChance { get; init; } = 0.08;
    [Chance] public double ChargeChance { get; init; } = 0.35;
    [Chance] public double ChargeMin { get; init; } = 0.7;
    [Chance] public double ChargeSpan { get; init; } = 0.3;
    [Chance] public double TapSpan { get; init; } = 0.4;
    public double ErrorFramesPerBatStat { get; init; } = 0.62;
    public double OutOfZoneErrorFrames { get; init; } = 4;
    public double SpraySigmaDeg { get; init; } = 12;
    public double LaunchAimSigma { get; init; } = 0.45;
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
    [Positive] public double PlateHeightFt { get; init; } = 2.5;
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
    public CarryBandRules Carry { get; init; } = new();
    public DeadBallRules DeadBall { get; init; } = new();

    internal void Validate(string source, List<string> errors)
    {
        RulesValidation.Order(source, "flight.skid.launchMinDeg", Skid.LaunchMinDeg, Skid.LaunchMaxDeg, errors);
        RulesValidation.Order(source, "flight.classes.topperMaxLaunchDeg", Classes.TopperMaxLaunchDeg, Classes.GrounderMaxLaunchDeg, errors);
        RulesValidation.Order(source, "flight.classes.grounderMaxLaunchDeg", Classes.GrounderMaxLaunchDeg, Classes.ChopperMaxLaunchDeg, errors);
        RulesValidation.Order(source, "flight.classes.chopperMaxLaunchDeg", Classes.ChopperMaxLaunchDeg, Classes.LinerMaxLaunchDeg, errors);
        RulesValidation.Order(source, "flight.carry.doubleFt", Carry.DoubleFt, Carry.TripleFt, errors);
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
    public double LinerMinExitMph { get; init; } = 78;
    /// <summary>Dirt / grass lip past the rubber. A fly landing inside it is a pop; infielders own the hop inside it.</summary>
    [Positive] public double InfieldLipFt { get; init; } = 155;
}

/// <summary>Hit type by carry when nobody catches it. P3 replaces these with runner geometry.</summary>
public sealed class CarryBandRules
{
    public double TripleFt { get; init; } = 330;
    public double DoubleFt { get; init; } = 250;
    public double LineDoubleFt { get; init; } = 180;
    public double GroundDoubleFt { get; init; } = 90;
}

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
    public FieldDashRules Dash { get; init; } = new();
    public CatchRules Catch { get; init; } = new();
    public RangeRules Range { get; init; } = new();
    public DropRules Drops { get; init; } = new();
    public GroundOutRollRules GroundOut { get; init; } = new();
    public WallPlantRules WallPlant { get; init; } = new();
    public FieldAbilityRules Abilities { get; init; } = new();
    public ThrowRules Throw { get; init; } = new();
    public CatcherRules Catcher { get; init; } = new();
    public ThrowChemistryRules Chem { get; init; } = new();
    public BobbleRules Bobble { get; init; } = new();
    public KnockbackRules Knockback { get; init; } = new();
    public ParkHazardRules Park { get; init; } = new();

    internal void Validate(string source, List<string> errors)
    {
        RulesValidation.Order(source, "fielding.catcher.cpuReleaseMinSec", Catcher.CpuReleaseMinSec, Catcher.CpuReleaseMaxSec, errors);
        RulesValidation.Order(source, "fielding.throw.flightMinSec", Throw.FlightMinSec, Throw.FlightMaxSec, errors);
    }
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
    /// <summary>Human stick glove. §8.1 unifies it with the CPU chase (P4); the second formula lives here until then.</summary>
    [Positive] public double StickBaseFtPerSec { get; init; } = 18;
    public double StickFtPerSecPerRun { get; init; } = 1.8;
    [Positive] public double StickFrozenMul { get; init; } = 0.4;
    /// <summary>Flat bag-cover speed (D11).</summary>
    [Positive] public double CoverFtPerSec { get; init; } = 28;
    public double CoverStopFt { get; init; } = 1.2;
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
    /// <summary>Resolver fly catch: the glove must arrive this long before hang.</summary>
    public double FlyWindowLeadSec { get; init; } = 0.25;
    public double LineWindowMul { get; init; } = 3.6;
    public double FlyRangeMul { get; init; } = 3.2;
    public double GroundRangeMul { get; init; } = 2.5;
    public double BuddyPlantFt { get; init; } = 26;
    public double BuddyJumpHoldSec { get; init; } = 0.18;
    public double FlyThrowDelaySec { get; init; } = 0.35;
    /// <summary>CPU dead-stick catch beats, as shipped. §8.3 replaces them with the radius at the window (P4).</summary>
    public double LineCatchLeadSec { get; init; } = 0.12;
    public double LineCatchMaxBallY { get; init; } = 8;
    public double FlyCatchLeadSec { get; init; } = 0.18;
    public double GrounderScoopMaxBallY { get; init; } = 3.2;
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

public sealed class RangeRules
{
    [Positive] public double BaseFt { get; init; } = 24;
    public double FtPerField { get; init; } = 2.8;
    public double FtPerRun { get; init; } = 1.8;
    public double ClamberFt { get; init; } = 18;
}

public sealed class DropRules
{
    [Chance] public double Heatball { get; init; } = 0.35;
    [Chance] public double PhonySwing { get; init; } = 0.35;
    [Chance] public double Frozen { get; init; } = 0.4;
}

/// <summary>The infield out/hit roll (spec A.4 #37). P4 replaces it with arrival geometry; the numbers live here until then.</summary>
public sealed class GroundOutRollRules
{
    public double RollSpan { get; init; } = 4;
    public double GloveMul { get; init; } = 4;
    public double Bonus { get; init; } = 3;
    public double Threshold { get; init; } = 7;
    public double PerfectBeat { get; init; } = 2.5;
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
    public double SuperJumpCatchBonusFt { get; init; } = 3;
    public double SuperJumpFlyRangeFt { get; init; } = 22;
    public double DiveGroundRangeFt { get; init; } = 16;
    [Positive] public double LaserMul { get; init; } = 1.45;
    [Positive] public double SnapThrowMul { get; init; } = 1.22;
}

/// <summary>
/// Throw clocks as shipped: the verdict clock (<see cref="InPlay.ThrowSec"/>) and the live
/// flight clock the client played (a fourth formula, spec A.4 #40). P4 collapses them into one.
/// </summary>
public sealed class ThrowRules
{
    public double ReleaseSec { get; init; } = 0.22;
    [Positive] public double BaseFtPerSec { get; init; } = 56;
    [Positive] public double MinFtPerSec { get; init; } = 32;
    [Positive] public double FlightBaseSec { get; init; } = 1.12;
    [Positive] public double FlightMinMul { get; init; } = 0.45;
    [Positive] public double FlightMinSec { get; init; } = 0.55;
    [Positive] public double FlightMaxSec { get; init; } = 1.55;
    public double HandHeightFt { get; init; } = 3.2;
    public double BagHeightFt { get; init; } = 1.2;
}

public sealed class CatcherRules
{
    public double ReleaseSec { get; init; } = 0.12;
    [Positive] public double BaseFtPerSec { get; init; } = 96;
    [Positive] public double MinFtPerSec { get; init; } = 64;
    [Positive] public double MinMul { get; init; } = 0.45;
    [Positive] public double ErrorMul { get; init; } = 0.72;
    public double CpuReleaseBaseSec { get; init; } = 0.42;
    public double CpuReleasePerField { get; init; } = 0.014;
    public double CpuReleaseNoiseSec { get; init; } = 0.20;
    public double CpuReleaseMinSec { get; init; } = 0.10;
    public double CpuReleaseMaxSec { get; init; } = 0.58;
    /// <summary>The tag beat after a steal throw lands, before the verdict is committed.</summary>
    public double TagHoldSec { get; init; } = 0.38;
    public double CpuRemainNoiseSec { get; init; } = 0.28;
}

public sealed class ThrowChemistryRules
{
    [Positive] public double GoodSpeedMul { get; init; } = 1.35;
    [Positive] public double BadSpeedMul { get; init; } = 0.70;
    [Chance] public double BadErrorChance { get; init; } = 0.25;
    public double BadLateralFt { get; init; } = 14;
    public double NeutralLateralFt { get; init; } = 3;
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
    [Positive] public double CrystalNightWindowMul { get; init; } = 0.85;
    [Positive] public double EmberNightFireMul { get; init; } = 1.6;
    public double PipeReachPadFt { get; init; } = 8;
    [Chance] public double ShellWarpChance { get; init; } = 0.6;
}

// ---------------------------------------------------------------------------------------
// running.json — §9, §10.2, §10.3, §10.6, §11.2, §11.6
// ---------------------------------------------------------------------------------------

public sealed class RunningRules
{
    public HomeToFirstRules HomeToFirst { get; init; } = new();
    public BagToBagRules BagToBag { get; init; } = new();
    public BagRules Bags { get; init; } = new();
    public ClosePlayRules Close { get; init; } = new();
    public StealRules Steal { get; init; } = new();
    public TagUpRules TagUp { get; init; } = new();
    public RunStickRules Stick { get; init; } = new();
    public DashRules Dash { get; init; } = new();
    public CpuRunnerRules Cpu { get; init; } = new();

    internal void Validate(string source, List<string> errors)
    {
        RulesValidation.Order(source, "running.homeToFirst.minSec", HomeToFirst.MinSec, HomeToFirst.MaxSec, errors);
        RulesValidation.Order(source, "running.bagToBag.minSec", BagToBag.MinSec, BagToBag.MaxSec, errors);
        RulesValidation.Order(source, "running.bags.tagSafeRadiusFt", Bags.TagSafeRadiusFt, Bags.OccupyRadiusFt, errors);
    }
}

public sealed class HomeToFirstRules
{
    [Positive] public double BaseSec { get; init; } = 4.32;
    public double SecPerRun { get; init; } = 0.13;
    [Positive] public double MinSec { get; init; } = 2.9;
    [Positive] public double MaxSec { get; init; } = 4.35;
    [Chance] public double DashMul { get; init; } = 0.12;
    [Positive] public double FloorSec { get; init; } = 2.45;
    /// <summary>Guard against a zero run time when positioning a runner along the path.</summary>
    [Positive] public double RunFeetMinSec { get; init; } = 0.4;
}

public sealed class BagToBagRules
{
    [Positive] public double BaseSec { get; init; } = 3.55;
    public double SecPerRun { get; init; } = 0.12;
    [Positive] public double MinSec { get; init; } = 2.45;
    [Positive] public double MaxSec { get; init; } = 3.65;
}

public sealed class BagRules
{
    [Positive] public double OccupyRadiusFt { get; init; } = 6;
    [Positive] public double TagReachFt { get; init; } = 14;
    [Positive] public double TagSafeRadiusFt { get; init; } = 3.5;
    [Positive] public double TimeOnBagSec { get; init; } = 1.0;
    /// <summary>Inside this of the end of a segment the runner is placed on the bag.</summary>
    public double SnapFt { get; init; } = 0.5;
}

public sealed class ClosePlayRules
{
    /// <summary>Throw landed and the runner got there first by no more than this: the SAFE stamp.</summary>
    public double MarginSec { get; init; } = 0.45;
    public double IconDelaySec { get; init; } = 0.22;
    public double CpuReactionBaseSec { get; init; } = 0.20;
    public double CpuReactionPerStat { get; init; } = 0.032;
}

/// <summary>Lead-as-time-credit steal race as shipped (spec A.6 #61). D2 replaces it with a break at release (P6).</summary>
public sealed class StealRules
{
    public double JumpBaseSec { get; init; } = 0.62;
    public double JumpPerLeadSec { get; init; } = 1.08;
    [Positive] public double RemainMinSec { get; init; } = 0.58;
    public double ReturnBaseSec { get; init; } = 0.62;
    public double ReturnPerLeadSec { get; init; } = 0.92;
    public double ReturnPerRunDeficitSec { get; init; } = 0.06;
    [Positive] public double ReturnMinSec { get; init; } = 0.42;
    [Chance] public double ArmedLeadMin { get; init; } = 0.2;
    /// <summary>The steal phase gives up and commits when nobody has thrown by this long past the runner's arrival.</summary>
    public double NoThrowRemainSec { get; init; } = 1.6;
}

public sealed class TagUpRules
{
    public double SacFlyCarryFt { get; init; } = 230;
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

/// <summary>The CPU steal roll as shipped inside the swing (spec A.1 #20). §11.6 moves it to a runner table (P6).</summary>
public sealed class CpuRunnerRules
{
    public int StealMinRun { get; init; } = 7;
    [Chance] public double StealChance { get; init; } = 0.16;
    [Chance] public double StealLeadMin { get; init; } = 0.45;
    [Chance] public double StealLeadSpan { get; init; } = 0.35;
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
    public double CaughtStealing { get; init; } = 0.4;
    public double Billboard { get; init; } = 1.0;
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
        TimingSigmaMul = 1.3, ReactionMul = 1.4, MakeableMarginSec = 0.30, PerfectStealChance = 0, PickoffChance = 0.03
    };
    public CpuLevelRules Normal { get; init; } = new();
    public CpuLevelRules Hard { get; init; } = new()
    {
        TimingSigmaMul = 0.8, ReactionMul = 0.8, MakeableMarginSec = 0.05, PerfectStealChance = 0.4, PickoffChance = 0.10
    };

    public CpuLevelRules Active => Level.ToLowerInvariant() switch
    {
        "easy" => Easy,
        "hard" => Hard,
        _ => Normal
    };

    internal void Validate(string source, List<string> errors)
    {
        if (Level.ToLowerInvariant() is not ("easy" or "normal" or "hard"))
            errors.Add($"{source}: cpu.level must be one of [easy, hard, normal]; got '{Level}'");
    }
}

public sealed class CpuLevelRules
{
    /// <summary>Multiplies the CPU batter's timing-error σ (§5.9).</summary>
    [Positive] public double TimingSigmaMul { get; init; } = 1.0;
    /// <summary>Multiplies CPU reaction and release delays (§8.8, §9.6, §11.3).</summary>
    [Positive] public double ReactionMul { get; init; } = 1.0;
    /// <summary>Margin a CPU fielder needs to call a play makeable (§8.8). Read by P4.</summary>
    public double MakeableMarginSec { get; init; } = 0.15;
    /// <summary>Perfect-steal chance (§11.6). Read by P6.</summary>
    [Chance] public double PerfectStealChance { get; init; } = 0.2;
    /// <summary>Pickoff attempt chance per SET with a runner on (§4.8). Read by P6.</summary>
    [Chance] public double PickoffChance { get; init; } = 0.06;
}
