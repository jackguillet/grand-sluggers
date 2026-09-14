using System.Text.Json;
using System.Text.Json.Nodes;

namespace GrandSluggers.Sim;

/// <summary>
/// Sitting and still-gate memory as a catalog (docs/agent-rails.md §2, #649).
/// JSON is the source of truth; <see cref="Defaults"/> is only the load fallback
/// when the file is missing. Recurring signatures promote to a test; GitHub
/// issues stay, they are not the memory.
/// </summary>
public sealed class DebugProtocol
{
    public const string Directory = "agent";
    public const string FileName = "debug-protocol.json";

    public static readonly IReadOnlyList<string> Stages =
        ["cli-match", "dcc", "sitting", "sim", "still-gate", "unity-console"];

    static readonly HashSet<string> StageSet = new(Stages, StringComparer.Ordinal);
    static readonly HashSet<string> RowFields = new(StringComparer.Ordinal)
    {
        "id", "signature", "stage", "cause", "fix", "promoted", "issue", "pr"
    };

    static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    static readonly JsonDocumentOptions Document = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public IReadOnlyList<DebugProtocolEntry> Entries { get; }

    DebugProtocol(IReadOnlyList<DebugProtocolEntry> entries) => Entries = entries;

    /// <summary>Empty catalog. Used only when the JSON file is missing.</summary>
    public static DebugProtocol Defaults => new([]);

    public static string PathFor(string dataRoot) =>
        Path.Combine(dataRoot, Directory, FileName);

    public static DebugProtocol Load(string dataRoot)
    {
        var errors = new List<string>();
        var protocol = Load(dataRoot, errors);
        if (errors.Count > 0)
            throw new InvalidDataException("Invalid debug protocol:" + Environment.NewLine
                + string.Join(Environment.NewLine, errors.Select(e => "  - " + e)));
        return protocol;
    }

    public static DebugProtocol Load(string dataRoot, List<string> errors)
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
        if (node is not JsonObject obj || obj["entries"] is not JsonArray rows)
            return Defaults;

        var entries = new List<DebugProtocolEntry>();
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i] is not JsonObject) continue;
            var dto = rows[i].Deserialize<DebugProtocolEntryDto>(Json);
            if (dto is null) continue;
            entries.Add(new DebugProtocolEntry(
                dto.Id ?? "", dto.Signature ?? "", dto.Stage ?? "",
                dto.Cause ?? "", dto.Fix ?? "", dto.Promoted ?? "",
                dto.Issue ?? "", dto.Pr ?? ""));
        }
        return new DebugProtocol(entries);
    }

    public static IReadOnlyList<string> Validate(string dataRoot)
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
            errors.Add($"{path}: debug protocol must be an object; got {Kind(node)}");
            return;
        }
        foreach (var field in obj)
        {
            if (field.Key == "entries") continue;
            errors.Add($"{path}: '{field.Key}' is not a debug-protocol field");
        }
        if (!obj.TryGetPropertyValue("entries", out var entriesNode))
        {
            errors.Add($"{path}: entries must be an array; got missing");
            return;
        }
        if (entriesNode is not JsonArray rows)
        {
            errors.Add($"{path}: entries must be an array; got {Kind(entriesNode)}");
            return;
        }

        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < rows.Count; i++)
        {
            var where = $"{path}: entries[{i}]";
            if (rows[i] is not JsonObject row)
            {
                errors.Add($"{where} must be an object; got {Kind(rows[i])}");
                continue;
            }
            foreach (var field in row)
            {
                if (!RowFields.Contains(field.Key))
                    errors.Add($"{where} '{field.Key}' is not a debug-protocol field");
            }
            var id = Text(row, "id");
            Required(where, "id", id, errors);
            Required(where, "signature", Text(row, "signature"), errors);
            var stage = Text(row, "stage");
            Required(where, "stage", stage, errors);
            if (!string.IsNullOrWhiteSpace(stage) && !StageSet.Contains(stage))
                errors.Add($"{where} stage must be one of [{string.Join(", ", Stages)}]; got '{stage}'");
            Required(where, "cause", Text(row, "cause"), errors);
            Required(where, "fix", Text(row, "fix"), errors);
            Required(where, "issue", Text(row, "issue"), errors);
            MustBeString(where, "promoted", row, errors);
            MustBeString(where, "pr", row, errors);
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (seen.TryGetValue(id, out var first))
                errors.Add($"{path}: duplicate debug-protocol id '{id.ToLowerInvariant()}' (case-insensitive): entries[{first}]; entries[{i}]");
            else
                seen[id] = i;
        }
    }

    static void Required(string where, string field, string value, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{where} {field} must not be empty");
    }

    static void MustBeString(string where, string field, JsonObject row, List<string> errors)
    {
        if (!row.TryGetPropertyValue(field, out var node) || node is null) return;
        if (node is not JsonValue value || value.GetValueKind() is not (JsonValueKind.String or JsonValueKind.Null))
            errors.Add($"{where} {field} must be a string; got {Kind(node)}");
    }

    static string Text(JsonObject row, string field)
    {
        if (!row.TryGetPropertyValue(field, out var node) || node is null) return "";
        return node is JsonValue value && value.TryGetValue<string>(out var text) ? text : "";
    }

    static string Kind(JsonNode? node) => node is null ? "null" : node.GetValueKind().ToString().ToLowerInvariant();

    sealed class DebugProtocolEntryDto
    {
        public string? Id { get; set; }
        public string? Signature { get; set; }
        public string? Stage { get; set; }
        public string? Cause { get; set; }
        public string? Fix { get; set; }
        public string? Promoted { get; set; }
        public string? Issue { get; set; }
        public string? Pr { get; set; }
    }
}

public sealed record DebugProtocolEntry(
    string Id,
    string Signature,
    string Stage,
    string Cause,
    string Fix,
    string Promoted,
    string Issue,
    string Pr);
