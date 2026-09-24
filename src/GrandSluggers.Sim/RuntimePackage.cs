namespace GrandSluggers.Sim;

/// <summary>
/// What a player build carries beside the app (<c>data/package.json</c>): the runtime folders, and within
/// them only files with a listed extension. Tooling folders stay in the repository. Every folder at the
/// data root is declared one or the other, so a new folder is a decision, not an accident that ships.
/// <c>tools/local-player.py</c> copies exactly <see cref="Files"/>; the file stays plain JSON so Python
/// reads it without a comment-aware parser.
/// </summary>
public sealed class RuntimePackage
{
    public const string FileName = "package.json";

    public string About { get; init; } = "";
    public List<string> Runtime { get; init; } = [];
    public List<string> Tooling { get; init; } = [];
    public List<string> Extensions { get; init; } = [];

    public static string PathFor(string shipped) => Path.Combine(shipped, FileName);

    public static RuntimePackage Load(string shipped) => DataJson.Require<RuntimePackage>(PathFor(shipped));

    /// <summary>The files the player build carries, relative to the data root, slash-separated, in ordinal order.</summary>
    public IReadOnlyList<string> Files(string shipped) =>
        Runtime.SelectMany(folder => System.IO.Directory.EnumerateFiles(Path.Combine(shipped, folder), "*", SearchOption.AllDirectories))
            .Where(file => !Path.GetFileName(file).StartsWith('.'))
            .Where(file => Extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .Select(file => Path.GetRelativePath(shipped, file).Replace('\\', '/'))
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<string> Validate(string shipped)
    {
        var path = PathFor(shipped);
        var errors = new List<string>();
        var package = DataJson.Read<RuntimePackage>(path, errors);
        if (package is null) return errors;

        var declared = new HashSet<string>(StringComparer.Ordinal);
        foreach (var folder in package.Runtime.Concat(package.Tooling))
        {
            if (!declared.Add(folder))
                errors.Add($"{path}: folder '{folder}' is declared twice");
            if (folder.Length == 0 || folder.IndexOfAny(['/', '\\']) >= 0 || folder.StartsWith('.'))
                errors.Add($"{path}: folder '{folder}' must be one folder name at the data root");
            else if (!System.IO.Directory.Exists(Path.Combine(shipped, folder)))
                errors.Add($"{path}: folder '{folder}' does not exist");
        }
        if (!package.Tooling.Contains(AgentData.Directory, StringComparer.Ordinal))
            errors.Add($"{path}: '{AgentData.Directory}' must be a tooling folder; the game never reads it");
        foreach (var folder in System.IO.Directory.EnumerateDirectories(shipped).Select(Path.GetFileName))
            if (folder is not null && !folder.StartsWith('.') && !declared.Contains(folder))
                errors.Add($"{path}: data folder '{folder}' is neither runtime nor tooling");
        foreach (var file in System.IO.Directory.EnumerateFiles(shipped).Select(Path.GetFileName))
            if (file is not null && !file.StartsWith('.') && file != FileName)
                errors.Add($"{path}: '{file}' sits at the data root, outside every folder the package declares");

        if (package.Extensions.Count == 0)
            errors.Add($"{path}: extensions must list at least one file type");
        foreach (var extension in package.Extensions)
            if (extension.Length < 2 || extension[0] != '.' || extension.IndexOf('.', 1) >= 0)
                errors.Add($"{path}: extension '{extension}' must look like '.json'");
        return errors;
    }
}
