using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class FeelInfraTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void PlateShotIsDistinctFromMound()
    {
        var plate = _content.Shots.Must("plate");
        var mound = _content.Shots.Must("mound");
        Assert.Equal("plate", plate.Look, ignoreCase: true);
        Assert.Equal("mound", mound.Look, ignoreCase: true);
        Assert.NotEqual(plate.Look, mound.Look, StringComparer.OrdinalIgnoreCase);
        Assert.NotEqual(plate.Fov, mound.Fov);
        Assert.False(Near(plate.Pos, mound.Pos),
            $"plate pos {plate.Pos} vs mound {mound.Pos}");
        Assert.False(Near(plate.Target, mound.Target),
            $"plate look {plate.Target} vs mound {mound.Target}");
        Assert.True(plate.Fov > 0 && mound.Fov > 0);
    }

    [Fact]
    public void NamedShotsCoverPlateMoundDiamondThrow()
    {
        foreach (var id in new[] { "plate", "mound", "diamond", "throw", "replay" })
        {
            var shot = _content.Shots.Must(id);
            Assert.Equal(id, shot.Id, ignoreCase: true);
            Assert.False(string.IsNullOrWhiteSpace(shot.Look));
            Assert.True(shot.Fov > 0, $"{id} fov");
        }
    }

    [Fact]
    public void SharedClipListHasIdleRunSwingPitchScoopSlide()
    {
        var names = MoveBones.Clips.Select(c => c.ToLowerInvariant()).ToHashSet();
        foreach (var need in new[] { "idle", "run", "jump", "swing", "pitch", "scoop", "slide" })
            Assert.Contains(need, names);
        Assert.Contains(MoveBones.ClipList, c => c.Id == "swing" && c.Marks.Contains(MoveBones.ClipEvent.Contact));
        Assert.Contains(MoveBones.ClipList, c => c.Id == "pitch" && c.Marks.Contains(MoveBones.ClipEvent.Release));
        Assert.Equal(MoveBones.Verb.Scoop, MoveBones.ClipList.Single(c => c.Id == "scoop").Verb);
        Assert.Equal(MoveBones.Verb.Slide, MoveBones.ClipList.Single(c => c.Id == "slide").Verb);
    }

    [Fact]
    public void FeelTableChargeAndSmashArePositive()
    {
        var feel = _content.Feel;
        Assert.True(feel.PitchChargeSeconds > 0, $"pitch charge {feel.PitchChargeSeconds}");
        Assert.True(feel.SwingChargeSeconds > 0, $"swing charge {feel.SwingChargeSeconds}");
        Assert.True(feel.SmashFreeze > 0, $"smash freeze {feel.SmashFreeze}");
        Assert.True(feel.SmashHold > 0, $"smash hold {feel.SmashHold}");
        Assert.True(feel.ThrowEase > 0, $"throw ease {feel.ThrowEase}");
        Assert.True(feel.CameraBlend > 0, $"camera blend {feel.CameraBlend}");
    }

    [Fact]
    public void ScoopDropsThenPicksAndSlideTucksThenPops()
    {
        var drop = MoveBones.Evaluate(MoveBones.Verb.Scoop, 0, 0.06);
        var pick = MoveBones.Evaluate(MoveBones.Verb.Scoop, 0, 0.22);
        Assert.True(pick.Torso.X > drop.Torso.X || pick.RUpper.X > drop.RUpper.X,
            $"scoop pick {pick.Torso.X}/{pick.RUpper.X} vs drop {drop.Torso.X}/{drop.RUpper.X}");
        var tuck = MoveBones.Evaluate(MoveBones.Verb.Slide, 0, 0.10);
        var pop = MoveBones.Evaluate(MoveBones.Verb.Slide, 0, 0.36);
        Assert.True(tuck.Torso.X > 20, $"slide tuck {tuck.Torso.X}");
        Assert.True(pop.Torso.X < tuck.Torso.X, $"slide pop {pop.Torso.X} vs tuck {tuck.Torso.X}");
    }

    static bool Near(Vec3 a, Vec3 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz < 0.25;
    }
}
