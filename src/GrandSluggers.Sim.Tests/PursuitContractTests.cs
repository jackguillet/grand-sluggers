using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-2 slice 1 (#718): the speed, the four read clocks, and cover at contact. A Run-5 body runs 18.0 ft/s,
/// every read is the accepted number, and a cover, cutoff or backup body walks at its own pursuit speed from
/// contact with no read.
/// </summary>
public sealed class PursuitContractTests
{
    static readonly ContentCatalog Game = ContentCatalog.Load();
    const double Frame = 1.0 / 60.0;

    // ---------------------------------------------------------------------------------
    // The speed
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// 18.0 ft/s at Run 5: D11's curve (21 + 1.9 × Run, floor 8, 30.5 at Run 5) scaled whole by 18 / 30.5, so the
    /// spread between characters is kept.
    /// </summary>
    [Fact]
    [Trait("Kind", "Balance")]
    public void ARunFiveBodyRunsEighteenFeetASecondAndTheSpreadIsKept()
    {
        var chase = Game.Rules.Fielding.Chase;
        Assert.Equal((12.4, 1.12, 4.72), (chase.BaseFtPerSec, chase.FtPerSecPerRun, chase.MinFtPerSec));
        Assert.Equal(18.0, chase.BaseFtPerSec + 5 * chase.FtPerSecPerRun, 9);

        var scale = 18.0 / 30.5;
        for (var run = 1; run <= 10; run++)
        {
            var was = 21.0 + run * 1.9;
            var now = chase.BaseFtPerSec + run * chase.FtPerSecPerRun;
            Assert.InRange(now / was, scale * 0.995, scale * 1.005);
        }
        Assert.InRange(chase.MinFtPerSec / 8.0, scale * 0.995, scale * 1.005);

        // Through the one glove speed, for a real body.
        var konga = Game.Must("konga");
        Assert.Equal(chase.BaseFtPerSec + konga.Stats.Run * chase.FtPerSecPerRun, FieldingResolver.ChaseSpeedFt(konga, false, Game.Rules), 9);
    }

    // ---------------------------------------------------------------------------------
    // The reads
    // ---------------------------------------------------------------------------------

    [Fact]
    [Trait("Kind", "Balance")]
    public void TheFourReadClocksAreTheAcceptedNumbers()
    {
        var reads = Game.Rules.Fielding.Reaction;
        Assert.Equal(0.35, reads.LockoutSec("P"));
        Assert.Equal(0.45, reads.LockoutSec("C"));
        foreach (var pos in new[] { "1B", "2B", "3B", "SS" }) Assert.Equal(0.25, reads.LockoutSec(pos));
        foreach (var pos in new[] { "LF", "CF", "RF" }) Assert.Equal(0.40, reads.LockoutSec(pos));

        // The CPU's delay before a throw is not a read and keeps its own numbers.
        Assert.Equal((0.35, 0.02, 0.08), (reads.ThrowBaseSec, reads.ThrowPerFieldSec, reads.ThrowMinSec));
    }

    // ---------------------------------------------------------------------------------
    // Cover at contact
    // ---------------------------------------------------------------------------------

    /// <summary>The cover walk is the body's own pursuit speed, from contact, with no read.</summary>
    [Fact]
    [Trait("Kind", "Balance")]
    public void TheCoverSpeedIsTheBodysOwn()
    {
        Assert.Equal((0.0, 0.0, 1.0), (Game.Rules.Fielding.Cover.StartSec, Game.Rules.Fielding.Cover.LockoutMul, Game.Rules.Fielding.Cover.ChaseSpeedWeight));
        foreach (var who in Game.Characters.Values)
            Assert.Equal(FieldingResolver.ChaseSpeedFt(who, false, Game.Rules), FieldingResolver.CoverSpeedFt(who, Game.Rules), 9);
    }

    /// <summary>
    /// A grounder to short with 1B covering first: 1B is moving on the first frame after contact at its own
    /// pursuit speed. Measured from <see cref="LivePlaySystem.Fielders"/>.
    /// </summary>
    [Fact]
    public void TheFirstBasemanCoversAtContact()
    {
        var content = Game;
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = content.Team("Offense", "zig", "boom", "jester", "grit", "soot", "nugget", "pip", "gull", "marlow");
        var match = Match.Exhibition(content, home, away, 3, 1, parkId: "harbor-diamond");
        var hit = FlightFixtures.Hit(match.Park, 85, -12, -18, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        var lace = content.Must("lace");

        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        var start = live.Fielders["1B"];
        var firstMove = -1;
        var track = new List<(double T, double X, double Z)>();
        for (var i = 0; (i < 60 && firstMove < 0) || i < 40; i++)
        {
            live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu));
            var at = live.Fielders["1B"];
            track.Add((live.ElapsedSeconds, at.X, at.Z));
            if (firstMove < 0 && Diamond.Dist(at.X, at.Z, start.X, start.Z) > 1e-6) firstMove = i;
            if (i >= 59) break;
        }
        Assert.True(firstMove >= 0, "1B never moved");
        var cover = match.Rules.Fielding.Cover;
        var read = match.Rules.Fielding.Reaction.LockoutSec("1B") * match.Rules.Cpu.Active.ReactionMul;
        var expectedWait = Math.Max(cover.StartSec, cover.LockoutMul * read);
        var movedAt = track[firstMove].T;
        Assert.InRange(movedAt, expectedWait - 1e-9, expectedWait + 2 * Frame);
        Assert.InRange(movedAt, Frame - 1e-9, 2 * Frame + 1e-9);

        // The speed over five frames, once walking and well short of the bag: after the response law's 0.20-s ramp
        // (#718 slice 2), where the first frames are the build-up.
        var settle = (int)Math.Ceiling(match.Rules.Fielding.Chase.AccelSec / Frame) + 2;
        var a = track[firstMove + settle];
        var b = track[firstMove + settle + 5];
        var speed = Diamond.Dist(a.X, a.Z, b.X, b.Z) / (b.T - a.T);
        Assert.InRange(speed, FieldingResolver.CoverSpeedFt(lace, match.Rules) * 0.97, FieldingResolver.CoverSpeedFt(lace, match.Rules) * 1.03);
        Assert.InRange(speed, FieldingResolver.ChaseSpeedFt(lace, false, match.Rules) * 0.97, FieldingResolver.ChaseSpeedFt(lace, false, match.Rules) * 1.03);
    }

    /// <summary>The bunt defence's cover walk on the square uses the same speed: the body's own.</summary>
    [Fact]
    public void TheSquaresCoverWalkTakesTheSameSpeed()
    {
        var frost = Game.Must("frost");   // 2B covers first behind the crash (fielding.bunt.coverFirst)
        var assigned = new Dictionary<string, Character>(StringComparer.OrdinalIgnoreCase) { ["2B"] = frost };
        var rules = Game.Rules;
        var speed = FieldingResolver.ChaseSpeedFt(frost, false, rules);
        var rest = BuntDefense.Spots(assigned, 0, rules);
        var walked = BuntDefense.Spots(assigned, 0.5, rules);
        var first = Diamond.Bag(1);
        var before = Diamond.Dist(rest["2B"].X, rest["2B"].Z, first.X, first.Z);
        var after = Diamond.Dist(walked["2B"].X, walked["2B"].Z, first.X, first.Z);
        Assert.Equal(speed * 0.5, before - after, 6);
    }
}
