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

    /// <summary>Start / H opens Call time in an at-bat. Front-of-house uses the same menu.</summary>
    public static bool Open(bool paused, bool allowed, bool start, float t) =>
        !paused && allowed && start && t > Debounce;

    /// <summary>Esc opens How to play on title / select / field / lineup without cycling mode.</summary>
    public static bool OpenHowTo(bool paused, bool allowed, bool howTo, float t) =>
        !paused && allowed && howTo && t > Debounce;

    public static bool Dismiss(bool startOrBack, float t) =>
        startOrBack && t > Debounce;

    public const float PanelW = 720f;
    public const float ItemH = 42f;
    public const float FooterH = 44f;

    public static readonly IReadOnlyList<string> FooterLines =
    [
        "stick / click  choose    South / left click ok",
        "Esc / East / right click resume"
    ];

    public static (float X, float Y, float W, float H) Panel(float screenW, float screenH)
    {
        var w = Math.Min(PanelW, Math.Max(16f, screenW - 16f));
        var mh = 64f + Items.Count * ItemH + FooterH + 16f;
        var x = screenW * 0.5f - w * 0.5f;
        var y = Math.Max(8f, screenH * 0.5f - mh * 0.5f);
        return (x, y, w, mh);
    }

    public static (float X, float Y, float W, float H) FooterRect(float screenW, float screenH)
    {
        var p = Panel(screenW, screenH);
        return (p.X + 24f, p.Y + p.H - FooterH - 8f, p.W - 48f, FooterH);
    }

    public static (float X, float Y, float W, float H) ItemRect(int index, float screenW, float screenH)
    {
        var p = Panel(screenW, screenH);
        return (p.X + 24, p.Y + 56 + index * ItemH, p.W - 48, 36);
    }

    public static int HitItem(float mx, float my, float screenW, float screenH)
    {
        for (var i = 0; i < Items.Count; i++)
        {
            var r = ItemRect(i, screenW, screenH);
            if (mx >= r.X && mx <= r.X + r.W && my >= r.Y && my <= r.Y + r.H)
                return i;
        }
        return -1;
    }

    public static bool Contains(float mx, float my, float screenW, float screenH)
    {
        var p = Panel(screenW, screenH);
        return mx >= p.X && mx <= p.X + p.W && my >= p.Y && my <= p.Y + p.H;
    }
}

public static class HowToPlay
{
    public sealed record Page(string Id, string Title, IReadOnlyList<string> Lines);

    public const float BookW = 760f;

    public static readonly IReadOnlyList<Page> Pages =
    [
        new("contents", "Contents",
        [
            "This is Grand Sluggers' instruction booklet. Call time opens it.",
            "Controls                  pad and keyboard / mouse",
            "Pitch and swing           tap, charge, star, bunt, scatter",
            "The box and the rubber    walk, curve, pickoff",
            "Running                   send, return, halt, steal, close plays",
            "Fielding                  glove, throw, jump, attack, buddy",
            "Captain and field         Exhibition front-of-house",
            "Lineup                    chemistry draft",
            "Two pads                  local 1v1",
            "Chemistry · Stars · Abilities · Error items",
            "Pause and Practice        call time, Training",
            "Pad is the nunchuk map: stick + face buttons. Keyboard and mouse are the pointer.",
            "Both work at once. Keyboard and mouse are player 1 only.",
        ]),
        new("controls", "Controls",
            new[]
            {
                "Two schemes. Pad is couch. Keyboard + mouse is the pointer. Same verbs.",
                "Offense and defense share the face buttons. Context decides the verb.",
                "Verb    Pad    Keyboard    Mouse",
            }
                .Concat(Scheme.Product.Select(v => $"{TitleCase(v.Id)}    {v.Pad}    {v.Keys}    {v.Mouse}"))
                .Append("Menus: point / stick, South / left click confirm, East / right click cancel.")
                .ToArray()),
        new("pitch-swing", "Pitch and swing",
        [
            "Same four verbs on the mound and in the box.",
            "Tap South / Space / left click    normal pitch    slap hit (easier contact)",
            "Hold LT / Shift / right click, commit at MAX    charge pitch    charge swing",
            "Rings line up at MAX, then power drops. Late charge is weaker.",
            "MAX commit    Nice! / Nice Hit!",
            "West / V through release    changeup (hangs, then dumps)",
            "SET when you pitch is the mound 3/4: over the pitcher, rubber in the bottom, looking at the box. SET when you bat is the plate 3/4, looking at the mound.",
            "Same recipe with one pad or two — pitcher view vs batter view, not seat count. Pad 2 does not move the HUD.",
            "When they throw, the camera cuts to pitch: arm through, ball leaving that hand. ~1s to the plate (not MLB 90).",
            "West hold / V / Left Ctrl    bunt",
            "North + South / Q + Space / middle + left click    star (costs a star even on a miss)",
            "Stick / mouse L/R at contact    scatter the hit.",
        ]),
        new("the-box", "The box and the rubber",
        [
            "Stick L/R / mouse    walk the rubber (pitch) or the box (hit). Down resets.",
            "Sweet-spot oval on the dirt is smaller than the zone. Walk so it eats the ball.",
            "Stick L/R / mouse after the ball is in the air    curve / late bite.",
            "Not a four-type pitch cycle.",
            "D-pad / 1 2 3 + South before the pitch    pickoff.",
            "A glued runner goes back. A dancing lead can be out.",
            "Select / R    swap pitcher when they sweat.",
        ]),
        new("running", "Running",
        [
            "LB / ,    all advance    RB / .    all return    both / /    halt all",
            "D-pad / 1 2 3    select a runner (right 1B, up 2B, left 3B). Down / 4 is home — not stealable.",
            "Stick toward the next bag    lead on the highlighted runner. Back    return.",
            "Stick toward a bag + halt    freeze that runner only. They hold the lead they have.",
            "L3 / Z    steal the selected runner toward their next bag. They go on the pitch. No steal home.",
            "After a take or miss the catcher guns. Arm 2B (default on a steal of second) and South.",
            "Early throw that beats the runner is CAUGHT STEALING. Late is STOLEN BASE.",
            "Dead stick    CPU catcher still guns. Take the stick and you own it.",
            "Mash South / Space after contact    dash to first.",
            "West / South near the bag    slide.",
            "Close play at 3rd or home    first South / left click after the icon. Runner safe if offense wins.",
            "Fair contact always sends the batter to first.",
            "Fly: hold. All-advance tags up after the catch.",
        ]),
        new("fielding", "Fielding",
        [
            "Dead stick    CPU takes the hop and throws.",
            "Fielders run the hop. Contact puffs dirt. Charge ring sits on the dirt around the box.",
            "Balls into the grass    the outfielder charges and takes the glove.",
            "Stick / WASD / mouse    take the glove. WASD while chasing does not throw.",
            "South / Space / left click    catch, then throw.",
            "On a fly    West jump in the window. South scoops if you are under it. Miss    the ball drops.",
            "A would-be homer sits on the wall. West (or buddy West) in the window robs. South does not.",
            "Super Jump / Grow / Clamber add window, not a skip. Dead stick    CPU still can catch.",
            "Camera    fly is a 3/4 on the glove. Homer rises with the ball, then the wall with the fielder.",
            "Hold East / G    dash. Tap East    dive. West / F    jump / buddy jump.",
            "North / B / middle click    attack. Kick the ball to a nearby glove, or smash a flying item.",
            "E near a chem partner    buddy toss (they laser). Attack also kicks if they are close.",
            "D-pad / 1 2 3 4    arm a bag. Mini-diamond pip is the armed bag.",
            "Hopper default is second when first is occupied, else first. WASD while chasing does not throw.",
            "Runner on first: throw to second (force), you are that glove, throw to first. You throw both. Dead stick CPU can turn two.",
            "LB / X with no bag    relay, not a random bag.",
            "After you throw, you are the glove at that bag.",
            "A steal gun is the same throw to a bag, from the catcher, without a hop.",
            "Select / R swaps. Stick points at who you want.",
        ]),
        new("exhibition", "Captain and field",
        [
            "Title is the park. Logo is a sticker. Home captain is the toy in front. South / Space    play ball.",
            "Captains are the toys. Stick L/R home, U/D away. The HUD card is the UI. The pick steps forward. Camera looks at the toy, not the brim, not the plate dirt.",
            "South    the field — a postcard. Gimmick is one line. Harbor is the slice.",
            "West / F    back. The park does not follow the captain.",
        ]),
        new("lineup", "Lineup",
        [
            "Two screens: Team Setup, then Offense / Defense Setup.",
            "Team Setup: home nine along the top, away nine along the bottom. Center is heads.",
            "Stick picks a head. South drops into the empty slot. West removes. Captain stays.",
            "Hearts are buddies. Scribbles are rivals. Stars jump when a buddy comes in.",
            "Away is CPU until a second pad sits. Pad 1 edits home. Pad 2 edits away. Tab random-fills — not the product path.",
            "Offense / Defense: batting 1–9 as a bar of heads. Two diamonds, gloves on P C 1B 2B 3B SS LF CF RF.",
            "Stick on the bar reorders. Stick on the diamond moves the glove. LB / East still cycle order.",
            "Card stickers the highlighted head. South / Space    first pitch.",
        ]),
        new("two-pads", "Two pads",
        [
            "Gamepad 0 is home. Gamepad 1 is away. Keyboard and mouse are player 1 only.",
            "Missing pad 2    that team is CPU. Unplug pad 2    they become CPU without restarting the inning.",
            "Title / captains / Team Setup / Defense Setup: pad 1 home, pad 2 away. Each edits their captain, roster, order, gloves.",
            "First pitch: pad 1 pitches the top, pad 2 bats. Bottom: they swap. CPU never bats or pitches when both pads are seated.",
            "SET is mound when a human is on the rubber, plate when you bat vs CPU. Same role recipe as 1P. Batter card bottom-left, pitcher card bottom-right. Highlight your card. HUD corners do not move.",
            "Pad-on-mound walks the rubber, charges, throws. Pad-in-the-box walks the box, charges, swings. Same verbs as 1P.",
            "In-play: the fielding pad takes the glove (stick to take, dead stick = CPU cover). The batting pad sends / returns / steals. Both at once.",
        ]),
        new("getting-started", "Getting started",
        [
            "Title is the park. South / Space / left click    play ball (pick captain).",
            "Esc    How to play (this book) from title, captains, field, lineup, or a pitch.",
            "Exhibition    pick captains, a field, a lineup, play. The product.",
            "Training    Title West. Harbor drills: Pitch, Bat, Field, Run, Special, Free.",
            "Two pads    gamepad 0 home, gamepad 1 away. Keyboard and mouse stay player 1.",
            "Challenge, Toy Field, minigames, and records stay later. Exhibition is why people stay.",
            "Tab on the title    3 / 6 / 9 innings. Home bats the bottom.",
        ]),
        new("screen", "The game screen",
        [
            "Scorebug    inning, runs, stars. B / S / O is balls, strikes, outs.",
            "Mini diamond    who is on. Highlighted pip is the selected runner. Leads walk off the bag.",
            "Batter card    AB, next batter, star, steal, error item. Pitcher card    ARM stamina.",
            "When the pitcher sweats, the card says TIRED. Ball speed and control drop. Select / R swaps.",
            "In-play    landing ring is the grass the ball wants. YOU  RF  ·  name is the glove you have. Dead stick    no YOU.",
            "Error item pointer    gold ring on the body, ITEM → name. Stick / mouse aim. E / left+right to throw.",
            "CPU fielding    the camera is the plate 3/4. You still see the mound.",
        ]),
        new("chemistry", "Chemistry",
        [
            "How well toys play together is chemistry. Hearts are buddies. Scribbles are rivals.",
            "Good throwing    faster, on-line, purple laser. Buddy jump and buddy toss.",
            "Bad throwing    slow, off the mark. Sometimes a comedy error.",
            "Good batting    if the on-deck toy likes the batter, an error item appears after contact.",
            "Buddies on base    juice a charge swing. Starting stars come from the captain's friends.",
            "A stacked team of strangers starts starved. A crew that likes each other starts loaded.",
        ]),
        new("stars", "Star skills",
        [
            "Shared meter, max 5. Spend 1 for your toy's star. A guest captain spends 2.",
            "Captains have a unique Star Pitch and Star Swing. Role players get juice (fast / hang / break).",
            "North + South / Q + Space / middle + left click    fire. Costs a star even on a miss.",
            "A special breaks a baseball rule for about two seconds, then baseball resumes.",
            "Not a free home run. The ball or the field changes — not the other player's eyes.",
        ]),
        new("abilities", "Who you are",
        [
            "Each toy has one field verb. Super Jump / Grow / Lick Catch add range.",
            "Dive / Burrow eat grounders. Laser / Snap Throw laser the bag.",
            "Clamber robs a wall. Spin Check knocks an extra-base hit down a bag.",
            "The card shows P / B / F / R, the star pitch, the star swing, and the field verb.",
            "Pitchers sweat. Stamina drops on long outings and star pitches. Swap when they are tired.",
        ]),
        new("items", "Error items",
        [
            "On-deck buddy    banana, rocket, or POW after contact.",
            "Stick / mouse aim at a glove. E / LT+RB / South+LT / left click+right    throw.",
            "Banana    peel on the grass. Rocket    daze the body. POW    infield hop.",
            "Fielding attack (North / B / middle click) smashes a flying item before it lands.",
            "Smoke, ghost, and paint stay banned. No full-screen blinds.",
        ]),
        new("pause-practice", "Pause and Practice",
        [
            "Start / H during a pitch or play    call time (this menu). Esc    How to play from title too.",
            "WASD or arrows choose. South / Space / left click ok. Start / H / Esc or East / G / right click resume.",
            "Click a row. Wheel turns How to play pages.",
            "Resume, Restart, How to play, Title.",
            "Title West    Practice. Stick picks Pitch / Bat / Field / Run / Special / Free.",
            "Fielding    catch, jump a fly, throw a bag, turn two.",
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

    public static (float X, float Y, float W, float H) BookPanel(float screenW, float screenH, int lineCount)
    {
        var h = 64f + lineCount * 24f + 48f;
        var x = screenW * 0.5f - BookW * 0.5f;
        var y = Math.Max(36f, screenH * 0.5f - h * 0.5f);
        return (x, y, BookW, h);
    }

    /// <summary>-1 previous page, 1 next, 0 miss. Left half of the book is back.</summary>
    public static int HitNav(float mx, float my, float screenW, float screenH, int lineCount)
    {
        var p = BookPanel(screenW, screenH, lineCount);
        if (mx < p.X || mx > p.X + p.W || my < p.Y || my > p.Y + p.H) return 0;
        return mx < p.X + p.W * 0.5f ? -1 : 1;
    }

    static string TitleCase(string id)
    {
        if (string.IsNullOrEmpty(id)) return id;
        return string.Join(' ', id.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }
}
