namespace GrandSluggers.Sim;

/// <summary>
/// How to play is one book, two schemes. Pad is the couch. Keyboard + mouse is player 1.
/// Last input picks the page. The toggle locks until the book closes.
/// </summary>
public enum InputScheme { Pad, Keys }

public static class BookScheme
{
    public static InputScheme Current { get; private set; } = InputScheme.Pad;
    public static bool Locked { get; private set; }

    public const string PadLabel = "Pad";
    public const string KeysLabel = "Keyboard + mouse";
    public const string OffenseLabel = "Offense";
    public const string DefenseLabel = "Defense";

    public static void Observe(InputScheme fromInput)
    {
        if (!Locked) Current = fromInput;
    }

    public static void Open() => Locked = false;

    public static void Close() => Locked = false;

    public static bool Select(InputScheme kind)
    {
        Locked = true;
        if (Current == kind) return false;
        Current = kind;
        return true;
    }

    public static InputScheme Toggle()
    {
        Select(Current == InputScheme.Pad ? InputScheme.Keys : InputScheme.Pad);
        return Current;
    }

    public static void Reset()
    {
        Current = InputScheme.Pad;
        Locked = false;
    }

    public static string Label(InputScheme kind) =>
        kind == InputScheme.Keys ? KeysLabel : PadLabel;

    public static string Footer(InputScheme kind) =>
        kind == InputScheme.Keys
            ? "Left click / Space next     wheel     Esc / right click back"
            : "South next     stick     East back";

    public static (float X, float Y, float W, float H) ToggleBar(float screenW, float screenH)
    {
        var book = HowToPlay.BookPanel(screenW, screenH);
        const float w = 340f;
        const float h = 36f;
        return (book.X + book.W - 16f - w, book.Y + 10f, w, h);
    }

    public static (float X, float Y, float W, float H) Tab(InputScheme kind, float screenW, float screenH)
    {
        var bar = ToggleBar(screenW, screenH);
        var w = (bar.W - 6f) * 0.5f;
        return kind == InputScheme.Pad
            ? (bar.X, bar.Y, w, bar.H)
            : (bar.X + w + 6f, bar.Y, w, bar.H);
    }

    public static InputScheme? HitToggle(float mx, float my, float screenW, float screenH)
    {
        if (Inside(Tab(InputScheme.Pad, screenW, screenH), mx, my)) return InputScheme.Pad;
        if (Inside(Tab(InputScheme.Keys, screenW, screenH), mx, my)) return InputScheme.Keys;
        return null;
    }

    static bool Inside((float X, float Y, float W, float H) r, float mx, float my) =>
        mx >= r.X && mx <= r.X + r.W && my >= r.Y && my <= r.Y + r.H;
}
