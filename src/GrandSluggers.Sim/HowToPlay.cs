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
    public sealed record Page(string Id, string Title, string Picture, IReadOnlyList<string> Lines);

    /// <summary>Couch book. Fills most of a 1280×800 player. 12-year-old type.</summary>
    public const float BookMargin = 0.04f;
    public const int KidLineMax = 6;
    public const float KidLineH = 36f;

    public static readonly IReadOnlyList<Page> Pages =
    [
        new("contents", "Contents", "contents",
        [
            "This is the instruction booklet. Call time (H) or Esc opens it.",
            "Pictures first. Short sentences. You can read it from the couch.",
            "South / Space / left click    next page. East / Esc    back.",
            "Pad, keyboard, and mouse all work. Keyboard and mouse are player 1 only.",
            "Exhibition is the game. Training is practice.",
        ]),
        new("controls", "Controls", "exhibition",
        [
            "South / Space / Left click    pitch, swing, catch, throw.",
            "Hold LT / Shift / right click    charge. Rings gold at MAX.",
            "Stick / WASD / mouse    move. D-pad / 1 2 3 4    bags.",
            "Start / H    call time. Esc    this book. East / G / right click    back.",
            "Same verbs on pad and on keyboard + mouse. Mouse is player 1 only.",
        ]),
        new("pitch-swing", "Pitch and swing", "pitch-swing",
        [
            "Tap South / Space / left click to pitch or swing. Hold charge for a bigger one.",
            "Charge ring sits on the dirt. Commit at MAX. Late charge is weaker.",
            "One pad: you pitch over the pitcher's shoulder looking at the box. You hit from behind home looking at the pitcher. The throw does not cut.",
            "Two pads: SET stays on the plate, behind home.",
            "West / V    changeup. North + South    star. Stick L/R at contact    spray.",
            "A ball past the foul line is a foul. Strike unless you already have two.",
        ]),
        new("the-box", "The box and the rubber", "the-box",
        [
            "Stick L/R / mouse    walk the rubber (pitch) or the box (hit).",
            "The sweet-spot oval on the dirt is smaller than the zone. Walk so it eats the ball.",
            "After the pitch is in the air, stick L/R    curve.",
            "D-pad / 1 2 3 + South    pickoff. Select / R    swap a tired pitcher.",
        ]),
        new("running", "Running", "running",
        [
            "Hit it and you run. Live runners must settle on a bag for a second. An out with nobody left ends it. 3 outs too.",
            "LB / ,    all advance. RB / .    all return. Both    halt.",
            "D-pad 1B 2B 3B picks the highlighted selected runner. L3 / Z    steal. No steal home.",
            "Dead stick    the catcher still guns. Early throw    CAUGHT STEALING.",
            "Close play: first South / left click wins. Fly: hold, then tag up.",
        ]),
        new("fielding", "Fielding", "fielding",
        [
            "Don't move: the outfielder runs to the landing on a fly and still can catch. The ball hangs. Contact puffs dirt.",
            "The throw is yours: bag + South. Stick still runs with the ball. They do not gun to first.",
            "Move the stick to take the glove. Select / R swaps — the next glove pulses. Pickup does not end it.",
            "After you throw, you are the glove at that bag. Runner on first: throw both to turn two.",
            "West jump in the window (the circle turns red). East    dive. North    attack. A homer sits on the wall.",
            "On contact the camera sits at 45°. A fly pulls back. CF is the top. Home is under second.",
        ]),
        new("exhibition", "Captain and field", "exhibition",
        [
            "Title is the park (dirt + diamond). GRAND SLUGGERS is a sticker over the infield.",
            "Your captain is the toy in front. North / Q    you are HOME or AWAY. HOME bats the bottom.",
            "Captains are the toys. Stick L/R your team. U/D the other. Camera looks at the toy, not the brim, not the dirt.",
            "South    the field — a postcard with a crowd and a padded wall. Harbor is the slice. The park does not follow the captain.",
        ]),
        new("lineup", "Lineup", "lineup",
        [
            "Team Setup first, then Offense / Defense Setup.",
            "Pick a head. South drops them in. Hearts are buddies. Stars jump when a buddy comes in.",
            "Two diamonds: gloves on P C 1B 2B 3B SS LF CF RF.",
            "South / Space    first pitch.",
        ]),
        new("two-pads", "Two pads", "exhibition",
        [
            "Gamepad 0 is player 1. North picks HOME or AWAY. Gamepad 1 sits the other side. Keyboard and mouse are player 1 only.",
            "Unplug pad 2 and that team is CPU.",
            "You pitch the top. Friend bats. CPU never bats when both pads sit.",
            "Two pads: camera stays on the plate, behind home. The fielding pad takes the glove.",
        ]),
        new("getting-started", "Getting started", "getting-started",
        [
            "South / Space / left click    play ball. Esc    this book.",
            "Exhibition    pick captains, a field, a lineup, play.",
            "Training    Title West. Harbor drills.",
            "Tab    3 / 6 / 9 innings. Home bats the bottom.",
        ]),
        new("screen", "The game screen", "exhibition",
        [
            "Scorebug    inning, runs, stars. B / S / O is balls, strikes, outs.",
            "Batter card    AB. Pitcher card    ARM. Sweat    TIRED. Select / R swaps.",
            "The landing ring is a yellow circle on the grass the ball wants. Red in the jump window. YOU is the glove you have.",
            "ITEM → name when an error item is ready.",
        ]),
        new("chemistry", "Chemistry", "chemistry",
        [
            "Hearts are buddies. Scribbles are rivals.",
            "Buddies throw faster. Rivals miss. Buddy jump. Buddy toss.",
            "A buddy on deck can gift a banana after you hit.",
            "Friends on your team start with more stars.",
        ]),
        new("stars", "Star skills", "stars",
        [
            "You get up to 5 stars. Spend 1 to fire your toy's star.",
            "North + South / Q + Space / middle + left click    fire.",
            "A special breaks a baseball rule for about two seconds, then baseball resumes.",
            "Not a free home run. The ball or the field changes.",
        ]),
        new("abilities", "Who you are", "abilities",
        [
            "Each toy has one field verb. Super Jump / Grow / Lick Catch add range.",
            "The card shows P / B / F / R, the star pitch, the star swing, and the field verb.",
            "Pitchers sweat. Swap when they are TIRED.",
        ]),
        new("items", "Error items", "items",
        [
            "A buddy on deck can give you a banana, rocket, or POW after contact.",
            "Aim with the stick / mouse. Throw with E or left click + right click.",
            "Banana    peel. Rocket    daze. POW    hop. Attack smashes a flying item.",
        ]),
        new("pause-practice", "Pause and Practice", "pause-practice",
        [
            "Start / H    call time. Esc    this book from title too.",
            "South / Space / left click ok. East / G / right click resume.",
            "Title West    Training. F1 F2 F3 stay debug, not this page.",
        ]),
    ];

    public static Page Must(string id) =>
        Pages.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"No how-to-play page '{id}'");

    public static bool Mentions(string needle) =>
        Pages.Any(p =>
            p.Title.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
            p.Lines.Any(l => l.Contains(needle, StringComparison.OrdinalIgnoreCase)));

    public static (float X, float Y, float W, float H) BookPanel(float screenW, float screenH, int lineCount = 0)
    {
        _ = lineCount;
        var x = screenW * BookMargin;
        var y = screenH * BookMargin;
        var w = screenW * (1f - 2f * BookMargin);
        var h = screenH * (1f - 2f * BookMargin);
        return (x, y, w, h);
    }

    public static (float X, float Y, float W, float H) PictureRect(float screenW, float screenH)
    {
        var p = BookPanel(screenW, screenH);
        var top = 88f;
        var foot = 44f;
        var picW = p.W * 0.52f - 20f;
        var picH = p.H - top - foot - 16f;
        return (p.X + 16f, p.Y + top, picW, picH);
    }

    public static (float X, float Y, float W, float H) TextRect(float screenW, float screenH)
    {
        var p = BookPanel(screenW, screenH);
        var pic = PictureRect(screenW, screenH);
        var x = pic.X + pic.W + 16f;
        return (x, pic.Y, p.X + p.W - 16f - x, pic.H);
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
