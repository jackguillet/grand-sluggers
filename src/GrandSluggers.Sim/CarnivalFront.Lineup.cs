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
    /// <summary>The card's crew badges (WD-28): the player's crews by name, or that they have none.</summary>
    public static string LineupCrewLine(ContentCatalog content, Character who) =>
        who.Crews.Count == 0 ? "No crew" : "CREWS  " + string.Join(" · ", who.Crews.Select(content.CrewName));

    /// <summary>
    /// The words for the rule that decides a pair's chemistry (<see cref="ChemistryTable.Reason(string, string)"/>). The book's
    /// chemistry page and the lineup card read these, so they say the same thing.
    /// </summary>
    public static string ChemistryWhyWord(ChemistryReason reason) => reason.Why switch
    {
        ChemistryWhy.SharedCrew => "shared crew",
        ChemistryWhy.FactionMates => "same home team",
        ChemistryWhy.RivalCrews => "rival crews",
        ChemistryWhy.StoryPair => reason.Chemistry == Chemistry.Bad ? "story rivals" : "story buddies",
        _ => "no link",
    };

    /// <summary>
    /// The card's chemistry line: how the player gets on with their side's captain and why — the shared crew by name, the
    /// same home team, rival crews, a story pair. The captain's own card says so instead.
    /// </summary>
    public static string LineupChemLine(ContentCatalog content, Character captain, Character who)
    {
        if (who.Id.Equals(captain.Id, StringComparison.OrdinalIgnoreCase)) return "CAPTAIN  this is the captain";
        var reason = content.Chemistry.Reason(captain, who);
        var head = "CAPTAIN  " + ChemistryWord(reason.Chemistry);
        if (reason.Why == ChemistryWhy.None) return head;
        return head + ": " + ChemistryWhyWord(reason)
            + (reason.Why == ChemistryWhy.SharedCrew && reason.Crew is { } crew ? " " + content.CrewName(crew) : "");
    }

    public static string LineupFieldCaption(LineupSeat seat, bool onField, bool ready) =>
        LineupSeatName(seat) + "  /  " + (ready ? "READY" : onField ? "FIELD POSITIONS" : "LB/RB  FIELD POSITIONS");
}
