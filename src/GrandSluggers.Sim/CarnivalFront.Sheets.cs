namespace GrandSluggers.Sim.Front;

/// <summary>The front-of-house sheets' words (#1048): the title, the setup sheet, the team and lineup sheets, the defense window.</summary>
public static partial class CarnivalFront
{
    // Title
    public const string ChallengeTitle = "CHALLENGE";
    public const string ChallengeNext = "South  ·  next match";
    public const string HarborIsTheSlice = "Harbor is the slice.";
    public static string TrialLabel(string overlay) => "TRIAL  " + overlay;

    // Setup sheet
    public const string ConnectTitle = "CONNECT A CONTROLLER";
    public const string ConnectBody = "Plug in or pair a controller to play. Two controllers play together.";
    public static readonly IReadOnlyList<string> TitleMenu = ["Exhibition", "Tutorials", "Controls", "Quit"];
    public static string[] StadiumRows(string park, bool night, bool hazards, bool versus, bool home) =>
    [
        "Stadium: " + park, "Time: " + (night ? "Night" : "Day"),
        "Hazards: " + (hazards ? "On" : "Off"), "Players: " + (versus ? "2 controllers" : "1 vs CPU"),
        "P1 side: " + (home ? "Home" : "Away"), "Choose captains",
    ];
    public const string StadiumHelp = "Up/down choose • Left/right change • South confirm • East back";

    // Team and lineup sheets
    public static string TeamStep(bool team) => team ? "03  /  CHOOSE YOUR TEAM" : "04  /  SET YOUR LINEUP";
    public static string TeamTitle(bool team) => team ? "Build your nine" : "Batting order & field positions";
    public const string ExhibitionTag = "EXHIBITION";
    public const string SettingsFollow = "MATCH SETTINGS FOLLOW LINEUP";
    public static string BackButton(bool team, bool ready, bool picking) =>
        team ? "East  Captains" : ready ? "East  Unready" : picking ? "East  Cancel" : "East  Back";
    public const string FillButton = "RB  Fill team";
    public static string ContinueButton(bool team, bool ready) =>
        team ? "South  Continue" : ready ? "P1 READY · waiting" : "North  Ready →";
    public static string SeatName(LineupSeat seat) => seat == LineupSeat.Cpu ? "CPU" : seat == LineupSeat.Pad1 ? "P1" : "P2";
    public const string CaptainMark = " · C";
    /// <summary>The badge on a lineup cell a pad has its cursor on: which pads.</summary>
    public static string SeatBadge(bool pad1, bool pad2) => pad1 && pad2 ? "P1\nP2" : pad1 ? "P1" : "P2";
    public const string Picked = "PICKED";
    public const string Inspect = "INSPECT";
    public const string OpenSlot = "OPEN";
    public const string InspectHint = "Move to a player\nto inspect their card.";
    /// <summary>The four bars every card prints, in order (<see cref="StatBars"/>).</summary>
    public static readonly IReadOnlyList<string> StatBarLabels = ["BAT", "PITCH", "FIELD", "RUN"];
    /// <summary>The lineup inspection card's portrait: a square at (14, 78) in the card.</summary>
    public const float LineupCardPortrait = 114;
    /// <summary>The lineup inspection card's four bars (<see cref="StatBars"/>), beside its portrait.</summary>
    public static readonly StatBarLayout LineupCardBars = new(Top: 76, Pitch: 29, LabelX: 156, LabelW: 62,
        BarX: 220, BarW: 66, BarH: 12, ValueX: 292, ValueW: 30, LabelFont: 18, ValueFont: 20);
    /// <summary>
    /// Where the lineup card's four text lines start, under the bars and the portrait, and their pitch: the star pitch and
    /// swing, the field verb and bat hand, the crews (WD-28), and the chemistry with the captain and why.
    /// </summary>
    public const float LineupCardVerbsTop = 194;
    public const float LineupCardLinePitch = 21;
    public const int LineupCardLines = 4;
    /// <summary>The pitcher-pick card's four stats, pitch first.</summary>
    public static readonly IReadOnlyList<string> CardStats = ["PITCH", "BAT", "FIELD", "RUN"];
    /// <summary>The select card's stat rows: pitch, bat, field, run.</summary>
    public const string StatPitch = "PIT", StatBat = "BAT", StatField = "FLD", StatRun = "RUN";

    // Defense window
    public static string DefenseStep(int seat) => "P" + (seat + 1) + "  /  DEFENSE";
    public const string DefenseTitle = "Arrange defense";
    public static string OnTheMound(string pitcher, string armLine) => pitcher + " on the mound  ·  " + armLine;
    public static string PlayerCardHead(string pos) => pos + "  /  PLAYER CARD";
    public static string ThrowsLine(Hand throws, string pitches) => (throws == Hand.L ? "Throws left" : "Throws right") + "  ·  " + pitches;
    public const string ChemistryKey = "CHEMISTRY WITH YOUR FOCUS  ·  Good / Poor / Neutral";
    public const string DefenseHelp = "Stick / D-pad  Move · South  Pick & swap";
    public static string SwapButton(bool canSwap) => canSwap ? "West  Quick swap to mound" : "Pitcher changed this half";
    public static string DefenseCancel(bool picking) => picking ? "East  Cancel pick" : "East  Close";
    public static string ChemistryWord(Chemistry chemistry) =>
        chemistry == Chemistry.Good ? "Good" : chemistry == Chemistry.Bad ? "Poor" : "Neutral";
}
