using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class HomeSetTests
{
    [Fact]
    public void OfficialHomeLayout()
    {
        Assert.True(HomeSet.IsOfficialLayout());
        Assert.True(HomeSet.PlatePointFacesTheCatcher());
        Assert.True(HomeSet.BoxesClearThePlate());
        Assert.Equal(17.0 / 12.0, HomeSet.PlateW, 9);
        Assert.Equal(4.0, HomeSet.BoxW);
        Assert.Equal(6.0, HomeSet.BoxD);
        Assert.Equal(6.0 / 12.0, HomeSet.BoxGap, 9);
        Assert.Equal(HomeSet.PlateCenterZ + 4.0, HomeSet.BoxFrontZ, 9);
        Assert.Equal(HomeSet.PlateCenterZ - 2.0, HomeSet.BoxRearZ, 9);
        Assert.Equal(8.0, HomeSet.CatcherBoxW);
        Assert.Equal(43.0 / 12.0, HomeSet.CatcherBoxD, 9);
        Assert.Equal(HomeSet.BoxRearZ, HomeSet.CatcherBoxFrontZ, 9);
        Assert.InRange(HomeSet.ChalkW, 2.0 / 12.0, 4.0 / 12.0);
        Assert.True(HomeSet.BatterX < 0, "RH batter in the third-base box");
        Assert.InRange(HomeSet.BatterZ, HomeSet.BoxRearZ, HomeSet.BoxFrontZ);
        Assert.InRange(Math.Abs(HomeSet.BatterX), HomeSet.BoxInnerX, HomeSet.BoxInnerX + HomeSet.BoxW);
        Assert.True(HomeSet.FoulLineClearsTheBattersBox());
        Assert.True(HomeSet.FoulLineStartZ >= HomeSet.BoxFrontZ);
        Assert.False(FoulRayHitsBox(1.5, 1.5), "line from the point through the box");
        Assert.False(FoulRayHitsBox(3, 3));
        Assert.True(HomeSet.FoulLineStartZ > 4, "start is past the box front (~4.7 ft)");
    }

    static bool FoulRayHitsBox(double x, double z)
    {
        if (Math.Abs(x - z) > 0.01) return false;
        if (z + 1e-9 < HomeSet.FoulLineStartZ) return false;
        return x >= HomeSet.BoxInnerX && x <= HomeSet.BoxInnerX + HomeSet.BoxW
            && z >= HomeSet.BoxRearZ && z <= HomeSet.BoxFrontZ;
    }

    [Fact]
    public void PlateMeshAuthoringMatchesOfficialInches()
    {
        var repo = Directory.GetParent(ContentCatalog.Load().Root)?.FullName
            ?? throw new InvalidOperationException("no repo root");
        var py = File.ReadAllText(Path.Combine(repo, "tools", "blender", "harbor_kit.py"));
        Assert.Contains("17.0 / 12.0", py);
        Assert.Contains("8.5 / 12.0", py);
        Assert.DoesNotContain("PLATE_FRONT = 2.00", py);
    }
}
