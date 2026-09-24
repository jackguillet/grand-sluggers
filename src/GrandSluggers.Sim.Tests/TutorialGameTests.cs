using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class TutorialGameTests
{
    static readonly ContentCatalog Shipped = global::GrandSluggers.Sim.Tests.Shipped.Content;
    public static IEnumerable<object[]> Cases => from profile in new[] { "shipped" }
        from id in new[] { "T-G04", "T-G04-F", "T-G04-H" }
        select new object[] { profile, id };

    static (ContentCatalog Content, TutorialCatalog Catalog) Load(string profile)
    {
        Assert.Equal("shipped", profile);
        return (Shipped, TutorialCatalog.Load(Shipped));
    }

    [Theory]
    [InlineData("shipped")]
    public void CountBallUsesReachableMoundCameraStickAndRubberWalk(string profile)
    {
        var (content, catalog) = Load(profile);
        var world = AtBatControl.WorldHorizontal(-1, content.Shots.Must(AtBatShots.Mound));
        Assert.Equal(1, world);
        foreach (var rubber in new[] { -1.0, 1.0 })
        {
            var run = new TutorialSession(content, catalog, "T-G04"); run.Begin();
            Assert.True(run.Match.WalkPitcher(rubber));
            Assert.Equal(rubber, run.Match.PitcherOffsetX);
            Assert.True(run.Pitch(new("fastball", 0, false, RubberX: run.Match.PitcherOffsetX)));
            Assert.Equal(PlayKind.TakeBall, run.LastPlay?.Kind);
            Assert.Equal(1, run.Match.Balls);
        }
    }

    static void Drain(TutorialSession run, LivePlayCommandSource source)
    {
        for (var i = 0; i < 2400 && run.Match.LivePlay.Active && run.Phase == TutorialPhase.Attempt; i++)
            run.Tick(1.0 / 60, LivePadInput.Dead, source);
        Assert.False(run.Match.LivePlay.Active);
    }

    static void Succeed(TutorialSession run, LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        if (run.Lesson.Id == "T-G04")
        {
            Assert.True(run.Pitch(new("fastball", 0, false, RubberX: 1), source));
            Assert.Equal((1, 0), (run.Match.Balls, run.Match.Strikes));
            Assert.True(run.Pitch(new("fastball", 0, false), source));
            return;
        }
        if (run.Lesson.Id == "T-G04-H")
        {
            Assert.Equal((2, 0, 2, true), (run.Match.Outs, run.Match.Balls, run.Match.Strikes, run.Match.Top));
            Assert.True(run.Pitch(new("fastball", 0, false), source));
            return;
        }
        var foulCommand = new SwingCommand(true, 0, -4, false);
        Assert.True(run.Swing(foulCommand, source));
        Assert.True(run.LastHit?.Foul);
        Drain(run, source);
        Assert.True(run.Match.Strikes == 1, $"first foul result {run.LastPlay?.Kind}, count {run.Match.Balls}-{run.Match.Strikes}, outs {run.Match.Outs}, feedback {run.Feedback}");
        Assert.Equal(TutorialPhase.Attempt, run.Phase);
        Assert.True(run.Swing(new(true, 0, 0, false), source));
        Assert.False(run.LastHit?.Foul);
        Drain(run, source);
    }

    [Theory, MemberData(nameof(Cases))]
    public void OrderedRealMatchSequenceEarnsThreeDistinctSuccessesAndReplays(string profile, string id)
    {
        var (content, catalog) = Load(profile);
        var run = new TutorialSession(content, catalog, id); run.Begin();
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            Succeed(run);
            Assert.True(run.Feedback?.Success, id + "/" + profile + ": " + run.Feedback);
            Assert.Equal(attempt, run.Successes);
            Assert.Equal(attempt == 3, run.Passed);
            Assert.Equal(run.Feedback, TutorialSession.Replay(content, catalog, run.Recording()).Feedback);
            run.Retry();
        }
    }

    [Theory, MemberData(nameof(Cases))]
    public void WrongOrderCpuAndDemonstrationDoNotEarnGameLesson(string profile, string id)
    {
        var (content, catalog) = Load(profile);
        var run = new TutorialSession(content, catalog, id); run.Begin();
        if (id == "T-G04-F") { run.Swing(new(true, 0, 0, false)); Drain(run, LivePlayCommandSource.Human); }
        else if (id == "T-G04") run.Pitch(new("fastball", 0, false));
        else run.Pitch(new("fastball", 0, false, RubberX: 1));
        Assert.False(run.Feedback?.Success ?? false);
        if (id == "T-G04-H") Assert.Equal("game-half-third-out-missed", run.Feedback?.Code);
        if (id == "T-G04") Assert.Equal("wrong-count", run.Feedback?.Code);
        Assert.Equal(0, run.Successes);
        run = new TutorialSession(content, catalog, id); run.Begin();
        if (id == "T-G04-F") Assert.False(run.Swing(new(true, 0, -4, false), LivePlayCommandSource.Cpu));
        else Assert.False(run.Pitch(new("fastball", 0, false), LivePlayCommandSource.Cpu));
        Assert.Equal(0, run.Successes);
        run = new TutorialSession(content, catalog, id); run.Begin(demonstration: true);
        Succeed(run, LivePlayCommandSource.Cpu);
        Assert.Equal("demonstration", run.Feedback?.Code);
        Assert.Equal(0, run.Successes);
    }
}
