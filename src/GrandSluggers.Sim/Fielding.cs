namespace GrandSluggers.Sim;

public sealed class FieldingResolver
{
    public static readonly IReadOnlyList<string> InfieldPursuitPositions = ["P", "C", "1B", "2B", "3B", "SS"];
    public static readonly IReadOnlyList<string> AirPursuitPositions = ["LF", "CF", "RF", "SS", "2B"];
    /// <summary>A pop: the corners and the catcher join the air pool in their sector; the P never takes one if anyone else can (§7.7, §8.2).</summary>
    public static readonly IReadOnlyList<string> PopPursuitPositions = ["LF", "CF", "RF", "SS", "2B", "1B", "3B", "C"];
    public static readonly IReadOnlyList<string> OutfieldPursuitPositions = ["LF", "CF", "RF"];
    /// <summary>A foul flight near the lines (§7.11).</summary>
    public static readonly IReadOnlyList<string> FoulPursuitPositions = ["C", "1B", "3B", "LF", "RF"];
    /// <summary>A bunt (§7.3): the pitcher, the catcher and the crashing corners; the middle infielders cover first and second (<see cref="BuntDefense"/>).</summary>
    public static readonly IReadOnlyList<string> BuntPursuitPositions = ["P", "C", "1B", "3B"];

    /// <summary>The pursuit pool for a class (§8.2): dirt and ropes to the infield, a bunt to its four, pops to the air pool plus the corners, flies to the air pool.</summary>
    public static IReadOnlyList<string> PursuitPool(BattedBallClass shape, bool foul)
    {
        if (foul) return FoulPursuitPositions;
        if (shape == BattedBallClass.Bunt) return BuntPursuitPositions;
        if (shape.OnTheDirt() || shape == BattedBallClass.Liner) return InfieldPursuitPositions;
        return shape == BattedBallClass.Pop ? PopPursuitPositions : AirPursuitPositions;
    }

    /// <summary>The infield pool for a ball on the dirt by its shape (§8.2): the bunt's four, else the six.</summary>
    public static IReadOnlyList<string> InfieldPool(BattedBallClass shape) =>
        shape == BattedBallClass.Bunt ? BuntPursuitPositions : InfieldPursuitPositions;

    readonly ChemistryTable _chem;
    readonly RulesTable _rules;

    public FieldingResolver(ChemistryTable chem, RulesTable rules)
    {
        _chem = chem;
        _rules = rules;
    }

    public FieldingPreview Preview(
        AtBatResult hit,
        Park park,
        IReadOnlyList<Character> defense,
        Character pitcher,
        Random rng,
        bool night = false,
        IReadOnlyDictionary<string, Character>? gloves = null,
        IReadOnlyDictionary<string, (double X, double Z)>? at = null)
    {
        // One flight for the preview, the ring, and the homer call (§5.6): the clipped path in this park.
        var ball = BattedBall.Of(hit, park, _rules);
        var samples = ball.Samples;
        var hang = ball.HangT;
        var landing = (X: ball.LandingX, Z: ball.LandingZ);
        var shape = ball.Shape;
        var grounder = shape.OnTheDirt();
        var assigned = Assign(defense, pitcher, gloves);
        var seed = new FieldingPreview(
            pitcher, "P", null, hang, landing.X, landing.Z, shape, false, false, false, 10, Foul: ball.Foul, Ball: ball);
        var pursuit = FieldingPursuit.Choose(
            assigned,
            PursuitPool(shape, ball.Foul),
            seed,
            park,
            samples, _rules,
            at,
            readyAt: CpuReactionLockouts(_rules, grounder ? null : hang, hit.Class == BattedBallClass.Bunt));
        var fielder = pursuit.Fielder;
        var pos = pursuit.Position;
        // A park's redirects act on the live ball (F4-c, FR-07): the preview plans the path as hit and nothing is foreseen.
        var warped = false;
        var buddy = Buddy(assigned, seed with
        {
            Fielder = fielder, Position = pos, Frozen = hit.StarSwingUsed == "heart-swing"
        }, park, samples, at);
        // The heart swing's slow (a special, §13; outside D21 and the 3e boundary): every chaser for the play, exactly as it
        // shipped. A park's status volume is not read here any more (F4-b, #896, FR-07): it slows the body that touches it,
        // live (BodySlows), and nothing is decided from where the ball lands.
        var freeze = hit.StarSwingUsed == "heart-swing";
        if (grounder && hit.StarSwingUsed is "shell-swing" or "cask-swing" && rng.NextDouble() < _rules.Fielding.Park.ShellWarpChance)
            warped = true;
        var radius = CatchRadiusFt(fielder, park, _rules, air: !grounder);
        var heat = hit.StarPitchUsed is "heatball" or "caskball";
        var furnace = hit.StarSwingUsed is "furnace" or "heat-swing";
        return new FieldingPreview(
            fielder, pos, buddy, hang, landing.X, landing.Z, shape,
            heat, furnace, freeze, radius, warped, Foul: ball.Foul, Ball: ball);
    }

    /// <summary>
    /// What the batted ball decides on its own, before any glove (§7): a homer at the crossing, a
    /// foul on the untouched path. Everything else is <see cref="PlayKind.InPlay"/>:
    /// the live ball's gloves, throws and runner bodies decide it, never a roll (§8.3, §8.8, A.4 #37).
    /// </summary>
    public FieldingResult Resolve(
        AtBatResult hit,
        Park park,
        IReadOnlyList<Character> defense,
        Character pitcher,
        Random rng,
        GloveItem? glove = null,
        FieldingPreview? pre = null,
        bool night = false,
        IReadOnlyDictionary<string, Character>? gloves = null)
    {
        _ = glove;
        var shown = pre ?? Preview(hit, park, defense, pitcher, rng, night, gloves);
        var ball = shown.Ball ?? BattedBall.Of(hit, park, _rules);
        var kind = shown.HomeRunLikely ? PlayKind.HomeRun
            : shown.Foul ? PlayKind.Foul
            : PlayKind.InPlay;
        return new FieldingResult(
            kind, shown.Fielder, null, shown.HangTimeSec, shown.LandingX, shown.LandingZ, shown.Heatball, shown.Furnace,
            Buddy: shown.Buddy, Warped: shown.Warped, GroundRule: ball.GroundRule);
    }

    /// <summary>The resolved catch verb, kept as a fact so copy can change without changing behavior.</summary>
    public static DefensiveFeat CatchFeat(FieldingPreview shown, AtBatResult hit, Park park, RulesTable rules)
    {
        _ = hit;
        if (BuddyJumpOffered(shown))
            return DefensiveFeat.BuddyJump;
        if (!shown.HomeRunLikely)
            return DefensiveFeat.None;
        if (ParkHazards.CanClamber(park, shown.Fielder, rules))
            return DefensiveFeat.Clamber;
        if (shown.Fielder.FieldAbility == FieldAbilityId.SuperJump)
            return DefensiveFeat.SuperJump;
        return DefensiveFeat.None;
    }

    /// <summary>The live glove verb the player actually completed on this catch.</summary>
    /// <summary>
    /// The feat the glove made the catch with (§8.4), typed for the outcome and the stamp (§15):
    /// the buddy jump, a wall rob by ability, a plain jump in the window, or a dive.
    /// </summary>
    public static DefensiveFeat PlayerCatchFeat(FieldingPreview shown, Park park, RulesTable rules, bool buddyJump, bool jumped, bool dived = false)
    {
        if (buddyJump)
            return DefensiveFeat.BuddyJump;
        if (jumped && shown.HomeRunLikely)
        {
            if (ParkHazards.CanClamber(park, shown.Fielder, rules))
                return DefensiveFeat.Clamber;
            if (shown.Fielder.FieldAbility == FieldAbilityId.SuperJump)
                return DefensiveFeat.SuperJump;
        }
        if (jumped) return DefensiveFeat.Jump;
        if (dived) return DefensiveFeat.Dive;
        return DefensiveFeat.None;
    }

    /// <summary>Closest glove to (x, z) among all nine. Pass live spots when fielders have moved.</summary>
    public static (Character Fielder, string Pos) NearestGlove(
        IReadOnlyList<Character> defense, Character pitcher, double x, double z, RulesTable rules) =>
        NearestGlove(Assign(defense, pitcher), x, z, rules);

    public static (Character Fielder, string Pos) NearestGlove(
        IReadOnlyDictionary<string, Character> assigned,
        double x,
        double z,
        RulesTable rules,
        IReadOnlyDictionary<string, (double X, double Z)>? at = null)
    {
        var starts = DiamondGeometry.Of(rules).Positions;
        Character? best = null;
        var bestPos = "P";
        var bestD = double.MaxValue;
        foreach (var kv in assigned)
        {
            var p = at != null && at.TryGetValue(kv.Key, out var live)
                ? live
                : starts[kv.Key];
            var d = Diamond.Dist(p.X, p.Z, x, z);
            if (d < bestD)
            {
                bestD = d;
                best = kv.Value;
                bestPos = kv.Key;
            }
        }
        return (best ?? assigned.Values.First(), bestPos);
    }

    /// <summary>
    /// Dirt scoop / armed-verb window (fielding.catch). A fly stand-up is the catch radius
    /// itself (#669); <c>windowPadFt</c> is scoop slack, not a stand-up fly out.
    /// </summary>
    public static double CatchWindowFt(double catchRadius, bool dive, bool jump, RulesTable rules)
    {
        var c = rules.Fielding.Catch;
        var w = catchRadius + c.WindowPadFt;
        if (dive) w += c.DiveReachFt;
        if (jump) w += c.JumpReachFt;
        return w;
    }

    /// <summary>The yellow ring / stand-up fly catch (#669): catch radius, no pad.</summary>
    public static double StandUpCatchFt(double catchRadius) => catchRadius;

    /// <summary>What this body's dive costs (F693-02-dive-recovery-cost, #719): <c>catch.diveRecoverySec</c> cut by <c>diveRecoveryFieldCut</c> of itself per Hands point above 1 (recovery is handling). 0 on the shipped table.</summary>
    public static double DiveRecoverySec(Character who, RulesTable rules)
    {
        var c = rules.Fielding.Catch;
        if (c.DiveRecoverySec <= 0) return 0;
        return c.DiveRecoverySec * Math.Max(0, 1 - c.DiveRecoveryFieldCut * (who.Stats.Hands - 1));
    }

    /// <summary>
    /// The speed severity of a take (F693-02-recoil-severity-curve, #720): 0 at or below the shared onset, 1 at or above the full
    /// speed, linear between. A catch in the air (<paramref name="airborne"/>) reads the airborne pair
    /// (F693-02-grounded-air-catch-recoil).
    /// </summary>
    public static double RecoilSeverity(double incomingFtPerSec, RulesTable rules, bool airborne = false)
    {
        var r = rules.Fielding.Recoil;
        var (onset, full) = airborne ? (r.AirOnsetFtPerSec, r.AirFullFtPerSec) : (r.OnsetFtPerSec, r.FullFtPerSec);
        if (onset <= 0 || full <= onset) return 0;
        return Math.Clamp((incomingFtPerSec - onset) / (full - onset), 0, 1);
    }

    /// <summary>The Hands factor (F693-02-recoil-field-factors): <c>1 − handsCutPerPoint × (Hands − 1)</c>, never below 0 — 1 / 0.80 / 0.55 at Hands 1 / 5 / 10.</summary>
    public static double RecoilHandsFactor(Character who, RulesTable rules) =>
        Math.Max(0, 1 - rules.Fielding.Recoil.HandsCutPerPoint * (who.Stats.Hands - 1));

    /// <summary>
    /// The one weight <c>w = S × F × K</c> the recovery, the kick and the skid all read (F693-02-recoil-field-shaping: severity
    /// bounded first, then the hands), with <c>K</c> the body class's <c>knockbackMul</c> (§8.1, CH-11): a heavy body is knocked
    /// back less than a light one by the same ball.
    /// </summary>
    public static double RecoilWeight(Character who, double incomingFtPerSec, RulesTable rules, bool airborne = false) =>
        RecoilSeverity(incomingFtPerSec, rules, airborne) * RecoilHandsFactor(who, rules) * BodyClasses.Of(who, rules).KnockbackMul;

    /// <summary>What this take costs these hands: <c>capSec × w</c> — 0.20 / 0.16 / 0.11 s at full severity for Hands 1 / 5 / 10, nothing for a routine arrival, nothing on the shipped table.</summary>
    public static double RecoilSec(Character who, double incomingFtPerSec, RulesTable rules, bool airborne = false) =>
        rules.Fielding.Recoil.CapSec * RecoilWeight(who, incomingFtPerSec, rules, airborne);

    /// <summary>The impact kick's initial speed, <c>kickFtPerSec × w</c> (F693-02-ordinary-recoil-motion-profile).</summary>
    public static double RecoilKickFtPerSec(double weight, RulesTable rules) =>
        rules.Fielding.Recoil.KickFtPerSec * weight;

    /// <summary>
    /// The normalized difficulty of a ground-ball take (F693-02-awkward-hop-difficulty-source, #721): 0 for a roll, a falling ball
    /// (the clean long hop) or a micro-bounce; on a rising ball, φ = y / (y + vy² / 2g) is how far up its hop the ball is, and the
    /// difficulty is 1 at φ = 0.5, falling linearly to 0 <c>hopPhaseHalfWidth</c> either side, scaled from 0 at <c>hopMinApexFt</c>
    /// to 1 at <c>hopFullApexFt</c> of projected apex. 0 whenever the rule is off.
    /// </summary>
    public static double HopDifficulty(double ballY, double ballVy, RulesTable rules)
    {
        var r = rules;
        var h = r.Fielding.Handling;
        if (ballVy <= 0 || ballY <= 1e-9) return 0;
        var apex = ballY + ballVy * ballVy / (2 * r.Flight.Gravity);
        if (apex < h.HopMinApexFt) return 0;
        var phi = ballY / apex;
        var band = Math.Max(0, 1 - Math.Abs(phi - 0.5) / h.HopPhaseHalfWidth);
        var height = h.HopFullApexFt <= h.HopMinApexFt ? 1 : Math.Clamp((apex - h.HopMinApexFt) / (h.HopFullApexFt - h.HopMinApexFt), 0, 1);
        return band * height;
    }

    /// <summary>Normalized handling quality H (F693-02-ordinary-handling-chance-curve): the Hands trait plus the glove's help, 1 → 0 and 10 → 1.</summary>
    public static double HandlingQuality(Character who, RulesTable rules, GloveItem? glove = null)
    {
        var hands = who.Stats.Hands + (glove?.ErrorReduction ?? 0) * rules.Fielding.Bobble.HandsPerGloveReduction;
        return (Math.Clamp(hands, 1, 10) - 1) / 9.0;
    }

    /// <summary>
    /// How squarely the ring met the ball (F693-02-error-outcome-selection under F693-02-arcade-fielding-simplification, #721): 1 with
    /// the ball at the body, 0 at the edge of the take's window — the one contact fact the resolved take has.
    /// </summary>
    public static double Obstruction(double distFt, double windowFt) =>
        windowFt <= 0 ? 1 : Math.Clamp(1 - distFt / windowFt, 0, 1);

    /// <summary>What a continuing deflection keeps of the ball's speed (F693-02-continuing-error-speed-retention): <c>retainMax</c> for a glancing touch, down to <c>retainMin</c> at the knockdown boundary.</summary>
    public static double DeflectionRetention(double obstruction, RulesTable rules)
    {
        var h = rules.Fielding.Handling;
        var c = h.DeflectObstruction <= 0 ? 1 : Math.Clamp(obstruction / h.DeflectObstruction, 0, 1);
        return h.DeflectRetainMax - (h.DeflectRetainMax - h.DeflectRetainMin) * c;
    }

    /// <summary>The failed take carries on rather than dropping at the feet when the touch was glancing and the ball came in hot (F693-02-error-outcome-selection, -expanded-ordinary-error-outcomes): the contact and the speed decide, never a second roll.</summary>
    public static bool DeflectionContinues(double obstruction, double incomingFtPerSec, RulesTable rules)
    {
        var h = rules.Fielding.Handling;
        return obstruction < h.DeflectObstruction && incomingFtPerSec >= h.DeflectMinFtPerSec;
    }

    /// <summary>The accepted curve, <c>p = cap × D × (1 − handsCut × H)</c>: 10 / 6 / 2 % at full difficulty for weak / middle / strong hands, zero for a routine take.</summary>
    public static double HandlingErrorChance(double difficulty, double quality, RulesTable rules)
    {
        var h = rules.Fielding.Handling;
        if (difficulty <= 0) return 0;
        return Math.Clamp(h.ChanceCap * Math.Clamp(difficulty, 0, 1) * (1 - h.HandsCut * Math.Clamp(quality, 0, 1)), 0, h.ChanceCap);
    }

    /// <summary>The skid the kick integrates to over the recovery, <c>K T / 2</c>: <c>w²</c> feet at 10 ft/s and 0.20 s, one foot at most (F693-02-ordinary-recoil-distance-cap).</summary>
    public static double RecoilSkidFt(double weight, RulesTable rules)
    {
        var r = rules.Fielding.Recoil;
        return r.KickFtPerSec * weight * r.CapSec * weight / 2;
    }

    /// <summary>Stand-up plus <c>diveReachFt</c> — the rim. Past this is a drop.</summary>
    public static double DiveCatchFt(double catchRadius, RulesTable rules) =>
        StandUpCatchFt(catchRadius) + rules.Fielding.Catch.DiveReachFt;

    /// <summary>
    /// Base catch radius for a glove (abilities, clamber parks). The stand-up reach is the body class's (§8.1, §8.3): its
    /// <c>flyReachFt</c> on a ball hit in the air (<paramref name="air"/>), its <c>groundReachFt</c> on a ball hit on the ground —
    /// authored in data, never measured off the mesh. A character with no class has the table's <c>standUpReachFt</c>.
    /// </summary>
    public static double CatchRadiusFt(Character fielder, Park? park, RulesTable rules, bool air)
    {
        var r = rules;
        var standUp = BodyClasses.ReachFt(fielder, air, r);
        var radius = standUp + FieldAbilities.CatchBonus(fielder, r);
        if (park != null && ParkHazards.CanClamber(park, fielder, r))
            radius += r.Fielding.Catch.ClamberRadiusFt;
        return radius;
    }

    public static bool IsOutfield(string pos) => pos is "LF" or "CF" or "RF";

    /// <summary>The four infielders who play at a depth (1B, 2B, SS, 3B); the pitcher and the catcher do not.</summary>
    public static bool IsInfieldDepth(string pos) => pos is "1B" or "2B" or "SS" or "3B";

    /// <summary>
    /// Dirt / grass lip ~95 ft past the rubber (flight.classes.infieldLipFt), same split baseball
    /// games use: infielders own the hop on the dirt; outfielders own the grass.
    /// </summary>
    public static bool OutfieldGrass(double x, double z, RulesTable rules) =>
        Diamond.Dist(0, 0, x, z) >= rules.Flight.Classes.InfieldLipFt;

    public static bool OutfieldShouldCharge(double ballX, double ballZ, double landingX, double landingZ, RulesTable rules) =>
        OutfieldGrass(ballX, ballZ, rules) || OutfieldGrass(landingX, landingZ, rules);

    /// <summary>
    /// Still up: fly or liner, hang not due, height above a hop (<c>catch.inAirMinY</c>).
    /// A hopper is never in the air for chase — they charge the live ball.
    /// </summary>
    public static bool InAir(FieldingPreview pre, double ballY, double hitT, RulesTable rules, double? hangSec = null)
    {
        if (pre.Grounder) return false;
        var hang = hangSec ?? pre.HangTimeSec;
        return hitT < hang && ballY > rules.Fielding.Catch.InAirMinY;
    }

    /// <summary>
    /// Where the glove runs. Air → landing / wall plant. Dirt hop → live ball.
    /// Chasing live XZ while the ball is still up is the home-first path
    /// (run to the plate, then watch it fly over).
    /// </summary>
    public static (double X, double Z) GloveChaseTarget(
        FieldingPreview pre,
        Park? park,
        double ballX,
        double ballZ,
        double ballY,
        double hitT,
        RulesTable rules,
        double? hangSec = null) =>
        InAir(pre, ballY, hitT, rules, hangSec)
            ? FlyCatch.ChaseTarget(pre, rules, park)
            : (ballX, ballZ);

    /// <summary>
    /// Charge the landing while the ball is in the air or still on the dirt.
    /// Live hop only once it is on the grass.
    /// </summary>
    public static (double X, double Z) OutfieldChaseTarget(
        double ballX, double ballZ, double landingX, double landingZ, RulesTable rules, bool inAir = false) =>
        inAir || !OutfieldGrass(ballX, ballZ, rules) ? (landingX, landingZ) : (ballX, ballZ);

    /// <summary>
    /// Live glove when the path is missing: IF while the ball is on the dirt, nearest OF once it
    /// reaches the grass. The live ball with a path uses <see cref="FieldingPursuit.Choose"/> (D16,
    /// #667) — nearest is not the play glove. One-way handoff — the infielder who first ran it does
    /// not keep the play in the outfield.
    /// </summary>
    public static (Character Fielder, string Pos) PlayGlove(
        IReadOnlyDictionary<string, Character> assigned,
        double ballX,
        double ballZ,
        RulesTable rules,
        IReadOnlyDictionary<string, (double X, double Z)>? at = null) =>
        OutfieldGrass(ballX, ballZ, rules)
            ? NearestIn(assigned, OutfieldPursuitPositions, ballX, ballZ, rules, at)
            : NearestIn(assigned, InfieldPursuitPositions, ballX, ballZ, rules, at);

    public static (Character Fielder, string Pos) NearestOutfielder(
        IReadOnlyDictionary<string, Character> assigned,
        double x,
        double z,
        RulesTable rules,
        IReadOnlyDictionary<string, (double X, double Z)>? at = null) =>
        NearestIn(assigned, OutfieldPursuitPositions, x, z, rules, at);

    public static bool HandoffToOutfield(string currentPos, string playPos) =>
        !IsOutfield(currentPos) && IsOutfield(playPos);

    /// <summary>
    /// The one glove speed (§8.1, fielding.chase): human stick and CPU chase share it; dash (East held) multiplies it.
    /// <paramref name="frozen"/> is the heart swing's play-wide slow (<see cref="FieldingPreview.Frozen"/>, a special). A
    /// status volume's slow is not an input here: it is the touching body's, applied to its steps (<see cref="BodySlows"/>).
    /// </summary>
    public static double ChaseSpeedFt(Character fielder, bool frozen, RulesTable rules, bool dash = false)
    {
        var c = rules.Fielding.Chase;
        return (c.BaseFtPerSec + fielder.Stats.Run * c.FtPerSecPerRun) * (frozen ? c.FrozenMul : 1)
               * (dash ? FieldDash.ChaseMul(rules) : 1);
    }

    /// <summary>
    /// The chase speed of the body at <paramref name="pos"/> on this ball (§8.1, §8.2): the one glove speed, × <c>fielding.chase.outfieldAirMul</c>
    /// for an outfielder on a ball hit in the air (a fly, a liner, a pop, a wall ball), × <c>fielding.chase.infieldAirMul</c> for an
    /// infielder under a ball on the stretched clock (a fly or a pop, §6.1: the hang is the watcher's, the reach under it is real). Human
    /// stick and CPU chase share it. A ball on the dirt, an infielder on a liner (a rope gets past the glove or it does not, §7.6), a carry,
    /// and a loose ball run at the one speed.
    /// </summary>
    public static double ChaseSpeedFt(Character fielder, string pos, FieldingPreview? pre, RulesTable rules, bool dash = false) =>
        ChaseSpeedFt(fielder, pre?.Frozen ?? false, rules, dash) * AirMul(pos, pre, rules);

    /// <summary>
    /// The speed a body walks to a bag, the throw line or a backup spot (§8.7, #718): the flat cover speed on the
    /// shipped table, the body's own pursuit speed as far as <c>fielding.cover.chaseSpeedWeight</c> reads it. At 0 it
    /// is <c>cover.ftPerSec</c> itself, not a product, so the shipped walk is the same double it always was.
    /// </summary>
    public static double CoverSpeedFt(Character who, RulesTable rules)
    {
        var r = rules;
        var c = r.Fielding.Cover;
        if (c.ChaseSpeedWeight <= 0) return c.FtPerSec;
        return c.FtPerSec + c.ChaseSpeedWeight * (ChaseSpeedFt(who, false, r) - c.FtPerSec);
    }

    /// <summary>
    /// The speed a body carries the ball (F693-02-ordinary-carry-speed, -ball-dash-carrier, #718): the pursuit speed it was
    /// asked for, × <c>fielding.abilities.ballDashMul</c> for a Ball Dash holder. For every other body it is the asked speed
    /// itself, not a product, so the walk the game shipped with is the same double it always was.
    /// </summary>
    public static double CarrySpeedFt(Character who, double pursuitFt, RulesTable rules) =>
        FieldAbilities.HasBallDash(who) ? pursuitFt * rules.Fielding.Abilities.BallDashMul : pursuitFt;

    static double AirMul(string pos, FieldingPreview? pre, RulesTable rules)
    {
        if (pre is not { Grounder: false }) return 1;
        var c = rules.Fielding.Chase;
        if (IsOutfield(pos)) return c.OutfieldAirMul;
        return pre.Line ? 1 : c.InfieldAirMul;
    }

    /// <summary>
    /// The reaction lockout per position (§8.2, fielding.reaction): play seconds before each body may move,
    /// × <paramref name="mul"/>. A ball in the air (<paramref name="airHangSec"/>, its landing instant) caps every
    /// lockout at its hang, so no body is still frozen when the ball it waits on comes down. A ball on the dirt
    /// passes no cap: the infield numbers are the ones the §10.4 double-play rows were tuned on.
    /// </summary>
    public static Dictionary<string, double> ReactionLockouts(RulesTable rules, double mul = 1, double? airHangSec = null, bool bunt = false)
    {
        var re = rules.Fielding.Reaction;
        var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var pos in Diamond.Order)
        {
            var sec = re.LockoutSec(pos) * mul;
            if (airHangSec is double hang) sec = Math.Min(sec, Math.Max(0, hang));
            if (pos == "P" && !bunt) sec = Math.Max(sec, re.PitcherRecoverySec);
            map[pos] = sec;
        }
        return map;
    }

    /// <summary>The lockouts a CPU-driven body waits: × the rung's <c>cpu.reactionMul</c> (§8.2, §16). The human glove waits <see cref="ReactionLockouts"/> at ×1.</summary>
    public static Dictionary<string, double> CpuReactionLockouts(RulesTable rules, double? airHangSec = null, bool bunt = false) =>
        ReactionLockouts(rules, rules.Cpu.Active.ReactionMul, airHangSec, bunt);

    public static (double X, double Z) StepToward(
        double x, double z, double tx, double tz, double speed, double dt, RulesTable rules, Park? park = null)
    {
        var dx = tx - x;
        var dz = tz - z;
        var dist = Math.Sqrt(dx * dx + dz * dz);
        if (dist <= rules.Fielding.Chase.StepStopFt) return park == null ? (x, z) : FieldBounds.ClampFielder(park, x, z, rules);
        var step = Math.Min(dist, speed * dt);
        var next = (X: x + dx / dist * step, Z: z + dz / dist * step);
        return park == null ? next : FieldBounds.ClampFielder(park, next.X, next.Z, rules);
    }

    /// <summary>Timed wall leap. Two good-chem outfielders under a would-be homer, not a flag on any fly.</summary>
    public static bool BuddyJumpOffered(FieldingPreview pre) =>
        pre.Buddy is not null && pre.HomeRunLikely && !pre.Grounder && !pre.Line && IsOutfield(pre.Position);

    static (Character Fielder, string Pos) NearestIn(
        IReadOnlyDictionary<string, Character> keyed,
        IReadOnlyList<string> pool,
        double x,
        double z,
        RulesTable rules,
        IReadOnlyDictionary<string, (double X, double Z)>? at)
    {
        var starts = DiamondGeometry.Of(rules).Positions;
        Character? best = null;
        var bestPos = pool[0];
        var bestD = double.MaxValue;
        foreach (var pos in pool)
        {
            if (!keyed.TryGetValue(pos, out var c)) continue;
            var p = at != null && at.TryGetValue(pos, out var live)
                ? live
                : starts[pos];
            var d = Diamond.Dist(p.X, p.Z, x, z);
            if (d < bestD)
            {
                bestD = d;
                best = c;
                bestPos = pos;
            }
        }
        return (best ?? keyed.Values.First(), bestPos);
    }

    Character? Buddy(
        IReadOnlyDictionary<string, Character> keyed,
        FieldingPreview pre,
        Park park,
        IReadOnlyList<Sample> path,
        IReadOnlyDictionary<string, (double X, double Z)>? at)
    {
        if (!IsOutfield(pre.Position) || !pre.HomeRunLikely) return null;
        var partners = keyed.Where(kv => IsOutfield(kv.Key) && kv.Key != pre.Position
            && _chem.Between(pre.Fielder, kv.Value) == Chemistry.Good).ToDictionary(kv => kv.Key, kv => kv.Value);
        if (partners.Count == 0) return null;
        pre = pre with { Buddy = partners.Values.First() };
        var start = at != null && at.TryGetValue(pre.Position, out var live)
            ? live : OutfieldStarts.Of(park, _rules)[pre.Position];
        var ready = CpuReactionLockouts(_rules, pre.HangTimeSec);
        var own = FieldingPursuit.Plan(pre, park, path, 0, start.X, start.Z,
            ChaseSpeedFt(pre.Fielder, pre.Position, pre, _rules), _rules, ready[pre.Position], body: pre.Fielder);
        if (!own.Reachable) return null;
        var choice = FieldingPursuit.Choose(partners, OutfieldPursuitPositions, pre, park, path, _rules, at, readyAt: ready);
        return choice.Route.Reachable ? choice.Fielder : null;
    }

    /// <summary>The defensive alignment: the team's glove diamond (§8.1) with whoever is on the mound now.</summary>
    public static Dictionary<string, Character> Assign(Team team, Character pitcher) =>
        Assign(team.Roster, pitcher, team.Gloves);

    /// <summary>
    /// Positions from the lineup's glove diamond, not roster order (§8.1). When the pitcher on the
    /// mound is not the diamond's P (a swap, §4.7), the old pitcher takes the vacated glove. Without
    /// a diamond the roster order stands in for it, P first.
    /// </summary>
    public static Dictionary<string, Character> Assign(
        IReadOnlyList<Character> defense, Character pitcher, IReadOnlyDictionary<string, Character>? gloves)
    {
        if (gloves is not null && gloves.TryGetValue("P", out var diamondP) && Diamond.Order.All(gloves.ContainsKey))
        {
            var map = new Dictionary<string, Character>(StringComparer.OrdinalIgnoreCase);
            foreach (var pos in Diamond.Order) map[pos] = gloves[pos];
            if (!diamondP.Id.Equals(pitcher.Id, StringComparison.OrdinalIgnoreCase))
            {
                var vacated = Diamond.Order.FirstOrDefault(pos => gloves[pos].Id.Equals(pitcher.Id, StringComparison.OrdinalIgnoreCase));
                if (vacated is not null)
                {
                    map[vacated] = diamondP;
                    map["P"] = pitcher;
                    return map;
                }
            }
            else
                return map;
        }
        return Assign(defense, pitcher);
    }

    public static Dictionary<string, Character> Assign(IReadOnlyList<Character> defense, Character pitcher)
    {
        var map = new Dictionary<string, Character>(StringComparer.OrdinalIgnoreCase) { ["P"] = pitcher };
        var rest = defense.Where(c => c.Id != pitcher.Id).ToList();
        var i = 0;
        foreach (var pos in Diamond.Order)
        {
            if (pos == "P") continue;
            if (i >= rest.Count) break;
            map[pos] = rest[i++];
        }
        return map;
    }
}

/// <summary>
/// The fielding facts of one batted ball. Before the live ball runs, <see cref="Kind"/> is only
/// what the flight decides alone (homer, foul, chomped) or <see cref="PlayKind.InPlay"/>; after
/// Time it carries what the gloves did — who took the ball, the last throw, a bobble, a sailed
/// throw — and Complete names the play from the bodies (§10.6).
/// </summary>
public sealed record FieldingResult(
    PlayKind Kind,
    Character? Fielder,
    Character? Cutoff,
    double HangTimeSec,
    double LandingX,
    double LandingZ,
    bool Heatball,
    bool Furnace,
    ThrowResult? Throw = null,
    Character? Buddy = null,
    bool Warped = false,
    string? Item = null,
    bool Bobble = false,
    double KnockbackSec = 0,
    DefensiveFeat Feat = DefensiveFeat.None,
    bool GroundRule = false,
    /// <summary>A glove took the ball this play.</summary>
    bool Caught = false,
    /// <summary>A throw missed its cover and skipped past, live (§8.5, §8.6): the ERROR.</summary>
    bool ThrowSailed = false,
    /// <summary>The thrown item landed on its body (§12); <see cref="ItemTarget"/> is who.</summary>
    bool ItemHit = false,
    Character? ItemTarget = null,
    /// <summary>The type of the last redirect the live ball went through this play (F4-c), or null.</summary>
    string? RedirectType = null);

/// <summary>
/// What the defense is looking at from the crack: the glove on it, the landing mark, and the
/// ball's class (§6.2) — the one table the pools, the ring, the catch window, and the cameras read.
///
/// <para>
/// <see cref="Frozen"/> is the heart swing's slow (a special, §13): every chaser runs at
/// <c>fielding.chase.frozenMul</c> for the play, and the CPU's catch rolls <c>fielding.drops.frozen</c>,
/// exactly as shipped, because the specials are outside this phase. It is no longer set by a park
/// (F4-b, #896; FD-08-R1, FR-07): the preview says nothing about a status volume, and a volume slows
/// only the body that touches it, live (<see cref="BodySlows"/>). The preview and the CPU plan at full
/// speed; they do not foresee a slow.
/// </para>
/// </summary>
public sealed record FieldingPreview(
    Character Fielder,
    string Position,
    Character? Buddy,
    double HangTimeSec,
    double LandingX,
    double LandingZ,
    BattedBallClass Class,
    bool Heatball,
    bool Furnace,
    bool Frozen,
    double CatchRadius,
    bool Warped = false,
    bool Foul = false,
    BattedBall? Ball = null)
{
    /// <summary>On the dirt: the glove scoops it, no ring, no window.</summary>
    public bool Grounder => Class.OnTheDirt();

    /// <summary>A rope: the short window on the infield.</summary>
    public bool Line => Class == BattedBallClass.Liner;

    /// <summary>The flight clears the fence: only a leap at the wall takes it (§8.4).</summary>
    public bool HomeRunLikely => Class == BattedBallClass.Homer;
}

/// <summary>
/// What a park's hazards do to a play (§14). The ball redirect, the reward target and the catch
/// stealer still run once, in <see cref="FieldingResolver.Preview"/>, against the ball's landing mark
/// (F4-c, F4-f and F4-g change them). The status volume does not (F4-b, #896): the live ball tests
/// every body against <see cref="StatusVolumes"/> each frame and slows the one that touches a disc
/// (<see cref="BodySlows"/>). #847 moved the <em>dispatch</em>; F4-b made the first pattern live.
///
/// <para>
/// <b>Patterns, not type strings (FD-09, FR-08).</b> Each method asks the hazard library
/// (<see cref="HazardRules"/>, <c>data/rules/hazards.json</c>) for the row of the type a park
/// authored and runs the pattern that row names. No method here spells a hazard type, and none
/// spells a park id (FR-04): Funfair's chompers are three <c>chomper</c> rows in
/// <c>data/parks/funfair-park.json</c>, the way every other hazard already was.
/// </para>
///
/// <para>
/// <b>The park is the played park (FD-11, F4-d).</b> Every method reads the instances of the park it
/// is handed, which in a match is <see cref="Match.Park"/> — resolved once by <see cref="PlayedPark.Of"/>,
/// the night block's instances already in it at night and every hazard gone with hazards off. Nothing
/// here asks whether an instance exists tonight. The one night number left is a type's own
/// (<see cref="NightDiscFt"/>); night reaches no rule of the at-bat (FD-11-R2).
/// </para>
///
/// <para>
/// A type with no row is a stop, not a shrug: <see cref="HazardRules.Of"/> throws rather than
/// letting an unknown hazard quietly do nothing. The park validator refuses one at load, so a
/// catalog that opened can never reach it.
/// </para>
/// </summary>
public static class ParkHazards
{
    public static bool InFreeze(Park park, double x, double z, RulesTable rules, bool night = false) =>
        InSlow(park, x, z, rules, night);

    /// <summary>
    /// The disc an instance of radius <paramref name="radiusFt"/> plays at night: the type row's own
    /// <c>nightRadiusMul</c> times it (FD-11-R2 keeps a hazard type's own night numbers). 1 for every
    /// row but Ember's breath, and a multiply by 1 is exact. The status volume and the park validator's
    /// placement rule (FD-19, map finding 31) both read it, so the disc a runner's lane is kept off is the disc
    /// a body is slowed in.
    /// </summary>
    public static double NightDiscFt(double radiusFt, HazardTypeRules row) => radiusFt * row.NightRadiusMul;

    /// <summary>
    /// A point inside one of the park's <see cref="HazardPattern.StatusVolume"/> discs: a body standing
    /// there is touching the volume (F4-b, #896). The disc is <see cref="StatusVolumes"/>'s, <see cref="NightDiscFt"/> at night. Nothing tests a landing mark against it any more.
    /// </summary>
    public static bool InSlow(Park park, double x, double z, RulesTable rules, bool night = false)
    {
        foreach (var v in StatusVolumes(park, rules, night))
            if (v.Contains(x, z)) return true;
        return false;
    }

    /// <summary>
    /// The park's <see cref="HazardPattern.StatusVolume"/> instances as a play on it reads them (FR-07,
    /// FD-08-R2; F4-b, #896), in park order: each instance's index in <see cref="Park.Hazards"/>, its
    /// centre, its radius × the row's <c>nightRadiusMul</c> at night, and the row's <c>slowSec</c>. Empty
    /// at a park with none, and at a hazards-off match's park, which has no instance to list.
    /// </summary>
    public static IReadOnlyList<StatusVolume> StatusVolumes(Park park, RulesTable rules, bool night = false)
    {
        var hazards = rules.Hazards;
        List<StatusVolume>? list = null;
        for (var i = 0; i < park.Hazards.Count; i++)
        {
            var h = park.Hazards[i];
            var row = hazards.Of(h.Type);
            if (row.Pattern != HazardPattern.StatusVolume) continue;
            var r = night ? NightDiscFt(h.Radius, row) : h.Radius;
            // A status volume always authors its time (HazardRules.Validate); a table that loaded cannot reach the throw.
            var slow = row.SlowSec ?? throw new InvalidOperationException(
                $"hazards.{HazardType.Key(h.Type)} is a {HazardPattern.StatusVolume} with no slowSec (FD-08-R2)");
            (list ??= []).Add(new StatusVolume(i, h.Type, h.X, h.Z, r, slow));
        }
        return list is null ? [] : list;
    }

    /// <summary>
    /// A Clamber fielder in a park that lists a <see cref="HazardPattern.WallTrait"/>. Park-wide
    /// today — the row's position and radius are never read — which is what FD-06 turns into a
    /// property of one wall span.
    /// </summary>
    public static bool CanClamber(Park park, Character fielder, RulesTable rules)
    {
        if (fielder.FieldAbility != FieldAbilityId.Clamber) return false;
        var hazards = rules.Hazards;
        return park.Hazards.Any(h => hazards.Of(h.Type).Pattern == HazardPattern.WallTrait);
    }

    /// <summary>Clamber robs a ball clearing the fence by at most fielding.catch.clamberRobFt (§8.4).</summary>
    public static bool CanClamberRob(Park park, Character fielder, AtBatResult hit, RulesTable rules)
    {
        if (!CanClamber(park, fielder, rules)) return false;
        var ball = BattedBall.Of(hit, park, rules);
        return ball.HomeRun && ball.FenceClearFt <= rules.Fielding.Catch.ClamberRobFt;
    }
}
