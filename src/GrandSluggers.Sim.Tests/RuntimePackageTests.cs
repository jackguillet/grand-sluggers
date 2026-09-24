using System.Text.Json;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The player build carries the runtime catalogs and nothing else (<c>data/package.json</c>), and the game
/// loads from that set alone: an agent ledger can neither ship nor stop a player from starting.
/// </summary>
public sealed class RuntimePackageTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "grand-sluggers-package-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    static string ShippedRoot => Shipped.Content.Root.Shipped;

    string Packaged()
    {
        var target = Path.Combine(_dir, "data");
        foreach (var relative in RuntimePackage.Load(ShippedRoot).Files(ShippedRoot))
        {
            var destination = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(Path.Combine(ShippedRoot, relative), destination);
        }
        return target;
    }

    [Fact]
    public void TheShippedPackageIsValid() => Assert.Empty(RuntimePackage.Validate(ShippedRoot));

    [Fact]
    public void ThePackageFileIsPlainJsonForTheDeliveryScript()
    {
        // tools/local-player.py reads it with Python's json module, which has no comment handling.
        using var doc = JsonDocument.Parse(File.ReadAllText(RuntimePackage.PathFor(ShippedRoot)));
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
    }

    [Fact]
    public void AgentLedgersAndBakeScriptsDoNotShip()
    {
        var files = RuntimePackage.Load(ShippedRoot).Files(ShippedRoot);
        Assert.Contains("rules/match.json", files);
        Assert.Contains("art/audio-clips/glove.wav", files);
        Assert.DoesNotContain(files, f => f.StartsWith(AgentData.Directory + "/", StringComparison.Ordinal));
        Assert.DoesNotContain(files, f => f.EndsWith(".py", StringComparison.Ordinal));
    }

    [Fact]
    public void TheGameLoadsFromThePackagedFilesAlone()
    {
        var root = Packaged();
        Assert.Empty(ContentDataValidator.Validate(root));
        Assert.Empty(RulesTable.Validate(root));
        var content = ContentCatalog.Load(root);
        Assert.Equal(Shipped.CaptainIds, content.CaptainIds);
    }

    [Fact]
    public void ABrokenAgentLedgerDoesNotStopTheGameLoad()
    {
        var root = Packaged();
        Directory.CreateDirectory(Path.Combine(root, AgentData.Directory));
        File.WriteAllText(RaceEvidence.PathFor(root), "{ not json");
        Assert.Empty(ContentDataValidator.Validate(root));
        Assert.NotEmpty(AgentData.Validate(root));
    }

    [Fact]
    public void AnUndeclaredFolderIsRefused()
    {
        var root = Packaged();
        File.Copy(RuntimePackage.PathFor(ShippedRoot), RuntimePackage.PathFor(root));
        Directory.CreateDirectory(Path.Combine(root, AgentData.Directory));
        Directory.CreateDirectory(Path.Combine(root, "renders"));
        Assert.Contains(RuntimePackage.Validate(root), e => e.Contains("'renders' is neither runtime nor tooling"));
    }

    [Fact]
    public void AgentDataMustStayTooling()
    {
        var root = Packaged();
        Directory.CreateDirectory(Path.Combine(root, AgentData.Directory));
        File.WriteAllText(RuntimePackage.PathFor(root), File.ReadAllText(RuntimePackage.PathFor(ShippedRoot))
            .Replace("\"tooling\": [\"agent\"]", "\"tooling\": []")
            .Replace("\"tutorials\"]", "\"tutorials\", \"agent\"]"));
        Assert.Contains(RuntimePackage.Validate(root), e => e.Contains("'agent' must be a tooling folder"));
    }

    [Fact]
    public void TheProcessTableAndTheCatalogReadOneRoot()
    {
        Rules.RequireDefaultRoot(Shipped.Content.Root);
        var elsewhere = new DataRoot(Packaged());
        var error = Assert.Throws<InvalidOperationException>(() => Rules.RequireDefaultRoot(elsewhere));
        Assert.Contains(elsewhere.Shipped, error.Message);
    }
}
