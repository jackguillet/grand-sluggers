namespace GrandSluggers.Sim;

/// <summary>
/// How to play chemistry + abilities: labeled stills and a measured four-row type table.
/// SMS p.14. Our toys. P / B / F / R is the card we draw on select.
/// </summary>
public static class ChemBook
{
    public sealed record Pair(
        string Id,
        string Title,
        string Picture,
        string Caption,
        Chemistry Chem);

    public sealed record TypeRow(string Id, string Title, string Line);

    public static readonly Pair Good = new(
        "chem-good",
        "Good chemistry",
        "how-to-chem-good",
        "Hearts. Buddy throws. Buddy jump. Items.",
        Chemistry.Good);

    public static readonly Pair Bad = new(
        "chem-bad",
        "Bad chemistry",
        "how-to-chem-bad",
        "Scribbles. Throws sail. Rivals miss.",
        Chemistry.Bad);

    public static readonly IReadOnlyList<Pair> ChemistryPairs = [Good, Bad];

    public const string AbilityPicture = "how-to-ability-card";

    public static readonly IReadOnlyList<string> CardStats = ["PIT", "BAT", "FLD", "RUN"];

    public static readonly IReadOnlyList<TypeRow> Types =
    [
        new("pitches", "Pitches", "Star pitch on the mound owns the ball about two seconds."),
        new("swings", "Swings", "Star swing at the plate, then baseball."),
        new("running", "Running", "Close play: first button wins."),
        new("fielding", "Fielding", "One field verb: Super Jump, Grow, Lick Catch add range."),
    ];

    public static (float X, float Y, float W, float H) ChemCell(int index, float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        const float gap = 12f;
        var lineBand = HowToPlay.KidLineH * HowToPlay.LineBandMul;
        var w = (board.W - gap) * 0.5f;
        return (board.X + index * (w + gap), board.Y, w, board.H - lineBand);
    }

    public static (float X, float Y, float W, float H) AbilityStill(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        var lineBand = HowToPlay.KidLineH * HowToPlay.LineBandMul;
        return (board.X, board.Y, board.W, board.H - lineBand - 8f);
    }

    public static (float X, float Y, float W, float H) TypeTable(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        return (board.X, board.Y, board.W, board.H);
    }

    public static (float X, float Y, float W, float H) TypeCell(int index, float screenW, float screenH)
    {
        var table = TypeTable(screenW, screenH);
        const float gap = 10f;
        var h = (table.H - gap * (Types.Count - 1)) / Types.Count;
        return (table.X, table.Y + index * (h + gap), table.W, h);
    }

    public static (float X, float Y, float W, float H) LineBand(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        var h = HowToPlay.KidLineH * HowToPlay.LineBandMul;
        return (board.X, board.Y + board.H - h, board.W, h);
    }
}
