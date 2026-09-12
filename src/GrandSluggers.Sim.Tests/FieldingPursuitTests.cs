using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class FieldingPursuitTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void GroundBallRoutesCutToAReachableFutureHopAcrossSpraysAndSpeeds()
    {
        foreach (var park in _content.Parks.Values)
        foreach (var exit in new[] { 68d, 84d, 102d })
        foreach (var spray in new[] { -28d, 0d, 28d })
        {
            var match = Match.Slice(_content, parkId: park.Id, seed: 7);
            var path = BallFlight.Trajectory(exit, 8, park.WindMph);
            var carry = BallFlight.FirstLandingDist(path);
            var hit = new AtBatResult(ContactQuality.Solid, true, false, exit, 8, carry,
                false, false, null, null, SprayDeg: spray);
            var pre = match.PreviewHit(hit);
            var start = Diamond.Positions[pre.Position];
            var speed = FieldingResolver.ChaseSpeedFt(pre.Fielder, pre.Frozen);
            var route = FieldingPursuit.Plan(pre, park, path, spray, 0, start.X, start.Z, speed);
            var early = BallFlight.PointAt(path, spray, Math.Min(0.2, route.MeetTimeSec * 0.5));

            Assert.False(route.AirCatch);
            Assert.True(route.MeetTimeSec > 0, $"{park.Id} {exit} mph {spray}°");
            Assert.True(FieldBounds.Inside(park, route.X, route.Z));
            Assert.True(FieldBounds.DistHome(route.X, route.Z) > FieldBounds.DistHome(early.X, early.Z) + 2,
                $"route must cut ahead of the live hop: {park.Id} {exit} mph {spray}°");
            if (route.Reachable)
                Assert.True(route.TravelTimeSec <= route.AvailableSec + 0.02,
                    $"reachable route {route.TravelTimeSec:0.00}s > {route.AvailableSec:0.00}s");
        }
    }

    [Fact]
    public void ReplanningAHopperKeepsTheGloveAheadInsteadOfFollowingFromBehind()
    {
        var match = Match.Slice(_content, seed: 4);
        var park = match.Park;
        const double exit = 92;
        const double spray = -24;
        var path = BallFlight.Trajectory(exit, 7, park.WindMph);
        var hit = new AtBatResult(ContactQuality.Solid, true, false, exit, 7,
            BallFlight.FirstLandingDist(path), false, false, null, null, SprayDeg: spray);
        var pre = match.PreviewHit(hit);
        var at = Diamond.Positions[pre.Position];
        var speed = FieldingResolver.ChaseSpeedFt(pre.Fielder, pre.Frozen);
        const double dt = 1.0 / 30;

        for (var t = 0.0; t < Math.Min(1.4, BallFlight.RestTime(path)); t += dt)
        {
            var live = BallFlight.PointAt(path, spray, t);
            var route = FieldingPursuit.Plan(pre, park, path, spray, t, at.X, at.Z, speed);
            Assert.True(FieldBounds.DistHome(route.X, route.Z) + 0.1 >= FieldBounds.DistHome(live.X, live.Z),
                $"target fell behind the ball at {t:0.00}s");
            at = FieldingResolver.StepToward(at.X, at.Z, route.X, route.Z, speed, dt, park);
        }
    }

    [Fact]
    public void FlyRoutesUseTheLegalPlantAtRatedSpeedIncludingTheWall()
    {
        foreach (var park in _content.Parks.Values)
        foreach (var spray in new[] { -32d, 0d, 32d })
        {
            var match = Match.Slice(_content, parkId: park.Id, seed: 3);
            var path = BallFlight.Trajectory(104, 30, park.WindMph);
            var carry = BallFlight.FirstLandingDist(path);
            var hit = new AtBatResult(ContactQuality.Perfect, true, false, 104, 30, carry,
                carry >= AtBatResolver.FenceAt(park, spray), false, null, null, SprayDeg: spray);
            var pre = match.PreviewHit(hit);
            var start = Diamond.Positions[pre.Position];
            var speed = FieldingResolver.ChaseSpeedFt(pre.Fielder, pre.Frozen);
            var route = FieldingPursuit.Plan(pre, park, path, spray, 0, start.X, start.Z, speed);
            var plant = FlyCatch.ChaseTarget(pre, park);

            Assert.True(route.AirCatch);
            Assert.Equal(plant.X, route.X, 6);
            Assert.Equal(plant.Z, route.Z, 6);
            Assert.True(FieldBounds.Inside(park, route.X, route.Z));

            var at = start;
            const double dt = 1.0 / 60;
            var elapsed = 0.0;
            while (elapsed + dt <= route.AvailableSec)
            {
                at = FieldingResolver.StepToward(at.X, at.Z, route.X, route.Z, speed, dt, park);
                elapsed += dt;
            }
            var traveled = Diamond.Dist(start.X, start.Z, at.X, at.Z);
            Assert.True(traveled <= speed * elapsed + 0.05,
                $"{park.Id} {spray}° route exceeded rated speed");
            Assert.Equal(route.Reachable, Diamond.Dist(at.X, at.Z, route.X, route.Z) < 1.0);
        }
    }

    [Fact]
    public void SelectionUsesArrivalTimeAndOneWayGrassHandoffStillApplies()
    {
        var match = Match.Slice(_content, seed: 1);
        var park = match.Park;
        var assigned = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
        var path = BallFlight.Trajectory(88, 28, park.WindMph);
        var landing = BallFlight.GroundPoint(BallFlight.FirstLandingDist(path), 20);
        var pre = new FieldingPreview(assigned["CF"], "CF", null, BallFlight.HangTime(path),
            landing.X, landing.Z, false, false, false, false, false, 14);
        var live = assigned.ToDictionary(pair => pair.Key, pair => Diamond.Positions[pair.Key]);
        live["LF"] = (landing.X - 36, landing.Z);
        live["RF"] = (landing.X + 26, landing.Z);
        assigned["LF"] = assigned["LF"] with { Stats = assigned["LF"].Stats with { Run = 10 } };
        assigned["RF"] = assigned["RF"] with { Stats = assigned["RF"].Stats with { Run = 1 } };

        var choice = FieldingPursuit.Choose(assigned, FieldingResolver.OutfieldPursuitPositions,
            pre, park, path, 20, live);
        Assert.Equal("LF", choice.Position);
        Assert.True(choice.Route.TravelTimeSec
            < FieldingPursuit.Plan(pre, park, path, 20, 0, live["RF"].X, live["RF"].Z,
                FieldingResolver.ChaseSpeedFt(assigned["RF"], false)).TravelTimeSec);

        Assert.True(FieldingResolver.HandoffToOutfield("SS", choice.Position));
        Assert.False(FieldingResolver.HandoffToOutfield(choice.Position, "SS"));
    }
}
