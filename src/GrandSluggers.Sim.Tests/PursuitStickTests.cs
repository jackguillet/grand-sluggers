using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-2 slice 4 (#718, the human seat): the calibrated radial pursuit stick. Manual pursuit from 0.20, back to
/// assistance at 0.15 with the owner kept between; the asked speed the linear remap of the magnitude from 0.15 to 1;
/// a seat arms with a valid profile and one neutral observation; a calibration is a 0.50-s released-stick window with
/// a centre within 0.10 and every sample within 0.02 of the mean. The shipped table carries <c>enterMag</c> 0 and is the
/// Manhattan gate at <c>feel.fieldAssistStick</c> the game always had — the same doubles, not a product by one.
/// </summary>
public sealed class PursuitStickTests
{
    static readonly ContentCatalog Control = ContentCatalog.Load();
    static readonly DataRoot Root = new(Control.Root.Shipped, Path.GetFullPath(Path.Combine(Control.Root.Shipped, "..", "trials", "c80")));
    static readonly ContentCatalog Trial = ContentCatalog.Load(Root);
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);
    static FieldStickRules Radial => Trial.Rules.Fielding.Stick;

    [Fact]
    public void TheShippedStickIsTheManhattanGateAndTheTrialIsTheCalibratedRadialOne()
    {
        var s = Control.Rules.Fielding.Stick;
        Assert.Equal((0.0, 0.0, false), (s.EnterMag, s.LeaveMag, s.Radial));
        Assert.Equal(0.35, Control.Feel.FieldAssistStick);
        var t = Radial;
        Assert.Equal((0.20, 0.15, true), (t.EnterMag, t.LeaveMag, t.Radial));
        Assert.Equal((0.50, 0.10, 0.02), (t.CalibrationSec, t.CenterOffsetMax, t.SampleSpreadMax));
        Assert.Equal((s.CalibrationSec, s.CenterOffsetMax, s.SampleSpreadMax), (t.CalibrationSec, t.CenterOffsetMax, t.SampleSpreadMax));
    }

    // ---------------------------------------------------------------------------------
    // Calibration
    // ---------------------------------------------------------------------------------

    [Fact]
    public void AFreshDeviceIsOnTheIdentityProfileAndAReplacementHasNone()
    {
        var c = new StickCalibration();
        Assert.True(c.Valid);
        Assert.Equal((0.0, 0.0), c.Center);
        Assert.Equal((0.3, -0.2), c.Apply(0.3, -0.2));
        c.Invalidate();
        Assert.False(c.Valid);
    }

    [Fact]
    public void ACompleteQuietWindowIsAdoptedAsTheCentre()
    {
        var c = new StickCalibration();
        c.Invalidate();
        for (var i = 0; i < 32; i++) c.Sample(0.03 + (i % 2 == 0 ? 0.01 : -0.01), -0.02, i / 60.0);   // 0.52 s of a stick resting a little right, jittering ±0.01 about 0.03
        Assert.True(c.Complete(Radial));
        Assert.True(c.TryAdopt(Radial));
        Assert.True(c.Valid);
        Assert.Equal(0.03, c.Center.X, 6);
        Assert.Equal(-0.02, c.Center.Y, 6);
        Assert.Equal((1, 0), (c.Adopted, c.Refused));
        Assert.False(c.Sampling);
        Assert.Equal(0.0, c.Apply(0.03, -0.02).X, 9);   // the resting stick reads as neutral from here on
    }

    [Fact]
    public void AnIncompleteWindowWaitsAndAWindowOffCentreOrNoisyKeepsThePriorProfile()
    {
        var c = new StickCalibration();
        for (var i = 0; i < 24; i++) c.Sample(0.0, 0.0, i / 60.0);           // 0.383 s: not yet
        Assert.False(c.Complete(Radial));
        Assert.False(c.TryAdopt(Radial));
        Assert.True(c.Sampling);

        c.Begin();
        for (var i = 0; i <= 30; i++) c.Sample(0.11, 0.0, i / 60.0);          // centre offset 0.11 > 0.10: refused, identity kept
        Assert.False(c.TryAdopt(Radial));
        Assert.True(c.Valid);
        Assert.Equal((0.0, 0.0), c.Center);

        for (var i = 0; i <= 30; i++) c.Sample(i == 15 ? 0.03 : 0.0, 0.0, i / 60.0);   // one sample 0.029 from the mean > 0.02: refused
        Assert.False(c.TryAdopt(Radial));
        Assert.Equal((0, 2), (c.Adopted, c.Refused));

        for (var i = 0; i <= 30; i++) c.Sample(0.10, 0.0, i / 60.0);          // exactly 0.10: inclusive, adopted
        Assert.True(c.TryAdopt(Radial));
        Assert.Equal(0.10, c.Center.X, 9);
    }

    // ---------------------------------------------------------------------------------
    // Arming, the gates, the remap
    // ---------------------------------------------------------------------------------

    [Fact]
    public void ASeatArmsOnOneNeutralObservationAndOwesItAgainOnEntryRecoveryOrRecalibration()
    {
        var p = new PursuitStick();
        Assert.False(p.Armed);
        Assert.False(p.Read(0.0, 0.6, Radial).Manual);   // held from the start: assistance, still unarmed
        Assert.False(p.Armed);
        Assert.False(p.Read(0.0, 0.10, Radial).Manual);  // the neutral observation arms; that frame is assistance
        Assert.True(p.Armed);
        Assert.True(p.Read(0.0, 0.6, Radial).Manual);

        p.EnterDefense();
        Assert.False(p.Armed);
        Assert.False(p.Read(0.0, 0.6, Radial).Manual);
        p.Read(0.0, 0.0, Radial);
        Assert.True(p.Armed);

        p.DeviceRecovered();
        Assert.False(p.Armed);
        Assert.True(p.Calibration.Valid);              // the same device: the profile stands

        p.DeviceReplaced();
        Assert.False(p.Calibration.Valid);             // a different device: nothing copied
        p.Read(0.0, 0.0, Radial);
        Assert.False(p.Armed, "no valid profile, no arming");
        for (var i = 0; i <= 30; i++) p.Calibration.Sample(0.0, 0.0, i / 60.0);
        Assert.True(p.Recalibrate(Radial));
        Assert.False(p.Armed, "a recalibration owes the neutral observation");
        p.Read(0.0, 0.0, Radial);
        Assert.True(p.Armed);
    }

    [Fact]
    public void ManualPursuitEntersAtTwentyLeavesAtFifteenAndKeepsTheOwnerBetween()
    {
        var p = new PursuitStick();
        p.Read(0, 0, Radial);
        Assert.False(p.Read(0.18, 0, Radial).Manual);   // between the gates from assistance: still assistance
        Assert.False(p.Read(0.199, 0, Radial).Manual);
        var r = p.Read(0.20, 0, Radial);
        Assert.True(r.Manual);
        Assert.Equal((0.20 - 0.15) / 0.85, r.WantX, 9);
        Assert.Equal(0, r.WantY, 9);
        r = p.Read(0.17, 0, Radial);                    // between the gates from manual: still manual, a sliver of speed
        Assert.True(r.Manual);
        Assert.Equal((0.17 - 0.15) / 0.85, r.WantX, 9);
        Assert.False(p.Read(0.15, 0, Radial).Manual);   // at the leave gate: assistance
        Assert.False(p.Read(0.19, 0, Radial).Manual);
    }

    [Fact]
    public void TheAskedSpeedIsLinearFromTheLeaveGateToFullAndCappedOnTheDiagonal()
    {
        var p = new PursuitStick();
        p.Read(0, 0, Radial);
        var half = p.Read(0, 0.575, Radial);            // (0.575 − 0.15) / 0.85 = 0.5: half the usable range asks half the speed
        Assert.Equal(0.5, half.WantY, 9);
        Assert.Equal(0, half.WantX, 9);
        var full = p.Read(0, 1.0, Radial);
        Assert.Equal(1.0, full.WantY, 9);
        var diag = p.Read(1.0, 1.0, Radial);             // magnitude 1.41: the cap holds, the direction is the diagonal
        Assert.Equal(1.0, Math.Sqrt(diag.WantX * diag.WantX + diag.WantY * diag.WantY), 9);
        Assert.Equal(diag.WantX, diag.WantY, 12);
        p.Read(0.6 * 0.6, 0.6 * 0.8, Radial);
        Assert.Equal(0.6, p.Magnitude, 9);   // the magnitude is radial, not Manhattan (|x| + |y| would be 0.84)
    }

    [Fact]
    public void TheCentreIsSubtractedBeforeTheGates()
    {
        var p = new PursuitStick();
        p.Calibration.Invalidate();
        for (var i = 0; i <= 30; i++) p.Calibration.Sample(0.08, 0.0, i / 60.0);
        Assert.True(p.Recalibrate(Radial));
        Assert.False(p.Read(0.08, 0.0, Radial).Manual);   // the resting stick is neutral, and arms the seat
        Assert.True(p.Armed);
        Assert.False(p.Read(0.27, 0.0, Radial).Manual);   // 0.19 from centre: not yet
        Assert.True(p.Read(0.28, 0.0, Radial).Manual);    // 0.20 from centre: manual
    }

    // ---------------------------------------------------------------------------------
    // In play
    // ---------------------------------------------------------------------------------

    /// <summary>The human shortstop (zig, 22.48 ft/s on the trial) holds the ball and runs with the stick: half the usable range is half the speed, full is full, the diagonal is still capped.</summary>
    [Theory]
    [InlineData(0.0, 0.575, 0.5)]
    [InlineData(0.0, 1.0, 1.0)]
    [InlineData(1.0, 1.0, 1.0)]
    public void UnderTheTrialTheStickAsksALinearFractionOfTheGloveSpeed(double x, double y, double fraction)
    {
        var (live, rated) = HumanShortstopHoldsTheBall(Trial, "zig");
        var track = Push(live, new LivePadInput(StickX: x, StickY: y), frames: 24);
        Assert.Equal(22.48, rated, 9);
        Assert.InRange(Speed(track, 20), rated * fraction * 0.97, rated * fraction * 1.03);
        Assert.InRange(Speed(track, 23), rated * fraction * 0.97, rated * fraction * 1.03);
        Assert.True(live.PursuitManual);
        Assert.False(live.PursuitUnready);
    }

    /// <summary>With the ball in hand: 0.18 from rest is assistance and the body stands; 0.30 is manual; 0.17 after that is still manual; 0.10 is assistance and the body brakes to rest.</summary>
    [Fact]
    public void UnderTheTrialTheOwnerIsKeptBetweenTheGatesInPlay()
    {
        var (live, rated) = HumanShortstopHoldsTheBall(Trial, "zig");
        var still = Push(live, new LivePadInput(StickY: 0.18), frames: 10);
        Assert.False(live.PursuitManual);
        Assert.Equal(0, Diamond.Dist(still[0].X, still[0].Z, still[^1].X, still[^1].Z), 6);

        var going = Push(live, new LivePadInput(StickY: 0.30), frames: 12);
        Assert.True(live.PursuitManual);
        Assert.InRange(Speed(going, 11), rated * (0.15 / 0.85) * 0.9, rated * (0.15 / 0.85) * 1.1);

        var kept = Push(live, new LivePadInput(StickY: 0.17), frames: 12);
        Assert.True(live.PursuitManual, "between the gates the seat keeps the body");
        Assert.InRange(Speed(kept, 11), rated * (0.02 / 0.85) * 0.8, rated * (0.02 / 0.85) * 1.2);

        var let = Push(live, new LivePadInput(StickY: 0.10), frames: 12);
        Assert.False(live.PursuitManual);
        Assert.InRange(Speed(let, 11), 0, 0.01);
    }

    /// <summary>A stick held from the first frame never arms: the assistance takes the grounder for the seat and the body stands with it; one neutral frame arms the seat and the next push steers.</summary>
    [Fact]
    public void UnderTheTrialAHeldStickDoesNotSteerUntilTheSeatHasBeenSeenNeutralOnce()
    {
        var (match, hit, preview) = Fixture(Trial, "zig");
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, HumanGlove, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        var held = new LivePadInput(StickY: 0.6);
        var took = false;
        for (var i = 0; i < 60 * 6 && !took; i++)
        {
            live.Apply(LivePlayCommand.Tick(Frame, held, LivePadInput.Dead, false, LivePlayCommandSource.Human));
            Assert.False(live.PursuitManual, "an unarmed seat never steers");
            Assert.True(live.PursuitUnready);
            took = live.HoldsBall && live.GlovePos == "SS" && !live.Throwing;
        }
        Assert.True(took, "the assistance took the grounder for the unready seat");
        var standing = Push(live, held, frames: 12);
        Assert.False(live.PursuitManual);
        Assert.InRange(Speed(standing, 11), 0, 0.01);

        live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Human));   // seen neutral once
        Assert.False(live.PursuitUnready);
        var going = Push(live, held, frames: 20);
        Assert.True(live.PursuitManual);
        Assert.InRange(Speed(going, 19), 22.48 * (0.45 / 0.85) * 0.97, 22.48 * (0.45 / 0.85) * 1.03);
    }

    /// <summary>The shipped stick is the one the game always had: |x| + |y| against 0.35, the raw stick vector as the asked velocity, no arming and nothing unready.</summary>
    [Fact]
    public void TheControlStickIsTheManhattanGateItAlwaysWas()
    {
        var (live, rated) = HumanShortstopHoldsTheBall(Control, "zig");
        Assert.Equal(38.1, rated, 9);
        Assert.False(live.PursuitUnready);
        var still = Push(live, new LivePadInput(StickX: 0.30), frames: 4);            // 0.30 < 0.35: dead
        Assert.False(live.PursuitManual);
        Assert.Equal(0, Diamond.Dist(still[0].X, still[0].Z, still[^1].X, still[^1].Z), 9);
        var going = Push(live, new LivePadInput(StickX: 0.2, StickY: 0.2), frames: 3);   // 0.4 ≥ 0.35: the raw (0.2, 0.2) × 38.1 on the first step
        Assert.True(live.PursuitManual);
        Assert.Equal(Math.Sqrt(0.08) * rated, Speed(going, 0), 6);
    }

    // ---------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------

    static double Speed(List<(double X, double Z)> track, int k) => Diamond.Dist(track[k].X, track[k].Z, track[k + 1].X, track[k + 1].Z) / Frame;

    static (Match Match, AtBatResult Hit, FieldingPreview Preview) Fixture(ContentCatalog content, string shortstop)
    {
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", shortstop, "vine", "moss", "hex");
        var away = content.Team("Offense", "rio", "boom", "cinder", "grit", "soot", "nugget", "nico", "gull", "marlow");
        var match = Match.Exhibition(content, home, away, 3, 1, parkId: "harbor-diamond");
        var hit = FlightFixtures.Landing(match.Park, 118, 4, -18, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        return (match, hit, preview);
    }

    /// <summary>A grounder to short on the human seat with a dead stick: the assistance takes it, the seat is seen neutral, the body comes to rest with the ball.</summary>
    static (LivePlaySystem Live, double Rated) HumanShortstopHoldsTheBall(ContentCatalog content, string shortstop)
    {
        var (match, hit, preview) = Fixture(content, shortstop);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, HumanGlove, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        var held = false;
        for (var i = 0; i < 60 * 6 && !held; i++)
        {
            live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Human));
            held = live.HoldsBall && live.GlovePos == "SS" && !live.Throwing;
        }
        Assert.True(held, $"{shortstop} never took the grounder");
        for (var i = 0; i < 12; i++)
            live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Human));
        Assert.True(live.HoldsBall && live.Active);
        return (live, FieldingResolver.ChaseSpeedFt(content.Must(shortstop), false, match.Rules));
    }

    static List<(double X, double Z)> Push(LivePlaySystem live, LivePadInput pad, int frames)
    {
        var track = new List<(double X, double Z)> { (live.GloveX, live.GloveZ) };
        for (var i = 0; i < frames; i++)
        {
            live.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false, LivePlayCommandSource.Human));
            Assert.True(live.HoldsBall && live.Active, $"the play moved on {i} frames into the push");
            track.Add((live.GloveX, live.GloveZ));
        }
        return track;
    }
}
