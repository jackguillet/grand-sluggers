using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The ordinary pitch repertoire rail (#807): the five-family library (PH-02-R2), the fastball
/// every pitcher throws plus exactly two others (PH-15-R1), and the 25 accepted assignments
/// (PH-15-R2 captains, PH-15-R4 role players).
///
/// The rule these encode is that the roster's repertoires are the ones <b>Jack accepted</b>, not
/// the ones somebody edited afterwards: the shipped rows are compared to the decision register's
/// own contract blocks, both ways, so a new character or a quietly retuned row fails until the
/// register names it.
///
/// Membership only. Nothing here asserts a speed, a break or a stamina cost, because curveball,
/// slider and sinker do not fly yet (P1-b) and cannot be selected yet (P1-c).
/// </summary>
public class RepertoireTests
{
    // Named explicitly rather than through the ambient root: the assertion is about the shipped
    // roster, whatever overlay a process names.
    static readonly ContentCatalog Shipped = ContentCatalog.Load(new DataRoot(global::GrandSluggers.Sim.Tests.Shipped.Content.Root.Shipped));
    static string Repo => Path.GetFullPath(Path.Combine(Shipped.Root.Shipped, ".."));

    // ---- the library -------------------------------------------------------------------------

    [Fact]
    public void TheLibraryIsTheFiveAcceptedFamiliesAndOnlyTheFastballIsUnassignable()
    {
        Assert.Equal(
            new[] { "fastball", "changeup", "curveball", "slider", "sinker" },
            PitchFamily.All);
        Assert.Equal(PitchFamily.All.Where(f => f != PitchFamily.Fastball), PitchFamily.Assignable);

        Assert.All(PitchFamily.All, f => Assert.True(PitchFamily.IsKnown(f)));
        Assert.All(PitchFamily.Assignable, f => Assert.True(PitchFamily.IsAssignable(f)));
        Assert.True(PitchFamily.IsKnown(PitchFamily.Fastball));
        Assert.False(PitchFamily.IsAssignable(PitchFamily.Fastball));

        // Ids are lowercase and exact, like every other content id.
        Assert.False(PitchFamily.IsKnown("Fastball"));
        Assert.False(PitchFamily.IsKnown("curve"));
        Assert.False(PitchFamily.IsAssignable("breaker"));
    }

    /// <summary>
    /// The star skills spell two of their own rows <c>fastball</c> and <c>changeup</c>, and Pip's
    /// star pitch is <c>changeup</c> while her ordinary repertoire is changeup + slider. The
    /// overlap is vocabulary, not a reference: neither set may be resolved or validated against the
    /// other, and <c>breaker</c> is a star id that is not a family at all.
    /// </summary>
    [Fact]
    public void StarPitchIdsAreADifferentNamespaceFromTheFamilyLibrary()
    {
        Assert.True(Shipped.StarSkills.Pitches.ContainsKey("fastball"));
        Assert.True(Shipped.StarSkills.Pitches.ContainsKey("changeup"));
        Assert.True(Shipped.StarSkills.Pitches.ContainsKey("breaker"));
        Assert.False(PitchFamily.IsKnown("breaker"));

        Assert.Equal("heatball", Shipped.Characters["rio"].StarPitch);
        Assert.False(PitchFamily.IsKnown(Shipped.Characters["rio"].StarPitch));

        // A star id that happens to spell a family says nothing about the ordinary repertoire.
        var boom = Shipped.Characters["boom"];
        Assert.Equal("fastball", boom.StarPitch);
        Assert.Equal(new Repertoire("slider", "sinker"), boom.Repertoire);
    }

    // ---- the type ----------------------------------------------------------------------------

    [Fact]
    public void ARepertoireIsTheFastballPlusItsTwoSlotsInOrder()
    {
        var vale = new Repertoire("curveball", "slider");
        Assert.Equal(new[] { "fastball", "curveball", "slider" }, vale.Ordinary);
        Assert.Equal(Repertoire.Slots, vale.Ordinary.Count);
        Assert.Equal("fastball", vale[0]);
        Assert.Equal("curveball", vale[1]);
        Assert.Equal("slider", vale[2]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = vale[3]);

        Assert.True(vale.Has("fastball"));
        Assert.True(vale.Has("curveball"));
        Assert.True(vale.Has("slider"));
        Assert.False(vale.Has("changeup"));
        Assert.False(vale.Has("sinker"));
    }

    [Theory]
    [InlineData("fastball", "slider")]
    [InlineData("slider", "fastball")]
    [InlineData("slider", "slider")]
    [InlineData("breaker", "slider")]
    [InlineData("slider", "Sinker")]
    [InlineData("", "slider")]
    public void ARepertoireThatIsNotFastballPlusTwoDifferentFamiliesCannotBeBuilt(string second, string third)
    {
        Assert.Throws<ArgumentException>(() => new Repertoire(second, third));
    }

    /// <summary>
    /// <c>PlayTraceRace</c> runs the roster through <c>Distinct()</c>, so <see cref="Character"/>
    /// has to keep comparing by value — the reason the repertoire is a record and not a list.
    /// </summary>
    [Fact]
    public void CharactersStillCompareByValueWithTheirRepertoires()
    {
        var rio = Shipped.Characters["rio"];
        var same = global::GrandSluggers.Sim.Tests.Shipped.Content.Characters["rio"];
        Assert.Equal(rio, same);
        Assert.Equal(rio.GetHashCode(), same.GetHashCode());
        Assert.Single(new[] { rio, same }.Distinct());

        var other = rio with { Repertoire = new Repertoire("slider", "sinker") };
        Assert.NotEqual(rio, other);
        Assert.Equal(2, new[] { rio, other }.Distinct().Count());

        Assert.Equal(new Repertoire("changeup", "curveball"), new Repertoire("changeup", "curveball"));
        Assert.NotEqual(new Repertoire("changeup", "curveball"), new Repertoire("curveball", "changeup"));
    }

    // ---- the accepted assignments ------------------------------------------------------------

    /// <summary>
    /// Every shipped character carries the repertoire the register records, in the accepted order,
    /// and the register names every shipped character. A 26th character fails here until PH-15
    /// says what it throws.
    /// </summary>
    [Fact]
    public void EveryShippedRepertoireIsTheAcceptedAssignmentAndEveryCharacterIsNamed()
    {
        var accepted = AcceptedAssignments();
        Assert.Equal(25, accepted.Count);

        // Every character the register assigned is shipped with that assignment. The world's new characters (WD-11,
        // WD-13 A, WD-22) carry proposed repertoires until Jack accepts them; each is named here, so a character that is
        // neither assigned nor proposed is still a failure.
        Assert.All(accepted.Keys, id => Assert.Contains(id, Shipped.Characters.Keys));
        Assert.Equal(
            accepted.Keys.Concat(ProposedWorldCharacters).OrderBy(id => id, StringComparer.Ordinal),
            Shipped.Characters.Keys.OrderBy(id => id, StringComparer.Ordinal));

        foreach (var (id, character) in Shipped.Characters.OrderBy(c => c.Key, StringComparer.Ordinal))
            if (accepted.TryGetValue(id, out var pitches)) Assert.Equal(pitches, character.Repertoire.Ordinary);
    }

    // ---- the validator -----------------------------------------------------------------------

    /// <summary>
    /// The character loader ignores unknown JSON keys, so a misspelled key reads as no repertoire
    /// at all. "Missing" therefore has to be its own error, or a typo would ship a character
    /// throwing the fixture default.
    /// </summary>
    [Fact]
    public void AMisspelledOrAbsentRepertoireKeyIsReportedAsMissing()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/vale.json", json => json.Remove("repertoire"));
        fixture.ChangeArray("characters/role-players.json", rows =>
        {
            var pitches = rows[0]!["repertoire"]!.DeepClone();
            rows[0]!.AsObject().Remove("repertoire");
            rows[0]!["repertoir"] = pitches;
        });

        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("character 'vale' repertoire must list the two ordinary pitches", StringComparison.Ordinal)
            && e.Contains("the key is missing", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("character 'nico' repertoire", StringComparison.Ordinal)
            && e.Contains("the key is missing", StringComparison.Ordinal));

        var thrown = Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root));
        Assert.Contains("character 'vale' repertoire", thrown.Message);
    }

    [Theory]
    // too few, too many, none
    [InlineData("changeup", "repertoire must name exactly 2 ordinary pitches beside the fastball; got 1")]
    [InlineData("changeup,curveball,slider", "repertoire must name exactly 2 ordinary pitches beside the fastball; got 3")]
    [InlineData("", "repertoire must name exactly 2 ordinary pitches beside the fastball; got 0")]
    // the same family twice
    [InlineData("slider,slider", "repertoire names 'slider' twice")]
    // the implied fastball, authored
    [InlineData("fastball,slider", "repertoire[0] must not name 'fastball'")]
    // a star skill id that is not a family, and a family spelled the wrong way
    [InlineData("breaker,slider", "repertoire[0] must be one of [changeup, curveball, slider, sinker]; got 'breaker'")]
    [InlineData("changeup,Slider", "repertoire[1] must be one of [changeup, curveball, slider, sinker]; got 'Slider'")]
    public void ARepertoireThatIsNotTwoDifferentAssignableFamiliesFailsTheValidator(string families, string message)
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/vale.json", json =>
            json["repertoire"] = new JsonArray(families
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => (JsonNode)JsonValue.Create(f)!)
                .ToArray()));

        var errors = ContentDataValidator.Validate(fixture.Root);
        var error = Assert.Single(errors, e => e.Contains($"character 'vale' {message}", StringComparison.Ordinal));
        Assert.Contains(fixture.Path("characters/vale.json"), error);
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root));
    }

    [Fact]
    public void TheShippedRootLoadsWithEveryRepertoireAuthored()
    {
        Assert.Empty(ContentDataValidator.Validate(Shipped.Root));
        Assert.All(Shipped.Characters.Values, c =>
        {
            Assert.Equal(Repertoire.Slots, c.Repertoire.Ordinary.Count);
            Assert.All(c.Repertoire.Ordinary, f => Assert.True(PitchFamily.IsKnown(f)));
        });
    }

    // ---- the register ------------------------------------------------------------------------

    /// <summary>
    /// The PH-15-R2 and PH-15-R4 contract blocks, as <c>character_id</c> to the ordered families
    /// they name. The register spells the pitches in prose case ("Fastball"); the data rail spells
    /// ids lowercase, and the leading Fastball is asserted rather than assumed (PH-15-R1).
    /// </summary>
    /// <summary>The world's new captains and role players, whose repertoires are proposals (#1148), not register rows yet.</summary>
    static readonly string[] ProposedWorldCharacters =
        ["sable", "sirocco", "tumble", "adobe", "hollis", "flint", "cairn", "scree", "reed", "cattail", "bog", "tad",
         // The sidekicks WD-27 added: their build's repertoire until Jack accepts one.
         "benny", "hattie", "jojo", "ruby", "tundra", "crispin", "sleet", "flurry", "glimmer", "bingo", "kazoo", "taffy", "tilt", "confetti", "whirl", "vinnie", "slick", "penny", "dice", "ace", "bongo", "thicket", "kiwi", "liana", "fern", "slag", "clinker", "brimsy", "scorch", "kiln", "sedge", "paddle", "ripple", "minnow", "lotus", "barnacle", "breaker", "conch", "sandy", "shelly", "kelp", "coral", "nori", "mesquite", "rattle", "dusty", "pebble", "sage", "boulder", "crag", "tarn", "ridge", "cornice"];

    static Dictionary<string, string[]> AcceptedAssignments()
    {
        var path = Path.Combine(Repo, "docs", "research", "pitching-hitting-decisions.json");
        using var register = JsonDocument.Parse(File.ReadAllText(path));
        var accepted = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        var ph15 = register.RootElement.GetProperty("decisions").EnumerateArray()
            .Single(d => d.GetProperty("id").GetString() == "PH-15");
        foreach (var refinement in ph15.GetProperty("refinements").EnumerateArray())
        {
            if (!refinement.TryGetProperty("contract", out var contract)) continue;
            foreach (var key in new[] { "captain_assignments", "role_player_assignments" })
            {
                if (!contract.TryGetProperty(key, out var rows)) continue;
                foreach (var row in rows.EnumerateArray())
                {
                    var pitches = row.GetProperty("ordinary_pitches").EnumerateArray()
                        .Select(p => p.GetString()!.ToLowerInvariant()).ToArray();
                    Assert.Equal(Repertoire.Slots, pitches.Length);
                    Assert.Equal(PitchFamily.Fastball, pitches[0]);
                    accepted.Add(row.GetProperty("character_id").GetString()!, pitches);
                }
            }
        }
        return accepted;
    }
}
