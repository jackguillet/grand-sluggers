namespace GrandSluggers.Sim;

/// <summary>
/// Exhibition pregame: captains, the field, and which seat pad 1 sits.
/// Cycling a captain must not move the park; cycling the park must not move the captains.
/// North on select toggles <see cref="Pad1Home"/> — pad 1 can sit away and bat the top.
/// </summary>
public readonly record struct ExhibitionPick(string Home, string Away, string Park, bool Pad1Home = true)
{
    public static readonly string[] Parks =
        ["harbor-diamond", "crystal-rink", "funfair-park", "rooftop-city", "canopy-yard", "ember-keep"];

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

    public static ExhibitionPick CyclePark(ExhibitionPick pick, int dir) =>
        pick with { Park = WrapPark(pick.Park, dir) };

    public static string WrapPark(string parkId, int dir)
    {
        var i = Array.FindIndex(Parks, id => id.Equals(parkId, StringComparison.OrdinalIgnoreCase));
        if (i < 0) i = 0;
        var n = Parks.Length;
        return Parks[(i + dir % n + n) % n];
    }
}
