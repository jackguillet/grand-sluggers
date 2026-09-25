namespace GrandSluggers.Sim;

/// <summary>
/// The two authored swings on the shared rig (#613). A slap has no windup and a compact arc; a
/// charge shows its windup during the hold and swings a bigger arc. Both meet the ball at the
/// same Contact mark (the D13 warp) and end on a held finish (#583).
/// </summary>
public enum SwingTake { Slap, Charge }

/// <summary>
/// Shared-rig bat path in batter-local Unity axes: X crosses the plate, Y is
/// height, and +Z faces the pitcher. The DCC takes are authored against these
/// keys (data/art/swing-takes.json) and baked for both hands; a left-handed swing
/// is the exact reflection.
/// </summary>
public static class SwingPresentation
{
    public const double LoadAt = 0.00;
    public const double LaunchAt = 0.15;
    /// <summary>The charge take's ready key: a held load at no charge. MAX holds the windup at <see cref="LoadAt"/>.</summary>
    public const double NormalLoadAt = LaunchAt * 0.5;
    public const double ApproachAt = 0.24;
    public const double ContactAt = Motion.SwingContact;
    /// <summary>
    /// Center of the authored swing's contact band (±1.2 ft): the DCC take contract, measured by
    /// the still gate. Not the pitch's crossing height (the middle of the batter's zone, <see cref="StrikeZoneGeometry.For"/>).
    /// </summary>
    public const double PlateBandY = 2.4;
    /// <summary>Half the height of the contact band around <see cref="PlateBandY"/>.</summary>
    public const double PlateBandHalf = 1.2;
    public const double FollowThroughAt = Motion.SwingDur;
    /// <summary>The held finish (#583): the takes' last key.</summary>
    public const double FinishAt = Motion.SwingFinish;
    public const double ContactStretchXZ = 1.14;
    public const double ContactSquashY = 0.84;
    /// <summary>
    /// A held load shows the barrel standing above the hands. Measured on the
    /// rendered bat in world space, so a squat captain's shared-root squash
    /// counts against it: the ready/load barrel must clear this on every body.
    /// </summary>
    public const double LoadedBarrelRise = 0.70;
    /// <summary>
    /// #560: from the plate SET the loaded barrel sits beside the head, not
    /// inside the head disk. Degrees of angular clearance of the hovering
    /// barrel-wood (not the handle) beyond the head's silhouette. The ready
    /// key that hid the bat up the back of the skull measured −1.3°.
    /// </summary>
    public const double PlateLoadedBesideDeg = 0.5;
    /// <summary>
    /// First sample of the barrel wood used for <see cref="BarrelBesideHeadDeg"/>.
    /// The handle-adjacent end can sit on the torso and still hide the hover.
    /// </summary>
    public const double BesideWoodFrom = 0.35;

    /// <summary>
    /// The shared rig's neutral head mesh at rest, rig units, batter-local (hero_shared_blockout.py HEAD,
    /// a sphere of radius <see cref="HeadRadius"/>; <see cref="Silhouette.HeadCenterAtRest"/>). A key's
    /// <see cref="Key.Lift"/> is the measured rise or drop of the head in that key's stance.
    /// </summary>
    public static readonly Vec3 HeadCenterAtRest = Silhouette.HeadCenterAtRest;
    public const double HeadRadius = Silhouette.HeadDiameter / 2;
    /// <summary>
    /// How far the head's center can wander from the vertical axis as the stance yaws the root,
    /// torso and head (measured at most 0.08 across both takes). The contract treats the head as
    /// this much bigger so the check never depends on the exact yaw.
    /// </summary>
    public const double HeadYawSlack = 0.10;
    /// <summary>
    /// #623: the bat never passes through the head. The smallest surface-to-surface distance, rig
    /// units, between the physical bat and the head on every sample of both takes and the whole
    /// charge-up (data/art/swing-takes.json <c>batHeadClearance</c>). The DCC bake measures it
    /// exactly on the rendered head, neutral and at the head build's max; <see cref="HeadClearance(Key)"/>
    /// is the conservative contract, against the biggest head the build allows
    /// (<see cref="Silhouette.HeadBuild"/> max). The root scale grows the bat and the head together;
    /// the Unity swing matrix measures the drawn result.
    /// </summary>
    public const double BatHeadClearance = 0.10;
    /// <summary>The physical bat along its axis from the grip socket, model units: the knob behind, the barrel end ahead.</summary>
    public const double BatStartFromGrip = -0.29;
    public const double BatEndFromGrip = ModelCenterFromGrip + BarrelFromModelCenter;

    /// <summary>
    /// Surface-to-surface distance from the physical bat to the head for one key (negative =
    /// through the head), with the head at the build's max and grown by <see cref="HeadYawSlack"/>.
    /// </summary>
    public static double HeadClearance(Key key) => HeadClearance(key, Silhouette.HeadBuild.Max);

    /// <summary>
    /// <see cref="HeadClearance(Key)"/> for one head build scale (1 = the neutral head), grown about
    /// <see cref="Silhouette.HeadPivot"/> as the head shape keys grow it.
    /// </summary>
    public static double HeadClearance(Key key, double headScale)
    {
        var center = new Vec3(0, Silhouette.HeadCenterAt(headScale).Y + key.Lift, 0);
        var axis = Normalize(key.BarrelDirection);
        var start = Add(key.Grip, Mul(axis, BatStartFromGrip * Silhouette.BatScale));
        var end = Add(key.Grip, Mul(axis, BatEndFromGrip * Silhouette.BatScale));
        var along = new Vec3(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
        var toCenter = new Vec3(center.X - start.X, center.Y - start.Y, center.Z - start.Z);
        var lengthSq = along.X * along.X + along.Y * along.Y + along.Z * along.Z;
        var u = Math.Clamp((toCenter.X * along.X + toCenter.Y * along.Y + toCenter.Z * along.Z) / lengthSq, 0, 1);
        return Distance(center, Add(start, Mul(along, u))) - HeadRadius * headScale - HeadYawSlack - BarrelRadius;
    }

    /// <summary>
    /// Angular clearance in degrees of the hovering barrel from the head
    /// silhouette as seen from <paramref name="shot"/>. Positive = beside.
    /// The key is already handed; <paramref name="hand"/> only places the body.
    /// </summary>
    public static double BarrelBesideHeadDeg(
        Key key, Hand hand, CameraShot shot, Silhouette.Spec body)
    {
        var scale = Silhouette.SharedRootScale(body);
        Vec3 World(Vec3 local) => new(
            HomeSet.BatterBodyX(hand) + local.X * scale.X,
            local.Y * scale.Y,
            HomeSet.BatterZ + local.Z * scale.Z);
        var rest = Silhouette.HeadCenterRig(body);
        var head = World(new Vec3(0, rest.Y + key.Lift, rest.Z));
        var radius = Silhouette.HeadRadiusRig(body) * Math.Max(scale.X, Math.Max(scale.Y, scale.Z));
        var cam = shot.Pos;
        var toHead = new Vec3(head.X - cam.X, head.Y - cam.Y, head.Z - cam.Z);
        var distHead = Math.Sqrt(toHead.X * toHead.X + toHead.Y * toHead.Y + toHead.Z * toHead.Z);
        if (distHead <= radius) return -180;
        var angRadius = Math.Asin(Math.Min(1, radius / distHead)) * 180 / Math.PI;
        var axis = Normalize(key.BarrelDirection);
        var startReach = (BarrelStartFromModelCenter + ModelCenterFromGrip) * Silhouette.BatScale;
        var wood0 = Add(key.Grip, Mul(axis, startReach));
        var wood1 = Add(key.Grip, Mul(axis, BarrelReach));
        var best = -180.0;
        const int samples = 24;
        for (var i = 0; i <= samples; i++)
        {
            var u = i / (double)samples;
            if (u < BesideWoodFrom) continue;
            var p = World(Lerp(wood0, wood1, u));
            var toP = new Vec3(p.X - cam.X, p.Y - cam.Y, p.Z - cam.Z);
            var distP = Math.Sqrt(toP.X * toP.X + toP.Y * toP.Y + toP.Z * toP.Z);
            if (distP < 1e-6 || distHead < 1e-6) continue;
            var cos = (toHead.X * toP.X + toHead.Y * toP.Y + toHead.Z * toP.Z) / (distHead * distP);
            var sep = Math.Acos(Math.Clamp(cos, -1, 1)) * 180 / Math.PI;
            best = Math.Max(best, sep - angRadius);
        }
        return best;
    }

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
    /// <summary>
    /// A rendered hand is a fist around the handle, not a point on it. The still
    /// gate measures the mesh against the physical handle; the authored keys
    /// must stay inside this so a DCC take can seat both fists at any sample.
    /// </summary>
    public const double HandToHandleAllowance = 0.20;
    public const double ModelHandleRadius = 0.08;
    public const double ModelBarrelRadius = 0.12;
    public static double BarrelRadius => ModelBarrelRadius * Silhouette.BatScale;
    public static double HandleLength =>
        (HandleEndFromModelCenter + ModelCenterFromGrip) * Silhouette.BatScale;

    public static double BarrelReach =>
        (ModelCenterFromGrip + BarrelFromModelCenter) * Silhouette.BatScale;

    /// <summary>The take a committed swing plays: the resolver's charge test (spec §5.1).</summary>
    public static SwingTake TakeFor(double charge01, RulesTable rules) =>
        ChargeFeel.IsCharge(Math.Clamp(charge01, 0, 1), rules) ? SwingTake.Charge : SwingTake.Slap;

    /// <summary>
    /// The held load samples the charge take: no charge holds its ready key, MAX its windup.
    /// Frames of the same DCC take, so the hands never blend toward a separate runtime pose.
    /// </summary>
    public static double HeldLoadAt(double charge01) =>
        Motion.LoadSampleAt(NormalLoadAt, charge01);

    /// <summary>
    /// Where a committed swing starts in its take: a charge continues from the held windup; a
    /// slap has no windup and starts on its ready key.
    /// </summary>
    public static double CommittedLoadAt(double charge01, RulesTable rules) =>
        TakeFor(charge01, rules) == SwingTake.Charge ? HeldLoadAt(charge01) : LoadAt;

    public static IReadOnlyList<Key> KeysFor(SwingTake take) => take == SwingTake.Charge ? ChargeKeys : SlapKeys;

    /// <summary>Shared root squash while the barrel accelerates through contact.</summary>
    public static Vec3 RootSquash(double poseT) =>
        poseT >= 0.12 && poseT < 0.32
            ? new Vec3(ContactStretchXZ, ContactSquashY, ContactStretchXZ)
            : new Vec3(1, 1, 1);

    /// <param name="Lift">The root lift (crouch) the take's legs carry at this key, rig units: it moves the head.</param>
    public readonly record struct Key(
        double T,
        Vec3 LeftHand,
        Vec3 RightHand,
        Vec3 Grip,
        Vec3 BarrelDirection,
        double Lift = 0);

    // Rendered-hand centers and the grip socket in batter-local space for a
    // right-handed batter; Mirror() derives the left-handed contract. LeftHand
    // and RightHand are the batter's own hands (the renderer named lHand). The
    // lead hand (BattingStance.LeadSide) holds the knob end: a right-handed
    // batter's LEFT hand sits nearest Grip on every key and under the right
    // hand in the held load. The DCC takes (tools/blender/hero_shared_takes.py)
    // read the same numbers from data/art/swing-takes.json and solve to them;
    // SwingPresentationTests holds the two equal, and the Unity swing matrix
    // measures the rendered result.
    // <swing-keys>
    // Generated from data/art/swing-takes.json by tools/blender/sync_swing_contract.py.
    public static readonly IReadOnlyList<Key> SlapKeys =
    [
        new(0, new(0.6485, 3.1287, -0.5863), new(0.8455, 3.5523, -0.7341), new(0.56, 2.9385, -0.52), Unit(0.4021, 0.86451, -0.30157), 0.04),
        new(0.15, new(0.304, 2.4353, 0.0776), new(0.2551, 2.7387, -0.304), new(0.326, 2.2991, 0.249), Unit(-0.0999, 0.6191, -0.7789), 0.08),
        new(0.24, new(0.1439, 1.6912, 0.4025), new(0.3553, 1.6936, -0.0395), new(0.049, 1.6901, 0.601), Unit(0.4315, 0.005, -0.9021), 0.03),
        new(0.3, new(0.2118, 1.8165, 0.6426), new(0.6836, 1.8509, 0.5148), new(0, 1.8011, 0.7), Unit(0.9628, 0.0702, -0.2608), -0.06),
        new(0.5, new(-0.0588, 2.8533, -0.2694), new(-0.3234, 3.0199, -0.6467), new(0.06, 2.7785, -0.1), Unit(-0.54, 0.34, -0.77), -0.01),
        new(0.6, new(-0.1359, 3.1024, 0.0464), new(-0.2159, 3.27, -0.407), new(-0.1, 3.0272, 0.25), Unit(-0.1632, 0.342, -0.9254), -0.01),
    ];

    public static readonly IReadOnlyList<Key> ChargeKeys =
    [
        new(0, new(0.7371, 3.3734, -0.4519), new(0.8197, 3.7997, -0.679), new(0.7, 3.182, -0.35), Unit(0.1686, 0.87, -0.4633), 0.03),
        new(0.075, new(0.6485, 3.1287, -0.5863), new(0.8455, 3.5523, -0.7341), new(0.56, 2.9385, -0.52), Unit(0.4021, 0.86451, -0.30157), 0.04),
        new(0.15, new(0.304, 2.4353, 0.0776), new(0.2551, 2.7387, -0.304), new(0.326, 2.2991, 0.249), Unit(-0.0999, 0.6191, -0.7789), 0.08),
        new(0.24, new(0.1439, 1.6912, 0.4025), new(0.3553, 1.6936, -0.0395), new(0.049, 1.6901, 0.601), Unit(0.4315, 0.005, -0.9021), 0.03),
        new(0.3, new(0.2118, 1.8165, 0.6426), new(0.6836, 1.8509, 0.5148), new(0, 1.8011, 0.7), Unit(0.9628, 0.0702, -0.2608), -0.06),
        new(0.5, new(-0.1538, 3.1078, -0.2274), new(-0.4963, 3.3133, -0.5112), new(0, 3.0155, -0.1), Unit(-0.699, 0.4194, -0.5792), -0.02),
        new(0.6, new(-0.1118, 3.1641, -0.0167), new(-0.0267, 3.1641, -0.4992), new(-0.15, 3.1641, 0.2), Unit(0.1736, 0, -0.9848), -0.02),
    ];

    // </swing-keys>

    public static Key At(double poseT, Hand hand, SwingTake take)
    {
        var right = Interpolate(KeysFor(take), poseT);
        return hand == Hand.L ? Mirror(right) : right;
    }

    public static Vec3 BarrelWorld(Silhouette.Spec body, Hand hand, double poseT, SwingTake take, double worldOffsetX = 0)
    {
        var key = At(poseT, hand, take);
        var scale = Mul(
            Silhouette.SharedRootScale(body),
            RootSquash(poseT));
        var local = Add(key.Grip, Mul(key.BarrelDirection, BarrelReach));
        return new Vec3(
            HomeSet.BatterBodyX(hand, worldOffsetX) + local.X * scale.X,
            local.Y * scale.Y,
            HomeSet.BatterZ + local.Z * scale.Z);
    }

    public static (Vec3 Start, Vec3 End, double Radius) BarrelSegmentWorld(
        Silhouette.Spec body, Hand hand, double poseT, SwingTake take, double worldOffsetX = 0)
    {
        var key = At(poseT, hand, take);
        var scale = Mul(
            Silhouette.SharedRootScale(body),
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
        Silhouette.Spec body, Hand hand, double poseT, SwingTake take, double worldOffsetX = 0)
    {
        var barrel = BarrelSegmentWorld(body, hand, poseT, take, worldOffsetX);
        var min = new Vec3(
            -HomeSet.PlateW / 2 - barrel.Radius,
            PlateBandY - PlateBandHalf - barrel.Radius,
            HomeSet.PlatePointZ - barrel.Radius);
        var max = new Vec3(
            HomeSet.PlateW / 2 + barrel.Radius,
            PlateBandY + PlateBandHalf + barrel.Radius,
            HomeSet.PlateFrontZ + barrel.Radius);
        return SegmentIntersectsBox(barrel.Start, barrel.End, min, max);
    }

    /// <summary>
    /// The barrel meets the ball where the ball is judged: the barrel wood cuts the zone plane
    /// (<see cref="StrikeZoneGeometry.PlateZ"/>, the plate front) over the plate's width and inside the
    /// contact band. A barrel that only crosses the plate's footprint behind the front edge fails.
    /// </summary>
    public static bool BarrelCutsZonePlane(
        Silhouette.Spec body, Hand hand, double poseT, SwingTake take, double worldOffsetX = 0)
    {
        var barrel = BarrelSegmentWorld(body, hand, poseT, take, worldOffsetX);
        return CutsZonePlane(barrel.Start, barrel.End, barrel.Radius);
    }

    /// <summary>
    /// <see cref="BarrelCutsZonePlane"/> for a measured barrel segment in world feet (the Unity swing
    /// matrix passes the drawn bat).
    /// </summary>
    public static bool CutsZonePlane(Vec3 start, Vec3 end, double radius)
    {
        var dz = end.Z - start.Z;
        if (Math.Abs(dz) < 1e-9) return false;
        var u = (StrikeZoneGeometry.PlateZ - start.Z) / dz;
        if (u < 0 || u > 1) return false;
        var x = start.X + (end.X - start.X) * u;
        var y = start.Y + (end.Y - start.Y) * u;
        return Math.Abs(x) <= HomeSet.PlateW / 2 + radius
            && y >= PlateBandY - PlateBandHalf - radius
            && y <= PlateBandY + PlateBandHalf + radius;
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

    /// <summary>
    /// Where a hand sits along the handle, in feet from the grip socket at the
    /// knob toward the barrel. The lead hand (<see cref="BattingStance.LeadSide"/>)
    /// holds the knob end, so it reads smaller than the top hand on every key.
    /// Height cannot say this once the barrel comes level at approach.
    /// </summary>
    public static double HandAlongHandle(Key key, Hand hand)
    {
        var point = hand == Hand.L ? key.LeftHand : key.RightHand;
        var axis = Normalize(key.BarrelDirection);
        return (point.X - key.Grip.X) * axis.X
            + (point.Y - key.Grip.Y) * axis.Y
            + (point.Z - key.Grip.Z) * axis.Z;
    }

    public static double ContactAttackAngleDeg(SwingTake take, Hand hand = Hand.R)
    {
        var from = BarrelPoint(At(ApproachAt, hand, take));
        var to = BarrelPoint(At(ContactAt, hand, take));
        var horizontal = Math.Sqrt(
            (to.X - from.X) * (to.X - from.X) +
            (to.Z - from.Z) * (to.Z - from.Z));
        return Math.Atan2(to.Y - from.Y, horizontal) * 180 / Math.PI;
    }

    public static Vec3 BarrelPoint(Key key) =>
        Add(key.Grip, Mul(key.BarrelDirection, BarrelReach));

    static Key Interpolate(IReadOnlyList<Key> keys, double poseT)
    {
        var t = Math.Clamp(poseT, keys[0].T, keys[^1].T);
        for (var i = 0; i < keys.Count - 1; i++)
        {
            var a = keys[i];
            var b = keys[i + 1];
            if (t > b.T && i < keys.Count - 2) continue;
            var u = b.T - a.T <= 1e-9 ? 1 : (t - a.T) / (b.T - a.T);
            return new Key(t,
                Lerp(a.LeftHand, b.LeftHand, u),
                Lerp(a.RightHand, b.RightHand, u),
                Lerp(a.Grip, b.Grip, u),
                Normalize(Lerp(a.BarrelDirection, b.BarrelDirection, u)),
                a.Lift + (b.Lift - a.Lift) * u);
        }
        return keys[^1];
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
///
/// Jack's stance, for both hands: the bat hovers above the batting-side
/// shoulder, the feet face the plate, and the lead side -- opposite the
/// batting hand -- rides under the top hand on the handle and stands nearer
/// the pitcher. A left-handed batter is that pose reflected across the plate
/// line, so every direction here is written against the lead side rather
/// than against "left" or "right".
/// </summary>
public static class BattingStance
{
    public const double AlignmentToleranceDeg = 15;
    public const double AlignmentDot = 0.9659258262890683;

    /// <param name="ChestForward">Where the visible chest faces.</param>
    /// <param name="EyesForward">Where the visible eyes look.</param>
    /// <param name="FeetAxis">
    /// From the back foot to the lead foot. Not "right minus left": a
    /// right-handed batter leads with the left foot, a left-handed batter with
    /// the right, so this signed line reads +Z only when the hips face the
    /// plate. Aiming the take at right-minus-left instead put the right foot
    /// forward on a right-handed batter, turned the hips out of the box with the
    /// toes pointing away from the plate, and left the torso twisted half a
    /// turn to keep the chest on it.
    /// </param>
    public readonly record struct Key(
        double T,
        Vec3 ChestForward,
        Vec3 EyesForward,
        Vec3 FeetAxis);

    /// <summary>
    /// The side that leads the swing is the one opposite the batting hand. It
    /// holds the knob end of the handle under the top hand and stands nearer
    /// the pitcher: a right-handed batter leads with the left hand and foot, a
    /// left-handed batter with the right.
    /// </summary>
    public static Hand LeadSide(Hand bats) => bats == Hand.L ? Hand.R : Hand.L;

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
            Unit(-0.70710678, 0, 0.70710678), Unit(0, 0, 1), Unit(0, 0, 1)),
        new(SwingPresentation.FinishAt,
            Unit(-0.8660254, 0, 0.5), Unit(0, 0, 1), Unit(0, 0, 1))
    ];

    public static Vec3 PlateDirection(Hand hand) =>
        hand == Hand.L ? new Vec3(-1, 0, 0) : new Vec3(1, 0, 0);

    public static Key At(double poseT, Hand hand = Hand.R)
    {
        var right = Interpolate(poseT);
        // The reflection that makes a left-handed batter swaps which foot
        // leads. FeetAxis is already written back-to-lead, so its Z survives.
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
