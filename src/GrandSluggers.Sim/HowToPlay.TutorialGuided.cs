namespace GrandSluggers.Sim;

public static partial class HowToPlay
{
    public static string CardBatHand(Hand hand) => hand == Hand.L ? "BATS LEFT" : "BATS RIGHT";
    public static string TutorialGuidedTitle(string id) => id switch
    {
        "T-G01" => "Build your lineup", "T-G05" => "Seat two players", "T-G06" => "Call time",
        "T-G06-R" => "Recover your seat", "T-G06-C" => "Reset the stick", "T-G07" => "Set the match rules", _ => ""
    };
    public static string TutorialGuidedGoal(string id) => id switch
    {
        "T-G01" => "Put a left-handed batter on your team, change batting order, and give a fielder a new glove position.",
        "T-G05" => "Connect two gamepads, choose 2 controllers in stadium setup, and seat both players in one match.",
        "T-G06" => "Call time, open How to play from that menu, then restart the match from Call time.",
        "T-G06-R" => "Lose an active controller and recover that same player's seat without changing teams.",
        "T-G06-C" => "Complete Reset stick from Call time on a supported controller.",
        "T-G07" => "Choose a stadium or time of day, change Stars, innings and mercy, then ready and play ball with those rules.",
        _ => ""
    };
    public static string TutorialGuidedSetup(string id) => id switch
    {
        "T-G01" => "Choose a stadium and captains, then use the ordinary Exhibition team and defense screens. A real roster drop, order change, and glove change must all stick. Each attempt starts fresh.",
        "T-G05" => "Choose a stadium first. Two distinct physical gamepads are required. Confirm the seats through the ordinary Select screen.",
        "T-G06" => "Use the ordinary Call time menu in an Exhibition play. Restart begins the same tutorial setup again.",
        "T-G06-R" => "Connect a gamepad before starting. The prepared Harbor play begins at SET. Disconnect the active pad; the game pauses. Reconnect it or take that same seat with an unseated pad.",
        "T-G06-C" => "Connect a gamepad before starting. The prepared Harbor play begins at SET. In Call time choose Reset stick and release the stick until the new centre is adopted.",
        "T-G07" => "Each attempt starts on the stadium screen with the default rules. Only Player 1 edits rules. Items stay unavailable. A rule change clears every ready, so each human player readies again. The CPU is always ready and earns nothing. With two controllers both players ready on their own pads.",
        _ => ""
    };
    public static string TutorialGuidedControls(string id) => id switch
    {
        "T-G01" => "South adds a pool player; RB fills the remaining places, but the lesson needs your own left-handed pick first. South after nine opens setup. Pick two batting slots with South to swap. LB/RB switches to the diamond; pick two fielders to swap positions.",
        "T-G05" => "Connect two pads. In stadium setup choose 2 controllers. Each player chooses a captain and presses South to confirm their own seat.",
        "T-G06" => "Start opens Call time. Choose How to play, return to Call time, then choose Restart.",
        "T-G06-R" => "An active pad loss pauses the match. Reconnect it or press South on an unseated pad to take that player's seat.",
        "T-G06-C" => "Physical gamepad required. Start opens Call time. Choose Reset stick, then let go until the new centre is accepted.",
        "T-G07" => "Stadium: up/down chooses Stadium or Time; left/right changes it. Settings: up/down chooses a rule; left/right or South changes it. North readies; East withdraws.",
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
        GuidedAction.StadiumChosen => "Change the stadium or time of day",
        GuidedAction.StarsChanged => "Change Stars in Match settings",
        GuidedAction.InningsChanged => "Change the innings",
        GuidedAction.MercyChanged => "Change the mercy rule",
        GuidedAction.SettingsStarted => "Every player readies to play ball",
        _ => ""
    };
}
