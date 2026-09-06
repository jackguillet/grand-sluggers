namespace GrandSluggers.Sim;

/// <summary>
/// Unique toys are packages, not skins on Rio's T-pose.
/// Bone <b>names</b> in <c>data/art/rig.json</c> are the contract (bat, glove, cameras).
/// Mesh, rest pose, and pieces live per id. Living spec: docs/character-package.md.
/// </summary>
public static class CharacterPackage
{
    public const string Shared = "shared";
    public const string Segmented = "segmented";
    public const string Skinned = "skinned";
    public const string Rigid = "rigid";

    public static readonly IReadOnlyList<string> Binds =
        ["", Shared, Segmented, Skinned, Rigid];

    public static readonly IReadOnlyList<string> Sockets =
    [
        "torso", "head",
        "lUpper", "lFore", "rUpper", "rFore",
        "lThigh", "lShin", "rThigh", "rShin",
        "bat", "glove"
    ];

    public static string Normalize(string? bind)
    {
        var b = (bind ?? "").Trim().ToLowerInvariant();
        return b;
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

    public static string ArtFolder(string id) => "Assets/Art/Characters/" + id;
    public static string ResourcesFolder(string id) => "Assets/Resources/Art/Characters/" + id;
    public static string MeshSlot(string id) => ArtFolder(id) + "/" + id + ".fbx";
    public static string AlbedoName(string id) => id + "-albedo.png";
    public static string ClipSlot(string id, string clip) =>
        ArtFolder(id) + "/" + id + "-" + clip + ".fbx";

    public static readonly IReadOnlyList<string> PackageClips = ["idle", "pose"];

    public static IReadOnlyList<string> ValidateFiles(string dataRoot, SkinSlot skin)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(skin.Mesh)) return errors;
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
        var fbx = Path.Combine(unity, ArtFolder(id).Replace('/', Path.DirectorySeparatorChar), id + ".fbx");
        var res = Path.Combine(unity, ResourcesFolder(id).Replace('/', Path.DirectorySeparatorChar), id + ".fbx");
        var albedo = Path.Combine(unity, ResourcesFolder(id).Replace('/', Path.DirectorySeparatorChar), AlbedoName(id));
        if (!File.Exists(fbx)) errors.Add("package missing body FBX " + fbx);
        else if (new FileInfo(fbx).Length < 10_000) errors.Add("package body FBX empty " + id);
        if (!File.Exists(res)) errors.Add("package missing player FBX " + res);
        if (!File.Exists(albedo)) errors.Add("package missing albedo " + albedo);
        else if (new FileInfo(albedo).Length < 10_000) errors.Add("package albedo empty " + id);
        foreach (var clip in PackageClips)
        {
            var take = Path.Combine(unity, ArtFolder(id).Replace('/', Path.DirectorySeparatorChar),
                id + "-" + clip + ".fbx");
            if (!File.Exists(take)) errors.Add("package missing clip " + take);
            else if (new FileInfo(take).Length < 1_000) errors.Add("package clip empty " + id + "-" + clip);
        }
        return errors;
    }
}
