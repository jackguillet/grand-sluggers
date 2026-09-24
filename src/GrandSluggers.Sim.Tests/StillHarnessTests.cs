using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class StillHarnessTests
{
    readonly ContentCatalog _content = Shipped.Content;

    [Fact]
    public void SkipToHomeHalfMakesHomeBatWithoutPlayingTheTop()
    {
        var match = Match.Exhibition(_content, "rio", "ashlord", seed: 7);
        Assert.True(match.Top);
        var awayBatter = match.Batter.Id;
        match.SkipToHomeCaptainAtBat();
        Assert.False(match.Top);
        Assert.Equal(0, match.Outs);
        Assert.Equal(0, match.Balls);
        Assert.Equal(0, match.Strikes);
        Assert.False(match.Over);
        Assert.NotEqual(awayBatter, match.Batter.Id);
        Assert.Equal("rio", match.Home.Captain.Id);
        Assert.Equal("rio", match.Batter.Id);
    }

    [Fact]
    public void GiveOffenseStarsUnlocksStarSwingOnTheHomeHalf()
    {
        var match = Match.Exhibition(_content, "rio", "ashlord", seed: 7);
        match.SkipToHomeCaptainAtBat();
        match.GiveOffenseStars(5);
        Assert.True(match.CanStarSwing);
        Assert.False(string.IsNullOrWhiteSpace(match.Batter.StarSwing));
        var hopper = new AtBatResult(ContactQuality.Nice, true, false, 90, 8, 40, false, false, null, null, SprayDeg: 4, Class: BattedBallClass.Grounder);
        Assert.Equal(PlayCamera.InPlay, InPlay.TheaterShot(hopper));
        var star = hopper with { StarSwingUsed = match.Batter.StarSwing, LaunchDeg = 28 };
        Assert.Equal(PlayCamera.InPlay, InPlay.TheaterShot(star));
    }

    [Fact]
    public void ScoopPoseIsTheFirstBaseHoleNotTheMound()
    {
        Assert.True(StillPose.PlateCatcherClearsTheLens(
            StillPose.PlateCamX, StillPose.PlateCamZ, StillPose.PlateLookX, StillPose.PlateLookZ));
        Assert.True(StillPose.ScoopIsNotTheMound(StillPose.ScoopX, StillPose.ScoopZ));
        Assert.True(StillPose.ScoopZ < Diamond.Mound - 16);
        Assert.True(StillPose.ScoopX > 12);
        Assert.InRange(StillPose.ScoopPoseT, 0.18, 0.26);
        Assert.Equal("2B", StillPose.ScoopGlove);
        Assert.True(StillPose.CameraClearsTheDugout(StillPose.CamX, StillPose.CamZ));
        Assert.True(StillPose.CameraIsSideThreeQuarter(
            StillPose.CamX, StillPose.CamZ, StillPose.ScoopX, StillPose.ScoopZ),
            "12:39 PNG looked down the path so gloves read as a T");
        Assert.True(Math.Abs(StillPose.ScoopX - StillPose.ScoopZ) < 4, "scoop sits on the first-base dirt path");
        Assert.True(StillPose.ScoopBallY < 0.5, "look at the leather on the dirt");
        Assert.True(StillPose.RunnerX > StillPose.ScoopX);
        Assert.True(StillPose.RunnerLeavesInFrame(
            StillPose.CamX, StillPose.CamZ, StillPose.ScoopX, StillPose.ScoopZ,
            StillPose.RunnerX, StillPose.RunnerZ),
            "14:16 PNG put the runner behind the camera");
        var rel = PitchFlight.Release(rules: Rules.Default);
        Assert.True(StillPose.PitchReleaseIsOnTheMound(rel.Z), $"release z={rel.Z}");
        var ball = PitchFlight.Point(PitchFamily.Fastball, StillPose.PitchBallU, Rules.Default, 0, 0, 0, 0, rel);
        Assert.True(StillPose.PitchBallIsOffTheHand(ball.Z),
            $"pitch still was a beach ball in the lens z={ball.Z}");
        Assert.False(StillPose.PitchReleaseIsOnTheMound(2), "home-plate from is not the hand");
        var defense = FieldingResolver.Assign(
            Match.Exhibition(_content, "rio", "ashlord", seed: 7).Away.Roster,
            _content.Must("ashlord"));
        Assert.True(defense.ContainsKey(StillPose.ScoopGlove));
        Assert.NotEqual("ashlord", defense[StillPose.ScoopGlove].Id);
    }

    [Fact]
    public void CharacterTurntableLooksAtTheChestNotTheBrim()
    {
        Assert.Equal(Motion.SwingContact, StillPose.CharPoseT);
        Assert.Equal(StillPose.CharUnscaledHeadCenterY, SwingPresentation.HeadCenterAtRest.Y, 2);
        Assert.Equal(StillPose.CharUnscaledHeadRadius, SwingPresentation.HeadRadius, 2);
        foreach (var id in Shipped.CaptainIds)
        {
            var shot = StillPose.CharFraming(Shipped.Content, id);
            Assert.True(StillPose.CharCameraLooksAtChest(shot.Target.Y, StillPose.CharChestY(Shipped.Content, id)),
                id + " look is not the chest from Silhouette.Proportions");
            Assert.True(StillPose.CharCameraIsNotBrim(shot.Target.Y, shot.Pos.Y),
                id + " look is the brim");
            Assert.True(shot.Target.Y < StillPose.CharHeadTopY(Shipped.Content, id) - 0.5,
                id + " look sits on the head, not the chest");
            Assert.True(StillPose.CharCameraIsThreeQuarter(shot.Pos.X, shot.Pos.Z, StillPose.CharZ),
                id + " 3/4 on the face — dead-on behind the shell is illegible");
        }
    }

    [Fact]
    public void CharacterTurntableKeepsEveryCaptainsHeadInFrame()
    {
        Assert.True(StillPose.CharHeadTopY(Shipped.Content, "ashlord") > StillPose.CharHeadTopY(Shipped.Content, "konga"));
        Assert.True(StillPose.CharHeadTopY(Shipped.Content, "konga") > StillPose.CharHeadTopY(Shipped.Content, "vale"));
        Assert.True(StillPose.CharHeadTopY(Shipped.Content, "vale") > StillPose.CharHeadTopY(Shipped.Content, "rio"));
        Assert.True(StillPose.CharHeadTopY(Shipped.Content, "rio") > StillPose.CharHeadTopY(Shipped.Content, "fenn"));
        Assert.True(StillPose.CharHeadTopY(Shipped.Content, "fenn") > StillPose.CharHeadTopY(Shipped.Content, "zig"));

        var rio = StillPose.CharFraming(Shipped.Content, "rio");
        var ashlord = StillPose.CharFraming(Shipped.Content, "ashlord");
        var rioPull = Dist(rio.Pos, new Vec3(StillPose.CharX, rio.Target.Y, StillPose.CharZ));
        var ashPull = Dist(ashlord.Pos, new Vec3(StillPose.CharX, ashlord.Target.Y, StillPose.CharZ));
        Assert.True(ashPull > rioPull + 2,
            $"Ashlord must pull back by extra height rio={rioPull:0.00} ash={ashPull:0.00}");

        var cropped = new CameraShot(
            "select",
            "chest",
            new Vec3(StillPose.CharCamX, 3.4, StillPose.CharCamZ),
            new Vec3(StillPose.CharX, 2.5, StillPose.CharZ),
            StillPose.CharFov,
            0);
        var croppedHead = PlayCamera.Project(
            cropped,
            new Vec3(StillPose.CharX, StillPose.CharHeadTopY(Shipped.Content, "ashlord"), StillPose.CharZ));
        Assert.False(PlayCamera.InFrame(croppedHead),
            "the old Rio-sized Y must fail Ashlord — otherwise the test is not a falsifier");

        foreach (var id in Shipped.CaptainIds)
        {
            var shot = StillPose.CharFraming(Shipped.Content, id);
            var feet = PlayCamera.Project(shot, new Vec3(StillPose.CharX, 0.05, StillPose.CharZ));
            var chest = PlayCamera.Project(shot, new Vec3(StillPose.CharX, StillPose.CharChestY(Shipped.Content, id), StillPose.CharZ));
            var head = PlayCamera.Project(shot, new Vec3(StillPose.CharX, StillPose.CharHeadTopY(Shipped.Content, id), StillPose.CharZ));
            Assert.True(PlayCamera.InFrame(feet), $"{id} feet off turntable {feet}");
            Assert.True(PlayCamera.InFrame(chest), $"{id} chest off turntable {chest}");
            Assert.True(StillPose.CharHeadTopInFrame(Shipped.Content, id), $"{id} head top off turntable {head}");
            Assert.True(head!.Value.Y - feet!.Value.Y > 0.28,
                $"{id} toy too small in the turntable h={head.Value.Y - feet.Value.Y:0.000}");
        }
    }

    static double Dist(Vec3 a, Vec3 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
