using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Elder Fenn's two (spec §13). Undertow (<c>fogball</c>) is slow, and a fair ball put in play off it washes a ring of radius
/// 12 ft out on home for 2 s from contact: the batter-runner's every step inside it is at 0.8 of his speed, and nobody else's
/// is touched. Driftwood Reach (<c>staff-swing</c>) has a contact oval 1.4 times as tall for its own swing only: the width
/// along the barrel and the timing window are the ordinary swing's, so a wide pitch or a late bat is still a miss. Nothing
/// is rolled; the infield-chaos flag is gone from the row.
/// </summary>
public sealed class FennAbilityTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;

    [Fact]
    public void FennCarriesUndertowAndDriftwoodReachUnderTheirOldIds()
    {
        var fenn = Game.Must("fenn");
        Assert.Equal("fogball", fenn.StarPitch);
        Assert.Equal("staff-swing", fenn.StarSwing);
        var pitch = Game.StarSkills.Pitch("fogball")!;
        var swing = Game.StarSkills.Swing("staff-swing")!;
        Assert.Equal("Undertow", pitch.Name);
        Assert.Equal("Driftwood Reach", swing.Name);
        Assert.Equal(0.82, pitch.SpeedMul);
        Assert.Equal(new PitchUndertow(12, 2, 0.8), pitch.Undertow);
        Assert.Equal(1.08, swing.ExitVeloMul);
        Assert.Equal(1.4, swing.OvalHeightMul);
        Assert.False(swing.InfieldChaos);   // the warp roll's flag is gone: the bend is the oval, spent at the plate
        Assert.Equal(1.0, swing.PerfectRingMul);
        Assert.Null(pitch.Rise);
        Assert.DoesNotContain(Game.Characters.Values, c => c.Id != "fenn" && (c.StarPitch == "fogball" || c.StarSwing == "staff-swing"));
    }

    // ---------------------------------------------------------------------------------
    // S-230  Undertow, live: the batter-runner inside the ring runs at 0.8, nobody else is touched
    // ---------------------------------------------------------------------------------

    sealed record Run(Match Match, LivePlaySystem Live, Runner Batter,
        List<(double T, double X, double Z, double Velocity, bool Slowed)> Steps, bool AnyFielderSlowed, PlayTrace Trace,
        IReadOnlyList<StatusVolume> Volumes, IReadOnlyList<BodySlowed> Touches);

    /// <summary>A CPU live ball from contact to Time (or 12 s), with the batter-runner recorded each frame.</summary>
    static Run Play(AtBatResult hit, double dt = Frame)
    {
        var match = new Match(Game, PresetTeams.EmberCourt(Game), PresetTeams.SparkAllStars(Game), Game.MustPark(ParkId.Harbor), seed: 1);
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        live.Recording = true;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview,
            match.ResolveFielding(hit, preview), LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        var batter = live.Runners.Single(r => r.IsBatter);
        var volumes = live.StatusVolumes;
        var touches = new List<BodySlowed>();
        var steps = new List<(double, double, double, double, bool)>();
        var fielderSlowed = false;
        PlayEvent? done = null;
        for (var i = 0; i < 12 / dt && done is null; i++)
        {
            // Where he stood when the frame's read was taken, then the frame.
            var (x, z) = batter.Position;
            var t = live.ElapsedSeconds;
            done = live.Apply(LivePlayCommand.Tick(dt, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
            if (done is not null) break;
            steps.Add((t, x, z, batter.Velocity, live.IsSlowed(batter)));
            touches.AddRange(live.Slows);
            fielderSlowed |= live.Fielders.Keys.Any(live.IsSlowed);
        }
        return new Run(match, live, batter, steps, fielderSlowed, live.TakeTrace(done), volumes, touches);
    }

    static AtBatResult Grounder(string? starPitch) =>
        FlightFixtures.Hit(Game.MustPark(ParkId.Harbor), 80, -12, -30) with { StarPitchUsed = starPitch };

    [Theory]
    [InlineData(Frame)]
    [InlineData(0.021)]
    public void S230_TheBatterRunnerRunsAtFourFifthsInsideTheRingAndHisOwnSpeedOutside(double dt)
    {
        var p = Play(Grounder("fogball"), dt);
        var who = p.Batter.Who;
        var speed = RunnerSystem.SpeedFtPerSec(who, p.Match.Rules, 0);
        var ring = Game.StarSkills.Pitch("fogball")!.Undertow!;
        var running = p.Steps.Where(s => s.Velocity > 0).ToArray();
        Assert.NotEmpty(running);
        foreach (var s in running)
        {
            // The read is taken where the last frame left him, on the play clock that starts at contact.
            var inside = s.T < ring.Sec && Diamond.Dist(0, 0, s.X, s.Z) <= ring.RadiusFt;
            Assert.True(inside == s.Slowed, $"t {s.T:0.000}: at {Diamond.Dist(0, 0, s.X, s.Z):0.00} ft slowed {s.Slowed}");
            Assert.Equal(inside ? speed * ring.RunnerMul : speed, s.Velocity, 9);
        }
        Assert.Contains(running, s => s.Slowed);
        Assert.Contains(running, s => !s.Slowed);
        // One touch, the batter-runner's, typed as the undertow and made by no park instance; no fielder is ever slowed.
        var touch = Assert.Single(p.Touches);
        Assert.True(touch.IsRunner);
        Assert.Same(who, touch.Who);
        Assert.Equal((StatusVolume.StarHazard, PitchUndertow.Type), (touch.Hazard, touch.Type));
        Assert.False(p.AnyFielderSlowed);
        var mark = Assert.Single(p.Trace.Marks!, m => m.Kind == PlayTraceMarkKind.BodySlowed);
        Assert.Equal(who.Id, mark.Runner!.Id);
        // The ring is on the play's volumes for presentation, centred on home, live for 2 s.
        var disc = Assert.Single(p.Volumes);
        Assert.Equal((0.0, 0.0, 12.0, 2.0, true), (disc.X, disc.Z, disc.RadiusFt, disc.UntilT, disc.BatterRunnerOnly));
    }

    [Fact]
    public void S230_TheSameBallOffAnOrdinaryPitchOrAnotherStarPitchHasNoRing()
    {
        foreach (var star in new string?[] { null, "heatball" })
        {
            var p = Play(Grounder(star));
            Assert.Empty(p.Touches);
            Assert.All(p.Steps, s => Assert.False(s.Slowed));
            Assert.Empty(p.Volumes);
        }
    }

    [Fact]
    public void S230_TheRingCostsTheBatterRunnerTimeToFirst()
    {
        // The same grounder, the same fielders: off Undertow he reaches his first 30 ft later than off the ordinary pitch,
        // by about the ring's extra quarter-step over the feet he runs in it, and no more.
        var plain = Play(Grounder(null));
        var tow = Play(Grounder("fogball"));
        static double At(Run r, double ft) => r.Steps.First(s => Diamond.Dist(0, 0, s.X, s.Z) >= ft).T;
        var lost = At(tow, 30) - At(plain, 30);
        var speed = RunnerSystem.SpeedFtPerSec(plain.Batter.Who, plain.Match.Rules, 0);
        Assert.True(lost > 0, $"lost {lost}");
        Assert.True(lost <= 12 * (1 / 0.8 - 1) / speed + 2 * Frame, $"lost {lost} s over at most 12 ft in the ring");
    }

    // ---------------------------------------------------------------------------------
    // S-231  The ring's rules: 2 s from contact, the batter-runner alone, fair balls alone, never stacked
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S231_TheRingIsGoneAtTwoSecondsAndTouchesOnlyTheBatterRunner()
    {
        var rules = Game.Rules;
        var ring = Game.StarSkills.Pitch("fogball")!.Undertow!.Volume();
        var slows = new BodySlows();
        slows.Begin([ring]);
        var fenn = Game.Must("fenn");
        var batter = Runner.BatterRunner(fenn, 2.5, 0, rules);
        var onFirst = new Runner(Game.Must("rio"), 1, rules);
        // In the ring at 1.99 s: the batter-runner at 0.8; the catcher and a runner standing in the same spot are untouched.
        Assert.Single(slows.Read(batter, 0, 6, 1.99, immune: false));
        Assert.Equal(0.8, slows.Mul(batter, rules));
        Assert.Empty(slows.Read(onFirst, 0, 6, 1.99, immune: false));
        Assert.Equal(1.0, slows.Mul(onFirst, rules));
        Assert.Empty(slows.Read("C", 0, 6, 1.99, immune: false));
        Assert.False(slows.Slowed("C"));
        Assert.Equal(1.0, slows.Mul("C", rules));
        // On the rim is inside; a step past it is not, and nothing lingers after he leaves.
        var rim = new BodySlows();
        rim.Begin([ring]);
        var runner = Runner.BatterRunner(fenn, 2.5, 0, rules);
        rim.Read(runner, 12, 0, 1.0, immune: false);
        Assert.True(rim.Slowed(runner));
        rim.Read(runner, 12.01, 0, 1.01, immune: false);
        Assert.False(rim.Slowed(runner));
        Assert.Equal(1.0, rim.Mul(runner, rules));
        // Back in at 2.0 s: the ring is gone.
        Assert.Empty(slows.Read(batter, 0, 6, 2.0, immune: false));
        Assert.False(slows.Slowed(batter));
        // The fielders' route never goes around it: it slows none of them.
        Assert.Empty(slows.FielderVolumes);
        Assert.Single(slows.Volumes);
    }

    [Fact]
    public void S231_ARingInAParksVolumeIsTheOneStrongerSlowNeverTwoStacked()
    {
        var rules = Game.Rules;
        var ring = Game.StarSkills.Pitch("fogball")!.Undertow!.Volume();
        var lava = new StatusVolume(0, HazardType.LavaPit, 0, 10, 4, 3.0);
        var slows = new BodySlows();
        slows.Begin([lava, ring]);
        var batter = Runner.BatterRunner(Game.Must("fenn"), 2.5, 0, rules);
        slows.Read(batter, 0, 8, 0.5, immune: false);
        Assert.Equal(rules.Fielding.Chase.FrozenMul, slows.Mul(batter, rules));
        // Out of the lava but still in the ring: the lava's time holds him at frozenMul, the stronger one.
        slows.Read(batter, 0, 11.5 - 6, 0.6, immune: false);
        Assert.Equal(rules.Fielding.Chase.FrozenMul, slows.Mul(batter, rules));
        // The park's own disc still slows a fielder exactly as before, and the route still reads it.
        slows.Read("C", 0, 10, 0.5, immune: false);
        Assert.Equal(BodySlows.Mul(true, rules), slows.Mul("C", rules));
        Assert.Equal([lava], slows.FielderVolumes);
    }

    [Fact]
    public void S231_AFoulBallOffUndertowWashesNoRing()
    {
        var foul = Grounder("fogball") with { Foul = true };
        var match = new Match(Game, PresetTeams.EmberCourt(Game), PresetTeams.SparkAllStars(Game), Game.MustPark(ParkId.Harbor), seed: 1);
        var preview = match.PreviewHit(foul);
        var live = match.LivePlay;
        live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, foul, preview,
            match.ResolveFielding(foul, preview), LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu));
        Assert.DoesNotContain(live.StatusVolumes, v => v.Type == PitchUndertow.Type);
    }

    [Fact]
    public void S231_UndertowIsSlowAndJudgedInTheOrdinaryWindowAtItsOwnCrossing()
    {
        var rules = Game.Rules;
        var harbor = Game.Parks[ParkId.Harbor];
        Assert.Equal(AtBatResolver.ContactWindowFrames(null, harbor, false, rules, Game.StarSkills),
            AtBatResolver.ContactWindowFrames("fogball", harbor, false, rules, Game.StarSkills));
        Assert.Equal(0.82, StarSkills.PitchSpeedMul("fogball", Game.StarSkills));
        // The path is the plain pitch's: the undertow bends nothing in the air.
        var star = new PitchCommand(PitchFamily.Changeup, 0.3, true, 0.2, -0.3, 0, Zone: StrikeZoneGeometry.Reference);
        var plain = star with { Star = false };
        for (var i = 0; i <= 20; i++)
            Assert.Equal(PitchFlight.Point(plain, i / 20.0, rules), PitchFlight.Point(star, i / 20.0, rules, "fogball", skills: Game.StarSkills));
    }

    // ---------------------------------------------------------------------------------
    // S-232  Driftwood Reach: the oval is 1.4 times as tall for this swing; the barrel and the window are not
    // ---------------------------------------------------------------------------------

    [Theory]
    // Height over the cursor centre as a share of the zone's half height, ordinary vs Driftwood Reach, on the centre line.
    [InlineData(0.00, ContactQuality.Perfect, ContactQuality.Perfect)]
    [InlineData(0.50, ContactQuality.Nice, ContactQuality.Perfect)]
    [InlineData(0.90, ContactQuality.Nice, ContactQuality.Nice)]
    [InlineData(1.20, ContactQuality.Sour, ContactQuality.Nice)]
    [InlineData(1.38, ContactQuality.Miss, ContactQuality.Nice)]
    [InlineData(-1.38, ContactQuality.Miss, ContactQuality.Nice)]
    [InlineData(1.60, ContactQuality.Miss, ContactQuality.Sour)]
    [InlineData(1.90, ContactQuality.Miss, ContactQuality.Miss)]
    public void S232_DriftwoodReachsOvalIsFourTenthsTallerAndNoOtherSwingsIs(double h, ContactQuality plain, ContactQuality reach)
    {
        Assert.Equal(plain, Resolve(0, h, star: false).Quality);
        Assert.Equal(reach, Resolve(0, h, star: true).Quality);
        // Another captain's star swing keeps the ordinary oval.
        Assert.Equal(plain, Resolve(0, h, star: true, batterId: "zig").Quality);
        // A bunt keeps it too.
        Assert.Equal(Resolve(0, h, star: false, bunt: true).Quality, Resolve(0, h, star: true, bunt: true).Quality);
    }

    [Theory]
    // Distance along the barrel as a share of the nice half-width, at the centre height: the same for both swings.
    [InlineData(0.30)]
    [InlineData(0.80)]
    [InlineData(1.20)]
    [InlineData(1.40)]
    [InlineData(-1.40)]
    [InlineData(2.00)]
    public void S232_AWidePitchBeatsDriftwoodReachExactlyAsItBeatsTheOrdinarySwing(double d)
    {
        Assert.Equal(Resolve(d, 0, star: false).Quality, Resolve(d, 0, star: true).Quality);
        Assert.Equal(ContactQuality.Miss, Resolve(2.0, 0, star: true).Quality);
    }

    [Fact]
    public void S232_ALateOrEarlyBatIsAMissWithDriftwoodReachToo()
    {
        var half = AtBatResolver.ContactWindowFrames(null, Game.Parks[ParkId.Harbor], false, Game.Rules, Game.StarSkills) / 2;
        Assert.Equal(ContactQuality.Miss, Resolve(0, 1.2, star: true, timing: half + 0.5).Quality);
        Assert.Equal(ContactQuality.Miss, Resolve(0, 1.2, star: true, timing: -(half + 0.5)).Quality);
        Assert.NotEqual(ContactQuality.Miss, Resolve(0, 1.2, star: true, timing: half - 0.5).Quality);
    }

    [Fact]
    public void S232_DriftwoodReachsExitIsTheOrdinarySwingsTimesItsMultiplierAndItsOvalIsDrawnTall()
    {
        var plain = Resolve(0, 0, star: false);
        var star = Resolve(0, 0, star: true);
        Assert.Equal(ContactQuality.Perfect, plain.Quality);
        Assert.Equal(Math.Round(plain.ExitVeloMph * 1.08, 1), star.ExitVeloMph, 1);
        // The drawn oval is the judged one: 1.4 times the zone's half height, the same barrel.
        var rules = Game.Rules;
        var fenn = Game.Must("fenn");
        var bat = Game.Bats["harbor-lumber"];
        var ordinary = SweetSpot.Oval(fenn, bat, 0, 0, rules);
        var tall = SweetSpot.Oval(fenn, bat, 0, 0, rules, 1.4);
        Assert.Equal(ordinary.HalfHeightFt * 1.4, tall.HalfHeightFt, 12);
        Assert.Equal((ordinary.TipHalfFt, ordinary.HandleHalfFt), (tall.TipHalfFt, tall.HandleHalfFt));
        // A ball in the reached band is met under the oval's own top: no extra under-the-ball lift until past it.
        var zone = StrikeZoneGeometry.For(fenn, rules);
        var y = zone.CenterY + zone.HalfHeight * 1.3;
        Assert.True(AtBatResolver.UnderTheBallDeg(0, y, rules, zone) > 0);
        Assert.Equal(0, AtBatResolver.UnderTheBallDeg(0, y, rules, zone, 1.4));
    }

    static AtBatResult Resolve(double d, double h, bool star, string batterId = "fenn", double timing = 0, bool bunt = false)
    {
        var rules = Game.Rules;
        var batter = Game.Must(batterId);
        var bat = Game.Bats["harbor-lumber"];
        var zone = StrikeZoneGeometry.For(batter, rules);
        var barrel = SweetSpot.SwingBarrel(batter, bat, 0, rules);
        var tip = SweetSpot.TipSign(batter.Bats);
        var x = tip * d * SweetSpot.NiceHalfWidthFt(batter.Bats, tip * d, barrel, rules);
        var input = new AtBatInput(
            Pitcher: Game.Must("ashlord"), Batter: batter, OnDeck: null, RunnersOn: [],
            ChargePitch: false, ChangeupPitch: false, TimingErrorFrames: timing,
            UseStarPitch: false, UseStarSwing: star, Bat: bat, PitcherStamina: 80,
            CrossingX: x, CrossingY: zone.CenterY + h * zone.HalfHeight, Bunt: bunt);
        return new AtBatResolver(Game.Chemistry, rules, Game.StarSkills).Resolve(input, Game.Parks[ParkId.Harbor], new Random(1));
    }

    // ---------------------------------------------------------------------------------
    // S-233  The rows are validated
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S233_TheUndertowIsAPitchsAndTheTallOvalIsASwingsAndBothAreBounded()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["fogball"]!["undertow"] = new JsonObject { ["radiusFt"] = 25.0, ["sec"] = 2.0, ["runnerMul"] = 0.8 };
            json["pitches"]!["charmball"]!["undertow"] = new JsonObject { ["radiusFt"] = 12.0, ["sec"] = 3.0, ["runnerMul"] = 0.8 };
            json["pitches"]!["skullball"]!["undertow"] = new JsonObject { ["radiusFt"] = 12.0, ["sec"] = 2.0, ["runnerMul"] = 1.0 };
            json["pitches"]!["prismball"]!["ovalHeightMul"] = 1.4;
            json["swings"]!["staff-swing"]!["ovalHeightMul"] = 2.5;
            json["swings"]!["heart-swing"]!["ovalHeightMul"] = 0.9;
            json["swings"]!["phony-swing"]!["undertow"] = new JsonObject { ["radiusFt"] = 12.0, ["sec"] = 2.0, ["runnerMul"] = 0.8 };
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("star pitch 'fogball' undertow needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'charmball' undertow needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'skullball' undertow needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'prismball' cannot carry ovalHeightMul", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'staff-swing' ovalHeightMul must be between 1 and 2", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'heart-swing' ovalHeightMul must be between 1 and 2", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'phony-swing' cannot carry an undertow", StringComparison.Ordinal));
        // The shipped rows are clean, and each of Fenn's names a family no other captain's special shares.
        Assert.DoesNotContain(ContentDataValidator.Validate(Game.Root.Shipped), e => e.Contains("star-skills", StringComparison.Ordinal));
    }
}
