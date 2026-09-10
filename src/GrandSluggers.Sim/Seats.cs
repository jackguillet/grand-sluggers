namespace GrandSluggers.Sim;

/// <summary>
/// Who sits home and away. The front end offers gamepad 0 as player 1 (keyboard
/// is that seat too) and gamepad 1 as player 2. Once play starts, DeviceSeats
/// locks the physical device ids behind Pad1 and Pad2 so collection reordering
/// cannot move a player to the other team.
/// </summary>
public readonly record struct Seats(LineupSeat Home, LineupSeat Away)
{
    public static Seats One { get; } = new(LineupSeat.Pad1, LineupSeat.Cpu);
    public static Seats Versus { get; } = new(LineupSeat.Pad1, LineupSeat.Pad2);
    public static Seats AwayOne { get; } = new(LineupSeat.Cpu, LineupSeat.Pad1);
    public static Seats AwayVersus { get; } = new(LineupSeat.Pad2, LineupSeat.Pad1);

    /// <summary>
    /// One player vs CPU until <paramref name="versus"/> is on and two pads sit.
    /// Plugging in pad 2 does not start 1v1. <paramref name="pad1Home"/> false
    /// sits pad 1 away (bats the top).
    /// </summary>
    public static Seats FromPads(int padCount, bool pad1Home = true, bool versus = false)
    {
        if (versus && padCount >= 2)
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

/// <summary>
/// Match-lifetime ownership of the physical devices behind logical Pad1 and Pad2.
/// Unity supplies InputDevice.deviceId values; this portable class owns the seating
/// rule and recovery transitions without depending on an input toolkit.
/// </summary>
public sealed class DeviceSeats
{
    int? _pad1DeviceId;
    int? _pad2DeviceId;
    bool _pad1KeyboardMouse;

    public static DeviceSeats BeginMatch(
        IReadOnlyList<int> connectedDeviceIds,
        bool player1KeyboardMouse,
        bool versus)
    {
        if (connectedDeviceIds == null)
            throw new ArgumentNullException(nameof(connectedDeviceIds));
        var player1Keys = player1KeyboardMouse || connectedDeviceIds.Count == 0;
        var player1Device = player1Keys ? (int?)null : connectedDeviceIds[0];
        // Keyboard/mouse is P1 only; P2 remains the second physical gamepad.
        var player2Device = versus && connectedDeviceIds.Count > 1
            ? (int?)connectedDeviceIds[1]
            : null;
        return new DeviceSeats(player1Device, player2Device, player1Keys);
    }

    public DeviceSeats(int? pad1DeviceId, int? pad2DeviceId, bool pad1KeyboardMouse = false)
    {
        if (pad1KeyboardMouse && pad1DeviceId.HasValue)
            throw new ArgumentException("Player 1 cannot own keyboard/mouse and a gamepad at the same time.");
        if (pad1DeviceId.HasValue && pad2DeviceId.HasValue && pad1DeviceId == pad2DeviceId)
            throw new ArgumentException("One physical gamepad cannot own both seats.");
        _pad1DeviceId = pad1DeviceId;
        _pad2DeviceId = pad2DeviceId;
        _pad1KeyboardMouse = pad1KeyboardMouse;
    }

    public int? Pad1DeviceId => _pad1DeviceId;
    public int? Pad2DeviceId => _pad2DeviceId;
    public bool Pad1UsesKeyboardMouse => _pad1KeyboardMouse;

    public int? DeviceId(LineupSeat seat) => seat switch
    {
        LineupSeat.Pad1 => _pad1DeviceId,
        LineupSeat.Pad2 => _pad2DeviceId,
        _ => null
    };

    public bool OwnsDevice(int deviceId) => _pad1DeviceId == deviceId || _pad2DeviceId == deviceId;

    public bool Present(LineupSeat seat, IReadOnlyCollection<int> connectedDeviceIds)
    {
        if (seat == LineupSeat.Cpu) return false;
        if (seat == LineupSeat.Pad1 && _pad1KeyboardMouse) return true;
        var deviceId = DeviceId(seat);
        return deviceId.HasValue && connectedDeviceIds.Contains(deviceId.Value);
    }

    /// <summary>First missing active logical seat. CPU and an unused Pad2 do not need recovery.</summary>
    public LineupSeat Missing(Seats seats, IReadOnlyCollection<int> connectedDeviceIds)
    {
        if (Active(seats, LineupSeat.Pad1) && !Present(LineupSeat.Pad1, connectedDeviceIds))
            return LineupSeat.Pad1;
        if (Active(seats, LineupSeat.Pad2) && !Present(LineupSeat.Pad2, connectedDeviceIds))
            return LineupSeat.Pad2;
        return LineupSeat.Cpu;
    }

    /// <summary>A deliberate South press may give an unowned gamepad to the missing seat.</summary>
    public bool Reassign(LineupSeat seat, int deviceId)
    {
        if (seat == LineupSeat.Cpu || deviceId < 0 || OwnsOtherSeat(seat, deviceId)) return false;
        if (seat == LineupSeat.Pad1)
        {
            _pad1DeviceId = deviceId;
            _pad1KeyboardMouse = false;
        }
        else
            _pad2DeviceId = deviceId;
        return true;
    }

    /// <summary>Keyboard and mouse can deliberately recover Player 1 only.</summary>
    public bool UseKeyboardMouse(LineupSeat seat)
    {
        if (seat != LineupSeat.Pad1) return false;
        _pad1DeviceId = null;
        _pad1KeyboardMouse = true;
        return true;
    }

    static bool Active(Seats seats, LineupSeat seat) => seats.Home == seat || seats.Away == seat;

    bool OwnsOtherSeat(LineupSeat seat, int deviceId) => seat switch
    {
        LineupSeat.Pad1 => _pad2DeviceId == deviceId,
        LineupSeat.Pad2 => _pad1DeviceId == deviceId,
        _ => true
    };
}

/// <summary>
/// Pause policy around a missing match seat. The match loop calls WaitFor before
/// any phase director ticks, so SET, pitch flight, live balls, and throws all use
/// the same recovery boundary.
/// </summary>
public sealed class DeviceSeatRecovery
{
    public bool Active { get; private set; }
    public bool ResumeWhenReady { get; private set; }
    public LineupSeat MissingSeat { get; private set; } = LineupSeat.Cpu;

    public void WaitFor(LineupSeat missingSeat, bool matchWasPaused)
    {
        if (missingSeat == LineupSeat.Cpu)
            throw new ArgumentException("A CPU seat cannot disconnect.", nameof(missingSeat));
        if (!Active) ResumeWhenReady = !matchWasPaused;
        Active = true;
        MissingSeat = missingSeat;
    }

    public void Complete()
    {
        Active = false;
        ResumeWhenReady = false;
        MissingSeat = LineupSeat.Cpu;
    }
}

/// <summary>Shared player-facing copy for the in-match recovery screen.</summary>
public static class SeatRecoveryCopy
{
    public static string Title(LineupSeat seat) =>
        $"{Player(seat)} CONTROLLER LOST";

    public static string[] Lines(LineupSeat seat) => seat == LineupSeat.Pad1
        ?
        [
            "Game paused. Your team stays in your seat.",
            "Reconnect the same controller to resume.",
            "South on an unseated controller takes Player 1.",
            "Space / Enter / left click uses keyboard + mouse this match."
        ]
        :
        [
            "Game paused. Your team stays in your seat.",
            "Reconnect the same controller to resume.",
            "South on an unseated controller takes Player 2."
        ];

    static string Player(LineupSeat seat) => seat == LineupSeat.Pad2 ? "PLAYER 2" : "PLAYER 1";
}
