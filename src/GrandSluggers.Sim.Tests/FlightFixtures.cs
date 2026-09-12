using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Real batted balls for tests: every fixture is a contact (exit, launch, spray) whose facts come
/// from the one flight (<see cref="BattedBall.Of(AtBatResult, Park, RulesTable?)"/>), never a
/// carry typed next to a launch it could not produce. A fixture that names a landing distance
/// finds the exit velocity that lands there in the open field, so the ball then meets this park's
/// fence by geometry.
/// </summary>
public static class FlightFixtures
{
    /// <summary>A contact judged by the flight in this park.</summary>
    public static AtBatResult Hit(Park park, double exitMph, double launchDeg, double sprayDeg,
        ContactQuality quality = ContactQuality.Nice, bool bunt = false, RulesTable? rules = null)
    {
        var ball = BattedBall.Of(exitMph, launchDeg, sprayDeg, bunt, park, rules);
        return new AtBatResult(quality, !ball.Foul, false, exitMph, launchDeg, Math.Round(ball.LandingDist, 1),
            ball.HomeRun, false, null, null, SprayDeg: sprayDeg, Foul: ball.Foul, Class: ball.Class);
    }

    /// <summary>The exit velocity whose open-field carry at this launch is <paramref name="carryFt"/> (bisected on the flight).</summary>
    public static double ExitForCarry(double carryFt, double launchDeg, RulesTable? rules = null)
    {
        var lo = 20.0;
        var hi = 160.0;
        for (var i = 0; i < 40; i++)
        {
            var mid = (lo + hi) * 0.5;
            if (BallFlight.CarryFeet(mid, launchDeg, 0, rules) < carryFt) lo = mid;
            else hi = mid;
        }
        return Math.Round((lo + hi) * 0.5, 1);
    }

    /// <summary>A contact that would land <paramref name="carryFt"/> out in the open, judged in this park.</summary>
    public static AtBatResult Landing(Park park, double carryFt, double launchDeg, double sprayDeg,
        ContactQuality quality = ContactQuality.Nice, RulesTable? rules = null) =>
        Hit(park, ExitForCarry(carryFt, launchDeg, rules), launchDeg, sprayDeg, quality, rules: rules);

    /// <summary>A fly clearing the fence at <paramref name="sprayDeg"/> by about <paramref name="clearFt"/> (bisected on the clearance).</summary>
    public static AtBatResult OverTheFence(Park park, double clearFt, double sprayDeg, double launchDeg = 30, RulesTable? rules = null)
    {
        var lo = 60.0;
        var hi = 170.0;
        for (var i = 0; i < 40; i++)
        {
            var mid = (lo + hi) * 0.5;
            var ball = BattedBall.Of(mid, launchDeg, sprayDeg, park, rules);
            var clear = ball.HomeRun ? ball.FenceClearFt : ball.WallT is not null ? ball.FenceClearFt : double.NegativeInfinity;
            if (clear < clearFt) lo = mid;
            else hi = mid;
        }
        return Hit(park, Math.Round((lo + hi) * 0.5, 1), launchDeg, sprayDeg, ContactQuality.Perfect, rules: rules);
    }

    /// <summary>A preview of any class for a glove, with the landing at (x, z). For pure geometry tests.</summary>
    public static FieldingPreview Preview(Character who, string pos, BattedBallClass shape, double hang, double x, double z,
        Character? buddy = null, double radius = 14, bool foul = false) =>
        new(who, pos, buddy, hang, x, z, shape, false, false, false, radius, Foul: foul);
}
