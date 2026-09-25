namespace GrandSluggers.Sim;

/// <summary>
/// The verb vocabulary the directors speak and the clocks the sim keeps for
/// it. No pose lives here: every verb plays a Blender take from
/// <c>data/art/clips.json</c>, and a left-handed batter or thrower plays the
/// take's baked mirror. Contract: docs/character-motion.md.
/// </summary>
public static class Motion
{
    public enum Verb
    {
        Idle, Field, Cheer, Charm,
        Walk, Run, Jump, Clamber,
        ChargePitch, ThrowPitch, Throw,
        ChargeSwing, Swing, CheckSwing, Bunt, Miss, LetGo,
        Catch, Dive, DiveAir, Crouch, StealLead, Spin,
        Scoop, Slide,
        // The steal race (#966): the catcher's receive-to-release, the sweep tag, the head-first slide, the runner's reversal.
        CatcherThrow, Tag, SlideHeadFirst, TurnBack
    }

    public enum ClipEvent { Contact, Release, FootPlant }

    /// <summary>What advances the sample time: match time, the verb's own clock, or a held charge.</summary>
    public enum Clock { World, Verb, Charge }

    public const double RunHz = 2.55;
    public const double RunDur = 1 / RunHz;
    public const double JumpDur = 0.55;
    public const double JumpPeak = 4.2;
    /// <summary>The swing takes' follow-through key: the bat around, still moving (docs/research-batting.md).</summary>
    public const double SwingDur = 0.50;
    /// <summary>
    /// The swing takes' last key, the held finish (#613, #583): weight on the front foot, the bat
    /// around. The take ends here and the batter holds it until the play moves on.
    /// </summary>
    public const double SwingFinish = 0.60;
    /// <summary>The slap swing take (#613): no windup, quick, compact.</summary>
    public const string SwingSlapClip = "swing-slap";
    /// <summary>The charge swing take (#613): the hold shows its windup, then a bigger arc.</summary>
    public const string SwingChargeClip = "swing-charge";
    public const double PitchDur = 0.70;
    public const double SwingContact = 0.30;
    public const double PitchRelease = 0.42;
    public const double ThrowRelease = 0.18;
    public const double ScoopContact = 0.22;
    public const double SlidePlant = 0.18;
    public const double HoldDur = 0.20;
    /// <summary>
    /// The dive in flight: laid out off the dirt while the lunge carries the body (<c>DiveT</c>). The sim lifts no diver, so
    /// the take bakes its own rise; the body lands in the ground <c>dive</c> for the recovery and the knockback.
    /// </summary>
    public const string DiveAirClip = "dive-air";
    /// <summary>The squared bunt toward the batter's pull side and toward the other field (PH-14-R3): the barrel shows the held side.</summary>
    public const string BuntPullClip = "bunt-pull";
    public const string BuntPushClip = "bunt-push";
    /// <summary>
    /// The let-go (PH-13-R1): a cancelled swing load walks back from the full coil to the stance by
    /// <see cref="LetGoReturnAt"/>, then settles until <see cref="LetGoDur"/> (data/art/baseball-takes.json <c>letGo</c>).
    /// </summary>
    public const string LetGoClip = "swing-letgo";
    public const double LetGoDur = 0.36;
    public const double LetGoReturnAt = 0.20;
    /// <summary>The catcher's take (#966): receive in the crouch, transfer, rise and plant, release here, follow through.</summary>
    public const double CatcherThrowDur = 0.60;
    public const double CatcherThrowRelease = 0.30;
    /// <summary>The runner's plant-and-turn back toward the bag it left (#966), before the run takes over.</summary>
    public const double TurnBackDur = 0.30;

    /// <summary>
    /// A held load samples the first part of its one-shot take. MAX holds the
    /// full coil at 0; a tap starts from the half load so a quick action still
    /// reads as a wind-up. Swing: <see cref="SwingPresentation.NormalLoadAt"/>.
    /// </summary>
    public const double PitchNormalLoadAt = 0.09;

    /// <param name="FinishAt">A held finish: the second of the take's last key, which the batter holds after the take (0 = none).</param>
    /// <param name="StandIn">
    /// Catalog first, then the take (#966): a slot with no take of its own yet plays this authored clip. Its row states the
    /// contract its take must meet (length, marker, hand); until the take lands every file, marker and hold is the stand-in's.
    /// </param>
    public readonly record struct Clip(
        string Id, bool Loop, bool Handed, double Duration, ClipEvent? Mark = null, double MarkAt = 0,
        double FinishAt = 0, string? StandIn = null);

    /// <summary>The file list. data/art/clips.json must match it row for row.</summary>
    public static readonly IReadOnlyList<Clip> Clips =
    [
        new("idle", true, false, 2.0),
        new("field", true, false, 2.0),
        new("cheer", true, false, 0.8),
        new("charm", true, false, 1.2),
        new("walk", true, false, RunDur / 0.55, ClipEvent.FootPlant, 0),
        new("run", true, false, RunDur, ClipEvent.FootPlant, 0),
        new("jump", false, false, JumpDur, ClipEvent.FootPlant, JumpDur),
        new("pitch", false, true, PitchDur, ClipEvent.Release, PitchRelease),
        new("pitch-charge", false, true, PitchDur, ClipEvent.Release, PitchRelease),
        new("throw", false, true, 0.40, ClipEvent.Release, ThrowRelease),
        new(SwingSlapClip, false, true, SwingFinish, ClipEvent.Contact, SwingContact, SwingFinish),
        new(SwingChargeClip, false, true, SwingFinish, ClipEvent.Contact, SwingContact, SwingFinish),
        new("checkSwing", false, true, HoldDur),
        new("bunt", false, true, HoldDur),
        new(BuntPullClip, false, true, HoldDur),
        new(BuntPushClip, false, true, HoldDur),
        new(LetGoClip, false, true, LetGoDur),
        new("miss", false, true, HoldDur),
        new("catch", false, false, HoldDur),
        new("dive", false, false, HoldDur),
        new(DiveAirClip, false, false, HoldDur),
        new("crouch", false, false, HoldDur),
        new("stealLead", false, false, HoldDur),
        new("spin", false, false, HoldDur),
        new("scoop", false, false, 0.50, ClipEvent.Contact, ScoopContact),
        new("slide", false, false, 0.40, ClipEvent.FootPlant, SlidePlant),
        new("catcherThrow", false, true, CatcherThrowDur, ClipEvent.Release, CatcherThrowRelease, StandIn: "throw"),
        new("tag", false, true, HoldDur, StandIn: "catch"),
        // The same length and plant as the feet-first slide: a style of slide is never a faster one.
        new("slideHeadFirst", false, false, 0.40, ClipEvent.FootPlant, SlidePlant, StandIn: "slide"),
        new("turnBack", false, false, TurnBackDur, StandIn: "run")
    ];

    public static IReadOnlyList<string> ClipIds { get; } = Clips.Select(c => c.Id).ToArray();

    /// <summary>The clip a slot plays today: its own take, or its stand-in's while it has none.</summary>
    public static Clip Played(string id) =>
        TryClip(id, out var clip) && clip.StandIn is { } standIn && TryClip(standIn, out var played) ? played : clip;

    /// <summary>The id of <see cref="Played"/>; an id Motion does not know is itself.</summary>
    public static string PlayedId(string id) => Played(id).Id ?? id;

    public static bool TryClip(string id, out Clip clip)
    {
        clip = Clips.FirstOrDefault(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return !string.IsNullOrEmpty(clip.Id);
    }

    /// <summary>Which take a verb plays and which clock samples it.</summary>
    public readonly record struct Cue(string Clip, Clock Clock);

    /// <summary>
    /// The committed swing's take: the resolver's charge test picks it (ChargeFeel.IsCharge, spec
    /// §5.1), so the narrow charge window and the charge take are always the same swing.
    /// </summary>
    public static string SwingClipFor(double charge01, RulesTable rules) =>
        SwingPresentation.TakeFor(charge01, rules) == SwingTake.Charge ? SwingChargeClip : SwingSlapClip;

    /// <summary>Which take a verb plays uncharged: a committed swing is the slap, a thrown pitch the ordinary pitch.</summary>
    public static Cue CueFor(Verb verb) => CueOf(verb, charged: false);

    /// <summary>
    /// Which take a verb plays. A held swing load is the charge take's windup; a committed swing
    /// is the slap or the charge take by <paramref name="charge01"/> against the match's charge line.
    /// </summary>
    public static Cue CueFor(Verb verb, double charge01, RulesTable rules) =>
        CueOf(verb, ChargeFeel.IsCharge(Math.Clamp(charge01, 0, 1), rules));

    static Cue CueOf(Verb verb, bool charged) => verb switch
    {
        Verb.Idle => new("idle", Clock.World),
        Verb.Field => new("field", Clock.World),
        Verb.Cheer => new("cheer", Clock.World),
        Verb.Charm => new("charm", Clock.World),
        Verb.Walk => new("walk", Clock.World),
        Verb.Run => new("run", Clock.World),
        Verb.Jump or Verb.Clamber => new("jump", Clock.Verb),
        Verb.ChargePitch => new("pitch-charge", Clock.Charge),
        Verb.ThrowPitch => new(charged ? "pitch-charge" : "pitch", Clock.Verb),
        Verb.Throw => new("throw", Clock.Verb),
        Verb.ChargeSwing => new(SwingChargeClip, Clock.Charge),
        Verb.Swing => new(charged ? SwingChargeClip : SwingSlapClip, Clock.Verb),
        Verb.CheckSwing => new("checkSwing", Clock.Verb),
        Verb.Bunt => new("bunt", Clock.Verb),
        Verb.LetGo => new(LetGoClip, Clock.Verb),
        Verb.Miss => new("miss", Clock.Verb),
        Verb.Catch => new("catch", Clock.Verb),
        Verb.Dive => new("dive", Clock.Verb),
        Verb.DiveAir => new(DiveAirClip, Clock.Verb),
        Verb.Crouch => new("crouch", Clock.Verb),
        Verb.StealLead => new("stealLead", Clock.Verb),
        Verb.Spin => new("spin", Clock.Verb),
        Verb.Scoop => new("scoop", Clock.Verb),
        Verb.Slide => new("slide", Clock.Verb),
        Verb.CatcherThrow => new("catcherThrow", Clock.Verb),
        Verb.Tag => new("tag", Clock.Verb),
        Verb.SlideHeadFirst => new("slideHeadFirst", Clock.Verb),
        Verb.TurnBack => new("turnBack", Clock.Verb),
        _ => new("idle", Clock.World)
    };

    public static IReadOnlyList<Verb> Verbs { get; } =
        Enum.GetValues(typeof(Verb)).Cast<Verb>().ToArray();

    /// <summary>The hand that picks the file: the batting hand for hitting verbs, the throwing hand otherwise.</summary>
    public static bool UsesBattingHand(Verb verb) =>
        verb is Verb.ChargeSwing or Verb.Swing or Verb.CheckSwing or Verb.Bunt or Verb.Miss or Verb.LetGo;

    public static bool IsHanded(string clipId) => TryClip(clipId, out var clip) && clip.Handed;

    /// <summary>
    /// File name for a clip and hand: the played take (a slot's stand-in until its own lands). Left-handed takes are baked
    /// as <c>{clip}-L</c>.
    /// </summary>
    public static string ClipFile(string clipId, Hand hand)
    {
        var played = PlayedId(clipId);
        return IsHanded(played) && hand == Hand.L ? played + "-L" : played;
    }

    public static string ClipFile(Verb verb, Hand bats, Hand throws) =>
        ClipFile(CueFor(verb).Clip, UsesBattingHand(verb) ? bats : throws);

    public static string ClipFile(Verb verb, Hand bats, Hand throws, double charge01, RulesTable rules) =>
        ClipFile(CueFor(verb, charge01, rules).Clip, UsesBattingHand(verb) ? bats : throws);

    /// <summary>
    /// The take file a verb plays for a hand and a motion style (CH-12): <c>{style}/{clip}</c> when the style has its own take
    /// of the clip, else the shared <see cref="ClipFile(string, Hand)"/>. The hand is already the batting or throwing hand.
    /// </summary>
    public static string ClipFor(Verb verb, Hand hand, MotionStyle? style, BuntSide bunt = BuntSide.None) =>
        StyledFile(verb == Verb.Bunt ? BuntClip(bunt, hand) : CueFor(verb).Clip, hand, style);

    public static string ClipFor(Verb verb, Hand hand, MotionStyle? style, double charge01, RulesTable rules, BuntSide bunt = BuntSide.None) =>
        StyledFile(verb == Verb.Bunt ? BuntClip(bunt, hand) : CueFor(verb, charge01, rules).Clip, hand, style);

    /// <summary>
    /// The squared take for a held side and the batting hand (PH-14-R3): the pull take when the side is the batter's pull
    /// field (third for a right-handed batter, first for a left-handed one), the push take for the other field, the
    /// sideless square when no side is held. The left-handed file is the baked mirror, so the side reads the same.
    /// </summary>
    public static string BuntClip(BuntSide side, Hand bats) => side switch
    {
        BuntSide.None => "bunt",
        _ => (side == BuntSide.Third) == (bats == Hand.R) ? BuntPullClip : BuntPushClip
    };

    /// <summary>
    /// Where the let-go starts for the load it discards: the full coil at 0, no charge at <see cref="LetGoReturnAt"/>. The
    /// return is linear in the take, so a partial load starts on its own held pose (<see cref="SwingPresentation.HeldLoadAt"/>).
    /// </summary>
    public static double LetGoStartAt(double charge01) => LetGoReturnAt * (1 - Math.Clamp(charge01, 0, 1));

    /// <summary>A clip's file for a hand and a style, falling back to the shared take.</summary>
    public static string StyledFile(string clipId, Hand hand, MotionStyle? style)
    {
        var played = PlayedId(clipId);
        var file = ClipFile(played, hand);
        return style is not null && style.Owns(played) ? style.Id + "/" + file : file;
    }

    /// <summary>The second of a verb's marker in the take it plays (a stand-in's while its own is missing), or 0.</summary>
    public static double Mark(Verb verb, ClipEvent ev)
    {
        var clip = Played(CueFor(verb).Clip);
        return clip.Id != null && clip.Mark == ev ? clip.MarkAt : 0;
    }

    /// <summary>Held load: MAX holds the full coil at 0, a tap starts from the half load.</summary>
    public static double LoadSampleAt(double normalLoadAt, double charge01) =>
        normalLoadAt * (1 - Math.Clamp(charge01, 0, 1));

    public static double PitchLoadSampleAt(double charge01) => LoadSampleAt(PitchNormalLoadAt, charge01);

    /// <summary>The verb's clip time when it was loaded, by charge, before it committed.</summary>
    public static double LoadAtFor(Verb verb, double charge01, RulesTable rules) => verb switch
    {
        Verb.ChargeSwing => SwingPresentation.HeldLoadAt(charge01),
        Verb.Swing => SwingPresentation.CommittedLoadAt(charge01, rules),
        Verb.ChargePitch => PitchLoadSampleAt(charge01),
        Verb.ThrowPitch => ChargeFeel.IsCharge(charge01, rules) ? PitchLoadSampleAt(charge01) : 0,
        _ => 0
    };

    public static bool Holds(Verb verb)
    {
        var clip = Played(CueFor(verb).Clip);
        return clip.Id != null && !clip.Loop && clip.Mark == null;
    }
}
