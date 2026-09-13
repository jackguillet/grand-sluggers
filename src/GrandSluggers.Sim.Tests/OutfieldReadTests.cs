using GrandSluggers.Sim;
using Xunit;
using Xunit.Abstractions;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The outfield read (spec §8.2, sitting 2026-09-12, #609): the reaction lockout is the reference's 50 frames
/// (0.83 s), never longer than the ball's hang; the human glove waits the reference at every rung and difficulty
/// scales only the CPU's; the infield and catcher numbers the §10.4 double-play rows were tuned on do not move.
/// S-29 is held by the outfielder's chase on a ball in the air (<c>fielding.chase.outfieldAirMul</c>), not by
/// freezing the read.
/// </summary>
public sealed class OutfieldReadTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    readonly ITestOutputHelper _out;
    const double Frame = 1.0 / 60.0;

    public OutfieldReadTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void TheOutfieldReadIsTheReferenceAndTheInfieldDidNotMove()
    {
        var re = _content.Rules.Fielding.Reaction;
        Assert.Equal(0.42, re.PitcherSec, 6);
        Assert.Equal(0.67, re.CatcherSec, 6);
        Assert.Equal(0.27, re.FirstSec, 6);
        Assert.Equal(0.25, re.SecondSec, 6);
        Assert.Equal(0.30, re.ThirdSec, 6);
        Assert.Equal(0.28, re.ShortSec, 6);
        Assert.Equal(0.83, re.OutfieldSec, 6);
        foreach (var of in new[] { "LF", "CF", "RF" })
            Assert.Equal(0.83, re.LockoutSec(of), 6);
    }

    [Fact]
    public void ALockoutNeverOutlastsABallInTheAirAndADirtBallKeepsTheInfieldNumbers()
    {
        var rules = _content.Rules;
        var capped = FieldingResolver.ReactionLockouts(rules, 1, airHangSec: 0.5);
        foreach (var kv in capped) Assert.True(kv.Value <= 0.5 + 1e-9, $"{kv.Key} waits {kv.Value} on a 0.5 s hang");
        var dirt = FieldingResolver.ReactionLockouts(rules);
        foreach (var pos in Diamond.Order) Assert.Equal(rules.Fielding.Reaction.LockoutSec(pos), dirt[pos], 9);
    }

    [Theory]
    [InlineData("easy")]
    [InlineData("normal")]
    [InlineData("hard")]
    public void DifficultyScalesTheCpuLockoutOnly(string level)
    {
        var rules = _content.Rules.AtLevel(level);
        var mul = rules.Cpu.Active.ReactionMul;
        var cpu = FieldingResolver.CpuReactionLockouts(rules);
        var human = FieldingResolver.ReactionLockouts(rules);
        foreach (var pos in Diamond.Order)
        {
            Assert.Equal(rules.Fielding.Reaction.LockoutSec(pos), human[pos], 9);
            Assert.Equal(rules.Fielding.Reaction.LockoutSec(pos) * mul, cpu[pos], 9);
        }
        if (level == "normal") Assert.Equal(1.0, mul, 9);
    }

    [Fact]
    public void S609_ALinerToLeftHasTheLeftFielderMovingBeforeItLands()
    {
        var scenario = new Scenario(_content, seed: 1);
        var match = scenario.Match;
        scenario.Contact();
        // The issue's 1.1 s liner lands on the dirt; the quickest liner that reaches left field's grass at Harbor hangs
        // about 1.4 s (linerTimeScale 1.0). Before #609 the outfield read (2.4 s) outlasted every such rope.
        var hit = LinerToLeft(match, targetHang: 1.1);
        var preview = match.PreviewHit(hit);
        var hang = preview.HangTimeSec;
        _out.WriteLine($"liner {hit.ExitVeloMph:0.0} mph at {hit.LaunchDeg:0}° / {hit.SprayDeg:0}°: hang {hang:0.00} s, landing ({preview.LandingX:0}, {preview.LandingZ:0}), picked {preview.Position}");
        Assert.InRange(hang, 1.0, 1.5);
        var movedAt = FirstMove(match, hit, preview, "LF", LiveSeats.CpuOnly);
        _out.WriteLine($"LF moves at {movedAt:0.00} s");
        Assert.True(movedAt < hang, $"LF first moves at {movedAt:0.00} s; the liner lands at {hang:0.00} s");
    }

    [Theory]
    [InlineData(LivePlayCommandSource.Cpu)]
    [InlineData(LivePlayCommandSource.Human)]
    public void S609_ARoutineFlyToCenterHasCenterMovingInsideNineTenthsOfASecond(LivePlayCommandSource seat)
    {
        var scenario = new Scenario(_content, seed: 1);
        var match = scenario.Match;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 250, 34, 0);
        var preview = match.PreviewHit(hit);
        Assert.Equal("CF", preview.Position);
        var seats = seat == LivePlayCommandSource.Human
            ? new LiveSeats(HumanBats: false, HumanPitches: true, PlayerMustField: false, Versus: false)
            : LiveSeats.CpuOnly;
        var movedAt = FirstMove(match, hit, preview, "CF", seats);
        _out.WriteLine($"{seat}: CF moves at {movedAt:0.00} s on a {preview.HangTimeSec:0.00} s fly");
        Assert.True(movedAt <= 0.9, $"CF first moves at {movedAt:0.00} s");
        Assert.True(movedAt >= match.Rules.Fielding.Reaction.OutfieldSec - Frame, $"CF moved at {movedAt:0.00} s, before its read");
    }

    [Fact]
    public void AtEveryRungTheHumanGloveWaitsTheReferenceAndTheCpuGloveWaitsItsRung()
    {
        foreach (var level in CpuRules.Levels)
        {
            var human = StickMoveAt(level, human: true);
            var cpu = StickMoveAt(level, human: false);
            var re = _content.Rules.AtLevel(level);
            var reference = re.Fielding.Reaction.OutfieldSec;
            _out.WriteLine($"{level}: human glove moves at {human:0.00} s, CPU glove at {cpu:0.00} s");
            Assert.InRange(human, reference - Frame, reference + 2 * Frame);
            Assert.InRange(cpu, reference * re.Cpu.Active.ReactionMul - Frame, reference * re.Cpu.Active.ReactionMul + 2 * Frame);
        }
    }

    [Fact]
    public void AnOutfielderOnABallInTheAirChasesAtTheAirMultiplierAndTheInfieldDoesNot()
    {
        var rules = _content.Rules;
        var who = _content.Must("rio");
        var fly = FlightFixtures.Preview(who, "CF", BattedBallClass.Fly, 4, 0, 250);
        var grounder = FlightFixtures.Preview(who, "SS", BattedBallClass.Grounder, 1, -40, 100);
        var one = FieldingResolver.ChaseSpeedFt(who, false, rules);
        Assert.Equal(one * rules.Fielding.Chase.OutfieldAirMul, FieldingResolver.ChaseSpeedFt(who, "CF", fly, rules), 9);
        Assert.Equal(one, FieldingResolver.ChaseSpeedFt(who, "SS", fly, rules), 9);
        Assert.Equal(one, FieldingResolver.ChaseSpeedFt(who, "LF", grounder with { Position = "LF" }, rules), 9);
        Assert.Equal(one, FieldingResolver.ChaseSpeedFt(who, "CF", null, rules), 9);
    }

    // ---------------------------------------------------------------------------------

    static AtBatResult LinerToLeft(Match match, double targetHang)
    {
        AtBatResult? best = null;
        var bestErr = double.MaxValue;
        for (var launch = 10.0; launch <= 20; launch += 1)
        for (var exit = 74.0; exit <= 140; exit += 0.5)
        {
            var hit = FlightFixtures.Hit(match.Park, exit, launch, -30);
            if (hit.Class != BattedBallClass.Liner || hit.Foul) continue;
            var ball = BattedBall.Of(hit, match.Park, match.Rules);
            if (!FieldingResolver.OutfieldGrass(ball.LandingX, ball.LandingZ, match.Rules)) continue;
            var err = Math.Abs(ball.HangT - targetHang);
            if (err < bestErr)
            {
                bestErr = err;
                best = hit;
            }
        }
        Assert.NotNull(best);
        return best!;
    }

    /// <summary>Play seconds at which the body at <paramref name="pos"/> first leaves its spot.</summary>
    static double FirstMove(Match match, AtBatResult hit, FieldingPreview preview, string pos, LiveSeats seats)
    {
        var live = match.LivePlay;
        var field = match.ResolveFielding(hit, preview);
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field, seats)).Snapshot.Active);
        var start = Diamond.Positions[pos];
        for (var i = 0; i < 60 * 6 && live.Active; i++)
        {
            live.Apply(LivePlayCommand.Tick(Frame));
            if (!live.Active) break;
            var at = pos == live.GlovePos ? (live.GloveX, live.GloveZ) : live.Fielders.TryGetValue(pos, out var p) ? p : start;
            if (Diamond.Dist(at.Item1, at.Item2, start.X, start.Z) > 0.05) return live.ElapsedSeconds;
        }
        return double.PositiveInfinity;
    }

    /// <summary>
    /// A fly to CF at this rung. The human defense holds the stick toward the landing from the first frame; the
    /// CPU seat runs the same glove itself. Returns when the glove first moves.
    /// </summary>
    double StickMoveAt(string level, bool human)
    {
        var match = Match.Exhibition(_content, "rio", "ashlord", innings: 3, seed: 1, difficulty: level);
        Assert.True(match.BeginAtBat(Scenario.Paint, Scenario.Swing, out _, out _), "the scripted swing must put the ball in play");
        var hit = FlightFixtures.Landing(match.Park, 250, 34, 0);
        var preview = match.PreviewHit(hit);
        Assert.Equal("CF", preview.Position);
        var seats = human ? new LiveSeats(HumanBats: false, HumanPitches: true, PlayerMustField: false, Versus: false) : LiveSeats.CpuOnly;
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, seats)).Snapshot.Active);
        var start = (live.GloveX, live.GloveZ);
        var pad = human ? new LivePadInput(StickX: 0, StickY: -1) : LivePadInput.Dead;
        for (var i = 0; i < 60 * 4 && live.Active; i++)
        {
            live.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false, human ? LivePlayCommandSource.Human : LivePlayCommandSource.Cpu));
            if (Diamond.Dist(live.GloveX, live.GloveZ, start.Item1, start.Item2) > 0.05) return live.ElapsedSeconds;
        }
        return double.PositiveInfinity;
    }
}
