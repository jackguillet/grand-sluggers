namespace GrandSluggers.Sim;

/// <summary>
/// Exhibition verbs. The game is gamepad only: pad 1 and pad 2, one scheme.
/// F1/F2/F3 are editor-only developer keys, never a player verb. Living spec: docs/how-to-play.md.
/// </summary>
public static class Scheme
{
    public sealed record Verb(string Id, string Pad);

    public static readonly IReadOnlyList<Verb> Product =
    [
        new("confirm", "South"),
        new("charge", "RT hold / release"),
        new("star", "LT hold at RT release"),
        new("aim-run", "Left stick"),
        new("bags", "Right stick flick"),
        new("all-advance", "LB"),
        new("all-return", "RB"),
        new("steal", "LB"),
        new("cyclePitch", "West"),
        new("swap", "LB"),
        new("bunt-third", "West hold"),
        new("bunt-first", "North hold"),
        new("cancel-swing", "East before release"),
        new("cutoff", "RB"),
        new("cancel-throw", "East while queued"),
        new("freeze", "D-pad Up"),
        new("all-select", "D-pad Down"),
        new("call-time", "Start"),
        new("how-to", "View / Select"),
        new("dash", "South mash"),
        new("pickoff", "Right stick held + RT"),
        new("skip", "East"),
        new("attack", "West"),
        new("dive", "East"),
        new("jump", "North"),
        new("throw", "RT"),
        new("catch", "Automatic by position"),
    ];

    public static readonly IReadOnlyList<string> DebugKeys = ["F1", "F2", "F3"];

    public static Verb Must(string id) =>
        Product.FirstOrDefault(v => v.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"No product verb '{id}'");

    public static string Pad(string id) => Must(id).Pad;

    public static bool IsDebug(string key) =>
        DebugKeys.Any(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));

    public static bool IsProductVerb(string id) =>
        Product.Any(v => v.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
