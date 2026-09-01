namespace GrandSluggers.Sim;

/// <summary>
/// Who sits home and away. Gamepad 0 is player 1 (keyboard is that seat too).
/// North on select can sit pad 1 away. Gamepad 1 sits the other side; missing
/// pad 2 is CPU. Unplug remaps without restarting the inning — SET is mound
/// when 1P pitches, plate when 1P bats, plate in 1v1.
/// </summary>
public readonly record struct Seats(LineupSeat Home, LineupSeat Away)
{
    public static Seats One { get; } = new(LineupSeat.Pad1, LineupSeat.Cpu);
    public static Seats Versus { get; } = new(LineupSeat.Pad1, LineupSeat.Pad2);
    public static Seats AwayOne { get; } = new(LineupSeat.Cpu, LineupSeat.Pad1);
    public static Seats AwayVersus { get; } = new(LineupSeat.Pad2, LineupSeat.Pad1);

    /// <summary>
    /// Zero or one pad is 1P vs CPU. Two or more pads is local 1v1.
    /// <paramref name="pad1Home"/> false sits pad 1 away (bats the top).
    /// </summary>
    public static Seats FromPads(int padCount, bool pad1Home = true)
    {
        if (padCount >= 2)
            return pad1Home ? Versus : AwayVersus;
        return pad1Home ? One : AwayOne;
    }

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
