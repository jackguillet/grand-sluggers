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
        bool closePlay = false, int closeBag = 0, bool rundown = false,
        int playBag = 0, double smashLeft = 0) =>
        new(t, hit ?? Hopper, runnerPlay, closePlay, closeBag, rundown, playBag, smashLeft,
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
        // The bag beats (D14): only a close play sits on the tag, and a runner play on the play's bag.
        Assert.Equal(PlayCamera.Beat.Tag, PlayCamera.LiveBeat(View(closePlay: true, closeBag: 4), feel));
        Assert.Equal(PlayCamera.Beat.Rundown, PlayCamera.LiveBeat(View(rundown: true), feel));
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

        var steal2 = PlayCamera.LiveFraming(shots, View(runnerPlay: true, playBag: 1) with { Hit = null }, feel)!.Value;
        var bag = Diamond.Bag(1);
        Assert.Equal(PlayCamera.ThrowShot, steal2.Shot);
        Assert.Equal(bag.X, steal2.Look.X, 6);
        Assert.Equal(bag.Z, steal2.Look.Z, 6);
        Assert.Equal(shots.Must(PlayCamera.ThrowShot).Target.Y, steal2.Look.Y, 6);
        Assert.Equal(shots.Must(PlayCamera.ThrowShot).Pos.Y, steal2.Pos.Y, 6);
        Assert.Equal(shots.Must(PlayCamera.ThrowShot).Fov, steal2.Fov);

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
        Assert.Equal(PlayCamera.Shot(PlayCamera.Beat.Tag, seats: 1), PlayCamera.Shot(PlayCamera.Beat.Tag, seats: 2));
    }

    // ---------------------------------------------------------------------------------
    // D14 (#610): one cut on contact, the follow stays on the ball, the bag cam only on a close play
    // ---------------------------------------------------------------------------------

    [Fact]
    public void EachLiveFrameCarriesItsShotsAuthoredBlend_TheInPlayViewsAndTheBagCamsAreCuts()
    {
        var content = ContentCatalog.Load();
        var feel = content.Feel;
        var shots = content.Shots;
        var fly = Hopper with { LaunchDeg = 32, CarryFt = 280, Class = BattedBallClass.Fly };
        var homer = Hopper with { HomeRun = true, Class = BattedBallClass.Homer };
        var frames = new[]
        {
            PlayCamera.LiveFraming(shots, View(), feel)!.Value,
            PlayCamera.LiveFraming(shots, View(hit: fly), feel)!.Value,
            PlayCamera.LiveFraming(shots, View(closePlay: true, closeBag: 4), feel)!.Value,
            PlayCamera.LiveFraming(shots, View(runnerPlay: true, playBag: 2) with { Hit = null }, feel)!.Value,
            PlayCamera.LiveFraming(shots, View(t: 0, hit: homer, smashLeft: 0.1), feel)!.Value,
        };
        foreach (var f in frames)
        {
            Assert.Equal(shots.Must(f.Shot).Blend, f.Blend);
            Assert.Equal(f.Blend <= 0, f.Cut);
        }
        // The contact transition is a cut to the in-play view, and so are the close play's bag cam and the steal's.
        foreach (var id in new[] { PlayCamera.InPlay, PlayCamera.InPlayFly, PlayCamera.TagShot, PlayCamera.ThrowShot })
            Assert.True(shots.Must(id).Blend == 0, $"{id} blend {shots.Must(id).Blend} is a swoop, not a cut");
        // The authored blend is read, not a constant: a hand-built shot's blend rides the frame.
        var custom = new CameraShot("custom", "ball", new Vec3(0, 10, -10), new Vec3(0, 0, 0), 50, 7);
        Assert.Equal(7, PlayCamera.FollowGround(custom, new Vec3(1, 2, 3)).Blend);
        Assert.False(PlayCamera.FollowGround(custom, new Vec3(1, 2, 3)).Cut);
    }

    [Fact]
    public void TheHoldKeepsATargetForCameraHoldSecondsAndABagCamOnItsFirstBag()
    {
        var feel = ContentCatalog.Load().Feel;
        var hold = feel.CameraHoldSeconds;
        Assert.Equal(0.25, hold, 6);
        var h = new PlayCamera.CameraHold();
        Assert.Equal((PlayCamera.Beat.Set, 0), h.Step(PlayCamera.Beat.Set, 0, 0, hold));
        Assert.Equal((PlayCamera.Beat.Grounder, 0), h.Step(PlayCamera.Beat.Grounder, 0, 0.42, hold));
        // A rundown that flickers on inside the hold does not re-aim; one that stays takes the camera.
        Assert.Equal((PlayCamera.Beat.Grounder, 0), h.Step(PlayCamera.Beat.Rundown, 0, 0.42 + hold * 0.5, hold));
        Assert.Equal((PlayCamera.Beat.Grounder, 0), h.Step(PlayCamera.Beat.Grounder, 0, 0.42 + hold * 0.9, hold));
        Assert.Equal((PlayCamera.Beat.Rundown, 0), h.Step(PlayCamera.Beat.Rundown, 0, 0.42 + hold, hold));
        // A close play at home opens the bag cam after the hold, stays on home, and releases after the verdict.
        Assert.Equal((PlayCamera.Beat.Tag, 4), h.Step(PlayCamera.Beat.Tag, 4, 1.0, hold));
        Assert.Equal((PlayCamera.Beat.Tag, 4), h.Step(PlayCamera.Beat.Grounder, 0, 1.1, hold));
        Assert.Equal((PlayCamera.Beat.Grounder, 0), h.Step(PlayCamera.Beat.Grounder, 0, 1.3, hold));
        // A steal's bag cam keeps the bag it opened on for the whole beat.
        var s = new PlayCamera.CameraHold();
        Assert.Equal((PlayCamera.Beat.StealThrow, 2), s.Step(PlayCamera.Beat.StealThrow, 2, 0, hold));
        Assert.Equal((PlayCamera.Beat.StealThrow, 2), s.Step(PlayCamera.Beat.StealThrow, 3, 1.5, hold));
        // A new play (the clock starts over) takes its first target at once.
        Assert.Equal((PlayCamera.Beat.Set, 0), h.Step(PlayCamera.Beat.Set, 0, 0, hold));
        h.Reset();
        Assert.True(double.IsNaN(h.Since));
    }

    [Fact]
    public void ARoutineSixThreeIsSetThenOneCutThenTheFollow_NoBagCamOnTheThrow()
    {
        var content = ContentCatalog.Load();
        var match = Match.Slice(content, seed: 2);
        var hit = FlightFixtures.Landing(match.Park, 118, 4, -18);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        var field = match.ResolveFielding(hit, preview);
        var live = match.LivePlay;
        live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu));
        var hold = new PlayCamera.CameraHold();
        var beats = new List<PlayCamera.Beat>();
        var shotsSeen = new HashSet<string>();
        var threwToFirst = false;
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 30 && play is null; i++)
        {
            var view = new PlayCamera.LiveView(live.ElapsedSeconds, hit, live.RunnerPlay, live.InClosePlay, live.CloseBag,
                live.InRundown, live.RunnerPlayBag, 0, new Vec3(live.BallX, live.BallY, live.BallZ), new Vec3(0, 3, 0));
            var framed = PlayCamera.LiveFraming(content.Shots, view, content.Feel, hold);
            if (beats.Count == 0 || beats[^1] != hold.Beat) beats.Add(hold.Beat);
            if (framed is { } f)
            {
                shotsSeen.Add(f.Shot);
                // The follow looks at the dirt under the ball on every frame, the throw included.
                Assert.Equal(live.BallX, f.Look.X, 6);
                Assert.Equal(live.BallZ, f.Look.Z, 6);
            }
            threwToFirst |= live.Throwing && live.ThrowBag == 1;
            play = live.Apply(LivePlayCommand.Tick(1.0 / 60.0, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
        }
        Assert.NotNull(play);
        Assert.True(threwToFirst, "the shortstop throws to first");
        Assert.Contains(play!.Outcome!.OutsMade, o => o.Type == OutType.ThrowOutAtFirst);
        // The class beat is the hit's own (a pulled grounder here); every dirt class is the one diamond shot.
        Assert.Equal([PlayCamera.Beat.Set, PlayCamera.BeatFrom(hit)], beats);
        Assert.True(PlayCamera.BeatFrom(hit) is PlayCamera.Beat.Grounder or PlayCamera.Beat.GrounderPull);
        Assert.Equal([PlayCamera.InPlay], shotsSeen);
    }

    [Fact]
    public void ACloseStealIsOneBagCamOnItsBagForTheWholePlay()
    {
        var content = ContentCatalog.Load();
        var scenario = new Scenario(content, seed: 2).Runner(1, 1);
        var match = scenario.Match;
        Assert.True(match.StartSteal());
        Assert.False(match.BeginAtBat(Scenario.Paint, Scenario.Take, out _, out var finished));
        Assert.True(match.StealThrowPending);
        var live = match.LivePlay;
        live.Apply(LivePlayCommand.BeginSteal(finished!, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu));
        var hold = new PlayCamera.CameraHold();
        var targets = new List<(PlayCamera.Beat, int)>();
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 30 && play is null && live.Active; i++)
        {
            var view = new PlayCamera.LiveView(live.ElapsedSeconds, null, live.RunnerPlay, live.InClosePlay, live.CloseBag,
                live.InRundown, live.RunnerPlayBag, 0, new Vec3(live.BallX, live.BallY, live.BallZ), new Vec3(0, 3, 0));
            var framed = PlayCamera.LiveFraming(content.Shots, view, content.Feel, hold);
            Assert.NotNull(framed);
            if (targets.Count == 0 || targets[^1] != (hold.Beat, hold.Bag)) targets.Add((hold.Beat, hold.Bag));
            play = live.Apply(LivePlayCommand.Tick(1.0 / 60.0, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
        }
        Assert.NotNull(play);
        Assert.Equal([(PlayCamera.Beat.StealThrow, 2)], targets);
    }

    [Fact]
    public void ACloseRaceAtHomeIsTheTagCamOnThePlate()
    {
        var content = ContentCatalog.Load();
        var shots = content.Shots;
        var feel = content.Feel;
        var hold = new PlayCamera.CameraHold();
        // A tag-up race home: the follow through the catch and the relay, then the bag cam at the plate inside the margin.
        var ball = new Vec3(-120, 6, 200);
        PlayCamera.LiveView At(double t, bool close) =>
            new(t, Hopper with { Class = BattedBallClass.Fly }, false, close, close ? 4 : 0, false, 0, 0, ball, new Vec3(0, 3, 0));
        Assert.Equal(PlayCamera.InPlayFly, PlayCamera.LiveFraming(shots, At(0.5, false), feel, hold)!.Value.Shot);
        Assert.Equal(PlayCamera.InPlayFly, PlayCamera.LiveFraming(shots, At(3.0, false), feel, hold)!.Value.Shot);
        var tag = PlayCamera.LiveFraming(shots, At(5.0, true), feel, hold)!.Value;
        Assert.Equal(PlayCamera.TagShot, tag.Shot);
        Assert.True(tag.Cut);
        Assert.Equal(0, tag.Look.X, 6);
        Assert.Equal(0, tag.Look.Z, 6);
        // The verdict releases the bag cam back to the follow once the hold has passed.
        Assert.Equal(PlayCamera.TagShot, PlayCamera.LiveFraming(shots, At(5.1, false), feel, hold)!.Value.Shot);
        Assert.Equal(PlayCamera.InPlayFly, PlayCamera.LiveFraming(shots, At(5.0 + feel.CameraHoldSeconds, false), feel, hold)!.Value.Shot);
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
