using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class BuddyJumpTests
{
    readonly ContentCatalog _content = Shipped.Content;
    const double Frame = 1.0 / 60;

    Match Defense()
    {
        var home = _content.Team("Buddies", "vale", "pewter", "lace", "frost", "grit", "marlow", "zig", "dart", "nico");
        return Match.Exhibition(_content, home, PresetTeams.EmberCourt(_content), seed: 1, parkId: "harbor-diamond");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreviewRejectsEitherOutfielderWhoCannotReachThePlant(bool mainLate)
    {
        var match = Defense();
        var hit = FlightFixtures.OverTheFence(match.Park, 5, 0, 60);
        var pre = match.PreviewHit(hit);
        var plant = FlyCatch.ChaseTarget(pre, match.Rules, match.Park);
        var map = FieldingResolver.Assign(match.Defense, match.Pitcher);
        var spots = OutfieldStarts.Of(match.Park, match.Rules).ToDictionary(kv => kv.Key, kv => kv.Value);
        spots["CF"] = mainLate ? (0, 0) : plant;
        spots["LF"] = (0, 0);
        spots["RF"] = (0, 0);
        var shown = new FieldingResolver(_content.Chemistry, match.Rules)
            .Preview(hit, match.Park, match.Defense.Roster, match.Pitcher, new Random(1), gloves: map, at: spots);
        Assert.Null(shown.Buddy);
    }

    [Theory]
    [InlineData(0, 0, 0, true)]
    [InlineData(5, 0, 0, false)]
    [InlineData(0, 5, 0, false)]
    [InlineData(0, 0, 5, false)]
    public void BothBodiesMustBeDirectlyUnderTheLiveBall(double gloveOffset, double buddyOffset, double ballOffset, bool expected)
    {
        var match = Defense();
        var pre = match.PreviewHit(FlightFixtures.OverTheFence(match.Park, 5, 0, 60));
        Assert.True(FieldingResolver.BuddyJumpOffered(pre));
        var plant = FlyCatch.ChaseTarget(pre, match.Rules, match.Park);
        Assert.Equal(expected, FlyCatch.BuddyInPosition(pre, match.Park,
            plant.X + gloveOffset, plant.Z, plant.X + buddyOffset, plant.Z,
            plant.X + ballOffset, 12, plant.Z, pre.HangTimeSec - .1, pre.HangTimeSec, match.Rules));
        Assert.False(FlyCatch.BuddyInPosition(pre, match.Park,
            plant.X, plant.Z, plant.X, plant.Z, plant.X, 100, plant.Z,
            pre.HangTimeSec - .1, pre.HangTimeSec, match.Rules));
        Assert.False(FlyCatch.BuddyInPosition(pre, match.Park,
            plant.X, plant.Z, plant.X, plant.Z, plant.X, 12, plant.Z,
            pre.HangTimeSec, pre.HangTimeSec, match.Rules));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void APlannedPairCannotGrantBuddyReachAfterItsBodiesAreSlowed(bool human)
    {
        var match = Defense();
        var hit = FlightFixtures.OverTheFence(match.Park, 5, 0, 60);
        var pre = match.PreviewHit(hit);
        Assert.NotNull(pre.Buddy);
        // Retain the original offer but slow the actual bodies: the live gate must recheck arrival.
        pre = pre with { Frozen = true };
        var live = match.LivePlay;
        live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, pre, null,
            human ? new LiveSeats(false, true, true, false) : LiveSeats.CpuOnly));
        for (var i = 0; i < 1000 && live.Active; i++)
        {
            live.Apply(LivePlayCommand.Tick(Frame,
                human ? new LivePadInput(WestDown: !live.Airborne) : LivePadInput.Dead,
                LivePadInput.Dead, false, human ? LivePlayCommandSource.Human : LivePlayCommandSource.Cpu));
            Assert.False(live.Buddy);
            Assert.DoesNotContain(LiveEvent.BuddyJump, live.Events);
            Assert.False(live.BuddyWindow);
        }
        Assert.False(live.Active);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void LiveBuddyRunsAtNormalSpeedAndCatchesOnlyWithBothBodiesUnderBall(bool human, bool versus)
    {
        var match = Defense();
        var hit = FlightFixtures.OverTheFence(match.Park, 5, 0, 60);
        var pre = match.PreviewHit(hit);
        Assert.True(FieldingResolver.BuddyJumpOffered(pre));
        var map = FieldingResolver.Assign(match.Defense, match.Pitcher);
        var partnerPos = map.Single(kv => kv.Value.Id == pre.Buddy!.Id).Key;
        var seats = human ? new LiveSeats(versus, true, true, versus) : LiveSeats.CpuOnly;
        var live = match.LivePlay;
        live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, pre, null, seats));
        var speed = FieldingResolver.ChaseSpeedFt(pre.Buddy!, partnerPos, pre, match.Rules);
        var ready = FieldingResolver.CpuReactionLockouts(match.Rules, pre.HangTimeSec)[partnerPos];
        var caught = false;
        for (var i = 0; i < 1000 && live.Active && !live.HoldsBall; i++)
        {
            var before = live.Fielders[partnerPos];
            var nextT = live.ElapsedSeconds + Frame;
            var ball = BallFlight.PointAt(live.Path!, nextT, match.Rules);
            var pad = human ? new LivePadInput(WestDown: live.BuddyWindow && !live.Airborne) : LivePadInput.Dead;
            var result = live.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false,
                human ? LivePlayCommandSource.Human : LivePlayCommandSource.Cpu));
            if (!live.Active)
            {
                Assert.Equal(DefensiveFeat.BuddyJump, result.CompletedPlay?.Outcome?.DefensiveFeat);
                var bodies = result.CompletedPlay!.Outcome!.BodiesAtTime;
                var glove = bodies.Single(b => b.Pos == pre.Position);
                var buddy = bodies.Single(b => b.Pos == partnerPos);
                Assert.True(FlyCatch.BuddyInPosition(pre, match.Park, glove.X, glove.Z,
                    buddy.X, buddy.Z, ball.X, ball.Y, ball.Z, nextT, pre.HangTimeSec, match.Rules));
                caught = true;
                break;
            }
            var after = live.Fielders[partnerPos];
            Assert.True(Diamond.Dist(before.X, before.Z, after.X, after.Z) <= speed * Frame + 1e-6);
            if (nextT + 1e-9 < ready) Assert.Equal(before, after);
            if (!live.Buddy) continue;
            caught = true;
            Assert.True(FlyCatch.BuddyInPosition(pre, match.Park, live.GloveX, live.GloveZ,
                after.X, after.Z, ball.X, ball.Y, ball.Z, nextT, pre.HangTimeSec, match.Rules));
        }
        Assert.True(caught, $"No buddy catch: glove {live.GlovePos}, partner {partnerPos}, hang {pre.HangTimeSec}");
    }
}
