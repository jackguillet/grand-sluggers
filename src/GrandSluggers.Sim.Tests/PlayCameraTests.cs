using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class PlayCameraTests
{
    [Theory]
    [InlineData(AtBatShots.Mound, -1)]
    [InlineData(AtBatShots.Plate, 1)]
    public void PitchingHorizontalIntentAlwaysProjectsToTheSameScreenSide(string shotId, double expectedWorldSign)
    {
        var content = ContentCatalog.Load();
        var shot = content.Shots.Must(shotId);
        var world = AtBatControl.WorldHorizontal(1, shot);
        Assert.Equal(expectedWorldSign, world);

        var centerRubber = PitchFlight.Release(0);
        var movedRubber = PitchFlight.Release(world);
        AssertProjectsRight(shot, centerRubber, movedRubber);

        var centerCurve = PitchFlight.Point("fastball", 0.82, breakX: 0);
        var movedCurve = PitchFlight.Point("fastball", 0.82, breakX: world);
        AssertProjectsRight(shot, centerCurve, movedCurve);
    }

    [Fact]
    public void SetIsMoundWhen1PPitchesPlateWhenBattingOr1v1()
    {
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.Set, seats: 1));
        Assert.Equal(AtBatShots.Mound, PlayCamera.Shot(PlayCamera.Beat.Set, seats: 1, pitchingSet: true));
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.PitchFlight, seats: 1));
        Assert.Equal(AtBatShots.Mound, PlayCamera.Shot(PlayCamera.Beat.PitchFlight, seats: 1, pitchingSet: true));
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.Set, seats: 2));
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.Set, seats: 2, pitchingSet: true));
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.PitchFlight, seats: 2, pitchingSet: true));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0, 0, 0, seats: 1));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, false, 0, 0, 0, seats: 1));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, true, 0, 0, 0, seats: 1));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, true, 0, 0, 0, seats: 1));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(true, false, 0, 0, 0, seats: 2));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, false, 0, 0, 0, seats: 2));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(true, true, 0, 0, 0, seats: 2));
    }

    static void AssertProjectsRight(CameraShot shot,
        (double X, double Y, double Z) center,
        (double X, double Y, double Z) moved)
    {
        var centerView = PlayCamera.Project(shot, new Vec3(center.X, center.Y, center.Z));
        var movedView = PlayCamera.Project(shot, new Vec3(moved.X, moved.Y, moved.Z));
        Assert.NotNull(centerView);
        Assert.NotNull(movedView);
        Assert.True(movedView.Value.X > centerView.Value.X,
            $"{shot.Id}: mapped right projected {movedView.Value.X} vs center {centerView.Value.X}");
    }

    [Fact]
    public void Training1PFollowsTheRole()
    {
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0, 0, 0, training: true, seats: 1));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, false, 0, 0, 0, training: true, seats: 1));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, true, 0, 0, 0, training: true, seats: 1));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, true, 0, 0, 0, training: true, seats: 1));
    }

    [Fact]
    public void InPlayTheaterDoesNotForkBySeatCount()
    {
        foreach (PlayCamera.Beat beat in Enum.GetValues<PlayCamera.Beat>())
        {
            if (beat is PlayCamera.Beat.Set or PlayCamera.Beat.PitchFlight) continue;
            var one = PlayCamera.Shot(beat, seats: 1);
            var two = PlayCamera.Shot(beat, seats: 2);
            Assert.Equal(one, two);
            Assert.False(string.IsNullOrWhiteSpace(one), beat.ToString());
        }
        // Spec §15: the shot per class, every id in data/feel/shots.json.
        var shots = ContentCatalog.Load().Shots;
        Assert.Equal(PlayCamera.InPlay, PlayCamera.Shot(PlayCamera.Beat.Grounder));
        Assert.Equal(PlayCamera.InPlay, PlayCamera.Shot(PlayCamera.Beat.GrounderPull));
        Assert.Equal(PlayCamera.InPlay, PlayCamera.Shot(PlayCamera.Beat.Rundown));
        Assert.Equal(PlayCamera.InPlayFly, PlayCamera.Shot(PlayCamera.Beat.Line));
        Assert.Equal(PlayCamera.InPlayFly, PlayCamera.Shot(PlayCamera.Beat.Fly));
        Assert.Equal(PlayCamera.InPlayFly, PlayCamera.Shot(PlayCamera.Beat.Homer));
        Assert.Equal(PlayCamera.InPlayFly, PlayCamera.Shot(PlayCamera.Beat.Wall));
        Assert.Equal(PlayCamera.SmashShot, PlayCamera.Shot(PlayCamera.Beat.Smash));
        Assert.Equal(PlayCamera.ThrowShot, PlayCamera.Shot(PlayCamera.Beat.Throw));
        Assert.Equal(PlayCamera.ThrowShot, PlayCamera.Shot(PlayCamera.Beat.StealThrow));
        Assert.Equal(PlayCamera.TagShot, PlayCamera.Shot(PlayCamera.Beat.Tag));
        foreach (PlayCamera.Beat beat in Enum.GetValues<PlayCamera.Beat>())
            Assert.True(shots.TryGet(PlayCamera.Shot(beat, seats: 1, pitchingSet: true), out _), beat.ToString());
        Assert.Equal("bag", shots.Must(PlayCamera.ThrowShot).Look);
        Assert.Equal("bag", shots.Must(PlayCamera.TagShot).Look);
        Assert.Equal("body", shots.Must(PlayCamera.SmashShot).Look);
    }

    static readonly AtBatResult Hopper = new(ContactQuality.Nice, true, false, 90, 8, 40, false, false, null, null,
        SprayDeg: 4, Class: BattedBallClass.Grounder);

    static PlayCamera.LiveView View(double t = 1.0, AtBatResult? hit = null, bool runnerPlay = false,
        bool throwing = false, int throwBag = 0, bool closePlay = false, int closeBag = 0, bool rundown = false,
        int playBag = 0, double smashLeft = 0) =>
        new(t, hit ?? Hopper, runnerPlay, throwing, throwBag, closePlay, closeBag, rundown, playBag, smashLeft,
            new Vec3(40, 3, 90), new Vec3(1, 3.2, 0));

    [Fact]
    public void LiveBeatIsDecidedFromTypedStateInSection15Order()
    {
        var content = ContentCatalog.Load();
        var feel = content.Feel;
        var fly = Hopper with { LaunchDeg = 32, CarryFt = 280, Class = BattedBallClass.Fly };
        var liner = Hopper with { LaunchDeg = 18, ExitVeloMph = 95, CarryFt = 180, Class = BattedBallClass.Liner };
        var homer = Hopper with { LaunchDeg = 32, CarryFt = 420, HomeRun = true, Class = BattedBallClass.Homer };
        var wall = Hopper with { LaunchDeg = 24, CarryFt = 380, Class = BattedBallClass.Wall };

        // The crack holds the SET shot for contactCutSeconds (§8.2), then the class beat.
        Assert.Equal(PlayCamera.Beat.Set, PlayCamera.LiveBeat(View(t: feel.ContactCutSeconds * 0.5), feel));
        Assert.Null(PlayCamera.LiveFraming(content.Shots, View(t: feel.ContactCutSeconds * 0.5), feel));
        Assert.Equal(PlayCamera.Beat.Grounder, PlayCamera.LiveBeat(View(t: feel.ContactCutSeconds), feel));
        Assert.Equal(PlayCamera.Beat.Fly, PlayCamera.LiveBeat(View(hit: fly), feel));
        Assert.Equal(PlayCamera.Beat.Line, PlayCamera.LiveBeat(View(hit: liner), feel));
        Assert.Equal(PlayCamera.Beat.Wall, PlayCamera.LiveBeat(View(hit: wall), feel));
        // A home run smashes at the crack, no hold, then pulls back with the ball.
        Assert.Equal(PlayCamera.Beat.Smash, PlayCamera.LiveBeat(View(t: 0, hit: homer, smashLeft: feel.SmashHold), feel));
        Assert.Equal(PlayCamera.Beat.Homer, PlayCamera.LiveBeat(View(t: 0.1, hit: homer, smashLeft: 0), feel));
        // The bag beats: a throw sits on its bag, a close play on the tag, a runner play on the play's bag.
        Assert.Equal(PlayCamera.Beat.Throw, PlayCamera.LiveBeat(View(throwing: true, throwBag: 1), feel));
        Assert.Equal(PlayCamera.Beat.Tag, PlayCamera.LiveBeat(View(throwing: true, throwBag: 4, closePlay: true, closeBag: 4), feel));
        Assert.Equal(PlayCamera.Beat.Rundown, PlayCamera.LiveBeat(View(rundown: true), feel));
        Assert.Equal(PlayCamera.Beat.Throw, PlayCamera.LiveBeat(View(rundown: true, throwing: true, throwBag: 2), feel));
        Assert.Equal(PlayCamera.Beat.StealThrow, PlayCamera.LiveBeat(View(t: 0, hit: null, runnerPlay: true, playBag: 2) with { Hit = null }, feel));
        Assert.Equal(2, PlayCamera.BeatBag(PlayCamera.Beat.StealThrow, View(runnerPlay: true, playBag: 2)));
        Assert.Equal(3, PlayCamera.BeatBag(PlayCamera.Beat.Tag, View(closePlay: true, closeBag: 3)));
        Assert.Equal(0, PlayCamera.BeatBag(PlayCamera.Beat.Fly, View(hit: fly)));
    }

    [Fact]
    public void LiveFramingTranslatesTheAuthoredShotOntoTheBagTheBallOrTheBody()
    {
        var content = ContentCatalog.Load();
        var feel = content.Feel;
        var shots = content.Shots;

        var toFirst = PlayCamera.LiveFraming(shots, View(throwing: true, throwBag: 1), feel)!.Value;
        var bag = Diamond.Bag(1);
        Assert.Equal(PlayCamera.ThrowShot, toFirst.Shot);
        Assert.Equal(bag.X, toFirst.Look.X, 6);
        Assert.Equal(bag.Z, toFirst.Look.Z, 6);
        Assert.Equal(shots.Must(PlayCamera.ThrowShot).Target.Y, toFirst.Look.Y, 6);
        Assert.Equal(shots.Must(PlayCamera.ThrowShot).Pos.Y, toFirst.Pos.Y, 6);
        Assert.Equal(shots.Must(PlayCamera.ThrowShot).Fov, toFirst.Fov);

        var home = PlayCamera.LiveFraming(shots, View(closePlay: true, closeBag: 4), feel)!.Value;
        Assert.Equal(PlayCamera.TagShot, home.Shot);
        Assert.Equal(0, home.Look.X, 6);
        Assert.Equal(0, home.Look.Z, 6);

        var steal = PlayCamera.LiveFraming(shots, View(runnerPlay: true, playBag: 2) with { Hit = null }, feel)!.Value;
        Assert.Equal(PlayCamera.ThrowShot, steal.Shot);
        Assert.Equal(Diamond.Bag(2).Z, steal.Look.Z, 6);

        var hop = PlayCamera.LiveFraming(shots, View(), feel)!.Value;
        Assert.Equal(PlayCamera.InPlay, hop.Shot);
        Assert.Equal(0, hop.Look.Y);
        Assert.Equal(40, hop.Look.X, 6);
        Assert.Equal(90, hop.Look.Z, 6);

        var homer = Hopper with { HomeRun = true, Class = BattedBallClass.Homer };
        var smash = PlayCamera.LiveFraming(shots, View(t: 0, hit: homer, smashLeft: 0.1), feel)!.Value;
        Assert.Equal(PlayCamera.SmashShot, smash.Shot);
        var s = shots.Must(PlayCamera.SmashShot);
        Assert.Equal(1 + s.Pos.X, smash.Pos.X, 6);
        Assert.Equal(3.2 + s.Target.Y, smash.Look.Y, 6);

        // One pad and two see the same live frame: nothing in the live table reads the seat count.
        Assert.Equal(PlayCamera.Shot(PlayCamera.Beat.Throw, seats: 1), PlayCamera.Shot(PlayCamera.Beat.Throw, seats: 2));
    }

    [Fact]
    public void FromHitMatchesTheaterShot()
    {
        // The class is typed on the hit since P2 (§6.2); the camera reads it, never the launch again.
        var hopper = Hopper;
        var pull = hopper with { SprayDeg = -20 };
        var fly = hopper with { LaunchDeg = 32, CarryFt = 280, Class = BattedBallClass.Fly };
        var pop = hopper with { LaunchDeg = 60, CarryFt = 90, Class = BattedBallClass.Pop };
        var homer = hopper with { LaunchDeg = 32, CarryFt = 420, HomeRun = true, Class = BattedBallClass.Homer };
        var line = hopper with { LaunchDeg = 18, ExitVeloMph = 95, CarryFt = 180, Class = BattedBallClass.Liner };
        var star = hopper with { LaunchDeg = 28, StarSwingUsed = "heat-swing", Class = BattedBallClass.Chopper };
        Assert.Equal(InPlay.TheaterShot(hopper), PlayCamera.FromHit(hopper));
        Assert.Equal(PlayCamera.InPlay, PlayCamera.FromHit(hopper));
        Assert.Equal(PlayCamera.InPlay, PlayCamera.FromHit(pull));
        Assert.Equal(PlayCamera.InPlayFly, PlayCamera.FromHit(line));
        Assert.Equal(PlayCamera.InPlayFly, PlayCamera.FromHit(fly));
        Assert.Equal(PlayCamera.InPlayFly, PlayCamera.FromHit(pop));
        Assert.Equal(PlayCamera.InPlayFly, PlayCamera.FromHit(homer));
        Assert.Equal(PlayCamera.InPlay, PlayCamera.FromHit(star));
        Assert.Equal(PlayCamera.Beat.Homer, PlayCamera.BeatFrom(homer));
        Assert.Equal(PlayCamera.Beat.Fly, PlayCamera.BeatFrom(fly));
        Assert.Equal(PlayCamera.Beat.GrounderPull, PlayCamera.BeatFrom(pull));
        Assert.Equal(PlayCamera.Beat.Line, PlayCamera.BeatFrom(line));
    }

    [Fact]
    public void FollowGroundLooksAtDirtNotTheAirborneBall()
    {
        var shot = new CameraShot(PlayCamera.InPlay, "ball", new Vec3(0, 54, -54), new Vec3(0, 0, 0), 50, 8);
        var air = new Vec3(40, 22, 90);
        var framed = PlayCamera.FollowGround(shot, air);
        Assert.Equal(PlayCamera.GroundUnder(air.X, air.Y, air.Z), framed.Look);
        Assert.Equal(0, framed.Look.Y);
        Assert.Equal(54, framed.Pos.Y);
        Assert.InRange(PlayCamera.LookDownDeg(shot), 44, 46);
        Assert.True(framed.Pos.Z < framed.Look.Z);
        Assert.Equal(air.X, framed.Look.X);
        Assert.Equal(air.X, framed.Pos.X);
        Assert.Equal(air.Z, framed.Look.Z);
    }

    [Fact]
    public void HudLayoutIsTheSameForOnePadAndTwo()
    {
        var one = BroadcastHud.Layout(1);
        var two = BroadcastHud.Layout(2);
        Assert.Equal(one, two);
        Assert.Equal(BroadcastHud.Standard, one);
        Assert.True(one.Score.X > 0.5, "score top-right");
        Assert.True(one.Score.Y < 0.2, "score top");
        Assert.True(one.BatterCard.X < 0.2, "batter bottom-left");
        Assert.True(one.BatterCard.Y > 0.7, "batter bottom");
        Assert.True(one.PitcherCard.X > 0.5, "pitcher bottom-right");
        Assert.True(one.PitcherCard.Y > 0.7, "pitcher bottom");
        Assert.True(one.MiniDiamond.X > 0.5, "diamond stays with the score cluster");
        Assert.True(one.Count.X > 0.5, "S/B/O stays with the score cluster");
        Assert.True(one.Banner.X > 0.2 && one.Banner.X + one.Banner.W < 0.8, "banner does not steal card corners");
        Assert.False(Overlaps(one.BatterCard, one.PitcherCard));
        Assert.False(Overlaps(one.Score, one.BatterCard));
        Assert.False(Overlaps(one.Score, one.PitcherCard));
        var px1 = one.BatterCard.Pixel(1920, 1080);
        var px2 = two.BatterCard.Pixel(1920, 1080);
        Assert.Equal(px1, px2);
    }

    static bool Overlaps(BroadcastHud.HudRect a, BroadcastHud.HudRect b) =>
        a.X < b.X + b.W && a.X + a.W > b.X && a.Y < b.Y + b.H && a.Y + a.H > b.Y;
}
