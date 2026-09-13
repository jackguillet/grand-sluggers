using GrandSluggers.Sim;
using Xunit;
using Xunit.Abstractions;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Facing (spec §8.2, #611, #576): a body turns and runs; it faces the ball only when planted, the throw target
/// on release, and the ball in the last few feet under a fly coming over its head. The turn is degrees per second
/// from data/feel/table.json, scaled by the frame, so the heading does not depend on the frame rate.
/// </summary>
public sealed class BodyFacingTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    readonly ITestOutputHelper _out;
    const double Frame = 1.0 / 60.0;

    public BodyFacingTests(ITestOutputHelper output) => _out = output;

    BodyFacing.Rates Rates => BodyFacing.Rates.Of(_content.Feel);

    [Fact]
    public void TheTurnIsFeelData()
    {
        var feel = _content.Feel;
        Assert.Equal(720, feel.BodyTurnDegPerSec, 6);
        Assert.Equal(12, feel.BackpedalFt, 6);
        Assert.Equal(3, feel.FaceBallMinFt, 6);
        Assert.Equal(0.08, feel.HeadingSmoothSec, 6);
        Assert.Equal(90, feel.HeadingTeleportFtPerSec, 6);
        // The fastest body on the field (Run 10, dashing) is not a teleport.
        var fastest = (_content.Rules.Fielding.Chase.BaseFtPerSec + 10 * _content.Rules.Fielding.Chase.FtPerSecPerRun)
                      * _content.Rules.Fielding.Dash.ChaseMul;
        Assert.True(fastest < feel.HeadingTeleportFtPerSec, $"fastest glove {fastest:0.0} ft/s");
    }

    [Fact]
    public void AGloveMovingPlusXFacesPlusXWithinTheTurnFrames()
    {
        var r = Rates;
        var heading = new BodyHeading();
        heading.Snap(0, 0, 0);
        // The ball is far behind at −Z: planted, the glove would face it. Running +X, it faces +X.
        var frames = (int)Math.Ceiling(90 / (r.TurnDegPerSec * Frame)) + 1;
        double x = 0;
        for (var i = 0; i < frames; i++)
        {
            x += 21 * Frame;
            heading.Tick(x, 0, Frame, BodyFacing.Fielder(x, 0, 0, -200, false, 0, 0, false, 0, 0, r), r);
        }
        Assert.Equal(BodyFacing.Source.Run, heading.Source);
        Assert.InRange(BodyFacing.Between(heading.YawDeg, 90), 0, 0.5);
        Assert.True(frames <= 9, $"a quarter turn takes {frames} frames at {r.TurnDegPerSec} deg/s");
    }

    [Fact]
    public void APlantedGloveWithTheBallAtMinusZFacesMinusZ()
    {
        var r = Rates;
        var heading = new BodyHeading();
        heading.Snap(0, 250, 90);
        for (var i = 0; i < 30; i++)
            heading.Tick(0, 250, Frame, BodyFacing.Fielder(0, 250, 0, 180, false, 0, 0, true, 0, 250, r), r);
        Assert.Equal(BodyFacing.Source.Look, heading.Source);
        Assert.InRange(BodyFacing.Between(heading.YawDeg, 180), 0, 0.5);
    }

    [Fact]
    public void AGloveThrowingToFirstFacesFirstEvenWhileItsFeetDrift()
    {
        var r = Rates;
        var ss = Diamond.Positions["SS"];
        var first = Diamond.First;
        var heading = new BodyHeading();
        heading.Snap(ss.X, ss.Z, 0);
        double x = ss.X, z = ss.Z;
        for (var i = 0; i < 30; i++)
        {
            z += 10 * Frame; // still carrying toward second base
            heading.Tick(x, z, Frame, BodyFacing.Fielder(x, z, x, z, true, first.X, first.Z, false, 0, 0, r), r);
        }
        Assert.Equal(BodyFacing.Source.Look, heading.Source);
        Assert.InRange(BodyFacing.Between(heading.YawDeg, BodyFacing.YawOf(first.X - x, first.Z - z)), 0, 0.5);
    }

    [Fact]
    public void TheTurnIsScaledByTheFrameNotCountedInFrames()
    {
        var r = Rates;
        // Mid-turn: the same play time is the same angle at 30 and 60 fps.
        var at30 = 0.0;
        var at60 = 0.0;
        for (var i = 0; i < 6; i++) at30 = BodyFacing.Turn(at30, 180, r.TurnDegPerSec, 1 / 30.0);
        for (var i = 0; i < 12; i++) at60 = BodyFacing.Turn(at60, 180, r.TurnDegPerSec, 1 / 60.0);
        Assert.Equal(at30, at60, 6);
        Assert.Equal(r.TurnDegPerSec * 0.2, at60, 6);

        // A body curving on the grass: after 0.5 s both frame rates face the same way.
        Assert.InRange(BodyFacing.Between(CurveHeading(30), CurveHeading(60)), 0, 1.0);
    }

    double CurveHeading(int fps)
    {
        var r = Rates;
        var dt = 1.0 / fps;
        var heading = new BodyHeading();
        heading.Snap(0, 200, -90);
        const double radius = 12, speed = 24;
        var steps = (int)Math.Round(0.5 * fps);
        for (var i = 1; i <= steps; i++)
        {
            var a = speed / radius * i * dt;
            var x = radius * Math.Sin(a);
            var z = 200 + radius * (1 - Math.Cos(a));
            heading.Tick(x, z, dt, BodyFacing.Fielder(x, z, 0, 0, false, 0, 0, false, 0, 0, r), r);
        }
        return heading.YawDeg;
    }

    [Fact]
    public void TheBackpedalIsOnlyTheLastFeetUnderAFlyComingOverTheHead()
    {
        var r = Rates;
        const double plantZ = 330;
        // Running away from home (+Z) with the ball behind, toward the plate.
        BodyHeading Run(double startZ, bool fly)
        {
            var heading = new BodyHeading();
            heading.Snap(0, startZ, 0);
            var z = startZ;
            for (var i = 0; i < 20; i++)
            {
                z += 20 * Frame;
                heading.Tick(0, z, Frame, BodyFacing.Fielder(0, z, 0, 200, false, 0, 0, fly, 0, plantZ, r), r);
            }
            return heading;
        }
        var far = Run(plantZ - 40, fly: true);
        Assert.Equal(BodyFacing.Source.Run, far.Source);
        Assert.InRange(BodyFacing.Between(far.YawDeg, 0), 0, 0.5);

        var close = Run(plantZ - r.BackpedalFt + 0.5, fly: true);
        Assert.Equal(BodyFacing.Source.Backpedal, close.Source);
        Assert.InRange(BodyFacing.Between(close.YawDeg, 180), 0, 0.5);

        var grounder = Run(plantZ - r.BackpedalFt + 0.5, fly: false);
        Assert.Equal(BodyFacing.Source.Run, grounder.Source);
    }

    [Fact]
    public void ABallOverheadOrInTheGloveKeepsTheHeading()
    {
        var r = Rates;
        var heading = new BodyHeading();
        heading.Snap(0, 300, 180);
        // The ball drops through the plant from the plate side and ends on the far side of the body.
        foreach (var (ballX, ballZ) in new[] { (0.0, 290.0), (0.5, 298.0), (-0.5, 300.0), (0.5, 302.0) })
            heading.Tick(0, 300, Frame, BodyFacing.Fielder(0, 300, ballX, ballZ, false, 0, 0, true, 0, 300, r), r);
        Assert.InRange(BodyFacing.Between(heading.YawDeg, 180), 0, 0.5);
    }

    [Fact]
    public void APlacedBodyIsNotARun()
    {
        var r = Rates;
        var heading = new BodyHeading();
        heading.Snap(0, 0, 0);
        heading.Tick(80, 0, Frame, new BodyFacing.Facts(0, 1), r);
        Assert.NotEqual(BodyFacing.Source.Run, heading.Source);
        Assert.Equal(0, heading.SpeedFtPerSec, 6);
    }

    // ---------------------------------------------------------------------------------
    // #576  The outfielder does not spin under a fly
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(250, 34, 0)]   // routine fly 55 ft in front of CF (GameplayTests' fixture)
    [InlineData(335, 36, 4)]   // a fly over CF's head: the run back, then the backpedal
    [InlineData(270, 32, -30)] // a fly into the LF–CF gap
    public void S576_TheGloveUnderAFlyPlantsFacesTheBallAndDoesNotSpin(double carryFt, double launchDeg, double sprayDeg)
    {
        var scenario = new Scenario(_content, seed: 1);
        var match = scenario.Match;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, carryFt, launchDeg, sprayDeg);
        var preview = match.PreviewHit(hit);
        Assert.True(FlyCatch.IsFly(preview), $"{hit.Class}");
        var r = Rates;
        var live = match.LivePlay;
        var field = match.ResolveFielding(hit, preview);
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field, LiveSeats.CpuOnly)).Snapshot.Active);
        var plant = FlyCatch.ChaseTarget(preview, match.Park, match.Rules);

        var headings = new Dictionary<string, BodyHeading>();
        foreach (var pos in Diamond.Order)
        {
            var at = Diamond.Positions[pos];
            var h = new BodyHeading();
            h.Snap(at.X, at.Z, BodyFacing.YawOf(-at.X, -at.Z));
            headings[pos] = h;
        }

        var arrivedPos = "";
        var committed = 0.0;
        var turns = 0;
        var trail = new List<(double T, double X, double Z)>();
        var caught = false;
        PlayEvent? done = null;
        for (var i = 0; i < 60 * 15 && live.Active; i++)
        {
            var result = live.Apply(LivePlayCommand.Tick(Frame));
            done = result.CompletedPlay;
            if (live.Caught || live.Events.Contains(LiveEvent.Glove) || done?.Kind == PlayKind.FlyOut)
            {
                caught = true;
                break;
            }
            if (!live.Active) break;
            foreach (var kv in live.Fielders)
            {
                var onBall = kv.Key == live.GlovePos;
                var (x, z) = onBall ? (live.GloveX, live.GloveZ) : kv.Value;
                var fly = onBall && !live.HoldsBall
                          && FieldingResolver.InAir(preview, live.BallY, live.ElapsedSeconds, preview.HangTimeSec);
                var facts = BodyFacing.Fielder(x, z, live.BallX, live.BallZ,
                    live.Throwing && kv.Key == live.ThrowFromPos, live.ThrowTo.X, live.ThrowTo.Z,
                    fly, plant.X, plant.Z, r);
                headings[kv.Key].Tick(x, z, Frame, facts, r);
            }
            var glove = headings[live.GlovePos];
            if (arrivedPos != live.GlovePos && Diamond.Dist(live.GloveX, live.GloveZ, plant.X, plant.Z) <= preview.CatchRadius)
            {
                arrivedPos = live.GlovePos;
                committed = glove.WantDeg;
                turns = 0;
                trail.Clear();
            }
            if (arrivedPos == live.GlovePos)
            {
                if (BodyFacing.Between(glove.WantDeg, committed) > TurnDeg)
                {
                    turns++;
                    committed = glove.WantDeg;
                }
                trail.Add((live.ElapsedSeconds, live.GloveX, live.GloveZ));
            }
        }
        Assert.True(caught, $"the fly at {carryFt} ft / {sprayDeg}° is caught (play {done?.Kind}, glove {live.GlovePos})");
        Assert.NotEqual("", arrivedPos);
        _out.WriteLine($"{arrivedPos}: {turns} heading changes inside the {preview.CatchRadius:0.0} ft radius over {trail.Count} frames");
        Assert.True(turns < 3, $"{arrivedPos} changed heading {turns} times after arriving under the fly");
        // Planted: the last 0.4 s before the catch the body does not re-steer.
        var catchT = trail[^1].T;
        var end = trail[^1];
        var drift = trail.Where(p => p.T >= catchT - 0.4).Max(p => Diamond.Dist(p.X, p.Z, end.X, end.Z));
        Assert.True(drift < 1.0, $"{arrivedPos} moved {drift:0.00} ft in the last 0.4 s before the catch");
    }

    /// <summary>A heading change is a turn of more than this many degrees from the heading last settled on.</summary>
    const double TurnDeg = 20;
}
