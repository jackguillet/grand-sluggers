namespace GrandSluggers.Sim;

/// <summary>
/// How to pitch / how to swing: two stills, a motion strip, one caption.
/// SMS p.7 / p.9 shape. Harbor cameras. One scheme's confirm verb.
/// </summary>
public static class HowToComic
{
    public sealed record Panel(string Picture, string Shot, string Label);

    public sealed record Motion(string Charge, string Commit);

    public sealed record Strip(
        string Id,
        string Title,
        Panel First,
        Panel Second,
        Motion PadMotion,
        Motion KeysMotion,
        string PadCaption,
        string KeysCaption);

    public static readonly Strip Pitch = new(
        "how-to-pitch",
        "How to pitch",
        new("how-to-pitch-1", "mound", "Charge at MAX"),
        new("how-to-pitch-2", "pitch", "The ball leaves the hand"),
        new("LT", "South"),
        new("Shift / right click", "Space / left click"),
        "Hold LT. Tap South to throw. Commit at MAX.",
        "Hold Shift / right click. Tap Space / left click to throw. Commit at MAX.");

    public static readonly Strip Swing = new(
        "how-to-swing",
        "How to swing",
        new("how-to-swing-1", "plate", "Charge at MAX"),
        new("how-to-swing-2", "smash", "Swing through the ball"),
        new("LT", "South"),
        new("Shift / right click", "Space / left click"),
        "Hold LT. Tap South to swing. Commit at MAX.",
        "Hold Shift / right click. Tap Space / left click to swing. Commit at MAX.");

    public static readonly IReadOnlyList<Strip> OnPitchSwingPage = [Pitch, Swing];

    public static string Caption(Strip strip, InputScheme scheme) =>
        scheme == InputScheme.Keys ? strip.KeysCaption : strip.PadCaption;

    public static Motion MotionOf(Strip strip, InputScheme scheme) =>
        scheme == InputScheme.Keys ? strip.KeysMotion : strip.PadMotion;

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
