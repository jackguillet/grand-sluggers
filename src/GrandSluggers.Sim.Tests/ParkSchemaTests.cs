using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// F1-a (#820, FD-01 / FR-03 / FR-04): a park file is held to the rule §16 already holds a rule table
/// to. A key the file does not declare stops the load and names itself; the field-pick cycle and the
/// home-park map come from the park files rather than from lists in code; an id the catalog does not
/// have is a stop rather than a silent Harbor.
///
/// <para>
/// The home-park rows carry the map as the <c>switch</c> in <c>PresetTeams.HomeParkId</c> spelled it
/// before this child deleted it, captain by captain, so the data has to reproduce it exactly.
/// </para>
/// </summary>
public sealed class ParkSchemaTests
{
    static readonly ContentCatalog Shipped = global::GrandSluggers.Sim.Tests.Shipped.Content;

    /// <summary>The field-pick cycle as the shipped six-id literal in <c>ExhibitionPick</c> spelled it.</summary>
    static readonly string[] PickOrder =
        [ParkId.Harbor, ParkId.Crystal, ParkId.Funfair, ParkId.Rooftop, ParkId.Canopy, ParkId.Ember,
         ParkId.Stillwater, ParkId.Coconut, ParkId.Sunscorch, ParkId.Summit];

    // ---------------------------------------------------------------------------------
    // SF-02  An unknown field, an unknown id or a misspelled key stops the load and names it
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("nightOnly", "nightOnly")]
    [InlineData("dayOnly", "dayOnly")]
    [InlineData("centreFenceFt", "centreFenceFt")]
    [InlineData("fenceHightFt", "fenceHightFt")]
    public void SF02_AnUnknownOrMisspelledParkKeyStopsTheLoadAndNamesTheFileAndTheKey(string key, string named)
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("parks/harbor-diamond.json", json => json[key] = 1);

        var errors = ContentDataValidator.Validate(fixture.Root);
        var error = Assert.Single(errors, e => e.Contains(named, StringComparison.Ordinal));
        Assert.StartsWith(fixture.Path("parks/harbor-diamond.json"), error, StringComparison.Ordinal);
        Assert.Contains("is not a key this file declares", error, StringComparison.Ordinal);
        Assert.Contains("the keys of park are", error, StringComparison.Ordinal);

        var thrown = Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root));
        Assert.Contains(error, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SF02_AnUnknownHazardKeyStopsTheLoadAndNamesTheRowAndTheKey()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("parks/funfair-park.json", json => json["hazards"]![3]!["periodSec"] = 18);

        var thrown = Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root));
        Assert.Contains(fixture.Path("parks/funfair-park.json") + ": hazards[3].periodSec is not a key this file declares",
            thrown.Message, StringComparison.Ordinal);
        Assert.Contains("the keys of hazard are [radius, tag, type, x, z]", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The dead fields are gone from the files and would now be refused if they came back: <c>notes</c>
    /// stays, because it is declared. It is declared on the loader's row and not on <see cref="Park"/>,
    /// so no rule can read it and no stored trace identity moves.
    /// </summary>
    [Fact]
    public void SF02_NotesIsDeclaredAndReachesNoRule()
    {
        Assert.Empty(ContentDataValidator.Validate(Shipped.Root));
        foreach (var id in PickOrder)
        {
            var file = Path.Combine(Shipped.Root.Shipped, "parks", id + ".json");
            var json = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
            Assert.True(json.ContainsKey("notes"), id);
            Assert.False(json.ContainsKey("nightOnly"), id);
            Assert.False(json.ContainsKey("dayOnly"), id);
        }
        Assert.DoesNotContain(typeof(Park).GetProperties(), p =>
            p.Name.Equals("Notes", StringComparison.Ordinal) || p.Name.Equals("PickOrder", StringComparison.Ordinal));
    }

    [Fact]
    public void SF02_AParkThatNamesNoPlaceInTheCycleOrTakesAnotherParksIsRefused()
    {
        using var missing = new ContentFixture();
        missing.ChangeObject("parks/crystal-rink.json", json => json.Remove("pickOrder"));
        Assert.Contains(ContentDataValidator.Validate(missing.Root), e =>
            e.StartsWith(missing.Path("parks/crystal-rink.json"), StringComparison.Ordinal)
            && e.Contains("park 'crystal-rink' pickOrder must name its place in the field-pick cycle", StringComparison.Ordinal));

        using var duplicate = new ContentFixture();
        duplicate.ChangeObject("parks/crystal-rink.json", json => json["pickOrder"] = 1);
        var clash = Assert.Single(ContentDataValidator.Validate(duplicate.Root), e =>
            e.Contains("park pickOrder '1' is claimed by more than one park", StringComparison.Ordinal));
        Assert.Contains(duplicate.Path("parks/crystal-rink.json"), clash, StringComparison.Ordinal);
        Assert.Contains(duplicate.Path("parks/harbor-diamond.json"), clash, StringComparison.Ordinal);
    }

    [Fact]
    public void SF02_TwoParksWithOneFactionAreRefused()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("parks/crystal-rink.json", json => json["faction"] = "spark");

        var thrown = Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root));
        Assert.Contains("park faction 'spark' is claimed by more than one park", thrown.Message, StringComparison.Ordinal);
        Assert.Contains(fixture.Path("parks/crystal-rink.json"), thrown.Message, StringComparison.Ordinal);
        Assert.Contains(fixture.Path("parks/harbor-diamond.json"), thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SF02_AnUnknownParkIdIsAStopNotHarbor()
    {
        var slice = Assert.Throws<KeyNotFoundException>(() => Match.Slice(Shipped, parkId: "nope"));
        Assert.Contains("No park 'nope'", slice.Message, StringComparison.Ordinal);
        Assert.Contains(ParkId.Harbor, slice.Message, StringComparison.Ordinal);

        Assert.Throws<KeyNotFoundException>(() =>
            Match.Exhibition(Shipped, "rio", "ashlord", innings: 3, seed: 1, parkId: "nope"));
        var (home, away) = PresetTeams.Pair(Shipped, "rio", "ashlord");
        Assert.Throws<KeyNotFoundException>(() =>
            Match.Exhibition(Shipped, home, away, innings: 3, seed: 1, parkId: "nope"));
        Assert.Throws<KeyNotFoundException>(() =>
            Challenge.Start(Shipped, "rio").MakeMatch(Shipped, parkId: "nope"));
    }

    [Fact]
    public void SF02_TheShippedRootLoadsCleanUnderTheSchema()
    {
        Assert.Empty(ContentDataValidator.Validate(Shipped.Root));
    }

    // ---------------------------------------------------------------------------------
    // Parity pins: the cycle and the home-park map the code used to hold
    // ---------------------------------------------------------------------------------

    [Fact]
    public void TheFieldPickCycleComesFromTheParkFilesInTheOrderTheLiteralHeld()
    {
        Assert.Equal(PickOrder, Shipped.ParkPickOrder);
        Assert.Equal(ParkId.Crystal, ExhibitionPick.WrapPark(Shipped, ParkId.Harbor, 1));
        Assert.Equal(ParkId.Summit, ExhibitionPick.WrapPark(Shipped, ParkId.Harbor, -1));
        Assert.Equal(ParkId.Harbor, ExhibitionPick.WrapPark(Shipped, ParkId.Summit, 1));
        // A pick the catalog does not have still cycles, from the first park.
        Assert.Equal(ParkId.Crystal, ExhibitionPick.WrapPark(Shipped, "nope", 1));

        var pick = ExhibitionPick.Default;
        foreach (var id in PickOrder.Skip(1).Concat([PickOrder[0]]))
        {
            pick = ExhibitionPick.CyclePark(Shipped, pick, 1);
            Assert.Equal(id, pick.Park);
            Assert.Equal("rio", pick.Home);
            Assert.Equal("ashlord", pick.Away);
        }
    }

    /// <summary>
    /// Every captain's home park, from the park files' factions: vale → Aurora Rink, zig → funfair, brondo → rooftop,
    /// konga → canopy, ashlord → ember, rio → Harbor, fenn → Coconut Cove, sable → Sunscorch Mesa, hollis → Summit
    /// Park, reed → Stillwater Marsh. A name that is no captain plays at Harbor.
    /// </summary>
    [Theory]
    [InlineData("rio", ParkId.Harbor)]
    [InlineData("vale", ParkId.Crystal)]
    [InlineData("zig", ParkId.Funfair)]
    [InlineData("brondo", ParkId.Rooftop)]
    [InlineData("konga", ParkId.Canopy)]
    [InlineData("ashlord", ParkId.Ember)]
    [InlineData("fenn", ParkId.Coconut)]
    [InlineData("sable", ParkId.Sunscorch)]
    [InlineData("hollis", ParkId.Summit)]
    [InlineData("reed", ParkId.Stillwater)]
    [InlineData("nobody-by-that-name", ParkId.Harbor)]
    public void EveryCaptainsHomeParkIsTheOneTheSwitchNamed(string captain, string park)
    {
        Assert.Equal(park, PresetTeams.HomeParkId(Shipped, captain));
        if (!Shipped.CaptainIds.Contains(captain, StringComparer.OrdinalIgnoreCase)) return;
        // An Exhibition with no park id named plays at the home captain's park, so the map is the one
        // the game actually uses, not a lookup nothing calls.
        Assert.Equal(park, Match.Exhibition(Shipped, captain, captain == "rio" ? "ashlord" : "rio",
            innings: 3, seed: 1).Park.Id);
    }

    [Fact]
    public void ARosterFactionWithNoParkOfItsOwnPlaysAtTheDefaultPark()
    {
        Assert.Equal(ExhibitionPick.DefaultPark, Shipped.HomeParkIdOfFaction("no-such-faction"));
        Assert.Equal(ExhibitionPick.DefaultPark, Shipped.HomeParkIdOfFaction(""));
        foreach (var id in Shipped.ParkPickOrder)
            Assert.Equal(id, Shipped.HomeParkIdOfFaction(Shipped.Parks[id].Faction));
    }

    // ---------------------------------------------------------------------------------
    // SF-04 (sim half)  No park id decides a rule but the one default
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// One default, named once (D21). The remaining exception is named with the child that owns it,
    /// so this row goes green again only when that child has moved its literal into data — not
    /// because somebody widened the allowance. The Unity half of SF-04 is F6-c's.
    ///
    /// <para>
    /// <b>#847 removed the first of the two.</b> <c>ParkHazards.ChompFly</c> gated Funfair's chompers
    /// on <c>park.Id != ParkId.Funfair</c>; the mouths are three <c>chomper</c> rows in
    /// <c>data/parks/funfair-park.json</c> now and the dispatch reads the hazard library's pattern,
    /// so <c>Fielding.cs</c> names no park at all. <c>CarnivalFront</c>'s per-id copy is F8-a's.
    /// </para>
    /// </summary>
    [Fact]
    public void SF04_TheOnlyParkIdLiteralInSimRuleCodeIsTheOneDefault()
    {
        var simDir = Path.Combine(RepoRoot(), "src", "GrandSluggers.Sim");
        var ids = new Regex("\"(" + string.Join("|", Shipped.ParkPickOrder.Select(Regex.Escape)) + ")\"");
        var found = new List<string>();
        foreach (var file in Directory.GetFiles(simDir, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                continue;
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
                if (ids.IsMatch(lines[i]))
                    found.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
        }

        // The field card (F8-a) reads the played park, so CarnivalFront names no park; F4-a (#847) took Fielding.cs off the
        // list. The one literal left is the default park.
        Assert.DoesNotContain(found, l => l.StartsWith("Fielding.cs:", StringComparison.Ordinal));
        Assert.DoesNotContain(found, l => l.StartsWith("CarnivalFront.cs:", StringComparison.Ordinal));
        var only = Assert.Single(found);
        Assert.StartsWith("ExhibitionPick.cs:", only, StringComparison.Ordinal);
        Assert.Contains("public const string DefaultPark = \"harbor-diamond\"", only, StringComparison.Ordinal);
    }

    static string RepoRoot() => Path.GetFullPath(Path.Combine(Shipped.Root.Shipped, ".."));
}
