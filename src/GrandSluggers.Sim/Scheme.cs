namespace GrandSluggers.Sim;

/// <summary>
/// Exhibition verbs: pad is the couch product, keyboard is the same scheme.
/// F1/F2/F3 stay debug. Living spec: docs/how-to-play.md.
/// </summary>
public static class Scheme
{
    public sealed record Verb(string Id, string Pad, string Keys);

    public static readonly IReadOnlyList<Verb> Product =
    [
        new("confirm", "South", "Space / Enter"),
        new("charge", "LT", "Shift"),
        new("star", "North", "Q"),
        new("aim-run", "Left stick", "WASD"),
        new("bags", "D-pad", "1 2 3 4"),
        new("all-advance", "LB", ","),
        new("all-return", "RB", "."),
        new("steal", "L3", "Z"),
        new("changeup", "West", "V"),
        new("swap", "Select", "R"),
        new("bunt", "West hold", "V"),
        new("cutoff", "LB", "X"),
        new("freeze", "LB+RB", "/"),
        new("call-time", "Start", "H"),
        new("dash", "South mash", "Space mash"),
        new("pickoff", "D-pad + South", "1 2 3 + Space"),
        new("skip", "East", "G"),
    ];

    public static readonly IReadOnlyList<string> DebugKeys = ["F1", "F2", "F3"];

    public static Verb Must(string id) =>
        Product.FirstOrDefault(v => v.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"No product verb '{id}'");

    public static string Keys(string id) => Must(id).Keys;

    public static string Pad(string id) => Must(id).Pad;

    public static bool IsDebug(string key) =>
        DebugKeys.Any(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));

    public static bool IsProductVerb(string id) =>
        Product.Any(v => v.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
