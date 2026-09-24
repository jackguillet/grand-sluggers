using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The stick at contact (#855, PH-12 option C, PH-18), Appendix B.1 rows S-128 … S-132 (S-13 lives
/// beside the other plate rows in <see cref="AtBatScenarioTests"/>).
///
/// Jack accepted the <c>trials/pitch5</c> stick trial on September 22, 2026 ("approve all"); #883
/// shipped it and #887 removed the switch and its off path. A swing that is neither a bunt nor a Star
/// Swing ignores both aims (<b>S-128</b>), while timing, contact, pitch height, the charge and the
/// out-of-zone spread still shape it (<b>S-129</b>); a bunt reads its held side and not the stick
/// (<b>S-130</b>, PH-14-R5); a Star Swing (<b>S-131</b>, until Phase 6) still follows the stick; and
/// the CPU batter holds no stick for an ordinary swing and draws no aim for it, while its Star Swing
/// draws both and its sac bunt holds a side (<b>S-132</b>).
///
/// The shipped root is loaded <b>in process</b> through a <see cref="DataRoot"/> built from the
/// repository, so nothing here depends on <c>GRAND_SLUGGERS_TRIAL</c> being set.
///
/// <b>No row stores a double.</b> Every claim compares two resolutions to each other (the same
/// swing with two sticks) or composes today's value in the test from a <see cref="Random"/> of the
/// same seed on the running platform (S-132), so a libm difference between machines cannot make a
/// row lie (#811).
/// </summary>
public sealed class StickShapingScenarioTests
{
    readonly ContentCatalog _shipped = ContentCatalog.Load(new DataRoot(Shipped.Content.Root.Shipped));

    static double CenterY => StrikeZoneGeometry.CenterY;

    /// <summary>A middle-middle fastball: the CPU's middle zone, the umpire's strike.</summary>
    static PitchCommand Middle => Scenario.PitchAt(0, CenterY);

    /// <summary>Every stick a pad can hold at contact: centered, the four sides and the four diagonals, full throw.</summary>
    static readonly (string Name, double X, double Y)[] Sticks =
    [
        ("center", 0, 0),
        ("up", 0, 1), ("down", 0, -1), ("left", -1, 0), ("right", 1, 0),
        ("up-left", -Math.Sqrt(0.5), Math.Sqrt(0.5)), ("up-right", Math.Sqrt(0.5), Math.Sqrt(0.5)),
        ("down-left", -Math.Sqrt(0.5), -Math.Sqrt(0.5)), ("down-right", Math.Sqrt(0.5), -Math.Sqrt(0.5)),
    ];

    static readonly ContactQuality[] Zones = [ContactQuality.Perfect, ContactQuality.Nice, ContactQuality.Sour];

    // ---------------------------------------------------------------------------------
    // The rule
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S128_TheStickShapesOnlyAStarSwing()
    {
        // #883: Jack accepted the stick trial on September 22, 2026 ("approve all"). The bunt's direction
        // is its held side (PH-14-R5), so the stick shapes neither it nor an ordinary swing.
        foreach (var bunt in new[] { false, true })
        foreach (var star in new[] { false, true })
            Assert.Equal(star && !bunt, AtBatResolver.StickShapesContact(bunt, star));

        // The stick numbers stay authored: the Star Swing still reads them.
        Assert.True(_shipped.Rules.Batting.Spray.StickDeg > 0 && _shipped.Rules.Batting.Launch.StickDeg > 0);
    }

    // ---------------------------------------------------------------------------------
    // S-128  Shipped invariance: an ordinary swing is the same ball wherever the stick is
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S128_UnderGeometryOnlyAnOrdinarySwingIsTheSameBallWhereverTheStickIs()
    {
        var resolver = Resolver(_shipped);
        var park = Harbor(_shipped);
        var cases = 0;
        foreach (var bats in new[] { Hand.R, Hand.L })
        foreach (var charge in new[] { 0.0, 1.0 })
        foreach (var zone in Zones)
        foreach (var err in new[] { -2.0, 0.0, 2.0 })
        foreach (var seed in new[] { 1, 7, 42 })
        {
            var batter = Batter(_shipped, bats);
            var x = CrossingFor(_shipped, batter, charge, zone);
            var reference = resolver.Resolve(Input(_shipped, batter, Intent(charge, 0, 0), err, x, CenterY), park, new Random(seed));
            Assert.Equal(zone, reference.Quality);
            foreach (var stick in Sticks)
            {
                var intent = Intent(charge, stick.X, stick.Y);
                // The intent still carries the stick (AtBatFeel is untouched); only the resolver ignores it.
                Assert.Equal(AtBatResolver.SprayAimDeg(stick.X, rules: Rules.Default), intent.SprayAimDeg);
                Assert.Equal(stick.Y, intent.LaunchAim);
                var hit = resolver.Resolve(Input(_shipped, batter, intent, err, x, CenterY), park, new Random(seed));
                // Record equality: exit, launch, spray, carry, class, foul, quality — every field, exactly.
                Assert.Equal(reference, hit);
                cases++;
            }
        }
        Assert.Equal(2 * 2 * Zones.Length * 3 * 3 * Sticks.Length, cases);
    }

    [Fact]
    public void S128_ThroughTheMatchTheSwingCarriesTheStickAndTheBallIgnoresIt()
    {
        // The whole path, not the resolver alone: Match.BeginAtBat hands the command's aims to the
        // resolver it built on the match's own table (§6.1, FD-03).
        foreach (var seed in new[] { 1, 2, 3, 4, 5 })
        {
            var shippedHits = Sticks.Select(s => AtBat(_shipped, seed, s.X, s.Y)).ToList();
            Assert.NotEqual(ContactQuality.Miss, shippedHits[0].Quality);
            Assert.All(shippedHits, h => Assert.Equal(shippedHits[0], h));
        }

        static AtBatResult AtBat(ContentCatalog content, int seed, double stickX, double stickY)
        {
            var match = new Scenario(content, seed).Match;
            var swing = Scenario.SwingAt(0, stickX: stickX, launchAim: stickY);
            Assert.Equal(AtBatResolver.SprayAimDeg(stickX, rules: Rules.Default), swing.SprayAimDeg);
            match.BeginAtBat(Middle, swing, out var hit, out _);
            return hit;
        }
    }

    // ---------------------------------------------------------------------------------
    // S-129  Timing and contact still shape the ball on the shipped root
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S129_EarlyStillPullsAndLateStillPushesByTheBattingHand()
    {
        var resolver = Resolver(_shipped);
        var park = Harbor(_shipped);
        foreach (var bats in new[] { Hand.R, Hand.L })
        foreach (var stick in Sticks)
        foreach (var seed in Enumerable.Range(1, 10))
        {
            var batter = Batter(_shipped, bats);
            var x = CrossingFor(_shipped, batter, 0, ContactQuality.Perfect);
            var intent = Intent(0, stick.X, stick.Y);
            var early = resolver.Resolve(Input(_shipped, batter, intent, -3, x, CenterY), park, new Random(seed));
            var late = resolver.Resolve(Input(_shipped, batter, intent, 3, x, CenterY), park, new Random(seed));
            // A right-handed batter pulls toward third (negative spray), a left-handed one toward first
            // (PH-10, §5.3) — and a stick held the other way cannot take it back.
            var toPull = -SweetSpot.TipSign(bats);
            var where = $"{bats} {stick.Name} seed {seed}";
            Assert.True(early.SprayDeg * toPull > 0, $"early pulls: {where} {early.SprayDeg}");
            Assert.True(late.SprayDeg * toPull < 0, $"late pushes: {where} {late.SprayDeg}");
        }
    }

    [Fact]
    public void S129_PitchHeightAndTheChargeStillShapeTheLaunch()
    {
        var resolver = Resolver(_shipped);
        var park = Harbor(_shipped);
        var height = _shipped.Rules.Batting.Launch.PerFtOfHeight;
        Assert.True(height > 0 && _shipped.Rules.Batting.Charge.LoftDeg > 0);
        foreach (var bats in new[] { Hand.R, Hand.L })
        foreach (var charge in new[] { 0.0, 1.0 })
        foreach (var stick in Sticks)
        foreach (var seed in Enumerable.Range(1, 10))
        {
            var batter = Batter(_shipped, bats);
            var intent = Intent(charge, stick.X, stick.Y);
            var where = $"{bats} {(charge > 0 ? "charged" : "quick")} {stick.Name} seed {seed}";

            // A low crossing launches lower than a high one (perFtOfHeight), whatever the stick says.
            var low = resolver.Resolve(Input(_shipped, batter, intent, 0, 0, CenterY - 0.6), park, new Random(seed));
            var high = resolver.Resolve(Input(_shipped, batter, intent, 0, 0, CenterY + 0.6), park, new Random(seed));
            Assert.True(low.Quality is ContactQuality.Perfect or ContactQuality.Nice, where);
            Assert.True(high.Quality is ContactQuality.Perfect or ContactQuality.Nice, where);
            Assert.True(low.LaunchDeg < high.LaunchDeg, $"a low pitch launches lower: {where} {low.LaunchDeg} vs {high.LaunchDeg}");

            if (charge > 0) continue;
            // A charged swing lofts more than a quick one on the same pitch and the same noise draw.
            var quick = resolver.Resolve(Input(_shipped, batter, intent, 0, 0, CenterY), park, new Random(seed));
            var charged = resolver.Resolve(Input(_shipped, batter, Intent(1, stick.X, stick.Y), 0, 0, CenterY), park, new Random(seed));
            Assert.Equal(ContactQuality.Perfect, quick.Quality);
            Assert.Equal(ContactQuality.Perfect, charged.Quality);
            Assert.True(charged.LaunchDeg > quick.LaunchDeg, $"a charge lofts more: {where} {charged.LaunchDeg} vs {quick.LaunchDeg}");
        }
    }

    [Fact]
    public void S129_TheOutOfZoneSpreadStillApplies()
    {
        var resolver = Resolver(_shipped);
        var park = Harbor(_shipped);
        var span = _shipped.Rules.Batting.Spray.OutOfZoneSpanDeg;
        Assert.True(span > 0);
        var moved = 0;
        foreach (var bats in new[] { Hand.R, Hand.L })
        foreach (var seed in Enumerable.Range(1, 40))
        {
            var batter = Batter(_shipped, bats);
            var x = CrossingFor(_shipped, batter, 0, ContactQuality.Perfect);
            var intent = Intent(0, 1, 1);
            var inZone = resolver.Resolve(Input(_shipped, batter, intent, 0, x, CenterY), park, new Random(seed));
            var outOfZone = resolver.Resolve(Input(_shipped, batter, intent, 0, x, CenterY, inZone: false), park, new Random(seed));
            // The same draws up to the extra one: the difference is that one draw's spread, and it is
            // inside half the span (plus the two one-decimal roundings the resolver makes).
            var diff = Math.Abs(outOfZone.SprayDeg - inZone.SprayDeg);
            Assert.True(diff <= span / 2 + 0.1, $"{bats} seed {seed}: {diff}");
            if (diff > 1) moved++;
        }
        Assert.True(moved > 40, $"the out-of-zone spread moved {moved} of 80 balls by more than a degree");
    }

    // ---------------------------------------------------------------------------------
    // S-130  A bunt reads its held side, never the stick (PH-14-R5)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S130_APadsBuntIsTheSameBallWhereverTheStickIsAndLeansToItsHeldSide()
    {
        var shipped = Resolver(_shipped);
        foreach (var bats in new[] { Hand.R, Hand.L })
        foreach (var seed in Enumerable.Range(1, 10))
        {
            var bySide = new Dictionary<BuntSide, AtBatResult>();
            foreach (var side in new[] { BuntSide.Third, BuntSide.None, BuntSide.First })
            {
                AtBatResult? centered = null;
                foreach (var stick in Sticks)
                {
                    var intent = Intent(0, stick.X, stick.Y, bunt: true, side: side);
                    var onShipped = shipped.Resolve(Input(_shipped, Batter(_shipped, bats), intent, 0, 0, CenterY), Harbor(_shipped), new Random(seed));
                    Assert.NotEqual(ContactQuality.Sour, onShipped.Quality);
                    centered ??= onShipped;
                    Assert.Equal(centered, onShipped);
                }
                bySide[side] = centered!;
            }
            var where = $"{bats} seed {seed}";
            Assert.True(bySide[BuntSide.Third].SprayDeg < bySide[BuntSide.None].SprayDeg, where);
            Assert.True(bySide[BuntSide.None].SprayDeg < bySide[BuntSide.First].SprayDeg, where);
        }
    }

    [Fact]
    public void S130_TheCpuSacBuntHoldsASideAndCarriesNoAim()
    {
        var bunts = CpuSacBunts();
        Assert.True(bunts > 0, "some seed squares and bunts");
    }

    // ---------------------------------------------------------------------------------
    // S-131  A Star Swing still follows the stick on the shipped root (owner: Phase 6)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S131_AStarSwingStillFollowsTheStickOnTheShippedRoot()
    {
        // PH-12's scope is ordinary hits. Each Star Swing is reviewed in Phase 6; until then it keeps
        // both stick terms, and this row is how Phase 6 finds it. Rio's heat-swing authors no launch
        // of its own, so both terms reach it.
        var captain = _shipped.Must("rio");
        Assert.Null(StarSkills.SwingLaunchDeg(captain.StarSwing, _shipped.StarSkills));
        var shipped = Resolver(_shipped);
        foreach (var bats in new[] { Hand.R, Hand.L })
        foreach (var seed in Enumerable.Range(1, 10))
        {
            var hits = new Dictionary<string, AtBatResult>();
            foreach (var stick in Sticks)
            {
                var intent = Intent(0, stick.X, stick.Y);
                var onShipped = shipped.Resolve(Input(_shipped, Batter(_shipped, bats), intent, 0, 0, CenterY, star: true), Harbor(_shipped), new Random(seed));
                Assert.Equal(captain.StarSwing, onShipped.StarSwingUsed);
                hits[stick.Name] = onShipped;
            }
            var where = $"{bats} seed {seed}";
            Assert.True(hits["up"].LaunchDeg < hits["center"].LaunchDeg, where);
            Assert.True(hits["down"].LaunchDeg > hits["center"].LaunchDeg, where);
            Assert.True(hits["left"].SprayDeg < hits["center"].SprayDeg, where);
            Assert.True(hits["center"].SprayDeg < hits["right"].SprayDeg, where);
        }
    }

    // ---------------------------------------------------------------------------------
    // S-132  The CPU batter: no aim for an ordinary swing
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S132_OnTheShippedRootAnOrdinaryCpuSwingHoldsNoStickAndDrawsNoAim()
    {
        foreach (var seed in Enumerable.Range(1, 25))
        {
            var match = Match.Slice(_shipped, innings: 3, seed: seed);
            AssertTheComposedPath(match);
            var first = match.CpuBatter.Swing(Middle);
            var second = match.CpuBatter.Swing(Middle);

            Assert.False(first.Star);
            Assert.Equal(0, first.SprayAimDeg);
            Assert.Equal(0, first.LaunchAim);
            // The replay skips both Gaussians and still lands the second call: neither was drawn.
            var replay = new Replay(seed);
            Assert.Equal(Today(match, replay), first);
            Assert.Equal(Today(match, replay), second);
        }
    }

    [Fact]
    public void S132_OnTheShippedRootACpuStarSwingDrawsItsAimsAndASacBuntHoldsASide()
    {
        var stars = 0;
        var ordinary = 0;
        foreach (var seed in Enumerable.Range(1, 120))
        {
            var onShipped = CaptainUp(_shipped, seed).CpuBatter.Swing(Middle);
            Assert.True(onShipped.Swing);
            if (onShipped.Bunt) continue;
            if (onShipped.Star)
            {
                // A Star Swing draws its aims.
                Assert.NotEqual(0, onShipped.SprayAimDeg);
                Assert.NotEqual(0, onShipped.LaunchAim);
                stars++;
                continue;
            }
            Assert.Equal(0, onShipped.SprayAimDeg);
            Assert.Equal(0, onShipped.LaunchAim);
            ordinary++;
        }
        Assert.True(stars > 0, "some seed swings a Star Swing");
        Assert.True(ordinary > 0, "and some seed an ordinary one");
        Assert.True(CpuSacBunts() > 0, "some seed squares and bunts");
    }

    // ---- helpers ---------------------------------------------------------------------

    /// <summary>
    /// The CPU's sac bunt, seed by seed: once the square is read at SET, the bunt it lays down is the
    /// held bunt (§5.8): the side decided with the square, no aim, no timed press. Returns how many
    /// seeds squared.
    /// </summary>
    int CpuSacBunts()
    {
        var bunts = 0;
        foreach (var seed in Enumerable.Range(1, 60))
        {
            var onShipped = LightBatWithARunnerOnFirst(_shipped, seed);
            if (!onShipped.CpuBatter.SquaresBunt()) continue;
            var shippedBunt = onShipped.CpuBatter.Swing(Middle);
            Assert.True(shippedBunt.Bunt);
            Assert.NotEqual(BuntSide.None, shippedBunt.BuntSide);
            Assert.Equal(onShipped.CpuBatter.BuntSide, shippedBunt.BuntSide);
            Assert.Equal(0, shippedBunt.SprayAimDeg);
            Assert.Equal(0, shippedBunt.LaunchAim);
            Assert.Equal(0, shippedBunt.TimingErrorFrames);
            bunts++;
        }
        return bunts;
    }

    /// <summary>The <c>BuntScenarioTests</c> setup: pip (a light bat) leads off, a runner on first, nobody out.</summary>
    static Match LightBatWithARunnerOnFirst(ContentCatalog content, int seed)
    {
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = content.Team("Offense", "zig", "pip", "dart", "jester", "cinder", "grit", "soot", "boom", "nugget");
        var match = Match.Exhibition(content, home, away, 3, seed);
        Assert.Equal("pip", match.Batter.Id);
        Assert.True(match.StationRunner(1, content.Must("dart")));
        return match;
    }

    /// <summary>The home captain (Rio) at the plate with a full meter and a runner on first: a Star Swing is on the table.</summary>
    static Match CaptainUp(ContentCatalog content, int seed)
    {
        var match = Match.Slice(content, innings: 3, seed: seed);
        match.SkipToHomeCaptainAtBat();
        match.GiveOffenseStars(5);
        Assert.True(match.StationRunner(1, content.Must("nico")));
        Assert.True(match.Batter.Captain && match.CanStarSwing);
        return match;
    }

    /// <summary>
    /// The state <see cref="Today"/> composes for: nobody on and 0-0 (no sac-bunt read, no Star Swing
    /// roll, no forced charge), a middle-middle fastball (a certain swing, never fooled), and a rubber
    /// that has not moved (the ordinary mistrack chance).
    /// </summary>
    static void AssertTheComposedPath(Match match)
    {
        Assert.Empty(match.RunnersOn());
        Assert.Equal((0, 0, 0), (match.Balls, match.Strikes, match.Outs));
        Assert.False(match.RubberMovedSinceLastPitch);
        Assert.False(match.Rules.Pitching.Families.Of(Middle.Type).OffSpeed);
        Assert.False(ChargeFeel.IsCharge(Middle.Charge01));
    }

    /// <summary>
    /// Today's <see cref="CpuBatter.Swing"/> on the path <see cref="AssertTheComposedPath"/> holds,
    /// composed from <paramref name="rng"/> in today's order: the charge roll, the tracking roll, the
    /// timing Gaussian, and the three mistrack draws when it did not track. No aim is drawn.
    /// </summary>
    static SwingCommand Today(Match match, Replay rng)
    {
        var c = match.Rules.Batting.Cpu;
        var level = match.Rules.Cpu.Active;
        var batter = match.Batter;
        var (cx, _) = PitchFlight.Crossing(Middle, match.Rules, match.Pitcher.StarPitch);
        var charge = rng.Next() < CpuBatter.ChargeChance(batter, c.Archetype) ? 1.0 : 0;
        var tracked = rng.Next() < c.TrackPerfectChance;
        var err = rng.Gauss() * (11 - batter.Stats.Contact) * c.ErrorFramesPerBatStat * level.TimingSigmaMul;
        double box;
        if (tracked)
            box = Math.Clamp(cx / HomeSet.BatterWalk, -1, 1);
        else
        {
            var chance = Math.Min(0.95, c.MistrackChance * level.MistrackMul);
            var guess = rng.Next() < chance ? 0 : cx;
            var offset = (c.MistrackMinFt + rng.Next() * c.MistrackSpanFt) * (rng.Next() < 0.5 ? -1 : 1);
            box = Math.Clamp((guess + offset) / HomeSet.BatterWalk, -1, 1);
        }
        return new SwingCommand(true, charge, err, false, BoxOffsetX: box);
    }

    /// <summary>The CPU batter's stream of the match's seed (<see cref="MatchStreams.BatAi"/>), read the way the batter reads it.</summary>
    sealed class Replay(int seed)
    {
        readonly SimRandom _rng = SimRandom.Stream(seed, "bat-ai");

        public double Next() => _rng.NextDouble();

        public double Gauss() => _rng.Gauss();
    }

    static AtBatResolver Resolver(ContentCatalog content) => new(content.Chemistry, content.Rules, content.StarSkills);

    static Park Harbor(ContentCatalog content) => content.Parks[ExhibitionPick.DefaultPark];

    /// <summary>Rio, batting from the named side: the same hitter both ways, so only the hand differs.</summary>
    static Character Batter(ContentCatalog content, Hand bats) => content.Must("rio") with { Bats = bats };

    /// <summary>A Pitch-5 arm, so the pitch factor is ×1 unless a row asks for it (the <see cref="AtBatScenarioTests"/> arm).</summary>
    static Character Arm(ContentCatalog content)
    {
        var arm = content.Must("vale");
        return arm with { Stats = arm.Stats with { Pitch = 5 } };
    }

    /// <summary>
    /// A pad's committed swing with this stick held: the real capture (<see cref="SwingInputIntent.Capture"/>),
    /// so the intent carries the stick exactly as the client's does.
    /// </summary>
    static SwingInputIntent Intent(double charge01, double stickX, double stickY, bool bunt = false,
        BuntSide side = BuntSide.None) =>
        SwingInputIntent.Capture(new ChargeButtonStep(default, true, charge01, 0), stickX, stickY, bunt, 0, Rules.Default, side);

    static AtBatInput Input(ContentCatalog content, Character batter, SwingInputIntent intent, double err,
        double crossingX, double crossingY, bool inZone = true, bool star = false) =>
        new(Arm(content), batter, null, [],
            ChargePitch: false, ChangeupPitch: false, TimingErrorFrames: err,
            UseStarPitch: false, UseStarSwing: star, Bat: content.Bats["harbor-lumber"], PitcherStamina: 80,
            SprayAimDeg: intent.SprayAimDeg, PitchInZone: inZone, Bunt: intent.Bunt, LaunchAim: intent.LaunchAim,
            Charge01: intent.Fill01, CrossingX: crossingX, CrossingY: crossingY, BuntSide: intent.BuntSide);

    /// <summary>
    /// The crossing X, from the heart of the oval toward the tip, where this hitter's swing meets the
    /// ball in <paramref name="zone"/>. Found by asking the resolver (quality draws nothing), so no
    /// barrel geometry is stored here.
    /// </summary>
    static double CrossingFor(ContentCatalog content, Character batter, double charge01, ContactQuality zone)
    {
        var resolver = Resolver(content);
        var park = Harbor(content);
        for (var i = 0; i <= 200; i++)
        {
            var x = SweetSpot.TipSign(batter.Bats) * i * 0.01;
            if (resolver.Resolve(Input(content, batter, Intent(charge01, 0, 0), 0, x, CenterY), park, new Random(1)).Quality == zone)
                return x;
        }
        throw new InvalidOperationException($"no crossing meets {zone} for {batter.Id}");
    }
}
