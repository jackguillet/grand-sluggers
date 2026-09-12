namespace GrandSluggers.Sim;

public readonly record struct PackageTimingMarker(string Event, double At);

public readonly record struct PackageVerbSlot(
    string Verb,
    string Source,
    string PlayerSource,
    string Clip,
    bool Loop,
    string Clock,
    IReadOnlyList<PackageTimingMarker> Markers,
    string Readiness,
    string Fallback);

public sealed record CharacterPackageSpec(
    string Id,
    string Controller,
    string PlayerController,
    IReadOnlyList<PackageVerbSlot> Verbs);

/// <summary>
/// Unique toys are packages, not skins on Rio's T-pose.
/// Bone <b>names</b> in <c>data/art/rig.json</c> are the contract (bat, glove, cameras).
/// Mesh, rest pose, and verb slots live per id. Living spec: docs/character-package.md.
/// </summary>
public static class CharacterPackage
{
    public const string Shared = "shared";
    public const string Segmented = "segmented";
    public const string Skinned = "skinned";
    public const string Rigid = "rigid";

    public const string Ready = "ready";
    public const string Fallback = "fallback";
    public const string CharacterMotionFallback = "character-motion";

    public const string WorldClock = "world";
    public const string PoseClock = "pose";
    public const string ChargeClock = "charge";

    public static readonly IReadOnlyList<string> Binds =
        ["", Shared, Segmented, Skinned, Rigid];

    public static readonly IReadOnlyList<string> Sockets =
    [
        "torso", "head",
        "lUpper", "lFore", "rUpper", "rFore",
        "lThigh", "lShin", "rThigh", "rShin",
        "bat", "glove"
    ];

    /// <summary>Every verb HeroActor can send to a unique package.</summary>
    public static readonly IReadOnlyList<MoveBones.Verb> RuntimeVerbs =
        Enum.GetValues(typeof(MoveBones.Verb)).Cast<MoveBones.Verb>().ToArray();

    public static string Normalize(string? value) =>
        (value ?? "").Trim().ToLowerInvariant();

    public static string VerbId(MoveBones.Verb verb)
    {
        var name = verb.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    public static bool Valid(string? bind)
    {
        var b = Normalize(bind);
        return b is "" or Shared or Segmented or Skinned or Rigid;
    }

    /// <summary>Own mesh + own rest pose. Not hero-shared extras.</summary>
    public static bool IsUnique(string? bind)
    {
        var b = Normalize(bind);
        return b is Segmented or Skinned;
    }

    /// <summary>Mesh is rigid pieces parented to bones. Limbs rotate; the shell cannot invert.</summary>
    public static bool IsSegmented(string? bind) => Normalize(bind) == Segmented;

    /// <summary>SMR with authored (painted) weights. Never the auto-drop default.</summary>
    public static bool IsSkinned(string? bind) => Normalize(bind) == Skinned;

    public static bool IsReady(PackageVerbSlot slot) => Normalize(slot.Readiness) == Ready;

    public static bool TryVerb(CharacterPackageSpec package, MoveBones.Verb verb, out PackageVerbSlot slot)
    {
        var id = VerbId(verb);
        slot = package.Verbs.FirstOrDefault(v => v.Verb.Equals(id, StringComparison.OrdinalIgnoreCase));
        return !string.IsNullOrWhiteSpace(slot.Verb);
    }

    public static bool TryMarker(PackageVerbSlot slot, MoveBones.ClipEvent ev, out double at)
    {
        var id = ev.ToString();
        var marker = slot.Markers.FirstOrDefault(m => m.Event.Equals(id, StringComparison.OrdinalIgnoreCase));
        at = marker.At;
        return !string.IsNullOrWhiteSpace(marker.Event);
    }

    public static string ArtFolder(string id) => "Assets/Art/Characters/" + id;
    public static string ResourcesFolder(string id) => "Assets/Resources/Art/Characters/" + id;
    public static string MeshSlot(string id) => ArtFolder(id) + "/" + id + ".fbx";
    public static string PlayerMeshSlot(string id) => ResourcesFolder(id) + "/" + id + ".fbx";
    public static string AlbedoName(string id) => id + "-albedo.png";
    public static string PrefabSlot(string id) => ArtFolder(id) + "/" + id + ".prefab";
    public static string ControllerSlot(string id) => ArtFolder(id) + "/" + id + ".controller";
    public static string PlayerControllerSlot(string id) => ResourcesFolder(id) + "/" + id + ".controller";
    public static string ClipSlot(string id, string clip) =>
        ArtFolder(id) + "/" + id + "-" + clip + ".fbx";
    public static string PlayerClipSlot(string id, string clip) =>
        ResourcesFolder(id) + "/" + id + "-" + clip + ".fbx";

    public static IReadOnlyList<string> ValidateFiles(
        string dataRoot,
        SkinSlot skin,
        CharacterPackageSpec? package = null)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(skin.Mesh))
        {
            if (package != null)
                errors.Add("shared skin " + skin.Id + " must not declare a character package");
            return errors;
        }
        if (!Valid(skin.Bind))
        {
            errors.Add("skin " + skin.Id + " bind must be shared, segmented, skinned, or rigid");
            return errors;
        }
        if (!IsUnique(skin.Bind) && Normalize(skin.Bind) != Rigid) return errors;

        var repo = Directory.GetParent(dataRoot)?.FullName;
        if (string.IsNullOrEmpty(repo)) return errors;
        var unity = Path.Combine(repo, "unity");
        var id = skin.Id;
        RequireFile(unity, MeshSlot(id), 10_000, "package body FBX", errors);
        RequireFile(unity, PlayerMeshSlot(id), 10_000, "package player FBX", errors);
        RequireFile(unity, ResourcesFolder(id) + "/" + AlbedoName(id), 10_000, "package albedo", errors);

        if (package == null)
        {
            errors.Add("package manifest missing " + id);
            return errors;
        }
        if (!package.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            errors.Add("package manifest id " + package.Id + " does not match skin " + id);
        ValidateController(unity, id, package, errors);
        ValidateVerbs(unity, id, package, errors);
        return errors;
    }

    static void ValidateController(string unity, string id, CharacterPackageSpec package, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(package.Controller))
            errors.Add("package " + id + " missing controller slot");
        else
        {
            if (!package.Controller.Equals(ControllerSlot(id), StringComparison.OrdinalIgnoreCase))
                errors.Add("package " + id + " controller must use " + ControllerSlot(id));
            RequireFile(unity, package.Controller, 100, "package controller", errors);
        }
        if (string.IsNullOrWhiteSpace(package.PlayerController))
            errors.Add("package " + id + " missing player controller slot");
        else
        {
            if (!package.PlayerController.Equals(PlayerControllerSlot(id), StringComparison.OrdinalIgnoreCase))
                errors.Add("package " + id + " player controller must use " + PlayerControllerSlot(id));
            RequireFile(unity, package.PlayerController, 100, "package player controller", errors);
        }
    }

    static void ValidateVerbs(string unity, string id, CharacterPackageSpec package, List<string> errors)
    {
        var known = RuntimeVerbs.Select(VerbId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var group in package.Verbs.GroupBy(v => v.Verb, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(group.Key) || !known.Contains(group.Key))
                errors.Add("package " + id + " unknown verb " + group.Key);
            if (group.Count() != 1)
                errors.Add("package " + id + " verb " + group.Key + " declared " + group.Count() + " times");
        }
        foreach (var verb in RuntimeVerbs)
        {
            if (!TryVerb(package, verb, out var slot))
            {
                errors.Add("package " + id + " missing verb " + VerbId(verb));
                continue;
            }
            ValidateVerb(unity, id, verb, slot, errors);
        }
    }

    static void ValidateVerb(
        string unity,
        string id,
        MoveBones.Verb verb,
        PackageVerbSlot slot,
        List<string> errors)
    {
        var prefix = "package " + id + " verb " + VerbId(verb);
        var readiness = Normalize(slot.Readiness);
        if (readiness is not Ready and not Fallback)
            errors.Add(prefix + " readiness must be ready or fallback");
        if (!slot.Fallback.Equals(CharacterMotionFallback, StringComparison.OrdinalIgnoreCase))
            errors.Add(prefix + " fallback must be " + CharacterMotionFallback);
        var clock = Normalize(slot.Clock);
        if (clock is not WorldClock and not PoseClock and not ChargeClock)
            errors.Add(prefix + " clock must be world, pose, or charge");
        var expectedClock = verb is MoveBones.Verb.Idle or MoveBones.Verb.Walk or MoveBones.Verb.Run
            ? WorldClock
            : verb is MoveBones.Verb.ChargePitch or MoveBones.Verb.ChargeSwing ? ChargeClock : PoseClock;
        if (clock != expectedClock)
            errors.Add(prefix + " clock must be " + expectedClock);
        var shouldLoop = verb is MoveBones.Verb.Idle or MoveBones.Verb.Walk or MoveBones.Verb.Run;
        if (slot.Loop != shouldLoop)
            errors.Add(prefix + " loop must be " + shouldLoop.ToString().ToLowerInvariant());

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var marker in slot.Markers)
        {
            if (!Enum.TryParse<MoveBones.ClipEvent>(marker.Event, true, out _))
                errors.Add(prefix + " unknown marker " + marker.Event);
            if (!seen.Add(marker.Event)) errors.Add(prefix + " duplicate marker " + marker.Event);
            if (double.IsNaN(marker.At) || double.IsInfinity(marker.At) || marker.At < 0)
                errors.Add(prefix + " marker " + marker.Event + " needs a finite non-negative time");
        }

        if (readiness != Ready) return;
        if (string.IsNullOrWhiteSpace(slot.Source) || string.IsNullOrWhiteSpace(slot.PlayerSource))
            errors.Add(prefix + " ready clip needs source and playerSource");
        if (string.IsNullOrWhiteSpace(slot.Clip))
            errors.Add(prefix + " ready clip needs an imported clip name");
        if (!slot.Source.Equals(ClipSlot(id, VerbId(verb)), StringComparison.OrdinalIgnoreCase))
            errors.Add(prefix + " source must use " + ClipSlot(id, VerbId(verb)));
        if (!slot.PlayerSource.Equals(PlayerClipSlot(id, VerbId(verb)), StringComparison.OrdinalIgnoreCase))
            errors.Add(prefix + " playerSource must use " + PlayerClipSlot(id, VerbId(verb)));
        RequireFile(unity, slot.Source, 1_000, prefix + " source", errors);
        RequireFile(unity, slot.PlayerSource, 1_000, prefix + " player source", errors);

        var contract = MoveBones.ClipList.FirstOrDefault(c => c.Verb == verb);
        foreach (var marker in contract.Marks ?? [])
        {
            if (!TryMarker(slot, marker, out var at))
                errors.Add(prefix + " ready clip missing " + marker + " marker");
            else
            {
                var gameplayAt = MoveBones.Mark(verb, marker);
                if (Math.Abs(at - gameplayAt) > 0.0001)
                    errors.Add(prefix + " " + marker + " marker " + at.ToString("0.###")
                        + " must match gameplay " + gameplayAt.ToString("0.###"));
            }
        }
    }

    static void RequireFile(string unity, string slot, long minimumBytes, string label, List<string> errors)
    {
        var path = Path.Combine(unity, slot.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path)) errors.Add(label + " missing " + path);
        else if (new FileInfo(path).Length < minimumBytes) errors.Add(label + " empty " + path);
    }
}
