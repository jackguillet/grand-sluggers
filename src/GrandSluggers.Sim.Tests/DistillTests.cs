using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// R7 #654: file the sitting child, append a protocol row, promote on the
/// second firing. character-art grows from one real failed still.
/// </summary>
public sealed class DistillTests
{
    readonly string _root = ContentCatalog.Load().Root;
    string Repo => Path.GetFullPath(Path.Combine(_root, ".."));

    [Fact]
    public void PlaybookSectionFiveNamesTheThreeSteps()
    {
        var section = Section(File.ReadAllText(Path.Combine(Repo, "docs/playbook.md")), "## 5. Sittings are the exit");
        Assert.Contains("one issue per finding", section, StringComparison.Ordinal);
        Assert.Contains("data/agent/debug-protocol.json", section, StringComparison.Ordinal);
        Assert.Contains("second firing", section, StringComparison.Ordinal);
        Assert.Contains("promoted", section, StringComparison.Ordinal);
        Assert.Contains(".grok/skills/character-art/", section, StringComparison.Ordinal);
        Assert.Contains("swing-*-max-load", section, StringComparison.Ordinal);
        Assert.DoesNotContain("When R2 (#649) ships", section, StringComparison.Ordinal);
    }

    [Fact]
    public void CharacterArtSkillNamesTheThreeSteps()
    {
        var skill = File.ReadAllText(Path.Combine(Repo, ".grok/skills/character-art/SKILL.md"));
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
        var skill = File.ReadAllText(Path.Combine(Repo, ".grok/skills/character-art/SKILL.md"));
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
    public void PromotedSignaturesNameARealTest()
    {
        var sources = Directory.GetFiles(
                Path.Combine(Repo, "src", "GrandSluggers.Sim.Tests"),
                "*Tests.cs")
            .Select(File.ReadAllText)
            .ToList();
        var protocol = DebugProtocol.Load(_root);
        Assert.Contains(protocol.Entries, e => !string.IsNullOrWhiteSpace(e.Promoted));
        foreach (var row in protocol.Entries)
        {
            if (string.IsNullOrWhiteSpace(row.Promoted)) continue;
            var dot = row.Promoted.IndexOf('.');
            Assert.True(dot > 0, $"{row.Id}: promoted must be Type.Method, got '{row.Promoted}'");
            var type = row.Promoted[..dot];
            var method = row.Promoted[(dot + 1)..];
            Assert.Contains(
                sources,
                src => src.Contains($"class {type}", StringComparison.Ordinal)
                    && src.Contains($"void {method}(", StringComparison.Ordinal));
        }
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
