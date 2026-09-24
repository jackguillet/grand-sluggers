namespace GrandSluggers.Sim.Front;

public readonly record struct CaptainPanelRect(float X, float Y, float W, float H);

public static partial class CarnivalFront
{
    public const string CaptainTitle = "CHOOSE YOUR CAPTAINS";
    public const string CaptainControls = "Left/right  Choose     South  Confirm     East  Undo / back     View  How to play";
    public static readonly string[] CaptainStats = ["BATTING", "PITCHING", "FIELDING", "RUNNING"];
    public static CaptainPanelRect CaptainPanel(int panel) => new(24 + panel * 624, 92, 608, 402);
    public static CaptainPanelRect CaptainTile(int i, int count)
    {
        var width = (1232f - (count - 1) * 14) / count;
        return new(24 + i * (width + 14), 558, width, 165);
    }
    public static string CaptainSeat(CaptainSelection s, int panel) =>
        (panel == 0 ? "P1" : s.Versus ? "P2" : "CPU") + "  /  " + (s.Home(panel) ? "HOME" : "AWAY");
    public static string CaptainStatus(CaptainSelection s, int panel, bool pad2) =>
        panel == 1 && s.Versus && !pad2 ? "CONNECT CONTROLLER 2"
        : s.Ready(panel) ? "CONFIRMED"
        : s.Taken(panel) ? "ALREADY CHOSEN"
        : panel == 1 && !s.Versus && !s.Ready(0) ? "CHOOSE AFTER P1"
        : "CHOOSING";
    public static string CaptainPrompt(CaptainSelection s, bool pad2) =>
        s.Versus && !pad2 ? "Connect controller 2 to choose the other captain. East returns to change player count."
        : s.Versus ? "Each player chooses a captain. Confirm both to build your teams."
        : s.Ready(0) ? "Now choose the CPU captain. South confirms and opens team selection."
        : "Choose your captain first. Then choose your opponent.";
    public static string CaptainTileMark(CaptainSelection s, bool one, bool two) =>
        one && two ? (s.Versus ? "P1 + P2" : "P1 + CPU")
        : one ? (s.Ready(0) ? "P1  READY" : "P1")
        : s.Versus ? (s.Ready(1) ? "P2  READY" : "P2") : "CPU";
}
