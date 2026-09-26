using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Reed's three (#1151, WD-15 A, WD-22; spec §13, §8.4): Leapfrog's pace and Pond Skip's first hop; his field ability is the pool's Lick Catch (AB-12).
/// The pitch keeps its path, crossing and arrival; the hop is the ground physics.
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
    // The rows are read strictly
    // ---------------------------------------------------------------------------------

    [Fact]
    public void ALeapThatNeverEndsOrABounceOnAPitchOrAWildBounceIsRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["leapfrog"]!["leap"]!["holdSpan"] = 0.7;
            json["pitches"]!["fastball"]!["firstHopBounceMul"] = 2;
            json["swings"]!["pond-skip"]!["firstHopBounceMul"] = 5;
            json["swings"]!["line"]!["leap"] = new JsonObject { ["at"] = 0.3, ["holdSpan"] = 0.2, ["holdPace"] = 0.1 };
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("star pitch 'leapfrog' leap needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'fastball' cannot carry firstHopBounceMul", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'pond-skip' firstHopBounceMul must be", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'line' cannot carry a leap", StringComparison.Ordinal));
    }
}
