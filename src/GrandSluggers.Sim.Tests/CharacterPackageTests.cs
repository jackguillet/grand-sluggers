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
            _content.Art.Packages.TryGetValue(skin.Id, out var package);
            var errors = CharacterPackage.ValidateFiles(_content.Root, skin, package);
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
        Assert.True(_content.Art.TryPackage("fenn", out var package));
        Assert.Equal(CharacterPackage.ControllerSlot("fenn"), package.Controller);
        Assert.Equal(CharacterPackage.PlayerControllerSlot("fenn"), package.PlayerController);
    }

    [Fact]
    public void FennAuthoredBattingTakesUseTheGenericPackageRail()
    {
        Assert.True(_content.Art.TryPackage("fenn", out var package));
        foreach (var verb in CharacterPackage.RuntimeVerbs)
        {
            Assert.True(CharacterPackage.TryVerb(package, verb, out var slot), CharacterPackage.VerbId(verb));
            Assert.Equal(CharacterPackage.CharacterMotionFallback, slot.Fallback);
        }

        Assert.True(CharacterPackage.TryVerb(package, MoveBones.Verb.Idle, out var idle));
        Assert.True(CharacterPackage.IsReady(idle));
        Assert.True(idle.Loop);
        Assert.Equal(CharacterPackage.WorldClock, idle.Clock);
        Assert.Equal(CharacterPackage.ClipSlot("fenn", "idle"), idle.Source);

        Assert.True(CharacterPackage.TryVerb(package, MoveBones.Verb.ChargeSwing, out var charge));
        Assert.True(CharacterPackage.IsReady(charge));
        Assert.Equal(CharacterPackage.ChargeClock, charge.Clock);
        Assert.Equal(CharacterPackage.ClipSlot("fenn", "chargeSwing"), charge.Source);
        Assert.Equal(CharacterPackage.PlayerClipSlot("fenn", "chargeSwing"), charge.PlayerSource);

        Assert.True(CharacterPackage.TryVerb(package, MoveBones.Verb.Swing, out var swing));
        Assert.True(CharacterPackage.IsReady(swing));
        Assert.Equal(CharacterPackage.PoseClock, swing.Clock);
        Assert.Equal(CharacterPackage.ClipSlot("fenn", "swing"), swing.Source);
        Assert.Equal(CharacterPackage.PlayerClipSlot("fenn", "swing"), swing.PlayerSource);
        Assert.True(CharacterPackage.TryMarker(swing, MoveBones.ClipEvent.Contact, out var contact));
        Assert.Equal(MoveBones.SwingContact, contact, 6);

        foreach (var verb in new[] { MoveBones.Verb.Run, MoveBones.Verb.Pitch, MoveBones.Verb.Scoop, MoveBones.Verb.Throw })
        {
            Assert.True(CharacterPackage.TryVerb(package, verb, out var slot));
            Assert.Equal(CharacterPackage.Fallback, slot.Readiness);
        }

        var repo = Directory.GetParent(_content.Root)!.FullName;
        foreach (var clip in new[] { "chargeSwing", "swing" })
        {
            var author = Path.Combine(repo, "unity", CharacterPackage.ClipSlot("fenn", clip));
            var player = Path.Combine(repo, "unity", CharacterPackage.PlayerClipSlot("fenn", clip));
            Assert.Equal(File.ReadAllBytes(author), File.ReadAllBytes(player));
        }

        var dcc = File.ReadAllText(Path.Combine(repo, "tools", "blender", "hero_fenn.py"));
        Assert.Contains("make_batting_action", dcc, StringComparison.Ordinal);
        Assert.Contains("TRACK_NEGATIVE_Y", dcc, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeVerbSelectionUsesTheManifestInsteadOfAPackageSwitch()
    {
        var run = new PackageVerbSlot(
            "run", "custom-run.fbx", "player-run.fbx", "stride", true,
            CharacterPackage.WorldClock, [new PackageTimingMarker("FootPlant", 0.12)],
            CharacterPackage.Ready, CharacterPackage.CharacterMotionFallback);
        var package = new CharacterPackageSpec("next", "next.controller", "player.controller", [run]);

        Assert.True(CharacterPackage.TryVerb(package, MoveBones.Verb.Run, out var selected));
        Assert.Equal("custom-run.fbx", selected.Source);
        Assert.Equal("stride", selected.Clip);
        Assert.True(CharacterPackage.TryMarker(selected, MoveBones.ClipEvent.FootPlant, out var plant));
        Assert.Equal(0.12, plant);
    }

    [Fact]
    public void ReadyVerbMarkerMustMatchTheGameplayBallEvent()
    {
        Assert.True(_content.Art.TryPackage("fenn", out var package));
        var verbs = package.Verbs.Select(slot =>
            slot.Verb.Equals("pitch", StringComparison.OrdinalIgnoreCase)
                ? slot with
                {
                    Source = CharacterPackage.ClipSlot("fenn", "pitch"),
                    PlayerSource = CharacterPackage.PlayerClipSlot("fenn", "pitch"),
                    Readiness = CharacterPackage.Ready,
                    Markers = [new PackageTimingMarker("Release", 0.70)]
                }
                : slot).ToArray();
        var mismatched = package with { Verbs = verbs };
        var skin = _content.Art.SkinOf(_content.Must("fenn"));

        var errors = CharacterPackage.ValidateFiles(_content.Root, skin, mismatched);

        Assert.Contains(errors, error => error.Contains("Release marker 0.7 must match gameplay 0.42"));
    }

    [Fact]
    public void PackageManifestReportsDuplicateEmptyAndNullRowsWithoutCrashing()
    {
        var manifest = """
            {
              "packages": [
                null,
                { "id": "", "verbs": [] },
                {
                  "id": "fenn",
                  "controller": "fenn.controller",
                  "playerController": "player.controller",
                  "verbs": [null, { "verb": "idle", "markers": [null] }]
                },
                { "id": "FENN", "verbs": [] }
              ]
            }
            """;

        var art = LoadArtWithManifest(manifest);

        Assert.Contains(art.PackageErrors, error => error.Contains("packages[0] must be an object"));
        Assert.Contains(art.PackageErrors, error => error.Contains("packages[1] id is required"));
        Assert.Contains(art.PackageErrors, error => error.Contains("verbs[0] must be an object"));
        Assert.Contains(art.PackageErrors, error => error.Contains("markers[0] must be an object"));
        Assert.Contains(art.PackageErrors, error => error.Contains("duplicates package id FENN"));
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

    string LoadManifestSource() => Path.Combine(_content.Root, "art");

    ArtCatalog LoadArtWithManifest(string manifest)
    {
        var temp = Path.Combine(Path.GetTempPath(), "gs-package-manifest-" + Guid.NewGuid().ToString("N"));
        var art = Path.Combine(temp, "art");
        Directory.CreateDirectory(art);
        try
        {
            foreach (var source in Directory.GetFiles(LoadManifestSource(), "*.json"))
            {
                if (Path.GetFileName(source).Equals("character-packages.json", StringComparison.OrdinalIgnoreCase))
                    continue;
                File.Copy(source, Path.Combine(art, Path.GetFileName(source)));
            }
            File.WriteAllText(Path.Combine(art, "character-packages.json"), manifest);
            return ArtCatalog.Load(temp);
        }
        finally
        {
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }
}
