namespace GrandSluggers.Sim;

/// <summary>
/// Timed fly / wall catch. CPU dead-stick still uses <see cref="FieldingResolver.Resolve"/>.
/// Player with the glove owns the jump. Super Jump / Grow / Clamber widen the window,
/// they do not skip it. Camera: <see cref="PlayCamera.Beat.Fly"/> / Homer / Wall.
/// Harbor wall only — no extra parks, no Nintendo mesh.
/// </summary>
public static class FlyCatch
{
    public const double WindowBeforeSec = 0.48;
    public const double WindowAfterSec = 0.14;
    public const double WallSitSec = 1.15;
    public const double JumpBallY = 2.2;
    public const double WallBallY = 4.5;

    public static bool IsFly(FieldingPreview pre) => !pre.Grounder && !pre.Line;

    /// <summary>Would-be homer: jump in the window at the wall. South does not scoop a rob.</summary>
    public static bool NeedsJump(FieldingPreview pre) =>
        IsFly(pre) && pre.HomeRunLikely;

    /// <summary>
    /// Super Jump / Grow / Clamber add seconds, not an auto-rob.
    /// Harbor has no climb wall, so Clamber is zero there.
    /// </summary>
    public static double ExtraWindowSec(Character? fielder, Park? park)
    {
        if (fielder is null) return 0;
        var extra = 0.0;
        if (fielder.FieldAbility.Equals("super-jump", StringComparison.OrdinalIgnoreCase))
            extra += 0.16;
        if (fielder.FieldAbility.Equals("grow", StringComparison.OrdinalIgnoreCase)
            || fielder.FieldAbility.Equals("lick-catch", StringComparison.OrdinalIgnoreCase))
            extra += 0.08;
        if (park != null && ParkHazards.CanClamber(park, fielder))
            extra += 0.12;
        return extra;
    }

    public static bool JumpWindow(double hitT, double hangSec, Character? fielder = null, Park? park = null)
    {
        var extra = ExtraWindowSec(fielder, park);
        return hitT >= hangSec - (WindowBeforeSec + extra)
               && hitT <= hangSec + (WindowAfterSec + extra * 0.5);
    }

    public static bool SitOnWall(double hitT, double hangSec) =>
        hitT >= hangSec - WallSitSec;

    public static bool HighEnough(double ballY, bool wall) =>
        ballY > (wall ? WallBallY : JumpBallY);

    public static bool Under(
        double gloveX,
        double gloveZ,
        double ballX,
        double ballZ,
        double plantX,
        double plantZ,
        double windowFt,
        bool needsJump)
    {
        var tx = needsJump ? plantX : ballX;
        var tz = needsJump ? plantZ : ballZ;
        var reach = needsJump ? Math.Max(windowFt, 22) : windowFt;
        return Diamond.Dist(gloveX, gloveZ, tx, tz) < reach;
    }

    /// <summary>
    /// One frame of stick-owned input. Jump must land in the window.
    /// South scoops a routine fly you are under — not a rob.
    /// </summary>
    public static bool PlayerCaught(
        bool jumpDown,
        bool southDown,
        bool under,
        bool inWindow,
        bool needsJump) =>
        (jumpDown && inWindow && under) || (southDown && under && !needsJump);

    public static PlayKind PlayerKind(bool caught, FieldingPreview pre, AtBatResult? hit)
    {
        if (caught)
            return pre.Grounder ? PlayKind.GroundOut : PlayKind.FlyOut;
        if (pre.HomeRunLikely)
            return PlayKind.HomeRun;
        var carry = hit?.CarryFt ?? 0;
        return carry >= 330 ? PlayKind.Triple
            : carry >= 250 ? PlayKind.Double
            : PlayKind.Single;
    }

    /// <summary>Just inside the fence, where the glove plants for a wall leap.</summary>
    public static (double X, double Z) WallPlant(FieldingPreview pre, Park? park = null)
    {
        var x = pre.LandingX;
        var z = pre.LandingZ;
        var dist = Math.Sqrt(x * x + z * z);
        if (dist < 1) return (x, z);
        var spray = Math.Atan2(x, z) * (180.0 / Math.PI);
        var fence = park != null ? AtBatResolver.FenceAt(park, spray) : dist;
        var along = Math.Min(dist, fence) - 8;
        if (along < dist * 0.45) along = dist - 10;
        along = Math.Max(8, along);
        var s = along / dist;
        return (x * s, z * s);
    }

    public static (double X, double Z) ChaseTarget(FieldingPreview pre, Park? park = null) =>
        NeedsJump(pre) ? WallPlant(pre, park) : (pre.LandingX, pre.LandingZ);

    /// <summary>
    /// Fly sits on the glove. Homer rises with the ball, then the wall.
    /// Seat count must not change the beat.
    /// </summary>
    public static PlayCamera.Beat LiveBeat(
        AtBatResult hit,
        FieldingPreview? pre,
        double hitT,
        double hangSec,
        bool caught)
    {
        if (FieldingResolver.IsGrounder(hit))
            return hit.SprayDeg < -8 ? PlayCamera.Beat.GrounderPull : PlayCamera.Beat.Grounder;
        if (FieldingResolver.IsLine(hit)) return PlayCamera.Beat.Line;
        if (pre != null && NeedsJump(pre))
        {
            if (caught || SitOnWall(hitT, hangSec)) return PlayCamera.Beat.Wall;
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
        int seats = 1) =>
        PlayCamera.Shot(LiveBeat(hit, pre, hitT, hangSec, caught), seats);
}
