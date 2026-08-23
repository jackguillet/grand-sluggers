using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class MoveBonesTests
{
    [Fact]
    public void RunPlantHasForwardPlantLegAndOppositeArm()
    {
        var tPlant = 0.0;
        Assert.InRange(MoveBones.RunPhase(tPlant), 0.0, 0.08);
        var plant = MoveBones.Evaluate(MoveBones.Verb.Run, tPlant, 0);
        Assert.True(plant.LThigh.X > plant.RThigh.X,
            $"left plant thigh {plant.LThigh.X} vs right {plant.RThigh.X}");
        Assert.True(plant.RUpper.X > plant.LUpper.X,
            $"opposite arm: right {plant.RUpper.X} vs left {plant.LUpper.X}");
        Assert.True(plant.RShin.X > plant.LShin.X,
            $"trailing shin flex {plant.RShin.X} vs plant {plant.LShin.X}");

        var tOther = 0.5 / MoveBones.RunHz;
        var other = MoveBones.Evaluate(MoveBones.Verb.Run, tOther, 0);
        Assert.True(other.RThigh.X > other.LThigh.X,
            $"right plant {other.RThigh.X} vs left {other.LThigh.X}");
        Assert.NotEqual(plant.LThigh.X, other.LThigh.X);
        Assert.NotEqual(plant.RUpper.X, other.RUpper.X);
    }

    [Fact]
    public void JumpLiftPeaksInTheHangNotAtTakeoffOrLand()
    {
        var take = MoveBones.Evaluate(MoveBones.Verb.Jump, 0, 0.02);
        var hang = MoveBones.Evaluate(MoveBones.Verb.Jump, 0, MoveBones.JumpDur * 0.5);
        var land = MoveBones.Evaluate(MoveBones.Verb.Jump, 0, MoveBones.JumpDur * 0.95);
        Assert.True(hang.Lift > take.Lift, $"hang {hang.Lift} vs takeoff {take.Lift}");
        Assert.True(hang.Lift > land.Lift, $"hang {hang.Lift} vs land {land.Lift}");
        Assert.True(hang.Lift > 2.5, $"peak lift {hang.Lift}");
        Assert.Equal(hang.Lift, MoveBones.JumpLift(MoveBones.JumpDur * 0.5));
    }

    [Fact]
    public void SwingHipsOpenThenBatCutsThenWraps()
    {
        var load = MoveBones.Evaluate(MoveBones.Verb.Swing, 0, 0);
        var hips = MoveBones.Evaluate(MoveBones.Verb.Swing, 0, 0.10);
        var cut = MoveBones.Evaluate(MoveBones.Verb.Swing, 0, 0.30);
        var wrap = MoveBones.Evaluate(MoveBones.Verb.Swing, 0, 0.48);
        Assert.True(hips.Torso.Y > load.Torso.Y,
            $"hips lead: torso Y {hips.Torso.Y} vs load {load.Torso.Y}");
        Assert.True(Math.Abs(hips.RUpper.X - load.RUpper.X) < Math.Abs(cut.RUpper.X - load.RUpper.X),
            "at hip-open the rear arm has not yet reached the cut");
        Assert.True(cut.Torso.Y > hips.Torso.Y, $"cut torso {cut.Torso.Y} vs hips {hips.Torso.Y}");
        Assert.True(cut.Bat.Y > load.Bat.Y, $"bat through zone {cut.Bat.Y} vs load {load.Bat.Y}");
        Assert.True(wrap.Torso.Y > cut.Torso.Y || wrap.Bat.Y > cut.Bat.Y,
            $"wrap past cut torso {wrap.Torso.Y}/{cut.Torso.Y} bat {wrap.Bat.Y}/{cut.Bat.Y}");
    }

    [Fact]
    public void SwingContactIsTheCutAndPitchReleaseIsTheArmForward()
    {
        Assert.True(MoveBones.Mark(MoveBones.Verb.Swing, MoveBones.ClipEvent.Contact) > 0);
        Assert.True(MoveBones.Mark(MoveBones.Verb.Pitch, MoveBones.ClipEvent.Release) > 0);
        var hips = MoveBones.Evaluate(MoveBones.Verb.Swing, 0, 0.10);
        var contact = MoveBones.Evaluate(MoveBones.Verb.Swing, 0, MoveBones.SwingContact);
        var wrap = MoveBones.Evaluate(MoveBones.Verb.Swing, 0, 0.48);
        Assert.True(contact.Torso.Y > hips.Torso.Y, "contact after hips open");
        Assert.True(wrap.Torso.Y > contact.Torso.Y || wrap.Bat.Y > contact.Bat.Y, "wrap after contact");
        var wind = MoveBones.Evaluate(MoveBones.Verb.Pitch, 0, 0.04);
        var rel = MoveBones.Evaluate(MoveBones.Verb.Pitch, 0, MoveBones.PitchRelease);
        Assert.True(rel.RUpper.X > wind.RUpper.X, "release arm forward of windup");
    }

    [Fact]
    public void PitchReleaseIsForwardOfWindupWithStrideLeg()
    {
        var wind = MoveBones.Evaluate(MoveBones.Verb.Pitch, 0, 0.04);
        var rel = MoveBones.Evaluate(MoveBones.Verb.Pitch, 0, 0.42);
        Assert.True(rel.RUpper.X > wind.RUpper.X,
            $"arm forward {rel.RUpper.X} vs windup {wind.RUpper.X}");
        Assert.True(rel.LThigh.X > wind.LThigh.X,
            $"stride {rel.LThigh.X} vs windup {wind.LThigh.X}");
        var charged = MoveBones.Evaluate(MoveBones.Verb.ChargePitch, 0, 0, charge: 1);
        var loose = MoveBones.Evaluate(MoveBones.Verb.ChargePitch, 0, 0, charge: 0);
        Assert.True(charged.RUpper.X < loose.RUpper.X,
            $"loaded arm {charged.RUpper.X} vs loose {loose.RUpper.X}");
    }
}
