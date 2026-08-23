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

    public AtBatResult Resolve(AtBatInput input, Park park, Random rng, bool night = false)
    {
        var contact = input.Batter.Stats.Bat + (input.Bat?.ContactMod ?? 0);
        var power = input.Batter.Stats.Bat + (input.Bat?.PowerMod ?? 0);
        contact = Math.Clamp(contact, 1, 10);
        power = Math.Clamp(power, 1, 10);

        var window = BaseContactWindowFrames + (contact - 5) * 0.55;
        if (input.ChargeSwing && input.Bat?.ChargeAlwaysFull != true)
            window *= 0.78;
        if (input.UseStarPitch)
            window *= StarSkills.BatterWindowMul(input.Pitcher.StarPitch);
        window *= ParkHazards.ContactWindowMul(park, night);

        var timing = Math.Abs(input.TimingErrorFrames);
        var quality = timing <= PerfectWindowFrames ? ContactQuality.Perfect
            : timing <= window * 0.55 ? ContactQuality.Solid
            : timing <= window ? ContactQuality.Cheap
            : ContactQuality.Miss;

        if (input.UseStarPitch && input.Pitcher.StarPitch == "phonyball"
            && quality != ContactQuality.Perfect && rng.NextDouble() < 0.4)
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

        var charge = input.ChargeSwing || input.Bat?.ChargeAlwaysFull == true ? 1.10 : 1.0;
        var qualityMul = quality switch
        {
            ContactQuality.Perfect => 1.10,
            ContactQuality.Solid => 1.0,
            _ => 0.58
        };
        var starSwingMul = input.UseStarSwing ? StarSkills.SwingExitMul(input.Batter.StarSwing) : 1.0;
        var onBaseMul = _chem.ChargePowerMul(input.Batter, input.RunnersOn);

        var exit = 52 + power * 3.35;
        exit *= charge * qualityMul * starSwingMul * onBaseMul;
        if (input.PitcherStamina < 25)
            exit *= 1.05;

        // Late / under (positive frames) and stick-up (LaunchAim +) pull launch down into a hopper.
        // Early / over pops up. Square still mixes liners and some grounders.
        var signed = input.TimingErrorFrames;
        var loft = 16 + (power - 5) * 1.0 + (input.ChargeSwing ? 2.5 : 0);
        var launch = loft - signed * 1.35 - input.LaunchAim * 12 + (rng.NextDouble() - 0.5) * 14;
        if (quality == ContactQuality.Cheap)
            launch = signed >= 0
                ? 4 + rng.NextDouble() * 10
                : 40 + rng.NextDouble() * 12;

        if (input.Bunt)
        {
            exit *= 0.42;
            launch = 5 + rng.NextDouble() * 7;
        }
        launch = Math.Clamp(launch, 3, 52);

        if (input.UseStarSwing && !input.Bunt)
            launch = StarLaunch(input.Batter.StarSwing, launch);

        var spray = input.SprayAimDeg + (rng.NextDouble() - 0.5) * SpraySpread(quality);
        if (input.UseStarPitch && input.Pitcher.StarPitch == "prismball")
            spray += (rng.NextDouble() - 0.5) * 22;
        if (!input.PitchInZone)
            spray += (rng.NextDouble() - 0.5) * 18;

        var carry = BallFlight.CarryFeet(exit, launch, park.WindMph);
        var fence = FenceAt(park, spray);
        var homer = !input.Bunt && carry >= fence && launch is > 18 and < 38;
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
        if (Math.Abs(pitch.AimX) > 0.001 || Math.Abs(pitch.AimY) > 0.001)
        {
            var xLim = 0.58;
            var yLo = -0.42;
            var yHi = 0.58;
            if (pitch.Charge01 > 0.6)
            {
                xLim *= 0.9;
                yLo += 0.05;
                yHi -= 0.05;
            }
            if (pitch.Star) xLim *= 0.92;
            return Math.Abs(pitch.AimX) <= xLim && pitch.AimY >= yLo && pitch.AimY <= yHi;
        }

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
            "slider" => 80,
            _ => 86
        };
        var speed = baseSpeed + pitchStat * 0.9 + pitch.Charge01 * 8;
        if (pitch.Star) speed *= 1.12;
        return speed;
    }

    public static double PitchSpeedMph(PitchCommand pitch, Character pitcher)
    {
        var speed = PitchSpeedMph(pitch, pitcher.Stats.Pitch);
        if (!pitch.Star) return speed;
        return speed / 1.12 * StarSkills.PitchSpeedMul(pitcher.StarPitch);
    }
}
