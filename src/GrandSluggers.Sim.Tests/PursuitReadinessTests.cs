using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The Unity pass of #718: the couch side of the pursuit stick (<see cref="PursuitReadiness"/>; F693-02-pursuit-calibration-policy,
/// -calibration-samples, -arming). A seated controller starts the match with no profile and adopts a released-stick window outside
/// live baseball; nothing is sampled while the ball is live and no window is stitched across a gap; a
/// different controller is a replacement, the same one back a recovery; Call time recalibrates every seated controller, and backing
/// out or failing keeps the old centre. A stick table with the radial stick switched off reads no calibration, so none of it runs
/// there.
/// </summary>
public sealed class PursuitReadinessTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    static FieldStickRules Radial => Game.Rules.Fielding.Stick;
    const double Frame = 1.0 / 60.0;

    static PursuitReadiness.SeatDevice Pad(int id, double x = 0, double y = 0, bool present = true) => new(true, id, present, x, y);
    static PursuitReadiness.SeatDevice Cpu => PursuitReadiness.SeatDevice.Empty;

    static LivePlaySystem NewLive(ContentCatalog content)
    {
        var home = content.Team("Home", "rio", "boom", "cinder", "soot", "nugget", "nico", "gull", "marlow", "ashlord");
        var away = content.Team("Away", "vale", "pewter", "lace", "frost", "basil", "grit", "vine", "moss", "hex");
        return Match.Exhibition(content, home, away, 3, 1, parkId: ParkIds.Harbor).LivePlay;
    }

    /// <summary>Tick <paramref name="seconds"/> of frames from <paramref name="clock"/>; returns the clock after.</summary>
    static double Run(PursuitReadiness r, LivePlaySystem live, FieldStickRules rules, double clock, double seconds, bool outsidePlay,
        params PursuitReadiness.SeatDevice[] seats)
    {
        var frames = (int)Math.Round(seconds / Frame);
        for (var i = 0; i < frames; i++)
        {
            r.Tick(live, rules, outsidePlay, clock, seats);
            clock += Frame;
        }
        return clock;
    }

    /// <summary>A controller seated for the match has no profile until a resting half second is sampled outside play; the seat is told to let go meanwhile, and the window's progress fills.</summary>
    [Fact]
    public void ASeatedControllerCalibratesOnARestingStickOutsidePlay()
    {
        var live = NewLive(Game);
        var r = new PursuitReadiness();
        r.Tick(live, Radial, true, 0, [Pad(7, 0.04, -0.02)]);
        var stick = live.FieldStick(0);
        Assert.False(stick.Calibration.Valid);
        Assert.Equal(PursuitReadiness.Tell.LetGo, r.TellFor(0, live, Radial));
        var clock = Run(r, live, Radial, Frame, 0.25, true, Pad(7, 0.04, -0.02));
        Assert.InRange(r.Progress(0, Radial, clock), 0.45, 0.55);
        Assert.False(stick.Calibration.Valid);
        Run(r, live, Radial, clock, 0.30, true, Pad(7, 0.04, -0.02));
        Assert.True(stick.Calibration.Valid);
        Assert.Equal(1, stick.Calibration.Adopted);
        Assert.Equal(0.04, stick.Calibration.Center.X, 9);
        Assert.Equal(-0.02, stick.Calibration.Center.Y, 9);
        Assert.False(stick.Armed, "adoption owes the neutral read; the seat's first read at rest arms it");
        Assert.Equal(PursuitReadiness.Tell.None, r.TellFor(0, live, Radial));
    }

    /// <summary>No live learning: two seconds of a resting stick during a live ball adopt nothing, and a window a live ball interrupts starts over.</summary>
    [Fact]
    public void NothingIsSampledWhileTheBallIsLiveAndAnInterruptedWindowStartsOver()
    {
        var live = NewLive(Game);
        var r = new PursuitReadiness();
        var clock = Run(r, live, Radial, 0, 2.0, false, Pad(7));
        var stick = live.FieldStick(0);
        Assert.False(stick.Calibration.Valid);
        Assert.Equal(PursuitReadiness.Tell.LetGo, r.TellFor(0, live, Radial));

        clock = Run(r, live, Radial, clock, 0.30, true, Pad(7));
        clock = Run(r, live, Radial, clock, 0.30, false, Pad(7));
        clock = Run(r, live, Radial, clock, 0.30, true, Pad(7));
        Assert.False(stick.Calibration.Valid, "0.30 + 0.30 s either side of a live ball is not a 0.50 s window");
        Run(r, live, Radial, clock, 0.25, true, Pad(7));
        Assert.True(stick.Calibration.Valid);
    }

    [Fact]
    public void AStallStartsTheWindowOverRatherThanSpanTheGap()
    {
        var live = NewLive(Game);
        var r = new PursuitReadiness();
        var clock = Run(r, live, Radial, 0, 0.30, true, Pad(7));
        clock += 0.40;   // the input clock ran on and nothing was sampled: a hitch
        clock = Run(r, live, Radial, clock, 0.30, true, Pad(7));
        Assert.False(live.FieldStick(0).Calibration.Valid);
        Run(r, live, Radial, clock, 0.25, true, Pad(7));
        Assert.True(live.FieldStick(0).Calibration.Valid);
    }

    /// <summary>A thumb still on the stick (0.30 off centre) or a jittering one is refused window after window; the seat keeps being told.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AHeldOrJitteryStickIsRefusedAndTheSeatKeepsBeingTold(bool jitter)
    {
        var live = NewLive(Game);
        var r = new PursuitReadiness();
        var clock = 0.0;
        for (var i = 0; i < 90; i++, clock += Frame)
            r.Tick(live, Radial, true, clock, [jitter ? Pad(7, i % 2 == 0 ? 0 : 0.05, 0) : Pad(7, 0.30, 0)]);
        var cal = live.FieldStick(0).Calibration;
        Assert.False(cal.Valid);
        Assert.True(cal.Refused >= 2, $"refused {cal.Refused}");
        Assert.Equal(PursuitReadiness.Tell.LetGo, r.TellFor(0, live, Radial));
    }

    /// <summary>A different controller taking the seat starts with no profile; the same one reconnecting keeps its centre and owes one neutral read.</summary>
    [Fact]
    public void ADifferentControllerIsAReplacementAndTheSameOneBackIsARecovery()
    {
        var live = NewLive(Game);
        var r = new PursuitReadiness();
        var clock = Run(r, live, Radial, 0, 0.6, true, Pad(7, 0.05, 0));
        var stick = live.FieldStick(0);
        Assert.True(stick.Calibration.Valid);
        Assert.True(stick.Read(0.05, 0, Radial) is { Manual: false });   // a neutral read arms the seat
        Assert.True(stick.Armed);

        // Unplugged for a moment, the same controller back: the centre stands, the neutral read is owed again.
        clock = Run(r, live, Radial, clock, 0.2, false, Pad(7, present: false));
        clock = Run(r, live, Radial, clock, Frame, false, Pad(7, 0.05, 0));
        Assert.True(stick.Calibration.Valid);
        Assert.Equal(0.05, stick.Calibration.Center.X, 9);
        Assert.False(stick.Armed);

        // Another controller takes the seat: nothing carried over.
        Run(r, live, Radial, clock, Frame, false, Pad(9, 0, 0));
        Assert.False(stick.Calibration.Valid);
        Assert.Equal((0.0, 0.0), stick.Calibration.Center);
        Assert.Equal(PursuitReadiness.Tell.LetGo, r.TellFor(0, live, Radial));
    }

    /// <summary>
    /// Call time's Reset stick: both seated controllers sample. Player 1 lets go and is reset to the new centre; player 2 keeps a
    /// thumb on it and is refused, then backs out — the old centre stands. A second try where both let go finishes the card.
    /// </summary>
    [Fact]
    public void CallTimeRecalibratesEverySeatedControllerAndBackingOutKeepsTheOldCentre()
    {
        var live = NewLive(Game);
        var r = new PursuitReadiness();
        var clock = Run(r, live, Radial, 0, 0.6, true, Pad(7, 0.02, 0), Pad(9, -0.03, 0.01));
        Assert.True(live.FieldStick(0).Calibration.Valid && live.FieldStick(1).Calibration.Valid);
        Assert.True(PursuitReadiness.Offered(Radial, [Pad(7), Pad(9)]));

        Assert.True(r.Request(live, Radial, [Pad(7), Pad(9)]));
        Assert.True(r.Recalibrating);
        Assert.Equal(PursuitReadiness.Tell.LetGo, r.TellFor(0, live, Radial));
        Assert.Equal(PursuitReadiness.Tell.LetGo, r.TellFor(1, live, Radial));
        clock = Run(r, live, Radial, clock, 0.6, true, Pad(7, 0.06, 0), Pad(9, 0.40, 0));
        Assert.Equal(PursuitReadiness.Tell.Reset, r.TellFor(0, live, Radial));
        Assert.Equal(PursuitReadiness.Tell.LetGo, r.TellFor(1, live, Radial));
        Assert.False(r.Recalibrated);
        Assert.Equal(0.06, live.FieldStick(0).Calibration.Center.X, 9);
        r.Close();
        Assert.False(r.Recalibrating);
        Assert.Equal(-0.03, live.FieldStick(1).Calibration.Center.X, 9);   // the failed seat keeps its old centre
        Assert.True(live.FieldStick(1).Calibration.Valid);
        Assert.Equal(PursuitReadiness.Tell.None, r.TellFor(0, live, Radial));
        Assert.Equal(PursuitReadiness.Tell.None, r.TellFor(1, live, Radial));

        Assert.True(r.Request(live, Radial, [Pad(7), Pad(9)]));
        Run(r, live, Radial, clock, 0.6, true, Pad(7, 0.01, 0), Pad(9, 0.01, 0.02));
        Assert.True(r.Recalibrated);
        Assert.Equal(0.02, live.FieldStick(1).Calibration.Center.Y, 9);
    }

    [Fact]
    public void ACpuSeatIsNeverTouchedAndANewMatchBindsAfresh()
    {
        var live = NewLive(Game);
        var r = new PursuitReadiness();
        Run(r, live, Radial, 0, 0.6, true, Pad(7), Cpu);
        Assert.True(live.FieldStick(0).Calibration.Valid);
        Assert.Equal(1, live.FieldStick(0).Calibration.Adopted);
        Assert.Equal(0, live.FieldStick(1).Calibration.Adopted);
        Assert.Equal(PursuitReadiness.Tell.None, r.TellFor(1, live, Radial));

        // Restart: a new match is a new set of sticks, and the seated controller owes a window again.
        var next = NewLive(Game);
        r.Tick(next, Radial, true, 1.0, [Pad(7), Cpu]);
        Assert.False(next.FieldStick(0).Calibration.Valid);
        Assert.Equal(PursuitReadiness.Tell.LetGo, r.TellFor(0, next, Radial));
        Assert.Equal(PursuitReadiness.Tell.None, r.TellFor(0, live, Radial));
    }
}
