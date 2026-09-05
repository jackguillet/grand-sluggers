namespace GrandSluggers.Sim;

/// <summary>
/// The running-booklet diagrams. They deliberately share the named-bag UV map used by
/// runner leads and the in-play throw tell: right 1B, up 2B, left 3B, down home.
/// </summary>
public static class BagDiagrams
{
    public enum Kind { BagMap, Advance, Return }

    public sealed record Route(int FromBag, int ToBag);

    public sealed record Diagram(
        Kind Kind,
        string Title,
        string PadPress,
        string KeysPress,
        IReadOnlyList<Route> Routes);

    public static readonly Diagram BagMap = new(
        Kind.BagMap,
        "Name a bag",
        "D-PAD",
        "1  2  3  4  ·  CLICK A QUADRANT",
        []);

    public static readonly Diagram Advance = new(
        Kind.Advance,
        "All advance",
        "LB",
        ",",
        [new(1, Baserunning.NextBag(1)), new(2, Baserunning.NextBag(2)), new(3, Baserunning.NextBag(3))]);

    public static readonly Diagram Return = new(
        Kind.Return,
        "All return",
        "RB",
        ".",
        [new(1, Baserunning.PrevBag(1)), new(2, Baserunning.PrevBag(2)), new(3, Baserunning.PrevBag(3))]);

    public static readonly IReadOnlyList<Diagram> Running = [BagMap, Advance, Return];

    public static string Press(Diagram diagram, InputScheme scheme) =>
        scheme == InputScheme.Keys ? diagram.KeysPress : diagram.PadPress;

    public static (double U, double V) Pip(int bag) => FieldAssist.BagPip(bag);

    public static string BagName(int bag) => bag switch
    {
        1 => "1B",
        2 => "2B",
        3 => "3B",
        4 => "HOME",
        _ => ""
    };

    /// <summary>Where each physical D-pad direction points in the shared diamond.</summary>
    public static string Direction(int bag) => bag switch
    {
        1 => "RIGHT",
        2 => "UP",
        3 => "LEFT",
        4 => "DOWN",
        _ => ""
    };

    public static (float X, float Y, float W, float H) Card(int index, float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        const float gap = 12f;
        var w = (board.W - gap * 2f) / 3f;
        return (board.X + index * (w + gap), board.Y, w, board.H - HowToPlay.KidLineH * 2.15f);
    }

    public static (float X, float Y, float W, float H) LineBand(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        var h = HowToPlay.KidLineH * 2.15f;
        return (board.X, board.Y + board.H - h, board.W, h);
    }
}
