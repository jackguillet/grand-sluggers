using System.Text.Json.Nodes;

namespace GrandSluggers.Sim;

/// <summary>
/// A color as the look data writes it (FD-16, F6-c): <c>"#RRGGBB"</c>, kept as the integer so presentation converts it
/// the way <c>Colors.Hex</c> does, or <c>[r, g, b]</c> in 0–1, kept as the numbers written.
/// </summary>
public readonly record struct LookColor(double R, double G, double B, int? Hex = null);

/// <summary>The camera's fog: its color and its linear start and end in feet.</summary>
public sealed record LookFog(LookColor Color, double Start, double End);

/// <summary>A sky at one time of day: the camera's background and its fog.</summary>
public sealed record SkyState(LookColor Color, LookFog Fog);

/// <summary>A directional light: color, intensity, rotation in degrees and whether it casts shadows.</summary>
public sealed record LookLight(LookColor Color, double Intensity, double EulerX, double EulerY, double EulerZ, bool Shadows);

/// <summary>A light at one time of day: the trilight ambient and the sun, fill and rim.</summary>
public sealed record LightState(LookColor AmbientSky, LookColor AmbientEquator, LookColor AmbientGround, LookLight Sun, LookLight Fill, LookLight Rim);

/// <summary>A named sky row of <c>data/art/looks.json</c>: the day, and the night that replaces it (null keeps the day's).</summary>
public sealed record SkyLook(string Id, SkyState Day, SkyState? Night)
{
    public SkyState At(bool night) => night && Night is not null ? Night : Day;
}

/// <summary>A named light row of <c>data/art/looks.json</c>: the day, and the night that replaces it (null keeps the day's).</summary>
public sealed record LightLook(string Id, LightState Day, LightState? Night)
{
    public LightState At(bool night) => night && Night is not null ? Night : Day;
}

/// <summary>A surface of the greybox: its color, an optional tiled texture (<c>grass</c> or <c>dirt</c>) and its smoothness.</summary>
public sealed record LookSurface(LookColor Color, double Smooth, string? Texture = null, double Tile = 1);

/// <summary>
/// The colors a park's greybox draws in (FD-16, F6-c; the <c>palette</c> of its row in <c>data/art/parks.json</c>): the
/// outfield grass, the water past it, the infield dirt, the wall (and the alternate panel, or null to repeat the wall), the
/// wall's cap and the foul poles.
/// </summary>
public sealed record ParkPalette(
    LookSurface Grass, LookSurface Water, LookSurface Dirt, LookSurface Wall, LookSurface? WallAlt, LookSurface Cap, LookSurface Pole);

/// <summary>
/// The park looks (FD-16, FR-04; F6-c): the named skies and lights of <c>data/art/looks.json</c>, which a park's kit names
/// in its <c>sky</c> and <c>light</c> slots. The reader is strict: an unknown key, a missing key or a bad value stops the
/// load and says where.
/// </summary>
public sealed class ParkLooks
{
    public static readonly IReadOnlyList<string> Textures = ["grass", "dirt"];

    ParkLooks(IReadOnlyDictionary<string, SkyLook> skies, IReadOnlyDictionary<string, LightLook> lights)
    {
        Skies = skies;
        Lights = lights;
    }

    public IReadOnlyDictionary<string, SkyLook> Skies { get; }
    public IReadOnlyDictionary<string, LightLook> Lights { get; }

    public static ParkLooks Parse(JsonNode? root, string where)
    {
        var obj = Obj(root, where, "skies", "lights");
        var skies = new Dictionary<string, SkyLook>(StringComparer.Ordinal);
        foreach (var (row, at) in Rows(obj["skies"], where + ".skies"))
        {
            var o = Obj(row, at, "id", "day", "night");
            var id = Str(o["id"], at + ".id");
            if (!skies.TryAdd(id, new SkyLook(id, Sky(o["day"], at + ".day"), o["night"] is null ? null : Sky(o["night"], at + ".night"))))
                throw Bad(at + ".id", "repeats sky " + id);
        }
        var lights = new Dictionary<string, LightLook>(StringComparer.Ordinal);
        foreach (var (row, at) in Rows(obj["lights"], where + ".lights"))
        {
            var o = Obj(row, at, "id", "day", "night");
            var id = Str(o["id"], at + ".id");
            if (!lights.TryAdd(id, new LightLook(id, Light(o["day"], at + ".day"), o["night"] is null ? null : Light(o["night"], at + ".night"))))
                throw Bad(at + ".id", "repeats light " + id);
        }
        return new ParkLooks(skies, lights);
    }

    /// <summary>A park's palette block, strictly.</summary>
    public static ParkPalette ParsePalette(JsonNode? node, string where)
    {
        var o = Obj(node, where, "grass", "water", "dirt", "wall", "wallAlt", "cap", "pole");
        return new ParkPalette(
            Surface(o["grass"], where + ".grass"),
            Surface(o["water"], where + ".water"),
            Surface(o["dirt"], where + ".dirt"),
            Surface(o["wall"], where + ".wall"),
            o["wallAlt"] is null ? null : Surface(o["wallAlt"], where + ".wallAlt"),
            Surface(o["cap"], where + ".cap"),
            Surface(o["pole"], where + ".pole"));
    }

    static SkyState Sky(JsonNode? node, string at)
    {
        var o = Obj(node, at, "color", "fog");
        var fog = Obj(o["fog"], at + ".fog", "color", "start", "end");
        return new SkyState(Color(o["color"], at + ".color"),
            new LookFog(Color(fog["color"], at + ".fog.color"), Num(fog["start"], at + ".fog.start"), Num(fog["end"], at + ".fog.end")));
    }

    static LightState Light(JsonNode? node, string at)
    {
        var o = Obj(node, at, "ambient", "sun", "fill", "rim");
        var amb = Obj(o["ambient"], at + ".ambient", "sky", "equator", "ground");
        return new LightState(
            Color(amb["sky"], at + ".ambient.sky"), Color(amb["equator"], at + ".ambient.equator"), Color(amb["ground"], at + ".ambient.ground"),
            Dir(o["sun"], at + ".sun"), Dir(o["fill"], at + ".fill"), Dir(o["rim"], at + ".rim"));
    }

    static LookLight Dir(JsonNode? node, string at)
    {
        var o = Obj(node, at, "color", "intensity", "euler", "shadows");
        var e = Nums(o["euler"], at + ".euler", 3);
        var shadows = o["shadows"] is JsonValue v && v.TryGetValue<bool>(out var b) ? b : throw Bad(at + ".shadows", "must be true or false");
        return new LookLight(Color(o["color"], at + ".color"), Num(o["intensity"], at + ".intensity"), e[0], e[1], e[2], shadows);
    }

    static LookSurface Surface(JsonNode? node, string at)
    {
        var o = Obj(node, at, "color", "smooth", "texture?", "tile?");
        string? texture = null;
        var tile = 1.0;
        if (o.ContainsKey("texture"))
        {
            texture = Str(o["texture"], at + ".texture");
            if (!Textures.Contains(texture)) throw Bad(at + ".texture", "is " + texture + "; a texture is grass or dirt");
            tile = Num(o["tile"], at + ".tile");
            if (tile <= 0) throw Bad(at + ".tile", "must be greater than 0");
        }
        else if (o.ContainsKey("tile")) throw Bad(at + ".tile", "names a tile with no texture");
        return new LookSurface(Color(o["color"], at + ".color"), Num(o["smooth"], at + ".smooth"), texture, tile);
    }

    internal static LookColor Color(JsonNode? node, string at)
    {
        if (node is JsonValue v && v.TryGetValue<string>(out var s))
        {
            if (s.Length != 7 || s[0] != '#' || !int.TryParse(s.Substring(1), System.Globalization.NumberStyles.HexNumber, null, out var hex))
                throw Bad(at, "is " + s + "; a color is \"#RRGGBB\" or [r, g, b]");
            return new LookColor(((hex >> 16) & 255) / 255.0, ((hex >> 8) & 255) / 255.0, (hex & 255) / 255.0, hex);
        }
        var n = Nums(node, at, 3);
        foreach (var c in n)
            if (c < 0 || c > 1) throw Bad(at, "has " + c + "; each channel is 0 to 1");
        return new LookColor(n[0], n[1], n[2]);
    }

    internal static double[] Nums(JsonNode? node, string at, int count)
    {
        if (node is not JsonArray a || a.Count != count) throw Bad(at, "must be " + count + " numbers");
        var n = new double[count];
        for (var i = 0; i < count; i++) n[i] = Num(a[i], at + "[" + i + "]");
        return n;
    }

    internal static double Num(JsonNode? node, string at) =>
        node is JsonValue v && v.TryGetValue<double>(out var d) ? d : throw Bad(at, "must be a number");

    static string Str(JsonNode? node, string at) =>
        node is JsonValue v && v.TryGetValue<string>(out var s) && s.Length > 0 ? s : throw Bad(at, "must be a name");

    /// <summary>An object with exactly these keys; a key ending in <c>?</c> may be left out, and a key may be null only where the caller allows it.</summary>
    internal static JsonObject Obj(JsonNode? node, string at, params string[] keys)
    {
        if (node is not JsonObject o) throw Bad(at, "must be an object");
        var names = keys.Select(k => k.TrimEnd('?')).ToHashSet(StringComparer.Ordinal);
        foreach (var (key, _) in o)
            if (!names.Contains(key)) throw Bad(at + "." + key, "is not a key here");
        foreach (var k in keys)
            if (!k.EndsWith("?", StringComparison.Ordinal) && !o.ContainsKey(k)) throw Bad(at + "." + k, "is missing (null where it may be empty)");
        return o;
    }

    static IEnumerable<(JsonNode? Row, string At)> Rows(JsonNode? node, string at)
    {
        if (node is not JsonArray a) throw Bad(at, "must be a list");
        for (var i = 0; i < a.Count; i++) yield return (a[i], at + "[" + i + "]");
    }

    internal static InvalidDataException Bad(string at, string what) => new(at + " " + what);
}
