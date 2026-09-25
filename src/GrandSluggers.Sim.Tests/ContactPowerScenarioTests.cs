using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The Contact / Power split (#837, PH-15-R5), Appendix B.1 rows S-121 … S-123.
///
/// <b>S-121</b>: every character authors Contact and Power, and the Bat bar is their rounded mean
/// (CH-07). <b>S-122</b>: each trait moves its own consumers and nobody else's — the cursor is Contact's, the exit and the loft are Power's, and each
/// CPU read takes the trait §5.9 names. <b>S-123</b>: the validator requires the keys and refuses them
/// outside 1–10 by character and field name.
///
/// Every row asserts a relationship or an integer identity, never a stored libm double: the values
/// are balance and must be able to move without editing a test.
/// </summary>
public class ContactPowerScenarioTests
{
    readonly ContentCatalog _content = Shipped.Content;

    // ---------------------------------------------------------------------------------
    // S-121 — every character authors both, and the Bat bar is their rounded mean
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S121_EveryShippedCharacterDerivesBatFromContactAndPower()
    {
        Assert.NotEmpty(_content.Characters);
        foreach (var c in _content.Characters.Values)
            Assert.Equal(Stats.Bar(c.Stats.Contact + c.Stats.Power, 2), c.Stats.Bat);
    }

    [Fact]
    public void S121_TheBatBarRoundsHalfUpAndAClampHoldsEachTrait()
    {
        Assert.Equal(8, (Stats.Even(5, 5, 5, 5) with { Contact = 9, Power = 6 }).Bat); // 7.5
        Assert.Equal(6, (Stats.Even(5, 5, 5, 5) with { Contact = 9, Power = 3 }).Bat); // 6.0
        Assert.Equal((4, 4), (Stats.Even(5, 5, 5, 5).WithBat(4).Contact, Stats.Even(5, 5, 5, 5).WithBat(4).Power));

        var clamped = (Stats.Even(5, 5, 5, 5) with { Contact = 99, Power = -3 }).Clamp();
        Assert.Equal((10, 1), (clamped.Contact, clamped.Power));
        Assert.Equal(6, clamped.Bat); // 5.5 rounds half up
    }

    [Fact]
    public void S121_StatsRoundTripsThroughJson()
    {
        // The flags are bookkeeping about where a number came from, not ratings: emitting them would
        // make Stats unstable across a round trip, because a serialized rating reloads through the
        // init setter and marks the trait authored (the Arm rule, #710). What has to be stable is
        // the serialized form PlayTraceIdentity hashes.
        foreach (var stats in new[]
                 {
                     Stats.Even(3, 7, 4, 6),
                     (Stats.Even(3, 7, 4, 6) with { Contact = 9, Power = 2 }),
                     (Stats.Even(3, 7, 4, 6) with { Arm = 8, Hands = 2, Contact = 2, Power = 9 })
                 })
        {
            var json = JsonSerializer.Serialize(stats, PlayTrace.Json);
            Assert.Contains("\"contact\":", json);
            Assert.Contains("\"power\":", json);

            var back = JsonSerializer.Deserialize<Stats>(json, PlayTrace.Json)!;
            Assert.Equal((stats.Pitch, stats.Bat, stats.Field, stats.Run), (back.Pitch, back.Bat, back.Field, back.Run));
            Assert.Equal((stats.Contact, stats.Power), (back.Contact, back.Power));
            Assert.Equal((stats.Arm, stats.Hands), (back.Arm, back.Hands));
            Assert.Equal(json, JsonSerializer.Serialize(back, PlayTrace.Json));
        }
    }

    [Fact]
    public void S121_TheCpuArchetypeIsUnchangedForEveryShippedCharacter()
    {
        // CpuChargeChance now reads Contact for the technique gate and Power for the split. While
        // nothing is authored that has to be the same answer the Bat/Run formula gave.
        var a = _content.Rules.Batting.Cpu.Archetype;
        foreach (var c in _content.Characters.Values)
        {
            var bat = c.Stats.Bat;
            var run = c.Stats.Run;
            var before = bat >= a.TechniqueMin && run >= a.TechniqueMin ? a.Technique
                : bat - run >= a.SplitStat ? a.Power
                : run - bat >= a.SplitStat ? a.Speed
                : a.Balanced;
            Assert.Equal(before, CpuBatter.ChargeChance(c, a));
        }
    }

    // ---------------------------------------------------------------------------------
    // S-122 — authored in a fixture: each consumer follows its own trait
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S122_TheJudgedOvalFollowsContactAndNotPower()
    {
        var park = _content.Parks[ExhibitionPick.DefaultPark];
        var resolver = new AtBatResolver(_content.Chemistry, rules: Rules.Default);
        var sure = Hitter(contact: 9, power: 2);
        var slugger = Hitter(contact: 2, power: 9);

        // The barrel the resolver judges with, and the one AtBatDirector draws, is Contact's.
        Assert.True(SweetSpot.BarrelScale(sure.Stats.Contact, false, false, _content.Rules)
                    > SweetSpot.BarrelScale(slugger.Stats.Contact, false, false, _content.Rules),
            "a better-Contact hitter carries the wider barrel");

        // A crossing out toward the tip that the wide barrel still squares up and the narrow one does not.
        var x = SweetSpot.TipSign(sure.Bats) * _content.Rules.Batting.Cursor.NiceTipFt;
        var sureHit = resolver.Resolve(Swing(sure, x), park, new Random(7));
        var sluggerHit = resolver.Resolve(Swing(slugger, x), park, new Random(7));
        Assert.True(sureHit.Quality > sluggerHit.Quality,
            $"the same crossing is {sureHit.Quality} on Contact 9 and {sluggerHit.Quality} on Contact 2");
        Assert.Equal(sure.Stats.Bat, slugger.Stats.Bat); // and the bar they share could not have told them apart
    }

    [Fact]
    public void S122_ExitVelocityAndLoftFollowPowerAndNotContact()
    {
        var park = _content.Parks[ExhibitionPick.DefaultPark];
        var resolver = new AtBatResolver(_content.Chemistry, rules: Rules.Default);
        var sure = Hitter(contact: 9, power: 2);
        var slugger = Hitter(contact: 2, power: 9);

        // Dead center: both square it up, so the only thing left between them is Power.
        var sureHit = resolver.Resolve(Swing(sure, 0), park, new Random(7));
        var sluggerHit = resolver.Resolve(Swing(slugger, 0), park, new Random(7));
        Assert.Equal(ContactQuality.Perfect, sureHit.Quality);
        Assert.Equal(ContactQuality.Perfect, sluggerHit.Quality);
        Assert.True(sluggerHit.ExitVeloMph > sureHit.ExitVeloMph,
            $"exit {sluggerHit.ExitVeloMph} on Power 9 vs {sureHit.ExitVeloMph} on Power 2");
        Assert.True(sluggerHit.LaunchDeg > sureHit.LaunchDeg,
            $"loft {sluggerHit.LaunchDeg} on Power 9 vs {sureHit.LaunchDeg} on Power 2");
    }

    // The window half of S-122 moved to SharedWindowScenarioTests (#844): it asserted that the
    // timing window reads Stats.Contact "until P2-b removes it", which is now a claim about the
    // shipped root only, and it sits beside the rest of the shipped-root half as
    // S124_TheShippedWindowStillFollowsContact_MovedFromS122. The σ, chase, forced-charge,
    // archetype and sac-bunt halves below are unchanged.

    [Fact]
    public void S122_TheCpuChasesOnContactAndItsTimingSigmaIsContacts()
    {
        var c = _content.Rules.Batting.Cpu;
        // Just outside the frame, inside nearFt: the §5.9 chase row.
        var near = Scenario.PitchAt(StrikeZoneGeometry.HalfWidth + c.NearFt / 2, StrikeZoneGeometry.Reference.CenterY);
        var middle = Scenario.PitchAt(0, StrikeZoneGeometry.Reference.CenterY);

        var sureChases = 0;
        var sluggerChases = 0;
        var sigmaRows = 0;
        for (var seed = 1; seed <= 200; seed++)
        {
            // The same seed gives the same draw, so the only thing that can move the answer is the trait.
            if (MatchWith(Hitter(contact: 9, power: 2), seed).CpuBatter.Swing(near).Swing) sureChases++;
            if (MatchWith(Hitter(contact: 2, power: 9), seed).CpuBatter.Swing(near).Swing) sluggerChases++;

            var sure = MatchWith(Hitter(contact: 9, power: 2), seed).CpuBatter.Swing(middle);
            var slugger = MatchWith(Hitter(contact: 2, power: 9), seed).CpuBatter.Swing(middle);
            Assert.True(sure.Swing && slugger.Swing, "the table swings at a middle-middle strike");
            if (Math.Abs(sure.TimingErrorFrames) < 1e-12) continue;
            sigmaRows++;
            // σ is (11 − Contact) × the table's frames, so the bats arrive apart by that ratio and
            // by nothing else — Power differs by 7 between these two and never enters it.
            var ratio = slugger.TimingErrorFrames / sure.TimingErrorFrames;
            Assert.Equal((11 - 2) / (double)(11 - 9), ratio, 9);
            Assert.True(Math.Abs(slugger.TimingErrorFrames) > Math.Abs(sure.TimingErrorFrames));
        }
        Assert.True(sigmaRows > 100, $"most seeds draw a non-zero timing error: {sigmaRows}");
        Assert.True(sluggerChases > sureChases,
            $"the weaker-Contact bat chases more: {sluggerChases} vs {sureChases} of 200");
        Assert.True(sureChases < 200, "and the better-Contact bat lays off at least sometimes");
    }

    [Fact]
    public void S122_TheCpuForcedChargeFollowsPower()
    {
        // Runner in scoring position, fewer than two outs, a middle-middle strike: §5.9's forced
        // charge. Its gate is rispChargeBatMin against Power, so the slugger charges on every seed
        // while the contact hitter is left to the archetype roll.
        var middle = Scenario.PitchAt(0, StrikeZoneGeometry.Reference.CenterY);
        var sluggerCharges = 0;
        var sureCharges = 0;
        for (var seed = 1; seed <= 40; seed++)
        {
            sluggerCharges += Charged(Hitter(contact: 2, power: 9), seed, middle) ? 1 : 0;
            sureCharges += Charged(Hitter(contact: 9, power: 2), seed, middle) ? 1 : 0;
        }
        Assert.Equal(40, sluggerCharges);
        Assert.True(sureCharges < 40, $"Power 2 is not forced: {sureCharges} of 40");
        // Both carry the same Bat bar (9 and 2 or 2 and 9), so the bar could not have split them.

        bool Charged(Character who, int seed, PitchCommand pitch)
        {
            var match = MatchWith(who, seed);
            Assert.True(match.StationRunner(2, match.Away.Roster[3]), "station second");
            return match.CpuBatter.Swing(pitch).Charge01 == 1.0;
        }
    }

    [Fact]
    public void S122_TheCpuArchetypeSplitIsPowersAndItsTechniqueGateIsContacts()
    {
        var a = _content.Rules.Batting.Cpu.Archetype;
        // The split is Power against Run: the same Contact, the same Bat, opposite answers.
        Assert.Equal(a.Power, CpuBatter.ChargeChance(Hitter(contact: 5, power: 9, run: 2), a));
        Assert.Equal(a.Speed, CpuBatter.ChargeChance(Hitter(contact: 5, power: 2, run: 9), a));
        // Swapping Contact alone cannot move the split.
        Assert.Equal(a.Power, CpuBatter.ChargeChance(Hitter(contact: 2, power: 9, run: 2), a));
        Assert.Equal(a.Power, CpuBatter.ChargeChance(Hitter(contact: 9, power: 9, run: 2), a));

        // The technique gate is Contact and Run: Contact 9 / Run 9 slaps, and the Power 9 twin does not.
        Assert.Equal(a.Technique, CpuBatter.ChargeChance(Hitter(contact: 9, power: 2, run: 9), a));
        Assert.Equal(a.Balanced, CpuBatter.ChargeChance(Hitter(contact: 2, power: 9, run: 9), a));
    }

    [Fact]
    public void S122_TheCpuSacBuntGateFollowsContact()
    {
        // Runner on first, no outs, a close game (§5.9). The gate is sacBuntBatMax against Contact.
        var lightSquares = 0;
        var sureSquares = 0;
        for (var seed = 1; seed <= 60; seed++)
        {
            lightSquares += Squares(Hitter(contact: 2, power: 9), seed) ? 1 : 0;
            sureSquares += Squares(Hitter(contact: 9, power: 2), seed) ? 1 : 0;
        }
        Assert.True(lightSquares > 0, "a weak-Contact hitter still gives himself up");
        // Both carry the same Bat bar: only the trait can tell them apart.
        Assert.Equal(0, sureSquares);

        bool Squares(Character who, int seed)
        {
            var match = MatchWith(who, seed);
            Assert.True(match.StationRunner(1, match.Away.Roster[3]), "station first");
            return match.CpuBatter.SquaresBunt();
        }
    }

    // ---------------------------------------------------------------------------------
    // S-123 — the validator
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("contact")]
    [InlineData("power")]
    public void S123_AnAbsentKeyIsRefusedByCharacterAndField(string field)
    {
        using var fixture = new ContentFixture();
        Assert.Empty(ContentDataValidator.Validate(fixture.Root));
        fixture.ChangeObject("characters/rio.json", json => json.Remove(field));
        var error = Assert.Single(ContentDataValidator.Validate(fixture.Root));
        Assert.Equal($"{fixture.Path("characters/rio.json")}: character 'rio' {field} is required (1–10); every character authors all nine sub-stats",
            error);
    }

    [Theory]
    [InlineData("contact", 11)]
    [InlineData("contact", 0)]
    [InlineData("contact", -1)]
    [InlineData("power", 11)]
    [InlineData("power", -1)]
    public void S123_AnOutOfRangeRatingIsRefusedByCharacterAndField(string field, int value)
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/rio.json", json => json[field] = value);

        var error = Assert.Single(ContentDataValidator.Validate(fixture.Root));
        Assert.Equal($"{fixture.Path("characters/rio.json")}: character 'rio' {field} must be between 1 and 10; got {value}",
            error);
        Assert.Contains(error, Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root)).Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void S123_AnInRangeRatingLoads(int value)
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/rio.json", json =>
        {
            json["contact"] = value;
            json["power"] = 11 - value;
        });
        Assert.Empty(ContentDataValidator.Validate(fixture.Root));

        var rio = ContentCatalog.Load(fixture.Root).Must("rio");
        Assert.Equal((value, 11 - value), (rio.Stats.Contact, rio.Stats.Power));
    }

    // ---------------------------------------------------------------------------------

    /// <summary>
    /// A hitter authored in this fixture and nowhere else. Contact 9 / Power 2 and Contact 2 / Power 9
    /// share one Bat bar, so a read that still took the bar could not tell them apart.
    /// </summary>
    Character Hitter(int contact, int power, int run = 5)
    {
        var who = _content.Must("pip");
        Assert.False(who.Captain, "the fixture hitter must not arm a star swing");
        return who with
        {
            Stats = who.Stats with { Contact = contact, Power = power, Run = run }
        };
    }

    AtBatInput Swing(Character batter, double crossingX) => new(
        _content.Must("vale"), batter, null, [],
        ChargePitch: false, ChangeupPitch: false, TimingErrorFrames: 0,
        UseStarPitch: false, UseStarSwing: false, Bat: null, PitcherStamina: 100,
        CrossingX: crossingX);

    /// <summary>A match whose first batter is the fixture hitter, on the away side (the CPU's half).</summary>
    Match MatchWith(Character batter, int seed)
    {
        var home = _content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var captain = _content.Must("zig");
        var roster = new List<Character>
        {
            captain, batter,
            _content.Must("dart"), _content.Must("jester"), _content.Must("cinder"),
            _content.Must("grit"), _content.Must("soot"), _content.Must("boom"), _content.Must("nugget")
        };
        var match = Match.Exhibition(_content, home, new Team("Offense", captain, roster), 3, seed);
        Assert.True(match.Top, "the away side bats first");
        Assert.Equal(batter.Stats, match.Batter.Stats);
        return match;
    }
}
