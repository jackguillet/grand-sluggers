using System.Text.Json;
using System.Text.Json.Nodes;

namespace GrandSluggers.Sim.Tooling;

/// <summary>
/// Stage-save DCC as a catalog (docs/agent-rails.md §6, #653). JSON names the
/// five checkpoints. One-shotting a captain extra or a kit mesh is a patch.
/// </summary>
public sealed class DccStages
{
    public const string Directory = AgentData.Directory;
    public const string FileName = "dcc-stages.json";
    public const string OneShotBanned = "banned";

    public static readonly IReadOnlyList<string> RequiredStageIds =
        ["blocking", "fill", "motion", "export", "still"];

    public static readonly IReadOnlyDictionary<string, string> RequiredKinds =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["blocking"] = "clay",
            ["fill"] = "clay",
            ["motion"] = "sheet",
            ["export"] = "fbx",
            ["still"] = "dual"
        };

    public static readonly IReadOnlyDictionary<string, string> CharacterScripts =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["blocking"] = "tools/blender/hero_shared_blockout.py",
            ["fill"] = "tools/blender/hero_shared_extras.py",
            ["motion"] = "tools/blender/hero_shared_takes.py",
            ["export"] = "tools/blender/",
            ["still"] = "tools/dcc-still.sh"
        };

    public static readonly IReadOnlyDictionary<string, string> CharacterFlags =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["blocking"] = "--clay",
            ["fill"] = "--clay",
            ["motion"] = "--sheets",
            ["export"] = "--out",
            ["still"] = "tools/still-gate-character.sh {id}"
        };

    public static readonly IReadOnlyDictionary<string, string> CharacterCheckpoints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["blocking"] = "scratchpad/takes/body.png",
            ["fill"] = "scratchpad/takes/extras.png",
            ["motion"] = "scratchpad/takes/{clip}.png",
            ["export"] = "unity/Assets/Art/",
            ["still"] = "scratchpad/stills/"
        };

    public static readonly IReadOnlyDictionary<string, string> HarborScripts =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["blocking"] = "tools/blender/harbor_kit.py",
            ["fill"] = "tools/blender/harbor_kit.py",
            ["export"] = "tools/blender/harbor_kit.py",
            ["still"] = "tools/dcc-still.sh harbor"
        };

    public static readonly IReadOnlyDictionary<string, string> HarborFlags =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["blocking"] = "--clay",
            ["fill"] = "--clay",
            ["export"] = "--out",
            ["still"] = "tools/still-gate.sh"
        };

    public static readonly IReadOnlyDictionary<string, string> HarborCheckpoints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["blocking"] = "scratchpad/takes/harbor-kit.png",
            ["fill"] = "scratchpad/takes/harbor-kit.png",
            ["export"] = "unity/Assets/Art/Parks/harbor-diamond/harbor-kit.fbx",
            ["still"] = "scratchpad/stills/"
        };

    static readonly HashSet<string> TopFields = new(StringComparer.Ordinal)
    {
        "oneShot", "stages"
    };
    static readonly HashSet<string> StageFields = new(StringComparer.Ordinal)
    {
        "id", "n", "kind", "character", "harbor"
    };
    static readonly HashSet<string> LaneFields = new(StringComparer.Ordinal)
    {
        "script", "work", "flag", "checkpoint"
    };
    static readonly HashSet<string> SkipLaneFields = new(StringComparer.Ordinal)
    {
        "skip"
    };

    public string OneShot { get; }
    public IReadOnlyList<DccStage> Stages { get; }

    DccStages(string oneShot, IReadOnlyList<DccStage> stages)
    {
        OneShot = oneShot;
        Stages = stages;
    }

    /// <summary>Empty catalog. Used only when the JSON file is missing.</summary>
    public static DccStages Defaults => new("", []);

    public static string PathFor(DataRoot dataRoot) =>
        dataRoot.Resolve(Directory, FileName);

    public static DccStages Load(DataRoot dataRoot)
    {
        var errors = new List<string>();
        var catalog = Load(dataRoot, errors);
        if (errors.Count > 0)
            throw new InvalidDataException("Invalid dcc stages:" + Environment.NewLine
                + string.Join(Environment.NewLine, errors.Select(e => "  - " + e)));
        return catalog;
    }

    public static DccStages Load(DataRoot dataRoot, List<string> errors)
    {
        var path = PathFor(dataRoot);
        if (!File.Exists(path))
            return Defaults;

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(File.ReadAllText(path), documentOptions: DataJson.Document);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            errors.Add($"{path}: cannot read agent catalog: {ex.Message}");
            return Defaults;
        }

        ValidateDocument(node, path, errors);
        if (node is not JsonObject obj)
            return Defaults;

        var oneShot = Text(obj, "oneShot");
        var stages = new List<DccStage>();
        if (obj["stages"] is JsonArray rows)
        {
            foreach (var row in rows)
            {
                if (row is not JsonObject stage) continue;
                stages.Add(new DccStage(
                    Text(stage, "id"),
                    Number(stage, "n"),
                    Text(stage, "kind"),
                    ReadLane(stage["character"] as JsonObject),
                    ReadLane(stage["harbor"] as JsonObject)));
            }
        }

        return new DccStages(oneShot, stages);
    }

    public static IReadOnlyList<string> Validate(DataRoot dataRoot)
    {
        var errors = new List<string>();
        var path = PathFor(dataRoot);
        if (!File.Exists(path))
        {
            errors.Add($"{path}: required agent catalog is missing");
            return errors;
        }
        Load(dataRoot, errors);
        return errors.OrderBy(e => e, StringComparer.Ordinal).ToList();
    }

    static void ValidateDocument(JsonNode? node, string path, List<string> errors)
    {
        if (node is not JsonObject obj)
        {
            errors.Add($"{path}: dcc stages must be an object; got {KindName(node)}");
            return;
        }
        foreach (var field in obj)
        {
            if (!TopFields.Contains(field.Key))
                errors.Add($"{path}: '{field.Key}' is not a dcc-stages field");
        }

        var oneShot = Text(obj, "oneShot");
        if (string.IsNullOrWhiteSpace(oneShot))
            errors.Add($"{path}: oneShot must not be empty");
        else if (!oneShot.Equals(OneShotBanned, StringComparison.Ordinal))
            errors.Add($"{path}: oneShot must be '{OneShotBanned}'; got '{oneShot}'");

        ValidateStages(obj, path, errors);
    }

    static void ValidateStages(JsonObject obj, string path, List<string> errors)
    {
        if (!obj.TryGetPropertyValue("stages", out var node) || node is null)
        {
            errors.Add($"{path}: stages must be an array; got missing");
            return;
        }
        if (node is not JsonArray rows)
        {
            errors.Add($"{path}: stages must be an array; got {KindName(node)}");
            return;
        }

        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < rows.Count; i++)
        {
            var where = $"{path}: stages[{i}]";
            if (rows[i] is not JsonObject row)
            {
                errors.Add($"{where} must be an object; got {KindName(rows[i])}");
                continue;
            }
            foreach (var field in row)
            {
                if (!StageFields.Contains(field.Key))
                    errors.Add($"{where} '{field.Key}' is not a dcc-stages stage field");
            }

            var id = Text(row, "id");
            Required(where, "id", id, errors);
            var kind = Text(row, "kind");
            Required(where, "kind", kind, errors);
            var n = Number(row, "n");
            if (!row.TryGetPropertyValue("n", out var nNode) || nNode is null)
                errors.Add($"{where} n must be an integer; got missing");
            else if (nNode is not JsonValue value || value.GetValueKind() != JsonValueKind.Number || n < 1)
                errors.Add($"{where} n must be an integer; got {KindName(nNode)}");
            else if (n != i + 1)
                errors.Add($"{where} n must be {i + 1}; got {n}");

            if (!string.IsNullOrWhiteSpace(id) && RequiredKinds.TryGetValue(id, out var needKind)
                && !string.IsNullOrWhiteSpace(kind) && !kind.Equals(needKind, StringComparison.Ordinal))
            {
                errors.Add($"{where} kind for '{id}' must be '{needKind}'; got '{kind}'");
            }

            ValidateLane(row, "character", where, id, skipAllowed: false, errors);
            ValidateLane(row, "harbor", where, id, skipAllowed: id == "motion", errors);

            if (string.IsNullOrWhiteSpace(id)) continue;
            if (seen.TryGetValue(id, out var first))
                errors.Add($"{path}: duplicate dcc-stages id '{id.ToLowerInvariant()}' (case-insensitive): stages[{first}]; stages[{i}]");
            else
                seen[id] = i;
        }

        for (var i = 0; i < RequiredStageIds.Count; i++)
        {
            var need = RequiredStageIds[i];
            if (!seen.ContainsKey(need))
                errors.Add($"{path}: stages missing '{need}'");
            else if (seen[need] != i)
                errors.Add($"{path}: stages[{i}] must be '{need}'");
        }
    }

    static void ValidateLane(
        JsonObject stage,
        string lane,
        string where,
        string id,
        bool skipAllowed,
        List<string> errors)
    {
        var laneWhere = $"{where} {lane}";
        if (!stage.TryGetPropertyValue(lane, out var node) || node is null)
        {
            errors.Add($"{laneWhere} must be an object; got missing");
            return;
        }
        if (node is not JsonObject row)
        {
            errors.Add($"{laneWhere} must be an object; got {KindName(node)}");
            return;
        }

        var skip = Flag(row, "skip");
        if (skip)
        {
            if (!skipAllowed)
                errors.Add($"{laneWhere} skip is only allowed on harbor motion");
            foreach (var field in row)
            {
                if (!SkipLaneFields.Contains(field.Key))
                    errors.Add($"{laneWhere} '{field.Key}' is not a skip-lane field");
            }
            return;
        }

        if (skipAllowed)
        {
            errors.Add($"{laneWhere} must skip; Harbor has no takes");
            return;
        }

        foreach (var field in row)
        {
            if (!LaneFields.Contains(field.Key))
                errors.Add($"{laneWhere} '{field.Key}' is not a dcc-stages lane field");
        }

        var script = Text(row, "script");
        var flag = Text(row, "flag");
        var checkpoint = Text(row, "checkpoint");
        Required(laneWhere, "script", script, errors);
        Required(laneWhere, "work", Text(row, "work"), errors);
        Required(laneWhere, "flag", flag, errors);
        Required(laneWhere, "checkpoint", checkpoint, errors);

        if (lane == "character")
            MatchNamed(laneWhere, id, script, flag, checkpoint, CharacterScripts, CharacterFlags, CharacterCheckpoints, errors);
        else
            MatchNamed(laneWhere, id, script, flag, checkpoint, HarborScripts, HarborFlags, HarborCheckpoints, errors);
    }

    static void MatchNamed(
        string where,
        string id,
        string script,
        string flag,
        string checkpoint,
        IReadOnlyDictionary<string, string> scripts,
        IReadOnlyDictionary<string, string> flags,
        IReadOnlyDictionary<string, string> checkpoints,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        if (scripts.TryGetValue(id, out var needScript)
            && !string.IsNullOrWhiteSpace(script) && !script.Equals(needScript, StringComparison.Ordinal))
            errors.Add($"{where} script for '{id}' must be '{needScript}'; got '{script}'");
        if (flags.TryGetValue(id, out var needFlag)
            && !string.IsNullOrWhiteSpace(flag) && !flag.Equals(needFlag, StringComparison.Ordinal))
            errors.Add($"{where} flag for '{id}' must be '{needFlag}'; got '{flag}'");
        if (checkpoints.TryGetValue(id, out var needCheck)
            && !string.IsNullOrWhiteSpace(checkpoint) && !checkpoint.Equals(needCheck, StringComparison.Ordinal))
            errors.Add($"{where} checkpoint for '{id}' must be '{needCheck}'; got '{checkpoint}'");
    }

    static DccStageLane ReadLane(JsonObject? row)
    {
        if (row is null) return DccStageLane.Missing;
        if (Flag(row, "skip")) return DccStageLane.Skipped;
        return new DccStageLane(
            Text(row, "script"),
            Text(row, "work"),
            Text(row, "flag"),
            Text(row, "checkpoint"),
            false);
    }

    static void Required(string where, string field, string value, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{where} {field} must not be empty");
    }

    static string Text(JsonObject row, string field)
    {
        if (!row.TryGetPropertyValue(field, out var node) || node is null) return "";
        return TextNode(node);
    }

    static string TextNode(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) ? text : "";

    static int Number(JsonObject row, string field)
    {
        if (!row.TryGetPropertyValue(field, out var node) || node is not JsonValue value)
            return 0;
        if (value.TryGetValue<int>(out var n)) return n;
        if (value.TryGetValue<long>(out var l) && l is >= int.MinValue and <= int.MaxValue)
            return (int)l;
        return 0;
    }

    static bool Flag(JsonObject row, string field)
    {
        if (!row.TryGetPropertyValue(field, out var node) || node is not JsonValue value)
            return false;
        return value.GetValueKind() == JsonValueKind.True;
    }

    static string KindName(JsonNode? node) =>
        node is null ? "null" : node.GetValueKind().ToString().ToLowerInvariant();
}

public sealed record DccStage(
    string Id,
    int N,
    string Kind,
    DccStageLane Character,
    DccStageLane Harbor);

public sealed record DccStageLane(
    string Script,
    string Work,
    string Flag,
    string Checkpoint,
    bool Skip)
{
    public static DccStageLane Missing { get; } = new("", "", "", "", false);
    public static DccStageLane Skipped { get; } = new("", "", "", "", true);
}
