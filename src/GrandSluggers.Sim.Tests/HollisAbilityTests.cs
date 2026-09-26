using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Hollis's three (#1150, WD-15 A; spec §13, §8.5): Rockfall's float and Updraft's wind; his field ability is the pool's
/// Wall Spring (AB-12). Each bends how the ball looks or moves; the crossing and the flight still decide the play.
/// </summary>
public sealed class HollisAbilityTests
{
    static readonly ContentCatalog Game = Shipped.Content;

    [Fact]
    public void HollisCarriesHisOwnThree()
    {
        var hollis = Game.Must("hollis");
        Assert.Equal("rockfall", hollis.StarPitch);
        Assert.Equal("updraft", hollis.StarSwing);
        Assert.Equal(FieldAbilityId.WallSpring, hollis.FieldAbility);   // the shared field pool (AB-12)
        Assert.DoesNotContain(Game.Characters.Values, c => c.Id != "hollis"
            && (c.StarPitch == "rockfall" || c.StarSwing == "updraft"));
    }

    // ---------------------------------------------------------------------------------
    // Rockfall
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.5, -0.4)]
    [InlineData(-0.6, 0.5)]
    public void RockfallFloatsHighThenLandsOnTheCrossingThePitchAlwaysHad(double aimX, double aimY)
    {
        var rules = Game.Rules;
        var rise = Game.StarSkills.Pitch("rockfall")!.Float!;
        var star = new PitchCommand("curveball", 0.3, true, aimX, aimY);
        var plain = star with { Star = false };
        var zone = StrikeZoneGeometry.Of(star);
        Assert.Equal(PitchFlight.Crossing(plain, rules), PitchFlight.Crossing(star, rules, "rockfall"));
        Assert.Equal(PitchFlight.Point(plain, 1, rules), PitchFlight.Point(star, 1, rules, "rockfall", skills: Game.StarSkills));
        var highest = 0.0;
        for (var u = 0.01; u < 1; u += 0.01)
        {
            var p = PitchFlight.Point(plain, u, rules);
            var r = PitchFlight.Point(star, u, rules, "rockfall", skills: Game.StarSkills);
            Assert.Equal(p.X, r.X, 9);
            Assert.Equal(p.Z, r.Z, 9);
            Assert.True(r.Y > p.Y, $"u={u}: the float is above the ordinary path until the plate");
            highest = Math.Max(highest, r.Y - p.Y);
        }
        Assert.Equal(rise.RiseFt * zone.VerticalScale, highest, 2);
        Assert.Equal(rise.RiseFt * zone.VerticalScale, PitchFlight.Point(star, rise.DropFrom, rules, "rockfall", skills: Game.StarSkills).Y
            - PitchFlight.Point(plain, rise.DropFrom, rules).Y, 9);
    }

    // ---------------------------------------------------------------------------------
    // Updraft
    // ---------------------------------------------------------------------------------

    static Park Harbor(double windMph, double windDeg) =>
        Game.Parks[ParkId.Harbor] with { WindMph = windMph, WindDeg = windDeg };

    [Fact]
    public void UpdraftAtACalmParkIsTheSameFlight()
    {
        var calm = Harbor(0, 0);
        var plain = BattedBall.Of(98, 34, 5, false, calm, Game.Rules);
        var updraft = BattedBall.Of(98, 34, 5, false, calm, Game.Rules, Game.StarSkills.Swing("updraft")!.WindMul);
        Assert.Equal(plain.Samples, updraft.Samples);
    }

    [Theory]
    [InlineData(0)]     // straight out
    [InlineData(180)]   // straight in
    [InlineData(90)]    // across
    public void UpdraftRidesTheParksWindHalfAgainAsHard(double windDeg)
    {
        var mul = Game.StarSkills.Swing("updraft")!.WindMul;
        Assert.Equal(1.5, mul);
        var windy = Harbor(10, windDeg);
        var calm = BattedBall.Of(98, 34, 5, false, Harbor(0, 0), Game.Rules);
        var plain = BattedBall.Of(98, 34, 5, false, windy, Game.Rules);
        var updraft = BattedBall.Of(98, 34, 5, false, windy, Game.Rules, mul);
        // The same ball in a wind half again as strong: 10 mph × 1.5 is the 15 mph flight, exactly.
        Assert.Equal(BattedBall.Of(98, 34, 5, false, Harbor(15, windDeg), Game.Rules).Samples, updraft.Samples);
        var (px, pz) = (plain.LandingX - calm.LandingX, plain.LandingZ - calm.LandingZ);
        var (ux, uz) = (updraft.LandingX - calm.LandingX, updraft.LandingZ - calm.LandingZ);
        // The wind moves the updraft's landing the same way it moves the plain ball's, and further.
        Assert.True(px * ux + pz * uz > 0, "the updraft rides the wind the plain ball rides");
        Assert.True(Math.Sqrt(ux * ux + uz * uz) > Math.Sqrt(px * px + pz * pz), "and further");
    }

    [Fact]
    public void TheResolverTagsTheUpdraftBallAndEveryFlightOfItReadsTheTag()
    {
        var windy = Harbor(10, 0);
        var resolver = new AtBatResolver(Game.Chemistry, Game.Rules, Game.StarSkills);
        var hollis = Game.Must("hollis");
        var input = new AtBatInput(Game.Must("vale"), hollis, null, [], ChargePitch: false, ChangeupPitch: false,
            TimingErrorFrames: 0, UseStarPitch: false, UseStarSwing: true, Bat: null, PitcherStamina: 80,
            CrossingX: 0, CrossingY: 2.5);
        var hit = resolver.Resolve(input, windy, new Random(4));
        Assert.Equal("updraft", hit.StarSwingUsed);
        Assert.Equal(1.5, hit.WindMul);
        Assert.Equal(BattedBall.Of(hit.ExitVeloMph, hit.LaunchDeg, hit.SprayDeg, false, windy, Game.Rules, 1.5).Samples,
            BattedBall.Of(hit, windy, Game.Rules).Samples);
        var ordinary = resolver.Resolve(input with { UseStarSwing = false }, windy, new Random(4));
        Assert.Equal(1, ordinary.WindMul);
    }

    // ---------------------------------------------------------------------------------
    // The rows are read strictly
    // ---------------------------------------------------------------------------------

    [Fact]
    public void AFloatOnASwingOrAWindOnAPitchOrATooStrongWindIsRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["rockfall"]!["float"]!["dropFrom"] = 1.0;
            json["pitches"]!["fastball"]!["windMul"] = 1.2;
            json["swings"]!["updraft"]!["windMul"] = 3.0;
            json["swings"]!["line"]!["float"] = new JsonObject { ["riseFt"] = 1, ["dropFrom"] = 0.5 };
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("star pitch 'rockfall' float needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'fastball' cannot carry windMul", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'updraft' windMul must be", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'line' cannot carry a float", StringComparison.Ordinal));
    }
}
