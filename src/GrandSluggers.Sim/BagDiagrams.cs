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

    public sealed record Callout(string Title, string PadPress, string KeysPress, string Line);

    public static readonly Callout ClosePlay = new(
        "Close play",
        "FIRST SOUTH",
        "FIRST SPACE / LEFT CLICK",
        "Camera on 3rd or home. Offense first is safe. Defense first is out.");

    public static readonly Callout Tag = new(
        "Tag",
        "TOUCH OFF THE BAG",
        "TOUCH OFF THE BAG",
        "Have the ball and touch a runner off a bag. On a bag they are safe. Force still needs a throw.");

    public static readonly IReadOnlyList<Callout> Callouts = [ClosePlay, Tag];

    public static string CalloutPress(Callout callout, InputScheme scheme) =>
        scheme == InputScheme.Keys ? callout.KeysPress : callout.PadPress;

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
        return (board.X + index * (w + gap), board.Y, w, board.H - HowToPlay.KidLineH * HowToPlay.LineBandMul);
    }

    public static (float X, float Y, float W, float H) LineBand(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        var h = HowToPlay.KidLineH * HowToPlay.LineBandMul;
        return (board.X, board.Y + board.H - h, board.W, h);
    }

    public static (float X, float Y, float W, float H) CalloutCard(int index, float screenW, float screenH)
    {
        var band = LineBand(screenW, screenH);
        const float gap = 12f;
        var w = (band.W - gap) * 0.5f;
        return (band.X + index * (w + gap), band.Y, w, band.H);
    }
}
