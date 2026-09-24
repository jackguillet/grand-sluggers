namespace GrandSluggers.Sim.Front;

public static partial class CarnivalFront
{
    public const string LineupPoolTitle = "AVAILABLE PLAYERS  /  SOUTH TO ADD";
    public const string LineupTeamHelp = "Stick  Choose · South  Add / continue\nWest  Remove from roster · View  How to play";
    public const string LineupPositionsHelp = "Stick  Choose · South  Pick / swap · LB/RB  Order / field\nEast  Cancel / unready · North  Ready · View  How to play";
    static string LineupSeatName(LineupSeat seat) => seat == LineupSeat.Pad1 ? "P1" : seat == LineupSeat.Pad2 ? "P2" : "CPU";
    public static string LineupTeamCaption(LineupSeat seat, bool home, string captain, int count, bool ready) =>
        LineupSeatName(seat) + "  /  " + (home ? "HOME" : "AWAY") + "  /  " + captain.ToUpperInvariant()
        + "   ·   " + (ready ? "READY" : count + " / 9");
    public static string LineupCardCaption(LineupSeat seat, bool home, bool ready) =>
        LineupSeatName(seat) + "  /  " + (home ? "HOME" : "AWAY") + "   ·   " + (ready ? "READY" : "PLAYER CARD");
    public static string LineupFieldCaption(LineupSeat seat, bool onField, bool ready) =>
        LineupSeatName(seat) + "  /  " + (ready ? "READY" : onField ? "FIELD POSITIONS" : "LB/RB  FIELD POSITIONS");
}
