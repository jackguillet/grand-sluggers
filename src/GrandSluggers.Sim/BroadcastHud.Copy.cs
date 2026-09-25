namespace GrandSluggers.Sim.Front;

/// <summary>The in-play HUD's words (#1048): bags, the close play, the scorebug, the seat cards, runner orders and items.</summary>
public static partial class BroadcastHud
{
    /// <summary>A bag as the HUD names it: 1B, 2B, 3B, HOME.</summary>
    public static string BagShort(int bag) => bag == 4 ? "HOME" : bag + "B";

    public static string ClosePlayTitle(int bag) => "CLOSE PLAY  ·  " + BagShort(bag);
    public static string ClosePlayPrompt(bool pressNow) => pressNow ? "PRESS SOUTH  ·  first wins" : "Get ready…";
    public static string InningHalf(bool over, bool top) => over ? "FINAL" : top ? "TOP" : "BOT";
    public static string NextBatter(string next) => "NEXT  " + next;
    public const string SweatTag = "  SWEAT";
    /// <summary>The seat cards' role tags: the batter at bat and the pitcher.</summary>
    public const string BatterRole = "AB", PitcherRole = "P";
    /// <summary>The label over the steal throw's bag pad.</summary>
    public const string ThrowPadLabel = "throw";
    public static string RunnerOrder(string label, bool held, int destBag) => label + (held ? " · HALTED" : " → " + BagShort(destBag));
    public const string ItemSmashed = "Item smashed.";
    public static string ItemAim(string item) => item.ToUpperInvariant() + "  ·  left stick aim  ·  North throw";
    public static string PitchCycle(string shortFamily) => shortFamily + " · West cycle";
    public static string SetupThrow(int target) => target == 0 ? "Right stick: choose base • RT throw" : "THROW " + BagShort(target) + " • RT";
}
