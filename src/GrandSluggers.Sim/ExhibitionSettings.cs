namespace GrandSluggers.Sim;

/// <summary>Shared Exhibition choices. Player 1 edits the rules; each human readies their own seat.</summary>
public sealed class ExhibitionSettings
{
    public const int RowCount = 5;
    public bool Stars { get; private set; } = true;
    public bool ItemsAvailable => false; // No item source is currently active (§12).
    public int Innings { get; private set; } = Match.DefaultInnings;
    public bool Mercy { get; private set; } = true;
    public string Difficulty { get; private set; } = "normal";
    public int Selected { get; private set; }
    public static readonly string[] Labels = { "Stars", "Items", "Innings", "Mercy rule", "CPU skill" };
    public void Select(int row) => Selected = Math.Clamp(row, 0, RowCount - 1);
    public void Move(int direction) => Select((Selected + Math.Sign(direction) + RowCount) % RowCount);
    public bool Change(LineupSeat seat, int direction = 1)
    {
        if (seat != LineupSeat.Pad1 || direction == 0 || Selected == 1) return false;
        switch (Selected)
        {
            case 0: Stars = !Stars; break;
            case 2: Innings = direction > 0 ? (Innings == 3 ? 6 : Innings == 6 ? 9 : 3) : (Innings == 3 ? 9 : Innings == 9 ? 6 : 3); break;
            case 3: Mercy = !Mercy; break;
            case 4: Difficulty = direction > 0 ? CpuRules.Next(Difficulty) : CpuRules.Next(CpuRules.Next(Difficulty)); break;
        }
        return true;
    }
    public string Value(int row) => row switch
    {
        0 => Stars ? "ON" : "OFF", 1 => "UNAVAILABLE", 2 => Innings.ToString(),
        3 => Mercy ? "ON" : "OFF", _ => Difficulty.ToUpperInvariant()
    };
    public string Description(int row, RulesTable rules) => row switch
    {
        0 => Stars ? "Star pitches and swings for both teams." : "Ordinary pitches and swings. Neither team earns Stars.",
        1 => "Items have no active source yet.",
        2 => "Scheduled innings before extras.",
        3 => Innings < rules.Match.Mercy.MinScheduledInnings
            ? "Not applied in a " + Innings + "-inning game."
            : rules.Match.Mercy.Runs + "-run lead after the trailing side bats, from inning " + rules.Match.Mercy.FromInning + ".",
        _ => "Changes the CPU's skill, never your timing window."
    };
}

public static class ExhibitionSetupLayout
{
    public static LineupCell SettingsRow(int index) => LineupLayout.Pixels(40, 166 + index * 96, 754, 84);
    public static LineupCell Back => LineupLayout.Pixels(40, 718, 174, 48);
    public static LineupCell Next => LineupLayout.Pixels(1010, 718, 230, 48);
    public static LineupCell PreviousPark => LineupLayout.Pixels(250, 650, 230, 48);
    public static LineupCell NextPark => LineupLayout.Pixels(500, 650, 230, 48);
    public static LineupCell Night => LineupLayout.Pixels(900, 168, 340, 54);
    public static LineupCell Hazards => LineupLayout.Pixels(900, 238, 340, 54);
    public static bool Contains(LineupCell c, double x, double y) => x >= c.X && x <= c.X + c.W && y >= c.Y && y <= c.Y + c.H;
}
