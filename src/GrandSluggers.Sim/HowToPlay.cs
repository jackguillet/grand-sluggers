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
    public sealed record Page(
        string Id,
        string Title,
        string Picture,
        IReadOnlyList<string> Lines,
        IReadOnlyList<string>? KeyLines = null)
    {
        public IReadOnlyList<string> Shown(InputScheme scheme) =>
            scheme == InputScheme.Keys && KeyLines is { Count: > 0 } ? KeyLines : Lines;
    }

    /// <summary>Couch book. Fills most of a 1280×800 player. 12-year-old type.</summary>
    public const float BookMargin = 0.04f;
    public const int KidLineMax = 6;
    public const float KidLineH = 36f;

    public static readonly IReadOnlyList<Page> Pages =
    [
        new("contents", "Contents", "contents",
        [
            "This is the instruction booklet. Call time (Start) opens it. Esc too.",
            "Pictures first. Short sentences. You can read it from the couch.",
            "South next page. East back. Toggle the scheme up top.",
            "Keyboard + mouse is player 1 only.",
            "Exhibition is the game. Training is practice.",
        ],
        [
            "This is the instruction booklet. H or Esc opens it.",
            "Pictures first. Short sentences. You can read it from the couch.",
            "Left click / Space next page. Esc / right click back. Toggle up top.",
            "Keyboard + mouse is player 1 only.",
            "Exhibition is the game. Training is practice.",
        ]),
        new("controls", "Controls", "controls",
        [
            "Green is offense. Red is defense.",
            "South pitches, swings, catches, throws. Hold LT to charge.",
            "Stick runs. D-pad names a bag.",
            "Start calls time. East back.",
            "Keyboard + mouse is player 1 only. Toggle up top.",
        ],
        [
            "Green is offense. Red is defense.",
            "Space / left click pitches, swings, catches, throws. Hold Shift to charge.",
            "WASD runs. 1 2 3 4 name a bag. Right-drag aims.",
            "H calls time. Esc this book.",
            "Keyboard + mouse is player 1 only. Toggle up top.",
        ]),
        new("pitch-swing", "Pitch and swing", "pitch-swing",
        [
            "Tap South to pitch or swing. Hold LT to charge. The pitch has a gold streak.",
            "Charge ring sits on the dirt. Commit at MAX. Late charge is weaker.",
            "One pad: you pitch over the pitcher's shoulder looking at the box. You hit from behind home looking at the pitcher. The throw does not cut.",
            "Two pads: SET stays on the plate, behind home.",
            "West changeup. North + South star. Stick L/R at contact spray.",
            "A ball past the foul line is a foul. Strike unless you already have two.",
        ],
        [
            "Tap Space / left click to pitch or swing. Hold Shift / right click to charge. The pitch has a gold streak.",
            "Charge ring sits on the dirt. Commit at MAX. Late charge is weaker.",
            "You pitch over the pitcher's shoulder looking at the box. You hit from behind home looking at the pitcher. The throw does not cut.",
            "Two pads: SET stays on the plate, behind home. Keyboard + mouse is player 1 only.",
            "V / Ctrl changeup. Q + Space star. A/D at contact spray.",
            "A ball past the foul line is a foul. Strike unless you already have two.",
        ]),
        new("the-box", "The box and the rubber", "the-box",
        [
            "Stick L/R walk the rubber (pitch) or the box (hit).",
            "The sweet-spot oval on the dirt is smaller than the zone. Walk so it eats the ball.",
            "After the pitch is in the air, stick L/R curve.",
            "D-pad 1 2 3 + South pickoff. Select swap a tired pitcher.",
        ],
        [
            "A/D or mouse walk the rubber (pitch) or the box (hit).",
            "The sweet-spot oval on the dirt is smaller than the zone. Walk so it eats the ball.",
            "After the pitch is in the air, A/D curve.",
            "1 2 3 + Space pickoff. R swap a tired pitcher.",
        ]),
        new("running", "Running", "running",
        [
            "Hit it and you run. Live runners must settle on a bag for a second. An out with nobody left ends it. 3 outs too.",
            "LB all advance. RB all return. Both halt.",
            "D-pad 1B 2B 3B picks the highlighted selected runner. L3 steal. No steal home.",
            "Dead stick    the catcher still guns. Early throw    CAUGHT STEALING.",
            "Close play: first South wins. Fly: hold, then tag up.",
            "Have the ball and touch a runner off a bag. That's a tag.",
        ],
        [
            "Hit it and you run. Live runners must settle on a bag for a second. An out with nobody left ends it. 3 outs too.",
            ", all advance. . all return. Both halt.",
            "1 2 3 picks the highlighted selected runner. Z steal. No steal home.",
            "Don't move: the catcher still guns. Early throw    CAUGHT STEALING.",
            "Close play: first Space / left click wins. Fly: hold, then tag up.",
            "Have the ball and touch a runner off a bag. That's a tag.",
        ]),
        new("fielding", "Fielding", "fielding",
        [
            "Don't move: the outfielder runs to the landing on a fly and still can catch. The ball hangs. Contact puffs dirt.",
            "The throw is yours: bag + South. Stick still runs with the ball. Outs land on the catch, the throw, or a tag — they do not guess a force.",
            "Move the stick to take the glove. Select swaps — the next glove pulses. Pickup does not end it.",
            "After you throw, you are the glove at that bag. Runner on first: throw both to turn two.",
            "West jump in the window (the circle turns red). East dive. North attack. A homer sits on the wall.",
            "On contact the camera sits at 45°. A fly pulls back. CF is the top. Home is under second.",
        ],
        [
            "Don't move: the outfielder runs to the landing on a fly and still can catch. The ball hangs. Contact puffs dirt.",
            "The throw is yours: bag + Space. WASD still runs with the ball. Outs land on the catch, the throw, or a tag — they do not guess a force.",
            "WASD takes the glove. R swaps — the next glove pulses. Pickup does not end it.",
            "After you throw, you are the glove at that bag. Runner on first: throw both to turn two.",
            "F jump in the window (the circle turns red). G dive. B attack. A homer sits on the wall.",
            "On contact the camera sits at 45°. A fly pulls back. CF is the top. Home is under second.",
        ]),
        new("exhibition", "Captain and field", "exhibition",
        [
            "Title is the park (dirt + diamond). GRAND SLUGGERS is a sticker over the infield.",
            "Your captain is the toy in front. North you are HOME or AWAY. HOME bats the bottom.",
            "Captains are the toys. Stick L/R your team. U/D the other. Camera looks at the toy, not the brim, not the dirt.",
            "South    the field — a postcard with a crowd and a padded wall. Harbor is the slice. The park does not follow the captain.",
        ],
        [
            "Title is the park (dirt + diamond). GRAND SLUGGERS is a sticker over the infield.",
            "Your captain is the toy in front. Q you are HOME or AWAY. HOME bats the bottom.",
            "Captains are the toys. A/D your team. W/S the other. Camera looks at the toy, not the brim, not the dirt.",
            "Space / left click    the field — a postcard with a crowd and a padded wall. Harbor is the slice. The park does not follow the captain.",
        ]),
        new("lineup", "Lineup", "lineup",
        [
            "Team Setup first, then Offense / Defense Setup.",
            "Pick a head. South drops them in. Hearts are buddies. Stars jump when a buddy comes in.",
            "Two diamonds: gloves on P C 1B 2B 3B SS LF CF RF.",
            "South    first pitch.",
        ],
        [
            "Team Setup first, then Offense / Defense Setup.",
            "Pick a head. Space / left click drops them in. Hearts are buddies. Stars jump when a buddy comes in.",
            "Two diamonds: gloves on P C 1B 2B 3B SS LF CF RF.",
            "Space / left click    first pitch.",
        ]),
        new("two-pads", "Two pads", "exhibition",
        [
            "Gamepad 0 is player 1. North picks HOME or AWAY. Gamepad 1 sits the other side.",
            "Keyboard and mouse are player 1 only.",
            "Unplug pad 2 and that team is CPU.",
            "You pitch the top. Friend bats. CPU never bats when both pads sit.",
            "Two pads: camera stays on the plate, behind home. The fielding pad takes the glove.",
        ],
        [
            "Keyboard and mouse are player 1 only. A second pad is player 2.",
            "Q on pad 1 picks HOME or AWAY.",
            "Unplug pad 2 and that team is CPU.",
            "You pitch the top. Friend bats. CPU never bats when both pads sit.",
            "Two pads: camera stays on the plate, behind home. The fielding pad takes the glove.",
        ]),
        new("getting-started", "Getting started", "getting-started",
        [
            "South    play ball. Esc    this book.",
            "Exhibition    pick captains, a field, a lineup, play.",
            "Training    Title West. Harbor drills.",
            "RB    3 / 6 / 9 innings. Home bats the bottom.",
        ],
        [
            "Space / left click    play ball. Esc    this book.",
            "Exhibition    pick captains, a field, a lineup, play.",
            "Training    Title F. Harbor drills.",
            "Tab    3 / 6 / 9 innings. Home bats the bottom.",
        ]),
        new("screen", "The game screen", "exhibition",
        [
            "Scorebug    inning, runs, stars. B / S / O is balls, strikes, outs.",
            "Batter card    AB. Pitcher card    ARM. Sweat    TIRED. Select swaps.",
            "The landing ring is a yellow circle on the grass the ball wants. Red in the jump window. YOU is the glove you have.",
            "ITEM → name when an error item is ready.",
        ],
        [
            "Scorebug    inning, runs, stars. B / S / O is balls, strikes, outs.",
            "Batter card    AB. Pitcher card    ARM. Sweat    TIRED. R swaps.",
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
            "North + South    fire.",
            "A special breaks a baseball rule for about two seconds, then baseball resumes.",
            "Not a free home run. The ball or the field changes.",
        ],
        [
            "You get up to 5 stars. Spend 1 to fire your toy's star.",
            "Q + Space / middle + left click    fire.",
            "A special breaks a baseball rule for about two seconds, then baseball resumes.",
            "Not a free home run. The ball or the field changes.",
        ]),
        new("abilities", "Who you are", "abilities",
        [
            "Each toy has one field verb. Super Jump / Grow / Lick Catch add range.",
            "The card shows P / B / F / R, the star pitch, the star swing, and the field verb.",
            "Pitchers sweat. Select swaps when they are TIRED.",
        ],
        [
            "Each toy has one field verb. Super Jump / Grow / Lick Catch add range.",
            "The card shows P / B / F / R, the star pitch, the star swing, and the field verb.",
            "Pitchers sweat. R swaps when they are TIRED.",
        ]),
        new("items", "Error items", "items",
        [
            "A buddy on deck can give you a banana, rocket, or POW after contact.",
            "Aim with the stick. Throw with South + LT.",
            "Banana    peel. Rocket    daze. POW    hop. North smashes a flying item.",
        ],
        [
            "A buddy on deck can give you a banana, rocket, or POW after contact.",
            "Aim with the mouse. Throw with E.",
            "Banana    peel. Rocket    daze. POW    hop. B smashes a flying item.",
        ]),
        new("pause-practice", "Pause and Practice", "pause-practice",
        [
            "Start    call time. Esc    this book from title too.",
            "South ok. East resume.",
            "Title West    Training. F1 F2 F3 stay debug, not this page.",
        ],
        [
            "H    call time. Esc    this book from title too.",
            "Space / left click ok. G / right click resume.",
            "Title F    Training. F1 F2 F3 stay debug, not this page.",
        ]),
    ];

    public static Page Must(string id) =>
        Pages.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"No how-to-play page '{id}'");

    public static bool Mentions(string needle) =>
        Pages.Any(p =>
            p.Title.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
            p.Lines.Any(l => l.Contains(needle, StringComparison.OrdinalIgnoreCase)) ||
            (p.KeyLines != null && p.KeyLines.Any(l => l.Contains(needle, StringComparison.OrdinalIgnoreCase))));

    static readonly string[] PadHardware =
        ["South", "East", "West", "North", "D-pad", "LT", "LB", "RB", "L3", "Select", "Start", "Gamepad"];

    static readonly string[] KeyHardware =
        ["Space", "WASD", "left click", "right click", "Shift", "middle click", "Right-drag", "Tab", "Ctrl", "Enter"];

    /// <summary>True if a line names pad hardware and key/mouse hardware together.</summary>
    public static bool MixesHardware(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return false;
        var pad = PadHardware.Any(w => ContainsWord(line, w));
        var stripped = line.Replace("Dead stick", "", StringComparison.OrdinalIgnoreCase);
        if (stripped.Contains("stick", StringComparison.OrdinalIgnoreCase)) pad = true;
        var keys = KeyHardware.Any(w => line.Contains(w, StringComparison.OrdinalIgnoreCase));
        if (line.Contains("mouse", StringComparison.OrdinalIgnoreCase)
            && !line.Contains("player 1 only", StringComparison.OrdinalIgnoreCase))
            keys = true;
        return pad && keys;
    }

    static bool ContainsWord(string line, string word)
    {
        var i = 0;
        while ((i = line.IndexOf(word, i, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var before = i == 0 || !char.IsLetterOrDigit(line[i - 1]);
            var after = i + word.Length >= line.Length || !char.IsLetterOrDigit(line[i + word.Length]);
            if (before && after) return true;
            i += word.Length;
        }
        return false;
    }

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

    /// <summary>-1 previous page, 1 next, 0 miss. Left half of the book is back. Toggle is not nav.</summary>
    public static int HitNav(float mx, float my, float screenW, float screenH, int lineCount)
    {
        if (BookScheme.HitToggle(mx, my, screenW, screenH) is not null) return 0;
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
