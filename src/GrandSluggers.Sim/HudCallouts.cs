namespace GrandSluggers.Sim;

/// <summary>
/// How to play Game screen: a still with orange labels on the HUD we actually draw.
/// SMS p.13. Anchors are <see cref="BroadcastHud"/> rects.
/// </summary>
public static class HudCallouts
{
    public const float CopyBandMul = 6.6f;
    public sealed record Mark(
        string Id,
        string Label,
        BroadcastHud.HudRect? Anchor);

    public sealed record Spread(
        string Id,
        string Title,
        string Picture,
        string Shot,
        IReadOnlyList<Mark> Marks);

    public static readonly Spread Set = new(
        "hud-set",
        "SET",
        "how-to-hud-set",
        "plate",
        [
            new("score", "Score / inning", BroadcastHud.Standard.Score),
            new("count", "B / S / O", BroadcastHud.Standard.Count),
            new("diamond", "On-base", BroadcastHud.Standard.MiniDiamond),
            new("batter", "Batter card · AB", BroadcastHud.Standard.BatterCard),
            new("pitcher", "Pitcher card · ARM · TIRED", BroadcastHud.Standard.PitcherCard),
        ]);

    public static readonly Spread InPlay = new(
        "hud-inplay",
        "In-play",
        "how-to-hud-play",
        "diamond-grounder",
        [
            new("you", "YOU · glove + bag stays up", BroadcastHud.YouTell),
            new("item", "ITEM → name", BroadcastHud.ItemTell),
            new("landing", "Landing ring", null),
            new("score", "Score / inning", BroadcastHud.Standard.Score),
        ]);

    public static readonly IReadOnlyList<Spread> OnScreenPage = [Set, InPlay];

    public static (float X, float Y, float W, float H) Row(int index, float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        const float gap = 10f;
        var lineBand = HowToPlay.KidLineH * CopyBandMul;
        var w = (board.W - gap) * 0.5f;
        return (board.X + index * (w + gap), board.Y, w, board.H - lineBand);
    }

    public static (float X, float Y, float W, float H) MarkCell(
        int spreadIndex, int markIndex, float screenW, float screenH)
    {
        var row = Row(spreadIndex, screenW, screenH);
        var count = Math.Max(1, OnScreenPage[spreadIndex].Marks.Count);
        const float top = 43f;
        const float gap = 1f;
        var h = (row.H - top - gap * (count - 1)) / count;
        return (row.X + 16f, row.Y + top + markIndex * (h + gap), row.W - 32f, h);
    }

    public static (float X, float Y, float W, float H) LineBand(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        var h = HowToPlay.KidLineH * CopyBandMul;
        return (board.X, board.Y + board.H - h, board.W, h);
    }
}
