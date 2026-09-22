using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The off paths of the switches #860 and #883 turned on in the shipped data (Jack accepted the
/// <c>trials/pitch5</c> duel window on September 22, 2026: "trial was good.", and the stick trial the
/// same day: "approve all"). The switches and their
/// off paths stay in code until a cleanup child removes them, and they stay tested — but a row that
/// asserts an off path must <b>build</b> it, never read it from shipped data. Each root here is the
/// shipped data root copied once per test process, with one named key changed and nothing else.
/// </summary>
static class SwitchOffPaths
{
    static readonly JsonDocumentOptions JsonComments = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    static readonly Lazy<DataRoot> _splitWindow = new(() =>
        Copy("split-window", "batting.json", json => json["window"]!["shared"] = false));

    /// <summary>The shipped root with <c>batting.window.shared</c> false: slap 9 / charge 7 ± Contact × the rung.</summary>
    public static DataRoot SplitWindow => _splitWindow.Value;

    /// <summary>The rules table on <see cref="SplitWindow"/>.</summary>
    public static RulesTable SplitWindowRules => _splitWindowRules.Value;
    static readonly Lazy<RulesTable> _splitWindowRules = new(() => RulesTable.Load(SplitWindow));

    /// <summary>The whole catalog on <see cref="SplitWindow"/>, for rows that play a match.</summary>
    public static ContentCatalog SplitWindowContent => _splitWindowContent.Value;
    static readonly Lazy<ContentCatalog> _splitWindowContent = new(() => ContentCatalog.Load(SplitWindow));

    static readonly Lazy<DataRoot> _stickShapes = new(() =>
        Copy("stick-shapes", "batting.json", json => json["geometryOnly"] = false));

    /// <summary>
    /// The shipped root with <c>batting.geometryOnly</c> false: stick L/R adds <c>spray.stickDeg</c>
    /// and stick U/D takes <c>launch.stickDeg</c> off the launch on every swing, and the CPU batter
    /// draws both aims (what played before #883).
    /// </summary>
    public static DataRoot StickShapes => _stickShapes.Value;

    /// <summary>The rules table on <see cref="StickShapes"/>.</summary>
    public static RulesTable StickShapesRules => _stickShapesRules.Value;
    static readonly Lazy<RulesTable> _stickShapesRules = new(() => RulesTable.Load(StickShapes));

    /// <summary>The whole catalog on <see cref="StickShapes"/>, for rows that play a match.</summary>
    public static ContentCatalog StickShapesContent => _stickShapesContent.Value;
    static readonly Lazy<ContentCatalog> _stickShapesContent = new(() => ContentCatalog.Load(StickShapes));

    static DataRoot Copy(string name, string file, Action<JsonObject> edit)
    {
        var source = ContentCatalog.Load().Root.Shipped;
        var root = Path.Combine(Path.GetTempPath(), $"grand-sluggers-off-{name}-{Guid.NewGuid():N}");
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(root, Path.GetRelativePath(source, dir)));
        Directory.CreateDirectory(root);
        foreach (var f in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(f, Path.Combine(root, Path.GetRelativePath(source, f)));

        var path = Path.Combine(root, RulesTable.Directory, file);
        var json = JsonNode.Parse(File.ReadAllText(path), null, JsonComments)!.AsObject();
        edit(json);
        File.WriteAllText(path, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { Directory.Delete(root, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        };
        return new DataRoot(root);
    }
}
