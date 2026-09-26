using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Vale's two specials (spec §13; Appendix B rows S-210 … S-213): Aurora Ribbon's swelling sway and Follow Spot's pause.
/// The sway bends only the look of the flight — the crossing is the ordinary one — and the pause holds one body, the
/// nearest fielder, while every other body plays on. The ball, the bodies and the geometry still decide the play.
/// </summary>
public sealed class ValeAbilityTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;

    [Fact]
    public void ValeCarriesAuroraRibbonAndFollowSpotUnderTheirOwnFamilies()
    {
        var vale = Game.Must("vale");
        Assert.Equal("charmball", vale.StarPitch);
        Assert.Equal("heart-swing", vale.StarSwing);
        Assert.Equal("Aurora Ribbon", Game.StarSkills.Pitch("charmball")!.Name);
        Assert.Equal("Follow Spot", Game.StarSkills.Swing("heart-swing")!.Name);
        Assert.Equal(0.9, Game.StarSkills.Pitch("charmball")!.SpeedMul);
        Assert.Equal(1.05, Game.StarSkills.Swing("heart-swing")!.ExitVeloMul);
        using var shipped = new ContentFixture();
        Assert.Empty(ContentDataValidator.Validate(shipped.Root));
    }

    // ---------------------------------------------------------------------------------
    // S-210  Aurora Ribbon: widest at mid-flight, on the ordinary path from 0.85, the ordinary crossing
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.5, -0.4)]
    [InlineData(-0.6, 0.5)]
    public void S210_AuroraRibbonSwellsToItsWidestMidFlightAndSettlesOntoTheOrdinaryCrossing(double aimX, double aimY)
    {
        var rules = Game.Rules;
        var sway = Game.StarSkills.Pitch("charmball")!.Sway!;
        Assert.Equal(0.5, sway.PeakAt);
        Assert.Equal(0.85, sway.SettleBy);
        var star = new PitchCommand("curveball", 0.3, true, aimX, aimY);
        var plain = star with { Star = false };
        Assert.Equal(PitchFlight.Crossing(plain, rules), PitchFlight.Crossing(star, rules, "charmball"));
        Assert.Equal(PitchFlight.Point(plain, 1, rules), PitchFlight.Point(star, 1, rules, "charmball", skills: Game.StarSkills));
        var widest = 0.0;
        var widestAt = 0.0;
        var left = false;
        var right = false;
        for (var i = 0; i <= 200; i++)
        {
            var u = i / 200.0;
            var p = PitchFlight.Point(plain, u, rules);
            var s = PitchFlight.Point(star, u, rules, "charmball", skills: Game.StarSkills);
            // A sway is sideways only: the height and the depth are the ordinary pitch's all the way.
            Assert.Equal(p.Y, s.Y, 12);
            Assert.Equal(p.Z, s.Z, 12);
            var side = s.X - p.X;
            if (u >= sway.SettleBy) Assert.Equal(p.X, s.X);   // the last stretch is the ordinary path, exactly
            left |= side < -0.1;
            right |= side > 0.1;
            if (Math.Abs(side) > widest) (widest, widestAt) = (Math.Abs(side), u);
        }
        Assert.True(left && right, "the ribbon sways to both sides");
        Assert.Equal(sway.WidthFt, widest, 9);
        Assert.Equal(sway.PeakAt, widestAt, 9);
        // It swells: the sway a quarter of the way in and three-quarters of the way in are both narrower than mid-flight.
        Assert.True(Math.Abs(sway.OffsetFt(0.25)) < sway.WidthFt && Math.Abs(sway.OffsetFt(0.75)) < Math.Abs(sway.OffsetFt(0.25)));
        Assert.Equal(0, sway.OffsetFt(0));
    }

    [Fact]
    public void S210_TheBatAndTheUmpireReadTheOrdinaryCrossingAndTheWindowIsTheOrdinaryOne()
    {
        var skills = Game.StarSkills;
        var harbor = Game.Parks[ParkId.Harbor];
        Assert.Equal(AtBatResolver.ContactWindowFrames(null, harbor, false, Game.Rules, skills),
            AtBatResolver.ContactWindowFrames("charmball", harbor, false, Game.Rules, skills));
        var star = new PitchCommand(PitchFamily.Fastball, 0, true, 0.4, 0.2);
        foreach (var aim in new[] { (0.4, 0.2), (1.1, 0.0), (0.0, -1.2) })
        {
            var pitch = star with { AimX = aim.Item1, AimY = aim.Item2 };
            Assert.Equal(StrikeZoneGeometry.Contains(pitch with { Star = false }, Game.Rules),
                StrikeZoneGeometry.Contains(pitch, Game.Rules, "charmball"));
        }
    }

    // ---------------------------------------------------------------------------------
    // Follow Spot
    // ---------------------------------------------------------------------------------

    static Match Harbor()
    {
        var home = Game.Team("Defense", "rio", "pewter", "lace", "frost", "basil", "soot", "vine", "moss", "hex");
        var away = Game.Team("Offense", "vale", "boom", "cinder", "grit", "sable", "nugget", "nico", "gull", "marlow");
        return Match.Exhibition(Game, home, away, 3, 1, parkId: ParkId.Harbor);
    }

    sealed record Run(FieldingPreview Preview, List<FielderDazzled> Dazzles,
        List<(double T, IReadOnlyDictionary<string, (double X, double Z)> Bodies)> Frames,
        Dictionary<string, double> Ready, PlayEvent? Play);

    /// <summary>One play of <paramref name="hit"/> on CPU gloves (or the defense's human seat) at Harbor, every frame's bodies kept.</summary>
    static Run Play(AtBatResult hit, LiveSeats? seats = null)
    {
        var match = Harbor();
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, seats ?? LiveSeats.CpuOnly, 0,
            LivePlayCommandSource.Cpu)).Snapshot.Active);
        var view = (IPursuitView)live;
        var ready = Diamond.Order.ToDictionary(p => p, view.ReadyAt);
        var dazzles = live.FactsThisPlay.OfType<FielderDazzled>().ToList();
        var frames = new List<(double, IReadOnlyDictionary<string, (double X, double Z)>)>();
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 12 && play is null; i++)
        {
            play = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
            if (play is not null) break;   // the completing frame resets the field
            frames.Add((live.ElapsedSeconds, new Dictionary<string, (double X, double Z)>(live.Fielders)));
        }
        return new Run(preview, dazzles, frames, ready, play);
    }

    static AtBatResult Spot(AtBatResult hit) => hit with { StarSwingUsed = "heart-swing" };

    /// <summary>A fly to center: CF is the nearest fielder and still the only one who can take it.</summary>
    static AtBatResult FlyToCenter() => FlightFixtures.Landing(Harbor().Park, 230, 30, 0, rules: Game.Rules);

    /// <summary>A grounder toward the hole: 3B is the nearest fielder; with 3B held, SS reaches it first.</summary>
    static AtBatResult GrounderToTheHole() => FlightFixtures.Landing(Harbor().Park, 60, 3, -35, rules: Game.Rules);

    // ---------------------------------------------------------------------------------
    // S-211  The nearest fielder stands still for the pause, then plays the ball
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S211_FollowSpotHoldsTheNearestFielderForItsPauseAndThenHePlaysTheBall()
    {
        var pause = Game.StarSkills.Swing("heart-swing")!.FielderPauseSec;
        Assert.Equal(0.8, pause);
        var hit = FlyToCenter();
        var plain = Play(hit);
        var spot = Play(Spot(hit));
        // The nearest fielder is the body the play would send without the spot: geometry names it, nothing is rolled.
        Assert.Equal("CF", plain.Preview.Position);
        Assert.Equal("", plain.Preview.Dazzled);
        Assert.Equal("CF", spot.Preview.Dazzled);
        Assert.Equal(pause, spot.Preview.DazzleSec);
        Assert.Equal("CF", spot.Preview.Position);
        Assert.Equal(new FielderDazzled("heart-swing", "CF", pause), Assert.Single(spot.Dazzles));
        Assert.Empty(plain.Dazzles);
        Assert.Equal(FieldingResolver.Dazzle(plain.Ready["CF"], pause), spot.Ready["CF"]);
        var start = spot.Frames[0].Bodies["CF"];
        foreach (var (t, bodies) in spot.Frames.Where(f => f.T < pause - 1e-9))
            Assert.Equal(start, bodies["CF"]);
        var after = spot.Frames.First(f => f.T > pause + 0.1).Bodies["CF"];
        Assert.True(Diamond.Dist(start.X, start.Z, after.X, after.Z) > 0.1, "the paused fielder runs once the pause is over");
        // The ordinary fly: CF is off before the pause would end.
        var early = plain.Frames.Last(f => f.T < pause - 1e-9).Bodies["CF"];
        Assert.True(Diamond.Dist(start.X, start.Z, early.X, early.Z) > 0.1, "without the spot CF is already running");
    }

    // ---------------------------------------------------------------------------------
    // S-212  With the nearest fielder held, a teammate who reaches the ball first backs him up
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S212_WhenAnotherFielderNowReachesTheBallFirstThePlaySendsHim()
    {
        var hit = GrounderToTheHole();
        var plain = Play(hit);
        var spot = Play(Spot(hit));
        Assert.Equal("3B", plain.Preview.Position);
        Assert.Equal("3B", spot.Preview.Dazzled);
        Assert.Equal("SS", spot.Preview.Position);
        var third = spot.Frames[0].Bodies["3B"];
        foreach (var (t, bodies) in spot.Frames.Where(f => f.T < spot.Preview.DazzleSec - 1e-9))
            Assert.Equal(third, bodies["3B"]);
        // SS moves at once on its own clock: the spot held one body, not the play.
        Assert.Equal(plain.Ready["SS"], spot.Ready["SS"]);
        Assert.NotNull(spot.Play);
    }

    // ---------------------------------------------------------------------------------
    // S-213  One body only; a foul holds nobody; a human glove pays the same pause
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void S213_OnlyTheOneNearestFielderWaitsAndEveryOtherBodyKeepsItsOwnClock(bool fly)
    {
        var hit = fly ? FlyToCenter() : GrounderToTheHole();
        var plain = Play(hit);
        var spot = Play(Spot(hit));
        var held = spot.Preview.Dazzled;
        Assert.NotEqual("", held);
        foreach (var pos in Diamond.Order.Where(p => p != held))
            Assert.Equal(plain.Ready[pos], spot.Ready[pos]);
        Assert.True(spot.Ready[held] > plain.Ready[held]);
    }

    [Fact]
    public void S213_AFoulHoldsNobodyAndAHumanGlovePaysThePauseTheCpuPays()
    {
        var park = Harbor().Park;
        var foul = Spot(FlightFixtures.Landing(park, 150, 35, 60, rules: Game.Rules));
        var preview = Harbor().PreviewHit(foul);
        Assert.True(preview.Foul);
        Assert.Equal("", preview.Dazzled);
        Assert.Equal(0, preview.DazzleSec);

        var cpu = Play(Spot(FlyToCenter()));
        var human = Play(Spot(FlyToCenter()), new LiveSeats(false, true, true, false));
        var pause = cpu.Preview.DazzleSec;
        Assert.Equal(FieldingResolver.Dazzle(0, pause), pause);
        Assert.Equal(pause, cpu.Ready["CF"]);
        Assert.Equal(pause, human.Ready["CF"]);
    }

    // ---------------------------------------------------------------------------------
    // The rows are read strictly
    // ---------------------------------------------------------------------------------

    [Fact]
    public void ASwayOnASwingOrThatReachesThePlateOrAPauseOnAPitchOrPastTwoSecondsIsRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["charmball"]!["sway"]!["settleBy"] = 1.0;
            json["pitches"]!["fastball"]!["fielderPauseSec"] = 0.5;
            json["swings"]!["heart-swing"]!["fielderPauseSec"] = 2.5;
            json["swings"]!["line"]!["sway"] = new System.Text.Json.Nodes.JsonObject
            {
                ["widthFt"] = 1, ["cycles"] = 2, ["peakAt"] = 0.5, ["settleBy"] = 0.85
            };
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("star pitch 'charmball' sway needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'fastball' cannot carry fielderPauseSec", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'heart-swing' fielderPauseSec must be", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'line' cannot carry a sway", StringComparison.Ordinal));
    }
}
