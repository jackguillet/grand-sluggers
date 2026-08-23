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
                null);
        }

        var charge = input.ChargeSwing || input.Bat?.ChargeAlwaysFull == true ? 1.18 : 1.0;
        var qualityMul = quality switch
        {
            ContactQuality.Perfect => 1.22,
            ContactQuality.Solid => 1.0,
            _ => 0.62
        };
        var starSwingMul = input.UseStarSwing ? 1.12 : 1.0;
        var onBaseMul = _chem.ChargePowerMul(input.Batter, input.RunnersOn);

        var exit = 58 + power * 4.6;
        exit *= charge * qualityMul * starSwingMul * onBaseMul;
        if (input.PitcherStamina < 25)
            exit *= 1.06;

        var launch = quality == ContactQuality.Cheap
            ? (rng.NextDouble() < 0.5 ? 8 : 48)
            : 18 + (power - 5) * 1.4 + (rng.NextDouble() - 0.5) * 8;

        if (input.UseStarSwing)
            launch = StarLaunch(input.Batter.StarSwing, launch);

        var carry = BallFlight.CarryFeet(exit, launch, park.WindMph);
        var fence = FenceAt(park, 0); // pull/oppo spray comes later
        var homer = carry >= fence && launch is > 12 and < 44;

        return new AtBatResult(
            quality,
            InPlay: true,
            Strike: false,
            ExitVeloMph: Math.Round(exit, 1),
            LaunchDeg: Math.Round(launch, 1),
            CarryFt: Math.Round(carry, 1),
            HomeRun: homer,
            ChemistryItemOffered: _chem.ChemistryItemOffered(input.Batter, input.OnDeck),
            StarPitchUsed: input.UseStarPitch ? input.Pitcher.StarPitch : null,
            StarSwingUsed: input.UseStarSwing ? input.Batter.StarSwing : null);
    }

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
}

public static class BallFlight
{
    /// <summary>Toy ballistic with linear drag. Tuned so a 95 mph / 28° carry is ~380 ft in still air.</summary>
    public static double CarryFeet(double exitMph, double launchDeg, double windMph)
    {
        const double g = 32.174;
        var v = exitMph * 1.4667; // ft/s
        var a = launchDeg * Math.PI / 180.0;
        var vx = v * Math.Cos(a) + windMph * 1.4667 * 0.35;
        var vy = v * Math.Sin(a);
        var x = 0.0;
        var y = 2.5; // tee height, feet
        var dt = 1.0 / 120.0;
        var drag = 0.0019;
        for (var i = 0; i < 120 * 8; i++)
        {
            var speed = Math.Sqrt(vx * vx + vy * vy);
            vx -= drag * speed * vx * dt;
            vy -= (g + drag * speed * vy) * dt;
            x += vx * dt;
            y += vy * dt;
            if (i > 8 && y <= 0)
                break;
        }
        return x;
    }
}
