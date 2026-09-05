namespace GrandSluggers.Sim;

/// <summary>
/// How to play Game screen: a still with orange labels on the HUD we actually draw.
/// SMS p.13. Anchors are <see cref="BroadcastHud"/> rects.
/// </summary>
public static class HudCallouts
{
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
            new("you", "YOU · the glove you have", BroadcastHud.YouTell),
            new("item", "ITEM → name", BroadcastHud.ItemTell),
            new("landing", "Landing ring", null),
            new("score", "Score / inning", BroadcastHud.Standard.Score),
        ]);

    public static readonly IReadOnlyList<Spread> OnScreenPage = [Set, InPlay];

    public static (float X, float Y, float W, float H) Row(int index, float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        const float gap = 10f;
        var lineBand = HowToPlay.KidLineH * 2.2f;
        var h = (board.H - lineBand - gap) * 0.5f;
        return (board.X, board.Y + index * (h + gap), board.W, h);
    }

    public static (float X, float Y, float W, float H) LineBand(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        var h = HowToPlay.KidLineH * 2.2f;
        return (board.X, board.Y + board.H - h, board.W, h);
    }
}
