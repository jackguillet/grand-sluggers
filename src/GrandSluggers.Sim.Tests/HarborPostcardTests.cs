using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class HarborPostcardTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void FieldPickAndLiveSetShareOneHarborDress()
    {
        Assert.True(HarborPostcard.Owns("harbor-diamond"));
        Assert.False(HarborPostcard.Owns("crystal-rink"));
        Assert.True(_content.Art.TryPark(HarborPostcard.ParkId, out var kit) && kit.Placed);
        var field = _content.Shots.Must("field");
        var harbor = _content.Parks[HarborPostcard.ParkId];
        Assert.True(HarborPostcard.ReadsFromField(field, harbor.CenterFenceFt),
            $"field z={field.Pos.Z}->{field.Target.Z} fence={harbor.CenterFenceFt} wall={HarborPostcard.SubtendDeg(field.Pos.Z, harbor.CenterFenceFt, HarborPostcard.WallHeightFt):0.0}deg");
        Assert.True(field.Target.Z > harbor.CenterFenceFt - 50,
            $"field look {field.Target.Z} should sit on the CF wall/town");
        Assert.True(HarborPostcard.CrowdInsideFt < 40);
        Assert.True(HarborPostcard.TownPastFenceFt > 20);
    }

    [Fact]
    public void ScoreboardDigitsChangeWithTheRun()
    {
        Assert.True(HarborPostcard.SegOn(0, 1));
        Assert.False(HarborPostcard.SegOn(0, 64), "0 does not light G");
        Assert.True(HarborPostcard.SegOn(1, 2) && HarborPostcard.SegOn(1, 4));
        Assert.False(HarborPostcard.SegOn(1, 1), "1 is not 0");
        Assert.NotEqual(HarborPostcard.DigitMask[0], HarborPostcard.DigitMask[1]);
        Assert.NotEqual(HarborPostcard.DigitMask[1], HarborPostcard.DigitMask[2]);
        for (var n = 0; n <= 9; n++)
            Assert.True(HarborPostcard.DigitMask[n] != 0, "digit " + n);
    }

    [Fact]
    public void HarborKitFbxIsInThePlayerResourcesSlot()
    {
        var repo = Directory.GetParent(_content.Root)?.FullName
            ?? throw new InvalidOperationException("no repo root");
        var drop = Path.GetFullPath(Path.Combine(repo, "unity",
            "Assets/Art/Parks/harbor-diamond/harbor-kit.fbx".Replace('/', Path.DirectorySeparatorChar)));
        var player = Path.GetFullPath(Path.Combine(repo, "unity",
            "Assets/Resources/Art/Parks/harbor-diamond/harbor-kit.fbx".Replace('/', Path.DirectorySeparatorChar)));
        Assert.True(File.Exists(drop), drop);
        Assert.True(new FileInfo(drop).Length > 10_000, "harbor-kit.fbx is empty");
        Assert.True(File.Exists(player), player + " — kit must bind in the Linux player, not only Editor Play");
        Assert.True(new FileInfo(player).Length > 10_000, "player harbor-kit.fbx is empty");
    }

    [Fact]
    public void HowToPlayNamesTheHarborPostcard()
    {
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("postcard"));
        Assert.True(HowToPlay.Mentions("padded wall") || HowToPlay.Mentions("crowd"));
    }
}
