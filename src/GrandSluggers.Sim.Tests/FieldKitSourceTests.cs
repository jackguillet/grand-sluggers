using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// F6-a (#859; FD-16, FR-13, FR-04): one field kit draws the diamond, rail and wall at every park.
/// Unity has no EditMode test assembly and <c>unity/</c> is not in the solution, so these read the
/// source the way <see cref="HarborKitPaintTests"/> does. The geometry the kit draws is pinned where it
/// is read from: <see cref="HarborWallTests"/> holds the loop to the flight polygon (<c>SF-05</c>).
/// </summary>
public sealed class FieldKitSourceTests
{
    static readonly ContentCatalog Content = ContentCatalog.Load();
    readonly string _repo = Path.GetFullPath(Path.Combine(Content.Root.Shipped, ".."));

    string Runtime(string file) =>
        File.ReadAllText(Path.Combine(_repo, "unity/Assets/Scripts/Runtime", file));

    /// <summary>The kit is park-neutral: no park id in it, so no park can be special-cased inside it.</summary>
    [Fact]
    public void TheFieldKitNamesNoPark()
    {
        var src = Runtime("FieldKit.cs");
        foreach (var id in Content.Parks.Keys)
            Assert.DoesNotContain("\"" + id + "\"", src, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The other five parks draw the kit, not the retired primitive diamond and fence, and Harbor's
    /// wall goes up through the kit rather than a second wall builder in <c>HarborKit.cs</c>.
    /// </summary>
    [Fact]
    public void EveryParkDrawsTheOneKit()
    {
        var view = Runtime("ParkView.cs");
        Assert.Contains("new FieldKit(", view, StringComparison.Ordinal);
        foreach (var retired in new[] { "void Infield(", "void Bags(", "void FoulLines(", "void Fence(", "void WarningTrack(", "void HarborDiamondSkin(" })
            Assert.DoesNotContain(retired, view, StringComparison.Ordinal);

        var harbor = Runtime("HarborKit.cs");
        Assert.Contains("Field.Wall(", harbor, StringComparison.Ordinal);
        Assert.DoesNotContain("RampPrism(", harbor, StringComparison.Ordinal);
    }
}
