using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-4 slice 2 (#720: F693-02-grounded-air-catch-recoil): a hard batted ball caught in the air by a body on its feet costs the
/// same response as a hot ground pickup, off its own anchor pair — the ground pair would charge every liner. A dive, a jump or a
/// buddy leap is not a grounded catch; a routine fly stays a routine fly. With the airborne pair at 0 no catch in the air is
/// charged at all.
/// </summary>
public sealed class AirRecoilTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    [Fact]
    [Trait("Kind", "Balance")]
    public void TheAirbornePairIsItsOwn()
    {
        var t = Game.Rules.Fielding.Recoil;
        Assert.Equal((80.0, 115.0), (t.AirOnsetFtPerSec, t.AirFullFtPerSec));
        Assert.Equal((55.0, 75.0), (t.OnsetFtPerSec, t.FullFtPerSec));   // the ground pair does not move

        var r = Game.Rules;
        Character Hands(int h) => Game.Must("vale") with { Stats = Game.Must("vale").Stats with { Hands = h } };
        // A routine fly (36–41 ft/s, 0.59 vertical) is under both pairs; a median liner in the air (77) is at the ground pair's
        // cap and under the airborne onset — the one pair cannot serve both, so the air reads its own.
        Assert.Equal(0, FieldingResolver.RecoilSeverity(41, r, airborne: true));
        Assert.Equal(1, FieldingResolver.RecoilSeverity(77, r), 9);
        Assert.Equal(0, FieldingResolver.RecoilSeverity(77, r, airborne: true));
        Assert.Equal(0, FieldingResolver.RecoilSeverity(80, r, airborne: true));
        Assert.Equal(0.5, FieldingResolver.RecoilSeverity(97.5, r, airborne: true), 9);
        Assert.Equal(1, FieldingResolver.RecoilSeverity(115, r, airborne: true), 9);
        Assert.Equal(1, FieldingResolver.RecoilSeverity(200, r, airborne: true), 9);
        // The same response: 0.20 / 0.16 / 0.11 at full severity for Hands 1 / 5 / 10, halfway 0.10 / 0.08 / 0.055.
        Assert.Equal(0.20, FieldingResolver.RecoilSec(Hands(1), 115, r, airborne: true), 9);
        Assert.Equal(0.16, FieldingResolver.RecoilSec(Hands(5), 115, r, airborne: true), 9);
        Assert.Equal(0.11, FieldingResolver.RecoilSec(Hands(10), 115, r, airborne: true), 9);
        Assert.Equal(0.10, FieldingResolver.RecoilSec(Hands(1), 97.5, r, airborne: true), 9);
        Assert.Equal(0.055, FieldingResolver.RecoilSec(Hands(10), 97.5, r, airborne: true), 9);
    }

    /// <summary>An 80-mph Perfect liner at 10° into the hole reaches grit (SS, Hands 6) on his feet at 101 ft/s in 0.33 s: 0.20 × 0.594 × 0.75 = 0.089 s, the catch an out at the take, the same ball twice the same.</summary>
    [Fact]
    public void AHotLinerCaughtStandingCostsTheHandsAndTheOutStands()
    {
        var a = Drive(Game, 140, 4, -18, human: true);
        var b = Drive(Game, 140, 4, -18, human: true);
        Assert.Equal("SS", a.Pos);
        Assert.True(a.TakeAt < a.Hang, "the liner was caught in the air");
        Assert.False(a.Dive || a.Jump || a.Airborne, "a catch on his feet");
        Assert.InRange(a.Speed, 80.01, 115);
        Assert.Equal(FieldingResolver.RecoilSec(Game.Must("grit"), a.Speed, Game.Rules, airborne: true), a.Dur, 9);
        Assert.True(a.Dur > 0);
        Assert.Equal(1, a.Events);
        Assert.Equal(PlayKind.FlyOut, a.Play.Kind);
        Assert.True(a.Play.Outcome?.OutsMade.Count >= 1, "the catch is the out");
        Assert.Equal((a.Speed, a.Dur, a.TakeAt), (b.Speed, b.Dur, b.TakeAt));
        Assert.Equal(a.Marks, b.Marks);

    }

    /// <summary>A 112-mph rope at 12° reaches hex (RF, Hands 4) at 123 ft/s, past the full speed: the cap binds in the air too — 0.20 × 0.85 = 0.17 s.</summary>
    [Fact]
    [Trait("Kind", "Balance")]
    public void TheCapBindsInTheAir()
    {
        var run = Drive(Game, 112, 12, 24);
        Assert.Equal("RF", run.Pos);
        Assert.True(run.Speed >= 115, $"the rope arrived at {run.Speed:0.0} ft/s");
        Assert.Equal(0.17, run.Dur, 9);
        Assert.Equal(1, run.Events);
    }

    /// <summary>
    /// A 130-mph liner at 12° into right-centre: the right fielder commits, dives and takes it at 81 ft/s — above the airborne onset,
    /// and no recoil: a dive is not a grounded catch, and it pays its own 0.555 s instead.
    /// </summary>
    [Fact]
    public void ADivingCatchIsNotAGroundedOneAndPaysTheDiveInstead()
    {
        var run = Drive(Game, 160, 4, -24, human: true, playerDive: true);
        Assert.Equal("SS", run.Pos);
        Assert.True(run.Dive, "the right fielder dove");
        Assert.True(run.Speed > 80, $"the liner arrived at {run.Speed:0.0} ft/s, above the airborne onset");
        Assert.Equal(0, run.Dur);
        Assert.Equal(0, run.Events);
        Assert.Equal(FieldingResolver.DiveRecoverySec(Game.Must("grit"), Game.Rules), run.DiveCost, 6);
    }

    /// <summary>A 245-ft fly at 34° comes down to the centre fielder at 36–41 ft/s, more than half of it straight down: nothing.</summary>
    [Fact]
    public void ARoutineFlyCostsNothing()
    {
        var content = Game;
        var run = Drive(content, FlightFixtures.ExitForCarry(245, 34, content.Rules), 34, 0);
        Assert.True(run.TakeAt < run.Hang, $"the fly was taken at {run.TakeAt:0.00} of {run.Hang:0.00}");
        Assert.InRange(run.Speed, 30, 60);
        Assert.Equal((0.0, 0, 0), (run.Dur, run.Events, run.RecoilFrames));
        Assert.Equal(PlayKind.FlyOut, run.Play.Kind);
    }

    /// <summary>The human seat with the runner on first who must get back: the assistance takes the liner standing; from the take the stick is held and South pressed — the body is held for the recovery, the press is remembered, and the throw to first leaves at readiness, not before.</summary>
    [Fact]
    public void TheHumanSeatIsHeldAndTheThrowWaitsAfterAStandingCatch()
    {
        var (match, hit, preview) = Fixture(Game, 140, 4, -18);
        var live = match.LivePlay;
        live.Recording = true;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, HumanGlove, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        PlayEvent? play = null;
        var takeAt = -1.0; var dur = 0.0; var queued = false; (double X, double Z)? held = null; var maxMove = 0.0; var frames = 0;
        for (var i = 0; i < 60 * 12 && play is null; i++)
        {
            var pad = live.HoldsBall && !live.Throwing ? new LivePadInput(KeysBag: 1, SouthDown: true, StickX: 1.0) : LivePadInput.Dead;
            var r = live.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false, LivePlayCommandSource.Human));
            if (r.CompletedPlay is null)
            {
                if (live.Events.Contains(LiveEvent.ThrowQueued)) queued = true;
                if (takeAt < 0 && live.HoldsBall) { takeAt = live.ElapsedSeconds; dur = live.RecoilDur; held = (live.GloveX, live.GloveZ); }
                else if (held is { } h && live.ImpactRecoil && live.RecoilT > 0)
                {
                    frames++;
                    maxMove = Math.Max(maxMove, Diamond.Dist(h.X, h.Z, live.GloveX, live.GloveZ));
                }
            }
            play = r.CompletedPlay;
        }
        Assert.NotNull(play);
        Assert.True(takeAt > 0 && takeAt < preview.HangTimeSec, "the liner was taken in the air");
        Assert.True(dur > 0);
        Assert.True(frames >= Math.Floor(dur / Frame) - 1, $"the recovery held for {frames} frames");
        Assert.True(maxMove < 1.0, $"the stick moved the body {maxMove:0.00} ft inside the recovery");
        Assert.True(queued, "the press inside the buffer was remembered");
        var marks = live.TakeTrace(play).Marks ?? [];
        var release = marks.First(m => m.Kind == PlayTraceMarkKind.ThrowRelease);
        Assert.InRange(release.T, takeAt + dur + match.Rules.Fielding.Throw.ReleaseSec - Frame - 1e-9, takeAt + dur + match.Rules.Fielding.Throw.ReleaseSec + 2 * Frame + 1e-9);
    }

    // ---------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------

    /// <summary>Harbor, grit (Hands 6) at short, hex (Hands 4) in right, gull on first so the catch is not the completing frame.</summary>
    static (Match Match, AtBatResult Hit, FieldingPreview Preview) Fixture(ContentCatalog content, double exitMph, double launchDeg, double sprayDeg)
    {
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "grit", "vine", "moss", "hex");
        var away = content.Team("Offense", "rio", "boom", "cinder", "soot", "nugget", "nico", "gull", "marlow", "ashlord");
        var match = Match.Exhibition(content, home, away, 3, 1, parkId: "harbor-diamond");
        var hit = FlightFixtures.Hit(match.Park, exitMph, launchDeg, sprayDeg, ContactQuality.Perfect, rules: match.Rules);
        Assert.False(hit.Foul);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.HangTimeSec > 0);
        Assert.True(match.StationRunner(1, content.Must("gull")));
        return (match, hit, preview);
    }

    sealed record Run(PlayEvent Play, string Pos, double TakeAt, double Hang, double Speed, double Dur, bool Impact, bool Dive, bool Jump, bool Airborne,
        double DiveCost, int RecoilFrames, int Events, IReadOnlyList<(PlayTraceMarkKind Kind, double T)> Marks);

    static Run Drive(ContentCatalog content, double exitMph, double launchDeg, double sprayDeg, bool human = false, bool playerDive = false)
    {
        var (match, hit, preview) = Fixture(content, exitMph, launchDeg, sprayDeg);
        var live = match.LivePlay;
        live.Recording = true;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, human ? HumanGlove : LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        PlayEvent? play = null;
        var takeAt = -1.0; var speed = 0.0; var dur = 0.0; var impact = false; var dive = false; var jump = false; var airborne = false; var diveCost = 0.0; var pos = "";
        var frames = 0; var events = 0;
        for (var i = 0; i < 60 * 15 && play is null; i++)
        {
            var pad = human && live.HoldsBall ? new LivePadInput(KeysBag: 1, SouthDown: true) : LivePadInput.Dead;
            if (playerDive && !live.HoldsBall && live.DiveT <= 0 && live.DiveRecoveryT <= 0 && live.ElapsedSeconds >= .7)
                pad = new LivePadInput(EastDown: true);
            var r = live.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false, LivePlayCommandSource.Cpu));
            play = r.CompletedPlay;
            if (play is not null) break;
            if (live.Events.Contains(LiveEvent.ImpactRecoil)) events++;
            diveCost = Math.Max(diveCost, live.DiveRecoveryT);
            if (takeAt < 0 && live.HoldsBall)
            {
                takeAt = live.ElapsedSeconds;
                speed = live.IncomingFtPerSec;
                dur = live.RecoilDur;
                impact = live.ImpactRecoil;
                dive = live.CatchDive;
                jump = live.CatchJump;
                airborne = live.Airborne;
                pos = live.GlovePos;
            }
            if (live.RecoilT > 0) frames++;
        }
        Assert.NotNull(play);
        Assert.True(takeAt > 0, "nobody took the ball");
        var marks = (live.TakeTrace(play).Marks ?? []).Select(m => (m.Kind, m.T)).ToList();
        return new Run(play, pos, takeAt, preview.HangTimeSec, speed, dur, impact, dive, jump, airborne, diveCost, frames, events, marks);
    }
}
