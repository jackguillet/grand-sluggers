using Xunit;
using GrandSluggers.Sim;
using System.Text.Json.Nodes;

namespace GrandSluggers.Sim.Tests;

public class SwingPresentationTests
{
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
            Assert.True(Math.Abs(Dot(stance.FeetAxis, new Vec3(0, 0, 1)))
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
                var t = SwingPresentation.LoadSampleAt(charge);
                foreach (var hand in new[] { Hand.R, Hand.L })
                {
                    var direction = SwingPresentation.At(t, hand).BarrelDirection;
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
        for (var step = 0; step <= 200; step++)
        {
            var t = SwingPresentation.FollowThroughAt * step / 200.0;
            foreach (var hand in new[] { Hand.R, Hand.L })
            {
                var key = SwingPresentation.At(t, hand);
                foreach (var fist in new[] { Hand.L, Hand.R })
                    Assert.True(
                        SwingPresentation.HandToHandle(key, fist)
                            <= SwingPresentation.HandToHandleAllowance,
                        $"{fist} fist leaves the handle at {t:0.000}: "
                        + $"{SwingPresentation.HandToHandle(key, fist):0.000}");
            }
        }
    }

    [Fact]
    public void DccCatalogCarriesThePortableStanceDirections()
    {
        var repo = Directory.GetParent(ContentCatalog.Load().Root)!.FullName;
        var path = Path.Combine(repo, "data", "art", "pose-clips", "swing.json");
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
        foreach (var hand in new[] { Hand.R, Hand.L })
        foreach (var key in SwingPresentation.Keys)
        {
            var pose = SwingPresentation.At(key.T, hand);
            Assert.InRange(SwingPresentation.HandGap(pose), 0.20, 0.55);
            Assert.InRange(SwingPresentation.HandToHandle(pose, Hand.L), 0, 0.30);
            Assert.InRange(SwingPresentation.HandToHandle(pose, Hand.R), 0, 0.30);
        }
    }

    [Fact]
    public void ContactBarrelCutsThePlateForEverySharedCaptainAndHand()
    {
        foreach (var body in SwingPresentation.SharedCaptains)
        foreach (var hand in new[] { Hand.R, Hand.L })
        {
            Assert.True(SwingPresentation.BarrelCrossesPlate(
                body, hand, MoveBones.SwingContact), $"{body} {hand} missed the plate");
        }
    }

    [Fact]
    public void BarrelApproachesContactUpwardThenWrapsToThePullSide()
    {
        Assert.InRange(Length(SwingPresentation.ModelBarrelAxisAtSocket), 0.999, 1.001);
        Assert.Equal(0.12 * Silhouette.BatScale, SwingPresentation.BarrelRadius, 8);
        foreach (var hand in new[] { Hand.R, Hand.L })
        {
            Assert.True(SwingPresentation.At(SwingPresentation.LoadAt, hand).BarrelDirection.Y > 0.70);
            Assert.True(SwingPresentation.At(SwingPresentation.NormalLoadAt, hand).BarrelDirection.Y > 0.70);
            Assert.InRange(SwingPresentation.ContactAttackAngleDeg(hand), 5, 20);
            var contact = SwingPresentation.BarrelPoint(SwingPresentation.At(SwingPresentation.ContactAt, hand));
            var follow = SwingPresentation.BarrelPoint(SwingPresentation.At(SwingPresentation.FollowThroughAt, hand));
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
            new PitchCommand("fastball", 0, 40, false),
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
