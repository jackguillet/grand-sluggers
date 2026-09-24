using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The Contact / Power split (#837, PH-15-R5), Appendix B.1 rows S-121 … S-123.
///
/// Two rules. <b>S-121</b>: an <b>unauthored</b> roster behaves exactly as it did before the split,
/// because every trait tracks <c>Bat</c>. <b>S-122</b>: once a trait is authored it moves its own
/// consumers and nobody else's — the cursor is Contact's, the exit and the loft are Power's, and each
/// CPU read takes the trait §5.9 names. <b>S-123</b>: the validator accepts the keys as optional and
/// refuses them outside 1–10 by character and field name.
///
/// Every row asserts a relationship or an integer identity, never a stored libm double: the values
/// are Jack's to author later (PH-15-R5 leaves them open) and must be able to move without editing a
/// test. No data file authors a value in this child.
/// </summary>
public class ContactPowerScenarioTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    // ---------------------------------------------------------------------------------
    // S-121 — unauthored tracks Bat, and stays unauthored
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S121_EveryShippedCharacterIsStillSeededFromBat()
    {
        // The migration authors no ratings, so the whole roster must still track Bat. This holds on
        // whichever data root the process plays (the trial overlays carry the same character rows).
        Assert.NotEmpty(_content.Characters);
        foreach (var c in _content.Characters.Values)
        {
            Assert.Equal(c.Stats.Bat, c.Stats.Contact);
            Assert.Equal(c.Stats.Bat, c.Stats.Power);
            Assert.False(c.Stats.ContactAuthored, $"{c.Id} authors contact; P2-a authors none");
            Assert.False(c.Stats.PowerAuthored, $"{c.Id} authors power; P2-a authors none");
        }
    }

    [Fact]
    public void S121_NoCharacterFileAuthorsAContactOrPowerKey()
    {
        // Belt and braces for the row above: a key spelled in a file would be an authored value
        // whoever read it, and authoring one is banned in this child.
        var root = ContentCatalog.Load().Root.Shipped;
        foreach (var file in Directory.GetFiles(Path.Combine(root, "characters"), "*.json"))
        {
            var node = JsonNode.Parse(File.ReadAllText(file))!;
            foreach (var row in node is JsonArray rows ? rows : [node])
            {
                var o = row!.AsObject();
                Assert.False(o.ContainsKey("contact"), $"{file} authors contact");
                Assert.False(o.ContainsKey("power"), $"{file} authors power");
            }
        }
    }

    [Fact]
    public void S121_AnUnauthoredTraitTracksBatAtEveryRating()
    {
        for (var bat = 1; bat <= 10; bat++)
        {
            var stats = new Stats(5, bat, 5, 5);
            Assert.Equal(bat, stats.Contact);
            Assert.Equal(bat, stats.Power);
            Assert.False(stats.ContactAuthored);
            Assert.False(stats.PowerAuthored);
        }

        // Authored is the number itself, and says so.
        var authored = new Stats(5, 5, 5, 5) { Contact = 9, Power = 2 };
        Assert.Equal((9, 2), (authored.Contact, authored.Power));
        Assert.True(authored.ContactAuthored);
        Assert.True(authored.PowerAuthored);
        // One authored trait does not author the other.
        var half = new Stats(5, 4, 5, 5) { Contact = 9 };
        Assert.Equal((9, 4), (half.Contact, half.Power));
        Assert.True(half.ContactAuthored);
        Assert.False(half.PowerAuthored);
    }

    [Fact]
    public void S121_AnUnauthoredTraitKeepsTrackingBatThroughAClamp()
    {
        var clamped = new Stats(5, 99, 5, 5).Clamp();
        Assert.Equal(10, clamped.Bat);
        Assert.Equal(10, clamped.Contact);
        Assert.Equal(10, clamped.Power);
        Assert.False(clamped.ContactAuthored);
        Assert.False(clamped.PowerAuthored);

        var authored = new Stats(5, 5, 5, 5) { Contact = 99, Power = -3 }.Clamp();
        Assert.Equal(10, authored.Contact);
        Assert.Equal(5, authored.Power); // -3 is not an authored value: it was never > 0, so it tracks Bat
        Assert.True(authored.ContactAuthored);
        Assert.False(authored.PowerAuthored);
    }

    [Fact]
    public void S121_StatsRoundTripsThroughJsonWithoutTheAuthoredFlags()
    {
        // The flags are bookkeeping about where a number came from, not ratings: emitting them would
        // make Stats unstable across a round trip, because a serialized rating reloads through the
        // init setter and marks the trait authored (the Arm rule, #710). What has to be stable is
        // the serialized form PlayTraceIdentity hashes.
        foreach (var stats in new[]
                 {
                     new Stats(3, 7, 4, 6),
                     new Stats(3, 7, 4, 6) { Contact = 9, Power = 2 },
                     new Stats(3, 7, 4, 6) { Arm = 8, Hands = 2, Contact = 2, Power = 9 }
                 })
        {
            var json = JsonSerializer.Serialize(stats, PlayTrace.Json);
            Assert.DoesNotContain("Authored", json, StringComparison.OrdinalIgnoreCase);
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
            Assert.Equal(before, Match.CpuChargeChance(c, a));
        }
    }

    // ---------------------------------------------------------------------------------
    // S-122 — authored in a fixture: each consumer follows its own trait
    // ---------------------------------------------------------------------------------

    /// <summary>Same <c>Bat</c>, opposite traits. Nothing but the fixture ever holds these.</summary>
    const int SharedBat = 5;

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
        Assert.Equal(SharedBat, sure.Stats.Bat); // and the aggregate they share could not have told them apart
        Assert.Equal(SharedBat, slugger.Stats.Bat);
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
        var near = Scenario.PitchAt(StrikeZoneGeometry.HalfWidth + c.NearFt / 2, StrikeZoneGeometry.CenterY);
        var middle = Scenario.PitchAt(0, StrikeZoneGeometry.CenterY);

        var sureChases = 0;
        var sluggerChases = 0;
        var sigmaRows = 0;
        for (var seed = 1; seed <= 200; seed++)
        {
            // The same seed gives the same draw, so the only thing that can move the answer is the trait.
            if (MatchWith(Hitter(contact: 9, power: 2), seed).CpuSwing(near).Swing) sureChases++;
            if (MatchWith(Hitter(contact: 2, power: 9), seed).CpuSwing(near).Swing) sluggerChases++;

            var sure = MatchWith(Hitter(contact: 9, power: 2), seed).CpuSwing(middle);
            var slugger = MatchWith(Hitter(contact: 2, power: 9), seed).CpuSwing(middle);
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
        var middle = Scenario.PitchAt(0, StrikeZoneGeometry.CenterY);
        var sluggerCharges = 0;
        var sureCharges = 0;
        for (var seed = 1; seed <= 40; seed++)
        {
            sluggerCharges += Charged(Hitter(contact: 2, power: 9), seed, middle) ? 1 : 0;
            sureCharges += Charged(Hitter(contact: 9, power: 2), seed, middle) ? 1 : 0;
        }
        Assert.Equal(40, sluggerCharges);
        Assert.True(sureCharges < 40, $"Power 2 is not forced: {sureCharges} of 40");
        Assert.True(_content.Rules.Batting.Cpu.RispChargeBatMin > SharedBat,
            "the shared Bat could not have forced either of them");

        bool Charged(Character who, int seed, PitchCommand pitch)
        {
            var match = MatchWith(who, seed);
            Assert.True(match.StationRunner(2, match.Away.Roster[3]), "station second");
            return match.CpuSwing(pitch).Charge01 == 1.0;
        }
    }

    [Fact]
    public void S122_TheCpuArchetypeSplitIsPowersAndItsTechniqueGateIsContacts()
    {
        var a = _content.Rules.Batting.Cpu.Archetype;
        // The split is Power against Run: the same Contact, the same Bat, opposite answers.
        Assert.Equal(a.Power, Match.CpuChargeChance(Hitter(contact: 5, power: 9, run: 2), a));
        Assert.Equal(a.Speed, Match.CpuChargeChance(Hitter(contact: 5, power: 2, run: 9), a));
        // Swapping Contact alone cannot move the split.
        Assert.Equal(a.Power, Match.CpuChargeChance(Hitter(contact: 2, power: 9, run: 2), a));
        Assert.Equal(a.Power, Match.CpuChargeChance(Hitter(contact: 9, power: 9, run: 2), a));

        // The technique gate is Contact and Run: Contact 9 / Run 9 slaps, and the Power 9 twin does not.
        Assert.Equal(a.Technique, Match.CpuChargeChance(Hitter(contact: 9, power: 2, run: 9), a));
        Assert.Equal(a.Balanced, Match.CpuChargeChance(Hitter(contact: 2, power: 9, run: 9), a));
        Assert.True(a.TechniqueMin > SharedBat, "the shared Bat could not have reached the gate");
    }

    [Fact]
    public void S122_TheCpuSacBuntGateFollowsContact()
    {
        // Runner on first, no outs, a close game (§5.9). The gate is sacBuntBatMax against Contact.
        Assert.Equal(SharedBat, _content.Rules.Batting.Cpu.SacBuntBatMax);
        var lightSquares = 0;
        var sureSquares = 0;
        for (var seed = 1; seed <= 60; seed++)
        {
            lightSquares += Squares(Hitter(contact: 2, power: 9), seed) ? 1 : 0;
            sureSquares += Squares(Hitter(contact: 9, power: 2), seed) ? 1 : 0;
        }
        Assert.True(lightSquares > 0, "a weak-Contact hitter still gives himself up");
        // Both share Bat 5, which is the gate's own maximum: only the trait can tell them apart.
        Assert.Equal(0, sureSquares);

        bool Squares(Character who, int seed)
        {
            var match = MatchWith(who, seed);
            Assert.True(match.StationRunner(1, match.Away.Roster[3]), "station first");
            return match.CpuSquaresBunt();
        }
    }

    // ---------------------------------------------------------------------------------
    // S-123 — the validator
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("contact")]
    [InlineData("power")]
    public void S123_AnAbsentOrZeroKeyIsUnauthored(string field)
    {
        using var fixture = new ContentFixture();
        Assert.Empty(ContentDataValidator.Validate(fixture.Root)); // absent on every shipped row

        fixture.ChangeObject("characters/rio.json", json => json[field] = 0);
        Assert.Empty(ContentDataValidator.Validate(fixture.Root));
        var rio = ContentCatalog.Load(fixture.Root).Must("rio");
        Assert.Equal(rio.Stats.Bat, rio.Stats.Contact);
        Assert.Equal(rio.Stats.Bat, rio.Stats.Power);
        Assert.False(rio.Stats.ContactAuthored);
        Assert.False(rio.Stats.PowerAuthored);
    }

    [Theory]
    [InlineData("contact", 11)]
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
    public void S123_AnInRangeRatingLoadsAsAuthored(int value)
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
        Assert.True(rio.Stats.ContactAuthored);
        Assert.True(rio.Stats.PowerAuthored);
    }

    [Fact]
    public void S123_BatIsStillRequired()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/rio.json", json =>
        {
            json.Remove("bat");
            json["contact"] = 7;
            json["power"] = 7;
        });

        // The traits are not a substitute for the aggregate: the card and Teams.Tools still read it.
        var error = Assert.Single(ContentDataValidator.Validate(fixture.Root));
        Assert.Equal($"{fixture.Path("characters/rio.json")}: character 'rio' bat must be between 1 and 10; got 0", error);
    }

    // ---------------------------------------------------------------------------------

    /// <summary>
    /// A hitter authored in this fixture and nowhere else: the same <see cref="SharedBat"/> every
    /// time, so a read that still took the aggregate could not tell two of them apart.
    /// </summary>
    Character Hitter(int contact, int power, int run = 5)
    {
        var who = _content.Must("pip");
        Assert.False(who.Captain, "the fixture hitter must not arm a star swing");
        return who with
        {
            Stats = new Stats(who.Stats.Pitch, SharedBat, who.Stats.Field, run) { Contact = contact, Power = power }
        };
    }

    AtBatInput Swing(Character batter, double crossingX) => new(
        _content.Must("vale"), batter, null, [],
        ChargePitch: false, ChangeupPitch: false, TimingErrorFrames: 0,
        UseStarPitch: false, UseStarSwing: false, Bat: null, PitcherStamina: 100,
        CrossingX: crossingX, CrossingY: StrikeZoneGeometry.CenterY);

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
