using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-5 slice 1 (#721: F693-02-handling-error-opportunities, -ordinary-handling-error-chance, -ordinary-handling-error-cap,
/// -ordinary-handling-chance-curve, -awkward-hop-difficulty-source, -ordinary-bobble-outcome, -bobble-stun, -bobble-stun-duration,
/// -bobble-recovery-reliability, -bobble-direction-spread, -uniform-error-direction, -local-bobble-*): where an ordinary play is
/// allowed to go wrong. On the <c>c80</c> copy a legal routine pickup never rolls; the awkward in-between hop is the one difficulty,
/// its chance the accepted curve off the ball and the hands, capped at 10 %; a failed take is a local bobble with a 0.40-s stun and
/// a recovery that never rolls again. The shipped table keeps the energy roll and the whole-tick fumble the game shipped with.
/// </summary>
public sealed class HandlingErrorTests
{
    static readonly ContentCatalog Control = ContentCatalog.Load();
    static readonly string TrialDir = Path.GetFullPath(Path.Combine(Control.Root.Shipped, "..", "trials", "c80"));
    static readonly ContentCatalog Trial = ContentCatalog.Load(new DataRoot(Control.Root.Shipped, TrialDir));
    const double Frame = 1.0 / 60.0;
    const double G = 32.174;

    [Fact]
    public void TheShippedTableRollsOnEnergyAndTheTrialOnlyOnTheAwkwardHop()
    {
        var s = Control.Rules.Fielding.Handling;
        Assert.Equal(0, s.AwkwardHop);
        Assert.False(s.Active);
        var t = Trial.Rules.Fielding.Handling;
        Assert.Equal(1, t.AwkwardHop);
        Assert.True(t.Active);
        // The accepted anchors, the same in both roots.
        foreach (var h in new[] { s, t })
        {
            Assert.Equal((0.10, 0.80, 0.5, 1.5, 0.35, 0.40), (h.ChanceCap, h.HandsCut, h.HopMinApexFt, h.HopFullApexFt, h.HopPhaseHalfWidth, h.StunSec));
            Assert.Equal((30.0, 0.20, 6.0, 0.35, 0.50, 0.25, 0.90, 6.0), (h.BobbleSpreadDeg, h.BobbleRetain, h.BobbleCapFtPerSec, h.BobbleRestitution, h.BobbleReboundCapFt, h.BobbleSettleFt, h.BobbleGroundRetain, h.BobbleDecelFtPerSec2));
        }
        // The shipped bobble block is untouched in both: the energy roll and the 0.58-s fumble are what the shipped table plays.
        Assert.Equal((78.0, 0.5, 0.58), (Trial.Rules.Fielding.Bobble.MinEnergy, Trial.Rules.Fielding.Bobble.MaxChance, Trial.Rules.Fielding.Bobble.FumbleSec));
    }

    /// <summary>D off the hop, H off the hands, p off the accepted curve — as numbers, with the 10 % ceiling binding.</summary>
    [Fact]
    public void TheChanceIsTheAcceptedCurveOffTheHopAndTheHands()
    {
        var r = Trial.Rules;
        // D: nothing for a roll, a falling ball or a micro-bounce; the middle of a full hop's rise is 1; linear either side.
        Assert.Equal(0, FieldingResolver.HopDifficulty(0, 0, r));
        Assert.Equal(0, FieldingResolver.HopDifficulty(1.0, -5, r));
        Assert.Equal(0, FieldingResolver.HopDifficulty(0.2, 3.0, r));           // apex 0.34 ft: a micro-bounce
        double Rise(double apex, double phi) => Math.Sqrt(2 * G * apex * (1 - phi));   // vy at height phi × apex on a hop to apex
        Assert.Equal(1, FieldingResolver.HopDifficulty(0.75, Rise(1.5, 0.5), r), 6);
        Assert.Equal(1, FieldingResolver.HopDifficulty(1.5, Rise(3.0, 0.5), r), 6);
        Assert.Equal(0.5, FieldingResolver.HopDifficulty(0.5, Rise(1.0, 0.5), r), 6);         // apex 1.0: half way from 0.5 to 1.5
        Assert.Equal(0.5, FieldingResolver.HopDifficulty(0.325 * 2, Rise(2, 0.325), r), 6);   // φ 0.325: half way down the band
        Assert.Equal(0, FieldingResolver.HopDifficulty(0.15 * 2, Rise(2, 0.15), r), 6);       // the clean short hop
        Assert.Equal(0, FieldingResolver.HopDifficulty(0.85 * 2, Rise(2, 0.85), r), 6);       // the clean long hop
        Assert.Equal(0, FieldingResolver.HopDifficulty(0.75, Rise(1.5, 0.5), Control.Rules));   // nothing on the shipped table
        // H: Hands 1 → 0, 10 → 1, and the glove's help counts as it did for the shipped roll.
        Character Hands(int h) => Trial.Must("vale") with { Stats = Trial.Must("vale").Stats with { Hands = h } };
        Assert.Equal(0, FieldingResolver.HandlingQuality(Hands(1), null, r));
        Assert.Equal(0.5, FieldingResolver.HandlingQuality(Hands(5), null, r) + 1.0 / 18, 9);
        Assert.Equal(1, FieldingResolver.HandlingQuality(Hands(10), null, r));
        // p = 0.10 × D × (1 − 0.80 × H): 10 / 6 / 2 % at full difficulty for weak / middle / strong; 5 / 3 / 1 at half.
        Assert.Equal(0.10, FieldingResolver.HandlingErrorChance(1, 0, r), 9);
        Assert.Equal(0.06, FieldingResolver.HandlingErrorChance(1, 0.5, r), 9);
        Assert.Equal(0.02, FieldingResolver.HandlingErrorChance(1, 1, r), 9);
        Assert.Equal(0.05, FieldingResolver.HandlingErrorChance(0.5, 0, r), 9);
        Assert.Equal(0.03, FieldingResolver.HandlingErrorChance(0.5, 0.5, r), 9);
        Assert.Equal(0.01, FieldingResolver.HandlingErrorChance(0.5, 1, r), 9);
        Assert.Equal(0, FieldingResolver.HandlingErrorChance(0, 0, r));
        Assert.Equal(0.10, FieldingResolver.HandlingErrorChance(5, -1, r), 9);   // the ceiling binds whatever the inputs
        Assert.Equal(0, FieldingResolver.HandlingErrorChance(1, 0, Control.Rules));
    }

    /// <summary>
    /// The invariant the design rests on: a routine grounder to a planted infielder never rolls. A roll, a short hop, a long hop and
    /// a ball dropping from a high bounce, each to the mound, each across forty seeds — no chance, no bobble, on either table's rule.
    /// </summary>
    [Theory]
    [InlineData(60, 2, 0, ContactQuality.Perfect)]   // a short hop: the ball 0.09 ft up, rising off a 0.24-ft bounce
    [InlineData(60, 4, 0, ContactQuality.Perfect)]   // a long hop: 1.2 ft up and falling
    [InlineData(60, 6, 0, ContactQuality.Perfect)]   // dropping from 2.7 ft
    [InlineData(70, 8, 0, ContactQuality.Nice)]      // the routine grounder of the recoil tests
    [InlineData(60, 8, 15, ContactQuality.Nice)]     // a roll to the mound at the top of a 0.9-ft hop
    public void ARoutineGrounderToAPlantedInfielderNeverRolls(double exit, double launch, double spray, ContactQuality q)
    {
        for (var seed = 1; seed <= 40; seed++)
        {
            var run = Drive(Trial, exit, launch, spray, q, seed);
            Assert.Equal("P", run.Pos);
            Assert.Equal(0, run.Difficulty);
            Assert.Equal(0, run.Chance);
            Assert.Equal(0, run.Bobbles);
            Assert.Equal(0, run.StunFrames);
            Assert.Equal(PlayKind.GroundOut, run.Play.Kind);
        }
    }

    /// <summary>
    /// A 120-mph liner at 12° into left lands and comes up at vine 1.3 ft off the grass, rising 9.8 ft/s: 98 % of the way to the
    /// hardest ball. Hands 8 with the glove faces 3.3 %; authored Hands 1 faces 9.4 %, and nothing faces more than 10 %. The chance
    /// is what the curve says of the take, the same every time; the shipped table has no such roll and stuns nobody.
    /// </summary>
    [Fact]
    public void TheAwkwardHopCarriesTheCurvesChanceAndTheCapBinds()
    {
        var run = Drive(Trial, 120, 12, -25, ContactQuality.Perfect, seed: 1);
        Assert.Equal("LF", run.Pos);
        Assert.InRange(run.Difficulty, 0.95, 1.0);
        var vine = Trial.Must("vine");
        Assert.Equal(FieldingResolver.HandlingErrorChance(run.Difficulty, FieldingResolver.HandlingQuality(vine, run.Glove, Trial.Rules), Trial.Rules), run.Chance, 12);
        Assert.InRange(run.Chance, 0.03, 0.04);
        var weak = Drive(Trial, 120, 12, -25, ContactQuality.Perfect, seed: 1, lfHands: 1);
        Assert.Equal(run.Difficulty, weak.Difficulty);
        Assert.InRange(weak.Chance, 0.09, 0.10);
        Assert.True(weak.Chance <= 0.10 + 1e-12);
        var strong = Drive(Trial, 120, 12, -25, ContactQuality.Perfect, seed: 1, lfHands: 10);
        Assert.InRange(strong.Chance, 0.015, 0.025);
        var again = Drive(Trial, 120, 12, -25, ContactQuality.Perfect, seed: 1);
        Assert.Equal((run.Difficulty, run.Chance, run.TakeAt), (again.Difficulty, again.Chance, again.TakeAt));

        // On the shipped field the same swing is a liner caught in the air; with a runner on first the catch is not the completing frame. No roll, no stun.
        var shipped = Drive(Control, 120, 12, -25, ContactQuality.Perfect, seed: 1, runnerOnFirst: true);
        Assert.Equal(0, shipped.Chance);
        Assert.Equal(0, shipped.StunFrames);
    }

    /// <summary>
    /// Seed 35 is the one in thirty where vine's 3.3 % comes up. Slice 1 recorded this failed take as a local bobble (six ft/s out
    /// inside ±30°, 3.2 ft of travel, recovered at 0.64 s, a double); slice 2 (F693-02-error-outcome-selection) reads the contact —
    /// the ring met the ball at its edge, obstruction 0.03, and the ball came in at 59.5 ft/s, past the hot line — so the ball gets
    /// past instead: `DeflectionTests` has its numbers. What holds either way: one roll, one bobble, the 24-frame stun, the same seed
    /// twice the same play to the mark, seed 34 the same ball taken clean, and a bobble is not by itself the error.
    /// </summary>
    [Fact]
    public void TheFailedTakeIsOneRollOneBobbleOneStunAndAReliableRecovery()
    {
        var run = Drive(Trial, 120, 12, -25, ContactQuality.Perfect, seed: 35);
        Assert.Equal(1, run.Bobbles);
        Assert.Equal("LF", run.Pos);
        Assert.InRange(run.Chance, 0.03, 0.04);
        Assert.Equal(24, run.StunFrames);
        Assert.True(run.StunMove < 1.0, $"the stunned body moved {run.StunMove:0.00} ft");
        Assert.Equal("LF", run.RecoverPos);
        Assert.InRange(run.RecoverAt - run.TakeAt, 0.40 - 1e-9, 0.80);
        Assert.Equal(1, run.Rolls);
        Assert.False(run.Play.Outcome?.Error ?? true, "a bobble is not by itself the error (§8.6)");

        var again = Drive(Trial, 120, 12, -25, ContactQuality.Perfect, seed: 35);
        Assert.Equal(run.Marks, again.Marks);
        Assert.Equal(run.DirOffsetDeg, again.DirOffsetDeg);

        var clean = Drive(Trial, 120, 12, -25, ContactQuality.Perfect, seed: 34);
        Assert.Equal(0, clean.Bobbles);
        Assert.Equal(run.Chance, clean.Chance);
        Assert.Equal(PlayKind.Single, clean.Play.Kind);
    }

    /// <summary>The direction is one seeded draw, uniform inside the branch's spread: four seeds that fail on the hot liner get past (slice 2) with four different offsets, none outside ±15°; slice 1 read the same four as local bobbles inside ±30°.</summary>
    [Fact]
    public void TheErrorsDirectionIsOneUniformDrawInsideItsSpread()
    {
        var runs = new[] { 14, 16, 35, 58 }.Select(seed => Drive(Trial, 120, 12, -25, ContactQuality.Perfect, seed, lfHands: 1)).Where(r => r.Bobbles == 1).ToList();
        Assert.True(runs.Count >= 3, $"only {runs.Count} of the four seeds bobbled");
        Assert.All(runs, r => Assert.InRange(r.DirOffsetDeg, -15, 15));
        Assert.True(runs.Select(r => r.DirOffsetDeg).Distinct().Count() == runs.Count, "the offsets are one draw each, not one value");
    }

    /// <summary>Every rising take fails on a copy with the cap at 1 and the bands wide open: an ordinary-speed grounder (40 ft/s, under the hot line) is the knockdown — the physics to the number, 6 ft/s out, rest in 1.0 s over 3.0 ft of roll once down, and the take-again never rolls.</summary>
    [Fact]
    public void TheLocalBobblesPhysicsToTheNumberOnACertainFumble()
    {
        using var certain = new PatchedTrial(text => text
            .Replace("\"chanceCap\": 0.10", "\"chanceCap\": 1.0").Replace("\"handsCut\": 0.80", "\"handsCut\": 0")
            .Replace("\"hopMinApexFt\": 0.5", "\"hopMinApexFt\": 0").Replace("\"hopFullApexFt\": 1.5", "\"hopFullApexFt\": 0").Replace("\"hopPhaseHalfWidth\": 0.35", "\"hopPhaseHalfWidth\": 100"));
        var run = Drive(certain.Content, 80, 6, 25, ContactQuality.Perfect, seed: 1);   // a grounder the second baseman meets rising 3 ft/s at 0.44 ft
        Assert.Equal("2B", run.Pos);
        Assert.InRange(run.Chance, 0.99, 1.0);   // the band is 1 − |φ − 0.5| / 100
        Assert.Equal(1, run.Bobbles);
        Assert.InRange(run.LooseSpeed, 5.99, 6.01);
        Assert.True(run.ReboundMaxY <= 0.50 + 1e-6);
        Assert.InRange(run.Travel, 2.5, 3.6);   // 6²/12 = 3.0 ft of roll plus the spill's drift
        Assert.Equal(24, run.StunFrames);
        Assert.Equal(1, run.Rolls);
        Assert.True(run.RecoverAt > run.TakeAt + 0.40 - 1e-9, "nobody took it back inside the stun");
    }

    // ---------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------

    sealed record Run(PlayEvent Play, string Pos, double TakeAt, double Difficulty, double Chance, int Bobbles, int Rolls, GloveItem Glove,
        double ContactY, double LooseMaxY, double ReboundMaxY, double LooseSpeed, double DirOffsetDeg, double Travel, int StunFrames, double StunMove,
        double RecoverAt, string RecoverPos, IReadOnlyList<(PlayTraceMarkKind Kind, double T)> Marks);

    /// <summary>Harbor, vale on the mound, vine (Hands 8) in left; the CPU plays the ball; the run reads the take, its roll, and what the loose ball and the stunned body did.</summary>
    static Run Drive(ContentCatalog content, double exitMph, double launchDeg, double sprayDeg, ContactQuality quality, int seed, int lfHands = 0, bool runnerOnFirst = false)
    {
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "grit", "vine", "moss", "hex");
        if (lfHands > 0)
        {
            var lf = home.Roster.First(c => c.Id == "vine") with { Stats = home.Roster.First(c => c.Id == "vine").Stats with { Hands = lfHands } };
            home = home with { Roster = home.Roster.Select(c => c.Id == "vine" ? lf : c).ToList() };
        }
        var away = content.Team("Offense", "rio", "boom", "cinder", "soot", "nugget", "nico", "gull", "marlow", "ashlord");
        var match = Match.Exhibition(content, home, away, 3, seed, parkId: "harbor-diamond");
        var hit = FlightFixtures.Hit(match.Park, exitMph, launchDeg, sprayDeg, quality, rules: match.Rules);
        Assert.False(hit.Foul);
        var preview = match.PreviewHit(hit);
        if (runnerOnFirst) Assert.True(match.StationRunner(1, content.Must("gull")));   // so a catch is not the completing frame
        var live = match.LivePlay;
        live.Recording = true;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        PlayEvent? play = null;
        var takeAt = -1.0; var difficulty = 0.0; var chance = 0.0; var bobbles = 0; var rolls = 0; var pos = ""; var lastChance = -1.0;
        var contactY = 0.0; var looseMaxY = 0.0; var reboundMaxY = 0.0; var looseSpeed = 0.0; var dir = double.NaN; var travel = 0.0; var stunFrames = 0; var stunMove = 0.0;
        var recoverAt = -1.0; var recoverPos = ""; (double X, double Y, double Z) prev = (0, 0, 0); (double X, double Z)? at = null; (double X, double Z)? stunAt = null; (double X, double Z)? inDir = null; var grounded = false;
        for (var i = 0; i < 60 * 20 && play is null; i++)
        {
            prev = (live.BallX, live.BallY, live.BallZ);
            var r = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu));
            play = r.CompletedPlay;
            if (play is not null) break;
            // A roll is a chance that was set this frame (the take), whether it came up or not.
            if (live.HandlingChance != lastChance) { if (live.HandlingChance > 0) rolls++; lastChance = live.HandlingChance; }
            var bobbledNow = live.Events.Contains(LiveEvent.Bobble);
            if (bobbledNow) bobbles++;
            if (takeAt < 0 && (live.HoldsBall || bobbledNow))
            {
                takeAt = live.ElapsedSeconds; difficulty = live.HopDifficulty; chance = live.HandlingChance; pos = live.GlovePos;
                if (bobbledNow)
                {
                    contactY = live.BallY; at = (live.BallX, live.BallZ); stunAt = (live.GloveX, live.GloveZ); inDir = (live.BallX - prev.X, live.BallZ - prev.Z);
                }
            }
            // The failed take's ball, loose (the local bobble) or carried on (the deflection, slice 2), until somebody has it again.
            if (bobbles > 0 && recoverAt < 0)
            {
                looseMaxY = Math.Max(looseMaxY, live.BallY);
                if (live.BallY <= 1e-9) grounded = true;
                if (grounded) reboundMaxY = Math.Max(reboundMaxY, live.BallY);
                if (at is { } a0) travel = Math.Max(travel, Diamond.Dist(a0.X, a0.Z, live.BallX, live.BallZ));
                if (double.IsNaN(dir) && at is { } a1 && inDir is { } d0 && (live.BallX != a1.X || live.BallZ != a1.Z))
                {
                    var ox = live.BallX - a1.X; var oz = live.BallZ - a1.Z;
                    looseSpeed = Math.Sqrt(ox * ox + oz * oz) / Frame;
                    var dd = (Math.Atan2(ox, oz) - Math.Atan2(d0.X, d0.Z)) * 180 / Math.PI;
                    while (dd > 180) dd -= 360;
                    while (dd < -180) dd += 360;
                    dir = dd;
                }
                if (stunAt is { } s0 && live.StunT > 0) { stunFrames++; stunMove = Math.Max(stunMove, Diamond.Dist(s0.X, s0.Z, live.GloveX, live.GloveZ)); }
            }
            if (bobbles > 0 && recoverAt < 0 && live.HoldsBall) { recoverAt = live.ElapsedSeconds; recoverPos = live.GlovePos; }
        }
        Assert.NotNull(play);
        Assert.True(takeAt > 0, "nobody took the ball");
        var marks = (live.TakeTrace(play).Marks ?? []).Select(m => (m.Kind, m.T)).ToList();
        return new Run(play, pos, takeAt, difficulty, chance, bobbles, rolls, match.DefenseGlove, contactY, looseMaxY, reboundMaxY, looseSpeed, dir, travel, stunFrames, stunMove, recoverAt, recoverPos, marks);
    }

    /// <summary>The c80 copy with its fielding table changed, laid over the shipped root.</summary>
    sealed class PatchedTrial : IDisposable
    {
        readonly string _dir;

        public PatchedTrial(Func<string, string> fielding)
        {
            _dir = Path.Combine(Path.GetTempPath(), "grand-sluggers-handling-" + Guid.NewGuid().ToString("N"));
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
