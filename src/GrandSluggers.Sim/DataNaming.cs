using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace GrandSluggers.Sim;

/// <summary>
/// How a number in <c>data/</c> names its unit. A key whose value is a number (or a list of numbers) ends in its unit,
/// spelled the short way: seconds <c>Sec</c>, feet <c>Ft</c>, degrees <c>Deg</c>, a body's speed <c>FtPerSec</c>, a
/// ball's speed <c>Mph</c>. Two mistakes are refused:
/// <list type="bullet">
/// <item>a unit spelled long (<c>chargeSeconds</c>, <c>depthFeet</c>): write <c>chargeSec</c>, <c>depthFt</c>;</item>
/// <item>a time or a speed with no unit (<c>smashFreeze</c>, <c>restSpeed</c>): write <c>smashFreezeSec</c>,
/// <c>restSpeedFtPerSec</c>.</item>
/// </list>
/// The keys that broke the rule before it existed are <see cref="Grandfathered"/>; renaming one removes its entry.
/// </summary>
public static class DataNaming
{
    static readonly Regex LongUnit = new(
        "(Seconds|Second|Secs|Milliseconds|Millis|Ms|Feet|Foot|Inches|Inch|Degrees|Degree|Radians|Meters|Metres|Kph|Fps)$",
        RegexOptions.CultureInvariant);

    static readonly Regex NoUnit = new(
        "(^|[a-z0-9])(Freeze|Hold|Blend|Delay|Duration|Time|Timeout|Lag|Wait|Pause|Speed|Velocity|Velo)$"
        + "|^(freeze|hold|blend|delay|duration|time|timeout|lag|wait|pause|speed|velocity|velo)$",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Keys that broke the rule before it existed: a data file relative to <c>data/</c>, and the key. Their C# members
    /// read them by these names; renaming one is its own change, and removes its line here.
    /// </summary>
    public static IReadOnlyCollection<(string File, string Key)> Grandfathered => _grandfathered;

    static readonly HashSet<(string File, string Key)> _grandfathered = new()
    {
        ("art/baseball-takes.json", "duration"),
        ("feel/shots.json", "blend"),
        ("feel/table.json", "afterCountSeconds"),
        ("feel/table.json", "afterOutSeconds"),
        ("feel/table.json", "cameraBlend"),
        ("feel/table.json", "cameraHoldSeconds"),
        ("feel/table.json", "chargeMaxHoldSeconds"),
        ("feel/table.json", "contactCutSeconds"),
        ("feel/table.json", "pitchChargeSeconds"),
        ("feel/table.json", "pitcherReadySeconds"),
        ("feel/table.json", "smashFreeze"),
        ("feel/table.json", "smashHold"),
        ("feel/table.json", "solidFreeze"),
        ("feel/table.json", "swingChargeSeconds"),
        ("rules/batting.json", "speed"),
        ("rules/flight.json", "maxSeconds"),
        ("rules/grounds.json", "restSpeed"),
    };

    /// <summary>Every unit-naming error under the data folder, one per file and key.</summary>
    public static IReadOnlyList<string> Validate(string dataFolder)
    {
        var errors = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(dataFolder, "*.json", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(dataFolder, file).Replace('\\', '/');
            JsonNode? root;
            try { root = JsonNode.Parse(File.ReadAllText(file), documentOptions: DataJson.Document); }
            catch (System.Text.Json.JsonException e) { errors.Add($"{relative}: not JSON ({e.Message})"); continue; }
            Walk(relative, root, errors);
        }
        return errors.ToList();
    }

    /// <summary>The grandfathered keys no data file carries any more: a rename that forgot to drop its line.</summary>
    public static IReadOnlyList<string> StaleGrandfathered(string dataFolder)
    {
        var seen = new HashSet<(string, string)>();
        foreach (var file in Directory.EnumerateFiles(dataFolder, "*.json", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(dataFolder, file).Replace('\\', '/');
            Keys(relative, JsonNode.Parse(File.ReadAllText(file), documentOptions: DataJson.Document), seen);
        }
        return _grandfathered.Where(g => !seen.Contains(g)).Select(g => $"{g.File}: {g.Key}").OrderBy(s => s, StringComparer.Ordinal).ToList();
    }

    /// <summary>The problem with one numeric key's name, or null when it names its unit.</summary>
    public static string? Problem(string key)
    {
        if (LongUnit.IsMatch(key)) return "spells its unit long; use Sec, Ft, Deg, FtPerSec or Mph";
        if (NoUnit.IsMatch(key)) return "is a time or a speed with no unit; end it in Sec, FtPerSec or Mph";
        return null;
    }

    static void Walk(string file, JsonNode? node, SortedSet<string> errors)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (key, value) in obj)
                {
                    if (IsNumeric(value) && !_grandfathered.Contains((file, key)) && Problem(key) is { } problem)
                        errors.Add($"{file}: '{key}' {problem}");
                    Walk(file, value, errors);
                }
                break;
            case JsonArray array:
                foreach (var item in array) Walk(file, item, errors);
                break;
        }
    }

    static void Keys(string file, JsonNode? node, HashSet<(string, string)> seen)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (key, value) in obj)
                {
                    if (IsNumeric(value)) seen.Add((file, key));
                    Keys(file, value, seen);
                }
                break;
            case JsonArray array:
                foreach (var item in array) Keys(file, item, seen);
                break;
        }
    }

    static bool IsNumeric(JsonNode? value) => value switch
    {
        JsonValue v => v.GetValueKind() == System.Text.Json.JsonValueKind.Number,
        JsonArray a => a.Count > 0 && a.All(IsNumeric),
        _ => false
    };
}
