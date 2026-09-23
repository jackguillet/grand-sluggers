using System.Text;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Spec Appendix B.1 rows S-101 … S-106 (#812): the pre-charge pitch selection — one button cycles
/// the pitcher's three ordinary families and wraps, an unauthored family is skipped, the charge
/// locks what is showing, the commit throws the locked family, and every SET starts on the fastball
/// (PH-02-R3/R4/R5, PH-15-R1).
///
/// The charge button is never hand-built here: every row drives the real
/// <see cref="ChargeButton.Advance"/> through <see cref="Mound"/> — press, hold frames, release,
/// <c>accepting</c> on and off — so the same-tick arm edge and the mid-hold disarm are proved
/// against the button the mound actually uses, not against a shape a test invented.
///
/// Nothing in the running game reads the step yet (P1-f wires the mound), so no row here asserts a
/// pitch, a speed or a count.
/// </summary>
public sealed class PitchSelectionScenarioTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    /// <summary>What the shipped <c>pitching.json</c> authors today: fastball and changeup (#810).</summary>
    static IReadOnlyList<string> ShippedAuthored => new PitchFamilyTable().Authored;

    /// <summary>A table that authors the whole library, so the cycle is the full accepted order (P1-d's future).</summary>
    static IReadOnlyList<string> AllAuthored => PitchFamily.All;

    /// <summary>The 25 shipped characters in a stable order: left and right hands, captains and role players.</summary>
    IReadOnlyList<Character> Roster => _content.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal).ToList();

    // ---------------------------------------------------------------------------------
    // S-101  Zero, one, two, three presses = fastball, second, third, fastball — all 25
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S101_PressesWalkTheWholeRepertoireAndWrapForEveryShippedPitcher()
    {
        var roster = Roster;
        Assert.Equal(25, roster.Count);
        Assert.Contains(roster, c => c.Throws == Hand.L);
        Assert.Contains(roster, c => c.Throws == Hand.R);
        Assert.Contains(roster, c => c.Captain);
        Assert.Contains(roster, c => !c.Captain);

        foreach (var who in roster)
        {
            var rep = who.Repertoire;
            var expected = new[] { PitchFamily.Fastball, rep.Second, rep.Third, PitchFamily.Fastball };

            for (var presses = 0; presses < expected.Length; presses++)
            {
                // A fresh SET each time: the count of presses is the whole input (PH-02-R5).
                var mound = new Mound(rep, AllAuthored);
                for (var i = 0; i < presses; i++) mound.Tick(cycle: true);

                // SET keeps ticking after the last press: the idle frame reads the same family.
                var settled = mound.Tick();
                Assert.Equal(presses % Repertoire.Slots, mound.Selection.Slot);
                Assert.False(mound.Selection.Locked);
                Assert.Equal(expected[presses], settled.Family);
                Assert.Equal(expected[presses], PitchSelection.FamilyAt(mound.Selection, rep, AllAuthored));
            }

            // A press on a tick with no cycle does nothing, and the slot never leaves the repertoire.
            var idle = new Mound(rep, AllAuthored);
            for (var i = 0; i < 10; i++)
            {
                idle.Tick();
                Assert.Equal(0, idle.Selection.Slot);
            }
        }
    }

    // ---------------------------------------------------------------------------------
    // S-102  The charge locks the family: later presses are ignored, quick and MAX alike
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void S102_CyclePressesAfterTheArmEdgeAreIgnoredAndTheCommitThrowsTheLockedFamily(bool maxRelease)
    {
        var rep = _content.Must("rio").Repertoire;
        var mound = new Mound(rep, AllAuthored);

        // Two presses before the charge: the third pitch is showing.
        mound.Tick(cycle: true);
        mound.Tick(cycle: true);
        Assert.Equal(rep.Third, mound.Last.Family);

        var armed = mound.Tick(pressed: true, held: true);
        Assert.True(armed.LockedThisTick);
        Assert.True(mound.Selection.Locked);
        Assert.Equal(2, mound.Selection.Slot);

        // Hold: to MAX for the charged release, one frame for the quick one. Every held frame
        // presses cycle, and every press is ignored while the charge is up (PH-02-R3).
        var holdFrames = maxRelease ? 40 : 1;
        for (var i = 0; i < holdFrames; i++)
        {
            var step = mound.Tick(cycle: true, held: true);
            Assert.False(step.Committed);
            Assert.True(step.Next.Locked);
            Assert.Equal(2, step.Next.Slot);
            Assert.Equal(rep.Third, step.Family);
        }

        Assert.Equal(maxRelease, mound.Button.Fill01 >= 1);

        // The release commits the locked family; a cycle press on the release tick changes nothing.
        var commit = mound.Tick(cycle: true, released: true);
        Assert.True(commit.Committed);
        Assert.Equal(rep.Third, commit.Family);
        Assert.Equal(PitchSelectionState.Reset, commit.Next);
        Assert.True(ChargeFeel.IsCharge(mound.LastButton.CommitFill01) == maxRelease);
    }

    // ---------------------------------------------------------------------------------
    // S-103  Cycle and arm on one tick: cycle first, then lock — no press is dropped
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S103_ACyclePressOnTheArmingTickIsAppliedBeforeTheLock()
    {
        var rep = _content.Must("rio").Repertoire;

        // Press the cycle and the charge on the same tick: the next family is what locks.
        var together = new Mound(rep, AllAuthored);
        var armed = together.Tick(cycle: true, pressed: true, held: true);
        Assert.True(armed.LockedThisTick);
        Assert.Equal(1, armed.Next.Slot);
        Assert.Equal(rep.Second, armed.Family);

        var commit = together.Tick(released: true);
        Assert.True(commit.Committed);
        Assert.Equal(rep.Second, commit.Family);

        // Press and release on one tick: the button commits without ever reporting Armed, and that
        // same-tick commit is an arm edge too — it locks and throws the family after the cycle.
        var oneTick = new Mound(rep, AllAuthored);
        var atOnce = oneTick.Tick(cycle: true, pressed: true, held: true, released: true);
        Assert.True(atOnce.Committed);
        Assert.True(atOnce.LockedThisTick);
        Assert.Equal(rep.Second, atOnce.Family);
        Assert.Equal(PitchSelectionState.Reset, atOnce.Next);

        // Two cycle presses on two ticks and the arm on the second: one press is one advance.
        var twice = new Mound(rep, AllAuthored);
        twice.Tick(cycle: true);
        var second = twice.Tick(cycle: true, pressed: true, held: true);
        Assert.Equal(2, second.Next.Slot);
        Assert.Equal(rep.Third, second.Family);
    }

    // ---------------------------------------------------------------------------------
    // S-104  Every SET entry starts on the fastball, unlocked — and so does a new pitcher
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S104_EverySetEntryAndEveryPitcherSwapStartsOnTheFastballUnlocked()
    {
        // The reset value itself: slot 0, unlocked, and the fastball for all 25 repertoires, so a
        // new SET, a SET after a pickoff and a SET after a foul all start in the same place.
        Assert.Equal(0, PitchSelectionState.Reset.Slot);
        Assert.False(PitchSelectionState.Reset.Locked);
        foreach (var who in Roster)
        {
            Assert.Equal(PitchFamily.Fastball,
                PitchSelection.FamilyAt(PitchSelectionState.Reset, who.Repertoire, AllAuthored));
            Assert.Equal(PitchFamily.Fastball,
                PitchSelection.FamilyAt(PitchSelectionState.Reset, who.Repertoire, ShippedAuthored));
        }

        var match = Match.Slice(_content, innings: 3, seed: 1);
        Assert.Equal(PitchFamily.Fastball, match.FamilyAt(PitchSelectionState.Reset));

        // A delivery resets by itself: the commit tick hands back Reset, so the next SET is fastball
        // even if the caller forgets.
        var pitcher = match.Pitcher;
        var state = PitchSelectionState.Reset;
        var button = default(ChargeButtonState);
        var cycled = match.SelectPitch(state, cyclePressed: true, selectable: true, button, default);
        Assert.Equal(pitcher.Repertoire.Second, cycled.Family);
        var press = ChargeButton.Advance(button, pressed: true, held: true, released: true,
            deltaSeconds: 1.0 / 60, secondsToFull: 0.55);
        var thrown = match.SelectPitch(cycled.Next, cyclePressed: false, selectable: true, button, press);
        Assert.True(thrown.Committed);
        Assert.Equal(pitcher.Repertoire.Second, thrown.Family);
        Assert.Equal(PitchSelectionState.Reset, thrown.Next);

        // A SET after a foul.
        var foul = FlightFixtures.Hit(match.Park, exitMph: 90, launchDeg: 20, sprayDeg: 60);
        Assert.True(foul.Foul);
        var preview = match.PreviewHit(foul);
        var dead = new FieldingResult(PlayKind.Foul, preview.Fielder, null, preview.HangTimeSec,
            preview.LandingX, preview.LandingZ, false, false);
        Assert.Equal(PlayKind.Foul, match.FinishAtBat(Scenario.Paint, Scenario.Swing, foul, dead).Kind);
        Assert.Equal(PitchFamily.Fastball, match.FamilyAt(PitchSelectionState.Reset));

        // A SET after a legal pitcher throw: the on-bag runner remains safe.
        Assert.True(match.StationRunner(1, match.AwayOrder[0]));
        var beat = match.Pickoff(1);
        Assert.NotNull(beat);
        Assert.Equal(PitchFamily.Fastball, match.FamilyAt(PitchSelectionState.Reset));

        // A swap to a pitcher with a different repertoire: the reads follow the new arm, and the
        // cycle lands on the new pitcher's second pitch, not the old one's.
        var old = match.Pitcher;
        var next = match.DefenseRoster.First(c =>
            !c.Id.Equals(old.Id, StringComparison.OrdinalIgnoreCase) && !c.Repertoire.Equals(old.Repertoire));
        Assert.True(match.SwapPitcher(next));
        Assert.Equal(next.Id, match.Pitcher.Id);
        Assert.Equal(PitchFamily.Fastball, match.FamilyAt(PitchSelectionState.Reset));

        var afterSwap = match.SelectPitch(PitchSelectionState.Reset, cyclePressed: true, selectable: true,
            default, default);
        Assert.Equal(next.Repertoire.Second, afterSwap.Family);
        Assert.Equal(next.Repertoire.Second, match.FamilyAt(afterSwap.Next));
    }

    // ---------------------------------------------------------------------------------
    // S-105  The shipped table: a family that cannot fly is never selected
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S105_UnauthoredFamiliesAreSkippedSoNoPressSelectsAPitchThatCannotFly()
    {
        Assert.Equal(new[] { PitchFamily.Fastball, PitchFamily.Changeup }, ShippedAuthored);

        // Rio throws changeup + curveball: the curveball has no row, so the cycle is a two-stop
        // loop between the fastball and the changeup.
        var rio = _content.Must("rio").Repertoire;
        Assert.Equal(PitchFamily.Curveball, rio.Third);
        var mound = new Mound(rio, ShippedAuthored);
        foreach (var expected in new[] { PitchFamily.Changeup, PitchFamily.Fastball, PitchFamily.Changeup })
            Assert.Equal(expected, mound.Tick(cycle: true).Family);

        // Vale throws curveball + slider: neither has a row, so every press stays on the fastball.
        var vale = _content.Must("vale").Repertoire;
        Assert.DoesNotContain(vale.Second, ShippedAuthored);
        Assert.DoesNotContain(vale.Third, ShippedAuthored);
        var only = new Mound(vale, ShippedAuthored);
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(PitchFamily.Fastball, only.Tick(cycle: true).Family);
            Assert.Equal(0, only.Selection.Slot);
        }

        // No press, for any of the 25, ever selects a family the table cannot fly.
        foreach (var who in Roster)
        {
            var seat = new Mound(who.Repertoire, ShippedAuthored);
            for (var press = 0; press < 12; press++)
            {
                var step = seat.Tick(cycle: true);
                Assert.Contains(step.Family, ShippedAuthored);
                Assert.True(who.Repertoire.Has(step.Family), $"{who.Id} does not throw {step.Family}");
            }
        }
    }

    // ---------------------------------------------------------------------------------
    // S-106  The disarm edge, and a seat that may not cycle
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S106_DisarmWithoutACommitReleasesTheLockAndKeepsTheSlotAndAClosedSeatIgnoresThePress()
    {
        var rep = _content.Must("rio").Repertoire;
        var mound = new Mound(rep, AllAuthored);

        mound.Tick(cycle: true);
        mound.Tick(cycle: true);
        Assert.Equal(2, mound.Selection.Slot);
        mound.Tick(pressed: true, held: true);
        Assert.True(mound.Selection.Locked);

        // The swap pick opens mid-hold: the button stops accepting, which discards the charge
        // without a delivery (§4.7). The lock releases; the slot the pitcher had chosen stays.
        var disarmed = mound.Tick(held: true, accepting: false, selectable: false);
        Assert.False(disarmed.Committed);
        Assert.False(disarmed.Next.Locked);
        Assert.Equal(2, disarmed.Next.Slot);
        Assert.Equal(rep.Third, disarmed.Family);
        Assert.False(mound.Button.Armed);

        // While the pick is open the seat may not cycle: the press is ignored, not banked.
        var ignored = mound.Tick(cycle: true, selectable: false, accepting: false);
        Assert.Equal(2, ignored.Next.Slot);
        Assert.Equal(rep.Third, ignored.Family);

        // The pick closes: the same press now moves, from where the pitcher left off.
        var resumed = mound.Tick(cycle: true);
        Assert.Equal(0, resumed.Next.Slot);
        Assert.Equal(PitchFamily.Fastball, resumed.Family);

        // Cycling before the pitcher-ready beat is allowed: the button is not accepting yet, so
        // nothing arms and nothing locks, but the selection moves (a selection is not a delivery).
        var early = new Mound(rep, AllAuthored);
        var beforeReady = early.Tick(cycle: true, pressed: true, held: true, accepting: false);
        Assert.Equal(1, beforeReady.Next.Slot);
        Assert.False(beforeReady.Next.Locked);
        Assert.False(beforeReady.LockedThisTick);
        Assert.False(early.Button.Armed);
    }

    // ---------------------------------------------------------------------------------
    // S-106b  A lock exists only while the charge is armed, even if the caller skips a tick
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S106b_ALockWhoseButtonIsNoLongerArmedIsStaleSoASkippedReleaseCannotStrandTheSelection()
    {
        var rep = _content.Must("rio").Repertoire;
        var mound = new Mound(rep, AllAuthored);

        // The third pitch is showing and the charge arms on it.
        mound.Tick(cycle: true);
        mound.Tick(cycle: true);
        mound.Tick(pressed: true, held: true);
        Assert.Equal(new PitchSelectionState(2, true), mound.Selection);

        // While the charge is genuinely armed, a cycle press is still ignored (the normal path).
        var held = mound.Tick(cycle: true, held: true);
        Assert.Equal(new PitchSelectionState(2, true), held.Next);
        Assert.Equal(rep.Third, held.Family);

        // Now SET returns early on the tick the charge goes away — the swap pick opens, the game
        // pauses, a tutorial gate fires — so the button disarms but the selection step never runs
        // and never sees the releasing edge. The state is left locked with a disarmed button.
        mound.SkipTick(held: true, accepting: false);
        Assert.False(mound.Button.Armed);
        Assert.True(mound.Selection.Locked, "the skipped tick is exactly what strands the lock");

        // The next tick reads that lock as stale: the press cycles instead of being swallowed.
        var resumed = mound.Tick(cycle: true);
        Assert.False(resumed.Next.Locked);
        Assert.Equal(0, resumed.Next.Slot);
        Assert.Equal(PitchFamily.Fastball, resumed.Family);

        // And the next charge locks afresh, then throws what it locked.
        var relocked = mound.Tick(cycle: true, pressed: true, held: true);
        Assert.True(relocked.LockedThisTick);
        Assert.Equal(1, relocked.Next.Slot);
        var commit = mound.Tick(cycle: true, released: true);
        Assert.True(commit.Committed);
        Assert.Equal(rep.Second, commit.Family);
        Assert.Equal(PitchSelectionState.Reset, commit.Next);

        // A stale lock plus a same-tick press-and-release still locks and throws on that tick.
        var stranded = new PitchSelectionState(2, true);
        var atOnce = ChargeButton.Advance(default, pressed: true, held: true, released: true,
            deltaSeconds: 1.0 / 60, secondsToFull: 0.55);
        var thrown = PitchSelection.Advance(stranded, cyclePressed: false, selectable: true,
            default, atOnce, rep, AllAuthored);
        Assert.True(thrown.Committed);
        Assert.True(thrown.LockedThisTick);
        Assert.Equal(rep.Third, thrown.Family);
    }

    // ---------------------------------------------------------------------------------
    // Determinism (S-90 shape) and the defensive read
    // ---------------------------------------------------------------------------------

    [Fact]
    public void TheSameInputStreamGivesTheSameStateStreamForEveryRepertoire()
    {
        foreach (var who in Roster)
        foreach (var authored in new[] { ShippedAuthored, AllAuthored })
        {
            var first = Stream(who.Repertoire, authored);
            var second = Stream(who.Repertoire, authored);
            Assert.Equal(first, second);
            Assert.NotEqual("", first);
        }
    }

    [Fact]
    public void AStaleSlotReadsAsTheFastballAndATableWithNoFastballStopsByName()
    {
        var rep = _content.Must("rio").Repertoire;

        // A slot no repertoire has (a caller's stale value).
        foreach (var slot in new[] { -1, 3, 7, int.MinValue, int.MaxValue })
        {
            var stale = new PitchSelectionState(slot, false);
            Assert.Equal(PitchFamily.Fastball, PitchSelection.FamilyAt(stale, rep, AllAuthored));
            var step = PitchSelection.Advance(stale, cyclePressed: false, selectable: true,
                default, default, rep, AllAuthored);
            Assert.Equal(0, step.Next.Slot);
            Assert.Equal(PitchFamily.Fastball, step.Family);

            // A cycle press from a stale slot advances from the fastball, not from nowhere.
            var cycled = PitchSelection.Advance(stale, cyclePressed: true, selectable: true,
                default, default, rep, AllAuthored);
            Assert.Equal(1, cycled.Next.Slot);
            Assert.Equal(rep.Second, cycled.Family);
        }

        // The mound changed without a reset and the new arm's slot 2 has no authored row: the
        // fastball every pitcher throws, never a pitch that would stop the delivery.
        var stillOnTheThird = new PitchSelectionState(2, true);
        Assert.Equal(PitchFamily.Fastball, PitchSelection.FamilyAt(stillOnTheThird, rep, ShippedAuthored));
        var read = PitchSelection.Advance(stillOnTheThird, cyclePressed: false, selectable: true,
            default, default, rep, ShippedAuthored);
        Assert.Equal(0, read.Next.Slot);
        Assert.Equal(PitchFamily.Fastball, read.Family);

        // A table that authors no fastball is a broken table, not a press: it stops and names itself.
        var noFastball = new[] { PitchFamily.Changeup };
        var blew = Assert.Throws<InvalidOperationException>(() =>
            PitchSelection.Advance(PitchSelectionState.Reset, cyclePressed: false, selectable: true,
                default, default, rep, noFastball));
        Assert.Contains(PitchFamily.Fastball, blew.Message);
        Assert.Throws<InvalidOperationException>(() =>
            PitchSelection.FamilyAt(PitchSelectionState.Reset, rep, noFastball));
    }

    /// <summary>A scripted SET: cycles, a charge that is discarded, a cycle, then a MAX delivery.</summary>
    static string Stream(Repertoire rep, IReadOnlyList<string> authored)
    {
        var mound = new Mound(rep, authored);
        var log = new StringBuilder();
        void Note(PitchSelectionStep s) =>
            log.Append(s.Next.Slot).Append(s.Next.Locked ? 'L' : '-')
                .Append(s.LockedThisTick ? '^' : '-').Append(s.Committed ? '!' : '-')
                .Append(' ').Append(s.Family).Append('\n');

        Note(mound.Tick(cycle: true));
        Note(mound.Tick(cycle: true));
        Note(mound.Tick(pressed: true, held: true));
        for (var i = 0; i < 3; i++) Note(mound.Tick(cycle: true, held: true));
        Note(mound.Tick(held: true, accepting: false, selectable: false));
        Note(mound.Tick(cycle: true));
        Note(mound.Tick(pressed: true, held: true));
        for (var i = 0; i < 40; i++) Note(mound.Tick(held: true));
        Note(mound.Tick(released: true));
        Note(mound.Tick(cycle: true));
        return log.ToString();
    }

    /// <summary>
    /// One pitching seat for a test row: the real <see cref="ChargeButton"/> and the selection
    /// stepped together, exactly as <c>AtBatDirector.TickSet</c> ticks them (the previous button
    /// state is what the arm edge is read from).
    /// </summary>
    sealed class Mound(Repertoire repertoire, IReadOnlyList<string> authored)
    {
        public ChargeButtonState Button { get; private set; }
        public ChargeButtonStep LastButton { get; private set; }
        public PitchSelectionState Selection { get; private set; }
        public PitchSelectionStep Last { get; private set; }

        public PitchSelectionStep Tick(
            bool cycle = false,
            bool pressed = false,
            bool held = false,
            bool released = false,
            bool selectable = true,
            bool accepting = true,
            double deltaSeconds = 1.0 / 60,
            double secondsToFull = 0.55)
        {
            var previous = Button;
            LastButton = ChargeButton.Advance(previous, pressed, held, released, deltaSeconds,
                secondsToFull, accepting);
            Button = LastButton.Next;
            Last = PitchSelection.Advance(Selection, cycle, selectable, previous, LastButton,
                repertoire, authored);
            Selection = Last.Next;
            return Last;
        }

        /// <summary>
        /// A SET frame the caller returned early from: the button still ticks, the selection step
        /// never runs. The selection keeps whatever the last tick left it, which is how a lock
        /// outlives the charge that made it.
        /// </summary>
        public void SkipTick(
            bool pressed = false,
            bool held = false,
            bool released = false,
            bool accepting = true,
            double deltaSeconds = 1.0 / 60,
            double secondsToFull = 0.55)
        {
            LastButton = ChargeButton.Advance(Button, pressed, held, released, deltaSeconds,
                secondsToFull, accepting);
            Button = LastButton.Next;
        }
    }
}
