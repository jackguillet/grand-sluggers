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
        var title = _content.Shots.Must("title");
        Assert.True(title.Pos.Z < 0, $"title behind home z={title.Pos.Z}");
        Assert.True(title.Pos.Y > 12, $"title too low to see Harbor y={title.Pos.Y}");
        Assert.True(title.Target.Z > 30, $"title looks into the park z={title.Target.Z}");
        Assert.True(title.Fov >= 42);
        var select = _content.Shots.Must("select");
        Assert.True(select.Pos.Z > -20, $"select must sit in front of the backstop cage z={select.Pos.Z}");
        Assert.True(select.Pos.Z < 0, $"select behind home z={select.Pos.Z}");
        Assert.InRange(select.Target.Z, 8, 22);
        Assert.True(select.Fov >= 44, $"select fov {select.Fov} too tight for six captains");
    }

    [Fact]
    public void PlateIsCatcherEyeAndMoundIsThreeQuarter()
    {
        var plate = _content.Shots.Must("plate");
        var mound = _content.Shots.Must("mound");
        Assert.True(plate.Pos.Z < 0, $"plate must sit behind home, z={plate.Pos.Z}");
        Assert.True(plate.Pos.Z > -21.5, $"plate in front of the backstop cage, z={plate.Pos.Z}");
        Assert.InRange(plate.Pos.Y, 3.2, 6.2);
        Assert.True(plate.Target.Z > 40, $"plate looks at the mound, target z={plate.Target.Z}");
        var plateDist = Dist(plate.Pos, new Vec3(0, 0, 0));
        Assert.True(plateDist > 20, $"plate too close for toy heads (cap shot) dist={plateDist}");
        Assert.True(plate.Fov >= 46, $"plate fov {plate.Fov} too tight for a full batter");
        Assert.True(mound.Pos.Z > Diamond.Mound, $"mound camera behind rubber z={mound.Pos.Z}");
        Assert.True(mound.Target.Z < 12, $"mound looks at the plate, target z={mound.Target.Z}");
        Assert.True(mound.Pos.Y > plate.Pos.Y, "mound eye is above catcher eye");
        Assert.True(mound.Pos.X > 8, $"mound is 3/4 off the pipe x={mound.Pos.X}");
        var moundDist = Dist(mound.Pos, new Vec3(0, 0, Diamond.Mound));
        Assert.True(moundDist > 18, $"mound too close (pitcher blob) dist={moundDist}");
        Assert.InRange(mound.Fov, 38, 44);
    }

    [Fact]
    public void LineAndTagShotsAreDistinctFromFlyAndThrow()
    {
        var fly = _content.Shots.Must("diamond");
        var hop = _content.Shots.Must("diamond-grounder");
        var line = _content.Shots.Must("diamond-line");
        var tag = _content.Shots.Must("tag");
        var thr = _content.Shots.Must("throw");
        Assert.True(line.Pos.Y > hop.Pos.Y, $"line height {line.Pos.Y} vs hopper {hop.Pos.Y}");
        Assert.True(line.Pos.Y < fly.Pos.Y, $"line height {line.Pos.Y} vs fly {fly.Pos.Y}");
        Assert.NotEqual(line.Fov, fly.Fov);
        Assert.True(tag.Fov < thr.Fov || tag.Pos.Y < thr.Pos.Y,
            $"tag fov/y {tag.Fov}/{tag.Pos.Y} vs throw {thr.Fov}/{thr.Pos.Y}");
        Assert.Equal("bag", tag.Look, ignoreCase: true);
    }

    [Fact]
    public void NamedShotsCoverPlateMoundDiamondThrow()
    {
        foreach (var id in new[] { "plate", "mound", "diamond", "diamond-line", "tag", "throw", "replay" })
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
    public void PitchingSetStartsOnPlateUntilTheRubber()
    {
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, false, 0, 0, 0));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(true, false, 0, 0, 0));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0.2, 0, 0));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0, 0.5, 0));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0, 0, -0.4));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, true, 0, 0, 0));
        Assert.True(_content.Shots.TryGet(AtBatShots.Plate, out _));
        Assert.True(_content.Shots.TryGet(AtBatShots.Mound, out _));
    }

    [Fact]
    public void BaseballDiameterReadsOnPlateWithoutEatingMound()
    {
        Assert.InRange(Baseball.DiameterFt, 0.45, 0.85);
        Assert.True(Baseball.DiameterFt < 1.0, "1.5 ft was a beach ball on mound");
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

    static double Dist(Vec3 a, Vec3 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
