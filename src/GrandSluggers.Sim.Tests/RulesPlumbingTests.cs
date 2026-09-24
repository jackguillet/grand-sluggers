using System.Text.RegularExpressions;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The sim plays the table it is handed (§16). A helper that could omit its table used to fall back
/// to the process-wide one, so a match, a trial or a test that built its own table could measure one
/// table and play another without any test failing (#730). These read the sim's source: the table
/// is a required argument everywhere, and only the named process-wide geometry reads
/// <see cref="Rules.Default"/>.
/// </summary>
public sealed class RulesPlumbingTests
{
    /// <summary>
    /// The one diamond every park shares, read as process-wide statics (<see cref="Diamond"/>,
    /// <see cref="ParkDiamond"/>'s dress, <see cref="ParkBoundary.Default"/>). Moving them onto the
    /// match's table is its own change; nothing else in the sim may join this list.
    /// </summary>
    static readonly string[] ProcessGeometry = ["Rules.cs", "Diamond.cs", "ParkDiamond.cs", "ParkBoundary.cs"];

    static readonly string Sim = Path.GetFullPath(Path.Combine(Shipped.Content.Root.Shipped, "..", "src", "GrandSluggers.Sim"));

    static IEnumerable<(string File, int Line, string Text)> Code()
    {
        foreach (var file in Directory.EnumerateFiles(Sim, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)) continue;
            var n = 0;
            foreach (var line in File.ReadLines(file))
            {
                n++;
                var code = line.Split("//", 2)[0];
                if (code.TrimStart().StartsWith('*')) continue;
                yield return (Path.GetFileName(file), n, code);
            }
        }
    }

    [Fact]
    public void NoSimHelperTakesAnOptionalRulesTable()
    {
        var optional = new Regex(@"RulesTable\?\s+\w+\s*=\s*null|RulesTable\?\s+\w+\s*[,)]");
        var offenders = Code().Where(c => optional.IsMatch(c.Text)).Select(c => $"{c.File}:{c.Line}").ToList();
        Assert.True(offenders.Count == 0, "a rules table is a required argument: " + string.Join(", ", offenders));
    }

    [Fact]
    public void OnlyTheProcessGeometryReadsTheProcessTable()
    {
        var offenders = Code()
            .Where(c => c.Text.Contains("Rules.Default", StringComparison.Ordinal) && !ProcessGeometry.Contains(c.File))
            .Select(c => $"{c.File}:{c.Line}")
            .ToList();
        Assert.True(offenders.Count == 0, "pass the match's table instead of Rules.Default: " + string.Join(", ", offenders));
    }
}
