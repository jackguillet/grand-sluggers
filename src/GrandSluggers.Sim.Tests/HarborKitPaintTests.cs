using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public class HarborKitPaintTests
{
    readonly string _repo = Path.GetFullPath(Path.Combine(Shipped.Content.Root.Shipped, ".."));

    [Theory]
    [InlineData("bag", "chalk")]
    [InlineData("bag", "cream")]
    [InlineData("bag", "bag")]
    [InlineData("bag", "")]
    [InlineData("bag", "Mesh")]
    [InlineData("bag", "Material")]
    [InlineData("bag", "Material (Instance)")]
    [InlineData("bag", "dirt")]
    [InlineData("bag", "wood")]
    [InlineData("bag", "hill")]
    [InlineData("home-plate", "chalk")]
    [InlineData("home-plate", "")]
    [InlineData("home-plate", "Mesh")]
    [InlineData("home-plate", "home-plate")]
    public void ChalkMeshesPaintChalkNotDirtOrWood(string mesh, string material)
    {
        var fill = HarborKitPaint.For(mesh, material);
        Assert.Equal(HarborKitPaint.Fill.Chalk, fill);
        Assert.False(HarborKitPaint.IsDirtOrWood(fill), $"{mesh}/{material} painted {fill}");
    }

    [Theory]
    [InlineData("bag", "navy")]
    [InlineData("bag", "bag-piping")]
    [InlineData("home-plate", "navy")]
    public void NavyPipingStaysNavyOnChalkMeshes(string mesh, string material)
    {
        Assert.Equal(HarborKitPaint.Fill.Navy, HarborKitPaint.For(mesh, material));
        Assert.False(HarborKitPaint.IsDirtOrWood(HarborKitPaint.For(mesh, material)));
    }

    [Fact]
    public void EveryChalkMeshUnknownNameIsChalk()
    {
        Assert.Contains("bag", HarborKitPaint.ChalkMeshes);
        Assert.Contains("home-plate", HarborKitPaint.ChalkMeshes);
        foreach (var mesh in HarborKitPaint.ChalkMeshes)
        {
            Assert.True(HarborKitPaint.IsChalkMesh(mesh), mesh);
            Assert.Equal(HarborKitPaint.Fill.Chalk, HarborKitPaint.For(mesh, ""));
            Assert.Equal(HarborKitPaint.Fill.Chalk, HarborKitPaint.For(mesh, "unknown"));
            Assert.False(HarborKitPaint.IsDirtOrWood(HarborKitPaint.For(mesh, "Mesh")));
        }
    }

    [Fact]
    public void PrimitiveBagIsChalk()
    {
        Assert.Equal(HarborKitPaint.Fill.Chalk, HarborKitPaint.PrimitiveBag);
        Assert.False(HarborKitPaint.IsDirtOrWood(HarborKitPaint.PrimitiveBag));
    }

    [Theory]
    [InlineData("mound", "", HarborKitPaint.Fill.Dirt)]
    [InlineData("mound", "hill", HarborKitPaint.Fill.Dirt)]
    [InlineData("warning-track", "", HarborKitPaint.Fill.Dirt)]
    [InlineData("infield-dirt", "dirt", HarborKitPaint.Fill.Dirt)]
    [InlineData("dugout-1b", "", HarborKitPaint.Fill.Wood)]
    [InlineData("dugout-1b", "mesh", HarborKitPaint.Fill.Post)]
    [InlineData("dugout-1b", "pad", HarborKitPaint.Fill.Pad)]
    [InlineData("dugout-1b", "gold", HarborKitPaint.Fill.Gold)]
    [InlineData("dugout-1b", "roof", HarborKitPaint.Fill.Roof)]
    public void OtherSlotsKeepNamedFillsAndSlotDefaults(string mesh, string material, HarborKitPaint.Fill expected)
    {
        Assert.Equal(expected, HarborKitPaint.For(mesh, material));
    }

    /// <summary>
    /// The kit FBX's meshes are painted in one place. Since #859 that place is the one field kit
    /// (<c>FieldKit.cs</c>), which drops the bags, the plate and the mound at every park, and Harbor's
    /// dugouts and fans drop through it rather than through a second palette in <c>HarborKit.cs</c>.
    /// </summary>
    [Fact]
    public void HarborKitPaintsFromTheTable()
    {
        var kit = File.ReadAllText(Path.Combine(_repo, "unity/Assets/Scripts/Runtime/FieldKit.cs"));
        var harbor = File.ReadAllText(Path.Combine(_repo, "unity/Assets/Scripts/Runtime/HarborKit.cs"));
        Assert.Contains("HarborKitPaint.For", kit, StringComparison.Ordinal);
        Assert.Contains("HarborKitPaint.PrimitiveBag", kit, StringComparison.Ordinal);
        Assert.DoesNotContain("else next[i] = _kitWood;", kit, StringComparison.Ordinal);
        Assert.DoesNotContain("else next[i] = _kitWood;", harbor, StringComparison.Ordinal);
        Assert.Contains("Field.DropMesh(", harbor, StringComparison.Ordinal);
    }

    [Fact]
    public void BlenderBagAndPlateStillAssignChalk()
    {
        var py = File.ReadAllText(Path.Combine(_repo, "tools/blender/harbor_kit.py"));
        Assert.Contains("def build_bag(chalk, navy):", py, StringComparison.Ordinal);
        Assert.Contains("def build_home_plate(chalk):", py, StringComparison.Ordinal);
        Assert.Contains("chalk = mat(\"chalk\"", py, StringComparison.Ordinal);
        Assert.DoesNotContain("build_bag(dirt", py, StringComparison.Ordinal);
        Assert.DoesNotContain("build_bag(wood", py, StringComparison.Ordinal);
        Assert.DoesNotContain("build_home_plate(dirt", py, StringComparison.Ordinal);
    }
}
