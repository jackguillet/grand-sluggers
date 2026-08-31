namespace GrandSluggers.Sim;

/// <summary>
/// File-drop protocol for Exhibition stills. Unity Play consumes
/// <c>unity/Temp/gs-still-request.json</c> and writes PNGs + a done file.
/// Personal Unity cannot -batchmode; this is how an agent captures HUD-off
/// cameras from the already-open editor.
/// </summary>
public sealed class StillRequest
{
    public const string RequestFileName = "gs-still-request.json";
    public const string DoneFileName = "gs-still-done.json";
    public const string DefaultOutFolder = "gs-stills";

    public static readonly string[] DefaultShots = ["title", "select", "lineup", "plate", "pitch", "mound", "diamond-grounder", "smash"];

    public static readonly HashSet<string> AllowedShots = new(StringComparer.OrdinalIgnoreCase)
    {
        "title", "select", "field", "lineup", "plate", "pitch", "mound",
        "diamond", "diamond-grounder", "diamond-line", "diamond-homer", "diamond-pull",
        "throw", "tag", "smash", "replay", "scoop"
    };

    public string[]? Shots { get; init; }
    public string? Home { get; init; }
    public string? Away { get; init; }
    public bool HudOff { get; init; } = true;
    public bool FeelDebug { get; init; }
    public int Width { get; init; } = 1920;
    public int Height { get; init; } = 1080;
    public string? OutDir { get; init; }
    public double Charge01 { get; init; } = 1;

    public IReadOnlyList<string> ResolvedShots()
    {
        var src = Shots is { Length: > 0 } ? Shots : DefaultShots;
        var list = new List<string>();
        foreach (var raw in src)
        {
            var id = (raw ?? "").Trim();
            if (id.Length == 0) continue;
            if (!AllowedShots.Contains(id))
                throw new InvalidDataException("still shot not allowed: " + id);
            if (id.Equals("scoop", StringComparison.OrdinalIgnoreCase))
                id = "diamond-grounder";
            list.Add(id.ToLowerInvariant());
        }
        if (list.Count == 0)
            throw new InvalidDataException("still request needs at least one shot");
        return list;
    }

    public string ResolvedHome() => string.IsNullOrWhiteSpace(Home) ? "rio" : Home.Trim().ToLowerInvariant();

    public string ResolvedAway()
    {
        var away = string.IsNullOrWhiteSpace(Away) ? "ashlord" : Away.Trim().ToLowerInvariant();
        var home = ResolvedHome();
        return away == home ? "brondo" : away;
    }

    public int ResolvedWidth() => Width < 320 ? 1920 : Width;

    public int ResolvedHeight() => Height < 180 ? 1080 : Height;

    public string ResolvedOutDir(string unityTemp)
    {
        if (!string.IsNullOrWhiteSpace(OutDir)) return OutDir;
        return Path.Combine(unityTemp, DefaultOutFolder);
    }

    public static string RequestPath(string unityTemp) => Path.Combine(unityTemp, RequestFileName);

    public static string DonePath(string unityTemp) => Path.Combine(unityTemp, DoneFileName);

    public static string PngPath(string outDir, string shot) =>
        Path.Combine(outDir, shot.ToLowerInvariant() + ".png");

    public static StillRequest Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidDataException("still request is empty");
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var req = JsonSerializer.Deserialize<StillRequest>(json, opts)
            ?? throw new InvalidDataException("still request is empty");
        _ = req.ResolvedShots();
        return req;
    }

    public static bool TryLoad(string unityTemp, out StillRequest request, out string error)
    {
        request = null!;
        error = "";
        var path = RequestPath(unityTemp);
        if (!File.Exists(path))
        {
            error = "missing " + path;
            return false;
        }
        try
        {
            request = Parse(File.ReadAllText(path));
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
