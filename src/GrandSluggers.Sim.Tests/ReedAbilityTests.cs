using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Reed's three (#1151, WD-15 A, WD-22; spec §13, §8.4): Leapfrog's pace, Pond Skip's first hop and Lily Leap's high jump.
/// The pitch keeps its path, crossing and arrival; the hop is the ground physics; the leap is the normal jump and the catch
/// is the glove meeting the ball — pressed by the player, never automatic.
/// </summary>
public sealed class ReedAbilityTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    [Fact]
    public void ReedCarriesHisOwnThree()
    {
        var reed = Game.Must("reed");
        Assert.Equal("leapfrog", reed.StarPitch);
        Assert.Equal("pond-skip", reed.StarSwing);
        Assert.Equal(FieldAbilityId.LilyLeap, reed.FieldAbility);
        Assert.DoesNotContain(Game.Characters.Values, c => c.Id != "reed"
            && (c.StarPitch == "leapfrog" || c.StarSwing == "pond-skip" || c.FieldAbility == FieldAbilityId.LilyLeap));
    }

    // ---------------------------------------------------------------------------------
    // Leapfrog
    // ---------------------------------------------------------------------------------

    [Fact]
    public void LeapfrogHangsThenLeapsAlongTheSamePathToTheSameCrossing()
    {
        var rules = Game.Rules;
        var leap = Game.StarSkills.Pitch("leapfrog")!.Leap!;
        var star = new PitchCommand("changeup", 0.2, true, 0.3, 0.1);
        var plain = star with { Star = false };
        Assert.Equal(PitchFlight.Crossing(plain, rules), PitchFlight.Crossing(star, rules, "leapfrog"));
        Assert.Equal(PitchFlight.Point(plain, 1, rules), PitchFlight.Point(star, 1, rules, "leapfrog", skills: Game.StarSkills));
        // The ball is where the plain pitch was at the leap's progress: the same path, a different clock.
        var last = 0.0;
        for (var u = 0.0; u <= 1.0001; u += 0.01)
        {
            var progress = leap.Progress(u);
            Assert.True(progress >= last - 1e-12, "the ball never goes backwards along its path");
            last = progress;
            Assert.Equal(PitchFlight.Point(plain, progress, rules), PitchFlight.Point(star, u, rules, "leapfrog", skills: Game.StarSkills));
        }
        Assert.Equal(leap.At, leap.Progress(leap.At), 12);
        // The hold: a quarter of the flight covers a fifth of the ground it would; the rest leaps.
        var held = leap.Progress(leap.At + leap.Hold) - leap.At;
        Assert.Equal(leap.Hold * leap.HoldPace, held, 12);
        var after = (1 - leap.Progress(leap.At + leap.Hold)) / (1 - leap.At - leap.Hold);
        Assert.True(after > 1.1, "after the hang the ball outruns the plain pitch");
        // The arrival instant is the plain pitch's: the timing window is judged against the same clock (PH-16-R18).
        Assert.Equal(AtBatResolver.ContactWindowFrames(null, Game.Parks[ParkId.Harbor], false, rules, Game.StarSkills),
            AtBatResolver.ContactWindowFrames("leapfrog", Game.Parks[ParkId.Harbor], false, rules, Game.StarSkills));
    }

    // ---------------------------------------------------------------------------------
    // Pond Skip
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(24)]
    public void PondSkipSpringsHighOffItsFirstHopAndTheGlovesDecide(double spray)
    {
        var skip = Run(spray, "pond-skip");
        var plain = Run(spray, "ground");
        Assert.Empty(plain.Kicks);
        var hop = Assert.Single(skip.Kicks);
        Assert.Equal(0, hop.TurnDeg);
        Assert.Equal(2.2, hop.BounceMul);
        // Before the hop the two balls are one ball; off it the skip climbs higher than the plain hop ever does.
        var at = skip.Balls.FindIndex(b => b.T >= hop.T);
        Assert.Equal(plain.Balls[at - 1].Y, skip.Balls[at - 1].Y, 9);
        var skipTop = skip.Balls.Skip(at).Take(40).Max(b => b.Y);
        var plainTop = plain.Balls.Skip(at).Take(40).Max(b => b.Y);
        Assert.True(skipTop > plainTop * 2, $"spray {spray}: the skip rises {skipTop:F1} ft against the plain hop's {plainTop:F1}");
        Assert.NotNull(skip.Play);
    }

    static (List<FirstHopKicked> Kicks, List<(double T, double Y)> Balls, PlayEvent? Play) Run(double spray, string swing)
    {
        var home = Game.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "soot", "vine", "moss", "hex");
        var away = Game.Team("Offense", "reed", "boom", "cinder", "grit", "rio", "nugget", "nico", "gull", "marlow");
        var match = Match.Exhibition(Game, home, away, 3, 1, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Hit(match.Park, 88, 0, spray, rules: match.Rules) with { StarSwingUsed = swing };
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0,
            LivePlayCommandSource.Cpu)).Snapshot.Active);
        var kicks = new List<FirstHopKicked>();
        var balls = new List<(double T, double Y)>();
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 12 && play is null; i++)
        {
            play = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
            if (play is not null) break;
            balls.Add((live.ElapsedSeconds, live.BallY));
            kicks.AddRange(live.Facts.OfType<FirstHopKicked>());
        }
        return (kicks, balls, play);
    }

    // ---------------------------------------------------------------------------------
    // Lily Leap
    // ---------------------------------------------------------------------------------

    [Fact]
    public void LilyLeapIsTheNormalJumpWithAHigherRiseForItsHolderOnly()
    {
        var rules = Game.Rules;
        Assert.Equal(4.5, rules.Fielding.Abilities.LilyLeapRiseFt);
        Assert.Equal(rules.Fielding.Abilities.LilyLeapRiseFt, FieldAbilities.JumpRiseFt(Game.Must("reed"), rules));
        Assert.Equal(rules.Fielding.Catch.JumpRiseFt, FieldAbilities.JumpRiseFt(Game.Must("soot"), rules));
        Assert.Equal(0, FieldAbilities.CatchBonus(Game.Must("reed"), rules));   // no wider ring: the reach is up, and only in the air
    }

    /// <summary>
    /// A liner over the shortstop's head (90 mph, 12°, 9.5 ft at the bag-side spot): Reed's leap takes it and the fact says
    /// only the leap could; Soot's ordinary jump at the same press cannot. A dead stick with no press catches nothing, and a
    /// 100-mph liner at 12.6 ft is over even Reed's leap.
    /// </summary>
    [Theory]
    [InlineData("reed", 90, true, true)]
    [InlineData("soot", 90, true, false)]
    [InlineData("reed", 90, false, false)]
    [InlineData("reed", 100, true, false)]
    public void ALinerOverTheShortstopsHeadIsReedsOnlyWithAPressedLeap(string shortstop, double exit, bool press, bool caught)
    {
        var home = Game.Team("Defense", "vale", "pewter", "lace", "frost", "basil", shortstop, "vine", "moss", "hex");
        var away = Game.Team("Offense", "rio", "boom", "cinder", "grit", "zig", "nugget", "nico", "gull", "marlow");
        var match = Match.Exhibition(Game, home, away, 3, 1, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Hit(match.Park, exit, 12, -20, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, HumanGlove, 0,
            LivePlayCommandSource.Human)).Snapshot.Active);
        // Stand on the shortstop's spot, where the liner passes overhead, with a manual sliver of stick, and leap 0.30 s before it arrives.
        var spot = DiamondGeometry.Of(match.Rules).Positions["SS"];
        var (over, overT) = (double.MaxValue, 0.0);
        foreach (var sample in preview.Ball!.Samples)
            if (Diamond.Dist(sample.X, sample.Z, spot.X, spot.Z) is var sd && sd < over) (over, overT) = (sd, sample.T);
        PlayEvent? play = null;
        var pressed = false;
        // Until a catch ends the play, or the ball is well past the spot: the leap had its chance.
        for (var i = 0; i < 60 * 12 && play is null && live.ElapsedSeconds < overT + 0.6; i++)
        {
            var dx = spot.X - live.GloveX;
            var dz = spot.Z - live.GloveZ;
            var d = Math.Sqrt(dx * dx + dz * dz);
            var mag = d > 1 ? 1.0 : 0.21;
            var pad = live.HoldsBall ? LivePadInput.Dead
                : new LivePadInput(StickX: dx / Math.Max(1e-6, d) * mag, StickY: dz / Math.Max(1e-6, d) * mag);
            if (!live.HoldsBall && press && !pressed && live.ElapsedSeconds >= overT - 0.30)
            {
                pad = pad with { WestDown = true };
                pressed = true;
            }
            play = live.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false, LivePlayCommandSource.Human)).CompletedPlay;
        }
        // Read the completed play: a catch with nobody on completes on its own frame, and that frame resets the field.
        var took = play is { Kind: PlayKind.FlyOut } && play.Outcome?.DefensiveFeat == DefensiveFeat.Jump && play.Fielder?.Id == shortstop;
        var leapFact = live.FactsThisPlay.OfType<ReachBonusTake>().Any(f => f.Ability == FieldAbilityId.LilyLeap);
        Assert.Equal(caught, took);
        Assert.Equal(caught, leapFact);
    }

    /// <summary>The lesson: a human leap three times, each a Lily Leap reach; the CPU and a demonstration earn nothing.</summary>
    [Fact]
    public void TheLilyLeapLessonNeedsTheSeatsOwnLeapThreeTimes()
    {
        var catalog = TutorialCatalog.Load(Game);
        var run = new TutorialSession(Game, catalog, "T-A-lily-leap");
        run.Begin();
        for (var n = 1; n <= 3; n++)
        {
            Leap(run, LivePlayCommandSource.Human);
            Assert.True(run.Feedback!.Success, run.Feedback.Detail);
            Assert.Equal("ability-reach-lily-leap", run.Feedback.Code);
            Assert.Equal(n, run.Successes);
            Assert.Equal(run.Feedback, TutorialSession.Replay(Game, catalog, run.Recording()).Feedback);
            run.Retry();
        }
        var cpu = new TutorialSession(Game, catalog, "T-A-lily-leap");
        cpu.Begin();
        Leap(cpu, LivePlayCommandSource.Cpu);
        Assert.Equal(0, cpu.Successes);
        var demo = new TutorialSession(Game, catalog, "T-A-lily-leap");
        demo.Begin(demonstration: true);
        Leap(demo, LivePlayCommandSource.Cpu);
        Assert.Equal(0, demo.Successes);
    }

    static void Leap(TutorialSession run, LivePlayCommandSource source)
    {
        var spot = DiamondGeometry.Of(run.Match.Rules).Positions["SS"];
        double? overT = null;
        var pressed = false;
        for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            var pad = LivePadInput.Dead;
            if (live.Preview?.Ball is { } ball && live.Active)
            {
                overT ??= ball.Samples.MinBy(s => Diamond.Dist(s.X, s.Z, spot.X, spot.Z)).T;
                var dx = spot.X - live.GloveX;
                var dz = spot.Z - live.GloveZ;
                var d = Math.Sqrt(dx * dx + dz * dz);
                var mag = d > 1 ? 1.0 : 0.21;
                pad = new LivePadInput(StickX: dx / Math.Max(1e-6, d) * mag, StickY: dz / Math.Max(1e-6, d) * mag);
                if (!pressed && live.ElapsedSeconds >= overT - 0.30) { pad = pad with { WestDown = true }; pressed = true; }
            }
            run.Tick(Frame, pad, source);
        }
    }

    // ---------------------------------------------------------------------------------
    // The rows are read strictly
    // ---------------------------------------------------------------------------------

    [Fact]
    public void ALeapThatNeverEndsOrABounceOnAPitchOrAWildBounceIsRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["leapfrog"]!["leap"]!["hold"] = 0.7;
            json["pitches"]!["fastball"]!["firstHopBounceMul"] = 2;
            json["swings"]!["pond-skip"]!["firstHopBounceMul"] = 5;
            json["swings"]!["line"]!["leap"] = new JsonObject { ["at"] = 0.3, ["hold"] = 0.2, ["holdPace"] = 0.1 };
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("star pitch 'leapfrog' leap needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'fastball' cannot carry firstHopBounceMul", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'pond-skip' firstHopBounceMul must be", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'line' cannot carry a leap", StringComparison.Ordinal));
    }
}
