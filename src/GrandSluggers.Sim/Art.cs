using System.Text.Json.Nodes;

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

/// <summary>
/// One park's kit (FD-16, FR-13; <c>data/art/parks.json</c>): its art folder, whether art is placed, and its kit slots —
/// each <see cref="ParkKitSlots.All"/> slot named by a builder from <see cref="ParkKitSlots.Builders"/>, or empty (null),
/// which draws the greybox.
/// </summary>
public readonly record struct ParkKitSlot(
    string Id, string Slot, bool Placed, IReadOnlyDictionary<string, string?>? Slots = null, ParkPalette? Palette = null)
{
    /// <summary>The builder that fills <paramref name="slot"/>, or null when the slot is empty.</summary>
    public string? Filler(string slot) => Slots != null && Slots.TryGetValue(slot, out var f) ? f : null;

    /// <summary>The slot is filled by exactly this builder.</summary>
    public bool Fills(string slot, string builder) => string.Equals(Filler(slot), builder, StringComparison.OrdinalIgnoreCase);

    /// <summary>The slots nobody fills yet, in <see cref="ParkKitSlots.All"/> order: they draw the greybox.</summary>
    public IReadOnlyList<string> Empty
    {
        get
        {
            var empty = new List<string>();
            foreach (var slot in ParkKitSlots.All)
                if (Filler(slot) is null) empty.Add(slot);
            return empty;
        }
    }
}

/// <summary>
/// The kit slots a park fills (FD-16 B, FR-13): a closed set, and for each slot the named builders presentation owns. A
/// park names a builder per slot or leaves it empty (JSON <c>null</c>), and an empty slot draws the greybox. A new builder
/// is a row here and its code; a park never picks a look by its id (FR-04).
///
/// <para>
/// Harbor fills the slots its kit draws today (<c>HarborKit</c>): the striped lawn, the dugouts, the padded wall with ads,
/// the scoreboard, the bowl of stands, the town and the night fireworks. The Harbor pieces stand on the Harbor lawn: a park
/// that names one must name the lawn.
/// </para>
///
/// <para>
/// <b>The other parks are the greybox in their own colors</b> (FR-13): their backdrop, props and night slots are empty, and
/// their stands slot names <see cref="KitBowl"/>, one bowl of bleachers for every park, painted by the <c>stands</c> block of
/// the park's palette. No park draws hand-built per-park dress. <c>hazardActors</c> names
/// <see cref="ToyActors"/> (each hazard type's toy, <see cref="HazardActors"/>) or is empty, which draws the pattern
/// greybox; either way every acting instance draws its ring at the sim's disc.
/// </para>
///
/// <para>
/// <b>Light and sky are data</b> (F6-c): their fillers are not code builders but rows of <c>data/art/looks.json</c>
/// (<see cref="ParkLooks"/>), and every park names one of each. Each row also carries a <c>palette</c> — the colors its
/// greybox draws in.
/// </para>
/// </summary>
public static class ParkKitSlots
{
    public const string Lawn = "lawn";
    public const string Dugouts = "dugouts";
    public const string Wall = "wall";
    public const string Scoreboard = "scoreboard";
    public const string Stands = "stands";
    public const string Backdrop = "backdrop";
    public const string Night = "night";
    public const string Light = "light";
    public const string Sky = "sky";
    public const string HazardActors = "hazardActors";
    public const string Props = "props";

    public const string HarborLawn = "harbor-lawn";
    public const string HarborDugouts = "harbor-dugouts";
    public const string HarborWall = "harbor-ads";
    public const string HarborScoreboard = "harbor-scoreboard";
    public const string HarborStands = "harbor-bowl";
    public const string HarborTown = "harbor-town";
    public const string HarborFireworks = "harbor-fireworks";

    public const string ToyActors = "toy-actors";

    /// <summary>The park-neutral bowl of bleachers: tiered risers, seat sections and crowd, painted by the palette's <c>stands</c> block.</summary>
    public const string KitBowl = "kit-bowl";

    /// <summary>Every slot, in the order <c>cli art</c> prints them.</summary>
    public static IReadOnlyList<string> All { get; } =
        [Lawn, Dugouts, Wall, Scoreboard, Stands, Backdrop, Night, Props, Light, Sky, HazardActors];

    /// <summary>The builders each slot may name. An empty list is a slot only the greybox fills so far.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Builders { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [Lawn] = [HarborLawn],
            [Dugouts] = [HarborDugouts],
            [Wall] = [HarborWall],
            [Scoreboard] = [HarborScoreboard],
            [Stands] = [HarborStands, KitBowl],
            [Backdrop] = [HarborTown],
            [Night] = [HarborFireworks],
            [Props] = [], // no park names props; a non-Harbor park draws the greybox
            [Light] = [], // rows of data/art/looks.json, checked against the catalog's looks
            [Sky] = [],
            [HazardActors] = [ToyActors],
        };

    /// <summary>The builders that are pieces of the Harbor kit, which stand on its lawn.</summary>
    static readonly HashSet<string> HarborPieces =
        [HarborDugouts, HarborWall, HarborScoreboard, HarborStands, HarborTown, HarborFireworks];

    /// <summary>
    /// What is wrong with one park's slots: a missing or unknown slot, an unknown builder, a Harbor piece off the Harbor lawn,
    /// and with <paramref name="looks"/>, a light or sky that is not a row of it, or none, and a missing palette.
    /// </summary>
    public static IReadOnlyList<string> Validate(ParkKitSlot kit, ParkLooks? looks = null)
    {
        var errors = new List<string>();
        if (kit.Slots is null)
        {
            errors.Add("park kit " + kit.Id + " names no slots");
            return errors;
        }
        foreach (var key in kit.Slots.Keys)
            if (!Builders.ContainsKey(key))
                errors.Add("park kit " + kit.Id + " slot " + key + " is not a kit slot");
        foreach (var slot in All)
        {
            if (!kit.Slots.TryGetValue(slot, out var builder))
            {
                errors.Add("park kit " + kit.Id + " must name slot " + slot + " (null leaves it empty)");
                continue;
            }
            if (slot is Light or Sky)
            {
                // Rows of looks.json, not code builders: checked when the table is at hand.
                if (looks is null) continue;
                var rows = slot == Light ? looks.Lights.Keys : looks.Skies.Keys;
                if (builder is null) errors.Add("park kit " + kit.Id + " must name a " + slot + " from looks.json");
                else if (!rows.Contains(builder)) errors.Add("park kit " + kit.Id + " slot " + slot + " names " + builder + ", which is not a " + slot + " in looks.json");
                continue;
            }
            if (builder is null) continue;
            if (!Builders[slot].Contains(builder))
                errors.Add("park kit " + kit.Id + " slot " + slot + " names " + builder + ", which is not a " + slot + " builder");
            if (HarborPieces.Contains(builder) && !kit.Fills(Lawn, HarborLawn))
                errors.Add("park kit " + kit.Id + " slot " + slot + " names " + builder + ", a Harbor piece, but its lawn is not " + HarborLawn);
        }
        if (looks is not null && kit.Palette is null)
            errors.Add("park kit " + kit.Id + " names no palette");
        if (kit.Fills(Stands, KitBowl) && kit.Palette is { Stands: null })
            errors.Add("park kit " + kit.Id + " slot " + Stands + " names " + KitBowl + ", but its palette names no stands");
        return errors;
    }
}

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
        IReadOnlyList<string> folders,
        ParkLooks looks,
        HazardActors actors)
    {
        Looks = looks;
        Actors = actors;
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
    /// <summary>The accessory FBX the extras are named meshes of (<c>data/art/extras.json</c> <c>slot</c>).</summary>
    public string ExtrasSlot { get; init; } = "";
    public IReadOnlyList<NamedSlot> Vfx { get; }
    public IReadOnlyList<NamedSlot> Audio { get; }
    public IReadOnlyList<NamedSlot> Materials { get; }
    public IReadOnlyList<ParkKitSlot> Parks { get; }
    /// <summary>The named skies and lights a park's kit names (F6-c, <c>data/art/looks.json</c>).</summary>
    public ParkLooks Looks { get; }
    /// <summary>How each hazard type is drawn (F6-d, <c>data/art/hazard-actors.json</c>).</summary>
    public HazardActors Actors { get; }
    /// <summary>Each faction's jersey, accent and skin (<c>data/art/factions.json</c>).</summary>
    public FactionLooks Factions { get; init; } = null!;
    /// <summary>The character toon's bands and rim (CF-7, <c>data/art/toon.json</c>).</summary>
    public ToonLook Toon { get; init; } = null!;
    public IReadOnlyList<string> Folders { get; }

    /// <summary>The motion styles (CH-12, <c>clips.json</c> <c>styles.rows</c>).</summary>
    public IReadOnlyList<MotionStyle> Styles { get; init; } = [];
    /// <summary>The clips every style bakes its own take of (<c>styles.clips</c>).</summary>
    public IReadOnlyList<string> StyledClips { get; init; } = [];
    /// <summary>
    /// The body-class table (<c>data/rules/body-classes.json</c>): a body moves in the style its class's <c>motionStyle</c>
    /// names. <see cref="ContentCatalog.Load"/> sets it from the rules; a role player plays its captain's class.
    /// </summary>
    public BodyClassLibrary? Classes { get; set; }
    public string StyleSlot { get; init; } = "";
    public string StylePlayerSlot { get; init; } = "";
    /// <summary>The bake's receipt (<c>data/art/takes-receipt.json</c>): per take file, its SHA-256 and the contracts it passed.</summary>
    public IReadOnlyDictionary<string, (string Sha, IReadOnlyList<string> Contracts)> Receipt { get; init; } =
        new Dictionary<string, (string, IReadOnlyList<string>)>();
    public bool ReceiptFound { get; init; }
    /// <summary>The walk / run threshold's pursuit profile; <see cref="ContentCatalog.Load"/> sets it from the rules and the feel.</summary>
    public GaitProfile? Gait { get; set; }

    public bool TryStyle(string id, out MotionStyle style)
    {
        style = Styles.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase))!;
        return style is not null;
    }

    /// <summary>
    /// The motion style this character moves in: its body class's <c>motionStyle</c>. Null for a character with no class
    /// (a hand-built fixture) or before the class table is bound; it then plays the shared takes.
    /// </summary>
    public MotionStyle? StyleOf(Character who) =>
        Classes is { } classes && !string.IsNullOrEmpty(who.BodyClass) && classes.Has(who.BodyClass)
        && TryStyle(classes.Of(who.BodyClass).MotionStyle, out var style) ? style : null;

    /// <summary>
    /// Authoring and player FBX paths for a clip, hand and style: the style's own take when it has one
    /// (<c>{styles slot}/{style}/{clip}</c>), else the shared take.
    /// </summary>
    public (string Slot, string PlayerSlot) ClipFiles(ClipSlot clip, Hand hand, MotionStyle? style)
    {
        if (style is null || !style.Owns(clip.Id)) return ClipFiles(clip, hand);
        var suffix = clip.Handed && hand == Hand.L ? "-L" : "";
        return ($"{StyleSlot}/{style.Id}/{clip.Id}{suffix}.fbx", $"{StylePlayerSlot}/{style.Id}/{clip.Id}{suffix}.fbx");
    }

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

    /// <summary>
    /// Art lives in the repository beside the data root, not inside it, so this resolves against the
    /// shipped root: a trial overlay carries rule numbers, never meshes.
    /// </summary>
    static string Unity(DataRoot dataRoot, string slot)
    {
        var repo = Directory.GetParent(dataRoot.Shipped)?.FullName ?? dataRoot.Shipped;
        return Path.GetFullPath(Path.Combine(repo, "unity", slot.Replace('/', Path.DirectorySeparatorChar)));
    }

    public IReadOnlyList<string> Validate(ContentCatalog content)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(ExtrasSlot) || !ExtrasSlot.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            errors.Add("extras.json slot must name the extras FBX; got '" + ExtrasSlot + "'");
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
            // The build (CH-04) is shape keys on the one mesh: a body without them draws every captain neutral.
            var body = System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(rigFbx));
            foreach (var channel in Silhouette.BuildChannels.Concat(Silhouette.StyleChannels))
            foreach (var key in new[] { channel.UpKey, channel.DownKey })
                if (!body.Contains(key, StringComparison.Ordinal))
                    errors.Add("rig FBX " + Rig.Slot + " has no build shape key " + key);
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
                CheckTake(content, slot, playerSlot, [], errors);
            }
        }
        ValidateStyles(content, listed, errors);
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

        foreach (var id in content.CaptainIds)
        {
            if (!Skins.TryGetValue(id, out var skin))
            {
                errors.Add("captain skin missing " + id);
                continue;
            }
            if (!skin.Captain) errors.Add("skin " + id + " should be captain");
            if (!skin.BodyType.Equals(id, StringComparison.OrdinalIgnoreCase))
                errors.Add("skin " + id + " bodyType should be self");
            if (string.IsNullOrWhiteSpace(skin.Portrait)) errors.Add("captain skin " + id + " needs portrait slot");
            // A build outside the shape keys' range would clamp: the captain would draw smaller than the sim measures.
            var spec = Silhouette.Proportions(content, id);
            foreach (var (channel, proportion) in new[]
            {
                (Silhouette.HeadBuild, spec.Head), (Silhouette.ArmsBuild, spec.Arms), (Silhouette.TorsoBuild, spec.Torso)
            })
            {
                var scale = channel.ScaleFor(proportion);
                if (scale < channel.Min - 1e-9 || scale > channel.Max + 1e-9)
                    errors.Add($"captain {id} {channel.Id} build {scale:0.###} is outside the rig's shape keys [{channel.Min}, {channel.Max}]");
            }
        }

        foreach (var who in content.Characters.Values)
        {
            var skin = SkinOf(who);
            var expected = Silhouette.BodyType(who);
            if (!skin.BodyType.Equals(expected, StringComparison.OrdinalIgnoreCase))
                errors.Add("skin " + who.Id + " bodyType " + skin.BodyType + " != " + expected);
            if (skin.Extras.Count > 0)
                errors.Add("skin " + who.Id + " must not list extras until they read as toys");
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
        foreach (var kitRow in Parks)
            errors.AddRange(ParkKitSlots.Validate(kitRow, Looks));
        errors.AddRange(Actors.Validate());
        errors.AddRange(ToonLook.Validate(Materials));
        // Every character's faction has its colors; a faction nobody plays is a stale row.
        var played = content.Characters.Values.Select(c => c.Faction).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var faction in played.OrderBy(f => f, StringComparer.Ordinal))
            if (Factions.Of(faction) is null) errors.Add("faction colors missing for " + faction + " in data/art/factions.json");
        foreach (var faction in Factions.Rows.Keys)
            if (!played.Contains(faction)) errors.Add("faction colors for " + faction + " name a faction no character plays");

        foreach (var need in new[] { "bat-perfect", "bat-solid", "bat-cheap", "glove", "throw", "crowd-bed", "crowd-swell" })
        {
            if (!TryAudio(need, out _)) errors.Add("audio missing " + need);
        }
        foreach (var cap in content.CaptainIds)
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
            // The slot names the file the game loads (AuthoredAudio): one place for a wav, not an empty Unity folder.
            var wav = "data/" + AuthoredAudio.Directory + "/" + ev.Id + ".wav";
            if (!string.Equals(ev.Slot, wav, StringComparison.Ordinal))
                errors.Add("audio " + ev.Id + " slot must be " + wav + "; got " + ev.Slot);
        }

        foreach (var need in new[] { "puff", "fireworks", "buddy-flash", "throw-trail-good", "throw-trail-bad" })
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

    /// <summary>The folder every take file sits under; the receipt names files relative to it.</summary>
    public const string ClipRoot = "Assets/Art/Animation/Clips";

    /// <summary>
    /// One take file: present, its player copy identical, baked on the current rig, and vouched for by the bake's receipt
    /// (the same bytes passed the takes script's per-frame contracts). Returns the contracts the receipt lists, or null.
    /// </summary>
    IReadOnlyList<string>? CheckTake(ContentCatalog content, string slot, string playerSlot, IReadOnlyList<string> mustPass,
        List<string> errors)
    {
        var file = Unity(content.Root, slot);
        var player = Unity(content.Root, playerSlot);
        if (!File.Exists(file) || new FileInfo(file).Length < 4096)
        {
            errors.Add("clip take missing " + slot);
            return null;
        }
        if (!File.Exists(player) || !SameBytes(file, player))
            errors.Add("clip player copy missing or different " + playerSlot);
        // FBX node names are uncompressed strings. Refuse an older
        // skeleton even if its authoring/player copies agree.
        var bytes = File.ReadAllBytes(file);
        var nodes = System.Text.Encoding.ASCII.GetString(bytes);
        foreach (var bone in Rig.Bones)
            if (!nodes.Contains(bone, StringComparison.Ordinal))
                errors.Add("clip " + slot + " was not baked for rig joint " + bone);
        if (!ReceiptFound) return null;
        var key = slot.StartsWith(ClipRoot + "/", StringComparison.Ordinal) ? slot[(ClipRoot.Length + 1)..] : slot;
        if (!Receipt.TryGetValue(key, out var row))
        {
            errors.Add("clip " + slot + " has no row in data/art/takes-receipt.json: bake it with tools/blender/hero_shared_takes.py");
            return null;
        }
        string sha;
        using (var hasher = System.Security.Cryptography.SHA256.Create())
            sha = BitConverter.ToString(hasher.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        if (!sha.Equals(row.Sha, StringComparison.OrdinalIgnoreCase))
            errors.Add("clip " + slot + " is not the file the takes script baked and checked (receipt SHA-256 differs)");
        foreach (var contract in mustPass)
            if (!row.Contracts.Contains(contract, StringComparer.Ordinal))
                errors.Add("clip " + slot + " did not pass the " + contract + " contract its shared take passes");
        return row.Contracts;
    }

    /// <summary>
    /// SC-20, SC-22: every style has its own take of every styled clip (a reach style: of every clip), both hands where
    /// handed, each vouched for by the receipt with at least the contracts the shared take passed; every captain's body
    /// names a style; the style's build fits the rig's style keys.
    /// </summary>
    void ValidateStyles(ContentCatalog content, IReadOnlyDictionary<string, ClipSlot> listed, List<string> errors)
    {
        if (!ReceiptFound) errors.Add("data/art/takes-receipt.json missing: the takes script writes it when it bakes");
        if (Styles.Count == 0) errors.Add("clips.json names no motion styles");
        if (string.IsNullOrWhiteSpace(StyleSlot) || string.IsNullOrWhiteSpace(StylePlayerSlot))
            errors.Add("clips.json styles needs slot and playerSlot");
        foreach (var id in StyledClips)
            if (!Motion.TryClip(id, out _)) errors.Add("styled clip " + id + " is not a Motion clip");
        foreach (var verb in new[] { "idle", "run", Motion.SwingSlapClip, Motion.SwingChargeClip, "pitch", "pitch-charge", "cheer" })
            if (!StyledClips.Contains(verb, StringComparer.OrdinalIgnoreCase))
                errors.Add("styles.clips must style " + verb + " (run, idle, batting stance, windup, signature)");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var signatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var style in Styles)
        {
            if (!seen.Add(style.Id)) errors.Add("motion style " + style.Id + " is listed twice");
            if (!System.Text.RegularExpressions.Regex.IsMatch(style.Id, "^[a-z][a-z0-9-]*$"))
                errors.Add("motion style id " + style.Id + " must be lower-case kebab");
            if (string.IsNullOrWhiteSpace(style.Signature) || !signatures.Add(style.Signature))
                errors.Add("motion style " + style.Id + " needs its own signature beat name");
            if (style.RunCycle <= 0 || style.WalkCycle <= 0 || style.WalkCycle >= style.RunCycle)
                errors.Add($"motion style {style.Id} cycles must be positive and walk shorter than run ({style.WalkCycle}, {style.RunCycle})");
            foreach (var (channel, scale) in new[] { (Silhouette.ReachBuild, style.Reach), (Silhouette.BootsBuild, style.Boots) })
                if (scale < channel.Min - 1e-9 || scale > channel.Max + 1e-9)
                    errors.Add($"motion style {style.Id} {channel.Id} {scale} is outside the rig's shape keys [{channel.Min}, {channel.Max}]");
            foreach (var clipId in style.Clips)
            {
                if (!listed.TryGetValue(clipId, out var clip)) continue;
                foreach (var hand in clip.Handed ? new[] { Hand.R, Hand.L } : new[] { Hand.R })
                {
                    var shared = ClipFiles(clip, hand);
                    var key = shared.Slot.StartsWith(ClipRoot + "/", StringComparison.Ordinal) ? shared.Slot[(ClipRoot.Length + 1)..] : shared.Slot;
                    var mustPass = Receipt.TryGetValue(key, out var row) ? row.Contracts : [];
                    var (slot, playerSlot) = ClipFiles(clip, hand, style);
                    CheckTake(content, slot, playerSlot, mustPass, errors);
                }
            }
        }
        // The link CF-3 deferred: every class names a style that exists, and every style is some class's or reserved.
        errors.AddRange(StyleLinkErrors(Styles, Classes ?? content.Rules.BodyClasses));
        foreach (var who in content.Characters.Values)
            if (!string.IsNullOrEmpty(who.BodyClass) && StyleOf(who) is null)
                errors.Add($"character {who.Id} (class {who.BodyClass}) resolves to no motion style");
    }

    /// <summary>
    /// The class ↔ style link (CF-3 deferred it to CF-5): every body class's <c>motionStyle</c> is a style row, and every
    /// style is some class's or marked <c>reserved</c> (authored ahead of its class).
    /// </summary>
    public static IReadOnlyList<string> StyleLinkErrors(IReadOnlyList<MotionStyle> styles, BodyClassLibrary classes)
    {
        var errors = new List<string>();
        var ids = new HashSet<string>(styles.Select(s => s.Id), StringComparer.OrdinalIgnoreCase);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in classes.Classes)
        {
            used.Add(row.MotionStyle);
            if (!ids.Contains(row.MotionStyle))
                errors.Add($"body class {row.Id} motionStyle '{row.MotionStyle}' is not a style in data/art/clips.json styles.rows");
            // A borrow ends when the owed style exists: the class must then play it (CH-12).
            if (row.BorrowsStyle && ids.Contains(row.OwedStyle!))
                errors.Add($"body class {row.Id} owes style '{row.OwedStyle}', which now exists; set motionStyle to it and drop owedStyle");
        }
        foreach (var style in styles)
        {
            if (!used.Contains(style.Id) && !style.Reserved)
                errors.Add($"motion style {style.Id} is no body class's motionStyle; name it in data/rules/body-classes.json or mark it reserved");
            if (used.Contains(style.Id) && style.Reserved)
                errors.Add($"motion style {style.Id} is marked reserved but a body class plays it");
        }
        return errors;
    }

    static bool SameBytes(string a, string b)
    {
        var fa = new FileInfo(a);
        var fb = new FileInfo(b);
        if (fa.Length != fb.Length) return false;
        return File.ReadAllBytes(a).AsSpan().SequenceEqual(File.ReadAllBytes(b));
    }

    public static ArtCatalog Load(DataRoot dataRoot)
    {
        string Art(string file) => dataRoot.Resolve("art", file);

        var rigDto = DataJson.Require<RigFile>(Art("rig.json"));
        var rig = new RigBoneMap(rigDto.Id, rigDto.Bones ?? [], rigDto.Events ?? [], rigDto.Slot ?? "");

        var clipDto = DataJson.Require<ClipsFile>(Art("clips.json"));
        var clips = (clipDto.Clips ?? []).Select(c =>
            new ClipSlot(c.Id, c.Loop, c.Handed, c.Events ?? [], c.Slot, c.PlayerSlot,
                c.ContactAt, c.ReleaseAt, c.FootPlantAt, c.FinishAt)).ToList();

        var skinDto = DataJson.Require<SkinsFile>(Art("skins.json"));
        var skins = new Dictionary<string, SkinSlot>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in skinDto.Skins ?? [])
            skins[s.Id] = new SkinSlot(s.Id, s.BodyType, s.Captain, s.Extras ?? [], s.Portrait, s.Palette);

        var extras = new Dictionary<string, ExtraSlot>(StringComparer.OrdinalIgnoreCase);
        var extrasFile = DataJson.Require<ExtrasFile>(Art("extras.json"));
        foreach (var e in extrasFile.Extras ?? [])
            extras[e.Id] = new ExtraSlot(e.Id, e.Bone, e.Hides ?? []);

        var vfx = (DataJson.Require<EventsFile>(Art("vfx.json")).Events ?? [])
            .Select(e => new NamedSlot(e.Id, e.Slot, e.Kind ?? "")).ToList();
        var audio = (DataJson.Require<EventsFile>(Art("audio.json")).Events ?? [])
            .Select(e => new NamedSlot(e.Id, e.Slot, e.Bus ?? e.Kind ?? "", e.Authored)).ToList();
        var mats = (DataJson.Require<MatsFile>(Art("materials.json")).Slots ?? [])
            .Select(e => new NamedSlot(e.Id, e.Slot, e.Shader ?? "")).ToList();
        var nodeOptions = DataJson.Document;
        // The palettes are read strictly from the same file (F6-c): a kit row's palette block, by the row's index.
        var parksNode = JsonNode.Parse(File.ReadAllText(Art("parks.json")), documentOptions: nodeOptions)?["kits"] as JsonArray;
        var parks = (DataJson.Require<ParksFile>(Art("parks.json")).Kits ?? [])
            .Select((p, i) => new ParkKitSlot(p.Id, p.Slot, p.Placed,
                p.Slots is null ? null : new Dictionary<string, string?>(p.Slots, StringComparer.Ordinal),
                parksNode?[i]?["palette"] is { } palette ? ParkLooks.ParsePalette(palette, "parks.json " + p.Id + ".palette") : null)).ToList();
        var looks = ParkLooks.Parse(JsonNode.Parse(File.ReadAllText(Art("looks.json")), documentOptions: nodeOptions), "looks.json");
        var folders = DataJson.Require<FoldersFile>(Art("folders.json")).Folders ?? [];

        var actors = HazardActors.Parse(JsonNode.Parse(File.ReadAllText(Art("hazard-actors.json")), documentOptions: nodeOptions), "hazard-actors.json");
        var styleDto = clipDto.Styles ?? new StylesDto();
        var styled = styleDto.Clips ?? [];
        var motionIds = Motion.ClipIds;
        var styles = (styleDto.Rows ?? []).Select(s =>
        {
            var owned = Math.Abs(s.ReachScale - 1) > 1e-9 ? motionIds : (IReadOnlyList<string>)styled;
            return new MotionStyle(s.Id, s.Signature, s.RunCycle, s.WalkCycle, s.ReachScale, s.BootsScale,
                new HashSet<string>(owned, StringComparer.OrdinalIgnoreCase)) { Reserved = s.Reserved };
        }).ToList();
        var receiptPath = Art("takes-receipt.json");
        var receipt = new Dictionary<string, (string Sha, IReadOnlyList<string> Contracts)>(StringComparer.Ordinal);
        if (File.Exists(receiptPath))
            foreach (var r in DataJson.Require<ReceiptFile>(receiptPath).Takes ?? [])
                receipt[r.File] = (r.Sha256, r.Contracts ?? []);
        return new ArtCatalog(rig, clips, skins, extras, vfx, audio, mats, parks, folders, looks, actors)
        {
            ExtrasSlot = extrasFile.Slot,
            Toon = ToonLook.Parse(JsonNode.Parse(File.ReadAllText(Art("toon.json")), documentOptions: nodeOptions), "toon.json"),
            Factions = FactionLooks.Parse(JsonNode.Parse(File.ReadAllText(Art("factions.json")), documentOptions: nodeOptions), "factions.json"),
            Styles = styles,
            StyledClips = styled,
            StyleSlot = styleDto.Slot,
            StylePlayerSlot = styleDto.PlayerSlot,
            Receipt = receipt,
            ReceiptFound = File.Exists(receiptPath),
        };
    }

    sealed class RigFile
    {
        public string Id { get; set; } = "";
        public string Slot { get; set; } = "";
        public List<string>? Bones { get; set; }
        public List<string>? Events { get; set; }
        // The DCC half of the rig contract (the Blender scripts and BaseballMotionContractTests read these); the sim carries
        // them through so a strict read still refuses a key nobody declares. Blender reads plain JSON, so notes are a key.
        public int Revision { get; set; }
        public JsonElement Joints { get; set; }
        public JsonElement Anatomy { get; set; }
        /// <summary>The per-captain build channels (shape keys on the one mesh); <see cref="Silhouette.Build"/> mirrors them.</summary>
        public JsonElement Build { get; set; }
        public string? Notes { get; set; }
    }

    sealed class ClipsFile
    {
        public List<ClipDto>? Clips { get; set; }
        public StylesDto? Styles { get; set; }
    }
    sealed class StylesDto
    {
        public string? Notes { get; set; }
        public string Slot { get; set; } = "";
        public string PlayerSlot { get; set; } = "";
        public List<string>? Clips { get; set; }
        public List<StyleDto>? Rows { get; set; }
    }
    sealed class StyleDto
    {
        public string Id { get; set; } = "";
        public string Signature { get; set; } = "";
        public double RunCycle { get; set; }
        public double WalkCycle { get; set; }
        public double ReachScale { get; set; }
        public double BootsScale { get; set; }
        /// <summary>A style authored ahead of the body class that will play it.</summary>
        public bool Reserved { get; set; }
    }
    sealed class ReceiptFile
    {
        public string? Notes { get; set; }
        public List<ReceiptDto>? Takes { get; set; }
    }
    sealed class ReceiptDto
    {
        public string File { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public int Frames { get; set; }
        public List<string>? Contracts { get; set; }
    }
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

    sealed class ExtrasFile
    {
        /// <summary>The accessory FBX every extra's mesh is named in (the shared rig's extras).</summary>
        public string Slot { get; set; } = "";
        public List<ExtraDto>? Extras { get; set; }
        /// <summary>Authoring notes: Blender reads plain JSON, so they are a key, not a comment.</summary>
        public string? Notes { get; set; }
    }
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
        public Dictionary<string, string?>? Slots { get; set; }
        /// <summary>Read strictly by <see cref="ParkLooks.ParsePalette"/>; here only so the row's key is known.</summary>
        public System.Text.Json.JsonElement? Palette { get; set; }
    }

    sealed class FoldersFile { public List<string>? Folders { get; set; } }
}
