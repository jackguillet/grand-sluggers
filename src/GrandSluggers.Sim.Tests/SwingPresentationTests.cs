using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class SwingPresentationTests
{
    [Fact]
    public void CommonBatIsAuthoredFromTheSharedGripTowardPositiveModelY()
    {
        Assert.Equal(new Vec3(0, -SwingPresentation.ModelCenterFromGrip, 0),
            SwingPresentation.ModelGrip);
        Assert.Equal(new Vec3(0, SwingPresentation.BarrelFromModelCenter, 0),
            SwingPresentation.ModelBarrelEnd);
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
            Assert.InRange(SwingPresentation.HandGap(pose), 0.18, 0.32);
            Assert.InRange(Distance(pose.LeftHand, pose.Grip), 0, 0.55);
            Assert.InRange(Distance(pose.RightHand, pose.Grip), 0, 0.55);
        }
    }

    [Fact]
    public void ContactBarrelCutsThePlateForEverySharedCaptainAndHand()
    {
        foreach (var body in SwingPresentation.SharedCaptains)
        foreach (var hand in new[] { Hand.R, Hand.L })
        {
            var contact = SwingPresentation.BarrelWorld(body, hand, MoveBones.SwingContact);
            Assert.InRange(contact.X, -HomeSet.PlateW / 2, HomeSet.PlateW / 2);
            Assert.InRange(contact.Z, HomeSet.PlatePointZ, HomeSet.PlateFrontZ);
            Assert.InRange(contact.Y, PitchFlight.PlateY - 1.2, PitchFlight.PlateY + 1.2);
        }
    }

    [Fact]
    public void BarrelApproachesContactUpwardThenWrapsToThePullSide()
    {
        Assert.InRange(Length(SwingPresentation.ModelBarrelAxisAtSocket), 0.999, 1.001);
        Assert.Equal(0.24 * Silhouette.BatScale, SwingPresentation.BarrelRadius, 8);
        foreach (var hand in new[] { Hand.R, Hand.L })
        {
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
}
