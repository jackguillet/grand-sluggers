using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-3 slice 2 (#719: F693-02-dive-jump-scoop-reach, -dive-recovery-cost, -cpu-dive-intent, -cpu-dive-intent-policy):
/// nobody dives for free. The neutral stick's assistance never dives, the CPU commits on the live ball at the last makeable
/// moment and can miss, and every dive — East or the CPU's, caught or missed — costs its recovery before the diver moves or
/// throws.
/// </summary>
public sealed class DiveTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    [Fact]
    [Trait("Kind", "Balance")]
    public void TheDiveIsNeitherFreeNorAutomatic()
    {
        var t = Game.Rules.Fielding.Catch;
        Assert.Equal((0.60, 0.025), (t.DiveRecoverySec, t.DiveRecoveryFieldCut));

        // 0.60 at Field 1, 2.5 % of it less per point: 0.54 at 5, 0.465 at 10.
        double Cost(ContentCatalog c, int field) => FieldingResolver.DiveRecoverySec(c.Must("ashlord") with { Stats = c.Must("ashlord").Stats with { Field = field } }, c.Rules);
        Assert.Equal(0.60, Cost(Game, 1), 9);
        Assert.Equal(0.54, Cost(Game, 5), 9);
        Assert.Equal(0.465, Cost(Game, 10), 9);
        Assert.Equal(0.555, FieldingResolver.DiveRecoverySec(Game.Must("moss"), Game.Rules), 9);   // Field 4, the centre fielder below
    }

    [Theory]
    [InlineData(130, 16, 6)]
    [InlineData(160, 4, -18)]
    [InlineData(140, 6, 18)]
    public void CpuPursuesWithoutEverCommittingADive(double exit, double launch, double spray)
    {
        var run = RunCpu(Game, out var commits, ball: (exit, launch, spray));
        Assert.Empty(commits);
        Assert.Equal(0, run.MaxRecovery);
        Assert.NotEqual(DefensiveFeat.Dive, run.Play.Outcome?.DefensiveFeat);
    }

    /// <summary>The human seat with a dead stick: the assistance does not dive for the player, and the ball falls in.</summary>
    [Fact]
    public void TheNeutralStickNeverDives() => NeutralStick((140, 12, 25));

    // A liner over second which the assistance's legs do not reach standing (the liner of the other tests they do — the human
    // seat reads 0.40 s where the CPU's glove reads it × cpu.reactionMul).
    static void NeutralStick((double Exit, double Launch, double Spray) ball)
    {
        var (match, hit, preview) = Fixture(Game, ball: ball);
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
        Assert.Equal(0, commits);
        Assert.NotEqual(PlayKind.FlyOut, play.Kind);
    }

    /// <summary>
    /// East, with a runner on first so the play goes on: the press lunges, pays 0.555 s and takes the liner; the
    /// stick does not move the body through the recovery; South pressed from the catch on is dropped until the last 0.25 s,
    /// where it is remembered and released at readiness — the throw leaves no earlier than the commitment plus the cost.
    /// </summary>
    [Fact]
    public void EastDivesPaysAndTheThrowWaitsForTheRecovery() => EastDive(null);

    static void EastDive((double Exit, double Launch, double Spray)? ball)
    {
        var (match, hit, preview) = Fixture(Game, ball);
        Assert.True(match.StationRunner(1, Game.Must("gull")));
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
        Assert.InRange(release.T, commitAt + cost + match.Rules.Fielding.Throw.ReleaseSec - Frame - 1e-9, commitAt + cost + match.Rules.Fielding.Throw.ReleaseSec + 2 * Frame + 1e-9);
    }

    // ---------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// A 115-mph liner at 16° just right of second on Harbor: #719 slice 2 recorded its dives on a 280-ft ball with the outfield's air
    /// multiplier at 1.0; at 0.6 (Jack, 2026-09-18) the centre fielder covers 10.8 ft/s under a ball in the air and that one is out of
    /// reach, so the fixture is the ball he can still dive for. moss (Field 4) in centre, the offence without him.
    /// </summary>
    static (Match Match, AtBatResult Hit, FieldingPreview Preview) Fixture(ContentCatalog content, (double Exit, double Launch, double Spray)? ball = null)
    {
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "grit", "vine", "moss", "hex");
        var away = content.Team("Offense", "rio", "boom", "cinder", "soot", "nugget", "nico", "gull", "marlow", "ashlord");
        var match = Match.Exhibition(content, home, away, 3, 1, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Hit(match.Park, ball?.Exit ?? 115, ball?.Launch ?? 16, ball?.Spray ?? 2, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        return (match, hit, preview);
    }

    sealed record Commit(double T, string Pos, double Cost);
    sealed record CpuRun(PlayEvent Play, double PossessionAt, double MaxRecovery, bool DiverHeld);

    static CpuRun RunCpu(ContentCatalog content, out List<Commit> commits, (double X, double Z)? nudgeOnCommit = null, (double Exit, double Launch, double Spray)? ball = null)
    {
        var (match, hit, preview) = Fixture(content, ball);
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
