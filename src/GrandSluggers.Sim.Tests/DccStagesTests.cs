using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class DccStagesTests
{
    /// <summary>A rename of a required stage is a spec change (docs/agent-rails.md §6).</summary>
    public static readonly string[] RequiredStageIds = ["blocking", "fill", "motion", "export", "still"];

    readonly DataRoot _root = ContentCatalog.Load().Root;
    string Repo => Path.GetFullPath(Path.Combine(_root.Shipped, ".."));

    [Fact]
    public void ShippedCatalogLoadsAndValidates()
    {
        Assert.Empty(DccStages.Validate(_root));
        var catalog = DccStages.Load(_root);
        Assert.Equal(DccStages.OneShotBanned, catalog.OneShot);
        Assert.Equal(RequiredStageIds, catalog.Stages.Select(s => s.Id).ToArray());
        for (var i = 0; i < catalog.Stages.Count; i++)
        {
            var stage = catalog.Stages[i];
            Assert.Equal(i + 1, stage.N);
            Assert.Equal(DccStages.RequiredKinds[stage.Id], stage.Kind);
            Assert.False(stage.Character.Skip);
            Assert.False(string.IsNullOrWhiteSpace(stage.Character.Checkpoint));
            if (stage.Id == "motion")
                Assert.True(stage.Harbor.Skip);
            else
            {
                Assert.False(stage.Harbor.Skip);
                Assert.False(string.IsNullOrWhiteSpace(stage.Harbor.Checkpoint));
            }
        }
        Assert.Equal("clay", catalog.Stages[0].Kind);
        Assert.Equal("clay", catalog.Stages[1].Kind);
        Assert.Equal("sheet", catalog.Stages[2].Kind);
        Assert.Equal("fbx", catalog.Stages[3].Kind);
        Assert.Equal("dual", catalog.Stages[4].Kind);
    }

    [Fact]
    public void RequiredStageIdsAreStable()
    {
        var ids = DccStages.Load(_root).Stages.Select(s => s.Id).ToList();
        Assert.Equal(RequiredStageIds, ids);
        Assert.Equal(RequiredStageIds, RequiredStageIds.Distinct(StringComparer.Ordinal));
    }

    [Fact]
    public void MissingFileFallsBackOnLoadAndFailsTheValidator()
    {
        var root = Path.Combine(Path.GetTempPath(), "gs-stages-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var loaded = DccStages.Load(root);
            Assert.Empty(loaded.Stages);
            var errors = DccStages.Validate(root);
            Assert.Contains(errors, e => e.Contains("required agent catalog is missing", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void OneShotAllowedFailsTheValidator()
    {
        using var fixture = new DccStagesFixture();
        fixture.Change(json => json["oneShot"] = "allowed");

        var errors = DccStages.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("oneShot must be 'banned'; got 'allowed'", StringComparison.Ordinal));
        var thrown = Assert.Throws<InvalidDataException>(() => DccStages.Load(fixture.Root));
        Assert.Contains("oneShot must be 'banned'", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void HarborMotionMustSkip()
    {
        using var fixture = new DccStagesFixture();
        fixture.Change(json =>
        {
            var motion = json["stages"]!.AsArray().Single(s => (string?)s!["id"] == "motion")!.AsObject();
            motion["harbor"] = new JsonObject
            {
                ["script"] = "tools/blender/harbor_kit.py",
                ["work"] = "takes",
                ["flag"] = "--sheets",
                ["checkpoint"] = "scratchpad/takes/harbor-kit.png"
            };
        });

        var errors = DccStages.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("harbor must skip; Harbor has no takes", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingStageFailsTheValidator()
    {
        using var fixture = new DccStagesFixture();
        fixture.Change(json =>
        {
            var stages = json["stages"]!.AsArray();
            for (var i = stages.Count - 1; i >= 0; i--)
            {
                if ((string?)stages[i]!["id"] == "fill")
                    stages.RemoveAt(i);
            }
        });

        var errors = DccStages.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("stages missing 'fill'", StringComparison.Ordinal));
    }

    [Fact]
    public void WrongKindFailsTheValidator()
    {
        using var fixture = new DccStagesFixture();
        fixture.Change(json => json["stages"]![0]!["kind"] = "fbx");

        var errors = DccStages.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("kind for 'blocking' must be 'clay'; got 'fbx'", StringComparison.Ordinal));
    }

    [Fact]
    public void RenamedCheckpointFailsTheValidator()
    {
        using var fixture = new DccStagesFixture();
        fixture.Change(json => json["stages"]![0]!["character"]!["checkpoint"] = "scratchpad/clay/body.png");

        var errors = DccStages.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains(
            "checkpoint for 'blocking' must be 'scratchpad/takes/body.png'; got 'scratchpad/clay/body.png'",
            StringComparison.Ordinal));
    }

    [Fact]
    public void UnknownFieldFailsTheValidator()
    {
        using var fixture = new DccStagesFixture();
        fixture.Change(json => json["blendCache"] = true);

        var errors = DccStages.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("'blendCache' is not a dcc-stages field", StringComparison.Ordinal));
    }

    [Fact]
    public void SkillAndHarborNameTheStages()
    {
        var skill = File.ReadAllText(Path.Combine(Repo, ".claude/skills/character-art/SKILL.md"));
        var kit = File.ReadAllText(Path.Combine(Repo, "tools/blender/harbor_kit.py"));
        var readme = File.ReadAllText(Path.Combine(Repo, "tools/blender/README.md"));
        var rails = File.ReadAllText(Path.Combine(Repo, "docs/agent-rails.md"));
        var art = File.ReadAllText(Path.Combine(Repo, "docs/art-rails.md"));

        foreach (var doc in new[] { skill, kit, readme, rails, art })
        {
            Assert.Contains("blocking", doc, StringComparison.Ordinal);
            Assert.Contains("fill", doc, StringComparison.Ordinal);
            Assert.Contains("motion", doc, StringComparison.Ordinal);
            Assert.Contains("export", doc, StringComparison.Ordinal);
            Assert.Contains("still", doc, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("one-shot", skill, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("banned", skill, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data/agent/dcc-stages.json", skill, StringComparison.Ordinal);
        Assert.Contains("cli stages", skill, StringComparison.Ordinal);
        Assert.Contains("next prompt", skill, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("one-shot", kit, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("diamond / wall ring", kit, StringComparison.Ordinal);
        Assert.Contains("--clay", kit, StringComparison.Ordinal);
        Assert.Contains("--out", kit, StringComparison.Ordinal);

        Assert.Contains("one-shot", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data/agent/dcc-stages.json", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void BakeFlagsStillRun()
    {
        var blockout = File.ReadAllText(Path.Combine(Repo, "tools/blender/hero_shared_blockout.py"));
        var extras = File.ReadAllText(Path.Combine(Repo, "tools/blender/hero_shared_extras.py"));
        var takes = File.ReadAllText(Path.Combine(Repo, "tools/blender/hero_shared_takes.py"));
        var kit = File.ReadAllText(Path.Combine(Repo, "tools/blender/harbor_kit.py"));

        Assert.Contains("p.add_argument(\"--out\"", blockout, StringComparison.Ordinal);
        Assert.Contains("p.add_argument(\"--clay\"", blockout, StringComparison.Ordinal);
        Assert.Contains("p.add_argument(\"--out\"", extras, StringComparison.Ordinal);
        Assert.Contains("p.add_argument(\"--clay\"", extras, StringComparison.Ordinal);
        Assert.Contains("p.add_argument(\"--out\"", takes, StringComparison.Ordinal);
        Assert.Contains("p.add_argument(\"--sheets\"", takes, StringComparison.Ordinal);
        Assert.Contains("p.add_argument(\"--out\"", kit, StringComparison.Ordinal);
        Assert.Contains("p.add_argument(\"--clay\"", kit, StringComparison.Ordinal);
        Assert.DoesNotContain("p.add_argument(\"--stage\"", blockout, StringComparison.Ordinal);
        Assert.DoesNotContain("p.add_argument(\"--stage\"", extras, StringComparison.Ordinal);
        Assert.DoesNotContain("p.add_argument(\"--stage\"", takes, StringComparison.Ordinal);
        Assert.DoesNotContain("p.add_argument(\"--stage\"", kit, StringComparison.Ordinal);
    }

    [Fact]
    public void AgentRailsMarksStageSaveShipped()
    {
        var rails = File.ReadAllText(Path.Combine(Repo, "docs/agent-rails.md"));
        var start = rails.IndexOf("## 6. Stage-save DCC", StringComparison.Ordinal);
        Assert.True(start >= 0, "missing ## 6. Stage-save DCC");
        var next = rails.IndexOf("\n## ", start + 10, StringComparison.Ordinal);
        var section = next < 0 ? rails[start..] : rails[start..next];
        Assert.Contains("✅ **R6 #653", section, StringComparison.Ordinal);
        Assert.DoesNotContain("Missing: named stages", section, StringComparison.Ordinal);
        Assert.Contains("data/agent/dcc-stages.json", section, StringComparison.Ordinal);
        Assert.Contains("one-shot", section, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("R6 ✅", rails, StringComparison.Ordinal);
    }

    sealed class DccStagesFixture : IDisposable
    {
        public DccStagesFixture()
        {
            Root = Path.Combine(Path.GetTempPath(), "gs-stages-" + Guid.NewGuid().ToString("N"));
            var dest = Path.Combine(Root, DccStages.Directory);
            Directory.CreateDirectory(dest);
            File.Copy(
                DccStages.PathFor(ContentCatalog.Load().Root),
                Path.Combine(dest, DccStages.FileName));
        }

        public string Root { get; }

        public void Change(Action<JsonObject> change)
        {
            var path = DccStages.PathFor(Root);
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
