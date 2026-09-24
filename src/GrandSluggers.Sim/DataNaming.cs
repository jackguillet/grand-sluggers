using System.Text.Json;
using System.Text.RegularExpressions;

namespace GrandSluggers.Sim;

/// <summary>
/// The unit a number is in is in its key (spec §16): time ends <c>Sec</c>, a length ends <c>Ft</c>, a speed ends
/// <c>Mph</c> (a ball) or <c>FtPerSec</c> (a body), an angle ends <c>Deg</c>, a rate names both units
/// (<c>DegPerSec</c>, <c>perFtOfHeight</c>), and a plain ratio ends <c>Mul</c>, <c>Chance</c> or <c>Fraction</c>.
/// This refuses the two ways a key hides its unit: a long spelling (<c>…Seconds</c>, <c>…Feet</c>), and a last
/// word that names a time, speed or length with no unit after it (<c>smashFreeze</c>, <c>restSpeed</c>,
/// <c>radius</c>). Keys that broke the rule before it existed are listed in <see cref="Grandfathered"/>; the list
/// only shrinks. <c>data/art</c> is exempt: its numbers are Blender units and take frames (<c>docs/art-rails.md</c>).
/// </summary>
public static class DataNaming
{
    static readonly string[] LongUnits = ["Seconds", "Secs", "Millis", "Ms", "Feet", "Inches", "Meters", "Degrees"];
    static readonly HashSet<string> TimeWords = new(StringComparer.Ordinal)
        { "freeze", "hold", "blend", "delay", "duration", "time", "timeout", "wait", "pause", "interval" };
    static readonly HashSet<string> SpeedWords = new(StringComparer.Ordinal) { "speed", "velocity" };
    static readonly HashSet<string> LengthWords = new(StringComparer.Ordinal)
        { "distance", "length", "height", "width", "radius", "depth", "reach", "range" };

    /// <summary>The exempt folders, relative to the data root.</summary>
    public static readonly IReadOnlyList<string> Exempt = ["art"];

    /// <summary>
    /// <c>file#path</c> rows that break the rule and are left as they are until the table they sit in is next
    /// reworked. A <c>*</c> in the file stands for any name; <c>[]</c> in the path stands for any row.
    /// </summary>
    public static readonly IReadOnlyList<string> Grandfathered =
    [
        "characters/*.json#proportions.height",   // a scale on the shared rig, not a length
        "characters/*.json#proportions.width",    // a scale on the shared rig, not a length
        "feel/shots.json#shots[].blend",
        "feel/table.json#afterCountSeconds",
        "feel/table.json#afterOutSeconds",
        "feel/table.json#cameraBlend",
        "feel/table.json#cameraHoldSeconds",
        "feel/table.json#chargeMaxHoldSeconds",
        "feel/table.json#contactCutSeconds",
        "feel/table.json#pitchChargeSeconds",
        "feel/table.json#pitcherReadySeconds",
        "feel/table.json#smashFreeze",
        "feel/table.json#smashHold",
        "feel/table.json#solidFreeze",
        "feel/table.json#swingChargeSeconds",
        "parks/*.json#hazards[].radius",
        "parks/*.json#night.hazards[].radius",
        "rules/batting.json#cpu.archetype.speed",
        "rules/fielding.json#handling.hopPhaseHalfWidth", // a fraction of the hop's phase, not a length
        "rules/flight.json#maxSeconds",
        "rules/grounds.json#*.roll.restSpeed",
    ];

    static readonly Regex Words = new("[A-Z]?[a-z0-9]+|[A-Z]+(?![a-z])", RegexOptions.CultureInvariant);
    static readonly Regex Rate = new("(^per|Per)[A-Z]", RegexOptions.CultureInvariant);

    /// <summary>Why a numeric key breaks the rule, or null when it does not.</summary>
    public static string? Problem(string key)
    {
        foreach (var unit in LongUnits)
            if (key.EndsWith(unit, StringComparison.Ordinal) && key.Length > unit.Length)
                return $"spells its unit long; end it '{Short(unit)}'";
        if (Rate.IsMatch(key)) return null;
        var words = Words.Matches(key);
        if (words.Count == 0) return null;
        var last = words[^1].Value.ToLowerInvariant();
        if (TimeWords.Contains(last)) return "is a time with no unit; end it 'Sec'";
        if (SpeedWords.Contains(last)) return "is a speed with no unit; end it 'Mph' or 'FtPerSec'";
        if (LengthWords.Contains(last)) return "is a length with no unit; end it 'Ft'";
        return null;
    }

    static string Short(string unit) => unit switch
    {
        "Seconds" or "Secs" or "Millis" or "Ms" => "Sec",
        "Feet" or "Inches" or "Meters" => "Ft",
        _ => "Deg",
    };

    public static IReadOnlyList<string> Validate(string shipped)
    {
        var errors = new List<string>();
        var used = new HashSet<string>(StringComparer.Ordinal);
        var files = RuntimePackage.Load(shipped).Files(shipped)
            .Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Where(f => !Exempt.Any(folder => f.StartsWith(folder + "/", StringComparison.Ordinal)));
        foreach (var file in files)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(shipped, file)), DataJson.Document);
            Walk(doc.RootElement, "", file, errors, used);
        }
        foreach (var row in Grandfathered.Where(row => !used.Contains(row)))
            errors.Add($"DataNaming.Grandfathered: '{row}' names no key any more; delete the row");
        return errors;
    }

    /// <summary>The rule over one document, for a file that is not on disk.</summary>
    public static IReadOnlyList<string> ValidateJson(string file, string json)
    {
        var errors = new List<string>();
        using var doc = JsonDocument.Parse(json, DataJson.Document);
        Walk(doc.RootElement, "", file, errors, new HashSet<string>(StringComparer.Ordinal));
        return errors;
    }

    static void Walk(JsonElement element, string path, string file, List<string> errors, HashSet<string> used)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in element.EnumerateArray()) Walk(row, path + "[]", file, errors, used);
            return;
        }
        if (element.ValueKind != JsonValueKind.Object) return;
        foreach (var property in element.EnumerateObject())
        {
            var at = path.Length == 0 ? property.Name : path + "." + property.Name;
            if (IsNumeric(property.Value) && Problem(property.Name) is { } problem)
            {
                if (GrandfatheredRow(file, at) is { } row) used.Add(row);
                else errors.Add($"data/{file}: '{at}' {problem} (spec §16 units)");
            }
            Walk(property.Value, at, file, errors, used);
        }
    }

    static bool IsNumeric(JsonElement value) =>
        value.ValueKind == JsonValueKind.Number
        || value.ValueKind == JsonValueKind.Array && value.GetArrayLength() > 0
           && value.EnumerateArray().All(e => e.ValueKind == JsonValueKind.Number);

    static string? GrandfatheredRow(string file, string path) => Grandfathered.FirstOrDefault(row =>
    {
        var hash = row.IndexOf('#');
        return Glob(row[..hash], file) && Glob(row[(hash + 1)..], path);
    });

    static bool Glob(string pattern, string value) =>
        Regex.IsMatch(value, "^" + Regex.Escape(pattern).Replace(@"\*", "[^/.]+") + "$", RegexOptions.CultureInvariant);
}
