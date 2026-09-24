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
        "diamond", "diamond-line", "diamond-fly", "diamond-grounder",
        "throw", "tag", "smash", "replay", "scoop",
        "char-rest", "char-pose", "swing-matrix",
        // Named park shots (F7-b1, #882): posed from the park being captured
        // (data/feel/shots.json parkShots, StillShots.Frame). Opt-in only.
        "pole-left", "pole-right"
    };

    public string[]? Shots { get; init; }
    public string[]? SwingCaptains { get; init; }
    public string? Home { get; init; }
    public string? Away { get; init; }
    public bool HudOff { get; init; } = true;
    public bool FeelDebug { get; init; }
    public int Width { get; init; } = 1920;
    public int Height { get; init; } = 1080;
    public string? OutDir { get; init; }
    public double Charge01 { get; init; } = 1;

    /// <summary>
    /// The park the batch captures. Absent or blank is the one default park
    /// (FR-04: no park id lives in code beyond <see cref="ExhibitionPick.DefaultPark"/>).
    /// </summary>
    public string? Park { get; init; }

    /// <summary>Night for the whole batch, the same declared layer the field pick toggles.</summary>
    public bool Night { get; init; }

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

    /// <summary>
    /// Trims and lower-cases like <see cref="ResolvedHome"/>. The park comes from
    /// the catalog, not a list in code (#820, FR-04): an id no park file declares
    /// is refused by name, the way an unknown shot is.
    /// </summary>
    public string ResolvedPark(ContentCatalog content)
    {
        if (string.IsNullOrWhiteSpace(Park)) return ExhibitionPick.DefaultPark;
        var id = Park.Trim().ToLowerInvariant();
        if (!content.Parks.ContainsKey(id))
            throw new InvalidDataException("still park not allowed: " + Park.Trim()
                + "; the fields are " + string.Join(", ", content.ParkPickOrder));
        return id;
    }

    public string ResolvedAway()
    {
        var away = string.IsNullOrWhiteSpace(Away) ? "ashlord" : Away.Trim().ToLowerInvariant();
        var home = ResolvedHome();
        return away == home ? "brondo" : away;
    }

    /// <summary>
    /// Captain subset for the opt-in swing matrix. The default remains the six
    /// shared-rig captains; Generic packages opt in explicitly because their
    /// anatomy cannot use shared-rig hand and plate thresholds.
    /// </summary>
    public IReadOnlyList<string> ResolvedSwingCaptains()
    {
        var src = SwingCaptains is { Length: > 0 }
            ? SwingCaptains
            : SwingPresentation.SharedCaptains;
        var resolved = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in src)
        {
            var id = (raw ?? "").Trim().ToLowerInvariant();
            if (id.Length == 0)
                throw new InvalidDataException("swing matrix captain id is empty");
            if (!PresetTeams.CaptainIds.Contains(id, StringComparer.OrdinalIgnoreCase))
                throw new InvalidDataException("swing matrix captain not playable: " + id);
            if (!seen.Add(id))
                throw new InvalidDataException("swing matrix captain is duplicated: " + id);
            resolved.Add(id);
        }
        return resolved;
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

    public static bool IsCharShot(string shot) =>
        shot.Equals("char-rest", StringComparison.OrdinalIgnoreCase)
        || shot.Equals("char-pose", StringComparison.OrdinalIgnoreCase);

    public static bool IsSwingMatrixShot(string shot) =>
        shot.Equals("swing-matrix", StringComparison.OrdinalIgnoreCase);

    public static string SwingPngPath(string outDir, string captain, string power, string beat) =>
        Path.Combine(outDir, "swing-" + captain.Trim().ToLowerInvariant()
            + "-" + power.Trim().ToLowerInvariant()
            + "-" + beat.Trim().ToLowerInvariant() + ".png");

    /// <summary>
    /// What a still names itself. The default park in daylight keeps today's
    /// names, so existing stills and gates do not move; any other park, and any
    /// night, is named in the file (<c>{shot}-{park}.png</c>,
    /// <c>{shot}-{park}-night.png</c>).
    /// </summary>
    public static string ParkSuffix(string? park, bool night)
    {
        var id = string.IsNullOrWhiteSpace(park)
            ? ExhibitionPick.DefaultPark
            : park.Trim().ToLowerInvariant();
        var suffix = id.Equals(ExhibitionPick.DefaultPark, StringComparison.Ordinal) ? "" : "-" + id;
        return night ? suffix + "-night" : suffix;
    }

    public static string PngPath(string outDir, string shot, string? who = null, string? park = null, bool night = false)
    {
        var id = (shot ?? "").ToLowerInvariant();
        var name = id;
        if (IsCharShot(id) && !string.IsNullOrWhiteSpace(who))
        {
            var kind = id.EndsWith("pose", StringComparison.Ordinal) ? "pose" : "rest";
            name = "char-" + who.Trim().ToLowerInvariant() + "-" + kind;
        }
        return Path.Combine(outDir, name + ParkSuffix(park, night) + ".png");
    }

    /// <summary>
    /// Shots and captains are refused from the request alone. The park needs the
    /// catalog that declares the parks, so a caller that has one passes it and the
    /// bad id is named here; a caller that has none refuses it at
    /// <see cref="ResolvedPark"/>, before the first still.
    /// </summary>
    public static StillRequest Parse(string json, ContentCatalog? content = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidDataException("still request is empty");
        var req = JsonSerializer.Deserialize<StillRequest>(json, DataJson.Options)
            ?? throw new InvalidDataException("still request is empty");
        _ = req.ResolvedShots();
        _ = req.ResolvedSwingCaptains();
        if (content != null) _ = req.ResolvedPark(content);
        return req;
    }

    /// <summary>
    /// Reads a durable request outside Unity's startup-cleaned Temp folder and
    /// validates it before an editor tool stages the JSON for Play mode. The
    /// caller passes the catalog it already loaded — an editor tool's data root is
    /// not the one a bare load would find — so a bad park is a menu error here and
    /// not a still at the wrong field.
    /// </summary>
    public static string ReadValidatedJsonFile(string? path, ContentCatalog content)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidDataException("still request file path is empty");
        if (!File.Exists(path))
            throw new FileNotFoundException("still request file not found", path);
        var json = File.ReadAllText(path);
        _ = Parse(json, content);
        return json;
    }

    public static bool TryLoad(string unityTemp, ContentCatalog? content, out StillRequest request, out string error)
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
            request = Parse(File.ReadAllText(path), content);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
