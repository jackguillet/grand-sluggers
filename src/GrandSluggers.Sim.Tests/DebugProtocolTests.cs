using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Tooling;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class DebugProtocolTests
{
    /// <summary>A rename of a seeded id is a spec change (docs/agent-rails.md §2).</summary>
    public static readonly string[] SeededIds =
    [
        "brim-in-plate-lens",
        "boxes-kiss-plate",
        "nice-hit-on-whiff",
        "cpu-defense-waits-for-batter",
        "bat-through-head",
        "bat-behind-head-at-ready"
    ];

    readonly DataRoot _root = Shipped.Content.Root;

    [Fact]
    public void ShippedCatalogLoadsAndValidates()
    {
        Assert.Empty(DebugProtocol.Validate(_root));
        var protocol = DebugProtocol.Load(_root);
        Assert.True(protocol.Entries.Count >= SeededIds.Length, "need at least five seeded rows");
        foreach (var id in SeededIds)
            Assert.Contains(protocol.Entries, e => e.Id == id);
        foreach (var row in protocol.Entries)
        {
            Assert.Contains(row.Stage, DebugProtocol.Stages);
            Assert.False(string.IsNullOrWhiteSpace(row.Signature));
            Assert.False(string.IsNullOrWhiteSpace(row.Cause));
            Assert.False(string.IsNullOrWhiteSpace(row.Fix));
            Assert.False(string.IsNullOrWhiteSpace(row.Issue));
        }
    }

    [Fact]
    public void SeededIdsAreStable()
    {
        var ids = DebugProtocol.Load(_root).Entries.Select(e => e.Id).ToList();
        foreach (var id in SeededIds)
            Assert.Contains(id, ids);
        Assert.Equal(SeededIds, SeededIds.Distinct(StringComparer.Ordinal));
    }

    [Fact]
    public void MissingFileFallsBackOnLoadAndFailsTheValidator()
    {
        var root = Path.Combine(Path.GetTempPath(), "gs-protocol-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var loaded = DebugProtocol.Load(root);
            Assert.Empty(loaded.Entries);
            var errors = DebugProtocol.Validate(root);
            Assert.Contains(errors, e => e.Contains("required agent catalog is missing", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void UnknownStageFailsTheValidator()
    {
        using var fixture = new ProtocolFixture();
        fixture.Change(json => json["entries"]![0]!["stage"] = "blender-gui");

        var errors = DebugProtocol.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("stage must be one of [cli-match, dcc, sitting, sim, still-gate, unity-console]; got 'blender-gui'", StringComparison.Ordinal));
        var thrown = Assert.Throws<InvalidDataException>(() => DebugProtocol.Load(fixture.Root));
        Assert.Contains("got 'blender-gui'", thrown.Message);
    }

    [Fact]
    public void MissingIdFailsTheValidator()
    {
        using var fixture = new ProtocolFixture();
        fixture.Change(json => json["entries"]![1]!["id"] = "");

        var errors = DebugProtocol.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("entries[1] id must not be empty", StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => DebugProtocol.Load(fixture.Root));
    }

    [Fact]
    public void DuplicateIdsAreCaseInsensitive()
    {
        using var fixture = new ProtocolFixture();
        fixture.Change(json => json["entries"]![2]!["id"] = "Brim-In-Plate-Lens");

        var errors = DebugProtocol.Validate(fixture.Root);
        var duplicate = Assert.Single(errors, e => e.Contains("duplicate debug-protocol id 'brim-in-plate-lens'", StringComparison.Ordinal));
        Assert.Contains("entries[0]", duplicate);
        Assert.Contains("entries[2]", duplicate);
    }

    [Fact]
    public void UnknownRowFieldFailsTheValidator()
    {
        using var fixture = new ProtocolFixture();
        fixture.Change(json => json["entries"]![0]!["patch"] = "shrink the hat");

        var errors = DebugProtocol.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("entries[0] 'patch' is not a debug-protocol field", StringComparison.Ordinal));
    }

    sealed class ProtocolFixture : IDisposable
    {
        public ProtocolFixture()
        {
            Root = Path.Combine(Path.GetTempPath(), "gs-protocol-" + Guid.NewGuid().ToString("N"));
            var dest = Path.Combine(Root, DebugProtocol.Directory);
            Directory.CreateDirectory(dest);
            File.Copy(
                DebugProtocol.PathFor(Shipped.Content.Root),
                Path.Combine(dest, DebugProtocol.FileName));
        }

        public string Root { get; }

        public void Change(Action<JsonObject> change)
        {
            var path = DebugProtocol.PathFor(Root);
            var json = JsonNode.Parse(
                File.ReadAllText(path),
                documentOptions: new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                })!.AsObject();
            change(json);
            File.WriteAllText(path, json.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
