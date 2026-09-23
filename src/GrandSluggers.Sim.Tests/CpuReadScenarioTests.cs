using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The CPU batter commits from the flight as it stands (PH-18, spec §3 and §5.9), Appendix B.1
/// rows S-141 … S-143, plus S-04 and S-28 read through it.
///
/// The CPU reads <see cref="Match.CpuReadPitch"/>: the delivery with the stick's break frozen where
/// it stood at the commit instant. Zone, swing / take and the box follow that read; the umpire and the
/// bat still meet the ball that is thrown. The rule has no switch: the batting table refuses one.
///
/// No row stores a double: every claim compares two decisions to each other on the same seed.
/// </summary>
public sealed class CpuReadScenarioTests
{
    static readonly ContentCatalog Shipped = ContentCatalog.Load(new DataRoot(ContentCatalog.Load().Root.Shipped));

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
    public void S141_APitchSteeredAfterTheCommitIsDecidedFromThePreCommitRead()
    {
        for (var seed = 1; seed <= 60; seed++)
        {
            // The stick was centered at the commit (the steer began after it): the CPU saw the edge strike.
            var late = new Scenario(Shipped, seed).Match;
            var (edge, steered) = EdgeAndSteered(late);
            var decided = late.CpuSwing(steered, breakAtCommit: 0);

            // The same seed shown the unsteered pitch: the decision is the pre-commit read's, draw for draw.
            var read = new Scenario(Shipped, seed).Match.CpuSwing(edge);
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
    public void S141_TheLateSteeredPitchIsOfferedAtTheEdgeRateNotTheChaseRate()
    {
        var swings = 0;
        const int n = 300;
        for (var seed = 1; seed <= n; seed++)
        {
            var late = new Scenario(Shipped, seed).Match;
            var (_, steered) = EdgeAndSteered(late);
            if (late.CpuSwing(steered, breakAtCommit: 0).Swing) swings++;
        }
        // An edge strike is offered at edgeSwingChance with fewer than two strikes, far above the chase a ball earns.
        var edgeChance = Shipped.Rules.Batting.Cpu.EdgeSwingChance;
        Assert.InRange(swings / (double)n, edgeChance - 0.08, edgeChance + 0.08);
    }

    [Fact]
    public void S141_TheReadIsTheBreakSoFarAndNeverTheCommandsFuture()
    {
        // No caller watched the stick: it is taken as held one way from release, which reaches at most
        // BreakReach(Pitch, commit) — never more than the command carries, never another sign.
        foreach (var seed in new[] { 1, 2, 3, 4, 5 })
        {
            var match = new Scenario(Shipped, seed).Match;
            var (edge, _) = EdgeAndSteered(match);
            var airSec = PitchFlight.AirSeconds(match.PitchSpeedMph(edge), Shipped.Rules);
            var commit = AtBatMotion.CpuDecisionTime(airSec, Shipped.Rules);
            Assert.True(commit > 0 && commit < airSec, "the commit is inside the flight");
            var reach = PitchFlight.BreakReach(match.Pitcher.Stats.Pitch, commit, Shipped.Rules);
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
            for (var i = 0; i < steps; i++) held = PitchFlight.BreakStep(held, 1, 1 / 600.0, match.Pitcher.Stats.Pitch, Shipped.Rules);
            Assert.InRange(match.CpuReadPitch(edge with { BreakX = 1 }).BreakX - held, 0, 0.01);

            // A client that watched the stick hands it in; it wins over the derivation.
            Assert.Equal(-0.25, match.CpuReadPitch(edge with { BreakX = 1 }, breakAtCommit: -0.25).BreakX);
        }
    }

    // ---------------------------------------------------------------------------------
    // S-142  A steer held from release is read in full, and the read adds no draw
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S142_ASteerHeldFromReleaseIsReadInFullAndTheReadAddsNoDraw()
    {
        for (var seed = 1; seed <= 100; seed++)
        {
            var unwatched = new Scenario(Shipped, seed).Match;
            var watched = new Scenario(Shipped, seed).Match;
            var (_, steered) = EdgeAndSteered(unwatched);
            EdgeAndSteered(watched);
            // S-04's full steer held one way from release has reached the whole ±1 by the commit (S-117): the
            // unwatched read is the stick watched at full. Five decisions in a row: the same commands in the
            // same order, so the read draws nothing of its own.
            Assert.Equal(steered, unwatched.CpuReadPitch(steered));
            for (var i = 0; i < 5; i++)
                Assert.Equal(unwatched.CpuSwing(steered), watched.CpuSwing(steered, breakAtCommit: 1));
            // The read pitch is pure: no draw, no state.
            Assert.Equal(steered with { BreakX = 0 }, unwatched.CpuReadPitch(steered, breakAtCommit: 0));
        }
    }

    // ---------------------------------------------------------------------------------
    // S-143  The read has no switch
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S143_TheBattingTableOwnsNoReadSwitch()
    {
        var opts = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip };
        var shipped = JsonNode.Parse(File.ReadAllText(Path.Combine(Shipped.Root.Shipped, "rules", "batting.json")), documentOptions: opts)!;
        Assert.False(shipped["cpu"]!.AsObject().ContainsKey("commitRead"));

        using var fixture = new ContentFixture();
        var path = fixture.Path("rules/batting.json");
        var json = JsonNode.Parse(File.ReadAllText(path), documentOptions: opts)!;
        json["cpu"]!["commitRead"] = false;
        File.WriteAllText(path, json.ToJsonString());
        Assert.Contains(RulesTable.Validate(new DataRoot(fixture.Root)),
            e => e.Contains("batting.cpu.commitRead is not a rule this table owns", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------
    // S-04 and S-28 read through the commit
    // ---------------------------------------------------------------------------------

    [Fact]
    [Trait("Kind", "Balance")]
    public void S04_TheCpuTakesAPitchSteeredOutBeforeTheCommitAndNotOneSteeredAfter()
    {
        // Steered out before the commit (the watched stick already at full): S-04's take at (100 − chase)%.
        // Steered out after it (the stick still centered): the edge strike S-141 reads, offered far more often.
        var takesBefore = 0;
        var takesAfter = 0;
        const int n = 300;
        double chase = 0;
        for (var seed = 1; seed <= n; seed++)
        {
            var before = new Scenario(Shipped, seed).Match;
            chase = (Shipped.Rules.Batting.Cpu.ChaseBase - before.Batter.Stats.Bat) / 100.0;
            var (_, steered) = EdgeAndSteered(before);
            if (!before.CpuSwing(steered, breakAtCommit: 1).Swing) takesBefore++;
            var after = new Scenario(Shipped, seed).Match;
            if (!after.CpuSwing(steered, breakAtCommit: 0).Swing) takesAfter++;
        }
        Assert.InRange(takesBefore / (double)n, 1 - chase - 0.08, 1 - chase + 0.08);
        Assert.True(takesAfter < takesBefore / 2, $"after {takesAfter} vs before {takesBefore}");
    }

    [Fact]
    public void S28_AnUnsteeredMeatballIsReadAsItselfAndTheReadHasNoSideEffects()
    {
        // No stick, no future to hide: the read is the pitch, whatever the watched stick says of a centered stick.
        for (var seed = 1; seed <= 100; seed++)
        {
            var meat = Scenario.PitchAt(0, CenterY);
            var unwatched = Match.Exhibition(Shipped, "rio", "ashlord", seed: seed).CpuSwing(meat);
            var watched = Match.Exhibition(Shipped, "rio", "ashlord", seed: seed).CpuSwing(meat, breakAtCommit: 0);
            Assert.Equal(unwatched, watched);
        }

        var s = new Scenario(Shipped, seed: 4).Runner(1, 3);
        var match = s.Match;
        var box = match.BatterOffsetX;
        var rubber = match.PitcherOffsetX;
        var stream = s.Stream();
        for (var i = 0; i < 50; i++)
            match.CpuSwing(Scenario.PitchAt(0.3, CenterY) with { BreakX = 1 });
        Assert.Equal(box, match.BatterOffsetX);
        Assert.Equal(rubber, match.PitcherOffsetX);
        Assert.Equal(stream, s.Stream());
    }
}
