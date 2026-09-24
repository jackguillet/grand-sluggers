using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Every catalog is read strictly through <see cref="DataJson"/> (#1031): a key the file's type does not declare stops the
/// load and names the file and the key. Before, characters, bats, gloves, chemistry, art and tutorials dropped an unknown
/// key in silence, so a misspelled key played the default and no test failed.
/// </summary>
public sealed class DataJsonTests
{
    [Theory]
    [InlineData("bats/harbor-lumber.json", "powerMods", "powerMods is not a key this file declares")]
    [InlineData("gloves/web-back.json", "errorReductions", "errorReductions is not a key this file declares")]
    [InlineData("characters/rio.json", "stat", "stat is not a key this file declares")]
    [InlineData("chemistry/overrides.json", "rival", "rival is not a key this file declares")]
    public void AnUnknownKeyInAContentCatalogIsRefusedByName(string file, string key, string expected)
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject(file, json => json[key] = 1);
        var error = Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(new DataRoot(fixture.Root)));
        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
        Assert.Contains(file.Split('/')[1], error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AListFileWalksEachRow()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeArray("characters/role-players.json", rows => rows[0]!["speedd"] = 7);
        var error = Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(new DataRoot(fixture.Root)));
        Assert.Contains("[0].speedd is not a key this file declares", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("art/clips.json", "clipz")]
    [InlineData("art/rig.json", "bone")]
    public void AnUnknownKeyInAnArtFileIsRefusedByName(string file, string key)
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject(file, json => json[key] = 1);
        var error = Assert.Throws<InvalidDataException>(() => ArtCatalog.Load(new DataRoot(fixture.Root)));
        Assert.Contains($"{key} is not a key this file declares", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownKeyInATutorialFileIsRefusedByName()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("tutorials/lessons.json", json => json["lesson"] = 1);
        var content = ContentCatalog.Load(new DataRoot(fixture.Root));
        var error = Assert.ThrowsAny<Exception>(() => TutorialCatalog.Load(content));
        Assert.Contains("lesson is not a key this file declares", error.ToString(), StringComparison.Ordinal);
    }

    /// <summary>One set of reader options: a loader that builds its own is where a permissive read comes back.</summary>
    [Fact]
    public void NoSimLoaderBuildsItsOwnJsonOptions()
    {
        var sim = Path.GetFullPath(Path.Combine(ContentCatalog.Load().Root.Shipped, "..", "src", "GrandSluggers.Sim"));
        var own = new Regex(@"new JsonSerializerOptions|new JsonDocumentOptions|JsonDocumentOptions \w+ = new|JsonSerializerOptions \w+ = new");
        var offenders = Directory.EnumerateFiles(sim, "*.cs")
            .Where(f => Path.GetFileName(f) is not ("DataJson.cs" or "PlayTrace.cs"))   // PlayTrace writes; it does not read data
            .SelectMany(f => File.ReadLines(f).Select((l, i) => (f, i, l)))
            .Where(x => own.IsMatch(x.l.Split("//")[0]))
            .Select(x => $"{Path.GetFileName(x.f)}:{x.i + 1}")
            .ToList();
        Assert.True(offenders.Count == 0, "read data through DataJson: " + string.Join(", ", offenders));
    }
}
