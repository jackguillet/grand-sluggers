using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class ArtCatalogTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void CatalogIsValidAgainstShippedRoster()
    {
        var errors = _content.Art.Validate(_content);
        Assert.True(errors.Count == 0, string.Join("; ", errors));
    }

    [Fact]
    public void SharedRigHasTheMoveBonesChain()
    {
        var bones = _content.Art.Rig.Bones.Select(b => b.ToLowerInvariant()).ToHashSet();
        foreach (var need in new[] { "torso", "head", "lupper", "lfore", "rupper", "rfore", "lthigh", "lshin", "rthigh", "rshin", "bat", "glove" })
            Assert.Contains(need, bones);
        foreach (var ev in new[] { "Contact", "Release", "FootPlant" })
            Assert.Contains(ev, _content.Art.Rig.Events, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("hero-shared", _content.Art.Rig.Id, ignoreCase: true);
        Assert.Contains("SharedRig", _content.Art.Rig.Slot, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(".fbx", _content.Art.Rig.Slot, StringComparison.OrdinalIgnoreCase);
        var repo = Directory.GetParent(_content.Root)?.FullName
            ?? throw new InvalidOperationException("no repo root");
        var fbx = Path.GetFullPath(Path.Combine(repo, "unity",
            _content.Art.Rig.Slot.Replace('/', Path.DirectorySeparatorChar)));
        Assert.True(File.Exists(fbx), fbx);
        Assert.True(new FileInfo(fbx).Length > 10_000, "hero-shared.fbx is empty");
    }

    [Fact]
    public void ClipCatalogMatchesMotionListRowForRow()
    {
        foreach (var need in Motion.Clips)
        {
            Assert.True(_content.Art.TryClip(need.Id, out var clip), "missing clip " + need.Id);
            Assert.Equal(need.Loop, clip.Loop);
            Assert.Equal(need.Handed, clip.Handed);
            Assert.StartsWith("Assets/Art/Animation/Clips/", clip.Slot, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith("Assets/Resources/Art/Animation/Clips/", clip.PlayerSlot, StringComparison.OrdinalIgnoreCase);
        }
        Assert.Equal(Motion.Clips.Count, _content.Art.Clips.Count);
        Assert.True(_content.Art.TryClip("swing-slap", out var swing));
        Assert.Contains("Contact", swing.Events, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(Motion.SwingContact, swing.ContactAt);
        Assert.Equal(Motion.SwingFinish, swing.FinishAt);
        Assert.True(_content.Art.TryClip("swing-charge", out var charge));
        Assert.Equal(Motion.SwingContact, charge.ContactAt);
        Assert.Equal(Motion.SwingFinish, charge.FinishAt);
        Assert.False(_content.Art.TryClip("swing", out _));
        Assert.True(_content.Art.TryClip("pitch", out var pitch));
        Assert.Contains("Release", pitch.Events, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(Motion.PitchRelease, pitch.ReleaseAt);
        Assert.Equal(("Assets/Art/Animation/Clips/swing-slap-L.fbx", "Assets/Resources/Art/Animation/Clips/swing-slap-L.fbx"),
            ArtCatalog.ClipFiles(swing, Hand.L));
        Assert.Equal(("Assets/Art/Animation/Clips/swing-charge.fbx", "Assets/Resources/Art/Animation/Clips/swing-charge.fbx"),
            ArtCatalog.ClipFiles(charge, Hand.R));
        Assert.True(_content.Art.TryClip("run", out var run));
        Assert.Equal(("Assets/Art/Animation/Clips/run.fbx", "Assets/Resources/Art/Animation/Clips/run.fbx"),
            ArtCatalog.ClipFiles(run, Hand.L));
    }

    [Fact]
    public void EveryTakeIsBakedForEveryHandItNeeds()
    {
        var repo = Directory.GetParent(_content.Root)?.FullName
            ?? throw new InvalidOperationException("no repo root");
        foreach (var clip in _content.Art.Clips)
        {
            foreach (var hand in clip.Handed ? new[] { Hand.R, Hand.L } : new[] { Hand.R })
            {
                var (slot, playerSlot) = ArtCatalog.ClipFiles(clip, hand);
                var fbx = Path.Combine(repo, "unity", slot.Replace('/', Path.DirectorySeparatorChar));
                var player = Path.Combine(repo, "unity", playerSlot.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(fbx), fbx);
                Assert.True(new FileInfo(fbx).Length > 4_096, slot + " is empty");
                Assert.True(new FileInfo(fbx).Length < 400_000, slot + " carries a mesh; takes are armature-only");
                Assert.True(File.Exists(player), player);
                Assert.Equal(File.ReadAllBytes(fbx), File.ReadAllBytes(player));
            }
        }
    }

    [Fact]
    public void ExtrasAreKitMeshesOnRigBones()
    {
        var repo = Directory.GetParent(_content.Root)?.FullName
            ?? throw new InvalidOperationException("no repo root");
        var kit = Path.Combine(repo, "unity", ArtCatalog.ExtrasKitSlot.Replace('/', Path.DirectorySeparatorChar));
        var ascii = System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(kit));
        Assert.NotEmpty(_content.Art.Extras);
        foreach (var extra in _content.Art.Extras.Values)
        {
            Assert.Contains(extra.Bone, _content.Art.Rig.Bones, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(extra.Id, ascii);
        }
        Assert.True(_content.Art.TryExtra("sneakers", out var sneakers));
        Assert.Equal(new[] { "lShoe", "rShoe" }, sneakers.Hides);
        Assert.True(_content.Art.TryExtra("shell", out var shell));
        Assert.Equal("head", shell.Bone);
        // The DCC kit and the catalog agree on sockets.
        var py = File.ReadAllText(Path.Combine(repo, "tools", "blender", "hero_shared_extras.py"));
        foreach (var extra in _content.Art.Extras.Values)
            Assert.Contains($"\"{extra.Id}\": \"{extra.Bone}\"", py);
        Assert.DoesNotContain("brim", _content.Art.Extras.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void CaptainsAreSkinsOnSharedBodyTypes()
    {
        foreach (var id in Silhouette.Captains)
        {
            var who = _content.Must(id);
            var skin = _content.Art.SkinOf(who);
            Assert.True(skin.Captain);
            Assert.Equal(id, skin.BodyType, ignoreCase: true);
            Assert.NotEmpty(skin.Extras);
            Assert.False(string.IsNullOrWhiteSpace(skin.Portrait));
        }
        var nico = _content.Art.SkinOf(_content.Must("nico"));
        Assert.False(nico.Captain);
        Assert.Equal("rio", nico.BodyType, ignoreCase: true);
        Assert.Empty(nico.Extras);
        var frost = _content.Art.SkinOf(_content.Must("frost"));
        Assert.Equal("vale", frost.BodyType, ignoreCase: true);
        Assert.Empty(frost.Extras);
        foreach (var id in Silhouette.Captains)
        {
            foreach (var e in _content.Art.SkinOf(_content.Must(id)).Extras)
                Assert.True(_content.Art.TryExtra(e, out _), id + " extra " + e);
        }
        var repo = Directory.GetParent(_content.Root)?.FullName
            ?? throw new InvalidOperationException("no repo root");
        var extrasFbx = Path.GetFullPath(Path.Combine(repo, "unity",
            "Assets/Art/Characters/SharedRig/extras.fbx".Replace('/', Path.DirectorySeparatorChar)));
        Assert.True(File.Exists(extrasFbx), extrasFbx);
        Assert.True(new FileInfo(extrasFbx).Length > 10_000, "extras.fbx is empty");
        var extrasAscii = System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(extrasFbx));
        Assert.Contains(GearMesh.HittingBatVisual(), extrasAscii);
        Assert.Contains("glove-brown", extrasAscii);
        Assert.Contains("baseball", extrasAscii);
        Assert.Contains("shell", extrasAscii);
        var extrasPy = Path.GetFullPath(Path.Combine(repo, "tools", "blender", "hero_shared_extras.py"));
        Assert.True(File.Exists(extrasPy), extrasPy);
        var extrasSrc = File.ReadAllText(extrasPy);
        Assert.Contains($"join(\"{GearMesh.HittingBatVisual()}\"", extrasSrc);
        Assert.Contains("authored_origin=(0.0, 0.0, 0.0)", extrasSrc);
        Assert.Contains("No caps", extrasSrc);
        Assert.Contains("Diameter 1", extrasSrc);
        Assert.Contains("(1.0, 1.0, 1.0), m[\"cream\"])", extrasSrc);
        var importer = File.ReadAllText(Path.GetFullPath(Path.Combine(repo, "unity",
            "Assets/Editor/SharedRigImport.cs".Replace('/', Path.DirectorySeparatorChar))));
        Assert.Contains("imp.isReadable = sharedExtras", importer);
        Assert.Contains("Art/Characters/SharedRig/extras.fbx", importer);
        var extrasRes = Path.GetFullPath(Path.Combine(repo, "unity",
            "Assets/Resources/Art/Characters/SharedRig/extras.fbx".Replace('/', Path.DirectorySeparatorChar)));
        Assert.True(File.Exists(extrasRes), extrasRes + " — toys must bind in the Linux player");
        Assert.Equal(new FileInfo(extrasFbx).Length, new FileInfo(extrasRes).Length);
        var resourcesAscii = System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(extrasRes));
        Assert.Contains(GearMesh.HittingBatVisual(), resourcesAscii);
        var fennSkin = _content.Art.SkinOf(_content.Must("fenn"));
        Assert.Equal("fenn", fennSkin.BodyType, ignoreCase: true);
        Assert.Contains("shell", fennSkin.Extras);
        Assert.False(Directory.Exists(Path.Combine(repo, "unity", "Assets", "Art", "Characters", "fenn")),
            "Fenn is the shared rig plus extras, not a package");
        foreach (var bone in new[] { "torso", "head", "lUpper", "lFore", "rUpper", "rFore", "lThigh", "lShin", "rThigh", "rShin", "bat", "glove" })
            Assert.Contains(bone, _content.Art.Rig.Bones, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void BodyTypesAreToyProportionsThatStillCutDifferently()
    {
        foreach (var id in Silhouette.Captains)
        {
            var spec = Silhouette.Proportions(id);
            Assert.True(Silhouette.HeadToHeight(spec) >= 1.0f,
                id + " head/height " + Silhouette.HeadToHeight(spec) + " — face must read at plate");
        }
        var rio = Silhouette.Proportions("rio");
        var vale = Silhouette.Proportions("vale");
        var zig = Silhouette.Proportions("zig");
        var brondo = Silhouette.Proportions("brondo");
        Assert.True(vale.Height > rio.Height, "vale stays tall");
        Assert.True(vale.Width < rio.Width, "vale stays slim");
        Assert.True(zig.Head / zig.Height > rio.Head / rio.Height, "zig stays huge-head");
        Assert.True(brondo.Width > rio.Width && brondo.Torso > rio.Torso, "brondo stays brick");
        var konga = Silhouette.Proportions("konga");
        var ashlord = Silhouette.Proportions("ashlord");
        Assert.True(zig.Height < rio.Height, "zig is the baby");
        Assert.True(ashlord.Height > konga.Height && ashlord.Height > vale.Height, "ashlord is the slug");
        Assert.True(konga.Arms > ashlord.Arms && konga.Arms > rio.Arms, "konga has the ape arms");
        Assert.True(Math.Abs(brondo.Height - rio.Height) < 0.12f, "brondo is rio-height, not a giant");
        var fenn = Silhouette.Proportions("fenn");
        Assert.True(fenn.Height < rio.Height && fenn.Height > zig.Height, "fenn is a short turtle, not the baby");
        Assert.True(fenn.Width > rio.Width && fenn.Head > rio.Head, "fenn shell reads as the brim");
    }

    [Fact]
    public void EveryParkHasAKitSlotAndHarborIsPlaced()
    {
        foreach (var id in _content.Parks.Keys)
            Assert.True(_content.Art.TryPark(id, out _), "park kit " + id);
        Assert.True(_content.Art.TryPark("harbor-diamond", out var harbor));
        Assert.True(harbor.Placed);
        Assert.StartsWith("Assets/Art/Parks/", harbor.Slot, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, _content.Art.Parks.Count(p => p.Placed));
    }

    [Fact]
    public void VfxAndAudioSlotsCoverPresentationEvents()
    {
        Assert.True(_content.Art.TryVfx("puff", out _));
        Assert.True(_content.Art.TryVfx("heatball", out _));
        Assert.True(_content.Art.TryVfx("heart-swing", out _));
        Assert.True(_content.Art.TryAudio("bat-perfect", out var bat));
        Assert.Equal("sfx", bat.Kind, ignoreCase: true);
        Assert.True(_content.Art.TryAudio("crowd-bed", out var crowd));
        Assert.Equal("crowd", crowd.Kind, ignoreCase: true);
        Assert.True(_content.Art.TryAudio("crowd-swell", out _));
        Assert.True(_content.Art.TryAudio("vo-rio", out var vo));
        Assert.Equal("vo", vo.Kind, ignoreCase: true);
        foreach (var ev in _content.Art.Audio)
            Assert.Contains(ev.Kind.ToLowerInvariant(), new[] { "sfx", "crowd", "vo" });
        Assert.Contains("Assets/Art/Characters/SharedRig", _content.Art.Folders, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Assets/Art/Animation/Clips", _content.Art.Folders, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuthoredHitsAreOriginalWavsNotEmpty()
    {
        foreach (var id in new[] { "bat-perfect", "bat-solid", "bat-cheap", "glove", "crowd-bed", "throw" })
        {
            Assert.True(_content.Art.TryAudio(id, out var slot) && slot.Authored, id);
            Assert.True(AuthoredAudio.TryLoad(_content.Root, id, out var pcm, out var rate), id);
            Assert.True(rate >= 22050, id + " rate " + rate);
            Assert.True(pcm.Length > rate * 0.04, id + " too short " + pcm.Length);
        }
        Assert.True(_content.Art.TryAudio("crowd-swell", out var swell) && !swell.Authored);
    }

    [Fact]
    public void AuthoredBatPerfectIsBrighterThanCheap()
    {
        Assert.True(AuthoredAudio.TryLoad(_content.Root, "bat-perfect", out var perfect, out _));
        Assert.True(AuthoredAudio.TryLoad(_content.Root, "bat-cheap", out var cheap, out _));
        var brightPerfect = Bright(perfect);
        var brightCheap = Bright(cheap);
        Assert.True(brightPerfect > brightCheap * 1.4,
            $"perfect {brightPerfect:0.###} vs cheap {brightCheap:0.###}");
        Assert.True(AuthoredAudio.TryLoad(_content.Root, "crowd-bed", out var bed, out var rate));
        Assert.True(bed.Length > rate * 2, "crowd bed must loop longer than a beep");
    }

    static double Bright(float[] s)
    {
        double e = 0;
        for (var i = 1; i < s.Length; i++)
        {
            var d = s[i] - s[i - 1];
            e += d * d;
        }
        return e / s.Length;
    }
}
