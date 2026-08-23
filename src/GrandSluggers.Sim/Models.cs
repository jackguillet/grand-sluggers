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
    bool ChargeAlwaysFull);

public sealed record GloveItem(
    string Id,
    string Name,
    double ErrorReduction,
    int ArmMod);

public sealed record Team(
    string Name,
    Character Captain,
    IReadOnlyList<Character> Roster)
{
    public IEnumerable<Character> Everyone => Roster;
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
    bool Bunt = false);

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
    double AimY = 0);

public sealed record SwingCommand(
    bool Swing,
    double Charge01,
    double TimingErrorFrames,
    bool Star,
    double SprayAimDeg = 0,
    bool Bunt = false);

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
