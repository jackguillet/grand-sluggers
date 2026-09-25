using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The pitching split (#890, PH-15-R6), Appendix B.1 rows S-138 … S-140.
///
/// <b>S-138</b>: every character authors Velocity (pitch power), Endurance (stamina), Control and
/// Movement (break), and the Pitch bar is their rounded mean (CH-07). <b>S-139</b>: each rating
/// moves its own read and nobody else's — speed is Velocity's, the arm's say over
/// non-perfect contact is Movement's, the steer rate and the CPU's scatter are Control's, the stamina
/// pool is Endurance's — shown on a fixture arm that moves one rating, beside one that does not.
/// <b>S-140</b>: the validator requires the four keys and refuses them outside 1–10 by character and
/// field name.
///
/// Every row asserts a relationship or an integer identity, never a stored libm double: the values
/// are balance and must be able to move without editing a test.
/// </summary>
public class PitchRatingsScenarioTests
{
    static readonly string[] Keys = ["velocity", "movement", "control", "endurance"];

    readonly ContentCatalog _content = Shipped.Content;

    // ---------------------------------------------------------------------------------
    // S-138 — every character authors the four, and the Pitch bar is their rounded mean
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S138_EveryShippedCharacterDerivesPitchFromItsFourRatings()
    {
        Assert.NotEmpty(_content.Characters);
        foreach (var c in _content.Characters.Values)
        {
            var s = c.Stats;
            Assert.Equal(Stats.Bar(s.Velocity + s.Endurance + s.Control + s.Movement, 4), s.Pitch);
        }
    }

    [Fact]
    public void S138_EveryCharacterFileAuthorsTheFourRatingKeys()
    {
        var root = Shipped.Content.Root.Shipped;
        foreach (var file in Directory.GetFiles(Path.Combine(root, "characters"), "*.json"))
        {
            var node = JsonNode.Parse(File.ReadAllText(file), documentOptions: DataJson.Document)!;
            foreach (var row in node is JsonArray rows ? rows : [node])
                foreach (var key in Keys)
                    Assert.True(row!.AsObject().ContainsKey(key), $"{file} does not author {key}");
        }
    }

    [Fact]
    public void S138_ThePitchBarRoundsHalfUpAndAClampHoldsEachRating()
    {
        Assert.Equal(5, (Stats.Even(5, 5, 5, 5) with { Velocity = 7, Movement = 4, Control = 5, Endurance = 4 }).Pitch); // 5.0
        Assert.Equal(6, (Stats.Even(5, 5, 5, 5) with { Velocity = 7, Movement = 5, Control = 5, Endurance = 5 }).Pitch); // 5.5
        Assert.Equal(5, (Stats.Even(5, 5, 5, 5) with { Velocity = 6, Movement = 5, Control = 5, Endurance = 5 }).Pitch); // 5.25

        var clamped = (Stats.Even(5, 5, 5, 5) with { Velocity = 99, Movement = -3 }).Clamp();
        Assert.Equal((10, 1, 5, 5), (clamped.Velocity, clamped.Movement, clamped.Control, clamped.Endurance));
        Assert.Equal(5, clamped.Pitch); // 21 / 4 = 5.25
    }

    [Fact]
    public void S138_StatsRoundTripsThroughJson()
    {
        foreach (var stats in new[]
                 {
                     Stats.Even(3, 7, 4, 6),
                     (Stats.Even(3, 7, 4, 6) with { Velocity = 9, Movement = 2, Control = 8, Endurance = 1 }),
                     (Stats.Even(3, 7, 4, 6) with { Arm = 8, Contact = 2, Control = 9 })
                 })
        {
            var json = JsonSerializer.Serialize(stats, PlayTrace.Json);
            foreach (var key in Keys) Assert.Contains($"\"{key}\":", json);

            var back = JsonSerializer.Deserialize<Stats>(json, PlayTrace.Json)!;
            Assert.Equal((stats.Velocity, stats.Movement, stats.Control, stats.Endurance),
                (back.Velocity, back.Movement, back.Control, back.Endurance));
            Assert.Equal(json, JsonSerializer.Serialize(back, PlayTrace.Json));
        }
    }

    // ---------------------------------------------------------------------------------
    // S-139 — each rating moves its own read and nobody else's
    // ---------------------------------------------------------------------------------

    /// <summary>The value every fixture arm's ratings start from; each row moves one.</summary>
    const int SharedPitch = 5;

    /// <summary>Every read a pitching rating owns, taken off one arm on one seed.</summary>
    readonly record struct Reads(double Mph, int Pool, double SteerStep, double CpuIntentX, double NiceExitMph);

    Reads ReadsOf(Character arm, int seed = 7)
    {
        var rules = _content.Rules;
        var fastball = new PitchCommand(PitchFamily.Fastball, 0, false);

        var match = MatchAgainst(arm, seed);
        Assert.Equal(arm.Stats, match.Pitcher.Stats);
        var mph = match.PitchSpeedMph(fastball);
        var pool = match.PitcherStaminaMax;
        match.CpuPitcher.PitchByInputs(out var plan);

        // One frame of a hand's held stick, from the client's own call shape (AtBatDirector passes the
        // pitcher's Control).
        var step = PitchFlight.BreakStep(0, 1, 1 / 60.0, match.Pitcher.Stats.Control, rules);

        // A Nice crossing: the arm's say over non-perfect contact is the one term left between arms.
        var resolver = new AtBatResolver(_content.Chemistry, rules: Rules.Default);
        var batter = _content.Must("pip");
        var x = SweetSpot.TipSign(batter.Bats) * rules.Batting.Cursor.NiceTipFt * 0.8;
        var hit = resolver.Resolve(new AtBatInput(
            arm, batter, null, [],
            ChargePitch: false, ChangeupPitch: false, TimingErrorFrames: 0,
            UseStarPitch: false, UseStarSwing: false, Bat: null, PitcherStamina: 100,
            CrossingX: x, CrossingY: StrikeZoneGeometry.CenterY), _content.Parks[ExhibitionPick.DefaultPark], new Random(seed));
        Assert.Equal(ContactQuality.Nice, hit.Quality);

        return new Reads(mph, pool, step, plan.IntentX, hit.ExitVeloMph);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void S139_EachRatingMovesOnlyItsOwnRead(int authored)
    {
        var base_ = ReadsOf(Arm(Stats.Even(SharedPitch, 5, 5, 5)));

        var velocity = ReadsOf(Arm((Stats.Even(SharedPitch, 5, 5, 5) with { Velocity = authored })));
        Assert.NotEqual(base_.Mph, velocity.Mph);
        Assert.Equal(authored > SharedPitch, velocity.Mph > base_.Mph);
        Assert.Equal(base_ with { Mph = velocity.Mph }, velocity);

        var movement = ReadsOf(Arm((Stats.Even(SharedPitch, 5, 5, 5) with { Movement = authored })));
        if (authored > SharedPitch)
        {
            // pitchFactor damps only above 5: a better-Movement arm takes speed off a Nice ball.
            Assert.True(movement.NiceExitMph < base_.NiceExitMph,
                $"Nice exit {movement.NiceExitMph} at Movement {authored} vs {base_.NiceExitMph} at {SharedPitch}");
            Assert.Equal(base_ with { NiceExitMph = movement.NiceExitMph }, movement);
        }
        else
        {
            Assert.Equal(base_, movement); // at or below 5 the damp is zero, as it was for Pitch
        }

        var control = ReadsOf(Arm((Stats.Even(SharedPitch, 5, 5, 5) with { Control = authored })));
        Assert.Equal(authored > SharedPitch, control.SteerStep > base_.SteerStep);
        Assert.NotEqual(base_.SteerStep, control.SteerStep);
        Assert.NotEqual(base_.CpuIntentX, control.CpuIntentX);
        Assert.Equal(base_ with { SteerStep = control.SteerStep, CpuIntentX = control.CpuIntentX }, control);

        var endurance = ReadsOf(Arm((Stats.Even(SharedPitch, 5, 5, 5) with { Endurance = authored })));
        var st = _content.Rules.Pitching.Stamina;
        Assert.Equal(st.PoolBase + authored * st.PoolPerPitch, endurance.Pool);
        Assert.Equal(base_ with { Pool = endurance.Pool }, endurance);
    }

    [Fact]
    public void S139_TheCpuScatterIsControlsAndScalesWithElevenLessControl()
    {
        // σ = (11 − Control) × scatterFtPerPitchStat on the same Gaussian, so on one seed the intent
        // moves off the unscattered one in proportion to (11 − Control) and nothing else.
        for (var seed = 1; seed <= 40; seed++)
        {
            var i1 = ReadsOf(Arm((Stats.Even(SharedPitch, 5, 5, 5) with { Control = 1 })), seed).CpuIntentX;
            var i6 = ReadsOf(Arm((Stats.Even(SharedPitch, 5, 5, 5) with { Control = 6 })), seed).CpuIntentX;
            var i10 = ReadsOf(Arm((Stats.Even(SharedPitch, 5, 5, 5) with { Control = 10 })), seed).CpuIntentX;
            if (Math.Abs(i1 - i10) < 1e-12) continue;
            // (10 − 5) / (5 − 1) = (i1 − i6) / (i6 − i10)
            Assert.Equal(5 / 4.0, (i1 - i6) / (i6 - i10), 9);
        }
    }

    [Fact]
    public void S139_ThePitchAggregateStillPicksTheSwapArm()
    {
        // The swap pick is a selection by the displayed bar, not a rating's read (§4.7): an arm with
        // the better Pitch bar is picked over one with the harder fastball.
        var strong = Arm(Stats.Even(8, 5, 5, 5), "pewter");
        var rated = Arm((Stats.Even(4, 5, 5, 5) with { Velocity = 10 }), "lace");
        Assert.True(rated.Stats.Velocity > strong.Stats.Velocity && rated.Stats.Pitch < strong.Stats.Pitch);
        var match = MatchAgainst(Arm(Stats.Even(1, 5, 5, 5)), 7, strong, rated);
        Assert.True(match.SwapPitcher());
        Assert.Equal(strong.Id, match.Pitcher.Id);
    }

    // ---------------------------------------------------------------------------------
    // S-140 — the validator
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("velocity")]
    [InlineData("movement")]
    [InlineData("control")]
    [InlineData("endurance")]
    public void S140_AnAbsentKeyIsRefusedByCharacterAndField(string field)
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/rio.json", json => json.Remove(field));
        var error = Assert.Single(ContentDataValidator.Validate(fixture.Root));
        Assert.Equal($"{fixture.Path("characters/rio.json")}: character 'rio' {field} is required (1–10); every character authors all nine sub-stats",
            error);
    }

    [Theory]
    [InlineData("velocity", 11)]
    [InlineData("velocity", 0)]
    [InlineData("velocity", -1)]
    [InlineData("movement", 11)]
    [InlineData("movement", -1)]
    [InlineData("control", 11)]
    [InlineData("control", -1)]
    [InlineData("endurance", 11)]
    [InlineData("endurance", -1)]
    public void S140_AnOutOfRangeRatingIsRefusedByCharacterAndField(string field, int value)
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
    public void S140_AnInRangeRatingLoads(int value)
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/rio.json", json =>
        {
            json["velocity"] = value;
            json["movement"] = 11 - value;
            json["control"] = value;
            json["endurance"] = 11 - value;
        });
        Assert.Empty(ContentDataValidator.Validate(fixture.Root));

        var s = ContentCatalog.Load(fixture.Root).Must("rio").Stats;
        Assert.Equal((value, 11 - value, value, 11 - value), (s.Velocity, s.Movement, s.Control, s.Endurance));
    }

    // ---------------------------------------------------------------------------------

    /// <summary>An arm authored in this fixture and nowhere else.</summary>
    Character Arm(Stats stats, string id = "vale") => _content.Must(id) with { Stats = stats };

    /// <summary>A match whose home arm is <paramref name="arm"/>, pitching to the away side in the top.</summary>
    Match MatchAgainst(Character arm, int seed, params Character[] gloves)
    {
        var ids = new[] { "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex" };
        var roster = new List<Character> { arm };
        foreach (var id in ids)
            roster.Add(gloves.FirstOrDefault(g => g.Id == id) ?? _content.Must(id));
        var home = new Team("Defense", arm, roster, Starter: arm);
        var away = _content.Team("Offense", "zig", "pip", "dart", "jester", "cinder", "grit", "soot", "boom", "nugget");
        var match = Match.Exhibition(_content, home, away, 3, seed);
        Assert.True(match.Top, "the away side bats first");
        return match;
    }
}
