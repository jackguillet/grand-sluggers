using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Body classes, size in play, the speed gap and the weight ramp (CH-05, CH-10, CH-11), scenarios SC-09 … SC-14.
///
/// <b>SC-09</b>: every captain and role player resolves exactly one body class. <b>SC-10</b>: reach comes from the class row,
/// not from height. <b>SC-11</b>: the fastest and the slowest body on the roster are inside the top-speed band, on the bases and
/// in the field (a Balance row until Jack's balance pass sets the curves). <b>SC-12</b>: a light body reaches its top speed sooner; a heavy one takes longer to brake and reverse, in the
/// step and in the planner alike. <b>SC-13</b>: one movement profile per body across a grounder, a liner and a fly. <b>SC-14</b>:
/// a light body recoils longer than a heavy one from the same ball.
///
/// The values are balance: every row asserts a relationship, never a shipped number, except the band SC-11 names.
/// </summary>
public class BodyClassScenarioTests
{
    const double Frame = 1.0 / 60.0;

    /// <summary>The proposed top-speed band (CH-10): fastest ÷ slowest on the roster.</summary>
    const double BandLow = 1.20, BandHigh = 1.30;

    readonly ContentCatalog _content = Shipped.Content;

    RulesTable R => _content.Rules;

    // ---------------------------------------------------------------------------------
    // SC-09 — every character resolves exactly one body class
    // ---------------------------------------------------------------------------------

    [Fact]
    public void SC09_EveryCaptainAndRolePlayerResolvesExactlyOneBodyClass()
    {
        var captains = _content.Characters.Values.Where(c => c.Captain).ToDictionary(c => c.Faction, StringComparer.OrdinalIgnoreCase);
        foreach (var c in _content.Characters.Values)
        {
            Assert.False(string.IsNullOrEmpty(c.BodyClass), $"{c.Id} plays no body class");
            Assert.Single(R.BodyClasses.Classes, k => k.Id.Equals(c.BodyClass, StringComparison.OrdinalIgnoreCase));
            Assert.Same(R.BodyClasses.Of(c.BodyClass), BodyClasses.Of(c, R));
            // A role player that names no class wears its captain's.
            if (!c.Captain) Assert.Equal(captains[c.Faction].BodyClass, c.BodyClass);
        }
        // Seven ship, one per captain cut, each with its own motion style; the table has room for about fifteen.
        Assert.Equal(captains.Count, captains.Values.Select(c => c.BodyClass).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(R.BodyClasses.Classes.Count, R.BodyClasses.Classes.Select(k => k.MotionStyle).Distinct().Count());
        Assert.InRange(BodyClassLibrary.Capacity, 15, 15);
    }

    [Fact]
    public void SC09_ARolePlayerMayNameItsOwnClass()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeArray("characters/role-players.json", rows =>
        {
            Assert.Equal("nico", rows[0]!["id"]!.GetValue<string>());
            rows[0]!["bodyClass"] = "brick";
        });
        Assert.Empty(ContentDataValidator.Validate(fixture.Root));
        var content = ContentCatalog.Load(new DataRoot(fixture.Root));
        var nico = content.Must("nico");
        Assert.Equal("brick", nico.BodyClass);
        // Its captain keeps its own.
        Assert.Equal("harbor-kid", content.Characters.Values.Single(c => c.Captain && c.Faction == nico.Faction).BodyClass);
    }

    [Fact]
    public void SC09_ACaptainWithNoClassIsRefused()
    {
        using var fixture = new ContentFixture();
        Assert.Empty(ContentDataValidator.Validate(fixture.Root));
        fixture.ChangeObject("characters/rio.json", json => json.Remove("bodyClass"));
        var error = Assert.Single(ContentDataValidator.Validate(fixture.Root));
        Assert.StartsWith($"{fixture.Path("characters/rio.json")}: captain 'rio' bodyClass is required", error, StringComparison.Ordinal);
    }

    [Fact]
    public void SC09_AClassWithNoRowIsRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/zig.json", json => json["bodyClass"] = "rocket");
        var error = Assert.Single(ContentDataValidator.Validate(fixture.Root));
        Assert.StartsWith($"{fixture.Path("characters/zig.json")}: character 'zig' bodyClass 'rocket' is not a row in data/rules/body-classes.json", error, StringComparison.Ordinal);
    }

    [Fact]
    public void SC09_AnAuthoredReachIsRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/konga.json", json => json["reachFt"] = 7.5);
        var error = Assert.Single(ContentDataValidator.Validate(fixture.Root));
        Assert.Contains("character 'konga' authors reachFt; reach comes from the body class", error);
    }

    [Fact]
    public void SC09_TheTableIsReadStrictly()
    {
        // Each row is read like a nested table: a missing number and an unknown one are refused by name.
        using (var shape = new ContentFixture())
        {
            shape.ChangeObject("rules/body-classes.json", json =>
            {
                var rows = json["classes"]!.AsArray();
                rows[0]!.AsObject().Remove("flyReachFt");
                rows[1]!["wingspanFt"] = 3;
            });
            var bad = RulesTable.Validate(new DataRoot(shape.Root));
            Assert.True(bad.Any(e => e.EndsWith("body-classes.classes[0].flyReachFt is missing; every rule is authored in the table", StringComparison.Ordinal)), string.Join("\n", bad));
            Assert.True(bad.Any(e => e.EndsWith("body-classes.classes[1].wingspanFt is not a rule this table owns", StringComparison.Ordinal)), string.Join("\n", bad));
        }

        // A whole table is held to its cross-row rules: one row per id, the full knockback at most.
        using var fixture = new ContentFixture();
        fixture.ChangeObject("rules/body-classes.json", json =>
        {
            var rows = json["classes"]!.AsArray();
            rows[3]!["id"] = rows[2]!["id"]!.GetValue<string>();
            rows[4]!["knockbackMul"] = 1.5;
            rows[5]!["accelSec"] = 0;
        });
        var errors = RulesTable.Validate(new DataRoot(fixture.Root));
        Assert.True(errors.Any(e => e.Contains("body-classes.classes[5].accelSec must be greater than 0", StringComparison.Ordinal)), string.Join("\n", errors));
        Assert.True(errors.Any(e => e.Contains("body-classes.classes[3].id") && e.Contains("is named twice")), string.Join("\n", errors));
        Assert.True(errors.Any(e => e.Contains("body-classes.classes[4].knockbackMul must be at most 1")), string.Join("\n", errors));
    }

    // ---------------------------------------------------------------------------------
    // SC-10 — reach is the class row's, not the body's height
    // ---------------------------------------------------------------------------------

    [Fact]
    public void SC10_ATallClassMayReachLessThanAShortOne()
    {
        // The shipped table has the Sluggers read: a taller captain whose class reaches less in the air than a shorter one.
        var captains = _content.Characters.Values.Where(c => c.Captain).ToList();
        var pair = (from tall in captains
                    from small in captains
                    where tall.Proportions.Height > small.Proportions.Height
                          && BodyClasses.ReachFt(tall, true, R) < BodyClasses.ReachFt(small, true, R)
                    select (tall, small)).FirstOrDefault();
        Assert.True(pair.tall is not null, "no taller captain reaches less in the air than a shorter one");

        // The resolver reads the class and nothing about the body: doubling the height moves no reach, swapping the class moves it all.
        foreach (var who in captains)
            foreach (var air in new[] { true, false })
            {
                var reach = FieldingResolver.CatchRadiusFt(who, null, R, air);
                var giant = who with { Proportions = who.Proportions with { Height = who.Proportions.Height * 2 } };
                Assert.Equal(reach, FieldingResolver.CatchRadiusFt(giant, null, R, air), 9);
            }
        var (t, s) = pair;
        var swapped = t with { BodyClass = s.BodyClass };
        Assert.Equal(BodyClasses.ReachFt(s, true, R), BodyClasses.ReachFt(swapped, true, R), 9);

        // At a fly between the two reaches, the short body is under it and the tall one is not.
        var tallR = FieldingResolver.CatchRadiusFt(t with { FieldAbility = "" }, null, R, air: true);
        var smallR = FieldingResolver.CatchRadiusFt(s with { FieldAbility = "" }, null, R, air: true);
        var d = (tallR + smallR) / 2;
        Assert.True(FlyCatch.Under(d, 0, 0, 0, 0, 0, smallR, false, R));
        Assert.False(FlyCatch.Under(d, 0, 0, 0, 0, 0, tallR, false, R));
    }

    [Fact]
    public void SC10_GroundAndFlyReachAreSeparateNumbers()
    {
        var row = R.BodyClasses.Classes.First(k => Math.Abs(k.GroundReachFt - k.FlyReachFt) > 0.1);
        var who = _content.Characters.Values.First(c => c.BodyClass == row.Id) with { FieldAbility = "" };
        Assert.Equal(row.GroundReachFt, FieldingResolver.CatchRadiusFt(who, null, R, air: false), 9);
        Assert.Equal(row.FlyReachFt, FieldingResolver.CatchRadiusFt(who, null, R, air: true), 9);
    }

    // ---------------------------------------------------------------------------------
    // SC-11 — the top-speed gap
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The top-speed band (CH-10). Deferred to Jack's balance pass: the shipped curves keep today's 1.54× in the field and 1.34×
    /// on the bases, so this row is the falsifier that pass has to turn green. The known constraint is the buddy jump at the
    /// wall (a Run-9 pair reaching the plant, T-F12).
    /// </summary>
    [Fact]
    [Trait("Kind", "Balance")]
    public void SC11_FastestOverSlowestIsInsideTheBandOnTheBasesAndInTheField()
    {
        var roster = _content.Characters.Values.ToList();
        var field = roster.Select(c => FieldingResolver.ChaseSpeedFt(c, false, R)).ToList();
        var bases = roster.Select(c => RunnerSystem.SpeedFtPerSec(c, R)).ToList();
        Assert.InRange(field.Max() / field.Min(), BandLow, BandHigh);
        Assert.InRange(bases.Max() / bases.Min(), BandLow, BandHigh);
    }

    /// <summary>Top speed is the Run sub-stat's, never the class's: the same order on the bases and in the field.</summary>
    [Fact]
    public void SC11_TheRunSubStatOrdersTopSpeedInBothPlaces()
    {
        var roster = _content.Characters.Values.ToList();
        var fastest = roster.OrderByDescending(c => c.Stats.Run).First();
        var slowest = roster.OrderBy(c => c.Stats.Run).First();
        Assert.Equal(roster.Max(c => FieldingResolver.ChaseSpeedFt(c, false, R)), FieldingResolver.ChaseSpeedFt(fastest, false, R), 9);
        Assert.Equal(roster.Max(c => RunnerSystem.SpeedFtPerSec(c, R)), RunnerSystem.SpeedFtPerSec(fastest, R), 9);
        Assert.Equal(roster.Min(c => FieldingResolver.ChaseSpeedFt(c, false, R)), FieldingResolver.ChaseSpeedFt(slowest, false, R), 9);
        // Two bodies with the same Run and different classes run the same top speed.
        var rio = _content.Must("rio");
        Assert.Equal(FieldingResolver.ChaseSpeedFt(rio, false, R), FieldingResolver.ChaseSpeedFt(rio with { BodyClass = "villain" }, false, R), 12);
        Assert.Equal(RunnerSystem.SpeedFtPerSec(rio, R), RunnerSystem.SpeedFtPerSec(rio with { BodyClass = "speed" }, R), 12);
    }

    // ---------------------------------------------------------------------------------
    // SC-12 — light ramps fast, heavy ramps slow
    // ---------------------------------------------------------------------------------

    (BodyClassRow Light, BodyClassRow Heavy) LightAndHeavy() =>
        (R.BodyClasses.Classes.MinBy(k => k.AccelSec)!, R.BodyClasses.Classes.MaxBy(k => k.AccelSec)!);

    static int FramesTo(Func<(double X, double Z)> step, Func<(double X, double Z), bool> done, int max = 600)
    {
        for (var i = 1; i <= max; i++)
            if (done(step())) return i;
        return int.MaxValue;
    }

    [Fact]
    public void SC12_ALightBodyGetsToSpeedSoonerAndAHeavyOneBrakesAndReversesLater()
    {
        var (light, heavy) = LightAndHeavy();
        Assert.True(light.AccelSec < heavy.AccelSec && light.BrakeSec < heavy.BrakeSec);
        var ground = R.Grounds.Grass.Body;
        const double top = 18;
        (int Start, int Stop, int Reverse) Run(BodyClassRow row)
        {
            var body = new BodyResponse();
            var ramp = (row.AccelSec, row.BrakeSec);
            var start = FramesTo(() => body.Respond("CF", (top, 0), top, 1, ground, ramp, Frame), v => v.X >= top - 1e-9);
            var stop = FramesTo(() => body.Respond("CF", (0, 0), top, 1, ground, ramp, Frame), v => Math.Abs(v.X) < 1e-9);
            FramesTo(() => body.Respond("CF", (top, 0), top, 1, ground, ramp, Frame), v => v.X >= top - 1e-9);
            var reverse = FramesTo(() => body.Respond("CF", (-top, 0), top, 1, ground, ramp, Frame), v => v.X <= -top + 1e-9);
            return (start, stop, reverse);
        }
        var l = Run(light);
        var h = Run(heavy);
        Assert.True(l.Start < h.Start, $"light to speed in {l.Start} frames, heavy in {h.Start}");
        Assert.True(l.Stop < h.Stop, $"light stops in {l.Stop} frames, heavy in {h.Stop}");
        Assert.True(l.Reverse < h.Reverse, $"light reverses in {l.Reverse} frames, heavy in {h.Reverse}");
        // Each is its own row's time, to the frame.
        Assert.InRange(l.Start, (int)Math.Floor(light.AccelSec / Frame), (int)Math.Ceiling(light.AccelSec / Frame) + 1);
        Assert.InRange(h.Start, (int)Math.Floor(heavy.AccelSec / Frame), (int)Math.Ceiling(heavy.AccelSec / Frame) + 1);
    }

    /// <summary>The planner charges each body its own class's half-ramp, the same ramp its step answers at: prediction and stepping agree.</summary>
    [Fact]
    public void SC12_ThePlannerChargesEachBodyItsOwnRamp()
    {
        var match = Match.Exhibition(_content, "rio", "ashlord", 3, 1, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Landing(match.Park, 245, 34, 0, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        var start = Diamond.Positions["CF"];
        var light = _content.Characters.Values.First(c => c.BodyClass == LightAndHeavy().Light.Id);
        var heavy = _content.Characters.Values.First(c => c.BodyClass == LightAndHeavy().Heavy.Id);
        var startMul = GroundZones.Of(match.Park, match.Rules).RowAt(start.X, start.Z, match.Rules.Grounds).Body.StartMul;
        foreach (var who in new[] { light, heavy })
        {
            var route = FieldingPursuit.Plan(preview, match.Park, preview.Ball!.Samples, 0, start.X, start.Z, 18, match.Rules, body: who);
            Assert.Equal(BodyClasses.Ramp(who, match.Rules).AccelSec * startMul / 2, route.RampSec, 9);
        }
    }

    /// <summary>In a live play the classed body's first frames are its class's ramp, not the table's shared one.</summary>
    [Fact]
    public void SC12_TheLiveStepAnswersAtTheBodysClassRamp()
    {
        var home = _content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = _content.Team("Offense", "zig", "boom", "jester", "grit", "soot", "nugget", "pip", "gull", "marlow");
        var match = Match.Exhibition(_content, home, away, 3, 1, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Hit(match.Park, 85, -12, -18, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        var who = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves)["1B"];
        Assert.NotEqual(match.Rules.Fielding.Chase.AccelSec, BodyClasses.Ramp(who, match.Rules).AccelSec);   // the fixture tells the two apart
        var accel = FieldingResolver.ChaseSpeedFt(who, false, match.Rules) / BodyClasses.Ramp(who, match.Rules).AccelSec;
        var gain = FirstFramesGain(live, "1B", 3);
        Assert.InRange(gain, accel * 0.9, accel * 1.1);
    }

    /// <summary>The body's speed gain per second over its first <paramref name="frames"/> moving frames from rest.</summary>
    static double FirstFramesGain(LivePlaySystem live, string pos, int frames, int max = 240)
    {
        var track = new List<(double X, double Z)> { live.Fielders[pos] };
        for (var i = 0; i < max && live.Active; i++)
        {
            live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu));
            if (!live.Active || !live.Fielders.TryGetValue(pos, out var at)) break;
            track.Add(at);
        }
        double Speed(int f) => Diamond.Dist(track[f - 1].X, track[f - 1].Z, track[f].X, track[f].Z) / Frame;
        var first = Enumerable.Range(1, track.Count - 1).First(f => Speed(f) > 1e-6);
        Assert.True(first + frames < track.Count, $"{pos} moved too late to measure");
        return (Speed(first + frames) - Speed(first)) / (frames * Frame);
    }

    // ---------------------------------------------------------------------------------
    // SC-13 — one movement profile per body, whatever the ball
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The same body (Lace at short) on a grounder, a liner and a fly: it builds speed at the one rate — its rated speed over its
    /// class's ramp — whatever the ball (F693-02 consistency). Its rated speed is its own pursuit speed, never the ball's.
    /// </summary>
    [Theory]
    [InlineData(85, -12, -18)]   // a grounder to short
    [InlineData(95, 12, 25)]     // a liner to right
    [InlineData(0, 34, 0)]       // a fly to center (Landing 245 ft)
    public void SC13_OneBodyBuildsSpeedAtOneRateOnEveryBall(double exitMph, double launchDeg, double sprayDeg)
    {
        var home = _content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = _content.Team("Offense", "zig", "boom", "jester", "grit", "soot", "nugget", "pip", "gull", "marlow");
        var match = Match.Exhibition(_content, home, away, 3, 1, parkId: ParkId.Harbor);
        var hit = exitMph > 0
            ? FlightFixtures.Hit(match.Park, exitMph, launchDeg, sprayDeg, rules: match.Rules)
            : FlightFixtures.Landing(match.Park, 245, launchDeg, sprayDeg, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        var gloves = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        var pos = gloves.Single(kv => kv.Value.Id == "lace").Key;
        Assert.False(FieldingResolver.IsOutfield(pos));
        var lace = gloves[pos];
        var accel = FieldingResolver.ChaseSpeedFt(lace, false, match.Rules) / BodyClasses.Ramp(lace, match.Rules).AccelSec;
        Assert.InRange(FirstFramesGain(live, pos, 3), accel * 0.9, accel * 1.1);
    }

    // ---------------------------------------------------------------------------------
    // SC-14 — a light body is knocked back further than a heavy one
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(70, false)]
    [InlineData(105, true)]
    public void SC14_ALightBodyRecoilsLongerThanAHeavyOne(double incomingFtPerSec, bool airborne)
    {
        var (light, heavy) = (R.BodyClasses.Classes.MaxBy(k => k.KnockbackMul)!, R.BodyClasses.Classes.MinBy(k => k.KnockbackMul)!);
        Assert.True(light.KnockbackMul > heavy.KnockbackMul);
        // The same hands, the same ball: only the class differs.
        var rio = _content.Must("rio");
        var l = rio with { BodyClass = light.Id };
        var h = rio with { BodyClass = heavy.Id };
        var lightSec = FieldingResolver.RecoilSec(l, incomingFtPerSec, R, airborne);
        var heavySec = FieldingResolver.RecoilSec(h, incomingFtPerSec, R, airborne);
        Assert.True(heavySec > 0, "the fixture ball is hot enough to recoil");
        Assert.True(lightSec > heavySec, $"light {lightSec:0.000} s, heavy {heavySec:0.000} s");
        Assert.Equal(light.KnockbackMul / heavy.KnockbackMul, lightSec / heavySec, 9);
        var lw = FieldingResolver.RecoilWeight(l, incomingFtPerSec, R, airborne);
        var hw = FieldingResolver.RecoilWeight(h, incomingFtPerSec, R, airborne);
        Assert.True(FieldingResolver.RecoilSkidFt(lw, R) > FieldingResolver.RecoilSkidFt(hw, R));
        // A routine ball costs nobody anything, whatever the class.
        Assert.Equal(0, FieldingResolver.RecoilSec(l, 20, R));
    }

    // ---------------------------------------------------------------------------------
    // Contact width — the class sizes the barrel, and every strike stays hittable
    // ---------------------------------------------------------------------------------

    [Fact]
    public void ContactWidthIsTheClassRowsAndTheOvalIsWhatTheResolverJudges()
    {
        var rio = _content.Must("rio");
        var narrow = R.BodyClasses.Classes.MinBy(k => k.ContactWidthMul)!;
        var wide = R.BodyClasses.Classes.MaxBy(k => k.ContactWidthMul)!;
        var n = SweetSpot.SwingBarrel(rio with { BodyClass = narrow.Id }, null, 0, R);
        var w = SweetSpot.SwingBarrel(rio with { BodyClass = wide.Id }, null, 0, R);
        Assert.Equal(wide.ContactWidthMul / narrow.ContactWidthMul, w / n, 9);
        var oval = SweetSpot.Oval(rio with { BodyClass = wide.Id }, null, 0, 0, R);
        Assert.Equal(w, oval.BarrelScale, 9);
        // The height is the zone's, never the class's.
        Assert.Equal(SweetSpot.HalfHeightFt(StrikeZoneGeometry.For(rio, R)), oval.HalfHeightFt, 9);

        // The class never takes a batter who reaches every strike with the box centered out of it (§5.2).
        foreach (var c in _content.Characters.Values)
        {
            var plain = SweetSpot.SwingBarrel(c with { BodyClass = "" }, null, 0, R);
            var zone = StrikeZoneGeometry.For(c, R);
            if (SweetSpot.CoversTheZone(c.Bats, R, zone, plain))
                Assert.True(SweetSpot.CoversTheZone(c.Bats, R, zone, SweetSpot.SwingBarrel(c, null, 0, R)), $"{c.Id}'s class takes a strike off the bat");
        }
    }
}
