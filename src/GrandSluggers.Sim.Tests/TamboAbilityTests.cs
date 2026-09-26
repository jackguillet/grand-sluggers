using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Tambo's two (character id <c>konga</c>; spec §13): Vine Swing swings the pitch in on one pendulum arc onto the crossing it
/// always had, and Lightning Liner jags its ball sideways twice in the air and lands it where the straight liner lands. Each
/// bends how the ball travels; the crossing, the landing and the gloves still decide the play.
/// </summary>
public sealed class TamboAbilityTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    static PitchPendulum Vine => Game.StarSkills.Pitch("caskball")!.Pendulum!;
    static StarSwingSkill Lightning => Game.StarSkills.Swing("cask-swing")!;

    [Fact]
    public void TamboCarriesHisOwnTwoAndTheOldPayloadsAreGone()
    {
        var tambo = Game.Must("konga");
        Assert.Equal("Tambo", tambo.Name);
        Assert.Equal("caskball", tambo.StarPitch);
        Assert.Equal("cask-swing", tambo.StarSwing);
        Assert.DoesNotContain(Game.Characters.Values, c => c.Id != "konga" && (c.StarPitch == "caskball" || c.StarSwing == "cask-swing"));
        var pitch = Game.StarSkills.Pitch("caskball")!;
        Assert.Equal("Vine Swing", pitch.Name);
        Assert.Null(pitch.OnCatch);
        Assert.Equal(1.0, pitch.SpeedMul);
        Assert.Equal("Lightning Liner", Lightning.Name);
        Assert.Equal(1.2, Lightning.ExitVeloMul);
        Assert.Equal(18, Lightning.LaunchDeg);
        Assert.Equal(BallJag.MaxOffsetFt, Lightning.Jag!.OffsetFt);
    }

    // ---------------------------------------------------------------------------------
    // Vine Swing
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// S-222: the ball is on one circle about the vine's pivot the whole flight, swings out wide and then in on one arc, and
    /// crosses on the ordinary spot at the ordinary instant and speed.
    /// </summary>
    [Theory]
    [InlineData("fastball", 0.0, 0.0)]
    [InlineData("curveball", 0.5, -0.4)]
    [InlineData("sinker", -0.6, 0.5)]
    public void S222_VineSwingSwingsInOnOneArcOntoTheOrdinaryCrossing(string family, double aimX, double aimY)
    {
        var rules = Game.Rules;
        var star = new PitchCommand(family, 0.3, true, aimX, aimY);
        var plain = star with { Star = false };
        // The ordinary crossing, the ordinary instant (u is the flight's own time) and the ordinary speed.
        Assert.Equal(PitchFlight.Point(plain, 1, rules), PitchFlight.Point(star, 1, rules, "caskball", skills: Game.StarSkills));
        Assert.Equal(PitchFlight.Crossing(plain, rules), PitchFlight.Crossing(star, rules, "caskball"));
        Assert.Equal(AtBatResolver.PitchSpeedMph(plain, 6, rules), AtBatResolver.PitchSpeedMph(star, 6, rules, "caskball", Game.StarSkills));
        var cross = PitchFlight.Crossing(plain, rules);
        var side = cross.X > 0 ? -1 : 1;
        var pivotAtPlate = PitchFlight.Pivot(star, 1, rules, "caskball", Game.StarSkills)!.Value;
        Assert.Equal(cross.X, pivotAtPlate.X, 9);
        Assert.Equal(cross.Y + Vine.LengthFt, pivotAtPlate.Y, 9);
        var widest = 0.0;
        var last = 0.0;
        var rising = true;
        for (var u = 0.0; u <= 1.0 + 1e-9; u += 0.01)
        {
            var p = PitchFlight.Point(plain, u, rules);
            var v = PitchFlight.Point(star, u, rules, "caskball", skills: Game.StarSkills);
            var pivot = PitchFlight.Pivot(star, u, rules, "caskball", Game.StarSkills)!.Value;
            // On the circle: the vine is always its own length, and the swing stays in the plane square to the plate.
            var r = Math.Sqrt((v.X - pivot.X) * (v.X - pivot.X) + (v.Y - pivot.Y) * (v.Y - pivot.Y));
            Assert.Equal(Vine.LengthFt, r, 9);
            Assert.Equal(p.Z, v.Z, 9);
            Assert.Equal(pivot.Z, v.Z, 9);
            Assert.True(v.Y >= p.Y - 1e-12, $"u={u}: the vine never swings the ball below its ordinary path");
            var off = (v.X - p.X) * side;
            Assert.True(off >= -1e-12, $"u={u}: the ball swings in from the far side of the plate");
            // One arc: out to the widest, then in, never back out.
            if (rising && off < last - 1e-12) rising = false;
            else if (!rising) Assert.True(off <= last + 1e-12, $"u={u}: the swing in never turns back out");
            last = off;
            widest = Math.Max(widest, off);
        }
        Assert.Equal(Vine.LengthFt * Math.Sin(Vine.SwingDeg * Math.PI / 180), widest, 2);
        // Well outside the zone at its widest: past the plate's edge by more than a foot.
        var wide = PitchFlight.Point(star, Vine.WidestAt, rules, "caskball", skills: Game.StarSkills);
        Assert.True(Math.Abs(wide.X) > StrikeZoneGeometry.HalfWidth + 1, $"at its widest the ball is at X {wide.X:F2}");
    }

    /// <summary>S-223: the umpire, the aim tell, the bat and the CPU batter read the one ordinary crossing; the bend ends inside 2 s.</summary>
    [Theory]
    [InlineData(0.0, 0.0, 0.0)]
    [InlineData(0.9, 0.6, 0.0)]
    [InlineData(-0.8, -0.5, -0.7)]
    public void S223_TheUmpireTheBatAndTheCpuReadTheOrdinaryPitch(double aimX, double aimY, double breakX)
    {
        var rules = Game.Rules;
        var star = new PitchCommand("fastball", 0.6, true, aimX, aimY, breakX);
        var plain = star with { Star = false };
        Assert.Equal(PitchFlight.ContactAim(plain, rules), PitchFlight.ContactAim(star, rules, "caskball"));
        Assert.Equal(StrikeZoneGeometry.Contains(plain, rules), StrikeZoneGeometry.Contains(star, rules, "caskball"));
        Assert.Equal(PitchFlight.AimForCrossing(plain, 0.2, -0.1, rules).AimX, PitchFlight.AimForCrossing(star, 0.2, -0.1, rules, "caskball").AimX);
        // The shown flight is the table's: the default table and the catalog's give the same ball.
        Assert.Equal(PitchFlight.Point(star, 0.4, rules, "caskball"), PitchFlight.Point(star, 0.4, rules, "caskball", skills: Game.StarSkills));
        Assert.True(PitchFlight.AirSeconds(AtBatResolver.PitchSpeedMph(star, 1, rules, "caskball", Game.StarSkills), rules) < 2);
        // Only the pendulum pitch has a pivot.
        Assert.Null(PitchFlight.Pivot(plain, 0.4, rules, "caskball", Game.StarSkills));
        Assert.Null(PitchFlight.Pivot(star, 0.4, rules, "leapfrog", Game.StarSkills));
    }

    // ---------------------------------------------------------------------------------
    // Lightning Liner
    // ---------------------------------------------------------------------------------

    static AtBatResult LightningHit(Park park, double exit, double spray, RulesTable rules)
    {
        var jag = Lightning.Jag!;
        var launch = Lightning.LaunchDeg!.Value;
        var ball = BattedBall.Of(exit, launch, spray, false, park, rules, 1, jag);
        return new AtBatResult(ContactQuality.Nice, !ball.Foul, false, exit, launch, Math.Round(ball.LandingDist, 1),
            ball.HomeRun, false, null, "cask-swing", SprayDeg: spray, Foul: ball.Foul, Class: ball.Shape, Jag: jag);
    }

    /// <summary>
    /// S-224: the jagged ball jags out up to 3 ft square to its line toward the middle of the field, runs beside it, jags back,
    /// and lands on exactly the straight ball's spot at the straight ball's time; the fence, the wall and the chalk are the
    /// straight ball's.
    /// </summary>
    [Theory]
    [InlineData(80, 0)]
    [InlineData(95, -25)]
    [InlineData(105, 20)]
    [InlineData(120, 8)]
    public void S224_LightningLinerJagsTwiceAndLandsWhereTheStraightLinerLands(double exit, double spray)
    {
        var rules = Game.Rules;
        var park = Game.Parks[ParkId.Harbor];
        var jag = Lightning.Jag!;
        var launch = Lightning.LaunchDeg!.Value;
        var straight = BattedBall.Of(exit, launch, spray, false, park, rules);
        var bolt = BattedBall.Of(exit, launch, spray, false, park, rules, 1, jag);
        Assert.Equal(straight.Samples.Count, bolt.Samples.Count);
        Assert.Equal((straight.LandingX, straight.LandingZ, straight.HangT), (bolt.LandingX, bolt.LandingZ, bolt.HangT));
        Assert.Equal((straight.Shape, straight.Foul, straight.HomeRun, straight.LeavesT, straight.WallT),
            (bolt.Shape, bolt.Foul, bolt.HomeRun, bolt.LeavesT, bolt.WallT));
        var window = BallJag.WindowSec(straight.Samples);
        Assert.InRange(window, 0.1, BallJag.WithinSec);
        // The line the straight ball flies, and the side toward the middle of the field.
        var land = straight.Samples[BallFlight.LandingIndex(straight.Samples)];
        var (lx, lz) = (land.X / Math.Sqrt(land.X * land.X + land.Z * land.Z), land.Z / Math.Sqrt(land.X * land.X + land.Z * land.Z));
        var outs = 0;
        var widest = 0.0;
        var wasOff = false;
        for (var i = 0; i < straight.Samples.Count; i++)
        {
            var (s, b) = (straight.Samples[i], bolt.Samples[i]);
            Assert.Equal((s.T, s.Height, s.Event), (b.T, b.Height, b.Event));
            var (dx, dz) = (b.X - s.X, b.Z - s.Z);
            var off = Math.Sqrt(dx * dx + dz * dz);
            if (s.T >= window) Assert.Equal((s.X, s.Z), (b.X, b.Z));
            if (off > 1e-9)
            {
                Assert.Equal(0, dx * lx + dz * lz, 6);
                Assert.True(dx * land.X <= 1e-9, "the jag leans toward the middle of the field, never toward a foul line");
                Assert.Equal(jag.OffFt(s.T / window), off, 9);
            }
            Assert.True(off <= BallJag.MaxOffsetFt + 1e-9);
            var isOff = off > jag.OffsetFt - 1e-9;
            if (isOff && !wasOff) outs++;
            wasOff = isOff;
            widest = Math.Max(widest, off);
        }
        Assert.Equal(1, outs);
        Assert.Equal(jag.OffsetFt, widest, 9);
    }

    [Fact]
    public void TheResolverTagsTheLightningBallAndEveryFlightOfItReadsTheTag()
    {
        var park = Game.Parks[ParkId.Harbor];
        var resolver = new AtBatResolver(Game.Chemistry, Game.Rules, Game.StarSkills);
        var input = new AtBatInput(Game.Must("vale"), Game.Must("konga"), null, [], ChargePitch: false, ChangeupPitch: false,
            TimingErrorFrames: 0, UseStarPitch: false, UseStarSwing: true, Bat: null, PitcherStamina: 80,
            CrossingX: 0, CrossingY: 2.5);
        var hit = resolver.Resolve(input, park, new Random(4));
        Assert.Equal("cask-swing", hit.StarSwingUsed);
        Assert.Equal(Lightning.LaunchDeg, hit.LaunchDeg);
        Assert.Same(Lightning.Jag, hit.Jag);
        Assert.Equal(BattedBall.Of(hit.ExitVeloMph, hit.LaunchDeg, hit.SprayDeg, false, park, Game.Rules, 1, Lightning.Jag).Samples,
            BattedBall.Of(hit, park, Game.Rules).Samples);
        var ordinary = resolver.Resolve(input with { UseStarSwing = false }, park, new Random(4));
        Assert.Null(ordinary.Jag);
        Assert.Null(resolver.Resolve(input with { Bunt = true }, park, new Random(4)).Jag);
    }

    /// <summary>
    /// S-225: the live ball is the jagged ball every frame, for the gloves and the CPU alike, and a glove waiting at the end of
    /// the line — the straight ball's landing — takes it for the out.
    /// </summary>
    [Fact]
    public void S225_AGloveAtTheEndOfTheLineTakesTheLightningLiner()
    {
        var home = Game.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "soot", "vine", "moss", "hex");
        var away = Game.Team("Offense", "rio", "boom", "cinder", "grit", "zig", "nugget", "nico", "gull", "konga");
        var match = Match.Exhibition(Game, home, away, 3, 1, parkId: ParkId.Harbor);
        // A liner up the middle that lands on the center fielder's spot: the glove at the end of the line.
        var hit = LightningHit(match.Park, 101, 0, match.Rules);
        var straight = BattedBall.Of(hit.ExitVeloMph, hit.LaunchDeg, hit.SprayDeg, false, match.Park, match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.False(preview.Foul);
        Assert.Equal((straight.LandingX, straight.LandingZ), (preview.LandingX, preview.LandingZ));
        // The preview (the CPU's read and the pursuit plan) is the jagged path, not the straight one.
        Assert.Equal(BattedBall.Of(hit, match.Park, match.Rules).Samples, preview.Ball!.Samples);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, HumanGlove, 0,
            LivePlayCommandSource.Human)).Snapshot.Active);
        var end = (X: straight.LandingX, Z: straight.LandingZ);
        var path = live.Path!;
        var window = BallJag.WindowSec(path);
        var offLine = 0.0;
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 12 && play is null; i++)
        {
            var dx = end.X - live.GloveX;
            var dz = end.Z - live.GloveZ;
            var d = Math.Sqrt(dx * dx + dz * dz);
            var mag = d > 1 ? 1.0 : 0.0;
            var pad = live.HoldsBall ? LivePadInput.Dead
                : new LivePadInput(StickX: dx / Math.Max(1e-6, d) * mag, StickY: dz / Math.Max(1e-6, d) * mag);
            var t = live.ElapsedSeconds;
            if (t > 0 && t < window && ReferenceEquals(path, live.Path))
            {
                var at = BallFlight.PointAt(path, t, match.Rules);
                Assert.Equal(at.X, live.BallX, 6);
                Assert.Equal(at.Z, live.BallZ, 6);
                var plain = BallFlight.PointAt(straight.Samples, t, match.Rules);
                offLine = Math.Max(offLine, Math.Sqrt((at.X - plain.X) * (at.X - plain.X) + (at.Z - plain.Z) * (at.Z - plain.Z)));
            }
            play = live.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false, LivePlayCommandSource.Human)).CompletedPlay;
        }
        Assert.True(offLine > 2, $"the live ball ran {offLine:F2} ft off its line");
        Assert.NotNull(play);
        Assert.Equal(PlayKind.FlyOut, play!.Kind);
    }

    // ---------------------------------------------------------------------------------
    // The rows are read strictly
    // ---------------------------------------------------------------------------------

    [Fact]
    public void AWildVineAJagOnAPitchATooWideJagOrAPendulumOnASwingIsRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["caskball"]!["pendulum"]!["swingDeg"] = 120;
            json["pitches"]!["fastball"]!["jag"] = new JsonObject { ["offsetFt"] = 1, ["firstAt"] = 0.2, ["secondAt"] = 0.5, ["span"] = 0.1 };
            json["swings"]!["cask-swing"]!["jag"]!["offsetFt"] = 5;
            json["swings"]!["line"]!["pendulum"] = new JsonObject { ["lengthFt"] = 4, ["swingDeg"] = 30, ["widestAt"] = 0.3 };
            json["swings"]!["ground"]!["jag"] = new JsonObject { ["offsetFt"] = 2, ["firstAt"] = 0.6, ["secondAt"] = 0.5, ["span"] = 0.1 };
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("star pitch 'caskball' pendulum needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'fastball' cannot carry a jag", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'cask-swing' jag needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'line' cannot carry a pendulum", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'ground' jag needs", StringComparison.Ordinal));
    }

    [Fact]
    public void TheRetiredFragmentsKeyIsRefusedByName()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json => json["swings"]!["cask-swing"]!["fragments"] = true);
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("fragments", StringComparison.Ordinal));
    }
}
