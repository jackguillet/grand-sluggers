using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Sable's three (#1149, WD-15 A; spec §13, §8.4): the Mirage Ball's twin and the Sidewinder's first-hop kick; his field
/// ability is the pool's Snap Throw (AB-12). Each changes what the eye sees or how the ball or a glove moves; the ball, the bodies and the
/// geometry still decide the play.
/// </summary>
public sealed class SableAbilityTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;

    [Fact]
    public void SableCarriesHisOwnThree()
    {
        var sable = Game.Must("sable");
        Assert.Equal("mirageball", sable.StarPitch);
        Assert.Equal("sidewinder", sable.StarSwing);
        Assert.Equal(FieldAbilityId.SnapThrow, sable.FieldAbility);   // the shared field pool (AB-12)
        Assert.DoesNotContain(Game.Characters.Values, c => c.Id != "sable"
            && (c.StarPitch == "mirageball" || c.StarSwing == "sidewinder"));
    }

    // ---------------------------------------------------------------------------------
    // Mirage Ball
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(0.6, 0.2)]
    [InlineData(-0.5, -0.3)]
    [InlineData(0, 0)]
    public void TheMirageTwinFliesOnTheFarSideAndIsGoneBeforeHalfTheFlight(double aimX, double aimY)
    {
        var rules = Game.Rules;
        var twin = Game.StarSkills.Pitch("mirageball")!.Twin!;
        var pitch = new PitchCommand("sinker", 0, true, aimX, aimY);
        var crossing = PitchFlight.Crossing(pitch, rules, "mirageball");
        for (var u = 0.0; u <= 1.0001; u += 0.01)
        {
            var real = PitchFlight.Point(pitch, u, rules, "mirageball");
            var shown = PitchFlight.Twin(pitch, u, rules, "mirageball", Game.StarSkills);
            if (u >= PitchTwin.GoneBy)
            {
                Assert.Null(shown);   // the second half of every flight shows one ball
                continue;
            }
            if (u < twin.FadeFrom) Assert.Equal(1, shown!.Value.Alpha);
            if (shown is not { } t) continue;
            Assert.Equal(real.Y, t.Y, 9);
            Assert.Equal(real.Z, t.Z, 9);
            Assert.Equal(twin.OffsetFt, Math.Abs(t.X - real.X), 9);
            Assert.True(Math.Sign(t.X - real.X) != Math.Sign(crossing.X) || crossing.X == 0 && t.X < real.X,
                $"u={u}: the twin must fly on the far side of the zone from the real crossing");
        }
    }

    [Fact]
    public void TheRealMirageBallIsThePitchAsThrownSoTheUmpireAndTheBatReadOneBall()
    {
        var rules = Game.Rules;
        var star = new PitchCommand("sinker", 0.4, true, 0.3, -0.2, BreakX: 0.5);
        var plain = star with { Star = false };
        for (var u = 0.0; u <= 1.0001; u += 0.05)
            Assert.Equal(PitchFlight.Point(plain, u, rules, "mirageball"), PitchFlight.Point(star, u, rules, "mirageball"));
        Assert.Equal(PitchFlight.Crossing(plain, rules), PitchFlight.Crossing(star, rules, "mirageball"));
        // No twin without the star, and none for a pitch whose row names none.
        Assert.Null(PitchFlight.Twin(plain, 0.1, rules, "mirageball", Game.StarSkills));
        Assert.Null(PitchFlight.Twin(star, 0.1, rules, "phonyball", Game.StarSkills));
    }

    // ---------------------------------------------------------------------------------
    // Sidewinder
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(-10)]
    [InlineData(12)]
    [InlineData(22)]
    [InlineData(30)]
    public void TheSidewinderTurnsAtItsFirstHopAwayFromTheChasingFielderAndTheGlovesDecide(double spray)
    {
        var kicked = Run(spray, "sidewinder");
        var plain = Run(spray, "line");
        Assert.Empty(plain.Kicks);
        var kick = Assert.Single(kicked.Kicks);
        Assert.Equal("sidewinder", kick.SwingId);
        Assert.Equal(20, Math.Abs(kick.TurnDeg), 9);
        // Up to the hop the two balls are the same ball; off it, the kicked ball heads the turn's way, away from its chaser.
        var at = kicked.Balls.FindIndex(b => b.T >= kick.T);
        Assert.True(at > 0 && at + 1 < plain.Balls.Count && at + 1 < kicked.Balls.Count);
        Assert.Equal(plain.Balls[at - 1].X, kicked.Balls[at - 1].X, 9);
        Assert.Equal(plain.Balls[at - 1].Z, kicked.Balls[at - 1].Z, 9);
        var plainHeading = Heading(plain.Balls[at], plain.Balls[at + 1]);
        var kickedHeading = Heading(kicked.Balls[at], kicked.Balls[at + 1]);
        Assert.Equal(kick.TurnDeg, Wrap(kickedHeading - plainHeading), 1);
        var (fx, fz) = kicked.AwayAt;
        var toChaser = Math.Atan2(fz - kick.Z, fx - kick.X) * 180 / Math.PI;
        Assert.True(Math.Abs(Wrap(kickedHeading - toChaser)) > Math.Abs(Wrap(plainHeading - toChaser)),
            $"spray {spray}: turned toward {kick.GloveId}");
        // The play still ends the ordinary way: a glove, a throw, a bag, or the ball through for a hit.
        Assert.NotNull(kicked.Play);
    }

    [Fact]
    public void ASidewinderCaughtBeforeItsHopNeverKicks()
    {
        // Up the middle the pitcher gloves the liner before it lands: a catch, and no hop to turn.
        var run = Run(-4, "sidewinder");
        Assert.Empty(run.Kicks);
        Assert.Equal(PlayKind.FlyOut, run.Play!.Kind);
    }

    static double Heading((double T, double X, double Z) a, (double T, double X, double Z) b) =>
        Math.Atan2(b.Z - a.Z, b.X - a.X) * 180 / Math.PI;

    static double Wrap(double deg) => ((deg % 360) + 540) % 360 - 180;

    [Fact]
    public void TheKickTurnsAwayFromTheBodyWhicheverSideItStandsOn()
    {
        // The ball runs up +z from the origin; a body on its left (x < 0) sends it right (clockwise, negative), and back.
        Assert.Equal(-20, LivePlaySystem.FirstHopTurnDeg(0, 1, 0, 0, -3, 5, 20));
        Assert.Equal(20, LivePlaySystem.FirstHopTurnDeg(0, 1, 0, 0, 3, 5, 20));
        foreach (var (fx, fz) in new[] { (-3.0, 5.0), (3.0, 5.0), (-6.0, 1.0), (4.0, 9.0) })
        {
            var turn = LivePlaySystem.FirstHopTurnDeg(0, 1, 0, 0, fx, fz, 20) * Math.PI / 180;
            var (kx, kz) = (-Math.Sin(turn), Math.Cos(turn));
            Assert.True(Diamond.Dist(kx * 10, kz * 10, fx, fz) > Diamond.Dist(0, 10, fx, fz), $"({fx},{fz})");
        }
    }

    /// <summary>A hard grounder off Sable's bat with the named star swing, on CPU gloves at Harbor.</summary>
    static (List<FirstHopKicked> Kicks, List<(double T, double X, double Z)> Balls, (double X, double Z) AwayAt, PlayEvent? Play) Run(
        double spray, string swing)
    {
        var home = Game.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "soot", "vine", "moss", "hex");
        var away = Game.Team("Offense", "sable", "boom", "cinder", "grit", "rio", "nugget", "nico", "gull", "marlow");
        var match = Match.Exhibition(Game, home, away, 3, 1, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Hit(match.Park, 92, 4, spray, rules: match.Rules) with { StarSwingUsed = swing };
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0,
            LivePlayCommandSource.Cpu)).Snapshot.Active);
        var kicks = new List<FirstHopKicked>();
        var balls = new List<(double T, double X, double Z)>();
        var awayAt = (X: 0.0, Z: 0.0);
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 12 && play is null; i++)
        {
            play = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
            if (play is not null) break;   // the completing frame resets the field
            balls.Add((live.ElapsedSeconds, live.BallX, live.BallZ));
            foreach (var k in live.Facts.OfType<FirstHopKicked>())
            {
                kicks.Add(k);
                awayAt = live.Fielders[k.GloveId];
            }
        }
        return (kicks, balls, awayAt, play);
    }

    // ---------------------------------------------------------------------------------
    // The rows are read strictly
    // ---------------------------------------------------------------------------------

    [Fact]
    public void ATwinThatOutlivesHalfTheFlightOrAKickOnThePitchIsRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["mirageball"]!["twin"]!["fadeTo"] = 0.6;
            json["pitches"]!["fastball"]!["firstHopKickDeg"] = 10;
            json["swings"]!["sidewinder"]!["firstHopKickDeg"] = 90;
            json["swings"]!["line"]!["twin"] = new JsonObject { ["offsetFt"] = 1, ["fadeFrom"] = 0.1, ["fadeTo"] = 0.2 };
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("star pitch 'mirageball' twin needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'fastball' cannot carry firstHopKickDeg", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'sidewinder' firstHopKickDeg must be", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'line' cannot carry a twin", StringComparison.Ordinal));
    }
}
