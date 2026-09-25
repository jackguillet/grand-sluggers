using System.Text.Json.Nodes;

namespace GrandSluggers.Sim;

/// <summary>
/// How a hazard is drawn (FD-16, FD-09, FR-13; F6-d; <c>data/art/hazard-actors.json</c>). Each hazard type of the library
/// names the toy that stands for it (a builder from <see cref="Builders"/>), drawn when a park's <c>hazardActors</c> slot
/// names <see cref="ParkKitSlots.ToyActors"/>; a park that leaves the slot empty draws the pattern greybox instead. Every
/// instance whose pattern acts in play (<see cref="HazardPattern.Hazards"/>) also draws a flat ring at the disc the sim
/// reads (<see cref="PlayDiscFt"/>), in its pattern's ring color, so what the player sees is the size of what plays.
/// </summary>
public sealed class HazardActors
{
    public const string FreezeStatue = "freeze-statue";
    public const string LavaPit = "lava-pit";
    public const string FireStatue = "fire-statue";
    public const string WarpCan = "warp-can";
    public const string BarrelCannon = "barrel-cannon";
    public const string StarBillboard = "star-billboard";
    public const string VineClimb = "vine-climb";
    public const string ChomperMouth = "chomper-mouth";
    public const string KeepStatue = "keep-statue";
    public const string MidwayTrain = "midway-train";
    public const string AcUnit = "ac-unit";
    public const string JungleTree = "jungle-tree";
    public const string LilyPad = "lily-pad";
    public const string TideWave = "tide-wave";

    /// <summary>The toys presentation can draw. A new toy is a row here and its code.</summary>
    public static IReadOnlyList<string> Builders { get; } =
    [
        FreezeStatue, LavaPit, FireStatue, WarpCan, BarrelCannon, StarBillboard, VineClimb, ChomperMouth, KeepStatue,
        MidwayTrain, AcUnit, JungleTree, LilyPad, TideWave
    ];

    HazardActors(IReadOnlyDictionary<string, string> toys, IReadOnlyDictionary<string, LookColor> rings)
    {
        Toys = toys;
        Rings = rings;
    }

    /// <summary>The toy of each hazard type, keyed by the type's id (<see cref="HazardType.All"/>).</summary>
    public IReadOnlyDictionary<string, string> Toys { get; }

    /// <summary>The ring color of each pattern that acts in play.</summary>
    public IReadOnlyDictionary<string, LookColor> Rings { get; }

    /// <summary>
    /// The disc the sim reads for an instance of radius <paramref name="radiusFt"/> (FD-16, FR-13): the radius, the type's
    /// night multiple at night (<see cref="ParkHazards.NightDiscFt"/>), plus the row's reach pad — a redirect catches a
    /// grounder inside its radius plus its row's pad, and the ring is drawn there, not at the radius alone.
    /// </summary>
    public static double PlayDiscFt(double radiusFt, HazardTypeRules row, bool night) =>
        (night ? ParkHazards.NightDiscFt(radiusFt, row) : radiusFt) + row.ReachPadFt;

    /// <summary>What is wrong with the rows against the library: a type with no toy, a toy that is not one, an acting pattern with no ring.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        foreach (var type in HazardType.All)
        {
            if (!Toys.TryGetValue(HazardType.Key(type), out var toy)) errors.Add("hazard actor missing for " + HazardType.Key(type));
            else if (!Builders.Contains(toy)) errors.Add("hazard actor " + HazardType.Key(type) + " names " + toy + ", which is not a toy");
        }
        foreach (var key in Toys.Keys)
            if (!HazardType.All.Any(t => HazardType.Key(t) == key)) errors.Add("hazard actor " + key + " is not a hazard type");
        foreach (var pattern in HazardPattern.Hazards)
            if (!Rings.ContainsKey(pattern)) errors.Add("hazard ring missing for pattern " + pattern);
        foreach (var pattern in Rings.Keys)
            if (!HazardPattern.Hazards.Contains(pattern)) errors.Add("hazard ring " + pattern + " is not a pattern that acts");
        return errors;
    }

    /// <summary>The toy a type's instance is drawn as: by its key in the rows (<c>freezeVolume</c> for <c>freeze_volume</c>).</summary>
    public string? Toy(string type) => Toys.TryGetValue(HazardType.Key(type), out var toy) ? toy : null;

    public static HazardActors Parse(JsonNode? root, string where)
    {
        if (root is not JsonObject o) throw new InvalidDataException(where + " must be an object");
        foreach (var (key, _) in o)
            if (key is not ("toys" or "rings")) throw new InvalidDataException(where + "." + key + " is not a key here");
        var toys = new Dictionary<string, string>(StringComparer.Ordinal);
        if (o["toys"] is not JsonObject t) throw new InvalidDataException(where + ".toys must be an object");
        foreach (var (key, value) in t)
            toys[key] = value is JsonValue v && v.TryGetValue<string>(out var s) ? s : throw new InvalidDataException(where + ".toys." + key + " must be a toy name");
        var rings = new Dictionary<string, LookColor>(StringComparer.Ordinal);
        if (o["rings"] is not JsonObject r) throw new InvalidDataException(where + ".rings must be an object");
        foreach (var (key, value) in r)
            rings[key] = ParkLooks.Color(value, where + ".rings." + key);
        return new HazardActors(toys, rings);
    }
}
