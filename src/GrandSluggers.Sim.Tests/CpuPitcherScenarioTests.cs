using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Spec Appendix B.1 rows S-114 … S-120 (#823): the CPU pitcher built from the inputs a human has,
/// behind <c>pitching.cpu.humanInputs</c> — off on the shipped root, on in <c>trials/pitch5</c>.
///
/// <b>The switch-off rows assert identity; the switch-on rows assert legality, never a number.</b>
/// The trial weights are proposals Jack judges in sitting 1 (PH-20-R1, PH-18-R1), so a row here
/// fails when the CPU does something a hand could not do — aims a height, throws a family its
/// pitcher does not own, bends further than a held stick reaches — and not when a weight moves.
///
/// The overlay is loaded <b>in process</b> through a <see cref="DataRoot"/> built from the
/// repository, the way <see cref="PitchFamilyTrialScenarioTests"/> does it, so nothing depends on
/// <c>GRAND_SLUGGERS_TRIAL</c> and CI is untouched.
///
/// No expected double crosses a Gaussian: <see cref="Match"/>'s <c>Gauss()</c> is
/// <c>Math.Log</c> / <c>Math.Sqrt</c> / <c>Math.Cos</c>, which differ by an ULP between macOS libm
/// and glibc (#811). Every claim below is a relationship, a count, a range, or an identity computed
/// in this process.
/// </summary>
public sealed class CpuPitcherScenarioTests
{
    const string TrialName = "trials/pitch5";

    readonly ContentCatalog _shipped = ContentCatalog.Load(new DataRoot(ContentCatalog.Load().Root.Shipped));

    string ShippedRoot => _shipped.Root.Shipped;
    string Repo => Path.GetFullPath(Path.Combine(ShippedRoot, ".."));
    DataRoot TrialRoot => new(ShippedRoot, Path.Combine(Repo, "trials", "pitch5"));

    ContentCatalog Trial => ContentCatalog.Load(TrialRoot);

    static double CenterY => StrikeZoneGeometry.CenterY;

    // ---------------------------------------------------------------------------------
    // S-114  The switch off is the shipped CPU: the endpoint model, and only it
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The identity proof that the shipped root did not move is the <b>seed-7 log</b>, captured
    /// before and after the edit and diffed byte for byte (the PR body carries it): one command
    /// stream cannot be compared against another revision's inside one process.
    ///
    /// <para>
    /// What this row can prove in process is that the code path which ran is the shipped one, and
    /// that it is a different function from the trial's. The shipped model's signature is written
    /// out: an endpoint with a <i>height</i> nobody can input, and four <i>exclusive</i> verbs — a
    /// charged pitch never also steers, a steered pitch is always the whole ±1 an arm cannot reach
    /// instantly, and the changeup is a verb rather than a family beside the others.
    /// </para>
    /// </summary>
    [Fact]
    public void S114_TheSwitchOffRunsTheShippedEndpointModelAndSpendsItsDraws()
    {
        Assert.False(_shipped.Rules.Pitching.Cpu.HumanInputs);
        var c = _shipped.Rules.Pitching.Cpu;

        var pitches = new List<PitchCommand>();
        for (var seed = 1; seed <= 20; seed++)
        {
            var match = new Scenario(_shipped, seed).Match;
            for (var i = 0; i < 30; i++) pitches.Add(match.CpuPitch());
        }
        Assert.Equal(600, pitches.Count);

        foreach (var p in pitches)
        {
            // (a) The endpoint: the shipped CPU sets a crossing height. That is the asymmetry #823
            //     exists to remove, and while the switch is off it is still here.
            Assert.NotEqual(0, p.AimY);
            Assert.True(p.AimX != 0 || p.AimY != 0, "the shipped CPU aims an endpoint");

            // (b) Exclusive verbs: the whole stick or none of it, and never beside a charge.
            Assert.Contains(Math.Abs(p.BreakX), new[] { 0.0, 1.0 });
            Assert.Contains(p.Type, new[] { PitchFamily.Fastball, PitchFamily.Changeup });
            if (p.Charge01 == 1.0) Assert.Equal(0, p.BreakX);
            if (p.BreakX != 0) Assert.NotEqual(1.0, p.Charge01);
            if (p.Type == PitchFamily.Changeup)
            {
                Assert.Equal(0, p.BreakX);
                Assert.NotEqual(1.0, p.Charge01);
            }
            if (p.Charge01 != 1.0)
                Assert.InRange(p.Charge01, c.TapMin, c.TapMin + c.TapSpan);
            if (p.Nice) Assert.Equal(1.0, p.Charge01);
        }
        Assert.Contains(pitches, p => p.Charge01 == 1.0);
        Assert.Contains(pitches, p => p.BreakX != 0);
        Assert.Contains(pitches, p => p.Type == PitchFamily.Changeup);
        // Nothing charged also steered: the four verbs are exclusive, which is exactly what the
        // trial stops doing (S-118).
        Assert.DoesNotContain(pitches, p => p.Charge01 == 1.0 && p.BreakX != 0);

        // (c) Same seed, same stream: the draws this path spends are a function of the seed alone.
        var first = new Scenario(_shipped, 7).Match;
        var again = new Scenario(_shipped, 7).Match;
        for (var i = 0; i < 50; i++) Assert.Equal(first.CpuPitch(), again.CpuPitch());

        // (d) The two paths are different functions, and the trial's never aims.
        var trial = Trial;
        Assert.True(trial.Rules.Pitching.Cpu.HumanInputs);
        var legal = new Scenario(trial, 7).Match;
        for (var i = 0; i < 50; i++)
        {
            var p = legal.CpuPitch();
            Assert.Equal(0, p.AimX);
            Assert.Equal(0, p.AimY);
        }
    }

    // ---------------------------------------------------------------------------------
    // S-115  Trial: no aim, a reachable crossing, and the arm stamped before the solve
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S115_TrialCpuNeverAimsAndWalksTheRubberOntoItsIntentWithBothArms()
    {
        var trial = Trial;
        var rules = trial.Rules;
        var hands = new Dictionary<Hand, int>();
        var clamped = 0;
        var stars = 0;
        var total = 0;

        foreach (var who in Roster(trial))
        foreach (var seed in new[] { 3, 11 })
        {
            var match = MatchPitchedBy(trial, who, seed);
            Assert.Equal(who.Id, match.Pitcher.Id);
            for (var i = 0; i < 25; i++)
            {
                var pitch = match.CpuPitchByInputs(out var plan);
                total++;
                hands[who.Throws] = hands.GetValueOrDefault(who.Throws) + 1;

                // (a) No input a hand does not have.
                Assert.Equal(0, pitch.AimX);
                Assert.Equal(0, pitch.AimY);
                Assert.Equal(who.Throws, pitch.Throws);
                Assert.Equal(plan.RubberX, pitch.RubberX);
                Assert.Equal(plan.RubberX, match.PitcherOffsetX);
                Assert.InRange(pitch.RubberX, -1, 1);

                // (b) The crossing is where the rubber put it. The solve is affine and exact, so a
                //     walk that did not run into the legal range lands on the intent; one that did
                //     misses short, on the near side, the way an arm out of rubber does.
                var (x, y) = PitchFlight.Crossing(pitch, match.Pitcher.StarPitch, rules);
                if (Math.Abs(pitch.RubberX) < 1)
                    Assert.Equal(plan.IntentX, x, 9);
                else
                {
                    clamped++;
                    Assert.True(Math.Abs(x) <= Math.Abs(plan.IntentX) + 1e-9,
                        $"a clamped walk overshoots: crossing {x} vs intent {plan.IntentX}");
                }

                // (c) Height is the family's and nothing else touches it (PH-03). A Star Pitch is the
                //     one exception and it is not a new CPU input: §13's shapes move the ball on
                //     their own, for a human's star as much as for this one, and the solve above
                //     lands on the intent anyway because the same wobble is in X₀.
                var row = rules.Pitching.Families.Of(pitch.Type);
                if (pitch.Star) stars++;
                else Assert.Equal(PitchFlight.PlateY - row.DropFt, y, 9);
            }
        }

        Assert.True(total >= 1000, $"{total} pitches");
        Assert.True(stars > 0, "star pitches were thrown, and their crossing still lands on the intent");
        // Both arms were driven, and every one of them landed on its own intent: the sweep is
        // mirrored by Throws, and Throws is stamped before the solve rather than after it.
        Assert.True(hands.GetValueOrDefault(Hand.L) > 0 && hands.GetValueOrDefault(Hand.R) > 0,
            $"both arms pitched: {string.Join(", ", hands.Select(h => $"{h.Key} {h.Value}"))}");
        Assert.True(clamped < total / 10, $"{clamped} of {total} walks ran out of rubber");
    }

    /// <summary>
    /// The solve is not a search: the same delivery from a wrong arm would miss its intent by twice
    /// the family's sweep. This is the row that would have failed if <c>Throws</c> were still
    /// stamped after the solve, as it was before #823 (P1-d's finding).
    /// </summary>
    [Fact]
    public void S115_AWrongArmWouldMissTheIntentByTwiceTheFamilysSweep()
    {
        var rules = Trial.Rules;
        foreach (var family in new[] { PitchFamily.Slider, PitchFamily.Curveball, PitchFamily.Sinker })
        {
            var row = rules.Pitching.Families.Of(family);
            Assert.NotEqual(0, row.SweepFt);
            foreach (var throws in new[] { Hand.L, Hand.R })
            {
                var intent = StrikeZoneGeometry.HalfWidth;
                var probe = new PitchCommand(family, 0, false, Throws: throws);
                var (zero, _) = PitchFlight.Crossing(probe, rules: rules);
                var solved = probe with { RubberX = (intent - zero) / HomeSet.PitcherWalk };
                Assert.Equal(intent, PitchFlight.Crossing(solved, rules: rules).X, 9);

                var wrongArm = solved with { Throws = throws == Hand.L ? Hand.R : Hand.L };
                var missed = PitchFlight.Crossing(wrongArm, rules: rules).X;
                Assert.Equal(2 * Math.Abs(row.SweepFt), Math.Abs(missed - intent), 9);
            }
        }
    }

    // ---------------------------------------------------------------------------------
    // S-116  Trial: the family is always one this pitcher can press to
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S116_TheTrialCpuOnlyEverThrowsAFamilyItsPresssesReach()
    {
        var trial = Trial;
        var authored = trial.Rules.Pitching.Families.Authored;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var pitchers = 0;

        foreach (var who in Roster(trial))
        {
            pitchers++;
            var match = MatchPitchedBy(trial, who, seed: 5);
            for (var i = 0; i < 60; i++)
            {
                var pitch = match.CpuPitchByInputs(out var plan);
                Assert.Equal(plan.Family, pitch.Type);
                seen.Add(pitch.Type);

                // In the repertoire, and authored: the two halves of IsSelectable.
                Assert.True(who.Repertoire.Has(pitch.Type), $"{who.Id} does not throw {pitch.Type}");
                Assert.True(trial.Rules.Pitching.Families.IsAuthored(pitch.Type), $"{pitch.Type} is unauthored");

                // The choice is the presses it is: replay them the way a player does and land on it.
                Assert.InRange(plan.Presses, 0, Repertoire.Slots - 1);
                Assert.Equal(plan.Family, match.CpuFamilyAfter(plan.Presses));
                Assert.Equal(plan.Family, Replay(plan.Presses, who.Repertoire, authored));
            }
        }

        Assert.Equal(25, pitchers);
        // Every family in the library came up somewhere across the roster, so the row weights are
        // not quietly collapsing onto the fastball.
        Assert.Equal(PitchFamily.All.OrderBy(f => f, StringComparer.Ordinal),
            seen.OrderBy(f => f, StringComparer.Ordinal));
    }

    /// <summary>
    /// The shipped table authors two families, so every repertoire's second and third pitch is
    /// skipped for most of the roster: the CPU under the switch must land on the fastball or the
    /// changeup and never reach for a row that does not exist.
    /// </summary>
    [Fact]
    public void S116_UnderAShippedFamilyTableThePressesOnlyReachWhatCanFly()
    {
        var content = ContentCatalog.Load(WriteRoot(json =>
        {
            json["cpu"]!["humanInputs"] = true;
            foreach (var row in Rows)
            {
                var families = json["cpu"]![row]!["families"]!.AsObject();
                foreach (var f in PitchFamily.All) families[f] = 20;
            }
        }));

        foreach (var who in Roster(content))
        {
            var match = MatchPitchedBy(content, who, seed: 9);
            for (var i = 0; i < 40; i++)
            {
                var pitch = match.CpuPitchByInputs(out var plan);
                Assert.Contains(pitch.Type, new[] { PitchFamily.Fastball, PitchFamily.Changeup });
                Assert.True(who.Repertoire.Has(pitch.Type));
                Assert.Equal(plan.Family, match.CpuFamilyAfter(plan.Presses));
            }
        }
    }

    // ---------------------------------------------------------------------------------
    // S-117  Trial: the bend is what a held stick reaches, never an instant ±1
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S117_TheTrialCpuNeverBendsFurtherThanAHeldStickReaches()
    {
        var trial = Trial;
        var rules = trial.Rules;
        var steered = 0;
        var saturated = 0;

        foreach (var who in Roster(trial))
        {
            var match = MatchPitchedBy(trial, who, seed: 13);
            for (var i = 0; i < 40; i++)
            {
                var pitch = match.CpuPitchByInputs(out var plan);
                var airSec = PitchFlight.AirSeconds(match.PitchSpeedMph(pitch), rules);
                var reach = PitchFlight.BreakReach(who.Stats.Pitch, airSec, rules);

                Assert.Equal(reach, plan.SteerReach, 12);
                Assert.True(Math.Abs(pitch.BreakX) <= reach + 1e-12,
                    $"{who.Id} bent {pitch.BreakX} past a reach of {reach}");
                Assert.Equal(plan.SteerDir * reach, pitch.BreakX, 12);
                Assert.InRange(plan.SteerDir, -1, 1);
                if (pitch.BreakX != 0)
                {
                    steered++;
                    if (reach >= 1) saturated++;
                }
            }
        }

        Assert.True(steered > 0, "some pitch steered");

        // **Measured, and stated as measured, not asserted as a target.** At the shipped
        // `breakRatePerSec` the slowest arm there is bends the stick to the cap in 0.80 s, and the
        // shortest flight any *real* pitch has is longer than that — so on this table every steered
        // CPU pitch does reach the whole ±1, and it reaches it the way a hand does rather than
        // because the model handed it over. The rule is what changed, not this number.
        Assert.Equal(steered, saturated);

        // The bound is not vacuous: it bites at the shortest flight the air clamps allow, which is
        // the first place a faster pitch or a slower rate would take a delivery.
        var flight = rules.Pitching.Flight;
        Assert.True(PitchFlight.BreakReach(1, flight.AirMinSec, rules) < 1,
            "the slowest arm cannot bend the whole stick inside the shortest flight the clamps allow");
        // And the margin at the tightest *real* case — the slowest arm's own hardest pitch — is the
        // number that would have to close for the bound to start showing on the field.
        var hardest = new PitchCommand(PitchFamily.Fastball, 1, false, Nice: true);
        var shortestReal = PitchFlight.AirSeconds(AtBatResolver.PitchSpeedMph(hardest, 1, rules), rules);
        Assert.True(shortestReal > flight.AirMinSec, "no real pitch leans on the air-time floor (S-107)");
        Assert.True(PitchFlight.BreakReach(1, shortestReal, rules) >= 1);

        // And the reach is monotone in both of its inputs, which is what makes it readable.
        Assert.True(PitchFlight.BreakReach(10, 0.5, rules) > PitchFlight.BreakReach(1, 0.5, rules));
        Assert.True(PitchFlight.BreakReach(5, 0.9, rules) > PitchFlight.BreakReach(5, 0.4, rules));
        Assert.Equal(0, PitchFlight.BreakReach(10, 0, rules));
    }

    /// <summary>
    /// One number for the hand and for the CPU: holding the stick one way from release, a frame at a
    /// time through <see cref="PitchFlight.BreakStep"/>, arrives at <see cref="PitchFlight.BreakReach"/>.
    /// </summary>
    [Fact]
    public void S117_HoldingTheStickAFrameAtATimeArrivesAtTheSameReach()
    {
        var rules = Trial.Rules;
        const double dt = 1.0 / 60;
        foreach (var stat in new[] { 1, 3, 5, 8, 10 })
        foreach (var airSec in new[] { 0.78, 0.95, 1.28 })
        {
            var held = 0.0;
            var frames = (int)Math.Round(airSec / dt);
            for (var f = 0; f < frames; f++) held = PitchFlight.BreakStep(held, 1, dt, stat, rules);
            Assert.Equal(PitchFlight.BreakReach(stat, frames * dt, rules), held, 9);
        }
    }

    // ---------------------------------------------------------------------------------
    // S-118  Trial: charge and steer are independent modifiers, not exclusive verbs
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S118_ChargeAndSteerCoOccurAtTheRateTheRowsSay()
    {
        var trial = Trial;
        var cpu = trial.Rules.Pitching.Cpu;

        foreach (var (name, row, match) in new[]
        {
            ("even", cpu.Even, EvenCount(trial, seed: 21)),
            ("ahead", cpu.Ahead, AheadCount(trial, seed: 22)),
        })
        {
            Assert.Same(row, match.CpuPitchRow());
            const int n = 4000;
            var charged = 0;
            var steered = 0;
            var both = 0;
            for (var i = 0; i < n; i++)
            {
                match.CpuPitchByInputs(out var plan);
                if (plan.Charged) charged++;
                if (plan.SteerDir != 0) steered++;
                if (plan.Charged && plan.SteerDir != 0) both++;
            }

            // Wide tolerances, a fixed seed, and the row's own numbers: this row fails when charge
            // and steer stop being what the table says, not when a weight is retuned.
            Assert.InRange(charged / (double)n, row.ChargeChance - 0.04, row.ChargeChance + 0.04);
            Assert.InRange(steered / (double)n, row.SteerChance - 0.04, row.SteerChance + 0.04);
            // Independent, so the joint rate is the product — and above all it is not zero, which is
            // the whole difference from the shipped model's four exclusive verbs (S-114).
            Assert.True(both > 0, $"{name}: a charged pitch is still steerable");
            Assert.InRange(both / (double)n,
                row.ChargeChance * row.SteerChance - 0.03, row.ChargeChance * row.SteerChance + 0.03);
        }
    }

    // ---------------------------------------------------------------------------------
    // S-119  Trial twin of S-27: the waste survives the loss of vertical aim
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S119_AtZeroTwoTheTrialCpuStillWastesOutsideTheZoneByRubberAlone()
    {
        var trial = Trial;
        var match = AheadCount(trial, seed: 27);
        Assert.Same(trial.Rules.Pitching.Cpu.Ahead, match.CpuPitchRow());

        var outside = 0;
        var byX = 0;
        var byY = 0;
        for (var i = 0; i < 100; i++)
        {
            var pitch = match.CpuPitchByInputs(out _);
            var (x, y) = PitchFlight.Crossing(pitch, match.Pitcher.StarPitch, trial.Rules);
            if (StrikeZoneGeometry.Contains(x, y)) continue;
            outside++;
            if (Math.Abs(x) > StrikeZoneGeometry.HalfWidth) byX++;
            if (y < StrikeZoneGeometry.Bottom || y > StrikeZoneGeometry.Top) byY++;
        }

        Assert.True(outside >= 30, $"{outside} of 100 outside");
        // The point of the row: with no vertical intent left, the waste has to come from the rubber.
        Assert.True(byX >= 30, $"{byX} of 100 outside by X (rubber); {byY} by Y (the family's own drop)");

        // And the even row still never sits down the middle, the second half of S-27's claim.
        var even = EvenCount(trial, seed: 28);
        Assert.Same(trial.Rules.Pitching.Cpu.Even, even.CpuPitchRow());
        var center = 0;
        for (var i = 0; i < 100; i++)
        {
            var (x, y) = PitchFlight.Crossing(even.CpuPitchByInputs(out _), even.Pitcher.StarPitch, trial.Rules);
            if (Math.Abs(x) < 0.25 && Math.Abs(y - CenterY) < 0.25) center++;
        }
        Assert.True(center < 10, $"{center} of 100 down the middle");
    }

    // ---------------------------------------------------------------------------------
    // S-120  The switch itself, and what the table may and may not say
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S120_TheSwitchIsOffShippedOnInTheTrialAndTheWeightsAreFilteredNotValidatedAgainstTheRoster()
    {
        // (a) The switch, both sides, from the files rather than from the code defaults.
        Assert.False(_shipped.Rules.Pitching.Cpu.HumanInputs);
        Assert.True(Trial.Rules.Pitching.Cpu.HumanInputs);
        Assert.Equal(new[] { "rules/pitching.json" }, TrialRoot.Overrides);
        Assert.Equal(TrialName, TrialRoot.OverlayName);

        // (b) The shipped rows carry the port of today's exclusive mix, so the switch has a stated
        //     starting point rather than a new invention: the fastball takes every verb that was not
        //     the changeup, and the charge and break shares become the two chances.
        var cpu = _shipped.Rules.Pitching.Cpu;
        foreach (var row in new[] { cpu.Even, cpu.Ahead, cpu.Behind, cpu.RunnerTwoOuts })
        {
            var mix = row.Normal + row.Charge + row.Changeup + row.Break;
            Assert.Equal(row.Normal + row.Charge + row.Break, row.Families.Fastball);
            Assert.Equal(row.Changeup, row.Families.Changeup);
            Assert.Equal(0, row.Families.Curveball + row.Families.Slider + row.Families.Sinker);
            Assert.Equal(row.Charge / mix, row.ChargeChance, 12);
            Assert.Equal(row.Break / mix, row.SteerChance, 12);
        }

        // (c) A weight for a family the active table cannot fly is legal — weights are filtered at
        //     run time, not validated against a roster — and never reaches the mound.
        var weighsTheUnauthored = WriteRoot(json =>
        {
            json["cpu"]!["humanInputs"] = true;
            json["cpu"]!["even"]!["families"]!["curveball"] = 50;
        });
        Assert.Empty(RulesTable.Validate(weighsTheUnauthored));
        var content = ContentCatalog.Load(weighsTheUnauthored);
        var match = EvenCount(content, seed: 31);
        for (var i = 0; i < 200; i++)
            Assert.Contains(match.CpuPitchByInputs(out _).Type, new[] { PitchFamily.Fastball, PitchFamily.Changeup });

        // (d) A row that weights nothing at all is a broken table and stops by name.
        var weighsNothing = WriteRoot(json =>
        {
            foreach (var f in PitchFamily.All) json["cpu"]!["even"]!["families"]![f] = 0;
        });
        Assert.Contains(RulesTable.Validate(weighsNothing),
            e => e.Contains("pitching.cpu.even.families must weight at least one family", StringComparison.Ordinal));

        // (e) A row that weights only families this pitcher cannot select is not a broken table — it
        //     is a row that does not apply here — and it falls back to the fastball, the one family
        //     every pitcher throws and every SET starts on (PH-15-R1, PH-02-R5). Stated, not implied.
        var weighsOnlyTheUnavailable = WriteRoot(json =>
        {
            json["cpu"]!["humanInputs"] = true;
            var families = json["cpu"]!["even"]!["families"]!.AsObject();
            foreach (var f in PitchFamily.All) families[f] = 0;
            families[PitchFamily.Curveball] = 10;
        });
        Assert.Empty(RulesTable.Validate(weighsOnlyTheUnavailable));
        var fallback = EvenCount(ContentCatalog.Load(weighsOnlyTheUnavailable), seed: 32);
        for (var i = 0; i < 50; i++)
        {
            var pitch = fallback.CpuPitchByInputs(out var plan);
            Assert.Equal(PitchFamily.Fastball, pitch.Type);
            Assert.Equal(0, plan.Presses);
        }
    }

    // ---------------------------------------------------------------------------------
    // helpers
    // ---------------------------------------------------------------------------------

    static readonly string[] Rows = ["even", "ahead", "behind", "runnerTwoOuts"];

    static IReadOnlyList<Character> Roster(ContentCatalog content) =>
        content.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal).ToList();

    /// <summary>A match whose pitcher on the mound is <paramref name="who"/>, so all 25 arms can be driven.</summary>
    static Match MatchPitchedBy(ContentCatalog content, Character who, int seed)
    {
        var (home, away) = PresetTeams.Pair(content, "rio", "ashlord");
        var roster = home.Roster.ToList();
        var at = roster.FindIndex(c => c.Id.Equals(who.Id, StringComparison.OrdinalIgnoreCase));
        if (at < 0)
        {
            // One body traded for the arm we want to watch; never the captain, whose chemistry and
            // stars the team is built from.
            var slot = roster.FindIndex(c => !c.Id.Equals(home.Captain.Id, StringComparison.OrdinalIgnoreCase));
            roster[slot] = who;
        }
        var match = Match.Exhibition(content, home with { Roster = roster, Starter = who }, away, seed: seed);
        Assert.True(match.Top, "the home arm is on the mound in the top half");
        return match;
    }

    /// <summary>A 0-0 count: the even row.</summary>
    static Match EvenCount(ContentCatalog content, int seed)
    {
        var match = new Scenario(content, seed).Match;
        Assert.Equal((0, 0), (match.Balls, match.Strikes));
        return match;
    }

    /// <summary>Two called strikes: the ahead row, the way S-27 reaches it.</summary>
    static Match AheadCount(ContentCatalog content, int seed)
    {
        var match = new Scenario(content, seed).Match;
        match.Play(Scenario.PitchAt(0, CenterY), Scenario.Take);
        match.Play(Scenario.PitchAt(0, CenterY), Scenario.Take);
        Assert.Equal((0, 2), (match.Balls, match.Strikes));
        return match;
    }

    /// <summary>The family those presses land on, replayed through the player's own step.</summary>
    static string Replay(int presses, Repertoire repertoire, IReadOnlyList<string> authored)
    {
        var state = PitchSelectionState.Reset;
        var button = new ChargeButtonState();
        for (var i = 0; i < presses; i++)
        {
            var step = ChargeButton.Advance(button, false, false, false, 1.0 / 60, 0.55);
            state = PitchSelection.Advance(state, true, true, button, step, repertoire, authored).Next;
            button = step.Next;
        }
        return PitchSelection.FamilyAt(state, repertoire, authored);
    }

    /// <summary>A throwaway data root: the shipped one, with <c>pitching.json</c> edited in place.</summary>
    DataRoot WriteRoot(Action<JsonObject> edit)
    {
        var root = Path.Combine(Path.GetTempPath(), "grand-sluggers-cpu-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        foreach (var dir in Directory.GetDirectories(ShippedRoot, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(root, Path.GetRelativePath(ShippedRoot, dir)));
        foreach (var file in Directory.GetFiles(ShippedRoot, "*", SearchOption.AllDirectories))
            File.Copy(file, Path.Combine(root, Path.GetRelativePath(ShippedRoot, file)));

        var path = Path.Combine(root, RulesTable.Directory, "pitching.json");
        var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        edit(json);
        File.WriteAllText(path, json.ToJsonString());
        return new DataRoot(root);
    }
}
