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

    static AtBatResult Fly(double carry, double launch, double spray, bool hr = false) =>
        new(ContactQuality.Solid, true, false, 95, launch, carry, hr, false, null, null, SprayDeg: spray);
}
