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
    static readonly string[] ItemLessons = ["T-X01", "T-I-banana", "T-I-rocket", "T-I-pow"];
    public static IEnumerable<object[]> Cases => from profile in new[] { "shipped", "c80" }
        from lesson in StarLessons select new object[] { profile, lesson };
    public static IEnumerable<object[]> ItemCases => from profile in new[] { "shipped", "c80" }
        from lesson in ItemLessons select new object[] { profile, lesson };

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

    [Theory]
    [InlineData("shipped")][InlineData("c80")]
    public void ResourceLessonRequiresEarnThenSpendAcrossThreeFreshAtBats(string profile)
    {
        var (content, catalog) = Load(profile);
        var run = new TutorialSession(content, catalog, "T-G03"); run.Begin();
        for (var n = 1; n <= 3; n++)
        {
            Assert.Equal(2, run.Match.Strikes);
            var before = run.Match.DefenseStars;
            Assert.True(run.Pitch(new("fastball", 0, false)));
            Assert.Equal(PlayKind.Strikeout, run.LastPlay!.Kind);
            Assert.True(run.Match.DefenseStars > before);
            Assert.Equal(TutorialPhase.Attempt, run.Phase);
            Assert.Equal(n - 1, run.Successes);
            var earned = run.Match.DefenseStars;
            Assert.True(run.Pitch(new("fastball", 0, true)));
            Assert.Equal(earned - run.Match.PitchStarCost, run.Match.DefenseStars, 5);
            Assert.True(run.Feedback!.Success, run.Feedback.Detail);
            Assert.Equal(n, run.Successes);
            Assert.Equal(run.Feedback, TutorialSession.Replay(content, catalog, run.Recording()).Feedback);
            run.Retry();
        }
        Assert.True(run.Passed);
    }

    [Theory]
    [InlineData("shipped")][InlineData("c80")]
    public void ResourceLessonRejectsOutOfOrderAndCpuOrDemoGains(string profile)
    {
        var (content, catalog) = Load(profile);
        var run = new TutorialSession(content, catalog, "T-G03"); run.Begin();
        Assert.False(run.Pitch(new("fastball", 0, false), LivePlayCommandSource.Cpu));
        Assert.True(run.Pitch(new("fastball", 0, true)));
        Assert.Equal("no-star-earned", run.Feedback!.Code);
        Assert.Equal(0, run.Successes);
        run.Retry();
        Assert.True(run.Pitch(new("fastball", 0, false)));
        Assert.True(run.Pitch(new("fastball", 0, false)));
        Assert.Equal("no-star-spent", run.Feedback!.Code);
        Assert.Equal(0, run.Successes);
        run = new TutorialSession(content, catalog, "T-G03"); run.Begin(demonstration: true);
        Assert.True(run.Pitch(new("fastball", 0, false), LivePlayCommandSource.Cpu));
        Assert.True(run.Pitch(new("fastball", 0, true), LivePlayCommandSource.Cpu));
        Assert.Equal("demonstration", run.Feedback!.Code);
        Assert.Equal(0, run.Successes);
    }

    [Theory, MemberData(nameof(ItemCases))]
    public void ItemNeedsHumanOfferedContactThrowAndRealEffectThreeTimes(string profile, string id)
    {
        var (content, catalog) = Load(profile);
        var run = new TutorialSession(content, catalog, id); run.Begin();
        var named = catalog.Setups.Single(s => s.Id == id).Skill;
        for (var n = 1; n <= 3; n++)
        {
            Assert.True(run.Swing(new(true, 0, 0, false)));
            Assert.True(run.LastHit!.ChemistryItemOffered);
            Assert.True(run.Match.LivePlay.Active);
            var target = run.Match.LivePlay.Preview!.Fielder!.Id;
            Assert.True(run.Item(named, target));
            for (var t = 0; t < 600 && run.Phase == TutorialPhase.Attempt; t++) run.Tick(.05);
            Assert.True(run.Feedback!.Success, id + "/" + profile + ": " + run.Feedback.Detail);
            Assert.Equal(n, run.Successes);
            Assert.Equal(run.Feedback, TutorialSession.Replay(content, catalog, run.Recording()).Feedback);
            run.Retry();
        }
        Assert.True(run.Passed);
    }

    [Theory, MemberData(nameof(ItemCases))]
    public void WrongItemCpuThrowAndDemoDoNotCredit(string profile, string id)
    {
        var (content, catalog) = Load(profile);
        var named = catalog.Setups.Single(s => s.Id == id).Skill;
        var run = new TutorialSession(content, catalog, id); run.Begin();
        Assert.False(run.Item(named, run.Match.DefenseRoster[0].Id));
        Assert.True(run.Swing(new(true, 0, 0, false)));
        var target = run.Match.LivePlay.Preview!.Fielder!.Id;
        Assert.False(run.Item(named, target, LivePlayCommandSource.Cpu));
        Assert.True(run.Item(named == "banana" ? "rocket" : "banana", target));
        Assert.Equal("wrong-item", run.Feedback!.Code);
        Assert.Equal(0, run.Successes);
        run = new TutorialSession(content, catalog, id); run.Begin(demonstration: true);
        Assert.True(run.Swing(new(true, 0, 0, false), LivePlayCommandSource.Cpu));
        Assert.True(run.Item(named, run.Match.LivePlay.Preview!.Fielder!.Id, LivePlayCommandSource.Cpu));
        for (var t = 0; t < 600 && run.Phase == TutorialPhase.Attempt; t++) run.Tick(.05, source: LivePlayCommandSource.Cpu);
        Assert.Equal("demonstration", run.Feedback!.Code);
        Assert.Equal(0, run.Successes);
    }
}
