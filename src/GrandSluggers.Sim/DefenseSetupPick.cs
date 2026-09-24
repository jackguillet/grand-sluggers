namespace GrandSluggers.Sim.Front;

/// <summary>Inspection and explicit source/destination selection for the in-game defense window.</summary>
public sealed class DefenseSetupPick
{
    public IReadOnlyList<PitcherSwapPick.Candidate> Candidates { get; private set; }
    public int Index { get; private set; }
    public PitcherSwapPick.Candidate Current => Candidates[Index];
    public string? PickedPosition { get; private set; }
    public string Notice { get; private set; } = "Pick a player, then their new position.";

    public DefenseSetupPick(Match match)
    {
        Candidates = Read(match);
        Index = Candidates.Select((c, i) => (c, i)).Where(p => p.c.Pos != "P")
            .OrderByDescending(p => p.c.Who.Stats.Pitch).First().i;
    }
    static IReadOnlyList<PitcherSwapPick.Candidate> Read(Match match)
    {
        var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        return Diamond.Order.Select(pos => new PitcherSwapPick.Candidate(pos, map[pos])).ToArray();
    }
    public void Inspect(int index) { if (index >= 0 && index < Candidates.Count) Index = index; }
    public void Move(int dx, int dy)
    {
        var next = LineupLayout.NeighborPosition(Index, dx, dy);
        if (next >= 0) Index = next;
    }
    public void CancelPick() { PickedPosition = null; Notice = "Pick a player, then their new position."; }
    /// <summary>
    /// A trade asks the match (<see cref="Match.SwapDefensePositions"/>) unless a caller routes it: <paramref name="pitcherSwap"/>
    /// owns a mound change, <paramref name="positionSwap"/> owns every other trade (a lesson records the player's command there).
    /// </summary>
    public bool QuickPitcher(Match match, Func<Character, bool>? pitcherSwap = null, Func<string, string, bool>? positionSwap = null) =>
        Trade(match, "P", Current.Pos, pitcherSwap, positionSwap);
    public bool PickOrSwap(Match match, Func<Character, bool>? pitcherSwap = null, Func<string, string, bool>? positionSwap = null)
    {
        if (PickedPosition == null)
        {
            PickedPosition = Current.Pos;
            Notice = Current.Who.Name + " picked · choose a destination.";
            return false;
        }
        if (PickedPosition == Current.Pos) { CancelPick(); return false; }
        return Trade(match, PickedPosition, Current.Pos, pitcherSwap, positionSwap);
    }
    bool Trade(Match match, string from, string to, Func<Character, bool>? pitcherSwap, Func<string, string, bool>? positionSwap)
    {
        if (!match.CanArrangeDefense) { Notice = "Positions can change before the pitch charge."; return false; }
        if (from == to) { Notice = "Already on the mound."; return false; }
        var mound = from == "P" || to == "P";
        if (mound && !match.CanSwapPitcher)
        { Notice = "Pitcher changed this half · other positions are free."; return false; }
        var who = Candidates.First(c => c.Pos == (from == "P" ? to : from)).Who;
        var changed = mound && pitcherSwap != null ? pitcherSwap(who)
            : positionSwap != null ? positionSwap(from, to) : match.SwapDefensePositions(from, to);
        if (!changed) { Notice = "Positions can change before the pitch charge."; return false; }
        Candidates = Read(match);
        PickedPosition = null;
        Notice = from + " ↔ " + to + " swapped. Pick another pair or Done.";
        return true;
    }
}
