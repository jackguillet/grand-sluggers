using System.Text.Json;
using System.Text.Json.Nodes;

namespace GrandSluggers.Sim;

/// <summary>
/// Dual stills as a catalog (docs/agent-rails.md §4, #651). JSON names the DCC
/// still, the in-game still, and a critic that files. It does not compare pixels.
/// </summary>
public sealed class DualStills
{
    public const string Directory = "agent";
    public const string FileName = "dual-stills.json";
    public const string DropFolder = "scratchpad/stills";
    public const string CriticSkill = ".grok/skills/look-critic/SKILL.md";

    public static readonly IReadOnlyList<string> RequiredTriggers =
    [
        "tools/blender/",
        "unity/Assets/Art/Animation/Clips/",
        "unity/Assets/Art/Characters/",
        "unity/Assets/Art/Parks/harbor-diamond/"
    ];

    public static readonly IReadOnlyList<string> RequiredKindIds =
        ["body", "extras", "harbor-kit", "takes"];

    public static readonly IReadOnlyDictionary<string, string> NamedDcc =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["body"] = "dcc-body.png",
            ["extras"] = "dcc-extras.png",
            ["takes"] = "dcc-{clip}.png",
            ["harbor-kit"] = "dcc-harbor-kit.png"
        };

    public static readonly IReadOnlyList<string> RequiredRubric =
        ["docs/screenshot-gate.md", "docs/silhouette-bible.md"];

    static readonly HashSet<string> TopFields = new(StringComparer.Ordinal)
    {
        "drop", "triggers", "kinds", "critic"
    };
    static readonly HashSet<string> KindFields = new(StringComparer.Ordinal)
    {
        "id", "dcc", "dccCommand", "inGame", "inGameCommand"
    };
    static readonly HashSet<string> CriticFields = new(StringComparer.Ordinal)
    {
        "skill", "rubric", "mayPassLook", "mayClose188", "mayEditRubric"
    };

    static readonly JsonDocumentOptions Document = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public string Drop { get; }
    public IReadOnlyList<string> Triggers { get; }
    public IReadOnlyList<DualStillsKind> Kinds { get; }
    public DualStillsCritic Critic { get; }

    DualStills(
        string drop,
        IReadOnlyList<string> triggers,
        IReadOnlyList<DualStillsKind> kinds,
        DualStillsCritic critic)
    {
        Drop = drop;
        Triggers = triggers;
        Kinds = kinds;
        Critic = critic;
    }

    /// <summary>Empty catalog. Used only when the JSON file is missing.</summary>
    public static DualStills Defaults => new(
        "",
        [],
        [],
        new DualStillsCritic("", [], false, false, false));

    public static string PathFor(DataRoot dataRoot) =>
        dataRoot.Resolve(Directory, FileName);

    public static DualStills Load(DataRoot dataRoot)
    {
        var errors = new List<string>();
        var catalog = Load(dataRoot, errors);
        if (errors.Count > 0)
            throw new InvalidDataException("Invalid dual stills:" + Environment.NewLine
                + string.Join(Environment.NewLine, errors.Select(e => "  - " + e)));
        return catalog;
    }

    public static DualStills Load(DataRoot dataRoot, List<string> errors)
    {
        var path = PathFor(dataRoot);
        if (!File.Exists(path))
            return Defaults;

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(File.ReadAllText(path), documentOptions: Document);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            errors.Add($"{path}: cannot read agent catalog: {ex.Message}");
            return Defaults;
        }

        ValidateDocument(node, path, errors);
        if (node is not JsonObject obj)
            return Defaults;

        var drop = Text(obj, "drop");
        var triggers = Strings(obj["triggers"]);
        var kinds = new List<DualStillsKind>();
        if (obj["kinds"] is JsonArray rows)
        {
            foreach (var row in rows)
            {
                if (row is not JsonObject kind) continue;
                kinds.Add(new DualStillsKind(
                    Text(kind, "id"),
                    Text(kind, "dcc"),
                    Text(kind, "dccCommand"),
                    Strings(kind["inGame"]),
                    Text(kind, "inGameCommand")));
            }
        }

        DualStillsCritic critic;
        if (obj["critic"] is JsonObject c)
        {
            critic = new DualStillsCritic(
                Text(c, "skill"),
                Strings(c["rubric"]),
                Flag(c, "mayPassLook"),
                Flag(c, "mayClose188"),
                Flag(c, "mayEditRubric"));
        }
        else
        {
            critic = Defaults.Critic;
        }

        return new DualStills(drop, triggers, kinds, critic);
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
            errors.Add($"{path}: dual stills must be an object; got {KindName(node)}");
            return;
        }
        foreach (var field in obj)
        {
            if (!TopFields.Contains(field.Key))
                errors.Add($"{path}: '{field.Key}' is not a dual-stills field");
        }

        var drop = Text(obj, "drop");
        if (string.IsNullOrWhiteSpace(drop))
            errors.Add($"{path}: drop must not be empty");
        else if (!drop.Equals(DropFolder, StringComparison.Ordinal))
            errors.Add($"{path}: drop must be '{DropFolder}'; got '{drop}'");

        ValidateTriggers(obj, path, errors);
        ValidateKinds(obj, path, errors);
        ValidateCritic(obj, path, errors);
    }

    static void ValidateTriggers(JsonObject obj, string path, List<string> errors)
    {
        if (!obj.TryGetPropertyValue("triggers", out var node) || node is null)
        {
            errors.Add($"{path}: triggers must be an array; got missing");
            return;
        }
        if (node is not JsonArray rows)
        {
            errors.Add($"{path}: triggers must be an array; got {KindName(node)}");
            return;
        }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < rows.Count; i++)
        {
            var value = TextNode(rows[i]);
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{path}: triggers[{i}] must not be empty");
                continue;
            }
            if (!seen.Add(value))
                errors.Add($"{path}: duplicate trigger '{value}'");
        }
        foreach (var need in RequiredTriggers)
        {
            if (!seen.Contains(need))
                errors.Add($"{path}: triggers missing '{need}'");
        }
    }

    static void ValidateKinds(JsonObject obj, string path, List<string> errors)
    {
        if (!obj.TryGetPropertyValue("kinds", out var node) || node is null)
        {
            errors.Add($"{path}: kinds must be an array; got missing");
            return;
        }
        if (node is not JsonArray rows)
        {
            errors.Add($"{path}: kinds must be an array; got {KindName(node)}");
            return;
        }

        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < rows.Count; i++)
        {
            var where = $"{path}: kinds[{i}]";
            if (rows[i] is not JsonObject row)
            {
                errors.Add($"{where} must be an object; got {KindName(rows[i])}");
                continue;
            }
            foreach (var field in row)
            {
                if (!KindFields.Contains(field.Key))
                    errors.Add($"{where} '{field.Key}' is not a dual-stills kind field");
            }
            var id = Text(row, "id");
            Required(where, "id", id, errors);
            var dcc = Text(row, "dcc");
            Required(where, "dcc", dcc, errors);
            Required(where, "dccCommand", Text(row, "dccCommand"), errors);
            Required(where, "inGameCommand", Text(row, "inGameCommand"), errors);
            if (!row.TryGetPropertyValue("inGame", out var inGameNode) || inGameNode is null)
                errors.Add($"{where} inGame must be an array; got missing");
            else if (inGameNode is not JsonArray inGame)
                errors.Add($"{where} inGame must be an array; got {KindName(inGameNode)}");
            else if (inGame.Count == 0)
                errors.Add($"{where} inGame must not be empty");
            else
            {
                for (var n = 0; n < inGame.Count; n++)
                {
                    if (string.IsNullOrWhiteSpace(TextNode(inGame[n])))
                        errors.Add($"{where} inGame[{n}] must not be empty");
                }
            }
            if (!string.IsNullOrWhiteSpace(id) && NamedDcc.TryGetValue(id, out var named)
                && !string.IsNullOrWhiteSpace(dcc) && !dcc.Equals(named, StringComparison.Ordinal))
            {
                errors.Add($"{where} dcc for '{id}' must be '{named}'; got '{dcc}'");
            }
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (seen.TryGetValue(id, out var first))
                errors.Add($"{path}: duplicate dual-stills kind '{id.ToLowerInvariant()}' (case-insensitive): kinds[{first}]; kinds[{i}]");
            else
                seen[id] = i;
        }
        foreach (var need in RequiredKindIds)
        {
            if (!seen.ContainsKey(need))
                errors.Add($"{path}: kinds missing '{need}'");
        }
    }

    static void ValidateCritic(JsonObject obj, string path, List<string> errors)
    {
        if (!obj.TryGetPropertyValue("critic", out var node) || node is null)
        {
            errors.Add($"{path}: critic must be an object; got missing");
            return;
        }
        if (node is not JsonObject critic)
        {
            errors.Add($"{path}: critic must be an object; got {KindName(node)}");
            return;
        }
        var where = $"{path}: critic";
        foreach (var field in critic)
        {
            if (!CriticFields.Contains(field.Key))
                errors.Add($"{where} '{field.Key}' is not a dual-stills critic field");
        }
        var skill = Text(critic, "skill");
        Required(where, "skill", skill, errors);
        if (!string.IsNullOrWhiteSpace(skill) && !skill.Equals(CriticSkill, StringComparison.Ordinal))
            errors.Add($"{where} skill must be '{CriticSkill}'; got '{skill}'");

        if (!critic.TryGetPropertyValue("rubric", out var rubricNode) || rubricNode is null)
            errors.Add($"{where} rubric must be an array; got missing");
        else if (rubricNode is not JsonArray rubric)
            errors.Add($"{where} rubric must be an array; got {KindName(rubricNode)}");
        else
        {
            var listed = new HashSet<string>(Strings(rubric), StringComparer.Ordinal);
            foreach (var need in RequiredRubric)
            {
                if (!listed.Contains(need))
                    errors.Add($"{where} rubric missing '{need}'");
            }
        }

        MustBeFalse(where, "mayPassLook", critic, errors);
        MustBeFalse(where, "mayClose188", critic, errors);
        MustBeFalse(where, "mayEditRubric", critic, errors);
    }

    static void MustBeFalse(string where, string field, JsonObject row, List<string> errors)
    {
        if (!row.TryGetPropertyValue(field, out var node) || node is null)
        {
            errors.Add($"{where} {field} must be false; got missing");
            return;
        }
        if (node is not JsonValue value || value.GetValueKind() != JsonValueKind.False)
            errors.Add($"{where} {field} must be false; a critic files, Jack passes look");
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

    static string TextNode(JsonNode? node)
    {
        return node is JsonValue value && value.TryGetValue<string>(out var text) ? text : "";
    }

    static bool Flag(JsonObject row, string field)
    {
        if (!row.TryGetPropertyValue(field, out var node) || node is not JsonValue value)
            return false;
        return value.GetValueKind() == JsonValueKind.True;
    }

    static IReadOnlyList<string> Strings(JsonNode? node)
    {
        if (node is not JsonArray rows) return [];
        var list = new List<string>();
        foreach (var item in rows)
        {
            var text = TextNode(item);
            if (!string.IsNullOrWhiteSpace(text)) list.Add(text);
        }
        return list;
    }

    static string KindName(JsonNode? node) =>
        node is null ? "null" : node.GetValueKind().ToString().ToLowerInvariant();
}

public sealed record DualStillsKind(
    string Id,
    string Dcc,
    string DccCommand,
    IReadOnlyList<string> InGame,
    string InGameCommand);

public sealed record DualStillsCritic(
    string Skill,
    IReadOnlyList<string> Rubric,
    bool MayPassLook,
    bool MayClose188,
    bool MayEditRubric);
