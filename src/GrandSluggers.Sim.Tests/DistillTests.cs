using System.Text.RegularExpressions;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Tooling;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// R7 #654: file the sitting child, append a protocol row, promote on the
/// second firing. character-art grows from one real failed still.
/// </summary>
public sealed class DistillTests
{
    readonly DataRoot _root = Shipped.Content.Root;
    string Repo => Path.GetFullPath(Path.Combine(_root.Shipped, ".."));

    [Fact]
    public void PlaybookSectionFiveNamesTheThreeSteps()
    {
        var section = Section(File.ReadAllText(Path.Combine(Repo, "docs/playbook.md")), "## 5. Sittings are the exit");
        Assert.Contains("one issue per finding", section, StringComparison.Ordinal);
        Assert.Contains("data/agent/debug-protocol.json", section, StringComparison.Ordinal);
        Assert.Contains("second firing", section, StringComparison.Ordinal);
        Assert.Contains("promoted", section, StringComparison.Ordinal);
        Assert.Contains(".claude/skills/character-art/", section, StringComparison.Ordinal);
        Assert.Contains("swing-*-max-load", section, StringComparison.Ordinal);
        Assert.DoesNotContain("When R2 (#649) ships", section, StringComparison.Ordinal);
    }

    [Fact]
    public void CharacterArtSkillNamesTheThreeSteps()
    {
        var skill = File.ReadAllText(Path.Combine(Repo, ".claude/skills/character-art/SKILL.md"));
        Assert.Contains("## Distill", skill, StringComparison.Ordinal);
        Assert.Contains("data/agent/debug-protocol.json", skill, StringComparison.Ordinal);
        Assert.Contains("second", skill, StringComparison.Ordinal);
        Assert.Contains("promote", skill, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("file", skill, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("append", skill, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CharacterArtGrewFromTheBatThroughHeadStill()
    {
        var skill = File.ReadAllText(Path.Combine(Repo, ".claude/skills/character-art/SKILL.md"));
        Assert.Contains("swing-*-max-load", skill, StringComparison.Ordinal);
        Assert.Contains("#623", skill, StringComparison.Ordinal);
        Assert.Contains("bat-through-head", skill, StringComparison.Ordinal);
        Assert.Contains("BatHeadClearance", skill, StringComparison.Ordinal);
        Assert.Contains("extend the falsifier", skill, StringComparison.Ordinal);
        Assert.Contains("Do not pose the bat in C#", skill, StringComparison.Ordinal);
        Assert.Contains(
            "TheBatClearsTheHeadOnEverySampleOfBothTakesAndTheWholeChargeUp",
            skill, StringComparison.Ordinal);

        var row = Assert.Single(DebugProtocol.Load(_root).Entries, e => e.Id == "bat-through-head");
        Assert.Equal("#623", row.Issue);
        Assert.Equal(
            "SwingPresentationTests.TheBatClearsTheHeadOnEverySampleOfBothTakesAndTheWholeChargeUp",
            row.Promoted);
        Assert.Contains("swing-*-max-load", row.Signature, StringComparison.Ordinal);
    }

    [Fact]
    public void CharacterArtGrewFromExtrasAsGeometryJunk()
    {
        var skill = File.ReadAllText(Path.Combine(Repo, ".claude/skills/character-art/SKILL.md"));
        Assert.Contains("#687", skill, StringComparison.Ordinal);
        Assert.Contains("extras-are-geometry-junk", skill, StringComparison.Ordinal);
        Assert.Contains("leave extras off the skin", skill, StringComparison.Ordinal);
        Assert.Contains("NoSkinListsAnExtraUntilTheyReadAsToys", skill, StringComparison.Ordinal);

        var row = Assert.Single(DebugProtocol.Load(_root).Entries, e => e.Id == "extras-are-geometry-junk");
        Assert.Equal("#687", row.Issue);
        Assert.Equal("ArtCatalogTests.NoSkinListsAnExtraUntilTheyReadAsToys", row.Promoted);
        Assert.Contains("circles/squares", row.Signature, StringComparison.Ordinal);
    }

    [Fact]
    public void PromotedSignaturesNameARealTest()
    {
        var protocol = DebugProtocol.Load(_root);
        Assert.Contains(protocol.Entries, e => !string.IsNullOrWhiteSpace(e.Promoted));
        foreach (var row in protocol.Entries)
        {
            if (string.IsNullOrWhiteSpace(row.Promoted)) continue;
            Assert.True(PromotionExists(row.Promoted),
                $"{row.Id}: promoted test or validator '{row.Promoted}' does not exist");
        }
    }

    [Theory]
    [InlineData("SwingPresentationTests.TheBatClearsTheHeadOnEverySampleOfBothTakesAndTheWholeChargeUp", true)]
    [InlineData("DotnetOutputPathsTests.test_both_configurations_keep_generated_files_outside_unity_package", true)]
    [InlineData("EditorShutdownTests.test_slow_editor_is_left_for_normal_shutdown", true)]
    [InlineData("tools/blender-run.sh", true)]
    [InlineData("SwingPresentationTests.MissingTest", false)]
    [InlineData("DotnetOutputPathsTests.test_missing_test", false)]
    [InlineData("MissingTests.test_slow_editor_is_left_for_normal_shutdown", false)]
    [InlineData("tools/missing-validator.sh", false)]
    [InlineData("tools/blender-run.sh Metal preflight", false)]
    [InlineData("tools/../tools/blender-run.sh", false)]
    [InlineData("docs/editor-startup.md", false)]
    public void PromotionReferencesResolveAcrossTestLanguages(string reference, bool expected)
    {
        Assert.Equal(expected, PromotionExists(reference));
    }

    bool PromotionExists(string reference)
    {
        // A validator is a repository-relative shell entry point, not prose.
        if (Regex.IsMatch(reference, @"^tools/(?:[A-Za-z0-9_-]+/)*[A-Za-z0-9_-]+\.sh$"))
        {
            var path = Path.Combine(Repo, reference);
            return File.Exists(path) && File.ReadLines(path).FirstOrDefault()?.StartsWith("#!", StringComparison.Ordinal) == true;
        }
        var match = Regex.Match(reference, @"^([A-Za-z_]\w*)\.([A-Za-z_]\w*)$");
        if (!match.Success) return false;
        var type = Regex.Escape(match.Groups[1].Value);
        var method = Regex.Escape(match.Groups[2].Value);
        var csharp = Directory.GetFiles(Path.Combine(Repo, "src/GrandSluggers.Sim.Tests"), "*Tests.cs");
        if (csharp.Select(File.ReadAllText).Any(src =>
            Regex.IsMatch(src, $@"(?m)^\s*public (?:sealed )?class {type}\b") &&
            Regex.IsMatch(src, $@"(?m)^\s*public void {method}\("))) return true;

        // Python tooling checks run in the same portable CI job as these tests.
        var python = Directory.GetFiles(Path.Combine(Repo, "tools/tests"), "test_*.py");
        return python.Select(File.ReadAllText).Any(src =>
        {
            var body = Regex.Match(src, $@"(?ms)^class {type}\(unittest\.TestCase\):\r?\n(.*?)(?=^\S|\z)");
            return body.Success && Regex.IsMatch(body.Groups[1].Value, $@"(?m)^    def {method}\(self\)");
        });
    }

    [Fact]
    public void BatThroughHeadPromotionSamplesBothTakesAndTheWholeChargeUp()
    {
        var src = File.ReadAllText(
            Path.Combine(Repo, "src/GrandSluggers.Sim.Tests/SwingPresentationTests.cs"));
        var start = src.IndexOf(
            "void TheBatClearsTheHeadOnEverySampleOfBothTakesAndTheWholeChargeUp()",
            StringComparison.Ordinal);
        Assert.True(start >= 0, "missing the promoted #623 test");
        var next = src.IndexOf("[Fact]", start + 1, StringComparison.Ordinal);
        var body = next < 0 ? src[start..] : src[start..next];
        Assert.Contains("SwingTake.Slap", body, StringComparison.Ordinal);
        Assert.Contains("SwingTake.Charge", body, StringComparison.Ordinal);
        Assert.Contains("HeldLoadAt", body, StringComparison.Ordinal);
        Assert.Contains("Hand.L", body, StringComparison.Ordinal);
        Assert.Contains("Hand.R", body, StringComparison.Ordinal);
        Assert.DoesNotContain("rio", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AgentRailsMarksDistillShipped()
    {
        var rails = File.ReadAllText(Path.Combine(Repo, "docs/agent-rails.md"));
        var section = Section(rails, "## 7. Distill");
        Assert.Contains("✅ **R7 #654", section, StringComparison.Ordinal);
        Assert.DoesNotContain("Missing: the finding also lands", section, StringComparison.Ordinal);
        Assert.Contains("swing-*-max-load", section, StringComparison.Ordinal);
        Assert.Contains("R7 ✅", rails, StringComparison.Ordinal);
    }

    static string Section(string markdown, string heading)
    {
        var start = markdown.IndexOf(heading, StringComparison.Ordinal);
        Assert.True(start >= 0, "missing " + heading);
        var next = markdown.IndexOf("\n## ", start + heading.Length, StringComparison.Ordinal);
        return next < 0 ? markdown[start..] : markdown[start..next];
    }
}
