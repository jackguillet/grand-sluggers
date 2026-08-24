namespace GrandSluggers.Sim;

/// <summary>Live ball: energy, bobble, and the race from home to first.</summary>
public static class InPlay
{
    public static double Energy(AtBatResult hit)
    {
        var q = hit.Quality switch
        {
            ContactQuality.Perfect => 1.25,
            ContactQuality.Solid => 1.0,
            ContactQuality.Cheap => 0.55,
            _ => 0
        };
        return hit.ExitVeloMph * q;
    }

    public static double KnockbackSec(double energy, Character? fielder)
    {
        if (energy < 72 || fielder is null) return 0;
        var w = (11 - fielder.Stats.Field) * 0.045;
        return Math.Clamp((energy - 72) / 90 * w, 0, 0.55);
    }

    public static bool Bobbles(double energy, Character fielder, Random rng, GloveItem? glove = null)
    {
        if (energy < 78) return false;
        var hands = fielder.Stats.Field + (glove?.ErrorReduction ?? 0) * 3;
        var chance = Math.Clamp((energy - 78) / 110.0 * (11 - hands) * 0.08, 0, 0.5);
        return rng.NextDouble() < chance;
    }

    public static double HomeToFirstSec(Character batter, double dash01 = 0)
    {
        var run = Math.Clamp(4.32 - batter.Stats.Run * 0.13, 2.9, 4.35);
        var dash = Math.Clamp(dash01, 0, 1);
        return Math.Max(2.45, run * (1 - 0.12 * dash));
    }

    /// <summary>Named camera for the contact type. JSON shots must exist under this id.</summary>
    public static string TheaterShot(AtBatResult hit)
    {
        if (!string.IsNullOrEmpty(hit.StarSwingUsed)) return "smash";
        if (hit.HomeRun) return "diamond-homer";
        if (FieldingResolver.IsGrounder(hit))
            return hit.SprayDeg < -8 ? "diamond-pull" : "diamond-grounder";
        if (FieldingResolver.IsLine(hit)) return "diamond-line";
        return "diamond";
    }

    public static double ThrowSec(double distFt, ThrowResult? thr)
    {
        var fps = 56 * (thr?.SpeedMul ?? 1);
        return 0.22 + distFt / Math.Max(32, fps);
    }

    /// <summary>True if the batter reaches first before the throw after a scoop at the landing.</summary>
    public static bool BatterBeatsThrow(Character batter, AtBatResult hit, FieldingResult field)
    {
        if (field.Kind != PlayKind.GroundOut || field.Fielder is null) return false;
        var run = HomeToFirstSec(batter);
        var already = field.HangTimeSec;
        var left = run - already;
        if (left <= 0) return true;
        var dist = Diamond.Dist(field.LandingX, field.LandingZ, Diamond.First.X, Diamond.First.Z);
        var tThrow = ThrowSec(dist, field.Throw) + KnockbackSec(Energy(hit), field.Fielder);
        return left < tThrow;
    }

    public static double BagToBagSec(Character runner) =>
        Math.Clamp(3.55 - runner.Stats.Run * 0.12, 2.45, 3.65);

    /// <summary>Lead non-force runner's next bag: home if third is on, else third if second is on.</summary>
    public static int TagBag(bool secondOccupied, bool thirdOccupied)
    {
        if (thirdOccupied) return 4;
        if (secondOccupied) return 3;
        return 0;
    }

    /// <summary>True if the runner reaches <paramref name="toBag"/> before the throw from the scoop.</summary>
    public static bool RunnerBeatsTag(Character runner, AtBatResult hit, FieldingResult field, int toBag)
    {
        if (field.Kind != PlayKind.GroundOut || field.Fielder is null || toBag <= 0) return false;
        var run = BagToBagSec(runner);
        var already = field.HangTimeSec;
        var left = run - already;
        if (left <= 0) return true;
        var dest = Diamond.Bag(toBag);
        var dist = Diamond.Dist(field.LandingX, field.LandingZ, dest.X, dest.Z);
        var tThrow = ThrowSec(dist, field.Throw) + KnockbackSec(Energy(hit), field.Fielder);
        return left < tThrow;
    }

    /// <summary>
    /// Bags to throw in order on a hopper. Force at second, then first when the batter is out.
    /// With first empty, throw to the tag bag (home or third). Empty when the batter already beat
    /// the play and nobody is in scoring position.
    /// </summary>
    public static int[] GroundThrowBags(bool firstOccupied, bool batterBeatsThrow) =>
        GroundThrowBags(firstOccupied, false, false, batterBeatsThrow);

    public static int[] GroundThrowBags(bool firstOccupied, bool secondOccupied, bool thirdOccupied, bool batterBeatsThrow)
    {
        if (firstOccupied)
            return batterBeatsThrow ? [2] : [2, 1];
        if (thirdOccupied) return [4];
        if (secondOccupied) return [3];
        return batterBeatsThrow ? [] : [1];
    }

    /// <summary>Pad stick / arrows name a bag only when you are not chasing the ball.</summary>
    public static bool StickNamesBag(bool chasing, bool caught) => !chasing || caught;

    /// <summary>Right 1B, up 2B, left 3B, down home. Dead stick is 0.</summary>
    public static int DiamondBag(double x, double y, double mag2 = 0.55)
    {
        if (x * x + y * y < mag2) return 0;
        if (Math.Abs(x) > Math.Abs(y)) return x > 0 ? 1 : 3;
        return y > 0 ? 2 : 4;
    }

    /// <summary>
    /// Keys (1–4 / d-pad) always arm. Stick / arrows only when <paramref name="stickOk"/>.
    /// Chasing WASD must not arm a throw.
    /// </summary>
    public static int ArmedBag(int keysBag, int stickBag, bool stickOk)
    {
        if (keysBag is >= 1 and <= 4) return keysBag;
        if (stickOk && stickBag is >= 1 and <= 4) return stickBag;
        return 0;
    }

    /// <summary>
    /// Hopper catch with no direction throws to first. Cutoff with no direction is a relay (0),
    /// not a random bag. A named bag always wins.
    /// </summary>
    public static int CommitBag(int armed, bool hopperCaught, bool cutoff)
    {
        if (armed is >= 1 and <= 4) return armed;
        if (cutoff) return 0;
        if (hopperCaught) return 1;
        return 0;
    }

    public static bool FairContactSendsBatter(AtBatResult hit) =>
        hit.InPlay && !hit.Foul;
}
