using System.Text.Json.Nodes;

namespace GrandSluggers.Sim;

/// <summary>How a palette color becomes a toy-bright fill: lerp toward white by <c>Whiten</c>, then times <c>Gain</c> per channel, capped at 1.</summary>
public sealed record ToonFill(double Whiten, double GainR, double GainG, double GainB)
{
    public (double R, double G, double B) Of(double r, double g, double b)
    {
        r += (1 - r) * Whiten;
        g += (1 - g) * Whiten;
        b += (1 - b) * Whiten;
        return (Math.Min(1, r * GainR), Math.Min(1, g * GainG), Math.Min(1, b * GainB));
    }
}

/// <summary>The warped diffuse and its two bands (see <c>data/art/toon.json</c>).</summary>
public sealed record ToonLight(
    double Wrap, double BandAt, double BandSoft, LookColor Shade, double Shadow, double SunFull, double SunTint, double Ambient);

/// <summary>The view rim (see <c>data/art/toon.json</c>).</summary>
public sealed record ToonRim(LookColor Color, double At, double Soft, double Strength, double Up);

/// <summary>
/// The character toon (CF-7, CH-14; <c>data/art/toon.json</c>): the numbers the <see cref="Shader"/> shader draws the
/// jersey, trim and flesh of a body with. The reader is strict: an unknown key, a missing key or a value out of its
/// range stops the load and says where.
/// </summary>
public sealed record ToonLook(ToonFill Fill, ToonLight Light, ToonRim Rim)
{
    /// <summary>The shader the <c>toon-body</c> material slot names (<c>data/art/materials.json</c>).</summary>
    public const string Shader = "ToonRim";

    /// <summary>The material slot a character body draws in.</summary>
    public const string BodySlot = "toon-body";

    public static ToonLook Parse(JsonNode? root, string where)
    {
        var o = ParkLooks.Obj(root, where, "fill", "light", "rim");

        var f = ParkLooks.Obj(o["fill"], where + ".fill", "whiten", "gain");
        var gain = ParkLooks.Nums(f["gain"], where + ".fill.gain", 3);
        for (var i = 0; i < 3; i++)
            if (gain[i] < 0.5 || gain[i] > 2) throw ParkLooks.Bad(where + ".fill.gain[" + i + "]", "is " + gain[i] + "; a gain is 0.5 to 2");
        var fill = new ToonFill(Unit(f["whiten"], where + ".fill.whiten"), gain[0], gain[1], gain[2]);

        var l = ParkLooks.Obj(o["light"], where + ".light", "wrap", "bandAt", "bandSoft", "shade", "shadow", "sunFull", "sunTint", "ambient");
        var bandSoft = ParkLooks.Num(l["bandSoft"], where + ".light.bandSoft");
        if (bandSoft < 0 || bandSoft > 0.5) throw ParkLooks.Bad(where + ".light.bandSoft", "is " + bandSoft + "; a step width is 0 to 0.5");
        var sunFull = ParkLooks.Num(l["sunFull"], where + ".light.sunFull");
        if (sunFull <= 0) throw ParkLooks.Bad(where + ".light.sunFull", "must be greater than 0");
        var light = new ToonLight(
            Unit(l["wrap"], where + ".light.wrap"),
            Unit(l["bandAt"], where + ".light.bandAt"),
            bandSoft,
            ParkLooks.Color(l["shade"], where + ".light.shade"),
            Unit(l["shadow"], where + ".light.shadow"),
            sunFull,
            Unit(l["sunTint"], where + ".light.sunTint"),
            Unit(l["ambient"], where + ".light.ambient"));

        var r = ParkLooks.Obj(o["rim"], where + ".rim", "color", "at", "soft", "strength", "up");
        var soft = ParkLooks.Num(r["soft"], where + ".rim.soft");
        if (soft < 0 || soft > 0.5) throw ParkLooks.Bad(where + ".rim.soft", "is " + soft + "; a step width is 0 to 0.5");
        var rim = new ToonRim(
            ParkLooks.Color(r["color"], where + ".rim.color"),
            Unit(r["at"], where + ".rim.at"),
            soft,
            Unit(r["strength"], where + ".rim.strength"),
            Unit(r["up"], where + ".rim.up"));

        return new ToonLook(fill, light, rim);
    }

    /// <summary>
    /// The catalog's side of the rail: the body slot is in <c>materials.json</c> and names <see cref="Shader"/>, so the
    /// table and the shader cannot drift apart.
    /// </summary>
    public static IReadOnlyList<string> Validate(IReadOnlyList<NamedSlot> materials)
    {
        var errors = new List<string>();
        var body = materials.FirstOrDefault(m => m.Id.Equals(BodySlot, StringComparison.Ordinal));
        if (string.IsNullOrEmpty(body.Id))
            errors.Add("materials.json has no " + BodySlot + " slot; the character toon (toon.json) draws in it");
        else if (!body.Kind.Equals(Shader, StringComparison.Ordinal))
            errors.Add("materials.json " + BodySlot + " names shader " + body.Kind + "; toon.json is drawn by " + Shader);
        return errors;
    }

    static double Unit(JsonNode? node, string at)
    {
        var v = ParkLooks.Num(node, at);
        if (v < 0 || v > 1) throw ParkLooks.Bad(at, "is " + v + "; it is 0 to 1");
        return v;
    }
}
