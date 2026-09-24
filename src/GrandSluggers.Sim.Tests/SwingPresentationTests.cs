using Xunit;
using GrandSluggers.Sim;
using System.Text.Json.Nodes;

namespace GrandSluggers.Sim.Tests;

public class SwingPresentationTests
{
    // -------------------------------------------------------------------------------------
    // D13 (#612): inside the window the take is warped so Contact lands on the ball's plate
    // time; outside it plays at its own 0.50 s and misses. Keys keep their order; never backward.
    // -------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void PressFourFramesEarlyStillLandsContactOnTheBallsPlateTime(double charge)
    {
        var rules = Rules.Default;
        const double plateAt = 0.98;
        var press = AtBatMotion.SquarePressAt(plateAt, rules: rules) - 4.0 / 60;
        var err = AtBatMotion.SwingErrorFrames(press, plateAt, rules: rules);
        Assert.Equal(-4, err, 8);
        var window = rules.Batting.Window.Frames;
        Assert.True(AtBatResolver.InWindow(err, window));

        var contactSec = AtBatMotion.SwingContactSec(err, window, rules);
        Assert.Equal(plateAt, press + contactSec, 8);
        Assert.Equal(Motion.SwingContact, AtBatMotion.SwingClipTime(plateAt - press, charge, contactSec), 8);

        // The committed clock reaches the ball's plate time on the flight and presents Contact there.
        var takeSec = AtBatMotion.SwingTakeSeconds(contactSec);
        var clock = AtBatMotion.AdvanceCommittedSwing(AtBatMotion.SwingNotStarted, press, press, 0, takeSec);
        Assert.Equal(0, clock, 8);
        clock = AtBatMotion.AdvanceCommittedSwing(clock, plateAt, press, 1.0 / 60, takeSec);
        Assert.Equal(Motion.SwingContact,
            AtBatMotion.SwingClipTime(AtBatMotion.CommittedSwingSample(clock, takeSec), charge, contactSec), 8);

        // Every key of the take this charge plays, in order, never backward (#613: slap or charge).
        var take = SwingPresentation.TakeFor(charge);
        var keys = SwingPresentation.KeysFor(take);
        var previous = AtBatMotion.SwingClipTime(0, charge, contactSec);
        Assert.Equal(SwingPresentation.CommittedLoadAt(charge), previous, 8);
        var crossed = new List<double>();
        var keyIndex = 0;
        void Cross(double sample)
        {
            while (keyIndex < keys.Count && sample >= keys[keyIndex].T - 1e-9)
                crossed.Add(keys[keyIndex++].T);
        }
        Cross(previous);
        const int steps = 2000;
        for (var i = 1; i <= steps; i++)
        {
            var t = takeSec * i / steps;
            var sample = AtBatMotion.SwingClipTime(t, charge, contactSec);
            Assert.True(sample >= previous - 1e-12, $"charge {charge} went backward at {t}: {sample} < {previous}");
            Cross(sample);
            previous = sample;
        }
        Assert.Equal(keys.Select(k => k.T), crossed);
        Assert.Equal(Motion.SwingFinish, AtBatMotion.SwingClipTime(takeSec, charge, contactSec), 8);
        // The follow-through and finish after contact are the take's own 0.30 s, not warped.
        Assert.Equal(Motion.SwingFinish - Motion.SwingContact, takeSec - contactSec, 8);
    }

    [Theory]
    [InlineData(9.0)]
    [InlineData(-18.0)]
    public void PressOutsideTheWindowPlaysTheTakeAtItsOwnLengthAndMisses(double err)
    {
        var rules = Rules.Default;
        var window = rules.Batting.Window.Frames;
        Assert.False(AtBatResolver.InWindow(err, window));
        var contactSec = AtBatMotion.SwingContactSec(err, window, rules);
        Assert.Equal(Motion.SwingContact, contactSec, 8);
        Assert.Equal(Motion.SwingFinish, AtBatMotion.SwingTakeSeconds(contactSec), 8);
        foreach (var charge in new[] { 0.0, 1.0 })
        for (var t = 0.0; t <= Motion.SwingFinish; t += 0.01)
            Assert.Equal(AtBatMotion.SwingClipTime(t, charge), AtBatMotion.SwingClipTime(t, charge, contactSec), 8);

        // The bat's Contact mark is not on the ball: early lands after the ball has gone, late before it would arrive.
        const double plateAt = 0.98;
        var press = AtBatMotion.SwingStart(plateAt, err, rules: rules);
        Assert.NotEqual(plateAt, press + contactSec, 3);
    }

    [Fact]
    public void AnInWindowPressAfterThePlateLandsContactAtThePressNotBeforeIt()
    {
        var rules = Rules.Default;
        Assert.Equal(0.18, rules.Batting.Window.LeadSec, 8);
        var platePress = rules.Batting.Window.LeadSec * 60;
        // EASY ×1.3 at contact 10 was 14.3 frames (half 7.15), the widest window the split rule ever
        // gave (retired by #887). A press on the plate is 10.8 frames late — outside it and outside
        // the one shipped window. Release has to lead the ball (#670).
        const double easyContactTen = 14.3;
        Assert.False(AtBatResolver.InWindow(platePress, easyContactTen));
        Assert.False(AtBatResolver.InWindow(platePress, rules.Batting.Window.Frames));
        // A window wide enough to hold a press after the plate still clamps Contact to the
        // press (never before it).
        const double window = 24;
        var err = platePress + 1;
        Assert.True(AtBatResolver.InWindow(err, window));
        var contactSec = AtBatMotion.SwingContactSec(err, window, rules);
        Assert.Equal(0, contactSec, 8);
        Assert.Equal(Motion.SwingContact, AtBatMotion.SwingClipTime(0, 0, contactSec), 8);
        Assert.Equal(SwingPresentation.CommittedLoadAt(0), AtBatMotion.SwingClipTime(-0.01, 0, contactSec), 8);
        Assert.True(rules.Batting.Window.LeadSec * 60 > rules.Batting.Window.Frames / 2,
            "batting.window.leadSec must cover half the window so contact meets the ball");
    }

    [Fact]
    public void ReadyAndLoadAreSidewaysToThePlateWithEyesOnThePitcher()
    {
        foreach (var hand in new[] { Hand.R, Hand.L })
        foreach (var t in new[] { SwingPresentation.LoadAt, SwingPresentation.NormalLoadAt,
                                  SwingPresentation.LaunchAt })
        {
            var stance = BattingStance.At(t, hand);
            Assert.True(Dot(stance.ChestForward, BattingStance.PlateDirection(hand))
                        >= BattingStance.AlignmentDot);
            Assert.True(Dot(stance.EyesForward, new Vec3(0, 0, 1))
                        >= BattingStance.AlignmentDot);
            // Signed: the back-to-lead foot line points at the pitcher. An
            // unsigned check scored hips turned out of the box as correct.
            Assert.True(Dot(stance.FeetAxis, new Vec3(0, 0, 1))
                        >= BattingStance.AlignmentDot);
        }
    }

    [Fact]
    public void AuthoredBodyTurnsThroughContactAndMirrorsOnlyAcrossThePlate()
    {
        var load = BattingStance.At(SwingPresentation.LoadAt);
        var approach = BattingStance.At(SwingPresentation.ApproachAt);
        var contact = BattingStance.At(SwingPresentation.ContactAt);
        var follow = BattingStance.At(SwingPresentation.FollowThroughAt);
        Assert.True(load.ChestForward.X > approach.ChestForward.X);
        Assert.True(approach.ChestForward.X > contact.ChestForward.X);
        Assert.True(contact.ChestForward.X > follow.ChestForward.X);
        Assert.True(load.ChestForward.Z < approach.ChestForward.Z);
        Assert.True(approach.ChestForward.Z < contact.ChestForward.Z);

        foreach (var key in BattingStance.Keys)
        {
            var left = BattingStance.At(key.T, Hand.L);
            Assert.Equal(-key.ChestForward.X, left.ChestForward.X, 7);
            Assert.Equal(key.ChestForward.Z, left.ChestForward.Z, 7);
            Assert.Equal(key.EyesForward, left.EyesForward);
        }
    }

    [Fact]
    public void LoadedBarrelStandsAboveTheHandsOnEveryHeldSample()
    {
        // The gate holds a batter at any charge, so it samples the take between
        // the authored keys. Zig's shared-root squash (Height 0.56) shortens the
        // rendered rise hardest; a body that fails here fails the still gate.
        foreach (var body in Shipped.CaptainIds)
        {
            var scale = Silhouette.SharedRootScale(Silhouette.Proportions(Shipped.Content, body));
            for (var step = 0; step <= 40; step++)
            {
                var charge = step / 40.0;
                var t = SwingPresentation.HeldLoadAt(charge);
                foreach (var hand in new[] { Hand.R, Hand.L })
                {
                    var direction = SwingPresentation.At(t, hand, SwingTake.Charge).BarrelDirection;
                    var world = new Vec3(
                        direction.X * scale.X, direction.Y * scale.Y, direction.Z * scale.Z);
                    var length = Math.Sqrt(
                        world.X * world.X + world.Y * world.Y + world.Z * world.Z);
                    Assert.True(
                        world.Y / length >= SwingPresentation.LoadedBarrelRise,
                        $"{body} held at {charge:0.00} drops the barrel to {world.Y / length:0.000}");
                }
            }
        }
    }

    [Fact]
    public void HeldAndSwungHandsStayOnTheAuthoredHandle()
    {
        // The DCC take bakes this contract on every frame, so the gate samples
        // it between the authored keys. A new key that pulls a hand off the
        // handle in between would author a rig the still gate then rejects.
        foreach (var take in new[] { SwingTake.Slap, SwingTake.Charge })
        for (var step = 0; step <= 200; step++)
        {
            var t = SwingPresentation.FinishAt * step / 200.0;
            foreach (var hand in new[] { Hand.R, Hand.L })
            {
                var key = SwingPresentation.At(t, hand, take);
                foreach (var fist in new[] { Hand.L, Hand.R })
                    Assert.True(
                        SwingPresentation.HandToHandle(key, fist)
                            <= SwingPresentation.HandToHandleAllowance,
                        $"{take} {fist} fist leaves the handle at {t:0.000}: "
                        + $"{SwingPresentation.HandToHandle(key, fist):0.000}");
            }
        }
    }

    // -------------------------------------------------------------------------------------
    // #613: two takes on one rig, one catalog, a held finish (#583).
    // -------------------------------------------------------------------------------------

    [Fact]
    public void DccSwingTakesCatalogCarriesTheContractKeys()
    {
        var repo = Directory.GetParent(ContentCatalog.Load().Root.Shipped)!.FullName;
        var doc = JsonNode.Parse(File.ReadAllText(Path.Combine(repo, "data", "art", "swing-takes.json")))!;
        Assert.Equal(Motion.SwingContact, doc["contactAt"]!.GetValue<double>(), 8);
        Assert.Equal(Motion.SwingFinish, doc["finishAt"]!.GetValue<double>(), 8);
        Assert.Equal(SwingPresentation.BatHeadClearance, doc["batHeadClearance"]!.GetValue<double>(), 8);
        var takes = doc["takes"]!.AsArray();
        Assert.Equal(2, takes.Count);
        foreach (var row in takes)
        {
            var take = row!["take"]!.GetValue<string>() == "charge" ? SwingTake.Charge : SwingTake.Slap;
            Assert.Equal(take == SwingTake.Charge ? Motion.SwingChargeClip : Motion.SwingSlapClip, row["id"]!.GetValue<string>());
            var keys = row["keys"]!.AsArray();
            var authored = SwingPresentation.KeysFor(take);
            Assert.Equal(authored.Count, keys.Count);
            for (var i = 0; i < keys.Count; i++)
            {
                Assert.Equal(authored[i].T, keys[i]!["t"]!.GetValue<double>(), 8);
                AssertVector(authored[i].LeftHand, keys[i]!["leftHand"]!.AsArray());
                AssertVector(authored[i].RightHand, keys[i]!["rightHand"]!.AsArray());
                AssertVector(authored[i].Grip, keys[i]!["grip"]!.AsArray());
                // The catalog stores the unit barrel to 4 places; compare directions, not digits.
                var barrel = keys[i]!["barrel"]!.AsArray();
                var (bx, by, bz) = (barrel[0]!.GetValue<double>(), barrel[1]!.GetValue<double>(), barrel[2]!.GetValue<double>());
                var bn = Math.Sqrt(bx * bx + by * by + bz * bz);
                Assert.Equal(authored[i].BarrelDirection.X, bx / bn, 5);
                Assert.Equal(authored[i].BarrelDirection.Y, by / bn, 5);
                Assert.Equal(authored[i].BarrelDirection.Z, bz / bn, 5);
                // The crouch moves the head the clearance contract measures (#623).
                Assert.Equal(keys[i]!["legs"]?["lift"]?.GetValue<double>() ?? 0, authored[i].Lift, 3);
            }
        }
        // Approach and contact are the measured contract (docs/research-batting.md): both takes share them.
        foreach (var t in new[] { SwingPresentation.ApproachAt, SwingPresentation.ContactAt })
            AssertSameKey(SwingPresentation.At(t, Hand.R, SwingTake.Slap), SwingPresentation.At(t, Hand.R, SwingTake.Charge));
        // Every take starts on a load, ends on the held finish, and a held charge at no charge is the slap's first key.
        foreach (var take in new[] { SwingTake.Slap, SwingTake.Charge })
        {
            Assert.Equal(SwingPresentation.LoadAt, SwingPresentation.KeysFor(take)[0].T);
            Assert.Equal(SwingPresentation.FinishAt, SwingPresentation.KeysFor(take)[^1].T);
        }
        AssertSameKey(SwingPresentation.At(SwingPresentation.LoadAt, Hand.R, SwingTake.Slap),
            SwingPresentation.At(SwingPresentation.HeldLoadAt(0), Hand.R, SwingTake.Charge));
    }

    static void AssertSameKey(SwingPresentation.Key a, SwingPresentation.Key b)
    {
        foreach (var (x, y) in new[] { (a.LeftHand, b.LeftHand), (a.RightHand, b.RightHand), (a.Grip, b.Grip), (a.BarrelDirection, b.BarrelDirection) })
        {
            Assert.Equal(x.X, y.X, 9);
            Assert.Equal(x.Y, y.Y, 9);
            Assert.Equal(x.Z, y.Z, 9);
        }
    }

    [Fact]
    public void TheChargeTakeWindsUpAndSwingsABiggerArcThanTheSlap()
    {
        foreach (var hand in new[] { Hand.R, Hand.L })
        {
            var ready = SwingPresentation.At(SwingPresentation.HeldLoadAt(0), hand, SwingTake.Charge);
            var windup = SwingPresentation.At(SwingPresentation.HeldLoadAt(1), hand, SwingTake.Charge);
            var top = hand == Hand.L ? (Func<SwingPresentation.Key, Vec3>)(k => k.LeftHand) : k => k.RightHand;
            Assert.True(top(windup).Y > top(ready).Y + 0.1, $"{hand}: MAX must show a windup above the ready hands");
            Assert.True(Distance(windup.Grip, ready.Grip) > 0.3, $"{hand}: the windup must move the hands back");

            var contact = SwingPresentation.BarrelPoint(SwingPresentation.At(SwingPresentation.ContactAt, hand, SwingTake.Slap));
            double Arc(SwingTake take) => Distance(contact,
                SwingPresentation.BarrelPoint(SwingPresentation.At(SwingPresentation.FollowThroughAt, hand, take)));
            Assert.True(Arc(SwingTake.Charge) > Arc(SwingTake.Slap) + 0.3,
                $"{hand}: charge follow-through arc {Arc(SwingTake.Charge):0.00} vs slap {Arc(SwingTake.Slap):0.00}");
        }
    }

    [Fact]
    public void TheResolversChargeTestPicksTheTake()
    {
        Assert.Equal(SwingTake.Slap, SwingPresentation.TakeFor(ChargeFeel.ChargeAt - 0.01));
        Assert.Equal(SwingTake.Charge, SwingPresentation.TakeFor(ChargeFeel.ChargeAt));
        Assert.Equal(Motion.SwingSlapClip, Motion.CueFor(Motion.Verb.Swing, 0).Clip);
        Assert.Equal(Motion.SwingChargeClip, Motion.CueFor(Motion.Verb.Swing, 1).Clip);
        Assert.Equal(Motion.SwingChargeClip, Motion.CueFor(Motion.Verb.ChargeSwing, 0).Clip);
        // A slap has no windup: it starts on its ready key. A charge continues from the held windup.
        Assert.Equal(SwingPresentation.LoadAt, SwingPresentation.CommittedLoadAt(0.3), 8);
        Assert.Equal(SwingPresentation.HeldLoadAt(0.8), SwingPresentation.CommittedLoadAt(0.8), 8);
    }

    // -------------------------------------------------------------------------------------
    // #623: the bat never passes through the batter's head.
    // -------------------------------------------------------------------------------------

    [Fact]
    public void TheBatClearsTheHeadOnEverySampleOfBothTakesAndTheWholeChargeUp()
    {
        foreach (var take in new[] { SwingTake.Slap, SwingTake.Charge })
        for (var step = 0; step <= 600; step++)
        {
            var t = SwingPresentation.FinishAt * step / 600.0;
            var right = SwingPresentation.HeadClearance(SwingPresentation.At(t, Hand.R, take));
            Assert.True(right >= SwingPresentation.BatHeadClearance,
                $"{take} at {t:0.000}: the bat surface is {right:0.000} from the head");
            // The head sits on the plate line's mirror axis: a left-handed batter clears it the same.
            Assert.Equal(right, SwingPresentation.HeadClearance(SwingPresentation.At(t, Hand.L, take)), 9);
        }
        // The hold samples the charge take between the ready key and the windup: every charge.
        for (var step = 0; step <= 100; step++)
        {
            var charge = step / 100.0;
            var clearance = SwingPresentation.HeadClearance(
                SwingPresentation.At(SwingPresentation.HeldLoadAt(charge), Hand.R, SwingTake.Charge));
            Assert.True(clearance >= SwingPresentation.BatHeadClearance,
                $"held charge {charge:0.00}: the bat surface is {clearance:0.000} from the head");
        }
    }

    [Fact]
    public void HeadClearanceCatchesABatThroughTheHead()
    {
        // The #613 windup that shipped: grip behind the ear, barrel up over the head.
        var through = new SwingPresentation.Key(0, new Vec3(0, 0, 0), new Vec3(0, 0, 0),
            new Vec3(0.25, 2.70, -0.62), new Vec3(0.06, 0.9007, 0.4303), -0.2);
        Assert.True(SwingPresentation.HeadClearance(through) < 0);
        var beside = through with { Grip = new Vec3(1.6, 2.70, 0) };
        Assert.True(SwingPresentation.HeadClearance(beside) > SwingPresentation.BatHeadClearance);
    }

    [Fact]
    public void TheLoadedBarrelSitsBesideTheHeadOnThePlateCamera()
    {
        // #560: the plate SET hid the ready barrel in the head disk. Tuning
        // that shot cannot pull it out inside the SET constraints; the ready
        // key has to stand the bat beside the head. MAX already did (#623).
        var plate = ContentCatalog.Load().Shots.Must("plate");
        Assert.Equal(HomeSet.CamX, plate.Pos.X, 6);
        Assert.Equal(HomeSet.CamZ, plate.Pos.Z, 6);
        foreach (var body in Shipped.CaptainIds)
        foreach (var hand in new[] { Hand.R, Hand.L })
        {
            // Ready only: the sitting was SET at no charge. MAX already stands
            // beside on Rio (#623); Zig's squat scale still hides that windup.
            var ready = SwingPresentation.At(
                SwingPresentation.HeldLoadAt(0), hand, SwingTake.Charge);
            var beside = SwingPresentation.BarrelBesideHeadDeg(ready, hand, plate, Silhouette.Proportions(Shipped.Content, body));
            Assert.True(
                beside >= SwingPresentation.PlateLoadedBesideDeg,
                $"{body} {hand} ready: barrel hides in the plate head disk ({beside:0.00} deg)");
            var slap = SwingPresentation.At(SwingPresentation.LoadAt, hand, SwingTake.Slap);
            Assert.True(
                SwingPresentation.BarrelBesideHeadDeg(slap, hand, plate, Silhouette.Proportions(Shipped.Content, body))
                    >= SwingPresentation.PlateLoadedBesideDeg,
                $"{body} {hand} slap ready hides in the plate head disk");
        }
    }

    [Fact]
    public void BarrelBesideHeadRejectsABarrelHiddenByTheCurrentRigHead()
    {
        var plate = ContentCatalog.Load().Shots.Must("plate");
        // #560 remains a projection falsifier after the rig revision: place the
        // barrel along the camera-to-head ray so the current head hides it.
        // The historical revision-1 key is no longer hidden by the smaller head.
        var scale = Silhouette.SharedRootScale(Silhouette.Proportions(Shipped.Content, "rio"));
        var head = SwingPresentation.HeadCenterAtRest with
        {
            Y = SwingPresentation.HeadCenterAtRest.Y - 0.2
        };
        var hidden = new SwingPresentation.Key(
            0, head, head, head,
            new Vec3(
                (HomeSet.BatterBodyX(Hand.R) + head.X * scale.X - plate.Pos.X) / scale.X,
                (head.Y * scale.Y - plate.Pos.Y) / scale.Y,
                (HomeSet.BatterZ + head.Z * scale.Z - plate.Pos.Z) / scale.Z),
            -0.2);
        Assert.True(
            SwingPresentation.BarrelBesideHeadDeg(hidden, Hand.R, plate, Silhouette.Proportions(Shipped.Content, "rio")) < 0,
            "a barrel behind the current head must fail the beside-head gate");
        Assert.True(
            SwingPresentation.BarrelBesideHeadDeg(
                SwingPresentation.At(SwingPresentation.LoadAt, Hand.R, SwingTake.Slap),
                Hand.R, plate, Silhouette.Proportions(Shipped.Content, "rio")) >= SwingPresentation.PlateLoadedBesideDeg);
    }

    [Fact]
    public void TheFinishHoldsThroughTheStampUntilSetOrTheFirstStep()
    {
        var feel = ContentCatalog.Load().Feel;
        var step = feel.SwingFinishStepFt;
        var takeSec = AtBatMotion.SwingTakeSeconds(Motion.SwingContact);
        // A whiff: the take, then its finish, however long the STRIKE stamp and the contact freeze last.
        for (var t = 0.0; t <= 5.0; t += 0.05)
            Assert.True(AtBatMotion.PresentsSwing(t, takeSec, contact: false, runnerFromBoxFt: 0, step));
        Assert.Equal(Motion.SwingFinish, AtBatMotion.CommittedSwingSample(5.0, takeSec), 8);
        // Contact: the finish holds while the batter-runner is still in the box, then lets go to the run.
        Assert.True(AtBatMotion.PresentsSwing(takeSec + 1.0, takeSec, contact: true, runnerFromBoxFt: step - 0.01, step));
        Assert.False(AtBatMotion.PresentsSwing(takeSec + 0.01, takeSec, contact: true, runnerFromBoxFt: step, step));
        // The take itself always plays through, contact or not.
        Assert.True(AtBatMotion.PresentsSwing(takeSec, takeSec, contact: true, runnerFromBoxFt: 99, step));
        Assert.False(AtBatMotion.PresentsSwing(AtBatMotion.SwingNotStarted, takeSec, contact: false, runnerFromBoxFt: 0, step));
    }

    [Fact]
    public void DccCatalogCarriesThePortableStanceDirections()
    {
        var repo = Directory.GetParent(ContentCatalog.Load().Root.Shipped)!.FullName;
        var path = Path.Combine(repo, "data", "art", "batting-stance.json");
        var keys = JsonNode.Parse(File.ReadAllText(path))!["keys"]!.AsArray();
        Assert.Equal(BattingStance.Keys.Count, keys.Count);
        for (var i = 0; i < keys.Count; i++)
        {
            var stance = BattingStance.Keys[i];
            AssertVector(stance.ChestForward, keys[i]!["chestForward"]!.AsArray());
            AssertVector(stance.EyesForward, keys[i]!["eyesForward"]!.AsArray());
            AssertVector(stance.FeetAxis, keys[i]!["feetAxis"]!.AsArray());
        }
    }

    [Fact]
    public void CommonBatIsAuthoredFromTheSharedGripTowardPositiveModelY()
    {
        Assert.Equal(new Vec3(0, -SwingPresentation.ModelCenterFromGrip, 0),
            SwingPresentation.ModelGrip);
        Assert.Equal(new Vec3(0, SwingPresentation.BarrelStartFromModelCenter, 0),
            SwingPresentation.ModelBarrelStart);
        Assert.Equal(new Vec3(0, SwingPresentation.HandleEndFromModelCenter, 0),
            SwingPresentation.ModelHandleEnd);
        Assert.Equal(new Vec3(0, SwingPresentation.BarrelFromModelCenter, 0),
            SwingPresentation.ModelBarrelEnd);
        Assert.True(SwingPresentation.ModelBarrelStart.Y > SwingPresentation.ModelGrip.Y);
        Assert.True(SwingPresentation.ModelHandleEnd.Y > SwingPresentation.ModelGrip.Y);
        Assert.True(SwingPresentation.ModelBarrelEnd.Y > SwingPresentation.ModelGrip.Y);
        Assert.Equal(
            (SwingPresentation.ModelBarrelEnd.Y - SwingPresentation.ModelGrip.Y) * Silhouette.BatScale,
            SwingPresentation.BarrelReach);
    }

    [Fact]
    public void BothHandsStayOnOneGripFromLoadThroughFollowThrough()
    {
        foreach (var take in new[] { SwingTake.Slap, SwingTake.Charge })
        foreach (var hand in new[] { Hand.R, Hand.L })
        foreach (var key in SwingPresentation.KeysFor(take))
        {
            var pose = SwingPresentation.At(key.T, hand, take);
            Assert.InRange(SwingPresentation.HandGap(pose), 0.20, 0.55);
            Assert.InRange(SwingPresentation.HandToHandle(pose, Hand.L), 0, 0.30);
            Assert.InRange(SwingPresentation.HandToHandle(pose, Hand.R), 0, 0.30);
        }
    }

    [Fact]
    public void ContactBarrelCutsThePlateForEverySharedCaptainAndHand()
    {
        foreach (var take in new[] { SwingTake.Slap, SwingTake.Charge })
        foreach (var body in Shipped.CaptainIds)
        foreach (var hand in new[] { Hand.R, Hand.L })
        {
            Assert.True(SwingPresentation.BarrelCrossesPlate(
                Silhouette.Proportions(Shipped.Content, body), hand, Motion.SwingContact, take), $"{take} {body} {hand} missed the plate");
        }
    }

    [Fact]
    public void BarrelApproachesContactUpwardThenWrapsToThePullSide()
    {
        Assert.InRange(Length(SwingPresentation.ModelBarrelAxisAtSocket), 0.999, 1.001);
        Assert.Equal(0.12 * Silhouette.BatScale, SwingPresentation.BarrelRadius, 8);
        foreach (var take in new[] { SwingTake.Slap, SwingTake.Charge })
        foreach (var hand in new[] { Hand.R, Hand.L })
        {
            Assert.True(SwingPresentation.At(SwingPresentation.LoadAt, hand, take).BarrelDirection.Y > 0.70);
            Assert.True(SwingPresentation.At(SwingPresentation.NormalLoadAt, hand, SwingTake.Charge).BarrelDirection.Y > 0.70);
            Assert.InRange(SwingPresentation.ContactAttackAngleDeg(take, hand), 5, 20);
            var contact = SwingPresentation.BarrelPoint(SwingPresentation.At(SwingPresentation.ContactAt, hand, take));
            var follow = SwingPresentation.BarrelPoint(SwingPresentation.At(SwingPresentation.FollowThroughAt, hand, take));
            var pullSign = hand == Hand.R ? -1 : 1;
            Assert.True((follow.X - contact.X) * pullSign > 1.0,
                $"{hand} follow x={follow.X:0.00}, contact x={contact.X:0.00}");
            Assert.True(follow.Y > contact.Y, $"{hand} barrel wraps above contact");
        }
    }

    [Fact]
    public void HandedBoxesMirrorButWalkOffsetKeepsWorldSign()
    {
        Assert.Equal(HomeSet.BatterX, HomeSet.BatterXFor(Hand.R));
        Assert.Equal(-HomeSet.BatterX, HomeSet.BatterXFor(Hand.L));
        Assert.Equal(HomeSet.BatterXFor(Hand.R) + HomeSet.BatterWalk,
            HomeSet.BatterBodyX(Hand.R, 1));
        Assert.Equal(HomeSet.BatterXFor(Hand.L) + HomeSet.BatterWalk,
            HomeSet.BatterBodyX(Hand.L, 1));
    }

    [Fact]
    public void MatchHoldsTheWorldBoxOffsetAfterResettingTheLiveCursor()
    {
        var match = Match.Slice(ContentCatalog.Load(), seed: 503);
        const double atContact = 0.35;
        match.BeginAtBat(
            new PitchCommand("fastball", 0, false),
            new SwingCommand(false, 0, 0, false, BoxOffsetX: atContact),
            out _, out _);

        Assert.Equal(0, match.BatterOffsetX);
        Assert.Equal(atContact, match.BatterContactOffsetX);
    }

    static double Distance(Vec3 a, Vec3 b) =>
        Math.Sqrt((a.X - b.X) * (a.X - b.X) +
                  (a.Y - b.Y) * (a.Y - b.Y) +
                  (a.Z - b.Z) * (a.Z - b.Z));

    static double Length(Vec3 v) => Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
    static double Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    static void AssertVector(Vec3 expected, JsonArray actual)
    {
        Assert.Equal(expected.X, actual[0]!.GetValue<double>(), 7);
        Assert.Equal(expected.Y, actual[1]!.GetValue<double>(), 7);
        Assert.Equal(expected.Z, actual[2]!.GetValue<double>(), 7);
    }
}
