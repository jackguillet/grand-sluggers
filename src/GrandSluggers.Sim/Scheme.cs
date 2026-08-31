namespace GrandSluggers.Sim;

/// <summary>
/// Exhibition verbs: pad is the couch product. Keyboard and mouse are the same scheme (player 1).
/// F1/F2/F3 stay debug. Living spec: docs/how-to-play.md.
/// </summary>
public static class Scheme
{
    public sealed record Verb(string Id, string Pad, string Keys, string Mouse);

    public static readonly IReadOnlyList<Verb> Product =
    [
        new("confirm", "South", "Space / Enter", "Left click"),
        new("charge", "LT", "Shift", "Right click hold"),
        new("star", "North", "Q", "Middle click"),
        new("aim-run", "Left stick", "WASD", "Mouse move"),
        new("bags", "D-pad", "1 2 3 4", "Click bag / mouse quadrant"),
        new("all-advance", "LB", ",", "Click advance"),
        new("all-return", "RB", ".", "Click return"),
        new("steal", "L3", "Z", "Click steal"),
        new("changeup", "West", "V", "Left Ctrl"),
        new("swap", "Select", "R", "Click swap"),
        new("bunt", "West hold", "V", "Left Ctrl hold in the box"),
        new("cutoff", "LB", "X", "Click relay"),
        new("freeze", "LB+RB", "/", "Click freeze"),
        new("call-time", "Start", "H / Esc", "Esc"),
        new("dash", "South mash", "Space mash", "Left click mash"),
        new("pickoff", "D-pad + South", "1 2 3 + Space", "Click bag + left click"),
        new("skip", "East", "G", "Right click (menus)"),
        new("attack", "North (in-play)", "B", "Middle click (in-play)"),
        new("dive", "East tap", "G", "Left click while dashing"),
        new("jump", "West", "F", "Click jump"),
    ];

    public static readonly IReadOnlyList<string> DebugKeys = ["F1", "F2", "F3"];

    public static Verb Must(string id) =>
        Product.FirstOrDefault(v => v.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"No product verb '{id}'");

    public static string Keys(string id) => Must(id).Keys;

    public static string Pad(string id) => Must(id).Pad;

    public static string Mouse(string id) => Must(id).Mouse;

    public static bool IsDebug(string key) =>
        DebugKeys.Any(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));

    public static bool IsProductVerb(string id) =>
        Product.Any(v => v.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
