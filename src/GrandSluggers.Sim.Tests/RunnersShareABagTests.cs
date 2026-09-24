using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Two runners on one bag (spec §9.1, OBR 5.06(a)(2), #688): a trail runner can run up to a bag the runner ahead stands on;
/// the preceding runner is entitled to it unless forced off it, when the following runner is; the other body is not
/// tag-safe there, the play does not end with both on it, and the CPU runner gives it back when the bag behind is free.
/// </summary>
public sealed class RunnersShareABagTests
{
    readonly ContentCatalog _content = Shipped.Content;
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanOffense = new(HumanBats: true, HumanPitches: false, PlayerMustField: false, Versus: false);
    static readonly Func<int, bool> NoForce = _ => false;

    Character Who(int i) => new Scenario(_content).Match.AwayOrder[i];

    static void TickUntil(IReadOnlyList<Runner> runners, Func<int, bool> forceAt, Func<bool> done, double maxSec = 12, FlyState fly = FlyState.None)
    {
        for (var t = 0.0; t < maxSec && !done(); t += Frame)
            RunnerSystem.Tick(runners, Frame, new RunnerTickContext(t, 0, fly, 0, forceAt, NoForce), rules: Rules.Default);
    }

    /// <summary>A runner from <paramref name="from"/> run all the way onto <paramref name="bag"/> by the tick, next to whoever stands there.</summary>
    Runner RunOnto(List<Runner> runners, int from, int bag, int order, Func<int, bool>? forceAt = null)
    {
        var trail = new Runner(Who(order), from);
        trail.BeginPlay(false, false);
        trail.Send(bag, human: true);
        runners.Add(trail);
        TickUntil(runners, forceAt ?? NoForce, () => trail.IsOn(bag) || !trail.Moving && trail.Velocity == 0 && trail.Feet > 0);
        return trail;
    }

    // ---------------------------------------------------------------------------------
    // Movement: up to a bag, never past a body between bags
    // ---------------------------------------------------------------------------------

    [Fact]
    public void ATrailRunnerSentToAnOccupiedBagRunsAllTheWayToIt()
    {
        var lead = new Runner(Who(2), 2);
        var runners = new List<Runner> { lead };
        var trail = RunOnto(runners, 1, 2, 1);

        Assert.True(trail.IsOn(2), $"the trail runner stopped {trail.SegmentFt - trail.Feet:0} ft short of second, bag {trail.Bag}");
        Assert.True(lead.IsOn(2), "the lead runner still stands on second");
    }

    [Fact]
    public void NoRunnerPassesABodyBetweenTheBags()
    {
        var rules = Rules.Default;
        var lead = new Runner(Who(2), 2);
        lead.BeginPlay(false, false);
        lead.Send(3, human: true);
        var runners = new List<Runner> { lead };
        TickUntil(runners, NoForce, () => lead.Feet >= 30);
        lead.Halt();
        var trail = new Runner(Who(1), 1);
        trail.BeginPlay(false, false);
        trail.Send(3, human: true);
        runners.Add(trail);
        var closest = double.MaxValue;
        for (var t = 0.0; t < 10; t += Frame)
        {
            RunnerSystem.Tick(runners, Frame, new RunnerTickContext(t, 0, FlyState.None, 0, NoForce, NoForce), rules: Rules.Default);
            closest = Math.Min(closest, lead.Progress - trail.Progress);
        }

        Assert.True(lead.Feet > 0, "the lead body is between second and third");
        Assert.True(closest >= rules.Running.BagSec.NoPassFt - 1e-6, $"the trail body came within {closest:0.0} ft of a body between the bags");
    }

    [Fact]
    public void AForcedRunnerIsForcedOneBagNotTheRestOfTheChain()
    {
        // Bases loaded on a grounder: the runner from first is forced to second, and no further. Reaching second ends his force
        // even while the force at third (the runner from second's) still stands.
        var fromFirst = new Runner(Who(1), 1);
        fromFirst.BeginPlay(true, false);
        var fromSecond = new Runner(Who(2), 2);
        fromSecond.BeginPlay(true, false);
        var runners = new List<Runner> { fromFirst, fromSecond };
        Func<int, bool> loaded = bag => bag is >= 1 and <= 4;
        TickUntil(runners, loaded, () => false, maxSec: 10);

        Assert.True(fromFirst.IsOn(2), $"the runner from first is at bag {fromFirst.Bag} + {fromFirst.Feet:0} ft, bound for {fromFirst.DestBag}");
        Assert.False(RunnerSystem.ForcedOff(fromFirst, loaded, FlyState.None));
    }

    // ---------------------------------------------------------------------------------
    // Entitlement: the preceding runner, unless forced off
    // ---------------------------------------------------------------------------------

    [Fact]
    public void TheLeadRunnerNotForcedIsEntitledAndTheTrailRunnerIsNot()
    {
        var lead = new Runner(Who(2), 2);
        var runners = new List<Runner> { lead };
        var trail = RunOnto(runners, 1, 2, 1);

        Assert.Same(lead, RunnerSystem.EntitledOn(runners, 2, NoForce, FlyState.None));
        Assert.True(RunnerSystem.Protects(runners, lead, 2, NoForce, FlyState.None));
        Assert.False(RunnerSystem.Protects(runners, trail, 2, NoForce, FlyState.None));
        Assert.True(RunnerSystem.Unentitled(runners, trail, NoForce, FlyState.None));
        Assert.True(trail.Unentitled, "the tick marks the body the bag does not protect");
        Assert.False(lead.Unentitled);
    }

    [Fact]
    public void TheLeadRunnerForcedOffTheBagLosesItToTheTrailRunner()
    {
        var lead = new Runner(Who(1), 1);
        lead.BeginPlay(true, false);
        var trail = Runner.BatterRunner(Who(2), Diamond.Home.X, Diamond.Home.Z);
        var runners = new List<Runner> { lead, trail };
        Func<int, bool> force = bag => bag is 1 or 2;
        // Both stand on first (the batter's bag and the forced runner's start): the force on the lead gives it to the trail.
        trail.Arrive(1, 0);
        Assert.True(lead.IsOn(1) && trail.IsOn(1));

        Assert.Same(trail, RunnerSystem.EntitledOn(runners, 1, force, FlyState.None));
        Assert.True(RunnerSystem.Unentitled(runners, lead, force, FlyState.None));
        // A caught fly removes the force: the preceding runner holds the bag again.
        Assert.Same(lead, RunnerSystem.EntitledOn(runners, 1, force, FlyState.Caught));
    }

    [Fact]
    public void ForcedLeadAndBatterOnFirstTheTagOnTheBagRetiresTheLeadRunner()
    {
        // A fly in the air holds the runner on first while the batter reaches first; the fly comes down uncaught, the force
        // is on, and the batter holds first (§9.1). A glove with the ball on first tags the lead runner out; the batter is safe.
        var scenario = new Scenario(_content, seed: 1).Runner(1, 1);
        var match = scenario.Match;
        var lead = match.RunnerAt(1)!;
        scenario.Contact();
        var live = match.LivePlay;
        live.Apply(LivePlayCommand.Begin(PlayKind.FlyOut, LivePlayCommandSource.Cpu));
        var batter = match.BatterRunner!;
        for (var i = 0; i < 60 * 8 && !(batter.OnBag && batter.Bag == 1); i++)
            live.Apply(LivePlayCommand.Advance(Frame, PlayKind.FlyOut, false, false, false, 0, LivePlayCommandSource.Cpu));
        Assert.True(batter.IsOn(1) && lead.IsOn(1), $"batter {batter.Bag}+{batter.Feet:0}, lead {lead.Bag}+{lead.Feet:0}");

        var first = Diamond.First;
        var tag = live.Apply(LivePlayCommand.Contact(PlayKind.Single, true, false, true, first.X, first.Z, 0, match.Pitcher, LivePlayCommandSource.Cpu));

        Assert.Equal(1, tag.TaggedFromBag);
        Assert.True(lead.Out, "the forced lead runner standing on first is out on the tag");
        Assert.True(batter.Live, "the batter holds first");
    }

    // ---------------------------------------------------------------------------------
    // The play's end and the bodies that resolve it
    // ---------------------------------------------------------------------------------

    [Fact]
    public void TimeDoesNotComeWhileTwoRunnersStandOnOneBag()
    {
        var lead = new Runner(Who(2), 2);
        var runners = new List<Runner> { lead };
        var trail = RunOnto(runners, 1, 2, 1);
        trail.Send(2);
        TickUntil(runners, NoForce, () => false, maxSec: 2);
        Assert.True(lead.OnBag && trail.OnBag && trail.OnBagSec >= Rules.Default.Running.Bags.TimeOnBagSec);

        Assert.False(InPlay.Time(true, false, 0, true, runners, rules: Rules.Default), "two bodies on second: one of them owes the play a move");
        trail.Retire();
        Assert.True(InPlay.Time(true, false, 0, true, runners, rules: Rules.Default));
    }

    [Fact]
    public void TheCpuRunnerGivesTheBagBackWhenTheBagBehindIsFree()
    {
        var lead = new Runner(Who(2), 2);
        var runners = new List<Runner> { lead };
        var trail = RunOnto(runners, 1, 2, 1);
        var ball = new BallSituation(true, false, 0, 0, 0, 60, 0, false, 0, 60, 0);
        RunnerAi.Decide(runners, new RunnerAiContext(0, 0, 0, FlyState.None, ball, 0), NoForce, rules: Rules.Default);
        Assert.Equal(1, trail.DestBag);
        TickUntil(runners, NoForce, () => trail.IsOn(1) && trail.OnBag);

        Assert.True(trail.IsOn(1), "back on first by his own legs");
        Assert.True(lead.IsOn(2));
        Assert.False(RunnerSystem.Shared(runners));
    }

    [Fact]
    public void TheCpuRunnerWaitsOnTheBagWhenTheBagBehindIsTaken()
    {
        var lead = new Runner(Who(3), 2);
        var behind = new Runner(Who(1), 1);
        var runners = new List<Runner> { lead, behind };
        var trail = new Runner(Who(2), 1);
        trail.Arrive(2, 0);
        runners.Add(trail);
        RunnerSystem.MarkShares(runners, NoForce, FlyState.None);
        var ball = new BallSituation(true, false, 0, 0, 0, 60, 0, false, 0, 60, 0);
        RunnerAi.Decide(runners, new RunnerAiContext(0, 0, 0, FlyState.None, ball, 0), NoForce, rules: Rules.Default);

        Assert.Equal(2, trail.DestBag);
        Assert.True(trail.Unentitled);
    }

    [Fact]
    public void TheCpuGloveTagsARunnerStandingOnABagHeIsNotEntitledTo()
    {
        // Runner on second, a ball into right center. The human offense sends the batter on to second and holds the
        // runner there (the AI never moves a human's runner): the batter reaches second, the bag is the lead runner's, and the
        // CPU glove with the ball tags the batter. The play ends with one body on second.
        var scenario = new Scenario(_content, seed: 1).Runner(2, 2);
        var match = scenario.Match;
        var lead = match.RunnerAt(2)!;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 230, 10, 30);
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, match.ResolveFielding(hit, preview), HumanOffense)).Snapshot.Active);
        var batter = match.BatterRunner!;
        var reachedSecond = false;
        var sharedAtTime = false;
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 40 && play is null; i++)
        {
            if (batter.Live && batter.DestBag < 2) batter.Send(2, human: true);
            if (lead.Live && lead.DestBag != 2) lead.Halt();
            var result = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead));
            if (batter.Live && batter.IsOn(2)) reachedSecond = true;
            if (result.Snapshot.IsTime && RunnerSystem.Shared(match.Runners)) sharedAtTime = true;
            play = result.CompletedPlay;
        }

        Assert.NotNull(play);
        Assert.True(reachedSecond, "the batter ran all the way to second: " + Scenario.Fingerprint(play!) + $" at {live.ElapsedSeconds:0.0}");
        Assert.False(sharedAtTime);
        var outAt = Assert.Single(play!.Outcome!.OutsMade);
        Assert.Equal(0, outAt.FromBag);
        Assert.Equal(OutType.Tag, outAt.Type);
        Assert.Equal(2, outAt.Bag);
        Assert.Equal(lead.Who.Id, match.Second?.Id);
    }

    // ---------------------------------------------------------------------------------
    // Presentation: two bodies on one bag never merge
    // ---------------------------------------------------------------------------------

    [Fact]
    public void TwoBodiesOnOneBagDrawApart()
    {
        var feel = _content.Feel;
        var lead = new Runner(Who(2), 2);
        var runners = new List<Runner> { lead };
        var trail = RunOnto(runners, 1, 2, 1);
        var a = lead.DrawPosition(feel.RunnerShareStepFt);
        var b = trail.DrawPosition(feel.RunnerShareStepFt);

        Assert.Equal(Diamond.Second, a);
        Assert.Equal(Diamond.Second, trail.Position);
        Assert.True(Diamond.Dist(a.X, a.Z, b.X, b.Z) >= 2 * feel.RaceCamera.BodyRadiusFt - 1e-6,
            $"the two bodies stand {Diamond.Dist(a.X, a.Z, b.X, b.Z):0.0} ft apart, two body radii is {2 * feel.RaceCamera.BodyRadiusFt:0.0}");
        // Off the bag toward first, on the baseline he came up.
        var first = Diamond.First;
        var along = Diamond.Dist(first.X, first.Z, b.X, b.Z) + Diamond.Dist(b.X, b.Z, a.X, a.Z);
        Assert.Equal(Diamond.Dist(first.X, first.Z, a.X, a.Z), along, 6);
    }
}
