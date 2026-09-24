using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// T-G01-S / T-G01-SA: rearrange two non-pitcher gloves in SET through the match's own swap, as the home nine in the
/// top half and as the away nine in the bottom half. Only the player's completed trade of the named pair earns.
/// </summary>
public sealed class TutorialDefenseSwapTests
{
    readonly ContentCatalog _content = Shipped.Content;
    public static IEnumerable<object[]> Lessons => new[] { "T-G01-S", "T-G01-SA" }.Select(id => new object[] { id });

    TutorialSession Start(string id, bool demonstration = false)
    {
        var run = new TutorialSession(_content, TutorialCatalog.Load(_content), id);
        run.Begin(demonstration);
        return run;
    }

    static Dictionary<string, string> Gloves(Match m) =>
        FieldingResolver.Assign(m.DefenseRoster, m.Pitcher, m.Defense.Gloves).ToDictionary(p => p.Key, p => p.Value.Id);

    /// <summary>The window's gesture: highlight each named position, South on each; the lesson owns the trade.</summary>
    static bool Window(TutorialSession run, string first, string second, LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        var pick = new DefenseSetupPick(run.Match);
        bool Route(string a, string b) { run.SwapPositions(a, b, out var changed, source); return changed; }
        pick.Inspect(Array.IndexOf(Diamond.Order, first));
        Assert.False(pick.PickOrSwap(run.Match, null, Route));
        pick.Inspect(Array.IndexOf(Diamond.Order, second));
        return pick.PickOrSwap(run.Match, null, Route);
    }

    [Theory, MemberData(nameof(Lessons))]
    public void SetupStagesTheTeachingNineInSetWithTheNamedPair(string id)
    {
        var run = Start(id);
        var away = id == "T-G01-SA";
        Assert.Equal(!away, run.Match.Top);
        Assert.Equal(away, run.DefendsAsAway);
        Assert.Equal(1, run.Match.Inning);
        Assert.Equal(0, run.Match.Outs);
        Assert.True(run.Match.CanArrangeDefense);
        Assert.True(run.Match.CanSwapPitcher);
        Assert.Same(away ? run.Match.Away : run.Match.Home, run.Match.Defense);
        Assert.Equal(2, run.SwapPair.Count);
        Assert.DoesNotContain("P", run.SwapPair);
        var gloves = Gloves(run.Match);
        Assert.NotEqual(gloves[run.SwapPair[0]], gloves[run.SwapPair[1]]);
    }

    [Theory, MemberData(nameof(Lessons))]
    public void ThreeHumanTradesOfTheNamedPairEarnMasteryAndReplay(string id)
    {
        var run = Start(id);
        for (var n = 1; n <= 3; n++)
        {
            var before = Gloves(run.Match);
            var order = (run.Match.Top ? run.Match.HomeOrder : run.Match.AwayOrder).Select(c => c.Id).ToArray();
            var pools = run.Match.DefenseRoster.ToDictionary(c => c.Id, run.Match.StaminaOf);
            var (a, b) = (run.SwapPair[0], run.SwapPair[1]);
            Assert.True(Window(run, a, b));
            Assert.True(run.Feedback?.Success == true, run.Feedback?.ToString());
            Assert.Equal("positions-traded", run.Feedback!.Code);
            var after = Gloves(run.Match);
            Assert.Equal(before[a], after[b]);
            Assert.Equal(before[b], after[a]);
            foreach (var pos in Diamond.Order.Where(p => p != a && p != b)) Assert.Equal(before[pos], after[pos]);
            Assert.Equal(order, (run.Match.Top ? run.Match.HomeOrder : run.Match.AwayOrder).Select(c => c.Id));
            foreach (var who in run.Match.DefenseRoster) Assert.Equal(pools[who.Id], run.Match.StaminaOf(who));
            Assert.True(run.Match.CanSwapPitcher);
            Assert.Equal(n, run.Successes);
            Assert.Equal(n == 3, run.Passed);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            Assert.Equal(after, Gloves(replay.Match));
            // A second trade after the verdict is not another attempt.
            Assert.False(run.SwapPositions(a, b));
            Assert.Equal(n, run.Successes);
            run.Retry();
            Assert.Equal(before, Gloves(run.Match));
        }
    }

    [Theory, MemberData(nameof(Lessons))]
    public void ReversedPickOrderIsTheSameTrade(string id)
    {
        var run = Start(id);
        Assert.True(Window(run, run.SwapPair[1], run.SwapPair[0]));
        Assert.True(run.Feedback!.Success, run.Feedback.ToString());
    }

    [Theory, MemberData(nameof(Lessons))]
    public void BrowsingAFirstPickAndCancelEarnNothingAndLeaveTheAttemptOpen(string id)
    {
        var run = Start(id);
        var before = Gloves(run.Match);
        var pick = new DefenseSetupPick(run.Match);
        bool Route(string a, string b) { run.SwapPositions(a, b, out var changed); return changed; }
        for (var i = 0; i < Diamond.Order.Length; i++) pick.Inspect(i);
        pick.Inspect(Array.IndexOf(Diamond.Order, run.SwapPair[0]));
        Assert.False(pick.PickOrSwap(run.Match, null, Route));
        pick.CancelPick();
        // Picking the same position twice cancels too.
        Assert.False(pick.PickOrSwap(run.Match, null, Route));
        Assert.False(pick.PickOrSwap(run.Match, null, Route));
        Assert.Null(pick.PickedPosition);
        Assert.Equal(before, Gloves(run.Match));
        Assert.Equal(TutorialPhase.Attempt, run.Phase);
        Assert.Null(run.Feedback);
        Assert.Empty(run.Inputs);
        // The attempt is still live: the named trade after browsing earns.
        Assert.True(Window(run, run.SwapPair[0], run.SwapPair[1]));
        Assert.True(run.Feedback!.Success);
    }

    [Theory, MemberData(nameof(Lessons))]
    public void AWrongPairIsATypedFailureAndRetryRestoresTheDefense(string id)
    {
        var run = Start(id);
        var before = Gloves(run.Match);
        var wrong = Diamond.Order.First(p => p != "P" && !run.SwapPair.Contains(p));
        Assert.True(Window(run, run.SwapPair[0], wrong));
        Assert.False(run.Feedback!.Success);
        Assert.Equal("wrong-pair", run.Feedback.Code);
        Assert.Equal(0, run.Successes);
        run.Retry();
        Assert.Equal(before, Gloves(run.Match));
        Assert.True(Window(run, run.SwapPair[0], run.SwapPair[1]));
        Assert.Equal(1, run.Successes);
    }

    [Theory, MemberData(nameof(Lessons))]
    public void APitcherChangeIsATypedFailureThroughTheQuickSwapAndThePick(string id)
    {
        var run = Start(id);
        var pitcher = run.Match.Pitcher.Id;
        var pick = new DefenseSetupPick(run.Match);
        bool Route(string a, string b) { run.SwapPositions(a, b, out var changed); return changed; }
        pick.Inspect(Array.IndexOf(Diamond.Order, run.SwapPair[0]));
        Assert.True(pick.QuickPitcher(run.Match, null, Route));
        Assert.NotEqual(pitcher, run.Match.Pitcher.Id);
        Assert.False(run.Feedback!.Success);
        Assert.Equal("pitcher-change", run.Feedback.Code);
        Assert.Equal(0, run.Successes);
        run.Retry();
        Assert.Equal(pitcher, run.Match.Pitcher.Id);
        Assert.True(Window(run, "P", run.SwapPair[1]));
        Assert.Equal("pitcher-change", run.Feedback!.Code);
        Assert.Equal(0, run.Successes);
    }

    [Theory, MemberData(nameof(Lessons))]
    public void CpuDemonstrationAndTimeoutNeverEarn(string id)
    {
        var run = Start(id);
        var before = Gloves(run.Match);
        Assert.False(run.SwapPositions(run.SwapPair[0], run.SwapPair[1], LivePlayCommandSource.Cpu));
        Assert.Equal(before, Gloves(run.Match));
        Assert.Empty(run.Inputs);
        for (var n = 0; n < 1300 && run.Phase == TutorialPhase.Attempt; n++) run.Tick(.05);
        Assert.Equal("timeout", run.Feedback!.Code);
        Assert.Equal(0, run.Successes);

        var demo = Start(id, demonstration: true);
        Assert.True(Window(demo, demo.SwapPair[0], demo.SwapPair[1], LivePlayCommandSource.Cpu));
        Assert.False(demo.Feedback!.Success);
        Assert.Equal("demonstration", demo.Feedback.Code);
        Assert.Equal(0, demo.Successes);
    }

    [Theory, MemberData(nameof(Lessons))]
    public void ACommittedPitchClosesTheWindowWithoutRecordingATrade(string id)
    {
        var run = Start(id);
        Assert.True(run.Match.PitchSetup.BeginCharge());
        Assert.False(run.Match.CanArrangeDefense);
        Assert.False(run.SwapPositions(run.SwapPair[0], run.SwapPair[1]));
        Assert.Empty(run.Inputs);
        Assert.Equal(TutorialPhase.Attempt, run.Phase);
    }

    [Fact]
    public void TheSwapPairIsValidatedAsTwoDistinctNonPitcherPositions()
    {
        foreach (var pair in new[] { new[] { "P", "SS" }, new[] { "SS", "SS" }, new[] { "SS" }, new[] { "SS", "DH" } })
        {
            var catalog = TutorialCatalog.Load(_content);
            var i = Array.FindIndex(catalog.Setups, s => s.Id == "T-G01-S");
            catalog.Setups[i] = catalog.Setups[i] with { SwapPair = pair };
            Assert.Contains(catalog.Validate(_content), e => e.Contains("T-G01-S needs two distinct non-pitcher positions"));
        }
        var other = TutorialCatalog.Load(_content);
        var p07 = Array.FindIndex(other.Setups, s => s.Id == "T-P07");
        other.Setups[p07] = other.Setups[p07] with { Bottom = true, SwapPair = ["SS", "CF"] };
        var errors = other.Validate(_content);
        Assert.Contains(errors, e => e.Contains("T-P07 swap pair belongs to the defense-swap policy only"));
        Assert.Contains(errors, e => e.Contains("T-P07 stages the bottom half outside the defense-swap policy"));
    }
}
