using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Rio's two (spec §13): Skyrocket (<c>heatball</c>) is fast and rises late to a crossing above the aimed one, and the
/// umpire, the bat and the CPU judge that real crossing in the ordinary window; Sparkler (<c>heat-swing</c>) has a larger
/// Perfect ring for its own swing only, and the bat must still meet the ball. Nothing is rolled; nothing burns the dirt.
/// </summary>
public sealed class RioAbilityTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    static readonly BatterZone Ref = StrikeZoneGeometry.Reference;

    [Fact]
    public void RioCarriesSkyrocketAndSparklerUnderTheirOldIds()
    {
        var rio = Game.Must("rio");
        Assert.Equal("heatball", rio.StarPitch);
        Assert.Equal("heat-swing", rio.StarSwing);
        var pitch = Game.StarSkills.Pitch("heatball")!;
        var swing = Game.StarSkills.Swing("heat-swing")!;
        Assert.Equal("Skyrocket", pitch.Name);
        Assert.Equal("Sparkler", swing.Name);
        Assert.Equal(1.15, pitch.SpeedMul);
        Assert.Equal(1.15, swing.ExitVeloMul);
        Assert.Equal(1.5, swing.PerfectRingMul);
        Assert.Null(pitch.OnCatch);        // the burn-hop is gone: a caught Skyrocket is an ordinary catch
        Assert.Null(swing.Terrain);        // the burn patch is gone: Sparkler's bend is spent at the plate
        Assert.Null(pitch.Hitch);
        Assert.Null(pitch.Leap);
        Assert.DoesNotContain(Game.Characters.Values, c => c.Id != "rio" && (c.StarPitch == "heatball" || c.StarSwing == "heat-swing"));
    }

    // ---------------------------------------------------------------------------------
    // S-206  Skyrocket: nothing until two-thirds, then up to the full rise at the plate
    // ---------------------------------------------------------------------------------

    public static IEnumerable<object[]> Deliveries =>
    [
        [PitchFamily.Fastball, 0.0, 0.0, 0.0, 0.0],
        [PitchFamily.Changeup, 0.3, -0.4, 0.2, 0.0],
        [PitchFamily.Fastball, 1.0, 0.5, -0.3, 1.0],
        [PitchFamily.Fastball, 0.0, -0.2, 0.6, -0.7],
    ];

    [Theory]
    [MemberData(nameof(Deliveries))]
    public void S206_SkyrocketIsThePlainPathUntilTwoThirdsThenRisesToOneFootAtThePlate(
        string family, double charge, double aimX, double aimY, double breakX)
    {
        var rules = Game.Rules;
        var rise = Game.StarSkills.Pitch("heatball")!.Rise!;
        Assert.Equal(1.0, rise.RiseFt);
        Assert.Equal(0, rise.Lift(2.0 / 3.0));
        Assert.Equal(1.0, rise.Lift(1.0));
        var star = new PitchCommand(family, charge, true, aimX, aimY, breakX, Zone: Ref);
        var plain = star with { Star = false };
        var last = 0.0;
        for (var i = 0; i <= 300; i++)
        {
            var u = i / 300.0;
            var a = PitchFlight.Point(star, u, rules, "heatball", skills: Game.StarSkills);
            var b = PitchFlight.Point(plain, u, rules);
            // The ball never moves sideways or along the line: the rise is straight up, always.
            Assert.Equal(b.X, a.X);
            Assert.Equal(b.Z, a.Z);
            var lift = a.Y - b.Y;
            if (u <= 2.0 / 3.0) Assert.Equal(0, lift);
            Assert.True(lift >= last - 1e-12, $"u {u:F3}: the rise never falls back ({lift} after {last})");
            last = lift;
        }
        Assert.Equal(1.0, last, 12);
        // Half-way through the last third the rise is a quarter of the whole: it climbs late, like a rocket.
        var mid = PitchFlight.Point(star, 5.0 / 6.0, rules, "heatball", skills: Game.StarSkills).Y
                  - PitchFlight.Point(plain, 5.0 / 6.0, rules).Y;
        Assert.InRange(mid, 0.24, 0.26);
    }

    [Fact]
    public void S206_TheRiseIsInTheBattersZoneLikeEveryVerticalStarShape()
    {
        var tall = new BatterZone(1.4, 4.2);
        var star = new PitchCommand(PitchFamily.Fastball, 0, true, 0, 0, 0, Zone: tall);
        var plain = star with { Star = false };
        var lift = PitchFlight.Crossing(star, Game.Rules, "heatball").Y - PitchFlight.Crossing(plain, Game.Rules).Y;
        Assert.Equal(tall.VerticalScale, lift, 12);
    }

    // ---------------------------------------------------------------------------------
    // S-207  The umpire, the bat and the CPU judge the risen crossing, in the ordinary window
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S207_TheRealCrossingIsTheRisenOneForTheUmpireTheBatAndTheCpu()
    {
        var rules = Game.Rules;
        // High in the zone: the plain pitch is a strike, the Skyrocket rises out of it for a ball.
        var high = new PitchCommand(PitchFamily.Fastball, 0, true, 0, 0.7, 0, Zone: Ref);
        var highPlain = high with { Star = false };
        Assert.Equal(PitchFlight.Crossing(highPlain, rules).Y + 1.0, PitchFlight.Crossing(high, rules, "heatball").Y, 12);
        Assert.True(AtBatResolver.PitchInZone(highPlain, 5, rules));
        Assert.False(AtBatResolver.PitchInZone(high, 5, rules, "heatball"));
        // Low under the zone: the plain pitch is a ball, the Skyrocket rises into it for a strike.
        var low = new PitchCommand(PitchFamily.Fastball, 0, true, 0, -1.0, 0, Zone: Ref);
        Assert.False(AtBatResolver.PitchInZone(low with { Star = false }, 5, rules));
        Assert.True(AtBatResolver.PitchInZone(low, 5, rules, "heatball"));
        // The contact aim is where the risen ball crosses: what the bat meets and the CPU reads.
        Assert.Equal(PitchFlight.ContactAim(highPlain, rules).Y + 1.0 / (PitchFlight.PlateScaleY * Ref.VerticalScale),
            PitchFlight.ContactAim(high, rules, "heatball").Y, 12);
        // A CPU pitcher aiming a Skyrocket at a spot aims under it and the rise lands it there.
        var aimed = PitchFlight.AimForCrossing(high, 0, 0, rules, "heatball");
        var landed = PitchFlight.ContactAim(aimed, rules, "heatball");
        Assert.Equal(0, landed.X, 9);
        Assert.Equal(0, landed.Y, 9);
    }

    [Fact]
    public void S207_SkyrocketIsFastButJudgedInTheOrdinaryWindow()
    {
        var rules = Game.Rules;
        var harbor = Game.Parks[ParkId.Harbor];
        Assert.Equal(AtBatResolver.ContactWindowFrames(null, harbor, false, rules, Game.StarSkills),
            AtBatResolver.ContactWindowFrames("heatball", harbor, false, rules, Game.StarSkills));
        Assert.Equal(1.15, StarSkills.PitchSpeedMul("heatball", Game.StarSkills));
        // The whole bend is inside the two-second rule: even the slowest flight is over well before 2 s.
        Assert.True(rules.Pitching.Flight.AirMaxSec <= 2.0);
    }

    // ---------------------------------------------------------------------------------
    // S-208  Sparkler: a larger Perfect ring for this swing only; a miss is still a miss
    // ---------------------------------------------------------------------------------

    [Theory]
    // Normalized distance from the cursor centre along the barrel, ordinary vs Sparkler.
    [InlineData(0.30, ContactQuality.Perfect, ContactQuality.Perfect)]
    [InlineData(0.50, ContactQuality.Nice, ContactQuality.Perfect)]
    [InlineData(0.60, ContactQuality.Nice, ContactQuality.Perfect)]
    [InlineData(0.70, ContactQuality.Nice, ContactQuality.Nice)]
    [InlineData(0.95, ContactQuality.Nice, ContactQuality.Nice)]
    [InlineData(1.10, ContactQuality.Sour, ContactQuality.Sour)]
    [InlineData(2.00, ContactQuality.Miss, ContactQuality.Miss)]
    public void S208_SparklersPerfectRingIsHalfAgainAsWideAndTheRestOfTheBatIsTheSame(
        double d, ContactQuality plain, ContactQuality sparkler)
    {
        var rules = Game.Rules;
        Assert.Equal(rules.Batting.Cursor.PerfectFraction * 1.5, SweetSpot.PerfectFraction(rules, 1.5), 12);
        Assert.Equal(plain, Resolve(d, star: false).Quality);
        Assert.Equal(sparkler, Resolve(d, star: true).Quality);
        // Another captain's star swing keeps the ordinary ring.
        Assert.Equal(plain, Resolve(d, star: true, batterId: "zig").Quality);
    }

    [Fact]
    public void S208_ABatOffThePlaneIsAMissWithSparklerToo()
    {
        var half = AtBatResolver.ContactWindowFrames(null, Game.Parks[ParkId.Harbor], false, Game.Rules, Game.StarSkills) / 2;
        Assert.Equal(ContactQuality.Miss, Resolve(0, star: true, timing: half + 0.5).Quality);
        Assert.Equal(ContactQuality.Miss, Resolve(0, star: true, timing: -(half + 0.5)).Quality);
        Assert.NotEqual(ContactQuality.Miss, Resolve(0, star: true, timing: half - 0.5).Quality);
    }

    [Fact]
    public void S208_SparklersExitIsTheOrdinarySwingsTimesItsMultiplier()
    {
        var plain = Resolve(0.3, star: false);
        var star = Resolve(0.3, star: true);
        Assert.Equal(ContactQuality.Perfect, plain.Quality);
        Assert.Equal(Math.Round(plain.ExitVeloMph * 1.15, 1), star.ExitVeloMph, 1);
    }

    static AtBatResult Resolve(double d, bool star, string batterId = "rio", double timing = 0)
    {
        var rules = Game.Rules;
        var batter = Game.Must(batterId);
        var bat = Game.Bats["harbor-lumber"];
        var zone = StrikeZoneGeometry.For(batter, rules);
        var barrel = SweetSpot.SwingBarrel(batter, bat, 0, rules);
        var tip = SweetSpot.TipSign(batter.Bats);
        var x = tip * d * SweetSpot.NiceHalfWidthFt(batter.Bats, tip, barrel, rules);
        var input = new AtBatInput(
            Pitcher: Game.Must("ashlord"), Batter: batter, OnDeck: null, RunnersOn: [],
            ChargePitch: false, ChangeupPitch: false, TimingErrorFrames: timing,
            UseStarPitch: false, UseStarSwing: star, Bat: bat, PitcherStamina: 80,
            CrossingX: x, CrossingY: zone.CenterY);
        return new AtBatResolver(Game.Chemistry, rules, Game.StarSkills).Resolve(input, Game.Parks[ParkId.Harbor], new Random(1));
    }

    // ---------------------------------------------------------------------------------
    // S-209  No patch on the dirt, and the rows are validated
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S209_SparklerRaisesNoFurnaceFlagAndHotIronDoes()
    {
        var home = Game.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "soot", "vine", "moss", "hex");
        var away = Game.Team("Offense", "rio", "boom", "cinder", "grit", "zig", "nugget", "nico", "gull", "marlow");
        var match = Match.Exhibition(Game, home, away, 3, 1, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Hit(match.Park, 95, 14, 10, rules: match.Rules);
        Assert.False(match.PreviewHit(hit with { StarSwingUsed = "heat-swing" }).Furnace);
        Assert.True(match.PreviewHit(hit with { StarSwingUsed = "furnace" }).Furnace);
    }

    [Fact]
    public void S209_TheRiseIsAPitchsAndTheRingIsASwingsAndBothAreBounded()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["heatball"]!["rise"] = new JsonObject { ["riseFt"] = 3.0, ["from"] = 0.6667 };
            json["pitches"]!["charmball"]!["rise"] = new JsonObject { ["riseFt"] = 1.0, ["from"] = 1.0 };
            json["pitches"]!["fogball"]!["perfectRingMul"] = 1.5;
            json["swings"]!["heat-swing"]!["perfectRingMul"] = 2.5;
            json["swings"]!["heart-swing"]!["perfectRingMul"] = 0.8;
            json["swings"]!["shell-swing"]!["rise"] = new JsonObject { ["riseFt"] = 1.0, ["from"] = 0.5 };
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("star pitch 'heatball' rise needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'charmball' rise needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'fogball' cannot carry perfectRingMul", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'heat-swing' perfectRingMul must be between 1 and 2", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'heart-swing' perfectRingMul must be between 1 and 2", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'shell-swing' cannot carry a rise", StringComparison.Ordinal));
        // The shipped rows are clean.
        Assert.DoesNotContain(ContentDataValidator.Validate(Game.Root.Shipped), e => e.Contains("star-skills", StringComparison.Ordinal));
    }
}
