namespace GrandSluggers.Sim;

/// <summary>
/// Loadout mesh ids. CycleBat/CycleGlove still swap the sim item; Unity reads Visual.
/// </summary>
public static class GearMesh
{
    public static string SignatureBatId(string captainId) => captainId.ToLowerInvariant() switch
    {
        "vale" => "pageant-wand",
        "zig" => "prism-stick",
        "brondo" => "gold-brick",
        "konga" => "barrel-bat",
        "ashlord" => "furnace-club",
        _ => "harbor-lumber"
    };

    public static string BatVisual(BatItem? bat) =>
        !string.IsNullOrEmpty(bat?.Visual) ? bat.Visual : "bat-wood";

    public static string GloveVisual(GloveItem? glove) =>
        !string.IsNullOrEmpty(glove?.Visual) ? glove.Visual : "glove-brown";

    public static BatItem SignatureBat(ContentCatalog content, string captainId)
    {
        var id = SignatureBatId(captainId);
        if (content.Bats.TryGetValue(id, out var bat)) return bat;
        if (content.Bats.TryGetValue("harbor-lumber", out bat)) return bat;
        return content.Bats.Values.First();
    }
}
