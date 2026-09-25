using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The four bars from nine sub-stats (CH-07, CH-08, CH-09), scenarios SC-01 … SC-04.
///
/// <b>SC-01</b>: every character file authors the nine sub-stats, and a file that authors a bar is
/// refused. <b>SC-02</b>: at most one derived bar at 9 or higher per character, and the refusal names
/// the character. <b>SC-03</b>: Pitch power (Velocity) moves the fastball and never a fielding throw.
/// <b>SC-04</b>: Field throw speed (Arm) moves a throw's arrival and never a fastball.
///
/// The values are balance: every row asserts a relationship, never a shipped number.
/// </summary>
public class StatBarsScenarioTests
{
    static readonly string[] SubStatKeys =
        ["contact", "power", "velocity", "endurance", "control", "movement", "hands", "arm", "run"];

    static readonly string[] BarKeys = ["pitch", "bat", "field"];

    readonly ContentCatalog _content = Shipped.Content;

    // ---------------------------------------------------------------------------------
    // SC-01 — nine sub-stats in every file; a bar in a file is refused
    // ---------------------------------------------------------------------------------

    [Fact]
    public void SC01_EveryCharacterFileAuthorsTheNineSubStatsAndNoBar()
    {
        var root = Shipped.Content.Root.Shipped;
        var rows = 0;
        foreach (var file in Directory.GetFiles(Path.Combine(root, "characters"), "*.json"))
        {
            var node = JsonNode.Parse(File.ReadAllText(file), documentOptions: DataJson.Document)!;
            foreach (var row in node is JsonArray array ? array : [node])
            {
                var o = row!.AsObject();
                rows++;
                foreach (var key in SubStatKeys)
                {
                    Assert.True(o.ContainsKey(key), $"{file} {o["id"]} does not author {key}");
                    Assert.InRange(o[key]!.GetValue<int>(), 1, 10);
                }
                foreach (var key in BarKeys)
                    Assert.False(o.ContainsKey(key), $"{file} {o["id"]} authors the {key} bar");
            }
        }
        Assert.Equal(_content.Characters.Count, rows);
    }

    [Fact]
    public void SC01_EveryBarIsTheRoundedMeanOfItsSubStats()
    {
        foreach (var c in _content.Characters.Values)
        {
            var s = c.Stats;
            Assert.Equal(Stats.Bar(s.Contact + s.Power, 2), s.Bat);
            Assert.Equal(Stats.Bar(s.Velocity + s.Endurance + s.Control + s.Movement, 4), s.Pitch);
            Assert.Equal(Stats.Bar(s.Hands + s.Arm, 2), s.Field);
        }

        // The one rounding rule: the mean, rounded half up.
        Assert.Equal(7, Stats.Bar(13, 2)); // 6.5
        Assert.Equal(6, Stats.Bar(12, 2)); // 6.0
        Assert.Equal(6, Stats.Bar(22, 4)); // 5.5
        Assert.Equal(5, Stats.Bar(21, 4)); // 5.25
        Assert.Equal(6, Stats.Bar(23, 4)); // 5.75
        Assert.Equal(1, Stats.Bar(4, 4));
        Assert.Equal(10, Stats.Bar(40, 4));
    }

    [Theory]
    [InlineData("pitch", "velocity, endurance, control and movement")]
    [InlineData("bat", "contact and power")]
    [InlineData("field", "hands and arm")]
    public void SC01_AFileThatAuthorsABarIsRefused(string bar, string instead)
    {
        using var fixture = new ContentFixture();
        Assert.Empty(ContentDataValidator.Validate(fixture.Root));
        fixture.ChangeObject("characters/rio.json", json => json[bar] = 6);

        var error = Assert.Single(ContentDataValidator.Validate(fixture.Root));
        Assert.Equal($"{fixture.Path("characters/rio.json")}: character 'rio' authors the '{bar}' bar; "
                     + $"bars are derived from sub-stats (spec §2), so author {instead} instead", error);
        Assert.Contains(error, Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root)).Message);
    }

    [Theory]
    [InlineData("contact")]
    [InlineData("velocity")]
    [InlineData("hands")]
    [InlineData("arm")]
    [InlineData("run")]
    public void SC01_AFileThatMissesASubStatIsRefused(string key)
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/rio.json", json => json.Remove(key));

        var error = Assert.Single(ContentDataValidator.Validate(fixture.Root));
        Assert.Equal($"{fixture.Path("characters/rio.json")}: character 'rio' {key} is required (1–10); every character authors all nine sub-stats",
            error);
    }

    // ---------------------------------------------------------------------------------
    // SC-02 — at most one bar at 9 or higher, by name
    // ---------------------------------------------------------------------------------

    [Fact]
    public void SC02_TheShippedRosterKeepsTheStatBudget()
    {
        foreach (var c in _content.Characters.Values)
        {
            var s = c.Stats;
            var stars = new[] { s.Bat, s.Pitch, s.Field, s.Run }.Count(b => b >= Stats.StarBar);
            Assert.True(stars <= Stats.MaxStarBars, $"{c.Id} has {stars} bars at {Stats.StarBar} or higher");
        }
    }

    [Fact]
    public void SC02_OneBarAtTenIsAllowed()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/rio.json", json =>
        {
            json["contact"] = 10;
            json["power"] = 10;
        });
        Assert.Empty(ContentDataValidator.Validate(fixture.Root));
        Assert.Equal(10, ContentCatalog.Load(fixture.Root).Must("rio").Stats.Bat);
    }

    [Fact]
    public void SC02_TwoStarBarsAreRefusedByCharacterName()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/rio.json", json =>
        {
            json["contact"] = 9;
            json["power"] = 9;
            json["run"] = 10;
        });

        var error = Assert.Single(ContentDataValidator.Validate(fixture.Root));
        Assert.Equal($"{fixture.Path("characters/rio.json")}: character 'rio' has 2 bars at 9 or higher (Bat 9, Run 10); "
                     + "at most 1 may be (CH-08)", error);
        Assert.Contains(error, Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root)).Message);
    }

    [Fact]
    public void SC02_TheCapReadsTheDerivedBarNotASubStat()
    {
        // Two 10s inside the Pitch group round to a Pitch bar of 8 (7.5 → 8): no star bar, however high one
        // sub-stat reaches. A star Bat beside it is still the character's only one.
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/rio.json", json =>
        {
            json["velocity"] = 10;
            json["control"] = 10;
            json["endurance"] = 5;
            json["movement"] = 5;
            json["contact"] = 9;
            json["power"] = 9;
        });
        Assert.Empty(ContentDataValidator.Validate(fixture.Root));
        var s = ContentCatalog.Load(fixture.Root).Must("rio").Stats;
        Assert.Equal((8, 9), (s.Pitch, s.Bat));
    }

    // ---------------------------------------------------------------------------------
    // SC-03 / SC-04 — pitch power and throw speed are separate verbs
    // ---------------------------------------------------------------------------------

    /// <summary>The two reads a pitch-power or throw-speed change could reach, off one body.</summary>
    readonly record struct Reads(double FastballMph, double ThrowArrivalSec, double ThrowReactionSec);

    Reads ReadsOf(Character who)
    {
        var rules = _content.Rules;
        var fastball = new PitchCommand(PitchFamily.Fastball, 0, false);
        var mph = AtBatResolver.PitchSpeedMph(fastball, who, rules);

        // A shortstop's throw to first, through the same chain a live play builds (§8.5).
        var receiver = _content.Must("nico");
        var thrown = FieldAbilities.ApplyThrow(who, _content.Chemistry.FieldingThrow(who, receiver, new Random(7)), rules);
        var from = Diamond.Positions["SS"];
        var arrival = InPlay.ThrowArrivalSec(from.X, from.Z, 1, thrown, rules);
        return new Reads(mph, arrival, InPlay.ThrowReactionSec(who, rules));
    }

    Character Body(int velocity = 5, int arm = 5)
    {
        var rio = _content.Must("rio");
        return rio with { Stats = Stats.Even(5, 5, 5, 5) with { Velocity = velocity, Arm = arm }, FieldAbility = "" };
    }

    [Fact]
    public void SC03_PitchPowerMovesTheFastballAndNotAFieldingThrow()
    {
        var soft = ReadsOf(Body(velocity: 3));
        var hard = ReadsOf(Body(velocity: 9));

        Assert.True(hard.FastballMph > soft.FastballMph, $"fastball {hard.FastballMph} at 9 vs {soft.FastballMph} at 3");
        Assert.Equal(soft.ThrowArrivalSec, hard.ThrowArrivalSec);
        Assert.Equal(soft.ThrowReactionSec, hard.ThrowReactionSec);
    }

    [Fact]
    public void SC04_ThrowSpeedMovesTheThrowAndNotTheFastball()
    {
        var weak = ReadsOf(Body(arm: 3));
        var strong = ReadsOf(Body(arm: 9));

        Assert.True(strong.ThrowArrivalSec < weak.ThrowArrivalSec,
            $"throw arrives at {strong.ThrowArrivalSec} s on Arm 9 vs {weak.ThrowArrivalSec} s on Arm 3");
        Assert.Equal(weak.FastballMph, strong.FastballMph);
        // The transfer is Hands', so a stronger arm does not get rid of the ball sooner either.
        Assert.Equal(weak.ThrowReactionSec, strong.ThrowReactionSec);
    }
}
