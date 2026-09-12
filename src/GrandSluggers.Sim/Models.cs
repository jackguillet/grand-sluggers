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

public sealed record Stats(int Pitch, int Bat, int Field, int Run)
{
    public Stats Clamp() => new(
        Math.Clamp(Pitch, 1, 10),
        Math.Clamp(Bat, 1, 10),
        Math.Clamp(Field, 1, 10),
        Math.Clamp(Run, 1, 10));
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
    string Bio);

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
    double FenceHeightFt = 8)
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

public sealed record Team(
    string Name,
    Character Captain,
    IReadOnlyList<Character> Roster,
    IReadOnlyList<Character>? Order = null,
    Character? Starter = null)
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
/// <paramref name="TimingErrorFrames"/> is bat-plane time minus ball-plate time at 60 Hz.
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
    double CrossingY = PitchFlight.PlateY);

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

public sealed record PitchCommand(
    string Type,
    double Charge01,
    double TimingErrorFrames,
    bool Star,
    double AimX = 0,
    double AimY = 0,
    double BreakX = 0,
    bool Changeup = false,
    double RubberX = 0,
    bool DeliveryPrepared = false);

public sealed record SwingCommand(
    bool Swing,
    double Charge01,
    double TimingErrorFrames,
    bool Star,
    double SprayAimDeg = 0,
    bool Bunt = false,
    double LaunchAim = 0,
    double BoxOffsetX = 0);

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
    CaughtStealing
}

public enum DefensiveFeat
{
    None,
    BuddyJump,
    SuperJump,
    Clamber
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
    bool GroundRuleDouble = false)
{
    public static PlayOutcome Empty { get; } = new();

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

public sealed record ThrowResult(
    Chemistry Relation,
    double SpeedMul,
    bool Error,
    double LateralFt = 0);

/// <summary>Live lead / steal / return on one occupied bag. 0 lead is glued to the bag.</summary>
public sealed class RunnerState
{
    public Character Who { get; }
    public double Lead01 { get; private set; }
    public bool StealAttempt { get; private set; }
    /// <summary>Named next bag while a steal is armed. 0 none; never 4 (no steal home).</summary>
    public int StealTarget { get; private set; }
    public bool Returning { get; private set; }
    public bool Sliding { get; private set; }

    public RunnerState(Character who) => Who = who;

    public void TakeLead(double delta = 0.25)
    {
        Returning = false;
        Sliding = false;
        Lead01 = Math.Clamp(Lead01 + delta, 0, 1);
    }

    public void ReturnToBag(double delta = 0.25)
    {
        Returning = true;
        StealAttempt = false;
        StealTarget = 0;
        Sliding = false;
        Lead01 = Math.Clamp(Lead01 - Math.Abs(delta), 0, 1);
        if (Lead01 <= 0) Returning = false;
    }

    /// <param name="armedLeadMin">running.steal.armedLeadMin: an armed runner has at least this lead.</param>
    public void StartSteal(int targetBag = 0, double? armedLeadMin = null)
    {
        StealAttempt = true;
        StealTarget = targetBag is 2 or 3 ? targetBag : 0;
        Returning = false;
        Sliding = false;
        var min = armedLeadMin ?? Rules.Default.Running.Steal.ArmedLeadMin;
        if (Lead01 < min) Lead01 = min;
    }

    public void CancelSteal()
    {
        StealAttempt = false;
        StealTarget = 0;
    }

    public void Slide() => Sliding = true;

    /// <summary>Hold this lead. Does not walk back; cancels a steal.</summary>
    public void Halt()
    {
        Returning = false;
        StealAttempt = false;
        StealTarget = 0;
        Sliding = false;
    }
}
