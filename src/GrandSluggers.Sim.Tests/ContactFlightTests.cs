using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class ContactFlightTests
{
    readonly ContentCatalog game = ContentCatalog.Load();
    Park Park => game.Parks["harbor-diamond"];

    [Fact]
    public void EveryLaunchSharesTheSameClockAcrossFormerClassBoundaries()
    {
        foreach (var exit in new[] { 60d, 74d, 95d })
        foreach (var angle in new[] { -25d, 0, 3, 10, 14, 22, 45 })
        {
            var a = BallFlight.Trajectory(exit, angle - .0001, 0, game.Rules);
            var b = BallFlight.Trajectory(exit, angle + .0001, 0, game.Rules);
            Assert.Equal(game.Rules.Flight.TimeScale / game.Rules.Flight.SampleHz, a[1].T, 9);
            Assert.Equal(a[1].T, b[1].T);
            for (var i = 1; i < 5; i++)
            {
                Assert.InRange(Math.Abs(a[i].Height - b[i].Height), 0, .001);
                Assert.InRange(Math.Abs(a[i].Dist - b[i].Dist), 0, .001);
            }
        }
    }

    [Fact]
    public void GroundBallsBounceEarlyOrLateAndKeepVisibleHopsWithoutACatchCircle()
    {
        var early = BallFlight.Trajectory(85, -20, 0, game.Rules);
        var late = BallFlight.Trajectory(85, 7, 0, game.Rules);
        Assert.InRange(BallFlight.FirstLandingDist(early), 0, 15);
        Assert.True(BallFlight.FirstLandingDist(late) > BallFlight.FirstLandingDist(early) + 40);
        foreach (var angle in new[] { -20d, 7d })
        {
            var match = Match.Slice(game);
            var pre = match.PreviewHit(FlightFixtures.Hit(Park, 85, angle, 0));
            var path = pre.Ball!.Samples;
            var first = BallFlight.LandingIndex(path);
            var next = path.Skip(first + 1).TakeWhile(s => s.Event != SampleEvent.Ground).ToArray();
            Assert.NotEmpty(next);
            Assert.True(next.Max(s => s.Height) > .25, "a real hop after the first bounce");
            Assert.All(path, s => Assert.False(LandingMark.On(pre, s.Height, s.T, false, false)));
        }
    }

    [Fact]
    public void IdenticalIncomingStatesBounceTheSameRegardlessOfOriginalHit()
    {
        var seed = new[] { new Sample(0, 50, 2, 0, 50) };
        var a = BallFlight.Continue(seed, 0, 0, 2, 50, 0, -20, 80, -20, 75, Park, game.Rules);
        var b = BallFlight.Continue(seed, 0, 0, 2, 50, 0, -20, 80, 18, 100, Park, game.Rules);
        Assert.Equal(a, b);
        Assert.Contains(a, s => s.Event == SampleEvent.Ground);
    }

    [Fact]
    public void LowLinerMeetsTheGloveAndHigherLinerClearsIt()
    {
        var match = Match.Slice(game);
        foreach (var (angle, canCatch) in new[] { (5d, true), (18d, false) })
        {
            var pre = match.PreviewHit(FlightFixtures.Hit(Park, 95, angle, 0));
            var sample = pre.Ball!.Samples.First(s => s.Z >= 65);
            Assert.True(sample.T < pre.HangTimeSec);
            foreach (var label in new[] { BattedBallClass.Grounder, BattedBallClass.Liner, BattedBallClass.Fly })
            {
                var named = pre with { Class = label };
                Assert.Equal(canCatch, FlyCatch.InPosition(named, sample.X, sample.Z,
                    sample.X, sample.Z, sample.Height, pre.LandingX, pre.LandingZ,
                    pre.CatchRadius, sample.T, pre.HangTimeSec, false, game.Rules));
                Assert.False(FlyCatch.PickupInPlay(named, Park, sample.X, sample.Z, sample.T, pre.HangTimeSec, game.Rules));
                Assert.Equal(PlayKind.FlyOut, FlyCatch.PlayerKind(true, named, inAir: true));
                Assert.Equal(PlayKind.InPlay, FlyCatch.PlayerKind(true, named, inAir: false));
            }
        }
    }

    [Fact]
    public void ABounceNeverBecomesAnAerialCatchEvenWhileTheHopIsRising()
    {
        var match = Match.Slice(game);
        var pre = match.PreviewHit(FlightFixtures.Hit(Park, 95, -20, 0));
        var sample = pre.Ball!.Samples.First(s => s.T > pre.HangTimeSec && s.Height > .5);
        foreach (var label in new[] { BattedBallClass.Grounder, BattedBallClass.Liner, BattedBallClass.Pop })
        {
            var named = pre with { Class = label };
            Assert.False(FlyCatch.InPosition(named, sample.X, sample.Z, sample.X, sample.Z,
                sample.Height, sample.X, sample.Z, 20, sample.T, pre.HangTimeSec, false, game.Rules));
            Assert.False(LandingMark.On(named, sample.Height, sample.T, false, false));
            Assert.True(FlyCatch.PickupInPlay(named, Park, sample.X, sample.Z, sample.T, pre.HangTimeSec, game.Rules));
        }
    }

    [Fact]
    public void ContinuingAFlightDoesNotApplyTheSharedSlowdownTwice()
    {
        var path = BallFlight.Trajectory(95, 30, 0, Park, game.Rules);
        const double dt = 1.0 / 60;
        const double t = 1;
        var before = BallFlight.PointAt(path, t - dt);
        var now = BallFlight.PointAt(path, t);
        var vx = (now.X - before.X) / dt;
        var vy = (now.Y - before.Y) / dt;
        var vz = (now.Z - before.Z) / dt;
        var continued = BallFlight.Continue(path, t, now.X, now.Y, now.Z, vx, vy, vz, 30, 95, Park, game.Rules);
        var after = BallFlight.PointAt(continued, t + dt);
        Assert.InRange((after.Z - now.Z) / dt / vz, .98, 1.01);
        Assert.InRange((after.Y - now.Y) / dt / vy, .98, 1.01);
    }

    [Fact]
    public void RisingHopDifficultyUsesThePhysicalVelocityBehindTheSharedClock()
    {
        var match = Match.Slice(game);
        var hit = FlightFixtures.Hit(match.Park, 120, -14, 0, rules: match.Rules);
        var live = match.LivePlay;
        live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, match.PreviewHit(hit), null, LiveSeats.CpuOnly));
        const double dt = 1.0 / 60;
        for (var i = 0; i < 600 && live.Active; i++)
        {
            var beforeY = live.BallY;
            live.Apply(LivePlayCommand.Tick(dt));
            if (live.IncomingFtPerSec <= 0) continue;
            var physicalVy = (live.BallY - beforeY) / dt * match.Rules.Flight.TimeScale;
            Assert.Equal(FieldingResolver.HopDifficulty(live.BallY, physicalVy, match.Rules), live.HopDifficulty, 8);
            Assert.True(live.HopDifficulty > 0, "the rising in-between hop stays an awkward pickup");
            return;
        }
        Assert.Fail("The pitcher did not reach the rising hop.");
    }

    [Fact]
    public void OrdinaryContactMovesContinuouslyFromDownwardToLoftedAcrossTheBat()
    {
        var resolver = new AtBatResolver(game.Chemistry, game.Rules, game.StarSkills);
        var swing = new AtBatInput(game.Must("vale"), game.Must("rio"), null, [],
            false, false, 0, false, false, Bat: game.Bats["harbor-lumber"], PitcherStamina: 80);
        var angles = new List<double>();
        for (var y = StrikeZoneGeometry.Bottom; y <= StrikeZoneGeometry.Top; y += .025)
        {
            var hit = resolver.Resolve(swing with { CrossingX = 0, CrossingY = y }, Park, new Random(7));
            Assert.NotEqual(ContactQuality.Miss, hit.Quality);
            if (angles.Count > 0) Assert.InRange(hit.LaunchDeg - angles[^1], 0, .6);
            angles.Add(hit.LaunchDeg);
        }
        Assert.True(angles.Min() < 0, "the low crossing drives the ball down");
        Assert.True(angles.Max() > 30, "the high crossing lofts the same ball");
    }
}
