using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The held directional bunt (P4-b; PH-13-R1, PH-14-R1 … R6; spec §5.1, §5.8), Appendix B.1 rows S-153 … S-169
/// (S-19 lives beside S-18 in <see cref="AtBatScenarioTests"/>).
///
/// Two triggers hold a side (third, first); the latest press wins; the side changes until contact; releasing every
/// trigger withdraws the bat; the held bat meets the ball with no timed press; the side is a typed fact on the swing
/// and leans the ball. A bunt press replaces an uncommitted swing load; a trigger held at contact is spent until it
/// comes up. The input rows drive <see cref="PlateButtons.Advance"/> frame by frame, the way a client does; the ball
/// rows go through <see cref="AtBatResolver"/> and <see cref="Match.Play"/>.
///
/// The ball off the held bat is the bunt's own response by contact quality (PH-14-R1, S-167 … S-169), with no switch
/// and no swing term. The shipped root is loaded in process, so nothing here depends on <c>GRAND_SLUGGERS_TRIAL</c>.
/// No row stores a double.
/// </summary>
public sealed class BuntHoldScenarioTests
{
    static readonly ContentCatalog Shipped = ContentCatalog.Load(new DataRoot(global::GrandSluggers.Sim.Tests.Shipped.Content.Root.Shipped));

    const double Dt = 1.0 / 60;
    static double ToFull => Shipped.Feel.SwingChargeSeconds;
    static double CenterY => StrikeZoneGeometry.CenterY;

    /// <summary>The two bunt triggers and the swing button on one frame.</summary>
    readonly record struct Pad(
        bool South = false, bool SouthDown = false, bool SouthUp = false,
        bool Third = false, bool ThirdDown = false,
        bool First = false, bool FirstDown = false,
        bool Cancel = false)
    {
        public PlateInput Input => new(SouthDown, South, SouthUp, ThirdDown, Third, FirstDown, First, Cancel);
    }

    /// <summary>One frame of the plate, the client's order: edges first, then the step.</summary>
    static PlateButtonsStep Frame(ref PlateButtonsState state, Pad pad, bool accepting = true, bool commits = true)
    {
        var step = PlateButtons.Advance(state, pad.Input, Dt, ToFull, accepting, commits);
        state = step.Next;
        return step;
    }

    static BuntHoldStep Hold(ref BuntHoldState state, bool thirdDown = false, bool third = false, bool firstDown = false,
        bool first = false, bool accepting = true)
    {
        var step = BuntHold.Advance(state, thirdDown, third, firstDown, first, accepting);
        state = step.Next;
        return step;
    }

    // ---------------------------------------------------------------------------------
    // S-153  Latest press wins
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S153_TheLatestPressAmongTheHeldTriggersIsTheSide()
    {
        var s = default(BuntHoldState);
        Assert.Equal(BuntSide.Third, Hold(ref s, thirdDown: true, third: true).Showing);
        Assert.Equal(BuntSide.Third, Hold(ref s, third: true).Showing);
        // First pressed while third is held: first wins, and stays while both are held.
        var change = Hold(ref s, third: true, firstDown: true, first: true);
        Assert.Equal(BuntSide.First, change.Showing);
        Assert.True(change.Pressed);
        for (var f = 0; f < 10; f++) Assert.Equal(BuntSide.First, Hold(ref s, third: true, first: true).Showing);
        // Third comes up and goes down again: now it is the latest press.
        Assert.Equal(BuntSide.First, Hold(ref s, first: true).Showing);
        Assert.Equal(BuntSide.Third, Hold(ref s, thirdDown: true, third: true, first: true).Showing);

        // Both pressed on one tick from nothing: the third-base side, the one rule for a tie.
        var tie = default(BuntHoldState);
        Assert.Equal(BuntSide.Third, Hold(ref tie, thirdDown: true, third: true, firstDown: true, first: true).Showing);
        // Both held and neither pressed keeps the side already held.
        var kept = new BuntHoldState(BuntSide.First);
        Assert.Equal(BuntSide.First, Hold(ref kept, third: true, first: true).Showing);
    }

    // ---------------------------------------------------------------------------------
    // S-154  The side changes until contact
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S154_TheSideChangesWhileSquaredAndMovesToTheOtherHeldTriggerWhenTheActiveOneComesUp()
    {
        var s = default(BuntHoldState);
        Hold(ref s, firstDown: true, first: true);
        Hold(ref s, thirdDown: true, third: true, first: true);
        Assert.Equal(BuntSide.Third, s.Side);
        // The active trigger comes up while the other is held: the bat moves to the other side, still squared.
        var moved = Hold(ref s, first: true);
        Assert.True(moved.Squared);
        Assert.False(moved.Withdrew);
        Assert.Equal(BuntSide.First, moved.Showing);

        // At the plate the ball takes the side as it stands on that tick.
        var match = new Scenario(Shipped).Match;
        var plate = new PlateButtonsState(default, s);
        var tick = Frame(ref plate, new Pad(First: true));
        var bunt = PlateButtons.HeldBuntAtPlate(tick, match.BatterOffsetX);
        Assert.NotNull(bunt);
        Assert.Equal(BuntSide.First, bunt!.BuntSide);
        var ev = match.Play(Scenario.PitchAt(0, CenterY), bunt);
        Assert.Equal(BuntSide.First, ev.Swing.BuntSide);
    }

    // ---------------------------------------------------------------------------------
    // S-155  Releasing every trigger withdraws the bat
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S155_ReleasingEveryTriggerWithdrawsTheBatAndLatchesNothing()
    {
        var match = new Scenario(Shipped).Match;
        var plate = default(PlateButtonsState);
        Frame(ref plate, new Pad(Third: true, ThirdDown: true));
        for (var f = 0; f < 30; f++) Frame(ref plate, new Pad(Third: true));
        var withdraw = Frame(ref plate, new Pad());
        Assert.True(withdraw.Bunt.Withdrew);
        Assert.False(withdraw.Bunt.Squared);
        Assert.False(withdraw.Swing.Committed, "a withdrawn bunt is not a swing");
        // The ball reaches an empty plate: no bunt, no swing, the pitch is called.
        Assert.Null(PlateButtons.HeldBuntAtPlate(Frame(ref plate, new Pad()), 0));
        Assert.Equal(PlayKind.TakeStrike, match.Play(Scenario.PitchAt(0, CenterY), Scenario.Take).Kind);

        // A tap (down and up inside one tick) holds nothing.
        var tap = default(PlateButtonsState);
        var t = Frame(ref tap, new Pad(ThirdDown: true));
        Assert.False(t.Bunt.Squared);
        Assert.Null(PlateButtons.HeldBuntAtPlate(t, 0));
    }

    // ---------------------------------------------------------------------------------
    // S-156  Contact is geometric: there is no timed press
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S156_TheHeldBatMeetsTheBallWithNoTimedPress()
    {
        var bunt = SwingCommand.HeldBunt(BuntSide.Third, 0);
        Assert.Equal((true, true, 0.0, 0.0), (bunt.Swing, bunt.Bunt, bunt.TimingErrorFrames, bunt.Charge01));
        var resolver = Resolver(Shipped);
        var park = Harbor(Shipped);
        foreach (var bats in new[] { Hand.R, Hand.L })
        foreach (var seed in new[] { 1, 7, 42 })
        {
            var square = resolver.Resolve(Input(Shipped, Rio(bats), BuntSide.Third, 0, 0, CenterY), park, new Random(seed));
            Assert.NotEqual(ContactQuality.Miss, square.Quality);
            // Whatever a caller writes as the error, the held bat is on the plane: the same ball.
            foreach (var err in new[] { -40.0, -4, 4, 40 })
                Assert.Equal(square, resolver.Resolve(Input(Shipped, Rio(bats), BuntSide.Third, err, 0, CenterY), park, new Random(seed)));
        }
    }

    // ---------------------------------------------------------------------------------
    // S-157  Holding is not contact: the bat must be on the ball
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S157_AHeldBatOffTheBallMissesAndIsAStrike()
    {
        var match = new Scenario(Shipped).Match;
        var ev = match.Play(Scenario.PitchAt(0, CenterY), SwingCommand.HeldBunt(BuntSide.First, boxOffsetX: 1));
        Assert.Equal(ContactQuality.Miss, ev.AtBat.Quality);
        Assert.Equal(PlayKind.SwingMiss, ev.Kind);
        Assert.Equal(1, match.Strikes);
    }

    // ---------------------------------------------------------------------------------
    // S-158  The side leans the ball; it is a bias, not a landing point
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S158_TheSideLeansTheBuntTowardItsBaseAndIsNotALandingPoint()
    {
        var resolver = Resolver(Shipped);
        var park = Harbor(Shipped);
        var lean = Shipped.Rules.Batting.Bunt.SideDeg;
        Assert.Equal(-lean, BuntHold.LeanDeg(BuntSide.Third, Shipped.Rules));
        Assert.Equal(lean, BuntHold.LeanDeg(BuntSide.First, Shipped.Rules));
        Assert.Equal(0, BuntHold.LeanDeg(BuntSide.None, Shipped.Rules));
        Assert.True(Diamond.First.X > 0 && Diamond.Third.X < 0, "positive spray is the first-base side");

        foreach (var bats in new[] { Hand.R, Hand.L })
        {
            var third = new List<double>();
            var first = new List<double>();
            var sourThird = new List<double>();
            var sourFirst = new List<double>();
            var sour = CrossingFor(Shipped, Rio(bats), ContactQuality.Sour, -0.3);
            for (var seed = 1; seed <= 200; seed++)
            {
                third.Add(resolver.Resolve(Input(Shipped, Rio(bats), BuntSide.Third, 0, 0, CenterY), park, new Random(seed)).SprayDeg);
                first.Add(resolver.Resolve(Input(Shipped, Rio(bats), BuntSide.First, 0, 0, CenterY), park, new Random(seed)).SprayDeg);
                sourThird.Add(resolver.Resolve(Input(Shipped, Rio(bats), BuntSide.Third, 0, sour, CenterY - 0.3), park, new Random(seed)).SprayDeg);
                sourFirst.Add(resolver.Resolve(Input(Shipped, Rio(bats), BuntSide.First, 0, sour, CenterY - 0.3), park, new Random(seed)).SprayDeg);
            }
            Assert.True(third.Average() < 0 && first.Average() > 0, $"{bats}: {third.Average()} / {first.Average()}");
            // A bias, not a landing point: the ball spreads around the lean (by quality, §5.8), and a poorly met bunt
            // can still roll across straight — some third-side sour bunts to the right, some first-side ones to the left.
            Assert.True(third.Distinct().Count() > 1 && first.Distinct().Count() > 1, $"{bats}: the side is not a point");
            Assert.Contains(sourThird, x => x > 0);
            Assert.Contains(sourFirst, x => x < 0);
        }
    }

    // ---------------------------------------------------------------------------------
    // S-159  Contact fixes the side
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S159_ContactFixesTheSideAndNoLaterTriggerRedirectsIt()
    {
        var s = default(BuntHoldState);
        Hold(ref s, thirdDown: true, third: true);
        s = BuntHold.Contact(s, thirdHeld: true, firstHeld: false);
        Assert.True(s.Fixed);
        // The other trigger pressed after contact moves nothing and squares nothing.
        var after = Hold(ref s, third: true, firstDown: true, first: true);
        Assert.False(after.Squared);
        Assert.Equal(BuntSide.Third, s.Side);
        // Next pitch: the side is no longer fixed; the free first trigger, still down, squares.
        s = s.NextPitch();
        Assert.False(s.Fixed);
        Assert.Equal(BuntSide.First, Hold(ref s, third: true, first: true).Showing);
    }

    // ---------------------------------------------------------------------------------
    // S-160  A bunt press replaces an uncommitted load (PH-13-R1)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S160_ABuntPressDuringALoadDiscardsItAndSquaresToThatSide()
    {
        foreach (var third in new[] { true, false })
        {
            var plate = default(PlateButtonsState);
            Frame(ref plate, new Pad(South: true, SouthDown: true));
            for (var f = 0; f < 15; f++) Frame(ref plate, new Pad(South: true));
            Assert.True(plate.Swing.Armed);
            Assert.True(plate.Swing.Fill01 >= 0.5);

            var press = third ? new Pad(South: true, Third: true, ThirdDown: true) : new Pad(South: true, First: true, FirstDown: true);
            var convert = Frame(ref plate, press);
            Assert.True(convert.Converted);
            Assert.True(convert.Swing.Cancelled);
            Assert.Equal(third ? BuntSide.Third : BuntSide.First, convert.Bunt.Showing);
            Assert.Equal(new ChargeButtonState(false, 0, 0, MustRelease: true), plate.Swing);

            // The old hold's release is not a swing, squared or withdrawn.
            var hold = third ? new Pad(Third: true) : new Pad(First: true);
            var up = Frame(ref plate, hold with { SouthUp = true });
            Assert.False(up.Swing.Committed);
            Assert.True(up.Bunt.Squared);
            Frame(ref plate, new Pad());
            // Withdrawn, a fresh press loads a new swing from zero and a release commits a one-frame slap.
            var fresh = Frame(ref plate, new Pad(South: true, SouthDown: true));
            Assert.True(plate.Swing.Armed);
            Assert.True(plate.Swing.Fill01 <= Dt / ToFull + 1e-9);
            var slap = Frame(ref plate, new Pad(SouthUp: true));
            Assert.True(slap.Swing.Committed);
            Assert.False(fresh.Converted);
        }
    }

    // ---------------------------------------------------------------------------------
    // S-161  Same tick: the square beats the release
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S161_ABuntPressOnTheReleasesTickIsTheBuntNotTheSwing()
    {
        var plate = default(PlateButtonsState);
        Frame(ref plate, new Pad(South: true, SouthDown: true));
        for (var f = 0; f < 10; f++) Frame(ref plate, new Pad(South: true));
        var tick = Frame(ref plate, new Pad(SouthUp: true, First: true, FirstDown: true));
        Assert.False(tick.Swing.Committed, "the square beats the release on its tick, as cancel does");
        Assert.True(tick.Converted);
        Assert.False(plate.SwingCommitted);
        Assert.Equal(BuntSide.First, tick.Bunt.Showing);
        Assert.Equal(default, plate.Swing);

        // The explicit cancel while squared changes nothing: there is no load to discard.
        var cancel = Frame(ref plate, new Pad(First: true, Cancel: true));
        Assert.True(cancel.Bunt.Squared);
        Assert.False(cancel.Swing.Cancelled);
    }

    // ---------------------------------------------------------------------------------
    // S-162  A committed swing cannot become a bunt
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S162_ACommittedSwingFollowsThroughAndNoTriggerSquaresUntilTheNextPitch()
    {
        var plate = default(PlateButtonsState);
        Frame(ref plate, new Pad(South: true, SouthDown: true));
        Assert.True(Frame(ref plate, new Pad(SouthUp: true)).Swing.Committed);
        Assert.True(plate.SwingCommitted);
        var late = Frame(ref plate, new Pad(Third: true, ThirdDown: true));
        Assert.False(late.Bunt.Squared);
        Assert.False(late.Converted);
        Assert.Null(PlateButtons.HeldBuntAtPlate(late, 0));
        // The next pitch: the trigger that is still down squares (it was never used for a bunt).
        plate = plate.NextPitch();
        Assert.False(plate.SwingCommitted);
        Assert.Equal(BuntSide.Third, Frame(ref plate, new Pad(Third: true)).Bunt.Showing);
    }

    // ---------------------------------------------------------------------------------
    // S-163  The swing button while squared counts for nothing
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S163_TheSwingButtonWhileSquaredIsNotASwingAndASwingNeedsTheTriggersUpAndAFreshPress()
    {
        var plate = default(PlateButtonsState);
        Frame(ref plate, new Pad(Third: true, ThirdDown: true));
        var committed = false;
        committed |= Frame(ref plate, new Pad(Third: true, South: true, SouthDown: true)).Swing.Committed;
        for (var f = 0; f < 20; f++) committed |= Frame(ref plate, new Pad(Third: true, South: true)).Swing.Committed;
        Assert.False(plate.Swing.Armed);
        Assert.True(plate.Swing.MustRelease);
        // The triggers come up with South still down: the hold still cannot swing.
        committed |= Frame(ref plate, new Pad(South: true)).Swing.Committed;
        committed |= Frame(ref plate, new Pad(SouthUp: true)).Swing.Committed;
        Assert.False(committed);
        Assert.Equal(default, plate.Swing);
        // A fresh press with the bat withdrawn is a swing.
        Frame(ref plate, new Pad(South: true, SouthDown: true));
        Assert.True(Frame(ref plate, new Pad(SouthUp: true)).Swing.Committed);
    }

    // ---------------------------------------------------------------------------------
    // S-164  A trigger held at contact is spent until it comes up (PH-14-R6)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S164_ATriggerHeldAtContactCountsAsNoVerbUntilReleasedAndPressedAgain()
    {
        foreach (var third in new[] { true, false })
        {
            var trigger = third ? BuntSide.Third : BuntSide.First;
            var other = third ? BuntSide.First : BuntSide.Third;
            var down = third ? new Pad(Third: true, ThirdDown: true) : new Pad(First: true, FirstDown: true);
            var held = third ? new Pad(Third: true) : new Pad(First: true);
            var plate = default(PlateButtonsState);
            Frame(ref plate, down);
            plate = plate with { Bunt = BuntHold.Contact(plate.Bunt, thirdHeld: third, firstHeld: !third) };
            Assert.False(BuntHold.IsFree(plate.Bunt, trigger));
            Assert.True(BuntHold.IsFree(plate.Bunt, other));

            // Held through the live ball, the SET and the next pitch: spent, it squares nothing.
            for (var f = 0; f < 30; f++) Frame(ref plate, held, accepting: false);
            plate = plate.NextPitch();
            for (var f = 0; f < 30; f++)
            {
                var tick = Frame(ref plate, held);
                Assert.False(tick.Bunt.Squared);
                Assert.False(BuntHold.IsFree(plate.Bunt, trigger));
            }
            // Released: free. Pressed again: a new bunt.
            Frame(ref plate, new Pad());
            Assert.True(BuntHold.IsFree(plate.Bunt, trigger));
            Assert.Equal(trigger, Frame(ref plate, down).Bunt.Showing);
        }

        // Both triggers down at contact are both spent; one press edge frees only its own.
        var both = new BuntHoldState(BuntSide.First);
        both = BuntHold.Contact(both, thirdHeld: true, firstHeld: true).NextPitch();
        var s = BuntHold.Advance(both, true, true, false, true);
        Assert.Equal(BuntSide.Third, s.Showing);
        Assert.False(BuntHold.IsFree(s.Next, BuntSide.First));
    }

    // ---------------------------------------------------------------------------------
    // S-186  A cancel press the plate took is no other verb until it comes up (PH-13-R1)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S186_ACancelPressAtThePlateIsSpentUntilTheButtonComesUp()
    {
        PlateInput East(bool press, bool held) => new(false, false, false, Cancel: press, CancelHeld: held);
        var plate = default(PlateButtonsState);
        Assert.True(PlateButtons.CancelIsFree(plate));

        // A load, then East: the load is discarded and the press is the plate's.
        Frame(ref plate, new Pad(South: true, SouthDown: true));
        var cancel = PlateButtons.Advance(plate, new(false, true, false, Cancel: true, CancelHeld: true), Dt, ToFull);
        plate = cancel.Next;
        Assert.True(cancel.Swing.Cancelled);
        Assert.False(PlateButtons.CancelIsFree(plate));

        // Held through the rest of the pitch, a live ball (not accepting) and the next SET: still spent.
        for (var f = 0; f < 20; f++) plate = PlateButtons.Advance(plate, East(false, true), Dt, ToFull).Next;
        for (var f = 0; f < 20; f++) plate = PlateButtons.Advance(plate, East(false, true), Dt, ToFull, accepting: false).Next;
        plate = plate.NextPitch();
        Assert.False(PlateButtons.CancelIsFree(plate));

        // Up: free. A new press outside the plate (not accepting) is not the plate's and spends nothing.
        plate = PlateButtons.Advance(plate, East(false, false), Dt, ToFull).Next;
        Assert.True(PlateButtons.CancelIsFree(plate));
        plate = PlateButtons.Advance(plate, East(true, true), Dt, ToFull, accepting: false).Next;
        Assert.True(PlateButtons.CancelIsFree(plate));

        // At the plate a press with nothing loaded is still the plate's verb; a tap is spent on its own tick only.
        plate = PlateButtons.Advance(default, East(true, false), Dt, ToFull).Next;
        Assert.False(PlateButtons.CancelIsFree(plate));
        plate = PlateButtons.Advance(plate, East(false, false), Dt, ToFull).Next;
        Assert.True(PlateButtons.CancelIsFree(plate));

        // The plate-wide contact is BuntHold.Contact on the triggers down in that tick's input.
        var squared = default(PlateButtonsState);
        Frame(ref squared, new Pad(First: true, FirstDown: true));
        var hit = PlateButtons.Contact(squared, new(false, false, false, FirstHeld: true));
        Assert.Equal(BuntHold.Contact(squared.Bunt, thirdHeld: false, firstHeld: true), hit.Bunt);
        Assert.False(BuntHold.IsFree(hit.Bunt, BuntSide.First));
    }

    // ---------------------------------------------------------------------------------
    // S-165  One path for both seats and the CPU
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S165_TwoSeatsOnTwoPadsAndTheCpuLayDownTheSameHeldBunt()
    {
        // Two pads, two states: one seat's triggers never move the other seat's bat.
        var home = default(PlateButtonsState);
        var away = default(PlateButtonsState);
        Frame(ref home, new Pad(Third: true, ThirdDown: true));
        Frame(ref away, new Pad(First: true, FirstDown: true));
        for (var f = 0; f < 20; f++)
        {
            Assert.Equal(BuntSide.Third, Frame(ref home, new Pad(Third: true)).Bunt.Showing);
            Assert.Equal(BuntSide.First, Frame(ref away, new Pad(First: true)).Bunt.Showing);
        }

        // A pad's held bunt and the CPU's are one command, bar the seat flag no rule reads: the same ball.
        foreach (var side in new[] { BuntSide.Third, BuntSide.First })
        foreach (var seed in new[] { 3, 9 })
        {
            var pad = new Scenario(Shipped, seed).Match.Play(Scenario.PitchAt(0, CenterY), SwingCommand.HeldBunt(side, 0.1, 1.8, human: true));
            var cpu = new Scenario(Shipped, seed).Match.Play(Scenario.PitchAt(0, CenterY), SwingCommand.HeldBunt(side, 0.1, 1.8, human: false));
            Assert.Equal(pad.AtBat, cpu.AtBat);
            Assert.Equal(pad.Kind, cpu.Kind);
        }
    }

    // ---------------------------------------------------------------------------------
    // S-166  The CPU sac bunt holds a side, decided with the square
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S166_TheCpuSacBuntHoldsASideDecidedAtSetAndCarriesItOnTheSwing()
    {
        var sides = new HashSet<BuntSide>();
        var squared = 0;
        foreach (var seed in Enumerable.Range(1, 80))
        {
            var match = LightBatWithARunnerOnFirst(Shipped, seed);
            Assert.Equal(BuntSide.None, match.CpuBuntSide);
            if (!match.CpuSquaresBunt())
            {
                Assert.Equal(BuntSide.None, match.CpuBuntSide);
                continue;
            }
            squared++;
            var side = match.CpuBuntSide;
            Assert.NotEqual(BuntSide.None, side);
            sides.Add(side);
            // Idempotent for the pitch: the tell does not flicker.
            Assert.True(match.CpuSquaresBunt());
            Assert.Equal(side, match.CpuBuntSide);

            // Out of the zone the bat is pulled back: a take, the corners still in.
            var ball = match.CpuSwing(Scenario.PitchAt(2.5, CenterY));
            Assert.False(ball.Swing);
            Assert.True(ball.SquareSec > 0);
            // In the zone: the held bunt, toward the side, with no timed press.
            var bunt = match.CpuSwing(Scenario.PitchAt(0, CenterY));
            Assert.Equal((true, side, 0.0), (bunt.Bunt, bunt.BuntSide, bunt.TimingErrorFrames));
            var ev = match.Play(Scenario.PitchAt(0, CenterY), bunt);
            Assert.Equal(side, ev.Swing.BuntSide);
            Assert.Equal(BuntSide.None, match.CpuBuntSide);
        }
        Assert.True(squared > 5, $"{squared} of 80 seeds squared");
        Assert.Equal([BuntSide.Third, BuntSide.First], sides.Order());
    }

    // ---------------------------------------------------------------------------------
    // S-167  The bunt's exit is its own: no swing term reaches it, and no switch or old term is a rule
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S167_TheBuntsExitIsItsOwnByQualityAndNoSwingTermReachesIt()
    {
        var resolver = Resolver(Shipped);
        var park = Harbor(Shipped);
        var r = Shipped.Rules.Batting.Bunt.Response;
        var rio = Rio(Hand.R);
        foreach (var zone in new[] { ContactQuality.Perfect, ContactQuality.Nice })
        foreach (var seed in new[] { 1, 7, 42 })
        {
            var x = CrossingFor(Shipped, rio, zone, 0);
            var input = Input(Shipped, rio, BuntSide.None, 0, x, CenterY);
            var swing = resolver.Resolve(input with { Bunt = false }, park, new Random(seed));
            // The swing's charged pitch, the tired arm and the swing's own exit move nothing on the held bat.
            foreach (var bunt in new[]
                     {
                         resolver.Resolve(input, park, new Random(seed)),
                         resolver.Resolve(input with { ChargePitch = true }, park, new Random(seed)),
                         resolver.Resolve(input with { PitcherStamina = 1 }, park, new Random(seed)),
                     })
            {
                Assert.Equal(zone, bunt.Quality);
                Assert.Equal(r.ExitMph.For(zone), bunt.ExitVeloMph);
                Assert.InRange(bunt.LaunchDeg, Shipped.Rules.Batting.Bunt.LaunchMinDeg,
                    Shipped.Rules.Batting.Bunt.LaunchMinDeg + Shipped.Rules.Batting.Bunt.LaunchSpanDeg);
            }
            Assert.True(swing.ExitVeloMph > r.ExitMph.For(zone), $"{zone} seed {seed}: the swing {swing.ExitVeloMph} is the harder ball");
        }

        // The bunt table authors no switch and none of the retired swing-derived terms; a copy that does is refused by name.
        var opts = new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip };
        foreach (var (key, value) in new (string, JsonNode)[] { ("response.byContact", false), ("exitMul", 0.42), ("spraySpanDeg", 28) })
        {
            using var fixture = new ContentFixture();
            var path = fixture.Path("rules/batting.json");
            var json = JsonNode.Parse(File.ReadAllText(path), documentOptions: opts)!;
            var parts = key.Split('.');
            var node = json["bunt"]!;
            foreach (var part in parts[..^1]) node = node[part]!;
            Assert.False(node.AsObject().ContainsKey(parts[^1]), key);
            node[parts[^1]] = value;
            File.WriteAllText(path, json.ToJsonString());
            Assert.Contains(RulesTable.Validate(new DataRoot(fixture.Root)),
                e => e.Contains($"batting.bunt.{key} is not a rule this table owns", StringComparison.Ordinal));
        }
    }

    // ---------------------------------------------------------------------------------
    // S-168  A square bunt is the dead, controlled one, and Power does not harden it (PH-14-R1)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S168_ASquareBuntIsTheDeadControlledOneAndPowerDoesNotHardenIt()
    {
        var r = Shipped.Rules.Batting.Bunt.Response;
        Assert.True(r.ExitMph.Perfect < r.ExitMph.Nice && r.ExitMph.Nice < r.ExitMph.Sour, "better contact is softer");
        Assert.True(r.SpreadDeg.Perfect < r.SpreadDeg.Nice && r.SpreadDeg.Nice < r.SpreadDeg.Sour, "better contact is tighter");
        Assert.True(r.ExitMph.Sour >= Shipped.Rules.Fielding.Bunt.HardExitMph, "a poor chop is too hard for the sac");
        var resolver = Resolver(Shipped);
        var park = Harbor(Shipped);
        var lean = BuntHold.LeanDeg(BuntSide.First, Shipped.Rules);
        foreach (var zone in new[] { ContactQuality.Perfect, ContactQuality.Nice })
        {
            var widest = 0.0;
            foreach (var power in new[] { 2, 9 })
            {
                var batter = Rio(Hand.R) with { Stats = Rio(Hand.R).Stats with { Power = power } };
                var x = CrossingFor(Shipped, batter, zone, 0);
                for (var seed = 1; seed <= 100; seed++)
                {
                    var hit = resolver.Resolve(Input(Shipped, batter, BuntSide.First, 0, x, CenterY), park, new Random(seed));
                    Assert.Equal(zone, hit.Quality);
                    Assert.Equal(r.ExitMph.For(zone), hit.ExitVeloMph);
                    widest = Math.Max(widest, Math.Abs(hit.SprayDeg - lean));
                }
            }
            Assert.True(widest <= r.SpreadDeg.For(zone) / 2 + 0.1, $"{zone}: {widest}");
        }
    }

    // ---------------------------------------------------------------------------------
    // S-169  A sour bunt pops above the bat's center and is chopped below it
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S169_ASourBuntPopsAboveTheBatsCenterAndIsChoppedHardBelowIt()
    {
        var resolver = Resolver(Shipped);
        var park = Harbor(Shipped);
        var b = Shipped.Rules.Batting;
        var rio = Rio(Hand.R);
        foreach (var seed in new[] { 1, 7, 42 })
        {
            var above = CrossingFor(Shipped, rio, ContactQuality.Sour, 0.3);
            var pop = resolver.Resolve(Input(Shipped, rio, BuntSide.Third, 0, above, CenterY + 0.3), park, new Random(seed));
            Assert.Equal(ContactQuality.Sour, pop.Quality);
            Assert.True(pop.LaunchDeg >= b.Launch.PopMinDeg, $"above: {pop.LaunchDeg}");

            var below = CrossingFor(Shipped, rio, ContactQuality.Sour, -0.3);
            var chop = resolver.Resolve(Input(Shipped, rio, BuntSide.Third, 0, below, CenterY - 0.3), park, new Random(seed));
            Assert.Equal(ContactQuality.Sour, chop.Quality);
            Assert.InRange(chop.LaunchDeg, b.Bunt.LaunchMinDeg, b.Bunt.LaunchMinDeg + b.Bunt.LaunchSpanDeg);
            Assert.Equal(b.Bunt.Response.ExitMph.Sour, chop.ExitVeloMph);
        }
        // A crossing high over the zone still pops whatever the quality.
        var high = resolver.Resolve(Input(Shipped, rio, BuntSide.Third, 0, 0, CenterY + b.Bunt.PopAboveCenterFt + 0.1), park, new Random(1));
        Assert.True(high.LaunchDeg >= b.Launch.PopMinDeg);
    }

    // ---- helpers ---------------------------------------------------------------------

    static AtBatResolver Resolver(ContentCatalog content) => new(content.Chemistry, content.Rules, content.StarSkills);

    static Park Harbor(ContentCatalog content) => content.Parks[ExhibitionPick.DefaultPark];

    static Character Rio(Hand bats) => Shipped.Must("rio") with { Bats = bats };

    /// <summary>A held bunt at one crossing against a Pitch-5 arm (the pitch factor ×1).</summary>
    static AtBatInput Input(ContentCatalog content, Character batter, BuntSide side, double err, double crossingX, double crossingY)
    {
        var arm = content.Must("vale");
        arm = arm with { Stats = arm.Stats with { Pitch = 5 } };
        return new AtBatInput(arm, batter, null, [],
            ChargePitch: false, ChangeupPitch: false, TimingErrorFrames: err,
            UseStarPitch: false, UseStarSwing: false, Bat: content.Bats["harbor-lumber"], PitcherStamina: 80,
            Bunt: true, CrossingX: crossingX, CrossingY: crossingY, BuntSide: side);
    }

    /// <summary>
    /// The crossing X, from the heart toward the tip, where this hitter's held bunt meets a ball <paramref name="dy"/>
    /// feet off the bat's center in <paramref name="zone"/>. Found by asking the cursor, so no geometry is stored here.
    /// </summary>
    static double CrossingFor(ContentCatalog content, Character batter, ContactQuality zone, double dy)
    {
        var barrel = SweetSpot.SwingBarrel(batter, content.Bats["harbor-lumber"], 0, content.Rules);
        var tip = SweetSpot.TipSign(batter.Bats);
        for (var i = 0; i <= 400; i++)
        {
            var x = tip * i * 0.01;
            if (SweetSpot.Zone(0, batter.Bats, x, CenterY + dy, content.Rules, barrel) == zone) return x;
        }
        throw new InvalidOperationException($"no {zone} crossing at dy {dy}");
    }

    /// <summary>The <c>BuntScenarioTests</c> setup: pip (a light bat) leads off, a runner on first, nobody out.</summary>
    static Match LightBatWithARunnerOnFirst(ContentCatalog content, int seed)
    {
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = content.Team("Offense", "zig", "pip", "dart", "jester", "cinder", "grit", "soot", "boom", "nugget");
        var match = Match.Exhibition(content, home, away, 3, seed);
        Assert.Equal("pip", match.Batter.Id);
        Assert.True(match.StationRunner(1, content.Must("dart")));
        return match;
    }
}
