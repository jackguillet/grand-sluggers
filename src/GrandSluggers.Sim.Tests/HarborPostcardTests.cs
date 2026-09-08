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
        Assert.False(HarborPostcard.CenterFieldHasBleachers);
        Assert.False(HarborWall.HasNet, "chain-link net is gone; the padded wall wraps home");
        Assert.True(HarborWall.WrapsTheDiamond(harbor));
        Assert.True(HarborWall.OutfieldIsTallerThanTheHip());
        Assert.True(HarborWall.TaperIsARamp(harbor), "taper is a ramp, not stairs");
        Assert.False(HarborStands.HasRoofs, "white roof slabs are not the postcard");
        Assert.True(HarborStands.CrowdIsPeople(),
            $"crowd {HarborStands.PersonFt}ft must be people, not 12-ft giants");
        Assert.True(HarborStands.CenterFieldIsOpen());
        Assert.True(HarborStands.WingsClearTheDugout());
        Assert.True(HarborStands.WingsSitInFoul());
        Assert.True(HarborStands.CornersSitBehindTheWall());
        Assert.True(HarborStands.BowlReadsFromField(field, harbor),
            "LF/RF bowl must still read from the field postcard with small people");
        Assert.Equal(HarborStands.PersonFt, HarborPostcard.CrowdPersonFt);
        Assert.True(HarborPostcard.WallSegs >= 36);
        Assert.True(HarborPostcard.WallOverlapFt >= 0.6f);
        Assert.True(HarborPostcard.WallPiecesConnect(harbor),
            "wall pieces must overlap along the ground loop, not sit as gapped slabs");
        Assert.True(HarborWall.WrapStaysInFoul(harbor),
            "wrap must follow foul territory, not cut the infield");
        Assert.True(HarborWall.LoopIsSymmetric(harbor),
            "1B and 3B walls must match — the home wrap is mirrored, not two different polylines");
        Assert.True(HarborWall.HomeWrapIsRound(harbor), "behind home is an arc, not a V");
        var cf = HarborPostcard.WallPoint(harbor, 0);
        var cfDist = Math.Sqrt(cf.X * cf.X + cf.Z * cf.Z);
        Assert.InRange(cfDist, harbor.CenterFenceFt - 4, harbor.CenterFenceFt + 4);
        var lf = HarborPostcard.WallPoint(harbor, -AtBatResolver.FoulLineDeg);
        Assert.InRange(Math.Sqrt(lf.X * lf.X + lf.Z * lf.Z), harbor.LeftFenceFt - 1, harbor.LeftFenceFt + 1);
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
    public void DugoutsAreSunkenAndSetBackOffTheDirt()
    {
        Assert.True(HarborDugout.IsSetBackFromTheDirt(),
            $"field lip {HarborDugout.FieldX(HarborDugout.X)} still on the path");
        Assert.True(HarborDugout.IsSunken());
        Assert.True(HarborDugout.HasStairs());
        Assert.True(HarborDugout.HasMeshFront(), "dugout is a padded rail + mesh pit, not a shed");
        Assert.True(HarborDugout.X < 96, "dugout must sit in front of the side bleachers (~102)");
        Assert.True(HarborDugout.X - HarborDugout.HalfDeep > 42, "old pavilion was at 42");
        Assert.True(HarborDugout.StartsAfterHome(), "starts just after home, not on the plate");
        Assert.True(HarborDugout.EndsBeforeTheBag(), "ends before 1B/3B, not on the bag");
        Assert.True(HarborDugout.HalfAlong >= 16f && HarborDugout.HalfAlong <= 24f,
            "dugout is two-thirds the old home-to-bag shed");
        Assert.True(HarborDugout.FieldStairRun < 3f, "stairs stay in the pit, not a runway on the grass");
        Assert.True(HarborDugout.RailFacesTheDiamond(), "local −X points at the diamond, not the stands");
        Assert.True(HarborDugout.YawFollowsTheFoulLine(), "dugout +Z is home→bag; a 180° yaw is the Play gap");
        Assert.True(HarborDugout.RailIsTheHipWall(), "front rail is the short wall, pit behind it");
        var harborPark = _content.Parks[HarborPostcard.ParkId];
        Assert.True(HarborDugout.WallMeetsTheRail(harborPark),
            "wall loop must pin a vertex on each dugout rail end so DressWall cannot skip a 16-ft gap");
        Assert.False(HarborDugout.KitSpansTheOpening(HarborDugout.HalfAlong),
            "a kit shorter than the opening must not dress the hole");
        Assert.True(HarborDugout.KitSpansTheOpening(HarborDugout.HalfAlong * 2f));
        Assert.True(HarborDugout.CameraClears(StillPose.CamX, StillPose.CamZ));
        Assert.True(HarborDugout.CameraClears(StillPose.PlateCamX, StillPose.PlateCamZ));
        Assert.False(HarborDugout.CameraClears(HarborDugout.X, HarborDugout.Z),
            "a camera in the pit is not clear");
        Assert.InRange(HarborDugout.StarZ0, HarborDugout.Z - HarborDugout.HalfAlong,
            HarborDugout.Z + HarborDugout.HalfAlong);
        Assert.True(HarborDugout.InPitHole(HarborDugout.X, HarborDugout.Z));
        Assert.True(HarborDugout.InPitHole(-HarborDugout.X, HarborDugout.Z));
        Assert.False(HarborDugout.LawnCovers(HarborDugout.X, HarborDugout.Z),
            "lawn must not cap the pit");
        Assert.True(HarborDugout.LawnCovers(0, HarborDugout.Z), "grass between the dugouts");
        Assert.True(HarborDugout.LawnCovers(StillPose.CamX, StillPose.CamZ));
        Assert.True(HarborDugout.LawnCovers(StillPose.ScoopX, StillPose.ScoopZ));
        Assert.True(HarborDugout.IsSetBackFromTheDirt());
        Assert.False(HarborDugout.InPitHole(Diamond.First.X, Diamond.First.Z), "pit does not cover 1B");
    }

    [Fact]
    public void InfieldIsPathsAndBagsNotADirtLake()
    {
        Assert.True(HarborInfield.PathIsNotALake());
        Assert.True(HarborInfield.BagIsABag());
        Assert.True(HarborInfield.HomePackedIsAPad());
        Assert.True(HarborInfield.LawnRespectsPits());
        Assert.True(HarborInfield.PathIsNotALake());
        Assert.True(HarborInfield.BagIsABag());
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
        var ascii = System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(drop));
        Assert.Contains("home-plate", ascii);
        Assert.Contains("bag", ascii);
        Assert.Contains("mound", ascii);
        Assert.Contains("foul-pole", ascii);
        Assert.Contains("warning-track", ascii);
        Assert.Contains("infield-dirt", ascii);
        Assert.Equal(new FileInfo(drop).Length, new FileInfo(player).Length);
        var dropTxt = File.ReadAllText(Path.Combine(Path.GetDirectoryName(drop)!, "DROP.txt"));
        Assert.Contains("home-plate", dropTxt);
        Assert.Contains("bag", dropTxt);
        Assert.Contains("mound", dropTxt);
        Assert.Contains("foul-pole", dropTxt);
        Assert.Contains("warning-track", dropTxt);
        Assert.Contains("infield-dirt", dropTxt);
    }

    [Fact]
    public void HowToPlayNamesTheHarborPostcard()
    {
        Assert.Contains(HowToPlay.Must("exhibition").Lines, l => l.Contains("postcard"));
        Assert.True(HowToPlay.Mentions("padded wall") || HowToPlay.Mentions("crowd"));
    }
}
