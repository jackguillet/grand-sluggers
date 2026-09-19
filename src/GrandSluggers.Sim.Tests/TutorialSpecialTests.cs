using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

[Trait("Rows", "compact")]
public sealed class TutorialSpecialTests
{
    static readonly ContentCatalog Control = ContentCatalog.Load();
    static readonly string[] StarLessons = ["T-P09", "T-B09",
        "T-SP-heatball", "T-SP-charmball", "T-SP-prismball", "T-SP-phonyball", "T-SP-caskball",
        "T-SP-skullball", "T-SP-fogball", "T-SP-fastball", "T-SP-changeup", "T-SP-breaker",
        "T-SS-heat-swing", "T-SS-heart-swing", "T-SS-shell-swing", "T-SS-phony-swing",
        "T-SS-cask-swing", "T-SS-furnace", "T-SS-staff-swing", "T-SS-ground", "T-SS-fly", "T-SS-line"];
    public static IEnumerable<object[]> Cases => from profile in new[] { "shipped", "c80" }
        from lesson in StarLessons select new object[] { profile, lesson };

    static (ContentCatalog Content, TutorialCatalog Catalog) Load(string profile)
    {
        var root = new DataRoot(Control.Root.Shipped, profile == "c80"
            ? Path.GetFullPath(Path.Combine(Control.Root.Shipped, "..", "trials", "c80")) : null);
        var content = ContentCatalog.Load(root);
        return (content, TutorialCatalog.Load(content));
    }

    static bool Perform(TutorialSession run, bool star = true, LivePlayCommandSource source = LivePlayCommandSource.Human)
        => run.Lesson.Objective == "star-pitch"
            ? run.Pitch(new("fastball", 0, star), source)
            : run.Swing(new(true, 0, 0, star), source);

    [Theory, MemberData(nameof(Cases))]
    public void NamedStarAndActualMeterSpendNeedThreeAttemptsAndReplay(string profile, string id)
    {
        var (content, catalog) = Load(profile);
        var run = new TutorialSession(content, catalog, id); run.Begin();
        for (var n = 1; n <= 3; n++)
        {
            Assert.True(Perform(run));
            Assert.True(run.Feedback!.Success, id + "/" + profile + ": " + run.Feedback.Detail + " hit=" + run.LastHit);
            Assert.Equal(n, run.Successes); Assert.Equal(n == 3, run.Passed);
            var replay = TutorialSession.Replay(content, catalog, run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            Assert.False(Perform(run));
            run.Retry();
        }
    }

    [Theory, MemberData(nameof(Cases))]
    public void OrdinaryCpuAndDemoActionsCannotCreditStar(string profile, string id)
    {
        var (content, catalog) = Load(profile);
        var run = new TutorialSession(content, catalog, id); run.Begin();
        Assert.False(Perform(run, source: LivePlayCommandSource.Cpu));
        Assert.Equal(0, run.Successes);
        Assert.True(Perform(run, star: false));
        Assert.False(run.Feedback!.Success);
        run = new TutorialSession(content, catalog, id); run.Begin(demonstration: true);
        Assert.True(Perform(run, source: LivePlayCommandSource.Cpu));
        Assert.Equal("demonstration", run.Feedback!.Code);
        Assert.Equal(0, run.Successes);
    }
}
