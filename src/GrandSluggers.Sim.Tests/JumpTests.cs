using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-3 slice 3 (#719, F693-02-normal-jump-* and -jump-catch-throw-readiness): a fresh eligible West press is a takeoff — 2.0 ft
/// of root rise over 0.60 s, the same for every character, one profile per press — a blocked press is remembered 0.10 s, the
/// airborne body answers the stick at a tenth of its ground rates, a neutral body coasts, and a jumping catch throws only after it
/// lands. With jumpAirSec 0 the jump is the old arm window instead.
/// </summary>
public sealed class JumpTests
{
    static readonly ContentCatalog Game = ContentCatalog.Load();
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    [Fact]
    [Trait("Kind", "Balance")]
    public void TheJumpIsAnArc()
    {
        Assert.False((Rules.Default.Fielding.Catch with { JumpAirSec = 0 }).JumpArc);
        var t = Game.Rules.Fielding.Catch;
        Assert.True(t.JumpArc);
        Assert.Equal((0.60, 2.0, 0.10, 0.10, 0.0), (t.JumpAirSec, t.JumpRiseFt, t.JumpBufferSec, t.JumpAirResponseMul, t.JumpReachFt));
    }

    /// <summary>West once the read is over: two feet at the apex on the eighteenth frame, a foot and a half a quarter of the way in, back on the ground after 0.60 s — basil and zig alike.</summary>
    [Theory]
    [InlineData("basil")]
    [InlineData("zig")]
    [Trait("Kind", "Balance")]
    public void TheArcIsTwoFeetOverPointSixSecondsForEveryBody(string centre)
    {
        var (match, hit, preview) = Fixture(Game, centre);
        var live = Begin(match, hit, preview);
        Run(live, 60, _ => LivePadInput.Dead);                                   // 1.0 s in: the read is long over, the ball is high
        var heights = new List<double>();
        var takeoffs = 0;
        Run(live, 40, i =>
        {
            heights.Add(live.JumpHeightFt);
            if (live.Events.Contains(LiveEvent.JumpTakeoff)) takeoffs++;
            return i == 0 ? new LivePadInput(WestDown: true) : LivePadInput.Dead;
        }, afterEach: () => heights[^1] = live.JumpHeightFt);
        Assert.Equal(1, takeoffs);
        Assert.Equal(0, heights[0], 9);                                          // the takeoff frame: the clock starts here
        Assert.Equal(2.0, heights[18], 9);                                       // 0.30 s: the apex
        Assert.Equal(1.5, heights[9], 9);                                        // 0.15 s: 8 × 0.25 × 0.75
        Assert.Equal(1.5, heights[27], 9);                                       // 0.45 s: coming down
        Assert.Equal(0, heights[36], 9);                                         // 0.60 s: landed
        Assert.False(live.Airborne);
    }

    /// <summary>
    /// The player owns the timing. A manual stick holds the centre fielder on the plant (no assistance catch); West 0.30 s
    /// before the ball lands is a jumping catch, and the runner on third tags, so the seat's South presses through the air are
    /// held and the throw leaves at landing — takeoff plus 0.60 s. A jump 1.2 s early lands before the ball; the ordinary glove can still catch automatically.
    /// </summary>
    [Theory]
    [InlineData(0.30, true)]
    [InlineData(1.20, false)]
    public void TheJumpCatchesOnlyWhenTheBodyIsInTheAirAndTheThrowWaitsForTheLanding(double lead, bool catches)
    {
        var (match, hit, preview) = Fixture(Game, "basil");
        Assert.True(match.StationRunner(3, Game.Must("konga")));
        var live = match.LivePlay;
        live.Recording = true;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, HumanGlove, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        var hang = preview.HangTimeSec;
        var plant = FlyCatch.ChaseTarget(preview, match.Rules, match.Park);
        PlayEvent? play = null;
        var pressed = false; var takeoffAt = -1.0; var caughtAt = -1.0; var caughtAirborne = false; var throwStartedAt = -1.0; var southWhileAirborne = 0;
        for (var i = 0; i < 60 * 14 && play is null; i++)
        {
            var t = live.ElapsedSeconds;
            LivePadInput pad;
            if (live.HoldsBall && !live.Throwing)
            {
                pad = new LivePadInput(KeysBag: 4, SouthDown: true);
                if (live.Airborne) southWhileAirborne++;
            }
            else if (t < hang - 1.5) pad = LivePadInput.Dead;                                          // the assistance runs the glove to the plant
            else if (!pressed && t >= hang - lead) { pad = Hold(live, plant, 0.21) with { WestDown = true }; pressed = true; }
            else pad = Hold(live, plant, 0.21);                                                         // a manual sliver: no assistance catch
            var r = live.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false, LivePlayCommandSource.Human));
            if (r.CompletedPlay is null)
            {
                if (takeoffAt < 0 && live.Events.Contains(LiveEvent.JumpTakeoff)) takeoffAt = live.ElapsedSeconds;
                if (caughtAt < 0 && live.HoldsBall) { caughtAt = live.ElapsedSeconds; caughtAirborne = live.Airborne; }
                if (throwStartedAt < 0 && live.Throwing) throwStartedAt = live.ElapsedSeconds;
            }
            play = r.CompletedPlay;
        }
        Assert.NotNull(play);
        Assert.True(pressed && takeoffAt > 0, "West never took off");
        if (catches)
        {
            Assert.Equal(PlayKind.FlyOut, play.Kind);
            Assert.Equal(DefensiveFeat.Jump, play.Outcome?.DefensiveFeat);
            Assert.True(caughtAirborne, "the catch came in the air");
            Assert.True(southWhileAirborne > 0, "the seat pressed South before landing");
            Assert.True(throwStartedAt > 0, "the throw never left");
            Assert.InRange(throwStartedAt, takeoffAt + 0.60 - Frame - 1e-9, takeoffAt + 0.60 + 2 * Frame + 1e-9);
        }
        else
        {
            Assert.Equal(PlayKind.FlyOut, play.Kind);
            Assert.False(caughtAirborne);
            Assert.NotEqual(DefensiveFeat.Jump, play.Outcome?.DefensiveFeat);
        }
    }

    /// <summary>A West press 0.10 s before the centre fielder's read is over is remembered and takes off at the first eligible frame; one 0.15 s before is dropped.</summary>
    [Theory]
    [InlineData(0.10, true)]
    [InlineData(0.15, false)]
    public void ABlockedPressIsRememberedForATenthOfASecond(double early, bool takesOff)
    {
        var (match, hit, preview) = Fixture(Game, "basil");
        var live = Begin(match, hit, preview);
        var ready = match.Rules.Fielding.Reaction.OutfieldSec;   // 0.40: the centre fielder cannot move before it
        Assert.Equal(0.40, ready, 9);
        var pressAt = ready - early;
        var pending = false; var airborneAt = -1.0;
        Run(live, 48, i =>
        {
            if (airborneAt < 0 && live.Airborne) airborneAt = live.ElapsedSeconds;
            pending |= live.JumpPending;
            var t = i * Frame;
            return Math.Abs(t - pressAt) < Frame / 2 ? new LivePadInput(WestDown: true) : LivePadInput.Dead;
        }, afterEach: () => { if (airborneAt < 0 && live.Airborne) airborneAt = live.ElapsedSeconds; });
        Assert.True(pending, "the press was never pending");
        if (takesOff) Assert.InRange(airborneAt, ready - 1e-9, ready + 2 * Frame + 1e-9);
        else Assert.True(airborneAt < 0, $"a press {early:0.00} s early took off at {airborneAt:0.00}");
        Assert.False(live.JumpPending);
    }

    /// <summary>West held through the whole jump and past the landing is one takeoff; a fresh press after a release is another.</summary>
    [Fact]
    public void HoldingWestRepeatsNothing()
    {
        var (match, hit, preview) = Fixture(Game, "basil");
        var live = Begin(match, hit, preview);
        Run(live, 60, _ => LivePadInput.Dead);
        var takeoffs = 0;
        Run(live, 90, _ => new LivePadInput(WestDown: true), afterEach: () => { if (live.Events.Contains(LiveEvent.JumpTakeoff)) takeoffs++; });   // 1.5 s held: through the 0.6 s arc and 0.9 s on the ground
        Assert.Equal(1, takeoffs);
        Run(live, 3, _ => LivePadInput.Dead);
        Run(live, 3, i => i == 0 ? new LivePadInput(WestDown: true) : LivePadInput.Dead, afterEach: () => { if (live.Events.Contains(LiveEvent.JumpTakeoff)) takeoffs++; });
        Assert.Equal(2, takeoffs);
    }

    /// <summary>
    /// The accepted air anchors at Run 5 (18 ft/s): full stick from rest carries 1.62 ft over the airtime; the opposite stick from
    /// full speed carries 7.56 ft forward against 10.8 of pure drift — a tenth of the ground rates, the cap untouched.
    /// </summary>
    [Fact]
    [Trait("Kind", "Balance")]
    public void AirborneTheStickWorksAtATenthOfTheGroundRates()
    {
        // From rest: the centre fielder is planted through his read, and West on the first eligible frame with a full stick east.
        var (match, hit, preview) = Fixture(Game, "basil");
        var live = Begin(match, hit, preview);
        // The read is 0.40 s; the tick that reaches it is the first eligible one, so the press lands there and the body has not yet stepped.
        var readyFrames = (int)Math.Round(match.Rules.Fielding.Reaction.OutfieldSec / Frame) - 1;
        Run(live, readyFrames, _ => LivePadInput.Dead);
        var start = (live.GloveX, live.GloveZ);
        var flew = 0; (double X, double Z) landed = default;
        Run(live, 40, i => new LivePadInput(StickX: 1.0, WestDown: i == 0), afterEach: () => { if (live.Airborne) { flew++; landed = (live.GloveX, live.GloveZ); } });
        Assert.Equal(36, flew);   // 0.60 s of air
        // Continuous 1.62 ft; the frame sum at 60 Hz is 9 × (1/60)² × (1 + 2 + … + 36) = 1.665.
        var fromRest = Diamond.Dist(start.Item1, start.Item2, landed.X, landed.Z);
        Assert.InRange(fromRest, 1.62 * 0.95, 1.62 * 1.08);

        // From full speed: the accepted anchor is a reversal from 18 ft/s. Slice 3 recorded it on this centre fielder under the fly, when
        // the outfield's air multiplier was 1.0; at 0.6 (Jack, 2026-09-18) he runs 10.8 ft/s there, so the body at the one speed under a
        // ball in the air is now an infielder: grit (Run 5) at short under a pop, run for a second, then West with the stick reversed.
        (match, hit, preview) = PopFixture(Game);
        live = Begin(match, hit, preview);
        var shortFrames = (int)Math.Round(match.Rules.Fielding.Reaction.ShortSec / Frame) - 1;
        Run(live, shortFrames, _ => LivePadInput.Dead);
        Run(live, 60, _ => new LivePadInput(StickY: -1.0));
        var speed = Diamond.Dist(live.GloveX, live.GloveZ, _lastX, _lastZ) / Frame;
        Assert.InRange(speed, 18.0 * 0.97, 18.0 * 1.03);
        var at = (live.GloveX, live.GloveZ);
        var airborneFrames = 0; (double X, double Z) down = default;
        Run(live, 36, i => new LivePadInput(StickY: 1.0, WestDown: i == 0), afterEach: () => { if (live.Airborne) { airborneFrames++; down = (live.GloveX, live.GloveZ); } });
        Assert.Equal(36, airborneFrames);
        // Continuous 7.56 ft; the frame sum is 10.8 − 18 × (1/60)² × 666 = 7.47.
        var forward = Diamond.Dist(at.Item1, at.Item2, down.X, down.Z);
        Assert.InRange(forward, 7.56 * 0.95, 7.56 * 1.05);

        // And the centre fielder under the fly, at what the multiplier leaves him: 10.8 ft/s in, the brake still a tenth of the rated
        // 18 ft/s body's — 18 ft/s² — so the reversal stops him in the 0.60 s of air: 3.24 ft (frame sum 6.48 − 18 × (1/60)² × 666 = 3.15).
        (match, hit, preview) = Fixture(Game, "basil");
        live = Begin(match, hit, preview);
        Run(live, readyFrames, _ => LivePadInput.Dead);
        Run(live, 60, _ => new LivePadInput(StickY: -1.0));
        var under = Diamond.Dist(live.GloveX, live.GloveZ, _lastX, _lastZ) / Frame;
        Assert.InRange(under, 10.8 * 0.97, 10.8 * 1.03);
        at = (live.GloveX, live.GloveZ);
        airborneFrames = 0;
        Run(live, 36, i => new LivePadInput(StickY: 1.0, WestDown: i == 0), afterEach: () => { if (live.Airborne) { airborneFrames++; down = (live.GloveX, live.GloveZ); } });
        Assert.Equal(36, airborneFrames);
        Assert.InRange(Diamond.Dist(at.Item1, at.Item2, down.X, down.Z), 3.24 * 0.93, 3.24 * 1.05);
    }

    // ---------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------

    /// <summary>A 245-ft fly at 34° to centre on Harbor; the centre fielder is the body under test, the offence without him.</summary>
    static (Match Match, AtBatResult Hit, FieldingPreview Preview) Fixture(ContentCatalog content, string centre)
    {
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "grit", "marlow", "vine", centre, "hex");
        var away = content.Team("Offense", "rio", "boom", "cinder", "soot", "nugget", "nico", "gull", "konga", "ashlord");
        var match = Match.Exhibition(content, home, away, 3, 1, parkId: "harbor-diamond");
        var hit = FlightFixtures.Landing(match.Park, 245, 34, 0, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("CF", preview.Position);
        return (match, hit, preview);
    }

    /// <summary>A 110-ft pop at 60° behind short on Harbor: grit (Run 5) is the shortstop under it, and the infield's air multiplier is 1.0.</summary>
    static (Match Match, AtBatResult Hit, FieldingPreview Preview) PopFixture(ContentCatalog content)
    {
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "marlow", "grit", "vine", "moss", "hex");
        var away = content.Team("Offense", "rio", "boom", "cinder", "soot", "nugget", "nico", "gull", "konga", "ashlord");
        var match = Match.Exhibition(content, home, away, 3, 1, parkId: "harbor-diamond");
        var hit = FlightFixtures.Landing(match.Park, 110, 60, -20, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        Assert.Equal("grit", preview.Fielder.Id);
        return (match, hit, preview);
    }

    static LivePlaySystem Begin(Match match, AtBatResult hit, FieldingPreview preview)
    {
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, HumanGlove, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        return live;
    }

    static double _lastX, _lastZ;

    /// <summary>Tick <paramref name="frames"/> frames on the human seat; <paramref name="pad"/> gives the frame's defense pad; the last two positions are kept for a speed read.</summary>
    static void Run(LivePlaySystem live, int frames, Func<int, LivePadInput> pad, Action? afterEach = null)
    {
        for (var i = 0; i < frames; i++)
        {
            _lastX = live.GloveX; _lastZ = live.GloveZ;
            var r = live.Apply(LivePlayCommand.Tick(Frame, pad(i), LivePadInput.Dead, false, LivePlayCommandSource.Human));
            Assert.Null(r.CompletedPlay);
            afterEach?.Invoke();
        }
    }

    /// <summary>A stick of <paramref name="mag"/> from the glove toward <paramref name="target"/>.</summary>
    static LivePadInput Hold(LivePlaySystem live, (double X, double Z) target, double mag)
    {
        var dx = target.X - live.GloveX;
        var dz = target.Z - live.GloveZ;
        var len = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
        return new LivePadInput(StickX: dx / len * mag, StickY: dz / len * mag);
    }
}
