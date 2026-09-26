using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Arroyo's three (`sable`; spec §13, §8.4): Mirage hides the ball for the middle third of its flight and leaves its path,
/// its crossing and its window alone; Dust Bowl raises a bowl of loose dust where the grounder first lands that slows the
/// fielders inside it; Sand Scoop reaches for the low ball. What the eye sees and how fast a glove runs change; the ball, the
/// bodies and the geometry still decide the play, and nothing is rolled.
/// </summary>
public sealed class SableAbilityTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;

    [Fact]
    public void ArroyoCarriesHisOwnThreeInTheirOwnFamilies()
    {
        var sable = Game.Must("sable");
        Assert.Equal("Arroyo", sable.Name);
        Assert.Equal("mirageball", sable.StarPitch);
        Assert.Equal("sidewinder", sable.StarSwing);
        Assert.Equal(FieldAbilityId.SandScoop, sable.FieldAbility);
        Assert.DoesNotContain(Game.Characters.Values, c => c.Id != "sable"
            && (c.StarPitch == "mirageball" || c.StarSwing == "sidewinder" || c.FieldAbility == FieldAbilityId.SandScoop));
        var pitch = Game.StarSkills.Pitch("mirageball")!;
        var swing = Game.StarSkills.Swing("sidewinder")!;
        Assert.Equal("Mirage", pitch.Name);
        Assert.Equal("Dust Bowl", swing.Name);
        // The middle third, and the ordinary speed: no twin, no path shape of any kind.
        Assert.Equal(new PitchVanish(0.3333, 0.6667), pitch.Vanish);
        Assert.Equal(1.0, pitch.SpeedMul);
        Assert.Null(pitch.Hitch);
        Assert.Null(pitch.Leap);
        Assert.Null(pitch.Loop);
        // An 8-ft bowl (a radius, like a park's slow disc) for 2 s at half speed; the ball's hop is left alone.
        Assert.Equal(new SwingDustBowl(8, 2, 0.5), swing.DustBowl);
        Assert.False(swing.BendsFirstHop);
        Assert.Equal(1, swing.FirstHopBounceMul);
        Assert.Equal(0, swing.FirstHopStallSec);
    }

    // ---------------------------------------------------------------------------------
    // Mirage
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// S-234: the Mirage is the ordinary pitch in everything but sight — the same point at every share of the flight, the same
    /// crossing, speed and timing window — and it cannot be seen exactly from one third to two thirds of the flight.
    /// </summary>
    [Theory]
    [InlineData("sinker", 0.0, 0.0, 0.0)]
    [InlineData("changeup", 0.4, -0.3, 0.5)]
    [InlineData("sinker", -0.5, 0.4, -1.0)]
    public void S234_TheMirageIsThePlainPitchUnseenForTheMiddleThird(string type, double aimX, double aimY, double breakX)
    {
        var rules = Game.Rules;
        var star = new PitchCommand(type, 0.2, true, aimX, aimY, BreakX: breakX);
        var plain = star with { Star = false };
        var vanish = Game.StarSkills.Pitch("mirageball")!.Vanish!;
        Assert.Equal(PitchFlight.Crossing(plain, rules), PitchFlight.Crossing(star, rules, "mirageball"));
        Assert.Equal(1.0, StarSkills.PitchSpeedMul("mirageball", Game.StarSkills));
        Assert.Equal(AtBatResolver.ContactWindowFrames(null, Game.Parks[ParkId.Harbor], false, rules, Game.StarSkills),
            AtBatResolver.ContactWindowFrames("mirageball", Game.Parks[ParkId.Harbor], false, rules, Game.StarSkills));
        var hidden = 0;
        for (var i = 0; i <= 300; i++)
        {
            var u = i / 300.0;
            Assert.Equal(PitchFlight.Point(plain, u, rules), PitchFlight.Point(star, u, rules, "mirageball", skills: Game.StarSkills));
            var seen = PitchFlight.Visible(star, u, "mirageball", Game.StarSkills);
            Assert.Equal(u < vanish.From || u >= vanish.To, seen);
            if (!seen) hidden++;
            // Unstarred, or another star pitch: always seen.
            Assert.True(PitchFlight.Visible(plain, u, "mirageball", Game.StarSkills));
            Assert.True(PitchFlight.Visible(star, u, "prismball", Game.StarSkills));
        }
        // A third of the flight, and back for the whole last third.
        Assert.InRange(hidden / 301.0, 0.33, 0.34);
        Assert.True(PitchFlight.Visible(star, vanish.To, "mirageball", Game.StarSkills));
        Assert.True(PitchFlight.Visible(star, 1, "mirageball", Game.StarSkills));
    }

    /// <summary>
    /// S-235: a hidden ball costs the CPU batter the read it costs a player. When its commit instant falls in the hidden
    /// stretch, it reads the stick's break no further than it had reached when the ball vanished; the same pitch unstarred, or
    /// a commit after the ball is back, is the ordinary read. Fixed: the same input gives the same read every time.
    /// </summary>
    [Theory]
    [InlineData("sinker")]
    [InlineData("changeup")]
    public void S235_TheCpuReadsAHiddenMirageFromWhereItLastSawIt(string type)
    {
        var rules = Game.Rules;
        var match = SableOnTheMound();
        Assert.Equal("sable", match.Pitcher.Id);
        var vanish = Game.StarSkills.Pitch("mirageball")!.Vanish!;
        var star = new PitchCommand(type, 0, true, 0.2, 0, BreakX: 1);
        var plain = star with { Star = false };
        var air = PitchFlight.AirSeconds(match.PitchSpeedMph(star), rules);
        var commit = AtBatMotion.CpuDecisionTime(air, rules);
        var control = match.Pitcher.Stats.Control;
        var hiddenAtCommit = commit / air >= vanish.From && commit / air < vanish.To;
        var seenSec = hiddenAtCommit ? vanish.From * air : commit;
        var expected = Math.Min(1, PitchFlight.BreakReach(control, seenSec, rules));
        Assert.Equal(expected, match.CpuBatter.ReadPitch(star).BreakX, 12);
        Assert.Equal(Math.Min(1, PitchFlight.BreakReach(control, commit, rules)), match.CpuBatter.ReadPitch(plain).BreakX, 12);
        // A client that watched the stick at the commit: a hidden ball caps the watched break at what the vanish instant allowed.
        var watched = match.CpuBatter.ReadPitch(star, breakAtCommit: -0.9).BreakX;
        Assert.Equal(-Math.Min(0.9, PitchFlight.BreakReach(control, seenSec, rules)), watched, 12);
        Assert.Equal(-0.9, match.CpuBatter.ReadPitch(plain, breakAtCommit: -0.9).BreakX, 12);
        // Pure: the same read again.
        Assert.Equal(match.CpuBatter.ReadPitch(star), match.CpuBatter.ReadPitch(star));
    }

    [Fact]
    public void AFastMirageIsHiddenAtTheCpuCommitAndTheReadIsShort()
    {
        // The fastest flight commits inside the middle third, so the fairness rule is live, not a dead branch.
        var rules = Game.Rules;
        var vanish = Game.StarSkills.Pitch("mirageball")!.Vanish!;
        var air = rules.Pitching.Flight.AirMinSec;
        var u = AtBatMotion.CpuDecisionTime(air, rules) / air;
        Assert.InRange(u, vanish.From, vanish.To);
        var match = SableOnTheMound();
        var star = new PitchCommand("sinker", 0, true, 0, 0, BreakX: 1);
        var hidden = match.CpuBatter.ReadPitch(star).BreakX;
        var plain = match.CpuBatter.ReadPitch(star with { Star = false }).BreakX;
        Assert.True(hidden <= plain, $"hidden read {hidden} vs seen {plain}");
    }

    static Match SableOnTheMound()
    {
        var home = Game.Team("Defense", "sable", "pewter", "lace", "frost", "basil", "soot", "vine", "moss", "hex");
        var away = Game.Team("Offense", "vale", "boom", "cinder", "grit", "rio", "nugget", "nico", "gull", "marlow");
        return Match.Exhibition(Game, home, away, 3, 1, parkId: ParkId.Harbor);
    }

    // ---------------------------------------------------------------------------------
    // Dust Bowl
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// S-236: a Dust Bowl grounder raises one bowl where it first meets the ground — 8 ft round, standing 2 s from that landing,
    /// fielders only at ×0.5 — and the ball runs on its own path: the same ball, frame by frame, as the plain swing's.
    /// </summary>
    [Theory]
    [InlineData(12)]
    [InlineData(20)]
    [InlineData(-8)]
    public void S236_TheBowlStandsWhereTheGrounderFirstLandsAndTheBallRunsOn(double spray)
    {
        var bowl = Game.StarSkills.Swing("sidewinder")!.DustBowl!;
        var dust = Run(spray, "sidewinder");
        var plain = Run(spray, null);
        Assert.Empty(plain.Bowls);
        Assert.Empty(dust.Kicks);   // the hop is not bent
        var raised = Assert.Single(dust.Bowls);
        Assert.Equal("sidewinder", raised.SwingId);
        Assert.Equal(bowl.RadiusFt, raised.RadiusFt);
        // It rises at the landing and settles two seconds after the contact (the play clock's 0), like every bend.
        Assert.True(raised.T < bowl.Sec, $"the fixture lands at {raised.T:F2} s");
        Assert.Equal(bowl.Sec, raised.UntilT, 12);
        // At the first landing of the ball's own path.
        var i = BallFlight.LandingIndex(dust.Path!);
        Assert.Equal(SampleEvent.Ground, dust.Path![i].Event);
        Assert.Equal((dust.Path[i].T, dust.Path[i].X, dust.Path[i].Z), (raised.T, raised.X, raised.Z));
        // The disc on the play's slow rail: the bowl's own slow, until the dust settles, and fielders only.
        var disc = Assert.Single(dust.Volumes, v => v.Type == SwingDustBowl.Type);
        Assert.Equal((raised.X, raised.Z, bowl.RadiusFt, bowl.Mul, raised.UntilT, true, 0.0),
            (disc.X, disc.Z, disc.RadiusFt, disc.SlowMul!.Value, disc.UntilT, disc.FieldersOnly, disc.SlowSec));
        // The ball is the plain ball until a glove takes it: the bowl leaves the path alone.
        var n = Math.Min(dust.Balls.Count, plain.Balls.Count);
        for (var k = 0; k < n && !dust.Balls[k].Held && !plain.Balls[k].Held; k++)
        {
            Assert.Equal(plain.Balls[k].X, dust.Balls[k].X, 9);
            Assert.Equal(plain.Balls[k].Z, dust.Balls[k].Z, 9);
        }
        Assert.NotNull(dust.Play);
        // No runner is ever slowed by it.
        Assert.False(dust.RunnerSlowed);
    }

    /// <summary>
    /// S-237: a fielder standing in the bowl runs at half his step while the dust stands and at his own step once it settles;
    /// a runner in it is never slowed; a park's disc and the bowl together slow by the stronger, never both; a route goes
    /// round the park's discs only. And a Dust Bowl ball gloved before it lands raises nothing.
    /// </summary>
    [Fact]
    public void S237_TheBowlSlowsTheFielderInItForItsSecondsAndNothingElse()
    {
        var rules = Game.Rules;
        var bowl = Game.StarSkills.Swing("sidewinder")!.DustBowl!;
        var park = new StatusVolume(0, "tar", 100, 100, 10, 3);
        var slows = new BodySlows();
        slows.Begin([park]);
        var disc = bowl.Volume(20, 60);
        Assert.Equal(bowl.Sec, disc.UntilT);
        Assert.True(bowl.RaisesAt(0.4) && bowl.RaisesAt(1.99) && !bowl.RaisesAt(2.0) && !bowl.RaisesAt(2.4));
        slows.Add(disc);
        Assert.Equal([park], slows.ParkVolumes);
        Assert.Equal(2, slows.Volumes.Count);
        var runner = new Runner(Game.Must("rio"), 0, rules);
        // Before the landing's frame reads it, nobody is inside; inside, the fielder is at half and the runner is not.
        slows.Read("SS", 20, 64, 0.5, immune: false);
        slows.Read(runner, 20, 64, 0.5, immune: false);
        Assert.Equal(bowl.Mul, slows.Mul("SS", rules));
        Assert.Equal(1.0, slows.Mul(runner, rules));
        Assert.True(slows.Slowed("SS"));
        Assert.False(slows.Slowed(runner));
        // Out of the bowl: his own step at once (no time after leaving).
        slows.Read("SS", 20, 60 + bowl.RadiusFt + 0.5, 0.6, immune: false);
        Assert.Equal(1.0, slows.Mul("SS", rules));
        // Back in after the dust settles: nothing.
        slows.Read("SS", 20, 60, disc.UntilT, immune: false);
        Assert.Equal(1.0, slows.Mul("SS", rules));
        // A park disc and a bowl on the same spot: the stronger slow, never both stacked.
        var both = new BodySlows();
        both.Begin([new StatusVolume(0, "tar", 20, 60, 10, 3)]);
        both.Add(disc);
        both.Read("2B", 20, 60, 0.5, immune: false);
        Assert.Equal(Math.Min(rules.Fielding.Chase.FrozenMul, bowl.Mul), both.Mul("2B", rules));
        // A Burrow body touches nothing, bowl or park.
        both.Read("3B", 20, 60, 0.5, immune: true);
        Assert.Equal(1.0, both.Mul("3B", rules));

        // Up the middle the pitcher gloves the liner before it lands: a catch, and no bowl.
        var run = Run(-4, "sidewinder");
        Assert.Empty(run.Bowls);
        Assert.DoesNotContain(run.Volumes, v => v.Type == SwingDustBowl.Type);
    }

    [Fact]
    public void AGloveIsSlowedOnlyInsideTheBowlWhileTheDustStands()
    {
        // A soft grounder to the right side: the chaser runs through the landing spot while the dust stands.
        var run = Run(8, "sidewinder", exit: 60);
        var raised = Assert.Single(run.Bowls);
        Assert.True(run.SlowedInBowl.Count > 0, "a fielder crossed the standing bowl");
        foreach (var (t, x, z) in run.SlowedInBowl)
        {
            Assert.InRange(t, raised.T, raised.UntilT);
            Assert.True(Diamond.Dist(x, z, raised.X, raised.Z) <= raised.RadiusFt + 1e-9);
        }
    }

    /// <summary>
    /// The two-second rule: a Dust Bowl ball whose first landing comes 2 s or more after the contact (a gapper that falls in
    /// at 3.2 s) raises no bowl at all.
    /// </summary>
    [Fact]
    public void ALandingPastTwoSecondsRaisesNoBowl()
    {
        var run = Run(-40, "sidewinder", exit: 100, launch: 14);
        var i = BallFlight.LandingIndex(run.Path!);
        Assert.Equal(SampleEvent.Ground, run.Path![i].Event);
        Assert.True(run.Path[i].T >= Game.StarSkills.Swing("sidewinder")!.DustBowl!.Sec, $"the fixture lands at {run.Path[i].T:F2} s");
        Assert.Empty(run.Bowls);
        Assert.DoesNotContain(run.Volumes, v => v.Type == SwingDustBowl.Type);
    }

    sealed record RunResult(List<FirstHopKicked> Kicks, List<DustBowlRaised> Bowls, IReadOnlyList<Sample>? Path,
        List<(double T, double X, double Z, bool Held)> Balls, List<StatusVolume> Volumes, List<(double T, double X, double Z)> SlowedInBowl,
        bool RunnerSlowed, PlayEvent? Play);

    /// <summary>A hard grounder off Arroyo's bat with the named star swing (or none), on CPU gloves at Harbor.</summary>
    static RunResult Run(double spray, string? swing, double exit = 92, double launch = 4)
    {
        var home = Game.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "soot", "vine", "moss", "hex");
        var away = Game.Team("Offense", "sable", "boom", "cinder", "grit", "rio", "nugget", "nico", "gull", "marlow");
        var match = Match.Exhibition(Game, home, away, 3, 1, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Hit(match.Park, exit, launch, spray, rules: match.Rules) with { StarSwingUsed = swing };
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0,
            LivePlayCommandSource.Cpu)).Snapshot.Active);
        var kicks = new List<FirstHopKicked>();
        var bowls = new List<DustBowlRaised>();
        var balls = new List<(double, double, double, bool)>();
        var volumes = new List<StatusVolume>();
        var slowedInBowl = new List<(double, double, double)>();
        var runnerSlowed = false;
        IReadOnlyList<Sample>? path = null;
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 12 && play is null; i++)
        {
            play = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
            if (play is not null) break;   // the completing frame resets the field
            balls.Add((live.ElapsedSeconds, live.BallX, live.BallZ, live.HoldsBall));
            path = live.Path;
            kicks.AddRange(live.Facts.OfType<FirstHopKicked>());
            bowls.AddRange(live.Facts.OfType<DustBowlRaised>());
            foreach (var v in live.StatusVolumes)
                if (!volumes.Contains(v)) volumes.Add(v);
            foreach (var r in live.Runners)
                runnerSlowed |= live.IsSlowed(r);
            if (bowls.Count > 0)
                foreach (var (pos, at) in live.Fielders)
                    if (live.IsSlowed(pos)) slowedInBowl.Add((live.ElapsedSeconds, at.X, at.Z));
        }
        return new RunResult(kicks, bowls, path, balls, volumes, slowedInBowl, runnerSlowed, play);
    }

    // ---------------------------------------------------------------------------------
    // Sand Scoop
    // ---------------------------------------------------------------------------------

    [Fact]
    public void SandScoopReachesOnlyForTheLowBallAndThatScoopNeverBobbles()
    {
        var rules = Game.Rules;
        var a = rules.Fielding.Abilities;
        var sable = Game.Must("sable");
        Assert.Equal(8, a.SandScoopFt);
        Assert.Equal(1.0, a.SandScoopMaxFt);
        Assert.True(a.SandScoopFt < a.DiveGroundRangeFt, "half Dive's reach, for the low ball only");
        Assert.Equal(a.SandScoopFt, FieldAbilities.GroundRangeBonus(sable, rules, 0));
        Assert.Equal(a.SandScoopFt, FieldAbilities.GroundRangeBonus(sable, rules, a.SandScoopMaxFt));
        Assert.Equal(0, FieldAbilities.GroundRangeBonus(sable, rules, a.SandScoopMaxFt + 0.01));
        Assert.True(FieldAbilities.SureScoop(sable, rules, 0.4));
        Assert.False(FieldAbilities.SureScoop(sable, rules, 1.4));
        Assert.False(FieldAbilities.SureScoop(Game.Must("soot"), rules, 0.4));
        Assert.Equal(0, FieldAbilities.CatchBonus(sable, rules));   // nothing in the air
        Assert.Equal(0, FieldAbilities.FlyRangeBonus(sable, rules));
    }

    // ---------------------------------------------------------------------------------
    // The rows are read strictly
    // ---------------------------------------------------------------------------------

    [Fact]
    public void ALateVanishABowlOnAPitchAVanishOnASwingOrABowlOffAFlyIsRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["mirageball"]!["vanish"]!["to"] = 0.9;
            json["pitches"]!["fastball"]!["dustBowl"] = new JsonObject { ["radiusFt"] = 8, ["sec"] = 2, ["mul"] = 0.5 };
            json["swings"]!["line"]!["vanish"] = new JsonObject { ["from"] = 0.3, ["to"] = 0.6 };
            json["swings"]!["sidewinder"]!["dustBowl"]!["sec"] = 3;
            json["swings"]!["fly"]!["dustBowl"] = new JsonObject { ["radiusFt"] = 8, ["sec"] = 2, ["mul"] = 0.5 };
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("star pitch 'mirageball' vanish needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'fastball' cannot carry a dust bowl", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'line' cannot carry a vanish", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'sidewinder' dustBowl needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'fly' raises a dust bowl at its first landing, so it must name a grounder's launchDeg", StringComparison.Ordinal));
    }

    [Fact]
    public void TheRetiredTwinAndKickAreRefusedByName()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["mirageball"]!["twin"] = new JsonObject { ["offsetFt"] = 1.2, ["fadeFrom"] = 0.25, ["fadeTo"] = 0.45 };
            json["swings"]!["sidewinder"]!["firstHopKickDeg"] = 20;
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("pitches.mirageball.twin", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("swings.sidewinder.firstHopKickDeg", StringComparison.Ordinal));
    }
}
