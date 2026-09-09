using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class CharacterPackageTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void UniqueBindIsNotRioShared()
    {
        Assert.True(CharacterPackage.IsUnique("segmented"));
        Assert.True(CharacterPackage.IsUnique("skinned"));
        Assert.False(CharacterPackage.IsUnique(""));
        Assert.False(CharacterPackage.IsUnique("shared"));
        Assert.False(CharacterPackage.IsUnique("rigid"));
        Assert.True(CharacterPackage.Valid("segmented"));
        Assert.False(CharacterPackage.Valid("heat-weight"));
    }

    [Fact]
    public void EveryPackagedSkinHasBodyAlbedoAndPlayerCopy()
    {
        foreach (var skin in _content.Art.Skins.Values)
        {
            var errors = CharacterPackage.ValidateFiles(_content.Root, skin);
            Assert.True(errors.Count == 0, string.Join("; ", errors));
        }
    }

    [Fact]
    public void FennIsASkinnedCartoonPackage()
    {
        var fenn = _content.Art.SkinOf(_content.Must("fenn"));
        Assert.Equal(CharacterPackage.Skinned, fenn.Bind);
        Assert.True(CharacterPackage.IsUnique(fenn.Bind));
        Assert.False(string.IsNullOrWhiteSpace(fenn.Mesh));
        Assert.Contains("Characters/fenn", fenn.Mesh.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("fenn", fenn.BodyType, ignoreCase: true);
        foreach (var bone in CharacterPackage.Sockets)
            Assert.Contains(bone, _content.Art.Rig.Bones, StringComparer.OrdinalIgnoreCase);
        foreach (var clip in CharacterPackage.PackageClips)
            Assert.True(File.Exists(Path.Combine(Directory.GetParent(_content.Root)!.FullName, "unity",
                CharacterPackage.ClipSlot("fenn", clip).Replace('/', Path.DirectorySeparatorChar))),
                "missing package clip " + clip);
    }

    [Fact]
    public void SharedCaptainsStayOnHeroSharedUntilPackaged()
    {
        foreach (var id in new[] { "rio", "vale", "zig", "brondo", "konga", "ashlord" })
        {
            var skin = _content.Art.SkinOf(_content.Must(id));
            Assert.True(string.IsNullOrWhiteSpace(skin.Mesh), id + " should stay on hero-shared until it is a package");
            Assert.False(CharacterPackage.IsUnique(skin.Bind), id + " bind " + skin.Bind);
        }
    }

    [Fact]
    public void MissingAlbedoOnAPackageIsACatalogError()
    {
        var fake = new SkinSlot("nexthero", "nexthero", true, ["horn"], "Resources/Art/nexthero-hero", "spark",
            CharacterPackage.MeshSlot("nexthero"), CharacterPackage.Segmented);
        var errors = CharacterPackage.ValidateFiles(_content.Root, fake);
        Assert.Contains(errors, e => e.Contains("albedo", StringComparison.OrdinalIgnoreCase)
            || e.Contains("FBX", StringComparison.OrdinalIgnoreCase));
    }
}
