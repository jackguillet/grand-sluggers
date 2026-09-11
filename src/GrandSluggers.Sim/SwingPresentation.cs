namespace GrandSluggers.Sim;

/// <summary>
/// Shared-rig bat path in batter-local Unity axes: X crosses the plate, Y is
/// height, and +Z faces the pitcher. The DCC take uses the same keys with its
/// Y/Z axes converted during FBX export. Left-handed swings mirror only X.
/// </summary>
public static class SwingPresentation
{
    public static readonly string[] SharedCaptains =
        ["rio", "vale", "zig", "brondo", "konga", "ashlord"];

    public const double LoadAt = 0.00;
    public const double LaunchAt = 0.15;
    public const double NormalLoadAt = LaunchAt * 0.5;
    public const double ApproachAt = 0.24;
    public const double ContactAt = MoveBones.SwingContact;
    public const double FollowThroughAt = MoveBones.SwingDur;

    /// <summary>Canonical bat-wood center sits this far above the grip socket.</summary>
    public const double ModelCenterFromGrip = 0.85;
    /// <summary>Authored bat-wood barrel begins below its model origin.</summary>
    public const double BarrelStartFromModelCenter = -0.15;
    /// <summary>Authored bat-wood barrel end from its model origin.</summary>
    public const double BarrelFromModelCenter = 1.25;

    // Authored model-local endpoints before HeroActor applies the shared-socket
    // bind conversion: handle/grip at -Y, barrel along +Y.
    public static readonly Vec3 ModelGrip = new(0, -ModelCenterFromGrip, 0);
    public static readonly Vec3 ModelBarrelStart = new(0, BarrelStartFromModelCenter, 0);
    public static readonly Vec3 ModelBarrelEnd = new(0, BarrelFromModelCenter, 0);
    /// <summary>
    /// The authored bat bone points from grip to barrel on local -Y. Blender's
    /// FBX handedness conversion is baked into the DCC socket curves; Unity
    /// applies only this fixed model-to-socket bind.
    /// </summary>
    public static readonly Vec3 ModelBarrelAxisAtSocket = new(0, -1, 0);
    public const double ModelHandleRadius = 0.08;
    public const double ModelBarrelRadius = 0.12;
    public static double BarrelRadius => ModelBarrelRadius * Silhouette.BatScale;

    public static double BarrelReach =>
        (ModelCenterFromGrip + BarrelFromModelCenter) * Silhouette.BatScale;

    /// <summary>
    /// A tap starts from the authored half-load; MAX starts from the full coil.
    /// Both are frames of the same DCC take, so the hands never blend toward a
    /// separate runtime pose.
    /// </summary>
    public static double LoadSampleAt(double charge01) =>
        NormalLoadAt * (1 - Math.Clamp(charge01, 0, 1));

    public readonly record struct Key(
        double T,
        Vec3 LeftHand,
        Vec3 RightHand,
        Vec3 Grip,
        Vec3 BarrelDirection);

    // Measured from the authored hero-shared armature after solving each hand
    // to the grip. DCC +Y maps to Unity -Z; DCC +Z maps to Unity +Y.
    public static readonly IReadOnlyList<Key> Keys =
    [
        new(LoadAt,
            new(0.171, 2.692, 0.417), new(0.222, 2.443, 0.300),
            new(0.055, 2.250, 0.300), Unit(-0.18, 0.89, 0.42)),
        new(LaunchAt,
            new(0.264, 2.349, -0.175), new(0.292, 2.176, 0.043),
            new(0.164, 2.098, 0.250), Unit(-0.10, 0.62, -0.78)),
        new(ApproachAt,
            new(0.407, 1.823, -0.419), new(0.288, 1.819, -0.172),
            new(0.288, 1.799, 0.082), Unit(0.4315, 0.005, -0.9022)),
        new(ContactAt,
            new(0.505, 1.924, -0.480), new(0.287, 1.917, -0.305),
            new(0.303, 1.944, -0.052), Unit(0.7790, 0.0275, -0.6264)),
        new(FollowThroughAt,
            new(-0.101, 2.233, -0.503), new(0.015, 2.143, -0.356),
            new(0.069, 2.227, -0.122), Unit(-0.54, 0.31, -0.78))
    ];

    public static Key At(double poseT, Hand hand = Hand.R)
    {
        var right = Interpolate(poseT);
        return hand == Hand.L ? Mirror(right) : right;
    }

    public static Vec3 BarrelWorld(string bodyType, Hand hand, double poseT, double worldOffsetX = 0)
    {
        var key = At(poseT, hand);
        var scale = Silhouette.SharedRootScale(Silhouette.Proportions(bodyType));
        var local = Add(key.Grip, Mul(key.BarrelDirection, BarrelReach));
        return new Vec3(
            HomeSet.BatterBodyX(hand, worldOffsetX) + local.X * scale.X,
            local.Y * scale.Y,
            HomeSet.BatterZ + local.Z * scale.Z);
    }

    public static double HandGap(Key key) => Distance(key.LeftHand, key.RightHand);

    public static double ContactAttackAngleDeg(Hand hand = Hand.R)
    {
        var from = BarrelPoint(At(ApproachAt, hand));
        var to = BarrelPoint(At(ContactAt, hand));
        var horizontal = Math.Sqrt(
            (to.X - from.X) * (to.X - from.X) +
            (to.Z - from.Z) * (to.Z - from.Z));
        return Math.Atan2(to.Y - from.Y, horizontal) * 180 / Math.PI;
    }

    public static Vec3 BarrelPoint(Key key) =>
        Add(key.Grip, Mul(key.BarrelDirection, BarrelReach));

    static Key Interpolate(double poseT)
    {
        var t = Math.Clamp(poseT, Keys[0].T, Keys[^1].T);
        for (var i = 0; i < Keys.Count - 1; i++)
        {
            var a = Keys[i];
            var b = Keys[i + 1];
            if (t > b.T && i < Keys.Count - 2) continue;
            var u = b.T - a.T <= 1e-9 ? 1 : (t - a.T) / (b.T - a.T);
            return new Key(t,
                Lerp(a.LeftHand, b.LeftHand, u),
                Lerp(a.RightHand, b.RightHand, u),
                Lerp(a.Grip, b.Grip, u),
                Normalize(Lerp(a.BarrelDirection, b.BarrelDirection, u)));
        }
        return Keys[^1];
    }

    static Key Mirror(Key key) => key with
    {
        LeftHand = MirrorX(key.RightHand),
        RightHand = MirrorX(key.LeftHand),
        Grip = MirrorX(key.Grip),
        BarrelDirection = MirrorX(key.BarrelDirection)
    };

    static Vec3 Unit(double x, double y, double z) => Normalize(new Vec3(x, y, z));

    static Vec3 Normalize(Vec3 value)
    {
        var d = Math.Sqrt(value.X * value.X + value.Y * value.Y + value.Z * value.Z);
        return d <= 1e-9 ? new Vec3(0, 1, 0) : new Vec3(value.X / d, value.Y / d, value.Z / d);
    }

    static Vec3 MirrorX(Vec3 value) => new(-value.X, value.Y, value.Z);
    static Vec3 Add(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    static Vec3 Mul(Vec3 a, double k) => new(a.X * k, a.Y * k, a.Z * k);
    static Vec3 Lerp(Vec3 a, Vec3 b, double u) => Add(a, Mul(new Vec3(b.X - a.X, b.Y - a.Y, b.Z - a.Z), u));
    static double Distance(Vec3 a, Vec3 b) =>
        Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z));
}
