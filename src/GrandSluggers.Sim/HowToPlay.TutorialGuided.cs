namespace GrandSluggers.Sim;

public static partial class HowToPlay
{
    public static string TutorialGuidedTitle(string id) => id switch
    {
        "T-G01" => "Build your lineup", "T-G05" => "Seat two players", "T-G06" => "Call time",
        "T-G06-R" => "Recover your seat", "T-G06-C" => "Reset the stick", _ => ""
    };
    public static string TutorialGuidedGoal(string id) => id switch
    {
        "T-G01" => "Put a left-handed batter on your team, change batting order, and give a fielder a new glove position.",
        "T-G05" => "Connect two gamepads, choose 2 PLAYERS on captain select, and seat both players in one match.",
        "T-G06" => "Call time, open How to play from that menu, then restart the match from Call time.",
        "T-G06-R" => "Lose an active controller and recover that same player's seat without changing teams.",
        "T-G06-C" => "Complete Reset stick from Call time on a supported controller.",
        _ => ""
    };
    public static string TutorialGuidedSetup(string id) => id switch
    {
        "T-G01" => "Use the ordinary Exhibition team and defense screens. A real roster drop, order change, and glove change must all stick. Each attempt starts fresh.",
        "T-G05" => "Two distinct physical gamepads are required. Keyboard and mouse can only take Player 1. Confirm the seats through the ordinary Select screen.",
        "T-G06" => "Use the ordinary Call time menu in an Exhibition play. Restart begins the same tutorial setup again.",
        "T-G06-R" => "Connect a gamepad and set F6 input to Controller or Auto before starting. The prepared Harbor play begins at SET. Disconnect the active pad; the game pauses. Reconnect it or take that same seat with an unseated pad. Keyboard and mouse can recover Player 1.",
        "T-G06-C" => "On the c80 radial pursuit profile, connect a gamepad and set F6 input to Controller or Auto before starting. The prepared Harbor play begins at SET. In Call time choose Reset stick and release the stick until the new centre is adopted.",
        _ => ""
    };
    public static string TutorialGuidedControls(string id, InputScheme scheme) => id switch
    {
        "T-G01" => scheme == InputScheme.Pad
            ? "South drops a pool head. South after nine opens defense. East changes order; Right shoulder changes the glove."
            : "Space drops a pool head. Space after nine opens defense. G changes order; N changes the glove.",
        "T-G05" => "Connect two pads. On captain select choose 2 PLAYERS, then South to confirm both seats.",
        "T-G06" => scheme == InputScheme.Pad
            ? "Start opens Call time. Choose How to play, return to Call time, then choose Restart."
            : "H opens Call time. Choose How to play, return to Call time, then choose Restart.",
        "T-G06-R" => "An active pad loss pauses the match. Reconnect it or press South on an unseated pad to take that player's seat.",
        "T-G06-C" => "Physical gamepad required. Start opens Call time. Choose Reset stick, then let go until the new centre is accepted.",
        _ => ""
    };
    public static string GuidedNext(IReadOnlyList<GuidedAction> missing) => missing.Count == 0 ? "Ready" : missing[0] switch
    {
        GuidedAction.LeftHandedRosterDrop => "Drop a left-handed batter from the pool",
        GuidedAction.BattingOrderChanged => "Change your batting order",
        GuidedAction.GlovePositionChanged => "Change a field position",
        GuidedAction.TwoPhysicalSeatsBound => "Seat two distinct gamepads",
        GuidedAction.CallTimeOpened => "Open Call time",
        GuidedAction.BookOpened => "Open How to play from Call time",
        GuidedAction.MatchRestarted => "Restart from Call time",
        GuidedAction.SeatLost => "Disconnect an active controller",
        GuidedAction.SeatRecovered => "Recover that same seat",
        GuidedAction.StickRecalibrated => "Complete Reset stick",
        _ => ""
    };
}
