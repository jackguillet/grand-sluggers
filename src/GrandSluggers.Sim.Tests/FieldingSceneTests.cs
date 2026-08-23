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
}
