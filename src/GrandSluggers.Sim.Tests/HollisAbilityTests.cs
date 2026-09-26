using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Hollis's three (spec §13, §8.5): Cable Car's stop and Summit Gust's apex carry; his field ability is the pool's Wall Spring
/// (AB-12). The pitch keeps its path,
/// its crossing and its arrival instant, so the timing window is the ordinary one; the gust stretches the fly's fall along its
/// own line in any park and any wind, and every reader of the ball reads the same carried path. Nothing is rolled.
/// </summary>
public sealed class HollisAbilityTests
{
    static readonly ContentCatalog Game = Shipped.Content;

    [Fact]
    public void HollisCarriesHisOwnThreeInTheirOwnFamilies()
    {
        var hollis = Game.Must("hollis");
        Assert.Equal("rockfall", hollis.StarPitch);
        Assert.Equal("updraft", hollis.StarSwing);
        Assert.Equal(FieldAbilityId.WallSpring, hollis.FieldAbility);   // the shared field pool (AB-12)
        Assert.DoesNotContain(Game.Characters.Values, c => c.Id != "hollis"
            && (c.StarPitch == "rockfall" || c.StarSwing == "updraft"));
        var pitch = Game.StarSkills.Pitch("rockfall")!;
        var swing = Game.StarSkills.Swing("updraft")!;
        Assert.Equal("Cable Car", pitch.Name);
        Assert.Equal("Summit Gust", swing.Name);
        // The row is a hitch and nothing else: no late break, no twin, no leap, no rise, no loop, no sway.
        Assert.NotNull(pitch.Hitch);
        Assert.False(pitch.LateBreak);
        Assert.Null(pitch.Rise);
        Assert.Null(pitch.Loop);
        Assert.Null(pitch.Sway);
        Assert.Equal(1.15, swing.ApexCarryMul);
        Assert.False(swing.ShapesFirstHop);
    }

    // ---------------------------------------------------------------------------------
    // Cable Car
    // ---------------------------------------------------------------------------------

    /// <summary>S-238: the crossing, the arrival instant and the timing window are the ordinary pitch's.</summary>
    [Theory]
    [InlineData("fastball", 0.0, 0.0, 0.69)]
    [InlineData("changeup", 0.3, -0.4, 1.0)]
    [InlineData("curveball", -0.5, 0.5, 1.28)]
    public void S238_TheCableCarCrossesWhereAndWhenThePlainPitchDoes(string type, double aimX, double aimY, double airSec)
    {
        var rules = Game.Rules;
        var star = new PitchCommand(type, 0.2, true, aimX, aimY);
        var plain = star with { Star = false };
        Assert.Equal(PitchFlight.Crossing(plain, rules), PitchFlight.Crossing(star, rules, "rockfall"));
        Assert.Equal(PitchFlight.Point(plain, 1, rules),
            PitchFlight.Point(star, 1, rules, "rockfall", skills: Game.StarSkills, airSec: airSec));
        Assert.Equal(1.0, StarSkills.PitchSpeedMul("rockfall", Game.StarSkills));
        Assert.Equal(AtBatResolver.ContactWindowFrames(null, Game.Parks[ParkId.Harbor], false, rules, Game.StarSkills),
            AtBatResolver.ContactWindowFrames("rockfall", Game.Parks[ParkId.Harbor], false, rules, Game.StarSkills));
        // Every point of the flight is a point of the plain path: before the station the plain ball on its own clock, after it
        // the plain ball further along. The path never leaves the line, and it never runs backwards along it.
        var hitch = Game.StarSkills.Pitch("rockfall")!.Hitch!;
        var last = 0.0;
        for (var u = 0.0; u <= 1.0001; u += 0.01)
        {
            var at = PitchFlight.Point(star, u, rules, "rockfall", skills: Game.StarSkills, airSec: airSec);
            var along = hitch.Progress(u, airSec, rules);
            Assert.Equal(PitchFlight.Point(plain, along, rules), at);
            if (u <= hitch.At) Assert.Equal(PitchFlight.Point(plain, u, rules), at);
            Assert.True(along >= last - 1e-12, $"u={u}: the car never runs back up the line");
            last = along;
        }
    }

    /// <summary>
    /// S-239: at mid-flight the ball stands dead at one point of its path — the plain pitch's point at <c>at</c> — for
    /// exactly <c>holdSec</c> seconds of every delivery, fast or slow, then runs on at an even pace that makes up the time.
    /// </summary>
    [Theory]
    [InlineData(0.69)]
    [InlineData(0.95)]
    [InlineData(1.28)]
    public void S239_TheCarStopsAtItsStationForItsSecondsThenRunsDownTheLine(double airSec)
    {
        var rules = Game.Rules;
        var hitch = Game.StarSkills.Pitch("rockfall")!.Hitch!;
        Assert.Equal(0.5, hitch.At);
        Assert.Equal(0.15, hitch.HoldSec);
        var star = new PitchCommand("fastball", 0, true, 0.2, -0.1);
        var plain = star with { Star = false };
        var station = PitchFlight.Point(plain, hitch.At, rules);
        Assert.Equal(station, Point(hitch.At));
        // Standing still, frame by frame, from the station for the stop's seconds.
        var frame = 1.0 / 60;
        for (var t = hitch.At * airSec; t <= hitch.At * airSec + hitch.HoldSec - 1e-9; t += frame)
            Assert.Equal(station, Point(t / airSec));
        Assert.Equal(station, Point(hitch.At + hitch.HoldSec / airSec));
        // A frame later it has moved on down the line, and it keeps an even pace to the plate: the time made up in the run.
        var after = hitch.At + (hitch.HoldSec + frame) / airSec;
        Assert.NotEqual(station, Point(after));
        var hold = hitch.HoldSec / airSec;
        var pace = (1 - hitch.At) / (1 - hitch.At - hold);
        Assert.Equal(pace * frame / airSec, hitch.Progress(after + frame / airSec, airSec, rules) - hitch.Progress(after, airSec, rules), 9);
        Assert.True(pace > 1, "the run down the line is faster than the plain pace");
        Assert.True(hitch.At * airSec + hitch.HoldSec < 2, "the stop is over inside two seconds of the release");
        return;

        (double X, double Y, double Z) Point(double u) =>
            PitchFlight.Point(star, u, rules, "rockfall", skills: Game.StarSkills, airSec: airSec);
    }

    [Fact]
    public void TheStopNeverTakesMoreThanHalfOfWhatIsLeftAndAClocklessCallerReadsTheSlowestFlight()
    {
        var rules = Game.Rules;
        var hitch = new PitchHitch(0.8, 0.3);
        Assert.Equal((1 - 0.8) / 2, hitch.HoldShare(0.5, rules), 12);
        Assert.Equal(1, hitch.Progress(1, 0.5, rules));
        var shipped = Game.StarSkills.Pitch("rockfall")!.Hitch!;
        Assert.Equal(shipped.HoldSec / rules.Pitching.Flight.AirMaxSec, shipped.HoldShare(0, rules), 12);
    }

    // ---------------------------------------------------------------------------------
    // Summit Gust
    // ---------------------------------------------------------------------------------

    static Park Harbor(double windMph, double windDeg) =>
        Game.Parks[ParkId.Harbor] with { WindMph = windMph, WindDeg = windDeg };

    /// <summary>
    /// S-240: up to the apex the gust ball is the plain ball; from the apex to the first landing its horizontal travel is exactly
    /// 1.15 times the plain ball's, along the same heading, at the same heights and on the same clock — in a calm park and in a
    /// wind from any side, because the gust is the swing's and not the park's.
    /// </summary>
    [Theory]
    [InlineData(0, 0, 5)]
    [InlineData(12, 0, -10)]    // straight out
    [InlineData(12, 180, 20)]   // straight in
    [InlineData(12, 90, 0)]     // across
    public void S240_FromItsApexTheFlyCarriesFifteenPercentFartherAlongItsOwnLine(double windMph, double windDeg, double spray)
    {
        var mul = Game.StarSkills.Swing("updraft")!.ApexCarryMul;
        Assert.Equal(1.15, mul);
        var park = Harbor(windMph, windDeg);
        var plain = BattedBall.Of(82, 34, spray, false, park, Game.Rules);
        var gust = BattedBall.Of(82, 34, spray, false, park, Game.Rules, mul);
        Assert.False(plain.Leaves || gust.Leaves || plain.MetTheWall || gust.MetTheWall, "a fly that lands on the grass either way");
        var p = plain.Samples;
        var g = gust.Samples;
        var land = BallFlight.LandingIndex(p);
        Assert.Equal(land, BallFlight.LandingIndex(g));
        Assert.Equal(SampleEvent.Ground, g[land].Event);
        // The first sample that differs is the apex step: until then it is the same ball.
        var k = 1;
        while (p[k] == g[k]) k++;
        var apex = p[k - 1];
        Assert.Equal(p.Take(land + 1).Max(s => s.Height), Math.Max(apex.Height, p[k].Height));
        for (var i = k; i <= land; i++)
        {
            Assert.Equal(p[i].T, g[i].T);
            Assert.Equal(p[i].Height, g[i].Height);
            Assert.Equal(apex.X + mul * (p[i].X - apex.X), g[i].X, 6);
            Assert.Equal(apex.Z + mul * (p[i].Z - apex.Z), g[i].Z, 6);
        }
        // The landing moved along the ball's own heading from the apex, farther, on the same instant.
        var (px, pz) = (plain.LandingX - apex.X, plain.LandingZ - apex.Z);
        var (gx, gz) = (gust.LandingX - apex.X, gust.LandingZ - apex.Z);
        Assert.Equal(mul * Math.Sqrt(px * px + pz * pz), Math.Sqrt(gx * gx + gz * gz), 6);
        Assert.Equal(0, px * gz - pz * gx, 6);
        Assert.Equal(plain.HangT, gust.HangT);
    }

    [Fact]
    public void AGroundBallHasNoApexToCatchAndTheFenceMeetsTheBallWhereItReallyIs()
    {
        var mul = Game.StarSkills.Swing("updraft")!.ApexCarryMul;
        var park = Harbor(0, 0);
        // A ball that never rises never stops rising: the grounder is the plain grounder.
        Assert.Equal(BattedBall.Of(90, -4, 10, false, park, Game.Rules).Samples,
            BattedBall.Of(90, -4, 10, false, park, Game.Rules, mul).Samples);
        // The gust after the landing is spent: the plain ball's bounces and roll run on from the new landing.
        var plain = BattedBall.Of(82, 34, 5, false, park, Game.Rules);
        var gust = BattedBall.Of(82, 34, 5, false, park, Game.Rules, mul);
        var land = BallFlight.LandingIndex(plain.Samples);
        var shift = (X: gust.Samples[land].X - plain.Samples[land].X, Z: gust.Samples[land].Z - plain.Samples[land].Z);
        for (var i = land + 1; i < Math.Min(plain.Samples.Count, land + 40); i++)
        {
            Assert.Equal(plain.Samples[i].X + shift.X, gust.Samples[i].X, 6);
            Assert.Equal(plain.Samples[i].Z + shift.Z, gust.Samples[i].Z, 6);
        }
    }

    /// <summary>
    /// S-241: the resolver tags the Summit Gust ball; the fielding preview, the landing ring and the live ball all read the
    /// carried path, so the shadow is honest from contact; and real contact that now carries over the fence is a home run.
    /// </summary>
    [Fact]
    public void S241_EveryReaderOfTheBallReadsTheGustAndALongerFlyMayClearTheFence()
    {
        var park = Harbor(0, 0);
        var resolver = new AtBatResolver(Game.Chemistry, Game.Rules, Game.StarSkills);
        var hollis = Game.Must("hollis");
        var input = new AtBatInput(Game.Must("vale"), hollis, null, [], ChargePitch: false, ChangeupPitch: false,
            TimingErrorFrames: 0, UseStarPitch: false, UseStarSwing: true, Bat: null, PitcherStamina: 80,
            CrossingX: 0, CrossingY: 2.5);
        var hit = resolver.Resolve(input, park, new Random(4));
        Assert.Equal("updraft", hit.StarSwingUsed);
        Assert.Equal(1.15, hit.ApexCarryMul);
        var carried = BattedBall.Of(hit.ExitVeloMph, hit.LaunchDeg, hit.SprayDeg, false, park, Game.Rules, 1.15);
        Assert.Equal(carried.Samples, BattedBall.Of(hit, park, Game.Rules).Samples);
        var ordinary = resolver.Resolve(input with { UseStarSwing = false }, park, new Random(4));
        Assert.Equal(1, ordinary.ApexCarryMul);

        // The preview the gloves plan on is the carried ball's landing, not the plain one's.
        var match = Match.Slice(Game, innings: 3, seed: 1);
        var fielding = new FieldingResolver(Game.Chemistry, Game.Rules);
        var fly = FlightFixtures.Hit(park, 82, 34, 5) with { StarSwingUsed = "updraft", ApexCarryMul = 1.15 };
        var shown = fielding.Preview(fly, park, match.Defense.Roster, match.Pitcher, new Random(1));
        var gust = BattedBall.Of(fly, park, Game.Rules);
        var plain = BattedBall.Of(fly with { ApexCarryMul = 1 }, park, Game.Rules);
        Assert.Equal((gust.LandingX, gust.LandingZ), (shown.LandingX, shown.LandingZ));
        Assert.True(Math.Sqrt(gust.LandingX * gust.LandingX + gust.LandingZ * gust.LandingZ)
            > Math.Sqrt(plain.LandingX * plain.LandingX + plain.LandingZ * plain.LandingZ) + 10, "the fielders read the deeper landing");

        // Some real contact dies at the track plain and clears the fence with the gust: a home run, by the fence's own geometry.
        var cleared = Enumerable.Range(80, 40).Select(e => (double)e)
            .FirstOrDefault(e => !BattedBall.Of(e, 34, 0, false, park, Game.Rules).HomeRun
                && BattedBall.Of(e, 34, 0, false, park, Game.Rules, 1.15).HomeRun);
        Assert.True(cleared > 0, "a fly short of the fence plain clears it with the gust");
    }

    // ---------------------------------------------------------------------------------
    // The rows are read strictly
    // ---------------------------------------------------------------------------------

    [Fact]
    public void AHitchOnASwingOrACarryOnAPitchOrAnOutOfRangeRowIsRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["rockfall"]!["hitch"]!["holdSec"] = 0.5;
            json["pitches"]!["fastball"]!["apexCarryMul"] = 1.2;
            json["swings"]!["updraft"]!["apexCarryMul"] = 0.9;
            json["swings"]!["line"]!["hitch"] = new JsonObject { ["at"] = 0.5, ["holdSec"] = 0.1 };
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("star pitch 'rockfall' hitch needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'fastball' cannot carry apexCarryMul", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'updraft' apexCarryMul must be", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'line' cannot carry a hitch", StringComparison.Ordinal));
    }

    [Fact]
    public void TheRetiredFloatAndWindKeysAreNotRulesAnyMore()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["rockfall"]!["float"] = new JsonObject { ["riseFt"] = 2.2, ["dropFrom"] = 0.6 };
            json["swings"]!["updraft"]!["windMul"] = 1.5;
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("float", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("windMul", StringComparison.Ordinal));
    }
}
