using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The CPU batter plays by the same table whoever is pitching (spec §5.9). The forced-miss clamp
/// against a human (|err| ≥ 3.2, 32% meatball takes) is gone; S-28 in the scenario harness pins
/// the perfect rate. The §5.9 tracking table lands with the CPU tables (P1 part c).
/// </summary>
public class CpuBatterTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    /// <summary>A normal fastball crossing at the zone center: the human's meatball.</summary>
    static PitchCommand Meatball => Scenario.PitchAt(0, StrikeZoneGeometry.CenterY);

    [Fact]
    public void NoForcedMissClampAgainstAHumanMeatball()
    {
        const int n = 200;
        var swings = 0;
        var squareErrors = 0;
        var perfect = 0;
        var misses = 0;
        for (var seed = 1; seed <= n; seed++)
        {
            var match = Match.Exhibition(_content, "rio", "ashlord", seed: seed);
            Assert.True(match.Top, "human pitches the top");
            var inZone = AtBatResolver.PitchInZone(Meatball, match.Pitcher.Stats.Pitch);
            Assert.True(inZone, "middle-middle uncharged fastball is in the zone");
            var swing = match.CpuSwing(Meatball, inZone);
            if (!swing.Swing) continue;
            swings++;
            if (Math.Abs(swing.TimingErrorFrames) < 3.2) squareErrors++;
            var ev = match.Play(Meatball, swing);
            if (ev.AtBat.Quality == ContactQuality.Perfect) perfect++;
            if (ev.Kind == PlayKind.SwingMiss) misses++;
        }
        Assert.True(swings >= n * 0.9, $"the table swings at a strike: {swings} of {n}");
        Assert.True(squareErrors > swings / 3, $"timing errors are a σ around square, not floored at 3.2: {squareErrors} of {swings}");
        Assert.True(perfect > 0, "a meatball can be met perfectly");
        Assert.True(misses > 0, "the σ still misses sometimes");
    }

    [Fact]
    public void AutoPlayArcadeStillSwingsInTheZone()
    {
        var swings = 0;
        var extra = 0;
        for (var seed = 1; seed <= 40; seed++)
        {
            var match = Match.Exhibition(_content, "rio", "ashlord", seed: seed);
            var inZone = AtBatResolver.PitchInZone(Meatball, match.Pitcher.Stats.Pitch);
            var swing = match.CpuSwing(Meatball, inZone);
            if (swing.Swing) swings++;
            var ev = match.Play(Meatball, swing);
            if (ev.Kind is PlayKind.Double or PlayKind.Triple or PlayKind.HomeRun)
                extra++;
        }
        Assert.True(swings >= 35, $"arcade swings {swings}");
        Assert.True(extra > 0, "arcade meatballs can still extra-base");
    }
}
