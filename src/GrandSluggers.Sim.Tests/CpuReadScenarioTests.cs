using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The CPU batter commits from the flight as it stands (#892, PH-18, spec §3 and §5.9), Appendix B.1
/// rows S-141 … S-143, plus S-04 and S-28 under the switch.
///
/// <c>batting.cpu.commitRead</c> is off on the shipped root, where the CPU reads the final crossing at
/// the plate plane, and on in <c>trials/cpu-read</c>, where it reads <see cref="Match.CpuReadPitch"/>:
/// the delivery with the stick's break frozen where it stood at the commit instant. Both roots are
/// loaded in process, so nothing here depends on <c>GRAND_SLUGGERS_TRIAL</c>.
///
/// No row stores a double: every claim compares two decisions to each other on the same seed.
/// </summary>
public sealed class CpuReadScenarioTests
{
    static readonly ContentCatalog Shipped = ContentCatalog.Load(new DataRoot(ContentCatalog.Load().Root.Shipped));
    static readonly string TrialDir = Path.GetFullPath(Path.Combine(Shipped.Root.Shipped, "..", "trials", "cpu-read"));
    static readonly ContentCatalog Trial = ContentCatalog.Load(new DataRoot(Shipped.Root.Shipped, TrialDir));

    static double CenterY => StrikeZoneGeometry.CenterY;

    /// <summary>An edge strike that full break carries out of the zone (S-04's pitch).</summary>
    static (PitchCommand Edge, PitchCommand Steered) EdgeAndSteered(Match match)
    {
        var edge = match.PreparePitch(Scenario.PitchAt(StrikeZoneGeometry.HalfWidth - 0.1, CenterY));
        var steered = edge with { BreakX = 1 };
        Assert.True(StrikeZoneGeometry.Contains(edge), "the launched pitch is a strike");
        Assert.False(StrikeZoneGeometry.Contains(steered), "full break carries it out");
        return (edge, steered);
    }

    // ---------------------------------------------------------------------------------
    // S-141  A steer after the commit beats the read
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S138_UnderTheTrialAPitchSteeredAfterTheCommitIsDecidedFromThePreCommitRead()
    {
        Assert.True(Trial.Rules.Batting.Cpu.CommitRead);
        for (var seed = 1; seed <= 60; seed++)
        {
            // The stick was centered at the commit (the steer began after it): the CPU saw the edge strike.
            var late = new Scenario(Trial, seed).Match;
            var (edge, steered) = EdgeAndSteered(late);
            var decided = late.CpuSwing(steered, inZone: false, breakAtCommit: 0);

            // The same seed shown the unsteered pitch: the decision is the pre-commit read's, draw for draw.
            var read = new Scenario(Trial, seed).Match.CpuSwing(edge, inZone: true);
            Assert.Equal(read, decided);

            // The ball that is thrown still crosses where the steer took it: the umpire judges the final crossing.
            if (seed <= 20)
            {
                var ev = late.Play(steered, decided);
                Assert.False(ev.AtBat.InZone);
            }
        }
    }

    [Fact]
    [Trait("Kind", "Balance")]
    public void S138_TheLateSteeredPitchIsOfferedAtTheEdgeRateNotTheChaseRate()
    {
        var swings = 0;
        const int n = 300;
        for (var seed = 1; seed <= n; seed++)
        {
            var late = new Scenario(Trial, seed).Match;
            var (_, steered) = EdgeAndSteered(late);
            if (late.CpuSwing(steered, inZone: false, breakAtCommit: 0).Swing) swings++;
        }
        // An edge strike is offered at edgeSwingChance with fewer than two strikes, far above the chase a ball earns.
        var edgeChance = Trial.Rules.Batting.Cpu.EdgeSwingChance;
        Assert.InRange(swings / (double)n, edgeChance - 0.08, edgeChance + 0.08);
    }

    [Fact]
    public void S138_TheReadIsTheBreakSoFarAndNeverTheCommandsFuture()
    {
        // No caller watched the stick: it is taken as held one way from release, which reaches at most
        // BreakReach(Pitch, commit) — never more than the command carries, never another sign.
        foreach (var seed in new[] { 1, 2, 3, 4, 5 })
        {
            var match = new Scenario(Trial, seed).Match;
            var (edge, _) = EdgeAndSteered(match);
            var airSec = PitchFlight.AirSeconds(match.PitchSpeedMph(edge), Trial.Rules);
            var commit = AtBatMotion.CpuDecisionTime(airSec, Trial.Rules);
            Assert.True(commit > 0 && commit < airSec, "the commit is inside the flight");
            var reach = PitchFlight.BreakReach(match.Pitcher.Stats.Pitch, commit, Trial.Rules);
            foreach (var b in new[] { -1.0, -0.4, 0.3, 1.0 })
            {
                var read = match.CpuReadPitch(edge with { BreakX = b });
                Assert.Equal(Math.Sign(b), Math.Sign(read.BreakX));
                Assert.Equal(Math.Min(Math.Abs(b), reach), Math.Abs(read.BreakX));
                Assert.Equal(edge with { BreakX = read.BreakX }, read);
            }

            // Frame by frame, a stick held from release arrives at the same break by the commit.
            var held = 0.0;
            var steps = (int)Math.Floor(commit * 600);
            for (var i = 0; i < steps; i++) held = PitchFlight.BreakStep(held, 1, 1 / 600.0, match.Pitcher.Stats.Pitch, Trial.Rules);
            Assert.InRange(match.CpuReadPitch(edge with { BreakX = 1 }).BreakX - held, 0, 0.01);

            // A client that watched the stick hands it in; it wins over the derivation.
            Assert.Equal(-0.25, match.CpuReadPitch(edge with { BreakX = 1 }, breakAtCommit: -0.25).BreakX);
        }
    }

    // ---------------------------------------------------------------------------------
    // S-142  Shipped: identical to today
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S139_OnTheShippedRootTheSwitchIsOffAndTheCallerReadIsUntouched()
    {
        Assert.False(Shipped.Rules.Batting.Cpu.CommitRead);
        for (var seed = 1; seed <= 100; seed++)
        {
            var a = new Scenario(Shipped, seed).Match;
            var b = new Scenario(Shipped, seed).Match;
            var (_, steered) = EdgeAndSteered(a);
            EdgeAndSteered(b);
            // Five decisions in a row: the same commands means the same draws, whatever the watched stick says.
            for (var i = 0; i < 5; i++)
            {
                var today = a.CpuSwing(steered, inZone: false);
                var ignored = b.CpuSwing(steered, inZone: false, breakAtCommit: 0);
                Assert.Equal(today, ignored);
            }
            // The read pitch is a pure helper; it is simply not consulted with the switch off.
            Assert.Equal(steered, a.CpuReadPitch(steered, breakAtCommit: 1));
        }
    }

    // ---------------------------------------------------------------------------------
    // S-143  The switch and the overlay
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S140_TheOverlayIsTheShippedBattingFileWithTheOneSwitchOn()
    {
        var files = Directory.GetFiles(TrialDir, "*.json", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(TrialDir, f).Replace('\\', '/')).ToArray();
        Assert.Equal(["rules/batting.json"], files);

        var opts = new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip };
        var shipped = JsonNode.Parse(File.ReadAllText(Path.Combine(Shipped.Root.Shipped, "rules", "batting.json")), documentOptions: opts)!;
        var trial = JsonNode.Parse(File.ReadAllText(Path.Combine(TrialDir, "rules", "batting.json")), documentOptions: opts)!;
        Assert.False(shipped["cpu"]!["commitRead"]!.GetValue<bool>());
        Assert.True(trial["cpu"]!["commitRead"]!.GetValue<bool>());
        trial["cpu"]!["commitRead"] = false;
        Assert.Equal(shipped.ToJsonString(), trial.ToJsonString());
    }

    // ---------------------------------------------------------------------------------
    // S-04 and S-28 follow the switch
    // ---------------------------------------------------------------------------------

    [Fact]
    [Trait("Kind", "Balance")]
    public void S04_UnderTheTrialTheCpuTakesAPitchSteeredOutBeforeTheCommitAndNotOneSteeredAfter()
    {
        // Steered out before the commit (the watched stick already at full): S-04's take at (100 − chase)%.
        // Steered out after it (the stick still centered): the edge strike S-141 reads, offered far more often.
        var takesBefore = 0;
        var takesAfter = 0;
        const int n = 300;
        double chase = 0;
        for (var seed = 1; seed <= n; seed++)
        {
            var before = new Scenario(Trial, seed).Match;
            chase = (Trial.Rules.Batting.Cpu.ChaseBase - before.Batter.Stats.Bat) / 100.0;
            var (_, steered) = EdgeAndSteered(before);
            if (!before.CpuSwing(steered, inZone: true, breakAtCommit: 1).Swing) takesBefore++;
            var after = new Scenario(Trial, seed).Match;
            if (!after.CpuSwing(steered, inZone: false, breakAtCommit: 0).Swing) takesAfter++;
        }
        Assert.InRange(takesBefore / (double)n, 1 - chase - 0.08, 1 - chase + 0.08);
        Assert.True(takesAfter < takesBefore / 2, $"after {takesAfter} vs before {takesBefore}");
    }

    [Fact]
    public void S28_UnderTheTrialAnUnsteeredMeatballIsTheSameDecisionAndStillHasNoSideEffects()
    {
        // No stick, no future to hide: the trial's read is the shipped read, command for command.
        for (var seed = 1; seed <= 100; seed++)
        {
            var meat = Scenario.PitchAt(0, CenterY);
            var shipped = Match.Exhibition(Shipped, "rio", "ashlord", seed: seed).CpuSwing(meat, true);
            var trial = Match.Exhibition(Trial, "rio", "ashlord", seed: seed).CpuSwing(meat, true);
            Assert.Equal(shipped, trial);
        }

        var s = new Scenario(Trial, seed: 4).Runner(1, 3);
        var match = s.Match;
        var box = match.BatterOffsetX;
        var rubber = match.PitcherOffsetX;
        var stream = s.Stream();
        for (var i = 0; i < 50; i++)
            match.CpuSwing(Scenario.PitchAt(0.3, CenterY) with { BreakX = 1 }, true);
        Assert.Equal(box, match.BatterOffsetX);
        Assert.Equal(rubber, match.PitcherOffsetX);
        Assert.Equal(stream, s.Stream());
    }
}
