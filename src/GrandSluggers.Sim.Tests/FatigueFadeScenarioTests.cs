using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Gradual fatigue (P3-c, PH-08, PH-08-R1, spec §4.7), Appendix B.1 rows S-147 … S-149. The arm loses speed and
/// steering room a little with every pitch below <c>pitching.stamina.fadeFrom</c>, reaching
/// <c>exhaustedMph</c> and <c>tiredBreakMul</c> at an empty pool. It is never a random miss, and TIRED is
/// only the label and the CPU's swap trigger.
/// </summary>
public sealed class FatigueFadeScenarioTests
{
    static readonly ContentCatalog Content = ContentCatalog.Load();

    static IEnumerable<int> Pools => Enumerable.Range(-40, 200);

    // ---------------------------------------------------------------------------------
    // S-147  The curve: fresh at fadeFrom, the table's empty arm at 0, a share per point between
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S147_TheArmLosesSpeedAndSteeringAShareAPointFromFadeFromToEmpty()
    {
        var st = Content.Rules.Pitching.Stamina;
        Assert.True(st.FadeFrom > st.TiredBelow, "the fade shows before TIRED does");

        double? lastMph = null, lastBreak = null;
        foreach (var s in Pools)
        {
            var mph = st.MphLost(s);
            var brk = st.BreakMul(s);
            if (s >= st.FadeFrom) Assert.Equal((0.0, 1.0), (mph, brk));
            if (s <= 0) Assert.Equal((st.ExhaustedMph, st.TiredBreakMul), (mph, brk));
            Assert.InRange(mph, 0, st.ExhaustedMph);
            Assert.InRange(brk, st.TiredBreakMul, 1);
            // Gradual: one point of pool moves each effect by one share of the fade, and never back.
            if (lastMph is { } m && lastBreak is { } b)
            {
                Assert.InRange(m - mph, 0, st.ExhaustedMph / st.FadeFrom + 1e-12);
                Assert.InRange(brk - b, 0, (1 - st.TiredBreakMul) / st.FadeFrom + 1e-12);
            }
            lastMph = mph;
            lastBreak = brk;
        }
    }

    // ---------------------------------------------------------------------------------
    // S-148  The pitch reads the curve, inside D7's range, and slows before TIRED
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S148_ThePitchReadsTheCurveStaysInsideTheFreshAndEmptyAirTimesAndSlowsBeforeTired()
    {
        var st = Content.Rules.Pitching.Stamina;
        foreach (var family in new[] { PitchFamily.Fastball, PitchFamily.Changeup })
        {
            var match = new Scenario(Content).Match;
            var fresh = match.PitchSpeedMph(Scenario.Paint with { Type = family });
            var freshAir = PitchFlight.AirSeconds(fresh, Content.Rules);
            var emptyAir = PitchFlight.AirSeconds(fresh - st.ExhaustedMph, Content.Rules);
            var outside = Scenario.PitchAt(2.5, StrikeZoneGeometry.CenterY);
            var sawFadeBeforeTired = false;
            for (var guard = 0; guard < 80 && match.PitcherStamina > -10; guard++)
            {
                var pool = match.PitcherStamina;
                var mph = match.PitchSpeedMph(Scenario.Paint with { Type = family });
                Assert.Equal(fresh - st.MphLost(pool), mph, 9);
                Assert.InRange(PitchFlight.AirSeconds(mph, Content.Rules), freshAir, emptyAir);
                var ready = match.PreparePitch(Scenario.Paint with { Type = family, BreakX = 1 });
                Assert.Equal(st.BreakMul(pool), ready.BreakMul, 12);
                Assert.Equal((0.0, 0.0), (ready.AimX, ready.AimY));
                sawFadeBeforeTired |= !match.PitcherTired && mph < fresh;
                match.Play(outside, Scenario.Take);
            }
            Assert.True(match.PitcherExhausted, $"{family}: the arm was worked to empty");
            Assert.True(sawFadeBeforeTired, $"{family}: the arm slowed before the TIRED label");
        }
    }

    // ---------------------------------------------------------------------------------
    // S-149  A fade that starts at empty is refused
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S149_AFadeFromOfZeroIsRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("rules/pitching.json", json => json["stamina"]!["fadeFrom"] = 0);
        Assert.Contains(RulesTable.Validate(new DataRoot(fixture.Root)), e => e.Contains("fadeFrom", StringComparison.Ordinal));
    }
}
