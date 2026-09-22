namespace GrandSluggers.Sim;

/// <summary>
/// Exhibition pregame: captains, the field, and which seat pad 1 sits.
/// Cycling a captain must not move the park; cycling the park must not move the captains.
/// North on select toggles <see cref="Pad1Home"/> — pad 1 can sit away and bat the top.
/// </summary>
public readonly record struct ExhibitionPick(string Home, string Away, string Park, bool Pad1Home = true)
{
    /// <summary>
    /// Harbor is the default park (D21), and this is the one place in <c>GrandSluggers.Sim</c> that spells
    /// its id. Every other rule that wants the default reads this constant; the list of parks and the
    /// home-park map are data (<see cref="ContentCatalog.ParkPickOrder"/>,
    /// <see cref="ContentCatalog.HomeParkIdOfFaction"/>). A source test holds the rule (#820, SF-04).
    /// </summary>
    public const string DefaultPark = "harbor-diamond";

    public static ExhibitionPick Default => new("rio", "ashlord", DefaultPark);

    public string Yours => Pad1Home ? Home : Away;
    public string Theirs => Pad1Home ? Away : Home;

    public static ExhibitionPick CycleHome(ExhibitionPick pick, int dir)
    {
        var home = dir >= 0 ? PresetTeams.NextCaptain(pick.Home) : PresetTeams.PrevCaptain(pick.Home);
        var away = home.Equals(pick.Away, StringComparison.OrdinalIgnoreCase)
            ? PresetTeams.NextCaptain(home)
            : pick.Away;
        return pick with { Home = home, Away = away };
    }

    public static ExhibitionPick CycleAway(ExhibitionPick pick, int dir)
    {
        var away = dir >= 0 ? PresetTeams.NextCaptain(pick.Away) : PresetTeams.PrevCaptain(pick.Away);
        if (away.Equals(pick.Home, StringComparison.OrdinalIgnoreCase))
            away = dir >= 0 ? PresetTeams.NextCaptain(away) : PresetTeams.PrevCaptain(away);
        return pick with { Away = away };
    }

    public static ExhibitionPick CycleYours(ExhibitionPick pick, int dir) =>
        pick.Pad1Home ? CycleHome(pick, dir) : CycleAway(pick, dir);

    public static ExhibitionPick CycleTheirs(ExhibitionPick pick, int dir) =>
        pick.Pad1Home ? CycleAway(pick, dir) : CycleHome(pick, dir);

    public static ExhibitionPick ToggleSeat(ExhibitionPick pick) =>
        pick with { Pad1Home = !pick.Pad1Home };

    public static ExhibitionPick CyclePark(ContentCatalog content, ExhibitionPick pick, int dir) =>
        pick with { Park = WrapPark(content, pick.Park, dir) };

    /// <summary>
    /// The next park in the catalog's declared cycle (<see cref="ContentCatalog.ParkPickOrder"/>). A park
    /// the catalog does not have starts the walk at the first park, so a stale saved pick still cycles.
    /// </summary>
    public static string WrapPark(ContentCatalog content, string parkId, int dir)
    {
        var parks = content.ParkPickOrder;
        var n = parks.Count;
        if (n == 0) return DefaultPark;
        var i = 0;
        for (var k = 0; k < n; k++)
            if (parks[k].Equals(parkId, StringComparison.OrdinalIgnoreCase)) { i = k; break; }
        return parks[(i + dir % n + n) % n];
    }
}
