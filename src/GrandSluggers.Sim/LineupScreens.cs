using System.Globalization;

namespace GrandSluggers.Sim.Front;

public enum LineupStep { TeamSetup, DefenseSetup, MatchSettings }

/// <summary>
/// Stick target. Team Setup: a row or the pool. Defense Setup: a batting list or a diamond.
/// </summary>
public enum LineupFocus
{
    HomeRow,
    AwayRow,
    Pool,
    HomeOrder,
    AwayOrder,
    HomeDiamond,
    AwayDiamond
}

/// <summary>Normalized board cell. X/Y origin is bottom-left. Unity flips Y for OnGUI.</summary>
public readonly record struct LineupCell(double X, double Y, double W, double H)
{
    public double CX => X + W * 0.5;
    public double CY => Y + H * 0.5;
}

/// <summary>
/// Exhibition lineup is two screens: Team Setup (two bars + pool) then Offense/Defense Setup
/// (two batting bars + two fielding diamonds). Unity draws this. Seats own a row; 1v1
/// sits pad 2 on the away side without a second toolkit.
/// </summary>
public sealed class LineupScreens
{
    public const int Size = TeamBuilder.Size;

    readonly ContentCatalog _content;
    readonly IReadOnlyList<Character> _grid;
    readonly Character?[] _home = new Character?[Size];
    readonly Character?[] _away = new Character?[Size];
    readonly SeatCursor _pad1 = new();
    readonly SeatCursor _pad2 = new();
    LineupSeat _acting = LineupSeat.Pad1;

    LineupScreens(
        ContentCatalog content,
        Character homeCaptain,
        Character awayCaptain,
        LineupSeat homeSeat,
        LineupSeat awaySeat,
        bool lockCaptain)
    {
        _content = content;
        _grid = CrewGrid(content);
        HomeCaptain = homeCaptain;
        AwayCaptain = awayCaptain;
        HomeSeat = homeSeat;
        AwaySeat = awaySeat;
        LockCaptain = lockCaptain;
        Step = LineupStep.TeamSetup;
        _acting = LineupSeat.Pad1;
        _pad1.Focus = LineupFocus.Pool;
        _pad1.SlotIndex = FirstEmpty(_home);
        _pad2.Focus = HomeSeat == LineupSeat.Pad2 ? LineupFocus.HomeRow : LineupFocus.AwayRow;
        _pad2.SlotIndex = FirstEmpty(_away);
    }

    public static LineupScreens Open(
        ContentCatalog content,
        string homeCaptain,
        string awayCaptain,
        LineupSeat homeSeat = LineupSeat.Pad1,
        LineupSeat awaySeat = LineupSeat.Cpu,
        bool lockCaptain = true)
    {
        var homeCap = content.Must(homeCaptain);
        var awayCap = content.Must(awayCaptain);
        var screens = new LineupScreens(content, homeCap, awayCap, homeSeat, awaySeat, lockCaptain);
        screens._home[0] = homeCap;
        screens._away[0] = awayCap;
        if (homeSeat == LineupSeat.Cpu)
            screens.FillRow(screens._home, homeCap, exclude: [awayCap.Id]);
        if (awaySeat == LineupSeat.Cpu)
            screens.FillRow(screens._away, awayCap, exclude: [homeCap.Id]);
        screens._pad1.Focus = LineupFocus.Pool;
        screens._pad1.SlotIndex = FirstEmpty(homeSeat == LineupSeat.Pad1 ? screens._home : screens._away);
        screens._pad1.PoolIndex = screens.StartCell(homeSeat == LineupSeat.Pad2 ? awayCap : homeCap);
        screens._pad2.Focus = LineupFocus.Pool;
        screens._pad2.SlotIndex = FirstEmpty(homeSeat == LineupSeat.Pad2 ? screens._home : screens._away);
        screens._pad2.PoolIndex = screens.StartCell(homeSeat == LineupSeat.Pad2 ? homeCap : awayCap);
        screens.Follow(LineupSeat.Pad1);
        return screens;
    }

    public LineupStep Step { get; private set; }
    public LineupSeat HomeSeat { get; private set; }
    public LineupSeat AwaySeat { get; private set; }
    public LineupFocus Focus { get => Active.Focus; private set => Active.Focus = value; }
    public bool LockCaptain { get; }
    public Character HomeCaptain { get; }
    public Character AwayCaptain { get; }
    public int SlotIndex { get => Active.SlotIndex; private set => Active.SlotIndex = value; }
    public int PoolIndex { get => Active.PoolIndex; private set => Active.PoolIndex = value; }
    public int OrderIndex { get => Active.OrderIndex; private set => Active.OrderIndex = value; }
    public int GloveIndex { get => Active.GloveIndex; private set => Active.GloveIndex = value; }
    public TeamBuilder? Home { get; private set; }
    public TeamBuilder? Away { get; private set; }

    public IReadOnlyList<Character?> HomeSlots => _home;
    public IReadOnlyList<Character?> AwaySlots => _away;
    public bool HomeFull => Full(_home);
    public bool AwayFull => Full(_away);
    public bool Ready => HomeFull && AwayFull;
    public bool CanPlay => Step != LineupStep.TeamSetup && Home != null && Away != null;

    /// <summary>
    /// The draft grid: every character, one park crew a row (captain first, then that crew's sidekicks), in captain select
    /// order. It never reflows: a picked character keeps its cell and is <see cref="Taken"/>.
    /// </summary>
    public IReadOnlyList<Character> Pool => _grid;

    /// <summary>The grid for a catalog: nine a row, a crew a row; anyone without a captain's crew trails after.</summary>
    public static IReadOnlyList<Character> CrewGrid(ContentCatalog content)
    {
        var grid = new List<Character>();
        var placed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in content.CaptainIds)
        {
            var captain = content.Must(id);
            grid.Add(captain);
            placed.Add(captain.Id);
            foreach (var c in content.Characters.Values
                .Where(c => !c.Captain && c.Faction.Equals(captain.Faction, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Species, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
                if (placed.Add(c.Id)) grid.Add(c);
        }
        grid.AddRange(content.Characters.Values.Where(c => !placed.Contains(c.Id))
            .OrderBy(c => c.Faction, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase));
        return grid;
    }

    /// <summary>On either roster; its grid cell stays put and cannot be added again.</summary>
    public bool Taken(Character? who) => OnRow(_home, who) || OnRow(_away, who);
    public bool OnHome(Character? who) => OnRow(_home, who);
    public bool OnAway(Character? who) => OnRow(_away, who);
    static bool OnRow(Character?[] row, Character? who) =>
        who != null && row.Any(c => c != null && c.Id.Equals(who.Id, StringComparison.OrdinalIgnoreCase));

    /// <summary>The first grid row on screen. It follows the seat that moved last (<see cref="LineupLayout.PoolVisibleRows"/>).</summary>
    public int PoolTop { get; private set; }
    public int PoolRows => Math.Max(1, (_grid.Count + LineupLayout.PoolColumns - 1) / LineupLayout.PoolColumns);
    public int PoolRowOf(LineupSeat seat) => Math.Clamp(Cur(seat).PoolIndex, 0, Math.Max(0, _grid.Count - 1)) / LineupLayout.PoolColumns;
    public bool PoolRowShown(int row) => row >= PoolTop && row < PoolTop + LineupLayout.PoolVisibleRows;

    /// <summary>The captain whose crew fills a grid row, or null past the crews.</summary>
    public Character? CrewCaptainOfRow(int row)
    {
        var i = row * LineupLayout.PoolColumns;
        return i >= 0 && i < _grid.Count && _grid[i].Captain ? _grid[i] : null;
    }

    /// <summary>The Stars each side starts with: the one reserve for both teams, whatever the draft (§12, PH-16-R16).</summary>
    public int HomeStars => _content.Rules.Stars.StartingReserve;
    public int AwayStars => _content.Rules.Stars.StartingReserve;

    public Character? Highlighted => InspectedBy(_acting);
    public Character? InspectedBy(LineupSeat seat) => seat == LineupSeat.Cpu || (seat != HomeSeat && seat != AwaySeat) ? null : CharacterAt(FocusOf(seat), IndexOf(seat));
    public bool IsReady(LineupSeat seat) => seat == LineupSeat.Cpu || Cur(seat).Ready;
    public bool BothReady => CanPlay && IsReady(HomeSeat) && IsReady(AwaySeat) && !AnyPick;
    public bool ToggleReady(LineupSeat seat)
    {
        if (!CanPlay || seat == LineupSeat.Cpu || (seat != HomeSeat && seat != AwaySeat) || HasPick(seat)) return false;
        Cur(seat).Ready = !Cur(seat).Ready;
        return true;
    }
    public void ResetReady() { _pad1.Ready = false; _pad2.Ready = false; }

    public bool OpenSettings()
    {
        if (Step != LineupStep.DefenseSetup || !BothReady) return false;
        Step = LineupStep.MatchSettings;
        ResetReady();
        return true;
    }
    public bool BackToDefense()
    {
        if (Step != LineupStep.MatchSettings) return false;
        Step = LineupStep.DefenseSetup;
        ResetReady();
        return true;
    }

    public Character? CharacterAt(LineupFocus focus, int index)
    {
        if (index < 0) return null;
        if (focus == LineupFocus.Pool)
        {
            return Step == LineupStep.TeamSetup && index < _grid.Count ? _grid[index] : null;
        }
        if (index >= Size) return null;
        if (focus == LineupFocus.HomeRow) return _home[index];
        if (focus == LineupFocus.AwayRow) return _away[index];
        var draft = focus is LineupFocus.AwayOrder or LineupFocus.AwayDiamond ? Away : Home;
        if (draft == null) return null;
        return focus is LineupFocus.HomeOrder or LineupFocus.AwayOrder
            ? draft.Order[index] : draft.Gloves[Diamond.Order[index]];
    }

    public int IndexOf(LineupSeat seat)
    {
        var c = Cur(seat);
        return c.Focus switch
        {
            LineupFocus.Pool => c.PoolIndex,
            LineupFocus.HomeRow or LineupFocus.AwayRow => c.SlotIndex,
            LineupFocus.HomeOrder or LineupFocus.AwayOrder => c.OrderIndex,
            _ => c.GloveIndex
        };
    }

    /// <summary>Inspection never changes a roster or transfers ownership.</summary>
    public bool FocusCell(LineupSeat seat, LineupFocus focus, int index)
    {
        if (!SeatOwns(seat, focus) || index < 0) return false;
        var teamFocus = focus is LineupFocus.Pool or LineupFocus.HomeRow or LineupFocus.AwayRow;
        if (teamFocus != (Step == LineupStep.TeamSetup)) return false;
        if (index >= (focus == LineupFocus.Pool ? Pool.Count : Size)) return false;
        _acting = seat;
        Active.Focus = focus;
        if (focus == LineupFocus.Pool) { Active.PoolIndex = index; Follow(seat); }
        else if (focus is LineupFocus.HomeRow or LineupFocus.AwayRow) Active.SlotIndex = index;
        else if (focus is LineupFocus.HomeOrder or LineupFocus.AwayOrder) Active.OrderIndex = index;
        else Active.GloveIndex = index;
        return true;
    }

    public bool Buddies(Character? inspected, Character? other) => inspected != null && other != null
        && inspected.Id != other.Id && _content.Chemistry.Between(inspected, other) == Chemistry.Good;

    /// <summary>The tag over a crew row's captain cell in the grid.</summary>
    public string CrewTag(Character captain) => CarnivalFront.LineupCrewTag(_content, captain);

    /// <summary>The inspection card's crew badges for a player (WD-28).</summary>
    public string CrewLine(Character who) => CarnivalFront.LineupCrewLine(_content, who);

    /// <summary>
    /// The inspection card's chemistry line: the player against the captain of the side whose card shows them, and why
    /// (<see cref="ChemistryTable.Reason(string, string)"/>). The same rule lights the buddy cells.
    /// </summary>
    public string ChemLine(Character who, bool home) =>
        CarnivalFront.LineupChemLine(_content, home ? HomeCaptain : AwayCaptain, who);

    public CharacterCard? CardFor(Character? who) => who == null ? null : CharacterCard.Of(who, Chemistry.Neutral, _content.StarSkills);

    public bool ToggleArea(LineupSeat seat)
    {
        if (seat == LineupSeat.Cpu || Step != LineupStep.DefenseSetup) return false;
        _acting = seat;
        CancelPick(seat);
        var away = seat == AwaySeat;
        Focus = Focus is LineupFocus.HomeOrder or LineupFocus.AwayOrder
            ? (away ? LineupFocus.AwayDiamond : LineupFocus.HomeDiamond)
            : (away ? LineupFocus.AwayOrder : LineupFocus.HomeOrder);
        return true;
    }

    public bool Picked(LineupFocus focus, int index) => PickedBy(HomeSeat, focus, index)
        || (HomeSeat != AwaySeat && PickedBy(AwaySeat, focus, index));
    bool PickedBy(LineupSeat seat, LineupFocus focus, int index) => seat != LineupSeat.Cpu
        && Cur(seat).PickedIndex == index && Cur(seat).PickedFocus == focus;
    public bool HasPick(LineupSeat seat) => Cur(seat).PickedIndex >= 0;
    public bool AnyPick => HasPick(LineupSeat.Pad1) || HasPick(LineupSeat.Pad2);
    public bool CancelPick(LineupSeat seat)
    {
        if (!HasPick(seat)) return false;
        Cur(seat).PickedIndex = -1;
        return true;
    }

    /// <summary>First confirm picks; second confirm swaps in the same list or diamond. No navigation edits.</summary>
    public bool PickOrSwap(LineupSeat seat)
    {
        if (Step != LineupStep.DefenseSetup || !SeatOwns(seat, Cur(seat).Focus)) return false;
        _acting = seat;
        var c = Active;
        c.Ready = false;
        var index = IndexOf(seat);
        if (c.PickedIndex < 0 || c.PickedFocus != c.Focus)
        {
            c.PickedFocus = c.Focus;
            c.PickedIndex = index;
            return false;
        }
        var from = c.PickedIndex;
        c.PickedIndex = -1;
        if (from == index) return false;
        var draft = EditableDraft(seat);
        if (draft == null) return false;
        return c.Focus is LineupFocus.HomeOrder or LineupFocus.AwayOrder
            ? draft.SwapOrder(from, index)
            : draft.SetGlove(Diamond.Order[index], draft.Gloves[Diamond.Order[from]].Id);
    }

    public Character HighlightCaptain =>
        Focus is LineupFocus.AwayRow or LineupFocus.AwayOrder or LineupFocus.AwayDiamond
            ? AwayCaptain
            : HomeCaptain;

    public string ChemSticker(Character? who)
    {
        if (who == null) return ChemistryToy.None;
        var cap = CaptainOf(who) ?? HighlightCaptain;
        if (who.Id.Equals(cap.Id, StringComparison.OrdinalIgnoreCase))
            return ChemistryToy.None;
        return ChemistryToy.Sticker(_content.Chemistry.Between(cap, who));
    }

    public CharacterCard? HighlightCard()
    {
        var who = Highlighted;
        if (who == null) return null;
        var cap = CaptainOf(who) ?? HighlightCaptain;
        var vs = who.Id.Equals(cap.Id, StringComparison.OrdinalIgnoreCase)
            ? Chemistry.Good
            : _content.Chemistry.Between(cap, who);
        return CharacterCard.Of(who, vs, _content.StarSkills);
    }

    public bool SeatOwns(LineupSeat seat, LineupFocus focus)
    {
        if (seat == LineupSeat.Cpu) return false;
        return focus switch
        {
            LineupFocus.HomeRow or LineupFocus.HomeOrder or LineupFocus.HomeDiamond => seat == HomeSeat,
            LineupFocus.AwayRow or LineupFocus.AwayOrder or LineupFocus.AwayDiamond => seat == AwaySeat,
            LineupFocus.Pool => seat == HomeSeat || seat == AwaySeat,
            _ => false
        };
    }

    public LineupFocus FocusOf(LineupSeat seat) => Cur(seat).Focus;
    public int SlotOf(LineupSeat seat) => Cur(seat).SlotIndex;
    public int PoolOf(LineupSeat seat) => Cur(seat).PoolIndex;
    public int OrderOf(LineupSeat seat) => Cur(seat).OrderIndex;
    public int GloveOf(LineupSeat seat) => Cur(seat).GloveIndex;

    public bool Lit(LineupFocus focus, int index)
    {
        if (HumanLit(HomeSeat, focus, index)) return true;
        if (HomeSeat != AwaySeat && HumanLit(AwaySeat, focus, index)) return true;
        return false;
    }

    /// <summary>Seat changes preserve human drafts; a newly human side starts with its captain.</summary>
    public void Sit(LineupSeat home, LineupSeat away)
    {
        if (HomeSeat == home && AwaySeat == away) return;
        ResetReady();
        CancelPick(LineupSeat.Pad1);
        CancelPick(LineupSeat.Pad2);
        UpdateSide(_home, HomeCaptain, HomeSeat, home);
        UpdateSide(_away, AwayCaptain, AwaySeat, away);
        HomeSeat = home;
        AwaySeat = away;
        foreach (var seat in new[] { LineupSeat.Pad1, LineupSeat.Pad2 })
        {
            var c = Cur(seat);
            var ownsHome = home == seat;
            c.Focus = Step == LineupStep.TeamSetup ? LineupFocus.Pool
                : ownsHome ? LineupFocus.HomeOrder : LineupFocus.AwayOrder;
            c.SlotIndex = FirstEmpty(ownsHome ? _home : _away);
            c.PoolIndex = Math.Clamp(c.PoolIndex, 0, Math.Max(0, _grid.Count - 1));
        }
        Follow(LineupSeat.Pad1);
    }

    void UpdateSide(Character?[] row, Character captain, LineupSeat previous, LineupSeat next)
    {
        if (previous == next) return;
        if (next == LineupSeat.Cpu)
            FillRow(row, captain, Occupied().Where(id => !row.Any(c => c != null && c.Id == id)));
        else if (previous == LineupSeat.Cpu && Step == LineupStep.TeamSetup)
            for (var i = 1; i < Size; i++) row[i] = null;
    }

    public string Help => Step == LineupStep.TeamSetup
        ? "Choose a player • Confirm to add • Fill team when you want a quick start"
        : "Pick a player, then pick their new spot • Soft highlights show chemistry partners";

    public bool Stick(int dx, int dy) => Stick(LineupSeat.Pad1, dx, dy);

    public bool Stick(LineupSeat seat, int dx, int dy)
    {
        if (dx == 0 && dy == 0 || seat == LineupSeat.Cpu) return false;
        _acting = seat;
        if (Step == LineupStep.TeamSetup) return StickTeam(seat, dx, dy);
        return StickDefense(seat, dx, dy);
    }

    public bool South() => South(LineupSeat.Pad1);

    public bool South(LineupSeat seat)
    {
        if (seat == LineupSeat.Cpu) return false;
        _acting = seat;
        if (Step != LineupStep.TeamSetup) return false;
        if (Drop(seat)) return true;
        return ConfirmTeam();
    }

    public bool West() => West(LineupSeat.Pad1);

    public bool West(LineupSeat seat)
    {
        if (seat == LineupSeat.Cpu) return false;
        _acting = seat;
        if (Step == LineupStep.MatchSettings)
        {
            if (IsReady(seat)) { Cur(seat).Ready = false; return true; }
            return seat == LineupSeat.Pad1 && BackToDefense();
        }
        if (Step == LineupStep.DefenseSetup)
        {
            if (IsReady(seat)) { Cur(seat).Ready = false; return true; }
            return CancelPick(seat) || (seat == LineupSeat.Pad1 && BackToTeam());
        }
        return Remove(seat);
    }

    /// <summary>Stick picks a pool head, South drops it into the highlighted empty slot.</summary>
    public bool Drop() => Drop(_acting);

    public bool Drop(LineupSeat seat)
    {
        if (Step != LineupStep.TeamSetup || seat == LineupSeat.Cpu) return false;
        _acting = seat;
        var row = EditableRow(seat);
        if (row == null) return false;
        var i = Math.Clamp(SlotIndex, 0, Size - 1);
        if (row[i] != null) return false;
        if (_grid.Count == 0) return false;
        var who = _grid[Math.Clamp(PoolIndex, 0, _grid.Count - 1)];
        if (Taken(who)) return false;
        row[i] = who;
        SlotIndex = FirstEmpty(row);
        return true;
    }

    /// <summary>West removes. Captain stays when <see cref="LockCaptain"/>.</summary>
    public bool Remove() => Remove(_acting);

    public bool Remove(LineupSeat seat)
    {
        if (Step != LineupStep.TeamSetup || seat == LineupSeat.Cpu) return false;
        _acting = seat;
        var row = EditableRow(seat);
        if (row == null) return false;
        var i = Math.Clamp(SlotIndex, 0, Size - 1);
        if (row[i] == null || Locked(row[i]!, row))
        {
            i = -1;
            for (var k = Size - 1; k >= 0; k--)
            {
                if (row[k] != null && !Locked(row[k]!, row))
                {
                    i = k;
                    break;
                }
            }
            if (i < 0) return false;
        }
        if (Locked(row[i]!, row)) return false;
        row[i] = null;
        SlotIndex = i;
        return true;
    }

    public bool RandomFill() => RandomFill(_acting);

    public bool RandomFill(LineupSeat seat)
    {
        if (Step != LineupStep.TeamSetup || seat == LineupSeat.Cpu) return false;
        _acting = seat;
        var row = EditableRow(seat);
        if (row == null) return false;
        var cap = row == _away ? AwayCaptain : HomeCaptain;
        var exclude = Occupied().Where(id => !id.Equals(cap.Id, StringComparison.OrdinalIgnoreCase));
        FillRow(row, cap, exclude);
        SlotIndex = FirstEmpty(row);
        return Full(row);
    }

    static bool SameRoster(TeamBuilder? draft, Character?[] row) => draft != null && row.All(c => c != null)
        && draft.Order.Select(c => c.Id).OrderBy(id => id).SequenceEqual(row.Select(c => c!.Id).OrderBy(id => id));

    public bool ConfirmTeam()
    {
        if (Step != LineupStep.TeamSetup || !Ready) return false;
        var home = SameRoster(Home, _home) ? Home : TeamBuilder.FromRoster(_content, HomeCaptain, Filled(_home), LockCaptain);
        var away = SameRoster(Away, _away) ? Away : TeamBuilder.FromRoster(_content, AwayCaptain, Filled(_away), LockCaptain);
        if (home == null || away == null) return false;
        Home = home;
        Away = away;
        Step = LineupStep.DefenseSetup;
        ResetReady();
        _pad1.Focus = HomeSeat == LineupSeat.Pad1 ? LineupFocus.HomeOrder : LineupFocus.AwayOrder;
        _pad1.OrderIndex = 0;
        _pad1.GloveIndex = 0;
        _pad2.Focus = HomeSeat == LineupSeat.Pad2 ? LineupFocus.HomeOrder : LineupFocus.AwayOrder;
        _pad2.OrderIndex = 0;
        _pad2.GloveIndex = 0;
        _acting = LineupSeat.Pad1;
        return true;
    }

    public bool BackToTeam()
    {
        if (Step != LineupStep.DefenseSetup) return false;
        Step = LineupStep.TeamSetup;
        ResetReady();
        CancelPick(LineupSeat.Pad1);
        CancelPick(LineupSeat.Pad2);
        _pad1.Focus = HomeSeat == LineupSeat.Pad1 ? LineupFocus.HomeRow : LineupFocus.AwayRow;
        _pad1.SlotIndex = 0;
        _pad2.Focus = HomeSeat == LineupSeat.Pad2 ? LineupFocus.HomeRow : LineupFocus.AwayRow;
        _pad2.SlotIndex = 0;
        for (var i = 0; i < Size; i++)
        {
            _home[i] = Home!.Order[i];
            _away[i] = Away!.Order[i];
        }
        _acting = LineupSeat.Pad1;
        return true;
    }

    /// <summary>Batting order 1–9 as a cycle. Nine steps restore.</summary>
    public bool StepBatting(int dir) => StepBatting(_acting, dir);

    public bool StepBatting(LineupSeat seat, int dir)
    {
        if (seat == LineupSeat.Cpu) return false;
        _acting = seat;
        var draft = EditableDraft(seat);
        if (draft == null || draft.Order.Count != Size) return false;
        dir = dir >= 0 ? 1 : -1;
        if (dir > 0)
        {
            for (var i = 0; i < Size - 1; i++)
                draft.SwapOrder(i, i + 1);
        }
        else
        {
            for (var i = Size - 1; i > 0; i--)
                draft.SwapOrder(i, i - 1);
        }
        Active.Ready = false;
        OrderIndex = (OrderIndex - dir + Size) % Size;
        return true;
    }

    public bool MoveOrderCursor(int dir)
    {
        if (Step != LineupStep.DefenseSetup) return false;
        OrderIndex = (OrderIndex + (dir >= 0 ? 1 : -1) + Size) % Size;
        if (Focus is not (LineupFocus.HomeOrder or LineupFocus.AwayOrder))
            Focus = OwnsAway ? LineupFocus.AwayOrder : LineupFocus.HomeOrder;
        return true;
    }

    public bool CycleGlove() => CycleGlove(_acting);

    public bool CycleGlove(LineupSeat seat)
    {
        if (seat == LineupSeat.Cpu) return false;
        _acting = seat;
        var draft = EditableDraft(seat);
        if (draft == null) return false;
        var who = Highlighted;
        if (who == null) return false;
        var changed = draft.CycleGlove(who.Id);
        if (changed) Active.Ready = false;
        return changed;
    }

    /// <summary>Stick on the diamond moves that glove onto the neighboring bag.</summary>
    public bool NudgeGlove(int dx, int dy)
    {
        var draft = EditableDraft();
        if (draft == null) return false;
        var next = NeighborGlove(dx, dy);
        if (next < 0) return false;
        var from = Diamond.Order[GloveIndex];
        if (!draft.Gloves.TryGetValue(from, out var who) || who == null) return false;
        if (!draft.SetGlove(Diamond.Order[next], who.Id)) return false;
        Active.Ready = false;
        GloveIndex = next;
        return true;
    }

    public bool MoveGloveCursor(int dx, int dy)
    {
        var next = NeighborGlove(dx, dy);
        if (next < 0) return false;
        GloveIndex = next;
        if (Focus is not (LineupFocus.HomeDiamond or LineupFocus.AwayDiamond))
            Focus = OwnsAway ? LineupFocus.AwayDiamond : LineupFocus.HomeDiamond;
        return true;
    }

    bool StickTeam(LineupSeat seat, int dx, int dy)
    {
        if (Focus == LineupFocus.Pool)
        {
            if (dy > 0 && PoolRow() == 0)
            {
                if (!SeatOwns(seat, LineupFocus.HomeRow)) return false;
                Focus = LineupFocus.HomeRow;
                return true;
            }
            if (dy < 0 && PoolRow() >= PoolRows - 1)
            {
                if (!SeatOwns(seat, LineupFocus.AwayRow)) return false;
                Focus = LineupFocus.AwayRow;
                SlotIndex = Math.Clamp(SlotIndex, 0, Size - 1);
                return true;
            }
            var moved = MovePool(dx, -dy);
            Follow(seat);
            return moved;
        }

        if (dx != 0)
        {
            SlotIndex = (SlotIndex + (dx > 0 ? 1 : -1) + Size) % Size;
            return true;
        }

        if (Focus == LineupFocus.HomeRow && dy < 0)
        {
            Focus = LineupFocus.Pool;
            Follow(seat);
            return true;
        }
        if (Focus == LineupFocus.AwayRow && dy > 0)
        {
            Focus = LineupFocus.Pool;
            Follow(seat);
            return true;
        }
        return false;
    }

    bool StickDefense(LineupSeat seat, int dx, int dy)
    {
        var away = seat == AwaySeat;
        var order = away ? LineupFocus.AwayOrder : LineupFocus.HomeOrder;
        var diamond = away ? LineupFocus.AwayDiamond : LineupFocus.HomeDiamond;
        if (Focus == diamond)
        {
            var pos = Diamond.Order[GloveIndex];
            if ((!away && dy > 0 && pos == "CF") || (away && dy < 0 && pos == "C"))
            {
                CancelPick(seat);
                Focus = order;
                return true;
            }
            return MoveGloveCursor(dx, dy);
        }
        if ((!away && dy < 0) || (away && dy > 0))
        {
            CancelPick(seat);
            Focus = diamond;
            return true;
        }
        return dx != 0 && MoveOrderCursor(dx);
    }

    bool MovePool(int dx, int dy)
    {
        if (_grid.Count == 0) return false;
        var cols = LineupLayout.PoolColumns;
        var i = Math.Clamp(PoolIndex, 0, _grid.Count - 1);
        var col = Math.Clamp(i % cols + dx, 0, cols - 1);
        var row = Math.Clamp(i / cols + dy, 0, PoolRows - 1);
        var next = Math.Min(row * cols + col, _grid.Count - 1);
        if (next == PoolIndex) return false;
        PoolIndex = next;
        return true;
    }

    int PoolRow() => _grid.Count == 0 ? 0 : Math.Clamp(PoolIndex, 0, _grid.Count - 1) / LineupLayout.PoolColumns;

    /// <summary>Scroll the least that shows this seat's pool cursor.</summary>
    void Follow(LineupSeat seat)
    {
        if (seat == LineupSeat.Cpu || Cur(seat).Focus != LineupFocus.Pool) return;
        var row = PoolRowOf(seat);
        var shown = LineupLayout.PoolVisibleRows;
        if (row < PoolTop) PoolTop = row;
        else if (row >= PoolTop + shown) PoolTop = row - shown + 1;
        PoolTop = Math.Clamp(PoolTop, 0, Math.Max(0, PoolRows - shown));
    }

    /// <summary>A seat starts on its captain's crew row, on the first sidekick nobody has taken.</summary>
    int StartCell(Character captain)
    {
        var start = Math.Max(0, _grid.ToList().FindIndex(c => c.Id.Equals(captain.Id, StringComparison.OrdinalIgnoreCase)));
        var end = Math.Min(_grid.Count, start - start % LineupLayout.PoolColumns + LineupLayout.PoolColumns);
        for (var i = start; i < end; i++)
            if (!Taken(_grid[i])) return i;
        return start;
    }

    int NeighborGlove(int dx, int dy) => LineupLayout.NeighborPosition(GloveIndex, dx, dy);

    Character?[]? EditableRow() => EditableRow(_acting);

    Character?[]? EditableRow(LineupSeat seat)
    {
        if (seat == LineupSeat.Cpu) return null;
        if (seat == AwaySeat && AwaySeat != LineupSeat.Cpu)
        {
            if (Focus is LineupFocus.AwayRow or LineupFocus.Pool) return _away;
            return null;
        }
        if (seat == HomeSeat && HomeSeat != LineupSeat.Cpu)
        {
            if (Focus is LineupFocus.HomeRow or LineupFocus.Pool) return _home;
            return null;
        }
        return null;
    }

    TeamBuilder? EditableDraft() => EditableDraft(_acting);

    TeamBuilder? EditableDraft(LineupSeat seat)
    {
        if (Step != LineupStep.DefenseSetup || seat == LineupSeat.Cpu) return null;
        if (seat == AwaySeat) return AwaySeat == LineupSeat.Cpu ? null : Away;
        if (seat == HomeSeat) return HomeSeat == LineupSeat.Cpu ? null : Home;
        return null;
    }

    bool OwnsAway => Focus is LineupFocus.AwayRow or LineupFocus.AwayOrder or LineupFocus.AwayDiamond;

    SeatCursor Cur(LineupSeat seat) => seat == LineupSeat.Pad2 ? _pad2 : _pad1;
    SeatCursor Active => Cur(_acting);

    bool HumanLit(LineupSeat seat, LineupFocus focus, int index)
    {
        if (seat == LineupSeat.Cpu) return false;
        var c = Cur(seat);
        if (c.Focus != focus) return false;
        var i = focus switch
        {
            LineupFocus.Pool => c.PoolIndex,
            LineupFocus.HomeRow or LineupFocus.AwayRow => c.SlotIndex,
            LineupFocus.HomeOrder or LineupFocus.AwayOrder => c.OrderIndex,
            _ => c.GloveIndex
        };
        return i == index;
    }

    sealed class SeatCursor
    {
        public LineupFocus Focus;
        public LineupFocus PickedFocus;
        public int PickedIndex = -1;
        public bool Ready;
        public int SlotIndex;
        public int PoolIndex;
        public int OrderIndex;
        public int GloveIndex;
    }

    bool Locked(Character who, Character?[] row)
    {
        if (!LockCaptain) return false;
        var cap = row == _away ? AwayCaptain : HomeCaptain;
        return who.Id.Equals(cap.Id, StringComparison.OrdinalIgnoreCase);
    }

    Character? CaptainOf(Character who)
    {
        foreach (var c in _home)
            if (c != null && c.Id.Equals(who.Id, StringComparison.OrdinalIgnoreCase))
                return HomeCaptain;
        foreach (var c in _away)
            if (c != null && c.Id.Equals(who.Id, StringComparison.OrdinalIgnoreCase))
                return AwayCaptain;
        if (Home?.Order.Any(c => c.Id.Equals(who.Id, StringComparison.OrdinalIgnoreCase)) == true)
            return HomeCaptain;
        if (Away?.Order.Any(c => c.Id.Equals(who.Id, StringComparison.OrdinalIgnoreCase)) == true)
            return AwayCaptain;
        return null;
    }

    void FillRow(Character?[] row, Character captain, IEnumerable<string> exclude)
    {
        var blocked = new HashSet<string>(exclude, StringComparer.OrdinalIgnoreCase) { captain.Id };
        foreach (var c in row)
            if (c != null) blocked.Add(c.Id);
        var filled = PresetTeams.ForCaptain(_content, captain.Id, exclude: blocked);
        row[0] = captain;
        var i = 1;
        foreach (var c in filled.Roster)
        {
            if (i >= Size) break;
            if (c.Id.Equals(captain.Id, StringComparison.OrdinalIgnoreCase)) continue;
            if (!blocked.Add(c.Id)) continue;
            while (i < Size && row[i] != null) i++;
            if (i >= Size) break;
            row[i++] = c;
        }
    }

    static bool Full(Character?[] row) => row.All(c => c != null);

    static List<Character> Filled(Character?[] row)
    {
        var list = new List<Character>(Size);
        foreach (var c in row)
            if (c != null) list.Add(c);
        return list;
    }

    static int FirstEmpty(Character?[] row)
    {
        for (var i = 0; i < row.Length; i++)
            if (row[i] == null) return i;
        return 0;
    }

    IEnumerable<string> Occupied()
    {
        foreach (var c in _home)
            if (c != null) yield return c.Id;
        foreach (var c in _away)
            if (c != null) yield return c.Id;
    }
}

/// <summary>Two bars, a pool grid, two diamonds. Tests lock the picture without Unity.</summary>
public static class LineupLayout
{
    public const int Size = TeamBuilder.Size;
    /// <summary>Nine a row: one park crew, and each column under the roster slot above it.</summary>
    public const int PoolColumns = Size;
    public const int PoolVisibleRows = 3;
    public const double LabelPadX = 0.12;
    public const double CouchW = 1280;
    public const double CouchH = 800;

    public static LineupCell Title => new(0.018, 0.922, 0.56, 0.052);
    public static LineupCell HomeCaption => new(0.018, 0.888, 0.30, 0.030);
    public static LineupCell AwayCaption => new(0.70, 0.888, 0.28, 0.030);
    public static LineupCell ParkLine => new(0.018, 0.862, 0.36, 0.022);
    public static LineupCell Help => new(0.018, 0.008, 0.96, 0.032);

    public static LineupCell HomeSlot(int i) => Pixels(24 + i * 98, 165, 90, 94);
    public static LineupCell AwaySlot(int i) => Pixels(24 + i * 98, 610, 90, 94);
    public static LineupCell HomeOrder(int i) => OrderCell(true, i);
    public static LineupCell AwayOrder(int i) => OrderCell(false, i);
    public static LineupCell OrderCell(bool home, int i) => Pixels(24 + i * 98, home ? 165 : 624, 90, 80);
    public static LineupCell CardPanel(bool home) => Pixels(928, home ? 126 : 422, 328, 282);
    public static LineupCell ContinueButton => Pixels(1012, 716, 244, 48);
    public static LineupCell BackButton => Pixels(24, 716, 160, 48);
    public static LineupCell FillButton => Pixels(200, 716, 168, 48);
    public static LineupCell Pixels(double x, double y, double w, double h) => new(x / CouchW, 1 - (y + h) / CouchH, w / CouchW, h / CouchH);

    /// <summary>The pool band between the home and away rows, in board pixels (top, height); the title sits above it.</summary>
    public const double PoolTopPx = 286, PoolRowPitchPx = 90, PoolCellHPx = 84;

    /// <summary>A grid cell on screen: <paramref name="row"/> is its row in the window (0 = <see cref="LineupScreens.PoolTop"/>).
    /// A column shares its roster slot's x and width.</summary>
    public static LineupCell PoolCell(int row, int col) => Pixels(24 + col * 98, PoolTopPx + row * PoolRowPitchPx, 90, PoolCellHPx);

    /// <summary>The scroll bar right of the grid: the window's place in the ten crew rows and each seat's cursor row.</summary>
    public static LineupCell PoolScroll => Pixels(904, PoolTopPx, 10, (PoolVisibleRows - 1) * PoolRowPitchPx + PoolCellHPx);

    // Schematic board positions, shared by drawing, hit testing and spatial navigation.
    // Origin top-left. Catcher behind home, pitcher inside the bags, middle infield behind second.
    public static (double X, double Y) FieldSpot(string pos) => pos switch
    {
        "C" => (0.50, 0.90), "P" => (0.50, 0.66),
        "1B" => (0.78, 0.61), "3B" => (0.22, 0.61),
        "2B" => (0.68, 0.34), "SS" => (0.32, 0.34),
        "LF" => (0.15, 0.22), "CF" => (0.50, 0.04), "RF" => (0.85, 0.22),
        _ => (0.50, 0.66)
    };

    /// <summary>Nearest glove in a stick direction, shared by pregame and in-game defense editing.</summary>
    public static int NeighborPosition(int index, int dx, int dy)
    {
        var cur = Diamond.Order[Math.Clamp(index, 0, Diamond.Order.Length - 1)];
        var uv = LineupLayout.FieldSpot(cur);
        var best = -1;
        var bestDist = double.MaxValue;
        for (var i = 0; i < Diamond.Order.Length; i++)
        {
            if (i == index) continue;
            var p = LineupLayout.FieldSpot(Diamond.Order[i]);
            var vx = p.X - uv.X;
            var vy = uv.Y - p.Y;
            var mag = Math.Sqrt(vx * vx + vy * vy);
            if (mag < 1e-6) continue;
            var dot = (vx * dx + vy * dy) / mag;
            if (dot < 0.35) continue;
            if (mag < bestDist)
            {
                bestDist = mag;
                best = i;
            }
        }
        return best;
    }

    public const double FieldHomeY = .90;
    public const double FieldRadius = .90;
    public const double FieldXScale = 1.65;
    public static bool InFairField(double x, double y)
    {
        var dx = x - .5;
        var depth = FieldHomeY - y;
        return depth >= Math.Abs(dx) && dx * dx * FieldXScale * FieldXScale + depth * depth < FieldRadius * FieldRadius;
    }

    public static LineupCell DiamondHead(bool home, string pos)
    {
        var field = home ? HomeDiamondPanel : AwayDiamondPanel;
        var spot = FieldSpot(pos);
        const double w = 66 / CouchW, h = 66 / CouchH;
        return new(field.X + spot.X * field.W - w / 2,
            field.Y + (1 - spot.Y) * field.H - h / 2, w, h);
    }

    public static LineupCell HomeDiamondPanel => Pixels(24, 292, 420, 300);
    public static LineupCell AwayDiamondPanel => Pixels(480, 292, 420, 300);

    /// <summary>Padded name strip at the bottom of a tile so JESTER does not clip to IESTER.</summary>
    public static LineupCell NameRect(LineupCell cell)
    {
        var padX = Math.Max(cell.W * LabelPadX, 0.006);
        var h = Math.Min(cell.H * 0.28, 0.034);
        return new LineupCell(cell.X + padX, cell.Y + cell.H * 0.02, cell.W - padX * 2, h);
    }

    public static LineupCell FaceRect(LineupCell cell)
    {
        var padX = cell.W * 0.08;
        var name = NameRect(cell);
        var y = name.Y + name.H;
        var top = cell.Y + cell.H - cell.H * 0.06;
        return new LineupCell(cell.X + padX, y, cell.W - padX * 2, Math.Max(0.02, top - y));
    }

    public static (double X, double Y, double W, double H) GuiPixel(LineupCell c, double screenW, double screenH) =>
        (c.X * screenW, (1.0 - c.Y - c.H) * screenH, c.W * screenW, c.H * screenH);

    public static string TeamMark(Character? who) => who != null && who.Captain ? "C" : "";
    public static string OrderMark(int i) => (Math.Clamp(i, 0, Size - 1) + 1).ToString(CultureInfo.InvariantCulture);
    public static string GloveMark(string pos) => string.IsNullOrEmpty(pos) ? "" : pos;

}
