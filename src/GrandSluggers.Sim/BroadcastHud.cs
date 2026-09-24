using System.Globalization;

namespace GrandSluggers.Sim.Front;

/// <summary>
/// Plate information gives way to runners and outs during live play. Effects never mute play information.
/// Title, select, lineup, and final still draw.
/// Play HUD anchors are normalized 0–1, Y down (IMGUI). 1P and 2P share one layout (#325).
/// </summary>
public static class BroadcastHud
{
    public enum PlayMode { Plate, InPlay, Hidden }

    /// <summary>
    /// A synchronous handoff on the play phase, shared by Exhibition and practice, one seat and two.
    /// Hit-freeze, smash and special lifetimes are deliberately not inputs: the next playable frame
    /// must already have runners, outs and control prompts. Only explicit capture/debug mute hides it.
    /// </summary>
    public static PlayMode Mode(bool inPlay, bool forceMute = false) =>
        forceMute ? PlayMode.Hidden : inPlay ? PlayMode.InPlay : PlayMode.Plate;

    // Compact in-play readout: diamond above outs, at the scorebug's right edge.
    public static readonly HudRect LivePanel = new(0.870, 0.025, 0.115, 0.165);
    public static readonly HudRect LiveDiamond = new(0.879, 0.035, 0.096, 0.105);
    public static readonly HudRect LiveOuts = new(0.885, 0.145, 0.085, 0.035);

    /// <summary>AB card extras. Steal names L3 until it's on.</summary>
    /// <summary>
    /// The pitcher card's verb tells in SET (spec §4.1, §4.7; #582): the way the batter card
    /// shows BUNT and STEAL. STAR when armed, the swap pick while open.
    ///
    /// <para>
    /// The CHANGE tell is gone (PH-02-R5, #825). The family is selected before the charge and the
    /// card is shared by both seats, so a tell that named the held pitch handed the batter the
    /// selection. What the card shows instead is <see cref="PitcherPitches"/> — the whole
    /// repertoire, always, with nothing marked.
    /// </para>
    /// </summary>
    public static string PitcherExtra(bool star, string? swapTell = null, bool canSwap = false)
    {
        var s = "";
        if (star) s += "STAR  ";
        if (!string.IsNullOrEmpty(swapTell)) s += swapTell;
        else if (canSwap) s += "Start → Arrange defense";
        return s.Trim();
    }

    /// <summary>
    /// Two letters per family, for the pitcher card (PH-02-R5). One table: a short name is couch
    /// copy, so it lives here beside the other card rows rather than beside the ids in
    /// <see cref="PitchFamily"/>. An id with no short name prints its first two letters upper-cased
    /// rather than vanishing.
    /// </summary>
    public static string ShortFamily(string family) => family switch
    {
        PitchFamily.Fastball => "FB",
        PitchFamily.Changeup => "CH",
        PitchFamily.Curveball => "CU",
        PitchFamily.Slider => "SL",
        PitchFamily.Sinker => "SI",
        _ => string.IsNullOrWhiteSpace(family) ? "" : family.Trim()[..Math.Min(2, family.Trim().Length)].ToUpperInvariant()
    };

    /// <summary>
    /// The pitcher's ordinary pitches on the card, in repertoire order, as one short row —
    /// <c>FB · CH</c> today, <c>FB · CH · CU</c> for a three-family arm under a table that authors
    /// three (PH-02-R5).
    ///
    /// <para>
    /// <b>No mark, no cursor, no press count.</b> The row is a function of the pitcher and the
    /// active family table and of nothing else — not of the selection state — so the shared screen
    /// cannot leak which family is showing, and both seats read the same card. What it lists is what
    /// the RB / Tab cycle can actually reach: a slot the active table does not author is skipped by
    /// <see cref="PitchSelection.Advance"/>, so printing it would name a pitch no press can select.
    /// </para>
    /// </summary>
    public static string PitcherPitches(Repertoire repertoire, PitchFamilyTable families)
    {
        if (repertoire is null) throw new ArgumentNullException(nameof(repertoire));
        if (families is null) throw new ArgumentNullException(nameof(families));
        return string.Join("  ·  ", repertoire.Ordinary
            .Where(families.IsAuthored)
            .Select(ShortFamily));
    }

    /// <summary>The card row for the arm on the mound right now (§2, PH-15-R1).</summary>
    public static string PitcherPitches(Match match) =>
        match is null
            ? throw new ArgumentNullException(nameof(match))
            : PitcherPitches(match.Pitcher.Repertoire, match.Rules.Pitching.Families);

    public static string BatterExtra(bool star, bool stealOn, bool canSteal, bool bunt, string item) =>
        BatterExtra(star, stealOn, canSteal, bunt ? BuntSide.Third : BuntSide.None, item, sideKnown: false);

    /// <summary>
    /// The batter card's verb tells. A squared batter reads <c>BUNT 3B</c> or <c>BUNT 1B</c>: the held side
    /// (§5.8, PH-14-R3) is public, for both seats and the CPU, the same fact the bat angle carries.
    /// </summary>
    public static string BatterExtra(bool star, bool stealOn, bool canSteal, BuntSide bunt, string item) =>
        BatterExtra(star, stealOn, canSteal, bunt, item, sideKnown: true);

    /// <summary>The card word for a held bunt side: <c>BUNT 3B</c>, <c>BUNT 1B</c>, or empty when not squared.</summary>
    public static string BuntTell(BuntSide side) => side switch
    {
        BuntSide.Third => "BUNT 3B",
        BuntSide.First => "BUNT 1B",
        _ => ""
    };

    static string BatterExtra(bool star, bool stealOn, bool canSteal, BuntSide bunt, string item, bool sideKnown)
    {
        var s = "";
        if (star) s += "STAR  ";
        if (bunt != BuntSide.None) s += (sideKnown ? BuntTell(bunt) : "BUNT") + "  ";
        if (stealOn) s += "STEAL  ";
        else if (canSteal) s += "L3 STEAL  ";
        if (!string.IsNullOrEmpty(item)) s += item;
        return s.Trim();
    }

    /// <summary>
    /// The "special unavailable" tell (§12, PH-16-R12): a Star Pitch or Star Swing asked for at the release on a pool
    /// short of its price went out as the ordinary action. The team's Stars on the scorebug flash red and the line
    /// under the scorebug names it, on both seats' screen. Built only from the typed <see cref="StarRequest"/> the
    /// match records (<see cref="PlayOutcome.Stars"/>), never from a caption.
    /// </summary>
    public readonly record struct StarUnavailableTell(bool Home, StarAction Action)
    {
        /// <summary>The scorebug row whose Stars flash: away is row 0, home row 1.</summary>
        public int Row => Home ? 1 : 0;

        /// <summary>The line under the scorebug.</summary>
        public string Words => Action == StarAction.Pitch ? "NO STARS · STAR PITCH" : "NO STARS · STAR SWING";
    }

    /// <summary>How long the tell shows, in seconds of the unscaled clock.</summary>
    public const double StarUnavailableSeconds = 1.5;

    /// <summary>Red on / off flips per second while it shows.</summary>
    public const double StarUnavailableFlips = 8;

    /// <summary>The tell for one settled request: null when it was afforded (or nothing was asked).</summary>
    public static StarUnavailableTell? StarUnavailable(StarRequest? request) =>
        request is { Afforded: false } r ? new StarUnavailableTell(r.Home, r.Action) : null;

    /// <summary>The tell for a play's requests (<see cref="PlayOutcome.Stars"/>): the first one not afforded.</summary>
    public static StarUnavailableTell? StarUnavailable(IReadOnlyList<StarRequest> requests) =>
        StarUnavailable(requests?.FirstOrDefault(r => !r.Afforded));

    /// <summary>Whether the tell still shows <paramref name="ageSeconds"/> after it began.</summary>
    public static bool StarUnavailableShows(double ageSeconds) => ageSeconds >= 0 && ageSeconds < StarUnavailableSeconds;

    /// <summary>Whether the Stars are red on this frame: they start red and flip at <see cref="StarUnavailableFlips"/>.</summary>
    public static bool StarUnavailableRed(double ageSeconds) =>
        StarUnavailableShows(ageSeconds) && (int)Math.Floor(ageSeconds * StarUnavailableFlips) % 2 == 0;

    /// <summary>The tell's line: flush under the scorebug, as wide as it (<see cref="OnScorebug"/>).</summary>
    public static HudRect StarUnavailableLine(HudRect score) => new(score.X, score.Bottom, score.W, 0.040);

    /// <summary>Normalized rect. X/Y is top-left. Pixel() scales to a screen.</summary>
    public readonly record struct HudRect(double X, double Y, double W, double H)
    {
        public (double X, double Y, double W, double H) Pixel(double screenW, double screenH) =>
            (X * screenW, Y * screenH, W * screenW, H * screenH);

        public double Right => X + W;
        public double Bottom => Y + H;
    }

    public sealed record PlayLayout(
        HudRect Score,
        HudRect Count,
        HudRect MiniDiamond,
        HudRect BatterCard,
        HudRect PitcherCard,
        HudRect Banner);

    // Coaching must leave the ordinary score, bases and player cards readable.
    public static readonly HudRect StealInset = new(0.018, 0.255, 0.29, 0.29);
    public const string StealInsetTitle = "RUNNER RACE";
    public const string PitchCommitted = "COMMITTED · deliver the pitch";

    public static readonly HudRect TutorialCoach = new(0.02, 0.018, 0.65, 0.19);

    /// <summary>
    /// SMS information architecture: score top-right with S/B/O + diamond on the panel,
    /// batter card bottom-left, pitcher card bottom-right. Seat count must not move them.
    /// </summary>
    public static PlayLayout Layout(int seats = 1)
    {
        _ = seats;
        return Standard;
    }

    /// <summary>
    /// One recipe for 1P and 1v1. Rects keep ≥8px margin at 1280×800 and 1920×1080.
    /// Count and diamond sit on the score panel, not in the sky.
    /// </summary>
    public static readonly PlayLayout Standard = new(
        Score: new(0.695, 0.015, 0.290, 0.248),
        Count: new(0.780, 0.178, 0.190, 0.072),
        MiniDiamond: new(0.705, 0.178, 0.070, 0.072),
        BatterCard: new(0.012, 0.735, 0.260, 0.225),
        PitcherCard: new(0.728, 0.735, 0.260, 0.225),
        Banner: new(0.28, 0.018, 0.40, 0.10));

    /// <summary>In-play YOU tell. Same recipe 1P and 1v1.</summary>
    public static readonly HudRect YouTell = new(0.028125, 0.850, 0.21875, 0.045);

    /// <summary>In-play ITEM pointer. Opposite corner from YOU.</summary>
    public static readonly HudRect ItemTell = new(0.750, 0.850, 0.221875, 0.045);

    /// <summary>
    /// The pursuit stick's tell (#718): top centre under the banner, clear of the scorebug, the cards and the throw pips the
    /// client anchors to the bottom edge. Same recipe 1P and 1v1; the copy names the player when two play.
    /// </summary>
    public static readonly HudRect StickTell = new(0.330, 0.125, 0.340, 0.045);

    /// <summary>
    /// Live-event dirt stickers (§15, #690). Same recipe 1P and 1v1. None is the old
    /// screen-center card (0.50, 0.40): they sit on the dirt, the glove, or the plate.
    /// </summary>
    public static HudRect Stamp(StampAnchor anchor) => anchor switch
    {
        StampAnchor.Glove => StampGlove,
        StampAnchor.Bag => StampBag,
        StampAnchor.Plate => StampPlate,
        _ => StampDirt
    };

    public static readonly HudRect StampGlove = new(0.040, 0.580, 0.240, 0.120);
    public static readonly HudRect StampBag = new(0.380, 0.560, 0.240, 0.120);
    public static readonly HudRect StampPlate = new(0.380, 0.660, 0.240, 0.120);
    public static readonly HudRect StampDirt = new(0.380, 0.500, 0.240, 0.120);

    /// <summary>The retired full-screen card: 50% X, 40% Y. Live tells must not sit here.</summary>
    public static bool IsScreenCenterCard(HudRect r)
    {
        var cx = r.X + r.W * 0.5;
        var cy = r.Y + r.H * 0.5;
        return Math.Abs(cx - 0.50) < 0.05 && Math.Abs(cy - 0.40) < 0.08;
    }

    public const double FrameMarginPx = 8;

    public static bool InFrame(HudRect r, double screenW, double screenH, double marginPx = FrameMarginPx)
    {
        var (x, y, w, h) = r.Pixel(screenW, screenH);
        return w > 0 && h > 0
            && x >= marginPx
            && y >= marginPx
            && x + w <= screenW - marginPx
            && y + h <= screenH - marginPx;
    }

    public static bool Contains(HudRect outer, HudRect inner) =>
        inner.X >= outer.X - 1e-9
        && inner.Y >= outer.Y - 1e-9
        && inner.Right <= outer.Right + 1e-9
        && inner.Bottom <= outer.Bottom + 1e-9;

    /// <summary>S/B/O belongs on the scorebug: inside the panel, or flush under it.</summary>
    public static bool OnScorebug(HudRect score, HudRect child)
    {
        if (Contains(score, child)) return true;
        var flush = Math.Abs(child.Y - score.Bottom) < 0.002;
        var xOverlap = child.X < score.Right && child.Right > score.X;
        return flush && xOverlap;
    }

    public static HudRect InningMark(HudRect score) =>
        new(score.X + score.W * 0.04, score.Y + score.H * 0.035, score.W * 0.22, score.H * 0.145);

    public static HudRect InningBox(HudRect score, int inning, int innings)
    {
        innings = Math.Max(1, innings);
        inning = Math.Clamp(inning, 1, innings);
        var stripX = score.X + score.W * 0.28;
        var stripY = score.Y + score.H * 0.035;
        var stripW = score.W * 0.68;
        var stripH = score.H * 0.145;
        var slot = stripW / innings;
        var gap = slot * 0.12;
        return new(stripX + (inning - 1) * slot + gap * 0.5, stripY, slot - gap, stripH);
    }

    public static HudRect NameCol(HudRect score, int row)
    {
        var y = score.Y + score.H * (0.20 + row * 0.20);
        return new(score.X + score.W * 0.07, y, score.W * 0.39, score.H * 0.18);
    }

    public static HudRect RunsCol(HudRect score, int row)
    {
        var y = score.Y + score.H * (0.18 + row * 0.20);
        return new(score.X + score.W * 0.52, y, score.W * 0.14, score.H * 0.20);
    }

    public static HudRect StarsCol(HudRect score, int row)
    {
        var y = score.Y + score.H * (0.22 + row * 0.20);
        return new(score.X + score.W * 0.68, y, score.W * 0.28, score.H * 0.14);
    }

    public static HudRect StripeCol(HudRect score, int row)
    {
        var y = score.Y + score.H * (0.22 + row * 0.20);
        return new(score.X + score.W * 0.025, y, score.W * 0.028, score.H * 0.12);
    }

    /// <summary>Captain last name on the bug. Never glued to the run total.</summary>
    public static string BugName(string captainName)
    {
        if (string.IsNullOrWhiteSpace(captainName)) return "";
        var sp = captainName.LastIndexOf(' ');
        return (sp >= 0 ? captainName[(sp + 1)..] : captainName).ToUpperInvariant();
    }

    public static string RunsLabel(int runs) => runs.ToString(CultureInfo.InvariantCulture);

    /// <summary>Couch scorebug. Every field is readable without F2.</summary>
    public sealed record Scorebug(
        int Inning,
        int Innings,
        bool Top,
        bool Over,
        int AwayScore,
        int HomeScore,
        int Outs,
        int Balls,
        int Strikes,
        bool RunnerFirst,
        bool RunnerSecond,
        bool RunnerThird,
        int SelectedBag,
        string Pitcher,
        string Batter,
        string Next,
        int OffenseStars,
        int DefenseStars,
        string AwayName,
        string HomeName,
        IReadOnlyList<RunnerPip> Runners,
        bool StarsEnabled);

    /// <summary>
    /// One live runner on the mini diamond (spec §15, #606): who, the bag they started this play on (0 is the
    /// batter-runner; the pad names runners by it), and where they stand now as a segment and a fraction
    /// (<see cref="Runner.Pip"/>). A seated runner is fraction 0 on their bag.
    /// </summary>
    public readonly record struct RunnerPip(string Id, int FromBag, int From, int To, double U)
    {
        public bool Batter => FromBag == 0;
    }

    /// <summary>Every live body on the basepaths this frame, the batter-runner included; out and scored runners are gone.</summary>
    public static IReadOnlyList<RunnerPip> RunnerPips(Match match)
    {
        var pips = new List<RunnerPip>();
        foreach (var r in match.Runners)
        {
            if (!r.Live) continue;
            var (from, to, u) = r.Pip;
            pips.Add(new RunnerPip(r.Who.Id, r.FromBag, from, to, u));
        }
        return pips;
    }

    public static Scorebug From(Match match)
    {
        if (match == null) throw new ArgumentNullException(nameof(match));
        return new Scorebug(
            match.Inning,
            match.Innings,
            match.Top,
            match.Over,
            match.AwayScore,
            match.HomeScore,
            match.Outs,
            match.Balls,
            match.Strikes,
            match.Occupied(1),
            match.Occupied(2),
            match.Occupied(3),
            match.SelectedBag,
            match.Pitcher.Name,
            match.Batter.Name,
            match.OnDeck?.Name ?? "",
            (int)Math.Floor(match.OffenseStars),
            (int)Math.Floor(match.DefenseStars),
            match.Away.Name,
            match.Home.Name,
            RunnerPips(match),
            match.StarsEnabled);
    }

    /// <summary>Booklet Game Rules spread. Copy a stranger can read without F2.</summary>
    public const int TiredArm = 25;

    public static bool PoorArm(int stamina) => stamina < TiredArm;

    /// <summary>TIRED below the rule's threshold (pitching.stamina.tiredBelow).</summary>
    public static bool PoorArm(int stamina, RulesTable rules) => stamina < rules.Pitching.Stamina.TiredBelow;

    public static string ArmLine(int stamina) =>
        PoorArm(stamina) ? $"ARM  {stamina}  ·  TIRED" : $"ARM  {stamina}";

    public static string ArmLine(int stamina, RulesTable rules) =>
        PoorArm(stamina, rules) ? $"ARM  {stamina}  ·  TIRED" : $"ARM  {stamina}";

    public static string ControlDisplay(bool hasGlove, string pos, string name, bool jump = false, bool dive = false)
    {
        if (!hasGlove || string.IsNullOrWhiteSpace(pos)) return "";
        var s = string.IsNullOrWhiteSpace(name)
            ? "YOU  " + pos
            : "YOU  " + pos + "  ·  " + name;
        if (jump) s += "  JUMP";
        if (dive) s += "  DIVE";
        return s;
    }

    /// <summary>
    /// What a seat is told about its pursuit stick (#718, F693-02-pursuit-calibration-policy, -arming): let go while the seat has
    /// no profile or Call time is taking one, and that it took. Empty when there is nothing to say. Numbered only when two play.
    /// </summary>
    public static string StickLine(PursuitReadiness.Tell tell, int seat, bool twoPlayers)
    {
        var who = twoPlayers ? $"P{seat + 1}  ·  " : "";
        return tell switch
        {
            PursuitReadiness.Tell.LetGo => who + "LET GO OF THE STICK",
            PursuitReadiness.Tell.Reset => who + "STICK RESET",
            _ => ""
        };
    }

    /// <summary>
    /// A live ball the fielding seat cannot steer yet (#718, <see cref="LivePlaySystem.PursuitUnready"/>): the glove runs on its
    /// own until the stick has been seen at rest once since the seat took the field.
    /// </summary>
    public const string UnreadyTell = "LET GO OF THE STICK TO STEER";

    /// <summary>Smart switch tell. Empty when the hint is you, or you have the ball.</summary>
    public static string SwitchTell(string current, string hint, string? hintName, bool hasBall)
    {
        if (hasBall || string.IsNullOrWhiteSpace(hint) || hint == current) return "";
        return string.IsNullOrWhiteSpace(hintName)
            ? "R  →  " + hint
            : "R  →  " + hint + "  ·  " + hintName;
    }

    public static string ItemPointer(bool offered, string? targetName) =>
        offered && !string.IsNullOrWhiteSpace(targetName)
            ? "ITEM  →  " + targetName
            : "";
}
