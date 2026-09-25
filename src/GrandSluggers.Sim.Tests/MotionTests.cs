using System.Linq;
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
        Assert.Equal(Motion.CueFor(Motion.Verb.ChargeSwing).Clip, Motion.CueFor(Motion.Verb.Swing, 1, Shipped.Content.Rules).Clip);
        Assert.Equal(Motion.CueFor(Motion.Verb.ChargePitch).Clip, Motion.CueFor(Motion.Verb.ThrowPitch, 1, Shipped.Content.Rules).Clip);
        Assert.Equal(Motion.Clock.Charge, Motion.CueFor(Motion.Verb.ChargeSwing).Clock);
        Assert.Equal(Motion.Clock.Charge, Motion.CueFor(Motion.Verb.ChargePitch).Clock);
        Assert.Equal(Motion.Clock.Verb, Motion.CueFor(Motion.Verb.Swing).Clock);
        Assert.Equal(Motion.Clock.Verb, Motion.CueFor(Motion.Verb.ThrowPitch).Clock);
    }

    [Fact]
    public void TheSquaredTakeShowsTheHeldSideForEitherHand()
    {
        // PH-14-R3: third is a right-handed batter's pull field and a left-handed batter's push field; the lefty file is the mirror.
        Assert.Equal("bunt-pull", Motion.ClipFor(Motion.Verb.Bunt, Hand.R, null, bunt: BuntSide.Third));
        Assert.Equal("bunt-push", Motion.ClipFor(Motion.Verb.Bunt, Hand.R, null, bunt: BuntSide.First));
        Assert.Equal("bunt-push-L", Motion.ClipFor(Motion.Verb.Bunt, Hand.L, null, bunt: BuntSide.Third));
        Assert.Equal("bunt-pull-L", Motion.ClipFor(Motion.Verb.Bunt, Hand.L, null, bunt: BuntSide.First));
        Assert.Equal("bunt", Motion.ClipFor(Motion.Verb.Bunt, Hand.R, null));
        // The side only picks the squared take.
        Assert.Equal("swing-slap", Motion.ClipFor(Motion.Verb.Swing, Hand.R, null, bunt: BuntSide.Third));
    }

    [Fact]
    public void TheLetGoStartsOnTheDiscardedLoadAndMatchesItsTake()
    {
        // PH-13-R1: a full load lets go from the coil, no load starts on the stance, a partial load part-way in.
        Assert.Equal(0, Motion.LetGoStartAt(1), 9);
        Assert.Equal(Motion.LetGoReturnAt, Motion.LetGoStartAt(0), 9);
        Assert.Equal(Motion.LetGoReturnAt * 0.4, Motion.LetGoStartAt(0.6), 9);
        Assert.Equal(Motion.Clock.Verb, Motion.CueFor(Motion.Verb.LetGo).Clock);
        Assert.True(Motion.UsesBattingHand(Motion.Verb.LetGo));
        // The take's numbers are the data the bake reads: it walks the charge take from its coil to its no-charge stance.
        var path = Path.Combine(Shipped.Content.Root.Shipped, "art", "baseball-takes.json");
        var letGo = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!["letGo"]!;
        Assert.Equal(Motion.LetGoDur, letGo["duration"]!.GetValue<double>(), 9);
        Assert.Equal(Motion.LetGoReturnAt, letGo["returnAt"]!.GetValue<double>(), 9);
        Assert.Equal(Motion.SwingChargeClip, letGo["source"]!.GetValue<string>());
        Assert.Equal(SwingPresentation.HeldLoadAt(1), letGo["fromAt"]!.GetValue<double>(), 9);
        Assert.Equal(SwingPresentation.HeldLoadAt(0), letGo["toAt"]!.GetValue<double>(), 9);
    }

    [Fact]
    public void TheDiveInFlightIsItsOwnHeldTake()
    {
        // The lunge plays the layout off the dirt; the ground dive stays the recovery and the knockback's.
        var air = Motion.CueFor(Motion.Verb.DiveAir);
        Assert.Equal(Motion.DiveAirClip, air.Clip);
        Assert.Equal(Motion.Clock.Verb, air.Clock);
        Assert.True(Motion.Holds(Motion.Verb.DiveAir));
        Assert.NotEqual(Motion.CueFor(Motion.Verb.Dive).Clip, air.Clip);
        Assert.False(Motion.IsHanded(Motion.DiveAirClip));
    }

    [Fact]
    public void HandedTakesAreExactlyTheHittingAndThrowingOnes()
    {
        var handed = Motion.Clips.Where(c => c.Handed).Select(c => c.Id).ToHashSet();
        // The catcher's throw and the sweep tag are the throwing hand's too (#966).
        Assert.Equal(new HashSet<string> { "swing-slap", "swing-charge", "pitch", "pitch-charge", "throw", "checkSwing", "bunt", "bunt-pull", "bunt-push", "swing-letgo", "miss",
            "catcherThrow", "tag" }, handed);
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
        Assert.Equal("swing-charge-L", Motion.ClipFile(Motion.Verb.Swing, Hand.L, Hand.R, 1, Shipped.Content.Rules));
        Assert.Equal("swing-charge", Motion.ClipFile(Motion.Verb.ChargeSwing, Hand.R, Hand.L));
        Assert.Equal("pitch-L", Motion.ClipFile(Motion.Verb.ThrowPitch, Hand.R, Hand.L));
        Assert.Equal("pitch-charge", Motion.ClipFile(Motion.Verb.ChargePitch, Hand.L, Hand.R));
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

    /// <summary>
    /// Catalog first, then the take (#966): a stand-in slot plays an authored clip until its own lands, every file, marker and
    /// hold the stand-in's, and the stand-in is never itself a stand-in.
    /// </summary>
    [Fact]
    public void AStandInSlotPlaysAnAuthoredClipUntilItsOwnTakeLands()
    {
        foreach (var clip in Motion.Clips.Where(c => c.StandIn != null))
        {
            Assert.True(Motion.TryClip(clip.StandIn!, out var played), clip.Id + " stands in with an unknown clip");
            Assert.Null(played.StandIn);
            Assert.Equal(played.Id, Motion.PlayedId(clip.Id));
        }
        // The steal race's takes have landed (#966): each plays its own file, both hands where handed, on its own marker.
        Assert.Equal("catcherThrow-L", Motion.ClipFile(Motion.Verb.CatcherThrow, Hand.R, Hand.L));
        Assert.Equal("tag-L", Motion.ClipFile(Motion.Verb.Tag, Hand.R, Hand.L));
        Assert.Equal("slideHeadFirst", Motion.ClipFile(Motion.Verb.SlideHeadFirst, Hand.R, Hand.R));
        Assert.Equal("turnBack", Motion.ClipFile(Motion.Verb.TurnBack, Hand.R, Hand.R));
        Assert.Equal(Motion.CatcherThrowRelease, Motion.Mark(Motion.Verb.CatcherThrow, Motion.ClipEvent.Release));
        Assert.Equal("idle", Motion.PlayedId("idle"));
    }

    /// <summary>The head-first slide is a style of slide, never a faster one: its slot's length and plant are the feet-first slide's.</summary>
    [Fact]
    public void TheHeadFirstSlideKeepsTheFeetFirstSlidesClock()
    {
        Assert.True(Motion.TryClip("slide", out var feetFirst));
        Assert.True(Motion.TryClip("slideHeadFirst", out var headFirst));
        Assert.Equal(feetFirst.Duration, headFirst.Duration);
        Assert.Equal(feetFirst.Mark, headFirst.Mark);
        Assert.Equal(feetFirst.MarkAt, headFirst.MarkAt);
    }
}
