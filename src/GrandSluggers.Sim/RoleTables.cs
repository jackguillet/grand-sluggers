namespace GrandSluggers.Sim;

/// <summary>
/// How to play in-game spread: four role tables, one scheme at a time.
/// Verb | what you press. SMS p.6 shape. Never mix pad and keys in a cell.
/// </summary>
public static class RoleTables
{
    public sealed record Row(string Verb, string Press);
    public sealed record Block(string Id, string Title, IReadOnlyList<Row> Rows);

    public static IReadOnlyList<Block> Of(InputScheme scheme) =>
        scheme == InputScheme.Keys ? Keys : Pad;

    public static readonly IReadOnlyList<Block> Pad =
    [
        new("batting", "Batting",
        [
            new("Move batter", "Stick L/R. Down resets."),
            new("Normal swing", "South"),
            new("Charge swing", "Hold LT. Commit at MAX."),
            new("Star swing", "North + South"),
            new("Bunt", "Hold West"),
            new("Spray", "Stick L/R at contact"),
        ]),
        new("pitching", "Pitching",
        [
            new("Move pitcher", "Stick L/R. Down resets."),
            new("Normal pitch", "South"),
            new("Charge pitch", "Hold LT. Commit at MAX."),
            new("Changeup", "Hold West"),
            new("Star pitch", "North + South"),
            new("Curve", "Stick L/R after release"),
            new("Pickoff", "D-pad + South"),
        ]),
        new("fielding", "Fielding",
        [
            new("Take the glove", "Stick"),
            new("Catch", "South in the window"),
            new("Throw", "D-pad + South"),
            new("Relay", "LB"),
            new("Jump", "West in the window"),
            new("Dive", "East"),
            new("Attack", "North"),
            new("Swap", "Select"),
        ]),
        new("running", "Running",
        [
            new("All advance", "LB"),
            new("All return", "RB"),
            new("Halt", "LB + RB"),
            new("Select runner", "D-pad 1B 2B 3B"),
            new("Steal", "L3. No steal home."),
            new("Dash", "Mash South"),
            new("Close play", "First South"),
            new("Tag", "Have the ball. Touch them off a bag."),
        ]),
    ];

    public static readonly IReadOnlyList<Block> Keys =
    [
        new("batting", "Batting",
        [
            new("Move batter", "A/D or mouse. S resets."),
            new("Normal swing", "Space / left click"),
            new("Charge swing", "Hold Shift / right click. Commit at MAX."),
            new("Star swing", "Q + Space"),
            new("Bunt", "Hold V / Ctrl"),
            new("Spray", "A/D at contact"),
        ]),
        new("pitching", "Pitching",
        [
            new("Move pitcher", "A/D or mouse. S resets."),
            new("Normal pitch", "Space / left click"),
            new("Charge pitch", "Hold Shift / right click. Commit at MAX."),
            new("Changeup", "Hold V / Ctrl"),
            new("Star pitch", "Q + Space"),
            new("Curve", "A/D after release"),
            new("Pickoff", "1 2 3 + Space"),
        ]),
        new("fielding", "Fielding",
        [
            new("Take the glove", "WASD"),
            new("Catch", "Space / left click in the window"),
            new("Throw", "1 2 3 4 + Space"),
            new("Relay", "X"),
            new("Jump", "F in the window"),
            new("Dive", "G"),
            new("Attack", "B"),
            new("Swap", "R"),
        ]),
        new("running", "Running",
        [
            new("All advance", ","),
            new("All return", "."),
            new("Halt", "/"),
            new("Select runner", "1 2 3"),
            new("Steal", "Z. No steal home."),
            new("Dash", "Mash Space / left click"),
            new("Close play", "First Space / left click"),
            new("Tag", "Have the ball. Touch them off a bag."),
        ]),
    ];

    public static (float X, float Y, float W, float H) Cell(int index, float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        const float gap = 12f;
        var w = (board.W - gap) * 0.5f;
        var h = (board.H - gap) * 0.5f;
        var col = index % 2;
        var row = index / 2;
        return (board.X + col * (w + gap), board.Y + row * (h + gap), w, h);
    }
}
