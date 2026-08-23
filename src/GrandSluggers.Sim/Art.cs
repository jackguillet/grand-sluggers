namespace GrandSluggers.Sim;

public readonly record struct RigBoneMap(string Id, IReadOnlyList<string> Bones, IReadOnlyList<string> Events);

public readonly record struct ClipSlot(
    string Id, string Verb, bool Loop, IReadOnlyList<string> Events, string Slot,
    double ContactAt, double ReleaseAt, double FootPlantAt);

public readonly record struct SkinSlot(
    string Id, string BodyType, bool Captain, IReadOnlyList<string> Extras, string? Portrait, string Palette);

public readonly record struct NamedSlot(string Id, string Slot, string Kind);

public readonly record struct ParkKitSlot(string Id, string Slot, bool Placed);

public sealed class ArtCatalog
{
    ArtCatalog(
        RigBoneMap rig,
        IReadOnlyList<ClipSlot> clips,
        IReadOnlyDictionary<string, SkinSlot> skins,
        IReadOnlyList<NamedSlot> vfx,
        IReadOnlyList<NamedSlot> audio,
        IReadOnlyList<NamedSlot> materials,
        IReadOnlyList<ParkKitSlot> parks,
        IReadOnlyList<string> folders)
    {
        Rig = rig;
        Clips = clips;
        Skins = skins;
        Vfx = vfx;
        Audio = audio;
        Materials = materials;
        Parks = parks;
        Folders = folders;
    }

    public RigBoneMap Rig { get; }
    public IReadOnlyList<ClipSlot> Clips { get; }
    public IReadOnlyDictionary<string, SkinSlot> Skins { get; }
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

    public IReadOnlyList<string> Validate(ContentCatalog content)
    {
        var errors = new List<string>();
        foreach (var bone in new[] { "torso", "head", "lUpper", "lFore", "rUpper", "rFore", "lThigh", "lShin", "rThigh", "rShin", "bat", "glove" })
        {
            if (!Rig.Bones.Any(b => b.Equals(bone, StringComparison.OrdinalIgnoreCase)))
                errors.Add("rig missing bone " + bone);
        }
        foreach (var ev in new[] { "Contact", "Release", "FootPlant" })
        {
            if (!Rig.Events.Any(e => e.Equals(ev, StringComparison.OrdinalIgnoreCase)))
                errors.Add("rig missing event " + ev);
        }

        var clipIds = Clips.Select(c => c.Id.ToLowerInvariant()).ToHashSet();
        foreach (var need in MoveBones.Clips)
        {
            if (!clipIds.Contains(need.ToLowerInvariant()))
                errors.Add("clip catalog missing " + need);
        }
        foreach (var clip in Clips)
        {
            if (!Enum.TryParse<MoveBones.Verb>(clip.Verb, ignoreCase: true, out var verb))
                errors.Add("clip " + clip.Id + " unknown verb " + clip.Verb);
            else
            {
                var listed = MoveBones.ClipList.FirstOrDefault(c => c.Id.Equals(clip.Id, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(listed.Id) && listed.Verb != verb)
                    errors.Add("clip " + clip.Id + " verb mismatch");
            }
            foreach (var ev in clip.Events)
            {
                if (!Rig.Events.Any(e => e.Equals(ev, StringComparison.OrdinalIgnoreCase)))
                    errors.Add("clip " + clip.Id + " event " + ev + " not on rig");
            }
            if (string.IsNullOrWhiteSpace(clip.Slot))
                errors.Add("clip " + clip.Id + " missing slot");
            if (clip.Events.Contains("Contact") && clip.ContactAt <= 0)
                errors.Add("clip " + clip.Id + " Contact needs contactAt");
            if (clip.Events.Contains("Release") && clip.ReleaseAt <= 0)
                errors.Add("clip " + clip.Id + " Release needs releaseAt");
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
        }

        foreach (var park in content.Parks.Keys)
        {
            if (!TryPark(park, out _))
                errors.Add("park kit missing " + park);
        }

        foreach (var need in new[] { "bat-perfect", "bat-solid", "bat-cheap", "glove", "throw", "crowd-bed" })
        {
            if (!TryAudio(need, out _)) errors.Add("audio missing " + need);
        }
        foreach (var cap in Silhouette.Captains)
        {
            if (!TryAudio("vo-" + cap, out _)) errors.Add("audio missing vo-" + cap);
        }

        foreach (var need in new[] { "puff", "fireworks", "buddy-flash", "throw-trail-good" })
        {
            if (!TryVfx(need, out _)) errors.Add("vfx missing " + need);
        }

        if (Folders.Count == 0) errors.Add("art folder list empty");
        return errors;
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
        var rig = new RigBoneMap(rigDto.Id, rigDto.Bones ?? [], rigDto.Events ?? []);

        var clipDto = Read<ClipsFile>(Path.Combine(art, "clips.json"), json);
        var clips = (clipDto.Clips ?? []).Select(c =>
            new ClipSlot(c.Id, c.Verb, c.Loop, c.Events ?? [], c.Slot, c.ContactAt, c.ReleaseAt, c.FootPlantAt)).ToList();

        var skinDto = Read<SkinsFile>(Path.Combine(art, "skins.json"), json);
        var skins = new Dictionary<string, SkinSlot>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in skinDto.Skins ?? [])
            skins[s.Id] = new SkinSlot(s.Id, s.BodyType, s.Captain, s.Extras ?? [], s.Portrait, s.Palette);

        var vfx = (Read<EventsFile>(Path.Combine(art, "vfx.json"), json).Events ?? [])
            .Select(e => new NamedSlot(e.Id, e.Slot, e.Kind ?? "")).ToList();
        var audio = (Read<EventsFile>(Path.Combine(art, "audio.json"), json).Events ?? [])
            .Select(e => new NamedSlot(e.Id, e.Slot, e.Bus ?? e.Kind ?? "")).ToList();
        var mats = (Read<MatsFile>(Path.Combine(art, "materials.json"), json).Slots ?? [])
            .Select(e => new NamedSlot(e.Id, e.Slot, e.Shader ?? "")).ToList();
        var parks = (Read<ParksFile>(Path.Combine(art, "parks.json"), json).Kits ?? [])
            .Select(p => new ParkKitSlot(p.Id, p.Slot, p.Placed)).ToList();
        var folders = Read<FoldersFile>(Path.Combine(art, "folders.json"), json).Folders ?? [];

        return new ArtCatalog(rig, clips, skins, vfx, audio, mats, parks, folders);
    }

    static T Read<T>(string path, JsonSerializerOptions json)
    {
        var dto = JsonSerializer.Deserialize<T>(File.ReadAllText(path), json);
        return dto ?? throw new InvalidDataException("Bad art file " + path);
    }

    sealed class RigFile
    {
        public string Id { get; set; } = "";
        public List<string>? Bones { get; set; }
        public List<string>? Events { get; set; }
    }

    sealed class ClipsFile { public List<ClipDto>? Clips { get; set; } }
    sealed class ClipDto
    {
        public string Id { get; set; } = "";
        public string Verb { get; set; } = "";
        public bool Loop { get; set; }
        public List<string>? Events { get; set; }
        public string Slot { get; set; } = "";
        public double ContactAt { get; set; }
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

    sealed class EventsFile { public List<EventDto>? Events { get; set; } }
    sealed class EventDto
    {
        public string Id { get; set; } = "";
        public string Slot { get; set; } = "";
        public string? Kind { get; set; }
        public string? Bus { get; set; }
        public string? Shader { get; set; }
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
