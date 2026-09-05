using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class CharacterMotionTests
{
    [Fact]
    public void IdleKeepsLimbsNearRest()
    {
        var s = CharacterMotion.Evaluate(MoveBones.Verb.Idle, 0.4, 0);
        Assert.InRange(s.RUpper.X, -20, 20);
        Assert.InRange(s.LThigh.X, -20, 20);
    }

    [Fact]
    public void SwingFlexesTheHittingArm()
    {
        var load = CharacterMotion.Evaluate(MoveBones.Verb.Swing, 0, 0.05);
        var contact = CharacterMotion.Evaluate(MoveBones.Verb.Swing, 0, MoveBones.SwingContact);
        Assert.True(contact.RUpper.X > load.RUpper.X, $"load {load.RUpper.X} contact {contact.RUpper.X}");
        Assert.True(Math.Abs(contact.RUpper.X) > 10);
    }

    [Fact]
    public void RunOpposesTheLegs()
    {
        var s = CharacterMotion.Evaluate(MoveBones.Verb.Run, 0.1, 0);
        Assert.True(s.LThigh.X * s.RThigh.X <= 0 || Math.Abs(s.LThigh.X) + Math.Abs(s.RThigh.X) < 1);
    }

    [Fact]
    public void PitchAndSwingAreDifferentArmsStory()
    {
        var pitch = CharacterMotion.Evaluate(MoveBones.Verb.Pitch, 0, 0.1);
        var swing = CharacterMotion.Evaluate(MoveBones.Verb.Swing, 0, 0.1);
        Assert.NotEqual(pitch.RUpper.X, swing.RUpper.X);
    }
}
