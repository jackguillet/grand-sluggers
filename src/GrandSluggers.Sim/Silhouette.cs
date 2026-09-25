namespace GrandSluggers.Sim;

/// <summary>
/// Locked body types. Role players reuse the faction captain. See docs/silhouette-bible.md. The body rows are data:
/// each captain's <c>proportions</c> in <c>data/characters</c> (#1032).
/// Height and Width are the Unity root scale (<see cref="SharedRootScale"/>). Head, Arms and Torso are the build
/// (<see cref="Build"/>): shape keys on the one mesh, never a joint, so every take stays shared. Everything that
/// measures a body (the swing contract, the stills, the strike-zone landmarks) reads these functions, and Unity
/// renders from the same ones.
/// </summary>
public static class Silhouette
{
    public readonly record struct Spec(float Height, float Width, float Head, float Arms, float Torso);

    /// <summary>SMS toys sit bigger than an honest diamond. Applied on the shared chain root.</summary>
    public const float ToyScale = 1.18f;
    public const float GloveScale = 1.42f;
    public const float BatScale = 1.28f;

    // ------------------------------------------------------------------ the rest rig (data/art/rig.json anatomy)
    // Batter-local rig units, Unity axes (+Y up, +Z forward). A test pins every number to rig.json.

    /// <summary>Ground to the top of the neutral head.</summary>
    public const double RigHeight = 4.80;
    /// <summary>The neutral head: a sphere this wide, centered at <see cref="HeadCenterAtRest"/>.</summary>
    public const double HeadDiameter = 1.20;
    public static readonly Vec3 HeadCenterAtRest = new(0, 4.20, 0.05);
    /// <summary>The knee (thigh → shin joint).</summary>
    public const double KneeY = 0.90;
    /// <summary>The middle of the thigh bone, halfway from the hip joint to the knee: the strike zone's bottom landmark.</summary>
    public const double ThighMidY = 1.25;
    /// <summary>The chest mark on the torso bone: the strike zone's top landmark.</summary>
    public const double ChestY = 2.99;

    /// <summary>The shared body's head count: rest height over head diameter (CH-02, about four).</summary>
    public const double RestHeadsTall = RigHeight / HeadDiameter;

    // ------------------------------------------------------------------ build (CH-04; data/art/rig.json build)

    /// <summary>
    /// One build channel: a pair of shape keys on the body, <c>{Id}+</c> authored at <see cref="Max"/> and
    /// <c>{Id}-</c> at <see cref="Min"/>. A captain's scale is <c>1 + Gain · (proportion / Neutral − 1)</c>.
    /// </summary>
    public readonly record struct BuildChannel(string Id, double Neutral, double Gain, double Min, double Max)
    {
        public double ScaleFor(double proportion) => 1 + Gain * (proportion / Neutral - 1);
        public string UpKey => Id + "+";
        public string DownKey => Id + "-";

        /// <summary>Shape-key weights 0..1 that draw <paramref name="scale"/>; the keys interpolate linearly.</summary>
        public (double Up, double Down) Weights(double scale) => (
            Math.Clamp((scale - 1) / (Max - 1), 0, 1),
            Math.Clamp((1 - scale) / (1 - Min), 0, 1));
    }

    /// <summary>The head grows about the neck top (<see cref="HeadPivot"/>).</summary>
    public static readonly BuildChannel HeadBuild = new("head", 1.38, 0.8, 0.86, 1.20);
    /// <summary>The arms thicken about the bone line and the mitts grow about their centers; no joint moves.</summary>
    public static readonly BuildChannel ArmsBuild = new("arms", 1.02, 0.6, 0.84, 1.45);
    /// <summary>The jersey, stripe, neck and hips widen about the body axis; heights do not move.</summary>
    public static readonly BuildChannel TorsoBuild = new("torso", 0.94, 0.5, 0.84, 1.32);
    public static IReadOnlyList<BuildChannel> BuildChannels { get; } = [HeadBuild, ArmsBuild, TorsoBuild];

    /// <summary>
    /// Style channels (CH-12, SC-08): the motion style names the scale (<see cref="MotionStyle.Reach"/>), not the proportions.
    /// Reach stretches the arm pieces; the style's own takes move the elbow and wrist to meet them, so a reach style owns
    /// every clip. Neutral and gain are 1: the scale is the style's number itself.
    /// </summary>
    public static readonly BuildChannel ReachBuild = new("reach", 1, 1, 0.90, 1.30);
    /// <summary>The shoes grow about the sole center; no take changes.</summary>
    public static readonly BuildChannel BootsBuild = new("boots", 1, 1, 0.85, 1.40);
    public static IReadOnlyList<BuildChannel> StyleChannels { get; } = [ReachBuild, BootsBuild];

    /// <summary>Shape-key name → weight 0..1 for a motion style's channels; Unity multiplies by 100.</summary>
    public static IReadOnlyList<(string Key, double Weight)> StyleWeights(MotionStyle? style)
    {
        var list = new List<(string, double)>(4);
        foreach (var (channel, scale) in new[] { (ReachBuild, style?.Reach ?? 1), (BootsBuild, style?.Boots ?? 1) })
        {
            var (up, down) = channel.Weights(scale);
            list.Add((channel.UpKey, up));
            list.Add((channel.DownKey, down));
        }
        return list;
    }

    /// <summary>The neck top, where the head bone starts: the head build's pivot.</summary>
    public static readonly Vec3 HeadPivot = new(0, 3.68, 0.05);

    public readonly record struct BodyBuild(double Head, double Arms, double Torso);

    /// <summary>The one build function: sim measurement and Unity's shape-key weights both read it.</summary>
    public static BodyBuild Build(Spec spec) => new(
        HeadBuild.ScaleFor(spec.Head), ArmsBuild.ScaleFor(spec.Arms), TorsoBuild.ScaleFor(spec.Torso));

    /// <summary>Shape-key name → weight 0..1 for this body; Unity multiplies by 100.</summary>
    public static IReadOnlyList<(string Key, double Weight)> BuildWeights(Spec spec)
    {
        var build = Build(spec);
        var list = new List<(string, double)>(6);
        foreach (var (channel, scale) in new[] { (HeadBuild, build.Head), (ArmsBuild, build.Arms), (TorsoBuild, build.Torso) })
        {
            var (up, down) = channel.Weights(scale);
            list.Add((channel.UpKey, up));
            list.Add((channel.DownKey, down));
        }
        return list;
    }

    /// <summary>The captain whose body this character wears: data (<see cref="Character.BodyType"/>), resolved at load.</summary>
    public static string BodyType(Character who) => who.BodyType;

    /// <summary>Lineup face. Role players reuse the faction captain — no unique JPGs.</summary>
    public static string PortraitId(Character who) => who.BodyType;

    public static Spec Proportions(Character who) => who.Proportions;

    /// <summary>A body type's proportions: the captain of that id's authored row (<c>data/characters</c>).</summary>
    public static Spec Proportions(ContentCatalog content, string bodyType) => content.Must(bodyType).Proportions;

    /// <summary>
    /// Shared-rig axes keep stature on Y while X/Z blend girth with height.
    /// Swing reach and Unity presentation consume this same scale relationship.
    /// </summary>
    public static Vec3 SharedRootScale(Spec spec) => new(
        (spec.Height * 0.45 + spec.Width * 0.55) * ToyScale,
        spec.Height * ToyScale,
        (spec.Height * 0.55 + spec.Width * 0.45) * ToyScale);

    /// <summary>Toy read: face must be at least as big as the body is tall.</summary>
    public static float HeadToHeight(Spec spec) => spec.Head / Math.Max(0.01f, spec.Height);

    // ------------------------------------------------------------------ measured on this body, at rest

    /// <summary>This body's head center, rig units (before the root scale): the neutral center grown about the pivot.</summary>
    public static Vec3 HeadCenterRig(Spec spec) => HeadCenterAt(Build(spec).Head);

    /// <summary>The head center for a head build scale, rig units.</summary>
    public static Vec3 HeadCenterAt(double headScale) => new(
        HeadPivot.X + (HeadCenterAtRest.X - HeadPivot.X) * headScale,
        HeadPivot.Y + (HeadCenterAtRest.Y - HeadPivot.Y) * headScale,
        HeadPivot.Z + (HeadCenterAtRest.Z - HeadPivot.Z) * headScale);

    /// <summary>This body's head radius, rig units.</summary>
    public static double HeadRadiusRig(Spec spec) => HeadDiameter / 2 * Build(spec).Head;

    /// <summary>Ground to the top of this body's head, rig units.</summary>
    public static double HeadTopRig(Spec spec) => HeadCenterRig(spec).Y + HeadRadiusRig(spec);

    /// <summary>This body's head count at rest: its height over its head's diameter.</summary>
    public static double HeadsTall(Spec spec) => HeadTopRig(spec) / (2 * HeadRadiusRig(spec));

    /// <summary>Ground to the top of the head in world feet: the height ladder (CH-03) reads this.</summary>
    public static double HeadTopFt(Spec spec) => HeadTopRig(spec) * SharedRootScale(spec).Y;

    /// <summary>The knee landmark in world feet, at rest. The pose never moves it.</summary>
    public static double KneeFt(Spec spec) => KneeY * SharedRootScale(spec).Y;

    /// <summary>The chest landmark in world feet, at rest. The torso build widens the body; it does not move this.</summary>
    public static double ChestFt(Spec spec) => ChestY * SharedRootScale(spec).Y;

    /// <summary>The mid-thigh landmark in world feet, at rest: the strike zone's bottom.</summary>
    public static double ThighMidFt(Spec spec) => ThighMidY * SharedRootScale(spec).Y;

    /// <summary>The rest landmarks of the body this character wears, in world feet (the zone is thigh-mid to chest, §4.4).</summary>
    public static (double KneeFt, double ChestFt, double HeadTopFt) Landmarks(Character who)
    {
        var spec = Proportions(who);
        return (KneeFt(spec), ChestFt(spec), HeadTopFt(spec));
    }

    /// <summary>The strike zone's two rest landmarks of the body this character wears, in world feet: mid-thigh and chest.</summary>
    public static (double ThighMidFt, double ChestFt) ZoneLandmarks(Character who)
    {
        var spec = Proportions(who);
        return (ThighMidFt(spec), ChestFt(spec));
    }
}
