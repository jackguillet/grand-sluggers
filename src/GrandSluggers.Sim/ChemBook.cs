namespace GrandSluggers.Sim;

/// <summary>
/// How to play chemistry + abilities: labeled stills and a four-row type table.
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
        "When chemistry is good",
        "how-to-chem-good",
        "Hearts. Buddy throws. Buddy jump. Items.",
        Chemistry.Good);

    public static readonly Pair Bad = new(
        "chem-bad",
        "When chemistry is bad",
        "how-to-chem-bad",
        "Scribbles. Throws sail. Rivals miss.",
        Chemistry.Bad);

    public static readonly IReadOnlyList<Pair> ChemistryPairs = [Good, Bad];

    public const string AbilityPicture = "how-to-ability-card";

    public static readonly IReadOnlyList<string> CardStats = ["PIT", "BAT", "FLD", "RUN"];

    public static readonly IReadOnlyList<TypeRow> Types =
    [
        new("pitches", "Pitches", "Star pitch on the mound. Owns the ball about two seconds."),
        new("swings", "Swings", "Star swing at the plate. Then baseball."),
        new("running", "Running", "Close play at third or home. First button wins."),
        new("fielding", "Fielding", "One field verb. Super Jump, Grow, Lick Catch add range."),
    ];

    public static (float X, float Y, float W, float H) ChemCell(int index, float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        const float gap = 12f;
        var lineBand = HowToPlay.KidLineH * 2.2f;
        var w = (board.W - gap) * 0.5f;
        return (board.X + index * (w + gap), board.Y, w, board.H - lineBand);
    }

    public static (float X, float Y, float W, float H) AbilityStill(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        var lineBand = HowToPlay.KidLineH * 2.2f;
        var h = board.H - lineBand;
        return (board.X, board.Y, board.W * 0.48f, h);
    }

    public static (float X, float Y, float W, float H) TypeTable(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        var still = AbilityStill(screenW, screenH);
        var lineBand = HowToPlay.KidLineH * 2.2f;
        return (still.X + still.W + 12f, board.Y, board.W - still.W - 12f, board.H - lineBand);
    }

    public static (float X, float Y, float W, float H) LineBand(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        var h = HowToPlay.KidLineH * 2.2f;
        return (board.X, board.Y + board.H - h, board.W, h);
    }
}
