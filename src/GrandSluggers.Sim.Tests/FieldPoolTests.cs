using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The field pool (AB-12; spec §8.4, §8.5, §8.9, §14): Snap Throw, Lick Catch, Laser, Relay Pivot and Wall Spring, one per
/// character, and Canopy Yard's climb wall as a park rule. Each relationship is the ball, the glove and the wall, never a
/// roll: Relay Pivot's release against the ordinary pivot, Wall Spring's reach only at a wall, the pressed tongue ahead and
/// never behind, the validator's closed pool, and any fielder climbing Canopy's wall. Rows S-254 … S-261.
/// </summary>
public sealed class FieldPoolTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60;
    static readonly PitchCommand Paint = new("fastball", 0, false);
    static readonly SwingCommand Swing = new(true, 0, 0, false);
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    // ---------------------------------------------------------------------------------
    // S-261  Every character carries one of the five
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S261_ThePoolIsFiveAndEveryCharacterCarriesOneSidekicksTheirSpecies()
    {
        Assert.Equal(
            new[] { "laser", "lick-catch", "relay-pivot", "snap-throw", "wall-spring" },
            FieldAbilityId.All.OrderBy(a => a, StringComparer.Ordinal));
        foreach (var c in Game.Characters.Values)
        {
            Assert.Contains(c.FieldAbility, FieldAbilityId.All);
            if (!c.Captain) Assert.Equal(Game.Species[c.Species].FieldAbility, c.FieldAbility);
        }
        var plan = new Dictionary<string, string>
        {
            ["rio"] = FieldAbilityId.Laser, ["vale"] = FieldAbilityId.SnapThrow, ["zig"] = FieldAbilityId.LickCatch,
            ["brondo"] = FieldAbilityId.Laser, ["konga"] = FieldAbilityId.WallSpring, ["ashlord"] = FieldAbilityId.Laser,
            ["fenn"] = FieldAbilityId.SnapThrow, ["sable"] = FieldAbilityId.SnapThrow, ["hollis"] = FieldAbilityId.WallSpring,
            ["reed"] = FieldAbilityId.LickCatch,
        };
        foreach (var (id, ability) in plan) Assert.Equal(ability, Game.Must(id).FieldAbility);
        // The species default by build: the bruiser throws the laser, the scamp springs off the wall, the glove snaps.
        foreach (var s in Game.Species.Values)
            Assert.Equal(s.Build switch
            {
                SpeciesBuilds.Bruiser => FieldAbilityId.Laser,
                SpeciesBuilds.Scamp => FieldAbilityId.WallSpring,
                _ => FieldAbilityId.SnapThrow
            }, s.FieldAbility);
        // Lick Catch only on a tongue body.
        Assert.All(Game.Characters.Values.Where(c => c.FieldAbility == FieldAbilityId.LickCatch),
            c => Assert.True(FieldAbilities.TongueBody(c.Faction, Game.Rules), c.Id));
    }

    // ---------------------------------------------------------------------------------
    // S-260  The validator keeps the pool closed
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S260_TheValidatorRefusesARetiredAbilityASixthOneAndLickCatchOffATongueBody()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/konga.json", json => json["fieldAbility"] = "clamber");
        fixture.ChangeObject("characters/vale.json", json => json["fieldAbility"] = "tail-whip");
        fixture.ChangeObject("characters/rio.json", json => json["fieldAbility"] = "lick-catch");
        fixture.ChangeObject("world/species.json", json =>
        {
            foreach (var s in json["species"]!.AsArray())
                if ((string?)s!["id"] == "big-kids") s["fieldAbility"] = "lick-catch";
        });
        fixture.ChangeArray("characters/role-players.json", rows =>
        {
            foreach (var r in rows)
                if ((string?)r!["id"] == "nico") r["fieldAbility"] = "laser";
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("character 'konga' fieldAbility must be one of", StringComparison.Ordinal) && e.Contains("'clamber'"));
        Assert.Contains(errors, e => e.Contains("character 'vale' fieldAbility must be one of", StringComparison.Ordinal) && e.Contains("'tail-whip'"));
        Assert.Contains(errors, e => e.Contains("character 'rio' carries lick-catch, but faction 'spark' is not a tongue body", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("species 'big-kids' carries lick-catch, but faction 'spark' is not a tongue body", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("sidekick 'nico' names fieldAbility 'laser'", StringComparison.Ordinal));
        // The shipped root is clean, and Zig and Reed are tongue bodies.
        Assert.Empty(ContentDataValidator.Validate(Game.Root));
        Assert.True(FieldAbilities.TongueBody(Game.Must("zig").Faction, Game.Rules));
        Assert.True(FieldAbilities.TongueBody(Game.Must("reed").Faction, Game.Rules));
        Assert.False(FieldAbilities.TongueBody(Game.Must("rio").Faction, Game.Rules));
    }

    // ---------------------------------------------------------------------------------
    // S-254  Relay Pivot: the cutoff throws on 0.15 s after its relay catch
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S254_RelayPivotIsTheCutoffsReleaseOnARelayOnly()
    {
        var r = Game.Rules;
        var a = r.Fielding.Abilities;
        var pivot = Game.Must("vale") with { FieldAbility = FieldAbilityId.RelayPivot };
        var snap = Game.Must("vale");
        var plain = Game.Must("rio");
        Assert.Equal(0.15, a.RelayPivotReleaseSec);
        Assert.True(a.RelayPivotReleaseSec < r.Fielding.Throw.ReleaseSec);
        Assert.Equal(a.RelayPivotReleaseSec, FieldAbilities.ReleaseSec(pivot, receivedClean: true, relayLeg: true, r));
        // Not a relay the cutoff caught, or not clean: the ordinary pivot.
        Assert.Null(FieldAbilities.ReleaseSec(pivot, receivedClean: true, relayLeg: false, r));
        Assert.Null(FieldAbilities.ReleaseSec(pivot, receivedClean: false, relayLeg: true, r));
        Assert.Null(FieldAbilities.ReleaseSec(plain, receivedClean: true, relayLeg: true, r));
        // Snap Throw is its own release on any clean received throw, relay or not.
        Assert.Equal(a.SnapReleaseSec, FieldAbilities.ReleaseSec(snap, receivedClean: true, relayLeg: true, r));
        Assert.Equal(a.SnapReleaseSec, FieldAbilities.ReleaseSec(snap, receivedClean: true, relayLeg: false, r));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void S254_TheLiveCutoffThrowsOnAtItsPivotsRelease(bool pivot)
    {
        // CF catches the deep fly, the human feeds the cutoff with home armed and presses the onward throw while the feed flies:
        // it fires at the catch. Every infielder carries Relay Pivot, or none does (Wall Spring throws nothing).
        var (release, from) = RunRelay(pivot ? FieldAbilityId.RelayPivot : FieldAbilityId.WallSpring);
        Assert.NotEqual("", from);
        Assert.NotEqual("CF", from);
        var r = Game.Rules;
        Assert.Equal(pivot ? r.Fielding.Abilities.RelayPivotReleaseSec : r.Fielding.Throw.ReleaseSec, release, 9);
    }

    static (double Release, string From) RunRelay(string infieldAbility)
    {
        var ids = new[] { "vale", "pewter", "lace", "marlow", "grit", "frost", "basil", "moss", "gull" };
        var home = Game.Team("Relay", ids[0], ids.Skip(1).ToArray());
        var match = Match.Exhibition(Game, home, PresetTeams.EmberCourt(Game), seed: 1, parkId: ParkId.Harbor);
        var map = FieldingResolver.Assign(match.Defense, match.Pitcher);
        var outfield = new[] { map["LF"].Id, map["CF"].Id, map["RF"].Id };
        var roster = home.Roster.Select(c => outfield.Contains(c.Id) ? c : c with { FieldAbility = infieldAbility }).ToList();
        home = home with { Captain = roster.Single(c => c.Id == home.Captain.Id), Roster = roster };
        match = Match.Exhibition(Game, home, PresetTeams.EmberCourt(Game), seed: 1, parkId: ParkId.Harbor);
        // A runner on third keeps the play live past the catch, so the relay has a throw home to make.
        Assert.True(match.StationRunner(3, match.Away.Roster[4]));
        var hit = TutorialContact.Create(match.Park, new TutorialBall(245, 0, 34, 0), match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("CF", preview.Position);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Paint, Swing, hit, preview, null, HumanGlove, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        var stage = 0;
        var release = -1.0;
        var from = "";
        for (var i = 0; i < 60 * 20 && release < 0; i++)
        {
            var pad = LivePadInput.Dead;
            if (stage == 0 && live.HoldsBall && live.GlovePos == "CF") { pad = new(KeysBag: 4); stage = 1; }
            else if (stage == 1) { pad = new(Cutoff: true); stage = 2; }
            else if (stage == 2 && live.Throwing && live.ThrowBag == 0 && live.ThrowDur - live.ThrowT <= .15) { pad = new(SouthDown: true); stage = 3; }
            var result = live.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false, LivePlayCommandSource.Human));
            if (stage == 3 && live.Events.Contains(LiveEvent.ThrowCommitted) && live.ThrowBag == 4)
            {
                release = live.ThrowReleaseSec;
                from = live.ThrowFromPos;
            }
            if (result.CompletedPlay is not null) break;
        }
        Assert.True(release >= 0, "the onward throw was committed");
        return (release, from);
    }

    // ---------------------------------------------------------------------------------
    // S-255  Wall Spring: 4 ft more, only at a wall
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S255_WallSpringReachesFourFeetFurtherOnlyAtAWall()
    {
        var r = Game.Rules;
        var park = Game.Parks[ParkId.Harbor];
        var konga = Game.Must("konga");
        var ashlord = Game.Must("ashlord");
        var spring = r.Fielding.Abilities.WallSpringReachFt;
        Assert.Equal(4, spring);
        var wall = (0.0, park.CenterFenceFt - r.Fielding.WallPlant.InsideFenceFt);
        var deep = (0.0, park.CenterFenceFt - 60);
        Assert.Equal(spring, FieldAbilities.WallSpringFt(konga, park, wall.Item1, wall.Item2, r));
        Assert.Equal(0, FieldAbilities.WallSpringFt(konga, park, deep.Item1, deep.Item2, r));
        Assert.Equal(0, FieldAbilities.WallSpringFt(ashlord, park, wall.Item1, wall.Item2, r));
        // Over any wall: the rob reaches the plain jump's height plus the spring, and no higher.
        Assert.Equal(r.Fielding.Catch.JumpRobFt + spring, FlyCatch.RobHeightFt(konga, park, r));
        Assert.Equal(r.Fielding.Catch.JumpRobFt, FlyCatch.RobHeightFt(ashlord, park, r));
        // The stand-up ring is the body's; no pool ability widens it.
        Assert.Equal(BodyClasses.ReachFt(konga, true, r), FieldingResolver.CatchRadiusFt(konga, park, r, air: true));
    }

    [Theory]
    [InlineData("konga", true)]
    [InlineData("ashlord", false)]
    public void S255_TheCpuCentreFielderWithWallSpringRobsASixFootHomer(string cf, bool robbed)
    {
        var match = Defending(ParkId.Harbor, cf);
        var hit = FlightFixtures.OverTheFence(match.Park, 6, 0);
        Assert.InRange(BattedBall.Of(hit, match.Park, match.Rules).FenceClearFt, 5, 7);
        var preview = match.PreviewHit(hit);
        Assert.Equal(cf, preview.Fielder.Id);
        Assert.False(FieldingResolver.BuddyJumpOffered(preview));
        var ev = match.FinishAtBat(Paint, Swing, hit, match.ResolveFielding(hit, preview));
        Assert.Equal(robbed ? PlayKind.FlyOut : PlayKind.HomeRun, ev.Kind);
        if (robbed) Assert.Equal(HighlightBeat.RobbedHomer, Highlight.BeatOf(ev));
    }

    // ---------------------------------------------------------------------------------
    // S-256 / S-257 / S-258  Lick Catch is the pressed tongue
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S257_TheTongueReachesAheadAlongTheFacingNeverBehindOrPastItsReach()
    {
        var r = Game.Rules;
        var a = r.Fielding.Abilities;
        Assert.Equal((8, 0.3, 0.8), (a.LickReachFt, a.LickSnapSec, a.LickRecoverySec));
        // Facing +z from the origin.
        Assert.True(FieldAbilities.TongueReaches(0, 0, 0, 1, 0, 3, 7.9, r), "8 ft ahead is on the tongue");
        Assert.False(FieldAbilities.TongueReaches(0, 0, 0, 1, 0, 3, 8.1, r), "past the reach");
        Assert.False(FieldAbilities.TongueReaches(0, 0, 0, 1, 0, 3, -2, r), "behind the body");
        Assert.False(FieldAbilities.TongueReaches(0, 0, 0, 1, a.LickWidthFt + 0.1, 3, 5, r), "beside the line");
        Assert.False(FieldAbilities.TongueReaches(0, 0, 0, 1, 0, r.Fielding.Catch.StandingHeightFt + 0.1, 5, r), "over the head");
        // The facing is the run when the body runs, the ball when it stands.
        Assert.Equal((0.0, -20.0), FieldAbilities.Facing(0, -20, 0, 0, 0, 10));
        Assert.Equal((0.0, 10.0), FieldAbilities.Facing(0, 0, 0, 0, 0, 10));
        // The snap is out 0.3 s; the recovery 0.8 s from the press.
        var tongue = new GloveTongue();
        tongue.Commit("CF", (0, 0), (0, 1), a.LickSnapSec, a.LickRecoverySec);
        for (var t = 0.0; t < a.LickSnapSec - 1e-9; t += Frame) { Assert.True(tongue.Out("CF")); tongue.Tick(Frame); }
        tongue.Tick(Frame);
        Assert.False(tongue.Out("CF"));
        Assert.True(tongue.Recovering("CF"));
        for (var t = 2 * Frame + a.LickSnapSec; t < a.LickRecoverySec + Frame; t += Frame) tongue.Tick(Frame);
        Assert.False(tongue.Recovering("CF"));
    }

    [Theory]
    [InlineData(2.5, true, true)]    // S-256: stopped short of the ball's line, the pressed tongue takes it in the air
    [InlineData(2.5, false, false)]  // no press: the ball lands out of reach
    [InlineData(4.5, true, false)]   // S-257: past the tongue's 8 ft
    public void S256_ThePressedTongueTakesAFlyAheadOfTheBodyInTheAir(double pastReachFt, bool press, bool caught)
    {
        var (play, took, snapped) = RunTongue("zig", pastReachFt, press);
        Assert.Equal(press, snapped);
        // Caught in the air is the out, and only the tongue could reach it (the ordinary ring is short by pastReachFt).
        Assert.Equal(caught, play.Kind == PlayKind.FlyOut);
        if (caught) Assert.True(took);
    }

    [Fact]
    public void S258_EastOnABodyWithNoTongueSnapsNoTongue()
    {
        // Hex's East is his own verb: no tongue snaps, nothing is taken on one, and no tongue recovery holds him.
        var (play, took, snapped) = RunTongue("hex", 2.5, press: true);
        Assert.False(snapped);
        Assert.False(took);
        Assert.NotEqual(PlayKind.FlyOut, play.Kind);
    }

    /// <summary>
    /// A fly to centre with <paramref name="cf"/> there: the glove stands beyond the landing by its ordinary reach plus
    /// <paramref name="pastReachFt"/>, planted and so facing the ball, and presses East once the ball is on a tongue of its
    /// facing (8 ft), or never.
    /// </summary>
    static (PlayEvent Play, bool Took, bool Snapped) RunTongue(string cf, double pastReachFt, bool press)
    {
        var match = Defending(ParkId.Harbor, cf);
        var r = match.Rules;
        var hit = TutorialContact.Create(match.Park, new TutorialBall(0, 88, 32, 0), r);
        var preview = match.PreviewHit(hit);
        Assert.Equal(cf, preview.Fielder.Id);
        var reach = FieldingResolver.CatchRadiusFt(preview.Fielder, match.Park, r, air: true);
        var landing = Math.Sqrt(preview.LandingX * preview.LandingX + preview.LandingZ * preview.LandingZ);
        var standX = preview.LandingX / landing * (landing + reach + pastReachFt);
        var standZ = preview.LandingZ / landing * (landing + reach + pastReachFt);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Paint, Swing, hit, preview, null, HumanGlove, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        PlayEvent? play = null;
        var pressed = false;
        var snapped = false;
        for (var i = 0; i < 60 * 40 && play is null; i++)
        {
            var dx = standX - live.GloveX;
            var dz = standZ - live.GloveZ;
            var d = Math.Sqrt(dx * dx + dz * dz);
            // Once the ball is down the stick goes dead: the helper collects it and the play ends.
            var pad = i == 0 || live.ElapsedSeconds > preview.HangTimeSec + 0.5 ? LivePadInput.Dead : Toward(dx, dz, d);
            // The ball in the glove on the ground: the throw to second ends the play.
            if (live.HoldsBall && !live.Throwing && live.ElapsedSeconds > preview.HangTimeSec) pad = new LivePadInput(KeysBag: 2, SouthDown: true);
            var aheadFt = Diamond.Dist(live.GloveX, live.GloveZ, live.BallX, live.BallZ);
            if (press && !pressed && !live.HoldsBall && live.ElapsedSeconds > 1 && d <= 0.4
                && aheadFt <= r.Fielding.Abilities.LickReachFt + pastReachFt - 2.4 && live.BallY < 12)
            {
                pad = pad with { EastDown = true };
                pressed = true;
            }
            var result = live.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false, LivePlayCommandSource.Human));
            snapped |= live.Events.Contains(LiveEvent.TongueSnap);
            if (!snapped) Assert.Equal(0, live.TongueRecoveryT);
            play = result.CompletedPlay;
        }
        Assert.NotNull(play);
        var took = live.FactsThisPlay.OfType<ReachBonusTake>().Any(f => f.Ability == FieldAbilityId.LickCatch);
        return (play!, took, snapped);
    }

    // ---------------------------------------------------------------------------------
    // S-259  Canopy Yard's climb wall is the park's
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(ParkId.Canopy, "ashlord", true)]
    [InlineData(ParkId.Canopy, "konga", true)]
    [InlineData(ParkId.Harbor, "ashlord", false)]
    public void S259_AnyFielderClimbsCanopysWallAndRobsATwelveFootHomer(string parkId, string cf, bool robbed)
    {
        var match = Defending(parkId, cf);
        var r = match.Rules;
        var hit = FlightFixtures.OverTheFence(match.Park, 12, 0);
        Assert.InRange(BattedBall.Of(hit, match.Park, r).FenceClearFt, 11, 13);
        var preview = match.PreviewHit(hit);
        Assert.Equal(cf, preview.Fielder.Id);
        Assert.True(FlyCatch.NeedsJump(preview));
        Assert.Equal(parkId == ParkId.Canopy, ParkHazards.ClimbsAt(match.Park, FlyCatch.ChaseTarget(preview, r, match.Park), r));
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Paint, Swing, hit, preview, null, HumanGlove, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 25 && play is null; i++)
        {
            var pad = FlyCatch.JumpWindow(live.ElapsedSeconds, preview.HangTimeSec, r) ? new LivePadInput(WestDown: true) : LivePadInput.Dead;
            play = live.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false, LivePlayCommandSource.Human)).CompletedPlay;
        }
        Assert.NotNull(play);
        Assert.Equal(robbed ? PlayKind.FlyOut : PlayKind.HomeRun, play!.Kind);
        if (robbed) Assert.Equal(DefensiveFeat.Clamber, play.Outcome!.DefensiveFeat);
        // The height is the park's, in data: 28 ft over, a trial number.
        Assert.Equal(28, r.Hazards.Of(HazardType.ClimbWall).RobFt);
    }

    // ---------------------------------------------------------------------------------
    // The lessons
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("T-A-lick-catch")]
    [InlineData("T-A-wall-spring")]
    public void TheAbilityLessonNeedsTheSeatsOwnPressThreeTimes(string id)
    {
        var catalog = TutorialCatalog.Load(Game);
        var run = new TutorialSession(Game, catalog, id);
        run.Begin();
        for (var n = 1; n <= 3; n++)
        {
            DriveLesson(run, LivePlayCommandSource.Human);
            Assert.True(run.Feedback!.Success, $"{id}: {run.Feedback.Detail}; kind {run.LastPlay?.Kind}");
            Assert.Equal(n, run.Successes);
            Assert.Equal(run.Feedback, TutorialSession.Replay(Game, catalog, run.Recording()).Feedback);
            run.Retry();
        }
        var cpu = new TutorialSession(Game, catalog, id);
        cpu.Begin();
        DriveLesson(cpu, LivePlayCommandSource.Cpu);
        Assert.Equal(0, cpu.Successes);
        var demo = new TutorialSession(Game, catalog, id);
        demo.Begin(demonstration: true);
        DriveLesson(demo, LivePlayCommandSource.Cpu);
        Assert.Equal(0, demo.Successes);
    }

    [Fact]
    public void TheRelayPivotLessonWaitsForACarrier()
    {
        var catalog = TutorialCatalog.Load(Game);
        var lesson = catalog.Lesson("T-A-relay-pivot");
        Assert.Equal("planned", lesson.Status);
        Assert.DoesNotContain(Game.Characters.Values, c => c.FieldAbility == FieldAbilityId.RelayPivot);
        Assert.True(catalog.Migration.ContainsKey("ability.relay-pivot"));
    }

    static void DriveLesson(TutorialSession run, LivePlayCommandSource source)
    {
        var pressed = false;
        for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            var r = run.Match.Rules;
            var pad = LivePadInput.Dead;
            if (i > 0 && live.Preview is { } pre)
            {
                (double X, double Z) stand;
                if (run.Lesson.Id == "T-A-wall-spring")
                    stand = FlyCatch.ChaseTarget(pre, r, run.Match.Park);
                else
                {
                    var reach = FieldingResolver.CatchRadiusFt(pre.Fielder, run.Match.Park, r, air: true);
                    var landing = Math.Sqrt(pre.LandingX * pre.LandingX + pre.LandingZ * pre.LandingZ);
                    stand = (pre.LandingX / landing * (landing + reach + 2.5), pre.LandingZ / landing * (landing + reach + 2.5));
                }
                var dx = stand.X - live.GloveX;
                var dz = stand.Z - live.GloveZ;
                var d = Math.Sqrt(dx * dx + dz * dz);
                pad = Toward(dx, dz, d);
                if (run.Lesson.Id == "T-A-wall-spring")
                    pad = pad with { WestDown = FlyCatch.JumpWindow(live.ElapsedSeconds, pre.HangTimeSec, r) };
                else if (!pressed && !live.HoldsBall && d <= 0.4 && live.ElapsedSeconds > 1
                         && Diamond.Dist(live.GloveX, live.GloveZ, live.BallX, live.BallZ) <= r.Fielding.Abilities.LickReachFt && live.BallY < 12)
                {
                    pad = pad with { EastDown = true };
                    pressed = true;
                }
            }
            run.Tick(Frame, pad, source);
        }
    }

    /// <summary>
    /// Full stick toward a spot more than 0.4 ft away; inside it a sliver just over the stick's leave magnitude (0.15), so the
    /// glove stays the player's and stands planted — slower than a walk, it faces the ball.
    /// </summary>
    static LivePadInput Toward(double dx, double dz, double d)
    {
        var len = Math.Max(d, 1e-6);
        var mag = d > 0.4 ? 1 : 0.16;
        return new LivePadInput(StickX: mag * dx / len, StickY: mag * dz / len);
    }

    /// <summary>A home nine with <paramref name="cf"/> in centre field, defending the top half at <paramref name="parkId"/>.</summary>
    static Match Defending(string parkId, string cf)
    {
        var infield = new[] { "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "hex", "moss" }
            .Where(id => id != cf).Take(7).ToList();
        var ids = infield.Take(6).Append(cf).Append(infield[6]).Prepend("rio").ToArray();
        var home = Game.Team("Field", ids[0], ids.Skip(1).ToArray());
        var match = Match.Exhibition(Game, home, PresetTeams.EmberCourt(Game), seed: 7, parkId: parkId);
        Assert.True(match.Top, "the home nine is on defense");
        Assert.Equal(cf, FieldingResolver.Assign(match.Defense, match.Pitcher)["CF"].Id);
        return match;
    }
}
