namespace GrandSluggers.Sim;

/// <summary>
/// How to play Getting started: numbered first-time path, then a mode table.
/// SMS p.11–12. Exhibition and Training are the products. Two controllers is seating.
/// </summary>
public static class GettingStarted
{
    public sealed record Step(
        string Id,
        string Title,
        string Picture,
        string Shot,
        string Caption);

    public sealed record Mode(string Id, string Title, string Line);

    public static readonly IReadOnlyList<Step> Path =
    [
        new("title", "Title", "how-to-start-title", "title",
            "South play ball."),
        new("field", "Stadium", "how-to-start-field", "field",
            "Choose stadium, time and hazards. South confirms."),
        new("captains", "Captains", "how-to-start-select", "select",
            "Choose players, teams and side. South confirms."),
        new("lineup", "Lineup", "how-to-start-lineup", "lineup",
            "Build nine, set positions/order, then rules. North ready."),
        new("pitch", "First pitch", "how-to-start-pitch", "plate",
            "Home bats the bottom."),
    ];

    public static readonly IReadOnlyList<Mode> Modes =
    [
        new("exhibition", "Exhibition",
            "Stadium, captains, lineup, positions/order, settings, play."),
        new("training", "Training",
            "Title → Tutorials. Harbor drills."),
        new("two-pads", "Two controllers",
            "2 PLAYERS: Controller 2 takes the other side. Seat drop pauses; reconnect."),
    ];

    public static (float X, float Y, float W, float H) PathRow(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        return (board.X, board.Y, board.W, board.H);
    }

    public static (float X, float Y, float W, float H) StepCell(int index, float screenW, float screenH)
    {
        var board = PathRow(screenW, screenH);
        var n = Math.Max(1, Path.Count);
        const float gap = 8f;
        var h = (board.H - gap * (n - 1)) / n;
        return (board.X, board.Y + index * (h + gap), board.W, h);
    }

    public static (float X, float Y, float W, float H) ModeTable(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        var band = LineBand(screenW, screenH);
        return (board.X, board.Y, board.W, band.Y - board.Y - 8f);
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
        var h = HowToPlay.KidLineH * HowToPlay.LineBandMul;
        return (board.X, board.Y + board.H - h, board.W, h);
    }
}
