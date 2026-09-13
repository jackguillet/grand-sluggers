namespace GrandSluggers.Sim;

/// <summary>
/// How to play in-game spread: four role tables, one scheme at a time.
/// Verb | what you press. SMS p.6 shape. Never mix pad and keys in a cell.
/// </summary>
public static class RoleTables
{
    public static readonly IReadOnlyList<string> PageIds =
        ["roles", "roles-batting-2", "roles-pitching", "roles-pitching-2", "roles-fielding", "roles-fielding-2", "roles-running", "roles-running-2"];

    /// <summary>Rows per block: four at least, and no more than two couch-size pages of five.</summary>
    public const int MinRows = 4;
    public const int MaxRows = 10;
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
            "roles-fielding" => Half(blocks[2], 0),
            "roles-fielding-2" => Half(blocks[2], 1),
            "roles-running" => Half(blocks[3], 0),
            "roles-running-2" => Half(blocks[3], 1),
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
            new("Move batter", "Stick L/R. Resets each pitch."),
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
            new("Pickoff", "D-pad + South in SET. On the bag is safe; one who broke is caught."),
            new("Swap pitcher", "Select; stick picks any fielder; Select again"),
        ]),
        new("fielding", "Fielding",
        [
            new("Take the glove", "Stick"),
            new("Catch", "South in the window"),
            new("Throw", "D-pad + South. You become the glove at that bag."),
            new("Cutoff / relay", "LB after the catch. The cutoff sends it on."),
            new("Tag", "Have the ball. Touch them off a bag."),
            new("Rundown", "Chase them; throw to the covered bag."),
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
            new("Halt", "Stick at a bag + LB + RB: that runner only"),
            new("Steal", "Stick to the bag or L3 in SET or the windup. Home counts."),
            new("Dash", "Mash South"),
            new("Close play", "First South, at third or home"),
            new("Rundown", "Stick back or forward turns you"),
        ]),
    ];

    public static readonly IReadOnlyList<Block> Keys =
    [
        new("batting", "Batting",
        [
            new("Move batter", "A/D or mouse. Resets each pitch."),
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
            new("Pickoff", "1 2 3 + Space in SET. On the bag is safe; one who broke is caught."),
            new("Swap pitcher", "R; A/D picks any fielder; R again"),
        ]),
        new("fielding", "Fielding",
        [
            new("Take the glove", "WASD"),
            new("Catch", "Space / left click in the window"),
            new("Throw", "1 2 3 4 + Space. You become the glove at that bag."),
            new("Cutoff / relay", "X after the catch. The cutoff sends it on."),
            new("Tag", "Have the ball. Touch them off a bag."),
            new("Rundown", "Chase them; throw to the covered bag."),
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
            new("Halt", "WASD at a bag + /: that runner only"),
            new("Steal", "WASD to the bag or Z in SET or the windup. Home counts."),
            new("Dash", "Mash Space / left click"),
            new("Close play", "First Space / left click, at third or home"),
            new("Rundown", "WASD back or forward turns you"),
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
