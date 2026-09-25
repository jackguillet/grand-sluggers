using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class FieldingPursuitTests
{
    readonly ContentCatalog _content = Shipped.Content;

    [Fact]
    public void GroundBallRoutesCutToAReachableFutureHopAcrossSpraysAndSpeeds()
    {
        foreach (var park in _content.Parks.Values)
        foreach (var exit in new[] { 68d, 84d, 102d })
        foreach (var spray in new[] { -28d, 0d, 28d })
        {
            var match = Match.Slice(_content, parkId: park.Id, seed: 7);
            var hit = FlightFixtures.Hit(park, exit, -12, spray);
            var path = BallFlight.Trajectory(exit, -12, spray, park, Rules.Default);
            var pre = match.PreviewHit(hit);
            var start = Diamond.Positions[pre.Position];
            var speed = FieldingResolver.ChaseSpeedFt(pre.Fielder, pre.Frozen, rules: Rules.Default);
            var route = FieldingPursuit.Plan(pre, park, path, 0, start.X, start.Z, speed, rules: Rules.Default);
            var early = BallFlight.PointAt(path, Math.Min(0.2, route.MeetTimeSec * 0.5), rules: Rules.Default);

            Assert.False(route.AirCatch);
            Assert.True(route.MeetTimeSec > 0, $"{park.Id} {exit} mph {spray}°");
            Assert.True(FieldBounds.Inside(park, route.X, route.Z));
            Assert.True(FieldBounds.DistHome(route.X, route.Z) > FieldBounds.DistHome(early.X, early.Z) + 2,
                $"route must cut ahead of the live hop: {park.Id} {exit} mph {spray}°");
            // Reachable carries chase.reachSlackFt (0.35 ft) of slack, which is under 0.02 s at the shipped legs. The C80 copy's
            // legs are slower (12.4 + 1.12 x Run ft/s), so the same slack is more time: the bound there is the slack over the speed.
            var slackSec = (Rules.Default.Fielding.Chase.ReachSlackFt / speed + 1e-6);
            if (route.Reachable)
                Assert.True(route.TravelTimeSec <= route.AvailableSec + slackSec,
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
        var path = BallFlight.Trajectory(exit, 7, spray, park, Rules.Default);
        var hit = FlightFixtures.Hit(park, exit, 7, spray);
        var pre = match.PreviewHit(hit);
        var at = Diamond.Positions[pre.Position];
        var speed = FieldingResolver.ChaseSpeedFt(pre.Fielder, pre.Frozen, rules: Rules.Default);
        const double dt = 1.0 / 30;

        for (var t = 0.0; t < Math.Min(1.4, BallFlight.RestTime(path)); t += dt)
        {
            var live = BallFlight.PointAt(path, t, rules: Rules.Default);
            var route = FieldingPursuit.Plan(pre, park, path, t, at.X, at.Z, speed, rules: Rules.Default);
            Assert.True(FieldBounds.DistHome(route.X, route.Z) + 0.1 >= FieldBounds.DistHome(live.X, live.Z),
                $"target fell behind the ball at {t:0.00}s");
            at = FieldingResolver.StepToward(at.X, at.Z, route.X, route.Z, speed, dt, Rules.Default, park);
        }
    }

    [Fact]
    public void FlyRoutesMeetTheBallWithinReachAtRatedSpeedIncludingTheWall()
    {
        foreach (var park in _content.Parks.Values)
        foreach (var spray in new[] { -32d, 0d, 32d })
        {
            var match = Match.Slice(_content, parkId: park.Id, seed: 3);
            var path = BallFlight.Trajectory(104, 30, spray, park, Rules.Default);
            var hit = FlightFixtures.Hit(park, 104, 30, spray, ContactQuality.Perfect);
            var pre = match.PreviewHit(hit);
            var start = Diamond.Positions[pre.Position];
            var speed = FieldingResolver.ChaseSpeedFt(pre.Fielder, pre.Frozen, rules: Rules.Default);
            var route = FieldingPursuit.Plan(pre, park, path, 0, start.X, start.Z, speed, rules: Rules.Default);
            var plant = FlyCatch.ChaseTarget(pre, Rules.Default, park);

            if (route.AirCatch)
            {
                var point = BallFlight.PointAt(path, route.MeetTimeSec, rules: Rules.Default);
                var target = FlyCatch.NeedsJump(pre) ? plant : (X: point.X, Z: point.Z);
                Assert.Equal(target.X, route.X, 6);
                Assert.Equal(target.Z, route.Z, 6);
                if (!FlyCatch.NeedsJump(pre))
                {
                    Assert.True(route.MeetTimeSec < pre.HangTimeSec);
                    Assert.InRange(point.Y, 0, match.Rules.Fielding.Catch.StandingHeightFt);
                }
            }
            else if (route.Reachable) Assert.True(route.MeetTimeSec >= pre.HangTimeSec);
            Assert.True(FieldBounds.Inside(park, route.X, route.Z));

            var at = start;
            const double dt = 1.0 / 60;
            var elapsed = 0.0;
            while (elapsed + dt <= route.AvailableSec)
            {
                at = FieldingResolver.StepToward(at.X, at.Z, route.X, route.Z, speed, dt, Rules.Default, park);
                elapsed += dt;
            }
            var traveled = Diamond.Dist(start.X, start.Z, at.X, at.Z);
            Assert.True(traveled <= speed * elapsed + 0.05,
                $"{park.Id} {spray}° route exceeded rated speed");
            if (route.Reachable) Assert.True(Diamond.Dist(at.X, at.Z, route.X, route.Z) < pre.CatchRadius + 1.0);
        }
    }

    [Fact]
    public void SelectionUsesArrivalTimeAndOneWayGrassHandoffStillApplies()
    {
        var match = Match.Slice(_content, seed: 1);
        var park = match.Park;
        var assigned = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
        var path = BallFlight.Trajectory(88, 28, 20, park, Rules.Default);
        var landing = BallFlight.LandingPoint(path);
        var pre = FlightFixtures.Preview(assigned["CF"], "CF", BattedBallClass.Fly, BallFlight.HangTime(path, rules: Rules.Default),
            landing.X, landing.Z);
        var live = assigned.ToDictionary(pair => pair.Key, pair => Diamond.Positions[pair.Key]);
        live["LF"] = (landing.X - 36, landing.Z);
        live["RF"] = (landing.X + 26, landing.Z);
        assigned["LF"] = assigned["LF"] with { Stats = assigned["LF"].Stats with { Run = 10 } };
        assigned["RF"] = assigned["RF"] with { Stats = assigned["RF"].Stats with { Run = 1 } };

        var choice = FieldingPursuit.Choose(assigned, FieldingResolver.OutfieldPursuitPositions,
            pre, park, path, Rules.Default, live);
        Assert.Equal("LF", choice.Position);
        Assert.True(choice.Route.TravelTimeSec
            < FieldingPursuit.Plan(pre, park, path, 0, live["RF"].X, live["RF"].Z,
                FieldingResolver.ChaseSpeedFt(assigned["RF"], "RF", pre, Rules.Default), Rules.Default).TravelTimeSec);

        Assert.True(FieldingResolver.HandoffToOutfield("SS", choice.Position));
        Assert.False(FieldingResolver.HandoffToOutfield(choice.Position, "SS"));
    }

    // #667: a liner that bounces in the gap (the corner starts closer) and rolls to the wall is CF's
    // when CF's roll meets it first — not nearest to the bounce. Harbor RC is the sitting; both gaps
    // and every park where that relationship holds are the rail.
    [Fact]
    public void ALinerThatBouncesInTheGapAndRollsToTheWallIsTheOutfielderWhoMeetsTheRoll()
    {
        // The C80 copy: the shipped 90 mph liner at 14° beats CF to the wall in every park (CF is still the choice, 13 to 26 ft
        // short), so nobody meets that roll. 78 mph between 8° and 12° is the gap liner CF runs down in every park there.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var exit = 78;
        // Every park still at today's numbers: on Crystal's ice (F9-a) the same gap liner runs past the centre fielder to the
        // glass, which is the park's point (CrystalRinkTests.SF11_TheSameGrounderRunsFartherOnTheIce).
        // ±12° left the sweep with F2-d: at a deeper park the centre fielder now starts at his fraction of its fence, and the
        // widest gap liner is the corner's there. The relationship is held where it holds (the note above).
        foreach (var spray in (new[] { 10d, -10d, 8d, -8d }))
        foreach (var park in TodaysParks.Of(_content))
        {
            var corner = spray > 0 ? "RF" : "LF";
            var match = Match.Slice(_content, parkId: park.Id, seed: 1);
            var hit = FlightFixtures.Hit(park, exit, 16, spray);
            var pre = match.PreviewHit(hit);
            if (!pre.Line) continue;
            var path = pre.Ball!.Samples;
            var live = BallFlight.PointAt(path, 0, match.Rules);
            if (!FieldingResolver.InAir(pre, live.Y, 0, match.Rules, pre.HangTimeSec)) continue;
            var atCorner = Diamond.Positions[corner];
            var atCf = Diamond.Positions["CF"];
            if (Diamond.Dist(atCorner.X, atCorner.Z, pre.LandingX, pre.LandingZ) + 8
                >= Diamond.Dist(atCf.X, atCf.Z, pre.LandingX, pre.LandingZ))
                continue;
            var assigned = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
            var ready = FieldingResolver.CpuReactionLockouts(match.Rules, pre.HangTimeSec);
            var cornerRoute = FieldingPursuit.Plan(pre, park, path, 0, atCorner.X, atCorner.Z,
                FieldingResolver.ChaseSpeedFt(assigned[corner], corner, pre, match.Rules), match.Rules, ready[corner], body: assigned[corner]);
            if (cornerRoute.AirCatch) continue;
            var cfRoute = FieldingPursuit.Plan(pre, park, path, 0, atCf.X, atCf.Z,
                FieldingResolver.ChaseSpeedFt(assigned["CF"], "CF", pre, match.Rules), match.Rules, ready["CF"], body: assigned["CF"]);
            if (!FieldingPursuit.Better(cfRoute, cornerRoute)) continue;
            var choice = FieldingPursuit.Choose(assigned, FieldingResolver.OutfieldPursuitPositions,
                pre, park, path, match.Rules, null, 0, ready);
            Assert.Equal("CF", choice.Position);
            Assert.False(choice.Route.AirCatch);
            Assert.True(choice.Route.Reachable, $"{park.Id} {spray}°: CF reaches the roll");
            Assert.True(FieldBounds.DistHome(choice.Route.X, choice.Route.Z)
                        > FieldBounds.DistHome(pre.LandingX, pre.LandingZ) + 20,
                $"{park.Id} {spray}°: the meet is the wall, not the bounce");
            seen.Add($"{park.Id}:{spray:0}");
        }
        Assert.Contains("harbor-diamond:10", seen);
    }

    [Fact]
    public void ACatchableFlyToRightCenterPicksAnAirInterceptBeforeTheBounce()
    {
        var match = Match.Slice(_content, seed: 1);
        var park = match.Park;
        var hit = FlightFixtures.Hit(park, 95, 35, 20);
        var pre = match.PreviewHit(hit);
        Assert.Equal(BattedBallClass.Fly, pre.Class);
        var plant = FlyCatch.ChaseTarget(pre, Rules.Default, park);
        var rf = Diamond.Positions["RF"];
        var cf = Diamond.Positions["CF"];
        Assert.True(Diamond.Dist(rf.X, rf.Z, plant.X, plant.Z) + 8
                    < Diamond.Dist(cf.X, cf.Z, plant.X, plant.Z),
            "the fixture: RF starts closer to the landing");
        var assigned = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
        var ready = FieldingResolver.CpuReactionLockouts(match.Rules, pre.HangTimeSec);
        var choice = FieldingPursuit.Choose(assigned, FieldingResolver.OutfieldPursuitPositions,
            pre, park, pre.Ball!.Samples, match.Rules, null, 0, ready);
        Assert.Contains(choice.Position, FieldingResolver.OutfieldPursuitPositions);
        Assert.True(choice.Route.AirCatch);
        var atCatch = BallFlight.PointAt(pre.Ball.Samples, choice.Route.MeetTimeSec, rules: Rules.Default);
        Assert.True(choice.Route.MeetTimeSec < pre.HangTimeSec);
        Assert.InRange(atCatch.Y, 0, match.Rules.Fielding.Catch.StandingHeightFt);
        Assert.Equal(atCatch.X, choice.Route.X, 6);
        Assert.Equal(atCatch.Z, choice.Route.Z, 6);
    }
}
