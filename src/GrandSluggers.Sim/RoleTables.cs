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
        Pad;

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
            new("Move batter", "Left stick"),
            new("Normal swing", "Tap RT"),
            new("Charge swing", "Hold RT; release at MAX"),
            new("Cancel swing", "East before release"),
            new("Star swing", "Hold LT at RT release"),
            new("Bunt to third", "Hold West"),
            new("Bunt to first", "Hold North"),
        ]),
        new("pitching", "Pitching",
        [
            new("Move pitcher", "Left stick L/R. Down resets."),
            new("Normal pitch", "Tap RT"),
            new("Charge pitch", "Hold RT; release at MAX"),
            new("Cycle pitch", "West before charge; starts Fastball; charge locks"),
            new("Star pitch", "Hold LT at RT release"),
            new("Break", "Left stick after release"),
            new("Pickoff", "Hold right stick + RT. After charge: BALK."),
            new("Swap pitcher", "Start → Arrange defense"),
        ]),
        new("fielding", "Fielding",
        [
            new("Take the glove", "Left stick"),
            new("Catch", "Automatic in reach"),
            new("Throw", "Right stick target + RT"),
            new("Cutoff / relay", "RB; RT sends next leg"),
            new("Tag", "Ball in glove; touch runner"),
            new("Rundown", "Move / throw ahead"),
            new("Jump", "North / Buddy Jump"),
            new("Dive", "East; queued throw: cancel"),
            new("Attack", "West at eligible target"),
            new("Swap", "LB"),
        ]),
        new("running", "Running",
        [
            new("All advance", "D-pad Down selects ALL; LB sends"),
            new("All return", "RB reverses selection now"),
            new("Select runner", "Right stick flick; D-pad Down ALL"),
            new("Send", "LB before or after contact"),
            new("Halt", "D-pad Up; fresh bumper to resume"),
            new("Steal", "LB departs NOW; home too"),
            new("Dash", "Mash South"),
            new("Close play", "Fresh South at icon"),
            new("Rundown", "RB return / LB go"),
            new("Throw item", "Offered lessons: D-left/right picks; North throws"),
        ]),
    ];
    public static readonly IReadOnlyList<Block> Keys = Pad;
}
