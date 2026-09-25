using System.Linq;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
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

    /// <summary>The ball leaves the hand when the sim says, for the ordinary throw and the catcher's take alike (#966).</summary>
    [Theory]
    [InlineData(.30, Motion.ThrowRelease)] [InlineData(.22, Motion.ThrowRelease)]
    [InlineData(.30, Motion.CatcherThrowRelease)] [InlineData(.22, Motion.CatcherThrowRelease)]
    public void TheAuthoredReleaseAndFollowThroughTrackThePhysicalPreparation(double preparation, double releaseAt)
    {
        Assert.Equal(0, StealPresentation.ThrowSample(0, preparation, releaseAt));
        Assert.Equal(releaseAt, StealPresentation.ThrowSample(preparation, preparation, releaseAt), 9);
        Assert.True(StealPresentation.ThrowSample(preparation / 2, preparation, releaseAt) < releaseAt);
        Assert.Equal(releaseAt + .1, StealPresentation.ThrowSample(preparation + .1, preparation, releaseAt), 9);
    }

    /// <summary>The catcher's own take is the runner play's; a catcher's throw on a batted ball, and every other thrower's, is the ordinary one.</summary>
    [Fact]
    public void OnlyTheCatcherOnTheRunnerPlayThrowsFromTheCatchersTake()
    {
        Assert.Equal(Motion.Verb.CatcherThrow, StealPresentation.ThrowVerb("C", runnerPlay: true));
        Assert.Equal(Motion.Verb.Throw, StealPresentation.ThrowVerb("C", runnerPlay: false));
        Assert.Equal(Motion.Verb.Throw, StealPresentation.ThrowVerb("SS", runnerPlay: true));
        Assert.Equal(Motion.Verb.Throw, StealPresentation.ThrowVerb("P", runnerPlay: true));
    }

    /// <summary>
    /// The sweep tag (#966) is a picture of the race the sim already runs: a ball on the bag the runner is bound for, while the
    /// runner is close. A glove off the bag, a runner still far, or a runner already on the bag shows no sweep.
    /// </summary>
    [Fact]
    public void TheSweepTagShowsWhenTheBallWaitsOnTheBagTheRunnerIsBoundFor()
    {
        var content = Shipped.Content;
        var feel = content.Feel;
        var match = Match.Slice(content, seed: 7);
        var diamond = DiamondGeometry.Of(match.Rules);
        match.StationRunner(1, match.AwayOrder[1]);
        match.StartStealAt(1);
        var runner = match.Runners.Single(r => !r.IsBatter);
        var second = diamond.Bag(2);
        match.PitchSetup.Advance(.1);
        Assert.Equal(2, StealPresentation.BoundFor(runner));
        Assert.False(StealPresentation.Tagging(second.X, second.Z, match.Runners, diamond, feel.TagStandFt, feel.TagWindowFt));
        var seen = false;
        for (var i = 0; i < 600 && !seen; i++)
        {
            match.PitchSetup.Advance(1.0 / 60);
            var (x, z) = runner.Position;
            var close = Diamond.Dist(x, z, second.X, second.Z) <= feel.TagWindowFt;
            Assert.Equal(close && !runner.IsOn(2),
                StealPresentation.Tagging(second.X, second.Z, match.Runners, diamond, feel.TagStandFt, feel.TagWindowFt));
            Assert.False(StealPresentation.Tagging(second.X + feel.TagStandFt + 1, second.Z, match.Runners, diamond,
                feel.TagStandFt, feel.TagWindowFt));
            seen = close;
        }
        Assert.True(seen, "the stealing runner never came within the tag window of second");
    }

    /// <summary>A runner that reverses plants and turns for the take's length, then runs back; a runner that never advanced does not.</summary>
    [Fact]
    public void AReversingRunnerTurnsBackThenRuns()
    {
        var match = Match.Slice(Shipped.Content, seed: 7);
        match.StationRunner(1, match.AwayOrder[1]);
        match.StartStealAt(1);
        match.PitchSetup.Advance(.5);
        var runner = match.Runners.Single(r => !r.IsBatter);
        var before = runner.Phase;
        Assert.True(match.ReturnToBagAt(1));
        match.PitchSetup.Advance(1.0 / 60);
        Assert.Equal(RunnerPhase.Returning, runner.Phase);
        Assert.True(StealPresentation.TurnedBack(before, runner.Phase));
        Assert.False(StealPresentation.TurnedBack(RunnerPhase.OnBag, RunnerPhase.Returning));
        Assert.False(StealPresentation.TurnedBack(RunnerPhase.Returning, RunnerPhase.Returning));
        Assert.Equal(Motion.Verb.TurnBack, StealPresentation.RunnerVerb(runner, 0));
        Assert.Equal(Motion.Verb.TurnBack, StealPresentation.RunnerVerb(runner, Motion.TurnBackDur - .01));
        Assert.Equal(Motion.Verb.Run, StealPresentation.RunnerVerb(runner, Motion.TurnBackDur));
        Assert.Equal(Motion.Verb.Run, StealPresentation.RunnerVerb(runner, -1));
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
        var subjects = new[] { PlayCamera.BagSubject(4, DiamondGeometry.Of(Rules.Default)), PlayCamera.BagSubject(1, DiamondGeometry.Of(Rules.Default)), PlayCamera.BagSubject(2, DiamondGeometry.Of(Rules.Default)), PlayCamera.BagSubject(bag, DiamondGeometry.Of(Rules.Default)) };
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
            var home = PlayCamera.Project(camera, PlayCamera.BagSubject(4, DiamondGeometry.Of(Rules.Default)), 16.0 / 9);
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
