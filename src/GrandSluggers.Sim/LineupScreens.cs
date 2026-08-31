namespace GrandSluggers.Sim;

public enum LineupStep { TeamSetup, DefenseSetup }

/// <summary>Who owns a roster row. Pad 2 is a later 1v1 seat — the model already names it.</summary>
public enum LineupSeat { Pad1, Pad2, Cpu }

/// <summary>
/// Stick target. Team Setup: a row or the pool. Defense Setup: a batting bar or a diamond.
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
/// (two batting bars + two fielding diamonds). Unity draws this. Seats own a row; 1v1 later
/// sits pad 2 on the away side without a second toolkit.
/// </summary>
public sealed class LineupScreens
{
    public const int Size = TeamBuilder.Size;

    readonly ContentCatalog _content;
    readonly Character?[] _home = new Character?[Size];
    readonly Character?[] _away = new Character?[Size];

    LineupScreens(
        ContentCatalog content,
        Character homeCaptain,
        Character awayCaptain,
        LineupSeat homeSeat,
        LineupSeat awaySeat,
        bool lockCaptain)
    {
        _content = content;
        HomeCaptain = homeCaptain;
        AwayCaptain = awayCaptain;
        HomeSeat = homeSeat;
        AwaySeat = awaySeat;
        LockCaptain = lockCaptain;
        Step = LineupStep.TeamSetup;
        Focus = LineupFocus.Pool;
        SlotIndex = FirstEmpty(_home);
        PoolIndex = 0;
        OrderIndex = 0;
        GloveIndex = 0;
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
        if (awaySeat == LineupSeat.Cpu)
            screens.FillRow(screens._away, awayCap, exclude: [homeCap.Id]);
        screens.SlotIndex = FirstEmpty(screens._home);
        return screens;
    }

    public LineupStep Step { get; private set; }
    public LineupSeat HomeSeat { get; }
    public LineupSeat AwaySeat { get; }
    public LineupFocus Focus { get; private set; }
    public bool LockCaptain { get; }
    public Character HomeCaptain { get; }
    public Character AwayCaptain { get; }
    public int SlotIndex { get; private set; }
    public int PoolIndex { get; private set; }
    public int OrderIndex { get; private set; }
    public int GloveIndex { get; private set; }
    public TeamBuilder? Home { get; private set; }
    public TeamBuilder? Away { get; private set; }

    public IReadOnlyList<Character?> HomeSlots => _home;
    public IReadOnlyList<Character?> AwaySlots => _away;
    public bool HomeFull => Full(_home);
    public bool AwayFull => Full(_away);
    public bool Ready => HomeFull && AwayFull;
    public bool CanPlay => Step == LineupStep.DefenseSetup && Home != null && Away != null;

    public IReadOnlyList<Character> Pool
    {
        get
        {
            var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in _home)
                if (c != null) blocked.Add(c.Id);
            foreach (var c in _away)
                if (c != null) blocked.Add(c.Id);
            return _content.Characters.Values
                .Where(c => !blocked.Contains(c.Id))
                .OrderByDescending(c => c.Captain)
                .ThenBy(c => c.Faction, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public int HomeStars => _content.Chemistry.StartingStars(HomeCaptain, Filled(_home));
    public int AwayStars => _content.Chemistry.StartingStars(AwayCaptain, Filled(_away));

    public Character? Highlighted
    {
        get
        {
            if (Step == LineupStep.TeamSetup)
            {
                if (Focus == LineupFocus.Pool)
                {
                    var pool = Pool;
                    return pool.Count == 0 ? null : pool[Math.Clamp(PoolIndex, 0, pool.Count - 1)];
                }
                var row = Focus == LineupFocus.AwayRow ? _away : _home;
                return row[Math.Clamp(SlotIndex, 0, Size - 1)];
            }

            var draft = Focus is LineupFocus.AwayOrder or LineupFocus.AwayDiamond ? Away : Home;
            if (draft == null) return null;
            if (Focus is LineupFocus.HomeOrder or LineupFocus.AwayOrder)
                return draft.Order[Math.Clamp(OrderIndex, 0, draft.Order.Count - 1)];
            var pos = Diamond.Order[Math.Clamp(GloveIndex, 0, Diamond.Order.Length - 1)];
            return draft.Gloves.TryGetValue(pos, out var who) ? who : null;
        }
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
        return CharacterCard.Of(who, vs);
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

    public string Help => Step == LineupStep.TeamSetup
        ? "stick head / slot    South drop    West remove    Tab fill    South when nine    next"
        : "stick bar order    stick diamond glove    LB/East order    RB glove    South first pitch    West back";

    public bool Stick(int dx, int dy)
    {
        if (dx == 0 && dy == 0) return false;
        if (Step == LineupStep.TeamSetup) return StickTeam(dx, dy);
        return StickDefense(dx, dy);
    }

    public bool South()
    {
        if (Step != LineupStep.TeamSetup) return false;
        if (Drop()) return true;
        return ConfirmTeam();
    }

    public bool West()
    {
        if (Step == LineupStep.DefenseSetup) return BackToTeam();
        return Remove();
    }

    /// <summary>Stick picks a pool head, South drops it into the highlighted empty slot.</summary>
    public bool Drop()
    {
        if (Step != LineupStep.TeamSetup) return false;
        var row = EditableRow();
        if (row == null) return false;
        var i = Math.Clamp(SlotIndex, 0, Size - 1);
        if (row[i] != null) return false;
        var pool = Pool;
        if (pool.Count == 0) return false;
        var who = pool[Math.Clamp(PoolIndex, 0, pool.Count - 1)];
        row[i] = who;
        ClampPool();
        SlotIndex = FirstEmpty(row);
        return true;
    }

    /// <summary>West removes. Captain stays when <see cref="LockCaptain"/>.</summary>
    public bool Remove()
    {
        if (Step != LineupStep.TeamSetup) return false;
        var row = EditableRow();
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

    public bool RandomFill()
    {
        if (Step != LineupStep.TeamSetup) return false;
        var row = EditableRow();
        if (row == null) return false;
        var cap = row == _away ? AwayCaptain : HomeCaptain;
        var exclude = Occupied().Where(id => !id.Equals(cap.Id, StringComparison.OrdinalIgnoreCase));
        FillRow(row, cap, exclude);
        SlotIndex = FirstEmpty(row);
        ClampPool();
        return Full(row);
    }

    public bool ConfirmTeam()
    {
        if (Step != LineupStep.TeamSetup || !Ready) return false;
        var home = TeamBuilder.FromRoster(_content, HomeCaptain, Filled(_home), LockCaptain);
        var away = TeamBuilder.FromRoster(_content, AwayCaptain, Filled(_away), LockCaptain);
        if (home == null || away == null) return false;
        Home = home;
        Away = away;
        Step = LineupStep.DefenseSetup;
        Focus = LineupFocus.HomeOrder;
        OrderIndex = 0;
        GloveIndex = 0;
        return true;
    }

    public bool BackToTeam()
    {
        if (Step != LineupStep.DefenseSetup) return false;
        Step = LineupStep.TeamSetup;
        Focus = LineupFocus.HomeRow;
        Home = null;
        Away = null;
        SlotIndex = 0;
        return true;
    }

    /// <summary>Batting order 1–9 as a cycle. Nine steps restore.</summary>
    public bool StepBatting(int dir)
    {
        var draft = EditableDraft();
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

    public bool CycleGlove()
    {
        var draft = EditableDraft();
        if (draft == null) return false;
        var who = Highlighted;
        if (who == null) return false;
        return draft.CycleGlove(who.Id);
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

    bool StickTeam(int dx, int dy)
    {
        if (Focus == LineupFocus.Pool)
        {
            if (dy > 0 && PoolRow() == 0) { Focus = LineupFocus.HomeRow; return true; }
            if (dy < 0 && PoolRow() >= PoolRows() - 1)
            {
                Focus = LineupFocus.AwayRow;
                SlotIndex = Math.Clamp(SlotIndex, 0, Size - 1);
                return true;
            }
            return MovePool(dx, -dy);
        }

        if (dx != 0)
        {
            SlotIndex = (SlotIndex + (dx > 0 ? 1 : -1) + Size) % Size;
            return true;
        }

        if (Focus == LineupFocus.HomeRow && dy < 0)
        {
            Focus = LineupFocus.Pool;
            return true;
        }
        if (Focus == LineupFocus.AwayRow && dy > 0)
        {
            Focus = LineupFocus.Pool;
            return true;
        }
        return false;
    }

    bool StickDefense(int dx, int dy)
    {
        if (Focus is LineupFocus.HomeDiamond or LineupFocus.AwayDiamond)
        {
            if (dy > 0 && Diamond.Order[GloveIndex] == "C")
            {
                Focus = OwnsAway ? LineupFocus.AwayOrder : LineupFocus.HomeOrder;
                return true;
            }
            if (dx != 0 || dy != 0) return NudgeGlove(dx, dy);
            return false;
        }

        if (dy < 0)
        {
            Focus = OwnsAway ? LineupFocus.AwayDiamond : LineupFocus.HomeDiamond;
            return true;
        }
        if (dx != 0) return StepBatting(dx);
        return false;
    }

    bool MovePool(int dx, int dy)
    {
        var pool = Pool;
        if (pool.Count == 0) return false;
        var cols = LineupLayout.PoolColumns;
        var i = Math.Clamp(PoolIndex, 0, pool.Count - 1);
        var col = i % cols;
        var row = i / cols;
        col = Math.Clamp(col + dx, 0, cols - 1);
        row = Math.Clamp(row + dy, 0, Math.Max(0, (pool.Count - 1) / cols));
        var next = Math.Min(row * cols + col, pool.Count - 1);
        if (next == PoolIndex) return false;
        PoolIndex = next;
        return true;
    }

    int PoolRow()
    {
        var pool = Pool;
        if (pool.Count == 0) return 0;
        return Math.Clamp(PoolIndex, 0, pool.Count - 1) / LineupLayout.PoolColumns;
    }

    int PoolRows()
    {
        var n = Math.Max(1, Pool.Count);
        return Math.Max(1, (n + LineupLayout.PoolColumns - 1) / LineupLayout.PoolColumns);
    }

    int NeighborGlove(int dx, int dy)
    {
        var cur = Diamond.Order[Math.Clamp(GloveIndex, 0, Diamond.Order.Length - 1)];
        var uv = ChemistryToy.MiniSpot(cur);
        var best = -1;
        var bestDist = double.MaxValue;
        for (var i = 0; i < Diamond.Order.Length; i++)
        {
            if (i == GloveIndex) continue;
            var p = ChemistryToy.MiniSpot(Diamond.Order[i]);
            var vx = p.U - uv.U;
            var vy = p.V - uv.V;
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

    Character?[]? EditableRow()
    {
        if (Focus == LineupFocus.AwayRow)
            return AwaySeat == LineupSeat.Cpu ? null : _away;
        return HomeSeat == LineupSeat.Cpu ? null : _home;
    }

    TeamBuilder? EditableDraft()
    {
        if (Step != LineupStep.DefenseSetup) return null;
        if (Focus is LineupFocus.AwayOrder or LineupFocus.AwayDiamond)
            return AwaySeat == LineupSeat.Cpu ? null : Away;
        return HomeSeat == LineupSeat.Cpu ? null : Home;
    }

    bool OwnsAway => Focus is LineupFocus.AwayRow or LineupFocus.AwayOrder or LineupFocus.AwayDiamond;

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

    void ClampPool()
    {
        var n = Pool.Count;
        PoolIndex = n == 0 ? 0 : Math.Clamp(PoolIndex, 0, n - 1);
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
    public const int PoolColumns = 6;

    public static LineupCell HomeSlot(int i) => Bar(i, 0.86);
    public static LineupCell AwaySlot(int i) => Bar(i, 0.06);
    public static LineupCell HomeOrder(int i) => Bar(i, 0.86);
    public static LineupCell AwayOrder(int i) => Bar(i, 0.06);

    public static LineupCell PoolCell(int index, int count)
    {
        var cols = PoolColumns;
        var n = Math.Max(1, count);
        var rows = Math.Max(3, (n + cols - 1) / cols);
        var col = index % cols;
        var row = index / cols;
        const double left = 0.16, width = 0.68, top = 0.72, height = 0.50;
        var w = width / cols;
        var h = height / rows;
        return new LineupCell(left + col * w + w * 0.04, top - (row + 1) * h + h * 0.08, w * 0.90, h * 0.84);
    }

    public static LineupCell DiamondHead(bool home, string pos)
    {
        var uv = ChemistryToy.MiniSpot(pos);
        var u01 = Math.Clamp(uv.U * 0.5 + 0.5, 0, 1);
        var v01 = Math.Clamp(uv.V, 0, 1);
        var left = home ? 0.10 : 0.54;
        const double width = 0.36, bottom = 0.20, height = 0.58, s = 0.072;
        return new LineupCell(
            left + u01 * width - s * 0.5,
            bottom + v01 * height - s * 0.5,
            s,
            s * 1.2);
    }

    public static LineupCell HomeDiamondPanel => new(0.08, 0.18, 0.40, 0.62);
    public static LineupCell AwayDiamondPanel => new(0.52, 0.18, 0.40, 0.62);

    static LineupCell Bar(int i, double y)
    {
        i = Math.Clamp(i, 0, Size - 1);
        const double left = 0.12, width = 0.76, h = 0.10;
        var w = width / Size;
        return new LineupCell(left + i * w + w * 0.04, y, w * 0.90, h);
    }
}
