using System.Text.RegularExpressions;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// <c>docs/archive/</c> is history, not rules (#1054). Every archived file says so in its opening lines, and a live
/// doc (AGENTS.md, README.md, <c>docs/*.md</c>, <c>docs/spec/</c>) never cites the archive as the source of a rule: a
/// sentence that links into it does not say "rule" or "contract". Cite the spec or a decision plan for the rule and the
/// archive for the evidence.
/// </summary>
public sealed partial class ArchiveCitationTests
{
    static string Repo => Path.GetFullPath(Path.Combine(Shipped.Content.Root.Shipped, ".."));
    static string Archive => Path.Combine(Repo, "docs", "archive");

    [GeneratedRegex(@"\]\(([^)\s#]+)")]
    private static partial Regex Link();

    [GeneratedRegex(@"(?<=[.!?])\s+(?=[A-Z*\[`])")]
    private static partial Regex SentenceBreak();

    [GeneratedRegex(@"\b(contract|rules?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex RuleWord();

    [GeneratedRegex(@"Historical|history|Archived")]
    private static partial Regex Banner();

    static IEnumerable<string> LiveDocs() =>
        new[] { Path.Combine(Repo, "AGENTS.md"), Path.Combine(Repo, "README.md") }
            .Concat(Directory.GetFiles(Path.Combine(Repo, "docs"), "*.md"))
            .Concat(Directory.GetFiles(Path.Combine(Repo, "docs", "spec"), "*.md"));

    [Fact]
    public void EveryArchivedFileOpensAsHistory()
    {
        var missing = Directory.GetFiles(Archive, "*.md", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) != "README.md")
            .Where(f => !Banner().IsMatch(string.Join("\n", File.ReadLines(f).Take(6))))
            .Select(f => Path.GetRelativePath(Repo, f))
            .ToList();
        Assert.Empty(missing);
    }

    [Fact]
    public void NoLiveDocCitesTheArchiveAsARule()
    {
        var archive = Archive + Path.DirectorySeparatorChar;
        var offences = new List<string>();
        foreach (var doc in LiveDocs())
        {
            var dir = Path.GetDirectoryName(doc)!;
            foreach (var sentence in File.ReadLines(doc).SelectMany(l => SentenceBreak().Split(l)))
            {
                var cites = Link().Matches(sentence)
                    .Select(m => m.Groups[1].Value)
                    .Where(u => !u.Contains("://", StringComparison.Ordinal))
                    .Any(u => Path.GetFullPath(Path.Combine(dir, u)).StartsWith(archive, StringComparison.Ordinal));
                if (cites && RuleWord().IsMatch(sentence))
                    offences.Add($"{Path.GetRelativePath(Repo, doc)}: {sentence[..Math.Min(160, sentence.Length)]}");
            }
        }
        Assert.True(offences.Count == 0, "A live doc cites docs/archive/ as a rule:\n" + string.Join("\n", offences));
    }
}
