namespace GrandSluggers.Sim.Front;

public static partial class CarnivalFront
{
    /// <summary>The pool's title: the grid is a park crew a row, and it says which rows are on screen.</summary>
    public static string LineupPoolTitle(int top, int shown, int rows) =>
        "PLAYERS BY PARK  /  ROWS " + (top + 1) + "–" + Math.Min(rows, top + shown) + " OF " + rows + "  /  SOUTH TO ADD";
    /// <summary>The tag over a crew row's captain cell: the first word of the crew's home park.</summary>
    public static string LineupCrewTag(ContentCatalog content, Character captain)
    {
        var park = content.Parks.TryGetValue(content.HomeParkIdOfFaction(captain.Faction), out var p) ? p.Name : captain.Faction;
        var space = park.IndexOf(' ');
        return space > 0 ? park[..space] : park;
    }
    /// <summary>A seat's pool cursor is on a crew row scrolled out of view: which way to look.</summary>
    public static string LineupCursorOffscreen(bool pad1, bool above) => (pad1 ? "P1" : "P2") + (above ? " ABOVE" : " BELOW");
    /// <summary>The tag over a grid cell someone has already put on a roster.</summary>
    public static string LineupTakenMark(bool home) => home ? "HOME" : "AWAY";
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
