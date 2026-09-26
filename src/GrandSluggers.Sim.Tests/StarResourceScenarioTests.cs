using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The Star resource (spec §12, PH-16-R3 … R9, R12, R13, R16), Appendix B.1 rows S-170 … S-177 (the price and the
/// settle) and S-180 … S-185 (one pool per team, the equal starting reserve, the plate-appearance seam).
///
/// <see cref="Match"/> settles every released special: affordable, the team pays the ability's tier price at the
/// release; unaffordable, the action is the ordinary pitch or swing at the same timing, nothing is spent, and the
/// play carries a typed <see cref="StarRequest"/> saying so. Every row reads prices, the reserve and the gains from
/// the shipped table; only S-176 names the accepted numbers.
/// </summary>
public sealed class StarResourceScenarioTests
{
    static readonly ContentCatalog Shipped = ContentCatalog.Load(new DataRoot(global::GrandSluggers.Sim.Tests.Shipped.Content.Root.Shipped));
    static double CenterY => StrikeZoneGeometry.Reference.CenterY;

    /// <summary>Spend the defense down with paid Star Pitches thrown as takes well outside, until it cannot pay.</summary>
    static void DrainDefense(Match m)
    {
        for (var guard = 0; m.CanStarPitch && guard < 20; guard++)
            m.Play(Scenario.PitchAt(2.5, CenterY) with { Star = true }, Scenario.Take);
        Assert.False(m.CanStarPitch);
    }

    /// <summary>Spend the offense down with paid Star Swings that whiff, until it cannot pay.</summary>
    static void DrainOffense(Match m)
    {
        for (var guard = 0; m.CanStarSwing && guard < 20; guard++)
            m.Play(Scenario.PitchAt(0, CenterY), new SwingCommand(true, 0, 40, true));
        Assert.False(m.CanStarSwing);
    }

    static void SamePlay(PlayEvent a, PlayEvent b)
    {
        Assert.Equal(b.Kind, a.Kind);
        Assert.Equal(b.AtBat, a.AtBat);
        Assert.Equal(b.Pitch, a.Pitch);
        Assert.Equal(b.Swing, a.Swing);
        Assert.Equal(b.NextState, a.NextState);
        Assert.Equal(b.LandingX, a.LandingX);
        Assert.Equal(b.LandingZ, a.LandingZ);
    }

    // ---------------------------------------------------------------------------------
    // S-170  An unaffordable Star Pitch is the ordinary pitch of the selected family
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S170_AnUnaffordableStarPitchIsTheOrdinaryPitchOfItsFamilyAtTheSameTimingAndCostsNothing()
    {
        var content = Shipped;
        foreach (var seed in new[] { 1, 2, 3 })
        {
            var families = new Scenario(content, seed).Match.Pitcher.Repertoire.Ordinary;
            foreach (var family in families)
            foreach (var swing in new[] { Scenario.Take, Scenario.SwingAt(0), Scenario.SwingAt(40) })
            {
                var asked = new Scenario(content, seed).Match;
                var plain = new Scenario(content, seed).Match;
                DrainDefense(asked);
                DrainDefense(plain);
                var pitch = Scenario.PitchAt(0.3, CenterY, charge: 0.5, family: family);
                var stars = asked.DefenseStars;
                var cost = asked.PitchStarCost;
                var stamina = asked.PitcherStamina;

                var a = asked.Play(pitch with { Star = true }, swing);
                var b = plain.Play(pitch, swing);

                // The ordinary pitch of that family, charged as it was, crossing where it was aimed: play for play.
                SamePlay(a, b);
                Assert.False(a.Pitch.Star);
                Assert.Equal(family, a.Pitch.Type);
                Assert.Null(a.AtBat.StarPitchUsed);
                Assert.Equal(plain.PitcherStamina, asked.PitcherStamina);
                Assert.True(asked.PitcherStamina < stamina);
                // Nothing spent; the typed event names what was asked and why it went out ordinary.
                Assert.Equal(plain.HomeStars, asked.HomeStars);
                Assert.Equal(plain.AwayStars, asked.AwayStars);
                var request = Assert.Single(a.Outcome!.Stars);
                Assert.Equal(new StarRequest(StarAction.Pitch, true, a.Pitcher.Id, a.Pitcher.StarPitch, cost, stars, false), request);
                Assert.Equal(0, request.Spent);
                Assert.Empty(b.Outcome!.Stars);
            }
        }
    }

    // ---------------------------------------------------------------------------------
    // S-171  An unaffordable Star Swing is the ordinary swing
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S171_AnUnaffordableStarSwingIsTheOrdinarySwingAtTheSameTimingAndCostsNothing()
    {
        var content = Shipped;
        foreach (var seed in new[] { 1, 2, 3, 4 })
        foreach (var err in new[] { -2.0, 0, 2, 40 })
        foreach (var charge in new[] { 0.0, 1 })
        {
            var asked = new Scenario(content, seed).Match;
            var plain = new Scenario(content, seed).Match;
            DrainOffense(asked);
            DrainOffense(plain);
            var stars = asked.OffenseStars;
            var cost = asked.SwingStarCost;
            var swing = Scenario.SwingAt(err, charge);

            var a = asked.Play(Scenario.PitchAt(0.2, CenterY), swing with { Star = true });
            var b = plain.Play(Scenario.PitchAt(0.2, CenterY), swing);

            SamePlay(a, b);
            Assert.False(a.Swing.Star);
            Assert.Null(a.AtBat.StarSwingUsed);
            Assert.Equal(plain.HomeStars, asked.HomeStars);
            Assert.Equal(plain.AwayStars, asked.AwayStars);
            var request = Assert.Single(a.Outcome!.Stars);
            Assert.Equal(new StarRequest(StarAction.Swing, false, a.Batter.Id, a.Batter.StarSwing, cost, stars, false), request);
        }
    }

    [Fact]
    public void S171_ATakeWithTheStarHeldReleasesNoSwingAndSettlesNothing()
    {
        var m = new Scenario(Shipped).Match;
        var before = m.OffenseStars;
        var ev = m.Play(Scenario.PitchAt(2.5, CenterY), Scenario.Take with { Star = true });
        Assert.Equal(before, m.OffenseStars);
        Assert.Empty(ev.Outcome!.Stars);
    }

    // ---------------------------------------------------------------------------------
    // S-172  An affordable special pays its carrier's price at the release
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S172_AnAffordableStarPitchPaysItsCarriersPriceAtTheReleaseAndFliesAsTheSpecial()
    {
        var content = Shipped;
        var m = new Scenario(content).Match;
        m.GiveDefenseStars(content.Rules.Stars.MeterMax);
        var cost = m.PitchStarCost;
        Assert.Equal(content.Rules.Stars.Prices.Of(m.Pitcher), cost);
        var before = m.DefenseStars;

        var ev = m.Play(Scenario.PitchAt(2.5, CenterY) with { Star = true }, Scenario.Take);

        Assert.True(ev.Pitch.Star);
        Assert.Equal(before - cost, m.HomeStars);
        var request = Assert.Single(ev.Outcome!.Stars);
        Assert.True(request.Afforded);
        Assert.Equal(cost, request.Spent);
        Assert.Equal(before, request.StarsBefore);
    }

    [Fact]
    public void S172_ExactlyThePriceIsEnoughAndOneShortIsNot()
    {
        // A captain, whose price is above 1, so "one short" is still a positive balance.
        var m = new Scenario(Shipped).Match;
        var cost = m.PitchStarCost;
        Assert.True(cost > 1, $"pitcher {m.Pitcher.Id} costs {cost}");
        DrainDefense(m);
        var shy = m.DefenseStars;
        Assert.True(shy < cost);
        m.GiveDefenseStars(cost - 0.01);
        Assert.False(m.CanStarPitch);
        Assert.False(m.Play(Scenario.PitchAt(2.5, CenterY) with { Star = true }, Scenario.Take).Pitch.Star);
        m.GiveDefenseStars(cost);
        Assert.True(m.CanStarPitch);
        var paid = m.Play(Scenario.PitchAt(2.5, CenterY) with { Star = true }, Scenario.Take);
        Assert.True(paid.Pitch.Star);
        var request = Assert.Single(paid.Outcome!.Stars);
        Assert.True(request.Afforded);
        Assert.InRange(request.StarsBefore - cost, 0, 0.2);
    }

    // ---------------------------------------------------------------------------------
    // S-173  A missed Star Swing pays the ability's full price
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S173_AMissedStarSwingPaysTheFullPriceOfTheAbilityItAttempted()
    {
        var content = Shipped;
        var m = new Scenario(content).Match;
        var prices = new HashSet<int>();
        // Nine hitters whiff their way through the order: every one pays his own ability's price, no discount.
        for (var pa = 0; pa < 9; pa++)
        {
            var batter = m.Batter;
            for (var strike = 0; strike < 3 && m.Batter == batter; strike++)
            {
                m.GiveOffenseStars(content.Rules.Stars.MeterMax);
                var before = m.OffenseStars;
                var cost = m.SwingStarCost;
                var guest = batter.Captain && !batter.Id.Equals(m.Offense.Captain.Id, StringComparison.OrdinalIgnoreCase);
                Assert.Equal(content.Rules.Stars.Prices.Of(batter) + (guest ? content.Rules.Stars.Costs.GuestCaptainSurcharge : 0), cost);
                var ev = m.Play(Scenario.PitchAt(0, CenterY), new SwingCommand(true, 0, 40, true));
                Assert.Equal(ContactQuality.Miss, ev.AtBat.Quality);
                Assert.True(ev.Swing.Star);
                var request = Assert.Single(ev.Outcome!.Stars);
                Assert.True(request.Afforded);
                Assert.Equal(cost, request.Spent);
                // A third strike completes the appearance, which earns both teams the base gain on top (P5-b).
                var paGain = ev.Kind == PlayKind.Strikeout ? content.Rules.Stars.Gains.PlateAppearance : 0;
                Assert.Equal(before - cost + paGain, ev.Context!.Top ? m.AwayStars : m.HomeStars, 9);
                prices.Add(cost);
            }
        }
        Assert.True(prices.Count > 1, "captains and sidekicks price the order's specials differently");
    }

    [Fact]
    public void S173_AGuestCaptainPaysTheCaptainPricePlusTheSurchargeOnAWhiffToo()
    {
        var content = Shipped;
        var m = new Scenario(content).Match;
        m.SkipToHomeHalf();
        // Vale bats for Rio's team: a captain who does not captain the side he swings for.
        for (var guard = 0; !m.Batter.Id.Equals("vale", StringComparison.OrdinalIgnoreCase) && guard < 40; guard++)
            m.Play(Scenario.PitchAt(0, CenterY), Scenario.SwingAt(40));
        var vale = m.Batter;
        Assert.Equal("vale", vale.Id);
        Assert.NotEqual(vale.Id, m.Offense.Captain.Id);
        var cost = content.Rules.Stars.Prices.Captain + content.Rules.Stars.Costs.GuestCaptainSurcharge;
        Assert.Equal(cost, m.SwingStarCost);
        m.GiveOffenseStars(content.Rules.Stars.MeterMax);
        var before = m.OffenseStars;
        var ev = m.Play(Scenario.PitchAt(0, CenterY), new SwingCommand(true, 0, 40, true));
        Assert.Equal(ContactQuality.Miss, ev.AtBat.Quality);
        Assert.Equal(before - cost, m.HomeStars, 9);
    }

    // ---------------------------------------------------------------------------------
    // S-174  Specials belong to their carrier; the prices are validated
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S174_ASidekickCarryingACaptainsSpecialIsRefusedByName()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json => json["pitches"]!["fastball"]!["kind"] = "element");
        var errors = ContentDataValidator.Validate(new DataRoot(fixture.Root));
        // The species carries its sidekicks' specials (AB-10), so the refusal names the species row.
        Assert.Contains(errors, e => e.Contains("starPitch 'fastball' is a captain's special; a species carries the generic pool's", StringComparison.Ordinal)
            && e.Contains("species.json", StringComparison.Ordinal));
    }

    [Fact]
    public void S174_TwoCaptainsSharingASpecialAndACaptainOnAGenericAreRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/vale.json", json => json["starPitch"] = "heatball");
        fixture.ChangeObject("characters/zig.json", json => json["starSwing"] = "line");
        var errors = ContentDataValidator.Validate(new DataRoot(fixture.Root));
        Assert.Contains(errors, e => e.Contains("starPitch 'heatball' is already", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("captain 'zig' starSwing 'line' is a sidekick's generic special", StringComparison.Ordinal));
    }

    [Fact]
    public void S174_ACaptainsSpecialWithNoEffectFamilyIsRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json => json["pitches"]!["heatball"]!.AsObject().Remove("family"));
        var errors = ContentDataValidator.Validate(new DataRoot(fixture.Root));
        Assert.Contains(errors, e => e.Contains("starPitch 'heatball' names no effect family", StringComparison.Ordinal));
    }

    [Fact]
    public void S174_TwoCaptainsSpecialsInOneEffectFamilyAreRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["heatball"]!["family"] = "late-rise";
            json["pitches"]!["skullball"]!["family"] = "late-rise";
        });
        var errors = ContentDataValidator.Validate(new DataRoot(fixture.Root));
        Assert.Contains(errors, e => e.Contains("shares effect family 'late-rise'", StringComparison.Ordinal));
    }

    [Fact]
    public void S174_ARowThatStillNamesATierIsRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json => json["swings"]!["furnace"]!["tier"] = "top");
        var errors = ContentDataValidator.Validate(new DataRoot(fixture.Root));
        Assert.Contains(errors, e => e.Contains("star swing 'furnace' names a tier; tiers are retired", StringComparison.Ordinal));
    }

    [Fact]
    public void S174_APriceThatIsFreeOrTooDearIsRefused()
    {
        foreach (var (captain, sidekick, expect) in new[]
                 {
                     (2, 0, "stars.prices.sidekick must cost at least 1"),
                     (6, 1, "stars.prices.captain must fit the meter"),
                 })
        {
            using var fixture = new ContentFixture();
            fixture.ChangeObject("rules/stars.json", json =>
            {
                json["prices"]!["captain"] = captain;
                json["prices"]!["sidekick"] = sidekick;
            });
            Assert.Contains(RulesTable.Validate(new DataRoot(fixture.Root)), e => e.Contains(expect, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void S174_EveryCharacterHasOneStarPitchAndOneStarSwingAndOnlyCaptainsCarryTheirOwn()
    {
        foreach (var content in new[] { Shipped })
        {
            Assert.Empty(ContentDataValidator.Validate(content.Root));
            // Every star lesson funds its special at its price, guest captains included.
            TutorialCatalog.Load(content);
            foreach (var c in content.Characters.Values)
            {
                var pitch = content.StarSkills.Pitch(c.StarPitch);
                var swing = content.StarSkills.Swing(c.StarSwing);
                Assert.NotNull(pitch);
                Assert.NotNull(swing);
                Assert.Equal(!c.Captain, pitch!.Kind == StarSkills.GenericKind);
                Assert.Equal(!c.Captain, swing!.Kind == StarSkills.GenericKind);
            }
        }
    }

    // ---------------------------------------------------------------------------------
    // S-175  Specials keep the ordinary charge tradeoffs
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S175_AChargedStarPitchGainsSpeedAndLosesSteeringExactlyAsTheOrdinaryPitchDoes()
    {
        var rules = Shipped.Rules;
        foreach (var c in Shipped.Characters.Values)
        foreach (var family in c.Repertoire.Ordinary)
        {
            var quick = new PitchCommand(family, 0, false);
            var loaded = new PitchCommand(family, 1, false);
            double Mph(PitchCommand p) => AtBatResolver.PitchSpeedMph(p, c, rules, Shipped.StarSkills);
            // Power: the charge adds the same speed share to the special as to the ordinary pitch (above the floor).
            if (Mph(quick with { Star = true }) > rules.Pitching.Flight.MinMph)
                Assert.Equal(Mph(loaded) / Mph(quick), Mph(loaded with { Star = true }) / Mph(quick with { Star = true }), 9);
            Assert.True(Mph(loaded with { Star = true }) > Mph(quick with { Star = true }));
            // Steering: the stick moves a charged special exactly as little as a charged ordinary pitch.
            double Steer(PitchCommand p) =>
                PitchFlight.Point(p with { BreakX = 1 }, 1, rules, c.StarPitch).X - PitchFlight.Point(p, 1, rules, c.StarPitch).X;
            Assert.Equal(Steer(loaded), Steer(loaded with { Star = true }), 9);
            Assert.Equal(Steer(quick), Steer(quick with { Star = true }), 9);
            // A family already damped at every charge (the changeup's row) loses nothing more; the rest lose steering.
            var damped = rules.Pitching.Families.Of(family).BreakDamped;
            if (damped) Assert.Equal(Steer(quick with { Star = true }), Steer(loaded with { Star = true }), 9);
            else Assert.True(Math.Abs(Steer(loaded with { Star = true })) < Math.Abs(Steer(quick with { Star = true })));
        }
    }

    [Fact]
    public void S175_AChargedStarSwingGainsPowerAndLosesPlacementExactlyAsTheOrdinarySwingDoes()
    {
        var park = Shipped.Parks[ParkId.Harbor];
        var resolver = new AtBatResolver(Shipped.Chemistry, Shipped.Rules, Shipped.StarSkills);
        var pitcher = Shipped.Must("vale");
        var bat = Shipped.Bats["harbor-lumber"];
        var lostPlacement = false;
        foreach (var batter in new[] { Shipped.Must("rio"), Shipped.Must("konga"), Shipped.Must("dart") })
        {
            var skill = Shipped.StarSkills.Swing(batter.StarSwing)!;
            foreach (var charge in new[] { 0.0, 0.5, 1 })
            foreach (var offset in new[] { 0.0, 0.3, 0.6, 0.9, 1.2 })
            {
                var input = new AtBatInput(pitcher, batter, null, [], false, false, 0, false, false, bat, 80,
                    PitchInZone: true, Charge01: charge, CrossingX: offset);
                var plain = resolver.Resolve(input, park, new Random(5));
                var star = resolver.Resolve(input with { UseStarSwing = true }, park, new Random(5));
                // Placement: the special meets the ball exactly where the ordinary swing at that charge does. A swing's own
                // larger Perfect ring (§13, Sparkler) only grades a Nice meeting Perfect; it never turns a miss into contact.
                if (skill.PerfectRingMul != 1 && plain.Quality == ContactQuality.Nice && star.Quality == ContactQuality.Perfect)
                    continue;
                Assert.Equal(plain.Quality, star.Quality);
                if (plain.Quality == ContactQuality.Miss)
                {
                    lostPlacement |= charge > 0;
                    continue;
                }
                // Power: the ability multiplies the charged exit, it does not replace it.
                Assert.InRange(star.ExitVeloMph - plain.ExitVeloMph * skill.ExitVeloMul, -0.2, 0.2);
            }
        }
        Assert.True(lostPlacement, "some offset the quick swing meets, the loaded swing misses");
    }

    // ---------------------------------------------------------------------------------
    // S-176  The accepted prices
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S176_ACaptainsSpecialCostsTwoASidekicksOneAndTheReserveBuysAGuestCaptainsToo()
    {
        var stars = Shipped.Rules.Stars;
        var p = stars.Prices;
        // Accepted by Jack: a captain's special 2, a sidekick's 1, surcharge 1, reserve 3, base gain 0.1 per appearance.
        Assert.Equal((2, 1), (p.Captain, p.Sidekick));
        Assert.Equal(1, stars.Costs.GuestCaptainSurcharge);
        Assert.Equal(3, stars.StartingReserve);
        Assert.Equal(0.1, stars.Gains.PlateAppearance, 9);
        // The relations: a captain's special costs more than a sidekick's; the reserve buys any captain's special,
        // a guest captain's included, at the opening appearance; a full meter buys it too.
        Assert.True(p.Sidekick < p.Captain);
        Assert.True(stars.StartingReserve >= p.Captain + stars.Costs.GuestCaptainSurcharge);
        Assert.True(p.Captain + stars.Costs.GuestCaptainSurcharge <= stars.MeterMax);
    }

    // ---------------------------------------------------------------------------------
    // S-177  The CPU never asks for what it cannot pay
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S177_InWholeCpuGamesEverySpecialIsPaidAtItsPriceAndNoneIsUnavailable()
    {
        var content = Shipped;
        var asked = 0;
        foreach (var seed in new[] { 1, 2, 3 })
        {
            var m = Match.Exhibition(content, "rio", "ashlord", innings: 3, seed: seed);
            m.AutoPlayGame();
            foreach (var r in m.Log.SelectMany(e => e.Outcome?.Stars ?? []))
            {
                asked++;
                Assert.True(r.Afforded, $"{r.CharacterId} asked for {r.AbilityId} at {r.Cost} with {r.StarsBefore}");
                Assert.Equal(r.Cost, r.Spent);
                Assert.True(r.StarsBefore >= r.Cost);
            }
        }
        Assert.True(asked > 0, "the CPU used a special in three games");
    }

    // =================================================================================
    // P5-b: one pool, one starting reserve, the plate-appearance seam (PH-16-R4, R5, R6, R16)
    // =================================================================================

    static readonly string[] Captains = ["rio", "vale", "zig", "brondo", "konga", "ashlord", "fenn"];

    // ---------------------------------------------------------------------------------
    // S-180  The reserve is fixed and equal (the chemistry rows sit in TeamBuilderTests / ChemistryTests)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S180_EveryPairingStartsBothTeamsOnTheSameReserveWhateverTheirChemistry()
    {
        var content = Shipped;
        var reserve = content.Rules.Stars.StartingReserve;
        foreach (var home in Captains)
        foreach (var away in Captains.Where(a => a != home))
        {
            var m = Match.Exhibition(content, home, away, innings: 3, seed: 1);
            Assert.Equal(reserve, m.HomeStars);
            Assert.Equal(reserve, m.AwayStars);
        }
    }

    // ---------------------------------------------------------------------------------
    // S-181  A usable reserve: a special from the opening plate appearance
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S181_BothSidesCanAffordTheirSpecialsAtTheirOpeningPlateAppearance()
    {
        var content = Shipped;
        foreach (var home in Captains)
        foreach (var away in Captains.Where(a => a != home))
        {
            // Top of the first: the home side's pitcher against the away leadoff.
            var top = Match.Exhibition(content, home, away, innings: 3, seed: 1);
            Assert.True(top.CanStarPitch, $"{top.Pitcher.Id} pitching the opener: {top.PitchStarCost} vs {top.DefenseStars}");
            Assert.True(top.CanStarSwing, $"{away} leadoff {top.Batter.Id}: {top.SwingStarCost} vs {top.OffenseStars}");
            // Bottom of the first, from the untouched reserve: the other side's pitcher and leadoff.
            var bottom = Match.Exhibition(content, home, away, innings: 3, seed: 1);
            bottom.SkipToHomeHalf();
            Assert.True(bottom.CanStarPitch, $"{bottom.Pitcher.Id} pitching: {bottom.PitchStarCost} vs {bottom.DefenseStars}");
            Assert.True(bottom.CanStarSwing, $"{home} leadoff {bottom.Batter.Id}: {bottom.SwingStarCost} vs {bottom.OffenseStars}");
        }
    }

    [Fact]
    public void S181_TheRulesValidatorRefusesAReserveOffTheMeterOrBelowTheCheaperPrice()
    {
        foreach (var (reserve, low, expect) in new[]
                 {
                     (6, 1, "stars.startingReserve must fit the meter"),
                     (1, 2, "stars.startingReserve must buy the cheaper price"),
                 })
        {
            using var fixture = new ContentFixture();
            fixture.ChangeObject("rules/stars.json", json =>
            {
                json["startingReserve"] = reserve;
                json["prices"]!["captain"] = low;
                json["prices"]!["sidekick"] = low;
            });
            Assert.Contains(RulesTable.Validate(new DataRoot(fixture.Root)), e => e.Contains(expect, StringComparison.Ordinal));
        }
    }

    // ---------------------------------------------------------------------------------
    // S-182  One pool per team, pitching and batting
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S182_ATeamsStarPitchAndItsStarSwingDrawOnOnePoolAndNeverTheOpponents()
    {
        var content = Shipped;
        var m = new Scenario(content).Match;
        var away = m.AwayStars;
        var pitchCost = m.PitchStarCost;
        // Top: the home side pitches a paid special; only the home pool moves.
        m.Play(Scenario.PitchAt(2.5, CenterY) with { Star = true }, Scenario.Take);
        Assert.Equal(content.Rules.Stars.StartingReserve - pitchCost, m.HomeStars, 9);
        Assert.Equal(away, m.AwayStars);
        // Bottom: the same home side bats, and its Star Swing reads the pool its pitch already drew down.
        var home = m.HomeStars;
        m.SkipToHomeHalf();
        Assert.Equal(home, m.OffenseStars);
        var cost = m.SwingStarCost;
        Assert.Equal(home >= cost, m.CanStarSwing);
        var ev = m.Play(Scenario.PitchAt(0, CenterY), new SwingCommand(true, 0, 40, true));
        // Paid from that pool when it covers the price; unavailable against that same pool when the pitch left it short.
        var request = Assert.Single(ev.Outcome!.Stars);
        Assert.Equal(home, request.StarsBefore);
        Assert.Equal(home >= cost, request.Afforded);
        Assert.Equal(home - request.Spent, m.HomeStars, 9);
        Assert.Equal(away, m.AwayStars);
    }

    // ---------------------------------------------------------------------------------
    // S-183  The base gain: both teams, once, at the completed plate appearance
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S183_EveryCompletedPlateAppearanceEarnsBothTeamsTheBaseGainOnceAndNoPitchBeforeItDoes()
    {
        var g = Shipped.Rules.Stars.Gains;
        var max = Shipped.Rules.Stars.MeterMax;
        Assert.True(g.PlateAppearance > 0, "the table names a base gain");
        foreach (var seed in new[] { 1, 2, 3 })
        {
            var m = new Scenario(Shipped, seed).Match;
            // Ball, ball, ball: nothing completes, nothing is earned.
            for (var i = 0; i < 3; i++)
            {
                var (h, a) = (m.HomeStars, m.AwayStars);
                m.Play(Scenario.PitchAt(2.5, CenterY), Scenario.Take);
                Assert.Equal((h, a), (m.HomeStars, m.AwayStars));
            }
            // Ball four: a walk earns no bonus, so each pool moves by exactly the base.
            var (hw, aw) = (m.HomeStars, m.AwayStars);
            Assert.Equal(PlayKind.Walk, m.Play(Scenario.PitchAt(2.5, CenterY), Scenario.Take).Kind);
            Assert.Equal(Math.Min(max, hw + g.PlateAppearance), m.HomeStars, 9);
            Assert.Equal(Math.Min(max, aw + g.PlateAppearance), m.AwayStars, 9);
            // A strikeout: the defense stacks its bonus on the base; the offense still earns the base.
            for (var i = 0; i < 2; i++) m.Play(Scenario.Paint, Scenario.Take);
            var (hk, ak) = (m.HomeStars, m.AwayStars);
            Assert.Equal(PlayKind.Strikeout, m.Play(Scenario.Paint, Scenario.Take).Kind);
            Assert.Equal(Math.Min(max, hk + g.Strikeout + g.PlateAppearance), m.HomeStars, 9);
            Assert.Equal(Math.Min(max, ak + g.PlateAppearance), m.AwayStars, 9);
        }
    }

    [Fact]
    public void S183_AHitEarnsTheOffenseItsBonusAndBothTeamsTheBaseOnce()
    {
        var g = Shipped.Rules.Stars.Gains;
        var max = Shipped.Rules.Stars.MeterMax;
        var seen = 0;
        for (var seed = 1; seed <= 40 && seen < 6; seed++)
        foreach (var err in new[] { -3.0, -1.5, 0, 1.5, 3 })
        {
            var m = new Scenario(Shipped, seed).Match;
            var (h, a) = (m.HomeStars, m.AwayStars);
            var ev = m.Play(Scenario.PitchAt(0, CenterY), Scenario.SwingAt(err, charge: 1));
            var bonus = ev.Kind switch
            {
                PlayKind.Single => g.Single,
                PlayKind.Double or PlayKind.Triple => g.ExtraBaseHit,
                PlayKind.HomeRun => g.HomeRun,
                _ => -1
            };
            if (bonus < 0 || ev.Outcome!.OutsMade.Count > 0) continue;
            seen++;
            // The hit's bonus (and the billboard's, if it rang) to the offense; the base to both, once.
            var offense = m.AwayStars;
            Assert.True(Math.Abs(offense - Math.Min(max, a + bonus + g.PlateAppearance)) < 1e-9
                        || Math.Abs(offense - Math.Min(max, a + bonus + g.Billboard + g.PlateAppearance)) < 1e-9,
                $"{ev.Kind}: {a} → {offense}");
            Assert.Equal(Math.Min(max, h + g.PlateAppearance), m.HomeStars, 9);
        }
        Assert.True(seen > 0, "a clean hit in two hundred swings");
    }

    [Fact]
    public void S183_ATableWithNoBaseGainEarnsNothingAtACompletedAppearance()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("rules/stars.json", json => json["gains"]!["plateAppearance"] = 0);
        var content = ContentCatalog.Load(new DataRoot(fixture.Root));
        Assert.Equal(0, content.Rules.Stars.Gains.PlateAppearance);
        var m = new Scenario(content).Match;
        var (h, a) = (m.HomeStars, m.AwayStars);
        for (var i = 0; i < 4; i++) m.Play(Scenario.PitchAt(2.5, CenterY), Scenario.Take);
        Assert.Equal((h, a), (m.HomeStars, m.AwayStars));
    }

    // ---------------------------------------------------------------------------------
    // S-184  The caught-stealing third out mid-appearance
    // ---------------------------------------------------------------------------------

    static Match StealTeams(ContentCatalog content)
    {
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = content.Team("Offense", "zig", "konga", "dart", "jester", "cinder", "grit", "soot", "boom", "nugget");
        return Match.Exhibition(content, home, away, 3, 1);
    }

    [Fact]
    public void S184_AHalfThatEndsOnACaughtStealingBetweenPitchesCompletesNoPlateAppearance()
    {
        var content = Shipped;
        var g = content.Rules.Stars.Gains;
        var m = StealTeams(content);
        Assert.True(m.SetOuts(2));
        Assert.True(m.StationRunner(1, m.AwayOrder.Single(c => c.Id == "konga")));
        Assert.Equal("konga", m.First!.Id);
        var batter = m.AwayBatter;
        var (h, a) = (m.HomeStars, m.AwayStars);
        Assert.True(m.StartSteal());

        var ev = m.Play(Scenario.Paint, Scenario.Take);

        Assert.Equal(PlayKind.CaughtStealing, ev.Kind);
        Assert.Single(ev.Outcome!.OutsMade);
        Assert.False(m.Top);
        // The batter's appearance did not complete: he leads off his side's next half with a fresh count,
        // and neither pool earned the base; the defense has only its live-out bonus.
        Assert.Equal(batter, m.AwayBatter);
        Assert.Equal((0, 0), (m.Balls, m.Strikes));
        Assert.Equal(a, m.AwayStars);
        Assert.Equal(Math.Min(content.Rules.Stars.MeterMax, h + g.LiveOut), m.HomeStars, 9);
    }

    [Fact]
    public void S184_AStrikeoutWithTheCaughtStealingCompletesTheAppearanceOnceAndEarnsTheBaseOnce()
    {
        var g = Shipped.Rules.Stars.Gains;
        var m = StealTeams(Shipped);
        Assert.True(m.StationRunner(1, m.AwayOrder.Single(c => c.Id == "konga")));
        for (var i = 0; i < 2; i++) m.Play(Scenario.Paint, Scenario.Take);
        var batter = m.AwayBatter;
        var (h, a) = (m.HomeStars, m.AwayStars);
        Assert.True(m.StartSteal());

        var ev = m.Play(Scenario.Paint, Scenario.Take);

        Assert.Equal(PlayKind.CaughtStealing, ev.Kind);
        Assert.Equal(2, ev.Outcome!.OutsMade.Count);
        Assert.Equal((batter + 1) % m.AwayOrder.Count, m.AwayBatter);
        Assert.Equal(a + g.PlateAppearance, m.AwayStars, 9);
        Assert.Equal(Math.Min(Shipped.Rules.Stars.MeterMax, h + g.Strikeout + g.LiveOut + g.PlateAppearance), m.HomeStars, 9);
    }

    // ---------------------------------------------------------------------------------
    // S-185  Whole games stay inside the meter
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S185_InWholeCpuGamesEveryPoolStaysBetweenEmptyAndTheMeter()
    {
        var content = Shipped;
        var max = content.Rules.Stars.MeterMax;
        foreach (var seed in new[] { 1, 2, 3 })
        {
            var m = Match.Exhibition(content, "vale", "konga", innings: 3, seed: seed);
            for (var guard = 0; !m.Over && guard < 2000; guard++)
            {
                m.AutoPlay();
                Assert.InRange(m.HomeStars, 0, max);
                Assert.InRange(m.AwayStars, 0, max);
            }
            Assert.True(m.Over);
        }
    }
}
