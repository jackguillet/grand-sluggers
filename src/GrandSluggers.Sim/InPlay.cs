namespace GrandSluggers.Sim;

/// <summary>Live ball: energy, bobble, throws to bags, forces, tags, and Time. Runners are bodies (<see cref="Runner"/>).</summary>
public static class InPlay
{
    /// <summary>
    /// A dead ball ends without glove possession or a bag dwell: a homer once it has crossed
    /// (<paramref name="deadAtSeconds"/> is the fence crossing) or a foul once the untouched
    /// path's verdict is in (where it landed past the bags, rolled foul, rested, or left the
    /// field). The spectacle beat (flight.deadBall) holds the camera on it first. A caught ball
    /// must instead finish through the ordinary live-play rules.
    /// </summary>
    public static bool DeadBallResultReady(PlayKind kind, double elapsed, double deadAtSeconds,
        bool caught, bool throwing, bool effectInFlight, RulesTable? rules = null)
    {
        var dead = Rules.Or(rules).Flight.DeadBall;
        return HasDeadBallResult(kind) && !caught && !throwing && !effectInFlight
            && elapsed >= Math.Max(dead.MinSec, deadAtSeconds + dead.AfterHangSec);
    }

    /// <summary>The play ends by the ball, not by a glove: a home run, or a foul nobody caught (§7.10, §7.11).</summary>
    public static bool HasDeadBallResult(PlayKind kind) => kind is PlayKind.HomeRun or PlayKind.Foul;

    public static double Energy(AtBatResult hit, RulesTable? rules = null)
    {
        var quality = Rules.Or(rules).Batting.Quality;
        var q = hit.Quality switch
        {
            ContactQuality.Perfect => quality.PerfectEnergyMul,
            ContactQuality.Nice => quality.NiceEnergyMul,
            ContactQuality.Sour => quality.SourEnergyMul,
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
    /// <param name="runnerAt">Live play time the runner touched the bag.</param>
    public static bool CloseSafe(double arrivedAt, double runnerAt, RulesTable? rules = null) =>
        arrivedAt >= runnerAt && arrivedAt - runnerAt <= Rules.Or(rules).Running.Close.MarginSec;

    /// <summary>Named camera for the contact type. One table: <see cref="PlayCamera"/>.</summary>
    public static string TheaterShot(AtBatResult hit) => PlayCamera.FromHit(hit);

    /// <summary>
    /// The one throw clock (spec §8.5, fielding.throw): release plus distance over the arm. It flies
    /// the live ball and judges the bag, for every arm on the field including the catcher's gun;
    /// the runner bodies race it (§9.1). <paramref name="thr"/> carries arm × chemistry × ability.
    /// </summary>
    public static double ThrowSec(double distFt, ThrowResult? thr, RulesTable? rules = null)
    {
        var t = Rules.Or(rules).Fielding.Throw;
        var fps = t.BaseFtPerSec * (thr?.SpeedMul ?? 1);
        return t.ReleaseSec + distFt / Math.Max(t.MinFtPerSec, fps);
    }

    /// <summary>The thrower's arm (§8.5): <c>armBase + Field × armPerField</c>.</summary>
    public static double ArmMul(Character who, RulesTable? rules = null)
    {
        var t = Rules.Or(rules).Fielding.Throw;
        return Math.Max(0.1, t.ArmBase + who.Stats.Field * t.ArmPerField);
    }

    /// <summary>The CPU fielder's delay between gaining the ball and throwing it (§8.8): <c>throwBaseSec − Field × throwPerFieldSec</c>, × the difficulty's reaction multiplier.</summary>
    public static double ThrowReactionSec(Character who, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        var re = r.Fielding.Reaction;
        return Math.Max(re.ThrowMinSec, re.ThrowBaseSec - who.Stats.Field * re.ThrowPerFieldSec) * r.Cpu.Active.ReactionMul;
    }

    /// <summary>A throw is caught when it lands inside the cover radius of its receiver (§8.5, fielding.cover.radiusFt).</summary>
    public static bool ThrowCaught(double landingX, double landingZ, double receiverX, double receiverZ, RulesTable? rules = null) =>
        Diamond.Dist(landingX, landingZ, receiverX, receiverZ) <= Rules.Or(rules).Fielding.Cover.RadiusFt;

    /// <summary>Where a throw released at (fromX, fromZ) toward (toX, toZ) lands with a signed lateral miss (feet to the thrower's right of the line).</summary>
    public static (double X, double Z) ThrowLanding(double fromX, double fromZ, double toX, double toZ, double lateralFt)
    {
        var dx = toX - fromX;
        var dz = toZ - fromZ;
        var len = Math.Sqrt(dx * dx + dz * dz);
        if (len < 1e-6) return (toX, toZ);
        // Perpendicular to the throw line, to the thrower's right.
        var px = dz / len;
        var pz = -dx / len;
        return (toX + px * lateralFt, toZ + pz * lateralFt);
    }

    /// <summary>
    /// Who covers each bag this play (§8.7): 1B covers first (2B when 1B is the glove), 2B / SS cover
    /// second (whichever is not the glove; with both free the one away from the ball's side), 3B
    /// third, C home, and P backfills any bag whose cover is the glove. The glove never covers.
    /// </summary>
    public static Dictionary<int, string> CoverMap(string glovePos, double ballX)
    {
        var map = new Dictionary<int, string>();
        string Pick(params string[] order)
        {
            foreach (var pos in order)
                if (pos != glovePos) return pos;
            return "";
        }
        map[1] = Pick("1B", "2B", "P");
        map[2] = glovePos == "SS" ? Pick("2B", "P")
            : glovePos == "2B" ? Pick("SS", "P")
            : ballX > 0 ? "SS" : "2B";
        map[3] = Pick("3B", "SS", "P");
        map[4] = Pick("C", "P");
        return map;
    }

    /// <summary>
    /// The cutoff for a throw from (fromX, fromZ) to (toX, toZ) (§8.7): the infielder nearest the
    /// line between them who is neither the glove nor the bag's cover, and their spot on the line.
    /// Null when the throw is short enough that nobody stands between.
    /// </summary>
    public static (string Pos, double X, double Z)? CutoffFor(
        double fromX, double fromZ, double toX, double toZ,
        IReadOnlyDictionary<string, (double X, double Z)> spots, string glovePos, string coverPos)
    {
        var dx = toX - fromX;
        var dz = toZ - fromZ;
        var len2 = dx * dx + dz * dz;
        if (len2 < 1e-6) return null;
        (string Pos, double X, double Z)? best = null;
        var bestD = double.MaxValue;
        foreach (var pos in new[] { "SS", "2B", "1B", "3B" })
        {
            if (pos == glovePos || pos == coverPos) continue;
            if (!spots.TryGetValue(pos, out var at)) continue;
            var t = ((at.X - fromX) * dx + (at.Z - fromZ) * dz) / len2;
            if (t is < 0.15 or > 0.85) continue;
            var lx = fromX + dx * t;
            var lz = fromZ + dz * t;
            var d = Diamond.Dist(at.X, at.Z, lx, lz);
            if (d < bestD)
            {
                bestD = d;
                best = (pos, lx, lz);
            }
        }
        return best;
    }

    /// <summary>The backup spot for a throw: on its line, <paramref name="backupFt"/> beyond the target (§8.7).</summary>
    public static (double X, double Z) BackupSpot(double fromX, double fromZ, double toX, double toZ, double backupFt)
    {
        var dx = toX - fromX;
        var dz = toZ - fromZ;
        var len = Math.Sqrt(dx * dx + dz * dz);
        if (len < 1e-6) return (toX, toZ);
        return (toX + dx / len * backupFt, toZ + dz / len * backupFt);
    }

    /// <summary>
    /// The body that backs up a throw to <paramref name="bag"/> (§8.7): the pitcher behind first
    /// and home, the outfielder nearest the backup spot behind second and third. Never the glove or
    /// the cover.
    /// </summary>
    public static string BackupPos(int bag, double spotX, double spotZ,
        IReadOnlyDictionary<string, (double X, double Z)> spots, string glovePos, string coverPos)
    {
        if (bag is 1 or 4)
            return glovePos != "P" && coverPos != "P" ? "P" : "";
        var best = "";
        var bestD = double.MaxValue;
        foreach (var pos in FieldingResolver.OutfieldPursuitPositions)
        {
            if (pos == glovePos || pos == coverPos || !spots.TryGetValue(pos, out var at)) continue;
            var d = Diamond.Dist(at.X, at.Z, spotX, spotZ);
            if (d < bestD)
            {
                bestD = d;
                best = pos;
            }
        }
        return best;
    }

    /// <summary>Seconds until a throw released now from (x, z) lands at <paramref name="bag"/>: <see cref="ThrowSec"/> over that distance.</summary>
    public static double ThrowArrivalSec(double fromX, double fromZ, int bag, ThrowResult? thr, RulesTable? rules = null)
    {
        var to = Diamond.Bag(bag);
        return ThrowSec(Diamond.Dist(fromX, fromZ, to.X, to.Z), thr, rules);
    }

    /// <summary>
    /// Bags to throw in order on a hopper. Force at second, then first when the batter is out.
    /// With first empty, the tag bag (home or third) only when that runner is going (§8.8 rules
    /// 2–3, S-36); otherwise first. Empty when the batter already beat the play and nobody is running.
    /// </summary>
    public static int[] GroundThrowBags(bool firstOccupied, bool batterBeatsThrow) =>
        GroundThrowBags(firstOccupied, false, false, batterBeatsThrow);

    public static int[] GroundThrowBags(bool firstOccupied, bool secondGoing, bool thirdGoing, bool batterBeatsThrow)
    {
        if (firstOccupied)
            return batterBeatsThrow ? [2] : [2, 1];
        if (thirdGoing) return batterBeatsThrow ? [4] : [4, 1];
        if (secondGoing) return batterBeatsThrow ? [3] : [3, 1];
        return batterBeatsThrow ? [] : [1];
    }

    /// <summary>Default hopper throw: second when first is occupied, else the bag a runner is going for, else first.</summary>
    public static int DefaultGroundBag(bool firstOccupied, bool secondGoing = false, bool thirdGoing = false)
    {
        var bags = GroundThrowBags(firstOccupied, secondGoing, thirdGoing, batterBeatsThrow: false);
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
        TagRunner,
        /// <summary>A runner off the bag at the catch, forced back at their start bag (§10.5).</summary>
        DoubledOff,
        /// <summary>A throw to a bag with nobody to play on (S-34): the caption names it; nothing is decided.</summary>
        Wasted
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
        ThrowVerdict.ForceOut or ThrowVerdict.DoubledOff => OutType.Force,
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
            ThrowVerdict.DoubledOff => $"{fielderName} doubles {runnerName ?? "the runner"} off{(bag == 1 ? " first" : bag == 2 ? " second" : where)}.",
            ThrowVerdict.Wasted => $"{fielderName} throws to {BagName(bag)} with nobody to play on.",
            _ => ""
        };
    }

    public static string BagName(int bag) => bag switch
    {
        1 => "first",
        2 => "second",
        3 => "third",
        4 => "home",
        _ => "the cutoff"
    };

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
    /// Time (spec §10.6): three outs; or the ball held unthrown by a fielder on the infield (inside
    /// the dirt / grass lip, flight.classes.infieldLipFt) while every live runner has stood on a bag
    /// for running.bags.timeOnBagSec. A runner still moving keeps the play alive; an out or a run
    /// is not a live runner. Picking up the ball is not Time.
    /// </summary>
    public static bool Time(
        bool hasBall,
        bool throwing,
        int outs,
        bool heldInInfield,
        IEnumerable<Runner> runners,
        RulesTable? rules = null)
    {
        if (outs >= 3) return true;
        if (!hasBall || throwing || !heldInInfield) return false;
        var onBagSec = Rules.Or(rules).Running.Bags.TimeOnBagSec;
        foreach (var r in runners)
        {
            if (!r.Live) continue;
            if (!r.OnBag || r.OnBagSec + 1e-9 < onBagSec) return false;
        }
        return true;
    }

    /// <summary>The ball is held on the infield: inside the dirt / grass lip (flight.classes.infieldLipFt), where 2B and SS stand.</summary>
    public static bool HeldInInfield(double gloveX, double gloveZ, RulesTable? rules = null) =>
        !FieldingResolver.OutfieldGrass(gloveX, gloveZ, rules);

    /// <summary>
    /// The tag reach of this glove (§10.3): running.bags.tagReachFt, plus the Lick / Grow bonus,
    /// less the slide cut when the runner is sliding (§9.4).
    /// </summary>
    public static double TagReachFt(Character? fielder, bool sliding = false, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        var bags = r.Running.Bags;
        return bags.TagReachFt + FieldAbilities.TagReachBonus(fielder, r) - (sliding ? bags.SlideReachCutFt : 0);
    }

    /// <summary>
    /// Glove with the ball touches a runner (running.bags.tagReachFt): body contact, not a force at
    /// the bag. A runner standing inside tagSafeRadiusFt of a bag is touching it and safe; tighter
    /// than the Time occupy so a step off is a tag.
    /// </summary>
    public static bool Touches(
        bool hasBall,
        bool throwing,
        double gloveX,
        double gloveZ,
        double runnerX,
        double runnerZ,
        bool? runnerOnBag = null,
        RulesTable? rules = null,
        bool sliding = false,
        Character? fielder = null)
    {
        if (!hasBall || throwing) return false;
        var bags = Rules.Or(rules).Running.Bags;
        var onBag = runnerOnBag ?? OccupyingBag(runnerX, runnerZ, bags.TagSafeRadiusFt);
        if (onBag) return false;
        return Diamond.Dist(gloveX, gloveZ, runnerX, runnerZ) < TagReachFt(fielder, sliding, rules);
    }

    /// <summary>
    /// The tag inside one frame (§10.2, §10.3): the glove held the ball through the frame while the
    /// runner's body moved from <c>prev</c> to <c>now</c>; the first point on that step inside the
    /// reach and off every bag is the tag, even when the body ends the frame on the bag. Returns
    /// the fraction of the frame at which it landed, or −1 when the body never came into reach
    /// off a bag. Tie goes to the runner: a body that is on the bag at the same point is safe.
    /// </summary>
    public static double TagWithinFrame(
        (double X, double Z) glovePrev, (double X, double Z) gloveNow,
        (double X, double Z) runnerPrev, (double X, double Z) runnerNow,
        double reachFt, bool homeIsABag, RulesTable? rules = null, int steps = 12)
    {
        var safe = Rules.Or(rules).Running.Bags.TagSafeRadiusFt;
        for (var i = 1; i <= steps; i++)
        {
            var u = (double)i / steps;
            var gx = glovePrev.X + (gloveNow.X - glovePrev.X) * u;
            var gz = glovePrev.Z + (gloveNow.Z - glovePrev.Z) * u;
            var rx = runnerPrev.X + (runnerNow.X - runnerPrev.X) * u;
            var rz = runnerPrev.Z + (runnerNow.Z - runnerPrev.Z) * u;
            var onBag = homeIsABag ? OccupyingBag(rx, rz, safe) : OccupyingNonHomeBag(rx, rz, safe);
            if (onBag) return -1;
            if (Diamond.Dist(gx, gz, rx, rz) < reachFt) return u;
        }
        return -1;
    }

    /// <summary>Inside a bag's safe radius of first, second, or third; the plate is not a bag for the batter leaving the box (§10.3).</summary>
    public static bool OccupyingNonHomeBag(double x, double z, double radius)
    {
        for (var bag = 1; bag <= 3; bag++)
        {
            var p = Diamond.Bag(bag);
            if (Diamond.Dist(x, z, p.X, p.Z) <= radius) return true;
        }
        return false;
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
}
