namespace GrandSluggers.Sim;

/// <summary>
/// In-game call-time menu. Couch copy lives here so Play can show it and tests can lock it.
/// docs/how-to-play.md is the same map for agents — keep them in the same PR.
/// </summary>
public static class PauseMenu
{
    public enum Item { Resume, Restart, HowToPlay, Title, ResetStick, ArrangeDefense, Quit }

    public static readonly IReadOnlyList<Item> Items =
        [Item.Resume, Item.Restart, Item.HowToPlay, Item.ArrangeDefense, Item.Title, Item.Quit];

    /// <summary>
    /// Call time on the calibrated pursuit stick with a seated controller (#718, F693-02-pursuit-calibration-policy): the
    /// explicit recalibration sits between the book and Title, so the book keeps its row and Title stays last.
    /// </summary>
    public static readonly IReadOnlyList<Item> ItemsWithStick =
        [Item.Resume, Item.Restart, Item.HowToPlay, Item.ResetStick, Item.ArrangeDefense, Item.Title, Item.Quit];

    public static IReadOnlyList<Item> ItemsFor(bool stick) => stick ? ItemsWithStick : Items;

    public static string Label(Item item) => item switch
    {
        Item.Resume => "Resume",
        Item.Restart => "Restart",
        Item.HowToPlay => "How to play",
        Item.Title => "Title",
        Item.ResetStick => "Reset stick",
        Item.ArrangeDefense => "Arrange defense (before pitch)",
        Item.Quit => "Quit game",
        _ => item.ToString()
    };

    public static int Wrap(int index, int dir) => Wrap(index, dir, false);

    public static int Wrap(int index, int dir, bool stick)
    {
        var n = ItemsFor(stick).Count;
        return (index + dir % n + n) % n;
    }

    public static Item At(int index) => At(index, false);

    public static Item At(int index, bool stick) => ItemsFor(stick)[Wrap(index, 0, stick)];

    /// <summary>The Reset stick card (#718): the release-stick instruction while each seated controller samples its window.</summary>
    public const string StickResetTitle = "RESET STICK";

    public static readonly IReadOnlyList<string> StickResetLines =
    [
        "Let go of the stick for half a second.",
        "East back — the old centre stays."
    ];

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
        "Stick / D-pad choose    South confirm",
        "East / Start resume"
    ];

    public static (float X, float Y, float W, float H) Panel(float screenW, float screenH) => Panel(screenW, screenH, false);

    public static (float X, float Y, float W, float H) Panel(float screenW, float screenH, bool stick)
    {
        var w = Math.Min(PanelW, Math.Max(16f, screenW - 16f));
        var mh = 64f + ItemsFor(stick).Count * ItemH + FooterH + 16f;
        var x = screenW * 0.5f - w * 0.5f;
        var y = Math.Max(8f, screenH * 0.5f - mh * 0.5f);
        return (x, y, w, mh);
    }

    public static (float X, float Y, float W, float H) FooterRect(float screenW, float screenH) => FooterRect(screenW, screenH, false);

    public static (float X, float Y, float W, float H) FooterRect(float screenW, float screenH, bool stick)
    {
        var p = Panel(screenW, screenH, stick);
        return (p.X + 24f, p.Y + p.H - FooterH - 8f, p.W - 48f, FooterH);
    }

    public static (float X, float Y, float W, float H) ItemRect(int index, float screenW, float screenH) =>
        ItemRect(index, screenW, screenH, false);

    public static (float X, float Y, float W, float H) ItemRect(int index, float screenW, float screenH, bool stick)
    {
        var p = Panel(screenW, screenH, stick);
        return (p.X + 24, p.Y + 56 + index * ItemH, p.W - 48, 36);
    }

    public static int HitItem(float mx, float my, float screenW, float screenH) => HitItem(mx, my, screenW, screenH, false);

    public static int HitItem(float mx, float my, float screenW, float screenH, bool stick)
    {
        for (var i = 0; i < ItemsFor(stick).Count; i++)
        {
            var r = ItemRect(i, screenW, screenH, stick);
            if (mx >= r.X && mx <= r.X + r.W && my >= r.Y && my <= r.Y + r.H)
                return i;
        }
        return -1;
    }

    public static bool Contains(float mx, float my, float screenW, float screenH) => Contains(mx, my, screenW, screenH, false);

    public static bool Contains(float mx, float my, float screenW, float screenH, bool stick)
    {
        var p = Panel(screenW, screenH, stick);
        return mx >= p.X && mx <= p.X + p.W && my >= p.Y && my <= p.Y + p.H;
    }
}

public static partial class HowToPlay
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
            "Call time has How to play. View opens this book from title too.",
        ],
        [
            "H calls time. View this book. F6 cycles input.",
        ]),
        new("controls", "Controls", "controls",
        [
            "Green is offense. Red is defense.",
            "RT pitches, swings and throws. Catch automatically by position.",
            "Left stick moves. Right stick selects.",
            "Start calls time. East back.",
        ],
        [
            "Green is offense. Red is defense.",
            "Space / left click pitches, swings, catches, throws. Hold, then release to charge.",
            "WASD runs. 1 2 3 4 name a bag. Right-drag aims.",
            "H calls time. View this book.",
        ]),
        new("controls-2", "Controls · Field", "controls",
        ["Hardware controls continue. Green is offense; red is defense."],
        ["Hardware controls continue. Green is offense; red is defense."]),
        new("controls-3", "Controls · Game", "controls",
        ["Hardware controls continue. Green is offense; red is defense."],
        ["Hardware controls continue. Green is offense; red is defense."]),
        new("controls-4", "Controls · Menus", "controls", ["View opens this book. Start opens options."]),
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
        new("roles-fielding-2", "Controls · Fielding II", "roles",
        ["Fielding verbs and what you press, continued."],
        ["Fielding verbs and what you press, continued."]),
        new("roles-running", "Controls · Running", "roles",
        ["Running verbs and what you press."],
        ["Running verbs and what you press."]),
        new("roles-running-2", "Controls · Running II", "roles",
        ["Running verbs and what you press, continued."],
        ["Running verbs and what you press, continued."]),
        new("pitch-swing", "Pitch and swing", "pitch-swing",
        [
            "Swing when the ball is on the plate (gold streak). Tap RT: normal. Hold/release at MAX: charge.",
            "One controller: pitch from the pitcher's shoulder; hit from behind home. The throw does not cut.",
            "Two controllers: SET stays behind the plate.",
            "Cycle pitch: West before charge. Bunt: hold West / North. East cancels a swing load. Star: hold LT at RT release.",
        ],
        [
            "Swing when the ball is on the plate (gold streak). Tap Space: normal. Hold/release at MAX: charge.",
            "Pitch from the pitcher's shoulder; hit from behind home. The throw does not cut.",
            "Two controllers: SET stays behind the plate.",
            "Cycle pitch: Tab before the charge (FB, 2nd, 3rd). Bunt: hold J or L. G cancels a load. Star: hold Q at release. Past line foul; Strike unless two.",
        ]),
        new("the-box", "The box and the rubber", "the-box",
        [
            "Stick L/R walk the rubber (pitch) or the box (hit).",
            "A sitting stick does not walk. Flick from rest.",
            "The sweet-spot oval follows the batter, never the pitch; the box resets each pitch.",
            "A pale ring in SET marks your rubber, not where the pitch will cross. In the air, stick L/R break.",
            "Take outside the white frame: ball. Swing and miss outside: strike.",
            "Right stick base + RT: pickoff before charge; on the bag is safe. Start → Arrange defense moves any fielder.",
        ],
        [
            "A/D or mouse walk the rubber (pitch) or the box (hit).",
            "Keys already down at SET do not walk. Right-drag is this-frame. A parked cursor is dead.",
            "The sweet-spot oval follows the batter, never the pitch; the box resets each pitch.",
            "A pale ring in SET marks your rubber, not where the pitch will cross. In the air, A/D break.",
            "Take outside the white frame: ball. Swing and miss outside: strike.",
            "1/2/3/4 + Space: pickoff before charge; on the bag is safe. R opens defense to move any fielder.",
        ]),
        new("arrange-defense", "Arrange defense", "exhibition",
        [
            "Start → Arrange defense before charging. Inspect stats, ARM, pitches and chemistry.",
            "Stick moves. South picks a player, then a second position to swap the pair.",
            "West quick-swaps your focus to P. One pitcher change per half; other gloves stay free.",
            "East cancels a pick or closes. Done keeps completed swaps and returns to play.",
            "Good / Poor / Neutral compares teammates with your focus. Stamina and batting order stay.",
        ],
        [
            "R before charging opens defense. Inspect stats, ARM, pitches and chemistry.",
            "WASD or hover moves. Space / click picks a player, then a second position to swap the pair.",
            "R quick-swaps your focus to P. One pitcher change per half; other gloves stay free.",
            "G cancels a pick or closes. Done keeps completed swaps and returns to play.",
            "Good / Poor / Neutral compares teammates with your focus. Stamina and batting order stay.",
        ]),
        new("running", "Running", "running",
        [
            "Hit it and you run. Runners stand on the bag until contact, a steal, or a send: there is no lead. Live runners must settle on a bag for a second. An out with nobody left ends it. 3 outs too.",
            "The small runner label shows your selection. LB sends the selection; RB returns it immediately. D-pad Up halts. D-pad Down selects ALL. These commands also work before contact. LT owns Stars.",
            "Right stick flick selects: right 1B, up 2B, left 3B, down batter. Selection follows that runner. Recenter before the next flick. Empty selections never order ALL.",
            "The tag decides: CAUGHT STEALING or STOLEN BASE. Before the catch, an inset shows the race. At the catch, the normal live-play camera follows the ball through the throw. Pickoffs and rundowns use that same view.",
            "Fly: everyone goes back to the bag until the catch or the drop; LB before the catch is tag and go. Close play and tag are the pictures below. First South wins. A bang-bang SAFE pops small. Have the ball and touch a runner off a bag to tag.",
        ],
        [
            "Hit it and you run. Runners stand on the bag until contact, a steal, or a send: there is no lead. Live runners must settle on a bag for a second. An out with nobody left ends it. 3 outs too.",
            "The pictures are the diamond. After contact , all advance, . all return. / halts both. A tap of the other key halts a runner who is going. Forced runners go anyway.",
            "1 2 3 picks the highlighted selected runner; 4 is the batter once the ball is live. WASD toward the next bag sends the selected runner, back returns them. Before or during the pitch, the same keys or Z starts a steal NOW, home included. Go back or send again from where you stand.",
            "The tag decides: CAUGHT STEALING or STOLEN BASE. Before the catch, an inset shows the race. At the catch, the normal live-play camera follows the ball through the throw. Pickoffs and rundowns use that same view.",
            "Fly: everyone goes back to the bag until the catch or the drop; , before the catch is tag and go. Close play and tag are the pictures below. First Space / left click wins. A bang-bang SAFE pops small. Touch a runner off a bag to tag.",
        ]),
        new("steal-race", "Steals and commitment", "running",
        [
            "LB sends your runner NOW. RB returns immediately. Off the bag is vulnerable.",
            "Before charge: Right stick base + RT throws there. Any of the four bases, even empty ones.",
            "Hold RT: committed. Choose a base while held: BALK. Runners gain one; count unchanged.",
            "You can hold the pitch indefinitely. Let go to deliver it; the catcher can challenge the runner.",
            "In flight, Right stick chooses the catcher target. Press RT just before the catch to buffer a throw; East cancels. Transfer comes before release.",
        ],
        [
            "Z sends your runner NOW. Point back to return, forward to go again. Off the bag is vulnerable.",
            "Before charge: 1/2/3/4 + Space throws there. Any of the four bases, even empty ones.",
            "Hold Space: committed. Choose a base while held: BALK. Runners gain one; count unchanged.",
            "You can hold the pitch indefinitely. Let go to deliver it; the catcher can challenge the runner.",
            "In flight, 1/2/3/4 chooses the catcher target. Press Space just before the catch to buffer a throw; period cancels. Transfer comes before release.",
        ]),
        new("fielding", "Fielding", "fielding",
        [
            "A fly hangs; outfielder to landing. Shadow tracks ball. HR: wall.",
            "The throw is yours. Bag + RT; stick runs with the ball; catch, throw, or tag: out. Force: 2B with first; 3B with first + second; home loaded.",
            "After release, you are the glove at that bag. Runner on first: throw both to turn two.",
            "YOU names glove. Dead stick auto-runs; stick steers. Stand on ball to scoop. Start → Arrange defense swaps; next glove pulses. Pickup stays live.",
            "North jumps; circle turns red. East dives sideways; never automatic. West attacks.",
            "Camera is 45°; liner vs fly. CF is the top. A close play cuts to bag.",
        ],
        [
            "A fly hangs; outfielder to landing. Shadow tracks ball. HR: wall.",
            "The throw is yours. Bag + Space; WASD runs with the ball; catch, throw, or tag: out. Force: 2B with first; 3B with first + second; home loaded.",
            "After release, you are the glove at that bag. Runner on first: throw both to turn two.",
            "YOU names glove. Don't move: they auto-run; WASD steers. Stand on ball to scoop. R swaps; next glove pulses. Pickup stays live.",
            "F jumps; circle turns red. G dives sideways; never automatic. B attacks.",
            "Camera is 45°; liner vs fly. CF is the top. A close play cuts to bag.",
        ]),
        new("tutorial-baseball", "Counts, fouls and halves", "training",
        [
            "Build 1–1: walk left to the rubber's edge, release the stick, and tap RT for a ball. Next SET, stick down resets; RT throws the strike.",
            "Foul then fair: stay centered in SET and tap RT well early to pull it foul. Next pitch, time RT for fair contact.",
            "Finish the half: two outs, two strikes. A centered called strike changes sides and resets the outs.",
            "Each complete sequence earns one success. Three successes pass; failed attempts keep earlier successes.",
        ],
        [
            "Build 1–1: walk left to the rubber's edge, release the key, and tap Space for a ball. Next SET, S resets; Space throws the strike.",
            "Foul then fair: stay centered in SET and tap Space well early to pull it foul. Next pitch, time Space for fair contact.",
            "Finish the half: two outs, two strikes. A centered called strike changes sides and resets the outs.",
            "Each complete sequence earns one success. Three successes pass; failed attempts keep earlier successes.",
        ]),
        new("tutorial-recovery", "Recovery and reach", "training",
        [
            "Bobble: wait for the helper's error, then steer with the stick through the loose-ball scoop. Assistance cannot finish the pickup for you.",
            "Ability reach: move to the landing ring's edge, catch automatically beyond ordinary reach. Burrow scoops at its reach edge.",
        ],
        [
            "Bobble: wait for the helper's error, then steer with WASD through the loose-ball scoop. Assistance cannot finish the pickup for you.",
            "Ability reach: move to the landing ring's edge, then Space catches beyond ordinary reach. Burrow scoops at its reach edge.",
        ]),
        new("tutorial-field-plays", "Fielding scenario lessons", "training",
        [
            "Force home: bases loaded, collect, then Right stick Down + RT. With two outs, this force ends the half with no run.",
            "Rundown: Right stick Right + RT checks first. Once trapped, Right stick Up + RT throws ahead for the tag.",
            "Close third: send and dash on offense; throw to third on defense. Press South fresh when the close-play icon appears.",
        ],
        [
            "Force home: bases loaded, collect, then 4 and Space. With two outs, this force ends the half with no run.",
            "Rundown: 1 and Space checks first. Once trapped, 2 and Space throws ahead for the tag.",
            "Close third: send and dash on offense; throw to third on defense. Press Space fresh when the close-play icon appears.",
        ]),
        new("tutorial-scoring", "Third outs and runs", "training",
        [
            "Two outs: collect, then throw as the lead runner nears home. The crossing must happen before the out.",
            "Force lesson: Right stick Up + RT forces second. A third force out cancels the earlier run.",
            "Tag lesson: Right stick Left + RT throws to third. Press South at the close-play icon. A nonforce third tag keeps the earlier run.",
        ],
        [
            "Two outs: collect, then throw as the lead runner nears home. The crossing must happen before the out.",
            "Force lesson: 2 and Space forces second. A third force out cancels the earlier run.",
            "Tag lesson: 3 and Space throws to third. Press Space at the close-play icon. A nonforce third tag keeps the earlier run.",
        ]),
        new("tutorial-live-plays", "Live-ball lessons", "training",
        [
            "Return on a fly: send off third, then hold RB before the catch. Stay safely on third instead of tagging up.",
            "Round first: choose first, send toward second, then dash during the turn. An early dash on the straight does not count.",
            "Tag up: wait on third for the catch, then hold LB to send home. Double off: catch, then Right stick Up + RT returns to second.",
            "Triple play: catch the fly, then return to second and first before both early runners retouch.",
        ],
        [
            "Return on a fly: send off third, then hold period before the catch. Stay safely on third instead of tagging up.",
            "Round first: choose first, send toward second, then dash during the turn. An early dash on the straight does not count.",
            "Tag up: wait on third for the catch, then hold comma to send home. Double off: catch, then 2 and Space returns to second.",
            "Triple play: catch the fly, then return to second and first before both early runners retouch.",
        ]),
        new("tutorial-items", "Items and star grounders", "training",
        [
            "Item lessons only: after contact, D-pad left/right chooses; left stick aims; North throws.",
            "Each item lesson needs its named effect on the defender. Field a star grounder needs your controlled movement and pickup.",
        ],
        [
            "Item lesson: hit fair first. Tab cycles; WASD aims. E throws the selected item.",
            "Each item lesson needs its named effect on the defender. Field a star grounder needs your controlled movement and pickup.",
        ]),
        new("tutorial-sequences", "Steals and star lessons", "training",
        [
            "Steal: Right stick Right selects first; LB starts the runner now. Reach second safely.",
            "Delayed home steal: send first, then Right stick Left and stick down send third home as the catcher throw passes the mound.",
            "Star lessons: hold LT as you let go of RT. Star swings need fair contact.",
            "Earn and spend: ordinary third strike, then star pitch against the next batter. Three complete sequences pass.",
        ],
        [
            "Steal: 1 selects first; Z starts the runner now. Reach second safely.",
            "Delayed home steal: send first, then 3 and S send third home as the catcher throw passes the mound.",
            "Star lessons: hold Q as you let go of Space. Star swings need fair contact.",
            "Earn and spend: ordinary third strike, then star pitch against the next batter. Three complete sequences pass.",
        ]),
        new("relay-control", "Relay and queued throws", "fielding",
        [
            "Arm the destination with the Right stick. RB feeds the cutoff. The receiver waits for you: RT sends the next leg.",
            "A press just before the catch waits for the receiver, then sends it.",
            "Change the Right stick target to retarget a waiting throw. Changing the target does not extend the press's short lifetime.",
            "Tap East to cancel the waiting throw. A cancelled or expired press cannot release the ball.",
        ],
        [
            "Arm the destination with 1/2/3/4. X feeds the cutoff. The receiver waits for you: Space sends the next leg.",
            "A press just before the catch waits for the receiver, then sends it.",
            "Change the 1/2/3/4 target to retarget a waiting throw. Changing the target does not extend the press's short lifetime.",
            "Tap period to cancel the waiting throw. A cancelled or expired press cannot release the ball.",
        ]),
        new("exhibition", "Stadium and captains", "exhibition",
        [
            "Stadium: choose time and hazards, 1 vs CPU or 2 controllers, and P1 HOME / AWAY. Home bats the bottom.",
            "Choose captains: Left/right browses portraits; South confirms yours, then the CPU captain.",
            "Two controllers: each player confirms their own captain. Confirmed captains are reserved.",
            "East undoes confirmation, then returns to stadium setup. The park does not follow the captain.",
        ],
        [
            "Title is the park (dirt + diamond). GRAND SLUGGERS is a sticker over the infield, left to right. No captain on the title.",
            "Pick captain: Q chooses HOME or AWAY. HOME bats the bottom.",
            "Captains are the toys on the dirt. A/D your team. W/S the other. Camera looks at the toy, not the brim, not the dirt.",
            "Comma 1 PLAYER. Tab 2 PLAYERS. Two players needs controller 2.",
            "Pick stadium first: a postcard with a crowd and padded wall. The park does not follow the captain. A/D park; N night; R hazards; Space captains, then lineup.",
        ]),
        new("lineup", "Lineup", "lineup",
        [
            "Team Setup: South adds a player; RB fills your team. West removes a roster player.",
            "Both teams get the same stars when ON. Focus shows a card and highlights buddies.",
            "Two diamonds between batting bars. Stick moves; LB/RB switches order / field. South picks, then swaps. East cancels.",
            "North continues to settings when both players are ready.",
        ],
        [
            "Team Setup first, then Offense / Defense Setup.",
            "Click a pool player to add them. Both teams get the same stars when ON. Hover shows a card and softly highlights buddies.",
            "Two diamonds between batting bars. WASD moves; G switches order / field. Space or click picks, then swaps. F cancels.",
            "Q / Continue goes to settings when both players are ready.",
        ]),
        new("match-settings", "Match settings", "exhibition",
        [
            "After positions/order, Player 1 sets Stars, innings (3/6/9), mercy and CPU skill. Items are unavailable.",
            "Stick up/down selects a row; left/right or South changes it. Changing a rule clears both ready states.",
            "North readies your seat. Both human players must be ready to play; CPU is always ready. East withdraws readiness or returns to positions.",
            "Mercy never ends a 3-inning game. Stars OFF disables Star pitches, swings and Star gains for both teams.",
        ],
        [
            "After positions/order, Player 1 sets Stars, innings (3/6/9), mercy and CPU skill. Items are unavailable.",
            "W/S selects a row; A/D or Space changes it. Click a setting to change it. Changes clear both ready states.",
            "Q / Ready readies Player 1. Player 2 readies on their controller. F withdraws readiness or returns to positions.",
            "Mercy never ends a 3-inning game. Stars OFF disables Star pitches, swings and Star gains for both teams.",
        ]),
        new("two-pads", "Two controllers", "exhibition",
        [
            "Stadium setup: choose 2 controllers and P1 HOME / AWAY. The first controller is player 1; the second controller sits the other side.",
            "Each player uses a controller.",
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
            "South play ball. View this book. 2 PLAYERS seats controller 2; a seat drop pauses. Home bats bottom. Harbor.",
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
            "Cards: batter AB, pitcher ARM and TIRED. Start → Arrange defense swaps.",
            "Yellow circle: landing ring (stand-up). Red: jump window. YOU: the glove.",
            "ITEM names an error item.",
            "A stamp on the field: BALL, STRIKE, FOUL, WALK, BALK, outs, hits, steals, homers, ERROR; SET starts the next pitch.",
            "OUT / SCORE pop mid-play. Close play: bag cam.",
        ],
        [
            "Scorebug: inning, runs, stars; B / S / O.",
            "Cards: batter AB, pitcher ARM and TIRED. R swaps.",
            "Yellow circle: landing ring (stand-up). Red: jump window. YOU: the glove.",
            "ITEM names an error item.",
            "A stamp on the field: BALL, STRIKE, FOUL, WALK, BALK, outs, hits, steals, homers, ERROR; SET starts the next pitch.",
            "OUT / SCORE pop mid-play. Close play: bag cam.",
        ]),
        new("screen-live", "After contact", "exhibition",
        [
            "Live: runners and outs replace the score panel and player cards.",
            "Effects never hide runners or outs. Follow the runner pips while you play.",
            "YOU names your glove. Throw and item prompts stay with their actions.",
            "When play ends, the plate HUD returns immediately, ready for the next pitch.",
        ]),
        new("chemistry", "Chemistry", "chemistry",
        [
            "Hearts are buddies. Scribbles are rivals.",
            "Buddies throw faster, rivals slower. Buddy jump. Buddy toss.",
            "A buddy on deck can gift a banana after you hit.",
            "Both teams start with the same stars.",
        ]),
        new("stars", "Star skills", "stars",
        [
            "Up to 5 stars; both teams start even. A star costs 1 or more.",
            "Hold LT as you let go of RT    fire.",
            "Short of stars: the ordinary pitch or swing goes, nothing is spent, your stars flash red.",
            "A special breaks a baseball rule for about two seconds, then baseball resumes.",
            "Not a free home run. The ball or the field changes.",
        ],
        [
            "Up to 5 stars; both teams start even. A star costs 1 or more.",
            "Hold Q as you let go of Space / left click    fire.",
            "Short of stars: the ordinary pitch or swing goes, nothing is spent, your stars flash red.",
            "A special breaks a baseball rule for about two seconds, then baseball resumes.",
            "Not a free home run. The ball or the field changes.",
        ]),
        new("abilities", "Who you are", "abilities",
        [
            "Each toy has one field verb. Super Jump / Grow / Lick Catch add range.",
            "The card shows P / B / F / R, the star pitch, the star swing, and the field verb.",
            "TIRED pitcher? Start → Arrange defense.",
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
            "Aim with the stick. Throw with North.",
            "Banana    peel. Rocket    daze. POW    hop. West smashes a flying item.",
        ],
        [
            "A buddy on deck can give you a banana, rocket, or POW after contact.",
            "Aim with the mouse. Throw with E.",
            "Banana    peel. Rocket    daze. POW    hop. B smashes a flying item.",
        ]),
        new("pause-practice", "Pause and Practice", "pause-practice",
        [
            "Start    call time. View    this book from title and a pitch.",
            "South ok. East resume.",
            "Title → Tutorials    Tutorials. Left/right categories, up/down lessons across pages. Attempts repeat automatically until three successes.",
            "Stadium → Hazards    on / off.",
            "Field lessons    Steer to catch automatically; North jumps; Right stick + RT throws.",
            "Run lessons    Right stick selects; LB sends; RB returns; D-pad Up halts. South dashes.",
        ],
        [
            "H    call time. View    this book from title and a pitch.",
            "Space / left click ok. G / right click resume.",
            "Title F    Tutorials. A/D categories, W/S lessons across pages. Attempts repeat automatically until three successes.",
            "Stadium R    hazards on / off.",
            "Field lessons    WASD + Space catches; F jumps; 1/2/3/4 + Space throws to the named bag.",
            "Run lessons    1/2/3/4 selects; WASD sends/returns; direction + slash halts one. Space dashes.",
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
        ["South", "East", "West", "North", "Right stick", "LT", "RT", "LB", "RB", "L3", "Select", "Start", "Gamepad"];

    static readonly string[] KeyHardware =
        ["Space", "WASD", "left click", "right click", "Shift", "middle click", "Right-drag", "Tab", "Ctrl", "Enter"];

    /// <summary>True if a line names pad hardware and key/mouse hardware together.</summary>
    public static bool MixesHardware(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return false;
        var pad = PadHardware.Any(w => ContainsWord(line, w));
        var stripped = line.Replace("Dead stick", "", StringComparison.OrdinalIgnoreCase);
        if (stripped.Contains("stick", StringComparison.OrdinalIgnoreCase)) pad = true;
        var keys = KeyHardware.Any(w => ContainsWord(line, w));
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
