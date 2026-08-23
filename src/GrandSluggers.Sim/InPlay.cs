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

    public static double HomeToFirstSec(Character batter) =>
        Math.Clamp(4.32 - batter.Stats.Run * 0.13, 2.9, 4.35);

    public static double ThrowSec(double distFt, ThrowResult? thr)
    {
        var fps = 56 * (thr?.SpeedMul ?? 1);
        return 0.22 + distFt / Math.Max(32, fps);
    }

    /// <summary>True if the batter reaches first before the throw after a scoop at the landing.</summary>
    public static bool BatterBeatsThrow(Character batter, AtBatResult hit, FieldingResult field)
    {
        if (field.Kind != PlayKind.GroundOut || field.Fielder is null) return false;
        var run = HomeToFirstSec(batter);
        var already = field.HangTimeSec;
        var left = run - already;
        if (left <= 0) return true;
        var dist = Diamond.Dist(field.LandingX, field.LandingZ, Diamond.First.X, Diamond.First.Z);
        var tThrow = ThrowSec(dist, field.Throw) + KnockbackSec(Energy(hit), field.Fielder);
        return left < tThrow;
    }

    /// <summary>
    /// Bags to throw in order on a hopper. Force at second, then first when the batter is out.
    /// Empty when the batter already beat the play and nobody is on first.
    /// </summary>
    public static int[] GroundThrowBags(bool firstOccupied, bool batterBeatsThrow)
    {
        if (firstOccupied)
            return batterBeatsThrow ? [2] : [2, 1];
        return batterBeatsThrow ? [] : [1];
    }
}
