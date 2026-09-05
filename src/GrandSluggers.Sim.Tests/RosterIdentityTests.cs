using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class RosterIdentityTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void CaptainsHaveDistinctSilhouettes()
    {
        var specs = Silhouette.Captains.Select(Silhouette.Proportions).ToList();
        Assert.Equal(Silhouette.Captains.Length, specs.Select(s => (s.Height, s.Width, s.Head, s.Arms, s.Torso)).Distinct().Count());
        Assert.Equal(Silhouette.Captains.Length, specs.Select(s => s.Height).Distinct().Count());
        Assert.Equal(Silhouette.Captains.Length, specs.Select(s => s.Width).Distinct().Count());
        Assert.Equal(Silhouette.Captains.Length, specs.Select(s => s.Head).Distinct().Count());
        Assert.True(Silhouette.ToyScale > 1f);
        Assert.True(Silhouette.GloveScale > 1f);
        Assert.True(Silhouette.BatScale > 1f);
        foreach (var spec in specs)
            Assert.True(Silhouette.HeadToHeight(spec) >= 1f, "face must still read after toy scale");
    }

    [Fact]
    public void RolePlayersReuseFactionBodyType()
    {
        Assert.Equal("rio", Silhouette.BodyType(_content.Must("rio")));
        Assert.Equal("rio", Silhouette.BodyType(_content.Must("nico")));
        Assert.Equal("vale", Silhouette.BodyType(_content.Must("vale")));
        Assert.Equal("vale", Silhouette.BodyType(_content.Must("frost")));
        Assert.Equal("zig", Silhouette.BodyType(_content.Must("dart")));
        Assert.Equal("brondo", Silhouette.BodyType(_content.Must("boom")));
        Assert.Equal("konga", Silhouette.BodyType(_content.Must("vine")));
        Assert.Equal("ashlord", Silhouette.BodyType(_content.Must("cinder")));
        Assert.Equal("fenn", Silhouette.BodyType(_content.Must("fenn")));
        Assert.Equal(Silhouette.BodyType(_content.Must("jester")), Silhouette.PortraitId(_content.Must("jester")));
        Assert.Equal("zig", Silhouette.PortraitId(_content.Must("jester")));
    }

    [Fact]
    public void SignatureBatIdsAndVisualsAreUnique()
    {
        var ids = Silhouette.Captains.Select(GearMesh.SignatureBatId).ToList();
        Assert.Equal(Silhouette.Captains.Length, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        var visuals = ids.Select(id => GearMesh.BatVisual(_content.Bats[id])).ToList();
        Assert.Equal(Silhouette.Captains.Length, visuals.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal("bat-staff", GearMesh.BatVisual(_content.Bats["fen-cane"]));
        Assert.Equal("bat-spark", GearMesh.BatVisual(_content.Bats["harbor-lumber"]));
        Assert.Equal("bat-wand", GearMesh.BatVisual(_content.Bats["pageant-wand"]));
        Assert.Equal("bat-short", GearMesh.BatVisual(_content.Bats["prism-stick"]));
        Assert.Equal("bat-brick", GearMesh.BatVisual(_content.Bats["gold-brick"]));
        Assert.Equal("bat-barrel", GearMesh.BatVisual(_content.Bats["barrel-bat"]));
        Assert.Equal("bat-furnace", GearMesh.BatVisual(_content.Bats["furnace-club"]));
    }

    [Fact]
    public void ExhibitionStartsOnCaptainSignatureBats()
    {
        var match = Match.Exhibition(_content, "vale", "brondo", seed: 7);
        Assert.Equal("pageant-wand", match.HomeBat.Id);
        Assert.Equal("gold-brick", match.AwayBat.Id);
        Assert.Equal("bat-wand", GearMesh.BatVisual(match.HomeBat));
        Assert.Equal("bat-brick", GearMesh.BatVisual(match.AwayBat));
        var first = match.HomeBat.Id;
        match.CycleBat(true);
        Assert.NotEqual(first, match.HomeBat.Id);
        Assert.NotEqual(GearMesh.BatVisual(_content.Bats[first]), GearMesh.BatVisual(match.HomeBat));
    }

    [Fact]
    public void SliceKeepsSparkAndEmberSignatureBats()
    {
        var match = Match.Slice(_content, seed: 1);
        Assert.Equal("harbor-lumber", match.HomeBat.Id);
        Assert.Equal("furnace-club", match.AwayBat.Id);
        Assert.Equal("glove-brown", GearMesh.GloveVisual(match.HomeGlove));
        Assert.Equal("glove-gold", GearMesh.GloveVisual(match.AwayGlove));
    }

    [Fact]
    public void LoadoutVisualFollowsTheSimItem()
    {
        var match = Match.Exhibition(_content, "rio", "ashlord", seed: 1);
        Assert.Equal("bat-spark", GearMesh.BatVisual(match.HomeBat));
        match.CycleBat(true);
        Assert.Equal(match.HomeBat.Visual, GearMesh.BatVisual(match.HomeBat));
        Assert.False(string.IsNullOrWhiteSpace(match.HomeBat.Visual));
        var g = match.HomeGlove.Visual;
        match.CycleGlove(true);
        Assert.NotEqual(g, match.HomeGlove.Visual);
        Assert.Equal(match.HomeGlove.Visual, GearMesh.GloveVisual(match.HomeGlove));
    }
}
