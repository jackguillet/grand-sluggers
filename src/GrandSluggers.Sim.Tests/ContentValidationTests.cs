using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class ContentValidationTests
{
    [Fact]
    public void ShippedGameplayCatalogPassesTheNamedValidator()
    {
        var root = ContentCatalog.Load().Root;
        Assert.Empty(ContentDataValidator.Validate(root));
    }

    [Fact]
    public void DuplicateCharacterIdsAcrossFilesAndRoleRowsReportEverySourceInOrder()
    {
        using var fixture = new ContentFixture();
        var extra = fixture.Copy("characters/rio.json", "characters/z-rio-copy.json");
        fixture.ChangeObject("characters/z-rio-copy.json", json => json["id"] = "rIo");
        fixture.ChangeArray("characters/role-players.json", rows => rows[0]!["id"] = "RIO");

        var errors = ContentDataValidator.Validate(fixture.Root);
        var duplicate = Assert.Single(errors, e => e.Contains("duplicate character id 'rio'", StringComparison.Ordinal));
        var roleSource = fixture.Path("characters/role-players.json") + "[0]";
        var rioSource = fixture.Path("characters/rio.json");
        Assert.Contains(roleSource, duplicate);
        Assert.Contains(rioSource, duplicate);
        Assert.Contains(extra, duplicate);
        Assert.True(duplicate.IndexOf(rioSource, StringComparison.Ordinal) < duplicate.IndexOf(roleSource, StringComparison.Ordinal));
        Assert.True(duplicate.IndexOf(roleSource, StringComparison.Ordinal) < duplicate.IndexOf(extra, StringComparison.Ordinal));
        Assert.Equal(errors.OrderBy(e => e, StringComparer.Ordinal), errors);

        var thrown = Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root));
        Assert.Contains(duplicate, thrown.Message);
    }

    [Theory]
    [InlineData("parks", "harbor-diamond.json", "Harbor-Diamond", "park")]
    [InlineData("bats", "harbor-lumber.json", "HARBOR-LUMBER", "bat")]
    [InlineData("gloves", "web-back.json", "Web-Back", "glove")]
    public void DuplicateCatalogIdsAreCaseInsensitiveAndNameBothFiles(
        string directory,
        string original,
        string duplicateId,
        string kind)
    {
        using var fixture = new ContentFixture();
        var copy = fixture.Copy($"{directory}/{original}", $"{directory}/zz-copy.json");
        fixture.ChangeObject($"{directory}/zz-copy.json", json => json["id"] = duplicateId);

        var ex = Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root));
        Assert.Contains($"duplicate {kind} id '{duplicateId.ToLowerInvariant()}' (case-insensitive)", ex.Message);
        Assert.Contains(fixture.Path($"{directory}/{original}"), ex.Message);
        Assert.Contains(copy, ex.Message);
    }

    [Fact]
    public void InvalidCharacterFieldsAndUnknownGameplayReferencesFailTogether()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("characters/rio.json", json =>
        {
            json["pitch"] = 0;
            json["bat"] = 11;
            json["bats"] = "switch";
            json["throws"] = "southpaw";
            json["starPitch"] = "missing-pitch";
            json["starSwing"] = "missing-swing";
            json["fieldAbility"] = "teleport";
        });
        fixture.ChangeObject("chemistry/overrides.json", json =>
        {
            json["buddies"]!.AsArray().Add(new JsonArray("rio", "missing-player"));
        });

        var ex = Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root));
        var source = fixture.Path("characters/rio.json");
        Assert.Contains(source, ex.Message);
        Assert.Contains("character 'rio' pitch must be between 1 and 10; got 0", ex.Message);
        Assert.Contains("character 'rio' bat must be between 1 and 10; got 11", ex.Message);
        Assert.Contains("character 'rio' bats must be one of [L, R]; got 'switch'", ex.Message);
        Assert.Contains("character 'rio' throws must be one of [L, R]; got 'southpaw'", ex.Message);
        Assert.Contains("character 'rio' starPitch references unknown id 'missing-pitch'", ex.Message);
        Assert.Contains("character 'rio' starSwing references unknown id 'missing-swing'", ex.Message);
        Assert.Contains("character 'rio' fieldAbility", ex.Message);
        Assert.Contains("chemistry buddies[12][1] references unknown character 'missing-player'", ex.Message);
    }

    [Fact]
    public void RuntimeGameplayIdsRequireCanonicalCase()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeArray("characters/role-players.json", rows =>
        {
            rows[0]!["starPitch"] = "FASTBALL";
            rows[0]!["starSwing"] = "LINE";
            rows[0]!["fieldAbility"] = "SUPER-JUMP";
        });
        fixture.ChangeObject("parks/funfair-park.json", json => json["hazards"]![0]!["type"] = "WARP_PIPE");

        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("starPitch references unknown id 'FASTBALL'", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("starSwing references unknown id 'LINE'", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("fieldAbility", StringComparison.Ordinal) && e.Contains("got 'SUPER-JUMP'", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("hazard[0] type", StringComparison.Ordinal) && e.Contains("got 'WARP_PIPE'", StringComparison.Ordinal));
    }

    [Fact]
    public void InvalidParkAndGloveNumericDomainsAreRejected()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("parks/harbor-diamond.json", json =>
        {
            json["surface"] = "water";
            json["leftFenceFt"] = 0;
            json["hazards"] = new JsonArray(new JsonObject
            {
                ["type"] = "teleporter",
                ["x"] = 0,
                ["z"] = 50,
                ["radius"] = -2
            });
        });
        fixture.ChangeObject("gloves/web-back.json", json => json["errorReduction"] = 1.1);

        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("surface must be one of [ash, dirt, grass, ice]; got 'water'", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("hazard[0] type must be one of", StringComparison.Ordinal)
            && e.Contains("got 'teleporter'", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("leftFenceFt must be greater than 0; got 0", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("hazard[0] radius must be finite and at least 0; got -2", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("errorReduction must be between 0 and 1; got 1.1", StringComparison.Ordinal));
    }

    [Fact]
    public void NullRowsObjectsAndArraysProduceActionableErrorsInsteadOfCrashes()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeArray("characters/role-players.json", rows => rows.Add(null));
        fixture.ChangeObject("parks/harbor-diamond.json", json => json["hazards"] = new JsonArray((JsonNode?)null));
        fixture.ChangeObject("abilities/star-skills.json", json => json["pitches"]!["fastball"] = null);
        fixture.ChangeObject("chemistry/overrides.json", json =>
        {
            json["buddies"] = null;
            json["rivals"]!.AsArray().Add(null);
            json["rivals"]!.AsArray().Add(new JsonArray("rio", null));
        });

        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("characters/role-players.json[18]: character row must be an object; got null", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("parks/harbor-diamond.json: park 'harbor-diamond' hazard[0] must be an object; got null", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("abilities/star-skills.json: star pitch 'fastball' must be an object; got null", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("chemistry/overrides.json: chemistry buddies must be an array; got null", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("chemistry rivals[10] must be an array; got null", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("chemistry rivals[11][1] references unknown character ''", StringComparison.Ordinal));

        var ex = Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root));
        Assert.Contains("character row must be an object; got null", ex.Message);
    }

    [Fact]
    public void MissingEquipmentVisualsStillUsePlaceholderFallbacks()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("bats/harbor-lumber.json", json => json.Remove("visual"));
        fixture.ChangeObject("gloves/web-back.json", json => json["visual"] = "");

        var content = ContentCatalog.Load(fixture.Root);
        Assert.Equal("bat-wood", content.Bats["harbor-lumber"].Visual);
        Assert.Equal("glove-brown", content.Gloves["web-back"].Visual);
    }

    sealed class ContentFixture : IDisposable
    {
        static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

        public ContentFixture()
        {
            Root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "grand-sluggers-content-" + Guid.NewGuid().ToString("N"));
            CopyDirectory(ContentCatalog.Load().Root, Root);
        }

        public string Root { get; }

        public string Path(string relative) => System.IO.Path.Combine(Root, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));

        public string Copy(string from, string to)
        {
            var destination = Path(to);
            File.Copy(Path(from), destination);
            return destination;
        }

        public void ChangeObject(string relative, Action<JsonObject> change)
        {
            var path = Path(relative);
            var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            change(json);
            File.WriteAllText(path, json.ToJsonString(WriteOptions));
        }

        public void ChangeArray(string relative, Action<JsonArray> change)
        {
            var path = Path(relative);
            var json = JsonNode.Parse(File.ReadAllText(path))!.AsArray();
            change(json);
            File.WriteAllText(path, json.ToJsonString(WriteOptions));
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);

        static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(System.IO.Path.Combine(destination, System.IO.Path.GetRelativePath(source, directory)));
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
                File.Copy(file, System.IO.Path.Combine(destination, System.IO.Path.GetRelativePath(source, file)));
        }
    }
}
