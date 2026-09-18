using System.Text.Json.Serialization;
namespace GrandSluggers.Sim;

public enum Hand
{
    L,
    R
}

public enum Chemistry
{
    Bad = 0,
    Neutral = 1,
    Good = 2
}

/// <summary>
/// Where the ball met the bat (spec §5.2, D4): the cursor zone decides quality. Sour is the
/// rim of the bat, Nice the oval, Perfect its heart. Miss is off the bat or outside the window.
/// </summary>
public enum ContactQuality
{
    Miss,
    Sour,
    Nice,
    Perfect
}

/// <summary>
/// A character's ratings. <see cref="Field"/> is the displayed defensive number; <see cref="Arm"/> and
/// <see cref="Hands"/> are the explicit traits behind it (F693-02-defensive-trait-mapping). Both are
/// <b>seeded from Field</b> until a character authors its own, so a roster that names neither behaves
/// exactly as it did before the split.
/// </summary>
public sealed record Stats(int Pitch, int Bat, int Field, int Run)
{
    readonly int _arm;
    readonly int _hands;

    /// <summary>Throwing: speed and accuracy. Unauthored (0) tracks <see cref="Field"/>.</summary>
    public int Arm
    {
        get => _arm > 0 ? _arm : Field;
        init => _arm = value;
    }

    /// <summary>Handling: securing the ball and recovering from it. Unauthored (0) tracks <see cref="Field"/>.</summary>
    public int Hands
    {
        get => _hands > 0 ? _hands : Field;
        init => _hands = value;
    }

    /// <summary>
    /// True when this rating was authored rather than seeded from <see cref="Field"/>.
    ///
    /// Not serialized. It is bookkeeping about where the number came from, not a rating, and a
    /// play trace records what happened rather than how the roster was written. Emitting it also
    /// made <see cref="Stats"/> unstable across a JSON round trip: the serialized <c>arm</c> reloads
    /// through the init setter, which marks the trait authored, so a replayed trace no longer
    /// matched the live one it replayed.
    /// </summary>
    [JsonIgnore] public bool ArmAuthored => _arm > 0;

    /// <inheritdoc cref="ArmAuthored"/>
    [JsonIgnore] public bool HandsAuthored => _hands > 0;

    // An unauthored trait stays unauthored through a clamp, so it keeps tracking the clamped Field.
    public Stats Clamp() => new(
        Math.Clamp(Pitch, 1, 10),
        Math.Clamp(Bat, 1, 10),
        Math.Clamp(Field, 1, 10),
        Math.Clamp(Run, 1, 10))
    {
        Arm = _arm > 0 ? Math.Clamp(_arm, 1, 10) : 0,
        Hands = _hands > 0 ? Math.Clamp(_hands, 1, 10) : 0
    };
}

public sealed record Character(
    string Id,
    string Name,
    string Faction,
    bool Captain,
    Stats Stats,
    Hand Bats,
    Hand Throws,
    string StarPitch,
    string StarSwing,
    string FieldAbility,
    string Bio,
    /// <summary>
    /// Authored stand-up catch reach in feet (F693-02-catch-reach-envelope). Null keeps the legacy
    /// <c>radiusBaseFt + radiusPerField x Field</c>, so an unauthored roster reaches exactly as far as it did.
    /// </summary>
    double? ReachFt = null);

public sealed record Park(
    string Id,
    string Name,
    string Faction,
    string Surface,
    int LeftFenceFt,
    int CenterFenceFt,
    int RightFenceFt,
    double WindMph,
    IReadOnlyList<Hazard> Hazards,
    double WindDeg = 0,
    double FenceHeightFt = 8,
    double NightContactWindowMul = 1.0)
{
    /// <summary>Where the wind blows toward, in the field frame: 0 out to CF, 90 toward the right-field line, 180 in at the plate.</summary>
    public (double X, double Z) WindDirection
    {
        get
        {
            var rad = WindDeg * Math.PI / 180.0;
            return (Math.Sin(rad), Math.Cos(rad));
        }
    }
}

public sealed record Hazard(string Type, double X, double Z, double Radius, string? Tag);

public sealed record BatItem(
    string Id,
    string Name,
    int ContactMod,
    int PowerMod,
    bool ChargeAlwaysFull,
    string Visual = "bat-wood");

public sealed record GloveItem(
    string Id,
    string Name,
    double ErrorReduction,
    int ArmMod,
    string Visual = "glove-brown");

/// <summary>
/// A nine. <paramref name="Gloves"/> is the lineup's glove diamond from Offense / Defense Setup
/// (position → player, §8.1); null means the roster order stands in for it (the preset teams).
/// </summary>
public sealed record Team(
    string Name,
    Character Captain,
    IReadOnlyList<Character> Roster,
    IReadOnlyList<Character>? Order = null,
    Character? Starter = null,
    IReadOnlyDictionary<string, Character>? Gloves = null)
{
    public IEnumerable<Character> Everyone => Roster;

    public Character Pitcher => Starter ?? Captain;

    public IReadOnlyList<Character> BattingOrder =>
        Order is { Count: > 0 } o ? o : DefaultBattingOrder(Captain, Roster);

    public static IReadOnlyList<Character> DefaultBattingOrder(Character captain, IReadOnlyList<Character> roster)
    {
        var rest = roster.Where(c => !c.Id.Equals(captain.Id, StringComparison.OrdinalIgnoreCase)).ToList();
        var order = new List<Character>(roster.Count);
        order.AddRange(rest.Take(3));
        order.Add(captain);
        order.AddRange(rest.Skip(3));
        return order;
    }
}

/// <summary>
/// One swing at one crossing. <paramref name="CrossingX"/> / <paramref name="CrossingY"/> are the
/// pitch at the plate plane in world feet (the same point the umpire and the aim tell read);
/// <paramref name="TimingErrorFrames"/> is the press minus the square press (the ball's plate time
/// less <c>batting.window.leadSec</c>) at 60 Hz, D13. <paramref name="HumanWindowMul"/> is the
/// difficulty rung's widening of a human batter's window (1 for the CPU).
/// </summary>
public sealed record AtBatInput(
    Character Pitcher,
    Character Batter,
    Character? OnDeck,
    IReadOnlyList<Character> RunnersOn,
    bool ChargePitch,
    bool ChangeupPitch,
    double TimingErrorFrames,
    bool UseStarPitch,
    bool UseStarSwing,
    BatItem? Bat,
    int PitcherStamina,
    double SprayAimDeg = 0,
    bool PitchInZone = true,
    bool Bunt = false,
    double LaunchAim = 0,
    double Charge01 = 0,
    double BoxOffsetX = 0,
    double CrossingX = 0,
    double CrossingY = PitchFlight.PlateY,
    double HumanWindowMul = 1);

public sealed record AtBatResult(
    ContactQuality Quality,
    bool InPlay,
    bool Strike,
    double ExitVeloMph,
    double LaunchDeg,
    double CarryFt,
    bool HomeRun,
    bool ChemistryItemOffered,
    string? StarPitchUsed,
    string? StarSwingUsed,
    double SprayDeg = 0,
    bool Foul = false,
    bool InZone = true,
    /// <summary>The batted ball's shape from the one flight (§6.2: topper … homer, bunt). <see cref="Foul"/> is the chalk.</summary>
    BattedBallClass Class = BattedBallClass.Fly);

/// <summary>
/// One pitch (spec §4.1 – §4.3). <paramref name="Type"/> is the shape id ("fastball" / "changeup";
/// <paramref name="Changeup"/> says the same for a modifier). Location is the rubber walk
/// (<paramref name="RubberX"/>, world feet per <see cref="HomeSet.PitcherWalk"/>) and the stick
/// after release (<paramref name="BreakX"/>, −1..1, capped at half a zone); <paramref name="AimX"/> /
/// <paramref name="AimY"/> are the CPU's plate-aim target and the tired wobble. <paramref name="Nice"/>
/// is a release inside the Nice band of MAX (+5% mph).
/// </summary>
public sealed record PitchCommand(
    string Type,
    double Charge01,
    bool Star,
    double AimX = 0,
    double AimY = 0,
    double BreakX = 0,
    bool Changeup = false,
    double RubberX = 0,
    bool DeliveryPrepared = false,
    bool Nice = false,
    double BreakMul = 1)
{
    public bool IsChangeup => Changeup || Type == "changeup";
}

public sealed record SwingCommand(
    bool Swing,
    double Charge01,
    double TimingErrorFrames,
    bool Star,
    double SprayAimDeg = 0,
    bool Bunt = false,
    double LaunchAim = 0,
    double BoxOffsetX = 0,
    /// <summary>A seat's pad pressed it: the rung's <see cref="CpuLevelRules.HumanWindowMul"/> applies.</summary>
    bool Human = false,
    /// <summary>
    /// The square clock at the plate time (§5.8, §7.3): play seconds the batter had been squared (West held), wound back
    /// down if they let go; 0 is no square. The defense reads the square as a tell before the pitch: the corners crash and
    /// the middle covers for this long (<see cref="BuntDefense.Spots"/>). It is independent of <see cref="Bunt"/>: a
    /// square pulled back into a swing still left the corners in, and a late press with no square is a bunt nobody read.
    /// </summary>
    double SquareSec = 0);

public enum PlayKind
{
    TakeBall,
    TakeStrike,
    SwingMiss,
    Foul,
    GroundOut,
    FlyOut,
    Single,
    Double,
    Triple,
    HomeRun,
    Walk,
    HitByPitch,
    Strikeout,
    StolenBase,
    CaughtStealing,
    /// <summary>A pickoff throw with every runner on their bag (§4.5, D3): a beat, no play, no count, no stamp.</summary>
    Pickoff,
    /// <summary>The ball is live and nobody has decided it yet: Complete names the play from the bodies (§10.6). Never stamped.</summary>
    InPlay
}

/// <summary>The catch feat on the typed outcome (§8.4): what the glove did to make the catch. The stamp reads it (§15).</summary>
public enum DefensiveFeat
{
    None,
    BuddyJump,
    SuperJump,
    Clamber,
    /// <summary>A plain jump catch in the window (West), not a wall rob.</summary>
    Jump,
    /// <summary>A dive catch or a dive scoop (East).</summary>
    Dive
}

public enum RunnerPlayResult
{
    None,
    StolenBase,
    CaughtStealing,
    PickedOff
}

public enum ThrowOrigin
{
    None,
    Catcher,
    PitcherRubber
}

/// <summary>The baseball endpoints of a resolved throw, independent of its presentation copy.</summary>
public sealed record ThrowEndpoint(ThrowOrigin Origin, int DestinationBag);

/// <summary>The five ways (spec §10.1). Caught stealing, picked off, and doubled off are tags or forces.</summary>
public enum OutType
{
    Strikeout,
    Catch,
    Force,
    Tag,
    ThrowOutAtFirst
}

/// <summary>
/// One out on the play: who, how, where. <paramref name="FromBag"/> 0 is the batter-runner.
/// <paramref name="Bag"/> is where it was made: 1..4, or 0 at the plate / in the field.
/// </summary>
public sealed record OutRecord(OutType Type, int Bag, int FromBag, Character Runner, Character? Fielder);

/// <summary>A runner's placement on the play. <paramref name="FromBag"/> 0 is the batter; <paramref name="ToBag"/> 4 scored.</summary>
public sealed record RunnerMove(Character Runner, int FromBag, int ToBag);

/// <summary>
/// A body on the field where it stood when the play died (§10.6, #574). <paramref name="Pos"/> is the
/// glove ("SS", "CF" …) or <see cref="Runner"/> for a live runner. The result beat is drawn from these;
/// nothing re-places a body from the position table until the next SET.
/// </summary>
public sealed record FieldBody(string Pos, Character Who, double X, double Z)
{
    public const string Runner = "runner";
    public bool IsRunner => Pos == Runner;
}

/// <summary>
/// Typed facts from a resolved play. Presentation, highlights, and the scenario harness read
/// these; the caption is produced from them last and is never read back (spec §15).
/// </summary>
public sealed record PlayOutcome(
    DefensiveFeat DefensiveFeat = DefensiveFeat.None,
    RunnerPlayResult RunnerResult = RunnerPlayResult.None,
    int RunnerFromBag = 0,
    int RunnerToBag = 0,
    ThrowEndpoint? ThrowEndpoint = null,
    IReadOnlyList<OutRecord>? Outs = null,
    IReadOnlyList<RunnerMove>? Advances = null,
    int BatterToBag = 0,
    bool Error = false,
    bool FieldersChoice = false,
    bool GroundRuleDouble = false,
    IReadOnlyList<FieldBody>? Bodies = null)
{
    public static PlayOutcome Empty { get; } = new();

    /// <summary>Every body on the field at Time, where it stood (§10.6, #574): gloves and live runners.</summary>
    public IReadOnlyList<FieldBody> BodiesAtTime => Bodies ?? [];

    /// <summary>Every out on the play, in the order it was made.</summary>
    public IReadOnlyList<OutRecord> OutsMade => Outs ?? [];

    /// <summary>Every runner who changed bags, including the batter-runner (from bag 0).</summary>
    public IReadOnlyList<RunnerMove> Moves => Advances ?? [];
}

/// <summary>The actors and match state at the start of one pitch or pickoff play.</summary>
public sealed record PlayContext(
    string BatterId,
    string PitcherId,
    int Inning,
    bool Top,
    int OutsBefore,
    int BallsBefore,
    int StrikesBefore,
    int AwayScoreBefore,
    int HomeScoreBefore,
    string? FirstRunnerId,
    string? SecondRunnerId,
    string? ThirdRunnerId);

/// <summary>The match state after the completed event and any lineup, half, or game transition.</summary>
public sealed record MatchState(
    string BatterId,
    string PitcherId,
    int Inning,
    bool Top,
    int Outs,
    int Balls,
    int Strikes,
    int AwayScore,
    int HomeScore,
    bool Over,
    string? FirstRunnerId,
    string? SecondRunnerId,
    string? ThirdRunnerId);

public sealed record PlayEvent(
    PlayKind Kind,
    AtBatResult AtBat,
    PitchCommand Pitch,
    SwingCommand Swing,
    Character Batter,
    Character Pitcher,
    Character? Fielder,
    ThrowResult? Throw,
    int RunsScored,
    IReadOnlyList<string> Scorers,
    string Caption,
    bool Heatball,
    bool Furnace,
    double HangTimeSec,
    double LandingX,
    double LandingZ,
    int OutsAfter,
    int AwayScoreAfter,
    int HomeScoreAfter,
    int OutsOnPlay = 0,
    PlayContext? Context = null,
    MatchState? NextState = null,
    PlayOutcome? Outcome = null);

/// <summary>What happened to the ball at this sample of the clipped path (spec §6.1).</summary>
public enum SampleEvent
{
    None,
    /// <summary>Ground contact (first grass, a hop, or the roll).</summary>
    Ground,
    /// <summary>Met the outfield fence below fence height: the carom starts here.</summary>
    Wall,
    /// <summary>Crossed the outfield fence above fence height between the poles: gone.</summary>
    Fence,
    /// <summary>Left the field over a foul wall or the backstop: dead in the stands.</summary>
    Stands,
    /// <summary>Touched a foul wall or the backstop below its top: foul, dead; the carom is for the camera.</summary>
    FoulWall
}

/// <summary>
/// One sample of the ball's clipped path. <see cref="T"/> is play seconds — the same clock
/// <see cref="LivePlaySystem.ElapsedSeconds"/> runs gloves and runners on (spec §6.1, one clock).
/// <see cref="Dist"/> is the horizontal distance from the plate; <see cref="X"/> / <see cref="Z"/> are the
/// field position (the wind bends the path, so the spray is not constant along it).
/// </summary>
public readonly record struct Sample(double T, double Dist, double Height, double X = 0, double Z = 0, SampleEvent Event = SampleEvent.None);

/// <summary>
/// One throw's input (§8.5): the pair's chemistry, the speed multiplier (arm × chemistry × ability)
/// the one throw clock flies it on, whether bad chemistry slanted it, the signed lateral miss in
/// feet at the target, and the thrower's Arm rating, which sets the comfortable range the long-throw
/// loss is measured from (#722). Whether it is caught is the receiver's radius, decided where it lands.
/// </summary>
public sealed record ThrowResult(
    Chemistry Relation,
    double SpeedMul,
    bool Slanted,
    double LateralFt = 0,
    int Arm = InPlay.NeutralArm,
    /// <summary>A release for this throw other than the table's (#723): Snap Throw's after a clean received teammate throw. Null is the ordinary release.</summary>
    double? ReleaseSec = null);
