using System.Reflection;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The drawn cursor oval and the placement pins (#889, P2-d), Appendix B.1 rows S-134 … S-137.
///
/// <b>S-134</b>: the oval the client draws (<see cref="SweetSpot.Oval"/>) is the oval the resolver
/// judges, across Contact 1–10, quick and charged, both hands, every bat and good-chemistry runners
/// on base (which change nothing since #891, S-144) —
/// asserted through <see cref="AtBatResolver.Resolve"/>, not through the helper alone.
/// <b>S-135</b>: a charge narrows the spatial barrel by <c>cursor.chargeMul</c> and nothing else; it
/// does not touch the timing window (PH-11-R1). <b>S-136</b>: Contact scales the spatial barrel by
/// <c>cursor.scalePerContact</c> and nothing else (PH-15-R7). <b>S-137</b>: holding a load before
/// the commit leaves the box walk's speed unchanged (PH-09-R1).
///
/// Every claim is an arithmetic identity recomputed from the same table, a relationship, or the
/// resolver's own judgement at a point — never a stored double. The oval is multiplies and a
/// clamp; the only libm call is the outline's cos / sin, and those points are compared with a
/// tolerance or through the resolver on the running platform.
/// </summary>
public sealed class CursorOvalScenarioTests
{
    readonly ContentCatalog _content = Shipped.Content;

    RulesTable R => _content.Rules;
    Park Harbor => _content.Parks[ExhibitionPick.DefaultPark];

    /// <summary>Quick (0 and just under the charge line) and charged (on the line, and MAX).</summary>
    static readonly double[] Charges = [0, ChargeFeel.ChargeAt - 0.01, ChargeFeel.ChargeAt, 1];

    static readonly Hand[] Hands = [Hand.R, Hand.L];

    /// <summary>No bat, then every authored bat item.</summary>
    IEnumerable<BatItem?> Bats => new BatItem?[] { null }.Concat(_content.Bats.Values.OrderBy(b => b.Id));

    // ---------------------------------------------------------------------------------
    // S-134  The drawn oval is the judged oval
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S134_TheDrawnOvalIsTheOvalTheResolverJudges()
    {
        var resolver = new AtBatResolver(_content.Chemistry, R, _content.StarSkills);
        var pitcher = _content.Must("vale");
        var cases = 0;
        foreach (var contact in Enumerable.Range(1, 10))
        foreach (var bats in Hands)
        {
            var hitter = Hitter(contact, bats);
            var buddies = Buddies(hitter);
            Assert.NotEmpty(buddies);
            foreach (var runners in new[] { new List<Character>(), buddies })
            foreach (var bat in Bats)
            foreach (var charge in Charges)
            foreach (var box in new[] { 0.0, -0.6 })
            {
                var oval = SweetSpot.Oval(hitter, bat, charge, box, R);
                Assert.Equal(SweetSpot.WorldCenter(box), (oval.CenterX, oval.CenterY));
                Assert.Equal(bats, oval.Bats);

                // Every drawn point sits on the judged boundary.
                var pts = SweetSpot.Outline(oval);
                foreach (var (x, y) in pts)
                    Assert.Equal(1.0, SweetSpot.Distance(box, bats, oval.CenterX + x, oval.CenterY + y, R,
                        oval.BarrelScale), 9);

                // And the resolver agrees at the drawn line: just inside is Nice, just outside is
                // Sour, on the four axes of the drawn line: the tip, the top, the handle, the bottom.
                for (var i = 0; i < pts.Count; i += pts.Count / 4)
                {
                    var (x, y) = pts[i];
                    Assert.Equal(ContactQuality.Nice,
                        resolver.Resolve(Swing(pitcher, hitter, bat, runners, charge, box,
                            oval.CenterX + x * 0.98, oval.CenterY + y * 0.98), Harbor, new Random(1)).Quality);
                    Assert.Equal(ContactQuality.Sour,
                        resolver.Resolve(Swing(pitcher, hitter, bat, runners, charge, box,
                            oval.CenterX + x * 1.02, oval.CenterY + y * 1.02), Harbor, new Random(1)).Quality);
                }
                cases++;
            }
        }
        Assert.Equal(10 * 2 * 2 * (_content.Bats.Count + 1) * Charges.Length * 2, cases);
    }

    [Fact]
    public void S134_TheOvalOutlineIsTheBarrelOutlineTheClientDrewBefore()
    {
        // The client used to draw SweetSpot.Outline(bats, scale); the oval's outline is the same
        // points for the same barrel, so the change of call site moves nothing drawn.
        foreach (var contact in Enumerable.Range(1, 10))
        foreach (var bats in Hands)
        foreach (var charge in Charges)
        {
            var oval = SweetSpot.Oval(Hitter(contact, bats), null, charge, 0, R);
            var before = SweetSpot.Outline(bats, R, oval.BarrelScale, 40);
            Assert.Equal(before, SweetSpot.Outline(oval, 40));
        }
    }

    // ---------------------------------------------------------------------------------
    // S-135  A charge narrows the spatial barrel only (PH-11-R1)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S135_AChargeNarrowsTheBarrelByChargeMulAndNothingElse()
    {
        var c = R.Batting.Cursor;
        foreach (var contact in Enumerable.Range(1, 10))
        foreach (var bats in Hands)
        foreach (var bat in Bats)
        {
            var hitter = Hitter(contact, bats);
            var quick = SweetSpot.Oval(hitter, bat, 0, 0.2, R);
            var charged = SweetSpot.Oval(hitter, bat, 1, 0.2, R);
            var justUnder = SweetSpot.Oval(hitter, bat, ChargeFeel.ChargeAt - 0.01, 0.2, R);
            var slap = SweetSpot.ContactScale(Math.Clamp(contact + (bat?.ContactMod ?? 0), 1, 10), R);

            // Under the charge line a load is still a slap's barrel.
            Assert.Equal(quick, justUnder);
            // Same center, same height: a charge never moves the cursor or its vertical reach.
            Assert.Equal((quick.CenterX, quick.CenterY, quick.HalfHeightFt),
                (charged.CenterX, charged.CenterY, charged.HalfHeightFt));

            if (bat?.ChargeAlwaysFull == true)
            {
                // The Charge Bat keeps the slap's barrel on its free MAX (spec §5.5).
                Assert.Equal(quick, charged);
                continue;
            }
            Assert.Equal(slap, quick.BarrelScale);
            Assert.Equal(slap * c.ChargeMul, charged.BarrelScale);
            Assert.Equal(c.NiceTipFt * (slap * c.ChargeMul), charged.TipHalfFt);
            Assert.Equal(c.NiceHandleFt * (slap * c.ChargeMul), charged.HandleHalfFt);
            Assert.True(charged.TipHalfFt < quick.TipHalfFt);
        }
    }

    [Fact]
    public void S135_AChargeDoesNotTouchTheTimingWindow()
    {
        // The window formula takes no hitter, bat or charge at all (#887: AtBatResolver.ContactWindowFrames),
        // so what is left to hold is the resolver's judgement against it.
        var resolver = new AtBatResolver(_content.Chemistry, R, _content.StarSkills);
        var pitcher = _content.Must("vale");
        var window = AtBatResolver.ContactWindowFrames(null, Harbor, false, R, _content.StarSkills);
        foreach (var contact in Enumerable.Range(1, 10))
        foreach (var bat in Bats)
        {
            var hitter = Hitter(contact, Hand.R);

            // Through the resolver: the same timing error is on the plane or off it for a quick and a
            // charged swing alike, at the rim of the window and just past it.
            var (cx, cy) = SweetSpot.WorldCenter(0);
            foreach (var err in new[] { window / 2, -window / 2, window / 2 + 0.01, -window / 2 - 0.01 })
            {
                var q = resolver.Resolve(Swing(pitcher, hitter, bat, [], 0, 0, cx, cy) with { TimingErrorFrames = err },
                    Harbor, new Random(1)).Quality == ContactQuality.Miss;
                var ch = resolver.Resolve(Swing(pitcher, hitter, bat, [], 1, 0, cx, cy) with { TimingErrorFrames = err },
                    Harbor, new Random(1)).Quality == ContactQuality.Miss;
                Assert.Equal(q, ch);
                Assert.Equal(Math.Abs(err) > window / 2, q);
            }
        }
    }

    // ---------------------------------------------------------------------------------
    // S-136  Contact scales the spatial barrel only (PH-15-R7)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S136_ContactScalesTheBarrelByScalePerContactAndNothingElse()
    {
        var c = R.Batting.Cursor;
        foreach (var bats in Hands)
        foreach (var charge in new[] { 0.0, 1.0 })
        {
            CursorOval? last = null;
            foreach (var contact in Enumerable.Range(1, 10))
            {
                var hitter = Hitter(contact, bats);
                var oval = SweetSpot.Oval(hitter, null, charge, -0.3, R);
                var scale = Math.Max(0.5, 1 + (contact - 5) * c.ScalePerContact) * (charge >= ChargeFeel.ChargeAt ? c.ChargeMul : 1);
                Assert.Equal(scale, oval.BarrelScale);
                Assert.Equal(c.NiceTipFt * scale, oval.TipHalfFt);
                Assert.Equal(c.NiceHandleFt * scale, oval.HandleHalfFt);
                Assert.Equal(SweetSpot.HalfHeightFt, oval.HalfHeightFt);
                Assert.Equal(SweetSpot.WorldCenter(-0.3), (oval.CenterX, oval.CenterY));
                if (last is { } prev)
                    Assert.True(oval.TipHalfFt > prev.TipHalfFt, $"Contact {contact} carries a wider barrel");
                last = oval;
                // No per-hitter timing: ContactWindowFrames takes no hitter since #887.
            }
        }

        // A bat's contactMod is Contact: the oval of Contact c with a +m bat is the oval of c + m, clamped.
        foreach (var bat in _content.Bats.Values.Where(b => b.ContactMod != 0 && !b.ChargeAlwaysFull))
        foreach (var contact in Enumerable.Range(1, 10))
            Assert.Equal(
                SweetSpot.Oval(Hitter(Math.Clamp(contact + bat.ContactMod, 1, 10), Hand.R), null, 0, 0, R),
                SweetSpot.Oval(Hitter(contact, Hand.R), bat, 0, 0, R));
    }

    // ---------------------------------------------------------------------------------
    // S-137  Holding a load leaves the box walk's speed unchanged (PH-09-R1)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S137_HoldingAChargeBeforeTheCommitLeavesTheBoxWalkUnchanged()
    {
        const float dt = 1f / 60f;
        const float stick = 0.5f;
        const int frames = 45; // past MAX (swingChargeSeconds 0.45 s = 27 frames) and into the hold
        var feel = _content.Feel;
        var loading = new Scenario(_content).Match;
        var walking = new Scenario(_content).Match;
        var state = default(ChargeButtonState);
        ChargeButtonStep commit = default;
        var loaded = 0.0;
        for (var f = 0; f < frames; f++)
        {
            var release = f == frames - 1;
            var step = ChargeButton.Advance(state, pressed: f == 0, held: !release, released: release,
                dt, feel.SwingChargeSeconds);
            state = step.Next;
            if (!release)
                loaded = ChargeFeel.Effective01(state.Fill01, state.SecondsPastFull,
                    feel.ChargeMaxHoldSeconds, feel.ChargeOverchargeDecay);
            else
                commit = step;

            // The same frame's walk on both seats: one holds the load, one does not touch the button.
            Assert.True(loading.WalkBatter(HomeSet.BoxWalkStep(stick, dt)));
            Assert.True(walking.WalkBatter(HomeSet.BoxWalkStep(stick, dt)));
            Assert.Equal(walking.BatterOffsetX, loading.BatterOffsetX);
        }
        Assert.True(ChargeFeel.IsCharge(loaded), "the load reached the charge line before the release");
        Assert.True(commit.Committed);

        // Full speed, and nothing lost to the load: the walk is stick × dt × rate every frame.
        var expected = 0.0;
        for (var f = 0; f < frames; f++) expected += (double)(stick * dt * HomeSet.BoxWalkPerSec);
        Assert.Equal(expected, loading.BatterOffsetX);

        // The committed swing carries the walked box, the same box a quick swing on that frame would.
        var charged = SwingInputIntent.Capture(commit, stick, 0, false, loading.BatterOffsetX, rules: Rules.Default);
        var quick = SwingInputIntent.Capture(
            ChargeButton.Advance(default, true, false, true, dt, feel.SwingChargeSeconds),
            stick, 0, false, walking.BatterOffsetX, rules: Rules.Default);
        Assert.True(quick.Committed);
        Assert.Equal(quick.BoxOffsetX, charged.BoxOffsetX);

        // The rail: the walk step takes the stick and the frame and nothing a load could reach.
        var p = typeof(HomeSet).GetMethod(nameof(HomeSet.BoxWalkStep), BindingFlags.Public | BindingFlags.Static)!
            .GetParameters();
        Assert.Equal(["stickX", "dt"], p.Select(x => x.Name!).ToArray());
    }

    // ---------------------------------------------------------------------------------

    /// <summary>A hand-built hitter: pip with Contact authored and a batting hand (a fixture; no data file authors a value).</summary>
    Character Hitter(int contact, Hand bats)
    {
        var who = _content.Must("pip");
        return who with { Bats = bats, Stats = who.Stats with { Contact = contact, Power = 5, Run = 5 } };
    }

    /// <summary>Up to three runners with good chemistry with the hitter (a slap widened for them before #891).</summary>
    List<Character> Buddies(Character hitter) =>
        _content.Characters.Values
            .Where(r => r.Id != hitter.Id && _content.Chemistry.Between(hitter, r) == Chemistry.Good)
            .OrderBy(r => r.Id)
            .Take(3)
            .ToList();

    static AtBatInput Swing(Character pitcher, Character batter, BatItem? bat, IReadOnlyList<Character> runners,
        double charge, double box, double crossingX, double crossingY) => new(
        pitcher, batter, null, runners,
        ChargePitch: false, ChangeupPitch: false, TimingErrorFrames: 0,
        UseStarPitch: false, UseStarSwing: false, Bat: bat, PitcherStamina: 100,
        Charge01: charge, BoxOffsetX: box, CrossingX: crossingX, CrossingY: crossingY);
}
