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
        var content = Shipped.Content;
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
            Assert.True(PlayCamera.InFrame(PlayCamera.Project(camera, p, aspect), content.Feel.RaceCamera.Margin - .01));
            Assert.True(PlayCamera.InFrame(PlayCamera.Project(camera, p with { Y = p.Y + content.Feel.RaceCamera.BodyHeightFt }, aspect), content.Feel.RaceCamera.Margin - .01));
        }
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void ThePrePitchInsetKeepsTheRaceVisibleFromInsideTheHomeBoard(int bag)
    {
        var content = Shipped.Content;
        var subjects = new[] { PlayCamera.BagSubject(4), PlayCamera.BagSubject(1), PlayCamera.BagSubject(2), PlayCamera.BagSubject(bag) };
        {
            var frame = PlayCamera.RaceFraming(content.Shots, subjects, 16.0 / 9, content.Feel.RaceCamera);
            // Harbor home score face is at Z=-37.3 (front at -36.94). Even a return
            // throw must keep the eye on the field side, rather than framing from behind it.
            Assert.True(frame.Pos.Z > -36.94);
            Assert.InRange(frame.Pos.Y, 13, 17);
            Assert.Equal(content.Shots.Must(PlayCamera.RaceInsetShot).Pos.Z, frame.Pos.Z);
            Assert.Equal(0, frame.Pos.X);
            Assert.Equal(0, frame.Look.X);
            Assert.True(frame.Pos.Z < frame.Look.Z);
            var pitch = Math.Atan2(frame.Pos.Y - frame.Look.Y, frame.Look.Z - frame.Pos.Z) * 180 / Math.PI;
            Assert.InRange(pitch, 16, 20); // below the 45° fly view, above catcher eye level
            var camera = new CameraShot(frame.Shot, "race", frame.Pos, frame.Look, frame.Fov, frame.Blend);
            var home = PlayCamera.Project(camera, PlayCamera.BagSubject(4), 16.0 / 9);
            var center = PlayCamera.Project(camera, new Vec3(0, 0, 300), 16.0 / 9);
            Assert.NotNull(home); Assert.NotNull(center);
            Assert.Equal(home.Value.X, center.Value.X, 9);
            foreach (var p in subjects)
                Assert.True(PlayCamera.InFrame(PlayCamera.Project(camera, p with { Y = 12 }, 16.0 / 9), content.Feel.RaceCamera.Margin - .01));
        }
    }

    [Fact]
    public void BalkIsAVisibleTypedDeadBallCall()
    {
        Assert.True(PlayStamp.ShowsAtTime(PlayKind.Balk));
        Assert.Equal("BALK", PlayStamp.Label(PlayKind.Balk, 0, 0));
    }
}
