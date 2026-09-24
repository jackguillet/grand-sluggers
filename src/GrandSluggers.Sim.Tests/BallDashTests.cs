using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-2 slice 3 (#718, F693-02-ball-dash-carrier, F693-02-ordinary-carry-speed): Ball Dash is a field ability, and its
/// holders carry the ball at 1.20× their pursuit speed with no press, no timer and no change to their response rates.
/// The roster gives it to dart, pip and jester, and there is no universal East-held sprint.
/// </summary>
public sealed class BallDashTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);
    static readonly string[] Carriers = ["dart", "jester", "pip"];

    [Fact]
    public void TheRosterGivesBallDashToThreeFastRolePlayers()
    {
        Assert.Equal(Carriers, Game.Characters.Values.Where(FieldAbilities.HasBallDash).Select(c => c.Id).OrderBy(id => id, StringComparer.Ordinal));

        // The three fastest role players.
        Assert.Equal(("ball-dash", 9), (Game.Must("dart").FieldAbility, Game.Must("dart").Stats.Run));
        Assert.Equal(("ball-dash", 8), (Game.Must("pip").FieldAbility, Game.Must("pip").Stats.Run));
        Assert.Equal(("ball-dash", 8), (Game.Must("jester").FieldAbility, Game.Must("jester").Stats.Run));
        Assert.Equal("Ball Dash", CharacterCard.Title("ball-dash"));
    }

    [Fact]
    [Trait("Kind", "Balance")]
    public void TheAbilityIsWorthOneTwentyAndThereIsNoSprint()
    {
        Assert.Equal((1.0, 1.20), (Game.Rules.Fielding.Dash.ChaseMul, Game.Rules.Fielding.Abilities.BallDashMul));

        var dart = Game.Must("dart");
        var zig = Game.Must("zig");   // Run 9 like dart, no Ball Dash
        Assert.Equal(1.20, FieldAbilities.CarryMul(dart, Game.Rules));
        Assert.Equal(1.0, FieldAbilities.CarryMul(zig, Game.Rules));

        // The carry speed: the boost for a holder, the very same double for everyone else.
        var v = FieldingResolver.ChaseSpeedFt(zig, false, Game.Rules);
        Assert.Equal(22.48, v, 9);
        Assert.Equal(v * 1.20, FieldingResolver.CarrySpeedFt(dart, v, Game.Rules), 12);
        Assert.Equal(v, FieldingResolver.CarrySpeedFt(zig, v, Game.Rules));

        // East held is no sprint.
        Assert.Equal(v, FieldingResolver.ChaseSpeedFt(zig, false, Game.Rules, dash: true));
    }

    /// <summary>
    /// The human shortstop takes a grounder, stands, then runs with the ball. dart (Ball Dash) and zig (no ability of the
    /// feet, the same Run 9) build at the same rate — half the rated speed after six frames, the rated speed after twelve —
    /// and part only at the cap: dart settles at 1.20 × 22.48 = 26.98 ft/s, zig at 22.48. The rate is the body's, the cap
    /// is the ability's (F693-02-carry-movement-response).
    /// </summary>
    [Theory]
    [InlineData("dart", 1.20)]
    [InlineData("zig", 1.0)]
    public void ACarrierBuildsAtItsOwnRateAndRunsTheBallAtTheCap(string shortstop, double cap)
    {
        var (live, rated) = HumanShortstopHoldsTheBall(Game, shortstop);
        var track = Push(live, frames: 24);
        double Speed(int k) => Diamond.Dist(track[k].X, track[k].Z, track[k + 1].X, track[k + 1].Z) / Frame;

        Assert.Equal(22.48, rated, 9);
        Assert.InRange(Speed(5), rated * 0.5 * 0.85, rated * 0.5 * 1.15);      // six frames in: half the rated speed, boost or not
        Assert.InRange(Speed(11), rated * 0.90, rated * 1.10);                 // twelve frames in: the rated speed, boost or not
        Assert.InRange(Speed(20), rated * cap * 0.97, rated * cap * 1.03);     // settled: the cap is the ability's
        Assert.InRange(Speed(23), rated * cap * 0.97, rated * cap * 1.03);
    }

    /// <summary>
    /// The CPU's own carry (F693-02-ball-dash-carrier: "actual ownership for both seats and CPU"). A grounder the first
    /// baseman takes 30-odd feet from the bag with nobody else to cover it: his legs beat any throw, so he walks it there
    /// himself. dart and zig brake to a stop, wait out the read, then build at the same 1.87 ft/s a frame; zig holds at
    /// 22.5 ft/s from the twelfth frame and dart parts from him there to settle at 27.0 on the fifteenth. The forecast that
    /// chose the walk (`CpuWalkSec`) read the same speed.
    /// </summary>
    [Theory]
    [InlineData("dart", 1.20)]
    [InlineData("zig", 1.0)]
    public void TheCpuFirstBasemanCarriesAGrounderToTheBagAtTheCap(string first, double cap)
    {
        var home = Game.Team("Defense", "vale", "pewter", first, "frost", "basil", "lace", "vine", "moss", "hex");
        var away = Game.Team("Offense", "rio", "boom", "cinder", "grit", "soot", "nugget", "nico", "gull", "marlow");
        var match = Match.Exhibition(Game, home, away, 3, 1, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Hit(match.Park, 85, -12, 34, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("1B", preview.Position);
        var rated = FieldingResolver.ChaseSpeedFt(Game.Must(first), false, match.Rules);
        Assert.Equal(22.48, rated, 9);

        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        var track = new List<(double X, double Z, bool Held, bool Throwing)>();
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 8 && play is null; i++)
        {
            play = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
            if (play is null) track.Add((live.GloveX, live.GloveZ, live.HoldsBall, live.Throwing));   // the completing frame resets the field
        }
        Assert.NotNull(play);
        Assert.DoesNotContain(track, t => t.Throwing);   // the walk, never a throw
        var held = track.FindIndex(t => t.Held);
        Assert.True(held > 0, "the first baseman never took the grounder");
        var speeds = new List<double>();
        for (var k = held; k < track.Count - 1; k++)
            speeds.Add(Diamond.Dist(track[k].X, track[k].Z, track[k + 1].X, track[k + 1].Z) / Frame);

        // The brake after the take, the stop while the table reads, then the walk: from its first moving frame, the ramp and then the cap.
        var braking = speeds.FindIndex(v => v > 1e-6);
        Assert.True(braking >= 0, "the body never moved after the take");
        var stopped = speeds.FindIndex(braking, v => v < 1e-6);
        Assert.True(stopped > braking, "the body never came to rest after the take");
        var go = speeds.FindIndex(stopped, v => v > 1e-6);
        Assert.True(go > stopped, "the body never walked to the bag");
        var walk = speeds.Skip(go).ToList();
        Assert.InRange(walk[5], rated * 0.5 * 0.9, rated * 0.5 * 1.1);           // six frames in: half the rated speed, the body's own rate
        Assert.InRange(walk[11], rated * 0.95, rated * 1.05);                     // twelve frames in: the rated speed, boost or not
        Assert.InRange(walk.Max(), rated * cap * 0.98, rated * cap * 1.02);        // the cap is the ability's
        Assert.True(walk.Count(v => v > rated * cap * 0.98) >= 3, "the body never held the cap");
    }

    // ---------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------

    /// <summary>A grounder to short on the human seat with a dead stick: the CPU takes it for the player, who then stands still until at rest.</summary>
    static (LivePlaySystem Live, double Rated) HumanShortstopHoldsTheBall(ContentCatalog content, string shortstop)
    {
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", shortstop, "vine", "moss", "hex");
        var away = content.Team("Offense", "rio", "boom", "cinder", "grit", "soot", "nugget", "nico", "gull", "marlow");
        var match = Match.Exhibition(content, home, away, 3, 1, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Hit(match.Park, 85, -12, -18, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, HumanGlove, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        var held = false;
        for (var i = 0; i < 60 * 6 && !held; i++)
        {
            live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Human));
            held = live.HoldsBall && live.GlovePos == "SS" && !live.Throwing;
        }
        Assert.True(held, $"{shortstop} never took the grounder");
        // Stand until at rest (the brake is 0.10 s).
        for (var i = 0; i < 12; i++)
            live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Human));
        Assert.True(live.HoldsBall && live.Active);
        return (live, FieldingResolver.ChaseSpeedFt(content.Must(shortstop), false, match.Rules));
    }

    /// <summary>Full stick toward the outfield for <paramref name="frames"/> frames; the glove's position before each step and after the last.</summary>
    static List<(double X, double Z)> Push(LivePlaySystem live, int frames)
    {
        var track = new List<(double X, double Z)> { (live.GloveX, live.GloveZ) };
        for (var i = 0; i < frames; i++)
        {
            live.Apply(LivePlayCommand.Tick(Frame, new LivePadInput(StickX: 0, StickY: 1), LivePadInput.Dead, false, LivePlayCommandSource.Human));
            Assert.True(live.HoldsBall, $"the glove lost the ball {i} frames into the push");
            track.Add((live.GloveX, live.GloveZ));
        }
        return track;
    }
}
