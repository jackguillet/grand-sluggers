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
        var window = rules.Batting.Window.SlapFrames;
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
        var window = rules.Batting.Window.SlapFrames;
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
        // A wide window (EASY, contact 10) can hold a press after the ball is on the plate.
        const double window = 14.3;
        var err = rules.Batting.Window.LeadSec * 60 + 1;
        Assert.True(AtBatResolver.InWindow(err, window));
        var contactSec = AtBatMotion.SwingContactSec(err, window, rules);
        Assert.Equal(0, contactSec, 8);
        Assert.Equal(Motion.SwingContact, AtBatMotion.SwingClipTime(0, 0, contactSec), 8);
        Assert.Equal(SwingPresentation.CommittedLoadAt(0), AtBatMotion.SwingClipTime(-0.01, 0, contactSec), 8);
        // The normal slap window never needs that clamp: its latest press is still before the ball.
        Assert.True(rules.Batting.Window.LeadSec * 60 > rules.Batting.Window.SlapFrames / 2,
            "batting.window.leadSec must cover half the slap window so contact meets the ball");
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
        foreach (var body in SwingPresentation.SharedCaptains)
        {
            var scale = Silhouette.SharedRootScale(Silhouette.Proportions(body));
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
        var repo = Directory.GetParent(ContentCatalog.Load().Root)!.FullName;
        var doc = JsonNode.Parse(File.ReadAllText(Path.Combine(repo, "data", "art", "swing-takes.json")))!;
        Assert.Equal(Motion.SwingContact, doc["contactAt"]!.GetValue<double>(), 8);
        Assert.Equal(Motion.SwingFinish, doc["finishAt"]!.GetValue<double>(), 8);
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
        var repo = Directory.GetParent(ContentCatalog.Load().Root)!.FullName;
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
        foreach (var body in SwingPresentation.SharedCaptains)
        foreach (var hand in new[] { Hand.R, Hand.L })
        {
            Assert.True(SwingPresentation.BarrelCrossesPlate(
                body, hand, Motion.SwingContact, take), $"{take} {body} {hand} missed the plate");
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
