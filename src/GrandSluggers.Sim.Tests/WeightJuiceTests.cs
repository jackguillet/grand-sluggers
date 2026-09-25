using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Juice by weight (CH-13, CF-6, SC-23): anticipation, hit-stop, squash and settle read off the body class's row in
/// <c>feel.weightJuice</c>; the hit-stop holds the sim for whole frames and never changes its trace or a play.
/// </summary>
public sealed class WeightJuiceTests
{
    readonly ContentCatalog _content = Shipped.Content;
    const double Frame = 1.0 / 60.0;

    FeelTable Feel => _content.Feel;
    WeightJuiceRow RowOf(string id) => WeightJuice.Of(_content.Must(id), Feel);
    double Knockback(string classId) => _content.Rules.BodyClasses.Of(classId).KnockbackMul;

    [Fact]
    public void SC23_AHeavyBodyHoldsAndSettlesLongerThanALightOneOnTheSameContact()
    {
        var heavy = _content.Must("ashlord");
        var light = _content.Must("zig");
        Assert.True(Knockback(heavy.BodyClass) < Knockback(light.BodyClass), "Ashlord is the heavier body class");
        var h = WeightJuice.Of(heavy, Feel);
        var l = WeightJuice.Of(light, Feel);
        // Read from the feel table: the class's own row, nothing in code.
        Assert.Same(Feel.WeightJuice.Of(heavy.BodyClass), h);
        Assert.Same(Feel.WeightJuice.Of(light.BodyClass), l);

        foreach (var freeze in new[] { Feel.SolidFreeze, Feel.SmashFreeze, CartoonJuice.SourFreeze })
        {
            Assert.Equal(freeze * h.HitStopMul, WeightJuice.HitStopSec(freeze, h), 12);
            Assert.True(WeightJuice.HitStopSec(freeze, h) > WeightJuice.HitStopSec(freeze, l),
                $"the heavy hit-stop on a {freeze} s contact must be longer: {WeightJuice.HitStopSec(freeze, h)} vs {WeightJuice.HitStopSec(freeze, l)}");
        }
        Assert.True(WeightJuice.CatchStopSec(h) > WeightJuice.CatchStopSec(l), "the heavy body holds on its own catch");
        Assert.True(h.SettleSec > l.SettleSec, $"settle {h.SettleSec} vs {l.SettleSec}");
        Assert.True(h.AnticipationSec > l.AnticipationSec, $"anticipation {h.AnticipationSec} vs {l.AnticipationSec}");

        // The same contact, frame by frame: the light body is at rest while the heavy one is still settling.
        var lightDone = l.SettleSec + Frame;
        Assert.Equal(new Vec3(1, 1, 1), WeightJuice.Settle(l, lightDone));
        Assert.NotEqual(new Vec3(1, 1, 1), WeightJuice.Settle(h, lightDone));
        // Heavy squashes lower and flatter at the impact; light overshoots tall, and sooner.
        Assert.True(WeightJuice.Settle(h, 0).Y < WeightJuice.Settle(l, 0).Y, "the heavy impact squash is lower");
        Assert.True(WeightJuice.Settle(h, 0).X > WeightJuice.Settle(l, 0).X, "and flatter");
        var lightPeak = PeakStretch(l);
        var heavyPeak = PeakStretch(h);
        Assert.True(lightPeak.Y > heavyPeak.Y, $"the light stretch is taller: {lightPeak.Y:0.000} vs {heavyPeak.Y:0.000}");
        Assert.True(lightPeak.T < heavyPeak.T, $"and sooner: {lightPeak.T:0.000} s vs {heavyPeak.T:0.000} s");
        // The wind-up: at the heavy body's anticipation window the heavy one is already sinking, the light one is not.
        var early = h.AnticipationSec * 0.9;
        Assert.True(WeightJuice.Anticipation(h, early).Y < 1);
        Assert.Equal(new Vec3(1, 1, 1), WeightJuice.Anticipation(l, early));
    }

    [Fact]
    public void HarborKidIsTheJuiceEveryBodyHadBeforeClasses()
    {
        var rio = _content.Must("rio");
        Assert.Equal(WeightJuiceFeel.ReferenceClass, rio.BodyClass);
        var row = WeightJuice.Of(rio, Feel);
        Assert.Equal(Feel.SolidFreeze, WeightJuice.HitStopSec(Feel.SolidFreeze, row));
        Assert.Equal(Feel.SmashFreeze, WeightJuice.HitStopSec(Feel.SmashFreeze, row));
        Assert.Equal(CartoonJuice.SourFreeze, WeightJuice.HitStopSec(CartoonJuice.SourFreeze, row));
        Assert.Equal(0, WeightJuice.CatchStopSec(row));
        for (var t = -1.0; t <= 1.0; t += Frame)
            Assert.Equal(new Vec3(1, 1, 1), WeightJuice.Wrapper(row, t, t));
        // A body built by hand, with no class, wears the same row.
        Assert.Same(row, WeightJuice.Of(_content.Must("brondo") with { BodyClass = "" }, Feel));
        Assert.Same(row, WeightJuice.Of(null, Feel));
    }

    [Fact]
    public void EveryBodyClassHasAJuiceRow()
    {
        Assert.Empty(Feel.WeightJuice.Coverage(_content.Rules.BodyClasses));
        foreach (var c in _content.Rules.BodyClasses.Classes)
            Assert.NotNull(Feel.WeightJuice.Find(c.Id));

        var missing = Feel.WeightJuice with { Classes = Feel.WeightJuice.Classes.Where(r => r.Class != "brick").ToList() };
        Assert.Contains(missing.Coverage(_content.Rules.BodyClasses), e => e.Contains("'brick'") && e.Contains("no row"));
        var stray = Feel.WeightJuice with { Classes = [.. Feel.WeightJuice.Classes, Feel.WeightJuice.Of("brick") with { Class = "blimp" }] };
        Assert.Contains(stray.Coverage(_content.Rules.BodyClasses), e => e.Contains("'blimp'") && e.Contains("not a body class"));
        var noReference = Feel.WeightJuice with { Classes = Feel.WeightJuice.Classes.Where(r => r.Class != WeightJuiceFeel.ReferenceClass).ToList() };
        Assert.Throws<InvalidDataException>(noReference.Validate);
        Assert.Throws<ArgumentException>(() => missing.Of("brick"));
    }

    [Fact]
    public void AHeavierClassNeverWindsUpHoldsOrSettlesShorter()
    {
        var rows = Feel.WeightJuice.Classes;
        foreach (var a in rows)
        foreach (var b in rows)
        {
            if (!(Knockback(a.Class) < Knockback(b.Class))) continue;
            // a is heavier than b.
            Assert.True(a.AnticipationSec >= b.AnticipationSec, $"{a.Class} anticipation {a.AnticipationSec} < {b.Class} {b.AnticipationSec}");
            Assert.True(a.HitStopMul >= b.HitStopMul, $"{a.Class} hitStopMul {a.HitStopMul} < {b.Class} {b.HitStopMul}");
            Assert.True(a.CatchStopSec >= b.CatchStopSec, $"{a.Class} catchStopSec {a.CatchStopSec} < {b.Class} {b.CatchStopSec}");
            Assert.True(a.SettleSec >= b.SettleSec, $"{a.Class} settleSec {a.SettleSec} < {b.Class} {b.SettleSec}");
            Assert.True(a.SettleStretch <= b.SettleStretch, $"{a.Class} settleStretch {a.SettleStretch} > {b.Class} {b.SettleStretch}");
        }
    }

    [Fact]
    public void TheHitStopHoldsWholeFramesAndKeepsTheLongerHold()
    {
        var stop = new HitStop();
        Assert.Equal((Frame, Frame, false), stop.Frame(Frame, 0.12));
        stop.Begin(0.04);
        stop.Begin(0.01); // a shorter hold does not cut a longer one
        var held = 0;
        while (stop.Frame(Frame, 0.12) is { Held: true } f)
        {
            Assert.Equal(0, f.SimSec);
            Assert.Equal(Frame * 0.12, f.DrawSec, 12);
            held++;
        }
        Assert.Equal(3, held);
        Assert.False(stop.Holding);
    }

    [Fact]
    public void APressMadeWhileTheSimIsHeldIsDeliveredWhenItSteps()
    {
        LivePadInput? held = null;
        held = HitStop.Latch(held, new LivePadInput(StickX: 0.2, SouthDown: true, KeysBag: 2));
        held = HitStop.Latch(held, new LivePadInput(StickX: 0.5));
        var step = HitStop.Latch(held, new LivePadInput(StickX: 0.7, EastHeld: true));
        Assert.True(step.SouthDown);
        Assert.Equal(2, step.KeysBag);
        Assert.Equal(0.7, step.StickX);
        Assert.True(step.EastHeld);
        Assert.Same(LivePadInput.Dead, HitStop.Latch(null, LivePadInput.Dead));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public void TheHitStopNeverChangesTheSimTrace(int seed)
    {
        // Shipped juice, three times the hold, and no hold at all: the same play, tick for tick.
        var shipped = Run(seed, Feel);
        var longer = Run(seed, Scaled(Feel, hitStopMul: 3, catchStopSec: 0.25));
        var none = Run(seed, Scaled(Feel, hitStopMul: 1e-9, catchStopSec: 0));

        Assert.True(shipped.Caught, "the fixture play has a catch, so the catch hold is exercised");
        Assert.Equal(none.Trace, shipped.Trace);
        Assert.Equal(none.Trace, longer.Trace);
        Assert.Equal(none.Kind, longer.Kind);
        // The juice did hold: only the wall clock moved.
        Assert.True(longer.HeldFrames > shipped.HeldFrames, $"held {longer.HeldFrames} vs {shipped.HeldFrames}");
        Assert.True(shipped.HeldFrames > none.HeldFrames);
    }

    (string Trace, PlayKind Kind, int HeldFrames, bool Caught) Run(int seed, FeelTable feel)
    {
        var scenario = new Scenario(_content, seed);
        var match = scenario.Match;
        var hit = scenario.Contact() with
        {
            ExitVeloMph = 92, LaunchDeg = 34, SprayDeg = 4, HomeRun = false, Foul = false, InPlay = true,
            CarryFt = BallFlight.CarryFeet(92, 34, match.Park.WindMph, match.Rules)
        };
        var preview = match.PreviewHit(hit);
        var field = new FieldingResult(PlayKind.FlyOut, preview.Fielder, null, preview.HangTimeSec, preview.LandingX,
            preview.LandingZ, false, false);
        var live = match.LivePlay;
        live.Recording = true;
        var stop = new HitStop();
        live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field, LiveSeats.CpuOnly));
        stop.Begin(WeightJuice.HitStopSec(feel.SolidFreeze, WeightJuice.Of(match.Batter, feel)));
        PlayEvent? play = null;
        var held = 0;
        var caught = false;
        for (var i = 0; i < 60 * 30 && play is null; i++)
        {
            var f = stop.Frame(Frame, feel.WeightJuice.HitStopCreepMul);
            if (f.Held) { held++; continue; }
            play = live.Apply(LivePlayCommand.Tick(f.SimSec)).CompletedPlay;
            if (live.Events.Contains(LiveEvent.Glove) && match.DefenseMap.TryGetValue(live.GlovePos, out var who))
            {
                caught = true;
                stop.Begin(WeightJuice.CatchStopSec(WeightJuice.Of(who, feel)));
            }
        }
        Assert.NotNull(play);
        return (live.TakeTrace(play).ToJson(), play!.Kind, held, caught);
    }

    static FeelTable Scaled(FeelTable feel, double hitStopMul, double catchStopSec) => feel with
    {
        WeightJuice = feel.WeightJuice with
        {
            Classes = feel.WeightJuice.Classes.Select(r => r with { HitStopMul = r.HitStopMul * hitStopMul, CatchStopSec = catchStopSec }).ToList()
        }
    };

    static (double T, double Y) PeakStretch(WeightJuiceRow row)
    {
        var best = (T: double.NaN, Y: 1.0);
        for (var t = 0.0; t < row.SettleSec; t += 0.001)
        {
            var y = WeightJuice.Settle(row, t).Y;
            if (y > best.Y + 1e-12) best = (t, y);
        }
        return best;
    }
}
