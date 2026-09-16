using System.Text.Json;
using System.Text.Json.Nodes;

namespace GrandSluggers.Sim;

/// <summary>
/// Where gameplay data is read from: the shipped root (<c>data/</c>), and optionally a
/// <b>trial overlay</b> that carries only its own diff (#716).
///
/// A trial is a candidate profile — a different infield, a different drag, a different read
/// clock — that has to be compared against the shipped game with identical inputs and seeds.
/// Checking in a second full <c>data/</c> tree would do it, and would rot the first time
/// somebody edited a character: the trial and the control would diverge on something the trial
/// never meant to own, and nobody would notice, because the diff that broke the comparison
/// would live in a file the trial does not care about.
///
/// So an overlay carries <b>only the files it overrides</b> and resolves everything else against
/// the shipped root. Three rules keep that honest:
///
/// <list type="bullet">
/// <item>Resolution is <b>per file</b>, never per field. A trial that wants a different
/// <c>flight.json</c> writes a whole <c>flight.json</c>. Merging fields would make a table that
/// is half one profile and half another, and "what is this trial changing?" would stop being
/// answerable by looking at the folder.</item>
/// <item>The tree <b>is</b> the declaration. A file is overridden because the overlay carries it,
/// not because a manifest says so; a manifest is a second place to keep in sync, and a file that
/// fell out of it would silently run the control.</item>
/// <item>An overlay may <b>override, never add</b>. A file the shipped root does not have is a
/// typo, and it stops the run — the same reasoning as #711's refusal to fall back silently.</item>
/// </list>
///
/// A checked-in trial is inert until a run names it in <see cref="OverlayVariable"/>. Like
/// <see cref="ContentCatalog.DataRootVariable"/>, that makes the control and the trial two runs
/// to diff rather than two tables inside one run.
/// </summary>
public sealed class DataRoot
{
    /// <summary>
    /// The environment variable that lays a trial overlay over the data root for a whole process.
    /// An absolute path is taken as given; a relative one is resolved beside the shipped root, so
    /// <c>GRAND_SLUGGERS_TRIAL=trials/c80</c> means the same thing from any working directory.
    /// </summary>
    public const string OverlayVariable = "GRAND_SLUGGERS_TRIAL";

    /// <summary>
    /// The one file an overlay may carry that the shipped root does not: a trial explaining what
    /// it changes and why. Everything else in the tree is data and must override something.
    /// </summary>
    public const string ReadmeFile = "README.md";

    readonly HashSet<string> _overrides;

    /// <summary>The shipped data root. Also the folder the repository sits above.</summary>
    public string Shipped { get; }

    /// <summary>The trial overlay laid over <see cref="Shipped"/>, or null when this run has none.</summary>
    public string? Overlay { get; }

    /// <summary>
    /// True when the environment named this root or its overlay — a run that asked for something
    /// other than the shipped defaults. Such a run must stop on a table it cannot read rather than
    /// fall back, or the control's numbers would play under the trial's name.
    /// </summary>
    public bool Named { get; init; }

    /// <summary>
    /// The files the overlay overrides, relative to either root and slash-separated, in ordinal
    /// order. Empty when there is no overlay — and empty when the overlay carries nothing, which
    /// is a trial that has not been authored yet, not an error.
    /// </summary>
    public IReadOnlyList<string> Overrides { get; }

    /// <summary>A root with no overlay: every file is read from <paramref name="shipped"/>.</summary>
    public DataRoot(string shipped) : this(shipped, null) { }

    public DataRoot(string shipped, string? overlay)
    {
        Shipped = System.IO.Path.GetFullPath(shipped);
        if (overlay is null)
        {
            Overlay = null;
            Overrides = [];
            _overrides = [];
            return;
        }
        Overlay = System.IO.Path.GetFullPath(overlay);
        Overrides = Declared(Shipped, Overlay);
        _overrides = new HashSet<string>(Overrides, StringComparer.Ordinal);
    }

    /// <summary>A bare path is a root with no overlay, so every existing caller keeps its meaning.</summary>
    public static implicit operator DataRoot(string path) => new(path);

    /// <summary>
    /// This run's root and overlay, as <c>cli</c> reports them. A trace whose provenance has to be
    /// reconstructed from memory is not evidence, so the line names every file the trial overrides.
    /// </summary>
    public string Provenance =>
        Overlay is null ? $"data root {Shipped}  overlay none"
        : Overrides.Count == 0 ? $"data root {Shipped}  overlay {Overlay} (overrides nothing)"
        : $"data root {Shipped}  overlay {Overlay} ({Overrides.Count} "
          + (Overrides.Count == 1 ? "file" : "files") + ": " + string.Join(", ", Overrides) + ")";

    /// <summary>
    /// Where one file lives for this run: the overlay's copy when it carries one, the shipped copy
    /// otherwise. Parts are the path below the root, e.g. <c>Resolve("rules", "flight.json")</c>.
    /// Naming a directory is the same call and always answers the shipped one, because an overlay
    /// overrides files rather than replacing folders.
    /// </summary>
    public string Resolve(params string[] parts)
    {
        var segments = Segments(parts);
        var relative = string.Join('/', segments);
        var root = Overlay is not null && _overrides.Contains(relative) ? Overlay : Shipped;
        return System.IO.Path.Combine(root, System.IO.Path.Combine(segments));
    }

    /// <summary>
    /// The path below a root, as one list of names. <see cref="Declared"/> builds its keys from the
    /// filesystem, so a caller's spelling has to be normalised the same way or a stray separator
    /// would miss the lookup and read the shipped file while provenance claimed the overlay.
    /// <c>.</c> and <c>..</c> are refused rather than normalised: nothing below a data root needs
    /// them, and a path that climbs out of the root is a bug, not a location.
    /// </summary>
    static string[] Segments(string[] parts)
    {
        var segments = parts
            .SelectMany(part => part.Split('/', '\\'))
            .Where(part => part.Length > 0)
            .ToArray();
        foreach (var segment in segments)
            if (segment is "." or "..")
                throw new ArgumentException(
                    $"'{string.Join('/', parts)}' walks the data root — name a file below it", nameof(parts));
        return segments;
    }

    /// <summary>
    /// A directory's files, resolved one by one. The listing and its order come from the shipped
    /// root: an overlay may substitute a park, never add a seventh one, so the set of files a run
    /// reads does not depend on whether a trial is named.
    /// </summary>
    public IReadOnlyList<string> Files(string directory, string pattern)
    {
        var dir = Resolve(directory);
        if (!System.IO.Directory.Exists(dir)) return [];
        return System.IO.Directory.GetFiles(dir, pattern)
            .Select(System.IO.Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => Resolve(directory, name!))
            .ToList();
    }

    /// <summary>True when the overlay, not the shipped root, answers this path.</summary>
    public bool Overridden(params string[] parts) =>
        Overlay is not null && _overrides.Contains(string.Join('/', Segments(parts)));

    /// <summary>
    /// The overlay as a run would name it: the path below the folder <see cref="Shipped"/> sits in
    /// when it is one of that folder's own (<c>trials/c80</c>), else the absolute path. This is what
    /// a saved trace records, so a trial authored in the repository attributes the same way on any
    /// machine. Null when there is no overlay.
    /// </summary>
    public string? OverlayName
    {
        get
        {
            if (Overlay is null) return null;
            var beside = System.IO.Directory.GetParent(Shipped)?.FullName;
            if (beside is null) return Overlay;
            var below = System.IO.Path.GetRelativePath(beside, Overlay).Replace(System.IO.Path.DirectorySeparatorChar, '/');
            return below.StartsWith("..", StringComparison.Ordinal) ? Overlay : below;
        }
    }

    /// <summary>
    /// The overlay <see cref="OverlayVariable"/> names, or null when it is unset. A value that is
    /// not a folder stops the run: a trial that quietly runs the control is worse than one that fails.
    /// </summary>
    public static string? NamedOverlay(string? value, string shipped)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var beside = System.IO.Directory.GetParent(System.IO.Path.GetFullPath(shipped))?.FullName;
        var path = System.IO.Path.GetFullPath(
            System.IO.Path.IsPathRooted(value) || beside is null
                ? value
                : System.IO.Path.Combine(beside, value));
        if (System.IO.Directory.Exists(path)) return path;
        throw new DirectoryNotFoundException(
            $"{OverlayVariable}={value} is not a folder (looked in {path}) — "
            + "name a trial overlay such as trials/c80, relative to the folder data/ sits in");
    }

    /// <summary>
    /// The shipped root with whatever overlay this process names. <paramref name="rootWasNamed"/>
    /// carries whether <see cref="ContentCatalog.DataRootVariable"/> chose the root, because either
    /// variable makes this a run that asked for something and must not silently get the control.
    /// </summary>
    public static DataRoot FromEnvironment(string shipped, bool rootWasNamed = false)
    {
        var overlay = NamedOverlay(Environment.GetEnvironmentVariable(OverlayVariable), shipped);
        return new DataRoot(shipped, overlay) { Named = rootWasNamed || overlay is not null };
    }

    /// <summary>
    /// What the overlay tree declares it overrides. Dot-files are not data — a stray
    /// <c>.DS_Store</c> must not fail a run — and <see cref="ReadmeFile"/> at the top is the trial
    /// explaining itself, exempt only while the shipped root has no README of its own. Everything
    /// else has to name a shipped file, spelled the way the shipped root spells it.
    /// </summary>
    static IReadOnlyList<string> Declared(string shipped, string overlay)
    {
        if (!System.IO.Directory.Exists(overlay))
            throw new DirectoryNotFoundException(
                $"trial overlay {overlay} is not a folder — an overlay is a tree of files that override the shipped root");

        var available = Relatives(shipped);
        var declared = new List<string>();
        var unknown = new List<string>();
        foreach (var relative in Relatives(overlay))
        {
            if (!available.Contains(relative))
            {
                if (relative.Equals(ReadmeFile, StringComparison.Ordinal)) continue;
                unknown.Add(relative + NearMiss(available, relative));
                continue;
            }
            declared.Add(relative);
        }
        if (unknown.Count > 0)
            throw new FileNotFoundException(
                $"trial overlay {overlay} names {string.Join(", ", unknown.OrderBy(x => x, StringComparer.Ordinal))}, "
                + $"which the shipped root {shipped} does not have — an overlay overrides shipped data, it never adds to it");

        declared.Sort(StringComparer.Ordinal);
        WholeFiles(shipped, overlay, declared);
        return declared.AsReadOnly();
    }

    /// <summary>
    /// A root's files, relative and slash-separated, as the filesystem spells them. Enumerating is
    /// what makes the spelling exact: <c>File.Exists</c> answers in the filesystem's own case, so on
    /// a case-insensitive disk an overlay carrying <c>rules/Flight.json</c> would be accepted and
    /// then never found again by <see cref="Resolve"/>, which matches ordinally — the run would read
    /// the shipped file while <see cref="Provenance"/> claimed the override applied. Worse, the same
    /// overlay would be refused outright on a case-sensitive one, so a trial would behave one way on
    /// a laptop and another in CI. Dot-files and dot-folders are skipped on both sides.
    /// </summary>
    static HashSet<string> Relatives(string root)
    {
        var relatives = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in System.IO.Directory.GetFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = System.IO.Path
                .GetRelativePath(root, file)
                .Replace(System.IO.Path.DirectorySeparatorChar, '/');
            if (relative.Split('/').Any(part => part.StartsWith('.'))) continue;
            relatives.Add(relative);
        }
        return relatives;
    }

    /// <summary>The shipped spelling of a stray file that differs only in case, named so a typo is one fix.</summary>
    static string NearMiss(HashSet<string> available, string relative)
    {
        var near = available.FirstOrDefault(x => x.Equals(relative, StringComparison.OrdinalIgnoreCase));
        return near is null ? "" : $" (the shipped root spells it {near})";
    }

    /// <summary>
    /// Whole files, never fields. A rules table names every field or the missing ones fall back to
    /// the C# defaults, and a trial would be half its own profile and half whatever the code says —
    /// the exact "half one table and half another" this mechanism exists to refuse. So a trial's
    /// copy must carry every key the shipped file carries; it is free to add none and to change any.
    ///
    /// Only object keys are walked. A trial may legitimately carry a different number of hazards or
    /// role players, so array elements are its own business, and a file the loader will reject
    /// anyway is left for the loader to report by name.
    /// </summary>
    static void WholeFiles(string shipped, string overlay, IReadOnlyList<string> declared)
    {
        var partial = new List<string>();
        foreach (var relative in declared)
        {
            if (!relative.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
            var native = relative.Replace('/', System.IO.Path.DirectorySeparatorChar);
            var missing = Missing(
                Parse(System.IO.Path.Combine(shipped, native)),
                Parse(System.IO.Path.Combine(overlay, native)));
            if (missing.Count > 0)
                partial.Add($"{relative} (no {string.Join(", ", missing.Take(4))}"
                    + (missing.Count > 4 ? $" and {missing.Count - 4} more)" : ")"));
        }
        if (partial.Count > 0)
            throw new InvalidDataException(
                $"trial overlay {overlay} carries part of {string.Join("; ", partial)} — "
                + "a trial writes whole files, so a field it does not name would quietly fall back to the code default");
    }

    static JsonNode? Parse(string path)
    {
        try
        {
            return JsonNode.Parse(
                System.IO.File.ReadAllText(path),
                documentOptions: new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return null;
        }
    }

    /// <summary>The object keys <paramref name="whole"/> has and <paramref name="part"/> does not.</summary>
    static List<string> Missing(JsonNode? whole, JsonNode? part)
    {
        var missing = new List<string>();
        if (whole is null || part is null) return missing;
        Walk(whole, part, "");
        return missing;

        void Walk(JsonNode? left, JsonNode? right, string path)
        {
            if (left is not JsonObject expected) return;
            if (right is not JsonObject actual)
            {
                missing.Add(path.Length == 0 ? "its fields" : path);
                return;
            }
            foreach (var (key, value) in expected)
            {
                var below = path.Length == 0 ? key : path + "." + key;
                if (!actual.TryGetPropertyValue(key, out var mine)) missing.Add(below);
                else Walk(value, mine, below);
            }
        }
    }
}
