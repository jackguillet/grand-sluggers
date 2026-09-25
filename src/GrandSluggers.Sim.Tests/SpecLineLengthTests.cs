using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The gameplay spec stays loadable (#1051). A reader that truncates long lines never sees the end of a long rule, so
/// every line of the index and of each <c>docs/spec/</c> file is at most <see cref="Cap"/> characters. Wrap prose at a
/// sentence; move a long table cell into a note under its table.
/// </summary>
public sealed class SpecLineLengthTests
{
    const int Cap = 600;

    static string Docs => Path.GetFullPath(Path.Combine(Shipped.Content.Root.Shipped, "..", "docs"));

    [Fact]
    public void EverySpecLineFitsTheCap()
    {
        var files = Directory.GetFiles(Path.Combine(Docs, "spec"), "*.md").Append(Path.Combine(Docs, "gameplay-spec.md"));
        var over = files
            .SelectMany(f => File.ReadAllLines(f).Select((line, i) => (f, i, line.Length)))
            .Where(x => x.Length > Cap)
            .Select(x => $"{Path.GetRelativePath(Docs, x.f)}:{x.i + 1} is {x.Length} characters")
            .ToList();
        Assert.True(over.Count == 0, $"Spec lines over {Cap} characters:\n{string.Join("\n", over)}");
    }

    [Fact]
    public void TheIndexLinksEverySectionFile()
    {
        var index = File.ReadAllText(Path.Combine(Docs, "gameplay-spec.md"));
        var missing = Directory.GetFiles(Path.Combine(Docs, "spec"), "*.md")
            .Select(f => "spec/" + Path.GetFileName(f))
            .Where(link => !index.Contains($"]({link})", StringComparison.Ordinal))
            .ToList();
        Assert.Empty(missing);
    }
}
