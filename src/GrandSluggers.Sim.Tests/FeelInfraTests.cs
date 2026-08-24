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
        Assert.True(title.Pos.Z > -20, $"title in front of the backstop cage z={title.Pos.Z}");
        Assert.True(title.Pos.Y > 12, $"title too low to see Harbor y={title.Pos.Y}");
        Assert.True(Math.Abs(title.Pos.X) > 6, $"title is a 3/4, not through the pipe x={title.Pos.X}");
        Assert.True(title.Target.Z > 30, $"title looks into the park z={title.Target.Z}");
        Assert.True(title.Fov >= 42);
        var select = _content.Shots.Must("select");
        Assert.True(select.Pos.Z > -20, $"select must sit in front of the backstop cage z={select.Pos.Z}");
        Assert.True(select.Pos.Z < 0, $"select behind home z={select.Pos.Z}");
        Assert.InRange(select.Target.Z, 8, 22);
        Assert.True(select.Fov >= 44, $"select fov {select.Fov} too tight for six captains");
    }

    [Fact]
    public void PlateIsBatterOverShoulderAndMoundIsPitcherThreeQuarter()
    {
        var plate = _content.Shots.Must("plate");
        var mound = _content.Shots.Must("mound");
        // Third-base 3/4 so the loaded bat is not the lens. Look at the dirt
        // around the box (ring + feet), not the brim.
        Assert.True(StillPose.PlateIsThirdBaseThreeQuarter(plate.Pos.X, plate.Pos.Z),
            $"plate third-base 3/4 x={plate.Pos.X} z={plate.Pos.Z}");
        Assert.InRange(plate.Pos.Y, 4.4, 6.8);
        Assert.True(plate.Target.Z > 10, $"plate looks into the diamond, target z={plate.Target.Z}");
        Assert.True(plate.Target.Z < 22, $"plate look too far past the box, Rio crops, z={plate.Target.Z}");
        Assert.True(plate.Target.Y < 1.4, $"plate look is on the dirt/box y={plate.Target.Y}");
        Assert.True(Math.Abs(plate.Target.X - 2.55) < 3, $"plate look is on the batter x={plate.Target.X}");
        var batter = new Vec3(2.55, 0, 2.4);
        var batterDist = Dist(plate.Pos, batter);
        Assert.InRange(batterDist, 12, 22);
        Assert.True(plate.Fov >= 48, $"plate fov {plate.Fov} too tight for box + infield");
        Assert.Equal(StillPose.PlateCamX, plate.Pos.X, 1);
        Assert.Equal(StillPose.PlateCamZ, plate.Pos.Z, 1);
        Assert.True(mound.Pos.Z > Diamond.Mound, $"mound camera behind rubber z={mound.Pos.Z}");
        Assert.True(mound.Target.Z < 8, $"mound looks at the plate/box, target z={mound.Target.Z}");
        Assert.True(mound.Pos.X > 6, $"mound is 3/4 off the pipe x={mound.Pos.X}");
        var moundDist = Dist(mound.Pos, new Vec3(0, 0, Diamond.Mound));
        Assert.True(moundDist > 16, $"mound too close (pitcher blob) dist={moundDist}");
        Assert.True(moundDist < 28, $"mound too far (pitcher ant) dist={moundDist}");
        Assert.InRange(mound.Fov, 38, 46);
    }

    [Fact]
    public void LineAndTagShotsAreDistinctFromFlyAndThrow()
    {
        var fly = _content.Shots.Must("diamond");
        var hop = _content.Shots.Must("diamond-grounder");
        var line = _content.Shots.Must("diamond-line");
        var homer = _content.Shots.Must("diamond-homer");
        var tag = _content.Shots.Must("tag");
        var thr = _content.Shots.Must("throw");
        Assert.True(line.Pos.Y > hop.Pos.Y, $"line height {line.Pos.Y} vs hopper {hop.Pos.Y}");
        Assert.True(line.Pos.Y < fly.Pos.Y, $"line height {line.Pos.Y} vs fly {fly.Pos.Y}");
        Assert.True(homer.Pos.Y > fly.Pos.Y, $"homer height {homer.Pos.Y} vs fly {fly.Pos.Y}");
        Assert.True(fly.Pos.Z > 20, $"fly is a 3/4 in the park, not high-home z={fly.Pos.Z}");
        Assert.True(hop.Pos.Y < 9, $"hopper is a 3/4, not top-down y={hop.Pos.Y}");
        Assert.True(hop.Pos.Y > 4, $"hopper too low y={hop.Pos.Y}");
        Assert.True(hop.Pos.Y - hop.Target.Y < 8, $"hopper look is too steep y {hop.Pos.Y} -> {hop.Target.Y}");
        Assert.NotEqual(line.Fov, fly.Fov);
        Assert.True(tag.Fov < thr.Fov || tag.Pos.Y < thr.Pos.Y,
            $"tag fov/y {tag.Fov}/{tag.Pos.Y} vs throw {thr.Fov}/{thr.Pos.Y}");
        Assert.Equal("bag", tag.Look, ignoreCase: true);
        var smash = _content.Shots.Must("smash");
        Assert.True(smash.Fov >= 40, $"smash fov {smash.Fov} is a nostril");
        Assert.True(smash.Pos.X > 5, $"smash is a 3/4 off the pipe, not through the catcher x={smash.Pos.X}");
        Assert.True(smash.Pos.Z > 4, $"smash looks from the field, not behind home z={smash.Pos.Z}");
        Assert.True(smash.Pos.Y >= 1.4, $"smash cam height {smash.Pos.Y}");
    }

    [Fact]
    public void NamedShotsCoverPlateMoundDiamondThrow()
    {
        foreach (var id in new[] { "plate", "mound", "diamond", "diamond-line", "diamond-homer", "tag", "throw", "replay" })
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
        foreach (var need in new[] { "idle", "run", "jump", "swing", "pitch", "scoop", "slide", "throw" })
            Assert.Contains(need, names);
        Assert.Contains(MoveBones.ClipList, c => c.Id == "swing" && c.Marks.Contains(MoveBones.ClipEvent.Contact));
        Assert.Contains(MoveBones.ClipList, c => c.Id == "pitch" && c.Marks.Contains(MoveBones.ClipEvent.Release));
        Assert.Equal(MoveBones.Verb.Scoop, MoveBones.ClipList.Single(c => c.Id == "scoop").Verb);
        Assert.Equal(MoveBones.Verb.Slide, MoveBones.ClipList.Single(c => c.Id == "slide").Verb);
    }

    [Fact]
    public void BattingSetIsPlatePitchingSetIsMound()
    {
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, false, 0, 0, 0));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, false, 0.2, 0, 0));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, true, 0, 0, 0));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0, 0, 0));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0.2, 0, 0));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0, 0.5, 0));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, true, 0, 0, 0));
        Assert.True(_content.Shots.TryGet(AtBatShots.Plate, out _));
        Assert.True(_content.Shots.TryGet(AtBatShots.Mound, out _));
    }

    [Fact]
    public void SetTellsAreProductStateNotF2()
    {
        Assert.Equal(0.15, SetTells.ChargePull);
        Assert.False(SetTells.RingOn(0));
        Assert.False(SetTells.RingOn(0.14));
        Assert.True(SetTells.RingOn(0.15));
        Assert.True(SetTells.RingOn(1));
        Assert.True(SetTells.RingScale(1) > SetTells.RingScale(0.2));
        Assert.Equal(0, SetTells.RingScale(0));
        Assert.True(SetTells.RingThickFt > 0.1, "plate 3/4 could not see a 0.045 pancake");
        Assert.True(SetTells.RingHeightFt < 0.2);
        Assert.True(SetTells.ZoneOn(true));
        Assert.False(SetTells.ZoneOn(false));
        Assert.True(SetTells.TrailOn(true));
        Assert.False(SetTells.TrailOn(false));
        var mid = SetTells.Locator(0, 0);
        var inRight = SetTells.Locator(0.4, 0);
        Assert.True(inRight.X > mid.X);
        Assert.Equal(PitchFlight.PlateTarget(0.4, -0.2), SetTells.Locator(0.4, -0.2));
        Assert.True(SetTells.InZone(0, 0));
        Assert.True(SetTells.InZone(0.4, 0.2));
        Assert.False(SetTells.InZone(1, 1));
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
        Assert.Equal(FieldAssist.StickTake, feel.FieldAssistStick);
        Assert.InRange(feel.PitcherReadySeconds, 0.4, 0.9);
        Assert.InRange(feel.AfterOutSeconds, 1.0, 1.6);
        Assert.True(feel.InPlayCommitSeconds > 0, $"in-play commit {feel.InPlayCommitSeconds}");
        Assert.True(feel.CpuVsHumanTake > 0 && feel.CpuVsHumanTake < 1);
        Assert.True(feel.CpuVsHumanMiss > 0 && feel.CpuVsHumanMiss < 1);
        Assert.True(feel.CpuVsHumanTake + feel.CpuVsHumanMiss < 1);
        Assert.InRange(feel.ChargeMaxHoldSeconds, 0.25, 0.9);
        Assert.True(feel.ChargeOverchargeDecay > 0);
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
