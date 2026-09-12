namespace GrandSluggers.Sim;

/// <summary>
/// How to play in-game spread: four role tables, one scheme at a time.
/// Verb | what you press. SMS p.6 shape. Never mix pad and keys in a cell.
/// </summary>
public static class RoleTables
{
    public static readonly IReadOnlyList<string> PageIds =
        ["roles", "roles-batting-2", "roles-pitching", "roles-pitching-2", "roles-fielding", "roles-running"];
    public sealed record Row(string Verb, string Press);
    public sealed record Block(string Id, string Title, IReadOnlyList<Row> Rows);

    public static IReadOnlyList<Block> Of(InputScheme scheme) =>
        scheme == InputScheme.Keys ? Keys : Pad;

    public static Block OnPage(InputScheme scheme, string pageId)
    {
        var blocks = Of(scheme);
        return pageId.ToLowerInvariant() switch
        {
            "roles" => Half(blocks[0], 0),
            "roles-batting-2" => Half(blocks[0], 1),
            "roles-pitching" => Half(blocks[1], 0),
            "roles-pitching-2" => Half(blocks[1], 1),
            "roles-fielding" => blocks[2],
            "roles-running" => blocks[3],
            _ => Half(blocks[0], 0),
        };
    }

    static Block Half(Block block, int part)
    {
        var start = block.Rows.Count * part / 2;
        var end = block.Rows.Count * (part + 1) / 2;
        return new Block(block.Id, block.Title, block.Rows.Skip(start).Take(end - start).ToArray());
    }

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

    public static readonly IReadOnlyList<Block> Pad =
    [
        new("batting", "Batting",
        [
            new("Move batter", "Stick L/R. Down resets."),
            new("Normal swing", "Tap South"),
            new("Charge swing", "Hold South; release at MAX"),
            new("Star swing", "North + South"),
            new("Bunt", "Hold West"),
            new("Spray", "Stick L/R at contact"),
        ]),
        new("pitching", "Pitching",
        [
            new("Move pitcher", "Stick L/R. Down resets."),
            new("Normal pitch", "Tap South"),
            new("Charge pitch", "Hold South; release at MAX"),
            new("Changeup", "Hold West through the release"),
            new("Star pitch", "North + South"),
            new("Break", "Stick L/R after release"),
            new("Pickoff", "D-pad + South"),
            new("Swap pitcher", "Select; stick picks any fielder; Select again"),
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
            new("All advance", "LB. LB + RB halts everyone."),
            new("All return", "RB. A tap halts a runner going."),
            new("Select runner", "D-pad 1B 2B 3B; down the batter"),
            new("Send", "Stick to the next bag. Back returns."),
            new("Steal", "L3 before the pitch. No steal home."),
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
            new("Normal swing", "Tap Space / left click"),
            new("Charge swing", "Hold Space / left click; release at MAX"),
            new("Star swing", "Q + Space"),
            new("Bunt", "Hold V / Ctrl"),
            new("Spray", "A/D at contact"),
        ]),
        new("pitching", "Pitching",
        [
            new("Move pitcher", "A/D or mouse. S resets."),
            new("Normal pitch", "Tap Space / left click"),
            new("Charge pitch", "Hold Space / left click; release at MAX"),
            new("Changeup", "Hold V / Ctrl through the release"),
            new("Star pitch", "Q + Space"),
            new("Break", "A/D after release"),
            new("Pickoff", "1 2 3 + Space"),
            new("Swap pitcher", "R; A/D picks any fielder; R again"),
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
            new("All advance", ",  and / halts everyone"),
            new("All return", ".  A tap halts a runner going."),
            new("Select runner", "1 2 3; 4 the batter"),
            new("Send", "WASD to the next bag. Back returns."),
            new("Steal", "Z before the pitch. No steal home."),
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
