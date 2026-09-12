using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Jack's stance contract, stated for both hands:
///
///   Right-handed: bat hovers above the RIGHT shoulder, feet face the plate,
///                 the LEFT hand is below the right.
///   Left-handed:  bat hovers above the LEFT shoulder, feet face the plate,
///                 the RIGHT hand is below the left.
///
/// The lead side is the one opposite the batting hand: a right-handed batter
/// leads with the left hand low on the handle and the left foot toward the
/// pitcher. The swing matrix cannot see any of this today -- it takes
/// Mathf.Abs of the feet axis, so the front foot has no sign, and it never
/// compares the two hands at all.
/// </summary>
public class BattingStanceHandednessTests
{
    static double LowerHandY(Hand hand)
    {
        var key = SwingPresentation.At(SwingPresentation.LoadAt, hand);
        return System.Math.Min(key.LeftHand.Y, key.RightHand.Y);
    }

    [Theory]
    [InlineData(Hand.R)]
    [InlineData(Hand.L)]
    public void LeadHandIsLowOnTheHandle(Hand bats)
    {
        var key = SwingPresentation.At(SwingPresentation.LoadAt, bats);
        // Lead hand is opposite the batting hand.
        var leadIsLeft = bats == Hand.R;
        var leadY = leadIsLeft ? key.LeftHand.Y : key.RightHand.Y;
        var topY = leadIsLeft ? key.RightHand.Y : key.LeftHand.Y;
        Assert.True(leadY < topY,
            $"{bats} batter: the {(leadIsLeft ? "left" : "right")} hand must sit below the "
            + $"{(leadIsLeft ? "right" : "left")} hand on the handle "
            + $"(lead y={leadY:0.000}, top y={topY:0.000})");
    }

    [Theory]
    [InlineData(Hand.R)]
    [InlineData(Hand.L)]
    public void HandsStayOnTheHandleWhicheverWayTheBatterStands(Hand bats)
    {
        var r = SwingPresentation.At(SwingPresentation.LoadAt, Hand.R);
        var m = SwingPresentation.At(SwingPresentation.LoadAt, bats);
        // Mirroring may swap which hand is where; it must not change how far
        // apart they are, or one batter would hold a different bat.
        double Gap(SwingPresentation.Key k) =>
            System.Math.Sqrt(
                (k.LeftHand.X - k.RightHand.X) * (k.LeftHand.X - k.RightHand.X)
                + (k.LeftHand.Y - k.RightHand.Y) * (k.LeftHand.Y - k.RightHand.Y)
                + (k.LeftHand.Z - k.RightHand.Z) * (k.LeftHand.Z - k.RightHand.Z));
        Assert.Equal(Gap(r), Gap(m), 6);
    }

    [Theory]
    [InlineData(Hand.R)]
    [InlineData(Hand.L)]
    public void FrontFootIsTheLeadFootTowardThePitcher(Hand bats)
    {
        // FeetAxis is authored as a world axis, so it cannot say which foot is
        // in front. The rendered body puts the batting-side foot toward the
        // pitcher for both hands; the lead foot belongs there instead.
        var stance = BattingStance.At(SwingPresentation.LoadAt, bats);
        Assert.True(System.Math.Abs(stance.FeetAxis.Z) > 0.9,
            "feet axis should run along the pitch line");
        // A handed stance must distinguish the two hands somewhere. If the
        // authored axis is identical for both, the front foot is unspecified
        // and the gate's Mathf.Abs cannot catch a reversed stance.
        var other = BattingStance.At(SwingPresentation.LoadAt, bats == Hand.R ? Hand.L : Hand.R);
        Assert.True(stance.FeetAxis.Z * other.FeetAxis.Z < 0,
            "the feet axis must flip between a right- and left-handed batter so the "
            + "lead foot is the one toward the pitcher; today both hands author the "
            + $"same axis (R z={other.FeetAxis.Z:0.000}, {bats} z={stance.FeetAxis.Z:0.000})");
    }
}
