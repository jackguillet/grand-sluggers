namespace GrandSluggers.Sim;

/// <summary>Research catalog validation; never a source of gameplay defaults. Unknown values stay null.</summary>
public static class RaceEvidence
{
    public const string FileName = "agent/race-evidence.json";
    public static IReadOnlyList<string> Validate(string root)
    {
        var path = Path.Combine(root, FileName);
        try { return ValidateJson(File.ReadAllText(path)).Select(e => $"{path}: {e}").ToArray(); }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        { return new[] { $"{path}: {e.Message}" }; }
    }

    public static IReadOnlyList<string> ValidateJson(string json)
    {
        var errors = new List<string>();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) return new[] { "expected an object" };
        if (!root.TryGetProperty("schemaVersion", out var schema) || schema.ValueKind != JsonValueKind.Number || !schema.TryGetInt32(out var version) || version != 1)
            errors.Add("schemaVersion must be 1");
        var sources = Rows("sources");
        var decisions = Rows("decisions");
        var fixtures = Rows("fixtures");
        var budgets = Rows("budgets");
        foreach (var (id, row) in sources)
        {
            Required(row, "locator", id); Required(row, "uncertainty", id);
            Choice(row, "status", id, "measured", "community", "baseline", "pending");
        }
        foreach (var (id, row) in decisions)
        {
            Required(row, "locator", id);
            Choice(row, "state", id, "accepted", "pending");
        }
        foreach (var (id, row) in fixtures)
        {
            Required(row, "locator", id);
            Choice(row, "kind", id, "fixed-input", "tactical", "cohort");
        }
        foreach (var (id, row) in budgets)
        {
            var source = Reference(row, "source", id, sources);
            var decision = Reference(row, "decision", id, decisions);
            Reference(row, "fixture", id, fixtures);
            Choice(row, "unit", id, "s", "ft", "ft/s", "ft/s^2", "ratio", "runs/side/game");
            var state = Choice(row, "state", id, "accepted", "pending");
            Required(row, "uncertainty", id);
            Choice(row, "purpose", id, "guardrail", "measurement", "target");
            var lower = Bound(row, "lower", id); var upper = Bound(row, "upper", id);
            if (lower.HasValue != upper.HasValue) errors.Add($"{id}: bounds must both be specified or both null");
            if (lower > upper) errors.Add($"{id}: inverted bounds");
            if (state == "accepted" && (lower is null || upper is null)) errors.Add($"{id}: accepted bounds required");
            if (state == "accepted" && Text(decision, "state") != "accepted") errors.Add($"{id}: accepting decision must be accepted");
            if (!row.TryGetProperty("activeDefault", out var active) || active.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                errors.Add($"{id}: activeDefault must be explicit boolean");
            else if (active.GetBoolean() && (state != "accepted" || lower is null || upper is null
                || Text(decision, "state") != "accepted" || Text(source, "status") == "pending"))
                errors.Add($"{id}: pending/unbounded evidence cannot be an active default");
        }
        return errors.OrderBy(e => e, StringComparer.Ordinal).ToArray();

        Dictionary<string, JsonElement> Rows(string key)
        {
            var rows = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            if (!root.TryGetProperty(key, out var array) || array.ValueKind != JsonValueKind.Array)
            { errors.Add($"{key}: required array"); return rows; }
            foreach (var row in array.EnumerateArray())
            {
                var id = Required(row, "id", key);
                if (id.Length == 0) continue;
                if (!rows.TryAdd(id, row)) errors.Add($"{key}: duplicate id '{id}'");
            }
            return rows;
        }
        string Required(JsonElement row, string key, string id)
        {
            var value = Text(row, key);
            if (string.IsNullOrWhiteSpace(value)) errors.Add($"{id}: missing {key}");
            return value;
        }
        string Choice(JsonElement row, string key, string id, params string[] choices)
        {
            var value = Required(row, key, id);
            if (!choices.Contains(value)) errors.Add($"{id}: invalid {key} '{value}'");
            return value;
        }
        JsonElement Reference(JsonElement row, string key, string id, Dictionary<string, JsonElement> table)
        {
            var value = Required(row, key, id);
            if (table.TryGetValue(value, out var found)) return found;
            errors.Add($"{id}: unknown {key} '{value}'"); return default;
        }
        double? Bound(JsonElement row, string key, string id)
        {
            if (!row.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null) return null;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var n) && !double.IsNaN(n) && !double.IsInfinity(n)) return n;
            errors.Add($"{id}: {key} must be finite number or null"); return null;
        }
    }

    static string Text(JsonElement row, string key) => row.ValueKind == JsonValueKind.Object
        && row.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
}
