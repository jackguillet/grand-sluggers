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
    public const float KidLineH = 52f;
    /// <summary>IMGUI point size. 24pt vanished at 10 feet.</summary>
    public const int BookLinePt = 36;
    public const int BookLineMinPt = 32;
    public const int BookHeaderPt = 38;
    public const int BookTabPt = 22;
    public const int BookBadgePt = 18;
    public const int BookFooterPt = 36;
    /// <summary>Bottom copy band. Must fit KidLineMax at KidLineH.</summary>
    public const float LineBandMul = 4.6f;

    public static readonly IReadOnlyList<Page> Pages =
    [
        new("contents", "Contents", "contents",
        [
            "Call time or Esc opens this instruction booklet.",
        ],
        [
            "H or Esc opens this instruction booklet. F6 cycles input.",
        ]),
        new("controls", "Controls", "controls",
        [
            "Green is offense. Red is defense.",
            "South pitches, swings, catches, throws. Hold, then release to charge.",
            "Stick runs. D-pad names a bag.",
            "Start calls time. East back.",
        ],
        [
            "Green is offense. Red is defense.",
            "Space / left click pitches, swings, catches, throws. Hold, then release to charge.",
            "WASD runs. 1 2 3 4 name a bag. Right-drag aims.",
            "H calls time. Esc this book.",
        ]),
        new("controls-2", "Controls · Field", "controls",
        ["Hardware controls continue. Green is offense; red is defense."],
        ["Hardware controls continue. Green is offense; red is defense."]),
        new("controls-3", "Controls · Game", "controls",
        ["Hardware controls continue. Green is offense; red is defense."],
        ["Hardware controls continue. Green is offense; red is defense."]),
        new("roles", "In-game controls", "roles",
        [
            "Four tables: batting, pitching, fielding, running.",
            "Verb on the left. What you press on the right.",
            "Same verbs as the hardware page. One scheme at a time.",
        ],
        [
            "Four tables: batting, pitching, fielding, running.",
            "Verb on the left. What you press on the right.",
            "One scheme at a time.",
        ]),
        new("roles-batting-2", "Controls · Batting II", "roles",
        ["Batting verbs and what you press, continued."],
        ["Batting verbs and what you press, continued."]),
        new("roles-pitching", "Controls · Pitching", "roles",
        ["Pitching verbs and what you press."],
        ["Pitching verbs and what you press."]),
        new("roles-pitching-2", "Controls · Pitching II", "roles",
        ["Pitching verbs and what you press, continued."],
        ["Pitching verbs and what you press, continued."]),
        new("roles-fielding", "Controls · Fielding", "roles",
        ["Fielding verbs and what you press."],
        ["Fielding verbs and what you press."]),
        new("roles-running", "Controls · Running", "roles",
        ["Running verbs and what you press."],
        ["Running verbs and what you press."]),
        new("pitch-swing", "Pitch and swing", "pitch-swing",
        [
            "Tap for normal before the gold streak reaches home. Hold/release at MAX for a charge.",
            "One controller: pitch from the pitcher's shoulder; hit from behind home. The throw does not cut.",
            "Two controllers: SET stays on the plate, behind home.",
            "Changeup: hold West, then South. Bunt: hold West. Star: North + South. Spray: stick L/R. Past line: foul; Strike unless two.",
        ],
        [
            "Tap for normal before the gold streak reaches home. Hold/release at MAX for a charge.",
            "Pitch from the pitcher's shoulder; hit from behind home. The throw does not cut.",
            "Two controllers: SET stays on the plate, behind home.",
            "Changeup: hold V/Ctrl, then Space/left click. Bunt: hold V/Ctrl. Star: Q+Space. Spray: A/D. Past line foul; Strike unless two.",
        ]),
        new("the-box", "The box and the rubber", "the-box",
        [
            "Stick L/R walk the rubber (pitch) or the box (hit).",
            "A sitting stick does not walk. Flick from rest.",
            "The sweet-spot oval follows the batter, never the pitch. Walk so its center eats the ball.",
            "After the pitch is in the air, stick L/R break. A pale ring on the plate is where it will cross.",
            "Take outside the white frame: ball. Swing and miss outside: strike.",
            "D-pad 1 2 3 + South pickoff. Select opens the swap: stick picks any fielder, Select again.",
        ],
        [
            "A/D or mouse walk the rubber (pitch) or the box (hit).",
            "Keys already down at SET do not walk. Right-drag is this-frame. A parked cursor is dead.",
            "The sweet-spot oval follows the batter, never the pitch. Walk so its center eats the ball.",
            "After the pitch is in the air, A/D break. A pale ring on the plate is where it will cross.",
            "Take outside the white frame: ball. Swing and miss outside: strike.",
            "1 2 3 + Space pickoff. R opens the swap: A/D picks any fielder, R again.",
        ]),
        new("running", "Running", "running",
        [
            "Hit it and you run. Runners stand on the bag until contact, a steal, or a send: there is no lead. Live runners must settle on a bag for a second. An out with nobody left ends it. 3 outs too.",
            "The pictures are the diamond. LB all advance. RB all return. Both halt. A tap of the other shoulder halts a runner who is going. Forced runners go anyway.",
            "D-pad 1B 2B 3B picks the highlighted selected runner; down is the batter once the ball is live. Stick toward the next bag sends the selected runner, back returns them. Before the pitch the same stick or L3 arms a steal. No steal home.",
            "Dead stick    the catcher still guns. Early throw    CAUGHT STEALING.",
            "Fly: everyone goes back to the bag until the catch or the drop; LB before the catch is tag and go. Close play and tag are the pictures below. First South wins. A bang-bang SAFE pops small. Have the ball and touch a runner off a bag to tag.",
        ],
        [
            "Hit it and you run. Runners stand on the bag until contact, a steal, or a send: there is no lead. Live runners must settle on a bag for a second. An out with nobody left ends it. 3 outs too.",
            "The pictures are the diamond. , all advance. . all return. / halts both. A tap of the other key halts a runner who is going. Forced runners go anyway.",
            "1 2 3 picks the highlighted selected runner; 4 is the batter once the ball is live. WASD toward the next bag sends the selected runner, back returns them. Before the pitch the same keys or Z arm a steal. No steal home.",
            "Don't move: the catcher still guns. Early throw    CAUGHT STEALING.",
            "Fly: everyone goes back to the bag until the catch or the drop; , before the catch is tag and go. Close play and tag are the pictures below. First Space / left click wins. A bang-bang SAFE pops small. Touch a runner off a bag to tag.",
        ]),
        new("fielding", "Fielding", "fielding",
        [
            "On fly: outfielder runs to landing; ball hangs. Contact puffs dirt.",
            "The throw is yours. Bag + South; stick runs with the ball; catch, throw, or tag: out. Force: 2B with first; 3B with first + second; home loaded.",
            "After a throw, you are the glove at that bag. Runner on first: throw both to turn two.",
            "YOU names glove. Dead stick auto-runs; stick steers. Stand on ball to scoop. Select swaps; next glove pulses. Pickup stays live.",
            "West jumps in window; the circle turns red. East dives. North attack. A homer sits on wall.",
            "Camera is 45°; a fly pulls back. CF is the top. Home is under second.",
        ],
        [
            "On fly: outfielder runs to landing; ball hangs. Contact puffs dirt.",
            "The throw is yours. Bag + Space; WASD runs with the ball; catch, throw, or tag: out. Force: 2B with first; 3B with first + second; home loaded.",
            "After a throw, you are the glove at that bag. Runner on first: throw both to turn two.",
            "YOU names glove. Don't move: they auto-run; WASD steers. Stand on ball to scoop. R swaps; next glove pulses. Pickup stays live.",
            "F jumps in window; the circle turns red. G dives. B attack. A homer sits on wall.",
            "Camera is 45°; a fly pulls back. CF is the top. Home is under second.",
        ]),
        new("exhibition", "Captain and field", "exhibition",
        [
            "Title is the park (dirt + diamond). GRAND SLUGGERS is a sticker over the infield.",
            "Your captain is the toy in front. North you are HOME or AWAY. HOME bats the bottom.",
            "Captains are the toys. Stick L/R your team. U/D the other. Camera looks at the toy, not the brim, not the dirt.",
            "LB 1 PLAYER. RB 2 PLAYERS. Two players needs controller 2.",
            "South    the field — a postcard with a crowd and a padded wall. Harbor is the slice. The park does not follow the captain.",
        ],
        [
            "Title is the park (dirt + diamond). GRAND SLUGGERS is a sticker over the infield.",
            "Your captain is the toy in front. Q you are HOME or AWAY. HOME bats the bottom.",
            "Captains are the toys. A/D your team. W/S the other. Camera looks at the toy, not the brim, not the dirt.",
            "Comma 1 PLAYER. Tab 2 PLAYERS. Two players needs controller 2.",
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
        new("two-pads", "Two controllers", "exhibition",
        [
            "Pick 2 PLAYERS on captains. The first controller is player 1. North picks HOME or AWAY. The second controller sits the other side.",
            "Keyboard and mouse are player 1 only.",
            "If a seated controller drops, play stops. The other controller keeps its team.",
            "Reconnect it, or press South on an unseated controller to take that seat.",
            "CPU never bats or pitches while both controllers are seated.",
            "Two controllers: camera stays on the plate, behind home. The fielding controller takes the glove.",
        ],
        [
            "Pick 2 PLAYERS on captains. Keyboard and mouse are player 1 only. A second controller is player 2.",
            "Player 1: Q picks HOME or AWAY.",
            "If Player 1's controller drops, play stops. Space / left click takes that seat on keyboard + mouse.",
            "Player 2 reconnects the same controller, or takes the seat on an unseated controller.",
            "CPU never bats or pitches while both seats are connected.",
            "Two controllers: camera stays on the plate, behind home. The fielding controller takes the glove.",
        ]),
        new("getting-started", "Getting started", "getting-started",
        [
            "The pictures are the path. Exhibition is the game; Training is practice.",
            "South play ball. Esc this book. 2 PLAYERS seats controller 2; a seat drop pauses. Home bats bottom. Harbor.",
        ],
        [
            "The pictures are the path. Exhibition is the game; Training is practice.",
            "Space / left click play ball. Esc this book. 2 PLAYERS seats controller 2; a seat drop pauses. Home bats bottom. Harbor.",
        ]),
        new("getting-started-modes", "Getting started · Modes", "getting-started",
        [
            "Exhibition is the game; Training is practice.",
            "2 PLAYERS seats controller 2. A seat drop pauses. Home bats bottom. Harbor.",
        ],
        [
            "Exhibition is the game; Training is practice.",
            "Keyboard + mouse is player 1. 2 PLAYERS seats controller 2. A seat drop pauses.",
        ]),
        new("screen", "The game screen", "exhibition",
        [
            "Scorebug: inning, runs, stars; B / S / O.",
            "Cards: batter AB, pitcher ARM and TIRED. Select swaps.",
            "Yellow circle: landing ring. Red: jump window. YOU: the glove.",
            "ITEM names an error item.",
            "A stamp on the field names BALL, STRIKE, FOUL, WALK, outs, hits, steals, and homers; then SET starts the next pitch.",
            "Runners still going keep play alive.",
        ],
        [
            "Scorebug: inning, runs, stars; B / S / O.",
            "Cards: batter AB, pitcher ARM and TIRED. R swaps.",
            "Yellow circle: landing ring. Red: jump window. YOU: the glove.",
            "ITEM names an error item.",
            "A stamp on the field names BALL, STRIKE, FOUL, WALK, outs, hits, steals, and homers; then SET starts the next pitch.",
            "Runners still going keep play alive.",
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
        new("abilities-types", "Who you are · Skills", "abilities",
        [
            "The four ability types and what each one changes.",
        ],
        [
            "The four ability types and what each one changes.",
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

    /// <summary>Splash stills ate the type. Diagram pages draw their own boards.</summary>
    public static bool ShowsSplash(string id)
    {
        _ = id;
        return false;
    }

    public static (float X, float Y, float W, float H) PictureRect(float screenW, float screenH)
    {
        var p = BookPanel(screenW, screenH);
        var top = 108f;
        return (p.X + 16f, p.Y + top, p.W - 32f, 0f);
    }

    public static (float X, float Y, float W, float H) TextRect(float screenW, float screenH)
    {
        var p = BookPanel(screenW, screenH);
        var top = 108f;
        var foot = 48f;
        return (p.X + 28f, p.Y + top, p.W - 56f, p.H - top - foot);
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
