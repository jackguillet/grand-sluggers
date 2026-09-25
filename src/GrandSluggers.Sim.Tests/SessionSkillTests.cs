using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Every session kind has a procedure skill in the repo, where every agent sees it (#1058): AGENTS.md names it, the
/// skill's frontmatter carries its name, and it points at its kind's debug-protocol rows.
/// </summary>
public sealed class SessionSkillTests
{
    static string Repo => Path.GetFullPath(Path.Combine(Shipped.Content.Root.Shipped, ".."));

    [Theory]
    [InlineData("gameplay-session", "gameplay")]
    [InlineData("presentation-session", "presentation")]
    public void EachKindHasASkillThatLoadsItsProtocolRows(string skill, string kind)
    {
        var path = Path.Combine(Repo, ".claude", "skills", skill, "SKILL.md");
        Assert.True(File.Exists(path), path);
        var text = File.ReadAllText(path);
        Assert.StartsWith("---\nname: " + skill + "\n", text.Replace("\r\n", "\n"), StringComparison.Ordinal);
        Assert.Contains("protocol --kind " + kind, text, StringComparison.Ordinal);
        Assert.Contains("tools/test-fast.sh", text, StringComparison.Ordinal);
        Assert.Contains(".claude/skills/" + skill + "/", File.ReadAllText(Path.Combine(Repo, "AGENTS.md")), StringComparison.Ordinal);
    }

    [Fact]
    public void ArtKeepsItsSkill() =>
        Assert.Contains(".claude/skills/character-art/", File.ReadAllText(Path.Combine(Repo, "AGENTS.md")), StringComparison.Ordinal);
}
