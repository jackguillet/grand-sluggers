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
        var homeToy = new Vec3(0, 0, CarnivalFront.FeaturedTitleZ);
        Assert.True(Dist(title.Pos, homeToy) < Dist(title.Pos, new Vec3(0, 0, Diamond.Mound)),
            $"title home captain is the toy, not the mound dist={Dist(title.Pos, homeToy)}");
        Assert.True(CarnivalFront.FeaturedTitleZ < 16, $"title captain too far z={CarnivalFront.FeaturedTitleZ}");
        Assert.True(CarnivalFront.LogoZ > -12 && CarnivalFront.LogoZ < 18, $"logo z={CarnivalFront.LogoZ}");
        Assert.True(Math.Abs(CarnivalFront.LogoX) < 8, $"logo off-frame x={CarnivalFront.LogoX}");
        Assert.True(CarnivalFront.LogoY > 5, $"logo y={CarnivalFront.LogoY}");
        var select = _content.Shots.Must("select");
        Assert.True(select.Pos.Z > -20, $"select must sit in front of the backstop cage z={select.Pos.Z}");
        Assert.True(select.Pos.Z < 0, $"select behind home z={select.Pos.Z}");
        Assert.InRange(select.Target.Z, 8, 22);
        Assert.True(select.Fov >= 44, $"select fov {select.Fov} too tight for six captains");
        var pickLook = new Vec3(0, CarnivalFront.SelectLookY, CarnivalFront.FeaturedSelectZ);
        Assert.True(CarnivalFront.SelectLookY < 3.2, $"select look is the brim y={CarnivalFront.SelectLookY}");
        Assert.True(CarnivalFront.FeaturedSelectZ >= 6.5, $"select pick too close z={CarnivalFront.FeaturedSelectZ}");
        Assert.True(CarnivalFront.SelectCamIsTheToy(select.Pos.Y, select.Pos.Z),
            $"select looks at the berm y={select.Pos.Y} z={select.Pos.Z}");
        Assert.InRange(select.Pos.Y, CarnivalFront.SelectCamMinY, CarnivalFront.SelectCamMaxY);
        var down = (select.Pos.Y - CarnivalFront.SelectLookY) / (CarnivalFront.FeaturedSelectZ - select.Pos.Z);
        Assert.True(down < CarnivalFront.SelectMaxDown, $"select looks down at the berm slope={down}");
        var dist = Dist(select.Pos, pickLook);
        Assert.True(dist > 14, $"select look too close (hat) dist={dist}");
        Assert.True(dist < 22, $"select too far (dirt is the picture) dist={dist}");
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
        Assert.True(StillPose.PlateCatcherClearsTheLens(
            plate.Pos.X, plate.Pos.Z, plate.Target.X, plate.Target.Z),
            $"plate catcher in the look cone x={plate.Pos.X} z={plate.Pos.Z}");
        Assert.InRange(plate.Pos.Y, 4.4, 6.8);
        Assert.True(plate.Target.Z > 10, $"plate looks into the diamond, target z={plate.Target.Z}");
        Assert.True(plate.Target.Z < 22, $"plate look too far past the box, Rio crops, z={plate.Target.Z}");
        Assert.True(plate.Target.Y < 1.4, $"plate look is on the dirt/box y={plate.Target.Y}");
        Assert.True(Math.Abs(plate.Target.X - 2.55) < 3, $"plate look is on the batter x={plate.Target.X}");
        var batter = new Vec3(2.55, 0, 2.4);
        var batterDist = Dist(plate.Pos, batter);
        Assert.InRange(batterDist, 12, 22);
        var batterDeg = LookDeg(plate.Pos, plate.Target, batter);
        Assert.True(batterDeg < 28, $"batter off the plate look {batterDeg:0.0} deg");
        Assert.True(plate.Fov >= 48, $"plate fov {plate.Fov} too tight for box + infield");
        Assert.Equal(StillPose.PlateCamX, plate.Pos.X, 1);
        Assert.Equal(StillPose.PlateCamZ, plate.Pos.Z, 1);
        // 3/4 behind the rubber looking at home. Rubber in the bottom; the
        // box is the look. Portrait/dirt and CF are both fails (#304).
        Assert.True(mound.Pos.Z > Diamond.Mound, $"mound camera behind the rubber z={mound.Pos.Z}");
        Assert.True(Math.Abs(mound.Pos.X) > 6, $"mound is a 3/4 off the pipe x={mound.Pos.X}");
        Assert.InRange(mound.Pos.Y, 6.0, 8.5);
        Assert.True(mound.Target.Z < 8, $"mound looks at the box, not CF z={mound.Target.Z}");
        Assert.True(mound.Target.Z > -2, $"mound look past the cage z={mound.Target.Z}");
        Assert.True(mound.Target.Y < 2.4, $"mound look too high y={mound.Target.Y}");
        var moundDist = Dist(mound.Pos, new Vec3(0, 0, Diamond.Mound));
        Assert.True(moundDist > 14, $"mound too close (pitcher blob) dist={moundDist}");
        Assert.True(moundDist < 28, $"mound too far (pitcher ant) dist={moundDist}");
        Assert.InRange(mound.Fov, 40, 48);
        var boxDeg = LookDeg(mound.Pos, mound.Target, new Vec3(0, 1.0, 2.4));
        Assert.True(boxDeg < 8, $"box off the mound look {boxDeg:0.0} deg");
        var pitcherDeg = LookDeg(mound.Pos, mound.Target, new Vec3(0, 2.2, Diamond.Mound));
        Assert.True(pitcherDeg < 24, $"pitcher off the mound look {pitcherDeg:0.0} deg");
        Assert.Equal(StillPose.MoundCamX, mound.Pos.X, 1);
        Assert.Equal(StillPose.MoundCamZ, mound.Pos.Z, 1);
        var rubberVp = PlayCamera.Project(mound, new Vec3(0, 0.2, Diamond.Mound));
        var boxVp = PlayCamera.Project(mound, new Vec3(0, 1.0, 2.4));
        var batterVp = PlayCamera.Project(mound, new Vec3(2.55, 3.2, 2.4));
        var catcherVp = PlayCamera.Project(mound, new Vec3(0, 1.6, -4));
        Assert.True(PlayCamera.InFrame(rubberVp, 0.02), $"rubber off mound frame {rubberVp}");
        Assert.True(PlayCamera.InFrame(boxVp), $"box off mound frame {boxVp}");
        Assert.True(PlayCamera.InFrame(batterVp), $"batter off mound frame {batterVp}");
        Assert.True(PlayCamera.InFrame(catcherVp), $"catcher off mound frame {catcherVp}");
        Assert.InRange(rubberVp!.Value.Y, 0.04, 0.28);
        Assert.True(boxVp!.Value.Y > rubberVp.Value.Y, $"box should sit above the rubber vy={boxVp.Value.Y} vs {rubberVp.Value.Y}");
    }

    [Fact]
    public void LineAndTagShotsAreDistinctFromFlyAndThrow()
    {
        var fly = _content.Shots.Must("diamond");
        var hop = _content.Shots.Must("diamond-grounder");
        var line = _content.Shots.Must("diamond-line");
        var homer = _content.Shots.Must("diamond-homer");
        var wall = _content.Shots.Must("wall");
        var tag = _content.Shots.Must("tag");
        var thr = _content.Shots.Must("throw");
        Assert.True(line.Pos.Y > hop.Pos.Y, $"line height {line.Pos.Y} vs hopper {hop.Pos.Y}");
        Assert.True(line.Pos.Y < fly.Pos.Y, $"line height {line.Pos.Y} vs fly {fly.Pos.Y}");
        Assert.True(homer.Pos.Y > fly.Pos.Y, $"homer height {homer.Pos.Y} vs fly {fly.Pos.Y}");
        Assert.Equal("glove", wall.Look, ignoreCase: true);
        Assert.True(wall.Pos.Y < homer.Pos.Y, $"wall height {wall.Pos.Y} vs homer {homer.Pos.Y}");
        Assert.True(Math.Abs(wall.Target.X) < 4 && Math.Abs(wall.Target.Z) < 4, "wall is a follow-cam on the glove");
        Assert.Equal(PlayCamera.Wall, wall.Id);
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
        Assert.True(smash.Pos.X > smash.Pos.Z,
            $"smash is side-on, not from behind x={smash.Pos.X} z={smash.Pos.Z}");
        Assert.True(smash.Target.Y >= 0.4, $"smash looks at the torso y={smash.Target.Y}");
        Assert.True(Math.Abs(smash.Target.X) < 0.8, $"smash looks at the body x={smash.Target.X}");
        Assert.True(smash.Target.Z < 1.5, $"smash looks at the body z={smash.Target.Z}");
        var field = _content.Shots.Must("field");
        Assert.True(field.Pos.Z > 20, $"field postcard is in the park z={field.Pos.Z}");
        Assert.True(field.Pos.Y > 12, $"field too low to see the wall y={field.Pos.Y}");
        Assert.True(field.Target.Z > 250, $"field looks at the wall/town z={field.Target.Z}");
        Assert.True(field.Fov >= 42);
        var lineup = _content.Shots.Must("lineup");
        Assert.True(ChemistryToy.CameraIsThreeQuarter(lineup.Pos.X, lineup.Pos.Y, lineup.Pos.Z),
            $"lineup bird's-eye x={lineup.Pos.X} y={lineup.Pos.Y} z={lineup.Pos.Z}");
        Assert.Equal(ChemistryToy.CamX, lineup.Pos.X, 1);
        Assert.Equal(ChemistryToy.CamZ, lineup.Pos.Z, 1);
        Assert.Equal(ChemistryToy.LookZ, lineup.Target.Z, 1);
        Assert.Equal(ChemistryToy.Fov, lineup.Fov, 1);
        var catcher = ChemistryToy.WorldSpot("C");
        var cf = ChemistryToy.WorldSpot("CF");
        var catcherDeg = LookDeg(lineup.Pos, lineup.Target, new Vec3(catcher.X, 1.6, catcher.Z));
        var cfDeg = LookDeg(lineup.Pos, lineup.Target, new Vec3(cf.X, 1.6, cf.Z));
        Assert.True(catcherDeg < 28, $"lineup catcher off look {catcherDeg:0.0} deg");
        Assert.True(cfDeg < 28, $"lineup CF off look {cfDeg:0.0} deg");
    }

    [Fact]
    public void NamedShotsCoverPlateMoundDiamondThrow()
    {
        foreach (var id in new[] { "plate", "pitch", "mound", "diamond", "diamond-line", "diamond-homer", "wall", "tag", "throw", "replay" })
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
    public void BattingSetIsPlateThrowAtYouIsPitchPitchingSetIsMound()
    {
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, false, 0, 0, 0));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, false, 0.2, 0, 0));
        Assert.Equal(AtBatShots.Pitch, AtBatShots.SetShot(false, true, 0, 0, 0));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0, 0, 0));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0.2, 0, 0));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0, 0.5, 0));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0, 0, 0, training: true));
        Assert.Equal(AtBatShots.Pitch, AtBatShots.SetShot(true, true, 0, 0, 0));
        Assert.True(_content.Shots.TryGet(AtBatShots.Plate, out _));
        Assert.True(_content.Shots.TryGet(AtBatShots.Pitch, out var pitch));
        Assert.True(_content.Shots.TryGet(AtBatShots.Mound, out var mound));
        Assert.True(mound.Target.Z < 8, $"mound looks at the box, not the rubber dirt z={mound.Target.Z}");
        Assert.True(LookDeg(mound.Pos, mound.Target, new Vec3(0, 2.2, Diamond.Mound)) < 24,
            $"pitching SET must keep Rio in the look cone");
        Assert.True(StillPose.PitchLooksAtTheThrow(pitch.Pos.X, pitch.Pos.Z, pitch.Target.Y, pitch.Target.Z),
            $"pitch looks at dirt/cage x={pitch.Pos.X} z={pitch.Pos.Z} look={pitch.Target.Y},{pitch.Target.Z}");
        Assert.True(StillPose.PlateCatcherClearsTheLens(pitch.Pos.X, pitch.Pos.Z, pitch.Target.X, pitch.Target.Z),
            $"pitch catcher in the look cone x={pitch.Pos.X} z={pitch.Pos.Z}");
        Assert.InRange(pitch.Fov, 28, 40);
        Assert.Equal(StillPose.PitchCamX, pitch.Pos.X, 1);
        Assert.Equal(StillPose.PitchCamZ, pitch.Pos.Z, 1);
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
        Assert.True(SetTells.RingScale(SetTells.ChargePull) > 3,
            $"pull ring under the feet r={SetTells.RingScale(SetTells.ChargePull)}");
        Assert.True(SetTells.RingScale(1) >= 5, $"max ring must clear the toy r={SetTells.RingScale(1)}");
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
    public void ChargeRingWorldYIsDirtNotChest()
    {
        const double chestY = 2.28;
        const double lift = 1.2;
        var box = SetTells.RingAt(2.55, 2.4, chestY, lift);
        Assert.Equal(2.55, box.X);
        Assert.Equal(2.4, box.Z);
        Assert.InRange(box.Y, 0.2, 0.8);
        Assert.True(box.Y < 1.0, $"box ring must sit on packed dirt, not chest y={box.Y}");
        Assert.Equal(SetTells.RingWorldY(2.4), SetTells.RingWorldY(2.4, chestY, lift));
        var rubber = SetTells.RingAt(0, Diamond.Mound, chestY, lift);
        Assert.InRange(rubber.Y, 0.8, 1.4);
        Assert.True(rubber.Y < chestY, $"rubber ring is in the torso y={rubber.Y}");
        Assert.Equal(SetTells.RingAt(0, Diamond.Mound).Y, rubber.Y);
    }

    [Fact]
    public void BaseballDiameterReadsOnPlateWithoutEatingMound()
    {
        Assert.InRange(Baseball.DiameterFt, 0.45, 0.85);
        Assert.True(Baseball.DiameterFt < 1.0, "1.5 ft was a beach ball on mound");
        Assert.True(Baseball.InPlayDiameterFt < 1.0, "in-play ball is a glove, not a torso");
        Assert.True(Baseball.FlightDiameterFt < 1.6, "2ft was a torso on a ~6ft toy");
        Assert.True(Baseball.FlightDiameterFt > Baseball.DiameterFt);
        Assert.Equal(Baseball.DiameterFt, Baseball.ApparentScale(false, 60));
        Assert.Equal(Baseball.InPlayDiameterFt, Baseball.ApparentScale(true, 45, inPlay: true));
        Assert.Equal(Baseball.InPlayDiameterFt, Baseball.ApparentScale(true, 280, inPlay: true));
        Assert.True(Baseball.ApparentScale(true, 58) > Baseball.DiameterFt, "pitch-toward-plate may grow");
        Assert.True(Baseball.ApparentScale(true, 2) <= Baseball.DiameterFt + 0.02);
        Assert.True(Baseball.ApparentScale(true, 45) > Baseball.ApparentScale(true, 45, inPlay: true),
            "infield hopper must not inherit pitch far-scale");
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

    static double LookDeg(Vec3 pos, Vec3 target, Vec3 p)
    {
        var lx = target.X - pos.X;
        var ly = target.Y - pos.Y;
        var lz = target.Z - pos.Z;
        var dx = p.X - pos.X;
        var dy = p.Y - pos.Y;
        var dz = p.Z - pos.Z;
        var ln = Math.Sqrt(lx * lx + ly * ly + lz * lz);
        var dn = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (ln < 1e-6 || dn < 1e-6) return 180;
        var dot = Math.Clamp((lx * dx + ly * dy + lz * dz) / (ln * dn), -1, 1);
        return Math.Acos(dot) * 180 / Math.PI;
    }
}
