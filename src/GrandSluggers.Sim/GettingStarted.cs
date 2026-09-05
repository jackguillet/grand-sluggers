namespace GrandSluggers.Sim;

/// <summary>
/// How to play Getting started: numbered first-time path, then a mode table.
/// SMS p.11–12. Exhibition and Training are the products. Two pads is seating.
/// </summary>
public static class GettingStarted
{
    public sealed record Step(
        string Id,
        string Title,
        string Picture,
        string Shot,
        string PadCaption,
        string KeysCaption);

    public sealed record Mode(string Id, string Title, string PadLine, string KeysLine);

    public static readonly IReadOnlyList<Step> Path =
    [
        new("title", "Title", "how-to-start-title", "title",
            "South play ball.",
            "Space / left click play ball."),
        new("captains", "Captains", "how-to-start-select", "select",
            "Stick L/R your team. South the field.",
            "A/D your team. Space / left click the field."),
        new("field", "Field", "how-to-start-field", "field",
            "Harbor is the slice. South lineup.",
            "Harbor is the slice. Space / left click lineup."),
        new("lineup", "Lineup", "how-to-start-lineup", "lineup",
            "South drops a head. Then first pitch.",
            "Space / left click drops a head. Then first pitch."),
        new("pitch", "First pitch", "how-to-start-pitch", "plate",
            "Home bats the bottom.",
            "Home bats the bottom."),
    ];

    public static readonly IReadOnlyList<Mode> Modes =
    [
        new("exhibition", "Exhibition",
            "The game. Captains, a field, a lineup, play. Harbor.",
            "The game. Captains, a field, a lineup, play. Harbor."),
        new("training", "Training",
            "Title West. Harbor drills.",
            "Title F. Harbor drills."),
        new("two-pads", "Two pads",
            "Gamepad 0 is player 1. Gamepad 1 sits the other side. Unplug = CPU.",
            "Keyboard + mouse is player 1 only. A second pad is player 2."),
    ];

    public static string Caption(Step step, InputScheme scheme) =>
        scheme == InputScheme.Keys ? step.KeysCaption : step.PadCaption;

    public static string Line(Mode mode, InputScheme scheme) =>
        scheme == InputScheme.Keys ? mode.KeysLine : mode.PadLine;

    public static (float X, float Y, float W, float H) PathRow(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        var table = ModeTable(screenW, screenH);
        var band = LineBand(screenW, screenH);
        var h = table.Y - board.Y - 10f;
        return (board.X, board.Y, board.W, Math.Max(80f, h));
    }

    public static (float X, float Y, float W, float H) StepCell(int index, float screenW, float screenH)
    {
        var row = PathRow(screenW, screenH);
        var n = Math.Max(1, Path.Count);
        const float gap = 8f;
        var w = (row.W - gap * (n - 1)) / n;
        return (row.X + index * (w + gap), row.Y, w, row.H);
    }

    public static (float X, float Y, float W, float H) ModeTable(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        var band = LineBand(screenW, screenH);
        var h = HowToPlay.KidLineH * 3.4f;
        return (board.X, band.Y - h - 8f, board.W, h);
    }

    public static (float X, float Y, float W, float H) ModeRow(int index, float screenW, float screenH)
    {
        var table = ModeTable(screenW, screenH);
        var n = Math.Max(1, Modes.Count);
        var h = table.H / n;
        return (table.X, table.Y + index * h, table.W, h);
    }

    public static (float X, float Y, float W, float H) LineBand(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        var h = HowToPlay.KidLineH * 2.2f;
        return (board.X, board.Y + board.H - h, board.W, h);
    }
}
