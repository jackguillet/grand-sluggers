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
    // S-04  The CPU batter decides from the final crossing
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S04_CpuBatterReadsTheSteeredCrossingAndTakesAPitchSteeredOut()
    {
        // A human pitcher on the edge steers full break out of the zone during flight. The CPU
        // decides from the pitch as it stands at the plate plane, so it takes at (100 − chase)%.
        var chase = _content.Rules.Batting.Cpu.ChaseChance;
        var takes = 0;
        const int n = 300;
        for (var seed = 1; seed <= n; seed++)
        {
            var s = new Scenario(_content, seed);
            var match = s.Match;
            var edge = match.PreparePitch(Scenario.PitchAt(StrikeZoneGeometry.HalfWidth - 0.1, CenterY));
            Assert.True(StrikeZoneGeometry.Contains(edge), "the launched pitch is a strike");
            var steered = edge with { BreakX = 1 };
            Assert.False(StrikeZoneGeometry.Contains(steered), "full break carries it out");
            var swing = match.CpuSwing(steered, AtBatResolver.PitchInZone(steered, match.Pitcher.Stats.Pitch, match.Pitcher.StarPitch));
            if (!swing.Swing) takes++;
        }
        Assert.InRange(takes / (double)n, 1 - chase - 0.08, 1 - chase + 0.08);

        // The decision instant is before the latest square press, and a CPU swing never claims an earlier bat.
        var plateAt = 1.0;
        var decide = AtBatMotion.CpuDecisionTime(plateAt, _content.Rules);
        Assert.True(decide < plateAt - Motion.SwingContact);
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
    // S-07 … S-09  Cursor decides quality, timing decides direction, the window is the window
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S07_SquareBatFiveSlapAtTheCenterIsPerfectStraightToCenter()
    {
        var resolver = new AtBatResolver(_content.Chemistry);
        var park = _content.Parks["harbor-diamond"];
        var spread = _content.Rules.Batting.Spray.PerfectSpreadDeg / 2;
        for (var seed = 0; seed < 20; seed++)
        {
            var r = resolver.Resolve(Input(bat: 5, err: 0), park, new Random(seed));
            Assert.Equal(ContactQuality.Perfect, r.Quality);
            Assert.InRange(r.SprayDeg, -spread, spread);
            Assert.True(r.InPlay);
        }
    }

    [Fact]
    public void S08_FourFramesEarlyInsideTheNineFrameWindowIsStillPerfectAndPulled()
    {
        var resolver = new AtBatResolver(_content.Chemistry);
        var park = _content.Parks["harbor-diamond"];
        var input = Input(bat: 5, err: -4);
        var pull = -SweetSpot.TipSign(input.Batter.Bats);
        Assert.Equal(9, AtBatResolver.ContactWindowFrames(5, false, null, park, false));
        for (var seed = 0; seed < 20; seed++)
        {
            var r = resolver.Resolve(input, park, new Random(seed));
            Assert.Equal(ContactQuality.Perfect, r.Quality);
            Assert.InRange(r.SprayDeg * pull, 40, 55);
        }
    }

    [Fact]
    public void S09_FiveFramesLateIsOutsideTheNineFrameWindowAndAMiss()
    {
        var resolver = new AtBatResolver(_content.Chemistry);
        var park = _content.Parks["harbor-diamond"];
        var r = resolver.Resolve(Input(bat: 5, err: 5), park, new Random(1));
        Assert.Equal(ContactQuality.Miss, r.Quality);
        Assert.False(r.InPlay);
        var s = new Scenario(_content);
        Assert.Equal(PlayKind.SwingMiss, s.Match.Play(Scenario.PitchAt(0, CenterY), Scenario.SwingAt(5)).Kind);
    }

    // ---------------------------------------------------------------------------------
    // S-10  The window is floored
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S10_BatOneChargedAgainstACharmballStillHasAFiveFrameWindow()
    {
        var park = _content.Parks["harbor-diamond"];
        var window = AtBatResolver.ContactWindowFrames(1, true, "charmball", park, false);
        Assert.Equal(_content.Rules.Batting.Window.FloorFrames, window);
        Assert.True(window >= 5);
        var raw = (7 + (1 - 5) * 0.4) * StarSkills.BatterWindowMul("charmball");
        Assert.True(raw < 5, $"the floor is doing work: raw {raw}");

        var charmer = _content.Characters.Values.First(c => c.StarPitch == "charmball");
        var resolver = new AtBatResolver(_content.Chemistry);
        var inside = Input(bat: 1, err: 2.4, charge: 1, pitcher: charmer) with { UseStarPitch = true };
        var outside = inside with { TimingErrorFrames = 2.6 };
        Assert.NotEqual(ContactQuality.Miss, resolver.Resolve(inside, park, new Random(1)).Quality);
        Assert.Equal(ContactQuality.Miss, resolver.Resolve(outside, park, new Random(1)).Quality);
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
    public void S12_SourSlapOnAChangeupOrAChargedPitchIsAPopUp()
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
            Assert.True(onChange.LaunchDeg >= b.Launch.PopMinDeg, $"changeup pop {onChange.LaunchDeg}");
            Assert.True(onCharge.LaunchDeg >= b.Launch.PopMinDeg, $"charged-pitch pop {onCharge.LaunchDeg}");
            Assert.True(onPlain.LaunchDeg <= b.Launch.TopperMinDeg + b.Launch.TopperSpanDeg,
                $"an early sour slap on a plain pitch tops it: {onPlain.LaunchDeg}");
        }
    }

    [Fact]
    public void S13_StickUpAtContactLaunchesAboutTwelveDegreesLower()
    {
        var resolver = new AtBatResolver(_content.Chemistry);
        var park = _content.Parks["harbor-diamond"];
        var diffs = new List<double>();
        for (var seed = 0; seed < 20; seed++)
        {
            var flat = resolver.Resolve(Input(bat: 6, err: 0), park, new Random(seed));
            var up = resolver.Resolve(Input(bat: 6, err: 0) with { LaunchAim = 1 }, park, new Random(seed));
            Assert.Equal(flat.Quality, up.Quality);
            diffs.Add(flat.LaunchDeg - up.LaunchDeg);
        }
        Assert.InRange(diffs.Average(), 10, _content.Rules.Batting.Launch.StickDeg + 0.5);
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
        var pitch = new PitchCommand("fastball", 0, 0, false, RubberX: match.PitcherOffsetX, BreakX: toward);
        var (x, y) = PitchFlight.Crossing(pitch);
        Assert.True(AtBatResolver.HitsBatter(0, x, y, bats), $"crossing {x:0.00} vs body {AtBatResolver.BatterBodyX(0, bats):0.00}");
        var ev = match.Play(pitch, Scenario.Take);
        Assert.Equal(PlayKind.HitByPitch, ev.Kind);
        // Without the break the same walk is a ball, not a plunk.
        var t = new Scenario(_content);
        t.Match.WalkPitcher(toward * 1.0);
        var straight = new PitchCommand("fastball", 0, 0, false, RubberX: t.Match.PitcherOffsetX);
        Assert.Equal(PlayKind.TakeBall, t.Match.Play(straight, Scenario.Take).Kind);
    }

    [Fact]
    public void S16_TheBoxPersistsAcrossPitchesOfOneAtBat()
    {
        var s = new Scenario(_content);
        var match = s.Match;
        match.WalkBatter(0.6);
        match.Play(Scenario.PitchAt(1.6, CenterY), Scenario.Take);
        Assert.Equal(0.6, match.BatterOffsetX);
        match.Play(Scenario.PitchAt(0, CenterY), Scenario.SwingAt(20));
        Assert.Equal(0.6, match.BatterOffsetX);
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
            strikeouts++;
            Assert.Equal(PlayKind.Strikeout, ev.Kind);
            Assert.Contains("bunts foul", ev.Caption);
            Assert.Equal(1, match.Outs);
            Assert.NotEqual(batter.Id, match.Batter.Id);
        }
        Assert.Equal(0, fouls);
        Assert.True(strikeouts > 5, $"a pulled sour bunt goes foul sometimes: {strikeouts} of 200");
    }

    [Fact]
    public void S19_BuntOnAHighPitchIsABuntPop()
    {
        var resolver = new AtBatResolver(_content.Chemistry);
        var park = _content.Parks["harbor-diamond"];
        var b = _content.Rules.Batting;
        var high = Input(bat: 5, err: 0, crossingY: CenterY + b.Bunt.PopAboveCenterFt + 0.1) with { Bunt = true };
        var low = high with { CrossingY = CenterY };
        for (var seed = 0; seed < 10; seed++)
        {
            var pop = resolver.Resolve(high, park, new Random(seed));
            var roll = resolver.Resolve(low, park, new Random(seed));
            Assert.NotEqual(ContactQuality.Miss, pop.Quality);
            Assert.True(pop.LaunchDeg >= b.Launch.PopMinDeg, $"bunt pop {pop.LaunchDeg}");
            Assert.True(roll.LaunchDeg <= b.Bunt.LaunchMinDeg + b.Bunt.LaunchSpanDeg, $"bunt roll {roll.LaunchDeg}");
            Assert.False(pop.HomeRun);
        }
        // Off the bat is still off the bat: a bunt needs the cursor (spec §5.8).
        var off = high with { CrossingX = SweetSpot.TipSign(high.Batter.Bats) * 2.6 };
        Assert.Equal(ContactQuality.Miss, resolver.Resolve(off, park, new Random(1)).Quality);
    }

    // ---------------------------------------------------------------------------------
    // S-30  The Charge Bat is never worse than a manual MAX
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S30_ChargeBatIsAtLeastAManualMaxWithNoWindowOrZonePenalty()
    {
        var resolver = new AtBatResolver(_content.Chemistry);
        var park = _content.Parks["harbor-diamond"];
        var manual = Input(bat: 5, err: 0, charge: 1);
        var chargeBat = Input(bat: 5, err: 0, charge: 0, batId: "charge-bat");
        Assert.True(_content.Bats["charge-bat"].ChargeAlwaysFull);

        var center = resolver.Resolve(manual, park, new Random(1));
        var free = resolver.Resolve(chargeBat, park, new Random(1));
        Assert.Equal(ContactQuality.Perfect, center.Quality);
        Assert.Equal(ContactQuality.Perfect, free.Quality);
        Assert.True(free.ExitVeloMph >= center.ExitVeloMph, $"charge bat {free.ExitVeloMph} vs manual MAX {center.ExitVeloMph}");

        // No window penalty: the slap window, not the charge window.
        var slapEdge = AtBatResolver.ContactWindowFrames(5, false, null, park, false) / 2 - 0.1;
        Assert.Equal(ContactQuality.Miss, resolver.Resolve(manual with { TimingErrorFrames = slapEdge }, park, new Random(1)).Quality);
        Assert.NotEqual(ContactQuality.Miss, resolver.Resolve(chargeBat with { TimingErrorFrames = slapEdge }, park, new Random(1)).Quality);

        // No zone penalty: the ball 0.4 ft toward the tip is Nice on a manual charge, Perfect with the bat.
        var tip = SweetSpot.TipSign(manual.Batter.Bats) * 0.4;
        Assert.Equal(ContactQuality.Nice, resolver.Resolve(manual with { CrossingX = tip }, park, new Random(1)).Quality);
        Assert.Equal(ContactQuality.Perfect, resolver.Resolve(chargeBat with { CrossingX = tip }, park, new Random(1)).Quality);
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
