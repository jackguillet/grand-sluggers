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
        var relative = string.Join('/', parts);
        var root = Overlay is not null && _overrides.Contains(relative) ? Overlay : Shipped;
        return System.IO.Path.Combine(root, System.IO.Path.Combine(parts));
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
        Overlay is not null && _overrides.Contains(string.Join('/', parts));

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

    /// <summary>The shipped root with whatever overlay this process names.</summary>
    public static DataRoot FromEnvironment(string shipped) =>
        new(shipped, NamedOverlay(Environment.GetEnvironmentVariable(OverlayVariable), shipped));

    /// <summary>
    /// What the overlay tree declares it overrides. Dot-files are not data — a stray
    /// <c>.DS_Store</c> must not fail a run — and <see cref="ReadmeFile"/> at the top is the
    /// trial explaining itself. Everything else has to name a shipped file.
    /// </summary>
    static IReadOnlyList<string> Declared(string shipped, string overlay)
    {
        if (!System.IO.Directory.Exists(overlay))
            throw new DirectoryNotFoundException(
                $"trial overlay {overlay} is not a folder — an overlay is a tree of files that override the shipped root");

        var declared = new List<string>();
        var unknown = new List<string>();
        foreach (var file in System.IO.Directory.GetFiles(overlay, "*", SearchOption.AllDirectories))
        {
            var relative = System.IO.Path
                .GetRelativePath(overlay, file)
                .Replace(System.IO.Path.DirectorySeparatorChar, '/');
            if (relative.Split('/').Any(part => part.StartsWith('.'))) continue;
            if (relative.Equals(ReadmeFile, StringComparison.Ordinal)) continue;
            if (System.IO.File.Exists(System.IO.Path.Combine(shipped, relative))) declared.Add(relative);
            else unknown.Add(relative);
        }
        if (unknown.Count > 0)
            throw new FileNotFoundException(
                $"trial overlay {overlay} names {string.Join(", ", unknown.OrderBy(x => x, StringComparer.Ordinal))}, "
                + $"which the shipped root {shipped} does not have — an overlay overrides shipped data, it never adds to it");
        declared.Sort(StringComparer.Ordinal);
        return declared;
    }
}
