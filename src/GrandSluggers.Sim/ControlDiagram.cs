namespace GrandSluggers.Sim;

/// <summary>
/// How to play Controls page: a drawn pad or a drawn keyboard+mouse.
/// Orange lozenges, green offense, red defense. Same verbs. Never mix schemes.
/// </summary>
public static class ControlDiagram
{
    public sealed record Part(string Id, float U, float V, float W, float H);

    public sealed record Callout(
        string Id,
        string Hardware,
        string Offense,
        string Defense,
        string Always,
        float U,
        float V);

    public static (float X, float Y, float W, float H) Board(float screenW, float screenH)
    {
        var book = HowToPlay.BookPanel(screenW, screenH);
        var top = 88f;
        var foot = 44f;
        return (book.X + 16f, book.Y + top, book.W - 32f, book.H - top - foot - 8f);
    }

    public static IReadOnlyList<Part> Parts(InputScheme scheme) =>
        scheme == InputScheme.Keys ? KeysParts : PadParts;

    public static IReadOnlyList<Callout> Callouts(InputScheme scheme) =>
        scheme == InputScheme.Keys ? KeysCallouts : PadCallouts;

    public static readonly IReadOnlyList<Part> PadParts =
    [
        new("body", 0.34f, 0.30f, 0.32f, 0.46f),
        new("lt", 0.35f, 0.22f, 0.10f, 0.07f),
        new("rt", 0.55f, 0.22f, 0.10f, 0.07f),
        new("lb", 0.35f, 0.28f, 0.10f, 0.05f),
        new("rb", 0.55f, 0.28f, 0.10f, 0.05f),
        new("stick", 0.38f, 0.40f, 0.08f, 0.11f),
        new("dpad", 0.38f, 0.56f, 0.09f, 0.12f),
        new("north", 0.57f, 0.38f, 0.045f, 0.06f),
        new("west", 0.535f, 0.44f, 0.045f, 0.06f),
        new("east", 0.605f, 0.44f, 0.045f, 0.06f),
        new("south", 0.57f, 0.50f, 0.045f, 0.06f),
        new("select", 0.46f, 0.48f, 0.035f, 0.04f),
        new("start", 0.505f, 0.48f, 0.035f, 0.04f),
    ];

    public static readonly IReadOnlyList<Callout> PadCallouts =
    [
        new("stick", "Left stick", "L3 steal", "", "Move / run", 0.02f, 0.38f),
        new("dpad", "D-pad", "", "", "Bags — 1B 2B 3B home", 0.02f, 0.56f),
        new("lt", "LT", "", "", "Charge", 0.02f, 0.20f),
        new("lb", "LB / RB", "All advance / return", "Cutoff", "", 0.02f, 0.28f),
        new("south", "South", "Pitch / swing / dash", "Catch / throw", "", 0.70f, 0.50f),
        new("east", "East", "", "Dive", "Back", 0.70f, 0.40f),
        new("west", "West", "Bunt (hold)", "Changeup / jump", "", 0.70f, 0.30f),
        new("north", "North", "Star swing", "Star pitch / attack", "", 0.70f, 0.20f),
        new("select", "Select", "", "", "Swap glove / pitcher", 0.70f, 0.62f),
        new("start", "Start", "", "", "Call time", 0.70f, 0.72f),
    ];

    public static readonly IReadOnlyList<Part> KeysParts =
    [
        new("wasd-w", 0.36f, 0.34f, 0.06f, 0.07f),
        new("wasd-a", 0.30f, 0.42f, 0.06f, 0.07f),
        new("wasd-s", 0.36f, 0.42f, 0.06f, 0.07f),
        new("wasd-d", 0.42f, 0.42f, 0.06f, 0.07f),
        new("n1", 0.28f, 0.24f, 0.05f, 0.07f),
        new("n2", 0.34f, 0.24f, 0.05f, 0.07f),
        new("n3", 0.40f, 0.24f, 0.05f, 0.07f),
        new("n4", 0.46f, 0.24f, 0.05f, 0.07f),
        new("shift", 0.28f, 0.52f, 0.10f, 0.07f),
        new("space", 0.32f, 0.62f, 0.22f, 0.08f),
        new("mouse", 0.58f, 0.36f, 0.10f, 0.22f),
    ];

    public static readonly IReadOnlyList<Callout> KeysCallouts =
    [
        new("wasd", "WASD", "", "", "Move / run", 0.02f, 0.38f),
        new("bags", "1 2 3 4", "", "", "Bags — 1B 2B 3B home", 0.02f, 0.22f),
        new("space", "Space / left click", "Pitch / swing / dash", "Catch / throw", "", 0.02f, 0.62f),
        new("charge", "Shift / right click", "", "", "Charge", 0.02f, 0.50f),
        new("star", "Q / middle click", "Star swing", "Star pitch / attack", "", 0.72f, 0.20f),
        new("west", "V / Ctrl", "Bunt (hold)", "Changeup", "", 0.72f, 0.32f),
        new("jump", "F / G", "", "Jump / dive", "Back", 0.72f, 0.44f),
        new("steal", "Z", "Steal", "", "", 0.72f, 0.54f),
        new("run", ", / .", "All advance / return", "", "", 0.72f, 0.64f),
        new("esc", "H / Esc", "", "", "Call time / this book", 0.72f, 0.74f),
        new("aim", "Right-drag", "", "", "Aim / run", 0.56f, 0.62f),
    ];

    public static bool MixesSchemes(Callout c)
    {
        var text = $"{c.Hardware} {c.Offense} {c.Defense} {c.Always}";
        var pad = text.Contains("South", StringComparison.OrdinalIgnoreCase)
            || text.Contains("D-pad", StringComparison.OrdinalIgnoreCase)
            || text.Contains("LT", StringComparison.Ordinal);
        var keys = text.Contains("Space", StringComparison.OrdinalIgnoreCase)
            || text.Contains("WASD", StringComparison.OrdinalIgnoreCase)
            || text.Contains("left click", StringComparison.OrdinalIgnoreCase);
        return pad && keys;
    }
}
