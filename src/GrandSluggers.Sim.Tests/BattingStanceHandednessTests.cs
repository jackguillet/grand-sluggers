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
/// The side opposite the batting hand leads: that hand holds the knob end of
/// the handle under the top hand, and that foot stands nearer the pitcher. A
/// left-handed batter is the right-handed pose reflected across the plate
/// line, so every check below is written against the lead side rather than
/// against "left" or "right", and holds for both boxes.
///
/// LeftHand / RightHand and the lHand / lShoe landmarks are the batter's own
/// left: the blockout places them at Blender -X with the character facing +Y,
/// and the FBX import X reflection cancels against the -Z export facing.
/// </summary>
public class BattingStanceHandednessTests
{
    static readonly Hand[] Both = [Hand.R, Hand.L];

    [Fact]
    public void LeadSideIsOppositeTheBattingHand()
    {
        Assert.Equal(Hand.L, BattingStance.LeadSide(Hand.R));
        Assert.Equal(Hand.R, BattingStance.LeadSide(Hand.L));
    }

    /// <summary>
    /// FeetAxis runs from the back foot to the lead foot, so it points at the
    /// pitcher for both batters: the reflection that turns a right-handed
    /// batter into a left-handed one swaps which foot leads and leaves the
    /// pitcher where it was. Signed on purpose -- an unsigned check scored a
    /// take with the hips turned out of the box the same as a correct one.
    /// </summary>
    [Theory]
    [InlineData(SwingPresentation.LoadAt)]
    [InlineData(SwingPresentation.LaunchAt)]
    [InlineData(SwingPresentation.ContactAt)]
    [InlineData(SwingPresentation.FollowThroughAt)]
    public void LeadFootStandsTowardThePitcherForBothHands(double poseT)
    {
        foreach (var bats in Both)
        {
            var stance = BattingStance.At(poseT, bats);
            Assert.True(stance.FeetAxis.Z >= BattingStance.AlignmentDot,
                $"{bats} batter's back-to-lead foot line must point at the pitcher "
                + $"(z={stance.FeetAxis.Z:0.000})");
        }
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

    /// <summary>
    /// The bottom hand on the handle is the lead hand. Jack's reference: a
    /// right-handed hitter's LEFT hand is below the right, and a left-handed
    /// hitter's RIGHT hand is below the left. Measured in the held load, where
    /// the barrel stands above the hands so "below" also means "at the knob".
    /// </summary>
    [Theory]
    [InlineData(Hand.R)]
    [InlineData(Hand.L)]
    public void LeadHandRidesUnderTheTopHand(Hand bats)
    {
        foreach (var take in new[] { SwingTake.Slap, SwingTake.Charge })
        {
            var key = SwingPresentation.At(SwingPresentation.LoadAt, bats, take);
            var lead = BattingStance.LeadSide(bats);
            var leadY = lead == Hand.L ? key.LeftHand.Y : key.RightHand.Y;
            var topY = lead == Hand.L ? key.RightHand.Y : key.LeftHand.Y;
            Assert.True(leadY < topY,
                $"{take} {bats} batter: the {lead} hand must sit below the {bats} hand on the "
                + $"handle (lead y={leadY:0.000}, top y={topY:0.000})");
        }
    }

    /// <summary>
    /// A two-handed grip cannot swap hands mid-swing. Height alone cannot say
    /// which hand is which once the barrel comes level at approach and contact
    /// -- read that way the old keys looked like they flipped twice -- so
    /// measure along the handle: the lead hand stays nearest the knob on every
    /// key and on every sample between them.
    /// </summary>
    [Theory]
    [InlineData(Hand.R)]
    [InlineData(Hand.L)]
    public void LeadHandHoldsTheKnobEndThroughTheWholeSwing(Hand bats)
    {
        var lead = BattingStance.LeadSide(bats);
        foreach (var take in new[] { SwingTake.Slap, SwingTake.Charge })
        for (var step = 0; step <= 100; step++)
        {
            var t = SwingPresentation.FinishAt * step / 100.0;
            var key = SwingPresentation.At(t, bats, take);
            var leadAlong = SwingPresentation.HandAlongHandle(key, lead);
            var topAlong = SwingPresentation.HandAlongHandle(key, bats);
            Assert.True(leadAlong >= 0 && leadAlong < topAlong,
                $"{take} {bats} batter at {t:0.000}: the {lead} hand must hold the knob end "
                + $"(lead {leadAlong:0.000} ft up the handle, top {topAlong:0.000})");
        }
    }

    [Fact]
    public void MirroringSwapsTheHandsWithoutChangingTheGrip()
    {
        var r = SwingPresentation.At(SwingPresentation.LoadAt, Hand.R, SwingTake.Charge);
        var l = SwingPresentation.At(SwingPresentation.LoadAt, Hand.L, SwingTake.Charge);

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
