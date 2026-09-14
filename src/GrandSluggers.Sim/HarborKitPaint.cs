namespace GrandSluggers.Sim;

/// <summary>
/// Harbor kit fill from Blender material names, with a slot default when Unity
/// drops those names (FBX <c>BasedOnTextureName</c> with no albedo). Bags and
/// the plate are chalk; they never fall through to wood or dirt.
/// </summary>
public static class HarborKitPaint
{
    public enum Fill
    {
        Wood,
        Roof,
        Gold,
        Pad,
        Post,
        Flesh,
        Chalk,
        Navy,
        Dirt,
    }

    /// <summary>Kit meshes whose canvas is chalk. Navy piping is an accent, not the bag.</summary>
    public static readonly string[] ChalkMeshes = ["bag", "home-plate"];

    public static readonly string[] DirtMeshes = ["mound", "warning-track", "infield-dirt"];

    public static Fill PrimitiveBag => Fill.Chalk;

    public static Fill For(string mesh, string materialName)
    {
        var slot = Norm(mesh);
        var n = Norm(materialName);
        if (IsChalkMesh(slot))
        {
            if (Has(n, "navy", "piping")) return Fill.Navy;
            if (Has(n, "gold", "fascia")) return Fill.Gold;
            return Fill.Chalk;
        }

        if (Has(n, "gold", "cap", "fascia")) return Fill.Gold;
        if (Has(n, "mesh", "screen")) return Fill.Post;
        if (Has(n, "roof")) return Fill.Roof;
        if (Has(n, "pad", "rail")) return Fill.Pad;
        if (Has(n, "post")) return Fill.Post;
        if (Has(n, "flesh", "head")) return Fill.Flesh;
        if (Has(n, "chalk", "cream")) return Fill.Chalk;
        if (Has(n, "navy")) return Fill.Navy;
        if (Has(n, "dirt", "hill")) return Fill.Dirt;
        return SlotDefault(slot);
    }

    public static bool IsDirtOrWood(Fill fill) => fill is Fill.Dirt or Fill.Wood;

    public static bool IsChalkMesh(string mesh)
    {
        var slot = Norm(mesh);
        foreach (var id in ChalkMeshes)
        {
            if (slot == id) return true;
        }
        return false;
    }

    static Fill SlotDefault(string slot)
    {
        foreach (var id in DirtMeshes)
        {
            if (slot == id) return Fill.Dirt;
        }
        return Fill.Wood;
    }

    static string Norm(string value)
    {
        var n = (value ?? "").Trim().ToLowerInvariant();
        const string instance = " (instance)";
        if (n.EndsWith(instance, StringComparison.Ordinal))
            n = n[..^instance.Length];
        return n;
    }

    static bool Has(string n, params string[] tokens)
    {
        if (n.Length == 0) return false;
        foreach (var token in tokens)
        {
            if (n.Contains(token, StringComparison.Ordinal)) return true;
        }
        return false;
    }
}
