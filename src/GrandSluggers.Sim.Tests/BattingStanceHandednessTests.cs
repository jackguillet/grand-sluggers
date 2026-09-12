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
/// Right-handed batters are confirmed correct on screen, so they are the
/// anchor: whatever a right-handed batter renders today is the reference, and
/// a left-handed batter has to stand the same way round the other box.
///
/// The feet axis is a WORLD axis, not an identity-mirrored one. Both batters
/// set their feet along the pitch line pointing the same way; handedness turns
/// the chest and swaps which hand is low, it does not reverse the feet. That is
/// already what <see cref="BattingStance"/> authors -- MirrorX leaves Z alone --
/// and it is what the rendered left-handed body currently disagrees with.
/// </summary>
public class BattingStanceHandednessTests
{
    static Hand Other(Hand hand) => hand == Hand.R ? Hand.L : Hand.R;

    [Theory]
    [InlineData(SwingPresentation.LoadAt)]
    [InlineData(SwingPresentation.LaunchAt)]
    public void FeetAxisPointsTheSameWayForBothHands(double poseT)
    {
        var r = BattingStance.At(poseT, Hand.R);
        var l = BattingStance.At(poseT, Hand.L);
        Assert.True(r.FeetAxis.Z > 0.9,
            $"right-handed feet axis should run toward the pitcher (z={r.FeetAxis.Z:0.000})");
        Assert.True(l.FeetAxis.Z > 0.9,
            "left-handed feet axis must point the same way as right-handed: handedness "
            + $"turns the chest, it does not reverse the feet (z={l.FeetAxis.Z:0.000})");
    }

    [Theory]
    [InlineData(Hand.R)]
    [InlineData(Hand.L)]
    public void ChestTurnsToTheBatterOwnPlateSide(Hand bats)
    {
        var stance = BattingStance.At(SwingPresentation.LoadAt, bats);
        var plate = BattingStance.PlateDirection(bats);
        var dot = stance.ChestForward.X * plate.X
            + stance.ChestForward.Y * plate.Y
            + stance.ChestForward.Z * plate.Z;
        Assert.True(dot >= BattingStance.AlignmentDot,
            $"{bats} batter's chest must face its own plate side (dot={dot:0.000})");
    }

    [Theory]
    [InlineData(Hand.R)]
    [InlineData(Hand.L)]
    public void EyesWatchThePitcherFromEitherBox(Hand bats)
    {
        var stance = BattingStance.At(SwingPresentation.LoadAt, bats);
        Assert.True(stance.EyesForward.Z >= BattingStance.AlignmentDot,
            $"{bats} batter must watch the pitcher (z={stance.EyesForward.Z:0.000})");
    }

    [Fact]
    public void MirroringSwapsTheHandsWithoutChangingTheGrip()
    {
        var r = SwingPresentation.At(SwingPresentation.LoadAt, Hand.R);
        var l = SwingPresentation.At(SwingPresentation.LoadAt, Hand.L);

        static double Gap(SwingPresentation.Key k)
        {
            var dx = k.LeftHand.X - k.RightHand.X;
            var dy = k.LeftHand.Y - k.RightHand.Y;
            var dz = k.LeftHand.Z - k.RightHand.Z;
            return System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        Assert.Equal(Gap(r), Gap(l), 6);
        // The low hand swaps sides, so the two batters are mirror images on the
        // handle rather than the same hand order in both boxes.
        Assert.Equal(r.LeftHand.Y < r.RightHand.Y, l.RightHand.Y < l.LeftHand.Y);
    }
}
