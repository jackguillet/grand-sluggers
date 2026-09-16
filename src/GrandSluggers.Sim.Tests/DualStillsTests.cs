using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class DualStillsTests
{
    /// <summary>A rename of a required kind is a spec change (docs/agent-rails.md §4).</summary>
    public static readonly string[] RequiredKindIds = ["body", "extras", "harbor-kit", "takes"];

    readonly DataRoot _root = ContentCatalog.Load().Root;
    string Repo => Path.GetFullPath(Path.Combine(_root.Shipped, ".."));

    [Fact]
    public void ShippedCatalogLoadsAndValidates()
    {
        Assert.Empty(DualStills.Validate(_root));
        var catalog = DualStills.Load(_root);
        Assert.Equal(DualStills.DropFolder, catalog.Drop);
        foreach (var trigger in DualStills.RequiredTriggers)
            Assert.Contains(trigger, catalog.Triggers);
        foreach (var id in RequiredKindIds)
            Assert.Contains(catalog.Kinds, k => k.Id == id);
        foreach (var kind in catalog.Kinds)
        {
            Assert.False(string.IsNullOrWhiteSpace(kind.Dcc));
            Assert.False(string.IsNullOrWhiteSpace(kind.DccCommand));
            Assert.NotEmpty(kind.InGame);
            Assert.False(string.IsNullOrWhiteSpace(kind.InGameCommand));
            if (DualStills.NamedDcc.TryGetValue(kind.Id, out var named))
                Assert.Equal(named, kind.Dcc);
        }
        Assert.Equal(DualStills.CriticSkill, catalog.Critic.Skill);
        foreach (var doc in DualStills.RequiredRubric)
            Assert.Contains(doc, catalog.Critic.Rubric);
        Assert.False(catalog.Critic.MayPassLook);
        Assert.False(catalog.Critic.MayClose188);
        Assert.False(catalog.Critic.MayEditRubric);
    }

    [Fact]
    public void RequiredKindIdsAreStable()
    {
        var ids = DualStills.Load(_root).Kinds.Select(k => k.Id).ToList();
        foreach (var id in RequiredKindIds)
            Assert.Contains(id, ids);
        Assert.Equal(RequiredKindIds, RequiredKindIds.Distinct(StringComparer.Ordinal));
    }

    [Fact]
    public void MissingFileFallsBackOnLoadAndFailsTheValidator()
    {
        var root = Path.Combine(Path.GetTempPath(), "gs-stills-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var loaded = DualStills.Load(root);
            Assert.Empty(loaded.Kinds);
            var errors = DualStills.Validate(root);
            Assert.Contains(errors, e => e.Contains("required agent catalog is missing", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CriticThatCanPassLookFailsTheValidator()
    {
        using var fixture = new DualStillsFixture();
        fixture.Change(json => json["critic"]!["mayPassLook"] = true);

        var errors = DualStills.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("critic mayPassLook must be false; a critic files, Jack passes look", StringComparison.Ordinal));
        var thrown = Assert.Throws<InvalidDataException>(() => DualStills.Load(fixture.Root));
        Assert.Contains("mayPassLook must be false", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingKindFailsTheValidator()
    {
        using var fixture = new DualStillsFixture();
        fixture.Change(json =>
        {
            var kinds = json["kinds"]!.AsArray();
            for (var i = kinds.Count - 1; i >= 0; i--)
            {
                if ((string?)kinds[i]!["id"] == "body")
                    kinds.RemoveAt(i);
            }
        });

        var errors = DualStills.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("kinds missing 'body'", StringComparison.Ordinal));
    }

    [Fact]
    public void RenamedDccStillFailsTheValidator()
    {
        using var fixture = new DualStillsFixture();
        fixture.Change(json => json["kinds"]![0]!["dcc"] = "clay-body.png");

        var errors = DualStills.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("dcc for 'body' must be 'dcc-body.png'; got 'clay-body.png'", StringComparison.Ordinal));
    }

    [Fact]
    public void UnknownFieldFailsTheValidator()
    {
        using var fixture = new DualStillsFixture();
        fixture.Change(json => json["pixelDiff"] = true);

        var errors = DualStills.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("'pixelDiff' is not a dual-stills field", StringComparison.Ordinal));
    }

    [Fact]
    public void CatalogDoesNotComparePixels()
    {
        var src = File.ReadAllText(Path.Combine(Repo, "src/GrandSluggers.Sim/DualStills.cs"));
        Assert.DoesNotContain("GetPixel", src, StringComparison.Ordinal);
        Assert.DoesNotContain("Bitmap", src, StringComparison.Ordinal);
        Assert.DoesNotContain("ImageDiff", src, StringComparison.Ordinal);
        Assert.DoesNotContain("PHash", src, StringComparison.Ordinal);
        Assert.DoesNotContain("perceptual", src, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SkillAndScreenshotGateListBothStills()
    {
        var gate = File.ReadAllText(Path.Combine(Repo, "docs/screenshot-gate.md"));
        var skill = File.ReadAllText(Path.Combine(Repo, ".grok/skills/character-art/SKILL.md"));
        var critic = File.ReadAllText(Path.Combine(Repo, ".grok/skills/look-critic/SKILL.md"));
        var rails = File.ReadAllText(Path.Combine(Repo, "docs/agent-rails.md"));

        Assert.Contains("tools/dcc-still.sh", gate, StringComparison.Ordinal);
        Assert.Contains("tools/still-gate-character.sh", gate, StringComparison.Ordinal);
        Assert.Contains("dcc-body.png", gate, StringComparison.Ordinal);
        Assert.Contains("char-{id}-rest.png", gate, StringComparison.Ordinal);
        Assert.Contains("look-critic", gate, StringComparison.Ordinal);
        Assert.Contains("cannot mark #188", gate, StringComparison.Ordinal);

        Assert.Contains("tools/dcc-still.sh", skill, StringComparison.Ordinal);
        Assert.Contains("still-gate-character.sh", skill, StringComparison.Ordinal);
        Assert.Contains("look-critic", skill, StringComparison.Ordinal);
        Assert.Contains("stop", skill, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("cannot mark #188", critic, StringComparison.Ordinal);
        Assert.Contains("cannot edit", critic, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docs/screenshot-gate.md", critic, StringComparison.Ordinal);
        Assert.Contains("docs/silhouette-bible.md", critic, StringComparison.Ordinal);

        Assert.Contains("tools/dcc-still.sh", rails, StringComparison.Ordinal);
        Assert.Contains("look-critic", rails, StringComparison.Ordinal);
    }

    [Fact]
    public void NamedDccScriptPrintsThePrPath()
    {
        var script = File.ReadAllText(Path.Combine(Repo, "tools/dcc-still.sh"));
        Assert.Contains("scratchpad/stills", script, StringComparison.Ordinal);
        Assert.Contains("dcc-body.png", script, StringComparison.Ordinal);
        Assert.Contains("dcc-extras.png", script, StringComparison.Ordinal);
        Assert.Contains("dcc-harbor-kit.png", script, StringComparison.Ordinal);
        Assert.Contains("--print", script, StringComparison.Ordinal);
        Assert.DoesNotContain("compare", script, StringComparison.OrdinalIgnoreCase);
    }

    sealed class DualStillsFixture : IDisposable
    {
        public DualStillsFixture()
        {
            Root = Path.Combine(Path.GetTempPath(), "gs-stills-" + Guid.NewGuid().ToString("N"));
            var dest = Path.Combine(Root, DualStills.Directory);
            Directory.CreateDirectory(dest);
            File.Copy(
                DualStills.PathFor(ContentCatalog.Load().Root),
                Path.Combine(dest, DualStills.FileName));
        }

        public string Root { get; }

        public void Change(Action<JsonObject> change)
        {
            var path = DualStills.PathFor(Root);
            var json = JsonNode.Parse(
                File.ReadAllText(path),
                documentOptions: new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                })!.AsObject();
            change(json);
            File.WriteAllText(path, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
