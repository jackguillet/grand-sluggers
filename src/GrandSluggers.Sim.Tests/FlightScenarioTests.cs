using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Spec Appendix B rows owned by P2 (#564): the flight, the fence, and the chalk. S-20 … S-23 are
/// the resolver's geometry at contact; S-56 … S-59 drive the live ball through
/// <see cref="LivePlaySystem"/> with a scripted human glove (S-91: no scene object). Every
/// fixture is a real contact judged by the one flight (<see cref="FlightFixtures"/>).
/// </summary>
public sealed class FlightScenarioTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    const double Frame = 1.0 / 60.0;

    Park Harbor => _content.Parks["harbor-diamond"];

    // ---------------------------------------------------------------------------------
    // S-20 / S-21  The chalk is geometry: 44° over the fence is a homer, 46° is foul (§5.6)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S20_BallAt44DegreesOverTheFenceIsAFairHomeRun()
    {
        var hit = FlightFixtures.OverTheFence(Harbor, 10, 44);
        var ball = BattedBall.Of(hit, Harbor);
        Assert.True(hit.HomeRun);
        Assert.False(hit.Foul);
        Assert.True(hit.InPlay);
        Assert.Equal(BattedBallClass.Homer, hit.Class);
        Assert.True(FieldBounds.IsFair(ball.DecidedX, ball.DecidedZ));
        Assert.InRange(ball.LandingDist, AtBatResolver.FenceAt(Harbor, 44) - 1, AtBatResolver.FenceAt(Harbor, 44) + 1);

        var match = Match.Slice(_content, seed: 3);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.HomeRunLikely, "the preview reads the same flight as the resolver");
        var field = match.ResolveFielding(hit, preview);
        var play = match.FinishAtBat(Scenario.Paint, Scenario.Swing, hit, field);
        Assert.Equal(PlayKind.HomeRun, play.Kind);
        Assert.Equal(4, play.Outcome!.BatterToBag);
    }

    [Fact]
    public void S21_TheSameBallAt46DegreesIsFoulNotAHomer()
    {
        var fair = FlightFixtures.OverTheFence(Harbor, 10, 44);
        var hit = FlightFixtures.Hit(Harbor, fair.ExitVeloMph, fair.LaunchDeg, 46, ContactQuality.Perfect);
        var ball = BattedBall.Of(hit, Harbor);
        Assert.True(hit.Foul);
        Assert.False(hit.HomeRun);
        Assert.False(hit.InPlay);
        Assert.Equal(BattedBallClass.Foul, ball.Class);
        Assert.False(FieldBounds.IsFair(ball.DecidedX, ball.DecidedZ));
        Assert.False(InPlay.FairContactSendsBatter(hit));
    }

    // ---------------------------------------------------------------------------------
    // S-22 / S-23  A roller is judged where it rests or passes the bag, not by its spray (§5.6)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S22_RollerThatSettlesFoulBeforeFirstUntouchedIsFoul()
    {
        var hit = FlightFixtures.Hit(Harbor, 36, 5, 47);
        var ball = BattedBall.Of(hit, Harbor);
        Assert.True(hit.Foul);
        Assert.Equal(BattedBallClass.Foul, ball.Class);
        Assert.False(FieldBounds.PastTheBags(ball.DecidedX, ball.DecidedZ), "judged before the bag, where it came to rest");
        Assert.False(FieldBounds.IsFair(ball.DecidedX, ball.DecidedZ));
        Assert.Equal(BallFlight.RestTime(ball.Samples), ball.DecidedT, 3);
    }

    [Fact]
    public void S23_GrounderThatPassesFirstFairIsFairWhereverItRollsAfter()
    {
        var hit = FlightFixtures.Hit(Harbor, 84, 8, 44);
        var ball = BattedBall.Of(hit, Harbor);
        Assert.False(hit.Foul);
        Assert.True(hit.InPlay);
        Assert.True(ball.Shape.OnTheDirt());
        Assert.True(FieldBounds.PastTheBags(ball.DecidedX, ball.DecidedZ), "judged as it passes the bag");
        Assert.True(FieldBounds.IsFair(ball.DecidedX, ball.DecidedZ));
        Assert.True(ball.DecidedT < BallFlight.RestTime(ball.Samples), "the roll after the bag cannot change the call");

        // The same slow roller as S-22 inside the line settles fair before the bag.
        var slow = BattedBall.Of(36, 5, 44, Harbor);
        Assert.False(slow.Foul);
        Assert.False(FieldBounds.PastTheBags(slow.DecidedX, slow.DecidedZ));
    }

    // ---------------------------------------------------------------------------------
    // S-24 / S-24b / S-21 live  A foul flight is fielded; a foul nobody plays is dead on landing (§7.11, #575)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S24_FoulPopBehindThePlate_CatcherUnderIt_IsAnOut()
    {
        var hit = FlightFixtures.Hit(Harbor, 30, 80, 180);
        Assert.True(hit.Foul);
        Assert.Equal(BattedBallClass.Pop, hit.Class);
        var match = Match.Slice(_content, seed: 2);
        var preview = match.PreviewHit(hit);
        Assert.Equal("C", preview.Position);
        Assert.True(preview.Foul);
        Assert.False(FlyCatch.NeedsJump(preview), "it comes down inside the backstop wrap: a plain catch");
        // The untouched path is foul; whether the catcher gets under it is the live glove's (§8.3).
        var field = match.ResolveFielding(hit, preview);
        Assert.Equal(PlayKind.Foul, field.Kind);

        var strikes = match.Strikes;
        var play = RunCpu(match, hit, preview, field, out var caughtAt);
        Assert.Equal(PlayKind.FlyOut, play.Kind);
        var only = Assert.Single(play.Outcome!.OutsMade);
        Assert.Equal(OutType.Catch, only.Type);
        Assert.Equal(0, only.FromBag);
        Assert.Equal(preview.Fielder.Id, only.Fielder?.Id);
        Assert.True(caughtAt > 0 && caughtAt <= preview.HangTimeSec + Frame, $"caught at {caughtAt:0.00} before it lands at {preview.HangTimeSec:0.00}");
        Assert.Equal(1, match.Outs);
        Assert.Equal(0, match.Strikes);
        _ = strikes;
    }

    [Fact]
    public void S24b_FoulFlyNobodyReaches_IsFoulAndDeadWithinTheCountHold()
    {
        var hit = FlightFixtures.Hit(Harbor, 85, 30, 50);
        var ball = BattedBall.Of(hit, Harbor);
        Assert.True(hit.Foul);
        Assert.NotNull(ball.LeavesT);
        var match = Match.Slice(_content, seed: 2);
        var preview = match.PreviewHit(hit);
        Assert.Contains(preview.Position, FieldingResolver.FoulPursuitPositions);
        Assert.True(FlyCatch.NeedsJump(preview), "it sails into the stands: only a rob at the rail takes it");
        Assert.Equal(PlayKind.Foul, match.ResolveFielding(hit, preview).Kind);

        var strikes = match.Strikes;
        var play = RunHuman(match, hit, preview, _ => new LivePadInput(StickX: -1, StickY: -1), out var caughtAt, out var deadAt);
        Assert.True(caughtAt < 0);
        Assert.Equal(PlayKind.Foul, play.Kind);
        Assert.Equal("FOUL", PlayStamp.Label(play));
        Assert.Equal(strikes + 1, match.Strikes);
        Assert.Equal(0, match.Outs);
        var hold = _content.Feel.AfterCountSeconds;
        Assert.True(deadAt >= ball.DecidedT - Frame, $"dead at {deadAt:0.00} before its call at {ball.DecidedT:0.00}");
        Assert.True(deadAt <= ball.DecidedT + hold + Frame, $"dead at {deadAt:0.00}, more than {hold} after landing at {ball.DecidedT:0.00}");
        Assert.False(match.LivePlay.Active, "the play never hangs");
    }

    [Fact]
    public void S21_Live_FoulAt46DegreesIsDeadWhenItLands()
    {
        var fair = FlightFixtures.OverTheFence(Harbor, 10, 44);
        var hit = FlightFixtures.Hit(Harbor, fair.ExitVeloMph, fair.LaunchDeg, 46, ContactQuality.Perfect);
        var ball = BattedBall.Of(hit, Harbor);
        var match = Match.Slice(_content, seed: 2);
        var preview = match.PreviewHit(hit);
        var field = match.ResolveFielding(hit, preview);
        Assert.Equal(PlayKind.Foul, field.Kind);
        var strikes = match.Strikes;
        var play = RunCpu(match, hit, preview, field, out var caughtAt, out var deadAt);
        Assert.True(caughtAt < 0);
        Assert.Equal(PlayKind.Foul, play.Kind);
        Assert.Equal(strikes + 1, match.Strikes);
        Assert.InRange(deadAt, ball.DecidedT - Frame, ball.DecidedT + _content.Feel.AfterCountSeconds + Frame);
        Assert.Equal(0, play.Outcome!.BatterToBag);
        Assert.Empty(play.Outcome.Moves);
    }

    [Fact]
    public void FoulRollerTouchedOnFoulGroundIsDeadInTheGlove()
    {
        var hit = FlightFixtures.Hit(Harbor, 36, 5, 47);
        var ball = BattedBall.Of(hit, Harbor);
        Assert.True(hit.Foul);
        Assert.True(ball.DecidedT > ball.HangT + 1, "untouched it would roll a while before resting foul");
        var match = Match.Slice(_content, seed: 2);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Grounder);
        var field = match.ResolveFielding(hit, preview);
        Assert.Equal(PlayKind.Foul, field.Kind);
        var play = RunCpu(match, hit, preview, field, out var caughtAt, out var deadAt);
        Assert.True(caughtAt > 0, "the corner glove scoops the roller");
        Assert.Equal(FairFoulCall.Undecided, match.LivePlay.Call); // reset after the commit
        Assert.Equal(PlayKind.Foul, play.Kind);
        Assert.True(deadAt < ball.DecidedT, $"the touch at {caughtAt:0.00} made the call; dead at {deadAt:0.00}, not at rest {ball.DecidedT:0.00}");
    }

    // ---------------------------------------------------------------------------------
    // S-56 / S-57  The rob is a height at the wall (§8.4)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S56_SuperJumpInTheWindowAtTheWallRobsATwelveFootHomer()
    {
        var match = RobbersMatch(centerFielder: "nico");
        var (hit, preview, play, caughtAt) = RunWallLeap(match, out var feat);
        Assert.Equal("nico", preview.Fielder.Id);
        Assert.Equal(PlayKind.FlyOut, play.Kind);
        var only = Assert.Single(play.Outcome!.OutsMade);
        Assert.Equal((OutType.Catch, 0), (only.Type, only.FromBag));
        Assert.True(FlyCatch.JumpWindow(caughtAt, preview.HangTimeSec, preview.Fielder, match.Park, match.Rules)
                    || FlyCatch.JumpWindow(caughtAt - Frame, preview.HangTimeSec, preview.Fielder, match.Park, match.Rules),
            $"robbed at {caughtAt:0.00} vs the fence crossing {preview.HangTimeSec:0.00}");
        Assert.Contains(feat, new[] { DefensiveFeat.SuperJump, DefensiveFeat.BuddyJump });
        Assert.Equal(feat, play.Outcome.DefensiveFeat);
        Assert.True(hit.HomeRun, "the flight itself clears the fence; the leap took it back");
    }

    [Fact]
    public void S57_ThePlainJumpCannotReachTwelveFeetOverTheFence()
    {
        var match = RobbersMatch(centerFielder: "grit");
        var (hit, preview, play, caughtAt) = RunWallLeap(match, out _);
        Assert.Equal("grit", preview.Fielder.Id);
        Assert.True(caughtAt < 0, "no catch: the plain jump's rob height is four feet");
        Assert.Equal(PlayKind.HomeRun, play.Kind);
        Assert.Equal(4, play.Outcome!.BatterToBag);
        Assert.True(hit.HomeRun);
    }

    // ---------------------------------------------------------------------------------
    // S-58 / S-59  Off the wall is live; bounced then over is two bases for everyone (§7.9, §1)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S58_FlyOffTheWallCaromsBackIntoThePark_LiveToADouble()
    {
        var hit = FlightFixtures.Hit(Harbor, 110, 35, 0, ContactQuality.Perfect);
        var ball = BattedBall.Of(hit, Harbor);
        Assert.Equal(BattedBallClass.Wall, hit.Class);
        Assert.NotNull(ball.WallT);
        Assert.InRange(ball.FenceClearFt, -Harbor.FenceHeightFt, 0);
        var afterWall = ball.Samples.Where(s => s.T > ball.WallT + 0.3).ToArray();
        Assert.NotEmpty(afterWall);
        Assert.All(afterWall, s => Assert.True(FieldBounds.InPark(Harbor, s.X, s.Z), "the carom stays on the field"));
        Assert.True(afterWall[^1].Dist < Harbor.CenterFenceFt - 5, "it drops and rolls back off the wall");

        var match = Match.Slice(_content, seed: 2);
        var preview = match.PreviewHit(hit);
        var plant = FlyCatch.ChaseTarget(preview, match.Park);
        Assert.True(Diamond.Dist(0, 0, plant.X, plant.Z) < Harbor.CenterFenceFt, "the glove plants inside the wall");
        var cued = false;
        var wallAt = preview.HangTimeSec;
        // The human steps off the plant through the catch window (no wall catch), leaves the stick so the
        // glove plays the carom like CPU, then throws to third once they have it. The batter's body reads
        // the pickup and the arm (§7.9, §9.9): a live play to the end, never a dead double. How far they get
        // is the arm table's (fielding.throw), P4's to make an arm.
        var play = RunHuman(match, hit, preview,
            live => live.HoldsBall ? new LivePadInput(SouthDown: true, KeysBag: 3)
                : live.ElapsedSeconds > wallAt - 0.7 && live.ElapsedSeconds < wallAt + 0.4 ? new LivePadInput(StickX: -1, StickY: 0)
                : LivePadInput.Dead,
            out var caughtAt, live => cued |= live.Events.Contains(LiveEvent.WallCarom));
        Assert.True(caughtAt < 0 || caughtAt > wallAt, "no catch in the air: the ball met the wall first; the pickup came after");
        Assert.True(cued, "the wall thump is a cue the client plays");
        Assert.True(play.Kind is PlayKind.Double or PlayKind.Triple or PlayKind.HomeRun, play.Kind.ToString());
        Assert.False(play.Outcome!.GroundRuleDouble);
        Assert.InRange(play.Outcome.BatterToBag, 2, 4);
        Assert.Empty(play.Outcome.OutsMade);
    }

    [Fact]
    public void S59_BounceThenOverTheFenceIsAGroundRuleDouble_EveryRunnerPlusTwo()
    {
        var hit = FlightFixtures.Hit(Harbor, 104, 30, 0, ContactQuality.Perfect);
        var ball = BattedBall.Of(hit, Harbor);
        Assert.True(ball.GroundRule, "lands on the field, hops the fence");
        Assert.False(ball.HomeRun);
        Assert.NotNull(ball.LeavesT);
        Assert.True(ball.LeavesT > ball.HangT);

        var scenario = new Scenario(_content, seed: 2).Runner(1, 1).Runner(2, 2);
        var match = scenario.Match;
        var first = match.First!;
        var second = match.Second!;
        var preview = match.PreviewHit(hit);
        var play = RunHuman(match, hit, preview, _ => new LivePadInput(StickX: -1, StickY: 0), out var caughtAt);
        Assert.True(caughtAt < 0);
        Assert.Equal(PlayKind.Double, play.Kind);
        Assert.True(play.Outcome!.GroundRuleDouble);
        Assert.Equal(2, play.Outcome.BatterToBag);
        Assert.Contains(play.Outcome.Moves, m => m.Runner.Id == second.Id && m.FromBag == 2 && m.ToBag == 4);
        Assert.Contains(play.Outcome.Moves, m => m.Runner.Id == first.Id && m.FromBag == 1 && m.ToBag == 3);
        Assert.Equal(1, play.RunsScored);
        Assert.Equal(first.Id, match.Third?.Id);
        Assert.Null(match.First);
    }

    // ---------------------------------------------------------------------------------
    // Property: a fair trajectory never leaves the park except over the fence (§6.1)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void NoSampleOfAFairTrajectoryLiesOutsideTheParkExceptAboveTheFence()
    {
        var fair = 0;
        foreach (var park in _content.Parks.Values)
        foreach (var exit in new[] { 40.0, 70.0, 90.0, 105.0, 120.0 })
        foreach (var launch in new[] { 3.0, 8.0, 14.0, 22.0, 30.0, 45.0 })
        foreach (var spray in new[] { -44.0, -30.0, -10.0, 0.0, 15.0, 35.0, 44.5 })
        {
            var ball = BattedBall.Of(exit, launch, spray, park);
            if (ball.Foul) continue;
            fair++;
            var samples = ball.Samples;
            for (var i = 0; i < samples.Count; i++)
            {
                var s = samples[i];
                if (ball.LeavesT is { } left && s.T >= left)
                {
                    if (s.T == left)
                    {
                        // Out only over the top: the fence between the poles, or the foul wrap after a fair bounce (two bases either way).
                        var top = s.Event == SampleEvent.Fence ? park.FenceHeightFt : FieldBounds.FoulWallHeightFt;
                        Assert.True(s.Event is SampleEvent.Fence or SampleEvent.Stands && s.Height >= top - 0.01,
                            $"{park.Id} {exit}/{launch}@{spray}: left the park at {s.T:0.00} below the wall ({s.Height:0.0} ft, {s.Event})");
                        Assert.True(ball.HomeRun || ball.GroundRule, "a fair ball that leaves is a homer or two bases");
                    }
                    break;
                }
                Assert.True(FieldBounds.InPark(park, s.X, s.Z) || NearBoundary(park, s),
                    $"{park.Id} {exit}/{launch}@{spray}: sample {s.T:0.00} at ({s.X:0.0},{s.Z:0.0}) h {s.Height:0.0} is outside the park");
            }
        }
        Assert.True(fair > 200, $"only {fair} fair fixtures");
    }

    static bool NearBoundary(Park park, Sample s)
    {
        var r = FieldBounds.DistHome(s.X, s.Z);
        var edge = FieldBounds.Of(park).RadiusAt(FieldBounds.SprayDeg(s.X, s.Z));
        return Math.Abs(r - edge) < 0.6;
    }

    // ---------------------------------------------------------------------------------
    // One clock (§0.3, §6.1): the ball's sample clock is the clock the live ball advances
    // ---------------------------------------------------------------------------------

    [Fact]
    public void OneClock_TheBallInFlightSitsOnItsSampleClockAtTheLivePlaysElapsedSeconds()
    {
        var match = Match.Slice(_content, seed: 2);
        var hit = FlightFixtures.Hit(match.Park, 92, 34, 4);
        var preview = match.PreviewHit(hit);
        var field = new FieldingResult(PlayKind.FlyOut, preview.Fielder, null, preview.HangTimeSec, preview.LandingX, preview.LandingZ, false, false);
        match.LivePlay.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field, LiveSeats.CpuOnly));
        var live = match.LivePlay;
        var path = live.Path!;
        var dt = 1.0 / match.Rules.Flight.SampleHz * match.Rules.Flight.TimeScale;
        Assert.Equal(dt, path[2].T - path[1].T, 9);
        var checks = 0;
        for (var i = 0; i < 60 * 4 && !live.Caught; i++)
        {
            live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu));
            if (live.Caught) break;
            var p = BallFlight.PointAt(path, live.ElapsedSeconds, match.Rules);
            Assert.Equal(p.X, live.BallX, 6);
            Assert.Equal(p.Y, live.BallY, 6);
            Assert.Equal(p.Z, live.BallZ, 6);
            checks++;
        }
        Assert.True(checks > 30, "the ball flew on the one clock for a while");
        Assert.True(live.ElapsedSeconds > 0.5);
    }

    // ---------------------------------------------------------------------------------
    // §8.1  Positions come from the lineup's glove diamond, not roster order (S-26 for the swap)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void PositionsComeFromTheLineupGloveDiamondNotRosterOrder()
    {
        var home = TeamBuilder.Draft(_content, "rio");
        var byRoster = FieldingResolver.Assign(home.ToTeam().Roster, home.ToTeam().Pitcher);
        // Offense / Defense Setup: the captain moves to center; the starting center fielder takes the mound.
        var centerFielder = home.Gloves["CF"];
        Assert.True(home.SetGlove("CF", "rio"));
        Assert.Equal("rio", home.Gloves["CF"].Id);
        Assert.Equal(centerFielder.Id, home.Gloves["P"].Id);
        var team = home.ToTeam();
        Assert.NotNull(team.Gloves);

        var match = Match.Exhibition(_content, team, PresetTeams.EmberCourt(_content), seed: 3);
        Assert.True(match.Top, "the home nine is on defense");
        var assigned = FieldingResolver.Assign(match.Defense, match.Pitcher);
        Assert.Equal("rio", assigned["CF"].Id);
        Assert.Equal(centerFielder.Id, assigned["P"].Id);
        Assert.Equal(centerFielder.Id, match.Pitcher.Id);
        foreach (var pos in Diamond.Order)
            Assert.Equal(home.Gloves[pos].Id, assigned[pos].Id);
        Assert.NotEqual(byRoster["CF"].Id, assigned["CF"].Id);

        // The live ball reads the same diamond: a fly to center is the captain's.
        var fly = FlightFixtures.Hit(match.Park, 90, 34, 0);
        var preview = match.PreviewHit(fly);
        Assert.Equal("CF", preview.Position);
        Assert.Equal("rio", preview.Fielder.Id);

        // S-26: a swap puts the new pitcher on the mound and the old pitcher on the vacated glove.
        var before = FieldingResolver.Assign(match.Defense, match.Pitcher);
        Assert.True(match.SwapPitcher());
        var swapped = FieldingResolver.Assign(match.Defense, match.Pitcher);
        var vacated = Diamond.Order.Single(pos => before[pos].Id == match.Pitcher.Id);
        Assert.NotEqual("P", vacated);
        Assert.Equal(centerFielder.Id, swapped[vacated].Id);
        Assert.Equal(match.Pitcher.Id, swapped["P"].Id);
        foreach (var pos in Diamond.Order.Where(p => p != "P" && p != vacated))
            Assert.Equal(before[pos].Id, swapped[pos].Id);
    }

    // ---------------------------------------------------------------------------------

    /// <summary>A Harbor match whose home defense (the top half) has the named glove in center.</summary>
    Match RobbersMatch(string centerFielder)
    {
        var mates = new List<string> { "vale", "boom", "lace", "marlow", "pip", "zig", centerFielder, "dart" };
        var home = _content.Team("Robbers", "rio", mates.ToArray());
        var away = _content.Team("Visitors", "ashlord", "cinder", "frost", "hex", "gull", "konga", "basil", "soot", "pewter");
        var match = new Match(_content, away, home, Harbor, innings: 3, seed: 1);
        Assert.True(match.Top);
        Assert.Equal(centerFielder, FieldingResolver.Assign(match.Defense.Roster, match.Pitcher)["CF"].Id);
        return match;
    }

    /// <summary>A twelve-foot homer to center; the human glove is dead-stick to the wall and presses West in the window.</summary>
    (AtBatResult Hit, FieldingPreview Preview, PlayEvent Play, double CaughtAt) RunWallLeap(Match match, out DefensiveFeat feat)
    {
        var hit = FlightFixtures.OverTheFence(Harbor, 12, 0);
        var ball = BattedBall.Of(hit, Harbor);
        Assert.InRange(ball.FenceClearFt, 11, 13);
        var preview = match.PreviewHit(hit);
        Assert.Equal("CF", preview.Position);
        Assert.True(FlyCatch.NeedsJump(preview));
        var who = preview.Fielder;
        var play = RunHuman(match, hit, preview, live =>
            FlyCatch.JumpWindow(live.ElapsedSeconds, preview.HangTimeSec, who, match.Park, match.Rules)
                ? new LivePadInput(WestDown: true)
                : LivePadInput.Dead, out var caughtAt);
        feat = play.Outcome?.DefensiveFeat ?? DefensiveFeat.None;
        return (hit, preview, play, caughtAt);
    }

    /// <summary>The human owns the glove from contact; the pad is scripted per frame from the live state.</summary>
    static PlayEvent RunHuman(Match match, AtBatResult hit, FieldingPreview preview, Func<LivePlaySystem, LivePadInput> pad,
        out double caughtAt, Action<LivePlaySystem>? observe = null) =>
        RunHuman(match, hit, preview, pad, out caughtAt, out _, observe);

    /// <summary>The CPU seat: dead pads, the resolver's call for the glove.</summary>
    static PlayEvent RunCpu(Match match, AtBatResult hit, FieldingPreview preview, FieldingResult field, out double caughtAt) =>
        RunCpu(match, hit, preview, field, out caughtAt, out _);

    static PlayEvent RunCpu(Match match, AtBatResult hit, FieldingPreview preview, FieldingResult field, out double caughtAt, out double deadAt)
    {
        var live = match.LivePlay;
        var begun = live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu));
        Assert.True(begun.Snapshot.Active);
        return Drive(live, _ => LivePadInput.Dead, LivePlayCommandSource.Cpu, null, out caughtAt, out deadAt);
    }

    static PlayEvent RunHuman(Match match, AtBatResult hit, FieldingPreview preview, Func<LivePlaySystem, LivePadInput> pad,
        out double caughtAt, out double deadAt, Action<LivePlaySystem>? observe = null)
    {
        var seats = new LiveSeats(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);
        var live = match.LivePlay;
        var begun = live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, seats, 0, LivePlayCommandSource.Human));
        Assert.True(begun.Snapshot.Active);
        Assert.True(live.PlayerFielding);
        return Drive(live, pad, LivePlayCommandSource.Human, observe, out caughtAt, out deadAt);
    }

    /// <summary>Ticks the live ball to Complete. <paramref name="deadAt"/> is the play clock at the commit tick.</summary>
    static PlayEvent Drive(LivePlaySystem live, Func<LivePlaySystem, LivePadInput> pad, LivePlayCommandSource seat,
        Action<LivePlaySystem>? observe, out double caughtAt, out double deadAt)
    {
        caughtAt = -1;
        deadAt = -1;
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 25 && play is null; i++)
        {
            var before = live.ElapsedSeconds;
            var result = live.Apply(LivePlayCommand.Tick(Frame, pad(live), LivePadInput.Dead, false, seat));
            observe?.Invoke(live);
            play = result.CompletedPlay;
            // A catch with nobody on (or a foul touch) is Time at once: the play completes, and the live ball
            // resets, on the touch tick — the glove cue raised that tick is the record of it.
            if (caughtAt < 0 && (live.Caught || live.Events.Contains(LiveEvent.Glove)
                                 || play?.Outcome?.OutsMade.Any(o => o.Type == OutType.Catch) == true))
                caughtAt = before + Frame;
            if (play is not null) deadAt = before + Frame;
        }
        Assert.NotNull(play);
        return play!;
    }
}
