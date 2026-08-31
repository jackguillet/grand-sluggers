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

    public const float Debounce = 0.2f;

    /// <summary>Start / H opens Call time. The same press must not close it.</summary>
    public static bool Open(bool paused, bool inAtBat, bool start, float t) =>
        !paused && inAtBat && start && t > Debounce;

    public static bool Dismiss(bool startOrBack, float t) =>
        startOrBack && t > Debounce;
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
            "SET is the plate 3/4 for pitch and swing. Same picture with one pad or two.",
            "Training pitching can still stand on the mound.",
            "The pitcher throws at you. Ball leaves that hand. ~1s to the plate (not MLB 90).",
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
            "D-pad / 1 2 3    select a runner (right 1B, up 2B, left 3B). Down / 4 is home — not stealable.",
            "Stick toward the next bag    lead on the highlighted runner. Back    return.",
            "L3 / Z    steal the selected runner toward their next bag. They go on the pitch. No steal home.",
            "Mash South / Space after contact    dash to first.",
            "West / South near the bag    slide.",
            "Fair contact always sends the batter to first.",
            "Fly: hold. All-advance tags up after the catch.",
        ]),
        new("fielding", "Fielding",
        [
            "Dead stick    CPU takes the hop and throws.",
            "Fielders run the hop. Contact puffs dirt. Charge ring sits on the dirt around the box.",
            "Balls into the grass    the outfielder charges and takes the glove.",
            "Stick / WASD    take the glove. WASD while chasing does not throw.",
            "South / Space    catch, then throw.",
            "Hold East / G    dash. Tap East    dive. West / F    jump.",
            "E near a chem partner    buddy toss (they laser).",
            "D-pad / 1 2 3 4    arm a bag. Mini-diamond pip is the armed bag.",
            "Hopper default is second when first is occupied, else first. WASD while chasing does not throw.",
            "Runner on first: throw to second (force), you are that glove, throw to first. You throw both. Dead stick CPU can turn two.",
            "LB / X with no bag    relay, not a random bag.",
            "After you throw, you are the glove at that bag.",
            "Select / R swaps. Stick points at who you want.",
        ]),
        new("exhibition", "Captain and field",
        [
            "Title is the park. Logo is a sticker. Home captain is the toy in front. South / Space    play ball.",
            "Captains are the toys. Stick L/R home, U/D away. The card is the UI.",
            "South    the field — a postcard. Gimmick is one line. Harbor is the slice.",
            "West / F    back. The park does not follow the captain.",
        ]),
        new("lineup", "Lineup",
        [
            "Two screens: Team Setup, then Offense / Defense Setup.",
            "Team Setup: home nine along the top, away nine along the bottom. Center is heads.",
            "Stick picks a head. South drops into the empty slot. West removes. Captain stays.",
            "Hearts are buddies. Scribbles are rivals. Stars jump when a buddy comes in.",
            "Away is CPU until a second pad sits. Tab random-fills — not the product path.",
            "Offense / Defense: batting 1–9 as a bar of heads. Two diamonds, gloves on P C 1B 2B 3B SS LF CF RF.",
            "Stick on the bar reorders. Stick on the diamond moves the glove. LB / East still cycle order.",
            "Card stickers the highlighted head. South / Space    first pitch.",
        ]),
        new("pause-practice", "Pause and Practice",
        [
            "Start / H during a pitch or play    call time (this menu).",
            "WASD or arrows choose. South / Space ok. Start / H or East / G resume.",
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
