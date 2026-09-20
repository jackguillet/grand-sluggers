using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

[Trait("Rows", "compact")]
public sealed class TutorialGameTests
{
    static readonly ContentCatalog Shipped = ContentCatalog.Load();
    public static IEnumerable<object[]> Cases => from profile in new[] { "shipped", "c80" }
        from id in new[] { "T-G04", "T-G04-F", "T-G04-H" }
        select new object[] { profile, id };

    static (ContentCatalog Content, TutorialCatalog Catalog) Load(string profile)
    {
        var content = profile == "shipped" ? Shipped : ContentCatalog.Load(new DataRoot(Shipped.Root.Shipped,
            Path.GetFullPath(Path.Combine(Shipped.Root.Shipped, "..", "trials", "c80"))));
        return (content, TutorialCatalog.Load(content));
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
        var foulCommand = new SwingCommand(true, 0, -4, false, SprayAimDeg: AtBatResolver.SprayAimDeg(-1));
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
        else run.Pitch(new("fastball", 0, false, AimX: 4));
        Assert.False(run.Feedback?.Success ?? false);
        Assert.Equal(0, run.Successes);
        run = new TutorialSession(content, catalog, id); run.Begin();
        if (id == "T-G04-F") Assert.False(run.Swing(new(true, 0, -4, false, SprayAimDeg: AtBatResolver.SprayAimDeg(-1)), LivePlayCommandSource.Cpu));
        else Assert.False(run.Pitch(new("fastball", 0, false), LivePlayCommandSource.Cpu));
        Assert.Equal(0, run.Successes);
        run = new TutorialSession(content, catalog, id); run.Begin(demonstration: true);
        Succeed(run, LivePlayCommandSource.Cpu);
        Assert.Equal("demonstration", run.Feedback?.Code);
        Assert.Equal(0, run.Successes);
    }
}
