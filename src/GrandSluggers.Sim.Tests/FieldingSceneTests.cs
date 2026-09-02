using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class FieldingSceneTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

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
    public void BadThrowIsSlowerAndOffLine()
    {
        var chem = _content.Chemistry;
        var rng = new Random(7);
        var good = chem.FieldingThrow(_content.Must("rio"), _content.Must("nico"), rng);
        var bad = chem.FieldingThrow(_content.Must("rio"), _content.Must("ashlord"), rng);
        var neu = chem.FieldingThrow(_content.Must("frost"), _content.Must("vine"), rng);
        Assert.Equal(Chemistry.Good, good.Relation);
        Assert.Equal(Chemistry.Bad, bad.Relation);
        Assert.Equal(Chemistry.Neutral, neu.Relation);
        Assert.True(good.SpeedMul > neu.SpeedMul);
        Assert.True(neu.SpeedMul > bad.SpeedMul);
        Assert.Equal(0, good.LateralFt);
        Assert.True(bad.LateralFt > neu.LateralFt);
        Assert.False(good.Error);
    }

    [Fact]
    public void DiveAndJumpExtendTheCatchWindow()
    {
        var plain = FieldingResolver.CatchWindowFt(10, false, false);
        var dive = FieldingResolver.CatchWindowFt(10, true, false);
        var jump = FieldingResolver.CatchWindowFt(10, false, true);
        var both = FieldingResolver.CatchWindowFt(10, true, true);
        Assert.Equal(14, plain);
        Assert.True(dive > plain);
        Assert.True(jump > plain);
        Assert.True(both > dive);
        Assert.True(both > jump);
    }

    [Fact]
    public void ResolveFieldingReusesPreview()
    {
        var match = Match.Slice(_content, seed: 7);
        var hit = new AtBatResult(ContactQuality.Solid, true, false, 88, 22, 240, false, false, null, null, SprayDeg: -8);
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
        var offered = new FieldingPreview(dart, "CF", zig, 4.2, 0, 390, false, true, false, false, false, 14);
        Assert.True(FieldingResolver.BuddyJumpOffered(offered));

        Assert.False(FieldingResolver.BuddyJumpOffered(offered with { Buddy = null }));
        Assert.False(FieldingResolver.BuddyJumpOffered(offered with { HomeRunLikely = false }));
        Assert.False(FieldingResolver.BuddyJumpOffered(offered with { Grounder = true }));
        Assert.False(FieldingResolver.BuddyJumpOffered(offered with { Fielder = lace, Position = "SS" }));
    }

    [Fact]
    public void PreviewOffersBuddyJumpOnASparkCenterHomer()
    {
        var spark = PresetTeams.SparkAllStars(_content);
        var park = _content.Parks["harbor-diamond"];
        var rio = _content.Must("rio");
        var fielding = new FieldingResolver(_content.Chemistry);
        var homer = Fly(395, 28, 0, hr: true);
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
        var fielding = new FieldingResolver(_content.Chemistry);
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

        var rivals = fielding.Preview(Fly(395, 28, 0, hr: true), park, mixed.Roster, mixed.Captain, rng);
        Assert.True(rivals.HomeRunLikely);
        Assert.Null(rivals.Buddy);
        Assert.False(FieldingResolver.BuddyJumpOffered(rivals));
    }

    [Fact]
    public void OutfieldGrassStartsPastTheInfieldLip()
    {
        var ss = Diamond.Positions["SS"];
        var cf = Diamond.Positions["CF"];
        Assert.False(FieldingResolver.OutfieldGrass(ss.X, ss.Z));
        Assert.False(FieldingResolver.OutfieldGrass(0, FieldingResolver.InfieldLipFt - 1));
        Assert.True(FieldingResolver.OutfieldGrass(0, FieldingResolver.InfieldLipFt));
        Assert.True(FieldingResolver.OutfieldGrass(cf.X, cf.Z));
    }

    [Fact]
    public void PlayGloveHandsTheHopToTheOutfielderOnceTheBallIsOnTheGrass()
    {
        var match = Match.Slice(_content, seed: 1);
        var assigned = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
        var dirt = FieldingResolver.PlayGlove(assigned, 0, 120);
        Assert.False(FieldingResolver.IsOutfield(dirt.Pos));
        Assert.Equal("2B", dirt.Pos);

        var overTheInfield = FieldingResolver.PlayGlove(assigned, 0, 140);
        Assert.False(FieldingResolver.IsOutfield(overTheInfield.Pos),
            "ball still on the dirt stays an infielder — they chase the hop over their head");

        var grass = FieldingResolver.PlayGlove(assigned, 0, 200);
        Assert.True(FieldingResolver.IsOutfield(grass.Pos));
        Assert.Equal("CF", grass.Pos);
        Assert.True(FieldingResolver.HandoffToOutfield(dirt.Pos, grass.Pos));
        Assert.False(FieldingResolver.HandoffToOutfield(grass.Pos, dirt.Pos));
    }

    [Fact]
    public void OutfielderChargesTheLandingThenTheLiveHop()
    {
        Assert.True(FieldingResolver.OutfieldShouldCharge(0, 80, 0, 220));
        Assert.False(FieldingResolver.OutfieldShouldCharge(0, 80, 0, 70));
        Assert.True(FieldingResolver.OutfieldShouldCharge(0, 180, 0, 70));
        var toLanding = FieldingResolver.OutfieldChaseTarget(0, 80, -40, 220);
        Assert.Equal(-40, toLanding.X);
        Assert.Equal(220, toLanding.Z);
        var toLive = FieldingResolver.OutfieldChaseTarget(12, 190, -40, 220);
        Assert.Equal(12, toLive.X);
        Assert.Equal(190, toLive.Z);
        var stillUp = FieldingResolver.OutfieldChaseTarget(12, 190, -40, 220, inAir: true);
        Assert.Equal(-40, stillUp.X);
        Assert.Equal(220, stillUp.Z);

        var match = Match.Slice(_content, seed: 1);
        var assigned = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
        var of = FieldingResolver.NearestOutfielder(assigned, -80, 180);
        Assert.Equal("LF", of.Pos);
        var start = Diamond.Positions["LF"];
        var speed = FieldingResolver.ChaseSpeedFt(of.Fielder, frozen: false);
        var stepped = start;
        for (var i = 0; i < 45; i++)
            stepped = FieldingResolver.StepToward(stepped.X, stepped.Z, -80, 180, speed, 1.0 / 30);
        Assert.True(Diamond.Dist(stepped.X, stepped.Z, -80, 180)
                    < Diamond.Dist(start.X, start.Z, -80, 180) - 20,
            "outfielder must close on a ball in the grass, not stay on the pad");
    }

    [Fact]
    public void DeepHopperStaysTheInfielderUntilPlayGloveHandsOff()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielding = new FieldingResolver(_content.Chemistry);
        var assigned = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
        var deep = new AtBatResult(ContactQuality.Solid, true, false, 92, 8, 220, false, false, null, null, SprayDeg: 0);
        Assert.True(FieldingResolver.IsGrounder(deep));
        var pre = fielding.Preview(deep, match.Park, match.Defense.Roster, match.Pitcher, new Random(1));
        Assert.True(pre.Grounder);
        Assert.True(FieldingResolver.OutfieldGrass(pre.LandingX, pre.LandingZ));
        Assert.False(FieldingResolver.IsOutfield(pre.Position),
            "infielder still owns the first run so they chase a ball over their head");
        Assert.True(FieldingResolver.OutfieldShouldCharge(0, 80, pre.LandingX, pre.LandingZ));
        var onGrass = FieldingResolver.PlayGlove(assigned, pre.LandingX, pre.LandingZ);
        Assert.True(FieldingResolver.IsOutfield(onGrass.Pos));
        Assert.True(FieldingResolver.HandoffToOutfield(pre.Position, onGrass.Pos));
    }

    [Fact]
    public void LineDriveIsInfieldWindowNotAFlyRing()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielding = new FieldingResolver(_content.Chemistry);
        var liner = new AtBatResult(ContactQuality.Solid, true, false, 95, 16, 120, false, false, null, null, SprayDeg: 6);
        Assert.True(FieldingResolver.IsLine(liner));
        Assert.False(FieldingResolver.IsGrounder(liner));
        var pre = fielding.Preview(liner, match.Park, match.Defense.Roster, match.Pitcher, new Random(1));
        Assert.True(pre.Line);
        Assert.False(pre.Grounder);
        Assert.False(FieldingResolver.IsOutfield(pre.Position));
        Assert.False(FieldingResolver.BuddyJumpOffered(pre));
        var flyHang = BallFlight.HangTime(BallFlight.Trajectory(95, 28, 0));
        Assert.True(pre.HangTimeSec < flyHang, $"line hang {pre.HangTimeSec} vs fly {flyHang}");
    }

    [Fact]
    public void GloveChaseOnAFlyIsTheLandingNotTheLiveBall()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielding = new FieldingResolver(_content.Chemistry);
        var fly = Fly(280, 28, 8);
        Assert.False(FieldingResolver.IsGrounder(fly));
        Assert.False(FieldingResolver.IsLine(fly));
        var pre = fielding.Preview(fly, match.Park, match.Defense.Roster, match.Pitcher, new Random(1));
        Assert.False(pre.Grounder);
        Assert.True(FieldingResolver.InAir(pre, ballY: 18, hitT: 0.25));

        var chase = FieldingResolver.GloveChaseTarget(pre, match.Park, ballX: 3, ballZ: 14, ballY: 18, hitT: 0.25);
        var plant = FlyCatch.ChaseTarget(pre, match.Park);
        Assert.Equal(plant.X, chase.X, 3);
        Assert.Equal(plant.Z, chase.Z, 3);
        Assert.True(Diamond.Dist(chase.X, chase.Z, 0, 0) > 150,
            "air chase must be the landing, not the plate");

        var start = Diamond.Positions[pre.Position];
        var at = start;
        var speed = FieldingResolver.ChaseSpeedFt(pre.Fielder, frozen: false);
        for (var i = 0; i < 24; i++)
            at = FieldingResolver.StepToward(at.X, at.Z, chase.X, chase.Z, speed, 1.0 / 30);
        Assert.True(Diamond.Dist(at.X, at.Z, chase.X, chase.Z)
                    < Diamond.Dist(start.X, start.Z, chase.X, chase.Z) - 12,
            "glove must close on the landing");
        Assert.True(Diamond.Dist(at.X, at.Z, chase.X, chase.Z)
                    < Diamond.Dist(at.X, at.Z, 3, 14),
            "closer to the landing than to the live ball at home");

        var assigned = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
        var liveGlove = FieldingResolver.PlayGlove(assigned, 3, 14);
        Assert.False(FieldingResolver.IsOutfield(liveGlove.Pos),
            "live XZ near home is an infielder — that is the bug if chase used it");
        var landingGlove = FieldingResolver.PlayGlove(assigned, chase.X, chase.Z);
        Assert.True(FieldingResolver.IsOutfield(landingGlove.Pos));
    }

    [Fact]
    public void GloveChaseOnALinerInTheAirIsStillTheLanding()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielding = new FieldingResolver(_content.Chemistry);
        var liner = new AtBatResult(ContactQuality.Solid, true, false, 95, 16, 120, false, false, null, null, SprayDeg: 6);
        Assert.True(FieldingResolver.IsLine(liner));
        var pre = fielding.Preview(liner, match.Park, match.Defense.Roster, match.Pitcher, new Random(1));
        Assert.True(pre.Line);
        Assert.True(FieldingResolver.InAir(pre, ballY: 7, hitT: 0.2));

        var chase = FieldingResolver.GloveChaseTarget(pre, match.Park, ballX: 5, ballZ: 16, ballY: 7, hitT: 0.2);
        Assert.Equal(pre.LandingX, chase.X, 3);
        Assert.Equal(pre.LandingZ, chase.Z, 3);
        Assert.True(Diamond.Dist(chase.X, chase.Z, 0, 0) > Diamond.Dist(5, 16, 0, 0) + 40,
            "liner still up must not chase the live ball near home");
    }

    [Fact]
    public void GloveChaseOnAHopperIsTheLiveHop()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielding = new FieldingResolver(_content.Chemistry);
        var hopper = new AtBatResult(ContactQuality.Solid, true, false, 88, 8, 90, false, false, null, null, SprayDeg: -12);
        Assert.True(FieldingResolver.IsGrounder(hopper));
        var pre = fielding.Preview(hopper, match.Park, match.Defense.Roster, match.Pitcher, new Random(1));
        Assert.True(pre.Grounder);
        Assert.False(FieldingResolver.InAir(pre, ballY: 4, hitT: 0.1));

        var chase = FieldingResolver.GloveChaseTarget(pre, match.Park, ballX: 18, ballZ: 62, ballY: 1.2, hitT: 0.35);
        Assert.Equal(18, chase.X);
        Assert.Equal(62, chase.Z);
    }

    [Fact]
    public void GloveChaseAfterTheBallIsDownIsTheLiveHop()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielding = new FieldingResolver(_content.Chemistry);
        var fly = Fly(260, 26, -10);
        var pre = fielding.Preview(fly, match.Park, match.Defense.Roster, match.Pitcher, new Random(1));
        Assert.False(pre.Grounder);
        Assert.False(FieldingResolver.InAir(pre, ballY: 0.2, hitT: pre.HangTimeSec + 0.3));

        var chase = FieldingResolver.GloveChaseTarget(
            pre, match.Park, ballX: 22, ballZ: 205, ballY: 0.2, hitT: pre.HangTimeSec + 0.3);
        Assert.Equal(22, chase.X);
        Assert.Equal(205, chase.Z);
    }

    [Fact]
    public void FieldBoundsUseEachParkFenceNotAHarborConstant()
    {
        var harbor = _content.Parks["harbor-diamond"];
        var canopy = _content.Parks["canopy-yard"];
        var ember = _content.Parks["ember-keep"];
        Assert.Equal(400, harbor.CenterFenceFt);
        Assert.Equal(378, canopy.CenterFenceFt);
        Assert.Equal(408, ember.CenterFenceFt);

        var cf = Diamond.Positions["CF"];
        Assert.True(FieldBounds.Inside(harbor, cf.X, cf.Z));
        Assert.True(FieldBounds.Inside(canopy, cf.X, cf.Z));
        Assert.True(FieldBounds.Inside(harbor, 0, HomeSet.CatcherZ), "catcher stays behind the plate");

        var pastHarbor = FieldBounds.Clamp(harbor, 0, 500);
        var pastCanopy = FieldBounds.Clamp(canopy, 0, 500);
        Assert.True(FieldBounds.Inside(harbor, pastHarbor.X, pastHarbor.Z));
        Assert.True(FieldBounds.Inside(canopy, pastCanopy.X, pastCanopy.Z));
        Assert.True(pastHarbor.Z < 400 - FieldBounds.InsideFt + 0.5);
        Assert.True(pastCanopy.Z < pastHarbor.Z - 10,
            "Canopy's shorter fence must clip sooner than Harbor");
        Assert.False(FieldBounds.Inside(harbor, 0, 500));
        Assert.False(FieldBounds.Inside(canopy, 0, 500));

        var rio = _content.Must("rio");
        var deep = new FieldingPreview(rio, "CF", null, 4.0, 0, 460, false, false, false, false, false, 14);
        var plant = FlyCatch.ChaseTarget(deep, harbor);
        Assert.True(FieldBounds.Inside(harbor, plant.X, plant.Z),
            "a 460 ft fly chase is the wall, not the seats");
        Assert.True(Diamond.Dist(0, 0, plant.X, plant.Z) < harbor.CenterFenceFt);

        var start = Diamond.Positions["CF"];
        var at = start;
        for (var i = 0; i < 90; i++)
            at = FieldingResolver.StepToward(at.X, at.Z, 0, 520, 28, 1.0 / 30, harbor);
        Assert.True(FieldBounds.Inside(harbor, at.X, at.Z), "running at the wall stops on the grass");
        Assert.True(at.Z < harbor.CenterFenceFt - 4);
    }

    static AtBatResult Fly(double carry, double launch, double spray, bool hr = false) =>
        new(ContactQuality.Solid, true, false, 95, launch, carry, hr, false, null, null, SprayDeg: spray);
}
