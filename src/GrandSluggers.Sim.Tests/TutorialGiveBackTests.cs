using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// T-R02-B (spec §9.1, OBR 5.06(a)(2)): the player sends the batter-runner to second while the runner who started there holds
/// it. The bag is the lead runner's; credit is a human return on the batter from that shared bag, the batter safe on first and
/// the lead runner on second when the play ends. A tag on the shared bag, a CPU source, a demonstration or dead input earn nothing.
/// </summary>
public sealed class TutorialGiveBackTests
{
    readonly ContentCatalog _content = Shipped.Content;
    const double Frame = 1.0 / 60;

    public enum Scheme { Keys, AllReturn, Controller }

    TutorialSession Start(bool demonstration = false)
    {
        var run = new TutorialSession(_content, TutorialCatalog.Load(_content), "T-R02-B");
        run.Begin(demonstration);
        return run;
    }

    static Runner? Batter(TutorialSession run) => run.Match.BatterRunner;

    /// <summary>Send the batter to second; give the bag back when <paramref name="giveBack"/> says so.</summary>
    static void Drive(TutorialSession run, Scheme scheme, bool giveBack = true,
        LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        var selected = false;
        var gaveBack = false;
        for (var i = 0; i < 2400 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var b = Batter(run);
            var pad = LivePadInput.Dead;
            if (b is { Live: true })
            {
                var send = b.Bag == 1 && b.DestBag < 2 && !gaveBack;
                var onShare = b is { Unentitled: true, Bag: 2, Feet: <= 0 } && giveBack;
                pad = scheme switch
                {
                    Scheme.Controller when send => new(Orders: new(SelectBag: selected ? 0 : 4, Advance: true)),
                    Scheme.Controller when onShare => new(Orders: new(Return: true)),
                    Scheme.Controller => new(Orders: new()),
                    _ when send => new(KeysBag: 4, StickBag: 2),
                    Scheme.Keys when onShare => new(KeysBag: 4, StickBag: 1),
                    Scheme.AllReturn when onShare => new(AllReturn: true),
                    _ => LivePadInput.Dead,
                };
                if (send && scheme == Scheme.Controller) selected = true;
                gaveBack |= onShare;
            }
            run.Tick(Frame, pad, source);
        }
    }

    [Fact]
    public void TheSetupStagesTwoRunnersOnSecondWithTheLeadEntitled()
    {
        var run = Start();
        var lead = run.Match.RunnerAt(2)!;
        var shared = false;
        for (var i = 0; i < 2400 && run.Phase == TutorialPhase.Attempt && !shared; i++)
        {
            var b = Batter(run);
            run.Tick(Frame, b is { Bag: 1, DestBag: < 2 } ? new LivePadInput(KeysBag: 4, StickBag: 2) : LivePadInput.Dead);
            shared = RunnerSystem.Shared(run.Match.Runners);
        }
        Assert.True(shared, "the sent batter reaches second while the lead runner stands on it");
        Assert.True(lead.IsOn(2) && !lead.Unentitled, "the lead runner, not forced, keeps second");
        Assert.True(Batter(run)!.Unentitled, "the batter-runner stands on a bag he is not entitled to");
        Assert.Equal(1, Batter(run)!.ReturnBag);
    }

    [Theory]
    [InlineData(Scheme.Controller)]
    [InlineData(Scheme.Keys)]
    [InlineData(Scheme.AllReturn)]
    public void AHumanGiveBackEarnsThreeDistinctReplayableAttempts(Scheme scheme)
    {
        var run = Start();
        for (var n = 1; n <= 3; n++)
        {
            Drive(run, scheme);
            Assert.True(run.Feedback?.Success == true, $"{scheme}: {run.Feedback} at {run.Elapsed:0.00}; "
                + string.Join(";", run.Match.Runners.Select(r => $"{r.FromBag}:{r.Bag}+{r.Feet:0.0}->{r.DestBag}/{r.Phase}")));
            Assert.Equal("bag-given-back", run.Feedback!.Code);
            Assert.Equal(n, run.Successes);
            Assert.Equal(n == 3, run.Passed);
            Assert.Empty(run.LastPlay!.Outcome?.OutsMade ?? []);
            Assert.NotNull(run.Match.First);
            Assert.NotNull(run.Match.Second);
            var replay = TutorialSession.Replay(_content, TutorialCatalog.Load(_content), run.Recording());
            Assert.Equal(run.Feedback, replay.Feedback);
            run.Retry();
        }
    }

    [Fact]
    public void StayingOnTheSharedBagIsTaggedThereAndFails()
    {
        var run = Start();
        Drive(run, Scheme.Keys, giveBack: false);
        Assert.False(run.Feedback?.Success ?? false);
        Assert.Equal("tagged-on-shared-bag", run.Feedback?.Code);
        var outAt = Assert.Single(run.LastPlay!.Outcome!.OutsMade);
        Assert.Equal(OutType.Tag, outAt.Type);
        Assert.Equal(2, outAt.Bag);
        Assert.Equal(0, outAt.FromBag);
        Assert.Equal(0, run.Successes);
    }

    [Fact]
    public void CpuSourcedDemonstrationAndDeadInputEarnNothing()
    {
        var run = Start();
        Drive(run, Scheme.Keys, source: LivePlayCommandSource.Cpu);
        Assert.False(run.Feedback?.Success ?? false);
        run.Retry();
        for (var i = 0; i < 2400 && run.Phase == TutorialPhase.Attempt; i++) run.Tick(Frame);
        Assert.False(run.Feedback?.Success ?? false);
        Assert.Equal("bag-not-shared", run.Feedback?.Code);

        var demo = Start(demonstration: true);
        Drive(demo, Scheme.Controller);
        Assert.False(demo.Feedback?.Success ?? false);
        Assert.Equal("demonstration", demo.Feedback?.Code);
        Assert.Equal(0, run.Successes + demo.Successes);
    }
}
