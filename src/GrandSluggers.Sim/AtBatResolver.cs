namespace GrandSluggers.Sim;

/// <summary>
/// Arcade at-bat: timing window + charge + bat + chemistry-on-base + a ballistic carry estimate.
/// Not a sim of spin axis. Good enough to tune numbers before Unity exists.
/// </summary>
public sealed class AtBatResolver
{
    public const double FastballMph = 88;
    public const double PerfectWindowFrames = 1.0;
    public const double BaseContactWindowFrames = 7.0;

    readonly ChemistryTable _chem;

    public AtBatResolver(ChemistryTable chem) => _chem = chem;

    public AtBatResult Resolve(AtBatInput input, Park park, Random rng)
    {
        var contact = input.Batter.Stats.Bat + (input.Bat?.ContactMod ?? 0);
        var power = input.Batter.Stats.Bat + (input.Bat?.PowerMod ?? 0);
        contact = Math.Clamp(contact, 1, 10);
        power = Math.Clamp(power, 1, 10);

        var window = BaseContactWindowFrames + (contact - 5) * 0.55;
        if (input.ChargeSwing && input.Bat?.ChargeAlwaysFull != true)
            window *= 0.78;

        var timing = Math.Abs(input.TimingErrorFrames);
        var quality = timing <= PerfectWindowFrames ? ContactQuality.Perfect
            : timing <= window * 0.55 ? ContactQuality.Solid
            : timing <= window ? ContactQuality.Cheap
            : ContactQuality.Miss;

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

        var charge = input.ChargeSwing || input.Bat?.ChargeAlwaysFull == true ? 1.10 : 1.0;
        var qualityMul = quality switch
        {
            ContactQuality.Perfect => 1.10,
            ContactQuality.Solid => 1.0,
            _ => 0.58
        };
        var starSwingMul = input.UseStarSwing ? 1.06 : 1.0;
        var onBaseMul = _chem.ChargePowerMul(input.Batter, input.RunnersOn);

        var exit = 52 + power * 3.35;
        exit *= charge * qualityMul * starSwingMul * onBaseMul;
        if (input.PitcherStamina < 25)
            exit *= 1.05;

        var launch = quality == ContactQuality.Cheap
            ? (rng.NextDouble() < 0.5 ? 8 : 48)
            : 18 + (power - 5) * 1.4 + (rng.NextDouble() - 0.5) * 8;

        if (input.UseStarSwing)
            launch = StarLaunch(input.Batter.StarSwing, launch);

        if (input.UseStarSwing && input.Batter.StarSwing == "furnace")
            exit *= 1.08;

        var spray = input.SprayAimDeg + (rng.NextDouble() - 0.5) * SpraySpread(quality);
        if (!input.PitchInZone)
            spray += (rng.NextDouble() - 0.5) * 18;

        var carry = BallFlight.CarryFeet(exit, launch, park.WindMph);
        var fence = FenceAt(park, spray);
        var homer = carry >= fence && launch is > 18 and < 38;
        var foul = !homer && (Math.Abs(spray) > 45 || (quality == ContactQuality.Cheap && Math.Abs(spray) > 32 && rng.NextDouble() < 0.45));

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

    static double SpraySpread(ContactQuality q) => q switch
    {
        ContactQuality.Perfect => 8,
        ContactQuality.Solid => 18,
        _ => 36
    };

    public static double FenceAt(Park park, double sprayDeg)
    {
        // spray  -45 left, 0 center, +45 right. Piecewise lerp of the three fences.
        var t = Math.Clamp((sprayDeg + 45) / 90, 0, 1);
        if (t < 0.5)
            return Lerp(park.LeftFenceFt, park.CenterFenceFt, t * 2);
        return Lerp(park.CenterFenceFt, park.RightFenceFt, (t - 0.5) * 2);
    }

    static double StarLaunch(string swing, double fallback) => swing switch
    {
        "ground" => 8,
        "fly" => 38,
        "line" => 18,
        _ => fallback
    };

    static double Lerp(double a, double b, double t) => a + (b - a) * t;

    public static bool PitchInZone(PitchCommand pitch, int pitchStat)
    {
        var window = 5.5 + pitchStat * 0.35;
        if (pitch.Charge01 > 0.6) window *= 0.85;
        if (pitch.Star) window *= 0.9;
        return Math.Abs(pitch.TimingErrorFrames) <= window;
    }

    public static double PitchSpeedMph(PitchCommand pitch, int pitchStat)
    {
        var baseSpeed = pitch.Type switch
        {
            "changeup" => 72,
            "curve" => 76,
            _ => 86
        };
        var speed = baseSpeed + pitchStat * 0.9 + pitch.Charge01 * 8;
        if (pitch.Star) speed *= 1.12;
        return speed;
    }
}
