using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-7 (#723): how a player commands a throw, and what two abilities do to it. Under the <c>c80</c> copy the
/// relay is player-owned — the cutoff holds until commanded, an early press is remembered for 0.25 s and a
/// fresh RB / period cancels it — Laser is ×1.25 and only on a throw home with a live runner on third, and
/// Snap Throw is a 0.22-s release after a clean received teammate throw. The shipped table keeps the rules
/// the game shipped with: the armed onward leg thrown for the player, Laser ×1.45 everywhere, Snap ×1.22, a
/// snap release equal to the ordinary one.
/// </summary>
public sealed class ThrowCommandTests
{
    static readonly ContentCatalog Control = ContentCatalog.Load();
    static readonly DataRoot Root = new(Control.Root.Shipped, Path.GetFullPath(Path.Combine(Control.Root.Shipped, "..", "trials", "c80")));
    static readonly ContentCatalog Trial = ContentCatalog.Load(Root);
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    // ---------------------------------------------------------------------------------
    // The tables and the clock
    // ---------------------------------------------------------------------------------

    [Fact]
    public void TheTablesCarryTheCommands()
    {
        var s = Control.Rules.Fielding;
        Assert.Equal((1.45, 1.22, 0.22, 0.0), (s.Abilities.LaserMul, s.Abilities.SnapThrowMul, s.Abilities.SnapReleaseSec, s.Abilities.LaserHomeOnly));
        Assert.Equal((1.0, 0.0), (s.Throw.RelayAutoContinue, s.Throw.RelayBufferSec));
        Assert.Equal(s.Throw.ReleaseSec, s.Abilities.SnapReleaseSec);   // inert on the shipped table by construction

        var t = Trial.Rules.Fielding;
        Assert.Equal((1.25, 1.0, 0.22, 1.0), (t.Abilities.LaserMul, t.Abilities.SnapThrowMul, t.Abilities.SnapReleaseSec, t.Abilities.LaserHomeOnly));
        Assert.Equal((0.0, 0.25), (t.Throw.RelayAutoContinue, t.Throw.RelayBufferSec));
        Assert.Equal(0.30, t.Throw.ReleaseSec);
        Assert.Contains(Scheme.Product, v => v.Id == "cancel-throw");
    }

    [Fact]
    public void AThrowCarriesItsOwnReleaseWhenItHasOne()
    {
        var r = Control.Rules;
        Assert.Equal(1.02, InPlay.ThrowSec(80, new ThrowResult(Chemistry.Neutral, 1.0, false), r), 12);
        Assert.Equal(1.30, InPlay.ThrowSec(80, new ThrowResult(Chemistry.Neutral, 1.0, false, ReleaseSec: 0.5), r), 12);
        Assert.Equal(1.02, InPlay.ThrowSec(80, new ThrowResult(Chemistry.Neutral, 1.0, false, ReleaseSec: 0.22), r), 12);
    }

    // ---------------------------------------------------------------------------------
    // Laser, through the trace of a CPU play
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// hex (Laser) in centre, konga on third, a 245-ft fly. On the trial the boost rides only a throw home with the
    /// runner on third: a throw home carries ×1.25, a cutoff feed carries none. On the control every throw hex
    /// makes carries ×1.45, the cutoff feed the forced relay makes him throw included.
    /// </summary>
    [Theory]
    [InlineData("control")]
    [InlineData("trial")]
    public void LaserRidesOnlyTheThrowHomeWithARunnerOnThirdUnderTheTrial(string root)
    {
        var content = root == "trial" ? Trial : Control;
        var (match, bodies) = Game(content, centre: "hex", second: "jester", shortstop: "marlow");
        var konga = content.Must("konga");
        Assert.True(match.StationRunner(3, konga));
        Assert.True(match.SetOuts(1));
        var hit = FlightFixtures.Landing(match.Park, 245, 34, 0, rules: match.Rules);
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
            var laser = root == "control" ? a.LaserMul : f.Bag == 4 ? a.LaserMul : 1.0;
            Assert.Equal(InPlay.ArmMul(hex, match.Rules) * chem * laser, f.SpeedMul, 9);
        }
        if (root == "trial")
        {
            // On the trial the boosted direct throw is the faster leg, so hex throws home direct.
            Assert.Equal(4, byCf[0].Bag);
            Assert.Equal(InPlay.ArmMul(hex, match.Rules) * Pair(content, hex, bodies["C"], match.Rules) * 1.25, byCf[0].SpeedMul, 9);
        }
        else
        {
            Assert.Equal(0, byCf[0].Bag);   // 245 ft is past the shipped ceiling: through the cutoff, boost and all
        }
    }

    /// <summary>With nobody on third there is no throw Laser is for: hex's throws carry no boost on the trial, every boost on the control.</summary>
    [Theory]
    [InlineData("control")]
    [InlineData("trial")]
    public void LaserCarriesNothingWhenNobodyIsOnThirdUnderTheTrial(string root)
    {
        var content = root == "trial" ? Trial : Control;
        var (match, bodies) = Game(content, centre: "hex", second: "jester", shortstop: "marlow");
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
            var laser = root == "control" ? match.Rules.Fielding.Abilities.LaserMul : 1.0;
            Assert.Equal(InPlay.ArmMul(hex, match.Rules) * chem * laser, f.SpeedMul, 9);
        }
    }

    // ---------------------------------------------------------------------------------
    // The relay is player-owned; Snap Throw is a release
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// A human centre fielder (moss) arms home and throws to the cutoff; the cutoff is a Snap Throw holder (frost at
    /// short — pip carried the ability until the c80 roster gave him Ball Dash, #718). What happens next is the case:
    /// control — the armed onward leg fires for the player at the catch, and a press in flight is nothing;
    /// trial, no press — the cutoff holds for a full second, then a press throws home with the 0.22-s release;
    /// trial, early press — a press 0.15 s before the catch is remembered and fires at the catch;
    /// trial, late press — a press 0.5 s before the catch expires and the cutoff holds;
    /// trial, cancel — a remembered press cancelled by a fresh RB is gone and the cutoff holds.
    /// </summary>
    [Theory]
    [InlineData("control", "none")]
    [InlineData("control", "early")]
    [InlineData("trial", "none")]
    [InlineData("trial", "early")]
    [InlineData("trial", "late")]
    [InlineData("trial", "cancel")]
    public void TheCutoffHoldsUntilCommandedUnderTheTrialAndSnapIsARelease(string root, string press)
    {
        var content = root == "trial" ? Trial : Control;
        var (match, bodies) = Game(content, centre: "moss", second: "marlow", shortstop: "frost");
        var konga = content.Must("konga");
        Assert.True(match.StationRunner(3, konga));
        Assert.True(match.SetOuts(1));
        var hit = FlightFixtures.Landing(match.Park, 245, 34, 0, rules: match.Rules);
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
        Assert.True(stage >= 2, $"{root} {press}: the centre fielder never threw to the cutoff (stage {stage})");

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

        switch (root, press)
        {
            case ("control", _):
                // The armed leg is thrown for the player at the catch; a press in flight is nothing on this table.
                Assert.False(queuedSeen);
                Assert.NotNull(onward);
                Assert.Equal(4, onward!.Bag);
                Assert.InRange(onwardAt - catchAt, 0, Frame + 1e-9);
                break;
            case ("trial", "none"):
                Assert.False(queuedSeen);
                Assert.NotNull(onward);
                Assert.Equal(4, onward!.Bag);
                Assert.True(onwardAt - catchAt >= 1.0 - Frame, $"the cutoff threw on its own at +{onwardAt - catchAt:0.00} s");
                break;
            case ("trial", "early"):
                Assert.True(queuedSeen);
                Assert.False(clearedSeen);
                Assert.NotNull(onward);
                Assert.Equal(4, onward!.Bag);
                Assert.InRange(onwardAt - catchAt, 0, Frame + 1e-9);
                break;
            case ("trial", "late"):
            case ("trial", "cancel"):
                Assert.True(queuedSeen);
                Assert.True(clearedSeen);
                Assert.Null(onward);
                break;
        }

        if (onward is not null)
        {
            // Snap Throw: the cutter holds a clean received throw, so its release is the snap release — 0.22 against the
            // trial's 0.30, and equal to the shipped release on the control, where it changes nothing.
            var who = bodies[cutter];
            Assert.Equal("snap-throw", who.FieldAbility);
            var home = Diamond.Bag(4);
            var dist = Diamond.Dist(onward.FromX, onward.FromZ, home.X, home.Z);
            var snap = new ThrowResult(Chemistry.Neutral, onward.SpeedMul, false, Arm: who.Stats.Arm, ReleaseSec: match.Rules.Fielding.Abilities.SnapReleaseSec);
            var ordinary = snap with { ReleaseSec = null };
            Assert.Equal(InPlay.ThrowSec(dist, snap, match.Rules), onward.DurationSec, 6);
            if (root == "trial") Assert.Equal(0.08, InPlay.ThrowSec(dist, ordinary, match.Rules) - onward.DurationSec, 6);
            else Assert.Equal(InPlay.ThrowSec(dist, ordinary, match.Rules), onward.DurationSec, 6);
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
    static (Match Match, Dictionary<string, Character> Bodies) Game(ContentCatalog content, string centre, string second, string shortstop)
    {
        var home = content.Team("Defense", "vale", "pewter", "lace", second, "grit", shortstop, "basil", centre, "gull");
        var away = content.Team("Offense", "zig", "boom", "cinder", "soot", "nugget", "ashlord", "konga", "dart", "rio");
        var match = Match.Exhibition(content, home, away, 3, 1, parkId: "harbor-diamond");
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
