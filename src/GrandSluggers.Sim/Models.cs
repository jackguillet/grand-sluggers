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

public enum ContactQuality
{
    Miss,
    Cheap,
    Solid,
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
    IReadOnlyList<Hazard> Hazards);

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

public sealed record AtBatInput(
    Character Pitcher,
    Character Batter,
    Character? OnDeck,
    IReadOnlyList<Character> RunnersOn,
    string PitchType,
    bool ChargePitch,
    bool ChargeSwing,
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
    double PitchAimX = 0,
    double PitchAimY = 0);

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
    bool InZone = true);

public sealed record PitchCommand(
    string Type,
    double Charge01,
    double TimingErrorFrames,
    bool Star,
    double AimX = 0,
    double AimY = 0,
    double BreakX = 0,
    bool Changeup = false,
    double RubberX = 0);

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
    Strikeout,
    StolenBase,
    CaughtStealing
}

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
    int HomeScoreAfter);

public readonly record struct Sample(double T, double Dist, double Height);

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

    public void StartSteal(int targetBag = 0)
    {
        StealAttempt = true;
        StealTarget = targetBag is 2 or 3 ? targetBag : 0;
        Returning = false;
        Sliding = false;
        if (Lead01 < 0.2) Lead01 = 0.2;
    }

    public void CancelSteal()
    {
        StealAttempt = false;
        StealTarget = 0;
    }

    public void Slide() => Sliding = true;
}
