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

    /// <summary>Named camera for the contact type. One table: <see cref="PlayCamera"/>.</summary>
    public static string TheaterShot(AtBatResult hit) => PlayCamera.FromHit(hit);

    public static double ThrowSec(double distFt, ThrowResult? thr)
    {
        var fps = 56 * (thr?.SpeedMul ?? 1);
        return 0.22 + distFt / Math.Max(32, fps);
    }

    /// <summary>True if the batter reaches first before the throw after a scoop at the landing.</summary>
    public static bool BatterBeatsThrow(Character batter, AtBatResult hit, FieldingResult field, double dash01 = 0)
    {
        if (field.Kind != PlayKind.GroundOut || field.Fielder is null) return false;
        var run = HomeToFirstSec(batter, dash01);
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

    /// <summary>Default hopper throw: second when first is occupied, else first / the tag bag.</summary>
    public static int DefaultGroundBag(bool firstOccupied, bool secondOccupied = false, bool thirdOccupied = false)
    {
        var bags = GroundThrowBags(firstOccupied, secondOccupied, thirdOccupied, batterBeatsThrow: false);
        return bags.Length > 0 ? bags[0] : 0;
    }

    /// <summary>After a force at second the next throw is first, unless the inning is over.</summary>
    public static int NextBagAfterForce(int forceBag, int outsAfterForce)
    {
        if (outsAfterForce >= 3) return 0;
        return forceBag == 2 ? 1 : 0;
    }

    /// <summary>Runner on first, fewer than two outs: two throws can turn two.</summary>
    public static bool DoublePlayOffered(bool firstOccupied, int outs) =>
        firstOccupied && outs < 2;

    /// <summary>
    /// One throw of a live double-play race. The director steps this as the ball lands so
    /// outs and the mini diamond update immediately. CPU FinishAtBat applies the same table
    /// for both throws at once. Does not invent a PlayKind — GroundOut stays the contact.
    /// </summary>
    public readonly record struct GroundThrowStep(
        int Bag,
        bool Out,
        bool Force,
        bool TurnedTwo,
        bool BatterSafe,
        bool PlayOver,
        int NextDefaultBag,
        string Caption);

    /// <summary>
    /// Pure baseball for one throw to a bag. Match applies it; the director decides when.
    /// </summary>
    public static GroundThrowStep ThrowToBag(
        int bag,
        bool firstOccupied,
        bool alreadyForced,
        bool runnerBeats,
        int outs,
        string? fielderName,
        string? batterName)
    {
        fielderName ??= "";
        batterName ??= "";
        if (outs >= 3)
            return new(bag, false, alreadyForced, false, false, true, 0, "");

        if (bag == 2 && firstOccupied && !alreadyForced)
        {
            if (runnerBeats)
                return new(bag, false, false, false, true, true, 0, $"{batterName} beats the throw.");
            var outsAfter = outs + 1;
            var over = outsAfter >= 3;
            return new(
                bag,
                Out: true,
                Force: true,
                TurnedTwo: false,
                BatterSafe: false,
                PlayOver: over,
                NextDefaultBag: NextBagAfterForce(2, outsAfter),
                Caption: $"{fielderName} forces the runner.");
        }

        if (bag == 1 && alreadyForced)
        {
            if (runnerBeats)
                return new(
                    bag, false, true, false, true, true, 0,
                    $"Force at second. {batterName} in at first.");
            return new(
                bag, true, true, true, false, true, 0,
                $"{fielderName} turns two.");
        }

        if (bag == 1)
        {
            if (runnerBeats)
                return new(bag, false, false, false, true, false, 0, "");
            var outsAfter = outs + 1;
            return new(
                bag, true, false, false, false, outsAfter >= 3, 0,
                $"{fielderName} to first.");
        }

        return new(bag, false, alreadyForced, false, false, false, 0, "");
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
    /// Hopper catch with no direction throws to the default bag (first, or second when first
    /// is occupied). Cutoff with no direction is a relay (0), not a random bag. A named bag always wins.
    /// </summary>
    public static int CommitBag(int armed, bool hopperCaught, bool cutoff) =>
        CommitBag(armed, hopperCaught, cutoff, defaultBag: 1);

    public static int CommitBag(int armed, bool hopperCaught, bool cutoff, int defaultBag)
    {
        if (armed is >= 1 and <= 4) return armed;
        if (cutoff) return 0;
        if (hopperCaught) return defaultBag is >= 1 and <= 4 ? defaultBag : 1;
        return 0;
    }

    public static bool FairContactSendsBatter(AtBatResult hit) =>
        hit.InPlay && !hit.Foul;

    /// <summary>
    /// Time. The glove has the ball, nobody is throwing.
    /// Three outs end it now. A putout with no remaining live runners ends it now.
    /// Otherwise every live runner has occupied a bag for <see cref="TimeOnBagSec"/>.
    /// Picking up the ball is not Time — the batter is still live until the out.
    /// </summary>
    public const double TimeOnBagSec = 1.0;

    public readonly record struct Occupy(bool OnBag, double Sec);

    public static bool Time(
        bool hasBall,
        bool throwing,
        int outs,
        Occupy batter,
        Occupy? first = null,
        Occupy? second = null,
        Occupy? third = null,
        bool batterOut = false)
    {
        if (outs >= 3) return true;
        if (!hasBall || throwing) return false;
        if (!batterOut && !Settled(batter)) return false;
        if (first is { } a && !Settled(a)) return false;
        if (second is { } b && !Settled(b)) return false;
        if (third is { } c && !Settled(c)) return false;
        return true;
    }

    /// <summary>Still racing or awarded a bag. An out is not a live runner.</summary>
    public static bool LiveBatter(PlayKind kind, bool putOut) =>
        BatterDestBag(kind) > 0 && !putOut;

    static bool Settled(Occupy o) => o.OnBag && o.Sec + 1e-9 >= TimeOnBagSec;

    public static Occupy TickOccupy(bool onBag, double sec, double dt) =>
        onBag ? new Occupy(true, sec + dt) : new Occupy(false, 0);

    /// <summary>Bags the batter is awarded. 0 = out (not running as a runner).</summary>
    public static int BatterDestBag(PlayKind kind) => kind switch
    {
        PlayKind.HomeRun => 4,
        PlayKind.Triple => 3,
        PlayKind.Double => 2,
        PlayKind.Single => 1,
        PlayKind.Walk => 1,
        PlayKind.GroundOut => 1,
        _ => 0
    };

    /// <summary>Occupied runner's dest on that contact. 4 = scores. Tag-up leaves on the catch.</summary>
    public static int OccupiedDestBag(int fromBag, PlayKind kind, bool tagUp = false, bool caught = false)
    {
        if (kind == PlayKind.FlyOut && tagUp && caught && fromBag is >= 1 and <= 3)
            return fromBag >= 3 ? 4 : fromBag + 1;
        var extra = BatterDestBag(kind);
        if (extra <= 0) return fromBag;
        var dest = fromBag + extra;
        return dest > 4 ? 4 : dest;
    }

    public const double OccupyRadiusFt = 6;

    public static bool OccupyingBag(double x, double z, double radius = OccupyRadiusFt)
    {
        if (Diamond.Dist(x, z, 0, 0) <= radius) return true;
        for (var bag = 1; bag <= 3; bag++)
        {
            var p = Diamond.Bag(bag);
            if (Diamond.Dist(x, z, p.X, p.Z) <= radius) return true;
        }
        return false;
    }

    /// <summary>Feet along home → 1B → 2B → 3B → home. destBag 1..4. fromBag 0 is home.</summary>
    public static (double X, double Z) TowardBag(
        int fromBag, int destBag, double feet, double homeX = 0, double homeZ = 0)
    {
        if (destBag <= fromBag)
            return fromBag <= 0 ? (homeX, homeZ) : Diamond.Bag(fromBag);
        var cap = (destBag - fromBag) * Diamond.Baseline;
        feet = Math.Clamp(feet, 0, cap);
        if (feet >= cap - 0.5)
        {
            var end = destBag >= 4 ? Diamond.Home : Diamond.Bag(destBag);
            return (end.X, end.Z);
        }
        var seg = (int)(feet / Diamond.Baseline);
        var u = (feet - seg * Diamond.Baseline) / Diamond.Baseline;
        var a = fromBag + seg;
        var from = a <= 0 ? (X: homeX, Z: homeZ) : Diamond.Bag(a);
        var to = Diamond.Bag(a + 1);
        return (from.X + (to.X - from.X) * u, from.Z + (to.Z - from.Z) * u);
    }

    public static (double X, double Z) AlongBases(double feet, int destBag, double startX = 0, double startZ = 0) =>
        TowardBag(0, destBag, feet, startX, startZ);

    public static double RunFeet(double elapsed, Character who, double dash01 = 0) =>
        elapsed * Diamond.Baseline / Math.Max(0.4, HomeToFirstSec(who, dash01));
}
