namespace GrandSluggers.Sim;

/// <summary>
/// The at-bat contract (spec §5, D4): the cursor decides quality, timing decides direction.
/// One swing at one crossing, resolved from world geometry and the tables in batting.json;
/// the only randomness is spread on the inputs (launch noise, spray spread), never the outcome.
/// </summary>
public sealed class AtBatResolver
{
    /// <summary>
    /// Chalk. Geometry of the diamond (first and third sit on the ±45° lines), shared by the
    /// wall and stands meshes, so it stays a constant like <see cref="Diamond.Baseline"/>.
    /// Past this spray is foul territory, not a caption on a fair fly.
    /// </summary>
    public const double FoulLineDeg = 45;

    /// <summary>A round fence shorter than this is a degenerate circle; the two-post lerp is used instead.</summary>
    const double RoundFenceMinFt = 50;

    /// <summary>Stick L/R at contact shifts the whole direction range (batting.spray.stickDeg, spec §5.3).</summary>
    public static double SprayAimDeg(double stickX, RulesTable? rules = null) =>
        Math.Clamp(stickX, -1, 1) * Rules.Or(rules).Batting.Spray.StickDeg;

    readonly ChemistryTable _chem;
    readonly RulesTable _rules;
    readonly StarSkillTable _skills;

    public AtBatResolver(ChemistryTable chem, RulesTable? rules = null, StarSkillTable? skills = null)
    {
        _chem = chem;
        _rules = Rules.Or(rules);
        _skills = StarSkillTable.Or(skills);
    }

    public AtBatResult Resolve(AtBatInput input, Park park, Random rng, bool night = false)
    {
        var b = _rules.Batting;
        var contact = Math.Clamp(input.Batter.Stats.Bat + (input.Bat?.ContactMod ?? 0), 1, 10);
        var power = Math.Clamp(input.Batter.Stats.Bat + (input.Bat?.PowerMod ?? 0), 1, 10);
        var bats = input.Batter.Bats;

        // The Charge Bat is a MAX charge for free with the narrow charge zones off (§5.5).
        var chargeBat = input.Bat?.ChargeAlwaysFull == true;
        var effective = chargeBat ? 1.0 : Math.Clamp(input.Charge01, 0, 1);
        var charged = ChargeFeel.IsCharge(effective);
        var buddies = _chem.BuddiesOnBase(input.Batter, input.RunnersOn);

        // Timing (§5.3): outside the window the bat is not on the plane.
        var window = ContactWindowFrames(contact, charged && !chargeBat,
            input.UseStarPitch ? input.Pitcher.StarPitch : null, park, night, _rules, _skills);
        var half = window / 2;
        var err = input.TimingErrorFrames;
        var onPlane = Math.Abs(err) <= half;

        // Cursor (§5.2): where the crossing meets the bat.
        var barrel = SweetSpot.BarrelScale(contact, charged, chargeBat, buddies, _rules);
        var quality = onPlane
            ? SweetSpot.Zone(input.BoxOffsetX, bats, input.CrossingX, input.CrossingY, barrel, _rules)
            : ContactQuality.Miss;
        // The rim of the window is not square: one tier down, never two (§5.3).
        if (quality > ContactQuality.Sour && Math.Abs(err) > half * b.Window.SquareFraction)
            quality--;

        if (input.UseStarPitch && input.Pitcher.StarPitch == "phonyball"
            && quality != ContactQuality.Perfect && rng.NextDouble() < b.Star.PhonyballWhiff)
            quality = ContactQuality.Miss;

        if (quality == ContactQuality.Miss)
        {
            return new AtBatResult(
                quality, false, true, 0, 0, 0, false,
                _chem.ChemistryItemOffered(input.Batter, input.OnDeck),
                input.UseStarPitch ? input.Pitcher.StarPitch : null,
                null,
                SprayDeg: 0,
                Foul: false,
                InZone: input.PitchInZone);
        }

        // Exit (§5.5): base(power) × zone (slap → charge column by the charge) × star × buddies × pitch.
        var zoneMul = Lerp(b.Quality.Slap.For(quality), b.Quality.Charge.For(quality), effective);
        var starSwingMul = input.UseStarSwing ? StarSkills.SwingExitMul(input.Batter.StarSwing, _skills) : 1.0;
        var onBaseMul = charged ? _chem.ChargePowerMul(input.Batter, input.RunnersOn) : 1.0;
        var pitchMul = PitchFactor(input.ChargePitch, quality, charged, input.Pitcher.Stats.Pitch, b.PitchFactor);

        var exit = b.Exit.BaseMph + power * b.Exit.MphPerPower;
        exit *= zoneMul * starSwingMul * onBaseMul * pitchMul;
        if (input.PitcherStamina < _rules.Pitching.Stamina.TiredBelow)
            exit *= b.Exit.TiredPitcherMul;

        // Launch (§5.4): power and charge lift, the pitch height, the stick (up = grounder), noise.
        var height = input.CrossingY - StrikeZoneGeometry.CenterY;
        var loft = b.Launch.LoftBaseDeg + (power - 5) * b.Launch.LoftPerPower
                   + (charged ? b.Charge.LoftDeg : 0) + height * b.Launch.PerFtOfHeight;
        var launch = loft - input.LaunchAim * b.Launch.StickDeg + (rng.NextDouble() - 0.5) * b.Launch.NoiseDeg;
        if (quality == ContactQuality.Sour)
        {
            // Sour is forced to a band: early tops it, late pops it; a sour slap on a changeup
            // or a charged pitch is a pop-up (§5.2, the pitcher-vs-batter game).
            var pop = err > 0 || (!charged && (input.ChangeupPitch || input.ChargePitch));
            launch = pop
                ? b.Launch.PopMinDeg + rng.NextDouble() * b.Launch.PopSpanDeg
                : b.Launch.TopperMinDeg + rng.NextDouble() * b.Launch.TopperSpanDeg;
        }

        if (input.Bunt)
        {
            exit *= b.Bunt.ExitMul;
            var pop = quality == ContactQuality.Sour || height > b.Bunt.PopAboveCenterFt;
            launch = pop
                ? b.Launch.PopMinDeg + rng.NextDouble() * b.Launch.PopSpanDeg
                : b.Bunt.LaunchMinDeg + rng.NextDouble() * b.Bunt.LaunchSpanDeg;
        }
        launch = Math.Clamp(launch, b.Launch.MinDeg, b.Launch.MaxDeg);

        if (input.UseStarSwing && !input.Bunt)
            launch = StarSkills.SwingLaunchDeg(input.Batter.StarSwing, _skills) ?? launch;

        // Direction (§5.3): early pulls, late pushes; the stick shifts; the zone spreads.
        var spray = (input.Bunt ? 0 : TimingSprayDeg(err, window, bats, _rules))
                    + input.SprayAimDeg + (rng.NextDouble() - 0.5) * SpraySpread(quality, b.Spray);
        if (input.UseStarPitch && input.Pitcher.StarPitch == "prismball")
            spray += (rng.NextDouble() - 0.5) * b.Star.PrismballSpraySpanDeg;
        if (!input.PitchInZone)
            spray += (rng.NextDouble() - 0.5) * b.Spray.OutOfZoneSpanDeg;
        if (input.Bunt)
            spray += (rng.NextDouble() - 0.5) * b.Bunt.SpraySpanDeg;
        spray = Math.Round(SourFoulPull(quality, spray, rng, b.Foul), 1);

        // The flight decides (spec §5.6, §6.1): the clipped path in this park says where the ball
        // lands, whether it clears the fence, and whether the untouched ball is fair or foul.
        // One rule for the resolver, the fielding preview, and the landing ring.
        exit = Math.Round(exit, 1);
        launch = Math.Round(launch, 1);
        spray = Math.Round(spray, 1);
        var ball = BattedBall.Of(exit, launch, spray, input.Bunt, park, _rules);

        return new AtBatResult(
            quality,
            InPlay: !ball.Foul,
            Strike: false,
            ExitVeloMph: exit,
            LaunchDeg: launch,
            CarryFt: Math.Round(ball.LandingDist, 1),
            HomeRun: ball.HomeRun,
            ChemistryItemOffered: _chem.ChemistryItemOffered(input.Batter, input.OnDeck),
            StarPitchUsed: input.UseStarPitch ? input.Pitcher.StarPitch : null,
            StarSwingUsed: input.UseStarSwing ? input.Batter.StarSwing : null,
            SprayDeg: spray,
            Foul: ball.Foul,
            InZone: input.PitchInZone,
            Class: ball.Shape);
    }

    /// <summary>
    /// The timing window in frames at 60 Hz (spec §5.3): slap 9 / charge 7, ± (contact − 5) × 0.4,
    /// × the star pitch's window multiplier × the park's, floored. Inside is ± half of this.
    /// </summary>
    public static double ContactWindowFrames(int contact, bool charged, string? starPitch, Park? park, bool night,
        RulesTable? rules = null, StarSkillTable? skills = null)
    {
        var r = Rules.Or(rules);
        var w = r.Batting.Window;
        var frames = (charged ? w.ChargeFrames : w.SlapFrames) + (Math.Clamp(contact, 1, 10) - 5) * w.FramesPerContact;
        if (starPitch is not null)
            frames *= StarSkills.BatterWindowMul(starPitch, skills);
        if (park is not null)
            frames *= ParkHazards.ContactWindowMul(park, night, r);
        return Math.Max(w.FloorFrames, frames);
    }

    /// <summary>
    /// Direction from timing (spec §5.3): linear across the window, earliest ≈ −timingDeg toward
    /// the pull line, latest ≈ +timingDeg toward the opposite line. A right-handed batter pulls
    /// toward third (negative spray); a left-handed batter pulls toward first.
    /// </summary>
    public static double TimingSprayDeg(double errFrames, double windowFrames, Hand bats, RulesTable? rules = null)
    {
        var half = Math.Max(0.01, windowFrames / 2);
        var t = Math.Clamp(errFrames / half, -1, 1);
        return t * Rules.Or(rules).Batting.Spray.TimingDeg * SweetSpot.TipSign(bats);
    }

    /// <summary>
    /// The pitch's say (spec §5.5): a charged pitch met sour ×0.6, met by a perfect charge ×1.1;
    /// a high-Pitch arm dampens non-perfect contact per stat point above 5.
    /// </summary>
    public static double PitchFactor(bool chargedPitch, ContactQuality quality, bool chargedSwing, int pitchStat, PitchFactorRules f)
    {
        var mul = 1.0;
        if (chargedPitch && quality == ContactQuality.Sour) mul *= f.ChargedVsSour;
        if (chargedPitch && chargedSwing && quality == ContactQuality.Perfect) mul *= f.ChargedVsPerfectCharge;
        var above = Math.Max(0, Math.Clamp(pitchStat, 1, 10) - 5);
        if (quality == ContactQuality.Nice) mul *= 1 - above * f.NiceDampPerPitch;
        if (quality == ContactQuality.Sour) mul *= 1 - above * f.SourDampPerPitch;
        return mul;
    }

    static double SpraySpread(ContactQuality q, SprayRules spray) => q switch
    {
        ContactQuality.Perfect => spray.PerfectSpreadDeg,
        ContactQuality.Nice => spray.NiceSpreadDeg,
        _ => spray.SourSpreadDeg
    };

    /// <summary>
    /// Sour contact already pulled toward a line can skip past the chalk (batting.foul, the
    /// spray cutoff as shipped until P2 lands the foul lines in the flight).
    /// </summary>
    static double SourFoulPull(ContactQuality quality, double spray, Random rng, FoulRules foul)
    {
        if (quality != ContactQuality.Sour || Math.Abs(spray) <= foul.CheapPullMinDeg || rng.NextDouble() >= foul.CheapPullChance)
            return spray;
        var side = spray >= 0 ? 1 : -1;
        return side * (FoulLineDeg + foul.CheapPullPastDeg + rng.NextDouble() * foul.CheapPullSpanDeg);
    }

    public static double FenceAt(Park park, double sprayDeg)
    {
        // spray −45 left, 0 center, +45 right. The wall is the circle through
        // the three posts so CF is round — not a chevron from two lerps.
        var t = Math.Clamp((sprayDeg + FoulLineDeg) / (FoulLineDeg * 2), 0, 1);
        var spray = -FoulLineDeg + t * 2 * FoulLineDeg;
        var round = RoundFence(park, spray);
        if (round > RoundFenceMinFt) return round;
        if (t < 0.5)
            return Lerp(park.LeftFenceFt, park.CenterFenceFt, t * 2);
        return Lerp(park.CenterFenceFt, park.RightFenceFt, (t - 0.5) * 2);
    }

    /// <summary>
    /// Left and right slopes at CF match. The old piecewise lerp kinks here
    /// (an indent / point in the wall).
    /// </summary>
    public static bool FenceIsSmoothAtCenter(Park park)
    {
        var c = FenceAt(park, 0);
        var sl = (c - FenceAt(park, -2)) / 2;
        var sr = (FenceAt(park, 2) - c) / 2;
        return Math.Abs(sl - sr) < 0.2;
    }

    /// <summary>The circle through the three posts, or 0 where it degenerates (then <see cref="FenceAt"/> lerps the posts).</summary>
    public static double RoundFence(Park park, double sprayDeg)
    {
        var lf = Post(park.LeftFenceFt, -FoulLineDeg);
        var cf = Post(park.CenterFenceFt, 0);
        var rf = Post(park.RightFenceFt, FoulLineDeg);
        var ax = lf.X;
        var az = lf.Z;
        var bx = cf.X;
        var bz = cf.Z;
        var cx = rf.X;
        var cz = rf.Z;
        var d = 2 * (ax * (bz - cz) + bx * (cz - az) + cx * (az - bz));
        if (Math.Abs(d) < 1e-6) return 0;
        var a2 = ax * ax + az * az;
        var b2 = bx * bx + bz * bz;
        var c2 = cx * cx + cz * cz;
        var ux = (a2 * (bz - cz) + b2 * (cz - az) + c2 * (az - bz)) / d;
        var uz = (a2 * (cx - bx) + b2 * (ax - cx) + c2 * (bx - ax)) / d;
        var r2 = (ux - bx) * (ux - bx) + (uz - bz) * (uz - bz);
        var rad = sprayDeg * Math.PI / 180.0;
        var sx = Math.Sin(rad);
        var sz = Math.Cos(rad);
        var b = sx * ux + sz * uz;
        var disc = b * b - (ux * ux + uz * uz - r2);
        if (disc < 0) return 0;
        var root = Math.Sqrt(disc);
        var far = Math.Max(b + root, b - root);
        return far > RoundFenceMinFt ? far : 0;
    }

    static (double X, double Z) Post(double fenceFt, double sprayDeg)
    {
        var rad = sprayDeg * Math.PI / 180.0;
        return (Math.Sin(rad) * fenceFt, Math.Cos(rad) * fenceFt);
    }

    static double Lerp(double a, double b, double t) => a + (b - a) * t;

    public static bool PitchInZone(PitchCommand pitch, int pitchStat, string? starPitchId = null)
    {
        // Skill/charge affect the delivery, never an invisible resizing of the zone.
        _ = pitchStat;
        return StrikeZoneGeometry.Contains(pitch, starPitchId);
    }

    /// <summary>The batter's body at the plate plane in world feet: the authored box plus the walk (spec §4.6).</summary>
    public static double BatterBodyX(double boxOffsetX, Hand bats = Hand.R) =>
        HomeSet.BatterBodyX(bats, boxOffsetX);

    /// <summary>
    /// Hit by pitch (spec §4.6): the crossing lies inside the body circle (batting.hbp.bodyRadiusFt)
    /// centered where the body actually is, at the natural crossing height. World feet, the same
    /// point the umpire and the cursor read; body and cursor move the same distance per box unit.
    /// </summary>
    public static bool HitsBatter(double boxOffsetX, double crossingX, double crossingY, Hand bats = Hand.R, RulesTable? rules = null)
    {
        var bodyR = Rules.Or(rules).Batting.Hbp.BodyRadiusFt;
        var dx = crossingX - BatterBodyX(boxOffsetX, bats);
        var dy = crossingY - PitchFlight.PlateY;
        return dx * dx + dy * dy <= bodyR * bodyR;
    }

    /// <summary>CPU sac (batting.cpu.sacBuntChance): runner on first, fewer than two outs, in the zone.</summary>
    public static bool CpuSacBuntSpot(bool inZone, bool runnerOnFirst, int outs, double roll, RulesTable? rules = null) =>
        inZone && runnerOnFirst && outs < 2 && roll < Rules.Or(rules).Batting.Cpu.SacBuntChance;

    /// <summary>
    /// Speed by shape, Pitch stat and charge (pitching.speed); a Nice! release adds
    /// pitching.release.niceMul (spec §4.1); a star pitch multiplies by its skill's speedMul
    /// (star-skills.json). <paramref name="mphPenalty"/> is the tired / exhausted arm (spec §4.7).
    /// </summary>
    public static double PitchSpeedMph(PitchCommand pitch, int pitchStat, RulesTable? rules = null,
        string? starPitchId = null, StarSkillTable? skills = null, double mphPenalty = 0)
    {
        var r = Rules.Or(rules);
        var sp = r.Pitching.Speed;
        var changeup = pitch.IsChangeup;
        var baseSpeed = changeup ? sp.ChangeupMph : sp.FastballMph;
        var speed = baseSpeed + pitchStat * sp.MphPerPitchStat + (changeup ? pitch.Charge01 * sp.ChangeupChargeMph : pitch.Charge01 * sp.ChargeMph);
        if (pitch.Nice) speed *= r.Pitching.Release.NiceMul;
        if (pitch.Star) speed *= StarSkills.PitchSpeedMul(starPitchId, skills);
        return Math.Max(r.Pitching.Flight.MinMph, speed - mphPenalty);
    }

    public static double PitchSpeedMph(PitchCommand pitch, Character pitcher, RulesTable? rules = null,
        StarSkillTable? skills = null, double mphPenalty = 0) =>
        PitchSpeedMph(pitch, pitcher.Stats.Pitch, rules, pitcher.StarPitch, skills, mphPenalty);
}
