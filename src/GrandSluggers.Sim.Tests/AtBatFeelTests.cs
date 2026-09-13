using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class AtBatFeelTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void OneButtonTapAndHoldBothCommitOnRelease()
    {
        var tapDown = ChargeButton.Advance(default, pressed: true, held: true, released: false,
            deltaSeconds: 1.0 / 60, secondsToFull: 0.45);
        Assert.False(tapDown.Committed);
        Assert.True(tapDown.Next.Armed);
        var tapUp = ChargeButton.Advance(tapDown.Next, pressed: false, held: false, released: true,
            deltaSeconds: 1.0 / 60, secondsToFull: 0.45);
        Assert.True(tapUp.Committed);
        Assert.True(ChargeFeel.IsSlap(tapUp.CommitFill01), $"tap fill {tapUp.CommitFill01}");
        Assert.Equal(default, tapUp.Next);

        var held = default(ChargeButtonState);
        for (var frame = 0; frame < 27; frame++)
        {
            var step = ChargeButton.Advance(held, pressed: frame == 0, held: true, released: false,
                deltaSeconds: 1.0 / 60, secondsToFull: 0.45);
            Assert.False(step.Committed);
            held = step.Next;
        }
        Assert.Equal(1, held.Fill01, 8);
        var maxUp = ChargeButton.Advance(held, pressed: false, held: false, released: true,
            deltaSeconds: 1.0 / 60, secondsToFull: 0.45);
        Assert.True(maxUp.Committed);
        Assert.Equal(1, ChargeFeel.Effective01(maxUp.CommitFill01, maxUp.CommitSecondsPastFull,
            maxHold: 0.5, decayPerSec: 0.8), 8);
    }

    [Fact]
    public void HoldingPastTheMaxBandKeepsTheReleaseButLosesPower()
    {
        var full = new ChargeButtonState(true, 1, 0);
        var late = ChargeButton.Advance(full, pressed: false, held: true, released: false,
            deltaSeconds: 0.8, secondsToFull: 0.45).Next;
        var released = ChargeButton.Advance(late, pressed: false, held: false, released: true,
            deltaSeconds: 1.0 / 60, secondsToFull: 0.45);
        Assert.True(released.Committed);
        var effective = ChargeFeel.Effective01(released.CommitFill01, released.CommitSecondsPastFull,
            maxHold: 0.5, decayPerSec: 0.8);
        Assert.InRange(effective, ChargeFeel.SlapBelow, 0.99);
    }

    [Fact]
    public void PitchButtonDoesNotArmUntilThePitcherIsReady()
    {
        var earlyDown = ChargeButton.Advance(default, pressed: true, held: true, released: false,
            deltaSeconds: 1.0 / 60, secondsToFull: 0.55, accepting: false);
        var earlyUp = ChargeButton.Advance(earlyDown.Next, pressed: false, held: false, released: true,
            deltaSeconds: 1.0 / 60, secondsToFull: 0.55, accepting: false);
        Assert.False(earlyDown.Next.Armed);
        Assert.False(earlyUp.Committed);

        var readyDown = ChargeButton.Advance(earlyUp.Next, pressed: true, held: true, released: false,
            deltaSeconds: 1.0 / 60, secondsToFull: 0.55, accepting: true);
        var readyUp = ChargeButton.Advance(readyDown.Next, pressed: false, held: false, released: true,
            deltaSeconds: 1.0 / 60, secondsToFull: 0.55, accepting: true);
        Assert.True(readyDown.Next.Armed);
        Assert.True(readyUp.Committed);
    }

    [Fact]
    public void SwingReleaseDuringPitcherWindupIsAnEarlySwingInsteadOfDisappearing()
    {
        var held = ChargeButton.Advance(default, pressed: true, held: true, released: false,
            deltaSeconds: 0.1, secondsToFull: 0.45);
        var released = ChargeButton.Advance(held.Next, pressed: false, held: false, released: true,
            deltaSeconds: 1.0 / 60, secondsToFull: 0.45);
        const double releaseDuringWindup = -0.2;
        const double plateAt = 1.0;
        var error = AtBatMotion.SwingErrorFrames(releaseDuringWindup, plateAt);

        Assert.True(released.Committed);
        Assert.True(error < 0, $"windup release should be early, got {error} frames");
        Assert.Equal(releaseDuringWindup, AtBatMotion.SwingStart(plateAt, error), 8);
    }

    [Fact]
    public void SwingIntentCarriesTheReleaseInputsAcrossAPhaseBoundary()
    {
        var held = ChargeButton.Advance(default, pressed: true, held: true, released: false,
            deltaSeconds: 0.2, secondsToFull: 0.45);
        var released = ChargeButton.Advance(held.Next, pressed: false, held: false, released: true,
            deltaSeconds: 1.0 / 60, secondsToFull: 0.45);
        var intent = SwingInputIntent.Capture(
            released, stickX: 0.6, stickY: 0.25, bunt: false, boxOffsetX: -0.35);

        Assert.True(intent.Committed);
        Assert.Equal(released.CommitFill01, intent.Fill01);
        Assert.Equal(AtBatResolver.SprayAimDeg(0.6), intent.SprayAimDeg);
        Assert.Equal(0.25, intent.LaunchAim);
        Assert.Equal(-0.35, intent.BoxOffsetX);

        const double releaseAt = -Motion.PitchRelease;
        const double plateAt = 1.0;
        var swing = intent.Resolve(releaseAt, plateAt, effectiveCharge: 0.4, star: true);
        Assert.True(swing.Swing);
        Assert.Equal(0.4, swing.Charge01);
        Assert.True(swing.Star);
        Assert.Equal(releaseAt, AtBatMotion.SwingStart(plateAt, swing.TimingErrorFrames), 8);
        Assert.Equal(intent.SprayAimDeg, swing.SprayAimDeg);
        Assert.Equal(intent.BoxOffsetX, swing.BoxOffsetX);
    }

    [Theory]
    [InlineData(0.78)]
    [InlineData(1.0)]
    [InlineData(1.28)]
    public void SwingTimesTheBatContactAgainstEveryPitchSpeed(double flight)
    {
        Assert.Equal(0, AtBatMotion.SwingErrorFrames(flight - Motion.SwingContact, flight), 8);
        Assert.Equal(-3, AtBatMotion.SwingErrorFrames(flight - Motion.SwingContact - 0.05, flight), 8);
        Assert.Equal(3, AtBatMotion.SwingErrorFrames(flight - Motion.SwingContact + 0.05, flight), 8);
        Assert.True(AtBatMotion.SwingErrorFrames(flight, flight) > Rules.Default.Batting.Window.SlapFrames);
        Assert.Equal(0, AtBatMotion.SwingErrorFrames(flight, flight, bunt: true), 8);
        foreach (var error in new[] { -8.0, 0, 5.0 })
            Assert.Equal(error, AtBatMotion.SwingErrorFrames(AtBatMotion.SwingStart(flight, error), flight), 8);
    }

    [Theory]
    [InlineData(Motion.PitchNormalLoadAt, Motion.PitchRelease)]
    [InlineData(SwingPresentation.NormalLoadAt, Motion.SwingContact)]
    public void HeldLoadBlendsContinuouslyButNeverDelaysTheEvent(double normalLoadAt, double mark)
    {
        foreach (var charge in new[] { 0.0, 0.5, 1.0 })
        {
            var loadAt = Motion.LoadSampleAt(normalLoadAt, charge);
            Assert.Equal(loadAt, AtBatMotion.LoadedClipTime(0, loadAt, mark), 8);
            Assert.Equal(mark, AtBatMotion.LoadedClipTime(mark, loadAt, mark), 8);
            Assert.Equal(mark * 0.5, AtBatMotion.LoadedClipTime(mark * 0.5, loadAt, mark), 8);
            var previous = AtBatMotion.LoadedClipTime(0, loadAt, mark);
            for (var poseT = 0.005; poseT <= mark; poseT += 0.005)
            {
                var sample = AtBatMotion.LoadedClipTime(poseT, loadAt, mark);
                Assert.True(sample >= previous, $"charge {charge} went backward at {poseT}");
                previous = sample;
            }
        }
        Assert.Equal(0, Motion.LoadSampleAt(normalLoadAt, 1), 8);
        Assert.Equal(normalLoadAt, Motion.LoadSampleAt(normalLoadAt, 0), 8);
    }

    [Fact]
    public void SharedSwingStartsOnAuthoredLoadsAndStillReachesExactContact()
    {
        Assert.Equal(SwingPresentation.NormalLoadAt, SwingPresentation.LoadSampleAt(0), 8);
        Assert.Equal(SwingPresentation.LoadAt, SwingPresentation.LoadSampleAt(1), 8);
        foreach (var charge in new[] { 0.0, 0.5, 1.0 })
        {
            var previous = AtBatMotion.SwingClipTime(0, charge);
            Assert.Equal(SwingPresentation.LoadSampleAt(charge), previous, 8);
            for (var poseT = 0.01; poseT <= Motion.SwingContact; poseT += 0.01)
            {
                var sampleT = AtBatMotion.SwingClipTime(poseT, charge);
                Assert.True(sampleT >= previous, $"charge {charge} went backward at {poseT}: {sampleT} < {previous}");
                previous = sampleT;
            }
            Assert.Equal(Motion.SwingContact,
                AtBatMotion.SwingClipTime(Motion.SwingContact, charge), 8);
        }
    }

    [Fact]
    public void LateCommittedSwingContinuesFromPlateResolutionThroughFollowThroughOnce()
    {
        const double plateAt = 0.50;
        const double lateFrames = 12;
        var start = AtBatMotion.SwingStart(plateAt, lateFrames);
        var clock = AtBatMotion.SwingNotStarted;

        clock = AtBatMotion.AdvanceCommittedSwing(clock, start, start, 0);
        Assert.Equal(0, clock, 8);
        clock = AtBatMotion.AdvanceCommittedSwing(clock, plateAt, start, 0.10);
        Assert.Equal(0.10, clock, 8);

        var sawContact = false;
        while (clock < Motion.SwingDur)
        {
            clock = AtBatMotion.AdvanceCommittedSwing(clock, plateAt, start, 0.05);
            sawContact |= Math.Abs(clock - Motion.SwingContact) < 1e-8;
            Assert.True(AtBatMotion.PresentsCommittedSwing(clock));
        }
        Assert.True(sawContact);
        Assert.Equal(Motion.SwingDur, AtBatMotion.CommittedSwingSample(clock), 8);

        clock = AtBatMotion.AdvanceCommittedSwing(clock, plateAt, start, 0.05);
        Assert.False(AtBatMotion.PresentsCommittedSwing(clock));
        Assert.Equal(Motion.SwingDur, AtBatMotion.CommittedSwingSample(clock), 8);
    }

    [Fact]
    public void EarlyCommittedSwingFinishedBeforeResolutionDoesNotRestart()
    {
        const double plateAt = 0.50;
        const double earlyFrames = -30;
        var start = AtBatMotion.SwingStart(plateAt, earlyFrames);
        var clock = AtBatMotion.SwingNotStarted;

        clock = AtBatMotion.AdvanceCommittedSwing(clock, start, start, 0);
        clock = AtBatMotion.AdvanceCommittedSwing(
            clock, start + Motion.SwingDur, start, Motion.SwingDur);
        Assert.Equal(Motion.SwingDur, clock, 8);
        Assert.True(AtBatMotion.PresentsCommittedSwing(clock));

        clock = AtBatMotion.AdvanceCommittedSwing(clock, plateAt, start, 1.0 / 60);
        Assert.True(clock > Motion.SwingDur);
        Assert.False(AtBatMotion.PresentsCommittedSwing(clock));
    }


    [Fact]
    public void ReleaseIsTheHandNotTheTorsoAndPathFacesBothLooks()
    {
        var rel = PitchFlight.Release();
        Assert.True(rel.X > 1.2, $"hand x={rel.X}");
        Assert.True(rel.Z < Diamond.Mound - 1.5, $"in front of rubber z={rel.Z}");
        Assert.InRange(Baseball.DiameterFt, 0.45, 0.85);
        Assert.True(Baseball.DiameterFt < 1.0, "posed ball is glove-sized");
        Assert.True(Baseball.FlightDiameterFt > Baseball.DiameterFt);
        Assert.True(Baseball.FlightDiameterFt < 1.6, "2ft pitch scale was a torso on the toys");
        Assert.True(Baseball.InPlayDiameterFt < 1.35, "in-play ball is not a torso");
        Assert.True(Baseball.HaloMul > 1.2);
        Assert.True(SetTells.TrailSeconds > 0.45);
        Assert.True(SetTells.TrailStartFt(Baseball.InPlayDiameterFt) > 0.25);
        Assert.Equal(Baseball.FlightDiameterFt, Baseball.InFlightScale(true));
        Assert.Equal(Baseball.DiameterFt, Baseball.InFlightScale(false));
        Assert.Equal(Baseball.InPlayDiameterFt, Baseball.ApparentScale(true, 48, inPlay: true));
        Assert.True(Baseball.ApparentScale(true, 280, inPlay: true) < 1.35, "outfield hopper stays a ball");
        var plate = _content.Shots.Must("plate");
        var mound = _content.Shots.Must("mound");
        var pitch = _content.Shots.Must("pitch");
        for (var u = 0.05; u <= 1; u += 0.15)
        {
            var p = PitchFlight.Point("fastball", u);
            Assert.True(PitchFlight.InFrontOfLook(p.X, p.Y, p.Z, plate), $"plate u={u} {p}");
            Assert.True(PitchFlight.InFrontOfLook(p.X, p.Y, p.Z, mound), $"mound u={u} {p}");
            Assert.True(PitchFlight.InFrontOfLook(p.X, p.Y, p.Z, pitch), $"pitch u={u} {p}");
        }
        var leave = PitchFlight.Point("fastball", StillPose.PitchBallU);
        Assert.True(PitchFlight.InFrontOfLook(leave.X, leave.Y, leave.Z, pitch), $"release {leave}");
        Assert.True(leave.Z > 40, $"release still on the pitcher z={leave.Z}");
        var mid = PitchFlight.Point("fastball", 0.55);
        var size = PitchFlight.ApparentDeg(mid.X, mid.Y, mid.Z, plate, Baseball.ApparentScale(true, mid.Z));
        var still = PitchFlight.ApparentDeg(mid.X, mid.Y, mid.Z, plate, Baseball.DiameterFt);
        Assert.True(size > still, $"flight {size} vs still {still}");
        Assert.True(size > 1.2, $"mid-flight speck {size} deg");
        var early = PitchFlight.Point("fastball", 0.2);
        var earlyDeg = PitchFlight.ApparentDeg(early.X, early.Y, early.Z, plate, Baseball.ApparentScale(true, early.Z));
        Assert.True(earlyDeg > 0.9, $"early pitch speck {earlyDeg} deg");
        for (var u = 0.15; u <= 0.9; u += 0.2)
        {
            var pt = PitchFlight.Point("fastball", u);
            var vp = PlayCamera.Project(plate, new Vec3(pt.X, pt.Y, pt.Z));
            Assert.True(PlayCamera.InFrame(vp, 0.0), $"hitter lost the pitch u={u} {vp}");
        }
    }

    [Fact]
    public void ChargeMaxIsStrongerThanOverchargeAndSlapContactsMore()
    {
        var feel = _content.Feel;
        Assert.True(feel.ChargeMaxHoldSeconds > 0);
        Assert.True(feel.ChargeOverchargeDecay > 0);
        var max = ChargeFeel.Effective01(1, 0, feel.ChargeMaxHoldSeconds, feel.ChargeOverchargeDecay);
        var late = ChargeFeel.Effective01(1, feel.ChargeMaxHoldSeconds + 0.6, feel.ChargeMaxHoldSeconds, feel.ChargeOverchargeDecay);
        Assert.Equal(1, max);
        Assert.True(late < max, $"overcharge {late} vs max {max}");
        Assert.True(ChargeFeel.AtMax(1, 0, feel.ChargeMaxHoldSeconds));
        Assert.False(ChargeFeel.AtMax(1, 0.8, feel.ChargeMaxHoldSeconds));
        Assert.Equal("Nice!", ChargeFeel.NiceCopy(true, 1, 0, feel.ChargeMaxHoldSeconds));
        Assert.Equal("MAX", ChargeFeel.NiceCopy(false, 1, 0, feel.ChargeMaxHoldSeconds));
        Assert.Equal("", ChargeFeel.NiceCopy(true, 1, 0.9, feel.ChargeMaxHoldSeconds));

        var park = _content.Parks["harbor-diamond"];
        var resolver = new AtBatResolver(_content.Chemistry);
        var vale = _content.Must("vale");
        var rio = _content.Must("rio");
        var bat = _content.Bats["harbor-lumber"];
        var slapHits = 0;
        var chargeHits = 0;
        var maxCarry = 0.0;
        var lateCarry = 0.0;
        // A frame inside the slap window and outside the charge window (spec §5.3: 9 vs 7 frames).
        var contact = rio.Stats.Bat;
        var edge = (AtBatResolver.ContactWindowFrames(contact, true, null, park, false)
                    + AtBatResolver.ContactWindowFrames(contact, false, null, park, false)) / 4;
        for (var seed = 0; seed < 36; seed++)
        {
            if (resolver.Resolve(Input(vale, rio, bat, 0, edge), park, new Random(seed)).Quality != ContactQuality.Miss) slapHits++;
            if (resolver.Resolve(Input(vale, rio, bat, 1, edge), park, new Random(seed)).Quality != ContactQuality.Miss) chargeHits++;
            maxCarry += resolver.Resolve(Input(vale, rio, bat, 1, 0), park, new Random(seed)).CarryFt;
            lateCarry += resolver.Resolve(Input(vale, rio, bat, late, 0), park, new Random(seed)).CarryFt;
        }
        Assert.True(slapHits > chargeHits, $"slap contact {slapHits} vs charge {chargeHits}");
        Assert.True(maxCarry > lateCarry, $"MAX carry {maxCarry} vs overcharge {lateCarry}");
        var maxMph = AtBatResolver.PitchSpeedMph(new PitchCommand("fastball", 1, false), 7);
        var overMph = AtBatResolver.PitchSpeedMph(new PitchCommand("fastball", late, false), 7);
        Assert.True(maxMph > overMph, $"MAX mph {maxMph} vs over {overMph}");
    }

    [Fact]
    public void CursorEatsHeartAndWalkedOffMisses()
    {
        Assert.Equal(ContactQuality.Perfect, SweetSpot.Zone(0, Hand.R, 0, StrikeZoneGeometry.CenterY));
        Assert.Equal(ContactQuality.Miss, SweetSpot.Zone(0.9, Hand.R, 0, StrikeZoneGeometry.CenterY));
        var left = SweetSpot.WorldCenter(-0.4);
        var right = SweetSpot.WorldCenter(0.4);
        Assert.True(right.X > left.X, $"cursor right {right.X} vs left {left.X}");
        Assert.Equal(0.8 * HomeSet.BatterWalk, right.X - left.X, 8);
        Assert.Equal(StrikeZoneGeometry.CenterY, left.Y);
        Assert.Equal(StrikeZoneGeometry.Height / 2, SweetSpot.HalfHeightFt);
        Assert.True(SweetSpot.CoversTheZone(Hand.R), "every strike is on the bat with the box centered");
        Assert.True(SweetSpot.CoversTheZone(Hand.L), "every strike is on the bat with the box centered");
        var park = _content.Parks["harbor-diamond"];
        var resolver = new AtBatResolver(_content.Chemistry);
        var vale = _content.Must("vale");
        var rio = _content.Must("rio");
        var bat = _content.Bats["harbor-lumber"];
        var square = resolver.Resolve(Input(vale, rio, bat, 0, 0, box: 0, aimX: 0), park, new Random(1));
        var miss = resolver.Resolve(Input(vale, rio, bat, 0, 0, box: 0.9, aimX: 0), park, new Random(1));
        Assert.True(square.InPlay || square.Quality != ContactQuality.Miss, square.Quality.ToString());
        Assert.Equal(ContactQuality.Miss, miss.Quality);
        Assert.False(miss.InPlay);
    }

    [Fact]
    public void BookletSaysOutsideTakesAndMissesAreDifferent()
    {
        var pad = HowToPlay.Must("the-box").Lines;
        var keys = HowToPlay.Must("the-box").KeyLines!;
        Assert.Contains(pad, line => line.Contains("outside") && line.Contains("ball") && line.Contains("strike"));
        Assert.Contains(keys, line => line.Contains("outside") && line.Contains("ball") && line.Contains("strike"));
    }

    [Fact]
    public void StarSpendsOnAMiss()
    {
        var match = Match.Exhibition(_content, "rio", "ashlord", seed: 1);
        var before = match.AwayStars;
        match.Play(new PitchCommand("fastball", 0, false), new SwingCommand(true, 0, 40, true));
        Assert.True(match.AwayStars < before, $"stars {match.AwayStars} vs {before}");
    }

    [Fact]
    public void PickoffNeverFreesAGluedRunnerAndCanCatchASteal()
    {
        // D1 / D3: a runner on the bag is always safe; only an armed runner can be caught.
        var glued = Match.Slice(_content, seed: 1);
        WalkOn(glued);
        Assert.NotNull(glued.First);
        Assert.False(glued.StealAttempt);
        var stay = glued.Pickoff(1);
        Assert.NotNull(stay);
        Assert.Equal(PlayKind.Pickoff, stay!.Kind);
        Assert.False(PlayStamp.Shows(stay.Kind), "the beat has no stamp (§4.5)");
        Assert.NotNull(glued.First);

        var dancing = Match.Slice(_content, seed: 4);
        WalkOn(dancing);
        dancing.ToggleSteal();
        var gun = dancing.Pickoff(1);
        Assert.NotNull(gun);
        Assert.True(gun!.Kind is PlayKind.CaughtStealing or PlayKind.StolenBase or PlayKind.Pickoff, gun.Kind.ToString());
    }

    [Fact]
    public void PitcherTiredAfterWorkAndSwapClearsIt()
    {
        var match = Match.Exhibition(_content, "rio", "ashlord", seed: 2);
        Assert.False(match.PitcherTired);
        var meat = new PitchCommand("fastball", 1, true);
        var take = new SwingCommand(false, 0, 0, false);
        for (var i = 0; i < 40 && !match.PitcherTired && !match.Over; i++)
            match.Play(meat, take);
        Assert.True(match.PitcherTired || match.Over);
        if (match.PitcherTired)
        {
            Assert.True(match.SwapPitcher());
            Assert.False(match.PitcherTired);
        }
    }

    [Fact]
    public void NineGlovesAndInningsThreeAndNineFinish()
    {
        var draft = TeamBuilder.Draft(_content, "rio");
        Assert.Equal(9, Diamond.Order.Length);
        Assert.Equal(9, draft.Gloves.Count);
        foreach (var pos in Diamond.Order)
            Assert.True(draft.Gloves.ContainsKey(pos), pos);
        var three = Match.Slice(_content, innings: 3, seed: 7);
        three.AutoPlayGame();
        Assert.True(three.Over);
        var nine = Match.Slice(_content, innings: 9, seed: 3);
        nine.AutoPlayGame();
        Assert.True(nine.Over);
        Assert.True(nine.Inning >= 9);
    }

    [Fact]
    public void DashShortensHomeToFirstBuddyTossTransfers()
    {
        var dart = _content.Must("dart");
        var still = RunnerSystem.SpeedFtPerSec(dart);
        var dash = RunnerSystem.SpeedFtPerSec(dart, 1);
        Assert.True(dash > still, $"dash {dash} vs {still}");
        Assert.True(dash < still * 1.3, "dash is not a teleport");
        Assert.True(FieldDash.ChaseMul() > 1);
        var rio = _content.Must("rio");
        var nico = _content.Must("nico");
        Assert.True(FieldDash.BuddyTossOffered(_content.Chemistry.Between(rio, nico), 12)
                    || FieldDash.BuddyTossOffered(Chemistry.Good, 12));
        Assert.False(FieldDash.BuddyTossOffered(Chemistry.Bad, 8));
        var field = new FieldingResult(PlayKind.GroundOut, rio, nico, 0.8, 10, 40, false, false,
            new ThrowResult(Chemistry.Neutral, 1.0, false));
        var thr = new ThrowResult(Chemistry.Good, 1.35, false);
        var after = FieldDash.ApplyBuddyToss(field, nico, thr);
        Assert.Equal(nico.Id, after.Fielder!.Id);
        Assert.Equal(Chemistry.Good, after.Throw!.Relation);
        Assert.True(after.Throw.SpeedMul > 1.2);
    }

    static AtBatInput Input(Character pitcher, Character batter, BatItem bat, double charge, double timing,
        double box = 0, double aimX = 0) =>
        new(pitcher, batter, null, [], false, false,
            timing, false, false, bat, 80, 0, true, false, 0, charge, box, aimX * PitchFlight.PlateScaleX, PitchFlight.PlateY);

    static void WalkOn(Match match)
    {
        var wild = new PitchCommand("fastball", 0, false, AimX: 1.5);
        var take = new SwingCommand(false, 0, 0, false);
        while (match.First is null && !match.Over)
            match.Play(wild, take);
    }
}
