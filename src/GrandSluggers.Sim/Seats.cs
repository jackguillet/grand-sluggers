namespace GrandSluggers.Sim;

/// <summary>
/// Who sits home and away. Gamepad 0 is home (keyboard is that seat too).
/// Gamepad 1 sits away; missing pad 2 is CPU. Unplug remaps without restarting
/// the inning — SET still forks pitcher view vs batter view, not seat count.
/// </summary>
public readonly record struct Seats(LineupSeat Home, LineupSeat Away)
{
    public static Seats One { get; } = new(LineupSeat.Pad1, LineupSeat.Cpu);
    public static Seats Versus { get; } = new(LineupSeat.Pad1, LineupSeat.Pad2);

    /// <summary>
    /// Gamepad index 0 → home. Index 1 → away. Zero or one pad is 1P vs CPU
    /// (keyboard still plays home). Two or more pads is local 1v1.
    /// </summary>
    public static Seats FromPads(int padCount) =>
        padCount >= 2 ? Versus : One;

    public bool HomeHuman => Home != LineupSeat.Cpu;
    public bool AwayHuman => Away != LineupSeat.Cpu;
    public bool BothHuman => HomeHuman && AwayHuman;
    public int Count => (HomeHuman ? 1 : 0) + (AwayHuman ? 1 : 0);

    public bool HumanPitches(bool top) => top ? HomeHuman : AwayHuman;
    public bool HumanBats(bool top) => top ? AwayHuman : HomeHuman;
    public bool CpuPitches(bool top) => !HumanPitches(top);
    public bool CpuBats(bool top) => !HumanBats(top);

    public LineupSeat Pitching(bool top) => top ? Home : Away;
    public LineupSeat Batting(bool top) => top ? Away : Home;
    public LineupSeat Fielding(bool top) => Pitching(top);
    public LineupSeat Running(bool top) => Batting(top);
}
