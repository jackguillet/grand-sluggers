using System.Reflection;

namespace GrandSluggers.Sim;

/// <summary>
/// The one way the game reads a data file (#1031). Every catalog under <c>data/</c> — characters, parks, bats, gloves,
/// chemistry, star skills, art, tutorials — goes through <see cref="Read{T}"/> or <see cref="Require{T}"/>, and every
/// other loader shares <see cref="Options"/> and <see cref="Document"/>: comments and trailing commas allowed, names
/// matched case-insensitively. A key the target type does not declare is an error that names the file and the key
/// (spec §16, FR-03); before this, <c>System.Text.Json</c> dropped it in silence, so a misspelled key played the
/// default and no test failed.
/// </summary>
public static class DataJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static readonly JsonDocumentOptions Document = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>Read one file strictly, or throw with every error it has.</summary>
    public static T Require<T>(string path) where T : class
    {
        var errors = new List<string>();
        var value = Read<T>(path, errors);
        if (errors.Count > 0 || value is null)
            throw new InvalidDataException("Invalid data file:" + Environment.NewLine
                + string.Join(Environment.NewLine, (errors.Count > 0 ? errors : [$"{path}: JSON document is empty"]).Select(e => "  - " + e)));
        return value;
    }

    public static T? Read<T>(string path, List<string> errors) where T : class
    {
        try
        {
            var text = File.ReadAllText(path);
            UnknownKeys(text, typeof(T), path, errors);
            var value = JsonSerializer.Deserialize<T>(text, Options);
            if (value is null) errors.Add($"{path}: JSON document is empty");
            return value;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            errors.Add($"{path}: cannot read gameplay data: {ex.Message}");
            return null;
        }
    }


    /// <summary>
    /// The strict read (spec §16, FR-03, #820): a key the row type does not declare stops the load and
    /// names the file and the key, the way <see cref="RulesValidation.UnknownFields"/> does for a rule
    /// table. Before this, <c>System.Text.Json</c> dropped an unknown park key in silence, so a
    /// misspelled <c>centerFenceFt</c> played Harbor's fence and no test failed.
    ///
    /// Rows nested in a list (a park's hazards) are walked against their own row type; a malformed
    /// document is left to <see cref="JsonSerializer"/>, which reports it with the parser's message.
    /// </summary>
    static void UnknownKeys(string text, Type type, string source, List<string> errors)
    {
        JsonDocument doc;
        try { doc = JsonDocument.Parse(text, Document); }
        catch (JsonException) { return; }
        using (doc)
        {
            // A file that is a list of rows (role-players.json) walks each row against the row type.
            if (doc.RootElement.ValueKind == JsonValueKind.Array && RowType(type) is { } row)
            {
                var i = 0;
                foreach (var entry in doc.RootElement.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Null)
                        UnknownKeys(entry, row, $"[{i}].", source, errors);
                    i++;
                }
            }
            else
                UnknownKeys(doc.RootElement, type, "", source, errors);
        }
    }

    static void UnknownKeys(JsonElement element, Type type, string path, string source, List<string> errors)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{source}: {(path.Length == 0 ? "the row" : path.TrimEnd('.'))} must be an object; got {element.ValueKind}");
            return;
        }
        var declared = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
        foreach (var field in element.EnumerateObject())
        {
            if (!declared.TryGetValue(field.Name, out var p))
            {
                var names = declared.Values.Select(x => Camel(x.Name)).OrderBy(x => x, StringComparer.Ordinal);
                var why = type.GetCustomAttribute<OnlyKeysAttribute>()?.Why;
                errors.Add($"{source}: {path}{field.Name} is not a key this file declares; "
                    + $"the keys of {Camel(TypeLabel(type))} are [{string.Join(", ", names)}]"
                    + (why is null ? "" : "; " + why));
                continue;
            }
            if (RowType(p.PropertyType) is { } row && field.Value.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var entry in field.Value.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Null)
                        UnknownKeys(entry, row, $"{path}{Camel(p.Name)}[{i}].", source, errors);
                    i++;
                }
            }
            else if (DictionaryRowType(p.PropertyType) is { } keyed && field.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var entry in field.Value.EnumerateObject())
                {
                    if (entry.Value.ValueKind != JsonValueKind.Null)
                        UnknownKeys(entry.Value, keyed, $"{path}{Camel(p.Name)}.{entry.Name}.", source, errors);
                }
            }
            else if (Nested(p.PropertyType))
                UnknownKeys(field.Value, p.PropertyType, $"{path}{Camel(p.Name)}.", source, errors);
        }
    }

    /// <summary>The row type behind a <c>List&lt;T?&gt;</c> or <c>T[]</c> of authored rows, else null.</summary>
    static Type? RowType(Type type)
    {
        if (type.IsArray)
        {
            var element = type.GetElementType()!;
            element = Nullable.GetUnderlyingType(element) ?? element;
            return Nested(element) ? element : null;
        }
        if (!type.IsGenericType) return null;
        var arg = type.GetGenericArguments()[0];
        arg = Nullable.GetUnderlyingType(arg) ?? arg;
        return Nested(arg) ? arg : null;
    }

    /// <summary>The row type behind a <c>Dictionary&lt;string, T?&gt;</c> of authored rows keyed by id, else null.</summary>
    static Type? DictionaryRowType(Type type)
    {
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Dictionary<,>)) return null;
        var args = type.GetGenericArguments();
        if (args[0] != typeof(string)) return null;
        var arg = Nullable.GetUnderlyingType(args[1]) ?? args[1];
        return Nested(arg) ? arg : null;
    }

    static bool Nested(Type type) =>
        type.IsClass && type != typeof(string) && !type.IsArray && type.Namespace == typeof(DataJson).Namespace
        && !typeof(System.Collections.IEnumerable).IsAssignableFrom(type);

    static string TypeLabel(Type type) =>
        type.Name.EndsWith("Dto", StringComparison.Ordinal) ? type.Name[..^3] : type.Name;

    static string Camel(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
