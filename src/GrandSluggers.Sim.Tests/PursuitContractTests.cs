using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-2 slice 1 (#718): the speed, the four read clocks, and cover at contact. On the <c>c80</c> copy a
/// Run-5 body runs 18.0 ft/s where it ran 30.5, every read is the accepted number, and a cover, cutoff or
/// backup body walks at its own pursuit speed from contact with no read. The shipped table keeps D11's
/// flat 28 ft/s after 0.23 s and the body's lockout, through two switches whose shipped values leave the
/// old arithmetic untouched to the double.
/// </summary>
public sealed class PursuitContractTests
{
    static readonly ContentCatalog Control = ContentCatalog.Load();
    static readonly DataRoot Root = new(Control.Root.Shipped, Path.GetFullPath(Path.Combine(Control.Root.Shipped, "..", "trials", "c80")));
    static readonly ContentCatalog Trial = ContentCatalog.Load(Root);
    const double Frame = 1.0 / 60.0;

    // ---------------------------------------------------------------------------------
    // The speed
    // ---------------------------------------------------------------------------------

    /// <summary>18.0 ft/s at Run 5 against today's 30.5, the whole curve scaled so the spread between characters is kept.</summary>
    [Fact]
    public void ARunFiveBodyRunsEighteenFeetASecondAndTheSpreadIsKept()
    {
        var shipped = Control.Rules.Fielding.Chase;
        var trial = Trial.Rules.Fielding.Chase;
        Assert.Equal((21.0, 1.9, 8.0), (shipped.BaseFtPerSec, shipped.FtPerSecPerRun, shipped.MinFtPerSec));
        Assert.Equal((12.4, 1.12, 4.72), (trial.BaseFtPerSec, trial.FtPerSecPerRun, trial.MinFtPerSec));
        Assert.Equal(30.5, shipped.BaseFtPerSec + 5 * shipped.FtPerSecPerRun, 9);
        Assert.Equal(18.0, trial.BaseFtPerSec + 5 * trial.FtPerSecPerRun, 9);

        var scale = 18.0 / 30.5;
        for (var run = 1; run <= 10; run++)
        {
            var was = shipped.BaseFtPerSec + run * shipped.FtPerSecPerRun;
            var now = trial.BaseFtPerSec + run * trial.FtPerSecPerRun;
            Assert.InRange(now / was, scale * 0.995, scale * 1.005);
        }
        Assert.InRange(trial.MinFtPerSec / shipped.MinFtPerSec, scale * 0.995, scale * 1.005);

        // Through the one glove speed, for a real body.
        var konga = Control.Must("konga");
        Assert.Equal(shipped.BaseFtPerSec + konga.Stats.Run * shipped.FtPerSecPerRun, FieldingResolver.ChaseSpeedFt(konga, false, Control.Rules), 9);
        Assert.Equal(trial.BaseFtPerSec + konga.Stats.Run * trial.FtPerSecPerRun, FieldingResolver.ChaseSpeedFt(konga, false, Trial.Rules), 9);
    }

    // ---------------------------------------------------------------------------------
    // The reads
    // ---------------------------------------------------------------------------------

    [Fact]
    public void TheFourReadClocksAreTheAcceptedNumbersAndTheShippedOnesDoNotMove()
    {
        var shipped = Control.Rules.Fielding.Reaction;
        Assert.Equal((0.42, 0.67, 0.27, 0.25, 0.30, 0.28, 0.83),
            (shipped.PitcherSec, shipped.CatcherSec, shipped.FirstSec, shipped.SecondSec, shipped.ThirdSec, shipped.ShortSec, shipped.OutfieldSec));

        var trial = Trial.Rules.Fielding.Reaction;
        Assert.Equal(0.35, trial.LockoutSec("P"));
        Assert.Equal(0.45, trial.LockoutSec("C"));
        foreach (var pos in new[] { "1B", "2B", "3B", "SS" }) Assert.Equal(0.25, trial.LockoutSec(pos));
        foreach (var pos in new[] { "LF", "CF", "RF" }) Assert.Equal(0.40, trial.LockoutSec(pos));

        // The CPU's delay before a throw is not a read and does not move here.
        Assert.Equal((shipped.ThrowBaseSec, shipped.ThrowPerFieldSec, shipped.ThrowMinSec), (trial.ThrowBaseSec, trial.ThrowPerFieldSec, trial.ThrowMinSec));
    }

    // ---------------------------------------------------------------------------------
    // Cover at contact
    // ---------------------------------------------------------------------------------

    /// <summary>The shipped walk is the flat 28 for every body, the same double; the trial walk is the body's own pursuit speed.</summary>
    [Fact]
    public void TheCoverSpeedIsFlatOnTheShippedTableAndTheBodysOwnOnTheTrial()
    {
        Assert.Equal((0.23, 1.0, 0.0), (Control.Rules.Fielding.Cover.StartSec, Control.Rules.Fielding.Cover.LockoutMul, Control.Rules.Fielding.Cover.ChaseSpeedWeight));
        Assert.Equal((0.0, 0.0, 1.0), (Trial.Rules.Fielding.Cover.StartSec, Trial.Rules.Fielding.Cover.LockoutMul, Trial.Rules.Fielding.Cover.ChaseSpeedWeight));
        foreach (var who in Control.Characters.Values)
        {
            Assert.Equal(Control.Rules.Fielding.Cover.FtPerSec, FieldingResolver.CoverSpeedFt(who, Control.Rules));
            Assert.Equal(FieldingResolver.ChaseSpeedFt(who, false, Trial.Rules), FieldingResolver.CoverSpeedFt(who, Trial.Rules), 9);
        }
    }

    /// <summary>
    /// A grounder to short with 1B covering first. On the control 1B waits the later of the cover start and
    /// its own 0.27-s read, then walks at 28 ft/s; on the trial 1B is moving on the first frame after contact
    /// at its own pursuit speed. Measured from <see cref="LivePlaySystem.Fielders"/>; the in-process trial
    /// stands the bodies on the shipped spots, which is fine for a question about when and how fast a body
    /// leaves its spot.
    /// </summary>
    [Theory]
    [InlineData("control")]
    [InlineData("trial")]
    public void TheFirstBasemanCoversAtContactOnTheTrialAndAfterHisReadOnTheControl(string root)
    {
        var content = root == "trial" ? Trial : Control;
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = content.Team("Offense", "zig", "boom", "jester", "grit", "soot", "nugget", "pip", "gull", "marlow");
        var match = Match.Exhibition(content, home, away, 3, 1, parkId: "harbor-diamond");
        var hit = FlightFixtures.Landing(match.Park, 118, 4, -18, rules: match.Rules);
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
        Assert.True(firstMove >= 0, $"{root}: 1B never moved");
        var cover = match.Rules.Fielding.Cover;
        var read = match.Rules.Fielding.Reaction.LockoutSec("1B") * match.Rules.Cpu.Active.ReactionMul;
        var expectedWait = Math.Max(cover.StartSec, cover.LockoutMul * read);
        var movedAt = track[firstMove].T;
        Assert.InRange(movedAt, expectedWait - 1e-9, expectedWait + 2 * Frame);
        if (root == "control") Assert.InRange(movedAt, 0.27 - 1e-9, 0.27 + 2 * Frame);
        else Assert.InRange(movedAt, Frame - 1e-9, 2 * Frame + 1e-9);

        // The speed over the next five frames, once walking and well short of the bag.
        var a = track[firstMove];
        var b = track[firstMove + 5];
        var speed = Diamond.Dist(a.X, a.Z, b.X, b.Z) / (b.T - a.T);
        Assert.InRange(speed, FieldingResolver.CoverSpeedFt(lace, match.Rules) * 0.97, FieldingResolver.CoverSpeedFt(lace, match.Rules) * 1.03);
        if (root == "control") Assert.InRange(speed, 27.2, 28.8);
        else Assert.InRange(speed, FieldingResolver.ChaseSpeedFt(lace, false, match.Rules) * 0.97, FieldingResolver.ChaseSpeedFt(lace, false, match.Rules) * 1.03);
    }

    /// <summary>The bunt defence's cover walk on the square uses the same speed: flat shipped, the body's own on the trial.</summary>
    [Fact]
    public void TheSquaresCoverWalkTakesTheSameSpeed()
    {
        var frost = Control.Must("frost");   // 2B covers first behind the crash (fielding.bunt.coverFirst)
        var assigned = new Dictionary<string, Character>(StringComparer.OrdinalIgnoreCase) { ["2B"] = frost };
        foreach (var (rules, speed) in new[] { (Control.Rules, Control.Rules.Fielding.Cover.FtPerSec), (Trial.Rules, FieldingResolver.ChaseSpeedFt(frost, false, Trial.Rules)) })
        {
            var rest = BuntDefense.Spots(assigned, 0, rules);
            var walked = BuntDefense.Spots(assigned, 0.5, rules);
            var first = Diamond.Bag(1);
            var before = Diamond.Dist(rest["2B"].X, rest["2B"].Z, first.X, first.Z);
            var after = Diamond.Dist(walked["2B"].X, walked["2B"].Z, first.X, first.Z);
            Assert.Equal(speed * 0.5, before - after, 6);
        }
    }
}
