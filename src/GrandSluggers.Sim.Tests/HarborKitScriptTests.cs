using System.Globalization;
using System.Text.RegularExpressions;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The Harbor DCC kit's numbers that are the game's own (F7-b): <c>tools/blender/harbor_kit.py</c> writes its field geometry as
/// Python literals, so the ones that must agree with the sim are pinned here — the 45° foul lines and the 17-inch plate. The kit
/// constants that differ from <c>boundary.json</c> on purpose or by history (<c>HOME_RADIUS</c> 34 against the 36-ft foul offset,
/// <c>DUGOUT_PAD</c> 14 against 18) are recorded, not forced: moving them re-bakes the kit, an art stage with its own stills.
/// </summary>
public sealed class HarborKitScriptTests
{
    static readonly string Script = File.ReadAllText(Path.Combine(
        Directory.GetParent(ContentCatalog.Load().Root.Shipped)!.FullName, "tools", "blender", "harbor_kit.py"));

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

    /// <summary>The two kit numbers that differ from the boundary table, recorded so a change to either is a decision, not drift.</summary>
    [Fact]
    public void TheKitsHomeWrapAndDugoutPadAreRecordedAgainstTheBoundaryTable()
    {
        Assert.Equal((34.0, 14.0), (Const("HOME_RADIUS"), Const("DUGOUT_PAD")));
        Assert.Equal((36.0, 18.0), (ParkBoundary.Default.FoulOffsetFt, ParkBoundary.Default.DugoutPadFt));
    }
}
