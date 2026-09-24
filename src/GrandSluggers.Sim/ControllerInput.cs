namespace GrandSluggers.Sim;

[Flags]
public enum ControllerButton
{
    None = 0, South = 1, East = 2, West = 4, North = 8,
    LB = 16, RB = 32, LT = 64, RT = 128, View = 256, Start = 512,
    Up = 1024, Down = 2048, Left = 4096, Right = 8192
}

/// <summary>Controller vocabulary shared by the device reader, role pages, and control reference.</summary>
public static class ControllerLayout
{
    public const ControllerButton Ball = ControllerButton.RT;
    public const ControllerButton Star = ControllerButton.LT;
    public const ControllerButton Advance = ControllerButton.LB;
    public const ControllerButton Return = ControllerButton.RB;
    public const ControllerButton Jump = ControllerButton.North;
    public const ControllerButton DiveCancel = ControllerButton.East;
    public const ControllerButton Attack = ControllerButton.West;
    public const ControllerButton Switch = ControllerButton.LB;
    public const ControllerButton Relay = ControllerButton.RB;
    public const ControllerButton PitchFamily = ControllerButton.West;
    public const ControllerButton BuntThird = ControllerButton.West;
    public const ControllerButton BuntFirst = ControllerButton.North;
    public const ControllerButton Halt = ControllerButton.Up;
    public const ControllerButton All = ControllerButton.Down;
    public static string Label(ControllerButton button) => button.ToString();
}

/// <summary>One seat's edges, trigger hysteresis, release gate, and neutral-separated target flicks.</summary>
public sealed class ControllerInput
{
    ControllerButton _raw, _blocked;
    bool _rt, _lt, _rightArmed = true, _rightActive;
    public ControllerButton Held { get; private set; }
    public ControllerButton Down { get; private set; }
    public ControllerButton Up { get; private set; }
    public int TargetFlick { get; private set; }
    public int TargetHeld { get; private set; }
    public int ThrowTarget { get; private set; }
    public bool IsHeld(ControllerButton b) => (Held & b) != 0;
    public bool IsDown(ControllerButton b) => (Down & b) != 0;
    public bool IsUp(ControllerButton b) => (Up & b) != 0;
    public void ClearTarget() => ThrowTarget = 0;
    public void Catch()
    {
        _blocked |= _raw;
        Held = Down = Up = ControllerButton.None;
        TargetHeld = TargetFlick = 0;
        _rightArmed = _rightActive = false;
    }
    public void Tick(ControllerButton buttons, double rt, double lt, double rightX, double rightY, RulesTable rules)
    {
        // Charge is time held, not trigger depth. Separate release threshold prevents chatter.
        _rt = rt >= (_rt ? .35 : .5);
        _lt = lt >= (_lt ? .35 : .5);
        _raw = buttons | (_rt ? ControllerButton.RT : 0) | (_lt ? ControllerButton.LT : 0);
        _blocked &= _raw;
        var next = _raw & ~_blocked;
        Down = next & ~Held;
        Up = Held & ~next;
        Held = next;
        TargetFlick = 0;
        var length = Math.Sqrt(rightX * rightX + rightY * rightY);
        if (length < .25) { _rightArmed = true; _rightActive = false; }
        TargetHeld = length >= .55 && _rightArmed ? InPlay.DiamondBag(rightX, rightY, rules) : 0;
        if (TargetHeld > 0)
        {
            ThrowTarget = TargetFlick = TargetHeld;
            _rightArmed = false;
            _rightActive = true;
        }
        // Held target is for a deliberate pre-charge pickoff. Flicks alone update the live throw latch.
        if (length >= .55 && _rightActive) TargetHeld = InPlay.DiamondBag(rightX, rightY, rules);
    }
}
