namespace GrandSluggers.Sim;

public readonly record struct RigBoneMap(string Id, IReadOnlyList<string> Bones, IReadOnlyList<string> Events, string Slot);

/// <summary>One clip file slot. Left-handed takes sit next to the right-handed file as <c>{id}-L</c>.</summary>
public readonly record struct ClipSlot(
    string Id, bool Loop, bool Handed, IReadOnlyList<string> Events, string Slot, string PlayerSlot,
    double ContactAt, double ReleaseAt, double FootPlantAt, double FinishAt = 0);

public readonly record struct SkinSlot(
    string Id, string BodyType, bool Captain, IReadOnlyList<string> Extras, string? Portrait, string Palette);

/// <summary>An accessory mesh in the extras kit, authored in its socket bone's space.</summary>
public readonly record struct ExtraSlot(string Id, string Bone, IReadOnlyList<string> Hides);

public readonly record struct NamedSlot(string Id, string Slot, string Kind, bool Authored = false);

public readonly record struct ParkKitSlot(string Id, string Slot, bool Placed);

public sealed class ArtCatalog
{
    public const string ExtrasKitSlot = "Assets/Art/Characters/SharedRig/extras.fbx";
    public const string ExtrasKitPlayerSlot = "Assets/Resources/Art/Characters/SharedRig/extras.fbx";

    ArtCatalog(
        RigBoneMap rig,
        IReadOnlyList<ClipSlot> clips,
        IReadOnlyDictionary<string, SkinSlot> skins,
        IReadOnlyDictionary<string, ExtraSlot> extras,
        IReadOnlyList<NamedSlot> vfx,
        IReadOnlyList<NamedSlot> audio,
        IReadOnlyList<NamedSlot> materials,
        IReadOnlyList<ParkKitSlot> parks,
        IReadOnlyList<string> folders)
    {
        Rig = rig;
        Clips = clips;
        Skins = skins;
        Extras = extras;
        Vfx = vfx;
        Audio = audio;
        Materials = materials;
        Parks = parks;
        Folders = folders;
    }

    public RigBoneMap Rig { get; }
    public IReadOnlyList<ClipSlot> Clips { get; }
    public IReadOnlyDictionary<string, SkinSlot> Skins { get; }
    public IReadOnlyDictionary<string, ExtraSlot> Extras { get; }
    public IReadOnlyList<NamedSlot> Vfx { get; }
    public IReadOnlyList<NamedSlot> Audio { get; }
    public IReadOnlyList<NamedSlot> Materials { get; }
    public IReadOnlyList<ParkKitSlot> Parks { get; }
    public IReadOnlyList<string> Folders { get; }

    public SkinSlot SkinOf(Character who)
    {
        if (Skins.TryGetValue(who.Id, out var skin)) return skin;
        return new SkinSlot(who.Id, Silhouette.BodyType(who), false, [], null, who.Faction);
    }

    public bool TryClip(string id, out ClipSlot clip)
    {
        clip = Clips.FirstOrDefault(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return !string.IsNullOrEmpty(clip.Id);
    }

    public bool TryExtra(string id, out ExtraSlot extra) => Extras.TryGetValue(id, out extra);

    public bool TryVfx(string id, out NamedSlot slot)
    {
        slot = Vfx.FirstOrDefault(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return !string.IsNullOrEmpty(slot.Id);
    }

    public bool TryAudio(string id, out NamedSlot slot)
    {
        slot = Audio.FirstOrDefault(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return !string.IsNullOrEmpty(slot.Id);
    }

    public bool TryPark(string id, out ParkKitSlot kit)
    {
        kit = Parks.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return !string.IsNullOrEmpty(kit.Id);
    }

    /// <summary>Authoring and player FBX paths for a clip and hand, relative to <c>unity/</c>.</summary>
    public static (string Slot, string PlayerSlot) ClipFiles(ClipSlot clip, Hand hand)
    {
        var suffix = clip.Handed && hand == Hand.L ? "-L" : "";
        return (clip.Slot + suffix + ".fbx", clip.PlayerSlot + suffix + ".fbx");
    }

    static string Unity(string dataRoot, string slot)
    {
        var repo = Directory.GetParent(Path.GetFullPath(dataRoot))?.FullName ?? dataRoot;
        return Path.GetFullPath(Path.Combine(repo, "unity", slot.Replace('/', Path.DirectorySeparatorChar)));
    }

    public IReadOnlyList<string> Validate(ContentCatalog content)
    {
        var errors = new List<string>();
        foreach (var bone in new[] { "root", "pelvis", "spine", "torso", "neck", "head", "lClavicle", "rClavicle", "lUpper", "lFore", "lWrist", "rUpper", "rFore", "rWrist", "lThigh", "lShin", "lFoot", "rThigh", "rShin", "rFoot", "lGlove", "rGlove", "lRelease", "rRelease", "bat", "glove" })
        {
            if (!Rig.Bones.Any(b => b.Equals(bone, StringComparison.OrdinalIgnoreCase)))
                errors.Add("rig missing bone " + bone);
        }
        foreach (var ev in new[] { "Contact", "Release", "FootPlant" })
        {
            if (!Rig.Events.Any(e => e.Equals(ev, StringComparison.OrdinalIgnoreCase)))
                errors.Add("rig missing event " + ev);
        }
        var rigFbx = Unity(content.Root, Rig.Slot);
        if (!File.Exists(rigFbx)) errors.Add("rig FBX missing " + Rig.Slot);
        else
        {
            var player = Unity(content.Root, "Assets/Resources/" + Rig.Slot["Assets/".Length..]);
            if (!File.Exists(player) || !SameBytes(rigFbx, player))
                errors.Add("rig player copy missing or different " + Rig.Slot);
        }

        // Every take the sim can ask for, both hands where handed, authoring and player copies identical.
        var listed = Clips.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var need in Motion.Clips)
        {
            if (!listed.TryGetValue(need.Id, out var clip))
            {
                errors.Add("clip catalog missing " + need.Id);
                continue;
            }
            if (clip.Loop != need.Loop) errors.Add("clip " + need.Id + " loop must be " + need.Loop);
            if (clip.Handed != need.Handed) errors.Add("clip " + need.Id + " handed must be " + need.Handed);
            var mark = need.Mark?.ToString();
            if (mark != null && !clip.Events.Contains(mark, StringComparer.OrdinalIgnoreCase))
                errors.Add("clip " + need.Id + " needs " + mark + " event");
            if (mark == null && clip.Events.Count > 0)
                errors.Add("clip " + need.Id + " has no marker in Motion but lists events");
            var at = need.Mark switch
            {
                Motion.ClipEvent.Contact => clip.ContactAt,
                Motion.ClipEvent.Release => clip.ReleaseAt,
                Motion.ClipEvent.FootPlant => clip.FootPlantAt,
                _ => 0
            };
            if (need.Mark != null && Math.Abs(at - need.MarkAt) > 1e-6)
                errors.Add("clip " + need.Id + " " + mark + " at " + at + " must be " + need.MarkAt);
            // A held finish (#583) is the take's last key: the catalog, Motion and the bake agree on its second.
            if (Math.Abs(clip.FinishAt - need.FinishAt) > 1e-6)
                errors.Add("clip " + need.Id + " finishAt " + clip.FinishAt + " must be " + need.FinishAt);
            if (need.FinishAt > 0 && Math.Abs(need.FinishAt - need.Duration) > 1e-6)
                errors.Add("clip " + need.Id + " held finish " + need.FinishAt + " must be its last second " + need.Duration);
            foreach (var hand in clip.Handed ? new[] { Hand.R, Hand.L } : new[] { Hand.R })
            {
                var (slot, playerSlot) = ClipFiles(clip, hand);
                var file = Unity(content.Root, slot);
                var player = Unity(content.Root, playerSlot);
                if (!File.Exists(file) || new FileInfo(file).Length < 4096)
                    errors.Add("clip take missing " + slot);
                else
                {
                    if (!File.Exists(player) || !SameBytes(file, player))
                        errors.Add("clip player copy missing or different " + playerSlot);
                    // FBX node names are uncompressed strings. Refuse an older
                    // skeleton even if its authoring/player copies agree.
                    var nodes = System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(file));
                    foreach (var bone in Rig.Bones)
                        if (!nodes.Contains(bone, StringComparison.Ordinal))
                            errors.Add("clip " + slot + " was not baked for rig joint " + bone);
                }
            }
        }
        foreach (var clip in Clips)
        {
            if (!Motion.TryClip(clip.Id, out _))
                errors.Add("clip " + clip.Id + " is not a Motion clip");
            foreach (var ev in clip.Events)
            {
                if (!Rig.Events.Any(e => e.Equals(ev, StringComparison.OrdinalIgnoreCase)))
                    errors.Add("clip " + clip.Id + " event " + ev + " not on rig");
            }
            if (string.IsNullOrWhiteSpace(clip.Slot) || string.IsNullOrWhiteSpace(clip.PlayerSlot))
                errors.Add("clip " + clip.Id + " missing slot");
        }

        foreach (var id in Silhouette.Captains)
        {
            if (!Skins.TryGetValue(id, out var skin))
            {
                errors.Add("captain skin missing " + id);
                continue;
            }
            if (!skin.Captain) errors.Add("skin " + id + " should be captain");
            if (!skin.BodyType.Equals(id, StringComparison.OrdinalIgnoreCase))
                errors.Add("skin " + id + " bodyType should be self");
            if (skin.Extras.Count == 0) errors.Add("captain skin " + id + " needs extras");
            if (string.IsNullOrWhiteSpace(skin.Portrait)) errors.Add("captain skin " + id + " needs portrait slot");
        }

        foreach (var who in content.Characters.Values)
        {
            var skin = SkinOf(who);
            var expected = Silhouette.BodyType(who);
            if (!skin.BodyType.Equals(expected, StringComparison.OrdinalIgnoreCase))
                errors.Add("skin " + who.Id + " bodyType " + skin.BodyType + " != " + expected);
            if (!who.Captain && skin.Extras.Count > 0)
                errors.Add("role skin " + who.Id + " must not grow captain extras");
            foreach (var extra in skin.Extras)
                if (!Extras.ContainsKey(extra))
                    errors.Add("skin " + who.Id + " extra " + extra + " is not in extras.json");
        }

        var kit = Unity(content.Root, ExtrasKitSlot);
        var kitNames = File.Exists(kit) ? System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(kit)) : "";
        if (kitNames.Length == 0) errors.Add("extras kit missing " + ExtrasKitSlot);
        else
        {
            var player = Unity(content.Root, ExtrasKitPlayerSlot);
            if (!File.Exists(player) || !SameBytes(kit, player))
                errors.Add("extras kit player copy missing or different " + ExtrasKitPlayerSlot);
        }
        foreach (var extra in Extras.Values)
        {
            if (!Rig.Bones.Any(b => b.Equals(extra.Bone, StringComparison.OrdinalIgnoreCase)))
                errors.Add("extra " + extra.Id + " bone " + extra.Bone + " not on rig");
            if (kitNames.Length > 0 && !kitNames.Contains(extra.Id, StringComparison.Ordinal))
                errors.Add("extra " + extra.Id + " is not a mesh in " + ExtrasKitSlot);
        }
        foreach (var prop in new[] { GearMesh.HittingBatVisual(), "glove-brown", "glove-brown-R", "glove-gold", "glove-gold-R", "baseball" })
            if (kitNames.Length > 0 && !kitNames.Contains(prop, StringComparison.Ordinal))
                errors.Add("prop " + prop + " is not a mesh in " + ExtrasKitSlot);

        foreach (var park in content.Parks.Keys)
        {
            if (!TryPark(park, out _))
                errors.Add("park kit missing " + park);
        }

        foreach (var need in new[] { "bat-perfect", "bat-solid", "bat-cheap", "glove", "throw", "crowd-bed", "crowd-swell" })
        {
            if (!TryAudio(need, out _)) errors.Add("audio missing " + need);
        }
        foreach (var cap in Silhouette.Captains)
        {
            if (!TryAudio("vo-" + cap, out _)) errors.Add("audio missing vo-" + cap);
        }
        var wavs = AuthoredAudio.Ids(content.Root);
        foreach (var ev in Audio)
        {
            var bus = ev.Kind ?? "";
            if (!bus.Equals("sfx", StringComparison.OrdinalIgnoreCase)
                && !bus.Equals("crowd", StringComparison.OrdinalIgnoreCase)
                && !bus.Equals("vo", StringComparison.OrdinalIgnoreCase))
                errors.Add("audio " + ev.Id + " unknown bus " + bus);
            if (ev.Authored && !wavs.Contains(ev.Id))
                errors.Add("authored audio missing wav " + ev.Id);
        }

        foreach (var need in new[] { "puff", "fireworks", "buddy-flash", "throw-trail-good" })
        {
            if (!TryVfx(need, out _)) errors.Add("vfx missing " + need);
        }
        foreach (var who in content.Characters.Values)
        {
            if (!who.Captain) continue;
            if (!TryVfx(who.StarPitch, out _))
                errors.Add("vfx missing captain pitch " + who.Id + " " + who.StarPitch);
            if (!TryVfx(who.StarSwing, out _))
                errors.Add("vfx missing captain swing " + who.Id + " " + who.StarSwing);
        }

        if (Folders.Count == 0) errors.Add("art folder list empty");
        return errors;
    }

    static bool SameBytes(string a, string b)
    {
        var fa = new FileInfo(a);
        var fb = new FileInfo(b);
        if (fa.Length != fb.Length) return false;
        return File.ReadAllBytes(a).AsSpan().SequenceEqual(File.ReadAllBytes(b));
    }

    public static ArtCatalog Load(string dataRoot)
    {
        var json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        var art = Path.Combine(dataRoot, "art");

        var rigDto = Read<RigFile>(Path.Combine(art, "rig.json"), json);
        var rig = new RigBoneMap(rigDto.Id, rigDto.Bones ?? [], rigDto.Events ?? [], rigDto.Slot ?? "");

        var clipDto = Read<ClipsFile>(Path.Combine(art, "clips.json"), json);
        var clips = (clipDto.Clips ?? []).Select(c =>
            new ClipSlot(c.Id, c.Loop, c.Handed, c.Events ?? [], c.Slot, c.PlayerSlot,
                c.ContactAt, c.ReleaseAt, c.FootPlantAt, c.FinishAt)).ToList();

        var skinDto = Read<SkinsFile>(Path.Combine(art, "skins.json"), json);
        var skins = new Dictionary<string, SkinSlot>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in skinDto.Skins ?? [])
            skins[s.Id] = new SkinSlot(s.Id, s.BodyType, s.Captain, s.Extras ?? [], s.Portrait, s.Palette);

        var extras = new Dictionary<string, ExtraSlot>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in Read<ExtrasFile>(Path.Combine(art, "extras.json"), json).Extras ?? [])
            extras[e.Id] = new ExtraSlot(e.Id, e.Bone, e.Hides ?? []);

        var vfx = (Read<EventsFile>(Path.Combine(art, "vfx.json"), json).Events ?? [])
            .Select(e => new NamedSlot(e.Id, e.Slot, e.Kind ?? "")).ToList();
        var audio = (Read<EventsFile>(Path.Combine(art, "audio.json"), json).Events ?? [])
            .Select(e => new NamedSlot(e.Id, e.Slot, e.Bus ?? e.Kind ?? "", e.Authored)).ToList();
        var mats = (Read<MatsFile>(Path.Combine(art, "materials.json"), json).Slots ?? [])
            .Select(e => new NamedSlot(e.Id, e.Slot, e.Shader ?? "")).ToList();
        var parks = (Read<ParksFile>(Path.Combine(art, "parks.json"), json).Kits ?? [])
            .Select(p => new ParkKitSlot(p.Id, p.Slot, p.Placed)).ToList();
        var folders = Read<FoldersFile>(Path.Combine(art, "folders.json"), json).Folders ?? [];

        return new ArtCatalog(rig, clips, skins, extras, vfx, audio, mats, parks, folders);
    }

    static T Read<T>(string path, JsonSerializerOptions json)
    {
        var dto = JsonSerializer.Deserialize<T>(File.ReadAllText(path), json);
        return dto ?? throw new InvalidDataException("Bad art file " + path);
    }

    sealed class RigFile
    {
        public string Id { get; set; } = "";
        public string Slot { get; set; } = "";
        public List<string>? Bones { get; set; }
        public List<string>? Events { get; set; }
    }

    sealed class ClipsFile { public List<ClipDto>? Clips { get; set; } }
    sealed class ClipDto
    {
        public string Id { get; set; } = "";
        public bool Loop { get; set; }
        public bool Handed { get; set; }
        public List<string>? Events { get; set; }
        public string Slot { get; set; } = "";
        public string PlayerSlot { get; set; } = "";
        public double ContactAt { get; set; }
        public double FinishAt { get; set; }
        public double ReleaseAt { get; set; }
        public double FootPlantAt { get; set; }
    }

    sealed class SkinsFile { public List<SkinDto>? Skins { get; set; } }
    sealed class SkinDto
    {
        public string Id { get; set; } = "";
        public string BodyType { get; set; } = "";
        public bool Captain { get; set; }
        public List<string>? Extras { get; set; }
        public string? Portrait { get; set; }
        public string Palette { get; set; } = "";
    }

    sealed class ExtrasFile { public List<ExtraDto>? Extras { get; set; } }
    sealed class ExtraDto
    {
        public string Id { get; set; } = "";
        public string Bone { get; set; } = "";
        public List<string>? Hides { get; set; }
    }

    sealed class EventsFile { public List<EventDto>? Events { get; set; } }
    sealed class EventDto
    {
        public string Id { get; set; } = "";
        public string Slot { get; set; } = "";
        public string? Kind { get; set; }
        public string? Bus { get; set; }
        public string? Shader { get; set; }
        public bool Authored { get; set; }
    }

    sealed class MatsFile { public List<EventDto>? Slots { get; set; } }
    sealed class ParksFile { public List<ParkDto>? Kits { get; set; } }
    sealed class ParkDto
    {
        public string Id { get; set; } = "";
        public string Slot { get; set; } = "";
        public bool Placed { get; set; }
    }

    sealed class FoldersFile { public List<string>? Folders { get; set; } }
}
