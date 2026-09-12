namespace GrandSluggers.Sim;

/// <summary>Live ball: energy, bobble, and the race from home to first.</summary>
public static class InPlay
{
    /// <summary>
    /// An uncaught ball leaving the park ends without glove possession or a bag dwell.
    /// Preserve the existing flight/spectacle beat, including the wall-catch window;
    /// a caught ball must instead finish through the ordinary live-play rules.
    /// </summary>
    public static bool DeadBallResultReady(PlayKind kind, double elapsed, double hangSeconds,
        bool caught, bool throwing, bool effectInFlight, RulesTable? rules = null)
    {
        var dead = Rules.Or(rules).Flight.DeadBall;
        return HasDeadBallResult(kind) && !caught && !throwing && !effectInFlight
            && elapsed >= Math.Max(dead.MinSec, hangSeconds + dead.AfterHangSec);
    }

    public static bool HasDeadBallResult(PlayKind kind) => kind == PlayKind.HomeRun;

    public static double Energy(AtBatResult hit, RulesTable? rules = null)
    {
        var quality = Rules.Or(rules).Batting.Quality;
        var q = hit.Quality switch
        {
            ContactQuality.Perfect => quality.PerfectEnergyMul,
            ContactQuality.Solid => quality.SolidEnergyMul,
            ContactQuality.Cheap => quality.CheapEnergyMul,
            _ => 0
        };
        return hit.ExitVeloMph * q;
    }

    public static double KnockbackSec(double energy, Character? fielder, RulesTable? rules = null)
    {
        var k = Rules.Or(rules).Fielding.Knockback;
        if (energy < k.MinEnergy || fielder is null) return 0;
        var w = (11 - fielder.Stats.Field) * k.SecPerFieldDeficit;
        return Math.Clamp((energy - k.MinEnergy) / k.EnergySpan * w, 0, k.MaxSec);
    }

    public static bool Bobbles(double energy, Character fielder, Random rng, GloveItem? glove = null, RulesTable? rules = null)
    {
        var b = Rules.Or(rules).Fielding.Bobble;
        if (energy < b.MinEnergy) return false;
        var hands = fielder.Stats.Field + (glove?.ErrorReduction ?? 0) * b.HandsPerGloveReduction;
        var chance = Math.Clamp((energy - b.MinEnergy) / b.EnergySpan * (11 - hands) * b.ChancePerHands, 0, b.MaxChance);
        return rng.NextDouble() < chance;
    }

    /// <summary>Bang-bang: the throw arrived and the runner got there first by a step (running.close.marginSec).</summary>
    /// <param name="arrivedAt">Live play time when the throw (or mash) lands.</param>
    /// <param name="needed">Run time to the bag.</param>
    public static bool CloseSafe(double arrivedAt, double needed, RulesTable? rules = null) =>
        arrivedAt >= needed && arrivedAt - needed <= Rules.Or(rules).Running.Close.MarginSec;

    public static double HomeToFirstSec(Character batter, double dash01 = 0, RulesTable? rules = null)
    {
        var h = Rules.Or(rules).Running.HomeToFirst;
        var run = Math.Clamp(h.BaseSec - batter.Stats.Run * h.SecPerRun, h.MinSec, h.MaxSec);
        var dash = Math.Clamp(dash01, 0, 1);
        return Math.Max(h.FloorSec, run * (1 - h.DashMul * dash));
    }

    /// <summary>Named camera for the contact type. One table: <see cref="PlayCamera"/>.</summary>
    public static string TheaterShot(AtBatResult hit) => PlayCamera.FromHit(hit);

    /// <summary>The verdict clock of a fielder's throw (fielding.throw). §8.5 makes it the only throw clock (P4).</summary>
    public static double ThrowSec(double distFt, ThrowResult? thr, RulesTable? rules = null)
    {
        var t = Rules.Or(rules).Fielding.Throw;
        var fps = t.BaseFtPerSec * (thr?.SpeedMul ?? 1);
        return t.ReleaseSec + distFt / Math.Max(t.MinFtPerSec, fps);
    }

    /// <summary>
    /// The live flight clock the client plays for a thrown ball, as shipped (fielding.throw.flight*).
    /// A second formula next to <see cref="ThrowSec"/>; P4 collapses them (spec A.4 #40).
    /// </summary>
    public static double ThrowFlightSec(ThrowResult? thr, RulesTable? rules = null)
    {
        var t = Rules.Or(rules).Fielding.Throw;
        var mul = Math.Max(t.FlightMinMul, thr?.SpeedMul ?? 1);
        return Math.Clamp(t.FlightBaseSec / mul, t.FlightMinSec, t.FlightMaxSec);
    }

    /// <summary>True if the batter reaches first before the throw after a scoop at the landing.</summary>
    public static bool BatterBeatsThrow(Character batter, AtBatResult hit, FieldingResult field, double dash01 = 0, RulesTable? rules = null)
    {
        if (field.Kind != PlayKind.GroundOut || field.Fielder is null) return false;
        var run = HomeToFirstSec(batter, dash01, rules);
        var already = field.HangTimeSec;
        var left = run - already;
        if (left <= 0) return true;
        var dist = Diamond.Dist(field.LandingX, field.LandingZ, Diamond.First.X, Diamond.First.Z);
        var tThrow = ThrowSec(dist, field.Throw, rules) + KnockbackSec(Energy(hit, rules), field.Fielder, rules);
        return left < tThrow;
    }

    public static double BagToBagSec(Character runner, RulesTable? rules = null)
    {
        var b = Rules.Or(rules).Running.BagToBag;
        return Math.Clamp(b.BaseSec - runner.Stats.Run * b.SecPerRun, b.MinSec, b.MaxSec);
    }

    /// <summary>Lead non-force runner's next bag: home if third is on, else third if second is on.</summary>
    public static int TagBag(bool secondOccupied, bool thirdOccupied)
    {
        if (thirdOccupied) return 4;
        if (secondOccupied) return 3;
        return 0;
    }

    /// <summary>True if the runner reaches <paramref name="toBag"/> before the throw from the scoop.</summary>
    public static bool RunnerBeatsTag(Character runner, AtBatResult hit, FieldingResult field, int toBag, RulesTable? rules = null)
    {
        if (field.Kind != PlayKind.GroundOut || field.Fielder is null || toBag <= 0) return false;
        var run = BagToBagSec(runner, rules);
        var already = field.HangTimeSec;
        var left = run - already;
        if (left <= 0) return true;
        var dest = Diamond.Bag(toBag);
        var dist = Diamond.Dist(field.LandingX, field.LandingZ, dest.X, dest.Z);
        var tThrow = ThrowSec(dist, field.Throw, rules) + KnockbackSec(Energy(hit, rules), field.Fielder, rules);
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
    /// Force at contact. Batter is always forced to first. Second when first is
    /// occupied, third when first and second, home when the bases are loaded.
    /// Putting out a trailing runner removes forces ahead.
    /// </summary>
    public readonly record struct ForceState(bool Batter, bool Second, bool Third, bool Home)
    {
        public static ForceState Empty { get; } = new(true, false, false, false);

        public static ForceState FromOccupancy(bool first, bool second, bool third) =>
            new(true, first, first && second, first && second && third);

        public bool At(int bag) => bag switch
        {
            1 => Batter,
            2 => Second,
            3 => Third,
            4 => Home,
            _ => false
        };

        public ForceState AfterOutAt(int bag) => bag switch
        {
            1 => new(false, false, false, false),
            2 => new(Batter, false, false, false),
            3 => new(Batter, Second, false, false),
            4 => new(Batter, Second, Third, false),
            _ => this
        };

        /// <summary>Bag the forced runner started on. 0 is the batter.</summary>
        public static int FromBag(int toBag) => toBag switch
        {
            2 => 1,
            3 => 2,
            4 => 3,
            _ => 0
        };
    }

    /// <summary>What one throw to a bag decided. The caption is narrated from this, never read back.</summary>
    public enum ThrowVerdict
    {
        None,
        /// <summary>Batter beat the relay to first after the force was recorded at second.</summary>
        BatterSafeAfterForce,
        /// <summary>The forced runner beat the throw to second.</summary>
        BeatForce,
        /// <summary>Batter beat an unforced throw to first; nothing to narrate.</summary>
        BatterBeat,
        /// <summary>The runner beat the throw at a bag.</summary>
        Beat,
        /// <summary>Second out of the turn, at first.</summary>
        TurnedTwo,
        /// <summary>The batter thrown out at first.</summary>
        OutAtFirst,
        /// <summary>A forced runner put out at a bag.</summary>
        ForceOut,
        /// <summary>An unforced runner tagged at a bag.</summary>
        TagOut,
        /// <summary>A live body tag away from a bag (named runner).</summary>
        TagRunner
    }

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
        string Caption,
        ThrowVerdict Verdict = ThrowVerdict.None)
    {
        /// <summary>The out this step recorded, when <see cref="Out"/>.</summary>
        public OutType OutType => OutTypeOf(Verdict);
    }

    public static OutType OutTypeOf(ThrowVerdict verdict) => verdict switch
    {
        ThrowVerdict.ForceOut => OutType.Force,
        ThrowVerdict.TagOut or ThrowVerdict.TagRunner => OutType.Tag,
        _ => OutType.ThrowOutAtFirst
    };

    /// <summary>Whether the narration of this verdict already places the batter at first.</summary>
    public static bool NarratesBatterAtFirst(ThrowVerdict verdict) => verdict == ThrowVerdict.BatterSafeAfterForce;

    /// <summary>The caption for a verdict. Produced from the typed facts, last; nothing reads it back.</summary>
    public static string Narrate(ThrowVerdict verdict, int bag, string? fielderName, string? batterName, string? runnerName = null)
    {
        fielderName ??= "";
        batterName ??= "";
        var where = bag == 3 ? " at third" : bag == 4 ? " at home" : "";
        return verdict switch
        {
            ThrowVerdict.BatterSafeAfterForce => $"Force at second. {batterName} in at first.",
            ThrowVerdict.BeatForce or ThrowVerdict.Beat => $"{batterName} beats the throw.",
            ThrowVerdict.TurnedTwo => $"{fielderName} turns two.",
            ThrowVerdict.OutAtFirst => $"{fielderName} to first.",
            ThrowVerdict.ForceOut => $"{fielderName} forces the runner{where}.",
            ThrowVerdict.TagOut => $"{fielderName} tags the runner{where}.",
            ThrowVerdict.TagRunner => $"{fielderName} tags {runnerName ?? "the runner"}.",
            _ => ""
        };
    }

    static GroundThrowStep Step(
        ThrowVerdict verdict, int bag, bool @out, bool force, bool turnedTwo, bool batterSafe, bool playOver,
        int nextDefaultBag, string? fielderName, string? batterName) =>
        new(bag, @out, force, turnedTwo, batterSafe, playOver, nextDefaultBag,
            Narrate(verdict, bag, fielderName, batterName), verdict);

    /// <summary>
    /// Pure baseball for one throw to a bag. Match applies it; the director decides when.
    /// Force vs tag comes from <paramref name="force"/> at contact, not live occupancy.
    /// </summary>
    public static GroundThrowStep ThrowToBag(
        int bag,
        bool firstOccupied,
        bool alreadyForced,
        bool runnerBeats,
        int outs,
        string? fielderName,
        string? batterName) =>
        ThrowToBag(
            bag,
            alreadyForced
                ? ForceState.FromOccupancy(true, false, false).AfterOutAt(2)
                : ForceState.FromOccupancy(firstOccupied, false, false),
            runnerPresent: true,
            runnerBeats,
            outs,
            alreadyForced,
            fielderName,
            batterName);

    public static GroundThrowStep ThrowToBag(
        int bag,
        ForceState force,
        bool runnerPresent,
        bool runnerBeats,
        int outs,
        bool alreadyForced,
        string? fielderName,
        string? batterName)
    {
        fielderName ??= "";
        batterName ??= "";
        if (outs >= 3)
            return Step(ThrowVerdict.None, bag, false, alreadyForced, false, false, true, 0, fielderName, batterName);
        if (bag is < 1 or > 4)
            return Step(ThrowVerdict.None, bag, false, alreadyForced, false, false, false, 0, fielderName, batterName);

        var isForce = force.At(bag);
        if (!isForce && !runnerPresent)
            return Step(ThrowVerdict.None, bag, false, alreadyForced, false, false, false, 0, fielderName, batterName);

        if (runnerBeats)
        {
            if (bag == 1 && alreadyForced)
                return Step(ThrowVerdict.BatterSafeAfterForce, bag, false, true, false, true, true, 0, fielderName, batterName);
            if (bag == 2 && isForce)
                return Step(ThrowVerdict.BeatForce, bag, false, false, false, true, true, 0, fielderName, batterName);
            if (bag == 1)
                return Step(ThrowVerdict.BatterBeat, bag, false, false, false, true, false, 0, fielderName, batterName);
            return Step(ThrowVerdict.Beat, bag, false, alreadyForced, false, false, false, 0, fielderName, batterName);
        }

        if (bag == 1 && alreadyForced)
            return Step(ThrowVerdict.TurnedTwo, bag, true, true, true, false, true, 0, fielderName, batterName);

        var outsAfter = outs + 1;
        var over = outsAfter >= 3;
        if (isForce)
            return Step(
                bag == 1 ? ThrowVerdict.OutAtFirst : ThrowVerdict.ForceOut,
                bag, true, bag != 1, false, false, over, NextBagAfterForce(bag, outsAfter), fielderName, batterName);

        if (bag == 1)
            return Step(ThrowVerdict.OutAtFirst, bag, true, false, false, false, over, 0, fielderName, batterName);

        return Step(ThrowVerdict.TagOut, bag, true, false, false, false, over, 0, fielderName, batterName);
    }

    /// <summary>
    /// Stick runs the glove. D-pad / keys arm a bag. After the catch the stick
    /// still runs — it does not steal the throw.
    /// </summary>
    public static bool StickNamesBag(bool chasing, bool caught) => !chasing && !caught;

    /// <summary>Right 1B, up 2B, left 3B, down home. Dead stick is 0 (running.stick.diamondDeadMag2).</summary>
    public static int DiamondBag(double x, double y, double? mag2 = null, RulesTable? rules = null)
    {
        var dead = mag2 ?? Rules.Or(rules).Running.Stick.DiamondDeadMag2;
        if (x * x + y * y < dead) return 0;
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
    /// Otherwise every live runner has occupied a bag for running.bags.timeOnBagSec.
    /// Picking up the ball is not Time — the batter is still live until the out.
    /// </summary>
    public readonly record struct Occupy(bool OnBag, double Sec);

    public static bool Time(
        bool hasBall,
        bool throwing,
        int outs,
        Occupy batter,
        Occupy? first = null,
        Occupy? second = null,
        Occupy? third = null,
        bool batterOut = false,
        RulesTable? rules = null)
    {
        if (outs >= 3) return true;
        if (!hasBall || throwing) return false;
        var onBagSec = Rules.Or(rules).Running.Bags.TimeOnBagSec;
        if (!batterOut && !Settled(batter, onBagSec)) return false;
        if (first is { } a && !Settled(a, onBagSec)) return false;
        if (second is { } b && !Settled(b, onBagSec)) return false;
        if (third is { } c && !Settled(c, onBagSec)) return false;
        return true;
    }

    /// <summary>Still racing or awarded a bag. An out is not a live runner.</summary>
    public static bool LiveBatter(PlayKind kind, bool putOut) =>
        BatterDestBag(kind) > 0 && !putOut;

    static bool Settled(Occupy o, double onBagSec) => o.OnBag && o.Sec + 1e-9 >= onBagSec;

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
        PlayKind.HitByPitch => 1,
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

    /// <summary>
    /// Glove with the ball touches a runner (running.bags.tagReachFt). Toy bodies read big from
    /// the diamond camera — this is body contact, not a force at the bag. A runner standing
    /// inside tagSafeRadiusFt of a bag is safe; tighter than the Time occupy so a step off is a tag.
    /// </summary>
    public static bool Touches(
        bool hasBall,
        bool throwing,
        double gloveX,
        double gloveZ,
        double runnerX,
        double runnerZ,
        bool? runnerOnBag = null,
        RulesTable? rules = null)
    {
        if (!hasBall || throwing) return false;
        var bags = Rules.Or(rules).Running.Bags;
        var onBag = runnerOnBag ?? OccupyingBag(runnerX, runnerZ, bags.TagSafeRadiusFt);
        if (onBag) return false;
        return Diamond.Dist(gloveX, gloveZ, runnerX, runnerZ) < bags.TagReachFt;
    }

    /// <summary>Inside a bag's occupy radius (running.bags.occupyRadiusFt) of home or any bag.</summary>
    public static bool OccupyingBag(double x, double z, double? radius = null, RulesTable? rules = null)
    {
        var r = radius ?? Rules.Or(rules).Running.Bags.OccupyRadiusFt;
        if (Diamond.Dist(x, z, 0, 0) <= r) return true;
        for (var bag = 1; bag <= 3; bag++)
        {
            var p = Diamond.Bag(bag);
            if (Diamond.Dist(x, z, p.X, p.Z) <= r) return true;
        }
        return false;
    }

    /// <summary>On this bag only. Home is 4.</summary>
    public static bool OnThisBag(int bag, double x, double z, double? radius = null, RulesTable? rules = null)
    {
        if (bag is < 1 or > 4) return false;
        var p = Diamond.Bag(bag);
        return Diamond.Dist(x, z, p.X, p.Z) <= (radius ?? Rules.Or(rules).Running.Bags.OccupyRadiusFt);
    }

    /// <summary>
    /// Force exists at first (batter), second when first is occupied, third when
    /// first and second, home when the bases are loaded.
    /// </summary>
    public static bool ForceAtBag(int bag, bool firstOccupied, bool secondOccupied, bool thirdOccupied) =>
        bag switch
        {
            1 => true,
            2 => firstOccupied,
            3 => firstOccupied && secondOccupied,
            4 => firstOccupied && secondOccupied && thirdOccupied,
            _ => false
        };

    /// <summary>
    /// Glove has the ball and is on a force bag; the runner is not there yet.
    /// Stepping on first is the out — you do not tag the batter-runner.
    /// Tie on the bag goes to the runner.
    /// </summary>
    public static bool ForceOnBag(
        bool force,
        int bag,
        bool hasBall,
        bool throwing,
        double gloveX,
        double gloveZ,
        double runnerX,
        double runnerZ,
        RulesTable? rules = null)
    {
        if (!force || !hasBall || throwing) return false;
        var bags = Rules.Or(rules).Running.Bags;
        if (!OnThisBag(bag, gloveX, gloveZ, bags.OccupyRadiusFt)) return false;
        if (OnThisBag(bag, runnerX, runnerZ, bags.TagSafeRadiusFt)) return false;
        return true;
    }

    /// <summary>Feet along home → 1B → 2B → 3B → home. destBag 1..4. fromBag 0 is home.</summary>
    public static (double X, double Z) TowardBag(
        int fromBag, int destBag, double feet, double homeX = 0, double homeZ = 0, RulesTable? rules = null)
    {
        if (destBag <= fromBag)
            return fromBag <= 0 ? (homeX, homeZ) : Diamond.Bag(fromBag);
        var cap = (destBag - fromBag) * Diamond.Baseline;
        feet = Math.Clamp(feet, 0, cap);
        if (feet >= cap - Rules.Or(rules).Running.Bags.SnapFt)
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

    public static (double X, double Z) AlongBases(double feet, int destBag, double startX = 0, double startZ = 0, RulesTable? rules = null) =>
        TowardBag(0, destBag, feet, startX, startZ, rules);

    public static double RunFeet(double elapsed, Character who, double dash01 = 0, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        return elapsed * Diamond.Baseline / Math.Max(r.Running.HomeToFirst.RunFeetMinSec, HomeToFirstSec(who, dash01, r));
    }
}
