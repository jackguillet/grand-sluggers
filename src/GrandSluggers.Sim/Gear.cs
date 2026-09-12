namespace GrandSluggers.Sim;

/// <summary>
/// Equipment visual ids. Loadout identity and the active hitting prop are
/// separate: cycling a bat keeps changing its sim item while every batter
/// takes the same authored bat to the plate.
/// </summary>
public static class GearMesh
{
    public const string CommonHittingBatVisual = "bat-wood";

    public static string SignatureBatId(string captainId) => captainId.ToLowerInvariant() switch
    {
        "vale" => "pageant-wand",
        "zig" => "prism-stick",
        "brondo" => "gold-brick",
        "konga" => "barrel-bat",
        "ashlord" => "furnace-club",
        "fenn" => "fen-cane",
        _ => "harbor-lumber"
    };

    public static string BatVisual(BatItem? bat) =>
        !string.IsNullOrEmpty(bat?.Visual) ? bat.Visual : CommonHittingBatVisual;

    /// <summary>
    /// The active batting prop. This deliberately takes no character, hand,
    /// or loadout argument so signature gear cannot replace it.
    /// </summary>
    public static string HittingBatVisual() => CommonHittingBatVisual;

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
