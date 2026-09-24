using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// "Do not grow MatchDirector" as a test (#1042). Each Unity director becomes a real class that owns its state; until
/// then the files still written as <c>partial class MatchDirector</c> are listed here, and the list only shrinks. A new
/// partial fails, and <c>MatchDirector.cs</c> may not grow past its ceiling. When a director moves out, delete its row
/// and lower the ceiling to the new length.
/// </summary>
public sealed class DirectorRailTests
{
    static readonly string[] StillPartial =
    [
        "ActorDirector.cs", "AtBatDirector.cs", "FlowDirector.cs", "GuidedTutorialDirector.cs", "InPlayDirector.cs",
        "MatchDirector.cs", "PursuitSeatDirector.cs", "StillCapture.cs", "TutorialDirector.cs",
    ];

    /// <summary>The line count of <c>MatchDirector.cs</c> may only fall. Lower it with every director that leaves.</summary>
    const int MatchDirectorCeiling = 917;

    static string Scripts => Path.GetFullPath(Path.Combine(Shipped.Content.Root.Shipped, "..", "unity", "Assets", "Scripts"));

    [Fact]
    public void NoNewFileExtendsMatchDirector()
    {
        var partials = Directory.GetFiles(Scripts, "*.cs", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains("partial class MatchDirector", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
        Assert.Empty(partials.Except(StillPartial));
        // A director that became a real class leaves the list, so the list cannot hide a regression later.
        Assert.Empty(StillPartial.Except(partials));
    }

    [Fact]
    public void MatchDirectorDoesNotGrow()
    {
        var lines = File.ReadAllLines(Path.Combine(Scripts, "Runtime", "MatchDirector.cs")).Length;
        Assert.True(lines <= MatchDirectorCeiling,
            $"MatchDirector.cs is {lines} lines, over its ceiling of {MatchDirectorCeiling}: put the code in a director that owns it");
    }

    [Fact]
    public void StealDirectorIsARealClass()
    {
        var text = File.ReadAllText(Path.Combine(Scripts, "Runtime", "StealDirector.cs"));
        Assert.Contains("public sealed class StealDirector", text, StringComparison.Ordinal);
        Assert.DoesNotContain("partial class MatchDirector", text, StringComparison.Ordinal);
    }
}
