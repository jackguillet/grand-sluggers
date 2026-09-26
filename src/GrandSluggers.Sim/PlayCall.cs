namespace GrandSluggers.Sim;

/// <summary>
/// What a play's call says, as typed facts in the order it says them (spec §12). The match raises these; the one
/// narrator, <see cref="PlayNarrator.Narrate(PlayCall)"/>, turns them into the caption. No rule reads the text.
/// </summary>
public enum CallBeat
{
    /// <summary>"Strike N." — <see cref="CallPart.Number"/> is the count's strikes after the pitch.</summary>
    StrikeSwinging,
    /// <summary>"Strike N looking."</summary>
    StrikeLooking,
    /// <summary>"Ball N."</summary>
    Ball,
    StruckOutSwinging,
    StruckOutLooking,
    /// <summary>A two-strike bunt rolled foul (§5.8).</summary>
    StruckOutBuntFoul,
    Walk,
    HitByPitch,
    /// <summary>The pitcher threw over (§4.5); the runner play that follows extends the call.</summary>
    Pickoff,
    Balk,
    /// <summary>A steal settled before the pitch (§11).</summary>
    StolenBase,
    Foul,
    /// <summary>Over the fence in the air. <see cref="CallPart.Word"/> names the Star Swing that did it, when it calls itself.</summary>
    HomeRun,
    /// <summary>Around the bases with the ball in play.</summary>
    InsideTheParkHomeRun,
    GroundRuleDouble,
    Triple,
    Double,
    Single,
    /// <summary>A single that went through a redirect; <see cref="CallPart.Word"/> is the hazard type.</summary>
    RedirectSingle,
    /// <summary>A single the Heatball dropped in.</summary>
    HeatballSingle,
    /// <summary>The live ball's last decision, <see cref="CallPart.Moment"/>.</summary>
    Live,
    BatterInAtFirst,
    TriplePlay,
    DoublePlay,
    BuddyJump,
    Clamber,
    PutAway,
    ToFirst,
    SacFly,
    FieldersChoice,
    /// <summary>The ball hit a reward target (F4-c).</summary>
    Billboard,
    /// <summary>An item landed; <see cref="CallPart.Word"/> is its id.</summary>
    Item,
    CaughtStealing,
    PickedOff,
    /// <summary>A runner took a bag; <see cref="CallPart.Number"/> is the bag.</summary>
    Steals,
    BackToBag,
}

/// <summary>
/// One fact of a call. <see cref="Who"/> is the name it is about (the batter, the fielder, the runner), <see cref="Other"/>
/// the second name a beat needs (the buddy; the fielder a live decision names by default). <see cref="Aside"/> starts a
/// new clause group, such as the runner play that followed a pitch.
/// </summary>
public sealed record CallPart(
    CallBeat Beat,
    string? Who = null,
    string? Other = null,
    int Number = 0,
    string? Word = null,
    LiveMoment? Moment = null,
    bool Aside = false);

/// <summary>A play's call: its facts, in order.</summary>
public sealed record PlayCall(IReadOnlyList<CallPart> Parts)
{
    public static PlayCall Of(params CallPart[] parts) => new(parts);

    /// <summary>This call with more facts after it.</summary>
    public PlayCall Then(IEnumerable<CallPart> more) => new(Parts.Concat(more).ToList());
}
