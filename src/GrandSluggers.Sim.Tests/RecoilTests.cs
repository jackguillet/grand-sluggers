using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-4 slice 1 (#720: F693-02-clean-ground-pickup-readiness, -ground-pickup-recoil-basis, -ground-pickup-recoil-cap,
/// -recoil-field-shaping, -recoil-field-factors, -recoil-severity-curve, -ordinary-recoil-actions, -ordinary-recoil-displacement,
/// -ordinary-recoil-distance-cap, -ordinary-recoil-motion-profile): what the ball costs the hands that take it off the ground. On
/// the <c>c80</c> copy the cost is a pure function of the ball's actual incoming speed and the body's Hands — the same ball to the
/// same hands costs the same every time, a routine arrival costs nothing, the cap and the one-foot skid both bind — and the world
/// goes on while the body recovers. The shipped table keeps the energy knockback the game shipped with.
/// </summary>
public sealed class RecoilTests
{
    static readonly ContentCatalog Control = ContentCatalog.Load();
    static readonly string TrialDir = Path.GetFullPath(Path.Combine(Control.Root.Shipped, "..", "trials", "c80"));
    static readonly ContentCatalog Trial = ContentCatalog.Load(new DataRoot(Control.Root.Shipped, TrialDir));
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    [Fact]
    public void TheShippedTableKeepsTheKnockbackAndTheTrialReadsTheBallsSpeed()
    {
        var s = Control.Rules.Fielding.Recoil;
        Assert.Equal((0.0, 0.0, 0.20, 0.05, 10.0), (s.OnsetFtPerSec, s.FullFtPerSec, s.CapSec, s.HandsCutPerPoint, s.KickFtPerSec));
        Assert.False(s.Active);
        var t = Trial.Rules.Fielding.Recoil;
        Assert.Equal((55.0, 75.0, 0.20, 0.05, 10.0), (t.OnsetFtPerSec, t.FullFtPerSec, t.CapSec, t.HandsCutPerPoint, t.KickFtPerSec));
        Assert.True(t.Active);
        // The knockback block is the same in both roots: the shipped rule, not read while the recoil is on.
        var sk = Control.Rules.Fielding.Knockback;
        var tk = Trial.Rules.Fielding.Knockback;
        Assert.Equal((sk.MinEnergy, sk.SecPerFieldDeficit, sk.EnergySpan, sk.MaxSec, sk.MinSec), (tk.MinEnergy, tk.SecPerFieldDeficit, tk.EnergySpan, tk.MaxSec, tk.MinSec));
        Assert.Equal((72.0, 0.55), (sk.MinEnergy, sk.MaxSec));
    }

    /// <summary>The curve (F693-02-recoil-severity-curve), the hands (F693-02-recoil-field-factors), the shaping (bounded severity first) and both caps, as numbers.</summary>
    [Fact]
    public void TheCostIsAPureFunctionOfIncomingSpeedAndHands()
    {
        var r = Trial.Rules;
        Character Hands(int h) => Trial.Must("vale") with { Stats = Trial.Must("vale").Stats with { Hands = h } };

        // S: zero through the onset, linear to the full speed, one past it.
        Assert.Equal(0, FieldingResolver.RecoilSeverity(40, r));
        Assert.Equal(0, FieldingResolver.RecoilSeverity(55, r));
        Assert.Equal(0.25, FieldingResolver.RecoilSeverity(60, r), 9);
        Assert.Equal(0.5, FieldingResolver.RecoilSeverity(65, r), 9);
        Assert.Equal(1, FieldingResolver.RecoilSeverity(75, r), 9);
        Assert.Equal(1, FieldingResolver.RecoilSeverity(200, r), 9);
        // F: five points per Hands point above 1.
        Assert.Equal(1.0, FieldingResolver.RecoilHandsFactor(Hands(1), r), 9);
        Assert.Equal(0.80, FieldingResolver.RecoilHandsFactor(Hands(5), r), 9);
        Assert.Equal(0.55, FieldingResolver.RecoilHandsFactor(Hands(10), r), 9);
        // Full severity: 0.20 / 0.16 / 0.11 s — the cap binds at Hands 1 and the hands still tell at the cap.
        Assert.Equal(0.20, FieldingResolver.RecoilSec(Hands(1), 75, r), 9);
        Assert.Equal(0.16, FieldingResolver.RecoilSec(Hands(5), 75, r), 9);
        Assert.Equal(0.11, FieldingResolver.RecoilSec(Hands(10), 75, r), 9);
        Assert.Equal(FieldingResolver.RecoilSec(Hands(1), 75, r), FieldingResolver.RecoilSec(Hands(1), 300, r), 9);
        // Halfway through the band: 0.10 / 0.08 / 0.055.
        Assert.Equal(0.10, FieldingResolver.RecoilSec(Hands(1), 65, r), 9);
        Assert.Equal(0.08, FieldingResolver.RecoilSec(Hands(5), 65, r), 9);
        Assert.Equal(0.055, FieldingResolver.RecoilSec(Hands(10), 65, r), 9);
        // The skid is w²: one foot at most, and less for better hands.
        Assert.Equal(1.0, FieldingResolver.RecoilSkidFt(1, r), 9);
        Assert.Equal(0.64, FieldingResolver.RecoilSkidFt(0.8, r), 9);
        Assert.Equal(0.3025, FieldingResolver.RecoilSkidFt(0.55, r), 9);
        Assert.Equal(10, FieldingResolver.RecoilKickFtPerSec(1, r), 9);
        // Monotone both ways: a slower ball never costs more, better hands never pay more.
        for (var v = 0; v < 120; v += 5)
        {
            Assert.True(FieldingResolver.RecoilSec(Hands(3), v, r) <= FieldingResolver.RecoilSec(Hands(3), v + 5, r) + 1e-12);
            Assert.True(FieldingResolver.RecoilSec(Hands(7), v, r) <= FieldingResolver.RecoilSec(Hands(3), v, r) + 1e-12);
        }
        // Nothing on the shipped table, at any speed, for any hands.
        Assert.Equal(0, FieldingResolver.RecoilSec(Control.Must("vale"), 300, Control.Rules));
        Assert.Equal(0, FieldingResolver.RecoilSeverity(300, Control.Rules));
    }

    /// <summary>
    /// A 70-mph Nice grounder back to the mound arrives at 48 ft/s, under the onset: the trial's pitcher pays nothing — no clock,
    /// no event, no skid — and the play's marks are the same, to the frame, as under a copy with the recoil off (where the shipped
    /// knockback charges this ball nothing either). The clean path is untouched.
    /// </summary>
    [Fact]
    public void ARoutinePickupAddsZeroFrames()
    {
        var on = RunCpu(Trial, 70, 8, 0, ContactQuality.Nice);
        Assert.Equal(0, on.RecoilFrames);
        Assert.Equal(0, on.Events);
        Assert.Equal(0, on.Dur);
        Assert.InRange(on.Speed, 40, 54.9);
        Assert.Equal(0, InPlay.KnockbackSec(InPlay.Energy(on.Hit, Trial.Rules), Trial.Must("vale"), Trial.Rules));

        using var legacy = new PatchedTrial(text => text.Replace("\"onsetFtPerSec\": 55", "\"onsetFtPerSec\": 0").Replace("\"fullFtPerSec\": 75", "\"fullFtPerSec\": 0"));
        var off = RunCpu(legacy.Content, 70, 8, 0, ContactQuality.Nice);
        Assert.False(legacy.Content.Rules.Fielding.Recoil.Active);
        Assert.Equal(off.Marks, on.Marks);
        Assert.Equal(off.Play.Kind, on.Play.Kind);
    }

    /// <summary>A routine fly stays a routine fly: the slice charges no airborne catch at all.</summary>
    [Fact]
    public void ARoutineCatchAddsZeroFrames()
    {
        var (match, hit, preview) = Fixture(Trial, FlightFixtures.ExitForCarry(245, 34, Trial.Rules), 34, 0, ContactQuality.Nice);
        Assert.False(preview.Grounder);
        var run = Drive(match, hit, preview);
        Assert.Equal(PlayKind.FlyOut, run.Play.Kind);
        Assert.Equal(0, run.RecoilFrames);
        Assert.Equal(0, run.Events);
        Assert.Equal(0, run.Dur);
    }

    /// <summary>
    /// A 125-mph Perfect comebacker at 2° reaches the mound in half a second at 91 ft/s — past the full speed. vale (Hands 8) pays
    /// 0.20 × 0.65 = 0.13 s and skids 0.65² = 0.42 ft along the ball's travel; the same ball twice costs the same to the frame and
    /// the foot; the shipped table stops him by the contact's energy instead, the way it always did.
    /// </summary>
    [Fact]
    public void TheComebackerCostsThePitcherWhatItsSpeedSaysAndTheSameTwice()
    {
        var a = RunCpu(Trial, 125, 2, 0, ContactQuality.Perfect);
        var b = RunCpu(Trial, 125, 2, 0, ContactQuality.Perfect);
        Assert.Equal("P", a.Pos);
        Assert.True(a.Speed >= 75, $"the comebacker arrived at {a.Speed:0.0} ft/s");
        Assert.Equal(0.13, a.Dur, 9);
        Assert.Equal(1, a.Events);
        Assert.Equal(FieldingResolver.RecoilSec(Trial.Must("vale"), a.Speed, Trial.Rules), a.Dur, 9);
        Assert.InRange(a.RecoilFrames, 7, 9);   // 0.13 s at 60 Hz
        Assert.InRange(a.Skid, 0.4225 - 1e-6, 0.4225 + 1e-6);   // w² exactly: the body was standing when the ball came
        Assert.Equal(PlayKind.GroundOut, a.Play.Kind);

        Assert.Equal(a.Speed, b.Speed);
        Assert.Equal(a.Dur, b.Dur);
        Assert.Equal(a.Skid, b.Skid);
        Assert.Equal(a.TakeAt, b.TakeAt);
        Assert.Equal(a.Marks, b.Marks);

        var shipped = RunCpu(Control, 125, 2, 0, ContactQuality.Perfect);
        Assert.Equal("P", shipped.Pos);
        Assert.False(shipped.Impact, "the shipped table has no impact recoil");
        Assert.Equal(0, shipped.Dur);
        Assert.Equal(0, shipped.Events);
        var knock = InPlay.KnockbackSec(InPlay.Energy(shipped.Hit, Control.Rules), Control.Must("vale"), Control.Rules);
        Assert.True(knock > Control.Rules.Fielding.Knockback.MinSec);
        Assert.Equal(knock, shipped.RecoilAtTake, 6);   // the knockback clock, set at the take and counted down from the next tick
        Assert.True(shipped.Speed >= 75, "the ball's speed is sampled on both tables");
    }

    /// <summary>The same rocket to authored hands: Hands 1 pays the whole 0.20 s and skids the whole foot; Hands 10 pays 0.11 s and 0.30 ft — the caps bind and the hands still tell there (F693-02-recoil-field-shaping).</summary>
    [Theory]
    [InlineData(1, 0.20, 1.0)]
    [InlineData(10, 0.11, 0.3025)]
    public void BothCapsBindOnARocketAndTheHandsStillTellAtTheCap(int hands, double sec, double skidFt)
    {
        var run = RunCpu(Trial, 125, 2, 0, ContactQuality.Perfect, pitcherHands: hands);
        Assert.True(run.Speed >= 75);
        Assert.Equal(sec, run.Dur, 9);
        Assert.InRange(run.Skid, skidFt - 1e-6, skidFt + 1e-6);
        Assert.Equal(1, run.Events);
    }

    /// <summary>
    /// A 130-mph liner at 10° into right lands at 1.79 s and skids to hex (Hands 4) at 63 ft/s: a ground pickup, so the trial charges
    /// it what its speed says even though no bobble is ever rolled on a liner; the shipped table, which charged grounders alone,
    /// charges nothing. (#720 recorded this on a 125-mph / 22° ball with the outfield's air multiplier at 1.0; at 0.6 the right
    /// fielder is elsewhere when that one lands, and this ball is the same play.)
    /// </summary>
    [Fact]
    public void ALandedLinerPickedUpOffTheGrassCostsOnTheTrialAndNothingShipped()
    {
        var trial = RunCpu(Trial, 130, 10, 25, ContactQuality.Perfect);
        Assert.Equal("RF", trial.Pos);
        Assert.True(trial.TakeAt > trial.Hang, "the liner landed before the take");
        Assert.InRange(trial.Speed, 55.01, 75);
        Assert.Equal(1, trial.Events);
        Assert.Equal(FieldingResolver.RecoilSec(Trial.Must("hex"), trial.Speed, Trial.Rules), trial.Dur, 9);
        Assert.True(trial.Dur > 0.05);

        var shipped = RunCpu(Control, 130, 10, 25, ContactQuality.Perfect);
        Assert.Equal(0, shipped.RecoilFrames);
        Assert.Equal(0, shipped.Dur);
        Assert.Equal(0, shipped.Events);
    }

    /// <summary>
    /// The human seat, a runner on first: the assistance brings the pitcher to the comebacker; from the take the stick is held
    /// and South pressed. The body does not walk for the 0.13 s (the skid and the brake's drift are under a foot; a walk is 2 ft
    /// and more), the press is remembered (it lands inside the 0.25 s buffer) and the throw leaves at readiness — the take plus
    /// the cost, not before.
    /// </summary>
    [Fact]
    public void TheHumanSeatIsHeldAndTheThrowWaitsForTheRecovery()
    {
        var (match, hit, preview) = Fixture(Trial, 125, 2, 0, ContactQuality.Perfect);
        Assert.True(match.StationRunner(1, Trial.Must("gull")));
        var live = match.LivePlay;
        live.Recording = true;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, HumanGlove, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        PlayEvent? play = null;
        var takeAt = -1.0; var dur = 0.0; var queued = false; (double X, double Z)? held = null; var maxMove = 0.0; var frames = 0;
        for (var i = 0; i < 60 * 12 && play is null; i++)
        {
            var pad = live.HoldsBall && !live.Throwing ? new LivePadInput(KeysBag: 2, SouthDown: true, StickX: 1.0) : LivePadInput.Dead;
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
        Assert.True(takeAt > 0, "the pitcher never took the ball");
        Assert.Equal(0.13, dur, 9);
        Assert.True(frames >= 6, $"the recovery held for {frames} frames");
        Assert.True(maxMove < 1.0, $"the stick moved the body {maxMove:0.00} ft inside the recovery");
        Assert.True(queued, "the press inside the buffer was remembered");
        var marks = live.TakeTrace(play).Marks ?? [];
        var release = marks.First(m => m.Kind == PlayTraceMarkKind.ThrowRelease);
        Assert.InRange(release.T, takeAt + dur - Frame - 1e-9, takeAt + dur + 2 * Frame + 1e-9);
    }

    // ---------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------

    /// <summary>Harbor, vale (Hands 8) on the mound, hex (Hands 4) in right; a contact judged by the flight.</summary>
    static (Match Match, AtBatResult Hit, FieldingPreview Preview) Fixture(ContentCatalog content, double exitMph, double launchDeg, double sprayDeg, ContactQuality quality, int pitcherHands = 0)
    {
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "grit", "vine", "moss", "hex");
        if (pitcherHands > 0)
        {
            var p = home.Captain with { Stats = home.Captain.Stats with { Hands = pitcherHands } };
            home = home with { Captain = p, Roster = home.Roster.Select(c => c.Id == p.Id ? p : c).ToList() };
        }
        var away = content.Team("Offense", "rio", "boom", "cinder", "soot", "nugget", "nico", "gull", "marlow", "ashlord");
        var match = Match.Exhibition(content, home, away, 3, 1, parkId: "harbor-diamond");
        var hit = FlightFixtures.Hit(match.Park, exitMph, launchDeg, sprayDeg, quality, rules: match.Rules);
        Assert.False(hit.Foul);
        var preview = match.PreviewHit(hit);
        return (match, hit, preview);
    }

    sealed record Run(PlayEvent Play, AtBatResult Hit, string Pos, double TakeAt, double Hang, double Speed, double Dur, double RecoilAtTake, bool Impact,
        int RecoilFrames, int Events, double Skid, IReadOnlyList<(PlayTraceMarkKind Kind, double T)> Marks);

    static Run RunCpu(ContentCatalog content, double exitMph, double launchDeg, double sprayDeg, ContactQuality quality, int pitcherHands = 0)
    {
        var (match, hit, preview) = Fixture(content, exitMph, launchDeg, sprayDeg, quality, pitcherHands);
        return Drive(match, hit, preview);
    }

    /// <summary>The CPU plays the ball; the run records the take, its cost and clock, and how far the body went while it recovered.</summary>
    static Run Drive(Match match, AtBatResult hit, FieldingPreview preview)
    {
        var live = match.LivePlay;
        live.Recording = true;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        PlayEvent? play = null;
        var takeAt = -1.0; var speed = 0.0; var dur = 0.0; var recoilAtTake = 0.0; var impact = false; var pos = "";
        var frames = 0; var events = 0; var skid = 0.0; (double X, double Z)? took = null; var wasRecovering = false;
        for (var i = 0; i < 60 * 15 && play is null; i++)
        {
            var r = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu));
            play = r.CompletedPlay;
            if (play is not null) break;
            if (live.Events.Contains(LiveEvent.ImpactRecoil)) events++;
            var recovering = live.ImpactRecoil && live.RecoilT > 0;
            if (takeAt < 0 && live.HoldsBall)
            {
                takeAt = live.ElapsedSeconds;
                speed = live.IncomingFtPerSec;
                dur = live.RecoilDur;
                recoilAtTake = live.RecoilT;
                impact = live.ImpactRecoil;
                pos = live.GlovePos;
                took = (live.GloveX, live.GloveZ);
            }
            if (live.RecoilT > 0) frames++;
            // The skid is read through the recovery and on the frame it ends — the last frame the kick integrates.
            if (took is { } t && (recovering || wasRecovering)) skid = Math.Max(skid, Diamond.Dist(t.X, t.Z, live.GloveX, live.GloveZ));
            wasRecovering = recovering;
        }
        Assert.NotNull(play);
        var marks = (live.TakeTrace(play).Marks ?? []).Select(m => (m.Kind, m.T)).ToList();
        return new Run(play, hit, pos, takeAt, preview.HangTimeSec, speed, dur, recoilAtTake, impact, frames, events, skid, marks);
    }

    /// <summary>The c80 copy with one of its files changed, laid over the shipped root — the split run, in a test.</summary>
    sealed class PatchedTrial : IDisposable
    {
        readonly string _dir;

        public PatchedTrial(Func<string, string> fielding)
        {
            _dir = Path.Combine(Path.GetTempPath(), "grand-sluggers-recoil-" + Guid.NewGuid().ToString("N"));
            foreach (var file in Directory.GetFiles(Control.Root.Shipped, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(Control.Root.Shipped, file);
                var source = Path.Combine(TrialDir, relative);
                if (!File.Exists(source)) continue;
                var to = Path.Combine(_dir, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(to)!);
                var text = File.ReadAllText(source);
                if (relative.Replace('\\', '/') == "rules/fielding.json") text = fielding(text);
                File.WriteAllText(to, text);
            }
            Content = ContentCatalog.Load(new DataRoot(Control.Root.Shipped, _dir));
        }

        public ContentCatalog Content { get; }

        public void Dispose() => Directory.Delete(_dir, recursive: true);
    }
}
