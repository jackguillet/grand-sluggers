namespace GrandSluggers.Sim;

/// <summary>
/// Arcade at-bat: timing window + charge + bat + chemistry-on-base + a ballistic carry estimate.
/// Not a sim of spin axis. Good enough to tune numbers before Unity exists.
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

    /// <summary>Full stick at contact pulls down the line (batting.spray.stickMaxDeg; spread can take it foul).</summary>
    public static double SprayAimDeg(double stickX, RulesTable? rules = null) =>
        Math.Clamp(stickX, -1, 1) * Rules.Or(rules).Batting.Spray.StickMaxDeg;

    public static bool IsFoul(double sprayDeg) =>
        Math.Abs(sprayDeg) > FoulLineDeg;

    readonly ChemistryTable _chem;
    readonly RulesTable _rules;

    public AtBatResolver(ChemistryTable chem, RulesTable? rules = null)
    {
        _chem = chem;
        _rules = Rules.Or(rules);
    }

    public AtBatResult Resolve(AtBatInput input, Park park, Random rng, bool night = false)
    {
        var b = _rules.Batting;
        var contact = input.Batter.Stats.Bat + (input.Bat?.ContactMod ?? 0);
        var power = input.Batter.Stats.Bat + (input.Bat?.PowerMod ?? 0);
        contact = Math.Clamp(contact, 1, 10);
        power = Math.Clamp(power, 1, 10);

        var effective = input.Bat?.ChargeAlwaysFull == true ? 1.0
            : input.Charge01 > 0 ? input.Charge01
            : input.ChargeSwing ? 1.0 : 0;
        var window = b.Window.BaseFrames + (contact - 5) * b.Window.FramesPerContact;
        if (ChargeFeel.IsCharge(effective) && input.Bat?.ChargeAlwaysFull != true)
            window *= b.Window.ChargeMul;
        if (input.UseStarPitch)
            window *= StarSkills.BatterWindowMul(input.Pitcher.StarPitch);
        window *= ParkHazards.ContactWindowMul(park, night, _rules);

        var oval = SweetSpot.Overlap(input.BoxOffsetX, input.PitchAimX, input.PitchAimY, _rules);
        var timing = Math.Abs(input.TimingErrorFrames);
        var quality = timing <= b.Window.PerfectFrames ? ContactQuality.Perfect
            : timing <= window * b.Window.SolidFraction ? ContactQuality.Solid
            : timing <= window ? ContactQuality.Cheap
            : ContactQuality.Miss;
        if (oval <= 0 && !input.Bunt)
            quality = ContactQuality.Miss;
        else if (oval < 1 && quality == ContactQuality.Perfect)
            quality = ContactQuality.Cheap;

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

        var charge = 1.0 + b.Charge.PowerPerCharge * Math.Clamp(effective, 0, 1);
        if (input.Bat?.ChargeAlwaysFull == true)
            charge = b.Charge.ChargeBatMul;
        var qualityMul = quality switch
        {
            ContactQuality.Perfect => b.Quality.PerfectExitMul,
            ContactQuality.Solid => b.Quality.SolidExitMul,
            _ => b.Quality.CheapExitMul
        };
        var starSwingMul = input.UseStarSwing ? StarSkills.SwingExitMul(input.Batter.StarSwing) : 1.0;
        var onBaseMul = _chem.ChargePowerMul(input.Batter, input.RunnersOn);

        var exit = b.Exit.BaseMph + power * b.Exit.MphPerPower;
        exit *= charge * qualityMul * starSwingMul * onBaseMul;
        if (input.PitcherStamina < _rules.Pitching.Stamina.TiredBelow)
            exit *= b.Exit.TiredPitcherMul;

        // Late / under (positive frames) and stick-up (LaunchAim +) pull launch down into a hopper.
        // Early / over pops up. Square still mixes liners and some grounders.
        var signed = input.TimingErrorFrames;
        var loft = b.Launch.LoftBaseDeg + (power - 5) * b.Launch.LoftPerPower + (ChargeFeel.IsCharge(effective) ? b.Charge.LoftDeg : 0);
        var launch = loft - signed * b.Launch.DegPerFrame - input.LaunchAim * b.Launch.StickDeg + (rng.NextDouble() - 0.5) * b.Launch.NoiseDeg;
        if (quality == ContactQuality.Cheap)
            launch = signed >= 0
                ? b.Launch.CheapLateMinDeg + rng.NextDouble() * b.Launch.CheapLateSpanDeg
                : b.Launch.CheapEarlyMinDeg + rng.NextDouble() * b.Launch.CheapEarlySpanDeg;

        if (input.Bunt)
        {
            exit *= b.Bunt.ExitMul;
            launch = b.Bunt.LaunchMinDeg + rng.NextDouble() * b.Bunt.LaunchSpanDeg;
        }
        launch = Math.Clamp(launch, b.Launch.MinDeg, b.Launch.MaxDeg);

        if (input.UseStarSwing && !input.Bunt)
            launch = StarLaunch(input.Batter.StarSwing, launch, b.Star);

        var spray = input.SprayAimDeg + (rng.NextDouble() - 0.5) * SpraySpread(quality, b.Spray);
        if (input.UseStarPitch && input.Pitcher.StarPitch == "prismball")
            spray += (rng.NextDouble() - 0.5) * b.Star.PrismballSpraySpanDeg;
        if (!input.PitchInZone)
            spray += (rng.NextDouble() - 0.5) * b.Spray.OutOfZoneSpanDeg;
        if (input.Bunt)
            spray += (rng.NextDouble() - 0.5) * b.Bunt.SpraySpanDeg;
        spray = CheapFoulPull(quality, spray, rng, b.Foul);

        var carry = BallFlight.CarryFeet(exit, launch, park.WindMph, _rules);
        var foul = IsFoul(spray);
        var fence = FenceAt(park, spray);
        var homer = !foul && !input.Bunt && carry >= fence && launch > b.Homer.LaunchMinDeg && launch < b.Homer.LaunchMaxDeg;

        return new AtBatResult(
            quality,
            InPlay: !foul,
            Strike: false,
            ExitVeloMph: Math.Round(exit, 1),
            LaunchDeg: Math.Round(launch, 1),
            CarryFt: Math.Round(carry, 1),
            HomeRun: homer && !foul,
            ChemistryItemOffered: _chem.ChemistryItemOffered(input.Batter, input.OnDeck),
            StarPitchUsed: input.UseStarPitch ? input.Pitcher.StarPitch : null,
            StarSwingUsed: input.UseStarSwing ? input.Batter.StarSwing : null,
            SprayDeg: Math.Round(spray, 1),
            Foul: foul,
            InZone: input.PitchInZone);
    }

    static double SpraySpread(ContactQuality q, SprayRules spray) => q switch
    {
        ContactQuality.Perfect => spray.PerfectSpreadDeg,
        ContactQuality.Solid => spray.SolidSpreadDeg,
        _ => spray.CheapSpreadDeg
    };

    /// <summary>
    /// Cheap contact already pulled toward a line can skip past the chalk.
    /// The ball flies foul — we do not stamp Foul on a fair spray.
    /// </summary>
    static double CheapFoulPull(ContactQuality quality, double spray, Random rng, FoulRules foul)
    {
        if (quality != ContactQuality.Cheap || Math.Abs(spray) <= foul.CheapPullMinDeg || rng.NextDouble() >= foul.CheapPullChance)
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

    static double RoundFence(Park park, double sprayDeg)
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

    static double StarLaunch(string swing, double fallback, StarSwingRules star) => swing switch
    {
        "ground" => star.GroundLaunchDeg,
        "fly" => star.FlyLaunchDeg,
        "line" => star.LineLaunchDeg,
        _ => fallback
    };

    static double Lerp(double a, double b, double t) => a + (b - a) * t;

    public static bool PitchInZone(PitchCommand pitch, int pitchStat, string? starPitchId = null)
    {
        // Skill/charge affect the delivery, never an invisible resizing of the zone.
        _ = pitchStat;
        return StrikeZoneGeometry.Contains(pitch, starPitchId);
    }

    /// <summary>
    /// The batter's body radius in normalized plate-aim units (batting.hbp.bodyRadius). The
    /// center comes from the authored batter's box and converts the actor's world-space walk.
    /// </summary>
    public static double BatterBodyPlateX(double boxOffsetX, Hand bats = Hand.R)
    {
        var boxWorldX = bats == Hand.L ? HomeSet.BoxX : -HomeSet.BoxX;
        var walkWorldX = boxOffsetX * HomeSet.BatterWalk;
        return (boxWorldX + walkWorldX) / PitchFlight.PlateScaleX;
    }

    public static bool HitsBatter(double boxOffsetX, double pitchAimX, double pitchAimY, Hand bats = Hand.R, RulesTable? rules = null)
    {
        var bodyR = Rules.Or(rules).Batting.Hbp.BodyRadius;
        var bodyX = BatterBodyPlateX(boxOffsetX, bats);
        var dx = pitchAimX - bodyX;
        var dy = pitchAimY;
        return dx * dx + dy * dy <= bodyR * bodyR;
    }

    /// <summary>CPU sac (batting.cpu.sacBuntChance): runner on first, fewer than two outs, in the zone.</summary>
    public static bool CpuSacBuntSpot(bool inZone, bool runnerOnFirst, int outs, double roll, RulesTable? rules = null) =>
        inZone && runnerOnFirst && outs < 2 && roll < Rules.Or(rules).Batting.Cpu.SacBuntChance;

    public static double PitchSpeedMph(PitchCommand pitch, int pitchStat, RulesTable? rules = null)
    {
        var sp = Rules.Or(rules).Pitching.Speed;
        var changeup = pitch.Changeup || pitch.Type == "changeup";
        var baseSpeed = changeup ? sp.ChangeupMph
            : pitch.Type == "curve" ? sp.CurveMph
            : pitch.Type == "slider" ? sp.SliderMph
            : sp.FastballMph;
        var speed = baseSpeed + pitchStat * sp.MphPerPitchStat + (changeup ? pitch.Charge01 * sp.ChangeupChargeMph : pitch.Charge01 * sp.ChargeMph);
        if (pitch.Star) speed *= sp.StarSpeedMul;
        return speed;
    }

    public static double PitchSpeedMph(PitchCommand pitch, Character pitcher, RulesTable? rules = null)
    {
        var speed = PitchSpeedMph(pitch, pitcher.Stats.Pitch, rules);
        if (!pitch.Star) return speed;
        return speed / Rules.Or(rules).Pitching.Speed.StarSpeedMul * StarSkills.PitchSpeedMul(pitcher.StarPitch);
    }
}
