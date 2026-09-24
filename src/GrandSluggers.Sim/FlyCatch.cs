namespace GrandSluggers.Sim;

/// <summary>
/// Timed fly / wall catch. CPU dead-stick still uses <see cref="FieldingResolver.Resolve"/>.
/// Player with the glove owns the jump. Super Jump / Grow / Clamber widen the window,
/// they do not skip it. Camera: <see cref="PlayCamera.Beat.Line"/> / Fly / Homer / Wall.
/// Harbor wall only — no extra parks, no Nintendo mesh.
/// </summary>
public static class FlyCatch
{
    public static bool IsFly(FieldingPreview pre) => pre.Class.IsFlyShape();

    /// <summary>
    /// The flight leaves the field in the air — over the fence, or a foul over a rail or the
    /// backstop: only a leap in the window at that wall takes it (§8.4). South does not scoop a rob.
    /// </summary>
    public static bool NeedsJump(FieldingPreview pre) =>
        pre.HomeRunLikely || (pre.Foul && pre.Ball is { LeavesInTheAir: true });

    /// <summary>
    /// The rob height (§8.4, fielding.catch.*RobFt): a leap takes a ball clearing the fence by at
    /// most the glove's reach over it — plain jump, Super Jump, Clamber on a climb wall, or a
    /// buddy jump. A ball higher than that is gone whatever the window says.
    /// </summary>
    public static double RobHeightFt(Character? fielder, Park? park, RulesTable rules, bool buddy = false)
    {
        var c = rules.Fielding.Catch;
        var reach = c.JumpRobFt;
        if (buddy) reach = Math.Max(reach, c.BuddyJumpRobFt);
        if (fielder is null) return reach;
        if (fielder.FieldAbility.Equals("super-jump", StringComparison.OrdinalIgnoreCase))
            reach = Math.Max(reach, c.SuperJumpRobFt);
        if (park != null && ParkHazards.CanClamber(park, fielder, rules))
            reach = Math.Max(reach, c.ClamberRobFt);
        return reach;
    }

    /// <summary>Can this glove's leap reach a ball clearing the fence by <paramref name="clearFt"/>?</summary>
    public static bool CanRob(double clearFt, Character? fielder, Park? park, RulesTable rules, bool buddy = false) =>
        !double.IsNaN(clearFt) && clearFt <= RobHeightFt(fielder, park, rules, buddy);

    /// <summary>Both outfielders must plant together and meet the live ball overhead before it leaves play.</summary>
    public static bool BuddyInPosition(FieldingPreview pre, Park park,
        double gloveX, double gloveZ, double buddyX, double buddyZ,
        double ballX, double ballY, double ballZ, double hitT, double hangSec, RulesTable rules)
    {
        var r = rules;
        var c = r.Fielding.Catch;
        var plant = ChaseTarget(pre, r, park);
        return FieldingResolver.BuddyJumpOffered(pre) && hitT < hangSec
            && (pre.Ball?.LeavesT is not double leaves || hitT < leaves)
            && JumpWindow(hitT, hangSec, r, pre.Fielder, park)
            && HighEnough(ballY, true, r)
            && ballY <= AtBatResolver.FenceSpotAt(park, FieldBounds.SprayDeg(plant.X, plant.Z)).TopFt + c.BuddyJumpRobFt
            && CanRob(pre.Ball?.FenceClearFt ?? double.NaN, pre.Fielder, park, r, true)
            && Diamond.Dist(gloveX, gloveZ, plant.X, plant.Z) < c.BuddyPlantFt
            && Diamond.Dist(buddyX, buddyZ, plant.X, plant.Z) < c.BuddyPlantFt
            && Diamond.Dist(gloveX, gloveZ, ballX, ballZ) < c.BuddyPlantFt
            && Diamond.Dist(buddyX, buddyZ, ballX, ballZ) < c.BuddyPlantFt;
    }

    /// <summary>
    /// Super Jump / Grow / Clamber add seconds, not an auto-rob.
    /// Harbor has no climb wall, so Clamber is zero there.
    /// </summary>
    public static double ExtraWindowSec(Character? fielder, Park? park, RulesTable rules)
    {
        if (fielder is null) return 0;
        var c = rules.Fielding.Catch;
        var extra = 0.0;
        if (fielder.FieldAbility.Equals("super-jump", StringComparison.OrdinalIgnoreCase))
            extra += c.SuperJumpWindowSec;
        if (fielder.FieldAbility.Equals("grow", StringComparison.OrdinalIgnoreCase)
            || fielder.FieldAbility.Equals("lick-catch", StringComparison.OrdinalIgnoreCase))
            extra += c.GrowWindowSec;
        if (park != null && ParkHazards.CanClamber(park, fielder, rules))
            extra += c.ClamberWindowSec;
        return extra;
    }

    /// <summary>[hang − windowBefore − extra, hang + windowAfter + extra/2] (fielding.catch).</summary>
    public static bool JumpWindow(double hitT, double hangSec, RulesTable rules, Character? fielder = null, Park? park = null)
    {
        var c = rules.Fielding.Catch;
        var extra = ExtraWindowSec(fielder, park, rules);
        return hitT >= hangSec - (c.WindowBeforeSec + extra)
               && hitT <= hangSec + (c.WindowAfterSec + extra * 0.5);
    }

    public static bool SitOnWall(double hitT, double hangSec, RulesTable rules) =>
        hitT >= hangSec - rules.Fielding.Catch.WallSitSec;

    public static bool HighEnough(double ballY, bool wall, RulesTable rules)
    {
        var c = rules.Fielding.Catch;
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
        RulesTable rules)
    {
        _ = ballX;
        _ = ballZ;
        // The landing ring is the stand-up catch (§8.3, #669). Live XZ while the
        // ball is still up is the home-first miss — standing in the circle was a drop.
        var reach = needsJump ? Math.Max(windowFt, rules.Fielding.Catch.NeedsJumpReachFt) : windowFt;
        return Diamond.Dist(gloveX, gloveZ, plantX, plantZ) < reach;
    }

    /// <summary>
    /// Stick-owned catch. Jump is the leap while it is armed, not only the
    /// press frame. Ordinary catches need position and eligibility, never a button.
    /// southDown remains a compatibility argument for recordings and has no catch effect.
    /// </summary>
    public static bool PlayerCaught(
        bool jumpDown,
        bool southDown,
        bool under,
        bool inWindow,
        bool needsJump,
        bool canRob = true,
        bool linerInAir = false) =>
        (jumpDown && inWindow && under && (!needsJump || canRob))
        || AutoCatch(under, inWindow, needsJump, canRob: false, linerInAir: linerInAir);

    /// <summary>
    /// East dive at the rim (#669): past the stand-up ring, inside dive reach, ball low
    /// enough. A plant catch is stand-up even if East is armed.
    /// </summary>
    public static bool PlayerDiveCatch(
        bool diveArmed,
        double distFt,
        double standUpFt,
        double diveWindowFt,
        double ballY,
        RulesTable rules) =>
        diveArmed && NeedsDive(distFt, standUpFt, diveWindowFt, ballY, rules);

    /// <summary>Past the stand-up ring, inside dive reach, ball below <c>diveMaxBallY</c>.</summary>
    public static bool NeedsDive(
        double distFt,
        double standUpFt,
        double diveWindowFt,
        double ballY,
        RulesTable rules) =>
        distFt >= standUpFt
        && distFt < diveWindowFt
        && ballY < rules.Fielding.Catch.DiveMaxBallY;

    /// <summary>
    /// Dead-stick / CPU: under a routine fly in the window is a stand-up catch. Not a rob unless
    /// <paramref name="canRob"/>. A liner on the glove before the bounce is a catch even
    /// outside the hang window (§7.6): the intercept is the window. Only a human press can use dive reach.
    /// </summary>
    public static bool AutoCatch(bool under, bool inWindow, bool needsJump, bool canRob = false, bool linerInAir = false) =>
        under && (!needsJump || canRob) && (inWindow || linerInAir);

    /// <summary>
    /// Ordinary catches require the live ball to meet the glove's horizontal reach and
    /// standing height plus its actual jump rise before the first surface contact.
    /// Wall robs use the wall plant and their separate timed leap window.
    /// </summary>
    /// <summary>
    /// Off the bat (§7.11): the batted ball is this far (3-D) from where it left the bat
    /// (<c>fielding.catch.offTheBatFt</c>), or down on the ground. Before that no glove takes it — a ball straight
    /// from the bat into the catcher's mitt is a foul tip, not a catch — so a pop behind the plate is caught coming down.
    /// </summary>
    public static bool OffTheBat(IReadOnlyList<Sample> path, double ballX, double ballY, double ballZ, RulesTable rules)
    {
        var c = rules.Fielding.Catch;
        if (ballY <= c.InAirMinY || path.Count == 0) return true;
        var o = path[0];
        var dx = ballX - o.X;
        var dy = ballY - o.Height;
        var dz = ballZ - o.Z;
        return dx * dx + dy * dy + dz * dz >= c.OffTheBatFt * c.OffTheBatFt;
    }

    public static bool InPosition(
        FieldingPreview pre,
        double gloveX,
        double gloveZ,
        double ballX,
        double ballZ,
        double ballY,
        double plantX,
        double plantZ,
        double windowFt,
        double hitT,
        double hangSec,
        bool needsJump,
        RulesTable rules,
        double gloveRiseFt = 0)
    {
        // A catch requires the untouched ball to meet the glove in three dimensions.
        // The projected landing and the hit's label cannot award possession.
        if (needsJump)
            return Under(gloveX, gloveZ, ballX, ballZ, plantX, plantZ, windowFt, true, rules);
        if (hitT >= hangSec) return false;
        var c = rules.Fielding.Catch;
        return ballY >= 0 && ballY <= c.StandingHeightFt + Math.Max(0, gloveRiseFt)
            && Diamond.Dist(gloveX, gloveZ, ballX, ballZ) < windowFt;
    }

    /// <summary>
    /// Hopper on the dirt: if the glove can touch the ball, they scoop.
    /// No South. No stick. A fly still in the air is not a pickup.
    /// </summary>
    public static bool TouchScoop(double distFt, double windowFt, double ballY, RulesTable rules) =>
        distFt < windowFt && ballY < rules.Fielding.Catch.TouchScoopY;

    /// <summary>A dirt pickup cannot reach through the wall or replace an aerial catch.</summary>
    public static bool TouchScoop(FieldingPreview pre, Park park, double ballX, double ballZ,
        double ballY, double hitT, double hangSec, double distFt, double windowFt, RulesTable rules) =>
        PickupInPlay(pre, park, ballX, ballZ, hitT, hangSec, rules)
        && TouchScoop(distFt, windowFt, ballY, rules);

    /// <summary>
    /// Shared eligibility for automatic, button and diving dirt pickups: a roller, or any ball once it
    /// has landed. A liner in the air is a catch in the short window (§7.6, §8.3), never a scoop.
    /// </summary>
    public static bool PickupInPlay(FieldingPreview pre, Park park, double ballX, double ballZ,
        double hitT, double hangSec, RulesTable rules) =>
        (hitT >= hangSec)
        && FieldBounds.InPark(park, ballX, ballZ);

    /// <summary>
    /// What the ball has decided so far. <paramref name="foul"/> is the fair / foul call: the
    /// untouched path's verdict until a touch decides it (§5.6). <paramref name="inAir"/> is
    /// before the first ground contact (the landing mark), not the chase-height flag. A fly or
    /// liner held then is an out wherever it was (§7.6, §7.11); an uncaught homer flight is a
    /// home run at the crossing; a foul is dead; anything else is <see cref="PlayKind.InPlay"/>
    /// until the bodies name it at Complete.
    /// </summary>
    public static PlayKind PlayerKind(bool caught, FieldingPreview pre, bool inAir = true, bool? foul = null)
    {
        var isFoul = foul ?? pre.Foul;
        if (caught && inAir) return PlayKind.FlyOut;
        if (isFoul) return PlayKind.Foul;
        if (pre.HomeRunLikely && !caught) return PlayKind.HomeRun;
        return PlayKind.InPlay;
    }

    /// <summary>Just inside the fence, where the glove plants for a wall leap (fielding.wallPlant).</summary>
    public static (double X, double Z) WallPlant(FieldingPreview pre, RulesTable rules, Park? park = null)
    {
        var w = rules.Fielding.WallPlant;
        var x = pre.LandingX;
        var z = pre.LandingZ;
        var dist = Math.Sqrt(x * x + z * z);
        if (dist < 1) return (x, z);
        var spray = Math.Atan2(x, z) * (180.0 / Math.PI);
        var fence = park != null ? AtBatResolver.FenceAt(park, spray) : dist;
        // A two-body boost plants at the legal wall edge so the live ball can pass overhead.
        var inside = FieldingResolver.BuddyJumpOffered(pre)
            ? rules.Fielding.Chase.WallClearanceFt : w.InsideFenceFt;
        var along = Math.Min(dist, fence) - inside;
        if (along < dist * w.MinFraction) along = dist - w.FallbackInsideFt;
        along = Math.Max(w.MinFt, along);
        var s = along / dist;
        return (x * s, z * s);
    }

    /// <summary>The ring: the landing mark, or the plant inside the wall when the ball meets the fence first (§6.1).</summary>
    public static (double X, double Z) ChaseTarget(FieldingPreview pre, RulesTable rules, Park? park = null)
    {
        var atWall = NeedsJump(pre) || pre.Class == BattedBallClass.Wall;
        var raw = atWall ? WallPlant(pre, rules, park) : (X: pre.LandingX, Z: pre.LandingZ);
        return park == null ? raw : FieldingResolver.BuddyJumpOffered(pre)
            ? FieldBounds.ClampFielder(park, raw.X, raw.Z, rules)
            : FieldBounds.Clamp(park, raw.X, raw.Z);
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
        RulesTable rules)
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
        RulesTable rules,
        int seats = 1) =>
        PlayCamera.Shot(LiveBeat(hit, pre, hitT, hangSec, caught, rules), seats);
}
