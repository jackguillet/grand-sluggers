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
        ChargeSwing, Swing, CheckSwing, Bunt, Miss,
        Catch, Dive, Crouch, StealLead, Spin,
        Scoop, Slide
    }

    public enum ClipEvent { Contact, Release, FootPlant }

    /// <summary>What advances the sample time: match time, the verb's own clock, or a held charge.</summary>
    public enum Clock { World, Verb, Charge }

    public const double RunHz = 2.55;
    public const double RunDur = 1 / RunHz;
    public const double JumpDur = 0.55;
    public const double JumpPeak = 4.2;
    public const double SwingDur = 0.50;
    public const double PitchDur = 0.50;
    public const double SwingContact = 0.30;
    public const double PitchRelease = 0.42;
    public const double ThrowRelease = 0.18;
    public const double ScoopContact = 0.22;
    public const double SlidePlant = 0.18;
    public const double HoldDur = 0.20;

    /// <summary>
    /// A held load samples the first part of its one-shot take. MAX holds the
    /// full coil at 0; a tap starts from the half load so a quick action still
    /// reads as a wind-up. Swing: <see cref="SwingPresentation.NormalLoadAt"/>.
    /// </summary>
    public const double PitchNormalLoadAt = 0.09;

    public readonly record struct Clip(
        string Id, bool Loop, bool Handed, double Duration, ClipEvent? Mark = null, double MarkAt = 0);

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
        new("throw", false, true, 0.40, ClipEvent.Release, ThrowRelease),
        new("swing", false, true, SwingDur, ClipEvent.Contact, SwingContact),
        new("checkSwing", false, true, HoldDur),
        new("bunt", false, true, HoldDur),
        new("miss", false, true, HoldDur),
        new("catch", false, false, HoldDur),
        new("dive", false, false, HoldDur),
        new("crouch", false, false, HoldDur),
        new("stealLead", false, false, HoldDur),
        new("spin", false, false, HoldDur),
        new("scoop", false, false, 0.50, ClipEvent.Contact, ScoopContact),
        new("slide", false, false, 0.40, ClipEvent.FootPlant, SlidePlant)
    ];

    public static IReadOnlyList<string> ClipIds { get; } = Clips.Select(c => c.Id).ToArray();

    public static bool TryClip(string id, out Clip clip)
    {
        clip = Clips.FirstOrDefault(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return !string.IsNullOrEmpty(clip.Id);
    }

    /// <summary>Which take a verb plays and which clock samples it.</summary>
    public readonly record struct Cue(string Clip, Clock Clock);

    public static Cue CueFor(Verb verb) => verb switch
    {
        Verb.Idle => new("idle", Clock.World),
        Verb.Field => new("field", Clock.World),
        Verb.Cheer => new("cheer", Clock.World),
        Verb.Charm => new("charm", Clock.World),
        Verb.Walk => new("walk", Clock.World),
        Verb.Run => new("run", Clock.World),
        Verb.Jump or Verb.Clamber => new("jump", Clock.Verb),
        Verb.ChargePitch => new("pitch", Clock.Charge),
        Verb.ThrowPitch => new("pitch", Clock.Verb),
        Verb.Throw => new("throw", Clock.Verb),
        Verb.ChargeSwing => new("swing", Clock.Charge),
        Verb.Swing => new("swing", Clock.Verb),
        Verb.CheckSwing => new("checkSwing", Clock.Verb),
        Verb.Bunt => new("bunt", Clock.Verb),
        Verb.Miss => new("miss", Clock.Verb),
        Verb.Catch => new("catch", Clock.Verb),
        Verb.Dive => new("dive", Clock.Verb),
        Verb.Crouch => new("crouch", Clock.Verb),
        Verb.StealLead => new("stealLead", Clock.Verb),
        Verb.Spin => new("spin", Clock.Verb),
        Verb.Scoop => new("scoop", Clock.Verb),
        Verb.Slide => new("slide", Clock.Verb),
        _ => new("idle", Clock.World)
    };

    public static IReadOnlyList<Verb> Verbs { get; } =
        Enum.GetValues(typeof(Verb)).Cast<Verb>().ToArray();

    /// <summary>The hand that picks the file: the batting hand for hitting verbs, the throwing hand otherwise.</summary>
    public static bool UsesBattingHand(Verb verb) =>
        verb is Verb.ChargeSwing or Verb.Swing or Verb.CheckSwing or Verb.Bunt or Verb.Miss;

    public static bool IsHanded(string clipId) => TryClip(clipId, out var clip) && clip.Handed;

    /// <summary>File name for a clip and hand. Left-handed takes are baked as <c>{clip}-L</c>.</summary>
    public static string ClipFile(string clipId, Hand hand) =>
        IsHanded(clipId) && hand == Hand.L ? clipId + "-L" : clipId;

    public static string ClipFile(Verb verb, Hand bats, Hand throws) =>
        ClipFile(CueFor(verb).Clip, UsesBattingHand(verb) ? bats : throws);

    public static double Mark(Verb verb, ClipEvent ev)
    {
        if (!TryClip(CueFor(verb).Clip, out var clip) || clip.Mark != ev) return 0;
        return clip.MarkAt;
    }

    /// <summary>Held load: MAX holds the full coil at 0, a tap starts from the half load.</summary>
    public static double LoadSampleAt(double normalLoadAt, double charge01) =>
        normalLoadAt * (1 - Math.Clamp(charge01, 0, 1));

    public static double PitchLoadSampleAt(double charge01) => LoadSampleAt(PitchNormalLoadAt, charge01);

    /// <summary>The verb's clip time when it was loaded, by charge, before it committed.</summary>
    public static double LoadAtFor(Verb verb, double charge01) => verb switch
    {
        Verb.ChargeSwing or Verb.Swing => SwingPresentation.LoadSampleAt(charge01),
        Verb.ChargePitch or Verb.ThrowPitch => PitchLoadSampleAt(charge01),
        _ => 0
    };

    public static bool Holds(Verb verb) =>
        TryClip(CueFor(verb).Clip, out var clip) && !clip.Loop && clip.Mark == null;
}
