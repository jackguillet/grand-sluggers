using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class FieldingReachTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Dt = 1d / 60;
    static readonly LiveSeats Human = new(false, true, true, false);

    [Theory]
    [InlineData("easy", false)] [InlineData("normal", false)] [InlineData("hard", false)]
    [InlineData("easy", true)] [InlineData("normal", true)] [InlineData("hard", true)]
    public void PitcherRecoveryBlocksBothMovementAndTheLinerAlreadyOnTheirBody(string level, bool human)
    {
        var m = Match.Exhibition(Game, seed: 1, parkId: ParkIds.Harbor, difficulty: level);
        var hit = FlightFixtures.Hit(m.Park, 125, 1, 0, rules: m.Rules);
        var pre = m.PreviewHit(hit) with { Position = "P", Fielder = m.Pitcher };
        var l = m.LivePlay;
        l.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, pre, null, human ? Human : LiveSeats.CpuOnly));
        var start = l.Fielders["P"];
        var crossed = false;
        for (var i = 0; (i + 1) * Dt < m.Rules.Fielding.Reaction.PitcherRecoverySec && l.Active; i++)
        {
            l.Apply(LivePlayCommand.Tick(Dt, human ? new LivePadInput(StickX: 1, SouthDown: true, EastDown: true) : LivePadInput.Dead));
            Assert.Equal(start, l.Fielders["P"]);
            Assert.False(l.HoldsBall && l.GlovePos == "P");
            Assert.DoesNotContain(LiveEvent.DiveCommit, l.Events);
            if (Diamond.Dist(start.X, start.Z, l.BallX, l.BallZ) < 4 && l.BallY < 6) crossed = true;
        }
        Assert.True(crossed, "the liner actually passed through standing reach during recovery");
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void BuntLetsThePitcherRespondAndEventuallyScoop(bool human)
    {
        var m = Match.Slice(Game, seed: 1);
        var hit = FlightFixtures.Hit(m.Park, 30, -10, 0, bunt: true, rules: m.Rules);
        var pre = m.PreviewHit(hit) with { Position = "P", Fielder = m.Pitcher };
        var l = m.LivePlay;
        l.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, pre, null, human ? Human : LiveSeats.CpuOnly));
        var start = l.Fielders["P"];
        var movedDuringRecovery = false;
        for (var i = 0; i < 360 && l.Active; i++)
        {
            l.Apply(LivePlayCommand.Tick(Dt));
            if (l.ElapsedSeconds < m.Rules.Fielding.Reaction.PitcherRecoverySec && l.Fielders["P"] != start)
                movedDuringRecovery = true;
            if (l.HoldsBall)
            {
                Assert.True(movedDuringRecovery);
                Assert.Equal("P", l.GlovePos);
                Assert.NotEqual(FairFoulCall.Caught, l.Call);
                return;
            }
        }
        Assert.Fail("the pitcher did not pursue and scoop the bunt");
    }

    [Theory]
    [InlineData(40, false)] [InlineData(50, false)] [InlineData(60, false)]
    [InlineData(40, true)] [InlineData(50, true)] [InlineData(60, true)]
    public void PitcherRecoversInTimeForWeakFullSwingGrounders(double exitMph, bool human)
    {
        var m = Match.Exhibition(Game, seed: 1, parkId: ParkIds.Harbor);
        var hit = FlightFixtures.Hit(m.Park, exitMph, -12, 0, rules: m.Rules);
        var pre = m.PreviewHit(hit);
        Assert.Equal("P", pre.Position);
        var l = m.LivePlay;
        l.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, pre, null, human ? Human : LiveSeats.CpuOnly));
        for (var i = 0; i < 360 && l.Active; i++)
        {
            l.Apply(LivePlayCommand.Tick(Dt));
            if (!l.HoldsBall) continue;
            Assert.Equal("P", l.GlovePos);
            Assert.True(l.ElapsedSeconds >= m.Rules.Fielding.Reaction.PitcherRecoverySec);
            Assert.NotEqual(FairFoulCall.Caught, l.Call);
            return;
        }
        Assert.Fail("the pitcher did not field the weak full-swing grounder");
    }

    [Fact]
    public void FielderCanReachALooseBallAgainstTheWall()
    {
        var park = Game.Parks[ParkIds.Harbor];
        var wall = FieldBounds.Of(park).RadiusAt(0);
        var at = FieldBounds.ClampFielder(park, 0, wall, Game.Rules);
        Assert.True(Diamond.Dist(0, wall, at.X, at.Z) < Game.Rules.Fielding.Chase.LooseScoopFt);
        Assert.True(FieldBounds.Of(park).Contains(at.X, at.Z));
    }

    [Theory]
    [InlineData(0, 58)] [InlineData(-70, 105)] [InlineData(70, 105)]
    [InlineData(-110, 190)] [InlineData(110, 190)] [InlineData(0, 220)]
    public void DiveHasNoComponentTowardOrAwayFromHome(double x, double z)
    {
        foreach (var (dx, dz) in new[] { (30d, 40d), (-30d, 40d), (30d, -40d), (-30d, -40d) })
        {
            var to = FieldDash.Lunge(x, z, x + dx, z + dz, rules: Game.Rules);
            Assert.Equal(0, (to.X-x)*x + (to.Z-z)*z, 8);
            Assert.InRange(Diamond.Dist(x,z,to.X,to.Z), 0, Game.Rules.Fielding.Dash.DiveLungeFt + 1e-9);
        }
        Assert.Equal((x,z), FieldDash.Lunge(x,z,x*1.5,z*1.5,rules:Game.Rules));
        Assert.Equal((x,z), FieldDash.Lunge(x,z,x*.5,z*.5,rules:Game.Rules));
    }

    [Fact]
    public void NewReachRequiresTheBodyToGetCloserForCatchScoopAndDive()
    {
        var r = Game.Rules;
        var reach = FieldingResolver.CatchRadiusFt(Game.Must("ashlord"), Game.Parks[ParkIds.Harbor], r);
        Assert.Equal(4, reach);
        Assert.Equal(5, FieldingResolver.CatchWindowFt(reach, false, false, r));
        Assert.Equal(6, FieldingResolver.DiveCatchFt(reach, r));
        Assert.False(FlyCatch.TouchScoop(6, FieldingResolver.CatchWindowFt(reach,false,false,r), 0, r));
        Assert.True(FlyCatch.TouchScoop(4.9, FieldingResolver.CatchWindowFt(reach,false,false,r), 0, r));
        Assert.False(FlyCatch.PlayerDiveCatch(true, 7, reach, FieldingResolver.DiveCatchFt(reach,r), 2,r));
    }
}
