using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Spec Appendix B.1 rows owned by P1 (#563), part (a): the pitch and swing contract — cursor
/// decides quality, timing decides direction (D4). Each row is named by its spec id. S-04 and
/// S-20 … S-29 land with the pitch shapes, the flight (P2), and the CPU tables.
/// </summary>
public sealed class AtBatScenarioTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    static double CenterY => StrikeZoneGeometry.CenterY;

    // ---------------------------------------------------------------------------------
    // S-01 … S-03  The frame never lies: strike / ball / swing-and-miss at the crossing
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S01_TakeInsideTheFrameIsACalledStrike()
    {
        var s = new Scenario(_content);
        var ev = s.Match.Play(Scenario.PitchAt(StrikeZoneGeometry.HalfWidth - 0.01, StrikeZoneGeometry.Top - 0.01), Scenario.Take);
        Assert.Equal(PlayKind.TakeStrike, ev.Kind);
        Assert.True(ev.AtBat.InZone);
        Assert.Equal(1, s.Match.Strikes);
    }

    [Fact]
    public void S02_TakeOutsideTheFrameByAHundredthIsABall()
    {
        var s = new Scenario(_content);
        var ev = s.Match.Play(Scenario.PitchAt(StrikeZoneGeometry.HalfWidth + 0.01, CenterY), Scenario.Take);
        Assert.Equal(PlayKind.TakeBall, ev.Kind);
        Assert.False(ev.AtBat.InZone);
        Assert.Equal(1, s.Match.Balls);
        Assert.Equal(0, s.Match.Strikes);
    }

    [Fact]
    public void S03_SwingAndMissOutsideIsAStrike()
    {
        var late = new Scenario(_content);
        var ev = late.Match.Play(Scenario.PitchAt(1.5, CenterY), Scenario.SwingAt(20));
        Assert.Equal(PlayKind.SwingMiss, ev.Kind);
        Assert.False(ev.AtBat.InZone);
        Assert.Equal(1, late.Match.Strikes);

        // Square timing but the ball is off the bat: still a miss, still a strike.
        var off = new Scenario(_content);
        var far = off.Match.Play(Scenario.PitchAt(SweetSpot.TipSign(off.Match.Batter.Bats) * 2.6, CenterY), Scenario.SwingAt(0));
        Assert.Equal(PlayKind.SwingMiss, far.Kind);
        Assert.Equal(ContactQuality.Miss, far.AtBat.Quality);
    }

    // ---------------------------------------------------------------------------------
    // S-04  The CPU batter decides from the flight as it stands at its commit
    // ---------------------------------------------------------------------------------

    [Fact]
    [Trait("Kind", "Balance")]
    public void S04_CpuBatterReadsTheSteeredCrossingAndTakesAPitchSteeredOut()
    {
        // A human pitcher on the edge steers full break out of the zone, held one way from release. By the
        // CPU's commit the break has already reached it (S-141 has the steer that starts after), so the
        // CPU reads the pitch out of the zone and takes at (100 − chase)%.
        var takes = 0;
        const int n = 300;
        double chase = 0;
        for (var seed = 1; seed <= n; seed++)
        {
            var s = new Scenario(_content, seed);
            var match = s.Match;
            // Near the frame with fewer than two strikes: chase (chaseBase − Bat)% (spec §5.9).
            chase = (_content.Rules.Batting.Cpu.ChaseBase - match.Batter.Stats.Bat) / 100.0;
            var edge = match.PreparePitch(Scenario.PitchAt(StrikeZoneGeometry.HalfWidth - 0.1, CenterY));
            Assert.True(StrikeZoneGeometry.Contains(edge), "the launched pitch is a strike");
            var steered = edge with { BreakX = 1 };
            Assert.False(StrikeZoneGeometry.Contains(steered), "full break carries it out");
            var swing = match.CpuSwing(steered);
            if (!swing.Swing) takes++;
        }
        Assert.InRange(takes / (double)n, 1 - chase - 0.08, 1 - chase + 0.08);

        // The decision instant is before the latest square press, and a CPU swing never claims an earlier bat.
        var plateAt = 1.0;
        var decide = AtBatMotion.CpuDecisionTime(plateAt, _content.Rules);
        Assert.True(decide < AtBatMotion.SquarePressAt(plateAt, rules: _content.Rules));
        var early = AtBatMotion.CommitCpuSwing(Scenario.SwingAt(-30), plateAt, _content.Rules);
        Assert.Equal(AtBatMotion.SwingErrorFrames(decide, plateAt), early.TimingErrorFrames, 8);
        Assert.Equal(2, AtBatMotion.CommitCpuSwing(Scenario.SwingAt(2), plateAt, _content.Rules).TimingErrorFrames);
    }

    // ---------------------------------------------------------------------------------
    // S-05 … S-06  Any strike is hittable; height is earned on the mound
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S05_ChargedFastballHighInTheZoneIsContactWithReducedQuality()
    {
        var s = new Scenario(_content);
        var ev = s.Match.Play(Scenario.PitchAt(0, 3.4, charge: 1), Scenario.SwingAt(0));
        Assert.NotEqual(ContactQuality.Miss, ev.AtBat.Quality);
        Assert.NotEqual(ContactQuality.Perfect, ev.AtBat.Quality);
        Assert.Equal(ContactQuality.Nice, ev.AtBat.Quality);
    }

    [Fact]
    public void S06_ChangeupDumpedLowIsContactWithAGrounderBias()
    {
        var resolver = new AtBatResolver(_content.Chemistry);
        var park = _content.Parks["harbor-diamond"];
        var low = Input(bat: 5, err: 0, crossingY: 1.6, changeup: true);
        var mid = low with { CrossingY = CenterY, ChangeupPitch = false };
        for (var seed = 0; seed < 12; seed++)
        {
            var dumped = resolver.Resolve(low, park, new Random(seed));
            var square = resolver.Resolve(mid, park, new Random(seed));
            Assert.NotEqual(ContactQuality.Miss, dumped.Quality);
            Assert.True(dumped.LaunchDeg < square.LaunchDeg,
                $"seed {seed}: low crossing launch {dumped.LaunchDeg} vs center {square.LaunchDeg}");
        }
        var perFt = _content.Rules.Batting.Launch.PerFtOfHeight;
        var drop = (CenterY - 1.6) * perFt;
        var a = resolver.Resolve(mid, park, new Random(3)).LaunchDeg;
        var b = resolver.Resolve(low, park, new Random(3)).LaunchDeg;
        Assert.Equal(drop, a - b, 0.2);
    }

    // ---------------------------------------------------------------------------------
    // S-07 … S-09  Cursor decides quality, timing decides direction, the window is the window.
    // Re-expressed against the ball's plate time (D13, #612 / #670): the square press is the
    // plate time less batting.window.leadSec (0.18 s), not press + 0.30.
    // ---------------------------------------------------------------------------------

    const double PlateAt = 0.98;
    const double LeadSec = 0.18;

    /// <summary>The judged error of a press <paramref name="beforePlate"/> seconds before the ball reaches the plate.</summary>
    double PressFrames(double beforePlate) =>
        AtBatMotion.SwingErrorFrames(PlateAt - beforePlate, PlateAt, rules: _content.Rules);

    [Fact]
    public void S07_SquareBatFiveSlapAtTheCenterIsPerfectStraightToCenter()
    {
        var resolver = new AtBatResolver(_content.Chemistry);
        var park = _content.Parks["harbor-diamond"];
        var spread = _content.Rules.Batting.Spray.PerfectSpreadDeg / 2;
        Assert.Equal(LeadSec, _content.Rules.Batting.Window.LeadSec, 8);
        var err = PressFrames(LeadSec);
        Assert.Equal(0, err, 8);
        for (var seed = 0; seed < 20; seed++)
        {
            var r = resolver.Resolve(Input(bat: 5, err: err), park, new Random(seed));
            Assert.Equal(ContactQuality.Perfect, r.Quality);
            Assert.InRange(r.SprayDeg, -spread, spread);
            Assert.True(r.InPlay);
        }
    }

    [Fact]
    [Trait("Kind", "Balance")]
    public void S08_FourFramesEarlyInsideTheNineFrameWindowIsStillPerfectAndPulled()
    {
        var resolver = new AtBatResolver(_content.Chemistry);
        var park = _content.Parks["harbor-diamond"];
        var err = PressFrames(LeadSec + 4.0 / 60);
        Assert.Equal(-4, err, 8);
        var input = Input(bat: 5, err: err);
        var pull = -SweetSpot.TipSign(input.Batter.Bats);
        Assert.Equal(9, AtBatResolver.ContactWindowFrames(null, park, false));
        for (var seed = 0; seed < 20; seed++)
        {
            var r = resolver.Resolve(input, park, new Random(seed));
            Assert.Equal(ContactQuality.Perfect, r.Quality);
            Assert.InRange(r.SprayDeg * pull, 40, 55);
        }
    }

    [Theory]
    [InlineData(0.25, -1)]
    [InlineData(0.11, 1)]
    [Trait("Kind", "Balance")]
    public void S08_PressesAtTheWindowsEdgesPullEarlyAndPushLate(double beforePlate, int side)
    {
        // plate − 0.25 is 4.2 frames early and plate − 0.11 is 4.2 frames late: inside the
        // 4.5-frame half window, on its unsquare rim (one tier down, never a miss).
        var resolver = new AtBatResolver(_content.Chemistry);
        var park = _content.Parks["harbor-diamond"];
        var err = PressFrames(beforePlate);
        Assert.Equal(side * 4.2, err, 8);
        var input = Input(bat: 5, err: err);
        var nice = _content.Rules.Batting.Spray.NiceSpreadDeg / 2;
        var expected = AtBatResolver.TimingSprayDeg(err, 9, input.Batter.Bats, _content.Rules);
        Assert.Equal(side, Math.Sign(expected * SweetSpot.TipSign(input.Batter.Bats)));
        for (var seed = 0; seed < 20; seed++)
        {
            var r = resolver.Resolve(input, park, new Random(seed));
            Assert.Equal(ContactQuality.Nice, r.Quality);
            Assert.InRange(r.SprayDeg, expected - nice - 0.1, expected + nice + 0.1);
        }
    }

    [Fact]
    [Trait("Kind", "Balance")]
    public void S09_FiveFramesLateIsOutsideTheNineFrameWindowAndAMiss()
    {
        var resolver = new AtBatResolver(_content.Chemistry);
        var park = _content.Parks["harbor-diamond"];
        var r = resolver.Resolve(Input(bat: 5, err: 5), park, new Random(1));
        Assert.Equal(ContactQuality.Miss, r.Quality);
        Assert.False(r.InPlay);
        // A press after the ball is on the plate (+0.05 s = 13.8 frames late) is a miss, and so
        // is plate − 0.10: 4.8 frames late is past the 4.5-frame half window. The old 0.10 s
        // square (#612) is now a miss — release has to lead the plate (#670).
        Assert.Equal(13.8, PressFrames(-0.05), 8);
        Assert.Equal(ContactQuality.Miss, resolver.Resolve(Input(bat: 5, err: PressFrames(-0.05)), park, new Random(1)).Quality);
        Assert.Equal(4.8, PressFrames(0.10), 8);
        Assert.Equal(ContactQuality.Miss, resolver.Resolve(Input(bat: 5, err: PressFrames(0.10)), park, new Random(1)).Quality);
        var s = new Scenario(_content);
        Assert.Equal(PlayKind.SwingMiss, s.Match.Play(Scenario.PitchAt(0, CenterY), Scenario.SwingAt(5)).Kind);
    }

    // ---------------------------------------------------------------------------------
    // S-10  The window is one number for everyone
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// #844, PH-10-R1; shipped by #860 — "trial was good.", September 22, 2026: every hitter and both
    /// swings share one window (the formula reads neither since #887), and a charmball is judged in
    /// the same window (PH-16-R1, PH-16-R18: no Star Pitch narrows it; S-190 holds every star pitch).
    /// No frame count is stored: the row reads the window from the table it is asserting about. The
    /// Bat 1 charged swing against a charmball that the split window floored (the off-path half) was
    /// retired by #887.
    /// </summary>
    [Fact]
    public void S10_OnTheShippedRootTheWindowIsOneNumberForEverySwingAndEveryHitter()
    {
        var park = _content.Parks["harbor-diamond"];
        var trial = _content.Rules;
        var frames = trial.Batting.Window.Frames;
        Assert.Equal(frames, AtBatResolver.ContactWindowFrames(null, park, false, trial, _content.StarSkills));

        // A Bat 1 and a Bat 10 hitter, quick and charged, miss at the same timing error.
        var resolver = new AtBatResolver(_content.Chemistry, trial, _content.StarSkills);
        foreach (var bat in new[] { 1, 5, 10 })
        foreach (var charge in new[] { 0.0, 1.0 })
        {
            Assert.NotEqual(ContactQuality.Miss, resolver.Resolve(Input(bat, frames / 2 - 0.1, charge), park, new Random(1)).Quality);
            Assert.Equal(ContactQuality.Miss, resolver.Resolve(Input(bat, frames / 2 + 0.1, charge), park, new Random(1)).Quality);
        }

        // The charmball is judged in the same window (PH-16-R18).
        Assert.Equal(frames, AtBatResolver.ContactWindowFrames("charmball", park, false, trial, _content.StarSkills));
    }

    // ---------------------------------------------------------------------------------
    // S-11 … S-13  The zones along the barrel, the pop-up rule, the stick
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S11_ChargedSwingWithTheBallTowardTheTipIsNiceAtTheChargeNiceExit()
    {
        var resolver = new AtBatResolver(_content.Chemistry);
        var park = _content.Parks["harbor-diamond"];
        var center = Input(bat: 5, err: 0);
        var tip = center with { Charge01 = 1, CrossingX = SweetSpot.TipSign(center.Batter.Bats) * 0.4 };
        var slap = resolver.Resolve(center, park, new Random(1));
        var charge = resolver.Resolve(tip, park, new Random(1));
        Assert.Equal(ContactQuality.Perfect, slap.Quality);
        Assert.Equal(ContactQuality.Nice, charge.Quality);
        var q = _content.Rules.Batting.Quality;
        Assert.Equal(q.Charge.Nice / q.Slap.Perfect, charge.ExitVeloMph / slap.ExitVeloMph, 2);
        Assert.Equal(1.12, q.Charge.Nice, 2);
    }

    [Fact]
    public void S12_OrdinarySourContactKeepsItsLaunchFromTheCrossing()
    {
        var resolver = new AtBatResolver(_content.Chemistry);
        var park = _content.Parks["harbor-diamond"];
        var b = _content.Rules.Batting;
        var sour = Input(bat: 5, err: -1);
        sour = sour with { CrossingX = -SweetSpot.TipSign(sour.Batter.Bats) * 0.9 };
        Assert.Equal(ContactQuality.Sour, resolver.Resolve(sour, park, new Random(1)).Quality);
        for (var seed = 0; seed < 10; seed++)
        {
            var onChange = resolver.Resolve(sour with { ChangeupPitch = true }, park, new Random(seed));
            var onCharge = resolver.Resolve(sour with { ChargePitch = true }, park, new Random(seed));
            var onPlain = resolver.Resolve(sour, park, new Random(seed));
            Assert.Equal(ContactQuality.Sour, onChange.Quality);
            Assert.Equal(onPlain.LaunchDeg, onChange.LaunchDeg);
            Assert.Equal(onPlain.LaunchDeg, onCharge.LaunchDeg);
        }
    }

    /// <summary>
    /// S-13, the shipped half (#855, PH-12; shipped by #883 after Jack accepted the stick trial on
    /// September 22, 2026: "approve all"): an ordinary swing ignores the stick at contact, so the same
    /// swing with the stick up and with it centered is the same launch — and the same ball — exactly.
    /// Nothing is stored: the two resolutions are compared to each other. The off-path half (stick up
    /// launched about <c>launch.stickDeg</c> lower) was retired by #887; the whole grid is
    /// <see cref="StickShapingScenarioTests"/> (S-128).
    /// </summary>
    [Fact]
    public void S13_OnTheShippedRootStickUpAndStickCenterLaunchTheSame()
    {
        var resolver = new AtBatResolver(_content.Chemistry, _content.Rules);
        var park = _content.Parks["harbor-diamond"];
        for (var seed = 0; seed < 20; seed++)
        {
            var flat = resolver.Resolve(Input(bat: 6, err: 0), park, new Random(seed));
            var up = resolver.Resolve(Input(bat: 6, err: 0) with { LaunchAim = 1 }, park, new Random(seed));
            Assert.NotEqual(ContactQuality.Miss, flat.Quality);
            Assert.Equal(flat.LaunchDeg, up.LaunchDeg);
            Assert.Equal(flat, up);
        }
    }

    // ---------------------------------------------------------------------------------
    // S-14 … S-15  A press during SET is not a swing; a press before release is an early miss
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S14_PressDuringSetIsNotASwingAndTheBatterCanStillSwingOnThePitch()
    {
        var held = ChargeButton.Advance(default, pressed: true, held: true, released: false,
            deltaSeconds: 0.2, secondsToFull: 0.45, commits: false);
        Assert.True(held.Next.Armed, "a hold during SET still builds the charge");
        var setRelease = ChargeButton.Advance(held.Next, pressed: false, held: false, released: true,
            deltaSeconds: 1.0 / 60, secondsToFull: 0.45, commits: false);
        Assert.False(setRelease.Committed);
        Assert.False(setRelease.Next.Armed);

        var press = ChargeButton.Advance(setRelease.Next, pressed: true, held: true, released: false,
            deltaSeconds: 1.0 / 60, secondsToFull: 0.45);
        var swing = ChargeButton.Advance(press.Next, pressed: false, held: false, released: true,
            deltaSeconds: 1.0 / 60, secondsToFull: 0.45);
        Assert.True(swing.Committed, "the same batter swings on the pitch");
    }

    [Fact]
    public void S15_PressATenthBeforeReleaseIsAnEarlyMissStrike()
    {
        const double plateAt = 1.0;
        var err = AtBatMotion.SwingErrorFrames(-0.1, plateAt);
        Assert.True(err < -9, $"early by {err} frames");
        var s = new Scenario(_content);
        var ev = s.Match.Play(Scenario.PitchAt(0, CenterY), Scenario.SwingAt(err));
        Assert.Equal(PlayKind.SwingMiss, ev.Kind);
        Assert.Equal(ContactQuality.Miss, ev.AtBat.Quality);
        Assert.Equal(1, s.Match.Strikes);
    }

    // ---------------------------------------------------------------------------------
    // S-16 … S-17  Hit by pitch is geometry the batter owns
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S16_WalkTheBoxAndTakeAPitchAtTheBodyIsHitByPitch()
    {
        var s = new Scenario(_content);
        var match = s.Match;
        var batter = match.Batter;
        match.Play(Scenario.PitchAt(1.6, CenterY), Scenario.Take);
        Assert.Equal((1, 0), (match.Balls, match.Strikes));
        Assert.True(match.WalkBatter(1.0));
        Assert.Equal(1.0, match.BatterOffsetX);
        var body = AtBatResolver.BatterBodyX(1.0, batter.Bats);
        Assert.NotEqual(body, AtBatResolver.BatterBodyX(0, batter.Bats));
        var ev = match.Play(Scenario.PitchAt(body, PitchFlight.PlateY), Scenario.Take);
        Assert.Equal(PlayKind.HitByPitch, ev.Kind);
        Assert.Equal(batter.Id, match.First?.Id);
        Assert.Equal((0, 0), (match.Balls, match.Strikes));
        Assert.Equal(0, match.BatterOffsetX);
    }

    [Fact]
    public void S16_HumanPitcherReachesTheBodyByWalkingTheRubberAndBreaking()
    {
        // Spec §4.6: rubber walked fully toward the batter's side plus full break toward them
        // reaches the body circle with the box centered. AimX is not a stick; only the walk and the break.
        var s = new Scenario(_content);
        var match = s.Match;
        var bats = match.Batter.Bats;
        var toward = -SweetSpot.TipSign(bats);
        Assert.True(match.WalkPitcher(toward * 1.0));
        var pitch = new PitchCommand("fastball", 0, false, RubberX: match.PitcherOffsetX, BreakX: toward);
        var (x, y) = PitchFlight.Crossing(pitch);
        Assert.True(AtBatResolver.HitsBatter(0, x, y, bats), $"crossing {x:0.00} vs body {AtBatResolver.BatterBodyX(0, bats):0.00}");
        var ev = match.Play(pitch, Scenario.Take);
        Assert.Equal(PlayKind.HitByPitch, ev.Kind);
        // Without the break the same walk is a ball, not a plunk.
        var t = new Scenario(_content);
        t.Match.WalkPitcher(toward * 1.0);
        var straight = new PitchCommand("fastball", 0, false, RubberX: t.Match.PitcherOffsetX);
        Assert.Equal(PlayKind.TakeBall, t.Match.Play(straight, Scenario.Take).Kind);
    }

    [Fact]
    public void S16_TheBoxRecentersAfterEveryPitch_ABallAStrikeAndAFoul()
    {
        // D12 (#607): the box is a per-pitch adjustment; the next SET starts centered whatever the pitch did.
        var s = new Scenario(_content);
        var match = s.Match;
        var batter = match.Batter;

        Assert.True(match.WalkBatter(0.8));
        Assert.Equal(0.8, match.BatterOffsetX);
        Assert.Equal(PlayKind.TakeBall, match.Play(Scenario.PitchAt(1.6, CenterY), Scenario.Take).Kind);
        Assert.Equal((1, 0), (match.Balls, match.Strikes));
        Assert.Equal(0, match.BatterOffsetX);

        Assert.True(match.WalkBatter(0.8));
        Assert.Equal(PlayKind.SwingMiss, match.Play(Scenario.PitchAt(0, CenterY), Scenario.SwingAt(20)).Kind);
        Assert.Equal((1, 1), (match.Balls, match.Strikes));
        Assert.Equal(0, match.BatterOffsetX);

        Assert.True(match.WalkBatter(0.8));
        var foul = FlightFixtures.Hit(match.Park, 90, 20, 60, ContactQuality.Nice);
        Assert.True(foul.Foul);
        var preview = match.PreviewHit(foul);
        var play = match.FinishAtBat(Scenario.Paint, Scenario.Swing, foul, match.ResolveFielding(foul, preview));
        Assert.Equal(PlayKind.Foul, play.Kind);
        Assert.Equal(batter.Id, match.Batter.Id);
        Assert.Equal((1, 2), (match.Balls, match.Strikes));
        Assert.Equal(0, match.BatterOffsetX);
    }

    [Fact]
    public void S16_TheContactOffsetLatchesTheWalkTheSwingUsedThenTheBoxRecenters()
    {
        var s = new Scenario(_content);
        var match = s.Match;
        var batter = match.Batter;
        Assert.True(match.WalkBatter(0.8));
        // A ball met on the cursor where the walked box put it.
        var sweet = SweetSpot.WorldCenter(match.BatterOffsetX);
        var ev = match.Play(Scenario.PitchAt(sweet.X, CenterY), Scenario.SwingAt(0));
        Assert.NotEqual(PlayKind.SwingMiss, ev.Kind);
        Assert.NotEqual(PlayKind.TakeBall, ev.Kind);
        Assert.Equal(0.8, match.BatterContactOffsetX, 9);
        Assert.Equal(0, match.BatterOffsetX);
        _ = batter;
    }

    [Fact]
    public void S17_SwingingAtThePitchAtTheBodyIsAStrikeNotHitByPitch()
    {
        var s = new Scenario(_content);
        var match = s.Match;
        match.WalkBatter(1.0);
        var body = AtBatResolver.BatterBodyX(1.0, match.Batter.Bats);
        var ev = match.Play(Scenario.PitchAt(body, PitchFlight.PlateY), Scenario.SwingAt(0));
        Assert.Equal(PlayKind.SwingMiss, ev.Kind);
        Assert.Null(match.First);
        Assert.Equal(1, match.Strikes);
    }

    // ---------------------------------------------------------------------------------
    // S-18 … S-19  Bunt on the bat plane through the cursor
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S18_FoulBuntWithTwoStrikesIsAStrikeout()
    {
        var strikeouts = 0;
        var caughtPops = 0;
        var fouls = 0;
        for (var seed = 1; seed <= 200; seed++)
        {
            var s = new Scenario(_content, seed);
            var match = s.Match;
            match.Play(Scenario.PitchAt(0, CenterY), Scenario.Take);
            match.Play(Scenario.PitchAt(0, CenterY), Scenario.Take);
            Assert.Equal(2, match.Strikes);
            var batter = match.Batter;
            var handle = -SweetSpot.TipSign(batter.Bats) * 0.9;
            var ev = match.Play(Scenario.PitchAt(handle, CenterY), Scenario.SwingAt(0, bunt: true, stickX: 1));
            if (ev.Kind == PlayKind.Foul) fouls++;
            if (!ev.AtBat.Foul) continue;
            // A foul bunt is live like any flight (§7.11): popped up and caught it is a fly out (§7.3);
            // dead on the ground with two strikes it is strike three (§1, §5.8). Either way the batter is out.
            if (ev.Kind == PlayKind.FlyOut)
            {
                caughtPops++;
                Assert.Equal(BattedBallClass.Pop, ev.AtBat.Class);
            }
            else
            {
                strikeouts++;
                Assert.Equal(PlayKind.Strikeout, ev.Kind);
                Assert.Contains("bunts foul", ev.Caption);
            }
            Assert.Equal(1, match.Outs);
            Assert.NotEqual(batter.Id, match.Batter.Id);
        }
        Assert.Equal(0, fouls);
        Assert.True(strikeouts + caughtPops > 5, $"a pulled sour bunt goes foul sometimes: {strikeouts} + {caughtPops} of 200");
    }

    [Fact]
    public void S19_AHeldBuntOnAHighPitchIsABuntPopAndTheHeldBatHasNoPressToTime()
    {
        var resolver = new AtBatResolver(_content.Chemistry);
        var park = _content.Parks["harbor-diamond"];
        var b = _content.Rules.Batting;
        var high = Input(bat: 5, err: 0, crossingY: CenterY + b.Bunt.PopAboveCenterFt + 0.1)
            with { Bunt = true, BuntSide = BuntSide.First };
        var low = high with { CrossingY = CenterY };
        for (var seed = 0; seed < 10; seed++)
        {
            var pop = resolver.Resolve(high, park, new Random(seed));
            var roll = resolver.Resolve(low, park, new Random(seed));
            Assert.NotEqual(ContactQuality.Miss, pop.Quality);
            Assert.True(pop.LaunchDeg >= b.Launch.PopMinDeg, $"bunt pop {pop.LaunchDeg}");
            Assert.True(roll.LaunchDeg <= b.Bunt.LaunchMinDeg + b.Bunt.LaunchSpanDeg, $"bunt roll {roll.LaunchDeg}");
            Assert.False(pop.HomeRun);
            // The held bat is already on the plane (§5.8, PH-14-R4): no error a caller writes moves either ball.
            foreach (var err in new[] { -30.0, 30 })
            {
                Assert.Equal(pop, resolver.Resolve(high with { TimingErrorFrames = err }, park, new Random(seed)));
                Assert.Equal(roll, resolver.Resolve(low with { TimingErrorFrames = err }, park, new Random(seed)));
            }
        }
        // Off the bat is still off the bat: a bunt needs the cursor (spec §5.8).
        var off = high with { CrossingX = SweetSpot.TipSign(high.Batter.Bats) * 2.6 };
        Assert.Equal(ContactQuality.Miss, resolver.Resolve(off, park, new Random(1)).Quality);
    }

    // ---------------------------------------------------------------------------------
    // S-25 … S-26  Stamina is the pitcher's own arm
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S25_TiredPitcherHasFadedSpeedAndSteeringRoomButNeverMissesAtRandom()
    {
        var s = new Scenario(_content);
        var match = s.Match;
        var st = _content.Rules.Pitching.Stamina;
        var pitcher = match.Pitcher;
        Assert.Equal(st.PoolBase + pitcher.Stats.Pitch * st.PoolPerPitch, match.PitcherStaminaMax);
        Assert.Equal(match.PitcherStaminaMax, match.PitcherStamina);
        var fresh = match.PitchSpeedMph(Scenario.Paint);

        // Work the arm down to 20 with normal pitches taken for balls (cost pitchCost each).
        var outside = Scenario.PitchAt(2.5, CenterY);
        var guard = 0;
        while (match.PitcherStamina > 20 && guard++ < 200) match.Play(outside, Scenario.Take);
        Assert.InRange(match.PitcherStamina, 20 - st.PitchCost, 20);
        Assert.True(match.PitcherTired);
        Assert.False(match.PitcherExhausted);
        Assert.Equal(fresh - st.MphLost(match.PitcherStamina), match.PitchSpeedMph(Scenario.Paint), 6);
        Assert.True(match.PitchSpeedMph(Scenario.Paint) < fresh - st.ExhaustedMph / 2, "past the middle of the fade at 20");
        Assert.True(BroadcastHud.PoorArm(match.PitcherStamina, match.Rules), "the card reads TIRED");
        Assert.Contains("TIRED", BroadcastHud.ArmLine(match.PitcherStamina, match.Rules));

        // No random miss (PH-08-R1): the aim is untouched, pitch after pitch, and only the break is damped.
        var aimed = Scenario.PitchAt(0.3, CenterY) with { BreakX = 1 };
        var ready = match.PreparePitch(aimed);
        Assert.Equal((aimed.AimX, aimed.AimY), (ready.AimX, ready.AimY));
        for (var i = 0; i < 20; i++)
            Assert.Equal(PitchFlight.Crossing(ready), PitchFlight.Crossing(match.PreparePitch(aimed)));
        var breakMul = st.BreakMul(match.PitcherStamina);
        Assert.Equal(breakMul, ready.BreakMul, 12);
        Assert.InRange(breakMul, st.TiredBreakMul, 1 - 1e-9);
        var damped = PitchFlight.Crossing(ready).X - PitchFlight.Crossing(ready with { BreakX = 0 }).X;
        Assert.Equal(_content.Rules.Pitching.Flight.BreakMaxFt * breakMul, damped, 6);
    }

    [Fact]
    public void S25_EveryPitchVerbCostsTheArmFromTheTable()
    {
        var s = new Scenario(_content);
        var match = s.Match;
        var st = _content.Rules.Pitching.Stamina;
        var full = match.PitcherStamina;
        match.Play(Scenario.PitchAt(2.5, CenterY), Scenario.Take);
        Assert.Equal(full - st.PitchCost, match.PitcherStamina);
        match.Play(Scenario.PitchAt(2.5, CenterY, charge: 1), Scenario.Take);
        Assert.Equal(full - 2 * st.PitchCost - st.ChargeCost, match.PitcherStamina);
        match.Play(Scenario.PitchAt(2.5, CenterY, family: PitchFamily.Changeup), Scenario.Take);
        Assert.Equal(full - 3 * st.PitchCost - st.ChargeCost
            - _content.Rules.Pitching.Families.Of(PitchFamily.Changeup).StaminaCost, match.PitcherStamina);
        // A star costs its skill's staminaCost from star-skills.json, never a C# literal.
        var star = new Scenario(_content);
        var skill = StarSkills.StaminaCost(star.Match.Pitcher.StarPitch, _content.StarSkills);
        Assert.True(skill > 0);
        Assert.Equal(skill, _content.StarSkills.Pitch(star.Match.Pitcher.StarPitch)!.StaminaCost);
        Assert.True(star.Match.CanStarPitch);
        var before = star.Match.PitcherStamina;
        star.Match.Play(Scenario.PitchAt(2.5, CenterY) with { Star = true }, Scenario.Take);
        Assert.Equal(before - st.PitchCost - skill, star.Match.PitcherStamina);
    }

    /// <summary>
    /// PH-08-R3: only pitching costs the arm. A homer and the runs it drives in cost nothing past the pitch
    /// that was hit, and a run forced in by a walk costs nothing past the four balls.
    /// </summary>
    [Fact]
    public void S25_AHomerOrARunAllowedCostsTheArmNothing()
    {
        var st = _content.Rules.Pitching.Stamina;
        var slam = new Scenario(_content).Runner(1, 1).Runner(2, 2).Runner(3, 3);
        var match = slam.Match;
        var full = match.PitcherStamina;
        slam.Contact();
        Assert.Equal(full - st.PitchCost, match.PitcherStamina);
        var hit = FlightFixtures.OverTheFence(match.Park, 20, 0);
        var field = new FieldingResult(PlayKind.HomeRun, null, null, 4, 0, 420, false, false);
        var play = match.FinishAtBat(Scenario.Paint, Scenario.Swing, hit, field);
        Assert.Equal(PlayKind.HomeRun, play.Kind);
        Assert.Equal(4, play.RunsScored);
        Assert.Equal(full - st.PitchCost, match.PitcherStamina);

        var walk = new Scenario(_content).Runner(1, 1).Runner(2, 2).Runner(3, 3).Match;
        var fresh = walk.PitcherStamina;
        var wide = Scenario.PitchAt(2.5, CenterY);
        PlayEvent? last = null;
        for (var i = 0; i < 4; i++) last = walk.Play(wide, Scenario.Take);
        Assert.Equal(PlayKind.Walk, last!.Kind);
        Assert.Equal(1, last.RunsScored);
        Assert.Equal(fresh - 4 * st.PitchCost, walk.PitcherStamina);
    }

    [Fact]
    public void S26_SwapGivesTheNewPitcherTheirOwnPoolAndTheOldPitcherTheVacatedGlove()
    {
        var s = new Scenario(_content);
        var match = s.Match;
        var old = match.Pitcher;
        var outside = Scenario.PitchAt(2.5, CenterY);
        for (var i = 0; i < 6; i++) match.Play(outside, Scenario.Take);
        var spent = match.PitcherStamina;
        Assert.True(spent < match.PitcherStaminaMax);

        var gloves = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher);
        var next = gloves["SS"];
        var vacated = "SS";
        Assert.True(match.CanSwapPitcher);
        Assert.True(match.SwapPitcher(next));
        Assert.Equal(next.Id, match.Pitcher.Id);
        Assert.Equal(match.StaminaPool(next), match.PitcherStamina);
        Assert.Equal(spent, match.StaminaOf(old));
        var after = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher);
        Assert.Equal(old.Id, after[vacated].Id);
        Assert.Equal(next.Id, after["P"].Id);
        foreach (var pos in Diamond.Order.Where(p => p is not ("P" or "SS")))
            Assert.Equal(gloves[pos].Id, after[pos].Id);
        Assert.False(match.CanSwapPitcher, "once per half-inning");
        Assert.False(match.SwapPitcher());
    }

    /// <summary>
    /// PH-08-R2: fatigue is the character's for the match. The arm that leaves the mound keeps what it spent
    /// through the halves it sits on a glove, and takes the mound back with exactly that pool; the reliever
    /// keeps what it spent too. Nothing rests an arm and no swap resets one.
    /// </summary>
    [Fact]
    public void S146_AReturningArmKeepsItsFatigue()
    {
        var match = new Scenario(_content).Match;
        var outside = Scenario.PitchAt(2.5, CenterY);
        var strike = Scenario.PitchAt(0, CenterY);
        Assert.True(match.Top);
        var starter = match.Pitcher;
        for (var i = 0; i < 3; i++) match.Play(outside, Scenario.Take);
        var spent = match.PitcherStamina;
        Assert.True(spent < match.StaminaPool(starter));

        var reliever = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher)["SS"];
        Assert.True(match.SwapPitcher(reliever));
        EndHalf(match, strike);
        Assert.False(match.Top);
        EndHalf(match, strike);
        Assert.True(match.Top);
        Assert.Equal(reliever.Id, match.Pitcher.Id);
        var relieverSpent = match.PitcherStamina;
        Assert.True(relieverSpent < match.StaminaPool(reliever));
        Assert.Equal(spent, match.StaminaOf(starter));

        Assert.True(match.SwapPitcher(starter));
        Assert.Equal(starter.Id, match.Pitcher.Id);
        Assert.Equal(spent, match.PitcherStamina);
        Assert.Equal(relieverSpent, match.StaminaOf(reliever));
    }

    /// <summary>Two outs, then three called strikes: the half ends on the arm that is on the mound.</summary>
    static void EndHalf(Match match, PitchCommand strike)
    {
        var top = match.Top;
        Assert.True(match.SetOuts(2));
        var guard = 0;
        while (match.Top == top && guard++ < 10) match.Play(strike, Scenario.Take);
        Assert.NotEqual(top, match.Top);
    }

    // ---------------------------------------------------------------------------------
    // S-27 … S-29  The CPU tables
    // ---------------------------------------------------------------------------------

    [Fact]
    [Trait("Kind", "Balance")]
    public void S27_CpuPitcherAheadZeroTwoWastesAtLeastThirtyPercentOutsideTheZone()
    {
        var s = new Scenario(_content, seed: 27);
        var match = s.Match;
        match.Play(Scenario.PitchAt(0, CenterY), Scenario.Take);
        match.Play(Scenario.PitchAt(0, CenterY), Scenario.Take);
        Assert.Equal((0, 2), (match.Balls, match.Strikes));
        Assert.Same(_content.Rules.Pitching.Cpu.Ahead, match.CpuPitchRow());
        var outside = 0;
        for (var i = 0; i < 100; i++)
            if (!StrikeZoneGeometry.Contains(match.CpuPitch(), match.Pitcher.StarPitch)) outside++;
        Assert.True(outside >= 30, $"{outside} of 100 outside");

        // No dead-center default: an even count never aims at the middle.
        var even = new Scenario(_content, seed: 28).Match;
        Assert.Same(_content.Rules.Pitching.Cpu.Even, even.CpuPitchRow());
        var center = 0;
        for (var i = 0; i < 100; i++)
        {
            var (x, y) = PitchFlight.Crossing(even.CpuPitch(), even.Pitcher.StarPitch, even.Rules);
            if (Math.Abs(x) < 0.25 && Math.Abs(y - CenterY) < 0.25) center++;
        }
        Assert.True(center < 10, $"{center} of 100 down the middle");
    }

    [Fact]
    [Trait("Kind", "Balance")]
    public void S28_CpuBatterMeetsAHumanMeatballPerfectlySometimesWithNoForcedMissClamp()
    {
        var perfect = 0;
        var square = 0;
        var swings = 0;
        for (var seed = 1; seed <= 100; seed++)
        {
            var match = Match.Exhibition(_content, "rio", "ashlord", seed: seed);
            Assert.True(match.Top, "the human pitches the top");
            var meat = Scenario.PitchAt(0, CenterY);
            var swing = match.CpuSwing(meat);
            if (!swing.Swing) continue;
            swings++;
            if (Math.Abs(swing.TimingErrorFrames) < 3.2) square++;
            if (match.Play(meat, swing).AtBat.Quality == ContactQuality.Perfect) perfect++;
        }
        Assert.True(swings >= 90, $"middle third is a swing: {swings} of 100");
        Assert.True(perfect > 0, "perfect rate > 0");
        Assert.True(square > swings / 3, $"no |err| ≥ 3.2 floor: {square} square of {swings}");
    }

    [Fact]
    public void S28_CpuSwingHasNoSideEffectsAndTheStealArmIsARunnerVerb()
    {
        var s = new Scenario(_content, seed: 4).Runner(1, 3);
        var match = s.Match;
        var stealBefore = match.StealOn;
        var box = match.BatterOffsetX;
        var rubber = match.PitcherOffsetX;
        var stream = s.Stream();
        for (var i = 0; i < 50; i++)
            match.CpuSwing(Scenario.PitchAt(0.3, CenterY));
        Assert.Equal(stealBefore, match.StealOn);
        Assert.Equal(box, match.BatterOffsetX);
        Assert.Equal(rubber, match.PitcherOffsetX);
        Assert.Equal(stream, s.Stream());
    }

    [Fact]
    [Trait("Kind", "Balance")]
    public void S28_CpuBatterMistracksMoreAfterTheRubberMoved()
    {
        // Two matches, same seeds: one where the pitcher stayed, one where the rubber moved since
        // the last pitch. The moved rubber puts more CPU boxes off the crossing (spec §5.9).
        var offStill = 0;
        var offMoved = 0;
        for (var seed = 1; seed <= 200; seed++)
        {
            offStill += OffCursor(seed, moveRubber: false);
            offMoved += OffCursor(seed, moveRubber: true);
        }
        Assert.True(offMoved > offStill * 1.3, $"moved {offMoved} vs still {offStill}");
    }

    int OffCursor(int seed, bool moveRubber)
    {
        var match = new Scenario(_content, seed).Match;
        match.Play(Scenario.PitchAt(-0.6, CenterY), Scenario.Take);
        if (moveRubber) match.WalkPitcher(0.5);
        Assert.Equal(moveRubber, match.RubberMovedSinceLastPitch);
        var pitch = Scenario.PitchAt(0.6, CenterY);
        var swing = match.CpuSwing(pitch);
        if (!swing.Swing) return 0;
        // A failed re-read leaves the box at the last crossing (1.2 ft away); a fixed offset alone is under 0.5 ft.
        var (cx, _) = PitchFlight.Crossing(pitch);
        return Math.Abs(SweetSpot.WorldCenter(swing.BoxOffsetX).X - cx) > 0.5 ? 1 : 0;
    }

    [Fact]
    [Trait("Kind", "Balance")]
    [Trait("Cost", "Heavy")]
    public void S29_FiftySeedCpuGamesLandInTheBand()
    {
        // 50 three-inning CPU-vs-CPU games across the captain pairs, each pair played both ways
        // (one pair one way is a roster mismatch by design). Per side is the away mean and the
        // home mean over the sample.
        var pairs = new[] { ("rio", "ashlord"), ("zig", "konga"), ("fenn", "brondo"), ("konga", "rio"), ("vale", "brondo") };
        var games = 0;
        var away = 0;
        var home = 0;
        var mostRuns = 0;
        var kinds = new Dictionary<PlayKind, int>();
        foreach (var (x, y) in pairs)
        foreach (var (h, a) in new[] { (x, y), (y, x) })
        for (var seed = 1; seed <= 5; seed++)
        {
            var match = Match.Exhibition(_content, h, a, innings: 3, seed: seed);
            match.AutoPlayGame();
            Assert.True(match.Over);
            games++;
            away += match.AwayScore;
            home += match.HomeScore;
            mostRuns = Math.Max(mostRuns, Math.Max(match.AwayScore, match.HomeScore));
            foreach (var ev in match.Log) kinds[ev.Kind] = kinds.GetValueOrDefault(ev.Kind) + 1;
        }
        Assert.Equal(50, games);
        var meanAway = away / (double)games;
        var meanHome = home / (double)games;
        var singles = kinds.GetValueOrDefault(PlayKind.Single) / (double)games;
        var doubles = kinds.GetValueOrDefault(PlayKind.Double) / (double)games;
        var homers = kinds.GetValueOrDefault(PlayKind.HomeRun) / (double)games;
        var strikeouts = kinds.GetValueOrDefault(PlayKind.Strikeout) / (double)games;
        var walks = kinds.GetValueOrDefault(PlayKind.Walk) / (double)games;
        var line = $"runs {meanAway:0.00} / {meanHome:0.00}, singles {singles:0.00}, doubles {doubles:0.00}, HR {homers:0.00}, K {strikeouts:0.00}, BB {walks:0.00}, most {mostRuns}";
        // The band (§B.1 S-29, P7 #569): a three-inning arcade game that reads like baseball. Tuned in
        // data/rules only: the liner's own stretch (flight.linerTimeScale), the outfielder's chase on a ball in
        // the air (fielding.chase.outfieldAirMul, #609; the read itself is the reference 0.83 s), the infielder's
        // under a fly or a pop (fielding.chase.infieldAirMul, #636: the hand-off honours the infielder's route in
        // the air, so the infield's reach back under a short fly is the lever, never the liner it can reach), the bat
        // (batting.exit), the CPU arm's scatter (pitching.cpu). #667: CF meeting the wall instead of RF chasing
        // the bounce converted doubles to singles (1.94 / 1.88 on these seeds). Do not send RF the bounce or
        // give the liner away to hold 2.2; the floor is 1.8 so the sitting stays the lever.
        Assert.True(meanAway is >= 1.8 and <= 5, line);
        Assert.True(meanHome is >= 1.8 and <= 5, line);
        Assert.True(doubles < singles, line);
        Assert.True(homers <= 2, line);
        Assert.True(strikeouts > 0 && walks > 0, line);
        // The tail is reported, not gated: a mismatch pair in a bandbox (Konga's lineup at Funfair) still
        // posts a double-digit half-game on some seeds, and the mercy rule does not run in three innings (§1).
        Assert.True(mostRuns > 0, line);
    }

    // ---------------------------------------------------------------------------------
    // S-30  The Charge Bat is never worse than a manual MAX
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The shipped half (#844; shipped by #860, "trial was good.", September 22, 2026): the Charge Bat's **spatial** clause is untouched — it still keeps the
    /// wide slap zones on a MAX charge, which is the half PH-11-R1 says a charge trades. Its window
    /// clause has nothing left to buy: a manual MAX, the Charge Bat and a quick swing are all judged
    /// in the same window, so "no window penalty" is true because there is no penalty for anyone.
    /// The split-window half (the Charge Bat kept the slap window) was retired by #887.
    /// </summary>
    [Fact]
    public void S30_OnTheShippedRootTheChargeBatKeepsItsZonesAndSharesEveryonesWindow()
    {
        var trial = _content.Rules;
        var park = _content.Parks["harbor-diamond"];
        var resolver = new AtBatResolver(_content.Chemistry, trial, _content.StarSkills);
        var manual = Input(bat: 5, err: 0, charge: 1);
        var chargeBat = Input(bat: 5, err: 0, charge: 0, batId: "charge-bat");
        var quick = Input(bat: 5, err: 0);
        Assert.True(_content.Bats["charge-bat"].ChargeAlwaysFull);

        var frames = AtBatResolver.ContactWindowFrames(null, park, false, trial, _content.StarSkills);

        // The spatial clause still holds: the ball toward the tip is Nice on a manual charge and
        // Perfect with the bat, exactly as on the shipped root.
        var tip = SweetSpot.TipSign(manual.Batter.Bats) * 0.4;
        Assert.Equal(ContactQuality.Nice, resolver.Resolve(manual with { CrossingX = tip }, park, new Random(1)).Quality);
        Assert.Equal(ContactQuality.Perfect, resolver.Resolve(chargeBat with { CrossingX = tip }, park, new Random(1)).Quality);
        Assert.True(resolver.Resolve(chargeBat, park, new Random(1)).ExitVeloMph
                    >= resolver.Resolve(manual, park, new Random(1)).ExitVeloMph,
            "the Charge Bat is still never worse than a manual MAX");

        // …and one window for all three: just inside its edge none misses, just outside all do.
        foreach (var swing in new[] { manual, chargeBat, quick })
        {
            Assert.NotEqual(ContactQuality.Miss,
                resolver.Resolve(swing with { TimingErrorFrames = frames / 2 - 0.1 }, park, new Random(1)).Quality);
            Assert.Equal(ContactQuality.Miss,
                resolver.Resolve(swing with { TimingErrorFrames = frames / 2 + 0.1 }, park, new Random(1)).Quality);
        }
    }

    // ---------------------------------------------------------------------------------

    AtBatInput Input(int bat, double err, double charge = 0, double crossingY = double.NaN,
        bool changeup = false, Character? pitcher = null, string batId = "harbor-lumber")
    {
        var batter = _content.Must("rio");
        batter = batter with { Stats = batter.Stats with { Bat = bat } };
        // A Pitch-5 arm so the pitch factor (spec §5.5) is ×1 unless a row asks for it.
        var arm = pitcher ?? _content.Must("vale");
        arm = arm with { Stats = arm.Stats with { Pitch = 5 } };
        return new AtBatInput(
            arm, batter, null, [],
            ChargePitch: false, ChangeupPitch: changeup, TimingErrorFrames: err,
            UseStarPitch: false, UseStarSwing: false, Bat: _content.Bats[batId], PitcherStamina: 80,
            Charge01: charge, CrossingX: 0, CrossingY: double.IsNaN(crossingY) ? CenterY : crossingY);
    }
}
