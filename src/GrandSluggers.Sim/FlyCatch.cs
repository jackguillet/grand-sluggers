namespace GrandSluggers.Sim;

/// <summary>
/// Timed fly / wall catch. CPU dead-stick still uses <see cref="FieldingResolver.Resolve"/>.
/// Player with the glove owns the jump. Super Jump / Grow / Clamber widen the window,
/// they do not skip it. Camera: <see cref="PlayCamera.Beat.Fly"/> / Homer / Wall.
/// Harbor wall only — no extra parks, no Nintendo mesh.
/// </summary>
public static class FlyCatch
{
    public static bool IsFly(FieldingPreview pre) => pre.Class.IsFlyShape();

    /// <summary>The flight clears the fence: only a leap in the window at the wall takes it. South does not scoop a rob.</summary>
    public static bool NeedsJump(FieldingPreview pre) => pre.HomeRunLikely;

    /// <summary>
    /// The rob height (§8.4, fielding.catch.*RobFt): a leap takes a ball clearing the fence by at
    /// most the glove's reach over it — plain jump, Super Jump, Clamber on a climb wall, or a
    /// buddy jump. A ball higher than that is gone whatever the window says.
    /// </summary>
    public static double RobHeightFt(Character? fielder, Park? park, bool buddy = false, RulesTable? rules = null)
    {
        var c = Rules.Or(rules).Fielding.Catch;
        var reach = c.JumpRobFt;
        if (buddy) reach = Math.Max(reach, c.BuddyJumpRobFt);
        if (fielder is null) return reach;
        if (fielder.FieldAbility.Equals("super-jump", StringComparison.OrdinalIgnoreCase))
            reach = Math.Max(reach, c.SuperJumpRobFt);
        if (park != null && ParkHazards.CanClamber(park, fielder))
            reach = Math.Max(reach, c.ClamberRobFt);
        return reach;
    }

    /// <summary>Can this glove's leap reach a ball clearing the fence by <paramref name="clearFt"/>?</summary>
    public static bool CanRob(double clearFt, Character? fielder, Park? park, bool buddy = false, RulesTable? rules = null) =>
        !double.IsNaN(clearFt) && clearFt <= RobHeightFt(fielder, park, buddy, rules);

    /// <summary>
    /// Super Jump / Grow / Clamber add seconds, not an auto-rob.
    /// Harbor has no climb wall, so Clamber is zero there.
    /// </summary>
    public static double ExtraWindowSec(Character? fielder, Park? park, RulesTable? rules = null)
    {
        if (fielder is null) return 0;
        var c = Rules.Or(rules).Fielding.Catch;
        var extra = 0.0;
        if (fielder.FieldAbility.Equals("super-jump", StringComparison.OrdinalIgnoreCase))
            extra += c.SuperJumpWindowSec;
        if (fielder.FieldAbility.Equals("grow", StringComparison.OrdinalIgnoreCase)
            || fielder.FieldAbility.Equals("lick-catch", StringComparison.OrdinalIgnoreCase))
            extra += c.GrowWindowSec;
        if (park != null && ParkHazards.CanClamber(park, fielder))
            extra += c.ClamberWindowSec;
        return extra;
    }

    /// <summary>[hang − windowBefore − extra, hang + windowAfter + extra/2] (fielding.catch).</summary>
    public static bool JumpWindow(double hitT, double hangSec, Character? fielder = null, Park? park = null, RulesTable? rules = null)
    {
        var c = Rules.Or(rules).Fielding.Catch;
        var extra = ExtraWindowSec(fielder, park, rules);
        return hitT >= hangSec - (c.WindowBeforeSec + extra)
               && hitT <= hangSec + (c.WindowAfterSec + extra * 0.5);
    }

    public static bool SitOnWall(double hitT, double hangSec, RulesTable? rules = null) =>
        hitT >= hangSec - Rules.Or(rules).Fielding.Catch.WallSitSec;

    public static bool HighEnough(double ballY, bool wall, RulesTable? rules = null)
    {
        var c = Rules.Or(rules).Fielding.Catch;
        return ballY > (wall ? c.WallBallY : c.JumpBallY);
    }

    public static bool Under(
        double gloveX,
        double gloveZ,
        double ballX,
        double ballZ,
        double plantX,
        double plantZ,
        double windowFt,
        bool needsJump,
        RulesTable? rules = null)
    {
        _ = ballX;
        _ = ballZ;
        // The landing ring is the catch. Live XZ while the ball is still up
        // is the home-first miss — standing in the circle was a drop.
        var reach = needsJump ? Math.Max(windowFt, Rules.Or(rules).Fielding.Catch.NeedsJumpReachFt) : windowFt;
        return Diamond.Dist(gloveX, gloveZ, plantX, plantZ) < reach;
    }

    /// <summary>
    /// Stick-owned catch. Jump is the leap while it is armed, not only the
    /// press frame. South scoops a routine fly you are under — not a rob.
    /// </summary>
    public static bool PlayerCaught(
        bool jumpDown,
        bool southDown,
        bool under,
        bool inWindow,
        bool needsJump,
        bool canRob = true) =>
        (jumpDown && inWindow && under && (!needsJump || canRob)) || (southDown && under && !needsJump);

    public static bool PlayerDiveCatch(bool diveArmed, double distFt, double windowFt, double ballY, RulesTable? rules = null) =>
        diveArmed && distFt < windowFt && ballY < Rules.Or(rules).Fielding.Catch.DiveMaxBallY;

    /// <summary>Dead-stick / CPU: under a routine fly in the window is a catch. Not a rob.</summary>
    public static bool AutoCatch(bool under, bool inWindow, bool needsJump) =>
        under && inWindow && !needsJump;

    /// <summary>
    /// Hopper on the dirt: if the glove can touch the ball, they scoop.
    /// No South. No stick. A fly still in the air is not a pickup.
    /// </summary>
    public static bool TouchScoop(double distFt, double windowFt, double ballY, RulesTable? rules = null) =>
        distFt < windowFt && ballY < Rules.Or(rules).Fielding.Catch.TouchScoopY;

    /// <summary>A dirt pickup cannot reach through the wall or replace an aerial catch.</summary>
    public static bool TouchScoop(FieldingPreview pre, Park park, double ballX, double ballZ,
        double ballY, double hitT, double hangSec, double distFt, double windowFt, RulesTable? rules = null) =>
        PickupInPlay(pre, park, ballX, ballZ, hitT, hangSec, rules)
        && TouchScoop(distFt, windowFt, ballY, rules);

    /// <summary>Shared eligibility for automatic, button and diving dirt pickups.</summary>
    public static bool PickupInPlay(FieldingPreview pre, Park park, double ballX, double ballZ,
        double hitT, double hangSec, RulesTable? rules = null) =>
        (pre.Grounder || pre.Line || hitT >= hangSec)
        && FieldBounds.InPark(park, ballX, ballZ);

    public static PlayKind PlayerKind(bool caught, FieldingPreview pre, AtBatResult? hit, bool inAir = true, RulesTable? rules = null)
    {
        if (caught && pre.Grounder) return PlayKind.GroundOut;
        if (caught && inAir && !pre.Grounder) return PlayKind.FlyOut;
        if (pre.HomeRunLikely)
            return PlayKind.HomeRun;
        // Bounced then over, or off the wall (§1, §7.9): a double until P3 runs the bases by geometry.
        if (pre.Ball is { GroundRule: true } || pre.Class == BattedBallClass.Wall)
            return PlayKind.Double;
        var carry = hit?.CarryFt ?? 0;
        var bands = Rules.Or(rules).Flight.Carry;
        return carry >= bands.TripleFt ? PlayKind.Triple
            : carry >= bands.DoubleFt ? PlayKind.Double
            : PlayKind.Single;
    }

    /// <summary>Just inside the fence, where the glove plants for a wall leap (fielding.wallPlant).</summary>
    public static (double X, double Z) WallPlant(FieldingPreview pre, Park? park = null, RulesTable? rules = null)
    {
        var w = Rules.Or(rules).Fielding.WallPlant;
        var x = pre.LandingX;
        var z = pre.LandingZ;
        var dist = Math.Sqrt(x * x + z * z);
        if (dist < 1) return (x, z);
        var spray = Math.Atan2(x, z) * (180.0 / Math.PI);
        var fence = park != null ? AtBatResolver.FenceAt(park, spray) : dist;
        var along = Math.Min(dist, fence) - w.InsideFenceFt;
        if (along < dist * w.MinFraction) along = dist - w.FallbackInsideFt;
        along = Math.Max(w.MinFt, along);
        var s = along / dist;
        return (x * s, z * s);
    }

    /// <summary>The ring: the landing mark, or the plant inside the wall when the ball meets the fence first (§6.1).</summary>
    public static (double X, double Z) ChaseTarget(FieldingPreview pre, Park? park = null, RulesTable? rules = null)
    {
        var atWall = NeedsJump(pre) || pre.Class == BattedBallClass.Wall;
        var raw = atWall ? WallPlant(pre, park, rules) : (X: pre.LandingX, Z: pre.LandingZ);
        return park == null ? raw : FieldBounds.Clamp(park, raw.X, raw.Z);
    }

    /// <summary>
    /// Fly sits on the glove. Homer rises with the ball, then the wall.
    /// Seat count must not change the beat.
    /// </summary>
    public static PlayCamera.Beat LiveBeat(
        AtBatResult hit,
        FieldingPreview? pre,
        double hitT,
        double hangSec,
        bool caught,
        RulesTable? rules = null)
    {
        var shape = pre?.Class ?? BattedBallClasses.ByLaunch(hit.LaunchDeg, hit.ExitVeloMph, rules);
        if (shape.OnTheDirt())
            return hit.SprayDeg < -8 ? PlayCamera.Beat.GrounderPull : PlayCamera.Beat.Grounder;
        if (shape == BattedBallClass.Liner) return PlayCamera.Beat.Line;
        if (pre != null && NeedsJump(pre))
        {
            if (caught || SitOnWall(hitT, hangSec, rules)) return PlayCamera.Beat.Wall;
            return PlayCamera.Beat.Homer;
        }
        return PlayCamera.Beat.Fly;
    }

    public static string LiveShot(
        AtBatResult hit,
        FieldingPreview? pre,
        double hitT,
        double hangSec,
        bool caught,
        int seats = 1,
        RulesTable? rules = null) =>
        PlayCamera.Shot(LiveBeat(hit, pre, hitT, hangSec, caught, rules), seats);
}
