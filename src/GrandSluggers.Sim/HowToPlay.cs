namespace GrandSluggers.Sim;

/// <summary>
/// In-game call-time menu. Couch copy lives here so Play can show it and tests can lock it.
/// docs/how-to-play.md is the same map for agents — keep them in the same PR.
/// </summary>
public static class PauseMenu
{
    public enum Item { Resume, Restart, HowToPlay, Title }

    public static readonly IReadOnlyList<Item> Items =
        [Item.Resume, Item.Restart, Item.HowToPlay, Item.Title];

    public static string Label(Item item) => item switch
    {
        Item.Resume => "Resume",
        Item.Restart => "Restart",
        Item.HowToPlay => "How to play",
        Item.Title => "Title",
        _ => item.ToString()
    };

    public static int Wrap(int index, int dir)
    {
        var n = Items.Count;
        return (index + dir % n + n) % n;
    }

    public static Item At(int index) => Items[Wrap(index, 0)];
}

public static class HowToPlay
{
    public sealed record Page(string Id, string Title, IReadOnlyList<string> Lines);

    public static readonly IReadOnlyList<Page> Pages =
    [
        new("controls", "Controls", Scheme.Product.Select(v => $"{TitleCase(v.Id)}    {v.Pad}    {v.Keys}").ToArray()),
        new("pitch-swing", "Pitch and swing",
        [
            "Same four verbs on the mound and in the box.",
            "Tap South / Space    normal pitch    slap hit (easier contact)",
            "Hold LT / Shift, commit at MAX    charge pitch    charge swing",
            "Rings line up at MAX, then power drops. Late charge is weaker.",
            "MAX commit    Nice! / Nice Hit!",
            "West / V through release    changeup (hangs, then dumps)",
            "West hold / V    bunt",
            "North + South / Q + Space    star (costs a star even on a miss)",
        ]),
        new("the-box", "The box and the rubber",
        [
            "Stick L/R    walk the rubber (pitch) or the box (hit). Down resets.",
            "Sweet-spot oval on the dirt is smaller than the zone. Walk so it eats the ball.",
            "Stick L/R after the ball is in the air    curve / late bite.",
            "Not a four-type pitch cycle.",
            "D-pad / 1 2 3 + South before the pitch    pickoff.",
            "A glued runner goes back. A dancing lead can be out.",
            "Select / R    swap pitcher when they sweat.",
        ]),
        new("running", "Running",
        [
            "LB / ,    all advance    RB / .    all return    both / /    freeze",
            "L3 / Z    steal the lead runner. No steal home.",
            "Stick toward the next bag    lead. Back    return.",
            "Mash South / Space after contact    dash to first.",
            "Fair contact always sends the batter to first.",
            "Fly: hold. All-advance tags up after the catch.",
        ]),
        new("fielding", "Fielding",
        [
            "Dead stick    CPU takes the hop and throws.",
            "Balls into the grass    the outfielder charges and takes the glove.",
            "Stick / WASD    take the glove. WASD while chasing does not throw.",
            "South / Space    catch, then throw.",
            "Hold East / G    dash. Tap East    dive. West / F    jump.",
            "E near a chem partner    buddy toss (they laser).",
            "D-pad / 1 2 3 4    arm a bag. Stick-dead hopper goes to first.",
            "LB / X with no bag    relay, not a random bag.",
        ]),
        new("exhibition", "Captain and field",
        [
            "Title is the park. Logo is a sticker. South / Space    play ball.",
            "Captains are the toys. Stick L/R home, U/D away. The card is the UI.",
            "South    the field — a postcard. Gimmick is one line. Harbor is the slice.",
            "West / F    back. The park does not follow the captain.",
        ]),
        new("lineup", "Lineup",
        [
            "The diamond is the draft. Highlighted toy grows. Card stays.",
            "Hearts are buddies. Scribbles are rivals. Stars jump when a buddy comes in.",
            "Stick slot vs pool. West swap. RB glove    P / C / IF / OF.",
            "LB / East    batting order. South / Space    play ball.",
        ]),
        new("pause-practice", "Pause and Practice",
        [
            "Start / H during a pitch or play    call time (this menu).",
            "Resume, Restart, How to play, Title.",
            "Title West    Practice. Stick picks Pitch / Bat / Field / Run / Special / Free.",
            "East from pitching    skip to Fielding (scoop). You are not trapped in lesson 1.",
            "Tab on the title    3 / 6 / 9 innings. Home bats the bottom.",
            "F1 F2 F3 stay debug, not this page.",
        ]),
    ];

    public static Page Must(string id) =>
        Pages.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"No how-to-play page '{id}'");

    public static bool Mentions(string needle) =>
        Pages.Any(p =>
            p.Title.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
            p.Lines.Any(l => l.Contains(needle, StringComparison.OrdinalIgnoreCase)));

    static string TitleCase(string id)
    {
        if (string.IsNullOrEmpty(id)) return id;
        return string.Join(' ', id.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }
}
