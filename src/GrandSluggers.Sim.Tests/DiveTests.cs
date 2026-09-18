using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-3 slice 2 (#719: F693-02-dive-jump-scoop-reach, -dive-recovery-cost, -cpu-dive-intent, -cpu-dive-intent-policy):
/// nobody dives for free. On the <c>c80</c> copy the neutral stick's assistance never dives, the CPU commits on the live ball
/// at the last makeable moment and can miss, and every dive — East or the CPU's, caught or missed — costs its recovery
/// before the diver moves or throws. The shipped table keeps the free automatic dive and no cost.
/// </summary>
public sealed class DiveTests
{
    static readonly ContentCatalog Control = ContentCatalog.Load();
    static readonly DataRoot Root = new(Control.Root.Shipped, Path.GetFullPath(Path.Combine(Control.Root.Shipped, "..", "trials", "c80")));
    static readonly ContentCatalog Trial = ContentCatalog.Load(Root);
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    [Fact]
    public void TheShippedDiveIsFreeAndAutomaticAndTheTrialsIsNeither()
    {
        var s = Control.Rules.Fielding.Catch;
        Assert.Equal((1.0, 0.0, 0.0), (s.AutoDive, s.DiveRecoverySec, s.DiveRecoveryFieldCut));
        var t = Trial.Rules.Fielding.Catch;
        Assert.Equal((0.0, 0.60, 0.025), (t.AutoDive, t.DiveRecoverySec, t.DiveRecoveryFieldCut));

        // 0.60 at Field 1, 2.5 % of it less per point: 0.54 at 5, 0.465 at 10. Nothing on the shipped table.
        double Cost(ContentCatalog c, int field) => FieldingResolver.DiveRecoverySec(c.Must("ashlord") with { Stats = c.Must("ashlord").Stats with { Field = field } }, c.Rules);
        Assert.Equal(0.60, Cost(Trial, 1), 9);
        Assert.Equal(0.54, Cost(Trial, 5), 9);
        Assert.Equal(0.465, Cost(Trial, 10), 9);
        Assert.Equal(0.555, FieldingResolver.DiveRecoverySec(Trial.Must("moss"), Trial.Rules), 9);   // Field 4, the centre fielder below
        Assert.Equal(0, Cost(Control, 1));
        Assert.Equal(0, Cost(Control, 10));
    }

    /// <summary>
    /// A 280-ft liner into the left-centre gap. The shipped centre fielder pays nothing for whatever he does; the trial's
    /// commits on the live ball a frame before it comes down to dive height, pays 0.555 s, and takes it — a dive, not a
    /// stand-up, though the lunge carried him under the ring.
    /// </summary>
    [Fact]
    public void TheTrialCpuCommitsOnTheLiveBallPaysAndTakesTheLiner()
    {
        var control = RunCpu(Control, out var controlCommits, carry: 280);
        Assert.Empty(controlCommits);
        Assert.Equal(0, control.MaxRecovery);

        var trial = RunCpu(Trial, out var commits);
        var commit = Assert.Single(commits);
        Assert.Equal("CF", commit.Pos);   // moss, Field 4
        Assert.Equal(0.555, commit.Cost, 6);
        Assert.InRange(commit.T, 1.6, 2.3);
        Assert.Equal(PlayKind.FlyOut, trial.Play.Kind);
        Assert.Equal(DefensiveFeat.Dive, trial.Play.Outcome?.DefensiveFeat);
        Assert.True(trial.PossessionAt >= commit.T - 1e-9, "the ball was taken at or after the commitment, never before");
    }

    /// <summary>
    /// The same liner, moved 16 ft further into the gap the frame after the centre fielder committed: the dive misses, the ball
    /// lands and stays live, the diver stands where he lunged for the whole recovery, and the recovery is the same 0.555 s the
    /// catch would have cost.
    /// </summary>
    [Fact]
    public void AMovedBallBeatsTheCommittedDiveAndTheRecoveryIsOwedAllTheSame()
    {
        var trial = RunCpu(Trial, out var commits, nudgeOnCommit: (-16, 0));
        var commit = Assert.Single(commits);
        Assert.Equal(0.555, commit.Cost, 6);
        Assert.NotEqual(PlayKind.FlyOut, trial.Play.Kind);
        Assert.NotEqual(DefensiveFeat.Dive, trial.Play.Outcome?.DefensiveFeat);
        Assert.True(trial.PossessionAt < 0 || trial.PossessionAt > commit.T + 0.555, $"nobody had the ball inside the recovery (possession at {trial.PossessionAt:0.00})");
        Assert.True(trial.DiverHeld, "the diver did not move while he recovered");
    }

    /// <summary>The human seat with a dead stick: the shipped assistance dives for the player and puts the liner away; the trial's does not, and the ball falls in.</summary>
    [Theory]
    [InlineData("control")]
    [InlineData("trial")]
    public void TheNeutralStickDivesForFreeOnlyOnTheShippedTable(string root)
    {
        var content = root == "trial" ? Trial : Control;
        // The shipped table's own diving liner: 250 ft at 12° into the left-field gap, which its left fielder reaches only at the rim.
        // The trial's own ball for this seat: a 120-mph liner at 16° over second, which the assistance's legs do not reach standing (the
        // 130-mph liner of the other tests they do — the human seat reads 0.40 s where the CPU's glove reads it × cpu.reactionMul).
        var (match, hit, preview) = root == "trial" ? Fixture(content, ball: (120, 16, 0)) : Fixture(content, carry: 250, spray: -18);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, HumanGlove, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        PlayEvent? play = null;
        var commits = 0;
        for (var i = 0; i < 60 * 12 && play is null; i++)
        {
            // A dead stick throughout; once the ball is in the glove the seat throws to first to finish the play.
            var pad = live.HoldsBall && !live.Throwing ? new LivePadInput(KeysBag: 1, SouthDown: true) : LivePadInput.Dead;
            play = live.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false, LivePlayCommandSource.Human)).CompletedPlay;
            if (live.Events.Contains(LiveEvent.DiveCommit)) commits++;
        }
        Assert.NotNull(play);
        if (root == "control")
        {
            Assert.Equal(PlayKind.FlyOut, play.Kind);
            Assert.Equal(DefensiveFeat.Dive, play.Outcome?.DefensiveFeat);
        }
        else
        {
            Assert.Equal(0, commits);
            Assert.NotEqual(PlayKind.FlyOut, play.Kind);
        }
    }

    /// <summary>
    /// East on the trial, with a runner on first so the play goes on: the press lunges, pays 0.555 s and takes the liner; the
    /// stick does not move the body through the recovery; South pressed from the catch on is dropped until the last 0.25 s,
    /// where it is remembered and released at readiness — the throw leaves no earlier than the commitment plus the cost.
    /// </summary>
    [Fact]
    public void EastDivesPaysAndTheThrowWaitsForTheRecoveryUnderTheTrial()
    {
        var (match, hit, preview) = Fixture(Trial);
        Assert.True(match.StationRunner(1, Trial.Must("gull")));
        var live = match.LivePlay;
        live.Recording = true;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, HumanGlove, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        PlayEvent? play = null;
        var commitAt = -1.0; var cost = 0.0; var caughtAt = -1.0; var queued = false; (double X, double Z)? held = null; var moved = false;
        for (var i = 0; i < 60 * 12 && play is null; i++)
        {
            var pad = LivePadInput.Dead;
            var d = Diamond.Dist(live.GloveX, live.GloveZ, live.BallX, live.BallZ);
            if (commitAt < 0 && !live.HoldsBall && live.ElapsedSeconds >= preview.HangTimeSec - 0.35 && d < 14)
                pad = new LivePadInput(EastDown: true);
            else if (live.HoldsBall && !live.Throwing)
                pad = new LivePadInput(KeysBag: 2, SouthDown: true, StickX: 1.0);   // throw to second, and try to walk
            var r = live.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false, LivePlayCommandSource.Human));
            if (r.CompletedPlay is null)
            {
                if (commitAt < 0 && live.Events.Contains(LiveEvent.DiveCommit)) { commitAt = live.ElapsedSeconds; cost = live.DiveRecoveryT; }
                if (live.Events.Contains(LiveEvent.ThrowQueued)) queued = true;
                if (caughtAt < 0 && live.HoldsBall) { caughtAt = live.ElapsedSeconds; held = (live.GloveX, live.GloveZ); }
                // The brake's drift (under a foot) is physical; a stick walk at 22 ft/s is not.
                else if (held is { } h && live.DiveRecoveryT > 0 && Diamond.Dist(h.X, h.Z, live.GloveX, live.GloveZ) > 1.5) moved = true;
            }
            play = r.CompletedPlay;
        }
        Assert.NotNull(play);
        Assert.True(commitAt > 0, "East never committed");
        Assert.Equal(0.555, cost, 6);
        Assert.True(caughtAt >= commitAt, "the dive took the ball");
        Assert.False(moved, "the stick moved the body inside the recovery");
        Assert.True(queued, "the last press inside the buffer was remembered");
        var marks = live.TakeTrace(play).Marks ?? [];
        var release = marks.First(m => m.Kind == PlayTraceMarkKind.ThrowRelease);
        Assert.InRange(release.T, commitAt + cost - Frame - 1e-9, commitAt + cost + 2 * Frame + 1e-9);
    }

    // ---------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The shipped table's liner is 280 ft at 12° into the left-centre gap on Harbor. The trial's is a 130-mph liner at 18° just left of second:
    /// #719 slice 2 recorded its dives on the 280-ft ball with the outfield's air multiplier at 1.0; at 0.6 (Jack, 2026-09-18) the centre
    /// fielder covers 10.8 ft/s under a ball in the air and that one is out of reach, so the trial's fixture is the ball he can still
    /// dive for. moss (Field 4) in centre, the offence without him.
    /// </summary>
    static (Match Match, AtBatResult Hit, FieldingPreview Preview) Fixture(ContentCatalog content, double carry = 0, double spray = -8, (double Exit, double Launch, double Spray)? ball = null)
    {
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "grit", "vine", "moss", "hex");
        var away = content.Team("Offense", "rio", "boom", "cinder", "soot", "nugget", "nico", "gull", "marlow", "ashlord");
        var match = Match.Exhibition(content, home, away, 3, 1, parkId: "harbor-diamond");
        var hit = carry > 0 ? FlightFixtures.Landing(match.Park, carry, 12, spray, rules: match.Rules)
            : FlightFixtures.Hit(match.Park, ball?.Exit ?? 130, ball?.Launch ?? 18, ball?.Spray ?? -6, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.False(preview.Grounder);   // the preview names an infielder first and hands to centre
        return (match, hit, preview);
    }

    sealed record Commit(double T, string Pos, double Cost);
    sealed record CpuRun(PlayEvent Play, double PossessionAt, double MaxRecovery, bool DiverHeld);

    static CpuRun RunCpu(ContentCatalog content, out List<Commit> commits, (double X, double Z)? nudgeOnCommit = null, double carry = 0)
    {
        var (match, hit, preview) = Fixture(content, carry);
        var live = match.LivePlay;
        live.Recording = true;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        commits = [];
        PlayEvent? play = null;
        var maxRecovery = 0.0; var possessionAt = -1.0; (double X, double Z)? diverAt = null; var diverHeld = true;
        for (var i = 0; i < 60 * 12 && play is null; i++)
        {
            var r = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu));
            play = r.CompletedPlay;
            if (play is not null) break;
            maxRecovery = Math.Max(maxRecovery, live.DiveRecoveryT);
            if (live.Events.Contains(LiveEvent.DiveCommit))
            {
                commits.Add(new Commit(live.ElapsedSeconds, live.GlovePos, live.DiveRecoveryT));
                diverAt = (live.GloveX, live.GloveZ);
                if (nudgeOnCommit is { } n) live.NudgeBall(n.X, n.Z);
            }
            else if (diverAt is { } at && live.DiveRecoveryT > 0 && live.DivingPos == live.GlovePos && Diamond.Dist(at.X, at.Z, live.GloveX, live.GloveZ) > 1.5)
                diverHeld = false;   // the brake's drift is physical; a chase is not
            if (possessionAt < 0 && live.HoldsBall) possessionAt = live.ElapsedSeconds;
        }
        Assert.NotNull(play);
        var marks = live.TakeTrace(play).Marks ?? [];
        var poss = marks.FirstOrDefault(m => m.Kind == PlayTraceMarkKind.Possession);
        if (poss is not null) possessionAt = poss.T;
        return new CpuRun(play, possessionAt, maxRecovery, diverHeld);
    }
}
