using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// #732, decision 5 of #730: a tag-up is a race, not a distance. At the catch — once — a CPU runner on
/// second or third goes when the margin to the next bag clears the bag's threshold plus the rung's
/// slack, on the same estimate every other CPU runner read uses (arm and relay included since #722).
/// The shipped table keeps the two carry gates and sets the thresholds to a margin no play reaches, so
/// it decides exactly as it always did; the <c>c80</c> copy sets the gates to never and authors the
/// thresholds from a measured sweep.
/// </summary>
public sealed class TagUpDecisionTests
{
    static readonly ContentCatalog Control = ContentCatalog.Load();
    static readonly DataRoot Root = new(Control.Root.Shipped, Path.GetFullPath(Path.Combine(Control.Root.Shipped, "..", "trials", "c80")));
    static readonly ContentCatalog Trial = ContentCatalog.Load(Root);
    const double Frame = 1.0 / 60.0;

    // ---------------------------------------------------------------------------------
    // The tables
    // ---------------------------------------------------------------------------------

    [Fact]
    public void TheShippedTableGatesByCarryAndTheTrialRaces()
    {
        var shipped = Control.Rules.Running.Cpu;
        Assert.Equal(200, shipped.TagThirdMinCarryFt);
        Assert.Equal(250, shipped.TagSecondMinCarryFt);
        Assert.Equal(99, shipped.TagUpHomeMarginSec);
        Assert.Equal(99, shipped.TagUpThirdMarginSec);

        var trial = Trial.Rules.Running.Cpu;
        Assert.Equal(9999, trial.TagThirdMinCarryFt);
        Assert.Equal(9999, trial.TagSecondMinCarryFt);
        Assert.Equal(0.25, trial.TagUpHomeMarginSec);
        Assert.Equal(0.07, trial.TagUpThirdMarginSec);

        // The runner clock is the C80 anchor: the carried file changes the tag-up and nothing about the clock.
        Assert.Equal(Control.Rules.Running.BagSec.BaseSec, Trial.Rules.Running.BagSec.BaseSec);
        Assert.Equal(Control.Rules.Running.BagSec.SecPerRun, Trial.Rules.Running.BagSec.SecPerRun);
        Assert.Equal(Control.Rules.Running.Cpu.ReactionSec, Trial.Rules.Running.Cpu.ReactionSec);
        Assert.Equal(Control.Rules.Running.Cpu.OutfieldGoSec, Trial.Rules.Running.Cpu.OutfieldGoSec);

        // Home from third, third from second, nowhere else.
        Assert.Equal(trial.TagUpHomeMarginSec, RunnerAi.TagUpThresholdSec(3, trial));
        Assert.Equal(trial.TagUpThirdMarginSec, RunnerAi.TagUpThresholdSec(2, trial));
        Assert.Equal(double.PositiveInfinity, RunnerAi.TagUpThresholdSec(1, trial));
    }

    // ---------------------------------------------------------------------------------
    // The rule, through RunnerAi with a synthetic catch
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// A runner on <paramref name="bag"/>, the fly just caught, the defense's read of the next throw
    /// dialled to land the margin where the case wants it. The shipped table sends on carry and on
    /// nothing else; the trial sends on margin, once, at the catch, and never on the carry.
    /// </summary>
    [Theory]
    [InlineData("control", 3, 222, +2.0, true, true)]    // shipped: deep enough, sent by the gate whatever the margin
    [InlineData("control", 3, 222, -2.0, true, true)]
    [InlineData("control", 3, 150, +2.0, true, false)]   // shipped: too shallow, held whatever the margin
    [InlineData("control", 2, 260, +2.0, true, true)]    // shipped: second goes for third on a deep fly to right
    [InlineData("trial", 3, 150, +2.0, true, true)]      // trial: a shallow fly with time to spare — go
    [InlineData("trial", 3, 260, -2.0, true, false)]     // trial: a deep fly the arm beats — hold
    [InlineData("trial", 3, 260, +2.0, false, false)]    // trial: the same margin read later than the catch — the door is shut
    [InlineData("trial", 2, 260, +2.0, true, true)]      // trial: second goes for third on the margin
    [InlineData("trial", 2, 260, -2.0, true, false)]
    [InlineData("trial", 1, 260, +2.0, true, false)]     // trial: a runner on first has no race here
    public void AtTheCatchTheShippedRunnerReadsTheCarryAndTheTrialRunnerReadsTheRace(string root, int bag, double carryFt, double marginWanted, bool atCatch, bool expectSent)
    {
        var content = root == "trial" ? Trial : Control;
        var rules = content.Rules;
        var who = content.Must("cinder");
        var runner = new Runner(who, bag);
        runner.BeginPlay(forced: false, tagAndGo: false);
        Assert.True(runner.OnBag);
        var next = bag + 1;
        // The glove has it in right field; the runner's read of the throw to the next bag is the clock we dial.
        var arrival = RunnerSystem.ArrivalSec(runner, next, 0, 0, rules) + marginWanted - rules.Running.Cpu.ReactionSec;
        var ball = new BallSituation(true, false, 0, 0, 90, 240, 0, true, 90, 240, carryFt, ThrowClock: (x, z, b) => b == next ? arrival : 99);
        var ctx = new RunnerAiContext(0, 1, int.MinValue, FlyState.Caught, ball, 0, AtCatch: atCatch);
        Assert.Equal(marginWanted, RunnerAi.Margin(runner, next, ctx, rules), 6);

        RunnerAi.Decide([runner], ctx, _ => false, rules);
        Assert.Equal(expectSent, runner.DestBag == next);
        if (!expectSent) Assert.True(runner.OnBag);
    }

    /// <summary>The threshold and the rung's slack together: normal needs 0.25 s of read margin home, hard 0.10, easy 0.40.</summary>
    [Theory]
    [InlineData("normal", 0.20, false)]
    [InlineData("normal", 0.30, true)]
    [InlineData("hard", 0.05, false)]
    [InlineData("hard", 0.15, true)]
    [InlineData("easy", 0.35, false)]
    [InlineData("easy", 0.45, true)]
    public void TheRungsSlackMovesTheLine(string rung, double marginWanted, bool expectSent)
    {
        var rules = Trial.Rules.AtLevel(rung);
        var runner = new Runner(Trial.Must("konga"), 3);
        runner.BeginPlay(forced: false, tagAndGo: false);
        var arrival = RunnerSystem.ArrivalSec(runner, 4, 0, 0, rules) + marginWanted - rules.Running.Cpu.ReactionSec;
        var ball = new BallSituation(true, false, 0, 0, 0, 250, 0, true, 0, 250, 250, ThrowClock: (x, z, b) => b == 4 ? arrival : 99);
        var ctx = new RunnerAiContext(0, 1, int.MinValue, FlyState.Caught, ball, 0, AtCatch: true);
        RunnerAi.Decide([runner], ctx, _ => false, rules);
        Assert.Equal(expectSent, runner.DestBag == 4);
    }

    // ---------------------------------------------------------------------------------
    // S-54's play under the trial
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// S-54's own play — vine (Arm 8) in left, konga / cinder / dart on third — read under the trial, at
    /// 215 ft rather than the row's 222 because at 222 cinder's read margin sits within a hundredth of the
    /// normal line (the sweep put him at +0.11 here and +0.42 at 230). The shipped rule sends all three
    /// because the fly is deep; the race sends only the body that can win it. Each send is checked against the margin the runner actually read at the catch,
    /// through <see cref="LivePlaySystem.RunnerRead"/>, against the threshold plus the rung's slack.
    /// In process the bodies stand on the shipped spots (the trial README says why), so this is a
    /// decision test: the geometry the sweep was authored on is the <c>GRAND_SLUGGERS_TRIAL</c> process.
    /// </summary>
    [Theory]
    [InlineData("konga", false)]
    [InlineData("cinder", false)]
    [InlineData("dart", true)]
    public void S54sPlayUnderTheTrialSendsOnlyTheRunnerWhoCanWinTheRace(string who, bool expectSent)
    {
        var home = Trial.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = Trial.Team("Offense", "zig", "boom", "jester", "grit", "soot", "nugget", "pip", "gull", "marlow");
        var match = Match.Exhibition(Trial, home, away, 3, 1, parkId: "harbor-diamond");
        var runner = Trial.Must(who);
        Assert.True(match.StationRunner(3, runner));
        Assert.True(match.SetOuts(1));
        var body = match.RunnerAt(3)!;
        var hit = FlightFixtures.Landing(match.Park, 215, 34, -30, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("LF", preview.Position);

        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        PlayEvent? play = null;
        var margin = double.NaN;
        var sent = false;
        var sentAfterTheCatchFrame = false;
        var caughtAt = -1;
        for (var i = 0; i < 60 * 30 && play is null; i++)
        {
            var r = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu));
            if (live.Fly == FlyState.Caught && caughtAt < 0)
            {
                caughtAt = i;
                margin = RunnerAi.Margin(body, 4, live.RunnerRead(live.Dash01), match.Rules);
            }
            var going = live.Fly == FlyState.Caught && body.Live && body.FromBag == 3 && body.Feet > 0;
            if (going && !sent && caughtAt >= 0 && i > caughtAt + 1) sentAfterTheCatchFrame = true;
            if (going) sent = true;
            play = r.CompletedPlay;
        }
        Assert.NotNull(play);
        var threshold = match.Rules.Running.Cpu.TagUpHomeMarginSec + match.Rules.Cpu.Active.RunnerMarginSec;
        Assert.True(sent == margin > threshold, $"{who}: read margin {margin:+0.000} against {threshold:+0.00}, sent {sent}");
        Assert.Equal(expectSent, sent);
        // Once, at the catch: a body held there is not sent by a later event.
        Assert.False(sentAfterTheCatchFrame, $"{who} was sent after the catch frame");
    }
}
