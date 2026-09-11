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
    public const double ContactStretchXZ = 1.14;
    public const double ContactSquashY = 0.84;

    /// <summary>Canonical bat-wood center sits this far above the grip socket.</summary>
    public const double ModelCenterFromGrip = 0.85;
    /// <summary>Authored bat-wood barrel begins below its model origin.</summary>
    public const double BarrelStartFromModelCenter = -0.15;
    /// <summary>Authored bat-wood handle ends just below its model origin.</summary>
    public const double HandleEndFromModelCenter = -0.10;
    /// <summary>Authored bat-wood barrel end from its model origin.</summary>
    public const double BarrelFromModelCenter = 1.25;

    // Authored model-local endpoints before HeroActor applies the shared-socket
    // bind conversion: handle/grip at -Y, barrel along +Y.
    public static readonly Vec3 ModelGrip = new(0, -ModelCenterFromGrip, 0);
    public static readonly Vec3 ModelHandleEnd = new(0, HandleEndFromModelCenter, 0);
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
    public static double HandleLength =>
        (HandleEndFromModelCenter + ModelCenterFromGrip) * Silhouette.BatScale;

    public static double BarrelReach =>
        (ModelCenterFromGrip + BarrelFromModelCenter) * Silhouette.BatScale;

    /// <summary>
    /// A tap starts from the authored half-load; MAX starts from the full coil.
    /// Both are frames of the same DCC take, so the hands never blend toward a
    /// separate runtime pose.
    /// </summary>
    public static double LoadSampleAt(double charge01) =>
        NormalLoadAt * (1 - Math.Clamp(charge01, 0, 1));

    /// <summary>Shared root squash while the barrel accelerates through contact.</summary>
    public static Vec3 RootSquash(double poseT) =>
        poseT >= 0.12 && poseT < 0.32
            ? new Vec3(ContactStretchXZ, ContactSquashY, ContactStretchXZ)
            : new Vec3(1, 1, 1);

    public readonly record struct Key(
        double T,
        Vec3 LeftHand,
        Vec3 RightHand,
        Vec3 Grip,
        Vec3 BarrelDirection);

    // Evaluated rendered-hand centers and socket positions in shared-root space.
    // The DCC authoring check and Unity swing matrix measure these same points;
    // DCC X is reflected during FBX import.
    public static readonly IReadOnlyList<Key> Keys =
    [
        new(LoadAt,
            new(0.300, 2.727, 0.512), new(0.080, 2.522, 0.326),
            new(0.273, 2.215, 0.226), Unit(-0.18, 0.89, 0.42)),
        new(LaunchAt,
            new(0.416, 2.356, -0.235), new(0.143, 2.240, 0.016),
            new(0.326, 2.013, 0.249), Unit(-0.10, 0.62, -0.78)),
        new(ApproachAt,
            new(0.366, 1.729, -0.547), new(0.129, 1.798, -0.140),
            new(0.049, 1.761, 0.071), Unit(0.4315, 0.005, -0.9022)),
        new(ContactAt,
            new(0.458, 1.856, -0.622), new(0.124, 1.914, -0.257),
            new(-0.069, 1.872, -0.149), Unit(0.7790, 0.0275, -0.6264)),
        new(FollowThroughAt,
            new(-0.242, 2.195, -0.576), new(-0.134, 2.155, -0.290),
            new(0.060, 2.032, -0.073), Unit(-0.54, 0.31, -0.78))
    ];

    public static Key At(double poseT, Hand hand = Hand.R)
    {
        var right = Interpolate(poseT);
        return hand == Hand.L ? Mirror(right) : right;
    }

    public static Vec3 BarrelWorld(string bodyType, Hand hand, double poseT, double worldOffsetX = 0)
    {
        var key = At(poseT, hand);
        var scale = Mul(
            Silhouette.SharedRootScale(Silhouette.Proportions(bodyType)),
            RootSquash(poseT));
        var local = Add(key.Grip, Mul(key.BarrelDirection, BarrelReach));
        return new Vec3(
            HomeSet.BatterBodyX(hand, worldOffsetX) + local.X * scale.X,
            local.Y * scale.Y,
            HomeSet.BatterZ + local.Z * scale.Z);
    }

    public static (Vec3 Start, Vec3 End, double Radius) BarrelSegmentWorld(
        string bodyType, Hand hand, double poseT, double worldOffsetX = 0)
    {
        var key = At(poseT, hand);
        var scale = Mul(
            Silhouette.SharedRootScale(Silhouette.Proportions(bodyType)),
            RootSquash(poseT));
        var startReach = (BarrelStartFromModelCenter + ModelCenterFromGrip) * Silhouette.BatScale;
        var start = Add(key.Grip, Mul(key.BarrelDirection, startReach));
        var end = Add(key.Grip, Mul(key.BarrelDirection, BarrelReach));
        Vec3 World(Vec3 local) => new(
            HomeSet.BatterBodyX(hand, worldOffsetX) + local.X * scale.X,
            local.Y * scale.Y,
            HomeSet.BatterZ + local.Z * scale.Z);
        var largest = Math.Max(scale.X, Math.Max(scale.Y, scale.Z));
        return (World(start), World(end), ModelBarrelRadius * Silhouette.BatScale * largest);
    }

    public static bool BarrelCrossesPlate(
        string bodyType, Hand hand, double poseT, double worldOffsetX = 0)
    {
        var barrel = BarrelSegmentWorld(bodyType, hand, poseT, worldOffsetX);
        var min = new Vec3(
            -HomeSet.PlateW / 2 - barrel.Radius,
            PitchFlight.PlateY - 1.2 - barrel.Radius,
            HomeSet.PlatePointZ - barrel.Radius);
        var max = new Vec3(
            HomeSet.PlateW / 2 + barrel.Radius,
            PitchFlight.PlateY + 1.2 + barrel.Radius,
            HomeSet.PlateFrontZ + barrel.Radius);
        return SegmentIntersectsBox(barrel.Start, barrel.End, min, max);
    }

    public static double HandGap(Key key) => Distance(key.LeftHand, key.RightHand);

    public static double HandToHandle(Key key, Hand hand)
    {
        var point = hand == Hand.L ? key.LeftHand : key.RightHand;
        var end = Add(key.Grip, Mul(key.BarrelDirection, HandleLength));
        var axis = new Vec3(end.X - key.Grip.X, end.Y - key.Grip.Y, end.Z - key.Grip.Z);
        var lengthSq = axis.X * axis.X + axis.Y * axis.Y + axis.Z * axis.Z;
        var fromGrip = new Vec3(point.X - key.Grip.X, point.Y - key.Grip.Y, point.Z - key.Grip.Z);
        var u = lengthSq <= 1e-9 ? 0 : Math.Clamp(
            (fromGrip.X * axis.X + fromGrip.Y * axis.Y + fromGrip.Z * axis.Z) / lengthSq,
            0,
            1);
        return Distance(point, Add(key.Grip, Mul(axis, u)));
    }

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
    static Vec3 Mul(Vec3 a, Vec3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    static Vec3 Lerp(Vec3 a, Vec3 b, double u) => Add(a, Mul(new Vec3(b.X - a.X, b.Y - a.Y, b.Z - a.Z), u));
    static double Distance(Vec3 a, Vec3 b) =>
        Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z));

    static bool SegmentIntersectsBox(Vec3 start, Vec3 end, Vec3 min, Vec3 max)
    {
        var direction = new Vec3(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
        var enter = 0.0;
        var exit = 1.0;
        foreach (var axis in new[]
        {
            (Start: start.X, Direction: direction.X, Min: min.X, Max: max.X),
            (Start: start.Y, Direction: direction.Y, Min: min.Y, Max: max.Y),
            (Start: start.Z, Direction: direction.Z, Min: min.Z, Max: max.Z)
        })
        {
            if (Math.Abs(axis.Direction) < 1e-9)
            {
                if (axis.Start < axis.Min || axis.Start > axis.Max) return false;
                continue;
            }
            var a = (axis.Min - axis.Start) / axis.Direction;
            var b = (axis.Max - axis.Start) / axis.Direction;
            if (a > b) (a, b) = (b, a);
            enter = Math.Max(enter, a);
            exit = Math.Min(exit, b);
            if (enter > exit) return false;
        }
        return true;
    }
}

/// <summary>
/// Camera-neutral body directions for the authored batting take. World +Z is
/// the pitcher and the handed plate direction is inward from the batter box.
/// These directions turn the body without moving the shared hand, bat, or
/// plate-intercept keys above.
/// </summary>
public static class BattingStance
{
    public const double AlignmentToleranceDeg = 15;
    public const double AlignmentDot = 0.9659258262890683;

    public readonly record struct Key(
        double T,
        Vec3 ChestForward,
        Vec3 EyesForward,
        Vec3 FeetAxis);

    // Grand Sluggers authored tuning. Ready and launch stay closed to the
    // pitcher; approach, contact, and follow then turn through the ball.
    public static readonly IReadOnlyList<Key> Keys =
    [
        new(SwingPresentation.LoadAt,
            Unit(1, 0, 0), Unit(0, 0, 1), Unit(0, 0, 1)),
        new(SwingPresentation.LaunchAt,
            Unit(1, 0, 0), Unit(0, 0, 1), Unit(0, 0, 1)),
        new(SwingPresentation.ApproachAt,
            Unit(0.70710678, 0, 0.70710678), Unit(0, 0, 1), Unit(0, 0, 1)),
        new(SwingPresentation.ContactAt,
            Unit(0.25881905, 0, 0.96592583), Unit(0, 0, 1), Unit(0, 0, 1)),
        new(SwingPresentation.FollowThroughAt,
            Unit(-0.70710678, 0, 0.70710678), Unit(0, 0, 1), Unit(0, 0, 1))
    ];

    public static Vec3 PlateDirection(Hand hand) =>
        hand == Hand.L ? new Vec3(-1, 0, 0) : new Vec3(1, 0, 0);

    public static Key At(double poseT, Hand hand = Hand.R)
    {
        var right = Interpolate(poseT);
        return hand == Hand.L ? right with
        {
            ChestForward = MirrorX(right.ChestForward),
            EyesForward = MirrorX(right.EyesForward),
            FeetAxis = MirrorX(right.FeetAxis)
        } : right;
    }

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
                Unit(Lerp(a.ChestForward, b.ChestForward, u)),
                Unit(Lerp(a.EyesForward, b.EyesForward, u)),
                Unit(Lerp(a.FeetAxis, b.FeetAxis, u)));
        }
        return Keys[^1];
    }

    static Vec3 Unit(double x, double y, double z) => Unit(new Vec3(x, y, z));
    static Vec3 Unit(Vec3 value)
    {
        var length = Math.Sqrt(value.X * value.X + value.Y * value.Y + value.Z * value.Z);
        return length <= 1e-9
            ? new Vec3(0, 0, 1)
            : new Vec3(value.X / length, value.Y / length, value.Z / length);
    }
    static Vec3 Lerp(Vec3 a, Vec3 b, double u) => new(
        a.X + (b.X - a.X) * u,
        a.Y + (b.Y - a.Y) * u,
        a.Z + (b.Z - a.Z) * u);
    static Vec3 MirrorX(Vec3 value) => new(-value.X, value.Y, value.Z);
}
