namespace GrandSluggers.Sim.Front;

/// <summary>Presentation state for the shared captain board. Hover never moves the other seat.</summary>
public sealed class CaptainSelection
{
    readonly IReadOnlyList<string> _ids;
    readonly int[] _cursor = new int[2];
    readonly bool[] _ready = new bool[2];
    public bool Versus { get; }
    public bool Pad1Home { get; }
    public int ActiveOne => !Versus && _ready[0] ? 1 : 0;
    public bool Complete => _ready[0] && _ready[1];
    public int Count => _ids.Count;
    public string Id(int panel) => _ids[_cursor[panel]];
    public bool Ready(int panel) => _ready[panel];
    public bool Taken(int panel) => _ready[1 - panel] && Id(panel) == Id(1 - panel);
    public bool Home(int panel) => panel == 0 ? Pad1Home : !Pad1Home;

    public CaptainSelection(ContentCatalog content, ExhibitionPick pick, bool versus)
    {
        _ids = content.CaptainIds;
        Versus = versus; Pad1Home = pick.Pad1Home;
        _cursor[0] = Math.Max(0, content.IndexOfCaptain(pick.Yours));
        _cursor[1] = Math.Max(0, content.IndexOfCaptain(pick.Theirs));
    }
    public void Move(int panel, int delta)
    {
        if (_ready[panel]) return;
        _cursor[panel] = (_cursor[panel] + delta % Count + Count) % Count;
    }
    public bool Confirm(int panel)
    {
        if (Taken(panel)) return false;
        _ready[panel] = true;
        return true;
    }
    /// <returns>True when P1 can return to stadium setup.</returns>
    public bool Back(int player)
    {
        if (!Versus && player == 0 && _ready[0]) { _ready[0] = _ready[1] = false; return false; }
        if (_ready[player]) { _ready[player] = false; return false; }
        return player == 0;
    }
    public ExhibitionPick ApplyTo(ExhibitionPick pick) => Pad1Home
        ? pick with { Home = Id(0), Away = Id(1) }
        : pick with { Home = Id(1), Away = Id(0) };
}
