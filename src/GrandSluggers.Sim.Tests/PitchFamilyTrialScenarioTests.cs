using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Spec Appendix B.1 rows S-107 … S-113 (#818): the curveball, the slider and the sinker flying
/// under the trial overlay <c>trials/pitch5</c>, and the shipped root still refusing them by name.
///
/// <b>#860:</b> Jack played that window and accepted it on September 22, 2026 ("trial was
/// good."), so the three rows are now in the shipped <c>pitching.json</c>. #883 retired the
/// <c>trials/pitch5</c> folder, so <see cref="Trial"/> is the shipped table: S-107 … S-112 hold the
/// shipped rows to the same roles. S-113 holds the promotion.
///
/// <b>Every row asserts a relationship, never a proposed number.</b> The numbers are Jack's to judge
/// in the trial window (PH-20-R1) and he is expected to change them; what he must not be able to
/// change without hearing about it is a family's <i>role</i> — the sinker is faster than the slider,
/// the slider sweeps farther than the curveball, the curveball arcs highest and drops last, every
/// one of them can be thrown for a strike and covered by a batter who reads it. So a row here fails
/// when a role breaks, not when a value moves.
///
/// The shipped table is loaded <b>in process</b>, through a <see cref="DataRoot"/> this class builds
/// from the repository, so nothing depends on <c>GRAND_SLUGGERS_TRIAL</c> being set and CI is untouched.
/// </summary>
[Trait("Kind", "Balance")]
public sealed class PitchFamilyTrialScenarioTests
{
    readonly ContentCatalog _shippedContent = ContentCatalog.Load(new DataRoot(ContentCatalog.Load().Root.Shipped));

    string Shipped => _shippedContent.Root.Shipped;

    /// <summary>
    /// The table the three families were proposed in: since #860 the shipped table (the
    /// <c>trials/pitch5</c> overlay that once carried them was retired by #883).
    /// </summary>
    RulesTable Trial => ShippedRules;
    RulesTable ShippedRules => _shippedContent.Rules;

    static readonly string[] Proposed = [PitchFamily.Curveball, PitchFamily.Slider, PitchFamily.Sinker];

    /// <summary>
    /// The margin a geometric claim leaves itself, in feet: the <b>real</b> ball's radius. The drawn
    /// ball is <see cref="Baseball.DiameterFt"/> = 0.62 ft, which that file says outright is a
    /// readability mesh over a ball "~0.25" ft across — it decides how big the ball looks, not where
    /// it is. A margin at the drawn ball's radius would also refuse the shipped changeup, which
    /// crosses 0.20 ft above the zone floor.
    /// </summary>
    const double BallRadiusFt = 0.125;

    /// <summary>
    /// How fast a batter walks the box, in feet per second: the Unity at-bat seat moves the box by
    /// <c>box.StickX * dt * 1.6</c> box-units (AtBatDirector), and <see cref="HomeSet.BatterWalk"/>
    /// is feet per box-unit. Full stick, which is the only speed there is.
    /// </summary>
    const double BoxWalkPerSec = 1.6 * HomeSet.BatterWalk;

    static readonly double[] UGrid = Enumerable.Range(0, 201).Select(i => i / 200.0).ToArray();

    // ---------------------------------------------------------------------------------
    // S-107  Speed: the order, the shipped envelope, and no new lean on the air clamp
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S107_EveryFamilyIsSlowerThanTheFastballFasterThanTheChangeupAndNeverClamps()
    {
        // Air time rides on Diamond.Mound, which is process-wide.
        var rules = Trial;
        var flight = rules.Pitching.Flight;

        foreach (var stat in new[] { 1, 5, 10 })
            foreach (var charge in new[] { 0.0, 0.4, 1.0 })
                foreach (var nice in new[] { false, true })
                {
                    double Mph(string family) => AtBatResolver.PitchSpeedMph(
                        new PitchCommand(family, charge, false, Nice: nice), stat, rules);

                    var fastball = Mph(PitchFamily.Fastball);
                    var sinker = Mph(PitchFamily.Sinker);
                    var slider = Mph(PitchFamily.Slider);
                    var curveball = Mph(PitchFamily.Curveball);
                    var changeup = Mph(PitchFamily.Changeup);
                    var where = $"stat {stat}, charge {charge}, nice {nice}";

                    // The order (PH-02-R2): the sinker is the fast one, the curveball the slow one.
                    Assert.True(fastball > sinker, $"{where}: fastball {fastball} vs sinker {sinker}");
                    Assert.True(sinker > slider, $"{where}: sinker {sinker} vs slider {slider}");
                    Assert.True(slider > curveball, $"{where}: slider {slider} vs curveball {curveball}");
                    Assert.True(curveball > changeup, $"{where}: curveball {curveball} vs changeup {changeup}");

                    // Inside the shipped envelope, stated as the issue states it: at the same stat,
                    // charge and release, every new family sits between the two shipped ones. So no
                    // pitch in the library is faster than the fastball or slower than the changeup,
                    // and D7's speed envelope is untouched.
                    foreach (var family in Proposed)
                    {
                        var mph = Mph(family);
                        Assert.InRange(mph, changeup, fastball);

                        // And its air time therefore sits strictly inside the shipped pair's, which
                        // is what "no new lean on the clamp" means: the shipped fastball at its
                        // fastest and the shipped changeup at its slowest are both inside
                        // [airMinSec, airMaxSec], so anything between them is too.
                        var air = PitchFlight.AirSeconds(mph, rules);
                        Assert.InRange(air, PitchFlight.AirSeconds(fastball, rules), PitchFlight.AirSeconds(changeup, rules));
                        Assert.True(air > flight.AirMinSec, $"{where}: {family} air {air} is on the floor");
                        Assert.True(air < flight.AirMaxSec, $"{where}: {family} air {air} is on the ceiling");
                    }
                }
    }

    // ---------------------------------------------------------------------------------
    // S-108  Strike-capable from the middle of the rubber, and the gold oval reaches it
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S108_EveryFamilyCrossesInsideTheZoneForBothArmsAndTheCursorCanCoverIt()
    {
        var rules = Trial;

        foreach (var family in PitchFamily.All)
            foreach (var throws in new[] { Hand.L, Hand.R })
            {
                // The middle of the rubber, no aim, no stick, no charge: what the pitch does by
                // itself. Nobody aims height in this game, so this crossing has to be a strike.
                var pitch = new PitchCommand(family, 0, false, Throws: throws);
                var (x, y) = PitchFlight.Crossing(pitch, rules: rules);
                var where = $"{family}/{throws}";

                Assert.True(Math.Abs(x) + BallRadiusFt <= StrikeZoneGeometry.HalfWidth,
                    $"{where}: crossing x {x} is not a ball inside the zone's {StrikeZoneGeometry.HalfWidth} ft half-width");
                Assert.True(y - BallRadiusFt >= StrikeZoneGeometry.Bottom, $"{where}: crossing y {y} is on the floor");
                Assert.True(y + BallRadiusFt <= StrikeZoneGeometry.Top, $"{where}: crossing y {y} is on the ceiling");
                Assert.True(StrikeZoneGeometry.Contains(x, y), where);

                // Reachable, not merely legal: a batter standing where the box starts them, with an
                // ordinary bat and no charge, has the crossing inside the drawn oval (§5.2, D4).
                foreach (var bats in new[] { Hand.L, Hand.R })
                    Assert.True(SweetSpot.Distance(0, bats, x, y, 1, rules) <= 1,
                        $"{where}: a {bats} batter's nice oval does not reach ({x}, {y})");
            }
    }

    // ---------------------------------------------------------------------------------
    // S-109  Height: the curveball arcs and drops, the sinker rides, the slider is flat
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S109_TheArcTheRideAndTheFlatOneAreThreeDifferentHeightPaths()
    {
        var rules = Trial;

        // The cross-field rule every authored row must satisfy: the drop finishes in flight.
        Assert.Empty(RulesTable.Validate(new DataRoot(Shipped)));

        var fastball = Heights(PitchFamily.Fastball, rules);
        var paths = PitchFamily.All.ToDictionary(f => f, f => Heights(f, rules), StringComparer.Ordinal);

        // (a) The curveball arcs: it carries the library's biggest hump term, and it is the only
        //     family whose path climbs at all after it leaves the hand. "Farthest above its own
        //     chord" is deliberately *not* the test — the shipped changeup's extreme hang puts it
        //     2.20 ft above its chord, a hump by another name, and the curveball only just beats
        //     that at 2.24 ft. The report says so rather than leaning on the difference.
        foreach (var other in PitchFamily.All.Where(f => f != PitchFamily.Curveball))
            Assert.True(rules.Pitching.Families.Of(PitchFamily.Curveball).Hump
                > rules.Pitching.Families.Of(other).Hump, $"curveball does not hump more than {other}");
        foreach (var family in PitchFamily.All)
        {
            var climbs = paths[family].Max() > paths[family][0];
            Assert.True(climbs == (family == PitchFamily.Curveball),
                $"{family} {(climbs ? "climbs" : "does not climb")} out of the hand; only the curveball should");
        }

        // (b) … and travels the farthest up-and-down of any family: peak to crossing.
        var travel = paths.ToDictionary(p => p.Key, p => p.Value.Max() - p.Value[^1], StringComparer.Ordinal);
        foreach (var other in PitchFamily.All.Where(f => f != PitchFamily.Curveball))
            Assert.True(travel[PitchFamily.Curveball] > travel[other],
                $"curveball travel {travel[PitchFamily.Curveball]} is not above {other}'s {travel[other]}");

        // (c) The late drop — the feet lost over the last two fifths — orders the three the way
        //     their roles do: the arc drops most, the flat one least.
        double LateDrop(string family) => paths[family][UGrid.Length * 3 / 5] - paths[family][^1];
        Assert.True(LateDrop(PitchFamily.Curveball) > LateDrop(PitchFamily.Sinker));
        Assert.True(LateDrop(PitchFamily.Sinker) > LateDrop(PitchFamily.Slider));

        // (d) The slider is the flattest of the three: the smallest excursion from its own chord in
        //     either direction.
        double Excursion(string family) => paths[family].Select((h, i) => Math.Abs(h - Chord(paths[family], i))).Max();
        Assert.True(Excursion(PitchFamily.Slider) < Excursion(PitchFamily.Sinker));
        Assert.True(Excursion(PitchFamily.Slider) < Excursion(PitchFamily.Curveball));

        // (e) The sinker rides the fastball's line longest and then dips: through the first three
        //     quarters of the flight it is the closest of the three to the fastball's height, and
        //     over the last quarter it leaves that line by more than either of the others does.
        //     Measured as two numbers rather than "when does it separate", because the flattest
        //     pitch is also a near neighbour of the fastball and would win that race by never
        //     doing anything — which is the slider's role, not the sinker's.
        var beforeTheDip = UGrid.Length * 3 / 4;
        double Rides(string family) => Enumerable.Range(0, beforeTheDip + 1)
            .Max(i => Math.Abs(paths[family][i] - fastball[i]));
        double Dips(string family) => Math.Abs(paths[family][^1] - fastball[^1]) - Rides(family);

        foreach (var other in new[] { PitchFamily.Slider, PitchFamily.Curveball })
        {
            Assert.True(Rides(PitchFamily.Sinker) < Rides(other),
                $"sinker strays {Rides(PitchFamily.Sinker)} ft from the fastball early, {other} only {Rides(other)}");
            Assert.True(Dips(PitchFamily.Sinker) > Dips(other),
                $"sinker dips {Dips(PitchFamily.Sinker)} ft late, {other} dips {Dips(other)}");
        }

        // (f) And the finding this child reports rather than fixes: no ordinary family crosses above
        //     the fastball. Every drop is downward, so the library has no rising pitch.
        foreach (var family in PitchFamily.All)
            Assert.True(paths[family][^1] <= fastball[^1] + 1e-12, $"{family} crosses above the fastball");
    }

    // ---------------------------------------------------------------------------------
    // S-110  Sweep: the order, the sides, and the exact mirror by throwing hand
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S110_SweepOrdersTheThreeAndMirrorsExactlyWithTheArmForAllTwentyFivePitchers()
    {
        var rules = Trial;
        var families = rules.Pitching.Families;

        var slider = families.Of(PitchFamily.Slider).SweepFt;
        var curveball = families.Of(PitchFamily.Curveball).SweepFt;
        var sinker = families.Of(PitchFamily.Sinker).SweepFt;

        // Sides and order: the slider sweeps farthest and to the glove side, the curveball less and
        // the same way, the sinker the other way (the arm side) and least of all.
        Assert.True(slider > curveball, $"slider {slider} does not sweep farther than curveball {curveball}");
        Assert.True(curveball > 0, $"curveball {curveball} is not toward the glove side");
        Assert.True(sinker < 0, $"sinker {sinker} is not toward the arm side");
        Assert.True(Math.Abs(sinker) < curveball, $"sinker |{sinker}| is not the smallest sweep");
        Assert.Equal(0.0, families.Of(PitchFamily.Fastball).SweepFt);
        Assert.Equal(0.0, families.Of(PitchFamily.Changeup).SweepFt);

        // The glove side is +X for a right-hander, because first base is +X on this diamond and a
        // right-hander faces the plate with first base on the glove hand's side (spec §4.2).
        Assert.True(Diamond.First.X > 0);
        Assert.Equal(1.0, PitchFlight.GloveSideSign(Hand.R));
        Assert.Equal(-1.0, PitchFlight.GloveSideSign(Hand.L));

        // The mirror is exact, term by term, everywhere in the flight: the sign is ±1 and negating a
        // double is exact, so this is bit equality and not a tolerance.
        foreach (var family in PitchFamily.All)
        {
            var row = families.Of(family);
            foreach (var u in UGrid)
                Assert.Equal(PitchFlight.SweepShiftFt(u, row, Hand.R), -PitchFlight.SweepShiftFt(u, row, Hand.L));
        }

        // And at the plate, from the middle of the rubber with no aim, the whole crossing mirrors
        // exactly: the unswept crossing there is +0.0, so the crossing *is* the sweep.
        foreach (var family in PitchFamily.All)
        {
            var right = PitchFlight.Crossing(new PitchCommand(family, 0, false, Throws: Hand.R), rules: rules).X;
            var left = PitchFlight.Crossing(new PitchCommand(family, 0, false, Throws: Hand.L), rules: rules).X;
            Assert.Equal(right, -left);
        }

        // Off the middle of the rubber the two hands are still mirror images about the line the same
        // delivery flies without a sweep, but not bit for bit: the flight rounds `base + sweep` once
        // per hand, so the difference carries those two roundings. The claim is the arithmetic one —
        // the whole of the hand's effect on the flight is twice the sweep term — to a hair.
        foreach (var family in PitchFamily.All)
            foreach (var u in new[] { 0.0, 0.3, 0.7, 0.9, 1.0 })
            {
                var row = families.Of(family);
                var right = PitchFlight.Point(family, u, 0.4, -0.3, 0.5, 0.6, rules: rules, throws: Hand.R).X;
                var left = PitchFlight.Point(family, u, 0.4, -0.3, 0.5, 0.6, rules: rules, throws: Hand.L).X;
                Assert.Equal(2 * PitchFlight.SweepShiftFt(u, row, Hand.R), right - left, 12);
            }

        // The arm rides on a record a saved trace carries (LivePlayCommand.Pitch), so a stream
        // written before this field existed still has to read. It comes back as a right-hander,
        // which is the only arm any flight before #818 was ever given.
        var oldStream = PlayTrace.Parse(
            """{"ticks":[],"commands":[{"i":0,"t":0,"input":{"kind":"Begin","pitch":{"type":"fastball","charge01":0,"star":false}}}]}""");
        Assert.Equal(Hand.R, oldStream.Commands![0].Input.Pitch!.Throws);

        // All 25 shipped pitchers: the sweep a delivery ends with is the one this arm gives it.
        var roster = _shippedContent.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal).ToList();
        Assert.Equal(25, roster.Count);
        Assert.Contains(roster, c => c.Throws == Hand.L);
        foreach (var who in roster)
            foreach (var family in Proposed)
            {
                var crossing = PitchFlight.Crossing(new PitchCommand(family, 0, false, Throws: who.Throws), rules: rules).X;
                var expected = families.Of(family).SweepFt * PitchFlight.GloveSideSign(who.Throws);
                Assert.Equal(expected, crossing);
            }
    }

    // ---------------------------------------------------------------------------------
    // S-111  Readable and coverable: the worst legal case still leaves the batter time
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S111_TheWorstLegalSweepIsStillCoverableFromTheMiddleOfTheBox()
    {
        foreach (var family in Proposed)
            foreach (var throws in new[] { Hand.L, Hand.R })
                foreach (var bats in new[] { Hand.L, Hand.R })
                    foreach (var charged in new[] { false, true })
                    {
                        var margin = CoverMargin(family, throws, bats, charged, Trial);
                        Assert.True(margin > 0,
                            $"{family}: a {bats} batter cannot cover a {throws} arm's worst legal "
                            + $"{(charged ? "charged" : "uncharged")} one — margin {margin:F3} ft");
                    }
    }

    /// <summary>
    /// Feet of reach left over in the worst case PH-04 has to survive, and the one number S-111
    /// asserts the sign of. The evidence tool derives the same margin independently
    /// (<c>tools/pitch-family-probes</c>); this is the falsifier.
    ///
    /// <list type="bullet">
    /// <item><b>The pitch</b>: the family's full sweep plus the player's full stick the same way,
    /// at the highest Pitch stat with a Nice! release — the fastest this family can legally arrive,
    /// with the most lateral movement it can legally carry. A charge damps the stick and shortens
    /// the flight, so both the charged and the uncharged worst case are measured.</item>
    /// <item><b>The read</b>: the batter starts centred and does nothing until the family's own
    /// sweep has taken the ball <see cref="BallRadiusFt"/> off the line it was flying. Only the
    /// sweep triggers the walk: the stick's shift shows far earlier (its mid-flight bend alone
    /// passes that threshold around u = 0.35), so counting it would only lengthen the window.</item>
    /// <item><b>The cover</b>: from there the batter walks the box at <see cref="BoxWalkPerSec"/>
    /// for whatever is left of the flight, and the oval reaches its nice half-width at the crossing's
    /// own height — the ellipse, not the rectangle, and the near side for that batter's hand.</item>
    /// </list>
    /// </summary>
    internal static double CoverMargin(string family, Hand throws, Hand bats, bool charged, RulesTable rules)
    {
        var row = rules.Pitching.Families.Of(family);

        // The stick pushed the same way the sweep goes, which is the sweep's own side for this arm.
        var side = Math.Sign(row.SweepFt * PitchFlight.GloveSideSign(throws));
        var pitch = new PitchCommand(family, charged ? 1 : 0, false, BreakX: side, Nice: true, Throws: throws);
        var air = PitchFlight.AirSeconds(AtBatResolver.PitchSpeedMph(pitch, 10, rules), rules);
        var (crossingX, crossingY) = PitchFlight.Crossing(pitch, rules: rules);

        // Where the sweep alone first shows: t² × sweepFt is the threshold, t is the share of the
        // sweep's own span that has passed, and the span starts at sweepFrom.
        var t = Math.Sqrt(BallRadiusFt / Math.Abs(row.SweepFt));
        var shows = row.SweepFrom + (1 - row.SweepFrom) * t;
        var window = Math.Max(0, 1 - shows) * air;

        // The oval at the crossing's height, not at the cursor's middle: a pitch that crosses low
        // meets a narrower bat.
        var dy = crossingY - StrikeZoneGeometry.CenterY;
        var half = SweetSpot.NiceHalfWidthFt(bats, crossingX, 1, rules)
            * Math.Sqrt(Math.Max(0, 1 - dy * dy / (SweetSpot.HalfHeightFt * SweetSpot.HalfHeightFt)));

        return window * BoxWalkPerSec + half - Math.Abs(crossingX);
    }

    // ---------------------------------------------------------------------------------
    // S-112  A charge damps the player's steering and never the family's own movement
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S112_TheChargeDampsTheStickAndLeavesTheSweepAlone()
    {
        var rules = Trial;
        var flight = rules.Pitching.Flight;

        foreach (var family in Proposed)
            foreach (var throws in new[] { Hand.L, Hand.R })
            {
                var row = rules.Pitching.Families.Of(family);
                Assert.False(row.BreakDamped, $"{family} is steerable like a fastball (PH-15-R6)");

                // No stick: the crossing is the same bits charged and uncharged. The charge changes
                // the speed, and speed is not in X.
                var quiet = PitchFlight.Crossing(new PitchCommand(family, 0, false, Throws: throws), rules: rules).X;
                var loud = PitchFlight.Crossing(new PitchCommand(family, 1, false, Throws: throws), rules: rules).X;
                Assert.Equal(quiet, loud);
                Assert.Equal(row.SweepFt * PitchFlight.GloveSideSign(throws), quiet);

                // Full stick: the steering the charge keeps is breakDampedMul of the steering it
                // spends, and the sweep underneath both is the same number.
                var steered = PitchFlight.Crossing(new PitchCommand(family, 0, false, BreakX: 1, Throws: throws), rules: rules).X;
                var chargedSteered = PitchFlight.Crossing(new PitchCommand(family, 1, false, BreakX: 1, Throws: throws), rules: rules).X;
                Assert.Equal(flight.BreakMaxFt, steered - quiet, 9);
                Assert.Equal(flight.BreakMaxFt * flight.BreakDampedMul, chargedSteered - loud, 9);
                Assert.True(Math.Abs(chargedSteered - loud) < Math.Abs(steered - quiet));

                // And it is untouched for the whole flight, not only at the plate: with no stick,
                // the charged and the uncharged delivery are the same X at every u.
                foreach (var u in UGrid)
                    Assert.Equal(
                        PitchFlight.Point(new PitchCommand(family, 0, false, Throws: throws), u, rules: rules).X,
                        PitchFlight.Point(new PitchCommand(family, 1, false, Throws: throws), u, rules: rules).X);
            }
    }

    // ---------------------------------------------------------------------------------
    // S-113  The shipped root authors all three (#860); a table without the rows still refuses them
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S113_TheShippedRootStopsByNameAndTheTrialAddsNothingElse()
    {
        // #860: Jack played the trials/pitch5 window and accepted it on September 22, 2026 ("trial
        // was good."), so the three rows moved into the shipped pitching.json. This row now holds
        // the promotion instead of the trial: the shipped root is the accepted trial (#883 retired
        // the overlay folder), and the stop by name is the off path — a table with no row, built
        // here (the shipped table with its three optional rows cleared).

        // (a) The off path: three library ids, no rows, a loud stop that names the family and says
        //     where its numbers live. Nothing flies as a fastball.
        var bare = RuleCopies.TwoFamilies();
        foreach (var family in Proposed)
        {
            Assert.False(bare.Pitching.Families.IsAuthored(family));
            var stopped = Assert.Throws<InvalidOperationException>(() => bare.Pitching.Families.Of(family));
            Assert.Contains(family, stopped.Message, StringComparison.Ordinal);
            Assert.Contains("pitching.json", stopped.Message, StringComparison.Ordinal);
            Assert.Throws<InvalidOperationException>(
                () => PitchFlight.Crossing(new PitchCommand(family, 0, false), rules: bare));
        }
        Assert.Equal(new[] { PitchFamily.Fastball, PitchFamily.Changeup }, bare.Pitching.Families.Authored);
        Assert.Equal(new[] { "fastball", "changeup" }, Training.PitchesOf(bare));

        // (b) The shipped root authors all five (the CPU pitcher runs on human inputs; #887 removed
        //     the switch, see CpuPitcherScenarioTests S-120).
        var shipped = ShippedRules.Pitching.Families;
        Assert.Equal(PitchFamily.All, shipped.Authored);

        // (c) The whole library is authored, in library order.
        var trial = Trial.Pitching.Families;
        Assert.Equal(PitchFamily.All, trial.Authored);
        foreach (var family in Proposed) Assert.True(trial.IsAuthored(family));

        // (d) … so the SET selection cycles all three slots for all 25 shipped repertoires, off the
        //     shipped table's own authored list — nothing in the step knows a number (#812).
        var roster = _shippedContent.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal).ToList();
        Assert.Equal(25, roster.Count);
        foreach (var who in roster)
        {
            var rep = who.Repertoire;
            var expected = new[] { PitchFamily.Fastball, rep.Second, rep.Third, PitchFamily.Fastball };
            for (var presses = 0; presses < expected.Length; presses++)
            {
                var state = PitchSelectionState.Reset;
                var button = new ChargeButtonState();
                for (var i = 0; i < presses; i++)
                {
                    var step = ChargeButton.Advance(button, false, false, false, 1.0 / 60, 0.55);
                    state = PitchSelection.Advance(state, true, true, button, step, rep, trial).Next;
                    button = step.Next;
                }
                Assert.Equal(presses % Repertoire.Slots, state.Slot);
                Assert.Equal(expected[presses], PitchSelection.FamilyAt(state, rep, trial));

                // Every slot the cycle can land on is a family this table can fly.
                Assert.True(trial.IsAuthored(PitchSelection.FamilyAt(state, rep, trial)));
            }
        }
    }

    // ---- helpers ---------------------------------------------------------------------

    /// <summary>The height of an unsteered delivery from the middle of the rubber, over the flight.</summary>
    static double[] Heights(string family, RulesTable rules) =>
        UGrid.Select(u => PitchFlight.Point(family, u, rules: rules).Y).ToArray();

    static double Chord(double[] path, int i) =>
        path[0] + (path[^1] - path[0]) * UGrid[i];
}
