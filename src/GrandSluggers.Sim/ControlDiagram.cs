namespace GrandSluggers.Sim.Front;

/// <summary>
/// How to play Controls page: a drawn pad.
/// Orange lozenges, green offense, red defense.
/// </summary>
public static class ControlDiagram
{
    public const string OffenseLabel = "Offense";
    public const string DefenseLabel = "Defense";
    public static readonly IReadOnlyList<string> PageIds = ["controls", "controls-2", "controls-3", "controls-4"];
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
        var top = 108f;
        var foot = 44f;
        return (book.X + 16f, book.Y + top, book.W - 32f, book.H - top - foot - 8f);
    }

    public static IReadOnlyList<Callout> PageCallouts(string pageId)
    {
        var all = PadCallouts;
        var page = Math.Max(0, PageIds.ToList().FindIndex(id => id.Equals(pageId, StringComparison.OrdinalIgnoreCase)));
        var start = all.Count * page / PageIds.Count;
        var end = all.Count * (page + 1) / PageIds.Count;
        return all.Skip(start).Take(end - start).ToArray();
    }

    /// <summary>Two-column list. No tiny schematic.</summary>
    public static (float X, float Y, float W, float H) CalloutCell(
        int index, float screenW, float screenH)
    {
        var b = Board(screenW, screenH);
        var n = PadCallouts.Count;
        var rows = Math.Max(1, (n + 1) / 2);
        var col = index < rows ? 0 : 1;
        var row = index < rows ? index : index - rows;
        const float gap = 18f;
        const float legend = 40f;
        var cw = (b.W - gap) * 0.5f;
        var rh = Math.Max(56f, (b.H - legend) / rows);
        return (b.X + col * (cw + gap), b.Y + legend + row * rh, cw, rh);
    }

    public static (float X, float Y, float W, float H) CalloutCell(
        int index, int count, float screenW, float screenH)
    {
        var b = Board(screenW, screenH);
        const float legend = 40f;
        var rows = Math.Max(1, count);
        var rh = (b.H - legend) / rows;
        return (b.X, b.Y + legend + index * rh, b.W, rh);
    }

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
        new("stick", "Left stick", "Move batter", "Move glove / break pitch", "", 0, 0),
        new("right-stick", "Right stick", "Runner", "Throw base", "Flick", 0, 0),
        new("dpad", "D-pad", "Up halt / Down ALL", "", "Menus", 0, 0),
        new("rt", "RT", "Swing", "Pitch / throw", "", 0, 0),
        new("lt", "LT", "Star swing", "Star pitch", "", 0, 0),
        new("lb", "LB / RB", "Advance-steal / return", "Switch / relay", "Pages", 0, 0),
        new("south", "South · Confirm", "Dash / close", "Close", "", 0, 0),
        new("east", "East · Back", "Cancel swing", "Dive / cancel", "", 0, 0),
        new("west", "West · Secondary", "Bunt 3B / slide", "Pitch / attack", "", 0, 0),
        new("north", "North · Ready", "Bunt 1B", "Jump / buddy", "", 0, 0),
        new("select", "View / Select", "", "", "How to play", 0, 0),
        new("start", "Start / Menu", "", "", "Call time / options", 0, 0),
    ];

    /// <summary>One role table row's card on the control board.</summary>
    public static (float X, float Y, float W, float H) RowCard(
        int index, int count, float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        const float head = 52f;
        const float gap = 6f;
        var rows = Math.Max(1, count);
        var h = (board.H - head - gap * (rows - 1)) / rows;
        return (board.X, board.Y + head + index * (h + gap), board.W, h);
    }
}
