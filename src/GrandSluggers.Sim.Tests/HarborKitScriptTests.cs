using System.Globalization;
using System.Text.RegularExpressions;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The Harbor DCC kit's numbers that are the game's own (F7-b): <c>tools/blender/harbor_kit.py</c> writes its field geometry as
/// Python literals, so the ones that must agree with the sim are pinned here — the 45° foul lines and the 17-inch plate. The kit
/// constant that differs from <c>boundary.json</c> by history (<c>HOME_RADIUS</c> 34 against the 36-ft backstop) is recorded, not
/// forced: moving it re-bakes the kit, an art stage with its own stills. The dirt and the dugout are not copies at all: the
/// kit reads <see cref="ParkDiamond"/>'s and <see cref="HarborDugout"/>'s own <c>const</c> table and the rules data (#928).
/// </summary>
public sealed class HarborKitScriptTests
{
    static readonly string Script = File.ReadAllText(Path.Combine(
        Directory.GetParent(Shipped.Content.Root.Shipped)!.FullName, "tools", "blender", "harbor_kit.py"));

    static double Const(string name)
    {
        var m = Regex.Match(Script, @"^" + name + @"\s*=\s*([^\n#]+)", RegexOptions.Multiline);
        Assert.True(m.Success, name);
        var parts = m.Groups[1].Value.Trim().Split('/', StringSplitOptions.TrimEntries);
        var v = double.Parse(parts[0], CultureInfo.InvariantCulture);
        foreach (var d in parts.Skip(1)) v /= double.Parse(d, CultureInfo.InvariantCulture);
        return v;
    }

    [Fact]
    public void TheKitsFoulLinesAndPlateAreTheGames()
    {
        Assert.Equal(45.0, Const("FOUL_DEG"));
        Assert.Equal(17.0 / 12.0, Const("PLATE_FRONT"), 12);
        Assert.Equal(17.0 / 12.0 / 2.0, Const("PLATE_HALF_W"), 12);
    }

    /// <summary>
    /// The kit number that differs from the boundary table, recorded so a change is a decision, not drift. The dugout pad is
    /// the table's own: the kit reads <c>boundary.json</c> (#928).
    /// </summary>
    [Fact]
    public void TheKitsHomeWrapIsRecordedAgainstTheBoundaryTable()
    {
        Assert.Equal(34.0, Const("HOME_RADIUS"));
        Assert.Equal(-36.0, ParkBoundary.From(Rules.Default.Boundary).BackstopZFt);
        Assert.Contains("\"data\" / \"rules\" / \"boundary.json\"", Script, StringComparison.Ordinal);
        Assert.Contains("DUGOUT_PAD = float(BOUNDARY[\"dugoutPadFt\"])", Script, StringComparison.Ordinal);
    }

    /// <summary>
    /// #928: the kit's path, bag pads, home pad and dugout are the sim's. The script reads each name from the Sim class's
    /// <c>const</c> table (<c>sim_consts</c>), so every name it reads must still be a numeric <c>const</c> there — a const
    /// turned into a property or renamed fails here, not as a <c>KeyError</c> mid-bake — and must be the value the game plays.
    /// </summary>
    [Fact]
    public void TheKitReadsTheDirtAndDugoutFromTheSimsOwnTable()
    {
        var reads = new (string Table, Type Class)[] { ("DIAMOND", typeof(ParkDiamond)), ("DUGOUT", typeof(HarborDugout)) };
        var repo = Directory.GetParent(Shipped.Content.Root.Shipped)!.FullName;
        foreach (var (table, cls) in reads)
        {
            Assert.Contains($"{table} = sim_consts(\"{cls.Name}\")", Script, StringComparison.Ordinal);
            var names = Regex.Matches(Script, table + @"\[""(\w+)""\]").Select(m => m.Groups[1].Value).Distinct().ToList();
            Assert.NotEmpty(names);
            var src = File.ReadAllText(Path.Combine(repo, "src", "GrandSluggers.Sim", cls.Name + ".cs"));
            foreach (var name in names)
            {
                var decl = Regex.Match(src, @"\bconst\s+(?:float|double|int)\s+" + name + @"\s*=\s*(-?\d+(?:\.\d+)?)[fd]?\s*;");
                Assert.True(decl.Success, $"harbor_kit.py reads {cls.Name}.{name}, which is not a numeric const");
                var field = cls.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                Assert.NotNull(field);
                Assert.Equal(Convert.ToDouble(field!.GetRawConstantValue(), CultureInfo.InvariantCulture),
                    double.Parse(decl.Groups[1].Value, CultureInfo.InvariantCulture), 4);
            }
        }

        foreach (var name in new[] { "PathWidth", "BagPadR", "HomePackedR", "SkinLoopSegs" })
            Assert.Contains($"DIAMOND[\"{name}\"]", Script, StringComparison.Ordinal);
        foreach (var name in new[] { "X", "Z", "HalfAlong", "HalfDeep" })
            Assert.Contains($"DUGOUT[\"{name}\"]", Script, StringComparison.Ordinal);
        // The back arc and the grass diamond are the infield table's (ParkDiamond.BackR / InnerHalf).
        Assert.Contains("INFIELD[\"backArcFt\"]", Script, StringComparison.Ordinal);
        Assert.Contains("INFIELD[\"innerHalfFt\"]", Script, StringComparison.Ordinal);
        // The copies #928 retired.
        foreach (var literal in new[] { "PATH_WIDTH = 8.0", "PATH_CORNER", "HOME_PACKED_R = 16.0", "DUGOUT_X = 70.0", "DUGOUT_Z = 40.0", "HALF_ALONG = 21.3" })
            Assert.DoesNotContain(literal, Script, StringComparison.Ordinal);
    }
}
