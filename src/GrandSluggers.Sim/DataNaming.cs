using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace GrandSluggers.Sim.Tooling;

/// <summary>
/// How a number in <c>data/</c> names its unit. A key whose value is a number (or a list of numbers) ends in its unit,
/// spelled the short way: seconds <c>Sec</c>, feet <c>Ft</c>, degrees <c>Deg</c>, a body's speed <c>FtPerSec</c>, a
/// ball's speed <c>Mph</c>. Two mistakes are refused:
/// <list type="bullet">
/// <item>a unit spelled long (<c>chargeSeconds</c>, <c>depthFeet</c>): write <c>chargeSec</c>, <c>depthFt</c>;</item>
/// <item>a time or a speed with no unit (<c>smashFreeze</c>, <c>restSpeed</c>): write <c>smashFreezeSec</c>,
/// <c>restSpeedFtPerSec</c>;</item>
/// <item>a length with no unit (<c>radius</c>, <c>throwDistance</c>): write <c>radiusFt</c>. A scale is not a length:
/// name it <c>…Mul</c> or <c>…Scale</c>. A rate names both units (<c>perFtOfHeight</c>) and passes.</item>
/// </list>
/// The keys that broke the rule before it existed are <see cref="Grandfathered"/>; renaming one removes its entry. An
/// entry's file may be <c>folder/*.json</c> for a key every file in that folder carries.
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

    static readonly Regex NoLength = new(
        "(^|[a-z0-9])(Distance|Length|Height|Width|Radius|Depth|Reach|Range)$"
        + "|^(distance|length|height|width|radius|depth|reach|range)$",
        RegexOptions.CultureInvariant);

    static readonly Regex Rate = new("(^per|Per)[A-Z]", RegexOptions.CultureInvariant);

    /// <summary>
    /// Keys that broke the rule before it existed: a data file relative to <c>data/</c>, and the key. Their C# members
    /// read them by these names; renaming one is its own change, and removes its line here.
    /// </summary>
    public static IReadOnlyCollection<(string File, string Key)> Grandfathered => _grandfathered;

    static readonly HashSet<(string File, string Key)> _grandfathered = new()
    {
        ("art/baseball-equipment.json", "length"),      // Blender units
        ("art/baseball-equipment.json", "pocketDepth"),
        ("art/baseball-equipment.json", "radius"),
        ("art/baseball-equipment.json", "width"),
        ("art/baseball-equipment.json", "wristRadius"),
        ("art/baseball-takes.json", "duration"),
        ("art/rig.json", "height"),                     // Blender units
        ("characters/*.json", "height"),                // a proportion: a scale on the shared rig
        ("characters/*.json", "width"),
        ("world/species.json", "height"),              // a sidekick build's proportion, the same scale as a captain's
        ("world/species.json", "width"),
        ("characters/*.json", "velocity"),              // a 1–10 rating (pitch power), the code slot's name, not a speed
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
        ("parks/*.json", "radius"),                     // a hazard's disc, in feet
        ("rules/fielding.json", "hopPhaseHalfWidth"),   // a fraction of the hop's phase
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
        if (NoLength.IsMatch(key) && !Rate.IsMatch(key)) return "is a length with no unit; end it in Ft (a scale ends in Mul or Scale)";
        return null;
    }

    static void Walk(string file, JsonNode? node, SortedSet<string> errors)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (key, value) in obj)
                {
                    if (IsNumeric(value) && !IsGrandfathered(file, key) && Problem(key) is { } problem)
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
                    if (IsNumeric(value))
                    {
                        seen.Add((file, key));
                        seen.Add((Folder(file), key));
                    }
                    Keys(file, value, seen);
                }
                break;
            case JsonArray array:
                foreach (var item in array) Keys(file, item, seen);
                break;
        }
    }

    static bool IsGrandfathered(string file, string key) =>
        _grandfathered.Contains((file, key)) || _grandfathered.Contains((Folder(file), key));

    /// <summary>The folder form of a data file: <c>parks/crystal-rink.json</c> is <c>parks/*.json</c>.</summary>
    static string Folder(string file)
    {
        var slash = file.LastIndexOf('/');
        return (slash < 0 ? "" : file[..(slash + 1)]) + "*.json";
    }

    static bool IsNumeric(JsonNode? value) => value switch
    {
        JsonValue v => v.GetValueKind() == System.Text.Json.JsonValueKind.Number,
        JsonArray a => a.Count > 0 && a.All(IsNumeric),
        _ => false
    };
}
