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
    public void EveryParkHasAKitSlotAndHarborIsPlaced()
    {
        foreach (var id in _content.Parks.Keys)
            Assert.True(_content.Art.TryPark(id, out _), "park kit " + id);
        Assert.True(_content.Art.TryPark("harbor-diamond", out var harbor));
        Assert.True(harbor.Placed);
        Assert.StartsWith("Assets/Art/Parks/", harbor.Slot, StringComparison.OrdinalIgnoreCase);
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
}
