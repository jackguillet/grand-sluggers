namespace GrandSluggers.Sim;

/// <summary>
/// How to pitch / how to swing: two stills, a motion strip, one caption.
/// SMS p.7 / p.9 shape. Harbor cameras. The pad's confirm verb.
/// </summary>
public static class HowToComic
{
    public const float CopyBandMul = 6.3f;
    public sealed record Panel(string Shot, string Label);

    public sealed record Motion(string Charge, string Commit);

    public sealed record Strip(
        string Id,
        string Title,
        Panel First,
        Panel Second,
        Motion Motion,
        string Caption);

    public static readonly Strip Pitch = new(
        "how-to-pitch",
        "How to pitch",
        new("mound", "Charge at MAX"),
        new("pitch", "The ball leaves the hand"),
        new("Hold RT", "Release RT"),
        "Tap RT for a normal pitch. Hold, then release at MAX for power.");

    public static readonly Strip Swing = new(
        "how-to-swing",
        "How to swing",
        new("plate", "Charge at MAX"),
        new("smash", "Swing through the ball"),
        new("Hold RT", "Release RT"),
        "Swing when the ball is on the plate: tap RT, or hold to MAX.");

    public static readonly IReadOnlyList<Strip> OnPitchSwingPage = [Pitch, Swing];

    public static (float X, float Y, float W, float H) Row(int index, float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        const float gap = 10f;
        var lineBand = HowToPlay.KidLineH * CopyBandMul;
        var w = (board.W - gap) * 0.5f;
        return (board.X + index * (w + gap), board.Y, w, board.H - lineBand);
    }

    public static (float X, float Y, float W, float H) LineBand(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        var h = HowToPlay.KidLineH * CopyBandMul;
        return (board.X, board.Y + board.H - h, board.W, h);
    }
}
