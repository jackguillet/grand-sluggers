using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class StealPresentationTests
{
    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void ATargetedPressIsAThrowBeforeChargeAndANewTargetDuringChargeIsAStepOff(int bag)
    {
        Assert.True(StealPresentation.BaseThrow(bag, bag, true, true, false));
        Assert.False(StealPresentation.BaseThrow(bag, 0, false, true, false));
        Assert.True(StealPresentation.BaseThrow(bag, 0, false, true, true));
        Assert.False(StealPresentation.BaseThrow(bag, bag, false, true, true));
        Assert.False(StealPresentation.BaseThrow(0, bag, true, true, true));
    }

    [Theory]
    [InlineData(.30)] [InlineData(.22)]
    public void TheAuthoredReleaseAndFollowThroughTrackThePhysicalPreparation(double preparation)
    {
        Assert.Equal(0, StealPresentation.ThrowSample(0, preparation));
        Assert.Equal(Motion.ThrowRelease, StealPresentation.ThrowSample(preparation, preparation), 9);
        Assert.True(StealPresentation.ThrowSample(preparation / 2, preparation) < Motion.ThrowRelease);
        Assert.Equal(Motion.ThrowRelease + .1, StealPresentation.ThrowSample(preparation + .1, preparation), 9);
    }

    [Theory]
    [InlineData(4.0 / 3)] [InlineData(16.0 / 10)] [InlineData(16.0 / 9)] [InlineData(21.0 / 9)]
    public void DoubleStealReturnAndRetargetKeepBodiesAndBothBagsInsideTheFrame(double aspect)
    {
        var content = ContentCatalog.Load();
        var match = Match.Slice(content, seed: 7);
        match.StationRunner(1, match.AwayOrder[1]);
        match.StationRunner(3, match.AwayOrder[2]);
        match.StartStealAt(1); match.StartStealAt(3);
        match.PitchSetup.Advance(.5);
        match.ReturnToBagAt(1);
        Check(match, content, aspect);
        match.BeginPickoff(2, new LiveSeats(true, true, true, true), out _);
        for (var i = 0; i < 30; i++)
        {
            Check(match, content, aspect);
            match.LivePlay.Apply(LivePlayCommand.Tick(1.0 / 60));
        }
    }

    static void Check(Match match, ContentCatalog content, double aspect)
    {
        var subjects = PlayCamera.RaceSubjects(match);
        Assert.NotEmpty(subjects);
        var frame = PlayCamera.RaceFraming(content.Shots, subjects, aspect, content.Feel.RaceCamera);
        var camera = new CameraShot(frame.Shot, "race", frame.Pos, frame.Look, frame.Fov, frame.Blend);
        foreach (var p in subjects)
        {
            Assert.True(PlayCamera.InFrame(PlayCamera.Project(camera, p, aspect), .09));
            Assert.True(PlayCamera.InFrame(PlayCamera.Project(camera, p with { Y = p.Y + content.Feel.RaceCamera.BodyHeightFt }, aspect), .09));
        }
    }

    [Fact]
    public void BalkIsAVisibleTypedDeadBallCall()
    {
        Assert.True(PlayStamp.ShowsAtTime(PlayKind.Balk));
        Assert.Equal("BALK", PlayStamp.Label(PlayKind.Balk, 0, 0));
    }
}
