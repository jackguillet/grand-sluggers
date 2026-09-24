using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// #732, decision 5 of #730: a tag-up is a race, not a distance. At the catch — once — a CPU runner on
/// second or third goes when the margin to the next bag clears the bag's threshold plus the rung's
/// slack, on the same estimate every other CPU runner read uses (arm and relay included since #722).
/// The table sets the two carry gates to never and authors the thresholds from a measured sweep; a
/// table that sets the gates and puts the thresholds at a margin no play reaches decides by carry alone.
/// </summary>
public sealed class TagUpDecisionTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;

    /// <summary>The game's table with the carry gates at 200 / 250 ft and the race off: the carry-gated tag-up.</summary>
    static readonly RulesTable CarryGated = PatchedRunning(
        ("\"tagThirdMinCarryFt\": 9999", "\"tagThirdMinCarryFt\": 200"),
        ("\"tagSecondMinCarryFt\": 9999", "\"tagSecondMinCarryFt\": 250"),
        ("\"tagUpHomeMarginSec\": 0.25", "\"tagUpHomeMarginSec\": 99"),
        ("\"tagUpThirdMarginSec\": 0.07", "\"tagUpThirdMarginSec\": 99"));

    /// <summary>The shipped rules with <c>rules/running.json</c> patched in a temporary overlay (a whole file, as an overlay must carry).</summary>
    static RulesTable PatchedRunning(params (string From, string To)[] edits)
    {
        var dir = Path.Combine(Path.GetTempPath(), "grand-sluggers-tagup-" + Guid.NewGuid().ToString("N"));
        try
        {
            var text = File.ReadAllText(Path.Combine(Game.Root.Shipped, "rules", "running.json"));
            foreach (var (from, to) in edits)
            {
                Assert.Contains(from, text);
                text = text.Replace(from, to);
            }
            Directory.CreateDirectory(Path.Combine(dir, "rules"));
            File.WriteAllText(Path.Combine(dir, "rules", "running.json"), text);
            return RulesTable.Load(new DataRoot(Game.Root.Shipped, dir));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    // ---------------------------------------------------------------------------------
    // The tables
    // ---------------------------------------------------------------------------------

    [Fact]
    public void TheTableRacesAndGatesNothingByCarry()
    {
        var t = Game.Rules.Running.Cpu;
        Assert.Equal(9999, t.TagThirdMinCarryFt);
        Assert.Equal(9999, t.TagSecondMinCarryFt);
        Assert.Equal(0.25, t.TagUpHomeMarginSec);
        Assert.Equal(0.07, t.TagUpThirdMarginSec);

        // Home from third, third from second, nowhere else.
        Assert.Equal(t.TagUpHomeMarginSec, RunnerAi.TagUpThresholdSec(3, t));
        Assert.Equal(t.TagUpThirdMarginSec, RunnerAi.TagUpThresholdSec(2, t));
        Assert.Equal(double.PositiveInfinity, RunnerAi.TagUpThresholdSec(1, t));
    }

    // ---------------------------------------------------------------------------------
    // The rule, through RunnerAi with a synthetic catch
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// A runner on <paramref name="bag"/>, the fly just caught, the defense's read of the next throw
    /// dialled to land the margin where the case wants it. The table sends on margin, once, at the catch,
    /// and never on the carry; the carry-gated table sends on carry and on nothing else.
    /// </summary>
    [Theory]
    [InlineData("gated", 3, 222, +2.0, true, true)]    // gated: deep enough, sent by the gate whatever the margin
    [InlineData("gated", 3, 222, -2.0, true, true)]
    [InlineData("gated", 3, 150, +2.0, true, false)]   // gated: too shallow, held whatever the margin
    [InlineData("gated", 2, 260, +2.0, true, true)]    // gated: second goes for third on a deep fly to right
    [InlineData("race", 3, 150, +2.0, true, true)]     // a shallow fly with time to spare — go
    [InlineData("race", 3, 260, -2.0, true, false)]    // a deep fly the arm beats — hold
    [InlineData("race", 3, 260, +2.0, false, false)]   // the same margin read later than the catch — the door is shut
    [InlineData("race", 2, 260, +2.0, true, true)]     // second goes for third on the margin
    [InlineData("race", 2, 260, -2.0, true, false)]
    [InlineData("race", 1, 260, +2.0, true, false)]    // a runner on first has no race here
    public void AtTheCatchTheRunnerReadsTheRaceAndTheGatedTableReadsTheCarry(string table, int bag, double carryFt, double marginWanted, bool atCatch, bool expectSent)
    {
        var content = Game;
        var rules = table == "gated" ? CarryGated : content.Rules;
        var who = content.Must("cinder");
        var runner = new Runner(who, bag);
        runner.BeginPlay(forced: false, tagAndGo: false);
        Assert.True(runner.OnBag);
        var next = bag + 1;
        // The glove has it in right field; the runner's read of the throw to the next bag is the clock we dial.
        var arrival = RunnerSystem.ArrivalSec(runner, next, 0, rules, 0) + marginWanted - rules.Running.Cpu.ReactionSec;
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
        var rules = Game.Rules.AtLevel(rung);
        var runner = new Runner(Game.Must("konga"), 3);
        runner.BeginPlay(forced: false, tagAndGo: false);
        var arrival = RunnerSystem.ArrivalSec(runner, 4, 0, rules, 0) + marginWanted - rules.Running.Cpu.ReactionSec;
        var ball = new BallSituation(true, false, 0, 0, 0, 250, 0, true, 0, 250, 250, ThrowClock: (x, z, b) => b == 4 ? arrival : 99);
        var ctx = new RunnerAiContext(0, 1, int.MinValue, FlyState.Caught, ball, 0, AtCatch: true);
        RunnerAi.Decide([runner], ctx, _ => false, rules);
        Assert.Equal(expectSent, runner.DestBag == 4);
    }

    // ---------------------------------------------------------------------------------
    // S-54's play
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// S-54's own play — vine (Arm 8) in left, konga / cinder / dart on third — at 215 ft rather than the
    /// row's 222, because at 222 cinder's read margin (+0.257) sits within a hundredth of the normal line;
    /// at 215 konga reads −0.243, cinder +0.117 and dart +0.597. A carry gate would send all three because
    /// the fly is deep; the race sends only the body that can win it. Each send is checked against the
    /// margin the runner actually read at the catch, through <see cref="LivePlaySystem.RunnerRead"/>,
    /// against the threshold plus the rung's slack.
    /// </summary>
    [Theory]
    [InlineData("konga", false)]
    [InlineData("cinder", false)]
    [InlineData("dart", true)]
    public void S54sPlaySendsOnlyTheRunnerWhoCanWinTheRace(string who, bool expectSent)
    {
        var home = Game.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = Game.Team("Offense", "zig", "boom", "jester", "grit", "soot", "nugget", "pip", "gull", "marlow");
        var match = Match.Exhibition(Game, home, away, 3, 1, parkId: ParkIds.Harbor);
        var runner = Game.Must(who);
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
