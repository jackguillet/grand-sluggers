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
    /// <see cref="ParkBoundary.Default"/>). Moving them onto the
    /// match's table is its own change; nothing else in the sim may join this list.
    /// </summary>
    static readonly string[] ProcessGeometry = ["Rules.cs", "Diamond.cs", "ParkBoundary.cs"];

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

    /// <summary>
    /// Readers of the process diamond (<see cref="Diamond"/>'s bags, rubber and starts; home is the origin on every table) left in the sim. A reader that holds a
    /// table reads <see cref="DiamondGeometry.Of"/> of it (#1067); this ceiling only goes down, so no new reader joins them.
    /// </summary>
    const int ProcessDiamondReaders = 17;

    [Fact]
    public void NoNewReaderTakesTheProcessDiamond()
    {
        var read = new Regex(@"(?<![\w.])Diamond\.(Baseline|Mound|First|Second|Third|Rubber|Positions|Bag)\b");
        var n = Code().Where(c => c.File != "Diamond.cs").Sum(c => read.Matches(c.Text).Count);
        Assert.True(n <= ProcessDiamondReaders, $"{n} reads of the process diamond (ceiling {ProcessDiamondReaders}): read DiamondGeometry.Of(rules) instead");
        Assert.True(n >= ProcessDiamondReaders, $"{n} reads of the process diamond: lower {nameof(ProcessDiamondReaders)} to {n}");
    }

    /// <summary>
    /// Readers of the process edge (<see cref="ParkBoundary.Default"/>, <see cref="ParkBoundary.For(Park)"/>) left in the sim: the
    /// drawn wall and its dress. The flight plays <see cref="ParkBoundary.For(Park, RulesTable)"/> (#1067); this ceiling only goes down.
    /// </summary>
    const int ProcessEdgeReaders = 2;

    [Fact]
    public void NoNewReaderTakesTheProcessEdge()
    {
        var read = new Regex(@"ParkBoundary\.Default\b|ParkBoundary\.For\(\w+\)");
        var n = Code().Where(c => c.File != "ParkBoundary.cs").Sum(c => read.Matches(c.Text).Count);
        Assert.True(n <= ProcessEdgeReaders, $"{n} reads of the process edge (ceiling {ProcessEdgeReaders}): pass the match's table to ParkBoundary.For");
        Assert.True(n >= ProcessEdgeReaders, $"{n} reads of the process edge: lower {nameof(ProcessEdgeReaders)} to {n}");
    }
}
