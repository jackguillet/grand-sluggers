using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Reed's three (spec §13, §8.4): Skipping Stone's two skips on the dirt, Lily Hop's hop over the first infield glove; his field
/// ability is the pool's Lick Catch (AB-12). The pitch keeps its ground track, crossing and arrival; the liner keeps its ground
/// track and landing.
/// </summary>
public sealed class ReedAbilityTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;

    [Fact]
    public void ReedCarriesHisOwnThree()
    {
        var reed = Game.Must("reed");
        Assert.Equal("leapfrog", reed.StarPitch);
        Assert.Equal("pond-skip", reed.StarSwing);
        Assert.Equal(FieldAbilityId.LickCatch, reed.FieldAbility);   // the shared field pool (AB-12): a tongue body
        Assert.DoesNotContain(Game.Characters.Values, c => c.Id != "reed"
            && (c.StarPitch == "leapfrog" || c.StarSwing == "pond-skip"));
    }

    static PitchSkips Skips => Game.StarSkills.Pitch("leapfrog")!.Skips!;
    static StarSwingSkill LilyHop => Game.StarSkills.Swing("pond-skip")!;

    [Fact]
    public void SkippingStoneAndLilyHopAreReedsAndTheOldPaceAndBounceAreGone()
    {
        var pitch = Game.StarSkills.Pitch("leapfrog")!;
        Assert.Equal("Skipping Stone", pitch.Name);
        Assert.Equal(1.0, pitch.SpeedMul);
        Assert.Equal("Lily Hop", LilyHop.Name);
        Assert.False(LilyHop.ShapesFirstHop);
        Assert.InRange(LilyHop.LaunchDeg!.Value, 8, 18);   // a liner
        Assert.Equal(5, LilyHop.Hop!.HeightFt);
    }

    // ---------------------------------------------------------------------------------
    // Skipping Stone
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// S-242: the ball touches the dirt exactly twice, at <c>firstAt</c> and <c>secondAt</c>; the skip between is lower than the
    /// flight to the first touch and shorter; off the second skip it climbs the whole way to the plate; every point is over the
    /// plain ball's ground point at the plain instant, and the crossing is the plain one.
    /// </summary>
    [Theory]
    [InlineData("fastball", 0.0, 0.0)]
    [InlineData("curveball", 0.5, -0.4)]
    [InlineData("sinker", -0.6, 0.6)]
    public void S242_SkippingStoneSkipsTwiceOnTheDirtAndPopsUpOntoTheOrdinaryCrossing(string family, double aimX, double aimY)
    {
        var rules = Game.Rules;
        var star = new PitchCommand(family, 0.3, true, aimX, aimY);
        var plain = star with { Star = false };
        Assert.Equal(PitchFlight.Point(plain, 1, rules), Point(star, 1));
        Assert.Equal(PitchFlight.Crossing(plain, rules), PitchFlight.Crossing(star, rules, "leapfrog"));
        Assert.Equal(0, Point(star, Skips.FirstAt).Y, 12);
        Assert.Equal(0, Point(star, Skips.SecondAt).Y, 12);
        var touches = 0;
        var wasDown = false;
        var beforeFirst = 0.0;
        var skipTop = 0.0;
        var last = 0.0;
        for (var i = 0; i <= 1000; i++)
        {
            var u = i / 1000.0;
            var p = PitchFlight.Point(plain, u, rules);
            var v = Point(star, u);
            // The same ground track on the same clock: only the height bends.
            Assert.Equal(p.X, v.X, 12);
            Assert.Equal(p.Z, v.Z, 12);
            Assert.True(v.Y >= -1e-12, $"u={u}: the ball never goes under the dirt");
            var down = v.Y < 1e-9;
            if (down && !wasDown) touches++;
            wasDown = down;
            if (u < Skips.FirstAt) beforeFirst = Math.Max(beforeFirst, v.Y);
            else if (u <= Skips.SecondAt) skipTop = Math.Max(skipTop, v.Y);
            else Assert.True(v.Y >= last - 1e-12, $"u={u}: off the second skip the ball climbs to the plate");
            last = v.Y;
        }
        Assert.Equal(2, touches);
        Assert.True(skipTop < beforeFirst, $"the skip ({skipTop:F2} ft) is lower than the flight to the first ({beforeFirst:F2} ft)");
        Assert.True(Skips.SecondAt - Skips.FirstAt < Skips.FirstAt, "the skip is shorter than the flight to the first touch");
        Assert.Equal(Skips.HopFt * StrikeZoneGeometry.Of(star).VerticalScale, skipTop, 3);
    }

    static (double X, double Y, double Z) Point(PitchCommand pitch, double u) =>
        PitchFlight.Point(pitch, u, Game.Rules, "leapfrog", skills: Game.StarSkills);

    /// <summary>
    /// S-243: the umpire, the aim tell, the bat, the timing window and the CPU batter read the plain crossing; a Skipping Stone
    /// aimed in the zone is a strike though it touched the dirt twice; the speed is the plain pitch's and the flight ends inside 2 s.
    /// </summary>
    [Theory]
    [InlineData(0.0, 0.0, 0.0)]
    [InlineData(0.9, 0.6, 0.0)]
    [InlineData(-0.8, -0.5, -0.7)]
    public void S243_TheUmpireTheBatAndTheCpuReadTheOrdinaryCrossing(double aimX, double aimY, double breakX)
    {
        var rules = Game.Rules;
        var star = new PitchCommand("fastball", 0.6, true, aimX, aimY, breakX);
        var plain = star with { Star = false };
        Assert.Equal(PitchFlight.ContactAim(plain, rules), PitchFlight.ContactAim(star, rules, "leapfrog"));
        Assert.Equal(StrikeZoneGeometry.Contains(plain, rules), StrikeZoneGeometry.Contains(star, rules, "leapfrog"));
        Assert.Equal(PitchFlight.AimForCrossing(plain, 0.2, -0.1, rules).AimX, PitchFlight.AimForCrossing(star, 0.2, -0.1, rules, "leapfrog").AimX);
        Assert.Equal(AtBatResolver.PitchSpeedMph(plain, 6, rules), AtBatResolver.PitchSpeedMph(star, 6, rules, "leapfrog", Game.StarSkills));
        Assert.Equal(AtBatResolver.ContactWindowFrames(null, Game.Parks[ParkId.Harbor], false, rules, Game.StarSkills),
            AtBatResolver.ContactWindowFrames("leapfrog", Game.Parks[ParkId.Harbor], false, rules, Game.StarSkills));
        Assert.True(PitchFlight.AirSeconds(AtBatResolver.PitchSpeedMph(star, 1, rules, "leapfrog", Game.StarSkills), rules) < 2);
        // Down the middle it touched the dirt twice and is still a strike: the crossing alone calls it.
        var middle = new PitchCommand("fastball", 0.6, true, 0, 0);
        Assert.Equal(0, Point(middle, Skips.FirstAt).Y, 12);
        Assert.True(StrikeZoneGeometry.Contains(middle, rules, "leapfrog"));
        // Only the starred Skipping Stone skips.
        Assert.True(PitchFlight.Point(middle with { Star = false }, Skips.FirstAt, rules, "leapfrog", skills: Game.StarSkills).Y > 1);
        Assert.True(PitchFlight.Point(middle, Skips.FirstAt, rules, "heatball", skills: Game.StarSkills).Y > 1);
    }

    // ---------------------------------------------------------------------------------
    // Lily Hop
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// S-244: the hop laid over a glove on a real Lily Hop liner: inside the glove's reach the ball is <c>heightFt</c> over its line
    /// and over the standing catch height; outside the hop's ring it is on its line; the ground track, the clock, the landing and
    /// everything after are the straight liner's. A ring that holds the landing gets no hop, and a ball over glove height is not
    /// entered.
    /// </summary>
    [Theory]
    [InlineData(85, -20, 0.0)]
    [InlineData(95, 20, 1.5)]
    [InlineData(75, -35, -2.5)]
    public void S244_TheHopLiftsTheBallOverTheGloveAndLeavesTheRestOfTheLinerAlone(double exit, double spray, double missFt)
    {
        var rules = Game.Rules;
        var park = Game.Parks[ParkId.Harbor];
        var hop = LilyHop.Hop!;
        var line = BattedBall.Of(exit, LilyHop.LaunchDeg!.Value, spray, false, park, rules).Samples;
        var land = BallFlight.LandingIndex(line);
        // A glove beside the line where the liner is at glove height, missFt to its side.
        var at = line.First(s => s.T > 0.4 && s.Height <= rules.Fielding.Catch.StandingHeightFt);
        var len = Math.Sqrt(at.X * at.X + at.Z * at.Z);
        var (gx, gz) = (at.X + missFt * -at.Z / len, at.Z + missFt * at.X / len);
        const double reach = 4.0;
        Assert.NotNull(BallHop.Entry(line, 0, gx, gz, reach, rules.Fielding.Catch.StandingHeightFt, BallHop.WithinSec));
        var hopped = hop.Apply(line, 0, gx, gz, reach)!;
        Assert.Equal(line.Count + 2, hopped.Count);
        Assert.Equal((line[land].X, line[land].Z, line[land].T), (hopped[land + 2].X, hopped[land + 2].Z, hopped[land + 2].T));
        for (var i = land; i < line.Count; i++) Assert.Equal(line[i], hopped[i + 2]);
        var inReach = 0;
        foreach (var s in hopped)
        {
            var straight = BallFlight.PointAt(line, s.T, rules);
            // Every sample, old or added, is over the straight ball's ground point at its instant.
            Assert.Equal(straight.X, s.X, 6);
            Assert.Equal(straight.Z, s.Z, 6);
            var d = Diamond.Dist(s.X, s.Z, gx, gz);
            var lift = s.Height - straight.Y;
            if (d >= hop.OuterFt(reach)) Assert.Equal(0, lift, 6);
            if (d < reach)
            {
                inReach++;
                Assert.Equal(hop.HeightFt, lift, 6);
                Assert.True(s.Height > rules.Fielding.Catch.StandingHeightFt, $"inside the reach the ball is {s.Height:F2} ft up");
            }
        }
        Assert.True(inReach > 3);
        // A ring around the landing: the ball would come down inside the hop, so there is none.
        Assert.Null(hop.Apply(line, 0, line[land].X, line[land].Z, reach));
        // Over glove height: a glove under the ball's highest point is not entered.
        var top = line.Take(land).MaxBy(s => s.Height);
        if (top.Height > rules.Fielding.Catch.StandingHeightFt)
            Assert.Null(BallHop.Entry(line, 0, top.X, top.Z, 0.5, rules.Fielding.Catch.StandingHeightFt, BallHop.WithinSec));
    }

    /// <summary>
    /// S-245: a Lily Hop liner at the shortstop. The plain liner off the same contact is his catch; the Lily Hop is laid over him
    /// live, the live ball is the hopped path every frame for every glove and the CPU, it is over his reach while it passes, and
    /// it is not his out. A Lily Hop liner no infield glove meets at glove height flies its straight line.
    /// </summary>
    [Theory]
    [InlineData(85, -20, Frame)]
    [InlineData(95, -20, Frame)]
    [InlineData(85, -20, 0.021)]   // Unity ticks at its own uneven dt
    public void S245_TheLilyHopClearsTheShortstopWhoCatchesThePlainLiner(double exit, double spray, double dt)
    {
        var plain = Live(exit, spray, "line", dt);
        Assert.Empty(plain.Hops);
        Assert.Equal(PlayKind.FlyOut, plain.Play!.Kind);
        Assert.Equal("soot", plain.Play.Fielder!.Id);
        var lily = Live(exit, spray, "pond-skip", dt);
        var hop = Assert.Single(lily.Hops);
        Assert.Equal("SS", hop.GloveId);
        Assert.Equal("soot", hop.FielderId);
        Assert.True(lily.OverReach > 0, "the ball passed through the shortstop's reach");
        Assert.NotNull(lily.Play);
        Assert.False(lily.Play!.Kind == PlayKind.FlyOut && lily.Play.Fielder?.Id == "soot", "the hopped glove does not catch it");
    }

    [Fact]
    public void ALilyHopNoInfieldGloveMeetsFliesItsStraightLine()
    {
        var far = Live(105, 0, "pond-skip", Frame);
        Assert.Empty(far.Hops);
    }

    static (List<GloveHopped> Hops, int OverReach, PlayEvent? Play) Live(double exit, double spray, string swing, double dt)
    {
        var home = Game.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "soot", "vine", "moss", "hex");
        var away = Game.Team("Offense", "reed", "boom", "cinder", "grit", "rio", "nugget", "nico", "gull", "marlow");
        var match = Match.Exhibition(Game, home, away, 3, 1, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Hit(match.Park, exit, LilyHop.LaunchDeg!.Value, spray, rules: match.Rules) with { StarSwingUsed = swing };
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0,
            LivePlayCommandSource.Cpu)).Snapshot.Active);
        var straight = BattedBall.Of(hit, match.Park, match.Rules).Samples;
        var hops = new List<GloveHopped>();
        var over = 0;
        PlayEvent? play = null;
        var standing = match.Rules.Fielding.Catch.StandingHeightFt;
        for (var i = 0; i < 12 / dt && play is null; i++)
        {
            play = live.Apply(LivePlayCommand.Tick(dt, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
            if (play is not null) break;
            hops.AddRange(live.Facts.OfType<GloveHopped>());
            if (live.HoldsBall || live.Path is null || live.ElapsedSeconds >= BallFlight.HangTime(straight, match.Rules)) continue;
            // The live ball is the path every glove and the CPU read, and on the straight ball's ground track.
            var at = BallFlight.PointAt(live.Path, live.ElapsedSeconds, match.Rules);
            Assert.Equal(at.Y, live.BallY, 9);
            var line = BallFlight.PointAt(straight, live.ElapsedSeconds, match.Rules);
            Assert.Equal(line.X, live.BallX, 6);
            Assert.Equal(line.Z, live.BallZ, 6);
            if (hops.Count == 0) Assert.Equal(line.Y, live.BallY, 6);
            // Inside the least reach a body class has (3.6 ft), the hopped glove's ball is over the standing catch height.
            if (hops.FirstOrDefault() is { } h
                && (h.GloveId == live.GlovePos ? (X: live.GloveX, Z: live.GloveZ) : live.Fielders[h.GloveId]) is var body
                && Diamond.Dist(body.X, body.Z, live.BallX, live.BallZ) < 3.5)
            {
                over++;
                Assert.True(live.BallY > standing, $"t={live.ElapsedSeconds:F3}: the ball is {live.BallY:F2} ft up inside the glove's reach");
            }
        }
        return (hops, over, play);
    }

    // ---------------------------------------------------------------------------------
    // The rows are read strictly
    // ---------------------------------------------------------------------------------

    [Fact]
    public void BadSkipsAHopOnAPitchSkipsOnASwingOrAWildHopAreRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["leapfrog"]!["skips"]!["secondAt"] = 0.5;
            json["pitches"]!["fastball"]!["hop"] = new JsonObject { ["heightFt"] = 5, ["padFt"] = 0.5, ["rampFt"] = 1 };
            json["swings"]!["pond-skip"]!["hop"]!["heightFt"] = 12;
            json["swings"]!["line"]!["skips"] = new JsonObject { ["firstAt"] = 0.5, ["secondAt"] = 0.7, ["hopFt"] = 1 };
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("star pitch 'leapfrog' skips needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'fastball' cannot carry a hop", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'pond-skip' hop needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'line' cannot carry skips", StringComparison.Ordinal));
    }

    [Fact]
    public void TheRetiredLeapAndFirstHopBounceKeysAreRefusedByName()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["swings"]!["pond-skip"]!["firstHopBounceMul"] = 2.2;
            json["pitches"]!["leapfrog"]!["leap"] = new JsonObject { ["at"] = 0.35, ["holdSpan"] = 0.25, ["holdPace"] = 0.2 };
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("swings.pond-skip.firstHopBounceMul is not a key this file declares", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("pitches.leapfrog.leap is not a key this file declares", StringComparison.Ordinal));
    }
}
