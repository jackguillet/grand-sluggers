using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-7 (#723): how a player commands a throw, and what two abilities do to it. The relay is player-owned —
/// the cutoff holds until commanded, an early press is remembered for 0.25 s and a fresh RB / period cancels
/// it — Laser is ×1.25 and only on a throw home with a live runner on third, and Snap Throw is a 0.22-s
/// release after a clean received teammate throw.
/// </summary>
public sealed class ThrowCommandTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    // ---------------------------------------------------------------------------------
    // The tables and the clock
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AutomaticCatchNeverCreatesAnUnrequestedOrUntargetedThrow(bool pressWithoutTarget)
    {
        var (match, _) = Defence(Game, centre: "moss", second: "marlow", shortstop: "frost");
        var hit = FlightFixtures.Landing(match.Park, 245, 34, 0, rules: match.Rules);
        var live = match.LivePlay;
        live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, match.PreviewHit(hit), null,
            HumanGlove, 0, LivePlayCommandSource.Human));
        var caught = false;
        for (var i = 0; i < 1800 && live.Active; i++)
        {
            var result = live.Apply(LivePlayCommand.Tick(Frame, new LivePadInput(SouthDown: pressWithoutTarget, ExplicitTarget: true),
                LivePadInput.Dead, false, LivePlayCommandSource.Human));
            caught |= live.Caught || result.CompletedPlay?.Outcome?.OutsMade.Any(o => o.Type == OutType.Catch) == true;
            Assert.False(live.Throwing);
            Assert.DoesNotContain(LiveEvent.ThrowCommitted, live.Events);
        }
        Assert.True(caught);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ExplicitThrowBeforePossessionUsesTheExistingBufferAndCancellation(bool cancel, bool tooEarly)
    {
        var (match, _) = Defence(Game, centre: "moss", second: "marlow", shortstop: "frost");
        Assert.True(match.StationRunner(3, Game.Must("konga")));
        var hit = FlightFixtures.Landing(match.Park, 245, 34, 0, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, HumanGlove, 0));
        // Measure actual glove possession, which follows the physical ball's reach envelope.
        var (baseline, _) = Defence(Game, centre: "moss", second: "marlow", shortstop: "frost");
        Assert.True(baseline.StationRunner(3, Game.Must("konga")));
        baseline.LivePlay.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit,
            baseline.PreviewHit(hit), null, HumanGlove, 0));
        while (baseline.LivePlay.Active && !baseline.LivePlay.Caught && baseline.LivePlay.ElapsedSeconds < 20)
            baseline.LivePlay.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false));
        Assert.True(baseline.LivePlay.Caught);
        var catchAt = baseline.LivePlay.ElapsedSeconds;
        var sent = false; var cancelled = false; var queued = false; var threw = false;
        for (var i = 0; i < 1000 && live.Active; i++)
        {
            var press = !sent && live.ElapsedSeconds >= catchAt - (tooEarly ? .5 : .1);
            var cancelNow = cancel && sent && !cancelled;
            live.Apply(LivePlayCommand.Tick(Frame,
                new LivePadInput(KeysBag: 4, SouthDown: press, Cancel: cancelNow, ExplicitTarget: true), LivePadInput.Dead, false));
            sent |= press; cancelled |= cancelNow;
            queued |= live.Events.Contains(LiveEvent.ThrowQueued);
            threw |= live.Events.Contains(LiveEvent.ThrowCommitted);
            if (live.ElapsedSeconds > catchAt + 1) break;
        }
        Assert.True(queued);
        Assert.Equal(!cancel && !tooEarly, threw);
    }

    [Fact]
    public void TheTablesCarryTheCommands()
    {
        var t = Game.Rules.Fielding;
        Assert.Equal((1.25, 1.0, 0.22), (t.Abilities.LaserMul, t.Abilities.SnapThrowMul, t.Abilities.SnapReleaseSec));
        Assert.Equal(0.25, t.Throw.RelayBufferSec);
        Assert.Equal(0.30, t.Throw.ReleaseSec);
        Assert.Contains(Scheme.Product, v => v.Id == "cancel-throw");
    }

    [Fact]
    public void AThrowCarriesItsOwnReleaseWhenItHasOne()
    {
        var r = Game.Rules;
        var flight = 80 / 88.89;
        Assert.Equal(0.30 + flight, InPlay.ThrowSec(80, new ThrowResult(Chemistry.Neutral, 1.0, false), r), 12);
        Assert.Equal(0.50 + flight, InPlay.ThrowSec(80, new ThrowResult(Chemistry.Neutral, 1.0, false, ReleaseSec: 0.5), r), 12);
        Assert.Equal(0.22 + flight, InPlay.ThrowSec(80, new ThrowResult(Chemistry.Neutral, 1.0, false, ReleaseSec: 0.22), r), 12);
    }

    // ---------------------------------------------------------------------------------
    // Laser, through the trace of a CPU play
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// hex (Laser) in centre, konga on third, a 245-ft fly. The boost rides only a throw home with the runner on
    /// third: a throw home carries ×1.25, a cutoff feed carries none.
    /// </summary>
    [Fact]
    public void LaserRidesOnlyTheThrowHomeWithARunnerOnThird()
    {
        var content = Game;
        var (match, bodies) = Defence(content, centre: "hex", second: "jester", shortstop: "marlow");
        var konga = content.Must("konga");
        Assert.True(match.StationRunner(3, konga));
        Assert.True(match.SetOuts(1));
        var hit = FlightFixtures.Landing(match.Park, 270, 34, 0, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("CF", preview.Position);
        var throws = RunCpu(match, hit, preview);
        var hex = content.Must("hex");
        var byCf = throws.Where(f => f.FromPos == "CF").ToList();
        Assert.NotEmpty(byCf);
        var a = match.Rules.Fielding.Abilities;
        foreach (var f in byCf)
        {
            var receiver = bodies[f.ReceiverPos];
            var chem = Pair(content, hex, receiver, match.Rules);
            var laser = f.Bag == 4 ? a.LaserMul : 1.0;
            Assert.Equal(InPlay.ArmMul(hex, match.Rules) * chem * laser, f.SpeedMul, 9);
        }
        // The boosted direct throw is the faster leg, so hex throws home direct.
        Assert.Equal(4, byCf[0].Bag);
        Assert.Equal(InPlay.ArmMul(hex, match.Rules) * Pair(content, hex, bodies["C"], match.Rules) * 1.25, byCf[0].SpeedMul, 9);
    }

    /// <summary>With nobody on third there is no throw Laser is for: hex's throws carry no boost.</summary>
    [Fact]
    public void LaserCarriesNothingWhenNobodyIsOnThird()
    {
        var content = Game;
        var (match, bodies) = Defence(content, centre: "hex", second: "jester", shortstop: "marlow");
        Assert.True(match.StationRunner(1, content.Must("dart")));
        var hit = FlightFixtures.Landing(match.Park, 230, 30, 0, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("CF", preview.Position);
        var throws = RunCpu(match, hit, preview);
        var hex = content.Must("hex");
        var byCf = throws.Where(f => f.FromPos == "CF").ToList();
        Assert.NotEmpty(byCf);
        foreach (var f in byCf)
        {
            var chem = Pair(content, hex, bodies[f.ReceiverPos], match.Rules);
            Assert.Equal(InPlay.ArmMul(hex, match.Rules) * chem, f.SpeedMul, 9);
        }
    }

    // ---------------------------------------------------------------------------------
    // The relay is player-owned; Snap Throw is a release
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// A human centre fielder (moss) arms home and throws to the cutoff; the cutoff is a Snap Throw holder (frost at
    /// short). What happens next is the case:
    /// no press — the cutoff holds for a full second, then a press throws home with the 0.22-s release;
    /// early press — a press 0.15 s before the catch is remembered and fires at the catch;
    /// late press — a press 0.5 s before the catch expires and the cutoff holds;
    /// cancel — a remembered press cancelled by a fresh RB is gone and the cutoff holds.
    /// </summary>
    [Theory]
    [InlineData("none")]
    [InlineData("early")]
    [InlineData("late")]
    [InlineData("cancel")]
    public void TheCutoffHoldsUntilCommandedAndSnapIsARelease(string press)
    {
        var content = Game;
        var (match, bodies) = Defence(content, centre: "moss", second: "marlow", shortstop: "frost");
        var konga = content.Must("konga");
        Assert.True(match.StationRunner(3, konga));
        Assert.True(match.SetOuts(1));
        var hit = FlightFixtures.Landing(match.Park, 270, 34, 0, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("CF", preview.Position);

        var live = match.LivePlay;
        live.Recording = true;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, HumanGlove, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        PlayEvent? play = null;
        var stage = 0;               // 0 waiting for the catch, 1 armed, 2 cutoff thrown, 3 the cutter holds, 4 commanded
        var pressed = false; var cancelled = false; var queuedSeen = false; var clearedSeen = false;
        double heldSince = -1;
        for (var i = 0; i < 60 * 30 && play is null; i++)
        {
            var pad = LivePadInput.Dead;
            if (stage == 0 && live.HoldsBall && live.GlovePos == "CF") { pad = new LivePadInput(KeysBag: 4); stage = 1; }
            else if (stage == 1) { pad = new LivePadInput(Cutoff: true); stage = 2; }
            else if (stage == 2 && live.Throwing && live.ThrowBag == 0)
            {
                var remaining = live.ThrowDur - live.ThrowT;
                if (!pressed && press == "early" && remaining <= 0.15) { pad = new LivePadInput(SouthDown: true); pressed = true; }
                else if (!pressed && press == "late" && remaining <= 0.50) { pad = new LivePadInput(SouthDown: true); pressed = true; }
                else if (!pressed && press == "cancel" && remaining <= 0.20) { pad = new LivePadInput(SouthDown: true); pressed = true; }
                else if (pressed && !cancelled && press == "cancel") { pad = new LivePadInput(Cancel: true); cancelled = true; }
            }
            else if (stage == 2 && live.HoldsBall && !live.Throwing) { stage = 3; heldSince = live.ElapsedSeconds; }
            else if (stage == 3 && press == "none" && live.ElapsedSeconds - heldSince >= 1.0 && live.HoldsBall && !live.Throwing)
            {
                pad = new LivePadInput(SouthDown: true); stage = 4;
            }
            var r = live.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false, LivePlayCommandSource.Human));
            if (live.Events.Contains(LiveEvent.ThrowQueued)) queuedSeen = true;
            if (live.Events.Contains(LiveEvent.ThrowQueueCleared)) clearedSeen = true;
            play = r.CompletedPlay;
        }
        Assert.NotNull(play);
        Assert.True(stage >= 2, $"{press}: the centre fielder never threw to the cutoff (stage {stage})");

        // Judged from the play's own trace: the feed, the cutter's reception, and the cutter's onward release if any.
        var marks = live.TakeTrace(play).Marks ?? [];
        var releases = marks.Where(m => m.Kind == PlayTraceMarkKind.ThrowRelease && m.Flight is not null).ToList();
        var feed = releases.First(m => m.Flight!.FromPos == "CF" && m.Flight.Bag == 0);
        var cutter = feed.Flight!.ReceiverPos;
        var reception = marks.First(m => m.Kind == PlayTraceMarkKind.Reception && m.Fielder == cutter && m.T >= feed.T);
        var catchAt = reception.T;
        var onwardMark = releases.FirstOrDefault(m => m.Flight!.FromPos == cutter && m.T >= catchAt - 1e-9);
        var onward = onwardMark?.Flight;
        var onwardAt = onwardMark?.T ?? double.NaN;

        switch (press)
        {
            case "none":
                Assert.False(queuedSeen);
                Assert.NotNull(onward);
                Assert.Equal(4, onward!.Bag);
                Assert.True(onwardAt - catchAt >= 1.0 - Frame, $"the cutoff threw on its own at +{onwardAt - catchAt:0.00} s");
                break;
            case "early":
                Assert.True(queuedSeen);
                Assert.False(clearedSeen);
                Assert.NotNull(onward);
                Assert.Equal(4, onward!.Bag);
                Assert.InRange(onwardAt - catchAt, match.Rules.Fielding.Abilities.SnapReleaseSec - Frame,
                    match.Rules.Fielding.Abilities.SnapReleaseSec + Frame + 1e-9);
                break;
            case "late":
            case "cancel":
                Assert.True(queuedSeen);
                Assert.True(clearedSeen);
                Assert.Null(onward);
                break;
        }

        if (onward is not null)
        {
            // Snap Throw: the cutter holds a clean received throw, so its release is the snap release — 0.22 against the
            // ordinary 0.30.
            var who = bodies[cutter];
            Assert.Equal("snap-throw", who.FieldAbility);
            var home = Diamond.Bag(4);
            var dist = Diamond.Dist(onward.FromX, onward.FromZ, home.X, home.Z);
            var snap = new ThrowResult(Chemistry.Neutral, onward.SpeedMul, false, Arm: who.Stats.Arm, ReleaseSec: match.Rules.Fielding.Abilities.SnapReleaseSec);
            var ordinary = snap with { ReleaseSec = null };
            Assert.Equal(InPlay.ThrowSec(dist, snap, match.Rules), onward.DurationSec + snap.ReleaseSec!.Value, 6);
            Assert.Equal(0.08, InPlay.ThrowSec(dist, ordinary, match.Rules) - (onward.DurationSec + snap.ReleaseSec.Value), 6);
        }
    }

    // ---------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------

    static double Pair(ContentCatalog content, Character from, Character to, RulesTable rules) =>
        content.Chemistry.Between(from, to) switch
        {
            Chemistry.Good => rules.Fielding.Chem.GoodSpeedMul,
            Chemistry.Bad => rules.Fielding.Chem.BadSpeedMul,
            _ => 1.0
        };

    /// <summary>The defence in glove order after the pitcher: vale P, pewter C, lace 1B, then the second baseman, grit 3B, the shortstop, basil LF, the centre fielder, gull RF.</summary>
    static (Match Match, Dictionary<string, Character> Bodies) Defence(ContentCatalog content, string centre, string second, string shortstop)
    {
        var home = content.Team("Defense", "vale", "pewter", "lace", second, "grit", shortstop, "basil", centre, "gull");
        var away = content.Team("Offense", "zig", "boom", "cinder", "soot", "nugget", "ashlord", "konga", "dart", "rio");
        var match = Match.Exhibition(content, home, away, 3, 1, parkId: ParkIds.Harbor);
        var bodies = new Dictionary<string, Character>(StringComparer.OrdinalIgnoreCase)
        {
            ["P"] = content.Must("vale"), ["C"] = content.Must("pewter"), ["1B"] = content.Must("lace"), ["2B"] = content.Must(second),
            ["3B"] = content.Must("grit"), ["SS"] = content.Must(shortstop), ["LF"] = content.Must("basil"), ["CF"] = content.Must(centre), ["RF"] = content.Must("gull")
        };
        return (match, bodies);
    }

    static List<PlayTraceThrow> RunCpu(Match match, AtBatResult hit, FieldingPreview preview)
    {
        var live = match.LivePlay;
        live.Recording = true;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 30 && play is null; i++)
            play = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
        Assert.NotNull(play);
        var marks = live.TakeTrace(play).Marks ?? [];
        return marks.Where(m => m.Kind == PlayTraceMarkKind.ThrowRelease && m.Flight is not null).Select(m => m.Flight!).ToList();
    }
}
