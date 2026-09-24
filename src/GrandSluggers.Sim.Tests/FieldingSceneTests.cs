using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class FieldingSceneTests
{
    readonly ContentCatalog _content = Shipped.Content;

    [Fact]
    public void NearestGlovePicksThePositionUnderTheBall()
    {
        var match = Match.Slice(_content, seed: 1);
        var assigned = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
        foreach (var pos in new[] { "LF", "CF", "RF", "SS", "2B", "C" })
        {
            var p = Diamond.Positions[pos];
            var picked = FieldingResolver.NearestGlove(assigned, p.X, p.Z);
            Assert.Equal(pos, picked.Pos);
            Assert.Equal(assigned[pos].Id, picked.Fielder.Id);
        }
    }

    [Fact]
    public void NearestGloveUsesLiveSpotsWhenAFielderHasMoved()
    {
        var match = Match.Slice(_content, seed: 1);
        var assigned = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
        var at = new Dictionary<string, (double X, double Z)>();
        foreach (var kv in assigned)
            at[kv.Key] = Diamond.Positions[kv.Key];
        at["1B"] = (200, 10);
        var picked = FieldingResolver.NearestGlove(assigned, 200, 10, at);
        Assert.Equal("1B", picked.Pos);
        Assert.Equal(assigned["1B"].Id, picked.Fielder.Id);
    }

    [Fact]
    public void NearestGloveFromRosterMatchesAssign()
    {
        var match = Match.Slice(_content, seed: 3);
        var cf = Diamond.Positions["CF"];
        var a = FieldingResolver.NearestGlove(match.Defense.Roster, match.Pitcher, cf.X, cf.Z);
        var b = FieldingResolver.NearestGlove(FieldingResolver.Assign(match.Defense.Roster, match.Pitcher), cf.X, cf.Z);
        Assert.Equal(a.Pos, b.Pos);
        Assert.Equal(a.Fielder.Id, b.Fielder.Id);
        Assert.Equal("CF", a.Pos);
    }

    [Fact]
    public void ThrowChemistryIsGoodBadOrNeutralForAPair()
    {
        var chem = _content.Chemistry;
        Assert.Equal(Chemistry.Good, chem.ThrowChemistry(_content.Must("rio"), _content.Must("nico")));
        Assert.Equal(Chemistry.Bad, chem.ThrowChemistry(_content.Must("rio"), _content.Must("ashlord")));
        Assert.Equal(Chemistry.Neutral, chem.ThrowChemistry(_content.Must("frost"), _content.Must("vine")));
    }

    [Fact]
    public void BadChemistrySlantsAShareOfThrowsAndGoodIsFaster()
    {
        // §8.5: bad chemistry is a chance of a slanted throw (slower, off the cover by 10–14 ft);
        // the rest are ordinary. Good chemistry is faster. Every arm carries its own lateral σ.
        // The C80 copy (#722): bad chemistry never slants (chem.slantChance 0); every bad throw is the slow one (badSpeedMul 0.90).
        var chem = _content.Chemistry;
        var rules = _content.Rules.Fielding;
        var rio = _content.Must("rio");
        var slanted = 0;
        for (var seed = 0; seed < 400; seed++)
        {
            var rng = new Random(seed);
            var good = chem.FieldingThrow(rio, _content.Must("nico"), rng);
            var bad = chem.FieldingThrow(rio, _content.Must("ashlord"), rng);
            var neu = chem.FieldingThrow(_content.Must("frost"), _content.Must("vine"), rng);
            Assert.Equal(Chemistry.Good, good.Relation);
            Assert.Equal(Chemistry.Bad, bad.Relation);
            Assert.Equal(Chemistry.Neutral, neu.Relation);
            Assert.Equal(rules.Chem.GoodSpeedMul, good.SpeedMul);
            Assert.False(good.Slanted);
            Assert.Equal(1.0, neu.SpeedMul);
            if (bad.Slanted)
            {
                slanted++;
                Assert.Equal(rules.Chem.SlantSpeedMul, bad.SpeedMul);
                Assert.InRange(Math.Abs(bad.LateralFt), rules.Chem.SlantLateralMinFt, rules.Chem.SlantLateralMaxFt);
                Assert.True(Math.Abs(bad.LateralFt) > rules.Cover.RadiusFt, "a slanted throw misses the cover's reach");
            }
            else
                Assert.Equal(0.90, bad.SpeedMul);
        }
        Assert.Equal(0, slanted);
    }

    [Fact]
    public void DiveAndJumpExtendTheCatchWindow()
    {
        var c = Rules.Default.Fielding.Catch;
        Assert.Equal(1, c.WindowPadFt);
        Assert.Equal(2, c.DiveReachFt);
        // The C80 copy (#719): the jump is a leap of the body (catch.jumpAirSec), not feet on the window.
        Assert.Equal(0, c.JumpReachFt);
        const double radius = 10;
        Assert.Equal(radius, FieldingResolver.StandUpCatchFt(radius));
        Assert.Equal(radius + c.DiveReachFt, FieldingResolver.DiveCatchFt(radius, rules: Rules.Default));
        var plain = FieldingResolver.CatchWindowFt(radius, false, false, rules: Rules.Default);
        var dive = FieldingResolver.CatchWindowFt(radius, true, false, rules: Rules.Default);
        var jump = FieldingResolver.CatchWindowFt(radius, false, true, rules: Rules.Default);
        var both = FieldingResolver.CatchWindowFt(radius, true, true, rules: Rules.Default);
        Assert.Equal(radius + c.WindowPadFt, plain);
        Assert.Equal(plain + c.DiveReachFt, dive);
        Assert.Equal(plain + c.JumpReachFt, jump);
        Assert.Equal(dive, both);
        Assert.True(both > jump);
        var lunged = FieldDash.Lunge(0, 0, 30, 0, Rules.Default, 10);
        Assert.InRange(lunged.X, 9, 11);
        Assert.Equal(0, lunged.Z);
    }

    [Fact]
    public void ResolveFieldingReusesPreview()
    {
        var match = Match.Slice(_content, seed: 7);
        var hit = new AtBatResult(ContactQuality.Nice, true, false, 88, 22, 240, false, false, null, null, SprayDeg: -8);
        var pre = match.PreviewHit(hit);
        var field = match.ResolveFielding(hit, pre);
        Assert.Equal(pre.Fielder.Id, field.Fielder?.Id);
        Assert.Equal(pre.LandingX, field.LandingX);
        Assert.Equal(pre.LandingZ, field.LandingZ);
    }

    [Fact]
    public void BuddyJumpNeedsTwoGoodChemOutfieldersUnderAHomer()
    {
        var dart = _content.Must("dart");
        var zig = _content.Must("zig");
        var lace = _content.Must("lace");
        var offered = FlightFixtures.Preview(dart, "CF", BattedBallClass.Homer, 4.2, 0, 390, zig);
        Assert.True(FieldingResolver.BuddyJumpOffered(offered));

        Assert.False(FieldingResolver.BuddyJumpOffered(offered with { Buddy = null }));
        Assert.False(FieldingResolver.BuddyJumpOffered(offered with { Class = BattedBallClass.Fly }));
        Assert.False(FieldingResolver.BuddyJumpOffered(offered with { Class = BattedBallClass.Grounder }));
        Assert.False(FieldingResolver.BuddyJumpOffered(offered with { Fielder = lace, Position = "SS" }));
    }

    [Fact]
    public void PreviewOffersBuddyJumpOnASparkCenterHomer()
    {
        var spark = PresetTeams.SparkAllStars(_content);
        var park = _content.Parks["harbor-diamond"];
        var rio = _content.Must("rio");
        var fielding = new FieldingResolver(_content.Chemistry, rules: Rules.Default);
        var homer = FlightFixtures.OverTheFence(park, 5, 0, 60);
        var pre = fielding.Preview(homer, park, spark.Roster, rio, new Random(1));
        Assert.Equal("CF", pre.Position);
        Assert.Equal("dart", pre.Fielder.Id);
        Assert.NotNull(pre.Buddy);
        Assert.Equal("zig", pre.Buddy.Id);
        Assert.Equal(Chemistry.Good, _content.Chemistry.Between(pre.Fielder, pre.Buddy));
        Assert.True(pre.HomeRunLikely);
        Assert.True(FieldingResolver.BuddyJumpOffered(pre));
        Assert.True(FieldingResolver.IsOutfield(pre.Position));
    }

    [Fact]
    public void PreviewWithholdsBuddyJumpWithoutTheSetPiece()
    {
        var spark = PresetTeams.SparkAllStars(_content);
        var mixed = PresetTeams.MixedRivals(_content);
        var park = _content.Parks["harbor-diamond"];
        var fielding = new FieldingResolver(_content.Chemistry, rules: Rules.Default);
        var rng = new Random(1);

        var can = fielding.Preview(Fly(180, 28, 0), park, spark.Roster, spark.Captain, rng);
        Assert.False(can.HomeRunLikely);
        Assert.False(FieldingResolver.BuddyJumpOffered(can));

        var ground = fielding.Preview(Fly(70, 8, 4), park, spark.Roster, spark.Captain, rng);
        Assert.True(ground.Grounder);
        Assert.False(FieldingResolver.BuddyJumpOffered(ground));

        var infield = fielding.Preview(Fly(140, 22, -18), park, spark.Roster, spark.Captain, rng);
        Assert.False(FieldingResolver.IsOutfield(infield.Position));
        Assert.Null(infield.Buddy);
        Assert.False(FieldingResolver.BuddyJumpOffered(infield));

        var rivals = fielding.Preview(FlightFixtures.OverTheFence(park, 10, 0), park, mixed.Roster, mixed.Captain, rng);
        Assert.True(rivals.HomeRunLikely);
        Assert.Null(rivals.Buddy);
        Assert.False(FieldingResolver.BuddyJumpOffered(rivals));
    }

    [Fact]
    public void OutfieldGrassStartsPastTheInfieldLip()
    {
        var ss = Diamond.Positions["SS"];
        var cf = Diamond.Positions["CF"];
        Assert.False(FieldingResolver.OutfieldGrass(ss.X, ss.Z, rules: Rules.Default));
        Assert.False(FieldingResolver.OutfieldGrass(0, Rules.Default.Flight.Classes.InfieldLipFt - 1, rules: Rules.Default));
        Assert.True(FieldingResolver.OutfieldGrass(0, Rules.Default.Flight.Classes.InfieldLipFt, rules: Rules.Default));
        Assert.True(FieldingResolver.OutfieldGrass(cf.X, cf.Z, rules: Rules.Default));
    }

    [Fact]
    public void PlayGloveHandsTheHopToTheOutfielderOnceTheBallIsOnTheGrass()
    {
        var match = Match.Slice(_content, seed: 1);
        var assigned = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
        // The C80 copy: the lip is 137.78 ft, not 155, and the infield stands at 8/9 of the depth. The same three balls at 8/9.
        var dirt = FieldingResolver.PlayGlove(assigned, 0, 107, rules: Rules.Default);
        Assert.False(FieldingResolver.IsOutfield(dirt.Pos));
        Assert.Equal("2B", dirt.Pos);

        var overTheInfield = FieldingResolver.PlayGlove(assigned, 0, 124, rules: Rules.Default);
        Assert.False(FieldingResolver.IsOutfield(overTheInfield.Pos),
            "ball still on the dirt stays an infielder — they chase the hop over their head");

        var grass = FieldingResolver.PlayGlove(assigned, 0, 178, rules: Rules.Default);
        Assert.True(FieldingResolver.IsOutfield(grass.Pos));
        Assert.Equal("CF", grass.Pos);
        Assert.True(FieldingResolver.HandoffToOutfield(dirt.Pos, grass.Pos));
        Assert.False(FieldingResolver.HandoffToOutfield(grass.Pos, dirt.Pos));
    }

    [Fact]
    public void OutfielderChargesTheLandingThenTheLiveHop()
    {
        Assert.True(FieldingResolver.OutfieldShouldCharge(0, 80, 0, 220, rules: Rules.Default));
        Assert.False(FieldingResolver.OutfieldShouldCharge(0, 80, 0, 70, rules: Rules.Default));
        Assert.True(FieldingResolver.OutfieldShouldCharge(0, 180, 0, 70, rules: Rules.Default));
        var toLanding = FieldingResolver.OutfieldChaseTarget(0, 80, -40, 220, rules: Rules.Default);
        Assert.Equal(-40, toLanding.X);
        Assert.Equal(220, toLanding.Z);
        var toLive = FieldingResolver.OutfieldChaseTarget(12, 190, -40, 220, rules: Rules.Default);
        Assert.Equal(12, toLive.X);
        Assert.Equal(190, toLive.Z);
        var stillUp = FieldingResolver.OutfieldChaseTarget(12, 190, -40, 220, inAir: true, rules: Rules.Default);
        Assert.Equal(-40, stillUp.X);
        Assert.Equal(220, stillUp.Z);

        var match = Match.Slice(_content, seed: 1);
        var assigned = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
        // The C80 copy: LF starts at (-77, 175), on top of the shipped ball. The same ball at 0.70 of the depth, in front of LF.
        var (ballX, ballZ) = (-56.0, 126.0);
        var of = FieldingResolver.NearestOutfielder(assigned, ballX, ballZ);
        Assert.Equal("LF", of.Pos);
        var start = Diamond.Positions["LF"];
        var speed = FieldingResolver.ChaseSpeedFt(of.Fielder, frozen: false, rules: Rules.Default);
        var stepped = start;
        for (var i = 0; i < 45; i++)
            stepped = FieldingResolver.StepToward(stepped.X, stepped.Z, ballX, ballZ, speed, 1.0 / 30, rules: Rules.Default);
        Assert.True(Diamond.Dist(stepped.X, stepped.Z, ballX, ballZ)
                    < Diamond.Dist(start.X, start.Z, ballX, ballZ) - 20,
            "outfielder must close on a ball in the grass, not stay on the pad");
    }

    [Fact]
    public void DeepHopperStaysTheInfielderUntilPlayGloveHandsOff()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielding = new FieldingResolver(_content.Chemistry, rules: Rules.Default);
        var assigned = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
        var deep = FlightFixtures.Hit(match.Park, 104, 9, 0);
        Assert.True(deep.Class.OnTheDirt());
        var pre = fielding.Preview(deep, match.Park, match.Defense.Roster, match.Pitcher, new Random(1));
        Assert.True(pre.Grounder);
        Assert.True(FieldingResolver.OutfieldGrass(pre.LandingX, pre.LandingZ, rules: Rules.Default));
        Assert.False(FieldingResolver.IsOutfield(pre.Position),
            "infielder still owns the first run so they chase a ball over their head");
        Assert.True(FieldingResolver.OutfieldShouldCharge(0, 80, pre.LandingX, pre.LandingZ, rules: Rules.Default));
        var onGrass = FieldingResolver.PlayGlove(assigned, pre.LandingX, pre.LandingZ, rules: Rules.Default);
        Assert.True(FieldingResolver.IsOutfield(onGrass.Pos));
        Assert.True(FieldingResolver.HandoffToOutfield(pre.Position, onGrass.Pos));
    }

    [Fact]
    public void LineDriveIsInfieldWindowNotAFlyRing()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielding = new FieldingResolver(_content.Chemistry, rules: Rules.Default);
        var liner = FlightFixtures.Hit(match.Park, 95, 16, 6);
        Assert.Equal(BattedBallClass.Liner, liner.Class);
        var pre = fielding.Preview(liner, match.Park, match.Defense.Roster, match.Pitcher, new Random(1));
        Assert.True(pre.Line);
        Assert.False(pre.Grounder);
        Assert.False(FieldingResolver.IsOutfield(pre.Position));
        Assert.False(FieldingResolver.BuddyJumpOffered(pre));
        var flyHang = BallFlight.HangTime(BallFlight.Trajectory(95, 28, 0, rules: Rules.Default), rules: Rules.Default);
        Assert.True(pre.HangTimeSec < flyHang, $"line hang {pre.HangTimeSec} vs fly {flyHang}");
    }

    [Fact]
    public void GloveChaseOnAFlyIsTheLandingNotTheLiveBall()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielding = new FieldingResolver(_content.Chemistry, rules: Rules.Default);
        // The C80 copy: Harbor's center fence is 280, so the shipped 280-ft fly meets the wall. The same fly at 0.70 of the carry.
        var fly = Fly(196, 28, 8);
        Assert.Equal(BattedBallClass.Fly, fly.Class);
        var pre = fielding.Preview(fly, match.Park, match.Defense.Roster, match.Pitcher, new Random(1));
        Assert.False(pre.Grounder);
        Assert.True(FieldingResolver.InAir(pre, ballY: 18, hitT: 0.25, rules: Rules.Default));

        var chase = FieldingResolver.GloveChaseTarget(pre, match.Park, ballX: 3, ballZ: 14, ballY: 18, hitT: 0.25, rules: Rules.Default);
        var plant = FlyCatch.ChaseTarget(pre, Rules.Default, match.Park);
        Assert.Equal(plant.X, chase.X, 3);
        Assert.Equal(plant.Z, chase.Z, 3);
        Assert.True(Diamond.Dist(chase.X, chase.Z, 0, 0) > 150,
            "air chase must be the landing, not the plate");

        var start = Diamond.Positions[pre.Position];
        var at = start;
        var speed = FieldingResolver.ChaseSpeedFt(pre.Fielder, frozen: false, rules: Rules.Default);
        for (var i = 0; i < 24; i++)
            at = FieldingResolver.StepToward(at.X, at.Z, chase.X, chase.Z, speed, 1.0 / 30, rules: Rules.Default);
        Assert.True(Diamond.Dist(at.X, at.Z, chase.X, chase.Z)
                    < Diamond.Dist(start.X, start.Z, chase.X, chase.Z) - 12,
            "glove must close on the landing");
        Assert.True(Diamond.Dist(at.X, at.Z, chase.X, chase.Z)
                    < Diamond.Dist(at.X, at.Z, 3, 14),
            "closer to the landing than to the live ball at home");

        var assigned = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
        var liveGlove = FieldingResolver.PlayGlove(assigned, 3, 14, rules: Rules.Default);
        Assert.False(FieldingResolver.IsOutfield(liveGlove.Pos),
            "live XZ near home is an infielder — that is the bug if chase used it");
        var landingGlove = FieldingResolver.PlayGlove(assigned, chase.X, chase.Z, rules: Rules.Default);
        Assert.True(FieldingResolver.IsOutfield(landingGlove.Pos));
    }

    [Fact]
    public void GloveChaseOnALinerInTheAirIsStillTheLanding()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielding = new FieldingResolver(_content.Chemistry, rules: Rules.Default);
        var liner = FlightFixtures.Hit(match.Park, 95, 16, 6);
        Assert.Equal(BattedBallClass.Liner, liner.Class);
        var pre = fielding.Preview(liner, match.Park, match.Defense.Roster, match.Pitcher, new Random(1));
        Assert.True(pre.Line);
        Assert.True(FieldingResolver.InAir(pre, ballY: 7, hitT: 0.2, rules: Rules.Default));

        var chase = FieldingResolver.GloveChaseTarget(pre, match.Park, ballX: 5, ballZ: 16, ballY: 7, hitT: 0.2, rules: Rules.Default);
        Assert.Equal(pre.LandingX, chase.X, 3);
        Assert.Equal(pre.LandingZ, chase.Z, 3);
        Assert.True(Diamond.Dist(chase.X, chase.Z, 0, 0) > Diamond.Dist(5, 16, 0, 0) + 40,
            "liner still up must not chase the live ball near home");
    }

    [Fact]
    public void GloveChaseOnAHopperIsTheLiveHop()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielding = new FieldingResolver(_content.Chemistry, rules: Rules.Default);
        var hopper = FlightFixtures.Hit(match.Park, 88, 8, -12);
        Assert.True(hopper.Class.OnTheDirt());
        var pre = fielding.Preview(hopper, match.Park, match.Defense.Roster, match.Pitcher, new Random(1));
        Assert.True(pre.Grounder);
        Assert.False(FieldingResolver.InAir(pre, ballY: 4, hitT: 0.1, rules: Rules.Default));

        var chase = FieldingResolver.GloveChaseTarget(pre, match.Park, ballX: 18, ballZ: 62, ballY: 1.2, hitT: 0.35, rules: Rules.Default);
        Assert.Equal(18, chase.X);
        Assert.Equal(62, chase.Z);
    }

    [Fact]
    public void GloveChaseAfterTheBallIsDownIsTheLiveHop()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielding = new FieldingResolver(_content.Chemistry, rules: Rules.Default);
        var fly = Fly(260, 26, -10);
        var pre = fielding.Preview(fly, match.Park, match.Defense.Roster, match.Pitcher, new Random(1));
        Assert.False(pre.Grounder);
        Assert.False(FieldingResolver.InAir(pre, ballY: 0.2, hitT: pre.HangTimeSec + 0.3, rules: Rules.Default));

        var chase = FieldingResolver.GloveChaseTarget(
            pre, match.Park, ballX: 22, ballZ: 205, ballY: 0.2, hitT: pre.HangTimeSec + 0.3, rules: Rules.Default);
        Assert.Equal(22, chase.X);
        Assert.Equal(205, chase.Z);
    }

    [Fact]
    public void FieldBoundsUseEachParkFenceNotAHarborConstant()
    {
        var harbor = _content.Parks["harbor-diamond"];
        var canopy = _content.Parks["canopy-yard"];
        var ember = _content.Parks["ember-keep"];
        // The C80 copy carries every fence at 0.70 (#717): 280 / 265 / 286.
        Assert.Equal(280, harbor.CenterFenceFt);
        Assert.Equal(265, canopy.CenterFenceFt);
        Assert.Equal(286, ember.CenterFenceFt);

        var cf = Diamond.Positions["CF"];
        Assert.True(FieldBounds.Inside(harbor, cf.X, cf.Z));
        Assert.True(FieldBounds.Inside(canopy, cf.X, cf.Z));
        Assert.True(FieldBounds.Inside(harbor, 0, HomeSet.CatcherZ), "catcher stays behind the plate");

        var pastHarbor = FieldBounds.Clamp(harbor, 0, 500);
        var pastCanopy = FieldBounds.Clamp(canopy, 0, 500);
        Assert.True(FieldBounds.Inside(harbor, pastHarbor.X, pastHarbor.Z));
        Assert.True(FieldBounds.Inside(canopy, pastCanopy.X, pastCanopy.Z));
        Assert.True(pastHarbor.Z < 280 - FieldBounds.InsideFt + 0.5);
        Assert.True(pastCanopy.Z < pastHarbor.Z - 10,
            "Canopy's shorter fence must clip sooner than Harbor");
        Assert.False(FieldBounds.Inside(harbor, 0, 500));
        Assert.False(FieldBounds.Inside(canopy, 0, 500));

        var rio = _content.Must("rio");
        var deep = FlightFixtures.Preview(rio, "CF", BattedBallClass.Fly, 4.0, 0, 460);
        var plant = FlyCatch.ChaseTarget(deep, Rules.Default, harbor);
        Assert.True(FieldBounds.Inside(harbor, plant.X, plant.Z),
            "a 460 ft fly chase is the wall, not the seats");
        Assert.True(Diamond.Dist(0, 0, plant.X, plant.Z) < harbor.CenterFenceFt);

        var start = Diamond.Positions["CF"];
        var at = start;
        for (var i = 0; i < 90; i++)
            at = FieldingResolver.StepToward(at.X, at.Z, 0, 520, 28, 1.0 / 30, Rules.Default, harbor);
        Assert.True(FieldBounds.Of(harbor).Contains(at.X, at.Z), "running at the wall stays inside the field");
        var clearance = _content.Rules.Fielding.Chase.WallClearanceFt;
        Assert.InRange(harbor.CenterFenceFt - at.Z, clearance - .01, clearance + .5);
    }

    /// <summary>A real fly that lands <paramref name="carry"/> out in the open; Harbor's fence then says whether it is gone.</summary>
    AtBatResult Fly(double carry, double launch, double spray, bool hr = false)
    {
        var hit = FlightFixtures.Landing(_content.Parks["harbor-diamond"], carry, launch, spray);
        Assert.Equal(hr, hit.HomeRun);
        return hit;
    }
}
