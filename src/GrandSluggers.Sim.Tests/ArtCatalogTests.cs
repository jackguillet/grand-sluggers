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
    public void ClipCatalogMatchesMoveBonesList()
    {
        foreach (var need in MoveBones.Clips)
            Assert.True(_content.Art.TryClip(need, out var clip), "missing clip " + need);
        Assert.Equal(MoveBones.Clips.Count, _content.Art.Clips.Count);
        Assert.True(_content.Art.TryClip("swing", out var swing));
        Assert.Contains("Contact", swing.Events, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(MoveBones.SwingContact, swing.ContactAt);
        Assert.True(_content.Art.TryClip("pitch", out var pitch));
        Assert.Contains("Release", pitch.Events, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(MoveBones.PitchRelease, pitch.ReleaseAt);
        Assert.StartsWith("Assets/Art/Animation/Clips/", swing.Slot, StringComparison.OrdinalIgnoreCase);
        var repo = Directory.GetParent(_content.Root)?.FullName
            ?? throw new InvalidOperationException("no repo root");
        var fbx = Path.GetFullPath(Path.Combine(repo, "unity",
            (swing.Slot + ".fbx").Replace('/', Path.DirectorySeparatorChar)));
        Assert.True(File.Exists(fbx), fbx);
        Assert.True(new FileInfo(fbx).Length > 10_000, "swing.fbx is empty");
    }

    [Fact]
    public void AuthoredRunClipIsNotRawMoveBones()
    {
        Assert.True(_content.Art.TryClip("run", out var clip) && clip.Authored);
        Assert.True(_content.Art.TryAuthored("run", 0, out var authored));
        var bones = MoveBones.Evaluate(MoveBones.Verb.Run, 0, 0);
        Assert.NotEqual(bones.Torso.Y, authored.Torso.Y);
        Assert.True(Math.Abs(authored.Torso.Y) > Math.Abs(bones.Torso.Y),
            $"authored lean {authored.Torso.Y} vs bones {bones.Torso.Y}");
    }

    [Fact]
    public void AuthoredSwingClipIsNotRawMoveBones()
    {
        Assert.True(_content.Art.TryClip("swing", out var clip) && clip.Authored);
        Assert.False(clip.Loop);
        Assert.Equal(MoveBones.SwingContact, clip.ContactAt);
        Assert.Contains("Contact", clip.Events, StringComparer.OrdinalIgnoreCase);
        Assert.True(_content.Art.TryAuthored("swing", 0, out var load));
        var bonesLoad = MoveBones.Evaluate(MoveBones.Verb.Swing, 0, 0);
        Assert.NotEqual(bonesLoad.Torso.Y, load.Torso.Y);
        Assert.True(Math.Abs(load.Torso.Y) > Math.Abs(bonesLoad.Torso.Y),
            $"authored load {load.Torso.Y} vs bones {bonesLoad.Torso.Y}");

        Assert.True(_content.Art.TryAuthored("swing", clip.ContactAt, out var contact));
        var bonesHit = MoveBones.Evaluate(MoveBones.Verb.Swing, 0, clip.ContactAt);
        Assert.NotEqual(bonesHit.Torso.Y, contact.Torso.Y);
        Assert.True(Math.Abs(contact.Torso.Y) > Math.Abs(bonesHit.Torso.Y),
            $"authored contact yaw {contact.Torso.Y} vs bones {bonesHit.Torso.Y}");
        Assert.True(_content.Art.TryAuthored("swing", 10, out var held));
        Assert.True(_content.Art.TryAuthored("swing", 0.50, out var wrap));
        Assert.Equal(wrap.Torso.Y, held.Torso.Y);
    }

    [Fact]
    public void AuthoredScoopClipIsNotRawMoveBones()
    {
        Assert.True(_content.Art.TryClip("scoop", out var clip) && clip.Authored);
        Assert.False(clip.Loop);
        Assert.Equal(MoveBones.Mark(MoveBones.Verb.Scoop, MoveBones.ClipEvent.Contact), clip.ContactAt);
        Assert.Contains("Contact", clip.Events, StringComparer.OrdinalIgnoreCase);
        Assert.True(_content.Art.TryAuthored("scoop", 0, out var start));
        var bonesStart = MoveBones.Evaluate(MoveBones.Verb.Scoop, 0, 0);
        Assert.NotEqual(bonesStart.Torso.X, start.Torso.X);

        Assert.True(_content.Art.TryAuthored("scoop", clip.ContactAt, out var pick));
        var bonesPick = MoveBones.Evaluate(MoveBones.Verb.Scoop, 0, clip.ContactAt);
        Assert.True(pick.Torso.X > bonesPick.Torso.X,
            $"authored pick {pick.Torso.X} vs bones {bonesPick.Torso.X}");
        Assert.True(pick.Lift < -0.4, $"authored scoop lift {pick.Lift} is not on the dirt");
        var repo = Directory.GetParent(_content.Root)?.FullName
            ?? throw new InvalidOperationException("no repo root");
        var fbx = Path.GetFullPath(Path.Combine(repo, "unity",
            (clip.Slot + ".fbx").Replace('/', Path.DirectorySeparatorChar)));
        Assert.True(File.Exists(fbx), fbx);
        Assert.True(new FileInfo(fbx).Length > 10_000, "scoop.fbx is empty");
        Assert.True(Math.Abs(pick.RUpper.Z) < 12,
            $"authored glove abducts into a T z={pick.RUpper.Z}");
        Assert.True(_content.Art.TryAuthored("scoop", 10, out var held));
        Assert.True(_content.Art.TryAuthored("scoop", 0.50, out var up));
        Assert.Equal(up.Torso.X, held.Torso.X);
    }

    [Fact]
    public void MissingAuthoredClipFallsBackToMoveBones()
    {
        Assert.True(_content.Art.TryClip("pitch", out var pitch) && !pitch.Authored);
        Assert.False(_content.Art.TryAuthored("pitch", 0, out _));
        Assert.True(_content.Art.TryClip("slide", out var slide) && !slide.Authored);
        Assert.False(_content.Art.TryAuthored("slide", 0, out _));
        Assert.True(_content.Art.TryClip("throw", out var thr) && !thr.Authored);
        Assert.False(_content.Art.TryAuthored("throw", 0, out _));
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
        var extras = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "brim", "cheeks", "sneakers", "sash", "crown", "neck",
            "goggles", "cube-chest", "brick-jaw", "snout", "belly",
            "horns", "cape", "ember-eyes"
        };
        foreach (var id in Silhouette.Captains)
        {
            foreach (var e in _content.Art.SkinOf(_content.Must(id)).Extras)
                Assert.Contains(e, extras);
        }
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
