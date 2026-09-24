namespace GrandSluggers.Sim;

/// <summary>
/// Equipment visual ids. Loadout identity and the active hitting prop are
/// separate: cycling a bat keeps changing its sim item while every batter
/// takes the same authored bat to the plate.
/// </summary>
public static class GearMesh
{
    public const string CommonHittingBatVisual = "bat-wood";

    public static string BatVisual(BatItem? bat) =>
        !string.IsNullOrEmpty(bat?.Visual) ? bat.Visual : CommonHittingBatVisual;

    /// <summary>
    /// The active batting prop. This deliberately takes no character, hand,
    /// or loadout argument so signature gear cannot replace it.
    /// </summary>
    public static string HittingBatVisual() => CommonHittingBatVisual;

    public static string GloveVisual(GloveItem? glove) =>
        !string.IsNullOrEmpty(glove?.Visual) ? glove.Visual : "glove-brown";

    /// <summary>
    /// A captain's signature bat: its <c>signatureBat</c> (data/characters), which the validator checks names a bat the
    /// catalog has. A team led by a role player carries the first captain's in select order.
    /// </summary>
    public static BatItem SignatureBat(ContentCatalog content, string captainId)
    {
        var who = content.Must(captainId);
        var id = who.SignatureBat ?? content.Must(content.CaptainIds[0]).SignatureBat!;
        return content.Bats[id];
    }
}
