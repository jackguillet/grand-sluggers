using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The verb catalog is the whole runtime motion contract: every verb names a
/// take, a clock, and (where it matters) a hand. There are no poses in C#.
/// </summary>
public class MotionTests
{
    [Fact]
    public void EveryVerbPlaysACatalogClip()
    {
        foreach (var verb in Motion.Verbs)
        {
            var cue = Motion.CueFor(verb);
            Assert.True(Motion.TryClip(cue.Clip, out var clip), $"{verb} plays {cue.Clip} which is not a clip");
            Assert.Equal(clip.Loop, cue.Clock == Motion.Clock.World);
        }
    }

    [Fact]
    public void HeldLoadsShareTheirCommittedTake()
    {
        // The held windup is the charge take a MAX release continues (#613).
        Assert.Equal(Motion.CueFor(Motion.Verb.ChargeSwing).Clip, Motion.CueFor(Motion.Verb.Swing, 1).Clip);
        Assert.Equal(Motion.CueFor(Motion.Verb.ChargePitch).Clip, Motion.CueFor(Motion.Verb.ThrowPitch).Clip);
        Assert.Equal(Motion.Clock.Charge, Motion.CueFor(Motion.Verb.ChargeSwing).Clock);
        Assert.Equal(Motion.Clock.Charge, Motion.CueFor(Motion.Verb.ChargePitch).Clock);
        Assert.Equal(Motion.Clock.Verb, Motion.CueFor(Motion.Verb.Swing).Clock);
        Assert.Equal(Motion.Clock.Verb, Motion.CueFor(Motion.Verb.ThrowPitch).Clock);
    }

    [Fact]
    public void HandedTakesAreExactlyTheHittingAndThrowingOnes()
    {
        var handed = Motion.Clips.Where(c => c.Handed).Select(c => c.Id).ToHashSet();
        Assert.Equal(new HashSet<string> { "swing-slap", "swing-charge", "pitch", "throw", "checkSwing", "bunt", "miss" }, handed);
        foreach (var verb in Motion.Verbs)
        {
            var clip = Motion.CueFor(verb).Clip;
            if (Motion.UsesBattingHand(verb))
                Assert.True(Motion.IsHanded(clip), $"{verb} is a hitting verb and needs a handed take");
        }
    }

    [Fact]
    public void LeftHandersPlayTheBakedMirrorNotARuntimeFlip()
    {
        Assert.Equal("swing-slap-L", Motion.ClipFile(Motion.Verb.Swing, Hand.L, Hand.R));
        Assert.Equal("swing-slap", Motion.ClipFile(Motion.Verb.Swing, Hand.R, Hand.L));
        Assert.Equal("swing-charge-L", Motion.ClipFile(Motion.Verb.Swing, Hand.L, Hand.R, charge01: 1));
        Assert.Equal("swing-charge", Motion.ClipFile(Motion.Verb.ChargeSwing, Hand.R, Hand.L));
        Assert.Equal("pitch-L", Motion.ClipFile(Motion.Verb.ThrowPitch, Hand.R, Hand.L));
        Assert.Equal("pitch", Motion.ClipFile(Motion.Verb.ChargePitch, Hand.L, Hand.R));
        Assert.Equal("throw-L", Motion.ClipFile(Motion.Verb.Throw, Hand.L, Hand.L));
        Assert.Equal("run", Motion.ClipFile(Motion.Verb.Run, Hand.L, Hand.L));
        Assert.Equal("jump", Motion.ClipFile(Motion.Verb.Clamber, Hand.L, Hand.L));
    }

    [Fact]
    public void MarkersAreTheBallEvents()
    {
        Assert.Equal(Motion.SwingContact, Motion.Mark(Motion.Verb.Swing, Motion.ClipEvent.Contact));
        Assert.Equal(Motion.PitchRelease, Motion.Mark(Motion.Verb.ThrowPitch, Motion.ClipEvent.Release));
        Assert.Equal(Motion.ThrowRelease, Motion.Mark(Motion.Verb.Throw, Motion.ClipEvent.Release));
        Assert.Equal(Motion.ScoopContact, Motion.Mark(Motion.Verb.Scoop, Motion.ClipEvent.Contact));
        Assert.Equal(Motion.SlidePlant, Motion.Mark(Motion.Verb.Slide, Motion.ClipEvent.FootPlant));
        Assert.Equal(Motion.JumpDur, Motion.Mark(Motion.Verb.Clamber, Motion.ClipEvent.FootPlant));
        Assert.Equal(0, Motion.Mark(Motion.Verb.Swing, Motion.ClipEvent.Release));
        foreach (var clip in Motion.Clips)
        {
            if (clip.Mark == null) continue;
            Assert.InRange(clip.MarkAt, 0, clip.Duration);
        }
        // The swings end on their held finish (#583): the last second of the take, after contact.
        foreach (var id in new[] { Motion.SwingSlapClip, Motion.SwingChargeClip })
        {
            Assert.True(Motion.TryClip(id, out var swing));
            Assert.Equal(Motion.SwingFinish, swing.FinishAt, 8);
            Assert.Equal(swing.Duration, swing.FinishAt, 8);
            Assert.True(swing.FinishAt > Motion.SwingDur && Motion.SwingDur > swing.MarkAt);
        }
    }

    [Fact]
    public void HoldsAreOneShotsWithoutAMarker()
    {
        Assert.True(Motion.Holds(Motion.Verb.Crouch));
        Assert.True(Motion.Holds(Motion.Verb.Bunt));
        Assert.False(Motion.Holds(Motion.Verb.Swing));
        Assert.False(Motion.Holds(Motion.Verb.Idle));
    }

    [Fact]
    public void RunCycleIsTheFeelStrideRate()
    {
        Assert.True(Motion.TryClip("run", out var run));
        Assert.Equal(1 / Motion.RunHz, run.Duration, 8);
        Assert.True(Motion.TryClip("walk", out var walk));
        Assert.True(walk.Duration > run.Duration, "a walk cycle is slower than a run cycle");
    }
}
