using GrandSluggers.Sim;
using Xunit;
namespace GrandSluggers.Sim.Tests;

public sealed class TutorialDirectionTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    public static IEnumerable<object[]> Lessons => new[] { "T-P02", "T-B03", "T-B03-L", "T-B05" }.Select(id => new object[] { id });
    TutorialSession Start(string id) { var run = new TutorialSession(_content, TutorialCatalog.Load(_content), id); run.Begin(); return run; }
    static bool Perform(TutorialSession run, LivePlayCommandSource source = LivePlayCommandSource.Human) => run.Lesson.Id switch
    {
        "T-P02" => run.Pitch(new("fastball", 0, false, RubberX: -.6), source),
        "T-B03" => run.Swing(new(true, 0, -2, false), source),
        "T-B03-L" => run.Swing(new(true, 0, 2, false), source),
        "T-B05" => run.Swing(new(true, 0, 0, false, BoxOffsetX: PitchFlight.Crossing(run.CpuPitch, rules: run.Match.Rules).X / HomeSet.BatterWalk), source),
        _ => throw new InvalidOperationException()
    };
    [Theory, MemberData(nameof(Lessons))]
    public void ThreeIndependentResultsPassAndReplay(string id)
    {
        var run = Start(id);
        for (var n = 1; n <= 3; n++)
        {
            Assert.True(Perform(run)); Assert.True(run.Feedback!.Success, id + ": " + run.Feedback + " hit=" + run.LastHit);
            Assert.Equal(n, run.Successes); Assert.Equal(n == 3, run.Passed);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback); Assert.Equal(run.LastHit, replay.LastHit);
            Assert.False(Perform(run)); Assert.Equal(n, run.Successes); run.Retry();
        }
    }
    [Theory, MemberData(nameof(Lessons))]
    public void CpuDemoAndTimeoutCannotEarnProgress(string id)
    {
        var run = Start(id); Assert.False(Perform(run, LivePlayCommandSource.Cpu));
        for (var n = 0; n < 601 && run.Phase == TutorialPhase.Attempt; n++) run.Tick(.05);
        Assert.Equal("timeout", run.Feedback!.Code); Assert.Equal(0, run.Successes);
        run = new TutorialSession(_content, TutorialCatalog.Load(_content), id); run.Begin(demonstration: true);
        Perform(run, LivePlayCommandSource.Cpu); Assert.Equal(0, run.Successes);
    }
    [Theory, MemberData(nameof(Lessons))]
    public void DefaultActionDoesNotTeachTheRequestedDecision(string id)
    {
        var run = Start(id);
        if (id == "T-P02") run.Pitch(new("fastball", 0, false)); else run.Swing(new(true, 0, 0, false));
        Assert.False(run.Feedback!.Success); Assert.Equal(0, run.Successes);
    }
    [Theory]
    [InlineData("T-B03")][InlineData("T-B03-L")]
    public void BothHandsMirrorTheTimingDirection(string id)
    {
        foreach (var hand in new[] { Hand.L, Hand.R })
        {
            var catalog = TutorialCatalog.Load(_content); var index = Array.FindIndex(catalog.Setups, s => s.Id == id);
            var setup = catalog.Setups[index]; var batter = setup.Away.First(c => _content.Characters[c].Bats == hand);
            var captain = setup.Away.First(c => c != batter);
            catalog.Setups[index] = setup with { Away = new[] { captain, batter }.Concat(setup.Away.Where(c => c != batter && c != captain)).ToArray() };
            var run = new TutorialSession(_content, catalog, id); run.Begin(); Assert.Equal(hand, run.Match.Batter.Bats); Perform(run);
            Assert.True(run.Feedback!.Success, run.Feedback.ToString());
            Assert.True((id == "T-B03" ? -1 : 1) * run.LastHit!.SprayDeg * SweetSpot.TipSign(hand) > 0);
        }
    }
    [Fact]
    public void BadDirectionThresholdsAndCentralBoxFixtureAreRejected()
    {
        var c = TutorialCatalog.Load(_content); var i = Array.FindIndex(c.Setups, s => s.Id == "T-B03");
        c.Setups[i] = c.Setups[i] with { MinTimingFrames = 0 }; Assert.Contains(c.Validate(_content), e => e.Contains("meaningful timing"));
        c.Setups[i] = c.Setups[i] with { MinTimingFrames = double.NaN }; Assert.Contains(c.Validate(_content), e => e.Contains("invalid timing"));
        i = Array.FindIndex(c.Setups, s => s.Id == "T-B05"); c.Setups[i] = c.Setups[i] with { Pitch = new("fastball", 0, false) };
        Assert.Contains(c.Validate(_content), e => e.Contains("offset pitch"));
    }
    [Theory]
    [InlineData("T-B03")][InlineData("T-B03-L")][InlineData("T-B05")]
    public void MissesAndWrongSwingTypesCannotSubstituteForTheLesson(string id)
    {
        var run = Start(id); run.Swing(new(true, 0, 50, false, LaunchAim: 1));
        Assert.Equal("miss", run.Feedback!.Code); Assert.Equal(0, run.Successes);
        run.Retry(); run.Swing(new(true, 1, 0, false, LaunchAim: 1));
        Assert.Equal("use-slap", run.Feedback!.Code); Assert.Equal(0, run.Successes);
    }
    [Fact]
    public void HittingTheBatterIsNotACompletedCalledBallLesson()
    {
        var run = Start("T-P02");
        var x = HomeSet.BatterBodyX(run.Match.Batter.Bats, 0);
        run.Pitch(new("fastball", 0, false, AimX: x / PitchFlight.PlateScaleX));
        Assert.Equal(PlayKind.HitByPitch, run.LastPlay!.Kind);
        Assert.False(run.Feedback!.Success); Assert.Equal(0, run.Successes);
    }
    [Fact]
    public void TheStickShapingLessonsAreRetiredWithTheRule()
    {
        // #883 (PH-12; Jack accepted the stick trial on September 22, 2026: "approve all"): an ordinary
        // swing ignores the stick at contact, so a lesson that taught the stick has nothing to teach.
        // T-B06 / T-B06-F, their setups, their objectives and mechanic batting.06 are gone, and no
        // control row claims the stick at contact steers an ordinary swing.
        var catalog = TutorialCatalog.Load(_content);
        Assert.Empty(catalog.Validate(_content));
        Assert.DoesNotContain(catalog.Lessons, l => l.Id is "T-B06" or "T-B06-F" || l.Objective is "grounder-fair" or "fly-fair");
        Assert.DoesNotContain(catalog.Setups, s => s.Id is "T-B06" or "T-B06-F");
        Assert.DoesNotContain(catalog.Mechanics, m => m.Id == "batting.06");
        Assert.DoesNotContain("grounder-fair", TutorialPlateObjectives.SwingIds);
        Assert.DoesNotContain("fly-fair", TutorialPlateObjectives.SwingIds);
        {
            // The bunt's side is its held trigger since P4-b / P4-c (PH-14-R5): no row aims anything at contact.
            var rows = RoleTables.Pad.SelectMany(b => b.Rows).ToArray();
            Assert.DoesNotContain(rows, r => r.Verb == "Spray");
            Assert.DoesNotContain(rows, r => r.Press.Contains("at contact") || r.Verb == "Aim bunt");
        }
    }
    [Theory]
    [InlineData("T-B03", 2)][InlineData("T-B03-L", -2)]
    public void FairContactInTheOppositeTimingDirectionFails(string id, double timing)
    {
        var run = Start(id); run.Swing(new(true, 0, timing, false));
        Assert.True(run.LastHit!.InPlay); Assert.False(run.Feedback!.Success); Assert.Equal(0, run.Successes);
    }

}
