using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Gradual fatigue (P3-c, PH-08, PH-08-R1, spec §4.7), Appendix B.1 rows S-147 … S-149.
///
/// <c>pitching.stamina.fadeFrom</c> is 0 on the shipped root, where fatigue is the step at TIRED and at empty,
/// and 50 in <c>trials/fatigue</c>, where the arm loses speed and steering room a little with every pitch
/// below 50. Both roots are loaded in process, so nothing here depends on <c>GRAND_SLUGGERS_TRIAL</c>.
/// </summary>
public sealed class FatigueFadeScenarioTests
{
    static readonly ContentCatalog Shipped = ContentCatalog.Load(new DataRoot(ContentCatalog.Load().Root.Shipped));
    static readonly string TrialDir = Path.GetFullPath(Path.Combine(Shipped.Root.Shipped, "..", "trials", "fatigue"));
    static readonly ContentCatalog Trial = ContentCatalog.Load(new DataRoot(Shipped.Root.Shipped, TrialDir));

    static IEnumerable<int> Pools => Enumerable.Range(-40, 200);

    // ---------------------------------------------------------------------------------
    // S-147  Off, the step is the step
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S147_OnTheShippedRootFatigueIsTheStepAtTiredAndAtEmpty()
    {
        var st = Shipped.Rules.Pitching.Stamina;
        Assert.Equal(0, st.FadeFrom);
        foreach (var s in Pools)
        {
            Assert.Equal(s < 0 ? st.ExhaustedMph : s < st.TiredBelow ? st.TiredMph : 0, st.MphLost(s));
            Assert.Equal(s < st.TiredBelow ? st.TiredBreakMul : 1, st.BreakMul(s));
            Assert.Equal(0, st.Fade(s));
        }
    }

    // ---------------------------------------------------------------------------------
    // S-148  On, the arm fades a little with every pitch, inside the step's own range
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S148_UnderTheTrialTheArmLosesSpeedAndSteeringGraduallyAndNeverPastTheStep()
    {
        var st = Trial.Rules.Pitching.Stamina;
        var shipped = Shipped.Rules.Pitching.Stamina;
        Assert.True(st.FadeFrom > st.TiredBelow, "the fade shows before TIRED does");

        double? lastMph = null, lastBreak = null;
        foreach (var s in Pools)
        {
            var mph = st.MphLost(s);
            var brk = st.BreakMul(s);
            // Fresh above the fade, empty at the bottom: the ends are the step's own ends.
            if (s >= st.FadeFrom) Assert.Equal((0.0, 1.0), (mph, brk));
            if (s <= 0) Assert.Equal((st.ExhaustedMph, st.TiredBreakMul), (mph, brk));
            // Never past what the shipped step can already do (D7: pace stays in the shipped range).
            Assert.InRange(mph, 0, shipped.ExhaustedMph);
            Assert.InRange(brk, shipped.TiredBreakMul, 1);
            // Gradual: one point of pool moves each effect by one share of the fade, and never back.
            if (lastMph is { } m && lastBreak is { } b)
            {
                Assert.InRange(m - mph, 0, st.ExhaustedMph / st.FadeFrom + 1e-12);
                Assert.InRange(brk - b, 0, (1 - st.TiredBreakMul) / st.FadeFrom + 1e-12);
            }
            lastMph = mph;
            lastBreak = brk;
        }

        // The pitch that crosses the plate reads the curve: no family flies faster or slower than the
        // shipped step's own fresh and empty arms would throw it.
        foreach (var family in new[] { PitchFamily.Fastball, PitchFamily.Changeup })
        {
            var match = new Scenario(Trial).Match;
            var fresh = match.PitchSpeedMph(Scenario.Paint with { Type = family });
            var freshAir = PitchFlight.AirSeconds(fresh, Trial.Rules);
            var emptyAir = PitchFlight.AirSeconds(fresh - shipped.ExhaustedMph, Trial.Rules);
            var outside = Scenario.PitchAt(2.5, StrikeZoneGeometry.CenterY);
            var sawFadeBeforeTired = false;
            for (var guard = 0; guard < 80 && match.PitcherStamina > -10; guard++)
            {
                var pool = match.PitcherStamina;
                var mph = match.PitchSpeedMph(Scenario.Paint with { Type = family });
                Assert.Equal(fresh - st.MphLost(pool), mph, 9);
                Assert.InRange(PitchFlight.AirSeconds(mph, Trial.Rules), freshAir, emptyAir);
                var ready = match.PreparePitch(Scenario.Paint with { Type = family, BreakX = 1 });
                Assert.Equal(st.BreakMul(pool), ready.BreakMul, 12);
                Assert.Equal((0.0, 0.0), (ready.AimX, ready.AimY));
                sawFadeBeforeTired |= !match.PitcherTired && mph < fresh;
                match.Play(outside, Scenario.Take);
            }
            Assert.True(sawFadeBeforeTired, $"{family}: the arm slowed before the TIRED label");
        }
    }

    // ---------------------------------------------------------------------------------
    // S-149  The overlay is the shipped file with the one switch on
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S149_TheOverlayIsTheShippedPitchingFileWithTheFadeOn()
    {
        var files = Directory.GetFiles(TrialDir, "*.json", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(TrialDir, f).Replace('\\', '/')).ToArray();
        Assert.Equal(["rules/pitching.json"], files);

        var opts = new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip };
        var shipped = JsonNode.Parse(File.ReadAllText(Path.Combine(Shipped.Root.Shipped, "rules", "pitching.json")), documentOptions: opts)!;
        var trial = JsonNode.Parse(File.ReadAllText(Path.Combine(TrialDir, "rules", "pitching.json")), documentOptions: opts)!;
        Assert.Equal(0, shipped["stamina"]!["fadeFrom"]!.GetValue<int>());
        Assert.Equal(50, trial["stamina"]!["fadeFrom"]!.GetValue<int>());
        trial["stamina"]!["fadeFrom"] = 0;
        Assert.Equal(shipped.ToJsonString(), trial.ToJsonString());
    }
}
