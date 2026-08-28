using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class StillHarnessTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

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
        var hopper = new AtBatResult(ContactQuality.Solid, true, false, 90, 8, 40, false, false, null, null, SprayDeg: 4);
        Assert.Equal("diamond-grounder", InPlay.TheaterShot(hopper));
        var star = hopper with { StarSwingUsed = match.Batter.StarSwing, LaunchDeg = 28 };
        Assert.Equal("smash", InPlay.TheaterShot(star));
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
        var defense = FieldingResolver.Assign(
            Match.Exhibition(_content, "rio", "ashlord", seed: 7).Away.Roster,
            _content.Must("ashlord"));
        Assert.True(defense.ContainsKey(StillPose.ScoopGlove));
        Assert.NotEqual("ashlord", defense[StillPose.ScoopGlove].Id);
        Assert.NotEqual("rio", defense[StillPose.ScoopGlove].Id);
    }

    [Fact]
    public void PlateStillIsHomeRioNotTheVisitorPitcher()
    {
        var req = StillRequest.Parse("""{"shots":["plate"],"home":"rio","away":"ashlord"}""");
        Assert.Equal("rio", req.ResolvedHome());
        Assert.Equal("ashlord", req.ResolvedAway());
        var match = Match.Exhibition(_content, req.ResolvedHome(), req.ResolvedAway(), seed: 7);
        match.SkipToHomeCaptainAtBat();
        Assert.Equal("rio", match.Batter.Id);
        Assert.Equal("ashlord", match.Pitcher.Id);
        Assert.False(match.Top);
        Assert.True(StillPose.PlateIsThirdBaseThreeQuarter(StillPose.PlateCamX, StillPose.PlateCamZ));
        Assert.True(StillPose.PlateCatcherClearsTheLens(
            StillPose.PlateCamX, StillPose.PlateCamZ, StillPose.PlateLookX, StillPose.PlateLookZ));
    }

    [Fact]
    public void SmashShotLooksAtTheTorsoNotTheDirt()
    {
        var smash = _content.Shots.Must("smash");
        Assert.True(smash.Target.Y >= 0.4, $"smash look is dirt y={smash.Target.Y}");
        Assert.True(smash.Pos.Y >= 1.4, $"smash cam is a nostril y={smash.Pos.Y}");
        Assert.True(smash.Pos.X > 5, $"smash is a 3/4 x={smash.Pos.X}");
        Assert.True(smash.Fov >= 40, $"smash fov {smash.Fov}");
        Assert.Equal("smash", InPlay.TheaterShot(
            new AtBatResult(ContactQuality.Perfect, true, false, 100, 28, 320, false, false, null, "heat-swing")));
    }
}
