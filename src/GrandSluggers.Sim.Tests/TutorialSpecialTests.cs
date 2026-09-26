using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class TutorialSpecialTests
{
    static readonly ContentCatalog Control = Shipped.Content;
    static readonly string[] StarLessons = ["T-P09", "T-B09",
        "T-SP-heatball", "T-SP-charmball", "T-SP-prismball", "T-SP-phonyball", "T-SP-caskball",
        "T-SP-skullball", "T-SP-fogball", "T-SP-fastball", "T-SP-changeup", "T-SP-breaker", "T-SP-dot", "T-SP-sinkball", "T-SP-lob", "T-SP-sidearm", "T-SP-mirageball", "T-SP-rockfall", "T-SP-leapfrog",
        "T-SS-heat-swing", "T-SS-heart-swing", "T-SS-shell-swing", "T-SS-phony-swing",
        "T-SS-cask-swing", "T-SS-furnace", "T-SS-staff-swing", "T-SS-sidewinder", "T-SS-updraft", "T-SS-pond-skip", "T-SS-ground", "T-SS-fly", "T-SS-line", "T-SS-pull", "T-SS-opposite", "T-SS-chopper", "T-SS-drag-bunt"];
    static readonly string[] ItemLessons = ["T-X01", "T-I-banana", "T-I-rocket", "T-I-pow"];
    public static IEnumerable<object[]> Cases => from profile in new[] { "shipped" }
        from lesson in StarLessons select new object[] { profile, lesson };
    public static IEnumerable<object[]> ItemCases => from profile in new[] { "shipped" }
        from lesson in ItemLessons select new object[] { profile, lesson };

    static (ContentCatalog Content, TutorialCatalog Catalog) Load(string profile)
    {
        Assert.Equal("shipped", profile);
        var content = ContentCatalog.Load(new DataRoot(Control.Root.Shipped));
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
    [InlineData("shipped")]
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
    [InlineData("shipped")]
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
    public void ItemLessonsAreBlockedWhileItemsAreDormant(string profile, string id)
    {
        // PH-16-R15 (#891): the on-deck item offer is removed, so no at-bat offers an item and an item
        // lesson cannot be earned. It is blocked on #891, not deleted: its setup and the item-effect
        // objective stay for a future item source, and a session refuses to start it.
        var (content, catalog) = Load(profile);
        var lesson = catalog.Lesson(id);
        Assert.Equal("blocked", lesson.Status);
        Assert.Equal(891, lesson.Issue);
        Assert.Equal("item-effect", lesson.Objective);
        Assert.Contains(catalog.Setups, s => s.Id == lesson.Setup);
        var setup = catalog.Setups.Single(s => s.Id == lesson.Setup);
        Assert.False(content.Chemistry.ChemistryItemOffered(content.Characters[setup.BatterId], content.Characters[setup.OnDeckId]));
        foreach (var mechanic in lesson.Mechanics) Assert.Equal(891, catalog.Migration[mechanic]);
        Assert.Throws<InvalidOperationException>(() => new TutorialSession(content, catalog, id));
    }

    static void ChaseSpecialGround(TutorialSession run, LivePlayCommandSource source, bool move)
    {
        for (var t = 0; t < 1800 && run.Phase == TutorialPhase.Attempt; t++)
        {
            var live = run.Match.LivePlay;
            var pad = LivePadInput.Dead;
            if (move && live.ElapsedSeconds >= .4 && !live.HoldsBall)
            {
                var dx = live.BallX - live.GloveX;
                var dz = live.BallZ - live.GloveZ;
                var len = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
                pad = new(StickX: dx / len, StickY: dz / len, SouthDown: true);
            }
            run.Tick(1.0 / 60, pad, source);
        }
    }

    [Theory]
    [InlineData("shipped")]
    public void CounterplayFieldsRealCpuStarGrounderWithHumanGloveThreeTimes(string profile)
    {
        var (content, catalog) = Load(profile);
        var run = new TutorialSession(content, catalog, "T-X02"); run.Begin();
        for (var n = 1; n <= 3; n++)
        {
            Assert.Equal("ground", run.LastHit!.StarSwingUsed);
            Assert.True(run.Match.LivePlay.Preview!.Grounder);
            ChaseSpecialGround(run, LivePlayCommandSource.Human, move: true);
            Assert.True(run.Feedback!.Success, profile + ": " + run.Feedback.Detail);
            Assert.Equal(n, run.Successes);
            Assert.Equal(run.Feedback, TutorialSession.Replay(content, catalog, run.Recording()).Feedback);
            run.Retry();
        }
        Assert.True(run.Passed);
    }

    [Theory]
    [InlineData("shipped")]
    public void CounterplayRejectsCpuAssistAndDemonstration(string profile)
    {
        var (content, catalog) = Load(profile);
        var run = new TutorialSession(content, catalog, "T-X02"); run.Begin();
        ChaseSpecialGround(run, LivePlayCommandSource.Cpu, move: true);
        Assert.False(run.Feedback!.Success);
        Assert.Equal(0, run.Successes);
        run = new TutorialSession(content, catalog, "T-X02"); run.Begin(demonstration: true);
        ChaseSpecialGround(run, LivePlayCommandSource.Cpu, move: true);
        Assert.Equal("demonstration", run.Feedback!.Code);
        Assert.Equal(0, run.Successes);
    }
}
